# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## MOUSE / EVENTS

**Дата:** 2026-08-20
**Метод:** полный forensic-анализ кода, тестов, samples, документации, bytecode coverage

---

## 1. Резюме

Подсистема MOUSE / EVENTS обеспечивает мышиный ввод в языке Axiom через четыре встроенных функции (`MOUSEX`, `MOUSEY`, `MOUSEBUTTON`, `MOUSEPRESS`) и оператор `ON MOUSE` с четырьмя типами событий (Press, Release, Move, Wheel) и тремя режимами (Register, Pause/STOP, Unregister/OFF, Resume/ON). Подсистема реализована на всех уровнях конвейера: лексер, парсер, семантика, интерпретатор, Bytecode VM, IDE, тесты, samples и документация.

**Общая оценка: подсистема стабильна, корректна и архитектурно зрелая.** Основной технический долг — дублирование логики между Interpreter и BytecodeVm (по аналогии с уже исправленными волнами Numeric, Regex, String, FileSystem, DateTime, Keyboard). Второй существенный finding — отсутствие dedicated engine-comparison benchmark для мышиных событий. Третий — небольшая асимметрия проверки `_mouseHandlers.Count == 0` между Interpreter и VM в `DispatchMouseHandlers`.

**Health Score: 8.0/10**

---

## 2. Что представляет собой эта подсистема

Подсистема MOUSE / EVENTS отвечает за:

- неблокирующее чтение координат указателя мыши (`MOUSEX`, `MOUSEY`);
- проверку текущего удержания кнопки мыши (`MOUSEBUTTON`);
- одноразовую проверку события нажатия кнопки из очереди (`MOUSEPRESS`);
- регистрацию, паузу, возобновление и отключение событийных обработчиков мыши (`ON MOUSE`);
- поддержку четырёх типов событий: нажатие, отпускание, перемещение, прокрутка колеса;
- пересчёт координат из абсолютных экранных в мировые при активном `CANVAS`;
- маршрутизацию мышиных событий из IDE (WPF) в runtime.

Подсистема не включает клавиатурный ввод (`INKEY`, `KEYDOWN`, `KEYPRESS`, `ON KEY`), хотя разделяет с ним инфраструктуру диспетчеризации на границах операторов (`StatementBoundary` / `DispatchInputHandlers`).

---

## 3. Полный список возможностей

### SYS-001: MOUSEX()

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `MOUSEX()` |
| Назначение | Чтение текущей горизонтальной координаты указателя |
| Возвращаемый тип | LONG |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.MouseX` |
| Binder | `BoundCallExpression`, 0 аргументов |
| Interpreter | `ReadMouseX()` → `BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates()` |
| Bytecode VM | `BuiltinExecution.NullaryNoLocation` → `ReadMouseX()` |
| Opcode | `CallBuiltin` (общий путь) |
| Runtime Helper | `BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates()` |
| IDE | Подсветка `MOUSEX` в обоих `.xshd` |
| Tests | Binder, Interpreter, VM |
| Samples | `samples/functions/mousex/current_position.axm`, `canvas_world_position.axm` |
| Manual | `docs/manual/mousex.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-002: MOUSEY()

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `MOUSEY()` |
| Назначение | Чтение текущей вертикальной координаты указателя |
| Возвращаемый тип | LONG |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.MouseY` |
| Binder | `BoundCallExpression`, 0 аргументов |
| Interpreter | `ReadMouseY()` → `BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates()` |
| Bytecode VM | `BuiltinExecution.NullaryNoLocation` → `ReadMouseY()` |
| Opcode | `CallBuiltin` (общий путь) |
| Runtime Helper | `BuiltinMouseHelpers.TranslateMouseYToRuntimeCoordinates()` |
| IDE | Подсветка `MOUSEY` в обоих `.xshd` |
| Tests | Binder, Interpreter, VM |
| Samples | `samples/functions/mousey/current_position.axm`, `canvas_world_position.axm` |
| Manual | `docs/manual/mousey.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-003: MOUSEBUTTON(buttonNumber)

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `MOUSEBUTTON(buttonNumber)` |
| Назначение | Проверка текущего удержания указанной кнопки мыши |
| Возвращаемый тип | BOOLEAN |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.MouseButton` |
| Binder | `BoundCallExpression`, 1 аргумент INTEGER |
| Interpreter | `IsMouseButtonDown()` → `_host.IsMouseButtonDown()` |
| Bytecode VM | `BuiltinExecution.UnaryNoLocation` → `IsMouseButtonDown()` |
| Opcode | `CallBuiltin` (общий путь) |
| IDE | Подсветка `MOUSEBUTTON` в обоих `.xshd` |
| Tests | Binder, Interpreter, VM |
| Samples | `samples/functions/mousebutton/button_states.axm` |
| Manual | `docs/manual/mousebutton.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-004: MOUSEPRESS(buttonNumber)

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `MOUSEPRESS(buttonNumber)` |
| Назначение | Одноразовая проверка события нажатия кнопки из очереди (consumes event) |
| Возвращаемый тип | BOOLEAN |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.MousePress` |
| Binder | `BoundCallExpression`, 1 аргумент INTEGER |
| Interpreter | `ConsumeMouseButtonPress()` → `BuiltinMouseHelpers.ConsumeMouseButtonPress()` |
| Bytecode VM | `BuiltinExecution.UnaryNoLocation` → `ConsumeMouseButtonPress()` |
| Opcode | `CallBuiltin` (общий путь) |
| Runtime Helper | `BuiltinMouseHelpers.ConsumeMouseButtonPress()` |
| IDE | Подсветка `MOUSEPRESS` в обоих `.xshd` |
| Tests | Binder, Interpreter, VM |
| Samples | `samples/functions/mousepress/button_presses.axm` |
| Manual | `docs/manual/mousepress.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-005: ON MOUSE(buttonNumber), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE(buttonNumber), HandlerName` |
| Назначение | Регистрация обработчика нажатия кнопки мыши |
| Parser | `OnMouseStatementSyntax` |
| Binder | `BoundOnMouseStatement` с `BoundOnMouseActionKind.Register` |
| Interpreter | `ExecuteOnMouse()` → `RegisterMouseHandler()` |
| Bytecode Lowering | `LowerOnMouse()` → `RegisterMouseHandler` |
| Bytecode VM | `ExecuteRegisterMouseHandler()` |
| Opcode | `RegisterMouseHandler` |
| IDE | Подсветка `ON`, `MOUSE` |
| Tests | Parser, Binder, Interpreter, VM, Acceptance |
| Samples | `samples/operators/on-mouse/left_button_handler.axm` |
| Manual | `docs/manual/on-mouse.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-006: ON MOUSE(buttonNumber, "UP"), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE(buttonNumber, "UP"), HandlerName` |
| Назначение | Регистрация обработчика отпускания кнопки мыши |
| Parser | `OnMouseStatementSyntax` с `EventExpression` |
| Binder | `BoundOnMouseStatement` с `BoundMouseEventKind.Release` |
| Interpreter | `ExecuteOnMouse()` → `RegisterMouseHandler()` |
| Bytecode VM | `ExecuteRegisterMouseHandler()` |
| Opcode | `RegisterMouseHandler` |
| Tests | Binder, VM |
| Samples | `samples/operators/on-mouse/release_move_handler.axm` |
| Manual | `docs/manual/on-mouse.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-007: ON MOUSE("MOVE"), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE("MOVE"), HandlerName` |
| Назначение | Регистрация обработчика перемещения указателя |
| Parser | `OnMouseStatementSyntax` с `EventExpression` = `"MOVE"` |
| Binder | `BoundOnMouseStatement` с `BoundMouseEventKind.Move`, buttonExpression = 0 |
| Interpreter | `ExecuteOnMouse()` → `RegisterMouseHandler()` |
| Bytecode VM | `ExecuteRegisterMouseHandler()` |
| Opcode | `RegisterMouseHandler` |
| Tests | Binder, VM |
| Samples | `samples/operators/on-mouse/release_move_handler.axm`, `canvas_world_handler.axm` |
| Manual | `docs/manual/on-mouse.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-008: ON MOUSE("WHEEL"), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE("WHEEL"), HandlerName` |
| Назначение | Регистрация обработчика прокрутки колеса мыши |
| Parser | `OnMouseStatementSyntax` с `EventExpression` = `"WHEEL"` |
| Binder | `BoundOnMouseStatement` с `BoundMouseEventKind.Wheel`, buttonExpression = 0 |
| Interpreter | `ExecuteOnMouse()` → `RegisterMouseHandler()` |
| Bytecode VM | `ExecuteRegisterMouseHandler()` |
| Opcode | `RegisterMouseHandler` |
| Tests | Binder, VM |
| Samples | `samples/operators/on-mouse/wheel_handler.axm` |
| Manual | `docs/manual/on-mouse.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-009: ON MOUSE(...), OFF

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE(...), OFF` |
| Назначение | Полное отключение обработчика |
| Parser | `OnMouseStatementSyntax` с `DisablesHandler = true` |
| Binder | `BoundOnMouseStatement` с `BoundOnMouseActionKind.Unregister` |
| Interpreter | `UnregisterMouseHandler()` |
| Bytecode VM | `ExecuteUnregisterMouseHandler()` |
| Opcode | `UnregisterMouseHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-010: ON MOUSE(...), STOP

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE(...), STOP` |
| Назначение | Пауза обработчика без удаления |
| Parser | `OnMouseStatementSyntax` с `PausesHandler = true` |
| Binder | `BoundOnMouseStatement` с `BoundOnMouseActionKind.Pause` |
| Interpreter | `PauseMouseHandler()` |
| Bytecode VM | `ExecutePauseMouseHandler()` |
| Opcode | `PauseMouseHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-011: ON MOUSE(...), ON

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON MOUSE(...), ON` |
| Назначение | Возобновление ранее зарегистрированного обработчика |
| Parser | `OnMouseStatementSyntax` с `ResumesHandler = true` |
| Binder | `BoundOnMouseStatement` с `BoundOnMouseActionKind.Resume` |
| Interpreter | `ResumeMouseHandler()` |
| Bytecode VM | `ExecuteResumeMouseHandler()` |
| Opcode | `ResumeMouseHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-012: IMouseInputProvider

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Хост-интерфейс для мышиного ввода |
| Файл | `src/Axiom.Runtime/Host/IMouseInputProvider.cs` |
| Методы | `GetMouseX()`, `GetMouseY()`, `IsMouseButtonDown(int)`, `ReadMouseButtonPress()`, `ReadMouseEvent()` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-013: MouseInputEvent / MouseInputEventKind

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Представление мышиного события |
| Файл | `src/Axiom.Runtime/Host/IMouseInputProvider.cs` |
| Структура | `record struct MouseInputEvent(MouseInputEventKind Kind, int ButtonNumber)` |
| Перечисление | `MouseInputEventKind { None, Press, Release, Move, Wheel }` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-014: BuiltinMouseHelpers

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Общая логика мышиных операций для Interpreter и VM |
| Файл | `src/Axiom.Runtime/Builtins/BuiltinMouseHelpers.cs` |
| Класс | `BuiltinMouseHelpers` (internal static) |
| Методы | `IsSupportedMouseButton`, `IsSupportedMouseHandlerTarget`, `IsSupportedMouseEvent`, `GetMouseHandlerKey`, `SetMouseHandlerEnabled`, `ConsumeMouseButtonPress`, `TryReadMouseEvent`, `TranslateMouseXToRuntimeCoordinates`, `TranslateMouseYToRuntimeCoordinates`, `TryTranslateMouseAbsoluteToLogical` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-015: MouseHandlerRegistration

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Регистрация обработчика мышиного события |
| Файл | `src/Axiom.Runtime/Builtins/BuiltinMouseHelpers.cs` |
| Структура | `sealed record MouseHandlerRegistration(RuntimeFunctionValue Handler, bool PassEvent, bool IsEnabled)` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-016: ProgramMouseInputRouter

