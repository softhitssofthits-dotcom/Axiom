# Матрица функций языка Axiom

Источник: реестр `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.*.cs` (аудит 2026-10-06, commit `2904ee9`).

Всего дескрипторов: **295**. Пользовательских имён: **241**. ID в enum: **285** (282 зарегистрированы; `DateAdd`, `DateDiff`, `DatePart` — осиротевшие плейсхолдеры сериализации, сознательно сохранены).

Покрытие (тесты/samples/manual): все 241 имя имеют тестовые упоминания в `tests/`, папку `samples/functions/<имя>/` (кроме операторных OPEN/CLOSE, у которых есть manual-страницы) и страницы в `docs/manual/`. Синхронизация реестр-VM-manual-samples автоматически проверяется инструментом `src/Axiom.ManualChecks` (команда `audit-bytecode-coverage`). Тонкое покрытие (1-4 упоминания в тестах): ~45 функций файловой подсистемы (DRIVE*, FILEDATE, FREESPACE и т.п.).

Статус исполнения: **все 282 ID исполняются и в Interpreter, и в BytecodeVm** (механическое сравнение диспетчеров; подробности — `docs/audit/LANGUAGE_FUNCTIONS_FULL_AUDIT.md`, разделы 5-6).

Известные дефекты подсистем см. в LANGUAGE_FUNCTIONS_FULL_AUDIT.md (разделы 36-38).

## Полная таблица дескрипторов


