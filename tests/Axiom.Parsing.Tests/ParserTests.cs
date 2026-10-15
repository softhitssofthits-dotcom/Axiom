using Axiom.Core;
using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Parsing.Tests;

public sealed class ParserTests
{
    [Theory]
    [InlineData("IF TRUE THEN PRINT 1", false)]
    [InlineData("IF TRUE THEN PRINT 1 ELSE PRINT 2", true)]
    [InlineData("IF TRUE THEN PRINT ELSE PRINT", true)]
    [InlineData("IF TRUE THEN PRINT 1; ELSE PRINT 2; ' конец", true)]
    public void Parse_SingleLineIf(string text, bool hasElse)
    {
        var program = ParseProgram(text, out var diagnostics);
        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<IfStatementSyntax>(program);
        Assert.True(statement.IsSingleLine);
        Assert.Single(statement.Statements);
        Assert.Equal(hasElse, statement.ElseClause is not null);
    }

    [Theory]
    [InlineData("IF TRUE THEN PRINT 1 ELSE")]
    [InlineData("IF TRUE THEN ELSE PRINT 2")]
    [InlineData("IF TRUE THEN PRINT 1 PRINT 2")]
    [InlineData("IF TRUE THEN PRINT 1 : PRINT 2")]
    [InlineData("IF TRUE THEN PRINT 1 ELSE PRINT 2 ELSE PRINT 3")]
    [InlineData("IF TRUE THEN PRINT 1 ELSEIF FALSE THEN PRINT 2")]
    [InlineData("IF TRUE THEN IF FALSE THEN PRINT 1 ELSE PRINT 2")]
    [InlineData("IF TRUE THEN FOR i = 1 TO 2")]
    [InlineData("IF TRUE THEN PRINT 1 END IF")]
    public void Parse_SingleLineIfRejectsUnsupportedForms(string text)
    {
        ParseProgram(text, out var diagnostics);
        Assert.NotEmpty(diagnostics);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Parse_SingleLineIfStopsAtNewlineInsideBlock(string newline)
    {
        var text = string.Join(newline, "IF TRUE THEN ' блочная форма", "IF FALSE THEN PRINT 1 ELSE PRINT 2", "PRINT 3", "END IF");
        var program = ParseProgram(text, out var diagnostics);
        Assert.Empty(diagnostics);
        var outer = AssertGlobalStatement<IfStatementSyntax>(program);
        Assert.Equal(2, outer.Statements.Count);
        Assert.False(outer.IsSingleLine);
    }

    [Fact]
    public void Parse_SingleLineIfErrorDoesNotConsumeFollowingLine()
    {
        var program = ParseProgram("IF TRUE THEN PRINT 1 PRINT 2\nPRINT 3\n", out var diagnostics);
        Assert.NotEmpty(diagnostics);
        Assert.Equal(2, program.Members.Count);
    }

    [Fact]
    public void Parse_LiteralExpression()
    {
        var program = ParseProgram("PRINT 123", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = AssertSinglePrintExpression(AssertPrintStatement(program));
        var literal = Assert.IsType<LiteralExpressionSyntax>(expression);
        Assert.Equal(123, Assert.IsType<int>(literal.LiteralToken.Value));
    }

    [Fact]
    public void Parse_LongLiteralExpression()
    {
        var program = ParseProgram("PRINT 21474836479", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = AssertSinglePrintExpression(AssertPrintStatement(program));
        var literal = Assert.IsType<LiteralExpressionSyntax>(expression);
        Assert.Equal(SyntaxKind.LongLiteralToken, literal.LiteralToken.Kind);
        Assert.Equal(21474836479L, Assert.IsType<long>(literal.LiteralToken.Value));
    }

    [Fact]
    public void Parse_HexLiteralExpression()
    {
        var program = ParseProgram("PRINT &HFF", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = AssertSinglePrintExpression(AssertPrintStatement(program));
        var literal = Assert.IsType<LiteralExpressionSyntax>(expression);
        Assert.Equal(SyntaxKind.IntegerLiteralToken, literal.LiteralToken.Kind);
        Assert.Equal(255, Assert.IsType<int>(literal.LiteralToken.Value));
    }

    [Fact]
    public void Parse_BinaryPrecedence()
    {
        var program = ParseProgram("PRINT 1 + 2 * 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.PlusToken, expression.OperatorToken.Kind);
        Assert.IsType<LiteralExpressionSyntax>(expression.Left);

        var right = Assert.IsType<BinaryExpressionSyntax>(expression.Right);
        Assert.Equal(SyntaxKind.StarToken, right.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_IntegerDivisionAndRemainderUseMultiplicativePrecedence()
    {
        var program = ParseProgram("PRINT 1 + 8 \\ 3 MOD 2", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.PlusToken, expression.OperatorToken.Kind);

        var right = Assert.IsType<BinaryExpressionSyntax>(expression.Right);
        Assert.Equal(SyntaxKind.ModKeyword, right.OperatorToken.Kind);

        var left = Assert.IsType<BinaryExpressionSyntax>(right.Left);
        Assert.Equal(SyntaxKind.BackslashToken, left.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_AmpersandUsesAdditivePrecedence()
    {
        var program = ParseProgram("PRINT \"A\" & 1 + 2", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.PlusToken, expression.OperatorToken.Kind);

        var left = Assert.IsType<BinaryExpressionSyntax>(expression.Left);
        Assert.Equal(SyntaxKind.AmpersandToken, left.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_UnaryOperatorsBindBetweenExponentiationAndMultiplicativeOperators()
    {
        var program = ParseProgram("PRINT 2 * -3 ^ 2", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.StarToken, expression.OperatorToken.Kind);

        var right = Assert.IsType<UnaryExpressionSyntax>(expression.Right);
        Assert.Equal(SyntaxKind.MinusToken, right.OperatorToken.Kind);

        var operand = Assert.IsType<BinaryExpressionSyntax>(right.Operand);
        Assert.Equal(SyntaxKind.CaretToken, operand.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_ExponentiationIsRightAssociative()
    {
        var program = ParseProgram("PRINT 2 ^ 3 ^ 2", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.CaretToken, expression.OperatorToken.Kind);
        Assert.IsType<LiteralExpressionSyntax>(expression.Left);

        var right = Assert.IsType<BinaryExpressionSyntax>(expression.Right);
        Assert.Equal(SyntaxKind.CaretToken, right.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_ShiftsUseLowerPrecedenceThanAddition()
    {
        var program = ParseProgram("PRINT 1 + 2 << 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.LessLessToken, expression.OperatorToken.Kind);
        var left = Assert.IsType<BinaryExpressionSyntax>(expression.Left);
        Assert.Equal(SyntaxKind.PlusToken, left.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_EqvAndImpUseLowestLogicalPrecedence()
    {
        var program = ParseProgram("PRINT TRUE OR FALSE EQV TRUE IMP FALSE", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<BinaryExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.Equal(SyntaxKind.ImpKeyword, expression.OperatorToken.Kind);

        var left = Assert.IsType<BinaryExpressionSyntax>(expression.Left);
        Assert.Equal(SyntaxKind.EqvKeyword, left.OperatorToken.Kind);

        var nestedLeft = Assert.IsType<BinaryExpressionSyntax>(left.Left);
        Assert.Equal(SyntaxKind.OrKeyword, nestedLeft.OperatorToken.Kind);
    }

    [Fact]
    public void Parse_ParenthesizedExpression()
    {
        var program = ParseProgram("PRINT (1 + 2)", out var diagnostics);

        Assert.Empty(diagnostics);
        var expression = Assert.IsType<ParenthesizedExpressionSyntax>(AssertSinglePrintExpression(AssertPrintStatement(program)));
        Assert.IsType<BinaryExpressionSyntax>(expression.Expression);
    }

    [Fact]
    public void Parse_DimStatement()
    {
        var program = ParseProgram("DIM value AS INTEGER = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var declarator = Assert.Single(declaration.Declarators);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);
        Assert.Equal("value", declarator.Identifier.Text);
        AssertNamedType(typeClause.Type, SyntaxKind.IntegerKeyword);
        Assert.NotNull(declaration.Initializer);
    }

    [Fact]
    public void Parse_DimStatementWithLongType()
    {
        var program = ParseProgram("DIM value AS LONG = 21474836479", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);
        AssertNamedType(typeClause.Type, SyntaxKind.LongKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithDecimalType()
    {
        var program = ParseProgram("DIM price AS DECIMAL = 10.50", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);
        AssertNamedType(typeClause.Type, SyntaxKind.DecimalKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithInferredType()
    {
        var program = ParseProgram("DIM value = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var declarator = Assert.Single(declaration.Declarators);
        Assert.Equal("value", declarator.Identifier.Text);
        Assert.Null(declaration.TypeClause);
        Assert.NotNull(declaration.Initializer);
    }

    [Fact]
    public void Parse_DimStatementWithGroupedDeclarations()
    {
        var program = ParseProgram("DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        Assert.Equal(2, dim.Declarations.Count);

        var booleans = dim.Declarations[0];
        var booleansTypeClause = Assert.IsType<TypeClauseSyntax>(booleans.TypeClause);
        Assert.Equal(["anotherVar", "choice"], booleans.Declarators.Select(static declarator => declarator.Identifier.Text));
        AssertNamedType(booleansTypeClause.Type, SyntaxKind.BooleanKeyword);

        var stringGroup = dim.Declarations[1];
        var stringTypeClause = Assert.IsType<TypeClauseSyntax>(stringGroup.TypeClause);
        Assert.Equal(["birthDate"], stringGroup.Declarators.Select(static declarator => declarator.Identifier.Text));
        AssertNamedType(stringTypeClause.Type, SyntaxKind.StringKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithFunctionType()
    {
        var program = ParseProgram("DIM adder AS FUNCTION(INTEGER, INTEGER) AS INTEGER", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);
        var functionType = Assert.IsType<FunctionTypeSyntax>(typeClause.Type);

        Assert.Equal(2, functionType.ParameterTypes.Count);
        AssertNamedType(functionType.ParameterTypes[0], SyntaxKind.IntegerKeyword);
        AssertNamedType(functionType.ParameterTypes[1], SyntaxKind.IntegerKeyword);
        AssertNamedType(functionType.ReturnType, SyntaxKind.IntegerKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithImageType()
    {
        var program = ParseProgram("DIM hero AS IMAGE", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);

        AssertNamedType(typeClause.Type, SyntaxKind.ImageKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithTilesetType()
    {
        var program = ParseProgram("DIM tiles AS TILESET", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);

        AssertNamedType(typeClause.Type, SyntaxKind.TilesetKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithTilemapType()
    {
        var program = ParseProgram("DIM level AS TILEMAP", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);

        AssertNamedType(typeClause.Type, SyntaxKind.TilemapKeyword);
    }

    [Fact]
    public void Parse_OnKeyStatement()
    {
        var program = ParseProgram("ON KEY(\"LEFT\"), HandleLeft", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<OnKeyStatementSyntax>(program);
        Assert.Equal(SyntaxKind.OnKeyword, statement.OnKeyword.Kind);
        Assert.Equal(SyntaxKind.KeyKeyword, statement.KeyKeyword.Kind);
        Assert.Equal(SyntaxKind.IdentifierToken, statement.ActionToken.Kind);
        Assert.Equal("HandleLeft", statement.ActionToken.Text);
        var keyLiteral = Assert.IsType<LiteralExpressionSyntax>(statement.KeyExpression);
        Assert.Equal("LEFT", Assert.IsType<string>(keyLiteral.LiteralToken.Value));
    }

    [Fact]
    public void Parse_OnKeyOffStatement()
    {
        var program = ParseProgram("ON KEY(\"LEFT\"), OFF", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<OnKeyStatementSyntax>(program);
        Assert.True(statement.DisablesHandler);
        Assert.Equal(SyntaxKind.OffKeyword, statement.ActionToken.Kind);
    }

    [Theory]
    [InlineData("ON KEY(\"LEFT\"), ON", SyntaxKind.OnKeyword)]
    [InlineData("ON KEY(\"LEFT\"), STOP", SyntaxKind.StopKeyword)]
    public void Parse_OnKeyModeStatement(string text, SyntaxKind expectedActionKind)
    {
        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<OnKeyStatementSyntax>(program);
        Assert.Equal(expectedActionKind, statement.ActionToken.Kind);
    }

    [Fact]
    public void Parse_OnMouseStatement()
    {
        var program = ParseProgram("ON MOUSE(1), HandleLeft", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<OnMouseStatementSyntax>(program);
        Assert.Equal(SyntaxKind.OnKeyword, statement.OnKeyword.Kind);
        Assert.Equal(SyntaxKind.MouseKeyword, statement.MouseKeyword.Kind);
        Assert.Equal(SyntaxKind.IdentifierToken, statement.ActionToken.Kind);
        Assert.Equal("HandleLeft", statement.ActionToken.Text);
        var buttonLiteral = Assert.IsType<LiteralExpressionSyntax>(statement.ButtonExpression);
        Assert.Equal(1, Assert.IsType<int>(buttonLiteral.LiteralToken.Value));
    }

    [Theory]
    [InlineData("ON MOUSE(1), ON", SyntaxKind.OnKeyword)]
    [InlineData("ON MOUSE(1), STOP", SyntaxKind.StopKeyword)]
    [InlineData("ON MOUSE(1), OFF", SyntaxKind.OffKeyword)]
    [InlineData("ON MOUSE(1, \"UP\"), OFF", SyntaxKind.OffKeyword)]
    [InlineData("ON MOUSE(\"MOVE\"), STOP", SyntaxKind.StopKeyword)]
    [InlineData("ON MOUSE(\"WHEEL\"), ON", SyntaxKind.OnKeyword)]
    public void Parse_OnMouseModeStatement(string text, SyntaxKind expectedActionKind)
    {
        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<OnMouseStatementSyntax>(program);
        Assert.Equal(expectedActionKind, statement.ActionToken.Kind);
    }

    [Fact]
    public void Parse_ScreenStatement()
    {
        var program = ParseProgram("SCREEN 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<ScreenStatementSyntax>(program);
        var mode = Assert.IsType<LiteralExpressionSyntax>(statement.ModeExpression);
        Assert.Equal(10, Assert.IsType<int>(mode.LiteralToken.Value));
    }

    [Theory]
    [InlineData("SMOOTH ON", true)]
    [InlineData("SMOOTH OFF", false)]
    public void Parse_SmoothStatement(string source, bool expectedEnabled)
    {
        var program = ParseProgram(source, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<SmoothStatementSyntax>(program);
        Assert.Equal(expectedEnabled, statement.Enabled);
        Assert.Equal(expectedEnabled ? SyntaxKind.OnKeyword : SyntaxKind.OffKeyword, statement.ModeKeyword.Kind);
    }

    [Theory]
    [InlineData("SMOOTH", "MB1001")]
    [InlineData("SMOOTH MAYBE", "MB1001")]
    [InlineData("SMOOTH ON 1", "MB1004")]
    public void Parse_ReportsInvalidSmoothForms(string source, string expectedCode)
    {
        ParseProgram(source, out var diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == expectedCode);
    }

    [Fact]
    public void Parse_ScreenStatementSupportsPages()
    {
        var program = ParseProgram("SCREEN 1, , 2, 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<ScreenStatementSyntax>(program);
        Assert.NotNull(statement.ColorSwitchCommaToken);
        Assert.Null(statement.ColorSwitchExpression);
        Assert.Equal(2, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(statement.ActivePageExpression).LiteralToken.Value));
        Assert.Equal(3, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(statement.VisualPageExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_PCopyStatement()
    {
        var program = ParseProgram("PCOPY 1, 0", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<PCopyStatementSyntax>(program);
        Assert.Equal(1, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(statement.SourcePageExpression).LiteralToken.Value));
        Assert.Equal(0, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(statement.DestinationPageExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_SleepStatementWithDuration()
    {
        var program = ParseProgram("SLEEP 500", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<SleepStatementSyntax>(program);
        var duration = Assert.IsType<LiteralExpressionSyntax>(statement.DurationExpression);
        Assert.Equal(500, Assert.IsType<int>(duration.LiteralToken.Value));
    }

    [Fact]
    public void Parse_SleepStatementWithoutDuration()
    {
        var program = ParseProgram("SLEEP", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<SleepStatementSyntax>(program);
        Assert.Null(statement.DurationExpression);
    }

    [Fact]
    public void Parse_ColorLocateAndClsStatements()
    {
        var program = ParseProgram("""
            COLOR 14, 1
            LOCATE 3, 5
            CLS
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        var color = Assert.IsType<ColorStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
        var locate = Assert.IsType<LocateStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);

        Assert.NotNull(color.BackgroundExpression);
        Assert.IsType<ClsStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        Assert.Equal(3, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(locate.RowExpression).LiteralToken.Value));
        Assert.Equal(5, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(locate.ColumnExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_CanvasFrameAndModernRectangles()
    {
        var program = ParseProgram("""
            CANVAS 320, 180
            BEGINFRAME
            CLS RGB(18, 22, 30)
            DRAWRECT 10, 20, 30, 40, RGB(255, 0, 0)
            FILLRECT 50, 60, 70, 80, RGB(0, 255, 0)
            PRESENT
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<CanvasStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
        Assert.IsType<BeginFrameStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var cls = Assert.IsType<ClsStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        Assert.IsType<CallExpressionSyntax>(cls.ColorExpression);
        var drawRect = Assert.IsType<DrawRectStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        Assert.IsType<CallExpressionSyntax>(drawRect.ColorExpression);
        var fillRect = Assert.IsType<FillRectStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[4]).Statement);
        Assert.IsType<CallExpressionSyntax>(fillRect.ColorExpression);
        Assert.IsType<PresentStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[5]).Statement);
    }

    [Fact]
    public void Parse_CanvasCameraAndModernPrimitives()
    {
        var program = ParseProgram("""
            CANVAS 320, 180
            CAMERA 16, 24
            DRAWLINE 16, 24, 80, 24, RGB(255, 255, 0)
            DRAWCIRCLE 48, 52, 10, RGB(255, 0, 0)
            FILLCIRCLE 96, 52, 6, RGB(0, 255, 0)
            RESETCAMERA
            DRAWTEXT "Очки: 12", 8, 8, RGB(255, 255, 255)
            PRESENT
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<CanvasStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);

        var camera = Assert.IsType<CameraStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        Assert.Equal(16, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(camera.XExpression).LiteralToken.Value));
        Assert.Equal(24, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(camera.YExpression).LiteralToken.Value));

        var drawLine = Assert.IsType<DrawLineStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        Assert.IsType<CallExpressionSyntax>(drawLine.ColorExpression);

        var drawCircle = Assert.IsType<DrawCircleStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        Assert.IsType<CallExpressionSyntax>(drawCircle.ColorExpression);

        var fillCircle = Assert.IsType<FillCircleStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[4]).Statement);
        Assert.IsType<CallExpressionSyntax>(fillCircle.ColorExpression);

        Assert.IsType<ResetCameraStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[5]).Statement);

        var drawText = Assert.IsType<DrawTextStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[6]).Statement);
        Assert.Equal("Очки: 12", Assert.IsType<string>(Assert.IsType<LiteralExpressionSyntax>(drawText.TextExpression).LiteralToken.Value));
        Assert.IsType<CallExpressionSyntax>(drawText.ColorExpression);

        Assert.IsType<PresentStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[7]).Statement);
    }

    [Fact]
    public void Parse_ViewportResetStatement()
    {
        var program = ParseProgram("""
            CANVAS 320, 180
            VIEWPORT
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        var viewport = Assert.IsType<ViewportStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        Assert.Null(viewport.TopLeftPoint);
        Assert.Null(viewport.BottomRightPoint);
    }

    [Fact]
    public void Parse_ViewportRectangleStatement()
    {
        var program = ParseProgram("""
            CANVAS 320, 180
            VIEWPORT (1, 2)-(10, 20)
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        var viewport = Assert.IsType<ViewportStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        Assert.NotNull(viewport.TopLeftPoint);
        Assert.NotNull(viewport.BottomRightPoint);
        Assert.Equal(1, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(viewport.TopLeftPoint!.XExpression).LiteralToken.Value));
        Assert.Equal(2, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(viewport.TopLeftPoint.YExpression).LiteralToken.Value));
        Assert.Equal(10, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(viewport.BottomRightPoint!.XExpression).LiteralToken.Value));
        Assert.Equal(20, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(viewport.BottomRightPoint.YExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_ColorTransparentStatement()
    {
        var program = ParseProgram("COLOR 15, TRANSPARENT", out var diagnostics);

        Assert.Empty(diagnostics);
        var color = AssertGlobalStatement<ColorStatementSyntax>(program);
        Assert.Null(color.BackgroundExpression);
        Assert.NotNull(color.TransparentKeyword);
        Assert.Equal(SyntaxKind.TransparentKeyword, color.TransparentKeyword!.Kind);
    }

    [Fact]
    public void Parse_PaletteResetStatement()
    {
        var program = ParseProgram("PALETTE", out var diagnostics);

        Assert.Empty(diagnostics);
        var palette = AssertGlobalStatement<PaletteStatementSyntax>(program);
        Assert.True(palette.IsReset);
        Assert.Null(palette.IndexExpression);
        Assert.Null(palette.ColorExpression);
    }

    [Fact]
    public void Parse_PaletteColorStatement()
    {
        var program = ParseProgram("PALETTE 1, 196", out var diagnostics);

        Assert.Empty(diagnostics);
        var palette = AssertGlobalStatement<PaletteStatementSyntax>(program);
        Assert.False(palette.IsReset);
        Assert.Equal(1, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(palette.IndexExpression).LiteralToken.Value));
        Assert.Equal(SyntaxKind.CommaToken, palette.CommaToken?.Kind);
        Assert.Equal(196, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(palette.ColorExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_PaletteUsingStatement()
    {
        var program = ParseProgram("PALETTE USING colors", out var diagnostics);

        Assert.Empty(diagnostics);
        var palette = AssertGlobalStatement<PaletteStatementSyntax>(program);
        Assert.False(palette.IsReset);
        Assert.True(palette.IsUsing);
        Assert.Equal(SyntaxKind.UsingKeyword, palette.UsingKeyword?.Kind);
        Assert.Equal("colors", Assert.IsType<NameExpressionSyntax>(palette.ValuesExpression).IdentifierToken.Text);
    }

    [Fact]
    public void Parse_GraphicsPrimitiveStatements()
    {
        var program = ParseProgram("""
            PSET (1, 2), 12
            PRESET (3, 4), 0
            LINE (0, 0)-(10, 10), 14, BF
            CIRCLE STEP (20, 20), 5, , -PI / 2, PI, 0.5
            PAINT (8, 9), 6, 1
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        var pset = Assert.IsType<PSetStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
        var preset = Assert.IsType<PSetStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var line = Assert.IsType<LineStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        var circle = Assert.IsType<CircleStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        var paint = Assert.IsType<PaintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[4]).Statement);

        Assert.False(pset.Reset);
        Assert.True(preset.Reset);
        Assert.Equal(LineBoxKind.Filled, line.BoxKind);
        Assert.True(circle.IsRelative);
        Assert.Null(circle.ColorExpression);
        Assert.NotNull(circle.StartExpression);
        Assert.NotNull(circle.EndExpression);
        Assert.NotNull(circle.AspectExpression);
        Assert.NotNull(paint.FillColorExpression);
        Assert.NotNull(paint.BorderColorExpression);
    }

    [Fact]
    public void Parse_GetAndPutStatements()
    {
        var program = ParseProgram("""
            GET STEP (1, 2)-STEP (3, 4), buffer
            PUT STEP (5, 6), buffer, PRESET
            PUT (7, 8), buffer
            """, out var diagnostics);

        Assert.Empty(diagnostics);
        var get = Assert.IsType<GetStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
        var putPreset = Assert.IsType<PutStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var putDefault = Assert.IsType<PutStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);

        Assert.True(get.IsStartRelative);
        Assert.True(get.IsEndRelative);
        Assert.IsType<NameExpressionSyntax>(get.BufferExpression);
        Assert.True(putPreset.IsRelative);
        Assert.NotNull(putPreset.ActionToken);
        Assert.Equal(SyntaxKind.PresetKeyword, putPreset.ActionToken!.Kind);
        Assert.False(putDefault.IsRelative);
        Assert.Null(putDefault.ActionToken);
    }

    [Fact]
    public void Parse_ViewResetStatement()
    {
        var program = ParseProgram("VIEW", out var diagnostics);

        Assert.Empty(diagnostics);
        var view = AssertGlobalStatement<ViewStatementSyntax>(program);
        Assert.True(view.IsReset);
        Assert.Null(view.ScreenKeyword);
    }

    [Fact]
    public void Parse_ViewStatementSupportsScreenAndOptionalBorder()
    {
        var program = ParseProgram("VIEW SCREEN (1, 2)-(3, 4), , 7", out var diagnostics);

        Assert.Empty(diagnostics);
        var view = AssertGlobalStatement<ViewStatementSyntax>(program);
        Assert.False(view.IsReset);
        Assert.NotNull(view.ScreenKeyword);
        Assert.NotNull(view.TopLeftPoint);
        Assert.NotNull(view.BottomRightPoint);
        Assert.Null(view.FillColorExpression);
        Assert.NotNull(view.BorderColorExpression);
    }

    [Fact]
    public void Parse_ViewPrintResetStatement()
    {
        var program = ParseProgram("VIEW PRINT", out var diagnostics);

        Assert.Empty(diagnostics);
        var viewPrint = AssertGlobalStatement<ViewPrintStatementSyntax>(program);
        Assert.True(viewPrint.IsReset);
        Assert.Null(viewPrint.TopExpression);
        Assert.Null(viewPrint.BottomExpression);
    }

    [Fact]
    public void Parse_ViewPrintStatementSupportsTopAndBottomRows()
    {
        var program = ParseProgram("VIEW PRINT 10 TO 20", out var diagnostics);

        Assert.Empty(diagnostics);
        var viewPrint = AssertGlobalStatement<ViewPrintStatementSyntax>(program);
        Assert.False(viewPrint.IsReset);
        Assert.NotNull(viewPrint.TopExpression);
        Assert.NotNull(viewPrint.ToKeyword);
        Assert.NotNull(viewPrint.BottomExpression);
    }

    [Fact]
    public void Parse_WindowResetStatement()
    {
        var program = ParseProgram("WINDOW", out var diagnostics);

        Assert.Empty(diagnostics);
        var window = AssertGlobalStatement<WindowStatementSyntax>(program);
        Assert.True(window.IsReset);
        Assert.Null(window.ScreenKeyword);
    }

    [Fact]
    public void Parse_WindowStatementSupportsScreenCoordinates()
    {
        var program = ParseProgram("WINDOW SCREEN (1, 2)-(3, 4)", out var diagnostics);

        Assert.Empty(diagnostics);
        var window = AssertGlobalStatement<WindowStatementSyntax>(program);
        Assert.False(window.IsReset);
        Assert.NotNull(window.ScreenKeyword);
        Assert.NotNull(window.TopLeftPoint);
        Assert.NotNull(window.BottomRightPoint);
    }

    [Fact]
    public void Parse_PaintStatementSupportsStepEmptySlotsAndBackground()
    {
        var program = ParseProgram("PAINT STEP (8, 9), pattern, , backPattern", out var diagnostics);

        Assert.Empty(diagnostics);
        var paint = AssertGlobalStatement<PaintStatementSyntax>(program);
        Assert.True(paint.IsRelative);
        Assert.NotNull(paint.FillColorExpression);
        Assert.Null(paint.BorderColorExpression);
        Assert.NotNull(paint.BackgroundExpression);
    }

    [Fact]
    public void Parse_DrawStringLiteralStatement()
    {
        var program = ParseProgram("DRAW \"R10\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var draw = AssertGlobalStatement<DrawStatementSyntax>(program);
        var literal = Assert.IsType<LiteralExpressionSyntax>(draw.CommandExpression);
        Assert.Equal("R10", Assert.IsType<string>(literal.LiteralToken.Value));
    }

    [Fact]
    public void Parse_DrawVariableStatement()
    {
        var program = ParseProgram("DRAW commands", out var diagnostics);

        Assert.Empty(diagnostics);
        var draw = AssertGlobalStatement<DrawStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(draw.CommandExpression);
    }

    [Fact]
    public void Parse_DrawConcatenatedExpressionStatement()
    {
        var program = ParseProgram("DRAW \"C\" + STR(penColor) + \"R20\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var draw = AssertGlobalStatement<DrawStatementSyntax>(program);
        Assert.IsType<BinaryExpressionSyntax>(draw.CommandExpression);
    }

    [Fact]
    public void Parse_DrawImageStatementNaturalSize()
    {
        var program = ParseProgram("DRAWIMAGE hero, 10, 20", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawImage = AssertGlobalStatement<DrawImageStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(drawImage.ImageExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawImage.XExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawImage.YExpression);
        Assert.False(drawImage.HasScale);
        Assert.Null(drawImage.WidthExpression);
        Assert.Null(drawImage.HeightExpression);
    }

    [Fact]
    public void Parse_DrawImageStatementWithTargetSize()
    {
        var program = ParseProgram("DRAWIMAGE hero, 10, 20, 32, 16", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawImage = AssertGlobalStatement<DrawImageStatementSyntax>(program);
        Assert.True(drawImage.HasScale);
        Assert.NotNull(drawImage.WidthExpression);
        Assert.NotNull(drawImage.HeightExpression);
    }

    [Fact]
    public void Parse_DrawSpriteStatementNaturalSize()
    {
        var program = ParseProgram("DRAWSPRITE hero, 1, 10, 20", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawSprite = AssertGlobalStatement<DrawSpriteStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(drawSprite.SpriteExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawSprite.FrameExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawSprite.XExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawSprite.YExpression);
        Assert.False(drawSprite.HasScale);
        Assert.Null(drawSprite.WidthExpression);
        Assert.Null(drawSprite.HeightExpression);
    }

    [Fact]
    public void Parse_DrawSpriteStatementWithTargetSize()
    {
        var program = ParseProgram("DRAWSPRITE hero, 1, 10, 20, 32, 16", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawSprite = AssertGlobalStatement<DrawSpriteStatementSyntax>(program);
        Assert.True(drawSprite.HasScale);
        Assert.NotNull(drawSprite.WidthExpression);
        Assert.NotNull(drawSprite.HeightExpression);
    }

    [Fact]
    public void Parse_DrawTileStatement()
    {
        var program = ParseProgram("DRAWTILE tiles, 1, 10, 20", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawTile = AssertGlobalStatement<DrawTileStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(drawTile.TilesetExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTile.TileIndexExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTile.XExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTile.YExpression);
    }

    [Fact]
    public void Parse_DrawTilemapStatement()
    {
        var program = ParseProgram("DRAWTILEMAP level, 32, 48", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawTilemap = AssertGlobalStatement<DrawTilemapStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(drawTilemap.TilemapExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTilemap.XExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTilemap.YExpression);
    }

    [Fact]
    public void Parse_DrawTileLayerStatement()
    {
        var program = ParseProgram("DRAWTILELAYER level, 2, 32, 48", out var diagnostics);

        Assert.Empty(diagnostics);
        var drawTileLayer = AssertGlobalStatement<DrawTileLayerStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(drawTileLayer.TilemapExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTileLayer.LayerIndexExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTileLayer.XExpression);
        Assert.IsType<LiteralExpressionSyntax>(drawTileLayer.YExpression);
    }

    [Fact]
    public void Parse_SetMapTileStatement()
    {
        var program = ParseProgram("SETMAPTILE level, 1, 2, 2, 1, 4", out var diagnostics);

        Assert.Empty(diagnostics);
        var setMapTile = AssertGlobalStatement<SetMapTileStatementSyntax>(program);
        Assert.IsType<NameExpressionSyntax>(setMapTile.TilemapExpression);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.LayerIndexExpression);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.ColumnExpression);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.RowExpression);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.TilesetIndexExpression);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.TileIndexExpression);
        Assert.Null(setMapTile.FlagsExpression);
    }

    [Fact]
    public void Parse_SetMapTileStatementWithFlags()
    {
        var program = ParseProgram("SETMAPTILE level, 1, 2, 2, 1, 4, 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var setMapTile = AssertGlobalStatement<SetMapTileStatementSyntax>(program);
        Assert.IsType<LiteralExpressionSyntax>(setMapTile.FlagsExpression);
    }

    [Fact]
    public void Parse_FunctionDeclarationWithFunctionTypedParameterAndReturnType()
    {
        const string text = """
FUNCTION Compose(first AS FUNCTION(INTEGER) AS INTEGER, second AS FUNCTION(INTEGER) AS INTEGER) AS FUNCTION(INTEGER) AS INTEGER
    RETURN first
END FUNCTION
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Equal(2, function.Parameters.Count);

        var firstParameterType = Assert.IsType<FunctionTypeSyntax>(function.Parameters[0].TypeClause.Type);
        Assert.Single(firstParameterType.ParameterTypes);
        AssertNamedType(firstParameterType.ParameterTypes[0], SyntaxKind.IntegerKeyword);
        AssertNamedType(firstParameterType.ReturnType, SyntaxKind.IntegerKeyword);

        var returnType = Assert.IsType<FunctionTypeSyntax>(function.ReturnType.Type);
        Assert.Single(returnType.ParameterTypes);
        AssertNamedType(returnType.ParameterTypes[0], SyntaxKind.IntegerKeyword);
        AssertNamedType(returnType.ReturnType, SyntaxKind.IntegerKeyword);
    }

    [Fact]
    public void Parse_SubDeclarationWithExplicitByRefAndByValParameters()
    {
        const string text = """
SUB Update(BYREF user AS INTEGER, BYVAL copy AS INTEGER)
END SUB
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var sub = Assert.IsType<SubDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Equal(SyntaxKind.ByRefKeyword, sub.Parameters[0].PassingKeyword?.Kind);
        Assert.Equal(SyntaxKind.ByValKeyword, sub.Parameters[1].PassingKeyword?.Kind);
    }

    [Fact]
    public void Parse_FunctionDeclarationWithArrayTypedParameterAndReturnType()
    {
        const string text = """
FUNCTION Clone(values AS INTEGER()) AS INTEGER()
    RETURN values
END FUNCTION
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        var parameterType = AssertArrayType(function.Parameters[0].TypeClause.Type);
        AssertNamedType(parameterType.ElementType, SyntaxKind.IntegerKeyword);

        var returnType = AssertArrayType(function.ReturnType.Type);
        AssertNamedType(returnType.ElementType, SyntaxKind.IntegerKeyword);
    }

    [Fact]
    public void Parse_CallThroughReturnedFunction()
    {
        var program = ParseProgram("PRINT MakeAdder()(5)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = AssertPrintStatement(program);
        var outerCall = Assert.IsType<CallExpressionSyntax>(AssertSinglePrintExpression(print));
        Assert.Single(outerCall.Arguments);

        var innerCall = Assert.IsType<CallExpressionSyntax>(outerCall.Target);
        var name = Assert.IsType<NameExpressionSyntax>(innerCall.Target);
        Assert.Equal("MakeAdder", name.IdentifierToken.Text);
    }

    [Fact]
    public void Parse_CallArgumentRangeExpression()
    {
        var program = ParseProgram("PRINT grid(2, 3 TO 4)", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = AssertPrintStatement(program);
        var call = Assert.IsType<CallExpressionSyntax>(AssertSinglePrintExpression(print));

        Assert.Equal("grid", Assert.IsType<NameExpressionSyntax>(call.Target).IdentifierToken.Text);
        Assert.Equal(2, call.Arguments.Count);
        Assert.IsType<LiteralExpressionSyntax>(call.Arguments[0]);

        var range = Assert.IsType<RangeExpressionSyntax>(call.Arguments[1]);
        Assert.Equal(SyntaxKind.ToKeyword, range.ToKeyword.Kind);
        Assert.Equal(3, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(range.LowerBound).LiteralToken.Value));
        Assert.Equal(4, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(range.UpperBound).LiteralToken.Value));
    }

    [Fact]
    public void Parse_DimStatementWithGroupedInferredDeclaration()
    {
        var program = ParseProgram("DIM x, y = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        Assert.Equal(["x", "y"], declaration.Declarators.Select(static declarator => declarator.Identifier.Text));
        Assert.Null(declaration.TypeClause);
        Assert.NotNull(declaration.Initializer);
    }

    [Fact]
    public void Parse_DimStatementWithDynamicArrayDeclarator()
    {
        var program = ParseProgram("DIM items() AS STRING", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var declarator = Assert.Single(declaration.Declarators);
        var bounds = Assert.IsType<ArrayBoundsSyntax>(declarator.ArrayBounds);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);

        Assert.True(bounds.IsDynamic);
        Assert.Null(bounds.StartExpression);
        Assert.Null(bounds.ToKeyword);
        Assert.Null(bounds.EndExpression);
        AssertNamedType(typeClause.Type, SyntaxKind.StringKeyword);
    }

    [Fact]
    public void Parse_DimStatementWithMultidimensionalArrayDeclarator()
    {
        var program = ParseProgram("DIM grid(1 TO 2, 10 TO 11) AS INTEGER", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var declarator = Assert.Single(declaration.Declarators);
        var bounds = Assert.IsType<ArrayBoundsSyntax>(declarator.ArrayBounds);

        Assert.Equal(2, bounds.Rank);
        Assert.Single(bounds.CommaTokens);
        Assert.False(bounds.IsDynamic);
        Assert.False(bounds.HasMixedDimensions);

        var firstDimension = bounds.Dimensions[0];
        Assert.Equal(SyntaxKind.ToKeyword, firstDimension.ToKeyword?.Kind);
        Assert.Equal(1, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(firstDimension.StartExpression).LiteralToken.Value));
        Assert.Equal(2, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(firstDimension.EndExpression).LiteralToken.Value));

        var secondDimension = bounds.Dimensions[1];
        Assert.Equal(SyntaxKind.ToKeyword, secondDimension.ToKeyword?.Kind);
        Assert.Equal(10, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(secondDimension.StartExpression).LiteralToken.Value));
        Assert.Equal(11, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(secondDimension.EndExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_DimStatementWithFixedLengthStringType()
    {
        var program = ParseProgram("DIM code AS STRING * 4", out var diagnostics);

        Assert.Empty(diagnostics);
        var dim = AssertGlobalStatement<DimStatementSyntax>(program);
        var declaration = Assert.Single(dim.Declarations);
        var typeClause = Assert.IsType<TypeClauseSyntax>(declaration.TypeClause);
        var fixedStringType = AssertFixedLengthStringType(typeClause.Type);

        Assert.Equal(SyntaxKind.StringKeyword, fixedStringType.StringKeyword.Kind);
        Assert.Equal(SyntaxKind.StarToken, fixedStringType.StarToken.Kind);
        Assert.Equal(4, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(fixedStringType.LengthExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_ConstStatementWithFixedLengthStringType()
    {
        var program = ParseProgram("CONST code AS STRING * 4 = \"AB\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var constStatement = AssertGlobalStatement<ConstStatementSyntax>(program);
        var fixedStringType = AssertFixedLengthStringType(constStatement.Declaration.TypeClause.Type);

        Assert.Equal(4, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(fixedStringType.LengthExpression).LiteralToken.Value));
        Assert.Equal("AB", Assert.IsType<string>(Assert.IsType<LiteralExpressionSyntax>(constStatement.Declaration.Initializer).LiteralToken.Value));
    }

    [Fact]
    public void Parse_FunctionDeclarationWithFixedLengthStringReturnType()
    {
        const string text = """
FUNCTION Code() AS STRING * 4
    RETURN "AB"
END FUNCTION
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        var fixedStringType = AssertFixedLengthStringType(function.ReturnType.Type);

        Assert.Equal(4, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(fixedStringType.LengthExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_RedimStatement()
    {
        var program = ParseProgram("REDIM items(1 TO 3)", out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = AssertGlobalStatement<RedimStatementSyntax>(program);
        Assert.False(redim.IsPreserve);
        Assert.NotNull(redim.Identifier);
        Assert.Equal("items", redim.Identifier!.Text);
        Assert.IsType<LiteralExpressionSyntax>(redim.ArrayBounds.StartExpression);
        Assert.Equal(SyntaxKind.ToKeyword, redim.ArrayBounds.ToKeyword?.Kind);
        Assert.IsType<LiteralExpressionSyntax>(redim.ArrayBounds.EndExpression);
    }

    [Fact]
    public void Parse_RedimStatementWithMultipleDimensions()
    {
        var program = ParseProgram("REDIM grid(1 TO 2, 10 TO 11)", out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = AssertGlobalStatement<RedimStatementSyntax>(program);
        Assert.Equal("grid", redim.Identifier!.Text);
        Assert.Equal(2, redim.ArrayBounds.Rank);
        Assert.Single(redim.ArrayBounds.CommaTokens);

        var firstDimension = redim.ArrayBounds.Dimensions[0];
        Assert.Equal(1, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(firstDimension.StartExpression).LiteralToken.Value));
        Assert.Equal(2, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(firstDimension.EndExpression).LiteralToken.Value));

        var secondDimension = redim.ArrayBounds.Dimensions[1];
        Assert.Equal(10, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(secondDimension.StartExpression).LiteralToken.Value));
        Assert.Equal(11, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(secondDimension.EndExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_RedimPreserveStatement()
    {
        var program = ParseProgram("REDIM PRESERVE items(2 TO 4)", out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = AssertGlobalStatement<RedimStatementSyntax>(program);
        Assert.True(redim.IsPreserve);
        Assert.Equal(SyntaxKind.PreserveKeyword, redim.PreserveKeyword?.Kind);
        Assert.NotNull(redim.Identifier);
        Assert.Equal("items", redim.Identifier!.Text);
        Assert.IsType<LiteralExpressionSyntax>(redim.ArrayBounds.StartExpression);
        Assert.Equal(SyntaxKind.ToKeyword, redim.ArrayBounds.ToKeyword?.Kind);
        Assert.IsType<LiteralExpressionSyntax>(redim.ArrayBounds.EndExpression);
    }

    [Fact]
    public void Parse_EraseStatement()
    {
        var program = ParseProgram("ERASE items", out var diagnostics);

        Assert.Empty(diagnostics);
        var erase = AssertGlobalStatement<EraseStatementSyntax>(program);
        Assert.NotNull(erase.Identifier);
        Assert.Equal("items", erase.Identifier!.Text);
    }

    [Fact]
    public void Parse_RedimStatementWithTypeFieldTarget()
    {
        var program = ParseProgram("REDIM user.Scores(1 TO 3)", out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = AssertGlobalStatement<RedimStatementSyntax>(program);
        var target = Assert.IsType<FieldAccessExpressionSyntax>(redim.Target);
        Assert.Equal("Scores", target.Identifier.Text);
        Assert.Equal("user", Assert.IsType<NameExpressionSyntax>(target.Target).IdentifierToken.Text);
    }

    [Fact]
    public void Parse_RedimStatementWithIndexedTypeFieldTarget()
    {
        var program = ParseProgram("REDIM people(1).Scores(1 TO 3)", out var diagnostics);

        Assert.Empty(diagnostics);
        var redim = AssertGlobalStatement<RedimStatementSyntax>(program);
        var target = Assert.IsType<FieldAccessExpressionSyntax>(redim.Target);
        Assert.Equal("Scores", target.Identifier.Text);

        var recordTarget = Assert.IsType<CallExpressionSyntax>(target.Target);
        Assert.Equal("people", Assert.IsType<NameExpressionSyntax>(recordTarget.Target).IdentifierToken.Text);
        Assert.Single(recordTarget.Arguments);
    }

    [Fact]
    public void Parse_EraseStatementWithIndexedTypeFieldTarget()
    {
        var program = ParseProgram("ERASE people(1).Scores", out var diagnostics);

        Assert.Empty(diagnostics);
        var erase = AssertGlobalStatement<EraseStatementSyntax>(program);
        var target = Assert.IsType<FieldAccessExpressionSyntax>(erase.Target);
        Assert.Equal("Scores", target.Identifier.Text);

        var recordTarget = Assert.IsType<CallExpressionSyntax>(target.Target);
        Assert.Equal("people", Assert.IsType<NameExpressionSyntax>(recordTarget.Target).IdentifierToken.Text);
        Assert.Single(recordTarget.Arguments);
    }

    [Fact]
    public void Parse_AssignmentStatement()
    {
        var program = ParseProgram("value = 10", out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = AssertGlobalStatement<AssignmentStatementSyntax>(program);
        var target = Assert.IsType<NameExpressionSyntax>(assignment.Target);
        Assert.Equal("value", target.IdentifierToken.Text);
    }

    [Fact]
    public void Parse_PrintStatement()
    {
        var program = ParseProgram("PRINT name", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertPrintStatement(program);
        var expression = Assert.IsType<NameExpressionSyntax>(AssertSinglePrintExpression(statement));
        Assert.Equal("name", expression.IdentifierToken.Text);
        Assert.Null(statement.Items[0].SeparatorToken);
    }

    [Fact]
    public void Parse_EmptyPrintStatement()
    {
        var program = ParseProgram("PRINT", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(AssertPrintStatement(program).Items);
    }

    [Fact]
    public void Parse_PrintStatementWithSemicolon()
    {
        var program = ParseProgram("PRINT first; second;", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertPrintStatement(program);
        Assert.Equal(2, statement.Items.Count);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.Items[0].SeparatorToken?.Kind);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.Items[1].SeparatorToken?.Kind);
    }

    [Fact]
    public void Parse_PrintStatementWithComma()
    {
        var program = ParseProgram("PRINT first, second", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertPrintStatement(program);
        Assert.Equal(2, statement.Items.Count);
        Assert.Equal(SyntaxKind.CommaToken, statement.Items[0].SeparatorToken?.Kind);
        Assert.Null(statement.Items[1].SeparatorToken);
    }

    [Fact]
    public void Parse_PrintStatementWithMixedList()
    {
        var program = ParseProgram("PRINT 1; 2, 3", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertPrintStatement(program);
        Assert.Equal(3, statement.Items.Count);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.Items[0].SeparatorToken?.Kind);
        Assert.Equal(SyntaxKind.CommaToken, statement.Items[1].SeparatorToken?.Kind);
        Assert.Null(statement.Items[2].SeparatorToken);
    }

    [Fact]
    public void Parse_PrintUsingStatement()
    {
        var program = ParseProgram("PRINT USING \"##.##\"; price; tax,", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<PrintUsingStatementSyntax>(program);
        Assert.Equal(SyntaxKind.UsingKeyword, statement.UsingKeyword.Kind);
        Assert.Equal("##.##", Assert.IsType<LiteralExpressionSyntax>(statement.FormatExpression).LiteralToken.Value);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.FormatSeparatorToken.Kind);
        Assert.Equal(2, statement.Items.Count);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.Items[0].SeparatorToken?.Kind);
        Assert.Equal(SyntaxKind.CommaToken, statement.Items[1].SeparatorToken?.Kind);
    }

    [Fact]
    public void Parse_PrintUsingStatementWithoutArguments()
    {
        var program = ParseProgram("PRINT USING \"Total:\"", out var diagnostics);

        var statement = AssertGlobalStatement<PrintUsingStatementSyntax>(program);
        Assert.Equal("Total:", Assert.IsType<LiteralExpressionSyntax>(statement.FormatExpression).LiteralToken.Value);
        Assert.Empty(statement.Items);
        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1001");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InputStatementWithMultipleVariables()
    {
        var program = ParseProgram("INPUT a, b", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<InputStatementSyntax>(program);
        Assert.Null(statement.PromptToken);
        Assert.Equal(["a", "b"], statement.Identifiers.Select(static token => token.Text));
        Assert.Single(statement.CommaTokens);
    }

    [Fact]
    public void Parse_InputStatementWithPromptAndSemicolon()
    {
        var program = ParseProgram("INPUT \"Name\"; value", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<InputStatementSyntax>(program);
        Assert.Equal("Name", statement.PromptToken?.Value);
        Assert.Equal(SyntaxKind.SemicolonToken, statement.PromptSeparatorToken?.Kind);
        Assert.Equal("value", Assert.Single(statement.Identifiers).Text);
    }

    [Fact]
    public void Parse_InputStatementWithPromptAndComma()
    {
        var program = ParseProgram("INPUT \"Pair\", left, right", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<InputStatementSyntax>(program);
        Assert.Equal("Pair", statement.PromptToken?.Value);
        Assert.Equal(SyntaxKind.CommaToken, statement.PromptSeparatorToken?.Kind);
        Assert.Equal(["left", "right"], statement.Identifiers.Select(static token => token.Text));
    }

    [Fact]
    public void Parse_IfElseStatement()
    {
        const string text = """
IF TRUE THEN
    PRINT "yes"
ELSE
    PRINT "no"
END IF
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<IfStatementSyntax>(program);
        Assert.Single(statement.Statements);
        Assert.NotNull(statement.ElseClause);
        Assert.Single(statement.ElseClause!.Statements);
    }

    [Fact]
    public void Parse_IfElseIfElseStatement()
    {
        const string text = """
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<IfStatementSyntax>(program);
        var elseIfClause = Assert.Single(statement.ElseIfClauses);
        Assert.IsType<BinaryExpressionSyntax>(elseIfClause.Condition);
        Assert.Single(elseIfClause.Statements);
        Assert.NotNull(statement.ElseClause);
        Assert.Single(statement.ElseClause!.Statements);
    }

    [Fact]
    public void Parse_SelectCaseStatement()
    {
        const string text = """
SELECT CASE score
    CASE 1, 2 TO 4, IS > 10
        PRINT "match"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<SelectCaseStatementSyntax>(program);
        Assert.Equal("score", Assert.IsType<NameExpressionSyntax>(statement.TestExpression).IdentifierToken.Text);

        var clause = Assert.Single(statement.Clauses);
        Assert.Equal(3, clause.Selectors.Count);
        Assert.IsType<CaseValueSelectorSyntax>(clause.Selectors[0]);
        Assert.IsType<CaseRangeSelectorSyntax>(clause.Selectors[1]);
        var comparisonSelector = Assert.IsType<CaseComparisonSelectorSyntax>(clause.Selectors[2]);
        Assert.Equal(SyntaxKind.GreaterToken, comparisonSelector.OperatorToken.Kind);
        Assert.Single(clause.Statements);

        Assert.NotNull(statement.CaseElseClause);
        Assert.Single(statement.CaseElseClause!.Statements);
    }

    [Fact]
    public void Parse_SelectCaseReportsEmptyCaseSelector()
    {
        const string text = """
SELECT CASE score
    CASE
        PRINT "missing selector"
END SELECT
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1003");
    }

    [Fact]
    public void Parse_SelectCaseReportsCaseElseBeforeLaterCase()
    {
        const string text = """
SELECT CASE score
    CASE ELSE
        PRINT "fallback"
    CASE 1
        PRINT "late"
END SELECT
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1004");
    }

    [Fact]
    public void Parse_SelectCaseReportsMissingCaseIsOperator()
    {
        const string text = """
SELECT CASE score
    CASE IS
        PRINT "missing operator"
END SELECT
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1001");
    }

    [Fact]
    public void Parse_IfReportsDuplicateElse()
    {
        const string text = """
IF TRUE THEN
    PRINT "yes"
ELSE
    PRINT "first"
ELSE
    PRINT "duplicate"
END IF
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1005");
    }

    [Fact]
    public void Parse_IfReportsDuplicateElseIfAfterElse()
    {
        const string text = """
IF TRUE THEN
    PRINT "yes"
ELSE
    PRINT "first"
ELSEIF FALSE THEN
    PRINT "duplicate"
END IF
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1005");
    }

    [Fact]
    public void Parse_SelectCaseReportsDuplicateCaseElse()
    {
        const string text = """
SELECT CASE 1
    CASE 1
        PRINT "one"
    CASE ELSE
        PRINT "first"
    CASE ELSE
        PRINT "duplicate"
END SELECT
""";

        ParseProgram(text, out var diagnostics);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "MB1006");
    }

    [Fact]
    public void Parse_ForNextStatement()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<ForStatementSyntax>(program);
        Assert.Equal("i", statement.Identifier.Text);
        Assert.Single(statement.Statements);
    }

    [Fact]
    public void Parse_WhileStatement()
    {
        const string text = """
WHILE TRUE
    PRINT "loop"
END WHILE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<WhileStatementSyntax>(program);
        Assert.Single(statement.Statements);
    }

    [Fact]
    public void Parse_DoLoopWithLeadingWhileCondition()
    {
        const string text = """
DO WHILE TRUE
    PRINT "loop"
LOOP
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<DoLoopStatementSyntax>(program);
        Assert.Equal(SyntaxKind.DoKeyword, statement.DoKeyword.Kind);
        Assert.Equal(SyntaxKind.WhileKeyword, statement.LeadingCondition?.Keyword.Kind);
        Assert.Single(statement.Statements);
        Assert.Equal(SyntaxKind.LoopKeyword, statement.LoopKeyword.Kind);
        Assert.Null(statement.TrailingCondition);
    }

    [Fact]
    public void Parse_DoLoopWithLeadingUntilCondition()
    {
        const string text = """
DO UNTIL done
    PRINT "loop"
LOOP
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<DoLoopStatementSyntax>(program);
        Assert.Equal(SyntaxKind.UntilKeyword, statement.LeadingCondition?.Keyword.Kind);
        Assert.Single(statement.Statements);
        Assert.Null(statement.TrailingCondition);
    }

    [Fact]
    public void Parse_DoLoopWithTrailingWhileCondition()
    {
        const string text = """
DO
    PRINT "loop"
LOOP WHILE keepGoing
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<DoLoopStatementSyntax>(program);
        Assert.Null(statement.LeadingCondition);
        Assert.Single(statement.Statements);
        Assert.Equal(SyntaxKind.WhileKeyword, statement.TrailingCondition?.Keyword.Kind);
    }

    [Fact]
    public void Parse_DoLoopWithTrailingUntilCondition()
    {
        const string text = """
DO
    PRINT "loop"
LOOP UNTIL done
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<DoLoopStatementSyntax>(program);
        Assert.Null(statement.LeadingCondition);
        Assert.Single(statement.Statements);
        Assert.Equal(SyntaxKind.UntilKeyword, statement.TrailingCondition?.Keyword.Kind);
    }

    [Fact]
    public void Parse_DoLoopWithoutCondition()
    {
        const string text = """
DO
    EXIT DO
LOOP
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<DoLoopStatementSyntax>(program);
        Assert.Null(statement.LeadingCondition);
        Assert.Null(statement.TrailingCondition);
        Assert.Single(statement.Statements);
    }

    [Fact]
    public void Parse_TryCatchStatement()
    {
        const string text = """
TRY
    PRINT 1
CATCH err
    PRINT err.Code
END TRY
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<TryStatementSyntax>(program);
        Assert.Equal(SyntaxKind.TryKeyword, statement.TryKeyword.Kind);
        Assert.Single(statement.Statements);
        Assert.NotNull(statement.CatchClause);
        Assert.Equal("err", statement.CatchClause!.Identifier.Text);
        Assert.Single(statement.CatchClause.Statements);
        Assert.Null(statement.FinallyClause);
        Assert.Equal(SyntaxKind.EndKeyword, statement.EndKeyword.Kind);
        Assert.Equal(SyntaxKind.TryKeyword, statement.EndTryKeyword.Kind);
    }

    [Fact]
    public void Parse_TryFinallyStatement()
    {
        const string text = """
TRY
    PRINT 1
FINALLY
    PRINT "cleanup"
END TRY
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<TryStatementSyntax>(program);
        Assert.Single(statement.Statements);
        Assert.Null(statement.CatchClause);
        Assert.NotNull(statement.FinallyClause);
        Assert.Single(statement.FinallyClause!.Statements);
    }

    [Fact]
    public void Parse_TryCatchFinallyStatement()
    {
        const string text = """
TRY
    PRINT 1
CATCH err
    PRINT err.Message
FINALLY
    PRINT "cleanup"
END TRY
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<TryStatementSyntax>(program);
        Assert.Single(statement.Statements);
        Assert.NotNull(statement.CatchClause);
        Assert.Single(statement.CatchClause!.Statements);
        Assert.NotNull(statement.FinallyClause);
        Assert.Single(statement.FinallyClause!.Statements);
    }

    [Fact]
    public void Parse_ReportsTryWithoutCatchOrFinally()
    {
        const string text = """
TRY
    PRINT 1
END TRY
""";

        ParseProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1001");
        Assert.Contains("CATCH", diagnostic.Message);
        Assert.Contains("FINALLY", diagnostic.Message);
    }

    [Fact]
    public void Parse_ExitForStatement()
    {
        var program = ParseProgram("EXIT FOR", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertGlobalStatement<ExitForStatementSyntax>(program);
    }

    [Fact]
    public void Parse_ExitDoStatement()
    {
        var program = ParseProgram("EXIT DO", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertGlobalStatement<ExitDoStatementSyntax>(program);
    }

    [Fact]
    public void Parse_ContinueForStatement()
    {
        var program = ParseProgram("CONTINUE FOR", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertGlobalStatement<ContinueForStatementSyntax>(program);
    }

    [Fact]
    public void Parse_ContinueWhileStatement()
    {
        var program = ParseProgram("CONTINUE WHILE", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertGlobalStatement<ContinueWhileStatementSyntax>(program);
    }

    [Fact]
    public void Parse_ContinueDoStatement()
    {
        var program = ParseProgram("CONTINUE DO", out var diagnostics);

        Assert.Empty(diagnostics);
        AssertGlobalStatement<ContinueDoStatementSyntax>(program);
    }

    [Fact]
    public void Parse_NestedForAndWhileStatements()
    {
        const string text = """
DIM i AS INTEGER
DIM j AS INTEGER
FOR i = 1 TO 2
    j = 1
    WHILE j <= 2
        PRINT i; j
        j = j + 1
    END WHILE
NEXT
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var forStatement = Assert.IsType<ForStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        var whileStatement = Assert.IsType<WhileStatementSyntax>(forStatement.Statements[1]);
        Assert.Equal(2, whileStatement.Statements.Count);
    }

    [Fact]
    public void Parse_NestedDoLoopAndForStatements()
    {
        const string text = """
DIM i AS INTEGER
DIM count AS INTEGER = 0
FOR i = 1 TO 2
    DO WHILE count < 2
        PRINT i
        count = count + 1
    LOOP
NEXT
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var forStatement = Assert.IsType<ForStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        var doLoopStatement = Assert.IsType<DoLoopStatementSyntax>(Assert.Single(forStatement.Statements));
        Assert.Equal(2, doLoopStatement.Statements.Count);
    }

    [Fact]
    public void Parse_SubDeclaration()
    {
        const string text = """
SUB Greet(name AS STRING)
    PRINT name
END SUB
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var sub = Assert.IsType<SubDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Equal("Greet", sub.Identifier.Text);
        Assert.Single(sub.Parameters);
        Assert.Single(sub.Statements);
    }

    [Fact]
    public void Parse_FunctionWithReturn()
    {
        const string text = """
FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Equal("Add", function.Identifier.Text);
        Assert.Equal(2, function.Parameters.Count);
        var returnStatement = Assert.IsType<ReturnStatementSyntax>(Assert.Single(function.Statements));
        Assert.NotNull(returnStatement.Expression);
    }

    [Fact]
    public void Parse_SubDeclarationWithLocalFunction()
    {
        const string text = """
SUB Outer()
    FUNCTION LocalValue() AS INTEGER
        RETURN 42
    END FUNCTION
    PRINT LocalValue()
END SUB
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var sub = Assert.IsType<SubDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Equal(2, sub.Statements.Count);

        var localFunction = Assert.IsType<LocalFunctionDeclarationStatementSyntax>(sub.Statements[0]);
        Assert.Equal("LocalValue", localFunction.Declaration.Identifier.Text);
        var returnStatement = Assert.IsType<ReturnStatementSyntax>(Assert.Single(localFunction.Declaration.Statements));
        Assert.NotNull(returnStatement.Expression);

        Assert.IsType<PrintStatementSyntax>(sub.Statements[1]);
    }

    [Fact]
    public void Parse_TypeDeclarationAndFieldAccess()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

DIM user AS Person
user.Name = "Ada"
PRINT user.Age
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);
        Assert.Equal("Person", typeDeclaration.Identifier.Text);
        Assert.Collection(
            typeDeclaration.Fields,
            firstField =>
            {
                var declarator = Assert.Single(firstField.Declarators);
                Assert.Equal("Name", declarator.Identifier.Text);
            },
            secondField =>
            {
                var declarator = Assert.Single(secondField.Declarators);
                Assert.Equal("Age", declarator.Identifier.Text);
            });

        var dim = Assert.IsType<DimStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var typeClause = Assert.IsType<TypeClauseSyntax>(Assert.Single(dim.Declarations).TypeClause);
        var userType = Assert.IsType<NamedTypeSyntax>(typeClause.Type);
        Assert.Equal("Person", userType.TypeToken.Text);

        var assignment = Assert.IsType<AssignmentStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        var assignmentTarget = Assert.IsType<FieldAccessExpressionSyntax>(assignment.Target);
        Assert.Equal("Name", assignmentTarget.Identifier.Text);

        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        var printTarget = Assert.IsType<FieldAccessExpressionSyntax>(AssertSinglePrintExpression(print));
        Assert.Equal("Age", printTarget.Identifier.Text);
    }

    [Fact]
    public void Parse_TypeArrayElementFieldAccess()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

DIM people(1 TO 2) AS Person
people(1).Name = "Ada"
PRINT people(2).Name
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<AssignmentStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[2]).Statement);
        var assignmentTarget = Assert.IsType<FieldAccessExpressionSyntax>(assignment.Target);
        var assignmentArrayAccess = Assert.IsType<CallExpressionSyntax>(assignmentTarget.Target);
        Assert.Equal("Name", assignmentTarget.Identifier.Text);
        Assert.Equal("people", Assert.IsType<NameExpressionSyntax>(assignmentArrayAccess.Target).IdentifierToken.Text);
        Assert.Single(assignmentArrayAccess.Arguments);

        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        var printTarget = Assert.IsType<FieldAccessExpressionSyntax>(AssertSinglePrintExpression(print));
        var printArrayAccess = Assert.IsType<CallExpressionSyntax>(printTarget.Target);
        Assert.Equal("Name", printTarget.Identifier.Text);
        Assert.Equal("people", Assert.IsType<NameExpressionSyntax>(printArrayAccess.Target).IdentifierToken.Text);
        Assert.Single(printArrayAccess.Arguments);
    }

    [Fact]
    public void Parse_TypeArrayFieldViewAccess()
    {
        const string text = """
TYPE Board
    Cells(,) AS INTEGER
END TYPE

DIM board AS Board
DIM lineView() AS INTEGER
lineView = board.Cells(2)
PRINT board.Cells(2 TO 3, 3 TO 4)
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);

        var assignment = Assert.IsType<AssignmentStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[3]).Statement);
        var assignmentValue = Assert.IsType<CallExpressionSyntax>(assignment.Expression);
        var assignmentFieldTarget = Assert.IsType<FieldAccessExpressionSyntax>(assignmentValue.Target);
        Assert.Equal("Cells", assignmentFieldTarget.Identifier.Text);
        Assert.Equal("board", Assert.IsType<NameExpressionSyntax>(assignmentFieldTarget.Target).IdentifierToken.Text);
        Assert.Single(assignmentValue.Arguments);

        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[4]).Statement);
        var printValue = Assert.IsType<CallExpressionSyntax>(AssertSinglePrintExpression(print));
        var printFieldTarget = Assert.IsType<FieldAccessExpressionSyntax>(printValue.Target);
        Assert.Equal("Cells", printFieldTarget.Identifier.Text);
        Assert.Equal(2, printValue.Arguments.Count);
        Assert.All(printValue.Arguments, static argument => Assert.IsType<RangeExpressionSyntax>(argument));
    }

    [Fact]
    public void Parse_TypeDeclarationWithDynamicArrayField()
    {
        const string text = """
TYPE Person
    Scores() AS INTEGER
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);
        var field = Assert.Single(typeDeclaration.Fields);
        var declarator = Assert.Single(field.Declarators);
        Assert.Equal("Scores", declarator.Identifier.Text);
        Assert.NotNull(declarator.ArrayBounds);
        Assert.True(declarator.ArrayBounds!.IsDynamic);
        AssertNamedType(field.TypeClause.Type, SyntaxKind.IntegerKeyword);
    }

    [Fact]
    public void Parse_TypeDeclarationWithFixedArrayField()
    {
        const string text = """
TYPE Person
    Scores(-1 TO 3) AS INTEGER
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);
        var field = Assert.Single(typeDeclaration.Fields);
        var declarator = Assert.Single(field.Declarators);
        Assert.NotNull(declarator.ArrayBounds);
        var bounds = declarator.ArrayBounds!;
        Assert.False(bounds.IsDynamic);
        Assert.IsType<UnaryExpressionSyntax>(bounds.StartExpression);
        Assert.Equal(SyntaxKind.ToKeyword, bounds.ToKeyword?.Kind);
        Assert.IsType<LiteralExpressionSyntax>(bounds.EndExpression);
        AssertNamedType(field.TypeClause.Type, SyntaxKind.IntegerKeyword);
    }

    [Fact]
    public void Parse_TypeDeclarationWithFixedLengthStringField()
    {
        const string text = """
TYPE Person
    Name AS STRING * 20
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);
        var field = Assert.Single(typeDeclaration.Fields);
        var fixedStringType = AssertFixedLengthStringType(field.TypeClause.Type);

        Assert.Equal("Name", Assert.Single(field.Declarators).Identifier.Text);
        Assert.Equal(20, Assert.IsType<int>(Assert.IsType<LiteralExpressionSyntax>(fixedStringType.LengthExpression).LiteralToken.Value));
    }

    [Fact]
    public void Parse_TypeDeclarationWithFieldDefaultValue()
    {
        const string text = """
TYPE Person
    Name AS STRING = "Ada"
    Age AS INTEGER = 36
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);

        Assert.Collection(
            typeDeclaration.Fields,
            nameField =>
            {
                Assert.NotNull(nameField.EqualsToken);
                var initializer = Assert.IsType<LiteralExpressionSyntax>(nameField.Initializer);
                Assert.Equal("Ada", Assert.IsType<string>(initializer.LiteralToken.Value));
            },
            ageField =>
            {
                Assert.NotNull(ageField.EqualsToken);
                var initializer = Assert.IsType<LiteralExpressionSyntax>(ageField.Initializer);
                Assert.Equal(36, Assert.IsType<int>(initializer.LiteralToken.Value));
            });
    }

    [Fact]
    public void Parse_TypeDeclarationWithGroupedFields()
    {
        const string text = """
TYPE Person
    Name, Alias AS STRING
    Scores(), Flags(1 TO 2) AS INTEGER
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(program.Members[0]);
        Assert.Equal(2, typeDeclaration.Fields.Count);

        Assert.Collection(
            typeDeclaration.Fields[0].Declarators,
            first =>
            {
                Assert.Equal("Name", first.Identifier.Text);
                Assert.Null(first.ArrayBounds);
            },
            second =>
            {
                Assert.Equal("Alias", second.Identifier.Text);
                Assert.Null(second.ArrayBounds);
            });

        Assert.Collection(
            typeDeclaration.Fields[1].Declarators,
            first =>
            {
                Assert.Equal("Scores", first.Identifier.Text);
                Assert.True(first.ArrayBounds?.IsDynamic);
            },
            second =>
            {
                Assert.Equal("Flags", second.Identifier.Text);
                Assert.False(second.ArrayBounds?.IsDynamic ?? true);
                Assert.Equal(SyntaxKind.ToKeyword, second.ArrayBounds?.ToKeyword?.Kind);
            });
    }

    [Fact]
    public void Parse_TypeNamedLiteral()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

PRINT Person(Name := "Ada", Age := 36)
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var call = Assert.IsType<CallExpressionSyntax>(AssertSinglePrintExpression(print));
        Assert.Equal("Person", Assert.IsType<NameExpressionSyntax>(call.Target).IdentifierToken.Text);
        Assert.Collection(
            call.Arguments,
            first =>
            {
                var named = Assert.IsType<NamedArgumentExpressionSyntax>(first);
                Assert.Equal("Name", named.IdentifierToken.Text);
                Assert.Equal(SyntaxKind.ColonEqualsToken, named.ColonEqualsToken.Kind);
            },
            second =>
            {
                var named = Assert.IsType<NamedArgumentExpressionSyntax>(second);
                Assert.Equal("Age", named.IdentifierToken.Text);
                Assert.Equal(SyntaxKind.ColonEqualsToken, named.ColonEqualsToken.Kind);
            });
    }

    [Fact]
    public void Parse_TypePositionalLiteral()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Age AS INTEGER
END TYPE

PRINT Person("Ada", 36)
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        var call = Assert.IsType<CallExpressionSyntax>(AssertSinglePrintExpression(print));
        Assert.Equal("Person", Assert.IsType<NameExpressionSyntax>(call.Target).IdentifierToken.Text);
        Assert.Collection(
            call.Arguments,
            first => Assert.Equal("Ada", Assert.IsType<LiteralExpressionSyntax>(first).LiteralToken.Value),
            second => Assert.Equal(36, Assert.IsType<LiteralExpressionSyntax>(second).LiteralToken.Value));
    }

    [Fact]
    public void Parse_TypeDeclarationWithMethodsAndSelfAssignments()
    {
        const string text = """
TYPE Counter
    Value AS INTEGER

    SUB Increment(stepValue AS INTEGER)
        SELF.Value = SELF.Value + stepValue
    END SUB

    FUNCTION Current() AS INTEGER
        RETURN SELF.Value
    END FUNCTION
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(Assert.Single(program.Members));
        Assert.Single(typeDeclaration.Fields);
        Assert.Equal(2, typeDeclaration.Methods.Count);

        var subDeclaration = Assert.IsType<SubDeclarationSyntax>(typeDeclaration.Methods[0]);
        var assignment = Assert.IsType<AssignmentStatementSyntax>(Assert.Single(subDeclaration.Statements));
        var assignmentTarget = Assert.IsType<FieldAccessExpressionSyntax>(assignment.Target);
        Assert.Equal(SyntaxKind.SelfKeyword, Assert.IsType<NameExpressionSyntax>(assignmentTarget.Target).IdentifierToken.Kind);

        var functionDeclaration = Assert.IsType<FunctionDeclarationSyntax>(typeDeclaration.Methods[1]);
        var returnStatement = Assert.IsType<ReturnStatementSyntax>(Assert.Single(functionDeclaration.Statements));
        var returnTarget = Assert.IsType<FieldAccessExpressionSyntax>(returnStatement.Expression);
        Assert.Equal(SyntaxKind.SelfKeyword, Assert.IsType<NameExpressionSyntax>(returnTarget.Target).IdentifierToken.Kind);
    }

    [Fact]
    public void Parse_TypeDeclarationWithConstructor()
    {
        const string text = """
TYPE Person
    Name AS STRING

    SUB New(name AS STRING)
        SELF.Name = name
    END SUB
END TYPE
""";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var typeDeclaration = Assert.IsType<TypeDeclarationSyntax>(Assert.Single(program.Members));
        var constructor = Assert.IsType<SubDeclarationSyntax>(Assert.Single(typeDeclaration.Methods));
        Assert.Equal("New", constructor.Identifier.Text);
        var assignment = Assert.IsType<AssignmentStatementSyntax>(Assert.Single(constructor.Statements));
        var assignmentTarget = Assert.IsType<FieldAccessExpressionSyntax>(assignment.Target);
        Assert.Equal(SyntaxKind.SelfKeyword, Assert.IsType<NameExpressionSyntax>(assignmentTarget.Target).IdentifierToken.Kind);
    }

    [Fact]
    public void Parse_ErrorStatementWithCodeAndMessage()
    {
        const string text = "ERROR \"APP001\", \"Manual failure\"";

        var program = ParseProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = AssertGlobalStatement<ErrorStatementSyntax>(program);
        Assert.Equal(SyntaxKind.ErrorKeyword, statement.ErrorKeyword.Kind);
        Assert.IsType<LiteralExpressionSyntax>(statement.CodeExpression);
        Assert.NotNull(statement.CommaToken);
        Assert.IsType<LiteralExpressionSyntax>(statement.MessageExpression);
    }

    [Fact]
    public void Parse_ReportsMissingThen()
    {
        const string text = """
IF TRUE
    PRINT 1
END IF
""";

        ParseProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1001");
        Assert.Contains("THEN", diagnostic.Message);
    }

    [Fact]
    public void Parse_ReportsMissingEndIf()
    {
        const string text = """
IF TRUE THEN
    PRINT 1
""";

        ParseProgram(text, out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1001");
        Assert.Contains("END IF", diagnostic.Message);
    }

    [Fact]
    public void Parse_ReportsUnfinishedExpression()
    {
        ParseProgram("PRINT 1 +", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1003");
        Assert.Equal("Ожидалось выражение.", diagnostic.Message);
    }

    [Fact]
    public void Parse_ReportsReadableRussianExpectedIdentifierMessage()
    {
        ParseProgram("DIM AS INTEGER", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1002");
        Assert.Equal("Ожидался идентификатор.", diagnostic.Message);
    }

    [Fact]
    public void Parse_ReportsReadableRussianUnexpectedBoundaryMessage()
    {
        ParseProgram("END IF", out var diagnostics);

        var diagnostic = Assert.Single(diagnostics, static d => d.Code == "MB1004");
        Assert.Equal("Неожиданный маркер конца блока: 'END'.", diagnostic.Message);
    }

    private static ProgramSyntax ParseProgram(string text, out DiagnosticBag diagnostics)
    {
        return Parser.Parse(text, out diagnostics);
    }

    private static ExpressionSyntax AssertSinglePrintExpression(PrintStatementSyntax statement)
    {
        return Assert.Single(statement.Items).Expression;
    }

    private static PrintStatementSyntax AssertPrintStatement(ProgramSyntax program)
    {
        return AssertGlobalStatement<PrintStatementSyntax>(program);
    }

    private static NamedTypeSyntax AssertNamedType(TypeSyntax type, SyntaxKind expectedKind)
    {
        var namedType = Assert.IsType<NamedTypeSyntax>(type);
        Assert.Equal(expectedKind, namedType.TypeToken.Kind);
        return namedType;
    }

    private static ArrayTypeSyntax AssertArrayType(TypeSyntax type)
    {
        return Assert.IsType<ArrayTypeSyntax>(type);
    }

    private static FixedLengthStringTypeSyntax AssertFixedLengthStringType(TypeSyntax type)
    {
        return Assert.IsType<FixedLengthStringTypeSyntax>(type);
    }

    private static TStatement AssertGlobalStatement<TStatement>(ProgramSyntax program)
        where TStatement : StatementSyntax
    {
        var member = Assert.IsType<GlobalStatementSyntax>(Assert.Single(program.Members));
        return Assert.IsType<TStatement>(member.Statement);
    }

    [Fact]
    public void Debug_InterpolatedStringTokens()
    {
        var tokens = Axiom.Lexing.Lexer.Lex("$\"Hello, {name}!\"", out var diagnostics);

        var tokenList = tokens.ToList();
        Assert.Empty(diagnostics);

        var kinds = tokenList.Select(t => $"{t.Kind}:{t.Text}").ToList();
        Assert.True(tokenList.Count > 0, $"No tokens. Kinds: {string.Join(", ", kinds)}");
    }

    [Fact]
    public void Parse_InterpolatedStringExpression()
    {
        var program = Parser.Parse("PRINT $\"Hello, {name}!\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = AssertGlobalStatement<PrintStatementSyntax>(program);
        Assert.Single(print.Items);
    }

    [Fact]
    public void Parse_InterpolatedStringWithOperator()
    {
        var program = Parser.Parse("PRINT $\"{a + b}\"", out var diagnostics);

        Assert.Empty(diagnostics);
        var print = AssertGlobalStatement<PrintStatementSyntax>(program);
        Assert.Single(print.Items);
    }
}
