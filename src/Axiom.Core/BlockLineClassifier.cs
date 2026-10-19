namespace Axiom.Core;

public static class BlockLineClassifier
{
    public static BlockLineInfo ClassifyLine(string lineText)
    {
        if (lineText.Length == 0)
        {
            return BlockLineInfo.Blank;
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
            return BlockLineInfo.Blank;
        }

        var upper = body.ToUpperInvariant();
        if (StartsWithWord(upper, "PUBLIC") || StartsWithWord(upper, "PRIVATE"))
        {
            var modifierLength = StartsWithWord(upper, "PUBLIC") ? 6 : 7;
            upper = upper[modifierLength..].TrimStart();
        }

        if (StartsWithWord(upper, "ELSEIF") && EndsWithWord(upper, "THEN"))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.ElseIf, IncreasesIndentNextLine: true);
        }

        if (string.Equals(upper, "ELSE", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.Else, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "CASE") && StartsWithWord(body.ToUpperInvariant()[4..].TrimStart(), "ELSE"))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.CaseElse, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "CASE"))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.Case, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "CATCH") && body.Length > 5 && !char.IsLetterOrDigit(body[5]))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.Catch, IncreasesIndentNextLine: true);
        }

        if (string.Equals(upper, "FINALLY", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, BranchKind: BlockBranchKind.Finally, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "IF") && EndsWithWord(upper, "THEN"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.If, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "FOR") && ContainsWord(upper, "TO"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.For, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "WHILE"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.While, IncreasesIndentNextLine: true);
        }

        if (StartsWithTwoWords(upper, "SELECT", "CASE"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.SelectCase, IncreasesIndentNextLine: true);
        }

        if (StartsWithTwoWords(upper, "DO", "WHILE"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Do, IncreasesIndentNextLine: true);
        }

        if (StartsWithTwoWords(upper, "DO", "UNTIL"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Do, IncreasesIndentNextLine: true);
        }

        if (string.Equals(upper, "DO", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Do, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "FUNCTION") && ContainsWord(upper, "AS"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Function, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "TYPE") && !StartsWithTwoWords(upper, "TYPE", "OF"))
        {
            var afterType = upper[4..].TrimStart();
            if (afterType.Length > 0 && (char.IsLetter(afterType[0]) || afterType[0] == '_'))
            {
                return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Type, IncreasesIndentNextLine: true);
            }
        }

        if (string.Equals(upper, "TRY", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Try, IncreasesIndentNextLine: true);
        }

        if (StartsWithWord(upper, "SUB"))
        {
            return new BlockLineInfo(indentation, OpeningKind: BlockOpeningKind.Sub, IncreasesIndentNextLine: true);
        }

        if (string.Equals(upper, "END IF", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndIf);
        }

        if (string.Equals(upper, "END WHILE", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndWhile);
        }

        if (string.Equals(upper, "END SELECT", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndSelect);
        }

        if (string.Equals(upper, "END SUB", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndSub);
        }

        if (string.Equals(upper, "END FUNCTION", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndFunction);
        }

        if (string.Equals(upper, "END TYPE", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndType);
        }

        if (string.Equals(upper, "END TRY", StringComparison.Ordinal))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.EndTry);
        }

        if (StartsWithWord(upper, "NEXT"))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.Next);
        }

        if (StartsWithWord(upper, "LOOP"))
        {
            return new BlockLineInfo(indentation, ClosingKind: BlockClosingKind.Loop);
        }

        return new BlockLineInfo(indentation);
    }

    private static bool StartsWithWord(string text, string word)
    {
        if (text.Length < word.Length)
        {
            return false;
        }

        if (!text.StartsWith(word, StringComparison.Ordinal))
        {
            return false;
        }

        return text.Length == word.Length || !char.IsLetterOrDigit(text[word.Length]);
    }

    private static bool EndsWithWord(string text, string word)
    {
        if (text.Length < word.Length)
        {
            return false;
        }

        if (!text.EndsWith(word, StringComparison.Ordinal))
        {
            return false;
        }

        var before = text.Length - word.Length;
        return before == 0 || !char.IsLetterOrDigit(text[before - 1]);
    }

    private static bool ContainsWord(string text, string word)
    {
        var index = text.IndexOf(word, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var afterOk = index + word.Length >= text.Length || !char.IsLetterOrDigit(text[index + word.Length]);

            if (beforeOk && afterOk)
            {
                return true;
            }

            index = text.IndexOf(word, index + 1, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool StartsWithTwoWords(string text, string first, string second)
    {
        if (!StartsWithWord(text, first))
        {
            return false;
        }

        var rest = text[first.Length..].TrimStart();
        return StartsWithWord(rest, second);
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

            if (index + 3 <= text.Length &&
                text[index..].StartsWith("REM", StringComparison.OrdinalIgnoreCase) &&
                (index == 0 || char.IsWhiteSpace(text[index - 1])) &&
                (index + 3 >= text.Length || char.IsWhiteSpace(text[index + 3])))
            {
                return text[..index];
            }
        }

        return text;
    }
}

public readonly record struct BlockLineInfo(
    string Indentation,
    BlockOpeningKind? OpeningKind = null,
    BlockClosingKind? ClosingKind = null,
    BlockBranchKind? BranchKind = null,
    bool IncreasesIndentNextLine = false,
    bool HasContent = true)
{
    public bool IsBlank => !HasContent;

    public static readonly BlockLineInfo Blank = new(string.Empty, HasContent: false);
}

public enum BlockOpeningKind
{
    If,
    For,
    While,
    SelectCase,
    Do,
    Sub,
    Function,
    Type,
    Try
}

public enum BlockClosingKind
{
    EndIf,
    Next,
    EndWhile,
    EndSelect,
    Loop,
    EndSub,
    EndFunction,
    EndType,
    EndTry
}

public enum BlockBranchKind
{
    ElseIf,
    Else,
    Case,
    CaseElse,
    Catch,
    Finally
}
