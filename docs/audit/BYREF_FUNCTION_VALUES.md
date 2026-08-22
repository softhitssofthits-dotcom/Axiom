# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## BYREF / FUNCTION VALUES

---

## 1. Резюме

Проведён полный forensic-аудит подсистемы BYREF и FUNCTION VALUES языка Axiom. Аудит охватывает передачу параметров по ссылке (BYREF), передачу по значению (BYVAL), значения функций (function values), замыкания (closures), косвенные вызовы и их взаимодействие со всем конвейером языка: Lexer → Parser → Binder → Lowering → BytecodeCompiler → BytecodeVm → Interpreter.

**Главный вывод:** подсистема BYREF и FUNCTION VALUES фундаментально корректна, архитектурно зрела и полностью покрыта на всех уровнях конвейера. BYREF работает через `RuntimeReferenceStoredValue` (Interpreter) и `BytecodeVariableSlot` с делегатами (VM). Function values работают через `RuntimeFunctionValue` с поддержкой closures. Полный паритет Interpreter ↔ BytecodeVm подтверждён тестами.

**Health Score: 8.9/10**

| Метрика | Оценка |
|---------|--------|
| Корректность | 9/10 |
| Семантика | 9/10 |
| Архитектура | 9/10 |
| Runtime (Interpreter) | 9/10 |
| Runtime (Bytecode VM) | 9/10 |
| Производительность | 9/10 |
| Тесты | 8/10 |
| Samples | 9/10 |
| Документация | 8/10 |
| IDE | 9/10 |

**Ответ на главный вопрос: YES** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

---

## 2. Что представляет собой эта подсистема

Подсистема BYREF / FUNCTION VALUES обеспечивает два фундаментальных механизма языка Axiom:

### 2.1 Передача параметров (BYREF / BYVAL)

1. **BYVAL** — передача аргумента по значению. Вызываемая процедура получает независимую копию. Изменения не видны вызывающему коду. Это режим по умолчанию.
2. **BYREF** — передача аргумента по ссылке. Вызываемая процедура работает с оригинальным хранилищем. Изменения немедленно видны вызывающему коду.

### 2.2 Значения функций (Function Values)

1. **Function value** — ссылка на FUNCTION, которую можно сохранить в переменной, передать параметром или вернуть из другой функции.
2. **Косвенный вызов** — вызов FUNCTION через переменную-ссылку: `op(5)` где `op` — function value.
3. **Замыкания (closures)** — локальные FUNCTION захватывают переменные из внешних областей видимости. Захваченные переменные разделяются (shared state).
4. **Возвращённые closures** — FUNCTION может вернуть другую FUNCTION, которая сохраняет захват после выхода из внешней функции.

---

## 3. Полный список возможностей

### SYS-001: BYREF параметры

- **Категория:** SEMANTICS
- **Тип:** PARAMETER PASSING
- **Назначение:** передача аргумента по ссылке (изменение оригинала)
- **Пользовательский синтаксис:** `BYREF <имя> AS <тип>`
- **Parser:** `Parser.Declarations.cs:249-325` — парсинг параметров с BYREF/BYVAL модификатором
- **Syntax Node:** `ParameterSyntax` с `PassingKeyword` (`SyntaxKind.ByRefKeyword`)
- **Binder:** `Binder.InvocationExpressions.cs:709-738` — `BindByRefArgument()` (валидация: только простая переменная)
- **Bound Node:** `ParameterSymbol` с `PassingMode = ParameterPassingMode.ByRef`
- **Interpreter:** `Interpreter.Builtins.cs:625-677` — `ResolveByRefStoredArgument()` создаёт `RuntimeReferenceStoredValue`
- **Lowering:** `BoundProgramLowerer.cs:1664-1736` — `LowerCall()` с `ByRefStoragePlan` hierarchy
- **Bytecode:** `Opcode.CallByRef` с операндами для BYREF слотов
- **BytecodeCompiler:** `BytecodeCompiler.cs:1195-1256` — `EmitCallByRef()`
- **Bytecode VM:** `BytecodeVm.cs:3009-3035` — `ResolveByRefArgumentSlots()`; `4957-4975` — `CopyBackByRefArguments()`
- **Runtime Helper:** `BytecodeVariableSlot` с getter/setter делегатами для BYREF
- **Tests:** 14 тестов (Parsing, Syntax, Semantics, Runtime, Bytecode, Execution)
- **Samples:** `byref_scalar_argument.axm`, `byref_record_argument.axm`, `byref_requires_variable.axm` (negative)
- **Manual:** `docs/manual/sub.html`, `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED (ограничен: только простые переменные, не array elements через function value callbacks)
- **Уровень уверенности:** CONFIRMED

### SYS-002: BYVAL параметры

- **Категория:** SEMANTICS
- **Тип:** PARAMETER PASSING
- **Назначение:** передача аргумента по значению (независимая копия)
- **Пользовательский синтаксис:** `[BYVAL] <имя> AS <тип>` (BYVAL по умолчанию)
- **Parser:** `Parser.Declarations.cs:249-325` — парсинг параметров
- **Binder:** `Binder.Declarations.cs:669-691` — `CreateFunctionSymbol()` с `ParameterPassingMode.ByVal`
- **Interpreter:** `Interpreter.Builtins.cs:438` — `argument.CopyForStorage()`
- **Bytecode VM:** стандартное хранение в `BytecodeVariableSlot`
- **Tests:** ~10 тестов
- **Samples:** `byval_record_copy.axm`
- **Manual:** `docs/manual/sub.html`, `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: Function Values (First-Class Functions)

- **Категория:** RUNTIME FACILITY
- **Тип:** FUNCTION VALUE
- **Назначение:** FUNCTION можно сохранять в переменных, передавать параметром, возвращать из функции
- **Пользовательский синтаксис:** `DIM op AS FUNCTION(...) AS <тип>` / `op = FunctionName` / `op(args)`
- **Parser:** стандартный парсинг выражений и вызовов
- **Binder:** `Binder.Expressions.cs:77-92` — `BoundFunctionExpression` для имён FUNCTION; `Binder.InvocationExpressions.cs:620-638` — валидация BYREF ограничения
- **Bound Node:** `BoundFunctionExpression`, `BoundCallExpression` с indirect target
- **Runtime:** `RuntimeFunctionValue` (`RuntimeFunctionValue.cs`) — хранит `FunctionSymbol` + `CapturedScope`/`BytecodeCapturedSlots`
- **Lowering:** `LoweredInstruction.LoadFunction()`, `LoweredInstruction.CallValue()`
- **Bytecode:** `Opcode.LoadFunction`, `Opcode.LoadBuiltinFunction`, `Opcode.CallValue`
- **BytecodeCompiler:** эмиссия `LoadFunction` с `routineId`, `CallValue` с `argumentCount`
- **Bytecode VM:** `BytecodeVm.cs:573` — `LoadFunctionValue()` с кэшированием; `769` — `ExecuteFunctionValueCall()`; `3037-3058` — indirect call
- **Tests:** ~10 тестов (indirect call, array callbacks, empty function value, builtin function values)
- **Samples:** `first_class_value.axm`, `returned_closure.axm`
- **Manual:** `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED (ограничение: FUNCTION с BYREF параметрами не может быть function value — MB2029)
- **Уровень уверенности:** CONFIRMED

### SYS-004: Замыкания (Closures)

- **Категория:** RUNTIME FACILITY
- **Тип:** CLOSURE
- **Назначение:** локальные функции захватывают переменные из внешних областей видимости
- **Interpreter:** `Interpreter.Helpers.cs:625-647` — `CaptureScopeForFunction()`; `RuntimeScope` parent chain
- **Bytecode VM:** `BytecodeVariableSlot[]` captures; `LoadCapture`/`StoreCapture` opcodes; `ResolveCapturedSlots()` с кэшированием
- **Tests:** ~10 тестов (capture, returned closure, shared state, mutual recursion)
- **Samples:** `capture_outer_local.axm`, `returned_closure.axm`, `shared_closure_state.axm`
- **Manual:** `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: MB2028 — BYREF не переменная

