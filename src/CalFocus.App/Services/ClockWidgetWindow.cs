using System.Runtime.InteropServices;
using CalFocus.Core.Domain.Entities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace CalFocus.App.Services;

public sealed class ClockWidgetWindow : Window
{
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private readonly WidgetInstance _widget;
    private readonly TextBlock _timeText;
    private readonly TextBlock _dateText;
    private readonly DispatcherTimer _timer;
    private readonly Border _innerCard;
    private readonly Border _outerCard;
    private readonly Border _topGlow;

    private AppWindow? _appWindow;
    private bool _placementInitialized;
    private bool _chromeConfigured;
    private bool _isApplyingConstrainedResize;
    private bool _suppressSizeModeAutoSwitch;

    public event Action<Guid>? WindowClosed;
    public event Action<Guid>? WidgetChanged;

    public ClockWidgetWindow(WidgetInstance widget)
    {
        _widget = widget;
        EnsureWidgetTint();
        _widget.SizeMode = WidgetSizingService.NormalizeSizeMode(_widget.SizeMode);

        _timeText = new TextBlock
        {
            FontSize = 46,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(248, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _dateText = new TextBlock
        {
            FontSize = 13,
            Opacity = 0.9,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(220, 240, 248, 255)),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _topGlow = new Border
        {
            Height = 46,
            CornerRadius = new CornerRadius(32, 32, 22, 22),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 8, 10, 0),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0))
        };

