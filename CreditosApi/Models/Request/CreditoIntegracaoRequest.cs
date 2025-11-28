namespace CreditosApi.Models.Request;

public sealed record CreditoIntegracaoRequest(string NumeroCredito,
                                              string NumeroNfse,
                                              DateTime DataConstituicao,
                                              decimal ValorIssqn,
                                              string TipoCredito,
                                              bool SimplesNacional,
                                              decimal Aliquota,
                                              decimal ValorFaturado,
                                              decimal ValorDeducao,
                                              decimal BaseCalculo);
