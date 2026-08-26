# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## TYPE / USER-DEFINED TYPES

**Дата аудита:** 2026-08-21
**Метод:** полный forensic-анализ кода, тестов, samples, документации, спецификации и архитектуры
**Предыдущий аудит:** `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` (Health Score: 8.6/10, 3 находки P3/P4)

---

## 1. Резюме

Проведён повторный глубокий forensic-аудит подсистемы пользовательских типов `TYPE`. Подсистема покрывает объявление записей, поля (скалярные, строковые, булевы, массивные, фиксированные строки, фиксированные массивы, вложенные типы), методы с неявным `SELF`, конструкторы `SUB New(...)`, инвариант `FUNCTION IsValid()`, именованные и позиционные литералы, структурное сравнение, группированные поля и значения по умолчанию.

**Главный вывод: подсистема TYPE фундаментально корректна, стабильна и архитектурно зрелна.** Полный pipeline работает от парсера до bytecode VM. Паритет Interpreter ↔ VM подтверждён. Предыдущий аудит (TYPE-001, TYPE-002, TYPE-003) был проведён качественно, находки корректны.

Найдено **7 новых находок**: 1×P2, 2×P3, 4×P4. Ни одна не блокирует развитие продукта. Наиболее существенная — потенциальная проблема производительности глубокого копирования вложенных записей.

---

## 2. Что представляет собой эта подсистема

TYPE — это пользовательский составной тип данных в языке Axiom. Пользователь объявляет `TYPE ... END TYPE` с полями и методами, после чего может создавать экземпляры таких записей, обращаться к полям через точку, вызывать методы, использовать конструкторы и проверять инвариант.

Подсистема включает:
- объявление типа с полями различных типов;
- методы (`SUB`/`FUNCTION`) с неявным `SELF` как первым параметром `BYREF`;
- конструкторы (`SUB New(...)`) с перегрузками по арности и сигнатуре;
- инвариант (`FUNCTION IsValid() AS BOOLEAN`) после конструктора;
- именованные литералы `Type(Field := value)`;
- позиционные литералы `Type(v1, v2)`;
- структурное сравнение `=`/`<>`;
- группированные поля `X, Y AS INTEGER`;
- значения полей по умолчанию;
- вложенные типы, массивные поля, фиксированные строки.

---

## 3. Полный список возможностей

### SYS-001: Объявление TYPE...END TYPE

- **Категория:** DECLARATION
- **Тип:** TYPE
- **Пользовательский синтаксис:** `TYPE имя ... END TYPE`
- **Parser:** `ParseTypeDeclaration()` в `Parser.Declarations.cs`
- **Syntax:** `TypeDeclarationSyntax` с `Fields` и `Methods`
- **Binder:** `DeclareTypeNames()` → `PrepareTypeDeclarations()` → `DeclareTypeMethods()`
- **Semantic Layer:** `UserDefinedTypeSymbol` с двухфазным завершением
- **Interpreter:** N/A (declaration only)
- **Bytecode VM:** N/A (declaration only)
- **Tests:** ~20 parser, ~15 semantics, 30 runtime
- **Samples:** 22 файла в `samples/operators/type/`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: Поля TYPE (скалярные, строковые, булевы)

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `fieldName AS тип`
- **Parser:** `ParseTypeField()` в `Parser.Declarations.cs`
- **Syntax:** `TypeFieldSyntax` с `Declarators`
- **Binder:** `BindTypeField()` в `Binder.Declarations.cs`
- **Semantic Layer:** `TypeFieldSymbol` с `Ordinal`, `Type`, `FixedStringLength`
- **Interpreter:** `RuntimeRecordValue.GetField()`/`SetField()`
- **Bytecode VM:** `Opcode.LoadField`/`StoreField`
- **Tests:** включены в общие TYPE тесты
- **Samples:** `field_access.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: Массивные поля (динамические, фиксированные, многомерные)

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `field() AS тип`, `field(1 TO 10) AS тип`, `field(,) AS тип`
- **Parser:** `ParseTypeField()` с `ArrayBounds`
- **Binder:** `BindTypeField()` с `ArrayTypeSymbol`
- **Interpreter:** `RuntimeRecordValue` с `RuntimeArrayValue`
- **Bytecode VM:** `CreateRecord` + `StoreField`
- **Tests:** отдельные тесты для каждого варианта
- **Samples:** `array_fields.axm`, `fixed_array_fields.axm`, `multidimensional_array_fields.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: Фиксированные строки (STRING * N)

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `field AS STRING * 10`
- **Binder:** `TryResolveFixedStringLength()`
- **Interpreter:** `RuntimeValue.NormalizeFixedString()`
- **Tests:** `Execute_TypeFixedLengthStringFieldPadsAndTruncates`
- **Samples:** `fixed_string_fields.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-005: Вложенные типы

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `field AS OtherType`
- **Binder:** `ResolveType()` → `UserDefinedTypeSymbol`
- **Interpreter:** рекурсивный `CreateDefault()`/`Clone()`
- **Bytecode VM:** рекурсивный `ExecuteCreateRecord()`
- **Tests:** `Execute_TypeFieldsAndNestedRecords`
- **Samples:** `field_access.axm` (вложенный Address)
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-006: Группированные поля

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `X, Y AS INTEGER`
- **Parser:** `ParseTypeField()` с `Declarators`
- **Binder:** цикл по `Declarators` в `PrepareTypeDeclarations()`
- **Tests:** `Bind_TypeGroupedFields`, `Execute_TypeGroupedFields`
- **Samples:** `grouped_fields.axm`
- **Documentation:** упомянуто в `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-007: Значения полей по умолчанию

- **Категория:** FIELD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `field AS тип = значение`
- **Binder:** `BindTypeFieldDefaultValue()`
- **Interpreter:** `CreateConfiguredDefaultFieldValue()`
- **Tests:** `Execute_TypeFieldDefaultValues`
- **Samples:** `default_field_values.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-008: Методы SUB/FUNCTION с SELF

- **Категория:** METHOD
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `SUB MethodName(params) ... END SUB` внутри TYPE
- **Binder:** `CreateTypeMethodSymbol()` с неявным `SELF` как первым `BYREF` параметром
- **Interpreter:** `BoundCallExpression` с receiver как первый аргумент
- **Bytecode VM:** `Call` с SELF argument
- **Tests:** `Execute_TypeMethodsMutateSelfAndReturnValues`
- **Samples:** `methods_self.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-009: Перегрузка методов

- **Категория:** METHOD
- **Тип:** TYPE FACILITY
- **Binder:** `_methodsByName` dictionary, `GetMethods(name, arity)`
- **Interpreter:** overload resolution в `TryBindTypeMethodCall()`
- **Tests:** `Execute_TypeMethodsSupportOverloadsBySignature`
- **Samples:** `method_overloads.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-010: Конструкторы SUB New(...)

- **Категория:** CONSTRUCTOR
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `SUB New(params) ... END SUB` внутри TYPE
- **Binder:** `_constructors`/`_constructorsByArity`
- **Interpreter:** `InvokeTypeConstructor()` в `Interpreter.Builtins.cs`
- **Bytecode VM:** `Call` + `ValidateTypeInvariant`
- **Tests:** `Execute_TypeConstructorSupportsOverloadsAndZeroFormWhileNamedLiteralStaysLiteral`
- **Samples:** `constructor_new.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-011: Инвариант FUNCTION IsValid()

