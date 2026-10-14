param([long]$WindowHandle,[uint32]$TargetProcessId,[int]$X,[int]$Y,[int]$Delta=25,[int]$Count=1)
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AxiomNativeWheel {
 [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
 public static void Wheel(long handle,uint expected,int x,int y,int delta,int count){
 var h=new IntPtr(handle);uint process;GetWindowThreadProcessId(h,out process);if(process!=expected)throw new Exception("Unexpected process");
 var prior=SetThreadDpiAwarenessContext(new IntPtr(-4));Point original;GetCursorPos(out original);
 try{var dpi=GetDpiForWindow(h)/96.0;var target=new Point{X=(int)(x*dpi),Y=(int)(y*dpi)};ClientToScreen(h,ref target);SetForegroundWindow(h);SetCursorPos(target.X,target.Y);
 for(int i=0;i<count;i++){mouse_event(0x0800,0,0,unchecked((uint)delta),UIntPtr.Zero);System.Threading.Thread.Sleep(15);}
 }finally{SetCursorPos(original.X,original.Y);SetThreadDpiAwarenessContext(prior);}
 }
}
"@
[AxiomNativeWheel]::Wheel($WindowHandle,$TargetProcessId,$X,$Y,$Delta,$Count)
