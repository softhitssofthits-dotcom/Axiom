using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide;

public partial class MainWindow
{
    private bool _preserveOutputSelectionOnRightClick;
    private int _outputSelectionStart;
    private int _outputSelectionLength;

    private void OnOutputTextBoxPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsOutputTextBoxMouseEvent(sender, e) &&
            _consoleService is not null &&
            _viewModel?.StatusBar.IsRunning == true)
        {
            ProgramMouseInputRouter.HandlePreviewMouseDown(_consoleService, e.GetPosition(OutputTextBox), e.ChangedButton);
        }

        if (!ReferenceEquals(sender, OutputTextBox) || OutputTextBox.SelectionLength <= 0)
        {
            _preserveOutputSelectionOnRightClick = false;
            _outputSelectionStart = 0;
            _outputSelectionLength = 0;
            return;
        }

        _preserveOutputSelectionOnRightClick = true;
        _outputSelectionStart = OutputTextBox.SelectionStart;
        _outputSelectionLength = OutputTextBox.SelectionLength;
    }

    private void OnOutputTextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsOutputTextBoxMouseEvent(sender, e) &&
            _consoleService is not null &&
            _viewModel?.StatusBar.IsRunning == true)
        {
            ProgramMouseInputRouter.HandlePreviewMouseUp(_consoleService, e.GetPosition(OutputTextBox), e.ChangedButton);
        }

        if (!ReferenceEquals(sender, OutputTextBox) ||
            !_preserveOutputSelectionOnRightClick ||
            _outputSelectionLength <= 0)
        {
            return;
        }

        OutputTextBox.Select(_outputSelectionStart, _outputSelectionLength);
    }

    private void OnOutputTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!ReferenceEquals(sender, OutputTextBox) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        e.Handled = ProgramKeyboardInputRouter.HandlePreviewKeyDown(_consoleService, key, Keyboard.Modifiers);
    }

    private void OnOutputTextBoxPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (!ReferenceEquals(sender, OutputTextBox) ||
            _consoleService is null)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        ProgramKeyboardInputRouter.HandlePreviewKeyUp(_consoleService, key);
    }

    private void OnOutputTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (ReferenceEquals(sender, OutputTextBox))
        {
            _consoleService?.ClearKeyState();
        }
    }

    private void OnOutputTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!ReferenceEquals(sender, OutputTextBox) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true ||
            string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        e.Handled = ProgramKeyboardInputRouter.HandlePreviewTextInput(_consoleService, e.Text, Keyboard.Modifiers);
    }

    private void OnOutputTextBoxPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!IsOutputTextBoxMouseEvent(sender, e) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true)
        {
            return;
        }

        ProgramMouseInputRouter.HandlePreviewMouseMove(_consoleService, e.GetPosition(OutputTextBox));
    }

    private void OnOutputTextBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsOutputTextBoxMouseEvent(sender, e) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true ||
            e.ChangedButton == MouseButton.Right)
        {
            return;
        }

        ProgramMouseInputRouter.HandlePreviewMouseDown(_consoleService, e.GetPosition(OutputTextBox), e.ChangedButton);
    }

    private void OnOutputTextBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsOutputTextBoxMouseEvent(sender, e) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true ||
            e.ChangedButton == MouseButton.Right)
        {
            return;
        }

        ProgramMouseInputRouter.HandlePreviewMouseUp(_consoleService, e.GetPosition(OutputTextBox), e.ChangedButton);
    }

    private void OnOutputTextBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsOutputTextBoxMouseEvent(sender, e) ||
            _consoleService is null ||
            _viewModel?.StatusBar.IsRunning != true)
        {
            return;
        }

        ProgramMouseInputRouter.HandlePreviewMouseWheel(_consoleService, e.GetPosition(OutputTextBox), e.Delta);
    }

    private void OnOutputTextBoxMouseLeave(object sender, MouseEventArgs e)
    {
        if (!IsOutputTextBoxMouseEvent(sender, e) || _consoleService is null)
        {
            return;
        }

        ProgramMouseInputRouter.HandleMouseLeave(_consoleService);
    }

    private bool IsOutputTextBoxMouseEvent(object sender, MouseEventArgs e)
    {
        if (ReferenceEquals(sender, OutputTextBox) || OutputTextBox.IsMouseOver)
        {
            return true;
        }

        var position = e.GetPosition(OutputTextBox);
        return position.X >= 0
            && position.Y >= 0
            && position.X <= OutputTextBox.ActualWidth
            && position.Y <= OutputTextBox.ActualHeight;
    }

    private void OnOutputTextBoxContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (_preserveOutputSelectionOnRightClick && _outputSelectionLength > 0)
        {
            OutputTextBox.Focus();
            OutputTextBox.Select(_outputSelectionStart, _outputSelectionLength);
        }

        _preserveOutputSelectionOnRightClick = false;
        _outputSelectionStart = 0;
        _outputSelectionLength = 0;
        UpdateOutputContextMenuState();
    }

    private void OnOutputCopyMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (OutputTextBox.SelectionLength <= 0)
        {
            return;
        }

        CopyTextToClipboard(OutputTextBox.SelectedText);
        e.Handled = true;
    }

    private void OnOutputClearMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (_consoleService is not null)
        {
            _consoleService.ClearOutput();
            if (_bottomPane is not null)
            {
                _bottomPane.OutputText = _consoleService.OutputText;
            }
        }
        else
        {
            _bottomPane?.ClearOutput();
        }

        OutputTextBox.Select(0, 0);
        UpdateOutputContextMenuState();
        e.Handled = true;
    }

    private void OnOutputSelectAllMenuItemClick(object sender, RoutedEventArgs e)
    {
        OutputTextBox.Focus();
        OutputTextBox.SelectAll();
        UpdateOutputContextMenuState();
        e.Handled = true;
    }

    private void UpdateOutputContextMenuState()
    {
        OutputCopyMenuItem.IsEnabled = OutputTextBox.SelectionLength > 0;
        OutputClearMenuItem.IsEnabled = !string.IsNullOrEmpty(OutputTextBox.Text);
    }
}
