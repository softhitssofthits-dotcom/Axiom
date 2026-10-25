using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Runtime;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Execution.Tests;

public sealed class ForEachContractTests
{
    [Fact]
    public void ByValWalksValuesInOrder()
    {
        const string source =
            "DIM fruits[] AS STRING = [\"яблоко\", \"банан\", \"апельсин\"]\r\n" +
            "FOR EACH fruit IN fruits[]\r\n" +
            "    PRINT fruit\r\n" +
            "NEXT fruit";

        CheckBoth(source, new[] { "яблоко", "банан", "апельсин" });
    }

    [Fact]
    public void ByValDoesNotWriteBackToSource()
    {
        const string source =
            "DIM values[1 TO 3] AS INTEGER\r\n" +
            "values[1] = 1\r\n" +
            "values[2] = 2\r\n" +
            "values[3] = 3\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    item = 99\r\n" +
            "NEXT\r\n" +
            "PRINT values[1], values[2], values[3]";

        CheckBoth(source, new[] { "1 2 3" });
    }

    [Fact]
    public void ByValCapturedThroughByRefParameterKeepsItsIterationCell()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
DIM values[] AS INTEGER = [1, 2]
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
FOR EACH BYVAL item IN values[]
    IF item = 1 THEN
        first = Capture(item)
    ELSE
        second = Capture(item)
    END IF
NEXT item
PRINT first()
PRINT second()
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1", "2"], result.Output);
    }

    [Fact]
    public void ArrayShapeChangeOnLastIterationFailsBeforeLoopExit()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
TRY
    FOR EACH item IN values[]
        REDIM PRESERVE values[1 TO 2]
    NEXT item
    PRINT "цикл завершён"
CATCH problem
    PRINT problem.Code
END TRY
""";

        CheckBoth(source, ["MB3014"]);
    }

    [Fact]
    public void ErasingSourceOnLastIterationReportsExpiredArrayView()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
TRY
    FOR EACH item IN values[]
        ERASE values[]
    NEXT item
    PRINT "цикл завершён"
CATCH problem
    PRINT problem.Code
END TRY
""";

        CheckBoth(source, ["MB3014"]);
    }

    [Fact]
    public void ArrayShapeMutatorInvalidatesForEachAfterTheCurrentItem()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2]
TRY
    FOR EACH BYREF item IN values[]
        ARRAYPUSH(values[], 3)
    NEXT item
    PRINT "цикл завершён"
CATCH problem
    PRINT problem.Code
END TRY
""";

        CheckBoth(source, ["MB3014"]);
    }

    [Fact]
    public void ByRefWritesThroughToArrayElement()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2]
FOR EACH BYREF item IN values[]
    Increment(item)
NEXT item
PRINT values[1], values[2]
SUB Increment(BYREF value AS INTEGER)
    value = value + 1
END SUB
""";

        CheckBoth(source, ["2 3"]);
    }

    [Fact]
    public void ByRefWritesThroughAValidArrayView()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2, 3, 4]
FOR EACH BYREF item IN values[2 TO 3]
    item = item * 10
NEXT item
PRINT values[1], values[2], values[3], values[4]
""";

        CheckBoth(source, ["1 20 30 4"]);
    }

    [Fact]
    public void ByRefTypeMethodMutatesTheCurrentRecord()
    {
        const string source = """
TYPE Counter
    Value AS INTEGER = 0
    SUB Increment()
        SELF.Value = SELF.Value + 1
    END SUB
END TYPE
DIM values[1 TO 2] AS Counter
FOR EACH BYREF item IN values[]
    item.Increment()
NEXT item
PRINT values[1].Value, values[2].Value
""";

        CheckBoth(source, ["1 1"]);
    }

    [Fact]
    public void ByRefFixedStringKeepsItsDeclaredLength()
    {
        const string source = """
DIM values[1 TO 1] AS STRING * 4
FOR EACH BYREF item IN values[]
    item = "alphabet"
NEXT item
PRINT values[1] = "alph"
""";

        CheckBoth(source, ["TRUE"]);
    }

    [Fact]
    public void ByRefAsRequiresExactElementType()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
FOR EACH BYREF item AS DOUBLE IN values[]
    PRINT item
NEXT
""";

        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(run.Succeeded);
        Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Code == "MB2063");
    }

    [Fact]
    public void ByRefItemReadAfterShapeChangeReportsExpiredArrayView()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
