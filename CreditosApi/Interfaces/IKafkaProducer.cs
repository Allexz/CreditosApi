namespace CreditosApi.Interfaces;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string message, CancellationToken cancellationToken = default);
}

