using CreditosApi.Models.Request;

namespace CreditosApi.Interfaces;

/// <summary>
/// Interface específica para o repositório de Crédito
/// </summary>
public interface ICreditoRepository : IRepository<CreditoIntegracaoRequest>
{
     Task<IEnumerable<CreditoIntegracaoRequest>> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken cancellationToken);
 }
