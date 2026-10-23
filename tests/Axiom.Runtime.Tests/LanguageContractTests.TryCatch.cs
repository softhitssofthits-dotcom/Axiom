using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public void Execute_CaughtSystemMessagesAreEnglishIndependentOfCulture(string culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        var previousUi = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(culture);
            System.Globalization.CultureInfo.CurrentUICulture = new(culture);
            var (_, host, result) = ExecuteProgram("""
                TRY
                    PRINT POW(-2, 0.5)
                CATCH problem
                    PRINT problem.Message
                END TRY
                TRY
                    PRINT 1 / 0
                CATCH division
                    PRINT division.Message
                END TRY
                """);
            Assert.True(result.Succeeded);
            Assert.Equal(["Function POW: argument 'value' cannot be used with the specified exponent.", "Division by zero."], host.Output);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
            System.Globalization.CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void Execute_EnglishArrayMessageKeepsIndicesAndBounds()
    {
        var (_, host, result) = ExecuteProgram("DIM a[1 TO 3] AS INTEGER\r\nTRY\r\n    PRINT a[5]\r\nCATCH err\r\n    PRINT err.Message\r\nEND TRY");
        Assert.True(result.Succeeded);
        Assert.Equal(["Indices (5) are outside the bounds of array 'a' (1..3)."], host.Output);
    }

    [Fact]
    public void Execute_EnglishGraphicsMessageKeepsOperationAndMode()
    {
        var (_, host, result) = ExecuteProgram("""
            TRY
                DRAWRECT 1, 2, 3, 4, RGB(255, 0, 0)
            CATCH err
                PRINT err.Message
            END TRY
            """);
        Assert.True(result.Succeeded);
        Assert.Equal(["DRAWRECT requires active CANVAS graphics mode."], host.Output);
    }

    [Theory]
    [InlineData("Деление на ноль.")]
    [InlineData("x' Деление на ноль.")]
    public void Execute_EnglishInputMessageKeepsTypeAndOriginalInput(string input)
    {
        var (_, host, result) = ExecuteProgram("""
            DIM number AS INTEGER
            TRY
                INPUT number
            CATCH err
                PRINT err.Message
            END TRY
            """, input);
        Assert.True(result.Succeeded);
        Assert.Contains($"Cannot convert input '{input}' to type INTEGER.", host.Output);
    }

    [Fact]
    public void Execute_EnglishInputMessageKeepsExpectedAndActualCounts()
    {
        var (_, host, result) = ExecuteProgram("""
            DIM first AS INTEGER, second AS INTEGER
            TRY
                INPUT first, second
            CATCH err
                PRINT err.Message
            END TRY
            """, "1");
        Assert.True(result.Succeeded);
        Assert.Contains("Expected 2 INPUT values, received 1.", host.Output);
    }

    [Fact]
    public void Execute_SystemMessageKeepsEnglishCallChainAndSurvivesRethrow()
    {
        var (_, host, result) = ExecuteProgram("""
            SUB Fail()
                PRINT POW(-2, 0.5)
            END SUB
            TRY
                TRY
                    Fail()
                CATCH inner
                    ERROR inner
                END TRY
            CATCH outer
                PRINT outer.Message
            END TRY
            """);
        Assert.True(result.Succeeded);
        var message = Assert.Single(host.Output);
        Assert.StartsWith("Function POW: argument 'value' cannot be used with the specified exponent.", message);
        Assert.Contains("Call chain:", message);
        Assert.Contains("procedure 'Fail'", message);
        Assert.DoesNotMatch("[А-Яа-яЁё]", message);
    }

    [Fact]
    public void Execute_UserMessageWithSystemCodeIsPreservedThroughRethrow()
    {
        var (_, host, result) = ExecuteProgram("""
            TRY
                TRY
                    ERROR "MB3007", "Моя ошибка"
                CATCH inner
                    ERROR inner
                END TRY
            CATCH outer
                PRINT outer.Code
                PRINT outer.Message
            END TRY
            """);
        Assert.True(result.Succeeded);
        Assert.Equal(["MB3007", "Моя ошибка"], host.Output);
    }

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
        Assert.Equal(["User error APP002."], host.Output);
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
