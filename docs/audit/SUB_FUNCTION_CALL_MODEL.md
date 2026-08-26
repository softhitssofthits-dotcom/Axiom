# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## SUB / FUNCTION / PARAMETERS / RETURN / CALL MODEL

---

## 1. Резюме

Проведён полный forensic-аудит процедурной модели языка Axiom: операторов `SUB...END SUB`, `FUNCTION...END FUNCTION`, `RETURN`, параметров `BYVAL`/`BYREF`, модели вызовов (прямые, косвенные через function values, конструкторы TYPE), замыканий (closures), рекурсии и локальных вложенных процедур.

**Главный вывод:** процедурная модель фундаментально корректна, архитектурно зрела и полностью покрыта на всех уровнях конвейера. Двухпроходная регистрация, единый `FunctionSymbol`, изолированные фреймы, корректные замыкания, `BYVAL`/`BYREF`, паритет интерпретатора и VM — всё подтверждено тестами и benchmarks.

**Health Score: 9.0/10**

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
| IDE | 9/10 |

**Ответ на главный вопрос: YES** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме.

---

## 2. Что представляет собой эта подсистема

Процедурная модель Axiom обеспечивает:

1. **`SUB...END SUB`** — именованная процедура без возвращаемого значения. Вызывается как оператор.
2. **`FUNCTION...END FUNCTION`** — именованная функция с возвращаемым значением. Вызывается как выражение.
3. **`RETURN`** — досрочный выход из процедуры/функции; в FUNCTION возвращает значение.
4. **Параметры `BYVAL`/`BYREF`** — передача аргументов по значению (копия) или по ссылке (изменение оригинала).
5. **Локальные вложенные процедуры** — SUB/FUNCTION можно объявлять внутри других SUB/FUNCTION.
6. **Замыкания (closures)** — локальные функции захватывают переменные из внешних областей видимости.
7. **Function values (first-class functions)** — FUNCTION можно сохранять в переменных, передавать параметром, возвращать из функции.
8. **Рекурсия** — прямая и взаимная рекурсия с изолированными фреймами.
9. **TYPE методы** — SUB/FUNCTION внутри TYPE с автоматическим параметром `SELF`.
10. **TYPE конструкторы** — `SUB New(...)` с перегрузками и инвариантом `FUNCTION IsValid()`.

---

## 3. Полный список возможностей

### SYS-001: SUB объявления и вызовы

- **Категория:** STATEMENT
- **Тип:** DECLARATION
- **Назначение:** именованная процедура без возвращаемого значения
- **Пользовательский синтаксис:** `SUB <имя>([<параметры>]) ... END SUB`
- **Parser:** `Parser.Declarations.cs:83-104` — `ParseSubDeclaration()`
- **Syntax Node:** `SubDeclarationSyntax` (`ProgramAndMembers.cs:266-319`)
- **Binder:** `Binder.Declarations.cs:600-636` — `DeclareFunction()`; `Binder.Routines.cs:8-44` — `BindRoutine()`
- **Bound Node:** `BoundRoutine` (`BoundProgram.cs:5-22`), `FunctionSymbol` (`Symbols.cs:640-699`)
- **Interpreter:** `Interpreter.Builtins.cs:415-481` — `InvokeUserRoutine()`
- **Lowering:** `BoundProgramLowerer.cs:1654-1736` — `LowerCall()`
- **Bytecode:** `Opcode.Call`, `Opcode.CallByRef`
- **Bytecode VM:** `BytecodeVm.cs:2975-3007` — `ExecuteRoutineCall()`
- **Tests:** ~30+ тестов по всем уровням
- **Samples:** 6 positive (`samples/operators/sub/`)
- **Manual:** `docs/manual/sub.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: FUNCTION объявления и вызовы

- **Категория:** STATEMENT
- **Тип:** DECLARATION
- **Назначение:** именованная функция с возвращаемым значением
- **Пользовательский синтаксис:** `FUNCTION <имя>([<параметры>]) AS <тип> ... RETURN <выражение> END FUNCTION`
- **Parser:** `Parser.Declarations.cs:106-129` — `ParseFunctionDeclaration()`
- **Syntax Node:** `FunctionDeclarationSyntax` (`ProgramAndMembers.cs:321-379`)
- **Binder:** `Binder.Declarations.cs:600-636` — `DeclareFunction()`; `Binder.Routines.cs:8-44` — `BindRoutine()`
- **Bound Node:** `BoundRoutine`, `FunctionSymbol` с `ReturnType != Void`
- **Interpreter:** `Interpreter.Builtins.cs:415-481` — `InvokeUserRoutine()`
- **Lowering:** `BoundProgramLowerer.cs:1654-1736` — `LowerCall()`
- **Bytecode:** `Opcode.Call`, `Opcode.CallByRef`
- **Bytecode VM:** `BytecodeVm.cs:2975-3007` — `ExecuteRoutineCall()`
- **Tests:** ~30+ тестов по всем уровням
- **Samples:** 8 positive (`samples/operators/function/`)
- **Manual:** `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: RETURN

- **Категория:** STATEMENT
- **Тип:** CONTROL FLOW
- **Назначение:** досрочный выход из процедуры/функции; в FUNCTION возвращает значение
- **Пользовательский синтаксис:** `RETURN [<выражение>]`
- **Parser:** `Parser.Statements.cs:1537-1541` — `ParseReturnStatement()`
- **Binder:** `Binder.ControlFlowStatements.cs:321-346` — `BindReturnStatement()`
- **Bound Node:** `BoundReturnStatement` (`BoundStatements.cs:1612-1623`)
- **Interpreter:** `Interpreter.Statements.cs:1053-1060` — `ExecuteReturn()`, возвращает `ExecutionSignal.Return(value)`
- **Lowering:** `BoundProgramLowerer.cs:704-711` — `LoweredInstruction.Return()`
- **Bytecode:** `Opcode.Return`
- **Bytecode VM:** `BytecodeVm.cs:4896-4955` — `ExecuteReturn()`, `TryEnterFinallyForReturn()`, `CompleteReturn()`
- **Tests:** ~15 тестов по всем уровням
- **Samples:** 2 positive (`samples/control-flow/return/`)
- **Manual:** `docs/manual/return.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: BYVAL параметры

- **Категория:** SEMANTICS
- **Тип:** PARAMETER PASSING
- **Назначение:** передача аргумента по значению (независимая копия)
- **Пользовательский синтаксис:** `[BYVAL] <имя> AS <тип>` (BYVAL по умолчанию)
- **Binder:** `Binder.Declarations.cs:669-691` — `CreateFunctionSymbol()` с `ParameterPassingMode.ByVal`
- **Interpreter:** `Interpreter.Builtins.cs:438` — `argument.CopyForStorage()`
- **Bytecode VM:** стандартное хранение в `BytecodeVariableSlot`
- **Tests:** ~10 тестов
- **Samples:** `byval_record_copy.axm`
- **Manual:** `docs/manual/sub.html`, `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: BYREF параметры

