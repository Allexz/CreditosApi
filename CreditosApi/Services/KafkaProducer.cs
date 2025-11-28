using Confluent.Kafka;
using CreditosApi.Interfaces;

namespace CreditosApi.Services;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(ILogger<KafkaProducer> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var kafkaBootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"]
            ?? configuration["Kafka:BootstrapServers"]
            ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync(string topic, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var kafkaMessage = new Message<Null, string> { Value = message };
            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            
            _logger.LogInformation(
                "Mensagem publicada no tópico {Topic} - Partição: {Partition}, Offset: {Offset}",
                deliveryResult.Topic,
                deliveryResult.Partition,
                deliveryResult.Offset);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem no tópico {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}

