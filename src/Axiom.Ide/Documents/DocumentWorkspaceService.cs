using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Axiom.Ide.Documents;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Documents;
using Axiom.Ide.Services.Persistence;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Documents;

/// <summary>
/// Сервис управления вкладками документов: open, save, close, dedupe.
/// </summary>
public sealed class DocumentWorkspaceService : ViewModelBase
{
    private readonly IDocumentFileService _documentFileService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly Func<IdeLanguage> _languageProvider;
    private DocumentTabViewModel? _activeDocument;
    private int _untitledCounter;

    public DocumentWorkspaceService(
        IDocumentFileService documentFileService,
        IRecentFilesService recentFilesService,
        Func<IdeLanguage>? languageProvider = null)
    {
        _documentFileService = documentFileService;
        _recentFilesService = recentFilesService;
        _languageProvider = languageProvider ?? (() => IdeLanguage.Russian);
        _untitledCounter = 0;
        Documents = [];
    }

    public ObservableCollection<DocumentTabViewModel> Documents { get; }

    public DocumentTabViewModel? ActiveDocument
    {
        get => _activeDocument;
        set => SetProperty(ref _activeDocument, value);
    }

    /// <summary>
    /// Возникает после открытия нового документа.
    /// Повторная активация уже открытой вкладки событие не вызывает.
    /// </summary>
    public event Action<DocumentTabViewModel>? DocumentOpened;

    /// <summary>
    /// Создаёт новую пустую вкладку и делает её активной.
    /// </summary>
    public DocumentTabViewModel CreateNewDocument()
    {
        _untitledCounter++;
        var document = new DocumentTabViewModel();

        Documents.Add(document);
        ActiveDocument = document;
        return document;
    }

