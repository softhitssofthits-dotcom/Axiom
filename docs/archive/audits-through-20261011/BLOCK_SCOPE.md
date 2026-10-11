# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## BLOCK SCOPE — Блочная область видимости

**Дата аудита:** 2026-08-21
**Аудитор:** Codex (automated forensic audit)
**Версия кодовой базы:** текущее состояние репозитория на момент аудита

---

## 1. Резюме

Подсистема **Block Scope** реализует блочную область видимости переменных и функций в языке Axiom. Она охватывает три уровня: семантический анализатор (Binder), интерпретатор (Interpreter) и байткодный движок (BytecodeVm). Подсистема находится в состоянии **STABLE** с несколькими архитектурными расхождениями между интерпретатором и VM, которые являются осознанными проектными решениями, а не багами.

**Главные выводы:**
- Binder является единственным источником истины для семантики блочных областей видимости
- Interpreter точно воспроизводит цепочку областей видимости через RuntimeScope
- BytecodeVm полностью схлопывает блочные области в плоские локальные слоты — это осознанное архитектурное решение
- Нет выделенной страницы manual для block scope; документация размазана по страницам операторов
- Тестовое покрытие позитивных сценариев хорошее, но отрицательных и граничных — недостаточное
- Семантика WHILE/DO отличается от FOR: WHILE/DO создают новую область видимости на каждой итерации, FOR — нет

**Итоговая оценка:** подсистема достаточно стабильна и корректна для перехода к следующей подсистеме, но требует нескольких точечных улучшений.

---

## 2. Что представляет собой эта подсистема

Block Scope — это механизм, определяющий видимость и доступность имён переменных и функций внутри вложенных блоков кода. Каждый блок конструкции `IF`, `ELSEIF`, `ELSE`, `FOR`, `WHILE`, `DO`, `SELECT CASE`, `CASE`, `CASE ELSE`, `TRY`, `CATCH`, `FINALLY`, `SUB` и `FUNCTION` создаёт собственную область видимости.

**Принцип работы:**
- Имя, объявленное через `DIM` внутри блока, существует только в пределах этого блока
- После выхода из блока переменная становится недоступной
- Вложенные блоки видят имена внешних блоков
- Соседние блоки (например, THEN и ELSE) являются независимыми — одна и та же переменная может быть объявлена в обеих ветвях
- Затенение (shadowing) внешнего имени внутренним допустимо, но генерирует предупреждение `MB2016`

---

## 3. Полный список возможностей

### SYS-001: Область видимости блока IF

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | Каждая ветвь IF/ELSEIF/ELSE создаёт свою область видимости |
| Тип | RUNTIME FACILITY |
| Parser | Полная поддержка |
| Syntax Node | `IfStatementSyntax`, `ElseIfClauseSyntax`, `ElseClauseSyntax` |
| Binder | `BindStatementsInChildScope` для каждой ветви |
| Interpreter | `ExecuteStatementsInChildScope` для каждой ветви |
| Bytecode | Коллекция переменных из всех ветвей через `CollectDeclaredVariables` |
| Tests | Позитивный smoke, два негативных acceptance-теста |
| Samples | `samples/smoke/block_scope_semantics.axm`, `samples/smoke/block_scope_full_manual.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-002: Область видимости блока FOR

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | Переменная цикла FOR живёт в области видимости всего цикла |
| Тип | RUNTIME FACILITY |
| Binder | Двухфазный scope: первый для вывода типа, второй для тела цикла |
| Interpreter | Один scope на весь цикл; переменная определяется в `loopScope` |
| Bytecode | Переменная цикла добавляется в `_currentRoutineLocals` |
| Tests | Smoke-тесты, тесты на повторное использование предобъявленной переменной |
| Samples | `samples/smoke/block_scope_semantics.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-003: Область видимости блока WHILE

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | Тело WHILE создаёт область видимости на каждой итерации |
| Тип | RUNTIME FACILITY |
| Binder | `BindStatementsInChildScope` |
| Interpreter | `ExecuteStatementsInChildScope` на каждой итерации |
| Bytecode | Плоские локальные слоты, нет runtime-scope |
| Tests | Smoke-тесты |
| Samples | `samples/smoke/block_scope_full_manual.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-004: Область видимости блока DO LOOP

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | Тело DO LOOP создаёт область видимости на каждой итерации |
| Тип | RUNTIME FACILITY |
| Binder | `BindStatementsInChildScope` |
| Interpreter | `ExecuteStatementsInChildScope` на каждой итерации |
| Bytecode | Плоские локальные слоты |
| Tests | Smoke-тесты |
| Samples | `samples/smoke/block_scope_full_manual.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-005: Область видимости блока SELECT CASE

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | Каждая ветвь CASE и CASE ELSE создаёт свою область видимости |
| Тип | RUNTIME FACILITY |
| Binder | `BindStatementsInChildScope` для каждой ветви |
| Interpreter | `ExecuteStatementsInChildScope` для совпавшей ветви |
| Bytecode | Коллекция переменных из всех ветвей |
| Tests | Smoke-тесты |
| Samples | `samples/smoke/block_scope_full_manual.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-006: Область видимости TRY/CATCH/FINALLY

| Поле | Значение |
|---|---|
| Категория | CONTROL FLOW |
| Назначение | TRY, CATCH и FINALLY создают отдельные области видимости |
| Тип | RUNTIME FACILITY |
| Binder | TRY и FINALLY через `BindStatementsInChildScope`; CATCH через `EnterScope`/`LeaveScope` с объявлением переменной ошибки |
| Interpreter | TRY и FINALLY через `ExecuteStatementsInChildScope`; CATCH через явный `RuntimeScope` с `Define` переменной ошибки |
| Bytecode | Переменная CATCH добавляется в `_currentRoutineLocals` |
| Tests | Тесты TRY/CATCH существуют отдельно |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-007: Область видимости SUB/FUNCTION

| Поле | Значение |
|---|---|
| Категория | PROCEDURES |
| Назначение | Каждая процедура/функция создаёт свою область видимости с параметрами |
| Тип | RUNTIME FACILITY |
| Binder | `BoundScope` с отдельными `variableParent` и `functionParent` для поддержки замыканий |
| Interpreter | `RuntimeScope` с `OwningRoutine`, pushed на `_scopeStack` при вызове |
| Bytecode | `BytecodeCallFrame` с `_locals[]`, `Arguments[]`, `Captures[]` |
| Tests | Тесты локальных функций, замыканий, рекурсии |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-008: Затенение имён (Shadowing)

| Поле | Значение |
|---|---|
| Категория | SEMANTICS |
| Назначение | Предупреждение при объявлении имени, которое затеняет внешнее |
| Тип | DIAGNOSTIC |
| Binder | `ReportShadowedDeclaration` генерирует `MB2016` |
| Tests | `Bind_ReportsShadowingEnclosingName` в BinderTests |
| Samples | `samples/acceptance/negative/if_shadowing_outer_variable.axm` |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-009: Двухфазный scope цикла FOR

| Поле | Значение |
|---|---|
| Категория | SEMANTICS |
| Назначение | Первый scope для вывода типа переменной цикла, второй для тела |
| Тип | RUNTIME FACILITY |
| Binder | `EnterScope` → вывод типа → `LeaveScope` → `EnterScope` → тело → `LeaveScope` |
| Interpreter | Один scope на весь цикл |
| Bytecode | Плоские слоты, различие не проявляется |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

### SYS-010: Локальные функции в области видимости

| Поле | Значение |
|---|---|
| Категория | PROCEDURES |
| Назначение | Локальные SUB/FUNCTION могут захватывать переменные охватывающей функции |
| Тип | RUNTIME FACILITY |
| Binder | `DeclareScopedFunction` с `_scope` как variableParent и functionParent |
| Interpreter | `CaptureScopeForFunction` находит scope для замыкания |
| Bytecode | `BytecodeVariableSlot` для захваченных переменных через shared slots |
| Статус | IMPLEMENTED |
| Уверенность | CONFIRMED |

---

## 4. Фактическая архитектура

### Уровень 1: Семантический анализатор (Binder)

**Файл:** `src/Axiom.Semantics/Scopes/BoundScope.cs`
**Класс:** `BoundScope`
**Ответственность:** Compile-time хранение таблицы имён с поддержкой вложенности

**Структура данных:**
- `_variables: Dictionary<string, VariableSymbol>` — нечувствительный к регистру
- `_functions: Dictionary<string, FunctionSymbol>` — нечувствительный к регистру
- `EnclosingScope` — структурный родитель
- `VariableParent` — цепочка поиска переменных (по умолчанию = EnclosingScope)
- `FunctionParent` — цепочка поиска функций (по умолчанию = EnclosingScope)

**Ключевой дизайн:** Три указателя на родителя позволяют локальным функциям захватывать переменные охватывающей routine, при этом сохраняя отдельную цепочку поиска функций.

### Уровень 2: Интерпретатор (RuntimeScope)

**Файл:** `src/Axiom.Runtime/Environment/RuntimeScope.cs`
**Класс:** `RuntimeScope`
**Ответственность:** Runtime-хранение значений переменных с цепочкой родителей

**Структура данных:**
- `_values: Dictionary<VariableSymbol, IRuntimeStoredValue>` — ключ по reference equality
- `Parent: RuntimeScope?` — единственный родитель
- `OwningRoutine: FunctionSymbol?` — какая routine владеет этим scope

**Ключевой дизайн:** Один parent pointer, lookup через рекурсивный обход цепочки. Define() всегда добавляет в текущий scope. TryAssign() ищет по цепочке вверх.

### Уровень 3: Байткодный движок (BytecodeCallFrame)

**Файл:** `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
**Класс:** `BytecodeCallFrame`
**Ответственность:** Runtime-хранение значений переменных через плоский массив слотов

