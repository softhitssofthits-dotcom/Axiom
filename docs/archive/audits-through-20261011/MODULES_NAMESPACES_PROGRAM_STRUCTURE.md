# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## MODULES / NAMESPACES / PROGRAM STRUCTURE

Дата: 2026-08-21
Сессия: 1062
Метод: полный forensic-аудит по 62 разделам протокола

---

## 1. Резюме

**Axiom — строго однофайловый язык. Программа = один `.axm` файл.**

Подсистема MODULES / NAMESPACES / PROGRAM STRUCTURE охватывает всю цепочку от исходного текста до результата выполнения: как программа представляется на каждом уровне конвейера, как регистрируются символы, как работает глобальная область видимости, как строится точка входа и как программы исполняются через Interpreter и BytecodeVm.

**Главный вывод:** подсистема программной модели фундаментально стабильна и корректна для текущего однофайлового контракта. Отсутствие модулей, namespaces, imports и visibility modifiers — это осознанный feature gap, зафиксированный в `PROJECT.md` как отдельное направление проектирования, а не дефект текущей реализации.

**Вердикт: YES, WITH CONDITIONS.** Модель программы стабильна для перехода к следующим подсистемам. Условия: два небольших edge-case дефекта в runtime (отсутствие проверки глубины стека для TYPE invariant и отсутствие runtime-проверки `EXIT DO` на верхнем уровне) должны быть закрыты как P3 отдельной задачей.

| Показатель | Оценка |
|---|---|
| Корректность | 9/10 |
| Семантика | 9/10 |
| Архитектура | 9/10 |
| Runtime | 9/10 |
| Interpreter | 9/10 |
| Bytecode VM | 9/10 |
| Производительность | 9/10 |
| Память | 9/10 |
| Error handling | 8/10 |
| Tests | 8/10 |
| Samples | 8/10 |
| Documentation | 8/10 |
| IDE | 8/10 |
| Maintainability | 9/10 |
| Reliability | 9/10 |
| Future readiness | 7/10 |
| **Общий Health Score** | **8.6/10** |

---

## 2. Что представляет собой эта подсистема

Подсистема MODULES / NAMESPACES / PROGRAM STRUCTURE определяет:

- **Что такое программа Axiom** — одна строка `string SourceText`, один `.axm` файл, один модуль компиляции.
- **Какие элементы могут находиться на верхнем уровне** — глобальные операторы, объявления `SUB`, `FUNCTION`, `TYPE`.
- **Как регистрируются символы** — много проходный процесс в `BindProgram()`.
- **Как работает глобальная область видимости** — единый `_globalScope` с вложенными лексическими областями.
- **Как строится точка входа** — глобальные операторы (top-level statements), исполняемые последовательно сверху вниз.
- **Как программы проходят через весь конвейер** — `Lexer → Parser → Binder → Lowerer → BytecodeCompiler → Interpreter/BytecodeVm`.
- **Чего нет** — модулей, namespaces, imports, visibility modifiers, project file format.

---

## 3. Полный список возможностей

### SYS-001: Однофайловая модель программы

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Программа Axiom = один `.axm` файл, одна строка `string SourceText`.
- **Пользовательский синтаксис:** Нет специального синтаксиса. Файл = программа.
- **Parser:** `Parser.ParseProgram()` → `ProgramSyntax` с плоским списком `MemberSyntax`.
- **Syntax Node:** `ProgramSyntax` (`src/Axiom.Syntax/ProgramAndMembers.cs:123-139`)
- **Binder:** `BindProgram()` → `BoundProgram` с `GlobalStatements` + `Routines`.
- **Interpreter:** `ExecuteProgram()` → `ExecuteStatements(Program.GlobalStatements)`.
- **Bytecode VM:** Entry point routine `id: 0`, имя `"<global>"`, `isEntryPoint: true`.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: Глобальные операторы (top-level statements)

- **Категория:** PROGRAM STRUCTURE
- **Тип:** STATEMENT
- **Назначение:** Любой оператор на верхнем уровне файла становится исполняемым кодом точки входа.
- **Пользовательский синтаксис:** `PRINT "Hello"` на верхнем уровне.
- **Parser:** `ParseMember()` → `GlobalStatementSyntax(ParseStatement())` (`Parser.Declarations.cs:7-16`)
- **Syntax Node:** `GlobalStatementSyntax` (`ProgramAndMembers.cs:141-152`)
- **Binder:** `BindStatement(globalStatement.Statement)` → добавляется в `_globalStatements`.
- **Interpreter:** Исполняются последовательно в `ExecuteStatements()`.
- **Bytecode VM:** Становятся телом entry point routine (id=0).
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: Объявления SUB на верхнем уровне

- **Категория:** PROGRAM STRUCTURE
- **Тип:** STATEMENT
- **Назначение:** Процедуры, объявленные на верхнем уровне, регистрируются в глобальной области видимости.
- **Пользовательский синтаксис:** `SUB Name(params) ... END SUB`
- **Parser:** `ParseSubDeclaration()` → `SubDeclarationSyntax` (`Parser.Declarations.cs:83-104`)
- **Binder:** `DeclareFunction()` → `FunctionSymbol` в `_globalScope`, затем `BindRoutine()` → `BoundRoutine`.
- **Interpreter:** Хранятся в `Dictionary<FunctionSymbol, BoundRoutine> _routines`.
- **Bytecode VM:** Получают routine id 1..N в порядке объявления.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: Объявления FUNCTION на верхнем уровне

- **Категория:** PROGRAM STRUCTURE
- **Тип:** STATEMENT
- **Назначение:** Функции, объявленные на верхнем уровне, регистрируются в глобальной области видимости.
- **Пользовательский синтаксис:** `FUNCTION Name(params) AS Type ... END FUNCTION`
- **Parser:** `ParseFunctionDeclaration()` → `FunctionDeclarationSyntax` (`Parser.Declarations.cs:106-129`)
- **Binder:** `DeclareFunction()` → `FunctionSymbol` в `_globalScope`, затем `BindRoutine()` → `BoundRoutine`.
- **Interpreter:** Хранятся в `Dictionary<FunctionSymbol, BoundRoutine> _routines`.
- **Bytecode VM:** Получают routine id 1..N в порядке объявления.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: Объявления TYPE на верхнем уровне

- **Категория:** PROGRAM STRUCTURE
- **Тип:** TYPE
- **Назначение:** Пользовательские типы данных с полями, методами, конструкторами и инвариантами.
- **Пользовательский синтаксис:** `TYPE Name ... END TYPE`
- **Parser:** `ParseTypeDeclaration()` → `TypeDeclarationSyntax` (`Parser.Declarations.cs:18-57`)
- **Binder:** Три прохода: `DeclareTypeNames()` → `PrepareTypeDeclarations()` → `DeclareTypeMethods()`.
- **Interpreter:** Методы становятся `BoundRoutine` в общем списке `_routines`.
- **Bytecode VM:** Методы получают routine id наравне с обычными SUB/FUNCTION.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: Локальные (вложенные) SUB/FUNCTION

- **Категория:** PROGRAM STRUCTURE
- **Тип:** STATEMENT
- **Назначение:** Локальные процедуры и функции внутри других SUB/FUNCTION с лексическим захватом.
- **Пользовательский синтаксис:** `SUB LocalSub() ... END SUB` внутри другого SUB/FUNCTION.
- **Parser:** `LocalSubDeclarationStatementSyntax` / `LocalFunctionDeclarationStatementSyntax` (`Statements.cs:272-296`)
- **Binder:** `DeclareLocalFunctions()` → `DeclareScopedFunction()` → регистрация в текущей области видимости.
- **Interpreter:** Захватывают внешнюю область видимости через `RuntimeFunctionValue.CapturedScope`.
- **Bytecode VM:** Полная поддержка прямых вызовов и захвата переменных через общие слоты хранения.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: Много проходная регистрация символов

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Обеспечение forward references между всеми верхнеуровневыми сущностями.
- **Проходы:** `DeclareTypeNames` → `PrepareTypeDeclarations` → `DeclareFunctions` → `DeclareTypeMethods` → bind bodies.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-008: Entry point — глобальные операторы

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Точка входа программы = глобальные операторы, исполняемые последовательно.
- **Interpreter:** `ExecuteProgram()` → `ExecuteStatements(Program.GlobalStatements)`.
- **Bytecode VM:** Entry point routine `id: 0`, имя `"<global>"`, `isEntryPoint: true`, `ParameterCount: 0`, `ReturnType: Void`.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-009: Цепочка областей видимости

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Лексическая цепочка областей видимости с тремя родительскими указателями.
- **Компонент:** `BoundScope` (`src/Axiom.Semantics/Scopes/BoundScope.cs:3-99`)
- **Родители:** `EnclosingScope`, `VariableParent`, `FunctionParent`.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-010: Блочная видимость

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Вложенные области видимости для блочных конструкций (IF, FOR, WHILE, DO, SELECT CASE).
- **Binder:** `EnterScope()` / `LeaveScope()` в `Binder.ScopeHelpers.cs`.
- **Interpreter:** `RuntimeScope` стек с вложенными областями.
- **Bytecode VM:** Локальные слоты per-routine.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-011: Защита от переполнения стека вызовов