### Массивы (Array) — 47 дескрипторов

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| LBOUND | LBound | 1 | ARRAY | INTEGER |
| UBOUND | UBound | 1 | ARRAY | INTEGER |
| ARRAYLENGTH | ArrayLength | 1 | ARRAY | INTEGER |
| ARRAYISALLOCATED | ArrayIsAllocated | 1 | ARRAY | BOOLEAN |
| ARRAYRANK | ArrayRank | 1 | ARRAY | INTEGER |
| ARRAYBOUNDS | ArrayBounds | 1 | ARRAY | ARRAY (ранг 2) |
| ARRAYCLONE | ArrayClone | 1 | ARRAY | ARRAY (как арг.1) |
| ARRAYSLICE | ArraySlice | 3 | ARRAY,INTEGER,INTEGER | ARRAY (как арг.1) |
| ARRAYSLICE | ArraySlice | 4 | ARRAY,INTEGER,INTEGER,INTEGER | ARRAY (как арг.1) |
| ARRAYTOSORTED | ArrayToSorted | 1 | ARRAY | ARRAY (как арг.1) |
| ARRAYTOSORTED | ArrayToSorted | 2 | ARRAY,INTEGER | ARRAY (как арг.1) |
| ARRAYTOREVERSED | ArrayToReversed | 1 | ARRAY | ARRAY (как арг.1) |
| ARRAYTOREVERSED | ArrayToReversed | 2 | ARRAY,INTEGER | ARRAY (как арг.1) |
| ARRAYTAKE | ArrayTake | 2 | ARRAY,INTEGER | ARRAY (как арг.1) |
| ARRAYTAKE | ArrayTake | 3 | ARRAY,INTEGER,INTEGER | ARRAY (как арг.1) |
| ARRAYSKIP | ArraySkip | 2 | ARRAY,INTEGER | ARRAY (как арг.1) |
| ARRAYSKIP | ArraySkip | 3 | ARRAY,INTEGER,INTEGER | ARRAY (как арг.1) |
| ARRAYFLATTEN | ArrayFlatten | 1 | ARRAY | ARRAY (элементы) |
| ARRAYRESHAPE | ArrayReshape | 2 | ARRAY,ARRAY | ARRAY (как арг.2) |
| ARRAYDISTINCT | ArrayDistinct | 1 | ARRAY | ARRAY (элементы) |
| ARRAYCONTAINS | ArrayContains | 2 | ARRAY,INTEGER | BOOLEAN |
| ARRAYINDEXOF | ArrayIndexOf | 2 | ARRAY,INTEGER | INTEGER (координата) |
| ARRAYLASTINDEXOF | ArrayLastIndexOf | 2 | ARRAY,INTEGER | INTEGER (координата) |
| ARRAYCOUNT | ArrayCount | 2 | ARRAY,INTEGER | INTEGER |
| ARRAYFINDALL | ArrayFindAll | 2 | ARRAY,INTEGER | ARRAY (координаты) |
| ARRAYBINARYSEARCH | ArrayBinarySearch | 2 | ARRAY,INTEGER | INTEGER (координата) |
| ARRAYBINARYSEARCH | ArrayBinarySearch | 3 | ARRAY,INTEGER,INTEGER | INTEGER (координата) |
| ARRAYFILL | ArrayFill | 2 | ARRAY,INTEGER | VOID |
| ARRAYREVERSE | ArrayReverse | 1 | ARRAY | VOID |
| ARRAYREVERSE | ArrayReverse | 2 | ARRAY,INTEGER | VOID |
| ARRAYSORT | ArraySort | 1 | ARRAY | VOID |
| ARRAYSORT | ArraySort | 2 | ARRAY,INTEGER | VOID |
| ARRAYPUSH | ArrayPush | 2 | ARRAY,INTEGER | VOID |
| ARRAYPUSH | ArrayPush | 3 | ARRAY,INTEGER,INTEGER | VOID |
| ARRAYPOP | ArrayPop | 1 | ARRAY | INTEGER |
| ARRAYPOP | ArrayPop | 2 | ARRAY,INTEGER | INTEGER |
| ARRAYINSERT | ArrayInsert | 3 | ARRAY,INTEGER,INTEGER | VOID |
| ARRAYINSERT | ArrayInsert | 4 | ARRAY,INTEGER,INTEGER,INTEGER | VOID |
| ARRAYREMOVE | ArrayRemove | 2 | ARRAY,INTEGER | INTEGER |
| ARRAYREMOVE | ArrayRemove | 3 | ARRAY,INTEGER,INTEGER | INTEGER |
| ARRAYEVERY | ArrayEvery | 2 | ARRAY,FUNCTION(INTEGER->BOOLEAN) | BOOLEAN |
| ARRAYSOME | ArraySome | 2 | ARRAY,FUNCTION(INTEGER->BOOLEAN) | BOOLEAN |
| ARRAYFILTER | ArrayFilter | 2 | ARRAY,FUNCTION(INTEGER->BOOLEAN) | ARRAY (элементы) |
| ARRAYMAP | ArrayMap | 2 | ARRAY,FUNCTION(INTEGER->INTEGER) | ARRAY |
| ARRAYREDUCE | ArrayReduce | 3 | ARRAY,INTEGER,FUNCTION(INTEGER,INTEGER->INTEGER) | INTEGER |
| ARRAYCONCAT | ArrayConcat | 2 | ARRAY,ARRAY | ARRAY (как арг.1) |
| ARRAYCONCAT | ArrayConcat | 3 | ARRAY,ARRAY,INTEGER | ARRAY (как арг.1) |


