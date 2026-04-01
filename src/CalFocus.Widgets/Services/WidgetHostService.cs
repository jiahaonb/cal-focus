using CalFocus.Core.Domain.Entities;

namespace CalFocus.Widgets.Services;

public sealed class WidgetHostService
{
    private readonly Dictionary<Guid, WidgetInstance> _widgets = new();
    private readonly List<Guid> _order = new();

    public event Action<WidgetInstance>? WidgetAdded;
    public event Action<Guid>? WidgetRemoved;
    public event Action? WidgetsChanged;

    public IReadOnlyList<WidgetInstance> ActiveWidgets => _order
        .Where(id => _widgets.ContainsKey(id))
        .Select(id => _widgets[id])
        .ToList();

    public WidgetInstance CreateClockWidget(string? tintColorHex = null)
    {
        var widget = new WidgetInstance
        {
            WidgetType = "Clock",
            Width = 280,
            Height = 160,
            X = 120,
            Y = 120,
            TintColorHex = string.IsNullOrWhiteSpace(tintColorHex) ? WidgetInstance.DefaultTintColorHex : tintColorHex
        };

        AddWidget(widget);
        return widget;
    }

    public WidgetInstance CreateScheduleWidget(string? tintColorHex = null)
    {
        var widget = new WidgetInstance
        {
            WidgetType = "Schedule",
            Width = 360,
            Height = 220,
            X = 160,
            Y = 160,
            TintColorHex = string.IsNullOrWhiteSpace(tintColorHex) ? WidgetInstance.DefaultTintColorHex : tintColorHex
        };

        AddWidget(widget);
        return widget;
    }

    public WidgetInstance CreatePomodoroWidget(string? tintColorHex = null)
    {
        var widget = new WidgetInstance
        {
            WidgetType = "Pomodoro",
            Width = 300,
            Height = 220,
            X = 180,
            Y = 180,
            TintColorHex = string.IsNullOrWhiteSpace(tintColorHex) ? WidgetInstance.DefaultTintColorHex : tintColorHex
        };

        AddWidget(widget);
        return widget;
    }

    public WidgetInstance? CopyLastWidget()
    {
        if (_order.Count == 0)
        {
            return null;
        }

        var sourceId = _order[^1];
        if (!_widgets.TryGetValue(sourceId, out var source))
        {
            return null;
        }

        var clone = new WidgetInstance
        {
            WidgetType = source.WidgetType,
            DisplayId = source.DisplayId,
            X = source.X + 28,
            Y = source.Y + 28,
            Width = source.Width,
            Height = source.Height,
            Opacity = source.Opacity,
            StylePreset = source.StylePreset,
            TintColorHex = source.TintColorHex,
            Locked = source.Locked
        };

        AddWidget(clone);
        return clone;
    }

    public void RestoreWidgets(IEnumerable<WidgetInstance> widgets)
    {
        foreach (var widget in widgets)
        {
            AddWidget(widget);
        }
    }

    public bool TryGetWidget(Guid widgetId, out WidgetInstance widget)
    {
        return _widgets.TryGetValue(widgetId, out widget!);
    }

    public bool RemoveWidget(Guid widgetId)
    {
        var removed = _widgets.Remove(widgetId);
        if (!removed)
        {
            return false;
        }

        _order.Remove(widgetId);
        WidgetRemoved?.Invoke(widgetId);
        WidgetsChanged?.Invoke();
        return true;
    }

    public bool RemoveLastWidget()
    {
        if (_order.Count == 0)
        {
            return false;
        }

        var lastId = _order[^1];
        return RemoveWidget(lastId);
    }

    public void NotifyWidgetsChanged()
    {
        WidgetsChanged?.Invoke();
    }

    private void AddWidget(WidgetInstance widget)
    {
        _widgets[widget.Id] = widget;
        _order.Add(widget.Id);

        WidgetAdded?.Invoke(widget);
        WidgetsChanged?.Invoke();
    }
}