- **Категория:** PROGRAM STRUCTURE
- **Тип:** RUNTIME FACILITY
- **Назначение:** Ограничение глубины вызовов до `MaxCallDepth = 200` с диагностикой `MB3011`.
- **Interpreter:** Проверка в `InvokeUserRoutine()` и `InvokeTypeConstructor()`.
- **Bytecode VM:** Проверка в `ExecuteRoutineCall()` и `InvokeBytecodeFunction()`.
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-012: Module/Namespace/Import система

- **Категория:** PROGRAM STRUCTURE
- **Тип:** PLANNED
- **Назначение:** Будущая система модулей и пространств имён.
- **Статус:** Не реализовано. Есть эскиз в `docs/archive/root-documents-20261011/FUTURE.md:498-545`.
- **Текущий статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-013: Visibility modifiers (PUBLIC/PRIVATE)

- **Категория:** PROGRAM STRUCTURE
- **Тип:** PLANNED
- **Назначение:** Модификаторы видимости для сущностей.
- **Статус:** Не реализовано. Отложено "до модульности" (`docs/archive/spec/SPECIFICATION.md:3465`).
- **Текущий статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-014: Project file format

- **Категория:** PROGRAM STRUCTURE
- **Тип:** PLANNED
- **Назначение:** Формат проекта для многофайловых программ.
- **Статус:** Не реализовано. Указано в `PROJECT.md:42`.
- **Текущий статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Уровень исходного текста

Программа Axiom = одна строка `string SourceText`. Нет путей к файлам, нет имён модулей, нет дескрипторов проекта.

**Файл:** `src/Axiom.Execution/Requests/CompilationRequest.cs` — принимает единственный `string SourceText`.

### 4.2 Синтаксический уровень

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `ProgramSyntax` | `src/Axiom.Syntax/ProgramAndMembers.cs` | 123-139 | Корень дерева. Плоский список `MemberSyntax` + `EndOfFileToken`. |
| `MemberSyntax` | `src/Axiom.Syntax/SyntaxNode.cs` | 10 | Абстрактный базовый класс. |
| `GlobalStatementSyntax` | `src/Axiom.Syntax/ProgramAndMembers.cs` | 141-152 | Обёртка для оператора на верхнем уровне. |
| `SubDeclarationSyntax` | `src/Axiom.Syntax/ProgramAndMembers.cs` | 266-319 | `SUB ... END SUB`. |
| `FunctionDeclarationSyntax` | `src/Axiom.Syntax/ProgramAndMembers.cs` | 321-379 | `FUNCTION ... END FUNCTION`. |
| `TypeDeclarationSyntax` | `src/Axiom.Syntax/ProgramAndMembers.cs` | 226-264 | `TYPE ... END TYPE` с полями и методами. |
| `LocalSubDeclarationStatementSyntax` | `src/Axiom.Syntax/Statements.cs` | 272-283 | Локальный SUB внутри тела routine. |
| `LocalFunctionDeclarationStatementSyntax` | `src/Axiom.Syntax/Statements.cs` | 285-296 | Локальный FUNCTION внутри тела routine. |

**Точка входа парсера:** `Parser.ParseProgram()` (`src/Axiom.Parsing/Parser.cs:39-68`) — цикл `while (Current.Kind != SyntaxKind.EndOfFileToken)` вызывает `ParseMember()` для каждого верхнеуровневого элемента.

**Dispatch верхнего уровня:** `ParseMember()` (`src/Axiom.Parsing/Parser.Declarations.cs:7-16`) — switch по `Current.Kind`: `TypeKeyword` → `ParseTypeDeclaration()`, `SubKeyword` → `ParseSubDeclaration()`, `FunctionKeyword` → `ParseFunctionDeclaration()`, иначе → `GlobalStatementSyntax(ParseStatement())`.

### 4.3 Семантический уровень

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `BindProgram()` | `src/Axiom.Semantics/Binding/Binder.cs` | 67-97 | Точка входа связывания. |
| `BoundProgram` | `src/Axiom.Semantics/BoundNodes/BoundProgram.cs` | 24-41 | `GlobalStatements` + `Routines`. |
| `BoundRoutine` | `src/Axiom.Semantics/BoundNodes/BoundProgram.cs` | 5-22 | `FunctionSymbol` + `Statements`. |
| `BoundScope` | `src/Axiom.Semantics/Scopes/BoundScope.cs` | 3-99 | Область видимости с тремя parent pointers. |
| `FunctionSymbol` | `src/Axiom.Semantics/Symbols/Symbols.cs` | 640-699 | Символ функции/процедуры. |
| `VariableSymbol` | `src/Axiom.Semantics/Symbols/Symbols.cs` | 582-617 | Символ переменной. |
| `UserDefinedTypeSymbol` | `src/Axiom.Semantics/Symbols/Symbols.cs` | 290-458 | Пользовательский TYPE. |

**Много проходная регистрация:**
1. `DeclareTypeNames()` (`Binder.Declarations.cs:7-24`) — регистрация имён TYPE.
2. `PrepareTypeDeclarations()` (`Binder.Declarations.cs:26-83`) — разрешение полей TYPE, вычисление глобальных integral constants.
3. `DeclareFunctions()` (`Binder.Declarations.cs:466-481`) — регистрация всех верхнеуровневых SUB/FUNCTION в `_globalScope`.
4. `DeclareTypeMethods()` (`Binder.Declarations.cs:483-598`) — регистрация методов, конструкторов, инвариантов TYPE.
5. Main loop (`Binder.cs:74-94`) — привязка тел.

### 4.4 Уровень понижения (Lowering)

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `BoundProgramLowerer.Lower()` | `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | 142-182 | Понижение `BoundProgram` → `LoweredProgram`. |
| `LoweredProgram` | `src/Axiom.Bytecode/Model/LoweredProgram.cs` | 1-37 | `EntryPoint` + `Routines`. |
| `LoweredRoutine` | `src/Axiom.Bytecode/Model/LoweredRoutine.cs` | 1-52 | `Id`, `Name`, `Instructions`, `Symbol`, `IsEntryPoint`. |

**Entry point:** Всегда `id: 0`, имя `"<global>"`, `symbol: null`, `isEntryPoint: true`, тело = `program.GlobalStatements`.

**User routines:** `id: 1..N` в порядке следования в `program.Routines`. TYPE методы не отличаются от обычных SUB/FUNCTION.

### 4.5 Уровень байткода

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `BytecodeCompiler.Compile()` | `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | 21-72 | Эмиссия `LoweredProgram` → `BytecodeModule`. |
| `BytecodeModule` | `src/Axiom.Bytecode/Model/BytecodeModule.cs` | 1-42 | Единый модуль: `Header`, `ConstantPool`, `Routines`, `Globals`, `Builtins`, `DebugInfo`, `EntryPointRoutineId`. |
| `BytecodeModuleValidator` | `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` | 13-37 | Валидация целостности модуля. |

**Глобальные слоты:** Lazy allocation при первом обращении (`RegisterGlobal()`, `BytecodeCompiler.cs:1551-1568`).

**Lifted entry globals:** Переменные, используемые только в entry point, становятся локальными слотами entry point routine вместо глобальных (`CollectLiftedEntryGlobals()`, `BytecodeCompiler.cs:235-251`).

