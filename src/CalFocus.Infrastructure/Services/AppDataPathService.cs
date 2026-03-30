using CalFocus.Core.Abstractions.Services;

namespace CalFocus.Infrastructure.Services;

public sealed class AppDataPathService : IAppDataPathService
{
    public string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CalFocus");

    public string DatabaseFilePath => Path.Combine(AppDataRoot, "calfocus.db");

    public string ThemeProfilePath => Path.Combine(AppDataRoot, "theme.profile.json");

    public string WidgetLayoutPath => Path.Combine(AppDataRoot, "widgets.layout.json");

    public string TraySettingsPath => Path.Combine(AppDataRoot, "tray.settings.json");
}
