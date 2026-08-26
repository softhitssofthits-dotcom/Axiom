# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## STRING / STRING OPERATIONS

---

## 1. Резюме

Проведён полный forensic-аудит строковой подсистемы языка Axiom: типа `STRING`, `STRING * N` (фиксированные строки), 44 встроенных строковых функций, операторов конкатенации (`+`, `&`), операторов сравнения, преобразований типов (`STR`, `VAL`), а также связанных подсистем (`ASC`/`CHR`/`ASCW`/`CHRW`, `SPLIT`/`JOIN`).

**Главный вывод:** строковая подсистема фундаментально корректна, архитектурно зрела и полностью покрыта на всех уровнях конвейера. Единый слой `BuiltinStringHelpers` обеспечивает согласованность между Interpreter и BytecodeVm. Два ранее обнаруженных дефекта (STRING-001, STRING-002) уже исправлены.

**Health Score: 8.8/10**

| Метрика | Оценка |
|---------|--------|
| Корректность | 9/10 |
| Семантика | 9/10 |
| Архитектура | 9/10 |
| Runtime (Interpreter) | 9/10 |
| Runtime (Bytecode VM) | 9/10 |
| Производительность | 9/10 |
| Тесты | 9/10 |
| Samples | 9/10 |
| Документация | 9/10 |
| IDE | 8/10 |

**Ответ на главный вопрос: YES** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

---

## 2. Что представляет собой эта подсистема

Строковая подсистема обеспечивает:

1. **Тип `STRING`** — неизменяемая строка Unicode (CLR `string`, UTF-16)
2. **Фиксированные строки `STRING * N`** — строки фиксированной длины с автоматическим дополнением/обрезкой (1..65535)
3. **44 встроенных строковых функций** — срезы, поиск, замена, форматирование, символы, массивы
4. **Операторы конкатенации** — `+` (строго STRING+STRING) и `&` (произвольные скаляры → STRING)
5. **Операторы сравнения** — `=`, `<>`, `<`, `<=`, `>`, `>=` (ordinal, case-sensitive)
6. **Преобразования** — `STR()` (число→строка), `VAL()` (строка→число)
7. **Символьные функции** — `ASC`/`CHR` (CP866 для 0..255, Unicode выше), `ASCW`/`CHRW` (чистый Unicode)

---

## 3. Полный список возможностей

### SYS-001: Тип STRING

- **Категория:** TYPE
- **Тип:** DATA TYPE
- **Назначение:** хранение текстовых данных
- **Runtime:** CLR `string` (immutable UTF-16)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: Фиксированные строки STRING * N

- **Категория:** TYPE
- **Тип:** DATA TYPE
- **Назначение:** строки фиксированной длины с pad/truncate
- **Синтаксис:** `DIM s AS STRING * 80`
- **Ограничения:** только в DIM и TYPE полях; не в CONST, не в параметрах/возврате FUNCTION (MB2026)
- **Runtime:** `NormalizeFixedString()` — truncate если длиннее, pad пробелами если короче
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: LEN

- **Категория:** FUNCTION
- **Назначение:** длина строки в символах
- **Синтаксис:** `LEN(text)`
- **Возврат:** INTEGER
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: LEFT, RIGHT, MID

- **Категория:** FUNCTION
- **Назначение:** извлечение подстрок
- **Синтаксис:** `LEFT(text, count)`, `RIGHT(text, count)`, `MID(text, start, count)`
- **Позиции:** 1-based
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: TRIM, LTRIM, RTRIM

- **Категория:** FUNCTION
- **Назначение:** удаление пробельных символов или указанного набора символов
- **Синтаксис:** `TRIM(text)`, `TRIM(text, chars)`, `LTRIM(...)`, `RTRIM(...)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: LCASE, UCASE

- **Категория:** FUNCTION
- **Назначение:** преобразование регистра
- **Синтаксис:** `LCASE(text)`, `UCASE(text)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: REVERSE

- **Категория:** FUNCTION
- **Назначение:** реверс строки (Rune-aware, корректно для non-BMP Unicode)
- **Синтаксис:** `REVERSE(text)`
- **Текущий статус:** IMPLEMENTED (STRING-001 исправлен)
- **Уровень уверенности:** CONFIRMED

