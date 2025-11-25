using System.Linq.Expressions;

namespace CreditosApi.Interfaces;

/// <summary>
/// Interface genérica para repositórios que define operações CRUD básicas
/// </summary>
/// <typeparam name="T">Tipo da entidade</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Obtém uma entidade pelo ID
    /// </summary>
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as entidades
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém entidades que correspondem ao predicado especificado
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a primeira entidade que corresponde ao predicado especificado
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe alguma entidade que corresponde ao predicado especificado
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta o número de entidades que correspondem ao predicado especificado
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova entidade
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona múltiplas entidades
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma entidade existente
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Atualiza múltiplas entidades
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Remove uma entidade
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Remove múltiplas entidades
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Remove uma entidade pelo ID
    /// </summary>
    Task<bool> RemoveByIdAsync(long id, CancellationToken cancellationToken = default);
}



