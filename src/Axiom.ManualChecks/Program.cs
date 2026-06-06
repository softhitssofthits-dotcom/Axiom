using System.Globalization;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Execution;
using Axiom.Runtime;

var root = FindRepositoryRoot();

return args.Length > 0 && string.Equals(args[0], "compare-engines", StringComparison.OrdinalIgnoreCase)
    ? RunEngineComparison(root, args[1..])
    : args.Length > 0 && string.Equals(args[0], "audit-bytecode-coverage", StringComparison.OrdinalIgnoreCase)
        ? RunBytecodeCoverageAudit(root, args[1..])
        : args.Length > 0 && string.Equals(args[0], "profile-bytecode-opcodes", StringComparison.OrdinalIgnoreCase)
            ? RunBytecodeOpcodeProfile(root, args[1..])
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

static int RunBytecodeOpcodeProfile(string root, string[] args)
{
    var outputPath = ResolveOutputPath(root, args, "axiom-bytecode-opcode-profile");
    string? filter = null;

    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], "--filter", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (index + 1 >= args.Length)
        {
            throw new ArgumentException("Option --filter requires a value.");
        }

        filter = args[index + 1];
        index++;
    }

    var benchmarks = GetBenchmarkCases()
        .Where(benchmark => filter is null
            || benchmark.Category.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || benchmark.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || benchmark.RelativePath.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (benchmarks.Length == 0)
    {
        Console.Error.WriteLine("No benchmark cases matched the provided filter.");
        return 1;
    }

    var report = new StringBuilder();
    var reportTime = DateTimeOffset.Now;
    var failed = 0;

    report.AppendLine("Axiom Bytecode Opcode Profile");
    report.AppendLine($"Generated: {reportTime:yyyy-MM-dd HH:mm:ss zzz}");
    report.AppendLine($"Repository: {root}");
    report.AppendLine();

    foreach (var benchmark in benchmarks)
    {
        var result = ProfileBytecodeBenchmark(root, benchmark);
        AppendOpcodeProfileReport(report, benchmark, result);
        if (!result.IsOk)
        {
            failed++;
        }
    }

    report.AppendLine("Summary");
    report.AppendLine("=======");
    report.AppendLine($"Cases:   {benchmarks.Length}");
    report.AppendLine($"Passed:  {benchmarks.Length - failed}");
    report.AppendLine($"Failed:  {failed}");
    report.AppendLine(failed == 0 ? "Overall result: PASS" : "Overall result: FAIL");

    var reportText = report.ToString();
    WriteReport(outputPath, reportText);
    Console.WriteLine(outputPath);
    Console.WriteLine();
    Console.WriteLine(reportText);

    return failed == 0 ? 0 : 1;
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
    IInputProvider? inputProvider = benchmark.UseGraphicsHost
        ? new BenchmarkGraphicsHost()
        : benchmark.InputLines.Count == 0
            ? null
            : new QueueInputProvider(benchmark.InputLines);
    var runtimeRandom = benchmark.RuntimeRandomFactory?.Invoke();
    return pipeline.Run(new ExecutionRequest(sourceText, inputProvider: inputProvider, runtimeClock: benchmark.RuntimeClock, runtimeRandom: runtimeRandom, mode: mode));
}

