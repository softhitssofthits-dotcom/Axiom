using System;
using System.IO;
using Axiom.Ide.Documents;

namespace Axiom.Ide.Editor.Formatting;

public static class DocumentFormattingPolicy
{
    public static bool CanApplyFormatting(DocumentTabViewModel? document)
    {
        return document is not null
            && !document.IsReadOnly
            && (document.IsUntitled
                || (!string.IsNullOrWhiteSpace(document.FilePath)
                    && string.Equals(Path.GetExtension(document.FilePath), ".axm", StringComparison.OrdinalIgnoreCase)));
    }
}