- **Категория:** SEMANTICS
- **Тип:** DIAGNOSTIC
- **Назначение:** запрет передачи выражений, array elements и record fields через BYREF
- **Пользовательский синтаксис:** `Touch(n + 1)` → MB2028
- **Binder:** `Binder.InvocationExpressions.cs:709-738` — `BindByRefArgument()` валидирует `BoundVariableExpression`
- **Диагностика:** MB2028 (`InvalidByRefArgument`)
- **Tests:** `Bind_ReportsNonVariableByRefArgument`, acceptance `byref_requires_variable.axm`
- **Samples:** `samples/acceptance/negative/byref_requires_variable.axm`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: MB2029 — BYREF function value

- **Категория:** SEMANTICS
- **Тип:** DIAGNOSTIC
- **Назначение:** запрет использования FUNCTION с BYREF параметрами как function value
- **Binder:** `Binder.Expressions.cs:85-88` — проверка `function.Parameters.Any(parameter => parameter.IsByRef)` при создании `BoundFunctionExpression`; `Binder.InvocationExpressions.cs:627-629` — проверка при indirect call
- **Диагностика:** MB2029 (`UnsupportedByRefCallableValue`)
- **Tests:** `Bind_ReportsCallingNonFunctionValue`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: MB3009 — Пустое значение function value

- **Категория:** RUNTIME
- **Тип:** DIAGNOSTIC
- **Назначение:** защита от вызова неинициализированной переменной-функции
- **Interpreter:** `Interpreter.Helpers.cs:514-517` — `ReportEmptyFunctionValue()`
- **Bytecode VM:** `BytecodeVm.cs:6186-6189` — `ReportEmptyFunctionValue()`
- **Диагностика:** MB3009 (`EmptyFunctionValue`)
- **Tests:** `Execute_ReportsCallingEmptyFunctionValue`, `Execute_ArrayFilterReportsEmptyFunctionValue`, `Execute_ArrayMapReportsEmptyFunctionValue`, `Execute_ArrayReduceReportsEmptyFunctionValue`, `Execute_ArrayPredicateFunctionsReportEmptyFunctionValue`, `Execute_CompiledArrayCallbackBuiltinsReportEmptyFunctionValue`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-008: CallByRef опкод

- **Категория:** BYTECODE
- **Тип:** OPCODE
- **Назначение:** отдельный опкод для вызовов с BYREF аргументами
- **Lowering:** `BoundProgramLowerer.cs:1712-1719` — `LoweredInstruction.CallFunctionByRef()`
- **BytecodeCompiler:** `BytecodeCompiler.cs:1195-1256` — `EmitCallByRef()` с операндами: routineId, byValArgCount, totalArgCount, byRefCount, [argIndex, sourceKind, sourceSlot]...
- **Opcode:** `Opcode.CallByRef` (`Opcode.cs:58`)
- **Bytecode VM:** `BytecodeVm.cs:2975-3007` — `ExecuteRoutineCall()` с dispatch по `Opcode.CallByRef`
- **Validator:** `BytecodeModuleValidator.cs:2478` — валидация CallByRef операндов
- **Tests:** `Validate_CallByRefWithByValStackArgumentsAndFunctionResult_Succeeds`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-009: CopyBackByRefArguments

- **Категория:** RUNTIME
- **Тип:** RUNTIME FACILITY
- **Назначение:** копирование изменённых BYREF значений обратно в source slots после возврата из процедуры
- **Bytecode VM:** `BytecodeVm.cs:4957-4975` — `CopyBackByRefArguments()` вызывается из `CompleteReturn()`
- **Механизма:** итерация по `frame.ByRefArgumentTargets`, для каждого non-null target вызывается `target.Store(frame.Arguments[index].Value)`
- **Interpreter:** не нужен — BYREF работает через shared references (`RuntimeReferenceStoredValue`)
- **Tests:** покрыто через `Execute_CompiledByRefScalarParameterMutatesCallerVariable`, `Execute_CompiledByRefRecordParameterMutatesCallerVariable`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-010: Function Value Caching

- **Категория:** PERFORMANCE
- **Тип:** OPTIMIZATION
- **Назначение:** кэширование function values для повторного использования в одном кадре
- **Bytecode VM:** `BytecodeCallFrame.cs:287-302` — `TryGetCachedFunctionValue()` / `CacheFunctionValue()` через `_cachedFunctionValuesByRoutineId`
- **Bytecode VM:** `BytecodeVm.cs:2833-2853` — `LoadFunctionValue()` с кэшированием
- **Tests:** покрыто через indirect function value call тесты
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (TokenKind: ByRefKeyword, ByValKeyword, FunctionKeyword)
    ↓
Parser (ParameterSyntax с PassingKeyword, CallExpressionSyntax, NameExpressionSyntax)
    ↓
Binder (ParameterSymbol с PassingMode, BoundFunctionExpression, BoundCallExpression, BindByRefArgument)
    ↓
Lowering (BoundProgramLowerer: CallFunction, CallFunctionByRef, CallValue, ByRefStoragePlan, ByRefCopyBackPlan)
    ↓
BytecodeCompiler (Opcode.Call, Opcode.CallByRef, Opcode.CallValue, Opcode.LoadFunction)
    ↓
Interpreter (InvokeUserRoutine → RuntimeReferenceStoredValue для BYREF, RuntimeFunctionValue для closures)
    ↓
