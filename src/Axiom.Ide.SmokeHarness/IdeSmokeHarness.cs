using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;

namespace Axiom.Ide.SmokeHarness;

internal sealed class IdeSmokeHarness
{
    private const int TimeoutMilliseconds = 30000;
    private const int PollDelayMilliseconds = 100;
    private const uint WmClose = 0x0010;
    private const uint WmAppSmokeHarnessShowFindReplace = 0x8001;
    private const uint WmAppSmokeHarnessCloseTab = 0x8002;
    private const uint WmAppSmokeHarnessRun = 0x8003;
    private const uint WmAppSmokeHarnessToggleFilesPane = 0x8004;
    private const uint WmAppSmokeHarnessToggleOutputPane = 0x8005;
    private const uint WmAppSmokeHarnessToggleDiagnosticsPane = 0x8006;
    private const uint WmAppSmokeHarnessToggleStatusBar = 0x8007;
    private const uint MouseeventfLeftDown = 0x0002;
    private const uint MouseeventfLeftUp = 0x0004;
    private const uint MouseeventfRightDown = 0x0008;
    private const uint MouseeventfRightUp = 0x0010;
    private const uint MouseeventfWheel = 0x0800;
    private const uint KeyeventfKeyUp = 0x0002;
    private const byte VkLeft = 0x25;
    private const byte VkReturn = 0x0D;
    private const byte VkEscape = 0x1B;

    private readonly SmokeHarnessOptions _options;
    private readonly string _repositoryRoot;
    private readonly string _ideExecutablePath;
    private readonly string _stateFilePath;
    private readonly string _workspaceRootPath;
    private readonly string _configFilePath;
    private readonly string _styleFilePath;
    private readonly string _blockedExecutablePath;
    private readonly string _hiddenFilePath;
    private readonly string _successProgramPath;
    private readonly string _syntaxErrorProgramPath;
    private readonly string _inKeyProgramPath;
    private readonly string _onMouseProgramPath;
    private Process? _ideProcess;
    private byte[]? _originalStateBytes;
    private bool _stateFileExisted;

    public IdeSmokeHarness(SmokeHarnessOptions options)
    {
        _options = options;
        _repositoryRoot = FindRepositoryRoot();
        _ideExecutablePath = ResolveIdeExecutablePath(_repositoryRoot, options.IdeExecutablePath);
        _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Axiom",
            "Axiom.Ide",
            "recent-files.json");

