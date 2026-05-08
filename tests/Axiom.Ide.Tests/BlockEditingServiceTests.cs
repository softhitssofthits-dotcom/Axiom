using ICSharpCode.AvalonEdit.Document;
using Axiom.Ide.Editor.Formatting;
using Axiom.Ide.Editor.StructuralEditing;

namespace Axiom.Ide.Tests;

public sealed class BlockEditingServiceTests
{
    private readonly AxiomBlockEditingService _service = new(new AxiomCodeFormattingService());

    [Fact]
    public void TryHandleLineTransition_EnterAfterIfHeader_InsertsBodyIndentAndClosingLine()
    {
        var document = new TextDocument("IF a = 0 THEN\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a = 0 THEN\r\n    \r\nEND IF", document.Text);
        Assert.Equal(document.GetLineByNumber(2).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterSubHeader_InsertsEndSub()
    {
        var document = new TextDocument("SUB Greet(name AS STRING)\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Modern, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SUB Greet(name AS STRING)\r\n    \r\nEnd Sub", document.Text);
        Assert.Equal(document.GetLineByNumber(2).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterFunctionHeader_InsertsEndFunction()
    {
        var document = new TextDocument("FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Classic, out _);

        Assert.True(changed);
        Assert.Equal("FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER\r\n    \r\nEND FUNCTION", document.Text);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterForHeader_InsertsNext()
    {
        var document = new TextDocument("FOR i = 1 TO 10\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("FOR i = 1 TO 10\r\n    \r\nNEXT", document.Text);
        Assert.Equal(document.GetLineByNumber(2).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterDoWhileHeader_InsertsLoop()
    {
        var document = new TextDocument("DO WHILE a < 10\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Modern, out _);

        Assert.True(changed);
        Assert.Equal("DO WHILE a < 10\r\n    \r\nLoop", document.Text);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterSelectCaseHeader_InsertsEndSelect()
    {
        var document = new TextDocument("SELECT CASE value\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SELECT CASE value\r\n    \r\nEND SELECT", document.Text);
        Assert.Equal(document.GetLineByNumber(2).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterTryHeader_InsertsEndTry()
    {
        var document = new TextDocument("TRY\r\n");

        var changed = _service.TryHandleLineTransition(document, 1, 2, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("TRY\r\n    \r\nEND TRY", document.Text);
        Assert.Equal(document.GetLineByNumber(2).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedCatch_DedentsToTryLevelAndIndentsBranchBody()
    {
        var document = new TextDocument("TRY\r\n    PRINT 10 / 0\r\n    CATCH err\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("TRY\r\n    PRINT 10 / 0\r\nCATCH err\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedFinally_DedentsToTryLevelAndIndentsBranchBody()
    {
        var document = new TextDocument("TRY\r\n    PRINT 1\r\n    FINALLY\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Modern, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("TRY\r\n    PRINT 1\r\nFINALLY\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterInsideNestedBody_PreservesCurrentIndent()
    {
        var document = new TextDocument("IF a THEN\r\n    IF b THEN\r\n        PRINT 1\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    IF b THEN\r\n        PRINT 1\r\n        ", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset + 8, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_EnterAfterNestedIfInsideElseIf_InsertsInnerClosingLine()
    {
        var document = new TextDocument(
            "IF a > 9 THEN\r\n" +
            "    PRINT \">\"\r\n" +
            "ELSEIF a < 9 THEN\r\n" +
            "    PRINT \"<\"\r\n" +
            "    IF a = 9 THEN\r\n" +
            "\r\n" +
            "ELSE\r\n" +
            "    PRINT \"=\"\r\n" +
            "END IF");

        var currentLine = document.GetLineByNumber(6);
        var changed = _service.TryHandleLineTransition(document, 5, 6, currentLine.Offset, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal(
            "IF a > 9 THEN\r\n" +
            "    PRINT \">\"\r\n" +
            "ELSEIF a < 9 THEN\r\n" +
            "    PRINT \"<\"\r\n" +
            "    IF a = 9 THEN\r\n" +
            "        \r\n" +
            "    END IF\r\n" +
            "ELSE\r\n" +
            "    PRINT \"=\"\r\n" +
            "END IF",
            document.Text);
        Assert.Equal(document.GetLineByNumber(6).Offset + 8, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_AfterInnerClosingLine_ReturnsToParentIndent()
    {
        var document = new TextDocument("IF a THEN\r\n    IF b THEN\r\n        PRINT 1\r\n    END IF\r\n");

        var changed = _service.TryHandleLineTransition(document, 4, 5, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    IF b THEN\r\n        PRINT 1\r\n    END IF\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(5).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_AfterOuterClosingLine_ReturnsToZeroIndent()
    {
        var document = new TextDocument("IF a THEN\r\n    PRINT 1\r\nEND IF\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.False(changed);
        Assert.Equal("IF a THEN\r\n    PRINT 1\r\nEND IF\r\n", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_AfterElseIf_IndentsBranchBody()
    {
        var document = new TextDocument("IF a THEN\r\nELSEIF b THEN\r\n");

        var changed = _service.TryHandleLineTransition(document, 2, 3, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\nELSEIF b THEN\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(3).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedElseIf_DedentsToIfLevelAndIndentsBranchBody()
    {
        var document = new TextDocument("IF a THEN\r\n    PRINT 1\r\n    ELSEIF b THEN\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    PRINT 1\r\nELSEIF b THEN\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedElse_DedentsToIfLevelAndIndentsBranchBody()
    {
        var document = new TextDocument("IF a THEN\r\n    PRINT 1\r\n    ELSE\r\n");

        var changed = _service.TryHandleLineTransition(document, 3, 4, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    PRINT 1\r\nELSE\r\n    ", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset + 4, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_MovingAwayFromRepeatedElseIf_InsertsBranchBodyBeforeClosingLine()
    {
        var document = new TextDocument("IF a THEN\r\n    PRINT 1\r\n    ELSEIF b THEN\r\nEND IF");
        var caretOffset = document.GetLineByNumber(4).Offset;

        var changed = _service.TryHandleLineTransition(document, 3, 4, caretOffset, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    PRINT 1\r\nELSEIF b THEN\r\n    \r\nEND IF", document.Text);
        Assert.Equal(document.GetLineByNumber(5).Offset, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_AfterCaseElse_IndentsCaseBody()
    {
        var document = new TextDocument("SELECT CASE x\r\nCASE ELSE\r\n");

        var changed = _service.TryHandleLineTransition(document, 2, 3, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SELECT CASE x\r\n    CASE ELSE\r\n        ", document.Text);
        Assert.Equal(document.GetLineByNumber(3).Offset + 8, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedCase_ReindentsToSelectInnerLevelAndIndentsCaseBody()
    {
        var document = new TextDocument("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n        CASE 2\r\n");

        var changed = _service.TryHandleLineTransition(document, 4, 5, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n    CASE 2\r\n        ", document.Text);
        Assert.Equal(document.GetLineByNumber(5).Offset + 8, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_IndentedCaseElse_ReindentsToSelectInnerLevelAndIndentsCaseBody()
    {
        var document = new TextDocument("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n        CASE ELSE\r\n");

        var changed = _service.TryHandleLineTransition(document, 4, 5, document.TextLength, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n    CASE ELSE\r\n        ", document.Text);
        Assert.Equal(document.GetLineByNumber(5).Offset + 8, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_MovingAwayFromRepeatedCase_InsertsCaseBodyBeforeClosingLine()
    {
        var document = new TextDocument("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n        CASE 2\r\nEND SELECT");
        var caretOffset = document.GetLineByNumber(5).Offset;

        var changed = _service.TryHandleLineTransition(document, 4, 5, caretOffset, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("SELECT CASE x\r\n    CASE 1\r\n        PRINT 1\r\n    CASE 2\r\n        \r\nEND SELECT", document.Text);
        Assert.Equal(document.GetLineByNumber(6).Offset, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_MovingAwayFromHeaderWithoutEnter_InsertsBodyAndClosingBeforeCurrentLine()
    {
        var document = new TextDocument("IF a THEN\r\nPRINT 1");
        var caretOffset = document.GetLineByNumber(2).Offset;

        var changed = _service.TryHandleLineTransition(document, 1, 2, caretOffset, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.True(changed);
        Assert.Equal("IF a THEN\r\n    \r\nEND IF\r\nPRINT 1", document.Text);
        Assert.Equal(document.GetLineByNumber(4).Offset, newCaretOffset);
    }

    [Fact]
    public void TryHandleLineTransition_DoesNotDuplicateExistingMatchingClose()
    {
        var document = new TextDocument("IF a THEN\r\n    PRINT 1\r\nEND IF");
        var caretOffset = document.GetLineByNumber(2).Offset;

        var changed = _service.TryHandleLineTransition(document, 1, 2, caretOffset, CodeFormattingStyle.Classic, out var newCaretOffset);

        Assert.False(changed);
        Assert.Equal("IF a THEN\r\n    PRINT 1\r\nEND IF", document.Text);
        Assert.Equal(caretOffset, newCaretOffset);
    }
}
