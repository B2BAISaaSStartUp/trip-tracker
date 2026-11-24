using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Persistence;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.OwnerId).IsRequired();

        // The title is stored as a plain string column so searches can be translated to SQL.
        builder.Ignore(t => t.Title);
        builder.Property<string>(Trip.TitleFieldName)
            .HasColumnName("title")
            .HasMaxLength(TripTitle.MaxLength)
            .IsRequired();

        builder.Property(t => t.Description).HasMaxLength(Trip.DescriptionMaxLength);
        builder.Property(t => t.StartDate).IsRequired();
        builder.Property(t => t.EndDate).IsRequired();
        builder.Property(t => t.AccessLevel).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(t => t.ShareToken).HasMaxLength(64).IsRequired();
        builder.Property(t => t.IsHidden).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.ShareToken).IsUnique();
    }
}