TRY
    FOR EACH BYREF item IN values[]
        REDIM PRESERVE values[1 TO 2]
        PRINT item
    NEXT item
CATCH problem
    PRINT problem.Code
END TRY
""";

        CheckBoth(source, ["MB3014"]);
    }

    [Fact]
    public void ByRefCapturedThroughByRefParameterKeepsItsIterationAddress()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
DIM values[] AS INTEGER = [1, 2]
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
FOR EACH BYREF item IN values[]
    IF item = 1 THEN
        first = Capture(item)
    ELSE
        second = Capture(item)
    END IF
NEXT item
PRINT first()
PRINT second()
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1", "2"], result.Output);
    }

    [Fact]
    public void MultidimensionalOrderChangesLastDimensionFastest()
    {
        const string source =
            "DIM grid[1 TO 2, 1 TO 3] AS INTEGER\r\n" +
            "FOR r = 1 TO 2\r\n" +
            "    FOR c = 1 TO 3\r\n" +
            "        grid[r, c] = r * 10 + c\r\n" +
            "    NEXT c\r\n" +
            "NEXT r\r\n" +
            "FOR EACH cell IN grid[]\r\n" +
            "    PRINT cell\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "11", "12", "13", "21", "22", "23" });
    }

    [Fact]
    public void NegativeAndZeroLowerBoundsAreHonoured()
    {
        const string source =
            "DIM values[-1 TO 1] AS INTEGER\r\n" +
            "values[-1] = 7\r\n" +
            "values[0] = 8\r\n" +
            "values[1] = 9\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "7", "8", "9" });
    }

    [Fact]
    public void EmptyArrayRunsZeroIterations()
    {
        const string source =
            "DIM values[] AS INTEGER = []\r\n" +
            "DIM count AS INTEGER\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    count = count + 1\r\n" +
            "NEXT\r\n" +
            "PRINT count";

        CheckBoth(source, new[] { "0" });
    }

    [Fact]
    public void SourceIsEvaluatedOnce()
    {
        const string source =
            "DIM calls AS INTEGER\r\n" +
            "FUNCTION Make() AS INTEGER[]\r\n" +
            "    calls = calls + 1\r\n" +
            "    DIM local[] AS INTEGER = [1, 2, 3]\r\n" +
            "    RETURN ARRAYCLONE(local[])\r\n" +
            "END FUNCTION\r\n" +
            "FOR EACH item IN Make()\r\n" +
            "    PRINT item\r\n" +
            "NEXT\r\n" +
            "PRINT calls";

        CheckBoth(source, new[] { "1", "2", "3", "1" });
    }

    [Fact]
    public void AsClauseConvertsEachValue()
    {
        const string source =
            "DIM values[1 TO 2] AS INTEGER\r\n" +
            "values[1] = 5\r\n" +
            "values[2] = 7\r\n" +
            "FOR EACH item AS DOUBLE IN values[]\r\n" +
            "    PRINT item / 2\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "2.5", "3.5" });
    }

    [Fact]
    public void ContinueAndExitForWorkInsideForEach()
    {
        const string source =
            "DIM values[1 TO 5] AS INTEGER\r\n" +
            "FOR index = 1 TO 5\r\n" +
            "    values[index] = index\r\n" +
            "NEXT index\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    IF item = 3 THEN\r\n" +
            "        CONTINUE FOR\r\n" +
            "    END IF\r\n" +
            "    IF item = 5 THEN\r\n" +
            "        EXIT FOR\r\n" +
            "    END IF\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "1", "2", "4" });
    }

    [Fact]
    public void RecordsAreCopiedByVal()
    {
        const string source =
            "TYPE Product\r\n" +
            "    Name AS STRING\r\n" +
            "    Price AS INTEGER\r\n" +
            "END TYPE\r\n" +
            "DIM basket[] AS Product = [Product(\"яблоко\", 30), Product(\"банан\", 20)]\r\n" +
            "FOR EACH item IN basket[]\r\n" +
            "    item.Price = 0\r\n" +
            "    PRINT item.Name\r\n" +
            "NEXT\r\n" +
            "PRINT basket[1].Price";

        CheckBoth(source, new[] { "яблоко", "банан", "30" });
    }

    [Fact]
    public void NestedForEachLoopsWalkBothArrays()
    {
        const string source =
            "DIM outer[1 TO 2] AS INTEGER\r\n" +
            "outer[1] = 1\r\n" +
            "outer[2] = 2\r\n" +
            "DIM inner[1 TO 2] AS INTEGER\r\n" +
            "inner[1] = 10\r\n" +
            "inner[2] = 20\r\n" +
            "FOR EACH a IN outer[]\r\n" +
            "    FOR EACH b IN inner[]\r\n" +
            "        PRINT a + b\r\n" +
            "    NEXT\r\n" +
            "NEXT";

        CheckBoth(source, new[] { "11", "21", "12", "22" });
    }

    [Fact]
    public void LastElementAtIntegerMaxDoesNotOverflowCounter()
    {
        // Единственный элемент с верхней границей INTEGER_MAX раньше увеличивался
        // до переполнения ещё до проверки исчерпания диапазона.
        const string source =
            "DIM values[2147483647 TO 2147483647] AS INTEGER\r\n" +
            "values[2147483647] = 7\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT\r\n" +
            "PRINT \"готово\"";

        CheckBoth(source, new[] { "7", "готово" });
    }

    [Fact]
    public void ByRefModifierIsAcceptedAndWritesThrough()
    {
        const string source =
            "DIM values[] AS INTEGER = [1, 2]\r\n" +
            "FOR EACH BYREF item IN values[]\r\n" +
            "    item = 99\r\n" +
            "NEXT\r\n" +
            "PRINT values[1], values[2]";

        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["99 99"], run.Output);
    }

    [Fact]
    public void UnallocatedSourceFailsAtRuntime()
    {
        const string source =
            "DIM values[] AS INTEGER\r\n" +
            "FOR EACH item IN values[]\r\n" +
            "    PRINT item\r\n" +
            "NEXT";

        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(run.Succeeded);
        Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Code == "MB3005");
    }

    [Fact]
    public void ByRefAcceptanceSampleRunsWithExpectedOutput()
    {
        var source = TestProgramLoader.LoadText("samples/control-flow/for-each/each_byref.axm");
        CheckBoth(source, ["4", "5"]);
    }

    [Fact]
    public void CapturedIterationCellsAndAddressesSampleRunsWithExpectedOutput()
    {
        var source = TestProgramLoader.LoadText("samples/control-flow/for-each/each_iteration_captures.axm");
        CheckBoth(source, ["1", "2", "3", "4"]);
    }

    [Fact]
    public void ByRefIterationAddressSurvivesBytecodeRoundTripAndStepBack()
    {
        const string source = """