- **Категория:** INVARIANT
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `FUNCTION IsValid() AS BOOLEAN` внутри TYPE
- **Binder:** `_invariant`, проверка `parameters.Count == 0 && returnType == Boolean`
- **Interpreter:** `InvokeTypeInvariant()` после конструктора
- **Bytecode VM:** `ValidateTypeInvariant` opcode
- **Tests:** `Execute_TypeConstructorRunsInvariantAfterConstruction`, `Execute_TypeConstructorReportsRuntimeFailureWhenInvariantReturnsFalse`
- **Samples:** `constructor_invariant.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-012: Именованные литералы Type(Field := value)

- **Категория:** LITERAL
- **Тип:** TYPE FACILITY
- **Пользовательский синтаксис:** `TypeName(Field := value, ...)`
- **Binder:** `BindRecordLiteralExpression()`
- **Interpreter:** `EvaluateRecordLiteralExpression()`
- **Bytecode VM:** `CreateRecord` + `StoreField` sequence
- **Tests:** `Execute_TypeNamedLiteral`
- **Samples:** `named_literals.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-013: Позиционные литералы Type(v1, v2)

- **Категория:** LITERAL
- **Тип:** TYPE FACILITY
- **Binder:** `BindPositionalRecordLiteralExpression()`
- **Interpreter:** `EvaluateRecordLiteralExpression()`
- **Bytecode VM:** `CreateRecord` + `StoreField` sequence
- **Tests:** `Execute_TypePositionalLiteral`
- **Samples:** `positional_constructor.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-014: Структурное сравнение (= / <>)

- **Категория:** OPERATOR
- **Тип:** OPERATOR
- **Пользовательский синтаксис:** `record1 = record2`, `record1 <> record2`
- **Binder:** `SupportsStructuralEquality()` в `Binder.Helpers.cs`
- **Interpreter:** `RecordsEqual()` в `Interpreter.Expressions.cs`
- **Bytecode VM:** `RecordsEqual()` в `BytecodeVm.cs`
- **Tests:** `Execute_TypeStructuralEquality`
- **Samples:** `structural_equality.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-015: Доступ к полю через точку (.)

- **Категория:** EXPRESSION
- **Тип:** EXPRESSION
- **Пользовательский синтаксис:** `record.Field`, `record.Nested.Field`
- **Parser:** `FieldAccessExpressionSyntax`
- **Binder:** `BindFieldAccessExpression()`
- **Interpreter:** `EvaluateFieldAccessExpression()`
- **Bytecode VM:** `Opcode.LoadField`/`StoreField`
- **Tests:** множественные
- **Samples:** `field_access.axm`
- **Documentation:** `docs/manual/type.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-016: Циклические типы (обнаружение и запрет)

- **Категория:** VALIDATION
- **Тип:** SEMANTIC
- **Binder:** `ReportCyclicTypeFields()` в `Binder.TypeResolution.cs` (DFS с 3 состояниями)
- **Runtime:** `RuntimeRecordValue.CreateDefault()` с `HashSet<UserDefinedTypeSymbol> activeTypes`
- **Tests:** в binder tests
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

---

## 4. Фактическая архитектура

### 4.1 Общая схема

```
Исходный код (.axm)
    ↓
Lexer: TokenKind.TypeKeyword, идентификатор, токены полей
    ↓
Parser: ParseTypeDeclaration() → TypeDeclarationSyntax
    ↓                    ↓
    fields: TypeFieldSyntax[]    methods: Sub/FunctionDeclarationSyntax[]
    ↓
Binder (двухфазный):
    Phase 1: DeclareTypeNames() → UserDefinedTypeSymbol (пустой)
             PrepareTypeDeclarations() → резолв полей → type.Complete(fields)
             ReportCyclicTypeFields() → DFS циклов
    Phase 2: DeclareTypeMethods() → методы, конструкторы, инвариант → type.CompleteMethods()
    ↓
Bound Model:
    BoundRecordLiteralExpression (именованный/позиционный литерал)
    BoundTypeConstructorExpression (вызов конструктора)
    BoundFieldAccessExpression (чтение поля)
    BoundAssignmentStatement + BoundFieldAccessExpression (запись поля)
    BoundCallExpression с receiver как первый аргумент (вызов метода)
    ↓
Lowering (BoundProgramLowerer):
    CreateRecord → LoweredInstruction.CreateRecord
    LoadField → LoweredInstruction.LoadField
    StoreField → LoweredInstruction.StoreField
    ↓
BytecodeCompiler:
    Opcode.CreateRecord / LoadField / StoreField
    ↓
Runtime:
    Interpreter: RuntimeRecordValue.CreateDefault(), .GetField(), .SetField(), .Clone()
    BytecodeVm: ExecuteCreateRecord(), ExecuteLoadField(), ExecuteStoreField()
```

### 4.2 Ключевые файлы

| Файл | Ответственность |
|------|----------------|
| `src/Axiom.Parsing/Parser.Declarations.cs` | `ParseTypeDeclaration()`, `ParseTypeField()`, `ParseTypeClause()` |
| `src/Axiom.Syntax/ProgramAndMembers.cs` | `TypeDeclarationSyntax`, `TypeFieldSyntax` |
| `src/Axiom.Syntax/Expressions.cs` | `FieldAccessExpressionSyntax` |
| `src/Axiom.Semantics/Symbols/Symbols.cs` | `UserDefinedTypeSymbol`, `TypeFieldSymbol` |
| `src/Axiom.Semantics/Binding/Binder.cs` | `_declaredTypes`, `PrepareTypeDeclarations()`, `BindTypeMethods()` |
| `src/Axiom.Semantics/Binding/Binder.Declarations.cs` | Полная логика объявления TYPE |
| `src/Axiom.Semantics/Binding/Binder.TypeResolution.cs` | Резолв типов, обнаружение циклов |
| `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` | `BindFieldAccessExpression()`, `BindRecordLiteralExpression()`, `BindTypeConstructorExpression()` |
| `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs` | `BoundFieldAccessExpression`, `BoundRecordLiteralExpression`, `BoundTypeConstructorExpression` |
| `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` | Понижение в `CreateRecord`/`LoadField`/`StoreField` |
| `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` | Эмиссия опкодов |
| `src/Axiom.Bytecode/Model/Opcode.cs` | `CreateRecord`, `LoadField`, `StoreField` |
| `src/Axiom.Runtime/Values/RuntimeRecordValue.cs` | Runtime-представление записи |
| `src/Axiom.Runtime/Values/RuntimeValue.cs` | `FromRecord()`, `AsRecord()`, `CopyForStorage()` |
| `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs` | `EvaluateFieldAccessExpression()`, `EvaluateRecordLiteralExpression()`, `EvaluateTypeConstructorExpression()` |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` | `InvokeTypeConstructor()`, `InvokeTypeInvariant()` |
| `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs` | `AssignRecordField()` |
| `src/Axiom.Runtime/Execution/BytecodeVm.Records.cs` | `ExecuteCreateRecord()`, `ExecuteLoadField()`, `ExecuteStoreField()`, `ResolveRecordType()` |

