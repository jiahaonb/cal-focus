using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;
using CalFocus.Widgets.Services;
using Microsoft.UI.Xaml;

namespace CalFocus.App.Services;

public sealed class DesktopWidgetManager
{
    private readonly WidgetHostService _hostService;
    private readonly IWidgetLayoutService _layoutService;
    private readonly IDisplayService _displayService;

    private readonly Dictionary<Guid, ClockWidgetWindow> _clockWindows = new();
    private readonly Dictionary<Guid, ScheduleWidgetWindow> _scheduleWindows = new();
    private readonly HashSet<Guid> _closingByProgram = new();

    private readonly DispatcherTimer _displayWatchTimer;
    private string _displayTopologyFingerprint = string.Empty;

    private bool _allVisible = true;
    public bool AreWidgetsVisible => _allVisible;
    private CancellationTokenSource? _saveCts;

    public DesktopWidgetManager(WidgetHostService hostService, IWidgetLayoutService layoutService, IDisplayService displayService)
    {
        _hostService = hostService;
        _layoutService = layoutService;
        _displayService = displayService;

        _hostService.WidgetAdded += OnWidgetAdded;
        _hostService.WidgetRemoved += OnWidgetRemoved;
        _hostService.WidgetsChanged += OnWidgetsChanged;

        _displayWatchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _displayWatchTimer.Tick += OnDisplayWatchTick;
        _displayWatchTimer.Start();

        _displayTopologyFingerprint = ComputeDisplayTopologyFingerprint();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var widgets = await _layoutService.GetAllAsync(cancellationToken);
        if (widgets.Count == 0)
        {
            return;
        }

        _hostService.RestoreWidgets(widgets);
    }

    public void ToggleAllVisibility()
    {
        _allVisible = !_allVisible;

        foreach (var window in _clockWindows.Values)
        {
            window.SetVisibility(_allVisible);
        }

        foreach (var window in _scheduleWindows.Values)
        {
            window.SetVisibility(_allVisible);
        }
    }

    private void OnWidgetsChanged()
    {
        SchedulePersist();
    }

    private void OnWidgetAdded(WidgetInstance widget)
    {
        NormalizeWidgetDisplay(widget);

        if (string.Equals(widget.WidgetType, "Clock", StringComparison.OrdinalIgnoreCase))
        {
            var window = new ClockWidgetWindow(widget);
            window.WindowClosed += OnWidgetWindowClosedByUser;
            window.WidgetChanged += OnWidgetWindowChanged;
            _clockWindows[widget.Id] = window;
            window.Activate();
            return;
        }

        if (string.Equals(widget.WidgetType, "Schedule", StringComparison.OrdinalIgnoreCase))
        {
            var window = new ScheduleWidgetWindow(widget);
            window.WindowClosed += OnWidgetWindowClosedByUser;
            window.WidgetChanged += OnWidgetWindowChanged;
            _scheduleWindows[widget.Id] = window;
            window.Activate();
        }
    }

    private void OnWidgetRemoved(Guid widgetId)
    {
        if (_clockWindows.TryGetValue(widgetId, out var clockWindow))
        {
            _closingByProgram.Add(widgetId);
            CloseWindowSafely(clockWindow);
            clockWindow.WindowClosed -= OnWidgetWindowClosedByUser;
            clockWindow.WidgetChanged -= OnWidgetWindowChanged;
            _clockWindows.Remove(widgetId);
            return;
        }

        if (_scheduleWindows.TryGetValue(widgetId, out var scheduleWindow))
        {
            _closingByProgram.Add(widgetId);
            CloseWindowSafely(scheduleWindow);
            scheduleWindow.WindowClosed -= OnWidgetWindowClosedByUser;
            scheduleWindow.WidgetChanged -= OnWidgetWindowChanged;
            _scheduleWindows.Remove(widgetId);
        }
    }

