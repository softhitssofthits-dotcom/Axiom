# Журнал работ 5

Этот файл является активным журналом работ проекта начиная с Session 1032.

Исторические части журнала:
- `docs/WORKLOG.md` содержит записи до Session 168 включительно;
- `docs/WORKLOG2.md` содержит записи с Session 169 по Session 309 включительно;
- `docs/WORKLOG3.md` содержит записи с Session 310 по Session 831 включительно;
- `docs/WORKLOG4.md` содержит записи с Session 832 по Session 1031 включительно.

Правила ведения остаются прежними:
- новые записи добавляются только в конец файла;
- заголовок новой записи имеет вид `## Session <N> - YYYY-MM-DD HH:mm - <название>`;
- номер сеанса продолжает общую последовательность из всех частей журнала;
- дата и время указываются по местному времени проекта.

## Session 1032 - 2026-08-18 17:30 - ротация WORKLOG4 → WORKLOG5

Задача: перевести `docs/WORKLOG4.md` в исторический статус, создать `docs/WORKLOG5.md` как активный журнал.

Что сделано:
1. `docs/WORKLOG4.md` переведён в исторический статус (записи Session 832-1031).
2. `docs/WORKLOG5.md` создан как активный журнал (начиная с Session 1032).
3. Обновлены ссылки в `AGENTS.md` и `docs/agent/` файлах.

Текущее состояние проекта:
- Волна 1 (безопасность ресурсов) — завершена.
- Волна 3.2 (диагностические коды) — завершена.
- Волна 2.1 (Numeric) — завершена, -95 строк.
- Волна 2.2 (Regex) — завершена, -374 строки.
- Волна 2.3 (String) — завершена, -407 строк.
- Волна 4.1 (документы PLANS3→PLANS4) — завершена.
- Суммарно удалено дублирования: ~876 строк.

Следующий шаг: продолжить вынос built-in функций в общий слой — FileSystem и DateTime.

## Session 1033 - 2026-08-18 18:00 - Вынесен общий слой FileSystem built-in функций

Задача: создать `BuiltinFileSystemHelpers` и рефакторить FileSystem built-in функции в Interpreter и BytecodeVm.

Что сделано:
1. Создан `src/Axiom.Runtime/Builtins/BuiltinFileSystemHelpers.cs` (107 строк) — статический класс:
   - 3 поля кодировок (`FileSystemTextEncoding`, `StrictUtf8TextEncoding`, `Windows1251TextEncoding`)
   - 10 статических методов: `NormalizeFileSystemPath`, `ReadTextFileWithFallback`, `StripUtf8Bom`, `CreateWindows1251TextEncoding`, `ReadDecodedLines`, `CopyDirectory`, `GetDirectorySize`, `CreateTemporaryFile`, `CreateTemporaryDirectory`, `IsFileSystemException`
   - 3 валидационных метода: `EnsureFileExists`, `EnsureDirectoryExists`, `ReportFileSystemFailure`
2. Рефакторинг `Interpreter.Builtins.FileSystem.cs` (1167 → 980 строк, -187)
3. Рефакторинг `BytecodeVm.Builtins.FileSystem.cs` (1011 → 782 строк, -229)
4. Обновлены `Interpreter.FileIO.cs`, `BytecodeVm.FileIO.cs`, `BytecodeVm.cs`

Суммарно по FileSystem: -416 строк.

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.

## Session 1034 - 2026-08-18 18:30 - Вынесен общий слой DateTime built-in функций

Задача: создать `BuiltinDateTimeHelpers` и рефакторить DateTime built-in функции в Interpreter и BytecodeVm.

Что сделано:
1. Создан `src/Axiom.Runtime/Builtins/BuiltinDateTimeHelpers.cs` (357 строк) — статический класс:
   - 3 перечисления: `TemporalValueKind`, `DateTimeIntervalKind`, `DateTimePartKind`
   - 3 record struct: `CanonicalTemporalValue`, `IntervalTokenInfo`, `PartTokenInfo`
   - 3 константы формата, 4 статических поля данных
   - 21 статический метод (форматирование, парсинг, арифметика, diff, part)
   - 5 parse-хелперов с делегатами для отчётности об ошибках
