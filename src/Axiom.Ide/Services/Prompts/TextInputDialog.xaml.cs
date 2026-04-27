using System.Windows;

namespace Axiom.Ide.Services.Prompts;

public partial class TextInputDialog : Window
{
    public TextInputDialog(string title, string prompt, string initialValue, string confirmButtonText)
    {
        InitializeComponent();

        Title = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Text = initialValue;
        OkButton.Content = confirmButtonText;

        Loaded += (_, _) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    public string EnteredText => InputTextBox.Text;

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