**Структура данных:**
- `_locals: RuntimeValue[]` — плоский массив локальных слотов
- `Arguments: BytecodeVariableSlot[]` — слоты параметров
- `Captures: BytecodeVariableSlot[]` — слоты захваченных переменных
- `_sharedLocals` — для BYREF-семантики замыканий

**Ключевой дизайн:** Нет цепочки областей видимости. Все переменные routine получают фиксированный целочисленный слот при компиляции. Блочная область видимости обеспечивается только на этапе связывания (MB2001).

---

## 5. Полный путь выполнения

### Путь 1: Interpreter (AST-walking)

```
Исходный код (.axm)
    ↓
Lexer → Token[]
    ↓
Parser → ProgramSyntax (AST)
    ↓
Binder → BoundProgram + BoundScope chain
    ↓
Interpreter.ExecuteProgram()
    ↓
  _scopeStack.Push(_globalScope)
    ↓
  Для каждого блока (IF/FOR/WHILE/DO/CASE/TRY):
    EnterScope() → _scopeStack.Push(new RuntimeScope(CurrentScope))
    ExecuteStatements(statements)
    LeaveScope() → _scopeStack.Pop()
    ↓
  Для каждого вызова SUB/FUNCTION:
    new RuntimeScope(capturedScope, function)
    _scopeStack.Push(scope)
    _callStack.Push(frame)
    ExecuteStatements(body)
    _scopeStack.Pop()
    _callStack.Pop()
    ↓
RuntimeResult (output, diagnostics)
```

### Путь 2: BytecodeVm

```
Исходный код (.axm)
    ↓
Lexer → Token[]
    ↓
Parser → ProgramSyntax (AST)
    ↓
Binder → BoundProgram
    ↓
BoundProgramLowerer → LoweredProgram
    CollectDeclaredVariables() → HashSet<VariableSymbol> для каждой routine
    ↓
BytecodeCompiler → BytecodeModule
    CreateLocalSlotMap() → Dictionary<VariableSymbol, int>
    ↓
BytecodeVm.ExecuteModule()
    CreateFrame() → _locals = new RuntimeValue[LocalSlotCount]
    ↓
  Для каждого LoadLocal/StoreLocal:
    frame._locals[slotIndex] → прямой доступ O(1)
    ↓
  Для каждого вызова routine:
    ExecuteRoutineCall → CreateFrame → push _callStack
    ↓
RuntimeResult (output, diagnostics)
```

---

## 6. Фактический семантический контракт

### Блочная область видимости IF

**Вход:** `IF condition THEN ... ELSEIF ... ELSE ... END IF`
**Обработка:** Каждая ветвь получает свой `BoundScope` при связывании и свой `RuntimeScope` при выполнении (Interpreter) или общий плоский набор слотов (BytecodeVm)
**Выход:** Переменные, объявленные в ветви, недоступны после `END IF`

**Допустимые входы:**
- `DIM x AS INTEGER = 5` внутри любой ветви
- `CONST x = 5` внутри любой ветви
- Вложенные блоки внутри ветви

**Недопустимые входы:**
- Использование имени из ветви после `END IF` → MB2001
- Повторное объявление того же имени в той же ветви → MB2002

**Побочные эффекты:** Затенение внешнего имени генерирует MB2016 (предупреждение)

### Область видимости FOR

**Вход:** `FOR i = start TO end [STEP step] ... NEXT`
**Обработка:** В Binder — двухфазный scope (вывод типа → тело). В Interpreter — один scope на весь цикл. В BytecodeVm — переменная цикла получает фиксированный слот.
**Выход:** Переменная цикла, созданная строкой `FOR`, недоступна после `NEXT`. Предобъявленная переменная остаётся доступной.

### Область видимости WHILE/DO

**Вход:** `WHILE condition ... END WHILE` / `DO ... LOOP [WHILE|UNTIL condition]`
**Обработка:** В Binder — `BindStatementsInChildScope`. В Interpreter — `ExecuteStatementsInChildScope` на каждой итерации (новый scope каждую итерацию). В BytecodeVm — плоские слоты.
**Выход:** Переменные, объявленные в теле, недоступны после `END WHILE` / `LOOP`

