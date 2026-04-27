namespace Axiom.Ide.Editor.Navigation;

public sealed record DiagnosticNavigationTarget(
    int Offset,
    int Length,
    int Line,
    int Column);
