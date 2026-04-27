using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Axiom.Ide.Services.Shell;

namespace Axiom.Ide.Services.EventLog;

public partial class IdeEventLogViewerWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _logFilePath;
    private readonly List<ViewerEntry> _allEntries = [];
    private string? _selectedCategory;
    private int _parseErrors;

    public IdeEventLogViewerWindow(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

        _logFilePath = Path.GetFullPath(logFilePath);

        InitializeComponent();

        IdeLanguageResourceService.LanguageApplied += OnLanguageApplied;
        UpdateLocalizedText();
        LoadEntries();
        ApplyFilter();
        UpdateSelectedDetails();
    }

    protected override void OnClosed(EventArgs e)
    {
        IdeLanguageResourceService.LanguageApplied -= OnLanguageApplied;
        base.OnClosed(e);
    }

    private void OnLanguageApplied(object? sender, EventArgs e)
    {
        UpdateLocalizedText();
        PopulateCategories(_selectedCategory);
        ApplyFilter();
    }

    private void UpdateLocalizedText()
    {
        Title = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.Title", "IDE Event Log Viewer");
        FilePathTextBlock.Text = $"{IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.FileLabel", "File")}: {_logFilePath}";
        SequenceColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.SequenceHeader", "Seq");
        TimeColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.TimeHeader", "Time");
        LevelColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.LevelHeader", "Level");
        CategoryColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.CategoryHeader", "Category");
        EventColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.EventHeader", "Event");
        CommandColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.CommandHeader", "Command");
        DocumentColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.DocumentHeader", "Document");
        DetailsColumn.Header = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.DetailsHeader", "Details");
        UpdateSelectedDetails();
    }

    private void LoadEntries()
    {
        var previouslySelectedCategory = _selectedCategory;
        _allEntries.Clear();
        var result = ReadEntries(_logFilePath);
        _parseErrors = result.ParseErrors;
        _allEntries.AddRange(result.Entries.Select(static entry => new ViewerEntry(entry)));

        PopulateCategories(previouslySelectedCategory);
    }

    internal static ReadEntriesResult ReadEntries(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

        if (!File.Exists(logFilePath))
        {
            return new ReadEntriesResult([], 0);
        }

        var entries = new List<IdeEventLogEntry>();
        var parseErrors = 0;

        using var stream = new FileStream(
            logFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<IdeEventLogEntry>(line, JsonOptions);
                if (entry is null)
                {
                    continue;
                }

                entries.Add(entry);
            }
            catch (JsonException)
            {
                parseErrors++;
            }
        }

        return new ReadEntriesResult(entries, parseErrors);
    }

    private void PopulateCategories(string? preferredCategory)
    {
        var allCategoriesText = IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.AllCategories", "All categories");
        var categories = _allEntries
            .Select(static entry => entry.Category)
            .Where(static category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CategoryComboBox.Items.Clear();
        CategoryComboBox.Items.Add(allCategoriesText);

        foreach (var category in categories)
        {
            CategoryComboBox.Items.Add(category);
        }

        if (!string.IsNullOrWhiteSpace(preferredCategory))
        {
            for (var index = 1; index < CategoryComboBox.Items.Count; index++)
            {
                if (CategoryComboBox.Items[index] is string category &&
                    string.Equals(category, preferredCategory, StringComparison.OrdinalIgnoreCase))
                {
                    CategoryComboBox.SelectedIndex = index;
                    _selectedCategory = category;
                    return;
                }
            }
        }

        CategoryComboBox.SelectedIndex = 0;
        _selectedCategory = null;
    }

    private void ApplyFilter()
    {
        IEnumerable<ViewerEntry> filtered = _allEntries;
        var searchText = SearchTextBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(_selectedCategory))
        {
            filtered = filtered.Where(entry => string.Equals(entry.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(entry =>
                ContainsText(entry.EventName, searchText) ||
                ContainsText(entry.Category, searchText) ||
                ContainsText(entry.Level, searchText) ||
                ContainsText(entry.CommandName, searchText) ||
                ContainsText(entry.DocumentName, searchText) ||
                ContainsText(entry.Details, searchText));
        }

        var filteredList = filtered.ToList();
        EntriesDataGrid.ItemsSource = filteredList;
        UpdateSelectedDetails();
        UpdateCount(filteredList.Count);
    }

    private void UpdateCount(int filteredCount)
    {
        if (_parseErrors > 0)
        {
            CountTextBlock.Text = IdeLanguageResourceService.GetCurrentFormattedString(
                "EventLog.Viewer.CountWithErrors",
                "Events: {0} / {1} (parse errors: {2})",
                filteredCount,
                _allEntries.Count,
                _parseErrors);
            return;
        }

        CountTextBlock.Text = IdeLanguageResourceService.GetCurrentFormattedString(
            "EventLog.Viewer.Count",
            "Events: {0} / {1}",
            filteredCount,
            _allEntries.Count);
    }

    private static bool ContainsText(string? text, string searchText)
    {
        return !string.IsNullOrWhiteSpace(text)
               && text.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is string category &&
            !string.Equals(
                category,
                IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.AllCategories", "All categories"),
                StringComparison.Ordinal))
        {
            _selectedCategory = category;
        }
        else
        {
            _selectedCategory = null;
        }

        ApplyFilter();
    }

    private void OnEntrySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedDetails();
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        var selectedSequence = (EntriesDataGrid.SelectedItem as ViewerEntry)?.Sequence;

        LoadEntries();
        ApplyFilter();

        if (selectedSequence is not null && EntriesDataGrid.ItemsSource is IEnumerable<ViewerEntry> entries)
        {
            var restored = entries.FirstOrDefault(entry => entry.Sequence == selectedSequence.Value);
            if (restored is not null)
            {
                EntriesDataGrid.SelectedItem = restored;
                EntriesDataGrid.ScrollIntoView(restored);
                return;
            }
        }

        if (EntriesDataGrid.Items.Count > 0)
        {
            EntriesDataGrid.SelectedIndex = EntriesDataGrid.Items.Count - 1;
            EntriesDataGrid.ScrollIntoView(EntriesDataGrid.SelectedItem);
        }
    }

    private void OnCopySelectedClick(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is not ViewerEntry entry)
        {
            return;
        }

        CopyToClipboard(entry.ClipboardText);
    }

    private void OnCopyCellClick(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is not ViewerEntry entry ||
            EntriesDataGrid.CurrentCell.Column is null)
        {
            return;
        }

        CopyToClipboard(entry.GetCellText(EntriesDataGrid.CurrentCell.Column.DisplayIndex));
    }

    private void OnCopyDetailsClick(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is not ViewerEntry entry)
        {
            return;
        }

        CopyToClipboard(entry.DetailsText);
    }

    private void OnEntriesDataGridPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var row = FindVisualParent<DataGridRow>(source);
        if (row is null)
        {
            return;
        }

        row.IsSelected = true;
        EntriesDataGrid.SelectedItem = row.Item;
        EntriesDataGrid.Focus();

        var cell = FindVisualParent<DataGridCell>(source);
        if (cell is not null)
        {
            EntriesDataGrid.CurrentCell = new DataGridCellInfo(row.Item, cell.Column);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateSelectedDetails()
    {
        SelectedDetailsTextBox.Text = EntriesDataGrid.SelectedItem is ViewerEntry entry
            ? entry.DetailsText
            : IdeLanguageResourceService.GetCurrentString("EventLog.Viewer.NoSelectionDetails", "No event selected.");
    }

    private static void CopyToClipboard(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }

    private static T? FindVisualParent<T>(DependencyObject source)
        where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T target)
            {
                return target;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    internal sealed record ReadEntriesResult(IReadOnlyList<IdeEventLogEntry> Entries, int ParseErrors);

    private sealed class ViewerEntry
    {
        public ViewerEntry(IdeEventLogEntry entry)
        {
            Sequence = entry.Sequence;
            TimestampLocalText = entry.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);
            Level = entry.Level.ToString();
            Category = entry.Category;
            EventName = entry.EventName;
            CommandName = entry.CommandName ?? string.Empty;
            DocumentName = entry.DocumentName ?? string.Empty;
            Details = BuildDetails(entry);
            DetailsText = BuildDetailsText(entry);
            ClipboardText = JsonSerializer.Serialize(entry, JsonOptions);
        }

        public long Sequence { get; }

        public string TimestampLocalText { get; }

        public string Level { get; }

        public string Category { get; }

        public string EventName { get; }

        public string CommandName { get; }

        public string DocumentName { get; }

        public string Details { get; }

        public string DetailsText { get; }

        public string ClipboardText { get; }

        public string GetCellText(int displayIndex)
        {
            return displayIndex switch
            {
                0 => Sequence.ToString(CultureInfo.InvariantCulture),
                1 => TimestampLocalText,
                2 => Level,
                3 => Category,
                4 => EventName,
                5 => CommandName,
                6 => DocumentName,
                7 => Details,
                _ => ClipboardText
            };
        }

        private static string BuildDetails(IdeEventLogEntry entry)
        {
            var details = new List<string>(8);

            if (entry.Line is not null && entry.Column is not null)
            {
                details.Add($"Ln {entry.Line}, Col {entry.Column}");
            }

            if (!string.IsNullOrWhiteSpace(entry.TextPreview))
            {
                details.Add(entry.TextPreview);
            }

            if (entry.Properties is not null)
            {
                foreach (var pair in entry.Properties)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    details.Add($"{pair.Key}={pair.Value}");
                }
            }

            return string.Join(" | ", details);
        }

        private static string BuildDetailsText(IdeEventLogEntry entry)
        {
            var lines = new List<string>(32)
            {
                $"sequence: {entry.Sequence}",
                $"timestampLocal: {entry.TimestampLocal:yyyy-MM-dd HH:mm:ss.fff zzz}",
                $"timestampUtc: {entry.TimestampUtc:yyyy-MM-dd HH:mm:ss.fff zzz}",
                $"level: {entry.Level}",
                $"category: {entry.Category}",
                $"eventName: {entry.EventName}",
                $"source: {entry.Source}",
                $"threadId: {entry.ThreadId}"
            };

            AddLine(lines, "sessionId", entry.SessionId);
            AddLine(lines, "commandName", entry.CommandName);
            AddLine(lines, "menuName", entry.MenuName);
            AddLine(lines, "documentName", entry.DocumentName);
            AddLine(lines, "documentPath", entry.DocumentPath);
            AddLine(lines, "line", entry.Line?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "column", entry.Column?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "debugLine", entry.DebugLine?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "opcode", entry.Opcode);
            AddLine(lines, "callDepth", entry.CallDepth?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "canStepBack", entry.CanStepBack?.ToString());
            AddLine(lines, "outputLength", entry.OutputLength?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "textLength", entry.TextLength?.ToString(CultureInfo.InvariantCulture));
            AddLine(lines, "textPreview", entry.TextPreview);

            if (entry.StatusBar is not null)
            {
                lines.Add("statusBar:");
                AddIndentedLine(lines, "pathText", entry.StatusBar.PathText);
                AddIndentedLine(lines, "statusText", entry.StatusBar.StatusText);
                AddIndentedLine(lines, "lineColumnText", entry.StatusBar.LineColumnText);
                AddIndentedLine(lines, "compilationTimingText", entry.StatusBar.CompilationTimingText);
                AddIndentedLine(lines, "preparationTimingText", entry.StatusBar.PreparationTimingText);
                AddIndentedLine(lines, "executionTimingText", entry.StatusBar.ExecutionTimingText);
                AddIndentedLine(lines, "totalTimingText", entry.StatusBar.TotalTimingText);
                AddIndentedLine(lines, "cacheTimingText", entry.StatusBar.CacheTimingText);
                AddIndentedLine(lines, "engineTimingText", entry.StatusBar.EngineTimingText);
                AddIndentedLine(lines, "isRunning", entry.StatusBar.IsRunning.ToString());
                AddIndentedLine(lines, "isVisible", entry.StatusBar.IsVisible.ToString());
                AddIndentedLine(lines, "isCompilationProgressVisible", entry.StatusBar.IsCompilationProgressVisible.ToString());
                AddIndentedLine(lines, "compilationProgressPercent", entry.StatusBar.CompilationProgressPercent.ToString(CultureInfo.InvariantCulture));
            }

            if (entry.Properties is { Count: > 0 })
            {
                lines.Add("properties:");
                foreach (var pair in entry.Properties.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    AddIndentedLine(lines, pair.Key, pair.Value);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static void AddLine(List<string> lines, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                lines.Add($"{name}: {value}");
            }
        }

        private static void AddIndentedLine(List<string> lines, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
            {
                lines.Add($"  {name}: {value}");
            }
        }
    }
}
