using CreditosApi.Models.Entities;
using CreditosApi.Models.Request;

namespace CreditosApi.Interfaces;

/// <summary>
/// Interface específica para o repositório de Crédito
/// </summary>
public interface ICreditoRepository : IRepository<CreditoIntegracao>
{
     Task<IEnumerable<CreditoIntegracao>> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken cancellationToken);
 }