DIM values[] AS INTEGER = [10, 1, 2, 20]
FOR EACH BYREF item IN values[2 TO 3]
    Increment(item)
NEXT item
PRINT values[2], values[3]
SUB Increment(BYREF value AS INTEGER)
    value = value + 1
END SUB
""";

        CheckRoundTripAndStepBack(source, breakpointLine: 3, ["2 3"]);
    }

    [Theory]
    [InlineData("BYVAL")]
    [InlineData("BYREF")]
    public void DebuggerShowsTheCurrentForEachElementAndRestoresItOnStepBack(string mode)
    {
        const string template = """
DIM values[] AS INTEGER = [7]
FOR EACH MODE item IN values[]
    PRINT item
NEXT item
""";
        var source = template.Replace("MODE", mode, StringComparison.Ordinal);
        var compiled = Compile(source);
        var output = new ReversibleBufferWriter();
        var pipeline = new AxiomExecutionPipeline();
        var stopped = pipeline.RunCompiled(
            compiled,
            new ExecutionRequest(source, sourceBreakpointLines: [3], outputWriter: output));

        Assert.True(stopped.Stopped);
        Assert.True(
            stopped.StopState!.CurrentForEachItem == new BytecodeVmForEachItem("item", "INTEGER", "7"),
            $"Остановка: {stopped.StopState.Snapshot.SourceLocation.Line}, {stopped.StopState.Snapshot.Opcode}; элемент={stopped.StopState.CurrentForEachItem}");
        Assert.True(stopped.CanStepBack);

        var backed = stopped.StopState.StepBack();
        Assert.True(backed.Stopped);
        Assert.Null(backed.StopState!.CurrentForEachItem);
    }

    [Fact]
    public void DebuggerClearsTheCurrentForEachElementAfterTheLoop()
    {
        const string source = """
