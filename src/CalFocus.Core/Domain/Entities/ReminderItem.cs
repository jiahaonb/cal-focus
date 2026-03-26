namespace CalFocus.Core.Domain.Entities;

public sealed class ReminderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset TriggerAt { get; set; }
    public string? RepeatRule { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public int SnoozeMinutes { get; set; } = 5;
}
