using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Trips.Domain;

/// <summary>Validated, trimmed trip title (1 to 200 characters).</summary>
internal sealed class TripTitle : StringValueObject
{
    /// <summary>Longest title accepted.</summary>
    public const int MaxLength = 200;

    private TripTitle(string value)
        : base(value)
    {
    }

    /// <summary>Validates <paramref name="value"/> and creates a title.</summary>
    public static Result<TripTitle, Error> Create(string? value) =>
        Create(value!, 1, MaxLength, v => new TripTitle(v));

    /// <summary>Rebuilds a title from storage, where it was validated before it was saved.</summary>
    public static TripTitle FromStorage(string value) => new(value);
}
