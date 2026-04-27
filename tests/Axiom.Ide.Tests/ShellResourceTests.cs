using System.Windows;
using ICSharpCode.AvalonEdit.Editing;

namespace Axiom.Ide.Tests;

public sealed class ShellResourceTests
{
    [Fact]
    public void ShellResources_DictionaryLoadsExpectedKeys()
    {
        var dictionary = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellResources.xaml", UriKind.Relative));

        Assert.NotNull(dictionary["WindowBackgroundBrush"]);
        Assert.NotNull(dictionary["SectionBorderBrush"]);
        Assert.NotNull(dictionary["ToolbarButtonStyle"]);
        Assert.NotNull(dictionary["EditorHostBorderStyle"]);
        Assert.NotNull(dictionary["ReadOnlyPanelTextBoxStyle"]);
        Assert.NotNull(dictionary["DiagnosticsDataGridStyle"]);
        Assert.NotNull(dictionary["RecentFilesListBoxStyle"]);
        Assert.NotNull(dictionary["RecentFileNameTextStyle"]);
    }

    [Fact]
    public void ShellColors_DictionaryLoadsDiagnosticNavigationHighlightKeys()
    {
        var dictionary = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellColors.xaml", UriKind.Relative));

        Assert.NotNull(dictionary["EditorCurrentLineHighlightBrush"]);
        Assert.NotNull(dictionary["EditorCurrentLineBorderPen"]);
        Assert.NotNull(dictionary["DiagnosticLineHighlightBrush"]);
        Assert.NotNull(dictionary["DiagnosticLineBorderPen"]);
        Assert.NotNull(dictionary["BreakpointMarkerBrush"]);
        Assert.NotNull(dictionary["BreakpointMarkerColor"]);
        Assert.NotNull(dictionary["StoppedRunLineHighlightBrush"]);
        Assert.NotNull(dictionary["StoppedRunLineBorderPen"]);
        Assert.NotNull(dictionary["StoppedRunExecutionSpanBrush"]);
        Assert.NotNull(dictionary["StoppedRunExecutionSpanBorderPen"]);
        Assert.NotNull(dictionary["EditorForegroundBrush"]);
        Assert.NotNull(dictionary["EditorCaretBrush"]);
        Assert.NotNull(dictionary["ControlHoverBrush"]);
        Assert.NotNull(dictionary["MenuHighlightBrush"]);
        Assert.NotNull(dictionary["MenuHighlightBorderBrush"]);
        Assert.NotNull(dictionary["MenuDisabledTextBrush"]);
        Assert.NotNull(dictionary["MenuIconBrush"]);
        Assert.NotNull(dictionary["MenuSeparatorBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionForegroundBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionInactiveBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionInactiveForegroundBrush"]);
        Assert.NotNull(dictionary["TreeSelectionBrush"]);
        Assert.NotNull(dictionary["TreeExpanderBackgroundBrush"]);
        Assert.NotNull(dictionary["TreeExpanderBorderBrush"]);
        Assert.NotNull(dictionary["TreeExpanderForegroundBrush"]);
        Assert.NotNull(dictionary["LineNumberMarginBackgroundBrush"]);
        Assert.NotNull(dictionary["LineNumberForegroundBrush"]);
        Assert.NotNull(dictionary["LineNumberMarginMinWidth"]);
    }

