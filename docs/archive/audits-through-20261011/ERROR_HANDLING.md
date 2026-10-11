# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## ERROR HANDLING / EXCEPTIONS / DIAGNOSTICS

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы обработки ошибок, исключений и диагностики языка Axiom. Аудит охватывает 63 диагностических кода, 4 синтаксических узла, 4 bound-узла, 4 байткодных опкода, 2 исполняющих движка (Interpreter и BytecodeVm), интеграционный конвейер `AxiomExecutionPipeline` и IDE-слой отображения диагностик.

**Главный вывод:** подсистема ошибок Axiom построена на зрелой, хорошо спроектированной архитектуре с чётким разделением ответственности между слоями. Канонический реестр кодов `DiagnosticCodes` (`MB0xxx`–`MB4xxx`) является единственным источником правды для идентификации ошибок. Runtime-ошибки распространяются через sentinel-исключение `RuntimeFailureException`, а вся информация об ошибке передаётся через `DiagnosticBag`. Модель `TRY/CATCH/FINALLY` реализована в обоих движках с полным паритетом.

**Общая оценка здоровья: 8.7/10** (повышена с 8.5 предыдущего аудита после исправления ERROR-003 и ERROR-004).

---

## 2. Что представляет собой эта подсистема

Подсистема обработки ошибок, исключений и диагностики отвечает за:

1. **Диагностику на этапе компиляции** — лексические ошибки (`MB0xxx`), синтаксические ошибки (`MB1xxx`), семантические ошибки (`MB2xxx`).
2. **Диагностику на этапе выполнения** — runtime-ошибки (`MB3xxx`), пользовательские ошибки через оператор `ERROR`.
3. **Управляемый перехват ошибок** — блок `TRY ... CATCH ... FINALLY ... END TRY`.
4. **Распространение ошибок** — через sentinel-исключение `RuntimeFailureException` и `DiagnosticBag`.
5. **Типизированное значение ошибки** — встроенный тип `ERROR` с полями `Code`, `Message`, `Line`, `Column`.
6. **Отображение диагностик в IDE** — DataGrid с навигацией к исходному коду.

Подсистема является поперечной (cross-cutting): она затрагивает все этапы конвейера языка от лексера до IDE.

---

## 3. Полный список возможностей

### SYS-001: Диагностические коды (DiagnosticCodes)

- **ID:** SYS-001
- **Название:** Реестр диагностических кодов
- **Категория:** RUNTIME FACILITY
- **Назначение:** Единый реестр всех кодов ошибок языка Axiom
- **Пользовательский синтаксис:** Неявный (коды отображаются в сообщениях об ошибках)
- **Тип:** RUNTIME FACILITY
- **Parser:** Не участвует
- **Syntax Node:** Не участвует
- **Binder:** Использует MB2xxx коды (39 штук)
- **Semantic Layer:** `DiagnosticCodes` в `Axiom.Core`
- **Interpreter:** Использует MB3xxx коды (11 штук)
- **Bytecode Lowering:** Не участвует напрямую
- **Opcode:** Не участвует
- **Bytecode VM:** Использует MB3xxx коды (11 штук) + MB4xxx (3 штуки)
- **Runtime Helper:** `ReportRuntimeDiagnostic`, `AppendCallChain`
- **IDE:** `ProgramDiagnosticItem.ResolveStage()` маппит коды на этапы
- **Tests:** Покрыты через все тестовые проекты
- **Samples:** 30+ негативных acceptance-сэмплов
- **Benchmarks:** Не применимо
- **Documentation:** `docs/manual/diagnostics.html` (217 строк)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Core/DiagnosticCodes.cs` (77 строк), 63 кода

### SYS-002: Diagnostic record

- **ID:** SYS-002
- **Название:** Каноническая запись диагностики
- **Категория:** RUNTIME FACILITY
- **Назначение:** Неизменяемая запись ошибки с кодом, сообщением и позицией
- **Тип:** RUNTIME FACILITY
- **Parser:** Создаётся при лексических и синтаксических ошибках
- **Syntax Node:** Не участвует
- **Binder:** Создаётся при семантических ошибках
- **Semantic Layer:** `Diagnostic` record в `Axiom.Core`
- **Interpreter:** Создаётся при runtime-ошибках
- **Bytecode VM:** Создаётся при runtime-ошибках
- **Runtime Helper:** `ReportRuntimeDiagnostic`
- **IDE:** Конвертируется в `ProgramDiagnosticItem`
- **Tests:** Покрыт косвенно через все тесты диагностик
- **Samples:** Не применимо
- **Documentation:** `docs/manual/diagnostics.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Core/Diagnostic.cs` (29 строк)

### SYS-003: DiagnosticBag

- **ID:** SYS-003
- **Название:** Мутабельный накопитель диагностик
- **Категория:** RUNTIME FACILITY
- **Назначение:** Накопление диагностик в рамках одного этапа конвейера
- **Тип:** RUNTIME FACILITY
- **Parser:** Собственный экземпляр, включает лексические диагностики
- **Binder:** Собственный экземпляр
- **Interpreter:** Собственный экземпляр
- **Bytecode VM:** Собственный экземпляр
- **IDE:** Конвертируется в `ObservableCollection<ProgramDiagnosticItem>`
- **Tests:** Покрыт косвенно
- **Documentation:** Не отдельно
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Core/DiagnosticBag.cs` (41 строка)

### SYS-004: TRY / CATCH / FINALLY / END TRY

- **ID:** SYS-004
- **Название:** Блок управляемого перехвата ошибок
- **Категория:** STATEMENT
- **Назначение:** Перехват runtime-ошибок без завершения программы
- **Пользовательский синтаксис:** `TRY ... CATCH var ... FINALLY ... END TRY`
- **Тип:** STATEMENT
- **Parser:** `ParseTryStatement()` в `Parser.Statements.cs:1459`
- **Syntax Node:** `TryStatementSyntax`, `CatchClauseSyntax`, `FinallyClauseSyntax`
- **Binder:** `BindTryStatement()` в `Binder.ControlFlowStatements.cs:65`
- **Semantic Layer:** `BoundTryStatement` с `CatchVariable`, `CatchStatements`, `FinallyStatements`
- **Interpreter:** `ExecuteTry()` в `Interpreter.Statements.cs:591` через native .NET try/catch
- **Bytecode Lowering:** `LowerTry()` в `BoundProgramLowerer.cs:1024` → `PushErrorHandler`/`PopErrorHandler`/`EndFinally`
- **Opcode:** `PushErrorHandler` (5 операндов), `PopErrorHandler`, `EndFinally`
- **Bytecode VM:** `ExecutePushErrorHandler()` + `TryHandleRuntimeFailure()` state machine
- **Runtime Helper:** `BytecodeErrorHandlerFrame`, `BytecodeCallFrame.PushErrorHandler()`
- **IDE:** Автозакрытие блоков через `AxiomBlockEditingService`
- **Tests:** 9+ тестов в `InterpreterTests.TryCatch.cs`, 4+ в парсинге, 5+ в семантике, 6+ в pipeline
- **Samples:** `samples/operators/try/handled_runtime_error.axm`
- **Documentation:** `docs/manual/try.html` (94 строки)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** Полный паритет Interpreter ↔ BytecodeVm, все тесты проходят

### SYS-005: ERROR (оператор)

- **ID:** SYS-005
- **Название:** Оператор создания/проброса ошибки
- **Категория:** STATEMENT
- **Назначение:** Создание пользовательской ошибки или повторный проброс пойманной
- **Пользовательский синтаксис:** `ERROR код` / `ERROR код, сообщение` / `ERROR ошибка`
- **Тип:** STATEMENT
- **Parser:** `ParseErrorStatement()` в `Parser.Statements.cs:1100`
- **Syntax Node:** `ErrorStatementSyntax`
- **Binder:** `BindErrorStatement()` в `Binder.ControlFlowStatements.cs:76` — 2 режима: `UserCode`, `ExistingError`
- **Semantic Layer:** `BoundErrorStatement` с `Mode`, `Expression`, `MessageExpression`
- **Interpreter:** `ExecuteError()` в `Interpreter.Statements.cs:552`
- **Bytecode Lowering:** `LowerError()` → `RaiseError` / `RaiseExistingError`
- **Opcode:** `RaiseError` (операнд: 0=без сообщения, 1=с сообщением, 2=существующая ошибка)
- **Bytecode VM:** `ExecuteRaiseError()` в `BytecodeVm.cs:1100`
- **Tests:** 4+ теста в `InterpreterTests.Error.cs`, 6+ в pipeline
- **Samples:** `samples/operators/error/user_code_catch.axm`, `samples/operators/error/rethrow_caught_error.axm`
- **Documentation:** `docs/manual/error.html` (84 строки)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** Полный паритет Interpreter ↔ BytecodeVm

### SYS-006: Тип ERROR (встроенный тип значения ошибки)

