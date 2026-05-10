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
            NameExpressionSyntax nameExpression => nameExpression.IdentifierToken.Location,
            UnaryExpressionSyntax unaryExpression => unaryExpression.OperatorToken.Location,
            BinaryExpressionSyntax binaryExpression => binaryExpression.OperatorToken.Location,
            ParenthesizedExpressionSyntax parenthesizedExpression => parenthesizedExpression.OpenParenToken.Location,
            CallExpressionSyntax callExpression => GetLocation(callExpression.Target),
            ArrayAccessExpressionSyntax arrayAccessExpression => arrayAccessExpression.Identifier.Location,
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
            ScreenStatementSyntax screenStatement => screenStatement.ScreenKeyword.Location,
            ClsStatementSyntax clsStatement => clsStatement.ClsKeyword.Location,
            ColorStatementSyntax colorStatement => colorStatement.ColorKeyword.Location,
            LocateStatementSyntax locateStatement => locateStatement.LocateKeyword.Location,
            PSetStatementSyntax psetStatement => psetStatement.Keyword.Location,
            LineStatementSyntax lineStatement => lineStatement.LineKeyword.Location,
            CircleStatementSyntax circleStatement => circleStatement.CircleKeyword.Location,
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
            DoLoopStatementSyntax doLoopStatement => doLoopStatement.DoKeyword.Location,
            WhileStatementSyntax whileStatement => whileStatement.WhileKeyword.Location,
            ExitForStatementSyntax exitForStatement => exitForStatement.ExitKeyword.Location,
            ExitDoStatementSyntax exitDoStatement => exitDoStatement.ExitKeyword.Location,
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
