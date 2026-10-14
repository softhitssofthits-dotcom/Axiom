param([long]$WindowHandle,[uint32]$TargetProcessId,[int]$X,[int]$Y,[long]$SourceHandle,[int]$SourceX,[int]$SourceY)
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AxiomNativeDrag {
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
 public static void Drag(long handle,uint expected,int x,int y,long sourceHandle,int sourceX,int sourceY){
 var h=new IntPtr(handle);uint process;GetWindowThreadProcessId(h,out process);if(process!=expected)throw new Exception("Unexpected process");
 var prior=SetThreadDpiAwarenessContext(new IntPtr(-4));Point original;GetCursorPos(out original);
 try{var dpi=GetDpiForWindow(h)/96.0;var target=new Point{X=(int)(x*dpi),Y=(int)(y*dpi)};if(!ClientToScreen(h,ref target))throw new Exception("ClientToScreen failed");
 var source=new IntPtr(sourceHandle);GetWindowThreadProcessId(source,out process);if(process!=expected)throw new Exception("Unexpected source process");
 var start=new Point{X=sourceX,Y=sourceY};ClientToScreen(source,ref start);SetForegroundWindow(source);SetCursorPos(start.X,start.Y);mouse_event(2,0,0,0,UIntPtr.Zero);
 Console.WriteLine("ready");Console.Out.Flush();System.Threading.Thread.Sleep(1500);
 for(int i=1;i<=30;i++){SetCursorPos(start.X+(target.X-start.X)*i/30,start.Y+(target.Y-start.Y)*i/30);System.Threading.Thread.Sleep(35);}
 mouse_event(4,0,0,0,UIntPtr.Zero);System.Threading.Thread.Sleep(400);
 }finally{mouse_event(4,0,0,0,UIntPtr.Zero);SetCursorPos(original.X,original.Y);SetThreadDpiAwarenessContext(prior);}
 }
}
"@
[AxiomNativeDrag]::Drag($WindowHandle,$TargetProcessId,$X,$Y,$SourceHandle,$SourceX,$SourceY)
