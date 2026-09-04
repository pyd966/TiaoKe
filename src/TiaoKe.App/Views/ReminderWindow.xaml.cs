using System.Windows;
using TiaoKe.App.Core;

namespace TiaoKe.App.Views;

public partial class ReminderWindow : Window
{
    private readonly Action _startRest;
    private readonly Action _endRest;

    public ReminderWindow(Action startRest, Action endRest)
    {
        InitializeComponent();
        _startRest = startRest;
        _endRest = endRest;
        Loaded += (_, _) => PlaceAtBottomLeft();
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
            PlaceAtBottomLeft();
        }
        else
        {
            Hide();
        }
    }

    private void StartRest_Click(object sender, RoutedEventArgs e) => _startRest();

    private void EndRest_Click(object sender, RoutedEventArgs e) => _endRest();

    private void PlaceAtBottomLeft()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + 16;
        Top = workArea.Bottom - ActualHeight - 16;
    }
}
