using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class VinculoIgrejaConfiguration : IEntityTypeConfiguration<VinculoIgreja>
{
    public void Configure(EntityTypeBuilder<VinculoIgreja> builder)
    {
        builder.ToTable("vinculos_igreja");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(v => v.Motivo).HasMaxLength(500);

        builder.HasIndex(v => new { v.PessoaId, v.DataFim });
    }
}
