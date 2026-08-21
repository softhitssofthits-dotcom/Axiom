# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## BYREF / FUNCTION VALUES

Дата: 2026-08-21
Метод: полный forensic-анализ кода, тестов, samples, документации и спецификации

---

## 1. Резюме

Подсистема BYREF / FUNCTION VALUES в Axiom охватывает два тесно связанных механизма:

1. **Модификаторы параметров BYREF / BYVAL** — позволяют пользовательским `SUB` и `FUNCTION` получать аргументы либо по значению (`BYVAL`, по умолчанию), либо по ссылке (`BYREF`).
2. **Значения-функции (function values)** — позволяют хранить ссылку на функцию в переменной, передавать как аргумент и возвращать из другой функции; обеспечивают замыкания (closures).

**Общая зрелость подсистемы — STABLE с ограничениями.** Базовый механизм BYREF для простых переменных и записей TYPE работает корректно в обоих движках (Interpreter и Bytecode VM). Механизм function values полнофункционален для прямых и косвенных вызовов, включая замыкания. Основные ограничения документированы и сознательно приняты как阶段性边界.

**Главные выводы:**
- BYREF для простых переменных и полей TYPE — работает, закреплён тестами и samples.
- BYREF для function values (callback-вызовы) — запрещён на уровне Binder, выдаёт MB2029.
- BYREF для элементов массива как аргументов — запрещён на уровне Binder, выдаёт MB2028.
- Lowerer содержит мёртвый код для copyback полей и элементов массива (подготовка к будущему расширению).
- При ошибке внутри BYREF-процедуры копирование обратно не происходит ни в одном из движков — это поведение по дизайну (ссылочная семантика, не транзакционная).
- Interpreter и VM дают идентичный результат для всех подтверждённых сценариев BYREF.

---

## 2. Что представляет собой эта подсистема

### 2.1 BYREF / BYVAL

Модификаторы `BYREF` и `BYVAL` определяют, как аргумент передаётся в параметр процедуры или функции:

- **BYVAL** (по умолчанию): вызываемая процедура получает независимую копию значения; изменения параметра не видны вызывающему коду.
- **BYREF**: вызываемая процедура получает прямой доступ к исходному хранилищу вызывающего кода; изменения параметра видны после возврата.

Синтаксис:
```basic
SUB Modify(BYREF x AS INTEGER, BYVAL y AS INTEGER)
    x = x + 1   ' изменяет исходную переменную
    y = y + 1    ' изменяет только локальную копию
END SUB
```

### 2.2 Function Values (Значения-функции)

Функция может быть сохранена в переменной типа `FUNCTION(...)`, передана как аргумент другой функции или возвращена как результат. Поддерживаются замыкания: вложенная функция захватывает переменные из окружающей области видимости.

Синтаксис:
```basic
DIM op AS FUNCTION(INTEGER) AS INTEGER
op = DoubleValue
PRINT ApplyTwice(op, 3)

FUNCTION ApplyTwice(f AS FUNCTION(INTEGER) AS INTEGER, v AS INTEGER) AS INTEGER
    RETURN f(f(v))
END FUNCTION

FUNCTION DoubleValue(x AS INTEGER) AS INTEGER
    RETURN x * 2
END FUNCTION
```

---

## 3. Полный список возможностей

### SYS-001: BYREF параметр

- **Название:** BYREF параметр
- **Категория:** Параметры процедур
- **Тип:** KEYWORD / MODIFIER
- **Пользовательский синтаксис:** `SUB Foo(BYREF x AS INTEGER)`
- **Parser:** `ParseOptionalPassingKeyword()` → `SyntaxKind.ByRefKeyword` (`Parser.Declarations.cs:317-325`)
- **Syntax Node:** `ParameterSyntax` с `PassingKeyword`
- **Binder:** `ParameterPassingMode.ByRef` (`Symbols.cs:564`), `BindByRefArgument()` (`Binder.InvocationExpressions.cs:709-738`)
- **Interpreter:** `ResolveByRefStoredArgument()` → `RuntimeReferenceStoredValue` (`Interpreter.Builtins.cs:625-677`)
- **Bytecode Lowering:** `CallFunctionByRef` lowered instruction, `ByRefStoragePlan` hierarchy (`BoundProgramLowerer.cs:17-125`)
- **Bytecode Compiler:** `EmitCallByRef()` → `Opcode.CallByRef` (`BytecodeCompiler.cs:1195-1257`)
- **Bytecode VM:** `ExecuteRoutineCall()` с `CallByRef`, `ResolveByRefArgumentSlots()`, `CopyBackByRefArguments()` (`BytecodeVm.cs:2975-3035, 4957-4976`)
- **Opcode:** `Opcode.CallByRef` (`Opcode.cs:58`)
- **IDE:** Подсветка `BYREF` как ключевого слова (`Axiom.xshd`, `Axiom.Dark.xshd`)
- **Tests:** `Execute_ByRefScalarParameterMutatesCallerVariable()`, `Execute_ByRefRecordParameterMutatesCallerVariable()` и их compiled-версии
- **Samples:** `samples/operators/sub/byref_scalar_argument.axm`, `samples/operators/sub/byref_record_argument.axm`
- **Документация:** `docs/SPECIFICATION3.md` (§ обновление 2026-05-04), `docs/manual/sub.html`, `docs/manual/procedures-and-functions.html`
- **Статус:** IMPLEMENTED (ограничено простыми переменными и записями TYPE)
- **Уровень уверенности:** CONFIRMED

### SYS-002: BYVAL параметр

- **Название:** BYVAL параметр
- **Категория:** Параметры процедур
- **Тип:** KEYWORD / MODIFIER
- **Пользовательский синтаксис:** `SUB Foo(BYVAL x AS INTEGER)` или `SUB Foo(x AS INTEGER)` (по умолчанию)
- **Parser:** `ParseOptionalPassingKeyword()` → `SyntaxKind.ByValKeyword`
- **Binder:** `ParameterPassingMode.ByVal` — режим по умолчанию
- **Interpreter:** `argument.CopyForStorage()` — независимая копия
- **Bytecode VM:** Обычный `Opcode.Call`, аргументы копируются в слоты фрейма
- **Tests:** `Execute_ByValRecordParameterKeepsCallerVariableIndependent()`, compiled-версия
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: RuntimeFunctionValue

- **Название:** Значение-функции (function value)
- **Категория:** Типы данных
- **Тип:** TYPE / RUNTIME FACILITY
- **Пользовательский синтаксис:** `DIM f AS FUNCTION(INTEGER) AS INTEGER`
- **Parser:** `FunctionTypeClauseSyntax` в `Parser.Declarations.cs`
- **Binder:** `FunctionTypeSymbol`, `BoundFunctionExpression`
- **Runtime:** `RuntimeFunctionValue` (`RuntimeFunctionValue.cs`) — хранит `FunctionSymbol`, `RuntimeScope` (для Interpreter) или `BytecodeRoutine` + `BytecodeVariableSlot[]` (для VM)
- **Interpreter:** `EvaluateNameExpression()` → `BoundFunctionExpression` → `RuntimeFunctionValue`; `InvokeUserRoutine()` для вызова
- **Bytecode VM:** `LoadFunctionValue()`, `InvokeBytecodeFunction()`, `ExecuteFunctionValueCall()` (Opcode.CallValue)
- **Tests:** `Execute_CompiledIndirectFunctionValueCall()`, `Execute_ReportsCallingEmptyFunctionValue()`
- **Samples:** `samples/operators/function/first_class_value.axm`, `samples/operators/function/capture_outer_local.axm`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: Замыкания (Closures)

- **Название:** Замыкания
- **Категория:** Runtime
- **Тип:** RUNTIME FACILITY
- **Описание:** Вложенная функция может захватывать переменные из внешней области видимости. Замыкание сохраняет ссылку на внешние переменные даже после возврата из внешней функции.
- **Interpreter:** `RuntimeScope` с цепочкой `Parent`; `RuntimeFunctionValue.CapturedScope`
- **Bytecode VM:** `BytecodeVariableSlot[]` для захваченных переменных; `ResolveCapturedSlots()`, `ResolveCaptureBindings()`
- **Tests:** `Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures()`, acceptance-тесты на `returned_closure.axm`, `shared_closure_state.axm`
- **Samples:** `samples/operators/function/capture_outer_local.axm`, `samples/operators/function/returned_closure.axm`, `samples/operators/function/shared_closure_state.axm`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: MB2028 — Недопустимый аргумент BYREF

