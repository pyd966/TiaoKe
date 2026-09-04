using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TiaoKe.App.Core;

namespace TiaoKe.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly Icon _applicationIcon;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _restNowItem;
    private TimerSnapshot _snapshot;

    public TrayService(
        TimerSnapshot initialSnapshot,
        Action startRestNow,
        Action reset,
        Action<TimeSpan> pauseReminders,
        Action showSettings,
        Action exit,
        bool darkTheme)
    {
        _snapshot = initialSnapshot;
        _applicationIcon = LoadApplicationIcon();
        _statusItem = new ToolStripMenuItem
        {
            Enabled = false,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(2, 2, 2, 4)
        };
        _restNowItem = new ToolStripMenuItem("立即休息", null, (_, _) => startRestNow());

        var pauseReminderMenu = new ToolStripMenuItem("暂停休息提醒");
        pauseReminderMenu.DropDownItems.Add("暂停 15 分钟", null, (_, _) => pauseReminders(TimeSpan.FromMinutes(15)));
        pauseReminderMenu.DropDownItems.Add("暂停 30 分钟", null, (_, _) => pauseReminders(TimeSpan.FromMinutes(30)));
        pauseReminderMenu.DropDownItems.Add("暂停 1 小时", null, (_, _) => pauseReminders(TimeSpan.FromHours(1)));
        pauseReminderMenu.DropDownItems.Add("暂停 2 小时", null, (_, _) => pauseReminders(TimeSpan.FromHours(2)));
        pauseReminderMenu.DropDownItems.Add("今天不再提醒", null, (_, _) =>
        {
            var untilTomorrow = DateTime.Today.AddDays(1) - DateTime.Now;
            pauseReminders(untilTomorrow > TimeSpan.Zero ? untilTomorrow : TimeSpan.FromDays(1));
        });

        _menu = new ContextMenuStrip
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(5, 6, 5, 6),
            ShowCheckMargin = false,
            ShowImageMargin = false,
            DropShadowEnabled = true
        };
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator { Margin = new Padding(7, 3, 7, 3) });
        AddMenuItem(_restNowItem);
        AddMenuItem(new ToolStripMenuItem("重置本轮计时", null, (_, _) => reset()));
        AddMenuItem(pauseReminderMenu);
        _menu.Items.Add(new ToolStripSeparator { Margin = new Padding(7, 5, 7, 5) });
        AddMenuItem(new ToolStripMenuItem("打开设置…", null, (_, _) => showSettings()));
        AddMenuItem(new ToolStripMenuItem("退出眺刻", null, (_, _) => exit()));
        _menu.Opening += (_, _) => RefreshMenu();

        _notifyIcon = new NotifyIcon
        {
            Text = "眺刻",
            Icon = _applicationIcon,
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) showSettings();
        };

        ApplyTheme(darkTheme);
        RefreshMenu();
    }

    public void Update(TimerSnapshot snapshot)
    {
        _snapshot = snapshot;
        var text = FormatStatus(snapshot);
        _notifyIcon.Text = text.Length <= 63 ? text : "眺刻";
        if (_menu.Visible) RefreshMenu();
    }

    public void ApplyTheme(bool darkTheme)
    {
        _menu.Renderer = new ToolStripProfessionalRenderer(new TiaoKeColorTable(darkTheme));
        _menu.ForeColor = darkTheme ? Color.FromArgb(241, 244, 242) : Color.FromArgb(32, 37, 34);
        _statusItem.ForeColor = darkTheme ? Color.FromArgb(170, 179, 174) : Color.FromArgb(104, 113, 108);
    }

    private void AddMenuItem(ToolStripMenuItem item)
    {
        item.Padding = new Padding(12, 7, 12, 7);
        item.Margin = new Padding(2, 1, 2, 1);
        _menu.Items.Add(item);
    }

    private void RefreshMenu()
    {
        var status = FormatStatus(_snapshot);
        if (_statusItem.Text != status) _statusItem.Text = status;
        _restNowItem.Visible = _snapshot.State is TimerState.Working or TimerState.ReminderDue or TimerState.Paused;
    }

    private static string FormatStatus(TimerSnapshot snapshot)
    {
        var remaining = snapshot.Remaining < TimeSpan.Zero ? TimeSpan.Zero : snapshot.Remaining;
        var time = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        return snapshot.State switch
        {
            TimerState.Working => $"距离下次远眺  {time}",
            TimerState.ReminderDue => "该看看远处了",
            TimerState.Resting => $"休息中  {time}",
            TimerState.Paused => $"提醒已暂停 · 至 {snapshot.PausedUntil?.ToLocalTime():HH:mm}",
            _ => "眺刻"
        };
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _applicationIcon.Dispose();
    }

    private static Icon LoadApplicationIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon is not null) return icon;
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private sealed class TiaoKeColorTable : ProfessionalColorTable
    {
        private readonly bool _darkTheme;

        public TiaoKeColorTable(bool darkTheme)
        {
            _darkTheme = darkTheme;
            UseSystemColors = false;
        }

        private Color Background => _darkTheme
            ? Color.FromArgb(34, 38, 36)
            : Color.FromArgb(255, 255, 255);

        private Color Border => _darkTheme
            ? Color.FromArgb(64, 71, 66)
            : Color.FromArgb(215, 221, 218);

        private Color Hover => _darkTheme
            ? Color.FromArgb(48, 54, 50)
            : Color.FromArgb(240, 243, 241);

        public override Color ToolStripDropDownBackground => Background;
        public override Color MenuBorder => Border;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemBorder => Hover;
        public override Color ImageMarginGradientBegin => Background;
        public override Color ImageMarginGradientMiddle => Background;
        public override Color ImageMarginGradientEnd => Background;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
