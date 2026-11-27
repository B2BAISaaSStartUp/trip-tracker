using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Tracking.Domain;

/// <summary>One GPS point of a trip's route. Always created through <see cref="Create"/>; never changed afterwards.</summary>
internal sealed class TrackPoint
{
    /// <summary>How far ahead of the server clock a device clock may be before a point is rejected.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    // Required by EF Core.
    private TrackPoint()
    {
    }

    private TrackPoint(
        Guid tripId,
        Guid recordedBy,
        Guid clientPointId,
        double latitude,
        double longitude,
        double? altitude,
        double? accuracy,
        DateTimeOffset recordedAt,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        TripId = tripId;
        RecordedBy = recordedBy;
        ClientPointId = clientPointId;
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Accuracy = accuracy;
        RecordedAt = recordedAt;
        ReceivedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    /// <summary>The user who uploaded the point.</summary>
    public Guid RecordedBy { get; private set; }

    /// <summary>Id given by the app; unique within a trip, which is what makes uploads idempotent (NFR-08).</summary>
    public Guid ClientPointId { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public double? Altitude { get; private set; }

    public double? Accuracy { get; private set; }

    /// <summary>When the device recorded the point.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>When the server stored the point.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Creates a point, or returns the first validation error.</summary>
    public static Result<TrackPoint, Error> Create(
        Guid tripId,
        Guid recordedBy,
        Guid clientPointId,
        double latitude,
        double longitude,
        double? altitude,
        double? accuracy,
        DateTimeOffset recordedAt,
        DateTimeOffset now)
    {
        if (tripId == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("TripId");
        }

        if (recordedBy == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("RecordedBy");
        }

        if (clientPointId == Guid.Empty)
        {
            return Errors.Validation.ValueIsRequired("ClientPointId");
        }

        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            return TrackingErrors.InvalidLatitude();
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            return TrackingErrors.InvalidLongitude();
        }

        if (altitude is { } alt && !double.IsFinite(alt))
        {
            return Errors.Validation.ValueIsInvalid("Altitude", "must be a finite number");
        }

        if (accuracy is { } acc && (!double.IsFinite(acc) || acc < 0))
        {
            return Errors.Validation.ValueIsInvalid("Accuracy", "must be a finite number that is not negative");
        }

        if (recordedAt > now + MaxClockSkew)
        {
            return TrackingErrors.RecordedInFuture();
        }

        return new TrackPoint(
            tripId,
            recordedBy,
            clientPointId,
            latitude,
            longitude,
            altitude,
            accuracy,
            recordedAt,
            now);
    }
}
