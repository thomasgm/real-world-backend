using Microsoft.EntityFrameworkCore;
using CricketStatsPro.Mvc.Models;

namespace CricketStatsPro.Mvc.Data;

public class T20DbContext : DbContext
{
    public T20DbContext(DbContextOptions<T20DbContext> options) : base(options)
    {
    }

    public DbSet<T20Match> Matches { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<T20Match>().ToTable("T20I");
    }
}