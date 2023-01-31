using Microsoft.EntityFrameworkCore;

namespace PMP.EdgeService.Persistence;

public class PmpDbContext : DbContext
{
    public PmpDbContext(
        DbContextOptions<PmpDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PmpDbContext).Assembly);
    }
}