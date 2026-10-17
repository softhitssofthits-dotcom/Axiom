namespace Axiom.Runtime;

public sealed class RuntimeInputSnapshot
{
    // Предел защищает от роста памяти, когда программа долго не читает события.
    // Сначала вытесняются схлопываемые перемещения и колесо; переходы кнопок
    // сохраняются, пока очередь состоит только из них.
    private const int MaximumPendingMouseEvents = 8192;

    private readonly object _gate = new();
    private readonly Queue<string> _pendingKeys = [];
    private readonly LinkedList<MouseInputEvent> _pendingMouseEvents = [];
    private readonly HashSet<string> _pressedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<int> _pressedMouseButtons = [];
    private long _mouseX = -1;
    private long _mouseY = -1;
    private long _mouseClearGeneration;
    private long _droppedMouseEvents;
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

    // Поколение меняется при каждой очистке. VM сравнивает его и сбрасывает
    // собственный буфер отложенных событий, который живёт вне этого снимка.
    public long MouseClearGeneration { get { lock (_gate) { return _mouseClearGeneration; } } }

    public long DroppedMouseEventCount { get { lock (_gate) { return _droppedMouseEvents; } } }

    public void Reset()
    {
        lock (_gate)
        {
            _pendingKeys.Clear();
            _pendingMouseEvents.Clear();
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
            _mouseClearGeneration++;
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

    public void GetMousePosition(out long x, out long y)
    {
        lock (_gate)
        {
            x = _mouseX;
            y = _mouseY;
        }
    }

    // Позиция, состояние кнопки и событие публикуются одной блокировкой.
    // Обработчик не может увидеть кнопку в состоянии соседнего события.
    public void PublishMouseMove(long x, long y)
    {
        lock (_gate)
        {
            _mouseX = x;
            _mouseY = y;
            AppendMouseEventLocked(new MouseInputEvent(MouseInputEventKind.Move, 0));
        }
    }

    public void PublishMouseButtonPress(int buttonNumber, long x, long y)
    {
        lock (_gate)
        {
            _mouseX = x;
            _mouseY = y;
            _pressedMouseButtons.Add(buttonNumber);
            AppendMouseEventLocked(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber));
        }
    }

    public void PublishMouseButtonRelease(int buttonNumber, long x, long y)
    {
        lock (_gate)
        {
            _mouseX = x;
            _mouseY = y;
            _pressedMouseButtons.Remove(buttonNumber);
            AppendMouseEventLocked(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
        }
    }

    // Синтетическое отпускание при потере фокуса сохраняет последнюю
    // известную позицию: указатель не перемещался, меняется только кнопка.
    public void PublishMouseButtonReleaseKeepPosition(int buttonNumber)
    {
        lock (_gate)
        {
            _pressedMouseButtons.Remove(buttonNumber);
            AppendMouseEventLocked(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
        }
    }

    public void PublishMouseWheel(int delta, long x, long y)
    {
        if (delta == 0)
        {
            return;
        }

        lock (_gate)
        {
            _mouseX = x;
            _mouseY = y;
            AppendMouseEventLocked(new MouseInputEvent(MouseInputEventKind.Wheel, delta));
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
            AppendMouseEventLocked(mouseEvent);
        }
    }

    public bool TryReadMouseEvent(out MouseInputEvent mouseEvent)
    {
        lock (_gate)
        {
            if (_pendingMouseEvents.First is { } first)
            {
                mouseEvent = first.Value;
                _pendingMouseEvents.RemoveFirst();
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
            var node = _pendingMouseEvents.First;
            while (node is not null)
            {
                var next = node.Next;
                if (buttonNumber == 0 && node.Value.Kind == MouseInputEventKind.Press)
                {
                    buttonNumber = node.Value.ButtonNumber;
                    _pendingMouseEvents.Remove(node);
                }

                node = next;
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
            _mouseClearGeneration++;
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

    private void AppendMouseEventLocked(MouseInputEvent mouseEvent)
    {
        if (mouseEvent.Kind == MouseInputEventKind.Move)
        {
            // Соседние перемещения схлопываются: для позиции важно последнее.
            // Порядок относительно нажатий и отпусканий сохраняется.
            if (_pendingMouseEvents.Last is { } lastMove && lastMove.Value.Kind == MouseInputEventKind.Move)
            {
                lastMove.Value = mouseEvent;
                return;
            }
        }
        else if (mouseEvent.Kind == MouseInputEventKind.Wheel)
        {
            // Соседние ступени колеса складываются, чтобы быстрый трекпад
            // не создавал тысячи отдельных событий.
            if (_pendingMouseEvents.Last is { } lastWheel && lastWheel.Value.Kind == MouseInputEventKind.Wheel)
            {
                var combined = (long)lastWheel.Value.ButtonNumber + mouseEvent.ButtonNumber;
                if (combined == 0)
                {
                    _pendingMouseEvents.RemoveLast();
                    return;
                }

                lastWheel.Value = lastWheel.Value with
                {
                    ButtonNumber = (int)Math.Clamp(combined, int.MinValue, int.MaxValue)
                };
                return;
            }
        }

        if (_pendingMouseEvents.Count >= MaximumPendingMouseEvents)
        {
            if (!TryRemoveOldestCoalescibleEventLocked())
            {
                _pendingMouseEvents.RemoveFirst();
            }

            _droppedMouseEvents++;
        }

        _pendingMouseEvents.AddLast(mouseEvent);
    }

    private bool TryRemoveOldestCoalescibleEventLocked()
    {
        var node = _pendingMouseEvents.First;
        while (node is not null)
        {
            if (node.Value.Kind is MouseInputEventKind.Move or MouseInputEventKind.Wheel)
            {
                _pendingMouseEvents.Remove(node);
                return true;
            }

            node = node.Next;
        }

        return false;
    }
}
