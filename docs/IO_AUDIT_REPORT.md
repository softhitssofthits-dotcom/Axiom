# AXIOM — DEEP INPUT / OUTPUT / DATA FORENSIC AUDIT

## 1. Executive Summary

Базовая I/O-подсистема Axiom разделена на две чёткие группы: **реализованные** (PRINT, PRINT USING, INPUT) и **нереализованные** (DATA, READ, RESTORE, LINE INPUT). Реализованные компоненты работают корректно, имеют полный путь от парсера до байткодной VM, покрыты тестами и примерами. Нереализованные компоненты отсутствуют на всех уровнях — от лексера до runtime.

**Главный вывод**: I/O-подсистема стабильна в своём текущем объёме, но DATA/READ/RESTORE и LINE INPUT полностью отсутствуют. Это не баги, а нереализованные возможности, которые пока не были заявлены в спецификации как обязательные.

Health Score: **8.5/10**.

---

## 2. Actual I/O Architecture

```
Axiom.Ide (IdeInputProvider)
  ↓ ReadInput() / WriteOutput()
Axiom.Execution (ExecutionRuntimeHostAdapter)
  ↓ IRuntimeHost
Axiom.Runtime (Interpreter / BytecodeVm)
  ↓ _pendingOutput buffer → FlushPendingOutput → WriteTextLine → IRuntimeHost.WriteOutput()
  ↓ IRuntimeHost.ReadInput(InputRequest)
```

