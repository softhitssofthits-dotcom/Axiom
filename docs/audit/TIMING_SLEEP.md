# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## TIMING / SLEEP

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы TIMING / SLEEP языка Axiom. Подсистема включает оператор `SLEEP` и встроенную функцию `TIMER()`, а также связанную инфраструктуру часов `IRuntimeClock`.

**Итоговая оценка здоровья: 8.6/10.**

Подсистема находится в стабильном, корректном и хорошо протестированном состоянии. Полный паритет между Interpreter и BytecodeVm. Единственное существенное замечание — дублирование трёх методов между движками, которое является частью более широкого системного технического долга, а не локальной проблемой.

**Главный вопрос: YES** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

---

## 2. Что представляет собой эта подсистема

Подсистема TIMING / SLEEP отвечает за два пользовательских механизма:

1. **`SLEEP`** — оператор приостановки выполнения программы. Две формы:
   - `SLEEP milliseconds` — приостанавливает выполнение на заданное количество миллисекунд.
   - `SLEEP` (без аргумента) — ожидает нажатия любой клавиши и забирает её из очереди ввода.

2. **`TIMER()`** — встроенная функция, возвращающая количество секунд с локальной полуночи текущего дня (тип `DOUBLE`).

Подсистема зависит от:
- `IRuntimeClock` — абстракция часов хоста (`GetLocalNow()`, `GetLocalTimeZone()`, `TryResolveTimeZone()`).
- `IRuntimeCancellationProvider` — механизм отмены выполнения.
- `IKeyboardInputProvider` — поставщик клавиатурного ввода для режима ожидания клавиши.

---

## 3. Полный список возможностей

### SYS-001: SLEEP с длительностью

| Поле | Значение |
|------|----------|
| ID | SYS-001 |
| Название | SLEEP с длительностью |
| Категория | I/O / Timing |
| Назначение | Приостановка выполнения на заданное количество миллисекунд |
| Пользовательский синтаксис | `SLEEP milliseconds` |
| Тип | STATEMENT |
| Parser | `ParseSleepStatement()` → `SleepStatementSyntax` |
| Syntax Node | `SleepStatementSyntax` (поле `DurationExpression`) |
| Binder | `BindSleepStatement()` → `BoundSleepStatement` (длительность как `TypeSymbol.Long`) |
| Semantic Layer | `BoundSleepStatement` |
| Interpreter | `ExecuteSleep()` → `SleepForMilliseconds()` |
| Bytecode Lowering | `LoweredInstruction.Sleep(location, hasDuration: true)` |
| Opcode | `Opcode.Sleep` с операндом `[1]` |
| Bytecode VM | `ExecuteSleepInstruction()` → `SleepForMilliseconds()` |
| Runtime Helper | Нет (реализация в движках) |
| IDE | Подсветка `SLEEP` как ключевого слова |
| Tests | 10 тестов (5 Interpreter + 5 BytecodeVm) + 2 Parser + 2 Binder |
| Samples | `samples/operators/sleep/delay_and_key_wait.axm` |
| Benchmarks | Нет отдельного сценария |
| Documentation | `docs/manual/sleep.html` |
| Текущий статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-002: SLEEP без аргумента (ожидание клавиши)

| Поле | Значение |
|------|----------|
| ID | SYS-002 |
| Название | SLEEP без аргумента |
| Категория | I/O / Timing |
| Назначение | Ожидание нажатия любой клавиши с извлечением из очереди ввода |
| Пользовательский синтаксис | `SLEEP` |
| Тип | STATEMENT |
| Parser | `ParseSleepStatement()` → `SleepStatementSyntax` (DurationExpression = null) |
| Syntax Node | `SleepStatementSyntax` (поле `DurationExpression` = null) |
| Binder | `BindSleepStatement()` → `BoundSleepStatement` (DurationExpression = null) |
| Semantic Layer | `BoundSleepStatement` |
| Interpreter | `ExecuteSleep()` → `WaitForAnyKey()` |
| Bytecode Lowering | `LoweredInstruction.Sleep(location, hasDuration: false)` |
| Opcode | `Opcode.Sleep` с операндом `[0]` |
| Bytecode VM | `ExecuteSleepInstruction()` → `WaitForAnyKey()` |
| Runtime Helper | Нет (реализация в движках) |
| IDE | Подсветка `SLEEP` как ключевого слова |
| Tests | Включены в тесты SYS-001 |
| Samples | Включены в `samples/operators/sleep/delay_and_key_wait.axm` |
| Benchmarks | Нет |
| Documentation | `docs/manual/sleep.html` |
| Текущий статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-003: TIMER()

| Поле | Значение |
|------|----------|
| ID | SYS-003 |
| Название | TIMER |
| Категория | DateTime Built-ins |
| Назначение | Возвращает количество секунд с локальной полуночи (тип `DOUBLE`) |
| Пользовательский синтаксис | `TIMER()` |
| Тип | FUNCTION |
| Parser | Обычный вызов встроенной функции |
| Syntax Node | `CallExpressionSyntax` |
| Binder | `BuiltinFunctionId.Timer`, возвращает `TypeSymbol.Double`, 0 параметров |
| Semantic Layer | `BoundCallExpression` |
| Interpreter | `EvaluateTimerBuiltin()` |
| Bytecode Lowering | Стандартный вызов встроенной функции |
| Opcode | `Opcode.CallBuiltin` |
| Bytecode VM | `EvaluateTimerBuiltin()` через `BuiltinExecution.NullaryNoLocation` |
| Runtime Helper | Нет (реализация в движках) |
| IDE | Подсветка `TIMER` как встроенной функции |
| Tests | 3 теста (2 Interpreter + 1 BytecodeVm + 1 Binder + 1 Acceptance) |
| Samples | `samples/functions/timer/basic_usage.axm` |
| Benchmarks | `samples/smoke/engine-compare/current_time_snapshot.axm` (0.438x) |
| Documentation | `docs/manual/date-time-functions-current.html` (раздел TIMER) |
| Текущий статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

### SYS-004: IRuntimeClock

| Поле | Значение |
|------|----------|
| ID | SYS-004 |
| Название | IRuntimeClock |
| Категория | Runtime Facility |
| Назначение | Абстракция часов хоста для всех временных функций |
| Пользовательский синтаксис | Нет (внутренний API) |
| Тип | RUNTIME FACILITY |
| Файл | `src/Axiom.Runtime/Host/IRuntimeClock.cs` |
| Методы | `GetLocalNow()`, `GetLocalTimeZone()`, `TryResolveTimeZone()` |
| Реализация | `NullRuntimeHost` (по умолчанию), `ExecutionRuntimeHostAdapter` (продакшн) |
| Текущий статус | IMPLEMENTED |
| Уровень уверенности | CONFIRMED |

---

## 4. Фактическая архитектура

### Уровень 1: Лексический анализ