        var contentStack = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "时钟",
                    FontSize = 14,
                    Opacity = 0.88,
                    Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(236, 229, 244, 255))
                },
                _timeText,
                _dateText
            }
        };

        _innerCard = new Border
        {
            CornerRadius = new CornerRadius(26),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(115, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(84, 30, 44, 58)),
            Padding = new Thickness(18, 14, 18, 16),
            Child = contentStack
        };

        _outerCard = new Border
        {
            CornerRadius = new CornerRadius(34),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(145, 255, 255, 255)),
            BorderThickness = new Thickness(1.2),
            Background = CreateGlassBrush(ResolveTintColor()),
            Padding = new Thickness(8),
            Child = _innerCard
        };

        var menuButton = CreateMenuButton();

        var root = new Grid
        {
            Margin = new Thickness(4),
            Children =
            {
                _outerCard,
                _topGlow,
                menuButton
            }
        };

        Content = root;
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
        ApplyTintToVisuals();
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

        ApplySizeModeToCurrentDisplay();

        _suppressSizeModeAutoSwitch = true;
        _appWindow!.Resize(new SizeInt32((int)_widget.Width, (int)_widget.Height));
        _suppressSizeModeAutoSwitch = false;
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

        ConfigureWindowChrome();

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
        if (_isApplyingConstrainedResize)
        {
            return;
        }

        var changed = false;

        if (args.DidSizeChange)
        {
            var workArea = GetWorkAreaBounds(sender);

            if (!_suppressSizeModeAutoSwitch)
            {
                _widget.SizeMode = WidgetInstance.SizeModeFree;
            }

            var constrained = WidgetSizingService.ConstrainSize(
                _widget.WidgetType,
                sender.Size.Width,
                sender.Size.Height,
                workArea.Width,
                workArea.Height,
                preferHeightSafety: true);

            if (constrained.Width != sender.Size.Width || constrained.Height != sender.Size.Height)
            {
                _isApplyingConstrainedResize = true;
                _suppressSizeModeAutoSwitch = true;
                sender.Resize(new SizeInt32(constrained.Width, constrained.Height));
                _suppressSizeModeAutoSwitch = false;
                _isApplyingConstrainedResize = false;
            }

            _widget.Width = constrained.Width;
            _widget.Height = constrained.Height;
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
        _dateText.Text = DateTime.Now.ToString("MM月dd日 dddd", new System.Globalization.CultureInfo("zh-CN"));
    }

    private void ConfigureWindowChrome()
    {
        if (_chromeConfigured || _appWindow is null)
        {
            return;
        }

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
        }

        _chromeConfigured = true;
    }

    private Button CreateMenuButton()
    {
        var button = new Button
        {
            Width = 34,
            Height = 34,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 10, 10, 0),
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(17),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(82, 16, 24, 34)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(96, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Content = new FontIcon
            {
                Glyph = "\uE712",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(232, 243, 248, 255))
            },
            Flyout = BuildMenuFlyout()
        };

        return button;
    }

    private MenuFlyout BuildMenuFlyout()
    {
        var flyout = new MenuFlyout();

        var sizeSubMenu = new MenuFlyoutSubItem { Text = "调整大小" };
        sizeSubMenu.Items.Add(CreateSizeModeMenuItem("小", WidgetInstance.SizeModeSmall));
        sizeSubMenu.Items.Add(CreateSizeModeMenuItem("中", WidgetInstance.SizeModeMedium));
        sizeSubMenu.Items.Add(CreateSizeModeMenuItem("大", WidgetInstance.SizeModeLarge));
        sizeSubMenu.Items.Add(new MenuFlyoutSeparator());
        sizeSubMenu.Items.Add(CreateSizeModeMenuItem("自由调节（固定比例）", WidgetInstance.SizeModeFree));

        var colorSubMenu = new MenuFlyoutSubItem { Text = "修改组件颜色" };
        colorSubMenu.Items.Add(CreateColorMenuItem("跟随主题色", null));
        colorSubMenu.Items.Add(CreateColorMenuItem("海洋蓝", "#2563EB"));
        colorSubMenu.Items.Add(CreateColorMenuItem("薄荷绿", "#0D9488"));
        colorSubMenu.Items.Add(CreateColorMenuItem("落日橙", "#EA580C"));
        colorSubMenu.Items.Add(CreateColorMenuItem("玫瑰粉", "#DB2777"));

        var closeItem = new MenuFlyoutItem { Text = "关闭组件" };
        closeItem.Click += (_, _) => Close();

        flyout.Items.Add(sizeSubMenu);
        flyout.Items.Add(colorSubMenu);
        flyout.Items.Add(new MenuFlyoutSeparator());
        flyout.Items.Add(closeItem);

        return flyout;
    }

    private MenuFlyoutItem CreateSizeModeMenuItem(string text, string sizeMode)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => ApplySizeMode(sizeMode);
        return item;
    }

    private MenuFlyoutItem CreateColorMenuItem(string text, string? colorHex)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => SetTintColor(colorHex ?? GetThemeBrandHex());
        return item;
    }

    private void ResizeWidget(int width, int height, bool notifyChanged = true)
    {
        _widget.Width = width;
        _widget.Height = height;

        ApplyWidgetPlacement();

        if (notifyChanged)
        {
            WidgetChanged?.Invoke(_widget.Id);
        }
    }

    private void ApplySizeMode(string sizeMode)
    {
        _widget.SizeMode = WidgetSizingService.NormalizeSizeMode(sizeMode);
        ApplySizeModeToCurrentDisplay();
        ResizeWidget((int)_widget.Width, (int)_widget.Height, notifyChanged: false);
        WidgetChanged?.Invoke(_widget.Id);
    }

    private void ApplySizeModeToCurrentDisplay()
    {
        if (_appWindow is null)
        {
            return;
        }

        var workArea = GetWorkAreaBounds(_appWindow);
        var requestedWidth = Math.Max(1, (int)Math.Round(_widget.Width));
        var requestedHeight = Math.Max(1, (int)Math.Round(_widget.Height));

        var size = WidgetSizingService.IsPresetMode(_widget.SizeMode)
            ? WidgetSizingService.CalculatePresetSize(
                _widget.WidgetType,
                _widget.SizeMode,
                workArea.Width,
                workArea.Height)
            : WidgetSizingService.ConstrainSize(
                _widget.WidgetType,
                requestedWidth,
                requestedHeight,
                workArea.Width,
                workArea.Height,
                preferHeightSafety: true);

        _widget.Width = size.Width;
        _widget.Height = size.Height;
    }

    private static (int Width, int Height) GetWorkAreaBounds(AppWindow appWindow)
    {
        var workArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = workArea.Width > 0 ? workArea.Width : Math.Max(1, appWindow.Size.Width);
        var height = workArea.Height > 0 ? workArea.Height : Math.Max(1, appWindow.Size.Height);
        return (width, height);
    }

    private void SetTintColor(string colorHex)
    {
        _widget.TintColorHex = colorHex;
        ApplyTintToVisuals();
        WidgetChanged?.Invoke(_widget.Id);
    }

    private void EnsureWidgetTint()
    {
        if (!string.IsNullOrWhiteSpace(_widget.TintColorHex))
        {
            return;
        }

        _widget.TintColorHex = GetThemeBrandHex();
    }

    private void ApplyTintToVisuals()
    {
        var tintColor = ResolveTintColor();
        _outerCard.Background = CreateGlassBrush(tintColor);
        _innerCard.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(98, tintColor.R, tintColor.G, tintColor.B));
        _topGlow.Background = CreateTopGlowBrush(tintColor);
    }

    private Color ResolveTintColor()
    {
        return ParseHexColor(_widget.TintColorHex, Microsoft.UI.ColorHelper.FromArgb(255, 13, 93, 86));
    }

    private static string GetThemeBrandHex()
    {
        if (Application.Current is not App app)
        {
            return WidgetInstance.DefaultTintColorHex;
        }

        var hex = app.UiSettingsService.Current.BrandColorHex;
        return string.IsNullOrWhiteSpace(hex) ? WidgetInstance.DefaultTintColorHex : hex;
    }

    private static Brush CreateGlassBrush(Color tintColor)
    {
        var fallback = Blend(tintColor, Colors.Black, 0.62);

        try
        {
            return new AcrylicBrush
            {
                TintColor = tintColor,
                TintOpacity = 0.32,
                TintLuminosityOpacity = 0.14,
                FallbackColor = Microsoft.UI.ColorHelper.FromArgb(206, fallback.R, fallback.G, fallback.B)
            };
        }
        catch
        {
            return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(206, fallback.R, fallback.G, fallback.B));
        }
    }

    private static Brush CreateTopGlowBrush(Color tintColor)
    {
        var glow = Blend(tintColor, Colors.White, 0.68);
        return new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(0, 1),
            GradientStops =
            {
                new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(112, glow.R, glow.G, glow.B), Offset = 0 },
                new GradientStop { Color = Microsoft.UI.ColorHelper.FromArgb(10, glow.R, glow.G, glow.B), Offset = 1 }
            }
        };
    }

    private static Color ParseHexColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        var normalized = hex.Trim();
        if (normalized.StartsWith("#", StringComparison.Ordinal))
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

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        var inv = 1 - amount;

        var r = (byte)(from.R * inv + to.R * amount);
        var g = (byte)(from.G * inv + to.G * amount);
        var b = (byte)(from.B * inv + to.B * amount);

        return Color.FromArgb(255, r, g, b);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
