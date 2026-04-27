using System.Windows;
using Axiom.Ide.Editor;
using Axiom.Ide.Services.Shell;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide;

public partial class MainWindow
{
    private void ShowFindReplaceDialog(bool selectSearchPattern)
    {
        var editor = GetActiveEditor();

        if (editor is null)
        {
            return;
        }

        if (_findReplaceDialog is null)
        {
            _findReplaceDialog = new FindReplaceDialog
            {
                Owner = this
            };

            _findReplaceDialog.ActionRequested += OnFindReplaceActionRequested;
            _findReplaceDialog.Closed += OnFindReplaceDialogClosed;
        }

        if (!string.IsNullOrWhiteSpace(editor.SelectedText))
        {
            _findReplaceOptions = _findReplaceOptions with { SearchPattern = editor.SelectedText };
        }

        _findReplaceDialog.ApplyOptions(_findReplaceOptions);
        _findReplaceDialog.PrepareForShow(selectSearchPattern);
        _findReplaceDialog.ShowDialog();
    }

    private void CloseFindReplaceDialog()
    {
        if (_findReplaceDialog is null)
        {
            return;
        }

        _findReplaceDialog.ActionRequested -= OnFindReplaceActionRequested;
        _findReplaceDialog.Closed -= OnFindReplaceDialogClosed;
        _findReplaceDialog.Close();
        _findReplaceDialog = null;
    }

    private void OnFindReplaceDialogClosed(object? sender, EventArgs e)
    {
        if (_findReplaceDialog is null)
        {
            return;
        }

        _findReplaceOptions = _findReplaceDialog.CurrentOptions;
        _findReplaceDialog.ActionRequested -= OnFindReplaceActionRequested;
        _findReplaceDialog.Closed -= OnFindReplaceDialogClosed;
        _findReplaceDialog = null;
        ClearFindReplaceStatusBarText();
    }

    private void OnFindReplaceActionRequested(object? sender, FindReplaceActionEventArgs e)
    {
        _findReplaceOptions = e.Options;

        switch (e.Action)
        {
            case FindReplaceAction.FindNext:
                ExecuteFind(_findReplaceOptions.SearchDown);
                break;
            case FindReplaceAction.Replace:
                ExecuteReplace();
                break;
            case FindReplaceAction.ReplaceAll:
                ExecuteReplaceAll();
                break;
        }
    }

    private void ExecuteFind(bool searchForward)
    {
        var editor = GetActiveEditor();

        if (editor is null)
        {
            return;
        }

        if (!TryGetSearchOptions(searchForward, out var options))
        {
            ShowFindReplaceDialog(selectSearchPattern: true);
            return;
        }

        var outcome = EditorFindReplaceWorkflow.Find(editor, options, searchForward);

        if (!HandleSearchWorkflowOutcome(outcome))
        {
            return;
        }

        _findReplaceOptions = options;
    }

    private void ExecuteReplace()
    {
        var editor = GetActiveEditor();

        if (editor is null || !TryGetSearchOptions(_findReplaceOptions.SearchDown, out var options))
        {
            ShowFindReplaceDialog(selectSearchPattern: true);
            return;
        }

        var outcome = EditorFindReplaceWorkflow.Replace(editor, options, options.SearchDown);

        if (!HandleSearchWorkflowOutcome(outcome))
        {
            return;
        }

        _findReplaceOptions = options;
    }

    private void ExecuteReplaceAll()
    {
        var editor = GetActiveEditor();

        if (editor is null || !TryGetSearchOptions(_findReplaceOptions.SearchDown, out var options))
        {
            ShowFindReplaceDialog(selectSearchPattern: true);
            return;
        }

        var replacedCount = EditorFindReplaceEngine.ReplaceAll(editor, options, out var errorMessage);

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ShowSearchError(errorMessage);
            return;
        }

        _findReplaceOptions = options;
        UpdateFindReplaceStatusBarText(replacedCount == 0
            ? "Совпадений не найдено."
            : $"Заменено: {replacedCount}.");
    }

    private bool TryGetSearchOptions(bool searchForward, out FindReplaceOptions options)
    {
        options = _findReplaceDialog?.CurrentOptions ?? _findReplaceOptions;

        if (string.IsNullOrWhiteSpace(options.SearchPattern))
        {
            return false;
        }

        options = options with { SearchDown = searchForward };
        return true;
    }

    private void ShowSearchError(string errorMessage)
    {
        UpdateFindReplaceStatusBarText("Ошибка поиска.");
        MessageBox.Show(
            this,
            errorMessage,
            "Найти и заменить",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private bool HandleSearchWorkflowOutcome(FindReplaceWorkflowOutcome outcome)
    {
        switch (outcome.Result)
        {
            case FindReplaceWorkflowResult.MatchFound:
                UpdateFindReplaceStatusBarText("Совпадение найдено.");
                return true;

            case FindReplaceWorkflowResult.MatchNotFound:
                UpdateFindReplaceStatusBarText("Совпадений не найдено.");
                return true;

            case FindReplaceWorkflowResult.ReplacedAndFoundNext:
                UpdateFindReplaceStatusBarText("Замена выполнена. Найдено следующее совпадение.");
                return true;

            case FindReplaceWorkflowResult.ReplacedAndNoMoreMatches:
                UpdateFindReplaceStatusBarText("Замена выполнена. Больше совпадений не найдено.");
                return true;

            case FindReplaceWorkflowResult.Error:
                if (!string.IsNullOrWhiteSpace(outcome.ErrorMessage))
                {
                    ShowSearchError(outcome.ErrorMessage);
                }

                return false;

            default:
                return false;
        }
    }

    private void UpdateFindReplaceStatusBarText(string text)
    {
        _hasFindReplaceStatusMessage = true;
        UpdateStatusBarText(text);
    }

    private void ClearFindReplaceStatusBarText()
    {
        if (!_hasFindReplaceStatusMessage)
        {
            return;
        }

        _hasFindReplaceStatusMessage = false;
        UpdateStatusBarText(IdeLanguageResourceService.GetCurrentString("Status.Ready", "Ready."));
    }

    private void UpdateStatusBarText(string text)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusBar.StatusText = text;
        }
    }
}
