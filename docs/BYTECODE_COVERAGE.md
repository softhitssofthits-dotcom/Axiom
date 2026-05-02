# Реестр покрытия Bytecode VM

Этот файл фиксирует текущее покрытие пользовательских операторов, встроенных функций и связанных проверок для байткодного пути Axiom.

## Как использовать реестр

- Это не разовый отчёт, а постоянный рабочий реестр.
- После каждой задачи, которая меняет наблюдаемое поведение языка, покрытие `BytecodeVm`, acceptance-примеры, manual или паритет с интерпретатором, этот файл нужно обновлять в той же задаче.
- Если задача касается только внутренней оптимизации без изменения пользовательского поведения, здесь всё равно нужно обновить как минимум столбцы `Профиль` и `Примечание`, если изменился статус горячего участка или появились новые замеры.

## Что именно отслеживаем

Минимальный набор для строки реестра:

- `VM`:
  байткодный путь реально выполняет эту сущность без отката на интерпретатор.
- `Автотесты`:
  есть проверка поведения хотя бы на уровне `InterpreterTests`, `BytecodeVmTests`, `ExecutionPipelineTests` или acceptance-набора.
- `Sample`:
  есть runnable `.axm`-пример в `samples/`.
- `Manual`:
  поведение отражено в `docs/manual/`.
- `Сквозной VM`:
  есть явное подтверждение, что сценарий проходит через `ExecutionEngineKind.BytecodeVm` без fallback.
- `Негативный паритет`:
  есть проверка ошибок или стабильности диагностики между интерпретатором и байткодом, точечная или групповая.
- `Профиль`:
  нужно ли это сейчас ускорять по измерениям.

Статусы:

- `да`:
  покрытие есть и подтверждено.
- `частично`:
  покрытие есть, но оно групповое, косвенное или требует точечной досверки.
- `проверить`:
  есть сильное основание считать, что покрытие есть, но это ещё не перенесено в явный учётный статус.
- `нет`:
  покрытия сейчас нет или оно не зафиксировано.

## Общие источники подтверждения

- Поведенческие тесты интерпретатора: `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- Поведенческие тесты VM: `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- Сквозной запуск через `BytecodeVm`: `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- Acceptance-примеры: `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- Общая отрицательная проверка паритета диагностики: `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)` в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

## Операторы и языковые формы

