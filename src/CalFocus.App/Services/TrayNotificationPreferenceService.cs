using System.Text.Json;
using CalFocus.Core.Abstractions.Services;

namespace CalFocus.App.Services;

public sealed class TrayNotificationPreferenceService
{
    private readonly string _settingsPath;

    public TrayNotificationPreferenceService(IAppDataPathService appDataPathService)
    {
        _settingsPath = appDataPathService.TraySettingsPath;
    }

    public bool IsEnabled()
    {
        if (!File.Exists(_settingsPath))
        {
            return true;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var data = JsonSerializer.Deserialize<TraySettings>(json);
            return data?.EnableBalloonNotifications ?? true;
        }
        catch
        {
            return true;
        }
    }

    public bool Toggle()
    {
        var enabled = !IsEnabled();
        SetEnabled(enabled);
        return enabled;
    }

    public void SetEnabled(bool enabled)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new TraySettings(enabled));
        File.WriteAllText(_settingsPath, json);
    }

    private sealed record TraySettings(bool EnableBalloonNotifications);
}
