using System.Runtime.ExceptionServices;
using System.Threading;
using ICSharpCode.AvalonEdit;
using Axiom.Ide.Editor;

namespace Axiom.Ide.Tests;

public sealed class EditorFindReplaceEngineTests
{
    [Fact]
    public void TryFind_WithRegexPattern_SelectsRegexMatch()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("alpha demo beta");
            var options = new FindReplaceOptions("de.o", string.Empty, MatchCase: true, WholeWords: false, UseRegex: true, SearchDown: true);

            var found = EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var errorMessage);

            Assert.True(found);
            Assert.Null(errorMessage);
            Assert.Equal("demo", editor.SelectedText);
        });
    }

    [Fact]
    public void TryFind_WithInvalidRegexPattern_ReturnsError()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("alpha demo beta");
            var options = new FindReplaceOptions("[demo", string.Empty, MatchCase: true, WholeWords: false, UseRegex: true, SearchDown: true);

            var found = EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var errorMessage);

            Assert.False(found);
            Assert.True(string.IsNullOrWhiteSpace(editor.SelectedText));
            Assert.False(string.IsNullOrWhiteSpace(errorMessage));
        });
    }

    [Fact]
    public void TryReplaceCurrent_WithRegexReplacement_UsesCaptureGroups()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("value=42");
            var options = new FindReplaceOptions(@"(\w+)=(\d+)", "$2:$1", MatchCase: true, WholeWords: false, UseRegex: true, SearchDown: true);

            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var findError));
            Assert.Null(findError);

            var replaced = EditorFindReplaceEngine.TryReplaceCurrent(editor, options, out var replaceError);

            Assert.True(replaced);
            Assert.Null(replaceError);
            Assert.Equal("42:value", editor.Text);
            Assert.Equal("42:value", editor.SelectedText);
        });
    }

    [Fact]
    public void ReplaceAll_WithRegexPattern_ReplacesEveryMatch()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo smoke demo");
            var options = new FindReplaceOptions("d.mo", "item", MatchCase: true, WholeWords: false, UseRegex: true, SearchDown: true);

            var replacedCount = EditorFindReplaceEngine.ReplaceAll(editor, options, out var errorMessage);

            Assert.Equal(2, replacedCount);
            Assert.Null(errorMessage);
            Assert.Equal("item smoke item", editor.Text);
        });
    }

    [Fact]
    public void TryFind_BackwardFromSelection_FindsPreviousMatch()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("alpha demo beta demo");
            var options = new FindReplaceOptions("demo", string.Empty, MatchCase: true, WholeWords: false, UseRegex: false, SearchDown: false);

            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var firstError));
            Assert.Null(firstError);
            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var secondError));
            Assert.Null(secondError);

            var found = EditorFindReplaceEngine.TryFind(editor, options, searchForward: false, out var errorMessage);

            Assert.True(found);
            Assert.Null(errorMessage);
            Assert.Equal("demo", editor.SelectedText);
            Assert.Equal(6, editor.SelectionStart);
        });
    }

    [Fact]
    public void TryReplaceCurrent_WithLiteralPattern_ReplacesOnlyCurrentSelection()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo smoke demo");
            var options = new FindReplaceOptions("demo", "item", MatchCase: true, WholeWords: false, UseRegex: false, SearchDown: true);

            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var findError));
            Assert.Null(findError);

            var replaced = EditorFindReplaceEngine.TryReplaceCurrent(editor, options, out var replaceError);

            Assert.True(replaced);
            Assert.Null(replaceError);
            Assert.Equal("item smoke demo", editor.Text);
            Assert.Equal("item", editor.SelectedText);
            Assert.Equal(0, editor.SelectionStart);
        });
    }

    [Fact]
    public void ReplaceAll_WithWholeWords_DoesNotReplaceInsideLongerWords()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo demography demo");
            var options = new FindReplaceOptions("demo", "item", MatchCase: true, WholeWords: true, UseRegex: false, SearchDown: true);

            var replacedCount = EditorFindReplaceEngine.ReplaceAll(editor, options, out var errorMessage);

            Assert.Equal(2, replacedCount);
            Assert.Null(errorMessage);
            Assert.Equal("item demography item", editor.Text);
        });
    }

    [Fact]
    public void TryFind_WithCaseInsensitiveLiteralPattern_FindsMatch()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("alpha Demo beta");
            var options = new FindReplaceOptions("demo", string.Empty, MatchCase: false, WholeWords: false, UseRegex: false, SearchDown: true);

            var found = EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var errorMessage);

            Assert.True(found);
            Assert.Null(errorMessage);
            Assert.Equal("Demo", editor.SelectedText);
        });
    }

    private static TextEditor CreateEditor(string text)
    {
        var editor = new TextEditor
        {
            Text = text
        };

        editor.Select(0, 0);
        editor.CaretOffset = 0;
        return editor;
    }

    private static void RunInSta(Action action)
    {
        Exception? capturedException = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                capturedException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (capturedException is not null)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }
    }
}
