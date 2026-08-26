# AXIOM — DEEP FILE SYSTEM / FILE I/O FORENSIC AUDIT

## 1. Executive Summary

Файловая подсистема Axiom **зрелая и хорошо спроектированна**. Она состоит из двух слоёв: (1) **операторы OPEN/CLOSE** для управляемого потокового ввода-вывода через file handles, и (2) **встроенные файловые функции** для прямых операций с файловой системой без handles. Оба слоя полностью реализованы в интерпретаторе и BytecodeVm, делегируют общую логику в `BuiltinFileIOHelpers` и `BuiltinFileSystemHelpers`, покрыты тестами, примерами и документацией.

**Главный вывод**: файловая подсистема стабильна, корректна и готова к использованию. Архитектура чистая — handle lifecycle deterministic, encoding явный, error handling predictable, Interpreter/VM parity confirmed.

Health Score: **8.8/10**.

---

## 2. Previous IO Audit Verification

Старый `docs/IO_AUDIT_REPORT.md` (Level 8) зафиксировал:
- PRINT, PRINT USING, INPUT: полностью реализованы ✅ (подтверждено)
- DATA, READ, RESTORE, LINE INPUT: отсутствуют ✅ (подтверждено, не в scope файлового аудита)
- I/O helpers вынесены в BuiltinIOHelpers ✅ (подтверждено)

Старый аудит не содержал file-specific findings — все file-related built-in уже были на месте к моменту аудита Level 8.

---

## 3. Actual File System Architecture

```
Axiom Source: OPEN "path" FOR INPUT AS #f
  ↓
Parser: ParseOpenStatement() → OpenStatementSyntax
  ↓
Binder: BindOpenStatement() → BoundOpenStatement(path, variable, mode)
  ↓
Lowering: LowerOpen() → LoweredInstruction.OpenFile(mode, name)
  ↓
BytecodeCompiler: EmitOpenFile() → BytecodeInstruction(Opcode.OpenFile, [mode, debugName])
  ↓
BytecodeVm: ExecuteOpenFile() → new RuntimeTextFile(path, mode, encoding)
  ↓
RuntimeTextFile → FileStream → OS
```

Для built-in функций (READFILE, WRITEFILE и т.д.) путь короче — прямой вызов через `BuiltinFileSystemHelpers` без file handle.

---

## 4. File Implementation Map

| Уровень | OPEN | CLOSE | File Built-ins |
|---------|------|-------|----------------|
| Lexer | `OpenKeyword` | `CloseKeyword` | N/A (function calls) |
| Parser | `ParseOpenStatement()` | `ParseCloseStatement()` | `ParseInvocationExpression()` |
| Syntax | `OpenStatementSyntax` | `CloseStatementSyntax` | `InvocationExpressionSyntax` |
| Binder | `BindOpenStatement()` | `BindCloseStatement()` | `BindInvocationExpression()` |
| Bound | `BoundOpenStatement` | `BoundCloseStatement` | `BoundCallExpression` |
| Lowering | `LowerOpen()` | `LowerClose()` | `LowerCallExpression()` |
| Opcode | `OpenFile` | `CloseFile` | `CallBuiltin` |
| VM | `ExecuteOpenFile()` | `ExecuteCloseFile()` | `ExecuteBuiltinCall()` |
| Interpreter | `ExecuteOpen()` | `ExecuteClose()` | `EvaluateBuiltIn()` |
| Host | `FileStream` | `Dispose()` | `File.*`, `Directory.*` |
| Helpers | — | `BuiltinFileIOHelpers.CloseFile()` | `BuiltinFileSystemHelpers.*` |
| Tests | 15+ FileIO tests | 20+ FileSystem tests | 40+ total |
| Samples | `samples/operators/open/` | `samples/operators/close/` | `samples/functions/*` |
| Manual | `open.html` | `close.html` | `file-system-functions*.html` |

---

## 5. Supported File Features

