using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Axiom.Ide.Graphics;

namespace Axiom.Ide.Services.Input;

internal sealed class ScreenModeSizeProvider : IScreenModeSizeProvider
{
    private const int MonitorDefaultToPrimary = 1;

    internal static ScreenModeSizeProvider Instance { get; } = new();

    private ScreenModeSizeProvider()
    {
    }

    public (int Width, int Height) GetSize(int mode) => mode switch
    {
        0 => (80 * ScreenTextRasterizer.CellWidth, 25 * ScreenTextRasterizer.CellHeight),
        1 => (320, 200),
        2 => (640, 480),
        3 => (800, 600),
        4 => (1024, 768),
        5 => (1280, 720),
        6 => (1366, 768),
        7 => (1600, 900),
        8 => (1920, 1080),
        9 => (2560, 1440),
        10 => ResolveScreen10Size(),
        _ => (0, 0)
    };

    internal static (int Width, int Height) ComputeScreen10BestFitSize(int availableWidth, int availableHeight)
    {
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return (1280, 720);
        }

        var scale = Math.Min(availableWidth / 16d, availableHeight / 9d);
        var width = Math.Max(1, (int)Math.Floor(16d * scale));
        var height = Math.Max(1, (int)Math.Floor(9d * scale));
        return (width, height);
    }

    private static (int Width, int Height) ResolveScreen10Size()
    {
        return TryGetScreen10AvailableArea(out var area)
            ? ComputeScreen10BestFitSize(area.Width, area.Height)
            : (1280, 720);
    }

    private static bool TryGetScreen10AvailableArea(out ScreenArea area)
    {
        var monitor = ResolveTargetMonitor();
        if (monitor != IntPtr.Zero && TryGetMonitorArea(monitor, preferWorkArea: true, out area))
        {
            return true;
        }

        if (monitor != IntPtr.Zero && TryGetMonitorArea(monitor, preferWorkArea: false, out area))
        {
            return true;
        }

        area = default;
        return false;
    }

    private static IntPtr ResolveTargetMonitor()
    {
        if (TryGetMainWindowCenter(out var center))
        {
            return MonitorFromPoint(center, MonitorDefaultToPrimary);
        }

        return MonitorFromPoint(default, MonitorDefaultToPrimary);
    }

    private static bool TryGetMainWindowCenter(out NativePoint center)
    {
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow is null)
        {
            center = default;
            return false;
        }

        var handle = new WindowInteropHelper(mainWindow).Handle;
        if (handle != IntPtr.Zero && GetWindowRect(handle, out var windowRect))
        {
            center = new NativePoint
            {
                X = windowRect.Left + ((windowRect.Right - windowRect.Left) / 2),
                Y = windowRect.Top + ((windowRect.Bottom - windowRect.Top) / 2)
            };
            return true;
        }

        if (!IsFinite(mainWindow.Left)
            || !IsFinite(mainWindow.Top)
            || !IsFinite(mainWindow.ActualWidth)
            || !IsFinite(mainWindow.ActualHeight)
            || mainWindow.ActualWidth <= 0
            || mainWindow.ActualHeight <= 0)
        {
            center = default;
            return false;
        }

        center = new NativePoint
        {
            X = (int)Math.Round(mainWindow.Left + (mainWindow.ActualWidth / 2d)),
            Y = (int)Math.Round(mainWindow.Top + (mainWindow.ActualHeight / 2d))
        };
        return true;
    }

    private static bool TryGetMonitorArea(IntPtr monitor, bool preferWorkArea, out ScreenArea area)
    {
        var monitorInfo = new MonitorInfo
        {
            cbSize = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            area = default;
            return false;
        }

        var candidate = preferWorkArea && HasPositiveArea(monitorInfo.WorkArea)
            ? monitorInfo.WorkArea
            : monitorInfo.MonitorArea;

        if (!HasPositiveArea(candidate))
        {
            area = default;
            return false;
        }

        area = new ScreenArea(candidate.Right - candidate.Left, candidate.Bottom - candidate.Top);
        return true;
    }

    private static bool HasPositiveArea(NativeRect rect) => rect.Right > rect.Left && rect.Bottom > rect.Top;

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public int Flags;
    }

    private readonly record struct ScreenArea(int Width, int Height);
}
