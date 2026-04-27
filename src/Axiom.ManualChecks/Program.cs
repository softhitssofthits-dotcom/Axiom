using System.Text;
using Axiom.Core;
using Axiom.Execution;

var root = FindRepositoryRoot();
var outputPath = ResolveOutputPath(args);
var pipeline = new AxiomExecutionPipeline();

var positiveCases = new[]
{
    new PositiveCase("ABS smoke", @"samples/functions/abs/basic_usage.axm", ["5", "10.5"]),
    new PositiveCase("RND smoke", @"samples/functions/rnd/basic_usage.axm", ["TRUE", "TRUE"]),
    new PositiveCase("SGN smoke", @"samples/functions/sgn/basic_usage.axm", ["-1", "0", "1"]),
    new PositiveCase("MIN smoke", @"samples/functions/min/basic_usage.axm", ["3", "100"]),
    new PositiveCase("MAX smoke", @"samples/functions/max/basic_usage.axm", ["10", "21474836479"]),
    new PositiveCase("CLAMP smoke", @"samples/functions/clamp/basic_usage.axm", ["1", "5", "10"]),
    new PositiveCase("SIN smoke", @"samples/functions/sin/basic_usage.axm", ["0", "0.8414709848078965"]),
    new PositiveCase("COS smoke", @"samples/functions/cos/basic_usage.axm", ["1", "0.5403023058681398"]),
    new PositiveCase("TAN smoke", @"samples/functions/tan/basic_usage.axm", ["0", "0.5463024898437905"]),
    new PositiveCase("ATN smoke", @"samples/functions/atn/basic_usage.axm", ["0", "0.7853981633974483"]),
    new PositiveCase("ATN2 smoke", @"samples/functions/atn2/basic_usage.axm", ["0", "0.7853981633974483"]),
    new PositiveCase("SQR smoke", @"samples/functions/sqr/basic_usage.axm", ["3", "1.5"]),
    new PositiveCase("CBRT smoke", @"samples/functions/cbrt/basic_usage.axm", ["3", "-2"]),
    new PositiveCase("EXP smoke", @"samples/functions/exp/basic_usage.axm", ["1", "2.718281828459045"]),
    new PositiveCase("LOG smoke", @"samples/functions/log/basic_usage.axm", ["0", "1"]),
    new PositiveCase("LOG10 smoke", @"samples/functions/log10/basic_usage.axm", ["0", "3"]),
    new PositiveCase("LOG2 smoke", @"samples/functions/log2/basic_usage.axm", ["0", "3"]),
    new PositiveCase("POW smoke", @"samples/functions/pow/basic_usage.axm", ["8", "3"]),
    new PositiveCase("ASIN smoke", @"samples/functions/asin/basic_usage.axm", ["0", "524"]),
    new PositiveCase("ACOS smoke", @"samples/functions/acos/basic_usage.axm", ["0", "1047"]),
    new PositiveCase("ASINH smoke", @"samples/functions/asinh/basic_usage.axm", ["0", "881"]),
    new PositiveCase("ACOSH smoke", @"samples/functions/acosh/basic_usage.axm", ["0", "1317"]),
    new PositiveCase("ATANH smoke", @"samples/functions/atanh/basic_usage.axm", ["0", "549"]),
    new PositiveCase("SINH smoke", @"samples/functions/sinh/basic_usage.axm", ["0", "1175"]),
    new PositiveCase("COSH smoke", @"samples/functions/cosh/basic_usage.axm", ["1", "1543"]),
    new PositiveCase("TANH smoke", @"samples/functions/tanh/basic_usage.axm", ["0", "762"]),
    new PositiveCase("SETSIGN smoke", @"samples/functions/setsign/basic_usage.axm", ["-5", "5", "10.5"]),
    new PositiveCase("FLOOR smoke", @"samples/functions/floor/basic_usage.axm", ["3", "-4"]),
    new PositiveCase("CEIL smoke", @"samples/functions/ceil/basic_usage.axm", ["4", "-3"]),
    new PositiveCase("ROUND smoke", @"samples/functions/round/basic_usage.axm", ["3", "-3", "2"]),
    new PositiveCase("FIX and INT contract", @"samples/functions/fix/basic_usage.axm", ["3", "-3", "-4"])
};

