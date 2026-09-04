using TiaoKe.App.Core;
using TiaoKe.App.Models;
using TiaoKe.App.Services;

var tests = new (string Name, Action Run)[]
{
    ("work timer reaches reminder once", WorkTimerReachesReminder),
    ("rest can start immediately", RestCanStartImmediately),
    ("reminder pause starts a fresh cycle when resumed", ReminderPauseStartsFreshCycle),
    ("rest can start while reminders are paused", RestCanStartWhileRemindersArePaused),
    ("behavior and appearance settings persist", BehaviorAndAppearanceSettingsPersist)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

return failures == 0 ? 0 : 1;

static void WorkTimerReachesReminder()
{
    var clock = new FakeClock();
    var timer = CreateTimer(clock);
    clock.Advance(TimeSpan.FromMinutes(20));
    timer.Tick();
    Equal(TimerState.ReminderDue, timer.State);
    Equal(TimeSpan.Zero, timer.Snapshot.Remaining);
}

static void RestCanStartImmediately()
{
    var clock = new FakeClock();
    var timer = CreateTimer(clock);
    clock.Advance(TimeSpan.FromMinutes(3));
    timer.Tick();
    timer.StartRestNow();
    Equal(TimerState.Resting, timer.State);
    Equal(TimeSpan.FromSeconds(20), timer.Snapshot.Remaining);
}

static void ReminderPauseStartsFreshCycle()
{
    var clock = new FakeClock();
    var timer = CreateTimer(clock);
    timer.PauseReminders(TimeSpan.FromMinutes(15));
    clock.Advance(TimeSpan.FromMinutes(15));
    timer.Tick();
    Equal(TimerState.Working, timer.State);
    Equal(TimeSpan.FromMinutes(20), timer.Snapshot.Remaining);
}

static void RestCanStartWhileRemindersArePaused()
{
    var clock = new FakeClock();
    var timer = CreateTimer(clock);
    timer.PauseReminders(TimeSpan.FromHours(1));
    Equal(TimerState.Paused, timer.State);
    timer.StartRestNow();
    Equal(TimerState.Resting, timer.State);
    Equal(TimeSpan.FromSeconds(20), timer.Snapshot.Remaining);
}

static void BehaviorAndAppearanceSettingsPersist()
{
    var path = Path.Combine(Path.GetTempPath(), $"tiaoke-settings-{Guid.NewGuid():N}.json");
    try
    {
        var store = new SettingsStore(path);
        store.Save(new AppSettings
        {
            ReminderCorner = "topRight",
            DisplayTarget = "primary",
            SoundEnabled = true,
            Theme = "dark",
            CompactReminder = true
        });

        var loaded = store.Load();
        Equal("topRight", loaded.ReminderCorner);
        Equal("primary", loaded.DisplayTarget);
        Equal(true, loaded.SoundEnabled);
        Equal("dark", loaded.Theme);
        Equal(true, loaded.CompactReminder);
    }
    finally
    {
        File.Delete(path);
    }
}

static BreakTimer CreateTimer(FakeClock clock) =>
    new(TimeSpan.FromMinutes(20), TimeSpan.FromSeconds(20), clock);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = new(2026, 9, 4, 0, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan elapsed) => UtcNow += elapsed;
}
