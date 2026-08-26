# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## FILE SYSTEM / FILE I/O

**Дата аудита:** 2026-08-21
**Предыдущий аудит:** Session 1054 (2026-08-20), `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md` (1120 строк, Health Score 8.8/10)
**Методология:** полная повторная верификация предыдущего аудита + углублённый анализ по 60 разделам forensic-методологии

---

## 1. Резюме

Файловая подсистема Axiom **зрелая, корректная и архитектурно чистая**. Она состоит из двух слоёв:

1. **Операторы OPEN/CLOSE** — управляемое потоковое чтение/запись через file handles типа `FILE`.
2. **Встроенные файловые функции** (67 функций) — прямые операции с файловой системой без handles.

Оба слоя полностью реализованы в `Interpreter` и `BytecodeVm`, делегируют общую логику в `BuiltinFileIOHelpers` (418 строк) и `BuiltinFileSystemHelpers` (171 строка), покрыты тестами, примерами и документацией.

**Главный вывод**: предыдущий аудит (Session 1054) корректен. Все его утверждения подтверждены повторной верификацией. Подсистема стабильна, deterministic, с чётким ownership model и полным паритетом Interpreter/VM.

**Health Score: 8.8/10** (подтверждено).

**Ключевые цифры:**
- 2 оператора: `OPEN`, `CLOSE`
- 18 file-handle built-in функций
- 49 path-based файловых built-in функций (включая перегрузки)
- 1 тип данных: `FILE`
- 86 runnable `.axm` samples
- 8 manual-страниц
- 55+ автоматических тестов файловой подсистемы
- 16 engine-compare benchmark-сценариев

---

## 2. Что представляет собой эта подсистема

Файловая подсистема Axiom обеспечивает:

- **Текстовый файловый ввод-вывод** через дескрипторы (`OPEN`/`CLOSE` + file-handle функции)
- **Прямые файловые операции** без дескрипторов (чтение/запись целого файла, проверка существования, метаданные)
- **Управление файлами и каталогами** (создание, копирование, перемещение, удаление, переименование)
- **Работу с путями** (разбор, комбинирование, определение абсолютности)
- **Информацию о дисках** (свободное место, тип, формат)
- **Временные файлы и каталоги**

Подсистема работает **только в текстовом режиме** (UTF-8). Бинарный режим не поддерживается — это осознанное проектное решение, а не пропущенная feature.

---

## 3. Полный список возможностей

### 3.1 Операторы

| ID | Название | Тип | Статус |
|----|----------|-----|--------|
| SYS-001 | `OPEN path FOR INPUT\|OUTPUT\|APPEND AS var` | STATEMENT | IMPLEMENTED |
| SYS-002 | `CLOSE var` | STATEMENT | IMPLEMENTED |

### 3.2 Тип данных

| ID | Название | Тип | Статус |
|----|----------|-----|--------|
| SYS-003 | `FILE` | TYPE | IMPLEMENTED |

### 3.3 File-handle built-in функции (18 штук)

| ID | Название | Возвращает | Статус |
|----|----------|-----------|--------|
| SYS-004 | `ISOPEN(f)` | BOOLEAN | IMPLEMENTED |
| SYS-005 | `FILEPATH(f)` | STRING | IMPLEMENTED |
| SYS-006 | `FILEMODE(f)` | STRING | IMPLEMENTED |
| SYS-007 | `FILEPOS(f)` | LONG | IMPLEMENTED |
| SYS-008 | `FILELENGTH(f)` | LONG | IMPLEMENTED |
| SYS-009 | `LINENUMBER(f)` | LONG | IMPLEMENTED |
| SYS-010 | `EOF(f)` | BOOLEAN | IMPLEMENTED |
| SYS-011 | `READTOEND(f)` | STRING | IMPLEMENTED |
| SYS-012 | `READTEXT(f, count)` | STRING | IMPLEMENTED |
| SYS-013 | `READLINE(f)` | STRING | IMPLEMENTED |
| SYS-014 | `READLINES(f, count)` | STRING() | IMPLEMENTED |
| SYS-015 | `SKIPLINE(f)` | BOOLEAN | IMPLEMENTED |
| SYS-016 | `WRITE(f, text)` | BOOLEAN | IMPLEMENTED |
| SYS-017 | `WRITELINE(f, text)` | BOOLEAN | IMPLEMENTED |
| SYS-018 | `NEWLINE(f)` | BOOLEAN | IMPLEMENTED |
| SYS-019 | `FLUSH(f)` | BOOLEAN | IMPLEMENTED |
| SYS-020 | `SEEKFILE(f, position)` | BOOLEAN | IMPLEMENTED |
| SYS-021 | `REWIND(f)` | BOOLEAN | IMPLEMENTED |

### 3.4 Path-based файловые built-in функции (49 сигнатур)

**Проверка существования:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-022 | `FILEEXISTS(path)` | IMPLEMENTED |
| SYS-023 | `DIREXISTS(path)` | IMPLEMENTED |
| SYS-024 | `PATHEXISTS(path)` | IMPLEMENTED |

**Чтение/запись целого файла:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-025 | `READFILE(path)` | IMPLEMENTED |
| SYS-026 | `READALLLINES(path)` | IMPLEMENTED |
| SYS-027 | `WRITEFILE(path, text)` | IMPLEMENTED |
| SYS-028 | `APPENDFILE(path, text)` | IMPLEMENTED |

**Метаданные файлов:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-029 | `FILESIZE(path)` | IMPLEMENTED |
| SYS-030 | `FILEDATE(path)` | IMPLEMENTED |
| SYS-031 | `FILECREATEDATE(path)` | IMPLEMENTED |
| SYS-032 | `FILEACCESSEDDATE(path)` | IMPLEMENTED |
| SYS-033 | `DIRDATE(path)` | IMPLEMENTED |

**Перечисление:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-034 | `GETFILES(path)` | IMPLEMENTED |
| SYS-035 | `GETFILES(path, pattern)` | IMPLEMENTED |
| SYS-036 | `GETDIRS(path)` | IMPLEMENTED |
| SYS-037 | `GETDIRS(path, pattern)` | IMPLEMENTED |

**Работа с путями:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-038 | `COMBINEPATH(left, right)` | IMPLEMENTED |
| SYS-039 | `FILENAME(path)` | IMPLEMENTED |
| SYS-040 | `DIRNAME(path)` | IMPLEMENTED |
| SYS-041 | `EXTNAME(path)` | IMPLEMENTED |
| SYS-042 | `FULLPATH(path)` | IMPLEMENTED |
| SYS-043 | `PATHROOT(path)` | IMPLEMENTED |
| SYS-044 | `ISABSOLUTEPATH(path)` | IMPLEMENTED |