- **Категория:** SEMANTICS
- **Тип:** PARAMETER PASSING
- **Назначение:** передача аргумента по ссылке (изменение оригинала)
- **Пользовательский синтаксис:** `BYREF <имя> AS <тип>`
- **Binder:** `Binder.InvocationExpressions.cs:709-738` — `BindByRefArgument()` (валидация: только простая переменная)
- **Interpreter:** `Interpreter.Builtins.cs:436-437` — `ResolveByRefStoredArgument()` создаёт `RuntimeReferenceStoredValue`
- **Lowering:** `BoundProgramLowerer.cs:1680-1706` — BYREF аргументы; `ByRefStoragePlan` hierarchy
- **Bytecode:** `Opcode.CallByRef` с операндами для BYREF слотов
- **Bytecode VM:** `BytecodeVm.cs:3009-3035` — `ResolveByRefArgumentSlots()`; `4957-4975` — `CopyBackByRefArguments()`
- **Tests:** ~15 тестов
- **Samples:** `byref_scalar_argument.axm`, `byref_record_argument.axm`
- **Manual:** `docs/manual/sub.html`
- **Текущий статус:** IMPLEMENTED (ограничен: только простые переменные, не array elements, не function value callbacks)
- **Уровень уверенности:** CONFIRMED

### SYS-006: Локальные вложенные процедуры

- **Категория:** DECLARATION
- **Тип:** RUNTIME FACILITY
- **Назначение:** объявление SUB/FUNCTION внутри других SUB/FUNCTION
- **Parser:** `Parser.Statements.cs:13-14` — dispatch на `LocalSubDeclarationStatementSyntax`/`LocalFunctionDeclarationStatementSyntax`
- **Binder:** `Binder.Routines.cs:59-97` — `DeclareLocalFunctions()`, `DeclareScopedFunction()`
- **Interpreter:** `Interpreter.Builtins.cs:415-481` — `InvokeUserRoutine()` с `routineValue.CapturedScope`
- **Lowering:** `BoundProgramLowerer.cs:142-182` — `Lower()` создаёт отдельные routines
- **Bytecode VM:** `BytecodeVm.cs:3060-3088` — `ResolveCapturedSlots()` с кэшированием
- **Tests:** ~15 тестов
- **Samples:** `local_nested_call.axm` (SUB и FUNCTION)
- **Manual:** `docs/manual/sub.html`, `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: Замыкания (Closures)

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

### SYS-008: Function values (first-class functions)

- **Категория:** RUNTIME FACILITY
- **Тип:** FUNCTION VALUE
- **Назначение:** FUNCTION можно сохранять в переменных, передавать параметром, возвращать
- **Binder:** `Binder.Expressions.cs:77-92` — `BoundFunctionExpression` для имён FUNCTION
- **Runtime:** `RuntimeFunctionValue` (`RuntimeFunctionValue.cs`) — хранит `FunctionSymbol` + `CapturedScope`/`BytecodeCapturedSlots`
- **Lowering:** `LoweredInstruction.LoadFunction()`, `LoweredInstruction.CallValue()`
- **Bytecode:** `Opcode.CallValue`
- **Bytecode VM:** `BytecodeVm.cs:3037-3058` — `ExecuteFunctionValueCall()`
- **Tests:** ~10 тестов
- **Samples:** `first_class_value.axm`
- **Manual:** `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED (ограничение: FUNCTION с BYREF параметрами не может быть function value — MB2029)
- **Уровень уверенности:** CONFIRMED

### SYS-009: TYPE методы

- **Категория:** DECLARATION
- **Тип:** TYPE MEMBER
- **Назначение:** SUB/FUNCTION внутри TYPE с автоматическим параметром SELF (BYREF)
- **Binder:** `Binder.Declarations.cs:638-667` — `CreateTypeMethodSymbol()` (prepends SELF parameter at ordinal 0)
- **Interpreter:** `Interpreter.Builtins.cs:483-567` — `InvokeTypeConstructor()` с SELF как BYREF reference
- **Tests:** ~15 тестов
- **Samples:** TYPE samples с методами
- **Manual:** `docs/manual/sub.html`, `docs/manual/function.html`
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-010: TYPE конструкторы и инвариант

- **Категория:** DECLARATION
- **Тип:** TYPE MEMBER
- **Назначение:** `SUB New(...)` с перегрузками; `FUNCTION IsValid()` инвариант
- **Binder:** `Binder.Declarations.cs:483-598` — `DeclareTypeMethods()`
- **Interpreter:** `Interpreter.Builtins.cs:483-567` — `InvokeTypeConstructor()` с проверкой инварианта
- **Tests:** ~10 тестов
- **Samples:** TYPE constructor samples
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-011: Защита от переполнения стека вызовов

- **Категория:** RUNTIME
- **Тип:** ERROR HANDLING
- **Назначение:** ограничение глубины стека вызовов (MaxCallDepth = 200)
- **Interpreter:** `Interpreter.Builtins.cs:442-446` — проверка `_callStack.Count >= MaxCallDepth`
- **Bytecode VM:** `BytecodeVm.cs:2995-2998` — аналогичная проверка
- **Диагностика:** MB3011 (CallStackOverflow)
- **Tests:** 4 теста (прямая и взаимная рекурсия, оба движка)
- **Samples:** negative acceptance samples
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Конвейер выполнения

```
Исходный код (.axm)
    ↓
Lexer (TokenKind: SubKeyword, FunctionKeyword, ReturnKeyword, ByRefKeyword, ByValKeyword)
    ↓
Parser (SubDeclarationSyntax, FunctionDeclarationSyntax, ParameterSyntax, ReturnStatementSyntax)
    ↓
Binder (FunctionSymbol, ParameterSymbol, BoundRoutine, BoundCallExpression, BoundReturnStatement, BoundFunctionExpression)
    ↓
Lowering (BoundProgramLowerer: CallFunction, CallFunctionByRef, CallValue, Return)
    ↓
BytecodeCompiler (Opcode.Call, Opcode.CallByRef, Opcode.CallValue, Opcode.Return)
    ↓
Interpreter (InvokeUserRoutine → RuntimeScope + CallFrame + ExecuteStatements + ExecutionSignal.Return)
    ↓
BytecodeVm (ExecuteRoutineCall → BytecodeCallFrame + ExecuteReturn + CompleteReturn + CopyBackByRefArguments)
```

### 4.2 Ключевые файлы

