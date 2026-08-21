# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## KEYBOARD / INKEY

**Дата:** 2026-08-20
**Метод:** полный forensic-анализ кода, тестов, samples, документации, bytecode coverage

---

## 1. Резюме

Подсистема KEYBOARD / INKEY обеспечивает клавиатурный ввод в языке Axiom через четыре пользовательских сущности: функции `INKEY()`, `KEYDOWN(keyText)`, `KEYPRESS(keyText)` и оператор `ON KEY(keyText), handler`. Подсистема реализована на всех уровнях конвейера: лексер, парсер, семантика, интерпретатор, Bytecode VM, IDE, тесты, samples и документация.

**Общая оценка: подсистема стабильна, корректна и архитектурно зрелая.** Основной технический долг — дублирование логики между Interpreter и BytecodeVm (по аналогии с уже исправленными волнами Numeric, Regex, String, FileSystem, DateTime). Второй существенный finding — отсутствие dedicated engine-comparison benchmark для клавиатурного ввода.

**Health Score: 7.8/10**

---

## 2. Что представляет собой эта подсистема

Подсистема KEYBOARD / INKEY отвечает за:

- неблокирующее чтение клавиши из очереди (`INKEY`);
- проверку текущего удержания клавиши (`KEYDOWN`);
- одноразовую проверку события нажатия (`KEYPRESS`);
- регистрацию, паузу, возобновление и отключение событийных обработчиков клавиатуры (`ON KEY`);
- нормализацию имён клавиш, алиасов и аккордов;
- маршрутизацию клавиатурных событий из IDE в runtime.

Подсистема не включает мышиный ввод (`MOUSEX`, `MOUSEY`, `MOUSEBUTTON`, `MOUSEPRESS`, `ON MOUSE`), хотя разделяет с ним инфраструктуру диспетчеризации на границах операторов.

---

## 3. Полный список возможностей

### SYS-001: INKEY()

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `INKEY()` |
| Назначение | Неблокирующее чтение одной клавиши из очереди |
| Возвращаемый тип | STRING |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.InKey` |
| Binder | `BoundCallExpression`, 0 аргументов |
| Interpreter | `ReadKeyboardInput()` → `TryReadKeyboardKey()` |
| Bytecode VM | `BuiltinExecution.NullaryNoLocation` → `ReadKeyboardInput()` |
| Opcode | `CallBuiltin` (общий путь) |
| IDE | Подсветка `INKEY` в обоих `.xshd` |
| Tests | Parser, Binder, Interpreter, VM, ExecutionPipeline, Acceptance |
| Samples | `samples/functions/inkey/poll_keys.axm` |
| Manual | `docs/manual/inkey.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-002: KEYDOWN(keyText)

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `KEYDOWN(keyText)` |
| Назначение | Проверка текущего удержания клавиши или аккорда |
| Возвращаемый тип | BOOLEAN |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.KeyDown` |
| Binder | `BoundCallExpression`, 1 аргумент STRING |
| Interpreter | `IsKeyboardKeyDown()` → `IsChordDown()` |
| Bytecode VM | `BuiltinExecution.UnaryNoLocation` → `IsKeyboardKeyDown()` |
| Opcode | `CallBuiltin` (общий путь) |
| IDE | Подсветка `KEYDOWN` в обоих `.xshd` |
| Tests | Parser, Binder, Interpreter, VM, ExecutionPipeline, Acceptance |
| Samples | `samples/functions/keydown/held_modifiers.axm`, `canonical_aliases.axm` |
| Manual | `docs/manual/keydown.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-003: KEYPRESS(keyText)

| Поле | Значение |
|------|----------|
| Категория | Built-in Function |
| Тип | FUNCTION |
| Пользовательский синтаксис | `KEYPRESS(keyText)` |
| Назначение | Одноразовая проверка события нажатия клавиши (consumes event) |
| Возвращаемый тип | BOOLEAN |
| Parser | `CallExpressionSyntax` с `BuiltinFunctionId.KeyPress` |
| Binder | `BoundCallExpression`, 1 аргумент STRING |
| Interpreter | `ConsumeKeyboardKeyPress()` → `TryConsumeKeyboardKeyPress()` |
| Bytecode VM | `BuiltinExecution.UnaryNoLocation` → `ConsumeKeyboardKeyPress()` |
| Opcode | `CallBuiltin` (общий путь) |
| IDE | Подсветка `KEYPRESS` в обоих `.xshd` |
| Tests | Parser, Binder, Interpreter, VM, ExecutionPipeline, Acceptance |
| Samples | `samples/functions/keypress/single_press.axm`, `modified_keypress.axm` |
| Manual | `docs/manual/keypress.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-004: ON KEY(keyText), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON KEY(keyText), HandlerName` |
| Назначение | Регистрация обработчика клавиатурного события |
| Parser | `OnKeyStatementSyntax` |
| Binder | `BoundOnKeyStatement` с `BoundOnKeyActionKind.Register` |
| Interpreter | `ExecuteOnKey()` → `RegisterKeyboardHandler()` |
| Bytecode Lowering | `LowerOnKey()` → `RegisterKeyHandler` |
| Bytecode VM | `ExecuteRegisterKeyHandler()` |
| Opcode | `RegisterKeyHandler` |
| IDE | Подсветка `ON`, `KEY` |
| Tests | Parser, Binder, Interpreter, VM, ExecutionPipeline, Acceptance |
| Samples | `samples/operators/on-key/left_handler.axm` |
| Manual | `docs/manual/on-key.html` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-005: ON KEY(keyText), OFF

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON KEY(keyText), OFF` |
| Назначение | Полное отключение обработчика |
| Parser | `OnKeyStatementSyntax` с `DisablesHandler = true` |
| Binder | `BoundOnKeyStatement` с `BoundOnKeyActionKind.Unregister` |
| Interpreter | `UnregisterKeyboardHandler()` |
| Bytecode VM | `ExecuteUnregisterKeyHandler()` |
| Opcode | `UnregisterKeyHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-006: ON KEY(keyText), STOP

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON KEY(keyText), STOP` |
| Назначение | Пауза обработчика без удаления |
| Parser | `OnKeyStatementSyntax` с `PausesHandler = true` |
| Binder | `BoundOnKeyStatement` с `BoundOnKeyActionKind.Pause` |
| Interpreter | `PauseKeyboardHandler()` |
| Bytecode VM | `ExecutePauseKeyHandler()` |
| Opcode | `PauseKeyHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-007: ON KEY(keyText), ON

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON KEY(keyText), ON` |
| Назначение | Возобновление ранее зарегистрированного обработчика |
| Parser | `OnKeyStatementSyntax` с `ResumesHandler = true` |
| Binder | `BoundOnKeyStatement` с `BoundOnKeyActionKind.Resume` |
| Interpreter | `ResumeKeyboardHandler()` |
| Bytecode VM | `ExecuteResumeKeyHandler()` |
| Opcode | `ResumeKeyHandler` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-008: ON KEY("ANY"), handler

| Поле | Значение |
|------|----------|
| Категория | Statement |
| Тип | STATEMENT |
| Пользовательский синтаксис | `ON KEY("ANY"), HandlerName` |
| Назначение | Обработчик для любой клавиши без точного обработчика |
| Реализация | Специальный случай в `TryGetKeyboardHandler()` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-009: KeyboardInputNormalizer

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Нормализация имён клавиш, алиасов, аккордов |
| Файл | `src/Axiom.Runtime/Execution/KeyboardInputNormalizer.cs` |
| Класс | `KeyboardInputNormalizer` (static) |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-010: IKeyboardInputProvider

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Хост-интерфейс для клавиатурного ввода |
| Файл | `src/Axiom.Runtime/Host/IKeyboardInputProvider.cs` |
| Методы | `ReadKey()`, `IsKeyDown(string)` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-011: KeyboardInputChord

| Поле | Значение |
|------|----------|
| Категория | Runtime Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Представление аккорда клавиш (primary + required) |
| Файл | `src/Axiom.Runtime/Execution/KeyboardInputNormalizer.cs` |
| Структура | `record struct KeyboardInputChord(CanonicalText, PrimaryKey, RequiredKeys)` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-012: ProgramKeyboardInputRouter

| Поле | Значение |
|------|----------|
| Категория | IDE Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Маршрутизация WPF-событий клавиатуры в ProgramConsoleService |
| Файл | `src/Axiom.Ide/Services/Input/ProgramKeyboardInputRouter.cs` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-013: KeyboardInputNameMapper

| Поле | Значение |
|------|----------|
| Категория | IDE Facility |
| Тип | RUNTIME FACILITY |
| Назначение | Преобразование WPF Key → Axiom имя клавиши |
| Файл | `src/Axiom.Ide/Services/Input/KeyboardInputNameMapper.cs` |
| Статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

---

## 4. Фактическая архитектура

### 4.1 Слои подсистемы

```
┌─────────────────────────────────────────────────────────────┐
│  IDE (WPF)                                                  │
│  ┌──────────────────────┐  ┌─────────────────────────────┐  │
│  │ ProgramKeyboardInput │  │ KeyboardInputNameMapper     │  │
│  │ Router               │  │ (WPF Key → Axiom key name)  │  │
│  └──────────┬───────────┘  └─────────────────────────────┘  │
│             │                                               │
│  ┌──────────▼───────────┐                                   │
│  │ ProgramConsoleService│                                   │
│  │ (IKeyboardInput-     │                                   │
│  │  Provider)           │                                   │
│  └──────────┬───────────┘                                   │
│             │                                               │
│  ┌──────────▼───────────┐                                   │
│  │ InputStateService    │                                   │
│  │ ┌─────────────────┐  │                                   │
│  │ │RuntimeInput-    │  │                                   │
│  │ │Snapshot         │  │                                   │
│  │ │(Queue + HashSet)│  │                                   │
│  │ └─────────────────┘  │                                   │
│  └──────────────────────┘                                   │
└─────────────────────────────────────────────────────────────┘
            │
            │ IKeyboardInputProvider.ReadKey() / IsKeyDown()
            ▼
