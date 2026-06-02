using System.Windows.Input;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Tests;

public sealed class ProgramKeyboardInputRouterTests
{
    [Fact]
    public void HandlePreviewKeyDown_EnqueuesSpecialKeyWithControlModifier()
    {
        var sink = new TestProgramInputSink();

        var handled = ProgramKeyboardInputRouter.HandlePreviewKeyDown(sink, Key.Left, ModifierKeys.Control);

        Assert.True(handled);
        Assert.Equal(["LEFT"], sink.Keys);
        Assert.Contains("LEFT", sink.DownKeys);
    }

    [Fact]
    public void HandlePreviewKeyDown_EnqueuesPrintableKeyWithControlModifier()
    {
        var sink = new TestProgramInputSink();

        var handled = ProgramKeyboardInputRouter.HandlePreviewKeyDown(sink, Key.A, ModifierKeys.Control);

        Assert.True(handled);
        Assert.Equal(["A"], sink.Keys);
        Assert.Contains("A", sink.DownKeys);
    }

    [Fact]
    public void HandlePreviewKeyDown_LeavesPlainPrintableKeyForTextInput()
    {
        var sink = new TestProgramInputSink();

        var handled = ProgramKeyboardInputRouter.HandlePreviewKeyDown(sink, Key.A, ModifierKeys.None);

        Assert.False(handled);
        Assert.Empty(sink.Keys);
        Assert.Contains("A", sink.DownKeys);
    }

    [Fact]
    public void HandlePreviewTextInput_EnqueuesPlainTextAndIgnoresCommandText()
    {
        var sink = new TestProgramInputSink();

        Assert.True(ProgramKeyboardInputRouter.HandlePreviewTextInput(sink, "A", ModifierKeys.Shift));
        Assert.False(ProgramKeyboardInputRouter.HandlePreviewTextInput(sink, "A", ModifierKeys.Control));

        Assert.Equal(["A"], sink.Keys);
    }

    [Fact]
    public void HandlePreviewKeyUp_ReleasesMappedStateKey()
    {
        var sink = new TestProgramInputSink();
        sink.SetKeyDown("CTRL");

        ProgramKeyboardInputRouter.HandlePreviewKeyUp(sink, Key.LeftCtrl);

        Assert.DoesNotContain("CTRL", sink.DownKeys);
    }

    private sealed class TestProgramInputSink : IProgramConsoleService
    {
        private readonly Queue<string> _keys = [];

        public List<string> Keys { get; } = [];

        public HashSet<string> DownKeys { get; } = new(StringComparer.Ordinal);

        public int OutputCheckpoint => 0;

        public string? ResourceBaseDirectory { get; set; }

        public void BeginSession()
        {
        }

        public void EndSession()
        {
        }

        public void Clear()
        {
        }

        public void ClearOutput()
        {
        }

        public void ClearKeyState() => DownKeys.Clear();

        public void ClearMouseState()
        {
        }

        public void EnqueueKey(string keyText)
        {
            Keys.Add(keyText);
            _keys.Enqueue(keyText);
        }

        public bool IsKeyDown(string keyText) => DownKeys.Contains(KeyboardInputNormalizer.Normalize(keyText));

        public long GetMouseX() => -1;

        public long GetMouseY() => -1;

        public bool IsMouseButtonDown(int buttonNumber) => false;

        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent() => default;

        public string ReadKey() => _keys.Count > 0 ? _keys.Dequeue() : string.Empty;

        public void RestoreOutputCheckpoint(int checkpoint)
        {
        }

        public string RequestInput(ProgramInputRequest request) => string.Empty;

        public void SetKeyDown(string keyText) => DownKeys.Add(KeyboardInputNormalizer.Normalize(keyText));

        public void SetKeyUp(string keyText) => DownKeys.Remove(KeyboardInputNormalizer.Normalize(keyText));

        public void SetMousePosition(long x, long y)
        {
        }

        public void SetMouseButtonDown(int buttonNumber)
        {
        }

        public void SetMouseButtonUp(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonPress(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonRelease(int buttonNumber)
        {
        }

        public void EnqueueMouseMove()
        {
        }

        public void EnqueueMouseWheel(int delta)
        {
        }

        public void WriteOutput(string text)
        {
        }
    }
}