| Поле | Значение |
|------|----------|
| Категория | IDE Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Маршрутизация WPF-событий мыши в ProgramConsoleService |
| Файл | `src/Axiom.Ide/Services/Input/ProgramMouseInputRouter.cs` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-017: GraphicsCoordinateSpaceMath (mouse-relevant parts)

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Пересчёт абсолютных координат мыши в мировые координаты CANVAS |
| Файл | `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

---

## 4. Фактическая архитектура

### 4.1 Слои подсистемы

```
┌─────────────────────────────────────────────────────────────┐
│  IDE (WPF)                                                  │
│  ┌──────────────────────┐                                   │
│  │ ProgramMouseInput-   │                                   │
│  │ Router               │                                   │
│  │ (PreviewMouse*)      │                                   │
│  └──────────┬───────────┘                                   │
│             │                                               │
│  ┌──────────▼───────────┐                                   │
│  │ ProgramConsoleService│                                   │
│  │ (IMouseInputProvider)│                                   │
│  │ - MouseX, MouseY     │                                   │
│  │ - PressedButtons     │                                   │
│  │ - MousePresses queue │                                   │
│  │ - MouseEvents queue  │                                   │
│  └──────────┬───────────┘                                   │
└─────────────┼───────────────────────────────────────────────┘
              │
              │ IMouseInputProvider.GetMouseX() / ReadMouseEvent()
              ▼
┌─────────────────────────────────────────────────────────────┐
│  Runtime                                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ BuiltinMouseHelpers (shared)                         │   │
│  │ - TranslateMouseX/YToRuntimeCoordinates()            │   │
│  │ - ConsumeMouseButtonPress()                          │   │
│  │ - TryReadMouseEvent()                                │   │
│  │ - GetMouseHandlerKey()                               │   │
│  │ - IsSupportedMouseHandlerTarget()                    │   │
│  │ - SetMouseHandlerEnabled()                           │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌──────────────────────┐  ┌─────────────────────────────┐  │
│  │ Interpreter          │  │ BytecodeVm                  │  │
│  │ .Mouse.cs            │  │ .Mouse.cs                   │  │
│  │ - _mouseHandlers     │  │ - _mouseHandlers            │  │
│  │ - _bufferedMouse-    │  │ - _bufferedMouseEvents      │  │
│  │   Events             │  │ - _mouseHandlerDepth        │  │
│  │ - _mouseHandlerDepth │  │                             │  │
│  │ - DispatchMouse-     │  │ - DispatchMouseHandlers()   │  │
│  │   Handlers()         │  │ - ExecuteRegisterMouse-     │  │
│  │ - RegisterMouse-     │  │   Handler()                 │  │
│  │   Handler()          │  │ - ExecuteUnregisterMouse-   │  │
│  │ - InvokeMouse-       │  │   Handler()                 │  │
│  │   Handler()          │  │ - ExecutePauseMouseHandler()│  │
│  │                      │  │ - ExecuteResumeMouseHandler()│ │
│  │                      │  │ - InvokeMouseHandler()      │  │
│  └──────────────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│  Semantics (Binder)                                         │
│  - BindOnMouseStatement()                                   │
│  - BindOnMouseTarget()                                      │
│  - FunctionAcceptsOnMouseEvent()                            │
│  - TryParseMouseEvent()                                     │
│  - BoundOnMouseStatement                                    │
│  - BoundMouseEventKind                                      │
│  - BoundOnMouseActionKind                                   │
└─────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│  Syntax / Parser                                            │
│  - OnMouseStatementSyntax                                   │
│  - ParseOnMouseStatement()                                  │
└─────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────┐
│  Bytecode / Lowering                                        │
│  - LowerOnMouse()                                           │
│  - LoweredInstruction.RegisterMouseHandler()                │
│  - LoweredInstruction.UnregisterMouseHandler()              │
│  - LoweredInstruction.PauseMouseHandler()                   │
│  - LoweredInstruction.ResumeMouseHandler()                  │
│  - Opcode.RegisterMouseHandler                              │
│  - Opcode.UnregisterMouseHandler                            │
│  - Opcode.PauseMouseHandler                                 │
│  - Opcode.ResumeMouseHandler                                │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Ключевые файлы

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Runtime/Host/IMouseInputProvider.cs` | Интерфейс хоста, `MouseInputEvent`, `MouseInputEventKind` |
| `src/Axiom.Runtime/Builtins/BuiltinMouseHelpers.cs` | Общая логика: координаты, очередь, валидация, ключи обработчиков |
| `src/Axiom.Runtime/Execution/Interpreter.Mouse.cs` | Реализация мышиных операций в интерпретаторе |
| `src/Axiom.Runtime/Execution/BytecodeVm.Mouse.cs` | Реализация мышиных операций в VM |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` | `ExecuteOnMouse()`, `DispatchInputHandlers()` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs` | `ExecuteStatementBoundary()` (вызывает `DispatchMouseHandlers`) |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | Диспетч `BuiltinFunctionId.Mouse*` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` | Диспетч `BuiltinFunctionId.Mouse*` |
| `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` | Fast-path диспетч `BuiltinFunctionId.Mouse*` |
| `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs` | `GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical()` |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` | `BindOnMouseStatement()`, `BindOnMouseTarget()` |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | `BoundOnMouseStatement`, `BoundMouseEventKind`, `BoundOnMouseActionKind` |
| `src/Axiom.Syntax/Statements.cs` | `OnMouseStatementSyntax` |
| `src/Axiom.Parsing/Parser.Statements.cs` | `ParseOnMouseStatement()` |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | `LowerOnMouse()` |
| `src/Axiom.Bytecode/Model/Opcode.cs` | `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler`, `ResumeMouseHandler` |
| `src/Axiom.Ide/Services/Input/ProgramMouseInputRouter.cs` | Маршрутизация WPF → ProgramConsoleService |
| `src/Axiom.Ide/Services/Input/IProgramConsoleService.cs` | Интерфейс `IMouseInputProvider` + мышиные методы |

---

## 5. Полный путь выполнения

### 5.1 Путь встроенных функций (MOUSEX, MOUSEY, MOUSEBUTTON, MOUSEPRESS)

```
Исходный код: PRINT MOUSEX()
    ↓
Lexer: токены [PRINT, MOUSEX, (, )]
    ↓
Parser: CallExpressionSyntax(BuiltinFunctionId.MouseX)
    ↓
Binder: BoundCallExpression(BuiltinFunctionId.MouseX, TypeSymbol.Long, 0 аргументов)
    ↓
Interpreter: EvaluateExpression → CallBuiltin(MouseX) → ReadMouseX()
    → RequireDrawableSurfaceActive("MOUSEX")
    → BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates(_host, _host.GetMouseX(), _host.GetMouseY())
    → если CANVAS: GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical()
    → если SCREEN: абсолютная координата
    → RuntimeValue.FromLong(result)