| Сущность | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Арифметика и логика выражений: `+`, `-`, `*`, `/`, `\`, `MOD`, `^`, `AND`, `OR`, `XOR`, `NOT`, `EQV`, `IMP`, `<<`, `>>`, сравнения | да | да | да | да | да | частично | не приоритет | Покрыто acceptance-группами `samples/expressions/` и `samples/operators/` |
| Объявления и присваивание: `DIM`, `CONST`, `=` | да | да | да | да | да | частично | не приоритет | Включая скалярные и массивные формы |
| Массивные объявления и изменение формы: `REDIM`, `ERASE`, индексирование, частичное индексирование | да | да | да | да | да | частично | не приоритет | Многомерные массивы доведены до VM и manual |
| Вывод: `PRINT` | да | да | да | да | да | частично | не приоритет | Есть отдельные acceptance-примеры по формам списка и разделителям |
| Форматированный вывод: `PRINT USING` | да | да | да | да | да | частично | не приоритет | Сквозной тест есть, semantics не менялись |
| Ввод: `INPUT` | да | да | да | да | да | частично | не приоритет | Есть формы с одной, несколькими переменными и подсказкой |
| Файловые операторы: `OPEN`, `CLOSE` | да | да | да | да | да | частично | не приоритет | Подтверждено отдельными VM-прогонами и manual-разделами по open files |
| Ветвление: `IF ... ELSE ... END IF`, `SELECT CASE` | да | да | да | да | да | частично | не приоритет | Покрыто control-flow acceptance-набором |
| Циклы: `FOR/NEXT`, `WHILE`, `DO/LOOP`, `EXIT FOR`, `EXIT DO` | да | да | да | да | да | частично | горячо пройдено | Для `FOR/NEXT` есть ещё и отдельный профильный след по ускорению opcode |
| Процедуры и функции: `SUB`, `FUNCTION`, `RETURN` | да | да | да | да | да | частично | не приоритет | Включая локальные функции, вложенность и возврат значений |
| Замыкания и захват внешних локалей | да | да | да | да | да | частично | не приоритет | Сквозные тесты на VM есть, но реестр стоит дальше держать под отдельным наблюдением |
| Пользовательские типы: `TYPE`, доступ к полям, массивные поля | да | да | да | да | да | частично | не приоритет | Покрыты отдельными acceptance и execution-тестами |

## Встроенные функции массивов

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`, `ARRAYRANK`, `ARRAYBOUNDS` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()`, `Execute_ArrayBoundsReportsUnallocatedArrayState()`, `Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayMetadataBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/lbound|ubound|arraylength|arrayisallocated|arrayrank|arraybounds/` |
| `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTAKE`, `ARRAYSKIP` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()`, `Execute_ArraySliceReturnsCompactIndependentSlice()`, `Execute_ArrayConcatReturnsCompactIndependentArray()`, `Execute_CompiledArrayCloneCopiesMultidimensionalVisibleShape()`, `Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()`, `Run_WithBytecodeMode_UsesBytecodeVmForPureArrayBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-slices.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arrayclone|arrayslice|arrayconcat|arraytake|arrayskip/` |
| `ARRAYTOREVERSED`, `ARRAYREVERSE`, `ARRAYDISTINCT`, `ARRAYFLATTEN`, `ARRAYRESHAPE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_ArrayToReversedReturnsCompactIndependentArray()`, `Execute_CompiledArrayToReversedSupportsMultidimensionalDimension()`, `Execute_CompiledArrayReverseSupportsMultidimensionalDimension()`, `Execute_CompiledArrayFlattenAndReshape()`, `Execute_CompiledArrayReshapeReportsMismatchedVisibleElementCount()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayFlattenAndReshape()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-slices.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arraytoreversed|arrayreverse|arraydistinct|arrayflatten|arrayreshape/` |
| `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL`, `ARRAYBINARYSEARCH` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledArraySearchBuiltins()`, `Execute_CompiledMultidimensionalArrayBinarySearchBuiltinReturnsCoordinateVectorOrEmptyArray()`, `Execute_CompiledArrayCoordinateSearchBuiltinsReturnCoordinateVectorsAndTables()`, `Execute_CompiledArrayContainsAndCountScanMultidimensionalVisibleElements()`, `Run_WithBytecodeMode_UsesBytecodeVmForArraySearchBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arraycontains|arrayindexof|arraylastindexof|arraycount|arrayfindall|arraybinarysearch/` |
| `ARRAYFILL`, `ARRAYSORT`, `ARRAYTOSORTED` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledMutatingArrayBuiltins()`, `Execute_CompiledArrayFillMutatesMultidimensionalVisibleElements()`, `Execute_CompiledArraySearchBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForArraySearchBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arrayfill|arraysort|arraytosorted/` |
| `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledMutatingArrayBuiltins()`, `Execute_CompiledMultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()`, `Execute_CompiledMultidimensionalArrayInsertReportsIncompatibleValueShape()`, `Execute_CompiledMutatingArrayBuiltinsReportEmptyPop()`, `Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()`, `Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalDynamicArrayBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-diagnostics.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arraypush|arraypop|arrayinsert|arrayremove/` |
| `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledArrayCallbackBuiltinsUseFunctionValuesAndClosures()`, `Execute_CompiledArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()`, `Execute_CompiledArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()`, `Execute_CompiledArrayMapPreservesMultidimensionalEmptyVisibleShape()`, `Execute_CompiledArrayCallbackBuiltinsUseBuiltinFunctionValues()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayCallbackBuiltins()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arrayevery|arraysome|arrayfilter|arraymap|arrayreduce/` |

## Встроенные строковые функции

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `LEN`, `LEFT`, `RIGHT`, `MID` | да | да | да | да | да | частично | горячо пройдено | Тесты: `Execute_CompiledStringSliceAndTrimBuiltins()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`; manual: `docs/manual/string-functions-slices-and-length.html`; sample: `samples/functions/len|left|right|mid/` |
| `TRIM`, `LTRIM`, `RTRIM`, `LCASE`, `UCASE`, `REVERSE` | да | да | да | да | да | частично | горячо пройдено | Тесты: `Execute_BuiltinTrimVariants()`, `Execute_CompiledStringSliceAndTrimBuiltins()`; manual: `docs/manual/string-functions-trim-and-case.html`; sample: `samples/functions/trim|ltrim|rtrim|lcase|ucase|reverse/`; `TRIM` недавно чинился по корректности |
| `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `INSTR`, `INSTRREV`, `SUBSTRCOUNT` | да | да | да | да | да | частично | горячо пройдено | Тесты: `Execute_CompiledStringSearchBuiltins()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`; manual: `docs/manual/string-functions-search-and-replace.html`; sample: `samples/functions/contains|startswith|endswith|instr|instrrev|substrcount/` |
| `REPLACE`, `INSERT`, `REMOVE` | да | да | да | да | да | частично | горячо пройдено | Тесты: `Execute_CompiledStringSearchBuiltins()`, `Execute_CompiledStringFillAndEditBuiltins()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`; manual: `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/replace|insert|remove/` |
| `REPEAT`, `SPACE`, `STRING`, `PADLEFT`, `PADRIGHT`, `CENTER` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinStringUsesFirstFillCharacter()`, `Execute_BuiltinStringReportsEmptyFill()`, `Execute_CompiledStringFillAndEditBuiltins()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/repeat|space|string|padleft|padright|center/` |
| `ASC`, `CHR` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledStringCharBuiltins()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/asc|chr/` |
| `SPLIT`, `JOIN` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_CompiledStringArrayBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForStringArrayBuiltins()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/split|join/` |

