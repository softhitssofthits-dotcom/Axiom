using ICSharpCode.AvalonEdit;

namespace Axiom.Ide.Editor;

internal enum FindReplaceWorkflowResult
{
    MatchFound = 0,
    MatchNotFound = 1,
    ReplacedAndFoundNext = 2,
    ReplacedAndNoMoreMatches = 3,
    Error = 4
}

internal readonly record struct FindReplaceWorkflowOutcome(
    FindReplaceWorkflowResult Result,
    string? ErrorMessage = null);

internal static class EditorFindReplaceWorkflow
{
    public static FindReplaceWorkflowOutcome Find(TextEditor editor, FindReplaceOptions options, bool searchForward)
    {
        return EditorFindReplaceEngine.TryFind(editor, options, searchForward, out var errorMessage)
            ? new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.MatchFound)
            : string.IsNullOrWhiteSpace(errorMessage)
                ? new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.MatchNotFound)
                : new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.Error, errorMessage);
    }

    public static FindReplaceWorkflowOutcome Replace(TextEditor editor, FindReplaceOptions options, bool searchForward)
    {
        var replaced = EditorFindReplaceEngine.TryReplaceCurrent(editor, options, out var replaceError);

        if (!replaced)
        {
            if (!string.IsNullOrWhiteSpace(replaceError))
            {
                return new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.Error, replaceError);
            }

            return Find(editor, options, searchForward);
        }

        var findOutcome = Find(editor, options, searchForward);

        return findOutcome.Result switch
        {
            FindReplaceWorkflowResult.MatchFound => new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.ReplacedAndFoundNext),
            FindReplaceWorkflowResult.MatchNotFound => new FindReplaceWorkflowOutcome(FindReplaceWorkflowResult.ReplacedAndNoMoreMatches),
            _ => findOutcome
        };
    }
}