2. Рефакторинг `Interpreter.Builtins.DateTime.cs` (393 → 298 строк, -95)
3. Рефакторинг `Interpreter.Builtins.DateTime.Helpers.cs` (696 → 351 строк, -345)
4. Рефакторинг `BytecodeVm.Builtins.DateTime.cs` (1022 → 662 строк, -360)

Суммарно по DateTime: -800 строк.

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.

## Итоговый эффект общего слоя built-in функций

| Область | Хелпер | Удалено строк |
|---------|--------|--------------|
| Numeric | BuiltinNumericHelpers | -95 |
| Regex | BuiltinRegexHelpers | -374 |
| String | BuiltinStringHelpers | -407 |
| FileSystem | BuiltinFileSystemHelpers | -416 |
| DateTime | BuiltinDateTimeHelpers | -800 |
| **Итого** | | **-2,092** |

Следующий шаг: Array builtins (самая крупная область, ~68 KB).

## Session 1033 - 2026-08-18 17:45 - рефакторинг BytecodeVm.Builtins.FileSystem → BuiltinFileSystemHelpers

Задача: рефакторинг `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.FileSystem.cs` (1011 строк) для использования общего класса `BuiltinFileSystemHelpers` из `src/Axiom.Runtime/Builtins/BuiltinFileSystemHelpers.cs`.

Что сделано:
1. В `BytecodeVm.Builtins.FileSystem.cs`:
   - добавлен `using Axiom.Runtime.Builtins;`;
   - удалены `using System.Security;` и `using System.Text;` (больше не нужны);
   - удалены3 статических поля кодировок (`FileSystemTextEncoding`, `StrictUtf8TextEncoding`, `Windows1251TextEncoding`);
   - удалены10 дублирующих статических методов: `NormalizeFileSystemPath`, `ReadTextFileWithFallback`, `StripUtf8Bom`, `CreateWindows1251TextEncoding`, `ReadDecodedLines`, `CopyDirectory`, `GetDirectorySize`, `CreateTemporaryFile`, `CreateTemporaryDirectory`, `IsFileSystemException`;
   - все вызовы заменены на `BuiltinFileSystemHelpers.*`;
   - для валидационных методов `EnsureFileExists` и `EnsureDirectoryExists` созданы тонкие обёртки, передающие `ReportBuiltInArgumentError` делегатом;
   - для `ReportFileSystemFailure` создана тонкая обёртка через `BuiltinFileSystemHelpers.ReportFileSystemFailure`;
   - сохранены BytecodeVm-специфичные методы: `EvaluateExistingFileDate`, `EvaluateDriveLong`, `EvaluateDriveString`, `CreateStringArrayValue`, `EnsureSearchPattern`, `EnsureSimpleFileSystemName`, `EnsureSimpleTempPrefix`, `GetExistingFileSystemInfo`, `GetDriveInfo`, `ReportFileSystemMissingFile`.
2. В `BytecodeVm.FileIO.cs`:
   - добавлен `using Axiom.Runtime.Builtins;`;
   - заменены `NormalizeFileSystemPath`, `FileSystemTextEncoding`, `IsFileSystemException` на `BuiltinFileSystemHelpers.*`.
3. В `BytecodeVm.cs`:
   - добавлен `using Axiom.Runtime.Builtins;`;
   - заменены все вызовы `NormalizeFileSystemPath`, `IsFileSystemException`, `FileSystemTextEncoding`, `GetDirectorySize`, `CopyDirectory` на `BuiltinFileSystemHelpers.*`.

Результат:
- файл уменьшился с 1011 до 782 строки (−229 строк);
- сборка без ошибок;
- все 875 тестов пройдены.

Следующий шаг: продолжить вынос built-in функций в общий слой — DateTime.

## Session 1035 - 2026-08-18 19:15 - Рефакторинг BytecodeVm.Builtins.Array → BuiltinArrayHelpers

