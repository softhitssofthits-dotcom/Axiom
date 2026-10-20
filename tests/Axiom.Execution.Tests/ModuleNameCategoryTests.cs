namespace Axiom.Execution.Tests;
public sealed class ModuleNameCategoryTests
{
    [Fact]
    public void VariableAndRoutineExportsUseSeparateCategories()
    {
        var documents = new SourceDocument[] {
            new("values.axm", "PUBLIC DIM Token AS INTEGER=5"),
            new("routines.axm", "PUBLIC FUNCTION Token() AS INTEGER\nRETURN 7\nEND FUNCTION") };
        var pipeline=new AxiomExecutionPipeline();
        var value=pipeline.Run(new ExecutionRequest("IMPORT values\nIMPORT routines\nPRINT Token",sourceName:"main.axm",additionalDocuments:documents));
        Assert.Equal(ExecutionStatus.Success,value.Status);
        Assert.Equal(["5"],value.Output);
        var invocation=pipeline.Compile(new CompilationRequest("IMPORT values\nIMPORT routines\nPRINT Token()",sourceName:"main.axm",additionalDocuments:documents));
        Assert.Contains(invocation.Diagnostics,d=>d.Code=="MB2020");
        Assert.DoesNotContain(invocation.Diagnostics,d=>d.Code=="MB2054");
    }

    [Theory]
    [InlineData("PUBLIC DIM Token AS INTEGER=1","PRINT Token")]
    [InlineData("PUBLIC TYPE Token\nItem AS INTEGER\nEND TYPE","DIM item AS Token")]
    public void TwoExportsInTheSameCategoryAreAmbiguous(string declaration,string use)
    {
        var compilation=new AxiomExecutionPipeline().Compile(new CompilationRequest("IMPORT first\nIMPORT second\n"+use,sourceName:"main.axm",additionalDocuments:[
            new SourceDocument("first.axm",declaration),new SourceDocument("second.axm",declaration)]));
        Assert.Contains(compilation.Diagnostics,d=>d.Code=="MB2054");
    }
}
