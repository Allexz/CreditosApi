using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CreditosApi.Models.Request;

namespace CreditosApi.Data.Configuration;

public class CreditoConfiguration : IEntityTypeConfiguration<CreditoIntegracaoRequest>
{
    public void Configure(EntityTypeBuilder<CreditoIntegracaoRequest> builder)
    {
         builder.ToTable("credito");

         builder.HasKey(c => c.Id);

         builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

         builder.Property(c => c.NumeroCredito)
            .HasColumnName("numero_credito")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.NumeroNfse)
            .HasColumnName("numero_nfse")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.DataConstituicao)
            .HasColumnName("data_constituicao")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.ValorIssqn)
            .HasColumnName("valor_issqn")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(c => c.TipoCredito)
            .HasColumnName("tipo_credito")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.SimplesNacional)
            .HasColumnName("simples_nacional")
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(c => c.Aliquota)
            .HasColumnName("aliquota")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(c => c.ValorFaturado)
            .HasColumnName("valor_faturado")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(c => c.ValorDeducao)
            .HasColumnName("valor_deducao")
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(c => c.BaseCalculo)
            .HasColumnName("base_calculo")
            .HasColumnType("decimal(15,2)")
            .IsRequired();
    }
}