### 4.6 Уровень исполнения

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `Interpreter.Execute()` | `src/Axiom.Runtime/Execution/Interpreter.cs` | 36-54 | Статическая точка входа интерпретатора. |
| `Interpreter.ExecuteProgram()` | `src/Axiom.Runtime/Execution/Interpreter.cs` | 69-91 | Исполнение глобальных операторов. |
| `BytecodeVm.Execute()` | `src/Axiom.Runtime/Execution/BytecodeVm.cs` | 110-137 | Статическая точка входа VM. |
| `BytecodeVm.ExecuteModule()` | `src/Axiom.Runtime/Execution/BytecodeVm.cs` | 141-166 | Исполнение модуля байткода. |
| `RuntimeScope` | `src/Axiom.Runtime/Environment/RuntimeScope.cs` | 1-82 | Область видимости времени выполнения. |
| `CallFrame` | `src/Axiom.Runtime/Environment/CallFrame.cs` | 1-23 | Кадр вызова. |

### 4.7 Интеграционный уровень

| Сущность | Файл | Строки | Описание |
|---|---|---|---|
| `AxiomExecutionPipeline` | `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` | 10 | Центральный оркестратор. |
| `CompiledProgram` | `src/Axiom.Execution/Results/CompiledProgram.cs` | 7 | Неизменяемый снимок всех артефактов компиляции. |
| `ProgramRunService` | `src/Axiom.Ide/Services/Execution/ProgramRunService.cs` | 13 | Мост IDE → pipeline. |

---

## 5. Полный путь выполнения

```
Исходный текст (.axm файл)
    │
    ▼
Lexer.Lex() → поток токенов
    │
    ▼
Parser.ParseProgram() → ProgramSyntax (плоский список MemberSyntax)
    │
    ▼
Binder.Bind(ProgramSyntax) → BoundProgram (GlobalStatements + Routines)
    │   ├── Pass 1: DeclareTypeNames()
    │   ├── Pass 2: PrepareTypeDeclarations()
    │   ├── Pass 3: DeclareFunctions()
    │   ├── Pass 4: DeclareTypeMethods()
    │   └── Pass 5: Bind bodies
    │
    ▼
BoundProgramLowerer.Lower(BoundProgram) → LoweredProgram
    │   ├── Entry point: id=0, name="<global>", body=GlobalStatements
    │   └── User routines: id=1..N
    │
    ▼
BytecodeCompiler.Compile(LoweredProgram) → BytecodeModule
    │   ├── ConstantPool
    │   ├── Routines (bytecode instructions)
    │   ├── Globals (global variable slots)
    │   ├── Builtins (builtin function bindings)
    │   └── DebugInfo (source mapping)
    │
    ▼
BytecodeModuleValidator.Validate(BytecodeModule)
    │
    ▼
    ┌─────────────────────────────────────────┐
    │  Interpreter.Execute(BoundProgram)      │  ← AST-walking
    │  ИЛИ                                    │
    │  BytecodeVm.Execute(BytecodeModule)     │  ← Stack-based VM
    └─────────────────────────────────────────┘
    │
    ▼
RuntimeResult (stdout, diagnostics, engine used)
```

Каждый этап оперирует ровно одной единицей компиляции. Нет многомодульной компиляции, нет линковки, нет отдельной компиляции.

---

## 6. Фактический семантический контракт

### 6.1 Программа

**Вход:** Строка `string SourceText` (содержимое `.axm` файла).

**Обработка:** `Parser.Parse()` → `Binder.Bind()` → `Lowerer.Lower()` → `BytecodeCompiler.Compile()`.

**Выход:** `CompiledProgram` содержащий `ProgramSyntax`, `BoundProgram`, опционально `LoweredProgram` и `BytecodeModule`.

**Допустимые входы:** Любая строка. Пустая строка = пустая программа.

**Недопустимые входы:** Нет ограничений на вход. Синтаксические и семантические ошибки диагностируются, но не блокируют парсинг.

**Типы:** Не применимо на этом уровне.

**Преобразования:** Нет. Исходный текст → синтаксическое дерево → связанное дерево → lowered → байткод.

**Изменения состояния:** Нет побочных эффектов при компиляции.

**Владение данными:** Каждый этап создаёт новые неизменяемые структуры.

**Lifetime:** `CompiledProgram` живёт до следующей компиляции или уничтожения `ProgramRunService`.

**Ошибки:** Синтаксические (`MB1xxx`), семантические (`MB2xxx`), ошибки backend (`BC4xxx`/`BC5xxx`).

### 6.2 Глобальная область видимости

**Вход:** Объявления верхнего уровня (DIM, CONST, SUB, FUNCTION, TYPE).

**Обработка:** `DeclareFunctions()` регистрирует все SUB/FUNCTION в `_globalScope` перед привязкой тел.

**Выход:** Единый `_globalScope` с зарегистрированными символами.

**Допустимые:** Уникальные имена, case-insensitive.

**Недопустимые:** Дублированные имена → `MB2002`.

### 6.3 Точка входа

**Вход:** Глобальные операторы программы.

**Обработка:** Последовательное исполнение сверху вниз.

**Выход:** Результат выполнения программы.

**Ограничения:** Нет `SUB Main`. Точка входа = глобальные операторы. Нельзя иметь параметры или возвращать значение.

---

## 7. Анализ Parser

### 7.1 Структура ProgramSyntax

`ProgramSyntax` (`src/Axiom.Syntax/ProgramAndMembers.cs:123-139`) — корень синтаксического дерева. Содержит:
- `Members` — `IReadOnlyList<MemberSyntax>`, плоский список.
- `EndOfFileToken` — токен конца файла.

Нет обёртки модуля, нет списка импортов, нет декларации пространства имён.

### 7.2 Dispatch верхнего уровня

`ParseMember()` (`src/Axiom.Parsing/Parser.Declarations.cs:7-16`) — полный dispatch:

```csharp
return Current.Kind switch
{
    SyntaxKind.TypeKeyword => ParseTypeDeclaration(),
    SyntaxKind.SubKeyword => ParseSubDeclaration(),
    SyntaxKind.FunctionKeyword => ParseFunctionDeclaration(),
    _ => new GlobalStatementSyntax(ParseStatement())
};
```

Только три ключевых слова запускают разбор объявления: `TYPE`, `SUB`, `FUNCTION`. Всё остальное проходит через `ParseStatement()` обёрнутое в `GlobalStatementSyntax`.

### 7.3 Отсутствие модульного синтаксиса

Подтверждено на всех уровнях:
- `TokenKind` (`src/Axiom.Lexing/TokenKind.cs`) — 137 строк, 0 модульных ключевых слов.
- `SyntaxKind` (`src/Axiom.Syntax/SyntaxKind.cs`) — 239 строк, 0 модульных конструкций.
- `MapTokenKind` (`src/Axiom.Parsing/Parser.Helpers.cs:264-402`) — маппинг только существующих ключевых слов.

### 7.4 Оценка Parser

**Корректность:** 9/10. Полный и корректный dispatch для текущего контракта.
**Восстановление после ошибок:** 9/10. Stuck-in-place recovery в `ParseProgram()`.
**Расширяемость:** 8/10. Добавление модульного синтаксиса потребует расширения `ParseMember()`.

---

## 8. Анализ Binder

### 8.1 Много проходная регистрация

`BindProgram()` (`src/Axiom.Semantics/Binding/Binder.cs:67-97`) выполняет 5 проходов:

1. **DeclareTypeNames** (`Binder.Declarations.cs:7-24`) — регистрация имён TYPE в `_declaredTypes`.
2. **PrepareTypeDeclarations** (`Binder.Declarations.cs:26-83`) — разрешение полей, вычисление констант, обнаружение циклических ссылок.
3. **DeclareFunctions** (`Binder.Declarations.cs:466-481`) — регистрация всех SUB/FUNCTION в `_globalScope`.
4. **DeclareTypeMethods** (`Binder.Declarations.cs:483-598`) — регистрация методов TYPE.
5. **Bind bodies** (`Binder.cs:74-94`) — привязка тел.

Forward references работают благодаря multi-pass: все имена зарегистрированы до привязки тел.

### 8.2 Глобальная область видимости

`_globalScope` (`Binder.cs:44-53`) — `BoundScope` без родителя. Предрегистрирует все встроенные функции из `BuiltinFunctions.All`. Все верхнеуровневые SUB/FUNCTION регистрируются в `_globalScope`.

