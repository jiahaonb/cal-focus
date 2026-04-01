using CalFocus.App.Services;
using CalFocus.Core.Abstractions.Services;
using CalFocus.Infrastructure.Persistence.Json;
using CalFocus.Infrastructure.Persistence.Sqlite;
using CalFocus.Infrastructure.Services;
using CalFocus.Widgets.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace CalFocus.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const double MainWindowAspectRatio = 2.0;
        private const double MainWindowDefaultWorkAreaUsage = 0.72;
        private const double MainWindowMaxWidthWorkAreaUsage = 0.95;
        private const double MainWindowMaxHeightWorkAreaUsage = 0.92;
        private const int MinWindowWidth = 900;
        private const int MinWindowHeight = 450;

        private Window? _window;
        private NativeTrayIconService? _trayIconService;
        private WindowMinSizeHook? _mainWindowMinSizeHook;

        private readonly IAppDataPathService _appDataPathService;
        private readonly IDatabaseInitializer _databaseInitializer;
        private readonly IAppLogger _logger;
        private readonly StartupLaunchService _startupLaunchService;
        private readonly TrayNotificationPreferenceService _trayNotificationPreferenceService;

        public ScheduleBoardService ScheduleBoardService { get; }
        public WidgetHostService WidgetHostService { get; }
        public DesktopWidgetManager DesktopWidgetManager { get; }
        public UiSettingsService UiSettingsService { get; }
        public ProductivityService ProductivityService { get; }
        public Window? MainWindow => _window;

        public event Action<string>? NavigationRequested;

        public App()
        {
            InitializeComponent();

            _appDataPathService = new AppDataPathService();
            _databaseInitializer = new SqliteDatabaseInitializer(_appDataPathService);
            _logger = new FileAppLogger(_appDataPathService);
            _startupLaunchService = new StartupLaunchService();
            _trayNotificationPreferenceService = new TrayNotificationPreferenceService(_appDataPathService);
            UiSettingsService = new UiSettingsService(_appDataPathService);
            ProductivityService = new ProductivityService(_appDataPathService);
            ProductivityService.ReminderTriggered += OnReminderTriggered;
            
            // 注册 ScheduleRepository 并创建 ScheduleBoardService
            IScheduleRepository scheduleRepository = new ScheduleRepository(_appDataPathService);
            ScheduleBoardService = new ScheduleBoardService(scheduleRepository);

            var jsonStore = new JsonFileStore();
            var widgetLayoutService = new WidgetLayoutService(_appDataPathService, jsonStore);
            var displayService = new DisplayService();

            WidgetHostService = new WidgetHostService();
            DesktopWidgetManager = new DesktopWidgetManager(WidgetHostService, widgetLayoutService, displayService);

            UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                await _databaseInitializer.InitializeAsync();
                await DesktopWidgetManager.InitializeAsync();

                _window ??= new Window();
                _window.Closed -= OnMainWindowClosed;
                _window.Closed += OnMainWindowClosed;

                if (_window.Content is not Frame rootFrame)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    _window.Content = rootFrame;
                }

                _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
                _window.Activate();

                try
                {
                    UiSettingsService.LoadAndApply(Resources);
                }
                catch (Exception ex)
                {
                    _logger.LogError("应用 UI 设置加载失败，已回退默认外观。", ex);
                }

                ConfigureMainWindow();

                _trayIconService ??= new NativeTrayIconService(
                    OpenMainWindow,
                    OpenSettings,
                    ToggleAllWidgetsVisibility,
                    AreWidgetsVisible,
                    ToggleStartup,
                    IsStartupEnabled,
                    ToggleTrayNotifications,
                    IsTrayNotificationsEnabled,
                    ExitApplication);

                _logger.LogInfo("应用启动成功。托盘服务已初始化。");
            }
            catch (Exception ex)
            {
                _logger.LogError("应用启动失败。", ex);
                throw;
            }
        }

        private void OpenMainWindow()
        {
            _window?.DispatcherQueue.TryEnqueue(() => _window.Activate());
        }

        private void OpenSettings()
        {
            _window?.DispatcherQueue.TryEnqueue(() =>
            {
                _window.Activate();
                NavigationRequested?.Invoke("settings");
            });
        }

        public void RequestNavigation(string tag)
        {
            _window?.DispatcherQueue.TryEnqueue(() => NavigationRequested?.Invoke(tag));
        }

        public void ShowNotification(string title, string message)
        {
            _window?.DispatcherQueue.TryEnqueue(() => _trayIconService?.ShowSystemNotification(title, message));
        }

        private void ToggleAllWidgetsVisibility()
        {
            DesktopWidgetManager.ToggleAllVisibility();
        }

        private bool AreWidgetsVisible()
        {
            return DesktopWidgetManager.AreWidgetsVisible;
        }

        private bool ToggleStartup()
        {
            var enabled = _startupLaunchService.Toggle();
            _logger.LogInfo(enabled ? "开机启动已开启。" : "开机启动已关闭。");
            return enabled;
        }

        private bool IsStartupEnabled()
        {
            return _startupLaunchService.IsEnabled();
        }

        private bool ToggleTrayNotifications()
        {
            var enabled = _trayNotificationPreferenceService.Toggle();
            _logger.LogInfo(enabled ? "托盘气泡提示已开启。" : "托盘气泡提示已关闭。");
            return enabled;
        }

        private bool IsTrayNotificationsEnabled()
        {
            return _trayNotificationPreferenceService.IsEnabled();
        }

        private void OnReminderTriggered(ReminderTaskItem reminder)
        {
            ShowNotification("CalFocus 提醒", $"{reminder.Title} ({reminder.ReminderAt:HH:mm})");
        }

        private void ExitApplication()
        {
            _logger.LogInfo("用户通过托盘退出应用。");
            ProductivityService.Dispose();
            _mainWindowMinSizeHook?.Dispose();
            _mainWindowMinSizeHook = null;
            _trayIconService?.Dispose();
            Exit();
        }

        private void ConfigureMainWindow()
        {
            if (_window is null)
            {
                return;
            }

            _mainWindowMinSizeHook?.Dispose();
            _mainWindowMinSizeHook = WindowMinSizeHook.Attach(_window, MinWindowWidth, MinWindowHeight, MainWindowAspectRatio);

            var appWindow = TryGetAppWindow(_window);
            if (appWindow is null)
            {
                return;
            }

            var workArea = GetWorkArea(appWindow);
            var defaultSize = CalculateMainWindowDefaultSize(workArea);
            appWindow.Resize(defaultSize);

            var centeredPosition = GetCenteredPosition(workArea, defaultSize.Width, defaultSize.Height);
            appWindow.Move(centeredPosition);
        }

        public bool EnsureMainWindowMinWidth(int requestedWidth)
        {
            if (_window is null)
            {
                return false;
            }

            var appWindow = TryGetAppWindow(_window);
            if (appWindow is null)
            {
                return false;
            }

            var workArea = GetWorkArea(appWindow);
            var maxWidthByWorkArea = Math.Max(1, workArea.Width);
            var maxHeightByWorkArea = Math.Max(1, workArea.Height);

            var targetWidth = Math.Clamp(Math.Max(appWindow.Size.Width, requestedWidth), 1, maxWidthByWorkArea);
            var targetHeight = Math.Max(1, (int)Math.Round(targetWidth / MainWindowAspectRatio));

            // 高度优先避免溢出工作区，再反推宽度以维持 2:1。
            if (targetHeight > maxHeightByWorkArea)
            {
                targetHeight = maxHeightByWorkArea;
                targetWidth = Math.Max(1, (int)Math.Round(targetHeight * MainWindowAspectRatio));
            }

            if (targetWidth > maxWidthByWorkArea)
            {
                targetWidth = maxWidthByWorkArea;
                targetHeight = Math.Max(1, (int)Math.Round(targetWidth / MainWindowAspectRatio));
            }

            appWindow.Resize(new SizeInt32(targetWidth, targetHeight));
            return true;
        }

        private void OnMainWindowClosed(object sender, WindowEventArgs args)
        {
            _mainWindowMinSizeHook?.Dispose();
            _mainWindowMinSizeHook = null;
            _trayIconService?.Dispose();
        }

        private static AppWindow? TryGetAppWindow(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private static RectInt32 GetWorkArea(AppWindow appWindow)
        {
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            if (workArea.Width <= 0 || workArea.Height <= 0)
            {
                return new RectInt32(0, 0, Math.Max(1, appWindow.Size.Width), Math.Max(1, appWindow.Size.Height));
            }

            return workArea;
        }

        private static SizeInt32 CalculateMainWindowDefaultSize(RectInt32 workArea)
        {
            var hardMaxWidth = Math.Max(1, workArea.Width);
            var hardMaxHeight = Math.Max(1, workArea.Height);

            var maxWidth = Math.Clamp((int)Math.Round(hardMaxWidth * MainWindowMaxWidthWorkAreaUsage), 1, hardMaxWidth);
            var maxHeight = Math.Clamp((int)Math.Round(hardMaxHeight * MainWindowMaxHeightWorkAreaUsage), 1, hardMaxHeight);

            var preferredWidth = Math.Clamp((int)Math.Round(hardMaxWidth * MainWindowDefaultWorkAreaUsage), 1, maxWidth);
            var preferredHeight = Math.Max(1, (int)Math.Round(preferredWidth / MainWindowAspectRatio));

            if (preferredHeight > maxHeight)
            {
                preferredHeight = maxHeight;
                preferredWidth = Math.Max(1, (int)Math.Round(preferredHeight * MainWindowAspectRatio));
            }

            if (preferredWidth > maxWidth)
            {
                preferredWidth = maxWidth;
                preferredHeight = Math.Max(1, (int)Math.Round(preferredWidth / MainWindowAspectRatio));
            }

            var minWidth = Math.Min(MinWindowWidth, hardMaxWidth);
            var minHeight = Math.Min(MinWindowHeight, hardMaxHeight);

            if (preferredWidth < minWidth)
            {
                preferredWidth = minWidth;
                preferredHeight = Math.Max(1, (int)Math.Round(preferredWidth / MainWindowAspectRatio));
            }

            if (preferredHeight < minHeight)
            {
                preferredHeight = minHeight;
                preferredWidth = Math.Max(1, (int)Math.Round(preferredHeight * MainWindowAspectRatio));
            }

            if (preferredHeight > hardMaxHeight)
            {
                preferredHeight = hardMaxHeight;
                preferredWidth = Math.Max(1, (int)Math.Round(preferredHeight * MainWindowAspectRatio));
            }

            if (preferredWidth > hardMaxWidth)
            {
                preferredWidth = hardMaxWidth;
                preferredHeight = Math.Max(1, (int)Math.Round(preferredWidth / MainWindowAspectRatio));
            }

            preferredWidth = Math.Clamp(preferredWidth, 1, hardMaxWidth);
            preferredHeight = Math.Clamp(preferredHeight, 1, hardMaxHeight);

            return new SizeInt32(preferredWidth, preferredHeight);
        }

        private static PointInt32 GetCenteredPosition(RectInt32 workArea, int width, int height)
        {
            var x = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
            var y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);
            return new PointInt32(x, y);
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _logger.LogError("UI 线程未处理异常。", e.Exception);
        }

        private void OnDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _logger.LogError("应用域未处理异常。", ex);
            }
            else
            {
                _logger.LogError("应用域未处理异常（未知类型）。");
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError("任务未观察异常。", e.Exception);
            e.SetObserved();
        }

        private sealed class WindowMinSizeHook : IDisposable
        {
            private const int GwlWndProc = -4;
            private const uint WmGetMinMaxInfo = 0x0024;
            private const uint WmSizing = 0x0214;
            private const uint MonitorDefaultToNearest = 0x00000002;

            private readonly IntPtr _hwnd;
            private readonly int _minWidth;
            private readonly int _minHeight;
            private readonly double _aspectRatio;
            private readonly WindowProc _windowProc;
            private IntPtr _originalWindowProc;
            private bool _isDisposed;

            private WindowMinSizeHook(IntPtr hwnd, int minWidth, int minHeight, double aspectRatio)
            {
                _hwnd = hwnd;
                _minWidth = minWidth;
                _minHeight = minHeight;
                _aspectRatio = aspectRatio;
                _windowProc = WindowProcedure;
            }

            public static WindowMinSizeHook? Attach(Window window, int minWidth, int minHeight, double aspectRatio)
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                var hook = new WindowMinSizeHook(hwnd, minWidth, minHeight, aspectRatio);
                return hook.TryAttach() ? hook : null;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;

                if (_originalWindowProc != IntPtr.Zero)
                {
                    _ = SetWindowLongPtr(_hwnd, GwlWndProc, _originalWindowProc);
                    _originalWindowProc = IntPtr.Zero;
                }
            }

            private bool TryAttach()
            {
                var newWindowProc = Marshal.GetFunctionPointerForDelegate(_windowProc);
                _originalWindowProc = SetWindowLongPtr(_hwnd, GwlWndProc, newWindowProc);
                if (_originalWindowProc != IntPtr.Zero)
                {
                    return true;
                }

                return Marshal.GetLastWin32Error() == 0;
            }

            private IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
            {
                if (message == WmGetMinMaxInfo && lParam != IntPtr.Zero)
                {
                    var workArea = ResolveWorkArea();
                    var workAreaWidth = Math.Max(1, workArea.Right - workArea.Left);
                    var workAreaHeight = Math.Max(1, workArea.Bottom - workArea.Top);
                    var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                    minMaxInfo.MinTrackSize.X = Math.Max(minMaxInfo.MinTrackSize.X, Math.Min(_minWidth, workAreaWidth));
                    minMaxInfo.MinTrackSize.Y = Math.Max(minMaxInfo.MinTrackSize.Y, Math.Min(_minHeight, workAreaHeight));
                    minMaxInfo.MaxTrackSize.X = Math.Max(minMaxInfo.MinTrackSize.X, workAreaWidth);
                    minMaxInfo.MaxTrackSize.Y = Math.Max(minMaxInfo.MinTrackSize.Y, workAreaHeight);
                    Marshal.StructureToPtr(minMaxInfo, lParam, false);
                }

                if (message == WmSizing && lParam != IntPtr.Zero)
                {
                    var edge = (int)wParam;
                    var rect = Marshal.PtrToStructure<Rect>(lParam);
                    ApplyAspectRatio(ref rect, edge);
                    Marshal.StructureToPtr(rect, lParam, false);
                    return new IntPtr(1);
                }

                return CallWindowProc(_originalWindowProc, hwnd, message, wParam, lParam);
            }

            private void ApplyAspectRatio(ref Rect rect, int edge)
            {
                var width = Math.Max(1, rect.Right - rect.Left);
                var height = Math.Max(1, rect.Bottom - rect.Top);
                var workArea = ResolveWorkArea();
                var maxWidth = Math.Max(1, workArea.Right - workArea.Left);
                var maxHeight = Math.Max(1, workArea.Bottom - workArea.Top);

                var leftEdge = edge is 1 or 4 or 7;
                var rightEdge = edge is 2 or 5 or 8;
                var topEdge = edge is 3 or 4 or 5;
                var bottomEdge = edge is 6 or 7 or 8;

                if (!leftEdge && !rightEdge && !topEdge && !bottomEdge)
                {
                    return;
                }

                var targetWidth = width;
                var targetHeight = height;
                var useWidthAsDriver = true;

                if ((leftEdge || rightEdge) && !(topEdge || bottomEdge))
                {
                    targetWidth = Math.Max(width, _minWidth);
                    targetHeight = Math.Max(_minHeight, (int)Math.Round(targetWidth / _aspectRatio));
                    useWidthAsDriver = true;
                }
                else if ((topEdge || bottomEdge) && !(leftEdge || rightEdge))
                {
                    targetHeight = Math.Max(height, _minHeight);
                    targetWidth = Math.Max(_minWidth, (int)Math.Round(targetHeight * _aspectRatio));
                    useWidthAsDriver = false;
                }
                else
                {
                    var candidateHeightByWidth = Math.Max(_minHeight, (int)Math.Round(width / _aspectRatio));
                    var candidateWidthByHeight = Math.Max(_minWidth, (int)Math.Round(height * _aspectRatio));

                    var widthDelta = Math.Abs(width - candidateWidthByHeight);
                    var heightDelta = Math.Abs(height - candidateHeightByWidth);

                    if (heightDelta <= widthDelta)
                    {
                        targetWidth = Math.Max(width, _minWidth);
                        targetHeight = Math.Max(_minHeight, (int)Math.Round(targetWidth / _aspectRatio));
                        useWidthAsDriver = true;
                    }
                    else
                    {
                        targetHeight = Math.Max(height, _minHeight);
                        targetWidth = Math.Max(_minWidth, (int)Math.Round(targetHeight * _aspectRatio));
                        useWidthAsDriver = false;
                    }
                }

                NormalizeSize(ref targetWidth, ref targetHeight, useWidthAsDriver, maxWidth, maxHeight);

                if (leftEdge)
                {
                    rect.Left = rect.Right - targetWidth;
                }
                else
                {
                    rect.Right = rect.Left + targetWidth;
                }

                if (topEdge)
                {
                    rect.Top = rect.Bottom - targetHeight;
                }
                else
                {
                    rect.Bottom = rect.Top + targetHeight;
                }
            }

            private void NormalizeSize(ref int width, ref int height, bool widthAsDriver, int maxWidth, int maxHeight)
            {
                if (widthAsDriver)
                {
                    width = Math.Max(width, _minWidth);
                    height = Math.Max(_minHeight, (int)Math.Round(width / _aspectRatio));
                }
                else
                {
                    height = Math.Max(height, _minHeight);
                    width = Math.Max(_minWidth, (int)Math.Round(height * _aspectRatio));
                }

                // 高度优先保证不溢出，再反推宽度保持固定比例。
                if (height > maxHeight)
                {
                    height = maxHeight;
                    width = Math.Max(1, (int)Math.Round(height * _aspectRatio));
                }

                if (width > maxWidth)
                {
                    width = maxWidth;
                    height = Math.Max(1, (int)Math.Round(width / _aspectRatio));
                }

                var minWidth = Math.Min(_minWidth, maxWidth);
                var minHeight = Math.Min(_minHeight, maxHeight);

                if (width < minWidth)
                {
                    var candidateHeight = Math.Max(1, (int)Math.Round(minWidth / _aspectRatio));
                    if (candidateHeight <= maxHeight)
                    {
                        width = minWidth;
                        height = candidateHeight;
                    }
                }

                if (height < minHeight)
                {
                    var candidateWidth = Math.Max(1, (int)Math.Round(minHeight * _aspectRatio));
                    if (candidateWidth <= maxWidth)
                    {
                        height = minHeight;
                        width = candidateWidth;
                    }
                }

                width = Math.Clamp(width, 1, maxWidth);
                height = Math.Clamp(height, 1, maxHeight);
            }

            private Rect ResolveWorkArea()
            {
                var monitor = MonitorFromWindow(_hwnd, MonitorDefaultToNearest);
                if (monitor == IntPtr.Zero)
                {
                    return new Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
                }

                var info = new MonitorInfo
                {
                    CbSize = Marshal.SizeOf<MonitorInfo>()
                };

                if (!GetMonitorInfo(monitor, ref info))
                {
                    return new Rect { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
                }

                return info.WorkArea;
            }

            private delegate IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

            [StructLayout(LayoutKind.Sequential)]
            private struct Point
            {
                public int X;
                public int Y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MinMaxInfo
            {
                public Point Reserved;
                public Point MaxSize;
                public Point MaxPosition;
                public Point MinTrackSize;
                public Point MaxTrackSize;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MonitorInfo
            {
                public int CbSize;
                public Rect MonitorArea;
                public Rect WorkArea;
                public uint Flags;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct Rect
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            {
                if (IntPtr.Size == 8)
                {
                    return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
                }

                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
            }

            [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
            private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongW")]
            private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
            private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
        }
    }
}
