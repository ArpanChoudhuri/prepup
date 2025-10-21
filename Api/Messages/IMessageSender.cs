namespace Api.Messaging;
public interface IMessageSender
{
    Task SendAsync(string type, string payload, CancellationToken ct);
}