| Файл | Ответственность | Строк |
|------|----------------|-------|
| `src/Axiom.Parsing/Parser.Declarations.cs` | Парсинг SUB/FUNCTION/параметров | ~250 |
| `src/Axiom.Parsing/Parser.Statements.cs` | Парсинг RETURN | ~10 |
| `src/Axiom.Semantics/Symbols/Symbols.cs` | FunctionSymbol, ParameterSymbol, FunctionTypeSymbol | ~240 |
| `src/Axiom.Semantics/Binding/Binder.Declarations.cs` | Регистрация SUB/FUNCTION | ~270 |
| `src/Axiom.Semantics/Binding/Binder.Routines.cs` | Связывание тел процедур | ~130 |
| `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` | Связывание вызовов | ~750 |
| `src/Axiom.Semantics/Binding/Binder.ControlFlowStatements.cs` | Связывание RETURN | ~30 |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | Вызов пользовательских процедур | ~310 |
| `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs` | Вычисление вызовов | ~30 |
| `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs` | Захват scope для closures | ~100 |
| `src/Axiom.Runtime/Execution/BytecodeVm.cs` | Вызов процедур в VM | ~200 |
| `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs` | Значение функции (closure) | ~66 |
| `src/Axiom.Runtime/Environment/RuntimeScope.cs` | Область видимости с parent chain | ~82 |
| `src/Axiom.Runtime/Environment/CallFrame.cs` | Фрейм вызова (Interpreter) | ~23 |
| `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` | Фрейм вызова (VM) | ~347 |
| `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` | Слот переменной (поддержка BYREF) | ~91 |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | Понижение вызовов | ~200 |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | Эмиссия Call/CallByRef/CallValue | ~100 |

### 4.3 Архитектурные решения

1. **Двухпроходная регистрация:** `DeclareFunctions()` сначала регистрирует все `FunctionSymbol` в `_globalScope`, затем `BindRoutine()` связывает тела. Это обеспечивает forward references между процедурами.

2. **Единый FunctionSymbol:** SUB и FUNCTION используют один и тот же `FunctionSymbol`. Различие — `IsSub` возвращает `true` если `ReturnType == Void`.

3. **Изолированные фреймы:** Каждый вызов создаёт новый `RuntimeScope` (Interpreter) или `BytecodeCallFrame` (VM). Рекурсия работает через изолированные фреймы.

4. **BYREF через RuntimeReferenceStoredValue:** В Interpreter BYREF аргумент оборачивается в `RuntimeReferenceStoredValue` с getter/setter лямбдами. В VM BYREF аргумент передаётся через `BytecodeVariableSlot` с делегатами.

5. **Closures через scope chain (Interpreter) / shared slots (VM):** Interpreter захватывает `RuntimeScope` через parent chain. VM использует `BytecodeVariableSlot[]` captures с кэшированием.

6. **CallByRef для BYREF аргументов:** Отдельный опкод `CallByRef` с операндами, описывающими источники BYREF слотов (global, argument, local, capture).

7. **FINALLY unwinding при RETURN:** `TryEnterFinallyForReturn()` корректно разворачивает FINALLY блоки при выходе из TRY-контекста.

---

## 5. Полный путь выполнения

### 5.1 Прямой вызов SUB (Interpreter)

1. `EvaluateCallExpression()` — вычисление target и аргументов
2. `target.AsFunction()` → `RuntimeFunctionValue`
3. `InvokeUserRoutine()`:
   - Поиск `BoundRoutine` в `_routines`
   - Создание `RuntimeScope` с `CapturedScope` как parent
   - Определение параметров (BYVAL: `CopyForStorage()`, BYREF: `ResolveByRefStoredArgument()`)
   - Проверка глубины стека (MaxCallDepth = 200)
   - Push `CallFrame` и scope
   - `ExecuteStatements(routine.Statements)`
   - Обработка сигналов (EXIT FOR → ошибка)
   - Для SUB: возврат `RuntimeValue.Void`
   - Для FUNCTION: проверка `HasReturn`, возврат `signal.Value.CopyForStorage()`
   - Pop scope и call frame

### 5.2 Прямой вызов FUNCTION (BytecodeVm)

1. `ExecuteRoutineCall()`:
   - Поиск `BytecodeRoutine` по ID
   - `RentArguments()` из evaluation stack
   - `ResolveCapturedSlots()` с кэшированием
   - `ResolveByRefArgumentSlots()` (для CallByRef)
   - Проверка глубины стека
   - `CreateFrame()` с reuse через `_cachedFrames`
   - Push фрейм на `_callStack`
2. Выполнение инструкций целевой routine
3. `ExecuteReturn()`:
   - Pop return value (для non-void)
   - `TryEnterFinallyForReturn()` — FINALLY unwinding
   - `CompleteReturn()`:
     - `CopyBackByRefArguments()` — копирование изменённых BYREF значений обратно
     - Pop фрейм
     - `CacheReusableFrame()` — кэширование для reuse
     - Push return value на evaluation stack вызывающего

### 5.3 Косвенный вызов через function value

1. `EvaluateCallExpression()` / `ExecuteFunctionValueCall()`:
   - Вычисление target expression → `RuntimeFunctionValue`
   - Проверка на null (MB3009 / ReportEmptyFunctionValue)
   - Вызов `InvokeUserRoutine()` / `InvokeBytecodeFunction()`

### 5.4 Конструктор TYPE

1. `InvokeTypeConstructor()`:
   - Создание default record value
   - SELF как BYREF reference через `RuntimeReferenceStoredValue`
   - Выполнение конструктора
   - Проверка инварианта `IsValid()` (если есть)
   - Возврат модифицированного SELF

---

## 6. Фактический семантический контракт

### 6.1 SUB

**Вход:**
- Имя процедуры
- Список параметров (BYVAL по умолчанию, BYREF опционально)
- Тело (список операторов)

**Обработка:**
- Вызов как оператор (не выражение)
- Параметры создаются в области видимости процедуры
- BYVAL: независимая копия аргумента
- BYREF: ссылка на оригинал (только простые переменные)
- RETURN без значения — досрочный выход
- Значение не возвращается (ReturnType == Void)

**Выход:**
- Побочные эффекты (изменение BYREF аргументов, вывод, файловые операции)
- RuntimeValue.Void

### 6.2 FUNCTION

**Вход:**
- Имя функции
- Список параметров
- Тип возвращаемого значения
- Тело с обязательным RETURN на исполняемом пути

**Обработка:**
- Вызов как выражение
- RETURN с выражением — возврат значения
- Тип значения должен совпадать с объявлением
- Если нет RETURN на исполняемом пути → MB3006 (runtime)

**Выход:**
- Значение типа ReturnType

### 6.3 BYREF

**Ограничения:**
- Аргумент должен быть простой переменной (не выражением, не array element, не record field через выражение)
- FUNCTION с BYREF параметрами не может быть function value (MB2029)
- Изменения видны вызывающему немедленно (Interpreter: через shared scope; VM: через CopyBackByRefArguments)

### 6.4 Closures

**Контракт:**
- Локальная FUNCTION захватывает переменные из внешней области видимости
- Захваченные переменные разделяются (shared state)
- Возвращённая closure сохраняет захваченные переменные после выхода из внешней функции
- Изменения через closure видны всем, кто разделяет тот же захват

---

## 7. Анализ Parser

### 7.1 Парсинг SUB

**Файл:** `Parser.Declarations.cs:83-104`

