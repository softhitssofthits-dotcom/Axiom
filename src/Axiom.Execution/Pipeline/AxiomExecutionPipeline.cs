using System.Diagnostics;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Runtime;
using Axiom.Semantics;

namespace Axiom.Execution;

public sealed class AxiomExecutionPipeline
{
    private static readonly HashSet<BuiltinFunctionId> VmSupportedBuiltins =
    [
        BuiltinFunctionId.Len,
        BuiltinFunctionId.Left,
        BuiltinFunctionId.Right,
        BuiltinFunctionId.Mid,
        BuiltinFunctionId.TrimWhitespace,
        BuiltinFunctionId.TrimCharacters,
        BuiltinFunctionId.LTrimWhitespace,
        BuiltinFunctionId.LTrimCharacters,
        BuiltinFunctionId.RTrimWhitespace,
        BuiltinFunctionId.RTrimCharacters,
        BuiltinFunctionId.Reverse,
        BuiltinFunctionId.Contains,
        BuiltinFunctionId.StartsWith,
        BuiltinFunctionId.EndsWith,
        BuiltinFunctionId.Replace,
        BuiltinFunctionId.ReplaceMany,
        BuiltinFunctionId.InStr,
        BuiltinFunctionId.InStrFrom,
        BuiltinFunctionId.InStrRev,
        BuiltinFunctionId.InStrRevFrom,
        BuiltinFunctionId.SubStrCount,
        BuiltinFunctionId.Repeat,
        BuiltinFunctionId.Space,
        BuiltinFunctionId.StringFill,
        BuiltinFunctionId.PadLeft,
        BuiltinFunctionId.PadLeftFill,
        BuiltinFunctionId.PadRight,
        BuiltinFunctionId.PadRightFill,
        BuiltinFunctionId.Center,
        BuiltinFunctionId.CenterFill,
        BuiltinFunctionId.Insert,
        BuiltinFunctionId.Remove,
        BuiltinFunctionId.Asc,
        BuiltinFunctionId.AscW,
        BuiltinFunctionId.Chr,
        BuiltinFunctionId.ChrW,
        BuiltinFunctionId.Split,
        BuiltinFunctionId.SplitSeparator,
        BuiltinFunctionId.Join,
        BuiltinFunctionId.JoinSeparator,
        BuiltinFunctionId.RegexIsMatch,
        BuiltinFunctionId.RegexIsMatchModes,
        BuiltinFunctionId.RegexMatch,
        BuiltinFunctionId.RegexMatchModes,
        BuiltinFunctionId.RegexExtract,
        BuiltinFunctionId.RegexExtractModes,
        BuiltinFunctionId.RegexMatches,
        BuiltinFunctionId.RegexMatchesModes,
        BuiltinFunctionId.RegexExtractAll,
        BuiltinFunctionId.RegexExtractAllModes,
        BuiltinFunctionId.RegexSplit,
        BuiltinFunctionId.RegexSplitModes,
        BuiltinFunctionId.RegexReplace,
        BuiltinFunctionId.RegexReplaceModes,
        BuiltinFunctionId.RegexReplaceCount,
        BuiltinFunctionId.RegexReplaceModesCount,
        BuiltinFunctionId.RegexCount,
        BuiltinFunctionId.RegexCountModes,
        BuiltinFunctionId.InKey,
        BuiltinFunctionId.KeyDown,
        BuiltinFunctionId.KeyPress,
        BuiltinFunctionId.MouseX,
        BuiltinFunctionId.MouseY,
        BuiltinFunctionId.MouseButton,
        BuiltinFunctionId.MousePress,
        BuiltinFunctionId.ScreenIsActive,
        BuiltinFunctionId.ScreenMode,
        BuiltinFunctionId.ScreenWidth,
        BuiltinFunctionId.ScreenHeight,
        BuiltinFunctionId.Point,
        BuiltinFunctionId.LoadImage,
        BuiltinFunctionId.ImageWidth,
        BuiltinFunctionId.ImageHeight,
        BuiltinFunctionId.LoadTileset,
        BuiltinFunctionId.LoadTilesetMargin,
        BuiltinFunctionId.LoadTilesetMarginSpacing,
        BuiltinFunctionId.TileCount,
        BuiltinFunctionId.TileWidth,
        BuiltinFunctionId.TileHeight,
        BuiltinFunctionId.Int,
        BuiltinFunctionId.Str,
        BuiltinFunctionId.Val,
        BuiltinFunctionId.LCase,
        BuiltinFunctionId.UCase,
        BuiltinFunctionId.Rnd,
        BuiltinFunctionId.Abs,
        BuiltinFunctionId.Sgn,
        BuiltinFunctionId.Min,
        BuiltinFunctionId.Max,
        BuiltinFunctionId.Clamp,
        BuiltinFunctionId.Sin,
        BuiltinFunctionId.Cos,
        BuiltinFunctionId.Tan,
        BuiltinFunctionId.Atn,
        BuiltinFunctionId.Atn2,
        BuiltinFunctionId.Sqr,
        BuiltinFunctionId.Cbrt,
        BuiltinFunctionId.Exp,
        BuiltinFunctionId.Log,
        BuiltinFunctionId.Log10,
        BuiltinFunctionId.Log2,
        BuiltinFunctionId.Pow,
        BuiltinFunctionId.Floor,
        BuiltinFunctionId.Ceil,
        BuiltinFunctionId.Round,
        BuiltinFunctionId.Fix,
        BuiltinFunctionId.Asin,
        BuiltinFunctionId.Acos,
        BuiltinFunctionId.Asinh,
        BuiltinFunctionId.Acosh,
        BuiltinFunctionId.Atanh,
        BuiltinFunctionId.Sinh,
        BuiltinFunctionId.Cosh,
        BuiltinFunctionId.Tanh,
        BuiltinFunctionId.SetSign,
        BuiltinFunctionId.LBound,
        BuiltinFunctionId.UBound,
        BuiltinFunctionId.ArrayLength,
        BuiltinFunctionId.ArrayIsAllocated,
        BuiltinFunctionId.ArrayRank,
        BuiltinFunctionId.ArrayBounds,
        BuiltinFunctionId.ArrayClone,
        BuiltinFunctionId.ArraySlice,
        BuiltinFunctionId.ArrayFlatten,
        BuiltinFunctionId.ArrayReshape,
        BuiltinFunctionId.ArrayConcat,
        BuiltinFunctionId.ArrayToSorted,
        BuiltinFunctionId.ArrayToReversed,
        BuiltinFunctionId.ArrayTake,
        BuiltinFunctionId.ArraySkip,
        BuiltinFunctionId.ArrayDistinct,
        BuiltinFunctionId.ArrayFill,
        BuiltinFunctionId.ArrayReverse,
        BuiltinFunctionId.ArraySort,
        BuiltinFunctionId.ArrayPush,
        BuiltinFunctionId.ArrayPop,
        BuiltinFunctionId.ArrayInsert,
        BuiltinFunctionId.ArrayRemove,
        BuiltinFunctionId.ArrayContains,
        BuiltinFunctionId.ArrayIndexOf,
        BuiltinFunctionId.ArrayLastIndexOf,
        BuiltinFunctionId.ArrayCount,
        BuiltinFunctionId.ArrayFindAll,
        BuiltinFunctionId.ArrayBinarySearch,
        BuiltinFunctionId.ArrayEvery,
        BuiltinFunctionId.ArraySome,
        BuiltinFunctionId.ArrayFilter,
        BuiltinFunctionId.ArrayMap,
        BuiltinFunctionId.ArrayReduce,
        BuiltinFunctionId.Date,
        BuiltinFunctionId.DateInZone,
        BuiltinFunctionId.Time,
        BuiltinFunctionId.TimeInZone,
        BuiltinFunctionId.Now,
        BuiltinFunctionId.NowInZone,
        BuiltinFunctionId.UtcDate,
        BuiltinFunctionId.UtcTime,
        BuiltinFunctionId.UtcNow,
        BuiltinFunctionId.Timer,
        BuiltinFunctionId.DateSerial,
        BuiltinFunctionId.TimeSerial,
        BuiltinFunctionId.Year,
        BuiltinFunctionId.Month,
        BuiltinFunctionId.MonthName,
        BuiltinFunctionId.Day,
        BuiltinFunctionId.Hour,
        BuiltinFunctionId.Minute,
        BuiltinFunctionId.Second,
        BuiltinFunctionId.Weekday,
        BuiltinFunctionId.WeekdayName,
        BuiltinFunctionId.DateAddDateValue,
        BuiltinFunctionId.DateAddTimeValue,
        BuiltinFunctionId.DateAddDateTimeValue,
        BuiltinFunctionId.DateDiffDateValues,
        BuiltinFunctionId.DateDiffTimeValues,
        BuiltinFunctionId.DateDiffDateTimeValues,
        BuiltinFunctionId.DatePartDateValue,
        BuiltinFunctionId.DatePartTimeValue,
        BuiltinFunctionId.DatePartDateTimeValue,
        BuiltinFunctionId.DateValue,
        BuiltinFunctionId.TimeValue,
        BuiltinFunctionId.IsDate,
        BuiltinFunctionId.UtcOffset,
        BuiltinFunctionId.UtcOffsetValue,
        BuiltinFunctionId.UtcOffsetValueZone,
        BuiltinFunctionId.ToUtc,
        BuiltinFunctionId.ToUtcZone,
        BuiltinFunctionId.FromUtc,
        BuiltinFunctionId.FromUtcZone,
        BuiltinFunctionId.IsLeapYear,
        BuiltinFunctionId.DaysInMonth,
        BuiltinFunctionId.StartOfMonth,
        BuiltinFunctionId.EndOfMonth,
        BuiltinFunctionId.StartOfYear,
        BuiltinFunctionId.EndOfYear,
        BuiltinFunctionId.IsOpen,
        BuiltinFunctionId.FilePath,
        BuiltinFunctionId.FileMode,
        BuiltinFunctionId.FilePos,
        BuiltinFunctionId.FileLength,
        BuiltinFunctionId.LineNumber,
        BuiltinFunctionId.Eof,
        BuiltinFunctionId.ReadToEnd,
        BuiltinFunctionId.ReadText,
        BuiltinFunctionId.ReadLine,
        BuiltinFunctionId.ReadLines,
        BuiltinFunctionId.SkipLine,
        BuiltinFunctionId.Write,
        BuiltinFunctionId.WriteLine,
        BuiltinFunctionId.NewLine,
        BuiltinFunctionId.Flush,
        BuiltinFunctionId.SeekFile,
        BuiltinFunctionId.Rewind,
        BuiltinFunctionId.FileExists,
        BuiltinFunctionId.DirExists,
        BuiltinFunctionId.PathExists,
        BuiltinFunctionId.ReadFile,
        BuiltinFunctionId.ReadAllLines,
        BuiltinFunctionId.WriteFile,
        BuiltinFunctionId.AppendFile,
        BuiltinFunctionId.FileSize,
        BuiltinFunctionId.FileDate,
        BuiltinFunctionId.FileCreateDate,
        BuiltinFunctionId.FileAccessDate,
        BuiltinFunctionId.DirDate,
        BuiltinFunctionId.GetFiles,
        BuiltinFunctionId.GetFilesPattern,
        BuiltinFunctionId.GetDirs,
        BuiltinFunctionId.GetDirsPattern,
        BuiltinFunctionId.CombinePath,
        BuiltinFunctionId.FileName,
        BuiltinFunctionId.DirName,
        BuiltinFunctionId.ExtName,
        BuiltinFunctionId.FullPath,
        BuiltinFunctionId.PathRoot,
        BuiltinFunctionId.IsAbsolutePath,
        BuiltinFunctionId.MakeDir,
        BuiltinFunctionId.DeleteFile,
        BuiltinFunctionId.CopyFile,
        BuiltinFunctionId.CopyFileOverwrite,
        BuiltinFunctionId.MoveFile,
        BuiltinFunctionId.MoveFileOverwrite,
        BuiltinFunctionId.RenameFile,
        BuiltinFunctionId.DeleteDir,
        BuiltinFunctionId.DeleteDirRecursive,
        BuiltinFunctionId.CopyDir,
        BuiltinFunctionId.CopyDirOverwrite,
        BuiltinFunctionId.MoveDir,
        BuiltinFunctionId.RenameDir,
        BuiltinFunctionId.CurrentDir,
        BuiltinFunctionId.ChangeDir,
        BuiltinFunctionId.DirSize,
        BuiltinFunctionId.FreeSpace,
        BuiltinFunctionId.TotalFreeSpace,
        BuiltinFunctionId.TotalSpace,
        BuiltinFunctionId.IsReadOnly,
        BuiltinFunctionId.IsHidden,
        BuiltinFunctionId.DriveName,
        BuiltinFunctionId.DriveFormat,
        BuiltinFunctionId.DriveType,
        BuiltinFunctionId.MakeTempFile,
        BuiltinFunctionId.MakeTempDir
    ];

