using Confluent.Kafka;
using CreditosApi.Interfaces;

namespace CreditosApi.Services;

public class KafkaBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaBackgroundService> _logger;
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaBackgroundService(IServiceScopeFactory scopeFactory,
                                  ILogger<KafkaBackgroundService> logger,
                                  IConsumer<Ignore, string> consumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("lancamentos-topic");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                using IServiceScope? scope = _scopeFactory.CreateScope();
                IKafkaMessageHandler handler = scope.ServiceProvider.GetRequiredService<IKafkaMessageHandler>();

                await handler.HandleAsync(consumeResult.Message.Value, stoppingToken);

                _consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consumir mensagem do Kafka");
                await Task.Delay(1000, stoppingToken); // back-off simples
            }
        }

        _consumer.Close();
    }
}