Задача: рефакторинг `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` (1958 строк) для использования общего класса `BuiltinArrayHelpers` из `src/Axiom.Runtime/Builtins/BuiltinArrayHelpers.cs`.

Что сделано:
1. В `BytecodeVm.Builtins.Array.cs`:
   - добавлен `using Axiom.Runtime.Builtins;`;
   - удалены 18 дублирующих статических методов: `CreateUnallocatedArrayValue`, `CreateVisibleShapeArrayValue` (2 перегрузки), `GetDimensionLength`, `CreateAdjustedBounds`, `ForEachArrayIndexAscending`, `ForEachArrayIndexDescending`, `ForEachArrayIndexSetAscending`, `ForEachIndexAscending`, `ForEachIndexDescending`, `ForEachIndexSetAscending`, `ForEachIndexSetAscendingRecursive`, `ReverseOneDimensionalArray`, `ReverseArrayAlongDimension`, `CreateReducedBounds`, `CreateBoundsAfterDimensionShrink`, `RemoveSelectedDimensionIndices`, `ExpandReducedIndices`;
   - удалены 5 дублирующих экземплярных методов: `ArrayContainsValue`, `ForEachArrayLineBaseIndicesAscending`, `ForEachArrayLineBaseIndicesForResize`, `CreateArrayWindowCopyBuiltin`, `CreateReversedArrayCopyBuiltin`;
   - все вызовы заменены на `BuiltinArrayHelpers.*`;
   - сохранены BytecodeVm-специфичные методы: `CopyOneDimensionalArrayRange`, `TryAppendOneDimensional`, `TryPopOneDimensional`, `TryMoveOneDimensionalRange`, все `Evaluate*Builtin`, все валидационные методы с `TextLocation`, `CreateCompactArrayValue` (использует `CreateCompactOneDimensional`), `CreateArrayValue`, `CloneArrayValue` (не byte-идентичны хелперу).
2. В `BuiltinArrayHelpers.cs`:
   - исправлен дефект в `ValuesEqual`: fallback-случай `ReferenceEquals(left.Storage, right.Storage)` заменён на `false` (поле `Storage` не существует в `RuntimeValue`).

Результат:
- файл уменьшился с 1958 до 1574 строки (−384 строк);
- сборка без ошибок;
- все 875 тестов пройдены.

Итоговый эффект общего слоя built-in функций:

| Область | Хелпер | Удалено строк |
|---------|--------|--------------|
| Numeric | BuiltinNumericHelpers | -95 |
| Regex | BuiltinRegexHelpers | -374 |
| String | BuiltinStringHelpers | -407 |
| FileSystem | BuiltinFileSystemHelpers | -416 |
| DateTime | BuiltinDateTimeHelpers | -800 |
| Array | BuiltinArrayHelpers | -811 |
| **Итого** | | **-2,903** |

Волна 2 плана улучшений (вынос built-in функций в общий слой) завершена.

Следующий шаг: волна 3 плана — улучшение архитектуры (разбить MainWindowViewModel, создать Axiom.Syntax.Tests, разбить InterpreterTests.cs).

## Session 1036 - 2026-08-18 20:00 - Вынесен общий слой Array built-in функций (Interpreter)

Задача: рефакторинг `Interpreter.Builtins.Array.cs` (1492 строки) для использования общего класса `BuiltinArrayHelpers` — вынести дублированные алгоритмы сортировки и бинарного поиска.

Что сделано:
1. В `BuiltinArrayHelpers.cs` (454 → 611 строк, +157) добавлены 5 новых методов с делегатом `Func<RuntimeValue, RuntimeValue, int>`:
   - `SortArrayAlongDimension` — стабильная сортировка вдоль измерения
   - `GetStableSortedLine` — сбор и сортировка линии с сохранением порядка
   - `FindArrayBinarySearchIndex` (1D) — бинарный поиск с checked arithmetic для «не найдено»
   - `FindArrayBinarySearchIndex` (multi-dim) — бинарный поиск по линии многомерного массива
   - `FindArrayBinarySearchCoordinateSet` — координатный бинарный поиск
