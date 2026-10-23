using Axiom.Execution;

namespace Axiom.Execution.Tests;

public sealed class ArraySyntaxMigrationTests
{
    [Fact]
    public void Migrate_MovesTypeArraySuffixToName()
    {
        var result = ArraySyntaxMigrationService.Migrate("DIM values AS INTEGER()\nPRINT values(1)\n");

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM values[] AS INTEGER\nPRINT values[1]\n", result.TransformedSource);
        Assert.NotEmpty(result.Edits);
        Assert.Equal("DIM values AS INTEGER()\nPRINT values(1)\n", result.OriginalSource);
    }

    [Fact]
    public void Migrate_MovesConstantArrayTypeSuffixAndPreservesWholeArrayReferences()
    {
        const string source = "CONST values AS STRING() = SPLIT(\"яблоко,груша\", \",\")\nPRINT LBOUND(values)\nDIM copy = values\nPRINT values(1)";
        const string expected = "CONST values[] AS STRING = SPLIT(\"яблоко,груша\", \",\")\nPRINT LBOUND(values[])\nDIM copy[] = values[]\nPRINT values[1]";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal(source, result.OriginalSource);
        Assert.Equal(expected, result.TransformedSource);
        AssertOrderedNonOverlapping(result);

        var repeated = ArraySyntaxMigrationService.Migrate(expected);
        Assert.True(repeated.CanApply, string.Join("\n", repeated.Diagnostics));
        Assert.Empty(repeated.Edits);
        Assert.Equal(expected, repeated.TransformedSource);
    }

    [Fact]
    public void Migrate_RejectsAssignmentToFunctionCallWithoutPartialEdits()
    {
        const string source = "FUNCTION Make() AS STRING()\nRETURN SPLIT(\"яблоко\", \",\")\nEND FUNCTION\nDIM values = SPLIT(\"груша\", \",\")\nMake() = values";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.False(result.CanApply);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Empty(result.Edits);
        Assert.Equal(source, result.OriginalSource);
        Assert.Equal(source, result.TransformedSource);
    }

    [Fact]
    public void Migrate_UsesBoundSymbolsForWholeArrayNames()
    {
        var result = ArraySyntaxMigrationService.Migrate(
            "DIM values AS INTEGER()\nDIM copy AS INTEGER()\ncopy = values\n");

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM values[] AS INTEGER\nDIM copy[] AS INTEGER\ncopy[] = values[]\n", result.TransformedSource);
    }

    [Fact]
    public void Migrate_LeavesFunctionsGroupingStringsAndCommentsUntouched()
    {
        var source = "DIM values AS INTEGER()\nPRINT LEN(\"values(2)\") + (2 * 3)\nPRINT \"values(2)\" ' values(3)\n";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Contains("LEN(\"values(2)\") + (2 * 3)", result.TransformedSource);
        Assert.Contains("\"values(2)\" ' values(3)", result.TransformedSource);
    }

    [Fact]
    public void Migrate_InvalidSourceDoesNotReturnPartialEdits()
    {
        var source = "DIM values AS INTEGER()\nIF\n";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.False(result.CanApply);
        Assert.Empty(result.Edits);
        Assert.Equal(source, result.TransformedSource);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Migrate_IsIdempotentForSquareSyntax()
    {
        var source = "DIM values[] AS INTEGER\nvalues[] = values[]\nPRINT values[1]\n";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal(source, result.TransformedSource);
        Assert.Empty(result.Edits);
    }

    [Fact]
    public void Migrate_PreservesCrLfAndUnicodeText()
    {
        var source = "DIM values AS STRING()\r\nPRINT \"фрукты: яблоко\"\r\n";
        var result = ArraySyntaxMigrationService.Migrate(source);

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM values[] AS STRING\r\nPRINT \"фрукты: яблоко\"\r\n", result.TransformedSource);
    }

