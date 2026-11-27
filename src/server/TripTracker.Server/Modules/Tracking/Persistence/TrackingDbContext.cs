using Microsoft.EntityFrameworkCore;
using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Persistence;

/// <summary>EF Core context for the "tracking" schema.</summary>
internal sealed class TrackingDbContext : DbContext
{
    public const string SchemaName = "tracking";

    public TrackingDbContext(DbContextOptions<TrackingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TrackPoint> TrackPoints => Set<TrackPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new TrackPointConfiguration());
    }
}
