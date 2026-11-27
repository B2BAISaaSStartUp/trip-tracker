using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Domain;

/// <summary>A place marked on a trip's map. Always created through <see cref="Create"/>.</summary>
internal sealed class Marker
{
    /// <summary>Longest description accepted.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>Name of the field that stores the title. Persistence maps it to a column.</summary>
    internal const string TitleFieldName = nameof(_title);

    private string _title;

    // Required by EF Core.
    private Marker()
    {
        _title = null!;
    }

    private Marker(Guid tripId, Guid createdBy, Fields fields, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        TripId = tripId;
        CreatedBy = createdBy;
        _title = fields.Title.Value;
        Apply(fields);
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    /// <summary>The user who put the marker on the map.</summary>
    public Guid CreatedBy { get; private set; }

    public MarkerTitle Title => MarkerTitle.FromStorage(_title);

    public string? Description { get; private set; }

    public MarkerCategory Category { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates a marker, or returns the first validation error.</summary>
    public static Result<Marker, Error> Create(
        Guid tripId,
        Guid createdBy,
        string? title,
        string? description,
        MarkerCategory category,
        double latitude,
        double longitude,
        DateTimeOffset now)
    {
        if (tripId == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("TripId");
        }

        if (createdBy == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("CreatedBy");
        }

        var validated = Validate(title, description, category, latitude, longitude);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        return new Marker(tripId, createdBy, validated.Value, now);
    }

    /// <summary>Replaces title, description, category and position.</summary>
    public UnitResult<Error> Update(
        string? title,
        string? description,
        MarkerCategory category,
        double latitude,
        double longitude,
        DateTimeOffset now)
    {
        var validated = Validate(title, description, category, latitude, longitude);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        Apply(validated.Value);
        UpdatedAt = now;
        return UnitResult.Success<Error>();
    }

    private void Apply(Fields fields)
    {
        _title = fields.Title.Value;
        Description = fields.Description;
        Category = fields.Category;
        Latitude = fields.Latitude;
        Longitude = fields.Longitude;
    }

    private static Result<Fields, Error> Validate(
        string? title,
        string? description,
        MarkerCategory category,
        double latitude,
        double longitude)
    {
        var titleResult = MarkerTitle.Create(title);
        if (titleResult.IsFailure)
        {
            return titleResult.Error;
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > DescriptionMaxLength)
        {
            return Errors.Validation.ValueTooLong("Description", DescriptionMaxLength);
        }

        if (!Enum.IsDefined(category))
        {
            return Errors.Validation.ValueIsInvalid("Category", "unknown category");
        }

        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            return MarkerErrors.InvalidLatitude();
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            return MarkerErrors.InvalidLongitude();
        }

        return new Fields(titleResult.Value, trimmedDescription, category, latitude, longitude);
    }

    /// <summary>Values that passed validation together.</summary>
    private sealed record Fields(
        MarkerTitle Title,
        string? Description,
        MarkerCategory Category,
        double Latitude,
        double Longitude);
}