┌─────────────────────────────────────────────────────────────┐
│  Runtime                                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ KeyboardInputNormalizer (shared)                     │   │
│  │ - Normalize(keyText) → canonical name                │   │
│  │ - NormalizeChord(keyText) → KeyboardInputChord       │   │
│  │ - CanonicalNamedKeys[28] + aliases                   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌──────────────────────┐  ┌─────────────────────────────┐  │
│  │ Interpreter          │  │ BytecodeVm                  │  │
│  │ .Keyboard.cs         │  │ .Keyboard.cs                │  │
│  │ - _keyboardHandlers  │  │ - _keyboardHandlers         │  │
│  │ - _bufferedKeys      │  │ - _bufferedKeys             │  │
│  │ - _keyboardHandler-  │  │ - _keyboardHandlerDepth     │  │
│  │   Depth              │  │                             │  │
│  │ - DispatchKeyboard-  │  │ - ExecuteStatementBoundary()│  │
│  │   Handlers()         │  │ - ExecuteRegisterKeyHandler │  │
│  │ - TryGetKeyboard-    │  │ - ExecuteUnregisterKey-     │  │
│  │   Handler()          │  │   Handler()                 │  │
│  │ - RegisterKeyboard-  │  │ - ExecutePauseKeyHandler()  │  │
│  │   Handler()          │  │ - ExecuteResumeKeyHandler() │  │
│  │ - InvokeKeyboard-    │  │ - InvokeKeyboardHandler()   │  │
│  │   Handler()          │  │                             │  │
│  └──────────────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│  Semantics (Binder)                                         │
│  - BuiltinFunctions.Input.cs: INKEY, KEYDOWN, KEYPRESS     │
│  - Binder.IoStatements.cs: BindOnKeyStatement()             │
│  - BoundOnKeyStatement + BoundOnKeyActionKind               │
└─────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│  Parsing                                                    │
│  - Parser.Statements.cs: ParseOnKeyStatement()              │
│  - OnKeyStatementSyntax (Axiom.Syntax/Statements.cs)        │
└─────────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│  Lexing                                                     │
│  - TokenKind.OnKeyword, TokenKind.KeyKeyword                │
│  - Lexer.cs: ["ON"] = OnKeyword, ["KEY"] = KeyKeyword       │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Файловая карта

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Lexing/Lexer.cs` | Токенизация `ON`, `KEY` |
| `src/Axiom.Lexing/TokenKind.cs` | `OnKeyword`, `KeyKeyword` |
| `src/Axiom.Syntax/Statements.cs` | `OnKeyStatementSyntax` |
| `src/Axiom.Parsing/Parser.Statements.cs` | `ParseOnKeyStatement()` |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctionId.cs` | `InKey`, `KeyDown`, `KeyPress` |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Input.cs` | Дескрипторы INKEY, KEYDOWN, KEYPRESS |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` | `BindOnKeyStatement()` |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | `BoundOnKeyStatement`, `BoundOnKeyActionKind` |
| `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs` | `OnKeyStatement` |
| `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs` | `ReportInvalidOnKeyHandler()` |
| `src/Axiom.Core/DiagnosticCodes.cs` | `MB2034` (InvalidOnKeyHandler) |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | `LowerOnKey()` |
| `src/Axiom.Bytecode/Model/Opcode.cs` | `RegisterKeyHandler`, `UnregisterKeyHandler`, `PauseKeyHandler`, `ResumeKeyHandler` |
| `src/Axiom.Bytecode/Model/LoweredInstruction.cs` | Factory methods for key handler instructions |
| `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs` | `RegisterKeyHandler` и др. |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | Эмиссия key handler instructions |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` | Валидация key handler instructions |
| `src/Axiom.Runtime/Execution/KeyboardInputNormalizer.cs` | Нормализация, алиасы, аккорды |
| `src/Axiom.Runtime/Host/IKeyboardInputProvider.cs` | Хост-интерфейс |
| `src/Axiom.Runtime/Host/RuntimeInputSnapshot.cs` | Снимок состояния ввода |
| `src/Axiom.Runtime/Execution/Interpreter.Keyboard.cs` | Keyboard logic для Interpreter |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | Dispatch INKEY/KEYDOWN/KEYPRESS |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` | `ExecuteOnKey()`, `DispatchInputHandlers()` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs` | Keyboard logic для BytecodeVm |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` | Dispatch INKEY/KEYDOWN/KEYPRESS |
| `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` | Fast-path dispatch |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | Opcode dispatch (RegisterKeyHandler и др.) |
| `src/Axiom.Ide/Services/Input/ProgramKeyboardInputRouter.cs` | WPF → ProgramConsoleService |
| `src/Axiom.Ide/Services/Input/KeyboardInputNameMapper.cs` | WPF Key → Axiom key name |
| `src/Axiom.Ide/Services/Input/InputStateService.cs` | Управление состоянием ввода |
| `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs` | IKeyboardInputProvider implementation |
| `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd` | Подсветка INKEY, KEYDOWN, KEYPRESS |
| `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd` | Подсветка INKEY, KEYDOWN, KEYPRESS |

---

## 5. Полный путь выполнения

### 5.1 Путь INKEY()