    /// <summary>
    /// Открывает файл в новой вкладке или активирует существующую.
    /// </summary>
    public DocumentTabViewModel? OpenDocument(string? initialDirectoryPath, out string? statusMessage)
    {
        statusMessage = null;

        try
        {
            var documentContent = _documentFileService.OpenDocument(initialDirectoryPath);

            if (documentContent is null)
            {
                statusMessage = GetLocalizedString("Documents.Open.StatusCancelled", "Open cancelled.");
                return null;
            }

            var document = OpenDocumentCore(documentContent);
            statusMessage = BuildOpenStatusMessage(documentContent.FilePath, document.IsReadOnly);
            return document;
        }
        catch (Exception exception)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Open.StatusFailed",
                "Open failed: {0}",
                exception.Message);
            return null;
        }
    }

    public DocumentTabViewModel? OpenDocumentFromPath(string filePath, out string? statusMessage)
    {
        statusMessage = null;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            statusMessage = GetLocalizedString("Documents.Open.StatusCancelled", "Open cancelled.");
            return null;
        }

        try
        {
            var documentContent = _documentFileService.LoadDocument(filePath);
            var document = OpenDocumentCore(documentContent);
            statusMessage = BuildOpenStatusMessage(documentContent.FilePath, document.IsReadOnly);
            return document;
        }
        catch (Exception exception)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Open.StatusFailed",
                "Open failed: {0}",
                exception.Message);
            return null;
        }
    }

    /// <summary>
    /// Сохраняет документ. Для untitled — Save As.
    /// </summary>
    public bool SaveDocument(DocumentTabViewModel document, out string? statusMessage)
    {
        statusMessage = null;

        if (document.IsReadOnly)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusReadOnly",
                "'{0}' is opened in view-only mode.",
                document.DisplayName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            return SaveDocumentAs(document, out statusMessage);
        }

        try
        {
            var filePath = _documentFileService.SaveDocument(document.FilePath, document.SourceText, null);

            if (filePath is null)
            {
                statusMessage = GetLocalizedString("Documents.Save.StatusCancelled", "Save cancelled.");
                return false;
            }

            document.MarkAsSaved(filePath);
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusSaved",
                "Saved {0}.",
                Path.GetFileName(filePath));
            RememberFile(filePath);
            return true;
        }
        catch (Exception exception)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusFailed",
                "Save failed: {0}",
                exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Save As — всегда показывает диалог.
    /// </summary>
    public bool SaveDocumentAs(DocumentTabViewModel document, out string? statusMessage)
    {
        statusMessage = null;

        if (document.IsReadOnly)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusReadOnly",
                "'{0}' is opened in view-only mode.",
                document.DisplayName);
            return false;
        }

        try
        {
            var suggestedDir = GetSuggestedDirectoryPath(document.FilePath);
            var filePath = _documentFileService.SaveDocument(null, document.SourceText, suggestedDir);

            if (filePath is null)
            {
                statusMessage = GetLocalizedString("Documents.Save.StatusCancelled", "Save cancelled.");
                return false;
            }

            document.MarkAsSaved(filePath);
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusSaved",
                "Saved {0}.",
                Path.GetFileName(filePath));
            RememberFile(filePath);
            return true;
        }
        catch (Exception exception)
        {
            statusMessage = GetLocalizedFormattedString(
                "Documents.Save.StatusFailed",
                "Save failed: {0}",
                exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Сохраняет все dirty вкладки.
    /// </summary>
    public bool SaveAllDocuments(out string? statusMessage)
    {
        statusMessage = null;
        var allSaved = true;

        foreach (var document in Documents.ToList())
        {
            if (!document.IsDirty || document.IsReadOnly)
            {
                continue;
            }

            if (!SaveDocument(document, out var msg))
            {
                allSaved = false;
                statusMessage = msg;
                break;
            }
        }

        if (allSaved)
        {
            statusMessage = Documents.Count > 0
                ? GetLocalizedString("Documents.SaveAll.StatusSaved", "All documents saved.")
                : GetLocalizedString("Documents.SaveAll.StatusNothing", "No documents to save.");
        }

        return allSaved;
    }

    /// <summary>
    /// Закрывает вкладку. Если dirty — требует подтверждения через prompt.
    /// </summary>
    public bool CloseDocument(DocumentTabViewModel document, Func<string, bool?> promptSaveDiscardCancel)
    {
        if (document.IsDirty)
        {
            var fileName = document.DisplayName;
            var result = promptSaveDiscardCancel(fileName);

            if (result is null)
            {
                return false; // cancel
            }

            if (result == true)
            {
                // save
                var saved = SaveDocument(document, out _);

                if (!saved)
                {
                    return false; // save cancelled or failed
                }
            }

            // result == false means discard — продолжаем закрытие
        }

        Documents.Remove(document);

        if (ActiveDocument == document)
        {
            ActiveDocument = Documents.LastOrDefault();

            if (ActiveDocument is null)
            {
                // Если последняя вкладка закрыта — создаём новую
                CreateNewDocument();
            }
        }

        return true;
    }

    /// <summary>
    /// Закрывает все вкладки с проверкой dirty state.
    /// </summary>
    public bool CloseAllDocuments(Func<string, bool?> promptSaveDiscardCancel)
    {
        foreach (var document in Documents.ToList())
        {
            var closed = CloseDocument(document, promptSaveDiscardCancel);

            if (!closed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Возвращает suggested directory path для file dialogs.
    /// </summary>
    public string? GetSuggestedDirectoryPath(string? currentFilePath)
    {
        var currentDirectoryPath = currentFilePath is null
            ? null
            : Path.GetDirectoryName(currentFilePath);

        if (!string.IsNullOrWhiteSpace(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        var snapshot = _recentFilesService.GetSnapshot();
        return IdeWorkspaceDefaults.ResolveRootPath(snapshot.LastDirectoryPath);
    }

    /// <summary>
    /// Запоминает файл в recent files.
    /// </summary>
    public void RememberFile(string filePath)
    {
        _recentFilesService.RememberFile(filePath);
    }

    private string BuildOpenStatusMessage(string filePath, bool isReadOnly)
    {
        var fileName = Path.GetFileName(filePath);
        return isReadOnly
            ? GetLocalizedFormattedString(
                "Documents.Open.StatusOpenedReadOnly",
                "Opened {0} in view-only mode.",
                fileName)
            : GetLocalizedFormattedString(
                "Documents.Open.StatusOpened",
                "Opened {0}.",
                fileName);
    }

    private string GetLocalizedString(string key, string fallback)
    {
        return IdeLanguageResourceService.GetString(_languageProvider(), key, fallback);
    }

    private string GetLocalizedFormattedString(string key, string fallbackFormat, params object?[] args)
    {
        return string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            GetLocalizedString(key, fallbackFormat),
            args);
    }

    private DocumentTabViewModel OpenDocumentCore(DocumentFileContent content)
    {
        // Dedupe: если файл уже открыт, активируем существующую вкладку
        var existing = Documents.FirstOrDefault(d =>
            d.FilePath != null &&
            d.FilePath.Equals(content.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            ActiveDocument = existing;
            return existing;
        }

        var document = new DocumentTabViewModel(content.FilePath, content.SourceText, content.IsReadOnly);

        Documents.Add(document);
        ActiveDocument = document;
        RememberFile(content.FilePath);
        DocumentOpened?.Invoke(document);
        return document;
    }
}
