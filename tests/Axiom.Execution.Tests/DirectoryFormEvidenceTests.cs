using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class DirectoryFormEvidenceTests
{
    [Theory]
    [InlineData("DIRSIZE(\"путь\")", "DirSize", "LONG")]
    [InlineData("CURRENTDIR()", "CurrentDir", "STRING")]
    [InlineData("CHANGEDIR(\"путь\")", "ChangeDir", "BOOLEAN")]
    public void SizeAndCurrentDirectoryHaveExactTypes(string call, string id, string type)
    {
        var syntax = Parser.Parse("PRINT " + call, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var bound = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(bound.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type, bound.Type.Name.ToUpperInvariant());
    }

    [Theory]
    [InlineData("DIREXISTS(\"путь\")", "DirExists")]
    [InlineData("PATHEXISTS(\"путь\")", "PathExists")]
    [InlineData("MAKEDIR(\"путь\")", "MakeDir")]
    [InlineData("DELETEDIR(\"путь\")", "DeleteDir")]
    [InlineData("DELETEDIRRECURSIVE(\"путь\")", "DeleteDirRecursive")]
    [InlineData("COPYDIR(\"источник\", \"назначение\")", "CopyDir")]
    [InlineData("COPYDIR(\"источник\", \"назначение\", TRUE)", "CopyDirOverwrite")]
    [InlineData("MOVEDIR(\"источник\", \"назначение\")", "MoveDir")]
    public void ExactRegisteredFormReturnsBoolean(string call, string id)
    {
        var syntax = Parser.Parse("PRINT " + call, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var bound = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(bound.Target).Function.BuiltinId?.ToString());
        Assert.Equal(TypeSymbol.Boolean, bound.Type);
    }

    [Theory]
    [InlineData("DIREXISTS(TRUE)")]
    [InlineData("PATHEXISTS(TRUE)")]
    [InlineData("MAKEDIR(TRUE)")]
    [InlineData("DELETEDIR(TRUE)")]
    [InlineData("DELETEDIRRECURSIVE(TRUE)")]
    [InlineData("COPYDIR(TRUE, \"путь\")")]
    [InlineData("COPYDIR(\"путь\", TRUE)")]
    [InlineData("COPYDIR(\"путь\", \"путь\", \"флаг\")")]
    [InlineData("MOVEDIR(TRUE, \"путь\")")]
    [InlineData("MOVEDIR(\"путь\", TRUE)")]
    [InlineData("DIRSIZE(TRUE)")]
    [InlineData("CHANGEDIR(TRUE)")]
    public void WrongArgumentTypesFailBeforeExecution(string call)
    {
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest("PRINT " + call, mode: ExecutionMode.Bytecode));
        Assert.Equal(ExecutionStatus.SemanticFailure, first.Status);
        Assert.Equal(ExecutionStatus.SemanticFailure, second.Status);
        Assert.NotEmpty(first.Diagnostics);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Span)));
    }
}