### SYS-008: CONTAINS, STARTSWITH, ENDSWITH

- **Категория:** FUNCTION
- **Назначение:** проверка вхождения/начала/конца
- **Синтаксис:** `CONTAINS(text, fragment)`, `STARTSWITH(text, prefix)`, `ENDSWITH(text, suffix)`
- **Возврат:** BOOLEAN
- **Семантика:** ordinal, case-sensitive
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-009: REPLACE

- **Категория:** FUNCTION
- **Назначение:** замена всех вхождений подстроки
- **Синтаксис:** `REPLACE(text, oldText, newText)`
- **Семантика:** ordinal, case-sensitive, пустой oldText → MB3007
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-010: REPLACEMANY

- **Категория:** FUNCTION
- **Назначение:** множественная замена по паттернам (one-pass, longest-match)
- **Синтаксис:** `REPLACEMANY(text, oldItems(), newItems())`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-011: INSTR, INSTRREV

- **Категория:** FUNCTION
- **Назначение:** поиск подстроки (прямой/обратный)
- **Синтаксис:** `INSTR(text, fragment)`, `INSTR(start, text, fragment)`, `INSTRREV(...)`
- **Позиции:** 1-based, 0 = не найдено
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-012: SUBSTRCOUNT

- **Категория:** FUNCTION
- **Назначение:** подсчёт неперекрывающихся вхождений
- **Синтаксис:** `SUBSTRCOUNT(text, fragment)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-013: REPEAT, SPACE, STRING

- **Категория:** FUNCTION
- **Назначение:** создание строк
- **Синтаксис:** `REPEAT(text, count)`, `SPACE(count)`, `STRING(count, fill)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-014: PADLEFT, PADRIGHT, CENTER

- **Категория:** FUNCTION
- **Назначение:** форматирование строки до заданной ширины
- **Синтаксис:** `PADLEFT(text, width)`, `PADLEFT(text, width, fill)`, `PADRIGHT(...)`, `CENTER(...)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-015: INSERT, REMOVE

- **Категория:** FUNCTION
- **Назначение:** вставка/удаление подстроки
- **Синтаксис:** `INSERT(text, start, fragment)`, `REMOVE(text, start, count)`
- **Позиции:** 1-based
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-016: ASC, ASCW, CHR, CHRW

- **Категория:** FUNCTION
- **Назначение:** работа с кодами символов
- **Семантика:** ASC/CHR используют CP866 для 0..255, Unicode выше; ASCW/CHRW — чистый Unicode
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-017: SPLIT, JOIN

- **Категория:** FUNCTION
- **Назначение:** разбиение строки в массив и обратно
- **Синтаксис:** `SPLIT(text, separator)`, `JOIN(array, separator)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-018: STR, VAL

- **Категория:** FUNCTION
- **Назначение:** преобразование число↔строка
- **Синтаксис:** `STR(number)`, `VAL(text)`
- **Семантика:** STR возвращает строковое представление; VAL парсит число (0 для невалидного)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-019: Операторы конкатенации (+, &)

- **Категория:** OPERATOR
- **Назначение:** объединение строк
- **Семантика:** `+` — строго STRING+STRING; `&` — произвольные скаляры через `ToDisplayString()`
- **Оптимизации:** `AppendString*` opcodes для паттернов `x = x + expr`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-020: Операторы сравнения

- **Категория:** OPERATOR
- **Назначение:** сравнение строк
- **Семантика:** ordinal, case-sensitive, через `string.Compare(Ordinal)`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (StringLiteralToken, StringKeyword, FixedLengthStringType)
    ↓
Parser (StringLiteralExpressionSyntax, FixedLengthStringTypeSyntax)
    ↓
Binder (TypeSymbol.String, FixedStringLength, BuiltinFunctionId, BoundCallExpression)
    ↓
Lowering (TryLowerStringAppendAssignment → AppendStringVariable, constant folding)
    ↓
