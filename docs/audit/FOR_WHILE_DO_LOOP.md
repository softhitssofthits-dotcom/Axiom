# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## FOR / WHILE / DO / LOOP

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы циклов языка Axiom: операторов `FOR...NEXT`, `WHILE...END WHILE`, `DO...LOOP` (все 5 форм), `EXIT FOR`, `EXIT DO`, `CONTINUE FOR`, `CONTINUE WHILE`, `CONTINUE DO`.

**Главный вывод:** подсистема фундаментально корректна, архитектурно зрела и полностью покрыта на всех уровнях конвейера. Паритет интерпретатора и BytecodeVm подтверждён. Специализированные опкоды `JumpIfForDoneLocal` и `AdvanceForLoopLocal` дают значительное ускорение VM.

**Health Score: 9.1/10**

| Метрика | Оценка |
|---------|--------|
| Корректность | 9/10 |
| Семантика | 9/10 |
| Архитектура | 9/10 |
| Runtime (Interpreter) | 9/10 |
| Runtime (Bytecode VM) | 10/10 |
| Производительность | 9/10 |
| Тесты | 9/10 |
| Samples | 9/10 |
| Документация | 9/10 |
| IDE | 9/10 |

**Ответ на главный вопрос: YES** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

---

## 2. Что представляет собой эта подсистема

Подсистема циклов обеспечивает три основных механизма итеративного выполнения в языке Axiom:

1. **`FOR...NEXT`** — цикл со счётчиком, автоматически объявляющий переменную-счётчик или использующий предобъявленную через `DIM`. Поддерживает `STEP` с положительным и отрицательным шагом.
2. **`WHILE...END WHILE`** — цикл с предусловием, проверяемым перед каждой итерацией.
3. **`DO...LOOP`** — универсальный цикл с 5 формами: `DO WHILE`, `DO UNTIL`, `LOOP WHILE`, `LOOP UNTIL`, бесконечный `DO...LOOP`.

Дополнительно:
- **`EXIT FOR`** / **`EXIT DO`** — досрочный выход из цикла
- **`CONTINUE FOR`** / **`CONTINUE WHILE`** / **`CONTINUE DO`** — переход к следующей итерации

Все формы работают во всех контекстах языка: глобальные операторы, тела `SUB`/`FUNCTION`, вложенные блоки, внутри `TRY/CATCH/FINALLY`.

---

## 3. Полный список возможностей

### SYS-001: FOR...NEXT

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** цикл со счётчиком
- **Пользовательский синтаксис:** `FOR <переменная> = <начало> TO <конец> [STEP <шаг>] ... NEXT`
- **Parser:** `Parser.Statements.cs:1300-1331` — `ParseForStatement()`
- **Syntax Node:** `ForStatementSyntax` (`Statements.cs:560-612`)
- **Binder:** `Binder.ControlFlowStatements.cs:170-269` — `BindForStatement()`
- **Bound Node:** `BoundForStatement` (`BoundStatements.cs:1521-1560`)
- **Interpreter:** `Interpreter.Statements.cs:817-1026` — `ExecuteFor()`, `RunIntegerForLoop()`, `RunLongForLoop()`, `RunDoubleForLoop()`
- **Lowering:** `BoundProgramLowerer.cs:1259-1324` — `LowerFor()`
- **Bytecode:** `BytecodeCompiler.cs:477-652` — collapsed patterns → `JumpIfForDoneLocal`, `AdvanceForLoopLocal`
- **Opcodes:** `JumpIfForDoneLocal` (4 операнда), `AdvanceForLoopLocal` (2 операнда), `ValidateForStep` (0 операндов)
- **Bytecode VM:** `BytecodeVm.cs:1780-1823` (termination), `1871-1885` (advance), `2379-2474` (fast paths)
- **Tests:** 15 parser, 8 binder, 16 interpreter, 4 VM, 15 lowerer, 3 compiler, 2 validator, 16 pipeline
- **Samples:** 7 positive, 1 negative
- **Manual:** `docs/manual/for.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: WHILE...END WHILE

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** цикл с предусловием
- **Пользовательский синтаксис:** `WHILE <условие> ... END WHILE`
- **Parser:** `Parser.Statements.cs:1434-1443` — `ParseWhileStatement()`
- **Syntax Node:** `WhileStatementSyntax` (`Statements.cs:614-647`)
- **Binder:** `Binder.ControlFlowStatements.cs:47-63` — `BindWhileStatement()`
- **Bound Node:** `BoundWhileStatement` (`BoundStatements.cs:1466-1483`)
- **Interpreter:** `Interpreter.Statements.cs:655-675` — `ExecuteWhile()`
- **Lowering:** `BoundProgramLowerer.cs:1232-1257` — `LowerWhile()`
- **Bytecode VM:** стандартные `JumpIfFalse`/`Jump` опкоды
- **Tests:** 1 parser, 1 binder, 3 interpreter, 1 pipeline
- **Samples:** 2 positive
- **Manual:** `docs/manual/while.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: DO...LOOP (5 форм)

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** универсальный цикл с постусловием или предусловием
- **Пользовательский синтаксис:**
  - `DO WHILE <условие> ... LOOP`
  - `DO UNTIL <условие> ... LOOP`
  - `DO ... LOOP WHILE <условие>`
  - `DO ... LOOP UNTIL <условие>`
  - `DO ... LOOP` (бесконечный)
