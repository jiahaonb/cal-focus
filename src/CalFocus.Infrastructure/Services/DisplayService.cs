using System.Runtime.InteropServices;
using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;

namespace CalFocus.Infrastructure.Services;

public sealed class DisplayService : IDisplayService
{
    public IReadOnlyList<DisplayProfile> GetDisplays()
    {
        var results = new List<DisplayProfile>();

        _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<MONITORINFOEX>()
            };

            if (!GetMonitorInfo(monitor, ref info))
            {
                return true;
            }

            results.Add(new DisplayProfile
            {
                DisplayId = info.szDevice.TrimEnd('\0'),
                IsPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0,
                DpiScale = 1.0,
                X = info.rcMonitor.Left,
                Y = info.rcMonitor.Top,
                Width = info.rcMonitor.Right - info.rcMonitor.Left,
                Height = info.rcMonitor.Bottom - info.rcMonitor.Top,
                WorkAreaX = info.rcWork.Left,
                WorkAreaY = info.rcWork.Top,
                WorkAreaWidth = info.rcWork.Right - info.rcWork.Left,
                WorkAreaHeight = info.rcWork.Bottom - info.rcWork.Top
            });

            return true;
        }, IntPtr.Zero);

        return results;
    }

    public DisplayProfile? GetPrimaryDisplay()
    {
        return GetDisplays().FirstOrDefault(x => x.IsPrimary) ?? GetDisplays().FirstOrDefault();
    }

    public DisplayProfile? GetDisplayForPoint(double x, double y)
    {
        return GetDisplays().FirstOrDefault(display =>
            x >= display.X && x < display.X + display.Width &&
            y >= display.Y && y < display.Y + display.Height);
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    private const uint MONITORINFOF_PRIMARY = 0x00000001;

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