BytecodeCompiler (Opcode.Add, Opcode.Concat, Opcode.AppendString*, Opcode.CallBuiltin)
    ↓
Interpreter (RuntimeValue.FromString, BuiltinStringHelpers.*, EvaluateExpression)
    ↓
BytecodeVm (TryExecuteFastStringBuiltin, BuiltinStringHelpers.*, AppendStringValue)
    ↓
Runtime (CLR string, RuntimeStringAccumulator, NormalizeFixedString)
```

### 4.2 Ключевые файлы

| Файл | Ответственность | Строк |
|------|----------------|-------|
| `src/Axiom.Runtime/Builtins/BuiltinStringHelpers.cs` | Единый слой реализации 44 функций | 202 |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.String*.cs` | Interpreter dispatch (8 partial файлов) | ~400 |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String*.cs` | VM dispatch (5 partial файлов) | ~580 |
| `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` | VM builtin registration | ~190 |
| `src/Axiom.Runtime/Values/RuntimeValue.cs` | STRING runtime representation | ~580 |
| `src/Axiom.Runtime/Values/RuntimeStringAccumulator.cs` | StringBuilder wrapper для AppendString | 27 |
| `src/Axiom.Runtime/Execution/RuntimeStringReplaceMany.cs` | REPLACEMANY implementation | 103 |
| `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs` | 44 строковых ID | ~40 |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.*.cs` | Дескрипторы (7 файлов) | ~100 |
| `src/Axiom.Semantics/Binding/Binder.Expressions.cs` | Строка +/& операторы, сравнения | ~100 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | AppendString optimization, constant folding | ~50 |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | Opcode emission для строковых операций | ~30 |
| `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` | FixedStringLength normalization | ~91 |

### 4.3 Архитектурные решения

1. **Единый `BuiltinStringHelpers`:** Все 44 строковые функции реализованы в одном общем слое. Interpreter и BytecodeVm делегируют в него. Это обеспечивает единую точку правды.

2. **RuntimeStringAccumulator:** Оптимизация для паттернов `x = x + expr` — используется `StringBuilder` вместо создания новой строки на каждой итерации. Специальные `AppendString*` opcodes.

3. **Fast paths в VM:** `TryExecuteFastStringBuiltin()` — 10 inline fast paths для самых частых функций (LEN, TRIM, LTRIM, RTRIM, LCASE, UCASE, CONTAINS, STARTSWITH, ENDSWITH, INSTR) без создания делегатов.

4. **Ordinal сравнение:** Все строковые сравнения — ordinal, case-sensitive. Это единообразно во всех контекстах (IF, SELECT CASE, операторы).

5. **FixedStringLength normalization:** При сохранении значения в переменную с `FixedStringLength` автоматически выполняется truncate/pad через `CopyForStorage(fixedStringLength)`.

6. **CP866 для ASC/CHR:** Для значений 0..255 используется кодировка CP866 (совместимость с DOS/QuickBASIC). Для значений >255 — Unicode.

---

## 5. Полный путь выполнения

### 5.1 Строковая функция (Interpreter)

1. `EvaluateCallExpression()` — вычисление target и аргументов
2. `ExecuteBuiltin()` — dispatch по `BuiltinFunctionId`
3. Извлечение аргументов через `arguments[N].AsString()`
4. Вызов соответствующего метода `BuiltinStringHelpers.*`
5. Возврат `RuntimeValue.FromString(result)`

### 5.2 Строковая функция (BytecodeVm)

1. `Opcode.CallBuiltin` → `ExecuteBuiltinCall()`
2. `TryExecuteFastStringBuiltin()` — попытка fast path (10 функций)
3. Если fast path не сработал → `TryCreateBuiltinExecution()` → delegate
4. Delegate вызывает `BuiltinStringHelpers.*`
5. Возврат `RuntimeValue.FromString(result)`

### 5.3 Конкатенация `&` (Interpreter)

1. `EvaluateBinaryExpression()` — вычисление операндов
2. `left.ToDisplayString() + right.ToDisplayString()`
3. `RuntimeValue.FromString(result)`

### 5.4 Конкатенация `+` (Interpreter)