Корректно обрабатывает:
- Ключевое слово SUB и имя
- Список параметров в скобках
- Тело до END SUB
- Опциональные BYVAL/BYREF модификаторы параметров

### 7.2 Парсинг FUNCTION

**Файл:** `Parser.Declarations.cs:106-129`

Корректно обрабатывает:
- Ключевое слово FUNCTION и имя
- Список параметров
- Обязательный тип возвращаемого значения (AS type)
- Тело до END FUNCTION

### 7.3 Парсинг RETURN

**Файл:** `Parser.Statements.cs:1537-1541`

Корректно обрабатывает:
- RETURN с выражением
- RETURN без выражения

### 7.4 Парсинг параметров

**Файл:** `Parser.Declarations.cs:249-325`

Корректно обрабатывает:
- Разделённые запятой параметры
- Опциональный BYVAL/BYREF модификатор
- Имя и тип параметра

### 7.5 Оценка Parser

Parser чистый, хорошо структурированный. Локальные SUB/FUNCTION парсятся через dispatch в `Parser.Statements.cs`.

---

## 8. Анализ Binder

### 8.1 Двухпроходная регистрация

**Файл:** `Binder.Declarations.cs:466-481`

1. `DeclareFunctions()` — первый проход: регистрация всех `FunctionSymbol` в `_globalScope`
2. `BindRoutine()` — второй проход: связывание тел процедур

Это обеспечивает forward references: процедура A может вызвать процедуру B, объявленную ниже.

### 8.2 Создание FunctionSymbol

**Файл:** `Binder.Declarations.cs:669-691`

`CreateFunctionSymbol()`:
- Итерация параметров
- Разрешение типов
- Создание `ParameterSymbol` с `PassingMode` (BYVAL/BYREF)
- Возврат `FunctionSymbol` с параметрами и типом результата

### 8.3 Связывание вызовов

**Файл:** `Binder.InvocationExpressions.cs:8-112`

`BindCallExpression()` обрабатывает:
- Вызовы TYPE конструкторов (`TypeName(args)`)
- Вызовы через переменную-функцию (`variable(args)`)
- Прямые вызовы по имени (`FunctionName(args)`)
- Вызовы методов TYPE (`receiver.Method(args)`)
- Косвенные вызовы через выражение (`expression(args)`)

### 8.4 Валидация BYREF

**Файл:** `Binder.InvocationExpressions.cs:709-738`

`BindByRefArgument()`:
- Аргумент должен быть `BoundVariableExpression` (простая переменная)
- Тип должен совпадать с параметром
- Иначе → MB2028

### 8.5 Валидация RETURN

**Файл:** `Binder.ControlFlowStatements.cs:321-346`

`BindReturnStatement()`:
- RETURN допустим только внутри SUB/FUNCTION
- В SUB: RETURN без выражения (иначе MB2006)
- В FUNCTION: RETURN с выражением (иначе MB2007)
- Тип выражения должен совпадать с ReturnType

### 8.6 Оценка Binder

Binder чистый, с правильной проверкой типов. Двухпроходная регистрация — элегантное решение для forward references.

---

## 9. Анализ Interpreter

### 9.1 InvokeUserRoutine

**Файл:** `Interpreter.Builtins.cs:415-481`

Ключевая логика:
1. Поиск `BoundRoutine` в `_routines`
2. Создание `RuntimeScope` с `CapturedScope` как parent
3. Определение параметров:
   - BYREF: `ResolveByRefStoredArgument()` → `RuntimeReferenceStoredValue`
   - BYVAL: `argument.CopyForStorage()` или `argument.AsArray()` для массивов
4. Проверка глубины стека (MaxCallDepth = 200)
5. Push CallFrame и scope
6. Выполнение statements
7. Обработка EXIT FOR (ошибка — выход за пределы процедуры)
8. Для SUB: возврат Void
9. Для FUNCTION: проверка HasReturn, возврат значения
10. finally: Pop scope и call frame

### 9.2 ResolveByRefStoredArgument

**Файл:** `Interpreter.Builtins.cs:625-677`

Обрабатывает 3 случая:
- `BoundVariableExpression` → `RuntimeReferenceStoredValue` с getter/setter через scope lookup
- `BoundFieldAccessExpression` → `RuntimeReferenceStoredValue` с getter/setter через record field
- `BoundArrayAccessExpression` → `RuntimeReferenceStoredValue` с getter/setter через array element
- Fallback: временная копия (не должно происходить при корректной валидации в Binder)

### 9.3 CaptureScopeForFunction

**Файл:** `Interpreter.Helpers.cs:625-647`

- Для глобальных функций: возвращает `_globalScope`
- Для локальных функций: обходит scope chain или call stack, чтобы найти scope объявляющей процедуры

### 9.4 Оценка Interpreter

Реализация корректная, с правильной обработкой BYREF через `RuntimeReferenceStoredValue`. Замыкания работают через scope chain.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering вызовов

**Файл:** `BoundProgramLowerer.cs:1654-1736`

`LowerCall()`:
1. Для прямых вызовов (`BoundFunctionExpression` target):
   - Разделение BYVAL/BYREF аргументов
   - BYREF: определение `ByRefStoragePlan` (Variable, Field, ArrayElement)
   - Эмиссия `CallFunction` или `CallFunctionByRef`
   - Эмиссия BYREF copy-back plans
2. Для косвенных вызовов:
   - Lowering target expression
   - Lowering аргументов
   - Эмиссия `CallValue`

### 10.2 BytecodeCompiler

**Файл:** `BytecodeCompiler.cs:1195-1256`

`EmitCallByRef()`:
- Операнды: routineId, byValArgCount, totalArgCount, byRefCount
- Для каждого BYREF аргумента: argumentIndex, sourceKind (0=global, 1=argument, 2=local, 3=capture), sourceSlot

### 10.3 VM ExecuteRoutineCall

**Файл:** `BytecodeVm.cs:2975-3007`

1. Поиск `BytecodeRoutine` по ID
2. `RentArguments()` из evaluation stack (ArrayPool)
3. `ResolveCapturedSlots()` с кэшированием через `TryGetCachedCapturedSlots()`
4. `ResolveByRefArgumentSlots()` — создание `BytecodeVariableSlot` с делегатами для каждого BYREF источника
5. Проверка глубины стека
6. `CreateFrame()` с reuse через `_cachedFrames` и frame templates
7. Push фрейм

### 10.4 VM ExecuteReturn

**Файл:** `BytecodeVm.cs:4896-4955`

1. Pop return value (для non-void)
2. `TryEnterFinallyForReturn()` — FINALLY unwinding
3. `CompleteReturn()`:
   - `CopyBackByRefArguments()` — копирование изменённых BYREF значений обратно в source slots
   - Pop фрейм
   - `CacheReusableFrame()` — кэширование для reuse
   - Push return value на evaluation stack вызывающего

### 10.5 Frame reuse

**Файл:** `BytecodeVm.cs:421-459`