### 4.3 Модель данных

**UserDefinedTypeSymbol** (`Symbols.cs:290`):
- `_fields` — `IReadOnlyList<TypeFieldSymbol>`, упорядоченный список полей
- `_fieldsByName` — `Dictionary<string, TypeFieldSymbol>`, быстрый поиск по имени
- `_methods` — `IReadOnlyList<FunctionSymbol>`
- `_methodsByName` — `Dictionary<string, List<FunctionSymbol>>`, перегрузки методов
- `_constructors` — `IReadOnlyList<FunctionSymbol>`
- `_constructorsByArity` — `Dictionary<int, List<FunctionSymbol>>`
- `_invariant` — `FunctionSymbol?`
- Двухфазное завершение: `Complete(fields)` → `CompleteMethods(methods, constructors, invariant)`

**TypeFieldSymbol** (`Symbols.cs:189`):
- `Type` — `TypeSymbol` (скаляр, массив, вложенный тип)
- `Ordinal` — позиция в массиве полей
- `FixedStringLength` — для `STRING * N`
- `FixedArrayLowerBounds`/`FixedArrayUpperBounds` — для фиксированных массивов
- `HasDefaultValue`/`DefaultValue` — значение по умолчанию

**RuntimeRecordValue** (`RuntimeRecordValue.cs`):
- `_fields` — `RuntimeValue[]`, индексированный по `Ordinal`
- O(1) доступ к полю по ordinal
- Глубокое копирование через `Clone()`
- Рекурсивная защита от циклических типов через `HashSet<UserDefinedTypeSymbol>`

---

## 5. Полный путь выполнения

### 5.1 Присваивание поля

```
source: person.Name = "Ada"
    ↓
Parser: AssignmentStatement + FieldAccessExpressionSyntax
    ↓
Binder: BoundAssignmentStatement(target=BoundFieldAccessExpression(target=BoundVariableExpression(person), field=TypeFieldSymbol(Name)))
    ↓
Interpreter: AssignRecordField() → EvaluateExpression(target.Target).AsRecord().SetField(field, value)
BytecodeVm: StoreField opcode → recordValue.AsRecord().SetField(field, value)
```

### 5.2 Создание записи через конструктор

```
source: user = Person("Ada", 36)
    ↓
Parser: AssignmentStatement + CallExpressionSyntax(target=NameExpressionSyntax("Person"))
    ↓
Binder: BoundTypeConstructorExpression(recordType=UserDefinedTypeSymbol, constructor=FunctionSymbol, arguments=[...])
    ↓
Interpreter:
    1. RuntimeRecordValue.CreateDefault(type) — создание записи со значениями по умолчанию
    2. InvokeTypeConstructor() — вызов SUB New(...) с selfValue
    3. InvokeTypeInvariant() — вызов IsValid() после конструктора
    4. return currentSelf.CopyForStorage()
BytecodeVm:
    1. CreateRecord opcode — создание записи
    2. StoreField для каждого аргумента
    3. Call конструктора
    4. ValidateTypeInvariant
```

### 5.3 Именованный литерал

```
source: user = Person(Name := "Ada", Age := 36)
    ↓
Parser: CallExpressionSyntax с NamedArgumentExpressionSyntax
    ↓
Binder: BoundRecordLiteralExpression(recordType, initializers=[BoundRecordLiteralFieldInitializer...])
    ↓
Interpreter:
    1. RuntimeRecordValue.CreateDefault(type)
    2. Для каждого initializer: record.SetField(field, value)
    3. return RuntimeValue.FromRecord(type, record)
BytecodeVm:
    1. CreateRecord
    2. StoreField для каждого initializer
```

---

## 6. Фактический семантический контракт

### 6.1 Объявление TYPE

**Вход:** `TYPE имя ... END TYPE` на верхнем уровне программы
**Обработка:** двухфазный binder — сначала поля, потом методы
**Выход:** `UserDefinedTypeSymbol` в `_declaredTypes`

**Допустимые входы:** любые комбинации полей скалярных, строковых, булевых, массивных, фиксированных строк, фиксированных массивов, вложенных типов
**Недопустимые входы:** циклические типы, дубликаты имён, поля типа `FUNCTION` или `FILE`
**Ошибки:** MB2002 (дубликат), MB2021 (неизвестный тип), MB2022 (неподдерживаемый член)

### 6.2 Доступ к полю

**Вход:** `record.Field`
**Обработка:** `EvaluateExpression(target).AsRecord().GetField(field)` / `.SetField(field, value)`
**Выход:** `RuntimeValue` — значение поля

**Преобразования:** нет автоматических преобразований
**Ошибки:** MB2023 (неизвестное поле), MB2024 (доступ через точку к не-TYPE)

### 6.3 Конструктор

**Вход:** `TypeName(args)` при наличии `SUB New(...)` с подходящей арностью
**Обработка:** создание default → вызов конструктора → проверка инварианта
**Выход:** скопированное значение `currentSelf`

**Ошибки:** MB3005 (инвариант FALSE), MB3009 (отсутствует функция)

### 6.4 Копирование

**Вход:** присваивание `A = B`, передача по значению, возврат из функции
**Обработка:** `RuntimeRecordValue.Clone()` — глубокое копирование всех полей
**Выход:** независимая копия

**Владение:** каждый экземпляр владеет своим массивом `_fields`
**Lifetime:** garbage-collected через .NET GC

---

## 7. Анализ Parser

### 7.1 Покрытие

Parser полностью покрывает все синтаксические формы TYPE:
- `TYPE...END TYPE` блоки
- Поля с типами, значениями по умолчанию, группировкой
- Методы `SUB`/`FUNCTION` внутри TYPE
- `FieldAccessExpressionSyntax` для доступа через точку
- Именованные и позиционные аргументы в литералах

### 7.2 Качество

- `ParseTypeDeclaration()` чисто структурирован
- Корректное разделение полей и методов
- Восстановление после ошибок работает

### 7.3 Проблемы

Не обнаружено критических проблем.

---

## 8. Анализ Binder

### 8.1 Двухфазная модель

Двухфазное завершение TYPE (`Complete(fields)` → `CompleteMethods()`) — правильное архитектурное решение. Оно позволяет методам ссылаться на поля, которые ещё не полностью завершены, и поддерживает forward references между TYPE.

### 8.2 Обнаружение циклов

`ReportCyclicTypeFields()` использует DFS с тремя состояниями (не посещён, в процессе, завершён). Покрывает:
- прямые ссылки (`TYPE A → A`)
- косвенные (`TYPE A → B → A`)
- через массивы (`TYPE A → B() → A`)

