using ICSharpCode.AvalonEdit.Document;
using Axiom.Core;
using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.Editor.StructuralEditing;

public sealed class AxiomBlockEditingService
{
    private const string IndentUnit = "    ";

    private readonly ICodeFormattingService _formattingService;

    public AxiomBlockEditingService(ICodeFormattingService formattingService)
    {
        _formattingService = formattingService;
    }

    public bool TryHandleLineTransition(
        TextDocument document,
        int previousLineNumber,
        int currentLineNumber,
        int caretOffset,
        CodeFormattingStyle style,
        out int newCaretOffset)
    {
        ArgumentNullException.ThrowIfNull(document);

        newCaretOffset = caretOffset;
        var changed = false;

        if (previousLineNumber < 1 || previousLineNumber > document.LineCount)
        {
            return false;
        }

        if (TryNormalizeBranchIndent(document, previousLineNumber, ref newCaretOffset))
        {
            changed = true;
        }

        var previousLine = document.GetLineByNumber(previousLineNumber);
        var previousAnalysis = AnalyzeLine(GetLineText(document, previousLine));

        if (previousAnalysis.IsBlank)
        {
            return changed;
        }

        if (currentLineNumber >= 1 && currentLineNumber <= document.LineCount)
        {
            var currentLine = document.GetLineByNumber(currentLineNumber);

            if (IsBlankLine(document, currentLine))
            {
                if (previousAnalysis.OpeningKind is not null &&
                    !HasMatchingClose(document, previousLineNumber, previousAnalysis.OpeningKind.Value))
                {
                    var bodyIndent = previousAnalysis.Indentation + IndentUnit;
                    var closingIndent = previousAnalysis.Indentation;
                    var closingText = FormatClosingText(previousAnalysis.OpeningKind.Value, style);
                    var lineBreak = GetPreferredLineBreak(document, previousLine, currentLine);
                    var replacementText = bodyIndent + lineBreak + closingIndent + closingText;

                    document.Replace(currentLine.Offset, currentLine.Length, replacementText);
                    newCaretOffset = currentLine.Offset + bodyIndent.Length;
                    return true;
                }

                var desiredIndent = previousAnalysis.IncreasesIndentNextLine
                    ? previousAnalysis.Indentation + IndentUnit
                    : previousAnalysis.Indentation;
                var currentText = GetLineText(document, currentLine);

                if (string.Equals(currentText, desiredIndent, StringComparison.Ordinal))
                {
                    newCaretOffset = Math.Min(currentLine.Offset + desiredIndent.Length, document.TextLength);
                    return changed;
                }

                document.Replace(currentLine.Offset, currentLine.Length, desiredIndent);
                newCaretOffset = currentLine.Offset + desiredIndent.Length;
                return true;
            }

            if (previousAnalysis.BranchKind is not null)
            {
                var branchInsertionOffset = GetInsertionOffsetAfterLine(document, previousLineNumber);
                var branchLineBreak = GetPreferredLineBreak(document, previousLine, currentLine);
                var branchBodyLine = previousAnalysis.Indentation + IndentUnit;
                var branchInsertionText = branchBodyLine + branchLineBreak;

                document.Insert(branchInsertionOffset, branchInsertionText);
                newCaretOffset = newCaretOffset >= branchInsertionOffset
                    ? newCaretOffset + branchInsertionText.Length
                    : newCaretOffset;
                return true;
            }
        }

        if (previousAnalysis.OpeningKind is null ||
            HasMatchingClose(document, previousLineNumber, previousAnalysis.OpeningKind.Value))
        {
            return changed;
        }

        var insertionOffset = GetInsertionOffsetAfterLine(document, previousLineNumber);
        var lineBreakForInsert = GetPreferredLineBreak(document, previousLine, null);
        var bodyLine = previousAnalysis.Indentation + IndentUnit;
        var closingLine = previousAnalysis.Indentation + FormatClosingText(previousAnalysis.OpeningKind.Value, style);
        var insertionText = bodyLine + lineBreakForInsert + closingLine + lineBreakForInsert;

        document.Insert(insertionOffset, insertionText);
        newCaretOffset = newCaretOffset >= insertionOffset
            ? newCaretOffset + insertionText.Length
            : newCaretOffset;
        return true;
    }

