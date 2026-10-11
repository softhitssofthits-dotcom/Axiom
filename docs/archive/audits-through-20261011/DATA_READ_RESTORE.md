# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## DATA / READ / RESTORE

---

## 1. Резюме

Подсистема `DATA` / `READ` / `RESTORE` **полностью отсутствует** во всех слоях языка Axiom: лексер, парсер, семантика, интерпретатор, байткодная VM, IDE и документация. Это не баг и не сломанная логика, а **нереализованная возможность** (feature gap), которая неоднократно фиксировалась в аудитах и инвентаризациях как запланированная, но не начинавшаяся.

**Health Score: 0/10** (не применимо — подсистема не существует).

**Главный вопрос: NO** — подсистема не существует, нестабильна и не может быть оценена по критериям корректности.

---

## 2. Что представляет собой эта подсистема

`DATA` / `READ` / `RESTORE` — это классическая конструкция языков семейства BASIC, позволяющая:

- **`DATA`** — хранить литеральные данные прямо в тексте программы (строки, числа, логические значения);
- **`READ`** — последовательно читать эти данные в переменные программы;
- **`RESTORE`** — сбрасывать указатель чтения на начало или на конкретную метку.

В QuickBASIC 4.5 конструкция использовалась для:
- инициализации массивов без явных присваиваний;
- хранения таблиц конфигурации, сообщений, ресурсов;
- чтения структурированных данных из тела программы;
- построения простых баз данных в памяти.

В Axiom подсистема не имеет ни одного уровня реализации.

---

## 3. Полный список возможностей

### SYS-001: Оператор DATA

| Поле | Значение |
|---|---|
| **ID** | SYS-001 |
| **Название** | Оператор DATA |
| **Категория** | STATEMENT |
| **Назначение** | Хранение литеральных данных в тексте программы |
| **Пользовательский синтаксис** | `DATA value1, value2, ...` |
| **Тип** | STATEMENT |
| **Parser** | ❌ Не реализован |
| **Syntax Node** | ❌ Не реализован |
| **Binder** | ❌ Не реализован |
| **Semantic Layer** | ❌ Не реализован |
| **Interpreter** | ❌ Не реализован |
| **Bytecode Lowering** | ❌ Не реализован |
| **Opcode** | ❌ Не реализован |
| **Bytecode VM** | ❌ Не реализован |
| **Runtime Helper** | ❌ Не реализован |
| **IDE** | ❌ Не реализован (нет подсветки) |
| **Tests** | ❌ Не реализован |
| **Samples** | ❌ Не реализован |
| **Benchmarks** | ❌ Не реализован |
| **Documentation** | ❌ Не реализован |
| **Текущий статус** | NOT_IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Отсутствие `DataKeyword` в `TokenKind.cs`, отсутствие `DataStatement` в `Syntax/Statements.cs`, отсутствие `BoundDataStatement` в `BoundStatements.cs`, отсутствие в `Interpreter.Statements.cs`, отсутствие в `BytecodeVm`, отсутствие в `Axiom.xshd` |

### SYS-002: Оператор READ

| Поле | Значение |
|---|---|
| **ID** | SYS-002 |
| **Название** | Оператор READ |
| **Категория** | STATEMENT |
| **Назначение** | Последовательное чтение данных из DATA в переменные |
| **Пользовательский синтаксис** | `READ var1, var2, ...` |
| **Тип** | STATEMENT |
| **Parser** | ❌ Не реализован |
| **Syntax Node** | ❌ Не реализован |
| **Binder** | ❌ Не реализован |
| **Semantic Layer** | ❌ Не реализован |
| **Interpreter** | ❌ Не реализован |
| **Bytecode Lowering** | ❌ Не реализован |
| **Opcode** | ❌ Не реализован |
| **Bytecode VM** | ❌ Не реализован |
| **Runtime Helper** | ❌ Не реализован |
| **IDE** | ❌ Не реализован |
| **Tests** | ❌ Не реализован |
| **Samples** | ❌ Не реализован |
| **Documentation** | ❌ Не реализован |
| **Текущий статус** | NOT_IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Отсутствие `ReadKeyword` в `TokenKind.cs`, отсутствие `ReadStatement` в `Syntax/Statements.cs` |

### SYS-003: Оператор RESTORE

| Поле | Значение |
|---|---|
| **ID** | SYS-003 |
| **Название** | Оператор RESTORE |
| **Категория** | STATEMENT |
| **Назначение** | Сброс указателя чтения DATA на начало или на метку |
| **Пользовательский синтаксис** | `RESTORE [label]` |
| **Тип** | STATEMENT |
| **Parser** | ❌ Не реализован |
| **Syntax Node** | ❌ Не реализован |
| **Binder** | ❌ Не реализован |
| **Semantic Layer** | ❌ Не реализован |
| **Interpreter** | ❌ Не реализован |
| **Bytecode Lowering** | ❌ Не реализован |
| **Opcode** | ❌ Не реализован |
| **Bytecode VM** | ❌ Не реализован |
| **Runtime Helper** | ❌ Не реализован |
| **IDE** | ❌ Не реализован |
| **Tests** | ❌ Не реализован |
| **Samples** | ❌ Не реализован |
| **Documentation** | ❌ Не реализован |
| **Текущий статус** | NOT_IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Отсутствие `RestoreKeyword` в `TokenKind.cs`, отсутствие `RestoreStatement` в `Syntax/Statements.cs` |

### SYS-004: Метки DATA (labels)

| Поле | Значение |
|---|---|
| **ID** | SYS-004 |
| **Название** | Метки DATA |
| **Категория** | LABEL / RUNTIME FACILITY |
| **Назначение** | Именованные точки в потоке DATA для RESTORE |
| **Пользовательский синтаксис** | `label: DATA ...` |
| **Тип** | OTHER |
| **Parser** | ❌ Не реализован |
| **Syntax Node** | ❌ Не реализован |
| **Binder** | ❌ Не реализован |
| **Semantic Layer** | ❌ Не реализован |
| **Interpreter** | ❌ Не реализован |
| **Bytecode Lowering** | ❌ Не реализован |
| **Opcode** | ❌ Не реализован |
| **Bytecode VM** | ❌ Не реализован |
| **Runtime Helper** | ❌ Не реализован |
| **IDE** | ❌ Не реализован |
| **Tests** | ❌ Не реализован |
| **Samples** | ❌ Не реализован |
| **Documentation** | ❌ Не реализован |
| **Текущий статус** | NOT_IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Отсутствие label-механизма в парсере и runtime |

