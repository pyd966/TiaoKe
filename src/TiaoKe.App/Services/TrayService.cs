using System.Drawing;
using System.Windows.Forms;
using TiaoKe.App.Core;

namespace TiaoKe.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _restNowItem;
    private TimerSnapshot _snapshot;

    public TrayService(
        TimerSnapshot initialSnapshot,
        Action startRestNow,
        Action reset,
        Action<TimeSpan> pauseReminders,
        Action showSettings,
        Action exit)
    {
        _snapshot = initialSnapshot;
        _statusItem = new ToolStripMenuItem { Enabled = false };
        _restNowItem = new ToolStripMenuItem("立即休息", null, (_, _) => startRestNow());

        var pauseReminderMenu = new ToolStripMenuItem("暂停休息提醒");
        pauseReminderMenu.DropDownItems.Add("15 分钟", null, (_, _) => pauseReminders(TimeSpan.FromMinutes(15)));
        pauseReminderMenu.DropDownItems.Add("30 分钟", null, (_, _) => pauseReminders(TimeSpan.FromMinutes(30)));
        pauseReminderMenu.DropDownItems.Add("1 小时", null, (_, _) => pauseReminders(TimeSpan.FromHours(1)));
        pauseReminderMenu.DropDownItems.Add("2 小时", null, (_, _) => pauseReminders(TimeSpan.FromHours(2)));
        pauseReminderMenu.DropDownItems.Add("今天不再提醒", null, (_, _) =>
        {
            var untilTomorrow = DateTime.Today.AddDays(1) - DateTime.Now;
            pauseReminders(untilTomorrow > TimeSpan.Zero ? untilTomorrow : TimeSpan.FromDays(1));
        });

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_restNowItem);
        menu.Items.Add("重置本轮计时", null, (_, _) => reset());
        menu.Items.Add(pauseReminderMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置…", null, (_, _) => showSettings());
        menu.Items.Add("退出眺刻", null, (_, _) => exit());
        menu.Opening += (_, _) => RefreshMenu();

        _notifyIcon = new NotifyIcon
        {
            Text = "眺刻",
            Icon = SystemIcons.Information,
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) showSettings();
        };

        RefreshMenu();
    }

    public void Update(TimerSnapshot snapshot)
    {
        _snapshot = snapshot;
        var text = FormatStatus(snapshot);
        _notifyIcon.Text = text.Length <= 63 ? text : "眺刻";
    }

    private void RefreshMenu()
    {
        _statusItem.Text = FormatStatus(_snapshot);
        _restNowItem.Visible = _snapshot.State is TimerState.Working or TimerState.ReminderDue or TimerState.Paused;
    }

    private static string FormatStatus(TimerSnapshot snapshot)
    {
        var remaining = snapshot.Remaining < TimeSpan.Zero ? TimeSpan.Zero : snapshot.Remaining;
        var time = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        return snapshot.State switch
        {
            TimerState.Working => $"眺刻 · 距下次远眺 {time}",
            TimerState.ReminderDue => "眺刻 · 该看看远处了",
            TimerState.Resting => $"眺刻 · 休息中 {time}",
            TimerState.Paused => $"眺刻 · 已暂停至 {snapshot.PausedUntil?.ToLocalTime():HH:mm}",
            _ => "眺刻"
        };
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