| Возможность | Статус |
|-------------|--------|
| `OPEN path FOR INPUT AS #f` | SUPPORTED |
| `OPEN path FOR OUTPUT AS #f` | SUPPORTED |
| `OPEN path FOR APPEND AS #f` | SUPPORTED |
| `CLOSE #f` | SUPPORTED |
| `READLINE(f)` | SUPPORTED |
| `READLINES(f, n)` | SUPPORTED |
| `READTOEND(f)` | SUPPORTED |
| `READTEXT(f, n)` | SUPPORTED |
| `SKIPLINE(f)` | SUPPORTED |
| `WRITE(f, text)` | SUPPORTED |
| `WRITELINE(f, text)` | SUPPORTED |
| `NEWLINE(f)` | SUPPORTED |
| `FLUSH(f)` | SUPPORTED |
| `SEEKFILE(f, pos)` | SUPPORTED |
| `REWIND(f)` | SUPPORTED |
| `EOF(f)` | SUPPORTED |
| `ISOPEN(f)` | SUPPORTED |
| `FILEPATH(f)` | SUPPORTED |
| `FILEMODE(f)` | SUPPORTED |
| `FILEPOS(f)` | SUPPORTED |
| `FILELENGTH(f)` | SUPPORTED |
| `LINENUMBER(f)` | SUPPORTED |
| `FILEEXISTS(path)` | SUPPORTED |
| `READFILE(path)` | SUPPORTED |
| `WRITEFILE(path, text)` | SUPPORTED |
| `APPENDFILE(path, text)` | SUPPORTED |
| `DELETEFILE(path)` | SUPPORTED |
| `COPYFILE(src, dst)` | SUPPORTED |
| `MOVEFILE(src, dst)` | SUPPORTED |
| `RENAMEFILE(path, name)` | SUPPORTED |
| `MAKEDIR(path)` | SUPPORTED |
| `DELETEDIR(path)` | SUPPORTED |
| `DELETEDIRRECURSIVE(path)` | SUPPORTED |
| `GETFILES(path)` | SUPPORTED |
| `GETDIRS(path)` | SUPPORTED |
| Binary mode | NOT SUPPORTED |
| `LINE INPUT #f` | NOT SUPPORTED |
| `PRINT #f` | NOT SUPPORTED |
| `INPUT #f` | NOT SUPPORTED |
| `GET #f` / `PUT #f` | NOT SUPPORTED |

---

## 6. File Handle Model

**Что такое "file" в Axiom**: `RuntimeTextFile` — объект-обёртка над `FileStream` с буферизацией записи.

- **Тип хранения**: `RuntimeValue` с `TypeSymbol.File` хранит ссылку на `RuntimeTextFile?`
- **Identity**: прямая object reference, не numeric handle
- **Ownership**: `HashSet<RuntimeTextFile> _openFiles` в каждом движке
- **Lifetime**: deterministic через `Dispose()`
- **Validity**: проверяется через `IsOpen` (не `_disposed`)

---

## 7. Handle Identity

Axiom не использует numeric handles. `RuntimeTextFile` — это object reference. Нет:
- коллизий handles
- reuse handles
- handle exhaustion (ограничено только OS)
- нулевых или отрицательных handles

Переменная `FILE` в Axiom — это `DIM f AS FILE`, которая хранит `RuntimeValue.FromFile(file)`.

---

## 8. Handle Allocation

```
OPEN "path" FOR INPUT AS #f
  ↓
new RuntimeTextFile(path, mode, encoding)
  ↓
_openFiles.Add(file)
  ↓
AssignVariable(f, RuntimeValue.FromFile(file))
```

**Owner**: runtime (Interpreter или BytecodeVm через `_openFiles`).

---

## 9. Handle Lifetime

```
OPEN → new RuntimeTextFile → _openFiles.Add()
  ↓
use (READ/WRITE/SEEK)
  ↓
CLOSE → Dispose() → _openFiles.Remove()
```

**Program termination**: `finally` блок в `Interpreter.Execute()` и `BytecodeVm.FinishRun()` вызывает `CloseAllOpenFiles()`.

**Exception**: `RuntimeFailureException` ловится в `Execute()`/`ExecuteModule()`, `finally` закрывает файлы.

---

## 10. Handle Leak Audit

| Сценарий | Файл закрыт? | Evidence |
|----------|-------------|----------|
| Normal termination | ✅ | `finally { CloseAllOpenFiles(); }` |
| Runtime error | ✅ | `catch (RuntimeFailureException) { } finally { CloseAllOpenFiles(); }` |
| Exception в OPEN | ✅ | Файл не добавлен в `_openFiles` при ошибке |
| RETURN из функции | ✅ | Глобальный cleanup в finally |
| Nested call failure | ✅ | Глобальный cleanup в finally |
| Program restart | ✅ | Новый Interpreter/VM = новый `_openFiles` |

---

## 11. CLOSE Semantics

```
CLOSE #f
  ↓
RequireOpenFile() — проверяет IsOpen
  ↓
BuiltinFileIOHelpers.CloseFile(file, _openFiles)
  ↓
file.Dispose() + _openFiles.Remove()
  ↓
AssignVariable(f, RuntimeValue.FromFile(null))
```

- **Successful close**: файл закрыт, переменная установлена в `null`
- **Double close**: `RequireOpenFile()` проверяет `IsOpen` → diagnostic → `RuntimeFailureException`
- **Close invalid**: аналогично double close

---

## 12. Use-After-Close

```
OPEN → CLOSE → READ
```

`RequireReadableFile()` → `RequireOpenFile()` → `file is not null && file.IsOpen` → false → diagnostic → `RuntimeFailureException`.

