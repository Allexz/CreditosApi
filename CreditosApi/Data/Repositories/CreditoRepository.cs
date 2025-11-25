using CreditosApi.Interfaces;
using CreditosApi.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.Data.Repositories;

/// <summary>
/// Implementação específica do repositório de Crédito
/// </summary>
public class CreditoRepository : Repository<CreditoIntegracaoRequest>, ICreditoRepository
{
    public CreditoRepository(CreditoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CreditoIntegracaoRequest>> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken cancellationToken)
    {
        return await _dbSet.Where(c => c.NumeroCredito == numeroCredito).ToListAsync();
    }
}