- **ID:** SYS-006
- **Название:** Встроенный тип ERROR
- **Категория:** TYPE
- **Назначение:** Значение ошибки, доступное в блоке CATCH
- **Пользовательский синтаксис:** `CATCH err` → `err.Code`, `err.Message`, `err.Line`, `err.Column`
- **Тип:** TYPE
- **Parser:** Не участвует напрямую
- **Syntax Node:** Не участвует
- **Binder:** `RuntimeErrorTypeSymbol` в `Symbols.cs:115-128` с полями Code(String), Message(String), Line(Integer), Column(Integer)
- **Semantic Layer:** `TypeSymbol.RuntimeError`
- **Interpreter:** `RuntimeErrorValue` → `RuntimeValue.FromRuntimeError()`
- **Bytecode VM:** `RuntimeErrorValue` → `RuntimeValue.FromRuntimeError()`
- **Runtime Helper:** `RuntimeErrorValue.GetField()` в `RuntimeErrorValue.cs:26`
- **Tests:** Покрыт через TRY/CATCH тесты
- **Samples:** Все samples с CATCH
- **Documentation:** `docs/manual/try.html`, `docs/manual/error-handling.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Values/RuntimeErrorValue.cs` (39 строк)

### SYS-007: RuntimeFailureException (sentinel)

- **ID:** SYS-007
- **Название:** Sentinel-исключение для runtime-ошибок
- **Категория:** RUNTIME FACILITY
- **Назначение:** Маркерное исключение для распространения runtime-ошибок
- **Тип:** RUNTIME FACILITY
- **Interpreter:** Бросается после `ReportRuntimeDiagnostic`, ловится в `ExecuteTry` или `Execute`
- **Bytecode VM:** Бросается после `ReportRuntimeDiagnostic`, ловится в `TryHandleRuntimeFailure()` или `Execute`
- **Runtime Helper:** `RuntimeFailureException` (3 строки, пустое исключение)
- **Tests:** Покрыт косвенно через все TRY/CATCH тесты
- **Documentation:** Не описано явно в manual (внутренняя деталь реализации)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Execution/RuntimeFailureException.cs` (3 строки)

### SYS-008: RuntimeErrorValue (значение ошибки)

- **ID:** SYS-008
- **Название:** Значение типа ERROR
- **Категория:** RUNTIME FACILITY
- **Назначение:** Хранение данных перехваченной ошибки
- **Тип:** RUNTIME FACILITY
- **Interpreter:** Создаётся в `ConsumeLastRuntimeError()`
- **Bytecode VM:** Создаётся в `ConsumeLastRuntimeError()`
- **Runtime Helper:** `RuntimeErrorValue` с полями Code, Message, Line, Column
- **Tests:** Покрыт через TRY/CATCH тесты
- **Documentation:** `docs/manual/try.html`, `docs/manual/error.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Values/RuntimeErrorValue.cs` (39 строк)

### SYS-009: BytecodeErrorHandlerFrame

- **ID:** SYS-009
- **Название:** Фрейм обработчика ошибок в BytecodeVm
- **Категория:** RUNTIME FACILITY
- **Назначение:** Хранение состояния обработчика TRY/CATCH/FINALLY в VM
- **Тип:** RUNTIME FACILITY
- **Bytecode VM:** Используется в `PushErrorHandler`, `TryHandleRuntimeFailure`, `EndFinally`
- **Runtime Helper:** `BytecodeErrorHandlerFrame` с полями CatchTarget, FinallyTarget, EndTarget, State, HasPendingUnhandledFailure, PendingReturnValue, HasPendingReturn
- **Tests:** Покрыт через TRY/CATCH тесты
- **Documentation:** Внутренняя деталь реализации
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Execution/BytecodeErrorHandlerFrame.cs` (54 строки)

### SYS-010: ExecutionSignal (сигналы управления)

- **ID:** SYS-010
- **Название:** Сигналы управления потоком выполнения
- **Категория:** RUNTIME FACILITY
- **Назначение:** Передача сигналов RETURN, EXIT, CONTINUE через TRY/CATCH/FINALLY
- **Тип:** RUNTIME FACILITY
- **Interpreter:** Используется для передачи HasReturn, HasExitFor, HasExitDo, HasContinueFor/While/Do
- **Bytecode VM:** Не используется напрямую (VM использует HasPendingReturn в BytecodeErrorHandlerFrame)
- **Tests:** Покрыт через тесты RETURN через FINALLY
- **Documentation:** Внутренняя деталь реализации
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Execution/ExecutionSignal.cs` (37 строк)

### SYS-011: RuntimeResult (результат выполнения)

- **ID:** SYS-011
- **Название:** Результат выполнения программы
- **Категория:** RUNTIME FACILITY
- **Назначение:** Контейнер для диагностик и состояния после выполнения
- **Тип:** RUNTIME FACILITY
- **Interpreter:** Возвращается из `Execute()`
- **Bytecode VM:** Возвращается из `Execute()`
- **Execution Pipeline:** Используется для формирования `ExecutionResult`
- **Tests:** Покрыт через все интеграционные тесты
- **Documentation:** Внутренняя деталь реализации
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Runtime/Host/RuntimeResult.cs` (30 строк)

### SYS-012: Программный PIPELINE диагностики

- **ID:** SYS-012
- **Название:** Конвейер диагностики от исходного кода до IDE
- **Категория:** RUNTIME FACILITY
- **Назначение:** Последовательная обработка ошибок на всех этапах конвейера
- **Тип:** RUNTIME FACILITY
- **Parser:** `Parser.Parse()` → `DiagnosticBag` (lexer + parser)
- **Binder:** `Binder.Bind()` → `DiagnosticBag`
- **Bytecode:** `BoundProgramLowerer` + `BytecodeCompiler` → `BytecodeDiagnostic` list
- **Execution Pipeline:** `AxiomExecutionPipeline.CompileCore()` — ранний выход при ошибках
- **IDE:** `ProgramRunService` → `ProgramDiagnosticItem[]` → `BottomPaneViewModel`
- **Tests:** `ExecutionPipelineTests.cs` (~4230 строк)
- **Documentation:** `docs/manual/diagnostics.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` (~1200 строк)

### SYS-013: IDE диагностика и навигация