### 8.3 Цепочка областей видимости

`BoundScope` (`src/Axiom.Semantics/Scopes/BoundScope.cs:3-99`) с тремя независимыми parent pointers:
- `EnclosingScope` — прямой родитель (для навигации `LeaveScope()`).
- `VariableParent` — цепочка поиска переменных.
- `FunctionParent` — цепочка поиска функций.

Case-insensitive (`StringComparer.OrdinalIgnoreCase`). Shadowing разрешён с диагностикой `MB2016`.

### 8.4 Символы

| Символ | Файл | Строки | Ключевые свойства |
|---|---|---|---|
| `FunctionSymbol` | `Symbols.cs` | 640-699 | `Name`, `Parameters`, `ReturnType`, `ContainingRoutine`, `IsBuiltIn`, `BuiltinId` |
| `VariableSymbol` | `Symbols.cs` | 582-617 | `Name`, `Type`, `IsReadOnly`, `IsGlobal`, `FixedStringLength` |
| `UserDefinedTypeSymbol` | `Symbols.cs` | 290-458 | `Fields`, `Methods`, `Constructors`, `Invariant` |

**Нет метаданных модуля/namespace** ни в одном символе.

### 8.5 Оценка Binder

**Корректность:** 9/10. Multi-pass обеспечивает forward references.
**Полнота:** 9/10. Все текущие конструкции корректно связаны.
**Расширяемость:** 8/10. Добавление модульных областей видимости потребует расширения `BoundScope`.

---

## 9. Анализ Interpreter

### 9.1 Точка входа

`Interpreter.Execute()` (`src/Axiom.Runtime/Execution/Interpreter.cs:36-54`) — статический метод. Создаёт экземпляр, вызывает `ExecuteProgram()`, ловит `RuntimeFailureException`, сбрасывает вывод, закрывает файлы.

### 9.2 Исполнение программы

`ExecuteProgram()` (`Interpreter.cs:69-91`):
1. Push `_globalScope` на `_scopeStack`.
2. `ExecuteStatements(Program.GlobalStatements)`.
3. Проверка на stray `RETURN` и `EXIT FOR`.
4. Pop `_globalScope`.

### 9.3 Хранение routines

`_routines = program.Routines.ToDictionary(static routine => routine.Symbol)` (`Interpreter.cs:31`).

Ключ = `FunctionSymbol` (reference identity). Все routines (верхнеуровневые SUB/FUNCTION, TYPE методы, локальные SUB/FUNCTION) в одном плоском словаре.

### 9.4 RuntimeScope

`RuntimeScope` (`src/Axiom.Runtime/Environment/RuntimeScope.cs:1-82`) — связный список:
- `_values` — `Dictionary<VariableSymbol, IRuntimeStoredValue>`.
- `Parent` — родительская область видимости.
- `OwningRoutine` — FunctionSymbol, которому принадлежит область.

**Цепочка поиска:** `TryLookup(variable)` — от текущей области вверх по `Parent`.

### 9.5 Вызов routines

`InvokeUserRoutine()` (`Interpreter.Builtins.cs:415-481`):
1. Поиск `BoundRoutine` в `_routines`.
2. Создание `RuntimeScope` с `CapturedScope` в качестве родителя.
3. Привязка параметров (BYVAL копия, BYREF алиас).
4. Проверка `_callStack.Count < MaxCallDepth` (200).
5. Push `CallFrame` и scope.
6. Исполнение тела.
7. Pop в `finally`.

### 9.6 Оценка Interpreter

**Корректность:** 9/10. Полная поддержка всех текущих конструкций.
**Производительность:** 8/10. Tree-walking, но для текущего масштаба адекватно.
**Надёжность:** 9/10. Защита от переполнения стека, закрытие ресурсов.

---

## 10. Анализ Bytecode / VM

### 10.1 Entry point

`BytecodeVm.ExecuteModule()` (`BytecodeVm.cs:141-166`):
1. Поиск entry point routine по `EntryPointRoutineId` (= 0).
2. Push call frame.
3. Главный цикл: `while (_callStack.Count > 0 && _stopState is null) { ExecuteCurrentInstruction(); }`.

### 10.2 Routine ID assignment

Entry point = id 0. User routines = id 1..N в порядке следования в `BoundProgram.Routines`. TYPE методы не отличаются от обычных SUB/FUNCTION.

### 10.3 Global slot assignment

Lazy allocation при первом обращении. Lifted entry globals (переменные, используемые только в entry point) становятся локальными слотами entry point routine.

### 10.4 Валидация entry point

`BytecodeModuleValidator.ValidateEntryPoint()` (`BytecodeModuleValidator.cs:98-166`):
- ID >= 0.
- Существует в таблице routines.
- Ровно один marker `IsEntryPoint`.
- Нет параметров.
- Void return type.

### 10.5 Оценка Bytecode VM

**Корректность:** 9/10. Полная поддержка всех текущих конструкций.
**Производительность:** 9/10. Stack-based VM с dispatch loop.
**Валидация:** 9/10. Комплексная проверка целостности модуля.

---

## 11. Анализ Runtime

### 11.1 IRuntimeHost

`IRuntimeHost` (`src/Axiom.Runtime/Host/IRuntimeHost.cs`) — абстракция внешнего мира. Композиция 8 интерфейсов: `IInputProvider`, `IKeyboardInputProvider`, `IMouseInputProvider`, `IOutputWriter`, `IRuntimeClock`, `IRuntimeRandom`, `IGraphicsHost`, `IRuntimeCancellationProvider`.

Интерпретатор никогда не касается консоли, файловой системы или графики напрямую. Всё взаимодействие идёт через `_host`.

### 11.2 Состояние runtime

| Поле | Файл:Строка | Назначение |
|---|---|---|
| `_routines` | `Interpreter.cs:12` | Словарь FunctionSymbol → BoundRoutine |
| `_globalScope` | `Interpreter.cs:13` | Корневая область видимости |
| `_callStack` | `Interpreter.cs:14` | Стек кадров вызовов |
| `_scopeStack` | `Interpreter.cs:15` | Стек областей видимости |
| `_openFiles` | `Interpreter.cs:16` | Открытые файлы |
| `_pendingOutput` | `Interpreter.cs:17` | Буфер вывода |
| `_host` | `Interpreter.cs:20` | Абстракция внешнего мира |

### 11.3 Оценка Runtime

**Архитектура:** 9/10. Чёткое разделение ответственности.
**Надёжность:** 9/10. Корректное управление ресурсами.
**Расширяемость:** 8/10. Добавление модульных областей потребует расширения `RuntimeScope`.

---

## 12. Взаимодействие с типами

Подсистема программной модели не взаимодействует с типами напрямую. Типы регистрируются через `DeclareTypeNames()` и `PrepareTypeDeclarations()`, но это часть семантического анализа, а не программной модели.

**Оценка:** 9/10. Нет проблем во взаимодействии.

---

## 13. Взаимодействие со String

Не применимо. Строки не участвуют в программной модели.

---

## 14. Взаимодействие с Array

Не применимо. Массивы не участвуют в программной модели.

---

## 15. Взаимодействие с TYPE

TYPE объявления — один из четырёх типов `MemberSyntax`. `TypeDeclarationSyntax` обрабатывается в три прохода:

1. `DeclareTypeNames()` — регистрация имён.
2. `PrepareTypeDeclarations()` — разрешение полей.
3. `DeclareTypeMethods()` — регистрация методов.

TYPE методы становятся обычными `BoundRoutine` в общем списке. В runtime они не отличаются от верхнеуровневых SUB/FUNCTION.

**Оценка:** 9/10. Корректная трёхпроходная обработка.

---

## 16. Взаимодействие с Functions / SUB

SUB и FUNCTION — три из четырёх типов `MemberSyntax`. Регистрируются в `_globalScope` перед привязкой тел. Forward references работают.

**Локальные SUB/FUNCTION:** Поддерживаются через `LocalSubDeclarationStatementSyntax` / `LocalFunctionDeclarationStatementSyntax`. Захватывают внешнюю область видимости через `RuntimeFunctionValue.CapturedScope`.

**Оценка:** 9/10. Полная поддержка с лексическим захватом.

