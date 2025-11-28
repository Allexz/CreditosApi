using CreditosApi.Interfaces;
using CreditosApi.Models.Response;
namespace CreditosApi.Models.Queries;
public sealed record CreditoConstituidoConsultaQuery(string numero_credito) : IQuery<CreditoIntegracaoResponse?>;
