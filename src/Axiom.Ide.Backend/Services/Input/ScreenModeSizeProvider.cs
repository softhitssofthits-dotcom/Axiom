using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Axiom.Ide.Graphics;
using Axiom.Runtime;

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
        scale = Math.Min(scale, Math.Min(GraphicsResourceLimits.MaximumDimension / 16d,
            Math.Min(GraphicsResourceLimits.MaximumDimension / 9d, Math.Sqrt(GraphicsResourceLimits.MaximumPixels / 144d))));
        var width = Math.Max(1, (int)Math.Floor(16d * scale));
        var height = Math.Max(1, (int)Math.Floor(9d * scale));
        return (width, height);
    }

    // Рабочая область целевого монитора нужна не только для размера SCREEN 10,
    // но и для явного размещения HWND на этом мониторе.
    internal static bool TryGetTargetWorkArea(out int left, out int top, out int width, out int height)
    {
        if (TryGetTargetMonitorArea(out var rect))
        {
            left = rect.Left;
            top = rect.Top;
            width = rect.Right - rect.Left;
            height = rect.Bottom - rect.Top;
            return width > 0 && height > 0;
        }

        left = 0;
        top = 0;
        width = 0;
        height = 0;
        return false;
    }

    private static (int Width, int Height) ResolveScreen10Size()
    {
        return TryGetScreen10AvailableArea(out var area)
            ? ComputeScreen10BestFitSize(area.Width, area.Height)
            : (1280, 720);
    }

    private static bool TryGetScreen10AvailableArea(out ScreenArea area)
    {
        if (TryGetTargetMonitorArea(out var rect))
        {
            area = new ScreenArea(rect.Right - rect.Left, rect.Bottom - rect.Top);
            return true;
        }

        area = default;
        return false;
    }

    private static bool TryGetTargetMonitorArea(out NativeRect rect)
    {
        var monitor = ResolveTargetMonitor();
        if (monitor != IntPtr.Zero && TryGetMonitorRect(monitor, preferWorkArea: true, out rect))
        {
            return true;
        }

        if (monitor != IntPtr.Zero && TryGetMonitorRect(monitor, preferWorkArea: false, out rect))
        {
            return true;
        }

        rect = default;
        return false;
    }

    private static IntPtr ResolveTargetMonitor()
    {
        // DesktopHost не имеет WPF MainWindow. HWND передаётся самим Electron;
        // монитор определяется при каждом SCREEN 10, включая перенос окна IDE.
        if (long.TryParse(Environment.GetEnvironmentVariable("AXIOM_IDE_OWNER_HWND"), out var owner) && owner > 0 &&
            GetWindowRect((IntPtr)owner, out var rect))
        {
            return MonitorFromPoint(new NativePoint
            {
                X = (int)(((long)rect.Left + rect.Right) / 2),
                Y = (int)(((long)rect.Top + rect.Bottom) / 2)
            }, MonitorDefaultToPrimary);
        }
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

    private static bool TryGetMonitorRect(IntPtr monitor, bool preferWorkArea, out NativeRect rect)
    {
        var monitorInfo = new MonitorInfo
        {
            cbSize = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            rect = default;
            return false;
        }

        var candidate = preferWorkArea && HasPositiveArea(monitorInfo.WorkArea)
            ? monitorInfo.WorkArea
            : monitorInfo.MonitorArea;

        if (!HasPositiveArea(candidate))
        {
            rect = default;
            return false;
        }

        rect = candidate;
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