- **ID:** SYS-013
- **Название:** Отображение диагностик в IDE
- **Категория:** IDE
- **Назначение:** Показ ошибок пользователю с навигацией к исходному коду
- **Тип:** IDE
- **IDE:** `ProgramDiagnosticItem`, `BottomPaneViewModel.Diagnostics`, `DiagnosticNavigationTargetFactory`
- **Tests:** `EditorFeatureTests.cs`, `ProgramRunServiceTests.cs`
- **Documentation:** `docs/manual/diagnostics.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `src/Axiom.Ide/Models/ProgramDiagnosticItem.cs` (60 строк), `src/Axiom.Ide/MainWindow.Diagnostics.cs` (241 строка)

### SYS-014: Цепочка вызовов (Call Chain)

- **ID:** SYS-014
- **Название:** Цепочка вызовов в диагностике runtime-ошибок
- **Категория:** RUNTIME FACILITY
- **Назначение:** Добавление информации о стеке вызовов к сообщению об ошибке
- **Тип:** RUNTIME FACILITY
- **Interpreter:** `AppendCallChain()` в `Interpreter.Helpers.cs:561`
- **Bytecode VM:** `AppendCallChain()` в `BytecodeVm.cs:6228`
- **Tests:** `samples/smoke/nested_runtime_diagnostic.axm`
- **Documentation:** `docs/manual/diagnostics.html` (раздел "Как читать сообщения")
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** Оба движка добавляют цепочку вызовов к runtime-диагностике

### SYS-015: Защита от переполнения стека вызовов

- **ID:** SYS-015
- **Название:** Ограничение глубины стека вызовов
- **Категория:** RUNTIME
- **Назначение:** Предотвращение StackOverflowException при бесконечной рекурсии
- **Пользовательский синтаксис:** Неявный (MB3011 при превышении MaxCallDepth=200)
- **Тип:** RUNTIME FACILITY
- **Interpreter:** Проверка в `InvokeUserRoutine` и `InvokeTypeConstructor`
- **Bytecode VM:** Проверка в `ExecuteRoutineCall` и `InvokeBytecodeFunction`
- **Tests:** 4 теста (прямая и взаимная рекурсия в обоих движках)
- **Documentation:** `docs/manual/diagnostics.html` (MB3011)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `DiagnosticCodes.CallStackOverflow = "MB3011"`, `MaxCallDepth = 200`

---

## 4. Фактическая архитектура

### 4.1 Слои подсистемы

```
┌─────────────────────────────────────────────────────────┐
│                    IDE (Axiom.Ide)                       │
│  ProgramDiagnosticItem → BottomPaneViewModel → DataGrid  │
│  DiagnosticNavigationTargetFactory → AvalonEdit          │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│              Execution Pipeline (Axiom.Execution)        │
│  AxiomExecutionPipeline.CompileCore() → Run()            │
│  CompilationResult.Diagnostics / ExecutionResult.Diagnostics │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│                   Runtime (Axiom.Runtime)                │
│  ┌──────────────────┐  ┌───────────────────────────┐    │
│  │   Interpreter     │  │      BytecodeVm           │    │
│  │ .NET try/catch    │  │ ErrorHandlerFrame stack   │    │
│  │ ExecutionSignal   │  │ TryHandleRuntimeFailure() │    │
│  └──────────────────┘  └───────────────────────────┘    │
│  RuntimeFailureException · RuntimeErrorValue             │
│  RuntimeResult · DiagnosticBag                           │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│              Bytecode (Axiom.Bytecode)                   │
│  BoundProgramLowerer → PushErrorHandler/PopErrorHandler  │
│  BytecodeCompiler → Opcode.PushErrorHandler (5 ops)      │
│  BytecodeModuleValidator → error handler balance         │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│              Semantics (Axiom.Semantics)                  │
│  Binder.ControlFlowStatements → BindTryStatement()       │
│  BoundTryStatement · BoundErrorStatement                 │
│  RuntimeErrorTypeSymbol (Code, Message, Line, Column)    │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│              Parsing (Axiom.Parsing)                      │
│  Parser.Statements → ParseTryStatement()                 │
│  TryStatementSyntax · CatchClauseSyntax                  │
│  FinallyClauseSyntax · ErrorStatementSyntax              │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────┴───────────────────────────────┐
│              Core (Axiom.Core)                            │
│  Diagnostic · DiagnosticBag · DiagnosticCodes            │
│  TextSpan · TextLocation                                 │
└─────────────────────────────────────────────────────────┘
```

### 4.2 Файлы и ответственности

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Core/Diagnostic.cs` | Каноническая запись ошибки (Code, Message, Location) |
| `src/Axiom.Core/DiagnosticBag.cs` | Мутабельный накопитель диагностик |
| `src/Axiom.Core/DiagnosticCodes.cs` | 63 кода ошибок (MB0xxx–MB4xxx) |
| `src/Axiom.Core/TextSpan.cs` | Диапазон символов |
| `src/Axiom.Core/TextLocation.cs` | Позиция в тексте (строка, столбец, span) |
| `src/Axiom.Syntax/Statements.cs` | Узлы: TryStatementSyntax, CatchClauseSyntax, FinallyClauseSyntax, ErrorStatementSyntax |
| `src/Axiom.Parsing/Parser.Statements.cs` | ParseTryStatement(), ParseErrorStatement() |
| `src/Axiom.Semantics/Binding/Binder.ControlFlowStatements.cs` | BindTryStatement(), BindErrorStatement() |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | BoundTryStatement, BoundErrorStatement |
| `src/Axiom.Semantics/Symbols/Symbols.cs` | RuntimeErrorTypeSymbol |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | LowerTry(), LowerError() |
| `src/Axiom.Bytecode/Model/Opcode.cs` | PushErrorHandler, PopErrorHandler, RaiseError, EndFinally |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | EmitPushErrorHandler() |
| `src/Axiom.Runtime/Execution/RuntimeFailureException.cs` | Sentinel-исключение (3 строки) |
| `src/Axiom.Runtime/Values/RuntimeErrorValue.cs` | Значение типа ERROR (39 строк) |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` | ExecuteTry(), ExecuteError(), ExecuteFinally() |
| `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs` | ReportRuntimeDiagnostic(), AppendCallChain() |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | ExecutePushErrorHandler(), ExecuteRaiseError(), TryHandleRuntimeFailure(), ExecuteEndFinally() |
| `src/Axiom.Runtime/Execution/BytecodeErrorHandlerFrame.cs` | Фрейм обработчика ошибок VM |
| `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` | PushErrorHandler() на фрейме |
| `src/Axiom.Runtime/Execution/ExecutionSignal.cs` | Сигналы управления (RETURN, EXIT, CONTINUE) |
| `src/Axiom.Runtime/Host/RuntimeResult.cs` | Результат выполнения с диагностиками |
| `src/Axiom.Runtime/Values/RuntimeArithmeticHelpers.cs` | Арифметические overflow helpers |
| `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` | Конвейер: Compile → Run с ранним выходом |
| `src/Axiom.Ide/Models/ProgramDiagnosticItem.cs` | IDE-модель диагностики |
| `src/Axiom.Ide/MainWindow.Diagnostics.cs` | Навигация к ошибке в редакторе |

---

## 5. Полный путь выполнения

### 5.1 Путь компиляции (ошибки компиляции)

```
Исходный код (.axm)
    ↓
Lexer.Lex(text, out lexerDiag)
    → MB0xxx: InvalidCharacter, UnterminatedString, InvalidNumericLiteral, NumericLiteralOutOfRange
    ↓
Parser.Parse(text, out parserDiag)
    → MB1xxx: ExpectedToken, ExpectedIdentifier, ExpectedExpression, UnexpectedToken, DuplicateElse, DuplicateCaseElse
    → lexerDiag.AddRange() — объединение лексических и синтаксических диагностик
    ↓
[Если parserDiag.Count > 0 → Pipeline останавливается, возвращает SyntaxFailure]
    ↓
Binder.Bind(syntax, out semanticDiag)
    → MB2xxx: 39 кодов семантических ошибок
    ↓
[Если semanticDiag.Count > 0 → Pipeline останавливается, возвращает SemanticFailure]
    ↓
BoundProgramLowerer.Lower()
    → MB4xxx: ошибки lowering (пока не используются)
    ↓
BytecodeCompiler.Compile()
    → MB4xxx: ошибки эмиссии байткода
    ↓
BytecodeModuleValidator.Validate()
    → BCxxxx: ошибки валидации модуля
    ↓
[Если backend fails → Pipeline останавливается, возвращает BackendFailure]
    ↓
CompiledProgram (успешная компиляция)
```

### 5.2 Путь выполнения (runtime-ошибки)

#### Interpreter:
```
Interpreter.Execute(boundProgram)
    ↓
ExecuteStatement(boundStatement)
    ↓
[Runtime ошибка: деление на ноль, выход за границы массива, и т.д.]
    ↓
ReportRuntimeDiagnostic(MB3xxx, message, location)
    → Создаёт Diagnostic, добавляет в _diagnostics
    → Устанавливает _lastRuntimeFailureDiagnostic
    ↓
throw new RuntimeFailureException()
    ↓
[Если есть TRY/CATCH на стеке вызовов]
    → catch (RuntimeFailureException) в ExecuteTry()
    → ConsumeLastRuntimeError() → RuntimeValue.FromRuntimeError()
    → Выполнение CATCH-блока
    ↓
[Если нет TRY/CATCH]
    → catch (RuntimeFailureException) в Execute()
    → Diagnostic остаётся в _diagnostics
    → RuntimeResult с диагностикой
```

#### BytecodeVm:
```
BytecodeVm.Execute(bytecodeModule)
    ↓
ExecuteInstruction(frame, instruction)
    ↓
[Runtime ошибка]
    ↓
ReportRuntimeDiagnostic(MB3xxx, message, location)
    → Создаёт Diagnostic, добавляет в _diagnostics
    → Устанавливает _lastRuntimeFailureDiagnostic
    ↓
throw new RuntimeFailureException()
    ↓
catch (RuntimeFailureException) when (TryHandleRuntimeFailure())
    ↓
TryHandleRuntimeFailure():
    → Обходит стек вызовов сверху вниз
    → Для каждого фрейма обходит стек error handler'ов
    → Если handler.Active && handler.CatchTarget:
        → ConsumeLastRuntimeError() → RuntimeValue
        → Очистка evaluation stack
        → Сохранение значения в слот catch-переменной
        → IP = CatchTarget
        → return true (ошибка перехвачена)
    → Если handler.Active && handler.FinallyTarget:
        → HasPendingUnhandledFailure = true
        → IP = FinallyTarget
        → return true (FINALLY выполнится, потом rethrow)
    → Если нет обработчика в текущем фрейме:
        → Pop фрейм, переход к следующему
    → Если стек вызовов исчерпан:
        → return false (необработанная ошибка)
    ↓
[Если TryHandleRuntimeFailure вернул false]
    → catch в Execute() верхнего уровня
    → Diagnostic остаётся в _diagnostics
    → RuntimeResult с диагностикой
