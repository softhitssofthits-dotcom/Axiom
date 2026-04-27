using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Data;
using ICSharpCode.AvalonEdit;
using Axiom.Ide.Editor;

namespace Axiom.Ide.Tests;

public sealed class AvalonEditTextBindingTests
{
    [Fact]
    public void EnsureAttached_UpdatesBoundSourceForInitiallyEmptyDocument()
    {
        Exception? capturedException = null;
        string? capturedText = null;

        var thread = new Thread(() =>
        {
            try
            {
                var state = new BindingState();
                var editor = new TextEditor();

                BindingOperations.SetBinding(
                    editor,
                    AvalonEditTextBinding.TextProperty,
                    new Binding(nameof(BindingState.Text))
                    {
                        Source = state,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });

                AvalonEditTextBinding.EnsureAttached(editor);
                editor.Text = "PRINT LEN(\"x\") + 1";
                capturedText = state.Text;
            }
            catch (Exception exception)
            {
                capturedException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (capturedException is not null)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }

        Assert.Equal("PRINT LEN(\"x\") + 1", capturedText);
    }

    [Fact]
    public void EnsureAttached_DoesNotMarkPreloadedDocumentDirtyDuringInitialSync()
    {
        Exception? capturedException = null;
        string? capturedText = null;
        var originalText = "PRINT 1\r\nPRINT 2";
        var dirtyAfterBinding = false;

        var thread = new Thread(() =>
        {
            try
            {
                var document = new Documents.DocumentTabViewModel(@"C:\temp\sample.axm", originalText);
                var editor = new TextEditor();

                BindingOperations.SetBinding(
                    editor,
                    AvalonEditTextBinding.TextProperty,
                    new Binding(nameof(Documents.DocumentTabViewModel.SourceText))
                    {
                        Source = document,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });

                AvalonEditTextBinding.EnsureAttached(editor);
                capturedText = document.SourceText;
                dirtyAfterBinding = document.IsDirty;
            }
            catch (Exception exception)
            {
                capturedException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (capturedException is not null)
        {
            ExceptionDispatchInfo.Capture(capturedException).Throw();
        }

        Assert.Equal(originalText, capturedText);
        Assert.False(dirtyAfterBinding);
    }

    private sealed class BindingState : ViewModels.ViewModelBase
    {
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
    }
}
