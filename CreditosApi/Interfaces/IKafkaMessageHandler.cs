namespace CreditosApi.Interfaces;

internal interface IKafkaMessageHandler
{
    Task HandleAsync(string messageJson, CancellationToken ct);
}