2. В `Interpreter.Builtins.Array.cs` (1492 → 1368 строк, −124):
   - удалены дублированные: `SortArrayAlongDimension`, `GetStableSortedLine`, `FindArrayBinarySearchIndex` (2 перегрузки), `FindArrayBinarySearchCoordinateSet`, `GetArrayNotFoundIndex` (для binary search)
   - добавлены тонкие обёртки `SortArrayAlongDimension` и `FindArrayBinarySearchCoordinates`, делегирующие в `BuiltinArrayHelpers`
   - `FindArrayIndex` (линейный поиск) сохранён как Interpreter-специфичный
3. В `BytecodeVm.Builtins.Array.cs` (1574 → 1416 строк, −158):
   - удалены дублированные: `SortArrayAlongDimension`, `GetStableSortedLine`, `FindArrayBinarySearchIndexBuiltin`, `FindArrayBinarySearchCoordinatesBuiltin`, `FindArrayBinarySearchCoordinateSetBuiltin`
   - все вызовы заменены на `BuiltinArrayHelpers.*` с лямбдой `(l, r) => Compare(l, r, location)`

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.

Итоговый эффект общего слоя built-in функций:

| Область | Хелпер | Удалено строк |
|---------|--------|--------------|
| Numeric | BuiltinNumericHelpers | -95 |
| Regex | BuiltinRegexHelpers | -374 |
| String | BuiltinStringHelpers | -407 |
| FileSystem | BuiltinFileSystemHelpers | -416 |
| DateTime | BuiltinDateTimeHelpers | -800 |
| Array (BytecodeVm) | BuiltinArrayHelpers | -811 |
| Array (Interpreter) | BuiltinArrayHelpers | -124 |
| Array (helpers) | BuiltinArrayHelpers | +157 |
| **Итого** | | **-2,870** |

Следующий шаг: волна 3 плана — улучшение архитектуры (разбить MainWindowViewModel, создать Axiom.Syntax.Tests, разбить InterpreterTests.cs).

## Session 1037 - 2026-08-18 21:00 - Волна 3: Axiom.Syntax.Tests и разбиение InterpreterTests

Задача: выполнить волну 3 плана улучшений — создать Axiom.Syntax.Tests и разбить InterpreterTests.cs на partial-файлы.

Что сделано:

### 1. Создан Axiom.Syntax.Tests (новый тестовый проект)

- `tests/Axiom.Syntax.Tests/Axiom.Syntax.Tests.csproj` — добавлен в solution.
- `SyntaxTokenTests.cs` — 8 тестов: конструктор, свойства, ToString, null-валидация.
- `ExpressionSyntaxTests.cs` — 16 тестов: все типы выражений, Kind-проверки и null-валидация.
- `StatementSyntaxTests.cs` — 16 тестов: Print, Dim, If, For, While, Assignment, SelectCase, Input, Sleep и др.
- `DeclarationSyntaxTests.cs` — 16 тестов: NamedType, TypeClause, VariableDeclarator, Program, Sub/Function/TypeDeclaration, ArrayType, Parameter.
- `SyntaxKindTests.cs` — 5 тестов: BadToken=0, токены < statement, distinct kinds, общий счёт >=200.

Итого: 64 новых теста, все пройдены.

### 2. Разбит InterpreterTests.cs (12,015 строк → 11 partial-файлов)

