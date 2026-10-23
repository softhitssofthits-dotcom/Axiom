using System.Text;

namespace Axiom.Execution.Tests;

public sealed class StringReplacementAllocationTests
{
    [Fact]
    public void LongRuleDoesNotCreateOneNodePerCharacter()
    {
        const string source = "DIM oldItems[1 TO 1] AS STRING\r\nDIM newItems[1 TO 1] AS STRING\r\noldItems[1] = REPEAT(\"z\", 100000)\r\nnewItems[1] = \"!\"\r\nPRINT REPLACEMANY(REPEAT(\"a\", 100000), oldItems[], newItems[]) = REPEAT(\"a\", 100000)";
        var pipeline = new AxiomExecutionPipeline();
        var compiled = pipeline.Compile(new CompilationRequest(source));
        Assert.True(compiled.Succeeded);
        pipeline.RunCompiled(compiled.CompiledProgram!, new ExecutionRequest(source));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var run = pipeline.RunCompiled(compiled.CompiledProgram!, new ExecutionRequest(source));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(run.Succeeded);
        Assert.Equal(new[] { "TRUE" }, run.Output);
        Assert.InRange(allocated, 0, 2_000_000);
    }

    [Fact]
    public void CompressedRulesKeepLongestMatchAndFirstDuplicate()
    {
        string[] oldItems = ["a", "abc", "ab", "abc", "abb", "b", "aaaa"];
        string[] newItems = ["1", "2", "3", "wrong", "4", "5", "6"];
        var random = new Random(301);
        for (var iteration = 0; iteration < 40; iteration++)
        {
            var text = new string(Enumerable.Range(0, 60).Select(_ => "abc"[random.Next(3)]).ToArray());
            var expected = new StringBuilder();
            for (var position = 0; position < text.Length;)
            {
                var selected = -1;
                for (var rule = 0; rule < oldItems.Length; rule++)
                    if (text.AsSpan(position).StartsWith(oldItems[rule], StringComparison.Ordinal)
                        && (selected < 0 || oldItems[rule].Length > oldItems[selected].Length)) selected = rule;
                if (selected < 0) expected.Append(text[position++]);
                else { expected.Append(newItems[selected]); position += oldItems[selected].Length; }
            }
            var source = $"PRINT REPLACEMANY(\"{text}\", SPLIT(\"{string.Join('|', oldItems)}\", \"|\"), SPLIT(\"{string.Join('|', newItems)}\", \"|\"))";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded);
            Assert.Equal(new[] { expected.ToString() }, run.Output);
        }
    }
}