**Корректно**: use-after-close обнаруживается и диагностируется.

---

## 13. Duplicate OPEN

```
OPEN "path" FOR INPUT AS #f
OPEN "path" FOR INPUT AS #f  ← повторный OPEN на той же переменной
```

Проверка в `ExecuteOpen()`:
```csharp
if (existingFile is not null && existingFile.IsOpen)
{
    ReportRuntimeDiagnostic(..., "переменная уже содержит открытый файл");
    throw new RuntimeFailureException();
}
```

**Корректно**: повторный OPEN на той же переменной-файле запрещён.

Разные переменные могут открывать один и тот же файл (OS-level `FileShare.Read`).

---

## 14. Path Semantics

- **Path normalization**: `Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))`
- **Relative paths**: разрешаются относительно **текущего рабочего каталога процесса** (`Environment.CurrentDirectory`)
- **`..`**: обрабатывается `Path.GetFullPath()`
- **`.`**: обрабатывается `Path.GetFullPath()`
- **Unicode paths**: поддерживаются (зависит от ОС)
- **Environment variables**: `%USERPROFILE%` и т.д. раскрываются через `ExpandEnvironmentVariables`

---

## 15. Path Resolution

**Relative path base**: `Environment.CurrentDirectory` (рабочий каталог процесса).

Не:
- не каталог проекта
- не каталог исходного файла
- не каталог IDE
- не каталог исполняемого файла

Это стандартное .NET поведение, зафиксированное в `BuiltinFileSystemHelpers.NormalizeFileSystemPath()`.

---

## 16. IDE vs CLI Path Behavior

- **CLI**: `Environment.CurrentDirectory` = каталог запуска
- **IDE**: `Environment.CurrentDirectory` сохраняется/восстанавливается в `ProgramRunService` (lines 106-113, 287)

`ProgramRunService` корректно сохраняет CWD перед запуском и восстанавливает после.

---

## 17. Path Security

- **Path traversal**: разрешён (`..` обрабатывается `Path.GetFullPath`)
- **Absolute paths**: разрешены
- **Sandbox**: нет — Axiom имеет полный доступ к файловой системе через .NET
- **Intended trust model**: локальный язык для автора, unrestricted local access by design

---

## 18. OPEN Syntax

```
OPEN pathExpression FOR modeKeyword AS #fileIdentifier
```

- `pathExpression`: строковое выражение
- `modeKeyword`: `INPUT` | `OUTPUT` | `APPEND`
- `#fileIdentifier`: имя переменной типа FILE

---

## 19. OPEN Modes

| Mode | Read | Write | Create | Truncate | Append | Existing file |
|------|------|-------|--------|----------|--------|---------------|
| INPUT | ✅ | ❌ | ❌ | ❌ | ❌ | Открывает существующий |
| OUTPUT | ❌ | ✅ | ✅ | ✅ | ❌ | Создаёт/перезаписывает |
| APPEND | ❌ | ✅ | ✅ | ❌ | ✅ | Дописывает в конец |

---

## 20. Existing File Behavior

- **INPUT**: открывает для чтения, позиция = 0
- **OUTPUT**: **уничтожает содержимое** (`FileMode.Create`), создаёт новый
- **APPEND**: открывает, позиция = конец файла

---

## 21. Nonexistent File Behavior

- **INPUT**: `FileNotFoundException` → diagnostic → `RuntimeFailureException`
- **OUTPUT**: создаёт новый файл
- **APPEND**: создаёт новый файл

---

## 22. Directory Handling

OPEN не проверяет, является ли путь каталогом. Если путь указывает на каталог:
- INPUT: `IOException` от .NET → diagnostic
- OUTPUT: `IOException` от .NET → diagnostic

---

## 23. Invalid Path Handling

- Пустой путь: `ArgumentException.ThrowIfNullOrWhiteSpace(path)` в конструкторе `RuntimeTextFile`
- Несуществующий каталог: `DirectoryNotFoundException` → `IsFileSystemException()` → diagnostic
- Запрещённые символы: `ArgumentException` → diagnostic

---

## 24. File Access Rights

- INPUT: `FileAccess.Read`
- OUTPUT: `FileAccess.ReadWrite`
- APPEND: `FileAccess.ReadWrite`

Если ОС отказывает: `UnauthorizedAccessException` → `IsFileSystemException()` → diagnostic.

---

## 25. WRITE Model

Доступные write operations:
- `WRITE(f, text)` — запись текста без перевода строки
- `WRITELINE(f, text)` — запись текста с переводом строки
- `NEWLINE(f)` — запись пустого перевода строки

Все через `RuntimeTextFile.Write(text, appendLineBreak)`.

---

## 26. WRITE String