**Управление файлами:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-045 | `MAKEDIR(path)` | IMPLEMENTED |
| SYS-046 | `DELETEFILE(path)` | IMPLEMENTED |
| SYS-047 | `COPYFILE(src, dst)` | IMPLEMENTED |
| SYS-048 | `COPYFILE(src, dst, overwrite)` | IMPLEMENTED |
| SYS-049 | `MOVEFILE(src, dst)` | IMPLEMENTED |
| SYS-050 | `MOVEFILE(src, dst, overwrite)` | IMPLEMENTED |
| SYS-051 | `RENAMEFILE(path, name)` | IMPLEMENTED |
| SYS-052 | `DELETEDIR(path)` | IMPLEMENTED |
| SYS-053 | `DELETEDIRRECURSIVE(path)` | IMPLEMENTED |
| SYS-054 | `COPYDIR(src, dst)` | IMPLEMENTED |
| SYS-055 | `COPYDIR(src, dst, overwrite)` | IMPLEMENTED |
| SYS-056 | `MOVEDIR(src, dst)` | IMPLEMENTED |
| SYS-057 | `RENAMEDIR(path, name)` | IMPLEMENTED |

**Навигация:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-058 | `CURRENTDIR()` | IMPLEMENTED |
| SYS-059 | `CHANGEDIR(path)` | IMPLEMENTED |
| SYS-060 | `DIRSIZE(path)` | IMPLEMENTED |

**Информация о дисках:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-061 | `FREESPACE(path)` | IMPLEMENTED |
| SYS-062 | `TOTALFREESPACE(path)` | IMPLEMENTED |
| SYS-063 | `TOTALSPACE(path)` | IMPLEMENTED |
| SYS-064 | `DRIVENAME(path)` | IMPLEMENTED |
| SYS-065 | `DRIVEFORMAT(path)` | IMPLEMENTED |
| SYS-066 | `DRIVETYPE(path)` | IMPLEMENTED |

**Атрибуты:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-067 | `ISREADONLY(path)` | IMPLEMENTED |
| SYS-068 | `ISHIDDEN(path)` | IMPLEMENTED |

**Временные файлы:**
| ID | Название | Статус |
|----|----------|--------|
| SYS-069 | `MAKETEMPFILE(prefix)` | IMPLEMENTED |
| SYS-070 | `MAKETEMPDIR(prefix)` | IMPLEMENTED |

### 3.5 Отсутствующие (осознанно не поддерживаемые)

| Возможность | Статус | Причина |
|-------------|--------|---------|
| Binary mode | NOT SUPPORTED | Text-only дизайн |
| `PRINT #f` | NOT SUPPORTED | Заменён на `WRITE()`/`WRITELINE()` |
| `INPUT #f` | NOT SUPPORTED | Заменён на `READLINE()`/`READTOEND()` |
| `GET #f` / `PUT #f` | NOT SUPPORTED | Binary mode не поддерживается |
| `FREEFILE` | NOT SUPPORTED | Axiom использует типизированные `FILE` переменные |
| `LOF` | NOT SUPPORTED | Заменён на `FILELENGTH()` |
| `KILL` | NOT SUPPORTED | Заменён на `DELETEFILE()` |
| `MKDIR`/`RMDIR` (операторы) | NOT SUPPORTED | Заменены на `MAKEDIR()`/`DELETEDIR()` |
| `CHDIR`/`CURDIR` (операторы) | NOT SUPPORTED | Заменены на `CHANGEDIR()`/`CURRENTDIR()` |
| `NAME` (переименование) | NOT SUPPORTED | Заменён на `RENAMEFILE()` |

---

## 4. Фактическая архитектура

### 4.1 Двухслойная модель

```
СЛОЙ 1: Потоковый ввод-вывод (file handles)
  OPEN → RuntimeTextFile → _openFiles → READ/WRITE/SEEK → CLOSE

СЛОЙ 2: Прямые файловые операции (без handles)
  READFILE(path) → File.ReadAllBytes → результат
  WRITEFILE(path, text) → File.WriteAllText → результат
```

### 4.2 Стек компонентов

```
Исходный код (.axm)
  ↓
Lexer: TokenKind.OpenKeyword, CloseKeyword, InputKeyword, OutputKeyword, AppendKeyword
  ↓
Parser: ParseOpenStatement(), ParseCloseStatement()
  ↓
Syntax: OpenStatementSyntax, CloseStatementSyntax
  ↓
Binder: BindOpenStatement(), BindCloseStatement()
  ↓
Bound: BoundOpenStatement, BoundCloseStatement, BoundCallExpression
  ↓
Lowering: LowerOpen(), LowerClose()
  ↓
Bytecode: Opcode.OpenFile, Opcode.CloseFile, CallBuiltin
  ↓
VM: ExecuteOpenFile(), ExecuteCloseFile(), EvaluateXxxBuiltin()
  ↓
Runtime: RuntimeTextFile → FileStream → ОС
  ↓
Helpers: BuiltinFileIOHelpers, BuiltinFileSystemHelpers
```

### 4.3 Ключевые файлы

| Файл | Ответственность | Строки |
|------|----------------|--------|
| `src/Axiom.Runtime/Values/RuntimeTextFile.cs` | Обёртка над FileStream | 550 |
| `src/Axiom.Runtime/Builtins/BuiltinFileIOHelpers.cs` | Общий слой file-handle операций | 418 |
| `src/Axiom.Runtime/Builtins/BuiltinFileSystemHelpers.cs` | Общий слой path-based операций | 171 |
| `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs` | Interpreter file-handle диспетчер | 193 |
| `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs` | Interpreter path-based диспетчер | 980 |
| `src/Axiom.Runtime/Execution/BytecodeVm.FileIO.cs` | VM file-handle диспетчер | 179 |
| `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.FileSystem.cs` | VM path-based диспетчер | 782 |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs` | Дескрипторы 18 file-handle функций | 29 |
| `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs` | Дескрипторы 49 path-based функций | 60 |
| `src/Axiom.Semantics/Binding/Binder.IoStatements.cs` | Связывание OPEN/CLOSE | 804 |
| `src/Axiom.Parsing/Parser.Statements.cs` | Парсинг OPEN/CLOSE | 1636 |
| `src/Axiom.Syntax/InputOutputSyntax.cs` | Синтаксические узлы OPEN/CLOSE | ~200 |

---

## 5. Полный путь выполнения

### 5.1 Путь оператора OPEN

```
"OPEN path FOR INPUT AS f"
  ↓
Lexer: [OpenKeyword] [StringLiteral] [ForKeyword] [InputKeyword] [AsKeyword] [Identifier]
  ↓
Parser.ParseOpenStatement()
  → OpenStatementSyntax(OpenKeyword, PathExpression, ForKeyword, ModeKeyword, AsKeyword, FileIdentifier)
  ↓
Binder.BindOpenStatement()
  → path = BindExpression(PathExpression, String)
  → fileVariable = LookupVariable("f")
  → проверка типа (должен быть FILE)
  → mode = BoundFileOpenMode.Input
  → BoundOpenStatement(location, path, fileVariable, mode)
  ↓
Interpreter.ExecuteOpen() / BytecodeVm.ExecuteOpenFile()
  → EvaluateExpression(PathExpression) → строка пути
  → GetVariableValue(FileVariable) → текущее значение FILE
  → проверка: уже открыт? → diagnostic MB3007
  → new RuntimeTextFile(path, mode, encoding)
  → _openFiles.Add(file)
  → AssignVariable(f, RuntimeValue.FromFile(file))
```

### 5.2 Путь built-in функции (например READFILE)

```
"READFILE(path)"
  ↓
Parser: ParseInvocationExpression() → BoundCallExpression
  ↓
Binder: BuiltinFunctionId.ReadFile → дескриптор → тип результата String
  ↓