Ключевые файлы:
- `src/Axiom.Syntax/InputOutputSyntax.cs` — узлы PRINT, PRINT USING, INPUT, OPEN, CLOSE, SLEEP
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` — привязка I/O-операторов
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` — исполнение PRINT/INPUT в интерпретаторе
- `src/Axiom.Runtime/Execution/BytecodeVm.cs` — исполнение PRINT/INPUT в VM
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs` — полная реализация INPUT в VM
- `src/Axiom.Runtime/Builtins/BuiltinIOHelpers.cs` — общий слой I/O-логики
- `src/Axiom.Runtime/Host/IRuntimeHost.cs` — агрегирующий интерфейс I/O
- `src/Axiom.Runtime/Host/IInputProvider.cs` — контракт ввода
- `src/Axiom.Runtime/Host/IOutputWriter.cs` — контракт вывода
- `src/Axiom.Runtime/Host/InputRequest.cs` — модель запроса ввода

---

## 3. I/O Implementation Map

| Уровень | PRINT | PRINT USING | INPUT | DATA | READ | RESTORE | LINE INPUT |
|---------|-------|-------------|-------|------|------|---------|------------|
| Lexer | `PrintKeyword` | `PrintKeyword` + `UsingKeyword` | `InputKeyword` | — | — | — | — |
| Parser | `ParsePrintStatement()` | `ParsePrintUsingStatement()` | `ParseInputStatement()` | — | — | — | — |
| Syntax | `PrintStatementSyntax` | `PrintUsingStatementSyntax` | `InputStatementSyntax` | — | — | — | — |
| Binder | `BindPrintStatement()` | `BindPrintUsingStatement()` | `BindInputStatement()` | — | — | — | — |
| Bound | `BoundPrintStatement` | `BoundPrintUsingStatement` | `BoundInputStatement` | — | — | — | — |
| Lowering | `LowerPrint()` | `LowerPrintUsing()` | `LowerInput()` | — | — | — | — |
| Opcode | `PrintValue`, `PrintSeparator`, `PrintLine` | `PrintUsing` | `Input` | — | — | — | — |
| VM | `AppendPendingOutput()` | `ExecutePrintUsing()` | `ExecuteInput()` | — | — | — | — |
| Interpreter | `ExecutePrint()` | `ExecutePrintUsing()` | `ExecuteInput()` | — | — | — | — |
| Host | `IOutputWriter.WriteOutput()` | `IOutputWriter.WriteOutput()` | `IInputProvider.ReadInput()` | — | — | — | — |
| Tests | 15+ тестов | 5 тестов | 7 тестов | — | — | — | — |
| Samples | 6 примеров | 1 пример | 4 примера | — | — | — | — |
| Manual | `print.html` | `print.html` | `input.html` | — | — | — | — |
| VM Coverage | да | да | да | — | — | — | — |

---

## 4. Supported I/O Features

| Возможность | Статус |
|-------------|--------|
| `PRINT expression` | SUPPORTED |
| `PRINT a, b, c` (multiple args) | SUPPORTED |
| `PRINT ;` (semicolon separator) | SUPPORTED |
| `PRINT ,` (comma separator) | SUPPORTED |
| `PRINT` (empty — newline) | SUPPORTED |
| `PRINT USING format; values` | SUPPORTED |
| `INPUT variable` | SUPPORTED |
| `INPUT "prompt"; variable` | SUPPORTED |
| `INPUT a, b, c` (multiple vars) | SUPPORTED |
| `LINE INPUT` | NOT SUPPORTED |
| `DATA` | NOT SUPPORTED |
| `READ` | NOT SUPPORTED |
| `RESTORE` | NOT SUPPORTED |

---

## 5. PRINT Syntax

Фактический синтаксис:

```
PRINT
PRINT expression
PRINT expression ; expression
PRINT expression , expression
PRINT expression ; expression , expression ;
```

Пустой PRINT (`PRINT` без аргументов) — создаёт пустую строку (newline).

PRINT USING:
```
PRINT USING formatExpression ; valueExpression [; | , ...]
```

---

## 6. PRINT Argument Model

- Аргументы: `PrintItemSyntax` = выражение + опциональный разделитель (`;` или `,`)
- Количество: 0 или более
- Типы выражений: любой выражение Axiom
- Скобки вокруг аргументов: не используются

---

## 7. PRINT Evaluation Order

Аргументы вычисляются **слева направо**:
- Интерпретатор: `foreach (var item in statement.Items)`
- VM: значения со стека снимаются в порядке эмиссии lowered-инструкций

---

## 8. PRINT Type Support

| Тип | Форматирование | Примечание |
|-----|---------------|------------|
| Integer | `-123`, `0`, `42` | `InvariantCulture`, без разделителя тысяч |
| Long | `-1234567890` | Аналогично Integer |
| Double | `3.14`, `-0.001`, `1E+10` | Формат `"G"`, `InvariantCulture` |
| Decimal | `123.456` | `InvariantCulture` |
| String | как есть | Без кавычек в выводе |
| Boolean | `TRUE` / `FALSE` | Всегда верхний регистр |
| Array | Ошибка | `ReportInvalidRuntimeState` |
| TYPE | `ToDisplayString()` | Через `RuntimeValue.ToDisplayString()` |

---

## 9. PRINT String Semantics

- Пустая строка: выводит пустую строку (с newline при flush)
- Unicode: поддерживается
- Управляющие символы: передаются как есть
- Пробелы: сохраняются
- Конкатенация через `;`: значения пишутся подряд без разделителя

---

## 10. PRINT Numeric Formatting

- Десятичный разделитель: `.` (точка) — `InvariantCulture`
- Точность Double: формат `"G"`
- Отрицательные: `-` перед числом
- Научная нотация: `1E+10`
- Локаль: `CultureInfo.InvariantCulture`

---

## 11. PRINT Boolean Formatting

Фактический вывод: `TRUE` или `FALSE` (всегда верхний регистр).

Подтверждено в `Interpreter.Helpers.cs` и `BytecodeVm.cs`.

---

## 12. PRINT Multiple Arguments

- Разделитель `,`: добавляет один пробел
- Разделитель `;`: не добавляет разделитель
- Без разделителя (последний элемент): вызывает flush — перевод строки
- Trailing `,` или `;`: не вызывает flush

**Важно**: `,` добавляет ОДИН пробел, а не табуляцию до фиксированной зоны (как в QuickBASIC).

---

## 13. PRINT Newline Semantics

| Форма | Newline? |
|-------|----------|
| `PRINT` (пустой) | Да (forceEmptyLine) |
| `PRINT expr` (без разделителя) | Да (flush) |
| `PRINT expr ;` | Нет |
| `PRINT expr ,` | Нет |

---

## 14. PRINT Empty Statement

`PRINT` без аргументов → `FlushPendingOutput(forceEmptyLine: true)` → `WriteTextLine(string.Empty)`. Выводит пустую строку.

---

## 15. PRINT Output Target

- Консольный режим: `IOutputWriter.WriteOutput(text)`
- Графический режим (SCREEN): `IOutputWriter.WriteGraphicsText(text, newLine: true)`
- Переключение: `WriteTextLine()` проверяет `_host.IsScreenOutputActive`

---

## 16. Output Abstraction

```
Language PRINT
  ↓