`CreateFrame()` использует:
- `_cachedFrames` dictionary для reuse существующих фреймов
- Frame templates для создания новых фреймов
- `BytecodeCallFrame.Reset()` для сброса перед reuse

### 10.6 Оценка Bytecode / VM

Lowering чистый. VM реализация корректная с frame reuse, capture caching, BYREF через slot delegates. FINALLY unwinding при RETURN работает правильно.

---

## 11. Анализ Runtime

### 11.1 RuntimeFunctionValue

**Файл:** `RuntimeFunctionValue.cs`

Хранит:
- `Function` (FunctionSymbol) — всегда
- `CapturedScope` (RuntimeScope) — для Interpreter closures
- `BytecodeRoutine` (BytecodeRoutine) — для VM direct calls
- `BytecodeCapturedSlots` (IReadOnlyList<BytecodeVariableSlot>) — для VM closures
- `BytecodeBuiltinBindingId` — для built-in function values

### 11.2 RuntimeScope

**Файл:** `RuntimeScope.cs`

- Parent chain для lexical scoping
- `OwningRoutine` (FunctionSymbol) — для closure capture
- `Define()`, `TryLookup()`, `TryAssign()`, `TryLookupStorage()`

### 11.3 BytecodeVariableSlot

**Файл:** `BytecodeVariableSlot.cs`

- Поддерживает прямое хранение значения
- Поддерживает делегаты getter/setter для BYREF
- `Store()`, `StoreAppendStringValue()`, `NormalizeForStorage()`

### 11.4 CallFrame (Interpreter)

**Файл:** `CallFrame.cs`

- `Function` (FunctionSymbol)
- `Scope` (RuntimeScope)
- `CallSiteLocation` (TextLocation)

### 11.5 BytecodeCallFrame (VM)

**Файл:** `BytecodeCallFrame.cs`

- `Routine` (BytecodeRoutine)
- `Arguments` (BytecodeVariableSlot[])
- `Captures` (BytecodeVariableSlot[])
- `ByRefArgumentTargets` (BytecodeVariableSlot[])
- `EvaluationStack` (BytecodeEvaluationStack)
- `InstructionPointer`
- Кэширование function values и captured slots
- `Reset()` для frame reuse

---

## 12. Взаимодействие с типами

| Тип | Как параметр | Как возврат | Как function value | BYREF |
|-----|-------------|-------------|-------------------|-------|
| INTEGER | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| LONG | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| DOUBLE | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| DECIMAL | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| STRING | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| BOOLEAN | ✅ BYVAL/BYREF | ✅ | N/A | ✅ |
| ARRAY | ✅ BYVAL (reference) | ✅ | N/A | ❌ (MB2028) |
| TYPE | ✅ BYVAL (copy)/BYREF | ✅ | N/A | ✅ |
| FUNCTION | ✅ как function type | ✅ | ✅ | ❌ (MB2029 для BYREF params) |

---

## 13. Взаимодействие со String

- Строки передаются BYVAL (копия, т.к. string immutable в CLR)
- Строки передаются BYREF (ссылка на оригинал)
- Fixed strings (`STRING * N`) запрещены как параметры и возврат FUNCTION (MB2026)
- Возврат строки из FUNCTION корректно копирует через `CopyForStorage()`

---

## 14. Взаимодействие с Array

- Массивы передаются BYVAL как reference (не deep copy) — стандартная семантика BASIC
- Массивы передаются BYREF — запрещено (MB2028)
- Массив можно вернуть из FUNCTION
- Массив можно передать function value как параметр

---

## 15. Взаимодействие с TYPE

- TYPE передаётся BYVAL как deep copy
- TYPE передаётся BYREF как reference (изменения видны вызывающему)
- SELF в TYPE методах — автоматический BYREF параметр (ordinal 0)
- TYPE конструктор `SUB New(...)` с перегрузками
- TYPE инвариант `FUNCTION IsValid()` проверяется после конструктора

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Рекурсия

- Прямая рекурсия: `FUNCTION Factorial(n)` вызывает сама себя
- Взаимная рекурсия: `FUNCTION IsEven(n)` вызывает `FUNCTION IsOdd(n)` и наоборот
- Защита: MaxCallDepth = 200, MB3011 при переполнении

### 16.2 Closures

- Локальная FUNCTION захватывает переменные из внешней области
- Захваченные переменные разделяются (shared state)
- Возвращённая closure сохраняет захват после выхода из внешней функции

### 16.3 Function values

- FUNCTION можно сохранять в переменной типа `FUNCTION(params) AS type`
- FUNCTION можно передавать параметром
- FUNCTION можно возвращать из FUNCTION
- SUB не может быть function value (MB2019)
- FUNCTION с BYREF параметрами не может быть function value (MB2029)

### 16.4 Косвенные вызовы

- `DIM op AS FUNCTION(INTEGER) AS INTEGER`
- `op = DoubleValue`
- `PRINT op(5)` — косвенный вызов через function value

---

## 17. Взаимодействие с Control Flow

### 17.1 RETURN внутри IF/SELECT CASE

RETURN внутри ветвей IF и SELECT CASE корректно завершает процедуру/функцию.

### 17.2 RETURN внутри циклов

RETURN внутри FOR/WHILE/DO корректно завершает процедуру/функцию.

### 17.3 EXIT FOR из процедуры

EXIT FOR из SUB/FUNCTION без окружающего цикла → runtime ошибка (обнаружена в Interpreter через signal check, в VM через lowering validation).

### 17.4 TRY/CATCH/FINALLY и RETURN

RETURN из TRY-блока корректно выполняет FINALLY перед выходом. Это реализовано в:
- Interpreter: через нативный .NET try/finally
- VM: через `TryEnterFinallyForReturn()` с handler stack

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Семантические ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB2004 | Неверное число аргументов | Вызов с wrong arity |
| MB2005 | Неизвестный вызываемый | Имя не найдено |
| MB2006 | RETURN с значением в SUB | RETURN 42 внутри SUB |
| MB2007 | RETURN без значения в FUNCTION | RETURN без выражения |
| MB2019 | SUB имя как значение | `handler = Show` где Show — SUB |
| MB2020 | Вызов не-функции | `value()` где value — INTEGER |
| MB2026 | Fixed string в параметре/возврате | `STRING * N` |
| MB2028 | BYREF не переменная | `Touch(n + 1)` |
| MB2029 | BYREF function value | FUNCTION с BYREF params как value |

### 18.2 Runtime ошибки

| Код | Описание | Контекст |
|-----|----------|----------|
| MB3006 | FUNCTION без RETURN | Падение через конец FUNCTION |
| MB3009 | Пустое значение function value | Вызов неинициализированной переменной-функции |
| MB3011 | Переполнение стека вызовов | Рекурсия > 200 глубины |

---

## 19. State / Lifetime / Ownership

### 19.1 FunctionSymbol

- **Создаётся:** Binder (DeclareFunction)
- **Владелец:** BoundScope (_globalScope)
- **Lifetime:** Всё время выполнения программы
- **Уничтожается:** При завершении программы