| Файл | Класс/Элемент | Ответственность |
|------|---------------|-----------------|
| `src/Axiom.Lexing/TokenKind.cs:117` | `TokenKind.SleepKeyword` | Токен ключевого слова SLEEP |
| `src/Axiom.Lexing/Lexer.cs:92` | `["SLEEP"] = TokenKind.SleepKeyword` | Маппинг строки → токен |

`TIMER` не имеет отдельного токена — обрабатывается как обычный идентификатор встроенной функции.

### Уровень 2: Синтаксический анализ

| Файл | Класс/Метод | Ответственность |
|------|-------------|-----------------|
| `src/Axiom.Syntax/SyntaxKind.cs:117` | `SyntaxKind.SleepKeyword` | Синтаксический вид ключевого слова |
| `src/Axiom.Syntax/SyntaxKind.cs:174` | `SyntaxKind.SleepStatement` | Синтаксический вид оператора |
| `src/Axiom.Syntax/InputOutputSyntax.cs:106-121` | `SleepStatementSyntax` | Узел AST: ключевое слово + необязательное выражение длительности |
| `src/Axiom.Parsing/Parser.Statements.cs:30` | Диспетчеризация `SleepKeyword` | Маршрутизация на `ParseSleepStatement()` |
| `src/Axiom.Parsing/Parser.Statements.cs:218-223` | `ParseSleepStatement()` | Разбор: `SLEEP [expression]` |

### Уровень 3: Семантический анализ

| Файл | Класс/Метод | Ответственность |
|------|-------------|-----------------|
| `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs:15` | `BoundNodeKind.SleepStatement` | Вид привязанного узла |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs:212-223` | `BoundSleepStatement` | Привязанный узел: необязательное выражение длительности |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs:52-57` | `BindSleepStatement()` | Привязка длительности как `TypeSymbol.Long` |
| `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs:147` | `BuiltinFunctionId.Timer` | Идентификатор встроенной функции TIMER |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs:18` | Регистрация TIMER | `TIMER()` → `TypeSymbol.Double`, 0 параметров |

### Уровень 4: Байткод

| Файл | Класс/Метод | Ответственность |
|------|-------------|-----------------|
| `src/Axiom.Bytecode/Model/Opcode.cs:120` | `Opcode.Sleep` | Опкод SLEEP |
| `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs:76` | `LoweredInstructionKind.Sleep` | Пониженный вид инструкции |
| `src/Axiom.Bytecode/Model/LoweredInstruction.cs:697-700` | `LoweredInstruction.Sleep()` | Фабрика: valueCount = 0 или 1 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs:274-281` | Понижение `BoundSleepStatement` | Генерация: [выражение длительности] + Sleep |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs:828` | Компиляция в `Opcode.Sleep` | Операнд = valueCount |
| `src/Axiom.Bytecode/Model/BytecodeRoutine.cs:168` | Stack effect | `-operand[0]` (0 или 1) |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs:706-708` | Валидация операндов | Проверка 1 операнда |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs:1496-1505` | Валидация стека | Если операнд=1, проверка типа Long |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs:3697-3705` | Анализ потока управления | Sleep(0) — терминальная инструкция |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs:4007-4027` | `ValidateSleepInstruction()` | Проверка флага 0 или 1 |

### Уровень 5: Исполнение

| Файл | Класс/Метод | Ответственность |
|------|-------------|-----------------|
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs:206-217` | `ExecuteSleep()` | Исполнение SLEEP в интерпретаторе |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs:219-230` | `WaitForAnyKey()` | Ожидание клавиши (интерпретатор) |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs:284-309` | `SleepForMilliseconds()` | Пауза с чанками (интерпретатор) |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs:60-63` | `EvaluateTimerBuiltin()` | TIMER в интерпретаторе |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs:275` | Диспетчеризация TIMER | Маршрутизация встроенной функции |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs:844-847` | Диспетчеризация Opcode.Sleep | Вызов `ExecuteSleepInstruction` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs:152-162` | `ExecuteSleepInstruction()` | Исполнение SLEEP в VM |
| `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs:164-175` | `WaitForAnyKey()` | Ожидание клавиши (VM) |
| `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs:177-202` | `SleepForMilliseconds()` | Пауза с чанками (VM) |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.DateTime.cs:60-63` | `EvaluateTimerBuiltin()` | TIMER в VM |
| `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs:245` | Оптимизированный диспатч TIMER | `NullaryNoLocation` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs:245` | Фолбэк-диспатч TIMER | Резервный путь |

### Уровень 6: Хост-абстракции

| Файл | Класс/Метод | Ответственность |
|------|-------------|-----------------|
| `src/Axiom.Runtime/Host/IRuntimeClock.cs` | `IRuntimeClock` | Интерфейс часов: `GetLocalNow()`, `GetLocalTimeZone()`, `TryResolveTimeZone()` |
| `src/Axiom.Runtime/Host/NullRuntimeHost.cs:41-43` | `GetLocalNow()`, `GetLocalTimeZone()` | Реализация по умолчанию: `DateTimeOffset.Now`, `TimeZoneInfo.Local` |
| `src/Axiom.Runtime/Host/IRuntimeCancellationProvider.cs` | `IRuntimeCancellationProvider` | Интерфейс отмены |

---

## 5. Полный путь выполнения

### Путь 1: SLEEP с длительностью

```
Исходный код: SLEEP 500
    ↓
Lexer: TokenKind.SleepKeyword + IntegerLiteralToken(500)
    ↓
Parser: SleepStatementSyntax(SleepKeyword, LiteralExpressionSyntax(500))
    ↓
Binder: BoundSleepStatement(BoundLiteralExpression(500L))
    ↓
Interpreter: EvaluateExpression(500L) → SleepForMilliseconds(500, location)
    ↓
Runtime: Thread.Sleep(10) × 50 итераций с проверкой отмены
    ↓
Результат: Пауза ~500мс

Bytecode путь:
    ↓
Lowering: [LoweredLiteral(500L)] + LoweredInstruction.Sleep(hasDuration: true)
    ↓
Bytecode: [PushLong 500] + Sleep[1]
    ↓
VM: PopValue → AsLong() → SleepForMilliseconds(500, location)
    ↓
Runtime: Thread.Sleep(10) × 50 итераций с проверкой отмены
    ↓
Результат: Пауза ~500мс
```

### Путь 2: SLEEP без аргумента

```
Исходный код: SLEEP
    ↓
Lexer: TokenKind.SleepKeyword
    ↓
Parser: SleepStatementSyntax(SleepKeyword, null)
    ↓
Binder: BoundSleepStatement(null)
    ↓
Interpreter: WaitForAnyKey() → цикл TryReadKeyboardKey + Thread.Sleep(10)
    ↓
Результат: Ожидание нажатия клавиши

Bytecode путь:
    ↓
Lowering: LoweredInstruction.Sleep(hasDuration: false)
    ↓
Bytecode: Sleep[0]
    ↓
VM: WaitForAnyKey() → цикл TryReadKeyboardKey + Thread.Sleep(10)
    ↓
Результат: Ожидание нажатия клавиши
```