- **Parser:** `Parser.Statements.cs:1446-1457` — `ParseDoLoopStatement()`, `ParseOptionalDoLoopConditionClause()`
- **Syntax Node:** `DoLoopStatementSyntax` (`Statements.cs:649-680`), `DoLoopConditionClauseSyntax` (`Statements.cs:254-265`)
- **Binder:** `Binder.ControlFlowStatements.cs:127-168` — `BindDoLoopStatement()`, `BindDoLoopCondition()`
- **Bound Node:** `BoundDoLoopStatement` (`BoundStatements.cs:1492-1519`), `BoundDoLoopConditionKind` enum
- **Interpreter:** `Interpreter.Statements.cs:739-815` — `ExecuteDoLoop()`, `ShouldContinueDoLoop()`
- **Lowering:** `BoundProgramLowerer.cs:1326-1458` — `LowerDoLoop()`, `LowerPreCheckDoLoop()`, `LowerPostCheckDoLoop()`, `LowerInfiniteDoLoop()`
- **Bytecode VM:** стандартные `JumpIfFalse`/`JumpIfTrue`/`Jump` опкоды
- **Tests:** 5 parser, 4 binder, 5 interpreter, 6 lowerer, 1 pipeline
- **Samples:** 7 positive
- **Manual:** `docs/manual/do-loop.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: EXIT FOR

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** досрочный выход из цикла FOR
- **Пользовательский синтаксис:** `EXIT FOR`
- **Parser:** `Parser.Statements.cs:1512-1522` — `ParseExitStatement()`
- **Binder:** `Binder.ControlFlowStatements.cs:271-279` — `BindExitForStatement()`, валидация `_forDepth > 0`
- **Bound Node:** `BoundExitForStatement` (`BoundStatements.cs:1562-1570`)
- **Interpreter:** `Interpreter.Statements.cs:1028-1031` — `ExecuteExitFor()`, возвращает `ExecutionSignal.ExitFor()`
- **Lowering:** `BoundProgramLowerer.cs:1479-1488` — `LowerExitFor()` с FINALLY unwinding
- **Tests:** 2 parser, 1 binder, 1 interpreter, 3 lowerer, 2 pipeline
- **Samples:** 1 positive
- **Manual:** `docs/manual/for.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: EXIT DO

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** досрочный выход из цикла DO
- **Пользовательский синтаксис:** `EXIT DO`
- **Parser:** `Parser.Statements.cs:1512-1522` — `ParseExitStatement()`
- **Binder:** `Binder.ControlFlowStatements.cs:281-289` — `BindExitDoStatement()`, валидация `_doLoopDepth > 0`
- **Bound Node:** `BoundExitDoStatement` (`BoundStatements.cs:1572-1580`)
- **Interpreter:** `Interpreter.Statements.cs:1033-1036` — `ExecuteExitDo()`, возвращает `ExecutionSignal.ExitDo()`
- **Lowering:** `BoundProgramLowerer.cs:1468-1477` — `LowerExitDo()` с FINALLY unwinding
- **Tests:** 1 parser, 1 binder, 1 interpreter, 2 lowerer, 2 pipeline
- **Samples:** 2 positive
- **Manual:** `docs/manual/do-loop.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: CONTINUE FOR / CONTINUE WHILE / CONTINUE DO

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** переход к следующей итерации цикла
- **Пользовательский синтаксис:** `CONTINUE FOR`, `CONTINUE WHILE`, `CONTINUE DO`
- **Parser:** `Parser.Statements.cs:1524-1535` — `ParseContinueStatement()`
- **Binder:** `Binder.ControlFlowStatements.cs:291-319` — валидация контекста
- **Bound Node:** `BoundContinueForStatement`, `BoundContinueWhileStatement`, `BoundContinueDoStatement`
- **Interpreter:** `Interpreter.Statements.cs:1038-1051` — возвращает соответствующий `ExecutionSignal`
- **Lowering:** `BoundProgramLowerer.cs:1505-1526` — `LowerContinue()` с FINALLY unwinding
- **Tests:** 3 parser, 3 binder, 3 interpreter, 3 lowerer, 3 pipeline
- **Samples:** 3 positive (`continue_for.axm`, `continue_while.axm`, `continue_do.axm`)
- **Manual:** `docs/manual/continue.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: Специализированные FOR-опкоды

- **Категория:** PERFORMANCE
- **Тип:** BYTECODE OPTIMIZATION
- **Назначение:** collapse 15 инструкций проверки → 1 опкод, 4 инструкции инкремента → 1 опкод
- **BytecodeCompiler:** `BytecodeCompiler.cs:477-652` — `TryMatchCollapsedLocalForLoopCheck()`, `TryMatchCollapsedAdvanceForLoopLocal()`
- **Opcodes:** `JumpIfForDoneLocal` (15→1), `AdvanceForLoopLocal` (4→1)
- **Bytecode VM:** `BytecodeVm.cs:1780-1823`, `1871-1885` — fast paths для Integer/Long/Double/Decimal
- **Tests:** 3 compiler tests
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-008: ValidateForStep

- **Категория:** RUNTIME
- **Тип:** ERROR HANDLING
- **Назначение:** проверка STEP ≠ 0 в runtime
- **Lowering:** `BoundProgramLowerer.cs:1278` — `ValidateForStep` после вычисления step
- **Opcode:** `ValidateForStep` (0 операндов, pops step)
- **Bytecode VM:** `BytecodeVm.cs:4858-4871` — проверка, выброс MB3004 при нуле
- **Interpreter:** `Interpreter.Statements.cs:823-827` — `IsZero(step)` проверка
- **Tests:** 2 (interpreter + VM)
- **Samples:** 1 negative (`for_step_zero.axm`)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (TokenKind: ForKeyword, WhileKeyword, DoKeyword, LoopKeyword, NextKeyword, StepKeyword, ExitKeyword, ContinueKeyword)
    ↓
Parser (ForStatementSyntax, WhileStatementSyntax, DoLoopStatementSyntax, ExitFor/DoStatementSyntax, ContinueFor/While/DoStatementSyntax)
    ↓
Binder (BoundForStatement, BoundWhileStatement, BoundDoLoopStatement, BoundExitFor/Do, BoundContinueFor/While/Do)
    ↓
Interpreter (прямое tree-walking: ExecuteFor → RunIntegerForLoop/RunLongForLoop/RunDoubleForLoop, ExecuteWhile, ExecuteDoLoop)
    ↓
Lowering (BoundProgramLowerer: LowerFor, LowerWhile, LowerDoLoop → LowerPreCheck/LowerPostCheck/LowerInfinite)
    ↓
BytecodeCompiler (pattern matching: 15→1 collapse для FOR check, 4→1 collapse для FOR advance)
    ↓
