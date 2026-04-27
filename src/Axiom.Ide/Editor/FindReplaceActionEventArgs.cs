namespace Axiom.Ide.Editor;

public enum FindReplaceAction
{
    FindNext = 0,
    Replace = 1,
    ReplaceAll = 2
}

public sealed class FindReplaceActionEventArgs : EventArgs
{
    public FindReplaceActionEventArgs(FindReplaceAction action, FindReplaceOptions options)
    {
        Action = action;
        Options = options;
    }

    public FindReplaceAction Action { get; }

    public FindReplaceOptions Options { get; }
}
