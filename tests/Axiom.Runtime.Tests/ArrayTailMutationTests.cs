using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class ArrayTailMutationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TailInsertAndRemoveAllocateLikePushAndPop(bool remove)
    {
        const int count = 2048;
        var setup = remove ? $"DIM a[1 TO {count}] AS INTEGER\n" : "DIM a[1 TO 0] AS INTEGER\n";
        var loop = remove ? $"FOR i = {count} TO 1 STEP -1\n" : $"FOR i = 1 TO {count}\n";
        var optimized = remove ? "ARRAYREMOVE(a[], i)" : "ARRAYINSERT(a[], i, i)";
        var control = remove ? "ARRAYPOP(a[])" : "ARRAYPUSH(a[], i)";
        var suffix = "\nNEXT i\nPRINT ARRAYLENGTH(a[])";
        var module = Compile(setup + loop + optimized + suffix);
        var controlModule = Compile(setup + loop + control + suffix);
        var expected = remove ? "0" : count.ToString();
        for (var warmup = 0; warmup < 2; warmup++)
        {
            _ = AllocatedByRun(controlModule, expected);
            _ = AllocatedByRun(module, expected);
        }

        var controlBytes = AllocatedByRun(controlModule, expected);
        var actualBytes = AllocatedByRun(module, expected);

        // Сравниваются выделения одинакового исполнителя, а не скорость машины.
        // Запас допускает дополнительные аргументы INSERT и вычисления индекса.
        Assert.True(actualBytes <= controlBytes * 4 + 262_144,
            $"Tail operation allocated {actualBytes} bytes; PUSH/POP control allocated {controlBytes}.");
    }

    [Theory]
    [InlineData(-2147483647, false)]
    [InlineData(-3, true)]
    [InlineData(1, false)]
    [InlineData(2147483646, true)]
    public void TailMutationsKeepBoundsAliasesAndExpireViews(int lower, bool observed)
    {
        var last = lower + 1;
        var source = $$"""
DIM a[{{lower}} TO {{lower}}] AS INTEGER
DIM alias[] AS INTEGER
DIM part[] AS INTEGER
a[{{lower}}] = 10
alias[] = a[]
part[] = a[{{lower}} TO {{lower}}]
ARRAYINSERT(alias[], {{last}}, 77, 1)
PRINT LBOUND(a[])
PRINT UBOUND(a[])
PRINT a[{{lower}}]
TRY
    PRINT part[{{lower}}]
CATCH err
    PRINT err.Code
END TRY
part[] = a[{{lower}} TO {{last}}]
PRINT ARRAYREMOVE(alias[], {{last}}, 1)
PRINT UBOUND(a[])
TRY
    PRINT part[{{lower}}]
CATCH err
    PRINT err.Code
END TRY
PRINT ARRAYREMOVE(a[], {{lower}})
PRINT LBOUND(alias[])
PRINT UBOUND(alias[])
PRINT ARRAYLENGTH(alias[])
PRINT ARRAYISALLOCATED(alias[])
""";
        Check(source, observed, [lower.ToString(), last.ToString(), "10", "MB3014", "77",
            lower.ToString(), "MB3014", "10", "1", "0", "0", "TRUE"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TailMutationPreservesFixedStrings(bool observed)
    {
        Check("DIM fruits[1 TO 1] AS STRING * 3\nfruits[1] = \"яблоко\"\nARRAYINSERT(fruits[], 2, \"банан\")\nPRINT fruits[1]\nPRINT fruits[2]\nPRINT ARRAYREMOVE(fruits[], 2)\nPRINT ARRAYREMOVE(fruits[], 1)\nARRAYINSERT(fruits[], 1, \"ёж\")\nPRINT LEN(fruits[1])\nPRINT fruits[1] = \"ёж \"", observed, ["ябл", "бан", "бан", "ябл", "3", "TRUE"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TailMutationPreservesRecordCopyRules(bool observed)
    {
        Check("TYPE Fruit\n    Price AS INTEGER\n    FixedTags[1 TO 1] AS INTEGER\n    SharedTags[] AS INTEGER\nEND TYPE\nDIM item AS Fruit\nitem.Price = 10\nitem.FixedTags[1] = 20\nREDIM item.SharedTags[1 TO 1]\nitem.SharedTags[1] = 30\nDIM basket[1 TO 0] AS Fruit\nARRAYINSERT(basket[], 1, item)\nitem.Price = 99\nitem.FixedTags[1] = 99\nitem.SharedTags[1] = 99\nPRINT basket[1].Price\nPRINT basket[1].FixedTags[1]\nPRINT basket[1].SharedTags[1]\nDIM removed AS Fruit\nremoved = ARRAYREMOVE(basket[], 1)\nPRINT removed.Price\nPRINT removed.FixedTags[1]\nPRINT removed.SharedTags[1]\nPRINT ARRAYLENGTH(basket[])", observed, ["10", "20", "99", "10", "20", "99", "0"]);
    }

    [Theory]
    [InlineData("ARRAYINSERT(values[], 3, 30)", false)]
    [InlineData("ARRAYINSERT(values[], 3, 30)", true)]
    [InlineData("ARRAYREMOVE(values[], 2)", false)]
    [InlineData("ARRAYREMOVE(values[], 2)", true)]
    public void TailMutationThroughByRefArrayExpiresViews(string mutation, bool observed)
    {
        var length = mutation.Contains("INSERT", StringComparison.Ordinal) ? "3" : "1";
        Check($$"""
DIM a[1 TO 2] AS INTEGER
DIM part[] AS INTEGER
a[1] = 10
a[2] = 20
part[] = a[1 TO 1]
Change(a[])
TRY
    PRINT part[1]
CATCH err
    PRINT err.Code
END TRY
PRINT a[1]
PRINT ARRAYLENGTH(a[])
SUB Change(BYREF values[] AS INTEGER)
    {{mutation}}
END SUB
""", observed, ["MB3014", "10", length]);
    }

    [Theory]
    [InlineData("DIM a[] AS INTEGER", "ARRAYINSERT(a[], 1, 7)", "MB3005")]
    [InlineData("DIM a[] AS INTEGER", "ARRAYREMOVE(a[], 1)", "MB3005")]
    [InlineData("DIM a[1 TO 0] AS INTEGER", "ARRAYINSERT(a[], 0, 7)", "MB3007")]
    [InlineData("DIM a[1 TO 0] AS INTEGER", "ARRAYREMOVE(a[], 1)", "MB3007")]
    [InlineData("DIM a[1 TO 1] AS INTEGER", "ARRAYINSERT(a[], 2, 7, 0)", "MB3007")]
    [InlineData("DIM a[1 TO 1] AS INTEGER", "ARRAYREMOVE(a[], 1, 0)", "MB3007")]
    [InlineData("DIM a[2147483647 TO 2147483647] AS INTEGER", "ARRAYINSERT(a[], 2147483647, 7)", "MB3007")]
    public void ValidationKeepsErrorCodeAndArrayLength(string setup, string operation, string code)
    {
        var length = setup.Contains("1 TO 0") || setup.Contains("a[]") ? "0" : "1";
        Check(setup + "\nTRY\n" + operation + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a[])",
            false, [code, length]);
    }

    [Theory]
    [InlineData("ARRAYINSERT(part[], 2, 7)")]
    [InlineData("ARRAYREMOVE(part[], 1)")]
    public void ResizingViewRemainsForbidden(string operation)
    {
        Check("DIM a[1 TO 1] AS INTEGER\nDIM part[] AS INTEGER\npart[] = a[1 TO 1]\nTRY\n" + operation
            + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a[])\nPRINT ARRAYLENGTH(part[])", false,
            ["MB3007", "1", "1"]);
    }

    [Fact]
    public void ObservedSnapshotRemainsIndependentAfterTailRemoval()
    {
        var module = Compile("DIM a[1 TO 1] AS INTEGER\na[1] = 7\nARRAYINSERT(a[], 2, 9)\nPRINT a[2]\nARRAYREMOVE(a[], 2)\nPRINT a[1]");
        var host = new TestRuntimeHost([]);
        var stoppedOnce = false;
        var stopped = BytecodeVm.Execute(module, host, _ => { }, snapshot =>
        {
            if (stoppedOnce || snapshot.SourceLocation.Line != 4 || snapshot.Opcode != Opcode.PrintValue)
                return false;
            stoppedOnce = true;
            return true;
        });
        Assert.True(stopped.Stopped);
        var previous = Assert.IsType<BytecodeVmStateSnapshot>(stopped.StopState!.PreviousStateSnapshot);
        var values = previous.Globals.Values.Concat(previous.CallFramesBottomToTop.SelectMany(frame => frame.Locals).Select(slot => slot.Value));
        var array = Assert.Single(values, value => value.Type is ArrayTypeSymbol).AsArray();
        Assert.Equal(2, array.Length);
        var resumed = stopped.StopState.Resume();
        Assert.True(resumed.Succeeded);
        Assert.Equal(["9", "7"], host.Output);
        Assert.Equal(2, array.Length);
        Assert.Equal(9, array.GetValue(2).AsInteger());
    }

    private static long AllocatedByRun(BytecodeModule module, string expected)
    {
        var host = new TestRuntimeHost([]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = BytecodeVm.Execute(module, host);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal([expected], host.Output);
        return bytes;
    }

    private static void Check(string source, bool observed, string[] expected)
    {
        var host = new TestRuntimeHost([]);
        var result = BytecodeVm.Execute(Compile(source), host, observed ? _ => { } : null);
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected, host.Output);
    }

    private static BytecodeModule Compile(string source)
    {
        var syntax = Parser.Parse(source, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.DoesNotContain(binding, d => d.Severity == DiagnosticSeverity.Error);
        var lowering = new BoundProgramLowerer().Lower(program);
        Assert.True(lowering.Succeeded);
        var compilation = new BytecodeCompiler().Compile(lowering.Program!);
        Assert.True(compilation.Succeeded);
        return compilation.Module!;
    }
}
