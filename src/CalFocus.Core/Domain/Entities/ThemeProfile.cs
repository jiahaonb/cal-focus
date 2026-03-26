namespace CalFocus.Core.Domain.Entities;

public sealed class ThemeProfile
{
    public string Mode { get; set; } = "Glass";
    public string ColorScheme { get; set; } = "Auto";
    public string? BackgroundImagePath { get; set; }
    public double BackgroundOpacity { get; set; } = 0.75;
    public double BlurStrength { get; set; } = 0.65;
    public double TintStrength { get; set; } = 0.25;
    public double NoiseStrength { get; set; } = 0.08;
}
