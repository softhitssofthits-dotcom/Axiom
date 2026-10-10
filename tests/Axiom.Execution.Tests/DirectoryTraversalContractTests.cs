using System.Diagnostics;

namespace Axiom.Execution.Tests;

public sealed class DirectoryTraversalContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopyDirectory_RejectsDescendantBeforeChangingFiles(bool extendedPath)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "copy-recheck-" + Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "child");
        var destinationArgument = extendedPath && OperatingSystem.IsWindows() ? @"\\?\" + destination : destination;
        Directory.CreateDirectory(destination);
        try
        {
            File.WriteAllText(Path.Combine(root, "a.dat"), "А");
            File.WriteAllText(Path.Combine(root, "b.dat"), "Б");
            // Конфликт второго файла останавливает старый алгоритм до рекурсии.
            File.WriteAllText(Path.Combine(destination, "b.dat"), "Сохранить");
            var source = $$"""
                TRY
                    PRINT COPYDIR("{{root}}", "{{destinationArgument}}")
                CATCH err
                    PRINT err.Code
                    PRINT INSTR(err.Message, "исход") > 0
                END TRY
                PRINT FILEEXISTS("{{destination}}\a.dat")
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(["MB3007", "TRUE", "FALSE"], result.Output);
            Assert.Equal("Сохранить", File.ReadAllText(Path.Combine(destination, "b.dat")));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void DirectorySizeAndCopy_DoNotFollowChildDirectoryLinks()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "directory-recheck-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var outside = Path.Combine(root, "outside");
        var destination = Path.Combine(root, "destination");
        var link = Path.Combine(source, "linked");
        var loop = Path.Combine(source, "loop");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outside);
        try
        {
            File.WriteAllBytes(Path.Combine(source, "own.dat"), new byte[3]);
            File.WriteAllBytes(Path.Combine(outside, "foreign.dat"), new byte[11]);
            CreateDirectoryLink(link, outside);
            CreateDirectoryLink(loop, source);
            var sourceText = $$"""
                PRINT DIRSIZE("{{source}}")
                PRINT COPYDIR("{{source}}", "{{destination}}")
                PRINT FILEEXISTS("{{destination}}\own.dat")
                PRINT DIREXISTS("{{destination}}\linked")
                PRINT DIREXISTS("{{destination}}\loop")
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(sourceText));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["3", "TRUE", "TRUE", "FALSE", "FALSE"], result.Output);
            Assert.Equal(new byte[11], File.ReadAllBytes(Path.Combine(outside, "foreign.dat")));
        }
        finally
        {
            if (Directory.Exists(link)) Directory.Delete(link);
            if (Directory.Exists(loop)) Directory.Delete(loop);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CopyDirectory_RejectsDescendantThroughDirectoryAlias()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "alias-recheck-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var alias = Path.Combine(root, "alias");
        Directory.CreateDirectory(source);
        try
        {
            File.WriteAllBytes(Path.Combine(source, "own.dat"), new byte[3]);
            CreateDirectoryLink(alias, source);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT COPYDIR("{{source}}", "{{alias}}\child", TRUE)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT DIREXISTS("{{source}}\child")
                PRINT COPYDIR("{{source}}", "{{root}}\source-extra")
                PRINT DIRSIZE("{{root}}\source-extra")
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(["MB3007", "FALSE", "TRUE", "3"], result.Output);
            Assert.Single(Directory.EnumerateFiles(source));
        }
        finally
        {
            if (Directory.Exists(alias)) Directory.Delete(alias);
            Directory.Delete(root, recursive: true);
        }
    }

    internal static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(link, target);
            return;
        }
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add($"New-Item -ItemType Junction -Path '{link.Replace("'", "''")}' -Target '{target.Replace("'", "''")}' -ErrorAction Stop | Out-Null");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(15000), "Создание ссылки не завершилось вовремя.");
        Assert.True(process.ExitCode == 0, output + error);
    }
}