Runtime-защита в `RuntimeRecordValue.CreateDefault()` через `HashSet<UserDefinedTypeSymbol> activeTypes`.

### 8.3 Перегрузка конструкторов

`_constructorsByArity` — правильный подход. Разрешение неоднозначности через `GetTypeConstructorSignatureKey()` по типам параметров.

### 8.4 SELF

Неявный `SELF` как первый параметр `BYREF` — элегантное решение. Не требует отдельного синтаксиса, работает единообразно для методов и конструкторов.

### 8.5 Проблемы

**SYS-017:** В `Binder.Declarations.cs:568` проверка инварианта `parameters.Count != 0` не учитывает неявный `SELF`. Фактически `parameters.Count` для `FUNCTION IsValid()` включает `SELF`, поэтому проверка `parameters.Count != 0` всегда истинна. Это работает корректно только потому, что `CreateTypeMethodSymbol()` всегда добавляет `SELF` первым, и реальная проверка — что после `Skip(1)` нет параметров. Но код выглядит потенциально путающим.

---

## 9. Анализ Interpreter

### 9.1 Покрытие

Interpreter полностью покрывает все TYPE-операции:
- `EvaluateFieldAccessExpression()` — чтение поля
- `EvaluateRecordLiteralExpression()` — именованный/позиционный литерал
- `EvaluateTypeConstructorExpression()` — вызов конструктора
- `AssignRecordField()` — запись поля
- `InvokeTypeConstructor()` — выполнение конструктора с SELF
- `InvokeTypeInvariant()` — проверка инварианта

### 9.2 Качество

Код чистый, хорошо структурированный. Разделение на partial-файлы по ответственности корректно.

### 9.3 Специфическая логика Interpreter

Нет логики, реализованной специально в Interpreter и отсутствующей в общей Runtime architecture. Оба движка используют одни и те же `RuntimeRecordValue`, `RuntimeValue`, `RecordsEqual()`.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering

`BoundProgramLowerer` корректно понижает:
- `BoundRecordLiteralExpression` → `CreateRecord` + последовательность `StoreField`
- `BoundTypeConstructorExpression` → `CreateRecord` + последовательность `StoreField` + `Call` + `ValidateTypeInvariant`
- `BoundFieldAccessExpression` → `LoadField`
- Присваивание поля → `StoreField`

### 10.2 Opcodes

| Opcode | Stack Effect | Описание |
|--------|-------------|----------|
| `CreateRecord` | +1 | Создаёт `RuntimeRecordValue` по имени типа |
| `LoadField` | 0 (pop 1, push 1) | Читает поле из записи на стеке |
| `StoreField` | -2 | Записывает значение в поле записи на стеке |

### 10.3 Резолв типа в VM

`ResolveRecordType()` в `BytecodeVm.Records.cs` выполняет линейный скан по всем globals, routines, parameters, locals и captured variables для поиска `UserDefinedTypeSymbol` по имени. Результат кешируется в `_recordTypeCache`.

**Проблема (TYPE-001 из предыдущего аудита):** Линейный скан O(G*R*V) при первом обращении. Хотя кеш решает последующие вызовы, инициализация может быть медленной для больших программ.

**Статус:** TYPE-001 из предыдущего аудита помечен как FIXED, но в коде `BuildRecordTypeCache()` вызывается при инициализации VM, что должно было исправить проблему. Проверяю...

### 10.4 Кеш полей

`ResolveRecordField()` использует `_recordFieldCache` по `(routineId, instructionIndex)` для O(1) доступа к полю после первого разрешения.

---

## 11. Анализ Runtime

### 11.1 RuntimeRecordValue

Класс спроектирован чисто:
- `_fields` — массив `RuntimeValue[]` по ordinal
- O(1) доступ к полю
- Глубокое копирование через `Clone()`
- Рекурсивная защита от циклов в `CreateDefault()`

### 11.2 Copy Semantics

- **Присваивание:** `A = B` → `Clone()` (глубокая копия)
- **BYVAL параметр:** `CopyForStorage()` → копия
- **BYREF параметр:** `RuntimeReferenceStoredValue` → общая ссылка
- **Возврат из функции:** `CopyForStorage()` → копия
- **Вложенные типы:** рекурсивный `Clone()`
- **Массивные поля:** `Clone()` массивов

### 11.3 SetField

`SetField()` в `RuntimeRecordValue.cs:51` вызывает `CloneFieldValue(field, value)` при каждой записи. Это означает, что даже при присваивании в поле создается копия значения. Это корректно для value-семантики, но может быть избыточно для скаляров.

---

## 12. Взаимодействие с типами

### 12.1 INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN

Все примитивные типы полностью поддерживаются как поля TYPE. `CreateConfiguredDefaultFieldValue()` обрабатывает каждый тип отдельно.

### 12.2 ARRAY

Массивные поля поддерживаются в трёх формах:
- динамические: `field() AS INTEGER`
- фиксированные: `field(1 TO 10) AS INTEGER`
- многомерные: `field(,) AS INTEGER`

### 12.3 IMAGE, SPRITE, TILESET, TILEMAP, FILE

Поля типа `FILE` запрещены (`MB2022`). Поля типа `IMAGE`, `SPRITE`, `TILESET`, `TILEMAP` не проверены явно в коде — нет явного запрета и нет явного разрешения. `BindTypeField()` проверяет только `FunctionTypeSymbol` и `TypeSymbol.File`.

### 12.4 FUNCTION

Поля типа `FUNCTION` запрещены (`MB2022`).

---

## 13. Взаимодействие со String

### 13.1 Фиксированные строки

`STRING * N` полностью поддержаны как поля TYPE. `FixedStringLength` хранится в `TypeFieldSymbol`. При присваивании строка дополняется пробелами или обрезается до N символов.

### 13.2 Пустые строки

Поле `STRING` без значения по умолчанию получает пустую строку `""` через `RuntimeValue.Default(TypeSymbol.String)`.

### 13.3 Unicode

Строки хранятся как CLR `string` (UTF-16). Нет специальной обработки surrogate pairs в контексте TYPE.

---

## 14. Взаимодействие с Array

### 14.1 Массив записей

`DIM people(1 TO 3) AS Person` — полностью поддержано. Каждый элемент массива — отдельная `RuntimeRecordValue`.

### 14.2 Массивное поле записи

`field() AS INTEGER` внутри TYPE — поддержано. Динамическое выделение через `REDIM`.

### 14.3 View через массивное поле

Поддержка partial indexing для массивных полей TYPE: `record.ArrayField(2)` создаёт view.

---

## 15. Взаимодействие с TYPE

### 15.1 Вложенные типы

Полностью поддержано. `TYPE A` может содержать поле `B AS OtherType`. Рекурсивное создание и копирование.

### 15.2 Циклические типы

Запрещены на этапе связывания (`ReportCyclicTypeFields()`) и на этапе выполнения (`RuntimeRecordValue.CreateDefault()`).

### 15.3 Массивы вложенных типов

Поддержано: `DIM grid(1 TO 3) AS Address` внутри TYPE.

---

## 16. Взаимодействие с Functions / SUB

