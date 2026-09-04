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
        var renderer = new TiaoKeMenuRenderer(darkTheme);
        _menu.Renderer = renderer;
        ConfigureDropDown(_menu, renderer);
        ConfigureDropDown(pauseReminderMenu.DropDown, renderer);
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
        var renderer = new TiaoKeMenuRenderer(darkTheme);
        ApplyRenderer(_menu, renderer);
        _statusItem.ForeColor = darkTheme ? Color.FromArgb(170, 179, 174) : Color.FromArgb(104, 113, 108);
        SetMenuColors(_menu, darkTheme ? Color.FromArgb(241, 244, 242) : Color.FromArgb(32, 37, 34),
            darkTheme ? Color.FromArgb(170, 179, 174) : Color.FromArgb(104, 113, 108));
    }

    private void AddMenuItem(ToolStripMenuItem item)
    {
        item.Padding = new Padding(12, 7, 12, 7);
        item.Margin = new Padding(2, 1, 2, 1);
        item.AutoSize = true;
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

    private static void ConfigureDropDown(ToolStripDropDown dropDown, TiaoKeMenuRenderer renderer)
    {
        dropDown.Renderer = renderer;
        if (dropDown is ToolStripDropDownMenu menu)
        {
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = false;
        }
        dropDown.Padding = new Padding(5, 6, 5, 6);
        dropDown.DropShadowEnabled = true;
        dropDown.VisibleChanged += (_, _) =>
        {
            if (dropDown.Visible) ApplyRoundedRegion(dropDown);
        };
        dropDown.Layout += (_, _) =>
        {
            if (dropDown.Visible) ApplyRoundedRegion(dropDown);
        };
    }

    private static void ApplyRenderer(ToolStrip toolStrip, TiaoKeMenuRenderer renderer)
    {
        toolStrip.Renderer = renderer;
        foreach (ToolStripItem item in toolStrip.Items)
        {
            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                ApplyRenderer(menuItem.DropDown, renderer);
            }
        }
    }

    private static void ApplyRoundedRegion(ToolStripDropDown dropDown)
    {
        if (dropDown.Width <= 0 || dropDown.Height <= 0) return;

        using var path = RoundedRectangle(
            new Rectangle(0, 0, dropDown.Width, dropDown.Height),
            10);
        var oldRegion = dropDown.Region;
        dropDown.Region = new Region(path);
        oldRegion?.Dispose();
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void SetMenuColors(ToolStrip dropDown, Color foreground, Color mutedForeground)
    {
        foreach (ToolStripItem item in dropDown.Items)
        {
            if (item is ToolStripSeparator) continue;
            item.ForeColor = item.Enabled ? foreground : mutedForeground;
            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                SetMenuColors(menuItem.DropDown, foreground, mutedForeground);
            }
        }
    }

    private sealed class TiaoKeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool _darkTheme;

        public TiaoKeMenuRenderer(bool darkTheme)
            : base(new TiaoKeColorTable(darkTheme))
        {
            _darkTheme = darkTheme;
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

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Background);
            using var path = RoundedRectangle(
                new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1),
                10);
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = e.Item.Bounds;
            bounds.Inflate(-1, -1);
            using var brush = new SolidBrush(Hover);
            using var path = RoundedRectangle(bounds, 6);
            e.Graphics.FillPath(brush, path);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Border);
            using var path = RoundedRectangle(
                new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1),
                10);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = _darkTheme
                    ? Color.FromArgb(170, 179, 174)
                    : Color.FromArgb(104, 113, 108);
            }
            else
            {
                e.TextColor = _darkTheme
                    ? Color.FromArgb(241, 244, 242)
                    : Color.FromArgb(32, 37, 34);
            }

            base.OnRenderItemText(e);
        }
    }

    private sealed class TiaoKeColorTable : ProfessionalColorTable
    {
        private readonly Color _background;
        private readonly Color _border;
        private readonly Color _hover;

        public TiaoKeColorTable(bool darkTheme)
        {
            _background = darkTheme
                ? Color.FromArgb(34, 38, 36)
                : Color.FromArgb(255, 255, 255);
            _border = darkTheme
                ? Color.FromArgb(64, 71, 66)
                : Color.FromArgb(215, 221, 218);
            _hover = darkTheme
                ? Color.FromArgb(48, 54, 50)
                : Color.FromArgb(240, 243, 241);

            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => _background;
        public override Color MenuBorder => _border;
        public override Color MenuItemSelected => _hover;
        public override Color MenuItemBorder => _hover;
        public override Color ImageMarginGradientBegin => _background;
        public override Color ImageMarginGradientMiddle => _background;
        public override Color ImageMarginGradientEnd => _background;
        public override Color SeparatorDark => _border;
        public override Color SeparatorLight => _border;
    }
}
