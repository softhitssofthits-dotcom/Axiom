using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ReadAllLinesEncodingFormEvidenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Utf16LineSeparatorsRespectUnitsAndPreserveBytes(bool bigEndian)
    {
        // U+0D41 содержит байт CR, но сам не является переводом строки.
        byte[] bytes = bigEndian
            ? [0xFE, 0xFF, 0x0D, 0x41, 0, 13, 0, 13, 0, 10, 0xD8, 0x3D, 0xDE, 0, 0, 10]
            : [0xFF, 0xFE, 0x41, 0x0D, 13, 0, 13, 0, 10, 0, 0x3D, 0xD8, 0, 0xDE, 10, 0];
        var root = Path.GetFullPath(Path.Combine(".artifacts", "read-lines-encoding-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "строки😀.txt");
        File.WriteAllBytes(path, bytes);
        try
        {
            var source = $$"""
                DIM lines() AS STRING
                lines = READALLLINES("{{path}}", "{{(bigEndian ? "UTF-16BE" : "UTF-16LE")}}")
                PRINT ARRAYLENGTH(lines)
                PRINT lines(1) = CHRW(3393)
                PRINT lines(2) = ""
                PRINT lines(3) = "😀"
                PRINT LBOUND(lines)
                PRINT UBOUND(lines)
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["3", "TRUE", "TRUE", "TRUE", "1", "3"], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }

    [Theory]
    [InlineData("READALLLINES(\"путь\")", "ReadAllLines")]
    [InlineData("READALLLINES(\"путь\", \"UTF-16LE\")", "ReadAllLinesEncoding")]
    public void ExactFormReturnsStringArray(string call, string id)
    {
        var syntax = Parser.Parse("PRINT ARRAYLENGTH(" + call + ")", out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var outer = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        var inner = Assert.IsType<BoundCallExpression>(Assert.Single(outer.Arguments));
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(inner.Target).Function.BuiltinId?.ToString());
        var array = Assert.IsType<ArrayTypeSymbol>(inner.Type);
        Assert.Equal(TypeSymbol.String, array.ElementType);
    }
}