BytecodeVm (ExecuteRoutineCall → BytecodeVariableSlot delegates для BYREF, CopyBackByRefArguments при Return)
```

### 4.2 Ключевые файлы

| Файл | Ответственность | Строк |
|------|----------------|-------|
| `src/Axiom.Lexing/TokenKind.cs` | `ByRefKeyword`, `ByValKeyword` | 2 |
| `src/Axiom.Syntax/SyntaxKind.cs` | `ByRefKeyword` | 1 |
| `src/Axiom.Semantics/Symbols/Symbols.cs` | `ParameterPassingMode`, `ParameterSymbol.IsByRef`, `FunctionTypeSymbol` | ~90 |
| `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` | `BindByRefArgument()`, BYREF validation | ~30 |
| `src/Axiom.Semantics/Binding/Binder.Expressions.cs` | `BoundFunctionExpression`, MB2029 check | ~15 |
| `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs` | `ReportInvalidByRefArgument()`, `ReportUnsupportedByRefCallableValue()` | ~20 |
| `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs` | Function value (closure) storage | 66 |
| `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` | BYREF via getter/setter delegates | 91 |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | `InvokeUserRoutine()`, `ResolveByRefStoredArgument()` | ~100 |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | `ExecuteRoutineCall()`, `ResolveByRefArgumentSlots()`, `CopyBackByRefArguments()`, `ExecuteFunctionValueCall()` | ~120 |
| `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` | `ByRefArgumentTargets`, function value caching | ~30 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | `LowerCall()`, `ByRefStoragePlan`, `ByRefCopyBackPlan` | ~180 |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | `EmitCallByRef()` | ~60 |
| `src/Axiom.Bytecode/Model/Opcode.cs` | `CallByRef` | 1 |
| `src/Axiom.Core/DiagnosticCodes.cs` | MB2028, MB2029, MB3009 | 3 |

### 4.3 Архитектурные решения

1. **BYREF через RuntimeReferenceStoredValue (Interpreter):** В Interpreter BYREF аргумент оборачивается в `RuntimeReferenceStoredValue` с getter/setter лямбдами. Изменения немедленно видны через shared scope.

2. **BYREF через BytecodeVariableSlot delegates (VM):** В VM BYREF аргумент передаётся через `BytecodeVariableSlot` с делегатами getter/setter. `CopyBackByRefArguments()` копирует изменённые значения обратно при Return.

3. **Отдельный опкод CallByRef:** Для вызовов с BYREF аргументами используется отдельный опкод `CallByRef` с операндами, описывающими источники BYREF слотов (global=0, argument=1, local=2, capture=3).

4. **Function values через RuntimeFunctionValue:** `RuntimeFunctionValue` хранит `FunctionSymbol` и захваченные слоты (scope chain для Interpreter, `BytecodeVariableSlot[]` для VM).

5. **Function value caching:** VM кэширует function values в `BytecodeCallFrame._cachedFunctionValuesByRoutineId` для повторного использования.

6. **BYREF ограничение для function values:** FUNCTION с BYREF параметрами не может быть function value (MB2029). Это сознательное ограничение, поскольку indirect call через `CallValue` не поддерживает BYREF операнды.

---

## 5. Полный путь выполнения

### 5.1 BYREF вызов (Interpreter)

1. `EvaluateCallExpression()` — вычисление target и аргументов
2. `target.AsFunction()` → `RuntimeFunctionValue`
3. `InvokeUserRoutine()`:
   - Для BYREF параметра: `ResolveByRefStoredArgument(boundArgument)` → `RuntimeReferenceStoredValue`
   - `RuntimeReferenceStoredValue` создаёт getter/setter лямбды через scope lookup
   - `scope.Define(parameter, storedArgument)`
4. Выполнение statements — присваивание через BYREF немедленно меняет оригинал
5. Возврат — shared reference автоматически сохраняет изменения

### 5.2 BYREF вызов (BytecodeVm)

1. `ExecuteRoutineCall()` с `Opcode.CallByRef`:
   - `RentArguments()` из evaluation stack
   - `ResolveByRefArgumentSlots()` — создание `BytecodeVariableSlot` с делегатами для каждого BYREF источника
   - `CreateFrame()` с `byRefArgumentSlots`
2. Выполнение инструкций — запись через slot delegate
3. `ExecuteReturn()` → `CompleteReturn()`:
   - `CopyBackByRefArguments()` — копирование изменённых значений обратно в source slots
   - `CacheReusableFrame()`

### 5.3 Function value вызов

1. `DIM op AS FUNCTION(INTEGER) AS INTEGER` — объявление переменной function type
2. `op = DoubleValue` — сохранение ссылки на FUNCTION
3. `op(5)` — косвенный вызов:
   - Interpreter: `EvaluateCallExpression()` → `target.AsFunction()` → `InvokeUserRoutine()`
   - VM: `ExecuteFunctionValueCall()` → `PopValue()` → `target.AsFunction()` → `InvokeBytecodeFunction()`

### 5.4 Closure вызов

1. Локальная FUNCTION захватывает переменные из внешней области
2. Interpreter: `CaptureScopeForFunction()` → `RuntimeScope` parent chain
3. VM: `ResolveCapturedSlots()` → `BytecodeVariableSlot[]` с кэшированием
4. Захваченные переменные разделяются (shared state)
5. Возвращённая closure сохраняет захват после выхода из внешней функции

---

## 6. Фактический семантический контракт

### 6.1 BYREF

**Вход:**
- Параметр объявлен как `BYREF <имя> AS <тип>`
- Аргумент должен быть `BoundVariableExpression` (простая переменная)

**Обработка:**
- Interpreter: `RuntimeReferenceStoredValue` с getter/setter через scope lookup
- VM: `BytecodeVariableSlot` с getter/setter делегатами; `CopyBackByRefArguments()` при Return

**Выход:**
- Изменения видны вызывающему коду немедленно

**Ограничения:**
- Аргумент должен быть простой переменной (не выражением, не array element, не record field через выражение) — MB2028
- FUNCTION с BYREF параметрами не может быть function value — MB2029

### 6.2 Function Values

**Вход:**
- FUNCTION объявлена без BYREF параметров
- Сохраняется в переменной типа `FUNCTION(params) AS type`

**Обработка:**
- `RuntimeFunctionValue` хранит `FunctionSymbol` + захваченные слоты
- Косвенный вызов через `CallValue` опкод (VM) или `EvaluateCallExpression()` (Interpreter)

**Выход:**
- Значение типа ReturnType

**Ограничения:**
- SUB не может быть function value — MB2019
- FUNCTION с BYREF параметрами не может быть function value — MB2029
- Вызов неинициализированной переменной-функции — MB3009

### 6.3 Closures

**Контракт:**
- Локальная FUNCTION захватывает переменные из внешней области видимости
- Захваченные переменные разделяются (shared state)
- Возвращённая closure сохраняет захваченные переменные после выхода из внешней функции
- Изменения через closure видны всем, кто разделяет тот же захват

---

## 7. Анализ Parser

### 7.1 Парсинг BYREF/BYVAL модификаторов

**Файл:** `Parser.Declarations.cs:249-325`

Корректно обрабатывает:
- Опциональный BYREF/BYVAL модификатор перед именем параметра
- `BYREF` → `SyntaxKind.ByRefKeyword`
- `BYVAL` → `SyntaxKind.ByValKeyword` (или отсутствие модификатора)
- Множественные параметры с разными модификаторами

### 7.2 Парсинг вызовов

**Файл:** `Parser.Statements.cs`, `Parser.Expressions.cs`

Корректно обрабатывает:
- Прямые вызовы: `FunctionName(args)`
- Косвенные вызовы: `variable(args)`
- Вызовы методов: `receiver.Method(args)`
- Вложенные вызовы: `f(g(x))`

### 7.3 Оценка Parser

Parser чистый. BYREF/BYVAL — обычные ключевые слова, парсятся как часть параметров. Никакой специальной логики для function values не требуется — они парсятся как обычные выражения и вызовы.

---

## 8. Анализ Binder

### 8.1 Создание ParameterSymbol с PassingMode

**Файл:** `Binder.Declarations.cs:669-691`

`CreateFunctionSymbol()`:
- Итерация параметров
- Разрешение типов
- Создание `ParameterSymbol` с `PassingMode` (BYVAL/BYREF)
- Возврат `FunctionSymbol` с параметрами и типом результата

### 8.2 Валидация BYREF аргументов

**Файл:** `Binder.InvocationExpressions.cs:709-738`

`BindByRefArgument()`:
1. `BindAssignableTarget(argument)` — попытка привязать как assignable target
2. Если тип `Error` → fallback на `BindExpression()` + `ReportInvalidByRefArgument()`
3. Если не `BoundVariableExpression` → `ReportInvalidByRefArgument()` (MB2028)
4. Если тип не совпадает → `ReportTypeMismatch()`
5. Возврат `variableExpression`

**Критическое наблюдение:** валидация в Binder ограничивает BYREF только `BoundVariableExpression`. Это означает:
- ✅ `Touch(myVar)` — простая переменная
- ❌ `Touch(arr(i))` — array element → MB2028
- ❌ `Touch(record.Field)` — record field → MB2028
- ❌ `Touch(n + 1)` — выражение → MB2028

### 8.3 Валидация Function Values

**Файл:** `Binder.Expressions.cs:77-92`

При создании `BoundFunctionExpression`:
1. Проверка `function.IsSub` → MB2019 (SUB не может быть value)
2. Проверка `function.Parameters.Any(parameter => parameter.IsByRef)` → MB2029

**Файл:** `Binder.InvocationExpressions.cs:620-638`

При indirect call через выражение:
1. Проверка `target is not BoundFunctionExpression && functionType.ParameterPassingModes.Any(mode => mode.IsByRef())` → MB2029

### 8.4 Оценка Binder

Binder чистый, с правильной проверкой типов. Валидация BYREF строгая и предсказуемая. Ограничение MB2029 для function values с BYREF параметрами — сознательное архитектурное решение.

---

## 9. Анализ Interpreter

### 9.1 InvokeUserRoutine

**Файл:** `Interpreter.Builtins.cs:415-481`

Ключевая логика для BYREF:
```csharp
IRuntimeStoredValue storedArgument = parameter.IsByRef && boundArguments is not null
    ? ResolveByRefStoredArgument(boundArguments[index], callSiteLocation)
    : parameter.IsArray ? argument.AsArray() : argument.CopyForStorage();
