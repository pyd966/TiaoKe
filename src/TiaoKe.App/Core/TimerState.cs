namespace TiaoKe.App.Core;

public enum TimerState
{
    Working,
    ReminderDue,
    Resting,
    Paused
}

public sealed record TimerSnapshot(
    TimerState State,
    TimeSpan Remaining,
    TimeSpan Total,
    DateTimeOffset? PausedUntil)
{
    public bool IsRestVisible => State == TimerState.Resting;
}
