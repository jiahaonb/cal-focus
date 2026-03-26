namespace CalFocus.Core.Domain.Entities;

public sealed class WidgetInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WidgetType { get; set; } = "Clock";
    public string DisplayId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 180;
    public double Opacity { get; set; } = 0.9;
    public string StylePreset { get; set; } = "Glass";
    public bool Locked { get; set; }
}