    [Fact]
    public void DarkShellColors_DictionaryLoadsThemeKeys()
    {
        var dictionary = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellColors.Dark.xaml", UriKind.Relative));

        Assert.NotNull(dictionary["WindowBackgroundBrush"]);
        Assert.NotNull(dictionary["EditorBackgroundBrush"]);
        Assert.NotNull(dictionary["EditorForegroundBrush"]);
        Assert.NotNull(dictionary["EditorCaretBrush"]);
        Assert.NotNull(dictionary["EditorCurrentLineHighlightBrush"]);
        Assert.NotNull(dictionary["EditorCurrentLineBorderPen"]);
        Assert.NotNull(dictionary["ControlHoverBrush"]);
        Assert.NotNull(dictionary["MenuHighlightBrush"]);
        Assert.NotNull(dictionary["MenuHighlightBorderBrush"]);
        Assert.NotNull(dictionary["MenuDisabledTextBrush"]);
        Assert.NotNull(dictionary["MenuIconBrush"]);
        Assert.NotNull(dictionary["MenuSeparatorBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionForegroundBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionInactiveBrush"]);
        Assert.NotNull(dictionary["StatusBarSelectionInactiveForegroundBrush"]);
        Assert.NotNull(dictionary["TreeSelectionBrush"]);
        Assert.NotNull(dictionary["TreeExpanderBackgroundBrush"]);
        Assert.NotNull(dictionary["TreeExpanderBorderBrush"]);
        Assert.NotNull(dictionary["TreeExpanderForegroundBrush"]);
        Assert.NotNull(dictionary["DiagnosticLineHighlightBrush"]);
        Assert.NotNull(dictionary["DiagnosticLineBorderPen"]);
        Assert.NotNull(dictionary["BreakpointMarkerBrush"]);
        Assert.NotNull(dictionary["BreakpointMarkerColor"]);
        Assert.NotNull(dictionary["StoppedRunLineHighlightBrush"]);
        Assert.NotNull(dictionary["StoppedRunLineBorderPen"]);
        Assert.NotNull(dictionary["StoppedRunExecutionSpanBrush"]);
        Assert.NotNull(dictionary["StoppedRunExecutionSpanBorderPen"]);
        Assert.NotNull(dictionary["LineNumberForegroundBrush"]);
    }

    [Fact]
    public void ShellStyles_DictionaryLoadsCustomTextAreaStyle()
    {
        var dictionary = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellStyles.xaml", UriKind.Relative));

        Assert.NotNull(dictionary[typeof(TextArea)]);
        Assert.NotNull(dictionary["ShellContextMenuStyle"]);
        Assert.NotNull(dictionary["DialogWindowStyle"]);
        Assert.NotNull(dictionary["DialogButtonStyle"]);
        Assert.NotNull(dictionary["DialogComboBoxStyle"]);
        Assert.NotNull(dictionary["QuickAccessToolbarBorderStyle"]);
        Assert.NotNull(dictionary["ShellTabControlStyle"]);
        Assert.NotNull(dictionary["ShellTabItemStyle"]);
        Assert.NotNull(dictionary["DocumentTabControlStyle"]);
        Assert.NotNull(dictionary["DocumentTabItemStyle"]);
        Assert.NotNull(dictionary["DialogTabControlStyle"]);
        Assert.NotNull(dictionary["DialogSectionBorderStyle"]);
        Assert.NotNull(dictionary["FindReplaceTextBoxStyle"]);
        Assert.NotNull(dictionary["FindReplaceActionButtonStyle"]);
        Assert.NotNull(dictionary["FindReplaceLastActionButtonStyle"]);
        Assert.NotNull(dictionary["FindReplaceOptionsBorderStyle"]);
    }