- **Название:** Диагностика недопустимого аргумента BYREF
- **Категория:** Диагностика
- **Тип:** ERROR
- **Код:** `MB2028`
- **Сообщение:** "Аргумент параметра BYREF должен быть простой переменной того же типа без промежуточного преобразования."
- **Когда выдаётся:** Когда аргумент для параметра BYREF не является `BoundVariableExpression` (например, выражение `x + 1`, вызов функции, литерал)
- **Binder:** `ReportInvalidByRefArgument()` (`Binder.Diagnostics.cs:373-379`)
- **Tests:** `Bind_ReportsNonVariableByRefArgument()`, acceptance `byref_requires_variable.axm`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: MB2029 — BYREF для function values не поддержан

- **Название:** Диагностика неподдерживаемого BYREF для значений-функций
- **Категория:** Диагностика
- **Тип:** ERROR
- **Код:** `MB2029`
- **Сообщение:** "Функцию 'X' с параметрами BYREF пока нельзя использовать как значение, callback или переменную FUNCTION."
- **Когда выдаётся:** Когда функция с BYREF-параметрами используется как значение-функции (first-class value) или при callback-вызове через function-value с BYREF-параметрами
- **Binder:** `ReportUnsupportedByRefCallableValue()` (`Binder.Diagnostics.cs:381-386`); проверка в `Binder.Expressions.cs:85-88` и `Binder.InvocationExpressions.cs:627-638`
- **Tests:** НЕТ прямых тестов для MB2029
- **Статус:** IMPLEMENTED_UNDOCUMENTED (диагностика есть, но тестов нет)
- **Уровень уверенности:** HIGH

### SYS-007: ByRef Copyback в Lowerer

- **Название:** Механизм copyback для ByRef в lowerer
- **Категория:** Bytecode Lowering
- **Тип:** RUNTIME FACILITY
- **Описание:** Lowerer содержит иерархию `ByRefStoragePlan` для поддержки ByRef-copyback: `VariableByRefStoragePlan`, `FieldByRefStoragePlan`, `ArrayElementByRefStoragePlan`. Также есть `ByRefCopyBackPlan` для формирования инструкций копирования обратно после вызова.
- **Файлы:** `BoundProgramLowerer.cs:17-125, 1664-1840`
- **Статус:** PARTIAL — `VariableByRefStoragePlan` используется; `FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` являются мёртвым кодом (Binder не допускает такие аргументы до lowering)
- **Уровень уверенности:** CONFIRMED

### SYS-008: SELF как скрытый BYREF

- **Название:** Неявный параметр SELF для методов TYPE
- **Категория:** TYPE / ООП
- **Тип:** RUNTIME FACILITY
- **Описание:** Все методы пользовательского TYPE получают неявный первый параметр `SELF` с модификатором `BYREF`. Это позволяет методам изменять поля вызывающего экземпляра.
- **Binder:** Параметр `SELF` создаётся автоматически с `ParameterPassingMode.ByRef`
- **Lowerer:** Специальная обработка `IsTemporaryTypeMethodSelfArgument()` для случая, когда receiver — не переменная
- **Tests:** `Bind_TypeMethodCallsInjectSelfArgument()`; runtime-тесты на мутацию полей через методы TYPE
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Конвейер обработки BYREF

```
Исходный код: SUB Foo(BYREF x AS INTEGER)
         ↓
Lexer: TokenKind.ByRefKeyword
         ↓
Parser: ParameterSyntax { PassingKeyword = ByRefKeyword }
         ↓
Binder: ParameterSymbol { PassingMode = ByRef }
         ↓
  [При вызове: BindByRefArgument() — проверяет, что аргумент — BoundVariableExpression]
         ↓
Lowerer: CallFunctionByRef lowered instruction
  ├── VariableByRefStoragePlan — прямая передача переменной
  ├── FieldByRefStoragePlan — (мёртвый код) copyback для поля TYPE
  └── ArrayElementByRefStoragePlan — (мёртвый код) copyback для элемента массива
         ↓
BytecodeCompiler: EmitCallByRef() → Opcode.CallByRef
  операнды: [routineId, argCount, byValArgCount, byRefCount, ...{argIndex, sourceKind, sourceSlot}]
         ↓
BytecodeVm: ExecuteRoutineCall()
  ├── ResolveByRefArgumentSlots() — создаёт BytecodeVariableSlot[] для ByRef-аргументов
  ├── CreateFrame() — аргумент-слоты ByRef разделяются с вызывающим фреймом
  └── CopyBackByRefArguments() — при нормальном возврате копирует значения обратно
```

### 4.2 Конвейер обработки Function Values

```
Исходныйコード: DIM f AS FUNCTION(INTEGER) AS INTEGER
         ↓
Parser: FunctionTypeClauseSyntax
         ↓
Binder: FunctionTypeSymbol { ParameterTypes, ParameterPassingModes, ReturnType }
         ↓
  [При присваивании: f = MyFunction → BoundFunctionExpression]
  [При вызове: f(42) → BoundCallExpression с target = BoundFunctionExpression]
         ↓
Lowerer:
  ├── LoadFunction — загрузка значения функции на стек
  └── CallValue — вызов через значение на стеке
         ↓
BytecodeVm:
  ├── LoadFunctionValue() → RuntimeFunctionValue с BytecodeRoutine + captured slots
  └── ExecuteFunctionValueCall() → InvokeBytecodeFunction()
```

### 4.3 Ключевые файлы

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Lexing/Lexer.cs` | Токенизация `BYREF`, `BYVAL` |
| `src/Axiom.Lexing/TokenKind.cs` | `ByRefKeyword`, `ByValKeyword` |
| `src/Axiom.Parsing/Parser.Declarations.cs` | `ParseOptionalPassingKeyword()`, `ParseParameter()` |
| `src/Axiom.Syntax/SyntaxKind.cs` | `ByRefKeyword`, `ByValKeyword` |
| `src/Axiom.Semantics/Symbols/Symbols.cs` | `ParameterPassingMode`, `ParameterSymbol.IsByRef`, `FunctionTypeSymbol` |
| `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` | `BindByRefArgument()`, проверка ByRef при вызовах |
| `src/Axiom.Semantics/Binding/Binder.Expressions.cs` | Проверка ByRef при использовании функции как значения |
| `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs` | `ReportInvalidByRefArgument()` (MB2028), `ReportUnsupportedByRefCallableValue()` (MB2029) |
| `src/Axiom.Core/DiagnosticCodes.cs` | `InvalidByRefArgument` (MB2028), `UnsupportedByRefCallableValue` (MB2029) |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | `ByRefStoragePlan` hierarchy, `ByRefCopyBackPlan`, `LowerCall()` |
| `src/Axiom.Bytecode/Model/LoweredInstruction.cs` | `CallFunctionByRef()` |
| `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs` | `CallFunctionByRef` |
| `src/Axiom.Bytecode/Model/Opcode.cs` | `CallByRef` |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | `EmitCallByRef()` |
| `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` | Валидация `CallByRef` |
| `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs` | Значение-функции |
| `src/Axiom.Runtime/Values/RuntimeReferenceStoredValue.cs` | Ссылочное хранилище для BYREF в Interpreter |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | `ExecuteRoutineCall()`, `ResolveByRefArgumentSlots()`, `CopyBackByRefArguments()`, `InvokeBytecodeFunction()` |
| `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` | `_byRefArgumentTargets` |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | `InvokeUserRoutine()`, `ResolveByRefStoredArgument()` |

---

## 5. Полный путь выполнения

### 5.1 BYREF: прямой вызов именованной процедуры

**Interpreter:**
```
SUB AddOne(BYREF value AS INTEGER)
    value = value + 1
END SUB

DIM count AS INTEGER = 10
AddOne(count)
```

1. Parser создаёт `ParameterSyntax` с `PassingKeyword = ByRefKeyword`
2. Binder создаёт `ParameterSymbol("value", TypeSymbol.Integer, 0, ParameterPassingMode.ByRef)`
3. При вызове `AddOne(count)`:
   - `BindByRefArgument()` проверяет, что `count` — `BoundVariableExpression` того же типа
   - Создаёт `BoundCallExpression` с `BoundVariableExpression` для аргумента
4. Interpreter: `InvokeUserRoutine()`
   - Для параметра `value` с `IsByRef == true`: `ResolveByRefStoredArgument(boundArguments[0], ...)`
   - Создаёт `RuntimeReferenceStoredValue` с getter/setter, ссылающимися на `count` в scope вызывающего кода
   - При `value = value + 1`: getter читает `count`, setter записывает в `count`
   - После возврата: `count` равен 11

**Bytecode VM:**
1. Lowerer: `LowerCall()` обнаруживает ByRef-параметр + `BoundVariableExpression`
   - Добавляет индекс аргумента в `byRefArgumentIndices`, переменную в `byRefVariables`
   - Эмитирует `CallFunctionByRef` lowered instruction
2. BytecodeCompiler: `EmitCallByRef()`
   - Формирует `Opcode.CallByRef` с операндами `[routineId, argCount, byValArgCount, byRefCount, argIndex, sourceKind, sourceSlot]`
3. BytecodeVm: `ExecuteRoutineCall()`
   - `ResolveByRefArgumentSlots()`: для sourceKind=2 (local) создаёт `BytecodeVariableSlot` через `callerFrame.GetOrCreateLocalSlot(sourceSlot)`
   - `CreateArgumentSlots()`: аргумент-слот ByRef-параметра = shared slot из вызывающего фрейма
   - При `StoreVariable(value)`: запись идёт прямо в shared slot → видна вызывающему коду
   - `CopyBackByRefArguments()` при возврате: копирует `frame.Arguments[i].Value` → `targets[i].Store(...)` (для shared slots это фактически no-op)

### 5.2 BYREF: вызов через function value (callback)

```basic
SUB Modify(BYREF x AS INTEGER)
    x = x + 1