### Путь 3: TIMER()

```
Исходный код: TIMER()
    ↓
Lexer: IdentifierToken("TIMER") + LeftParen + RightParen
    ↓
Parser: CallExpressionSyntax("TIMER", [])
    ↓
Binder: BoundCallExpression(BuiltinFunctionId.Timer, TypeSymbol.Double, [])
    ↓
Interpreter: EvaluateTimerBuiltin() → _host.GetLocalNow().TimeOfDay.TotalSeconds
    ↓
Результат: DOUBLE — секунды с полуночи

Bytecode путь:
    ↓
Lowering: Standard builtin call
    ↓
Bytecode: CallBuiltin[Timer]
    ↓
VM: EvaluateTimerBuiltin() → _host.GetLocalNow().TimeOfDay.TotalSeconds
    ↓
Результат: DOUBLE — секунды с полуночи
```

---

## 6. Фактический семантический контракт

### SLEEP milliseconds

| Параметр | Значение |
|----------|----------|
| Вход | Выражение, приводимое к `LONG` |
| Допустимые входы | Целые числа 0 .. 2,147,483,647 (`int.MaxValue`) |
| Недопустимые входы | Отрицательные, > `int.MaxValue`, нечисловые типы |
| Тип аргумента | `TypeSymbol.Long` (после привязки) |
| Преобразования | `INTEGER` → `LONG` (неявное); `DOUBLE`, `STRING`, `BOOLEAN` — отклоняются на этапе привязки |
| Изменения состояния | Нет |
| Побочные эффекты | Приостановка потока выполнения на указанное количество миллисекунд |
| Ошибки | `MB3007` при отрицательном или слишком большом значении |
| Точность | Зависит от планировщика ОС (~15.6мс на Windows); чанки по 10мс |

### SLEEP (без аргумента)

| Параметр | Значение |
|----------|----------|
| Вход | Нет |
| Побочные эффекты | Блокирующее ожидание клавиши; извлечение клавиши из очереди ввода |
| Ошибки | Нет (кроме отмены) |
| Точность | Опрос каждые 10мс |

### TIMER()

| Параметр | Значение |
|----------|----------|
| Вход | Нет |
| Выход | `DOUBLE` — секунды с локальной полуночи |
| Диапазон | [0.0, 86400.0) |
| Точность | Зависит от `DateTimeOffset.Now` (~1мс на Windows 8+) |
| Побочные эффекты | Нет |
| Чистота | Нет (зависит от текущего времени; помечен как `false` в purity check) |

---

## 7. Анализ Parser

### Синтаксис SLEEP

Парсер обрабатывает `SLEEP` через диспетчеризацию `TokenKind.SleepKeyword` в `Parser.Statements.cs:30`. Метод `ParseSleepStatement()` (строки 218-223) корректно обрабатывает обе формы:

- `SLEEP expression` — парсит выражение длительности.
- `SLEEP` (конец оператора) — `DurationExpression = null`.

**Оценка: Корректно.** Парсер не знает семантических ограничений (тип, диапазон) — это ответственность Binder.

### Синтаксис TIMER

`TIMER` не имеет специального синтаксиса. Он парсится как обычный вызов встроенной функции через `ParseCallExpression()`. Это стандартный путь для всех встроенных функций.

**Оценка: Корректно.** Нет отклонений от общей модели.

---

## 8. Анализ Binder

### Привязка SLEEP

`BindSleepStatement()` в `Binder.IoStatements.cs:52-57` привязывает длительность с ожидаемым типом `TypeSymbol.Long`. Система привязки:

- `INTEGER` → `LONG`: неявная конверсия разрешена.
- `DOUBLE` → `LONG`: **отклоняется** (нет в таблице `CanConvert`).
- `STRING` → `LONG`: **отклоняется**.
- `BOOLEAN` → `LONG`: **отклоняется**.

**Оценка: Корректно.** Строгая типизация предотвращает неоднозначные преобразования.

### Привязка TIMER

`TIMER` регистрируется в `BuiltinFunctions.DateTime.cs:18` как функция с 0 параметрами, возвращающая `TypeSymbol.Double`. Стандартный путь привязки встроенных функций.

**Оценка: Корректно.**

---

## 9. Анализ Interpreter

### ExecuteSleep (Interpreter.Statements.cs:206-217)

```csharp
private ExecutionSignal ExecuteSleep(BoundSleepStatement statement)
{
    if (statement.DurationExpression is null)
    {
        WaitForAnyKey();
        return ExecutionSignal.None;
    }
    var milliseconds = EvaluateExpression(statement.DurationExpression).AsLong();
    SleepForMilliseconds(milliseconds, statement.Location);
    return ExecutionSignal.None;
}
```

**Оценка: Корректно.** Чистая логика: проверка на null → ожидание клавиши, иначе → пауза.

### WaitForAnyKey (Interpreter.Statements.cs:219-230)

Цикл опроса каждые 10мс с проверкой отмены. Использует `TryReadKeyboardKey()`.

**Оценка: Корректно.** Единственный нюанс — 10мс интервал опроса создаёт небольшую задержку реакции на нажатие клавиши (до 10мс).

### SleepForMilliseconds (Interpreter.Statements.cs:284-309)

Валидация диапазона `0..int.MaxValue`, чанковый сон по 10мс с проверкой отмены.

**Оценка: Корректно.** Чанковый подход корректно поддерживает отмену во время длительного сна.

### EvaluateTimerBuiltin (Interpreter.Builtins.DateTime.cs:60-63)

```csharp
private RuntimeValue EvaluateTimerBuiltin()
{
    return RuntimeValue.FromDouble(_host.GetLocalNow().TimeOfDay.TotalSeconds);
}
```

**Оценка: Корректно.** Минимальная, чистая реализация.

---

## 10. Анализ Bytecode / VM

### Lowering BoundSleepStatement

`BoundProgramLowerer.cs:274-281`:
- Если есть выражение длительности — сначала понижает его (результат на стеке).
- Затем emit `LoweredInstruction.Sleep(location, hasDuration)`.
- `valueCount` = 1 если есть длительность, 0 если нет.

**Оценка: Корректно.** Минимальное количество инструкций, нет лишних temporaries.

### Bytecode emission

`BytecodeCompiler.cs:828`: `Opcode.Sleep` с операндом `[instruction.ValueCount]`.

**Оценка: Корректно.** Операнд кодирует наличие аргумента (0 или 1).

### Stack effect

`BytecodeRoutine.cs:168`: `Opcode.Sleep => -instruction.GetOperand(0)`.

**Оценка: Корректно.** Pop 0 или 1 значений в зависимости от наличия аргумента.

