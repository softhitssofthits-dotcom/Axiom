using Axiom.Ide.Models;

namespace Axiom.Ide.Services.Documents;

public interface IDocumentFileService
{
    DocumentFileContent? OpenDocument(string? initialDirectoryPath);

    DocumentFileContent LoadDocument(string filePath);

    string? SaveDocument(string? currentFilePath, string sourceText, string? initialDirectoryPath);
}
