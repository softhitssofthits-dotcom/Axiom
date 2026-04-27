using System.Windows;
using ICSharpCode.AvalonEdit;

namespace Axiom.Ide.Editor;

public static class AvalonEditTextBinding
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(AvalonEditTextBinding),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(AvalonEditTextBinding),
            new PropertyMetadata(false));

    private static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(AvalonEditTextBinding),
            new PropertyMetadata(false));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    public static void EnsureAttached(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (GetIsAttached(editor))
        {
            return;
        }

        editor.TextChanged += EditorOnTextChanged;
        SetIsAttached(editor, true);
    }

    private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

    private static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    private static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not TextEditor editor)
        {
            return;
        }

        EnsureAttached(editor);

        if (GetIsUpdating(editor))
        {
            return;
        }

        var newText = eventArgs.NewValue as string ?? string.Empty;

        if (!string.Equals(editor.Text, newText, StringComparison.Ordinal))
        {
            SetIsUpdating(editor, true);

            try
            {
                editor.Text = newText;
            }
            finally
            {
                SetIsUpdating(editor, false);
            }
        }
    }

    private static void EditorOnTextChanged(object? sender, EventArgs eventArgs)
    {
        if (sender is not TextEditor editor)
        {
            return;
        }

        SetIsUpdating(editor, true);
        SetText(editor, editor.Text);
        SetIsUpdating(editor, false);
    }
}
