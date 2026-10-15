using Axiom.Lexing;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class EditorHoverTests
{
    private static IReadOnlyList<EditorHoverInfo> Hovers(string source)
    {
        var syntax = Parser.Parse(source, out var errors);
        Assert.Empty(errors);
        var identifiers = Lexer.Lex(source, out _).Where(t => t.Kind == TokenKind.IdentifierToken)
            .Select(t => new EditorIdentifier(t.Text, t.Location.Span)).ToArray();
        return Binder.GetEditorHovers(syntax, identifiers);
    }

    [Theory]
    [InlineData("DECIMAL")]
    [InlineData("STRING")]
    [InlineData("INTEGER")]
    [InlineData("LONG")]
    [InlineData("DOUBLE")]
    [InlineData("BOOLEAN")]
    [InlineData("DATE")]
    [InlineData("TIME")]
    [InlineData("DATETIME")]
    [InlineData("FILE")]
    [InlineData("IMAGE")]
    [InlineData("SPRITE")]
    [InlineData("TILESET")]
    [InlineData("TILEMAP")]
    public void DeclaredAndReferencedType(string type)
    {
        var source = $"DIM price AS {type}\nPRINT price\n";
        var items = Hovers(source).Where(h => h.Name == "price").ToArray();
        Assert.Equal(2, items.Length);
        Assert.All(items, h => Assert.Equal(type, h.Type));
    }

    [Fact]
    public void InferredArraysGroupedDeclarationsConstantsAndParameters()
    {
        var source = """
            DIM amount = CDEC(2)
            DIM label, title AS STRING
            DIM grid(1 TO 2, 1 TO 3) AS DECIMAL
            CONST limit AS INTEGER = 5
            SUB Show(BYREF value AS DECIMAL, values AS INTEGER())
                PRINT value, values(0)
            END SUB
            PRINT amount, label, title, grid(1, 1), limit
            """;
        var items = Hovers(source);
        Assert.All(items.Where(h => h.Name == "amount"), h => Assert.Equal("DECIMAL", h.Type));
        Assert.Equal(2, items.Count(h => h.Name == "label" && h.Type == "STRING"));
        Assert.Equal(2, items.Count(h => h.Name == "grid" && h.Type == "DECIMAL(,)"));
        Assert.All(items.Where(h => h.Name == "limit"), h => Assert.Equal("constant", h.Kind));
        Assert.Equal(2, items.Count(h => h.Name == "value" && h.Kind == "parameter" && h.Type == "DECIMAL"));
        Assert.Contains(items, h => h.Name == "Show" && h.Signature == "SUB Show(BYREF value AS DECIMAL, values AS INTEGER())");
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ScopeFieldsInterpolationAndForUseBoundSymbols(string newline)
    {
        var source = """
            TYPE Product
                Price AS DECIMAL
            END TYPE
            DIM item AS Product
            DIM value AS STRING
            SUB Show(value AS INTEGER)
                PRINT value
            END SUB
            PRINT value, item.Price
            PRINT $"Цена {item.Price}"
            FOR counter = 1 TO 2
                PRINT counter
            NEXT
            """;
        source = source.Replace("\r\n", "\n").Replace("\n", newline);
        var items = Hovers(source);
        Assert.Contains(items, h => h.Start == source.IndexOf("value, item", StringComparison.Ordinal) && h.Type == "STRING");
        Assert.Contains(items, h => h.Start == source.IndexOf("PRINT value", StringComparison.Ordinal) + "PRINT ".Length && h.Type == "INTEGER");
        Assert.Equal(3, items.Count(h => h.Kind == "field" && h.Name == "Price" && h.Type == "DECIMAL"));
        Assert.Equal(2, items.Count(h => h.Name == "counter" && h.Type == "INTEGER"));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void InputFilesCatchAndNextHaveExactIdentifierLocations(string newline)
    {
        var source = """
            DIM answer AS STRING
            INPUT "answer"; answer
            DIM stream AS FILE
            OPEN "stream" FOR OUTPUT AS stream
            CLOSE stream
            TRY
                ERROR "DEMO"
            CATCH problem
                PRINT problem.Message
            END TRY
            FOR counter = 1 TO 2
                PRINT counter
            NEXT counter
            """;
        source = source.Replace("\r\n", "\n").Replace("\n", newline);
        var items = Hovers(source);
        Assert.Equal(2, items.Count(h => h.Name == "answer" && h.Type == "STRING"));
        Assert.Equal(3, items.Count(h => h.Name == "stream" && h.Type == "FILE"));
        var catchStart = source.IndexOf("CATCH problem", StringComparison.Ordinal) + "CATCH ".Length;
        Assert.Contains(items, h => h.Name == "problem" && h.Kind == "errorVariable" && h.Type == "ERROR" && h.Start == catchStart && h.Length == "problem".Length);
        Assert.Equal(3, items.Count(h => h.Name == "counter" && h.Type == "INTEGER"));
    }

    [Fact]
    public void CatchVariableIsReadOnlyErrorRatherThanConstant()
    {
        var source = """
            CONST limit AS INTEGER = 2
            TRY
                PRINT POW(-2, 0.5)
            CATCH problem
                PRINT problem.Code, problem.Message, problem.Line, problem.Column
            END TRY
            PRINT limit
            """;
        var items = Hovers(source);
        var caught = items.Where(h => h.Name == "problem").ToArray();
        Assert.Equal(5, caught.Length);
        Assert.All(caught, h => {
            Assert.Equal("errorVariable", h.Kind);
            Assert.Equal("ERROR", h.Type);
        });
        Assert.All(items.Where(h => h.Name == "limit"), h => Assert.Equal("constant", h.Kind));
        foreach (var (name, type) in new[] { ("Code", "STRING"), ("Message", "STRING"), ("Line", "INTEGER"), ("Column", "INTEGER") })
            Assert.Contains(items, h => h.Name == name && h.Kind == "field" && h.Type == type);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void BlockShadowingAndArbitraryTypeInferenceComeFromBinder(string newline)
    {
        var source = """
            DIM result = CDEC(2) + CDEC(3)
            DIM value AS STRING
            IF TRUE THEN
                DIM value AS BOOLEAN
                PRINT value
            END IF
            PRINT value, result
            """;
        source = source.Replace("\r\n", "\n").Replace("\n", newline);
        var items = Hovers(source);
        Assert.Contains(items, h => h.Name == "result" && h.Type == "DECIMAL");
        Assert.Contains(items, h => h.Start == source.IndexOf("PRINT value", StringComparison.Ordinal) + "PRINT ".Length && h.Type == "BOOLEAN");
        Assert.Contains(items, h => h.Start == source.IndexOf("value, result", StringComparison.Ordinal) && h.Type == "STRING");
    }

    [Fact]
    public void PredefinedConstantsIncludingInterpolationHaveTheirActualType()
    {
        var items = Hovers("PRINT PI, ROW, COLUMN\nPRINT $\"Число {PI}\"");
        Assert.Equal(2, items.Count(h => h.Name == "PI" && h.Kind == "constant" && h.Type == "DOUBLE"));
        Assert.Contains(items, h => h.Name == "ROW" && h.Kind == "constant" && h.Type == "INTEGER");
        Assert.Contains(items, h => h.Name == "COLUMN" && h.Kind == "constant" && h.Type == "INTEGER");
    }

    [Fact]
    public void CaseSelectorsIncludeBothBoundsAndComparisonExpression()
    {
        var source = """
            DIM selected AS INTEGER = 2
            DIM lower AS INTEGER = 1
            DIM upper AS INTEGER = 3
            SELECT CASE selected
                CASE lower TO upper
                    PRINT selected
                CASE IS > upper
                    PRINT selected
            END SELECT
            """;
        var items = Hovers(source);
        Assert.Equal(2, items.Count(h => h.Name == "lower" && h.Type == "INTEGER"));
        Assert.Equal(3, items.Count(h => h.Name == "upper" && h.Type == "INTEGER"));
    }
}
