using Microsoft.Win32;

namespace TiaoKe.App.Services;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TiaoKe";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine the application path.");
        key.SetValue(ValueName, $"\"{executablePath}\"");
    }
}
