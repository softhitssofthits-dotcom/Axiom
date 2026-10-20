using Axiom.Lexing;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Semantics.Tests;

/// <summary>
/// Подсказка обязана скрывать только настоящий неявный получатель метода SELF.
/// Пользовательский параметр THIS и обычная функция с параметром SELF
/// остаются в сигнатуре.
/// </summary>
public sealed class EditorHoverSignatureTests
{
    [Fact]
    public void MethodHidesImplicitSelfAndKeepsExplicitArgument()
    {
        const string source = """
            TYPE Counter
                Value AS INTEGER
                SUB Set(value AS INTEGER)
                    SELF.Value = value
                END SUB
            END TYPE
            DIM item AS Counter
            item.Set(2)
            """;

        var signature = SingleSignature(source, "Set");

        Assert.Equal("SUB Set(value AS INTEGER)", signature);
    }

    [Fact]
    public void OrdinaryFunctionKeepsUserDeclaredThisParameter()
    {
        const string source = """
            FUNCTION EchoThis(THIS AS INTEGER) AS INTEGER
                RETURN THIS
            END FUNCTION
            PRINT EchoThis(5)
            """;

        var signature = SingleSignature(source, "EchoThis");

        Assert.Equal("FUNCTION EchoThis(THIS AS INTEGER) AS INTEGER", signature);
    }

    [Fact]
    public void MethodWithByRefArgumentKeepsThePassingMode()
    {
        const string source = """
            TYPE Counter
                Value AS INTEGER
                SUB Add(BYREF amount AS INTEGER)
                    SELF.Value = SELF.Value + amount
                END SUB
            END TYPE
            """;

        var signature = SingleSignature(source, "Add");

        Assert.Equal("SUB Add(BYREF amount AS INTEGER)", signature);
    }

    [Fact]
    public void MethodWithNoArgumentsShowsEmptyParameterList()
    {
        const string source = """
            TYPE Counter
                Value AS INTEGER
                FUNCTION Read() AS INTEGER
                    RETURN SELF.Value
                END FUNCTION
            END TYPE
            """;

        var signature = SingleSignature(source, "Read");

        Assert.Equal("FUNCTION Read() AS INTEGER", signature);
    }

    private static string SingleSignature(string source, string name)
    {
        var syntax = Parser.Parse(source, out var parseDiagnostics);
        Assert.Empty(parseDiagnostics);

        var identifiers = Lexer.Lex(source, out _)
            .Where(token => token.Kind == TokenKind.IdentifierToken)
            .Select(token => new EditorIdentifier(token.Text, token.Location.Span))
            .ToArray();

        var hovers = Binder.GetEditorHovers(syntax, identifiers);

        var signatures = hovers
            .Where(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && item.Signature is not null)
            .Select(item => item.Signature!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return Assert.Single(signatures);
    }
}
