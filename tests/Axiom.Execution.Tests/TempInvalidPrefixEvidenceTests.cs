namespace Axiom.Execution.Tests;

public sealed class TempInvalidPrefixEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "MAKETEMPFILE", "MAKETEMPDIR" })
        foreach (var suffix in new[] { ".", "..", "/x", "\\x", ":", "*", "?", "\"", "|", "<", ">", "null", "newline" })
            yield return [mode, operation, suffix];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidPrefixIsCatchableBeforeObjectCreation(ExecutionMode mode, string operation, string suffix)
    {
        var prefix = "axiom_invalid_" + Guid.NewGuid().ToString("N") + "_";
        var argument = suffix switch
        {
            "." or ".." => $"\"{suffix}\"",
            "null" => $"\"{prefix}\" + CHRW(0)",
            "newline" => $"\"{prefix}\" + CHRW(10)",
            _ => "\"" + (prefix + suffix).Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
        };
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{operation}}({{argument}})
            CATCH err
                PRINT err.Code
            FINALLY
                PRINT "Завершено"
            END TRY
            PRINT "Продолжение"
            """, mode: mode));
        try
        {
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, result.Output);
            Assert.Empty(Directory.EnumerateFileSystemEntries(Path.GetTempPath(), prefix + "*"));
        }
        finally
        {
            // Удаляется только неожиданный результат с собственным уникальным префиксом.
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            foreach (var path in result.Output)
            {
                if (!Path.IsPathFullyQualified(path)) continue;
                if (!string.Equals(Path.GetDirectoryName(path), temp, StringComparison.OrdinalIgnoreCase)
                    || !Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(path)) Directory.Delete(path);
            }
        }
    }
}
