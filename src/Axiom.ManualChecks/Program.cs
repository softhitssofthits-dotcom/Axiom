using System.Globalization;
using System.Text;
using Axiom.Core;
using Axiom.Execution;
using Axiom.Runtime;

var root = FindRepositoryRoot();

return args.Length > 0 && string.Equals(args[0], "compare-engines", StringComparison.OrdinalIgnoreCase)
    ? RunEngineComparison(root, args[1..])
    : args.Length > 0 && string.Equals(args[0], "audit-bytecode-coverage", StringComparison.OrdinalIgnoreCase)
        ? RunBytecodeCoverageAudit(root, args[1..])
        : RunMathManualChecks(root, args);

static int RunMathManualChecks(string root, string[] args)
{
    var outputPath = ResolveOutputPath(root, args, "axiom-math-manual-check");
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

    var pipeline = new AxiomExecutionPipeline();
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
        var isOk = IsPositiveCaseMatch(check, result);

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

    WriteReport(outputPath, report.ToString());
    Console.WriteLine(outputPath);
    Console.WriteLine();
    Console.WriteLine(report.ToString());

    return failed == 0 ? 0 : 1;
}

static int RunEngineComparison(string root, string[] args)
{
    var options = ParseBenchmarkOptions(root, args);
    var benchmarks = GetBenchmarkCases()
        .Where(benchmark => options.Filter is null
            || benchmark.Category.Contains(options.Filter, StringComparison.OrdinalIgnoreCase)
            || benchmark.Name.Contains(options.Filter, StringComparison.OrdinalIgnoreCase)
            || benchmark.RelativePath.Contains(options.Filter, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (benchmarks.Length == 0)
    {
        Console.Error.WriteLine("No benchmark cases matched the provided filter.");
        return 1;
    }

    var report = new StringBuilder();
    var reportTime = DateTimeOffset.Now;
    var passed = 0;
    var failed = 0;

    report.AppendLine("Axiom Engine Comparison Report");
    report.AppendLine($"Generated: {reportTime:yyyy-MM-dd HH:mm:ss zzz}");
    report.AppendLine($"Repository: {root}");
    report.AppendLine($"Iterations per engine: {options.Iterations}");
    report.AppendLine($"Warmup iterations per engine: {options.WarmupIterations}");
    report.AppendLine("Measurement order: interleaved with alternating engine start");
    report.AppendLine();

    foreach (var benchmark in benchmarks)
    {
        var outcome = RunBenchmarkCase(root, benchmark, options);
        AppendBenchmarkReport(report, benchmark, outcome, options);

        if (outcome.IsOk)
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
    report.AppendLine($"Cases:   {benchmarks.Length}");
    report.AppendLine($"Passed:  {passed}");
    report.AppendLine($"Failed:  {failed}");
    report.AppendLine(failed == 0
        ? "Overall result: PASS"
        : "Overall result: FAIL");
    report.AppendLine();
    report.AppendLine("Interpretation");
    report.AppendLine("==============");
    report.AppendLine("A lower bytecode/interpreter ratio means the bytecode VM is faster on that benchmark.");
    report.AppendLine("Runtime ratios compare only execution time; total ratios include frontend preparation and lowering.");

    var reportText = report.ToString();
    WriteReport(options.OutputPath, reportText);
    Console.WriteLine(options.OutputPath);
    Console.WriteLine();
    Console.WriteLine(reportText);

    return failed == 0 ? 0 : 1;
}

static int RunBytecodeCoverageAudit(string root, string[] args)
{
    var outputPath = ResolveOutputPath(root, args, "axiom-bytecode-coverage-audit");
    return BytecodeCoverageAudit.Run(root, outputPath);
}

static BenchmarkOutcome RunBenchmarkCase(string root, BenchmarkCase benchmark, BenchmarkOptions options)
{
    var sourcePath = Path.Combine(root, benchmark.RelativePath);
    var sourceText = File.ReadAllText(sourcePath, Encoding.UTF8);

    WarmUpBenchmarkRuns(sourceText, benchmark, ExecutionMode.Interpreter, options.WarmupIterations);
    WarmUpBenchmarkRuns(sourceText, benchmark, ExecutionMode.Bytecode, options.WarmupIterations);

    ExecutionResult? interpreterBaseline = null;
    ExecutionResult? bytecodeBaseline = null;
    var interpreterRuntimeSamples = new List<double>(options.Iterations);
    var interpreterTotalSamples = new List<double>(options.Iterations);
    var bytecodeRuntimeSamples = new List<double>(options.Iterations);
    var bytecodeTotalSamples = new List<double>(options.Iterations);
    var interpreterStabilityIssue = string.Empty;
    var bytecodeStabilityIssue = string.Empty;

    for (var iteration = 0; iteration < options.Iterations; iteration++)
    {
        if (iteration % 2 == 0)
        {
            AddBenchmarkSample(
                ExecuteBenchmarkRun(sourceText, benchmark, ExecutionMode.Interpreter),
                "Interpreter",
                ref interpreterBaseline,
                interpreterRuntimeSamples,
                interpreterTotalSamples,
                ref interpreterStabilityIssue);
            AddBenchmarkSample(
                ExecuteBenchmarkRun(sourceText, benchmark, ExecutionMode.Bytecode),
                "Bytecode",
                ref bytecodeBaseline,
                bytecodeRuntimeSamples,
                bytecodeTotalSamples,
                ref bytecodeStabilityIssue);
        }
        else
        {
            AddBenchmarkSample(
                ExecuteBenchmarkRun(sourceText, benchmark, ExecutionMode.Bytecode),
                "Bytecode",
                ref bytecodeBaseline,
                bytecodeRuntimeSamples,
                bytecodeTotalSamples,
                ref bytecodeStabilityIssue);
            AddBenchmarkSample(
                ExecuteBenchmarkRun(sourceText, benchmark, ExecutionMode.Interpreter),
                "Interpreter",
                ref interpreterBaseline,
                interpreterRuntimeSamples,
                interpreterTotalSamples,
                ref interpreterStabilityIssue);
        }
    }

    var interpreterSummary = new BenchmarkExecutionRuns(
        interpreterBaseline ?? throw new InvalidOperationException("Expected at least one interpreter benchmark run."),
        CreateStats(interpreterRuntimeSamples),
        CreateStats(interpreterTotalSamples),
        interpreterStabilityIssue);
    var bytecodeSummary = new BenchmarkExecutionRuns(
        bytecodeBaseline ?? throw new InvalidOperationException("Expected at least one bytecode benchmark run."),
        CreateStats(bytecodeRuntimeSamples),
        CreateStats(bytecodeTotalSamples),
        bytecodeStabilityIssue);

    var issues = new List<string>();

    if (interpreterSummary.Result.Status != ExecutionStatus.Success)
    {
        issues.Add($"Interpreter finished with status {interpreterSummary.Result.Status}.");
    }

    if (bytecodeSummary.Result.Status != ExecutionStatus.Success)
    {
        issues.Add($"Bytecode finished with status {bytecodeSummary.Result.Status}.");
    }

    if (!string.Equals(interpreterSummary.StabilityIssue, string.Empty, StringComparison.Ordinal))
    {
        issues.Add(interpreterSummary.StabilityIssue);
    }

    if (!string.Equals(bytecodeSummary.StabilityIssue, string.Empty, StringComparison.Ordinal))
    {
        issues.Add(bytecodeSummary.StabilityIssue);
    }

    if (interpreterSummary.Result.Status == ExecutionStatus.Success
        && bytecodeSummary.Result.Status == ExecutionStatus.Success)
    {
        if (!interpreterSummary.Result.Output.SequenceEqual(bytecodeSummary.Result.Output, StringComparer.Ordinal))
        {
            issues.Add("Interpreter and bytecode produced different output.");
        }

        if (!AreDiagnosticsEqual(interpreterSummary.Result.Diagnostics, bytecodeSummary.Result.Diagnostics))
        {
            issues.Add("Interpreter and bytecode produced different diagnostics.");
        }
    }

    return new BenchmarkOutcome(
        interpreterSummary.Result,
        interpreterSummary.RuntimeStats,
        interpreterSummary.TotalStats,
        bytecodeSummary.Result,
        bytecodeSummary.RuntimeStats,
        bytecodeSummary.TotalStats,
        issues);
}

static void WarmUpBenchmarkRuns(string sourceText, BenchmarkCase benchmark, ExecutionMode mode, int warmupIterations)
{
    for (var iteration = 0; iteration < warmupIterations; iteration++)
    {
        _ = ExecuteBenchmarkRun(sourceText, benchmark, mode);
    }
}

static ExecutionResult ExecuteBenchmarkRun(string sourceText, BenchmarkCase benchmark, ExecutionMode mode)
{
    var pipeline = new AxiomExecutionPipeline();
    var inputProvider = benchmark.InputLines.Count == 0
        ? null
        : new QueueInputProvider(benchmark.InputLines);
    var runtimeRandom = benchmark.RuntimeRandomFactory?.Invoke();
    return pipeline.Run(new ExecutionRequest(sourceText, inputProvider: inputProvider, runtimeClock: benchmark.RuntimeClock, runtimeRandom: runtimeRandom, mode: mode));
}

static void AddBenchmarkSample(
    ExecutionResult result,
    string modeName,
    ref ExecutionResult? baseline,
    List<double> runtimeSamples,
    List<double> totalSamples,
    ref string stabilityIssue)
{
    baseline ??= result;

    runtimeSamples.Add(result.RuntimeExecutionDuration.TotalMilliseconds);
    totalSamples.Add(result.TotalDuration.TotalMilliseconds);

    if (stabilityIssue.Length > 0)
    {
        return;
    }

    if (result.Status != baseline.Status
        || !result.Output.SequenceEqual(baseline.Output, StringComparer.Ordinal)
        || !AreDiagnosticsEqual(result.Diagnostics, baseline.Diagnostics))
    {
        stabilityIssue = $"{modeName} produced unstable status, output, or diagnostics between measured iterations.";
    }
}

static BenchmarkStats CreateStats(IReadOnlyList<double> values)
{
    if (values.Count == 0)
    {
        throw new InvalidOperationException("Benchmark statistics require at least one sample.");
    }

    var ordered = values.OrderBy(static value => value).ToArray();
    var sum = values.Sum();
    var middle = ordered.Length / 2;
    var median = ordered.Length % 2 == 0
        ? (ordered[middle - 1] + ordered[middle]) / 2.0
        : ordered[middle];

    return new BenchmarkStats(
        values.Min(),
        sum / values.Count,
        median,
        values.Max());
}

static void AppendBenchmarkReport(StringBuilder report, BenchmarkCase benchmark, BenchmarkOutcome outcome, BenchmarkOptions options)
{
    report.AppendLine($"{(outcome.IsOk ? "[PASS]" : "[FAIL]")} {benchmark.Name}");
    report.AppendLine($"Category: {benchmark.Category}");
    report.AppendLine($"File: {benchmark.RelativePath}");
    report.AppendLine($"Iterations: {options.Iterations}");
    report.AppendLine($"Warmup iterations: {options.WarmupIterations}");
    report.AppendLine($"Interpreter engine: {outcome.InterpreterResult.ActualEngine?.ToString() ?? "<none>"}");
    report.AppendLine($"Bytecode engine:    {outcome.BytecodeResult.ActualEngine?.ToString() ?? "<none>"}");
    report.AppendLine($"Interpreter status: {outcome.InterpreterResult.Status}");
    report.AppendLine($"Bytecode status:    {outcome.BytecodeResult.Status}");
    report.AppendLine($"Interpreter runtime ms (min/avg/median/max): {FormatStats(outcome.InterpreterRuntime)}");
    report.AppendLine($"Bytecode runtime ms (min/avg/median/max):    {FormatStats(outcome.BytecodeRuntime)}");
    report.AppendLine($"Interpreter total ms   (min/avg/median/max): {FormatStats(outcome.InterpreterTotal)}");
    report.AppendLine($"Bytecode total ms      (min/avg/median/max): {FormatStats(outcome.BytecodeTotal)}");
    report.AppendLine($"Runtime ratio bytecode/interpreter: {FormatRatio(outcome.BytecodeRuntime.Average, outcome.InterpreterRuntime.Average)}");
    report.AppendLine($"Total ratio bytecode/interpreter:   {FormatRatio(outcome.BytecodeTotal.Average, outcome.InterpreterTotal.Average)}");
    report.AppendLine($"Output lines: {outcome.InterpreterResult.Output.Count}");
    report.AppendLine($"Diagnostics:  {FormatDiagnostics(outcome.InterpreterResult.Diagnostics)}");

    if (outcome.Issues.Count > 0)
    {
        report.AppendLine("Issues:");
        foreach (var issue in outcome.Issues)
        {
            report.AppendLine($"- {issue}");
        }
    }

    report.AppendLine();
}

static bool AreDiagnosticsEqual(IReadOnlyList<Diagnostic> left, IReadOnlyList<Diagnostic> right)
{
    if (left.Count != right.Count)
    {
        return false;
    }

    for (var index = 0; index < left.Count; index++)
    {
        var leftDiagnostic = left[index];
        var rightDiagnostic = right[index];

        if (!string.Equals(leftDiagnostic.Code, rightDiagnostic.Code, StringComparison.Ordinal)
            || !string.Equals(leftDiagnostic.Message, rightDiagnostic.Message, StringComparison.Ordinal)
            || leftDiagnostic.Location.Line != rightDiagnostic.Location.Line
            || leftDiagnostic.Location.Column != rightDiagnostic.Location.Column)
        {
            return false;
        }
    }

    return true;
}

static string FormatStats(BenchmarkStats stats)
{
    return $"{stats.Minimum:F1} / {stats.Average:F1} / {stats.Median:F1} / {stats.Maximum:F1}";
}

static string FormatRatio(double numerator, double denominator)
{
    if (denominator == 0)
    {
        return "n/a";
    }

    return $"{numerator / denominator:F3}x";
}

static BenchmarkOptions ParseBenchmarkOptions(string root, string[] args)
{
    string? filter = null;
    var iterations = 1;
    var warmupIterations = 1;
    var outputPath = ResolveOutputPath(root, args, "axiom-engine-compare");

    for (var index = 0; index < args.Length; index++)
    {
        if (string.Equals(args[index], "--iterations", StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out iterations) || iterations < 1)
            {
                throw new ArgumentException("Option --iterations requires a positive integer value.");
            }

            index++;
            continue;
        }

        if (string.Equals(args[index], "--filter", StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Option --filter requires a value.");
            }

            filter = args[index + 1];
            index++;
            continue;
        }

        if (string.Equals(args[index], "--warmup", StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out warmupIterations) || warmupIterations < 0)
            {
                throw new ArgumentException("Option --warmup requires a non-negative integer value.");
            }

            index++;
        }
    }

    return new BenchmarkOptions(outputPath, iterations, warmupIterations, filter);
}

static BenchmarkCase[] GetBenchmarkCases()
{
    return
    [
        new BenchmarkCase("Документ: строки и файлы", @"samples/smoke/engine-compare/document_batch_repair.axm"),
        new BenchmarkCase("Файлы: пакетный обход каталога", @"samples/smoke/engine-compare/file_batch_roundtrip.axm"),
        new BenchmarkCase("Файлы: потоковое чтение и запись", @"samples/smoke/engine-compare/open_file_stream_cycle.axm"),
        new BenchmarkCase("Файлы: path/info sweep", @"samples/smoke/engine-compare/file_path_metadata_sweep.axm"),
        new BenchmarkCase("Файлы: управление и roundtrip", @"samples/smoke/engine-compare/file_management_roundtrip.axm"),
        new BenchmarkCase("Файлы: диск и temp sweep", @"samples/smoke/engine-compare/drive_temp_storage_sweep.axm"),
        new BenchmarkCase("Регулярные выражения: журнал", @"samples/smoke/engine-compare/regex_log_analysis.axm"),
        new BenchmarkCase("Регекс: режимы и цепочка замен", @"samples/smoke/engine-compare/regex_modes_pipeline.axm"),
        new BenchmarkCase("Регекс: массивные результаты", @"samples/smoke/engine-compare/regex_array_result_mesh.axm"),
        new BenchmarkCase("Строки: примитивные операции", @"samples/smoke/engine-compare/string_primitive_scan.axm"),
        new BenchmarkCase("Строки: накопление конкатенацией", @"samples/smoke/engine-compare/string_concat_accumulator.axm"),
        new BenchmarkCase("Побитовый слой: сдвиги и маски", @"samples/smoke/engine-compare/bitwise_shift_mesh.axm"),
        new BenchmarkCase("Массивы и циклы: 3D сводка", @"samples/smoke/engine-compare/array_cube_rollup.axm"),
        new BenchmarkCase("Массивы: цепочка преобразований", @"samples/smoke/engine-compare/array_transform_chain.axm"),
        new BenchmarkCase("Массивы: shape-преобразования", @"samples/smoke/engine-compare/array_shape_transform_mesh.axm"),
        new BenchmarkCase("Массивы: callback-конвейер", @"samples/smoke/engine-compare/array_callback_pipeline.axm"),
        new BenchmarkCase("Массивы: поисковые built-in", @"samples/smoke/engine-compare/array_search_probe.axm"),
        new BenchmarkCase("Строковые массивы: циклы и дата-время", @"samples/smoke/engine-compare/string_array_loop_pipeline.axm"),
        new BenchmarkCase("Смешанная нагрузка: циклы, строки, regex, дата и SELECT CASE", @"samples/smoke/engine-compare/mixed_workload_pipeline.axm"),
        new BenchmarkCase("Скалярный слой и присваивания: expression mesh", @"samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm"),
        new BenchmarkCase("Числовые built-in: плотный проход", @"samples/smoke/engine-compare/numeric_function_sweep.axm"),
        new BenchmarkCase("RND: deterministic stream", @"samples/smoke/engine-compare/random_stream_snapshot.axm", Array.Empty<string>(), null, static () => new SeededRuntimeRandom(24681357)),
        new BenchmarkCase("Дата и время: календарный конвейер", @"samples/smoke/engine-compare/date_time_calendar_pipeline.axm"),
        new BenchmarkCase("Дата и время: current-time снимок", @"samples/smoke/engine-compare/current_time_snapshot.axm", Array.Empty<string>(), CreateCurrentTimeBenchmarkClock()),
        new BenchmarkCase("PRINT / PRINT USING: output gallery", @"samples/smoke/engine-compare/print_output_gallery.axm"),
        new BenchmarkCase("Управляющий слой: ветвления и циклы", @"samples/smoke/engine-compare/control_flow_branch_maze.axm"),
        new BenchmarkCase("Процедуры и замыкания: routine pipeline", @"samples/smoke/engine-compare/routine_closure_pipeline.axm"),
        new BenchmarkCase("Многомерный ранг: metadata-проверка", @"samples/smoke/engine-compare/high_rank_metadata.axm"),
        new BenchmarkCase("Пользовательский TYPE: records и поля", @"samples/smoke/engine-compare/type_record_roundtrip.axm"),
        new BenchmarkCase("Ввод: пакетный приём анкет", @"samples/smoke/engine-compare/input_record_intake.axm", BuildInputRecordIntakeLines())
    ];
}

static string[] BuildInputRecordIntakeLines()
{
    var lines = new string[180];

    for (var index = 0; index < lines.Length; index++)
    {
        var personNumber = index + 1;
        var name = $"USER{personNumber:000}";
        var age = 20 + (personNumber % 37);
        var score = 45 + ((personNumber * 7) % 51);
        lines[index] = $"{name}, {age}, {score}";
    }

    return lines;
}

static IRuntimeClock CreateCurrentTimeBenchmarkClock()
{
    var localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "Axiom Benchmark East",
        TimeSpan.FromHours(3),
        "Axiom Benchmark East",
        "Axiom Benchmark East");
    var namedTimeZones = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["Axiom Benchmark East"] = localTimeZone,
        ["UTC"] = TimeZoneInfo.Utc
    };
    return new FixedRuntimeClock(
        new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3)),
        localTimeZone,
        namedTimeZones);
}

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

