using Microsoft.EntityFrameworkCore;
using IPLAnalysis.Mvc.Models;

namespace IPLAnalysis.Mvc.Data;

public class IplDbContext : DbContext
{
    public IplDbContext(DbContextOptions<IplDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().ToTable("IPLPlayers");
    }
}