    private bool TryNormalizeBranchIndent(TextDocument document, int lineNumber, ref int caretOffset)
    {
        var line = document.GetLineByNumber(lineNumber);
        var analysis = AnalyzeLine(GetLineText(document, line));

        if (analysis.BranchKind is null ||
            !TryFindExpectedBranchIndent(document, lineNumber, analysis.BranchKind.Value, out var expectedIndent) ||
            string.Equals(analysis.Indentation, expectedIndent, StringComparison.Ordinal))
        {
            return false;
        }

        var replacedLength = analysis.Indentation.Length;
        document.Replace(line.Offset, replacedLength, expectedIndent);

        var delta = expectedIndent.Length - replacedLength;
        var replacedEnd = line.Offset + replacedLength;

        if (caretOffset >= replacedEnd)
        {
            caretOffset += delta;
        }
        else if (caretOffset > line.Offset)
        {
            caretOffset = Math.Max(line.Offset, caretOffset + delta);
        }

        return true;
    }

    private bool TryFindExpectedBranchIndent(
        TextDocument document,
        int lineNumber,
        BlockBranchKind branchKind,
        out string indentation)
    {
        indentation = string.Empty;
        var expectedOpeningKind = branchKind switch
        {
            BlockBranchKind.ElseIf => BlockOpeningKind.If,
            BlockBranchKind.Else => BlockOpeningKind.If,
            BlockBranchKind.Case => BlockOpeningKind.SelectCase,
            BlockBranchKind.CaseElse => BlockOpeningKind.SelectCase,
            BlockBranchKind.Catch => BlockOpeningKind.Try,
            BlockBranchKind.Finally => BlockOpeningKind.Try,
            _ => throw new ArgumentOutOfRangeException(nameof(branchKind))
        };

        if (!TryFindInnermostOpenBlock(document, lineNumber, out var openBlock) ||
            openBlock.Kind != expectedOpeningKind)
        {
            return false;
        }

        indentation = branchKind switch
        {
            BlockBranchKind.Case => openBlock.Indentation + IndentUnit,
            BlockBranchKind.CaseElse => openBlock.Indentation + IndentUnit,
            _ => openBlock.Indentation
        };
        return true;
    }

    private bool TryFindInnermostOpenBlock(TextDocument document, int lineNumber, out OpenBlockContext openBlock)
    {
        var stack = new Stack<OpenBlockContext>();

        for (var currentLineNumber = 1; currentLineNumber < lineNumber; currentLineNumber++)
        {
            var analysis = AnalyzeLine(GetLineText(document, document.GetLineByNumber(currentLineNumber)));

            if (analysis.IsBlank)
            {
                continue;
            }

            if (analysis.OpeningKind is not null)
            {
                stack.Push(new OpenBlockContext(analysis.OpeningKind.Value, analysis.Indentation));
                continue;
            }

            if (analysis.ClosingKind is not null &&
                stack.Count > 0 &&
                ToClosingKind(stack.Peek().Kind) == analysis.ClosingKind.Value &&
                string.Equals(stack.Peek().Indentation, analysis.Indentation, StringComparison.Ordinal))
            {
                stack.Pop();
            }
        }

        if (stack.Count == 0)
        {
            openBlock = default;
            return false;
        }

        openBlock = stack.Peek();
        return true;
    }

