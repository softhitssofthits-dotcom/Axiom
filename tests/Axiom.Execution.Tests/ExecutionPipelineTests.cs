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
    public void Compile_ReturnsDiagnosticWhenBytecodeSnapshotWriteFails()
    {
        var pipeline = new AxiomExecutionPipeline();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var snapshotDirectory = Path.Combine(root!.FullName, ".artifacts", "snapshot-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(snapshotDirectory);

        try
        {
            var result = pipeline.Compile(new CompilationRequest("PRINT 1 + 2", bytecodeSnapshotPath: snapshotDirectory));

            Assert.Equal(CompilationStatus.BackendFailure, result.Status);
            Assert.True(result.FrontendSucceeded);
            Assert.False(result.Succeeded);
            Assert.False(result.BackendSucceeded);
            Assert.False(result.BytecodeAvailable);
            var diagnostic = Assert.Single(result.BackendDiagnostics);
            Assert.Equal("MB4003", diagnostic.Code);
            Assert.Contains("Не удалось записать JSON-снимок байткода", diagnostic.Message, StringComparison.Ordinal);

            var followUp = pipeline.Run(new ExecutionRequest("PRINT 7"));
            Assert.Equal(ExecutionStatus.Success, followUp.Status);
            Assert.Equal(["7"], followUp.Output);
        }
        finally
        {
            if (Directory.Exists(snapshotDirectory))
            {
                Directory.Delete(snapshotDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void CompileSnapshotFailureIsNotFullCompilationSuccess()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var directory = Path.Combine(root!.FullName, ".artifacts",
            "snapshot-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var result = new AxiomExecutionPipeline().Compile(
                new CompilationRequest("PRINT 3", bytecodeSnapshotPath: directory));
            Assert.Equal(CompilationStatus.BackendFailure, result.Status);
            Assert.True(result.FrontendSucceeded);
            Assert.False(result.Succeeded);
            Assert.False(result.BackendSucceeded);
            Assert.Equal("MB4003", Assert.Single(result.BackendDiagnostics).Code);
        }
        finally
        {
            Directory.Delete(directory);
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
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.True(result.RuntimeExecutionDuration >= TimeSpan.Zero);
    }

    [Fact]
    public void Run_WithWarningDiagnostic_SucceedsAndCarriesWarning()
    {
        var result = Run("""
            SUB Foo()
            END SUB

            DIM Foo AS INTEGER = 7
            PRINT Foo
            """);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["7"], result.Output);

        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.NameClassCollision, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void Run_WithForCounterReuse_ReportsError()
    {
        var result = Run("""
            DIM i AS INTEGER

            FOR i = 1 TO 3
                PRINT i
            NEXT
            """);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);

        var error = Assert.Single(result.Diagnostics, static d => d.Code == "MB2016");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("локальный", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_WithImplicitForCounter_SucceedsWithoutDiagnostics()
    {
        var result = Run("""
            FOR i = 1 TO 3
                PRINT i
            NEXT
            """);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1", "2", "3"], result.Output);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Run_WithCounterValueCopiedInsideBody_Succeeds()
    {
        var result = Run("""
            DIM afterValue AS INTEGER

            FOR i = 1 TO 3
                PRINT i
                afterValue = i
            NEXT

            PRINT "after="; afterValue
            """);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1", "2", "3", "after=3"], result.Output);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForCoveredProgram()
    {
        var pipeline = new AxiomExecutionPipeline();

        var result = pipeline.Run(new ExecutionRequest(
            "PRINT 1 + 2"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
            sourceText));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

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
        var result = pipeline.Run(new ExecutionRequest(text));

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
    public void Run_SelectCaseExecutesOnlyFirstMatchingBranch()
    {
        const string text = """
DIM value AS INTEGER = 5

SELECT CASE value
    CASE 5
        PRINT "first"
    CASE 5
        PRINT "second"
    CASE ELSE
        PRINT "else"
END SELECT
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["first"], result.Output);
    }

    [Fact]
    public void Run_SelectCaseWithoutMatchAndElseContinuesAfterEndSelect()
    {
        const string text = """
DIM value AS INTEGER = 10

SELECT CASE value
    CASE 1
        PRINT "one"
    CASE 2
        PRINT "two"
END SELECT

PRINT "after"
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["after"], result.Output);
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
    public void Run_SupportsContinueFor()
    {
        const string text = """
FOR i = 1 TO 5
    IF i = 3 THEN
        CONTINUE FOR
    END IF
    PRINT STR(i)
NEXT
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1", "2", "4", "5"], result.Output);
    }

    [Fact]
    public void Run_SupportsContinueWhile()
    {
        const string text = """
DIM i AS INTEGER = 0
WHILE i < 5
    i = i + 1
    IF i = 3 THEN
        CONTINUE WHILE
    END IF
    PRINT STR(i)
END WHILE
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1", "2", "4", "5"], result.Output);
    }

    [Fact]
    public void Run_SupportsContinueDo()
    {
        const string text = """
DIM i AS INTEGER = 0
DO WHILE i < 5
    i = i + 1
    IF i = 3 THEN
        CONTINUE DO
    END IF
    PRINT STR(i)
LOOP
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["1", "2", "4", "5"], result.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForContinueForOutsideFor()
    {
        const string text = """
WHILE TRUE
    CONTINUE FOR
END WHILE
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2037");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForContinueWhileOutsideWhile()
    {
        const string text = """
FOR i = 1 TO 3
    CONTINUE WHILE
NEXT
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2038");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
    }

    [Fact]
    public void Run_ReturnsSemanticFailureForContinueDoOutsideDo()
    {
        const string text = """
FOR i = 1 TO 3
    CONTINUE DO
NEXT
""";

        var output = new CollectingOutputWriter();
        var result = Run(text, outputWriter: output);

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2039");
        Assert.Empty(result.Output);
        Assert.Empty(output.Output);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
PRINT ASCW(CHR(1071))
PRINT CHR(219)
PRINT CHRW(9608)
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["Hel", "llo", "ello", "hello", "one three", "3", "[.A..]", "159", "1071", "█", "█"], result.Output);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "aaaaa", "ab-cd", "aef"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForInKey()
    {
        const string text = """
PRINT INKEY()
PRINT INKEY()
PRINT INKEY()
""";

        var result = Run(
            text,
            inputProvider: new QueueKeyboardInputProvider("A", "LEFT"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["A", "LEFT", ""], result.Output);
    }

    [Fact]
    public void Run_InKeyWithoutKeyboardProviderReturnsEmptyString()
    {
        var result = Run(
            "PRINT INKEY()",
            inputProvider: new QueueInputProvider());

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal([""], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForKeyDown()
    {
        const string text = """
PRINT KEYDOWN("LEFT")
PRINT KEYDOWN("CTRL") AND KEYDOWN("LEFT")
PRINT KEYDOWN("RIGHT")
PRINT INKEY()
""";

        var result = Run(
            text,
            inputProvider: new QueueKeyboardInputProvider(keys: [], pressedKeys: ["LEFT", "CTRL"]));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["TRUE", "TRUE", "FALSE", ""], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForKeyPress()
    {
        const string text = """
PRINT KEYPRESS("LEFT")
PRINT KEYPRESS("LEFT")
PRINT KEYPRESS("ESCAPE")
PRINT INKEY()
""";

        var result = Run(
            text,
            inputProvider: new QueueKeyboardInputProvider("LEFT", "ESC", "A"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "A"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMouseFunctions()
    {
        const string text = """
SCREEN 1
DIM x AS LONG = MOUSEX()
DIM y AS LONG = MOUSEY()
DIM leftDown AS BOOLEAN = MOUSEBUTTON(1)
DIM rightDown AS BOOLEAN = MOUSEBUTTON(2)
SCREEN 0
PRINT x
PRINT y
PRINT leftDown
PRINT rightDown
""";

        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, pressedMouseButtons: [1]);
        var result = Run(
            text,
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["320", "240", "TRUE", "FALSE"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForGraphicsStepAndPointState()
    {
        var inputProvider = new QueueKeyboardInputProvider();
        var result = Run(
            """
SCREEN 1
CLS
COLOR 6, 2
PSET (10, 20)
PRINT "point0="; POINT(0); " point1="; POINT(1)
PSET STEP (5, 4)
PRINT "after pset step="; POINT(0); ","; POINT(1)
LINE -(30, 40)
PRINT "after line from current="; POINT(0); ","; POINT(1)
LINE STEP (5, 5)-STEP (10, 15)
PRINT "after line step-step="; POINT(0); ","; POINT(1)
PRINT "pixel 10,20="; POINT(10, 20)
PRINT "pixel 15,24="; POINT(15, 24)
""",
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "point0=10 point1=20",
                "after pset step=15,24",
                "after line from current=30,40",
                "after line step-step=45,60",
                "pixel 10,20=6",
                "pixel 15,24=6"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForViewScreenAndRelativeCoordinates()
    {
        var inputProvider = new QueueKeyboardInputProvider();
        var result = Run(
            """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20), 3, 9
PSET (11, 11), 6
PRINT "abs border="; POINT(10, 10); " abs fill="; POINT(12, 12)
VIEW (1, 1)-(3, 3), 2, 8
PSET (0, 0), 7
PRINT "rel pixel="; POINT(0, 0)
PRINT "rel pos="; POINT(0); ","; POINT(1)
VIEW
PRINT "reset pos="; POINT(0); ","; POINT(1)
""",
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "abs border=9 abs fill=3",
                "rel pixel=7",
                "rel pos=0,0",
                "reset pos=11,11"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForWindowScreenCartesianAndDraw()
    {
        var inputProvider = new QueueKeyboardInputProvider();
        var result = Run(
            """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20)
WINDOW SCREEN (100, 200)-(110, 210)
PSET (104, 205), 6
DRAW "R1"
PRINT "draw pixel="; POINT(105, 205)
PRINT "screen pos="; POINT(0); ","; POINT(1)
PRINT "screen logical="; POINT(2); ","; POINT(3)
WINDOW
PRINT "reset pos="; POINT(0); ","; POINT(1); " logical="; POINT(2); ","; POINT(3)
VIEW (2, 3)-(8, 9)
PRINT "view pos="; POINT(0); ","; POINT(1); " logical="; POINT(2); ","; POINT(3)
WINDOW (100, 210)-(110, 200)
PSET (104, 205), 7
PRINT "cart pixel="; POINT(104, 205)
PRINT "cart pos="; POINT(0); ","; POINT(1)
PRINT "cart logical="; POINT(2); ","; POINT(3)
""",
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "draw pixel=15",
                "screen pos=15,15",
                "screen logical=105,205",
                "reset pos=15,15 logical=15,15",
                "view pos=3,2 logical=3,2",
                "cart pixel=7",
                "cart pos=2,3",
                "cart logical=104,205"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForPMapAcrossViewAndWindow()
    {
        var inputProvider = new QueueKeyboardInputProvider();
        var result = Run(
            """
SCREEN 1
VIEW SCREEN (10, 10)-(20, 20)
WINDOW SCREEN (100, 200)-(110, 210)
PRINT "abs="; PMAP(104, 0); ","; PMAP(205, 1)
PRINT "back="; PMAP(14, 2); ","; PMAP(15, 3)
VIEW (2, 3)-(8, 9)
WINDOW
PRINT "view only="; PMAP(3, 0); ","; PMAP(2, 1); " back="; PMAP(3, 2); ","; PMAP(2, 3)
WINDOW SCREEN (100, 200)-(110, 210)
PRINT "frac="; PMAP(102.5, 0); ","; PMAP(1.5, 2)
WINDOW (100, 210)-(110, 200)
PRINT "cart="; PMAP(105, 0); ","; PMAP(205, 1)
PRINT "cart back="; PMAP(3, 2); ","; PMAP(3, 3)
""",
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(
            [
                "abs=14,15",
                "back=104,205",
                "view only=3,2 back=3,2",
                "frac=1.5,102.5",
                "cart=3,3",
                "cart back=105,205"
            ],
            inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_DrawLineFallbackWorksWithoutSpecialGraphicsHost()
    {
        const string text = """
SCREEN 1
LINE (1, 1)-(4, 1), 12
LINE (6, 6)-(8, 8), 9, B
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(2, 1)
PRINT POINT(3, 1)
PRINT POINT(6, 6)
PRINT POINT(7, 7)
PRINT POINT(8, 8)
""";

        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: []);
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["8", "8", "12", "12", "9", "0", "9"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_DrawCircleFallbackWorksWithoutSpecialGraphicsHost()
    {
        const string text = """
SCREEN 1
CIRCLE (10, 10), 3, 14
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(13, 10)
PRINT POINT(7, 10)
PRINT POINT(10, 13)
PRINT POINT(10, 7)
""";

        var inputProvider = new MinimalGraphicsHostInputProvider();
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["10", "10", "14", "14", "14", "14"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_GraphicsOperatorsMaintainPositionAcrossStepArcAndPaint()
    {
        const string text = """
SCREEN 1
COLOR 15, 2
PSET (10, 10), 9
LINE STEP (0, 0)-STEP (4, 0), 9
CIRCLE STEP (6, 0), 2, 14, -PI / 2, PI / 2
PRINT POINT(0)
PRINT POINT(1)
LINE (13, 8)-(17, 12), 9, B
PAINT STEP (0, 0), , 9
PRINT POINT(0)
PRINT POINT(1)
PRINT POINT(15, 10)
PRINT POINT(17, 10)
""";

        var inputProvider = new MinimalGraphicsHostInputProvider();
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["20", "10", "17", "12", "0", "9"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForGetPutTransfers()
    {
        const string text = """
SCREEN 1
DIM buffer(1 TO 2, 1 TO 2) AS INTEGER
PSET (10, 10), 1
PSET (11, 10), 2
PSET (10, 11), 3
PSET (11, 11), 4
GET (11, 11)-(10, 10), buffer
LINE (10, 10)-(11, 11), 0, BF
PUT (10, 10), buffer
PRINT buffer(1, 1)
PRINT buffer(2, 1)
PRINT buffer(1, 2)
PRINT buffer(2, 2)
PRINT POINT(10, 10)
PRINT POINT(11, 10)
PRINT POINT(10, 11)
PRINT POINT(11, 11)
""";

        var inputProvider = new MinimalGraphicsHostInputProvider();
        var result = Run(text, inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["1", "2", "3", "4", "1", "2", "3", "4"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_DrawCircleFallbackIgnoresNonFiniteAngles()
    {
        Assert.False(GraphicsCircleValidation.HasValidArguments(3d, double.NaN, null, 1d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(3d, null, double.PositiveInfinity, 1d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(3d, null, double.NegativeInfinity, 1d));
        Assert.False(GraphicsCircleValidation.HasValidArguments(3d, 0d, Math.PI, 1_000_001d));
    }

    [Fact]
    public void Run_WithBytecodeMode_DrawLineFallbackSurvivesErrorDoublingOverflow()
    {
        Assert.Equal(long.MaxValue, GraphicsLineMath.SaturatingDouble(long.MaxValue / 2 + 1));
        Assert.Equal(long.MinValue, GraphicsLineMath.SaturatingDouble(long.MinValue / 2 - 1));
        Assert.Equal(-8642L, GraphicsLineMath.SaturatingDouble(-4321));
        Assert.Equal(long.MaxValue, GraphicsLineMath.SaturatingAdd(long.MaxValue - 1, 10));
        Assert.Equal(long.MinValue, GraphicsLineMath.SaturatingAdd(long.MinValue + 1, -10));
        Assert.Equal(77L, GraphicsLineMath.SaturatingAdd(70, 7));
    }

    [Fact]
    public void Run_MouseFunctionsWithoutActiveScreenReturnDiagnostic()
    {
        var result = Run(
            """
PRINT MOUSEX()
PRINT MOUSEY()
PRINT MOUSEBUTTON(1)
""",
            inputProvider: new QueueInputProvider());

        Assert.Equal(ExecutionStatus.SemanticFailure, result.Status);
        Assert.Null(result.ActualEngine);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "MB2046");
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMousePress()
    {
        const string text = """
SCREEN 1
DIM firstLeft AS BOOLEAN = MOUSEPRESS(1)
DIM secondLeft AS BOOLEAN = MOUSEPRESS(1)
DIM middle AS BOOLEAN = MOUSEPRESS(3)
SCREEN 0
PRINT firstLeft
PRINT secondLeft
PRINT middle
""";

        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mousePresses: [1, 3]);
        var result = Run(
            text,
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["TRUE", "FALSE", "TRUE"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForOnKey()
    {
        const string text = """
PRINT "ready"
ON KEY("LEFT"), HandleLeft
DIM marker AS INTEGER = 1
PRINT INKEY()

SUB HandleLeft()
    PRINT "handler"
END SUB
""";

        var result = Run(
            text,
            inputProvider: new QueueKeyboardInputProvider("LEFT", "A"));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["ready", "handler", "A"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForOnMouse()
    {
        const string text = """
SCREEN 1
DIM handled AS BOOLEAN = FALSE
DIM handledButton AS INTEGER = 0
DIM handledX AS LONG = 0
DIM handledY AS LONG = 0
ON MOUSE(1), HandleMouse
DIM marker AS INTEGER = 1
DIM consumed AS BOOLEAN = MOUSEPRESS(1)
SCREEN 0
PRINT handled
PRINT handledButton
PRINT handledX
PRINT handledY
PRINT consumed

SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)
    handled = TRUE
    handledButton = button
    handledX = x
    handledY = y
END SUB
""";

        var inputProvider = new QueueKeyboardInputProvider(keys: [], pressedKeys: [], mouseX: 320, mouseY: 240, mousePresses: [1]);
        var result = Run(
            text,
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["TRUE", "1", "320", "240", "FALSE"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForOnMouseWheel()
    {
        const string text = """
SCREEN 1
DIM handled AS BOOLEAN = FALSE
DIM handledDelta AS INTEGER = 0
DIM handledX AS LONG = 0
DIM handledY AS LONG = 0
ON MOUSE("WHEEL"), HandleMouse
DIM marker AS INTEGER = 1
SCREEN 0
PRINT handled
PRINT handledDelta
PRINT handledX
PRINT handledY

SUB HandleMouse(delta AS INTEGER, x AS LONG, y AS LONG)
    handled = TRUE
    handledDelta = delta
    handledX = x
    handledY = y
END SUB
""";

        var inputProvider = new QueueKeyboardInputProvider(
            keys: [],
            pressedKeys: [],
            mouseX: 320,
            mouseY: 240,
            mouseEvents: [new MouseInputEvent(MouseInputEventKind.Wheel, -120)]);
        var result = Run(
            text,
            inputProvider: inputProvider);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Output);
        Assert.Equal(["TRUE", "-120", "320", "240"], inputProvider.GraphicsOutput);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()
    {
        const string text = """
PRINT REPLACE("abc", "", "x")
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        Assert.Equal(["TRUE", "42", "12", "one # two 34", "2"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsRegexScalarBuiltinArgumentError()
    {
        const string text = """
PRINT REGEXEXTRACT("abc", "(a)", -1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        Assert.Equal([" 3.14", "H"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsPrintUsingRuntimeFailureFromVm()
    {
        const string text = """
PRINT USING "##"; "oops"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        Assert.Equal(["Ada42"], result.Output);
        var request = Assert.Single(inputProvider.Requests);
        Assert.Equal("Pair? ", request.PromptText);
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
        var result = pipeline.Run(new ExecutionRequest(text, inputProvider: new QueueInputProvider("oops")));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()
    {
        const string text = """
PRINT LOG(0)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
PRINT DATEADD("m", 1, DATEVALUE("2026-01-31"))
DIM pdt1 AS DATETIME = "2026-04-20 23:59:59"
DIM pdt2 AS DATETIME = "2026-04-21 00:00:00"
PRINT DATEDIFF("d", pdt1, pdt2)
PRINT DATEPART("y", DATEVALUE("2026-04-21"))
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
        Assert.Equal(["TRUE", "TRUE", "TRUE", "TRUE"], result.Output);
    }

    [Fact]
    public void Run_SupportsNativeDateTimeBuiltinsOnBytecodeVm()
    {
        const string text = """
DIM d AS DATE = DATESERIAL(2026, 1, 31)
PRINT DATEADD("m", 1, d)
DIM t AS TIME = TIMESERIAL(14, 5, 59)
PRINT DATEADD("n", 1, t)
DIM dt AS DATETIME = "2026-04-20 23:05:06"
PRINT DATEADD("h", 2, dt)
PRINT DATEDIFF("m", d, DATEADD("m", 2, d))
PRINT DATEDIFF("n", t, DATEADD("n", 1, t))
PRINT DATEDIFF("d", dt, DATEADD("d", 1, dt))
PRINT DATEPART("q", d)
PRINT DATEPART("h", t)
PRINT DATEPART("y", dt)
""";

        var result = Run(text);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "2026-02-28",
                "14:06:59",
                "2026-04-21 01:05:06",
                "2",
                "1",
                "1",
                "1",
                "14",
                "110"
            ],
            result.Output);
    }

    [Fact]
    public void Run_UsesProvidedRuntimeClockForCurrentDateTimeBuiltins()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT FIX(TIMER())
""";

        var localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Axiom Execution Tests East",
            TimeSpan.FromHours(3),
            "Axiom Execution Tests East",
            "Axiom Execution Tests East");
        var runtimeClock = new FixedRuntimeClock(
            new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3)),
            localTimeZone,
            new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axiom Execution Tests East"] = localTimeZone,
                ["UTC"] = TimeZoneInfo.Utc
            });

        var result = Run(text, runtimeClock: runtimeClock);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026-04-20 14:05:06",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "50706"
            ],
            result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()
    {
        const string text = """
PRINT DATEADD("ww", 1, DATEVALUE("2026-04-20"))
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["12"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsFinallyBeforeFunctionReturn()
    {
        const string text = """
PRINT Compute()

FUNCTION Compute() AS INTEGER
    TRY
        RETURN 42
    FINALLY
        PRINT "cleanup"
    END TRY
END FUNCTION
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["cleanup", "42"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsFinallyBeforeExitForLeavesTry()
    {
        const string text = """
FOR i = 1 TO 1
    TRY
        PRINT "before"
        EXIT FOR
    FINALLY
        PRINT "cleanup"
    END TRY

    PRINT "unreachable"
NEXT

PRINT "after"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["before", "cleanup", "after"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_DoesNotRunFinallyBeforeExitForInsideSameTry()
    {
        const string text = """
TRY
    FOR i = 1 TO 1
        EXIT FOR
    NEXT

    PRINT "inside"
FINALLY
    PRINT "cleanup"
END TRY

PRINT "after"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["inside", "cleanup", "after"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsFinallyBeforeExitDoLeavesTry()
    {
        const string text = """
DO
    TRY
        PRINT "before"
        EXIT DO
    FINALLY
        PRINT "cleanup"
    END TRY

    PRINT "unreachable"
LOOP

PRINT "after"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["before", "cleanup", "after"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsNestedFinallyBlocksInsideOut()
    {
        const string text = """
TRY
    TRY
        ERROR "INNER", "inner failure"
    FINALLY
        PRINT "inner cleanup"
    END TRY
FINALLY
    PRINT "outer cleanup"
END TRY
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["inner cleanup", "outer cleanup"], result.Output);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INNER", diagnostic.Code);
    }

    [Fact]
    public void Run_WithBytecodeMode_AllowsOuterCatchToHandleErrorFromFinally()
    {
        const string text = """
TRY
    TRY
        ERROR "INNER", "inner failure"
    CATCH err
        PRINT err.Code
    FINALLY
        ERROR "CLEANUP", "cleanup failure"
    END TRY
CATCH outerErr
    PRINT outerErr.Code
    PRINT outerErr.Message
END TRY

PRINT "after"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["INNER", "CLEANUP", "cleanup failure", "after"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RethrowsCaughtRuntimeError()
    {
        const string text = """
TRY
    TRY
        ERROR "APP003", "Original failure"
    CATCH err
        PRINT "inner"
        ERROR err
    END TRY
CATCH outerErr
    PRINT outerErr.Code
    PRINT outerErr.Message
    PRINT outerErr.Line
END TRY
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["inner", "APP003", "Original failure", "3"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsFinallyWhenCatchBodyRethrows()
    {
        const string text = """
TRY
    TRY
        ERROR "APP010", "Original failure"
    CATCH inner
        ERROR inner
    FINALLY
        PRINT "inner-cleanup"
    END TRY
CATCH outer
    PRINT outer.Code
END TRY

PRINT "after"
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["inner-cleanup", "APP010", "after"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsFinallyWhenCatchBodyRaisesUnhandledError()
    {
        const string text = """
TRY
    ERROR "APP011"
CATCH err
    ERROR "APP012", "Replacement failure"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["cleanup"], result.Output);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("APP012", diagnostic.Code);
    }

    [Theory]
    [InlineData(ExecutionEngineKind.BytecodeVm)]
    public void Run_PowerDomainErrorMatchesInBytecodeVm(ExecutionEngineKind expectedEngine)
    {
        const string text = "PRINT (-8) ^ (1.0 / 3.0)";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(expectedEngine, result.ActualEngine);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3012", diagnostic.Code);
        Assert.Equal("Оператор '^' не определён для отрицательного основания и нецелой степени.", diagnostic.Message);
    }

    [Theory]
    [InlineData(ExecutionEngineKind.BytecodeVm)]
    public void Run_UnifiedDiagnosticTextsMatchInBytecodeVm(ExecutionEngineKind expectedEngine)
    {
        const string text = """
DIM low AS DOUBLE
DIM high AS DOUBLE
low = 10
high = 1
PRINT CLAMP(5, low, high)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(expectedEngine, result.ActualEngine);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Equal("Функция CLAMP: аргумент 'low' не должен быть больше аргумента 'high'.", diagnostic.Message);
    }

    [Theory]
    [InlineData(ExecutionEngineKind.BytecodeVm)]
    public void Run_Wave4FeaturesMatchInBytecodeVm(ExecutionEngineKind expectedEngine)
    {
        const string text = """
RANDOMIZE 42
PRINT RND()
PRINT CINT(-2.9)
PRINT CLNG(7.9)
PRINT CDBL(3)
PRINT CDEC(0.5)

DIM a(1 TO 3) AS INTEGER
a(1) = 10
a(2) = 20
a(3) = 30
PRINT ARRAYSUM(a)
PRINT ARRAYMIN(a)
PRINT ARRAYMAX(a)
PRINT ARRAYAVERAGE(a)

PRINT a(1) <= 3 ANDALSO a(2) = 0
PRINT a(1) <= 3 ORELSE a(2) = 0
PRINT a(1) >= 3 ANDALSO a(1) = 10
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(expectedEngine, result.ActualEngine);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(
        [
            "-2", "7", "3", "0.5",
            "60", "10", "30", "20",
            "FALSE", "FALSE", "TRUE"
        ], result.Output.Skip(1).ToList());
    }

    [Theory]
    [InlineData(ExecutionEngineKind.BytecodeVm)]
    public void Run_TypeInvariantFailureReportsConstructorCallChainInBytecodeVm(ExecutionEngineKind expectedEngine)
    {
        const string text = """
TYPE Person
    Age AS INTEGER

    SUB New(age AS INTEGER)
        SELF.Age = age
    END SUB

    FUNCTION IsValid() AS BOOLEAN
        RETURN SELF.Age >= 0
    END FUNCTION
END TYPE

DIM p AS Person
p = Person(-1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(expectedEngine, result.ActualEngine);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("Инвариант TYPE 'Person' вернул FALSE", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("Цепочка вызовов:", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("процедура 'Person.New', вызов в 14:5.", diagnostic.Message, StringComparison.Ordinal);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["1", "2", "10", "11", "9", "0"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForArrayMetadataBuiltins()
    {
        const string text = """
DIM cube(,,) AS INTEGER
DIM bounds(,) AS INTEGER
REDIM cube(2 TO 3, 10 TO 11, -1 TO 0)
PRINT ARRAYRANK(cube)
bounds = ARRAYBOUNDS(cube)
PRINT bounds(1, 1)
PRINT bounds(3, 2)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["3", "2", "0"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForPartialMultidimensionalIndexingProgram()
    {
        const string text = """
DIM grid(,) AS INTEGER
DIM rowView() AS INTEGER
REDIM grid(1 TO 2, 10 TO 11)
grid(2, 10) = 7
grid(2, 11) = 9
rowView = grid(2)
rowView(10) = 70
PRINT grid(2, 10)
PRINT rowView(11)
PRINT LBOUND(rowView)
PRINT UBOUND(rowView)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["1", "2", "5", "6", "3", "4"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalDynamicArrayBuiltins()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 11) AS INTEGER
DIM newColumn(1 TO 2) AS INTEGER
DIM removedColumn() AS INTEGER

board(1, 10) = 110
board(1, 11) = 111
board(2, 10) = 210
board(2, 11) = 211
newColumn(1) = 112
newColumn(2) = 212

ARRAYPUSH(board, newColumn, COLUMN)
removedColumn = ARRAYPOP(board, COLUMN)
ARRAYINSERT(board, 11, newColumn, COLUMN)
removedColumn = ARRAYREMOVE(board, 10, COLUMN)

PRINT removedColumn(1)
PRINT removedColumn(2)
PRINT board(1, 10)
PRINT board(1, 11)
PRINT board(2, 10)
PRINT board(2, 11)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["110", "210", "112", "111", "212", "211"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["TRUE", "5", "9", "2", "4", "6", "9", "6", "4"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalSortingGroup()
    {
        const string text = """
DIM board(1 TO 2, 1 TO 4) AS INTEGER
DIM sortedByColumn(1 TO 2, 1 TO 4) AS INTEGER
DIM hit() AS INTEGER

board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 2
board(1, 4) = 7
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5
board(2, 4) = 8

sortedByColumn = ARRAYTOSORTED(board, COLUMN)
ARRAYSORT(board, ROW)
hit = ARRAYBINARYSEARCH(board, 5, ROW)

PRINT sortedByColumn(1, 1)
PRINT sortedByColumn(1, 2)
PRINT sortedByColumn(2, 2)
PRINT board(1, 1)
PRINT board(2, 3)
PRINT hit(1)
PRINT hit(2)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["2", "2", "3", "3", "5", "2", "3"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_UsesBytecodeVmForArrayFlattenAndReshape()
    {
        const string text = """
DIM board(1 TO 2, 10 TO 12) AS INTEGER
DIM flat() AS INTEGER
DIM reshaped(,) AS INTEGER

board(1, 10) = 3
board(1, 11) = 1
board(1, 12) = 2
board(2, 10) = 6
board(2, 11) = 4
board(2, 12) = 5

flat = ARRAYFLATTEN(board)
reshaped = ARRAYRESHAPE(flat, board)

PRINT flat(1)
PRINT flat(6)
PRINT reshaped(1, 10)
PRINT reshaped(2, 12)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["3", "5", "3", "5"], result.Output);
    }

    [Fact]
    public void Run_WithBytecodeMode_ReportsArraySearchBuiltinRuntimeError()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYBINARYSEARCH(values, 1)
""";

        var pipeline = new AxiomExecutionPipeline();
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
        var result = pipeline.Run(new ExecutionRequest(text));

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
            var result = pipeline.Run(new ExecutionRequest(text));

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
    public void Run_WithBytecodeMode_ReportsCopyFileMissingSourceFromVm()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var missingPath = Path.Combine(workspace, "missing.txt");
            var targetPath = Path.Combine(workspace, "copy.txt");
            var text = $$"""
PRINT COPYFILE("{{EscapePath(missingPath)}}", "{{EscapePath(targetPath)}}")
""";

            var pipeline = new AxiomExecutionPipeline();
            var result = pipeline.Run(new ExecutionRequest(text));

            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("COPYFILE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("source", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
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
            var result = pipeline.Run(new ExecutionRequest(text));

            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["first", "second", "FALSE", "TRUE"], result.Output);
            Assert.Equal("saved\n", File.ReadAllText(outputPath));
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
            var result = pipeline.Run(new ExecutionRequest(text));

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
        Assert.Equal("? ", request.PromptText);
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
        Assert.Equal("Value? ", Assert.Single(inputProvider.Requests).PromptText);
    }

    [Fact]
    public void Run_UsesProvidedRuntimeRandomForRndBuiltin()
    {
        const string text = """
PRINT INT(RND() * 1000)
PRINT INT(RND() * 1000)
PRINT INT(RND() * 1000)
""";

        var bytecodeResult = Run(
            text,
            runtimeRandom: new FixedRuntimeRandom(0.125, 0.5, 0.875));

        Assert.Equal(ExecutionStatus.Success, bytecodeResult.Status);
        Assert.Equal(["125", "500", "875"], bytecodeResult.Output);
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
        IDiagnosticSink? diagnosticSink = null,
        IRuntimeClock? runtimeClock = null,
        IRuntimeRandom? runtimeRandom = null)
    {
        var request = new ExecutionRequest(sourceText, inputProvider, outputWriter, diagnosticSink, runtimeClock, runtimeRandom);
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

    private sealed class QueueKeyboardInputProvider : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IGraphicsHost
    {
        private readonly Queue<string> _keys;
        private readonly Queue<MouseInputEvent> _mouseEvents;
        private readonly HashSet<string> _pressedKeys;
        private readonly HashSet<int> _pressedMouseButtons;
        private readonly long _mouseX;
        private readonly long _mouseY;
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private bool _isScreenActive;
        private bool _isScreenOutputActive;
        private int _screenMode;
        private int _screenWidth;
        private int _screenHeight;
        private int _currentForegroundColor = 15;
        private int _currentBackgroundColor;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;
        private bool _viewportEnabled;
        private bool _viewportRelativeCoordinates;
        private long _viewportLeft;
        private long _viewportTop;
        private long _viewportRight = -1;
        private long _viewportBottom = -1;
        private bool _windowEnabled;
        private bool _windowUsesScreenCoordinates;
        private double _windowLeft;
        private double _windowTop;
        private double _windowRight;
        private double _windowBottom;
        private string _pendingGraphicsLine = string.Empty;

        public QueueKeyboardInputProvider(params string[] keys)
            : this(keys, pressedKeys: null)
        {
        }

        public QueueKeyboardInputProvider(
            IEnumerable<string>? keys,
            IEnumerable<string>? pressedKeys,
            long mouseX = -1,
            long mouseY = -1,
            IEnumerable<int>? pressedMouseButtons = null,
            IEnumerable<int>? mousePresses = null,
            IEnumerable<MouseInputEvent>? mouseEvents = null)
        {
            _keys = new Queue<string>(keys ?? []);
            _mouseEvents = new Queue<MouseInputEvent>(mouseEvents ?? (mousePresses ?? []).Select(static buttonNumber => new MouseInputEvent(MouseInputEventKind.Press, buttonNumber)));
            _pressedKeys = new HashSet<string>(
                (pressedKeys ?? []).Select(KeyboardInputNormalizer.Normalize),
                StringComparer.Ordinal);
            _mouseX = mouseX;
            _mouseY = mouseY;
            _pressedMouseButtons = new HashSet<int>(pressedMouseButtons ?? []);
        }

        public List<string> GraphicsOutput { get; } = [];

        public string ReadInput(InputRequest request) => string.Empty;

        public string ReadKey() => _keys.Count > 0 ? _keys.Dequeue() : string.Empty;

        public bool IsKeyDown(string keyText) => _pressedKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => _mouseX;

        public long GetMouseY() => _mouseY;

        public bool IsMouseButtonDown(int buttonNumber) => _pressedMouseButtons.Contains(buttonNumber);

        public int ReadMouseButtonPress()
        {
            var buttonNumber = 0;
            var deferredEvents = new List<MouseInputEvent>();

            while (_mouseEvents.Count > 0)
            {
                var candidate = _mouseEvents.Dequeue();
                if (buttonNumber == 0 && candidate.Kind == MouseInputEventKind.Press)
                {
                    buttonNumber = candidate.ButtonNumber;
                    continue;
                }

                deferredEvents.Add(candidate);
            }

            foreach (var deferredEvent in deferredEvents)
            {
                _mouseEvents.Enqueue(deferredEvent);
            }

            return buttonNumber;
        }

        public MouseInputEvent ReadMouseEvent() => _mouseEvents.Count > 0 ? _mouseEvents.Dequeue() : default;

        public bool IsScreenActive => _isScreenActive;

        public bool IsScreenOutputActive => _isScreenOutputActive;

        public int ScreenMode => _screenMode;

        public int ScreenWidth => _screenWidth;

        public int ScreenHeight => _screenHeight;

        public int CurrentForegroundColor => _currentForegroundColor;

        public int CurrentBackgroundColor => _currentBackgroundColor;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

        public bool ViewportEnabled => _viewportEnabled;

        public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

        public long ViewportLeft => _viewportLeft;

        public long ViewportTop => _viewportTop;

        public long ViewportRight => _viewportRight;

        public long ViewportBottom => _viewportBottom;

        public bool WindowEnabled => _windowEnabled;

        public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

        public double WindowLeft => _windowLeft;

        public double WindowTop => _windowTop;

        public double WindowRight => _windowRight;

        public double WindowBottom => _windowBottom;

        public void ActivateScreen(int mode)
        {
            _isScreenActive = mode != 0;
            _isScreenOutputActive = true;
            _screenMode = mode;
            (_screenWidth, _screenHeight) = mode switch
            {
                0 => (640, 300),
                1 => (320, 200),
                2 => (640, 480),
                3 => (800, 600),
                4 => (1024, 768),
                5 => (1280, 720),
                6 => (1366, 768),
                7 => (1600, 900),
                8 => (1920, 1080),
                9 => (2560, 1440),
                10 => (1918, 1079),
                _ => (0, 0)
            };
            _pixels.Clear();
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentForegroundColor = 15;
            _currentBackgroundColor = 0;
            _isTextBackgroundTransparent = false;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _pixels.Clear();
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void ClearGraphics()
        {
            _pixels.Clear();
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
        }

        public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
        {
            _currentForegroundColor = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _currentBackgroundColor = background;
                    _isTextBackgroundTransparent = false;
                    break;
                case TextBackgroundMode.Transparent:
                    _isTextBackgroundTransparent = true;
                    break;
                default:
                    break;
            }
        }

        public void WriteGraphicsText(string text, bool newLine)
        {
            _pendingGraphicsLine += text;

            if (!newLine)
            {
                return;
            }

            GraphicsOutput.Add(_pendingGraphicsLine);
            _pendingGraphicsLine = string.Empty;
        }

        public void SetPixel(long x, long y, int color)
        {
            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            _pixels[(absoluteX, absoluteY)] = color;
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetAbsolutePixel(long x, long y, int color)
        {
            if (IsInsideScreen(x, y))
            {
                _pixels[(x, y)] = color;
            }
        }

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void DrawLine(long x1, long y1, long x2, long y2, int color, GraphicsLineKind kind)
        {
            GraphicsHostDrawing.DrawLine(this, x1, y1, x2, y2, color, kind);
        }

        public int GetPixel(long x, long y)
        {
            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return 0;
            }

            return _pixels.TryGetValue((absoluteX, absoluteY), out var color) ? color : 0;
        }

        public int GetAbsolutePixel(long x, long y) =>
            _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
        {
            if (!_isScreenActive || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideScreen(absoluteLeft, absoluteTop) || !IsInsideScreen(absoluteRight, absoluteBottom))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            if (fillColor.HasValue)
            {
                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    for (var x = absoluteLeft; x <= absoluteRight; x++)
                    {
                        _pixels[(x, y)] = fillColor.Value;
                    }
                }
            }

            if (borderColor.HasValue)
            {
                for (var x = absoluteLeft; x <= absoluteRight; x++)
                {
                    _pixels[(x, absoluteTop)] = borderColor.Value;
                    _pixels[(x, absoluteBottom)] = borderColor.Value;
                }

                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    _pixels[(absoluteLeft, y)] = borderColor.Value;
                    _pixels[(absoluteRight, y)] = borderColor.Value;
                }
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
            return true;
        }

        public void ResetViewport()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _viewportEnabled = false;
            _viewportRelativeCoordinates = false;
            _viewportLeft = 0;
            _viewportTop = 0;
            _viewportRight = _screenWidth - 1L;
            _viewportBottom = _screenHeight - 1L;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
        {
            var viewport = _viewportEnabled
                ? new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates)
                : new GraphicsViewport(0, 0, _screenWidth - 1L, _screenHeight - 1L, UsesRelativeCoordinates: false);
            if (!_isScreenActive || viewport.Width < 2 || viewport.Height < 2)
            {
                return false;
            }

            if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = true;
            _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
            _windowLeft = window.Left;
            _windowTop = window.Top;
            _windowRight = window.Right;
            _windowBottom = window.Bottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }

            return true;
        }

        public void ResetWindow()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = false;
            _windowUsesScreenCoordinates = false;
            _windowLeft = 0d;
            _windowTop = 0d;
            _windowRight = 0d;
            _windowBottom = 0d;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        private (long X, long Y) ResolveCurrentAbsolutePosition()
        {
            if (GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY))
            {
                return (absoluteX, absoluteY);
            }

            return (_currentGraphicsX, _currentGraphicsY);
        }

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < _screenWidth && y < _screenHeight;
    }

    private sealed class MinimalGraphicsHostInputProvider : IInputProvider, IKeyboardInputProvider, IMouseInputProvider, IGraphicsHost
    {
        private readonly Dictionary<(long X, long Y), int> _pixels = [];
        private bool _isScreenActive;
        private bool _isScreenOutputActive;
        private int _screenMode;
        private int _screenWidth;
        private int _screenHeight;
        private int _currentForegroundColor = 15;
        private int _currentBackgroundColor;
        private bool _isTextBackgroundTransparent;
        private long _currentGraphicsX;
        private long _currentGraphicsY;
        private bool _viewportEnabled;
        private bool _viewportRelativeCoordinates;
        private long _viewportLeft;
        private long _viewportTop;
        private long _viewportRight = -1;
        private long _viewportBottom = -1;
        private bool _windowEnabled;
        private bool _windowUsesScreenCoordinates;
        private double _windowLeft;
        private double _windowTop;
        private double _windowRight;
        private double _windowBottom;
        private string _pendingGraphicsLine = string.Empty;

        public List<string> GraphicsOutput { get; } = [];

        public bool IsScreenActive => _isScreenActive;

        public bool IsScreenOutputActive => _isScreenOutputActive;

        public int ScreenMode => _screenMode;

        public int ScreenWidth => _screenWidth;

        public int ScreenHeight => _screenHeight;

        public int CurrentForegroundColor => _currentForegroundColor;

        public int CurrentBackgroundColor => _currentBackgroundColor;

        public bool IsTextBackgroundTransparent => _isTextBackgroundTransparent;

        public long CurrentGraphicsX => _currentGraphicsX;

        public long CurrentGraphicsY => _currentGraphicsY;

        public bool ViewportEnabled => _viewportEnabled;

        public bool ViewportRelativeCoordinates => _viewportRelativeCoordinates;

        public long ViewportLeft => _viewportLeft;

        public long ViewportTop => _viewportTop;

        public long ViewportRight => _viewportRight;

        public long ViewportBottom => _viewportBottom;

        public bool WindowEnabled => _windowEnabled;

        public bool WindowUsesScreenCoordinates => _windowUsesScreenCoordinates;

        public double WindowLeft => _windowLeft;

        public double WindowTop => _windowTop;

        public double WindowRight => _windowRight;

        public double WindowBottom => _windowBottom;

        public string ReadInput(InputRequest request) => string.Empty;

        public string ReadKey() => string.Empty;

        public bool IsKeyDown(string keyText) => false;

        public long GetMouseX() => -1;

        public long GetMouseY() => -1;

        public bool IsMouseButtonDown(int buttonNumber) => false;

        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent() => default;

        public void ActivateScreen(int mode)
        {
            _isScreenActive = mode != 0;
            _isScreenOutputActive = true;
            _screenMode = mode;
            (_screenWidth, _screenHeight) = mode switch
            {
                0 => (640, 300),
                1 => (320, 200),
                2 => (640, 480),
                3 => (800, 600),
                4 => (1024, 768),
                5 => (1280, 720),
                6 => (1366, 768),
                7 => (1600, 900),
                8 => (1920, 1080),
                9 => (2560, 1440),
                10 => (1918, 1079),
                _ => (0, 0)
            };
            _pixels.Clear();
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentForegroundColor = 15;
            _currentBackgroundColor = 0;
            _isTextBackgroundTransparent = false;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void DeactivateScreen()
        {
            _isScreenActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
            _pixels.Clear();
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
            ResetViewport();
            ResetWindow();
        }

        public void ClearGraphics()
        {
            _pixels.Clear();
            GraphicsOutput.Clear();
            _pendingGraphicsLine = string.Empty;
            _currentGraphicsX = 0;
            _currentGraphicsY = 0;
        }

        public void SetTextColor(int foreground, TextBackgroundMode backgroundMode, int background = 0)
        {
            _currentForegroundColor = foreground;
            switch (backgroundMode)
            {
                case TextBackgroundMode.Color:
                    _currentBackgroundColor = background;
                    _isTextBackgroundTransparent = false;
                    break;
                case TextBackgroundMode.Transparent:
                    _isTextBackgroundTransparent = true;
                    break;
            }
        }

        public void WriteGraphicsText(string text, bool newLine)
        {
            _pendingGraphicsLine += text;
            if (!newLine)
            {
                return;
            }

            GraphicsOutput.Add(_pendingGraphicsLine);
            _pendingGraphicsLine = string.Empty;
        }

        public void SetPixel(long x, long y, int color)
        {
            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return;
            }

            _pixels[(absoluteX, absoluteY)] = color;
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public void SetGraphicsCurrentPosition(long x, long y)
        {
            _currentGraphicsX = x;
            _currentGraphicsY = y;
        }

        public int GetPixel(long x, long y)
        {
            if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsolute(this, x, y, out var absoluteX, out var absoluteY))
            {
                return 0;
            }

            return _pixels.TryGetValue((absoluteX, absoluteY), out var color) ? color : 0;
        }

        public void SetAbsolutePixel(long x, long y, int color)
        {
            if (IsInsideScreen(x, y))
            {
                _pixels[(x, y)] = color;
            }
        }

        public int GetAbsolutePixel(long x, long y) =>
            _pixels.TryGetValue((x, y), out var color) ? color : 0;

        public bool SetViewport(long left, long top, long right, long bottom, int? fillColor, int? borderColor, bool useScreenCoordinates)
        {
            if (!_isScreenActive || left > right || top > bottom)
            {
                return false;
            }

            var baseLeft = _viewportEnabled ? _viewportLeft : 0L;
            var baseTop = _viewportEnabled ? _viewportTop : 0L;
            var absoluteLeft = useScreenCoordinates ? left : GraphicsLineMath.SaturatingAdd(baseLeft, left);
            var absoluteTop = useScreenCoordinates ? top : GraphicsLineMath.SaturatingAdd(baseTop, top);
            var absoluteRight = useScreenCoordinates ? right : GraphicsLineMath.SaturatingAdd(baseLeft, right);
            var absoluteBottom = useScreenCoordinates ? bottom : GraphicsLineMath.SaturatingAdd(baseTop, bottom);
            if (!IsInsideScreen(absoluteLeft, absoluteTop) || !IsInsideScreen(absoluteRight, absoluteBottom))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            if (fillColor.HasValue)
            {
                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    for (var x = absoluteLeft; x <= absoluteRight; x++)
                    {
                        _pixels[(x, y)] = fillColor.Value;
                    }
                }
            }

            if (borderColor.HasValue)
            {
                for (var x = absoluteLeft; x <= absoluteRight; x++)
                {
                    _pixels[(x, absoluteTop)] = borderColor.Value;
                    _pixels[(x, absoluteBottom)] = borderColor.Value;
                }

                for (var y = absoluteTop; y <= absoluteBottom; y++)
                {
                    _pixels[(absoluteLeft, y)] = borderColor.Value;
                    _pixels[(absoluteRight, y)] = borderColor.Value;
                }
            }

            _viewportEnabled = true;
            _viewportRelativeCoordinates = !useScreenCoordinates;
            _viewportLeft = absoluteLeft;
            _viewportTop = absoluteTop;
            _viewportRight = absoluteRight;
            _viewportBottom = absoluteBottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
            return true;
        }

        public void ResetViewport()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _viewportEnabled = false;
            _viewportRelativeCoordinates = false;
            _viewportLeft = 0;
            _viewportTop = 0;
            _viewportRight = _screenWidth - 1L;
            _viewportBottom = _screenHeight - 1L;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        public bool SetWindow(double left, double top, double right, double bottom, bool useScreenCoordinates)
        {
            var viewport = _viewportEnabled
                ? new GraphicsViewport(_viewportLeft, _viewportTop, _viewportRight, _viewportBottom, _viewportRelativeCoordinates)
                : new GraphicsViewport(0, 0, _screenWidth - 1L, _screenHeight - 1L, UsesRelativeCoordinates: false);
            if (!_isScreenActive || viewport.Width < 2 || viewport.Height < 2)
            {
                return false;
            }

            if (!GraphicsCoordinateSpaceMath.TryCreateWindow(left, top, right, bottom, useScreenCoordinates, out var window))
            {
                return false;
            }

            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = true;
            _windowUsesScreenCoordinates = window.UsesScreenCoordinates;
            _windowLeft = window.Left;
            _windowTop = window.Top;
            _windowRight = window.Right;
            _windowBottom = window.Bottom;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }

            return true;
        }

        public void ResetWindow()
        {
            var currentAbsolute = ResolveCurrentAbsolutePosition();
            _windowEnabled = false;
            _windowUsesScreenCoordinates = false;
            _windowLeft = 0d;
            _windowTop = 0d;
            _windowRight = 0d;
            _windowBottom = 0d;
            if (GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(this, currentAbsolute.X, currentAbsolute.Y, out var logicalX, out var logicalY))
            {
                _currentGraphicsX = logicalX;
                _currentGraphicsY = logicalY;
            }
        }

        private (long X, long Y) ResolveCurrentAbsolutePosition()
        {
            if (GraphicsCoordinateSpaceMath.TryResolveCurrentAbsolutePosition(this, out var absoluteX, out var absoluteY))
            {
                return (absoluteX, absoluteY);
            }

            return (_currentGraphicsX, _currentGraphicsY);
        }

        private bool IsInsideScreen(long x, long y) => x >= 0 && y >= 0 && x < _screenWidth && y < _screenHeight;
    }

    private sealed class FixedRuntimeClock : IRuntimeClock
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

    private sealed class FixedRuntimeRandom : IRuntimeRandom
    {
        private readonly Queue<double> _values;

        public FixedRuntimeRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double NextRandomDouble() => _values.Count > 0 ? _values.Dequeue() : 0.0;

        public void Reseed(int seed) { }
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
