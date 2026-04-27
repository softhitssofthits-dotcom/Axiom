using System.IO;
using System.Text;
using Microsoft.Win32;
using Axiom.Ide.Models;

namespace Axiom.Ide.Services.Documents;

public sealed class DocumentFileService : IDocumentFileService
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe"
    };

    public DocumentFileContent? OpenDocument(string? initialDirectoryPath)
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".axm",
            Filter = "Axiom Files (*.axm)|*.axm|Text Files (*.txt)|*.txt|Markdown Files (*.md)|*.md|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        ApplyInitialDirectory(dialog, initialDirectoryPath);

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return LoadDocument(dialog.FileName);
    }

    public DocumentFileContent LoadDocument(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var extension = Path.GetExtension(filePath);

        if (BlockedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Executable .exe files are not supported in editor.");
        }

        var sourceText = File.ReadAllText(filePath, Encoding.UTF8);
        return new DocumentFileContent(filePath, sourceText, IsReadOnly: false);
    }

    public string? SaveDocument(string? currentFilePath, string sourceText, string? initialDirectoryPath)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        var targetFilePath = currentFilePath;

        if (string.IsNullOrWhiteSpace(targetFilePath))
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".axm",
                Filter = "Axiom Files (*.axm)|*.axm|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true
            };

            ApplyInitialDirectory(dialog, initialDirectoryPath);

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            targetFilePath = dialog.FileName;
        }

        File.WriteAllText(targetFilePath, sourceText, Encoding.UTF8);
        return targetFilePath;
    }

    private static void ApplyInitialDirectory(FileDialog dialog, string? initialDirectoryPath)
    {
        if (!string.IsNullOrWhiteSpace(initialDirectoryPath) && Directory.Exists(initialDirectoryPath))
        {
            dialog.InitialDirectory = initialDirectoryPath;
        }
    }
}
