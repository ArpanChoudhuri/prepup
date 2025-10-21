using Api.data;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;

namespace Api.Messaging;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(2);
    private static readonly int MaxAttempts = 5;

    public OutboxDispatcher(IServiceProvider sp) => _sp = sp;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delaySeq = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var sender = scope.ServiceProvider.GetRequiredService<IMessageSender>();

                var now = DateTime.UtcNow;
                var batch = await db.Outbox
                    .Where(x => x.DispatchedUtc == null && (x.NextAttemptUtc == null || x.NextAttemptUtc <= now))
                    .OrderBy(x => x.Id)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var m in batch)
                {
                    try
                    {
                        if (!await db.TryMarkProcessedAsync(m.Id, stoppingToken))
                        {
                            Log.Information("Skip already processed {Id}", m.Id);
                            m.DispatchedUtc = DateTime.UtcNow; // or keep it as is if only skipping consumer
                            continue;
                        }
                        await Policy
                            .Handle<Exception>()
                            .WaitAndRetryAsync(delaySeq)
                            .ExecuteAsync(ct => sender.SendAsync(m.Type, m.Payload, ct), stoppingToken);

                        m.DispatchedUtc = DateTime.UtcNow;
                        m.Error = null;
                    }
                    catch (Exception ex)
                    {
                        m.Attempt++;
                        m.Error = ex.Message;
                        if (m.Attempt >= MaxAttempts)
                        {
                            // poison: park it far in the future (or move to DLQ table)
                            m.NextAttemptUtc = DateTime.UtcNow.AddYears(100);
                            Log.Error(ex, "Message {Id} moved to poison", m.Id);
                        }
                        else
                        {
                            // exponential-ish retry with jitter
                            m.NextAttemptUtc = DateTime.UtcNow.AddSeconds(Math.Pow(2, m.Attempt) + Random.Shared.Next(1, 4));
                        }
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
                Log.Information("Dispatched message  for {Orders}", batch.Count());

            }
            catch (Exception ex)
            {
                // guard the loop: never crash the service
                Log.Error(ex, "Outbox loop error");
            }

            await Task.Delay(Poll, stoppingToken);
        }
    }
}