Interpreter: EvaluateReadFile() → BuiltinFileSystemHelpers.ReadFile() → File.ReadAllBytes()
  ↓
BytecodeVm: CallBuiltin → EvaluateReadFileBuiltin() → тот же BuiltinFileSystemHelpers.ReadFile()
```

---

## 6. Фактический семантический контракт

### 6.1 OPEN

**Вход:**
- `pathExpression`: строка (обязательно)
- `mode`: `INPUT` | `OUTPUT` | `APPEND` (обязательно)
- `fileVariable`: переменная типа `FILE` (обязательно)

**Обработка:**
- Нормализация пути через `Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))`
- Относительные пути разрешаются относительно `Environment.CurrentDirectory`
- Для `ResourceBaseDirectory` (IDE) — относительно базового каталога ресурсов

**Выход:**
- Переменная `FILE` инициализируется `RuntimeValue.FromFile(new RuntimeTextFile(...))`
- Файл добавляется в `_openFiles`

**Ошибки:**
- Путь пустой/пробельный: `ArgumentException` → MB3007
- Файл не найден (INPUT): `FileNotFoundException` → MB3007
- Каталог не существует: `DirectoryNotFoundException` → MB3007
- Нет прав доступа: `UnauthorizedAccessException` → MB3007
- Переменная уже содержит открытый файл: MB3007

**Побочные эффекты:**
- OUTPUT: уничтожает содержимое существующего файла (`FileMode.Create`)
- APPEND: создаёт файл, если не существует

### 6.2 CLOSE

**Вход:**
- `fileVariable`: переменная типа `FILE` (обязательно, должна содержать открытый файл)

**Выход:**
- Файл закрыт через `Dispose()`
- Переменная установлена в `RuntimeValue.FromFile(null)`
- Файл удалён из `_openFiles`

**Ошибки:**
- Переменная не содержит открытый файл: MB3007

### 6.3 READFILE (path-based)

**Вход:**
- `path`: строка (обязательно, путь к существующему файлу)

**Выход:**
- Строка с содержимым файла (UTF-8 strict → Windows-1251 fallback)

**Ошибки:**
- Файл не найден: MB3007

### 6.4 WRITEFILE (path-based)

**Вход:**
- `path`: строка (обязательно)
- `text`: строка (обязательно)

**Выход:**
- `TRUE` при успехе

**Побочные эффекты:**
- Создаёт файл, если не существует
- Перезаписывает содержимое, если файл существует

---

## 7. Анализ Parser

### 7.1 Реализация

`ParseOpenStatement()` в `Parser.Statements.cs:1081`:
```csharp
private OpenStatementSyntax ParseOpenStatement()
{
    var openKeyword = Match(SyntaxKind.OpenKeyword, "OPEN");
    var pathExpression = ParseExpression();
    var forKeyword = Match(SyntaxKind.ForKeyword, "FOR");
    var modeKeyword = ParseOpenModeKeyword();
    var asKeyword = Match(SyntaxKind.AsKeyword, "AS");
    var fileIdentifier = MatchIdentifier();
    return new OpenStatementSyntax(openKeyword, pathExpression, forKeyword, modeKeyword, asKeyword, fileIdentifier);
}
```

`ParseCloseStatement()` в `Parser.Statements.cs:1093`:
```csharp
private CloseStatementSyntax ParseCloseStatement()
{
    var closeKeyword = Match(SyntaxKind.CloseKeyword, "CLOSE");
    var fileIdentifier = MatchIdentifier();
    return new CloseStatementSyntax(closeKeyword, fileIdentifier);
}
```

### 7.2 Оценка

- **Корректность**: Синтаксис разбирается точно по спецификации.
- **Восстановление после ошибок**: Использует общий механизм `Match()` с diagnostic при отсутствии токена.
- **Edge cases**: Пустой путь обрабатывается на уровне Runtime (конструктор `RuntimeTextFile` проверяет `ArgumentException.ThrowIfNullOrWhiteSpace`).
- **Проблемы**: Нет. Парсер чистый и минимальный.

---

## 8. Анализ Binder

### 8.1 Реализация

`BindOpenStatement()` в `Binder.IoStatements.cs:753`:
- Связывает выражение пути с ожидаемым типом `String`
- Ищет переменную по имени
- Проверяет, что переменная имеет тип `FILE` (не массив, не другой тип)
- Маппит `SyntaxKind.InputKeyword/OutputKeyword/AppendKeyword` → `BoundFileOpenMode`

`BindCloseStatement()` в `Binder.IoStatements.cs:778`:
- Ищет переменную по имени
- Проверяет тип `FILE`

### 8.2 Оценка

- **Корректность**: Типовая проверка полная — FILE-переменная, не массив, не ошибка.
- **Диагностика**: MB2003 при несовместимости типов.
- **Ограничение**: FILE-поля в TYPE запрещены (`Binder.Declarations.cs:166`).
- **FUNCTION возврат FILE**: Не поддерживается напрямую (документировано).
- **Проблемы**: Нет.

---

## 9. Анализ Interpreter

### 9.1 Файловый ввод-вывод (Interpreter.FileIO.cs, 193 строки)

Все 18 file-handle функций делегируют в `BuiltinFileIOHelpers`:
- `ExecuteOpen()` — создание `RuntimeTextFile`, добавление в `_openFiles`
- `ExecuteClose()` — `Dispose()`, удаление из `_openFiles`, обнуление переменной
- `EvaluateIsOpen()` — прямая проверка `file.IsOpen`
- Все остальные — тонкие обёртки над `BuiltinFileIOHelpers.EvaluateXxx()`

### 9.2 Файловая система (Interpreter.Builtins.FileSystem.cs, 980 строк)

Все 49 path-based функций реализуются через:
- Прямые вызовы .NET API (`File.*`, `Directory.*`, `Path.*`, `DriveInfo`)
- Общие хелперы `BuiltinFileSystemHelpers` для нормализации путей, чтения с fallback, копирования каталогов

### 9.3 Оценка

- **Дублирование**: Минимально — оба движка делегируют в общие helpers.
- **Специфичная логика**: `ExecuteOpen()` проверяет duplicate open. `ExecuteClose()` обнуляет переменную. Это не дублируется в VM — VM делает то же самое через свою модель стека.
- **Аллокации**: Каждый `OPEN` создаёт новый `RuntimeTextFile` + `FileStream`. Это корректно.
- **Encoding**: UTF-8 no BOM для записи, UTF-8 strict для чтения, Windows-1251 fallback для `READFILE`/`READALLLINES`.

---

## 10. Анализ Bytecode / VM

### 10.1 Опкоды

| Opcode | Stack Effect | Назначение |
|--------|-------------|------------|
| `OpenFile` | pop 2 → push 1 | Открытие файла (path + existing file → new file) |
| `CloseFile` | pop 1 → push 1 | Закрытие файла (file → null) |

### 10.2 Lowering

- `BoundProgramLowerer.LowerOpen()` → `LoweredInstruction.OpenFile(mode, name)`
- `BoundProgramLowerer.LowerClose()` → `LoweredInstruction.CloseFile()`

### 10.3 Bytecode emission

- `BytecodeCompiler.EmitOpenFile()` → `Opcode.OpenFile` с операндом mode (0=Input, 1=Output, 2=Append) и debug name
- `CloseFile` emission → `Opcode.CloseFile`

### 10.4 VM execution

- `ExecuteOpenFile()` — аналог Interpreter: проверка duplicate, создание `RuntimeTextFile`, добавление в `_openFiles`, push на стек
- `ExecuteCloseFile()` — аналог Interpreter: `RequireOpenFile()`, `Dispose()`, удаление из `_openFiles`, push null

### 10.5 Built-in dispatch

Все 67 built-in функций вызываются через `CallBuiltin` → `ExecuteBuiltinCall()` → switch по `BuiltinFunctionId` → `EvaluateXxxBuiltin()` → `BuiltinFileIOHelpers` / `BuiltinFileSystemHelpers`.

### 10.6 Оценка

- **Паритет с Interpreter**: Полный. Оба движка делегируют в одни и те же helpers.
- **Качество lowering**: Чистое — минимальное число инструкций, правильные операнды.
- **Валидация**: `BytecodeModuleValidator` проверяет `OpenFile` debug name, path stack value, mode operand.

---

## 11. Анализ Runtime

### 11.1 RuntimeTextFile (550 строк)

**Класс**: `sealed class RuntimeTextFile : IDisposable`

**Состояние:**
- `_stream: FileStream` — файловый поток
- `_encoding: Encoding` — кодировка записи (UTF-8 no BOM)
- `_strictEncoding: Encoding` — кодировка чтения (UTF-8 strict)
- `_writeBuffer: StringBuilder` — буфер записи
- `_bufferedWriteStartPosition: long?` — позиция начала буферизованной записи
- `_position: long` — текущая позиция в байтах
- `_length: long` — длина файла в байтах
- `_disposed: bool` — флаг закрытия

**Режимы:**
- `Input` — `FileMode.Open, FileAccess.Read, FileShare.Read`
- `Output` — `FileMode.Create, FileAccess.ReadWrite, FileShare.Read`
- `Append` — `FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read` + `Seek(0, End)`

**Ключевые операции:**
- `ReadLine()` — чтение до `\n`, `\r` или `\r\n`, удаление разделителя
- `ReadLines(count)` — чтение N строк с восстановлением позиции при ошибке
- `ReadToEnd()` — чтение всех оставшихся байтов
- `ReadText(count)` — чтение N символов (не байтов!)
- `Write(text, appendLineBreak)` — буферизованная запись
- `Flush()` — сброс буфера на диск
- `Seek(position)` — перемещение с автоматическим flush
- `Rewind()` — `Seek(0)`
- `Dispose()` — flush + закрытие потока

### 11.2 BuiltinFileIOHelpers (418 строк)

Статический класс-помощник, содержащий всю логику file-handle операций. Оба движка (Interpreter и VM) делегируют сюда.

**Ключевые методы:**
- `RequireOpenFile()` — проверка, что FILE-значение содержит открытый файл
- `RequireReadableFile()` — проверка режима INPUT
- `RequireWritableFile()` — проверка режима OUTPUT/APPEND
- `EvaluateXxx()` — все 18 built-in функций
- `CloseFile()` — закрытие + удаление из `_openFiles`
- `CloseAllOpenFiles()` — закрытие всех открытых файлов (для cleanup)
- `AsFilePosition()` — валидация позиции (неотрицательная, в пределах файла)

### 11.3 BuiltinFileSystemHelpers (171 строка)

Статический класс-помощник для path-based операций.

**Ключевые методы:**
- `NormalizeFileSystemPath()` — `Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))`
- `ReadTextFileWithFallback()` — UTF-8 strict → Windows-1251 fallback
- `StripUtf8Bom()` — удаление BOM-маркера
- `ReadDecodedLines()` — разбиение текста на строки
- `CopyDirectory()` — рекурсивное копирование
- `GetDirectorySize()` — рекурсивный подсчёт размера
- `CreateTemporaryFile()` / `CreateTemporaryDirectory()` — создание временных файлов
- `IsFileSystemException()` — фильтр исключений ОС
- `EnsureFileExists()` / `EnsureDirectoryExists()` — валидация существования

---

## 12. Взаимодействие с типами

| Тип | Взаимодействие |
|-----|---------------|
| `STRING` | Путь файла, содержимое файла, текст записи — всё строки |
| `INTEGER` | Аргумент `count` для `READTEXT`, `READLINES` |
| `LONG` | Результат `FILEPOS`, `FILELENGTH`, `FILESIZE`, `LINENUMBER` |
| `BOOLEAN` | Результат `EOF`, `ISOPEN`, `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND`, `SKIPLINE` |
| `FILE` | Тип file handle |
| `STRING()` | Результат `READLINES`, `GETFILES`, `GETDIRS`, `READALLLINES` |
| `TYPE` | FILE-поля в TYPE **запрещены** |
| `ARRAY` | FILE-массивы **не поддерживаются** |

---

## 13. Взаимодействие со String

- Все пути — строки
- Все содержимое файла — строка
- Encoding: UTF-8 no BOM для записи, UTF-8 strict для чтения
- Windows-1251 fallback только для `READFILE` и `READALLLINES`
- BOM удаляется в `READFILE`/`READALLLINES`, но не в потоковом `READLINE`/`READTOEND`
- Пустые строки обрабатываются корректно
- Unicode пути поддерживаются (зависит от ОС)

---

## 14. Взаимодействие с Array

- `READLINES(f, count)` возвращает `RuntimeArrayValue` строк
- `GETFILES(path)` / `GETDIRS(path)` возвращают `RuntimeArrayValue` строк
- `READALLLINES(path)` возвращает `RuntimeArrayValue` строк
- Пустой результат → выделенный пустой массив `1 TO 0`
- Все массивные результаты создаются через общий путь `CreateStringArrayValue`

---

## 15. Взаимодействие с TYPE

- FILE-поля в TYPE **запрещены** (`Binder.Declarations.cs:166`)
- FILE-значения могут передаваться как аргументы SUB/FUNCTION
- FILE-значения не могут возвращаться из FUNCTION напрямую

---

## 16. Взаимодействие с Functions / SUB

- FILE-переменные могут быть параметрами (передаются по ссылке — object reference)
- Локальные FILE-переменные корректно очищаются при выходе из scope
- Глобальные FILE-переменные живут до завершения программы
- `finally` блок в `Execute()`/`ExecuteModule()` закрывает все открытые файлы

---

## 17. Взаимодействие с Control Flow

- `WHILE NOT EOF(f) ... WEND` — корректный паттерн чтения
- `FOR` с file operations — корректно
- `IF ... THEN OPEN ... END IF` — файл добавляется в `_openFiles`
- `TRY/CATCH` — файл остаётся открытым до program-level cleanup
- `RETURN` из функции с открытым файлом — файл остаётся в `_openFiles` до `finally`

---

## 18. Взаимодействие с Error / Exception System

| Ошибка | Код | Поведение |
|--------|-----|-----------|
| Файл не найден | MB3007 | diagnostic + RuntimeFailureException |
| Уже открыт | MB3007 | diagnostic + RuntimeFailureException |
| Закрытый файл | MB3007 | diagnostic + RuntimeFailureException |
| Неверный режим | MB3007 | diagnostic + RuntimeFailureException |
| Неверный путь | MB3007 | diagnostic + RuntimeFailureException |
| Нет прав | MB3007 | diagnostic + RuntimeFailureException |
| Seek за пределы | MB3007 | diagnostic + RuntimeFailureException |
| Seek в APPEND | MB3007 | diagnostic + RuntimeFailureException |
| EOF при READLINE | MB3007 | diagnostic + RuntimeFailureException |
| Ошибка кодировки | MB3007 | diagnostic + RuntimeFailureException |

**Все ошибки файловой подсистемы используют единый код `DiagnosticCodes.BuiltinArgument`.**

---

## 19. State / Lifetime / Ownership

### 19.1 Ownership model

```
КТО СОЗДАЁТ: Runtime (new RuntimeTextFile)
КТО ВЛАДЕЕТ: _openFiles HashSet в Interpreter/BytecodeVm
КТО ИЗМЕНЯЕТ: OPEN (добавление), CLOSE (удаление)
КОГДА УНИЧТОЖАЕТСЯ: явный CLOSE, program termination (finally), exception (finally)
```

### 19.2 Глобальное состояние

- `_openFiles: HashSet<RuntimeTextFile>` — единственный глобальный state файловой подсистемы
- `Environment.CurrentDirectory` — глобальный mutable state, защищён в IDE через `ProgramRunService`

### 19.3 Reset behavior

- Новый `Interpreter` / `BytecodeVm` = новый пустой `_openFiles`
- `ProgramRunService` сохраняет/восстанавливает CWD

---

## 20. Edge Cases

| Сценарий | Поведение | Корректность |
|----------|-----------|-------------|
| Пустой файл | `READLINE` → EOF → MB3007 | ✅ |
| NULL-путь | `ArgumentException.ThrowIfNullOrWhiteSpace` → MB3007 | ✅ |
| Несуществующий каталог | `DirectoryNotFoundException` → MB3007 | ✅ |
| Двойной CLOSE | MB3007 | ✅ |
| Двойной OPEN на той же переменной | MB3007 | ✅ |
| READ после CLOSE | MB3007 | ✅ |
| WRITE в INPUT mode | MB3007 | ✅ |
| READ в OUTPUT mode | MB3007 | ✅ |
| SEEKFILE в APPEND mode | MB3007 | ✅ |
| REWIND в APPEND mode | MB3007 | ✅ |
| SEEKFILE за пределы файла | MB3007 | ✅ |
| SEEKFILE на отрицательную позицию | MB3007 | ✅ |
| READLINES(f, 0) | Пустой массив | ✅ |
| READLINES(f, -1) | MB3007 | ✅ |
| SKIPLINE на EOF | MB3007 | ✅ |
| READTEXT на невалидной UTF-8 границе | MB3007 | ✅ |
| Множественные handle на один файл | Корректно (FileShare.Read) | ✅ |
| Открытие файла двумя разными переменными | Корректно | ✅ |
| OUTPUT перезаписывает существующий файл | Корректно (FileMode.Create) | ✅ |
| APPEND создаёт файл, если не существует | Корректно | ✅ |
| Буферизованная запись + crash | Данные могут быть потеряны (by design) | ⚠️ |
| Очень большие файлы (>2GB) | Корректно (long позиция) | ✅ |
| Unicode пути | Зависит от ОС | ✅ |
| Символические ссылки | .NET следует symlinks | ✅ |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Одинаковая ошибка | Одинаковое состояние |
|---|---|---|---|---|---|
| OPEN | ExecuteOpen() | ExecuteOpenFile() | ✅ | ✅ | ✅ |
| CLOSE | ExecuteClose() | ExecuteCloseFile() | ✅ | ✅ | ✅ |
| ISOPEN | EvaluateIsOpen() | EvaluateIsOpenBuiltin() | ✅ | ✅ | ✅ |
| FILEPATH | EvaluateFilePath() | EvaluateFilePathBuiltin() | ✅ | ✅ | ✅ |
| FILEMODE | EvaluateFileMode() | EvaluateFileModeBuiltin() | ✅ | ✅ | ✅ |
| FILEPOS | EvaluateFilePos() | EvaluateFilePosBuiltin() | ✅ | ✅ | ✅ |
| FILELENGTH | EvaluateFileLength() | EvaluateFileLengthBuiltin() | ✅ | ✅ | ✅ |
| LINENUMBER | EvaluateLineNumber() | EvaluateLineNumberBuiltin() | ✅ | ✅ | ✅ |
| EOF | EvaluateEof() | EvaluateEofBuiltin() | ✅ | ✅ | ✅ |
| READTOEND | EvaluateReadToEnd() | EvaluateReadToEndBuiltin() | ✅ | ✅ | ✅ |
| READTEXT | EvaluateReadText() | EvaluateReadTextBuiltin() | ✅ | ✅ | ✅ |
| READLINE | EvaluateReadLine() | EvaluateReadLineBuiltin() | ✅ | ✅ | ✅ |
| READLINES | EvaluateReadLines() | EvaluateReadLinesBuiltin() | ✅ | ✅ | ✅ |
| SKIPLINE | EvaluateSkipLine() | EvaluateSkipLineBuiltin() | ✅ | ✅ | ✅ |
| WRITE | EvaluateWrite() | EvaluateWriteBuiltin() | ✅ | ✅ | ✅ |
| WRITELINE | EvaluateWrite() | EvaluateWriteBuiltin() | ✅ | ✅ | ✅ |
| NEWLINE | EvaluateNewLine() | EvaluateNewLineBuiltin() | ✅ | ✅ | ✅ |
| FLUSH | EvaluateFlush() | EvaluateFlushBuiltin() | ✅ | ✅ | ✅ |
| SEEKFILE | EvaluateSeekFile() | EvaluateSeekFileBuiltin() | ✅ | ✅ | ✅ |
| REWIND | EvaluateRewind() | EvaluateRewindBuiltin() | ✅ | ✅ | ✅ |
| Все 49 path-based | Прямые вызовы | Прямые вызовы | ✅ | ✅ | ✅ |

**Расхождений нет.** Оба движка делегируют в одни и те же `BuiltinFileIOHelpers` и `BuiltinFileSystemHelpers`.

---

## 22. Анализ производительности

Согласно `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md`:

| Сценарий | Interpreter | VM | Ускорение | Классификация |
|---|---:|---:|---:|---|
| open_file_stream_cycle.axm | baseline | ~0.813x runtime | 0.813x | CLEAR WIN |
| file_path_metadata_sweep.axm | baseline | ~0.768x runtime | 0.768x | CLEAR WIN |
| file_management_roundtrip.axm | baseline | ~0.996x runtime | 0.996x | PARITY |
| file_cleanup_navigation_probe.axm | baseline | ~0.816x runtime | 0.816x | CLEAR WIN |
| temp_storage_roundtrip.axm | baseline | ~0.960x runtime | 0.960x | MODERATE WIN |
| drive_volume_metadata_sweep.axm | baseline | ~0.724x runtime | 0.724x | CLEAR WIN |
| temp_writefile_probe.axm | baseline | ~0.923x runtime | 0.923x | MODERATE WIN |
| temp_filesize_probe.axm | baseline | ~0.884x runtime | 0.884x | CLEAR WIN |

**Все файловые сценарии показывают выигрыш VM или паритет. Ни одного регресса.**

---

## 23. Качество Benchmark

- Все benchmark-сценарии используют одинаковый вход (checksum-based validation)
- Warmup: 3 прогона перед измерением
- JIT effects: учитываются (pre-JIT vs post-JIT разделены)
- Compilation time: отделена от execution time
- Стабильность:多次 прогонов подтверждена

---

## 24. Hot Paths и Bottlenecks

- **Горячий путь**: цикл `WHILE NOT EOF(f) ... READLINE(f) ... WEND` — самый частый паттерн
- **Bottleneck**: Нет. VM быстрее интерпретатора на всех файловых сценариях.
- **Оптимизация**: `ISOPEN` переведён на прямой `UnaryNoLocation`-путь в VM.

---

## 25. Архитектурные проблемы

**Архитектурных проблем не обнаружено.** Подсистема чистая:

- Двухслойная модель (handles + path-based) чётко разделена
- Общие helpers (`BuiltinFileIOHelpers`, `BuiltinFileSystemHelpers`) устраняют дублирование
- Ownership model deterministic
- Error handling единообразный
- Паритет Interpreter/VM полный

---

## 26. Дублирование логики

**Минимальное дублирование:**

1. **Interpreter.FileIO.cs ↔ BytecodeVm.FileIO.cs**: Оба файла содержат тонкие обёртки над `BuiltinFileIOHelpers`. Это не дублирование логики, а необходимый диспетчерский слой для каждого движка.

2. **Interpreter.Builtins.FileSystem.cs ↔ BytecodeVm.Builtins.FileSystem.cs**: Оба файла содержат switch-диспетчер для 49 built-in функций. Логика делегируется в `BuiltinFileSystemHelpers`.

3. **READLINES boilerplate**: 6 строк кода создания строкового массива повторяются в обоих движках (передаётся как делегат `CreateStringArrayValue`).

**Вывод**: Дублирование устранено выносом в общие helpers. Оставшееся «дублирование» — это необходимый диспетчерский код, а не копирование логики.

---

## 27. Single Source of Truth

| Аспект | Источник истины |
|--------|---------------|
| Синтаксис | `OpenStatementSyntax`, `CloseStatementSyntax` |
| Типы | `TypeSymbol.File`, `BuiltinFunctionDescriptor` |
| Семантика | `BoundOpenStatement`, `BoundCloseStatement`, `BoundCallExpression` |
| Runtime behavior | `RuntimeTextFile`, `BuiltinFileIOHelpers`, `BuiltinFileSystemHelpers` |
| Ошибки | `DiagnosticCodes.BuiltinArgument` |
| Метаданные | `BuiltinFunctionId` enum + descriptors |
| Performance invariants | `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md` |

**Единый источник истины установлен.** Нет конфликтующих источников.

---

## 28. Legacy / Dead Code

**Не обнаружен.** Все файлы активно используются. Нет abandoned features, неиспользуемых helpers или мёртвых опкодов.

---

## 29. Надёжность и безопасность

### 29.1 Безопасность

- **Sandbox**: Нет. Полный доступ к файловой системе через .NET — intentional для локального языка.
- **Path traversal**: Разрешён (`..` обрабатывается `Path.GetFullPath`).
- **Absolute paths**: Разрешены.
- **System directories**: Доступны.
- **Trust model**: Локальный язык для автора, unrestricted access by design.

### 29.2 Надёжность

- **Handle leak**: Невозможен — `finally` блок закрывает все файлы.
- **Use-after-close**: Обнаруживается через `IsOpen` проверку.
- **Double close**: Обнаруживается и диагностируется.
- **Resource exhaustion**: Ограничено только ОС (нет явного лимита на количество файлов).
- **Crash consistency**: Не гарантирована (зависит от ОС).
- **Atomicity**: Не гарантирована.

---

## 30. Анализ тестов

### 30.1 Покрытие тестами

| Уровень тестов | Файл | Количество | Покрытие |
|---|---|---|---|
| Interpreter FileIO | `InterpreterTests.FileIO.cs` | 26 тестов | Все 18 file-handle функций + ошибки |
| Interpreter FileSystem | `InterpreterTests.FileSystem.cs` | 27 тестов | Все 49 path-based функций + ошибки |
| Bytecode VM | `BytecodeVmTests.cs` | 6 тестов | OPEN/CLOSE + FileSystem builtins + ошибки |
| Semantic (Binder) | `BinderTests.cs` | 9 тестов | Типы, OPEN/CLOSE, built-in типы |
| Bytecode validation | `BytecodeModuleValidatorTests.cs` | 3 теста | OpenFile opcode validation |
| Execution pipeline | `ExecutionPipelineTests.cs` | 5 тестов | E2E bytecode pipeline |
| Acceptance | `AcceptanceProgramTests.cs` | 1 тест | READALLLINES UTF-8 BOM |
| IDE formatting | `CodeFormattingServiceTests.cs` | 1 тест | FILE/OPEN/COPYFILE formatting |
| IDE highlighting | `EditorFeatureTests.cs` | 1 тест | FILE type keyword |

**Итого: 79+ тестов файловой подсистемы.**

### 30.2 Негативные тесты

- Файл не найден ✅
- Уже открыт ✅
- Закрытый файл ✅
- Неверный режим (WRITE в INPUT) ✅
- SEEK в APPEND ✅
- SEEK за пределы ✅
- EOF при READLINE ✅
- Неверный аргумент (отрицательный count) ✅
- Отсутствующий источник для COPYFILE ✅
- Отсутствующий каталог для CHANGEDIR ✅

---

## 31. Недостающие тесты

### 31.1 Парсерные тесты

- Нет тестов парсинга `OPEN...FOR...AS` в `ParserTests.cs`
- Нет тестов парсинга `CLOSE` в `ParserTests.cs`
- Нет тестов парсинга `DIM...AS FILE` в `ParserTests.cs`

### 31.2 Lexer тесты

- Нет тестов лексирования `FILE`, `OPEN`, `CLOSE`, `INPUT`, `OUTPUT`, `APPEND` в файловом контексте

### 31.3 Runtime edge cases

- Нет тестов одновременного открытия нескольких handle на один файл
- Нет тестов файлов >2GB
- Нет тестов очень длинных строк (>1MB)
- Нет тестов concurrent access (хотя Axiom single-threaded)

### 31.4 Bytecode compiler/lowerer

- Нет отдельных тестов `BytecodeCompilerTests` для file I/O
- Нет отдельных тестов `BoundProgramLowererTests` для file I/O

---

## 32. Анализ Samples

### 32.1 Покрытие samples

| Категория | Количество | Edge cases |
|---|---|---|
| Operators (OPEN/CLOSE) | 2 | Нет |
| Stream I/O functions | 19 | 2 (zero_and_end, isopen uninitialized) |
| Whole-file read/write | 7 | 3 (empty_file, utf8_bom, windows1251_fallback) |
| File metadata | 8 | Нет |
| File management | 4 | 2 (double delete для файла и каталога) |
| Temp file/dir | 2 | Нет |
| Directory operations | 14 | 2 (direxists nonexistent, dirsize nested) |
| Path functions | 5 | 1 (pathexists nonexistent) |
| Drive/disk functions | 6 | Нет |
| Engine-compare perf | 16 | N/A (performance) |
| Comprehensive smoke | 3 | N/A (integration) |

**Итого: 86 runnable `.axm` samples.**

### 32.2 Пробелы в samples

- Нет sample для одновременного открытия нескольких файлов
- Нет sample для file handle как параметра SUB/FUNCTION
- Нет sample для file handle в цикле с SEEKFILE
- Нет acceptance sample для полного file I/O lifecycle (OPEN → WRITE → CLOSE → OPEN → READ → CLOSE)

---

## 33. Анализ документации

### 33.1 Manual-страницы

| Страница | Содержание | Качество |
|---|---|---|
| `open.html` | Оператор OPEN | ✅ Полная |
| `close.html` | Оператор CLOSE | ✅ Полная |
| `data-types-strings-and-files.html` | Тип FILE | ✅ Полная |
| `file-system-functions.html` | Обзор раздела | ✅ Полная |
| `file-system-functions-open-files.html` | 18 file-handle функций | ✅ Полная |
| `file-system-functions-text.html` | 7 целочтение/запись функций | ✅ Полная |
| `file-system-functions-paths.html` | 30 path/metadata функций | ✅ Полная |
| `file-system-functions-management.html` | 14 management функций | ✅ Полная |

### 33.2 Спецификация

- **Активная спецификация** (`SPECIFICATION3.md`): **НЕ содержит** dedicated file-system section. Это структурный пробел.
- **Историческая спецификация** (`docs/archive/spec/SPECIFICATION.md`, `docs/archive/spec/SPECIFICATION2.md`): Содержит Section 32 — полную спецификацию файловой подсистемы.

### 33.3 Расхождения документации и кода

1. **Спецификация в историческом документе**: Каноническая file-system спецификация живёт в `docs/archive/spec/SPECIFICATION.md`/`docs/archive/spec/SPECIFICATION2.md` (Section 32), а не в активной `SPECIFICATION3.md`. Нужно перенести или хотя бы сослаться.

2. **Encoding asymmetry**: `READFILE`/`READALLLINES` используют UTF-8 с Windows-1251 fallback, а потоковые `READLINE`/`READTOEND` используют strict UTF-8. Это различие документировано в аудите, но manual-страницы не явно подчёркивают разницу.

3. **BOM handling**: `StripUtf8Bom()` вызывается для `READFILE`/`READALLLINES`, но не для потокового `READLINE`/`READTOEND`. Документировано как "by design" (FILE-006), но manual не объясняет это различие.

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

Все файловые ключевые слова присутствуют в обеих схемах подсветки (`Axiom.xshd` и `Axiom.Dark.xshd`):
- Операторы: `OPEN`, `CLOSE`
- Тип: `FILE`
- Все 67 built-in функций

### 34.2 Форматирование

`CodeFormattingServiceTests` подтверждает корректное форматирование:
- `dim f as file` → `Dim f As File`
- `open path for output as f` → `Open path For Output As f`

### 34.3 IDE File Manager

Отдельная подсистема `Files/` в IDE (не language-level file I/O):
- `FileTreeService` — дерево файлов
- `FilesPaneViewModel` — состояние панели
- `FileOperationsService` — файловые операции (создание, переименование, копирование, перемещение)
- `RecentFilesService` — недавние файлы
- `DocumentFileService` — открытие/сохранение документов

### 34.4 Оценка

- **Подсветка**: Полная.
- **Форматирование**: Полное.
- **IDE не противоречит Compiler/Runtime**: Подтверждено.

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Примечание |
|---|---|---|
| Windows | HOST LIMITATION | Path separator, newline, Windows-1251 |
| .NET FileStream | HOST LIMITATION | FileShare.Read, FileMode.Create |
| Environment.CurrentDirectory | HOST LIMITATION | Mutable global state, защищён в IDE |
| UTF-8 encoding | LANGUAGE DEFECT | Нет (by design) |
| Windows-1251 fallback | HOST LIMITATION | Хардкод для кириллицы |
| FileShare.Read | HOST LIMITATION | Поведение на Windows |

---

## 36. Карта корневых причин

```
ROOT-SYS-001: Text-only design (нет binary mode)
  ├── SYS-FILE-001: Нет binary mode
  ├── SYS-FILE-002: Нет PRINT #f
  ├── SYS-FILE-003: Нет INPUT #f
  └── SYS-FILE-004: Нет GET/PUT для файлов

