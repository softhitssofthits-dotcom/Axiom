using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Axiom.Ide.Files;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide;

public partial class MainWindow
{
    private void OnFilesTreeItemExpanded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            e.OriginalSource is not TreeViewItem { DataContext: FileTreeNode node })
        {
            return;
        }

        if (viewModel.ExpandFilesPaneNodeCommand.CanExecute(node))
        {
            viewModel.ExpandFilesPaneNodeCommand.Execute(node);
        }
    }

    private void OnFilesTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.FilesPane.SelectedNode = e.NewValue as FileTreeNode;

        if (e.NewValue is not FileTreeNode node || _suppressFilesTreeNodeActivation)
        {
            return;
        }

        if (viewModel.OpenFilesPaneNodeCommand.CanExecute(node))
        {
            viewModel.OpenFilesPaneNodeCommand.Execute(node);
        }
    }

    private void OnFilesTreeItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TreeViewItem treeViewItem || treeViewItem.DataContext is not FileTreeNode node)
        {
            return;
        }

        _suppressFilesTreeNodeActivation = true;
        treeViewItem.IsSelected = true;
        treeViewItem.Focus();

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.FilesPane.SelectedNode = node;
        }

        Dispatcher.BeginInvoke(() => _suppressFilesTreeNodeActivation = false);
    }

    private void OnFilesTreeRenameTextBoxIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox || e.NewValue is not true || textBox.DataContext is not FileTreeNode node)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (!node.IsRenaming)
            {
                return;
            }

            textBox.Focus();
            textBox.Select(0, GetFilesTreeRenameSelectionLength(node));
        });
    }

    private void OnFilesTreeRenameTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox { DataContext: FileTreeNode node } || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.CommitFilesPaneNodeRename(node);
    }

    private void OnFilesTreeRenameTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not FileTreeNode node || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                viewModel.CommitFilesPaneNodeRename(node);
                FocusFilesTreeItem(textBox);
                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.CancelFilesPaneNodeRename(node);
                FocusFilesTreeItem(textBox);
                e.Handled = true;
                break;
        }
    }
}