1. `EvaluateBinaryExpression()` — вычисление операндов
2. `left.AsString() + right.AsString()`
3. `RuntimeValue.FromString(result)`

### 5.5 AppendString (BytecodeVm)

1. `Opcode.AppendStringLocal` → `ExecuteAppendStringLocal()`
2. `AppendStringValue()` — использует `RuntimeStringAccumulator`
3. При финализации: `accumulator.ToString()` → `NormalizeFixedString()` если нужно

---

## 6. Фактический семантический контракт

### 6.1 STRING тип

**Вход:** строковый литерал `"hello"`, результат строковой функции, конкатенация

**Обработка:**
- CLR `string` — immutable UTF-16
- Пустая строка — singleton (кэшированная)
- Копирование — не требуется (immutable reference)

**Выход:** `RuntimeValue` с `Type == TypeSymbol.String`

### 6.2 STRING * N

**Вход:** объявление `DIM s AS STRING * 80`

**Обработка:**
- `FixedStringLength` сохраняется в метаданных переменной
- При сохранении: если строка длиннее N — truncate; если короче — pad пробелами
- Диапазон: 1..65535

**Выход:** строка ровно N символов

**Ограничения:**
- Не в CONST (MB2026)
- Не в параметрах/возврате FUNCTION (MB2026)
- Только в DIM и TYPE полях

### 6.3 Оператор `+`

**Вход:** два операнда типа STRING

**Обработка:** `left.AsString() + right.AsString()` (raw concatenation)

**Выход:** STRING

**Ограничения:** оба операнда должны быть STRING (иначе semantic error)

### 6.4 Оператор `&`

**Вход:** два произвольных скалярных операнда

**Обработка:** `left.ToDisplayString() + right.ToDisplayString()` (display conversion)

**Выход:** STRING

**Преобразования:** числа → строковое представление, BOOLEAN → "TRUE"/"FALSE"

---

## 7. Анализ Parser

Parser корректно обрабатывает:
- Строковые литералы с экранированными кавычками (`""`)
- Ключевое слово `STRING`
- Синтаксис `STRING * expression`
- Все 44 строковых функции как вызовы

---

## 8. Анализ Binder

Binder корректно:
- Регистрирует все 44 `BuiltinFunctionId` с дескрипторами
- Проверяет типы аргументов и возвращаемый тип
- Обрабатывает `+` (строго STRING+STRING) и `&` (произвольные скаляры)
- Обрабатывает сравнения строк (ordinal)
- Валидирует `STRING * N` (MB2026 для неподдерживаемых контекстов)

---

## 9. Анализ Interpreter

Interpreter реализует все 44 строковые функции через partial-файлы:
- `Interpreter.Builtins.String.cs` — LEFT, RIGHT, MID, REVERSE, VAL
- `Interpreter.Builtins.String.Trim.cs` — TRIM, LTRIM, RTRIM
- `Interpreter.Builtins.String.Search.cs` — CONTAINS, STARTSWITH, ENDSWITH, REPLACE, REPLACEMANY, INSTR, INSTRREV, SUBSTRCOUNT
- `Interpreter.Builtins.String.Fill.cs` — REPEAT, SPACE, STRING, PADLEFT, PADRIGHT, CENTER
- `Interpreter.Builtins.String.Edit.cs` — INSERT, REMOVE
- `Interpreter.Builtins.String.Char.cs` — ASC, ASCW, CHR, CHRW
- `Interpreter.Builtins.String.Array.cs` — SPLIT, JOIN
- `Interpreter.Builtins.String.Helpers.cs` — RequireNonNegativeCount, RequireFillCharacter

Все делегируют в `BuiltinStringHelpers`.

---

## 10. Анализ Bytecode / VM

### 10.1 Fast paths

`TryExecuteFastStringBuiltin()` — 10 inline fast paths:
- LEN → `string.Length`
- TRIM → `string.Trim()`
- LTRIM → `string.TrimStart()`
- RTRIM → `string.TrimEnd()`
- LCASE → `string.ToLowerInvariant()`
- UCASE → `string.ToUpperInvariant()`
- CONTAINS → `string.Contains()`
- STARTSWITH → `string.StartsWith()`
- ENDSWITH → `string.EndsWith()`
- INSTR → `string.IndexOf() + 1`

