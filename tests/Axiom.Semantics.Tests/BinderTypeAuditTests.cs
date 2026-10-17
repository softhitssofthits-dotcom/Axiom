using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

/// <summary>
/// Постоянные регрессии аудита TYPE от 17 октября 2026 года: устойчивый обход
/// графа типов при сравнении записей и константные значения полей.
/// </summary>
public sealed class BinderTypeAuditTests
{
    [Fact]
    public void Bind_CyclicTypeComparedWithEqualityReportsCycleWithoutOverflow()
    {
        const string text = """
TYPE Node
 Child AS Node
END TYPE
DIM a AS Node
DIM b AS Node
PRINT a = b
""";

        var diagnostics = Bind(text);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTypeMember);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.TypeMismatch);
    }

    [Fact]
    public void Bind_MutuallyCyclicTypesComparedWithNotEqualReportsCycleWithoutOverflow()
    {
        const string text = """
TYPE A
 Child AS B
END TYPE
TYPE B
 Child AS A
END TYPE
DIM x AS A
DIM y AS A
PRINT x <> y
""";

        var diagnostics = Bind(text);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTypeMember);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.TypeMismatch);
    }

    [Fact]
    public void Bind_RepeatedRecordTypeGraphIsCheckedWithoutExponentialTraversal()
    {
        var text = BuildDiamondTypeChain(24) + "\nDIM x AS T23\nDIM y AS T23\nPRINT x = y\n";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var diagnostics = Bind(text);
        stopwatch.Stop();

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Повторно используемый граф типов проверялся {stopwatch.ElapsedMilliseconds} мс.");
    }

    [Theory]
    [InlineData("DOUBLE", "1", "1")]
    [InlineData("DOUBLE", "1.5", "1.5")]
    [InlineData("LONG", "1", "1")]
    [InlineData("DECIMAL", "1", "1")]
    [InlineData("INTEGER", "-1", "-1")]
    public void Bind_NumericFieldDefaultAcceptsCompatibleLiteral(string fieldType, string literal, string expectedText)
    {
        var text = $"TYPE A\n Value AS {fieldType} = {literal}\nEND TYPE\nDIM x AS A\nPRINT x.Value\n";

        var program = BindProgram(text, out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.Single(program.GlobalStatements.OfType<BoundVariableDeclarationStatement>());
        var type = Assert.IsType<UserDefinedTypeSymbol>(declaration.Variable.Type);
        var field = Assert.Single(type.Fields);
        Assert.True(field.HasDefaultValue);
        Assert.Equal(decimal.Parse(expectedText, System.Globalization.CultureInfo.InvariantCulture), Convert.ToDecimal(field.DefaultValue, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Bind_FieldDefaultRejectsComputedExpression()
    {
        const string text = """
TYPE A
 Value AS DOUBLE = 1 + 1
END TYPE
""";

        var diagnostics = Bind(text);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTypeFieldDefaultValue);
    }

    [Fact]
    public void Bind_TemporalFieldDefaultRemainsUnsupported()
    {
        const string text = """
TYPE A
 Value AS DATE = "2024-01-01"
END TYPE
""";

        var diagnostics = Bind(text);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTypeFieldDefaultValue);
    }

    private static List<Diagnostic> Bind(string text)
    {
        _ = BindProgram(text, out var diagnostics);
        return [.. diagnostics];
    }

    private static BoundProgram BindProgram(string text, out DiagnosticBag diagnostics)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        return Binder.Bind(syntax, out diagnostics, checkGraphicsModes: false);
    }

    private static string BuildDiamondTypeChain(int count)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("TYPE T0");
        builder.AppendLine(" V AS INTEGER");
        builder.AppendLine("END TYPE");

        for (var index = 1; index < count; index++)
        {
            builder.AppendLine($"TYPE T{index}");
            builder.AppendLine($" L AS T{index - 1}");
            builder.AppendLine($" R AS T{index - 1}");
            builder.AppendLine("END TYPE");
        }

        return builder.ToString();
    }
}
