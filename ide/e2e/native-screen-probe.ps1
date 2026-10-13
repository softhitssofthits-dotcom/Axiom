param([long]$WindowHandle)
$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ScreenProbeNative {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam, uint flags, uint timeout, out IntPtr result);
}
'@
$hwnd = [IntPtr]::new($WindowHandle)
$original = [ScreenProbeNative+Rect]::new()
if (-not [ScreenProbeNative]::GetWindowRect($hwnd, [ref]$original)) { throw 'Окно SCREEN не найдено' }
$latencies = @()
try {
    for ($index = 0; $index -lt 12; $index++) {
        $width = 1500 + ($index % 3) * 100
        $height = 850 + ($index % 2) * 100
        $timer = [Diagnostics.Stopwatch]::StartNew()
        if (-not [ScreenProbeNative]::SetWindowPos($hwnd, [IntPtr]::Zero, 30 + $index * 3, 30 + $index * 2, $width, $height, 0x14)) {
            throw 'Не удалось переместить и изменить размер SCREEN'
        }
        $result = [IntPtr]::Zero
        if ([ScreenProbeNative]::SendMessageTimeout($hwnd, 0x000F, [IntPtr]::Zero, [IntPtr]::Zero, 2, 2000, [ref]$result) -eq [IntPtr]::Zero) {
            throw 'Окно SCREEN не ответило на перерисовку за две секунды'
        }
        $latencies += $timer.Elapsed.TotalMilliseconds
    }
} finally {
    [void][ScreenProbeNative]::SetWindowPos($hwnd, [IntPtr]::Zero, $original.Left, $original.Top,
        $original.Right - $original.Left, $original.Bottom - $original.Top, 0x14)
}
$sorted = @($latencies | Sort-Object)
[pscustomobject]@{ count = $latencies.Count; medianMs = $sorted[6]; maxMs = $sorted[-1]; samplesMs = $latencies } | ConvertTo-Json -Compress
