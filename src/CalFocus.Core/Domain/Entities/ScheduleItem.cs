namespace CalFocus.Core.Domain.Entities;

public sealed class ScheduleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? RepeatRule { get; set; }
    public string? Category { get; set; }
    public string? ColorHex { get; set; }
    public string? Note { get; set; }
}