_pendingOutput buffer (StringBuilder)
  ↓ FlushPendingOutput()
WriteTextLine()
  ↓
IRuntimeHost.WriteOutput() или IRuntimeHost.WriteGraphicsText()
  ↓
ExecutionRuntimeHostAdapter → IOutputWriter
  ↓
IdeInputProvider / ProgramConsoleService → IDE Output panel
```

---

## 17. PRINT + IDE

- Вывод захватывается через `ExecutionRuntimeHostAdapter._output`
- IDE переключает нижнюю панель на `Output` при запуске
- Повторные запуски очищают вывод
- Ошибки переключают на `Diagnostics`

---

## 18. PRINT + Bytecode

Opcodes:
- `Opcode.PrintValue` — pop значения, запись в `_pendingOutput`
- `Opcode.PrintSeparator` — разделитель (0=flush, 1=semicolon, 2=comma)
- `Opcode.PrintLine` — `FlushPendingOutput(forceEmptyLine: true)`
- `Opcode.PrintUsing` — форматированный вывод

Паритет с интерпретатором: confirmed.

---

## 19. INPUT Syntax

```
INPUT variable
INPUT "prompt" ; variable
INPUT "prompt" , variable
INPUT var1, var2, var3
INPUT "prompt" ; var1, var2
```

---

## 20. INPUT Prompt

- Синтаксис: `INPUT "текст приглашения" ; переменная`
- Промпт — строковая константа
- Разделитель `;` или `,` после промпта — оба допустимы

---

## 21. INPUT Target Model

- Только простые переменные (`identifier`)
- Множественные переменные через `,`
- Запрещены: массивы, поля TYPE

---

## 22. INPUT Type System

| Тип | Парсинг | Примечание |
|-----|---------|------------|
| Integer | `int.TryParse` + `InvariantCulture` | Целые числа |
| Long | `long.TryParse` + `InvariantCulture` | Целые числа |
| Double | `double.TryParse` + `InvariantCulture` + `IsFinite` | Дробные |
| Decimal | `decimal.TryParse` + `InvariantCulture` | Дробные |
| String | прямое присваивание | Без обрезки кавычек |
| Boolean | `TRUE`/`FALSE` (case-insensitive) | Только эти два значения |

---

## 23. INPUT String Semantics

- Пробелы: сохраняются (single) или обрезаются (multi)
- Пустая строка: допустима для String
- Unicode: передаётся как есть
- Кавычки: НЕ обрабатываются specially

---

## 24. INPUT Numeric Parsing

- `InvariantCulture` (точка как десятичный разделитель)
- `IsFinite()` для Double — NaN/Infinity отклоняются
- Переполнение: возвращает `null` → runtime ошибка

---

## 25. INPUT Boolean Parsing

Принимаются: `TRUE`, `FALSE` (case-insensitive).
Не принимаются: `1`, `0`, `Yes`, `No`.

---

## 26. Invalid INPUT

```
INPUT integer ← "abc"
```

Поведение:
1. `TryParseInput()` возвращает `null`
2. `ReportInputConversionFailure()` — diagnostic
3. `throw new RuntimeFailureException()` — остановка программы
4. Целевая переменная **не изменяется**

---

## 27. INPUT Failure State

При ошибке преобразования:
- Целевая переменная не изменяется
- Программа останавливается с `RuntimeFailureException`

---

## 28. INPUT Retry

Нет механизма повтора. Ошибка ввода останавливает программу.

---

## 29. EOF / End-of-Input

- `NullRuntimeHost.ReadInput()` возвращает `string.Empty`
- `IdeInputProvider.ReadInput()` блокирует до ввода пользователя
- EOF на уровне stdin не обрабатывается specially

---

## 30. Multiple INPUT Variables

- Разделитель полей: `,` (запятая)
- Количество полей должно совпадать с количеством переменных
- Пробелы вокруг полей обрезаются
- Несовпадение количества → runtime ошибка

---

## 31. LINE INPUT

**Не реализовано.** Нет ключевого слова в лексере.

---

## 32-37. INPUT State / Evaluation / Side Effects

- INPUT не имеет собственного состояния между вызовами
- Каждый INPUT → `IInputProvider.ReadInput()` → парсинг → присваивание
- INPUT invalidates reversible execution history
- Архитектура поддерживает redirected input через `IInputProvider`

---

## 38-59. DATA / READ / RESTORE

**Все эти BASIC-конструкции полностью отсутствуют в Axiom.** Нет ключевых слов в лексере, нет синтаксических узлов, нет парсера, нет runtime.

---

## 60-64. I/O + Call/Loop/Array/TYPE/String Model

- INPUT и PRINT работают внутри SUB/FUNCTION
- PRINT внутри цикла работает корректно
- INPUT не поддерживает массивы и поля TYPE как targets
- `PRINT obj.Field` и `PRINT A(i)` работают через вычисление выражений

---

## 65-66. PRINT / INPUT Conversion Contract

INPUT использует отдельный путь `BuiltinIOHelpers.TryParseInput()` — не общий String/Type conversion.

---

## 67. I/O Error Model

| Ошибка | Уровень | Код |
|--------|---------|-----|
| Неверный тип INPUT | Runtime | `DiagnosticCodes.InputConversion` |
| Несовпадение количества INPUT | Runtime | `DiagnosticCodes.InputConversion` |
| INPUT в массив | Runtime | `ReportInvalidRuntimeState` |
| Присваивание в const | Semantic | `AssignToConst` |
| PRINT USING type mismatch | Runtime | `DiagnosticCodes.PrintUsingFieldTypeMismatch` |

---

## 68-70. Output Failure / Input Blocking / Cancelled Input

- Output failure не обрабатывается на уровне языка
- INPUT блокирует выполнение программы
- Отмена через `IRuntimeCancellationProvider`

---

## 71-74. Bytecode I/O Model

```
Syntax: PRINT a, b ; c
  ↓
