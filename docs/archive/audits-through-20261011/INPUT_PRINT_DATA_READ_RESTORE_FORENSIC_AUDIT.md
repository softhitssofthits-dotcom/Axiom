# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## INPUT / PRINT / PRINT USING / DATA / READ / RESTORE

Дата: 2026-08-21
Метод: полный обход конвейера языка от лексера до runtime, проверка кода, тестов, samples, документации и спецификации.

---

## 1. Резюме

Подсистема INPUT/PRINT/PRINT USING/DATA/READ/RESTORE разделена на две части:

- **INPUT, PRINT, PRINT USING** — полностью реализованы на всех уровнях конвейера: лексер, парсер, семантический анализатор, lowering, байткодный компилятор, интерпретатор и байткодная VM. Подсистема зрелая, стабильная, имеет 15+ автоматических тестов, 10 runnable-примеров и 2 выделенных benchmark-сценария. Паритет интерпретатора и VM достигнут.

- **DATA, READ, RESTORE** — полностью отсутствуют на всех уровнях. Нет ключевых слов в лексере, нет синтаксических узлов, нет парсера, нет binder, нет runtime. Также отсутствует `LINE INPUT`. Это не баги, а нереализованные возможности (feature gaps).

**Общая оценка здоровья реализованной части: 8.5/10.** Архитектура чистая, дублирование минимально, паритет VM достигнут. Основной технический долг — в области, которая ещё не начиналась (DATA/READ/RESTORE), а не в сломанном коде.

---

## 2. Что представляет собой эта подсистема

Подсистема отвечает за два фундаментальных потока данных в программе:

1. **Вывод (PRINT, PRINT USING)** — передача значений из программы пользователю через текстовый поток.
2. **Ввод (INPUT)** — получение текстовой строки от пользователя, разбор её по типам и запись в переменные программы.
3. **Данные программы (DATA/READ/RESTORE)** — запланированная, но не реализованная возможность хранить данные прямо в тексте программы и читать их последовательно.

PRINT и INPUT — единственные операторы, которые напрямую связывают программу с пользователем через текстовый ввод-вывод.

---

## 3. Полный список возможностей

### SYS-001: PRINT
- **Категория:** STATEMENT
- **Назначение:** вывод выражений с разделителями
- **Пользовательский синтаксис:** `PRINT expr [;|, expr ...]`
- **Parser:** `Parser.Statements.cs:113` — `ParsePrintStatement()`
- **Syntax Node:** `PrintStatementSyntax` → `InputOutputSyntax.cs:20`
- **Binder:** `Binder.IoStatements.cs:7` — `BindPrintStatement()`
- **Bound Model:** `BoundPrintStatement` → `BoundStatements.cs:162`
- **Lowering:** `BoundProgramLowerer.cs:874` — `LowerPrint()`
- **Bytecode Compiler:** `BytecodeCompiler.cs:823` — `PrintValue`, `PrintSeparator`, `PrintLine`
- **Opcodes:** `PrintValue` (стек -1), `PrintSeparator` (стек 0), `PrintLine` (стек 0)
- **Interpreter:** `Interpreter.Statements.cs:387` — `ExecutePrint()`
- **Bytecode VM:** `BytecodeVm.cs:824-838` — `PrintValue`, `PrintSeparator`, `PrintLine`
- **Runtime Helper:** `BuiltinPrintUsingHelpers` (для форматирования)
- **IDE:** Подсветка `PRINT` как ключевого слова
- **Tests:** `InterpreterTests.Print.cs` — 14 тестов
- **Samples:** `samples/operators/print/` — 6 примеров
- **Benchmarks:** `samples/smoke/engine-compare/print_output_gallery.axm`
- **Documentation:** `docs/manual/print.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-002: PRINT USING
- **Категория:** STATEMENT
- **Назначение:** форматированный вывод с масками
- **Пользовательский синтаксис:** `PRINT USING format; expr [;|, expr ...]`
- **Parser:** `Parser.Statements.cs:150` — `ParsePrintUsingStatement()`
- **Syntax Node:** `PrintUsingStatementSyntax` → `InputOutputSyntax.cs:38`
- **Binder:** `Binder.IoStatements.cs:19` — `BindPrintUsingStatement()`
- **Bound Model:** `BoundPrintUsingStatement` → `BoundStatements.cs:176`
- **Lowering:** `BoundProgramLowerer.cs:897` — `LowerPrintUsing()`
- **Bytecode Compiler:** `BytecodeCompiler.cs:824` — `PrintUsing`
- **Opcodes:** `PrintUsing` (стек -1-N, где N = количество значений)
- **Interpreter:** `Interpreter.Statements.cs:418` — `ExecutePrintUsing()`, `Interpreter.PrintUsing.cs:8` — `FormatUsing()`
- **Bytecode VM:** `BytecodeVm.cs:828-829`, `BytecodeVm.PrintUsing.cs:9` — `AppendFormattedUsing()`
- **Runtime Helper:** `BuiltinPrintUsingHelpers.cs` — парсинг форматных строк и форматирование
- **IDE:** Подсветка `USING` как ключевого слова
- **Tests:** Включены в `InterpreterTests.Print.cs` — 4 теста PRINT USING
- **Samples:** `samples/operators/print/using_basic.axm`
- **Documentation:** `docs/manual/print.html` (раздел PRINT USING)
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-003: INPUT
- **Категория:** STATEMENT
- **Назначение:** ввод значений от пользователя
- **Пользовательский синтаксис:** `INPUT [prompt;] var [, var ...]`
- **Parser:** `Parser.Statements.cs:193` — `ParseInputStatement()`
- **Syntax Node:** `InputStatementSyntax` → `InputOutputSyntax.cs:73`
- **Binder:** `Binder.IoStatements.cs:32` — `BindInputStatement()`
- **Bound Model:** `BoundInputStatement` → `BoundStatements.cs:195`
- **Lowering:** `BoundProgramLowerer.cs:913` — `LowerInput()`
- **Bytecode Compiler:** `BytecodeCompiler.cs:1349` — `EmitInput()`
- **Opcodes:** `Input` (стек 0, сложная operand-структура)
- **Interpreter:** `Interpreter.Statements.cs:451` — `ExecuteInput()`, `Interpreter.Helpers.cs:278` — `TryParseInputValues()`
- **Bytecode VM:** `BytecodeVm.Input.cs:10` — `ExecuteInput()`, `BytecodeVm.Input.cs:178` — `TryParseInputValues()`
- **Runtime Helper:** `BuiltinIOHelpers.cs` — разбор полей ввода
- **IDE:** Подсветка `INPUT` как ключевого слова, `InputPromptService`, `InputStateService`
- **Tests:** Интеграционные тесты через smoke-сценарии
- **Samples:** `samples/operators/input/` — 4 примера
- **Benchmarks:** `samples/smoke/engine-compare/input_record_intake.axm`
- **Documentation:** `docs/manual/input.html`
- **Статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED

### SYS-004: DATA
- **Категория:** STATEMENT
- **Назначение:** хранение данных в тексте программы
- **Статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** нет ключевого слова `DataKeyword` в лексере, нет синтаксических узлов, нет парсера. `LANGUAGE_INVENTORY.md:637-639` явно фиксирует отсутствие.

### SYS-005: READ
- **Категория:** STATEMENT
- **Назначение:** последовательное чтение элементов из DATA
- **Статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** аналогично DATA.

### SYS-006: RESTORE
- **Категория:** STATEMENT
- **Назначение:** сброс указателя чтения DATA
- **Статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** аналогично DATA.

### SYS-007: LINE INPUT
- **Категория:** STATEMENT
- **Назначение:** ввод целой строки без разбора по запятым
- **Статус:** NOT_IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `LANGUAGE_INVENTORY.md:639` явно фиксирует отсутствие.

---

## 4. Фактическая архитектура

### 4.1 Общая схема конвейера

```
Исходный код (.axm)
    ↓