```
Исходный код: PRINT INKEY()
    ↓
Lexer: [PRINT] [INKEY] [(] [)]
    ↓
Parser: PrintStatementSyntax
        └── CallExpressionSyntax(NameExpressionSyntax("INKEY"), [])
    ↓
Binder: BoundPrintStatement
        └── BoundCallExpression(BuiltinFunctionId.InKey, [])
    ↓
Interpreter путь:
    ExecutePrint → EvaluateExpression(BoundCallExpression)
        → ExecuteBuiltinCall(InKey, [])
            → ReadKeyboardInput()
                → TryReadKeyboardKey()
                    → _bufferedKeys.Dequeue() или _host.ReadKey()
        → RuntimeValue.FromString(keyText)
    ↓
Bytecode VM путь:
    Lowering: CallBuiltin(InKey, 0)
    Emission: BytecodeInstruction(Opcode.CallBuiltin, BuiltinFunctionId.InKey)
    VM: ExecuteCallBuiltin → BuiltinDispatch[InKey]
        → NullaryNoLocation(vm => RuntimeValue.FromString(vm.ReadKeyboardInput()))
            → ReadKeyboardInput()
                → TryReadKeyboardKey()
                    → _bufferedKeys.Dequeue() или _host.ReadKey()
    ↓
Host: IKeyboardInputProvider.ReadKey()
    → RuntimeInputSnapshot.TryReadKey()
        → _pendingKeys.Dequeue()
    ↓
Результат: строка клавиши или ""
```

### 5.2 Путь ON KEY("LEFT"), HandleLeft

```
Исходный код: ON KEY("LEFT"), HandleLeft
    ↓
Lexer: [ON] [KEY] [(] ["LEFT"] [,] [HandleLeft] [)]
    ↓
Parser: OnKeyStatementSyntax
        ├── KeyExpression: LiteralExpressionSyntax("LEFT")
        └── ActionToken: IdentifierToken("HandleLeft")
    ↓
Binder: BoundOnKeyStatement
        ├── KeyExpression: BoundLiteralExpression("LEFT")
        ├── HandlerExpression: BoundFunctionExpression(FunctionSymbol(HandleLeft))
        ├── ActionKind: Register
        └── HandlerReceivesKeyText: false (SUB без параметров)
    ↓
Interpreter путь:
    ExecuteOnKey(BoundOnKeyStatement)
        → EvaluateExpression(KeyExpression) → "LEFT"
        → EvaluateExpression(HandlerExpression) → RuntimeFunctionValue
        → RegisterKeyboardHandler("LEFT", handler, passKeyText: false)
            → KeyboardInputNormalizer.NormalizeChord("LEFT")
            → _keyboardHandlers["LEFT"] = KeyboardHandlerRegistration(...)
    ↓
Bytecode VM путь:
    Lowering: LowerOnKey(BoundOnKeyStatement)
        → Push "LEFT"
        → Push handler
        → RegisterKeyHandler(location, handlerReceivesKeyText: false)
    Emission: BytecodeInstruction(Opcode.RegisterKeyHandler, operandCount: 0)
    VM: ExecuteRegisterKeyHandler(frame, instructionIndex, instruction)
        → PopValue().AsFunction() → handler
        → PopValue().AsString() → "LEFT"
        → KeyboardInputNormalizer.NormalizeChord("LEFT")
        → _keyboardHandlers["LEFT"] = KeyboardHandlerRegistration(...)
    ↓
Диспетчеризация (на границе следующего оператора):
    Interpreter: DispatchInputHandlers(location)
        → DispatchKeyboardHandlers(location)
            → TryReadKeyboardKey() → "LEFT"
            → KeyboardInputNormalizer.Normalize("LEFT") → "LEFT"
            → TryGetKeyboardHandler("LEFT") → registration found
            → InvokeKeyboardHandler(registration, "LEFT", location)
                → InvokeUserRoutine(handler, [], location)
    ↓
    BytecodeVm: ExecuteStatementBoundary(instructionIndex)
        → TryReadKeyboardKey() → "LEFT"
        → KeyboardInputNormalizer.Normalize("LEFT") → "LEFT"
        → TryGetKeyboardHandler("LEFT") → registration found
        → InvokeKeyboardHandler(registration, "LEFT", location)
            → InvokeBytecodeFunction(handler, [], location)
```

---

## 6. Фактический семантический контракт

### 6.1 INKEY()

| Параметр | Значение |
|----------|----------|
| Вход | Нет аргументов |
| Обработка | Чтение из буфера клавиш (_bufferedKeys) или из host.ReadKey() |
| Выход | STRING — символ клавиши, каноническое имя специальной клавиши или "" |
| Побочные эффекты | Удаляет прочитанную клавишу из очереди |
| Ошибки | MB2004 при наличии аргументов |
| Тип возврата | Всегда STRING |

### 6.2 KEYDOWN(keyText)

| Параметр | Значение |
|----------|----------|
| Вход | keyText: STRING — имя клавиши или аккорд |
| Обработка | Нормализация → проверка удержания через host.IsKeyDown() |
| Выход | BOOLEAN — TRUE если клавиша удерживается |
| Побочные эффекты | Не читает очередь, не удаляет события |
| Ошибки | MB2004 при отсутствии/лишних аргументах, MB2003 при не-STRING |
| Аккорды | "CTRL+LEFT" — все части должны быть удержаны |

### 6.3 KEYPRESS(keyText)

| Параметр | Значение |
|----------|----------|
| Вход | keyText: STRING — имя клавиши или аккорд |
| Обработка | Нормализация → поиск в очереди → удаление совпавшего события |
| Выход | BOOLEAN — TRUE если подходящее нажатие найдено и удалено |
| Побочные эффекты | Удаляет совпавшее событие из очереди; неподходящие клавиши остаются |
| Ошибки | MB2004 при отсутствии/лишних аргументах, MB2003 при не-STRING |
| Аккорды | "CTRL+LEFT" — CTRL удержан, LEFT пришло событием |

### 6.4 ON KEY(keyText), handler

| Параметр | Значение |
|----------|----------|
| Вход | keyText: STRING, handler: SUB без параметров или SUB(keyName AS STRING) |
| Обработка | Нормализация → регистрация в _keyboardHandlers |
| Выход | Нет |
| Побочные эффекты | Добавляет/заменяет обработчик в словаре |
| Ошибки | MB2034 если handler не SUB или имеет неподходящие параметры |
| Диспетчеризация | На границе оператора (StatementBoundary) |
| Reentrancy | _keyboardHandlerDepth предотвращает рекурсивный вход |

---

## 7. Анализ Parser

### 7.1 Синтаксический узел

`OnKeyStatementSyntax` (src/Axiom.Syntax/Statements.cs:320) содержит:
- `OnKeyword`, `KeyKeyword` — токены `ON`, `KEY`
- `OpenParenToken`, `CloseParenToken` — скобки
- `KeyExpression` — выражение имени клавиши
- `CommaToken` — запятая перед action
- `ActionToken` — токен обработчика или режима (ON/STOP/OFF/identifier)

### 7.2 Парсинг

`ParseOnKeyStatement()` (src/Axiom.Parsing/Parser.Statements.cs:1024) корректно:
- Разбирает `ON KEY(expr), handler`
- Определяет режим через `ActionToken.Kind`: `OnKeyword` → ResumesHandler, `OffKeyword` → DisablesHandler, `StopKeyword` → PausesHandler
- Для регистрации принимает идентификатор (имя SUB)

### 7.3 Вход в ON statement

`ParseOnStatement()` (строка 1015) определяет подтип по `Peek(1).Kind`:
- `MouseKeyword` → `ParseOnMouseStatement()`
- Иначе → `ParseOnKeyStatement()`

**Замечание:** Парсер не проверяет `SyntaxKind.KeyKeyword` явно — любое `ON` без `MOUSE` парсится как `ON KEY`. Это работает корректно, поскольку `ON` без `KEY`/`MOUSE` не имеет смысла в текущем языке, но может стать проблемой при расширении `ON` для других событий.

---

## 8. Анализ Binder

### 8.1 BindOnKeyStatement()

`BindOnKeyStatement()` (src/Axiom.Semantics/Binding/Binder.IoStatements.cs:578):

