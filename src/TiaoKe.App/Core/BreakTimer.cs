namespace TiaoKe.App.Core;

public sealed class BreakTimer
{
    private readonly IClock _clock;
    private TimeSpan _workDuration;
    private TimeSpan _restDuration;
    private DateTimeOffset _lastUpdate;
    private TimerState _state;
    private TimeSpan _remaining;
    private DateTimeOffset? _pausedUntil;

    public BreakTimer(TimeSpan workDuration, TimeSpan restDuration, IClock? clock = null)
    {
        if (workDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(workDuration));
        if (restDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(restDuration));

        _clock = clock ?? new SystemClock();
        _workDuration = workDuration;
        _restDuration = restDuration;
        _state = TimerState.Working;
        _remaining = workDuration;
        _lastUpdate = _clock.UtcNow;
    }

    public event EventHandler<TimerSnapshot>? Changed;

    public TimerSnapshot Snapshot => new(_state, _remaining, CurrentTotal, _pausedUntil);

    public TimerState State => _state;

    public void SetDurations(TimeSpan workDuration, TimeSpan restDuration)
    {
        if (workDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(workDuration));
        if (restDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(restDuration));

        Tick();
        _workDuration = workDuration;
        _restDuration = restDuration;
        Reset();
    }

    public void Tick()
    {
        var now = _clock.UtcNow;
        var elapsed = now - _lastUpdate;
        _lastUpdate = now;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        switch (_state)
        {
            case TimerState.Working:
                AdvanceWork(elapsed);
                break;
            case TimerState.Resting:
                AdvanceRest(elapsed);
                break;
            case TimerState.Paused when _pausedUntil is not null && now >= _pausedUntil:
                _pausedUntil = null;
                _state = TimerState.Working;
                _remaining = _workDuration;
                RaiseChanged();
                break;
        }
    }

    public void StartRestNow()
    {
        if (_state == TimerState.Resting) return;
        _pausedUntil = null;
        _state = TimerState.Resting;
        _remaining = _restDuration;
        _lastUpdate = _clock.UtcNow;
        RaiseChanged();
    }

    public void StartRestFromReminder()
    {
        if (_state == TimerState.ReminderDue) StartRestNow();
    }

    public void EndRest()
    {
        if (_state != TimerState.Resting) return;
        StartWorking();
    }

    public void PauseReminders(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (_state == TimerState.Resting) return;

        _pausedUntil = _clock.UtcNow + duration;
        _state = TimerState.Paused;
        _remaining = TimeSpan.Zero;
        _lastUpdate = _clock.UtcNow;
        RaiseChanged();
    }

    public void ResumeReminders()
    {
        if (_state != TimerState.Paused) return;
        StartWorking();
    }

    public void Reset()
    {
        _pausedUntil = null;
        StartWorking();
    }

    private void AdvanceWork(TimeSpan elapsed)
    {
        _remaining -= elapsed;
        if (_remaining > TimeSpan.Zero)
        {
            RaiseChanged();
            return;
        }

        _remaining = TimeSpan.Zero;
        _state = TimerState.ReminderDue;
        RaiseChanged();
    }

    private void AdvanceRest(TimeSpan elapsed)
    {
        _remaining -= elapsed;
        if (_remaining > TimeSpan.Zero)
        {
            RaiseChanged();
            return;
        }

        StartWorking();
    }

    private void StartWorking()
    {
        _pausedUntil = null;
        _state = TimerState.Working;
        _remaining = _workDuration;
        _lastUpdate = _clock.UtcNow;
        RaiseChanged();
    }

    private TimeSpan CurrentTotal => _state == TimerState.Resting
        ? _restDuration
        : _workDuration;

    private void RaiseChanged() => Changed?.Invoke(this, Snapshot);
}