DIM values[] AS INTEGER = [7]
FOR EACH BYREF item IN values[]
    PRINT item
NEXT item
PRINT values[1]
""";
        var compiled = Compile(source);
        var stopped = new AxiomExecutionPipeline().RunCompiled(
            compiled,
            new ExecutionRequest(source, sourceBreakpointLines: [5]));

        Assert.True(stopped.Stopped);
        Assert.Null(stopped.StopState!.CurrentForEachItem);
    }

    [Fact]
    public void DebuggerCanStopBeforeHandlingAnExpiredArrayView()
    {
        const string source = """
DIM stale[] AS INTEGER
DIM values[] AS INTEGER = [1, 2, 3]
stale[] = values[1 TO 2]
DIM removed AS INTEGER = ARRAYPOP(values[])
DIM marker AS INTEGER = 0
TRY
    PRINT stale[1]
CATCH problem
    PRINT problem.Code
END TRY
""";

        var compiled = Compile(source);
        var output = new ReversibleBufferWriter();
        var stopped = new AxiomExecutionPipeline().RunCompiled(
            compiled,
            new ExecutionRequest(source, sourceBreakpointLines: [6], outputWriter: output));

        Assert.True(stopped.Stopped);
        Assert.Equal(6, stopped.StopState!.Snapshot.SourceLocation.Line);

        var stop = stopped.StopState;
        for (var guard = 0; guard < 20; guard++)
        {
            var resumed = stop.Resume();
            if (!resumed.Stopped)
            {
                Assert.True(resumed.Succeeded, string.Join("\n", resumed.Diagnostics.Select(d => d.Message)));
                Assert.Equal(["MB3014"], output.Lines);
                return;
            }

            stop = resumed.StopState!;
        }

        Assert.Fail("Выполнение после остановки перед просроченным представлением не завершилось.");
    }

    [Fact]
    public void StepBackKeepsTheCapturedByRefCellFromTheEarlierRecursiveCall()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2]
DIM reader AS FUNCTION() AS INTEGER
FOR EACH BYREF item IN values[]
    reader = Capture(item, reader)
NEXT item
PRINT reader()
FUNCTION Capture(BYREF value AS INTEGER, previous AS FUNCTION() AS INTEGER) AS FUNCTION() AS INTEGER
    IF value = 2 THEN
        PRINT previous()
    END IF
    FUNCTION ReadValue() AS INTEGER
        DIM marker AS INTEGER = 0
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
""";

        CheckRoundTripAndStepBack(source, breakpointLine: 13, ["1", "2"]);
    }

    [Fact]
    public void StepBackKeepsFunctionsInsideArraysOnTheRestoredValueGraph()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
DIM readers[1 TO 1] AS FUNCTION() AS INTEGER
FOR EACH BYREF item IN values[]
    readers[1] = Capture(item)
NEXT item
values[1] = 9
PRINT readers[1]()
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
""";

        CheckRoundTripAndStepBack(source, breakpointLine: 6, ["9"]);
    }

    [Fact]
    public void DebuggerClearsTheCurrentForEachItemAfterAHandlerOutsideTheLoop()
    {
        const string source = """
DIM values[] AS INTEGER = [1]
TRY
    FOR EACH BYREF item IN values[]
        REDIM values[1 TO 2]
    NEXT item
CATCH problem
    PRINT problem.Code