BytecodeVm (JumpIfForDoneLocal с fast paths, AdvanceForLoopLocal с fast paths, ValidateForStep)
```

### 4.2 Ключевые файлы

| Файл | Ответственность | Строк |
|------|----------------|-------|
| `src/Axiom.Parsing/Parser.Statements.cs` | Парсинг всех форм циклов | ~240 (1300-1535) |
| `src/Axiom.Semantics/Binding/Binder.ControlFlowStatements.cs` | Связывание всех форм циклов | ~275 (47-319) |
| `src/Axiom.Semantics/BoundNodes/BoundStatements.cs` | Bound-узлы циклов | ~150 (1466-1610) |
| `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` | Выполнение циклов (Interpreter) | ~400 (655-1051) |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | Понижение в lowered-инструкции | ~300 (1232-1526) |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | Collapsed FOR patterns | ~180 (477-652) |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | Выполнение FOR-опкодов | ~120 (1780-1885, 2379-2474) |
| `src/Axiom.Runtime/Execution/ExecutionSignal.cs` | Сигналы EXIT/CONTINUE | 37 |

### 4.3 Архитектурные решения

1. **Типоспециализированные циклы FOR в Interpreter:** `RunIntegerForLoop()`, `RunLongForLoop()`, `RunDoubleForLoop()` — отдельные методы для каждого числового типа, избегающие boxing/unboxing на каждой итерации.

2. **Collapsed opcodes для FOR:** `BytecodeCompiler` распознаёт 15-инструкционный паттерн проверки границ и схлопывает в один `JumpIfForDoneLocal` с 4 операндами. Аналогично 4-инструкционный паттерн инкремента → `AdvanceForLoopLocal` с 2 операндами.

3. **Fast paths в VM:** `TryShouldTerminateLocalForLoop()` и `TryAdvanceLocalForLoop()` работают напрямую с `GetIntegerUnchecked()`/`GetLongUnchecked()`/`GetDoubleUnchecked()`/`GetDecimalUnchecked()` без boxing.

4. **Отдельные стеки меток для EXIT:** `_forExitLabels`, `_doLoopExitLabels`, `_loopContinueLabels` — три отдельных стека, чтобы `EXIT FOR` не мог выйти из `DO` и наоборот.

5. **FINALLY unwinding:** `LowerExitJump()` и `LowerContinue()` корректно разворачивают активные `TRY/CATCH/FINALLY` контексты перед выходом из цикла.

6. **ExecutionSignal для управления потоком:** Единая структура `ExecutionSignal` с 6 флагами (`HasReturn`, `HasExitFor`, `HasExitDo`, `HasContinueFor`, `HasContinueWhile`, `HasContinueDo`) обеспечивает чистую пропагацию сигналов через вложенные области видимости.

---

## 5. Полный путь выполнения

### 5.1 FOR...NEXT (Interpreter)

1. `Interpreter.Statements.cs:88` — dispatch на `ExecuteFor()`
2. `ExecuteFor()` — вычисление start, end, step
3. Проверка `IsZero(step)` → MB3004
4. Создание `loopScope`, вход в область видимости
5. Определение типа переменной → dispatch на `RunIntegerForLoop()`/`RunLongForLoop()`/`RunDoubleForLoop()`
6. Цикл: проверка границ → выполнение тела → обработка сигналов → инкремент → повтор
7. Выход из области видимости

### 5.2 FOR...NEXT (BytecodeVm)

1. `BoundProgramLowerer.LowerFor()` — эмитит:
   - Вычисление start → `StoreVariable(counter)`
   - Вычисление end → `StoreVariable(endVar)`
   - Вычисление step → `StoreVariable(stepVar)` + `ValidateForStep`
   - `LabelMarker(checkLabel)` + `StatementBoundary`
   - 15-инструкционный паттерн проверки (step > 0? → positive check → negative check)
   - `LabelMarker(bodyLabel)` — тело цикла
   - `LabelMarker(continueLabel)` — 4-инструкционный паттерн инкремента
   - `Jump(checkLabel)` — возврат к проверке
   - `LabelMarker(endLabel)` — конец цикла
2. `BytecodeCompiler.TryMatchCollapsedLocalForLoopCheck()` — 15→1 `JumpIfForDoneLocal`
3. `BytecodeCompiler.TryMatchCollapsedAdvanceForLoopLocal()` — 4→1 `AdvanceForLoopLocal`
4. `BytecodeVm.ExecuteJumpIfForDoneLocalInstruction()` — fast path для Integer/Long/Double/Decimal
5. `BytecodeVm.ExecuteAdvanceForLoopLocalInstruction()` — fast path для инкремента

### 5.3 WHILE...END WHILE

1. `LowerWhile()` — эмитит: start label, condition, JumpIfFalse(end), body, Jump(start), end label
2. `ExecuteWhile()` — `while (EvaluateExpression(condition).AsBoolean()) { ... }`

### 5.4 DO...LOOP (все 5 форм)

1. `LowerDoLoop()` — dispatch на `LowerPreCheckDoLoop()`/`LowerPostCheckDoLoop()`/`LowerInfiniteDoLoop()`
2. `ExecuteDoLoop()` — обработка предусловия/постусловия/бесконечного цикла

---

## 6. Фактический семантический контракт

### 6.1 FOR...NEXT

**Вход:**
- Переменная-счётчик: имя идентификатора
- Начальное значение: числовое выражение (INTEGER, LONG, DOUBLE)
- Конечное значение: числовое выражение того же типа
- Шаг: числовое выражение того же типа (по умолчанию 1 для INTEGER, 1L для LONG, 1.0 для DOUBLE)

**Обработка:**
- Если переменная не объявлена — автоматическое объявление с типом, выведенным из start/end/step
- Если переменная объявлена через `DIM` — повторное использование существующей переменной
- STEP 0 → MB3004 (runtime ошибка)
- Положительный шаг: цикл выполняется пока `current <= end`
- Отрицательный шаг: цикл выполняется пока `current >= end`
- Тело цикла выполняется в дочерней области видимости
- Переменная-счётчик доступна после `NEXT` (в отличие от QuickBASIC)

**Выход:**
- Завершение цикла при выходе за границы
- `EXIT FOR` — досрочный выход
- `RETURN` — выход из функции
- `CONTINUE FOR` — переход к инкременту

**Ограничения:**
- Тип счётчика: только INTEGER, LONG, DOUBLE (не STRING, не BOOLEAN, не DECIMAL)
- STEP 0 → runtime ошибка MB3004
- Переполнение при инкременте → runtime ошибка

### 6.2 WHILE...END WHILE

**Вход:**
- Условие: выражение типа BOOLEAN

**Обработка:**
- Условие проверяется перед каждой итерацией
- Если условие FALSE с первой проверки — тело не выполняется ни разу

**Выход:**
- Условие стало FALSE
- `RETURN` — выход из функции
- `CONTINUE WHILE` — переход к проверке условия

### 6.3 DO...LOOP

**Вход:**
- Условие (опционально): выражение типа BOOLEAN
- Тип условия: WHILE или UNTIL
- Размещение: перед телом (pre-check) или после тела (post-check)

**Обработка:**
- Pre-check (DO WHILE/UNTIL): условие проверяется перед каждой итерацией
- Post-check (LOOP WHILE/UNTIL): тело выполняется минимум 1 раз, условие проверяется после
- Infinite (DO...LOOP без условия): бесконечный цикл, выход только через EXIT DO

**Выход:**
- Условие стало неподходящим (для WHILE: FALSE, для UNTIL: TRUE)
- `EXIT DO` — досрочный выход
- `RETURN` — выход из функции
- `CONTINUE DO` — переход к проверке условия (для post-check) или к началу тела (для pre-check)

---

## 7. Анализ Parser

### 7.1 Парсинг FOR

**Файл:** `Parser.Statements.cs:1300-1331`

Парсер корректно обрабатывает:
- Ключевое слово FOR и имя переменной
- Обязательный `=` и начальное выражение
- Обязательный `TO` и конечное выражение
- Опциональный `STEP` с выражением
- Тело цикла до `NEXT`
- Обязательный `NEXT`

### 7.2 Парсинг WHILE

**Файл:** `Parser.Statements.cs:1434-1443`

Парсер корректно обрабатывает:
- Ключевое слово WHILE и условие
- Тело цикла до `END WHILE`

### 7.3 Парсинг DO...LOOP

**Файл:** `Parser.Statements.cs:1446-1457`

Парсер корректно обрабатывает все 5 форм:
- `DO WHILE <условие> ... LOOP`
- `DO UNTIL <условие> ... LOOP`
- `DO ... LOOP WHILE <условие>`
- `DO ... LOOP UNTIL <условие>`
- `DO ... LOOP` (без условия)

### 7.4 Парсинг EXIT и CONTINUE

**Файл:** `Parser.Statements.cs:1512-1535`

- `EXIT FOR` / `EXIT DO` — через `ParseExitStatement()`
- `CONTINUE FOR` / `CONTINUE WHILE` / `CONTINUE DO` — через `ParseContinueStatement()`

### 7.5 Оценка Parser

Parser чистый, хорошо структурированный. Все формы парсятся корректно.

---

## 8. Анализ Binder

### 8.1 Связывание FOR

**Файл:** `Binder.ControlFlowStatements.cs:170-269`

Ключевая логика:
1. Проверка, существует ли переменная с таким именем
2. Если существует и numeric — повторное использование (`reusesExistingVariable = true`)
3. Если не существует — вывод типа из start/end/step
4. Вход в область видимости цикла
5. Если переменная не предобъявлена — объявление в области видимости цикла
6. Привязка start/end/step выражений с ожидаемым типом
7. Валидация типа счётчика (только INTEGER, LONG, DOUBLE)
8. `_forDepth++` перед телом, `_forDepth--` после

**Ключевое решение:** переменная-счётчик FOR живёт в области видимости цикла, а не внешней области. После `NEXT` переменная всё ещё доступна (в отличие от QuickBASIC, где она уничтожалась).

### 8.2 Связывание WHILE

**Файл:** `Binder.ControlFlowStatements.cs:47-63`

- `BindCondition()` проверяет тип BOOLEAN
- `_whileDepth++` / `_whileDepth--` для валидации CONTINUE WHILE

### 8.3 Связывание DO...LOOP

**Файл:** `Binder.ControlFlowStatements.cs:127-168`

- `BindDoLoopCondition()` — привязка условия с определением типа (WHILE/UNTIL)
- `_doLoopDepth++` / `_doLoopDepth--` для валидации EXIT DO / CONTINUE DO

### 8.4 Валидация EXIT и CONTINUE

- `EXIT FOR` — валидация `_forDepth > 0` (иначе MB2013)
- `EXIT DO` — валидация `_doLoopDepth > 0` (иначе MB2014)
- `CONTINUE FOR` — валидация `_forDepth > 0` (иначе MB2037)
- `CONTINUE WHILE` — валидация `_whileDepth > 0` (иначе MB2038)
- `CONTINUE DO` — валидация `_doLoopDepth > 0` (иначе MB2039)

### 8.5 Оценка Binder

Binder чистый, с правильной проверкой типов и контекста. Разделение на отдельные методы для каждого типа цикла — хорошее архитектурное решение.

---

## 9. Анализ Interpreter

### 9.1 Выполнение FOR

**Файл:** `Interpreter.Statements.cs:817-1026`

- `ExecuteFor()` — вычисление start/end/step, проверка STEP ≠ 0, dispatch по типу
- `RunIntegerForLoop()` — типоспециализированный цикл для INTEGER с `checked` арифметикой
- `RunLongForLoop()` — типоспециализированный цикл для LONG с `checked` арифметикой
- `RunDoubleForLoop()` — типоспециализированный цикл для DOUBLE с `EnsureFiniteDoubleResult()`

**Обработка сигналов в каждом типовом цикле:**
- `HasReturn || HasExitDo || HasContinueWhile || HasContinueDo` → возврат сигнала наверх
- `HasExitFor` → `break` (выход из цикла)
- Нет обработки `HasContinueFor` явно — это потому, что `CONTINUE FOR` в Interpreter просто переходит к инкременту, что естественно происходит при `continue` в C# цикле. Но на самом деле `ExecuteContinueFor()` возвращает `ExecutionSignal.ContinueFor()`, а `RunIntegerForLoop()` не проверяет `HasContinueFor` — он просто продолжает цикл, и инкремент выполняется автоматически. Это корректно, потому что `CONTINUE FOR` в BASIC означает "перейти к NEXT", что эквивалентно инкременту.

### 9.2 Выполнение WHILE

**Файл:** `Interpreter.Statements.cs:655-675`

Компактная и корректная реализация. Обработка всех сигналов.

### 9.3 Выполнение DO...LOOP

**Файл:** `Interpreter.Statements.cs:739-815`

Сложная логика с обработкой всех 5 форм и всех сигналов. Ключевые моменты:
- Pre-check: `!statement.EvaluateConditionAfterBody && !ShouldContinueDoLoop(statement)` → немедленный выход
- Post-check: условие проверяется после тела
- `CONTINUE DO` в post-check форме: повторная проверка условия
- `CONTINUE DO` в pre-check форме: переход к началу тела (условие проверяется на следующей итерации)

### 9.4 Оценка Interpreter

Реализация корректная, типоспециализированные циклы FOR — правильное архитектурное решение для производительности. Обработка сигналов полная и последовательная.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering FOR

**Файл:** `BoundProgramLowerer.cs:1259-1324`

Lowering FOR генерирует ~25 инструкций:
1. Вычисление start → StoreVariable(counter)
2. Вычисление end → StoreVariable(endVar)
3. Вычисление step → StoreVariable(stepVar) + ValidateForStep
4. LabelMarker(check) + StatementBoundary
5. LoadVariable(stepVar) + LoadLiteral(0) + BinaryOperation(>) + JumpIfFalse(negativeStep)
6. [positive path]: LoadVariable(counter) + LoadVariable(endVar) + BinaryOperation(>) + JumpIfTrue(end) + Jump(body)
7. [negative path]: LoadVariable(counter) + LoadVariable(endVar) + BinaryOperation(<) + JumpIfTrue(end)
8. LabelMarker(body) — тело цикла
9. LabelMarker(continue) — LoadVariable(counter) + LoadVariable(stepVar) + BinaryOperation(+) + StoreVariable(counter) + Jump(check)
10. LabelMarker(end)

### 10.2 Collapsed Opcodes

**BytecodeCompiler** распознаёт два паттерна:

**15→1 collapse (JumpIfForDoneLocal):**
Паттерн: step > 0? → positive: current > end? → jump; negative: current < end? → jump
Результат: `JumpIfForDoneLocal(currentSlot, endSlot, stepSlot, endTarget)`

**4→1 collapse (AdvanceForLoopLocal):**
Паттерн: LoadVariable(current) + LoadVariable(step) + BinaryOperation(+) + StoreVariable(current)
Результат: `AdvanceForLoopLocal(currentSlot, stepSlot)`

### 10.3 VM Fast Paths

**`TryShouldTerminateLocalForLoop()`** — прямой unbox + сравнение для Integer/Long/Double/Decimal:
- Integer: `GetIntegerUnchecked(current) > GetIntegerUnchecked(end)` при положительном шаге
- Long: аналогично
- Double: аналогично
- Decimal: аналогично

**`TryAdvanceLocalForLoop()`** — прямой unbox + checked add:
- Integer: `checked(GetIntegerUnchecked(current) + GetIntegerUnchecked(step))`
- Long: аналогично
- Double: `TryCreateFiniteDouble(current + step)`
- Decimal: `checked(GetDecimalUnchecked(current) + GetDecimalUnchecked(step))`

### 10.4 Оценка Bytecode / VM

Lowering чистый и корректный. Collapsed opcodes — значительная оптимизация (15→1 и 4→1 инструкции). Fast paths минимизируют overhead на каждой итерации. VM поддерживает Decimal в fast paths, хотя Interpreter не имеет типоспециализированного цикла для Decimal (но это не проблема, т.к. Binder не выводит Decimal для FOR-счётчика).

---

## 11. Анализ Runtime

### 11.1 ExecutionSignal

**Файл:** `ExecutionSignal.cs`

Единая структура с 6 флагами обеспечивает чистую пропагацию сигналов:
- `HasReturn` — выход из функции
- `HasExitFor` — выход из FOR
- `HasExitDo` — выход из DO
- `HasContinueFor` — переход к NEXT
- `HasContinueWhile` — переход к проверке WHILE
- `HasContinueDo` — переход к проверке/началу DO

### 11.2 Область видимости FOR

Переменная-счётчик FOR живёт в области видимости цикла. Тело цикла выполняется в дочерней области видимости (`ExecuteStatementsInChildScope`). Это означает:
- Переменные, объявленные внутри тела, уничтожаются после каждой итерации
- Счётчик доступен после `NEXT` (область видимости цикла шире тела)

### 11.3 Область видимости WHILE/DO

Тело WHILE/DO выполняется в дочерней области видимости. Переменные, объявленные внутри тела, уничтожаются после каждой итерации.

---

## 12. Взаимодействие с типами

### 12.1 FOR

| Тип счётчика | Поддержка | Примечание |
|---------------|-----------|------------|
| INTEGER | ✅ | Типоспециализированный цикл |
| LONG | ✅ | Типоспециализированный цикл |
| DOUBLE | ✅ | Типоспециализированный цикл |
| DECIMAL | ❌ | Не выводится Binder'ом, VM поддерживает в fast path |
| STRING | ❌ | MB2009 |
| BOOLEAN | ❌ | MB2009 |

### 12.2 WHILE/DO

- Условие должно быть типа BOOLEAN
- Нет неявного преобразования чисел в логические

---

## 13. Взаимодействие со String

Строки не могут быть счётчиком FOR (MB2009). Строковые значения могут использоваться в условиях WHILE/DO через функции, возвращающие BOOLEAN.

---

## 14. Взаимодействие с Array

Массивы могут использоваться внутри тел циклов. FOR-циклы часто используются как итераторы по массивам:

```basic
DIM values(1 TO 5) AS INTEGER
FOR i = 1 TO 5
    PRINT values(i)