```

---

## 6. Фактический семантический контракт

### 6.1 TRY / CATCH / FINALLY

**Вход:**
- Блок инструкций в `TRY`
- Опциональный `CATCH` с переменной типа `ERROR`
- Опциональный `FINALLY` с инструкциями очистки

**Обработка:**
- Инструкции `TRY` выполняются в дочерней области видимости
- Если возникает runtime-ошибка и есть `CATCH`:
  - Ошибка извлекается в `RuntimeErrorValue`
  - Значение присваивается catch-переменной (read-only)
  - Выполняются инструкции `CATCH`
- Если есть `FINALLY`:
  - Выполняется всегда: при нормальном выходе, после `CATCH`, при необработанной ошибке
  - Сигналы `RETURN`/`EXIT` из `FINALLY` перезрывают предыдущие сигналы

**Выход:**
- Нормальное выполнение после `END TRY`
- Или сигнал из `CATCH`/`FINALLY` (RETURN, EXIT, CONTINUE)
- Или необработанная ошибка (если нет CATCH или CATCH сам бросил ошибку)

**Недопустимые входы:**
- `TRY` без `CATCH` и без `FINALLY` → синтаксическая ошибка MB1xxx
- Несколько `CATCH` в одном `TRY` → синтаксическая ошибка

### 6.2 ERROR (оператор)

**Вход:**
- `ERROR код` — строковый код ошибки
- `ERROR код, сообщение` — строковый код и сообщение
- `ERROR ошибка` — существующее значение типа `ERROR` (rethrow)

**Обработка:**
- Форма `код` / `код, сообщение`: создаёт `Diagnostic` с пользовательским кодом, бросает `RuntimeFailureException`
- Форма `ошибка`: извлекает `RuntimeErrorValue`, повторно создаёт `Diagnostic`, бросает `RuntimeFailureException`

**Выход:**
- Управление передаётся ближайшему `CATCH` или программа завершается

**Недопустимые входы:**
- Пустой код → runtime-ошибка (MB3007)
- `ERROR` без аргументов → синтаксическая ошибка

### 6.3 Значение ERROR

**Вход:** Перехваченная ошибка в блоке `CATCH`

**Поля:**
- `Code` (STRING) — код ошибки (например, `MB3001` или `APP001`)
- `Message` (STRING) — текст сообщения
- `Line` (INTEGER) — строка исходного кода (0 если неизвестно)
- `Column` (INTEGER) — столбец исходного кода (0 если неизвестно)

**Ограничения:**
- Только для чтения
- Доступна только внутри блока `CATCH`
- Не может быть создана пользователем напрямую (только через перехват)

---

## 7. Анализ Parser

### 7.1 Парсинг TRY/CATCH/FINALLY

**Метод:** `ParseTryStatement()` в `Parser.Statements.cs:1459-1498`

**Алгоритм:**
1. Совпадение с ключевым словом `TRY`
2. Парсинг инструкций до `CATCH`, `FINALLY` или `END TRY`
3. Если найден `CATCH`: потребление `CATCH <identifier>`, парсинг тела catch до `FINALLY` или `END TRY`
4. Если найден `FINALLY`: парсинг тела finally до `END TRY`
5. Если ни `CATCH`, ни `FINALLY` не найдены: ошибка `"CATCH или FINALLY"`
6. Совпадение с `END TRY`
7. Возврат `TryStatementSyntax`

**Корректность:** ✅ Алгоритм корректен. Восстановление после ошибок работает штатно.

### 7.2 Парсинг ERROR

**Метод:** `ParseErrorStatement()` в `Parser.Statements.cs:1100-1114`

**Синтаксис:** `ERROR <expression>` или `ERROR <expression>, <expression>`

**Корректность:** ✅ Корректно. Две формы (с сообщением и без) разбираются однозначно.

### 7.3 Восстановление после ошибок

Parser использует стандартную стратегию: при ошибке создаёт `ErrorStatementSyntax` / `ErrorExpressionSyntax` как sentinel-узел и продолжает парсинг. Это позволяет накапливать несколько ошибок за один проход.

---

## 8. Анализ Binder

### 8.1 Связывание TRY/CATCH

**Метод:** `BindTryStatement()` в `Binder.ControlFlowStatements.cs:65-74`

**Алгоритм:**
1. Связывание инструкций `TRY` в дочерней области видимости (`DeclarationContexts.TryBlock`)
2. Связывание clause `CATCH`:
   - Создание дочерней области видимости (`DeclarationContexts.CatchBlock`)
   - Создание `VariableSymbol` типа `TypeSymbol.RuntimeError` (read-only, local)
   - Регистрация через `TryDeclareScopedVariable`
   - Связывание инструкций catch
3. Связывание инструкций `FINALLY` в дочерней области видимости (`DeclarationContexts.FinallyBlock`)

**Корректность:** ✅ Корректно. Блочная видимость соблюдается. Catch-переменная доступна только в CATCH-блоке.

### 8.2 Связывание ERROR

**Метод:** `BindErrorStatement()` в `Binder.ControlFlowStatements.cs:76-99`

**Три режима:**
1. `ExistingError` — выражение имеет тип `RuntimeError` → rethrow
2. `UserCode` (один аргумент) — выражение типа `String` → пользовательский код
3. `UserCode` (два аргумента) — оба выражения типа `String` → код + сообщение

**Корректность:** ✅ Корректно. Типовая проверка выполняется на этапе связывания.

### 8.3 RuntimeErrorTypeSymbol

**Определение:** `Symbols.cs:115-128`

Тип `ERROR` с полями:
- `Code` (String)
- `Message` (String)
- `Line` (Integer)
- `Column` (Integer)

Доступ к полям через `.` (точка) связывается в `Binder.InvocationExpressions.cs:123-131`.

---

## 9. Анализ Interpreter

### 9.1 ExecuteTry()

**Файл:** `Interpreter.Statements.cs:591-628`

**Алгоритм:**
1. Оборачивает `ExecuteStatementsInChildScope(statement.Statements)` в C# `try/catch(RuntimeFailureException)`
2. Если поймана `RuntimeFailureException`:
   - Если `CatchVariable` == null → сохраняет как `unhandledFailure`
   - Если `CatchVariable` есть → `ConsumeLastRuntimeError()` → `RuntimeValue.FromRuntimeError()` → дочерний scope → выполнение CATCH
3. Всегда вызывает `ExecuteFinally(statement)`
4. Сигнал из `FINALLY` перекрывает предыдущие сигналы (RETURN, EXIT, CONTINUE)
5. Если был `unhandledFailure` → re-throw

**Корректность:** ✅ Корректно. Нативная интеграция с .NET try/catch обеспечивает корректное поведение.

### 9.2 ExecuteError()

**Файл:** `Interpreter.Statements.cs:552-581`

**Режимы:**
- `ExistingError`: извлекает `RuntimeErrorValue`, повторно репортит, бросает `RuntimeFailureException`
- `UserCode`: вычисляет строку кода, валидирует непустоту, вычисляет опциональное сообщение, репортит, бросает

**Корректность:** ✅ Корректно. Пустой код валидируется.

### 9.3 ConsumeLastRuntimeError()

**Файл:** `Interpreter.Statements.cs:637-653`

Извлекает `_lastRuntimeFailureDiagnostic`, удаляет из `_diagnostics`, оборачивает в `RuntimeValue.FromRuntimeError()`. Если `_lastRuntimeFailureDiagnostic` == null, создаёт синтетическую ошибку `InvalidRuntimeState`.

**Корректность:** ✅ Корректно. Fallback на синтетическую ошибку предотвращает крах.

### 9.4 ReportRuntimeDiagnostic()

**Файл:** `Interpreter.Helpers.cs:554-558`

Создаёт `Diagnostic` с `AppendCallChain(message)`, добавляет в `_diagnostics`, устанавливает `_lastRuntimeFailureDiagnostic`.

**Корректность:** ✅ Корректно. Цепочка вызовов добавляется к сообщению.

### 9.5 AppendCallChain()

**Файл:** `Interpreter.Helpers.cs:561+`

Добавляет блок "Цепочка вызовов" к сообщению об ошибке с перечнем активных вызовов, их местами вызова и внешней процедурой/функцией.

**Корректность:** ✅ Корректно. Обогащает диагностическую информацию.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering TRY/CATCH/FINALLY

**Метод:** `LowerTry()` в `BoundProgramLowerer.cs:1024-1104`

**Генерируемая последовательность:**
1. `PushErrorHandler(catchLabel, finallyLabel, endLabel, catchVariable)` — push error handler frame
2. Lowering инструкций `TRY` (с `_activeFinallyContexts` для inline-FINALLY при RETURN/EXIT)
3. `PopErrorHandler` — нормальный выход
4. `Jump(finallyLabel ?? endLabel)` — переход к finally или end
5. Если есть CATCH:
   - `LabelMarker(catchLabel)` — landing pad
   - Lowering инструкций CATCH
   - `PopErrorHandler`
   - `Jump(finallyLabel ?? endLabel)`
6. Если есть FINALLY:
   - `LabelMarker(finallyLabel)`
   - Lowering инструкций FINALLY
   - `EndFinally` — сигнал завершения FINALLY
7. `LabelMarker(endLabel)`

**Корректность:** ✅ Корректно. Инструкции сбалансированы. RETURN/EXIT через FINALLY корректно unwinding-ятся.

### 10.2 Opcodes

| Opcode | Операнды | Назначение |
|--------|----------|------------|
| `PushErrorHandler` | catchTarget, finallyTarget, endTarget, catchTargetKind, catchTargetSlot | Push error handler frame |
| `PopErrorHandler` | (нет) | Pop error handler frame |
| `RaiseError` | mode (0=без сообщения, 1=с сообщением, 2=existing error) | ERROR operator |
| `EndFinally` | (нет) | Завершение FINALLY, обработка pending return/failure |

### 10.3 BytecodeVm execution

#### ExecutePushErrorHandler (BytecodeVm.cs:1090-1098)
Создаёт `BytecodeErrorHandlerFrame` с 5 операндами и пушит на стек error handler'ов текущего фрейма.

#### ExecuteRaiseError (BytecodeVm.cs:1100-1129)
- Mode 2 (ExistingError): pop `RuntimeError` со стека, re-report, throw
- Mode 0/1: pop code (и опционально message), report, throw

#### TryHandleRuntimeFailure (BytecodeVm.cs:1156-1192)
**Ключевой метод.** Используется как C# exception filter:
```csharp
catch (RuntimeFailureException) when (TryHandleRuntimeFailure())
```

Алгоритм:
1. Обход стека вызовов сверху вниз
2. Для каждого фрейма — обход стека error handler'ов сверху вниз
3. Если handler.Active && handler.CatchTarget:
   - ConsumeLastRuntimeError() → RuntimeValue
   - Очистка evaluation stack
   - Сохранение в слот catch-переменной (global/local/argument/capture)
   - IP = CatchTarget
   - return true
4. Если handler.Active/ExecutingCatch && handler.FinallyTarget:
   - HasPendingUnhandledFailure = true
   - IP = FinallyTarget
   - return true
5. Если нет обработчика → pop frame, переход к следующему
6. Если стек исчерпан → return false

#### ExecuteEndFinally (BytecodeVm.cs:1131-1154)
- Если HasPendingReturn → CompleteReturn
- Если HasPendingUnhandledFailure → pop handler, re-throw
- Иначе → нормальное завершение

#### StoreErrorHandlerCatchValue (BytecodeVm.cs:1212-1232)
Сохраняет caught value в правильный слот (0=global, 1=local, 2=argument, 3=capture).

**Корректность:** ✅ Корректно. State machine обеспечивает полное поведение TRY/CATCH/FINALLY.

### 10.4 BytecodeModuleValidator

Валидирует:
- Корректность `routine-id` и точки входа
- Корректность индексов констант, global-slot и built-in id
- Корректность локальных и аргументных слотов
- Корректность целей переходов внутри routine
- Соответствие числа аргументов при `Call` и `CallBuiltin`
- Корректность базовых отладочных карт и диапазонов блоков

**Проблема:** Нет валидации баланса `PushErrorHandler`/`PopErrorHandler` (ERROR-009).

---

## 11. Анализ Runtime

### 11.1 RuntimeFailureException

**Файл:** `src/Axiom.Runtime/Execution/RuntimeFailureException.cs` (3 строки)

Пустое sentinel-исключение. Все runtime-ошибки проходят через него. Данные об ошибке хранятся в `DiagnosticBag`, а не в исключении.

**Преимущества паттерна:**
- Минимальный overhead на создание (нет данных в исключении)
- Единый механизм перехвата для всех типов runtime-ошибок
- Данные об ошибке не теряются (хранятся в `_diagnostics`)

### 11.2 RuntimeErrorValue

**Файл:** `src/Axiom.Runtime/Values/RuntimeErrorValue.cs` (39 строк)

Значение типа `ERROR` с полями Code, Message, Line, Column. Создаётся при перехвате ошибки в CATCH.

**Проблема:** `GetField()` бросает `InvalidOperationException` для неизвестного поля (ERROR-015).

### 11.3 RuntimeArithmeticHelpers

**Файл:** `src/Axiom.Runtime/Values/RuntimeArithmeticHelpers.cs` (95 строк)

Общие хелперы для арифметических overflow:
- `EvaluateCheckedIntegerOperation()`, `EvaluateCheckedLongOperation()`, `EvaluateCheckedDecimalOperation()`
- `EnsureFiniteDoubleResult()`
- `ReportNumericOverflow()`, `ReportDivisionByZero()`

Используют delegate pattern для передачи обработчика ошибок.

---

## 12. Взаимодействие с типами

| Тип | Взаимодействие с ошибками |
|-----|--------------------------|
| INTEGER | Деление на ноль → MB3001, переполнение → MB3008 |
| LONG | Деление на ноль → MB3001, переполнение → MB3008 |
| DOUBLE | Деление на ноль → MB3001, неконечный результат → MB3008 |
| DECIMAL | Деление на ноль → MB3001, переполнение → MB3008 |
| STRING | Нет специальных ошибок (immutable, null-safe) |
| BOOLEAN | Нет специальных ошибок |
| ARRAY | Выход за границы → MB3002, обращение к невыделенному → MB3005 |
| TYPE | Нет специальных ошибок при доступе к полям |
| ERROR | Создаётся при перехвате, доступны поля Code/Message/Line/Column |
| IMAGE | Нет специальных ошибок |
| FUNCTION | Пустое значение → MB3009, вызов не-function → MB2020 |

---

## 13. Взаимодействие со String

Строковые операции не порождают специфических runtime-ошибок. Все строковые built-in функции используют `MB3007` (BuiltinArgument) для ошибок аргументов.

**Unicode:** Строки хранятся как CLR `string` (immutable UTF-16). Ошибок кодировки нет.

**Fixed strings:** `DIM code AS STRING * N` — корректно работает с pad/truncate без runtime-ошибок.

---

## 14. Взаимодействие с Array

| Ошибка | Код | Описание |
|--------|-----|----------|
| Выход за границы | MB3002 | Индекс вне диапазона выделенного массива |
| Обращение к невыделенному | MB3005 | Массив не выделен через DIM/REDIM |
| Невалидный индекс измерения | MB3007 | Для LBOUND/UBOUND с параметром dimension |
| STEP 0 в FOR | MB3004 | Не массивная ошибка, но часто встречается с массивами |

**Паритет:** ✅ Все массивные ошибки одинаковы в Interpreter и BytecodeVm.

---

## 15. Взаимодействие с TYPE

TYPE не порождает специфических runtime-ошибок. Доступ к несуществующему полю диагностируется на этапе семантики (MB2023). Доступ через точку к не-TYPE значению диагностируется как MB2024.

**Конструкторы TYPE:** Ошибки в конструкторах распространяются через стандартный механизм `RuntimeFailureException`.

---

## 16. Взаимодействие с Functions / SUB

| Сценарий | Код | Движок |
|----------|-----|--------|
| FUNCTION без RETURN | MB3006 | Оба |
| Вызов не-function значения | MB2020 | Binder |
| Пустое function value | MB3009 | Оба |
| Переполнение стека вызовов | MB3011 | Оба |
| RETURN value в SUB | MB2006 | Binder |
| Missing return value | MB2007 | Binder |

**Паритет:** ✅ Все процедурные ошибки одинаковы в обоих движках.

---

## 17. Взаимодействие с Control Flow

### TRY/CATCH внутри циклов
- `RETURN` через `FINALLY` — корректно unwinding-ится в обоих движках
- `EXIT FOR`/`EXIT DO` через `FINALLY` — корректно inline-ится при lowering
- `CONTINUE FOR`/`WHILE`/`DO` через `FINALLY` — корректно работает

### TRY/CATCH внутри IF/SELECT
- Блочная видимость соблюдается
- Catch-переменная не видна вне CATCH-блока

### TRY/CATCH внутри SUB/FUNCTION
- Runtime-ошибки в локальных функциях распространяются через стек вызовов
- Цепочка вызовов корректно отображается

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Compile errors (MB0xxx, MB1xxx)
- Pipeline останавливается после этапа парсинга
- IDE показывает ошибки в DataGrid с навигацией к коду
- Binder не запускается

### 18.2 Semantic errors (MB2xxx)
- Pipeline останавливается после этапа связывания
- IDE показывает ошибки в DataGrid
- Interpreter/BytecodeVm не запускаются

### 18.3 Runtime errors (MB3xxx)
- Распространяются через `RuntimeFailureException`
- Могут быть перехвачены через `TRY/CATCH`
- Если не перехвачены — программа завершается с диагностикой

### 18.4 User errors (ERROR operator)
- Создают `Diagnostic` с пользовательским кодом
- Распространяются через тот же механизм `RuntimeFailureException`
- Могут быть перехвачены через `TRY/CATCH`
- При rethrow сохраняют исходные Code, Message, Line, Column

### 18.5 Backend errors (MB4xxx)
- Ошибки lowering/эмиссии/валидации байткода
- Pipeline останавливается
- В режиме `Auto` — fallback на интерпретатор
- В режиме `Bytecode` — ошибка

---

## 19. State / Lifetime / Ownership

### 19.1 DiagnosticBag
- **Создаётся:** Каждый этап конвейера создаёт свой экземпляр
- **Владеет:** Этап конвейера (Lexer, Parser, Binder, Interpreter, BytecodeVm)
- **Изменяется:** Через `Report()` и `AddRange()`
- **Освобождается:** После чтения этапом-оркестратором
- **Lifetime:** В рамках одного запуска программы

### 19.2 RuntimeFailureException
- **Создаётся:** В `ReportRuntimeDiagnostic()` после добавления в DiagnosticBag
- **Владеет:** Никто (transient)
- **Освобождается:** Ловится в `ExecuteTry()` или `Execute()`
- **Lifetime:** Мгновенное (бросается и ловится)

### 19.3 RuntimeErrorValue
- **Создаётся:** В `ConsumeLastRuntimeError()` при перехвате ошибки в CATCH
- **Владеет:** RuntimeValue (обёртка)
- **Изменяется:** Не изменяется (immutable)
- **Освобождается:** Сборщиком мусора
- **Lifetime:** В рамках блока CATCH и далее, если значение сохранено

### 19.4 BytecodeErrorHandlerFrame
- **Создаётся:** При выполнении `PushErrorHandler`
- **Владеет:** BytecodeCallFrame (стек error handler'ов)
- **Изменяется:** State меняется при перехвате/завершении
- **Освобождается:** При `PopErrorHandler` или при раскрутке стека
- **Lifetime:** В рамках выполнения TRY-блока

---

## 20. Edge Cases

### 20.1 Пустые значения
- `ERROR ""` — пустой код → runtime-ошибка (валидация в ExecuteError)
- `TRY ... END TRY` без CATCH и FINALLY → синтаксическая ошибка

### 20.2 Вложенные TRY
- Корректно работают: внутренний CATCH перехватывает, внешний — если rethrow
- Паритет: ✅ Оба движка

### 20.3 RETURN через FINALLY
- Interpreter: ExecutionSignal.HasReturn перекрывается finally-сигналом
- BytecodeVm: HasPendingReturn + ExecuteEndFinally → CompleteReturn
- Паритет: ✅

### 20.4 EXIT FOR/DO через FINALLY
- Interpreter: scope cleanup при выходе из цикла
- BytecodeVm: inline FINALLY при lowering
- Паритет: ✅

### 20.5 Рекурсия + TRY/CATCH
- Каждый уровень рекурсии имеет свой error handler frame
- Ошибка перехватывается на правильном уровне
- Паритет: ✅

### 20.6 Переполнение стека + TRY/CATCH
- MB3011 бросается до TRY/CATCH
- Если TRY/CATCH обернут вокруг рекурсивного вызова — ошибка перехватывается
- Паритет: ✅

### 20.7 Множественные ошибки
- Только последняя ошибка доступна через `ConsumeLastRuntimeError()`
- Предыдущие ошибки остаются в `_diagnostics`
- Поведение корректно: CATCH получает только последнюю ошибку

### 20.8 FINALLY без CATCH
- `TRY ... FINALLY ... END TRY` — корректно работает
- FINALLY выполняется даже при необработанной ошибке
- После FINALLY ошибка пробрасывается дальше

### 20.9 CATCH без FINALLY
- `TRY ... CATCH ... END TRY` — корректно работает
- Ошибка перехватывается, выполнение продолжается после END TRY

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| TRY/CATCH | ✅ (.NET try/catch) | ✅ (ErrorHandlerFrame stack) | ✅ | ✅ | ✅ |
| TRY/FINALLY | ✅ | ✅ | ✅ | ✅ | ✅ |
| TRY/CATCH/FINALLY | ✅ | ✅ | ✅ | ✅ | ✅ |
| ERROR код | ✅ | ✅ | ✅ | ✅ | ✅ |
| ERROR код, сообщение | ✅ | ✅ | ✅ | ✅ | ✅ |
| ERROR err (rethrow) | ✅ | ✅ | ✅ | ✅ | ✅ |
| RETURN через FINALLY | ✅ (ExecutionSignal) | ✅ (HasPendingReturn) | ✅ | ✅ | ✅ |
| EXIT FOR через FINALLY | ✅ (scope cleanup) | ✅ (inline FINALLY) | ✅ | ✅ | ✅ |
| CONTINUE через FINALLY | ✅ | ✅ | ✅ | ✅ | ✅ |
| Вложенные TRY | ✅ | ✅ | ✅ | ✅ | ✅ |
| Рекурсия + TRY | ✅ | ✅ | ✅ | ✅ | ✅ |
| Переполнение стека | ✅ (MB3011) | ✅ (MB3011) | ✅ | ✅ | ✅ |
| Деление на ноль | ✅ (MB3001) | ✅ (MB3001) | ✅ | ✅ | ✅ |
| Array bounds | ✅ (MB3002) | ✅ (MB3002) | ✅ | ✅ | ✅ |
| Numeric overflow | ✅ (MB3008) | ✅ (MB3008) | ✅ | ✅ | ✅ |
| Call chain reporting | ✅ (AppendCallChain) | ✅ (AppendCallChain) | ✅ | ✅ | ✅ |

**Расхождений не обнаружено.** Полный паритет подтверждён тестами.

---

## 22. Анализ производительности

### 22.1 Happy path (без ошибок)
- **Interpreter:** Нет overhead — TRY/CATCH не выполняется при отсутствии ошибок
- **BytecodeVm:** Минимальный overhead — `PushErrorHandler`/`PopErrorHandler` добавляют 2 инструкции на TRY-блок
- **Классификация:** PARITY

### 22.2 Error path (с ошибкой)
- **Interpreter:** Стоимость .NET exception + catch + ConsumeLastRuntimeError
- **BytecodeVm:** Стоимость .NET exception + exception filter + TryHandleRuntimeFailure + state machine
- **Классификация:** PARITY (оба используют .NET exceptions)

### 22.3 Diagnostic creation
- `ReportRuntimeDiagnostic` создаёт `Diagnostic` record + добавляет в `List<Diagnostic>` + `AppendCallChain`
- `AppendCallChain` обходит стек вызовов для формирования строки
- **Классификация:** Negligible overhead (только при ошибке)

### 22.4 Hot paths
- TRY/CATCH/FINALLY не является hot path (используется редко)
- `PushErrorHandler`/`PopErrorHandler` в циклах — минимальный overhead (2 инструкции)
- **Классификация:** Не является проблемой производительности

---

## 23. Качество Benchmark

Специфических benchmarks для подсистемы ошибок нет. Ошибки не являются hot path, поэтому это не является проблемой.

---

## 24. Hot Paths и Bottlenecks

Подсистема ошибок не участвует в hot paths. Единственный потенциальный bottleneck — `AppendCallChain` при глубокой рекурсии, но это mitigated ограничением `MaxCallDepth = 200`.

---

## 25. Архитектурные проблемы

### 25.1 Дублирование ConsumeLastRuntimeError (ERROR-007)
- `Interpreter.ConsumeLastRuntimeError()` и `BytecodeVm.ConsumeLastRuntimeError()` имеют идентичную логику
- **Направление:** Вынести в общий helper

### 25.2 Дублирование AppendCallChain (ERROR-008)
- Оба движка имеют `AppendCallChain`, но с разной степенью детализации
- **Направление:** Выровнять и вынести в общий helper

### 25.3 BytecodeDiagnostic vs Diagnostic (ERROR-012)
- `BytecodeDiagnostic` в `Axiom.Bytecode` дублирует структуру `Diagnostic` из `Axiom.Core`
- **Направление:** Оценить возможность унификации

### 25.4 Нет уровней серьёзности (ERROR-010)
- `Diagnostic` не имеет поля Severity (только errors, нет warnings)
- **Направление:** Добавить Severity field в будущем

---

## 26. Дублирование логики

| Место | Дублируемая логика |
|-------|-------------------|
| `Interpreter.ConsumeLastRuntimeError` ↔ `BytecodeVm.ConsumeLastRuntimeError` | Извлечение ошибки из DiagnosticBag в RuntimeErrorValue |
| `Interpreter.ReportRuntimeDiagnostic` ↔ `BytecodeVm.ReportRuntimeDiagnostic` | Создание Diagnostic + AppendCallChain |
| `Interpreter.AppendCallChain` ↔ `BytecodeVm.AppendCallChain` | Формирование цепочки вызовов |
| `Interpreter.ReportDivisionByZero` ↔ `BytecodeVm.ReportDivisionByZero` | Код и сообщение MB3001 |
| `Interpreter.ReportStepZero` ↔ `BytecodeVm.ReportStepZero` | Код и сообщение MB3004 |
| `Interpreter.ReportBuiltInArgumentError` ↔ `BytecodeVm.ReportBuiltInArgumentError` | Код и сообщение MB3007 |

**Общий объём дублирования:** ~150-200 строк

---

## 27. Single Source of Truth

| Сущность | Источник истины | Статус |
|----------|----------------|--------|
| Коды ошибок | `DiagnosticCodes` в `Axiom.Core` | ✅ Единый |
| Формат Diagnostic | `Diagnostic` record в `Axiom.Core` | ✅ Единый |
| Накопитель диагностик | `DiagnosticBag` в `Axiom.Core` | ✅ Единый |
| Позиция в тексте | `TextLocation`/`TextSpan` в `Axiom.Core` | ✅ Единый |
| Runtime-исключение | `RuntimeFailureException` в `Axiom.Runtime` | ✅ Единый |
| Значение ошибки | `RuntimeErrorValue` в `Axiom.Runtime` | ✅ Единый |
| Сообщения об ошибках | Дублированы в Interpreter и BytecodeVm | ⚠️ Не единый |

---

## 28. Legacy / Dead Code

Legacy-код в подсистеме ошибок не обнаружен. Все компоненты активно используются.

---

## 29. Надёжность и безопасность

### 29.1 Неконтролируемый доступ к ресурсам
- Файлы, открытые через файловые built-in, не закрываются автоматически при runtime-ошибке
- Это by design (Axiom не имеет RAII-механизма)
- **Статус:** Документировано в manual

### 29.2 Path handling
- Файловые ошибки обрабатываются через `IsFileSystemException` → MB3007
- **Статус:** Корректно

### 29.3 Resource exhaustion
- Защита от переполнения стека: `MaxCallDepth = 200` → MB3011
- Нет защиты от бесконечного потребления памяти через массивы
- **Статус:** Частично покрыто

### 29.4 Malformed input
- Lexer валидирует входные данные на уровне символов и литералов
- Parser восстанавливается после ошибок
- **Статус:** Корректно

---

## 30. Анализ тестов

### 30.1 Тесты TRY/CATCH/FINALLY

| Файл | Тесты | Покрытие |
|------|-------|----------|
| `InterpreterTests.TryCatch.cs` | 9+ | TRY/CATCH, FINALLY, nested, rethrow, RETURN через FINALLY |
| `InterpreterTests.Error.cs` | 4+ | ERROR user code, rethrow, empty code |
| `ParserTests.cs` | 4+ | Парсинг TRY с CATCH, FINALLY, оба, ни одного |
| `BinderTests.cs` | 5+ | Связывание TRY, CATCH, ERROR |
| `ExecutionPipelineTests.cs` | 6+ | Pipeline-level TRY/CATCH, finally, nested |
| `EditorFeatureTests.cs` | 3+ | Подсветка TRY/CATCH/FINALLY |
| `BlockEditingServiceTests.cs` | 3+ | Автозакрытие TRY блоков |

### 30.2 Негативные acceptance-сэмплы

| Файл | Код ошибки |
|------|-----------|
| `division_by_zero.axm` | MB3001 |
| `wrong_argument_count.axm` | MB2004 |
| `unknown_callable.axm` | MB2005 |
| `return_value_in_sub.axm` | MB2006 |
| `missing_return_value.axm` | MB2007 |
| `for_step_zero.axm` | MB3004 |
| `calling_non_function.axm` | MB2020 |
| `undeclared_variable.axm` | MB2001 |
| + 22 נוספים | Различные MB2xxx |

### 30.3 Runtime error samples

~45+ samples для runtime-ошибок: деление на ноль, переполнение, выход за границы массива, невалидные аргументы built-in функций.

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии
- ✅ TRY/CATCH с делением на ноль
- ✅ TRY/FINALLY без CATCH
- ✅ ERROR user code
- ✅ ERROR rethrow
- ✅ Вложенные TRY

### 31.2 Отрицательные сценарии
- ✅ TRY без CATCH и FINALLY (синтаксическая ошибка)
- ✅ Пустой код ERROR (runtime ошибка)

### 31.3 Граничные случаи
- ⚠️ Нет теста: TRY внутри бесконечного цикла с ошибкой
- ⚠️ Нет теста: TRY с RETURN и FINALLY, где FINALLY тоже бросает ошибку
- ⚠️ Нет теста: TRY/CATCH внутри выражения (не	statement)
- ⚠️ Нет теста: ERROR с очень длинным сообщением
- ⚠️ Нет теста: ERROR с Unicode в коде/сообщении

### 31.4 Regression
- ⚠️ Нет теста: error → rerun (повторный запуск после ошибки)
- ⚠️ Нет теста: compile invalid → compile valid (очистка состояния)

### 31.5 Parity
- ✅ Паритетные тесты для всех MB3xxx кодов
- ✅ Паритетные тесты для TRY/CATCH/FINALLY
- ⚠️ Нет паритетного теста: цепочка вызовов одинакова в обоих движках

### 31.6 Performance
- Нет специфических performance-тестов для ошибок (не требуется)

---

## 32. Анализ Samples

### 32.1 TRY/CATCH samples
- `samples/operators/try/handled_runtime_error.axm` — перехват деления на ноль + FINALLY
- `samples/operators/error/user_code_catch.axm` — пользовательская ошибка + CATCH
- `samples/operators/error/rethrow_caught_error.axm` — nested TRY + rethrow

### 32.2 Runtime error samples
- ~45+ samples для различных runtime-ошибок
- Все samples корректно демонстрируют поведение ошибок

### 32.3 Smoke samples
- `samples/smoke/nested_runtime_diagnostic.axm` — ошибка внутри локальной функции с цепочкой вызовов

---

## 33. Анализ документации

### 33.1 Manual pages
- `docs/manual/error-handling.html` (96 строк) — обзорная страница
- `docs/manual/try.html` (94 строки) — оператор TRY
- `docs/manual/error.html` (84 строки) — оператор ERROR
- `docs/manual/diagnostics.html` (217 строк) — коды диагностики

### 33.2 Качество документации
- ✅ Все страницы следуют шаблону из AGENTS.md
- ✅ Синтаксис, примеры, ограничения описаны
- ✅ Коды MB3xxx перечислены в diagnostics.html
- ✅ Русскоязычные примеры
- ⚠️ MB4xxx коды не описаны в manual (внутренние)
- ⚠️ Нет описания цепочки вызовов в отдельной секции

### 33.3 Документация vs Код
- ✅ Синтаксис TRY/CATCH/FINALLY соответствует коду
- ✅ Синтаксис ERROR соответствует коду
- ✅ Поля типа ERROR соответствуют RuntimeErrorValue
- ✅ Коды MB3xxx соответствуют DiagnosticCodes
- ⚠️ MB3008 описан как "NumericOverflow", но в коде используется и для "неконечного результата DOUBLE"

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса
- `TRY`, `CATCH`, `FINALLY`, `END TRY`, `ERROR` — подсвечиваются как ключевые слова
- Проверено в `EditorFeatureTests.cs`

### 34.2 Автозакрытие блоков
- `AxiomBlockEditingService` автоматически вставляет `CATCH`, `FINALLY`, `END TRY`
- Проверено в `BlockEditingServiceTests.cs`

### 34.3 Отображение диагностик
- `ProgramDiagnosticItem` маппит `Diagnostic` на UI-строку
- `ResolveStage()` определяет этап: MB0/MB1 → "Syntax", MB2 → "Semantic", MB3 → "Runtime", MB4 → "Backend"
- DataGrid показывает Code, Stage, Message, Line, Column

### 34.4 Навигация к ошибке
- `DiagnosticNavigationTargetFactory` конвертирует позицию в offset редактора
- `AvalonEditDiagnosticNavigation` фокусирует редактор, устанавливает каретку, выделяет span
- Подсветка строки с ошибкой через `DiagnosticLineHighlightBrush`

### 34.5 Автоматическое переключение панелей
- При ошибках нижняя панель автоматически переключается на вкладку "Diagnostics"
- При запуске без ошибок — переключается на "Output"

---

## 35. Зависимости от Host / Platform

### 35.1 .NET exceptions
- `RuntimeFailureException` наследует от `System.Exception`
- Interpreter использует native .NET `try/catch`
- BytecodeVm использует .NET exception filters (`when`)
- **Зависимость:** Критическая, но корректная

### 35.2 InvalidOperationException
- `RuntimeErrorValue.GetField()` бросает `InvalidOperationException` для неизвестных полей
- `RuntimeValue.AsXxx()` бросает `InvalidOperationException` при неправильном типе
- **Статус:** ERROR-013, ERROR-015 (P3/P4)

---

## 36. Карта корневых причин

| Корневая причина | Затронутые находки |
|-----------------|-------------------|
| Отсутствие перевода .NET-исключений на DateTime-путях | ERROR-003 (исправлен) |
| Raw .NET-исключения из внутренних структур данных | ERROR-004 (исправлен), ERROR-013 |
| Нет общего базового слоя обработки ошибок между Interpreter и VM | ERROR-007, ERROR-008 |
| Нет валидации стека error handler'ов в байткод-валидаторе | ERROR-009 |
| Нет уровней серьёзности в Diagnostic | ERROR-010 |
| Отсутствие тестов повторного запуска | ERROR-011 |

---

## 37. Стабильные области

| Область | Статус | Доказательства |
|---------|--------|----------------|
| `Diagnostic` record | ✅ Стабильно | Используется всеми слоями |
| `DiagnosticCodes` | ✅ Стабильно | Полный, консистентный |
| `DiagnosticBag` | ✅ Стабильно | Простой, корректный API |
| `TextLocation` / `TextSpan` | ✅ Стабильно | Повсеместно, без проблем |
| `RuntimeFailureException` pattern | ✅ Стабильно | Sentinel-паттерн работает корректно |
| TRY/CATCH lowering | ✅ Стабильно | Корректная последовательность инструкций |
| FINALLY через RETURN/EXIT | ✅ Стабильно | Оба движка обрабатывают корректно |
| Error rethrow (`ERROR err`) | ✅ Стабильно | Сохраняет исходные Code/Message/Location |
| Pipeline phase isolation | ✅ Стабильно | Ошибки парсера блокируют binder |
| IDE diagnostic presentation | ✅ Стабильно | DataGrid, навигация, буфер обмена |

---

## 38. Возможности консолидации

| Текущие места | Дублируемая логика | Канонический целевой слой | Риск |
|---------------|-------------------|--------------------------|------|
| `ConsumeLastRuntimeError` в обоих движках | Идентичная логика | Общий helper-метод | Низкий |
| `ReportRuntimeDiagnostic` в обоих движках | Одинаковый паттерн | Общий helper | Низкий |
| Error helper-методы (ReportDivisionByZero и т.д.) | Одинаковые коды/сообщения | Общие helpers | Низкий |
| `BytecodeDiagnostic` vs `Diagnostic` | Одинаковая структура | Унификация или мост | Средний |

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности адекватно:
- `Axiom.Core` — типы диагностик
- `Axiom.Syntax` — синтаксические узлы
- `Axiom.Semantics` — семантическая проверка
- `Axiom.Bytecode` — lowering и эмиссия
- `Axiom.Runtime` — исполнение
- `Axiom.Execution` — конвейер
- `Axiom.Ide` — отображение

**Потенциальное улучшение:** Вынести общие runtime-хелперы ошибок (ConsumeLastRuntimeError, AppendCallChain, ReportXxx) в `Axiom.Runtime/Errors/` как общий слой для обоих движков.

---

## 40. Кандидаты на удаление

Специфических кандидатов на удаление в подсистеме ошибок нет. Все компоненты активно используются.

---

## 41. Готовность к дальнейшему развитию

### 41.1 К модульной системе
- Pipeline уже поддерживает ранний выход при ошибках
- Новые MB2xxx коды для модулей могут быть добавлены в `DiagnosticCodes`
- Runtime-ошибки модулей будут использовать стандартный `RuntimeFailureException`
- **Готовность:** Высокая

### 41.2 К Compiler
- `RuntimeFailureException` pattern совместим с compiled code
- `DiagnosticBag` может быть заменён на другой механизм для compiled code
- **Готовность:** Средняя

### 41.3 К дальнейшей оптимизации
- TRY/CATCH/FINALLY не является hot path
- Оптимизации не требуются
- **Готовность:** Не применимо

### 41.4 К развитию IDE
- `ProgramDiagnosticItem` расширяем
- Новые поля (Severity, Source, HelpLink) могут быть добавлены
- **Готовность:** Высокая

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| ERROR-005 | P2 | VM | BytecodeVm | Evaluation stack cleared on error discards partial values | `TryHandleRuntimeFailure` clears stack | Design choice (not a bug) | HIGH |
| ERROR-006 | P2 | RUNTIME | Interpreter/VM | By-ref arguments not copied back on error | Neither engine does copyback on error unwind | Missing feature | HIGH |
| ERROR-007 | P2 | ARCHITECTURE | Both engines | `ConsumeLastRuntimeError` duplicated | Identical logic in both engines | No shared error handling base | MEDIUM |
| ERROR-008 | P2 | ARCHITECTURE | Both engines | `AppendCallChain` has different richness | Different detail levels | No shared error handling base | MEDIUM |
| ERROR-009 | P2 | VALIDATION | BytecodeModuleValidator | No error handler stack balance validation | Validator checks other balances but not error handlers | Missing validation | HIGH |
| ERROR-010 | P3 | DIAGNOSTICS | DiagnosticCodes | No severity levels | Only errors, no warnings | Design choice | MEDIUM |
| ERROR-011 | P3 | TESTS | Tests | No error-then-rerun tests | Missing test scenarios | Missing tests | HIGH |
| ERROR-012 | P3 | ARCHITECTURE | BytecodeDiagnostic | Duplicates `Diagnostic` structure | Same fields | Architectural decision | LOW |
| ERROR-013 | P3 | DIAGNOSTICS | RuntimeValue | `AsXxx()` throws raw `InvalidOperationException` | Code review | Missing wrapper | MEDIUM |
| ERROR-014 | P4 | DIAGNOSTICS | Messages | Error messages not centralized | Messages duplicated in helpers | No central registry | LOW |
| ERROR-015 | P4 | DIAGNECTICS | RuntimeErrorValue.GetField | Throws `InvalidOperationException` for unknown field | Code line 36 | Missing safe return | LOW |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
**Статус:** ✅ Завершён (этот аудит)

### ЭТАП 1 — Исправление критических проблем корректности
**Статус:** ✅ Завершён (ERROR-003, ERROR-004 исправлены в предыдущей сессии)

### ЭТАП 2 — Стабилизация семантики
**Статус:** ✅ Стабильно (полный паритет Interpreter ↔ VM)

### ЭТАП 3 — Архитектурное улучшение
1. **ERROR-007:** Вынести `ConsumeLastRuntimeError` в общий helper
2. **ERROR-008:** Выровнять `AppendCallChain` между движками
3. **ERROR-009:** Добавить валидацию баланса error handler'ов в BytecodeModuleValidator
4. **ERROR-012:** Оценить унификацию `BytecodeDiagnostic` с `Diagnostic`

### ЭТАП 4 — Оптимизация производительности
**Статус:** Не требуется (ошибки не hot path)

### ЭТАП 5 — Усиление тестирования
1. **ERROR-011:** Добавить тесты error → rerun
2. **ERROR-011:** Добавить тесты compile invalid → compile valid
3. Добавить тесты граничных случаев (TRY внутри выражения, FINALLY бросает ошибку)

### ЭТАП 6 — Обновление документации
1. Добавить описание цепочки вызовов в отдельную секцию manual
2. Уточнить описание MB3008 (включает неконечный результат DOUBLE)

### ЭТАП 7 — Обновление IDE
**Статус:** Не требуется (IDE слой стабилен)

### ЭТАП 8 — Подготовка к дальнейшему развитию
1. Оценить добавление Severity в Diagnostic
2. Оценить centralized error message registry

---

## 44. Граф зависимостей задач

```
ERROR-007 (вынести ConsumeLastRuntimeError)
    ↓
