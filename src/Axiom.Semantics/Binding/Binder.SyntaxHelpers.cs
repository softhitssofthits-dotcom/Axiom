using Axiom.Core;
using Axiom.Syntax;
using System.Globalization;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private static readonly TextLocation DefaultLocation = new(new TextSpan(0, 0), 1, 1);

    private static string GetOperatorText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.AmpersandToken => "&",
            SyntaxKind.PlusToken => "+",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.StarToken => "*",
            SyntaxKind.CaretToken => "^",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.BackslashToken => "\\",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.NotEqualsToken => "<>",
            SyntaxKind.LessToken => "<",
            SyntaxKind.LessOrEqualsToken => "<=",
            SyntaxKind.LessLessToken => "<<",
            SyntaxKind.GreaterToken => ">",
            SyntaxKind.GreaterOrEqualsToken => ">=",
            SyntaxKind.GreaterGreaterToken => ">>",
            SyntaxKind.AndKeyword => "AND",
            SyntaxKind.AndAlsoKeyword => "ANDALSO",
            SyntaxKind.OrElseKeyword => "ORELSE",
            SyntaxKind.EqvKeyword => "EQV",
            SyntaxKind.ImpKeyword => "IMP",
            SyntaxKind.ModKeyword => "MOD",
            SyntaxKind.OrKeyword => "OR",
            SyntaxKind.NotKeyword => "NOT",
            SyntaxKind.XorKeyword => "XOR",
            _ => kind.ToString()
        };
    }

    private static TextLocation GetLocation(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literalExpression => literalExpression.LiteralToken.Location,
            InterpolatedStringExpressionSyntax interpolatedExpression => interpolatedExpression.StartToken.Location,
            RangeExpressionSyntax rangeExpression => rangeExpression.ToKeyword.Location,
            NamedArgumentExpressionSyntax namedArgumentExpression => namedArgumentExpression.IdentifierToken.Location,
            NameExpressionSyntax nameExpression => nameExpression.IdentifierToken.Location,
            UnaryExpressionSyntax unaryExpression => unaryExpression.OperatorToken.Location,
            BinaryExpressionSyntax binaryExpression => binaryExpression.OperatorToken.Location,
            ParenthesizedExpressionSyntax parenthesizedExpression => parenthesizedExpression.OpenParenToken.Location,
            CallExpressionSyntax callExpression => GetLocation(callExpression.Target),
            ArrayAccessExpressionSyntax arrayAccessExpression => GetLocation(arrayAccessExpression.Target),
            ArrayLiteralExpressionSyntax arrayLiteralExpression => arrayLiteralExpression.OpenBracketToken.Location,
            FieldAccessExpressionSyntax fieldAccessExpression => fieldAccessExpression.Identifier.Location,
            ErrorExpressionSyntax errorExpression => errorExpression.Token.Location,
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unsupported expression.")
        };
    }

    private static TextLocation GetLocation(StatementSyntax statement)
    {
        return statement switch
        {
            LocalSubDeclarationStatementSyntax localSubDeclaration => localSubDeclaration.Declaration.Identifier.Location,
            LocalFunctionDeclarationStatementSyntax localFunctionDeclaration => localFunctionDeclaration.Declaration.Identifier.Location,
            PrintStatementSyntax printStatement => printStatement.PrintKeyword.Location,
            PrintUsingStatementSyntax printUsingStatement => printUsingStatement.PrintKeyword.Location,
            InputStatementSyntax inputStatement => inputStatement.Identifiers.Count > 0
                ? inputStatement.Identifiers[0].Location
                : inputStatement.InputKeyword.Location,
            SleepStatementSyntax sleepStatement => sleepStatement.SleepKeyword.Location,
            RandomizeStatementSyntax randomizeStatement => randomizeStatement.RandomizeKeyword.Location,
            ScreenStatementSyntax screenStatement => screenStatement.ScreenKeyword.Location,
            SmoothStatementSyntax smoothStatement => smoothStatement.SmoothKeyword.Location,
            CanvasStatementSyntax canvasStatement => canvasStatement.CanvasKeyword.Location,
            BeginFrameStatementSyntax beginFrameStatement => beginFrameStatement.BeginFrameKeyword.Location,
            PresentStatementSyntax presentStatement => presentStatement.PresentKeyword.Location,
            CameraStatementSyntax cameraStatement => cameraStatement.CameraKeyword.Location,
            ViewportStatementSyntax viewportStatement => viewportStatement.ViewportKeyword.Location,
            ResetCameraStatementSyntax resetCameraStatement => resetCameraStatement.ResetCameraKeyword.Location,
            ViewStatementSyntax viewStatement => viewStatement.ViewKeyword.Location,
            ViewPrintStatementSyntax viewPrintStatement => viewPrintStatement.ViewKeyword.Location,
            WindowStatementSyntax windowStatement => windowStatement.WindowKeyword.Location,
            ClsStatementSyntax clsStatement => clsStatement.ClsKeyword.Location,
            ColorStatementSyntax colorStatement => colorStatement.ColorKeyword.Location,
            LocateStatementSyntax locateStatement => locateStatement.LocateKeyword.Location,
            PaletteStatementSyntax paletteStatement => paletteStatement.PaletteKeyword.Location,
            PCopyStatementSyntax pcopyStatement => pcopyStatement.PCopyKeyword.Location,
            PSetStatementSyntax psetStatement => psetStatement.Keyword.Location,
            GetStatementSyntax getStatement => getStatement.GetKeyword.Location,
            PutStatementSyntax putStatement => putStatement.PutKeyword.Location,
            LineStatementSyntax lineStatement => lineStatement.LineKeyword.Location,
            CircleStatementSyntax circleStatement => circleStatement.CircleKeyword.Location,
            PaintStatementSyntax paintStatement => paintStatement.PaintKeyword.Location,
            DrawStatementSyntax drawStatement => drawStatement.DrawKeyword.Location,
            DrawImageStatementSyntax drawImageStatement => drawImageStatement.DrawImageKeyword.Location,
            DrawLineStatementSyntax drawLineStatement => drawLineStatement.DrawLineKeyword.Location,
            DrawCircleStatementSyntax drawCircleStatement => drawCircleStatement.DrawCircleKeyword.Location,
            DrawRectStatementSyntax drawRectStatement => drawRectStatement.DrawRectKeyword.Location,
            FillCircleStatementSyntax fillCircleStatement => fillCircleStatement.FillCircleKeyword.Location,
            FillRectStatementSyntax fillRectStatement => fillRectStatement.FillRectKeyword.Location,
            DrawSpriteStatementSyntax drawSpriteStatement => drawSpriteStatement.DrawSpriteKeyword.Location,
            DrawTileStatementSyntax drawTileStatement => drawTileStatement.DrawTileKeyword.Location,
            DrawTilemapStatementSyntax drawTilemapStatement => drawTilemapStatement.DrawTilemapKeyword.Location,
            DrawTileLayerStatementSyntax drawTileLayerStatement => drawTileLayerStatement.DrawTileLayerKeyword.Location,
            SetMapTileStatementSyntax setMapTileStatement => setMapTileStatement.SetMapTileKeyword.Location,
            DrawTextStatementSyntax drawTextStatement => drawTextStatement.DrawTextKeyword.Location,
            OnKeyStatementSyntax onKeyStatement => onKeyStatement.OnKeyword.Location,
            OnMouseStatementSyntax onMouseStatement => onMouseStatement.OnKeyword.Location,
            OpenStatementSyntax openStatement => openStatement.OpenKeyword.Location,
            CloseStatementSyntax closeStatement => closeStatement.CloseKeyword.Location,
            ErrorStatementSyntax errorStatement => errorStatement.ErrorKeyword.Location,
            AssignmentStatementSyntax assignmentStatement => GetLocation(assignmentStatement.Target),
            EraseStatementSyntax eraseStatement => eraseStatement.EraseKeyword.Location,
            RedimStatementSyntax redimStatement => redimStatement.RedimKeyword.Location,
            IfStatementSyntax ifStatement => ifStatement.IfKeyword.Location,
            TryStatementSyntax tryStatement => tryStatement.TryKeyword.Location,
            SelectCaseStatementSyntax selectCaseStatement => selectCaseStatement.SelectKeyword.Location,
            ForStatementSyntax forStatement => forStatement.ForKeyword.Location,
            ForEachStatementSyntax forEachStatement => forEachStatement.ForKeyword.Location,
            DoLoopStatementSyntax doLoopStatement => doLoopStatement.DoKeyword.Location,
            WhileStatementSyntax whileStatement => whileStatement.WhileKeyword.Location,
            ExitForStatementSyntax exitForStatement => exitForStatement.ExitKeyword.Location,
            ExitDoStatementSyntax exitDoStatement => exitDoStatement.ExitKeyword.Location,
            ContinueForStatementSyntax continueForStatement => continueForStatement.ContinueKeyword.Location,
            ContinueWhileStatementSyntax continueWhileStatement => continueWhileStatement.ContinueKeyword.Location,
            ContinueDoStatementSyntax continueDoStatement => continueDoStatement.ContinueKeyword.Location,
            ReturnStatementSyntax returnStatement => returnStatement.ReturnKeyword.Location,
            ExpressionStatementSyntax expressionStatement => GetLocation(expressionStatement.Expression),
            DimStatementSyntax dimStatement => dimStatement.DimKeyword.Location,
            ConstStatementSyntax constStatement => constStatement.ConstKeyword.Location,
            _ => throw new ArgumentOutOfRangeException(nameof(statement), statement, "Unsupported statement.")
        };
    }

    private static TextLocation GetLocation(MemberSyntax declaration)
    {
        return declaration switch
        {
            GlobalStatementSyntax globalStatement => GetLocation(globalStatement.Statement),
            ImportStatementSyntax importStatement => importStatement.ImportKeyword.Location,
            ModuleStatementSyntax moduleStatement => GetLocation(moduleStatement.Statement),
            TypeDeclarationSyntax typeDeclaration => typeDeclaration.Identifier.Location,
            SubDeclarationSyntax subDeclaration => subDeclaration.Identifier.Location,
            FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Identifier.Location,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };
    }

    private static TextLocation GetProgramLocation(ProgramSyntax program)
    {
        ArgumentNullException.ThrowIfNull(program);

        return program.Members.Count > 0
            ? GetLocation(program.Members[0])
            : DefaultLocation;
    }

    private static TextLocation GetParameterLocation(MemberSyntax declaration, int ordinal)
    {
        return declaration switch
        {
            SubDeclarationSyntax subDeclaration => subDeclaration.Parameters[ordinal].Identifier.Location,
            FunctionDeclarationSyntax functionDeclaration => functionDeclaration.Parameters[ordinal].Identifier.Location,
            _ => throw new ArgumentOutOfRangeException(nameof(declaration), declaration, "Unsupported callable declaration.")
        };
    }

    private static bool TryParseLiteralTextAsType(ExpressionSyntax expression, TypeSymbol expectedType, out object? value)
    {
        value = null;

        if (!TryGetNumericLiteralText(expression, out var literalText))
        {
            return false;
        }

        var digits = literalText.AsSpan();
        var negative = digits.Length > 0 && digits[0] == '-';
        if (negative) digits = digits[1..];
        var binary = digits.StartsWith("&B", StringComparison.OrdinalIgnoreCase);
        if (binary || digits.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
        {
            // Лексер уже проверил диапазон положительной величины литерала.
            // Минус остаётся отдельной операцией, а целевой тип проверяется здесь.
            var style = binary ? NumberStyles.AllowBinarySpecifier : NumberStyles.AllowHexSpecifier;
            if (!ulong.TryParse(digits[2..], style,
                CultureInfo.InvariantCulture, out var magnitude) || magnitude > long.MaxValue)
                return false;
            var signedValue = negative ? -(long)magnitude : (long)magnitude;
            if (expectedType == TypeSymbol.Integer && signedValue is >= int.MinValue and <= int.MaxValue)
                value = (int)signedValue;
            else if (expectedType == TypeSymbol.Long)
                value = signedValue;
            else if (expectedType == TypeSymbol.Decimal)
                value = (decimal)signedValue;
            return value is not null;
        }

        if (expectedType == TypeSymbol.Integer
            && int.TryParse(literalText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
        {
            value = integerValue;
            return true;
        }

        if (expectedType == TypeSymbol.Long
            && long.TryParse(literalText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            value = longValue;
            return true;
        }

        if (expectedType == TypeSymbol.Decimal
            && decimal.TryParse(literalText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalValue))
        {
            value = decimalValue;
            return true;
        }

        return false;
    }

    private static bool TryGetNumericLiteralText(ExpressionSyntax expression, out string literalText)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literalExpression when literalExpression.LiteralToken.Kind is SyntaxKind.IntegerLiteralToken or SyntaxKind.LongLiteralToken or SyntaxKind.DoubleLiteralToken:
                literalText = literalExpression.LiteralToken.Text;
                return true;

            case UnaryExpressionSyntax unaryExpression
                when unaryExpression.OperatorToken.Kind == SyntaxKind.MinusToken
                && unaryExpression.Operand is LiteralExpressionSyntax operandLiteral
                && operandLiteral.LiteralToken.Kind is SyntaxKind.IntegerLiteralToken or SyntaxKind.LongLiteralToken or SyntaxKind.DoubleLiteralToken:
                literalText = "-" + operandLiteral.LiteralToken.Text;
                return true;

            default:
                literalText = string.Empty;
                return false;
        }
    }
}
