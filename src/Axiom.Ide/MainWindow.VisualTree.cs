using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Axiom.Ide.Files;

namespace Axiom.Ide;

public partial class MainWindow
{
    private static T? FindDescendant<T>(DependencyObject? root)
        where T : DependencyObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(root);

        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            var descendant = FindDescendant<T>(child);

            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject? root)
        where T : DependencyObject
    {
        if (root is null)
        {
            yield break;
        }

        if (root is T match)
        {
            yield return match;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(root);

        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject? start)
        where T : DependencyObject
    {
        var current = start;

        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void FocusFilesTreeItem(DependencyObject start)
    {
        FindAncestor<TreeViewItem>(start)?.Focus();
    }

    private static int GetFilesTreeRenameSelectionLength(FileTreeNode node)
    {
        if (node.IsDirectory)
        {
            return node.DisplayName.Length;
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(node.DisplayName);
        return string.IsNullOrEmpty(fileNameWithoutExtension)
            ? node.DisplayName.Length
            : fileNameWithoutExtension.Length;
    }
}