### Ввод и графика (Input/Graphics) — 41

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| INKEY | InKey | 0 | - | STRING |
| KEYDOWN | KeyDown | 1 | STRING | BOOLEAN |
| KEYPRESS | KeyPress | 1 | STRING | BOOLEAN |
| MOUSEX | MouseX | 0 | - | LONG |
| MOUSEY | MouseY | 0 | - | LONG |
| MOUSEBUTTON | MouseButton | 1 | INTEGER | BOOLEAN |
| MOUSEPRESS | MousePress | 1 | INTEGER | BOOLEAN |
| SCREENISACTIVE | ScreenIsActive | 0 | - | BOOLEAN |
| SCREENMODE | ScreenMode | 0 | - | INTEGER |
| SCREENWIDTH | ScreenWidth | 0 | - | INTEGER |
| SCREENHEIGHT | ScreenHeight | 0 | - | INTEGER |
| TEXTWIDTH | TextWidth | 1 | STRING | INTEGER |
| TEXTHEIGHT | TextHeight | 1 | STRING | INTEGER |
| POINT | PointPosition | 1 | INTEGER | INTEGER |
| POINT | Point | 2 | LONG,LONG | INTEGER |
| PMAP | PMap | 2 | DOUBLE,INTEGER | DOUBLE |
| LOADIMAGE | LoadImage | 1 | STRING | IMAGE |
| IMAGEWIDTH | ImageWidth | 1 | IMAGE | INTEGER |
| IMAGEHEIGHT | ImageHeight | 1 | IMAGE | INTEGER |
| LOADSPRITE | LoadSprite | 3 | STRING,LONG,LONG | SPRITE |
| SPRITEWIDTH | SpriteWidth | 1 | SPRITE | INTEGER |
| SPRITEHEIGHT | SpriteHeight | 1 | SPRITE | INTEGER |
| SPRITECOUNT | SpriteCount | 1 | SPRITE | INTEGER |
| LOADTILESET | LoadTileset | 3 | STRING,LONG,LONG | TILESET |
| LOADTILESET | LoadTilesetMargin | 4 | STRING,LONG,LONG,LONG | TILESET |
| LOADTILESET | LoadTilesetMarginSpacing | 5 | STRING,LONG,LONG,LONG,LONG | TILESET |
| LOADTILEMAP | LoadTilemap | 1 | STRING | TILEMAP |
| TILECOUNT | TileCount | 1 | TILESET | INTEGER |
| TILEWIDTH | TileWidth | 1 | TILESET | INTEGER |
| TILEHEIGHT | TileHeight | 1 | TILESET | INTEGER |
| MAPWIDTH | MapWidth | 1 | TILEMAP | INTEGER |
| MAPHEIGHT | MapHeight | 1 | TILEMAP | INTEGER |
| MAPPIXELWIDTH | MapPixelWidth | 1 | TILEMAP | INTEGER |
| MAPPIXELHEIGHT | MapPixelHeight | 1 | TILEMAP | INTEGER |
| MAPTILEWIDTH | MapTileWidth | 1 | TILEMAP | INTEGER |
| MAPTILEHEIGHT | MapTileHeight | 1 | TILEMAP | INTEGER |
| MAPLAYERCOUNT | MapLayerCount | 1 | TILEMAP | INTEGER |
| MAPTILE | MapTile | 4 | TILEMAP,LONG,LONG,LONG | INTEGER |
| MAPTILESET | MapTileset | 4 | TILEMAP,LONG,LONG,LONG | INTEGER |
| MAPTILEFLAGS | MapTileFlags | 4 | TILEMAP,LONG,LONG,LONG | INTEGER |
| RGB | Rgb | 3 | LONG,LONG,LONG | INTEGER |


