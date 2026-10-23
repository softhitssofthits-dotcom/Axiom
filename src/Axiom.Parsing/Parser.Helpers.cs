using Axiom.Core;
using Axiom.Lexing;
using Axiom.Syntax;

namespace Axiom.Parsing;

public sealed partial class Parser
{
    private SyntaxToken Current => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;

        if (index >= _tokens.Count)
        {
            return _tokens[^1];
        }

        return _tokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = Current;

        if (_position < _tokens.Count - 1)
        {
            _position++;
        }

        return current;
    }

    private SyntaxToken MatchIdentifier()
    {
        if (Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword)
        {
            return NextToken();
        }

        Diagnostics.Report(
            DiagnosticCodes.ExpectedIdentifier,
            "Ожидался идентификатор.",
            CreateDiagnosticLocation(Current.Location));

        return CreateMissingToken(SyntaxKind.IdentifierToken);
    }

    private SyntaxToken MatchFieldIdentifier()
    {
        if (Current.Kind is SyntaxKind.IdentifierToken or SyntaxKind.LineKeyword)
        {
            return NextToken();
        }

        Diagnostics.Report(
            DiagnosticCodes.ExpectedIdentifier,
            "Ожидался идентификатор.",
            CreateDiagnosticLocation(Current.Location));

        return CreateMissingToken(SyntaxKind.IdentifierToken);
    }

    private bool IsExpressionNameToken(SyntaxKind kind)
    {
        return kind is SyntaxKind.IdentifierToken or SyntaxKind.SelfKeyword or SyntaxKind.LineKeyword;
    }

    private SyntaxToken MatchNamedTypeToken()
    {
        if (Current.Kind is SyntaxKind.IntegerKeyword
            or SyntaxKind.LongKeyword
            or SyntaxKind.DoubleKeyword
            or SyntaxKind.DecimalKeyword
            or SyntaxKind.StringKeyword
            or SyntaxKind.BooleanKeyword
            or SyntaxKind.DateKeyword
            or SyntaxKind.TimeKeyword
            or SyntaxKind.DateTimeKeyword
            or SyntaxKind.UtcDateTimeKeyword
            or SyntaxKind.ImageKeyword
            or SyntaxKind.SpriteKeyword
            or SyntaxKind.TilemapKeyword
            or SyntaxKind.TilesetKeyword
            or SyntaxKind.FileKeyword)
        {
            return NextToken();
        }

        if (Current.Kind == SyntaxKind.IdentifierToken)
        {
            return NextToken();
        }

        ReportExpectedToken("имя типа");
        return CreateMissingToken(SyntaxKind.IntegerKeyword);
    }

    private SyntaxToken Match(SyntaxKind kind, string expectedDisplayName)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }

        ReportExpectedToken(expectedDisplayName);
        return CreateMissingToken(kind);
    }

    private (SyntaxToken endKeyword, SyntaxToken secondKeyword) MatchEndPair(SyntaxKind secondKeywordKind, string displayName)
    {
        if (Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == secondKeywordKind)
        {
            var endKeyword = NextToken();
            var secondKeyword = NextToken();
            return (endKeyword, secondKeyword);
        }

        Diagnostics.Report(
            DiagnosticCodes.ExpectedToken,
            $"Ожидался {displayName}.",
            CreateDiagnosticLocation(Current.Location));

        return (CreateMissingToken(SyntaxKind.EndKeyword), CreateMissingToken(secondKeywordKind));
    }

    private void SkipNewLines()
    {
        while (Current.Kind == SyntaxKind.NewLineToken)
        {
            NextToken();
        }
    }

    private bool IsElseIfClauseStart() => Current.Kind == SyntaxKind.ElseIfKeyword;

    private bool IsElseClauseStart() => Current.Kind == SyntaxKind.ElseKeyword;

    private bool IsCatchClauseStart() => Current.Kind == SyntaxKind.CatchKeyword;

    private bool IsFinallyClauseStart() => Current.Kind == SyntaxKind.FinallyKeyword;

    private bool IsCaseClauseStart() => Current.Kind == SyntaxKind.CaseKeyword;

    private bool IsCaseElseClauseStart() => Current.Kind == SyntaxKind.CaseKeyword && Peek(1).Kind == SyntaxKind.ElseKeyword;

    private bool IsEndIf() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.IfKeyword;

    private bool IsEndSelect() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.SelectKeyword;

    private bool IsLoop() => Current.Kind == SyntaxKind.LoopKeyword;

    private bool IsDoLoopConditionKeyword() => Current.Kind is SyntaxKind.WhileKeyword or SyntaxKind.UntilKeyword;

    private bool IsEndWhile() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.WhileKeyword;

    private bool IsEndTry() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.TryKeyword;

    private bool IsEndSub() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.SubKeyword;

    private bool IsEndFunction() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.FunctionKeyword;

    private bool IsEndType() => Current.Kind == SyntaxKind.EndKeyword && Peek(1).Kind == SyntaxKind.TypeKeyword;

    private bool IsUnexpectedBoundaryToken()
    {
        return Current.Kind == SyntaxKind.ElseIfKeyword
            || Current.Kind == SyntaxKind.ElseKeyword
            || Current.Kind == SyntaxKind.CaseKeyword
            || Current.Kind == SyntaxKind.CatchKeyword
            || Current.Kind == SyntaxKind.FinallyKeyword
            || Current.Kind == SyntaxKind.LoopKeyword
            || Current.Kind == SyntaxKind.NextKeyword
            || Current.Kind == SyntaxKind.EndKeyword;
    }

    private void ReportExpectedToken(string expectedDisplayName)
    {
        Diagnostics.Report(
            DiagnosticCodes.ExpectedToken,
            $"Ожидался {expectedDisplayName}.",
            CreateDiagnosticLocation(Current.Location));
    }

    private void ReportExpectedExpression()
    {
        Diagnostics.Report(
            DiagnosticCodes.ExpectedExpression,
            "Ожидалось выражение.",
            CreateDiagnosticLocation(Current.Location));
    }

    private void ReportUnexpectedToken(SyntaxToken token, string description)
    {
        Diagnostics.Report(
            DiagnosticCodes.UnexpectedToken,
            $"{description}: '{token.Text}'.",
            CreateDiagnosticLocation(token.Location));
    }

    private void ReportDuplicateElse(SyntaxToken token)
    {
        Diagnostics.Report(
            DiagnosticCodes.DuplicateElse,
            "В конструкции IF допускается только один блок ELSE.",
            CreateDiagnosticLocation(token.Location));
    }

    private void ReportDuplicateCaseElse(SyntaxToken token)
    {
        Diagnostics.Report(
            DiagnosticCodes.DuplicateCaseElse,
            "В конструкции SELECT CASE допускается только один блок CASE ELSE.",
            CreateDiagnosticLocation(token.Location));
    }

    private SyntaxToken CreateMissingToken(SyntaxKind kind)
    {
        return new SyntaxToken(kind, string.Empty, CreateDiagnosticLocation(Current.Location));
    }

    private static TextLocation CreateDiagnosticLocation(TextLocation sourceLocation)
    {
        return new TextLocation(new TextSpan(sourceLocation.Span.Start, 0), sourceLocation.Line, sourceLocation.Column, sourceLocation.Document);
    }

    private static bool IsStatementTerminator(SyntaxKind kind)
    {
        return kind is SyntaxKind.NewLineToken
            or SyntaxKind.EndOfFileToken
            or SyntaxKind.ElseIfKeyword
            or SyntaxKind.ElseKeyword
            or SyntaxKind.CaseKeyword
            or SyntaxKind.CatchKeyword
            or SyntaxKind.FinallyKeyword
            or SyntaxKind.LoopKeyword
            or SyntaxKind.NextKeyword
            or SyntaxKind.EndKeyword;
    }

    private static bool IsExpressionTerminator(SyntaxKind kind)
    {
        return kind is SyntaxKind.NewLineToken
            or SyntaxKind.EndOfFileToken
            or SyntaxKind.CommaToken
            or SyntaxKind.SemicolonToken
            or SyntaxKind.CloseParenToken
            or SyntaxKind.CloseBracketToken
            or SyntaxKind.ThenKeyword
            or SyntaxKind.ForKeyword
            or SyntaxKind.ToKeyword
            or SyntaxKind.StepKeyword
            or SyntaxKind.ElseIfKeyword
            or SyntaxKind.ElseKeyword
            or SyntaxKind.CaseKeyword
            or SyntaxKind.CatchKeyword
            or SyntaxKind.FinallyKeyword
            or SyntaxKind.LoopKeyword
            or SyntaxKind.NextKeyword
            or SyntaxKind.EndKeyword;
    }

    private static bool CanConsumeUnexpectedExpressionToken(SyntaxKind kind)
    {
        return !IsExpressionTerminator(kind);
    }

    private static SyntaxKind MapTokenKind(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.BadToken => SyntaxKind.BadToken,
            TokenKind.EndOfFileToken => SyntaxKind.EndOfFileToken,
            TokenKind.NewLineToken => SyntaxKind.NewLineToken,
            TokenKind.CommentToken => SyntaxKind.CommentToken,
            TokenKind.IdentifierToken => SyntaxKind.IdentifierToken,
            TokenKind.IntegerLiteralToken => SyntaxKind.IntegerLiteralToken,
            TokenKind.LongLiteralToken => SyntaxKind.LongLiteralToken,
            TokenKind.DoubleLiteralToken => SyntaxKind.DoubleLiteralToken,
            TokenKind.StringLiteralToken => SyntaxKind.StringLiteralToken,
            TokenKind.BooleanLiteralToken => SyntaxKind.BooleanLiteralToken,
            TokenKind.AmpersandToken => SyntaxKind.AmpersandToken,
            TokenKind.PlusToken => SyntaxKind.PlusToken,
            TokenKind.MinusToken => SyntaxKind.MinusToken,
            TokenKind.StarToken => SyntaxKind.StarToken,
            TokenKind.CaretToken => SyntaxKind.CaretToken,
            TokenKind.SlashToken => SyntaxKind.SlashToken,
            TokenKind.BackslashToken => SyntaxKind.BackslashToken,
            TokenKind.ColonEqualsToken => SyntaxKind.ColonEqualsToken,
            TokenKind.ColonToken => SyntaxKind.ColonToken,
            TokenKind.EqualsToken => SyntaxKind.EqualsToken,
            TokenKind.NotEqualsToken => SyntaxKind.NotEqualsToken,
            TokenKind.LessToken => SyntaxKind.LessToken,
            TokenKind.LessOrEqualsToken => SyntaxKind.LessOrEqualsToken,
            TokenKind.LessLessToken => SyntaxKind.LessLessToken,
            TokenKind.GreaterToken => SyntaxKind.GreaterToken,
            TokenKind.GreaterOrEqualsToken => SyntaxKind.GreaterOrEqualsToken,
            TokenKind.GreaterGreaterToken => SyntaxKind.GreaterGreaterToken,
            TokenKind.OpenParenToken => SyntaxKind.OpenParenToken,
            TokenKind.CloseParenToken => SyntaxKind.CloseParenToken,
            TokenKind.OpenBracketToken => SyntaxKind.OpenBracketToken,
            TokenKind.CloseBracketToken => SyntaxKind.CloseBracketToken,
            TokenKind.CommaToken => SyntaxKind.CommaToken,
            TokenKind.SemicolonToken => SyntaxKind.SemicolonToken,
            TokenKind.DotToken => SyntaxKind.DotToken,
            TokenKind.AndKeyword => SyntaxKind.AndKeyword,
            TokenKind.AsKeyword => SyntaxKind.AsKeyword,
            TokenKind.AppendKeyword => SyntaxKind.AppendKeyword,
            TokenKind.BeginFrameKeyword => SyntaxKind.BeginFrameKeyword,
            TokenKind.BooleanKeyword => SyntaxKind.BooleanKeyword,
            TokenKind.ByRefKeyword => SyntaxKind.ByRefKeyword,
            TokenKind.ByValKeyword => SyntaxKind.ByValKeyword,
            TokenKind.CanvasKeyword => SyntaxKind.CanvasKeyword,
            TokenKind.CameraKeyword => SyntaxKind.CameraKeyword,
            TokenKind.ViewportKeyword => SyntaxKind.ViewportKeyword,
            TokenKind.CaseKeyword => SyntaxKind.CaseKeyword,
            TokenKind.CatchKeyword => SyntaxKind.CatchKeyword,
            TokenKind.CircleKeyword => SyntaxKind.CircleKeyword,
            TokenKind.ClsKeyword => SyntaxKind.ClsKeyword,
            TokenKind.CloseKeyword => SyntaxKind.CloseKeyword,
            TokenKind.ColorKeyword => SyntaxKind.ColorKeyword,
            TokenKind.ConstKeyword => SyntaxKind.ConstKeyword,
            TokenKind.ContinueKeyword => SyntaxKind.ContinueKeyword,
            TokenKind.DateKeyword => SyntaxKind.DateKeyword,
            TokenKind.DateTimeKeyword => SyntaxKind.DateTimeKeyword,
            TokenKind.DecimalKeyword => SyntaxKind.DecimalKeyword,
            TokenKind.DimKeyword => SyntaxKind.DimKeyword,
            TokenKind.DoKeyword => SyntaxKind.DoKeyword,
            TokenKind.DoubleKeyword => SyntaxKind.DoubleKeyword,
            TokenKind.DrawKeyword => SyntaxKind.DrawKeyword,
            TokenKind.DrawCircleKeyword => SyntaxKind.DrawCircleKeyword,
            TokenKind.DrawImageKeyword => SyntaxKind.DrawImageKeyword,
            TokenKind.DrawLineKeyword => SyntaxKind.DrawLineKeyword,
            TokenKind.DrawRectKeyword => SyntaxKind.DrawRectKeyword,
            TokenKind.DrawSpriteKeyword => SyntaxKind.DrawSpriteKeyword,
            TokenKind.DrawTileKeyword => SyntaxKind.DrawTileKeyword,
            TokenKind.DrawTileLayerKeyword => SyntaxKind.DrawTileLayerKeyword,
            TokenKind.DrawTilemapKeyword => SyntaxKind.DrawTilemapKeyword,
            TokenKind.DrawTextKeyword => SyntaxKind.DrawTextKeyword,
            TokenKind.ElseKeyword => SyntaxKind.ElseKeyword,
            TokenKind.ElseIfKeyword => SyntaxKind.ElseIfKeyword,
            TokenKind.EndKeyword => SyntaxKind.EndKeyword,
            TokenKind.ErrorKeyword => SyntaxKind.ErrorKeyword,
            TokenKind.EraseKeyword => SyntaxKind.EraseKeyword,
            TokenKind.EqvKeyword => SyntaxKind.EqvKeyword,
            TokenKind.ExitKeyword => SyntaxKind.ExitKeyword,
            TokenKind.FileKeyword => SyntaxKind.FileKeyword,
            TokenKind.FillCircleKeyword => SyntaxKind.FillCircleKeyword,
            TokenKind.FillRectKeyword => SyntaxKind.FillRectKeyword,
            TokenKind.FinallyKeyword => SyntaxKind.FinallyKeyword,
            TokenKind.ForKeyword => SyntaxKind.ForKeyword,
            TokenKind.FunctionKeyword => SyntaxKind.FunctionKeyword,
            TokenKind.GetKeyword => SyntaxKind.GetKeyword,
            TokenKind.IfKeyword => SyntaxKind.IfKeyword,
            TokenKind.ImageKeyword => SyntaxKind.ImageKeyword,
            TokenKind.ImpKeyword => SyntaxKind.ImpKeyword,
            TokenKind.InputKeyword => SyntaxKind.InputKeyword,
            TokenKind.IntegerKeyword => SyntaxKind.IntegerKeyword,
            TokenKind.IsKeyword => SyntaxKind.IsKeyword,
            TokenKind.KeyKeyword => SyntaxKind.KeyKeyword,
            TokenKind.LineKeyword => SyntaxKind.LineKeyword,
            TokenKind.LocateKeyword => SyntaxKind.LocateKeyword,
            TokenKind.LongKeyword => SyntaxKind.LongKeyword,
            TokenKind.ModKeyword => SyntaxKind.ModKeyword,
            TokenKind.MouseKeyword => SyntaxKind.MouseKeyword,
            TokenKind.NextKeyword => SyntaxKind.NextKeyword,
            TokenKind.NotKeyword => SyntaxKind.NotKeyword,
            TokenKind.OffKeyword => SyntaxKind.OffKeyword,
            TokenKind.OnKeyword => SyntaxKind.OnKeyword,
            TokenKind.LoopKeyword => SyntaxKind.LoopKeyword,
            TokenKind.OpenKeyword => SyntaxKind.OpenKeyword,
            TokenKind.OrKeyword => SyntaxKind.OrKeyword,
            TokenKind.OutputKeyword => SyntaxKind.OutputKeyword,
            TokenKind.PaletteKeyword => SyntaxKind.PaletteKeyword,
            TokenKind.PaintKeyword => SyntaxKind.PaintKeyword,
            TokenKind.PCopyKeyword => SyntaxKind.PCopyKeyword,
            TokenKind.PresentKeyword => SyntaxKind.PresentKeyword,
            TokenKind.PreserveKeyword => SyntaxKind.PreserveKeyword,
            TokenKind.PresetKeyword => SyntaxKind.PresetKeyword,
            TokenKind.PrintKeyword => SyntaxKind.PrintKeyword,
            TokenKind.PrivateKeyword => SyntaxKind.PrivateKeyword,
            TokenKind.PublicKeyword => SyntaxKind.PublicKeyword,
            TokenKind.ImportKeyword => SyntaxKind.ImportKeyword,
            TokenKind.PutKeyword => SyntaxKind.PutKeyword,
            TokenKind.PSetKeyword => SyntaxKind.PSetKeyword,
            TokenKind.RedimKeyword => SyntaxKind.RedimKeyword,
            TokenKind.ResetCameraKeyword => SyntaxKind.ResetCameraKeyword,
            TokenKind.ReturnKeyword => SyntaxKind.ReturnKeyword,
            TokenKind.SelfKeyword => SyntaxKind.SelfKeyword,
            TokenKind.SelectKeyword => SyntaxKind.SelectKeyword,
            TokenKind.SetMapTileKeyword => SyntaxKind.SetMapTileKeyword,
            TokenKind.ScreenKeyword => SyntaxKind.ScreenKeyword,
            TokenKind.SmoothKeyword => SyntaxKind.SmoothKeyword,
            TokenKind.SleepKeyword => SyntaxKind.SleepKeyword,
            TokenKind.RandomizeKeyword => SyntaxKind.RandomizeKeyword,
            TokenKind.AndAlsoKeyword => SyntaxKind.AndAlsoKeyword,
            TokenKind.OrElseKeyword => SyntaxKind.OrElseKeyword,
            TokenKind.StepKeyword => SyntaxKind.StepKeyword,
            TokenKind.StopKeyword => SyntaxKind.StopKeyword,
            TokenKind.SpriteKeyword => SyntaxKind.SpriteKeyword,
            TokenKind.StringKeyword => SyntaxKind.StringKeyword,
            TokenKind.SubKeyword => SyntaxKind.SubKeyword,
            TokenKind.TilemapKeyword => SyntaxKind.TilemapKeyword,
            TokenKind.TilesetKeyword => SyntaxKind.TilesetKeyword,
            TokenKind.ThenKeyword => SyntaxKind.ThenKeyword,
            TokenKind.TimeKeyword => SyntaxKind.TimeKeyword,
            TokenKind.ToKeyword => SyntaxKind.ToKeyword,
            TokenKind.TransparentKeyword => SyntaxKind.TransparentKeyword,
            TokenKind.TryKeyword => SyntaxKind.TryKeyword,
            TokenKind.TypeKeyword => SyntaxKind.TypeKeyword,
            TokenKind.UsingKeyword => SyntaxKind.UsingKeyword,
            TokenKind.UtcDateTimeKeyword => SyntaxKind.UtcDateTimeKeyword,
            TokenKind.UntilKeyword => SyntaxKind.UntilKeyword,
            TokenKind.ViewKeyword => SyntaxKind.ViewKeyword,
            TokenKind.WindowKeyword => SyntaxKind.WindowKeyword,
            TokenKind.WhileKeyword => SyntaxKind.WhileKeyword,
            TokenKind.XorKeyword => SyntaxKind.XorKeyword,
            TokenKind.InterpolatedStringStartToken => SyntaxKind.InterpolatedStringStartToken,
            TokenKind.InterpolatedStringTextToken => SyntaxKind.InterpolatedStringTextToken,
            TokenKind.InterpolatedStringExpressionStartToken => SyntaxKind.InterpolatedStringExpressionStartToken,
            TokenKind.InterpolatedStringExpressionEndToken => SyntaxKind.InterpolatedStringExpressionEndToken,
            TokenKind.InterpolatedStringEndToken => SyntaxKind.InterpolatedStringEndToken,
            TokenKind.InterpolatedStringFormatSpecifierToken => SyntaxKind.InterpolatedStringFormatSpecifierToken,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported token kind.")
        };
    }
}
