using Microsoft.EntityFrameworkCore;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Persistence;

/// <summary>EF Core context for the "trips" schema.</summary>
internal sealed class TripsDbContext : DbContext
{
    public const string SchemaName = "trips";

    public TripsDbContext(DbContextOptions<TripsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new TripConfiguration());
    }
}
