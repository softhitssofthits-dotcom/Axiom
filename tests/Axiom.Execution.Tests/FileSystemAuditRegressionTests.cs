namespace Axiom.Execution.Tests;

public sealed class FileSystemAuditRegressionTests
{
    [Theory]
    [InlineData("samples/functions/getfiles/path_mask_rejected.axm", "MB3007\nMB3007")]
    [InlineData("samples/functions/skipline/large_line.axm", "TRUE\nСледующая строка")]
    [InlineData("samples/functions/appendfile/open_append_series.axm", "TRUE\nTRUE")]
    public void AcceptanceSamplesHaveIndependentOutput(string path, string expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(TestProgramLoader.LoadText(path)));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected.Split('\n'), result.Output);
    }

    private static string Root()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "filesystem-regressions", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        return root;
    }

    [Theory]
    [InlineData("GETFILES", "nested\\*.txt")]
    [InlineData("GETFILES", "..\\*.txt")]
    [InlineData("GETDIRS", "nested\\*")]
    [InlineData("GETDIRS", "../*")]
    public void ListingRejectsPathInsideMask(string operation, string mask)
    {
        var root = Root();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM entries[] AS STRING
                TRY
                    entries[] = {{operation}}("{{root}}", "{{mask}}")
                    PRINT "accepted"
                CATCH e
                    PRINT e.Code
                END TRY
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Equal(["MB3007"], result.Output);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("COMBINEPATH(CHR(0), \"b\")")]
    [InlineData("COMBINEPATH(\"a\", CHR(0))")]
    [InlineData("ISABSOLUTEPATH(\"C:\\\" & CHR(0))")]
    public void PathFunctionsRejectNullCharacter(string expression)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{expression}}
            CATCH e
                PRINT e.Code
            END TRY
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["MB3007"], result.Output);
    }

    [Theory]
    [InlineData("COPYFILE")]
    [InlineData("MOVEFILE")]
    [InlineData("COPYDIR")]
    [InlineData("MOVEDIR")]
    public void InvalidSourceReportsSourceArgument(string operation)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{operation}}(CHR(0), "unused")
            CATCH e
                PRINT INSTR(e.Message, "source") > 0
                PRINT e.Code
            END TRY
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["TRUE", "MB3007"], result.Output);
    }

    [Theory]
    [InlineData("GETFILES")]
    [InlineData("GETDIRS")]
    public void InvalidListingPathIsNotReportedAsPattern(string operation)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            DIM entries[] AS STRING
            TRY
                entries[] = {{operation}}(CHR(0), "*")
            CATCH e
                PRINT INSTR(e.Message, "path") > 0
                PRINT INSTR(e.Message, "pattern") = 0
            END TRY
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["TRUE", "TRUE"], result.Output);
    }

    [Theory]
    [InlineData("READFILE(\"unused\", \"bad-encoding\")")]
    [InlineData("WRITEFILE(\"unused\", \"text\", \"bad-encoding\")")]
    [InlineData("APPENDFILE(\"unused\", \"text\", \"bad-encoding\")")]
    public void InvalidEncodingReportsEncodingArgument(string expression)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            TRY
                PRINT {{expression}}
            CATCH e
                PRINT INSTR(e.Message, "'encoding'") > 0
                PRINT e.Code
            END TRY
            """));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(["TRUE", "MB3007"], result.Output);
    }
}