### SYS-005: Указатель чтения DATA (data pointer)

| Поле | Значение |
|---|---|
| **ID** | SYS-005 |
| **Название** | Указатель чтения DATA |
| **Категория** | RUNTIME FACILITY |
| **Назначение** | Внутреннее состояние, отслеживающее текущую позицию в потоке DATA |
| **Пользовательский синтаксис** | Неявное (внутреннее состояние) |
| **Тип** | RUNTIME FACILITY |
| **Parser** | ❌ Не реализован |
| **Syntax Node** | ❌ Не реализован |
| **Binder** | ❌ Не реализован |
| **Semantic Layer** | ❌ Не реализован |
| **Interpreter** | ❌ Не реализован |
| **Bytecode Lowering** | ❌ Не реализован |
| **Opcode** | ❌ Не реализован |
| **Bytecode VM** | ❌ Не реализован |
| **Runtime Helper** | ❌ Не реализован |
| **IDE** | ❌ Не реализован |
| **Tests** | ❌ Не реализован |
| **Samples** | ❌ Не реализован |
| **Documentation** | ❌ Не реализован |
| **Текущий статус** | NOT_IMPLEMENTED |
| **Уровень уверенности** | CONFIRMED |
| **Доказательства** | Отсутствие data pointer в runtime environment |

---

## 4. Фактическая архитектура

### 4.1 Текущее состояние: подсистема отсутствует

Фактическая архитектура подсистемы DATA/READ/RESTORE в Axiom: **пустая**. Ни один из слоёв языкового конвейера не содержит компонентов, связанных с DATA, READ или RESTORE.

### 4.2 Существующая инфраструктура, которую подсистема должна использовать

При реализации DATA/READ/RESTORE подсистема будет опираться на следующие уже существующие компоненты:

| Слой | Компонент | Роль в DATA/READ/RESTORE |
|---|---|---|
| **Lexer** | `TokenKind.cs`, `Lexer.cs` | Добавление ключевых слов `DATA`, `READ`, `RESTORE` |
| **Syntax** | `Statements.cs`, `SyntaxKind.cs` | Добавление синтаксических узлов |
| **Parser** | `Parser.Statements.cs` | Добавление методов парсинга |
| **Semantics** | `Binder`, `BoundScope` | Связывание переменных READ, проверка типов |
| **Runtime** | `RuntimeValue`, `IRuntimeHost` | Хранение data pool, указатель чтения |
| **Interpreter** | `Interpreter.Statements.cs` | Исполнение DATA/READ/RESTORE |
| **Bytecode** | `BoundProgramLowerer`, `BytecodeCompiler` | Lowering и эмиссия опкодов |
| **BytecodeVm** | `BytecodeVm.cs` | Исполнение байткодных опкодов |
| **IDE** | `Axiom.xshd`, `Axiom.Dark.xshd` | Подсветка ключевых слов |

### 4.3 Архитектурные слои, которые будут затронуты

```
Исходный код (.axm)
    ↓
Lexer (новые ключевые слова: DATA, READ, RESTORE)
    ↓
Parser (новые узлы: DataStatementSyntax, ReadStatementSyntax, RestoreStatementSyntax)
    ↓
Syntax (новые SyntaxKind, StatementSyntax подклассы)
    ↓
Binder (связывание переменных READ, проверка типов, метки DATA)
    ↓
Bound Model (BoundDataStatement, BoundReadStatement, BoundRestoreStatement)
    ↓
Lowering (понижение в линейную форму)
    ↓
Bytecode (новые опкоды: ReadData, RestoreDataPointer, LoadDataValue)
    ↓
Interpreter (исполнение через data pool + указатель)
    ↓
BytecodeVm (исполнение через байткодные опкоды)
    ↓
Runtime (DataPool, DataPointer, DataLabel)
```

---

## 5. Полный путь выполнения

### 5.1 Путь отсутствует

Все этапы конвейера отсутствуют:

```
Исходный код (.axm)
    ↓ ❌ Лексер не распознаёт DATA/READ/RESTORE
Lexer
    ↓ ❌ Парсер не строит узлы
Parser
    ↓ ❌ Нет синтаксических узлов
Syntax
    ↓ ❌ Binder не связывает
Binder
    ↓ ❌ Нет bound nodes
Bound Model
    ↓ ❌ Lowering не понижает
Lowering
    ↓ ❌ Нет опкодов
Bytecode
    ↓ ❌ Interpreter не исполняет
Interpreter
    ↓ ❌ BytecodeVm не исполняет
Bytecode VM
    ↓ ❌ Runtime не имеет data pool
Runtime
    ↓
Результат: ❌ НЕТ РЕЗУЛЬТАТА
```

---

## 6. Фактический семантический контракт

### 6.1 Контракт отсутствует

Поскольку подсистема не реализована, фактический семантический контракт не определён. Ниже приведён **предлагаемый** контракт на основе классического BASIC и архитектуры Axiom.

### 6.2 Предлагаемый семантический контракт

#### DATA

**Вход:** литеральные значения (INTEGER, LONG, DOUBLE, DECIMAL, STRING, BOOLEAN), разделённые запятыми.

**Обработка:**
- значения собираются в глобальный data pool при запуске программы;
- каждое значение сохраняется с типом;
- порядок значений соответствует порядку DATA-операторов в исходном тексте сверху вниз;
- DATA-операторы исполняются в момент встречи, но их данные доступны с начала программы (как в QuickBASIC).

**Выход:** нет прямого выхода — данные помещаются в data pool.

**Ошибки:**
- синтаксические: некорректные литералы;
- семантические: не обнаружены (DATA содержит только литералы).

#### READ

**Вход:** список переменных-целей.

**Обработка:**
- читает следующее значение из data pool;
- приводит значение к типу целевой переменной (как при присваивании);
- если тип несовместим — runtime ошибка MB3007;
- если данных недостаточно — runtime ошибка MB3012 (конец DATA).

**Выход:** значения записываются в указанные переменные.

**Ошибки:**
- MB3007: несовместимый тип;
- MB3012: недостаточно данных в DATA.

#### RESTORE

**Вход:** опциональная метка (имя label).

**Обработка:**
- без аргумента: сбрасывает указатель на начало data pool;
- с меткой: сбрасывает указатель на позицию после указанной метки.

**Выход:** нет — меняется только внутреннее состояние.

**Ошибки:**
- MB2040: неизвестная метка DATA (если метка не найдена).