static OpcodeProfileResult ProfileBytecodeBenchmark(string root, BenchmarkCase benchmark)
{
    var sourcePath = Path.Combine(root, benchmark.RelativePath);
    var sourceText = File.ReadAllText(sourcePath, Encoding.UTF8);
    var pipeline = new AxiomExecutionPipeline();
    var compilation = pipeline.Compile(new CompilationRequest(sourceText));

    if (!compilation.FrontendSucceeded || !compilation.BytecodeAvailable || compilation.CompiledProgram?.BytecodeModule is null)
    {
        return OpcodeProfileResult.Failure(
            compilation.Status.ToString(),
            compilation.Diagnostics.Concat(compilation.BackendDiagnostics).ToArray());
    }

    IInputProvider inputProvider = benchmark.UseGraphicsHost
        ? new BenchmarkGraphicsHost()
        : benchmark.InputLines.Count == 0
            ? NullRuntimeHost.Instance
            : new QueueInputProvider(benchmark.InputLines);
    var runtimeClock = benchmark.RuntimeClock ?? NullRuntimeHost.Instance;
    var runtimeRandom = benchmark.RuntimeRandomFactory?.Invoke() ?? NullRuntimeHost.Instance;
    var host = new ProfilingRuntimeHost(inputProvider, runtimeClock, runtimeRandom);
    var module = compilation.CompiledProgram.BytecodeModule;
    var routinesById = module.Routines.ToDictionary(static routine => routine.Id);
    var builtinsById = module.Builtins.ToDictionary(static builtin => builtin.Id);
    var opcodeCounts = new Dictionary<Opcode, long>();
    var builtinCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    var routineCounts = new Dictionary<string, long>(StringComparer.Ordinal);
    var convertPatternCounts = new Dictionary<string, long>(StringComparer.Ordinal);

    var runtimeResult = BytecodeVm.Execute(
        module,
        host,
        instructionObserver: snapshot =>
        {
            IncrementCount(opcodeCounts, snapshot.Opcode);
            IncrementCount(routineCounts, snapshot.RoutineName);

            if (snapshot.Opcode != Opcode.CallBuiltin)
            {
                if (snapshot.Opcode == Opcode.Convert)
                {
                    var routine = routinesById[snapshot.RoutineId];
                    var instructions = routine.InstructionArray;
                    var currentInstruction = instructions[snapshot.InstructionIndex];
                    var targetType = (BytecodeScalarTypeCode)currentInstruction.GetOperand(0);
                    var previousOpcode = snapshot.InstructionIndex > 0
                        ? instructions[snapshot.InstructionIndex - 1].Opcode.ToString()
                        : "<start>";
                    var nextOpcode = snapshot.InstructionIndex + 1 < instructions.Length
                        ? instructions[snapshot.InstructionIndex + 1].Opcode.ToString()
                        : "<end>";
                    IncrementCount(convertPatternCounts, $"{previousOpcode} -> Convert({targetType}) -> {nextOpcode}");
                }

                return;
            }

            var instruction = routinesById[snapshot.RoutineId].InstructionArray[snapshot.InstructionIndex];
            var builtinId = instruction.GetOperand(0);
            if (builtinsById.TryGetValue(builtinId, out var builtin))
            {
                IncrementCount(builtinCounts, builtin.Name);
            }
        });

    return new OpcodeProfileResult(
        runtimeResult.Succeeded && !runtimeResult.Cancelled,
        runtimeResult.Diagnostics.ToArray(),
        host.Output,
        opcodeCounts,
        builtinCounts,
        routineCounts,
        convertPatternCounts);
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
        new BenchmarkCase("Современная графика: metadata image/sprite", @"samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm", useGraphicsHost: true),
        new BenchmarkCase("Современная графика: burst примитивов CANVAS", @"samples/smoke/engine-compare/modern_canvas_primitive_burst.axm", useGraphicsHost: true),
        new BenchmarkCase("Современная графика: burst изображений и спрайтов", @"samples/smoke/engine-compare/modern_canvas_sprite_burst.axm", useGraphicsHost: true),
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

static void AppendOpcodeProfileReport(StringBuilder report, BenchmarkCase benchmark, OpcodeProfileResult result)
{
    report.AppendLine($"{(result.IsOk ? "[PASS]" : "[FAIL]")} {benchmark.Name}");
    report.AppendLine($"Category: {benchmark.Category}");
    report.AppendLine($"File: {benchmark.RelativePath}");
    report.AppendLine($"Status: {(result.Completed ? "Success" : "Failure")}");
    report.AppendLine($"Output lines: {result.Output.Count}");
    report.AppendLine($"Diagnostics:  {FormatDiagnostics(result.Diagnostics)}");

    if (result.TotalInstructionCount > 0)
    {
        report.AppendLine($"Total instructions observed: {result.TotalInstructionCount}");
        report.AppendLine("Top opcodes:");
        AppendTopCounts(report, result.OpcodeCounts, result.TotalInstructionCount, 20);

        if (result.BuiltinCounts.Count > 0)
        {
            report.AppendLine("Top built-ins:");
            AppendTopCounts(report, result.BuiltinCounts, result.BuiltinCounts.Values.Sum(), 15);
        }

        if (result.RoutineCounts.Count > 0)
        {
            report.AppendLine("Top routines:");
            AppendTopCounts(report, result.RoutineCounts, result.RoutineCounts.Values.Sum(), 15);
        }

        if (result.ConvertPatternCounts.Count > 0)
        {
            report.AppendLine("Top convert patterns:");
            AppendTopCounts(report, result.ConvertPatternCounts, result.ConvertPatternCounts.Values.Sum(), 15);
        }
    }

    report.AppendLine();
}

static void AppendTopCounts<TKey>(StringBuilder report, IReadOnlyDictionary<TKey, long> counts, long totalCount, int top)
{
    foreach (var pair in counts
        .OrderByDescending(static pair => pair.Value)
        .ThenBy(static pair => pair.Key?.ToString(), StringComparer.Ordinal)
        .Take(top))
    {
        var share = totalCount == 0 ? 0d : (pair.Value * 100d) / totalCount;
        report.AppendLine($"  {pair.Key}: {pair.Value} ({share:F2}%)");
    }
}

static void IncrementCount<TKey>(Dictionary<TKey, long> counts, TKey key) where TKey : notnull
{
    counts.TryGetValue(key, out var count);
    counts[key] = count + 1;
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
    Func<IRuntimeRandom>? RuntimeRandomFactory,
    bool UseGraphicsHost)
{
    public BenchmarkCase(string name, string relativePath)
        : this(name, relativePath, Array.Empty<string>(), null, null, false)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines)
        : this(name, relativePath, inputLines, null, null, false)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines, IRuntimeClock? runtimeClock)
        : this(name, relativePath, inputLines, runtimeClock, null, false)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines, IRuntimeClock? runtimeClock, Func<IRuntimeRandom>? runtimeRandomFactory)
        : this(name, relativePath, inputLines, runtimeClock, runtimeRandomFactory, false)
    {
    }

    public BenchmarkCase(string name, string relativePath, bool useGraphicsHost)
        : this(name, relativePath, Array.Empty<string>(), null, null, useGraphicsHost)
    {
    }

    public BenchmarkCase(string name, string relativePath, IReadOnlyList<string> inputLines, IRuntimeClock? runtimeClock, bool useGraphicsHost)
        : this(name, relativePath, inputLines, runtimeClock, null, useGraphicsHost)
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

