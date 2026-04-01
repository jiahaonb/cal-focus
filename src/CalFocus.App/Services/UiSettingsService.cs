using System.Text.Json;
using CalFocus.Core.Abstractions.Services;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CalFocus.App.Services;

public sealed class UiSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;

    public event Action<UiSettings>? SettingsChanged;

    public UiSettings Current { get; private set; } = UiSettings.CreateDefault();

    public UiSettingsService(IAppDataPathService pathService)
    {
        _settingsPath = pathService.UiSettingsPath;
    }

    public void LoadAndApply(ResourceDictionary resources)
    {
        Current = LoadInternal();
        TryApply(Current, resources);
    }

    public void Update(UiSettings settings, ResourceDictionary resources)
    {
        Current = settings;
        TryApply(Current, resources);
        SaveInternal(Current);
        SettingsChanged?.Invoke(Current);
    }

    private static void TryApply(UiSettings settings, ResourceDictionary resources)
    {
        try
        {
            Apply(settings, resources);
        }
        catch
        {
            // Some devices/contexts can throw COMException when creating advanced brushes during app startup.
            // Fall back to a safe baseline so the app can always launch.
            try
            {
                Apply(settings.WithGlassDisabled(), resources);
            }
            catch
            {
                ApplyMinimalFallback(resources);
            }
        }
    }

    private static void ApplyMinimalFallback(ResourceDictionary resources)
    {
        var background = Color.FromArgb(255, 238, 243, 241);
        var surface = Color.FromArgb(255, 246, 251, 249);
        var brand = Color.FromArgb(255, 13, 93, 86);
        var text = Color.FromArgb(255, 27, 42, 39);
        var muted = Color.FromArgb(255, 111, 133, 128);
        var border = Color.FromArgb(68, 255, 255, 255);
        var card = Color.FromArgb(245, 255, 255, 255);

        resources["AppBrandColor"] = brand;
        resources["AppBackgroundColor"] = background;
        resources["AppSurfaceColor"] = surface;
        resources["AppCardColor"] = card;
        resources["AppCardHoverColor"] = Color.FromArgb(255, 255, 255, 255);
        resources["AppTextColor"] = text;
        resources["AppMutedColor"] = muted;
        resources["AppBorderColor"] = border;
        resources["AppTodayHighlightColor"] = Color.FromArgb(56, brand.R, brand.G, brand.B);

        resources["AppBrandBrush"] = new SolidColorBrush(brand);
        resources["AppBackgroundBrush"] = new SolidColorBrush(background);
        resources["AppSurfaceBrush"] = new SolidColorBrush(surface);
        resources["AppCardBrush"] = new SolidColorBrush(card);
        resources["AppCardHoverBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        resources["AppTextBrush"] = new SolidColorBrush(text);
        resources["AppTitleBrush"] = new SolidColorBrush(text);
        resources["AppMutedBrush"] = new SolidColorBrush(muted);
        resources["AppBorderBrush"] = new SolidColorBrush(border);
        resources["AppTodayHighlightBrush"] = new SolidColorBrush(Color.FromArgb(56, brand.R, brand.G, brand.B));
        resources["AppGlassBrush"] = new SolidColorBrush(card);
    }

    private UiSettings LoadInternal()
    {
        if (!File.Exists(_settingsPath))
        {
            return UiSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<UiSettings>(json, JsonOptions);
            return settings ?? UiSettings.CreateDefault();
        }
        catch
        {
            return UiSettings.CreateDefault();
        }
    }

    private void SaveInternal(UiSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static void Apply(UiSettings settings, ResourceDictionary resources)
    {
        var brand = ParseHex(settings.BrandColorHex, Color.FromArgb(255, 13, 93, 86));
        var text = Blend(brand, Microsoft.UI.Colors.Black, 0.86);
        var background = Blend(brand, Microsoft.UI.Colors.White, 0.93);
        var surface = Blend(brand, Microsoft.UI.Colors.White, 0.97);
        var card = Color.FromArgb(245, 255, 255, 255);
        var hover = Color.FromArgb(255, 255, 255, 255);
        var muted = Blend(text, Microsoft.UI.Colors.White, 0.45);
        var border = Color.FromArgb(64, 255, 255, 255);
        var todayHighlight = Color.FromArgb(56, brand.R, brand.G, brand.B);

        resources["AppBrandColor"] = brand;
        resources["AppBackgroundColor"] = background;
        resources["AppSurfaceColor"] = surface;
        resources["AppCardColor"] = card;
        resources["AppCardHoverColor"] = hover;
        resources["AppTextColor"] = text;
        resources["AppMutedColor"] = muted;
        resources["AppBorderColor"] = border;
        resources["AppTodayHighlightColor"] = todayHighlight;

        resources["AppBrandBrush"] = new SolidColorBrush(brand);
        resources["AppBackgroundBrush"] = new SolidColorBrush(background);
        resources["AppSurfaceBrush"] = new SolidColorBrush(surface);
        resources["AppCardBrush"] = new SolidColorBrush(card);
        resources["AppCardHoverBrush"] = new SolidColorBrush(hover);
        resources["AppTextBrush"] = new SolidColorBrush(text);
        resources["AppTitleBrush"] = new SolidColorBrush(text);
        resources["AppMutedBrush"] = new SolidColorBrush(muted);
        resources["AppBorderBrush"] = new SolidColorBrush(border);
        resources["AppTodayHighlightBrush"] = new SolidColorBrush(todayHighlight);

        resources["AppGlassBrush"] = CreateGlassBrush(settings);
    }

    private static Brush CreateGlassBrush(UiSettings settings)
    {
        var fallback = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255));

        if (!settings.EnableGlass)
        {
            return fallback;
        }

        try
        {
            return new AcrylicBrush
            {
                TintColor = Microsoft.UI.Colors.White,
                TintOpacity = settings.HighQualityBlur ? 0.60 : 0.48,
                TintLuminosityOpacity = settings.HighQualityBlur ? 0.26 : 0.16,
                FallbackColor = Color.FromArgb(247, 255, 255, 255)
            };
        }
        catch
        {
            return fallback;
        }
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        var inv = 1 - amount;

        var r = (byte)(from.R * inv + to.R * amount);
        var g = (byte)(from.G * inv + to.G * amount);
        var b = (byte)(from.B * inv + to.B * amount);

        return Color.FromArgb(255, r, g, b);
    }

    private static Color ParseHex(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        var normalized = hex.Trim();
        if (normalized.StartsWith("#"))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 6)
        {
            normalized = $"FF{normalized}";
        }

        if (normalized.Length != 8)
        {
            return fallback;
        }

        try
        {
            var a = Convert.ToByte(normalized.Substring(0, 2), 16);
            var r = Convert.ToByte(normalized.Substring(2, 2), 16);
            var g = Convert.ToByte(normalized.Substring(4, 2), 16);
            var b = Convert.ToByte(normalized.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return fallback;
        }
    }
}

public sealed class UiSettings
{
    public string ThemeName { get; set; } = "CalFocus Glass";
    public string BrandColorHex { get; set; } = "#0D5D56";
    public bool EnableGlass { get; set; } = true;
    public bool EnableHoverFeedback { get; set; } = true;
    public bool HighQualityBlur { get; set; } = true;
    public bool EnableComplexAnimations { get; set; } = true;
    public int ShadowStrength { get; set; } = 70;

    public static UiSettings CreateDefault()
    {
        return new UiSettings();
    }

    public UiSettings WithGlassDisabled()
    {
        return new UiSettings
        {
            ThemeName = ThemeName,
            BrandColorHex = BrandColorHex,
            EnableGlass = false,
            EnableHoverFeedback = EnableHoverFeedback,
            HighQualityBlur = HighQualityBlur,
            EnableComplexAnimations = EnableComplexAnimations,
            ShadowStrength = ShadowStrength
        };
    }
}