---

## 7. Анализ Parser

### 7.1 Статус: НЕ РЕАЛИЗОВАН

Парсер не содержит ни одного метода, связанного с DATA/READ/RESTORE.

### 7.2 Готовность существующей инфраструктуры парсера

Парсер Axiom готов к расширению:
- `Parser.Statements.cs` имеет чёткий паттерн `switch` по `TokenKind` для dispatch операторов;
- добавление новых ключевых слов в `TokenKind.cs` и маппинг в `Lexer.cs` — стандартная процедура;
- паттерн парсинга списков (через запятую) уже используется в `ParsePrintStatement`, `ParseInputStatement`, `ParseDimStatement`;
- восстановление после ошибок через `SynchronizeTo` уже работает.

### 7.3 Предлагаемая структура синтаксических узлов

```
DataStatementSyntax
  ├── DataKeyword: SyntaxToken
  └── Values: SeparatedSyntaxList<ExpressionSyntax>

ReadStatementSyntax
  ├── ReadKeyword: SyntaxToken
  └── Variables: SeparatedSyntaxList<ExpressionSyntax>

RestoreStatementSyntax
  ├── RestoreKeyword: SyntaxToken
  └── Label: SyntaxToken? (опционально)
```

### 7.4 Архитектурные замечания

**Важно:** DATA в классическом BASIC содержит только литералы, но Axiom может расширить это до произвольных выражений. Однако это потребует runtime-вычисления при запуске программы, что сложнее. Рекомендуется начать с литеральных значений.

---

## 8. Анализ Binder

### 8.1 Статус: НЕ РЕАЛИЗОВАН

Binder не содержит ни одного bound node, связанного с DATA/READ/RESTORE.

### 8.2 Готовность существующей инфраструктуры Binder

Binder Axiom готов к расширению:
- `Binder.Statements.cs` имеет чёткий dispatch по типам statements;
- `BoundScope` поддерживает lookup переменных для READ;
- проверка типов через `CanConvert` и `BindConversion` уже работает;
- двухпроходная модель (сначала регистрация символов, потом связывание тел) подходит для DATA.

### 8.3 Предлагаемая структура bound nodes

```
BoundDataStatement
  ├── Values: ImmutableArray<BoundExpression>
  └── Label: string?

BoundReadStatement
  ├── Variables: ImmutableArray<BoundExpression>
  └── Location: TextLocation

BoundRestoreStatement
  ├── Label: string?
  └── Location: TextLocation
```

### 8.4 Семантические проверки

- READ: каждая целевая переменная должна быть объявлена;
- READ: тип каждого значения DATA должен быть совместим с типом переменной;
- RESTORE: метка должна существовать (или быть пустой);
- DATA: значения должны быть вычисляемы на этапе компиляции (если только литералы).

---

## 9. Анализ Interpreter

### 9.1 Статус: НЕ РЕАЛИЗОВАН

Interpreter не содержит ни одного метода исполнения DATA/READ/RESTORE.

### 9.2 Готовность существующей инфраструктуры Interpreter

Interpreter Axiom готов к расширению:
- `Interpreter.Statements.cs` имеет dispatch по `BoundNodeKind`;
- `Interpreter` имеет доступ к `RuntimeEnvironment` для хранения data pool;
- паттерн исполнения через `ExecuteStatements` уже работает;
- обработка ошибок через `RuntimeResult` уже работает.

### 9.3 Предлагаемая модель исполнения

**Data Pool:**
- хранится в `RuntimeEnvironment` как `List<RuntimeValue>`;
- заполняется при первом проходе по программе (или при запуске);
- каждое значение DATA добавляется в порядке появления в коде.

**Data Pointer:**
- целочисленный индекс в `RuntimeEnvironment`;
- начинается с 0;
- увеличивается на 1 при каждом READ;
- сбрасывается RESTORE.

**Метки:**
- `Dictionary<string, int>` в `RuntimeEnvironment`;
- ключ — имя метки, значение — индекс в data pool.

### 9.4 Особенности Interpreter vs BytecodeVm

Interpreter может исполнять DATA/READ/RESTORE напрямую через AST-walking. BytecodeVm потребует:
- сбор data pool на этапе компиляции (или первого запуска);
- хранение data pool в `BytecodeModule` или `BytecodeVm` state;
- опкоды для чтения и сброса указателя.

---

## 10. Анализ Bytecode / VM

### 10.1 Статус: НЕ РЕАЛИЗОВАН

Bytecode не содержит ни одного опкода, связанного с DATA/READ/RESTORE.

### 10.2 Предлагаемые опкоды

```
Opcode.LoadDataValue     // Прочитать следующее значение из data pool
Opcode.RestoreDataPointer // Сбросить указатель на начало
Opcode.RestoreToLabel     // Сбросить указатель на метку
```

### 10.3 Lowering

DATA-операторы не генерируют байткод напрямую (данные хранятся отдельно). READ генерирует `LoadDataValue` для каждой переменной. RESTORE генерирует `RestoreDataPointer` или `RestoreToLabel`.

### 10.4 Stack Effect

- `LoadDataValue`: push 1 значение;
- `RestoreDataPointer`: stack unchanged;
- `RestoreToLabel`: stack unchanged.

---

## 11. Анализ Runtime

### 11.1 Статус: НЕ РЕАЛИЗОВАН

Runtime не содержит data pool, data pointer или механизм меток DATA.

### 11.2 Предлагаемые компоненты Runtime

```csharp
// В RuntimeEnvironment или отдельный DataPoolManager
public class DataPool
{
    private readonly List<RuntimeValue> _values;
    private readonly Dictionary<string, int> _labels;
    private int _pointer;

    public void AddValue(RuntimeValue value);
    public RuntimeValue ReadNext();
    public void Restore();
    public void RestoreToLabel(string label);
    public bool HasMoreData { get; }
}
```

### 11.3 Взаимодействие с IRuntimeHost

DATA/READ/RESTORE не взаимодействуют с `IRuntimeHost` напрямую — это чисто внутренний механизм runtime. Однако runtime-ошибки (MB3012, MB3007) передаются через существующий `RuntimeResult`.

---

## 12. Взаимодействие с типами

### 12.1 Поддерживаемые типы в DATA

