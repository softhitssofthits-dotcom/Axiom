param([long]$WindowHandle, [string]$Action, [int]$Key, [int]$X, [int]$Y,
      [int]$Width, [int]$Height, [int]$Button = 1, [int]$Delta, [string]$OutputPath)
$ErrorActionPreference = 'Stop'
Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.Runtime.InteropServices;
public static class CanvasProbe {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct BitmapInfo {
        public uint Size; public int Width, Height; public ushort Planes, Bits;
        public uint Compression, ImageSize; public int XPels, YPels; public uint Used, Important;
    }
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint first, uint second, bool attach);
    [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wp, IntPtr lp, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dest, int x, int y, int w, int h, IntPtr source, int sx, int sy, uint op);
    [DllImport("gdi32.dll")] static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfo info, uint usage);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    public static void Message(IntPtr hwnd, uint message, int wp, int lp) {
        IntPtr result;
        if (SendMessageTimeout(hwnd, message, new IntPtr(wp), new IntPtr(lp), 2, 2000, out result) == IntPtr.Zero)
            throw new InvalidOperationException("Native CANVAS did not respond");
    }
    public static bool Focus(IntPtr hwnd) {
        uint pid; var current = GetCurrentThreadId();
        var foreground = GetWindowThreadProcessId(GetForegroundWindow(), out pid);
        var target = GetWindowThreadProcessId(hwnd, out pid);
        bool attachedForeground = foreground != current && AttachThreadInput(current, foreground, true);
        bool attachedTarget = target != current && target != foreground && AttachThreadInput(current, target, true);
        try { SetForegroundWindow(hwnd); SetFocus(hwnd); return GetForegroundWindow() == hwnd; }
        finally {
            if (attachedTarget) AttachThreadInput(current, target, false);
            if (attachedForeground) AttachThreadInput(current, foreground, false);
        }
    }
    public static byte[] Capture(IntPtr hwnd, int width, int height) {
        var source = GetDC(hwnd); var dc = CreateCompatibleDC(source);
        var bitmap = CreateCompatibleBitmap(source, width, height); var old = SelectObject(dc, bitmap);
        try {
            if (!BitBlt(dc, 0, 0, width, height, source, 0, 0, 0x00CC0020)) throw new InvalidOperationException("BitBlt failed");
            SelectObject(dc, old);
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
            var data = new byte[checked(width * height * 4)];
            if (GetDIBits(dc, bitmap, 0, (uint)height, data, ref info, 0) != height) throw new InvalidOperationException("GetDIBits failed");
            return data;
        } finally { SelectObject(dc, old); DeleteObject(bitmap); DeleteDC(dc); ReleaseDC(hwnd, source); }
    }
    public static void Save(byte[] data, int width, int height, string path) {
        for (int i=3; i<data.Length; i+=4) data[i]=255;
        using (var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
            var bits = bitmap.LockBits(new System.Drawing.Rectangle(0,0,width,height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try { Marshal.Copy(data, 0, bits.Scan0, data.Length); }
            finally { bitmap.UnlockBits(bits); }
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
'@
$hwnd = [IntPtr]::new($WindowHandle)
[void][CanvasProbe]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
if ($Action -eq 'exists') { [pscustomobject]@{ exists = [CanvasProbe]::IsWindow($hwnd) } | ConvertTo-Json -Compress; exit }
if (-not [CanvasProbe]::IsWindow($hwnd)) { throw 'Native CANVAS window not found' }
$rect = [CanvasProbe+Rect]::new()
[void][CanvasProbe]::GetClientRect($hwnd, [ref]$rect)
$position = (($Y -band 65535) -shl 16) -bor ($X -band 65535)
switch ($Action) {
    'capture' { [void][CanvasProbe]::Focus($hwnd); Start-Sleep -Milliseconds 40
        $data = [CanvasProbe]::Capture($hwnd, $rect.Right, $rect.Bottom)
        if ($OutputPath) { [CanvasProbe]::Save($data, $rect.Right, $rect.Bottom, $OutputPath) }
        [pscustomobject]@{ width=$rect.Right; height=$rect.Bottom; bgra=[Convert]::ToBase64String($data) } | ConvertTo-Json -Compress; exit }
    'down' { [CanvasProbe]::Message($hwnd, 0x100, $Key, 0) }
    'up' { [CanvasProbe]::Message($hwnd, 0x101, $Key, 0) }
    'char' { [CanvasProbe]::Message($hwnd, 0x102, $Key, 0) }
    'blur' { [CanvasProbe]::Message($hwnd, 8, 0, 0) }
    'focus' { [pscustomobject]@{ focused=[CanvasProbe]::Focus($hwnd) } | ConvertTo-Json -Compress; exit }
    'move' { [CanvasProbe]::Message($hwnd, 0x200, 0, $position) }
    'press' { $message = @(0x201,0x204,0x207)[$Button-1]; [CanvasProbe]::Message($hwnd, $message, 0, $position) }
    'release' { $message = @(0x202,0x205,0x208)[$Button-1]; [CanvasProbe]::Message($hwnd, $message, 0, $position) }
    'wheel' { $point = [CanvasProbe+Point]::new(); $point.X=$X; $point.Y=$Y
        [void][CanvasProbe]::ClientToScreen($hwnd, [ref]$point)
        $position = (($point.Y -band 65535) -shl 16) -bor ($point.X -band 65535)
        [CanvasProbe]::Message($hwnd, 0x20A, ($Delta -shl 16), $position) }
    'close' { [CanvasProbe]::Message($hwnd, 0x10, 0, 0) }
    'resize' { if (-not [CanvasProbe]::SetWindowPos($hwnd, [IntPtr]::Zero, 20, 20, $Width, $Height, 0x14)) { throw 'Resize failed' } }
    'size' { }
    'state' { [pscustomobject]@{ focused=([CanvasProbe]::GetForegroundWindow() -eq $hwnd) } | ConvertTo-Json -Compress; exit }
    default { throw 'Unknown native CANVAS action' }
}
[pscustomobject]@{ width=$rect.Right; height=$rect.Bottom; ok=$true } | ConvertTo-Json -Compress