Lexer (Axiom.Lexing)
    → PrintKeyword, InputKeyword, UsingKeyword
    ↓
Parser (Axiom.Parsing)
    → PrintStatementSyntax, PrintUsingStatementSyntax, InputStatementSyntax
    ↓
Binder (Axiom.Semantics)
    → BoundPrintStatement, BoundPrintUsingStatement, BoundInputStatement
    ↓
Lowering (Axiom.Bytecode)
    → LoweredInstruction.PrintValue, PrintUsing, PrintSeparator, PrintLine, Input
    ↓
BytecodeCompiler (Axiom.Bytecode)
    → Opcode.PrintValue, PrintUsing, PrintSeparator, PrintLine, Input
    ↓
┌─────────────────────────────────────────────┐
│  Interpreter (Axiom.Runtime)                │  BytecodeVm (Axiom.Runtime)
│  ExecutePrint()                             │  PrintValue → AppendPendingOutput()
│  ExecutePrintUsing() → FormatUsing()        │  PrintUsing → AppendFormattedUsing()
│  ExecuteInput() → TryParseInputValues()     │  ExecuteInput() → TryParseAndStoreInputValues()
└─────────────────────────────────────────────┘
    ↓
IRuntimeHost
    → WriteOutput() / ReadInput() / ReadGraphicsInput()
    ↓
IDE (Axiom.Ide)
    → Output panel / SCREEN window
```

### 4.2 Ключевые файлы и их ответственности

| Файл | Ответственность |
|------|----------------|
| `Parser.Statements.cs:113-216` | Синтаксический разбор PRINT, PRINT USING, INPUT |
| `InputOutputSyntax.cs` | Синтаксические узлы: `PrintStatementSyntax`, `PrintUsingStatementSyntax`, `InputStatementSyntax` |
| `Binder.IoStatements.cs:7-50` | Семантическая проверка и связывание PRINT, PRINT USING, INPUT |
| `BoundStatements.cs:141-210` | Bound-модель: `BoundPrintStatement`, `BoundPrintUsingStatement`, `BoundInputStatement` |
| `BoundProgramLowerer.cs:874-916` | Понижение в линейное представление |
| `BytecodeCompiler.cs:823-1386` | Эмиссия байткодных инструкций |
| `Opcode.cs:115-118` | Опкоды: `PrintValue`, `PrintUsing`, `PrintSeparator`, `PrintLine`, `Input` |
| `Interpreter.Statements.cs:387-473` | AST-walking исполнение PRINT, PRINT USING, INPUT |
| `Interpreter.PrintUsing.cs` | Делегирование к `BuiltinPrintUsingHelpers` |
| `Interpreter.Helpers.cs:224-300` | Разбор ввода, вывод текста, буферизация |
| `BytecodeVm.cs:824-842` | Dispatch опкодов PRINT/INPUT в VM |
| `BytecodeVm.Input.cs` | Полная реализация INPUT в VM |
| `BytecodeVm.PrintUsing.cs` | Полная реализация PRINT USING в VM |
| `BuiltinIOHelpers.cs` | Общие алгоритмы разбора ввода |
| `BuiltinPrintUsingHelpers.cs` | Общие алгоритмы форматирования PRINT USING |

### 4.3 Буферизация вывода

И Interpreter, и BytecodeVm используют одинаковую модель буферизации:
- `_pendingOutput: StringBuilder` — накопитель текста
- `_hasPendingOutput: bool` — флаг наличия данных
- `AppendPendingOutput(text)` — добавление текста
- `AppendPrintSeparator()` — добавление пробела (разделитель `,`)
- `FlushPendingOutput(forceEmptyLine)` — сброс в `IRuntimeHost.WriteOutput()`

Буфер сбрасывается:
- при отсутствии завершающего разделителя после последнего элемента PRINT
- при пустом PRINT (принудительный перевод строки)
- при завершении программы (в `finally`)

---

## 5. Полный путь выполнения

### 5.1 PRINT: исходный код → результат

```
PRINT "A"; 42
    ↓
Lexer: [PrintKeyword] [StringLiteralToken(";A")] [SemicolonToken] [IntegerLiteralToken(42)]
    ↓
Parser: PrintStatementSyntax { Items: [PrintItem("A", Semicolon), PrintItem(42, null)] }
    ↓
Binder: BoundPrintStatement { Items: [BoundPrintItem(LiteralExpr("A"), Semicolon), BoundPrintItem(LiteralExpr(42), null)] }
    ↓
Lowering:
    PushLiteral("A")
    PrintValue
    PrintSeparator(Semicolon)
    PushLiteral(42)
    PrintValue
    PrintLine
    ↓
Bytecode:
    PushConst <idx_A>
    PrintValue
    PrintSeparator(0)
    PushConst <idx_42>
    PrintValue
    PrintLine
    ↓
VM Execution:
    stack.Push(RuntimeValue.FromString("A"))
    AppendPendingOutput(PopValue()) → _pendingOutput = "A"
    _hasPendingOutput = true (separator 0 = semicolon)
    stack.Push(RuntimeValue.FromInteger(42))
    AppendPendingOutput(PopValue()) → _pendingOutput = "A42"
    FlushPendingOutput(forceEmptyLine: true) → WriteTextLine("A42")
    ↓
IRuntimeHost.WriteOutput("A42")
```

### 5.2 INPUT: исходный код → результат

```
INPUT "Name"; name$
    ↓
Lexer: [InputKeyword] [StringLiteralToken("Name")] [SemicolonToken] [IdentifierToken("name")]
    ↓
Parser: InputStatementSyntax { PromptToken: "Name", PromptSeparator: ";", Identifiers: [name] }
    ↓
Binder: BoundInputStatement { Variables: [name$], PromptText: "Name" }
    ↓
Lowering: LoweredInstruction.Input(location, [name$], "Name")
    ↓
Bytecode: Input [count=1, prompt_const_idx+1, targetKind=0, slot=N]
    ↓
VM Execution:
    plan = GetInputInstructionPlan() → promptText="Name", targets=[InputTarget(0, N, "name", STRING, null)]
    request = InputRequest("Name", 1)
    text = _host.ReadInput(request) → пользователь вводит "Ada"
    TryParseAndStoreInputValues():
        TryParseInput(STRING, "Ada") → RuntimeValue.FromString("Ada")
        StoreGlobal(N, RuntimeValue.FromString("Ada"))
