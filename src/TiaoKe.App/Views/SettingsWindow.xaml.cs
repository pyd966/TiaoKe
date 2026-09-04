using System.Windows;
using System.Windows.Controls;
using TiaoKe.App.Models;
using TiaoKe.App.Services;

namespace TiaoKe.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _save;
    private readonly Action _restNow;
    private readonly NotificationSound _notificationSound = new();
    private readonly string _originalTheme;
    private bool _isInitializing = true;
    private bool _isUpdatingPreset;
    private bool _saved;

    public SettingsWindow(AppSettings settings, Action<AppSettings> save, Action restNow)
    {
        InitializeComponent();
        _settings = settings;
        _save = save;
        _restNow = restNow;
        _originalTheme = settings.Theme;

        WorkMinutesTextBox.Text = settings.WorkMinutes.ToString();
        RestSecondsTextBox.Text = settings.RestSeconds.ToString();
        SelectPreset(settings.SchedulePreset);

        LaunchAtLoginToggle.IsChecked = settings.LaunchAtLogin;
        SoundEnabledToggle.IsChecked = settings.SoundEnabled;
        SelectReminderCorner(settings.ReminderCorner);
        ActiveDisplayRadio.IsChecked = settings.DisplayTarget != "primary";
        PrimaryDisplayRadio.IsChecked = settings.DisplayTarget == "primary";

        SystemThemeRadio.IsChecked = settings.Theme == "system";
        LightThemeRadio.IsChecked = settings.Theme == "light";
        DarkThemeRadio.IsChecked = settings.Theme == "dark";
        CompactReminderToggle.IsChecked = settings.CompactReminder;

        _isInitializing = false;
        UpdateReminderPreview();
        Closed += SettingsWindow_Closed;
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
        _settings.SchedulePreset = SelectedPreset();
        _settings.LaunchAtLogin = LaunchAtLoginToggle.IsChecked == true;
        _settings.SoundEnabled = SoundEnabledToggle.IsChecked == true;
        _settings.ReminderCorner = SelectedReminderCorner();
        _settings.DisplayTarget = PrimaryDisplayRadio.IsChecked == true ? "primary" : "active";
        _settings.Theme = SelectedTheme();
        _settings.CompactReminder = CompactReminderToggle.IsChecked == true;

        _saved = true;
        _save(_settings);
        Close();
    }

    private void RestNow_Click(object sender, RoutedEventArgs e)
    {
        _restNow();
        Close();
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        _isUpdatingPreset = true;
        WorkMinutesTextBox.Text = "20";
        RestSecondsTextBox.Text = "20";
        StandardPresetRadio.IsChecked = true;
        _isUpdatingPreset = false;

        LaunchAtLoginToggle.IsChecked = false;
        SoundEnabledToggle.IsChecked = false;
        BottomLeftCornerRadio.IsChecked = true;
        ActiveDisplayRadio.IsChecked = true;
        SystemThemeRadio.IsChecked = true;
        CompactReminderToggle.IsChecked = false;
        ValidationText.Visibility = Visibility.Collapsed;
    }

    private void Preset_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || _isUpdatingPreset || sender == ManualPresetRadio) return;

        _isUpdatingPreset = true;
        if (sender == StandardPresetRadio)
        {
            WorkMinutesTextBox.Text = "20";
            RestSecondsTextBox.Text = "20";
        }
        else if (sender == FrequentPresetRadio)
        {
            WorkMinutesTextBox.Text = "10";
            RestSecondsTextBox.Text = "20";
        }
        else if (sender == ActivePresetRadio)
        {
            WorkMinutesTextBox.Text = "30";
            RestSecondsTextBox.Text = "120";
        }

        _isUpdatingPreset = false;
    }

    private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing || _isUpdatingPreset) return;
        ManualPresetRadio.IsChecked = true;
        ValidationText.Visibility = Visibility.Collapsed;
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        if (System.Windows.Application.Current is App app) app.ApplyTheme(SelectedTheme());
    }

    private void CompactReminder_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing) UpdateReminderPreview();
    }

    private void SoundPreview_Click(object sender, RoutedEventArgs e) => _notificationSound.PlayReminder();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Escape) return;
        Close();
        e.Handled = true;
    }

    private void SettingsWindow_Closed(object? sender, EventArgs e)
    {
        _notificationSound.Dispose();
        if (!_saved && System.Windows.Application.Current is App app)
        {
            app.ApplyTheme(_originalTheme);
        }
    }

    private void UpdateReminderPreview()
    {
        var compact = CompactReminderToggle.IsChecked == true;
        ReminderPreview.Height = compact ? 112 : 148;
        PreviewDetailsText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SelectPreset(SchedulePreset preset)
    {
        StandardPresetRadio.IsChecked = preset == SchedulePreset.Standard;
        FrequentPresetRadio.IsChecked = preset == SchedulePreset.Frequent;
        ActivePresetRadio.IsChecked = preset == SchedulePreset.Active;
        ManualPresetRadio.IsChecked = preset == SchedulePreset.Manual;
    }

    private SchedulePreset SelectedPreset()
    {
        if (StandardPresetRadio.IsChecked == true) return SchedulePreset.Standard;
        if (FrequentPresetRadio.IsChecked == true) return SchedulePreset.Frequent;
        if (ActivePresetRadio.IsChecked == true) return SchedulePreset.Active;
        return SchedulePreset.Manual;
    }

    private void SelectReminderCorner(string corner)
    {
        TopLeftCornerRadio.IsChecked = corner == "topLeft";
        TopRightCornerRadio.IsChecked = corner == "topRight";
        BottomRightCornerRadio.IsChecked = corner == "bottomRight";
        BottomLeftCornerRadio.IsChecked = corner is not ("topLeft" or "topRight" or "bottomRight");
    }

    private string SelectedReminderCorner()
    {
        if (TopLeftCornerRadio.IsChecked == true) return "topLeft";
        if (TopRightCornerRadio.IsChecked == true) return "topRight";
        if (BottomRightCornerRadio.IsChecked == true) return "bottomRight";
        return "bottomLeft";
    }

    private string SelectedTheme()
    {
        if (DarkThemeRadio.IsChecked == true) return "dark";
        if (LightThemeRadio.IsChecked == true) return "light";
        return "system";
    }

    private void ShowValidation(string message)
    {
        ValidationText.Text = message;
        ValidationText.Visibility = Visibility.Visible;
    }
}
