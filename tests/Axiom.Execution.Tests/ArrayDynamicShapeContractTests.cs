namespace Axiom.Execution.Tests;

public sealed class ArrayDynamicShapeContractTests
{
    public static IEnumerable<object[]> Operations()
    {
        foreach (var axis in new[] { 1, 2 })
        {
            yield return new object[] { axis, "ARRAYPUSH(a[], values[], D)", "0", new[] { "11", "12", "21", "22", "31", "32" } };
            yield return new object[] { axis, "ARRAYINSERT(a[], -1, values[], D)", "0", new[] { "11", "12", "31", "32", "21", "22" } };
            yield return new object[] { axis, "removed[] = ARRAYPOP(a[], D)\nPRINT LBOUND(removed[])\nPRINT UBOUND(removed[])\nPRINT removed[7]\nPRINT removed[8]", "-2", new[] { "7", "8", "21", "22", "11", "12" } };
            yield return new object[] { axis, "removed[] = ARRAYREMOVE(a[], -2, D)\nPRINT LBOUND(removed[])\nPRINT UBOUND(removed[])\nPRINT removed[7]\nPRINT removed[8]", "-2", new[] { "7", "8", "11", "12", "21", "22" } };
        }
    }

    private static string Setup(int axis)
    {
        var bounds = axis == 1 ? "-2 TO -1, 7 TO 8" : "7 TO 8, -2 TO -1";
        var first = axis == 1 ? "-2, 7" : "7, -2";
        var second = axis == 1 ? "-2, 8" : "8, -2";
        var third = axis == 1 ? "-1, 7" : "7, -1";
        var fourth = axis == 1 ? "-1, 8" : "8, -1";
        return $"DIM a[{bounds}] AS INTEGER\nDIM values[7 TO 8] AS INTEGER\nDIM removed[] AS INTEGER\na[{first}] = 11\na[{second}] = 12\na[{third}] = 21\na[{fourth}] = 22\nvalues[7] = 31\nvalues[8] = 32\n";
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public void DynamicOperationsPreserveOtherBoundsAndUseIndependentExpectedOrder(int axis, string operation, string upper, string[] expectedValues)
    {
        var indices = axis == 1 ? "i, j" : "j, i";
        var body = operation.Replace("D)", axis + ")", StringComparison.Ordinal);
        var source = Setup(axis) + body + $"\nPRINT LBOUND(a[], {axis})\nPRINT UBOUND(a[], {axis})\nPRINT LBOUND(a[], {3-axis})\nPRINT UBOUND(a[], {3-axis})\nFOR i = LBOUND(a[], {axis}) TO UBOUND(a[], {axis})\nFOR j = 7 TO 8\nPRINT a[{indices}]\nNEXT j\nNEXT i";
        var hasRemoved = operation.StartsWith("removed", StringComparison.Ordinal);
        var expected = (hasRemoved ? expectedValues.Take(4) : Array.Empty<string>()).Concat(new[] { "-2", upper, "7", "8" }).Concat(hasRemoved ? expectedValues.Skip(4) : expectedValues).ToArray();

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }

    [Theory]
    [InlineData("ARRAYPUSH(a[], wrong[], 1)")]
    [InlineData("ARRAYINSERT(a[], -1, wrong[], 1)")]
    [InlineData("ARRAYINSERT(a[], -3, values[], 1)")]
    [InlineData("removed[] = ARRAYREMOVE(a[], 0, 1)")]
    public void ShapeOrIndexRejectionIsCatchableAndLeavesAllElementsUnchanged(string operation)
    {
        var setup = Setup(1) + "DIM wrong[8 TO 9] AS INTEGER\n";
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(setup + operation));
        Assert.False(second.Succeeded);
        Assert.Equal("MB3007", Assert.Single(second.Diagnostics).Code);

        {
            var source = setup + "TRY\n" + operation + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT LBOUND(a[], 1)\nPRINT UBOUND(a[], 1)\nPRINT ARRAYLENGTH(a[])\nPRINT a[-2, 7]\nPRINT a[-2, 8]\nPRINT a[-1, 7]\nPRINT a[-1, 8]";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "MB3007", "-2", "-1", "4", "11", "12", "21", "22" }, run.Output);
        }
    }

    [Theory]
    [InlineData("ARRAYPUSH(a[], a[-2], 1)", "0", "11", "12", "21", "22", "11", "12")]
    [InlineData("ARRAYINSERT(a[], -2, a[-1], 1)", "0", "21", "22", "11", "12", "21", "22")]
    public void SameArrayViewIsSafeAsInsertionValue(string operation, string upper, params string[] expected)
    {
        var source = Setup(1) + operation + "\nFOR i = -2 TO " + upper + "\nFOR j = 7 TO 8\nPRINT a[i, j]\nNEXT j\nNEXT i";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ExpansionPastMaximumIndexIsCatchableBeforeMutation(int axis)
    {
        var bounds = axis == 1 ? "2147483647 TO 2147483647, 7 TO 8" : "7 TO 8, 2147483647 TO 2147483647";
        var first = axis == 1 ? "2147483647, 7" : "7, 2147483647";
        var last = axis == 1 ? "2147483647, 8" : "8, 2147483647";
        foreach (var statement in new[] { $"ARRAYPUSH(a[], values[], {axis})", $"ARRAYINSERT(a[], 2147483647, values[], {axis})" })
        {
            var source = $"DIM a[{bounds}] AS INTEGER\nDIM values[7 TO 8] AS INTEGER\na[{first}] = 42\na[{last}] = 43\nTRY\n{statement}\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a[])\nPRINT UBOUND(a[], {axis})\nPRINT a[{first}]\nPRINT a[{last}]";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "MB3007", "2", "2147483647", "42", "43" }, run.Output);
        }
    }

    [Theory]
    [InlineData("ARRAYPUSH(a[-2], 99)")]
    [InlineData("PRINT ARRAYPOP(a[-2])")]
    [InlineData("ARRAYINSERT(a[-2], 7, 99)")]
    [InlineData("PRINT ARRAYREMOVE(a[-2], 7)")]
    public void ResizingAViewIsRejectedWithoutChangingItsOwner(string statement)
    {

        {
            var source = Setup(1) + "TRY\n" + statement + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ARRAYLENGTH(a[])\nPRINT a[-2, 7]\nPRINT a[-2, 8]\nPRINT a[-1, 7]\nPRINT a[-1, 8]";
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { "MB3007", "4", "11", "12", "21", "22" }, run.Output);
        }
    }

}