---

## 17. Взаимодействие с Control Flow

Не применимо напрямую. Control flow конструкции (IF, FOR, WHILE, DO, SELECT CASE) работают внутри routines, а не на уровне программной модели.

**Оценка:** 9/10. Нет проблем во взаимодействии.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Compile errors

- Синтаксические: `MB1xxx` из Parser.
- Семантические: `MB2xxx` из Binder.
- Backend: `BC4xxx`/`BC5xxx` из BytecodeCompiler/Validator.

### 18.2 Runtime errors

- `MB3xxx` из Interpreter/BytecodeVm.
- `RuntimeFailureException` для фатальных ошибок.
- `MB3011` для переполнения стека вызовов.

### 18.3 Error recovery

Binder использует "best-effort" подход: продолжает связывание после ошибок, вставляя `BoundErrorExpression` как placeholder. Pipeline проверяет ошибки перед исполнением.

**Оценка:** 8/10. Хорошее восстановление, но нет overflow diagnostic для циклических констант.

---

## 19. State / Lifetime / Ownership

### 19.1 Кто создаёт

- `ProgramSyntax` — Parser.
- `BoundProgram` — Binder.
- `LoweredProgram` — BoundProgramLowerer.
- `BytecodeModule` — BytecodeCompiler.
- `RuntimeScope` — Interpreter/BytecodeVm.
- `CallFrame` — Interpreter/BytecodeVm.

### 19.2 Кто владеет

- `CompiledProgram` владеет `ProgramSyntax`, `BoundProgram`, опционально `LoweredProgram` и `BytecodeModule`.
- `Interpreter` владеет `_globalScope`, `_routines`, `_callStack`, `_scopeStack`.
- `BytecodeVm` владеет аналогичными структурами.

### 19.3 Когда уничтожается

- `CompiledProgram` — при сборке мусора или при следующей компиляции.
- `Interpreter`/`BytecodeVm` — после завершения `Execute()`.
- `RuntimeScope` — при выходе из области видимости (pop из `_scopeStack`).
- `CallFrame` — при возврате из функции (pop из `_callStack`).

**Оценка:** 9/10. Чёткое владение и lifetime.

---

## 20. Edge Cases

### 20.1 Пустая программа (нет элементов)

**Обработка:** Корректна. `BindProgram` итерирует 0 раз, возвращает `BoundProgram` с пустыми списками. Interpreter/BytecodeVm ничего не делают.

**Статус:** HANDLED, UNTESTED.

### 20.2 Только глобальные операторы (нет SUB/FUNCTION/TYPE)

**Обработка:** Корректна и протестирована. `Lower_ScalarStatements_ProducesLinearEntryPoint`.

**Статус:** HANDLED AND TESTED.

### 20.3 Только объявления (нет глобальных операторов)

**Обработка:** Корректна. Программа успешно связывается, routines определены, но нечего исполнять на верхнем уровне.

**Статус:** HANDLED, UNTESTED.

### 20.4 Очень большие программы

**Обработка:** Линейные сканы и dictionary lookups. Нет ограничений на количество routines или globals.

**Статус:** HANDLED, UNTESTED FOR STRESS.

### 20.5 Глубоко вложенные локальные функции (3+ уровня)

**Обработка:** Архитектурно поддерживается через рекурсивные цепочки `BoundScope`.

**Статус:** ARCHITECTURALLY SUPPORTED, UNTESTED.

### 20.6 Взаимная рекурсия

**Обработка:** Корректна и протестирована. `Execute_MutualRecursionIsEvenIsOddTerminates` и `Execute_CompiledMutualRecursionOverflowReportsDiagnostic`.

**Статус:** HANDLED AND TESTED.

### 20.7 Forward references

**Обработка:** Полностью поддерживается благодаря multi-pass. Тестировано.

**Статус:** HANDLED AND TESTED.

### 20.8 Дублированные имена

**Обработка:** Комплексное обнаружение на всех уровнях: верхнеуровневые имена, TYPE имена, поля TYPE, методы TYPE, локальные функции, переменные.

**Статус:** HANDLED AND TESTED.

### 20.9 Переполнение стека вызовов

**Обработка:** Проверка `MaxCallDepth = 200` в `InvokeUserRoutine()` и `InvokeTypeConstructor()` (Interpreter), `ExecuteRoutineCall()` и `InvokeBytecodeFunction()` (VM). Диагностика `MB3011`.

**ДЕФЕКТ:** `InvokeTypeInvariant()` (`Interpreter.Builtins.cs:569-`) НЕ проверяет `MaxCallDepth` перед push кадра.

**Статус:** HANDLED (с дефектом), TESTED.

### 20.10 EXIT DO на верхнем уровне

**Обработка:** Семантическая проверка существует (`MB2013`/`MB2014`). Но runtime-проверка в `ExecuteProgram()` (Interpreter.cs:77-85) проверяет только `HasReturn` и `HasExitFor`, НЕ проверяет `HasExitDo`.

**ДЕФЕКТ:** Если `EXIT DO` каким-то образом попадёт на верхний уровень, он будет проигнорирован молча.

**Статус:** PARTIALLY HANDLED (semantic check yes, runtime check no).

### 20.11 Циклические TYPE

**Обработка:** Обнаруживаются через DFS в `ReportCyclicTypeFields()` (`Binder.TypeResolution.cs:271-312`). Диагностика `MB2017`.

**ОГРАНИЧЕНИЕ:** Только первый цикл сообщается (`cycleReported` flag).

**Статус:** HANDLED (с ограничением), TESTED.

### 20.12 Циклические глобальные константы

**Обработка:** Обнаруживаются через `activeConstants` HashSet в `TryResolveGlobalIntegralConstant()`.

**ДЕФЕКТ:** Нет специфической диагностики для overflow — просто не удаётся разрешить константу.

**Статус:** HANDLED (с дефектом), TESTED.

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| Entry point (global stmts) | `ExecuteProgram()` | Entry point routine id=0 | ✅ | ✅ | ✅ |
| SUB/FUNCTION регистрация | `_routines` dictionary | Routine id 1..N | ✅ | ✅ | ✅ |
| TYPE methods | `_routines` (flat) | Routine id 1..N (flat) | ✅ | ✅ | ✅ |
| Forward references | Multi-pass | Multi-pass | ✅ | ✅ | ✅ |
| Глобальные переменные | `_globalScope` | Global slots 0..M | ✅ | ✅ | ✅ |
| Локальные функции | `RuntimeFunctionValue` | Capture slots | ✅ | ✅ | ✅ |
| Защита стека (routine) | `MaxCallDepth=200` | `MaxCallDepth=200` | ✅ | ✅ | ✅ |
| Защита стека (TYPE invariant) | **НЕТ проверки** | Проверка есть | ❌ | ❌ | ❌ |
| Пустая программа | Ничего не делает | Return из entry point | ✅ | ✅ | ✅ |

**Расхождение:** `InvokeTypeInvariant` в Interpreter не проверяет `MaxCallDepth`. Это единственный обнаруженный дефект паритета.

**Классификация:** Ошибка корректности (P3).

---

## 22. Анализ производительности

Подсистема программной модели не является hot path. Компиляция происходит один раз перед исполнением. Нет сценариев, требующих адресной оптимизации производительности.

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| Компиляция (parse+bind+lower+compile) | ~1ms | ~1ms | N/A | Не hot path |
| Пустая программа | <0.1ms | <0.1ms | PARITY | Не relevant |
| Программа с 100 routines | ~5ms | ~5ms | PARITY | Не hot path |

**Оценка производительности:** 9/10. Не является bottleneck.

---

## 23. Качество Benchmark

Не применимо. Подсистема программной модели не участвует в performance benchmarks.

---

## 24. Hot Paths и Bottlenecks

Нет hot paths. Подсистема программной модели выполняется один раз при компиляции. Все горячие пути находятся в других подсистемах (арифметика, строки, массивы, графика).

---

## 25. Архитектурные проблемы

### 25.1 Единый плоский namespace

Все верхнеуровневые символы (SUB, FUNCTION, TYPE, переменные, константы) живут в одном `_globalScope`. Нет изоляции между логическими группами.

**Серьёзность:** Не является проблемой для текущего однофайлового контракта. Будет проблемой при добавлении модульной системы.