    public CompilationResult Compile(CompilationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var compilation = CompileCore(request.SourceText, request.ProgressReporter, request.BytecodeSnapshotPath);

        foreach (var diagnostic in compilation.Diagnostics)
        {
            request.DiagnosticSink.ReportDiagnostic(diagnostic);
        }

        foreach (var diagnostic in compilation.BackendDiagnostics)
        {
            request.DiagnosticSink.ReportDiagnostic(diagnostic);
        }

        return compilation;
    }

    public ExecutionResult Run(ExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var compilation = CompileCore(request.SourceText, progressReporter: null, request.BytecodeSnapshotPath);

        if (!compilation.FrontendSucceeded)
        {
            return CreateExecutionFailureResult(
                compilation.Status,
                compilation.Diagnostics,
                request.DiagnosticSink,
                compilation.FrontendPreparationDuration,
                compilation.LoweringDuration,
                compilation.BytecodeEmissionDuration);
        }

        if (!compilation.Succeeded)
        {
            return CreateExecutionResult(
                ExecutionStatus.BackendFailure,
                compilation.Diagnostics.Concat(compilation.BackendDiagnostics).ToArray(),
                [],
                request.DiagnosticSink,
                actualEngine: null,
                frontendPreparationDuration: compilation.FrontendPreparationDuration,
                loweringDuration: compilation.LoweringDuration,
                bytecodeEmissionDuration: compilation.BytecodeEmissionDuration);
        }

        var execution = RunCompiled(compilation.CompiledProgram!, request);
        return WithPreparationDurations(
            MergeFrontendDiagnostics(execution, compilation.Diagnostics),
            compilation.FrontendPreparationDuration,
            compilation.LoweringDuration,
            compilation.BytecodeEmissionDuration);
    }