sealed class ProfilingRuntimeHost : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IRuntimeClock _runtimeClock;
    private readonly IRuntimeRandom _runtimeRandom;

    public ProfilingRuntimeHost(IInputProvider inputProvider, IRuntimeClock runtimeClock, IRuntimeRandom runtimeRandom)
    {
        _inputProvider = inputProvider;
        _runtimeClock = runtimeClock;
        _runtimeRandom = runtimeRandom;
    }

    public IReadOnlyList<string> Output => _output;

    public bool IsCancellationRequested => false;

    public int OutputCheckpoint => _output.Count;

    public string ReadInput(InputRequest request) => _inputProvider.ReadInput(request);

    public string ReadKey() =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider
            ? keyboardInputProvider.ReadKey()
            : string.Empty;

    public bool IsKeyDown(string keyText) =>
        _inputProvider is IKeyboardInputProvider keyboardInputProvider
        && keyboardInputProvider.IsKeyDown(keyText);

    public long GetMouseX() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseX()
            : -1;

    public long GetMouseY() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseY()
            : -1;

    public bool IsMouseButtonDown(int buttonNumber) =>
        _inputProvider is IMouseInputProvider mouseInputProvider
        && mouseInputProvider.IsMouseButtonDown(buttonNumber);

    public int ReadMouseButtonPress() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseButtonPress()
            : 0;

    public MouseInputEvent ReadMouseEvent() =>
        _inputProvider is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseEvent()
            : default;

    public void WriteOutput(string text)
    {
        _output.Add(text);
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > _output.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is out of range.");
        }

        _output.RemoveRange(checkpoint, _output.Count - checkpoint);
    }

    public DateTimeOffset GetLocalNow() => _runtimeClock.GetLocalNow();

    public TimeZoneInfo GetLocalTimeZone() => _runtimeClock.GetLocalTimeZone();

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone) => _runtimeClock.TryResolveTimeZone(id, out timeZone);

    public double NextRandomDouble() => _runtimeRandom.NextRandomDouble();

    public bool IsCanvasActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsCanvasActive;

    public bool IsScreenActive =>
        _inputProvider is IGraphicsHost graphicsHost && graphicsHost.IsScreenActive;

    public int ScreenWidth =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenWidth : 0;

    public int ScreenHeight =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.ScreenHeight : 0;

    public long CurrentGraphicsX =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsX : 0;

    public long CurrentGraphicsY =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.CurrentGraphicsY : 0;

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetGraphicsCurrentPosition(x, y);
        }
    }

    public void ActivateCanvas(long width, long height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ActivateCanvas(width, height);
        }
    }

    public void BeginFrame()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.BeginFrame();
        }
    }

    public void PresentFrame()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.PresentFrame();
        }
    }

    public void SetCanvasCamera(long x, long y)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.SetCanvasCamera(x, y);
        }
    }

    public void ResetCanvasCamera()
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ResetCanvasCamera();
        }
    }

    public void ClearFrame(int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.ClearFrame(color);
        }
    }

    public void DrawRect(long x, long y, long width, long height, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawRect(x, y, width, height, color);
        }
    }

    public void FillRect(long x, long y, long width, long height, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.FillRect(x, y, width, height, color);
        }
    }

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasLine(x1, y1, x2, y2, color);
        }
    }

    public void DrawCanvasCircle(long x, long y, long radius, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasCircle(x, y, radius, color);
        }
    }

    public void FillCanvasCircle(long x, long y, long radius, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.FillCanvasCircle(x, y, radius, color);
        }
    }

    public void DrawCanvasText(string text, long x, long y, int color)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawCanvasText(text, x, y, color);
        }
    }

    public RuntimeImageValue? LoadImage(string path) =>
        _inputProvider is IGraphicsHost graphicsHost ? graphicsHost.LoadImage(path) : null;

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawImage(image, x, y, width, height);
        }
    }

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
        if (_inputProvider is IGraphicsHost graphicsHost)
        {
            graphicsHost.DrawSprite(sprite, frame, x, y, width, height);
        }
    }
}