static string ResolveOutputPath(string root, string[] args, string prefix)
{
    for (var index = 0; index < args.Length; index++)
    {
        if (string.Equals(args[index], "--out", StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Option --out requires a path.");
            }

            return Path.GetFullPath(args[index + 1]);
        }
    }

    return Path.Combine(root, ".artifacts", "manual-checks", $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
}

static void WriteReport(string outputPath, string reportText)
{
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, reportText, Encoding.UTF8);
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

static bool IsPositiveCaseMatch(PositiveCase check, ExecutionResult result)
{
    if (result.Status != ExecutionStatus.Success || result.Diagnostics.Count != 0)
    {
        return false;
    }

    if (string.Equals(check.Name, "RND smoke", StringComparison.Ordinal))
    {
        if (result.Output.Count != 3)
        {
            return false;
        }

        if (!string.Equals(result.Output[0], "TRUE", StringComparison.Ordinal)
            || !string.Equals(result.Output[1], "TRUE", StringComparison.Ordinal))
        {
            return false;
        }

        return double.TryParse(result.Output[2], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value)
            && value >= 0
            && value < 1;
    }

    return result.Output.SequenceEqual(check.ExpectedOutput, StringComparer.Ordinal);
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

sealed record BenchmarkCase(
    string Name,
    string RelativePath,
    IReadOnlyList<string> InputLines,
    IRuntimeClock? RuntimeClock,
    Func<IRuntimeRandom>? RuntimeRandomFactory)
{
    public BenchmarkCase(string name, string relativePath)
        : this(name, relativePath, Array.Empty<string>(), null, null)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines)
        : this(name, relativePath, inputLines, null, null)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines, IRuntimeClock? runtimeClock)
        : this(name, relativePath, inputLines, runtimeClock, null)
    {
    }

    public string Category => Name.Split(':', 2)[0].Trim();
}

