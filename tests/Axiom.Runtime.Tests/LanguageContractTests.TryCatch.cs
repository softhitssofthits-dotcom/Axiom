using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_TryFinallyRunsBeforeUnhandledFailureEscapes()
    {
        const string text = """
TRY
    PRINT 10 \ 0
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Equal(["cleanup"], host.Output);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3001");
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_TryCatchHandlesRuntimeFailureAndExposesErrorFields()
    {
        const string text = """
TRY
    PRINT 10 \ 0
    PRINT "unreachable"
CATCH err
    PRINT err.Code
    PRINT err.Line
    PRINT err.Column
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["MB3001", "2", "14", "after"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsAfterNormalBody()
    {
        const string text = """
TRY
    PRINT "body"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["body", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_TryCatchFinallyRunsFinallyAfterHandledFailure()
    {
        const string text = """
TRY
    PRINT 10 \ 0
CATCH err
    PRINT "caught"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["caught", "cleanup"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementCanBeCaughtByTryCatch()
    {
        const string text = """
TRY
    ERROR "APP001", "Manual failure"
CATCH err
    PRINT err.Code
    PRINT err.Message
    PRINT err.Line
FINALLY
    PRINT "cleanup"
END TRY

PRINT "after"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["APP001", "Manual failure", "2", "cleanup", "after"], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementUsesDefaultMessage()
    {
        const string text = """
TRY
    ERROR "APP002"
CATCH err
    PRINT err.Message
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Пользовательская ошибка APP002."], host.Output);
    }

    [Fact]
    public void Execute_ErrorStatementRethrowsCaughtRuntimeError()
    {
        const string text = """
TRY
    TRY
        ERROR "APP003", "Original failure"
    CATCH err
        PRINT "inner"
        ERROR err
    END TRY
CATCH outerErr
    PRINT outerErr.Code
    PRINT outerErr.Message
    PRINT outerErr.Line
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["inner", "APP003", "Original failure", "3"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsWhenCatchBodyRethrows()
    {
        const string text = """
TRY
    TRY
        ERROR "APP010", "Original failure"
    CATCH inner
        ERROR inner
    FINALLY
        PRINT "inner-cleanup"
    END TRY
CATCH outer
    PRINT outer.Code
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["inner-cleanup", "APP010"], host.Output);
    }

    [Fact]
    public void Execute_TryFinallyRunsWhenCatchBodyRaisesNewError()
    {
        const string text = """
TRY
    ERROR "APP011"
CATCH err
    ERROR "APP012", "Replacement failure"
FINALLY
    PRINT "cleanup"
END TRY
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        Assert.Equal(["cleanup"], host.Output);
        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "APP012");
        Assert.Contains("Replacement failure", diagnostic.Message, StringComparison.Ordinal);
    }
}
