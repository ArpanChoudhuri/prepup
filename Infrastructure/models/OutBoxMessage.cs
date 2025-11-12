namespace Infra.Data;
public class OutboxMessage
{
    public long Id { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = default!;            // e.g., "OrderCreated"
    public string Payload { get; set; } = default!;         // JSON
    public int Attempt { get; set; }                        // dispatch attempts
    public DateTime? NextAttemptUtc { get; set; }           // backoff
    public DateTime? DispatchedUtc { get; set; }
    public string? Error { get; set; }
}