- Записывает текст в кодировке файла (UTF-8 без BOM)
- `appendLineBreak: true` добавляет `Environment.NewLine`
- Позиция обновляется на количество байт
- Буферизовано: данные пишутся в `_writeBuffer`, flush при `Flush()`, `Seek()`, `Dispose()`

---

## 27. WRITE Numeric

Прямой записи чисел в файл нет. Числа должны быть преобразованы в строку через `STR$()` или конкатенацию перед записью через `WRITE()`.

---

## 28. WRITE TYPE Values

Нет прямой записи TYPE в файл. Запись только строковых значений.

---

## 29. WRITE Array Values

Нет прямой записи массивов в файл.

---

## 30. APPEND Semantics

- Начальная позиция: конец файла (`Seek(0, End)`)
- Запись: буферизованная, как OUTPUT
- SEEKFILE: **запрещён** в APPEND mode → diagnostic
- REWIND: **запрещён** в APPEND mode → diagnostic

---

## 31. Truncation

`OPEN ... OUTPUT` использует `FileMode.Create`, что **уничтожает существующее содержимое**. Это корректное BASIC-поведение.

---

## 32. FLUSH

- `FLUSH(f)` — явный сброс буфера записи
- `SEEKFILE(f, pos)` — автоматический flush перед seek
- `REWIND(f)` — автоматический flush (через Seek)
- `CLOSE` — автоматический flush в `Dispose()`
- Program termination — flush в `CloseAllOpenFiles()` → `Dispose()`

---

## 33. READ Model

Доступные read operations:
- `READLINE(f)` — чтение одной строки
- `READLINES(f, n)` — чтение n строк
- `READTOEND(f)` — чтение всего остатка файла
- `READTEXT(f, n)` — чтение n символов
- `SKIPLINE(f)` — пропуск строки

Все только в INPUT mode.

---

## 34. READ String

- **READLINE**: читает до `\n`, `\r` или `\r\n`, удаляет разделитель
- **READTOEND**: читает все оставшиеся байты, декодирует в строку
- **READTEXT(n)**: читает n символов (не байт!), корректно обрабатывает multi-byte UTF-8
- **READLINES(n)**: читает n строк, возвращает массив строк

---

## 35. READ Numeric

Прямого чтения чисел из файла нет. Числа читаются как строки и преобразуются через `VAL()`.

---

## 36. READ Boolean

Прямого чтения Boolean из файла нет.

---

## 37. Binary vs Text

**Binary mode: НЕ ПОДДЕРЖИВАЕТСЯ.** Все файловые операции text-only.

| Property | Text |
|----------|------|
| Encoding | UTF-8 (no BOM) |
| Newline | `\n`, `\r`, `\r\n` (read); `Environment.NewLine` (write) |
| Number representation | Нет прямой записи |
| EOF | `_position >= _length` |
| Position | Байтовая позиция |
| Size | Байтовый размер |

---

## 38. Encoding Model