sealed class BenchmarkGraphicsHost : IInputProvider, IGraphicsHost
{
    private readonly Dictionary<string, RuntimeImageValue> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private long _currentGraphicsX;
    private long _currentGraphicsY;
    private long _cameraX;
    private long _cameraY;
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCanvasActive;
    private int[] _framePixels = [];
    private long _sink;

    public bool IsCanvasActive => _isCanvasActive;

    public int ScreenWidth => _screenWidth;

    public int ScreenHeight => _screenHeight;

    public long CurrentGraphicsX => _currentGraphicsX;

    public long CurrentGraphicsY => _currentGraphicsY;

    public string ReadInput(InputRequest request) => string.Empty;

    public void ActivateCanvas(long width, long height)
    {
        _isCanvasActive = true;
        _screenWidth = checked((int)width);
        _screenHeight = checked((int)height);
        _currentGraphicsX = 0;
        _currentGraphicsY = 0;
        _cameraX = 0;
        _cameraY = 0;
        _framePixels = new int[_screenWidth * _screenHeight];
        _sink = width + height;
    }

    public void BeginFrame()
    {
        _sink ^= 17;
    }

    public void PresentFrame()
    {
        _sink ^= 31;
    }

    public void SetCanvasCamera(long x, long y)
    {
        _cameraX = x;
        _cameraY = y;
        _sink += x + y;
    }

