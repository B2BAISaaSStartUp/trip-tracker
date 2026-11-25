using Microsoft.EntityFrameworkCore;
using TripTracker.Photos.Domain;

namespace TripTracker.Photos.Persistence;

/// <summary>EF Core context for the "photos" schema.</summary>
internal sealed class PhotosDbContext : DbContext
{
    public const string SchemaName = "photos";

    public PhotosDbContext(DbContextOptions<PhotosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Photo> Photos => Set<Photo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new PhotoConfiguration());
    }
}
