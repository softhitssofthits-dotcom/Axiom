using Axiom.Bytecode;
using Axiom.Execution;

namespace Axiom.Execution.Tests;

/// <summary>
/// Начальные значения временных полей TYPE (DTA-011). Поле получает типизированное
/// значение из канонического текста при объявлении, поэтому недопустимая дата не
/// попадает в программу, а потеря компонента отклоняется общим договором.
/// </summary>
public sealed class TemporalTypeFieldDefaultContractTests
{
    [Fact]
    public void TemporalFieldDefaultsAreTypedAndReadable()
    {
        const string source = """
TYPE Event
    Day AS DATE = "2024-02-29"
    Clock AS TIME = "12:34:56.125"
    Moment AS DATETIME = "2024-02-29 12:34:56.125"
END TYPE

DIM item AS Event
PRINT item.Day
PRINT item.Clock
PRINT item.Moment
PRINT YEAR(item.Day)
PRINT HOUR(item.Moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(
            ["2024-02-29", "12:34:56.125", "2024-02-29 12:34:56.125", "2024", "12"],
            result.Output);
    }

    [Fact]
    public void TemporalFieldDefaultSurvivesRecordCopy()
    {
        const string source = """
TYPE Event
    Day AS DATE = "2024-02-29"
    Moment AS DATETIME = "2024-02-29 12:34:56"
END TYPE

DIM first AS Event
DIM second AS Event = first
second.Day = "2024-03-01"
PRINT first.Day
PRINT second.Day
PRINT second.Moment
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29", "2024-03-01", "2024-02-29 12:34:56"], result.Output);
    }

    [Fact]
    public void DefaultValueTakesPartInArithmeticAndComparison()
    {
        const string source = """
TYPE Event
    Moment AS DATETIME = "2024-02-29 12:34:56"
END TYPE

DIM item AS Event
PRINT DATEADD("h", 1, item.Moment)
PRINT item.Moment = DATETIMEVALUE("2024-02-29T12:34:56")
PRINT DATEPART("m", item.Moment)
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2024-02-29 13:34:56", "TRUE", "2"], result.Output);
    }

    [Fact]
    public void InvalidCalendarTextIsRejectedAtDeclaration()
    {
        const string source = """
TYPE Event
    Day AS DATE = "2024-02-30"
END TYPE
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, static d => d.Code == "MB2027");
    }

    [Fact]
    public void LossyTextIsRejectedByCommonContract()
    {
        const string source = """
TYPE Event
    Day AS DATE = "2024-02-29 12:34:56"
END TYPE
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB2003", diagnostic.Code);
        Assert.Contains("truncated", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CallsAndVariablesAreStillRejected()
    {
        foreach (var initializer in new[] { "DATESERIAL(2024, 2, 29)", "DATETIMEVALUE(\"2024-02-29T12:34:56\")" })
        {
            var source = $$"""
                TYPE Event
                    Value AS DATETIME = {{initializer}}
                END TYPE
                """;

            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Diagnostics);
        }
    }

    [Fact]
    public void TemporalDefaultsSurviveBytecodeJsonRoundTrip()
    {
        const string source = """
TYPE Event
    Day AS DATE = "2024-02-29"
    Clock AS TIME = "12:34:56.125"
    Moment AS DATETIME = "2024-02-29 12:34:56.125"
END TYPE

DIM item AS Event
PRINT item.Day
PRINT item.Clock
PRINT item.Moment
""";
        var directory = Path.Combine(".artifacts", "temporal-type-default", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var snapshot = Path.Combine(directory, "module.json");
        try
        {
            var compiled = new AxiomExecutionPipeline().Compile(new CompilationRequest(source, bytecodeSnapshotPath: snapshot));
            Assert.True(compiled.Succeeded, string.Join(" | ", compiled.Diagnostics.Select(d => d.Message)));
            Assert.True(File.Exists(snapshot));

            var json = File.ReadAllText(snapshot);
            Assert.Contains("2024-02-29", json, StringComparison.Ordinal);
            Assert.Contains("12:34:56.125", json, StringComparison.Ordinal);

            // Значение записано каноническим текстом, поэтому артефакт читаем и
            // сохраняет точность; восстановление проверяется по артефакту типа.
            var artifact = BytecodeModuleSerializer.ToArtifact(compiled.CompiledProgram!.BytecodeModule!);
            var definition = artifact.TypeDefinitions.Single(
                t => t.Name.Equals("Event", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("2024-02-29", definition.Fields[0].DefaultValue);
            Assert.Equal("12:34:56.125", definition.Fields[1].DefaultValue);
            Assert.Equal("2024-02-29 12:34:56.125", definition.Fields[2].DefaultValue);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void OtherFieldTypesKeepTheirDefaults()
    {
        const string source = """
TYPE Sample
    Count AS INTEGER = 7
    Ratio AS DOUBLE = 1.5
    Name AS STRING = "пример"
    Flag AS BOOLEAN = TRUE
END TYPE

DIM item AS Sample
PRINT item.Count
PRINT item.Ratio
PRINT item.Name
PRINT item.Flag
""";

        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["7", "1.5", "пример", "TRUE"], result.Output);
    }
}