| Тип | Поддержка | Примечание |
|---|---|---|
| INTEGER | ✅ | `DATA 42` |
| LONG | ✅ | `DATA 1234567890` |
| DOUBLE | ✅ | `DATA 3.14` |
| DECIMAL | ✅ | `DATA 100.00` |
| STRING | ✅ | `DATA "Hello"` |
| BOOLEAN | ✅ | `DATA TRUE` |
| ARRAY | ❌ | DATA не поддерживает массивы |
| TYPE | ❌ | DATA не поддерживает записи |
| IMAGE | ❌ | Не применимо |
| SPRITE | ❌ | Не применимо |
| FUNCTION | ❌ | Не применимо |

### 12.2 Преобразования типов при READ

READ должен использовать существующий механизм преобразований Axiom:
- STRING → INTEGER: через `VAL()`;
- INTEGER → DOUBLE: неявное расширение;
- DOUBLE → INTEGER: неявное сужение (с потерей);
- несовместимые типы: MB3007.

---

## 13. Взаимодействие со String

### 13.1 Строки в DATA

- строковые литералы в DATA заключены в кавычки: `DATA "Hello, World!"`;
- пустые строки допустимы: `DATA ""`;
- Unicode поддерживается (как и в остальном языке);
- fixed strings не применяются к DATA.

### 13.2 Строки в READ

- присваивание строки в строковую переменную — прямое копирование;
- присваивание числа в строковую переменную — конверсия через `STR()`;
- присваивание строки в числовую переменную — конверсия через `VAL()`.

---

## 14. Взаимодействие с Array

### 14.1 DATA и массивы

DATA не поддерживает массивы напрямую. Однако паттерн заполнения массива через DATA/READ классичен:

```basic
DIM values(1 TO 5) AS INTEGER
FOR i = 1 TO 5
    READ values(i)
NEXT i
DATA 10, 20, 30, 40, 50
```

Этот паттерн должен работать без специальной поддержки — READ читает в элемент массива как в обычную переменную.

---

## 15. Взаимодействие с TYPE

### 15.1 DATA и записи

DATA не поддерживает записи напрямую. Чтение полей TYPE через READ:

```basic
TYPE Point
    X AS INTEGER
    Y AS INTEGER
END TYPE

DIM p AS Point
READ p.X, p.Y
DATA 10, 20
```

Этот паттерн должен работать, если парсер и binder поддерживают `p.X` как l-value в READ.

---

## 16. Взаимодействие с Functions / SUB

### 16.1 DATA в процедурах

В классическом BASIC DATA-операторы глобальны — они видны из любого места программы. RESTORE также глобален.

Рекомендация для Axiom:
- DATA-операторы собираются в data pool при запуске программы;
- READ/RESTORE работают с глобальным data pool независимо от текущей области видимости;
- это соответствует классическому поведению и упрощает реализацию.

### 16.2 DATA в локальных функциях

DATA внутри локальной FUNCTION/SUB — допустимый синтаксис, но данные всё равно попадают в глобальный pool. Это может быть источником путаницы, но соответствует классическому BASIC.

---

## 17. Взаимодействие с Control Flow

### 17.1 DATA и поток исполнения

DATA-операторы **не исполняются** в обычном смысле — они только добавляют значения в data pool. Это означает:

- DATA внутри IF...THEN — данные доступны regardless of whether the branch is taken;
- DATA внутри цикла — данные добавляются только один раз (при первом проходе);
- DATA после END — данные всё равно доступны.

Это классическое поведение BASIC, но оно может быть counterintuitive. Рекомендуется:
- собирать DATA на этапе компиляции или при запуске программы;
- не привязывать добавление данных к потоку исполнения.

### 17.2 READ и поток исполнения

READ, напротив, является обычным оператором и исполняется в потоке:
- READ внутри IF — читает только если ветка взята;
- READ внутри цикла — читает на каждой итерации;
- READ после GOTO — читает следующее значение по указателю.

### 17.3 RESTORE и поток исполнения

RESTORE — обычный оператор, исполняемый в потоке.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Предлагаемые диагностические коды

| Код | Тип | Описание |
|---|---|---|
| MB1040 | Syntax | Ошибка синтаксиса в DATA |
| MB1041 | Syntax | Ошибка синтаксиса в READ |
| MB1042 | Syntax | Ошибка синтаксиса в RESTORE |
| MB2040 | Semantic | Неизвестная метка DATA в RESTORE |
| MB2041 | Semantic | READ в необъявленную переменную |
| MB2042 | Semantic | Несовместимый тип в READ |
| MB3012 | Runtime | Конец данных DATA (Out of Data) |
| MB3013 | Runtime | Несовместимый тип при чтении DATA |

### 18.2 Восстановление после ошибок

- MB3012 (конец данных) — программа должна завершиться с ошибкой;
- MB3013 (несовместимый тип) — программа должна завершиться с ошибкой;
- TRY/CATCH должен перехватывать runtime-ошибки DATA/READ.

---

## 19. State / Lifetime / Ownership

### 19.1 Data Pool

| Аспект | Значение |
|---|---|
| **Кто создаёт** | Runtime при запуске программы |
| **Кто владеет** | RuntimeEnvironment (или DataPoolManager) |
| **Кто изменяет** | READ (увеличивает указатель), RESTORE (сбрасывает указатель) |
| **Кто освобождает** | Runtime при завершении программы |
| **Когда уничтожается** | При завершении программы или сбросе runtime |

### 19.2 Data Pointer

| Аспект | Значение |
|---|---|
| **Кто создаёт** | Runtime при запуске программы |
| **Кто владеет** | RuntimeEnvironment |
| **Кто изменяет** | READ (increment), RESTORE (reset) |
| **Кто освобождает** | Часть DataPool |
| **Когда уничтожается** | При завершении программы |

### 19.3 Labels

| Аспект | Значение |
|---|---|
| **Кто создаёт** | Binder или Lowerer (при первом проходе) |
| **Кто владеет** | DataPool |
| **Кто изменяет** | Не изменяются после создания |
| **Кто освобождает** | Часть DataPool |
| **Когда уничтожается** | При завершении программы |

---

## 20. Edge Cases

### 20.1 Предлагаемые edge cases для тестирования

