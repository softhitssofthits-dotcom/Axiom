namespace Axiom.Ide.Models;

public sealed record DocumentFileContent(string FilePath, string SourceText, bool IsReadOnly);