    private static ExecutionResult MergeFrontendDiagnostics(
        ExecutionResult execution,
        IReadOnlyList<Diagnostic> frontendDiagnostics)
    {
        if (frontendDiagnostics.Count == 0)
        {
            return execution;
        }

        var merged = new List<Diagnostic>(frontendDiagnostics.Count + execution.Diagnostics.Count);
        merged.AddRange(frontendDiagnostics);
        merged.AddRange(execution.Diagnostics);

        return new ExecutionResult(
            execution.Status,
            merged,
            execution.Output,
            execution.ActualEngine,
            execution.FrontendPreparationDuration,
            execution.LoweringDuration,
            execution.BytecodeEmissionDuration,
            execution.RuntimeExecutionDuration,
            execution.UsedCompiledCache,
            execution.UsedLoweredCache,
            execution.UsedBytecodeCache,
            execution.StopState,
            execution.PendingOutput);
    }

    public ExecutionResult RunCompiled(CompiledProgram compiledProgram, ExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(compiledProgram);
        ArgumentNullException.ThrowIfNull(request);

        var coverageDiagnostic = GetBytecodeCoverageDiagnostic(compiledProgram);
        if (compiledProgram.BytecodeModule is not null
            && coverageDiagnostic is null)
        {
            return RunThroughBytecodeVm(compiledProgram.BytecodeModule, request);
        }

        return CreateExecutionResult(
            ExecutionStatus.BackendFailure,
            [coverageDiagnostic ?? CreateGenericBytecodeCoverageDiagnostic(compiledProgram.BoundProgram.Location)],
            [],
            request.DiagnosticSink,
            actualEngine: null);
    }