## Встроенные функции регулярных выражений

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXCOUNT` | да | да | да | да | да | частично | не приоритет | Скаляры покрыты execution- и acceptance-тестами |
| `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT` | да | да | да | да | да | частично | не приоритет | Массивные формы отдельно закрыты на VM |
| `REGEXREPLACE` | да | да | да | да | да | частично | горячо пройдено | Тесты: `Run_WithBytecodeMode_UsesBytecodeVmForRegexScalarBuiltins()` и адресные проверки в `InterpreterTests`/`BytecodeVmTests`; manual: `docs/manual/regex-functions-replace-and-count.html`; sample: `samples/functions/regexreplace/`; есть недавние no-op-оптимизации и контрольные замеры по `RegexReplaceModes` |

## Встроенные числовые функции

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `INT`, `STR`, `VAL` | да | да | да | да | да | частично | не приоритет | Базовые преобразования закрыты acceptance- и execution-путём |
| `RND`, `ABS`, `SGN`, `MIN`, `MAX`, `CLAMP`, `SETSIGN` | да | да | да | да | да | частично | не приоритет | Явная VM-поддержка есть |
| `SIN`, `COS`, `TAN`, `ATN`, `ATN2`, `ASIN`, `ACOS`, `ASINH`, `ACOSH`, `ATANH`, `SINH`, `COSH`, `TANH` | да | да | да | да | да | частично | не приоритет | Тригонометрическая группа закрыта сквозным тестом и sample-набором |
| `SQR`, `CBRT`, `EXP`, `LOG`, `LOG10`, `LOG2`, `POW`, `FLOOR`, `CEIL`, `ROUND`, `FIX` | да | да | да | да | да | частично | не приоритет | Отдельного горячего статуса пока нет |

## Встроенные функции даты и времени

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `DATE`, `TIME`, `NOW`, `UTCDATE`, `UTCTIME`, `UTCNOW`, `TIMER` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_DateTimeBuiltinsReturnCurrentValues()`, `Execute_TimerReturnsNonNegativeNumber()`, `Execute_CompiledDateTimeCurrentBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`; manual: `docs/manual/date-time-functions-current.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/date|time|now|utcdate|utctime|utcnow|timer/` |
| `DATESERIAL`, `TIMESERIAL`, `YEAR`, `MONTH`, `MONTHNAME`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `WEEKDAYNAME` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_DateTimeSerialAndPartsBuiltins()`, `Execute_DateTimePartsRejectNonCanonicalDates()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`; manual: `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/dateserial|timeserial|year|month|monthname|day|hour|minute|second|weekday|weekdayname/` |
| `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_DateTimeArithmeticAndValidationBuiltins()`, `Execute_DateTimeArithmeticRejectsUnsupportedInterval()`, `Execute_TimeValueRejectsNonCanonicalInput()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`; manual: `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/dateadd|datediff|datepart|datevalue|timevalue|isdate/` |
| `UTCOFFSET`, `TOUTC`, `FROMUTC`, `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_DateTimeUtcBuiltins()`, `Execute_DateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_DateTimeUtcBuiltinsSupportSeasonalNamedTimeZones()`, `Execute_DateTimeCalendarHelpers()`, `Execute_CompiledDateTimeCurrentBuiltins()`, `Execute_CompiledDateTimeUtcBuiltinsSupportNamedTimeZones()`, `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`; manual: `docs/manual/date-time-functions-time-zones.html`, `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/utcoffset|toutc|fromutc|isleapyear|daysinmonth|startofmonth|endofmonth|startofyear|endofyear/` |

## Встроенные функции файловой системы и файлового ввода-вывода

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ISOPEN`, `FILEPATH`, `FILEMODE`, `FILEPOS`, `FILELENGTH`, `LINENUMBER`, `EOF` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_FileStateBuiltins()`, `Execute_FilePositionAndLengthForInput()`, `Execute_OpenReadLineAndEof()`, `Execute_CompiledOpenInputFileProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`, `docs/manual/data-types-strings-and-files.html`; sample: `samples/functions/isopen|filepath|filemode|filepos|filelength|linenumber|eof/` |
| `READTOEND`, `READTEXT`, `READLINE`, `READLINES`, `SKIPLINE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_ReadToEndReturnsRemainingText()`, `Execute_ReadTextCountsCharactersWhilePositionStaysByteBased()`, `Execute_ReadLinesReadsUpToCountAndAdvancesPosition()`, `Execute_ReadLinesReturnsEmptyArrayForZeroCountAndEndOfFile()`, `Execute_CompiledOpenInputFileProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`; sample: `samples/functions/readtoend|readtext|readline|readlines|skipline/` |
| `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_FilePositionAndSeekForOutput()`, `Execute_RewindMovesFileBackToBeginning()`, `Execute_CompiledOpenOutputAndAppendFileProgram()`, `Execute_SeekFileReportsAppendMode()`, `Execute_RewindReportsAppendMode()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`, `docs/manual/close.html`; sample: `samples/functions/write|writeline|newline|flush|seekfile|rewind/` |
| `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `READFILE`, `READALLLINES`, `WRITEFILE`, `APPENDFILE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid()`, `Execute_BuiltinFileSystemReadsAllLines()`, `Execute_BuiltinFileSystemReadsAllLinesFromEmptyFile()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`; manual: `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/fileexists|direxists|pathexists|readfile|readalllines|writefile|appendfile/` |
| `FILESIZE`, `FILEDATE`, `FILECREATEDATE`, `FILEACCESSEDDATE`, `DIRDATE` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`; manual: `docs/manual/file-system-functions-paths.html`; sample: `samples/functions/filesize|filedate|filecreatedate|fileaccesseddate|dirdate/` |
| `GETFILES`, `GETDIRS`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `EXTNAME`, `FULLPATH`, `PATHROOT`, `ISABSOLUTEPATH` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinFileSystemPathHelpersReturnExpectedValues()`, `Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`; manual: `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/getfiles|getdirs|combinepath|filename|dirname|extname|fullpath|pathroot|isabsolutepath/` |
| `MAKEDIR`, `DELETEFILE`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEDIR`, `DELETEDIRRECURSIVE`, `COPYDIR`, `MOVEDIR`, `RENAMEDIR` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles()`, `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories()`, `Execute_BuiltinFileSystemReportsCopyFileMissingSource()`, `Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/makedir|deletefile|copyfile|movefile|renamefile|deletedir|deletedirrecursive|copydir|movedir|renamedir/` |
| `CHANGEDIR`, `CURRENTDIR`, `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `ISREADONLY`, `ISHIDDEN`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE`, `MAKETEMPDIR` | да | да | да | да | да | частично | не приоритет | Тесты: `Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace()`, `Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()`, `Execute_BuiltinFileSystemCreatesTemporaryFileAndDirectory()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/changedir|currentdir|dirsize|freespace|totalfreespace|totalspace|isreadonly|ishidden|drivename|driveformat|drivetype|maketempfile|maketempdir/` |

