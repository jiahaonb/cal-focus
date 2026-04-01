using CalFocus.App.Services;

namespace CalFocus.App.Views.Pages;

public sealed partial class ThemePage : Page
{
    private App CurrentApp => (App)Application.Current;
    private string _selectedThemeColor = "#0D5D56";

    public ThemePage()
    {
        InitializeComponent();
        BindFromSettings(CurrentApp.UiSettingsService.Current);
    }

    private void BindFromSettings(UiSettings settings)
    {
        _selectedThemeColor = settings.BrandColorHex;
        CurrentThemeColorText.Text = $"当前主题色: {_selectedThemeColor}";

        EnableGlassSwitch.IsOn = settings.EnableGlass;
        EnableHoverSwitch.IsOn = settings.EnableHoverFeedback;
        EnableAnimationSwitch.IsOn = settings.EnableComplexAnimations;
        HighQualityBlurSwitch.IsOn = settings.HighQualityBlur;
        ShadowStrengthSlider.Value = settings.ShadowStrength;

        SaveStateText.Text = "";
    }

    private void OnThemeColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string color)
        {
            _selectedThemeColor = color;
            CurrentThemeColorText.Text = $"当前主题色: {_selectedThemeColor}";
        }
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        var settings = new UiSettings
        {
            ThemeName = "CalFocus Glass",
            BrandColorHex = _selectedThemeColor,
            EnableGlass = EnableGlassSwitch.IsOn,
            EnableHoverFeedback = EnableHoverSwitch.IsOn,
            EnableComplexAnimations = EnableAnimationSwitch.IsOn,
            HighQualityBlur = HighQualityBlurSwitch.IsOn,
            ShadowStrength = (int)ShadowStrengthSlider.Value
        };

        CurrentApp.UiSettingsService.Update(settings, Application.Current.Resources);
        SaveStateText.Text = "已应用，全局页面已同步更新";
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        var defaults = UiSettings.CreateDefault();
        BindFromSettings(defaults);
        CurrentApp.UiSettingsService.Update(defaults, Application.Current.Resources);
        SaveStateText.Text = "已恢复默认";
    }
}