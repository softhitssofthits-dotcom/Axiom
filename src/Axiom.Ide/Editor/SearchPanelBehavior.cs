using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;

namespace Axiom.Ide.Editor;

/// <summary>
/// Attached behavior to install and reuse AvalonEdit SearchPanel per editor instance.
/// </summary>
public static class SearchPanelBehavior
{
    public static readonly DependencyProperty InstallSearchPanelProperty =
        DependencyProperty.RegisterAttached(
            "InstallSearchPanel",
            typeof(bool),
            typeof(SearchPanelBehavior),
            new PropertyMetadata(false, OnInstallSearchPanelChanged));

    private static readonly DependencyProperty SearchPanelProperty =
        DependencyProperty.RegisterAttached(
            "SearchPanel",
            typeof(SearchPanel),
            typeof(SearchPanelBehavior),
            new PropertyMetadata(null));

    public static bool GetInstallSearchPanel(TextEditor element) =>
        (bool)element.GetValue(InstallSearchPanelProperty);

    public static void SetInstallSearchPanel(TextEditor element, bool value) =>
        element.SetValue(InstallSearchPanelProperty, value);

    public static void OpenSearchPanel(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = EnsureSearchPanel(editor);
        panel.Open();
        panel.Reactivate();
        editor.Focus();
    }

    public static void FindNext(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = EnsureSearchPanel(editor);

        if (panel.IsClosed)
        {
            panel.Open();
            panel.Reactivate();
            return;
        }

        panel.FindNext();
        editor.Focus();
    }

    public static void FindPrevious(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var panel = EnsureSearchPanel(editor);

        if (panel.IsClosed)
        {
            panel.Open();
            panel.Reactivate();
            return;
        }

        panel.FindPrevious();
        editor.Focus();
    }

    private static void OnInstallSearchPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextEditor editor || !(bool)e.NewValue)
        {
            return;
        }

        EnsureSearchPanel(editor);
    }

    private static SearchPanel EnsureSearchPanel(TextEditor editor)
    {
        if (editor.GetValue(SearchPanelProperty) is SearchPanel panel)
        {
            return panel;
        }

        panel = SearchPanel.Install(editor);
        editor.SetValue(SearchPanelProperty, panel);
        return panel;
    }
}
