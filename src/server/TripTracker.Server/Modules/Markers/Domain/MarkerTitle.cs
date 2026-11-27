using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Domain;

/// <summary>Validated, trimmed marker title (1 to 200 characters).</summary>
internal sealed class MarkerTitle : StringValueObject
{
    /// <summary>Longest title accepted.</summary>
    public const int MaxLength = 200;

    private MarkerTitle(string value)
        : base(value)
    {
    }

    /// <summary>Validates <paramref name="value"/> and creates a title.</summary>
    public static Result<MarkerTitle, Error> Create(string? value) =>
        Create(value!, 1, MaxLength, v => new MarkerTitle(v));

    /// <summary>Rebuilds a title from storage, where it was validated before it was saved.</summary>
    public static MarkerTitle FromStorage(string value) => new(value);
}