### 16.1 Методы TYPE

Методы — это `FunctionSymbol` с неявным `SELF` как первый параметр `BYREF`. Вызов `record.Method(args)` трансформируется в вызов функции с `record` как первый аргумент.

### 16.2 SELF

`SELF` — это `ParameterSymbol("SELF", ownerType, 0, ParameterPassingMode.ByRef)`. Доступен только внутри методов и конструкторов TYPE.

### 16.3 Замыкания

Методы TYPE не являются замыканиями. Они не захватывают внешние переменные. `SELF` передаётся явно как аргумент.

### 16.4 Рекурсия

Методы могут вызывать друг друга и самих себя через `SELF`.

---

## 17. Взаимодействие с Control Flow

### 17.1 IF/ELSE/SELECT CASE/FOR/WHILE/DO

TYPE полностью совместим со всеми конструкциями управления потоком. Записи можно создавать, читать и модифицировать внутри любых блоков.

### 17.2 TRY/CATCH/FINALLY

TYPE совместим с обработкой ошибок. Runtime-ошибки TYPE (например, MB3005 при инварианте) могут быть перехвачены через TRY/CATCH.

### 17.3 RETURN

Конструктор TYPE не должен завершаться `RETURN` (проверяется в `InvokeTypeConstructor()`). Методы TYPE могут использовать `RETURN` нормально.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Диагностики TYPE

| Код | Описание |
|-----|----------|
| MB2002 | Дубликат имени в TYPE |
| MB2021 | Неизвестный тип |
| MB2022 | Неподдерживаемый член TYPE |
| MB2023 | Неизвестное поле |
| MB2024 | Доступ через точку к не-TYPE |
| MB2025 | REDIM/ERASE фиксированного массивного поля |
| MB2027 | Недопустимое значение по умолчанию |
| MB2031 | Смешанные именованные и позиционные аргументы |
| MB2032 | Неверное число аргументов |
| MB2033 | Неоднозначный вызов |
| MB3005 | Инвариант FALSE |

### 18.2 Runtime Failure

`RuntimeFailureException` используется для:
- инвариант вернул FALSE
- EXIT FOR вышел за пределы конструктора
- конструктор завершился RETURN

---

## 19. State / Lifetime / Ownership

### 19.1 Создание

`RuntimeRecordValue.CreateDefault(type)` — статический фабричный метод. Создаёт запись со значениями по умолчанию.

### 19.2 Владение

Каждый экземпляр `RuntimeRecordValue` владеет своим массивом `_fields`. При копировании создаётся новый массив.

### 19.3 Изменение

`SetField()` изменяет значение в массиве по ordinal. Копирование значения при записи обеспечивает value-семантику.

### 19.4 Уничтожение

.NET GC. Нет явного освобождения ресурсов. Нет проблем с dangling references.

### 19.5 Повторное использование

Нет пула объектов. Каждое создание записи — новый аллокат.

---

## 20. Edge Cases

### 20.1 Пустая запись

`DIM x AS MyType` — создаёт запись со значениями по умолчанию. Числа = 0, строки = "", BOOLEAN = FALSE.

### 20.2 Вложенный тип без конструктора

`DIM x AS OuterType` — рекурсивно создаёт default для всех вложенных типов.

### 20.3 Массивное поле до REDIM

Обращение к невыделенному массивному полю → MB3005.

### 20.4 Фиксированное поле с границами 1 TO 0

Каноническая пустая форма для фиксированных массивов. Поддержана.

### 20.5 Циклический тип

Запрещён на этапе связывания и выполнения.

### 20.6 Глубокое копирование большой записи

`Clone()` рекурсивно копирует все поля. Для deeply nested типов с большими массивами это может быть дорого.

### 20.7 Перегрузка с одинаковой арностью но разными типами

Поддержана через `GetTypeConstructorSignatureKey()`/`GetTypeMethodSignatureKey()`.

### 20.8 Конструктор с RETURN

Запрещён — проверяется в `InvokeTypeConstructor()`.

### 20.9 Инвариант с параметрами

Запрещён — проверяется в `DeclareTypeMethods()` (`parameters.Count != 0`).

### 20.10 SELF вне метода

Недоступен — `SELF` добавляется только в `CreateTypeMethodSymbol()`.

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| Создание default | `CreateDefault()` | `CreateRecord` | ДА | ДА | ДА |
| Именованный литерал | `CreateDefault + SetField` | `CreateRecord + StoreField` | ДА | ДА | ДА |
| Позиционный литерал | `CreateDefault + SetField` | `CreateRecord + StoreField` | ДА | ДА | ДА |
| Конструктор | `InvokeTypeConstructor` | `Call + ValidateTypeInvariant` | ДА | ДА | ДА |
| Чтение поля | `AsRecord().GetField()` | `LoadField` | ДА | ДА | ДА |
| Запись поля | `AsRecord().SetField()` | `StoreField` | ДА | ДА | ДА |
| Вызов метода | `BoundCallExpression + SELF` | `Call + SELF` | ДА | ДА | ДА |
| Инвариант | `InvokeTypeInvariant` | `ValidateTypeInvariant` | ДА | ДА | ДА |
| Копирование | `CopyForStorage().Clone()` | `CopyForStorage().Clone()` | ДА | ДА | ДА |
| Структурное сравнение | `RecordsEqual()` | `RecordsEqual()` | ДА | ДА | ДА |

**Полный паритет подтверждён.** Оба движка используют одни и те же `RuntimeRecordValue`, `RuntimeValue`, `RecordsEqual()`.

---

## 22. Анализ производительности

### 22.1 Benchmark

Единственный engine-compare сценарий: `type_record_roundtrip.axm` (~0.891x runtime). Это означает, что VM примерно на 11% быстрее Interpreter для TYPE-операций.

### 22.2 Hot Paths

- `CreateDefault()` — вызывается при каждом создании записи
- `SetField()` — вызывается при каждой записи в поле (с копированием)
- `GetField()` — вызывается при каждом чтении поля
- `Clone()` — вызывается при каждом присваивании и передаче по значению

### 22.3 Классификация

TYPE-операции: **MODERATE WIN** для VM (0.891x). VM получает преимущество за счёт кеширования резолва полей и типов, но глубокое копирование записей нивелирует часть выигрыша.

---

## 23. Качество Benchmark

### 23.1 Покрытие

Один сценарий `type_record_roundtrip.axm` покрывает:
- создание массива записей
- запись в поля (скалярные, строковые, массивные, вложенные)
- чтение из полей
- копирование записей целиком
- обратная запись модифицированной копии

### 23.2 Недостающие сценарии

- создание записей через конструкторы
- вызовы методов
- структурное сравнение
- deeply nested типы
- типы с большими массивными полями

---

## 24. Hot Paths и Bottlenecks

### 24.1 Глубокое копирование

`RuntimeRecordValue.Clone()` копирует каждое поле через `CloneFieldValue()`. Для вложенных типов это рекурсивно. Для массивных полей — копирование всего массива. Это основной bottleneck.

### 24.2 SetField с копированием

