using CreditosApi.Models.Common;

namespace CreditosApi.Models.Entities;

public sealed class CreditoIntegracao
{
    public long Id { get; private set; }
    public string NumeroCredito { get; private set; }
    public string NumeroNfse { get; private set; }
    public DateTime DataConstituicao { get; private set; }
    public decimal ValorIssqn { get; private set; }
    public string TipoCredito { get; private set; }
    public bool SimplesNacional { get; private set; }
    public decimal Aliquota { get; private set; }
    public decimal ValorFaturado { get; private set; }
    public decimal ValorDeducao { get; private set; }
    public decimal BaseCalculo { get; private set; }

    // Construtor principal para inicialização
    private CreditoIntegracao(string numeroCredito,
                             string numeroNfse,
                             DateTime dataConstituicao,
                             decimal valorIssqn,
                             string tipoCredito,
                             bool simplesNacional,
                             decimal aliquota,
                             decimal valorFaturado,
                             decimal valorDeducao,
                             decimal baseCalculo)
    {
        NumeroCredito = numeroCredito;
        NumeroNfse = numeroNfse;
        DataConstituicao = dataConstituicao;
        ValorIssqn = valorIssqn;
        TipoCredito = tipoCredito;
        SimplesNacional = simplesNacional;
        Aliquota = aliquota;
        ValorFaturado = valorFaturado;
        ValorDeducao = valorDeducao;
        BaseCalculo = baseCalculo;
    }

    public static Result<CreditoIntegracao> Create(string numeroCredito,
                                                   string numeroNfse,
                                                   DateTime dataConstituicao,
                                                   decimal valorIssqn,
                                                   string tipoCredito,
                                                   bool simplesNacional,
                                                   decimal aliquota,
                                                   decimal valorFaturado,
                                                   decimal valorDeducao,
                                                   decimal baseCalculo)
    {
        // Validações para campos não nulos
        if (string.IsNullOrWhiteSpace(numeroCredito))
            return Result<CreditoIntegracao>.Failure("Número do crédito não pode ser nulo ou vazio.");
        
        if (string.IsNullOrWhiteSpace(numeroNfse))
            return Result<CreditoIntegracao>.Failure("Número da NFSE não pode ser nulo ou vazio.");
        
        if (string.IsNullOrWhiteSpace(tipoCredito))
            return Result<CreditoIntegracao>.Failure("Tipo de crédito não pode ser nulo ou vazio.");
        
        // Validações para valores decimais maiores que zero
        if (valorIssqn <= 0)
            return Result<CreditoIntegracao>.Failure("Valor do ISSQN deve ser maior que zero.");
        
        if (aliquota <= 0)
            return Result<CreditoIntegracao>.Failure("Alíquota deve ser maior que zero.");
        
        if (valorFaturado <= 0)
            return Result<CreditoIntegracao>.Failure("Valor faturado deve ser maior que zero.");
        
        if (baseCalculo <= 0)
            return Result<CreditoIntegracao>.Failure("Base de cálculo deve ser maior que zero.");
        
        // Validação para data não ser no passado
        if (dataConstituicao < DateTime.Today)
            return Result<CreditoIntegracao>.Failure("Data de constituição não pode ser no passado.");
        
        return Result<CreditoIntegracao>.Success(new CreditoIntegracao(numeroCredito,
                                                                       numeroNfse,
                                                                       dataConstituicao,
                                                                       valorIssqn,
                                                                       tipoCredito,
                                                                       simplesNacional,
                                                                       aliquota,
                                                                       valorFaturado,
                                                                       valorDeducao,
                                                                       baseCalculo))   ;
    }

}
