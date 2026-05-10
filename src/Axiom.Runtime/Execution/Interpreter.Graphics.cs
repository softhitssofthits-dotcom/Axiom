using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private ExecutionSignal ExecuteScreen(BoundScreenStatement statement)
    {
        var mode = EvaluateExpression(statement.ModeExpression).AsInteger();
        if (mode == 0)
        {
            FlushPendingOutput(forceEmptyLine: false);
            _host.DeactivateScreen();
            return ExecutionSignal.None;
        }

        if (mode is < 1 or > 10)
        {
            ReportBuiltInArgumentError("SCREEN", "mode", "должен быть в диапазоне 0..10.", statement.Location);
            throw new RuntimeFailureException();
        }

        FlushPendingOutput(forceEmptyLine: false);
        _host.ActivateScreen(mode);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteCls(BoundClsStatement statement)
    {
        if (_host.IsScreenActive)
        {
            _host.ClearGraphics();
        }
        else
        {
            _host.WriteOutput(string.Empty);
        }

        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteColor(BoundColorStatement statement)
    {
        RequireScreenActive("COLOR", statement.Location);
        var foreground = EvaluateExpression(statement.ForegroundExpression).AsInteger();
        int? background = statement.BackgroundExpression is null ? null : EvaluateExpression(statement.BackgroundExpression).AsInteger();
        _host.SetTextColor(foreground, background);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteLocate(BoundLocateStatement statement)
    {
        RequireScreenActive("LOCATE", statement.Location);
        _host.LocateText(EvaluateExpression(statement.RowExpression).AsInteger(), EvaluateExpression(statement.ColumnExpression).AsInteger());
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecutePSet(BoundPSetStatement statement)
    {
        RequireScreenActive(statement.Reset ? "PRESET" : "PSET", statement.Location);
        var color = statement.ColorExpression is null ? (statement.Reset ? 0 : 15) : EvaluateExpression(statement.ColorExpression).AsInteger();
        _host.SetPixel(EvaluateExpression(statement.XExpression).AsLong(), EvaluateExpression(statement.YExpression).AsLong(), color);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteLine(BoundLineStatement statement)
    {
        RequireScreenActive("LINE", statement.Location);
        var color = statement.ColorExpression is null ? 15 : EvaluateExpression(statement.ColorExpression).AsInteger();
        var kind = statement.BoxKind switch
        {
            BoundLineBoxKind.Border => GraphicsLineKind.Box,
            BoundLineBoxKind.Filled => GraphicsLineKind.FilledBox,
            _ => GraphicsLineKind.Normal
        };

        _host.DrawLine(
            EvaluateExpression(statement.X1Expression).AsLong(),
            EvaluateExpression(statement.Y1Expression).AsLong(),
            EvaluateExpression(statement.X2Expression).AsLong(),
            EvaluateExpression(statement.Y2Expression).AsLong(),
            color,
            kind);
        return ExecutionSignal.None;
    }

    private ExecutionSignal ExecuteCircle(BoundCircleStatement statement)
    {
        RequireScreenActive("CIRCLE", statement.Location);
        var color = statement.ColorExpression is null ? 15 : EvaluateExpression(statement.ColorExpression).AsInteger();
        _host.DrawCircle(
            EvaluateExpression(statement.XExpression).AsLong(),
            EvaluateExpression(statement.YExpression).AsLong(),
            EvaluateExpression(statement.RadiusExpression).AsLong(),
            color);
        return ExecutionSignal.None;
    }

    private RuntimeValue EvaluatePoint(BoundCallExpression expression, IReadOnlyList<RuntimeValue> arguments)
    {
        RequireScreenActive("POINT", expression.Location);
        return RuntimeValue.FromInteger(_host.GetPixel(arguments[0].AsLong(), arguments[1].AsLong()));
    }

    private void RequireScreenActive(string operationName, TextLocation location)
    {
        if (_host.IsScreenActive)
        {
            return;
        }

        ReportRuntimeDiagnostic("MB3010", $"{operationName} требует активного графического режима SCREEN.", location);
        throw new RuntimeFailureException();
    }
}