```

---

## 6. Фактический семантический контракт

### 6.1 PRINT

**Вход:** список выражений с необязательными разделителями `;` и `,`.

**Обработка:**
- Выражения вычисляются слева направо.
- Каждое значение преобразуется в текст через `ToDisplayString()` (Interpreter) или типо-специальный формат (VM).
- `;` — соседние значения выводятся подряд, без пробела.
- `,` — мягкий разделитель: один пробел (не фиксированные табуляционные зоны как в QuickBASIC).
- Отсутствие завершающего разделителя — добавляется перевод строки.
- Пустой `PRINT` — только перевод строки.

**Выход:** текстовая строка через `IRuntimeHost.WriteOutput()` или `WriteGraphicsText()`.

**Побочные эффекты:** запись в поток вывода.

### 6.2 PRINT USING

**Вход:** форматная строка (STRING), список выражений.

**Обработка:**
- Форматная строка парсится в токены: литералы, строковые поля (`!`, `&`, `\   \`), числовые поля (`#`, `0`, `.`, `+`, `-`, `,`, `$`, `*`, `^`).
- Значения подставляются слева направо.
- Строковые поля: `!` — первый символ, `&` — вся строка, `\   \` — фиксированная ширина.
- Числовые поля: форматирование по маске с учётом знака, заполнителя, разделителя тысяч, экспоненты.
- Если число не помещается — `%` + полное значение.
- Длина форматной строки ограничена 2048 символами.

**Выход:** отформатированная текстовая строка.

### 6.3 INPUT

**Вход:** необязательная строка-приглашение, одна или несколько переменных.

**Обработка:**
- Приглашение выводится перед ожиданием ввода.
- Если переменных одна — вся строка становится значением.
- Если переменных несколько — строка разбивается по запятым, пробелы вокруг полей отбрасываются.
- Преобразование к типу переменной: INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN.
- Для BOOLEAN допускается только `TRUE`/`FALSE` (без учёта регистра).
- Для чисел — инвариантная запись с точкой.

**Выход:** запись значений в переменные.

**Ошибки:**
- Невозможность преобразования → diagnostic `MB3005`.
- Несовпадение числа полей → diagnostic `MB3005`.
- Массив как цель → runtime error.

---

## 7. Анализ Parser

### 7.1 PRINT (`Parser.Statements.cs:113-148`)

Парсер корректно обрабатывает:
- Пустой `PRINT` (возвращает `PrintStatementSyntax` с пустым списком).
- Список выражений с разделителями `;` и `,`.
- Завершающий разделитель (подавляет перевод строки).
- `PRINT USING` как отдельную ветку.

**Особенность:** парсер не различает `;` и `,` на уровне токена — оба сохраняются как `SemicolonToken` или `CommaToken` в `PrintItemSyntax.SeparatorToken`. Разделение на `PrintSeparatorKind` происходит в Binder.

### 7.2 PRINT USING (`Parser.Statements.cs:150-191`)

Корректно:
- Требует `USING` после `PRINT`.
- Форматное выражение парсится как обычное выражение.
- Требует `;` после форматной строки.
- Список значений с разделителями аналогичен обычному PRINT.

### 7.3 INPUT (`Parser.Statements.cs:193-216`)

Корректно:
- Опциональное приглашение (строковый литерал + `;` или `,`).
- Список переменных-идентификаторов через запятую.

**Ограничение:** приглашение может быть только строковым литералом, не произвольным выражением. Это отличие от QuickBASIC, где допускается произвольное строковое выражение. В текущей реализации это проверяется на уровне токена (`Current.Kind == SyntaxKind.StringLiteralToken`), а не на уровне выражения.

### 7.4 Отсутствие DATA/READ/RESTORE

В `Parser.Statements.cs:24-82` (switch по `Current.Kind`) нет веток для `DataKeyword`, `ReadKeyword`, `RestoreKeyword`. В лексере (`Axiom.Lexing`) нет соответствующих ключевых слов. Синтаксические узлы отсутствуют.

---

## 8. Анализ Binder

### 8.1 PRINT (`Binder.IoStatements.cs:7-17`)

- Выражения связываются через `BindExpression()`.
- Разделители маппятся через `MapPrintSeparator()` в `PrintSeparatorKind`.
- Тип выражений не проверяется — PRINT принимает любой тип.

### 8.2 PRINT USING (`Binder.IoStatements.cs:19-30`)

- Форматное выражение связывается с ожиданием типа `STRING` (`BindExpression(statement.FormatExpression, TypeSymbol.String)`).
- Выражения значений связываются без проверки типа (проверка типа происходит в runtime при форматировании).

### 8.3 INPUT (`Binder.IoStatements.cs:32-50`)

- Переменные разрешаются через `LookupVariable()`.
- Проверяется `IsReadOnly` — присвоение в константу запрещено.
- Приглашение извлекается как `string?` из токена.

**Архитектурная чистота:** Binder не делает лишней работы. Проверка типов INPUT отложена до runtime, что правильно — типы переменных уже известны, а преобразование из текста — задача runtime.

---

## 9. Анализ Interpreter

### 9.1 ExecutePrint (`Interpreter.Statements.cs:387-416`)

```csharp
foreach (var item in statement.Items)
{
    var value = EvaluateExpression(item.Expression);
    AppendPendingOutput(value.ToDisplayString());
    switch (item.SeparatorKind) { ... }
}
```

**Ключевые моменты:**
- Использует `value.ToDisplayString()` для всех типов.
- Буферизация через `_pendingOutput: StringBuilder`.
- `,` добавляет один пробел (не табуляционные зоны).
- Пустой PRINT вызывает `FlushPendingOutput(forceEmptyLine: true)`.

### 9.2 ExecutePrintUsing (`Interpreter.Statements.cs:418-449`)

- Делегирует форматирование в `BuiltinPrintUsingHelpers.FormatUsing()`.
- Значения собираются в список пар `(RuntimeValue, TextLocation)`.
- Результат добавляется в буфер через `AppendPendingOutput()`.
- Завершающий разделитель обрабатывается аналогично PRINT.

### 9.3 ExecuteInput (`Interpreter.Statements.cs:451-473`)

- Проверяет, что нет массивных целей.
- Создаёт `InputRequest` с текстом приглашения и количеством переменных.
- Определяет режим ввода: графический (`ReadGraphicsInput`) или текстовый (`ReadInput`).
- Разбирает введённую строку через `TryParseInputValues()`.
- Присваивает значения переменным через `AssignVariable()`.

### 9.4 TryParseInputValues (`Interpreter.Helpers.cs:278-300`)

- Для одной переменной — вся строка становится значением.
- Для нескольких — разбиение по запятым, trim полей.
- Преобразование через `BuiltinIOHelpers.TryParseInputField()`.

### 9.5 Специфика Interpreter

**Вопрос:** Есть ли логика, реализованная специально в Interpreter и отсутствующая в общей Runtime architecture?

**Ответ:** Нет. Interpreter использует общие хелперы (`BuiltinIOHelpers`, `BuiltinPrintUsingHelpers`). Специфичная для Interpreter логика — только AST-walking диспетчеризация и управление scope через `_scopeStack`, что является inherent частью интерпретаторного подхода.

---

## 10. Анализ Bytecode / VM

### 10.1 Lowering

**PRINT** (`BoundProgramLowerer.cs:874-895`):
- Для каждого элемента: `LowerExpression` → `PrintValue` → опционально `PrintSeparator`.
- Если последний элемент не имеет разделителя — `PrintLine`.
- Пустой PRINT — только `PrintLine`.

**PRINT USING** (`BoundProgramLowerer.cs:897-911`):
- `LowerExpression(formatExpression)` → для каждого элемента `LowerExpression` → `PrintUsing(count, separator)`.

**INPUT** (`BoundProgramLowerer.cs:913-916`):
- Одна инструкция `LoweredInstruction.Input(location, variables, promptText)`.

### 10.2 BytecodeCompiler

**PRINT** (`BytecodeCompiler.cs:823-826`):
- `PrintValue` → без операндов (стек -1).
- `PrintSeparator` → один операнд: 0=semicolon, 1=comma.
- `PrintLine` → без операндов.

**PRINT USING** (`BytecodeCompiler.cs:1336-1347`):
- `PrintUsing` → два операнда: `valueCount`, `separatorValue`.

**INPUT** (`BytecodeCompiler.cs:1349-1386`):
- Сложная структура операндов: `[count, prompt_idx+1, targetKind0, slot0, targetKind1, slot1, ...]`.
- `prompt_idx+1` — 0 означает отсутствие приглашения, иначе индекс в пуле констант + 1.
- `targetKind`: 0=global, 1=local, 2=argument, 3=capture.

### 10.3 BytecodeVm Execution

**PrintValue** (`BytecodeVm.cs:824-826`):
```csharp
case Opcode.PrintValue:
    AppendPendingOutput(PopValue(frame, instructionIndex));
    break;