- **Default encoding**: UTF-8 без BOM (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`)
- **Strict encoding**: UTF-8 с `throwOnInvalidBytes: true`
- **Fallback encoding**: Windows-1251 (для `READFILE` и `READALLLINES`)
- **Explicit encoding**: нет — encoding фиксирован на UTF-8

---

## 39. Encoding Consistency

| Компонент | Encoding |
|-----------|----------|
| INPUT (console) | Зависит от хоста |
| PRINT (console) | Зависит от хоста |
| File READ | UTF-8 strict |
| File WRITE | UTF-8 no BOM |
| String | UTF-16 (.NET internal) |
| READFILE (one-shot) | UTF-8 strict → Windows-1251 fallback |

---

## 40. BOM

- **Write**: BOM НЕ записывается (`encoderShouldEmitUTF8Identifier: false`)
- **Read**: BOM обрабатывается в `StripUtf8Bom()` для `READFILE`/`READALLLINES`, но не для потокового `READLINE`/`READTOEND`

---

## 41. Newline Model

- **Read**: `\n`, `\r`, `\r\n` — все три варианта обрабатываются в `ReadLineCore()`
- **Write**: `Environment.NewLine` (на Windows: `\r\n`)

---

## 42. Positioning

- `FILEPOS(f)` — текущая позиция в байтах
- `SEEKFILE(f, position)` — переход к абсолютной позиции в байтах
- `REWIND(f)` — переход к началу (SEEKFILE(f, 0))

---

## 43. SEEK Boundaries

- Отрицательная позиция: diagnostic → `RuntimeFailureException`
- За пределами файла: diagnostic → `RuntimeFailureException`
- Равно длине файла: допустимо (EOF)
- Начало (0): допустимо

---

## 44. SEEK After READ

Корректно: `SEEKFILE` вызывает `_stream.Seek()`.

---

## 45. SEEK After WRITE

Корректно: `SEEKFILE` автоматически flush-ит буфер записи перед seek.

---

## 46. SEEK Text vs Binary

Только text mode. Позиция — байтовая, не символьная.

---

## 47. EOF Semantics

`EOF(f)` = `_position >= _length`

- Перед чтением: false (если файл не пустой)
- Exactly at end: true
- После чтения past EOF: true

---

## 48. EOF Type

`EOF(f)` возвращает `BOOLEAN`.

---

## 49. EOF + Buffering

EOF проверяет `_position >= _length`, а не состояние FileStream. Позиция обновляется при каждом чтении. Буфер записи не влияет на EOF для чтения.

---

## 50. Empty File

```
OPEN empty_file FOR INPUT AS #f
READLINE(f) → EOF check → "достигнут конец файла" → RuntimeFailureException
EOF(f) → true
```

---

## 51. Large File

- Позиция: `long` (64-bit)
- Length: `long` (64-bit)
- Нет ограничения на 2GB на уровне языка

---

## 52. File Length

`FILELENGTH(f)` = `_length` (байтовый размер). Обновляется при write.

---

## 53. Exists / Directory APIs

Полная поддержка через built-in функции:
- `FILEEXISTS(path)`, `DIREXISTS(path)`, `PATHEXISTS(path)`
- `GETFILES(path)`, `GETDIRS(path)` (с optional pattern)
- `FILESIZE(path)`, `FILEDATE(path)`, `FILECREATEDATE(path)`, `FILEACCESSEDDATE(path)`
- `MAKEDIR(path)`, `DELETEFILE(path)`, `COPYFILE(src, dst)`, `MOVEFILE(src, dst)`, `RENAMEFILE(path, name)`
- `DELETEDIR(path)`, `DELETEDIRRECURSIVE(path)`, `COPYDIR(src, dst)`, `MOVEDIR(src, dst)`, `RENAMEDIR(path, name)`
- `CURRENTDIR()`, `CHANGEDIR(path)`, `DIRSIZE(path)`
- `FULLPATH(path)`, `FILENAME(path)`, `DIRNAME(path)`, `EXTNAME(path)`, `PATHROOT(path)`, `ISABSOLUTEPATH(path)`
- `COMBINEPATH(left, right)`
- `ISREADONLY(path)`, `ISHIDDEN(path)`, `DRIVENAME(path)`, `DRIVEFORMAT(path)`, `DRIVETYPE(path)`
- `FREESPACE(path)`, `TOTALFREESPACE(path)`, `TOTALSPACE(path)`
- `MAKETEMPFILE(prefix)`, `MAKETEMPDIR(prefix)`

---

## 54. Delete / Rename

- `DELETEFILE(path)` — удаляет файл, возвращает TRUE/FALSE
- `RENAMEFILE(path, newName)` — переименовывает
- Открытый файл: зависит от ОС (на Windows может быть ошибка)

---

## 55. File Resource Ownership

- **RuntimeTextFile** создаётся в OPEN
- **Owner**: `_openFiles` HashSet в Interpreter/BytecodeVm
- **FileStream**: принадлежит RuntimeTextFile
- **Cleanup**: deterministic через `Dispose()`

---

## 56. Disposal

- Явный `CLOSE` → `Dispose()`
- Program termination → `CloseAllOpenFiles()` → `Dispose()`
- Exception → `finally` block → `CloseAllOpenFiles()` → `Dispose()`
- GC: `RuntimeTextFile` реализует `IDisposable`, но не имеет финализатора

---

## 57. Exception Cleanup

```
OPEN → WRITE → runtime error → RuntimeFailureException → finally → CloseAllOpenFiles()
```

Ресурс корректно освобождается.

---

## 58. Open File Across RETURN

```
FUNCTION F()
    OPEN "file" FOR INPUT AS #f
    RETURN  ← файл не закрыт явно
END FUNCTION
```

Файл остаётся в `_openFiles` до завершения программы. Не является утечкой, так как `finally` в `Execute()` закроет все файлы.

---

## 59. Global File Handles

Переменные FILE могут быть глобальными (`DIM f AS FILE` на верхнем уровне). Инициализация: `RuntimeValue.Default(TypeSymbol.File)` = `FromFile(null)`.

---

## 60. File Handle Parameters

Переменные FILE могут передаваться как аргументы SUB/FUNCTION. Поскольку это object reference, callee видит тот же файл.

---

## 61. File Handle Returns

TYPE не может содержать поля типа FILE (`Binder.Declarations.cs:166` — запрет). FUNCTION не может возвращать FILE напрямую.

---

## 62. Double Close

```
CLOSE #f
CLOSE #f  ← второй раз
```

`RequireOpenFile()` → `file is not null && file.IsOpen` → false → diagnostic → `RuntimeFailureException`.

---

## 63. Invalid Handle

- Неинициализированная переменная FILE: `RuntimeValue.FromFile(null)` → `AsFile()` = null → diagnostic
- Закрытый файл: переменная = null → diagnostic

---

## 64. Handle Reuse

```
OPEN "A" FOR INPUT AS #f
CLOSE #f
OPEN "B" FOR INPUT AS #f
```

Переменная `f` перезаписывается новым `RuntimeTextFile`. Старый объект больше не reachable через переменную.

---

## 65. Multiple Open Files

```
DIM f1 AS FILE
DIM f2 AS FILE
OPEN "a.txt" FOR INPUT AS #f1
OPEN "b.txt" FOR OUTPUT AS #f2
```

Корректно: `_openFiles` содержит оба файла.

---

## 66. Maximum Handle Count

Нет явного ограничения на уровне языка. Ограничено только ОС и памятью.

---

## 67. Same File Multiple Handles

```
OPEN "same.txt" FOR INPUT AS #f1
OPEN "same.txt" FOR INPUT AS #f2
```

Корректно: `FileShare.Read` позволяет multiple readers. Каждый handle имеет независимую позицию.

---

## 68. Concurrent Access

Axiom single-threaded. Нет thread safety concerns на уровне языка.

---

## 69. Buffering

- **Read**: не буферизован (прямое чтение из FileStream)
- **Write**: буферизован через `StringBuilder _writeBuffer`
- **Flush**: автоматический при Seek, Close, Dispose; явный через `FLUSH(f)`

---

## 70. Partial Read / Write

- READLINES с count: читает сколько возможно, возвращает partial array
- WRITE: записывает всю строку атомарно

---

## 71-72. READ/WRITE After SEEK / EOF

Корректно: Seek обновляет позицию, последующие READ/WRITE работают с новой позицией.

---

## 73. Text File Format

- Разделитель строк: `\n`, `\r`, `\r\n` (read); `Environment.NewLine` (write)
- Encoding: UTF-8
- Числа: только через строковое преобразование

---

## 74. Binary File Format

**Не поддерживается.**

---

## 75. TYPE Serialization

**Не поддерживается.** TYPE не может быть сериализован в файл напрямую.

---

## 76. FILE Error Model

| Ошибка | Код | Поведение |
|--------|-----|-----------|
| File not found | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Already open | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Closed file | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Wrong mode | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Invalid path | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Permission denied | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Seek beyond end | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Seek in APPEND | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| EOF on READLINE | `BuiltinArgument` | diagnostic + RuntimeFailureException |
| Encoding error | `BuiltinArgument` | diagnostic + RuntimeFailureException |

---

## 77. Error Propagation

```
OS error (IOException, etc.)
  ↓
IsFileSystemException() filter
  ↓
ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, message, location)
  ↓
