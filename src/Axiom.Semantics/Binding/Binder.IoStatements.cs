using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private BoundStatement BindPrintStatement(PrintStatementSyntax statement)
    {
        var items = new List<BoundPrintItem>(statement.Items.Count);

        foreach (var item in statement.Items)
        {
            items.Add(new BoundPrintItem(BindExpression(item.Expression), MapPrintSeparator(item.SeparatorToken)));
        }

        return new BoundPrintStatement(GetLocation(statement), items);
    }

    private BoundStatement BindPrintUsingStatement(PrintUsingStatementSyntax statement)
    {
        var formatExpression = BindExpression(statement.FormatExpression, TypeSymbol.String);
        var items = new List<BoundPrintItem>(statement.Items.Count);

        foreach (var item in statement.Items)
        {
            items.Add(new BoundPrintItem(BindExpression(item.Expression), MapPrintSeparator(item.SeparatorToken)));
        }

        return new BoundPrintUsingStatement(GetLocation(statement), formatExpression, items);
    }

    private BoundStatement BindInputStatement(InputStatementSyntax statement)
    {
        var variables = new List<VariableSymbol>(statement.Identifiers.Count);

        foreach (var identifier in statement.Identifiers)
        {
            var variable = LookupVariable(identifier.Text, identifier.Location);

            if (variable.IsReadOnly)
            {
                ReportAssignToConst(variable.Name, identifier.Location);
            }

            variables.Add(variable);
        }

        var promptText = statement.PromptToken?.Value as string;
        return new BoundInputStatement(GetLocation(statement), variables, promptText);
    }

    private BoundStatement BindSleepStatement(SleepStatementSyntax statement)
    {
        return new BoundSleepStatement(
            GetLocation(statement),
            statement.DurationExpression is null ? null : BindExpression(statement.DurationExpression, TypeSymbol.Long));
    }

    private BoundStatement BindScreenStatement(ScreenStatementSyntax statement)
    {
        return new BoundScreenStatement(GetLocation(statement), BindExpression(statement.ModeExpression, TypeSymbol.Integer));
    }

    private BoundStatement BindColorStatement(ColorStatementSyntax statement)
    {
        var backgroundMode = statement switch
        {
            { TransparentKeyword: not null } => BoundColorBackgroundMode.Transparent,
            { BackgroundExpression: not null } => BoundColorBackgroundMode.Color,
            _ => BoundColorBackgroundMode.Unchanged
        };

        return new BoundColorStatement(
            GetLocation(statement),
            BindExpression(statement.ForegroundExpression, TypeSymbol.Integer),
            backgroundMode,
            backgroundMode == BoundColorBackgroundMode.Color
                ? BindExpression(statement.BackgroundExpression!, TypeSymbol.Integer)
                : null);
    }

    private BoundStatement BindLocateStatement(LocateStatementSyntax statement)
    {
        return new BoundLocateStatement(
            GetLocation(statement),
            BindExpression(statement.RowExpression, TypeSymbol.Integer),
            BindExpression(statement.ColumnExpression, TypeSymbol.Integer));
    }

    private BoundStatement BindPSetStatement(PSetStatementSyntax statement)
    {
        return new BoundPSetStatement(
            GetLocation(statement),
            BindExpression(statement.Point.XExpression, TypeSymbol.Long),
            BindExpression(statement.Point.YExpression, TypeSymbol.Long),
            statement.ColorExpression is null ? null : BindExpression(statement.ColorExpression, TypeSymbol.Integer),
            statement.Reset,
            statement.IsRelative);
    }

    private BoundStatement BindLineStatement(LineStatementSyntax statement)
    {
        return new BoundLineStatement(
            GetLocation(statement),
            statement.StartPoint is null ? null : BindExpression(statement.StartPoint.XExpression, TypeSymbol.Long),
            statement.StartPoint is null ? null : BindExpression(statement.StartPoint.YExpression, TypeSymbol.Long),
            BindExpression(statement.EndPoint.XExpression, TypeSymbol.Long),
            BindExpression(statement.EndPoint.YExpression, TypeSymbol.Long),
            statement.ColorExpression is null ? null : BindExpression(statement.ColorExpression, TypeSymbol.Integer),
            statement.BoxKind switch
            {
                LineBoxKind.Border => BoundLineBoxKind.Border,
                LineBoxKind.Filled => BoundLineBoxKind.Filled,
                _ => BoundLineBoxKind.None
            },
            statement.StartPoint is not null,
            statement.IsStartRelative,
            statement.IsEndRelative);
    }

    private BoundStatement BindCircleStatement(CircleStatementSyntax statement)
    {
        return new BoundCircleStatement(
            GetLocation(statement),
            BindExpression(statement.CenterPoint.XExpression, TypeSymbol.Long),
            BindExpression(statement.CenterPoint.YExpression, TypeSymbol.Long),
            BindExpression(statement.RadiusExpression, TypeSymbol.Double),
            statement.ColorExpression is null ? null : BindExpression(statement.ColorExpression, TypeSymbol.Integer),
            statement.StartExpression is null ? null : BindExpression(statement.StartExpression, TypeSymbol.Double),
            statement.EndExpression is null ? null : BindExpression(statement.EndExpression, TypeSymbol.Double),
            statement.AspectExpression is null ? null : BindExpression(statement.AspectExpression, TypeSymbol.Double),
            statement.IsRelative);
    }

    private BoundStatement BindPaintStatement(PaintStatementSyntax statement)
    {
        var fillExpression = statement.FillColorExpression is null ? null : BindPaintFillExpression(statement.FillColorExpression);
        var borderExpression = statement.BorderColorExpression is null ? null : BindPaintBorderExpression(statement.BorderColorExpression);
        var backgroundExpression = statement.BackgroundExpression is null ? null : BindPaintBackgroundExpression(statement.BackgroundExpression);

        if (backgroundExpression is not null && fillExpression is null)
        {
            ReportInvalidPaintBackgroundWithoutPattern(GetLocation(statement.BackgroundExpression!));
            backgroundExpression = new BoundErrorExpression(GetLocation(statement.BackgroundExpression!));
        }
        else if (backgroundExpression is not null
                 && fillExpression is not null
                 && fillExpression.Type != TypeSymbol.String
                 && fillExpression.Type != TypeSymbol.Error)
        {
            ReportInvalidPaintBackgroundWithoutPattern(GetLocation(statement.BackgroundExpression!));
            backgroundExpression = new BoundErrorExpression(GetLocation(statement.BackgroundExpression!));
        }

        return new BoundPaintStatement(
            GetLocation(statement),
            BindExpression(statement.Point.XExpression, TypeSymbol.Long),
            BindExpression(statement.Point.YExpression, TypeSymbol.Long),
            fillExpression,
            borderExpression,
            backgroundExpression,
            statement.IsRelative);
    }

    private BoundStatement BindDrawStatement(DrawStatementSyntax statement)
    {
        return new BoundDrawStatement(
            GetLocation(statement),
            BindExpression(statement.CommandExpression, TypeSymbol.String));
    }

    private BoundExpression BindPaintFillExpression(ExpressionSyntax expression)
    {
        var boundExpression = BindExpression(expression);
        if (boundExpression.Type == TypeSymbol.Error
            || boundExpression.Type == TypeSymbol.String
            || IsNumeric(boundExpression.Type))
        {
            return boundExpression;
        }

        ReportInvalidPaintFillType(boundExpression.Type, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundExpression BindPaintBorderExpression(ExpressionSyntax expression)
    {
        var boundExpression = BindExpression(expression);
        if (boundExpression.Type == TypeSymbol.Error || IsNumeric(boundExpression.Type))
        {
            return boundExpression;
        }

        ReportInvalidPaintBorderType(boundExpression.Type, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundExpression BindPaintBackgroundExpression(ExpressionSyntax expression)
    {
        var boundExpression = BindExpression(expression);
        if (boundExpression.Type == TypeSymbol.Error || boundExpression.Type == TypeSymbol.String)
        {
            return boundExpression;
        }

        ReportInvalidPaintBackgroundType(boundExpression.Type, GetLocation(expression));
        return new BoundErrorExpression(GetLocation(expression));
    }

    private BoundStatement BindOnKeyStatement(OnKeyStatementSyntax statement)
    {
        var keyExpression = BindExpression(statement.KeyExpression, TypeSymbol.String);

        if (statement.DisablesHandler)
        {
            return new BoundOnKeyStatement(GetLocation(statement), keyExpression, handlerExpression: null, BoundOnKeyActionKind.Unregister);
        }

        if (statement.PausesHandler)
        {
            return new BoundOnKeyStatement(GetLocation(statement), keyExpression, handlerExpression: null, BoundOnKeyActionKind.Pause);
        }

        if (statement.ResumesHandler)
        {
            return new BoundOnKeyStatement(GetLocation(statement), keyExpression, handlerExpression: null, BoundOnKeyActionKind.Resume);
        }

        if (_scope.TryLookupFunction(statement.ActionToken.Text, out var function) && function is not null)
        {
            var handlerReceivesKeyText = FunctionAcceptsOnKeyEvent(function);
            if (!function.IsSub || !handlerReceivesKeyText.HasValue)
            {
                ReportInvalidOnKeyHandler(statement.ActionToken.Text, statement.ActionToken.Location);
                return new BoundOnKeyStatement(GetLocation(statement), keyExpression, handlerExpression: null, BoundOnKeyActionKind.Unregister);
            }

            return new BoundOnKeyStatement(
                GetLocation(statement),
                keyExpression,
                new BoundFunctionExpression(statement.ActionToken.Location, function),
                BoundOnKeyActionKind.Register,
                handlerReceivesKeyText.Value);
        }

        ReportUnknownCallable(statement.ActionToken.Text, statement.ActionToken.Location);
        return new BoundOnKeyStatement(GetLocation(statement), keyExpression, handlerExpression: null, BoundOnKeyActionKind.Unregister);
    }

    private static bool? FunctionAcceptsOnKeyEvent(FunctionSymbol function)
    {
        if (function.Parameters.Count == 0)
        {
            return false;
        }

        if (function.Parameters.Count == 1
            && function.Parameters[0].Type == TypeSymbol.String
            && !function.Parameters[0].IsByRef)
        {
            return true;
        }

        return null;
    }

    private BoundStatement BindOnMouseStatement(OnMouseStatementSyntax statement)
    {
        var (buttonExpression, eventKind) = BindOnMouseTarget(statement);

        if (statement.DisablesHandler)
        {
            return new BoundOnMouseStatement(GetLocation(statement), buttonExpression, eventKind, handlerExpression: null, BoundOnMouseActionKind.Unregister);
        }

        if (statement.PausesHandler)
        {
            return new BoundOnMouseStatement(GetLocation(statement), buttonExpression, eventKind, handlerExpression: null, BoundOnMouseActionKind.Pause);
        }

        if (statement.ResumesHandler)
        {
            return new BoundOnMouseStatement(GetLocation(statement), buttonExpression, eventKind, handlerExpression: null, BoundOnMouseActionKind.Resume);
        }

        if (_scope.TryLookupFunction(statement.ActionToken.Text, out var function) && function is not null)
        {
            var handlerReceivesEvent = FunctionAcceptsOnMouseEvent(function);
            if (!function.IsSub || !handlerReceivesEvent.HasValue)
            {
                ReportInvalidOnMouseHandler(statement.ActionToken.Text, statement.ActionToken.Location);
                return new BoundOnMouseStatement(GetLocation(statement), buttonExpression, eventKind, handlerExpression: null, BoundOnMouseActionKind.Unregister);
            }

            return new BoundOnMouseStatement(
                GetLocation(statement),
                buttonExpression,
                eventKind,
                new BoundFunctionExpression(statement.ActionToken.Location, function),
                BoundOnMouseActionKind.Register,
                handlerReceivesEvent.Value);
        }

        ReportUnknownCallable(statement.ActionToken.Text, statement.ActionToken.Location);
        return new BoundOnMouseStatement(GetLocation(statement), buttonExpression, eventKind, handlerExpression: null, BoundOnMouseActionKind.Unregister);
    }

    private (BoundExpression ButtonExpression, BoundMouseEventKind EventKind) BindOnMouseTarget(OnMouseStatementSyntax statement)
    {
        if (statement.EventExpression is null && TryGetStringLiteral(statement.ButtonExpression, out var singleEventName))
        {
            if (TryParseMouseEvent(singleEventName, out var singleEventKind)
                && singleEventKind is BoundMouseEventKind.Move or BoundMouseEventKind.Wheel)
            {
                return (new BoundLiteralExpression(GetLocation(statement.ButtonExpression), 0, TypeSymbol.Integer), singleEventKind);
            }

            ReportInvalidOnMouseEvent(GetLocation(statement.ButtonExpression));
            return (new BoundLiteralExpression(GetLocation(statement.ButtonExpression), 0, TypeSymbol.Integer), BoundMouseEventKind.Press);
        }

        var buttonExpression = BindExpression(statement.ButtonExpression, TypeSymbol.Integer);
        var eventKind = BoundMouseEventKind.Press;

        if (statement.EventExpression is not null)
        {
            if (!TryGetStringLiteral(statement.EventExpression, out var eventName)
                || !TryParseMouseEvent(eventName, out eventKind)
                || eventKind is BoundMouseEventKind.Move or BoundMouseEventKind.Wheel)
            {
                ReportInvalidOnMouseEvent(GetLocation(statement.EventExpression));
                eventKind = BoundMouseEventKind.Press;
            }
        }

        return (buttonExpression, eventKind);
    }

    private static bool TryGetStringLiteral(ExpressionSyntax expression, out string value)
    {
        if (expression is LiteralExpressionSyntax { LiteralToken.Value: string literalValue })
        {
            value = literalValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryParseMouseEvent(string text, out BoundMouseEventKind eventKind)
    {
        var normalized = text.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        eventKind = normalized switch
        {
            "DOWN" or "PRESS" or "PRESSED" => BoundMouseEventKind.Press,
            "UP" or "RELEASE" or "RELEASED" => BoundMouseEventKind.Release,
            "MOVE" or "MOVED" or "MOUSEMOVE" => BoundMouseEventKind.Move,
            "WHEEL" or "MOUSEWHEEL" or "SCROLL" or "SCROLLED" => BoundMouseEventKind.Wheel,
            _ => BoundMouseEventKind.Press
        };

        return normalized is "DOWN" or "PRESS" or "PRESSED" or "UP" or "RELEASE" or "RELEASED" or "MOVE" or "MOVED" or "MOUSEMOVE" or "WHEEL" or "MOUSEWHEEL" or "SCROLL" or "SCROLLED";
    }

    private static bool? FunctionAcceptsOnMouseEvent(FunctionSymbol function)
    {
        if (function.Parameters.Count == 0)
        {
            return false;
        }

        if (function.Parameters.Count == 3
            && function.Parameters[0].Type == TypeSymbol.Integer
            && function.Parameters[1].Type == TypeSymbol.Long
            && function.Parameters[2].Type == TypeSymbol.Long
            && function.Parameters.All(static parameter => !parameter.IsByRef))
        {
            return true;
        }

        return null;
    }

    private BoundStatement BindOpenStatement(OpenStatementSyntax statement)
    {
        var pathExpression = BindExpression(statement.PathExpression, TypeSymbol.String);
        var fileVariable = LookupVariable(statement.FileIdentifier.Text, statement.FileIdentifier.Location);

        if (fileVariable.IsArray)
        {
            ReportTypeMismatch(TypeSymbol.Error, TypeSymbol.File, statement.FileIdentifier.Location);
        }
        else if (fileVariable.Type != TypeSymbol.File && fileVariable.Type != TypeSymbol.Error)
        {
            ReportTypeMismatch(fileVariable.Type, TypeSymbol.File, statement.FileIdentifier.Location);
        }

        var mode = statement.ModeKeyword.Kind switch
        {
            SyntaxKind.InputKeyword => BoundFileOpenMode.Input,
            SyntaxKind.OutputKeyword => BoundFileOpenMode.Output,
            SyntaxKind.AppendKeyword => BoundFileOpenMode.Append,
            _ => BoundFileOpenMode.Input
        };

        return new BoundOpenStatement(GetLocation(statement), pathExpression, fileVariable, mode);
    }

    private BoundStatement BindCloseStatement(CloseStatementSyntax statement)
    {
        var fileVariable = LookupVariable(statement.FileIdentifier.Text, statement.FileIdentifier.Location);

        if (fileVariable.IsArray)
        {
            ReportTypeMismatch(TypeSymbol.Error, TypeSymbol.File, statement.FileIdentifier.Location);
        }
        else if (fileVariable.Type != TypeSymbol.File && fileVariable.Type != TypeSymbol.Error)
        {
            ReportTypeMismatch(fileVariable.Type, TypeSymbol.File, statement.FileIdentifier.Location);
        }

        return new BoundCloseStatement(GetLocation(statement), fileVariable);
    }

    private static PrintSeparatorKind? MapPrintSeparator(SyntaxToken? token)
    {
        return token?.Kind switch
        {
            null => null,
            SyntaxKind.SemicolonToken => PrintSeparatorKind.Semicolon,
            SyntaxKind.CommaToken => PrintSeparatorKind.Comma,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token.Kind, "Unsupported print separator token.")
        };
    }
}