## Что ещё нужно довести сверх текущих галочек

Это уже не про реализацию самих функций, а про качество сопровождения покрытия.

1. Нужна точечная досверка строк `Негативный паритет`.
   Сейчас для многих групп стоит `частично`, потому что есть общий отрицательный набор и стабильная паритетная проверка диагностики, но не всегда есть явная адресная привязка к каждой отдельной функции или оператору.

2. Нужна точечная досверка строк `Manual` и `Sample` на уровне отдельных имён.
   По крупным группам покрытие очевидно, но для рабочего реестра полезно дойти до режима, где каждая строка ссылается на конкретную страницу manual и конкретную папку в `samples/`.

3. Нужен полуавтоматический аудит на рассинхронизацию.
   Следующий полезный шаг после появления этого реестра — сделать небольшой служебный сценарий, который сравнивает:
   - список visible built-in из `src/Axiom.Semantics/Symbols/Builtins/`,
   - наличие VM-диспетчеризации,
   - наличие sample-папок,
   - наличие manual-страниц или упоминаний в индексе.

4. Нужно поддерживать столбец `Профиль` в живом состоянии.
   Не все функции надо ускорять, но после каждой оптимизационной задачи надо явно переносить сюда новый статус: `горячо`, `не приоритет`, `не мерили`, `закрыто`.

5. Нужно постепенно разукрупнять слишком широкие строки.
   Этот первый выпуск реестра нарочно группирует близкие функции, чтобы быстро получить рабочую картину. По мере работы полезно делить самые крупные строки на более точные, когда именно они становятся предметом отдельной задачи.
