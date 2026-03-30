using CalFocus.App.Services;
using CalFocus.Core.Abstractions.Services;
using CalFocus.Infrastructure.Persistence.Json;
using CalFocus.Infrastructure.Persistence.Sqlite;
using CalFocus.Infrastructure.Services;
using CalFocus.Widgets.Services;
using Microsoft.UI.Xaml.Navigation;

namespace CalFocus.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private NativeTrayIconService? _trayIconService;

        private readonly IAppDataPathService _appDataPathService;
        private readonly IDatabaseInitializer _databaseInitializer;
        private readonly IAppLogger _logger;
        private readonly StartupLaunchService _startupLaunchService;
        private readonly TrayNotificationPreferenceService _trayNotificationPreferenceService;

        public ScheduleBoardService ScheduleBoardService { get; }
        public WidgetHostService WidgetHostService { get; }
        public DesktopWidgetManager DesktopWidgetManager { get; }

        public App()
        {
            InitializeComponent();

            _appDataPathService = new AppDataPathService();
            _databaseInitializer = new SqliteDatabaseInitializer(_appDataPathService);
            _logger = new FileAppLogger(_appDataPathService);
            _startupLaunchService = new StartupLaunchService();
            _trayNotificationPreferenceService = new TrayNotificationPreferenceService(_appDataPathService);
            
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
                _window.Closed += (_, _) => _trayIconService?.Dispose();

                if (_window.Content is not Frame rootFrame)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    _window.Content = rootFrame;
                }

                _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
                _window.Activate();

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
            OpenMainWindow();
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

        private void ExitApplication()
        {
            _logger.LogInfo("用户通过托盘退出应用。");
            _trayIconService?.Dispose();
            Exit();
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
    }
}