### Область видимости SELECT CASE

**Вход:** `SELECT CASE expr ... CASE ... CASE ELSE ... END SELECT`
**Обработка:** Каждая ветвь `CASE` и `CASE ELSE` получает свой scope
**Выход:** Переменные из одной ветви не видны в другой и после `END SELECT`

---

## 7. Анализ Parser

Parser не управляет областями видимости напрямую — он создаёт синтаксическое дерево (`IfStatementSyntax`, `ForStatementSyntax`, и т.д.), которое Binder затем обходит и создаёт scope-ы. Parser корректно разбирает вложенные блоки и не требует изменений для поддержки block scope.

**Оценка:** Parser не участвует в block scope. Это правильная архитектура.

---

## 8. Анализ Binder

### 8.1. Механизм создания scope

Binder использует `_scope` (текущий scope) и стек `_declarationContexts` для отслеживания контекста объявления. Каждый блок создаёт дочерний scope через `EnterScope()`, который делает `new BoundScope(_scope)`. После блока `LeaveScope()` возвращает `_scope` к родителю.

**Качество реализации:** Высокое. Механизм чистый, предсказуемый, легко расширяемый.

### 8.2. Двухфазный scope цикла FOR

Для цикла `FOR` используется особый паттерн:
1. Первый `EnterScope()` — для вывода типа переменной цикла из выражений start/end/step
2. `LeaveScope()` — закрытие первого scope
3. Второй `EnterScope()` — для объявления переменной цикла и связывания тела

**Причина:** Тип переменной цикла может зависеть от выражений `start`, `end`, `step`, которые ещё не были связаны. Временный scope позволяет объявить провизорную переменную для вывода типа, не загрязняя основной scope.

**Потенциальная проблема:** Если `start`, `end` или `step` ссылаются на имя, которое совпадает с именем переменной цикла, первый scope может дать неверный результат. Однако на практике это маловероятно и не обнаружено в тестах.

### 8.3. Затенение имён

`TryDeclareScopedVariable` проверяет:
1. Предопределённые константы → MB2002 (дублирование)
2. Текущий scope → MB2002 (дублирование)
3. Родительские scope-ы → MB2016 (затенение, предупреждение)

**Качество:** Корректно. Затенение — предупреждение, а не ошибка, что соответствует модели BASIC.

### 8.4. Область видимости параметров

Параметры `SUB`/`FUNCTION` объявляются в scope самой routine через `TryDeclareScopedVariable` с контекстом `DeclarationContexts.RoutineParameters`. Это корректно — параметры видны во всём теле функции.

### 8.5. Локальные функции

`DeclareScopedFunction` объявляет локальную функцию в текущем scope и проверяет:
- Дублирование в текущем scope → MB2002
- Затенение в родительских scope-ах → MB2016

При связывании тела локальной функции `variableParent` и `functionParent` устанавливаются на scope охватывающей routine, что позволяет локальной функции видеть переменные охватывающей функции.

---

## 9. Анализ Interpreter

### 9.1. Механизм выполнения блочных scope-ов

Interpreter использует `_scopeStack` (стек `RuntimeScope`). Каждый блок создаёт новый `RuntimeScope` через `ExecuteStatementsInChildScope()`, который:
1. Создаёт `new RuntimeScope(CurrentScope)`
2. Push на `_scopeStack`
3. Выполняет statements
4. Pop из `_scopeStack`

**Качество:** Высокое. Прямое соответствие семантике блочных scope-ов.

### 9.2. WHILE/DO — новый scope на каждой итерации

Критическое наблюдение: `ExecuteWhile` и `ExecuteDoLoop` вызывают `ExecuteStatementsInChildScope` **на каждой итерации**. Это означает:
- Переменная, объявленная в теле WHILE, существует только в пределах одной итерации
- После итерации scope уничтожается
- На следующей итерации создаётся новый scope

**Семантическое следствие:** Если объявить `DIM x AS INTEGER` в теле WHILE, на каждой итерации `x` будет новой переменной со значением по умолчанию (0), а не сохранённым значением с предыдущей итерации.

**Это корректно для модели BASIC** — каждая итерация цикла должна начинать с чистого scope для блочных переменных.

### 9.3. FOR — один scope на весь цикл

В отличие от WHILE/DO, цикл FOR создаёт **один** scope на весь цикл:
```csharp
var loopScope = new RuntimeScope(CurrentScope);
EnterScope(loopScope);
try {
    loopScope.Define(statement.Variable, startValue);
    // ... тело цикла ...
} finally { LeaveScope(); }
```

**Семантическое следствие:** Переменная цикла FOR сохраняется между итерациями. Это необходимо для корректной работы счётчика.

**Важный нюанс:** Тело цикла FOR выполняется через `ExecuteStatements(statements)`, а не `ExecuteStatementsInChildScope(statements)`. Это означает, что вложенные `DIM` в теле FOR живут в scope самого FOR, а не в отдельном per-iteration scope. Это отличается от WHILE/DO, где каждая итерация создаёт свой scope.

### 9.4. TRY/CATCH — особый случай

CATCH создаёт явный scope с определением переменной ошибки:
```csharp
var catchScope = new RuntimeScope(CurrentScope);
catchScope.Define(statement.CatchVariable, error);
signal = ExecuteStatementsInScope(catchScope, statement.CatchStatements);
```

**Качество:** Корректно. Переменная ошибки `CATCH` видна только в теле CATCH.

---

## 10. Анализ Bytecode / VM

### 10.1. Стратегия схлопывания scope-ов

BytecodeVm **не реализует** блочные scope-ы на уровне выполнения. Вместо этого:

1. `BoundProgramLowerer.CollectDeclaredVariables()` рекурсивно обходит **все** ветви (включая ELSE, CASE ELSE, вложенные блоки) и собирает каждую объявленную переменную в плоский `HashSet<VariableSymbol>`
2. `BytecodeCompiler.CreateLocalSlotMap()` присваивает каждой уникальной переменной целочисленный слот
3. `BytecodeCallFrame` выделяет `RuntimeValue[]` массив при создании фрейма
4. `LoadLocal`/`StoreLocal` работают по прямому индексу O(1)

### 10.2. Следствия схлопывания

**Переменные из всех ветвей получают слоты:**
- В `IF a THEN DIM x ... ELSE DIM x ... END IF` обе переменные `x` — это разные `VariableSymbol` (созданы в разных scope-ах binder-а), поэтому каждая получает свой слот
- Слоты выделяются для переменных из **всех** ветвей, включая невыполняемые

**Различие с Interpreter:**
- В Interpreter переменная из ELSE-ветви не существует, если выполнялась THEN-ветвь
- В BytecodeVm слот для переменной из ELSE-ветви выделен, но не инициализирован (значение по умолчанию — `default(RuntimeValue)`)

