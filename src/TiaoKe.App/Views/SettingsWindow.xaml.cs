using System.Windows;
using TiaoKe.App.Models;

namespace TiaoKe.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _save;
    private readonly Action _restNow;

    public SettingsWindow(AppSettings settings, Action<AppSettings> save, Action restNow)
    {
        InitializeComponent();
        _settings = settings;
        _save = save;
        _restNow = restNow;
        WorkMinutesTextBox.Text = settings.WorkMinutes.ToString();
        RestSecondsTextBox.Text = settings.RestSeconds.ToString();
        LaunchAtLoginCheckBox.IsChecked = settings.LaunchAtLogin;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WorkMinutesTextBox.Text, out var workMinutes) || workMinutes is < 1 or > 180)
        {
            ShowValidation("工作时长应为 1 到 180 分钟。");
            return;
        }

        if (!int.TryParse(RestSecondsTextBox.Text, out var restSeconds) || restSeconds is < 5 or > 300)
        {
            ShowValidation("休息时长应为 5 到 300 秒。");
            return;
        }

        _settings.WorkMinutes = workMinutes;
        _settings.RestSeconds = restSeconds;
        _settings.SchedulePreset = SchedulePreset.Manual;
        _settings.LaunchAtLogin = LaunchAtLoginCheckBox.IsChecked == true;
        _save(_settings);
        Close();
    }

    private void RestNow_Click(object sender, RoutedEventArgs e)
    {
        _restNow();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
    }
}
