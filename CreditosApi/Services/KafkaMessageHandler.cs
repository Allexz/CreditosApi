using CreditosApi.Interfaces;
using CreditosApi.Models.Entities;
using CreditosApi.Models.Request;
using System.Text.Json;

namespace CreditosApi.Services;

/// <summary>
/// Implementação do handler de mensagens do Kafka
/// </summary>
public class KafkaMessageHandler : IKafkaMessageHandler
{
     private readonly ILogger<KafkaMessageHandler> _logger;
    private readonly ICreditoRepository _creditoRepository;

    public KafkaMessageHandler(ILogger<KafkaMessageHandler> logger, ICreditoRepository creditoRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _creditoRepository = creditoRepository;
    }

    public async Task HandleAsync(string messageJson, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Processando mensagem do Kafka: {Message}", messageJson);

            // Deserializa a mensagem JSON
            var listaCreditos = JsonSerializer.Deserialize<ListaCreditosIntegracaoRequest>(
                messageJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (listaCreditos?.Creditos == null || !listaCreditos.Creditos.Any())
            {
                _logger.LogWarning("Mensagem do Kafka não contém créditos válidos");
                return;
            }

            // Inicia uma transação
            await _creditoRepository.BeginTransactionAsync(ct);

            try
            {
                // Processa cada crédito
                foreach (var credito in listaCreditos.Creditos)
                {
                    // Verifica se o crédito já existe
                    var creditosExistentes = await _creditoRepository.GetByNumeroCreditoAsync(credito.NumeroCredito, ct);

                    if (!creditosExistentes.Any())
                    {

                        // Valida e cria a entidade de crédito
                        var result = CreditoIntegracao.Create(credito.NumeroCredito,
                                                              credito.NumeroNfse,
                                                              credito.DataConstituicao,
                                                              credito.ValorIssqn,
                                                              credito.TipoCredito,
                                                              credito.SimplesNacional,
                                                              credito.Aliquota,
                                                              credito.ValorFaturado,
                                                              credito.ValorDeducao,
                                                              credito.BaseCalculo);

                        if (result.IsFailure)
                        {
                            throw new Exception(result?.Error);
                        }

                        // Adiciona o novo crédito
                        await _creditoRepository.AddAsync(result.Value, ct);
                        _logger.LogInformation("Crédito {NumeroCredito} adicionado com sucesso",
                            credito.NumeroCredito);
                    }
                    else
                    {
                        _logger.LogWarning("Crédito {NumeroCredito} já existe no banco de dados",
                            credito.NumeroCredito);
                    }
                }

                // Salva as alterações e confirma a transação
                await _creditoRepository.SaveChangesAsync(ct);
                await _creditoRepository.CommitTransactionAsync(ct);

                _logger.LogInformation("Processados {Count} créditos com sucesso",
                    listaCreditos.Creditos.Count());
            }
            catch (Exception ex)
            {
                // Reverte a transação em caso de erro
                await _creditoRepository.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Erro ao processar créditos. Transação revertida.");
                throw;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar mensagem do Kafka: {Message}", messageJson);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do Kafka: {Message}", messageJson);
            throw;
        }
    }
}

