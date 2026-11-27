namespace TripTracker.Markers.Tests.Fakes;

/// <summary>A clock the test moves by hand.</summary>
internal sealed class FakeClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