    private static CompilationResult CompileCore(
        string sourceText,
        IProgress<int>? progressReporter,
        string? bytecodeSnapshotPath)
    {
        var frontendStopwatch = Stopwatch.StartNew();

        progressReporter?.Report(5);
        var syntax = Parser.Parse(sourceText, out var syntaxDiagnostics);

        if (syntaxDiagnostics.Count > 0)
        {
            frontendStopwatch.Stop();
            progressReporter?.Report(100);
            return new CompilationResult(
                CompilationStatus.SyntaxFailure,
                syntaxDiagnostics.ToArray(),
                compiledProgram: null,
                frontendPreparationDuration: frontendStopwatch.Elapsed);
        }

        progressReporter?.Report(55);
        var boundProgram = Binder.Bind(syntax, out var semanticDiagnostics);

        if (semanticDiagnostics.Any(static diagnostic => diagnostic.Severity != DiagnosticSeverity.Warning))
        {
            // Предупреждения (например, MB2045 о совпадении имён переменной и процедуры)
            // не блокируют компиляцию и выполнение — они остаются в списке диагностик.
            frontendStopwatch.Stop();
            progressReporter?.Report(100);
            return new CompilationResult(
                CompilationStatus.SemanticFailure,
                semanticDiagnostics.ToArray(),
                compiledProgram: null,
                frontendPreparationDuration: frontendStopwatch.Elapsed);
        }

        frontendStopwatch.Stop();
        var frontendPreparationDuration = frontendStopwatch.Elapsed;
        var compiledProgram = new CompiledProgram(sourceText, syntax, boundProgram);

        var loweringStopwatch = Stopwatch.StartNew();
        progressReporter?.Report(70);
        var loweringResult = new BoundProgramLowerer().Lower(boundProgram);
        loweringStopwatch.Stop();
        var loweringDuration = loweringStopwatch.Elapsed;

        if (!loweringResult.Succeeded)
        {
            progressReporter?.Report(100);
            return new CompilationResult(
                CompilationStatus.BackendFailure,
                semanticDiagnostics.ToArray(),
                compiledProgram,
                loweringResult.Diagnostics.Select(MapBackendDiagnostic).ToArray(),
                backendSucceeded: false,
                bytecodeAvailable: false,
                frontendPreparationDuration: frontendPreparationDuration,
                loweringDuration: loweringDuration);
        }

        compiledProgram = new CompiledProgram(
            sourceText,
            syntax,
            boundProgram,
            loweredProgram: loweringResult.Program);

        var bytecodeStopwatch = Stopwatch.StartNew();
        progressReporter?.Report(90);
        var bytecodeResult = new BytecodeCompiler().Compile(loweringResult.Program!);
        bytecodeStopwatch.Stop();
        var bytecodeEmissionDuration = bytecodeStopwatch.Elapsed;
        progressReporter?.Report(100);

        if (!bytecodeResult.Succeeded)
        {
            return new CompilationResult(
                CompilationStatus.BackendFailure,
                semanticDiagnostics.ToArray(),
                compiledProgram,
                bytecodeResult.Diagnostics.Select(MapBackendDiagnostic).ToArray(),
                backendSucceeded: false,
                bytecodeAvailable: false,
                frontendPreparationDuration: frontendPreparationDuration,
                loweringDuration: loweringDuration,
                bytecodeEmissionDuration: bytecodeEmissionDuration);
        }

        if (bytecodeSnapshotPath is not null
            && !TryWriteBytecodeSnapshot(bytecodeSnapshotPath, bytecodeResult.Module!, out var snapshotDiagnostic))
        {
            return new CompilationResult(
                CompilationStatus.BackendFailure,
                semanticDiagnostics.ToArray(),
                compiledProgram,
                [snapshotDiagnostic],
                backendSucceeded: false,
                bytecodeAvailable: false,
                frontendPreparationDuration: frontendPreparationDuration,
                loweringDuration: loweringDuration,
                bytecodeEmissionDuration: bytecodeEmissionDuration);
        }

        compiledProgram = new CompiledProgram(
            sourceText,
            syntax,
            boundProgram,
            loweredProgram: loweringResult.Program,
            bytecodeModule: bytecodeResult.Module);

        return new CompilationResult(
            CompilationStatus.Success,
            semanticDiagnostics.ToArray(),
            compiledProgram,
            backendDiagnostics: [],
            backendSucceeded: true,
            bytecodeAvailable: true,
            frontendPreparationDuration: frontendPreparationDuration,
            loweringDuration: loweringDuration,
            bytecodeEmissionDuration: bytecodeEmissionDuration);
    }