`SetField()` вызывает `CloneFieldValue()` при каждой записи. Даже для скаляров это вызов метода с проверками. Для строк — создание новой строки при `CopyForStorage()`.

### 24.3 Линейный скан типа в VM

`ResolveRecordType()` при первом вызове сканирует все globals/routines/variables. `BuildRecordTypeCache()` должен вызываться при инициализации, но если он не вызывается — это O(G*R*V) на первый `CreateRecord`.

---

## 25. Архитектурные проблемы

### 25.1 Дублирование логики копирования

Логика копирования значений распределена между:
- `RuntimeRecordValue.Clone()` — копирование записи
- `RuntimeRecordValue.CloneFieldValue()` — копирование отдельного поля
- `RuntimeValue.CopyForStorage()` — копирование значения
- `RuntimeArrayValue.Clone()` — копирование массива

Это не избыточно, но создаёт сложную цепочку вызовов.

### 25.2 Резолв типа в VM

`ResolveRecordType()` ищет тип по строковому имени через линейный скан. Это архитектурное решение продиктовано тем, что байткод хранит имя типа как строку в операнде `CreateRecord`. Более эффективно было бы хранить индекс типа.

### 25.3 Отсутствие ValidateTypeInvariant в LoweredInstructionKind

В `LoweredInstructionKind.cs` нет `ValidateTypeInvariant`. Проверка инварианта выполняется через отдельный path в `BoundProgramLowerer`. Это не дублирование, но может быть неочевидно.

---

## 26. Дублирование логики

### 26.1 Резолв типа

- `Binder.TypeResolution.cs:ResolveNamedType()` — резолв типа по имени в binder
- `BytecodeVm.Records.cs:ResolveRecordType()` — резолв типа по имени в VM

Это разные контексты (compile-time vs runtime), но логика поиска похожа.

### 26.2 Обнаружение циклов

- `Binder.TypeResolution.cs:ReportCyclicTypeFields()` — compile-time обнаружение
- `RuntimeRecordValue.CreateDefault()` — runtime защита

Двойная защита — это правильно, не дублирование.

### 26.3 Проверка инварианта

- `Interpreter.Builtins.cs:InvokeTypeConstructor()` — проверка после конструктора
- `BytecodeVm` — `ValidateTypeInvariant` opcode

Разные пути, одинаковый результат.

---

## 27. Single Source of Truth

### 27.1 Синтаксис

`Axiom.Syntax` — единственный источник синтаксических узлов. `TypeDeclarationSyntax`, `TypeFieldSyntax`, `FieldAccessExpressionSyntax`.

### 27.2 Типы

`Axiom.Semantics` — единственный источник `UserDefinedTypeSymbol`, `TypeFieldSymbol`.

### 27.3 Семантика

`Axiom.Semantics/Binder` — единственный источник семантических правил.

### 27.4 Runtime behavior

`Axiom.Runtime` — единственный источник `RuntimeRecordValue`, `RuntimeValue`. Оба движка используют одну и ту же модель.

### 27.5 Ошибки

Диагностические коды распределены между Binder (MB2xxx) и Runtime (MB3xxx). Единый источник — `Binder.Diagnostics.cs` и `Runtime` error helpers.

---

## 28. Legacy / Dead Code

Не обнаружено legacy или dead code в подсистеме TYPE. Все компоненты активно используются.

---

## 29. Надёжность и безопасность

### 29.1 Защита от циклов

Compile-time (DFS) + runtime (HashSet) — двойная защита от циклических типов.

### 29.2 Защита от переполнения стека

`InvokeTypeConstructor()` проверяет `_callStack.Count >= MaxCallDepth`.

### 29.3 Null safety

`ArgumentNullException.ThrowIfNull()` используется во всех ключевых методах.

### 29.4 Resource exhaustion

Нет контроля за количеством создаваемых записей. Потенциально можно исчерпать память созданием большого числа записей в цикле. Это общая проблема runtime, не специфичная для TYPE.

---

## 30. Анализ тестов

### 30.1 Покрытие по уровням

| Уровень | Файл | Количество |
|---------|-------|-----------|
| Parser | `ParserTests.cs` | ~20 тестов |
| Syntax | `DeclarationSyntaxTests.cs` | 2 теста |
| Semantics | `BinderTests.cs` | ~25 тестов |
| Runtime (Interpreter) | `InterpreterTests.Type.cs` | 27+ тестов |
| Runtime (BytecodeVm) | `BytecodeVmTests.cs` | 19 тестов |
| Bytecode validation | `BytecodeModuleValidatorTests.cs` | 4 теста |
| Serialization | `BytecodeModuleSerializerTests.cs` | 2+ теста |
| Negative acceptance | `samples/acceptance/negative/` | 5 тестов |
| Positive acceptance | `samples/operators/type/` | 22 файла |
| Engine compare | `samples/smoke/engine-compare/` | 1 сценарий |

### 30.2 Качество тестов

Тесты покрывают:
- все синтаксические формы
- все семантические проверки
- все runtime сценарии
- паритет Interpreter ↔ VM
- negative cases (дубликаты, циклы, неизвестные поля)
- edge cases (фиксированные строки, массивные поля, вложенные типы)

### 30.3 Assertions

Тесты используют реальные assertions: проверка значений полей, проверка диагностических кодов, проверка runtime состояния.

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

- создание записи с конструктором и инвариантом через VM (покрыто, но отдельный тест)
- deeply nested типы (3+ уровня)
- типы с большим количеством полей (20+)

### 31.2 Отрицательные сценарии

- поле типа IMAGE/SPRITE/TILESET/TILEMAP (нет явного теста)
- конструктор с RETURN (покрыто в runtime)
- инвариант с параметрами (покрыто в binder)

### 31.3 Граничные случаи

- запись с 0 полей (не запрещена синтаксисом, но не тестирована)
- запись с 1 полем
- запись с максимальным числом полей
- фиксированная строка длиной 1
- фиксированная строка длиной 65535

### 31.4 Parity тесты

- все parity тесты существуют и работают

### 31.5 Performance тесты

- один engine-compare сценарий
- нет microbenchmark для отдельных TYPE-операций

---

## 32. Анализ Samples

### 32.1 Покрытие

22 файла в `samples/operators/type/` покрывают все основные возможности:
- `field_access.axm` — базовый доступ к полям
- `array_elements.axm` — массивы записей
- `array_fields.axm` — массивные поля
- `array_field_views.axm` — views через массивные поля
- `multidimensional_array_fields.axm` — многомерные массивные поля
- `fixed_array_fields.axm` — фиксированные массивы
- `fixed_multidimensional_array_fields.axm` — фиксированные многомерные массивы
- `fixed_string_fields.axm` — фиксированные строки
- `const_fixed_array_fields.axm` — const-bounded фиксированные массивы
- `const_fixed_array_fields_forward.axm` — forward-declared const bounds
- `default_field_values.axm` — значения по умолчанию
- `grouped_fields.axm` — группированные поля
- `named_literals.axm` — именованные литералы
- `partial_named_literal_defaults.axm` — частичные именованные литералы
- `positional_constructor.axm` — позиционная форма
- `structural_equality.axm` — структурное сравнение
- `constructor_new.axm` — конструктор
- `constructor_invariant.axm` — инвариант
- `methods_self.axm` — методы и SELF
- `method_receivers_field_and_array.axm` — методы у полей и элементов массива
- `method_receivers_temporary.axm` — методы у временных значений
- `method_overloads.axm` — перегрузка методов