### Строки (String) — 42

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| LEN | Len | 1 | STRING | INTEGER |
| LEFT | Left | 2 | STRING,INTEGER | STRING |
| RIGHT | Right | 2 | STRING,INTEGER | STRING |
| MID | Mid | 3 | STRING,INTEGER,INTEGER | STRING |
| LCASE | LCase | 1 | STRING | STRING |
| UCASE | UCase | 1 | STRING | STRING |
| REVERSE | Reverse | 1 | STRING | STRING |
| ASC | Asc | 1 | STRING | INTEGER |
| ASCW | AscW | 1 | STRING | INTEGER |
| CHR | Chr | 1 | INTEGER | STRING |
| CHRW | ChrW | 1 | INTEGER | STRING |
| TRIM | TrimWhitespace | 1 | STRING | STRING |
| TRIM | TrimCharacters | 2 | STRING,STRING | STRING |
| LTRIM | LTrimWhitespace | 1 | STRING | STRING |
| LTRIM | LTrimCharacters | 2 | STRING,STRING | STRING |
| RTRIM | RTrimWhitespace | 1 | STRING | STRING |
| RTRIM | RTrimCharacters | 2 | STRING,STRING | STRING |
| CONTAINS | Contains | 2 | STRING,STRING | BOOLEAN |
| STARTSWITH | StartsWith | 2 | STRING,STRING | BOOLEAN |
| ENDSWITH | EndsWith | 2 | STRING,STRING | BOOLEAN |
| REPLACE | Replace | 3 | STRING,STRING,STRING | STRING |
| REPLACEMANY | ReplaceMany | 3 | STRING,ARRAY,ARRAY | STRING |
| INSTR | InStr | 2 | STRING,STRING | INTEGER |
| INSTR | InStrFrom | 3 | INTEGER,STRING,STRING | INTEGER |
| INSTRREV | InStrRev | 2 | STRING,STRING | INTEGER |
| INSTRREV | InStrRevFrom | 3 | INTEGER,STRING,STRING | INTEGER |
| SUBSTRCOUNT | SubStrCount | 2 | STRING,STRING | INTEGER |
| REPEAT | Repeat | 2 | STRING,INTEGER | STRING |
| SPACE | Space | 1 | INTEGER | STRING |
| STRING | StringFill | 2 | INTEGER,STRING | STRING |
| PADLEFT | PadLeft | 2 | STRING,INTEGER | STRING |
| PADLEFT | PadLeftFill | 3 | STRING,INTEGER,STRING | STRING |
| PADRIGHT | PadRight | 2 | STRING,INTEGER | STRING |
| PADRIGHT | PadRightFill | 3 | STRING,INTEGER,STRING | STRING |
| CENTER | Center | 2 | STRING,INTEGER | STRING |
| CENTER | CenterFill | 3 | STRING,INTEGER,STRING | STRING |
| INSERT | Insert | 3 | STRING,INTEGER,STRING | STRING |
| REMOVE | Remove | 3 | STRING,INTEGER,INTEGER | STRING |
| SPLIT | Split | 1 | STRING | ARRAY |
| SPLIT | SplitSeparator | 2 | STRING,STRING | ARRAY |
| JOIN | Join | 1 | ARRAY | STRING |
| JOIN | JoinSeparator | 2 | ARRAY,STRING | STRING |


### Регулярные выражения (Regex) — 18

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| REGEXISMATCH | RegexIsMatch | 2 | STRING,STRING | BOOLEAN |
| REGEXISMATCH | RegexIsMatchModes | 3 | STRING,STRING,STRING | BOOLEAN |
| REGEXMATCH | RegexMatch | 2 | STRING,STRING | STRING |
| REGEXMATCH | RegexMatchModes | 3 | STRING,STRING,STRING | STRING |
| REGEXEXTRACT | RegexExtract | 3 | STRING,STRING,INTEGER | STRING |
| REGEXEXTRACT | RegexExtractModes | 4 | STRING,STRING,INTEGER,STRING | STRING |
| REGEXMATCHES | RegexMatches | 2 | STRING,STRING | ARRAY |
| REGEXMATCHES | RegexMatchesModes | 3 | STRING,STRING,STRING | ARRAY |
| REGEXEXTRACTALL | RegexExtractAll | 3 | STRING,STRING,INTEGER | ARRAY |
| REGEXEXTRACTALL | RegexExtractAllModes | 4 | STRING,STRING,INTEGER,STRING | ARRAY |
| REGEXSPLIT | RegexSplit | 2 | STRING,STRING | ARRAY |
| REGEXSPLIT | RegexSplitModes | 3 | STRING,STRING,STRING | ARRAY |
| REGEXREPLACE | RegexReplace | 3 | STRING,STRING,STRING | STRING |
| REGEXREPLACE | RegexReplaceModes | 4 | STRING,STRING,STRING,STRING | STRING |
| REGEXREPLACE | RegexReplaceCount | 4 | STRING,STRING,STRING,INTEGER | STRING |
| REGEXREPLACE | RegexReplaceModesCount | 5 | STRING,STRING,STRING,STRING,INTEGER | STRING |
| REGEXCOUNT | RegexCount | 2 | STRING,STRING | INTEGER |
| REGEXCOUNT | RegexCountModes | 3 | STRING,STRING,STRING | INTEGER |


### Дата и время (DateTime) — 46