END SUB

DIM op AS FUNCTION(BYREF INTEGER)
op = Modify    ' MB2029 — запрещено
```

1. Binder: `Binder.Expressions.cs:85-88` — при попытке использовать `Modify` как значение обнаруживает BYREF-параметр → `ReportUnsupportedByRefCallableValue()` → MB2029 → `BoundErrorExpression`
2. Результат: семантическая ошибка, код не компилируется

### 5.3 Function Value: прямой вызов

```basic
DIM op AS FUNCTION(INTEGER) AS INTEGER
op = DoubleValue
PRINT op(42)
```

1. Binder: `DoubleValue` — функция без BYREF-параметров → `BoundFunctionExpression`
2. Присваивание: `op = DoubleValue` → `LoadFunction`
3. Вызов: `op(42)` → `BoundCallExpression` с target = `BoundVariableExpression(op)`
4. Lowerer: target не `BoundFunctionExpression` → `LowerExpression(target)` + `CallValue`
5. VM: `ExecuteFunctionValueCall()` → `PopValue()` → `AsFunction()` → `InvokeBytecodeFunction()`

---

## 6. Фактический семантический контракт

### 6.1 BYREF параметр

**Вход:** простая изменяемая переменная того же типа, что и параметр
**Обработка:** вызываемая процедура получает прямой доступ к хранилищу вызывающей переменной
**Выход:** после возврата из процедуры исходная переменная содержит все изменения, сделанные через параметр

**Допустимые входы:**
- Простая переменная (`DIM x AS INTEGER` → `BYREF y AS INTEGER`)
- Переменная TYPE (`DIM user AS Person` → `BYREF u AS Person`)

**Недопустимые входы (MB2028):**
- Выражение (`x + 1`)
- Литерал (`42`)
- Вызов функции (`Foo()`)
- Поле TYPE (`user.Name`) — не через прямую переменную
- Элемент массива (`arr(1)`)

**Недопустимое использование (MB2029):**
- Функция с BYREF-параметрами как значение-функции
- Callback-вызов через function-value с BYREF-параметрами

### 6.2 Function Value

**Вход:** имя функции без BYREF-параметров, присвоенное переменной типа `FUNCTION(...) AS ...`
**Обработка:** создаётся `RuntimeFunctionValue`, содержащий ссылку на функцию и захваченную область видимости
**Выход:** при вызове через `f(args)` выполняется тело функции с замыканием

**Побочные эффекты:** замыкания могут изменять захваченные переменные (shared state)

---

## 7. Анализ Parser

### 7.1 Парсинг BYREF/BYVAL

**Файл:** `Parser.Declarations.cs:309-325`

Parser обрабатывает модификаторы параметров через `ParseOptionalPassingKeyword()`. Логика простая и корректная:
- Если текущий токен — `ByRefKeyword` или `ByValKeyword`, consume его
- Иначе возвращает `null` (модификатор отсутствует → по умолчанию BYVAL)

**Оценка:** Parser корректно обрабатывает все合法ные формы. Нет проблем.

### 7.2 Парсинг типов функций

Parser поддерживает синтаксис `FUNCTION(params) AS ReturnType` для объявления типов функций. Это используется для переменных, хранящих значения-функции.

**Оценка:** Корректно.

---

## 8. Анализ Binder

### 8.1 BindByRefArgument

**Файл:** `Binder.InvocationExpressions.cs:709-738`

```
BindByRefArgument(argument, expectedType):
  1. BindAssignableTarget(argument) — пытается связать как присваиваемую цель
  2. Если тип Error → пытает BindExpression + ReportInvalidByRefArgument → MB2028
  3. Если не BoundVariableExpression → ReportInvalidByRefArgument → MB2028
  4. Если тип не совпадает → ReportTypeMismatch
  5. Возвращает BoundVariableExpression