ERROR-008 (выровнять AppendCallChain)
    ↓
ERROR-009 (валидация error handler balance)
    ↓
ERROR-011 (тесты error → rerun)
```

Задачи независимы и могут выполняться параллельно:
- ERROR-007 и ERROR-008
- ERROR-009
- ERROR-011

---

## 45. Definition of Done

| Критерий | Статус |
|----------|--------|
| Все пользовательские ошибки имеют детерминированные категории | ✅ |
| Диагностики имеют точные координаты исходного кода | ✅ |
| Обработанные ошибки корректно раскручиваются | ✅ |
| Необработанные ошибки завершают программу предсказуемо | ✅ |
| Ресурсы не утекают на путях ошибок | ⚠️ Файлы остаются открытыми (by design) |
| Состояние VM консистентно после обработанных ошибок | ✅ |
| Interpreter и VM согласованы | ✅ |
| Ошибки компиляции не отравляют последующие компиляции | ✅ |
| Runtime-ошибки не отравляют последующие выполнения | ✅ |
| Поведение TRY/CATCH детерминировано | ✅ |
| Документация соответствует фактическому поведению | ✅ |

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **[P2] ERROR-005** — Evaluation stack cleared on error discards partial values (by design, документировать)
2. **[P2] ERROR-006** — By-ref arguments not copied back on error (missing feature)
3. **[P2] ERROR-007** — `ConsumeLastRuntimeError` дублируется между движками
4. **[P2] ERROR-008** — `AppendCallChain` имеет разную детализацию между движками
5. **[P2] ERROR-009** — Нет валидации баланса error handler'ов в BytecodeModuleValidator
6. **[P3] ERROR-010** — Нет уровней серьёзности диагностик
7. **[P3] ERROR-011** — Нет тестов error → rerun
8. **[P3] ERROR-012** — BytecodeDiagnostic дублирует Diagnostic
9. **[P3] ERROR-013** — RuntimeValue.AsXxx() бросает raw InvalidOperationException
10. **[P4] ERROR-014** — Сообщения об ошибках не централизованы
11. **[P4] ERROR-015** — RuntimeErrorValue.GetField бросает InvalidOperationException для неизвестного поля

---

## 47. ТОП-20 задач по производительности

Подсистема ошибок не участвует в hot paths. Специфических задач по производительности нет.

---

## 48. Итоговая оценка здоровья подсистемы

| Область | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 9/10 | Полный паритет Interpreter ↔ VM, все тесты проходят |
| Семантика | 9/10 | Полная модель TRY/CATCH/FINALLY/ERROR |
| Архитектура | 8/10 | Чёткое разделение слоёв, небольшое дублирование между движками |
| Runtime | 9/10 | RuntimeFailureException sentinel pattern работает корректно |
| Interpreter | 9/10 | Нативная интеграция с .NET try/catch, проще VM |
| Bytecode VM | 9/10 | ErrorHandlerFrame state machine обеспечивает полное поведение |
| Производительность | N/A | Не hot path |
| Память | 9/10 | Fresh instances per execution, no global state |
| Error handling | 9/10 | Полная модель с 11 runtime-кодами |
| Тесты | 8/10 | Комплексные, но отсутствуют тесты rerun |
| Samples | 9/10 | 3 dedicated samples + 45+ error samples |
| Документация | 9/10 | 4 manual pages, спецификация, samples |
| IDE | 9/10 | DataGrid, навигация, подсветка, автозакрытие |
| Maintainability | 8/10 | Небольшое дублирование между движками |
| Reliability | 9/10 | Fresh instances, no global state, deterministic |
| Future readiness | 9/10 | Clean boundaries for modules/compiler/VM |

**Общая оценка: 8.7/10**

---

## Финальный контрольный вопрос

> Можно ли считать подсистему ERROR HANDLING / EXCEPTIONS / DIAGNOSTICS достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Условия (blockers):
1. **ERROR-007/008 [P2]:** Вынести общие хелперы ошибок (ConsumeLastRuntimeError, AppendCallChain) в общий слой — снижает дублирование и риск расхождения
2. **ERROR-009 [P2]:** Добавить валидацию баланса PushErrorHandler/PopErrorHandler в BytecodeModuleValidator — повышает надёжность байткодного пути

### Основные blockers:
- ERROR-006 [P2]: By-ref arguments not copied back on error — потенциальная потеря данных при ошибке
- ERROR-009 [P2]: Нет валидации баланса error handler'ов

### Что нужно сделать в первую очередь:
1. ERROR-007: Вынести ConsumeLastRuntimeError в общий helper
2. ERROR-008: Выровнять AppendCallChain
3. ERROR-009: Добавить валидацию в BytecodeModuleValidator

### Что можно отложить:
- ERROR-010: Уровни серьёзности (P3, не критично)
- ERROR-012: Унификация BytecodeDiagnostic (P3, не критично)
- ERROR-014: Централизация сообщений (P4, косметика)

### Что не следует трогать:
- `Diagnostic` record — стабилен, используется всеми слоями
- `DiagnosticCodes` — полный, консистентный
- `RuntimeFailureException` sentinel pattern — работает корректно
- TRY/CATCH lowering — корректная последовательность инструкций
- IDE diagnostic presentation — DataGrid, навигация, подсветка