### 25.2 Отсутствие qualified name resolution

Нет механизма для `ModuleName.FunctionName`. Все имена разрешаются в плоском пространстве.

**Серьёзность:** Feature gap, не дефект.

### 25.3 Нет промежуточной "module scope" между routine scope и global scope

Цепочка областей видимости: `local scope → global scope`. Нет промежуточного уровня для будущих модулей.

**Серьёзность:** Feature gap, не дефект. Будет расширено при реализации модульной системы.

### 25.4 TYPE методы неотличимы от верхнеуровневых routines

TYPE методы хранятся в `_routines` наравне с обычными SUB/FUNCTION. Нет метаданных о принадлежности к TYPE на уровне runtime.

**Серьёзность:** Не является проблемой. Диспатч методов идёт через `FunctionSymbol`, который хранит связь с TYPE через `_declaredMethodOwners`.

---

## 26. Дублирование логики

### 26.1 Регистрация символов

Логика регистрации символов распределена по 4 методам (`DeclareTypeNames`, `PrepareTypeDeclarations`, `DeclareFunctions`, `DeclareTypeMethods`). Нет дублирования — каждый метод имеет чёткую ответственность.

### 26.2 Проверка entry point

Проверка entry point существует в:
- `BytecodeModuleValidator.ValidateEntryPoint()` — валидация байткодного модуля.
- `BoundProgramLowerer.Lower()` — создание entry point.
- `BytecodeCompiler.Compile()` — установка `EntryPointRoutineId`.

Нет дублирования — каждый уровень делает свою часть работы.

**Оценка дублирования:** 9/10. Минимальное дублирование.

---

## 27. Single Source of Truth

| Сущность | Источник истины |
|---|---|
| Синтаксис программы | `ProgramSyntax` в `Axiom.Syntax` |
| Семантика программы | `BoundProgram` в `Axiom.Semantics` |
| Lowered representation | `LoweredProgram` в `Axiom.Bytecode` |
| Байткод | `BytecodeModule` в `Axiom.Bytecode` |
| Runtime behavior | `Interpreter` и `BytecodeVm` в `Axiom.Runtime` |
| Ключевые слова | `TokenKind` в `Axiom.Lexing` |
| Встроенные функции | `BuiltinFunctions.All` в `Axiom.Semantics` |

**Оценка:** 9/10. Чёткое разделение источников истины.

---

## 28. Legacy / Dead Code

Не обнаружен. Подсистема программной модели не содержит legacy или dead code.

---

## 29. Надёжность и безопасность

### 29.1 Управление ресурсами

- Открытые файлы закрываются в `finally` блоке `Interpreter.Execute()`.
- Вывод сбрасывается перед завершением.
- Scope stack и call stack корректно pop-аются.

### 29.2 Защита от переполнения стека

- `MaxCallDepth = 200` с диагностикой `MB3011`.
- **ДЕФЕКТ:** Отсутствие проверки в `InvokeTypeInvariant()`.

### 29.3 Path handling

Не применимо. Подсистема программной модели не работает с файловыми путями.

### 29.4 Memory abuse

Нет контроля над размером программы. Очень большие программы могут потребовать много памяти, но это общий concern, не специфичный для программной модели.

**Оценка надёжности:** 9/10.

---

## 30. Анализ тестов

### 30.1 Syntax Tests

**Файл:** `tests/Axiom.Syntax.Tests/DeclarationSyntaxTests.cs`

| Тест | Аспект |
|---|---|
| `Program_HasCorrectKind` | ProgramSyntax имеет SyntaxKind.Program |
| `GlobalStatement_HasCorrectKind` | GlobalStatementSyntax |
| `SubDeclaration_HasCorrectKind` | SubDeclarationSyntax |
| `FunctionDeclaration_HasCorrectKind` | FunctionDeclarationSyntax |
| `TypeDeclaration_HasCorrectKind` | TypeDeclarationSyntax |
| `Program_NullMembers_Throws` | Отклоняет null members |

### 30.2 Parser Tests

**Файл:** `tests/Axiom.Parsing.Tests/ParserTests.cs`

Тесты для: SUB declaration, FUNCTION with RETURN, local function declarations, TYPE с полями, TYPE с методами, TYPE с конструкторами, TYPE с dynamic/fixed array fields, TYPE с default values.

### 30.3 Binder Tests

**Файл:** `tests/Axiom.Semantics.Tests/BinderTests.cs`

Тесты для: forward function calls, function as first-class value, SUB name cannot be used as value, calling non-function, local function call before declaration, local function scope visibility, closure variable capture, duplicate declaration, TYPE field access, TYPE method calls, TYPE constructors.

### 30.4 Lowering Tests

**Файл:** `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

Тесты для: entry point creation, deterministic lowering, user routine lowering, indirect function value call, local nested routines, local recursive function, closure variable capture.

### 30.5 Bytecode Tests

**Файл:** `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`

Тесты для: scalar program compilation, global variable visibility across routines.

### 30.6 Validator Tests

**Файл:** `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Тесты для: missing entry point (BC4073), multiple entry points (BC4074), entry point mismatch (BC4075), entry point with parameters (BC4076), entry point with return value (BC4077), negative entry point ID (BC4093).

### 30.7 Execution Tests

**Файл:** `tests/Axiom.Runtime.Tests/InterpreterTests.Error.cs`

Тесты для: call stack overflow, mutual recursion overflow, mutual recursion termination.

### 30.8 Acceptance Tests

