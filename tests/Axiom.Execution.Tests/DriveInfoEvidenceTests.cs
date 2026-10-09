using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Axiom.Execution.Tests;

public sealed class DriveInfoEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void LocalDriveMatchesIndependentWindowsInventory(ExecutionMode mode)
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        Assert.True(root.Length == 3 && char.IsAsciiLetter(root[0]) && root[1] == ':');
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add($"Get-CimInstance Win32_LogicalDisk -Filter \"DeviceID='{root[..2]}'\" | Select-Object FileSystem,DriveType,Size | ConvertTo-Json -Compress");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(30000));
        Assert.True(process.ExitCode == 0, error);
        using var data = JsonDocument.Parse(output);
        var size = data.RootElement.GetProperty("Size").GetInt64();
        var format = data.RootElement.GetProperty("FileSystem").GetString();
        var type = data.RootElement.GetProperty("DriveType").GetInt32() switch
        {
            2 => "Removable", 3 => "Fixed", 4 => "Network", 5 => "CDRom", 6 => "Ram", _ => "Unknown"
        };
        var path = root + "axiom-not-created-" + Guid.NewGuid().ToString("N") + "\\file.txt";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            PRINT DRIVENAME("{{path}}")
            PRINT DRIVEFORMAT("{{path}}")
            PRINT DRIVETYPE("{{path}}")
            PRINT TOTALSPACE("{{path}}")
            PRINT FREESPACE("{{path}}")
            PRINT TOTALFREESPACE("{{path}}")
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { root, format, type, size.ToString(CultureInfo.InvariantCulture) }, result.Output.Take(4));
        foreach (var value in result.Output.Skip(4)) Assert.InRange(long.Parse(value, CultureInfo.InvariantCulture), 0L, size);
        Assert.False(Path.Exists(path));
    }
}
