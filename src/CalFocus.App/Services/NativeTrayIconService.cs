using System.Runtime.InteropServices;

namespace CalFocus.App.Services;

public sealed class NativeTrayIconService : IDisposable
{
    private const int WM_APP = 0x8000;
    private const int WM_TRAYICON = WM_APP + 1;
    private const int WM_COMMAND = 0x0111;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_CONTEXTMENU = 0x007B;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;

    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_INFO = 0x00000010;

    private const int TPM_LEFTALIGN = 0x0000;
    private const int TPM_BOTTOMALIGN = 0x0020;
    private const int TPM_RIGHTBUTTON = 0x0002;

    private const uint NIIF_INFO = 0x00000001;

    private const int IDM_OPEN = 1001;
    private const int IDM_OPEN_SETTINGS = 1002;
    private const int IDM_TOGGLE_WIDGETS = 1003;
    private const int IDM_TOGGLE_STARTUP = 1004;
    private const int IDM_TOGGLE_TRAY_NOTIFICATIONS = 1005;
    private const int IDM_EXIT = 1006;

    private readonly Action _onOpenMainWindow;
    private readonly Action _onOpenSettings;
    private readonly Action _onToggleWidgets;
    private readonly Func<bool> _areWidgetsVisible;
    private readonly Func<bool> _onToggleStartup;
    private readonly Func<bool> _isStartupEnabled;
    private readonly Func<bool> _onToggleTrayNotifications;
    private readonly Func<bool> _isTrayNotificationsEnabled;
    private readonly Action _onExit;

    private readonly string _windowClassName = $"CalFocusTray_{Guid.NewGuid():N}";

    private readonly WndProc _wndProc;
    private IntPtr _windowHandle;
    private NOTIFYICONDATA _notifyIconData;
    private bool _disposed;

    public NativeTrayIconService(
        Action onOpenMainWindow,
        Action onOpenSettings,
        Action onToggleWidgets,
        Func<bool> areWidgetsVisible,
        Func<bool> onToggleStartup,
        Func<bool> isStartupEnabled,
        Func<bool> onToggleTrayNotifications,
        Func<bool> isTrayNotificationsEnabled,
        Action onExit)
    {
        _onOpenMainWindow = onOpenMainWindow;
        _onOpenSettings = onOpenSettings;
        _onToggleWidgets = onToggleWidgets;
        _areWidgetsVisible = areWidgetsVisible;
        _onToggleStartup = onToggleStartup;
        _isStartupEnabled = isStartupEnabled;
        _onToggleTrayNotifications = onToggleTrayNotifications;
        _isTrayNotificationsEnabled = isTrayNotificationsEnabled;
        _onExit = onExit;

        _wndProc = WindowProcedure;

        RegisterMessageWindow();
        CreateTrayIcon();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ = Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);

        if (_windowHandle != IntPtr.Zero)
        {
            _ = DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void ShowSystemNotification(string title, string message)
    {
        ShowInfoBalloon(title, message, force: true);
    }

    private void RegisterMessageWindow()
    {
        var instance = GetModuleHandle(null);
        var cls = new WNDCLASS
        {
            lpfnWndProc = _wndProc,
            hInstance = instance,
            lpszClassName = _windowClassName
        };

        _ = RegisterClass(ref cls);

        _windowHandle = CreateWindowEx(
            0,
            _windowClassName,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            instance,
            IntPtr.Zero);
    }

    private void CreateTrayIcon()
    {
        _notifyIconData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512),
            szTip = "Cal Focus"
        };

        _ = Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
    }

    private IntPtr WindowProcedure(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            var eventId = lParam.ToInt32();
            if (eventId == WM_LBUTTONDBLCLK)
            {
                _onOpenMainWindow();
                return IntPtr.Zero;
            }

            if (eventId == WM_RBUTTONUP || eventId == WM_CONTEXTMENU)
            {
                ShowContextMenu();
                return IntPtr.Zero;
            }
        }

        if (msg == WM_COMMAND)
        {
            var commandId = (ushort)(wParam.ToInt64() & 0xFFFF);

            switch (commandId)
            {
                case IDM_OPEN:
                    _onOpenMainWindow();
                    return IntPtr.Zero;
                case IDM_OPEN_SETTINGS:
                    _onOpenSettings();
                    return IntPtr.Zero;
                case IDM_TOGGLE_WIDGETS:
                    _onToggleWidgets();
                    ShowInfoBalloon(
                        "小组件状态",
                        _areWidgetsVisible() ? "已显示全部小组件" : "已隐藏全部小组件");
                    return IntPtr.Zero;
                case IDM_TOGGLE_STARTUP:
                    var startupEnabled = _onToggleStartup();
                    ShowInfoBalloon(
                        "开机启动",
                        startupEnabled ? "开机启动已开启" : "开机启动已关闭");
                    return IntPtr.Zero;
                case IDM_TOGGLE_TRAY_NOTIFICATIONS:
                    var notificationsEnabled = _onToggleTrayNotifications();
                    ShowInfoBalloon(
                        "托盘提醒",
                        notificationsEnabled ? "托盘气泡提示已开启" : "托盘气泡提示已关闭",
                        force: true);
                    return IntPtr.Zero;
                case IDM_EXIT:
                    _onExit();
                    return IntPtr.Zero;
            }
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var startupText = _isStartupEnabled() ? "开机启动：已开启" : "开机启动：已关闭";
        var widgetsText = _areWidgetsVisible() ? "隐藏全部小组件" : "显示全部小组件";
        var notificationText = _isTrayNotificationsEnabled() ? "托盘提醒：已开启" : "托盘提醒：已关闭";

        var menu = CreatePopupMenu();
        _ = AppendMenu(menu, 0, IDM_OPEN, "打开主界面");
        _ = AppendMenu(menu, 0, IDM_OPEN_SETTINGS, "设置");
        _ = AppendMenu(menu, 0, IDM_TOGGLE_WIDGETS, widgetsText);
        _ = AppendMenu(menu, 0, IDM_TOGGLE_STARTUP, startupText);
        _ = AppendMenu(menu, 0, IDM_TOGGLE_TRAY_NOTIFICATIONS, notificationText);
        _ = AppendMenu(menu, 0x0800, 0, string.Empty);
        _ = AppendMenu(menu, 0, IDM_EXIT, "退出");

        _ = GetCursorPos(out var point);
        _ = SetForegroundWindow(_windowHandle);

        _ = TrackPopupMenu(
            menu,
            TPM_LEFTALIGN | TPM_BOTTOMALIGN | TPM_RIGHTBUTTON,
            point.X,
            point.Y,
            0,
            _windowHandle,
            IntPtr.Zero);

        _ = DestroyMenu(menu);
    }

    private void ShowInfoBalloon(string title, string message, bool force = false)
    {
        if (!force && !_isTrayNotificationsEnabled())
        {
            return;
        }

        var balloon = _notifyIconData;
        balloon.uFlags = NIF_INFO;
        balloon.szInfoTitle = title;
        balloon.szInfo = message;
        balloon.dwInfoFlags = NIIF_INFO;

        _ = Shell_NotifyIcon(NIM_MODIFY, ref balloon);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass([In] ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, [In] ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool TrackPopupMenu(
        IntPtr hMenu,
        int uFlags,
        int x,
        int y,
        int nReserved,
        IntPtr hWnd,
        IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
