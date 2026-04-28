using Axiom.Core;
using Axiom.Runtime;

using Axiom.Bytecode;

namespace Axiom.Execution.Tests;

public sealed class ExecutionPipelineTests
{
    [Fact]
    public void Compile_SucceedsForSimpleProgram()
    {
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Compile(new CompilationRequest("PRINT 1 + 2"));

        Assert.Equal(CompilationStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.CompiledProgram);
        Assert.True(result.BackendSucceeded);
        Assert.True(result.BytecodeAvailable);
        Assert.NotNull(result.CompiledProgram!.LoweredProgram);
        Assert.NotNull(result.CompiledProgram.BytecodeModule);
        Assert.True(result.LoweringDuration >= TimeSpan.Zero);
        Assert.True(result.BytecodeEmissionDuration >= TimeSpan.Zero);
    }

    [Fact]
    public void Compile_WritesBytecodeSnapshotWhenPathIsProvided()
    {
        var pipeline = new AxiomExecutionPipeline();
        var snapshotPath = CreateTemporarySnapshotPath();

        try
        {
            var result = pipeline.Compile(new CompilationRequest("PRINT 1 + 2", bytecodeSnapshotPath: snapshotPath));

            Assert.Equal(CompilationStatus.Success, result.Status);
            Assert.True(result.BackendSucceeded);
            Assert.True(result.BytecodeAvailable);
            Assert.True(File.Exists(snapshotPath));

            using var stream = File.OpenRead(snapshotPath);
            var module = BytecodeModuleSerializer.DeserializeModuleFromJson(stream);
            var validationResult = new BytecodeModuleValidator().Validate(module);
            Assert.True(
                validationResult.Succeeded,
                string.Join(" | ", validationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}")));
        }
        finally
        {
            DeleteTemporaryFile(snapshotPath);
        }
    }

    [Fact]
    public void Run_WritesBytecodeSnapshotWhenPathIsProvided()
    {
        var pipeline = new AxiomExecutionPipeline();
        var snapshotPath = CreateTemporarySnapshotPath();

        try
        {
            var result = pipeline.Run(new ExecutionRequest("PRINT 1 + 2", bytecodeSnapshotPath: snapshotPath));

            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["3"], result.Output);
            Assert.True(File.Exists(snapshotPath));

            var json = File.ReadAllText(snapshotPath);
            var module = BytecodeModuleSerializer.DeserializeModuleFromJson(json);
            Assert.Equal(0, module.EntryPointRoutineId);
        }
        finally
        {
            DeleteTemporaryFile(snapshotPath);
        }
    }

    [Fact]
    public void RunCompiled_ExecutesPrecompiledProgram()
    {
        var pipeline = new AxiomExecutionPipeline();
        var compilation = pipeline.Compile(new CompilationRequest("PRINT 1 + 2"));

        var result = pipeline.RunCompiled(
            compilation.CompiledProgram!,
            new ExecutionRequest("PRINT 1 + 2"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["3"], result.Output);
    }

    [Fact]
    public void Run_SucceedsForSimpleProgram()
    {
        var result = Run("PRINT 1 + 2");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["3"], result.Output);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.True(result.RuntimeExecutionDuration >= TimeSpan.Zero);
    }