1. Связывает `KeyExpression` с ожидаемым типом `TypeSymbol.String`
2. Для режимов OFF/STOP/ON создаёт `BoundOnKeyStatement` без handler
3. Для регистрации:
   - Ищет функцию в области видимости через `_scope.TryLookupFunction()`
   - Проверяет, что это `SUB` (не `FUNCTION`)
   - Проверяет сигнатуру через `FunctionAcceptsOnKeyEvent()`:
     - 0 параметров → `handlerReceivesKeyText = false`
     - 1 параметр STRING (не BYREF) → `handlerReceivesKeyText = true`
     - Иначе → ошибка MB2034
   - Создаёт `BoundFunctionExpression` для handler

### 8.2 FunctionAcceptsOnKeyEvent()

Корректно определяет:
- 0 параметров → `false` (handler не получает имя клавиши)
- 1 параметр STRING, не BYREF → `true` (handler получает имя клавиши)
- Любая другая сигнатура → `null` (ошибка)

**Замечание:** Функция не позволяет передавать имя клавиши по значению в BYREF параметр. Это корректно — BYREF для входного параметра обработчика не имеет смысла.

---

## 9. Анализ Interpreter

### 9.1 Структура данных

`Interpreter.Keyboard.cs` содержит:
- `_keyboardHandlers: Dictionary<string, KeyboardHandlerRegistration>` — словарь обработчиков
- `_bufferedKeys: Queue<string>` — буфер неподходящих клавиш
- `_keyboardHandlerDepth: int` — защита от рекурсивного входа

### 9.2 Диспетчеризация

`DispatchKeyboardHandlers()` вызывается из `DispatchInputHandlers()` на каждой границе оператора. Алгоритм:

1. Если нет обработчиков или уже внутри обработчика — выход
2. Читать клавиши из очереди
3. Для каждой клавиши:
   - Нормализовать
   - Искать обработчик через `TryGetKeyboardHandler()`
   - Если найден — вызвать
   - Если не найден — добавить в deferredKeys
4. Вернуть deferredKeys в буфер

### 9.3 TryGetKeyboardHandler()

Порядок поиска:
1. Аккорды (multi-key) с совпадением PrimaryKey и удержанием RequiredKeys (по убыванию длины)
2. Точное совпадение по canonical text
3. Обработчик "ANY" (если запрошенная клавиша не "ANY")

### 9.4 INKEY/KEYDOWN/KEYPRESS в Interpreter

В `Interpreter.Builtins.cs` (строки 225-227):
```csharp
BuiltinFunctionId.InKey => RuntimeValue.FromString(ReadKeyboardInput()),
BuiltinFunctionId.KeyDown => RuntimeValue.FromBoolean(IsKeyboardKeyDown(arguments[0].AsString())),
BuiltinFunctionId.KeyPress => RuntimeValue.FromBoolean(ConsumeKeyboardKeyPress(arguments[0].AsString())),
```

---

## 10. Анализ Bytecode / VM

### 10.1 Opcodes

| Opcode | Операнды | Стековый эффект |
|--------|----------|-----------------|
| `RegisterKeyHandler` | operandCount (0 или 1, флаг passKeyText) | -2 (pop handler, pop keyText) |
| `UnregisterKeyHandler` | нет | -1 (pop keyText) |
| `PauseKeyHandler` | нет | -1 (pop keyText) |
| `ResumeKeyHandler` | нет | -1 (pop keyText) |
| `StatementBoundary` | нет | 0 |

### 10.2 Lowering

`LowerOnKey()` (src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs:918):
- Для OFF: `UnregisterKeyHandler`
- Для STOP: `PauseKeyHandler`
- Для ON: `ResumeKeyHandler`
- Для Register: выталкивает keyExpression и handlerExpression, затем `RegisterKeyHandler`

### 10.3 Валидация

`BytecodeModuleValidator.cs` содержит:
- `ValidateRegisterKeyHandlerInstruction()` — проверяет флаг passKeyText
- Проверки стекового эффекта для всех key handler opcodes

### 10.4 INKEY/KEYDOWN/KEYPRESS в VM

Два пути:
1. **Fast-path** (`BytecodeVm.BuiltinDispatch.cs:140-142`): `BuiltinExecution.NullaryNoLocation` / `UnaryNoLocation` — прямой вызов без аллокации массива аргументов
2. **Fallback** (`BytecodeVm.Builtins.cs:141-143`): общий путь через `arguments[]`

---

## 11. Анализ Runtime

### 11.1 KeyboardInputNormalizer

Статический класс (157 строк) обеспечивает:
- 28 канонических имён клавиш
- Алиасы: LEFT→LEFTARROW/ARROWLEFT, ESC→ESCAPE, ENTER→RETURN, и др.
- Нормализация: удаление пробелов/дефисов/подчёркиваний, верхний регистр
- Аккорды: разбор по `+`, последняя часть = primaryKey, остальные = requiredKeys

### 11.2 IKeyboardInputProvider

Интерфейс (8 строк):
- `ReadKey()` — чтение следующей клавиши из очереди
- `IsKeyDown(string keyText)` — проверка текущего удержания

### 11.3 RuntimeInputSnapshot

Класс (276 строк) управляет:
- `_pendingKeys: Queue<string>` — очередь событий
- `_pressedKeys: HashSet<string>` — текущее удержание
- Потокобезопасность через `lock (_gate)`

---

## 12. Взаимодействие с типами

| Тип | Взаимодействие |
|-----|---------------|
| STRING | INKEY возвращает STRING; KEYDOWN/KEYPRESS/ON KEY принимают STRING |
| BOOLEAN | KEYDOWN/KEYPRESS возвращают BOOLEAN |
| INTEGER/LONG/DOUBLE/DECIMAL | Не участвуют напрямую |
| ARRAY | Не участвует |
| TYPE | Не участвует |
| FUNCTION | ON KEY принимает значение-функцию (SUB) как обработчик |

---

## 13. Взаимодействие со String

- Все клавиатурные функции работают со строками
- Нормализация: `ToUpperInvariant()`, удаление пробелов/дефисов/подчёркиваний
- Пустая строка: INKEY возвращает "" при отсутствии клавиши; пустой keyText игнорируется
- Unicode: односимвольные строки проходят как есть; multi-char строки нормализуются
- Fixed strings: не участвуют специфическим образом

---

## 14. Взаимодействие с Array

Не применимо. Клавиатурные функции не работают с массивами.

---

## 15. Взаимодействие с TYPE

Не применимо напрямую. Обработчик ON KEY — это SUB/FUNCTION, а не метод TYPE. Однако обработчик может быть методом TYPE через значение-функцию.

---

## 16. Взаимодействие с Functions / SUB

- ON KEY принимает только SUB (не FUNCTION)
- SUB может быть без параметров или с одним параметром STRING (не BYREF)
- Обработчик вызывается через `InvokeUserRoutine()` (Interpreter) или `InvokeBytecodeFunction()` (VM)
- Замыкания: обработчик может захватывать переменные из внешней области

---

## 17. Взаимодействие с Control Flow

- Диспетчеризация обработчиков происходит на границах операторов
- В циклах FOR/WHILE/DO обработчики вызываются на каждой итерации
- RETURN из обработчика возвращает из обработчика, не из основной программы
- EXIT FOR/EXIT DO внутри обработчика работает корректно

---

## 18. Взаимодействие с Error / Exception System

- MB2004: неправильное количество аргументов INKEY/KEYDOWN/KEYPRESS
- MB2003: не-STRING аргумент KEYDOWN/KEYPRESS
- MB2034: неподходящий обработчик ON KEY (не SUB или неправильные параметры)
- RuntimeFailureException не генерируется клавиатурными функциями
- TRY/CATCH внутри обработчика ON KEY работает корректно

---

## 19. State / Lifetime / Ownership

### 19.1 RuntimeInputSnapshot (IDE side)

| Аспект | Значение |
|--------|----------|
| Создаёт | `InputStateService` в конструкторе |
| Владеет | `InputStateService` |
| Изменяет | `ProgramKeyboardInputRouter` (через `EnqueueKey`, `SetKeyDown`, `SetKeyUp`) |
| Читает | `ProgramConsoleService` (через `ReadKey`, `IsKeyDown`) |
| Уничтожает | При завершении программы или `Reset()` |

