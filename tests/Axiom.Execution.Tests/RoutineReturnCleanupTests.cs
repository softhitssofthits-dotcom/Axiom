namespace Axiom.Execution.Tests;

public sealed class RoutineReturnCleanupTests
{
    [Fact]
    public void ReturnRunsEveryNestedFinallyExactlyOnce()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
DIM cleanup AS INTEGER = 0
FUNCTION Work() AS INTEGER
    TRY
        TRY
            RETURN 7
        FINALLY
            PRINT "inner"
            cleanup = cleanup + 1
        END TRY
    FINALLY
        PRINT "outer"
        cleanup = cleanup + 10
    END TRY
END FUNCTION
PRINT Work()
PRINT cleanup
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["inner", "outer", "7", "11"], result.Output);
    }

    [Fact]
    public void ReturnSkipsInnerCatchOnlyHandlerAndRunsOuterFinally()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    TRY
        TRY
            RETURN 8
        CATCH e
            PRINT "caught"
        END TRY
    FINALLY
        PRINT "outer"
    END TRY
END FUNCTION
PRINT Work()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["outer", "8"], result.Output);
    }

    [Fact]
    public void ReturnInsideCatchStillRunsItsFinally()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    TRY
        ERROR "EXPECTED", "failure"
    CATCH e
        RETURN 5
    FINALLY
        PRINT "finally"
    END TRY
END FUNCTION
PRINT Work()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["finally", "5"], result.Output);
    }

    [Fact]
    public void ReturnInsideFinallyReplacesPendingValueWithoutRepeatingFinally()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    TRY
        RETURN 1
    FINALLY
        PRINT "finally"
        RETURN 2
    END TRY
END FUNCTION
PRINT Work()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["finally", "2"], result.Output);
    }

    [Fact]
    public void ReturnUnwindsThreeFinallyLevelsInOrder()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Work() AS INTEGER
    TRY
        TRY
            TRY
                RETURN 9
            FINALLY
                PRINT "third"
            END TRY
        FINALLY
            PRINT "second"
        END TRY
    FINALLY
        PRINT "first"
    END TRY
END FUNCTION
PRINT Work()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["third", "second", "first", "9"], result.Output);
    }

    [Fact]
    public void ReturnThroughFinallyStillClosesFileOpenedInTry()
    {
        var directory = Path.Combine(Path.GetTempPath(), "axiom-routine-finally-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "closed.txt");

        try
        {
            var source = $$"""
DIM handle AS FILE
FUNCTION Work() AS INTEGER
    TRY
        OPEN "{{path.Replace("\\", "\\\\")}}" FOR OUTPUT AS handle
        WRITE(handle, "line")
        RETURN 4
    FINALLY
        CLOSE handle
    END TRY
END FUNCTION
PRINT Work()
PRINT ISOPEN(handle)
""";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, resourceBaseDirectory: directory));

            Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal(["4", "FALSE"], result.Output);
            Assert.Equal(["line"], File.ReadAllLines(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