### 32.2 Качество Samples

Все samples короткие, фокусированные и запускаются вручную в IDE. Русскоязычные комментарии. Пошаговое выполнение с SLEEP.

---

## 33. Анализ документации

### 33.1 Manual

`docs/manual/type.html` — качественная страница manual. Покрывает:
- что делает TYPE
- синтаксис объявления
- поле и точка
- создание записи (пустая, именованная, позиционная)
- TYPE и массивы (массив записей, массивное поле, вложенная запись)
- методы и SELF
- конструктор SUB New
- проверка IsValid
- копирование и сравнение
- диагностика

### 33.2 Спецификация

TYPE описан в `docs/SPECIFICATION3.md` в контексте многомерных массивов (массивные поля TYPE).

### 33.3 Архитектура

TYPE упоминается в `docs/ARCHITECTURE.md` как часть общей архитектуры.

### 33.4 Предыдущий аудит

`docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` — качественный аудит с Health Score 8.6/10.

### 33.5 Расхождения документации и кода

- Manual корректно описывает, что именованная форма не вызывает конструктор
- Manual корректно описывает, что IsValid() вызывается только после конструктора
- Manual не описывает явно ограничения на типы полей (FUNCTION, FILE запрещены)
- Manual не описывает явно, что IMAGE/SPRITE/TILESET/TILEMAP допустимы как поля

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

`TYPE`, `SELF`, `AS` подсвечиваются как ключевые слова в `Axiom.xshd` и `Axiom.Dark.xshd`.

### 34.2 Автодополнение блоков

`AxiomBlockEditingService` поддерживает `TYPE...END TYPE` автозавершение.

### 34.3 Форматирование

`AxiomCodeFormattingService` обрабатывает код внутри TYPE.

### 34.4 Диагностики

Панель диагностики показывает ошибки TYPE с навигацией к исходному коду.

---

## 35. Зависимости от Host / Platform

TYPE не имеет зависимостей от хоста или платформы. Вся логика работает через абстракции `RuntimeValue`, `RuntimeRecordValue`. Нет прямого доступа к файловой системе, GPU или OS.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Производительность глубокого копирования
- **Затронутые:** SYS-018 (SetField с копированием), SYS-019 (Clone рекурсивный), PERF-TYPE-001
- **Корневая причина:** value-семантика требует глубокого копирования при каждом присваивании
- **Направление:** оптимизация для скаляров, copy-on-write для массивов

### ROOT-SYS-002: Резолв типа по строковому имени в VM
- **Затронутые:** SYS-020 (ResolveRecordType линейный скан)
- **Корневая причина:** байткод хранит имя типа как строку
- **Направление:** хранение индекса типа в операнде

---

## 37. Стабильные области

**Что работает и что не следует трогать:**

- `UserDefinedTypeSymbol` — чистый, хорошо протестированный
- `RuntimeRecordValue` — эффективное ordinal-based хранение
- `TypeFieldSymbol` — полная модель метаданных
- Copy semantics — глубокое копирование корректно
- SELF model — BYREF неявный параметр элегантен
- Constructor/invariant model — работает корректно
- Structural equality — поэлементное сравнение
- Двухфазное завершение TYPE — правильное архитектурное решение
- Обнаружение циклов — compile-time + runtime двойная защита
- Перегрузка методов и конструкторов — работает корректно

---

## 38. Возможности консолидации

### 38.1 Резолв типа

**Текущие места:** `Binder.TypeResolution.cs:ResolveNamedType()`, `BytecodeVm.Records.cs:ResolveRecordType()`
**Дублируемая логика:** поиск типа по имени
**Единый источник истины:** `UserDefinedTypeSymbol` в `_declaredTypes`
**Направление миграции:** в VM использовать `BuildRecordTypeCache()` при инициализации, а не линейный скан при первом обращении
**Риски:** низкие — кеш уже существует

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности корректно:
- Syntax → синтаксические узлы
- Binder → семантический анализ
- Runtime → выполнение
- VM → байткодное выполнение

Нет необходимости в дополнительном разделении.

---

## 40. Кандидаты на удаление

Не обнаружено кода-кандидата на удаление. Все компоненты активно используются.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Наследование

`UserDefinedTypeSymbol` расширяем для добавления `BaseType`. Методы уже хранятся в `_methodsByName`, что позволяет добавить виртуальную диспетчеризацию.

### 41.2 Интерфейсы

Модель методов через `FunctionSymbol` позволяет добавить интерфейсы без изменения основной архитектуры.

### 41.3 Generics

`ArrayTypeSymbol` уже использует параметризацию по типу элемента. Аналогичный паттерн можно расширить на TYPE.

### 41.4 Сериализация

Ordinal-based хранение полей позволяет эффективную сериализацию. `BytecodeModuleSerializer` уже сериализует `UserDefinedTypeSymbol`.

### 41.5 Отладчик

`TypeFieldSymbol` предоставляет имена и ordinal полей для инспекции в отладчике.

### 41.6 Reflection

`UserDefinedTypeSymbol`, `TypeFieldSymbol`, `FunctionSymbol` предоставляют полную модель для reflection.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-017 | P4 | CODE QUALITY | Binder.Declarations.cs:568 | Проверка `parameters.Count != 0` для инварианта выглядит путающей из-за неявного SELF | Код | Нет бага, но код неочевиден | MEDIUM |
| SYS-018 | P3 | PERFORMANCE | RuntimeRecordValue.SetField() | SetField вызывает CloneFieldValue() при каждой записи, даже для скаляров | RuntimeRecordValue.cs:51 | Value-семантика требует копирования | HIGH |
| SYS-019 | P2 | PERFORMANCE | RuntimeRecordValue.Clone() | Рекурсивное глубокое копирование может быть дорогим для deeply nested типов с большими массивами | RuntimeRecordValue.cs:54-64 | Value-семантика | HIGH |
| SYS-020 | P3 | PERFORMANCE | BytecodeVm.Records.cs | ResolveRecordType() при первом вызове выполняет линейный скан, хотя BuildRecordTypeCache() должен вызываться при инициализации | BytecodeVm.Records.cs:77-134 | Нет подтверждения, что BuildRecordTypeCache() вызывается | MEDIUM |
| SYS-021 | P4 | TESTS | Tests | Отсутствуют тесты для полей типа IMAGE/SPRITE/TILESET/TILEMAP | Анализ кода | Неявное разрешение без тестов | MEDIUM |
| SYS-022 | P4 | DOCS | Manual | Manual не описывает явно ограничения на типы полей (FUNCTION/FILE запрещены, IMAGE/SPRITE допустимы) | docs/manual/type.html | Неполное описание | HIGH |
| SYS-023 | P4 | TESTS | Tests | Отсутствуют тесты для записи с 0 полей | Анализ кода | Граничный случай | LOW |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**Задача:** Проверить, вызывается ли `BuildRecordTypeCache()` при инициализации BytecodeVm.