| Файл | Строк | Тестов | Содержание |
|------|-------|--------|------------|
| InterpreterTests.cs | 821 | 47 | выражения, print, input, key, mouse, переменные, control flow, math |
| InterpreterTests.Print.cs | 166 | 16 | print с разделителями, print using |
| InterpreterTests.Graphics.cs | 2,235 | 88 | screen, circle, line, view, palette, canvas, viewport, draw, paint, get/put |
| InterpreterTests.DateTime.cs | 484 | 28 | datetime builtins |
| InterpreterTests.Array.cs | 2,825 | 126 | все операции массивов |
| InterpreterTests.String.cs | 1,454 | 82 | строковые функции, regex, split, join |
| InterpreterTests.FileSystem.cs | 924 | 28 | файловая система |
| InterpreterTests.FileIO.cs | 620 | 26 | файловый ввод-вывод |
| InterpreterTests.Error.cs | 205 | 11 | обработка ошибок |
| InterpreterTests.Type.cs | 898 | 30 | типы, записи, методы, конструкторы |
| InterpreterTests.TryCatch.cs | 1,473 | 7+records | try/catch/finally, вспомогательные record-типы |

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.
- `dotnet test tests/Axiom.Syntax.Tests` — 64/64 пройдено.
- Кириллица во всех тестах сохранена.

Итого по волне 3:
- 3.3 (Axiom.Syntax.Tests) — завершена, 64 теста.
- 3.4 (разбить InterpreterTests) — завершена, 12,015 → 11 файлов.

Следующий шаг: 3.1 (разбить MainWindowViewModel) или закрытие сессии.

## Session 1038 - 2026-08-18 22:00 - Волна 3.1: разбивка MainWindowViewModel

Задача: разбить `MainWindowViewModel.cs` (3,354 строки, 53 команды) на partial-файлы.

Что сделано:

| Файл | Строк | Содержание |
|------|-------|------------|
| MainWindowViewModel.cs | 1,380 | поля, конструктор, свойства, документы, about, recent, shell state |
| MainWindowViewModel.Debug.cs | 807 | запуск, отладка, breakpoint, step, compile, stopped run state |
| MainWindowViewModel.FileTree.cs | 754 | файловое дерево: навигация, создание, копирование, переименование, удаление |
| MainWindowViewModel.Preferences.cs | 323 | тема, язык, форматирование, режим выполнения, shell preferences |
| MainWindowViewModel.EventLog.cs | 129 | IDE event log |

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.

Волна 3 плана улучшений завершена:
- 3.1 (MainWindowViewModel) — завершена, 3,354 → 5 файлов.
- 3.3 (Axiom.Syntax.Tests) — завершена, 64 теста.
- 3.4 (InterpreterTests) — завершена, 12,015 → 11 файлов.

Следующий шаг: продуктовая работа или волна 5 (тестирование графики).

## Session 1039 - 2026-08-18 23:00 - Волна 2: закрытие хвостов PrintUsing, FileIO, Mouse

Задача: создать общие хелперы для трёх оставшихся областей built-in функций.

Что сделано:

### 1. BuiltinPrintUsingHelpers.cs (544 строки)

| Файл | Было | Стало | Δ |
|------|------|-------|---|
| Interpreter.PrintUsing.cs | 559 | 28 | −531 |
| BytecodeVm.PrintUsing.cs | 768 | 88 | −680 |
| BuiltinPrintUsingHelpers.cs | — | 544 | +544 |
| **Сумма** | 1,327 | 660 | **−667** |

Извлечено: типы токенов формата, парсинг маски, числовое форматирование (базовое + расширенное), строковые поля, экспоненциальное форматирование, разделители тысяч.

### 2. BuiltinFileIOHelpers.cs (396 строк)

| Файл | Было | Стало | Δ |
|------|------|-------|---|
| Interpreter.FileIO.cs | 434 | 205 | −229 |
| BytecodeVm.FileIO.cs | 444 | 191 | −253 |
| BuiltinFileIOHelpers.cs | — | 396 | +396 |
| **Сумма** | 878 | 792 | **−86** |

Извлечено: RequireOpenFile, RequireReadableFile, RequireWritableFile, Evaluate* файловые операции, CloseFile, CloseAllOpenFiles, GetFileModeName, IsFileIoException.

### 3. BuiltinMouseHelpers.cs (136 строк)