        _workspaceRootPath = Path.Combine(
            _repositoryRoot,
            ".artifacts",
            "ui-smoke",
            DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));

        _configFilePath = Path.Combine(_workspaceRootPath, "config.json");
        _styleFilePath = Path.Combine(_workspaceRootPath, "styles.css");
        _blockedExecutablePath = Path.Combine(_workspaceRootPath, "blocked.exe");
        _hiddenFilePath = Path.Combine(_workspaceRootPath, "hidden-note.txt");
        _successProgramPath = Path.Combine(_workspaceRootPath, "success.axm");
        _syntaxErrorProgramPath = Path.Combine(_workspaceRootPath, "syntax_error.axm");
        _inKeyProgramPath = Path.Combine(_workspaceRootPath, "poll_keys.axm");
        _onMouseProgramPath = Path.Combine(_workspaceRootPath, "on_mouse.axm");
    }

    public void Run()
    {
        Console.WriteLine($"Repository: {_repositoryRoot}");
        Console.WriteLine($"IDE executable: {_ideExecutablePath}");
        Console.WriteLine($"Workspace: {_workspaceRootPath}");
        Console.WriteLine();

        BackupStateFile();
        PrepareWorkspace();

        try
        {
            WriteShellState(isFilesPaneVisible: true, isStatusBarVisible: true, windowState: "Normal");

            LaunchIde();
            var mainWindow = WaitForMainWindow();

            if (_options.EventLogOnly)
            {
                RunEventLogOnlySmoke(mainWindow);
                Console.WriteLine();
                Console.WriteLine("Axiom IDE event log smoke completed successfully.");
                return;
            }

            if (_options.InKeyOnly)
            {
                RunInKeyOnlySmoke(mainWindow);
                Console.WriteLine();
                Console.WriteLine("Axiom IDE INKEY smoke completed successfully.");
                return;
            }

            if (_options.OnMouseOnly)
            {
                RunOnMouseOnlySmoke(mainWindow);
                Console.WriteLine();
                Console.WriteLine("Axiom IDE ON MOUSE smoke completed successfully.");
                return;
            }

            RunUiSmoke(mainWindow);

            ReportStep("Persist maximized window + hidden file manager");
            SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleFilesPane, "toggle File Manager");
            SetWindowVisualState(mainWindow, WindowVisualState.Maximized);
            CloseWindow(mainWindow, expectProcessExit: true);

            LaunchIde();
            mainWindow = WaitForMainWindow();
            AssertWindowState(mainWindow, WindowVisualState.Maximized);
            AssertHidden(mainWindow, "FilesTreeView");
            ReportStepCompleted();

            Console.WriteLine();
            Console.WriteLine("Axiom IDE smoke harness completed successfully.");
        }
        finally
        {
            CleanupProcess();
            RestoreStateFile();

            if (!_options.KeepWorkspace && Directory.Exists(_workspaceRootPath))
            {
                Directory.Delete(_workspaceRootPath, recursive: true);
            }
        }
    }

    private void RunEventLogOnlySmoke(AutomationElement mainWindow)
    {
        ReportStep("Initial shell state");
        RequireElement(mainWindow, "MainMenu");
        RequireElement(mainWindow, "ShellStatusBar");
        ReportStepCompleted();

        ReportStep("IDE event log viewer shows menu command events");
        RunIdeEventLogViewerSmoke(mainWindow);
        ReportStepCompleted();

        ReportStep("Close IDE after event log smoke");
        CloseWindow(mainWindow, expectProcessExit: true);
        ReportStepCompleted();
    }

    private void RunUiSmoke(AutomationElement mainWindow)
    {
        ReportStep("Initial shell state");
        RequireElement(mainWindow, "MainMenu");
        RequireElement(mainWindow, "FilesTreeView");
        RequireElement(mainWindow, "ShellStatusBar");
        ReportStepCompleted();

        ReportStep("Open editable text files from File Manager");
        OpenFileFromTree(mainWindow, "config.json");
        AssertWindowTitleContains(mainWindow, "config.json");
        OpenFileFromTree(mainWindow, "styles.css");
        AssertWindowTitleContains(mainWindow, "styles.css");
        ReportStepCompleted();

        ReportStep("Do not open .exe from File Manager");
        var previousTitle = mainWindow.Current.Name;
        SelectTreeItem(mainWindow, "blocked.exe");
        Thread.Sleep(500);
        WaitForWindowTitle(mainWindow, title => string.Equals(title, previousTitle, StringComparison.OrdinalIgnoreCase));
        ReportStepCompleted();

        ReportStep("File Manager context menu + file operations");
        RunFileManagerContextMenuSmoke(mainWindow);
        ReportStepCompleted();

        ReportStep("Find / Replace dialog + regex mode");
        OpenFileFromTree(mainWindow, "config.json");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessShowFindReplace, "show Find / Replace dialog");
        var findDialog = WaitForWindow("FindReplaceDialog");
        SetElementValue(findDialog, "SearchPatternTextBox", "demo");
        SetElementValue(findDialog, "ReplacePatternTextBox", "smoke");
        var previousStatus = ReadElementText(mainWindow, "StatusBarTextBlock");
        InvokeButton(findDialog, "FindNextButton");
        WaitForStatusText(text => !string.IsNullOrWhiteSpace(text)
            && !string.Equals(text, previousStatus, StringComparison.Ordinal));
        var findStatus = ReadElementText(mainWindow, "StatusBarTextBlock");
        InvokeButton(findDialog, "ReplaceAllButton");
        WaitForStatusText(text => text.Contains("1", StringComparison.Ordinal)
            && !string.Equals(text, findStatus, StringComparison.Ordinal));
        SetToggleState(findDialog, "UseRegexCheckBox", expectedOn: true);
        SetElementValue(findDialog, "SearchPatternTextBox", "\"[a-z]+\"");
        SetElementValue(findDialog, "ReplacePatternTextBox", "\"regex\"");
        InvokeButton(findDialog, "ReplaceAllButton");
        WaitForStatusText(text => text.Contains("4", StringComparison.Ordinal));
        CloseWindow(findDialog);
        ReportStepCompleted();

        ReportStep("Discard dirty tab on close");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessCloseTab, "close active tab");
        var saveChangesDialog = WaitForMessageBox("Save Changes");
        InvokeDialogButton(saveChangesDialog, "7");
        WaitForWindowTitle(mainWindow, static title => !title.Contains("config.json", StringComparison.OrdinalIgnoreCase));
        ReportStepCompleted();

        ReportStep("Run success -> Output");
        OpenFileFromTree(mainWindow, Path.GetFileName(_successProgramPath));
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessRun, "run active document");
        WaitForText("OutputTextBox", static text => text.Contains("42", StringComparison.Ordinal));
        ReportStepCompleted();

        ReportStep("Run INKEY sample -> Output keyboard input");
        OpenFileFromTree(mainWindow, Path.GetFileName(_inKeyProgramPath));
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessRun, "run active document");
        WaitForText(
            "OutputTextBox",
            static text => text.Contains("Click the output pane and press a key while the loop runs.", StringComparison.Ordinal));
        var outputTextBox = RequireElement(mainWindow, "OutputTextBox");
        ClickElement(outputTextBox);
        PressVirtualKey(outputTextBox, VkLeft);
        WaitForText("OutputTextBox", static text => text.Contains("LEFT", StringComparison.Ordinal));
        ReportStepCompleted();

        ReportStep("Run failure -> Diagnostics");
        OpenFileFromTree(mainWindow, Path.GetFileName(_syntaxErrorProgramPath));
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessRun, "run active document");
        WaitForDataGridRows(mainWindow, "DiagnosticsDataGrid", minimumRowCount: 1);
        ReportStepCompleted();

        ReportStep("IDE event log viewer shows menu command events");
        RunIdeEventLogViewerSmoke(mainWindow);
        ReportStepCompleted();

        ReportStep("Bottom pane toggles");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleOutputPane, "toggle Output pane");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleDiagnosticsPane, "toggle Diagnostics pane");
        AssertHidden(mainWindow, "BottomPaneTabs");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleOutputPane, "toggle Output pane");
        RequireElement(mainWindow, "BottomPaneTabs");
        ReportStepCompleted();

        ReportStep("Status bar toggle");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleStatusBar, "toggle status bar");
        AssertHidden(mainWindow, "ShellStatusBar");
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessToggleStatusBar, "toggle status bar");
        RequireElement(mainWindow, "ShellStatusBar");
        ReportStepCompleted();
    }

    private void RunInKeyOnlySmoke(AutomationElement mainWindow)
    {
        ReportStep("Initial shell state");
        RequireElement(mainWindow, "MainMenu");
        RequireElement(mainWindow, "FilesTreeView");
        RequireElement(mainWindow, "OutputTextBox");
        ReportStepCompleted();

        ReportStep("Run INKEY sample -> Output keyboard input");
        OpenFileFromTree(mainWindow, Path.GetFileName(_inKeyProgramPath));
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessRun, "run active document");
        WaitForText(
            "OutputTextBox",
            static text => text.Contains("Click the output pane and press a key while the loop runs.", StringComparison.Ordinal));
        var outputTextBox = RequireElement(mainWindow, "OutputTextBox");
        ClickElement(outputTextBox);
        PressVirtualKey(outputTextBox, VkLeft);
        WaitForText("OutputTextBox", static text => text.Contains("LEFT", StringComparison.Ordinal));
        ReportStepCompleted();

        ReportStep("Close IDE after INKEY smoke");
        CloseWindow(mainWindow, expectProcessExit: true);
        ReportStepCompleted();
    }

    private void RunOnMouseOnlySmoke(AutomationElement mainWindow)
    {
        ReportStep("Initial shell state");
        RequireElement(mainWindow, "MainMenu");
        RequireElement(mainWindow, "FilesTreeView");
        RequireElement(mainWindow, "OutputTextBox");
        ReportStepCompleted();

        ReportStep("Run ON MOUSE sample -> Output mouse input");
        OpenFileFromTree(mainWindow, Path.GetFileName(_onMouseProgramPath));
        SendSmokeCommand(mainWindow, WmAppSmokeHarnessRun, "run active document");
        WaitForLatestIdeLogText(static text =>
            text.Contains("console.output | Click inside Output, roll the mouse wheel, then press ESC.", StringComparison.Ordinal));
        var outputTextBox = RequireElement(mainWindow, "OutputTextBox");
        ClickElement(outputTextBox);
        mouse_event(MouseeventfWheel, 0, 0, 120, UIntPtr.Zero);
        WaitForLatestIdeLogText(static text =>
            text.Contains("console.output | PRESS", StringComparison.Ordinal)
            && text.Contains("console.output | UP", StringComparison.Ordinal)
            && text.Contains("console.output | WHEEL", StringComparison.Ordinal));
        PressVirtualKey(outputTextBox, VkEscape);
        WaitForLatestIdeLogText(static text => text.Contains("console.output | DONE", StringComparison.Ordinal));
        ReportStepCompleted();

        ReportStep("Close IDE after ON MOUSE smoke");
        CloseWindow(mainWindow, expectProcessExit: true);
        ReportStepCompleted();
    }

    private void RunFileManagerContextMenuSmoke(AutomationElement mainWindow)
    {
        const string createdFileName = "context-created.axm";
        const string renamedFileName = "context-renamed.axm";
        const string moveFileName = "context-move.axm";
        const string folderName = "context-folder";

        AssertWorkspaceNodeMissing(_hiddenFilePath);
        OpenTreeItemContextMenu(mainWindow);
        ActivateContextMenuItem("FilesTreeShowHiddenFilesMenuItem");
        WaitForWorkspaceNode(mainWindow, _hiddenFilePath);

        OpenTreeItemContextMenu(mainWindow);
        ActivateContextMenuItem("FilesTreeNewFileMenuItem");
        CompleteTextInputDialog(createdFileName);
        WaitFor(
            () => File.Exists(Path.Combine(_workspaceRootPath, createdFileName)) ? true : (bool?)null,
            $"Created file '{createdFileName}' did not appear on disk. Status: '{ReadElementText(mainWindow, "StatusBarTextBlock")}'.");
        WaitForWorkspaceNode(mainWindow, createdFileName);
        AssertWindowTitleContains(mainWindow, createdFileName);

        OpenTreeItemContextMenu(mainWindow, createdFileName);
        ActivateContextMenuItem("FilesTreeRenameMenuItem");
        CompleteTextInputDialog(renamedFileName);
        WaitForWorkspaceNode(mainWindow, renamedFileName);
        AssertWorkspaceNodeMissing(createdFileName);
        AssertWindowTitleContains(mainWindow, renamedFileName);

        OpenTreeItemContextMenu(mainWindow);
        ActivateContextMenuItem("FilesTreeNewDirectoryMenuItem");
        CompleteTextInputDialog(folderName);
        WaitForWorkspaceNode(mainWindow, folderName);

        OpenTreeItemContextMenu(mainWindow, renamedFileName);
        ActivateContextMenuItem("FilesTreeCopyMenuItem");
        OpenTreeItemContextMenu(mainWindow, folderName);
        ActivateContextMenuItem("FilesTreePasteMenuItem");
        WaitForWorkspaceNode(mainWindow, Path.Combine(folderName, renamedFileName));
        WaitForWorkspaceNode(mainWindow, renamedFileName);

        OpenTreeItemContextMenu(mainWindow);
        ActivateContextMenuItem("FilesTreeNewFileMenuItem");
        CompleteTextInputDialog(moveFileName);
        WaitForWorkspaceNode(mainWindow, moveFileName);
        AssertWindowTitleContains(mainWindow, moveFileName);

        OpenTreeItemContextMenu(mainWindow, moveFileName);
        ActivateContextMenuItem("FilesTreeCutMenuItem");
        OpenTreeItemContextMenu(mainWindow, folderName);
        ActivateContextMenuItem("FilesTreePasteMenuItem");
        WaitForWorkspaceNode(mainWindow, Path.Combine(folderName, moveFileName));
        AssertWorkspaceNodeMissing(moveFileName);

        OpenTreeItemContextMenu(mainWindow, Path.Combine(folderName, moveFileName));
        ActivateContextMenuItem("FilesTreeDeleteMenuItem");
        var deleteDialog = WaitForAnyOwnedWindow(excludedAutomationIds: ["MainWindow", "FindReplaceDialog", "TextInputDialog"]);
        AcceptDialog(deleteDialog);
        AssertWorkspaceNodeMissing(Path.Combine(folderName, moveFileName));
    }

    private void RunIdeEventLogViewerSmoke(AutomationElement mainWindow)
    {
        InvokeMenuItem(mainWindow, "ViewMenuItem", "ViewLanguageMenuItem", "ViewLanguageEnglishMenuItem");
        InvokeMenuItem(mainWindow, "RunMenuItem", "ExecutionModeMenuItem", "ExecutionModeInterpreterMenuItem");
        InvokeMenuItem(mainWindow, "FormatMenuItem", "FormatSmartIdentifierMenuItem");
        InvokeMenuItem(mainWindow, "HelpMenuItem", "HelpAboutMenuItem");

        var informationDialog = WaitForWindow("InformationDialog");
        InvokeButton(informationDialog, "InformationDialogOkButton");
        WaitFor(
            () => FindElementForProcess("InformationDialog") is null ? true : (bool?)null,
            "Timed out waiting for InformationDialog to close.");

        InvokeMenuItem(mainWindow, "ViewMenuItem", "ViewEventLogMenuItem", "ViewEventLogViewerMenuItem");
        var viewerWindow = WaitForWindow("IdeEventLogViewerWindow");

        AssertIdeEventLogViewerEntry(
            viewerWindow,
            "UseEnglishLanguage",
            "commandName: UseEnglishLanguage",
            "menuName: View",
            "language: English");
        AssertIdeEventLogViewerEntry(
            viewerWindow,
            "UseInterpreterExecutionMode",
            "commandName: UseInterpreterExecutionMode",
            "menuName: Run",
            "executionMode: Interpreter");
        AssertIdeEventLogViewerEntry(
            viewerWindow,
            "ToggleSmartIdentifier",
            "commandName: ToggleSmartIdentifier",
            "menuName: Format",
            "enabled: True");
        AssertIdeEventLogViewerEntry(
            viewerWindow,
            "About",
            "commandName: About",
            "menuName: Help");

        InvokeButton(viewerWindow, "IdeEventLogViewerCloseButton");
        WaitFor(
            () => FindElementForProcess("IdeEventLogViewerWindow") is null ? true : (bool?)null,
            "Timed out waiting for IdeEventLogViewerWindow to close.");
    }

    private void AssertIdeEventLogViewerEntry(
        AutomationElement viewerWindow,
        string searchText,
        params string[] expectedFragments)
    {
        SetElementValue(viewerWindow, "IdeEventLogViewerSearchTextBox", searchText);
        InvokeButton(viewerWindow, "IdeEventLogViewerRefreshButton");
        WaitForElementText(
            viewerWindow,
            "IdeEventLogViewerSelectedDetailsTextBox",
            text => expectedFragments.All(fragment => text.Contains(fragment, StringComparison.Ordinal)),
            $"Timed out waiting for event log viewer details for '{searchText}'.");
    }

    private void LaunchIde()
    {
        CleanupProcess();

        var startInfo = new ProcessStartInfo
        {
            FileName = _ideExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(_ideExecutablePath)!,
            UseShellExecute = true
        };

        _ideProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Axiom.Ide.");

        _ideProcess.WaitForInputIdle(TimeoutMilliseconds);
    }

    private AutomationElement WaitForMainWindow()
    {
        return WaitFor(
            () =>
            {
                var processId = GetProcessId();
                return AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                        new PropertyCondition(AutomationElement.AutomationIdProperty, "MainWindow")));
            },
            "Timed out waiting for Axiom IDE main window.");
    }

    private AutomationElement WaitForWindow(string automationId)
    {
        return WaitFor(
            () =>
            {
                var processId = GetProcessId();
                return AutomationElement.RootElement.FindFirst(
                    TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                        new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)));
            },
            $"Timed out waiting for window '{automationId}'.");
    }

    private AutomationElement WaitForMessageBox(string title)
    {
        return WaitFor(
            () =>
            {
                var processId = GetProcessId();
                return AutomationElement.RootElement.FindFirst(
                    TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                        new PropertyCondition(AutomationElement.NameProperty, title)));
            },
            $"Timed out waiting for dialog '{title}'.");
    }

    private AutomationElement WaitForAnyOwnedWindow(IReadOnlyCollection<string> excludedAutomationIds)
    {
        return WaitFor(
            () =>
            {
                var processId = GetProcessId();
                var windows = AutomationElement.RootElement.FindAll(
                    TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));

                foreach (AutomationElement window in windows)
                {
                    var automationId = window.Current.AutomationId;
                    if (excludedAutomationIds.Contains(automationId, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    return window;
                }

                return null;
            },
            "Timed out waiting for owned dialog window.");
    }

    private void OpenFileFromTree(AutomationElement mainWindow, string fileName)
    {
        SelectTreeItem(mainWindow, fileName);
        WaitForWindowTitle(mainWindow, title => title.Contains(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private void SelectTreeItem(AutomationElement mainWindow, string workspaceRelativePath)
    {
        var treeItem = WaitForWorkspaceNode(mainWindow, workspaceRelativePath);
        SelectTreeItem(treeItem);
    }

    private static void SelectTreeItem(AutomationElement treeItem)
    {
        BringElementWindowToForeground(treeItem);

        if (treeItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return;
        }

        if (treeItem.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return;
        }

        throw new InvalidOperationException("Tree item does not support selection or invoke.");
    }

    private static void TrySelectTreeItemWithoutInvoke(AutomationElement treeItem)
    {
        BringElementWindowToForeground(treeItem);

        if (treeItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            Thread.Sleep(100);
        }
    }

    private void OpenTreeItemContextMenu(AutomationElement mainWindow, string? workspaceRelativePath = null)
    {
        var treeItem = workspaceRelativePath is null
            ? EnsureWorkspaceDirectoryExpanded(mainWindow)
            : WaitForWorkspaceNode(mainWindow, workspaceRelativePath);

        TrySelectTreeItemWithoutInvoke(treeItem);
        RightClickElement(treeItem);
    }

    private void ActivateContextMenuItem(string automationId)
    {
        var menuItem = WaitFor(
            () => FindElementForProcess(automationId),
            $"Could not find context menu item '{automationId}'.");

        if (menuItem.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return;
        }

        if (menuItem.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            ((TogglePattern)togglePattern).Toggle();
            return;
        }

        ClickElement(menuItem);
    }

    private void CompleteTextInputDialog(string value)
    {
        var dialog = WaitForWindow("TextInputDialog");
        SetElementValue(dialog, "TextInputTextBox", value);
        ClickElement(dialog, "TextInputOkButton");
        WaitFor(
            () => FindElementForProcess("TextInputDialog") is null ? true : (bool?)null,
            "Timed out waiting for text input dialog to close.");
    }

    private AutomationElement WaitForWorkspaceNode(AutomationElement mainWindow, string workspaceRelativePath)
    {
        return WaitFor(
            () => TryFindWorkspaceNode(mainWindow, workspaceRelativePath),
            $"Could not find workspace node '{workspaceRelativePath}'.");
    }

    private void AssertWorkspaceNodeMissing(string workspaceRelativePath)
    {
        WaitFor(
            () => TryFindWorkspaceNode(RequireMainWindow(), workspaceRelativePath) is null ? true : (bool?)null,
            $"Workspace node '{workspaceRelativePath}' is still present.");
    }

    private AutomationElement? TryFindWorkspaceNode(AutomationElement mainWindow, string workspaceRelativePath)
    {
        var currentNode = EnsureWorkspaceDirectoryExpanded(mainWindow);
        var segments = GetWorkspaceRelativeSegments(workspaceRelativePath);

        if (segments.Count == 0)
        {
            return currentNode;
        }

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var isLastSegment = index == segments.Count - 1;
            var childNode = FindImmediateChildTreeItemByLabel(currentNode, segment);

            if (childNode is null)
            {
                return null;
            }

            if (!isLastSegment)
            {
                ExpandTreeItem(childNode);
            }

            currentNode = childNode;
        }

        return currentNode;
    }

    private AutomationElement EnsureWorkspaceDirectoryExpanded(AutomationElement mainWindow)
    {
        var tree = RequireElement(mainWindow, "FilesTreeView");
        var currentNode = WaitFor(
            () => FindTreeItemByLabel(tree, BuildDriveLabel(_workspaceRootPath)),
            "Could not find workspace drive root in FilesTreeView.");

        ExpandTreeItem(currentNode);

        foreach (var segment in GetPathSegments(_workspaceRootPath))
        {
            currentNode = WaitFor(
                () => FindTreeItemByLabel(currentNode, segment),
                $"Could not find directory '{segment}' while expanding workspace path.");
            ExpandTreeItem(currentNode);
        }

        return currentNode;
    }

    private static void ExpandTreeItem(AutomationElement treeItem)
    {
        if (!treeItem.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePattern))
        {
            return;
        }

        var pattern = (ExpandCollapsePattern)expandCollapsePattern;

        if (pattern.Current.ExpandCollapseState is ExpandCollapseState.Expanded or ExpandCollapseState.LeafNode)
        {
            return;
        }

        pattern.Expand();
        Thread.Sleep(250);
    }

    private static AutomationElement? FindTreeItemByLabel(AutomationElement root, string label)
    {
        var labelElement = root.FindFirst(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.NameProperty, label)));

        return labelElement is null
            ? null
            : FindAncestorTreeItem(labelElement);
    }

    private static AutomationElement? FindImmediateChildTreeItemByLabel(AutomationElement parentTreeItem, string label)
    {
        var labelElements = parentTreeItem.FindAll(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.NameProperty, label)));

        foreach (AutomationElement labelElement in labelElements)
        {
            var treeItem = FindAncestorTreeItem(labelElement);
            if (treeItem is null)
            {
                continue;
            }

            var parentTreeNode = FindParentTreeItem(treeItem);
            if (parentTreeNode is not null && Automation.Compare(parentTreeNode, parentTreeItem))
            {
                return treeItem;
            }
        }

        return null;
    }

    private static AutomationElement? FindAncestorTreeItem(AutomationElement element)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = walker.GetParent(element);

        while (current is not null)
        {
            if (current.Current.ControlType == ControlType.TreeItem)
            {
                return current;
            }

            current = walker.GetParent(current);
        }

        return null;
    }

    private static AutomationElement? FindParentTreeItem(AutomationElement element)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = walker.GetParent(element);

        while (current is not null)
        {
            if (current.Current.ControlType == ControlType.TreeItem)
            {
                return current;
            }

            current = walker.GetParent(current);
        }

        return null;
    }

    private static string BuildDriveLabel(string path)
    {
        var driveRoot = Path.GetPathRoot(path);

        if (string.IsNullOrWhiteSpace(driveRoot))
        {
            throw new InvalidOperationException($"Could not resolve drive root for '{path}'.");
        }

        return $"Disk ({driveRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)})";
    }

    private static IReadOnlyList<string> GetPathSegments(string path)
    {
        var driveRoot = Path.GetPathRoot(path);

        if (string.IsNullOrWhiteSpace(driveRoot))
        {
            return [];
        }

        return path[driveRoot.Length..]
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    }

    private IReadOnlyList<string> GetWorkspaceRelativeSegments(string path)
    {
        var relativePath = Path.IsPathRooted(path)
            ? Path.GetRelativePath(_workspaceRootPath, path)
            : path;

        if (string.Equals(relativePath, ".", StringComparison.Ordinal))
        {
            return [];
        }

        return relativePath
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    }

    private void InvokeMenuItem(AutomationElement mainWindow, params string[] automationIds)
    {
        if (automationIds.Length < 2)
        {
            throw new ArgumentException("A menu path must contain at least a top-level menu and one item.", nameof(automationIds));
        }

        BringElementWindowToForeground(mainWindow);
        ClickElement(mainWindow, automationIds[0]);

        for (var index = 1; index < automationIds.Length; index++)
        {
            var automationId = automationIds[index];
            var current = WaitFor(
                () => FindElementForProcess(automationId),
                $"Could not find menu item '{automationIds[index]}'.");
            ClickElement(current);
        }
    }

    private void ToggleViewMenu(AutomationElement mainWindow, string automationId, bool expectedOn)
    {
        var currentState = GetMenuToggleState(mainWindow, automationId);

        if (currentState == expectedOn)
        {
            return;
        }

        InvokeMenuItem(mainWindow, "ViewMenuItem", automationId);
        WaitFor(
            () => GetMenuToggleState(mainWindow, automationId) == expectedOn ? true : (bool?)null,
            $"Menu item '{automationId}' did not reach expected state {expectedOn}.");
    }

    private bool GetMenuToggleState(AutomationElement mainWindow, string automationId)
    {
        var viewMenu = RequireElement(mainWindow, "ViewMenuItem");

        if (viewMenu.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePattern))
        {
            ((ExpandCollapsePattern)expandCollapsePattern).Expand();
        }

        var menuItem = WaitFor(
            () => viewMenu.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)),
            $"Could not find view menu item '{automationId}'.");

        if (!menuItem.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            throw new InvalidOperationException($"Menu item '{automationId}' does not support TogglePattern.");
        }

        return ((TogglePattern)togglePattern).Current.ToggleState == ToggleState.On;
    }

    private void InvokeButton(AutomationElement root, string automationId)
    {
        var button = RequireElement(root, automationId);

        if (!button.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            throw new InvalidOperationException($"Button '{automationId}' does not support InvokePattern.");
        }

        ((InvokePattern)invokePattern).Invoke();
    }

    private void SetElementValue(AutomationElement root, string automationId, string value)
    {
        var element = RequireElement(root, automationId);

        if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            throw new InvalidOperationException($"Element '{automationId}' does not support ValuePattern.");
        }

        ((ValuePattern)valuePattern).SetValue(value);
    }

    private void SetToggleState(AutomationElement root, string automationId, bool expectedOn)
    {
        var element = RequireElement(root, automationId);

        if (!element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            throw new InvalidOperationException($"Element '{automationId}' does not support TogglePattern.");
        }

        var pattern = (TogglePattern)togglePattern;
        var expectedState = expectedOn ? ToggleState.On : ToggleState.Off;

        if (pattern.Current.ToggleState == expectedState)
        {
            return;
        }

        pattern.Toggle();
        WaitFor(
            () => GetToggleState(root, automationId) == expectedState ? true : (bool?)null,
            $"Element '{automationId}' did not reach expected toggle state {expectedState}.");
    }

    private ToggleState GetToggleState(AutomationElement root, string automationId)
    {
        var element = RequireElement(root, automationId);

        if (!element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            throw new InvalidOperationException($"Element '{automationId}' does not support TogglePattern.");
        }

        return ((TogglePattern)togglePattern).Current.ToggleState;
    }

    private void InvokeDialogButton(AutomationElement dialog, string automationId)
    {
        var button = WaitFor(
            () => dialog.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.AutomationIdProperty, automationId))),
            $"Could not find dialog button '{automationId}'.");

        if (!button.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            throw new InvalidOperationException($"Dialog button '{automationId}' does not support InvokePattern.");
        }

        ((InvokePattern)invokePattern).Invoke();
    }

    private void AcceptDialog(AutomationElement dialog)
    {
        var button = dialog.FindFirst(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new OrCondition(
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "6"),
                    new PropertyCondition(AutomationElement.NameProperty, "Да"),
                    new PropertyCondition(AutomationElement.NameProperty, "Yes"))));

        if (button is null)
        {
            button = dialog.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        }

        if (button is null)
        {
            PressVirtualKey(dialog, VkReturn);
            TryWaitForWindowClosed(dialog);
            return;
        }

        if (button.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            TryWaitForWindowClosed(dialog);
            return;
        }

        ClickElement(button);
        TryWaitForWindowClosed(dialog);
    }

    private static void PressVirtualKey(AutomationElement element, byte virtualKey)
    {
        BringElementWindowToForeground(element);
        Thread.Sleep(100);
        keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
        keybd_event(virtualKey, 0, KeyeventfKeyUp, UIntPtr.Zero);
        Thread.Sleep(150);
    }

    private static void TryWaitForWindowClosed(AutomationElement window)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < 3000)
        {
            try
            {
                if (window.Current.NativeWindowHandle == 0 || window.Current.IsOffscreen)
                {
                    return;
                }
            }
            catch (ElementNotAvailableException)
            {
                return;
            }

            Thread.Sleep(PollDelayMilliseconds);
        }
    }

    private void WaitForText(string automationId, Func<string, bool> predicate)
    {
        var lastText = string.Empty;

        WaitFor(
            () =>
            {
                var mainWindow = RequireMainWindow();
                var text = ReadElementText(mainWindow, automationId);
                lastText = text;
                return predicate(text) ? true : (bool?)null;
            },
            $"Timed out waiting for text in '{automationId}'. Last value: '{lastText}'.");
    }

    private void WaitForLatestIdeLogText(Func<string, bool> predicate)
    {
        var lastText = string.Empty;

        WaitFor(
            () =>
            {
                lastText = ReadLatestIdeLogText();
                return predicate(lastText) ? true : (bool?)null;
            },
            "Timed out waiting for expected IDE log text.");
    }

    private string ReadLatestIdeLogText()
    {
        var logsDirectory = Path.Combine(Path.GetDirectoryName(_ideExecutablePath) ?? _repositoryRoot, "Logs");
        if (!Directory.Exists(logsDirectory))
        {
            return string.Empty;
        }

        var latestLog = Directory.EnumerateFiles(logsDirectory, "*.log")
            .Select(static path => new FileInfo(path))
            .OrderByDescending(static file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestLog is null)
        {
            return string.Empty;
        }

        try
        {
            return File.ReadAllText(latestLog.FullName, Encoding.UTF8);
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private void WaitForElementText(
        AutomationElement root,
        string automationId,
        Func<string, bool> predicate,
        string failureMessage)
    {
        var lastText = string.Empty;

        WaitFor(
            () =>
            {
                lastText = ReadElementText(root, automationId);
                return predicate(lastText) ? true : (bool?)null;
            },
            $"{failureMessage} Last value: '{lastText}'.");
    }

    private void WaitForStatusText(Func<string, bool> predicate)
    {
        WaitForText("StatusBarTextBlock", predicate);
    }

    private void WaitForWindowTitle(AutomationElement window, Func<string, bool> predicate)
    {
        WaitFor(
            () =>
            {
                var title = window.Current.Name;
                return predicate(title) ? true : (bool?)null;
            },
            "Timed out waiting for window title update.");
    }

    private void AssertWindowTitleContains(AutomationElement window, string fragment)
    {
        WaitForWindowTitle(window, title => title.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private void WaitForDataGridRows(AutomationElement mainWindow, string automationId, int minimumRowCount)
    {
        WaitFor(
            () =>
            {
                var grid = RequireElement(mainWindow, automationId);
                var rows = grid.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem));
                return rows.Count >= minimumRowCount ? true : (bool?)null;
            },
            $"Timed out waiting for rows in '{automationId}'.");
    }

    private void AssertHidden(AutomationElement root, string automationId)
    {
        WaitFor(
            () =>
            {
                var element = FindElement(root, automationId);
                return element is null || element.Current.IsOffscreen ? true : (bool?)null;
            },
            $"Element '{automationId}' is still visible.");
    }

    private void AssertWindowState(AutomationElement window, WindowVisualState expectedState)
    {
        if (!window.TryGetCurrentPattern(WindowPattern.Pattern, out var windowPattern))
        {
            throw new InvalidOperationException("Main window does not support WindowPattern.");
        }

        var actualState = ((WindowPattern)windowPattern).Current.WindowVisualState;

        if (actualState != expectedState)
        {
            throw new InvalidOperationException($"Expected window state {expectedState}, got {actualState}.");
        }
    }

    private void SetWindowVisualState(AutomationElement window, WindowVisualState targetState)
    {
        if (!window.TryGetCurrentPattern(WindowPattern.Pattern, out var windowPattern))
        {
            throw new InvalidOperationException("Main window does not support WindowPattern.");
        }

        ((WindowPattern)windowPattern).SetWindowVisualState(targetState);
        AssertWindowState(window, targetState);
    }

    private void CloseWindow(AutomationElement window, bool expectProcessExit = false)
    {
        var handle = new IntPtr(window.Current.NativeWindowHandle);

        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Window does not expose a native handle.");
        }

        PostMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero);

        if (expectProcessExit)
        {
            WaitFor(
                () =>
                {
                    var refreshedProcess = _ideProcess;
                    if (refreshedProcess is null)
                    {
                        return true;
                    }

                    refreshedProcess.Refresh();
                    return refreshedProcess.HasExited ? true : (bool?)null;
                },
                "Timed out waiting for IDE process to exit after close.");
            return;
        }

        var automationId = window.Current.AutomationId;

        WaitFor(
            () =>
            {
                if (!string.IsNullOrWhiteSpace(automationId))
                {
                    return FindElementForProcess(automationId) is null ? true : (bool?)null;
                }

                try
                {
                    return window.Current.NativeWindowHandle == 0 ? true : (bool?)null;
                }
                catch (ElementNotAvailableException)
                {
                    return true;
                }
            },
            "Timed out waiting for child window to close.");
    }

    private void SendSmokeCommand(AutomationElement mainWindow, uint message, string description)
    {
        var handle = new IntPtr(mainWindow.Current.NativeWindowHandle);

        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Main window does not expose a native handle.");
        }

        if (!PostMessage(handle, message, IntPtr.Zero, IntPtr.Zero))
        {
            throw new InvalidOperationException($"Could not {description}.");
        }

        Thread.Sleep(150);
    }

    private string ReadElementText(AutomationElement root, string automationId)
    {
        var element = RequireElement(root, automationId);

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            var value = ((ValuePattern)valuePattern).Current.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var textPattern))
        {
            var text = ((TextPattern)textPattern).DocumentRange.GetText(-1);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return element.Current.Name;
    }

    private AutomationElement RequireElement(AutomationElement root, string automationId)
    {
        return WaitFor(
            () => FindElement(root, automationId),
            $"Could not find automation element '{automationId}'.");
    }

    private static AutomationElement? FindElement(AutomationElement root, string automationId)
    {
        return root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
    }

    private AutomationElement? FindElementForProcess(string automationId)
    {
        return AutomationElement.RootElement.FindFirst(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, GetProcessId()),
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)));
    }

    private void ClickElement(AutomationElement root, string automationId)
    {
        var element = RequireElement(root, automationId);
        ClickElement(element);
    }

    private static void ClickElement(AutomationElement element)
    {
        ClickElement(element, MouseeventfLeftDown, MouseeventfLeftUp);
    }

    private static void RightClickElement(AutomationElement element)
    {
        ClickElement(element, MouseeventfRightDown, MouseeventfRightUp);
    }

    private static void ClickElement(AutomationElement element, uint mouseDownFlag, uint mouseUpFlag)
    {
        var bounds = element.Current.BoundingRectangle;
        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException($"Element '{element.Current.AutomationId}' does not expose clickable bounds.");
        }

        BringElementWindowToForeground(element);

        var x = (int)Math.Round(bounds.Left + (bounds.Width / 2));
        var y = (int)Math.Round(bounds.Top + (bounds.Height / 2));

        SetCursorPos(x, y);
        Thread.Sleep(100);
        mouse_event(mouseDownFlag, 0, 0, 0, UIntPtr.Zero);
        mouse_event(mouseUpFlag, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(150);
    }

    private static void BringElementWindowToForeground(AutomationElement element)
    {
        var current = element;

        while (current is not null)
        {
            var handle = new IntPtr(current.Current.NativeWindowHandle);
            if (handle != IntPtr.Zero)
            {
                SetForegroundWindow(handle);
                Thread.Sleep(100);
                return;
            }

            current = TreeWalker.RawViewWalker.GetParent(current);
        }
    }

    private AutomationElement RequireMainWindow() => WaitForMainWindow();

    private void PrepareWorkspace()
    {
        Directory.CreateDirectory(_workspaceRootPath);

        File.WriteAllText(
            _configFilePath,
            "{\n  \"name\": \"demo\",\n  \"mode\": \"smoke\"\n}\n",
            Encoding.UTF8);

        File.WriteAllText(
            _styleFilePath,
            ".demo {\n  color: red;\n}\n",
            Encoding.UTF8);

        File.WriteAllBytes(_blockedExecutablePath, [0x4D, 0x5A]);

        File.WriteAllText(
            _hiddenFilePath,
            "hidden",
            Encoding.UTF8);
        File.SetAttributes(_hiddenFilePath, File.GetAttributes(_hiddenFilePath) | FileAttributes.Hidden);

        File.WriteAllText(
            _successProgramPath,
            "PRINT 42" + Environment.NewLine,
            Encoding.UTF8);

        File.WriteAllText(
            _syntaxErrorProgramPath,
            "PRINT LEN(" + Environment.NewLine,
            Encoding.UTF8);

        File.WriteAllText(
            _inKeyProgramPath,
            """
            PRINT "Click the output pane and press a key while the loop runs."

            DIM keyName AS STRING

            FOR i = 1 TO 200000
                keyName = INKEY()
                IF keyName <> "" THEN
                    PRINT keyName
                    EXIT FOR
                END IF
            NEXT

            IF keyName = "" THEN
                PRINT "NO KEY"
            END IF
            """ + Environment.NewLine,
            Encoding.UTF8);

        File.WriteAllText(
            _onMouseProgramPath,
            """
            ON MOUSE(1), Pressed
            ON MOUSE(1, "UP"), Released
            ON MOUSE("WHEEL"), Wheeled

            PRINT "Click inside Output, roll the mouse wheel, then press ESC."

            DO WHILE NOT KEYPRESS("ESC")
                DIM idle AS INTEGER = 1
            LOOP

            PRINT "DONE"

            SUB Pressed(button AS INTEGER, x AS LONG, y AS LONG)
                PRINT "PRESS"
            END SUB

            SUB Released(button AS INTEGER, x AS LONG, y AS LONG)
                PRINT "UP"
            END SUB

            SUB Wheeled(delta AS INTEGER, x AS LONG, y AS LONG)
                PRINT "WHEEL"
            END SUB
            """ + Environment.NewLine,
            Encoding.UTF8);
    }

    private void BackupStateFile()
    {
        _stateFileExisted = File.Exists(_stateFilePath);
        _originalStateBytes = _stateFileExisted
            ? File.ReadAllBytes(_stateFilePath)
            : null;
    }

    private void RestoreStateFile()
    {
        var stateDirectoryPath = Path.GetDirectoryName(_stateFilePath);

        if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
        {
            Directory.CreateDirectory(stateDirectoryPath);
        }

        if (_stateFileExisted && _originalStateBytes is not null)
        {
            File.WriteAllBytes(_stateFilePath, _originalStateBytes);
            return;
        }

        if (File.Exists(_stateFilePath))
        {
            File.Delete(_stateFilePath);
        }
    }

    private void WriteShellState(bool isFilesPaneVisible, bool isStatusBarVisible, string windowState)
    {
        var stateDirectoryPath = Path.GetDirectoryName(_stateFilePath);

        if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
        {
            Directory.CreateDirectory(stateDirectoryPath);
        }

        var state = new
        {
            RecentFiles = new[]
            {
                _successProgramPath,
                _inKeyProgramPath,
                _configFilePath,
                _styleFilePath,
                _syntaxErrorProgramPath
            },
            LastDirectoryPath = _workspaceRootPath,
            IsFilesPaneVisible = isFilesPaneVisible,
            IsStatusBarVisible = isStatusBarVisible,
            WindowState = windowState,
            Theme = "Light",
            Language = "Russian",
            SmartIdentifierEnabled = false,
            PreferredExecutionMode = "Bytecode"
        };

        File.WriteAllText(
            _stateFilePath,
            JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
    }

    private void CleanupProcess()
    {
        if (_ideProcess is null)
        {
            return;
        }

        try
        {
            _ideProcess.Refresh();

            if (!_ideProcess.HasExited)
            {
                _ideProcess.Kill(entireProcessTree: true);
                _ideProcess.WaitForExit(10000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            _ideProcess.Dispose();
            _ideProcess = null;
        }
    }

    private int GetProcessId()
    {
        if (_ideProcess is null)
        {
            throw new InvalidOperationException("IDE process has not been started.");
        }

        _ideProcess.Refresh();
        return _ideProcess.Id;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Axiom.sln.");
    }

    private static string ResolveIdeExecutablePath(string repositoryRoot, string? overrideExecutablePath)
    {
        if (!string.IsNullOrWhiteSpace(overrideExecutablePath))
        {
            var explicitPath = Path.GetFullPath(overrideExecutablePath);

            if (!File.Exists(explicitPath))
            {
                throw new FileNotFoundException(
                    "Explicit Axiom.Ide executable path was not found.",
                    explicitPath);
            }

            return explicitPath;
        }

        var executablePath = Path.Combine(
            repositoryRoot,
            ".artifacts",
            "bin",
            "Axiom.Ide",
            "Debug",
            "net10.0-windows",
            "Axiom.Ide.exe");

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                "Built Axiom.Ide executable was not found. Run `dotnet build Axiom.sln` first.",
                executablePath);
        }

        return executablePath;
    }

    private static T WaitFor<T>(Func<T?> factory, string failureMessage)
        where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < TimeoutMilliseconds)
        {
            var result = factory();

            if (result is not null)
            {
                return result;
            }

            Thread.Sleep(PollDelayMilliseconds);
        }

        throw new TimeoutException(failureMessage);
    }

    private static bool WaitFor(Func<bool?> probe, string failureMessage)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < TimeoutMilliseconds)
        {
            var result = probe();

            if (result == true)
            {
                return true;
            }

            Thread.Sleep(PollDelayMilliseconds);
        }

        throw new TimeoutException(failureMessage);
    }

    private static void ReportStep(string title)
    {
        Console.Write($"[STEP] {title} ... ");
    }

    private static void ReportStepCompleted()
    {
        Console.WriteLine("OK");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
