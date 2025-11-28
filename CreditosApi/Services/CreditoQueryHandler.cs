using CreditosApi.Interfaces;
using CreditosApi.Models.Entities;
using CreditosApi.Models.Queries;
using CreditosApi.Models.Response;

namespace CreditosApi.Services;

public class CreditoQueryHandler :
    IQueryHandler<CreditoConstituidoConsultaQuery, CreditoIntegracaoResponse?>
{

    private readonly ICreditoRepository _creditoRepository;
    public CreditoQueryHandler(ICreditoRepository creditoRepository)
    {
        _creditoRepository = creditoRepository;
    }

    public async Task<CreditoIntegracaoResponse?> Handle(CreditoConstituidoConsultaQuery query, CancellationToken cancellationToken)
    {
        IEnumerable<CreditoIntegracao> resultList = await _creditoRepository.GetByNumeroCreditoAsync(query.numero_credito, cancellationToken);
        if (!resultList.Any())
        {
            return null;
        }
        CreditoIntegracao response = resultList.First();
        return new CreditoIntegracaoResponse(response.NumeroCredito,
                                             response.NumeroNfse,
                                             response.DataConstituicao,
                                             response.ValorIssqn,
                                             response.TipoCredito,
                                             response.SimplesNacional,
                                             response.Aliquota,
                                             response.ValorFaturado,
                                             response.ValorDeducao,
                                             response.BaseCalculo);
    }
}