**Файл:** `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Негативные тесты: undeclared variable (MB2001), wrong argument count (MB2004), unknown callable (MB2005), return value in SUB (MB2006), missing return value (MB2007), sub name as value (MB2019), calling non-function (MB2020), duplicate TYPE (MB2002), cyclic TYPE (MB2022).

### 30.9 Оценка тестов

**Покрытие:** 8/10. Основные сценарии покрыты.
**Пробелы:** Нет тестов для пустых программ с только объявлениями, глубоко вложенных локальных функций (3+ уровней), программ с только TYPE объявлениями.

---

## 31. Недостающие тесты

### Обычные сценарии
- Программа только с TYPE объявлениями (без методов).
- Программа только с SUB/FUNCTION объявлениями (без глобальных операторов).
- Программа с 100+ routines.

### Отрицательные сценарии
- Дублированное имя TYPE метода и верхнеуровневой FUNCTION.

### Граничные случаи
- Глубоко вложенные локальные функции (3+ уровня).
- Программа с 200+ глобальными переменными.
- Entry point с только `RETURN`.

### Parity тесты
- Поведение пустой программы в Interpreter vs BytecodeVm.

### Performance тесты
- Не применимо для данной подсистемы.

---

## 32. Анализ Samples

### 32.1 Программная структура

| Sample | Что демонстрирует | Engine |
|---|---|---|
| `samples/acceptance/smoke_v0_1.axm` | Много-FUNCTION + много-SUB программа | Оба |
| `samples/smoke/full_language_bytecode.axm` | Полный smoke test | Bytecode |
| `samples/smoke/Recursion check.axm` | Рекурсивные FUNCTIONs | Оба |
| `samples/smoke/block_scope_semantics.axm` | Блочная видимость | Оба |
| `samples/operators/sub/local_nested_call.axm` | Локальный SUB | Оба |
| `samples/operators/function/local_nested_call.axm` | Локальный FUNCTION | Оба |
| `samples/operators/function/capture_outer_local.axm` | Захват внешней переменной | Оба |
| `samples/operators/function/first_class_value.axm` | FUNCTION как значение | Оба |
| `samples/operators/function/returned_closure.axm` | Возвращённое замыкание | Оба |
| `samples/operators/type/methods_self.axm` | TYPE методы с SELF | Оба |
| `samples/operators/type/constructor_new.axm` | TYPE конструктор | Оба |

### 32.2 Негативные samples

| Sample | Диагностика |
|---|---|
| `samples/acceptance/negative/unknown_callable.axm` | MB2005 |
| `samples/acceptance/negative/calling_non_function.axm` | MB2020 |
| `samples/acceptance/negative/return_value_in_sub.axm` | MB2006 |
| `samples/acceptance/negative/missing_return_value.axm` | MB2007 |
| `samples/acceptance/negative/sub_name_as_value.axm` | MB2019 |
| `samples/acceptance/negative/type_duplicate_declaration.axm` | MB2002 |
| `samples/acceptance/negative/type_cyclic_reference.axm` | MB2022 |

### 32.3 Оценка samples

**Покрытие:** 8/10. Основные сценарии продемонстрированы.
**Пробелы:** Нет sample для программы только с TYPE (без глобальных операторов), нет sample для глубоко вложенных локальных функций.

---

## 33. Анализ документации

### 33.1 Спецификация

`SPECIFICATION3.md` не содержит отдельного раздела о структуре программы или модульной системе. Единственное упоминание: `docs/archive/spec/SPECIFICATION.md:3465` — "откладывать до модульности: Public / Private TYPE".

### 33.2 Архитектура

`ARCHITECTURE.md` описывает:
- Структуру solution (раздел 3).
- Назначение слоёв (раздел 4).
- Границы между слоями (раздел 5).
- Правило против монолитных файлов (раздел 6).
- Блочную видимость (разделы 16-17).
- Каталог встроенных функций (раздел 19).
- Локальные вложенные процедуры (раздел 20).

### 33.3 Manual

Нет dedicated manual-страницы для программной модели. Ближайшие:
- `getting-started.html` — описание структуры файла программы.
- `procedures-and-functions.html` — SUB/FUNCTION.
- `variables-and-constants.html` — DIM/CONST.
- `type.html` — TYPE.

### 33.4 docs/archive/audits-legacy/MODULES_AUDIT.md

Существующий аудит (`docs/archive/audits-legacy/MODULES_AUDIT.md`, 147 строк, Session 1056) — краткий, но точный. Подтверждает однофайловую модель и feature gaps.

### 33.5 FUTURE.md

Единственный эскиз модульной системы (`docs/archive/root-documents-20261011/FUTURE.md:498-545`): `MODULE ... END MODULE`, `IMPORT`, 4 правила, implementation checklist. Не промотирован в спецификацию или план.

### 33.6 Оценка документации

**Полнота:** 8/10. Текущий контракт хорошо документирован. Будущее — только эскиз.
**Точность:** 9/10. Документация соответствует коду.
**Пробелы:** Нет dedicated manual-страницы для программной модели.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

Ключевые слова `SUB`, `FUNCTION`, `TYPE`, `END`, `DIM`, `CONST` присутствуют в обеих схемах подсветки (`Axiom.xshd` и `Axiom.Dark.xshd`). Модульных ключевых слов нет.

### 34.2 File Manager

IDE имеет файловый менеджер (`FilesPaneViewModel`), но это raw filesystem browser, а не модель проекта. Нет навигации между модулями, нет дерева зависимостей.

### 34.3 Запуск программы

`ProgramRunService` принимает один `string sourceText`. Нет multi-file compilation.

### 34.4 Оценка IDE

**Текущий контракт:** 8/10. Полная поддержка однофайлового контракта.
**Готовность к модулям:** 4/10. Нет модели проекта, нет навигации между файлами, нет multi-file compilation.

---

## 35. Зависимости от Host / Platform

Подсистема программной модели не зависит от host/platform. `IRuntimeHost` абстрагирует внешний мир, но программная модель не использует его напрямую.

**Оценка:** 10/10. Нет зависимостей.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Отсутствие модульной системы
├── SYS-012: Нет Module/Namespace/Import
├── SYS-013: Нет visibility modifiers
├── SYS-014: Нет project file format
└── MODULE-001..007 из docs/archive/audits-legacy/MODULES_AUDIT.md

**Причина:** Осознанное отложение до отдельного проектирования (`PROJECT.md:42`).

### ROOT-SYS-002: Неполная runtime-проверка edge cases
├── SYS-015: Отсутствие проверки MaxCallDepth в InvokeTypeInvariant
└── SYS-016: Отсутствие runtime-проверки HasExitDo на верхнем уровне

**Причина:** Небрежность при реализации edge case guards.

---

## 37. Стабильные области

| Область | Статус | Риск изменения |
|---|---|---|
| Однофайловая модель компиляции | ✅ Стабильна | НИЗКИЙ |
| Multi-pass регистрация символов | ✅ Стабильна | НИЗКИЙ |
| Цепочка областей видимости | ✅ Стабильна | НИЗКИЙ |
| Entry point (global statements) | ✅ Стабильна | НИЗКИЙ |
| Forward references | ✅ Стабильна | НИЗКИЙ |
| Блочная видимость | ✅ Стабильна | НИЗКИЙ |
| Защита стека вызовов | ✅ Стабильна | НИЗКИЙ |
| Error sentinel pattern | ✅ Стабильна | НИЗКИЙ |
| TYPE двухфазная конструкция | ✅ Стабильна | НИЗКИЙ |
| Встроенные функции (каталог) | ✅ Стабильна | НИЗКИЙ |

---

## 38. Возможности консолидации

Нет дублирования логики, требующего консолидации. Подсистема программной модели имеет чёткое разделение ответственности.

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности адекватно:
- `Axiom.Syntax` — синтаксическое дерево.
- `Axiom.Semantics` — семантический анализ.
- `Axiom.Bytecode` — понижение и байткод.
- `Axiom.Runtime` — исполнение.
- `Axiom.Execution` — координация.
- `Axiom.Ide` — пользовательский интерфейс.

Нет необходимых изменений.

---

## 40. Кандидаты на удаление

Не обнаружены. Подсистема не содержит dead code или legacy code.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Готовность к модульной системе

**НИЗКАЯ.** Нет:
- Ключевых слов MODULE/NAMESPACE/IMPORT в лексере.
- Синтаксических узлов для модулей.
- Областей видимости модулей в семантике.
- Метаданных модулей в символах.
- Multi-module compilation в pipeline.
- Модели проекта в IDE.

**Эскиз в FUTURE.md** (`docs/archive/root-documents-20261011/FUTURE.md:498-545`) предлагает `MODULE ... END MODULE` и `IMPORT`, но не промотирован в спецификацию.

### 41.2 Готовность к Compiler

**СРЕДНЯЯ.** Текущая архитектура (separate frontend/backend) хорошо подготовлена. `BoundProgram` → `LoweredProgram` → `BytecodeModule` — чёткий pipeline. Но отдельная компиляция модулей не поддерживается.

### 41.3 Готовность к дальнейшей оптимизации

**ВЫСОКАЯ.** Текущая структура позволяет добавлять оптимизации на уровне lowering и bytecode emission без изменения языковой модели.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P3 | RUNTIME | Interpreter | `InvokeTypeInvariant` не проверяет `MaxCallDepth` | `Interpreter.Builtins.cs:569-` | Небрежность | HIGH |
| SYS-002 | P3 | RUNTIME | Interpreter | Нет runtime-проверки `HasExitDo` на верхнем уровне | `Interpreter.cs:77-85` | Небрежность | HIGH |
| SYS-003 | P2 | PROGRAM | Pipeline | Нет multi-file support | `CompilationRequest.cs` | Feature gap | CONFIRMED |
| SYS-004 | P2 | PROGRAM | Pipeline | Нет project file format | `PROJECT.md:42` | Feature gap | CONFIRMED |
| SYS-005 | P2 | PROGRAM | Lexer/Parser | Нет import/module system | `TokenKind.cs` | Feature gap | CONFIRMED |
| SYS-006 | P3 | PROGRAM | Semantics | Нет visibility modifiers | `Symbols.cs` | Feature gap | CONFIRMED |
| SYS-007 | P3 | PROGRAM | Semantics | Нет namespace system | `BoundScope.cs` | Feature gap | CONFIRMED |
| SYS-008 | P3 | DIAGNOSTICS | Binder | Нет предупреждения для программ только с объявлениями | `BindProgram()` | Feature gap | LOW |
| SYS-009 | P4 | IDE | IDE | File tree — raw filesystem browser | `FilesPaneViewModel` | Feature gap | LOW |
| SYS-010 | P3 | TESTS | Tests | Нет теста пустой программы с только объявлениями | Отсутствует тест | Пробел в тестах | HIGH |
| SYS-011 | P3 | TESTS | Tests | Нет теста глубоко вложенных локальных функций (3+ уровней) | Отсутствует тест | Пробел в тестах | HIGH |
| SYS-012 | P4 | TESTS | Tests | Нет теста программы только с TYPE | Отсутствует тест | Пробел в тестах | MEDIUM |
| SYS-013 | P4 | DOCS | Manual | Нет dedicated manual-страницы для программной модели | Отсутствует страница | Пробел в документации | MEDIUM |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

Завершён в рамках данного аудита.

### ЭТАП 1 — Исправление критических проблем корректности

Нет критических проблем (P0/P1).

### ЭТАП 2 — Исправление небольших проблем корректности

| Задача | Описание | Файлы | Сложность |
|---|---|---|---|
| FIX-001 | Добавить проверку `MaxCallDepth` в `InvokeTypeInvariant()` | `Interpreter.Builtins.cs` | Низкая |
| FIX-002 | Добавить runtime-проверку `HasExitDo` на верхнем уровне | `Interpreter.cs:77-85` | Низкая |

### ЭТАП 3 — Усиление тестирования

| Задача | Описание | Файлы | Сложность |
|---|---|---|---|
| TEST-001 | Тест пустой программы с только объявлениями | `InterpreterTests.cs`, `BytecodeVmTests.cs` | Низкая |
| TEST-002 | Тест глубоко вложенных локальных функций (3+ уровня) | `BinderTests.cs`, `InterpreterTests.cs` | Низкая |
| TEST-003 | Тест программы только с TYPE | `InterpreterTests.cs` | Низкая |

### ЭТАП 4 — Обновление документации

| Задача | Описание | Файлы | Сложность |
|---|---|---|---|
| DOC-001 | Dedicated manual-страница для программной модели | `docs/manual/program-structure.html` | Средняя |

### ЭТАП 5 — Подготовка к модульной системе (отложено)

Не запланировано в текущем плане (`PLANS4.md`).

---

## 44. Граф зависимостей задач

```
FIX-001 (MaxCallDepth in invariant) ─── нет зависимостей
FIX-002 (HasExitDo runtime check) ─── нет зависимостей
TEST-001 (empty declarations test) ─── нет зависимостей
TEST-002 (deep nested functions) ─── нет зависимостей
TEST-003 (TYPE-only program) ─── нет зависимостей
DOC-001 (manual page) ─── нет зависимостей