| Сценарий | Ожидаемое поведение |
|---|---|
| Пустой DATA | `READ` → MB3012 |
| READ больше значений, чем есть в DATA | MB3012 |
| READ меньше значений, чем есть в DATA | Указатель остаётся на позиции |
| RESTORE без аргумента | Указатель → 0 |
| RESTORE с меткой | Указатель → позиция метки |
| RESTORE с несуществующей меткой | MB2040 |
| DATA с пустой строкой | `""` — корректное значение |
| DATA с запятой внутри строки | `"Hello, World"` — корректное значение |
| DATA с кавычками внутри строки | Экранирование |
| READ в переменную несовместимого типа | MB3013 |
| DATA внутри непройденной ветки IF | Данные всё равно доступны |
| DATA внутри цикла | Данные добавляются один раз |
| READ после всех данных | MB3012 |
| RESTORE + READ в цикле | Бесконечное чтение одних данных |
| DATA с очень большим числом значений | Нет ограничения (кроме памяти) |
| DATA с очень длинной строкой | Нет ограничения (кроме памяти) |
| Вложенные READ в рекурсии | Глобальный указатель — каждый READ двигает его |
| DATA в локальной FUNCTION | Данные глобальны |
| READ из замыкания | Глобальный data pool |

---

## 21. Interpreter ↔ Bytecode VM

### 21.1 Паритет

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| DATA (сбор данных) | ❌ | ❌ | N/A | N/A | N/A |
| READ (чтение) | ❌ | ❌ | N/A | N/A | N/A |
| RESTORE (сброс) | ❌ | ❌ | N/A | N/A | N/A |
| Метки DATA | ❌ | ❌ | N/A | N/A | N/A |
| Указатель чтения | ❌ | ❌ | N/A | N/A | N/A |

**Расхождений нет** — оба движка одинаково не поддерживают подсистему.

---

## 22. Анализ производительности

### 22.1 Производительность не применима

Подсистема не реализована, поэтому производительность не может быть измерена.

### 22.2 Предлагаемые сценарии для будущих benchmarks

| Сценарий | Описание |
|---|---|
| `data_read_1000` | Чтение 1000 значений из DATA |
| `data_read_string_heavy` | Чтение 100 строковых значений |
| `data_restore_loop` | RESTORE + READ в цикле 10000 итераций |
| `data_large_pool` | DATA pool с 100000 значений |

---

## 23. Качество Benchmark

### 23.1 Benchmarks отсутствуют

Нет benchmarks для несуществующей подсистемы.

---

## 24. Hot Paths и Bottlenecks

### 24.1 Hot paths не применимы

Подсистема не реализована.

### 24.2 Потенциальные hot paths при реализации

- **READ в цикле** — каждый вызов READ увеличивает указатель и возвращает значение. Это O(1) операция.
- **RESTORE в цикле** — сброс указателя — O(1) операция.
- **Сбор DATA при запуске** — O(N) где N — количество DATA-операторов. Выполняется один раз.

---

## 25. Архитектурные проблемы

### 25.1 Архитектурных проблем нет (подсистема не существует)

Поскольку подсистема не реализована, архитектурных проблем в ней нет. Однако при проектировании необходимо учитывать:

1. **Глобальность data pool** — DATA/READ/RESTORE в классическом BASIC глобальны. Это не противоречит архитектуре Axiom, но должно быть явно задокументировано.

2. **Двухпроходная модель** — DATA должны быть собраны до первого READ. Это требует либо предварительного прохода, либо сбора при первом исполнении.

3. **Паритет Interpreter/VM** — оба движка должны использовать одинаковый data pool и одинаковые правила.

---

## 26. Дублирование логики

### 26.1 Дублирования нет (подсистема не существует)

Поскольку подсистема не реализована, дублирования логики нет.

### 26.2 Потенциальное дублирование при реализации

При реализации необходимо избежать дублирования:
- data pool должен быть в одном месте (RuntimeEnvironment);
- логика преобразования типов должна использовать существующий `BindConversion`;
- логика runtime-ошибок должна использовать существующий `RuntimeResult`.

---

## 27. Single Source of Truth

### 27.1 Источник истины для DATA/READ/RESTORE

При реализации единственным источником истины должны быть:
- **Семантика:** `docs/SPECIFICATION3.md` (после добавления);
- **Реализация:** `src/Axiom.Runtime/` (data pool, pointer);
- **Тесты:** `tests/Axiom.Runtime.Tests/` (автоматические тесты);
- **Документация:** `docs/manual/data.html` (пользовательский справочник).

---

## 28. Legacy / Dead Code

### 28.1 Legacy кода нет

Подсистема не реализована, legacy кода нет.

---

## 29. Надёжность и безопасность

### 29.1 Риски при реализации

| Риск | Описание | Митигация |
|---|---|---|
| **Exhaustion** | Очень большой DATA pool может исчерпать память | Лимит на количество значений (например, 1M) |
| **Infinite loop** | RESTORE + READ в бесконечном цикле | Таймаут выполнения (уже реализован в Axiom) |
| **Type confusion** | READ несовместимого типа | Строгая проверка типов через Binder |
| **Stale pointer** | Указатель после ошибки | Сброс указателя при перезапуске программы |

---

## 30. Анализ тестов

### 30.1 Тесты отсутствуют

Нет ни одного теста для DATA/READ/RESTORE.

### 30.2 Предлагаемые тесты

#### Unit-тесты (парсер)
- `Parse_DataStatement_SingleValue`
- `Parse_DataStatement_MultipleValues`
- `Parse_DataStatement_StringWithComma`
- `Parse_DataStatement_Empty`
- `Parse_ReadStatement_SingleVariable`
- `Parse_ReadStatement_MultipleVariables`
- `Parse_RestoreStatement_NoLabel`
- `Parse_RestoreStatement_WithLabel`

#### Semantic-тесты
- `Bind_ReadReportsUndeclaredVariable`
- `Bind_ReadReportsIncompatibleType`
- `Bind_RestoreReportsUnknownLabel`

#### Runtime-тесты (Interpreter)
- `Execute_DataReadBasicValues`
- `Execute_DataReadStringValues`
- `Execute_DataReadMixedTypes`
- `Execute_ReadReportsOutOfData`
- `Execute_ReadReportsIncompatibleType`
- `Execute_RestoreResetsPointer`
- `Execute_RestoreToLabel`
- `Execute_RestoreReportsUnknownLabel`
- `Execute_DataInsideIfBranch_StillAccessible`
- `Execute_DataInsideLoop_AddedOnce`
- `Execute_ReadInLoop`
- `Execute_RestoreReadLoop`

#### Runtime-тесты (BytecodeVm)
- Все вышеперечисленные тесты должны быть продублированы для BytecodeVm.

#### Engine-comparison тесты
- `EngineCompare_DataReadBasic`
- `EngineCompare_DataReadString`
- `EngineCompare_DataRestoreLoop`

---

## 31. Недостающие тесты

### 31.1 Все тесты отсутствуют

