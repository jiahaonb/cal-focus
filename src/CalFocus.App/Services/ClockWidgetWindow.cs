using System.Runtime.InteropServices;
using CalFocus.Core.Domain.Entities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace CalFocus.App.Services;

public sealed class ClockWidgetWindow : Window
{
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private readonly WidgetInstance _widget;
    private readonly TextBlock _timeText;
    private readonly DispatcherTimer _timer;

    private AppWindow? _appWindow;
    private bool _placementInitialized;

    public event Action<Guid>? WindowClosed;
    public event Action<Guid>? WidgetChanged;

    public ClockWidgetWindow(WidgetInstance widget)
    {
        _widget = widget;

        var border = new Border
        {
            CornerRadius = new CornerRadius(18),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.2 },
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black) { Opacity = 0.55 },
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "桌面时钟",
                        FontSize = 13,
                        Opacity = 0.75
                    }
                }
            }
        };

        _timeText = new TextBlock
        {
            FontSize = 42,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        ((StackPanel)border.Child).Children.Add(_timeText);

        Content = border;
        Title = "Cal Focus Clock";

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => UpdateTime();

        Activated += OnActivated;
        Closed += OnClosed;

        UpdateTime();
        _timer.Start();
    }

    public void SetVisibility(bool visible)
    {
        var handle = WindowNative.GetWindowHandle(this);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _ = ShowWindow(handle, visible ? SW_SHOW : SW_HIDE);
    }

    public void ApplyWidgetPlacement()
    {
        if (!TryEnsureAppWindow())
        {
            return;
        }

        _appWindow!.Resize(new SizeInt32((int)_widget.Width, (int)_widget.Height));
        _appWindow.Move(new PointInt32((int)_widget.X, (int)_widget.Y));
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_placementInitialized)
        {
            return;
        }

        ApplyWidgetPlacement();

        if (_appWindow is null)
        {
            return;
        }

        _appWindow.Changed += OnAppWindowChanged;

        _placementInitialized = true;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _timer.Stop();

        if (_appWindow is not null)
        {
            _appWindow.Changed -= OnAppWindowChanged;
        }

        WindowClosed?.Invoke(_widget.Id);
    }

    private bool TryEnsureAppWindow()
    {
        if (_appWindow is not null)
        {
            return true;
        }

        var handle = WindowNative.GetWindowHandle(this);
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        return _appWindow is not null;
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        var changed = false;

        if (args.DidSizeChange)
        {
            _widget.Width = sender.Size.Width;
            _widget.Height = sender.Size.Height;
            changed = true;
        }

        if (args.DidPositionChange)
        {
            _widget.X = sender.Position.X;
            _widget.Y = sender.Position.Y;
            changed = true;
        }

        if (changed)
        {
            WidgetChanged?.Invoke(_widget.Id);
        }
    }

    private void UpdateTime()
    {
        _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