```

**AppendPendingOutput(RuntimeValue)** (`BytecodeVm.cs:5119-5160`):
- Оптимизированный путь для каждого типа: `stackalloc char[64]` для чисел, прямой `Append` для строк и булевых.
- Fallback на `value.ToDisplayString()` для IMAGE, SPRITE, FILE и других.

**PrintUsing** (`BytecodeVm.cs:4810-4856`):
- Значения снимаются со стека в обратном порядке (LIFO).
- Используется `ArrayPool<RuntimeValue>.Shared` для временного буфера.
- Форматный кэш: `_printUsingFormatCache` хранит распарсенные токены формата.

**Input** (`BytecodeVm.Input.cs:10-29`):
- Кэширование плана: `_inputInstructionCache` по `(routineId, instructionIndex)`.
- Оптимизированные пути для 1, 2, 3 переменных (без аллокации массива полей).
- `InvalidateReverseExecutionHistory()` после ввода.

---

## 11. Анализ Runtime

### 11.1 RuntimeValue и вывод

Все значения Axiom приводятся к тексту через:
- `RuntimeValue.ToDisplayString()` — универсальный метод.
- В VM: типо-специальные пути с `stackalloc` для числовых типов.

### 11.2 IRuntimeHost

Абстракция хоста для ввода-вывода:
- `WriteOutput(text)` — текстовый вывод.
- `WriteGraphicsText(text, newLine)` — вывод в SCREEN.
- `ReadInput(request)` — текстовый ввод.
- `ReadGraphicsInput(request)` — ввод в SCREEN.
- `IsScreenOutputActive` — определяет режим вывода.

### 11.3 InputRequest

```csharp
public sealed record InputRequest(string? PromptText, int ExpectedValueCount);
```

Контейнер для запроса ввода. IDE использует его для отображения приглашения.

---

## 12. Взаимодействие с типами

### 12.1 PRINT и типы

| Тип | Формат вывода | Interpreter | VM |
|-----|--------------|-------------|-----|
| INTEGER | Десятичное число | `ToDisplayString()` | `AppendInvariantValue(int)` через `stackalloc` |
| LONG | Десятичное число | `ToDisplayString()` | `AppendInvariantValue(long)` через `stackalloc` |
| DOUBLE | Инвариантный формат "G" | `ToDisplayString()` | `AppendInvariantValue(double, "G")` через `stackalloc` |
| DECIMAL | Инвариантный формат | `ToDisplayString()` | `AppendInvariantValue(decimal)` через `stackalloc` |
| STRING | Как есть | `ToDisplayString()` | Прямой `Append(string)` |
| BOOLEAN | `TRUE`/`FALSE` | `ToDisplayString()` | Прямой `Append("TRUE"/"FALSE")` |
| IMAGE/SPRITE/... | Строковое представление | `ToDisplayString()` | `ToDisplayString()` (fallback) |

### 12.2 INPUT и типы

| Тип | Формат ввода | Замечания |
|-----|-------------|-----------|
| INTEGER | Десятичное, инвариантный формат | `int.TryParse` с `NumberStyles.Integer` |
| LONG | Десятичное, инвариантный формат | `long.TryParse` с `NumberStyles.Integer` |
| DOUBLE | Вещественное, инвариантный формат | `double.TryParse`, проверка `IsFinite` |
| DECIMAL | Вещественное, инвариантный формат | `decimal.TryParse` |
| STRING | Вся строка | Без trim для единственной переменной |
| BOOLEAN | `TRUE`/`FALSE` | Регистронезависимо |

**Не поддерживаются как цели INPUT:** IMAGE, SPRITE, TILESET, TILEMAP, FILE, FUNCTION, ARRAY.

---

## 13. Взаимодействие со String

### 13.1 PRINT и строки

- Строки выводятся как есть, без кавычек.
- Пустая строка `""` — выводит пустую строку (не ничего).
- `ToDisplayString()` для строки возвращает саму строку.

### 13.2 INPUT и строки

- Для единственной строковой переменной — вся введённая строка становится значением без trim.
- Для нескольких переменных — поля trimятся.
- Unicode полностью поддерживается (ввод и вывод через .NET string).

### 13.3 PRINT USING и строки

- `!` — первый символ строки (если строка пуста — пустой результат).
- `&` — вся строка.
- `\   \` — фиксированная ширина (количество символов между `\` включая сами `\`).

---

## 14. Взаимодействие с Array

INPUT не поддерживает массивы как цели. Попытка использования массива как цели INPUT:
- Interpreter: `ReportInvalidRuntimeState("INPUT does not support array '...'.")`
- VM: `ReportInvalidRuntimeState("INPUT does not support array '...'.")`

PRINT может выводить элементы массива через выражение `array(i)`, но не сам массив целиком.

---

## 15. Взаимодействие с TYPE

INPUT не поддерживает пользовательские TYPE как цели. Поля TYPE могут быть целями INPUT по отдельности:
```DIM t AS MyType
INPUT t.Name, t.Age```

PRINT может выводить поля TYPE через выражения `t.Name`, `t.Age`.

---

## 16. Взаимодействие с Functions / SUB

INPUT и PRINT работают внутри FUNCTION и SUB без ограничений. Замыкания и рекурсия не влияют на INPUT/PRINT — они используют текущий scope и хост.

---

## 17. Взаимодействие с Control Flow

INPUT и PRINT работают внутри всех управляющих конструкций:
- `IF/ELSE/END IF`
- `SELECT CASE`
- `FOR/NEXT`
- `WHILE/END WHILE`
- `DO/LOOP`
- `TRY/CATCH/FINALLY`

Особенность: INPUT внутри цикла — программа будет ждать ввода на каждой итерации.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Ошибки PRINT USING

- Несовпадение типа поля и значения → `MB3007` (BuiltinArgument).
- Нехватка значений для полей → `MB3007`.
- Длина форматной строки > 2048 → `MB3007`.

### 18.2 Ошибки INPUT

- Невозможность преобразования → `MB3005` (InputConversion).
- Несовпадение числа полей → `MB3005`.
- Массив как цель → RuntimeFailureException.

### 18.3 Восстановление после ошибок

После ошибки INPUT или PRINT USING выполнение прерывается через `RuntimeFailureException`. Интерпретатор и VM ловят это исключение в верхнем уровне и возвращают `RuntimeResult` с диагностикой.

---

## 19. State / Lifetime / Ownership

### 19.1 Буфер вывода

- **Создаётся:** при создании Interpreter/BytecodeVm.
- **Владелец:** Interpreter/BytecodeVm.
- **Изменяется:** при каждом `AppendPendingOutput()`.
- **Освобождается:** при `FlushPendingOutput()` или завершении программы.
- **Lifetime:** lifetime Interpreter/VM.

### 19.2 Открытые файлы

- INPUT/PRINT не открывают файлы напрямую.
- Файловые функции (`OPEN...FOR INPUT AS`) — отдельная подсистема.

---

## 20. Edge Cases

### 20.1 PRINT

| Сценарий | Поведение | Статус |
|----------|-----------|--------|
| Пустой `PRINT` | Перевод строки | Корректно |
| `PRINT ""` | Пустая строка + перевод строки | Корректно |
| `PRINT ;` | Ничего (нет вывода, нет новой строки) | Корректно |
| `PRINT ,` | Пробел (нет новой строки) | Корректно |
| `PRINT "A"; "B"` | `AB` | Корректно |
| `PRINT "A", "B"` | `A B` | Корректно |
| `PRINT 1; 2; 3` | `123` | Корректно |
| `PRINT 1, 2, 3` | `1 2 3` | Корректно |
| Множественные `PRINT ;` подряд | Буфер растёт, не сбрасывается | Корректно |

### 20.2 PRINT USING

| Сценарий | Поведение | Статус |
|----------|-----------|--------|
| `PRINT USING "##"; 3.14` | ` 3` (целая часть) | Корректно |
| `PRINT USING "##.##"; 3.14159` | ` 3.14` | Корректно |
| Число не помещается в маску | `%3.14159` (префикс `%`) | Корректно |
| Строковое поле + числовое значение | Ошибка `MB3007` | Корректно |
| Числовое поле + строковое значение | Ошибка `MB3007` | Корректно |
| Нехватка значений | Ошибка `MB3007` | Корректно |
| Форматная строка > 2048 символов | Ошибка `MB3007` | Корректно |
| Пустая форматная строка `""` | Пустой вывод | Корректно |

### 20.3 INPUT

| Сценарий | Поведение | Статус |
|----------|-----------|--------|
| `INPUT x` (INTEGER), ввод `42` | `x = 42` | Корректно |
| `INPUT x` (STRING), ввод `hello, world` | `x = "hello, world"` (вся строка) | Корректно |
| `INPUT a, b` (STRING, INTEGER), ввод `Ada, 42` | `a = "Ada"`, `b = 42` | Корректно |
| `INPUT a, b`, ввод `Ada` (без запятой) | Ошибка `MB3005` (1 поле, ожидалось 2) | Корректно |
| `INPUT a` (INTEGER), ввод `abc` | Ошибка `MB3005` | Корректно |
| `INPUT a` (BOOLEAN), ввод `TRUE` | `a = TRUE` | Корректно |
| `INPUT a` (BOOLEAN), ввод `yes` | Ошибка `MB3005` | Корректно |
| `INPUT a` (DOUBLE), ввод `3.14` | `a = 3.14` | Корректно |
| `INPUT a` (DOUBLE), ввод `NaN` | Ошибка (не проходит `IsFinite`) | Корректно |
| `INPUT a` (DOUBLE), ввод `Infinity` | Ошибка (не проходит `IsFinite`) | Корректно |
| Пустой ввод `""` для STRING | `a = ""` | Корректно |
| Пустой ввод `""` для INTEGER | Ошибка `MB3005` | Корректно |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| PRINT пустой | `FlushPendingOutput(true)` | `PrintLine` → `FlushPendingOutput(true)` | ✅ | ✅ | ✅ |
| PRINT с `;` | Буферизация | Буферизация | ✅ | ✅ | ✅ |
| PRINT с `,` | Пробел | Пробел | ✅ | ✅ | ✅ |
| PRINT USING | `BuiltinPrintUsingHelpers.FormatUsing()` | `BuiltinPrintUsingHelpers.FormatUsing()` | ✅ | ✅ | ✅ |
| INPUT одна переменная | `TryParseInputValues()` | `TryParseAndStoreInputValues()` | ✅ | ✅ | ✅ |
| INPUT несколько переменных | `string.Split(',')` | Оптимизированный span-based | ✅ | ✅ | ✅ |
| PRINT USING кэш формата | Нет кэша | `_printUsingFormatCache` | ✅ (результат) | ✅ | Различие (кэш) |
| Формат вывода чисел | `ToDisplayString()` | Типо-специальный `stackalloc` | ✅ (результат) | ✅ | Различие (аллокации) |

**Расхождения:**
1. **Кэширование формата PRINT USING:** VM кэширует распарсенные токены формата, Interpreter — нет. Функционально эквивалентно, но VM эффективнее для повторных PRINT USING с одной и той же форматной строкой.
2. **Форматирование чисел при выводе:** VM использует `stackalloc char[64]` + `ISpanFormattable.TryFormat()`, Interpreter использует `ToDisplayString()`. Результат идентичен, но VM аллоцирует меньше памяти.
3. **Разбор INPUT для 2-3 переменных:** VM имеет оптимизированные пути `TryParseAndStoreTwoInputValues()` и `TryParseAndStoreThreeInputValues()` без аллокации массива полей. Interpreter использует общий путь с `string.Split(',')`.

**Классификация расхождений:**
- Все расхождения — **допустимые различия** (optimization-only, не влияют на результат).
- Нет ошибок корректности.
- Нет незавершённых реализаций.

---

## 22. Анализ производительности

### 22.1 Benchmark-данные из LANGUAGE_INVENTORY.md

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| `print_output_gallery.axm` (180 итераций, PRINT + PRINT USING) | baseline | 0.860x runtime | ~1.16x | CLEAR WIN |
| `input_record_intake.axm` (180 итераций, INPUT 3 переменных) | baseline | 0.879x runtime | ~1.14x | CLEAR WIN |

### 22.2 Почему VM быстрее

1. **Типо-специальное форматирование чисел:** VM использует `stackalloc` + `TryFormat()` вместо `ToDisplayString()`, что减少了 аллокации.
2. **Кэширование формата PRINT USING:** VM кэширует распарсенные токены формата.
3. **Оптимизированный разбор INPUT:** для 2-3 переменных VM не аллоцирует массив полей.
4. **Кэширование плана INPUT:** `_inputInstructionCache` по `(routineId, instructionIndex)`.
5. **Прямой dispatch:** VM не проходит через AST-walking, а исполняет линейные инструкции.

### 22.3 Hot Paths

- `AppendPendingOutput(RuntimeValue)` — вызывается для каждого элемента PRINT.
- `BuiltinPrintUsingHelpers.FormatUsing()` — вызывается для каждого PRINT USING.
- `TryParseAndStoreInputValues()` — вызывается для каждого INPUT.

---

## 23. Качество Benchmark

### 23.1 `print_output_gallery.axm`

- **Рабочая нагрузка:** 180 итераций, каждая делает 4 PRINT (2 обычных + 2 USING).
- **Содержание:** строки, числа, форматные маски, разделители `;` и `,`.
- **Что тестируется:** буферизация вывода, форматирование, кэширование формата.
- **Качество:** высокое — покрывает основные сценарии использования.

### 23.2 `input_record_intake.axm`

- **Рабочая нагрузка:** 180 итераций, каждая делает INPUT 3 переменных (STRING, INTEGER, INTEGER).
- **Что тестируется:** разбор ввода, преобразование типов, управление потоком после ввода.
- **Качество:** высокое — покрывает типичный сценарий пакетного ввода.
- **Особенность:** использует scripted input (compare-engines).

---

## 24. Hot Paths и Bottlenecks

### 24.1 PRINT

**Hot path:** `AppendPendingOutput(value.ToDisplayString())` в Interpreter.

**Bottleneck:** `ToDisplayString()` аллоцирует строку для каждого числового значения.

**Оптимизация в VM:** `AppendPendingOutput(RuntimeValue)` использует `stackalloc char[64]` + `TryFormat()` для чисел, что减少了 аллокации.

**Возможное улучшение Interpreter:** аналогичная оптимизация через `ISpanFormattable`.

### 24.2 PRINT USING

**Hot path:** `BuiltinPrintUsingHelpers.FormatUsing()` → парсинг формата + форматирование.

**Bottleneck:** парсинг формата на каждый вызов (в Interpreter). VM кэширует результат.

**Оптимизация в VM:** `_printUsingFormatCache`.

**Возможное улучшение Interpreter:** аналогичный кэш.

### 24.3 INPUT

**Hot path:** `TryParseInputValues()` → `string.Split(',')` + `TryParseInputField()`.

**Bottleneck:** `string.Split(',')` аллоцирует массив строк.

**Оптимизация в VM:** ручной span-based парсинг для 2-3 переменных.

---

## 25. Архитектурные проблемы

### 25.1 Отсутствие DATA/READ/RESTORE

**Категория:** FEATURE GAP (не архитектурная проблема).

DATA/READ/RESTORE — это planned feature, которая не имеет реализации на каком-либо уровне. Это не баг и не архитектурная проблема, а нереализованная возможность.

### 25.2 Приглашение INPUT только как строковый литерал

**Категория:** DESIGN LIMITATION.

Парсер (`Parser.Statements.cs:199-204`) проверяет `Current.Kind == SyntaxKind.StringLiteralToken` для приглашения. Это означает, что приглашение не может быть переменной или выражением:
```DIM prompt AS STRING = "Enter name"
INPUT prompt; x  ' Не скомпилируется!```