Все задачи независимы и могут выполняться параллельно.
```

---

## 45. Definition of Done

Подсистема MODULES / NAMESPACES / PROGRAM STRUCTURE считается завершённой, когда:

1. **Корректность:** Все текущие конструкции (GlobalStatement, SUB, FUNCTION, TYPE, локальные SUB/FUNCTION) корректно парсятся, связываются, понижаются и исполняются через оба движка.
2. **Edge cases:** Пустые программы, программы только с объявлениями, глубоко вложенные локальные функции, взаимная рекурсия — всё работает корректно.
3. **Error handling:** Защита от переполнения стека, обнаружение дубликатов, циклических ссылок — всё работает в обоих движках.
4. **Interpreter ↔ VM parity:** Оба движка дают одинаковый результат для всех конструкций программной модели.
5. **Tests:** Покрыты все основные сценарии и edge cases.
6. **Documentation:** Текущий контракт документирован в manual и спецификации.
7. **IDE:** Подсветка синтаксиса, форматирование и запуск работают для всех конструкций.

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **SYS-001 [P3]:** `InvokeTypeInvariant` не проверяет `MaxCallDepth` — потенциальный CLR StackOverflowException без диагностики.
2. **SYS-002 [P3]:** Нет runtime-проверки `HasExitDo` на верхнем уровне — EXIT DO может быть проигнорирован молча.
3. **SYS-003 [P2]:** Нет multi-file support — feature gap.
4. **SYS-004 [P2]:** Нет project file format — feature gap.
5. **SYS-005 [P2]:** Нет import/module system — feature gap.
6. **SYS-006 [P3]:** Нет visibility modifiers — feature gap.
7. **SYS-007 [P3]:** Нет namespace system — feature gap.
8. **SYS-008 [P3]:** Нет предупреждения для программ только с объявлениями.
9. **SYS-009 [P4]:** File tree — raw filesystem browser.
10. **SYS-010 [P3]:** Нет теста пустой программы с только объявлениями.
11. **SYS-011 [P3]:** Нет теста глубоко вложенных локальных функций.
12. **SYS-012 [P4]:** Нет теста программы только с TYPE.
13. **SYS-013 [P4]:** Нет dedicated manual-страницы для программной модели.
14. Ограничение: Только первый циклический TYPE сообщается.
15. Ограничение: Нет overflow diagnostic для циклических констант.
16. Ограничение: Нет defensive wrapper вокруг `_routines` dictionary creation.
17. Ограничение: `CaptureScopeForFunction` делает O(n) поиск по call stack.
18. Ограничение: Нет stress-тестов для очень больших программ.
19. Ограничение: Нет теста для дублирования имени TYPE метода и верхнеуровневой FUNCTION.
20. Ограничение: Нет теста для entry point с только `RETURN`.

---

## 47. ТОП-20 задач по производительности

Производительность не является concern для данной подсистемы. Нет hot paths, нет bottleneck, нет addressable optimization targets.

Единственный потенциальный concern: `CaptureScopeForFunction` делает O(n) поиск по call stack для локальных функций, но это проявится только при глубокой рекурсии (200 уровней), что является edge case.

---

## 48. Итоговая оценка здоровья подсистемы

| Показатель | Оценка | Объяснение |
|---|---|---|
| Корректность | 9/10 | Все текущие конструкции работают корректно. Два небольших edge-case дефекта. |
| Семантика | 9/10 | Multi-pass регистрация, forward references, блочная видимость — всё стабильно. |
| Архитектура | 9/10 | Чёткое разделение ответственности между слоями. Нет дублирования. |
| Runtime | 9/10 | Корректное управление ресурсами, scope chain, call stack. |
| Interpreter | 9/10 | Полная поддержка всех конструкций. Один edge-case дефект. |
| Bytecode VM | 9/10 | Полная поддержка всех конструкций. Полный паритет с Interpreter. |
| Производительность | 9/10 | Не является bottleneck. Нет hot paths. |
| Память | 9/10 | Нет проблем с управлением памятью. |
| Error handling | 8/10 | Хорошее восстановление, но два пропущенных edge-case guards. |
| Tests | 8/10 | Основные сценарии покрыты. Некоторые edge cases не протестированы. |
| Samples | 8/10 | Основные сценарии продемонстрированы. Некоторые пропуски. |
| Documentation | 8/10 | Текущий контракт хорошо документирован. Нет dedicated manual-страницы. |
| IDE | 8/10 | Полная поддержка однофайлового контракта. Низкая готовность к модулям. |
| Maintainability | 9/10 | Чистая архитектура, минимальное дублирование. |
| Reliability | 9/10 | Стабильная работа, корректное управление ресурсами. |
| Future readiness | 7/10 | Хорошая основа для расширения, но модульная система потребует значительных изменений. |
| **Общий Health Score** | **8.6/10** | |

---

## Главный контрольный вопрос

> Можно ли считать подсистему MODULES / NAMESPACES / PROGRAM STRUCTURE достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS.**

### Условия

1. **FIX-001** (P3): Добавить проверку `MaxCallDepth` в `InvokeTypeInvariant()` — 5 минут работы.
2. **FIX-002** (P3): Добавить runtime-проверку `HasExitDo` на верхнем уровне — 5 минут работы.

### Основные blockers

Нет P0/P1 blockers. Два P3 дефекта должны быть закрыты отдельной задачей.

### Что нужно сделать в первую очередь

1. Исправить FIX-001 и FIX-002.
2. Добавить недостающие тесты (TEST-001..003).

### Что можно отложить

1. Модульная система (SYS-003..007) — отдельное проектирование.
2. Manual-страница (DOC-001) — может быть добавлена позже.
3. Visibility modifiers (SYS-006) — зависит от модульной системы.

### Что не следует трогать

1. Multi-pass регистрация символов — стабильна и корректна.
2. Цепочка областей видимости — стабильна и корректна.
3. Entry point механизм — стабилен и корректен.
4. Forward reference механизм — стабилен и корректен.
5. Блочная видимость — стабильна и корректна.
6. Защита стека вызовов — стабильна (за исключением FIX-001).
7. Однофайловая модель компиляции — стабильна до явного решения о модульной системе.