    [Fact]
    public void Run_WithInterpreterMode_UsesInterpreterEngine()
    {
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            "PRINT 1 + 2",
            mode: ExecutionMode.Interpreter));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Interpreter, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(["3"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForCoveredProgram()
    {
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            "PRINT 1 + 2",
            mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["3"], result.Output);
    }

    [Fact]
    public void Run_WithSourceBreakpointLines_StopsBeforeLineOnBytecodeVm()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            text,
            mode: ExecutionMode.Bytecode,
            sourceBreakpointLines: [2]));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(["1"], result.Output);
    }

    [Fact]
    public void Run_WithReversibleBytecodeStopExposesStepBackAvailability()
    {
        const string text = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            text,
            mode: ExecutionMode.Bytecode,
            sourceBreakpointLines: [3]));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.CanStepBack);

        var backed = result.StopState!.StepBack();

        Assert.True(backed.Stopped);
        Assert.Equal(2, backed.StoppedAt!.SourceLocation.Line);
    }

    [Fact]
    public void ExecutionRequest_NormalizesSourceBreakpointLines()
    {
        var request = new ExecutionRequest("PRINT 1", sourceBreakpointLines: [3, 2, 3, 2]);

        Assert.Equal([3, 2], request.SourceBreakpointLines);
    }

    [Fact]
    public void ExecutionRequest_StoresStopBeforeFirstInstructionFlag()
    {
        var request = new ExecutionRequest("PRINT 1", stopBeforeFirstInstruction: true);

        Assert.True(request.StopBeforeFirstInstruction);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForIndirectFunctionValueCall()
    {
        const string sourceText = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

DIM op AS FUNCTION(INTEGER) AS INTEGER
op = AddOne
PRINT op(4)
""";
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            sourceText,
            mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["5"], result.Output);
    }

    [Fact]
    public void Run_WithDefaultMode_UsesBytecodeVmForIndirectFunctionValueCall()
    {
        const string sourceText = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

DIM op AS FUNCTION(INTEGER) AS INTEGER
op = AddOne
PRINT op(4)
""";
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(sourceText));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["5"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForRemainingArithmeticOperators()
    {
        const string text = """
DIM mask AS INTEGER = 16
DIM wide AS LONG = 64
PRINT 7 \ 2
PRINT 2 ^ 3
PRINT 1 << 3
PRINT mask >> 2
PRINT wide >> 3
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["3", "8", "8", "4", "8"], result.Output);
    }

    [Fact]
    public void Run_SupportsEmptyPrint()
    {
        var result = Run("PRINT");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal([""], result.Output);
    }

    [Fact]
    public void Run_SupportsPrintWithSemicolon()
    {
        var result = Run("PRINT \"A\"; \"B\"");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["AB"], result.Output);
    }

    [Fact]
    public void Run_SupportsPrintWithComma()
    {
        var result = Run("PRINT \"A\", \"B\"");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["A B"], result.Output);
    }

    [Fact]
    public void Run_SupportsMixedPrintList()
    {
        var result = Run("PRINT 1; 2, 3");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["12 3"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_PreservesPrintTrailingCommaAcrossLoopIterations()
    {
        const string text = """
FOR i = 1 TO 10
    PRINT i,
NEXT
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["1 2 3 4 5 6 7 8 9 10 "], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_PreservesPrintTrailingSemicolonAcrossLoopIterations()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i;
NEXT
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["123"], result.Output);
    }

    [Fact]
    public void Run_SupportsElseIfChains()
    {
        const string text = """
DIM score AS INTEGER = 3
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["low"], result.Output);
    }

    [Fact]
    public void Run_SupportsOrderedStringComparisons()
    {
        const string text = """
DIM name AS STRING = "Nuts"
IF name > "M" THEN
    PRINT "later"
END IF
IF "abc" < "abd" THEN
    PRINT "ordered"
END IF
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["later", "ordered"], result.Output);
    }

    [Fact]
    public void Run_SupportsSelectCase()
    {
        const string text = """
DIM command AS STRING = "SAVE"
DIM score AS INTEGER = 88

SELECT CASE command
    CASE "OPEN"
        PRINT "open"
    CASE "SAVE"
        PRINT "save"
    CASE ELSE
        PRINT "other"
END SELECT

SELECT CASE score
    CASE IS > 90
        PRINT "excellent"
    CASE 75 TO 90
        PRINT "good"
    CASE ELSE
        PRINT "retry"
END SELECT
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["save", "good"], result.Output);
    }

    [Fact]
    public void Run_SupportsSelectCaseStringRangeAndCaseIs()
    {
        const string text = """
DIM command AS STRING = "Nuts"
DIM letter AS STRING = "B"

SELECT CASE command
    CASE IS > "M"
        PRINT "after"
    CASE ELSE
        PRINT "before"
END SELECT

SELECT CASE letter
    CASE "A" TO "M"
        PRINT "first"
    CASE ELSE
        PRINT "second"
END SELECT
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["after", "first"], result.Output);
    }

    [Fact]
    public void Run_SupportsGroupedDimDeclarations()
    {
        const string text = """
DIM i AS INTEGER, j AS INTEGER
DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING
i = 3
j = 4
anotherVar = TRUE
choice = TRUE
birthDate = "2014-05-01"
PRINT i
PRINT j
PRINT anotherVar
PRINT choice
PRINT birthDate
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["3", "4", "TRUE", "TRUE", "2014-05-01"], result.Output);
    }

    [Fact]
    public void Run_SupportsInferredDimDeclarations()
    {
        const string text = """
DIM value = 10
DIM firstName = "Alice"
DIM ready = TRUE
DIM ratio = 1.5
PRINT value
PRINT firstName
PRINT ready
PRINT ratio
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["10", "Alice", "TRUE", "1.5"], result.Output);
    }

    [Fact]
    public void Run_SupportsInferredLongDeclarations()
    {
        const string text = """
DIM value = 21474836479
PRINT value
PRINT value + value
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["21474836479", "42949672958"], result.Output);
    }

    [Fact]
    public void Run_SupportsExplicitDecimalDeclarations()
    {
        const string text = """
DIM price AS DECIMAL = 10.10
DIM tax AS DECIMAL = 0.20
PRINT price + tax
PRINT (price + tax) / 2
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["10.30", "5.15"], result.Output);
    }

    [Fact]
    public void Run_SupportsExitFor()
    {
        const string text = """
FOR i = 1 TO 5
    IF i = 3 THEN
        EXIT FOR
    END IF
    PRINT i
NEXT
PRINT "done"
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["1", "2", "done"], result.Output);
    }

    [Fact]
    public void Run_SupportsExitDo()
    {
        const string text = """
DIM i AS INTEGER = 0
DO WHILE i < 5
    IF i = 2 THEN
        EXIT DO
    END IF
    PRINT i
    i = i + 1
LOOP
PRINT "done"
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["0", "1", "done"], result.Output);
    }

    [Fact]
    public void Run_SupportsBlockLocalsInsideBranches()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "yes"
    PRINT message
