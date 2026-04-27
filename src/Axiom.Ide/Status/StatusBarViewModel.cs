using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Status;

/// <summary>
/// ViewModel статус бара: line/column, path, dirty state, run state.
/// </summary>
public sealed class StatusBarViewModel : ViewModelBase
{
    private string _statusText;
    private string _lineColumnText;
    private string _pathText;
    private bool _isRunning;
    private bool _isVisible;
    private bool _isCompilationProgressVisible;
    private int _compilationProgressPercent;
    private string _compilationTimingText = string.Empty;
    private string _preparationTimingText = string.Empty;
    private string _executionTimingText = string.Empty;
    private string _totalTimingText = string.Empty;
    private string _cacheTimingText = string.Empty;
    private string _engineTimingText = string.Empty;

    public StatusBarViewModel()
    {
        _statusText = IdeLanguageResourceService.GetCurrentString("Status.Ready", "Ready.");
        _lineColumnText = "Ln 1, Col 1";
        _pathText = string.Empty;
        _isVisible = true;
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string LineColumnText
    {
        get => _lineColumnText;
        set => SetProperty(ref _lineColumnText, value);
    }

    public string PathText
    {
        get => _pathText;
        set => SetProperty(ref _pathText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsCompilationProgressVisible
    {
        get => _isCompilationProgressVisible;
        set => SetProperty(ref _isCompilationProgressVisible, value);
    }

    public int CompilationProgressPercent
    {
        get => _compilationProgressPercent;
        set
        {
            var normalizedValue = Math.Clamp(value, 0, 100);

            if (!SetProperty(ref _compilationProgressPercent, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(CompilationProgressText));
        }
    }

    public string CompilationProgressText => $"{CompilationProgressPercent}%";

    public string CompilationTimingText
    {
        get => _compilationTimingText;
        set
        {
            if (!SetProperty(ref _compilationTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string PreparationTimingText
    {
        get => _preparationTimingText;
        set
        {
            if (!SetProperty(ref _preparationTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string ExecutionTimingText
    {
        get => _executionTimingText;
        set
        {
            if (!SetProperty(ref _executionTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string TotalTimingText
    {
        get => _totalTimingText;
        set
        {
            if (!SetProperty(ref _totalTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string CacheTimingText
    {
        get => _cacheTimingText;
        set
        {
            if (!SetProperty(ref _cacheTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string EngineTimingText
    {
        get => _engineTimingText;
        set
        {
            if (!SetProperty(ref _engineTimingText, value))
            {
                return;
            }

            OnTimingChanged();
        }
    }

    public string TimingText => string.Join(
        " | ",
        GetVisibleTimingParts());

    public bool IsTimingVisible => IsCompilationTimingVisible || IsRunTimingVisible;

    public bool IsTimingTextVisible => IsTimingVisible;

    public bool IsCompilationTimingVisible => !string.IsNullOrWhiteSpace(CompilationTimingText);

    public bool IsRunTimingVisible =>
        !string.IsNullOrWhiteSpace(PreparationTimingText)
        || !string.IsNullOrWhiteSpace(ExecutionTimingText)
        || !string.IsNullOrWhiteSpace(TotalTimingText)
        || !string.IsNullOrWhiteSpace(CacheTimingText)
        || !string.IsNullOrWhiteSpace(EngineTimingText);

    public void UpdateLineColumn(int line, int column)
    {
        LineColumnText = $"Ln {line}, Col {column}";
    }

    public void ShowCompilationProgress(int initialPercent = 0)
    {
        CompilationProgressPercent = initialPercent;
        IsCompilationProgressVisible = true;
    }

    public void HideCompilationProgress()
    {
        IsCompilationProgressVisible = false;
        CompilationProgressPercent = 0;
    }

    public void ShowCompilationTiming(string timingText)
    {
        ClearTimingText();
        CompilationTimingText = timingText;
    }

    public void ShowRunTiming(
        string preparationTimingText,
        string executionTimingText,
        string totalTimingText,
        string cacheTimingText,
        string engineTimingText)
    {
        ClearTimingText();
        PreparationTimingText = preparationTimingText;
        ExecutionTimingText = executionTimingText;
        TotalTimingText = totalTimingText;
        CacheTimingText = cacheTimingText;
        EngineTimingText = engineTimingText;
    }

    public void ClearTimingText()
    {
        CompilationTimingText = string.Empty;
        PreparationTimingText = string.Empty;
        ExecutionTimingText = string.Empty;
        TotalTimingText = string.Empty;
        CacheTimingText = string.Empty;
        EngineTimingText = string.Empty;
    }

    private IEnumerable<string> GetVisibleTimingParts()
    {
        if (IsCompilationTimingVisible)
        {
            yield return CompilationTimingText;
        }

        if (!string.IsNullOrWhiteSpace(PreparationTimingText))
        {
            yield return PreparationTimingText;
        }

        if (!string.IsNullOrWhiteSpace(ExecutionTimingText))
        {
            yield return ExecutionTimingText;
        }

        if (!string.IsNullOrWhiteSpace(TotalTimingText))
        {
            yield return TotalTimingText;
        }

        if (!string.IsNullOrWhiteSpace(CacheTimingText))
        {
            yield return CacheTimingText;
        }

        if (!string.IsNullOrWhiteSpace(EngineTimingText))
        {
            yield return EngineTimingText;
        }
    }

    private void OnTimingChanged()
    {
        OnPropertyChanged(nameof(TimingText));
        OnPropertyChanged(nameof(IsTimingVisible));
        OnPropertyChanged(nameof(IsTimingTextVisible));
        OnPropertyChanged(nameof(IsCompilationTimingVisible));
        OnPropertyChanged(nameof(IsRunTimingVisible));
    }
}
