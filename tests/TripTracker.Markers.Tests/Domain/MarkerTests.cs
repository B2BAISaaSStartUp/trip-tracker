using CSharpFunctionalExtensions;
using TripTracker.Markers.Domain;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Tests.Domain;

public class MarkerTests
{
    private static readonly Guid Trip = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private static Result<Marker, Error> Create(
        string? title = "Old Town",
        string? description = null,
        MarkerCategory category = MarkerCategory.Sight,
        double latitude = 50.45,
        double longitude = 30.52) =>
        Marker.Create(Trip, User, title, description, category, latitude, longitude, Now);

    [Fact]
    public void Create_WithValidData_TrimsTextAndKeepsValues()
    {
        var result = Create(title: "  Old Town  ", description: "  Meet here  ");

        Assert.True(result.IsSuccess);
        var marker = result.Value;
        Assert.NotEqual(Guid.Empty, marker.Id);
        Assert.Equal(Trip, marker.TripId);
        Assert.Equal(User, marker.CreatedBy);
        Assert.Equal("Old Town", marker.Title.Value);
        Assert.Equal("Meet here", marker.Description);
        Assert.Equal(MarkerCategory.Sight, marker.Category);
        Assert.Equal(50.45, marker.Latitude);
        Assert.Equal(30.52, marker.Longitude);
        Assert.Equal(Now, marker.CreatedAt);
        Assert.Equal(Now, marker.UpdatedAt);
    }

    // Required by the lab instructions: a marker without a title is not created.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutTitle_IsRejected(string? title)
    {
        var result = Create(title: title);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Equal("value.is.required", result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongTitle_IsRejected()
    {
        var result = Create(title: new string('a', MarkerTitle.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("value.too.long", result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongDescription_IsRejected()
    {
        var result = Create(description: new string('a', Marker.DescriptionMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("value.too.long", result.Error.Code);
    }

    [Fact]
    public void Create_WithUnknownCategory_IsRejected()
    {
        var result = Create(category: (MarkerCategory)999);

        Assert.True(result.IsFailure);
        Assert.Equal("value.is.invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(-90.0001, 0, "marker.latitude.invalid")]
    [InlineData(90.0001, 0, "marker.latitude.invalid")]
    [InlineData(double.NaN, 0, "marker.latitude.invalid")]
    [InlineData(0, -180.0001, "marker.longitude.invalid")]
    [InlineData(0, 180.0001, "marker.longitude.invalid")]
    public void Create_WithInvalidPosition_IsRejected(double latitude, double longitude, string code)
    {
        var result = Create(latitude: latitude, longitude: longitude);

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public void Update_ReplacesFieldsAndUpdatedAt()
    {
        var marker = Create().Value;
        var later = Now.AddHours(1);

        var result = marker.Update("Cafe", null, MarkerCategory.Food, 49.84, 24.03, later);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cafe", marker.Title.Value);
        Assert.Null(marker.Description);
        Assert.Equal(MarkerCategory.Food, marker.Category);
        Assert.Equal(49.84, marker.Latitude);
        Assert.Equal(24.03, marker.Longitude);
        Assert.Equal(Now, marker.CreatedAt);
        Assert.Equal(later, marker.UpdatedAt);
    }

    [Fact]
    public void Update_WithoutTitle_IsRejectedAndChangesNothing()
    {
        var marker = Create().Value;

        var result = marker.Update(" ", null, MarkerCategory.Food, 0, 0, Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("Old Town", marker.Title.Value);
        Assert.Equal(MarkerCategory.Sight, marker.Category);
        Assert.Equal(Now, marker.UpdatedAt);
    }
}
