using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;

namespace TripTracker.Trips.Domain;

/// <summary>A trip owned by one user. Always created through <see cref="Create"/>.</summary>
internal sealed class Trip
{
    /// <summary>Longest description accepted.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Name of the field that stores the title. Persistence maps it to a column so searches can run in SQL.</summary>
    internal const string TitleFieldName = nameof(_title);

    private string _title;

    // Required by EF Core.
    private Trip()
    {
        _title = null!;
        ShareToken = null!;
    }

    private Trip(
        Guid ownerId,
        TripTitle title,
        string? description,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        _title = title.Value;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        AccessLevel = TripAccessLevel.Private;
        ShareToken = ShareTokenGenerator.Generate();
        IsHidden = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public TripTitle Title => TripTitle.FromStorage(_title);

    public string? Description { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public TripAccessLevel AccessLevel { get; private set; }

    /// <summary>Secret that grants read access when <see cref="AccessLevel"/> is not Private. Stable for the life of the trip.</summary>
    public string ShareToken { get; private set; }

    /// <summary>Set by moderation; a hidden trip is readable only by its owner.</summary>
    public bool IsHidden { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates a private trip, or returns the first validation error.</summary>
    public static Result<Trip, Error> Create(
        Guid ownerId,
        string? title,
        string? description,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("OwnerId");
        }

        var validated = Validate(title, description, startDate, endDate);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        var (validTitle, validDescription) = validated.Value;
        return new Trip(ownerId, validTitle, validDescription, startDate, endDate, now);
    }

    /// <summary>Changes title, description and dates.</summary>
    public UnitResult<Error> Update(
        string? title,
        string? description,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset now)
    {
        var validated = Validate(title, description, startDate, endDate);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        _title = validated.Value.Title.Value;
        Description = validated.Value.Description;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = now;
        return UnitResult.Success<Error>();
    }

    /// <summary>Changes who may see the trip. The share token is kept.</summary>
    public void ChangeAccessLevel(TripAccessLevel level, DateTimeOffset now)
    {
        AccessLevel = level;
        UpdatedAt = now;
    }

    /// <summary>Hides a public trip from everyone but its owner. Hiding an already hidden trip succeeds.</summary>
    public UnitResult<Error> Hide(DateTimeOffset now)
    {
        if (AccessLevel != TripAccessLevel.Public)
        {
            return TripErrors.OnlyPublicCanBeHidden();
        }

        if (!IsHidden)
        {
            IsHidden = true;
            UpdatedAt = now;
        }

        return UnitResult.Success<Error>();
    }

    /// <summary>Makes a hidden trip visible again. Restoring a visible trip does nothing.</summary>
    public void Restore(DateTimeOffset now)
    {
        if (IsHidden)
        {
            IsHidden = false;
            UpdatedAt = now;
        }
    }

    private static Result<(TripTitle Title, string? Description), Error> Validate(
        string? title,
        string? description,
        DateOnly startDate,
        DateOnly endDate)
    {
        var titleResult = TripTitle.Create(title);
        if (titleResult.IsFailure)
        {
            return titleResult.Error;
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription?.Length > DescriptionMaxLength)
        {
            return Errors.Validation.ValueTooLong("Description", DescriptionMaxLength);
        }

        if (endDate < startDate)
        {
            return TripErrors.InvalidDates();
        }

        return (titleResult.Value, trimmedDescription);
    }
}
