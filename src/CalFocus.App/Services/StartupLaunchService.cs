using Microsoft.Win32;

namespace CalFocus.App.Services;

public sealed class StartupLaunchService
{
    private const string RunRegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppRegistryName = "CalFocus";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        var value = key?.GetValue(AppRegistryName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public bool Toggle()
    {
        var next = !IsEnabled();
        SetEnabled(next);
        return next;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true);
        if (key is null)
        {
            return;
        }

        if (!enabled)
        {
            key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
            return;
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        key.SetValue(AppRegistryName, $"\"{exePath}\"");
    }
}
