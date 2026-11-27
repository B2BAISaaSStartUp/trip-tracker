using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Persistence;

internal sealed class TrackPointConfiguration : IEntityTypeConfiguration<TrackPoint>
{
    public void Configure(EntityTypeBuilder<TrackPoint> builder)
    {
        builder.ToTable("track_points");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.TripId).IsRequired();
        builder.Property(p => p.RecordedBy).IsRequired();
        builder.Property(p => p.ClientPointId).IsRequired();
        builder.Property(p => p.Latitude).IsRequired();
        builder.Property(p => p.Longitude).IsRequired();
        builder.Property(p => p.RecordedAt).IsRequired();
        builder.Property(p => p.ReceivedAt).IsRequired();

        // The database itself guarantees that a point is stored once (NFR-08), even for two overlapping uploads.
        builder.HasIndex(p => new { p.TripId, p.ClientPointId }).IsUnique();
        builder.HasIndex(p => new { p.TripId, p.RecordedAt });
    }
}