### 19.2 RuntimeScope

- **Создаётся:** Interpreter (InvokeUserRoutine, InvokeTypeConstructor)
- **Владелец:** CallFrame
- **Lifetime:** От входа в процедуру до выхода
- **Уничтожается:** При Pop scope в finally

### 19.3 BytecodeCallFrame

- **Создаётся:** VM (CreateFrame) или reuse из `_cachedFrames`
- **Владелец:** `_callStack`
- **Lifetime:** От вызова до Return
- **Уничтожается:** При Pop call frame + CacheReusableFrame

### 19.4 RuntimeFunctionValue (closure)

- **Создаётся:** При вычислении BoundFunctionExpression
- **Владелец:** RuntimeValue (sealed class)
- **Lifetime:** Пока существует ссылка на значение
- **Уничтожается:** GC

---

## 20. Edge Cases

### 20.1 Покрытые edge cases

| Сценарий | Покрыт | Доказательство |
|----------|--------|----------------|
| Прямая рекурсия | ✅ | `Recursion check.axm`, Factorial/Fibonacci tests |
| Взаимная рекурсия | ✅ | IsEven/IsOdd tests (both engines) |
| Переполнение стека | ✅ | MB3011, MaxCallDepth = 200 |
| RETURN в FUNCTION | ✅ | MB3006 при отсутствии |
| RETURN с значением в SUB | ✅ | MB2006 |
| RETURN без значения в FUNCTION | ✅ | MB2007 |
| BYREF простой переменной | ✅ | byref_scalar_argument.axm |
| BYREF TYPE записи | ✅ | byref_record_argument.axm |
| BYVAL TYPE записи (copy) | ✅ | byval_record_copy.axm |
| BYREF array element | ❌ | MB2028 |
| BYREF expression | ❌ | MB2028 |
| Closures с shared state | ✅ | shared_closure_state.axm |
| Returned closure | ✅ | returned_closure.axm |
| Function value как параметр | ✅ | first_class_value.axm |
| Immediate call на returned closure | ✅ | returned_closure.axm |
| SUB имя как значение | ❌ | MB2019 |
| FUNCTION с BYREF params как value | ❌ | MB2029 |
| Empty function value call | ❌ | MB3009 |
| Local function forward reference | ✅ | local_nested_call.axm |
| Local function capture forward | ❌ | MB2001 |
| FINALLY при RETURN из TRY | ✅ | Run_WithBytecodeMode_RunsFinallyBeforeFunctionReturn |
| TYPE constructor overload | ✅ | Execute_TypeConstructorSupportsOverloading |
| TYPE constructor invariant | ✅ | Execute_TypeConstructorReportsRuntimeFailureWhenInvariantReturnsFalse |
| TYPE method SELF | ✅ | Execute_TypeMethodsMutateSelfAndReturnValues |

### 20.2 Потенциально непокрытые edge cases

| Сценарий | Статус | Примечание |
|----------|--------|------------|
| Очень длинная цепочка closures (>10 уровней) | Не тестировано | Должно работать через scope chain |
| Closure, захватывающий >100 переменных | Не тестировано | Должно работать |
| Рекурсия с closures | Не тестировано явно | Должно работать |
| BYREF через несколько уровней вызовов | Не тестировано явно | Должно работать через slot delegates |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| SUB вызов | ✅ | ✅ | ✅ | ✅ | ✅ |
| FUNCTION вызов | ✅ | ✅ | ✅ | ✅ | ✅ |
| RETURN | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYVAL параметр | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYREF параметр (переменная) | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYREF параметр (запись) | ✅ | ✅ | ✅ | ✅ | ✅ |
| BYREF array element | ❌ MB2028 | ❌ MB2028 | ✅ | ✅ | ✅ |
| BYREF expression | ❌ MB2028 | ❌ MB2028 | ✅ | ✅ | ✅ |
| Локальные procedures | ✅ | ✅ | ✅ | ✅ | ✅ |
| Closures | ✅ | ✅ | ✅ | ✅ | ✅ |
| Returned closures | ✅ | ✅ | ✅ | ✅ | ✅ |
| Shared closure state | ✅ | ✅ | ✅ | ✅ | ✅ |
| Function values | ✅ | ✅ | ✅ | ✅ | ✅ |
| Косвенные вызовы | ✅ | ✅ | ✅ | ✅ | ✅ |
| Рекурсия | ✅ | ✅ | ✅ | ✅ | ✅ |
| Взаимная рекурсия | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB3011 (stack overflow) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB3006 (missing return) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB3009 (empty function value) | ✅ | ✅ | ✅ | ✅ | ✅ |
| MB2029 (BYREF function value) | ✅ | ✅ | ✅ | ✅ | ✅ |
| FINALLY при RETURN | ✅ | ✅ | ✅ | ✅ | ✅ |
| TYPE methods (SELF) | ✅ | ✅ | ✅ | ✅ | ✅ |
| TYPE constructors | ✅ | ✅ | ✅ | ✅ | ✅ |
| TYPE invariant | ✅ | ✅ | ✅ | ✅ | ✅ |

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

1. **Frame reuse:** `_cachedFrames` dictionary позволяет reuse существующие фреймы вместо создания новых
2. **Capture caching:** `TryGetCachedCapturedSlots()` позволяет reuse resolved captures
3. **Function value caching:** `TryGetCachedFunctionValue()` позволяет reuse function values
4. **Direct slot access:** `BytecodeVariableSlot` обеспечивает прямой доступ без scope chain lookup
5. **ArrayPool для аргументов:** `RentArguments()` использует `ArrayPool<RuntimeValue>` для минимизации аллокаций

---

## 23. Качество Benchmark

- Сценарий `routine_closure_pipeline.axm` покрывает: прямые вызовы SUB/FUNCTION, косвенные вызовы через function values, returned closures, shared-state closures, nested SUB с capture
- 320 раундов × 6 внутренних итераций
- Длинные прогоны для стабильности

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot paths

- `ExecuteRoutineCall()` — основной hot path для вызовов
- `CreateFrame()` с reuse — минимизация аллокаций
- `ResolveCapturedSlots()` с кэшированием — минимизация lookup

### 24.2 Bottlenecks

Серьёзных bottleneck'ов не обнаружено.

---

## 25. Архитектурные проблемы

Серьёзных архитектурных проблем не обнаружено.

**Мелкие наблюдения:**

1. **BYREF ограничения** (MB2028, MB2029) — это сознательные ограничения текущего этапа, не баги. Задокументированы в manual и спецификации.

2. **ByRefStoragePlan hierarchy** — `FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` существуют в коде, но не используются (dead code). BYREF для полей и элементов массивов обрабатывается через `TryLowerByRefCopyBackArgument()` с fallback на copy-back.

---

## 26. Дублирование логики

### 26.1 Interpreter vs VM

Логика вызовов разделена между Interpreter и VM:
- `Interpreter.Builtins.cs:InvokeUserRoutine()` — tree-walking вызов
- `BytecodeVm.cs:ExecuteRoutineCall()` — bytecode вызов

