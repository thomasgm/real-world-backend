using Microsoft.EntityFrameworkCore;
using SkyRoute.Mvc.Models;

namespace SkyRoute.Mvc.Data;

public class SkyDbContext : DbContext
{
    public SkyDbContext(DbContextOptions<SkyDbContext> options) : base(options)
    {
    }

    public DbSet<Airport> Airports { get; set; } = null!;
    public DbSet<Airline> Airlines { get; set; } = null!;
    public DbSet<Flight> Flights { get; set; } = null!;
    public DbSet<Passenger> Passengers { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Airport>().ToTable("Airports");
        modelBuilder.Entity<Airline>().ToTable("Airlines");
        modelBuilder.Entity<Flight>().ToTable("Flights");
        modelBuilder.Entity<Passenger>().ToTable("Passengers");
        modelBuilder.Entity<Ticket>().ToTable("Tickets");
    }
}