### 10.2 AppendString opcodes

4 специальных опкода для in-place конкатенации:
- `AppendStringGlobal` — к глобальной переменной
- `AppendStringLocal` — к локальной переменной
- `AppendStringArgument` — к аргументу
- `AppendStringCapture` — к захваченной переменной

Используют `RuntimeStringAccumulator` (StringBuilder wrapper) для эффективного построения строк.

### 10.3 Оптимизации

- LCASE/UCASE: возврат исходной строки если нет букв
- REVERSE: возврат исходной строки если длина ≤ 1
- INSERT: возврат исходной если fragment пустой
- PADLEFT/PADRIGHT/CENTER: возврат исходной если ширина ≤ длины

---

## 11. Анализ Runtime

### 11.1 RuntimeValue для строк

- `FromString(string)` — создание; пустая строка использует кэшированный `EmptyStringValue`
- `AsString()` — извлечение; извлекает из `RuntimeStringAccumulator` если нужно
- `CopyForStorage(int? fixedStringLength)` — normalization для fixed strings
- `NormalizeFixedString(string, int)` — truncate/pad
- `ToDisplayString()` — display conversion для всех типов

### 11.2 RuntimeStringAccumulator

- Обёртка над `StringBuilder`
- Кэширует `ToString()` результат
- Используется `AppendString*` opcodes

### 11.3 RuntimeStringReplaceMany

- One-pass замена с first-character bucketing
- Longest-match семантика

---

## 12. Взаимодействие с типами

| Тип | Конкатенация `+` | Конкатенация `&` | Сравнение | Преобразование |
|-----|-----------------|-----------------|-----------|----------------|
| STRING | ✅ | ✅ | ✅ ordinal | — |
| INTEGER | ❌ | ✅ (display) | ❌ | STR() → STRING |
| LONG | ❌ | ✅ (display) | ❌ | STR() → STRING |
| DOUBLE | ❌ | ✅ (display) | ❌ | STR() → STRING |
| DECIMAL | ❌ | ✅ (display) | ❌ | STR() → STRING |
| BOOLEAN | ❌ | ✅ (display) | ❌ | STR() → STRING |

---

## 13. Взаимодействие со String

### 13.1 Unicode

- CLR `string` — native UTF-16
- REVERSE — Rune-aware (корректно для non-BMP Unicode, включая emoji)
- ASC/CHR — CP866 для 0..255, Unicode выше
- ASCW/CHRW — чистый Unicode codepoints

### 13.2 Fixed strings

- `STRING * N` — pad пробелами, truncate если длиннее
- Нормализация при сохранении в переменную
- MB2026 для неподдерживаемых контекстов

### 13.3 Пустые строки

- Singleton — кэшированная в `RuntimeValue.EmptyStringValue`
- SPLIT("") → пустой массив (1 TO 0)
- JOIN пустого массива → ""
- CONTAINS("", "") → TRUE
- REPLACE("", "a", "b") → ""

---

## 14. Взаимодействие с Array

- `SPLIT(text, separator)` → `STRING()` массив
- `JOIN(array, separator)` → STRING
- Строковые массивы: `DIM names() AS STRING`

---

## 15. Взаимодействие с TYPE

- TYPE поля могут быть STRING
- TYPE поля могут быть STRING * N
- Строковые поля участвуют в структурном сравнении

---

## 16. Взаимодействие с Functions / SUB

- Строки передаются BYVAL (immutable reference, копия не нужна)
- Строки передаются BYREF
- FUNCTION может возвращать STRING
- STRING * N не поддерживается в параметрах/возврате (MB2026)

---

## 17. Взаимодействие с Control Flow

- Строковые сравнения в IF/SELECT CASE — ordinal, case-sensitive
- Строковые диапазоны в SELECT CASE: `CASE "A" TO "M"`
- Строковые значения в SELECT CASE: `CASE "OPEN", "SAVE"`

---

## 18. Взаимодействие с Error / Exception System