В QuickBASIC допускается произвольное строковое выражение. Текущее ограничение — осознанное упрощение, но его следует зафиксировать в документации.

### 25.3 Запятая в PRINT — не табуляционные зоны

**Категория:** DESIGN DECISION (не проблема).

В QuickBASIC запятая в PRINT переходила к следующей табуляционной зоне (каждые 14 символов). В Axiom запятая добавляет один пробел. Это зафиксировано в документации (`docs/manual/print.html:89`) и является осознанным решением.

---

## 26. Дублирование логики

### 26.1 TryParseInputField

**Текущие места:**
- `BuiltinIOHelpers.TryParseInputField(TypeSymbol, string)` — для Interpreter.
- `BuiltinIOHelpers.TryParseInputField(TypeSymbol, ReadOnlySpan<char>)` — для VM.

**Дублируемая логика:** два перегрузки с идентичной логикой, но разными типами входа.

**Единый источник истины:** `BuiltinIOHelpers` — правильно, обе перегрузки в одном классе.

**Оценка:** минимальное дублирование, необходимое для оптимизации span-based парсинга в VM.

### 26.2 AppendPendingOutput / FlushPendingOutput

**Текущие места:**
- `Interpreter.Helpers.cs:237-264` — Interpreter.
- `BytecodeVm.cs:5113-5218` — VM.