Все категории тестов отсутствуют:
- Обычные сценарии: ❌
- Отрицательные сценарии: ❌
- Граничные случаи: ❌
- Regression: ❌
- Parity: ❌
- Performance: ❌

---

## 32. Анализ Samples

### 32.1 Samples отсутствуют

Нет ни одного runnable `.axm` примера для DATA/READ/RESTORE.

### 32.2 Предлагаемые samples

| Sample | Описание |
|---|---|
| `samples/operators/data/basic_usage.axm` | Базовое использование DATA/READ |
| `samples/operators/data/string_data.axm` | Строковые данные |
| `samples/operators/data/mixed_types.axm` | Смешанные типы |
| `samples/operators/read/single_variable.axm` | Чтение в одну переменную |
| `samples/operators/read/multiple_variables.axm` | Чтение в несколько переменных |
| `samples/operators/read/array_fill.axm` | Заполнение массива через DATA/READ |
| `samples/operators/restore/basic_restore.axm` | Базовый RESTORE |
| `samples/operators/restore/restore_to_label.axm` | RESTORE с меткой |
| `samples/operators/restore/restore_loop.axm` | Циклическое чтение через RESTORE |

---

## 33. Анализ документации

### 33.1 Документация отсутствует

Нет ни одной страницы manual для DATA/READ/RESTORE.

### 33.2 Предлагаемые страницы manual

| Страница | Содержание |
|---|---|
| `docs/manual/data.html` | Оператор DATA |
| `docs/manual/read.html` | Оператор READ |
| `docs/manual/restore.html` | Оператор RESTORE |

### 33.3 Обновление существующих страниц

- `docs/manual/diagnostics.html` — добавить MB1040-MB1042, MB2040-MB2042, MB3012-MB3013;
- `docs/manual/index.html` — добавить ссылки на новые страницы;
- `docs/manual/scripts/manual.js` — добавить DATA/READ/RESTORE в индекс.

---

## 34. Интеграция с IDE

### 34.1 IDE не поддерживает DATA/READ/RESTORE

- Подсветка синтаксиса: ❌ DATA, READ, RESTORE не в `Axiom.xshd` и `Axiom.Dark.xshd`;
- Автодополнение: ❌ Не реализовано;
- Диагностики: ❌ Не реализованы;
- Форматирование: ❌ Не реализовано.

### 34.2 Предлагаемые изменения IDE

- Добавить `DATA`, `READ`, `RESTORE` в оба файла подсветки (`Axiom.xshd`, `Axiom.Dark.xshd`);
- Добавить в словарь подсветки `AxiomHighlightingVocabulary`;
- Добавить тесты в `tests/Axiom.Ide.Tests/`.

---

## 35. Зависимости от Host / Platform

### 35.1 Зависимостей нет

DATA/READ/RESTORE не зависят от хоста, файловой системы, GPU или ОС. Это чисто внутренний механизм runtime.

---

## 36. Карта корневых причин

### 36.1 ROOT-SYS-001: Подсистема не реализована

**Корневая причина:** DATA/READ/RESTORE никогда не были запланированы как приоритетная feature. В PROJECT.md они не упоминаются как обязательные для v0.1. В спецификации они упоминаются как будущее расширение.

**Затронутые компоненты:**
- SYS-001 (DATA)
- SYS-002 (READ)
- SYS-003 (RESTORE)
- SYS-004 (Метки)
- SYS-005 (Указатель чтения)

**Направление исправления:** Полная реализация подсистемы с нуля.

---

## 37. Стабильные области

### 37.1 Области, которые не следует трогать

Поскольку подсистема не реализована, нет областей, которые можно было бы трогать или не трогать. Однако **существующая I/O инфраструктура стабильна и не должна быть нарушена** при реализации DATA/READ/RESTORE:
- `IRuntimeHost` — не изменять;
- `RuntimeResult` — не изменять;
- `RuntimeEnvironment` — расширять, не переписывать;
- `Interpreter.Statements.cs` — добавлять новые case, не менять существующие.

---

## 38. Возможности консолидации

### 38.1 Логика, которую следует объединить

При реализации DATA/READ/RESTORE необходимо использовать существующие механизмы:
- **Преобразование типов** — использовать `BindConversion` из Binder, не создавать отдельную логику;
- **Runtime-ошибки** — использовать `RuntimeResult` и `DiagnosticBag`, не создавать отдельную систему;
- **Подсветка IDE** — использовать существующий паттерн `Axiom.xshd` / `Axiom.Dark.xshd`.

---

## 39. Возможности разделения ответственности

### 39.1 Предлагаемое разделение

| Компонент | Ответственность |
|---|---|
| **Lexer** | Распознавание ключевых слов DATA, READ, RESTORE |
| **Parser** | Построение синтаксических узлов |
| **Binder** | Связывание переменных READ, проверка типов, метки |
| **Interpreter** | Исполнение через data pool + указатель |
| **BytecodeVm** | Исполнение через байткодные опкоды |
| **Runtime** | Хранение data pool, указателя, меток |
| **IDE** | Подсветка ключевых слов |

---

## 40. Кандидаты на удаление

### 40.1 Кандидатов нет

Подсистема не реализована, нечего удалять.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Оценка готовности

| Аспект | Готовность | Примечание |
|---|---|---|
| **Будущий Compiler** | ✅ Готова | DATA pool может быть собран на этапе компиляции |
| **Дальнейшая оптимизация** | ✅ Готова | data pool может быть оптимизирован (константный pool) |
| **Специализированные Opcode** | ✅ Готова | LoadDataValue, RestoreDataPointer — чистые опкоды |
| **Typed fast paths** | ✅ Готова | READ может иметь fast path для INTEGER/DOUBLE |
| **Развитие Runtime** | ✅ Готова | DataPool — изолированный компонент |
| **Развитие IDE** | ✅ Готова | Подсветка — стандартная процедура |
| **Расширение языка** | ✅ Готова | DATA может быть расширен до выражений |

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P3 | FEATURE GAP | Все уровни | DATA не реализован | Отсутствие в коде | Нереализованная feature | CONFIRMED |
| SYS-002 | P3 | FEATURE GAP | Все уровни | READ не реализован | Отсутствие в коде | Нереализованная feature | CONFIRMED |
| SYS-003 | P3 | FEATURE GAP | Все уровни | RESTORE не реализован | Отсутствие в коде | Нереализованная feature | CONFIRMED |
| SYS-004 | P3 | FEATURE GAP | Все уровни | Метки DATA не реализованы | Отсутствие в коде | Нереализованная feature | CONFIRMED |
| SYS-005 | P3 | FEATURE GAP | Runtime | Указатель чтения не реализован | Отсутствие в коде | Нереализованная feature | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Проектирование (1-2 дня)