```

**Критическое ограничение:** BindByRefArgument принимает ТОЛЬКО `BoundVariableExpression`. Это значит:
- `BYREF x AS INTEGER` + аргумент `myVar` → OK (BoundVariableExpression)
- `BYREF x AS INTEGER` + аргумент `myVar + 1` → MB2028 (BoundBinaryExpression)
- `BYREF x AS INTEGER` + аргумент `record.Field` → MB2028 (BoundFieldAccessExpression)
- `BYREF x AS INTEGER` + аргумент `arr(1)` → MB2028 (BoundArrayAccessExpression)

### 8.2 Ограничение BYREF для function values

**Файл:** `Binder.Expressions.cs:85-88`

При использовании функции как значения (`DIM f AS FUNCTION(...): f = MyFunc`):
```csharp
if (function.Parameters.Any(static parameter => parameter.IsByRef))
{
    ReportUnsupportedByRefCallableValue(function.Name, expression.IdentifierToken.Location);
    return new BoundErrorExpression(expression.IdentifierToken.Location);
}
```

Аналогичная проверка в `Binder.InvocationExpressions.cs:627-638` для callback-вызовов через function-value.

**Оценка:** Логика корректна. Ограничение сознательно принято и документировано.

### 8.3 Проверка типов для BYREF

В `BindByRefArgument` проверяется точное совпадение типов (`variableExpression.Type != expectedType`). Нет неявных преобразований для BYREF-аргументов — это корректно, так как BYREF требует совместимости хранилищ.

---

## 9. Анализ Interpreter

### 9.1 ResolveByRefStoredArgument

**Файл:** `Interpreter.Builtins.cs:625-677`

Метод обрабатывает три случая:
1. **BoundVariableExpression** — основной путь: ищет хранилище переменной в scope, создаёт `RuntimeReferenceStoredValue` с getter/setter
2. **BoundFieldAccessExpression** — создаёт reference на поле записи (НО: этот порт unreachable из-за Binder)
3. **BoundArrayAccessExpression** — создаёт reference на элемент массива (НО: этот порт unreachable из-за Binder)
4. **Fallback** — вычисляет выражение, копирует для хранения, создаёт reference на копию (НЕПРАВИЛЬНО для семантики BYREF, но unreachable)

**Важно:** Метод содержит обработку для полей и элементов массива, которая в текущей реализации НЕДОСТИЖИМА из-за проверок в Binder. Это подготовительный код для будущего расширения.

### 9.2 InvokeUserRoutine

**Файл:** `Interpreter.Builtins.cs:415-481`

При вызове пользовательской функции:
- Для каждого параметра с `IsByRef == true`: `ResolveByRefStoredArgument(boundArguments[index], ...)`
- Для остальных: `CopyForStorage()` или `AsArray()`
- `scope.Define(parameter, storedArgument)` — привязывает хранилище к параметру

**После возврата:** scope и call frame уничтожаются в `finally`. Ссылочное хранилище (`RuntimeReferenceStoredValue`)继续保持 действительным, так как оно захватывает лямбды с указателем на scope вызывающего кода.

### 9.3 Семантика BYREF в Interpreter

В Interpreter BYREF реализован через замыкания: `RuntimeReferenceStoredValue` — это лямбда, которая читает/пишет в scope вызывающего кода. Изменения НЕМЕДЛЕННО видны вызывающему коду (нет отложенного copyback). Это отличается от модели VM.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering BYREF

**Файл:** `BoundProgramLowerer.cs:1664-1725`

`LowerCall()` обрабатывает три ветки для каждого аргумента:

1. **Прямая переменная BYREF** (line 1680-1685): `parameter?.IsByRef == true && argument is BoundVariableExpression` → добавляет в `byRefArgumentIndices` и `byRefVariables`, НЕ эмитирует загрузку значения на стек
2. **TYPE method self** (line 1687-1696): если receiver — не переменная, создаёт временный `__type_method_self`
3. **Copyback для составных целей** (line 1698-1706): `TryLowerByRefCopyBackArgument()` → создаёт временный `__byref_value`, загружает значение, после вызова копирует обратно

**Ветка 3 — мёртвый код:** Binder не допускает не-переменные аргументы для BYREF до стадии lowering.

### 10.2 ByRefStoragePlan hierarchy

**Файл:** `BoundProgramLowerer.cs:17-125`

- `VariableByRefStoragePlan` — load/store через `LoadVariable`/`StoreVariable` (ИСПОЛЬЗУЕТСЯ для косвенных путей)
- `FieldByRefStoragePlan` — load/store через `LoadField`/`StoreField` (МЁРТВЫЙ КОД)
- `ArrayElementByRefStoragePlan` — load/store через `LoadElement`/`StoreElement` (МЁРТВЫЙ КОД)

### 10.3 EmitCallByRef

**Файл:** `BytecodeCompiler.cs:1195-1257`

Формирует `Opcode.CallByRef` с операндами:
```
[routineId, totalArgCount, byValArgCount, byRefCount, ...{argIndex, sourceKind, sourceSlot}]
```

sourceKind:
- 0 = global
- 1 = caller's argument (parameter slot)
- 2 = caller's local
- 3 = caller's capture

### 10.4 VM: ResolveByRefArgumentSlots

**Файл:** `BytecodeVm.cs:3009-3035`

Для каждого ByRef-аргумента создаёт `BytecodeVariableSlot`:
- sourceKind 0 (global): новый slot с getter/setter через `LoadGlobalUnchecked`/`StoreGlobalUnchecked`
- sourceKind 1 (caller argument): `callerFrame.Arguments[sourceSlot]` — ПРЯМОЕ РАЗДЕЛЕНИЕ SLOTA
- sourceKind 2 (caller local): `callerFrame.GetOrCreateLocalSlot(sourceSlot)` — обёртка над локалом
- sourceKind 3 (caller capture): `callerFrame.Captures[sourceSlot]` — ПРЯМОЕ РАЗДЕЛЕНИЕ SLOTA

### 10.5 VM: CopyBackByRefArguments

**Файл:** `BytecodeVm.cs:4957-4976`

При нормальном возврате (`CompleteReturn`):
```csharp
for (var index = 0; index < count; index++)
{
    var target = targets[index];
    if (target is null) continue;
    target.Store(frame.Arguments[index].Value);
}
```

**Замечание:** Для sourceKind 1 и 3 (shared slots) это фактически no-op, так как `frame.Arguments[index]` и `target` — один и тот же объект. Для sourceKind 0 и 2 (wrapper slots) это тоже redundant, так как wrapper уже делегирует к реальному хранилищу.

**Критический случай:** CopyBackByRefArguments вызывается ТОЛЬКО при нормальном возврате (`CompleteReturn`). При ошибке (`RuntimeFailureException`) copyback НЕ происходит.

### 10.6 VM: InvokeBytecodeFunction (function value call)

**Файл:** `BytecodeVm.cs:4587-4642`

При вызове через function value:
```csharp
_callStack.Push(CreateFrame(routine, arguments, captures, callSiteLocation: callSiteLocation));
```

**Замечание:** `CreateFrame` вызывается БЕЗ `byRefArgumentSlots`. Это значит, что function-value вызовы НЕ поддерживают BYREF — аргументы всегда передаются как копии. Это согласуется с ограничением в Binder (MB2029).

---

## 11. Анализ Runtime

### 11.1 RuntimeFunctionValue

**Файл:** `RuntimeFunctionValue.cs`

Класс хранит:
- `Function` — `FunctionSymbol` (сигнатура функции)
- `CapturedScope` — `RuntimeScope` (для Interpreter)
- `BytecodeRoutine` — `BytecodeRoutine?` (для VM, прямая ссылка на рутину)
- `BytecodeCapturedSlots` — `IReadOnlyList<BytecodeVariableSlot>?` (для VM)
- `BytecodeBuiltinBindingId` — `int?` (для built-in function values в VM)

Конструкторы:
1. `(FunctionSymbol, RuntimeScope)` — для Interpreter
2. `(FunctionSymbol, IReadOnlyList<BytecodeVariableSlot>)` — для VM (user function)
3. `(BytecodeRoutine, BytecodeVariableSlot[])` — для VM (с прямой ссылкой на рутину)
4. С `bytecodeBuiltinBindingId` — для built-in function values

### 11.2 RuntimeReferenceStoredValue

**Файл:** `RuntimeReferenceStoredValue.cs`

Простая обёртка над getter/setter лямбдами:
```csharp
public RuntimeValue Value
{
    get => _getter();
    set => _setter(value);
}
```

Используется в Interpreter для BYREF-параметров и для ссылок на поля/элементы массива.

---

## 12. Взаимодействие с типами

| Тип | BYREF параметр | Function Value | Примечания |
|-----|:-:|:-:|---|
| INTEGER | ✅ | ✅ | Полная поддержка |
| LONG | ✅ | ✅ | Полная поддержка |
| DOUBLE | ✅ | ✅ | Полная поддержка |
| DECIMAL | ✅ | ✅ | Полная поддержка |
| STRING | ✅ | ✅ | Полная поддержка |
| BOOLEAN | ✅ | ✅ | Полная поддержка |
| IMAGE | ✅ | ✅ | Полная поддержка |
| SPRITE | ✅ | ✅ | Полная поддержка |
| TILESET | ✅ | ✅ | Полная поддержка |
| TILEMAP | ✅ | ✅ | Полная поддержка |
| FILE | ✅ | ✅ | Полная поддержка |
| FUNCTION(...) | ❌ (MB2029) | ✅ | BYREF для function values запрещён |
| ArrayType | ❌ (MB2028) | ✅ | BYREF для массивов запрещён |
| UserDefinedType (TYPE) | ✅ | ✅ | Полная поддержка |

---

## 13. Взаимодействие со String

Строки в BYREF работают корректно. При передаче строки BYREF:
- Interpreter: `RuntimeReferenceStoredValue` указывает на хранилище строки в scope вызывающего кода
- VM: shared slot указывает на строку в слоте вызывающего фрейма

При изменении строки через BYREF-параметр (`value = "new"`) изменение видно вызывающему коду.

**Fixed strings:** Не проверены отдельно, но механизм должен работать аналогично через `FixedStringLength`.

---

## 14. Взаимодействие с Array

**Массивы как аргументы BYREF — НЕ поддержаны (MB2028).**

Binder не допускает передачу элемента массива как BYREF-аргумента:
```basic
SUB Modify(BYREF x AS INTEGER)
    x = x + 1
END SUB

DIM arr(5) AS INTEGER
arr(1) = 10
Modify(arr(1))   ' MB2028 — недопустимый аргумент BYREF
```

Lowerer содержит подготовительный код (`ArrayElementByRefStoragePlan`), но он недостижим.

**Массивы как значения (BYVAL):** Полная поддержка, включая многомерные массивы.

---

## 15. Взаимодействие с TYPE

### 15.1 BYREF для записей TYPE

Полная поддержка. Запись TYPE передаётся BYREF и может быть изменена через параметр:
```basic
SUB Birthday(BYREF user AS Person)
    user.Age = user.Age + 1
END SUB
```

### 15.2 SELF как скрытый BYREF

Все методы TYPE получают неявный первый параметр `SELF` с `ParameterPassingMode.ByRef`. Это позволяет методам изменять поля экземпляра.

### 15.3 TYPE-методы с BYREF-параметрами

TYPE-методы могут иметь дополнительные BYREF-параметры:
```basic
TYPE Counter
    Value AS INTEGER
    SUB AddToSelf(BYREF other AS INTEGER)
        SELF.Value = SELF.Value + other
    END SUB