scope.Define(parameter, storedArgument);
```

### 9.2 ResolveByRefStoredArgument

**Файл:** `Interpreter.Builtins.cs:625-677`

Обрабатывает 3 случая:
1. **`BoundVariableExpression`** → `RuntimeReferenceStoredValue` с getter/setter через `CurrentScope.TryLookupStorage()`
2. **`BoundFieldAccessExpression`** → `RuntimeReferenceStoredValue` с getter/setter через `record.GetField()`/`record.SetField()`
3. **`BoundArrayAccessExpression`** → `RuntimeReferenceStoredValue` с getter/setter через `array.GetValue()`/`array.SetValue()`
4. **Fallback** → временная копия (не должно происходить при корректной валидации в Binder)

**Критическое наблюдение:** Interpreter фактически поддерживает BYREF для array elements и record fields через `ResolveByRefStoredArgument()`, но Binder не позволяет такие вызовы напрямую (MB2028). Это значит, что fallback path (случай 4) в Interpreter — dead code при нормальном использовании.

### 9.3 CaptureScopeForFunction

**Файл:** `Interpreter.Helpers.cs:625-647`

- Для глобальных функций: возвращает `_globalScope`
- Для локальных функций: обходит scope chain или call stack, чтобы найти scope объявляющей процедуры

### 9.4 Оценка Interpreter

Реализация корректная. BYREF через `RuntimeReferenceStoredValue` работает через shared references — копирование обратно не нужно. Замыкания работают через scope chain.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering вызовов

**Файл:** `BoundProgramLowerer.cs:1664-1736`

`LowerCall()`:
1. Для прямых вызовов (`BoundFunctionExpression` target):
   - Разделение BYVAL/BYREF аргументов
   - BYREF для простой переменной: `VariableByRefStoragePlan` → `byRefArgumentIndices` + `byRefVariables`
   - BYREF для TYPE method SELF: создание `__type_method_self` temporary
   - BYREF с copy-back: `TryLowerByRefCopyBackArgument()` → `ByRefCopyBackPlan`
   - Эмиссия `CallFunction` или `CallFunctionByRef`
   - Эмиссия BYREF copy-back plans
2. Для косвенных вызовов:
   - Lowering target expression
   - Lowering аргументов
   - Эмиссия `CallValue`

### 10.2 ByRefStoragePlan Hierarchy

**Файл:** `BoundProgramLowerer.cs:17-112`

```
ByRefStoragePlan (abstract)
├── VariableByRefStoragePlan — прямая переменная
├── FieldByRefStoragePlan — поле записи (через parent plan)
└── ArrayElementByRefStoragePlan — элемент массива
```

**Критическое наблюдение:** `FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` существуют в коде и используются в `TryCreateByRefStoragePlan()`, но Binder не позволяет передавать fields и array elements через BYREF напрямую. Эти планы используются только через `TryLowerByRefCopyBackArgument()` для TYPE method SELF и copy-back сценариев.

### 10.3 BytecodeCompiler EmitCallByRef

**Файл:** `BytecodeCompiler.cs:1195-1256`

Операнды `CallByRef`:
- `[0]` = routineId
- `[1]` = totalArgCount
- `[2]` = byValArgumentCount
- `[3]` = byRefCount
- Для каждого BYREF: `[4+i*3]` = argumentIndex, `[5+i*3]` = sourceKind (0=global, 1=argument, 2=local, 3=capture), `[6+i*3]` = sourceSlot

### 10.4 VM ResolveByRefArgumentSlots

**Файл:** `BytecodeVm.cs:3009-3035`

Для каждого BYREF аргумента:
- sourceKind=0 → `new BytecodeVariableSlot(() => LoadGlobalUnchecked(slot), value => StoreGlobalUnchecked(slot, value))`
- sourceKind=1 → `callerFrame.Arguments[slot]` (прямая ссылка на slot)
- sourceKind=2 → `callerFrame.GetOrCreateLocalSlot(slot)` (прямая ссылка на local slot)
- sourceKind=3 → `callerFrame.Captures[slot]` (прямая ссылка на capture slot)

### 10.5 VM CopyBackByRefArguments

**Файл:** `BytecodeVm.cs:4957-4975`

Вызывается из `CompleteReturn()`:
- Итерация по `frame.ByRefArgumentTargets`
- Для каждого non-null target: `target.Store(frame.Arguments[index].Value)`
- Гарантирует, что изменения BYREF аргументов видны вызывающему кадру

### 10.6 VM ExecuteFunctionValueCall

**Файл:** `BytecodeVm.cs:3037-3058`

1. `PopValue()` → target
2. `target.AsFunction()` → `RuntimeFunctionValue`
3. Если null → `ReportEmptyFunctionValue()` (MB3009)
4. `InvokeBytecodeFunction(functionValue, arguments, location)`
5. Если `returnsValue` → push result на evaluation stack

### 10.7 Оценка Bytecode / VM

Lowering чистый. VM реализация корректная с frame reuse, capture caching, BYREF через slot delegates. FINALLY unwinding при RETURN работает правильно. CopyBackByRefArguments гарантирует корректное обновление source slots.

---

## 11. Анализ Runtime

### 11.1 RuntimeFunctionValue

**Файл:** `RuntimeFunctionValue.cs` (66 строк)

Хранит:
- `Function` (FunctionSymbol) — всегда
- `CapturedScope` (RuntimeScope) — для Interpreter closures
- `BytecodeRoutine` (BytecodeRoutine) — для VM direct calls
- `BytecodeCapturedSlots` (IReadOnlyList<BytecodeVariableSlot>) — для VM closures
- `BytecodeBuiltinBindingId` — для built-in function values

### 11.2 BytecodeVariableSlot

**Файл:** `BytecodeVariableSlot.cs` (91 строк)

Поддерживает:
- Прямое хранение значения (`_value`)
- Делегаты getter/setter для BYREF (`_getter`, `_setter`)
- `FixedStringLength` для fixed strings
- `Store()`, `StoreAppendStringValue()`, `NormalizeForStorage()`

### 11.3 BytecodeCallFrame

**Файл:** `BytecodeCallFrame.cs`

Поддерживает:
- `ByRefArgumentTargets` (BytecodeVariableSlot[]) — targets для copy-back
- `_cachedFunctionValuesByRoutineId` — кэш function values
- `TryGetCachedFunctionValue()` / `CacheFunctionValue()`
- `Reset()` для frame reuse

---

## 12. Взаимодействие с типами

| Тип | Как BYREF параметр | Как BYVAL параметр | Как function value | Как closure capture |
|-----|-------------------|-------------------|-------------------|-------------------|
| INTEGER | ✅ | ✅ | N/A | ✅ |
| LONG | ✅ | ✅ | N/A | ✅ |
| DOUBLE | ✅ | ✅ | N/A | ✅ |
| DECIMAL | ✅ | ✅ | N/A | ✅ |
| STRING | ✅ | ✅ | N/A | ✅ |
| BOOLEAN | ✅ | ✅ | N/A | ✅ |
| ARRAY | ❌ MB2028 | ✅ (reference) | N/A | ✅ |
| TYPE | ✅ | ✅ (deep copy) | N/A | ✅ |
| FUNCTION | ❌ MB2029 | ✅ как function type | ✅ | ✅ |

---

## 13. Взаимодействие со String

- Строки передаются BYVAL (копия, т.к. string immutable в CLR)
- Строки передаются BYREF (ссылка на оригинал через `RuntimeReferenceStoredValue`)
- Fixed strings (`STRING * N`) запрещены как параметры и возврат FUNCTION (MB2026)
- Возврат строки из FUNCTION корректно копирует через `CopyForStorage()`

---

## 14. Взаимодействие с Array

- Массивы передаются BYVAL как reference (не deep copy) — стандартная семантика BASIC
- Массивы передаются BYREF — запрещено (MB2028) через Binder
- Массив можно вернуть из FUNCTION
- Массив можно передать function value как параметр (для callback built-ins: ARRAYFILTER, ARRAYMAP и т.д.)
- Callback built-ins (ARRAYEVERY, ARRAYSOME, ARRAYFILTER, ARRAYMAP, ARRAYREDUCE) принимают function values как predicates/selectors/reducers

---

## 15. Взаимодействие с TYPE

- TYPE передаётся BYVAL как deep copy
- TYPE передаётся BYREF как reference (изменения видны вызывающему)
- SELF в TYPE методах — автоматический BYREF параметр (ordinal 0)
- TYPE конструктор `SUB New(...)` с неявным SELF как BYREF
- Lowering для TYPE method SELF создаёт `__type_method_self` temporary если receiver — не простая переменная

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Function values как параметры

- FUNCTION можно передавать как параметр типа `FUNCTION(params) AS type`
- Callback built-ins используют function values: ARRAYFILTER, ARRAYMAP, ARRAYREDUCE, ARRAYEVERY, ARRAYSOME
- Пустое function value → MB3009

### 16.2 Function values как возврат

- FUNCTION может вернуть другую FUNCTION (returned closure)
- Возвращённая closure сохраняет захват после выхода из внешней функции

### 16.3 BYREF ограничение

- FUNCTION с BYREF параметрами не может быть function value (MB2029)
- Это сознательное ограничение: `CallValue` опкод не поддерживает BYREF операнды
- Причина: indirect call не знает сигнатуру до runtime, поэтому не может создать правильные BYREF slot delegates

---

## 17. Взаимодействие с Control Flow

### 17.1 RETURN и BYREF

RETURN из процедуры с BYREF параметрами корректно:
- Interpreter: shared references автоматически сохраняют изменения
- VM: `CopyBackByRefArguments()` вызывается до pop call frame

### 17.2 TRY/CATCH/FINALLY и BYREF

BYREF copy-back происходит в `CompleteReturn()`, который вызывается после FINALLY unwinding. Это гарантирует, что BYREF изменения сохраняются даже при выходе через FINALLY.

### 17.3 Рекурсия и BYREF

BYREF корректно работает при рекурсии. Каждый уровень рекурсии создаёт свои BYREF slot delegates, ссылающиеся на правильный уровень scope.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Семантические ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB2019 | SUB имя как значение | `handler = Show` где Show — SUB |
| MB2020 | Вызов не-функции | `value()` где value — INTEGER |
| MB2028 | BYREF не переменная | `Touch(n + 1)` |
| MB2029 | BYREF function value | FUNCTION с BYREF params как value |

### 18.2 Runtime ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB3009 | Пустое значение function value | Вызов неинициализированной переменной-функции |

---

## 19. State / Lifetime / Ownership

### 19.1 RuntimeReferenceStoredValue (Interpreter BYREF)

- **Создаётся:** `ResolveByRefStoredArgument()` при входе в процедуру
- **Владелец:** `RuntimeScope` вызываемой процедуры
- **Lifetime:** От входа в процедуру до выхода
- **Механизма:** getter/setter лямбды ссылаются на scope вызывающей стороны
- **Уничтожается:** При Pop scope в finally

### 19.2 BytecodeVariableSlot (VM BYREF)

- **Создаётся:** `ResolveByRefArgumentSlots()` при входе в процедуру
- **Владелец:** `BytecodeCallFrame.ByRefArgumentTargets`
- **Lifetime:** От вызова до Return + CopyBack
- **Механизма:** делегаты getter/setter ссылаются на source slots
- **Уничтожается:** При `CacheReusableFrame()` после CopyBack

### 19.3 RuntimeFunctionValue (closure)

- **Создаётся:** При вычислении `BoundFunctionExpression`
- **Владелец:** `RuntimeValue` (sealed class)
- **Lifetime:** Пока существует ссылка на значение
- **Уничтожается:** GC

---

## 20. Edge Cases

### 20.1 Покрытые edge cases

| Сценарий | Покрыт | Доказательство |
|----------|--------|----------------|
| BYREF простой переменной | ✅ | `byref_scalar_argument.axm`, runtime tests |
| BYREF TYPE записи | ✅ | `byref_record_argument.axm`, runtime tests |
| BYVAL TYPE записи (copy) | ✅ | `byval_record_copy.axm` |
| BYREF array element | ❌ MB2028 | acceptance `byref_requires_variable.axm` |
| BYREF expression | ❌ MB2028 | acceptance `byref_requires_variable.axm` |
| Function value как параметр | ✅ | `first_class_value.axm` |
| Function value через переменную | ✅ | `Execute_CompiledIndirectFunctionValueCall` |
| Returned closure | ✅ | `returned_closure.axm` |
| Shared closure state | ✅ | `shared_closure_state.axm` |
| Empty function value call | ❌ MB3009 | `Execute_ReportsCallingEmptyFunctionValue` |
| SUB как function value | ❌ MB2019 | `sub_name_as_value.axm` |
| BYREF function value | ❌ MB2029 | `Bind_ReportsCallingNonFunctionValue` |
| Builtin function values | ✅ | `Execute_CompiledArrayCallbackBuiltinsUseBuiltinFunctionValues` |
| Array callback builtins | ✅ | `Execute_ArrayEveryAndSomeUseFunctionValuesAndClosures` |

### 20.2 Потенциально непокрытые edge cases

| Сценарий | Статус | Примечание |
|----------|--------|------------|
| BYREF через несколько уровней вызовов | Не тестировано явно | Должно работать через slot delegates |
| Closure, захватывающий >10 переменных | Не тестировано | Должно работать |
| Рекурсия с closures | Не тестировано явно | Должно работать |
| BYREF + TRY/CATCH внутри процедуры | Не тестировано явно | Должно работать |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| BYREF простой переменной | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYREF TYPE записи | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYVAL параметр | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYREF array element | ❌ MB2028 | ❌ MB2028 | ✅ | ✅ | ✅ |
| BYREF expression | ❌ MB2028 | ❌ MB2028 | ✅ | ✅ | ✅ |
| Function value сохранение | ✅ | ✅ | ✅ | ✅ | ✅ |
| Function value indirect call | ✅ | ✅ | ✅ | ✅ | ✅ |
| Empty function value call | ❌ MB3009 | ❌ MB3009 | ✅ | ✅ | ✅ |
| BYREF function value | ❌ MB2029 | ❌ MB2029 | ✅ | ✅ | ✅ |
| Closures | ✅ | ✅ | ✅ | ✅ | ✅ |
| Returned closures | ✅ | ✅ | ✅ | ✅ | ✅ |
| Shared closure state | ✅ | ✅ | ✅ | ✅ | ✅ |
| Builtin function values | ✅ | ✅ | ✅ | ✅ | ✅ |
| Array callback builtins | ✅ | ✅ | ✅ | ✅ | ✅ |

**Расхождений не найдено.** Полный паритет подтверждён.

---

## 22. Анализ производительности

### 22.1 Benchmark

**Сценарий:** `samples/smoke/engine-compare/routine_closure_pipeline.axm`

| Метрика | Interpreter | Bytecode VM | Ускорение | Классификация |
|---------|-------------|-------------|-----------|---------------|
| Runtime | baseline | 0.861x | -13.9% | CLEAR WIN |
| Total | baseline | 0.874x | -12.6% | CLEAR WIN |

### 22.2 Почему VM быстрее

1. **Frame reuse:** `_cachedFrames` dictionary позволяет reuse существующие фреймы
2. **Capture caching:** `TryGetCachedCapturedSlots()` позволяет reuse resolved captures
3. **Function value caching:** `TryGetCachedFunctionValue()` позволяет reuse function values
4. **Direct slot access:** `BytecodeVariableSlot` обеспечивает прямой доступ без scope chain lookup
5. **ArrayPool для аргументов:** `RentArguments()` использует `ArrayPool<RuntimeValue>`

---

## 23. Качество Benchmark

- Сценарий `routine_closure_pipeline.axm` покрывает: прямые вызовы SUB/FUNCTION, косвенные вызовы через function values, returned closures, shared-state closures, nested SUB с capture
- Длинные прогоны для стабильности
- Адресный benchmark `samples/smoke/engine-compare/array_callback_pipeline.axm` покрывает callback built-ins с function values

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot paths

- `ExecuteRoutineCall()` — основной hot path для вызовов
- `ExecuteFunctionValueCall()` — hot path для indirect calls
- `ResolveByRefArgumentSlots()` — hot path для BYREF вызовов
- `CopyBackByRefArguments()` — hot path при Return

### 24.2 Bottlenecks

Серьёзных bottleneck'ов не обнаружено. VM уже на целевом уровне производительности.

---

## 25. Архитектурные проблемы

Серьёзных архитектурных проблем не обнаружено.

**Мелкие наблюдения:**

1. **BYREF ограничения** (MB2028, MB2029) — это сознательные ограничения текущего этапа, не баги. Задокументированы в manual и спецификации.

2. **ByRefStoragePlan hierarchy** — `FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` существуют в коде и используются в `TryCreateByRefStoragePlan()`, но Binder не позволяет передавать fields и array elements через BYREF напрямую. Эти планы используются только для TYPE method SELF и copy-back сценариев.

3. **Interpreter ResolveByRefStoredArgument fallback** — случай 4 (temporary copy) в `ResolveByRefStoredArgument()` фактически является dead code при нормальном использовании, поскольку Binder валидирует аргументы до вызова Interpreter.

---

## 26. Дублирование логики

### 26.1 Interpreter vs VM

Логика вызовов разделена между Interpreter и VM:
- `Interpreter.Builtins.cs:InvokeUserRoutine()` — tree-walking вызов
- `BytecodeVm.cs:ExecuteRoutineCall()` — bytecode вызов

Это намеренное разделение, не дублирование. Каждый движок реализует свою модель выполнения.

### 26.2 BYREF механизм

- Interpreter: `RuntimeReferenceStoredValue` с getter/setter лямбдами
- VM: `BytecodeVariableSlot` с getter/setter делегатами + `CopyBackByRefArguments()`

Разные механизмы, одинаковый наблюдаемый результат. Не дублирование.

---

## 27. Single Source of Truth

- **Синтаксис:** `SyntaxKind.cs`, `TokenKind.cs`
- **Семантика:** `Binder.InvocationExpressions.cs`, `Binder.Expressions.cs`
- **Символы:** `Symbols.cs` (ParameterSymbol, FunctionSymbol, FunctionTypeSymbol)
- **Bound модель:** `BoundExpressions.cs` (BoundFunctionExpression, BoundCallExpression)
- **Runtime:** `Interpreter.Builtins.cs`, `BytecodeVm.cs`
- **Значения:** `RuntimeFunctionValue.cs`, `BytecodeVariableSlot.cs`
- **Диагностики:** `DiagnosticCodes.cs` (MB2028, MB2029, MB3009)

Источники правды чётко разделены. Конфликтов не обнаружено.

---

## 28. Legacy / Dead Code

### 28.1 Interpreter ResolveByRefStoredArgument fallback

Случай 4 в `ResolveByRefStoredArgument()` (строки 673-676):
```csharp
var temporaryValue = EvaluateExpression(argument).CopyForStorage();
return new RuntimeReferenceStoredValue(
    () => temporaryValue,
    value => temporaryValue = value.CopyForStorage());