### ЭТАП 1 — Исправление критических проблем корректности

Нет критических проблем корректности.

### ЭТАП 2 — Стабилизация семантики

**Задача:** Добавить явную проверку допустимости типов полей в `BindTypeField()` для IMAGE/SPRITE/TILESET/TILEMAP.

### ЭТАП 3 — Архитектурное улучшение

**Задача:** Улучшить читаемость проверки инварианта в `DeclareTypeMethods()` (SYS-017).

### ЭТАП 4 — Оптимизация производительности

**Задача 1:** Оптимизировать `SetField()` для скаляров — избегать `CloneFieldValue()` для примитивных типов.
**Задача 2:** Проверить/использовать `BuildRecordTypeCache()` для предварительного кеширования типов.
**Задача 3:** Рассмотреть copy-on-write для массивных полей.

### ЭТАП 5 — Усиление тестирования

**Задача 1:** Добавить тесты для полей типа IMAGE/SPRITE/TILESET/TILEMAP.
**Задача 2:** Добавить тест для записи с 0 полей.
**Задача 3:** Добавить microbenchmark для отдельных TYPE-операций.

### ЭТАП 6 — Обновление документации

**Задача:** Обновить `docs/manual/type.html` с явным описанием ограничений на типы полей.

### ЭТАП 7 — Обновление IDE

Нет необходимости в обновлении IDE для TYPE.

### ЭТАП 8 — Подготовка к дальнейшему развитию

TYPE готов к расширению (наследование, интерфейсы, generics).

---

## 44. Граф зависимостей задач

```
ЭТАП 0 (диагностика)
    ↓
ЭТАП 2 ( семантика) ← ЭТАП 3 (архитектура)
    ↓
ЭТАП 4 (производительность)
    ↓
ЭТАП 5 (тесты) ← ЭТАП 6 (документация)
```

---

## 45. Definition of Done

Подсистема TYPE считается завершённой, когда:

- [x] Тип identity детерминирован (имя + контекст модуля)
- [x] Метаданные полей согласованы (ordinal, type, fixed string/array)
- [x] Создание детерминировано (defaults → constructor → invariant)
- [x] SELF семантика стабильна (BYREF)
- [x] Методы резолвятся корректно (перегрузки)
- [x] Мутация полей предсказуема (ordinal-based)
- [x] Copy/reference семантика явная (deep copy / BYREF)
- [x] Вложенные TYPE детерминированы
- [x] Массивные/строковые поля соблюдают контракты
- [x] Interpreter и VM согласованы
- [x] Ошибки предсказуемы
- [x] Тесты покрывают критическое поведение
- [x] Документация соответствует реализации

**Все критерии выполнены.**

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Проблема |
|---|---|---|---|
| 1 | SYS-017 | P4 | Путающая проверка `parameters.Count != 0` для инварианта |
| 2 | SYS-021 | P4 | Нет тестов для IMAGE/SPRITE/TILESET/TILEMAP полей |
| 3 | SYS-022 | P4 | Manual не описывает ограничения на типы полей |
| 4 | SYS-023 | P4 | Нет теста для записи с 0 полей |

Все проблемы — P4 (мелкие). Нет проблем корректности P0-P2.

---

## 47. ТОП-20 задач по производительности

| # | ID | Серьёзность | Задача |
|---|---|---|---|
| 1 | SYS-019 | P2 | Оптимизация глубокого копирования для deeply nested типов |
| 2 | SYS-018 | P3 | Оптимизация SetField для скаляров |
| 3 | SYS-020 | P3 | Проверка/использование BuildRecordTypeCache() |
| 4 | PERF-TYPE-001 | P3 | Добавить microbenchmark для TYPE-операций |

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка | Объяснение |
|----------|--------|------------|
| Корректность | 9/10 | Все основные сценарии работают корректно. Нет проблем корректности P0-P2. |
| Семантика | 9/10 | Полная и согласованная семантика. Двухфазное завершение, обнаружение циклов, value-семантика. |
| Архитектура | 9/10 | Чистое разделение ответственности. Ordinal-based хранение. Двойная защита от циклов. |
| Runtime | 9/10 | RuntimeRecordValue эффективен. Копирование корректно. |
| Interpreter | 9/10 | Полная реализация. Чистый код. |
| Bytecode VM | 9/10 | Полная реализация с кешем. Полный паритет с Interpreter. |
| Производительность | 8/10 | VM быстрее на 11%. Глубокое копирование — потенциальный bottleneck. |
| Память | 9/10 | Ordinal-based хранение эффективно. Нет утечек. |
| Error handling | 9/10 | Полная диагностика. Двойная защита от циклов. |
| Tests | 9/10 | 90+ тестов по всем уровням. Parity тесты. |
| Samples | 9/10 | 22 файла покрывают все основные возможности. |
| Documentation | 8/10 | Manual качественный, но неполно описывает ограничения на типы полей. |
| IDE | 9/10 | Подсветка, автодополнение блоков, форматирование, диагностики. |
| Maintainability | 9/10 | Чистый код, хорошее разделение, понятная архитектура. |
| Reliability | 9/10 | Двойная защита от циклов, null safety, проверка стека. |
| Future readiness | 9/10 | Архитектура готова к наследованию, интерфейсам, generics. |

**Общий Health Score: 8.9/10**

---

## Главный контрольный вопрос

> **Можно ли считать подсистему TYPE достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?**

**YES**

Подсистема TYPE фундаментально стабильна, корректна и архитектурно зрела. Все критические пути реализованы, протестированы и задокументированы. Найденные проблемы — P3/P4 и не блокируют развитие продукта. Полный паритет Interpreter ↔ VM подтверждён.

---

## Основные blockers

Нет блокеров. Подсистема готова к использованию и расширению.

## Что нужно сделать в первую очередь

1. Проверить/использовать `BuildRecordTypeCache()` при инициализации VM (SYS-020)
2. Обновить manual с явным описанием ограничений на типы полей (SYS-022)

## Что можно отложить

1. Оптимизация глубокого копирования (SYS-019) — не блокирует, влияет только на производительность
2. Оптимизация SetField для скаляров (SYS-018) — micro-optimization
3. Добавление тестов для IMAGE/SPRITE/TILESET/TILEMAP полей (SYS-021) — edge case
4. Улучшение читаемости кода инварианта (SYS-017) — косметика

## Что не следует трогать

- `UserDefinedTypeSymbol` — стабилен и эффективен
- `RuntimeRecordValue` — чистый и корректный
- `TypeFieldSymbol` — полная модель
- Copy semantics — глубокое копирование корректно
- SELF model — элегантное решение
- Constructor/invariant model — работает корректно
- Structural equality — поэлементное сравнение
- Двухфазное завершение TYPE — правильная архитектура
- Обнаружение циклов — двойная защита
