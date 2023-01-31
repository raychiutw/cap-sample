using Microsoft.EntityFrameworkCore;

namespace PMP.EdgeService.Persistence;

public class CapDbContext : DbContext
{
    public CapDbContext(
        DbContextOptions<CapDbContext> options) : base(options)
    {
    }
}