using System.Windows;

namespace Axiom.Ide.Services.Prompts;

public partial class InformationDialog : Window
{
    public InformationDialog(string title, string message)
    {
        InitializeComponent();

        Title = title;
        MessageTextBlock.Text = message;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
