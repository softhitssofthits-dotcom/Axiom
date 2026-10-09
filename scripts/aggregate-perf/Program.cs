using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Axiom.Core;
using Axiom.Execution;
using Axiom.Runtime;
using Axiom.Semantics;

var root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
var output = Path.Combine(root, ".artifacts", "aggregate-perf-1125");
Directory.CreateDirectory(output);
var reportName = args.ElementAtOrDefault(1) ?? "measurements.json";
if (Path.GetFileName(reportName) != reportName)
    throw new ArgumentException("Имя отчёта должно быть простым именем файла.");
// Привязка выполняется один раз, вне измерения. Публичный API языка не расширяется.
var type = typeof(RuntimeArrayValue).Assembly.GetType("Axiom.Runtime.Builtins.BuiltinArrayAggregateHelpers", throwOnError: true)!;
var average = type.GetMethod("Average", BindingFlags.Public | BindingFlags.Static)!
    .CreateDelegate<Func<RuntimeArrayValue, TextLocation, Action<string, TextLocation>, Action<string, TextLocation>, RuntimeValue>>();
var location = new TextLocation(new TextSpan(0, 1), 1, 1);
Action<string, TextLocation> fail = (message, _) => throw new InvalidOperationException(message);
var measurements = new List<object>();
foreach (var size in new[] { 3, 63, 999, 99_999, 999_999 })
foreach (var pattern in new[] { "ordinary", "cancellation", "subnormal", "zero" })
{
    var array = new RuntimeArrayValue(TypeSymbol.Double, null, 1, size);
    var expected = pattern switch { "ordinary" => 2.0, "cancellation" => 1e-308 / 3, "subnormal" => double.Epsilon, _ => 0.0 };
    for (var index = 1; index <= size; index++)
    {
        var value = pattern switch
        {
            "ordinary" => (double)(index % 3 + 1),
            "cancellation" => (index % 3) switch { 0 => 1e308, 1 => -1e308, _ => 1e-308 },
            "subnormal" => double.Epsilon,
            _ => 0.0
        };
        array.SetValue(index, RuntimeValue.FromDouble(value));
    }
    double Invoke()
    {
        var actual = average(array, location, fail, fail).AsDouble();
        if (BitConverter.DoubleToInt64Bits(actual) != BitConverter.DoubleToInt64Bits(expected))
            throw new InvalidOperationException($"{pattern}/{size}: {actual:R} != {expected:R}");
        return actual;
    }
    var row = Measure(Invoke);
    measurements.Add(new { Scope = "helper", Pattern = pattern, Size = size, row.Milliseconds, row.Bytes, row.MedianMs, row.MedianBytes });
    Console.WriteLine($"Функция: {pattern}, {size} элементов: {row.MedianMs:F3} мс, {row.MedianBytes / (double)size:F1} байт/элемент");
}
foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
{
    var pipeline = new AxiomExecutionPipeline();
    var source = File.ReadAllText(Path.Combine(root, "samples/smoke/engine-compare/arrayaverage_exact_cancellation.axm"));
    var compiled = pipeline.Compile(new CompilationRequest(source));
    if (!compiled.Succeeded) throw new InvalidOperationException("Программа измерения не компилируется.");
    double Invoke()
    {
        var result = pipeline.RunCompiled(compiled.CompiledProgram!, new ExecutionRequest(source, mode: mode));
        if (!result.Succeeded || result.UsedInterpreterFallback || string.Join("\n", result.Output) != "TRUE")
            throw new InvalidOperationException("Нарушен результат или выбранный движок программы измерения.");
        return 1;
    }
    var row = Measure(Invoke);
    measurements.Add(new { Scope = "program", Mode = mode.ToString(), Size = 30_000, Calls = 8, row.Milliseconds, row.Bytes, row.MedianMs, row.MedianBytes });
    Console.WriteLine($"Программа: {mode}: {row.MedianMs:F3} мс, {row.MedianBytes} байт");
}
File.WriteAllText(Path.Combine(output, reportName), JsonSerializer.Serialize(new
{
    Timestamp = DateTimeOffset.Now, Framework = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription, Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    CpuCount = Environment.ProcessorCount, Configuration = "Release", Warmups = 3, Samples = 7,
    Measurements = measurements
}, new JsonSerializerOptions { WriteIndented = true }));
return 0;

static Measurement Measure(Func<double> invoke)
{
    for (var i = 0; i < 3; i++) invoke();
    var times = new double[7];
    var bytes = new long[7];
    for (var i = 0; i < times.Length; i++)
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        invoke();
        times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        bytes[i] = GC.GetAllocatedBytesForCurrentThread() - allocated;
    }
    return new(times, bytes, times.Order().ElementAt(3), bytes.Order().ElementAt(3));
}
record Measurement(double[] Milliseconds, long[] Bytes, double MedianMs, long MedianBytes);
