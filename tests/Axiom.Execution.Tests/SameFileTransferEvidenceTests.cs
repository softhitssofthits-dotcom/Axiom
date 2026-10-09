using System.Diagnostics;

namespace Axiom.Execution.Tests;

public sealed class SameFileTransferEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "COPYFILE", "MOVEFILE" })
        foreach (var overwrite in new[] { false, true }) yield return [mode, operation, overwrite];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IdenticalPathNeverChangesBytes(ExecutionMode mode, string operation, bool overwrite)
    {
        var path = Path.GetTempFileName();
        try
        {
            byte[] bytes = [0, 0xFF, 0x80, 0xEF, 0xBB, 0xBF, 13, 10];
            File.WriteAllBytes(path, bytes);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{operation}}("{{path}}", "{{path}}", {{(overwrite ? "TRUE" : "FALSE")}})
                CATCH err
                    PRINT err.Code
                END TRY
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(operation == "COPYFILE" ? ["MB3007"] : new[] { "TRUE" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HardLinkedNamesNeverCorruptSharedBytes(ExecutionMode mode, string operation, bool overwrite)
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "axiom-hardlink-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.bin");
        var alias = Path.Combine(root, "alias.bin");
        try
        {
            File.WriteAllBytes(source, [1]);
            var start = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-Command");
            start.ArgumentList.Add($"New-Item -ItemType HardLink -Path '{alias.Replace("'", "''")}' -Target '{source.Replace("'", "''")}' -ErrorAction Stop | Out-Null");
            using (var process = Process.Start(start)!)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                Assert.True(process.WaitForExit(15000));
                Assert.True(process.ExitCode == 0, output + error);
            }
            byte[] bytes = [0, 0xFF, 0x80, 13, 10];
            File.WriteAllBytes(alias, bytes);
            // Запись через второе имя изменила первое: это один файл, а не копия.
            Assert.Equal(bytes, File.ReadAllBytes(source));
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{operation}}("{{source}}", "{{alias}}", {{(overwrite ? "TRUE" : "FALSE")}})
                CATCH err
                    PRINT err.Code
                END TRY
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            var successfulMove = operation == "MOVEFILE";
            Assert.Equal(successfulMove ? ["TRUE"] : new[] { "MB3007" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(alias));
            if (successfulMove) Assert.False(File.Exists(source));
            else Assert.Equal(bytes, File.ReadAllBytes(source));
        }
        finally { Directory.Delete(root, true); }
    }
}