### 19.2 _keyboardHandlers (Runtime side)

| Аспект | Значение |
|--------|----------|
| Создаёт | Interpreter/BytecodeVm при старте программы |
| Владеет | Interpreter/BytecodeVm |
| Изменяет | `RegisterKeyboardHandler()`, `UnregisterKeyboardHandler()`, `SetKeyboardHandlerEnabled()` |
| Читает | `DispatchKeyboardHandlers()`, `TryGetKeyboardHandler()` |
| Уничтожает | При завершении программы (GC) |

### 19.3 _bufferedKeys (Runtime side)

| Аспект | Значение |
|--------|----------|
| Создаёт | Interpreter/BytecodeVm при старте программы |
| Владеет | Interpreter/BytecodeVm |
| Изменяет | `DispatchKeyboardHandlers()` (enqueue deferred), `TryReadKeyboardKey()` (dequeue) |
| Уничтожает | При завершении программы (GC) |

---

## 20. Edge Cases

### 20.1 Пустая строка keyText

- `INKEY()` → возвращает "" при пустой очереди (корректно)
- `KEYDOWN("")` → `NormalizeChord("")` → `KeyboardInputChord.Empty` → `false` (корректно)
- `KEYPRESS("")` → `NormalizeChord("")` → `KeyboardInputChord.Empty` → `false` (корректно)
- `ON KEY(""), handler` → `NormalizeChord("")` → `KeyboardInputChord.Empty` → игнорируется (корректно)

### 20.2 Неизвестное имя клавиши

- `KEYDOWN("UNKNOWN")` → нормализуется в "UNKNOWN" → `IsKeyDown("UNKNOWN")` → `false` (корректно)
- `KEYPRESS("UNKNOWN")` → нормализуется в "UNKNOWN" → не найдено в очереди → `false` (корректно)
- `ON KEY("UNKNOWN"), handler` → регистрируется, но никогда не сработает (допустимо)

### 20.3 Аккорды с неизвестными клавишами

- `KEYDOWN("CTRL+UNKNOWN")` → `NormalizeChord` → requiredKeys=["CTRL"], primaryKey="UNKNOWN" → `AreRequiredKeysDown` может вернуть true, но `IsKeyDown("UNKNOWN")` → false (корректно)

### 20.4 Повторная регистрация обработчика

- `ON KEY("LEFT"), Handler1` затем `ON KEY("LEFT"), Handler2` → Handler2 заменяет Handler1 (корректно, документировано)

### 20.5 Рекурсивный вызов обработчика

- `_keyboardHandlerDepth > 0` предотвращает рекурсивный вход (корректно)

### 20.6 Множественные клавиши в очереди

- `DispatchKeyboardHandlers()` обрабатывает все накопленные клавиши за одну диспетчеризацию (корректно)

### 20.7 Клавиша не подошла ни под один обработчик

- Клавиша возвращается в `_bufferedKeys` и доступна для последующего `INKEY()` (корректно, документировано)

### 20.8 SLEEP без аргументов

- `SLEEP` без аргументов вызывает `WaitForAnyKey()`, который ждёт клавишу из той же очереди (корректно)

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| INKEY() | ReadKeyboardInput() | ReadKeyboardInput() | Да | Да | Да |
| KEYDOWN("LEFT") | IsKeyboardKeyDown() | IsKeyboardKeyDown() | Да | Да | Da |
| KEYPRESS("LEFT") | ConsumeKeyboardKeyPress() | ConsumeKeyboardKeyPress() | Да | Да | Да |
| ON KEY("LEFT"), H | RegisterKeyboardHandler() | ExecuteRegisterKeyHandler() | Да | Да | Да |
| ON KEY("LEFT"), OFF | UnregisterKeyboardHandler() | ExecuteUnregisterKeyHandler() | Да | Да | Да |
| ON KEY("LEFT"), STOP | PauseKeyboardHandler() | ExecutePauseKeyHandler() | Да | Да | Да |
| ON KEY("LEFT"), ON | ResumeKeyboardHandler() | ExecuteResumeKeyHandler() | Да | Да | Да |
| ON KEY("ANY"), H | TryGetKeyboardHandler() с ANY | TryGetKeyboardHandler() с ANY | Да | Да | Да |
| Аккорды | NormalizeChord + IsChordDown | NormalizeChord + IsChordDown | Да | Да | Да |
| Диспетчеризация | DispatchInputHandlers() | ExecuteStatementBoundary() | Да | Да | Да |

**Расхождений не обнаружено.** Логика KeyboardHandlerRegistration, TryGetKeyboardHandler, TryConsumeKeyboardKeyPress и InvokeKeyboardHandler идентична в обоих движках.

---

## 22. Анализ производительности

### 22.1 Доступные данные

Из `BYTECODE_PERF_MATRIX.md`:
- Игровой ввод (maze_chase): **0.739x runtime** (VM быстрее)
- INPUT (записи): **0.879x runtime** (VM быстрее)

### 22.2 Отсутствие dedicated keyboard benchmark

Нет отдельного engine-comparison сценария, изолирующего именно клавиатурный ввод без графики и игровой логики. Сценарий `maze_chase_input_polling.axm` смешивает клавиатурный опрос со строковыми операциями, SELECT CASE и арифметикой.

### 22.3 Hot paths

| Hot path | Стоимость | Примечание |
|----------|-----------|------------|
| `KeyboardInputNormalizer.Normalize()` | Низкая | Dictionary lookup + ToUpperInvariant |
| `KeyboardInputNormalizer.NormalizeChord()` | Средняя | Split + Select + Where + ToArray |
| `TryGetKeyboardHandler()` | Средняя | LINQ Where + OrderByDescending при аккордах |
| `TryConsumeKeyboardKeyPress()` | Средняя | Цикл с deferred keys |
| `DispatchKeyboardHandlers()` | Средняя | Цикл чтения + поиск + вызов |

### 22.4 Потенциальные узкие места

1. **LINQ в TryGetKeyboardHandler()**: `Where().OrderByDescending()` создаёт временные аллокации на каждой границе оператора при наличии обработчиков аккордов
2. **NormalizeChord()**: `Split('+').Select().Where().ToArray()` создаёт массивы при каждом вызове
3. **deferredKeys**: `new List<string>()` при каждой диспетчеризации с неподходящими клавишами

---

## 23. Качество Benchmark

### 23.1 Имеющиеся сценарии

| Сценарий | Покрывает | Ограничения |
|----------|-----------|-------------|
| `maze_chase_input_polling.axm` | KEYPRESS, KEYDOWN, UCASE, LCASE в игровом цикле | Смешан со строковой логикой |
| `input_record_intake.axm` | INPUT (не клавиатурный опрос) | Не покрывает INKEY/KEYDOWN/KEYPRESS |

### 23.2 Отсутствующие сценарии

- Чистый INKEY polling без игровой логики
- Чистый KEYDOWN polling
- Чистый KEYPRESS polling
- ON KEY handler dispatch
- Аккорды в цикле

---

## 24. Hot Paths и Bottlenecks

### 24.1 TryGetKeyboardHandler() — LINQ в hot path

```csharp
foreach (var candidate in _keyboardHandlers.Values
    .Where(candidate => candidate.IsEnabled && !candidate.Chord.IsSingleKey 
          && candidate.Chord.PrimaryKey == normalizedKey 
          && AreRequiredKeysDown(candidate.Chord))
    .OrderByDescending(candidate => candidate.Chord.RequiredKeys.Count))
```

Этот LINQ-запрос выполняется при каждом вызове `TryGetKeyboardHandler()`, т.е. на каждой границе оператора при наличии обработчиков. Создаёт временные итераторы и аллокации.

**Направление оптимизации:** Замена на явный цикл с ручным поиском максимума.

