namespace CreditosApi.Models.Response;

public sealed record CreditoIntegracaoResponse(
                             string NumeroCredito,
                             string NumeroNfse,
                             DateTime DataConstituicao,
                             decimal ValorIssqn,
                             string TipoCredito,
                             bool SimplesNacional,
                             decimal Aliquota,
                             decimal ValorFaturado,
                             decimal ValorDeducao,
                             decimal BaseCalculo);