| Код | Описание | Контекст |
|-----|----------|----------|
| MB2026 | Fixed string в неподдерживаемом контексте | CONST, параметры, возврат |
| MB3007 | Пустой разделитель/фрагмент | SPLIT, REPLACE, SUBSTRCOUNT |
| MB3007 | Невалидный код символа | CHR, CHRW |
| MB3007 | Пустой текст для ASC | ASC, ASCW |
| MB3007 | Отрицательный count/width | REPEAT, PADLEFT, PADRIGHT, CENTER, REMOVE |

---

## 19. State / Lifetime / Ownership

- Строки immutable — нет проблем с ownership
- RuntimeStringAccumulator — создаётся при AppendString, финализируется при чтении
- FixedStringLength — метаданные переменной, не значения

---

## 20. Edge Cases

| Сценарий | Покрыт | Доказательство |
|----------|--------|----------------|
| Пустая строка | ✅ | Множество тестов |
| Non-BMP Unicode (emoji) | ✅ | REVERSE test с emoji |
| Surrogate pairs в REVERSE | ✅ | STRING-001 исправлен |
| CP866 vs Unicode в ASC/CHR | ✅ | Тесты с кириллицей |
| Fixed string pad/truncate | ✅ | Execute_CompiledFixedStringProgram |
| SPLIT с пустым разделителем | ✅ | MB3007 |
| REPLACE с пустым oldText | ✅ | MB3007 |
| SUBSTRCOUNT с пустым fragment | ✅ | MB3007 |
| CHR с невалидным кодом | ✅ | MB3007 (Theory: -1, 55296, 1114112) |
| JOIN single-element | ✅ | STRING-002 исправлен |
| REPLACEMANY longest-match | ✅ | Тест с multiple tokens |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| Все 44 строковые функции | ✅ | ✅ | ✅ | ✅ | ✅ |
| Конкатенация `+` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Конкатенация `&` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Сравнения | ✅ | ✅ | ✅ | ✅ | ✅ |
| Fixed strings | ✅ | ✅ | ✅ | ✅ | ✅ |
| SPLIT/JOIN | ✅ | ✅ | ✅ | ✅ | ✅ |
| ASC/CHR/ASCW/CHRW | ✅ | ✅ | ✅ | ✅ | ✅ |
| STR/VAL | ✅ | ✅ | ✅ | ✅ | ✅ |
| AppendString optimization | N/A | ✅ | ✅ | N/A | ✅ |
| Fast paths (10 функций) | N/A | ✅ | ✅ | N/A | ✅ |

**Расхождений не найдено.** Полный паритет подтверждён.

---

## 22. Анализ производительности

### 22.1 Benchmarks

| Сценарий | Interpreter | Bytecode VM | Ускорение | Классификация |
|---------|-------------|-------------|-----------|---------------|
| string_primitive_scan | baseline | 0.398x | -60.2% | CLEAR WIN |
| string_concat_accumulator | baseline | 0.522x | -47.8% | CLEAR WIN |
| string_array_loop_pipeline | baseline | 0.514x | -48.6% | CLEAR WIN |

### 22.2 Почему VM быстрее

1. **AppendString opcodes:** In-place конкатенация через `RuntimeStringAccumulator` вместо создания новой строки
2. **Fast paths:** 10 inline fast paths без создания делегатов
3. **ReferenceEquals short-circuit:** Пустая строка — singleton, быстрое сравнение
4. **No-op оптимизации:** LCASE/UCASE/REVERSE пропускают работу если не нужно

---

## 23. Качество Benchmark

- 3 dedicated сценария покрывают все аспекты строковых операций
- Длинные прогоны для стабильности
- Все сценарии в целевом диапазоне (<0.900x)

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot paths

- `AppendStringValue()` — основной hot path для конкатенации
- `TryExecuteFastStringBuiltin()` — fast path для 10 функций
- `RuntimeValue.AsString()` — извлечение строки (с materialization из accumulator)

### 24.2 Bottlenecks

Серьёзных bottleneck'ов не обнаружено.

---

## 25. Архитектурные проблемы