### 24.2 NormalizeChord() — аллокации

```csharp
var parts = keyText
    .Split('+', StringSplitOptions.TrimEntries)
    .Select(Normalize)
    .Where(static part => part.Length > 0)
    .ToArray();
```

Создаёт массив при каждом вызове. Для `KEYDOWN`/`KEYPRESS` это происходит при каждом опросе.

**Направление оптимизации:** Кеширование результатов NormalizeChord или использование Span-based парсинга.

---

## 25. Архитектурные проблемы

### 25.1 Дублирование логики между Interpreter и BytecodeVm

**Серьёзность: P2**
**Категория: ARCHITECTURE**

Следующие методы полностью дублируются между `Interpreter.Keyboard.cs` (216 строк) и `BytecodeVm.Keyboard.cs` (289 строк):

| Метод | Interpreter | BytecodeVm | Идентичны |
|-------|-------------|------------|-----------|
| `TryGetKeyboardHandler()` | строки 45-69 | строки 56-80 | Да |
| `RegisterKeyboardHandler()` | строки 71-82 | — | — |
| `UnregisterKeyboardHandler()` | строки 84-96 | — | — |
| `PauseKeyboardHandler()` | строки 98-101 | — | — |
| `ResumeKeyboardHandler()` | строки 103-106 | — | — |
| `SetKeyboardHandlerEnabled()` | строки 108-122 | строки 129-143 | Да |
| `ReadKeyboardInput()` | строки 124-129 | строки 145-150 | Да |
| `IsKeyboardKeyDown()` | строки 131-135 | строки 204-208 | Да |
| `ConsumeKeyboardKeyPress()` | строки 137-140 | строки 210-213 | Да |
| `TryConsumeKeyboardKeyPress()` | строки 142-175 | строки 215-248 | Да |
| `IsChordDown()` | строки 177-178 | строки 250-251 | Да |
| `AreRequiredKeysDown()` | строки 180-181 | строки 253-254 | Да |
| `IsKeyDown()` | строка 183 | строка 256 | Да |
| `TryReadKeyboardKey()` | строки 185-195 | строки 258-268 | Да |
| `KeyboardHandlerRegistration` | строка 215 | строка 288 | Да |

Это тот же паттерн дублирования, который был исправлен для Numeric, Regex, String, FileSystem и DateTime.

### 25.2 Дублирование KeyboardHandlerRegistration

`KeyboardHandlerRegistration` определён как `private readonly record struct` в обоих файлах:
- `Interpreter.Keyboard.cs:215`
- `BytecodeVm.Keyboard.cs:288`

Идентичные определения.

---

## 26. Дублирование логики

### 26.1 Runtime ↔ IDE: маппинг имён клавиш

`KeyboardInputNormalizer` (Runtime) и `KeyboardInputNameMapper` (IDE) оба содержат маппинг специальных клавиш:

| Runtime (KeyboardInputNormalizer) | IDE (KeyboardInputNameMapper) |
|---|---|
| Алиасы через Dictionary | Switch expression |
| "LEFT" → "LEFT" | Key.Left → "LEFT" |
| "ESC" → "ESC" | Key.Escape → "ESC" |
| "ENTER" → "ENTER" | Key.Enter → "ENTER" |

Логика не дублируется напрямую (разные входы: строка vs WPF Key), но конечные канонические имена должны совпадать. Нет единого источника истины для списка канонических имён.

### 26.2 Interpreter ↔ BytecodeVm: полное дублирование

Подробности в разделе 25.1.

---

## 27. Single Source of Truth

| Сущность | Источник истины | Дубликаты |
|----------|----------------|-----------|
| Канонические имена клавиш | `KeyboardInputNormalizer.CanonicalNamedKeys` | `KeyboardInputNameMapper.MapKeyStateName()` |
| Алиасы клавиш | `KeyboardInputNormalizer.NamedKeyAliases` | `KeyboardInputNameMapper.MapSpecialKey()` |
| Keyboard handler logic | Нет единого источника | `Interpreter.Keyboard.cs` + `BytecodeVm.Keyboard.cs` |
| KeyboardHandlerRegistration | Нет единого источника | Определён в обоих файлах |
| BuiltinFunctionId для keyboard | `BuiltinFunctionId.cs` | — (единственный источник) |
| Opcode для keyboard | `Opcode.cs` | — (единственный источник) |

---

## 28. Legacy / Dead Code

Не обнаружен. Все компоненты подсистемы активно используются.

---

## 29. Надёжность и безопасность

### 29.1 Потокобезопасность

- `RuntimeInputSnapshot` использует `lock (_gate)` для всех операций (корректно)
- `_keyboardHandlers` и `_bufferedKeys` в Interpreter/BytecodeVm не защищены синхронизацией, но runtime однопоточный (допустимо)

### 29.2 Ресурсы

- Клавиатурные функции не создают ресурсов, требующих освобождения
- `RuntimeFunctionValue` (обработчик) управляется GC

### 29.3 Защита от рекурсии

- `_keyboardHandlerDepth` предотвращает рекурсивный вход в обработчики (корректно)

---

## 30. Анализ тестов

### 30.1 Покрытие тестами

| Тип теста | Файл | Количество | Покрытие |
|-----------|------|------------|----------|
| Parser | `ParserTests.cs` | 3 теста | ON KEY (register, OFF, ON/STOP) |
| Binder | `BinderTests.cs` | 7 тестов | INKEY, KEYDOWN, KEYPRESS (positive/negative), ON KEY (handler types, modes) |
| Interpreter | `InterpreterTests.cs` | Покрыто через builtins | INKEY, KEYDOWN, KEYPRESS |
| BytecodeVm | `BytecodeVmTests.cs` | Покрыто через builtins | INKEY, KEYDOWN, KEYPRESS |
| ExecutionPipeline | `ExecutionPipelineTests.cs` | 10+ тестов | INKEY, KEYDOWN, KEYPRESS, ON KEY (register, ANY, chord, modes) |
| Acceptance | `AcceptanceProgramTests.cs` | 10+ тестов | ON KEY samples, KEYDOWN aliases, KEYPRESS modifiers, chords |

### 30.2 Качество assertions

Тесты проверяют:
- Возвращаемые значения INKEY/KEYDOWN/KEYPRESS
- Регистрацию и вызов обработчиков
- Режимы ON/STOP/OFF
- Аккорды
- Передачу имени клавиши в обработчик
- Поведение "ANY"
- Паритет Interpreter ↔ VM

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

- Множественные обработчики одновременно (LEFT и RIGHT)
- Замена обработчика (повторная регистрация)
- INKEY после ON KEY (обработанная клавиша не попадает в INKEY)

### 31.2 Отрицательные сценарии

- KEYDOWN/KEYPRESS с неизвестным именем клавиши
- ON KEY с пустой строкой keyText
- ON KEY с не-SUB обработчиком (покрыто в Binder)

### 31.3 Граничные случаи

- Очень длинная очередь клавиш
- Одновременное нажатие множества клавиш
- Аккорд с 4+ клавишами
- Быстрая последовательность ON KEY ON/STOP/OFF

### 31.4 Parity

- Все существующие тесты проверяют паритет Interpreter ↔ VM (корректно)

### 31.5 Performance

- Нет dedicated keyboard benchmark для измерения overhead диспетчеризации

---

## 32. Анализ Samples