    [Theory]
    [InlineData("DIM grid(1 TO 2, -1 TO 3) AS INTEGER\nPRINT grid(1, 2)",
        "DIM grid[1 TO 2, -1 TO 3] AS INTEGER\nPRINT grid[1, 2]")]
    [InlineData("DIM values = SPLIT(\"яблоко,груша\", \",\")\nPRINT values(1)",
        "DIM values[] = SPLIT(\"яблоко,груша\", \",\")\nPRINT values[1]")]
    [InlineData("DIM values() AS STRING = SPLIT(\"яблоко\", \",\")\nERASE values",
        "DIM values[] AS STRING = SPLIT(\"яблоко\", \",\")\nERASE values[]")]
    [InlineData("DIM grid(,) AS INTEGER\nREDIM PRESERVE grid(1 TO 2, 1 TO 3)\nERASE grid()",
        "DIM grid[,] AS INTEGER\nREDIM PRESERVE grid[1 TO 2, 1 TO 3]\nERASE grid[]")]
    [InlineData("SUB Show(BYREF values AS INTEGER())\nPRINT values(1)\nEND SUB",
        "SUB Show(BYREF values[] AS INTEGER)\nPRINT values[1]\nEND SUB")]
    [InlineData("DIM reader AS FUNCTION(INTEGER()) AS STRING()",
        "DIM reader AS FUNCTION(INTEGER[]) AS STRING[]")]
    [InlineData("DIM values(1 TO 2) AS INTEGER\nPRINT $\"Значение: {values(1)}\"",
        "DIM values[1 TO 2] AS INTEGER\nPRINT $\"Значение: {values[1]}\"")]
    public void Migrate_ConvertsOnlyArrayForms(string source, string expected)
    {
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal(expected, result.TransformedSource);
        AssertOrderedNonOverlapping(result);
        var repeated = ArraySyntaxMigrationService.Migrate(result.TransformedSource);
        Assert.True(repeated.CanApply, string.Join("\n", repeated.Diagnostics));
        Assert.Empty(repeated.Edits);
        Assert.Equal(expected, repeated.TransformedSource);
    }

    [Fact]
    public void Migrate_HandlesNestedPartialArraysAndRanges()
    {
        var source = "DIM grid(1 TO 2, 1 TO 3) AS INTEGER\nPRINT grid(1)(2)\nPRINT grid(1 TO 2, 1 TO 3)(1)(2)";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM grid[1 TO 2, 1 TO 3] AS INTEGER\nPRINT grid[1][2]\nPRINT grid[1 TO 2, 1 TO 3][1][2]", result.TransformedSource);
        AssertOrderedNonOverlapping(result);
    }

    [Fact]
    public void Migrate_DistinguishesFunctionResultAndArrayIndexing()
    {
        var source = "FUNCTION Make() AS STRING()\nDIM result = SPLIT(\"яблоко\", \",\")\nRETURN result\nEND FUNCTION\nPRINT Make()(1)\nPRINT SPLIT(\"груша\", \",\")(1)";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Contains("FUNCTION Make() AS STRING[]", result.TransformedSource);
        Assert.Contains("RETURN result[]", result.TransformedSource);
        Assert.Contains("PRINT Make()[1]", result.TransformedSource);
        Assert.Contains("PRINT SPLIT(\"груша\", \",\")[1]", result.TransformedSource);
    }

    [Fact]
    public void Migrate_HandlesFieldsAndCompositeRedimTargets()
    {
        var source = "TYPE Bag\nvalues() AS INTEGER\nEND TYPE\nDIM bags(1 TO 2) AS Bag\nREDIM bags(1).values(1 TO 3)\nbags(1).values(2) = 4\nERASE bags(1).values";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("TYPE Bag\nvalues[] AS INTEGER\nEND TYPE\nDIM bags[1 TO 2] AS Bag\nREDIM bags[1].values[1 TO 3]\nbags[1].values[2] = 4\nERASE bags[1].values[]", result.TransformedSource);
    }

    [Fact]
    public void Migrate_DoesNotConfuseSameSpellingInSeparateScopes()
    {
        var source = "DIM basket() AS INTEGER\nPRINT LBOUND(basket)\nSUB Show()\nDIM values AS INTEGER = 7\nPRINT values\nEND SUB";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM basket[] AS INTEGER\nPRINT LBOUND(basket[])\nSUB Show()\nDIM values AS INTEGER = 7\nPRINT values\nEND SUB", result.TransformedSource);
    }

