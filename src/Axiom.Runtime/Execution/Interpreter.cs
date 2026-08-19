using Axiom.Core;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private const int MaxCallDepth = 200;

    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<FunctionSymbol, BoundRoutine> _routines;
    private readonly RuntimeScope _globalScope = new(parent: null);
    private readonly Stack<CallFrame> _callStack = new();
    private readonly Stack<RuntimeScope> _scopeStack = new();
    private readonly HashSet<RuntimeTextFile> _openFiles = [];
    private readonly StringBuilder _pendingOutput = new();
    private readonly Dictionary<TileTransformCacheKey, RuntimeImageValue> _tileTransformCache = [];
    private bool _hasPendingOutput;
    private readonly IRuntimeHost _host;
    private Diagnostic? _lastRuntimeFailureDiagnostic;
    private bool _cancelled;

    private Interpreter(BoundProgram program, IRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(host);

        Program = program;
        _host = host;
        _routines = program.Routines.ToDictionary(static routine => routine.Symbol);
    }

    public BoundProgram Program { get; }

    public static RuntimeResult Execute(BoundProgram program, IRuntimeHost? host = null)
    {
        var interpreter = new Interpreter(program, host ?? NullRuntimeHost.Instance);

        try
        {
            interpreter.ExecuteProgram();
        }
        catch (RuntimeFailureException)
        {
        }
        finally
        {
            interpreter.FlushPendingOutput(forceEmptyLine: false);
            interpreter.CloseAllOpenFiles();
        }

        return new RuntimeResult(interpreter._diagnostics, cancelled: interpreter._cancelled);
    }

    private RuntimeScope CurrentScope => _scopeStack.Peek();

    private bool CheckCancellationRequested()
    {
        if (!_host.IsCancellationRequested)
        {
            return false;
        }

        _cancelled = true;
        return true;
    }

    private void ExecuteProgram()
    {
        _scopeStack.Push(_globalScope);

        try
        {
            var signal = ExecuteStatements(Program.GlobalStatements);

            if (signal.HasReturn)
            {
                ReportInvalidRuntimeState("RETURN недопустим на верхнем уровне программы.", Program.Location);
            }

            if (signal.HasExitFor)
            {
                ReportInvalidRuntimeState("EXIT FOR недопустим вне FOR ... NEXT.", Program.Location);
            }
        }
        finally
        {
            _scopeStack.Pop();
        }
    }
}