    [Fact]
    public void ShellStringDictionaries_LoadExpectedLocalizationKeys()
    {
        var russian = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellStrings.ru.xaml", UriKind.Relative));
        var english = (ResourceDictionary)Application.LoadComponent(
            new Uri("/Axiom.Ide;component/Resources/ShellStrings.en.xaml", UriKind.Relative));

        Assert.Equal("_Язык", russian["Menu.View.Language"]);
        Assert.Equal("_Language", english["Menu.View.Language"]);
        Assert.Equal("Найти и заменить", russian["FindReplace.Title"]);
        Assert.Equal("Find and Replace", english["FindReplace.Title"]);
        Assert.Equal("Готово.", russian["Status.Ready"]);
        Assert.Equal("Ready.", english["Status.Ready"]);
        Assert.Equal("Очистить", russian["Bottom.Output.Clear"]);
        Assert.Equal("Clear", english["Bottom.Output.Clear"]);
        Assert.Equal("_Открыть просмотр в IDE", russian["Menu.View.EventLog.Viewer"]);
        Assert.Equal("Open _Viewer in IDE", english["Menu.View.EventLog.Viewer"]);
        Assert.Equal("Просмотр журнала действий IDE", russian["EventLog.Viewer.Title"]);
        Assert.Equal("IDE Event Log Viewer", english["EventLog.Viewer.Title"]);
        Assert.Equal("№", russian["EventLog.Viewer.SequenceHeader"]);
        Assert.Equal("Seq", english["EventLog.Viewer.SequenceHeader"]);
        Assert.Equal("События: {0} / {1}", russian["EventLog.Viewer.Count"]);
        Assert.Equal("Events: {0} / {1}", english["EventLog.Viewer.Count"]);
        Assert.Equal("Обновить", russian["EventLog.Viewer.RefreshButton"]);
        Assert.Equal("Refresh", english["EventLog.Viewer.RefreshButton"]);
        Assert.Equal("Копировать", russian["EventLog.Viewer.CopySelectedButton"]);
        Assert.Equal("Copy", english["EventLog.Viewer.CopySelectedButton"]);
        Assert.Equal("Копировать ячейку", russian["EventLog.Viewer.CopyCellMenuItem"]);
        Assert.Equal("Copy cell", english["EventLog.Viewer.CopyCellMenuItem"]);
        Assert.Equal("Копировать событие как JSON", russian["EventLog.Viewer.CopySelectedMenuItem"]);
        Assert.Equal("Copy event as JSON", english["EventLog.Viewer.CopySelectedMenuItem"]);
        Assert.Equal("Копировать подробности", russian["EventLog.Viewer.CopyDetailsMenuItem"]);
        Assert.Equal("Copy details", english["EventLog.Viewer.CopyDetailsMenuItem"]);
        Assert.Equal("Выделить всё", russian["EventLog.Viewer.SelectAllMenuItem"]);
        Assert.Equal("Select all", english["EventLog.Viewer.SelectAllMenuItem"]);
        Assert.Equal("Подробности выбранного события", russian["EventLog.Viewer.SelectedDetailsLabel"]);
        Assert.Equal("Selected event details", english["EventLog.Viewer.SelectedDetailsLabel"]);
        Assert.Equal("Axiom IDE", russian["Window.Title.Default"]);
        Assert.Equal("{0}{1} - Axiom IDE", english["Window.Title.WithDocument"]);
        Assert.Equal("О программе Axiom IDE", russian["About.Title"]);
        Assert.Equal("About Axiom IDE", english["About.Title"]);
        Assert.Contains("оболочка интерпретатора", (string)russian["About.Message"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("interpreter shell", (string)english["About.Message"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Окно «О программе» открыто.", russian["About.StatusOpened"]);
        Assert.Equal("Окно «О программе» закрыто.", russian["About.StatusClosed"]);
        Assert.Equal("About dialog opened.", english["About.StatusOpened"]);
        Assert.Equal("About dialog closed.", english["About.StatusClosed"]);
        Assert.Equal("Save Changes", english["Prompts.SaveChanges.Title"]);
        Assert.Equal("Do you want to save changes to '{0}'?", english["Prompts.SaveChanges.Message"]);
        Assert.Equal("Axiom IDE", english["Prompts.DefaultTitle"]);
        Assert.Equal("OK", english["Prompts.Ok"]);
        Assert.Equal("Axiom IDE", russian["Prompts.DefaultTitle"]);
        Assert.Equal("OK", russian["Prompts.Ok"]);
        Assert.Equal("Открыт {0}.", russian["Documents.Open.StatusOpened"]);
        Assert.Equal("Opened {0}.", english["Documents.Open.StatusOpened"]);
        Assert.Equal("Все документы сохранены.", russian["Documents.SaveAll.StatusSaved"]);
        Assert.Equal("All documents saved.", english["Documents.SaveAll.StatusSaved"]);
        Assert.Equal("Unexpected execution error: {0}", english["Run.Infrastructure.UnexpectedError"]);
        Assert.Equal("_Запуск программы", russian["Menu.Run.Run"]);
        Assert.Equal("_Run Program", english["Menu.Run.Run"]);
        Assert.Equal("_Отладка", russian["Menu.Debug"]);
        Assert.Equal("_Debug", english["Menu.Debug"]);
        Assert.Equal("_Начать/продолжить отладку", russian["Menu.Debug.StartDebugging"]);
        Assert.Equal("Start/_Continue Debugging", english["Menu.Debug.StartDebugging"]);
        Assert.Equal("Шаг _назад", russian["Menu.Debug.StepBack"]);
        Assert.Equal("Step _Back", english["Menu.Debug.StepBack"]);
        Assert.Equal("Пошагово с _заходом в процедуру/функцию", russian["Menu.Debug.StepInto"]);
        Assert.Equal("Step _Into", english["Menu.Debug.StepInto"]);
        Assert.Equal("Пошагово _без захода в процедуру/функцию", russian["Menu.Debug.StepOver"]);
        Assert.Equal("Step _Over", english["Menu.Debug.StepOver"]);
        Assert.Equal("_Выйти из процедуры/функции", russian["Menu.Debug.StepOut"]);
        Assert.Equal("Step O_ut", english["Menu.Debug.StepOut"]);
        Assert.Equal("_Остановить отладку", russian["Menu.Debug.StopDebugging"]);
        Assert.Equal("Stop _Debugging", english["Menu.Debug.StopDebugging"]);
        Assert.Equal("_Перезапустить отладку", russian["Menu.Debug.RestartDebugging"]);
        Assert.Equal("_Restart Debugging", english["Menu.Debug.RestartDebugging"]);
        Assert.Equal("_Установить/снять точку остановки", russian["Menu.Debug.ToggleBreakpoint"]);
        Assert.Equal("_Set/Clear Breakpoint", english["Menu.Debug.ToggleBreakpoint"]);
        Assert.Equal("_Очистить все точки остановки", russian["Menu.Debug.ClearAllBreakpoints"]);
        Assert.Equal("_Clear All Breakpoints", english["Menu.Debug.ClearAllBreakpoints"]);
        Assert.Equal("Очищено точек остановки: {0}.", russian["Run.Breakpoints.ClearedAll"]);
        Assert.Equal("Cleared {0} breakpoints.", english["Run.Breakpoints.ClearedAll"]);
        Assert.NotNull(russian["Run.Breakpoints.ClearedAllAndStoppedDebugging"]);
        Assert.Equal("Cleared {0} breakpoints. Debugging stopped.", english["Run.Breakpoints.ClearedAllAndStoppedDebugging"]);
        Assert.Equal("Отладка остановлена.", russian["Run.StatusDebuggingStopped"]);
        Assert.Equal("Debugging stopped.", english["Run.StatusDebuggingStopped"]);
        Assert.Equal("Пошаговое выполнение без захода в процедуру или функцию...", russian["Run.StatusSteppingOver"]);
        Assert.Equal("Stepping over...", english["Run.StatusSteppingOver"]);
        Assert.Equal("Пошаговое выполнение с заходом в процедуру или функцию...", russian["Run.StatusSteppingInto"]);
        Assert.Equal("Stepping into...", english["Run.StatusSteppingInto"]);
        Assert.Equal("Выполняется выход из текущей процедуры или функции...", russian["Run.StatusSteppingOut"]);
        Assert.Equal("Stepping out...", english["Run.StatusSteppingOut"]);
        Assert.Equal("Выполняется шаг назад...", russian["Run.StatusSteppingBack"]);
        Assert.Equal("Stepping back...", english["Run.StatusSteppingBack"]);
        Assert.Equal("Перезапуск отладки...", russian["Run.StatusRestartingDebugging"]);
        Assert.Equal("Restarting debugging...", english["Run.StatusRestartingDebugging"]);
        Assert.Equal("Запуск программы", russian["Toolbar.Run"]);
        Assert.Equal("Run Program", english["Toolbar.Run"]);
        Assert.Equal("Начать/продолжить отладку", russian["Toolbar.StartDebugging"]);
        Assert.Equal("Start/Continue Debugging", english["Toolbar.StartDebugging"]);
        Assert.Equal("Шаг назад", russian["Toolbar.StepBack"]);
        Assert.Equal("Step Back", english["Toolbar.StepBack"]);
        Assert.Equal("Пошагово с заходом в процедуру/функцию", russian["Toolbar.StepInto"]);
        Assert.Equal("Step Into", english["Toolbar.StepInto"]);
        Assert.Equal("Пошагово без захода в процедуру/функцию", russian["Toolbar.StepOver"]);
        Assert.Equal("Step Over", english["Toolbar.StepOver"]);
        Assert.Equal("Выйти из процедуры/функции", russian["Toolbar.StepOut"]);
        Assert.Equal("Step Out", english["Toolbar.StepOut"]);
        Assert.Equal("Перезапустить отладку", russian["Toolbar.RestartDebugging"]);
        Assert.Equal("Restart Debugging", english["Toolbar.RestartDebugging"]);
        Assert.Equal("Остановить отладку", russian["Toolbar.StopDebugging"]);
        Assert.Equal("Stop Debugging", english["Toolbar.StopDebugging"]);
        Assert.Equal("Очистить все точки остановки", russian["Toolbar.ClearAllBreakpoints"]);
        Assert.Equal("Clear All Breakpoints", english["Toolbar.ClearAllBreakpoints"]);
        Assert.Equal("Необработанная ошибка IDE.{0}{0}{1}{0}{0}Журнал: {2}", russian["App.Error.UnhandledMessage"]);
    }
}