sealed class QueueInputProvider : IInputProvider
{
    private readonly Queue<string> _values;

    public QueueInputProvider(IEnumerable<string> values)
    {
        _values = new Queue<string>(values);
    }

    public string ReadInput(InputRequest request) => _values.Count > 0 ? _values.Dequeue() : string.Empty;
}

sealed class FixedRuntimeClock : IRuntimeClock
{
    private readonly DateTimeOffset _localNow;
    private readonly TimeZoneInfo _localTimeZone;
    private readonly IReadOnlyDictionary<string, TimeZoneInfo> _namedTimeZones;

    public FixedRuntimeClock(
        DateTimeOffset localNow,
        TimeZoneInfo localTimeZone,
        IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null)
    {
        _localNow = localNow;
        _localTimeZone = localTimeZone;
        _namedTimeZones = namedTimeZones ?? new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
    }

    public DateTimeOffset GetLocalNow() => _localNow;

    public TimeZoneInfo GetLocalTimeZone() => _localTimeZone;

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
    {
        if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = _localTimeZone;
            return true;
        }

        if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Utc;
            return true;
        }

        return _namedTimeZones.TryGetValue(id, out timeZone!);
    }
}

sealed class SeededRuntimeRandom : IRuntimeRandom
{
    private readonly Random _random;

    public SeededRuntimeRandom(int seed)
    {
        _random = new Random(seed);
    }

    public double NextRandomDouble() => _random.NextDouble();
}

sealed record BenchmarkOptions(string OutputPath, int Iterations, int WarmupIterations, string? Filter);

sealed record BenchmarkStats(double Minimum, double Average, double Median, double Maximum);

sealed record BenchmarkExecutionRuns(
    ExecutionResult Result,
    BenchmarkStats RuntimeStats,
    BenchmarkStats TotalStats,
    string StabilityIssue);

sealed record BenchmarkOutcome(
    ExecutionResult InterpreterResult,
    BenchmarkStats InterpreterRuntime,
    BenchmarkStats InterpreterTotal,
    ExecutionResult BytecodeResult,
    BenchmarkStats BytecodeRuntime,
    BenchmarkStats BytecodeTotal,
    IReadOnlyList<string> Issues)
{
    public bool IsOk => Issues.Count == 0;
}