### 10.3. Отсутствие runtime-проверки видимости

BytecodeVm не проверяет, что переменная используется в пределах своего блока. Если binder по какой-то причине пропустил ошибку (например, при сложных вложенных конструкциях), VM не обнаружит проблему.

**Это приемлемо** — binder является единственным источником истины для видимости, а VM доверяет ему.

---

## 11. Анализ Runtime

Runtime-слой (`Axiom.Runtime`) предоставляет два исполнительных движка:
- `Interpreter` — AST-walking с `RuntimeScope` chain
- `BytecodeVm` — slot-based без scope chain

Оба используют общие типы значений (`RuntimeValue`, `RuntimeArrayValue`, `RuntimeRecordValue` и т.д.) и общий `IRuntimeHost` для ввода/вывода.

**Качество интеграции:** Высокое. Общие типы значений обеспечивают консистентность между движками.

---

## 12. Взаимодействие с типами

Block scope не зависит от типов переменных. Любой тип (`INTEGER`, `LONG`, `DOUBLE`, `DECIMAL`, `STRING`, `BOOLEAN`, `ARRAY`, `TYPE`, `FUNCTION`) может быть объявлен в блочном scope и подчиняется тем же правилам видимости.

**Специальные случаи:**
- `ARRAY` — ссылочная семантика; массив, объявленный в блочном scope, может быть передан наружу через параметр, но сама переменная-ссылка исчезает после выхода из блока
- `TYPE` (record) — копирование при присваивании; record, объявленный в блочном scope, копируется при передаче наружу
- `FUNCTION` (value) — замыкание; функциональное значение может захватывать переменные из scope-ов, которые уже уничтожены

---

## 13. Взаимодействие со String

Строковые переменные в блочных scope-ах работают по общим правилам. Fixed strings (`DIM s AS STRING * N`) в блочных scope-ах получают заполнение пробелами при объявлении и уничтожаются при выходе из блока (Interpreter) или сохраняют слот до конца routine (BytecodeVm).

**Потенциальная проблема:** В BytecodeVm fixed-string переменная из IF-ветви, которая не выполнялась, будет содержать значение по умолчанию пустой строки, а не строку с заполнением пробелами. Это может привести к расхождению с Interpreter, где переменная не существует вовсе.

---

## 14. Взаимодействие с Array

Массивы в блочных scope-ах:
- Ссылочная семантика: если массив объявлен в блочном scope и передан наружу через параметр, массив остаётся в памяти, но переменная-ссылка исчезает из scope
- В Interpreter: после выхода из блока переменная-ссылка на массив уничтожается; если массив был передан наружу, он остаётся доступен через другую переменную
- В BytecodeVm: слот для переменной-ссылки сохраняется до конца routine, но не инициализирован

---

## 15. Взаимодействие с TYPE

Записи пользовательских типов в блочных scope-ах:
- Присваивание записи копирует значение
- Запись, объявленная в блочном scope, копируется при передаче наружу
- Массивы внутри записи сохраняют ссылочную семантику

---

## 16. Взаимодействие с Functions / SUB

### Локальные функции и block scope

Локальные `SUB`/`FUNCTION` могут быть объявлены внутри блочных scope-ов. Они захватывают переменные охватывающей routine через shared slots (BytecodeVm) или scope chain (Interpreter).

### Параметры и block scope

Параметры `SUB`/`FUNCTION` живут в scope самой routine, а не в отдельном block scope. Это корректно — параметры должны быть видны во всём теле функции.

### Рекурсия

Рекурсивные вызовы создают новые scope-ы (Interpreter) или новые фреймы (BytecodeVm) на каждом уровне рекурсии. Это корректно — каждая итерация рекурсии имеет свои локальные переменные.

---

## 17. Взаимодействие с Control Flow

### EXIT FOR / EXIT DO / CONTINUE FOR / CONTINUE WHILE / CONTINUE DO

Эти операторы корректно взаимодействуют с block scope:
- `EXIT FOR` и `EXIT DO` выходят из цикла и, соответственно, из scope цикла
- `CONTINUE FOR` и `CONTINUE WHILE` и `CONTINUE DO` переходят к следующей итерации, сохраняя текущий scope

### RETURN

`RETURN` из функции корректно покидает все вложенные scope-ы. В Interpreter это происходит через `ExecutionSignal.HasReturn`, который обрабатывается на каждом уровне `ExecuteStatementsInChildScope`. В BytecodeVm `Return` opcode очищает фрейм.

---

## 18. Взаимодействие с Error / Exception System

### TRY/CATCH/FINALLY

Каждая из трёх конструкций TRY, CATCH и FINALLY создаёт свой scope:
- TRY body — `BindStatementsInChildScope`
- CATCH — `EnterScope` + `TryDeclareScopedVariable` для переменной ошибки + `LeaveScope`
- FINALLY — `BindStatementsInChildScope`

### ERROR statement

Оператор `ERROR` не связан с block scope напрямую — он генерирует runtime-ошибку, которая может быть перехвачена CATCH.

### MB2001 в runtime

Если binder пропустил ошибку видимости (маловероятно), runtime не генерирует MB2001. BytecodeVm также не проверяет видимость на этапе выполнения. Это означает, что **все проверки видимости происходят только в binder**.

---

## 19. State / Lifetime / Ownership

### Interpreter

| Ресурс | Создаёт | Владеет | Изменяет | Уничтожает |
|---|---|---|---|---|
| RuntimeScope | `EnterScope()` | `_scopeStack` | `Define()`, `TryAssign()` | `LeaveScope()` → GC |
| Значение переменной | `Define()` | `RuntimeScope._values` | `TryAssign()` | scope destruction → GC |

### BytecodeVm

| Ресурс | Создаёт | Владеет | Изменяет | Уничтожает |
|---|---|---|---|---|
| `RuntimeValue[]` locals | `CreateFrame()` | `BytecodeCallFrame` | `StoreLocalValue()` | frame pop → GC |
| Слот переменной | `CreateLocalSlotMap()` | `BytecodeRoutine` | неизменяем | module unload → GC |

---

## 20. Edge Cases

### 20.1. Пустой блок

```basic
IF TRUE THEN
END IF
```
**Ожидание:** Корректное выполнение без ошибок.
**Факт:** Проверено smoke-тестами — работает корректно.

### 20.2. Вложенные блоки одного типа

```basic
IF TRUE THEN
    IF TRUE THEN
        DIM x AS INTEGER = 5
    END IF
    ' x здесь не видно
END IF
```
**Ожидание:** MB2001 при попытке использовать `x` после внутреннего `END IF`.
**Факт:** Binder корректно создаёт отдельные scope-ы для вложенных IF.

### 20.3. Sibling-ветви с одинаковым именем

```basic
IF TRUE THEN
    DIM x AS INTEGER = 1
ELSE
    DIM x AS STRING = "hello"
END IF
```
**Ожидание:** Корректное выполнение — это разные переменные в разных scope-ах.
**Факт:** Binder создаёт отдельные `VariableSymbol` для каждой ветви. BytecodeVm выделяет отдельные слоты.