### BytecodeModuleValidator

Три уровня валидации:
1. Проверка количества операндов (1).
2. Проверка флага длительности (0 или 1) — `ValidateSleepInstruction()`.
3. Проверка типа на стеке: если операнд=1, pop и проверка `TypeSymbol.Long`.

**Оценка: Корректно.** Полная валидация на всех уровнях.

### ExecuteSleepInstruction (BytecodeVm.Keyboard.cs:152-162)

```csharp
private void ExecuteSleepInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
{
    if (instruction.GetOperand(0) == 0)
    {
        WaitForAnyKey();
        return;
    }
    var milliseconds = PopValue(frame, instructionIndex).AsLong();
    SleepForMilliseconds(milliseconds, ResolveLocation(frame.Routine.Id, instructionIndex));
}
```

**Оценка: Корректно.** Аналогичная логика интерпретатору, но с использованием стека.

### InvalidateReverseExecutionHistory

При исполнении `Opcode.Sleep` вызывается `InvalidateReverseExecutionHistory()` (BytecodeVm.cs:846). Это корректно — SLEEP является side-effecting операцией, которая делает обратное исполнение бессмысленным.

---

## 11. Анализ Runtime

### IRuntimeClock

Интерфейс `IRuntimeClock` (3 метода) предоставляет абстракцию часов:
- `GetLocalNow()` — текущее локальное время как `DateTimeOffset`.
- `GetLocalTimeZone()` — локальный часовой пояс.
- `TryResolveTimeZone(id)` — разрешение часового пояса по строковому идентификатору.

**Оценка: Корректно.** Чистый интерфейс без побочных эффектов.

### NullRuntimeHost

Реализация по умолчанию:
- `GetLocalNow()` → `DateTimeOffset.Now` (недетерминированный).
- `GetLocalTimeZone()` → `TimeZoneInfo.Local`.
- `TryResolveTimeZone()` → поддержка "LOCAL", "UTC", и системных идентификаторов.

**Оценка: Корректно.** Для тестов используется `TestRuntimeHost` с фиксированным временем.

### ExecutionRuntimeHostAdapter

Продакшн-реализация с делегированием на `IRuntimeClock` и `IRuntimeCancellationProvider`.

**Оценка: Корректно.**

---

## 12. Взаимодействие с типами

| Тип | SLEEP | TIMER |
|-----|-------|-------|
| INTEGER | Неявная конверсия → LONG | Не применимо |
| LONG | Прямое использование | Не применимо |
| DOUBLE | Отклоняется (тип mismatch) | Возвращает DOUBLE |
| DECIMAL | Отклоняется | Не применимо |
| STRING | Отклоняется | Не применимо |
| BOOLEAN | Отклоняется | Не применимо |
| ARRAY | Не применимо | Не применимо |
| TYPE | Не применимо | Не применимо |

**Оценка: Корректно.** Строгая типизация, нет неожиданных конверсий.

---

## 13. Взаимодействие со String

Строковые операции не пересекаются с подсистемой TIMING / SLEEP.

Единственная точка пересечения: `TIMER()` возвращает `DOUBLE`, а не `STRING`. Если пользователю нужна строка, он должен использовать `FORMAT$(TIMER())` или аналог.

---

## 14. Взаимодействие с Array

Массивы не пересекаются с подсистемой TIMING / SLEEP.

---

## 15. Взаимодействие с TYPE

Пользовательские типы не пересекаются с подсистемой TIMING / SLEEP.

---

## 16. Взаимодействие с Functions / SUB

### SLEEP внутри функций и подпрограмм

`SLEEP` корректно работает внутри `FUNCTION`, `SUB`, `IF`, `FOR`, `WHILE`, `DO` и любых других контекстов. Это обычный оператор, не требующий специальной обработки.

### TIMER() внутри функций

`TIMER()` корректно вызывается из любых контекстов как обычная встроенная функция.

### Рекурсия

`SLEEP` внутри рекурсивной функции корректно приостанавливает каждый вызов. Нет рисков reentrancy — `SLEEP` не захватывает ресурсы.

---

## 17. Взаимодействие с Control Flow

| Конструкция | SLEEP | TIMER |
|-------------|-------|-------|
| IF/ELSE | Корректно | Корректно |
| SELECT CASE | Корректно | Корректно |
| FOR/NEXT | Корректно (включая `CONTINUE FOR`) | Корректно |
| WHILE/END WHILE | Корректно | Корректно |
| DO/LOOP | Корректно | Корректно |
| TRY/CATCH/FINALLY | Корректно | Корректно |
| FUNCTION/SUB | Корректно | Корректно |

**Оценка: Нет отклонений.**

---

## 18. Взаимодействие с Error / Exception System

### Диагностика MB3007

Единственная диагностика подсистемы: `MB3007` — отрицательное или слишком большое значение длительности SLEEP.

- Генерируется в `SleepForMilliseconds()` обоих движков.
- Сообщение: `"должен быть в диапазоне 0..{int.MaxValue}."`
- После генерации бросается `RuntimeFailureException`.

### RuntimeFailureException

`SleepForMilliseconds()` бросает `RuntimeFailureException` после `ReportBuiltInArgumentError`. Это стандартный паттерн для runtime-ошибок в Axiom.

### TRY/CATCH

`SLEEP` внутри `TRY`-блока: если `SLEEP` бросает `RuntimeFailureException` (например, отрицательная длительность), он корректно ловится `CATCH`.

**Оценка: Корректно.**

---

## 19. State / Lifetime / Ownership

### Владение данными

Подсистема TIMING / SLEEP не создаёт и не владеет никакими ресурсами. Нет:
- Файловых дескрипторов.
- Графических ресурсов.
- Выделений памяти.
- Состояния, которое нужно освобождать.

### Состояние часов

`IRuntimeClock` — stateless интерфейс. Каждый вызов `GetLocalNow()` возвращает текущее время. Нет кэширования.

### Состояние SLEEP

`SLEEP` не сохраняет состояние между вызовами. Каждый вызов независим.

**Оценка: Нет проблем с ownership или lifetime.**

---

## 20. Edge Cases

### SYS-005: SLEEP 0

**Поведение:** Немедленный возврат без паузы.
**Корректность:** Да — `SleepForMilliseconds` проверяет `milliseconds == 0` и возвращает управление.
**Тест:** `Execute_SleepZeroContinues` / `Execute_CompiledSleepZeroContinues`.

### SYS-006: SLEEP с отрицательным значением

**Поведение:** Диагностика `MB3007` + `RuntimeFailureException`.
**Корректность:** Да.
**Тест:** `Execute_SleepReportsNegativeDuration` / `Execute_CompiledSleepReportsNegativeDuration`.

### SYS-007: SLEEP с очень большим значением

