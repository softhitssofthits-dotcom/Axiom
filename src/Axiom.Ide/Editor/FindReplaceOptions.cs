namespace Axiom.Ide.Editor;

public sealed record FindReplaceOptions(
    string SearchPattern,
    string ReplacePattern,
    bool MatchCase,
    bool WholeWords,
    bool UseRegex,
    bool SearchDown)
{
    public static FindReplaceOptions Default { get; } =
        new(string.Empty, string.Empty, MatchCase: false, WholeWords: false, UseRegex: false, SearchDown: true);
}