| Sample | Демонстрирует | Engine | Проверяемый результат |
|--------|---------------|--------|----------------------|
| `samples/functions/inkey/poll_keys.axm` | INKEY в цикле | Любой | Ручной |
| `samples/functions/keydown/held_modifiers.axm` | KEYDOWN с модификаторами | Любой | Ручной |
| `samples/functions/keydown/canonical_aliases.axm` | Канонические имена и алиасы | Любой | Ручной |
| `samples/functions/keypress/single_press.axm` | Одиночное нажатие | Любой | Ручной |
| `samples/functions/keypress/modified_keypress.axm` | KEYPRESS с модификаторами | Любой | Ручной |
| `samples/operators/on-key/left_handler.axm` | Обработчик LEFT | Любой | Ручной |
| `samples/operators/on-key/any_key_handler.axm` | Обработчик ANY | Любой | Ручной |
| `samples/operators/on-key/handler_key_name.axm` | Передача имени клавиши | Любой | Ручной |
| `samples/operators/on-key/chord_handler.axm` | Аккорд CTRL+LEFT | Любой | Ручной |
| `samples/operators/on-key/stop_resume_handler.axm` | Пауза/возобновление | Любой | Ручной |
| `samples/operators/on-key/invalid_handler_parameter.axm` | Негативный: MB2034 | Любой | Ручной |
| `samples/operators/sleep/delay_and_key_wait.axm` | SLEEP + ожидание клавиши | Любой | Ручной |

**Замечание:** Все samples — ручные проверки. Нет автоматизированных acceptance samples с deterministic input.

---

## 33. Анализ документации

### 33.1 Manual pages

| Страница | Качество | Полнота |
|----------|----------|---------|
| `docs/manual/inkey.html` | Высокое | Полная |
| `docs/manual/keydown.html` | Высокое | Полная (включая таблицу имён) |
| `docs/manual/keypress.html` | Высокое | Полная |
| `docs/manual/on-key.html` | Высокое | Полная (включая аккорды, режимы) |
| `docs/manual/keyboard-mouse-events.html` | Высокое | Обзорная |

### 33.2 Документация vs Код

| Аспект | Документация | Код | Соответствие |
|--------|-------------|-----|-------------|
| INKEY() без аргументов | Да | Да | Да |
| KEYDOWN принимает STRING | Да | Да | Да |
| KEYPRESS consumes event | Да | Да | Да |
| ON KEY принимает SUB | Да | Да | Да |
| ON KEY с параметром STRING | Да | Да | Да |
| Аккорды через + | Да | Да | Да |
| "ANY" pseudo-key | Да | Да | Да |
| ON/STOP/OFF режимы | Да | Да | Да |
| Канонические имена и алиасы | Да (таблица) | Да | Да |

**Расхождений документации и кода не обнаружено.**

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

- `INKEY`, `KEYDOWN`, `KEYPRESS` подсвечиваются как built-in функции в обоих `.xshd`
- `ON`, `KEY` подсвечиваются как ключевые слова

### 34.2 Маршрутизация ввода

`ProgramKeyboardInputRouter` обрабатывает:
- `HandlePreviewKeyDown` → `SetKeyDown` + `EnqueueKey` для специальных клавиш
- `HandlePreviewKeyUp` → `SetKeyUp`
- `HandlePreviewTextInput` → `EnqueueKey` для печатных символов

### 34.3 KeyboardInputNameMapper

Маппит WPF `Key` enum в Axiom-имена:
- Специальные клавиши: `Key.Left` → `"LEFT"`, `Key.Escape` → `"ESC"`
- Буквенно-цифровые: `Key.A` → `"A"`, `Key.D0` → `"0"`
- F-клавиши: `Key.F1` → `"F1"`
- Модификаторы: `Key.LeftCtrl` → `"CTRL"`

**Замечание:** `KeyboardInputNameMapper` не использует `KeyboardInputNormalizer` — это два независимых маппинга с общими каноническими именами.

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Примечание |
|-------------|-----|------------|
| WPF Key enum | IDE-only | `KeyboardInputNameMapper` зависит от `System.Windows.Input` |
| .NET Thread.Sleep | Runtime | `WaitForAnyKey()` использует 10ms polling |
| .NET Dictionary | Runtime | `_keyboardHandlers` |
| .NET Queue | Runtime | `_bufferedKeys` |
| .NET HashSet | Runtime | `_pressedKeys` в `RuntimeInputSnapshot` |

**HOST LIMITATION:** Клавиатурный ввод полностью зависит от хоста (IDE или тестовый mock). Без `IKeyboardInputProvider` клавиатурные функции возвращают пустые значения.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Дублирование keyboard logic между Interpreter и BytecodeVm

├── SYS-014: TryGetKeyboardHandler() дублируется
├── SYS-015: TryConsumeKeyboardKeyPress() дублируется
├── SYS-016: SetKeyboardHandlerEnabled() дублируется
├── SYS-017: ReadKeyboardInput() дублируется
├── SYS-018: IsKeyboardKeyDown() дублируется
├── SYS-019: KeyboardHandlerRegistration дублируется
└── SYS-020: InvokeKeyboardHandler() дублируется (с разницей в вызове)

**Направление:** Вынести общую логику в `BuiltinKeyboardHelpers` (по аналогии с `BuiltinStringHelpers`, `BuiltinNumericHelpers` и др.)

---

## 37. Стабильные области

| Область | Статус | Почему не трогать |
|---------|--------|-------------------|
| KeyboardInputNormalizer | Стабильно | Чистый, протестированный, не дублируется |
| IKeyboardInputProvider | Стабильно | Минимальный интерфейс, не менялся |
| OnKeyStatementSyntax | Стабильно | Корректный парсинг |
| BoundOnKeyStatement | Стабильно | Корректная семантика |
| Lowering ON KEY | Стабильно | Корректное понижение в opcodes |
| Opcode definitions | Стабильно | 4 опкода, не менялись |
| Manual pages | Стабильно | Полные, корректные |
| IDE highlighting | Стабильно | Все слова подсвечиваются |

---

## 38. Возможности консолидации

### 38.1 BuiltinKeyboardHelpers

**Текущие места:**
- `Interpreter.Keyboard.cs` (216 строк)
- `BytecodeVm.Keyboard.cs` (289 строк)

**Дублируемая логика:**
- TryGetKeyboardHandler()
- TryConsumeKeyboardKeyPress()
- SetKeyboardHandlerEnabled()
- ReadKeyboardInput()
- IsKeyboardKeyDown()
- IsChordDown()
- AreRequiredKeysDown()
- TryReadKeyboardKey()
- KeyboardHandlerRegistration

**Единый источник истины:** Новый `BuiltinKeyboardHelpers` в `src/Axiom.Runtime/Builtins/`

**Направление миграции:**
1. Создать `BuiltinKeyboardHelpers` с общей логикой
2. Переместить `KeyboardHandlerRegistration` в общий тип
3. Оставить в Interpreter/BytecodeVm только вызовы к хосту и InvokeUserRoutine/InvokeBytecodeFunction

**Риски:** Низкий риск — логика чистая, не зависит от состояния движка.

---

## 39. Возможности разделения ответственности

Текущее разделение корректно:
- **Lexer:** Токенизация ON, KEY
- **Parser:** Синтаксис ON KEY
- **Binder:** Семантика ON KEY, типы аргументов INKEY/KEYDOWN/KEYPRESS
- **Lowering:** Понижение в opcodes
- **VM/Interpreter:** Исполнение
- **Runtime:** Нормализация, хост-интерфейс
- **IDE:** Маршрутизация WPF-событий

Разделение ответственностей не требует изменений.

---

## 40. Кандидаты на удаление

Не обнаружены. Все компоненты активно используются.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Готовность к Compiler

- Keyboard opcodes уже линейные (RegisterKeyHandler, UnregisterKeyHandler, PauseKeyHandler, ResumeKeyHandler)
- INKEY/KEYDOWN/KEYPRESS идут через общий CallBuiltin путь
- Готовность: **высокая**

### 41.2 Готовность к оптимизации

- TryGetKeyboardHandler() можно оптимизировать (LINQ → явный цикл)
- NormalizeChord() можно кешировать
- Готовность: **средняя**

### 41.3 Готовность к расширению языка