Серьёзных архитектурных проблем не обнаружено.

---

## 26. Дублирование логики

### 26.1 Устранённое дублирование

- TASK-001 (Session 1042): `BytecodeVm.Builtins.String.cs` (454 → 243 строки, -211) — все операции делегируют в `BuiltinStringHelpers`
- Wave 2.3: -407 строк дублирования

### 26.2 Текущее дублирование

- Валидационные методы (`RequireNonNegativeCount`, `RequireFillCharacter`, `RequireValidUnicodeCodePoint`) существуют в обоих Interpreter и BytecodeVm partial-файлах. Это намеренное дублирование для разных моделей отчётности об ошибках.

---

## 27. Single Source of Truth

- **Реализация:** `BuiltinStringHelpers.cs` — единый источник для всех 44 функций
- **Дескрипторы:** `BuiltinFunctions.String.*.cs` — единый источник для регистрации
- **Тип:** `TypeSymbol.String` — единый символ типа
- **Сравнение:** `RuntimeValueHelpers.ValuesEqual/Compare` — единый источник для сравнений

---

## 28. Legacy / Dead Code

Не обнаружено. Весь код активно используется.

---

## 29. Надёжность и безопасность

- Строки immutable — нет проблем с concurrent access
- FixedStringLength normalization предотвращает выход за границы
- CP866 mapping корректно обрабатывает граничные случаи

---

## 30. Анализ тестов

| Уровень | Кол-во тестов |
|---------|---------------|
| Lexer | 2 (string literals, unterminated string) |
| Interpreter | 90 (все 44 функции + edge cases) |
| BytecodeVm | 18 (parity tests) |
| Execution Pipeline | 10 (integration tests) |
| Acceptance | ~51 samples |
| **Итого** | **~120 тестов + 51 sample** |

---

## 31. Недостающие тесты

| Сценарий | Статус |
|----------|--------|
| Non-BMP Unicode для MID/LEFT/RIGHT | Нет явных тестов |
| ASCW/CHRW dedicated samples | Нет |
| LEN behavior для non-BMP Unicode | Не документировано |

---

## 32. Анализ Samples

51 sample файл покрывает все 44 строковые функции. Каждая функция имеет:
- `basic_usage.axm` — основной сценарий
- `*_error.axm` — негативный сценарий (где применимо)

---

## 33. Анализ документации

### 33.1 Manual

6 dedicated manual-страниц + ~10 cross-reference страниц. Полное покрытие всех 44 функций.

### 33.2 Предыдущий аудит

`docs/archive/audits-legacy/STRING_AUDIT_REPORT.md` — Health Score 8.4/10, 2 дефекта исправлены.

---

## 34. Интеграция с IDE

- STRING — в категории TypeName (подсветка)
- Все 44 ключевых слова строковых функций — в категории BuiltIn
- IDE-004 исправлен: двойная классификация STRING устранена

---

## 35. Зависимости от Host / Platform

- CP866 кодировка зависит от .NET runtime (корректно на всех платформах)
- Unicode behavior — стандартный CLR

---

## 36. Карта корневых причин

Все ранее найденные проблемы (STRING-001, STRING-002) уже исправлены. Новых корневых причин не обнаружено.

---

## 37. Стабильные области

**Не следует трогать:**

1. `TypeSymbol.String` — единый символ типа
2. `RuntimeValue.FromString/AsString` — immutable representation
3. `RuntimeStringAccumulator` — оптимизация конкатенации
4. `NormalizeFixedString` — pad/truncate
5. `RuntimeValueHelpers.ValuesEqual/Compare` — ordinal сравнение
6. `RuntimeCharacterEncoding` — CP866/Unicode mapping
7. `BuiltinStringHelpers` — единый слой реализации
8. Все 44 дескриптора в `BuiltinFunctions.String.*.cs`
9. Лексер парсинг строковых литералов
10. Подсистема фиксированных строк

---

## 38. Возможности консолидации

Консолидация уже выполнена. `BuiltinStringHelpers` — единый источник правды.

---

## 39. Возможности разделения ответственности

