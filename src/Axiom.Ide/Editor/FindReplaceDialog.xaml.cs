using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Axiom.Ide.Editor;

public partial class FindReplaceDialog : Window
{
    private bool _selectAllOnShow;

    public FindReplaceDialog()
    {
        InitializeComponent();
        FindTabItem.IsSelected = true;
        PreviewKeyDown += OnPreviewKeyDown;
        UpdateModePresentation();
        UpdateButtonsState();
    }

    public event EventHandler<FindReplaceActionEventArgs>? ActionRequested;

    public FindReplaceOptions CurrentOptions => BuildOptions();

    public void ApplyOptions(FindReplaceOptions options)
    {
        SearchPatternTextBox.Text = options.SearchPattern;
        ReplacePatternTextBox.Text = options.ReplacePattern;
        MatchCaseCheckBox.IsChecked = options.MatchCase;
        WholeWordsCheckBox.IsChecked = options.WholeWords;
        UseRegexCheckBox.IsChecked = options.UseRegex;
        ReverseDirectionCheckBox.IsChecked = !options.SearchDown;
        UpdateModePresentation();
        UpdateButtonsState();
    }

    public void FocusSearchPattern(bool selectAll = false)
    {
        SearchPatternTextBox.Focus();

        if (selectAll)
        {
            SearchPatternTextBox.SelectAll();
        }
    }

    public void PrepareForShow(bool selectAll = false)
    {
        _selectAllOnShow = selectAll;
    }

    private bool IsReplaceMode => ReplaceTabItem.IsSelected;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        FocusSearchPattern(_selectAllOnShow);
        _selectAllOnShow = false;
    }

    private void OnModeTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, ModeTabControl))
        {
            return;
        }

        UpdateModePresentation();
        UpdateButtonsState();
    }

    private void OnFindNextClicked(object sender, RoutedEventArgs e)
    {
        RaiseAction(FindReplaceAction.FindNext);
    }

    private void OnReplaceClicked(object sender, RoutedEventArgs e)
    {
        RaiseAction(FindReplaceAction.Replace);
    }

    private void OnReplaceAllClicked(object sender, RoutedEventArgs e)
    {
        RaiseAction(FindReplaceAction.ReplaceAll);
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnOptionsChanged(object sender, RoutedEventArgs e)
    {
        UpdateButtonsState();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RaiseAction(FindReplaceAction.FindNext);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void RaiseAction(FindReplaceAction action)
    {
        if (string.IsNullOrWhiteSpace(SearchPatternTextBox.Text))
        {
            return;
        }

        ActionRequested?.Invoke(this, new FindReplaceActionEventArgs(action, BuildOptions()));
    }

    private FindReplaceOptions BuildOptions()
    {
        return new FindReplaceOptions(
            SearchPatternTextBox.Text,
            ReplacePatternTextBox.Text,
            MatchCase: MatchCaseCheckBox.IsChecked == true,
            WholeWords: WholeWordsCheckBox.IsChecked == true,
            UseRegex: UseRegexCheckBox.IsChecked == true,
            SearchDown: ReverseDirectionCheckBox.IsChecked != true);
    }

    private void UpdateButtonsState()
    {
        var hasPattern = !string.IsNullOrWhiteSpace(SearchPatternTextBox.Text);
        FindNextButton.IsEnabled = hasPattern;
        ReplaceButton.IsEnabled = hasPattern && IsReplaceMode;
        ReplaceAllButton.IsEnabled = hasPattern && IsReplaceMode;
    }

    private void UpdateModePresentation()
    {
        ReplaceRow.Visibility = IsReplaceMode ? Visibility.Visible : Visibility.Collapsed;
        ReplaceButton.Visibility = IsReplaceMode ? Visibility.Visible : Visibility.Collapsed;
        ReplaceAllButton.Visibility = IsReplaceMode ? Visibility.Visible : Visibility.Collapsed;
        Title = GetModeTitle();
    }

    private string GetModeTitle()
    {
        var resourceKey = IsReplaceMode
            ? "FindReplace.ReplaceTab"
            : "FindReplace.FindTab";

        return TryFindResource(resourceKey) as string
            ?? (IsReplaceMode ? "Replace" : "Find");
    }
}
