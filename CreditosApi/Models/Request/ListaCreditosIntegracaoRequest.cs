namespace CreditosApi.Models.Request;

public sealed record ListaCreditosIntegracaoRequest(IEnumerable<CreditoIntegracaoRequest> Creditos);
