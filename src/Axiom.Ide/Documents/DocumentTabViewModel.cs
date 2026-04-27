using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Documents;

/// <summary>
/// ViewModel одной вкладки редактора.
/// </summary>
public sealed class DocumentTabViewModel : ViewModelBase
{
    private string _displayName;
    private string? _filePath;
    private bool _isDirty;
    private bool _isUntitled;
    private bool _isReadOnly;
    private string _savedSourceText;
    private string _sourceText;
    private IReadOnlyList<int> _sourceBreakpointLines = [];

    public DocumentTabViewModel(string? filePath = null, string? initialText = null, bool isReadOnly = false)
    {
        _filePath = filePath;
        _sourceText = initialText ?? string.Empty;
        _savedSourceText = _sourceText;
        _isUntitled = string.IsNullOrWhiteSpace(filePath);
        _isReadOnly = isReadOnly;
        _displayName = ComputeDisplayName(filePath);
    }

    /// <summary>Путь к файлу, null для untitled документов.</summary>
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                IsUntitled = string.IsNullOrWhiteSpace(value);
                DisplayName = ComputeDisplayName(value);
            }
        }
    }

    /// <summary>Отображаемое имя вкладки.</summary>
    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    /// <summary>Текст документа.</summary>
    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
            {
                UpdateDirtyState();
            }
        }
    }

    /// <summary>Флаг несохранённых изменений.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    /// <summary>Флаг untitled документа.</summary>
    public bool IsUntitled
    {
        get => _isUntitled;
        private set => SetProperty(ref _isUntitled, value);
    }

    /// <summary>Флаг view-only документа (неподдерживаемый для редактирования формат).</summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    public IReadOnlyList<int> SourceBreakpointLines => _sourceBreakpointLines;

    public void SetSourceBreakpointLines(IEnumerable<int> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var normalized = NormalizeSourceBreakpointLines(lines);
        if (_sourceBreakpointLines.SequenceEqual(normalized))
        {
            return;
        }

        _sourceBreakpointLines = normalized;
        OnPropertyChanged(nameof(SourceBreakpointLines));
    }

    public void ClearSourceBreakpointLines()
    {
        SetSourceBreakpointLines([]);
    }

    public void ToggleSourceBreakpointLine(int line)
    {
        if (line <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "Source breakpoint line must be greater than zero.");
        }

        var lines = _sourceBreakpointLines.ToList();
        if (!lines.Remove(line))
        {
            lines.Add(line);
        }

        SetSourceBreakpointLines(lines);
    }

    public void MarkAsSaved(string? filePath = null)
    {
        if (filePath is not null)
        {
            FilePath = filePath;
        }

        _savedSourceText = _sourceText;
        IsDirty = false;
    }

    private void UpdateDirtyState()
    {
        IsDirty = !string.Equals(_sourceText, _savedSourceText, StringComparison.Ordinal);
    }

    private static string ComputeDisplayName(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "Untitled.axm";
        }

        return System.IO.Path.GetFileName(filePath);
    }

    private static IReadOnlyList<int> NormalizeSourceBreakpointLines(IEnumerable<int> lines)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();

        foreach (var line in lines)
        {
            if (line <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lines), line, "Source breakpoint line must be greater than zero.");
            }

            if (seen.Add(line))
            {
                result.Add(line);
            }
        }

        return result;
    }
}
