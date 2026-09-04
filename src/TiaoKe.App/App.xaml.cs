using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
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
    private NotificationSound? _notificationSound;
    private DispatcherTimer? _dispatcherTimer;
    private TimerState _lastTimerState = TimerState.Working;

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
        ApplyTheme(_settings.Theme);
        _notificationSound = new NotificationSound();
        _timer = new BreakTimer(
            TimeSpan.FromMinutes(_settings.WorkMinutes),
            TimeSpan.FromSeconds(_settings.RestSeconds));

        _reminderWindow = new ReminderWindow(
            () => _timer.StartRestFromReminder(),
            () => _timer.EndRest(),
            () => _timer.Reset(),
            duration => _timer.PauseReminders(duration),
            ShowSettings);
        _reminderWindow.ApplySettings(_settings);

        _trayService = new TrayService(
            _timer.Snapshot,
            () => Dispatcher.Invoke(() => _timer.StartRestNow()),
            () => Dispatcher.Invoke(() => _timer.Reset()),
            duration => Dispatcher.Invoke(() => _timer.PauseReminders(duration)),
            () => Dispatcher.Invoke(ShowSettings),
            () => Dispatcher.Invoke(ExitApplication),
            IsDarkTheme(_settings.Theme));

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
        if (_settings.SoundEnabled && snapshot.State == TimerState.ReminderDue &&
            _lastTimerState != TimerState.ReminderDue)
        {
            _notificationSound?.PlayReminder();
        }
        else if (_settings.SoundEnabled && snapshot.State == TimerState.Working &&
                 _lastTimerState == TimerState.Resting)
        {
            _notificationSound?.PlayRestComplete();
        }

        _lastTimerState = snapshot.State;
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
        ApplyTheme(settings.Theme);
        _trayService?.ApplyTheme(IsDarkTheme(settings.Theme));
        _reminderWindow?.ApplySettings(settings);
        _timer?.SetDurations(
            TimeSpan.FromMinutes(settings.WorkMinutes),
            TimeSpan.FromSeconds(settings.RestSeconds));
    }

    public void ApplyTheme(string theme)
    {
        var useDarkTheme = IsDarkTheme(theme);
        var palette = useDarkTheme
            ? new Dictionary<string, string>
            {
                ["PageBackgroundBrush"] = "#171A18",
                ["SurfaceBrush"] = "#222624",
                ["InputBackgroundBrush"] = "#1C201E",
                ["TextBrush"] = "#F1F4F2",
                ["MutedTextBrush"] = "#AAB3AE",
                ["BrandBrush"] = "#70B99B",
                ["BrandHoverBrush"] = "#82C7AA",
                ["BrandPressedBrush"] = "#5EA88A",
                ["BrandForegroundBrush"] = "#13231C",
                ["BrandTintBrush"] = "#2B4439",
                ["WarningBrush"] = "#E9B84A",
                ["BorderBrush"] = "#404742",
                ["DividerBrush"] = "#303632",
                ["HoverBrush"] = "#303632",
                ["DangerBrush"] = "#EE8279"
            }
            : new Dictionary<string, string>
            {
                ["PageBackgroundBrush"] = "#F6F7F5",
                ["SurfaceBrush"] = "#FFFFFF",
                ["InputBackgroundBrush"] = "#FFFFFF",
                ["TextBrush"] = "#202522",
                ["MutedTextBrush"] = "#68716C",
                ["BrandBrush"] = "#2F765E",
                ["BrandHoverBrush"] = "#28674F",
                ["BrandPressedBrush"] = "#225843",
                ["BrandForegroundBrush"] = "#FFFFFF",
                ["BrandTintBrush"] = "#E7F1ED",
                ["WarningBrush"] = "#D99A20",
                ["BorderBrush"] = "#D7DDDA",
                ["DividerBrush"] = "#EDF0EE",
                ["HoverBrush"] = "#F0F3F1",
                ["DangerBrush"] = "#B7473E"
            };

        foreach (var (key, value) in palette)
        {
            Resources[key] = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value));
        }
    }

    public static bool IsDarkTheme(string theme)
        => theme == "dark" || (theme == "system" && SystemUsesDarkTheme());

    private static bool SystemUsesDarkTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);
            return value is int useLightTheme && useLightTheme == 0;
        }
        catch
        {
            return false;
        }
    }

    private void ExitApplication()
    {
        _dispatcherTimer?.Stop();
        _trayService?.Dispose();
        _trayService = null;
        _notificationSound?.Dispose();
        _notificationSound = null;
        _reminderWindow?.Close();
        _settingsWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        _notificationSound?.Dispose();
        _notificationSound = null;
        _activationRegistration?.Unregister(null);
        _activationEvent?.Dispose();
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
