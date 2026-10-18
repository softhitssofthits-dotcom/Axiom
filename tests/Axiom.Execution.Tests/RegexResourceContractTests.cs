using System.Collections;
using System.Reflection;
using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class RegexResourceContractTests
{
    public static IEnumerable<object[]> MultiMatchForms()
    {
        foreach (var modes in new[] { "", ", \"i\"" })
        {
            yield return [$"REGEXCOUNT(text, \"a\"{modes})"];
            yield return [$"REGEXMATCHES(text, \"a\"{modes})"];
            yield return [$"REGEXEXTRACTALL(text, \"(a)\", 1{modes})"];
            yield return [$"REGEXSPLIT(text, \"a\"{modes})"];
            yield return [$"REGEXREPLACE(text, \"a\", \"b\"{modes})"];
            yield return [$"REGEXREPLACE(text, \"a\", \"$&b\"{modes})"];
            yield return [$"REGEXREPLACE(text, \"a\", \"a\"{modes}, 10000)"];
        }
    }

    [Theory]
    [MemberData(nameof(MultiMatchForms))]
    public void CancellationIsObservedInsideEnumerationAndBypassesCatch(string expression)
    {
        var source = $"DIM text AS STRING = \"{new string('a', 20000)}\"\nTRY\nPRINT {expression}\nCATCH e\nPRINT \"caught\"\nFINALLY\nPRINT \"finally\"\nEND TRY\nPRINT \"after\"";
        var cancellation = new CancelOnCheck(50);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, cancellationProvider: cancellation));
        Assert.Equal(ExecutionStatus.Cancelled, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Output);
    }

    [Theory]
    [InlineData("REGEXCOUNT(text, \"a\")", "100000")]
    [InlineData("REGEXREPLACE(text + \"b\", \"a|b\", \"a\") = text + \"a\"", "TRUE")]
    public void CountAndLateReplacementAvoidMatchObjects(string expression, string expected)
    {
        var source = $"DIM text AS STRING = \"{new string('a', 100000)}\"\nPRINT {expression}";
        var pipeline = new AxiomExecutionPipeline();
        var compiled = pipeline.Compile(new CompilationRequest(source));
        Assert.True(compiled.Succeeded);
        var program = compiled.CompiledProgram!;
        var request = new ExecutionRequest(source);
        Assert.True(pipeline.RunCompiled(program, request).Succeeded);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = pipeline.RunCompiled(program, request);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(result.Succeeded);
        Assert.Equal([expected], result.Output);
        Assert.True(allocated < 8_000_000, $"Allocated {allocated} bytes for {expression}");
    }

    [Fact]
    public void DynamicPatternsHaveBoundedCacheAndEquivalentModesAreNotRetained()
    {
        const string source = """
DIM found AS BOOLEAN
FOR i = 1 TO 1000
found = REGEXISMATCH("a", "(?#" & i & ")a")
NEXT
DIM modes AS STRING = ""
FOR i = 1 TO 1000
modes = modes + "i"
found = REGEXISMATCH("a", "a", modes)
NEXT
PRINT "done"
""";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, sourceBreakpointLines: [10]));
        Assert.Empty(result.Diagnostics);
        var state = Assert.IsType<BytecodeVmStopState>(result.StopState);
        var vm = typeof(BytecodeVmStopState).GetField("_vm", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state)!;
        var cache = (IDictionary)typeof(BytecodeVm).GetField("_regexCache", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vm)!;
        Assert.InRange(cache.Count, 1, 128);
        var modesField = typeof(BytecodeVm).GetField("_regexModesCache", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Null(modesField);
    }

    [Fact]
    public void CacheReusesNormalizedModesAndEvictsLeastRecentlyUsedPattern()
    {
        const string source = """
DIM found AS BOOLEAN = REGEXISMATCH("a", "a", "i")
FOR i = 1 TO 127
found = REGEXISMATCH("a", "(?#" & i & ")a")
NEXT
found = REGEXISMATCH("A", "a", "Ii")
found = REGEXISMATCH("a", "new")
PRINT "done"
""";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, sourceBreakpointLines: [5, 7]));
        Assert.Empty(run.Diagnostics);
        var state = Assert.IsType<BytecodeVmStopState>(run.StopState);
        var vm = typeof(BytecodeVmStopState).GetField("_vm", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(state)!;
        var cache = (IDictionary)typeof(BytecodeVm).GetField("_regexCache", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vm)!;
        var invariant = System.Text.RegularExpressions.RegexOptions.CultureInvariant;
        var key = ("a", invariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var original = ((System.Runtime.CompilerServices.ITuple)cache[key]!)[0];
        Assert.Equal(128, cache.Count);
        var resumed = state.Resume();
        Assert.True(resumed.Stopped);
        Assert.Empty(resumed.Diagnostics);
        Assert.Equal(128, cache.Count);
        Assert.Same(original, ((System.Runtime.CompilerServices.ITuple)cache[key]!)[0]);
        Assert.False(cache.Contains(("(?#1)a", invariant)));
        Assert.True(cache.Contains(("new", invariant)));
    }

    [Theory]
    [InlineData("REGEXISMATCH(\"a\", \"[\")", "pattern", "Invalid pattern")]
    [InlineData("REGEXISMATCH(\"a\", \"a\", \"q\")", "modes", "Unknown mode")]
    [InlineData("REGEXEXTRACT(\"a\", \"(a)\", -1)", "group", "cannot be negative")]
    [InlineData("REGEXREPLACE(\"a\", \"a\", \"x\", -1)", "count", "cannot be negative")]
    public void CatchMessageKeepsArgumentAndSpecificReason(string expression, string argument, string reason)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"TRY\nPRINT {expression}\nCATCH e\nPRINT e.Message\nEND TRY"));
        Assert.True(result.Succeeded);
        var message = Assert.Single(result.Output);
        Assert.Contains(argument, message, StringComparison.Ordinal);
        Assert.Contains(reason, message, StringComparison.Ordinal);
    }

    private sealed class CancelOnCheck(int limit) : IRuntimeCancellationProvider
    {
        private int _checks;
        public bool IsCancellationRequested => ++_checks >= limit;
    }
}
