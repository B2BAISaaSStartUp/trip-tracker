using Microsoft.EntityFrameworkCore;
using TripTracker.Markers.Domain;

namespace TripTracker.Markers.Persistence;

/// <summary>EF Core context for the "markers" schema.</summary>
internal sealed class MarkersDbContext : DbContext
{
    public const string SchemaName = "markers";

    public MarkersDbContext(DbContextOptions<MarkersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Marker> Markers => Set<Marker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new MarkerConfiguration());
    }
}
