using System.ComponentModel;
using System.Runtime.InteropServices;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Graphics;

internal sealed class NativeGraphicsScreenWindow : IDisposable
{
    private const int CwUseDefault = unchecked((int)0x80000000);
    private const int SwShowNormal = 1;
    private const int WsOverlappedWindow = 0x00CF0000;
    private const int WsVisible = 0x10000000;
    private const int CsHRedraw = 0x0002;
    private const int CsVRedraw = 0x0001;
    private const int WmClose = 0x0010;
    private const int WmDestroy = 0x0002;
    private const int WmSize = 0x0005;
    private const int WmPaint = 0x000F;
    private const int WmMouseActivate = 0x0021;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmChar = 0x0102;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmMouseWheel = 0x020A;
    private const int MaActivate = 1;
    private const int SizeMinimized = 1;
    private const int ErrorClassAlreadyExists = 1410;

    private static readonly string WindowClassName = "AxiomNativeScreenWindow";
    private static readonly WndProc StaticWndProc = DispatchWindowMessage;
    private static ushort s_windowClassAtom;

    private readonly ProgramConsoleService _consoleService;
    private readonly Direct3D11SwapChainRenderer _renderer;
    private ScreenFramebufferSnapshot? _lastFrame;
    private bool _closed;

    public NativeGraphicsScreenWindow(ProgramConsoleService consoleService, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(consoleService);

        _consoleService = consoleService;
        RegisterWindowClass();

        var windowRect = new Rect
        {
            Left = 0,
            Top = 0,
            Right = Math.Max(1, width),
            Bottom = Math.Max(1, height)
        };
        AdjustWindowRectEx(ref windowRect, WsOverlappedWindow, false, 0);

        Hwnd = CreateWindowEx(
            0,
            WindowClassName,
            "Axiom SCREEN",
            WsOverlappedWindow,
            CwUseDefault,
            CwUseDefault,
            Math.Max(1, windowRect.Right - windowRect.Left),
            Math.Max(1, windowRect.Bottom - windowRect.Top),
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (Hwnd == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create native SCREEN HWND.");
        }

        SetWindowLongPtr(Hwnd, -21, GCHandle.ToIntPtr(GCHandle.Alloc(this)));
        var (clientWidth, clientHeight) = GetClientSize(Hwnd);
        _renderer = new Direct3D11SwapChainRenderer(Hwnd, clientWidth, clientHeight);
    }

    public event EventHandler? Closed;

    public IntPtr Hwnd { get; private set; }

    public void Show()
    {
        ShowWindow(Hwnd, SwShowNormal);
        UpdateWindow(Hwnd);
        SetFocus(Hwnd);
    }

    public void Activate()
    {
        SetForegroundWindow(Hwnd);
        SetFocus(Hwnd);
    }

    public void PresentFrame(ScreenFramebufferSnapshot? frame)
    {
        if (frame is null || _closed)
        {
            return;
        }

        _lastFrame = frame;
        _renderer.PresentFrame(frame);
    }

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        DestroyWindow(Hwnd);
    }