**Цель:** Зафиксировать семантический контракт DATA/READ/RESTORE в спецификации.

**Задачи:**
1. Добавить раздел DATA/READ/RESTORE в `docs/SPECIFICATION3.md`;
2. Определить: DATA только с литералами или с произвольными выражениями;
3. Определить: метки — именованные или позиционные;
4. Определить: поведение DATA внутри непройденных веток;
5. Определить: лимиты на количество DATA-значений.

**Файлы:** `docs/SPECIFICATION3.md`

### ЭТАП 1 — Lexer + Parser + Syntax (1-2 дня)

**Цель:** Добавить ключевые слова и синтаксические узлы.

**Задачи:**
1. Добавить `DataKeyword`, `ReadKeyword`, `RestoreKeyword` в `TokenKind.cs`;
2. Добавить маппинг в `Lexer.cs`;
3. Добавить `DataStatementSyntax`, `ReadStatementSyntax`, `RestoreStatementSyntax` в `Statements.cs`;
4. Добавить `SyntaxKind` для новых узлов;
5. Добавить `ParseDataStatement`, `ParseReadStatement`, `ParseRestoreStatement` в `Parser.Statements.cs`;
6. Добавить тесты парсера.

**Файлы:** `src/Axiom.Lexing/TokenKind.cs`, `src/Axiom.Lexing/Lexer.cs`, `src/Axiom.Syntax/Statements.cs`, `src/Axiom.Syntax/SyntaxKind.cs`, `src/Axiom.Parsing/Parser.Statements.cs`

**Зависимости:** ЭТАП 0

### ЭТАП 2 — Binder + Semantics (1-2 дня)

**Цель:** Связать READ с переменными, проверить типы.

**Задачи:**
1. Добавить `BoundDataStatement`, `BoundReadStatement`, `BoundRestoreStatement` в `BoundStatements.cs`;
2. Добавить `BoundNodeKind` для новых узлов;
3. Добавить `BindDataStatement`, `BindReadStatement`, `BindRestoreStatement` в `Binder.Statements.cs`;
4. Добавить диагностические коды MB1040-MB1042, MB2040-MB2042;
5. Добавить тесты семантики.

**Файлы:** `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`, `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`, `src/Axiom.Semantics/Binding/Binder.Statements.cs`, `src/Axiom.Core/DiagnosticCodes.cs`

**Зависимости:** ЭТАП 1

### ЭТАП 3 — Runtime (1-2 дня)

**Цель:** Реализовать data pool и указатель чтения.

**Задачи:**
1. Создать `DataPool` в `src/Axiom.Runtime/` (или расширить `RuntimeEnvironment`);
2. Добавить MB3012, MB3013 в диагностические коды;
3. Добавить тесты data pool.

**Файлы:** `src/Axiom.Runtime/DataPool.cs` (новый), `src/Axiom.Runtime/Environment/RuntimeEnvironment.cs`

**Зависимости:** ЭТАП 2

### ЭТАП 4 — Interpreter (1-2 дня)

**Цель:** Исполнять DATA/READ/RESTORE через интерпретатор.

**Задачи:**
1. Добавить `ExecuteDataStatement`, `ExecuteReadStatement`, `ExecuteRestoreStatement` в `Interpreter.Statements.cs`;
2. Добавить dispatch в `ExecuteStatements`;
3. Добавить runtime-тесты.

**Файлы:** `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`

**Зависимости:** ЭТАП 3

### ЭТАП 5 — Lowering + Bytecode + VM (2-3 дня)

**Цель:** Исполнять DATA/READ/RESTORE через байткодную VM.

**Задачи:**
1. Добавить опкоды `LoadDataValue`, `RestoreDataPointer`, `RestoreToLabel` в `Opcode.cs`;
2. Добавить lowering в `BoundProgramLowerer.cs`;
3. Добавить эмиссию в `BytecodeCompiler.cs`;
4. Добавить исполнение в `BytecodeVm.cs`;
5. Добавить parity-тесты.

**Файлы:** `src/Axiom.Bytecode/Opcode.cs`, `src/Axiom.Bytecode/BoundProgramLowerer.cs`, `src/Axiom.Bytecode/BytecodeCompiler.cs`, `src/Axiom.Runtime/Execution/BytecodeVm.cs`

**Зависимости:** ЭТАП 4

### ЭТАП 6 — IDE (1 день)

**Цель:** Подсветка ключевых слов.

**Задачи:**
1. Добавить `DATA`, `READ`, `RESTORE` в `Axiom.xshd` и `Axiom.Dark.xshd`;
2. Добавить тесты подсветки.

**Файлы:** `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`, `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`

**Зависимости:** ЭТАП 1

### ЭТАП 7 — Samples + Manual (1-2 дня)

**Цель:** Документация и примеры.

**Задачи:**
1. Создать `samples/operators/data/`, `samples/operators/read/`, `samples/operators/restore/`;
2. Создать `docs/manual/data.html`, `docs/manual/read.html`, `docs/manual/restore.html`;
3. Обновить `docs/manual/diagnostics.html`, `docs/manual/index.html`, `docs/manual/scripts/manual.js`.

**Файлы:** `samples/operators/data/`, `samples/operators/read/`, `samples/operators/restore/`, `docs/manual/data.html`, `docs/manual/read.html`, `docs/manual/restore.html`

**Зависимости:** ЭТАП 5

### ЭТАП 8 — Интеграция и стабилизация (1-2 дня)

**Цель:** Сквозные тесты и smoke-тесты.

**Задачи:**
1. Добавить acceptance-тесты в `samples/acceptance/`;
2. Добавить engine-comparison тесты в `samples/smoke/engine-compare/`;
3. Обновить `docs/BYTECODE_COVERAGE.md`;
4. Обновить `docs/archive/root-documents-20261011/LANGUAGE_INVENTORY.md`.

**Файлы:** `samples/acceptance/`, `samples/smoke/engine-compare/`, `docs/BYTECODE_COVERAGE.md`, `docs/archive/root-documents-20261011/LANGUAGE_INVENTORY.md`

**Зависимости:** ЭТАП 7

---

## 44. Граф зависимостей задач