### 20.4. Затенение переменной цикла FOR

```basic
DIM i AS INTEGER = 10
FOR i = 1 TO 5
    PRINT i
NEXT
PRINT i
```
**Ожидание:** Цикл использует предобъявленную переменную `i`. После цикла `i` равно 6.
**Факт:** Binder определяет, что `i` уже объявлена, и переиспользует её (`reusesExistingVariable = true`). Переменная цикла не удаляется после `NEXT`.

### 20.5. Объявление переменной в WHILE (per-iteration scope)

```basic
DIM counter AS INTEGER = 0
WHILE counter < 3
    DIM x AS INTEGER = counter
    PRINT x
    counter = counter + 1
END WHILE
```
**Ожидание:** В Interpreter `x` создаётся заново на каждой итерации. В BytecodeVm слот `x` сохраняется.
**Факт:** Это расхождение между движками. В Interpreter `x` инициализируется значением `counter` на каждой итерации. В BytecodeVm слот `x` сохраняет значение с предыдущей итерации, но перезаписывается `counter` при выполнении `DIM x AS INTEGER = counter`.

**Семантическое расхождение:** Если `DIM` в теле WHILE не выполняется (например, в ветви IF), то в Interpreter переменная не существует, а в BytecodeVm слот содержит значение по умолчанию или с предыдущей итерации.

### 20.6. RETURN из вложенного блока

```basic
FUNCTION F() AS INTEGER
    IF TRUE THEN
        DIM x AS INTEGER = 42
        RETURN x
    END IF
    RETURN 0
END FUNCTION
```
**Ожидание:** Функция возвращает 42.
**Факт:** Корректно работает в обоих движках. `RETURN` обрабатывается через `ExecutionSignal.HasReturn` (Interpreter) или `Return` opcode (BytecodeVm).

### 20.7. TRY/CATCH с переменной в CATCH

```basic
TRY
    ERROR "test"
CATCH e
    PRINT e.Message
END TRY
```
**Ожидание:** Переменная `e` видна только в теле CATCH.
**Факт:** Binder создаёт отдельный scope для CATCH с объявлением переменной `e`. Interpreter создаёт явный `RuntimeScope` с `Define(catchVariable, error)`.

### 20.8. FOR с STEP 0

```basic
FOR i = 1 TO 10 STEP 0
    PRINT i
NEXT
```
**Ожидание:** Runtime-ошибка MB3004.
**Факт:** Проверено — оба движка корректно генерируют ошибку.

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| IF block scope | RuntimeScope per branch | Flat slots | ✅ | ✅ | ⚠️ |
| FOR block scope | Single scope for loop | Flat slots | ✅ | ✅ | ⚠️ |
| WHILE per-iteration scope | New scope per iteration | Flat slots | ⚠️ | ✅ | ❌ |
| DO per-iteration scope | New scope per iteration | Flat slots | ⚠️ | ✅ | ❌ |
| SELECT CASE block scope | RuntimeScope per branch | Flat slots | ✅ | ✅ | ⚠️ |
| TRY/CATCH scope | Explicit catch scope | Flat slots | ✅ | ✅ | ⚠️ |
| Variable shadowing | MB2016 warning | MB2016 warning | ✅ | ✅ | ✅ |
| Variable out of scope | MB2001 error | MB2001 error | ✅ | ✅ | ✅ |
| Nested block scopes | Recursive scope chain | Flat slots | ✅ | ✅ | ⚠️ |
| Closure capture | Scope chain walk | Shared variable slots | ✅ | ✅ | ✅ |

**Классификация расхождений:**
- ⚠️ WHILE/DO per-iteration scope: **допустимое различие** — переменные из тела WHILE/DO получают новые значения при каждом `DIM`, поэтому observable behavior совпадает в большинстве случаев
- ⚠️ Слоты из невыполняемых ветвей: **допустимое различие** — слоты существуют, но не используются

---

## 22. Анализ производительности

### 22.1. Interpreter — cost per block

| Операция | Стоимость |
|---|---|
| `EnterScope()` | `new RuntimeScope(CurrentScope)` + `_scopeStack.Push()` — аллокация + O(1) push |
| `LeaveScope()` | `_scopeStack.Pop()` — O(1) |
| `Define()` | `Dictionary[variable] = value` — O(1) amortized |
| `TryLookup()` | Walk parent chain — O(depth) |

**Hot path:** `TryLookup()` вызывается при каждом чтении/записи переменной. Для глубоко вложенных блоков (10+ уровней) это может быть заметно.

### 22.2. BytecodeVm — cost per block

| Операция | Стоимость |
|---|---|
| `LoadLocal(slot)` | `frame._locals[slot]` — O(1) прямой доступ |
| `StoreLocal(slot, value)` | `frame._locals[slot] = value` — O(1) прямой доступ |
| Нет scope push/pop | 0 |

**Выигрыш:** BytecodeVm полностью устраняет накладные расходы на scope chain. Каждый доступ к переменной — O(1) прямой доступ к массиву.

### 22.3. Сравнение

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---|---|---|---|
| Глубоко вложенные блоки | O(depth) per lookup | O(1) per lookup | значительное | CLEAR WIN |
| Плоский код без вложенности | O(1) per lookup | O(1) per lookup | нет | PARITY |
| Много переменных в scope | O(1) per Define | O(1) per Define | нет | PARITY |
| Частые scope push/pop | аллокация + push/pop | нет операций | значительное | CLEAR WIN |

---

## 23. Качество Benchmark

Специализированных benchmark для block scope не существует. Существующие engine-comparison сценарии (`samples/smoke/engine-compare/`) покрывают общие сценарии, но не фокусируются на block scope.

**Проблема:** Невозможно точно измерить накладные расходы scope chain без адресного benchmark.

---

## 24. Hot Paths и Bottlenecks

### 24.1. Scope chain lookup в Interpreter

`RuntimeScope.TryLookup()` рекурсивно обходит цепочку parent. Для глубоко вложенных блоков (IF внутри FOR внутри WHILE внутри DO) это 4+ уровня рекурсии на каждый доступ к переменной.

**Направление оптимизации:** Можно добавить inline-кэш последнего найденного scope, но это сложность ради небольшого выигрыша.

### 24.2. Аллокация RuntimeScope

Каждый `EnterScope()` создаёт `new RuntimeScope(CurrentScope)`. Для WHILE/DO это происходит на каждой итерации. Для горячих циклов с миллионами итераций это нагрузка на GC.

**Направление оптимизации:** Можно использовать пул scope-ов, но это увеличивает сложность кода.

---

## 25. Архитектурные проблемы

### 25.1. Несогласованность per-iteration scope между WHILE/DO и FOR

**Проблема:** WHILE и DO создают новый scope на каждой итерации. FOR создаёт один scope на весь цикл.

