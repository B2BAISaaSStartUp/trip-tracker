using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripTracker.Photos.Domain;

namespace TripTracker.Photos.Persistence;

internal sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(p => p.Id);

        // TripId and MarkerId point into other modules' tables, so they are plain columns without foreign keys.
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.TripId).IsRequired();
        builder.Property(p => p.OwnerId).IsRequired();
        builder.Property(p => p.FileName).HasMaxLength(Photo.FileNameMaxLength).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(Photo.ContentTypeMaxLength).IsRequired();
        builder.Property(p => p.SizeBytes).IsRequired();
        builder.Property(p => p.OriginalKey).HasMaxLength(Photo.KeyMaxLength).IsRequired();
        builder.Property(p => p.ThumbnailKey).HasMaxLength(Photo.KeyMaxLength);
        builder.Property(p => p.UploadedAt).IsRequired();

        builder.HasIndex(p => new { p.TripId, p.MarkerId });
        builder.HasIndex(p => p.OwnerId);
    }
}