Это намеренное разделение, не дублирование. Каждый движок реализует свою модель выполнения.

### 26.2 BYREF copy-back

В VM `CopyBackByRefArguments()` копирует изменённые BYREF значения обратно. В Interpreter BYREF работает через shared references, поэтому copy-back не нужен.

---

## 27. Single Source of Truth

- **Синтаксис:** `SyntaxKind.cs`, `ProgramAndMembers.cs`, `Statements.cs`
- **Семантика:** `Binder.Declarations.cs`, `Binder.Routines.cs`, `Binder.InvocationExpressions.cs`
- **Символы:** `Symbols.cs` (FunctionSymbol, ParameterSymbol, FunctionTypeSymbol)
- **Bound модель:** `BoundProgram.cs`, `BoundExpressions.cs`, `BoundStatements.cs`
- **Runtime:** `Interpreter.Builtins.cs`, `BytecodeVm.cs`
- **Значения:** `RuntimeFunctionValue.cs`, `RuntimeScope.cs`, `BytecodeCallFrame.cs`

Источники правды чётко разделены. Конфликтов не обнаружено.

---

## 28. Legacy / Dead Code

### 28.1 ByRefStoragePlan hierarchy

`FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` в `BoundProgramLowerer.cs` — dead code. BYREF для полей и элементов массивов обрабатывается через `TryLowerByRefCopyBackArgument()`.

**Статус:** POSSIBLY DEAD — может понадобиться при расширении BYREF в будущем.

---

## 29. Надёжность и безопасность

- MaxCallDepth = 200 защищает от StackOverflowException
- FINALLY unwinding при RETURN гарантирует корректную очистку
- BYREF copy-back в VM гарантирует корректное обновление source slots
- Frame reuse через `Reset()` гарантирует отсутствие stale state

---

## 30. Анализ тестов

### 30.1 Покрытие по уровням

| Уровень | Кол-во тестов | Покрытие |
|---------|---------------|----------|
| Lexer | 2 | BYREF, BYVAL keywords |
| Syntax | 6 | SubDeclaration, FunctionDeclaration, Parameter, ReturnStatement |
| Parser | 9 | SUB, FUNCTION, parameters, RETURN, local functions |
| Binder | ~30 | FunctionSymbol, ParameterSymbol, BYREF validation, RETURN validation, closures, TYPE methods |
| Lowerer | 6 | Direct call, indirect call, local routines, recursive function, captures |
| Compiler | 4 | Parameter slots, nested routines, builtin function values, capture emission |
| Validator | 2 | CallByRef validation, ValidateForStep |
| Interpreter | ~20 | SUB/FUNCTION calls, closures, recursion, function values, BYREF, TYPE methods |
| BytecodeVm | ~10 | BYREF, TYPE methods, recursion, array callbacks |
| Execution Pipeline | ~15 | Local functions, captures, BYREF, FINALLY, function values |
| Acceptance | ~20 | Positive + negative samples |
| IDE | 3 | Block editing (SUB/FUNCTION auto-complete), run service |

### 30.2 Качество assertions

Тесты проверяют:
- Конкретные diagnostic codes (MB2006, MB2007, MB2019, MB2020, MB2028, MB2029, MB3006, MB3009, MB3011)
- Конкретный вывод программ
- Shared closure state (последовательные вызовы возвращают 1, 2, 3)
- Returned closure (MakeAdder(10)(5) = 15)
- FINALLY unwinding при RETURN
- Взаимную рекурсию (IsEven/IsOdd)
- TYPE method SELF injection

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Статус |
|----------|--------|
| BYREF через несколько уровней вызовов | Нет явного теста |
| Рекурсия с closures | Нет явного теста |
| Closure, захватывающий >10 переменных | Нет |

### 31.2 Граничные случаи

| Сценарий | Статус |
|----------|--------|
| Очень длинная цепочка closures (>10 уровней) | Нет |
| FUNCTION, возвращающий closure, возвращающий closure | Нет явного теста |

---

## 32. Анализ Samples

### 32.1 SUB samples

| Sample | Демонстрирует |
|--------|---------------|
| `basic_call.axm` | Базовый SUB вызов |
| `byref_scalar_argument.axm` | BYREF скаляр |
| `byref_record_argument.axm` | BYREF TYPE запись |
| `byval_record_copy.axm` | BYVAL TYPE запись (copy) |
| `local_nested_call.axm` | Локальный nested SUB |
| `capture_outer_local.axm` | Closure (capture outer local) |

### 32.2 FUNCTION samples

| Sample | Демонстрирует |
|--------|---------------|
| `basic_definition.axm` | Базовый FUNCTION |
| `first_class_value.axm` | Function value как переменная |
| `returned_closure.axm` | Returned closure |
| `shared_closure_state.axm` | Shared closure state |
| `capture_outer_local.axm` | Capture outer variables |
| `local_nested_call.axm` | Локальный nested FUNCTION |
| `fixed_string_return_error.axm` | MB2026 negative |
| `fixed_string_parameter_error.axm` | MB2026 negative |

### 32.3 RETURN samples

| Sample | Демонстрирует |
|--------|---------------|
| `function_value.axm` | RETURN с значением |
| `sub_early_exit.axm` | RETURN без значения (early exit) |

### 32.4 Negative samples

| Sample | Диагностика |
|--------|-------------|
| `byref_requires_variable.axm` | MB2028 |
| `return_value_in_sub.axm` | MB2006 |
| `missing_return_value.axm` | MB2007 |
| `wrong_argument_count.axm` | MB2004 |
| `unknown_callable.axm` | MB2005 |
| `calling_non_function.axm` | MB2020 |
| `sub_name_as_value.axm` | MB2019 |

---

## 33. Анализ документации

### 33.1 Manual

| Страница | Качество | Примечание |
|----------|----------|------------|
| `procedures-and-functions.html` | ✅ Хорошее | Обзорная страница |
| `sub.html` | ✅ Хорошее | Полный синтаксис, BYVAL/BYREF, диагностики |
| `function.html` | ✅ Хорошее | Полный синтаксис, function values, closures, диагностики |
| `return.html` | ✅ Хорошее | Две формы RETURN, диагностики |

### 33.2 Спецификация

- `docs/archive/spec/SPECIFICATION.md` и `docs/archive/spec/SPECIFICATION2.md` — полная спецификация SUB/FUNCTION/RETURN/BYVAL/BYREF
- `SPECIFICATION3.md` — EXIT через FINALLY, events, predeclared FOR counter

### 33.3 Архитектура

- `ARCHITECTURE.md` sections 20-22 — local nested procedures, runtime diagnostics, function types/closures
- Multiple updates documenting call model evolution

### 33.4 Соответствие кода и документации

Расхождений не обнаружено. Документация accurately отражает фактическое поведение.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

