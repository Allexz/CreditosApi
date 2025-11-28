namespace CreditosApi.Interfaces;

/// <summary>
/// Interface para processar créditos individualmente
/// </summary>
public interface ICreditoProcessor
{
    /// <summary>
    /// Processa uma mensagem do Kafka e insere créditos individualmente na base de dados
    /// </summary>
    Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken = default);
}