    public void ResetCanvasCamera()
    {
        _cameraX = 0;
        _cameraY = 0;
    }

    public void SetGraphicsCurrentPosition(long x, long y)
    {
        _currentGraphicsX = x;
        _currentGraphicsY = y;
    }

    public void ClearFrame(int color)
    {
        Array.Fill(_framePixels, color);
        _sink += color;
    }

    public void DrawRect(long x, long y, long width, long height, int color)
    {
        DrawRectangleCore(x, y, width, height, color, fill: false);
    }

    public void FillRect(long x, long y, long width, long height, int color)
    {
        DrawRectangleCore(x, y, width, height, color, fill: true);
    }

    public void DrawCanvasLine(long x1, long y1, long x2, long y2, int color)
    {
        PlotLine(TranslateX(x1), TranslateY(y1), TranslateX(x2), TranslateY(y2), color);
    }

    public void DrawCanvasCircle(long x, long y, long radius, int color)
    {
        DrawCircleCore(TranslateX(x), TranslateY(y), checked((int)radius), color, fill: false);
    }

    public void FillCanvasCircle(long x, long y, long radius, int color)
    {
        DrawCircleCore(TranslateX(x), TranslateY(y), checked((int)radius), color, fill: true);
    }

    public void DrawCanvasText(string text, long x, long y, int color)
    {
        var baseX = TranslateX(x);
        var baseY = TranslateY(y);
        for (var index = 0; index < text.Length; index++)
        {
            var glyphX = baseX + (index * 4);
            DrawRectangleCore(glyphX, baseY, 3, 5, color ^ text[index], fill: true);
        }
    }

    public RuntimeImageValue? LoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (_imageCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var hash = path.Aggregate(2166136261u, static (current, ch) => (current ^ ch) * 16777619u);
        var width = path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ? 32 : 64;
        var height = path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ? 32 : 64;
        var pixels = new uint[width * height];

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * width;
            for (var x = 0; x < width; x++)
            {
                var noise = unchecked(hash + (uint)(x * 31) + (uint)(y * 17));
                var red = (byte)(noise >> 16);
                var green = (byte)(noise >> 8);
                var blue = (byte)noise;
                var alpha = (byte)(((x + y) % 7) == 0 ? 0 : 255);
                pixels[rowOffset + x] = ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | blue;
            }
        }