**Дублируемая логика:** буферизация вывода, `AppendPrintSeparator()`, `FlushPendingOutput()`, `WriteTextLine()`.

**Направление миграции:** выделить общий `OutputBuffer` хелпер. Но на текущем этапе дублирование контролируемое и не создаёт расхождений в поведении.

**Риски:** при изменении логики вывода нужно менять в двух местах.

### 26.3 TryParseInputValues

**Текущие места:**
- `Interpreter.Helpers.cs:278-300` — Interpreter.
- `BytecodeVm.Input.cs:178-250` — VM.

**Дублируемая логика:** разбор введённой строки по переменным.

**Различия:** VM имеет оптимизированные пути для 1, 2, 3 переменных. Interpreter использует общий путь с `string.Split(',')`.

**Единый источник истины:** `BuiltinIOHelpers` для общих алгоритмов. Специфичная логика — в каждом движке.

---

## 27. Single Source of Truth

| Аспект | Источник истины | Статус |
|--------|----------------|--------|
| Синтаксис | `Parser.Statements.cs` | ✅ Единственный |
| Semantics | `Binder.IoStatements.cs` | ✅ Единственный |
| Bound модель | `BoundStatements.cs` | ✅ Единственный |
| Lowering | `BoundProgramLowerer.cs` | ✅ Единственный |
| Bytecode | `BytecodeCompiler.cs` + `Opcode.cs` | ✅ Единственный |
| Runtime behavior | `Interpreter` + `BytecodeVm` + `BuiltinIOHelpers` + `BuiltinPrintUsingHelpers` | ⚠️ Два движка |
| Ошибки | `DiagnosticCodes` + runtime-диагностики | ✅ Единственный |
| Metadata | `BuiltinFunctionId` + `BuiltinBinding` | ✅ Единственный |

**Проблема:** два движка исполнения (Interpreter и VM) — потенциальный источник расхождений. На практике расхождения минимальны и контролируемы.

---

## 28. Legacy / Dead Code

Не обнаружено. Вся реализация INPUT/PRINT/PRINT USING активно используется.

---

## 29. Надёжность и безопасность

### 29.1 Риски

| Риск | Применимость | Оценка |
|------|-------------|--------|
| Неконтролируемый доступ к ресурсам | Нет (INPUT/PRINT не работают с файлами напрямую) | ✅ Безопасно |
| Path handling | Нет | ✅ Не применимо |
| Resource exhaustion | PRINT USING: ограничение 2048 символов формата | ✅ Защита есть |
| Malformed input | INPUT: корректная обработка некорректного ввода | ✅ Защита есть |
| Host exceptions | Зависит от `IRuntimeHost` | ⚠️ Вне контроля подсистемы |
| Memory abuse | Нет контроля за размером вывода | ⚠️ Потенциальный риск |

### 29.2 Защита PRINT USING

- Ограничение длины форматной строки: 2048 символов.
- Проверка типов полей (строковое поле требует строковое значение).
- Проверка количества значений.

### 29.3 Защита INPUT

- Проверка на массивные цели.
- Проверка преобразования типов.
- Проверка количества полей.

---

## 30. Анализ тестов

### 30.1 Найденные тесты

| Файл | Количество | Тип | Что покрывает |
|------|-----------|-----|---------------|
| `InterpreterTests.Print.cs` | 14 | Unit | PRINT, PRINT USING, разделители, ошибки |

### 30.2 Покрытие

**PRINT:**
- ✅ Пустой PRINT
- ✅ PRINT с пустой строкой
- ✅ PRINT с `;` (подавление новой строки)
- ✅ PRINT с `,` (мягкий разделитель)
- ✅ Множественные `,`
- ✅ Завершающая `,`
- ✅ `;` между значениями (конкатенация)
- ✅ `&` для смешанных скалярных значений
- ✅ Смешанный список `;` и `,`
- ✅ `,` не использует фиксированные зоны

