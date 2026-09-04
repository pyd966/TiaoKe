using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TiaoKe.App.Core;
using TiaoKe.App.Models;
using TiaoKe.App.Services;
using TiaoKe.App.Views;

namespace TiaoKe.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Local\\TiaoKe.SingleInstance";
    private const string ActivationEventName = "Local\\TiaoKe.Activate";

    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _activationRegistration;
    private SettingsStore? _settingsStore;
    private AutostartService? _autostartService;
    private AppSettings? _settings;
    private BreakTimer? _timer;
    private ReminderWindow? _reminderWindow;
    private SettingsWindow? _settingsWindow;
    private TrayService? _trayService;
    private DispatcherTimer? _dispatcherTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        _ownsMutex = isFirstInstance;
        if (!isFirstInstance)
        {
            _activationEvent.Set();
            Shutdown();
            return;
        }

        _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => Dispatcher.BeginInvoke(ShowSettings),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        _settingsStore = new SettingsStore();
        _autostartService = new AutostartService();
        _settings = _settingsStore.Load();
        _settings.LaunchAtLogin = _autostartService.IsEnabled();
        _timer = new BreakTimer(
            TimeSpan.FromMinutes(_settings.WorkMinutes),
            TimeSpan.FromSeconds(_settings.RestSeconds));

        _reminderWindow = new ReminderWindow(
            () => _timer.StartRestFromReminder(),
            () => _timer.EndRest());

        _trayService = new TrayService(
            _timer.Snapshot,
            () => Dispatcher.Invoke(() => _timer.StartRestNow()),
            () => Dispatcher.Invoke(() => _timer.Reset()),
            duration => Dispatcher.Invoke(() => _timer.PauseReminders(duration)),
            () => Dispatcher.Invoke(ShowSettings),
            () => Dispatcher.Invoke(ExitApplication));

        _timer.Changed += Timer_Changed;
        _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _dispatcherTimer.Tick += (_, _) => _timer.Tick();
        _dispatcherTimer.Start();
    }

    private void Timer_Changed(object? sender, TimerSnapshot snapshot)
    {
        if (_settings is null) return;
        _trayService?.Update(snapshot);
        _reminderWindow?.Render(snapshot, _settings.WorkMinutes);
    }

    private void ShowSettings()
    {
        if (_settings is null) return;
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, SaveSettings, () => _timer?.StartRestNow());
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        _settingsStore?.Save(settings);
        _autostartService?.SetEnabled(settings.LaunchAtLogin);
        _timer?.SetDurations(
            TimeSpan.FromMinutes(settings.WorkMinutes),
            TimeSpan.FromSeconds(settings.RestSeconds));
    }

    private void ExitApplication()
    {
        _dispatcherTimer?.Stop();
        _trayService?.Dispose();
        _trayService = null;
        _reminderWindow?.Close();
        _settingsWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        _activationRegistration?.Unregister(null);
        _activationEvent?.Dispose();
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
