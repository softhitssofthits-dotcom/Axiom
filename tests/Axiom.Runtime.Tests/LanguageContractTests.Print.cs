using Axiom.Parsing;
using Axiom.Semantics;
using System.Text;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_EmptyPrint()
    {
        var (_, host, result) = ExecuteProgram("PRINT");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_PrintExpressionCanProduceEmptyString()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"\"");

        Assert.True(result.Succeeded);
        Assert.Equal([""], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\";");

        Assert.True(result.Succeeded);
        Assert.Equal(["A"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaUsesSoftSeparator()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaSeparatesNumericValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT 10, 20");

        Assert.True(result.Succeeded);
        Assert.Equal(["10 20"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithMultipleCommasUsesSingleSpaces()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\", \"B\", \"C\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["A B C"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithTrailingCommaSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\",");

        Assert.True(result.Succeeded);
        Assert.Equal(["A "], host.Output);
    }

    [Fact]
    public void Execute_PrintWithSemicolonConcatenatesValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"A\"; \"B\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["AB"], host.Output);
    }

    [Fact]
    public void Execute_AmpersandConcatenatesMixedScalarValues()
    {
        const string text = """
DIM name AS STRING = "Иван"
DIM age AS INTEGER = 7
PRINT "Привет, " & name & " тебе " & age & " лет."
PRINT "flag=" & TRUE
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Привет, Иван тебе 7 лет.", "flag=TRUE"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithMixedListUsesSeparatorSemantics()
    {
        var (_, host, result) = ExecuteProgram("PRINT 1; 2, 3");

        Assert.True(result.Succeeded);
        Assert.Equal(["12 3"], host.Output);
    }

    [Fact]
    public void Execute_PrintWithCommaDoesNotUseFixedZones()
    {
        var (_, host, result) = ExecuteProgram("PRINT \"ABCD\", \"E\"");

        Assert.True(result.Succeeded);
        Assert.Equal(["ABCD E"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsNumericMask()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##.##\"; 3.14159");

        Assert.True(result.Succeeded);
        Assert.Equal([" 3.14"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingFormatsStringMasks()
    {
        const string text = """
PRINT USING "! & \    \"; "Hello"; "World"; "xy"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["H World xy    "], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingTrailingSemicolonSuppressesTrailingNewLine()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##\"; 12;");

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingReportsStringFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"!\"; 1");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PrintUsingReportsNumericFieldTypeMismatch()
    {
        var (_, _, result) = ExecuteProgram("PRINT USING \"##\"; \"oops\"");

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Code == "MB3007");
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("числов", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_PrintUsingForceSignPositive()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"+###\"; 42");

        Assert.True(result.Succeeded);
        Assert.Equal(["+ 42"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingForceSignNegative()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"+###\"; -7");

        Assert.True(result.Succeeded);
        Assert.Equal(["-  7"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingNegativeSignOnly()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"-###\"; 5");

        Assert.True(result.Succeeded);
        Assert.Equal(["   5"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingDollarSign()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"$###.##\"; 12.5");

        Assert.True(result.Succeeded);
        Assert.Equal([" $12.50"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingStarFill()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"**###.##\"; 3.14");

        Assert.True(result.Succeeded);
        Assert.Equal(["****3.14"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingStarFillWithDollar()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"**$###.##\"; 7.5");

        Assert.True(result.Succeeded);
        Assert.Equal(["****$7.50"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingThousandsSeparator()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"#,###.##\"; 1234.56");

        Assert.True(result.Succeeded);
        Assert.Equal(["1,234.56"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingNumberDoesNotFit()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"##\"; 1234");

        Assert.True(result.Succeeded);
        Assert.Equal(["%1234"], host.Output);
    }

    [Fact]
    public void Execute_PrintUsingMultipleValues()
    {
        var (_, host, result) = ExecuteProgram("PRINT USING \"## = ##\"; 1; 2");

        Assert.True(result.Succeeded);
        Assert.Equal([" 1 =  2"], host.Output);
    }

}