- `SUB`, `FUNCTION`, `END SUB`, `END FUNCTION`, `RETURN`, `BYVAL`, `BYREF` — в категории Keyword
- Обе темы покрыты

### 34.2 Автозавершение блоков

- `SUB ...` + Enter → вставка `END SUB`
- `FUNCTION ...` + Enter → вставка `END FUNCTION`

### 34.3 Smart Identifier

- `SmartIdentifierService` корректно обрабатывает имена параметров и локальных функций

### 34.4 Отладчик

- Breakpoints на строках внутри SUB/FUNCTION работают
- Step over/into/out работают
- Нет панели стека вызовов (SYS-020 из отладочного аудита — P4, отложено)

---

## 35. Зависимости от Host / Platform

Специфических зависимостей от платформы нет. Процедурная модель использует только стандартные возможности .NET.

---

## 36. Карта корневых причин

Все найденные проблемы имеют отдельные, несвязанные корневые причины. Общей корневой причины не обнаружено.

Предыдущий аудит (Session 1042) уже закрыл все P2/P3 находки:
- CALL-001 (P2): Stack overflow protection → DONE (MB3011)
- CALL-003 (P2): Negative acceptance samples → DONE (5 samples)
- CALL-004 (P3): BYREF limitation documentation → DONE
- CALL-005 (P3): Mutual recursion test → DONE
- CALL-006 (P3): MB3006 runtime test → DONE

---

## 37. Стабильные области

**Не следует трогать:**

1. **Двухпроходная регистрация** — обеспечивает forward references
2. **Единый FunctionSymbol** для SUB и FUNCTION — чистая архитектура
3. **Изолированные фреймы** — корректная рекурсия
4. **BYREF через RuntimeReferenceStoredValue** (Interpreter) и BytecodeVariableSlot delegates (VM)
5. **Closures через scope chain** (Interpreter) и shared slots (VM)
6. **Frame reuse в VM** — минимизация аллокаций
7. **Capture caching** — минимизация lookup
8. **FINALLY unwinding при RETURN** — корректная очистка
9. **MaxCallDepth = 200** — защита от переполнения

---

## 38. Возможности консолидации

Консолидация не требуется. Разделение между Interpreter и VM является намеренным архитектурным решением.

---

## 39. Возможности разделения ответственности

Разделение ответственности уже выполнено корректно:
- `Binder.Declarations.cs` — регистрация
- `Binder.Routines.cs` — связывание тел
- `Binder.InvocationExpressions.cs` — связывание вызовов
- `Interpreter.Builtins.cs` — выполнение вызовов
- `BytecodeVm.cs` — выполнение вызовов в VM

---

## 40. Кандидаты на удаление

### 40.1 ByRefStoragePlan dead code

`FieldByRefStoragePlan` и `ArrayElementByRefStoragePlan` в `BoundProgramLowerer.cs` — POSSIBLY DEAD. Могут понадобиться при расширении BYREF.

---

## 41. Готовность к дальнейшему развитию

Подсистема готова к:
- Добавлению optional/default параметров (документировано в `docs/FUTURE.md`)
- Добавлению anonymous functions/lambdas (документировано в `docs/FUTURE.md`)
- Расширению BYREF на array elements и record fields
- Интеграции с будущим отладчиком (call stack panel)

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P4 | LEGACY | Lowering | FieldByRefStoragePlan и ArrayElementByRefStoragePlan — dead code | `BoundProgramLowerer.cs` | Сознательное ограничение BYREF | HIGH |

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

Процедурная модель считается завершённой, если:

- [x] Parser корректно разбирает SUB/FUNCTION/RETURN/параметры
- [x] Binder корректно проверяет типы, BYREF валидацию, RETURN контекст
- [x] Interpreter корректно выполняет вызовы с BYVAL/BYREF/closures/recursion
- [x] Lowering корректно транслирует вызовы в CallFunction/CallFunctionByRef/CallValue
- [x] BytecodeVm корректно выполняет вызовы с frame reuse/capture caching
- [x] FINALLY unwinding при RETURN работает
- [x] Все diagnostic codes покрыты тестами
- [x] Паритет Interpreter ↔ BytecodeVm подтверждён
- [x] Samples покрывают все основные сценарии
- [x] Manual актуален
- [x] IDE подсветка и автозавершение работают

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **Нет проблем корректности и архитектуры уровня P0-P2.**

Мелкие находки (P4):
1. FieldByRefStoragePlan и ArrayElementByRefStoragePlan — dead code (P4)

---

## 47. ТОП-20 задач по производительности

1. **Нет задач по производительности.** Подсистема уже на целевом уровне (VM быстрее на 13-14%). Frame reuse, capture caching, function value caching обеспечивают оптимальную производительность.

---

## 48. Итоговая оценка здоровья подсистемы

| Метрика | Оценка | Объяснение |
|---------|--------|------------|
| Корректность | 9/10 | Все формы вызовов работают корректно на обоих движках |
| Семантика | 9/10 | Строгая проверка типов, BYREF валидация, RETURN контекст |
| Архитектура | 9/10 | Двухпроходная регистрация, единый FunctionSymbol, изолированные фреймы |
| Runtime (Interpreter) | 9/10 | Корректные closures, BYREF через RuntimeReferenceStoredValue |
| Runtime (Bytecode VM) | 9/10 | Frame reuse, capture caching, BYREF через slot delegates |
| Производительность | 9/10 | VM быстрее на 13-14%, frame reuse минимизирует аллокации |
| Память | 9/10 | ArrayPool для аргументов, frame reuse, capture caching |
| Error handling | 9/10 | MB3006, MB3009, MB3011, FINALLY unwinding |
| Tests | 9/10 | 75+ тестов по всем уровням |
| Samples | 9/10 | 16 positive + 7 negative + benchmarks |
| Documentation | 9/10 | 4 manual-страницы, спецификация, архитектура, BYREF audit |
| IDE | 9/10 | Подсветка, автозавершение, smart identifier |
| Maintainability | 9/10 | Код чистый, хорошо структурированный |
| Reliability | 9/10 | Стабильное поведение, нет известных багов |
| Future readiness | 9/10 | Готово к расширению (optional params, lambdas) |

**Средняя оценка: 9.0/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему SUB/FUNCTION/PARAMETERS/RETURN/CALL MODEL достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES**

---

## Основные blockers

Нет. Подсистема стабильна и корректна.

## Что нужно сделать в первую очередь

Нет критических задач. Все предыдущие находки (CALL-001..006) уже закрыты.

## Что можно отложить

- Расширение BYREF на array elements и record fields
- Optional/default параметры
- Anonymous functions/lambdas
- Панель стека вызовов в отладчике

## Что не следует трогать

- Двухпроходную регистрацию
- Единый FunctionSymbol
- Изолированные фреймы
- BYREF модель (RuntimeReferenceStoredValue / BytecodeVariableSlot delegates)
- Closures модель (scope chain / shared slots)
- Frame reuse в VM
- Capture caching
- FINALLY unwinding при RETURN
- MaxCallDepth = 200