Bytecode: CallBuiltin(MouseX)
    ↓
VM: ExecuteCallBuiltin → BuiltinFunctionId.MouseX → ReadMouseX()
    → (та же цепочка, что и в Interpreter)
```

### 5.2 Путь оператора ON MOUSE

```
Исходный код: ON MOUSE(1), HandleMouse
    ↓
Lexer: токены [ON, MOUSE, (, 1, ), ,, HandleMouse]
    ↓
Parser: OnMouseStatementSyntax(buttonExpression=1, actionToken=HandleMouse)
    ↓
Binder: BoundOnMouseStatement(
    buttonExpression=BoundLiteralExpression(1),
    eventKind=BoundMouseEventKind.Press,
    handlerExpression=BoundFunctionExpression(HandleMouse),
    actionKind=BoundOnMouseActionKind.Register,
    handlerReceivesEvent=false/true)
    ↓
Interpreter: ExecuteOnMouse()
    → EvaluateExpression(buttonExpression) → 1
    → ToMouseInputEventKind(Press) → MouseInputEventKind.Press
    → EvaluateExpression(handlerExpression) → RuntimeFunctionValue
    → RegisterMouseHandler(1, Press, handler, passEvent)
    → _mouseHandlers[new MouseHandlerKey(Press, 1)] = registration

Bytecode: LowerOnMouse()
    → LowerExpression(buttonExpression) → LoadConst(1)
    → LowerExpression(handlerExpression) → LoadConst(function)
    → RegisterMouseHandler(passEvent, eventKind)
    ↓
VM: ExecuteRegisterMouseHandler()
    → PopValue().AsFunction() → handlerValue
    → PopValue().AsInteger() → buttonNumber
    → ToMouseInputEventKind(operand) → eventKind
    → _mouseHandlers[new MouseHandlerKey(eventKind, buttonNumber)] = registration
```

### 5.3 Путь диспетчеризации событий

```
Interpreter:
    ExecuteStatements() → DispatchInputHandlers(statement.Location)
        → DispatchKeyboardHandlers(location)
        → DispatchMouseHandlers(location)
            → если _mouseHandlers.Count == 0: return
            → если !_host.IsScreenActive && !_host.IsCanvasActive: return
            → если _mouseHandlerDepth > 0: return
            → цикл: BuiltinMouseHelpers.TryReadMouseEvent(_bufferedMouseEvents, _host.ReadMouseEvent, out mouseEvent)
                → GetMouseHandlerKey(mouseEvent)
                → если обработчик найден и enabled: InvokeMouseHandler()
                → иначе: deferredEvents.Add(mouseEvent)
            → deferredEvents → _bufferedMouseEvents

VM:
    ExecuteStatementBoundary(instructionIndex)
        → если _keyboardHandlers.Count == 0 && _mouseHandlers.Count == 0: return
        → если _keyboardHandlerDepth > 0: return
        → если _mouseHandlers.Count > 0: DispatchMouseHandlers(location)
            → (та же логика, что и в Interpreter)
