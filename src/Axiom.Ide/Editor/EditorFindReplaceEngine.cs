using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit;

namespace Axiom.Ide.Editor;

public static class EditorFindReplaceEngine
{
    public static bool TryFind(TextEditor editor, FindReplaceOptions options, bool searchForward, out string? errorMessage)
    {
        errorMessage = null;
        var regex = TryCreateRegex(options, out errorMessage);

        if (regex is null)
        {
            return false;
        }

        var text = editor.Document?.Text ?? string.Empty;

        if (text.Length == 0)
        {
            return false;
        }

        var startOffset = GetSearchStartOffset(editor, searchForward);
        var match = searchForward
            ? FindForward(regex, text, startOffset)
            : FindBackward(regex, text, startOffset);

        if (match is null)
        {
            return false;
        }

        SelectMatch(editor, match.Index, match.Length, searchForward);
        return true;
    }

    public static bool TryReplaceCurrent(TextEditor editor, FindReplaceOptions options, out string? errorMessage)
    {
        errorMessage = null;
        var document = editor.Document;
        var regex = TryCreateRegex(options, out errorMessage);

        if (regex is null || document is null || editor.SelectionLength == 0)
        {
            return false;
        }

        var selectionStart = editor.SelectionStart;
        var selectedText = editor.SelectedText;
        var selectedMatch = regex.Match(selectedText);

        if (!selectedMatch.Success || selectedMatch.Index != 0 || selectedMatch.Length != selectedText.Length)
        {
            return false;
        }

        try
        {
            var replacementText = options.UseRegex
                ? selectedMatch.Result(options.ReplacePattern)
                : options.ReplacePattern;

            document.Replace(selectionStart, editor.SelectionLength, replacementText);
            editor.Select(selectionStart, replacementText.Length);
            editor.CaretOffset = selectionStart + replacementText.Length;
            editor.TextArea.Caret.BringCaretToView();
            return true;
        }
        catch (ArgumentException exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public static int ReplaceAll(TextEditor editor, FindReplaceOptions options, out string? errorMessage)
    {
        errorMessage = null;
        var document = editor.Document;
        var regex = TryCreateRegex(options, out errorMessage);

        if (regex is null || document is null)
        {
            return 0;
        }

        var text = document.Text;

        if (text.Length == 0)
        {
            return 0;
        }

        var replacements = 0;

        try
        {
            var replacedText = regex.Replace(
                text,
                match =>
                {
                    replacements++;
                    return options.UseRegex
                        ? match.Result(options.ReplacePattern)
                        : options.ReplacePattern;
                });

            if (replacements > 0)
            {
                document.Text = replacedText;
                editor.Select(0, 0);
            }

            return replacements;
        }
        catch (ArgumentException exception)
        {
            errorMessage = exception.Message;
            return 0;
        }
    }

    private static Regex? TryCreateRegex(FindReplaceOptions options, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(options.SearchPattern))
        {
            return null;
        }

        var pattern = options.UseRegex
            ? options.SearchPattern
            : Regex.Escape(options.SearchPattern);

        if (options.WholeWords)
        {
            pattern = $@"\b(?:{pattern})\b";
        }

        var regexOptions = RegexOptions.Multiline | RegexOptions.CultureInvariant;

        if (!options.MatchCase)
        {
            regexOptions |= RegexOptions.IgnoreCase;
        }

        try
        {
            return new Regex(pattern, regexOptions);
        }
        catch (ArgumentException exception)
        {
            errorMessage = exception.Message;
            return null;
        }
    }

    private static int GetSearchStartOffset(TextEditor editor, bool searchForward)
    {
        if (editor.SelectionLength <= 0)
        {
            return editor.CaretOffset;
        }

        return searchForward
            ? editor.SelectionStart + editor.SelectionLength
            : editor.SelectionStart;
    }

    private static Match? FindForward(Regex regex, string text, int startOffset)
    {
        startOffset = Math.Clamp(startOffset, 0, text.Length);

        var match = regex.Match(text, startOffset);

        if (match.Success)
        {
            return match;
        }

        if (startOffset == 0)
        {
            return null;
        }

        var wrappedMatch = regex.Match(text, 0);
        return wrappedMatch.Success ? wrappedMatch : null;
    }

    private static Match? FindBackward(Regex regex, string text, int startOffset)
    {
        startOffset = Math.Clamp(startOffset, 0, text.Length);
        var matches = regex.Matches(text);

        if (matches.Count == 0)
        {
            return null;
        }

        for (var index = matches.Count - 1; index >= 0; index--)
        {
            var candidate = matches[index];

            if (candidate.Index < startOffset)
            {
                return candidate;
            }
        }

        return matches[matches.Count - 1];
    }

    private static void SelectMatch(TextEditor editor, int offset, int length, bool searchForward)
    {
        editor.Select(offset, length);
        editor.CaretOffset = searchForward ? offset + length : offset;
        editor.TextArea.Caret.BringCaretToView();
        editor.Focus();
    }
}
