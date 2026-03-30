namespace CalFocus.Core.Abstractions.Services;

public interface IAppDataPathService
{
    string AppDataRoot { get; }
    string DatabaseFilePath { get; }
    string ThemeProfilePath { get; }
    string WidgetLayoutPath { get; }
    string TraySettingsPath { get; }
}
