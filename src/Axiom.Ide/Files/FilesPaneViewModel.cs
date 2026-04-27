using System.Collections.ObjectModel;
using Axiom.Ide.ViewModels;

namespace Axiom.Ide.Files;

/// <summary>
/// ViewModel для дерева файлов текущей рабочей папки.
/// </summary>
public sealed class FilesPaneViewModel : ViewModelBase
{
    private string _rootPath;
    private ObservableCollection<FileTreeNode> _nodes;
    private FileTreeNode? _selectedNode;
    private bool _isVisible;
    private bool _showHiddenFiles;

    public FilesPaneViewModel()
    {
        _rootPath = string.Empty;
        _nodes = [];
        _isVisible = true;
    }

    public string RootPath
    {
        get => _rootPath;
        set => SetProperty(ref _rootPath, value);
    }

    public ObservableCollection<FileTreeNode> Nodes
    {
        get => _nodes;
        set => SetProperty(ref _nodes, value);
    }

    public FileTreeNode? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set => SetProperty(ref _showHiddenFiles, value);
    }

    public void ReplaceNodes(IEnumerable<FileTreeNode> nodes)
    {
        Nodes = [.. nodes];
        SelectedNode = null;
    }
}

/// <summary>
/// Узел дерева файлов.
/// </summary>
public sealed class FileTreeNode : ViewModelBase
{
    private string _fullPath = string.Empty;
    private string _displayName = string.Empty;
    private string _renameText = string.Empty;
    private bool _isDirectory;
    private bool _isDrive;
    private bool _isPlaceholder;
    private bool _isLoaded;
    private bool _isExpanded;
    private bool _isRenaming;

    public string FullPath
    {
        get => _fullPath;
        set => SetProperty(ref _fullPath, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string RenameText
    {
        get => _renameText;
        set => SetProperty(ref _renameText, value);
    }

    public bool IsDirectory
    {
        get => _isDirectory;
        set => SetProperty(ref _isDirectory, value);
    }

    public bool IsDrive
    {
        get => _isDrive;
        set => SetProperty(ref _isDrive, value);
    }

    public bool IsPlaceholder
    {
        get => _isPlaceholder;
        set => SetProperty(ref _isPlaceholder, value);
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetProperty(ref _isLoaded, value);
    }

    public ObservableCollection<FileTreeNode> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set => SetProperty(ref _isRenaming, value);
    }

    public static FileTreeNode CreatePlaceholder()
    {
        return new FileTreeNode
        {
            IsPlaceholder = true,
            DisplayName = string.Empty
        };
    }
}
