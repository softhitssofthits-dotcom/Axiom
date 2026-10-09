using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class DriveFormEvidenceTests
{
    [Theory]
    [InlineData("FREESPACE", "FreeSpace", "LONG")]
    [InlineData("TOTALFREESPACE", "TotalFreeSpace", "LONG")]
    [InlineData("TOTALSPACE", "TotalSpace", "LONG")]
    [InlineData("DRIVENAME", "DriveName", "STRING")]
    [InlineData("DRIVEFORMAT", "DriveFormat", "STRING")]
    [InlineData("DRIVETYPE", "DriveType", "STRING")]
    public void RegisteredFormAndWrongPathTypeHaveExactContracts(string name, string id, string type)
    {
        var syntax = Parser.Parse($"PRINT {name}(\"путь\")", out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type, call.Type.Name.ToUpperInvariant());
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {name}(TRUE)", mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {name}(TRUE)", mode: ExecutionMode.Bytecode));
        Assert.Equal(ExecutionStatus.SemanticFailure, first.Status);
        Assert.Equal(ExecutionStatus.SemanticFailure, second.Status);
        Assert.NotEmpty(first.Diagnostics);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Span)));
    }
}