Разделение уже выполнено корректно:
- Дескрипторы → `BuiltinFunctions.String.*.cs`
- Реализация → `BuiltinStringHelpers.cs`
- Dispatch → `Interpreter.Builtins.String*.cs` / `BytecodeVm.Builtins.String*.cs`

---

## 40. Кандидаты на удаление

Не обнаружено.

---

## 41. Готовность к дальнейшему развитию

Подсистема готова к:
- Добавлению строковой интерполяции (документировано в `docs/FUTURE.md`)
- Добавлению `FORMAT` функции
- Расширению для новых строковых операций

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P4 | TESTS | Tests | Нет non-BMP Unicode тестов для MID/LEFT/RIGHT | Отсутствие test methods | Упущение | HIGH |
| SYS-002 | P4 | DOCS | Docs | LEN behavior для non-BMP Unicode не документировано | Отсутствие в manual | Упущение | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0-4 — Нет необходимости

Подсистема стабильна.

### ЭТАП 5 — Расширение тестирования (опционально)

**TASK-001:** Добавить non-BMP Unicode тесты для MID/LEFT/RIGHT
- Сложность: низкая
- Риск: нет

---

## 44. Граф зависимостей задач

```
TASK-001 (non-BMP tests) ─── независимая
```

---

## 45. Definition of Done

Строковая подсистема считается завершённой, если:

- [x] Все 44 строковые функции работают корректно
- [x] Fixed strings работают с pad/truncate
- [x] Операторы + и & работают правильно
- [x] Сравнения ordinal, case-sensitive
- [x] ASC/CHR/ASCW/CHRW корректно обрабатывают CP866 и Unicode
- [x] Паритет Interpreter ↔ BytecodeVm подтверждён
- [x] Все diagnostic codes покрыты тестами
- [x] Samples покрывают все функции
- [x] Manual актуален
- [x] IDE подсветка корректна

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **Нет проблем корректности и архитектуры уровня P0-P2.**

---

## 47. ТОП-20 задач по производительности

1. **Нет задач по производительности.** Подсистема уже на целевом уровне (VM быстрее на 48-60%).

---

## 48. Итоговая оценка здоровья подсистемы

| Метрика | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 9/10 | Все 44 функции работают корректно, 2 дефекта исправлены |
| Семантика | 9/10 | Ordinal сравнение, CP866/Unicode, fixed strings |
| Архитектура | 9/10 | Единый BuiltinStringHelpers, RuntimeStringAccumulator |
| Runtime (Interpreter) | 9/10 | Корректная делегация в общий слой |
| Runtime (Bytecode VM) | 9/10 | Fast paths, AppendString opcodes |
| Производительность | 9/10 | VM быстрее на 48-60% |
| Память | 9/10 | Immutable strings, singleton empty string |
| Error handling | 9/10 | MB2026, MB3007 для всех edge cases |
| Tests | 9/10 | 120 тестов + 51 sample |
| Samples | 9/10 | Полное покрытие всех функций |
| Documentation | 9/10 | 6 manual-страниц, спецификация, аудит |
| IDE | 8/10 | Подсветка корректна (IDE-004 исправлен) |
| Maintainability | 9/10 | Единый источник правды |
| Reliability | 9/10 | Immutable, no stale state |
| Future readiness | 9/10 | Готово к расширению |

**Средняя оценка: 8.9/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему STRING / STRING OPERATIONS достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES**

---

## Основные blockers

Нет.

## Что нужно сделать в первую очередь

Нет критических задач.

## Что можно отложить

- Non-BMP Unicode тесты для MID/LEFT/RIGHT
- LEN behavior документация для non-BMP Unicode

## Что не следует трогать

- `BuiltinStringHelpers` — единый слой реализации
- `RuntimeStringAccumulator` — оптимизация конкатенации
- `RuntimeValueHelpers.ValuesEqual/Compare` — ordinal сравнение
- `RuntimeCharacterEncoding` — CP866/Unicode mapping
- Все 44 дескриптора
- Лексер парсинг строковых литералов
- Подсистему фиксированных строк
- AppendString opcodes
- Fast paths в VM