    private void OnWidgetWindowClosedByUser(Guid widgetId)
    {
        if (_closingByProgram.Remove(widgetId))
        {
            return;
        }

        _ = _hostService.RemoveWidget(widgetId);
    }

    private void OnWidgetWindowChanged(Guid widgetId)
    {
        if (_hostService.TryGetWidget(widgetId, out var widget))
        {
            var display = _displayService.GetDisplayForPoint(widget.X, widget.Y) ?? _displayService.GetPrimaryDisplay();
            if (display is not null)
            {
                widget.DisplayId = display.DisplayId;
            }
        }

        _hostService.NotifyWidgetsChanged();
    }

    private void OnDisplayWatchTick(object? sender, object e)
    {
        var current = ComputeDisplayTopologyFingerprint();
        if (string.Equals(current, _displayTopologyFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _displayTopologyFingerprint = current;
        ReflowWidgetsForDisplayTopology();
    }

    private string ComputeDisplayTopologyFingerprint()
    {
        var displays = _displayService.GetDisplays()
            .OrderBy(x => x.DisplayId, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.DisplayId}:{x.X},{x.Y},{x.Width},{x.Height}:{x.WorkAreaX},{x.WorkAreaY},{x.WorkAreaWidth},{x.WorkAreaHeight}");

        return string.Join("|", displays);
    }

    private void ReflowWidgetsForDisplayTopology()
    {
        var changed = false;

        foreach (var widget in _hostService.ActiveWidgets)
        {
            var originalX = widget.X;
            var originalY = widget.Y;
            var originalDisplay = widget.DisplayId;

            NormalizeWidgetDisplay(widget);

            if (Math.Abs(widget.X - originalX) > 0.01 ||
                Math.Abs(widget.Y - originalY) > 0.01 ||
                !string.Equals(widget.DisplayId, originalDisplay, StringComparison.OrdinalIgnoreCase))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        foreach (var widget in _hostService.ActiveWidgets)
        {
            if (_clockWindows.TryGetValue(widget.Id, out var clockWindow))
            {
                clockWindow.ApplyWidgetPlacement();
                continue;
            }

            if (_scheduleWindows.TryGetValue(widget.Id, out var scheduleWindow))
            {
                scheduleWindow.ApplyWidgetPlacement();
            }
        }

        _hostService.NotifyWidgetsChanged();
    }

    private void NormalizeWidgetDisplay(WidgetInstance widget)
    {
        var displays = _displayService.GetDisplays();
        if (displays.Count == 0)
        {
            return;
        }

        var targetDisplay = displays.FirstOrDefault(x => string.Equals(x.DisplayId, widget.DisplayId, StringComparison.OrdinalIgnoreCase));
        if (targetDisplay is null)
        {
            targetDisplay = _displayService.GetDisplayForPoint(widget.X, widget.Y) ?? _displayService.GetPrimaryDisplay();
        }

        if (targetDisplay is null)
        {
            return;
        }

        widget.DisplayId = targetDisplay.DisplayId;

        var maxX = targetDisplay.WorkAreaX + targetDisplay.WorkAreaWidth - (int)widget.Width;
        var maxY = targetDisplay.WorkAreaY + targetDisplay.WorkAreaHeight - (int)widget.Height;

        widget.X = Math.Clamp(widget.X, targetDisplay.WorkAreaX, Math.Max(targetDisplay.WorkAreaX, maxX));
        widget.Y = Math.Clamp(widget.Y, targetDisplay.WorkAreaY, Math.Max(targetDisplay.WorkAreaY, maxY));
    }

    private void SchedulePersist()
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();

        var token = _saveCts.Token;
        _ = PersistAsync(token);
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(500, cancellationToken);
            await _layoutService.SaveAllAsync(_hostService.ActiveWidgets, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void CloseWindowSafely(Window window)
    {
        if (window.DispatcherQueue.HasThreadAccess)
        {
            window.Close();
        }
        else
        {
            _ = window.DispatcherQueue.TryEnqueue(window.Close);
        }
    }
}
