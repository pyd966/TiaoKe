using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TiaoKe.App.Core;
using TiaoKe.App.Models;
using Forms = System.Windows.Forms;

namespace TiaoKe.App.Views;

public partial class ReminderWindow : Window
{
    private readonly Action _startRest;
    private readonly Action _endRest;
    private readonly Action _reset;
    private readonly Action<TimeSpan> _pauseReminders;
    private readonly Action _showSettings;
    private string _reminderCorner = "bottomLeft";
    private string _displayTarget = "active";
    private bool _compactReminder;

    public ReminderWindow(
        Action startRest,
        Action endRest,
        Action reset,
        Action<TimeSpan> pauseReminders,
        Action showSettings)
    {
        InitializeComponent();
        _startRest = startRest;
        _endRest = endRest;
        _reset = reset;
        _pauseReminders = pauseReminders;
        _showSettings = showSettings;
        Loaded += (_, _) => PlaceOnScreen();
        SizeChanged += (_, _) =>
        {
            if (IsVisible) PlaceOnScreen();
        };
    }

    public void ApplySettings(AppSettings settings)
    {
        _reminderCorner = settings.ReminderCorner;
        _displayTarget = settings.DisplayTarget;
        _compactReminder = settings.CompactReminder;
        WorkSummaryText.Visibility = _compactReminder ? Visibility.Collapsed : Visibility.Visible;
        RestHintText.Visibility = _compactReminder ? Visibility.Collapsed : Visibility.Visible;

        if (IsVisible)
        {
            UpdateLayout();
            PlaceOnScreen();
        }
    }

    public void Render(TimerSnapshot snapshot, int workMinutes)
    {
        ReminderPanel.Visibility = snapshot.State == TimerState.ReminderDue
            ? Visibility.Visible
            : Visibility.Collapsed;
        RestPanel.Visibility = snapshot.IsRestVisible
            ? Visibility.Visible
            : Visibility.Collapsed;

        WorkSummaryText.Text = $"这一轮已经专注 {workMinutes} 分钟";
        if (snapshot.IsRestVisible)
        {
            var seconds = Math.Max(0, (int)Math.Ceiling(snapshot.Remaining.TotalSeconds));
            RestTimeText.Text = $"{seconds / 60:00}:{seconds % 60:00}";
            RestProgress.Maximum = Math.Max(1, snapshot.Total.TotalSeconds);
            RestProgress.Value = Math.Clamp(snapshot.Remaining.TotalSeconds, 0, RestProgress.Maximum);
        }

        if (snapshot.State is TimerState.ReminderDue or TimerState.Resting)
        {
            if (!IsVisible) Show();
            UpdateLayout();
            PlaceOnScreen();
        }
        else
        {
            Hide();
        }
    }

    private void StartRest_Click(object sender, RoutedEventArgs e) => _startRest();

    private void EndRest_Click(object sender, RoutedEventArgs e) => _endRest();

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (MoreButton.ContextMenu is null) return;
        MoreButton.ContextMenu.PlacementTarget = MoreButton;
        MoreButton.ContextMenu.Placement = PlacementMode.Bottom;
        MoreButton.ContextMenu.IsOpen = true;
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => _reset();

    private void Pause15_Click(object sender, RoutedEventArgs e) =>
        _pauseReminders(TimeSpan.FromMinutes(15));

    private void Pause30_Click(object sender, RoutedEventArgs e) =>
        _pauseReminders(TimeSpan.FromMinutes(30));

    private void Pause60_Click(object sender, RoutedEventArgs e) =>
        _pauseReminders(TimeSpan.FromHours(1));

    private void Pause120_Click(object sender, RoutedEventArgs e) =>
        _pauseReminders(TimeSpan.FromHours(2));

    private void PauseToday_Click(object sender, RoutedEventArgs e)
    {
        var untilTomorrow = DateTime.Today.AddDays(1) - DateTime.Now;
        _pauseReminders(untilTomorrow > TimeSpan.Zero ? untilTomorrow : TimeSpan.FromDays(1));
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e) => _showSettings();

    private void PlaceOnScreen()
    {
        if (!IsLoaded || ActualWidth <= 0 || ActualHeight <= 0) return;

        var screen = ResolveTargetScreen();
        var dpi = VisualTreeHelper.GetDpi(this);
        var workArea = screen.WorkingArea;
        var left = workArea.Left / dpi.DpiScaleX;
        var top = workArea.Top / dpi.DpiScaleY;
        var right = workArea.Right / dpi.DpiScaleX;
        var bottom = workArea.Bottom / dpi.DpiScaleY;
        const double gap = 16;

        Left = _reminderCorner is "topRight" or "bottomRight"
            ? right - ActualWidth - gap
            : left + gap;
        Top = _reminderCorner is "topLeft" or "topRight"
            ? top + gap
            : bottom - ActualHeight - gap;
    }

    private Forms.Screen ResolveTargetScreen()
    {
        if (_displayTarget == "primary")
        {
            return Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];
        }

        var foregroundWindow = GetForegroundWindow();
        return foregroundWindow != IntPtr.Zero
            ? Forms.Screen.FromHandle(foregroundWindow)
            : Forms.Screen.FromPoint(Forms.Cursor.Position);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
