using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class InterpreterTests
{
    [Fact]
    public void Execute_SplitReturnsStringArrayWithOneBasedBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red,green,,blue", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(3)
PRINT parts(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue"], host.Output);
    }

    [Fact]
    public void Execute_SplitEmptyStringReturnsCanonicalEmptyArray()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("", ",")
PRINT ARRAYISALLOCATED(parts)
PRINT ARRAYLENGTH(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_SplitReportsEmptySeparator()
    {
        const string text = """
PRINT SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_JoinUsesStringArrayBounds()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("red|green|blue", "|")
PRINT JOIN(parts, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["red/green/blue"], host.Output);
    }

    [Fact]
    public void Execute_JoinUnallocatedAndEmptyArraysReturnEmptyString()
    {
        const string text = """
DIM first() AS STRING
DIM second(1 TO 0) AS STRING
PRINT JOIN(first)
PRINT JOIN(second)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["", ""], host.Output);
    }

    [Fact]
    public void Execute_JoinSingleElementReturnsThatElement()
    {
        const string text = """
DIM parts(5 TO 5) AS STRING
parts(5) = "solo"
PRINT JOIN(parts)
PRINT JOIN(parts, "/")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["solo", "solo"], host.Output);
    }

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

    [Fact]
    public void Execute_BuiltinLen()
    {
        var (_, host, result) = ExecuteProgram("PRINT LEN(\"hello\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStrAndVal()
    {
        var (_, host, result) = ExecuteProgram("PRINT STR(VAL(\"12.5\"))");

        Assert.True(result.Succeeded);
        Assert.Equal(["12.5"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinTrimVariants()
    {
        const string text = """
PRINT TRIM("  hello  ")
PRINT TRIM("--hello--", "-")
PRINT TRIM("hello")
PRINT "[" + TRIM("hello   ") + "]"
PRINT "[" + TRIM("   hello") + "]"
PRINT "[" + TRIM("   ") + "]"
PRINT LTRIM("  hello  ")
PRINT LTRIM("hello")
PRINT RTRIM("  hello  ")
PRINT RTRIM("hello")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hello", "hello", "hello", "[hello]", "[hello]", "[]", "hello  ", "hello", "  hello", "hello", "hello", "hello"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReverse()
    {
        var (_, host, result) = ExecuteProgram("PRINT REVERSE(\"abcd\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["dcba"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatAndSpace()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT REPEAT("x", 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", ""], host.Output);
    }

    [Fact]
    public void Execute_BuiltinStringUsesFirstFillCharacter()
    {
        var (_, host, result) = ExecuteProgram("PRINT STRING(5, \"ab\")");

        Assert.True(result.Succeeded);
        Assert.Equal(["aaaaa"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinPadLeftAndPadRight()
    {
        const string text = """
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADLEFT("7", 3, "0")
PRINT PADLEFT("word", 2, ".")
PRINT PADLEFT("word", 4)
PRINT "[" + PADRIGHT("7", 3) + "]"
PRINT PADRIGHT("7", 3, ".")
PRINT PADRIGHT("word", 2, ".")
PRINT PADRIGHT("word", 4, ".")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[  7]", "007", "word", "word", "[7  ]", "7..", "word", "word"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInsertAndRemove()
    {
        const string text = """
PRINT INSERT("abcd", 1, "<")
PRINT INSERT("abcd", 3, "-")
PRINT INSERT("abcd", 99, "!")
PRINT INSERT("", 5, "seed")
PRINT INSERT("abcd", 2, "")
PRINT REMOVE("abcdef", 2, 3)
PRINT REMOVE("abcdef", 6, 10)
PRINT REMOVE("abcdef", 99, 2)
PRINT REMOVE("abcdef", 3, 0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["<abcd", "ab-cd", "abcd!", "seed", "abcd", "aef", "abcde", "abcdef", "abcdef"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRepeatReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPEAT(\"ha\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPEAT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadLeftReportsNegativeWidth()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADLEFT(\"7\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADLEFT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinPadRightReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT PADRIGHT(\"7\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PADRIGHT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInsertReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSERT(\"abc\", 0, \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRemoveReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REMOVE(\"abc\", 1, -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REMOVE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinStringReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT STRING(3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSearchFunctionsUseOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT CONTAINS("Hello", "ELL")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT STARTSWITH("Hello", "")
PRINT ENDSWITH("Hello", "")
PRINT STARTSWITH("Hello", "Hello")
PRINT ENDSWITH("Hello", "Hello")
PRINT STARTSWITH("Hello", "Hello!")
PRINT ENDSWITH("Hello", "Hello!")
PRINT CONTAINS("Hello", "")
PRINT CONTAINS("", "x")
PRINT CONTAINS("Hello", "Hello")
PRINT CONTAINS("Hello", "Hello!")
PRINT CONTAINS("Hello", "o")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceUsesOrdinalCaseSensitiveRules()
    {
        const string text = """
PRINT REPLACE("one two two", "two", "three")
PRINT REPLACE("One one", "one", "two")
PRINT REPLACE("abc", "x", "y")
PRINT REPLACE("hello", "he", "he")
PRINT REPLACE("hello", "hello", "done")
PRINT REPLACE("abba", "b", "c")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one three three", "One two", "abc", "hello", "done", "acca"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinReplaceManyUsesOnePassLongestMatchRules()
    {
        const string text = """
DIM oldItems(1 TO 4) AS STRING
DIM newItems(1 TO 4) AS STRING
oldItems(1) = "ab"
newItems(1) = "X"
oldItems(2) = "abc"
newItems(2) = "Y"
oldItems(3) = "x"
newItems(3) = "z"
oldItems(4) = "one"
newItems(4) = "two"
PRINT REPLACEMANY("abc ab x One", oldItems, newItems)
PRINT REPLACEMANY("one", oldItems, newItems)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Y X z One", "two"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReturnsOneBasedPositions()
    {
        const string text = """
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTR("banana", "NA")
PRINT INSTR(7, "banana", "")
PRINT INSTR(8, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "5", "0", "7", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTR(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReturnsLastOneBasedPosition()
    {
        const string text = """
PRINT INSTRREV("banana", "na")
PRINT INSTRREV("banana", "NA")
PRINT INSTRREV("abc", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["5", "0", "4"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevCanSearchBackFromStartPosition()
    {
        const string text = """
PRINT INSTRREV(4, "banana", "na")
PRINT INSTRREV(2, "banana", "na")
PRINT INSTRREV(100, "banana", "na")
PRINT INSTRREV(4, "banana", "")
PRINT INSTRREV(100, "banana", "")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "0", "5", "5", "7"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinInStrRevReportsInvalidStart()
    {
        var (_, _, result) = ExecuteProgram("PRINT INSTRREV(0, \"abc\", \"a\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("INSTRREV", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscAndChrUseCp866ForByteCodesAndUnicodeAbove255()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
PRINT ASC("Я")
PRINT CHR(159)
PRINT ASC("█")
PRINT CHR(219)
PRINT ASC(CHR(1071))
PRINT ASC(CHR(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["65", "A", "159", "Я", "219", "█", "159", "128512"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinAscWAndChrWUseUnicodeCodePoints()
    {
        const string text = """
PRINT ASC("Я")
PRINT ASCW("Я")
PRINT ASC("█")
PRINT ASCW("█")
PRINT ASCW(CHR(159))
PRINT ASCW(CHRW(1071))
PRINT CHRW(1071)
PRINT ASCW(CHRW(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["159", "1071", "219", "9608", "1071", "1071", "Я", "128512"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterCentersText()
    {
        const string text = """
PRINT "[" + CENTER("A", 3) + "]"
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT "[" + CENTER("AB", 5, ".") + "]"
PRINT "[" + CENTER("Hello", 3, ".") + "]"
PRINT "[" + CENTER("Hello", 5, ".") + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[ A ]", "[.A..]", "[.AB..]", "[Hello]", "[Hello]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinCenterReportsEmptyFill()
    {
        var (_, _, result) = ExecuteProgram("PRINT CENTER(\"A\", 3, \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fill", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    public void Execute_BuiltinCenterReportsNegativeWidth(string widthText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CENTER(\"A\", {widthText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CENTER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASC(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASC", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinAscWReportsEmptyText()
    {
        var (_, _, result) = ExecuteProgram("PRINT ASCW(\"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("ASCW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHR({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("55296")]
    [InlineData("1114112")]
    public void Execute_BuiltinChrWReportsInvalidCode(string codeText)
    {
        var (_, _, result) = ExecuteProgram($"PRINT CHRW({codeText})");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("CHRW", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReturnsNonOverlappingCount()
    {
        const string text = """
PRINT SUBSTRCOUNT("banana", "na")
PRINT SUBSTRCOUNT("aaaa", "aa")
PRINT SUBSTRCOUNT("banana", "NA")
PRINT SUBSTRCOUNT("", "a")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinSubStrCountReportsEmptyFragment()
    {
        var (_, _, result) = ExecuteProgram("PRINT SUBSTRCOUNT(\"abc\", \"\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("SUBSTRCOUNT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fragment", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinReplaceReportsEmptyOldText()
    {
        var (_, _, result) = ExecuteProgram("PRINT REPLACE(\"abc\", \"\", \"x\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не может быть пустой строкой", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_BuiltinReplaceManyReportsInvalidRules()
    {
        const string text = """
DIM oldItems(1 TO 1) AS STRING
DIM newItems(1 TO 2) AS STRING
oldItems(1) = ""
newItems(1) = "x"
newItems(2) = "y"
PRINT REPLACEMANY("abc", oldItems, newItems)
""";

        var (_, _, result) = ExecuteProgram(text);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REPLACEMANY", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexIsMatchSupportsModes()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXISMATCH("one" + CHR(10) + "two", "^two", "m")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "FALSE", "TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchReturnsFirstMatchOrEmptyString()
    {
        const string text = """
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXMATCH("A12", "a\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "[]", "A12"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReturnsRequestedGroup()
    {
        const string text = """
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXEXTRACT("a12", "([A-Z])(\d+)", 1, "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12", "[]", "a"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceSupportsGroupReferencesAndCount()
    {
        const string text = """
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#")
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("A12 B34", "([A-Z])(\d+)", "$1-$2")
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
PRINT REGEXREPLACE("payload 42", "payload\s+", "payload ", "i")
PRINT REGEXREPLACE("abc", "x", "y", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["one # two #", "one # two 34", "A-12 B-34", "# b34", "payload 42", "abc"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexCountReturnsMatchCount()
    {
        const string text = """
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
PRINT REGEXCOUNT("text", "\d+")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "2", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexMatchesReturnsAllMatches()
    {
        const string text = """
DIM values() AS STRING
values = REGEXMATCHES("Order 42 and 100", "\d+")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXMATCHES("Order", "\d+")
PRINT LBOUND(values)
PRINT UBOUND(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "42", "100", "2", "A12", "b34", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReturnsRequestedGroupForEveryMatch()
    {
        const string text = """
DIM values() AS STRING
values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 2)
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("a12 b34", "([A-Z])(\d+)", 1, "i")
PRINT ARRAYLENGTH(values)
PRINT values(1)
PRINT values(2)

values = REGEXEXTRACTALL("A12 B34", "([A-Z])(\d+)", 3)
PRINT "[" + values(1) + "]"
PRINT "[" + values(2) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2", "12", "34", "2", "a", "b", "[]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexSplitReturnsStringArray()
    {
        const string text = """
DIM parts() AS STRING
parts = REGEXSPLIT("one, two;three", "[,;]\s*")
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(2)
PRINT parts(3)

parts = REGEXSPLIT("A12 b34", "[a-z]\d+", "i")
PRINT ARRAYLENGTH(parts)
PRINT "[" + parts(1) + "]"
PRINT "[" + parts(2) + "]"
PRINT "[" + parts(3) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "one", "two", "three", "3", "[]", "[ ]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsInvalidPattern()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"[\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pattern", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReportsUnknownMode()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXISMATCH(\"abc\", \"a\", \"z\")");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXISMATCH", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexReplaceReportsNegativeCount()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXREPLACE(\"abc\", \"a\", \"x\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACT(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_BuiltinRegexExtractAllReportsNegativeGroup()
    {
        var (_, _, result) = ExecuteProgram("PRINT REGEXEXTRACTALL(\"abc\", \"(a)\", -1)");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("REGEXEXTRACTALL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("group", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

}
