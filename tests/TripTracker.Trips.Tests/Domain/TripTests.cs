using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Tests.Domain;

public class TripTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_DefaultsToPrivateAndVisible()
    {
        var result = Trip.Create(Owner, "  Alps  ", "  Hiking  ", Start, Start.AddDays(5), Now);

        Assert.True(result.IsSuccess);
        var trip = result.Value;
        Assert.Equal(TripAccessLevel.Private, trip.AccessLevel);
        Assert.False(trip.IsHidden);
        Assert.Equal("Alps", trip.Title.Value);
        Assert.Equal("Hiking", trip.Description);
        Assert.Equal(Owner, trip.OwnerId);
        Assert.NotEqual(Guid.Empty, trip.Id);
        Assert.False(string.IsNullOrEmpty(trip.ShareToken));
        Assert.Equal(Now, trip.CreatedAt);
        Assert.Equal(Now, trip.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_IsRejected(string? title)
    {
        var result = Trip.Create(Owner, title, null, Start, Start, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Equal("value.is.required", result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongTitle_IsRejected()
    {
        var result = Trip.Create(Owner, new string('a', TripTitle.MaxLength + 1), null, Start, Start, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("value.too.long", result.Error.Code);
    }

    [Fact]
    public void Create_WithEndBeforeStart_IsRejected()
    {
        var result = Trip.Create(Owner, "Alps", null, Start, Start.AddDays(-1), Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Equal("trip.dates.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_WithSameStartAndEnd_IsAllowed()
    {
        Assert.True(Trip.Create(Owner, "Day trip", null, Start, Start, Now).IsSuccess);
    }

    [Fact]
    public void Create_WithTooLongDescription_IsRejected()
    {
        var result = Trip.Create(Owner, "Alps", new string('a', Trip.DescriptionMaxLength + 1), Start, Start, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("value.too.long", result.Error.Code);
    }

    [Fact]
    public void Create_WithEmptyOwner_IsRejected()
    {
        Assert.True(Trip.Create(Guid.Empty, "Alps", null, Start, Start, Now).IsFailure);
    }

    [Fact]
    public void Update_WithInvalidDates_LeavesTripUnchanged()
    {
        var trip = Trip.Create(Owner, "Alps", null, Start, Start.AddDays(5), Now).Value;

        var result = trip.Update("Renamed", null, Start, Start.AddDays(-3), Now.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal("Alps", trip.Title.Value);
        Assert.Equal(Now, trip.UpdatedAt);
    }

    [Fact]
    public void ChangeAccessLevel_KeepsShareToken()
    {
        var trip = Trip.Create(Owner, "Alps", null, Start, Start, Now).Value;
        var token = trip.ShareToken;

        trip.ChangeAccessLevel(TripAccessLevel.ByLink, Now);
        trip.ChangeAccessLevel(TripAccessLevel.Private, Now);
        trip.ChangeAccessLevel(TripAccessLevel.ByLink, Now);

        Assert.Equal(token, trip.ShareToken);
    }

    [Theory]
    [InlineData(TripAccessLevel.Private)]
    [InlineData(TripAccessLevel.ByLink)]
    public void Hide_OnNonPublicTrip_IsConflict(TripAccessLevel level)
    {
        var trip = Trip.Create(Owner, "Alps", null, Start, Start, Now).Value;
        trip.ChangeAccessLevel(level, Now);

        var result = trip.Hide(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.CONFLICT, result.Error.Type);
        Assert.False(trip.IsHidden);
    }

    [Fact]
    public void Hide_ThenRestore_TogglesFlag()
    {
        var trip = Trip.Create(Owner, "Alps", null, Start, Start, Now).Value;
        trip.ChangeAccessLevel(TripAccessLevel.Public, Now);

        Assert.True(trip.Hide(Now).IsSuccess);
        Assert.True(trip.IsHidden);
        Assert.True(trip.Hide(Now).IsSuccess);

        trip.Restore(Now);
        Assert.False(trip.IsHidden);
        trip.Restore(Now);
        Assert.False(trip.IsHidden);
    }

    [Fact]
    public void ShareTokens_AreUniqueAndUrlSafe()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => ShareTokenGenerator.Generate()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
        Assert.All(tokens, t => Assert.Matches("^[A-Za-z0-9_-]+$", t));
    }
}
