namespace TripTracker.Web.Tests.Fakes;

/// <summary>A clock that moves only when told to, and then fires the one-shot timers that became due.</summary>
internal sealed class FakeClock : TimeProvider
{
    private readonly List<FakeTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Delays, from now, of the timers that are still waiting.</summary>
    public IReadOnlyList<TimeSpan> PendingDelays => _timers.Select(t => t.DueAt - _now).ToList();

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, () => callback(state), _now + dueTime);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        _now += by;
        foreach (var timer in _timers.Where(t => t.DueAt <= _now).ToList())
        {
            // A callback may dispose or replace timers, including ones still in this list.
            if (_timers.Remove(timer))
            {
                timer.Fire();
            }
        }
    }

    private sealed class FakeTimer : ITimer
    {
        private readonly FakeClock _clock;
        private readonly Action _fire;

        public FakeTimer(FakeClock clock, Action fire, DateTimeOffset dueAt)
        {
            _clock = clock;
            _fire = fire;
            DueAt = dueAt;
        }

        public DateTimeOffset DueAt { get; }

        public void Fire() => _fire();

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Dispose() => _clock._timers.Remove(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
