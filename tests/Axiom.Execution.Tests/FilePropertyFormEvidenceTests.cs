using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class FilePropertyFormEvidenceTests
{
    [Theory]
    [InlineData("FilePath", "FILEPATH(f)", "STRING")]
    [InlineData("FileMode", "FILEMODE(f)", "STRING")]
    [InlineData("FilePos", "FILEPOS(f)", "LONG")]
    [InlineData("FileLength", "FILELENGTH(f)", "LONG")]
    [InlineData("SeekFile", "SEEKFILE(f, CLNG(0))", "BOOLEAN")]
    [InlineData("IsOpen", "ISOPEN(f)", "BOOLEAN")]
    [InlineData("Eof", "EOF(f)", "BOOLEAN")]
    [InlineData("LineNumber", "LINENUMBER(f)", "LONG")]
    [InlineData("Rewind", "REWIND(f)", "BOOLEAN")]
    [InlineData("Flush", "FLUSH(f)", "BOOLEAN")]
    [InlineData("ReadToEnd", "READTOEND(f)", "STRING")]
    [InlineData("ReadLine", "READLINE(f)", "STRING")]
    [InlineData("SkipLine", "SKIPLINE(f)", "BOOLEAN")]
    [InlineData("Write", "WRITE(f, \"😀\")", "BOOLEAN")]
    [InlineData("WriteLine", "WRITELINE(f, \"😀\")", "BOOLEAN")]
    [InlineData("NewLine", "NEWLINE(f)", "BOOLEAN")]
    public void RegisteredIdentifierAndResultTypeAreExact(string id, string call, string type)
    {
        var syntax = Parser.Parse("DIM f AS FILE\nPRINT " + call, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last());
        var bound = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(bound.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type, bound.Type.Name.ToUpperInvariant());
    }

    [Theory]
    [InlineData("FILEPATH(TRUE)")]
    [InlineData("FILEMODE(1)")]
    [InlineData("FILEPOS(\"путь\")")]
    [InlineData("FILELENGTH(TRUE)")]
    [InlineData("SEEKFILE(TRUE, 0)")]
    [InlineData("SEEKFILE(f, TRUE)")]
    [InlineData("SEEKFILE(f, \"позиция\")")]
    [InlineData("ISOPEN(TRUE)")]
    [InlineData("EOF(TRUE)")]
    [InlineData("LINENUMBER(TRUE)")]
    [InlineData("REWIND(TRUE)")]
    [InlineData("FLUSH(TRUE)")]
    [InlineData("READTOEND(TRUE)")]
    [InlineData("READLINE(TRUE)")]
    [InlineData("SKIPLINE(TRUE)")]
    [InlineData("WRITE(TRUE, \"😀\")")]
    [InlineData("WRITELINE(TRUE, \"😀\")")]
    [InlineData("NEWLINE(TRUE)")]
    [InlineData("WRITE(f, TRUE)")]
    [InlineData("WRITELINE(f, TRUE)")]
    public void WrongArgumentsFailSemanticallyAtSameLocation(string call)
    {
        var source = "DIM f AS FILE\nPRINT " + call;
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.Equal(ExecutionStatus.SemanticFailure, first.Status);
        Assert.Equal(ExecutionStatus.SemanticFailure, second.Status);
        Assert.NotEmpty(first.Diagnostics);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Span)));
    }
}
