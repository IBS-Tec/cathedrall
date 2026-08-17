using Microsoft.EntityFrameworkCore;

namespace CathedrAll.Kernel.Infrastructure.Tests;

internal sealed class FakeDbContext(DbContextOptions<FakeDbContext> options) : DbContext(options)
{
    public DbSet<FakeRow> Rows => Set<FakeRow>();
}
