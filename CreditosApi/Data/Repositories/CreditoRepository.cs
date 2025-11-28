using CreditosApi.Interfaces;
using CreditosApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.Data.Repositories;

/// <summary>
/// Implementação específica do repositório de Crédito
/// </summary>
public class CreditoRepository : Repository<CreditoIntegracao>, ICreditoRepository
{
    public CreditoRepository(CreditoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CreditoIntegracao>> GetByNumeroCreditoAsync(string numeroCredito, CancellationToken cancellationToken)
    {
        return await _dbSet.Where(c => c.NumeroCredito == numeroCredito).ToListAsync();
    }
}