    private static bool TryWriteBytecodeSnapshot(
        string path,
        BytecodeModule module,
        out Diagnostic diagnostic)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            BytecodeModuleSerializer.SerializeToJson(stream, module);
            diagnostic = null!;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException
            || exception is NotSupportedException)
        {
            diagnostic = new Diagnostic(
                DiagnosticCodes.BytecodeJsonSnapshot,
                $"Не удалось записать JSON-снимок байткода '{path}': {exception.Message}",
                default);
            return false;
        }
    }

    private static ExecutionResult CreateExecutionFailureResult(
        CompilationStatus compilationStatus,
        IReadOnlyList<Diagnostic> diagnostics,
        IDiagnosticSink diagnosticSink,
        TimeSpan frontendPreparationDuration,
        TimeSpan loweringDuration,
        TimeSpan bytecodeEmissionDuration)
    {
        var executionStatus = compilationStatus switch
        {
            CompilationStatus.SyntaxFailure => ExecutionStatus.SyntaxFailure,
            CompilationStatus.SemanticFailure => ExecutionStatus.SemanticFailure,
            CompilationStatus.BackendFailure => ExecutionStatus.BackendFailure,
            CompilationStatus.Success => ExecutionStatus.Success,
            _ => throw new ArgumentOutOfRangeException(nameof(compilationStatus))
        };

        return CreateExecutionResult(
            executionStatus,
            diagnostics,
            [],
            diagnosticSink,
            actualEngine: null,
            frontendPreparationDuration: frontendPreparationDuration,
            loweringDuration: loweringDuration,
            bytecodeEmissionDuration: bytecodeEmissionDuration);
    }

    private static ExecutionResult WithPreparationDurations(
        ExecutionResult result,
        TimeSpan frontendPreparationDuration,
        TimeSpan loweringDuration,
        TimeSpan bytecodeEmissionDuration)
    {
        return new ExecutionResult(
            result.Status,
            result.Diagnostics,
            result.Output,
            result.ActualEngine,
            frontendPreparationDuration,
            loweringDuration,
            bytecodeEmissionDuration,
            result.RuntimeExecutionDuration,
            result.UsedCompiledCache,
            result.UsedLoweredCache,
            result.UsedBytecodeCache,
            result.StopState,
            result.PendingOutput);
    }

    private static ExecutionResult RunThroughBytecodeVm(BytecodeModule module, ExecutionRequest request)
    {
        var runtimeHost = new ExecutionRuntimeHostAdapter(request.InputProvider, request.OutputWriter, request.RuntimeClock, request.RuntimeRandom, request.CancellationProvider, request.ResourceBaseDirectory);
        var executionStopwatch = Stopwatch.StartNew();
        var breakpoints = request.SourceBreakpointLines.Count == 0
            ? null
            : BytecodeBreakpointSet.FromLines(request.SourceBreakpointLines.ToArray());
        var firstInstructionAlreadyStopped = false;
        var stopBeforeFirstInstruction = request.StopBeforeFirstInstruction
            ? snapshot =>
            {
                if (firstInstructionAlreadyStopped)
                {
                    return false;
                }

                firstInstructionAlreadyStopped = true;
                return true;
            }
            : (Func<BytecodeVmExecutionSnapshot, bool>?)null;
        var runtimeResult = BytecodeVm.Execute(
            module,
            runtimeHost,
            shouldStopBeforeInstruction: stopBeforeFirstInstruction,
            breakpoints: breakpoints);
        executionStopwatch.Stop();
        var status = runtimeResult.Cancelled
            ? ExecutionStatus.Cancelled
            : runtimeResult.Succeeded ? ExecutionStatus.Success : ExecutionStatus.RuntimeFailure;

        return CreateExecutionResult(
            status,
            runtimeResult.Diagnostics,
            runtimeHost.Output,
            request.DiagnosticSink,
            ExecutionEngineKind.BytecodeVm,
            runtimeExecutionDuration: executionStopwatch.Elapsed,
            stopState: runtimeResult.StopState,
            pendingOutput: runtimeResult.PendingOutput);
    }

    private static Diagnostic MapBackendDiagnostic(BytecodeDiagnostic diagnostic)
    {
        return new Diagnostic(diagnostic.Code, diagnostic.Message, diagnostic.Location);
    }

    private static Diagnostic? GetBytecodeCoverageDiagnostic(CompiledProgram compiledProgram)
    {
        ArgumentNullException.ThrowIfNull(compiledProgram);

        if (compiledProgram.BytecodeModule is null)
        {
            return CreateGenericBytecodeCoverageDiagnostic(compiledProgram.BoundProgram.Location);
        }

        if (!CanExecuteOnBytecodeVm(compiledProgram.BytecodeModule))
        {
            return CreateGenericBytecodeCoverageDiagnostic(compiledProgram.BoundProgram.Location);
        }

        return HasUnsupportedVmSemantics(compiledProgram.BoundProgram)
            ? CreateGenericBytecodeCoverageDiagnostic(compiledProgram.BoundProgram.Location)
            : null;
    }

    private static bool CanExecuteOnBytecodeVm(BytecodeModule module)
    {
        foreach (var builtin in module.Builtins)
        {
            if (!BytecodeVm.SupportsBuiltin(builtin.CanonicalId, builtin.ParameterCount))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasUnsupportedVmSemantics(BoundProgram program)
    {
        foreach (var statement in program.GlobalStatements)
        {
            if (HasUnsupportedVmSemantics(statement, insideNestedBlock: false))
            {
                return true;
            }
        }

        foreach (var routine in program.Routines)
        {
            foreach (var statement in routine.Statements)
            {
                if (HasUnsupportedVmSemantics(statement, insideNestedBlock: false))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetUnsupportedLocalRoutineCaptureDiagnostic(BoundProgram program, out Diagnostic? diagnostic)
    {
        foreach (var routine in program.Routines)
        {
            if (!routine.Symbol.IsLocal)
            {
                continue;
            }

            var ownVariables = CollectOwnRoutineVariables(routine);
            foreach (var statement in routine.Statements)
            {
                if (TryGetCapturedVariable(statement, ownVariables, out var capturedVariable, out var location))
                {
                    diagnostic = new Diagnostic(
                        DiagnosticCodes.BytecodeCapture,
                        $"Байт-кодный путь пока не поддерживает локальную процедуру или функцию '{routine.Symbol.Name}', захватывающую внешнюю переменную '{capturedVariable.Name}'.",
                        location);
                    return true;
                }
            }
        }

        diagnostic = null;
        return false;
    }

    private static HashSet<VariableSymbol> CollectOwnRoutineVariables(BoundRoutine routine)
    {
        var variables = new HashSet<VariableSymbol>();

        foreach (var parameter in routine.Symbol.Parameters)
        {
            variables.Add(parameter);
        }

        foreach (var statement in routine.Statements)
        {
            CollectOwnRoutineVariables(statement, variables);
        }

        return variables;
    }

    private static void CollectOwnRoutineVariables(BoundStatement statement, HashSet<VariableSymbol> variables)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    variables.Add(declaration.Variable);
                }
                break;

            case BoundVariableDeclarationStatement declaration:
                variables.Add(declaration.Variable);
                break;

            case BoundIfStatement ifStatement:
                CollectOwnRoutineVariables(ifStatement.Statements, variables);
                CollectOwnRoutineVariables(ifStatement.ElseStatements, variables);
                break;

            case BoundWhileStatement whileStatement:
                CollectOwnRoutineVariables(whileStatement.Statements, variables);
                break;

            case BoundDoLoopStatement doLoopStatement:
                CollectOwnRoutineVariables(doLoopStatement.Statements, variables);
                break;

            case BoundForStatement forStatement:
                variables.Add(forStatement.Variable);
                CollectOwnRoutineVariables(forStatement.Statements, variables);
                break;

            case BoundSelectCaseStatement selectCaseStatement:
                foreach (var clause in selectCaseStatement.Clauses)
                {
                    CollectOwnRoutineVariables(clause.Statements, variables);
                }

                CollectOwnRoutineVariables(selectCaseStatement.ElseStatements, variables);
                break;
        }
    }

    private static void CollectOwnRoutineVariables(IEnumerable<BoundStatement> statements, HashSet<VariableSymbol> variables)
    {
        foreach (var statement in statements)
        {
            CollectOwnRoutineVariables(statement, variables);
        }
    }

    private static bool TryGetCapturedVariable(
        BoundStatement statement,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    if (TryGetCapturedVariable(declaration, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }
                break;

            case BoundVariableDeclarationStatement declaration:
                if (TryGetCapturedVariable(declaration.Initializer, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(declaration.ArrayBounds, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundAssignmentStatement assignment:
                if (TryGetCapturedVariable(assignment.Target, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(assignment.Expression, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundEraseStatement eraseStatement:
                if (TryGetCapturedVariable(eraseStatement.Target, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundRedimStatement redimStatement:
                if (TryGetCapturedVariable(redimStatement.Target, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(redimStatement.ArrayBounds, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundPrintStatement printStatement:
                foreach (var item in printStatement.Items)
                {
                    if (TryGetCapturedVariable(item.Expression, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }
                break;

            case BoundPrintUsingStatement printUsingStatement:
                if (TryGetCapturedVariable(printUsingStatement.FormatExpression, ownVariables, out variable, out location))
                {
                    return true;
                }

                foreach (var item in printUsingStatement.Items)
                {
                    if (TryGetCapturedVariable(item.Expression, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }
                break;

            case BoundInputStatement inputStatement:
                foreach (var inputVariable in inputStatement.Variables)
                {
                    if (TryGetCapturedVariable(inputVariable, statement.Location, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }
                break;

            case BoundOpenStatement openStatement:
                if ((openStatement.EncodingExpression is not null && TryGetCapturedVariable(openStatement.EncodingExpression, ownVariables, out variable, out location))
                    || TryGetCapturedVariable(openStatement.PathExpression, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(openStatement.FileVariable, statement.Location, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundCloseStatement closeStatement:
                if (TryGetCapturedVariable(closeStatement.FileVariable, statement.Location, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundIfStatement ifStatement:
                if (TryGetCapturedVariable(ifStatement.Condition, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(ifStatement.Statements, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(ifStatement.ElseStatements, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundWhileStatement whileStatement:
                if (TryGetCapturedVariable(whileStatement.Condition, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(whileStatement.Statements, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundDoLoopStatement doLoopStatement:
                if (TryGetCapturedVariable(doLoopStatement.Condition, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(doLoopStatement.Statements, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundForStatement forStatement:
                if (TryGetCapturedVariable(forStatement.Variable, statement.Location, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(forStatement.StartExpression, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(forStatement.EndExpression, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(forStatement.StepExpression, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(forStatement.Statements, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundReturnStatement returnStatement:
                if (TryGetCapturedVariable(returnStatement.Expression, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundExpressionStatement expressionStatement:
                if (TryGetCapturedVariable(expressionStatement.Expression, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;

            case BoundSelectCaseStatement selectCaseStatement:
                if (TryGetCapturedVariable(selectCaseStatement.TestExpression, ownVariables, out variable, out location))
                {
                    return true;
                }

                foreach (var clause in selectCaseStatement.Clauses)
                {
                    foreach (var selector in clause.Selectors)
                    {
                        if (TryGetCapturedVariable(selector, ownVariables, out variable, out location))
                        {
                            return true;
                        }
                    }

                    if (TryGetCapturedVariable(clause.Statements, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }

                if (TryGetCapturedVariable(selectCaseStatement.ElseStatements, ownVariables, out variable, out location))
                {
                    return true;
                }
                break;
        }

        variable = null!;
        location = default;
        return false;
    }

    private static bool TryGetCapturedVariable(
        IEnumerable<BoundStatement> statements,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        foreach (var statement in statements)
        {
            if (TryGetCapturedVariable(statement, ownVariables, out variable, out location))
            {
                return true;
            }
        }

        variable = null!;
        location = default;
        return false;
    }

    private static bool TryGetCapturedVariable(
        IReadOnlyList<BoundArrayBound> bounds,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        foreach (var bound in bounds)
        {
            if (TryGetCapturedVariable(bound.LowerBound, ownVariables, out variable, out location)
                || TryGetCapturedVariable(bound.UpperBound, ownVariables, out variable, out location))
            {
                return true;
            }
        }

        variable = null!;
        location = default;
        return false;
    }

    private static bool TryGetCapturedVariable(
        BoundSelectCaseSelector selector,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        switch (selector)
        {
            case BoundCaseValueSelector valueSelector:
                return TryGetCapturedVariable(valueSelector.Expression, ownVariables, out variable, out location);

            case BoundCaseRangeSelector rangeSelector:
                return TryGetCapturedVariable(rangeSelector.LowerBound, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(rangeSelector.UpperBound, ownVariables, out variable, out location);

            case BoundCaseComparisonSelector comparisonSelector:
                return TryGetCapturedVariable(comparisonSelector.Expression, ownVariables, out variable, out location);

            default:
                variable = null!;
                location = default;
                return false;
        }
    }

    private static bool TryGetCapturedVariable(
        BoundExpression? expression,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        switch (expression)
        {
            case null:
                variable = null!;
                location = default;
                return false;

            case BoundVariableExpression variableExpression:
                return TryGetCapturedVariable(variableExpression.Variable, variableExpression.Location, ownVariables, out variable, out location);

            case BoundArrayAccessExpression arrayAccessExpression:
                return TryGetCapturedVariable(arrayAccessExpression.Target, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(arrayAccessExpression.Indices, ownVariables, out variable, out location);

            case BoundFieldAccessExpression fieldAccessExpression:
                return TryGetCapturedVariable(fieldAccessExpression.Target, ownVariables, out variable, out location);

            case BoundUnaryExpression unaryExpression:
                return TryGetCapturedVariable(unaryExpression.Operand, ownVariables, out variable, out location);

            case BoundBinaryExpression binaryExpression:
                return TryGetCapturedVariable(binaryExpression.Left, ownVariables, out variable, out location)
                    || TryGetCapturedVariable(binaryExpression.Right, ownVariables, out variable, out location);

            case BoundCallExpression callExpression:
                if (TryGetCapturedVariable(callExpression.Target, ownVariables, out variable, out location))
                {
                    return true;
                }

                foreach (var argument in callExpression.Arguments)
                {
                    if (TryGetCapturedVariable(argument, ownVariables, out variable, out location))
                    {
                        return true;
                    }
                }
                break;

            case BoundConversionExpression conversionExpression:
                return TryGetCapturedVariable(conversionExpression.Expression, ownVariables, out variable, out location);
        }

        variable = null!;
        location = default;
        return false;
    }

    private static bool TryGetCapturedVariable(
        IReadOnlyList<BoundExpression> expressions,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation location)
    {
        foreach (var expression in expressions)
        {
            if (TryGetCapturedVariable(expression, ownVariables, out variable, out location))
            {
                return true;
            }
        }

        variable = null!;
        location = default;
        return false;
    }

    private static bool TryGetCapturedVariable(
        VariableSymbol candidate,
        TextLocation location,
        IReadOnlySet<VariableSymbol> ownVariables,
        out VariableSymbol variable,
        out TextLocation capturedLocation)
    {
        if (!candidate.IsGlobal && !ownVariables.Contains(candidate))
        {
            variable = candidate;
            capturedLocation = location;
            return true;
        }

        variable = null!;
        capturedLocation = default;
        return false;
    }

    private static bool HasUnsupportedVmSemantics(BoundStatement statement, bool insideNestedBlock)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                return declarations.Declarations.Any(declaration => HasUnsupportedVmSemantics(declaration, insideNestedBlock));

            case BoundVariableDeclarationStatement declaration:
                return false;
            case BoundForStatement forStatement:
                return forStatement.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            case BoundTryStatement tryStatement:
                return tryStatement.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true))
                    || tryStatement.CatchStatements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true))
                    || tryStatement.FinallyStatements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            case BoundIfStatement ifStatement:
                return ifStatement.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true))
                    || ifStatement.ElseStatements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            case BoundSelectCaseStatement selectCaseStatement:
                return selectCaseStatement.Clauses.Any(clause => clause.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true)))
                    || selectCaseStatement.ElseStatements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            case BoundWhileStatement whileStatement:
                return whileStatement.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            case BoundDoLoopStatement doLoopStatement:
                return doLoopStatement.Statements.Any(statement => HasUnsupportedVmSemantics(statement, insideNestedBlock: true));

            default:
                return false;
        }
    }

    private static Diagnostic CreateGenericBytecodeCoverageDiagnostic(TextLocation location)
    {
        return new Diagnostic(
            DiagnosticCodes.BytecodeCoverage,
            "Компилятор не создал исполняемый байт-код для текущей программы.",
            location);
    }

    private static ExecutionResult CreateExecutionResult(
        ExecutionStatus status,
        IEnumerable<Diagnostic> diagnostics,
        IReadOnlyList<string> output,
        IDiagnosticSink diagnosticSink,
        ExecutionEngineKind? actualEngine,
        TimeSpan frontendPreparationDuration = default,
        TimeSpan loweringDuration = default,
        TimeSpan bytecodeEmissionDuration = default,
        TimeSpan runtimeExecutionDuration = default,
        bool usedCompiledCache = false,
        bool usedLoweredCache = false,
        bool usedBytecodeCache = false,
        BytecodeVmStopState? stopState = null,
        string pendingOutput = "")
    {
        var diagnosticList = diagnostics.ToArray();

        foreach (var diagnostic in diagnosticList)
        {
            diagnosticSink.ReportDiagnostic(diagnostic);
        }

        return new ExecutionResult(
            status,
            diagnosticList,
            output.ToArray(),
            actualEngine,
            frontendPreparationDuration,
            loweringDuration,
            bytecodeEmissionDuration,
            runtimeExecutionDuration,
            usedCompiledCache,
            usedLoweredCache,
            usedBytecodeCache,
            stopState,
            pendingOutput);
    }
}

