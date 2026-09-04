using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TiaoKe.App.Models;

namespace TiaoKe.App.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _settingsPath;

    public SettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TiaoKe",
            "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            var json = File.ReadAllText(_settingsPath);
            return Validate(JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings());
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings = Validate(settings);
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("Settings path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, SerializerOptions));
        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }

    private static AppSettings Validate(AppSettings settings)
    {
        settings.WorkMinutes = Math.Clamp(settings.WorkMinutes, 1, 180);
        settings.RestSeconds = Math.Clamp(settings.RestSeconds, 5, 300);
        settings.Theme = settings.Theme is "light" or "dark" ? settings.Theme : "system";
        settings.ReminderCorner = settings.ReminderCorner is "topLeft" or "topRight" or "bottomRight"
            ? settings.ReminderCorner
            : "bottomLeft";
        return settings;
    }
}
