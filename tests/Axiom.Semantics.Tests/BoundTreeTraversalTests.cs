using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class BoundTreeTraversalTests
{
    // Один вызов в каждом контейнере, который легко потерять при отдельном
    // обходе: инициализатор записи, элемент PRINT, часть интерполяции и
    // селектор CASE.
    private const string Source = """
        TYPE Box
            value AS INTEGER
        END TYPE
        FUNCTION ComputeOne() AS INTEGER
            RETURN 1
        END FUNCTION
        FUNCTION ComputeTwo() AS INTEGER
            RETURN 2
        END FUNCTION
        FUNCTION ComputeThree() AS INTEGER
            RETURN 3
        END FUNCTION
        DIM item AS Box = Box(value := ComputeOne())
        PRINT ComputeTwo()
        PRINT $"итог {ComputeTwo()}"
        SELECT CASE ComputeThree()
            CASE 3
                PRINT "три"
        END SELECT
        """;

    [Fact]
    public void DescendantsVisitEachObjectOnceAndReachEveryContainer()
    {
        var tree = Parser.Parse(Source, out _, "main.axm");
        var program = Binder.Bind(tree, out _, checkGraphicsModes: false);

        var nodes = BoundTreeTraversal.Descendants(
            program.GlobalStatements.Cast<object>().Concat(program.Routines)).ToArray();

        Assert.Equal(nodes.Length, nodes.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Contains(nodes, node => node is BoundRecordLiteralFieldInitializer);
        Assert.Contains(nodes, node => node is BoundPrintItem);
        Assert.Contains(nodes, node => node is BoundInterpolatedStringPart);
        Assert.Contains(nodes, node => node is BoundCaseValueSelector);

        var calls = nodes.OfType<BoundCallExpression>().Select(call => (call.Target as BoundFunctionExpression)?.Function.Name).ToArray();
        Assert.Equal(4, calls.Length);
        Assert.Single(calls, name => name == "ComputeOne");
        Assert.Equal(2, calls.Count(name => name == "ComputeTwo"));
        Assert.Single(calls, name => name == "ComputeThree");
    }

    [Fact]
    public void DescendantsYieldParentBeforeChildren()
    {
        var tree = Parser.Parse(Source, out _, "main.axm");
        var program = Binder.Bind(tree, out _, checkGraphicsModes: false);

        var nodes = BoundTreeTraversal.Descendants(
            program.GlobalStatements.Cast<object>().Concat(program.Routines)).ToArray();

        var statement = Assert.Single(nodes.OfType<BoundSelectCaseStatement>());
        var selector = Assert.Single(nodes.OfType<BoundCaseValueSelector>());
        Assert.True(Array.IndexOf(nodes, statement) < Array.IndexOf(nodes, selector));
        Assert.True(Array.IndexOf(nodes, selector) < Array.IndexOf(nodes, selector.Expression));
    }
}