**Причина:** FOR хранит переменную цикла, которая должна сохраняться между итерациями. WHILE/DO не имеют такой переменной.

**Следствие:** Объявление `DIM x AS INTEGER` в теле WHILE создаёт новую переменную на каждой итерации, а в теле FOR — одну переменную на весь цикл. Это семантически корректно, но может быть неочевидно пользователю.

**Оценка:** Это **допустимое различие**, а не баг. Переменная цикла FOR — особый случай.

### 25.2. BytecodeVm не реализует per-iteration scope

**Проблема:** В BytecodeVm переменные из тела WHILE/DO сохраняют значения между итерациями, потому что слоты не переинициализируются.

**Следствие:** Если в теле WHILE есть `DIM x AS INTEGER = 0`, то в Interpreter `x` сбрасывается в 0 на каждой итерации, а в BytecodeVm слот `x` сохраняет значение с предыдущей итерации (если `DIM` выполняется, значение перезаписывается; если нет — слот содержит старое значение).

**Реальный impact:** Минимальный, потому что `DIM` с инициализатором всегда перезаписывает значение. Проблема может проявиться только если `DIM` находится в условной ветви, которая не выполняется на каждой итерации.

**Оценка:** Это **допустимое различие** с низким реальным impact.

### 25.3. CollectDeclaredVariables собирает переменные из всех ветвей

**Проблема:** `CollectDeclaredVariables` обходит ВСЕ ветви IF/SELECT CASE и собирает переменные из невыполняемых ветвей.

**Следствие:** BytecodeVm выделяет слоты для переменных, которые могут никогда не использоваться. Это потенциальная трата памяти, но на практике объём пренебрежимо мал.

**Оценка:** Это **осознанное архитектурное решение** для упрощения lowering.

---

## 26. Дублирование логики

### 26.1. Scope creation в Binder и Interpreter

Binder создаёт `BoundScope` для каждого блока. Interpreter создаёт `RuntimeScope` для каждого блока. Это не дублирование — это два разных уровня абстракции (compile-time vs runtime).

### 26.2. CollectDeclaredVariables vs Binder scope walk

`CollectDeclaredVariables` в lowerer-е дублирует знание о том, какие конструкции создают scope-ы, которое уже есть в Binder. Если добавить новую конструкцию с block scope, нужно обновлять оба места.

**Направление:** Можно передавать информацию о scope-ах из Binder в Lowerer через metadata на BoundNode.

---

## 27. Single Source of Truth

**Единственный источник истины для block scope — Binder.**

- Binder определяет, какие конструкции создают scope-ы
- Binder определяет, какие переменные видны в каких scope-ах
- Binder генерирует MB2001 и MB2016
- Interpreter и BytecodeVm доверяют Binder

**Расхождение:** Lowerer должен знать о scope-ах для `CollectDeclaredVariables`, но получает информацию из bound tree, а не из Binder напрямую.

---

## 28. Legacy / Dead Code

Не обнаружено. Весь код, связанный с block scope, используется.

---

## 29. Надёжность и безопасность

### 29.1. RuntimeScope — утечка через замыкания

Если локальная функция захватывает переменную из scope, который должен быть уничтожен, `RuntimeScope` остаётся в памяти через ссылку в замыкании. Это корректное поведение, но может привести к утечкам памяти при длительных сессиях с большим количеством замыканий.

### 29.2. BytecodeVm — слоты из невыполняемых ветвей

Слоты для переменных из невыполняемых ветвей содержат `default(RuntimeValue)`. Если binder пропустит ошибку видимости, VM может прочитать неинициализированный слот. На практике это маловероятно.

---

## 30. Анализ тестов

### 30.1. Существующие тесты

| Тест | Тип | Покрытие |
|---|---|---|
| `BlockScopeSmokeSample_RunsSuccessfully` | Integration | IF block scope + FOR |
| `FullBlockScopeManualSmokeSample_RunsSuccessfully` | Integration | IF/ELSEIF/ELSE, SELECT CASE, FOR, WHILE, DO, SUB, FUNCTION |
| `Bind_ReportsShadowingEnclosingName` | Unit | Затенение имени в IF |
| `Bind_ForReusesPredeclaredControlVariableWithInitializer` | Unit | Переиспользование переменной FOR |
| Negative acceptance: `block_scope_visibility.axm` | Acceptance | MB2001 после END IF |
| Negative acceptance: `if_block_scope_escape.axm` | Acceptance | MB2001 после END IF |
| Negative acceptance: `if_shadowing_outer_variable.axm` | Acceptance | MB2016 |
| `Execute_CompiledSubBlockLocalProgram` | VM Unit | Блочные scope-ы в SUB через VM |
| `Execute_CompiledFunctionBlockLocalProgram` | VM Unit | Блочные scope-ы в FUNCTION через VM |

### 30.2. Качество тестов

**Позитивные сценарии:** Хорошее покрытие. Основные конструкции проверены через smoke-тесты.

**Негативные сценарии:** Базовое покрытие. Только MB2001 и MB2016 проверены через acceptance-тесты.

**VM-specific тесты:** Проверяют только SUB и FUNCTION с блочными scope-ами. Нет отдельных VM-тестов для WHILE/DO per-iteration scope.

---

## 31. Недостающие тесты

### Обычные сценарии
- WHILE с `DIM` в теле и проверкой начального значения на каждой итерации
- DO LOOP с `DIM` в теле
- SELECT CASE с `DIM` в разных ветвях и проверкой значений
- TRY/CATCH с `DIM` в CATCH
- Вложенные IF внутри FOR с одинаковыми именами переменных

### Отрицательные сценарии
- Использование переменной из WHILE после END WHILE
- Использование переменной из DO после LOOP
- Использование переменной из CASE в другой ветви CASE
- Использование переменной из CATCH после END TRY
- Попытка `REDIM` переменной из блочного scope после выхода из блока

### Граничные случаи
- `DIM` в теле WHILE, который выполняется 0 раз
- `DIM` в ELSE-ветви IF, где THEN выполняется
- `DIM` в CASE ELSE, где другой CASE совпадает
- Глубоко вложенные scope-ы (10+ уровней)
- Множественные `DIM` одного имени в разных sibling-ветвях

### Parity тесты
- Одна и та же программа с блочными scope-ами, запущенная через Interpreter и BytecodeVm, с одинаковым ожидаемым выводом

### Performance тесты
- WHILE с 1_000_000 итераций и `DIM` в теле: Interpreter vs BytecodeVm
- Глубоко вложенные scope-ы: Interpreter vs BytecodeVm

---

## 32. Анализ Samples

### Существующие samples

