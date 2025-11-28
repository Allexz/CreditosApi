using CreditosApi.Interfaces;
using CreditosApi.Models.Entities;
using CreditosApi.Models.Request;
using System.Text.Json;

namespace CreditosApi.Services;

/// <summary>
/// Processador de créditos que insere individualmente na base de dados
/// </summary>
public class CreditoProcessor : ICreditoProcessor
{
    private readonly ILogger<CreditoProcessor> _logger;
    private readonly ICreditoRepository _creditoRepository;

    public CreditoProcessor(ILogger<CreditoProcessor> logger, ICreditoRepository creditoRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _creditoRepository = creditoRepository ?? throw new ArgumentNullException(nameof(creditoRepository));
    }

    public async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Processando mensagem: {Message}", messageJson);

            // Deserializa a mensagem JSON
            var listaCreditos = JsonSerializer.Deserialize<ListaCreditosIntegracaoRequest>(
                messageJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (listaCreditos?.Creditos == null || !listaCreditos.Creditos.Any())
            {
                _logger.LogWarning("Mensagem não contém créditos válidos");
                return;
            }

            // Processa cada crédito individualmente (não em bulk)
            foreach (CreditoIntegracaoRequest credito in listaCreditos.Creditos)
            {
                try
                {
                    // Verifica se o crédito já existe
                    var creditosExistentes = await _creditoRepository.GetByNumeroCreditoAsync(
                        credito.NumeroCredito, 
                        cancellationToken);

                    if (!creditosExistentes.Any())
                    {
                        // Inicia uma transação para este crédito individual
                        await _creditoRepository.BeginTransactionAsync(cancellationToken);

                        try
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


                            // Adiciona o crédito
                            await _creditoRepository.AddAsync(result.Value, cancellationToken);
                            
                            // Salva e confirma a transação
                            await _creditoRepository.SaveChangesAsync(cancellationToken);
                            await _creditoRepository.CommitTransactionAsync(cancellationToken);

                            _logger.LogInformation(
                                "Crédito {NumeroCredito} inserido com sucesso",
                                credito.NumeroCredito);
                        }
                        catch (Exception ex)
                        {
                            // Reverte a transação em caso de erro
                            await _creditoRepository.RollbackTransactionAsync(cancellationToken);
                            _logger.LogError(
                                ex,
                                "Erro ao inserir crédito {NumeroCredito}. Transação revertida.",
                                credito.NumeroCredito);
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Crédito {NumeroCredito} já existe no banco de dados",
                            credito.NumeroCredito);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao processar crédito {NumeroCredito}",
                        credito.NumeroCredito);
                    // Continua processando os próximos créditos mesmo se um falhar
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar mensagem: {Message}", messageJson);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem: {Message}", messageJson);
            throw;
        }
    }
}