Голые ID DateAdd/DateDiff/DatePart не регистрируются (плейсхолдеры сериализации); строковые формы удалены.

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| DATE | Date | 0 | - | DATE |
| DATE | DateInZone | 1 | STRING | DATE |
| TIME | Time | 0 | - | TIME |
| TIME | TimeInZone | 1 | STRING | TIME |
| NOW | Now | 0 | - | DATETIME |
| NOW | NowInZone | 1 | STRING | DATETIME |
| UTCDATE | UtcDate | 0 | - | DATE |
| UTCTIME | UtcTime | 0 | - | TIME |
| UTCNOW | UtcNow | 0 | - | DATETIME |
| TIMER | Timer | 0 | - | DOUBLE |
| DATESERIAL | DateSerial | 3 | INTEGER,INTEGER,INTEGER | DATE |
| TIMESERIAL | TimeSerial | 3 | INTEGER,INTEGER,INTEGER | TIME |
| YEAR | Year | 1 | DATE | INTEGER |
| MONTH | Month | 1 | DATE | INTEGER |
| MONTHNAME | MonthName | 1 | INTEGER | STRING |
| DAY | Day | 1 | DATE | INTEGER |
| HOUR | Hour | 1 | TIME | INTEGER |
| MINUTE | Minute | 1 | TIME | INTEGER |
| SECOND | Second | 1 | TIME | INTEGER |
| WEEKDAY | Weekday | 1 | DATE | INTEGER |
| WEEKDAYNAME | WeekdayName | 1 | INTEGER | STRING |
| DATEADD | DateAddDateValue | 3 | STRING,INTEGER,DATE | DATE |
| DATEADD | DateAddTimeValue | 3 | STRING,INTEGER,TIME | TIME |
| DATEADD | DateAddDateTimeValue | 3 | STRING,INTEGER,DATETIME | DATETIME |
| DATEDIFF | DateDiffDateValues | 3 | STRING,DATE,DATE | LONG |
| DATEDIFF | DateDiffTimeValues | 3 | STRING,TIME,TIME | LONG |
| DATEDIFF | DateDiffDateTimeValues | 3 | STRING,DATETIME,DATETIME | LONG |
| DATEPART | DatePartDateValue | 2 | STRING,DATE | INTEGER |
| DATEPART | DatePartTimeValue | 2 | STRING,TIME | INTEGER |
| DATEPART | DatePartDateTimeValue | 2 | STRING,DATETIME | INTEGER |
| DATEVALUE | DateValue | 1 | STRING | DATE |
| TIMEVALUE | TimeValue | 1 | STRING | TIME |
| ISDATE | IsDate | 1 | STRING | BOOLEAN |
| UTCOFFSET | UtcOffset | 0 | - | INTEGER (минуты) |
| UTCOFFSET | UtcOffsetValue | 1 | DATETIME | INTEGER |
| UTCOFFSET | UtcOffsetValueZone | 2 | DATETIME,STRING | INTEGER |
| TOUTC | ToUtc | 1 | DATETIME | DATETIME |
| TOUTC | ToUtcZone | 2 | DATETIME,STRING | DATETIME |
| FROMUTC | FromUtc | 1 | DATETIME | DATETIME |
| FROMUTC | FromUtcZone | 2 | DATETIME,STRING | DATETIME |
| ISLEAPYEAR | IsLeapYear | 1 | INTEGER | BOOLEAN |
| DAYSINMONTH | DaysInMonth | 2 | INTEGER,INTEGER | INTEGER |
| STARTOFMONTH | StartOfMonth | 1 | DATE | DATE |
| ENDOFMONTH | EndOfMonth | 1 | DATE | DATE |
| STARTOFYEAR | StartOfYear | 1 | DATE | DATE |
| ENDOFYEAR | EndOfYear | 1 | DATE | DATE |


### Файловые дескрипторы (FileIO) — 18

