using System.Runtime.ExceptionServices;
using System.Threading;
using Axiom.Ide.Editor;
using ICSharpCode.AvalonEdit;

namespace Axiom.Ide.Tests;

public sealed class EditorFindReplaceWorkflowTests
{
    [Fact]
    public void Replace_WithoutCurrentMatch_SelectsNextMatchWithoutReplacing()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo smoke demo");
            var options = new FindReplaceOptions("demo", "item", MatchCase: true, WholeWords: false, UseRegex: false, SearchDown: true);

            var outcome = EditorFindReplaceWorkflow.Replace(editor, options, searchForward: true);

            Assert.Equal(FindReplaceWorkflowResult.MatchFound, outcome.Result);
            Assert.Null(outcome.ErrorMessage);
            Assert.Equal("demo smoke demo", editor.Text);
            Assert.Equal("demo", editor.SelectedText);
            Assert.Equal(0, editor.SelectionStart);
        });
    }

    [Fact]
    public void Replace_WithCurrentMatch_ReplacesAndFindsNextMatch()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo smoke demo");
            var options = new FindReplaceOptions("demo", "item", MatchCase: true, WholeWords: false, UseRegex: false, SearchDown: true);

            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var errorMessage));
            Assert.Null(errorMessage);

            var outcome = EditorFindReplaceWorkflow.Replace(editor, options, searchForward: true);

            Assert.Equal(FindReplaceWorkflowResult.ReplacedAndFoundNext, outcome.Result);
            Assert.Null(outcome.ErrorMessage);
            Assert.Equal("item smoke demo", editor.Text);
            Assert.Equal("demo", editor.SelectedText);
            Assert.Equal(11, editor.SelectionStart);
        });
    }

    [Fact]
    public void Replace_WithLastMatch_ReplacesAndReportsNoMoreMatches()
    {
        RunInSta(() =>
        {
            var editor = CreateEditor("demo");
            var options = new FindReplaceOptions("demo", "item", MatchCase: true, WholeWords: false, UseRegex: false, SearchDown: true);

            Assert.True(EditorFindReplaceEngine.TryFind(editor, options, searchForward: true, out var errorMessage));
            Assert.Null(errorMessage);

            var outcome = EditorFindReplaceWorkflow.Replace(editor, options, searchForward: true);

            Assert.Equal(FindReplaceWorkflowResult.ReplacedAndNoMoreMatches, outcome.Result);
            Assert.Null(outcome.ErrorMessage);
            Assert.Equal("item", editor.Text);
            Assert.Equal("item", editor.SelectedText);
            Assert.Equal(0, editor.SelectionStart);
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
