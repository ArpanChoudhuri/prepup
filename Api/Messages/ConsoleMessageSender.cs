using Serilog;

namespace Api.Messaging;
public class ConsoleMessageSender : IMessageSender
{
    public Task SendAsync(string type, string payload, CancellationToken ct)
    {
        Log.Information("Dispatching message {Type}: {Payload}", type, payload);
        return Task.CompletedTask;
    }
}