**Поведение:** Значения > `int.MaxValue` (2,147,483,647) отклоняются с `MB3007`.
**Корректность:** Да, но есть нюанс — Binder принимает `LONG` (до 9.2 × 10¹⁸), а Runtime отклоняет > `int.MaxValue`. Это означает, что `SLEEP 3000000000` компилируется, но падает в runtime.
**Оценка:** Допустимо, но можно улучшить валидацию на этапе привязки.

### SYS-008: TIMER() в полночь

**Поведение:** `TimeOfDay.TotalSeconds` = 0.0.
**Корректность:** Да.

### SYS-009: TIMER() перед/после полуночи

**Поведение:** Если программа вызывает `TIMER()` перед полуночью (например, 86399.5) и после полуночи (0.5), разница будет отрицательной (-86399.0).
**Корректность:** Это стандартное поведение BASIC TIMER. Не является багом, но является подводным камнем для пользователей.
**Рекомендация:** Документировать в manual.

### SYS-010: SLEEP с длительностью 1 мс

**Поведение:** Один чанк `Thread.Sleep(1)`.
**Корректность:** Да, но фактическая пауза может быть больше (~15.6мс на Windows).

### SYS-011: SLEEP внутри TRY/CATCH с отрицательным значением

**Поведение:** `RuntimeFailureException` ловится `CATCH`.
**Корректность:** Да.

### SYS-012: SLEEP с отменой во время паузы

**Поведение:** `CheckCancellationRequested()` вызывается каждые 10мс. При обнаружении отмены — немедленный возврат.
**Корректность:** Да. Тесты: `Execute_SleepWithDuration_StopsWhenCancellationIsRequested`.

### SYS-013: SLEEP без аргумента с отменой

**Поведение:** `WaitForAnyKey()` проверяет отмену каждые 10мс.
**Корректность:** Да. Тесты: `Execute_SleepWithoutDuration_StopsWhenCancellationIsRequested`.

### SYS-014: TIMER() точность дробных секунд

**Поведение:** `DateTimeOffset.Now` на Windows 8+ использует `GetSystemTimePreciseAsFileTime` с точностью до 100нс.
**Корректность:** Да. Тест: `Execute_TimerPreservesFractionalSeconds` (проверяет 1.25 секунды).

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| SLEEP 500 | ✅ | ✅ | ✅ | ✅ | ✅ |
| SLEEP 0 | ✅ | ✅ | ✅ | ✅ | ✅ |
| SLEEP (key wait) | ✅ | ✅ | ✅ | ✅ | ✅ |
| SLEEP -1 | MB3007 | MB3007 | ✅ | ✅ | ✅ |
| SLEEP cancellation | ✅ | ✅ | ✅ | ✅ | ✅ |
| TIMER() | ✅ | ✅ | ✅ | ✅ | ✅ |
| TIMER() fractional | ✅ | ✅ | ✅ | ✅ | ✅ |

**Расхождений не обнаружено.** Полный паритет.

---

## 22. Анализ производительности

### Существующие бенчмарки

| Сценарий | Ratio (VM/Interpreter) | Категория | Классификация |
|---|---:|---|---|
| `current_time_snapshot` | 0.438x | DateTime builtins (включая TIMER) | CLEAR WIN |

TIMER() вызывается 220 раз в цикле бенчмарка. VM значительно быстрее за счёт:
- Оптимизированного диспатча через `BuiltinExecution.NullaryNoLocation`.
- Отсутствия накладных расходов tree-walking.

### SLEEP как бенчмарк

SLEEP не подлежит бенчмаркированию в классическом смысле — его «производительность» определяется ОС-планировщиком, а не движком. Оба движка делают одинаковые вызовы `Thread.Sleep()`.

### Hot path аналитика

**TIMER() hot path в VM:**
1. `CallBuiltin[Timer]` → `BuiltinExecution.NullaryNoLocation`
2. `EvaluateTimerBuiltin()` → `_host.GetLocalNow().TimeOfDay.TotalSeconds`
3. `RuntimeValue.FromDouble(result)` → push на стек

**TIMER() hot path в Interpreter:**
1. `EvaluateCallExpression` → `BuiltinFunctionId.Timer` dispatch
2. `EvaluateTimerBuiltin()` → `_host.GetLocalNow().TimeOfDay.TotalSeconds`
3. `RuntimeValue.FromDouble(result)` → возврат

Разница: VM использует делегат `static vm => vm.EvaluateTimerBuiltin()` без виртуального диспатча, Interpreter использует `switch` по `BuiltinFunctionId`.

---

## 23. Качество Benchmark

### current_time_snapshot.axm

- 220 итераций цикла с 9 вызовами DATE/TIME/NOW + 1 TIMER().
- Использует фиксированные часы (детерминированный хост).
- Проверяет контрольные суммы, а не абсолютное время.
- **Оценка: Хороший benchmark.** Покрывает TIMER в контексте других DateTime builtins.

### Отсутствие изолированного benchmark для TIMER

Нет отдельного сценария, который измерял бы только TIMER() в изоляции. `current_time_snapshot` смешивает TIMER с 9 другими DateTime-вызовами.

**Рекомендация:** Низкий приоритет — текущий benchmark достаточен для оценки.

---

## 24. Hot Paths и Bottlenecks

### TIMER() — нет bottlenecks

TIMER() — тривиальная функция: один вызов `_host.GetLocalNow()`, одно обращение к `.TimeOfDay.TotalSeconds`, один `RuntimeValue.FromDouble()`. Нет аллокаций, нет конверсий, нет валидации.

### SLEEP — нет bottlenecks в коде движка

SLEEP — I/O-bound операция. Время выполнения определяется `Thread.Sleep()` и ОС-планировщиком, а не кодом движка. Чанковый подход (10мс) добавляет накладные расходы ~0.01% для типичных пауз.

### WaitForAnyKey — потенциальная задержка

Опрос каждые 10мс означает до 10мс задержки между нажатием клавиши и его обработкой. Это стандартный компромисс между отзывчивостью и нагрузкой на CPU.

---

## 25. Архитектурные проблемы

### SYS-015: Дублирование SleepForMilliseconds и WaitForAnyKey

**Серьёзность:** P3
**Категория:** ARCHITECTURE
**Уверенность:** CONFIRMED

Три метода полностью дублированы между Interpreter и BytecodeVm:

| Метод | Interpreter | BytecodeVm | Статус |
|-------|-------------|------------|--------|
| `WaitForAnyKey()` | `Interpreter.Statements.cs:219-230` | `BytecodeVm.Keyboard.cs:164-175` | Байт-в-байт идентичны |
| `SleepForMilliseconds()` | `Interpreter.Statements.cs:284-309` | `BytecodeVm.Keyboard.cs:177-202` | Байт-в-байт идентичны |
| `EvaluateTimerBuiltin()` | `Interpreter.Builtins.DateTime.cs:60-63` | `BytecodeVm.Builtins.DateTime.cs:60-63` | Байт-в-байт идентичны |

