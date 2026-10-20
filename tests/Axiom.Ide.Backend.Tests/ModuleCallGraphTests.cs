using Axiom.DesktopHost;
using Axiom.Execution;

namespace Axiom.Ide.Backend.Tests;

public sealed class ModuleCallGraphTests
{
    [Fact]
    public void ImportedCallsKeepDocumentIdentityAndSeparatePrivateNames()
    {
        var graph = EditorCallGraph.Analyse("IMPORT alpha\nIMPORT beta\nPRINT AlphaValue() + BetaValue()", "main.axm",
        [new SourceDocument("alpha.axm", "PRINT Hidden()\nPUBLIC FUNCTION AlphaValue() AS INTEGER\nRETURN Hidden()\nEND FUNCTION\nFUNCTION Hidden() AS INTEGER\nRETURN 1\nEND FUNCTION"),
         new SourceDocument("beta.axm", "PRINT Hidden()\nPUBLIC FUNCTION BetaValue() AS INTEGER\nRETURN Hidden()\nEND FUNCTION\nFUNCTION Hidden() AS INTEGER\nRETURN 2\nEND FUNCTION")]);
        Assert.True(graph.Complete);
        var hidden = graph.Routines.Where(r => r.Name == "Hidden").ToArray();
        Assert.Equal(2, hidden.Length);
        Assert.NotEqual(hidden[0].Id, hidden[1].Id);
        Assert.All(hidden, routine => Assert.All(graph.Calls.Where(c => c.Callee == routine.Id),
            call => Assert.Equal(routine.SourceFilePath, call.SourceFilePath)));
        Assert.Equal(3, graph.Routines.Count(r => r.Kind == "main"));
        Assert.Equal(2, graph.Calls.Count(c => c.Caller == "main"));
        Assert.Equal(3, graph.SourceDocuments.Length);
    }

    [Fact]
    public void UnresolvedImportIsIncompleteWithoutInventingTargets()
    {
        var graph = EditorCallGraph.Analyse("IMPORT missing\nPRINT Unknown()", "main.axm", []);
        Assert.False(graph.Complete);
        Assert.Empty(graph.Calls);
    }

    [Fact]
    public void SymbolIdsJoinImportedUsesAndKeepPrivateDeclarationsSeparate()
    {
        var graph = EditorCallGraph.Analyse("IMPORT alpha\nIMPORT beta\nPRINT total + AlphaValue() + BetaValue()", "main.axm",
        [new SourceDocument("alpha.axm", "PUBLIC DIM total AS INTEGER = 2\nPUBLIC FUNCTION AlphaValue() AS INTEGER\nRETURN hidden\nEND FUNCTION\nDIM hidden AS INTEGER = 1"),
         new SourceDocument("beta.axm", "PUBLIC FUNCTION BetaValue() AS INTEGER\nRETURN hidden\nEND FUNCTION\nDIM hidden AS INTEGER = 2")]);
        Assert.True(graph.Complete);
        var total = Assert.Single(graph.Symbols, s => s.Name == "total" && s.IsDeclaration);
        Assert.Contains(graph.Symbols, s => s.SymbolId == total.SymbolId && !s.IsDeclaration && s.SourceFilePath == "main.axm");
        var hidden = graph.Symbols.Where(s => s.Name == "hidden" && s.IsDeclaration).ToArray();
        Assert.Equal(2, hidden.Length);
        Assert.NotEqual(hidden[0].SymbolId, hidden[1].SymbolId);
        Assert.All(hidden, declaration => Assert.All(graph.Symbols.Where(s => s.SymbolId == declaration.SymbolId),
            site => Assert.Equal(declaration.SourceFilePath, site.SourceFilePath)));
    }

    [Fact]
    public void SharedTraversalFindsCallsInNestedContainersAndHandlerEdges()
    {
        const string source = """
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
            SUB HandleLeft()
                PRINT "нажата клавиша"
            END SUB
            DIM item AS Box = Box(value := ComputeOne())
            PRINT ComputeTwo()
            PRINT $"итог {ComputeTwo()}"
            SELECT CASE ComputeThree()
                CASE 3
                    PRINT item.value
            END SELECT
            ON KEY("LEFT"), HandleLeft
            """;
        var graph = EditorCallGraph.Analyse(source, "main.axm");
        Assert.True(graph.Complete);
        var first = Assert.Single(graph.Routines, r => r.Name == "ComputeOne");
        var second = Assert.Single(graph.Routines, r => r.Name == "ComputeTwo");
        var third = Assert.Single(graph.Routines, r => r.Name == "ComputeThree");
        var handler = Assert.Single(graph.Routines, r => r.Name == "HandleLeft");
        Assert.Single(graph.Calls, c => c.Callee == first.Id && c.Kind == "call");
        Assert.Equal(2, graph.Calls.Count(c => c.Callee == second.Id && c.Kind == "call"));
        Assert.Single(graph.Calls, c => c.Callee == third.Id && c.Kind == "call");
        // Обработчик описан отдельным ребром и не повторяется как ссылка.
        Assert.Single(graph.Calls, c => c.Callee == handler.Id && c.Kind == "handler");
        Assert.DoesNotContain(graph.Calls, c => c.Kind == "reference");
    }
}