Плюс операторы OPEN/CLOSE (не входят в реестр функций).

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| ISOPEN | IsOpen | 1 | FILE | BOOLEAN |
| FILEPATH | FilePath | 1 | FILE | STRING |
| FILEMODE | FileMode | 1 | FILE | STRING |
| FILEPOS | FilePos | 1 | FILE | LONG |
| FILELENGTH | FileLength | 1 | FILE | LONG |
| LINENUMBER | LineNumber | 1 | FILE | LONG |
| EOF | Eof | 1 | FILE | BOOLEAN |
| READTOEND | ReadToEnd | 1 | FILE | STRING |
| READTEXT | ReadText | 2 | FILE,INTEGER | STRING |
| READLINE | ReadLine | 1 | FILE | STRING |
| READLINES | ReadLines | 2 | FILE,INTEGER | ARRAY |
| SKIPLINE | SkipLine | 1 | FILE | BOOLEAN |
| WRITE | Write | 2 | FILE,STRING | BOOLEAN |
| WRITELINE | WriteLine | 2 | FILE,STRING | BOOLEAN |
| NEWLINE | NewLine | 1 | FILE | BOOLEAN |
| FLUSH | Flush | 1 | FILE | BOOLEAN |
| SEEKFILE | SeekFile | 2 | FILE,LONG | BOOLEAN |
| REWIND | Rewind | 1 | FILE | BOOLEAN |


### Файловая система (FileSystem) — 49

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| FILEEXISTS | FileExists | 1 | STRING | BOOLEAN |
| DIREXISTS | DirExists | 1 | STRING | BOOLEAN |
| PATHEXISTS | PathExists | 1 | STRING | BOOLEAN |
| READFILE | ReadFile | 1 | STRING | STRING |
| READALLLINES | ReadAllLines | 1 | STRING | ARRAY |
| WRITEFILE | WriteFile | 2 | STRING,STRING | BOOLEAN |
| APPENDFILE | AppendFile | 2 | STRING,STRING | BOOLEAN |
| FILESIZE | FileSize | 1 | STRING | LONG |
| FILEDATE | FileDate | 1 | STRING | STRING |
| FILECREATEDATE | FileCreateDate | 1 | STRING | STRING |
| FILEACCESSEDDATE | FileAccessDate | 1 | STRING | STRING |
| DIRDATE | DirDate | 1 | STRING | STRING |
| GETFILES | GetFiles | 1 | STRING | ARRAY |
| GETFILES | GetFilesPattern | 2 | STRING,STRING | ARRAY |
| GETDIRS | GetDirs | 1 | STRING | ARRAY |
| GETDIRS | GetDirsPattern | 2 | STRING,STRING | ARRAY |
| COMBINEPATH | CombinePath | 2 | STRING,STRING | STRING |
| FILENAME | FileName | 1 | STRING | STRING |
| DIRNAME | DirName | 1 | STRING | STRING |
| EXTNAME | ExtName | 1 | STRING | STRING |
| FULLPATH | FullPath | 1 | STRING | STRING |
| PATHROOT | PathRoot | 1 | STRING | STRING |
| ISABSOLUTEPATH | IsAbsolutePath | 1 | STRING | BOOLEAN |
| MAKEDIR | MakeDir | 1 | STRING | BOOLEAN |
| DELETEFILE | DeleteFile | 1 | STRING | BOOLEAN |
| COPYFILE | CopyFile | 2 | STRING,STRING | BOOLEAN |
| COPYFILE | CopyFileOverwrite | 3 | STRING,STRING,BOOLEAN | BOOLEAN |
| MOVEFILE | MoveFile | 2 | STRING,STRING | BOOLEAN |
| MOVEFILE | MoveFileOverwrite | 3 | STRING,STRING,BOOLEAN | BOOLEAN |
| RENAMEFILE | RenameFile | 2 | STRING,STRING | BOOLEAN |
| DELETEDIR | DeleteDir | 1 | STRING | BOOLEAN |
| DELETEDIRRECURSIVE | DeleteDirRecursive | 1 | STRING | BOOLEAN |
| COPYDIR | CopyDir | 2 | STRING,STRING | BOOLEAN |
| COPYDIR | CopyDirOverwrite | 3 | STRING,STRING,BOOLEAN | BOOLEAN |
| MOVEDIR | MoveDir | 2 | STRING,STRING | BOOLEAN |
| RENAMEDIR | RenameDir | 2 | STRING,STRING | BOOLEAN |
| CURRENTDIR | CurrentDir | 0 | - | STRING |
| CHANGEDIR | ChangeDir | 1 | STRING | BOOLEAN |
| DIRSIZE | DirSize | 1 | STRING | LONG |
| FREESPACE | FreeSpace | 1 | STRING | LONG |
| TOTALFREESPACE | TotalFreeSpace | 1 | STRING | LONG |
| TOTALSPACE | TotalSpace | 1 | STRING | LONG |
| ISREADONLY | IsReadOnly | 1 | STRING | BOOLEAN |
| ISHIDDEN | IsHidden | 1 | STRING | BOOLEAN |
| DRIVENAME | DriveName | 1 | STRING | STRING |
| DRIVEFORMAT | DriveFormat | 1 | STRING | STRING |
| DRIVETYPE | DriveType | 1 | STRING | STRING |
| MAKETEMPFILE | MakeTempFile | 1 | STRING | STRING |
| MAKETEMPDIR | MakeTempDir | 1 | STRING | STRING |


