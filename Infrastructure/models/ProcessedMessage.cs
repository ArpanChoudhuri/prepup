namespace Infra.models
{
    public class ProcessedMessage
    {
        public long MessageId { get; set; }
        public DateTime ProcessedUtc { get; set; } = DateTime.UtcNow;
    }

}