    [Fact]
    public void Migrate_SemanticErrorAndInvalidDependencyBlockAllEdits()
    {
        const string source = "DIM values() AS INTEGER\nPRINT missing";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.False(result.CanApply);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MB2001");
        Assert.Empty(result.Edits);
        Assert.Equal(source, result.TransformedSource);

        const string validSource = "DIM values() AS INTEGER";
        var withDependency = ArraySyntaxMigrationService.Migrate(validSource, "main.axm",
            [new SourceDocument("unused.axm", "PRINT missing")]);
        Assert.False(withDependency.CanApply);
        Assert.Empty(withDependency.Edits);
        Assert.Equal(validSource, withDependency.TransformedSource);
        Assert.Contains(withDependency.Diagnostics, diagnostic => diagnostic.Document == "unused.axm");
    }

    [Fact]
    public void Migrate_OverloadSelectsEntryAndLeavesDependencySnapshotsUnchanged()
    {
        var dependency = new SourceDocument("helper.axm", "PUBLIC DIM values() AS INTEGER");
        var entry = new SourceDocument("main.axm", "IMPORT helper\nPRINT LBOUND(values)");
        var result = ArraySyntaxMigrationService.Migrate([dependency, entry], "main");
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("IMPORT helper\nPRINT LBOUND(values[])", result.TransformedSource);
        Assert.Equal("PUBLIC DIM values() AS INTEGER", dependency.Text);
        Assert.All(result.Edits, edit => Assert.Equal("main.axm", edit.Document));
    }

    private static void AssertOrderedNonOverlapping(ArraySyntaxMigrationResult result)
    {
        var rebuilt = new System.Text.StringBuilder(result.OriginalSource);
        for (var index = result.Edits.Count - 1; index >= 0; index--)
        {
            var edit = result.Edits[index];
            if (index > 0) Assert.True(result.Edits[index - 1].Span.End <= edit.Span.Start);
            rebuilt.Remove(edit.Span.Start, edit.Span.Length).Insert(edit.Span.Start, edit.Replacement);
        }
        Assert.Equal(result.TransformedSource, rebuilt.ToString());
    }

    [Fact]
    public void Migrate_DoesNotEvaluateArrayProducingCalls()
    {
        const string source = "DIM lines = READALLLINES(\"файл-которого-нет.txt\")\nPRINT lines(1)";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("DIM lines[] = READALLLINES(\"файл-которого-нет.txt\")\nPRINT lines[1]", result.TransformedSource);
    }

    [Fact]
    public void Migrate_SharedTypeSuffixAndSpacedTokensHaveNoOverlappingEdits()
    {
        const string source = "DIM first, second AS INTEGER( , )\nfirst = second";
        var result = ArraySyntaxMigrationService.Migrate(source);
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        // Пробелы старого суффикса остаются нетронутыми, токены переносятся к именам.
        Assert.Equal("DIM first[,], second[,] AS INTEGER  \nfirst[] = second[]", result.TransformedSource);
        AssertOrderedNonOverlapping(result);
    }

    [Fact]
    public void Migrate_PreservesNonBmpTextAndExactEditOffsets()
    {
        const string source = "PRINT \"Привет 🍎\"\r\nDIM values(1 TO 2) AS INTEGER\r\nPRINT values(1)";
        var result = ArraySyntaxMigrationService.Migrate(source, "main.axm");
        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Equal("PRINT \"Привет 🍎\"\r\nDIM values[1 TO 2] AS INTEGER\r\nPRINT values[1]", result.TransformedSource);
        AssertOrderedNonOverlapping(result);
    }

    [Fact]
    public void Migrate_BindsAdditionalDocumentsForScopes()
    {
        var result = ArraySyntaxMigrationService.Migrate(
            "IMPORT helper\nDIM local AS INTEGER()\nlocal = shared\n",
            "main.axm",
            [new SourceDocument("helper.axm", "PUBLIC DIM shared AS INTEGER()\n")]);

        Assert.True(result.CanApply, string.Join("\n", result.Diagnostics));
        Assert.Contains("local[] = shared[]", result.TransformedSource);
    }
}
