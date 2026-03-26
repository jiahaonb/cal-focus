using System.Text.Json;

namespace CalFocus.App.Services;

public sealed class NavigationStateService
{
    private readonly string _statePath;

    public NavigationStateService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CalFocus");

        Directory.CreateDirectory(root);
        _statePath = Path.Combine(root, "ui.state.json");
    }

    public string LoadSelectedTag()
    {
        if (!File.Exists(_statePath))
        {
            return "schedule";
        }

        var json = File.ReadAllText(_statePath);
        var state = JsonSerializer.Deserialize<NavigationState>(json);
        return string.IsNullOrWhiteSpace(state?.SelectedTag) ? "schedule" : state.SelectedTag;
    }

    public void SaveSelectedTag(string tag)
    {
        var json = JsonSerializer.Serialize(new NavigationState(tag));
        File.WriteAllText(_statePath, json);
    }

    private sealed record NavigationState(string SelectedTag);
}