- ON KEY("ANY") уже работает
- Аккорды уже работают
- Легко добавить новые имена клавиш в KeyboardInputNormalizer
- Готовность: **высокая**

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-014 | P2 | ARCHITECTURE | Interpreter + VM | TryGetKeyboardHandler() дублируется | Сравнение кода | Отсутствие общего слоя | CONFIRMED |
| SYS-015 | P2 | ARCHITECTURE | Interpreter + VM | TryConsumeKeyboardKeyPress() дублируется | Сравнение кода | Отсутствие общего слоя | CONFIRMED |
| SYS-016 | P2 | ARCHITECTURE | Interpreter + VM | SetKeyboardHandlerEnabled() дублируется | Сравнение кода | Отсутствие общего слоя | CONFIRMED |
| SYS-017 | P2 | ARCHITECTURE | Interpreter + VM | ReadKeyboardInput() дублируется | Сравнение кода | Отсутствие общего слоя | CONFIRMED |
| SYS-018 | P2 | ARCHITECTURE | Interpreter + VM | IsKeyboardKeyDown() дублируется | Сравнение кода | Отсутствие общего слоя | CONFIRMED |
| SYS-019 | P2 | ARCHITECTURE | Interpreter + VM | KeyboardHandlerRegistration дублируется | Сравнение кода | Отсутствие общего типа | CONFIRMED |
| SYS-020 | P2 | ARCHITECTURE | Interpreter + VM | InvokeKeyboardHandler() дублируется | Сравнение кода | Разный вызов (UserRoutine vs BytecodeFunction) | CONFIRMED |
| SYS-021 | P3 | PERFORMANCE | Runtime | LINQ в TryGetKeyboardHandler() hot path | Код | Неоптимизированный поиск | CONFIRMED |
| SYS-022 | P3 | PERFORMANCE | Runtime | NormalizeChord() создаёт массивы при каждом вызове | Код | Неоптимизированный парсинг | CONFIRMED |
| SYS-023 | P3 | TESTS | Tests | Нет dedicated keyboard engine-comparison benchmark | Поиск в samples/smoke/engine-compare/ | Не создан | CONFIRMED |
| SYS-024 | P4 | ARCHITECTURE | IDE + Runtime | KeyboardInputNameMapper не использует KeyboardInputNormalizer | Сравнение кода | Разные слои (WPF Key vs string) | MEDIUM |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

- TASK-000: Создать dedicated keyboard engine-comparison benchmark

### ЭТАП 1 — Архитектурное улучшение

- TASK-001: Создать `BuiltinKeyboardHelpers` и вынести общую логику
- TASK-002: Переместить `KeyboardHandlerRegistration` в общий тип
- TASK-003: Рефакторить Interpreter.Keyboard.cs и BytecodeVm.Keyboard.cs

### ЭТАП 2 — Оптимизация производительности

- TASK-004: Заменить LINQ в TryGetKeyboardHandler() на явный цикл
- TASK-005: Оптимизировать NormalizeChord() (кеширование или Span-based)

### ЭТАП 3 — Усиление тестирования

- TASK-006: Добавить тесты для граничных случаев
- TASK-007: Добавить тесты для множественных обработчиков

### ЭТАП 4 — Обновление документации

- Нет необходимости — документация актуальна

---

## 44. Граф зависимостей задач

```
TASK-000 (benchmark) ─────────────────────────────────┐
                                                       │
TASK-001 (BuiltinKeyboardHelpers) ──→ TASK-002 ──→ TASK-003
                                                       │
                                                       ▼
                                               TASK-004 (LINQ)
                                                       │
                                                       ▼
                                               TASK-005 (NormalizeChord)
                                                       │
                                                       ▼
                                               TASK-006 (тесты)
```

---

## 45. Definition of Done

Подсистема KEYBOARD / INKEY считается завершённой, когда:

1. **Корректность:** INKEY, KEYDOWN, KEYPRESS, ON KEY работают в Interpreter и VM с одинаковым результатом
2. **Edge cases:** Пустая строка, неизвестная клавиша, аккорды, рекурсия — обработаны корректно
3. **Error handling:** MB2004, MB2003, MB2034 генерируются корректно
4. **Interpreter:** Все keyboard функции работают через IKeyboardInputProvider
5. **VM:** Все keyboard функции работают без interpreter fallback
6. **Тесты:** Parser, Binder, Interpreter, VM, ExecutionPipeline, Acceptance покрыты
7. **Documentation:** 5 manual pages актуальны и полны
8. **IDE:** Подсветка INKEY, KEYDOWN, KEYPRESS работает
9. **Performance:** Keyboard hot paths не содержат избыточных аллокаций

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | SYS-014 | P2 | TryGetKeyboardHandler() дублируется между Interpreter и VM |
| 2 | SYS-015 | P2 | TryConsumeKeyboardKeyPress() дублируется |
| 3 | SYS-016 | P2 | SetKeyboardHandlerEnabled() дублируется |
| 4 | SYS-017 | P2 | ReadKeyboardInput() дублируется |
| 5 | SYS-018 | P2 | IsKeyboardKeyDown() дублируется |
| 6 | SYS-019 | P2 | KeyboardHandlerRegistration дублируется |
| 7 | SYS-020 | P2 | InvokeKeyboardHandler() дублируется |
| 8 | SYS-024 | P4 | KeyboardInputNameMapper не использует KeyboardInputNormalizer |

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемый эффект |
|---|---|---|---|
| 1 | SYS-021 | Замена LINQ в TryGetKeyboardHandler() | Уменьшение аллокаций на каждой границе оператора |
| 2 | SYS-022 | Оптимизация NormalizeChord() | Уменьшение аллокаций при каждом вызове KEYDOWN/KEYPRESS |
| 3 | SYS-023 | Dedicated keyboard benchmark | Точное измерение overhead клавиатурного ввода |

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---------------|------------|
| Корректность | 9 | Все функции работают корректно в обоих движках |
| Семантика | 9 | Полный контракт: INKEY, KEYDOWN, KEYPRESS, ON KEY, аккорды, ANY, режимы |
| Архитектура | 7 | Чистое разделение слоёв, но дублирование между Interpreter и VM |
| Runtime | 8 | KeyboardInputNormalizer чистый и эффективный |
| Interpreter | 8 | Полная реализация, но дублирование с VM |
| Bytecode VM | 8 | Полная реализация, но дублирование с Interpreter |
| Производительность | 7 | Работает быстро, но есть неоптимизированные hot paths |
| Память | 8 | Минимальные аллокации, но LINQ в hot path |
| Error handling | 9 | MB2004, MB2003, MB2034 корректны |
| Tests | 8 | Хорошее покрытие, но нет dedicated benchmark |
| Samples | 9 | 12 samples покрывают все сценарии |
| Documentation | 9 | 5 manual pages, полные и корректные |
| IDE | 9 | Подсветка, маршрутизация ввода работают |
| Maintainability | 7 | Дублирование затрудняет сопровождение |
| Reliability | 9 | Стабильная работа, защита от рекурсии |
| Future readiness | 9 | Готово к Compiler, оптимизации, расширению |

**Общий Health Score: 7.8/10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему KEYBOARD / INKEY достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers

Нет критических blockers. Подсистема работает корректно.

### Что нужно сделать в первую очередь

1. Вынести дублированную keyboard logic в `BuiltinKeyboardHelpers` (по аналогии с Numeric, Regex, String, FileSystem, DateTime)
2. Создать dedicated keyboard engine-comparison benchmark

### Что можно отложить

1. Оптимизация LINQ в TryGetKeyboardHandler()
2. Оптимизация NormalizeChord()
3. Дополнительные edge-case тесты

### Что не следует трогать

1. KeyboardInputNormalizer — стабилен, чист, не дублируется
2. IKeyboardInputProvider — минимальный интерфейс
3. OnKeyStatementSyntax — корректный парсинг
4. BoundOnKeyStatement — корректная семантика
5. Lowering ON KEY — корректное понижение
6. Opcode definitions — стабильны
7. Manual pages — полные и корректные
8. IDE highlighting — работает
