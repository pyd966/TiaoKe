namespace TiaoKe.App.Models;

public enum SchedulePreset
{
    Standard,
    Frequent,
    Active,
    Manual
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;
    public int WorkMinutes { get; set; } = 20;
    public int RestSeconds { get; set; } = 20;
    public SchedulePreset SchedulePreset { get; set; } = global::TiaoKe.App.Models.SchedulePreset.Standard;
    public string ReminderCorner { get; set; } = "bottomLeft";
    public string DisplayTarget { get; set; } = "active";
    public bool LaunchAtLogin { get; set; }
    public string Theme { get; set; } = "system";
    public bool SoundEnabled { get; set; }
    public bool CompactReminder { get; set; }
}
