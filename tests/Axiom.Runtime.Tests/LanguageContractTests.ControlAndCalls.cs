using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_DecimalDeclarationAndArithmetic()
    {
        const string text = """
DIM price AS DECIMAL = 10.10
DIM tax AS DECIMAL = 0.20
PRINT price + tax
PRINT (price + tax) / 2
PRINT STR(price + tax)
PRINT INT(price + tax)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10.30", "5.15", "10.30", "10"], host.Output);
    }

    [Fact]
    public void Execute_IfElse()
    {
        const string text = """
DIM result AS STRING
IF TRUE THEN
    result = "yes"
ELSE
    result = "no"
END IF
PRINT result
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["yes"], host.Output);
    }

    [Fact]
    public void Execute_ElseIfChain()
    {
        const string text = """
DIM score AS INTEGER = 7
IF score > 10 THEN
    PRINT "high"
ELSEIF score > 5 THEN
    PRINT "mid"
ELSE
    PRINT "low"
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["mid"], host.Output);
    }

    [Fact]
    public void Execute_StringOrderedComparisonsUseOrdinalSemantics()
    {
        const string text = """
PRINT "Z" > "M"
PRINT "nuts" > "Nuts"
PRINT "abc" < "abd"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseMatchesFirstClauseOnly()
    {
        const string text = """
DIM x AS INTEGER = 5
SELECT CASE x
    CASE IS > 0
        PRINT "positive"
    CASE 5
        PRINT "five"
    CASE ELSE
        PRINT "other"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["positive"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsRangesAndStrings()
    {
        const string text = """
DIM score AS INTEGER = 8
DIM answer AS STRING = "y"

SELECT CASE score
    CASE 1, 3, 5
        PRINT "odd"
    CASE 6 TO 9
        PRINT "range"
    CASE IS > 100
        PRINT "big"
    CASE ELSE
        PRINT "other"
END SELECT

SELECT CASE answer
    CASE "Y", "y"
        PRINT "yes"
    CASE ELSE
        PRINT "no"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["range", "yes"], host.Output);
    }

    [Fact]
    public void Execute_SelectCaseSupportsStringRangesAndComparisons()
    {
        const string text = """
DIM command AS STRING = "Nuts"
DIM letter AS STRING = "K"

SELECT CASE command
    CASE IS > "M"
        PRINT "after"
    CASE ELSE
        PRINT "before"
END SELECT

SELECT CASE letter
    CASE "A" TO "M"
        PRINT "first-half"
    CASE ELSE
        PRINT "second-half"
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["after", "first-half"], host.Output);
    }

    [Fact]
    public void Execute_While()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
WHILE i < 3
    total = total + i
    i = i + 1
END WHILE
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingWhileCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO WHILE i < 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithLeadingUntilCondition()
    {
        const string text = """
DIM i AS INTEGER = 0
DIM total AS INTEGER = 0
DO UNTIL i = 3
    total = total + i
    i = i + 1
LOOP
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingWhileConditionRunsAtLeastOnce()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP WHILE i < 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithTrailingUntilConditionRunsUntilConditionBecomesTrue()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    PRINT i
    i = i + 1
LOOP UNTIL i = 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "2"], host.Output);
    }

    [Fact]
    public void Execute_DoLoopWithoutConditionUsesExitDo()
    {
        const string text = """
DIM i AS INTEGER = 0
DO
    IF i = 2 THEN
        EXIT DO
    END IF
    PRINT i
    i = i + 1
LOOP
PRINT "done"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "1", "done"], host.Output);
    }

    [Fact]
    public void Execute_ForLoop()
    {
        const string text = """
DIM total AS INTEGER = 0
FOR i = 1 TO 3
    total = total + i
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_ExitForBreaksNearestLoop()
    {
        const string text = """
FOR i = 1 TO 2
    FOR j = 1 TO 3
        IF j = 2 THEN
            EXIT FOR
        END IF
        PRINT STR(i) + ":" + STR(j)
    NEXT
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:1", "2:1"], host.Output);
    }

    [Fact]
    public void Execute_ExitDoBreaksNearestDoLoop()
    {
        const string text = """
DIM outer AS INTEGER = 0
DIM inner AS INTEGER
DO WHILE outer < 2
    inner = 0
    DO WHILE inner < 3
        IF inner = 1 THEN
            EXIT DO
        END IF
        PRINT STR(outer) + ":" + STR(inner)
        inner = inner + 1
    LOOP
    outer = outer + 1
LOOP
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0:0", "1:0"], host.Output);
    }

    [Fact]
    public void Execute_ContinueForSkipsRestOfBody()
    {
        const string text = """
FOR i = 1 TO 5
    IF i = 3 THEN
        CONTINUE FOR
    END IF
    PRINT STR(i)
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_ContinueWhileSkipsRestOfBody()
    {
        const string text = """
DIM i AS INTEGER = 0
WHILE i < 5
    i = i + 1
    IF i = 3 THEN
        CONTINUE WHILE
    END IF
    PRINT STR(i)
END WHILE
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_ContinueDoSkipsRestOfBody()
    {
        const string text = """
DIM i AS INTEGER = 0
DO WHILE i < 5
    i = i + 1
    IF i = 3 THEN
        CONTINUE DO
    END IF
    PRINT STR(i)
LOOP
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "4", "5"], host.Output);
    }

    [Fact]
    public void Execute_ContinueForWithNegativeStep()
    {
        const string text = """
FOR i = 5 TO 1 STEP -1
    IF i = 3 THEN
        CONTINUE FOR
    END IF
    PRINT STR(i)
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "4", "2", "1"], host.Output);
    }

    [Fact]
    public void Execute_NestedContinueForTargetsNearestFor()
    {
        const string text = """
FOR i = 1 TO 3
    FOR j = 1 TO 3
        IF j = 2 THEN
            CONTINUE FOR
        END IF
        PRINT STR(i) + ":" + STR(j)
    NEXT
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:1", "1:3", "2:1", "2:3", "3:1", "3:3"], host.Output);
    }

    [Fact]
    public void Execute_NestedForAndWhileLoops()
    {
        const string text = """
DIM j AS INTEGER
DIM total AS INTEGER = 0
FOR i = 1 TO 2
    j = 1
    WHILE j <= 2
        total = total + 1
        j = j + 1
    END WHILE
NEXT
PRINT total
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["4"], host.Output);
    }

    [Fact]
    public void Execute_NestedDoLoopAndForLoops()
    {
        const string text = """
DIM count AS INTEGER = 0
FOR i = 1 TO 2
    DO WHILE count < 2
        PRINT STR(i) + ":" + STR(count)
        count = count + 1
    LOOP
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1:0", "1:1"], host.Output);
    }

    [Fact]
    public void Execute_ReportsStepZero()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3004");
        Assert.Contains("STEP 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_FunctionCallWithReturn()
    {
        const string text = """
PRINT Add(2, 3)

FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER
    RETURN a + b
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_SubCall()
    {
        const string text = """
DIM total AS INTEGER = 0
Increment()
PRINT total

SUB Increment()
    total = total + 1
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubAndFunctionCanBeCalledBeforeTheirDeclaration()
    {
        const string text = """
DIM result AS INTEGER = 0
Outer()
PRINT result

SUB Outer()
    LocalIncrement()
    result = result + AddLocal(2, 3)

    SUB LocalIncrement()
        result = result + 1
    END SUB

    FUNCTION AddLocal(left AS INTEGER, right AS INTEGER) AS INTEGER
        RETURN left + right
    END FUNCTION
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["6"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCallItselfRecursively()
    {
        const string text = """
PRINT Outer(4)

FUNCTION Outer(value AS INTEGER) AS INTEGER
    RETURN Factorial(value)

    FUNCTION Factorial(n AS INTEGER) AS INTEGER
        IF n <= 1 THEN
            RETURN 1
        END IF

        RETURN n * Factorial(n - 1)
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["24"], host.Output);
    }

    [Fact]
    public void Execute_LocalSubCanMutateOuterLocalVariable()
    {
        const string text = """
SUB Outer()
    DIM total AS INTEGER = 10
    AddTwo()
    PRINT total

    SUB AddTwo()
        total = total + 2
    END SUB
END SUB

Outer()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_LocalFunctionCanCaptureOuterParameterAndLocalVariable()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23"], host.Output);
    }

    [Fact]
    public void Execute_FunctionCanBeStoredInVariableAndCalled()
    {
        const string text = """
DIM op AS FUNCTION(INTEGER, INTEGER) AS INTEGER
op = Add
PRINT op(2, 3)

FUNCTION Add(left AS INTEGER, right AS INTEGER) AS INTEGER
    RETURN left + right
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_FunctionCanBePassedAsParameter()
    {
        const string text = """
PRINT ApplyTwice(DoubleValue, 3)

FUNCTION ApplyTwice(op AS FUNCTION(INTEGER) AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN op(op(value))
END FUNCTION

FUNCTION DoubleValue(value AS INTEGER) AS INTEGER
    RETURN value * 2
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionPreservesCapturedValueAfterOuterFunctionReturns()
    {
        const string text = """
DIM addTen AS FUNCTION(INTEGER) AS INTEGER
addTen = MakeAdder(10)
PRINT addTen(5)

FUNCTION MakeAdder(baseValue AS INTEGER) AS FUNCTION(INTEGER) AS INTEGER
    FUNCTION Add(delta AS INTEGER) AS INTEGER
        RETURN baseValue + delta
    END FUNCTION

    RETURN Add
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanMutateSharedCapturedState()
    {
        const string text = """
DIM counter AS FUNCTION() AS INTEGER
counter = MakeCounter()
PRINT counter()
PRINT counter()
PRINT counter()

FUNCTION MakeCounter() AS FUNCTION() AS INTEGER
    DIM current AS INTEGER = 0

    FUNCTION NextValue() AS INTEGER
        current = current + 1
        RETURN current
    END FUNCTION

    RETURN NextValue
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_ReturnedFunctionCanBeCalledImmediately()
    {
        const string text = """
PRINT MakeAdder()(5)

FUNCTION MakeAdder() AS FUNCTION(INTEGER) AS INTEGER
    DIM offset AS INTEGER = 10

    FUNCTION AddOffset(value AS INTEGER) AS INTEGER
        RETURN offset + value
    END FUNCTION

    RETURN AddOffset
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["15"], host.Output);
    }

    [Fact]
    public void Execute_ReportsCallingEmptyFunctionValue()
    {
        const string text = """
DIM missing AS FUNCTION(INTEGER) AS INTEGER
PRINT missing(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3009");
        Assert.Contains("функционального типа", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