**Корневая причина:** Общая проблема архитектуры — отсутствие общего базового класса или shared utility для идентичной логики между движками. Это часть более широкого технического долга, который уже частично адресован выносом built-in хелперов (`BuiltinStringHelpers`, `BuiltinArrayHelpers`, `BuiltinDateTimeHelpers` и т.д.).

**Направление исправления:** Вынести `WaitForAnyKey()`, `SleepForMilliseconds()` и `EvaluateTimerBuiltin()` в общий слой (например, `BuiltinTimingHelpers` или в существующий `BuiltinDateTimeHelpers`).

**Риск регрессии:** Низкий — методы тривиальные и хорошо протестированы.

---

## 26. Дублирование логики

### Полное дублирование (3 метода)

1. **`WaitForAnyKey()`** — 12 строк, идентичны в обоих движках.
2. **`SleepForMilliseconds()`** — 26 строк, идентичны в обоих движках.
3. **`EvaluateTimerBuiltin()`** — 4 строки, идентичны в обоих движках.

**Суммарно:** ~42 строки дублированного кода.

### Частичное дублирование (1 метод)

4. **`ExecuteSleep()` / `ExecuteSleepInstruction()`** — логика идентична, но входные данные различаются (AST vs bytecode). Это engine-specific код, не подлежащий выносу.

---

## 27. Single Source of Truth

| Аспект | Источник истины | Статус |
|--------|----------------|--------|
| Синтаксис SLEEP | `Parser.Statements.cs` + `SleepStatementSyntax` | ✅ Единственный |
| Семантика SLEEP | `Binder.IoStatements.cs` + `BoundSleepStatement` | ✅ Единственный |
| Семантика TIMER | `BuiltinFunctions.DateTime.cs` + `BuiltinFunctionId.Timer` | ✅ Единственный |
| Runtime-поведение SLEEP | Дублировано в двух движках | ⚠️ Два источника |
| Runtime-поведение TIMER | Дублировано в двух движках | ⚠️ Два источника |
| Диагностика MB3007 | Дублировано в двух движках | ⚠️ Два источника |
| Часы | `IRuntimeClock` | ✅ Единственный |

---

## 28. Legacy / Dead Code

**Не обнаружено.** Подсистема не содержит:
- Старых реализаций.
- Compatibility layers.
- Неиспользуемых helpers.
- Abandoned features.

---

## 29. Надёжность и безопасность

### Потенциальное исчерпание ресурсов

`SLEEP` с очень большим значением (например, `SLEEP 2147483647` = ~24.8 дней) заблокирует поток выполнения на всё это время. Однако:
- Отмена проверяется каждые 10мс.
- Пользователь может закрыть IDE.
- `TimeoutCancellationProvider` может прервать выполнение.

**Оценка: Приемлемо.**

### Thread.Sleep и точность

`Thread.Sleep(10)` на Windows имеет точность ~15.6мс (取决于 таймера ОС). Это не проблема Axiom, а ограничение платформы.

### Потокобезопасность

`CheckCancellationRequested()` использует `volatile bool` (через `IRuntimeCancellationProvider`). `TryReadKeyboardKey()` обращается к `_bufferedKeys` (очередь), которая заполняется из UI-потока. Нет явной синхронизации, но в текущей архитектуре чтение и запись происходят в разных контекстах с гарантированным порядком.

**Оценка: Приемлемо для текущей архитектуры.**

---

## 30. Анализ тестов

### Покрытие тестами

| Тип теста | SLEEP | TIMER | Файл |
|-----------|-------|-------|------|
| Parser | 2 теста | — | `ParserTests.cs:389, 400` |
| Binder | 2 теста | 1 тест | `BinderTests.cs:2836, 2847, 3468` |
| Syntax | 2 теста | — | `SyntaxKindTests.cs:75, 128`; `StatementSyntaxTests.cs:111` |
| Interpreter Runtime | 5 тестов | 2 теста | `InterpreterTests.cs:228-309`; `InterpreterTests.DateTime.cs:10, 28` |
| BytecodeVm Runtime | 5 тестов | 1 тест | `BytecodeVmTests.cs:123-202, 5699` |
| Acceptance | 1 тест | 1 тест | `AcceptanceProgramTests.cs:1371, 457` |
| IDE Formatting | 1 тест | — | `CodeFormattingServiceTests.cs:106` |
| **Итого** | **18 тестов** | **5 тестов** | |

### Качество assertions

- **SLEEP tests:** Проверяют корректное поведение (пауза, ожидание клавиши, отрицательная длительность, отмена).
- **TIMER tests:** Проверяют точность (дробные секунды), формат вывода, канонические форматы.
- **Acceptance tests:** Запускают полные `.axm` программы и проверяют вывод.

**Оценка: Хорошее покрытие.** Все ключевые сценарии покрыты.

---

## 31. Недостающие тесты

### Обычные сценарии

| # | Сценарий | Статус |
|---|----------|--------|
| 1 | SLEEP с INTEGER-аргументом | ✅ Покрыт (через LONG) |
| 2 | SLEEP с LONG-аргументом | ✅ Покрыт |
| 3 | SLEEP 0 | ✅ Покрыт |
| 4 | SLEEP (key wait) | ✅ Покрыт |
| 5 | TIMER() базовый | ✅ Покрыт |
| 6 | TIMER() дробные секунды | ✅ Покрыт |

### Отрицательные сценарии

| # | Сценарий | Статус |
|---|----------|--------|
| 1 | SLEEP -1 | ✅ Покрыт |
| 2 | SLEEP > int.MaxValue | ⚠️ Нет теста |
| 3 | SLEEP с DOUBLE-аргументом | ⚠️ Нет теста (но Binder отклоняет) |
| 4 | SLEEP с STRING-аргументом | ⚠️ Нет теста (но Binder отклоняет) |

### Граничные случаи

| # | Сценарий | Статус |
|---|----------|--------|
| 1 | SLEEP 1 (минимальная пауза) | ⚠️ Нет теста |
| 2 | SLEEP int.MaxValue | ⚠️ Нет теста |
| 3 | TIMER() ровно в полночь | ⚠️ Нет теста |
| 4 | TIMER() wrap через полночь | ⚠️ Нет теста (документировать) |

### Parity-тесты

| # | Сценарий | Статус |
|---|----------|--------|
| 1 | SLEEP Interpreter vs VM | ✅ Косвенно покрыт (одинаковые acceptance) |
| 2 | TIMER Interpreter vs VM | ✅ Косвенно покрыт |

---

## 32. Анализ Samples

### samples/operators/sleep/delay_and_key_wait.axm

```basic
PRINT "Пауза 500 мс..."
SLEEP 500
PRINT "Нажмите любую клавишу."
SLEEP
PRINT "Готово"
```

