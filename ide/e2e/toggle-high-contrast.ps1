# Реальный системный сигнал для проверки Monaco; исходное состояние возвращается в finally.
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class AxiomHighContrastProbe {
 [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
 public struct State { public uint Size; public uint Flags; public IntPtr Scheme; }
 [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
 public static extern bool SystemParametersInfoW(uint action,uint size,ref State state,uint flags);
 public static State Read(){var value=new State();value.Size=(uint)Marshal.SizeOf<State>();if(!SystemParametersInfoW(0x42,value.Size,ref value,0))throw new System.ComponentModel.Win32Exception();return value;}
 public static void Set(ref State value){if(!SystemParametersInfoW(0x43,value.Size,ref value,2))throw new System.ComponentModel.Win32Exception();}
}
'@
$originalState = [AxiomHighContrastProbe]::Read()
$originalFlags = $originalState.Flags
$changedState = [AxiomHighContrastProbe]::Read()
$changedState.Flags = $changedState.Flags -bxor 1
try {
    [AxiomHighContrastProbe]::Set([ref]$changedState)
    Write-Output ('{"changed":true,"on":'+([bool]($changedState.Flags -band 1)).ToString().ToLowerInvariant()+'}')
    $readTask = [Console]::In.ReadLineAsync()
    $null = $readTask.Wait(20000)
} finally {
    $originalState.Flags = $originalFlags
    [AxiomHighContrastProbe]::Set([ref]$originalState)
    $restoredState = [AxiomHighContrastProbe]::Read()
    Write-Output ('{"restored":'+([bool]($restoredState.Flags -eq $originalState.Flags)).ToString().ToLowerInvariant()+'}')
}
