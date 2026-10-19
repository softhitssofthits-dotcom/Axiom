namespace Axiom.Execution.Tests;

public sealed class RoutineCaptureTransitivityTests
{
    [Fact]
    public void SiblingCallReceivesTransitiveCapture()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Outer() AS INTEGER
    DIM x AS INTEGER = 10
    FUNCTION PeerAlpha() AS INTEGER
        RETURN PeerBeta()
    END FUNCTION
    FUNCTION PeerBeta() AS INTEGER
        x = x + 1
        RETURN x
    END FUNCTION
    RETURN PeerAlpha()
END FUNCTION
PRINT Outer()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["11"], result.Output);
    }

    [Fact]
    public void ReturnedClosureReceivesTransitiveCaptureThroughLoadedFunction()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Outer() AS FUNCTION() AS INTEGER
    DIM x AS INTEGER = 10
    FUNCTION Middle() AS FUNCTION() AS INTEGER
        FUNCTION Deep() AS INTEGER
            RETURN x
        END FUNCTION
        RETURN Deep
    END FUNCTION
    RETURN Middle()
END FUNCTION
DIM f AS FUNCTION() AS INTEGER
f = Outer()
PRINT f()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["10"], result.Output);
    }

    [Fact]
    public void TransitiveCaptureOfParameterSurvivesCallerReturn()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Outer(seed AS INTEGER) AS FUNCTION() AS INTEGER
    FUNCTION Alpha() AS FUNCTION() AS INTEGER
        RETURN Beta
    END FUNCTION
    FUNCTION Beta() AS INTEGER
        seed = seed + 1
        RETURN seed
    END FUNCTION
    RETURN Alpha()
END FUNCTION
DIM f AS FUNCTION() AS INTEGER
f = Outer(40)
PRINT f()
PRINT f()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["41", "42"], result.Output);
    }

    [Fact]
    public void MutualRecursionWithTransitiveCaptureTerminates()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Outer() AS INTEGER
    DIM total AS INTEGER = 0
    FUNCTION EvenStep(n AS INTEGER) AS BOOLEAN
        IF n = 0 THEN RETURN TRUE
        total = total + 1
        RETURN OddStep(n - 1)
    END FUNCTION
    FUNCTION OddStep(n AS INTEGER) AS BOOLEAN
        IF n = 0 THEN RETURN FALSE
        total = total + 1
        RETURN EvenStep(n - 1)
    END FUNCTION
    DIM answer AS BOOLEAN = EvenStep(4)
    PRINT answer
    RETURN total
END FUNCTION
PRINT Outer()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["TRUE", "4"], result.Output);
    }

    [Fact]
    public void TransitiveCaptureDoesNotMergeIndependentFactoryStates()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
FUNCTION Make(seed AS INTEGER) AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = seed
    FUNCTION Alpha() AS INTEGER
        RETURN Beta()
    END FUNCTION
    FUNCTION Beta() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION
    RETURN Alpha
END FUNCTION
DIM first AS FUNCTION() AS INTEGER
DIM second AS FUNCTION() AS INTEGER
first = Make(10)
second = Make(100)
PRINT first()
PRINT second()
PRINT first()
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["11", "101", "12"], result.Output);
    }
}
