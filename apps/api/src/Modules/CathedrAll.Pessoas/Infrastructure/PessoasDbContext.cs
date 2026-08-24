using CathedrAll.Pessoas.Domain;
using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Pessoas.Infrastructure;

internal sealed class PessoasDbContext(DbContextOptions<PessoasDbContext> options)
    : DbContext(options)
{
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<PessoaId>().HaveConversion<PessoaIdConverter>();
        configurationBuilder.Properties<VinculoIgrejaId>().HaveConversion<VinculoIgrejaIdConverter>();
        configurationBuilder.Properties<Celular>().HaveConversion<CelularConverter>();
        configurationBuilder.Properties<Email>().HaveConversion<EmailConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pessoas");

        modelBuilder.ApplyConfiguration(new PessoaConfiguration());
        modelBuilder.ApplyConfiguration(new VinculoIgrejaConfiguration());
    }
}