var negativeCases = new[]
{
    NegativeCase.FromFile("SQR negative domain", @"samples/functions/sqr/negative_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("LOG non-positive domain", @"samples/functions/log/non_positive_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("CLAMP invalid bounds", @"samples/functions/clamp/invalid_bounds_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("POW invalid domain", @"samples/functions/pow/invalid_domain_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("ASIN out of range", @"samples/functions/asin/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("ACOS out of range", @"samples/functions/acos/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("ACOSH less than one", @"samples/functions/acosh/less_than_one_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromFile("ATANH out of range", @"samples/functions/atanh/out_of_range_error.axm", ExecutionStatus.RuntimeFailure, "MB3007"),
    NegativeCase.FromSource("CLAMP wrong arity", "PRINT CLAMP(1, 2)", ExecutionStatus.SemanticFailure, "MB2004"),
    NegativeCase.FromSource("CLAMP decimal and double mismatch", """
DIM value AS DECIMAL = 10.5
PRINT CLAMP(value, 0.1, 20)
""", ExecutionStatus.SemanticFailure, "MB2003"),
    NegativeCase.FromSource("POW wrong argument type", "PRINT POW(\"x\", 2)", ExecutionStatus.SemanticFailure, "MB2003"),
    NegativeCase.FromSource("SIN wrong arity", "PRINT SIN()", ExecutionStatus.SemanticFailure, "MB2004"),
    NegativeCase.FromSource("ROUND unexpected second argument", "PRINT ROUND(1, 2)", ExecutionStatus.SemanticFailure, "MB2004"),
    NegativeCase.FromSource("SETSIGN wrong argument type", "PRINT SETSIGN(10, \"x\")", ExecutionStatus.SemanticFailure, "MB2003")
};

var report = new StringBuilder();
var reportTime = DateTimeOffset.Now;
var passed = 0;
var failed = 0;

report.AppendLine("Axiom Math Manual Check Runner");
report.AppendLine($"Generated: {reportTime:yyyy-MM-dd HH:mm:ss zzz}");
report.AppendLine($"Repository: {root}");
report.AppendLine();
report.AppendLine("Positive checks");
report.AppendLine("===============");

foreach (var check in positiveCases)
{
    var result = RunFile(pipeline, root, check.RelativePath);
    var isOk = result.Status == ExecutionStatus.Success
        && result.Diagnostics.Count == 0
        && result.Output.SequenceEqual(check.ExpectedOutput, StringComparer.Ordinal);

    AppendCaseHeader(report, isOk, check.Name, check.RelativePath);
    report.AppendLine($"Expected status: {ExecutionStatus.Success}");
    report.AppendLine($"Actual status:   {result.Status}");
    report.AppendLine($"Expected output: {FormatValues(check.ExpectedOutput)}");
    report.AppendLine($"Actual output:   {FormatValues(result.Output)}");
    report.AppendLine($"Diagnostics:     {FormatDiagnostics(result.Diagnostics)}");
    report.AppendLine();

    if (isOk)
    {
        passed++;
    }
    else
    {
        failed++;
    }
}

report.AppendLine("Negative checks");
report.AppendLine("===============");

foreach (var check in negativeCases)
{
    var result = check.RelativePath is not null
        ? RunFile(pipeline, root, check.RelativePath)
        : RunSource(pipeline, check.SourceText!);
    var actualCode = result.Diagnostics.FirstOrDefault()?.Code ?? "<none>";
    var isOk = result.Status == check.ExpectedStatus
        && result.Diagnostics.Count == 1
        && string.Equals(actualCode, check.ExpectedDiagnosticCode, StringComparison.Ordinal);

    AppendCaseHeader(report, isOk, check.Name, check.RelativePath ?? "<inline>");
    report.AppendLine($"Expected status: {check.ExpectedStatus}");
    report.AppendLine($"Actual status:   {result.Status}");
    report.AppendLine($"Expected code:   {check.ExpectedDiagnosticCode}");
    report.AppendLine($"Actual code:     {actualCode}");
    report.AppendLine($"Output:          {FormatValues(result.Output)}");
    report.AppendLine($"Diagnostics:     {FormatDiagnostics(result.Diagnostics)}");
    report.AppendLine();

    if (isOk)
    {
        passed++;
    }
    else
    {
        failed++;
    }
}

report.AppendLine("Summary");
report.AppendLine("=======");
report.AppendLine($"Passed: {passed}");
report.AppendLine($"Failed: {failed}");
report.AppendLine(failed == 0
    ? "Overall result: PASS"
    : "Overall result: FAIL");
report.AppendLine();
report.AppendLine("Paste this report into the chat after running it.");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);

Console.WriteLine(outputPath);
Console.WriteLine();
Console.WriteLine(report.ToString());

return failed == 0 ? 0 : 1;

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing Axiom.sln.");
}

static string ResolveOutputPath(string[] args)
{
    if (args.Length >= 2 && string.Equals(args[0], "--out", StringComparison.OrdinalIgnoreCase))
    {
        return Path.GetFullPath(args[1]);
    }

    var fileName = $"axiom-math-manual-check-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
    return Path.Combine(Path.GetTempPath(), fileName);
}

static ExecutionResult RunFile(AxiomExecutionPipeline pipeline, string root, string relativePath)
{
    var sourcePath = Path.Combine(root, relativePath);
    var sourceText = File.ReadAllText(sourcePath, Encoding.UTF8);
    return pipeline.Run(new ExecutionRequest(sourceText));
}

static ExecutionResult RunSource(AxiomExecutionPipeline pipeline, string sourceText)
{
    return pipeline.Run(new ExecutionRequest(sourceText));
}

static void AppendCaseHeader(StringBuilder report, bool isOk, string name, string relativePath)
{
    report.AppendLine($"{(isOk ? "[PASS]" : "[FAIL]")} {name}");
    report.AppendLine($"File: {relativePath}");
}

static string FormatValues(IReadOnlyList<string> values)
{
    return values.Count == 0 ? "<empty>" : string.Join(" | ", values);
}

static string FormatDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
{
    if (diagnostics.Count == 0)
    {
        return "<none>";
    }

    return string.Join(
        " | ",
        diagnostics.Select(diagnostic =>
            $"{diagnostic.Code}@{diagnostic.Location.Line}:{diagnostic.Location.Column} {diagnostic.Message}"));
}

sealed record PositiveCase(string Name, string RelativePath, string[] ExpectedOutput);
sealed record NegativeCase(
    string Name,
    string? RelativePath,
    string? SourceText,
    ExecutionStatus ExpectedStatus,
    string ExpectedDiagnosticCode)
{
    public static NegativeCase FromFile(string name, string relativePath, ExecutionStatus expectedStatus, string expectedDiagnosticCode)
        => new(name, relativePath, null, expectedStatus, expectedDiagnosticCode);

    public static NegativeCase FromSource(string name, string sourceText, ExecutionStatus expectedStatus, string expectedDiagnosticCode)
        => new(name, null, sourceText, expectedStatus, expectedDiagnosticCode);
}