NEXT
```

---

## 15. Взаимодействие с TYPE

Поля TYPE могут использоваться в условиях WHILE/DO и как начальные/конечные значения FOR:

```basic
TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE

DIM p AS Point
FOR i = 1 TO p.X
    PRINT i
NEXT
```

---

## 16. Взаимодействие с Functions / SUB

- Функции могут вызываться в условиях WHILE/DO и в выражениях start/end/step FOR
- `RETURN` внутри цикла корректно завершает функцию
- Замыкания корректно захватывают переменные из внешних областей видимости циклов

---

## 17. Взаимодействие с Control Flow

### 17.1 Вложенные циклы

Все формы циклов можно вкладывать друг в друга без ограничений. `EXIT FOR` выходит только из ближайшего `FOR`, `EXIT DO` — только из ближайшего `DO`.

### 17.2 Циклы внутри IF/SELECT CASE

Циклы корректно работают внутри ветвей IF и SELECT CASE.

### 17.3 TRY/CATCH/FINALLY внутри циклов

TRY/CATCH/FINALLY корректно работает внутри тел циклов. `EXIT FOR` и `EXIT DO` корректно разворачивают FINALLY при выходе из TRY-блока.

### 17.4 CONTINUE и EXIT в одном цикле

`CONTINUE FOR` и `EXIT FOR` могут использоваться в одном цикле. `CONTINUE` пропускает остаток тела и переходит к инкременту, `EXIT` выходит из цикла.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Семантические ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB2009 | Неподдерживаемый тип счётчика FOR | FOR с STRING/BOOLEAN счётчиком |
| MB2013 | EXIT FOR вне цикла FOR | EXIT FOR без FOR |
| MB2014 | EXIT DO вне цикла DO | EXIT DO без DO |
| MB2016 | Затенение переменной FOR | FOR счётчик затеняет внешнюю переменную |
| MB2037 | CONTINUE FOR вне цикла FOR | CONTINUE FOR без FOR |
| MB2038 | CONTINUE WHILE вне цикла WHILE | CONTINUE WHILE без WHILE |
| MB2039 | CONTINUE DO вне цикла DO | CONTINUE DO без DO |

### 18.2 Runtime ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB3004 | STEP 0 в цикле FOR | FOR i = 1 TO 3 STEP 0 |
| MB3011 | Переполнение стека вызовов | Рекурсивный вызов из тела цикла |

### 18.3 FINALLY unwinding

`EXIT FOR` и `EXIT DO` корректно разворачивают активные `FINALLY` блоки при выходе из TRY-контекста. Это реализовано в `LowerExitJump()` и `LowerContinue()`.

---

## 19. State / Lifetime / Ownership

### 19.1 Переменная-счётчик FOR

- **Создаётся:** Binder (если не предобъявлена) или Interpreter (при выполнении)
- **Владелец:** Область видимости цикла
- **Lifetime:** От начала цикла до выхода из области видимости
- **Уничтожается:** При выходе из `ExecuteFor()` (LeaveScope)

### 19.2 Временные переменные FOR (end, step)

- **Создаётся:** `BoundProgramLowerer.CreateForTemporary()`
- **Владелец:** Текущая routine
- **Lifetime:** От вычисления до конца цикла
- **Уничтожаются:** При выходе из routine

### 19.3 Стеки меток

- **`_forExitLabels`** — стек меток конца FOR-циклов
- **`_doLoopExitLabels`** — стек меток конца DO-циклов
- **`_loopContinueLabels`** — стек меток продолжения (общий для всех типов циклов)
- Все три стека push/pop при входе/выходе из цикла

---

## 20. Edge Cases

### 20.1 Покрытые edge cases

| Сценарий | Покрыт | Доказательство |
|----------|--------|----------------|
| STEP 0 | ✅ | MB3004, `for_step_zero.axm` |
| Отрицательный STEP | ✅ | `step_loop.axm`, `Lower_ForNextWithNegativeStep_UsesSameDeterministicBranchShape()` |
| STEP 1 (по умолчанию) | ✅ | `basic_loop.axm` |
| Переполнение INTEGER при инкременте | ✅ | `checked` арифметика в Interpreter, `TryAdvanceLocalForLoop` с OverflowException catch |
| Переполнение LONG при инкременте | ✅ | Аналогично |
| DOUBLE infinity при инкременте | ✅ | `EnsureFiniteDoubleResult()` в Interpreter, `TryCreateFiniteDouble()` в VM |
| FOR с предобъявленной переменной | ✅ | `predeclared_counter.axm`, `Bind_ForReusesPredeclaredNumericControlVariable()` |
| FOR с неподдерживаемым типом счётчика | ✅ | MB2009, `predeclared_counter_invalid_type.axm` |
| Вложенные FOR | ✅ | `nested_loops.axm`, `Lower_NestedForNext_UsesDeterministicLabels()` |
| EXIT FOR из вложенного цикла | ✅ | `Execute_ExitForBreaksNearestLoop()`, `Lower_NestedExitFor_TargetsNearestForLoop()` |
| CONTINUE FOR с отрицательным шагом | ✅ | `Execute_ContinueForWithNegativeStep()` |
| CONTINUE FOR во вложенном цикле | ✅ | `Execute_NestedContinueForTargetsNearestFor()` |
| DO WHILE с FALSE условием с первой проверки | ✅ | Тело не выполняется |
| DO UNTIL с TRUE условием с первой проверки | ✅ | Тело не выполняется |
| LOOP WHILE выполняется минимум 1 раз | ✅ | `Execute_DoLoopWithTrailingWhileConditionRunsAtLeastOnce()` |
| LOOP UNTIL выполняется минимум 1 раз | ✅ | `Execute_DoLoopWithTrailingUntilConditionRunsUntilConditionBecomesTrue()` |
| Бесконечный DO...LOOP с EXIT DO | ✅ | `Execute_DoLoopWithoutConditionUsesExitDo()` |
| FINALLY при EXIT FOR из TRY | ✅ | `Run_WithBytecodeMode_RunsFinallyBeforeExitForLeavesTry()` |
| FINALLY при EXIT DO из TRY | ✅ | `Run_WithBytecodeMode_RunsFinallyBeforeExitDoLeavesTry()` |
| CONTINUE FOR/WHILE/DO вне контекста | ✅ | MB2037/MB2038/MB2039 |
| EXIT FOR/DO вне контекста | ✅ | MB2013/MB2014 |

### 20.2 Потенциально непокрытые edge cases

| Сценарий | Статус | Примечание |
|----------|--------|------------|
| FOR с DECIMAL счётчиком | Не выводится Binder'ом | VM поддерживает в fast path, но Binder не генерирует |
| FOR с очень большим числом итераций (>2^31) | Не тестировано | INTEGER переполнится, LONG должен работать |
| WHILE с побочными эффектами в условии | Не тестировано явно | Должно работать, но нет явного теста |
| DO...LOOP с TRY/CATCH внутри тела и CONTINUE | Не тестировано явно | FINALLY unwinding должен работать |
| Рекурсивный вызов функции из тела цикла | Не тестировано явно | Должно работать через изолированные фреймы |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| FOR...NEXT (INTEGER) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FOR...NEXT (LONG) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FOR...NEXT (DOUBLE) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FOR...NEXT (STEP отрицательный) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FOR...NEXT (STEP 0) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FOR...NEXT (предобъявленный счётчик) | ✅ | ✅ | ✅ | ✅ | ✅ |
| WHILE...END WHILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DO WHILE...LOOP | ✅ | ✅ | ✅ | ✅ | ✅ |
| DO UNTIL...LOOP | ✅ | ✅ | ✅ | ✅ | ✅ |
| DO...LOOP WHILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| DO...LOOP UNTIL | ✅ | ✅ | ✅ | ✅ | ✅ |
| DO...LOOP (бесконечный) | ✅ | ✅ | ✅ | ✅ | ✅ |
| EXIT FOR | ✅ | ✅ | ✅ | ✅ | ✅ |
| EXIT DO | ✅ | ✅ | ✅ | ✅ | ✅ |
| CONTINUE FOR | ✅ | ✅ | ✅ | ✅ | ✅ |
| CONTINUE WHILE | ✅ | ✅ | ✅ | ✅ | ✅ |
| CONTINUE DO | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB3004 (STEP 0) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB2009 (неподдерживаемый тип) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB2013 (EXIT FOR вне FOR) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB2014 (EXIT DO вне DO) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB2037-2039 (CONTINUE вне контекста) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FINALLY при EXIT FOR | ✅ | ✅ | ✅ | ✅ | ✅ |
| FINALLY при EXIT DO | ✅ | ✅ | ✅ | ✅ | ✅ |

**Расхождений не найдено.** Полный паритет подтверждён.

---

## 22. Анализ производительности

### 22.1 Benchmark

**Сценарий:** `samples/smoke/engine-compare/control_flow_branch_maze.axm`

| Метрика | Interpreter | Bytecode VM | Ускорение | Классификация |
|---------|-------------|-------------|-----------|---------------|
| Runtime | baseline | 0.826x | -17.4% | CLEAR WIN |
| Total | baseline | 0.814x | -18.6% | CLEAR WIN |

### 22.2 Почему VM быстрее

1. **Collapsed opcodes:** `JumpIfForDoneLocal` (15→1) и `AdvanceForLoopLocal` (4→1) значительно сокращают число инструкций на итерацию
2. **Fast paths:** `TryShouldTerminateLocalForLoop()` и `TryAdvanceLocalForLoop()` работают без boxing
3. **Прямой unbox:** `GetIntegerUnchecked()`, `GetLongUnchecked()`, `GetDoubleUnchecked()` — `AggressiveInlining`
4. **Нет overhead вызова:** VM не вызывает `EvaluateExpression()` на каждой итерации для проверки границ
5. **StatementBoundary:** `EmitLoopIterationBoundary()` только если `_emitStatementBoundaries` (для событий мыши/клавиатуры)

---

## 23. Качество Benchmark

- Сценарий `control_flow_branch_maze.axm` покрывает FOR, WHILE, DO WHILE, DO UNTIL, DO с EXIT DO в смеси с IF/SELECT CASE
- 220 внешних итераций с множеством внутренних циклов
- Длинные прогоны (10 итераций, 3 прогрева) для стабильности

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot paths

- `ExecuteJumpIfForDoneLocalInstruction()` — основной hot path для FOR-циклов
- `ExecuteAdvanceForLoopLocalInstruction()` — hot path для инкремента
- `TryShouldTerminateLocalForLoop()` — inline fast path для проверки границ
- `TryAdvanceLocalForLoop()` — inline fast path для инкремента

### 24.2 Bottlenecks

Серьёзных bottleneck'ов не обнаружено. Подсистема циклов является одним из наиболее оптимизированных участков VM.

---

## 25. Архитектурные проблемы

Серьёзных архитектурных проблем не обнаружено.

**Мелкие наблюдения:**

1. **Interpreter не имеет типоспециализированного цикла для DECIMAL** (`Interpreter.Statements.cs:879`). Если в будущем Binder будет выводить DECIMAL для FOR-счётчика, понадобится `RunDecimalForLoop()`. Сейчас это не проблема, т.к. Binder не выводит DECIMAL.

2. **`ExecuteDoLoop()` сложнее, чем `ExecuteWhile()`** из-за обработки 5 форм и всех сигналов. Но сложность обоснована — каждая форма имеет свою семантику.

---

## 26. Дублирование логики

### 26.1 Типоспециализированные циклы FOR в Interpreter

`RunIntegerForLoop()`, `RunLongForLoop()`, `RunDoubleForLoop()` содержат похожую структуру (проверка границ → тело → сигналы → инкремент), но с разными типами. Это намеренное дублирование для производительности (avoid boxing).

### 26.2 Fast paths в VM

`TryShouldTerminateLocalForLoop()` и `TryAdvanceLocalForLoop()` содержат 4 ветки для 4 типов. Это намеренное дублирование для производительности.

### 26.3 Оценка

Дублирование в подсистеме циклов является **намеренным** и **обоснованным** — оно обеспечивает типоспециализированное выполнение без boxing/unboxing.

---

## 27. Single Source of Truth

- **Синтаксис:** `SyntaxKind.cs`, `Statements.cs`
- **Семантика:** `Binder.ControlFlowStatements.cs`
- **Bound модель:** `BoundStatements.cs`
- **Runtime поведение:** `Interpreter.Statements.cs`
- **Bytecode:** `BoundProgramLowerer.cs`, `BytecodeCompiler.cs`, `BytecodeVm.cs`
- **Диагностики:** `DiagnosticCodes.cs`

Источники правды чётко разделены. Конфликтов не обнаружено.

---

## 28. Legacy / Dead Code

Не обнаружено. Весь код активно используется.

---

## 29. Надёжность и безопасность

- Нет неконтролируемого доступа к ресурсам
- `checked` арифметика предотвращает молчаливое переполнение
- `EnsureFiniteDoubleResult()` предотвращает бесконечные значения
- Execution timeout защищает от бесконечных циклов
- `DispatchInputHandlers()` обеспечивает отзывчивость UI во время длинных циклов

---

## 30. Анализ тестов

### 30.1 Покрытие по уровням

| Уровень | Кол-во тестов | Покрытие |
|---------|---------------|----------|
| Syntax | 12 | Kind checks, null validation |
| Parser | 15 | Все формы FOR/WHILE/DO, EXIT, CONTINUE, вложенность |
| Binder | 21 | Типы, предобъявленный счётчик, EXIT/CONTINUE валидация |
| Interpreter | 16 | Все формы, сигналы, вложенность, STEP 0 |
| Bytecode VM | 4 | Предобъявленный счётчик, STEP 0, ON MOUSE в пустом цикле |
| Bytecode Lowerer | 15 | Все формы DO, EXIT, CONTINUE, FOR с паттернами |
| Bytecode Compiler | 3 | Collapsed patterns |
| Bytecode Validator | 2 | Type mismatch, non-numeric step |
| Execution Pipeline | 16 | Все формы, EXIT/CONTINUE, FINALLY unwinding |
| Acceptance | 11 | Positive + negative samples |
| IDE | 1 | Smart identifier casing |

### 30.2 Качество assertions

Тесты проверяют:
- Конкретные типы bound-узлов
- Конкретные diagnostic codes
- Конкретный вывод программ
- Конкретные последовательности lowered-инструкций
- FINALLY unwinding при EXIT
- Signal propagation через вложенные циклы

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Статус |
|----------|--------|
| FOR с DECIMAL (если будет поддерживаться) | N/A — не поддерживается |
| WHILE с побочными эффектами в условии | Нет явного теста |
| DO...LOOP с TRY/CATCH внутри тела и CONTINUE | Нет явного теста |

### 31.2 Граничные случаи

| Сценарий | Статус |
|----------|--------|
| FOR с очень большим числом итераций (>2^31 для LONG) | Нет |
| FOR с DOUBLE step = 0.1 (точность) | Нет |
| WHILE с условием, которое всегда TRUE (бесконечный цикл + timeout) | Нет явного теста |

---

## 32. Анализ Samples

### 32.1 FOR samples

| Sample | Демонстрирует | Проверяемый результат |
|--------|---------------|----------------------|
| `basic_loop.axm` | Базовый FOR | 1, 2, 3 |
| `step_loop.axm` | STEP 2 | 0, 2, 4, 6 |
| `nested_loops.axm` | Вложенные FOR + WHILE | "1:1", "1:2", "2:1", "2:2" |
| `exit_for.axm` | EXIT FOR | 1, 2, "done" |
| `continue_for.axm` | CONTINUE FOR | 1, 2, 4, 5 |
| `predeclared_counter.axm` | Предобъявленный DIM | 1, 2, 3, "after=4" |
| `predeclared_counter_invalid_type.axm` | MB2009 | Negative |

### 32.2 WHILE samples

| Sample | Демонстрирует | Проверяемый результат |
|--------|---------------|----------------------|
| `basic_loop.axm` | Базовый WHILE | 0, 1, 2 |
| `continue_while.axm` | CONTINUE WHILE | 1, 2, 4, 5 |

### 32.3 DO samples

| Sample | Демонстрирует | Проверяемый результат |
|--------|---------------|----------------------|
| `do_while_loop.axm` | DO WHILE | 0, 1, 2, "done" |
| `do_until_loop.axm` | DO UNTIL | 0, 1, 2 |
| `loop_while.axm` | LOOP WHILE | 0, 1, 2 |
| `loop_until.axm` | LOOP UNTIL | 0, 1, 2 |
| `do_loop_exit.axm` | EXIT DO | 0, 1, "done" |
| `exit_do.axm` | EXIT DO из DO WHILE | 0, 1, "done" |
| `continue_do.axm` | CONTINUE DO | 1, 2, 4, 5 |

---

## 33. Анализ документации

### 33.1 Manual

| Страница | Качество | Примечание |
|----------|----------|------------|
| `loops.html` | ✅ Хорошее | Обзорная страница с ссылками |
| `for.html` | ✅ Хорошее | Полный синтаксис, STEP, EXIT FOR, CONTINUE FOR, диагностики |
| `while.html` | ✅ Хорошее | Полный синтаксис, CONTINUE WHILE |
| `do-loop.html` | ✅ Хорошее | Все 5 форм, EXIT DO, CONTINUE DO |
| `continue.html` | ✅ Хорошее | Все 3 формы CONTINUE, вложенность |

### 33.2 Соответствие кода и документации

Расхождений не обнаружено. Документация accurately отражает фактическое поведение.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

- `FOR`, `NEXT`, `STEP`, `WHILE`, `END WHILE`, `DO`, `LOOP`, `EXIT`, `CONTINUE` — в категории Keyword
- Обе темы (светлая и тёмная) покрыты

### 34.2 Автозавершение блоков

- `FOR ...` + Enter → вставка `NEXT`
- `WHILE ...` + Enter → вставка `END WHILE`
- `DO ...` + Enter → вставка `LOOP`
- Вложенные блоки корректно обрабатываются

### 34.3 Форматирование

- Ключевые слова переводятся в верхний регистр
- Структурный отступ для тел циклов

### 34.4 Smart Identifier

- `SmartIdentifierService` корректно нормализует регистр переменной-счётчика FOR внутри тела цикла

---

## 35. Зависимости от Host / Platform

Специфических зависимостей от платформы нет. Подсистема циклов использует только стандартные возможности .NET.

---

## 36. Карта корневых причин

Все найденные проблемы имеют отдельные, несвязанные корневые причины. Общей корневой причины не обнаружено.

---

## 37. Стабильные области

**Не следует трогать:**

1. **Collapsed opcodes** (`JumpIfForDoneLocal`, `AdvanceForLoopLocal`) — доказанная выгода производительности
2. **Типоспециализированные циклы FOR в Interpreter** — правильное архитектурное решение
3. **Fast paths в VM** — минимизируют overhead на каждой итерации
4. **Отдельные стеки меток** для EXIT FOR/EXIT DO/CONTINUE — корректная изоляция
5. **FINALLY unwinding** при EXIT/CONTINUE — корректно реализован
6. **ExecutionSignal** — единая модель пропагации сигналов

---

## 38. Возможности консолидации

Консолидация не требуется. Намеренное дублирование в типоспециализированных циклах обосновано производительностью.

---

## 39. Возможности разделения ответственности

Разделение ответственности уже выполнено корректно:
- `Binder.ControlFlowStatements.cs` — все формы циклов
- `Interpreter.Statements.cs` — выполнение всех форм
- `BoundProgramLowerer.cs` — lowering всех форм

---

## 40. Кандидаты на удаление

Кандидатов на удаление не обнаружено. Весь код активно используется.

---

## 41. Готовность к дальнейшему развитию

Подсистема готова к:
- Добавлению `FOR EACH` для итерации по массивам и коллекциям
- Добавлению `DECIMAL` как типа счётчика FOR (VM уже поддерживает)
- Интеграции с будущим отладчиком (breakpoints на строках циклов)
- Дальнейшей оптимизации (superinstructions для WHILE/DO)

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P4 | TESTS | Tests | Нет явного теста WHILE с побочными эффектами в условии | Отсутствие test methods | Упущение при написании тестов | HIGH |
| SYS-002 | P4 | TESTS | Tests | Нет теста DO...LOOP с TRY/CATCH и CONTINUE | Отсутствие test methods | Упущение при написании тестов | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0-4 — Нет необходимости

Подсистема стабильна, семантика корректна, архитектура чистая, производительность на целевом уровне.

### ЭТАП 5 — Расширение тестирования (опционально)

**TASK-001:** Добавить тест WHILE с побочными эффектами в условии
- Файлы: `tests/Axiom.Runtime.Tests/InterpreterTests.String.cs`
- Сложность: низкая
- Риск: нет

**TASK-002:** Добавить тест DO...LOOP с TRY/CATCH и CONTINUE DO
- Файлы: `tests/Axiom.Runtime.Tests/InterpreterTests.TryCatch.cs`
- Сложность: низкая
- Риск: нет

---

## 44. Граф зависимостей задач

```
TASK-001 (WHILE побочные эффекты) ─── независимая
TASK-002 (DO + TRY/CATCH + CONTINUE) ─── независимая
```

---

## 45. Definition of Done

Подсистема FOR/WHILE/DO/LOOP считается завершённой, если:

- [x] Parser корректно разбирает все формы циклов
- [x] Binder корректно проверяет типы и контекст
- [x] Interpreter корректно выполняет все формы с типоспециализацией
- [x] Lowering корректно транслирует в линейные инструкции
- [x] BytecodeCompiler корректно схлопывает FOR-паттерны
- [x] BytecodeVm корректно выполняет все опкоды с fast paths
- [x] EXIT/CONTINUE корректно работают с FINALLY unwinding
- [x] Все diagnostic codes покрыты тестами
- [x] Паритет Interpreter ↔ BytecodeVm подтверждён
- [x] Samples покрывают все основные сценарии
- [x] Manual актуален
- [x] IDE подсветка и автозавершение работают

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **Нет проблем корректности и архитектуры уровня P0-P2.**

Мелкие находки (P4):
1. Нет явного теста WHILE с побочными эффектами в условии (P4)
2. Нет теста DO...LOOP с TRY/CATCH и CONTINUE DO (P4)

---

## 47. ТОП-20 задач по производительности

1. **Нет задач по производительности.** Подсистема уже на целевом уровне (VM быстрее на 17-19%). Collapsed opcodes и fast paths обеспечивают оптимальную производительность.

---

## 48. Итоговая оценка здоровья подсистемы

| Метрика | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 9/10 | Все формы циклов работают корректно на обоих движках |
| Семантика | 9/10 | Строгая проверка типов, корректная блочная видимость |
| Архитектура | 9/10 | Чистое разделение ответственности, типоспециализация |
| Runtime (Interpreter) | 9/10 | Типоспециализированные циклы, корректная обработка сигналов |
| Runtime (Bytecode VM) | 10/10 | Collapsed opcodes, fast paths, максимальная производительность |
| Производительность | 9/10 | VM быстрее на 17-19%, collapsed opcodes 15→1 и 4→1 |
| Память | 9/10 | Минимальные аллокации, временные переменные FOR |
| Error handling | 9/10 | STEP 0, переполнение, FINALLY unwinding |
| Tests | 9/10 | 100+ тестов по всем уровням |
| Samples | 9/10 | 17 positive + 1 negative + benchmarks |
| Documentation | 9/10 | 5 manual-страниц, спецификация, архитектура |
| IDE | 9/10 | Подсветка, автозавершение, форматирование, smart identifier |
| Maintainability | 9/10 | Код чистый, хорошо структурированный |
| Reliability | 9/10 | Стабильное поведение, нет известных багов |
| Future readiness | 9/10 | Готово к расширению (FOR EACH, DECIMAL) |

**Средняя оценка: 9.1/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему FOR/WHILE/DO/LOOP достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES**

---

## Основные blockers

Нет. Подсистема стабильна и корректна.

## Что нужно сделать в первую очередь

Нет критических задач.

## Что можно отложить

- Тест WHILE с побочными эффектами (P4)
- Тест DO...LOOP с TRY/CATCH и CONTINUE (P4)

## Что не следует трогать

- Collapsed opcodes (`JumpIfForDoneLocal`, `AdvanceForLoopLocal`)
- Типоспециализированные циклы FOR в Interpreter
- Fast paths в VM
- Отдельные стеки меток для EXIT/CONTINUE
- FINALLY unwinding при EXIT/CONTINUE
- ExecutionSignal модель