### Преобразования (Conversion) — 3

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| INT | Int | 1 | DOUBLE | INTEGER |
| STR | Str | 1 | DOUBLE | STRING |
| VAL | Val | 1 | STRING | DOUBLE |


### Числовые (Numeric) — 31

| Имя | BuiltinFunctionId | Арность | Параметры | Результат |
|---|---|---|---|---|
| RND | Rnd | 0 | - | DOUBLE |
| ABS | Abs | 1 | DOUBLE | DOUBLE (как арг.) |
| SGN | Sgn | 1 | DOUBLE | INTEGER |
| MIN | Min | 2 | DOUBLE,DOUBLE | DOUBLE (promoted) |
| MAX | Max | 2 | DOUBLE,DOUBLE | DOUBLE (promoted) |
| CLAMP | Clamp | 3 | DOUBLE,DOUBLE,DOUBLE | DOUBLE (promoted) |
| SIN | Sin | 1 | DOUBLE | DOUBLE |
| COS | Cos | 1 | DOUBLE | DOUBLE |
| TAN | Tan | 1 | DOUBLE | DOUBLE |
| ATN | Atn | 1 | DOUBLE | DOUBLE |
| ATN2 | Atn2 | 2 | DOUBLE,DOUBLE | DOUBLE |
| SQR | Sqr | 1 | DOUBLE | DOUBLE |
| CBRT | Cbrt | 1 | DOUBLE | DOUBLE |
| EXP | Exp | 1 | DOUBLE | DOUBLE |
| LOG | Log | 1 | DOUBLE | DOUBLE |
| LOG10 | Log10 | 1 | DOUBLE | DOUBLE |
| LOG2 | Log2 | 1 | DOUBLE | DOUBLE |
| POW | Pow | 2 | DOUBLE,DOUBLE | DOUBLE |
| FLOOR | Floor | 1 | DOUBLE | DOUBLE (как арг.) |
| CEIL | Ceil | 1 | DOUBLE | DOUBLE (как арг.) |
| ROUND | Round | 1 | DOUBLE | DOUBLE (как арг.) |
| FIX | Fix | 1 | DOUBLE | DOUBLE (как арг.) |
| ASIN | Asin | 1 | DOUBLE | DOUBLE |
| ACOS | Acos | 1 | DOUBLE | DOUBLE |
| ASINH | Asinh | 1 | DOUBLE | DOUBLE |
| ACOSH | Acosh | 1 | DOUBLE | DOUBLE |
| ATANH | Atanh | 1 | DOUBLE | DOUBLE |
| SINH | Sinh | 1 | DOUBLE | DOUBLE |
| COSH | Cosh | 1 | DOUBLE | DOUBLE |
| TANH | Tanh | 1 | DOUBLE | DOUBLE |
| SETSIGN | SetSign | 2 | DOUBLE,DOUBLE | DOUBLE (как арг.) |
