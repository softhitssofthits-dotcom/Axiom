using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.Editor.StructuralEditing;

public sealed class AxiomBlockEditingService
{
    private const string IndentUnit = "    ";

    private static readonly Regex IfHeaderRegex = new(@"^IF\b.+\bTHEN$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ElseIfHeaderRegex = new(@"^ELSEIF\b.+\bTHEN$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ElseHeaderRegex = new(@"^ELSE$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ForHeaderRegex = new(@"^FOR\b.+\bTO\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex WhileHeaderRegex = new(@"^WHILE\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SelectCaseHeaderRegex = new(@"^SELECT\s+CASE\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DoWhileHeaderRegex = new(@"^DO\s+WHILE\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DoUntilHeaderRegex = new(@"^DO\s+UNTIL\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex DoHeaderRegex = new(@"^DO$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SubHeaderRegex = new(@"^SUB\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex FunctionHeaderRegex = new(@"^FUNCTION\b.+\bAS\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex TypeHeaderRegex = new(@"^TYPE\s+[\p{L}_][\p{L}\p{Nd}_]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex CaseElseHeaderRegex = new(@"^CASE\s+ELSE$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex CaseHeaderRegex = new(@"^CASE\b.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex EndIfRegex = new(@"^END\s+IF$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NextRegex = new(@"^NEXT(?:\b.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndWhileRegex = new(@"^END\s+WHILE$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndSelectRegex = new(@"^END\s+SELECT$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LoopRegex = new(@"^LOOP(?:\s+(?:WHILE|UNTIL)\b.+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndSubRegex = new(@"^END\s+SUB$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndFunctionRegex = new(@"^END\s+FUNCTION$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndTypeRegex = new(@"^END\s+TYPE$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
        BranchLineKind branchKind,
        out string indentation)
    {
        indentation = string.Empty;
        var expectedOpeningKind = branchKind switch
        {
            BranchLineKind.ElseIf => BlockOpeningKind.If,
            BranchLineKind.Else => BlockOpeningKind.If,
            BranchLineKind.Case => BlockOpeningKind.SelectCase,
            BranchLineKind.CaseElse => BlockOpeningKind.SelectCase,
            _ => throw new ArgumentOutOfRangeException(nameof(branchKind))
        };

        if (!TryFindInnermostOpenBlock(document, lineNumber, out var openBlock) ||
            openBlock.Kind != expectedOpeningKind)
        {
            return false;
        }

        indentation = branchKind switch
        {
            BranchLineKind.Case => openBlock.Indentation + IndentUnit,
            BranchLineKind.CaseElse => openBlock.Indentation + IndentUnit,
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
        if (lineText.Length == 0)
        {
            return LineAnalysis.Blank();
        }

        var indentLength = 0;
        while (indentLength < lineText.Length && char.IsWhiteSpace(lineText[indentLength]))
        {
            indentLength++;
        }

        var indentation = lineText[..indentLength];
        var body = StripComments(lineText[indentLength..]).Trim();

        if (body.Length == 0)
        {
            return LineAnalysis.Blank();
        }

        if (ElseIfHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BranchKind: BranchLineKind.ElseIf, IncreasesIndentNextLine: true);
        }

        if (ElseHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BranchKind: BranchLineKind.Else, IncreasesIndentNextLine: true);
        }

        if (CaseElseHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BranchKind: BranchLineKind.CaseElse, IncreasesIndentNextLine: true);
        }

        if (CaseHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BranchKind: BranchLineKind.Case, IncreasesIndentNextLine: true);
        }

        if (IfHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.If, IncreasesIndentNextLine: true);
        }

        if (ForHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.For, IncreasesIndentNextLine: true);
        }

        if (WhileHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.While, IncreasesIndentNextLine: true);
        }

        if (SelectCaseHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.SelectCase, IncreasesIndentNextLine: true);
        }

        if (DoWhileHeaderRegex.IsMatch(body) || DoUntilHeaderRegex.IsMatch(body) || DoHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.Do, IncreasesIndentNextLine: true);
        }

        if (FunctionHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.Function, IncreasesIndentNextLine: true);
        }

        if (TypeHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.Type, IncreasesIndentNextLine: true);
        }

        if (SubHeaderRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, BlockOpeningKind.Sub, IncreasesIndentNextLine: true);
        }

        if (EndIfRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndIf);
        }

        if (EndWhileRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndWhile);
        }

        if (EndSelectRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndSelect);
        }

        if (EndSubRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndSub);
        }

        if (EndFunctionRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndFunction);
        }

        if (EndTypeRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.EndType);
        }

        if (NextRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.Next);
        }

        if (LoopRegex.IsMatch(body))
        {
            return new LineAnalysis(indentation, ClosingKind: BlockClosingKind.Loop);
        }

        return new LineAnalysis(indentation);
    }

    private static string StripComments(string text)
    {
        var inString = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];

            if (current == '"')
            {
                if (inString && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '\'')
            {
                return text[..index];
            }

            if (StartsRemComment(text, index))
            {
                return text[..index];
            }
        }

        return text;
    }

    private static bool StartsRemComment(string text, int index)
    {
        if (index + 2 >= text.Length)
        {
            return false;
        }

        if (!text[index..(index + 3)].Equals("REM", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hasLeadingBoundary = index == 0 || char.IsWhiteSpace(text[index - 1]);
        var afterIndex = index + 3;
        var hasTrailingBoundary = afterIndex >= text.Length || char.IsWhiteSpace(text[afterIndex]);
        return hasLeadingBoundary && hasTrailingBoundary;
    }

    private readonly record struct LineAnalysis(
        string Indentation,
        BlockOpeningKind? OpeningKind = null,
        BlockClosingKind? ClosingKind = null,
        BranchLineKind? BranchKind = null,
        bool IncreasesIndentNextLine = false,
        bool HasContent = true)
    {
        public bool IsBlank => !HasContent;

        public static LineAnalysis Blank() => new(string.Empty, HasContent: false);
    }

    private readonly record struct OpenBlockContext(BlockOpeningKind Kind, string Indentation);

    private enum BlockOpeningKind
    {
        If,
        For,
        While,
        SelectCase,
        Do,
        Sub,
        Function,
        Type
    }

    private enum BlockClosingKind
    {
        EndIf,
        Next,
        EndWhile,
        EndSelect,
        Loop,
        EndSub,
        EndFunction,
        EndType
    }

    private enum BranchLineKind
    {
        ElseIf,
        Else,
        Case,
        CaseElse
    }
}