| Файл | Движок | Проверяемый результат | Рисковые сценарии |
|---|---|---|---|
| `samples/smoke/block_scope_semantics.axm` | Interpreter | Вывод: branch, 1, 2, done | IF block scope, FOR |
| `samples/smoke/block_scope_full_manual.axm` | Interpreter | 20 строк вывода | IF/ELSEIF/ELSE, SELECT CASE, FOR, WHILE, DO, SUB, FUNCTION |
| `samples/acceptance/negative/block_scope_visibility.axm` | Interpreter | MB2001 | IF escape |
| `samples/acceptance/negative/if_block_scope_escape.axm` | Interpreter | MB2001 | IF escape |

### Недостающие samples
- WHILE с `DIM` в теле (per-iteration scope)
- DO LOOP с `DIM` в теле
- SELECT CASE с `DIM` в разных ветвях
- TRY/CATCH с `DIM` в CATCH
- Вложенные блоки с затенением

---

## 33. Анализ документации

### Существующая документация

| Страница | Покрытие block scope |
|---|---|
| `docs/manual/if.html` | Упоминает блочную область видимости, ссылка на sample |
| `docs/manual/select-case.html` | Упоминает блочную область видимости |
| `docs/manual/for.html` | Упоминает область видимости переменной цикла |
| `docs/manual/try.html` | Упоминает отдельные области видимости |
| `docs/manual/dim.html` | Общие правила областей видимости |
| `docs/manual/variables-and-constants.html` | Упоминает текущую область видимости |
| `docs/manual/procedures-and-functions.html` | Правила области видимости для SUB/FUNCTION |
| `docs/manual/diagnostics.html` | MB2001 и MB2016 |

### Проблемы документации
1. **Нет выделенной страницы manual для block scope** — информация размазана по страницам операторов
2. **Нет единого объяснения концепции** — пользователь должен читать несколько страниц, чтобы понять общую модель
3. **WHILE/DO per-iteration scope не документирован** — нигде не объясняется, что `DIM` в теле WHILE создаёт новую переменную на каждой итерации
4. **Различие между FOR и WHILE/DO не объяснено** — пользователь может ожидать одинакового поведения

---

## 34. Интеграция с IDE

### Подсветка синтаксиса

Ключевые слова `IF`, `ELSEIF`, `ELSE`, `FOR`, `WHILE`, `DO`, `LOOP`, `SELECT`, `CASE`, `TRY`, `CATCH`, `FINALLY`, `DIM`, `CONST` подсвечиваются через `Axiom.xshd` и `Axiom.Dark.xshd`. Block scope не влияет на подсветку.

### Диагностика

MB2001 и MB2016 отображаются в панели `Diagnostics` IDE. Навигация к месту ошибки работает через `TextLocation`.

### Автодополнение

Не реализовано. Block scope не влияет на автодополнение.

---

## 35. Зависимости от Host / Platform

Block scope не зависит от host/platform. Вся логика scope-ов является чистой логикой языка и не использует Windows, .NET, файловую систему или GPU.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Отсутствие единой документации block scope
- SYS-033: Нет выделенной страницы manual
- SYS-033: WHILE/DO per-iteration scope не документирован
- SYS-033: Различие FOR vs WHILE/DO не объяснено

### ROOT-SYS-002: Схлопывание scope-ов в BytecodeVm
- SYS-025.2: Per-iteration scope WHILE/DO не реализован в VM
  - Примечание (2026-08-26): паритет подтверждён — и Interpreter, и BytecodeVm пересоздают переменные WHILE/DO на каждой итерации (вывод `1 1 1` в обоих движках). Запись устарела.
- SYS-025.3: Слоты из невыполняемых ветвей выделяются

### ROOT-SYS-003: Lowerer дублирует знание о scope-ах
- SYS-026.2: CollectDeclaredVariables знает о scope-creating конструкциях

---

## 37. Стабильные области

### Не следует трогать

1. **BoundScope** — чистый, проверенный дизайн с тремя parent pointers
2. **Binder.EnterScope/LeaveScope** — корректный механизм создания scope-ов
3. **Binder.TryDeclareScopedVariable** — правильная проверка дублирования и затенения
4. **RuntimeScope** — простой и эффективный parent-chain для Interpreter
5. **BytecodeVm flat slots** — осознанное архитектурное решение, работает корректно
6. **Двухфазный scope цикла FOR** — решает реальную проблему вывода типа

---

## 38. Возможности консолидации

### Единая страница manual для block scope

**Текущее состояние:** Информация размазана по 8+ страницам manual.
**Целевое состояние:** Выделенная страница `block-scope.html` с:
- Общим объяснением концепции
- Списком конструкций, создающих scope-ы
- Правилами видимости
- Примерами затенения
- Различием FOR vs WHILE/DO
- Ссылками на страницы отдельных операторов

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности корректно:
- Binder — compile-time scope analysis
- Interpreter — runtime scope chain
- BytecodeVm — flat slots (compile-time resolved)
- Lowerer — flattening bridge между Binder и BytecodeVm

Нет необходимости в разделении или перераспределении.

---

## 40. Кандидаты на удаление

Не обнаружено. Весь код block scope используется.

---

## 41. Готовность к дальнейшему развитию

### Готовность к Compiler
Высокая. Flat slots в BytecodeVm — это уже модель, близкая к компилятору. Binder корректно разделяет compile-time и runtime.

### Готовность к оптимизации
Средняя. Scope chain в Interpreter можно оптимизировать (inline cache), но это не приоритет.

### Готовность к расширению языка
Высокая. Механизм `EnterScope`/`LeaveScope` легко расширяется для новых конструкций.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P3 | DOCS | Manual | Нет выделенной страницы manual для block scope | Создана docs/manual/variables-scope.html (2026-08-26) | Выполнено | RESOLVED |
| SYS-002 | P3 | DOCS | Manual | WHILE/DO per-iteration scope не документирован | Описано на docs/manual/variables-scope.html (раздел «Переменные в блоках», 2026-08-26) | Выполнено | RESOLVED |
| SYS-003 | P3 | DOCS | Manual | Различие FOR vs WHILE/DO scope не объяснено | Описано на docs/manual/variables-scope.html (2026-08-26) | Выполнено | RESOLVED |
| SYS-004 | P2 | TESTS | Tests | Нет тестов WHILE с DIM в теле | Поиск по tests/ | Не написаны | CONFIRMED |
| SYS-005 | P2 | TESTS | Tests | Нет тестов DO LOOP с DIM в теле | Поиск по tests/ | Не написаны | CONFIRMED |
| SYS-006 | P2 | TESTS | Tests | Нет тестов SELECT CASE с DIM в разных ветвях | Поиск по tests/ | Не написаны | CONFIRMED |
| SYS-007 | P2 | TESTS | Tests | Нет parity-тестов block scope (Interpreter vs VM) | Поиск по tests/ | Не написаны | CONFIRMED |
| SYS-008 | P3 | TESTS | Tests | Нет тестов TRY/CATCH с DIM в CATCH | Поиск по tests/ | Не написаны | CONFIRMED |
| SYS-009 | P4 | ARCHITECTURE | Lowerer | CollectDeclaredVariables дублирует знание о scope-ах | Анализ кода | Архитектурное решение | CONFIRMED |
| SYS-010 | P4 | ARCHITECTURE | VM | Слоты из невыполняемых ветвей выделяются | CollectDeclaredVariables | Осознанное решение | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
- TASK-001: Запустить существующие тесты block scope и убедиться в их прохождении
- TASK-002: Создать benchmark для измерения накладных расходов scope chain