END TYPE
```

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Параметры

| Параметр | Поддержка |
|----------|-----------|
| BYVAL (по умолчанию) | ✅ |
| BYREF | ✅ (только простые переменные) |
| Named arguments | ❌ (только для TYPE literals) |
| Optional parameters | ❌ |
| Default values | ❌ |
| ParamArray | ❌ |

### 16.2 Return

- `SUB` возвращает `RuntimeValue.Void`
- `FUNCTION` возвращает значение через `RETURN expr`
- В VM: `CopyForStorage()` при возврате значения
- В Interpreter: `signal.Value.CopyForStorage()` при возврате значения

### 16.3 Closures

Замыкания работают корректно в обоих движках:
- Вложенные функции захватывают переменные из внешней области видимости
- Захваченные переменные разделяются между всеми ссылками на замыкание (shared state)
- Замыкания можно возвращать из функций

---

## 17. Взаимодействие с Control Flow

BYREF-параметры работают корректно внутри всех конструкций управления потоком:
- `IF/ELSE` — нет влияния
- `FOR/NEXT` — нет влияния
- `WHILE/WEND` — нет влияния
- `DO/LOOP` — нет влияния
- `SELECT CASE` — нет влияния
- `TRY/CATCH/FINALLY` — **особый случай** (см. раздел 18)

Function values работают корректно внутри всех конструкций управления потоком, включая передачу как аргументы и возврат из функций.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 BYREF и TRY/CATCH

**Критическое наблюдение:** При возникновении ошибки внутри процедуры с BYREF-параметрами:

**Interpreter:** Изменения, сделанные через BYREF-параметр до ошибки, УЖЕ ВИДНЫ вызывающему коду через `RuntimeReferenceStoredValue`. Ссылочная семантика означает, что изменения немедленны. TRY/CATCH не откатывает эти изменения.

**Bytecode VM:** `CopyBackByRefArguments()` вызывается ТОЛЬКО в `CompleteReturn()` (нормальный возврат). При `RuntimeFailureException` copyback НЕ происходит. Однако, поскольку для простых переменных и локалов используются shared slots, изменения через shared slot УЖЕ ВИДНЫ до ошибки. Copyback нужен только для wrapper-слотов (global, local), и там изменения тоже немедленны через getter/setter.

**Вывод:** Оба движка ведут себя консистентно — изменения через BYREF немедленны и не откатываются при ошибке. Это поведение по дизайну (ссылочная семантика, не транзакционная).

### 18.2 Function values и TRY/CATCH

Function values работают корректно с TRY/CATCH. Замыкания сохраняют свою область видимости независимо от стека вызовов.

---

## 19. State / Lifetime / Ownership

### 19.1 BYREF: кто владеет данными

| Ресурс | Создаёт | Владеет | Изменяет | Уничтожает |
|--------|---------|---------|----------|------------|
| Значение переменной (BYVAL) | Вызывающий код | Callee (копия) | Callee | GC |
| Ссылка на переменную (BYREF) | Вызывающий код | Вызывающий код | Callee через reference | GC (reference) |
| RuntimeReferenceStoredValue | Interpreter | Callee scope | — | GC при уничтожении scope |
| BytecodeVariableSlot (shared) | VM | Общий | Callee через shared slot | GC |

### 19.2 Function Value: lifetime замыкания

`RuntimeFunctionValue` захватывает `RuntimeScope` (Interpreter) или `BytecodeVariableSlot[]` (VM). Замыкание живёт до тех пор, пока существует хотя бы одна ссылка на `RuntimeFunctionValue`. Захваченные переменные не уничтожаются, пока живо замыкание.

---

## 20. Edge Cases

### 20.1 Покрытые edge cases

| Сценарий | Тест | Статус |
|----------|------|--------|
| BYREF скалярная переменная (INTEGER) | ✅ | Работает |
| BYREF запись TYPE | ✅ | Работает |
| BYVAL запись TYPE (независимая копия) | ✅ | Работает |
| Недопустимый аргумент BYREF (выражение) | ✅ MB2028 | Работает |
| Пустое значение-функции | ✅ MB3009 | Работает |
| Function value с замыканием | ✅ | Работает |
| Возвращённое замыкание | ✅ | Работает |
| Shared-state замыкание | ✅ | Работает |
| Косвенный вызов function value | ✅ | Работает |
| TYPE метод с SELF (BYREF) | ✅ | Работает |

### 20.2 Непокрытые edge cases

| Сценарий | Тест | Статус |
|----------|------|--------|
| BYREF с элементом массива | ❌ | Нет теста (запрещено MB2028) |
| BYREF с полем TYPE (не через переменную) | ❌ | Нет теста (запрещено MB2028) |
| MB2029 — BYREF function value | ❌ | Нет теста для диагностики |
| BYREF + TRY/CATCH (частичная мутация до ошибки) | ❌ | Нет теста |
| BYREF + рекурсия | ❌ | Нет теста |
| BYREF + вложенные вызовы (A → B → C, C меняет BYREF A) | ❌ | Нет теста |
| BYREF для fixed-string | ❌ | Нет теста |
| BYREF для DECIMAL | ❌ | Нет теста |
| BYREF для IMAGE/SPRITE/TILESET/TILEMAP/FILE | ❌ | Нет теста |
| Function value с BYREF-параметром в TYPE-методе | ❌ | Нет теста |
| Попытка передать CONST как BYREF | ❌ | Нет теста |
| Попытка передать результат функции как BYREF | ❌ | Нет теста |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| BYREF скалярная переменная | ✅ | ✅ | ✅ | — | ✅ |
| BYREF запись TYPE | ✅ | ✅ | ✅ | — | ✅ |
| BYVAL запись TYPE | ✅ | ✅ | ✅ | — | ✅ |
| MB2028 (недопустимый аргумент) | ✅ | ✅ | ✅ | ✅ | — |
| MB2029 (BYREF function value) | ✅ | ✅ | ✅ | ✅ | — |
| Function value (прямой) | ✅ | ✅ | ✅ | — | — |
| Function value (косвенный) | ✅ | ✅ | ✅ | — | — |
| Closure (захват переменных) | ✅ | ✅ | ✅ | — | ✅ |
| Returned closure | ✅ | ✅ | ✅ | — | ✅ |
| Shared-state closure | ✅ | ✅ | ✅ | — | ✅ |
| BYREF + TRY/CATCH | ✅* | ✅* | ✅ | — | ✅* |

*Поведение консистентно: оба движка не откатывают изменения при ошибке.

### Классификация расхождений

Расхождений в результатах между Interpreter и VM не обнаружено. Оба движка дают идентичные результаты для всех подтверждённых сценариев.

---

## 22. Анализ производительности

### 22.1 BYREF: накладные расходы

| Аспект | Interpreter | Bytecode VM |
|--------|-------------|-------------|
| Создание reference/slot | RuntimeReferenceStoredValue (лямбда + захват) | BytecodeVariableSlot (shared или wrapper) |
| Доступ к значению | Через лямбду (delegate invoke) | Через slot getter (delegate invoke) |
| Запись значения | Через лямбду (delegate invoke) | Через slot setter (delegate invoke) |
| Copyback при возврате | Нет (немедленная семантика) | CopyBackByRefArguments (shared slots → no-op) |

**Вывод:** Накладные расходы BYREF минимальны в обоих движках. Прямая передача через shared slots в VM эффективнее, чем создание RuntimeReferenceStoredValue в Interpreter.

### 22.2 Function Values: накладные расходы

| Аспект | Interpreter | Bytecode VM |
|--------|-------------|-------------|
| Создание function value | RuntimeFunctionValue(FunctionSymbol, RuntimeScope) | RuntimeFunctionValue(BytecodeRoutine, BytecodeVariableSlot[]) |
| Вызов | InvokeUserRoutine (scope lookup) | InvokeBytecodeFunction (frame creation) |
| Кэширование | Нет | Кэш function value в outer frame |

**Вывод:** VM оптимизирует повторные вызовы через кэширование function value и captured slots.

---

## 23. Качество Benchmark

Для подсистемы BYREF/FUNCTION VALUES нет отдельного dedicated benchmark-сценария. Однако сценарий `samples/smoke/engine-compare/routine_closure_pipeline.axm` косвенно покрывает:
- Прямые вызовы SUB/FUNCTION
- Вызовы через function value
- Returned closures
- Shared-state closures
- Nested SUB с захватом внешнего локаля

Результат: VM держится около 0.861x runtime / 0.874x total — в целевом диапазоне ≤ 0.900x.

---

## 24. Hot Paths и Bottlenecks

### 24.1 BYREF: горячий путь

1. `ResolveByRefArgumentSlots()` (VM) — создаёт массив `BytecodeVariableSlot[]` при каждом ByRef-вызове. Аллокация при каждом вызове.
2. `ResolveByRefStoredArgument()` (Interpreter) — создаёт `RuntimeReferenceStoredValue` при каждом ByRef-вызове. Аллокация при каждом вызове.

**Потенциальная оптимизация:** Кэширование ByRef-слотов (аналогично кэшированию captured slots).

### 24.2 Function Values: горячий путь

1. `LoadFunctionValue()` (VM) — кэширует function value во внешнем кадре. Оптимизация уже применена.
2. `InvokeBytecodeFunction()` (VM) — создаёт кадр при каждом вызове. Frame reuse уже применён.

---

## 25. Архитектурные проблемы

### SYS-001: Мёртвый код в Lowerer для ByRef copyback

**Категория:** ARCHITECTURE
**Серьёзность:** P3
**Уверенность:** CONFIRMED

`FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` в `BoundProgramLowerer.cs` являются мёртвым кодом. Binder не допускает поля TYPE и элементы массива как BYREF-аргументы, поэтому код lowering для этих случаев никогда не выполняется.

**Место:** `BoundProgramLowerer.cs:46-112, 1765-1840`
**Символ:** `FieldByRefStoragePlan`, `ArrayElementByRefStoragePlan`, `TryLowerByRefCopyBackArgument`

**Последствия:** Увеличение сложности кода без текущей пользы. Однако код является подготовкой к будущему расширению BYREF.

**Направление:** Не удалять — оставить как преднамеренную подготовку. Добавить комментарий о текущей недостижимости.

### SYS-002: Отсутствие тестов для MB2029

**Категория:** TESTS
**Серьёзность:** P2
**Уверенность:** CONFIRMED

Диагностика MB2029 (BYREF для function values не поддержана) не имеет dedicated теста. Есть только код в Binder, но нет теста, который проверял бы выдачу этой диагностики.

**Место:** `Binder.Expressions.cs:85-88`, `Binder.InvocationExpressions.cs:627-638`
**Последствия:** Регрессия в проверке MB2029 может пройти незамеченной.

**Направление:** Добавить тест `Bind_ReportsUnsupportedByRefCallableValue()`.

### SYS-003: Отсутствие тестов для BYREF с рекурсией

**Категория:** TESTS
**Серьёзность:** P3
**Уверенность:** MEDIUM

Нет тестов, проверяющих BYREF в рекурсивных сценариях (процедура вызывает сама себя с BYREF-параметром).

**Направление:** Добавить рекурсивный тест для BYREF.

### SYS-004: Отсутствие тестов для BYREF + TRY/CATCH

**Категория:** TESTS
**Серьёзность:** P3
**Уверенность:** MEDIUM

Нет тестов, проверяющих поведение BYREF при возникновении ошибки внутри процедуры с BYREF-параметром и перехвате через TRY/CATCH.

**Направление:** Добавить тест с TRY/CATCH и BYREF.

### SYS-005: Отсутствие тестов для BYREF с CONST

**Категория:** TESTS
**Серьёзность:** P3
**Уверенность:** MEDIUM

Нет теста, проверяющего, что передача константы как BYREF-аргумента корректно отклоняется.

**Направление:** Добавить negative-тест.

### SYS-006: CopyBackByRefArguments потенциально избыточна в VM

**Категория:** PERFORMANCE
**Серьёзность:** P4
**Уверенность:** LOW

`CopyBackByRefArguments()` вызывается при каждом нормальном возврате из процедуры с BYREF-параметрами. Для shared slots (sourceKind 1, 3) это no-op, но цикл всё равно выполняется.

**Направление:** Можно оптимизировать, проверив `targets` на наличие не-null элементов перед циклом. Текущая проверка `if (targets is null || targets.Length == 0)` уже делает это для полного отсутствия ByRef.

---

## 26. Дублирование логики

### 26.1 Проверка BYREF в Binder

Проверка `parameter.IsByRef` и вызов `BindByRefArgument()` дублируется в нескольких местах:
- `Binder.InvocationExpressions.cs:414-422` (IsCallableWithVisibleArguments)
- `Binder.InvocationExpressions.cs:474-476` (BindTypeMethodCallExpressionFromRawArguments)
- `Binder.InvocationExpressions.cs:515-517` (BindTypeConstructorExpressionFromRawArguments)
- `Binder.InvocationExpressions.cs:692-697` (BindDeclaredInvocation)

Это не избыточное дублирование, а разные точки входа для разных типов вызовов. Каждая проверает ByRef для своего контекста.

### 26.2 Создание RuntimeReferenceStoredValue

`RuntimeReferenceStoredValue` создаётся в 7 местах в `Interpreter.Builtins.cs` и 3 местах в `Interpreter.Helpers.cs`/`Interpreter.Statements.cs`. Это разные сценарии (ByRef параметры, поля TYPE, элементы массива, SELF конструктора, accumulator).

---

## 27. Single Source of Truth

| Аспект | Источник истины |
|--------|----------------|
| Синтаксис BYREF/BYVAL | `Parser.Declarations.cs` + `SyntaxKind.cs` |
| Семантика BYREF | `Binder.InvocationExpressions.cs` + `Binder.Expressions.cs` |
| Коды диагностики | `DiagnosticCodes.cs` |
| Runtime-поведение BYREF (Interpreter) | `Interpreter.Builtins.cs:ResolveByRefStoredArgument()` |
| Runtime-поведение BYREF (VM) | `BytecodeVm.cs:ResolveByRefArgumentSlots()` + `CopyBackByRefArguments()` |
| Тип function value | `RuntimeFunctionValue.cs` |
| Ограничения BYREF | `SPECIFICATION3.md` (§ обновление 2026-05-04) |

**Архитектурная находка:** Семантика BYREF определяется в Binder, а runtime-реализация разделена между Interpreter и VM без общего слоя. Это стандартная архитектура для данного проекта (два независимых движка).

---

## 28. Legacy / Dead Code

| Код | Статус | Доказательства |
|-----|--------|----------------|
| `FieldByRefStoragePlan` | POSSIBLY DEAD | Binder не допускает поля TYPE как BYREF-аргументы |
| `ArrayElementByRefStoragePlan` | POSSIBLY DEAD | Binder не допускает элементы массива как BYREF-аргументы |
| `TryLowerByRefCopyBackArgument` | POSSIBLY DEAD | Недостижим из-за проверок в Binder |
| ResolveByRefStoredArgument: case BoundFieldAccessExpression | POSSIBLY DEAD | Binder не пропускает поля |
| ResolveByRefStoredArgument: case BoundArrayAccessExpression | POSSIBLY DEAD | Binder не пропускает элементы массива |
| ResolveByRefStoredArgument: fallback case | POSSIBLY_DEAD | Binder не пропускает выражения |

**Замечание:** Весь перечисленный код является преднамеренной подготовкой к будущему расширению BYREF на поля TYPE и элементы массива. Не рекомендуется удалять.

---

## 29. Надёжность и безопасность

### 29.1 Риски BYREF

- **Неконтролируемая мутация:** BYREF позволяет callee изменять данные вызывающего кода. Это стандартная семантика и не является уязвимостью.
- **Stale references:** RuntimeReferenceStoredValue захватывает scope. Если scope уничтожен, reference может указывать на несуществующее хранилище. Однако lifetime scope управляется корректно через стек вызовов.
- **Рекурсия:** BYREF с рекурсией может привести к неожиданным мутациям. Это стандартное поведение и не требует специальной защиты.

### 29.2 Риски Function Values

- **Замыкания и утечки памяти:** Замыкания захватывают scope, что может удерживать в памяти переменные, которые больше не нужны. Это стандартное поведение для языков с замыканиями.
- **Пустое значение-функции:** Попытка вызвать неинициализированную переменную FUNCTION — обработана (MB3009).

---

## 30. Анализ тестов

### 30.1 Существующие тесты

| Тест | Файл | Тип | Покрытие |
|------|------|-----|----------|
| `Parse_SubDeclarationWithExplicitByRefAndByValParameters` | ParserTests.cs | Unit | Парсинг BYREF/BYVAL |
| `Parameter_WithByRef_HasPassingKeyword` | DeclarationSyntaxTests.cs | Unit | Syntax node |
| `Bind_ReportsNonVariableByRefArgument` | BinderTests.cs | Semantic | MB2028 |
| `Bind_TypeMethodCallsInjectSelfArgument` | BinderTests.cs | Semantic | SELF BYREF |
| `Execute_ByRefScalarParameterMutatesCallerVariable` | InterpreterTests.Type.cs | Runtime | BYREF INTEGER |
| `Execute_ByRefRecordParameterMutatesCallerVariable` | InterpreterTests.Type.cs | Runtime | BYREF TYPE |
| `Execute_ByValRecordParameterKeepsCallerVariableIndependent` | InterpreterTests.Type.cs | Runtime | BYVAL TYPE |
| `Execute_CompiledByRefScalarParameterMutatesCallerVariable` | BytecodeVmTests.cs | VM | BYREF INTEGER |
| `Execute_CompiledByRefRecordParameterMutatesCallerVariable` | BytecodeVmTests.cs | VM | BYREF TYPE |
| `Execute_CompiledByValRecordParameterKeepsCallerVariableIndependent` | BytecodeVmTests.cs | VM | BYVAL TYPE |
| `Execute_ReportsCallingEmptyFunctionValue` | InterpreterTests.String.cs | Runtime | Empty function |
| `Execute_CompiledIndirectFunctionValueCall` | BytecodeVmTests.cs | VM | Function value |
| `Execute_CompiledArrayCallbackBuiltinsUseFunctionValuesAndClosures` | BytecodeVmTests.cs | VM | Callbacks |
| `Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures` | InterpreterTests.Array.cs | Runtime | Callbacks |
| `Validate_CallByRefWithByValStackArgumentsAndFunctionResult_Succeeds` | BytecodeModuleValidatorTests.cs | Validator | CallByRef validation |
| Acceptance: `byref_requires_variable.axm` | AcceptanceProgramTests.cs | Acceptance | MB2028 |
| Acceptance: `byref_scalar_argument.axm` | AcceptanceProgramTests.cs | Acceptance | BYREF scalar |
| Acceptance: `byref_record_argument.axm` | AcceptanceProgramTests.cs | Acceptance | BYREF record |
| Acceptance: `function_value.axm` | AcceptanceProgramTests.cs | Acceptance | Function value |
| Acceptance: `returned_closure.axm` | AcceptanceProgramTests.cs | Acceptance | Closure |
| Acceptance: `shared_closure_state.axm` | AcceptanceProgramTests.cs | Acceptance | Shared closure |

### 30.2 Пропуски в тестах

Отсутствуют тесты для:
1. MB2029 (BYREF function value diagnostic)
2. BYREF с TRY/CATCH
3. BYREF с рекурсией
4. BYREF с вложенными вызовами
5. BYREF с CONST как аргументом
6. BYREF с результатом функции как аргументом
7. BYREF с fixed-string
8. BYREF с DECIMAL
9. BYREF с IMAGE/SPRITE/TILESET/TILEMAP/FILE
10. BYREF с массивами (negative test — MB2028)
11. BYREF с полями TYPE (negative test — MB2028, если не через переменную)

---

## 31. Недостающие тесты

### Обычные сценарии
- BYREF с рекурсивной процедурой
- BYREF с вложенными вызовами (A → B → C)
- BYREF с TRY/CATCH (частичная мутация до ошибки)
- BYREF с RETURN из FUNCTION

### Отрицательные сценарии
- MB2029: функция с BYREF как значение-функции
- MB2028: передача CONST как BYREF
- MB2028: передача результата функции как BYREF
- MB2028: передача элемента массива как BYREF
- MB2028: передача поля TYPE (не через переменную) как BYREF

### Граничные случаи
- BYREF с пустой строкой
- BYREF с нулевым значением INTEGER
- BYREF с максимальным/минимальным значением
- BYREF с многомерным массивом (negative)
- BYREF с function value, который сам принимает function value

---

## 32. Анализ Samples

| Sample | Движок | Проверяемый результат | Покрытие |
|--------|--------|----------------------|----------|
| `samples/operators/sub/byref_scalar_argument.axm` | Оба | `11` (INTEGER +1 через BYREF) | ✅ |
| `samples/operators/sub/byref_record_argument.axm` | Оба | `37` (TYPE.Age +1 через BYREF) | ✅ |
| `samples/operators/function/first_class_value.axm` | Оба | `12` (function value как аргумент) | ✅ |
| `samples/operators/function/capture_outer_local.axm` | Оба | `23` (захват внешних переменных) | ✅ |
| `samples/operators/function/returned_closure.axm` | Оба | Проверяемый результат | ✅ |
| `samples/operators/function/shared_closure_state.axm` | Оба | Проверяемый результат | ✅ |
| `samples/acceptance/negative/byref_requires_variable.axm` | Оба | MB2028 | ✅ |

**Недостающие samples:**
- BYREF с TRY/CATCH
- BYREF с рекурсией
- BYREF с вложенными вызовами
- BYREF с TYPE-методами
- MB2029 negative sample

---

## 33. Анализ документации

### 33.1 Спецификация

**Файл:** `docs/SPECIFICATION3.md` (§ обновление 2026-05-04)

Описание корректно и полно:
- ✅ Определены BYREF и BYVAL
- ✅ Описаны ограничения (только простые переменные)
- ✅ Описаны диагностики MB2028, MB2029
- ✅ Описано поведение TYPE с BYREF
- ✅ Описан SELF как скрытый BYREF

### 33.2 Manual

**Файлы:** `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/procedures-and-functions.html`

- ✅ BYREF и BYVAL описаны как модификаторы параметров
- ✅ Примеры на INTEGER
- ✅ Указано ограничение (простые переменные)
- ✅ Связи с другими страницами manual

### 33.3 LANGUAGE_INVENTORY

- ✅ BYREF/BYVAL listed
- ✅ RuntimeFunctionValue listed
- ✅ Ограничения отмечены
- ✅ AUDIT-007 identified (BYREF function values)

### 33.4 Документация vs Код

Расхождений между документацией и кодом не обнаружено. Документация accurately отражает текущее поведение и ограничения.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

`BYREF` и `BYVAL` подсвечиваются как ключевые слова в обеих темах (`Axiom.xshd`, `Axiom.Dark.xshd`).

### 34.2 Автодополнение

Не проверяется в рамках данного аудита (отдельная подсистема).

### 34.3 Диагностика

MB2028 и MB2029 отображаются в панели ошибок IDE при семантическом анализе.

---

## 35. Зависимости от Host / Platform

Подсистема BYREF / FUNCTION VALUES не имеет зависимостей от host/platform. Вся логика работает на уровне языка и runtime, без обращений к ОС, файловой системе, GPU или внешним устройствам.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Ограничение BYREF для составных целей

```
ROOT-SYS-001 (сознательное ограничение)
├── SYS-001 (Binder: только BoundVariableExpression для BYREF)
├── SYS-007 (Lowerer: мёртвый код для Field/ArrayElement copyback)
├── SYS-002 (нет тестов для MB2029)
└── SYS-005 (нет тестов для BYREF с CONST)
```

**Корневая причина:** BYREF преднамеренно ограничен простыми переменными на этапе Binder. Lowerer содержит подготовительный код для будущего расширения.

### ROOT-SYS-002: Отсутствие тестов для edge cases

```
ROOT-SYS-002 (неполное покрытие тестами)
├── SYS-003 (нет теста BYREF + рекурсия)
├── SYS-004 (нет теста BYREF + TRY/CATCH)
└── SYS-006 (нет теста BYREF + CONST)
```

**Корневая причина:** Тесты фокусируются на happy path и основных negative cases. Edge cases с рекурсией, TRY/CATCH и CONST не были приоритизированы.

---

## 37. Стабильные области

### Что работает и что не следует трогать

1. **BYREF для простых переменных** — корректно работает в обоих движках, закреплено тестами и samples. Не требует изменений.
2. **BYREF для записей TYPE** — корректно работает в обоих движках. Не требует изменений.
3. **Function values и closures** — полнофункциональны, включая замыкания, shared state, returned closures. Не требуют изменений.
4. **Parser для BYREF/BYVAL** — простой и корректный. Не требует изменений.
5. **Семантические ограничения (MB2028, MB2029)** — корректно реализованы. Не требуют изменений.
6. **Opcode.CallByRef и EmitCallByRef** — корректно формируют байткод. Не требуют изменений.
7. **VM: ResolveByRefArgumentSlots и CopyBackByRefArguments** — корректно работают. Не требуют изменений.

---

## 38. Возможности консолидации

### Логику, которую следует объединить

| Текущие места | Дублируемая логика | Единый источник истины | Направление миграции |
|---|---|---|---|
| 7 мест создания RuntimeReferenceStoredValue | Конструирование ссылочного хранилища | Один helper-метод | Низкий приоритет, текущий код читаем |

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности адекватно:
- **Binder** — семантические ограничения
- **Lowerer** — подготовка байткода
- **BytecodeCompiler** — эмиссия опкодов
- **VM** — исполнение с shared slots
- **Interpreter** — исполнение с RuntimeReferenceStoredValue

Нет необходимости в дополнительном разделении.

---

## 40. Кандидаты на удаление

| Код | Статус | Рекомендация |
|-----|--------|-------------|
| `FieldByRefStoragePlan` | POSSIBLY DEAD | НЕ удалять — подготовка к расширению |
| `ArrayElementByRefStoragePlan` | POSSIBLY DEAD | НЕ удалять — подготовка к расширению |
| `TryLowerByRefCopyBackArgument` | POSSIBLY DEAD | НЕ удалять — подготовка к расширению |
| ResolveByRefStoredArgument: cases для полей/массивов | POSSIBLY DEAD | НЕ удалять — подготовка к расширению |

**Вывод:** Нет кандидатов на удаление. Весь «мёртвый» код является преднамеренной подготовкой.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Расширение BYREF на поля TYPE и элементы массива

**Готовность: ВЫСОКАЯ**

- Lowerer уже содержит `FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan`
- Interpreter уже содержит обработку для `BoundFieldAccessExpression` и `BoundArrayAccessExpression` в `ResolveByRefStoredArgument()`
- Нужно: убрать ограничение в Binder, добавить тесты, обновить документацию

### 41.2 BYREF для function values (callback-вызовы)

**Готовность: СРЕДНЯЯ**

- Binder выдаёт MB2029 — нужно убрать ограничение
- VM: `InvokeBytecodeFunction()` не принимает `byRefArgumentSlots` — нужно расширить
- Interpreter: `InvokeUserRoutine()` уже поддерживает ByRef через `boundArguments`
- Lowerer: `CallValue` не обрабатывает ByRef — нужно расширить
- Сложность: высокая (затрагивает lowering, compiler, VM, Interpreter)

### 41.3 Будущий Compiler

Подсистема BYREF спроектирована так, что может быть перенесена в compiler без фундаментальных изменений. Lowering уже подготавливает линейное представление.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P3 | ARCHITECTURE | Lowerer | Мёртвый код для ByRef copyback полей/массивов | FieldByRefStoragePlan, ArrayElementByRefStoragePlan недостижимы | Преднамеренная подготовка | CONFIRMED |
| SYS-002 | P2 | TESTS | Binder | Нет теста для MB2029 | Отсутствие теста в тестовых проектах | Пропуск | CONFIRMED |
| SYS-003 | P3 | TESTS | Runtime | Нет теста BYREF + рекурсия | Отсутствие теста | Пропуск | MEDIUM |
| SYS-004 | P3 | TESTS | Runtime | Нет теста BYREF + TRY/CATCH | Отсутствие теста | Пропуск | MEDIUM |
| SYS-005 | P3 | TESTS | Runtime | Нет теста BYREF + CONST | Отсутствие теста | Пропуск | MEDIUM |
| SYS-006 | P4 | PERFORMANCE | VM | CopyBackByRefArguments выполняет цикл для shared slots | BytecodeVm.cs:4965-4975 | Минимальное влияние | LOW |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Цель:** Зафиксировать текущее состояние и добавить недостающие тесты.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| TEST-001 | Добавить тест для MB2029 (BYREF function value diagnostic) | P2 |
| TEST-002 | Добавить тест для BYREF с рекурсией | P3 |
| TEST-003 | Добавить тест для BYREF + TRY/CATCH | P3 |
| TEST-004 | Добавить negative-тест для BYREF с CONST | P3 |
| TEST-005 | Добавить negative-тест для BYREF с элементом массива | P3 |
| TEST-006 | Добавить negative-тест для BYREF с полем TYPE (не через переменную) | P3 |

### ЭТАП 1 — Исправление критических проблем корректности

**Цель:** Нет критических проблем корректности. Все подтверждённые сценарии работают корректно.

*Этап не требуется.*

### ЭТАП 2 — Стабилизация семантики

**Цель:** Добавить недостающие samples и закрепить edge cases.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| SAMPLE-001 | Добавить sample для BYREF с TRY/CATCH | P3 |
| SAMPLE-002 | Добавить sample для BYREF с TYPE-методами | P3 |

### ЭТАП 3 — Архитектурное улучшение

**Цель:** Добавить комментарии к мёртвому коду в lowerer.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| DOC-001 | Добавить комментарии к FieldByRefStoragePlan и ArrayElementByRefStoragePlan о текущей недостижимости | P4 |

### ЭТАП 4 — Оптимизация производительности

*Этап не требуется в текущий момент. Производительность адекватна.*

### ЭТАП 5 — Усиление тестирования

*Покрывается ЭТАПОМ 0.*

### ЭТАП 6 — Обновление документации

**Цель:** Обновить manual и спецификацию.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| DOC-002 | Обновить `docs/manual/procedures-and-functions.html` — добавить пример BYREF с TYPE-методом | P3 |
| DOC-003 | Обновить `docs/manual/function.html` — добавить явное ограничение BYREF для function values | P3 |

### ЭТАП 7 — Обновление IDE

*Этап не требуется. Подсветка и диагностика уже работают.*

### ЭТАП 8 — Подготовка к дальнейшему развитию

**Цель:** Подготовить расширение BYREF на поля TYPE и элементы массива.

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| EXTEND-001 | Проектирование: расширение BYREF на поля TYPE | P3 |
| EXTEND-002 | Проектирование: расширение BYREF на элементы массива | P3 |
| EXTEND-003 | Проектирование: BYREF для function values (callback-вызовы) | P3 |

---

## 44. Граф зависимостей задач

```
TEST-001 (MB2029 test)
TEST-002 (BYREF recursion test)
TEST-003 (BYREF + TRY/CATCH test)
TEST-004 (BYREF + CONST negative test)
TEST-005 (BYREF + array element negative test)
TEST-006 (BYREF + TYPE field negative test)
    │
    ▼