ELSE
    DIM message AS STRING = "no"
    PRINT message
END IF
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["yes"], result.Output);
    }

    [Fact]
    public void Run_SupportsFixedStringDeclarationsOnBytecodeVm()
    {
        const string text = """
DIM code AS STRING * 4

PRINT "[" + code + "]"

code = "AB"
PRINT "[" + code + "]"

code = "Alphabet"
PRINT "[" + code + "]"
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsFixedStringDeclarations()
    {
        const string text = """
DIM code AS STRING * 4

PRINT "[" + code + "]"

code = "AB"
PRINT "[" + code + "]"

code = "Alphabet"
PRINT "[" + code + "]"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], result.Output);
    }

    [Fact]
    public void Run_SupportsStringBuiltinsOnBytecodeVm()
    {
        const string text = """
PRINT LEFT("Hello", 3)
PRINT RIGHT("Hello", 3)
PRINT MID("Hello", 2, 4)
PRINT TRIM("  hello  ")
PRINT REPLACE("one two", "two", "three")
PRINT INSTR("banana", "na")
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT ASC(CHR(1071))
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["Hel", "llo", "ello", "hello", "one three", "3", "[.A..]", "1071"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsStringBuiltins()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT STRING(5, "ab")
PRINT INSERT("abcd", 3, "-")
PRINT REMOVE("abcdef", 2, 3)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "aaaaa", "ab-cd", "aef"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()
    {
        const string text = """
PRINT REPLACE("abc", "", "x")
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForRegexArrayBuiltins()
    {
        const string text = """
DIM matches() AS STRING
DIM extracted() AS STRING
DIM parts() AS STRING

matches = REGEXMATCHES("A12 b34 C56", "[a-z]\d+", "i")
extracted = REGEXEXTRACTALL("A12 b34 C56", "([a-z])(\d+)", 2, "i")
parts = REGEXSPLIT("A12,b34;C56", "[,;]")

PRINT ARRAYLENGTH(matches)
PRINT matches(1)
PRINT matches(3)
PRINT ARRAYLENGTH(extracted)
PRINT extracted(2)
PRINT ARRAYLENGTH(parts)
PRINT parts(2)
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["3", "A12", "C56", "3", "34", "3", "b34"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsRegexArrayBuiltinArgumentError()
    {
        const string text = """
DIM matches() AS STRING
matches = REGEXMATCHES("abc", "a", "q")
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXMATCHES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForRegexScalarBuiltins()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["TRUE", "42", "12", "one # two 34", "2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsRegexScalarBuiltinArgumentError()
    {
        const string text = """
PRINT REGEXEXTRACT("abc", "(a)", -1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXEXTRACT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForStringArrayBuiltins()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red|green|blue", "|")
PRINT ARRAYLENGTH(parts)
PRINT parts(2)
PRINT JOIN(parts, "/")
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["3", "green", "red/green/blue"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsStringArrayBuiltinArgumentError()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("abc", "")
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForPrintUsing()
    {
        const string text = """
PRINT USING "##.##"; 3.14159
PRINT USING "!"; "Hello"
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal([" 3.14", "H"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsPrintUsingRuntimeFailureFromVm()
    {
        const string text = """
PRINT USING "##"; "oops"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForInput()
    {
        const string text = """
DIM name AS STRING
DIM age AS INTEGER
INPUT "Pair"; name, age
PRINT name; age + 1
""";

        var inputProvider = new QueueInputProvider("Ada, 41");
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["Ada42"], result.Output);
        var request = Assert.Single(inputProvider.Requests);
        Assert.Equal("Pair", request.PromptText);
        Assert.Equal(2, request.ValueCount);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsInputRuntimeFailureFromVm()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, inputProvider: new QueueInputProvider("oops"), mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_SupportsNumericBuiltinsOnBytecodeVm()
    {
        const string text = """
PRINT INT(-3.2)
PRINT ROUND(2.5)
PRINT FIX(-3.8)
PRINT SQR(9)
PRINT POW(2, 3)
PRINT LOG2(8)
PRINT SETSIGN(5, -1)
DIM value AS DOUBLE = RND()
PRINT value >= 0
PRINT value < 1
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["-4", "3", "-3", "3", "8", "3", "-5", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForAutomaticNumericConversions()
    {
        const string text = """
DIM seed AS INTEGER = 1
DIM amount AS DECIMAL = seed
DIM half AS DECIMAL = 0.5
DIM wide AS LONG = seed
DIM angle AS INTEGER = 0
PRINT amount + half
PRINT wide + 7
PRINT SIN(angle)
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1.5", "8", "0"], result.Output);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()
    {
        const string text = """
PRINT LOG(0)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("LOG", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_SupportsDateTimeBuiltinsOnBytecodeVm()
    {
        const string text = """
PRINT DATESERIAL(2026, 4, 20)
PRINT TIMESERIAL(14, 5, 6)
PRINT DATEADD("m", 1, "2026-01-31")
PRINT DATEDIFF("d", "2026-04-20 23:59:59", "2026-04-21 00:00:00")
PRINT DATEPART("y", "2026-04-21")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20 14:05:06")
PRINT ISLEAPYEAR(2024)
PRINT DAYSINMONTH(2024, 2)
PRINT STARTOFMONTH("2026-04-20")
PRINT ENDOFYEAR("2026-04-20")
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026-02-28",
                "1",
                "111",
                "2026-04-20",
                "14:05:06",
                "TRUE",
                "TRUE",
                "29",
                "2026-04-01",
                "2026-12-31"
            ],
            result.Output);
    }

    [Fact]
    public void Run_SupportsCurrentDateTimeBuiltinsOnBytecodeVm()
    {
        const string text = """
PRINT ISDATE(NOW())
PRINT ISDATE(DATE())
PRINT ISDATE(TIME())
PRINT ISDATE(UTCNOW())
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()
    {
        const string text = """
PRINT DATEADD("ww", 1, "2026-04-20")
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("DATEADD", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("interval", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_SupportsNestedLoops()
    {
        const string text = """
DIM j AS INTEGER
FOR i = 1 TO 2
    j = 1
    WHILE j <= 2
        PRINT STR(i) + ":" + STR(j)
        j = j + 1
    END WHILE
NEXT
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["1:1", "1:2", "2:1", "2:2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsForLoop()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["1", "2", "3"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsStepZeroAsRuntimeFailure()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3004", diagnostic.Code);
    }

    [Fact]
    public void Run_SupportsLocalFunctionWithoutCaptureOnBytecodeVm()
    {
        const string text = """
PRINT Outer(10)

FUNCTION Outer(base AS INTEGER) AS INTEGER
    RETURN AddTwo(base)

    FUNCTION AddTwo(value AS INTEGER) AS INTEGER
        RETURN value + 2
    END FUNCTION
END FUNCTION
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsLocalFunctionWithoutCapture()
    {
        const string text = """
PRINT Outer(10)

FUNCTION Outer(base AS INTEGER) AS INTEGER
    RETURN AddTwo(base)

    FUNCTION AddTwo(value AS INTEGER) AS INTEGER
        RETURN value + 2
    END FUNCTION
END FUNCTION
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void Run_SupportsLocalSubWithoutCaptureOnBytecodeVm()
    {
        const string text = """
DIM total AS INTEGER = 0

Demo()
PRINT total

SUB Demo()
    AddOne()
    AddOne()

    SUB AddOne()
        total = total + 1
    END SUB
END SUB
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsLocalSubWithoutCapture()
    {
        const string text = """
DIM total AS INTEGER = 0

Demo()
PRINT total

SUB Demo()
    AddOne()
    AddOne()

    SUB AddOne()
        total = total + 1
    END SUB
END SUB
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForLocalFunctionCapture()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["23"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsLocalFunctionCapture()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["23"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_SupportsOuterLocalCapture()
    {
        const string text = """
PRINT Outer()

FUNCTION Outer() AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["7"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForLocalSubOuterLocalCapture()
    {
        const string text = """
SUB Outer()
    DIM total AS INTEGER = 2
    Increase()
    Increase()
    PRINT total

    SUB Increase()
        total = total + 1
    END SUB
END SUB

Outer()
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["4"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForArrayProgram()
    {
        const string text = """
DIM values() AS INTEGER
REDIM values(1 TO 2)
values(1) = 7
values(2) = 8
PRINT values(1)
PRINT ARRAYLENGTH(values)
PRINT UBOUND(values)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["7", "2", "2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalArrayProgram()
    {
        const string text = """
DIM grid(,) AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(1, 10) = 7
grid(2, 11) = 9
PRINT LBOUND(grid, 1)
PRINT UBOUND(grid, 1)
PRINT LBOUND(grid, 2)
PRINT UBOUND(grid, 2)
REDIM PRESERVE grid(2 TO 3, 11 TO 12)
PRINT grid(2, 11)
PRINT grid(3, 12)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["1", "2", "10", "11", "9", "0"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForPartialMultidimensionalIndexingProgram()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM row() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(2, 10) = 7
grid(2, 11) = 9
row = grid(2)
row(10) = 70
PRINT grid(2, 10)
PRINT row(11)
PRINT LBOUND(row)
PRINT UBOUND(row)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["70", "9", "10", "11"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForPureArrayBuiltins()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM clone() AS INTEGER
DIM sortedValues() AS INTEGER
DIM reversedValues() AS INTEGER
DIM uniqueValues() AS INTEGER

source(5) = 30
source(6) = 10
source(7) = 30

clone = ARRAYCLONE(source)
sortedValues = ARRAYTOSORTED(source)
reversedValues = ARRAYTOREVERSED(source)
uniqueValues = ARRAYDISTINCT(source)

PRINT LBOUND(clone)
PRINT UBOUND(clone)
PRINT sortedValues(5)
PRINT sortedValues(7)
PRINT reversedValues(1)
PRINT ARRAYLENGTH(uniqueValues)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["5", "7", "10", "30", "30", "2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYSKIP(source, -1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYSKIP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
DIM removed AS INTEGER
DIM popped AS INTEGER

values(5) = 3
values(6) = 1
ARRAYPUSH(values, 2)
ARRAYINSERT(values, 6, 4)
removed = ARRAYREMOVE(values, 7)
popped = ARRAYPOP(values)
ARRAYREVERSE(values)
ARRAYSORT(values)

PRINT removed
PRINT popped
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(5)
PRINT values(6)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["1", "2", "5", "6", "3", "4"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("array", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForArraySearchBuiltins()
    {
        const string text = """
DIM values(5 TO 9) AS INTEGER
DIM matches() AS INTEGER
DIM sorted(5 TO 9) AS INTEGER

values(5) = 10
values(6) = 20
values(7) = 10
values(8) = 30
values(9) = 20

sorted(5) = 10
sorted(6) = 20
sorted(7) = 20
sorted(8) = 30
sorted(9) = 40

matches = ARRAYFINDALL(values, 20)

PRINT ARRAYCONTAINS(values, 20)
PRINT ARRAYINDEXOF(values, 10)
PRINT ARRAYLASTINDEXOF(values, 20)
PRINT ARRAYCOUNT(values, 20)
PRINT ARRAYINDEXOF(values, 99)
PRINT matches(1)
PRINT matches(2)
PRINT ARRAYBINARYSEARCH(sorted, 20)
PRINT ARRAYBINARYSEARCH(sorted, 25)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["TRUE", "5", "9", "2", "4", "6", "9", "6", "4"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsArraySearchBuiltinRuntimeError()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForArrayCallbackBuiltins()
    {
        const string text = """
SUB RunCallbacks()
DIM values(1 TO 4) AS INTEGER
DIM filtered() AS INTEGER
DIM mapped() AS STRING
DIM threshold AS INTEGER = 2
DIM prefix AS STRING = "N"
DIM bonus AS INTEGER = 1

values(1) = 1
values(2) = 2
values(3) = 3
values(4) = 4

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsLarge)

filtered = ARRAYFILTER(values, IsLarge)
mapped = ARRAYMAP(filtered, FormatValue)

PRINT ARRAYLENGTH(filtered)
PRINT mapped(1)
PRINT mapped(2)
PRINT ARRAYREDUCE(values, 10, AddWithBonus)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION

FUNCTION AddWithBonus(accumulator AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN accumulator + value + bonus
END FUNCTION
END SUB

RunCallbacks()
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["TRUE", "TRUE", "2", "N3", "N4", "24"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForBuiltinFunctionCallbacks()
    {
        const string text = """
DIM names(1 TO 3) AS STRING
DIM upper() AS STRING
DIM lengths() AS INTEGER
DIM dateTexts(1 TO 3) AS STRING
DIM validDates() AS STRING

names(1) = "ada"
names(2) = "Grace"
names(3) = "linus"

dateTexts(1) = "2026-04-24"
dateTexts(2) = "not-a-date"
dateTexts(3) = "2026-05-01"

upper = ARRAYMAP(names, UCASE)
lengths = ARRAYMAP(names, LEN)
validDates = ARRAYFILTER(dateTexts, ISDATE)

PRINT upper(1)
PRINT upper(3)
PRINT lengths(2)
PRINT ARRAYLENGTH(validDates)
PRINT validDates(2)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["ADA", "LINUS", "5", "2", "2026-05-01"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForTypeFields()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Employee
    Name AS STRING * 5
    Age AS INTEGER
    Home AS Address
END TYPE

DIM first AS Employee
DIM second AS Employee

PRINT "[" + first.Name + "]"
first.Name = "Ada"
first.Age = 36
first.Home.City = "London"
second = first
first.Name = "Grace"
PRINT "[" + second.Name + "]"
PRINT second.Age
PRINT second.Home.City
PRINT "[" + first.Name + "]"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Null(result.InterpreterFallbackReason);
        Assert.Equal(["[     ]", "[Ada  ]", "36", "London", "[Grace]"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var filePath = Path.Combine(workspace, "note.txt");
            var nestedDirectory = Path.Combine(workspace, "nested");

            var text = $$"""
DIM filePath AS STRING = "{{EscapePath(filePath)}}"
DIM nestedDirectory AS STRING = "{{EscapePath(nestedDirectory)}}"

PRINT WRITEFILE(filePath, "hello")
PRINT APPENDFILE(filePath, "-vm")
PRINT READFILE(filePath)
PRINT FILEEXISTS(filePath)
PRINT MAKEDIR(nestedDirectory)
PRINT DIREXISTS(nestedDirectory)
DIM files() AS STRING
files = GETFILES("{{EscapePath(workspace)}}", "*.txt")
PRINT ARRAYLENGTH(files)
PRINT DELETEFILE(filePath)
PRINT DELETEDIR(nestedDirectory)
""";

            var pipeline = new AxiomExecutionPipeline();
            var result = pipeline.Run(new ExecutionRequest(text));

            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["TRUE", "TRUE", "hello-vm", "TRUE", "TRUE", "TRUE", "1", "TRUE", "TRUE"], result.Output);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var missingPath = Path.Combine(workspace, "missing.txt");
            var text = $$"""
PRINT READFILE("{{EscapePath(missingPath)}}")
""";

            var pipeline = new AxiomExecutionPipeline();
            var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("READFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var inputPath = Path.Combine(workspace, "input.txt");
            var outputPath = Path.Combine(workspace, "output.txt");
            File.WriteAllText(inputPath, string.Join(Environment.NewLine, ["first", "second"]));

            var text = $$"""
DIM source AS FILE
OPEN "{{EscapePath(inputPath)}}" FOR INPUT AS source
PRINT READLINE(source)
PRINT READTOEND(source)
CLOSE source
PRINT ISOPEN(source)

DIM target AS FILE
OPEN "{{EscapePath(outputPath)}}" FOR OUTPUT AS target
PRINT WRITELINE(target, "saved")
CLOSE target
""";

            var pipeline = new AxiomExecutionPipeline();
            var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(["first", "second", "FALSE", "TRUE"], result.Output);
            Assert.Equal($"saved{Environment.NewLine}", File.ReadAllText(outputPath));
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var missingPath = Path.Combine(workspace, "missing.txt");
            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(missingPath)}}" FOR INPUT AS f
""";

            var pipeline = new AxiomExecutionPipeline();
            var result = pipeline.Run(new ExecutionRequest(text, mode: ExecutionMode.Bytecode));

            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("OPEN", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Run_SupportsDoLoopVariants()
    {
        const string text = """
DIM i AS INTEGER = 0
DO WHILE i < 3
    PRINT i
    i = i + 1
LOOP
DO
    PRINT i
    i = i + 1
LOOP UNTIL i = 5
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["0", "1", "2", "3", "4"], result.Output);
    }

    [Fact]
    public void Run_SupportsTrailingCommaWithoutFixedZones()
    {
        var result = Run("PRINT \"ABCD\",");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["ABCD "], result.Output);
    }

    [Fact]
    public void Run_ReturnsSyntaxFailureAndDoesNotExecute()
    {
        const string text = """
IF TRUE
    PRINT 1
END IF
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SyntaxFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code.StartsWith("MB1", StringComparison.Ordinal));
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Theory]
    [InlineData("PRINT 001 + 1")]
    [InlineData("PRINT RIGHT(\"Hello\", 000+3)")]
    public void Run_ReturnsSyntaxFailureForLeadingZeroNumericLiteral(string sourceText)
    {
        var output = new CollectingOutputWriter();
        var result = Run(sourceText, outputWriter: output);

        Assert.Equal(ExecutionStatus.SyntaxFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB0003");
        Assert.Contains("нули", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Theory]
    [InlineData("PRINT 9223372036854775808")]
    [InlineData("PRINT 9999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999.0")]
    public void Run_ReturnsSyntaxFailureForOutOfRangeNumericLiteral(string sourceText)
    {
        var output = new CollectingOutputWriter();
        var result = Run(sourceText, outputWriter: output);

        Assert.Equal(ExecutionStatus.SyntaxFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB0004");
        Assert.Contains("диапазон", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureAndDoesNotExecute()
    {
        var output = new CollectingOutputWriter();
        var result = Run("PRINT missingValue", outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2001");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForExitForOutsideFor()
    {
        const string text = """
WHILE TRUE
    EXIT FOR
END WHILE
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2013");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForExitDoOutsideDo()
    {
        const string text = """
WHILE TRUE
    EXIT DO
END WHILE
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2014");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForBooleanOrderedComparison()
    {
        var output = new CollectingOutputWriter();
        var result = Run("PRINT TRUE > FALSE", outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2003");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForSelectCaseRangeOnBoolean()
    {
        const string text = """
DIM flag AS BOOLEAN = TRUE
SELECT CASE flag
    CASE FALSE TO TRUE
        PRINT flag
END SELECT
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2015");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForUsingForVariableOutsideLoop()
    {
        const string text = """
FOR i = 1 TO 2
    PRINT i
NEXT
PRINT i
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2001");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForUsingIfLocalOutsideBranch()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "inside"
    PRINT message
END IF
PRINT message
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2001");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsRuntimeFailure()
    {
        var result = Run("PRINT 1 / 0");

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3001");
        Assert.Contains("ноль", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(9, diagnostic.Location.Column);
    }

    [Theory]
    [InlineData("DIM value AS INTEGER = 2147483647\r\nPRINT value + 1", "INTEGER")]
    [InlineData("DIM value AS LONG = 9223372036854775807\r\nPRINT value + 1", "LONG")]
    [InlineData("DIM value AS DOUBLE = VAL(\"1e308\")\r\nPRINT value * 10", "DOUBLE")]
    [InlineData("DIM value AS DECIMAL = 79228162514264337593543950335.0\r\nPRINT value * 10", "DECIMAL")]
    public void Run_ReturnsRuntimeFailureForNumericOverflow(string sourceText, string expectedType)
    {
        var result = Run(sourceText);

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3008");
        Assert.Contains(expectedType, diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_CapturesOutputThroughAbstraction()
    {
        var output = new CollectingOutputWriter();
        var result = Run("PRINT \"hello\"", outputWriter: output);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["hello"], result.Output);
        Assert.Equal(["hello"], output.Output);
    }

    [Fact]
    public void Run_UsesInputProvider()
    {
        const string text = """
DIM name AS STRING
INPUT name
PRINT name
""";

        var inputProvider = new QueueInputProvider("Ada");
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["Ada"], result.Output);
        var request = Assert.Single(inputProvider.Requests);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Run_UsesInputProviderForMultipleValues()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
PRINT firstName; age
""";

        var inputProvider = new QueueInputProvider("Ada, 42");
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["Ada42"], result.Output);
        Assert.Equal(2, Assert.Single(inputProvider.Requests).ValueCount);
    }

    [Fact]
    public void Run_UsesInputProviderForPromptedInput()
    {
        const string text = """
DIM value AS INTEGER
INPUT "Value"; value
PRINT value + 1
""";

        var inputProvider = new QueueInputProvider("41");
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["42"], result.Output);
        Assert.Equal("Value", Assert.Single(inputProvider.Requests).PromptText);
    }

    [Fact]
    public void Run_ReportsInvalidMultiInputHandling()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT firstName, age
""";

        var result = Run(text, inputProvider: new QueueInputProvider("Ada"));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3003");
        Assert.Contains("2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Run_PreservesMultipleDiagnostics()
    {
        const string text = """
PRINT firstMissing
PRINT secondMissing
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.All(result.Diagnostics, static diagnostic => Assert.Equal("MB2001", diagnostic.Code));
    }

    [Fact]
    public void Run_PreservesMappedRuntimeDiagnosticLocations()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var result = Run(text, inputProvider: new QueueInputProvider("oops"));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3003");
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Run_ReportsDiagnosticsToSink()
    {
        var sink = new CollectingDiagnosticSink();
        var result = Run("PRINT missingValue", diagnosticSink: sink);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Single(sink.Diagnostics);
        Assert.Equal(result.Diagnostics[0].Code, sink.Diagnostics[0].Code);
    }

    private static ExecutionResult Run(
        string sourceText,
        IInputProvider? inputProvider = null,
        IOutputWriter? outputWriter = null,
        IDiagnosticSink? diagnosticSink = null)
    {
        var request = new ExecutionRequest(sourceText, inputProvider, outputWriter, diagnosticSink);
        var pipeline = new AxiomExecutionPipeline();
        return pipeline.Run(request);
    }

    private static string CreateTemporaryWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "axiom-exec-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTemporaryWorkspace(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string EscapePath(string path)
    {
        return path.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static string CreateTemporarySnapshotPath()
    {
        return Path.Combine(Path.GetTempPath(), $"axiom-bytecode-{Guid.NewGuid():N}.json");
    }

    private static void DeleteTemporaryFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class QueueInputProvider : IInputProvider
    {
        private readonly Queue<string> _values;

        public QueueInputProvider(params string[] values)
        {
            _values = new Queue<string>(values);
        }

        public List<InputRequest> Requests { get; } = [];

        public string ReadInput(InputRequest request)
        {
            Requests.Add(request);
            return _values.Count > 0 ? _values.Dequeue() : string.Empty;
        }
    }

    private sealed class CollectingOutputWriter : IOutputWriter
    {
        public List<string> Output { get; } = [];

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }
    }

    private sealed class CollectingDiagnosticSink : IDiagnosticSink
    {
        public List<Diagnostic> Diagnostics { get; } = [];

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }
}