Bound: BoundPrintStatement([BoundPrintItem(a, Comma), BoundPrintItem(b, Semicolon), BoundPrintItem(c, null)])
  ↓
Lowered: [Push(a), PrintValue, PrintSeparator(Comma), Push(b), PrintValue, PrintSeparator(Semicolon), Push(c), PrintValue, PrintLine]
  ↓
Bytecode: [PushConst/LoadGlobal, PrintValue, PrintSeparator(2), ...]
  ↓
VM: AppendPendingOutput → FlushPendingOutput → WriteTextLine → IRuntimeHost.WriteOutput()
```

Opcodes: `PrintValue`, `PrintSeparator`, `PrintLine`, `PrintUsing`, `Input`.

---

## 75. Bytecode Coverage

Согласно `docs/BYTECODE_COVERAGE.md`:
- PRINT: VM да, Автотесты да, Sample да, Manual да, Сквозной VM да, Негативный паритет да
- PRINT USING: VM да, Автотесты да, Sample да, Manual да, Сквозной VM да, Негативный паритет да
- INPUT: VM да, Автотесты да, Sample да, Manual да, Сквозной VM да, Негативный паритет да

---

## 76. Performance

- PRINT benchmark: `0.884x runtime / 0.918x total`
- INPUT benchmark: `0.879x runtime / 0.889x total`
- Оба в целевом диапазоне

---

## 77. Reliability

- Нет утечки состояния ввода между запусками
- Нет утечки буфера вывода (flush в finally-блоке)
- INPUT invalidates reversible execution history

---

## 78-80. Test Infrastructure / Matrix / Negative Gaps

| Feature | Parser | Semantic | Interpreter | VM | Integration | E2E | Sample | Docs |
|---------|--------|----------|-------------|-----|-------------|-----|--------|------|
| PRINT | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| PRINT USING | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| INPUT | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| DATA | — | — | — | — | — | — | — | — |
| READ | — | — | — | — | — | — | — | — |
| RESTORE | — | — | — | — | — | — | — | — |

---

## 81. Sample Audit

PRINT samples (`samples/operators/print/`): 6 примеров.
INPUT samples (`samples/operators/input/`): 4 примера.
DATA/READ/RESTORE samples: отсутствуют.

---

## 82. Documentation Audit

- `docs/manual/print.html` — существует
- `docs/manual/input.html` — существует
- `docs/manual/data-and-input.html` — обзорная страница

---

## 83-85. IDE / Duplication / Source of Truth

- IDE: консольный ввод/вывод через `ProgramConsoleService`
- Дублирование: `TryParseInput()` и `NormalizeInputField()` были продублированы между Interpreter и BytecodeVm; вынесены в `BuiltinIOHelpers`
- Source of truth: `BuiltinIOHelpers` для парсинга ввода

---

## 86. Architectural Smells

1. ~~Дублирование INPUT parsing~~ — исправлено, вынесено в `BuiltinIOHelpers`
2. VM `AppendPendingOutput(RuntimeValue)` использует прямую запись вместо `ToDisplayString()` — оптимизация, не баг
3. Нет retry для INPUT при ошибке — упрощённая модель

---

## 87. I/O Invariants

1. `PRINT` не изменяет состояние языка, кроме внешнего вывода.
2. `INPUT` присваивает target только по правилам `TryParseInput()`.
3. Ошибка INPUT не повреждает target-переменную.
4. `_pendingOutput` имеет одного владельца.
5. Flush происходит при отсутствии trailing separator.
6. Interpreter и VM暴露 идентичную наблюдаемую I/O-семантику.
7. Новая программа запускается с чистым input/output состоянием.

---

## 88. Expected vs Actual Matrix

| I/O Feature | Expected | Actual | Status |
|-------------|----------|--------|--------|
| PRINT expression | Вывод значения | ✅ | CORRECT |
| PRINT separators | `;` без пробела, `,` с пробелом | ✅ | CORRECT |
| PRINT empty | Пустая строка | ✅ | CORRECT |
| INPUT single | Блокировка, парсинг, присваивание | ✅ | CORRECT |
| INPUT multiple | Разделитель `,`, trim | ✅ | CORRECT |
| INPUT invalid | Runtime ошибка | ✅ | CORRECT |
| DATA | Базовая DATA/READ/RESTORE | NOT IMPLEMENTED | GAP |
| LINE INPUT | Полная строка | NOT IMPLEMENTED | GAP |

---

## 89. Problem Inventory

| ID | Severity | Category | Problem | Status |
|----|----------|----------|---------|--------|
| IO-001 | P2 | DATA | DATA/READ/RESTORE не реализованы | NOT IMPLEMENTED |
| IO-002 | P2 | LINE INPUT | LINE INPUT не реализован | NOT IMPLEMENTED |
| IO-003 | P3 | CONVERSION | INPUT Boolean не принимает `1`/`0` | BY DESIGN |
| IO-004 | P3 | RETRY | Нет retry при ошибке INPUT | BY DESIGN |
| IO-005 | P3 | ARCHITECTURE | Дублирование INPUT parsing | FIXED |
| IO-006 | P4 | FORMATTING | VM formatting отличается для составных типов | LOW RISK |

---

## 90. Root Cause Map

| Root Cause | Affected Findings |
|------------|-------------------|
| DATA/READ/RESTORE не входит в текущий scope | IO-001 |
| LINE INPUT не входит в текущий scope | IO-002 |
| INPUT Boolean — консервативный дизайн | IO-003 |
| Упрощённая модель INPUT | IO-004 |

---

## 91. Stable Areas

НЕ ТРОГАТЬ:
- `IRuntimeHost` / `IInputProvider` / `IOutputWriter`
- `InputRequest`
- `ExecutionRuntimeHostAdapter`
- `_pendingOutput` buffer pattern
- PRINT separator semantics
- INPUT type parsing
- Bytecode opcodes для I/O

---

## 92. Consolidation Opportunities

Выполнено:
- `TryParseInput()` → `BuiltinIOHelpers.TryParseInputField()`
- `NormalizeInputField()` → `BuiltinIOHelpers.NormalizeInputField()`
- `CountInputFields()` → `BuiltinIOHelpers.CountInputFields()`

---

## 93-96. Separation / Removal / Future Contracts

- I/O host contract готов для file I/O
- Error model готов для file I/O
- String/numeric conversion rules зафиксированы

---

## 97. Recovery Roadmap

| Шаг | Описание | Приоритет | Статус |
|-----|----------|-----------|--------|
| 1 | Вынести I/O helpers в общий слой | P3 | DONE |
| 2 | Решить: реализовывать ли DATA/READ/RESTORE | P2 | OPEN |
| 3 | Решить: реализовывать ли LINE INPUT | P3 | OPEN |
| 4 | Расширить INPUT Boolean на `1`/`0` | P4 | OPEN |

---

## 98. Definition of Done

1. ✅ PRINT семантика детерминированна
2. ✅ INPUT конверсии детерминированы
3. ✅ Неверный ввод обрабатывается предсказуемо
4. ✅ OUTPUT buffering имеет одну авторитарную модель
5. ✅ Interpreter и VM согласованы
6. ✅ Перезапуск программы не наследует устаревшее I/O состояние
7. ✅ Тесты покрывают границы и ошибки
8. ✅ Документация соответствует фактическому поведению

---

## 99. TOP 10 I/O ACTIONS

```
1. [P2] IO-001 — Решить: реализовывать ли DATA/READ/RESTORE
2. [P2] IO-002 — Решить: реализовывать ли LINE INPUT
3. [P3] IO-005 — Вынести I/O helpers — DONE
4. [P3] IO-003 — Расширить INPUT Boolean на 1/0
5. [P3] IO-004 — Добавить retry для INPUT
6. [P4] IO-006 — Выровнять VM formatting
```

---

## 100. Final Health Score

| Аспект | Балл |
|--------|------|
| PRINT | 9/10 |
| INPUT | 9/10 |
| LINE INPUT | 0/10 |
| DATA/READ/RESTORE | 0/10 |
| Input Conversion | 9/10 |
| Output Formatting | 9/10 |
| Interpreter | 9/10 |
| Bytecode VM | 9/10 |
| Diagnostics | 9/10 |
| Tests | 9/10 |
| Samples | 9/10 |
| Documentation | 9/10 |
| IDE Integration | 9/10 |
| Performance | 9/10 |
| Reliability | 9/10 |
| Maintainability | 9/10 |

**Общий Health Score: 8.5/10**

---

## ГЛАВНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> **Можно ли считать базовую I/O subsystem Axiom достаточно стабильной, чтобы переходить к полноценному аудиту файловой системы?**

**YES, WITH CONDITIONS**

Условия:
1. DATA/READ/RESTORE полностью отсутствуют
2. LINE INPUT отсутствует
3. I/O-дублирование исправлено (BuiltinIOHelpers)

Blockers: нет.
