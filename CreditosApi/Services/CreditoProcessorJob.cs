using Confluent.Kafka;
using CreditosApi.Interfaces;
using Quartz;

namespace CreditosApi.Services;

/// <summary>
/// Job do Quartz que verifica mensagens no Kafka e processa créditos
/// </summary>
[DisallowConcurrentExecution]
public class CreditoProcessorJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreditoProcessorJob> _logger;
    private const string TOPIC_NAME = "integrar-credito-constituido-entry";

    public CreditoProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CreditoProcessorJob> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IConsumer<Ignore, string>? consumer = null;
        
        try
        {
            // Obtém o Consumer do JobDataMap
            consumer = context.JobDetail.JobDataMap["Consumer"] as IConsumer<Ignore, string>;
            
            if (consumer == null)
            {
                _logger.LogError("Consumer não encontrado no JobDataMap");
                return;
            }
            
            // Verifica se há mensagens disponíveis (timeout de 500ms para não bloquear)
            ConsumeResult<Ignore, string>? consumeResult = null;
            
            try
            {
                consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
            }
            catch (ConsumeException ex)
            {
                // Trata erros específicos do Kafka Consumer
                if (ex.Error.Code == ErrorCode.Local_PartitionEOF)
                {
                    // Fim da partição - não há mais mensagens, isso é normal
                    return;
                }
                
                if (ex.Error.Code == ErrorCode.InvalidSessionTimeout)
                {
                    // Timeout - não há mensagens disponíveis no momento, isso é normal
                    return;
                }
                
                // Outros erros de consumo devem ser logados
                _logger.LogWarning(ex, "Erro ao consumir mensagem do Kafka. Código: {ErrorCode}", ex.Error.Code);
                return;
            }

            // Verifica se há uma mensagem válida
            if (consumeResult == null || consumeResult.IsPartitionEOF)
            {
                // Não há mensagens disponíveis - isso é normal, não é um erro
                return;
            }

            // Processa a mensagem
            try
            {
                _logger.LogDebug(
                    "Mensagem recebida do tópico {Topic} - Partição: {Partition}, Offset: {Offset}",
                    consumeResult.Topic,
                    consumeResult.Partition,
                    consumeResult.Offset);

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ICreditoProcessor>();

                // Processa a mensagem (insere créditos individualmente)
                await processor.ProcessMessageAsync(consumeResult.Message.Value, context.CancellationToken);

                // Confirma o processamento da mensagem
                consumer.Commit(consumeResult);
                
                _logger.LogInformation(
                    "Mensagem processada com sucesso - Partição: {Partition}, Offset: {Offset}",
                    consumeResult.Partition,
                    consumeResult.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Erro ao processar mensagem - Partição: {Partition}, Offset: {Offset}",
                    consumeResult?.Partition,
                    consumeResult?.Offset);
                
                // Não faz commit em caso de erro - a mensagem será reprocessada
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            // Operação cancelada - não é um erro, apenas retorna
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar job de processamento de créditos");
            // Não relança a exceção para evitar que o Quartz marque o job como falho
            // O job continuará sendo executado no próximo trigger
        }
    }

}