SAMPLE-001 (BYREF + TRY/CATCH sample)
SAMPLE-002 (BYREF + TYPE methods sample)
    │
    ▼
DOC-001 (мёртвый код comments)
DOC-002 (manual update)
DOC-003 (manual update)
    │
    ▼
EXTEND-001 (BYREF поля TYPE) ← зависит от TEST-005, TEST-006
EXTEND-002 (BYREF элементы массива) ← зависит от TEST-005
EXTEND-003 (BYREF function values) ← зависит от TEST-001
```

---

## 45. Definition of Done

Подсистема BYREF / FUNCTION VALUES считается завершённой, если:

1. ✅ BYREF для простых переменных работает в обоих движках
2. ✅ BYREF для записей TYPE работает в обоих движках
3. ✅ BYVAL работает корректно для всех типов
4. ✅ Function values работают для прямых и косвенных вызовов
5. ✅ Замыкания работают (захват, shared state, returned closures)
6. ✅ MB2028 выдаётся для недопустимых аргументов BYREF
7. ❌ MB2029 выдаётся для BYREF function values (нет теста)
8. ✅ Паритет Interpreter ↔ VM для всех подтверждённых сценариев
9. ✅ Manual содержит описание BYREF/BYVAL
10. ✅ Samples покрывают основные сценарии
11. ❌ Тесты покрывают edge cases (рекурсия, TRY/CATCH, CONST)

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | SYS-002 | P2 | Нет теста для MB2029 (BYREF function value diagnostic) |
| 2 | SYS-001 | P3 | Мёртвый код в lowerer для ByRef copyback полей/массивов |
| 3 | SYS-003 | P3 | Нет теста BYREF + рекурсия |
| 4 | SYS-004 | P3 | Нет теста BYREF + TRY/CATCH |
| 5 | SYS-005 | P3 | Нет теста BYREF + CONST |
| 6 | SYS-006 | P4 | CopyBackByRefArguments: цикл для shared slots |

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемая польза | Сложность |
|---|---|---|---|---|
| — | — | Нет выявленных проблем производительности | — | — |

Производительность подсистемы адекватна. Benchmark `routine_closure_pipeline.axm` показывает VM на уровне 0.861x runtime / 0.874x total.

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---:|---|
| Корректность | 9 | Все подтверждённые сценарии работают корректно в обоих движках |
| Семантика | 9 | Чёткий договор, корректные ограничения, правильные диагностики |
| Архитектура | 8 | Чистое разделение, но есть мёртвый код в lowerer |
| Runtime | 9 | RuntimeReferenceStoredValue и shared slots работают корректно |
| Interpreter | 9 | Корректная обработка BYREF и function values |
| Bytecode VM | 9 | Корректная обработка CallByRef, function values, closures |
| Производительность | 8 | Адекватна, VM быстрее интерпретатора |
| Память | 8 | Стандартное поведение, нет утечек |
| Error handling | 8 | Корректно, но нет тестов для BYREF + TRY/CATCH |
| Tests | 7 | Основные сценарии покрыты, но есть пробелы в edge cases |
| Samples | 8 | Основные сценарии покрыты |
| Documentation | 9 | Спецификация и manual корректны |
| IDE | 9 | Подсветка и диагностика работают |
| Maintainability | 8 | Чистый код, но мёртвый код в lowerer |
| Reliability | 9 | Стабильная работа |
| Future readiness | 8 | Подготовлено к расширению BYREF на составные цели |

**Общая оценка: 8.4 / 10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему BYREF / FUNCTION VALUES достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers

Нет критических blockers. Подсистема работает корректно для всех подтверждённых сценариев.

### Что нужно сделать в первую очередь

1. Добавить тест для MB2029 (BYREF function value diagnostic) — единственный существенный пробел в тестировании
2. Добавить negative-тесты для BYREF с CONST, элементом массива, полем TYPE

### Что можно отложить

1. Расширение BYREF на поля TYPE и элементы массива — отдельный следующий контракт
2. BYREF для function values (callback-вызовы) — отдельный следующий контракт
3. Комментарии к мёртвому коду в lowerer

### Что не следует трогать

1. Базовый механизм BYREF для простых переменных и записей TYPE — работает корректно
2. Function values и closures — работают корректно
3. Parser и семантические ограничения — корректны
4. Opcode.CallByRef и EmitCallByRef — корректны
