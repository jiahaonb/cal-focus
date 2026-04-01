using System.Runtime.InteropServices;
using CalFocus.Core.Domain.Entities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace CalFocus.App.Services;

public sealed class ScheduleWidgetWindow : Window
{
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    private readonly WidgetInstance _widget;
    private readonly Border _innerCard;
    private readonly Border _outerCard;
    private readonly Border _glow;
    private AppWindow? _appWindow;
    private bool _placementInitialized;
    private bool _chromeConfigured;

    public event Action<Guid>? WindowClosed;
    public event Action<Guid>? WidgetChanged;

    public ScheduleWidgetWindow(WidgetInstance widget)
    {
        _widget = widget;
        EnsureWidgetTint();

        var itemsPanel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                CreateAgendaRow("09:30", "项目站会"),
                CreateAgendaRow("14:00", "交互评审"),
                CreateAgendaRow("17:30", "每日复盘")
            }
        };

        _innerCard = new Border
        {
            CornerRadius = new CornerRadius(24),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(112, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(84, 32, 46, 60)),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "日程",
                        FontSize = 17,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(240, 244, 252, 255))
                    },
                    itemsPanel
                }
            }
        };

        _outerCard = new Border
        {
            CornerRadius = new CornerRadius(32),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(150, 255, 255, 255)),
            BorderThickness = new Thickness(1.2),
            Background = CreateGlassBrush(ResolveTintColor()),
            Padding = new Thickness(8),
            Child = _innerCard
        };

        _glow = new Border
        {
            Height = 42,
            CornerRadius = new CornerRadius(30, 30, 20, 20),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12, 8, 12, 0),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0))
        };

        var menuButton = CreateMenuButton();

        Content = new Grid
        {
            Margin = new Thickness(4),
            Children = { _outerCard, _glow, menuButton }
        };

        Title = "Cal Focus Schedule";

        Activated += OnActivated;
        Closed += OnClosed;
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

        ConfigureWindowChrome();

        _appWindow.Changed += OnAppWindowChanged;

        _placementInitialized = true;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
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
        sizeSubMenu.Items.Add(CreateSizeMenuItem("小 (320×200)", 320, 200));
        sizeSubMenu.Items.Add(CreateSizeMenuItem("中 (380×230)", 380, 230));
        sizeSubMenu.Items.Add(CreateSizeMenuItem("大 (460×280)", 460, 280));

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

    private MenuFlyoutItem CreateSizeMenuItem(string text, int width, int height)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => ResizeWidget(width, height);
        return item;
    }

    private MenuFlyoutItem CreateColorMenuItem(string text, string? colorHex)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => SetTintColor(colorHex ?? GetThemeBrandHex());
        return item;
    }

    private void ResizeWidget(int width, int height)
    {
        _widget.Width = width;
        _widget.Height = height;
        ApplyWidgetPlacement();
        WidgetChanged?.Invoke(_widget.Id);
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
        _glow.Background = CreateTopGlowBrush(tintColor);
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

    private static Border CreateAgendaRow(string time, string title)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var timeText = new TextBlock
        {
            Text = time,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(240, 255, 255, 255))
        };

        var titleText = new TextBlock
        {
            Text = title,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(225, 240, 250, 255))
        };

        Grid.SetColumn(titleText, 1);
        grid.Children.Add(timeText);
        grid.Children.Add(titleText);

        return new Border
        {
            CornerRadius = new CornerRadius(13),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(58, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(68, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 8, 10, 8),
            Child = grid
        };
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
