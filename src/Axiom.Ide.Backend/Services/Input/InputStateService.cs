using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

/// <summary>
/// Управляет состоянием ввода: клавиатура, мышь, очередь событий.
/// Выделен из ProgramConsoleService для разделения ответственности.
/// </summary>
public sealed class InputStateService
{
    private readonly RuntimeInputSnapshot _inputSnapshot;

    public InputStateService()
    {
        _inputSnapshot = new RuntimeInputSnapshot();
    }

    /// <summary>
    /// Снимок ввода — доступен для чтения флагов (IsScreenActive и т.д.).
    /// </summary>
    public RuntimeInputSnapshot Snapshot => _inputSnapshot;

    #region Клавиатурный ввод

    public void EnqueueKey(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.EnqueueKey(keyText);
    }

    public void SetKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.SetKeyDown(keyText);
    }

    public void SetKeyUp(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return;
        }

        _inputSnapshot.SetKeyUp(keyText);
    }

    public string ReadKey()
    {
        if (_inputSnapshot.TryReadKey(out var keyText))
        {
            return keyText;
        }

        return string.Empty;
    }

    public bool IsKeyDown(string keyText)
    {
        if (string.IsNullOrEmpty(keyText))
        {
            return false;
        }

        return _inputSnapshot.IsKeyDown(keyText);
    }

    public void ClearKeyState()
    {
        _inputSnapshot.ClearKeyState();
    }

    #endregion

    #region Мышечный ввод

    public void SetMousePosition(long x, long y)
    {
        _inputSnapshot.SetMousePosition(x, y);
    }

    public void SetMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.SetMouseButtonDown(buttonNumber);
    }

    public void SetMouseButtonUp(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.SetMouseButtonUp(buttonNumber);
    }

    public void EnqueueMouseButtonPress(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.EnqueueMouseEvent(new MouseInputEvent(MouseInputEventKind.Press, buttonNumber));
    }

    public void EnqueueMouseButtonRelease(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.EnqueueMouseEvent(new MouseInputEvent(MouseInputEventKind.Release, buttonNumber));
    }

    public void EnqueueMouseMove()
    {
        _inputSnapshot.EnqueueMouseEvent(new MouseInputEvent(MouseInputEventKind.Move, 0));
    }

    public void EnqueueMouseWheel(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        _inputSnapshot.EnqueueMouseEvent(new MouseInputEvent(MouseInputEventKind.Wheel, delta));
    }

    public long GetMouseX()
    {
        return _inputSnapshot.GetMouseX();
    }

    public long GetMouseY()
    {
        return _inputSnapshot.GetMouseY();
    }

    public void GetMousePosition(out long x, out long y)
    {
        _inputSnapshot.GetMousePosition(out x, out y);
    }

    public long GetMouseClearGeneration()
    {
        return _inputSnapshot.MouseClearGeneration;
    }

    public void PublishMouseMove(long x, long y)
    {
        _inputSnapshot.PublishMouseMove(x, y);
    }

    public void PublishMouseButtonPress(int buttonNumber, long x, long y)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.PublishMouseButtonPress(buttonNumber, x, y);
    }

    public void PublishMouseButtonRelease(int buttonNumber, long x, long y)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.PublishMouseButtonRelease(buttonNumber, x, y);
    }

    public void PublishMouseButtonReleaseKeepPosition(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return;
        }

        _inputSnapshot.PublishMouseButtonReleaseKeepPosition(buttonNumber);
    }

    public void PublishMouseWheel(int delta, long x, long y)
    {
        _inputSnapshot.PublishMouseWheel(delta, x, y);
    }

    public bool IsMouseButtonDown(int buttonNumber)
    {
        if (!IsSupportedMouseButton(buttonNumber))
        {
            return false;
        }

        return _inputSnapshot.IsMouseButtonDown(buttonNumber);
    }

    public int ReadMouseButtonPress()
    {
        return _inputSnapshot.ReadMouseButtonPress();
    }

    public MouseInputEvent ReadMouseEvent()
    {
        if (_inputSnapshot.TryReadMouseEvent(out var mouseEvent))
        {
            return mouseEvent;
        }

        return default;
    }

    public void ClearMouseState()
    {
        _inputSnapshot.ClearMouseState();
    }

    #endregion

    #region Управление состоянием экрана

    /// <summary>
    /// Обновить флаги экрана в снимке ввода.
    /// </summary>
    public void SetScreenState(bool isScreenActive, bool isScreenOutputActive, int screenMode, int screenWidth, int screenHeight)
    {
        _inputSnapshot.SetScreenState(isScreenActive, isScreenOutputActive, screenMode, screenWidth, screenHeight);
    }

    #endregion

    #region Сброс

    public void Reset()
    {
        _inputSnapshot.Reset();
    }

    #endregion

    private static bool IsSupportedMouseButton(int buttonNumber) => buttonNumber is >= 1 and <= 3;
}