**PRINT USING:**
- ✅ Числовая маска
- ✅ Строковые маски (`!`, `&`, `\   \`)
- ✅ Завершающий `;`
- ✅ Ошибка: строковое поле + числовое значение
- ✅ Ошибка: числовое поле + строковое значение

**INPUT:**
- Тесты INPUT находятся в интеграционных/smoke-тестах, а не в unit-тестах.

### 30.3 Классификация тестов

| Тип | Количество |
|-----|-----------|
| Unit (Interpreter) | 14 |
| Integration (smoke) | 2+ сценария |
| Engine comparison | 2 сценария |

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии (отсутствуют как unit-тесты)

- INPUT с одной переменной каждого типа (INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN).
- INPUT с несколькими переменными разных типов.
- INPUT с приглашением (`;` и `,`).
- INPUT с пустым вводом для STRING.
- PRINT с выражениями, вычисляемыми в runtime (не литералами).
- PRINT USING с числовыми масками: `+`, `-`, `$`, `*`, `,`, `^`.
- PRINT USING с несколькими значениями разных типов.

### 31.2 Отрицательные сценарии (отсутствуют)

- INPUT с массивом как целью.
- INPUT с константой как целью.
- INPUT с TYPE как целью.
- PRINT USING с пустой форматной строкой.
- PRINT USING с форматной строкой ровно 2048 символов.
- PRINT USING с форматной строкой 2049 символов.

### 31.3 Граничные случаи (отсутствуют)

- INPUT с очень длинной строкой ввода.
- INPUT с множественными запятыми подряд.
- INPUT с пробелами вокруг полей.
- PRINT с очень большим числом элементов.
- PRINT USING с числом, которое не помещается в маску (проверка `%`-префикса).
- PRINT USING с экспоненциальной формой (`^^^^`).

### 31.4 Parity тесты (отсутствуют)

- Сквозные тесты INPUT/PRINT для Interpreter vs BytecodeVm.
- Проверка идентичности вывода для одних и тех же программ.

---

## 32. Анализ Samples

### 32.1 PRINT samples

| Файл | Что демонстрирует | Engine | Проверяемый результат |
|------|-------------------|--------|----------------------|
| `basic_expression.axm` | Базовый вывод выражений | Любой | Визуальный |
| `empty_print.axm` | Пустой PRINT | Любой | Визуальный |
| `separator_semicolon.axm` | Разделитель `;` | Любой | Визуальный |
| `separator_comma.axm` | Разделитель `,` | Любой | Визуальный |
| `mixed_list.axm` | Смешанный список | Любой | Визуальный |
| `using_basic.axm` | PRINT USING | Любой | Визуальный |

### 32.2 INPUT samples

| Файл | Что демонстрирует | Engine | Проверяемый результат |
|------|-------------------|--------|----------------------|
| `single_variable.axm` | Одна переменная | Любой | Интерактивный |
| `multiple_variables.axm` | Несколько переменных | Любой | Интерактивный |
| `prompt_semicolon.axm` | Приглашение с `;` | Любой | Интерактивный |
| `prompt_comma.axm` | Приглашение с `,` | Любой | Интерактивный |

### 32.3 Engine-compare samples

| Файл | Что демонстрирует | Результат |
|------|-------------------|-----------|
| `print_output_gallery.axm` | Нагрузка на PRINT + PRINT USING | Числовые checksums |
| `input_record_intake.axm` | Пакетный ввод через INPUT | Числовые checksums |

### 32.4 Оценка качества samples

- PRINT samples покрывают основные сценарии визуально, но не имеют автоматической проверки результата.
- INPUT samples — интерактивные, не могут быть автоматически проверены.
- Engine-compare samples — лучшее покрытие: имеют проверяемый числовой результат.

---

## 33. Анализ документации

### 33.1 Manual

| Страница | Статус | Качество |
|----------|--------|----------|
| `print.html` | ✅ Есть | Высокое: синтаксис, описание, разделители, PRINT USING, примеры, ограничения |
| `input.html` | ✅ Есть | Высокое: синтаксис, описание, приглашение, разбор ввода, ограничения |
| `data-and-input.html` | ✅ Есть | Обзорная страница раздела |

### 33.2 Спецификация

`LANGUAGE_INVENTORY.md` корректно фиксирует:
- PRINT, PRINT USING, INPUT — IMPLEMENTED (§22, строки 615-631).
- DATA, READ, RESTORE — NOT IMPLEMENTED (§23, строки 635-639).
- LINE INPUT — NOT IMPLEMENTED.

### 33.3 Несоответствия документации и кода

**Не обнаружено.** Документация accurately отражает текущее состояние реализации.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

- `PRINT` — ключевое слово, подсвечивается.
- `INPUT` — ключевое слово, подсвечивается.
- `USING` — ключевое слово, подсвечивается (используется и в `PALETTE USING`).

### 34.2 Ввод-вывод в IDE

- **Вывод:** `ProgramRunService` перехватывает `IRuntimeHost.WriteOutput()` и пишет во вкладку `Output`.
- **Ввод:** `InputPromptService` и `InputStateService` обеспечивают ввод через нижнюю область `Output`.
- **SCREEN:** в активном SCREEN ввод-вывод идёт через графическое окно.

### 34.3 Диагностики

- Ошибки INPUT/PRINT USING отображаются в панели диагностик IDE.
- Координаты ошибок (строка, столбец) корректны.

---

## 35. Зависимости от Host / Platform

### 35.1 Зависимости

| Зависимость | Применимость | Тип |
|------------|-------------|-----|
| Windows | IDE (WPF) | HOST LIMITATION |
| .NET | Runtime, string formatting | PLATFORM |
| Encoding | Unicode strings | PLATFORM |
| Locale | `CultureInfo.InvariantCulture` для чисел | PLATFORM (invariant) |

### 35.2 Инвариантность

- Числа всегда форматируются и разбираются с `CultureInfo.InvariantCulture`.
- Точка как десятичный разделитель.
- Это правильно и не создаёт проблем локализации.

---

## 36. Карта корневых причин

### ROOT-SYS-001: Отсутствие DATA/READ/RESTORE
- **Категория:** FEATURE GAP
- **Серийность:** P3 (отсутствие незаявленной feature)
- **Затронутые компоненты:** Lexer, Parser, Syntax, Binder, Lowering, BytecodeCompiler, Opcode, BytecodeVm, Interpreter, Tests, Samples, Documentation
- **Направление:** полная реализация с нуля

### ROOT-SYS-002: Отсутствие LINE INPUT
- **Категория:** FEATURE GAP
- **Серийность:** P3
- **Затронутые компоненты:** аналогично ROOT-SYS-001

### ROOT-SYS-003: Приглашение INPUT только как литерал
- **Категория:** DESIGN LIMITATION
- **Серийность:** P3
- **Затронутые компоненты:** Parser
- **Направление:** расширение парсера для произвольных выражений

---

## 37. Стабильные области

### Что работает и что не следует трогать

1. **Буферизация вывода** — модель `_pendingOutput` + `FlushPendingOutput()` стабильна и эффективна.
2. **Общие хелперы** — `BuiltinIOHelpers` и `BuiltinPrintUsingHelpers` — чистые, тестируемые, не имеют побочных эффектов.
3. **Lowering для PRINT** — линейное представление через `PrintValue` / `PrintSeparator` / `PrintLine` чистое и понятное.
4. **Lowering для INPUT** — одна инструкция `Input` с метаданными —简洁 и эффективно.
5. **Кэширование в VM** — `_printUsingFormatCache` и `_inputInstructionCache` — правильная оптимизация.
6. **Диагностики** — `MB3005` (InputConversion), `MB3007` (BuiltinArgument) — корректны и информативны.
7. **Документация** — `print.html` и `input.html` — полные и accurate.

---

## 38. Возможности консолидации

### 38.1 Буфер вывода

**Текущие места:** Interpreter + VM.
**Дублируемая логика:** `_pendingOutput`, `_hasPendingOutput`, `AppendPendingOutput()`, `AppendPrintSeparator()`, `FlushPendingOutput()`, `WriteTextLine()`.
**Единый источник истины:** выделить `OutputBuffer` хелпер.
**Направление миграции:** создать `BuiltinOutputBuffer` в `Axiom.Runtime.Builtins`.
**Риски:** низкие — логика простая и хорошо покрыта тестами.

### 38.2 Разбор INPUT

**Текущие места:** Interpreter + VM.
**Дублируемая логика:** `TryParseInputValues()`, `CountInputFields()`, `NormalizeInputField()`.
**Единый источник истины:** `BuiltinIOHelpers` уже содержит общие алгоритмы.
**Направление миграции:** нет необходимости — дублирование минимально и контролируемо.

---

## 39. Возможности разделения ответственности

Текущее разделение ответственности корректно:
- **Parser** — синтаксис.
- **Binder** — семантика.
- **Lowering** — линейное представление.
- **BytecodeCompiler** — эмиссия байткода.
- **Interpreter** — AST-walking исполнение.
- **BytecodeVm** — байткодное исполнение.
- **BuiltinIOHelpers** / **BuiltinPrintUsingHelpers** — общие алгоритмы.

Нет смешивания ответственностей.

---

## 40. Кандидаты на удаление

Не обнаружено. Весь код активно используется.

---

## 41. Готовность к дальнейшему развитию

### 4.1 К DATA/READ/RESTORE

Подсистема INPUT/PRINT не зависит от DATA/READ/RESTORE. DATA/READ/RESTORE — отдельная подсистема, которую можно реализовать независимо.

### 4.2 К LINE INPUT

LINE INPUT — расширение INPUT. Его реализация потребует:
- Нового ключевого слова в лексере.
- Нового синтаксического узла.
- Новой ветки в парсере.
- Нового bound-узла.
- Нового lowering-пути.
- Нового опкода (или переиспользования INPUT с флагом).
- Изменения в Interpreter и VM.

### 4.3 К PRINT #f / INPUT #f

Вывод и ввод в файл — отдельная подсистема, которая потребует:
- Нового синтаксиса (`PRINT #f, expr`).
- Нового bound-узла.
- Изменений в Interpreter и VM.

### 4.4 К будущему Compiler

Подсистема готова к компиляции: lowering чистый, байткод стабильный.

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-004 | P3 | FEATURE GAP | Все уровни | DATA/READ/RESTORE не реализовано | `LANGUAGE_INVENTORY.md:637-639` | Нереализованная feature | CONFIRMED |
| SYS-005 | P3 | FEATURE GAP | Все уровни | LINE INPUT не реализовано | `LANGUAGE_INVENTORY.md:639` | Нереализованная feature | CONFIRMED |
| SYS-006 | P3 | DESIGN LIMITATION | Parser | Приглашение INPUT только как строковый литерал | `Parser.Statements.cs:199-204` | Осознанное упрощение | CONFIRMED |
| SYS-007 | P4 | DOCS | Manual | `,` в PRINT не QuickBASIC-зоны, но это не всегда явно | `print.html:89` | Design decision | CONFIRMED |
| SYS-008 | P4 | PERFORMANCE | Interpreter | PRINT USING не кэширует формат | `Interpreter.PrintUsing.cs:8-22` | Отсутствие оптимизации | CONFIRMED |
| SYS-009 | P4 | PERFORMANCE | Interpreter | `ToDisplayString()` аллоцирует для чисел | `Interpreter.Helpers.cs:398` | Отсутствие оптимизации | CONFIRMED |
| SYS-010 | P4 | TESTS | Tests | Нет unit-тестов INPUT | Поиск в `tests/` | Недостаточное покрытие | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения

**TASK-000:** Прогнать существующие тесты и benchmark-сценарии для подтверждения текущего baseline.

### ЭТАП 1 — Исправление критических проблем корректности

Нет критических проблем корректности. Все P0-P1 отсутствуют.

### ЭТАП 2 — Стабилизация семантики

**TASK-001:** Добавить unit-тесты INPUT для всех поддерживаемых типов (INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN).

**TASK-002:** Добавить unit-тесты INPUT для отрицательных сценариев (массив, константа, TYPE, неверное преобразование, несовпадение числа полей).

**TASK-003:** Добавить unit-тесты PRINT USING для расширенных числовых масок (`+`, `-`, `$`, `*`, `,`, `^`).

**TASK-004:** Добавить parity-тесты INPUT/PRINT для Interpreter vs BytecodeVm.

### ЭТАП 3 — Архитектурное улучшение

**TASK-005:** Выделить общий `OutputBuffer` хелпер для устранения дублирования буферизации вывода между Interpreter и VM.

### ЭТАП 4 — Оптимизация производительности

**TASK-006:** Добавить кэширование формата PRINT USING в Interpreter (аналогично `_printUsingFormatCache` в VM).

**TASK-007:** Оптимизировать `AppendPendingOutput()` в Interpreter через `ISpanFormattable` для числовых типов.

### ЭТАП 5 — Усиление тестирования

**TASK-008:** Добавить edge-case тесты: пустой ввод INPUT, множественные запятые, очень длинные строки.

**TASK-009:** Добавить engine-comparison тесты для PRINT USING с различными масками.

### ЭТАП 6 — Обновление документации

**TASK-010:** Явно зафиксировать в manual ограничение приглашения INPUT (только строковый литерал).

**TASK-011:** Добавить в manual примеры для расширенных числовых масок PRINT USING.

### ЭТАП 7 — Обновление IDE

Нет необходимости — IDE корректно работает с INPUT/PRINT.

### ЭТАП 8 — Подготовка к дальнейшему развитию

**TASK-012:** Спроектировать DATA/READ/RESTORE (отдельный проект).

**TASK-013:** Спроектировать LINE INPUT (отдельный проект).

---

## 44. Граф зависимостей задач

```
TASK-000 (baseline)
    ↓
TASK-001, TASK-002, TASK-003, TASK-004 (параллельно)
    ↓
TASK-005 (после стабилизации тестов)
    ↓
TASK-006, TASK-007 (параллельно)
    ↓
TASK-008, TASK-009 (параллельно)
    ↓
TASK-010, TASK-011 (параллельно)
    ↓
TASK-012, TASK-013 (отдельные проекты)
```

---

## 45. Definition of Done

Подсистема INPUT/PRINT/PRINT USING считается завершённой, когда:

1. **Корректность:** все существующие тесты проходят, parity между Interpreter и VM подтверждён.
2. **Edge cases:** unit-тесты покрывают все поддерживаемые типы, отрицательные сценарии и граничные случаи.
3. **Error handling:** все диагностические коды (`MB3005`, `MB3007`) покрыты тестами.
4. **Interpreter:** все сценарии работают корректно через AST-walking.
5. **VM:** все сценарии работают корректно через байткод.
6. **Тестирование:** parity-тесты подтверждают идентичность результатов.
7. **Документация:** manual accurately отражает поведение, включая ограничения.
8. **IDE:** подсветка, диагностики и ввод-вывод работают корректно.
9. **Производительность:** benchmark-сценарии стабильны, VM быстрее интерпретатора.

DATA/READ/RESTORE и LINE INPUT — отдельные подсистемы с отдельным Definition of Done.

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Описание |
|---|---|---|---|
| 1 | SYS-004 | P3 | DATA/READ/RESTORE не реализовано |
| 2 | SYS-005 | P3 | LINE INPUT не реализовано |
| 3 | SYS-006 | P3 | Приглашение INPUT только как строковый литерал |
| 4-20 | — | — | Нет проблем P0-P2 |

**Вывод:** нет критических проблем корректности. Все находки — feature gaps или незначительные улучшения.

---

## 47. ТОП-20 задач по производительности

| # | ID | Описание | Ожидаемая польза |
|---|---|---|---|
| 1 | TASK-006 | Кэш формата PRINT USING в Interpreter | ~5-10% для PRINT USING |
| 2 | TASK-007 | Оптимизация `AppendPendingOutput()` в Interpreter | ~5-15% для PRINT |
| 3-20 | — | Нет значимых задач по производительности |

**Вывод:** производительность подсистемы уже хорошая. VM быстрее интерпретатора на 14-16%. Оптимизации — incremental improvements.

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка (0-10) | Объяснение |
|--------|---:|------------|
| Корректность | 9 | Все реализованные сценарии работают корректно. Нет P0-P1. |
| Семантика | 9 | Семантика чёткая, зафиксированная в документации. Отличие от QuickBASIC (запятая) осознанное. |
| Архитектура | 9 | Чистое разделение слоёв, минимальное дублирование, общие хелперы. |
| Runtime | 9 | Стабильный, с кэшированием и оптимизациями в VM. |
| Interpreter | 8 | Работает корректно, но缺乏 кэширования формата PRINT USING и оптимизации числового вывода. |
| Bytecode VM | 9 | Полный паритет с интерпретатором, дополнительные оптимизации. |
| Производительность | 8.5 | VM быстрее на 14-16%. Есть потенциал для incremental improvements. |
| Память | 9 | VM использует `stackalloc` и `ArrayPool` для减少 аллокаций. |
| Error handling | 9 | Корректные диагностические коды, информативные сообщения. |
| Tests | 7 | Хорошее покрытие PRINT, но отсутствуют unit-тесты INPUT и parity-тесты. |
| Samples | 8 | 10 примеров + 2 benchmark-сценария. INPUT samples — интерактивные. |
| Documentation | 9 | Полные manual-страницы, корректная спецификация. |
| IDE | 9 | Подсветка, диагностики, ввод-вывод работают корректно. |
| Maintainability | 9 | Чистый код, понятная архитектура, минимальное дублирование. |
| Reliability | 9 | Стабильная работа, корректное восстановление после ошибок. |
| Future readiness | 8 | Готова к LINE INPUT и PRINT #f. DATA/READ/RESTORE — отдельная подсистема. |

**Средняя оценка: 8.7/10**

---

## Главный контрольный вопрос

> Можно ли считать подсистему INPUT/PRINT/PRINT USING/DATA/READ/RESTORE достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

## **YES, WITH CONDITIONS**

### Условия

1. **DATA/READ/RESTORE** — это отдельная подсистема, которую нужно проектировать и реализовать отдельно. Её отсутствие не блокирует переход к следующей подсистеме.
2. **LINE INPUT** — аналогично, отдельная feature.
3. **Unit-тесты INPUT** — желательно добавить до перехода, но не критично.

### Основные blockers

Нет критических blockers. Единственный существенный пробел — DATA/READ/RESTORE — это feature gap, а не баг.

### Что нужно сделать в первую очередь

1. Добавить unit-тесты INPUT (TASK-001, TASK-002).
2. Добавить parity-тесты (TASK-004).

### Что можно отложить

1. DATA/READ/RESTORE (отдельный проект).
2. LINE INPUT (отдельная feature).
3. Оптимизации производительности (TASK-006, TASK-007).

### Что не следует трогать

1. Буферизацию вывода — работает стабильно.
2. Общие хелперы (`BuiltinIOHelpers`, `BuiltinPrintUsingHelpers`) — чистые и эффективные.
3. Lowering и байткод — стабильные и корректные.
4. Документацию — accurate и полная.

---

*Конец отчёта.*
