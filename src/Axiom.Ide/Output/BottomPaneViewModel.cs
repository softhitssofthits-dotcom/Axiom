using System.Collections.ObjectModel;
using Axiom.Ide.Models;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Output;

/// <summary>
/// ViewModel нижней панели с вкладками Output / Diagnostics.
/// </summary>
public sealed class BottomPaneViewModel : ViewModelBase
{
    private int _selectedTabIndex;
    private string _outputText;
    private bool _isOutputVisible;
    private bool _isDiagnosticsVisible;
    private bool _isWaitingForInput;
    private string _currentPromptText;
    private string _currentInputText;
    private ProgramDiagnosticItem? _selectedDiagnostic;
    private BottomPaneFocusTarget _focusTarget;
    private int _focusRequestVersion;

    public BottomPaneViewModel()
    {
        _outputText = string.Empty;
        _isOutputVisible = true;
        _isDiagnosticsVisible = true;
        _currentPromptText = string.Empty;
        _currentInputText = string.Empty;
        Diagnostics = [];
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    public bool IsOutputVisible
    {
        get => _isOutputVisible;
        set
        {
            if (!value && IsWaitingForInput)
            {
                return;
            }

            if (!SetProperty(ref _isOutputVisible, value))
            {
                return;
            }

            HandlePaneVisibilityChanged(paneIndex: 0, isVisible: value);
        }
    }

    public bool IsDiagnosticsVisible
    {
        get => _isDiagnosticsVisible;
        set
        {
            if (!SetProperty(ref _isDiagnosticsVisible, value))
            {
                return;
            }

            HandlePaneVisibilityChanged(paneIndex: 1, isVisible: value);
        }
    }

    public bool IsVisible => IsOutputVisible || IsDiagnosticsVisible;

    public bool IsWaitingForInput
    {
        get => _isWaitingForInput;
        set => SetProperty(ref _isWaitingForInput, value);
    }

    public string CurrentPromptText
    {
        get => _currentPromptText;
        set => SetProperty(ref _currentPromptText, value);
    }

    public string CurrentInputText
    {
        get => _currentInputText;
        set => SetProperty(ref _currentInputText, value);
    }

    public ObservableCollection<ProgramDiagnosticItem> Diagnostics { get; }

    public ProgramDiagnosticItem? SelectedDiagnostic
    {
        get => _selectedDiagnostic;
        set => SetProperty(ref _selectedDiagnostic, value);
    }

    public BottomPaneFocusTarget FocusTarget
    {
        get => _focusTarget;
        private set => SetProperty(ref _focusTarget, value);
    }

    public int FocusRequestVersion
    {
        get => _focusRequestVersion;
        private set => SetProperty(ref _focusRequestVersion, value);
    }

    /// <summary>
    /// Переключает на вкладку Output.
    /// </summary>
    public void SelectOutput()
    {
        ShowOutput();
    }

    /// <summary>
    /// Переключает на вкладку Diagnostics.
    /// </summary>
    public void SelectDiagnostics()
    {
        ShowDiagnostics();
    }

    public void ShowOutput()
    {
        if (!IsOutputVisible)
        {
            IsOutputVisible = true;
        }

        SelectedTabIndex = 0;
    }

    public void ShowDiagnostics()
    {
        if (!IsDiagnosticsVisible)
        {
            IsDiagnosticsVisible = true;
        }

        SelectedTabIndex = 1;
    }

    public void HideOutput()
    {
        if (IsWaitingForInput)
        {
            return;
        }

        IsOutputVisible = false;
    }

    public void HideDiagnostics()
    {
        IsDiagnosticsVisible = false;
    }

    /// <summary>
    /// Заменяет список диагностик новым.
    /// </summary>
    public void ReplaceDiagnostics(IEnumerable<ProgramDiagnosticItem> diagnostics)
    {
        Diagnostics.Clear();

        foreach (var diagnostic in diagnostics)
        {
            Diagnostics.Add(diagnostic);
        }

        SelectedDiagnostic = Diagnostics.FirstOrDefault();
    }

    /// <summary>
    /// Очищает output.
    /// </summary>
    public void ClearOutput()
    {
        OutputText = string.Empty;
        IsWaitingForInput = false;
        CurrentPromptText = string.Empty;
        CurrentInputText = string.Empty;
    }

    /// <summary>
    /// Очищает список диагностик.
    /// </summary>
    public void ClearDiagnostics()
    {
        if (Diagnostics.Count > 0)
        {
            Diagnostics.Clear();
            SelectedDiagnostic = null;
        }
    }

    /// <summary>
    /// Добавляет строку в output transcript.
    /// </summary>
    public void AppendOutputLine(string line)
    {
        OutputText = string.IsNullOrEmpty(OutputText)
            ? line
            : $"{OutputText}{Environment.NewLine}{line}";
    }

    public void RequestOutputFocus()
    {
        FocusTarget = BottomPaneFocusTarget.Output;
        FocusRequestVersion++;
    }

    public void RequestDiagnosticsFocus()
    {
        FocusTarget = BottomPaneFocusTarget.Diagnostics;
        FocusRequestVersion++;
    }

    private void HandlePaneVisibilityChanged(int paneIndex, bool isVisible)
    {
        OnPropertyChanged(nameof(IsVisible));

        if (isVisible)
        {
            SelectedTabIndex = paneIndex;
            return;
        }

        if (!IsVisible)
        {
            SelectedTabIndex = 0;
            return;
        }

        if (SelectedTabIndex == paneIndex)
        {
            SelectedTabIndex = paneIndex == 0 ? 1 : 0;
        }
    }
}

public enum BottomPaneFocusTarget
{
    Output = 0,
    Diagnostics = 1
}
