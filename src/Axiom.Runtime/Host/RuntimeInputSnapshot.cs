namespace Axiom.Runtime;

public sealed class RuntimeInputSnapshot
{
    private readonly object _gate = new();
    private readonly Queue<string> _pendingKeys = [];
    private readonly Queue<MouseInputEvent> _pendingMouseEvents = [];
    private readonly HashSet<string> _pressedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<int> _pressedMouseButtons = [];
    private long _mouseX = -1;
    private long _mouseY = -1;
    private bool _isScreenActive;
    private bool _isScreenOutputActive;
    private int _screenMode;
    private int _screenWidth;
    private int _screenHeight;

    public bool IsScreenActive { get { lock (_gate) { return _isScreenActive; } } }

    public bool IsScreenOutputActive { get { lock (_gate) { return _isScreenOutputActive; } } }

    public int ScreenMode { get { lock (_gate) { return _screenMode; } } }

    public int ScreenWidth { get { lock (_gate) { return _screenWidth; } } }

    public int ScreenHeight { get { lock (_gate) { return _screenHeight; } } }

    public int PendingKeyCount { get { lock (_gate) { return _pendingKeys.Count; } } }

    public int PendingMouseEventCount { get { lock (_gate) { return _pendingMouseEvents.Count; } } }

    public void Reset()
    {
        lock (_gate)
        {
            _pendingKeys.Clear();
            _pendingMouseEvents.Clear();
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
            _mouseX = -1;
            _mouseY = -1;
            _isScreenActive = false;
            _isScreenOutputActive = false;
            _screenMode = 0;
            _screenWidth = 0;
            _screenHeight = 0;
        }
    }

    public void EnqueueKey(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        lock (_gate)
        {
            _pendingKeys.Enqueue(keyText);
        }
    }

    public bool TryReadKey(out string keyText)
    {
        lock (_gate)
        {
            if (_pendingKeys.Count > 0)
            {
                keyText = _pendingKeys.Dequeue();
                return true;
            }
        }

        keyText = string.Empty;
        return false;
    }

    public void SetKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        lock (_gate)
        {
            _pressedKeys.Add(KeyboardInputNormalizer.Normalize(keyText));
        }
    }

    public void SetKeyUp(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        lock (_gate)
        {
            _pressedKeys.Remove(KeyboardInputNormalizer.Normalize(keyText));
        }
    }

    public bool IsKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return false;
        }

        var normalizedKey = KeyboardInputNormalizer.Normalize(keyText);
        lock (_gate)
        {
            return _pressedKeys.Contains(normalizedKey);
        }
    }

    public void ClearKeyState()
    {
        lock (_gate)
        {
            _pressedKeys.Clear();
        }
    }

    public void SetMousePosition(long x, long y)
    {
        lock (_gate)
        {
            _mouseX = x;
            _mouseY = y;
        }
    }

    public long GetMouseX()
    {
        lock (_gate)
        {
            return _mouseX;
        }
    }

    public long GetMouseY()
    {
        lock (_gate)
        {
            return _mouseY;
        }
    }

    public void SetMouseButtonDown(int buttonNumber)
    {
        lock (_gate)
        {
            _pressedMouseButtons.Add(buttonNumber);
        }
    }

    public void SetMouseButtonUp(int buttonNumber)
    {
        lock (_gate)
        {
            _pressedMouseButtons.Remove(buttonNumber);
        }
    }

    public bool IsMouseButtonDown(int buttonNumber)
    {
        lock (_gate)
        {
            return _pressedMouseButtons.Contains(buttonNumber);
        }
    }

    public void EnqueueMouseEvent(MouseInputEvent mouseEvent)
    {
        if (mouseEvent == default)
        {
            return;
        }

        lock (_gate)
        {
            if (mouseEvent.Kind == MouseInputEventKind.Move)
            {
                CoalescePendingMouseMoveEvents();
            }

            _pendingMouseEvents.Enqueue(mouseEvent);
        }
    }

    public bool TryReadMouseEvent(out MouseInputEvent mouseEvent)
    {
        lock (_gate)
        {
            if (_pendingMouseEvents.Count > 0)
            {
                mouseEvent = _pendingMouseEvents.Dequeue();
                return true;
            }
        }

        mouseEvent = default;
        return false;
    }

    public int ReadMouseButtonPress()
    {
        lock (_gate)
        {
            var buttonNumber = 0;
            var deferredEvents = new List<MouseInputEvent>();

            while (_pendingMouseEvents.Count > 0)
            {
                var candidate = _pendingMouseEvents.Dequeue();
                if (buttonNumber == 0 && candidate.Kind == MouseInputEventKind.Press)
                {
                    buttonNumber = candidate.ButtonNumber;
                    continue;
                }

                deferredEvents.Add(candidate);
            }

            foreach (var deferredEvent in deferredEvents)
            {
                _pendingMouseEvents.Enqueue(deferredEvent);
            }

            return buttonNumber;
        }
    }

    public void ClearMouseState()
    {
        lock (_gate)
        {
            _mouseX = -1;
            _mouseY = -1;
            _pendingMouseEvents.Clear();
            _pressedMouseButtons.Clear();
        }
    }

    public void SetScreenState(bool isScreenActive, bool isScreenOutputActive, int mode, int width, int height)
    {
        lock (_gate)
        {
            _isScreenActive = isScreenActive;
            _isScreenOutputActive = isScreenOutputActive;
            _screenMode = mode;
            _screenWidth = width;
            _screenHeight = height;
        }
    }

    private void CoalescePendingMouseMoveEvents()
    {
        if (_pendingMouseEvents.Count == 0)
        {
            return;
        }

        var eventCount = _pendingMouseEvents.Count;
        for (var i = 0; i < eventCount; i++)
        {
            var pendingEvent = _pendingMouseEvents.Dequeue();
            if (pendingEvent.Kind != MouseInputEventKind.Move)
            {
                _pendingMouseEvents.Enqueue(pendingEvent);
            }
        }
    }
}