        var image = new RuntimeImageValue(width, height, pixels);
        _imageCache[path] = image;
        _sink += width + height + hash;
        return image;
    }

    public RuntimeSpriteValue? LoadSprite(string path, int frameWidth, int frameHeight)
    {
        var image = LoadImage(path);
        return image is null ? null : new RuntimeSpriteValue(image, frameWidth, frameHeight);
    }

    public void DrawImage(RuntimeImageValue image, long x, long y, int width, int height)
    {
        DrawImageRegion(image, 0, 0, image.Width, image.Height, x, y, width, height);
    }

    public void DrawSprite(RuntimeSpriteValue sprite, int frame, long x, long y, int width, int height)
    {
        var sourceX = sprite.GetFrameLeft(frame);
        var sourceY = sprite.GetFrameTop(frame);
        DrawImageRegion(sprite.Image, sourceX, sourceY, sprite.FrameWidth, sprite.FrameHeight, x, y, width, height);
    }

    private void DrawRectangleCore(long x, long y, long width, long height, int color, bool fill)
    {
        var left = Math.Max(0, checked((int)TranslateX(x)));
        var top = Math.Max(0, checked((int)TranslateY(y)));
        var right = Math.Min(_screenWidth - 1, checked((int)(TranslateX(x) + width - 1)));
        var bottom = Math.Min(_screenHeight - 1, checked((int)(TranslateY(y) + height - 1)));

        if (left > right || top > bottom)
        {
            return;
        }

        if (fill)
        {
            for (var row = top; row <= bottom; row++)
            {
                var offset = row * _screenWidth;
                for (var column = left; column <= right; column++)
                {
                    _framePixels[offset + column] = color;
                }
            }
        }
        else
        {
            for (var column = left; column <= right; column++)
            {
                _framePixels[(top * _screenWidth) + column] = color;
                _framePixels[(bottom * _screenWidth) + column] = color;
            }

            for (var row = top; row <= bottom; row++)
            {
                _framePixels[(row * _screenWidth) + left] = color;
                _framePixels[(row * _screenWidth) + right] = color;
            }
        }

        _sink += color + left + top + right + bottom;
    }

    private void PlotLine(long x1, long y1, long x2, long y2, int color)
    {
        var startX = checked((int)x1);
        var startY = checked((int)y1);
        var endX = checked((int)x2);
        var endY = checked((int)y2);
        var deltaX = Math.Abs(endX - startX);
        var deltaY = Math.Abs(endY - startY);
        var stepX = startX < endX ? 1 : -1;
        var stepY = startY < endY ? 1 : -1;
        var error = deltaX - deltaY;

        while (true)
        {
            PlotPixel(startX, startY, color);
            if (startX == endX && startY == endY)
            {
                break;
            }

            var twiceError = error * 2;
            if (twiceError > -deltaY)
            {
                error -= deltaY;
                startX += stepX;
            }

            if (twiceError < deltaX)
            {
                error += deltaX;
                startY += stepY;
            }
        }
    }

    private void DrawCircleCore(long centerX, long centerY, int radius, int color, bool fill)
    {
        if (radius <= 0)
        {
            return;
        }

        var x = radius;
        var y = 0;
        var error = 1 - radius;

        while (x >= y)
        {
            if (fill)
            {
                DrawHorizontalSpan(centerX - x, centerX + x, centerY + y, color);
                DrawHorizontalSpan(centerX - y, centerX + y, centerY + x, color);
                DrawHorizontalSpan(centerX - x, centerX + x, centerY - y, color);
                DrawHorizontalSpan(centerX - y, centerX + y, centerY - x, color);
            }
            else
            {
                PlotCirclePoints(centerX, centerY, x, y, color);
            }

            y++;
            if (error < 0)
            {
                error += (2 * y) + 1;
            }
            else
            {
                x--;
                error += 2 * (y - x) + 1;
            }
        }
    }

    private void PlotCirclePoints(long centerX, long centerY, int x, int y, int color)
    {
        PlotPixel(checked((int)(centerX + x)), checked((int)(centerY + y)), color);
        PlotPixel(checked((int)(centerX - x)), checked((int)(centerY + y)), color);
        PlotPixel(checked((int)(centerX + x)), checked((int)(centerY - y)), color);
        PlotPixel(checked((int)(centerX - x)), checked((int)(centerY - y)), color);
        PlotPixel(checked((int)(centerX + y)), checked((int)(centerY + x)), color);
        PlotPixel(checked((int)(centerX - y)), checked((int)(centerY + x)), color);
        PlotPixel(checked((int)(centerX + y)), checked((int)(centerY - x)), color);
        PlotPixel(checked((int)(centerX - y)), checked((int)(centerY - x)), color);
    }

    private void DrawHorizontalSpan(long x1, long x2, long y, int color)
    {
        var row = checked((int)y);
        if (row < 0 || row >= _screenHeight)
        {
            return;
        }

        var left = Math.Max(0, checked((int)Math.Min(x1, x2)));
        var right = Math.Min(_screenWidth - 1, checked((int)Math.Max(x1, x2)));
        var offset = row * _screenWidth;
        for (var column = left; column <= right; column++)
        {
            _framePixels[offset + column] = color;
        }

        _sink += right - left + color;
    }

    private void PlotPixel(int x, int y, int color)
    {
        if ((uint)x >= (uint)_screenWidth || (uint)y >= (uint)_screenHeight)
        {
            return;
        }

        _framePixels[(y * _screenWidth) + x] = color;
        _sink += color + x + y;
    }

    private void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight, long targetX, long targetY, int targetWidth, int targetHeight)
    {
        var left = checked((int)TranslateX(targetX));
        var top = checked((int)TranslateY(targetY));
        var right = left + targetWidth;
        var bottom = top + targetHeight;
        var clipLeft = Math.Max(0, left);
        var clipTop = Math.Max(0, top);
        var clipRight = Math.Min(_screenWidth, right);
        var clipBottom = Math.Min(_screenHeight, bottom);

        if (clipLeft >= clipRight || clipTop >= clipBottom)
        {
            return;
        }

        var pixels = image.BgraPixels;
        if (targetWidth == sourceWidth && targetHeight == sourceHeight)
        {
            var sourceRowStart = sourceY + (clipTop - top);
            var sourceColumnStart = sourceX + (clipLeft - left);
            for (var screenY = clipTop; screenY < clipBottom; screenY++)
            {
                var sourceOffset = sourceRowStart * image.Width;
                var screenOffset = screenY * _screenWidth;
                var sourceColumn = sourceColumnStart;

                for (var screenX = clipLeft; screenX < clipRight; screenX++, sourceColumn++)
                {
                    var pixel = pixels[sourceOffset + sourceColumn];
                    if ((pixel & 0xFF000000u) == 0)
                    {
                        continue;
                    }

                    _framePixels[screenOffset + screenX] = unchecked((int)(pixel & 0x00FFFFFFu));
                    _sink += pixel + (uint)screenX + (uint)screenY;
                }

                sourceRowStart++;
            }

            return;
        }

        const int fractionBits = 16;
        var sourceRowFixed = (((long)(clipTop - top) * sourceHeight) << fractionBits) / targetHeight;
        var sourceColumnFixedStart = (((long)(clipLeft - left) * sourceWidth) << fractionBits) / targetWidth;
        var sourceRowStep = ((long)sourceHeight << fractionBits) / targetHeight;
        var sourceColumnStep = ((long)sourceWidth << fractionBits) / targetWidth;
        for (var screenY = clipTop; screenY < clipBottom; screenY++)
        {
            var sourceRow = sourceY + (int)(sourceRowFixed >> fractionBits);
            var screenOffset = screenY * _screenWidth;
            var sourceOffset = sourceRow * image.Width;
            var sourceColumnFixed = sourceColumnFixedStart;

            for (var screenX = clipLeft; screenX < clipRight; screenX++)
            {
                var sourceColumn = sourceX + (int)(sourceColumnFixed >> fractionBits);
                var pixel = pixels[sourceOffset + sourceColumn];
                if ((pixel & 0xFF000000u) == 0)
                {
                    sourceColumnFixed += sourceColumnStep;
                    continue;
                }

                _framePixels[screenOffset + screenX] = unchecked((int)(pixel & 0x00FFFFFFu));
                _sink += pixel + (uint)screenX + (uint)screenY;
                sourceColumnFixed += sourceColumnStep;
            }

            sourceRowFixed += sourceRowStep;
        }
    }

    private long TranslateX(long x) => x - _cameraX;

    private long TranslateY(long y) => y - _cameraY;
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

sealed record OpcodeProfileResult(
    bool Completed,
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyList<string> Output,
    IReadOnlyDictionary<Opcode, long> OpcodeCounts,
    IReadOnlyDictionary<string, long> BuiltinCounts,
    IReadOnlyDictionary<string, long> RoutineCounts,
    IReadOnlyDictionary<string, long> ConvertPatternCounts)
{
    public bool IsOk => Completed && Diagnostics.Count == 0;

    public long TotalInstructionCount => OpcodeCounts.Values.Sum();

    public static OpcodeProfileResult Failure(string stage, IReadOnlyList<Diagnostic> diagnostics)
        => new(
            false,
            diagnostics,
            [$"PROFILE FAILURE: {stage}"],
            new Dictionary<Opcode, long>(),
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, long>(StringComparer.Ordinal),
            new Dictionary<string, long>(StringComparer.Ordinal));
}