```
ЭТАП 0 (Проектирование)
    ↓
ЭТАП 1 (Lexer + Parser + Syntax) ──→ ЭТАП 6 (IDE подсветка)
    ↓
ЭТАП 2 (Binder + Semantics)
    ↓
ЭТАП 3 (Runtime)
    ↓
ЭТАП 4 (Interpreter)
    ↓
ЭТАП 5 (Lowering + Bytecode + VM)
    ↓
ЭТАП 7 (Samples + Manual)
    ↓
ЭТАП 8 (Интеграция и стабилизация)
```

---

## 45. Definition of Done

### Для подсистемы DATA/READ/RESTORE

**Корректность:**
- [ ] DATA собирает литеральные значения в data pool;
- [ ] READ читает значения из data pool в переменные;
- [ ] RESTORE сбрасывает указатель на начало или на метку;
- [ ] Метки DATA работают корректно;
- [ ] Преобразования типов при READ работают как при присваивании.

**Edge cases:**
- [ ] Пустой DATA → MB3012 при READ;
- [ ] READ больше значений, чем есть → MB3012;
- [ ] READ несовместимого типа → MB3013;
- [ ] RESTORE с несуществующей меткой → MB2040;
- [ ] DATA внутри непройденной ветки — данные доступны;
- [ ] DATA внутри цикла — данные добавляются один раз.

**Error handling:**
- [ ] Все диагностические коды MB1040-MB1042, MB2040-MB2042, MB3012-MB3013 реализованы;
- [ ] TRY/CATCH перехватывает runtime-ошибки DATA/READ.

**Interpreter:**
- [ ] DATA/READ/RESTORE работают через интерпретатор;
- [ ] Все runtime-тесты проходят.

**Bytecode VM:**
- [ ] DATA/READ/RESTORE работают через байткодную VM;
- [ ] Parity-тесты Interpreter vs VM проходят.

**Тестирование:**
- [ ] ≥ 20 автоматических тестов (парсер, семантика, runtime, parity);
- [ ] ≥ 5 негативных acceptance-тестов;
- [ ] ≥ 3 engine-comparison теста.

**Документация:**
- [ ] Страницы manual: data.html, read.html, restore.html;
- [ ] Обновлена diagnostics.html;
- [ ] Обновлена LANGUAGE_INVENTORY.md.

**IDE:**
- [ ] Подсветка DATA, READ, RESTORE в обеих темах;
- [ ] Тесты подсветки.

**Performance:**
- [ ] READ 1000 значений < 1ms;
- [ ] RESTORE + READ loop 10000 итераций < 10ms.

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Категория | Проблема |
|---|---|---|---|---|
| 1 | SYS-001 | P3 | FEATURE GAP | DATA не реализован |
| 2 | SYS-002 | P3 | FEATURE GAP | READ не реализован |
| 3 | SYS-003 | P3 | FEATURE GAP | RESTORE не реализован |
| 4 | SYS-004 | P3 | FEATURE GAP | Метки DATA не реализованы |
| 5 | SYS-005 | P3 | FEATURE GAP | Указатель чтения не реализован |

**Примечание:** Все 5 находок — это feature gaps, не баги. Подсистема не реализована, поэтому нет проблем корректности в существующем коде.

---

## 47. ТОП-20 задач по производительности

| # | ID | Категория | Задача |
|---|---|---|---|
| 1 | PERF-001 | DATA | Сбор data pool при запуске (O(N)) |
| 2 | PERF-002 | READ | Чтение значения из pool (O(1)) |
| 3 | PERF-003 | RESTORE | Сброс указателя (O(1)) |
| 4 | PERF-004 | READ | Преобразование типов (существующий механизм) |
| 5 | PERF-005 | DATA | Хранение в памяти (лимит?) |

**Примечание:** Все задачи — проектировочные, не оптимизационные. Подсистема не реализована.

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Оценка | Объяснение |
|---|---|---|
| Корректность | N/A | Подсистема не реализована |
| Семантика | N/A | Подсистема не реализована |
| Архитектура | N/A | Подсистема не реализована |
| Runtime | N/A | Подсистема не реализована |
| Interpreter | N/A | Подсистема не реализована |
| Bytecode VM | N/A | Подсистема не реализована |
| Производительность | N/A | Подсистема не реализована |
| Память | N/A | Подсистема не реализована |
| Error handling | N/A | Подсистема не реализована |
| Tests | 0 | Нет ни одного теста |
| Samples | 0 | Нет ни одного примера |
| Documentation | 0 | Нет ни одной страницы manual |
| IDE | 0 | Нет подсветки |
| Maintainability | N/A | Подсистема не реализована |
| Reliability | N/A | Подсистема не реализована |
| Future readiness | 8 | Инфраструктура готова к реализации |

**Общий Health Score: 0/10** (не применимо — подсистема не существует).

**Оценка готовности к реализации: 8/10** — существующая архитектура Axiom (lexer → parser → binder → interpreter/VM) полностью подходит для DATA/READ/RESTORE. Все необходимые паттерны уже отработаны на других features.

---

## Главный контрольный вопрос

> Можно ли считать подсистему DATA/READ/RESTORE достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**NO**

Подсистема полностью отсутствует. Она не может быть оценена на стабильность, корректность или архитектурную ясность, потому что не существует.

---

## Основные blockers

1. **DATA/READ/RESTORE не реализованы ни на одном уровне** — от лексера до runtime;
2. **Нет семантического контракта** — не определено поведение в спецификации;
3. **Нет тестов** — невозможно проверить корректность;
4. **Нет документации** — пользователь не знает о возможности.

---

## Что нужно сделать в первую очередь

1. Зафиксировать семантический контракт в `docs/SPECIFICATION3.md`;
2. Реализовать лексер + парсер + syntax (ЭТАП 1);
3. Реализовать binder + semantics (ЭТАП 2);
4. Реализовать runtime (data pool) (ЭТАП 3).

---

## Что можно отложить

1. Байткодная VM (ЭТАП 5) — можно начать с интерпретатора;
2. IDE-подсветка (ЭТАП 6) — можно добавить после базовой реализации;
3. Samples и manual (ЭТАП 7) — можно добавить после stabilization;
4. DATA с произвольными выражениями — начать с литералов.

---

## Что не следует трогать

1. **IRuntimeHost** — не изменять контракт;
2. **RuntimeResult** — не изменять модель результата;
3. **Существующие операторы** — не менять поведение INPUT, PRINT, DIM;
4. **Архитектуру конвейера** — не нарушать разделение слоёв.