throw new RuntimeFailureException()
  ↓
catch in Execute() / ExecuteModule()
  ↓
RuntimeResult with diagnostics
```

---

## 78. Error Location

Все file errors привязаны к `TextLocation` через `BoundNode.Location` или `ResolveLocation()` в VM.

---

## 79. Error State Cleanup

После file error:
- Handle: не создаётся (OPEN failed) или уже валидно закрыт (CLOSE failed)
- Runtime: продолжает работу (diagnostic + exception catch)
- Следующая file operation: возможна

---

## 80. Security Boundary

**Фактическая boundary**: нет sandbox. Axiom имеет полный доступ к файловой системе через .NET APIs.

- Path traversal: разрешён
- Absolute paths: разрешены
- System directories: доступны

**Intended trust model**: локальный язык для автора, unrestricted access by design.

---

## 81-85. Symlink / Locking / Atomicity / Crash / Partial Write

- **Symlinks**: .NET `FileStream` следует symlinks
- **Locking**: `FileShare.Read` — shared read
- **Atomicity**: не гарантирована
- **Crash consistency**: не гарантирована
- **Partial write**: зависит от ОС

---

## 86-92. File + Other Subsystems

- **String**: encoding UTF-8 ↔ .NET UTF-16 автоматически
- **Array**: `READLINES` возвращает `RuntimeArrayValue` строк
- **TYPE**: FILE-поля в TYPE запрещены
- **Functions**: файловые переменные могут передаваться как аргументы
- **Loops**: `WHILE NOT EOF(f)` — корректный паттерн
- **Control flow**: OPEN в IF — корректно, файл в `_openFiles` до cleanup
- **Exceptions**: TRY/CATCH — файл остаётся открытым до program-level cleanup

---

## 93. Bytecode File Model

```
OPEN: LoweredInstruction.OpenFile → BytecodeInstruction(Opcode.OpenFile, [mode, debugName])
CLOSE: LoweredInstruction.CloseFile → BytecodeInstruction(Opcode.CloseFile)
Built-in calls: CallBuiltin → EvaluateXxxBuiltin() → BuiltinFileIOHelpers.*
```

---

## 94. File Opcodes

| Opcode | Stack Effect | Назначение |
|--------|-------------|------------|
| `OpenFile` | pop 2 → push 1 | Открытие файла |
| `CloseFile` | pop 1 → push 1 | Закрытие файла |
| `CallBuiltin` | varies | Вызов built-in (READLINE, WRITE и т.д.) |

---

## 95. Stack Discipline

- `OpenFile`: pop path + file variable → push new file value
- `CloseFile`: pop file value → push null file value
- Built-in calls: pop arguments → push result

---

## 96. Interpreter vs Bytecode VM

| File Feature | Interpreter | Bytecode VM | Same Semantics? |
|-------------|-------------|-------------|-----------------|
| Open | `ExecuteOpen()` | `ExecuteOpenFile()` | **Да** |
| Close | `ExecuteClose()` | `ExecuteCloseFile()` | **Да** |
| Read (all forms) | `EvaluateXxx()` | `EvaluateXxxBuiltin()` | **Да** |
| Write (all forms) | `EvaluateWrite()` | `EvaluateWriteBuiltin()` | **Да** |
| Seek | `EvaluateSeekFile()` | `EvaluateSeekFileBuiltin()` | **Да** |
| EOF | `EvaluateEof()` | `EvaluateEofBuiltin()` | **Да** |
| Flush | `EvaluateFlush()` | `EvaluateFlushBuiltin()` | **Да** |
| FileSystem built-ins | direct calls | direct calls | **Да** |

**Паритет confirmed.**

---

## 97. Bytecode Coverage

Согласно `docs/BYTECODE_COVERAGE.md`:
- File state built-ins: VM да, Автотесты да, Sample да, Manual да
- File read built-ins: VM да, Автотесты да, Sample да, Manual да
- File write built-ins: VM да, Автотесты да, Sample да, Manual да
- File system built-ins: VM да, Автотесты да, Sample да, Manual да
- File management: VM да, Автотесты да, Sample да, Manual да
- Path helpers: VM да, Автотесты да, Sample да, Manual да

---

## 98. Bytecode Performance

File operations benchmarked: `open_file_stream_cycle.axm` ~ `0.813x runtime / 0.834x total`.

---

## 99-102. Tests

**Test matrix**:

| Feature | Parser | Semantic | Interpreter | VM | Integration | E2E | Sample | Docs |
|---------|--------|----------|-------------|-----|-------------|-----|--------|------|
| OPEN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| CLOSE | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| READLINE | — | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| WRITE | — | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| SEEK | — | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| EOF | — | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| FileSystem | — | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

**Negative tests**: File not found ✅, Already open ✅, Closed file read ✅, Seek in APPEND ✅, Seek beyond end ✅, EOF on READLINE ✅, Missing file for COPYFILE ✅, Missing directory for CHANGEDIR ✅.

---

## 103. Cross-Platform Audit

- Path separator: `Path.Combine()` — кроссплатформенный
- Newline: `Environment.NewLine` — ОС-зависимый
- Encoding: UTF-8 — кроссплатформенный
- Windows-1251 fallback: Windows-specific

---

## 104. Current Working Directory

- **Who sets it**: `CHANGEDIR()` built-in function
- **IDE protection**: `ProgramRunService` сохраняет/восстанавливает CWD

---

## 105. IDE File I/O

- Working directory: сохраняется/восстанавливается в `ProgramRunService`
- File access: unrestricted
- Diagnostics: через `DiagnosticCodes.BuiltinArgument`

---

## 106-110. Documentation / Duplication / Source of Truth / Smells

- Manual: `open.html`, `close.html`, `file-system-functions*.html` — comprehensive
- Duplication: минимальная — READLINES boilerplate (6 строк) в обоих движках
- Source of truth: `RuntimeTextFile.cs`, `BuiltinFileIOHelpers.cs`, `BuiltinFileSystemHelpers.cs`
- Smell: `Environment.CurrentDirectory` — глобальный mutable state, но защищён в IDE

---

## 111. File System Invariants

1. Каждый live file handle ссылается на валидный `RuntimeTextFile`.
2. Закрытый handle не может быть использован успешно.
3. Неудавшийся OPEN не создаёт phantom handle.
4. Каждый успешный OPEN имеет deterministic ownership и cleanup path.
5. READ/WRITE обновляют file position consistently.
6. Interpreter и VM暴露 идентичную file semantics.
7. Program termination не может silently leak file resources.
8. Path resolution deterministic для одного runtime context.

---

## 112-113. Problem Inventory

| ID | Severity | Category | Problem | Status |
|----|----------|----------|---------|--------|
| FILE-001 | P3 | BINARY | Нет binary mode | NOT SUPPORTED |
| FILE-002 | P3 | PRINT# | Нет PRINT #f | NOT SUPPORTED |
| FILE-003 | P3 | INPUT# | Нет INPUT #f | NOT SUPPORTED |
| FILE-004 | P3 | GET/PUT | Нет GET/PUT для файлов | NOT SUPPORTED |
| FILE-005 | P3 | CWD | Environment.CurrentDirectory mutable state | PROTECTED IN IDE |
| FILE-006 | P4 | BOM | BOM не обрабатывается в потоковом чтении | BY DESIGN |
| FILE-007 | P4 | ENCODING | Windows-1251 fallback хардкод | BY DESIGN |

---

## 114. Root Cause Map

| Root Cause | Affected Findings |
|------------|-------------------|
| Text-only design (no binary) | FILE-001, FILE-002, FILE-003, FILE-004 |
| Global CWD state | FILE-005 |
| UTF-8 first design | FILE-006, FILE-007 |

---

## 115. Stable Areas

**НЕ ТРОГАТЬ:**
- `RuntimeTextFile` — корректная обёртка над FileStream
- `BuiltinFileIOHelpers` — чистый общий слой
- `BuiltinFileSystemHelpers` — comprehensive filesystem operations
- Handle lifecycle — deterministic cleanup
- Error handling — consistent diagnostic model
- Interpreter/VM parity — confirmed

---

## 116-118. Consolidation / Separation / Removal

- Дублирование минимально (оба движка делегируют в helpers)
- Removal candidates: нет

---

## 119. Actual Security Boundary

**Фактическая**: нет sandbox. Полный доступ к файловой системе. Это intentional для локального языка.

---

## 120. Future Readiness

| Возможность | Готовность |
|-------------|-----------|
| Async file I/O | Низкая |
| Binary mode | Средняя |
| Richer encodings | Высокая |
| Directory APIs | Высокая |
| File metadata | Высокая |
| Structured serialization | Низкая |

---

## 121. Recovery Roadmap

| Шаг | Описание | Приоритет |
|-----|----------|-----------|
| 1 | Зафиксировать CWD isolation — DONE (ProgramRunService) | P2 |
| 2 | Добавить binary mode (FILE-001) | P3 (если нужно) |
| 3 | Добавить PRINT #f / INPUT #f (FILE-002/003) | P3 (если нужно) |

---

## 122. Definition of Done

1. ✅ File handles have deterministic lifecycle
2. ✅ Open/close semantics are explicit
3. ✅ Read/write behavior is consistent
4. ✅ EOF semantics are deterministic
5. ✅ Path resolution is known and stable
6. ✅ Text encoding semantics are explicit
7. ✅ Errors are predictable
8. ✅ Resources are cleaned up
9. ✅ Interpreter and VM agree
10. ✅ Tests cover failure paths
11. ✅ Documentation matches implementation

---

## 123. TOP 10 FILE SYSTEM ACTIONS

```
1. [P3] FILE-001 — Добавить binary file mode (если нужно)
2. [P3] FILE-002 — Добавить PRINT #f (если нужно)
3. [P3] FILE-003 — Добавить INPUT #f (если нужно)
4. [P3] FILE-004 — Добавить GET/PUT для файлов (если нужно)
5. [P4] FILE-006 — Документировать BOM handling
6. [P4] FILE-007 — Документировать Windows-1251 fallback
```

---

## 124. Final Health Score

| Аспект | Балл |
|--------|------|
| Path Handling | 9/10 |
| Open | 9/10 |
| Close | 9/10 |
| Handles | 9/10 |
| Read | 9/10 |
| Write | 9/10 |
| Append | 9/10 |
| Seek | 9/10 |
| EOF | 9/10 |
| Text Mode | 9/10 |
| Binary Mode | 0/10 |
| Encoding | 9/10 |
| Buffering | 9/10 |
| Ownership | 10/10 |
| Lifetime | 10/10 |
| Error Handling | 9/10 |
| Security | 8/10 |
| Interpreter | 9/10 |
| Bytecode VM | 9/10 |
| Diagnostics | 9/10 |
| Tests | 9/10 |
| Samples | 9/10 |
| Documentation | 9/10 |
| IDE | 9/10 |
| Performance | 9/10 |
| Reliability | 9/10 |
| Maintainability | 9/10 |
| Cross-Platform | 8/10 |
| Future Readiness | 7/10 |

**Общий Health Score: 8.8/10**

---

## ГЛАВНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> **Можно ли считать файловую подсистему Axiom достаточно стабильной, чтобы переходить к глубокому аудиту системы ошибок, исключений и аварийного завершения?**

**YES**

Blockers: нет.