    public void Dispose()
    {
        Close();
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WmClose:
                DestroyWindow(hwnd);
                return IntPtr.Zero;

            case WmDestroy:
                HandleDestroyed();
                return IntPtr.Zero;

            case WmSize:
                if ((int)wParam != SizeMinimized)
                {
                    HandleResize(GetLowWord(lParam), GetHighWord(lParam));
                }

                return IntPtr.Zero;

            case WmPaint:
                HandlePaint();
                return IntPtr.Zero;

            case WmMouseActivate:
                SetFocus(Hwnd);
                return new IntPtr(MaActivate);

            case WmKeyDown:
                HandleKeyDown((int)wParam);
                return IntPtr.Zero;

            case WmKeyUp:
                HandleKeyUp((int)wParam);
                return IntPtr.Zero;

            case WmChar:
                HandleChar((char)(int)wParam);
                return IntPtr.Zero;

            case WmMouseMove:
                SetFocus(Hwnd);
                _consoleService.SetMousePosition(GetX(lParam), GetY(lParam));
                _consoleService.EnqueueMouseMove();
                return IntPtr.Zero;

            case WmLButtonDown:
                HandleMouseButton(lParam, 1, down: true);
                return IntPtr.Zero;

            case WmLButtonUp:
                HandleMouseButton(lParam, 1, down: false);
                return IntPtr.Zero;

            case WmRButtonDown:
                HandleMouseButton(lParam, 2, down: true);
                return IntPtr.Zero;

            case WmRButtonUp:
                HandleMouseButton(lParam, 2, down: false);
                return IntPtr.Zero;

            case WmMButtonDown:
                HandleMouseButton(lParam, 3, down: true);
                return IntPtr.Zero;

            case WmMButtonUp:
                HandleMouseButton(lParam, 3, down: false);
                return IntPtr.Zero;

            case WmMouseWheel:
                SetFocus(Hwnd);
                UpdateMousePositionFromScreenPoint(lParam);
                _consoleService.EnqueueMouseWheel(GetWheelDelta(wParam));
                return IntPtr.Zero;

            default:
                return DefWindowProc(hwnd, message, wParam, lParam);
        }
    }

    private void HandleKeyDown(int virtualKey)
    {
        if (TryMapVirtualKey(virtualKey, out var keyName))
        {
            _consoleService.SetKeyDown(keyName);
            if (!_consoleService.IsWaitingForInput && ShouldEnqueueKeyDown(virtualKey))
            {
                _consoleService.EnqueueKey(keyName);
            }
        }
    }

    private void HandleKeyUp(int virtualKey)
    {
        if (TryMapVirtualKey(virtualKey, out var keyName))
        {
            _consoleService.SetKeyUp(keyName);
        }
    }

    private void HandleChar(char character)
    {
        if (character == '\r')
        {
            if (_consoleService.IsWaitingForInput)
            {
                _consoleService.SubmitInput();
            }

            return;
        }

        if (character == '\b')
        {
            if (_consoleService.IsWaitingForInput)
            {
                _consoleService.BackspaceInputCharacter();
            }

            return;
        }

        if (_consoleService.IsWaitingForInput)
        {
            _consoleService.AppendInputCharacter(character);
        }
        else if (ShouldEnqueueChar(character))
        {
            _consoleService.EnqueueKey(character.ToString());
        }
    }

    private void HandleMouseButton(IntPtr lParam, int button, bool down)
    {
        SetFocus(Hwnd);
        _consoleService.SetMousePosition(GetX(lParam), GetY(lParam));
        if (down)
        {
            SetCapture(Hwnd);
            _consoleService.EnqueueMouseButtonPress(button);
            _consoleService.SetMouseButtonDown(button);
        }
        else
        {
            _consoleService.EnqueueMouseButtonRelease(button);
            _consoleService.SetMouseButtonUp(button);
            ReleaseCapture();
        }
    }

    private void UpdateMousePositionFromScreenPoint(IntPtr lParam)
    {
        var point = new Point
        {
            X = GetX(lParam),
            Y = GetY(lParam)
        };

        if (ScreenToClient(Hwnd, ref point))
        {
            _consoleService.SetMousePosition(point.X, point.Y);
        }
    }

    private void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _renderer.Resize(width, height);
        if (_lastFrame is not null)
        {
            _renderer.PresentFrame(_lastFrame);
        }
    }

    private void HandlePaint()
    {
        ValidateRect(Hwnd, IntPtr.Zero);
        if (_lastFrame is not null)
        {
            _renderer.PresentFrame(_lastFrame);
        }
    }

    private void HandleDestroyed()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _renderer.Dispose();
        var handle = GCHandle.FromIntPtr(GetWindowLongPtr(Hwnd, -21));
        if (handle.IsAllocated)
        {
            handle.Free();
        }

        Hwnd = IntPtr.Zero;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private static void RegisterWindowClass()
    {
        if (s_windowClassAtom != 0)
        {
            return;
        }

        var windowClass = new WndClass
        {
            style = CsHRedraw | CsVRedraw,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(StaticWndProc),
            hInstance = IntPtr.Zero,
            lpszClassName = WindowClassName
        };

        s_windowClassAtom = RegisterClass(ref windowClass);
        if (s_windowClassAtom == 0)
        {
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorClassAlreadyExists)
            {
                throw new Win32Exception(error, "Failed to register native SCREEN window class.");
            }

            s_windowClassAtom = 1;
        }
    }

    private static IntPtr DispatchWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        var userData = GetWindowLongPtr(hwnd, -21);
        if (userData != IntPtr.Zero)
        {
            var handle = GCHandle.FromIntPtr(userData);
            if (handle.Target is NativeGraphicsScreenWindow window)
            {
                return window.WindowProc(hwnd, message, wParam, lParam);
            }
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static bool TryMapVirtualKey(int virtualKey, out string keyName)
    {
        keyName = virtualKey switch
        {
            0x25 => "LEFT",
            0x26 => "UP",
            0x27 => "RIGHT",
            0x28 => "DOWN",
            0x1B => "ESC",
            0x0D => "ENTER",
            0x08 => "BACKSPACE",
            0x09 => "TAB",
            0x20 => "SPACE",
            0x2E => "DELETE",
            0x2D => "INSERT",
            0x24 => "HOME",
            0x23 => "END",
            0x21 => "PAGEUP",
            0x22 => "PAGEDOWN",
            >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
            >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
            >= 0x70 and <= 0x7B => $"F{virtualKey - 0x70 + 1}",
            _ => string.Empty
        };

        return keyName.Length != 0;
    }

    internal static bool ShouldEnqueueKeyDown(int virtualKey) =>
        virtualKey is not (>= 0x30 and <= 0x39) and not (>= 0x41 and <= 0x5A);

    internal static bool ShouldEnqueueChar(char character) =>
        !char.IsControl(character) && character != ' ';

    private static short GetX(IntPtr lParam) => unchecked((short)((long)lParam & 0xFFFF));

    private static short GetY(IntPtr lParam) => unchecked((short)(((long)lParam >> 16) & 0xFFFF));

    private static int GetWheelDelta(IntPtr wParam) => unchecked((short)(((long)wParam >> 16) & 0xFFFF));

    private static int GetLowWord(IntPtr value) => unchecked((short)((long)value & 0xFFFF));

    private static int GetHighWord(IntPtr value) => unchecked((short)(((long)value >> 16) & 0xFFFF));

    private static (int Width, int Height) GetClientSize(IntPtr hwnd)
    {
        if (!GetClientRect(hwnd, out var rect))
        {
            return (1, 1);
        }

        return (Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));
    }

    private delegate IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public int style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WndClass lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AdjustWindowRectEx(ref Rect lpRect, int dwStyle, bool bMenu, int dwExStyle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, value)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, value.ToInt32()));

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
}