**Что демонстрирует:** Обе формы SLEEP — с длительностью и ожидание клавиши.
**Engine:** Работает в обоих движках.
**Проверяемый результат:** Вывод 3 строк (acceptance-тест проверяет через queued key).
**Оценка: Хороший sample.** Покрывает основные сценарии.

### samples/functions/timer/basic_usage.axm

```basic
DIM first AS DOUBLE = TIMER()
DIM second AS DOUBLE = TIMER()
PRINT first >= 0
PRINT first < 86400
PRINT second >= first
```

**Что демонстрирует:** TIMER() возвращает неотрицательное значение < 86400, последовательные вызовы неубывающие.
**Engine:** Работает в обоих движках.
**Проверяемый результат:** `TRUE`, `TRUE`, `TRUE`.
**Оценка: Хороший sample.** Проверяет логические свойства, а не абсолютные значения.

---

## 33. Анализ документации

### docs/manual/sleep.html

- Полная страница-справочник для оператора SLEEP.
- Описывает обе формы: с длительностью и ожидание клавиши.
- Указывает на `MB3007` для отрицательных значений.
- Связанная страница: loops, DO...LOOP, INKEY, KEYPRESS, KEYDOWN.
- **Оценка: Полная и корректная.**

### docs/manual/date-time-functions-current.html (раздел TIMER)

- Описывает TIMER() как функцию, возвращающую секунды с полуночи.
- Указывает тип результата: DOUBLE.
- Пример: `PRINT TIMER()`.
- **Оценка: Корректная, но минимальная.** Можно добавить пример использования для измерения elapsed time.

### docs/SPECIFICATION3.md

- SLEEP описан в разделе 2285-2304.
- Две формы, тип привязки, диагностика — всё задокументировано.
- Упоминания в контексте SCREEN 0, PCOPY, CANVAS.
- **TIMER не упоминается в SPECIFICATION3.md.** Это не является проблемой, так как TIMER — встроенная функция, а не оператор, и документируется в manual.

### Несоответствия документации и кода

| # | Несоответствие | Серьёзность |
|---|----------------|-------------|
| 1 | TIMER midnight wrap не документирован | P4 |
| 2 | SLEEP range limit (int.MaxValue) не документирован в manual | P4 |
| 3 | TIMER отсутствует в SPECIFICATION3.md | P4 (не обязательно для built-in) |

---

## 34. Интеграция с IDE

### Подсветка синтаксиса

- `SLEEP` — в обеих темах (`Axiom.xshd:104`, `Axiom.Dark.xshd:104`) как ключевое слово.
- `TIMER` — в обеих темах (`Axiom.xshd:358`, `Axiom.Dark.xshd:358`) как встроенная функция.

### Форматирование

- `sleep 500` → `SLEEP 500` (нормализация регистра). Тест: `CodeFormattingServiceTests.cs:106`.

### Диагностика

- `MB3007` отображается в панели ошибок IDE при отрицательной длительности.

### Статусная строка

- IDE показывает время выполнения программы в статусной строке (не связано с TIMER, но визуально пересекается).

**Оценка: Полная интеграция.** Нет расхождений между IDE и runtime.

---

## 35. Зависимости от Host / Platform

| Зависимость | Описание | Классификация |
|-------------|----------|---------------|
| Windows | `Thread.Sleep()` точность ~15.6мс | HOST LIMITATION |
| .NET | `DateTimeOffset.Now` точность | HOST LIMITATION |
| .NET | `TimeOfDay.TotalSeconds` поведение | PLATFORM |
| Windows | `GetSystemTimePreciseAsFileTime` (Windows 8+) | HOST LIMITATION |

**Нет LANGUAGE DEFECT.** Все ограничения связаны с платформой, а не с языком.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Дублирование логики между движками

├── SYS-015: Дублирование `WaitForAnyKey()`
├── SYS-015: Дублирование `SleepForMilliseconds()`
└── SYS-015: Дублирование `EvaluateTimerBuiltin()`

**Корневая причина:** Отсутствие общего базового класса или shared utility для идентичной логики. Это часть более широкой архитектурной проблемы, которая уже частично решена выносом built-in хелперов.

**Направление:** Вынести в `BuiltinTimingHelpers` или расширить `BuiltinDateTimeHelpers`.

---

## 37. Стабильные области

| Область | Статус | Причина |
|---------|--------|---------|
| Парсинг SLEEP | ✅ Стабилен | Простой синтаксис, не менялся |
| Привязка SLEEP | ✅ Стабилен | Строгая типизация, корректная валидация |
| Привязка TIMER | ✅ Стабилен | Стандартный путь built-in функций |
| Lowering SLEEP | ✅ Стабилен | Минимальная трансляция |
| Bytecode валидация SLEEP | ✅ Стабилен | Полная трехуровневая валидация |
| IRuntimeClock | ✅ Стабилен | Чистый интерфейс, не менялся |
| Тесты | ✅ Стабильны | 23 теста, все проходят |
| Документация | ✅ Стабильна | Полная manual-страница |

---

## 38. Возможности консолидации

### Консолидация 1: SleepForMilliseconds + WaitForAnyKey + EvaluateTimerBuiltin

| Текущие места | Дублируемая логика | Единый источник истины | Направление миграции | Риски |
|---|---|---|---|---|
| `Interpreter.Statements.cs` + `BytecodeVm.Keyboard.cs` | `WaitForAnyKey()` | `BuiltinTimingHelpers` или `BuiltinDateTimeHelpers` | Вынести в общий слой | Низкий |
| `Interpreter.Statements.cs` + `BytecodeVm.Keyboard.cs` | `SleepForMilliseconds()` | `BuiltinTimingHelpers` или `BuiltinDateTimeHelpers` | Вынести в общий слой | Низкий |
| `Interpreter.Builtins.DateTime.cs` + `BytecodeVm.Builtins.DateTime.cs` | `EvaluateTimerBuiltin()` | `BuiltinDateTimeHelpers` | Вынести в общий слой | Низкий |

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности корректно:

- **Lexer** — токенизация `SLEEP`.
- **Parser** — построение AST.
- **Binder** — типизация и валидация.
- **Lowering** — трансляция в bytecode.
- **VM/Interpreter** — исполнение.
- **Runtime Host** — абстракция часов и ввода.

Нет смешивания ответственностей.

---

## 40. Кандидаты на удаление

**Нет кандидатов на удаление.** Весь код подсистемы используется.

---

## 41. Готовность к дальнейшему развитию