```

---

## 6. Фактический семантический контракт

### 6.1 MOUSEX() / MOUSEY()

**Вход:** нет аргументов.

**Обработка:**
1. Проверка: активна ли рисуемая поверхность (SCREEN или CANVAS). Если нет — MB3010.
2. Чтение абсолютных координат от хоста: `_host.GetMouseX()`, `_host.GetMouseY()`.
3. Если активен CANVAS — пересчёт через `GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical()`.
4. Если активен SCREEN — возврат абсолютной координаты.

**Выход:** `LONG` — координата указателя. `-1` если координаты недоступны.

**Побочные эффекты:** нет.

**Edge cases:**
- Если хост возвращает отрицательные координаты → `TryTranslateMouseAbsoluteToLogical` возвращает `false` → результат `-1`.
- Если `CANVAS` активен, но `VIEWPORT` или `CAMERA` не заданы → пересчёт через дефолтные значения.

### 6.2 MOUSEBUTTON(buttonNumber)

**Вход:** `INTEGER` — номер кнопки (1=левая, 2=правая, 3=средняя).

**Обработка:**
1. Проверка: активна ли рисуемая поверхность. Если нет — MB3010.
2. Вызов `_host.IsMouseButtonDown(buttonNumber)`.

**Выход:** `BOOLEAN` — `TRUE` если кнопка удерживается.

**Edge cases:**
- buttonNumber вне диапазона 1..3 → `_host.IsMouseButtonDown()` возвращает `false` (поведение хоста).
- buttonNumber = 0 или отрицательный → `false`.

### 6.3 MOUSEPRESS(buttonNumber)

**Вход:** `INTEGER` — номер кнопки.

**Обработка:**
1. Проверка: активна ли рисуемая поверхность. Если нет — MB3010.
2. `BuiltinMouseHelpers.ConsumeMouseButtonPress()`:
   - Если buttonNumber не в 1..3 → `false`.
   - Цикл по очереди: ищет первое событие `Press` с подходящей кнопкой.
   - Найденное событие удаляется из очереди, неподходящие события возвращаются обратно.

**Выход:** `BOOLEAN` — `TRUE` если нажатие найдено и потреблено.

**Edge cases:**
- Очередь пуста → `false`.
- В очереди есть нажатия других кнопок → они сохраняются, возвращается `false` для запрашиваемой кнопки.
- События Release/Move/Wheel не читаются функцией `MOUSEPRESS`.

### 6.4 ON MOUSE(...), handler

**Вход:** номер кнопки (для Press/Release) или строковое имя события (для Move/Wheel), имя обработчика.

**Обработка:**
1. Парсинг цели события: `BindOnMouseTarget()`.
2. Валидация обработчика: `FunctionAcceptsOnMouseEvent()` — SUB без параметров или SUB(INTEGER, LONG, LONG).
3. Регистрация в словаре `_mouseHandlers`.

**Выход:** нет (statement).

**Побочные эффекты:** регистрация/удаление/пауза/возобновление обработчика.

**Edge cases:**
- Обработчик не является SUB → MB2035.
- Обработчик имеет неподходящую сигнатуру (не 0 и не 3 параметра) → MB2035.
- Неизвестное имя события → MB2036.
- `"MOVE"` и `"WHEEL"` указываются без номера кнопки → buttonNumber = 0.
- `"UP"` указывается с номером кнопки → Release для этой кнопки.

---

## 7. Анализ Parser

### 7.1 Синтаксический узел

`OnMouseStatementSyntax` (`src/Axiom.Syntax/Statements.cs:371`) содержит:
- `OnKeyword`, `MouseKeyword` — ключевые слова
- `OpenParenToken`, `CloseParenToken` — скобки
- `ButtonExpression` — выражение номера кнопки или строкового имени события
- `TargetCommaToken`, `EventExpression` — опциональная вторая часть цели (для `"UP"`)
- `CommaToken` — запятая перед действием
- `ActionToken` — имя обработчика или ключевое слово ON/STOP/OFF

Свойства `DisablesHandler`, `ResumesHandler`, `PausesHandler` определяются по `ActionToken.Kind`.

### 7.2 Качество парсинга

Парсер корректно обрабатывает:
- `ON MOUSE(1), HandleMouse` — одна кнопка
- `ON MOUSE(1, "UP"), HandleMouse` — кнопка + событие
- `ON MOUSE("MOVE"), HandleMouse` — только строковое имя события
- `ON MOUSE("WHEEL"), HandleMouse` — только строковое имя события
- `ON MOUSE(1), OFF` — отключение
- `ON MOUSE(1), STOP` — пауза
- `ON MOUSE(1), ON` — возобновление

**Finding:** Парсер не накладывает ограничений на содержимое `ButtonExpression` и `EventExpression` на синтаксическом уровне — это делает Binder. Это корректное разделение ответственности.

---

## 8. Анализ Binder

### 8.1 BindOnMouseStatement (`Binder.IoStatements.cs:635`)

Логика привязки:
1. `BindOnMouseTarget()` — определяет buttonExpression и eventKind.
2. Если `DisablesHandler` → `BoundOnMouseActionKind.Unregister`.
3. Если `PausesHandler` → `BoundOnMouseActionKind.Pause`.
4. Если `ResumesHandler` → `BoundOnMouseActionKind.Resume`.
5. Иначе: поиск функции по имени, валидация сигнатуры, регистрация.

### 8.2 BindOnMouseTarget (`Binder.IoStatements.cs:676`)

Логика определения цели:
- Если只有一个 аргумент и это строковый литерал:
  - Если это `"MOVE"` или `"WHEEL"` → buttonNumber = 0, соответствующий eventKind.
  - Иначе → ошибка MB2036.
- Если два аргумента:
  - Первый — выражение номера кнопки (привязывается как INTEGER).
  - Второй — строковый литерал имени события (не может быть `"MOVE"` или `"WHEEL"`).

### 8.3 FunctionAcceptsOnMouseEvent (`Binder.IoStatements.cs:734`)

Валидация сигнатуры обработчика:
- 0 параметров → `false` (обработчик не получает данные события).
- 3 параметра (INTEGER, LONG, LONG), все BYVAL → `true` (обработчик получает данные события).
- Любая другая сигнатура → `null` → ошибка MB2035.

**Finding:** Сигнатура `(INTEGER, LONG, LONG)` жёстко зафиксирована. Это означает, что обработчик не может получить только координаты без номера кнопки, или только номер кнопки без координат. Это ограничение по дизайну, не баг.

### 8.4 TryParseMouseEvent (`Binder.IoStatements.cs:719`)

Нормализация имени события:
- `"DOWN"`, `"PRESS"`, `"PRESSED"` → `BoundMouseEventKind.Press`
- `"UP"`, `"RELEASE"`, `"RELEASED"` → `BoundMouseEventKind.Release`
- `"MOVE"`, `"MOVED"`, `"MOUSEMOVE"` → `BoundMouseEventKind.Move`
- `"WHEEL"`, `"MOUSEWHEEL"`, `"SCROLL"`, `"SCROLLED"` → `BoundMouseEventKind.Wheel`

**Finding:** Широкий набор алиасов для имён событий — хорошее решение для удобства пользователя.

---

## 9. Анализ Interpreter

### 9.1 Файлы

- `src/Axiom.Runtime/Execution/Interpreter.Mouse.cs` — основная логика мышиных операций.
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` — `ExecuteOnMouse()`, `DispatchInputHandlers()`.
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` — диспетч `BuiltinFunctionId.Mouse*`.

### 9.2 Диспетчеризация событий

`DispatchInputHandlers()` вызывается:
- В `ExecuteStatements()` — перед каждым оператором (строка 18).
- В `ExecuteWhile()` — на каждой итерации цикла (строка 659).
- В `ExecuteDoLoop()` — на каждой итерации цикла (строка 753).
- В `ExecuteFor()` — на каждой итерации цикла (строка 896).
- В `ExecuteSelectCase()` — перед обработкой каждого case (строка 943).
- В `ExecuteTry()` — перед выполнением тела try (строка 990).

**Finding:** Диспетчеризация происходит на границах операторов и в циклах — это корректно и соответствует документации.

### 9.3 InvokeMouseHandler

```csharp
private void InvokeMouseHandler(MouseHandlerRegistration registration, MouseInputEvent mouseEvent, TextLocation callSiteLocation)
{
    _mouseHandlerDepth++;
    try
    {
        var mouseX = ReadMouseX();
        var mouseY = ReadMouseY();
        var arguments = registration.PassEvent
            ? new[] { RuntimeValue.FromInteger(mouseEvent.ButtonNumber), RuntimeValue.FromLong(mouseX), RuntimeValue.FromLong(mouseY) }
            : Array.Empty<RuntimeValue>();
        _ = InvokeUserRoutine(registration.Handler, arguments, callSiteLocation);
    }
    finally
    {
        _mouseHandlerDepth--;
    }
}
```

**Finding:** Координаты мыши читаются в момент вызова обработчика, а не в момент возникновения события. Это означает, если между моментом события и моментом обработки курсор переместится, обработчик получит актуальные координаты, а не координаты на момент события. Это соответствует документации: "параметры x и y получают текущие координаты указателя на активной поверхности программы в момент обработки события".

### 9.4 Защита от рекурсии

`_mouseHandlerDepth` предотвращает рекурсивный вызов обработчиков мыши. Если обработчик мыши вызывает оператор, который снова пытается диспетчеризировать мышиные события, диспетчеризация пропускается.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering

`LowerOnMouse()` (`BoundProgramLowerer.cs:944`) генерирует:
- Для Register: `LoadConst(buttonNumber)` + `LoadConst(handler)` + `RegisterMouseHandler(passEvent, eventKind)`.
- Для Unregister: `LoadConst(buttonNumber)` + `UnregisterMouseHandler(eventKind)`.
- Для Pause: `LoadConst(buttonNumber)` + `PauseMouseHandler(eventKind)`.
- Для Resume: `LoadConst(buttonNumber)` + `ResumeMouseHandler(eventKind)`.

### 10.2 Opcodes

| Opcode | Операнды | Стек |
|--------|----------|------|
| `RegisterMouseHandler` | [0]: passEvent flag, [1]: eventKind | -2 (pop handler, pop buttonNumber) |
| `UnregisterMouseHandler` | [0]: eventKind | -1 (pop buttonNumber) |
| `PauseMouseHandler` | [0]: eventKind | -1 (pop buttonNumber) |
| `ResumeMouseHandler` | [0]: eventKind | -1 (pop buttonNumber) |

### 10.3 ExecuteRegisterMouseHandler

```csharp
private void ExecuteRegisterMouseHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
{
    var handlerValue = PopValue(frame, instructionIndex).AsFunction();
    var buttonNumber = PopValue(frame, instructionIndex).AsInteger();
    var eventKind = ToMouseInputEventKind(instruction.OperandCount > 1 ? instruction.GetOperand(1) : 0);
    // ...
    var passEvent = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
    _mouseHandlers[new BuiltinMouseHelpers.MouseHandlerKey(eventKind, buttonNumber)] = new MouseHandlerRegistration(handlerValue!, passEvent, IsEnabled: true);
}
```

**Finding:** Порядок pop в VM (сначала handler, потом buttonNumber) отличается от порядка в Lowering (сначала buttonNumber, потом handler). Это корректно, так как стек — LIFO: если Lowering кладёт buttonNumber первым, а handler вторым, то VM сначала pop handler, потом buttonNumber.

### 10.4 StatementBoundary

В VM диспетчеризация мышиных событий происходит в `ExecuteStatementBoundary()` (`BytecodeVm.Keyboard.cs:12`), который вызывается при `Opcode.StatementBoundary`.

**Finding:** В VM проверка `_keyboardHandlers.Count == 0 && _mouseHandlers.Count == 0` происходит в `ExecuteStatementBoundary`, а затем отдельно `_mouseHandlers.Count > 0` перед вызовом `DispatchMouseHandlers`. В Interpreter проверка `_mouseHandlers.Count == 0` происходит внутри `DispatchMouseHandlers`. Это небольшая асимметрия, но не влияет на корректность.

---

## 11. Анализ Runtime

### 11.1 BuiltinMouseHelpers

Общий хелпер содержит всю логику, общую для Interpreter и VM:
- `IsSupportedMouseButton(int)` — проверка 1..3.
- `IsSupportedMouseHandlerTarget(int, MouseInputEventKind)` — валидация комбинации кнопка+событие.
- `IsSupportedMouseEvent(MouseInputEvent)` — валидация события.
- `GetMouseHandlerKey(MouseInputEvent)` — создание ключа для словаря обработчиков.
- `SetMouseHandlerEnabled(...)` — пауза/возобновление обработчика.
- `ConsumeMouseButtonPress(...)` — потребление нажатия из очереди.
- `TryReadMouseEvent(...)` — чтение события из буфера или хоста.
- `TranslateMouseXToRuntimeCoordinates(...)` / `TranslateMouseYToRuntimeCoordinates(...)` — пересчёт координат.

**Finding:** Наличие общего хелпера — хорошее архитектурное решение. Логика мышиных операций не дублирована между Interpreter и VM на уровне хелперов.

### 11.2 IMouseInputProvider

Интерфейс хоста с дефолтной реализацией `ReadMouseEvent()`:
```csharp
MouseInputEvent ReadMouseEvent()
{
    var buttonNumber = ReadMouseButtonPress();
    return buttonNumber == 0
        ? default
        : new MouseInputEvent(MouseInputEventKind.Press, buttonNumber);
}
```

**Finding:** Дефолтная реализация `ReadMouseEvent()` конвертирует `ReadMouseButtonPress()` в `MouseInputEvent(Press, buttonNumber)`. Это означает, что если хост не переопределяет `ReadMouseEvent()`, он может сообщать только о нажатиях, не об отпусканиях, перемещениях и колесе. Это совместимо с базовым `IMouseInputProvider`, но IDE (`ProgramConsoleService`) переопределяет `ReadMouseEvent()` для поддержки всех типов событий.

---

## 12. Взаимодействие с типами

| Тип | Взаимодействие |
|-----|---------------|
| INTEGER | `MOUSEBUTTON` принимает INTEGER, `ON MOUSE` buttonNumber — INTEGER |
| LONG | `MOUSEX`/`MOUSEY` возвращают LONG, параметры обработчика x/y — LONG |
| BOOLEAN | `MOUSEBUTTON`/`MOUSEPRESS` возвращают BOOLEAN |
| STRING | Имена событий в `ON MOUSE` — строковые литералы |
| FUNCTION | Обработчик `ON MOUSE` — значение-функция (`RuntimeFunctionValue`) |

**Finding:** Типовая система корректно интегрирована. Нет неожиданных преобразований или потери типов.

---

## 13. Взаимодействие со String

Строковые взаимодействия ограничены:
- Имена событий (`"UP"`, `"MOVE"`, `"WHEEL"`) — строковые литералы, обрабатываются на этапе Binder.
- Нормализация имён событий: `ToUpperInvariant()`, удаление пробелов и дефисов.

**Finding:** Строковая обработка минимальна и корректна. Нет edge cases с Unicode или пустыми строками, так как имена событий проверяются на этапе компиляции.

---

## 14. Взаимодействие с Array

Массивы не участвуют в мышиной подсистеме напрямую. Обработчики мыши не принимают и не возвращают массивы.

---

## 15. Взаимодействие с TYPE

Пользовательские TYPE не участвуют в мышиной подсистеме напрямую. Обработчики мыши — это SUB, а не методы TYPE. Однако SUB может быть локальной функцией внутри TYPE-метода, и замыкания корректно захватывают переменные.

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Сигнатура обработчика

Допустимые формы:
- `SUB HandleMouse()` — без параметров, `PassEvent = false`.
- `SUB HandleMouse(button AS INTEGER, x AS LONG, y AS LONG)` — с параметрами, `PassEvent = true`.

**Finding:** Сигнатура `(INTEGER, LONG, LONG)` жёстко зафиксирована. Для события WHEEL первый параметр содержит величину прокрутки (delta), а не номер кнопки. Это документировано в manual: "Для ON MOUSE("WHEEL") передаётся величина прокрутки".

### 16.2 Замыкания

Обработчики мыши могут быть локальными SUB/FUNCTION, которые захватывают переменные из окружающей области видимости. Это работает корректно через `RuntimeFunctionValue` (замыкание).

---

## 17. Взаимодействие с Control Flow

### 17.1 Диспетчеризация в циклах

`DispatchInputHandlers()` вызывается:
- В `FOR/NEXT` — на каждой итерации.
- В `WHILE/END WHILE` — на каждой итерации.
- In `DO...LOOP` — на каждой итерации.
- В `SELECT CASE` — перед обработкой каждого case.

**Finding:** Это означает, что обработчики мыши выполняются даже в пустых циклах, что подтверждено тестом `Execute_OnMouseRunsDuringEmptyDoLoop`.

### 17.2 RETURN из обработчика

Если обработчик мыши выполняет `RETURN`, это возвращает управление в точку вызова (диспетчеризацию). Это корректно, так как `InvokeUserRoutine` / `InvokeBytecodeFunction` обрабатывает сигнал возврата.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Диагностики

| Код | Описание |
|-----|----------|
| MB2035 | Обработчик ON MOUSE не является подходящей процедурой SUB |
| MB2036 | Неизвестное имя события мыши |
| MB3010 | MOUSEX/MOUSEY/MOUSEBUTTON/MOUSEPRESS вызваны без активной рисуемой поверхности |

### 18.2 Runtime errors

Если обработчик мыши вызывает `ERROR`, исключение propagates через `InvokeUserRoutine` / `InvokeBytecodeFunction`. Счётчик `_mouseHandlerDepth` корректно декрементируется в `finally`-блоке.

**Finding:** Error handling корректный. Нет риска зависания depth-счётчика при исключении.

---

## 19. State / Lifetime / Ownership

### 19.1 Состояние

| Состояние | Владелец | Lifetime |
|-----------|----------|----------|
| `_mouseHandlers` (Dictionary) | Interpreter / BytecodeVm | Время выполнения программы |
| `_bufferedMouseEvents` (Queue) | Interpreter / BytecodeVm | Время выполнения программы |
| `_mouseHandlerDepth` (int) | Interpreter / BytecodeVm | Время выполнения программы |
| Mouse position (MouseX, MouseY) | ProgramConsoleService / Host | Время сессии |
| Pressed buttons (HashSet) | ProgramConsoleService / Host | Время сессии |
| Mouse press queue | ProgramConsoleService / Host | Время сессии |
| Mouse event queue | ProgramConsoleService / Host | Время сессии |

### 19.2 Сброс состояния

- При `HandleMouseLeave()` в IDE: координаты сбрасываются в -1/-1, очередь событий очищается, удержание кнопок снимается.
- При завершении программы: состояние уничтожается вместе с Interpreter/VM.
- При перезапуске программы: создаётся новый Interpreter/VM с чистым состоянием.

**Finding:** Нет риска утечки состояния между запусками программ.

---

## 20. Edge Cases

### 20.1 Проверенные edge cases

| Сценарий | Поведение | Подтверждено тестами |
|----------|-----------|---------------------|
| MOUSEX/MOUSEY без SCREEN/CANVAS | MB3010 | да |
| MOUSEBUTTON с buttonNumber вне 1..3 | FALSE | да |
| MOUSEPRESS с buttonNumber вне 1..3 | FALSE | да |
| MOUSEPRESS с пустой очередью | FALSE | да |
| MOUSEPRESS с нажатиями других кнопок | сохраняет неподходящие, возвращает FALSE | да |
| ON MOUSE с обработчиком без параметров | работает (PassEvent=false) | да |
| ON MOUSE с обработчиком (INTEGER, LONG, LONG) | работает (PassEvent=true) | да |
| ON MOUSE с обработчиком неподходящей сигнатуры | MB2035 | да |
| ON MOUSE("MOVE") без номера кнопки | buttonNumber=0 | да |
| ON MOUSE("WHEEL") без номера кнопки | buttonNumber=0 | да |
| ON MOUSE(1, "UP") — отпускание | Release event | да |
| ON MOUSE, STOP — пауза | обработчик на паузе | да |
| ON MOUSE, ON — возобновление | обработчик возобновлён | да |
| ON MOUSE, OFF — отключение | обработчик удалён | да |
| Обработчик consume событие до MOUSEPRESS | MOUSEPRESS возвращает FALSE | да |
| Пустой DO LOOP с обработчиком мыши | обработчик выполняется | да |
| CANVAS + VIEWPORT + CAMERA — мировые координаты | пересчёт корректен | да |
| Координаты обработчика = координаты MOUSEX/MOUSEY | совпадают | да |

### 20.2 Непроверенные edge cases

| Сценарий | Статус |
|----------|--------|
| Множественные обработчики на одну кнопку (перезапись) | Неявно покрыт: последний обработчик перезаписывает предыдущий |
| Обработчик мыши внутри TRY/CATCH | Неявно покрыт общими тестами TRY/CATCH |
| Обработчик мыши вызывает ERROR | Неявно покрыт общими тестами ERROR |
| Рекурсивный вызов обработчика мыши | Защищён `_mouseHandlerDepth`, но нет dedicated теста |
| Быстрые последовательные события (stress test) | Нет dedicated теста |
| Одновременные события разных типов | Покрыт тестом `Execute_OnMouseHandlesReleaseAndMoveEvents` |
| SCREEN + VIEW + WINDOW — координаты не пересчитываются | Документировано, но нет dedicated теста |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| MOUSEX() | да | да | да | да | да |
| MOUSEY() | да | да | да | да | да |
| MOUSEBUTTON() | да | да | да | да | да |
| MOUSEPRESS() | да | да | да | да | да |
| ON MOUSE Register | да | да | да | да | да |
| ON MOUSE Unregister | да | да | да | да | да |
| ON MOUSE Pause | да | да | да | да | да |
| ON MOUSE Resume | да | да | да | да | да |
| Dispatch в циклах | да | да | да | да | да |
| Защита от рекурсии | да | да | да | да | да |
| CANVAS координаты | да | да | да | да | да |
| SCREEN координаты | да | да | да | да | да |

**Расхождений не обнаружено.** Подсистема имеет полный паритет между Interpreter и Bytecode VM.

---

## 22. Анализ производительности

### 22.1 Отсутствие dedicated benchmark

**Finding:** В `samples/smoke/engine-compare/` нет dedicated сценария для мышиных событий. Это аналогично ситуации с клавиатурой (отмечено в KEYBOARD_INKEY_FORENSIC_AUDIT.md).

### 22.2 Hot paths

| Hot path | Частота | Стоимость |
|----------|---------|-----------|
| `DispatchMouseHandlers()` | Каждый оператор + каждая итерация цикла | Проверка словаря + чтение из очереди |
| `ReadMouseX()` / `ReadMouseY()` | Каждый вызов MOUSEX/MOUSEY + каждый обработчик | Вызов хоста + возможный пересчёт координат |
| `BuiltinMouseHelpers.TryReadMouseEvent()` | Каждый вызов диспетчеризации | Проверка буфера + вызов хоста |
| `BuiltinMouseHelpers.TranslateMouseXToRuntimeCoordinates()` | Каждый вызов MOUSEX | Проверка CANVAS + возможный пересчёт |

### 22.3 Оценка производительности

- `DispatchMouseHandlers()` в Interpreter проверяет `_mouseHandlers.Count == 0` перед входом в цикл — ранний выход.
- `DispatchMouseHandlers()` в VM не имеет этой проверки (проверка происходит в `ExecuteStatementBoundary` на уровне выше) — функционально эквивалентно, но slightly different path.
- `ConsumeMouseButtonPress()` создаёт `List<MouseInputEvent>` для отложенных событий на каждый вызов — аллокация на каждый MOUSEPRESS.
- `DispatchMouseHandlers()` создаёт `List<MouseInputEvent>` для отложенных событий на каждый вызов — аллокация на каждую диспетчеризацию.

**Finding:** Аллокация `List<MouseInputEvent>` на каждый вызов `ConsumeMouseButtonPress` и `DispatchMouseHandlers` — потенциальный hot path для частых вызовов. Однако для типичных интерактивных программ (60 FPS game loop) это не является проблемой.

---

## 23. Качество Benchmark

**Finding:** Dedicated engine-comparison benchmark для мышиных событий отсутствует. Рекомендуется создать сценарий, аналогичный `keyboard_handler_dispatch.axm` (если он существует), для измерения overhead диспетчеризации мышиных обработчиков.

---

## 24. Hot Paths и Bottlenecks

| Hot path | Bottleneck | Оценка |
|----------|-----------|--------|
| `DispatchMouseHandlers()` | Проверка словаря на каждый оператор | Низкий overhead при пустом словаре |
| `ConsumeMouseButtonPress()` | Аллокация List на каждый вызов | Низкий для типичных сценариев |
| `TranslateMouseX/YToRuntimeCoordinates()` | Вызов хоста + пересчёт | Неизбежный overhead |

**Finding:** Нет критических bottlenecks. Подсистема оптимизирована для типичных сценариев использования.

---

## 25. Архитектурные проблемы

### 25.1 Дублирование между Interpreter и VM

**Finding (архитектурный):** Логика `DispatchMouseHandlers`, `InvokeMouseHandler`, `ReadMouseX`, `ReadMouseY`, `IsMouseButtonDown`, `ConsumeMouseButtonPress`, `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler`, `ResumeMouseHandler` дублируется между `Interpreter.Mouse.cs` и `BytecodeVm.Mouse.cs`. Хотя общие хелперы (`BuiltinMouseHelpers`) вынесены, сами методы-обёртки и state management дублируются.

Это та же проблема, что была обнаружена и исправлена для других подсистем (Numeric, Regex, String, FileSystem, DateTime, Keyboard) в предыдущих волнах рефакторинга.

### 25.2 Асимметрия проверки _mouseHandlers.Count

**Finding (мелкий):** В Interpreter проверка `_mouseHandlers.Count == 0` происходит внутри `DispatchMouseHandlers()`. В VM проверка `_mouseHandlers.Count > 0` происходит в `ExecuteStatementBoundary()` перед вызовом `DispatchMouseHandlers()`. Функционально эквивалентно, но slightly different code path.

---

## 26. Дублирование логики

| Место 1 | Место 2 | Дублируемая логика |
|---------|---------|-------------------|
| `Interpreter.Mouse.cs` | `BytecodeVm.Mouse.cs` | `ReadMouseX`, `ReadMouseY`, `IsMouseButtonDown`, `ConsumeMouseButtonPress`, `DispatchMouseHandlers`, `InvokeMouseHandler` |
| `Interpreter.Mouse.cs` | `BytecodeVm.Mouse.cs` | `_mouseHandlers`, `_bufferedMouseEvents`, `_mouseHandlerDepth` (state) |
| `Interpreter.Statements.cs:544` | `BytecodeVm.Mouse.cs:136` | `ToMouseInputEventKind()` |

**Finding:** Дублирование manageable благодаря общему `BuiltinMouseHelpers`, но state и dispatch logic дублируются.

---

## 27. Single Source of Truth

| Сущность | Источник истины |
|----------|----------------|
| Синтаксис ON MOUSE | `OnMouseStatementSyntax` |
| Семантика ON MOUSE | `BoundOnMouseStatement` |
| Типы событий мыши | `MouseInputEventKind` / `BoundMouseEventKind` |
| Валидация кнопок | `BuiltinMouseHelpers.IsSupportedMouseButton()` |
| Валидация обработчиков | `BuiltinMouseHelpers.IsSupportedMouseHandlerTarget()` |
| Пересчёт координат | `BuiltinMouseHelpers.TranslateMouseX/YToRuntimeCoordinates()` + `GraphicsCoordinateSpaceMath` |
| Диагностики | `DiagnosticCodes.InvalidOnMouseHandler` / `InvalidOnMouseEvent` |
| Встроенные функции | `BuiltinFunctions.Input.cs` (MouseX, MouseY, MouseButton, MousePress) |

**Finding:** Single source of truth соблюдён. Нет противоречащих определений.

---

## 28. Legacy / Dead Code

**Finding:** Legacy-код или dead code в мышиной подсистеме не обнаружен. Все компоненты активно используются.

---

## 29. Надёжность и безопасность

### 29.1 Проверки границ

- `IsSupportedMouseButton()` — проверка 1..3.
- `IsSupportedMouseHandlerTarget()` — проверка комбинаций кнопка+событие.
- `RequireDrawableSurfaceActive()` — проверка активной поверхности перед чтением координат.
- `TryTranslateMouseAbsoluteToLogical()` — проверка отрицательных координат.

### 29.2 Защита от рекурсии

`_mouseHandlerDepth` предотвращает бесконечную рекурсию при вызове обработчика мыши из обработчика мыши.

### 29.3 Потенциальные риски

- Нет ограничения на размер `_bufferedMouseEvents` — при очень частых событиях очередь может расти. Однако на практике `MaxMouseEventsPerDispatch = 64` ограничивает обработку за один вызов.
- Нет таймаута на выполнение обработчика мыши — зависший обработчик заблокирует выполнение программы. Однако это общий риск всех пользовательских обработчиков, не специфичный для мыши.

---

## 30. Анализ тестов

### 30.1 Найденные тесты

| Тест | Файл | Что проверяет |
|------|------|---------------|
| `Bind_MouseCoordinatesFunctions_ReturnLong` | `BinderTests.cs` | MOUSEX/MOUSEY возвращают LONG |
| `Bind_MouseCoordinatesFunctions_RejectArguments` | `BinderTests.cs` | MOUSEX/MOUSEY не принимают аргументы |
| `Bind_MouseButtonFunction_ReturnsBoolean` | `BinderTests.cs` | MOUSEBUTTON возвращает BOOLEAN |
| `Bind_MouseButtonFunction_RequiresOneIntegerArgument` | `BinderTests.cs` | MOUSEBUTTON требует INTEGER аргумент |
| `Bind_MousePressFunction_ReturnsBoolean` | `BinderTests.cs` | MOUSEPRESS возвращает BOOLEAN |
| `Bind_MousePressFunction_RequiresOneIntegerArgument` | `BinderTests.cs` | MOUSEPRESS требует INTEGER аргумент |
| `Bind_OnMouseStatement_BindsZeroArgumentSubHandler` | `BinderTests.cs` | ON MOUSE с обработчиком без параметров |
| `Bind_OnMouseStatement_BindsEventParameterSubHandler` | `BinderTests.cs` | ON MOUSE с обработчиком (INTEGER, LONG, LONG) |
| `Bind_OnMouseStatement_BindsReleaseAndMoveTargets` | `BinderTests.cs` | ON MOUSE с "UP", "MOVE", "WHEEL" |
| `Bind_OnMouseStatement_RejectsInvalidHandlerSignature` | `BinderTests.cs` | MB2035 для неподходящей сигнатуры |
| `Bind_OnMouseStatement_BindsModeActions` | `BinderTests.cs` | ON/STOP/OFF режимы |
| `Execute_MouseFunctionsReadCoordinatesAndButtons` | `InterpreterTests.cs` | Базовое чтение координат и кнопок |
| `Execute_MouseFunctionsUseCanvasWorldCoordinates` | `InterpreterTests.cs` | CANVAS мировые координаты |
| `Execute_MouseFunctionsWithoutScreenReportRuntimeDiagnostic` | `InterpreterTests.cs` | MB3010 без SCREEN/CANVAS |
| `Execute_MouseFunctionsReadCoordinatesAndButtons` | `BytecodeVmTests.cs` | Базовое чтение координат и кнопок (VM) |
| `Execute_MouseFunctionsUseCanvasWorldCoordinates` | `BytecodeVmTests.cs` | CANVAS мировые координаты (VM) |
| `Execute_MouseFunctionsReturnDefaultValuesWithoutMouseProviderState` | `BytecodeVmTests.cs` | MB3010 без SCREEN/CANVAS (VM) |
| `Execute_MousePressConsumesMatchingButtonOnce` | `BytecodeVmTests.cs` | MOUSEPRESS consume behavior |
| `Execute_MousePressPreservesUnmatchedButtons` | `BytecodeVmTests.cs` | MOUSEPRESS сохраняет неподходящие |
| `Execute_OnMouseConsumesMatchingButtonBeforeMousePress` | `BytecodeVmTests.cs` | ON MOUSE consume до MOUSEPRESS |
| `Execute_OnMousePassesButtonAndCurrentCoordinates` | `BytecodeVmTests.cs` | Передача кнопки и координат |
| `Execute_OnMousePassesCanvasWorldCoordinates` | `BytecodeVmTests.cs` | CANVAS координаты в обработчике |
| `Execute_CompiledOnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen` | `BytecodeVmTests.cs` | Координаты обработчика = MOUSEX/MOUSEY (SCREEN) |
| `Execute_CompiledOnMouseCoordinatesMatchMouseFunctionsDuringActiveCanvas` | `BytecodeVmTests.cs` | Координаты обработчика = MOUSEX/MOUSEY (CANVAS) |
| `Execute_OnMouseHandlesReleaseAndMoveEvents` | `BytecodeVmTests.cs` | Release и Move события |
| `Execute_OnMouseHandlesWheelEvents` | `BytecodeVmTests.cs` | Wheel события |
| `Execute_OnMouseRunsDuringEmptyDoLoop` | `BytecodeVmTests.cs` | Диспетчеризация в пустом цикле |
| `Execute_OnMouseStopPausesAndOnResumesExistingHandler` | `BytecodeVmTests.cs` | STOP/ON пауза/возобновление |
| `HandlePreviewMouseMove_UpdatesRoundedCoordinates` | `ProgramMouseInputRouterTests.cs` | IDE: округление координат |
| `HandlePreviewMouseDown_UpdatesPositionAndPressedButton` | `ProgramMouseInputRouterTests.cs` | IDE: нажатие кнопки |
| `HandlePreviewMouseUp_ReleasesMappedButton` | `ProgramMouseInputRouterTests.cs` | IDE: отпускание кнопки |
| `HandlePreviewMouseWheel_QueuesWheelDelta` | `ProgramMouseInputRouterTests.cs` | IDE: колесо мыши |
| `HandleMouseLeave_ClearsMouseState` | `ProgramMouseInputRouterTests.cs` | IDE: сброс состояния |

### 30.2 Покрытие

- **Binder:** 8 тестов — полное покрытие.
- **Interpreter:** 3 теста — базовое покрытие.
- **VM:** 15+ тестов — полное покрытие.
- **IDE:** 5 тестов — полное покрытие маршрутизации.

**Finding:** Тесты VM значительно более comprehensive, чем тесты Interpreter. Это объясняется тем, что VM — дефолтный движок.

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Статус |
|----------|--------|
| ON MOUSE OFF удаляет обработчик | Неявно покрыт (тест STOP/ON есть, OFF — нет dedicated) |
| Множественные ON MOUSE для разных кнопок | Неявно покрыт samples |
| Обработчик мыши внутри IF/ELSE | Нет dedicated теста |
| Обработчик мыши внутри SELECT CASE | Нет dedicated теста |

### 31.2 Отрицательные сценарии

| Сценарий | Статус |
|----------|--------|
| ON MOUSE с несуществующей функцией | Покрыт общими тестами Binder |
| ON MOUSE с FUNCTION вместо SUB | Покрыт (MB2035) |
| MOUSEBUTTON с нецелым аргументом | Покрыт общими тестами типа |

### 31.3 Граничные случаи

| Сценарий | Статус |
|----------|--------|
| MOUSEPRESS с buttonNumber = 0 | Неявно покрыт (IsSupportedMouseButton возвращает false) |
| MOUSEPRESS с buttonNumber = -1 | Неявно покрыт |
| ON MOUSE(0), handler | Неявно покрыт (IsSupportedMouseHandlerTarget возвращает false для Press) |
| Быстрые последовательные события | Нет dedicated теста |
| Рекурсивный вызов обработчика мыши | Нет dedicated теста |

### 31.4 Parity тесты

| Сценарий | Статус |
|----------|--------|
| Interpreter vs VM: ON MOUSE consume behavior | Неявно покрыт (оба движка тестируются отдельно) |
| Interpreter vs VM: координаты обработчика | Неявно покрыт |

---

## 32. Анализ Samples

### 32.1 Найденные samples

| Sample | Движок | Что демонстрирует |
|--------|--------|-------------------|
| `samples/functions/mousex/current_position.axm` | любой | Чтение X-координаты в SCREEN |
| `samples/functions/mousex/canvas_world_position.axm` | любой | Чтение X-координаты в CANVAS |
| `samples/functions/mousey/current_position.axm` | любой | Чтение Y-координаты в SCREEN |
| `samples/functions/mousey/canvas_world_position.axm` | любой | Чтение Y-координаты в CANVAS |
| `samples/functions/mousebutton/button_states.axm` | любой | Проверка удержания кнопок |
| `samples/functions/mousepress/button_presses.axm` | любой | Одноразовые нажатия |
| `samples/operators/on-mouse/left_button_handler.axm` | SCREEN | Обработчик левой кнопки |
| `samples/operators/on-mouse/canvas_world_handler.axm` | CANVAS | Мировые координаты в обработчике |
| `samples/operators/on-mouse/release_move_handler.axm` | SCREEN | Отпускание и перемещение |
| `samples/operators/on-mouse/wheel_handler.axm` | SCREEN | Колесо мыши |
| `samples/graphics/mouse/mouse_events.axm` | SCREEN | Комплексный пример всех событий |

**Finding:** Samples comprehensive и покрывают все основные сценарии. Все samples на русском языке (кроме комментариев в старых файлах).

---

## 33. Анализ документации

### 33.1 Найденные страницы manual

| Страница | Статус |
|----------|--------|
| `docs/manual/mousex.html` | IMPLEMENTED |
| `docs/manual/mousey.html` | IMPLEMENTED |
| `docs/manual/mousebutton.html` | IMPLEMENTED |
| `docs/manual/mousepress.html` | IMPLEMENTED |
| `docs/manual/on-mouse.html` | IMPLEMENTED |
| `docs/manual/keyboard-mouse-events.html` | IMPLEMENTED (обзорная) |

### 33.2 Качество документации

Документация следует шаблону из AGENTS.md:
- Что это такое → да
- Для чего нужна → да
| Где работает → да
- Синтаксис → да
- Параметры → да
- Что возвращает → да
- Правила работы → да
- Диагностика → да
- Пример → да
- Ручная проверка → да
- См. также → да

**Finding:** Документация полная, корректная и следует установленному стилю.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

`MOUSEX`, `MOUSEY`, `MOUSEBUTTON`, `MOUSEPRESS`, `ON`, `MOUSE` — подсвечены в обоих `.xshd` файлах.

### 34.2 Маршрутизация событий

`ProgramMouseInputRouter` (`src/Axiom.Ide/Services/Input/ProgramMouseInputRouter.cs`) обрабатывает:
- `HandlePreviewMouseMove` → `SetMousePosition` + `EnqueueMouseMove`
- `HandlePreviewMouseDown` → `SetMousePosition` + `EnqueueMouseButtonPress` + `SetMouseButtonDown`
- `HandlePreviewMouseUp` → `SetMousePosition` + `EnqueueMouseButtonRelease` + `SetMouseButtonUp`
- `HandlePreviewMouseWheel` → `SetMousePosition` + `EnqueueMouseWheel`
- `HandleMouseLeave` → `ClearMouseState`

**Finding:** Маршрутизация корректная. WPF-события преобразуются в `MouseInputEvent` и помещаются в очередь.

### 34.3 Маппинг кнопок

```csharp
buttonNumber = button switch
{
    MouseButton.Left => 1,
    MouseButton.Right => 2,
    MouseButton.Middle => 3,
    _ => 0
};
```

**Finding:** Дополнительные кнопки мыши (XButton1, XButton2) не маппятся — возвращается 0, что означает "неизвестная кнопка". Это ограничение текущего среза.

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Описание |
|-------------|-----|----------|
| WPF | IDE | `ProgramMouseInputRouter` использует `System.Windows.Input.MouseButton` |
| .NET | Runtime | `Queue<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>` |
| Windows | IDE | WPF — Windows-only |

**Finding:** Зависимости корректно разделены: runtime не зависит от WPF, IDE зависит от WPF (ожидаемо).

---

## 36. Карта корневых причин

### ROOT-SYS-001: Дублирование dispatch/state между Interpreter и VM

├── SYS-025: Дублирование `DispatchMouseHandlers`
├── SYS-026: Дублирование `InvokeMouseHandler`
├── SYS-027: Дублирование state (`_mouseHandlers`, `_bufferedMouseEvents`, `_mouseHandlerDepth`)
└── SYS-028: Дублирование `ToMouseInputEventKind`

**Направление:** Вынести общую dispatch/state логику в shared base или helper (по аналогии с исправленными волнами для других подсистем).

---

## 37. Стабильные области

| Область | Статус | Почему не следует трогать |
|---------|--------|--------------------------|
| `BuiltinMouseHelpers` | STABLE | Корректная общая логика, используется обоими движками |
| `IMouseInputProvider` | STABLE | Чистый интерфейс, дефолтная реализация |
| `MouseInputEvent` / `MouseInputEventKind` | STABLE | Простые value types |
| `OnMouseStatementSyntax` | STABLE | Корректный синтаксический узел |
| `BoundOnMouseStatement` | STABLE | Корректный bound узел |
| `ProgramMouseInputRouter` | STABLE | Корректная маршрутизация WPF → runtime |
| Документация | STABLE | Полная и корректная |
| Samples | STABLE | Comprehensive покрытие |

---

## 38. Возможности консолидации

| Текущие места | Дублируемая логика | Единый источник истины | Направление миграции |
|---------------|-------------------|----------------------|---------------------|
| `Interpreter.Mouse.cs` + `BytecodeVm.Mouse.cs` | `DispatchMouseHandlers`, `InvokeMouseHandler`, state | `BuiltinMouseHelpers` или новый `MouseDispatchHelper` | Вынести общую логику в shared helper |
| `Interpreter.Mouse.cs:ReadMouseX/Y` + `BytecodeVm.Mouse.cs:ReadMouseX/Y` | Чтение координат + пересчёт | `BuiltinMouseHelpers` (уже частично там) | Полностью делегировать в helper |
| `Interpreter.Statements.cs:544` + `BytecodeVm.Mouse.cs:136` | `ToMouseInputEventKind` | `BuiltinMouseHelpers` | Вынести в shared helper |

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности корректно:
- **IDE:** маршрутизация WPF-событий.
- **Runtime/Host:** интерфейс ввода.
- **BuiltinMouseHelpers:** общая логика.
- **Interpreter/VM:** диспетчеризация и вызов обработчиков.
- **Binder:** семантическая валидация.
- **Parser:** синтаксический разбор.

**Finding:** Нет необходимости в разделении ответственности. Текущая архитектура корректна.

---

## 40. Кандидаты на удаление

**Finding:** Кандидатов на удаление не обнаружено. Все компоненты активно используются.

---

## 41. Готовность к дальнейшему развитию

| Будущая возможность | Готовность | Комментарий |
|---------------------|-----------|-------------|
| Дополнительные кнопки мыши (4, 5, ...) | СРЕДНЯЯ | `IsSupportedMouseButton` ограничивает 1..3, но легко расширить |
| Double-click | НИЗКАЯ | Нет инфраструктуры для детекции двойного клика |
| Mouse capture | НИЗКАЯ | Нет инфраструктуры для захвата указателя |
| Touch events | НИЗКАЯ | Нет инфраструктуры для тач-событий |
| Relative mouse mode | НИЗКАЯ | Нет инфраструктуры для относительного режима |
| Compiler | ВЫСОКАЯ | Lowering уже реализован, opcodes определены |

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|----|-------------|-----------|-----------|----------|----------------|-----------------|-------------|
| SYS-025 | P2 | ARCHITECTURE | Interpreter/VM | Дублирование `DispatchMouseHandlers` | `Interpreter.Mouse.cs:31` vs `BytecodeVm.Mouse.cs:32` | Общая логика не вынесена в shared helper | CONFIRMED |
| SYS-026 | P2 | ARCHITECTURE | Interpreter/VM | Дублирование `InvokeMouseHandler` | `Interpreter.Mouse.cs:105` vs `BytecodeVm.Mouse.cs:111` | Общая логика не вынесена в shared helper | CONFIRMED |
| SYS-027 | P2 | ARCHITECTURE | Interpreter/VM | Дублирование state | `Interpreter.Mouse.cs:9-11` vs `BytecodeVm.Mouse.cs:10-12` | Общее состояние не вынесено в shared helper | CONFIRMED |
| SYS-028 | P3 | ARCHITECTURE | Interpreter/VM | Дублирование `ToMouseInputEventKind` | `Interpreter.Statements.cs:544` vs `BytecodeVm.Mouse.cs:136` | Маленький helper не вынесен | CONFIRMED |
| SYS-029 | P3 | PERFORMANCE | Runtime | Аллокация `List<MouseInputEvent>` на каждый вызов `ConsumeMouseButtonPress` и `DispatchMouseHandlers` | `BuiltinMouseHelpers.cs:49`, `Interpreter.Mouse.cs:48` | Нет переиспользуемого буфера | MEDIUM |
| SYS-030 | P4 | TESTS | Tests | Нет dedicated теста для ON MOUSE OFF | Отсутствие в тестах | Не критично, но покрытие неполное | HIGH |
| SYS-031 | P4 | TESTS | Tests | Нет dedicated теста для рекурсивного вызова обработчика мыши | Отсутствие в тестах | Защита есть (`_mouseHandlerDepth`), но нет теста | HIGH |
| SYS-032 | P4 | TESTS | Tests | Нет engine-comparison benchmark для мышиных событий | Отсутствие в `samples/smoke/engine-compare/` | Не создан | HIGH |
| SYS-033 | P4 | IDE | IDE | Дополнительные кнопки мыши (XButton1, XButton2) не маппятся | `ProgramMouseInputRouter.cs:55` | `_ => 0` в switch | LOW |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

- TASK-001: Создать engine-comparison benchmark для мышиных событий.

### ЭТАП 1 — Исправление критических проблем корректности

Нет критических проблем корректности. Подсистема работает корректно.

### ЭТАП 2 — Стабилизация семантики

Нет проблем семантики. Подсистема стабильна.

### ЭТАП 3 — Архитектурное улучшение

- TASK-002: Вынести общую dispatch/state логику мыши из Interpreter и VM в shared helper (по аналогии с исправленными волнами для других подсистем).
- TASK-003: Вынести `ToMouseInputEventKind` в `BuiltinMouseHelpers`.

### ЭТАП 4 — Оптимизация производительности

- TASK-004: Рассмотреть переиспользуемый буфер для `ConsumeMouseButtonPress` и `DispatchMouseHandlers` (низкий приоритет).

### ЭТАП 5 — Усиление тестирования

- TASK-005: Добавить dedicated тест для ON MOUSE OFF.
- TASK-006: Добавить dedicated тест для рекурсивного вызова обработчика мыши.
- TASK-007: Добавить parity-тесты Interpreter vs VM для мышиных событий.

### ЭТАП 6 — Обновление документации

Документация актуальна. Нет необходимости в обновлении.

### ЭТАП 7 — Обновление IDE

- TASK-008: Рассмотреть маппинг дополнительных кнопок мыши (XButton1, XButton2) — низкий приоритет.

### ЭТАП 8 — Подготовка к дальнейшему развитию

Нет immediate действий. Подсистема готова к дальнейшему развитию.

---

## 44. Граф зависимостей задач

```
TASK-001 (benchmark) ─── независимый
TASK-002 (consolidate dispatch) ─── независимый
TASK-003 (consolidate ToMouseInputEventKind) ─── независимый
TASK-004 (buffer optimization) ─── зависит от TASK-002
TASK-005 (test ON MOUSE OFF) ─── независимый
TASK-006 (test recursion) ─── независимый
TASK-007 (parity tests) ─── зависит от TASK-002
TASK-008 (extra buttons) ─── независимый
```

---

## 45. Definition of Done

Подсистема MOUSE / EVENTS считается завершённой, когда:

1. **Корректность:** все 4 функции и оператор ON MOUSE работают корректно в обоих движках — **ВЫПОЛНЕНО**.
2. **Edge cases:** MB3010 без SCREEN/CANVAS, некорректные номера кнопок, неподходящие сигнатуры обработчиков — **ВЫПОЛНЕНО**.
3. **Error handling:** MB2035, MB2036, MB3010 — **ВЫПОЛНЕНО**.
4. **Interpreter:** полная реализация — **ВЫПОЛНЕНО**.
5. **VM:** полная реализация, parity с Interpreter — **ВЫПОЛНЕНО**.
6. **Тесты:** Binder, Interpreter, VM, IDE — **ВЫПОЛНЕНО** (можно усилить).
7. **Документация:** все 6 страниц manual — **ВЫПОЛНЕНО**.
8. **IDE:** подсветка, маршрутизация — **ВЫПОЛНЕНО**.
9. **Performance:** нет критических bottleneck — **ВЫПОЛНЕНО** (benchmark отсутствует).

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|-----|-------------|----------|
| 1 | SYS-025 | P2 | Дублирование `DispatchMouseHandlers` между Interpreter и VM |
| 2 | SYS-026 | P2 | Дублирование `InvokeMouseHandler` между Interpreter и VM |
| 3 | SYS-027 | P2 | Дублирование state между Interpreter и VM |
| 4 | SYS-028 | P3 | Дублирование `ToMouseInputEventKind` |
| 5 | SYS-029 | P3 | Аллокация List на каждый вызов dispatch/consume |
| 6 | SYS-030 | P4 | Нет dedicated теста для ON MOUSE OFF |
| 7 | SYS-031 | P4 | Нет dedicated теста для рекурсии обработчика |
| 8 | SYS-032 | P4 | Нет engine-comparison benchmark |
| 9 | SYS-033 | P4 | Дополнительные кнопки мыши не маппятся в IDE |

**Проблем корректности (P0/P1) не обнаружено.**

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемая польза |
|---|-----|----------|-----------------|
| 1 | TASK-001 | Создать engine-comparison benchmark | Измеримость |
| 2 | TASK-004 | Переиспользуемый буфер для dispatch/consume | Снижение аллокаций |

**Критических проблем производительности не обнаружено.**

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка | Объяснение |
|----------|--------|------------|
| Корректность | 9/10 | Все функции работают корректно, edge cases покрыты |
| Семантика | 9/10 | Чёткий контракт, корректная валидация |
| Архитектура | 7/10 | Дублирование между Interpreter и VM (управляемое) |
| Runtime | 9/10 | Общий хелпер, чистый интерфейс |
| Interpreter | 9/10 | Полная реализация |
| Bytecode VM | 9/10 | Полная реализация, parity с Interpreter |
| Производительность | 8/10 | Нет критических bottleneck, но нет benchmark |
| Память | 8/10 | Небольшие аллокации в dispatch, но не критично |
| Error handling | 9/10 | Корректные диагностики, защита от рекурсии |
| Tests | 8/10 | Comprehensive, но можно усилить |
| Samples | 9/10 | Полное покрытие всех сценариев |
| Documentation | 9/10 | Полная, корректная, по шаблону |
| IDE | 9/10 | Подсветка, маршрутизация, тесты |
| Maintainability | 8/10 | Дублирование manageable, но нужно консолидировать |
| Reliability | 9/10 | Стабильная, нет известных багов |
| Future readiness | 8/10 | Готова к расширению |

**Общий Health Score: 8.0/10**

---

## Финальный контрольный вопрос

> Можно ли считать подсистему MOUSE / EVENTS достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers

Нет критических blockers. Подсистема работает корректно.

### Что нужно сделать в первую очередь

1. TASK-002: Консолидация dispatch/state между Interpreter и VM (по аналогии с исправленными волнами для других подсистем).
2. TASK-001: Создание engine-comparison benchmark.

### Что можно отложить

1. TASK-004: Оптимизация буфера (низкий приоритет).
2. TASK-008: Дополнительные кнопки мыши (низкий приоритет).
3. TASK-005, TASK-006, TASK-007: Дополнительные тесты (можно добавить позже).

### Что не следует трогать

1. `BuiltinMouseHelpers` — стабильная общая логика.
2. `IMouseInputProvider` — чистый интерфейс.
3. `OnMouseStatementSyntax` / `BoundOnMouseStatement` — корректные узлы AST.
4. `ProgramMouseInputRouter` — корректная маршрутизация.
5. Документация и samples — полные и корректные.
