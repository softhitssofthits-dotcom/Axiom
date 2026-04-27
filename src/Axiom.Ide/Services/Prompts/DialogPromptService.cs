using System.Windows;
using Axiom.Ide.Services.Shell;

namespace Axiom.Ide.Services.Prompts;

public interface IDialogPromptService
{
    bool? ShowSaveDiscardCancel(string fileName);
    bool ShowConfirmation(string message, string? title = null);
    string? ShowTextInput(
        string message,
        string? title = null,
        string initialValue = "",
        string? confirmButtonText = null);
    void ShowInfo(string message, string? title = null);
    void ShowError(string message, string? title = null);
}

public sealed class WpfDialogPromptService : IDialogPromptService
{
    public bool? ShowSaveDiscardCancel(string fileName)
    {
        var message = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            IdeLanguageResourceService.GetCurrentString("Prompts.SaveChanges.Message", "Do you want to save changes to '{0}'?"),
            fileName);
        var title = IdeLanguageResourceService.GetCurrentString("Prompts.SaveChanges.Title", "Save Changes");

        MessageBoxResult ShowDialog()
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
        }

        var dispatcher = Application.Current.Dispatcher;
        var result = dispatcher.CheckAccess()
            ? ShowDialog()
            : dispatcher.Invoke(ShowDialog);

        return result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null
        };
    }

    public void ShowInfo(string message, string? title = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new InformationDialog(ResolveTitle(title), message)
            {
                Owner = Application.Current?.MainWindow
            };

            dialog.ShowDialog();
        });
    }

    public bool ShowConfirmation(string message, string? title = null)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var result = MessageBox.Show(
                message,
                ResolveTitle(title),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        });
    }

    public string? ShowTextInput(
        string message,
        string? title = null,
        string initialValue = "",
        string? confirmButtonText = null)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new TextInputDialog(
                ResolveTitle(title),
                message,
                initialValue,
                confirmButtonText ?? IdeLanguageResourceService.GetCurrentString("Prompts.Ok", "OK"))
            {
                Owner = Application.Current?.MainWindow
            };

            return dialog.ShowDialog() == true
                ? dialog.EnteredText
                : null;
        });
    }

    public void ShowError(string message, string? title = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, ResolveTitle(title), MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    private static string ResolveTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title)
            ? IdeLanguageResourceService.GetCurrentString("Prompts.DefaultTitle", "Axiom IDE")
            : title;
    }
}