### ЭТАП 1 — Исправление критических проблем корректности
- Нет критических проблем. Все P0 отсутствуют.

### ЭТАП 2 — Стабилизация семантики
- TASK-003: Добавить parity-тесты: одна программа через Interpreter и BytecodeVm с одинаковым выводом

### ЭТАП 3 — Архитектурное улучшение
- Нет необходимости. Архитектура стабильна.

### ЭТАП 4 — Оптимизация производительности
- Нет необходимости. Производительность адекватна.

### ЭТАП 5 — Усиление тестирования
- TASK-004: Добавить тест WHILE с DIM в теле
- TASK-005: Добавить тест DO LOOP с DIM в теле
- TASK-006: Добавить тест SELECT CASE с DIM в разных ветвях
- TASK-007: Добавить тест TRY/CATCH с DIM в CATCH
- TASK-008: Добавить негативные тесты: переменная из WHILE после END WHILE
- TASK-009: Добавить негативные тесты: переменная из DO после LOOP
- TASK-010: Добавить негативные тесты: переменная из CASE в другой ветви CASE

### ЭТАП 6 — Обновление документации
- TASK-011: Создать выделенную страницу manual `block-scope.html`
- TASK-012: Добавить объяснение WHILE/DO per-iteration scope
- TASK-013: Добавить объяснение различия FOR vs WHILE/DO scope

### ЭТАП 7 — Обновление IDE
- Нет необходимости. IDE корректно отображает MB2001 и MB2016.

### ЭТАП 8 — Подготовка к дальнейшему развитию
- Нет необходимости. Подсистема готова к расширению.

---

## 44. Граф зависимостей задач

```
TASK-001 (запуск тестов)
    ↓
TASK-003 (parity-тесты)
    ↓
TASK-004..010 (новые тесты)
    ↓
TASK-011..013 (документация)
```

TASK-002 (benchmark) — независимая задача.

---

## 45. Definition of Done

Подсистема Block Scope считается завершённой, когда:

1. **Корректность:** Все существующие тесты проходят. Binder корректно генерирует MB2001 и MB2016.
2. **Edge cases:** WHILE/DO per-iteration scope протестирован. SELECT CASE с DIM в разных ветвях протестирован.
3. **Error handling:** MB2001 генерируется при использовании переменной после выхода из блока. MB2016 генерируется при затенении.
4. **Interpreter:** RuntimeScope chain работает корректно для всех конструкций.
5. **Bytecode VM:** Flat slots работают корректно для всех конструкций. Parity с Interpreter подтверждена.
6. **Тестирование:** Есть parity-тесты, негативные тесты, граничные тесты.
7. **Документация:** Выделенная страница manual существует. WHILE/DO per-iteration scope объяснён.
8. **IDE:** MB2001 и MB2016 отображаются в панели Diagnostics.

---

## 46. ТОП-20 проблем корректности и архитектуры

1. **SYS-001** (P3): Нет выделенной страницы manual для block scope
2. **SYS-002** (P3): WHILE/DO per-iteration scope не документирован
3. **SYS-003** (P3): Различие FOR vs WHILE/DO scope не объяснено
4. **SYS-004** (P2): Нет тестов WHILE с DIM в теле
5. **SYS-005** (P2): Нет тестов DO LOOP с DIM в теле
6. **SYS-006** (P2): Нет тестов SELECT CASE с DIM в разных ветвях
7. **SYS-007** (P2): Нет parity-тестов block scope
8. **SYS-008** (P3): Нет тестов TRY/CATCH с DIM в CATCH
9. **SYS-009** (P4): Lowerer дублирует знание о scope-ах
10. **SYS-010** (P4): Слоты из невыполняемых ветвей выделяются

(Проблемы P0/P1 отсутствуют)

---

## 47. ТОП-20 задач по производительности

1. **PERF-001**: Benchmark scope chain lookup в Interpreter (измерение текущей производительности)
2. **PERF-002**: Benchmark per-iteration scope creation в WHILE/DO (измерение накладных расходов аллокации)

(Специфических проблем производительности не обнаружено. Текущая производительность адекватна.)

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка (0-10) | Объяснение |
|---|---|---|
| Корректность | 9 | Все основные сценарии работают корректно. Нет P0/P1 проблем. |
| Семантика | 9 | Семантика блочных scope-ов чётко определена и корректно реализована. |
| Архитектура | 9 | Чистое разделение на три уровня. BoundScope/RuntimeScope/flat slots. |
| Runtime | 9 | RuntimeScope корректно работает для Interpreter. Flat slots для VM. |
| Interpreter | 9 | Корректно реализует per-iteration scope для WHILE/DO, single scope для FOR. |
| Bytecode VM | 8 | Flat slots работают корректно, но per-iteration scope не реализован (допустимое различие). |
| Производительность | 8 | Scope chain lookup O(depth) в Interpreter, O(1) в VM. Специальных benchmark нет. |
| Память | 9 | Нет утечек. Аллокация scope-ов корректно управляется GC. |
| Error handling | 9 | MB2001 и MB2016 корректно генерируются в Binder. |
| Tests | 7 | Позитивные сценарии покрыты хорошо. Негативных и parity-тестов недостаточно. |
| Samples | 7 | Есть smoke-тесты. Нет samples для WHILE/DO/SELECT CASE с DIM. |
| Documentation | 6 | Информация размазана по страницам. Нет выделенной страницы. WHILE/DO не объяснён. |
| IDE | 8 | MB2001 и MB2016 отображаются корректно. |
| Maintainability | 9 | Чистый код, легко расширяемый. |
| Reliability | 9 | Нет известных проблем надёжности. |
| Future readiness | 9 | Готово к расширению языка и будущему компилятору. |

**Средняя оценка:** 8.4 / 10

---

## Главный контрольный вопрос

> Можно ли считать подсистему Block Scope достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

### Основные blockers
Нет критических blockers. Все P0 отсутствуют.

### Что нужно сделать в первую очередь
1. Добавить parity-тесты (Interpreter vs BytecodeVm) для block scope
2. Добавить тесты WHILE/DO с DIM в теле
3. Создать выделенную страницу manual для block scope

### Что можно отложить
1. Benchmark scope chain lookup
2. Оптимизация аллокации scope-ов
3. Передача scope metadata из Binder в Lowerer

### Что не следует трогать
1. BoundScope — стабильный дизайн
2. RuntimeScope — корректная реализация
3. BytecodeVm flat slots — осознанное архитектурное решение
4. Двухфазный scope цикла FOR — решает реальную проблему