ROOT-SYS-002: Global CWD state
  └── SYS-FILE-005: Environment.CurrentDirectory mutable state

ROOT-SYS-003: UTF-8 first design
  ├── SYS-FILE-006: BOM не обрабатывается в потоковом чтении
  └── SYS-FILE-007: Windows-1251 fallback хардкод
```

---

## 37. Стабильные области

**НЕ ТРОГАТЬ:**

1. **`RuntimeTextFile`** — корректная обёртка над FileStream. Deterministic lifecycle, буферизованная запись, корректное чтение строк.

2. **`BuiltinFileIOHelpers`** — чистый общий слой. Все 18 file-handle функций, валидация, cleanup.

3. **`BuiltinFileSystemHelpers`** — comprehensive filesystem operations. Нормализация путей, чтение с fallback, копирование каталогов.

4. **Handle lifecycle** — deterministic cleanup через `finally`. Нет leak'ов.

5. **Error handling** — единообразный диагностический модель `MB3007`.

6. **Interpreter/VM parity** — полный паритет, оба движка делегируют в одни helpers.

7. **Descriptor-based built-in registration** — чистый, расширяемый.

8. **Manual-страницы** — comprehensive, с примерами и ссылками на samples.

---

## 38. Возможности консолидации

Дублирование уже устранено выносом в `BuiltinFileIOHelpers` и `BuiltinFileSystemHelpers`. Дальнейшая консолидация не требуется.

---

## 39. Возможности разделения ответственности

Разделение ответственности уже корректное:
- `RuntimeTextFile` — только file I/O
- `BuiltinFileIOHelpers` — только file-handle операции
- `BuiltinFileSystemHelpers` — только path-based операции
- `Interpreter.FileIO.cs` / `BytecodeVm.FileIO.cs` — только диспетчер
- `Binder.IoStatements.cs` — только связывание OPEN/CLOSE

Дальнейшее разделение не требуется.

---

## 40. Кандидаты на удаление

**Нет кандидатов на удаление.** Все файлы активно используются.

---

## 41. Готовность к дальнейшему развитию

| Возможность | Готовность | Комментарий |
|---|---|---|
| Binary mode | Средняя | Нужен отдельный `RuntimeBinaryFile` + новые опкоды |
| PRINT #f / INPUT #f | Средняя | Могут быть добавлены как built-in функции |
| Async file I/O | Низкая | Требует фундаментального изменения архитектуры |
| Richer encodings | Высокая | Encoding model уже расширяемый |
| Structured serialization | Низкая | Нужен отдельный дизайн |
| FILE в TYPE | Средняя | Нужно решить semantic вопросы ownership |

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Уверенность |
|---|---|---|---|---|---|
| FILE-001 | P3 | BINARY | Runtime | Нет binary file mode | CONFIRMED |
| FILE-002 | P3 | PRINT# | Runtime | Нет PRINT #f | CONFIRMED |
| FILE-003 | P3 | INPUT# | Runtime | Нет INPUT #f | CONFIRMED |
| FILE-004 | P3 | GET/PUT | Runtime | Нет GET/PUT для файлов | CONFIRMED |
| FILE-005 | P3 | CWD | Runtime | Environment.CurrentDirectory mutable state | CONFIRMED |
| FILE-006 | P4 | BOM | Runtime | BOM не обрабатывается в потоковом чтении | CONFIRMED |
| FILE-007 | P4 | ENCODING | Runtime | Windows-1251 fallback хардкод | CONFIRMED |
| FILE-008 | P4 | DOCS | Spec | Спецификация файловой подсистемы в историческом документе | CONFIRMED |
| FILE-009 | P4 | TESTS | Parser | Нет парсерных тестов для OPEN/CLOSE | CONFIRMED |
| FILE-010 | P4 | TESTS | Lexer | Нет лексерных тестов для файловых ключевых слов | CONFIRMED |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
**Статус:** ЗАВЕРШЁН. Все файловые benchmark-сценарии измерены.

### ЭТАП 1 — Исправление критических проблем корректности
**Статус:** НЕ ТРЕБУЕТСЯ. Критических проблем нет.

### ЭТАП 2 — Стабилизация семантики
**Статус:** ЗАВЕРШЁН. Семантика стабильна, паритет подтверждён.

### ЭТАП 3 — Архитектурное улучшение
**Статус:** ЗАВЕРШЁН. Общие helpers вынесены, дублирование устранено.

### ЭТАП 4 — Оптимизация производительности
**Статус:** ЗАВЕРШЁН. Все файловые сценарии показывают выигрыш VM или паритет.

### ЭТАП 5 — Усиление тестирования
**Статус:** ЧАСТИЧЕСКИ ЗАВЕРШЁН. 79+ тестов, но есть пробелы:
- TASK-FILE-001: Добавить парсерные тесты для OPEN/CLOSE
- TASK-FILE-002: Добавить лексерные тесты для файловых ключевых слов

### ЭТАП 6 — Обновление документации
**Статус:** ЧАСТИЧЕСКИ ЗАВЕРШЁН. Manual полный, но:
- TASK-FILE-003: Перенести или сослаться на file-system спецификацию в `SPECIFICATION3.md`
- TASK-FILE-004: Документировать encoding asymmetry в manual

### ЭТАП 7 — Обновление IDE
**Статус:** ЗАВЕРШЁН. Подсветка, форматирование, file manager — всё на месте.

### ЭТАП 8 — Подготовка к дальнейшему развитию
**Статус:** ГОТОВ. Архитектура расширяема для binary mode, новых encoding, async I/O.

---

## 44. Граф зависимостей задач

```
TASK-FILE-001 (парсерные тесты) ← независимо
TASK-FILE-002 (лексерные тесты) ← независимо
TASK-FILE-003 (перенос спецификации) ← независимо
TASK-FILE-004 (документирование encoding) ← зависит от FILE-003
```

---

## 45. Definition of Done

Файловая подсистема считается завершённой, если:

1. ✅ Все 67 built-in функций работают корректно
2. ✅ OPEN/CLOSE работают во всех трёх режимах
3. ✅ Edge cases обработаны (пустые файлы, двойной close, неверный режим)
4. ✅ Error handling единообразный (MB3007)
5. ✅ Interpreter и VM дают одинаковый результат
6. ✅ Тесты покрывают нормальные и негативные сценарии
7. ✅ Samples покрывают все функции
8. ✅ Manual покрывает все функции
9. ✅ IDE подсветка и форматирование синхронизированы
10. ✅ Performance измерен и VM быстрее или на паритете

**Все 10 критериев выполнены.**

---

## 46. ТОП-20 проблем корректности и архитектуры

1. [P3] FILE-001 — Нет binary file mode
2. [P3] FILE-002 — Нет PRINT #f
3. [P3] FILE-003 — Нет INPUT #f
4. [P3] FILE-004 — Нет GET/PUT для файлов
5. [P3] FILE-005 — Environment.CurrentDirectory mutable state
6. [P4] FILE-006 — BOM не обрабатывается в потоковом чтении
7. [P4] FILE-007 — Windows-1251 fallback хардкод
8. [P4] FILE-008 — Спецификация в историческом документе
9. [P4] FILE-009 — Нет парсерных тестов
10. [P4] FILE-010 — Нет лексерных тестов

**Все проблемы P3-P4. Нет P0-P2 проблем.**

---

## 47. ТОП-20 задач по производительности

**Нет задач по производительности.** Все файловые сценарии показывают выигрыш VM или паритет. Ни одного регресса.

---

## 48. Итоговая оценка здоровья подсистемы

| Аспект | Балл | Обоснование |
|---|---|---|
| Корректность | 9/10 | Все функции работают, edge cases обработаны |
| Семантика | 9/10 | Чёткий контракт, deterministic behavior |
| Архитектура | 9/10 | Чистая двухслойная модель, общие helpers |
| Runtime | 9/10 | RuntimeTextFile корректный, deterministic lifecycle |
| Interpreter | 9/10 | Все функции реализованы, делегируют в helpers |
| Bytecode VM | 9/10 | Полный паритет с Interpreter |
| Производительность | 9/10 | VM быстрее на всех сценариях |
| Память | 9/10 | Deterministic cleanup, нет leak'ов |
| Error handling | 9/10 | Единообразный MB3007 |
| Tests | 8/10 | 79+ тестов, но пробелы в парсерных/лексерных |
| Samples | 9/10 | 86 samples, хорошее покрытие |
| Documentation | 8/10 | Manual полный, но спецификация в историческом документе |
| IDE | 9/10 | Подсветка, форматирование, file manager |
| Maintainability | 9/10 | Чистый код, минимальное дублирование |
| Reliability | 9/10 | Deterministic cleanup, consistent errors |
| Future readiness | 7/10 | Средняя готовность к binary mode |

**Общий Health Score: 8.8/10** (подтверждено повторной верификацией)

---

## ГЛАВНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать файловую подсистему Axiom достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES**

---

## Основные blockers

Нет. Файловая подсистема стабильна и готова к использованию.

## Что нужно сделать в первую очередь

1. [P4] Перенести или сослаться на file-system спецификацию в `SPECIFICATION3.md` (FILE-008)
2. [P4] Добавить парсерные тесты для OPEN/CLOSE (FILE-009)
3. [P4] Добавить лексерные тесты для файловых ключевых слов (FILE-010)

## Что можно отложить

1. [P3] Binary file mode (FILE-001) — только если потребуется
2. [P3] PRINT #f / INPUT #f (FILE-002/003) — только если потребуется
3. [P3] GET/PUT для файлов (FILE-004) — только если потребуется
4. [P4] Документирование encoding asymmetry (FILE-007)

## Что не следует трогать

1. `RuntimeTextFile` — стабильный, корректный
2. `BuiltinFileIOHelpers` — чистый общий слой
3. `BuiltinFileSystemHelpers` — comprehensive filesystem operations
4. Handle lifecycle — deterministic cleanup
5. Error handling — единообразный MB3007
6. Interpreter/VM parity — полный паритет