| Направление | Готовность | Оценка |
|-------------|------------|--------|
| Будущий Compiler | ✅ Готово | SLEEP уже имеет opcode и lowering |
| Дальнейшая оптимизация | ✅ Готово | TIMER уже имеет optimized dispatch |
| Специализированные Opcode | ✅ Готово | Opcode.Sleep уже существует |
| Typed fast paths | ⚠️ Не применимо | SLEEP не возвращает значение |
| Развитие Runtime | ✅ Готово | IRuntimeClock расширяем |
| Развитие IDE | ✅ Готово | Подсветка и форматирование работают |
| Расширение языка | ✅ Готово | Можно добавить SLEEP EVERY, ON TIMER и т.д. |

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-015 | P3 | ARCHITECTURE | Interpreter + VM | Дублирование 3 методов (~42 строки) | Байт-в-байт сравнение | Отсутствие общего слоя | CONFIRMED |
| SYS-007 | P4 | RUNTIME | SLEEP | Компилируется с LONG, падает с >int.MaxValue | Код Binder + Runtime | Отсутствие bind-time валидации диапазона | HIGH |
| SYS-009 | P4 | DOCS | TIMER | Midnight wrap не документирован | Отсутствие упоминания в manual | Незадокументированный edge case | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Статус:** Не требуется — подсистема стабильна.

### ЭТАП 1 — Исправление критических проблем корректности

**Статус:** Нет критических проблем. Все P0 отсутствуют.

### ЭТАП 2 — Стабилизация семантики

**Статус:** Семантика стабильна. Паритет полный.

### ЭТАП 3 — Архитектурное улучшение

| Задача | Приоритет | Описание |
|--------|-----------|----------|
| TIMING-001 | P3 | Вынести `WaitForAnyKey()`, `SleepForMilliseconds()`, `EvaluateTimerBuiltin()` в общий слой |

### ЭТАП 4 — Оптимизация производительности

**Статус:** Не требуется — TIMER уже имеет optimized dispatch, SLEEP I/O-bound.

### ЭТАП 5 — Усиление тестирования

| Задача | Приоритет | Описание |
|--------|-----------|----------|
| TIMING-002 | P4 | Добавить тест `SLEEP > int.MaxValue` (runtime error) |
| TIMING-003 | P4 | Добавить тест `SLEEP 1` (минимальная пауза) |
| TIMING-004 | P4 | Добавить тест TIMER() в полночь |

### ЭТАП 6 — Обновление документации

| Задача | Приоритет | Описание |
|--------|-----------|----------|
| TIMING-005 | P4 | Добавить в manual описание midnight wrap для TIMER() |
| TIMING-006 | P4 | Добавить в manual описание ограничения диапазона SLEEP (0..int.MaxValue) |

### ЭТАП 7 — Обновление IDE

**Статус:** Не требуется — IDE интеграция полная.

### ЭТАП 8 — Подготовка к дальнейшему развитию

**Статус:** Подсистема готова к расширению (SLEEP EVERY, ON TIMER и т.д.).

---

## 44. Граф зависимостей задач

```
TIMING-001 (вынос общего слоя)
    ├── Нет зависимостей
    └── Низкий риск регрессии

TIMING-002..006 (тесты и документация)
    ├── Нет зависимостей
    └── Нет рисков
```

Все задачи независимы и могут выполняться в любом порядке.

---

## 45. Definition of Done

Подсистема TIMING / SLEEP считается завершённой, когда:

1. ✅ `SLEEP` с длительностью корректно приостанавливает выполнение.
2. ✅ `SLEEP` без аргумента корректно ожидает клавишу.
3. ✅ `SLEEP 0` возвращает управление немедленно.
4. ✅ Отрицательная длительность генерирует `MB3007`.
5. ✅ Отмена корректно прерывает SLEEP.
6. ✅ `TIMER()` возвращает секунды с полуночи с дробной частью.
7. ✅ Паритет Interpreter ↔ BytecodeVm полный.
8. ✅ Тесты покрывают основные сценарии.
9. ✅ Manual-страница существует и корректна.
10. ✅ IDE-подсветка работает для SLEEP и TIMER.

**Все 10 критериев выполнены.**

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | SYS-015 | P3 | Дублирование 3 методов между Interpreter и BytecodeVm |
| 2 | SYS-007 | P4 | SLEEP компилируется с LONG, падает с >int.MaxValue |
| 3 | SYS-009 | P4 | TIMER midnight wrap не документирован |

**Всего 3 находки.** Ни одной P0/P1/P2.

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемый эффект |
|---|---|---|---|
| — | — | Нет задач по производительности | — |

Подсистема не является performance bottleneck. TIMER() уже имеет optimized dispatch. SLEEP I/O-bound.

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---:|------------|
| Корректность | 9 | Все сценарии работают корректно, полный паритет |
| Семантика | 9 | Чёткий контракт, строгая типизация |
| Архитектура | 8 | Единственное замечание — дублирование 3 методов |
| Runtime | 9 | Чистая реализация, корректная абстракция часов |
| Interpreter | 9 | Корректное исполнение, нет побочных эффектов |
| Bytecode VM | 9 | Полный паритет с Interpreter, optimized dispatch |
| Производительность | 9 | TIMER CLEAR WIN (0.438x), SLEEP I/O-bound |
| Память | 10 | Нет аллокаций, нет ресурсов |
| Error handling | 9 | MB3007 корректно генерируется и ловится |
| Tests | 8 | 23 теста, все проходят; можно расширить edge cases |
| Samples | 8 | 2 sample-файла, покрывают основные сценарии |
| Documentation | 8 | Полная manual-страница; можно расширить edge cases |
| IDE | 9 | Полная подсветка и форматирование |
| Maintainability | 8 | Дублирование 3 методов снижает оценку |
| Reliability | 9 | Стабильная работа, корректная отмена |
| Future readiness | 9 | Готово к расширению |

**Средняя оценка: 8.6/10.**

---

## Основные blockers

Нет blockers. Подсистема стабильна и может быть передана следующей подсистеме.

## Что нужно сделать в первую очередь

1. **TIMING-001** [P3]: Вынести `WaitForAnyKey()`, `SleepForMilliseconds()`, `EvaluateTimerBuiltin()` в общий слой (~42 строки дублирования).

## Что можно отложить

1. **TIMING-002..004** [P4]: Дополнительные edge-case тесты.
2. **TIMING-005..006** [P4]: Документация edge cases (midnight wrap, range limit).

## Что не следует трогать

1. **Парсинг SLEEP** — стабилен, простой, корректный.
2. **Привязка SLEEP/TIMER** — строгая типизация, корректная валидация.
3. **Lowering SLEEP** — минимальная трансляция, нет оптимизационных возможностей.
4. **IRuntimeClock** — чистый интерфейс, не требует изменений.
5. **Opcode.Sleep** — корректный, полная валидация.

---

## Главный контрольный вопрос

> Можно ли считать подсистему TIMING / SLEEP достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES.**

Подсистема полностью реализована, протестирована, задокументирована и имеет полный паритет между Interpreter и BytecodeVm. Единственное замечание — дублирование 3 тривиальных методов (~42 строки) — является частью более широкого системного технического долга и не блокирует переход к следующей подсистеме.
