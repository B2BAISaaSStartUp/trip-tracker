using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripTracker.Markers.Domain;

namespace TripTracker.Markers.Persistence;

internal sealed class MarkerConfiguration : IEntityTypeConfiguration<Marker>
{
    public void Configure(EntityTypeBuilder<Marker> builder)
    {
        builder.ToTable("markers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.TripId).IsRequired();
        builder.Property(m => m.CreatedBy).IsRequired();

        // The title is stored as a plain string column; the value object is rebuilt from it.
        builder.Ignore(m => m.Title);
        builder.Property<string>(Marker.TitleFieldName)
            .HasColumnName("title")
            .HasMaxLength(MarkerTitle.MaxLength)
            .IsRequired();

        builder.Property(m => m.Description).HasMaxLength(Marker.DescriptionMaxLength);
        builder.Property(m => m.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(m => m.Latitude).IsRequired();
        builder.Property(m => m.Longitude).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => m.TripId);
    }
}