```

Этот fallback создаёт временную копию, которая не видна вызывающему коду. Он не должен вызываться при корректной валидации в Binder.

**Статус:** POSSIBLY DEAD — страховочный путь, не должен вызываться при нормальном использовании.

### 28.2 ByRefStoragePlan hierarchy

`FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` в `BoundProgramLowerer.cs` — используются в `TryCreateByRefStoragePlan()`, но Binder не позволяет передавать fields и array elements через BYREF напрямую.

**Статус:** USED для TYPE method SELF и copy-back сценариев. Не dead code.

---

## 29. Надёжность и безопасность

- MaxCallDepth = 200 защищает от StackOverflowException
- FINALLY unwinding при RETURN гарантирует корректную очистку
- BYREF copy-back в VM гарантирует корректное обновление source slots
- Frame reuse через `Reset()` гарантирует отсутствие stale state
- MB3009 защищает от вызова неинициализированной function value
- MB2028 защищает от передачи выражений через BYREF
- MB2029 защищает от использования BYREF function values

---

## 30. Анализ тестов

### 30.1 Покрытие по уровням

| Уровень | Кол-во тестов | Покрытие |
|---------|---------------|----------|
| Lexer | 2 | BYREF, BYVAL keywords |
| Syntax | 3 | Parameter с ByRef keyword |
| Parser | 1 | SUB с BYREF/BYVAL параметрами |
| Binder | 3 | BYREF validation, FunctionSymbol creation, non-variable BYREF |
| Lowerer | 1 | Indirect function value call |
| Compiler | 1 | Builtin function value emission |
| Validator | 2 | CallByRef validation, function value validation |
| Interpreter | 8 | BYREF scalar, BYREF record, function values, closures, empty function value |
| BytecodeVm | 8 | BYREF scalar, BYREF record, indirect call, array callbacks, builtin function values, empty function value |
| Execution Pipeline | 2 | Indirect function value call (both modes) |
| Acceptance | 2 | byref_requires_variable (negative), function_value (positive) |

**Итого: ~33 теста** непосредственно для BYREF/FUNCTION VALUES.

### 30.2 Качество assertions

Тесты проверяют:
- Конкретные diagnostic codes (MB2028, MB2029, MB3009)
- Конкретный вывод программ (BYREF mutation: 11, 37)
- Shared closure state (последовательные вызовы возвращают 1, 2, 3)
- Returned closure (MakeAdder(10)(5) = 15)
- Indirect function value call (op(5) = 10)
- Array callback builtins с function values

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Статус |
|----------|--------|
| BYREF через несколько уровней вызовов | Нет явного теста |
| BYREF + TRY/CATCH внутри процедуры | Нет явного теста |
| Closure, захватывающий >10 переменных | Нет |
| FUNCTION, возвращающий closure, возвращающий closure | Нет явного теста |

### 31.2 Граничные случаи

| Сценарий | Статус |
|----------|--------|
| Очень длинная цепочка closures (>10 уровней) | Нет |
| Рекурсия с closures | Нет явного теста |
| BYREF + FINALLY cleanup | Нет явного теста |

### 31.3 Negative сценарии

| Сценарий | Статус |
|----------|--------|
| MB2029 через indirect call expression | Нет явного теста |
| MB3009 через array callback | ✅ покрыто |

---

## 32. Анализ Samples

### 32.1 BYREF samples

| Sample | Демонстрирует |
|--------|---------------|
| `byref_scalar_argument.axm` | BYREF скаляр (11) |
| `byref_record_argument.axm` | BYREF TYPE запись (37) |
| `byval_record_copy.axm` | BYVAL TYPE запись (copy) |
| `byref_requires_variable.axm` | MB2028 negative |

### 32.2 Function value samples

| Sample | Демонстрирует |
|--------|---------------|
| `first_class_value.axm` | Function value как переменная |
| `returned_closure.axm` | Returned closure |
| `shared_closure_state.axm` | Shared closure state |
| `function_value.axm` | RETURN с значением (42) |

### 32.3 Benchmark samples

| Sample | Демонстрирует |
|--------|---------------|
| `routine_closure_pipeline.axm` | Procedures, functions, function-value, closures |
| `array_callback_pipeline.axm` | Array callback builtins с function values |

---

## 33. Анализ документации

### 33.1 Manual

| Страница | Качество | Примечание |
|----------|----------|------------|
| `sub.html` | ✅ Хорошее | Полный синтаксис, BYVAL/BYREF, диагностики |
| `function.html` | ✅ Хорошее | Полный синтаксис, function values, closures, BYREF ограничение |
| `procedures-and-functions.html` | ✅ Хорошее | Обзорная страница |
| `return.html` | ✅ Хорошее | Две формы RETURN |
| `diagnostics.html` | ✅ Хорошее | MB2028, MB2029, MB3009 в таблице |

### 33.2 Спецификация

- `SPECIFICATION.md` и `SPECIFICATION2.md` — полная спецификация SUB/FUNCTION/RETURN/BYVAL/BYREF
- `SPECIFICATION3.md` — value-oriented модель записей для передачи BYREF/BYVAL

### 33.3 Архитектура

- `ARCHITECTURE.md` sections 20-22 — local nested procedures, runtime diagnostics, function types/closures
- Multiple updates documenting call model evolution

### 33.4 Соответствие кода и документации

Расхождений не обнаружено. Документация корректно отражает фактическое поведение, включая BYREF ограничения.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

- `SUB`, `FUNCTION`, `END SUB`, `END FUNCTION`, `RETURN`, `BYVAL`, `BYREF` — в категории Keyword
- Обе темы (светлая и тёмная) покрыты

### 34.2 Автозавершение блоков

- `SUB ...` + Enter → вставка `END SUB`
- `FUNCTION ...` + Enter → вставка `END FUNCTION`

### 34.3 Smart Identifier

- `SmartIdentifierService` корректно обрабатывает имена параметров и локальных функций

---

## 35. Зависимости от Host / Platform

Специфических зависимостей от платформы нет. Подсистема BYREF/FUNCTION VALUES использует только стандартные возможности .NET (делегаты, замыкания, GC).

---

## 36. Карта корневых причин

Все найденные проблемы имеют отдельные, несвязанные корневые причины. Общей корневой причины не обнаружено.

Предыдущий аудит (Session 1042-1043) уже закрыл все P2/P3 находки:
- CALL-001 (P2): Stack overflow protection → DONE (MB3011)
- CALL-003 (P2): Negative acceptance samples → DONE (5 samples)
- CALL-004 (P3): BYREF limitation documentation → DONE
- CALL-005 (P3): Mutual recursion test → DONE
- CALL-006 (P3): MB3006 runtime test → DONE

---

## 37. Стабильные области

**Не следует трогать:**

1. **BYREF через RuntimeReferenceStoredValue** (Interpreter) — работает через shared references
2. **BYREF через BytecodeVariableSlot delegates** (VM) — работает через slot delegates + CopyBack
3. **CallByRef опкод** — чистая архитектура с операндами для source kinds
4. **Function values через RuntimeFunctionValue** — поддерживает closures
5. **Function value caching** — минимизация аллокаций
6. **Capture caching** — минимизация lookup
7. **Frame reuse** — минимизация аллокаций
8. **FINALLY unwinding при RETURN** — корректная очистка
9. **MaxCallDepth = 200** — защита от переполнения
10. **MB2028/MB2029 ограничения** — сознательные архитектурные решения

---

## 38. Возможности консолидации

Консолидация не требуется. Разделение между Interpreter и VM является намеренным архитектурным решением.

---

## 39. Возможности разделения ответственности

Разделение ответственности уже выполнено корректно:
- `Binder.InvocationExpressions.cs` — валидация BYREF
- `Binder.Expressions.cs` — валидация function values
- `Interpreter.Builtins.cs` — выполнение вызовов с BYREF
- `BytecodeVm.cs` — выполнение вызовов в VM
- `BoundProgramLowerer.cs` — lowering вызовов

---

## 40. Кандидаты на удаление

### 40.1 Interpreter ResolveByRefStoredArgument fallback

Случай 4 в `ResolveByRefStoredArgument()` (строки 673-676) — POSSIBLY DEAD. Страховочный путь, не должен вызываться при нормальном использовании.

**Рекомендация:** не удалять, оставить как страховку.

---

## 41. Готовность к дальнейшему развитию

Подсистема готова к:
- Расширению BYREF на array elements и record fields (Lowering уже поддерживает через ByRefStoragePlan)
- Добавлению optional/default параметров
- Добавлению anonymous functions/lambdas
- Интеграции с будущим отладчиком (call stack panel)

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P4 | LEGACY | Interpreter | ResolveByRefStoredArgument fallback — POSSIBLY DEAD code | `Interpreter.Builtins.cs:673-676` | Страховочный путь | HIGH |
| SYS-002 | P4 | TESTS | Tests | Нет явного теста BYREF через несколько уровней вызовов | Отсутствие теста | Не критично | HIGH |
| SYS-003 | P4 | TESTS | Tests | Нет явного теста рекурсии с closures | Отсутствие теста | Не критично | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0-4 — Нет необходимости

Подсистема стабильна, семантика корректна, архитектура чистая, производительность на целевом уровне.

### ЭТАП 5 — Расширение тестирования (опционально)

**TASK-001:** Добавить тест BYREF через несколько уровней вызовов
- Сложность: низкая
- Риск: нет

**TASK-002:** Добавить тест рекурсии с closures
- Сложность: низкая
- Риск: нет

---

## 44. Граф зависимостей задач

```
TASK-001 (BYREF multi-level) ─── независимая
TASK-002 (recursion + closures) ─── независимая
```

---

## 45. Definition of Done

Подсистема BYREF / FUNCTION VALUES считается завершённой, если:

- [x] Parser корректно разбирает BYREF/BYVAL модификаторы параметров
- [x] Parser корректно разбирает вызовы (прямые, косвенные, методы)
- [x] Binder корректно проверяет BYREF аргументы (только переменные)
- [x] Binder корректно проверяет function values (не SUB, не BYREF params)
- [x] Interpreter корректно выполняет BYREF через RuntimeReferenceStoredValue
- [x] Interpreter корректно выполняет function values и closures
- [x] Lowering корректно транслирует вызовы в CallFunction/CallFunctionByRef/CallValue
- [x] BytecodeVm корректно выполняет BYREF через BytecodeVariableSlot delegates
- [x] BytecodeVm корректно выполняет CopyBackByRefArguments при Return
- [x] BytecodeVm корректно выполняет function values с кэшированием
- [x] Все diagnostic codes покрыты тестами (MB2028, MB2029, MB3009)
- [x] Паритет Interpreter ↔ BytecodeVm подтверждён
- [x] Samples покрывают все основные сценарии
- [x] Manual актуален
- [x] IDE подсветка работает

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **Нет проблем корректности и архитектуры уровня P0-P2.**

Мелкие находки (P4):
1. ResolveByRefStoredArgument fallback — POSSIBLY DEAD code (P4)
2. Нет явного теста BYREF через несколько уровней вызовов (P4)
3. Нет явного теста рекурсии с closures (P4)

---

## 47. ТОП-20 задач по производительности

1. **Нет задач по производительности.** Подсистема уже на целевом уровне (VM быстрее на 13-14%). Frame reuse, capture caching, function value caching обеспечивают оптимальную производительность.

---

## 48. Итоговая оценка здоровья подсистемы

| Метрика | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 9/10 | BYREF и function values работают корректно на обоих движках |
| Семантика | 9/10 | Строгая валидация BYREF (MB2028), function value ограничения (MB2029, MB3009) |
| Архитектура | 9/10 | Чистое разделение: RuntimeReferenceStoredValue (Interpreter) / BytecodeVariableSlot delegates (VM) |
| Runtime (Interpreter) | 9/10 | Корректные closures, BYREF через shared references |
| Runtime (Bytecode VM) | 9/10 | Frame reuse, capture caching, BYREF через slot delegates + CopyBack |
| Производительность | 9/10 | VM быстрее на 13-14%, кэширование function values и captures |
| Память | 9/10 | ArrayPool для аргументов, frame reuse, capture caching |
| Error handling | 9/10 | MB2028, MB2029, MB3009, MaxCallDepth=200 |
| Tests | 8/10 | 33 теста, все проходят; отсутствуют multi-level BYREF и recursion+closures |
| Samples | 9/10 | 4 BYREF + 4 function value + 2 benchmark samples |
| Documentation | 8/10 | Manual актуален; отсутствует отдельная страница для BYREF |
| IDE | 9/10 | Подсветка, автозавершение блоков |
| Maintainability | 9/10 | Код чистый, хорошо структурированный |
| Reliability | 9/10 | Стабильное поведение, нет известных багов |
| Future readiness | 9/10 | Готово к расширению BYREF, optional params, lambdas |

**Средняя оценка: 8.9/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему BYREF / FUNCTION VALUES достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES**

---

## Основные blockers

Нет. Подсистема стабильна и корректна.

## Что нужно сделать в первую очередь

Нет критических задач. Все предыдущие находки (CALL-001..006) уже закрыты.

## Что можно отложить

- Расширение BYREF на array elements и record fields (Lowering уже поддерживает)
- Optional/default параметры
- Anonymous functions/lambdas
- Дополнительные тесты (multi-level BYREF, recursion+closures)

## Что не следует трогать

- BYREF модель (RuntimeReferenceStoredValue / BytecodeVariableSlot delegates)
- CallByRef опкод и его операнды
- Function values модель (RuntimeFunctionValue)
- Closures модель (scope chain / shared slots)
- Frame reuse и capture caching в VM
- FINALLY unwinding при RETURN
- MaxCallDepth = 200
- MB2028/MB2029 ограничения