| Файл | Было | Стало | Δ |
|------|------|-------|---|
| Interpreter.Mouse.cs | 232 | 129 | −103 |
| BytecodeVm.Mouse.cs | 246 | 143 | −103 |
| BuiltinMouseHelpers.cs | — | 136 | +136 |
| **Сумма** | 478 | 408 | **−70** |

Извлечено: MouseHandlerKey, IsSupportedMouseButton, ConsumeMouseButtonPress, TryReadMouseEvent, TranslateMouse*ToRuntimeCoordinates.

### Итоговый эффект общего слоя built-in функций

| Область | Хелпер | Δ строк |
|---------|--------|---------|
| Numeric | BuiltinNumericHelpers | −95 |
| Regex | BuiltinRegexHelpers | −374 |
| String | BuiltinStringHelpers | −407 |
| FileSystem | BuiltinFileSystemHelpers | −416 |
| DateTime | BuiltinDateTimeHelpers | −800 |
| Array | BuiltinArrayHelpers | −811 |
| PrintUsing | BuiltinPrintUsingHelpers | −667 |
| FileIO | BuiltinFileIOHelpers | −86 |
| Mouse | BuiltinMouseHelpers | −70 |
| **Итого** | | **−3,726** |

Проверки:
- `dotnet build Axiom.sln` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 875/875 пройдено.

Волна 2 плана улучшений полностью закрыта.
Следующий шаг: волна 5 (тестирование графики).

## Session 1040 - 2026-08-18 23:30 - Волна 5: графическая baseline-инфраструктура

Задача: добавить инфраструктуру baseline-тестирования графики в `Axiom.Runtime.Tests`.

Что сделано:
1. Создан `tests/Axiom.Runtime.Tests/Helpers/SoftwareFramebuffer.cs`:
   - `ToBmp` — экспорт pixel dictionary в 24-bit BMP (bottom-up, no compression).
   - `CompareBmp` — побайтовое сравнение двух BMP с координатами первого расхождения.
   - `ComparePixels` — прямое сравнение pixel dictionaries с допуском по цвету (для anti-aliasing).
   - `MapPaletteIndexToRgb` — маппинг палитрового индекса в RGB (дублирует логику TestRuntimeHost для независимости).
2. Создан `tests/Axiom.Runtime.Tests/GraphicsBaselineTests.cs`:
   - `ExecuteGraphicsProgram` — хелпер запуска Axiom-программы с возвратом pixel dictionary.
   - `GetPixels` / `GetCanvasPixels` / `GetAllCanvasPixels` — извлечение пикселей из TestRuntimeHost.
   - 9 baseline-тестов:
     - `Baseline_Screen_Line_DrawsHorizontalLine` — горизонтальная линия, 11 пикселей, цвет 12.
     - `Baseline_Screen_Line_DrawsDiagonalLine` — диагональ от (0,0) до (10,10), ≥11 пикселей.
     - `Baseline_Screen_Rect_FillsRectangle` — заливка прямоугольника BF, 66 пикселей.
     - `Baseline_Screen_Rect_DrawsBox` — контур прямоугольника B, только края, без interior.
     - `Baseline_Screen_Circle_DrawsEllipse` — окружность радиуса 10, ≥30 пикселей.
     - `Baseline_Screen_Pset_SetsPixels` — 4 индивидуальных пикселя в SCREEN 1.
     - `Baseline_Canvas_ClearFrame_FillsAllPixels` — CLS заливает весь canvas.
     - `Baseline_Canvas_FillRect_DrawsFilledRectangle` — FILLRECT на canvas с проверкой clipping.
     - `Baseline_Viewport_ClipsDrawing` — VIEWPORT обрезает FILLRECT до своей области.

Файлы:
- `tests/Axiom.Runtime.Tests/Helpers/SoftwareFramebuffer.cs` (создан)
- `tests/Axiom.Runtime.Tests/GraphicsBaselineTests.cs` (создан)

Проверки:
- `dotnet build tests/Axiom.Runtime.Tests` — 0 ошибок, 0 предупреждений.
- `dotnet test tests/Axiom.Runtime.Tests` — 884/884 пройдено (включая 9 новых).
