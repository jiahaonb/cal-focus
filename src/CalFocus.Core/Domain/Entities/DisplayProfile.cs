namespace CalFocus.Core.Domain.Entities;

public sealed class DisplayProfile
{
    public string DisplayId { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public double DpiScale { get; set; } = 1.0;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int WorkAreaX { get; set; }
    public int WorkAreaY { get; set; }
    public int WorkAreaWidth { get; set; }
    public int WorkAreaHeight { get; set; }
}