END TRY
PRINT "done"
""";
        var compiled = Compile(source);
        var stopped = new AxiomExecutionPipeline().RunCompiled(
            compiled,
            new ExecutionRequest(source, sourceBreakpointLines: [9]));

        Assert.True(stopped.Stopped);
        Assert.Null(stopped.StopState!.CurrentForEachItem);
    }

    [Fact]
    public void DebuggerDoesNotTreatAClosureFromAnEarlierIterationAsTheCurrentItem()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2]
DIM first AS FUNCTION() AS INTEGER
FOR EACH BYREF item IN values[]
    IF item = 1 THEN first = Capture(item)
NEXT item
PRINT first()
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
""";
        var compiled = Compile(source);
        var stopped = new AxiomExecutionPipeline().RunCompiled(
            compiled,
            new ExecutionRequest(source, sourceBreakpointLines: [9]));

        Assert.True(stopped.Stopped);
        Assert.Null(stopped.StopState!.CurrentForEachItem);
    }

    [Fact]
    public void CapturedByValIterationCellsSurviveStepBack()
    {
        const string source = """
DIM values[] AS INTEGER = [1, 2]
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
FOR EACH BYVAL item IN values[]
    IF item = 1 THEN
        first = Capture(item)
    ELSE
        second = Capture(item)
    END IF
NEXT item
PRINT first()
PRINT second()
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
""";

        CheckRoundTripAndStepBack(source, breakpointLine: 11, ["1", "2"]);
    }

    [Fact]
    public void CapturedByRefIterationAddressesSurviveStepBack()
    {
        const string source = """
DIM values[] AS INTEGER = [10, 1, 2, 20]
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
FOR EACH BYREF item IN values[2 TO 3]
    IF item = 1 THEN
        first = Capture(item)
    ELSE
        second = Capture(item)
    END IF
NEXT item
PRINT first()
PRINT second()
PRINT values[2], values[3]
FUNCTION Capture(BYREF value AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION ReadValue() AS INTEGER
        RETURN value
    END FUNCTION
    RETURN ReadValue
END FUNCTION
""";

        CheckRoundTripAndStepBack(source, breakpointLine: 11, ["1", "2", "1 2"]);
    }

    private static void CheckBoth(string source, string[] expected)
    {
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, run.Output);
    }

    private static void CheckRoundTripAndStepBack(string source, int breakpointLine, string[] expected)
    {
        var compiled = Compile(source);
        var restoredModule = BytecodeModuleSerializer.DeserializeModuleFromJson(
            BytecodeModuleSerializer.SerializeToJson(compiled.BytecodeModule!));
        var validation = new BytecodeModuleValidator().Validate(restoredModule);
        Assert.True(validation.Succeeded, string.Join("\n", validation.Diagnostics.Select(d => d.Message)));

        var restoredProgram = new CompiledProgram(
            source,
            compiled.Syntax,
            compiled.BoundProgram,
            compiled.LoweredProgram,
            restoredModule,
            compiled.SourceDocuments);
        var output = new ReversibleBufferWriter();
        var pipeline = new AxiomExecutionPipeline();
        var stopped = pipeline.RunCompiled(
            restoredProgram,
            new ExecutionRequest(source, sourceBreakpointLines: [breakpointLine], outputWriter: output));

        Assert.True(stopped.Succeeded, string.Join("\n", stopped.Diagnostics.Select(d => d.Message)));
        Assert.True(stopped.Stopped);
        Assert.True(stopped.CanStepBack);

        var backed = stopped.StopState!.StepBack();
        Assert.True(backed.Stopped);

        var stop = backed.StopState!;
        for (var guard = 0; guard < 200; guard++)
        {
            var resumed = stop.Resume();
            if (!resumed.Stopped)
            {
                Assert.True(resumed.Succeeded, string.Join("\n", resumed.Diagnostics.Select(d => d.Message)));
                Assert.Equal(expected, output.Lines);
                return;
            }

            stop = resumed.StopState!;
        }

        Assert.Fail("Выполнение после обратного шага не завершилось.");
    }

    private static CompiledProgram Compile(string source)
    {
        var syntax = Parser.ParseSquareBracketSyntax(source, out var parsing);
        Assert.Empty(parsing);
        var bound = Binder.Bind(syntax, out var binding, checkGraphicsModes: false);
        Assert.Empty(binding);
        var lowering = new BoundProgramLowerer().Lower(bound);
        Assert.True(lowering.Succeeded, string.Join("\n", lowering.Diagnostics.Select(d => d.Message)));
        var compilation = new BytecodeCompiler().Compile(lowering.Program!);
        Assert.True(compilation.Succeeded, string.Join("\n", compilation.Diagnostics.Select(d => d.Message)));
        return new CompiledProgram(source, syntax, bound, lowering.Program, compilation.Module);
    }

    private sealed class ReversibleBufferWriter : IReversibleOutputWriter
    {
        public List<string> Lines { get; } = [];

        public int OutputCheckpoint => Lines.Count;

        public void WriteOutput(string text) => Lines.Add(text);

        public void RestoreOutputCheckpoint(int checkpoint) => Lines.RemoveRange(checkpoint, Lines.Count - checkpoint);
    }
}