    private bool HasMatchingClose(TextDocument document, int openingLineNumber, BlockOpeningKind openingKind)
    {
        var openingLine = document.GetLineByNumber(openingLineNumber);
        var openingAnalysis = AnalyzeLine(GetLineText(document, openingLine));
        var stack = new Stack<OpenBlockContext>();
        stack.Push(new OpenBlockContext(openingKind, openingAnalysis.Indentation));

        for (var lineNumber = openingLineNumber + 1; lineNumber <= document.LineCount; lineNumber++)
        {
            var analysis = AnalyzeLine(GetLineText(document, document.GetLineByNumber(lineNumber)));

            if (analysis.IsBlank)
            {
                continue;
            }

            if (analysis.OpeningKind is not null)
            {
                stack.Push(new OpenBlockContext(analysis.OpeningKind.Value, analysis.Indentation));
                continue;
            }

            if (analysis.ClosingKind is null || stack.Count == 0)
            {
                continue;
            }

            if (analysis.ClosingKind == ToClosingKind(stack.Peek().Kind) &&
                string.Equals(analysis.Indentation, stack.Peek().Indentation, StringComparison.Ordinal))
            {
                stack.Pop();

                if (stack.Count == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string FormatClosingText(BlockOpeningKind openingKind, CodeFormattingStyle style)
    {
        var canonicalClosing = openingKind switch
        {
            BlockOpeningKind.If => "END IF",
            BlockOpeningKind.For => "NEXT",
            BlockOpeningKind.While => "END WHILE",
            BlockOpeningKind.SelectCase => "END SELECT",
            BlockOpeningKind.Do => "LOOP",
            BlockOpeningKind.Sub => "END SUB",
            BlockOpeningKind.Function => "END FUNCTION",
            BlockOpeningKind.Type => "END TYPE",
            BlockOpeningKind.Try => "END TRY",
            _ => throw new ArgumentOutOfRangeException(nameof(openingKind))
        };

        return _formattingService.FormatLine(canonicalClosing, style);
    }

    private static BlockClosingKind ToClosingKind(BlockOpeningKind openingKind) =>
        openingKind switch
        {
            BlockOpeningKind.If => BlockClosingKind.EndIf,
            BlockOpeningKind.For => BlockClosingKind.Next,
            BlockOpeningKind.While => BlockClosingKind.EndWhile,
            BlockOpeningKind.SelectCase => BlockClosingKind.EndSelect,
            BlockOpeningKind.Do => BlockClosingKind.Loop,
            BlockOpeningKind.Sub => BlockClosingKind.EndSub,
            BlockOpeningKind.Function => BlockClosingKind.EndFunction,
            BlockOpeningKind.Type => BlockClosingKind.EndType,
            BlockOpeningKind.Try => BlockClosingKind.EndTry,
            _ => throw new ArgumentOutOfRangeException(nameof(openingKind))
        };

    private static int GetInsertionOffsetAfterLine(TextDocument document, int lineNumber)
    {
        if (lineNumber >= document.LineCount)
        {
            return document.GetLineByNumber(lineNumber).EndOffset;
        }

        return document.GetLineByNumber(lineNumber + 1).Offset;
    }

    private static bool IsBlankLine(TextDocument document, DocumentLine line) =>
        string.IsNullOrWhiteSpace(GetLineText(document, line));

    private static string GetLineText(TextDocument document, DocumentLine line) =>
        document.GetText(line.Offset, line.Length);

    private static string GetPreferredLineBreak(TextDocument document, DocumentLine primaryLine, DocumentLine? secondaryLine)
    {
        var primaryDelimiter = GetLineDelimiter(document, primaryLine);
        if (primaryDelimiter.Length > 0)
        {
            return primaryDelimiter;
        }

        if (secondaryLine is not null)
        {
            var secondaryDelimiter = GetLineDelimiter(document, secondaryLine);
            if (secondaryDelimiter.Length > 0)
            {
                return secondaryDelimiter;
            }
        }

        return Environment.NewLine;
    }

    private static string GetLineDelimiter(TextDocument document, DocumentLine line)
    {
        if (line.DelimiterLength <= 0)
        {
            return string.Empty;
        }

        return document.GetText(line.EndOffset, line.DelimiterLength);
    }

    private static LineAnalysis AnalyzeLine(string lineText)
    {
        var info = BlockLineClassifier.ClassifyLine(lineText);

        if (info.IsBlank)
        {
            return LineAnalysis.Blank;
        }

        return new LineAnalysis(
            info.Indentation,
            info.OpeningKind,
            info.ClosingKind,
            info.BranchKind,
            info.IncreasesIndentNextLine);
    }

    private readonly record struct LineAnalysis(
        string Indentation,
        BlockOpeningKind? OpeningKind = null,
        BlockClosingKind? ClosingKind = null,
        BlockBranchKind? BranchKind = null,
        bool IncreasesIndentNextLine = false,
        bool HasContent = true)
    {
        public bool IsBlank => !HasContent;

        public static readonly LineAnalysis Blank = new(string.Empty, HasContent: false);
    }

    private readonly record struct OpenBlockContext(BlockOpeningKind Kind, string Indentation);
}
