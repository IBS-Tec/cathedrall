using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class PessoaConfiguration : IEntityTypeConfiguration<Pessoa>
{
    public void Configure(EntityTypeBuilder<Pessoa> builder)
    {
        builder.ToTable("pessoas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).HasMaxLength(120);

        builder.Property(p => p.NomeNormalizado).HasMaxLength(120);

        builder.Property(p => p.Celular).HasMaxLength(20);

        builder.Property(p => p.Email).HasMaxLength(200);

        builder.Property(p => p.EstadoCivil).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.Profissao).HasMaxLength(120);

        builder.ComplexProperty(p => p.Endereco, endereco =>
        {
            endereco.Property(e => e.Cep).HasMaxLength(8);
            endereco.Property(e => e.Logradouro).HasMaxLength(150);
            endereco.Property(e => e.Numero).HasMaxLength(10);
            endereco.Property(e => e.Complemento).HasMaxLength(60);
            endereco.Property(e => e.Bairro).HasMaxLength(80);
            endereco.Property(e => e.Cidade).HasMaxLength(80);
            endereco.Property(e => e.Uf).HasMaxLength(2);
        });

        builder.HasMany(p => p.Vinculos)
            .WithOne()
            .HasForeignKey(v => v.PessoaId);

        builder.Navigation(p => p.Vinculos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.NomeNormalizado);
    }
}
