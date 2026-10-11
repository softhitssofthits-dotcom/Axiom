# Матрица функций языка Axiom

Источник: реестр `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.*.cs` (аудит 2026-10-06, commit `2904ee9`).

Всего дескрипторов: **295**. Пользовательских имён: **241**. ID в enum: **285** (282 зарегистрированы; `DateAdd`, `DateDiff`, `DatePart` — осиротевшие плейсхолдеры сериализации, сознательно сохранены).

Покрытие (тесты/samples/manual): все 241 имя имеют тестовые упоминания в `tests/`, папку `samples/functions/<имя>/` (кроме операторных OPEN/CLOSE, у которых есть manual-страницы) и страницы в `docs/manual/`. Синхронизация реестр-VM-manual-samples автоматически проверяется инструментом `src/Axiom.ManualChecks` (команда `audit-bytecode-coverage`). Тонкое покрытие (1-4 упоминания в тестах): ~45 функций файловой подсистемы (DRIVE*, FILEDATE, FREESPACE и т.п.).

Статус исполнения: **все 282 ID исполняются и в Interpreter, и в BytecodeVm** (механическое сравнение диспетчеров; подробности — `docs/archive/audits-through-20261011/LANGUAGE_FUNCTIONS_FULL_AUDIT.md`, разделы 5-6).

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


# Независимая матрица реестра — 2026-10-07

В актуальном CSV отдельное поле `Status` отмечает подтверждённые дефектные семейства как `BROKEN` со ссылками LA26-001…014; остальные остаются `PARTIAL`. Непроверенные семантические клетки каждой перегрузки сохранены явно. Помимо 303 дескрипторов, CSV содержит две формы `Origin=binder-synthetic`: `LBOUND(array, dimension)` и `UBOUND(array, dimension)`. Они проверены сценарием `bound-dimension`, но отсутствуют в загруженном registry и CLI metadata; к числу 303 дескрипторов не прибавляются.

Получена из загруженного реестра, включая перегрузки. Тип результата при специальной политике определяется связывателем; объявленный тип не является окончательным. Упоминания в тестах, примерах и справочнике — карта поиска, не доказательство выполнения. Итоговые статусы подтверждённых дефектных семейств и непроверенные клетки находятся в актуальном CSV и реестре LA26-001…014.

Дескрипторов: 303; имён: 249; ID: 293; незарегистрированные ID: DateAdd, DateDiff, DatePart.

| Имя | ID | Параметры | Объявленный результат | Связывание / операнды / результат | Interpreter / VM | Тесты / примеры / manual | Подсветка светлая / тёмная |
|---|---|---|---|---|---|---|---|
| LBOUND | LBound | array: INTEGER() | INTEGER | Array / Declared / Declared | 1 файлов / True | 9 / 55 / 9 | True / True |
| UBOUND | UBound | array: INTEGER() | INTEGER | Array / Declared / Declared | 1 файлов / True | 9 / 56 / 10 | True / True |
| ARRAYLENGTH | ArrayLength | array: INTEGER() | INTEGER | Array / Declared / Declared | 1 файлов / True | 8 / 51 / 4 | True / True |
| ARRAYISALLOCATED | ArrayIsAllocated | array: INTEGER() | BOOLEAN | Array / Declared / Declared | 1 файлов / True | 7 / 14 / 4 | True / True |
| ARRAYRANK | ArrayRank | array: INTEGER() | INTEGER | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 4 | True / True |
| ARRAYBOUNDS | ArrayBounds | array: INTEGER() | INTEGER(,) | Array / Declared / Declared | 1 файлов / True | 6 / 2 / 5 | True / True |
| ARRAYCLONE | ArrayClone | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 5 / 2 | True / True |
| ARRAYSLICE | ArraySlice | array: INTEGER(), start: INTEGER, count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 4 / 4 | True / True |
| ARRAYSLICE | ArraySlice | array: INTEGER(), start: INTEGER, count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 4 / 4 | True / True |
| ARRAYTOSORTED | ArrayToSorted | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 5 / 1 | True / True |
| ARRAYTOSORTED | ArrayToSorted | array: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 5 / 1 | True / True |
| ARRAYTOREVERSED | ArrayToReversed | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYTOREVERSED | ArrayToReversed | array: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYTAKE | ArrayTake | array: INTEGER(), count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 6 / 1 | True / True |
| ARRAYTAKE | ArrayTake | array: INTEGER(), count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 6 / 1 | True / True |
| ARRAYSKIP | ArraySkip | array: INTEGER(), count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 5 / 1 | True / True |
| ARRAYSKIP | ArraySkip | array: INTEGER(), count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 4 / 5 / 1 | True / True |
| ARRAYFLATTEN | ArrayFlatten | array: INTEGER() | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYRESHAPE | ArrayReshape | source: INTEGER(), template: INTEGER() | INTEGER() | Array / Declared / SameAsSecondArgument | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYDISTINCT | ArrayDistinct | array: INTEGER() | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYCONTAINS | ArrayContains | array: INTEGER(), value: INTEGER | BOOLEAN | Array / Declared / Declared | 1 файлов / True | 5 / 5 / 1 | True / True |
| ARRAYINDEXOF | ArrayIndexOf | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYLASTINDEXOF | ArrayLastIndexOf | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYCOUNT | ArrayCount | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYFINDALL | ArrayFindAll | array: INTEGER(), value: INTEGER | INTEGER() | Array / Declared / ArrayFindAllCoordinateResult | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYBINARYSEARCH | ArrayBinarySearch | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYBINARYSEARCH | ArrayBinarySearch | array: INTEGER(), value: INTEGER, dimension: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYFILL | ArrayFill | array: INTEGER(), value: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 3 / 2 / 1 | True / True |
| ARRAYREVERSE | ArrayReverse | array: INTEGER() | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYREVERSE | ArrayReverse | array: INTEGER(), dimension: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 3 / 1 | True / True |
| ARRAYSORT | ArraySort | array: INTEGER() | VOID | Array / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| ARRAYSORT | ArraySort | array: INTEGER(), dimension: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| ARRAYPUSH | ArrayPush | array: INTEGER(), value: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 5 / 5 | True / True |
| ARRAYPUSH | ArrayPush | array: INTEGER(), value: INTEGER, dimension: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 5 / 5 | True / True |
| ARRAYPOP | ArrayPop | array: INTEGER() | INTEGER | Array / Declared / ArrayDynamicSliceResult | 1 файлов / True | 4 / 5 / 4 | True / True |
| ARRAYPOP | ArrayPop | array: INTEGER(), dimension: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | 1 файлов / True | 4 / 5 / 4 | True / True |
| ARRAYINSERT | ArrayInsert | array: INTEGER(), index: INTEGER, value: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 5 | True / True |
| ARRAYINSERT | ArrayInsert | array: INTEGER(), index: INTEGER, value: INTEGER, dimension: INTEGER | VOID | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 5 | True / True |
| ARRAYREMOVE | ArrayRemove | array: INTEGER(), index: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | 1 файлов / True | 4 / 4 / 4 | True / True |
| ARRAYREMOVE | ArrayRemove | array: INTEGER(), index: INTEGER, dimension: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | 1 файлов / True | 4 / 4 / 4 | True / True |
| ARRAYEVERY | ArrayEvery | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | BOOLEAN | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYSOME | ArraySome | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | BOOLEAN | Array / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| ARRAYFILTER | ArrayFilter | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | 1 файлов / True | 5 / 4 / 1 | True / True |
| ARRAYMAP | ArrayMap | array: INTEGER(), selector: FUNCTION(INTEGER) AS INTEGER | INTEGER() | Array / Declared / ArrayMapSelectorReturnArray | 1 файлов / True | 5 / 6 / 1 | True / True |
| ARRAYREDUCE | ArrayReduce | array: INTEGER(), seed: INTEGER, reducer: FUNCTION(INTEGER, INTEGER) AS INTEGER | INTEGER | Array / Declared / ArrayReduceAccumulator | 1 файлов / True | 4 / 6 / 1 | True / True |
| ARRAYCONCAT | ArrayConcat | left: INTEGER(), right: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 5 / 1 | True / True |
| ARRAYCONCAT | ArrayConcat | left: INTEGER(), right: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 3 / 5 / 1 | True / True |
| ARRAYSUM | ArraySum | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 2 / 3 / 1 | True / True |
| ARRAYMIN | ArrayMin | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 2 / 1 / 1 | True / True |
| ARRAYMAX | ArrayMax | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | 1 файлов / True | 2 / 1 / 1 | True / True |
| ARRAYAVERAGE | ArrayAverage | array: INTEGER() | DOUBLE | Array / Declared / Double | 1 файлов / True | 2 / 2 / 1 | True / True |
| INKEY | InKey |  | STRING | Default / Declared / Declared | 1 файлов / True | 6 / 8 / 8 | True / True |
| KEYDOWN | KeyDown | keyText: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 6 / 5 / 8 | True / True |
| KEYPRESS | KeyPress | keyText: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 26 / 7 | True / True |
| MOUSEX | MouseX |  | LONG | Default / Declared / Declared | 1 файлов / True | 9 / 5 / 7 | True / True |
| MOUSEY | MouseY |  | LONG | Default / Declared / Declared | 1 файлов / True | 9 / 5 / 7 | True / True |
| MOUSEBUTTON | MouseButton | buttonNumber: INTEGER | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 7 / 2 / 7 | True / True |
| MOUSEPRESS | MousePress | buttonNumber: INTEGER | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 1 / 7 | True / True |
| SCREENISACTIVE | ScreenIsActive |  | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 1 / 2 / 1 | True / True |
| SCREENMODE | ScreenMode |  | INTEGER | Default / Declared / Declared | 1 файлов / True | 8 / 2 / 1 | True / True |
| SCREENWIDTH | ScreenWidth |  | INTEGER | Default / Declared / Declared | 1 файлов / True | 9 / 5 / 3 | True / True |
| SCREENHEIGHT | ScreenHeight |  | INTEGER | Default / Declared / Declared | 1 файлов / True | 9 / 5 / 3 | True / True |
| TEXTWIDTH | TextWidth | text: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| TEXTHEIGHT | TextHeight | text: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| POINT | PointPosition | number: INTEGER | INTEGER | Default / Declared / Declared | 1 файлов / True | 11 / 16 / 10 | True / True |
| POINT | Point | x: LONG, y: LONG | INTEGER | Default / Declared / Declared | 1 файлов / True | 11 / 16 / 10 | True / True |
| PMAP | PMap | coordinate: DOUBLE, selector: INTEGER | DOUBLE | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 9 | True / True |
| LOADIMAGE | LoadImage | path: STRING | IMAGE | Default / Declared / Declared | 1 файлов / True | 8 / 7 / 2 | True / True |
| IMAGEWIDTH | ImageWidth | image: IMAGE | INTEGER | Default / Declared / Declared | 1 файлов / True | 6 / 3 / 2 | True / True |
| IMAGEHEIGHT | ImageHeight | image: IMAGE | INTEGER | Default / Declared / Declared | 1 файлов / True | 6 / 2 / 2 | True / True |
| LOADSPRITE | LoadSprite | path: STRING, frameWidth: LONG, frameHeight: LONG | SPRITE | Default / Declared / Declared | 1 файлов / True | 8 / 8 / 2 | True / True |
| SPRITEWIDTH | SpriteWidth | sprite: SPRITE | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 1 / 2 | True / True |
| SPRITEHEIGHT | SpriteHeight | sprite: SPRITE | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 1 / 2 | True / True |
| SPRITECOUNT | SpriteCount | sprite: SPRITE | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 2 | True / True |
| LOADTILESET | LoadTileset | path: STRING, tileWidth: LONG, tileHeight: LONG | TILESET | Default / Declared / Declared | 1 файлов / True | 6 / 1 / 2 | True / True |
| LOADTILESET | LoadTilesetMargin | path: STRING, tileWidth: LONG, tileHeight: LONG, margin: LONG | TILESET | Default / Declared / Declared | 1 файлов / True | 6 / 1 / 2 | True / True |
| LOADTILESET | LoadTilesetMarginSpacing | path: STRING, tileWidth: LONG, tileHeight: LONG, margin: LONG, spacing: LONG | TILESET | Default / Declared / Declared | 1 файлов / True | 6 / 1 / 2 | True / True |
| LOADTILEMAP | LoadTilemap | path: STRING | TILEMAP | Default / Declared / Declared | 1 файлов / True | 7 / 2 / 2 | True / True |
| TILECOUNT | TileCount | tileset: TILESET | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 0 / 2 | True / True |
| TILEWIDTH | TileWidth | tileset: TILESET | INTEGER | Default / Declared / Declared | 1 файлов / True | 7 / 0 / 2 | True / True |
| TILEHEIGHT | TileHeight | tileset: TILESET | INTEGER | Default / Declared / Declared | 1 файлов / True | 7 / 0 / 2 | True / True |
| MAPWIDTH | MapWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 1 / 2 | True / True |
| MAPHEIGHT | MapHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| MAPPIXELWIDTH | MapPixelWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 0 / 2 | True / True |
| MAPPIXELHEIGHT | MapPixelHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 0 / 2 | True / True |
| MAPTILEWIDTH | MapTileWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 0 / 2 | True / True |
| MAPTILEHEIGHT | MapTileHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 0 / 2 | True / True |
| MAPLAYERCOUNT | MapLayerCount | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| MAPTILE | MapTile | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 1 / 4 | True / True |
| MAPTILESET | MapTileset | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 0 / 4 | True / True |
| MAPTILEFLAGS | MapTileFlags | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 0 / 4 | True / True |
| RGB | Rgb | red: LONG, green: LONG, blue: LONG | INTEGER | Default / Declared / Declared | 1 файлов / True | 7 / 25 / 7 | True / True |
| LEN | Len | text: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 20 / 34 / 5 | True / True |
| LEFT | Left | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 22 / 41 / 19 | True / True |
| RIGHT | Right | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 20 / 27 / 13 | True / True |
| MID | Mid | text: STRING, start: INTEGER, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 7 / 11 / 4 | True / True |
| LCASE | LCase | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 5 / 4 | True / True |
| UCASE | UCase | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 4 | True / True |
| REVERSE | Reverse | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 2 / 3 | True / True |
| ASC | Asc | text: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 7 / 6 | True / True |
| ASCW | AscW | text: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 6 / 4 / 6 | True / True |
| CHR | Chr | code: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 6 / 28 / 7 | True / True |
| CHRW | ChrW | code: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 6 / 4 / 6 | True / True |
| TRIM | TrimWhitespace | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 18 / 5 | True / True |
| TRIM | TrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 18 / 5 | True / True |
| LTRIM | LTrimWhitespace | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 2 | True / True |
| LTRIM | LTrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 2 | True / True |
| RTRIM | RTrimWhitespace | text: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 2 | True / True |
| RTRIM | RTrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 2 | True / True |
| CONTAINS | Contains | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 36 / 6 / 2 | True / True |
| STARTSWITH | StartsWith | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 10 / 3 / 2 | True / True |
| ENDSWITH | EndsWith | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 7 / 2 / 2 | True / True |
| REPLACE | Replace | text: STRING, oldText: STRING, newText: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 10 / 7 / 10 | True / True |
| REPLACEMANY | ReplaceMany | text: STRING, oldItems: STRING(), newItems: STRING() | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 2 | True / True |
| INSTR | InStr | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| INSTR | InStrFrom | start: INTEGER, text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| INSTRREV | InStrRev | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 3 / 5 / 2 | True / True |
| INSTRREV | InStrRevFrom | start: INTEGER, text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 3 / 5 / 2 | True / True |
| SUBSTRCOUNT | SubStrCount | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 3 / 4 / 2 | True / True |
| REPEAT | Repeat | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 2 | True / True |
| SPACE | Space | count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 7 / 7 / 3 | True / True |
| STRING | StringFill | count: INTEGER, fill: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 54 / 172 / 52 | True / True |
| PADLEFT | PadLeft | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 2 | True / True |
| PADLEFT | PadLeftFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 2 | True / True |
| PADRIGHT | PadRight | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 2 | True / True |
| PADRIGHT | PadRightFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 2 | True / True |
| CENTER | Center | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 3 / 2 | True / True |
| CENTER | CenterFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 3 / 2 | True / True |
| INSERT | Insert | text: STRING, start: INTEGER, fragment: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 7 / 5 / 3 | True / True |
| REMOVE | Remove | text: STRING, start: INTEGER, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 9 / 4 / 2 | True / True |
| SPLIT | Split | text: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 8 / 7 / 10 | True / True |
| SPLIT | SplitSeparator | text: STRING, separator: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 8 / 7 / 10 | True / True |
| JOIN | Join | items: STRING() | STRING | Default / Declared / Declared | 1 файлов / True | 17 / 8 / 2 | True / True |
| JOIN | JoinSeparator | items: STRING(), separator: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 17 / 8 / 2 | True / True |
| REGEXISMATCH | RegexIsMatch | text: STRING, pattern: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 2 | True / True |
| REGEXISMATCH | RegexIsMatchModes | text: STRING, pattern: STRING, modes: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 2 | True / True |
| REGEXMATCH | RegexMatch | text: STRING, pattern: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 1 | True / True |
| REGEXMATCH | RegexMatchModes | text: STRING, pattern: STRING, modes: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 1 | True / True |
| REGEXEXTRACT | RegexExtract | text: STRING, pattern: STRING, group: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| REGEXEXTRACT | RegexExtractModes | text: STRING, pattern: STRING, group: INTEGER, modes: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 1 / 2 | True / True |
| REGEXMATCHES | RegexMatches | text: STRING, pattern: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 2 | True / True |
| REGEXMATCHES | RegexMatchesModes | text: STRING, pattern: STRING, modes: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 2 | True / True |
| REGEXEXTRACTALL | RegexExtractAll | text: STRING, pattern: STRING, group: INTEGER | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 2 | True / True |
| REGEXEXTRACTALL | RegexExtractAllModes | text: STRING, pattern: STRING, group: INTEGER, modes: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 2 | True / True |
| REGEXSPLIT | RegexSplit | text: STRING, pattern: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 2 | True / True |
| REGEXSPLIT | RegexSplitModes | text: STRING, pattern: STRING, modes: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 2 | True / True |
| REGEXREPLACE | RegexReplace | text: STRING, pattern: STRING, replacement: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| REGEXREPLACE | RegexReplaceModes | text: STRING, pattern: STRING, replacement: STRING, modes: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| REGEXREPLACE | RegexReplaceCount | text: STRING, pattern: STRING, replacement: STRING, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| REGEXREPLACE | RegexReplaceModesCount | text: STRING, pattern: STRING, replacement: STRING, modes: STRING, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| REGEXCOUNT | RegexCount | text: STRING, pattern: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 2 | True / True |
| REGEXCOUNT | RegexCountModes | text: STRING, pattern: STRING, modes: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 2 | True / True |
| DATE | Date |  | DATE | Default / Declared / Declared | 1 файлов / True | 7 / 7 / 12 | True / True |
| DATE | DateInZone | timeZone: STRING | DATE | Default / Declared / Declared | 1 файлов / True | 7 / 7 / 12 | True / True |
| TIME | Time |  | TIME | Default / Declared / Declared | 1 файлов / True | 7 / 9 / 12 | True / True |
| TIME | TimeInZone | timeZone: STRING | TIME | Default / Declared / Declared | 1 файлов / True | 7 / 9 / 12 | True / True |
| NOW | Now |  | DATETIME | Default / Declared / Declared | 1 файлов / True | 7 / 4 / 2 | True / True |
| NOW | NowInZone | timeZone: STRING | DATETIME | Default / Declared / Declared | 1 файлов / True | 7 / 4 / 2 | True / True |
| UTCDATE | UtcDate |  | DATE | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 1 | True / True |
| UTCTIME | UtcTime |  | TIME | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 1 | True / True |
| UTCNOW | UtcNow |  | DATETIME | Default / Declared / Declared | 1 файлов / True | 6 / 2 / 1 | True / True |
| TIMER | Timer |  | DOUBLE | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 1 | True / True |
| DATESERIAL | DateSerial | year: INTEGER, month: INTEGER, day: INTEGER | DATE | Default / Declared / Declared | 1 файлов / True | 5 / 6 / 3 | True / True |
| TIMESERIAL | TimeSerial | hour: INTEGER, minute: INTEGER, second: INTEGER | TIME | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 3 | True / True |
| YEAR | Year | value: DATE | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 3 | True / True |
| MONTH | Month | value: DATE | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 7 / 2 | True / True |
| MONTHNAME | MonthName | month: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 1 | True / True |
| DAY | Day | value: DATE | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 5 | True / True |
| HOUR | Hour | value: TIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 2 | True / True |
| MINUTE | Minute | value: TIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| SECOND | Second | value: TIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 13 / 14 / 2 | True / True |
| WEEKDAY | Weekday | value: DATE | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 1 | True / True |
| WEEKDAYNAME | WeekdayName | weekday: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| DATEADD | DateAddDateValue | interval: STRING, number: INTEGER, value: DATE | DATE | Default / Declared / Declared | 1 файлов / True | 6 / 7 / 2 | True / True |
| DATEADD | DateAddTimeValue | interval: STRING, number: INTEGER, value: TIME | TIME | Default / Declared / Declared | 1 файлов / True | 6 / 7 / 2 | True / True |
| DATEADD | DateAddDateTimeValue | interval: STRING, number: INTEGER, value: DATETIME | DATETIME | Default / Declared / Declared | 1 файлов / True | 6 / 7 / 2 | True / True |
| DATEDIFF | DateDiffDateValues | interval: STRING, fromValue: DATE, toValue: DATE | LONG | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 2 | True / True |
| DATEDIFF | DateDiffTimeValues | interval: STRING, fromValue: TIME, toValue: TIME | LONG | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 2 | True / True |
| DATEDIFF | DateDiffDateTimeValues | interval: STRING, fromValue: DATETIME, toValue: DATETIME | LONG | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 2 | True / True |
| DATEPART | DatePartDateValue | part: STRING, value: DATE | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 2 | True / True |
| DATEPART | DatePartTimeValue | part: STRING, value: TIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 2 | True / True |
| DATEPART | DatePartDateTimeValue | part: STRING, value: DATETIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 2 | True / True |
| DATEVALUE | DateValue | value: STRING | DATE | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 2 | True / True |
| TIMEVALUE | TimeValue | value: STRING | TIME | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 2 | True / True |
| ISDATE | IsDate | value: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 5 / 1 | True / True |
| UTCOFFSET | UtcOffset |  | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 7 / 2 | True / True |
| UTCOFFSET | UtcOffsetValue | value: DATETIME | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 7 / 2 | True / True |
| UTCOFFSET | UtcOffsetValueZone | value: DATETIME, timeZone: STRING | INTEGER | Default / Declared / Declared | 1 файлов / True | 4 / 7 / 2 | True / True |
| TOUTC | ToUtc | value: DATETIME | DATETIME | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| TOUTC | ToUtcZone | value: DATETIME, timeZone: STRING | DATETIME | Default / Declared / Declared | 1 файлов / True | 4 / 6 / 2 | True / True |
| FROMUTC | FromUtc | value: DATETIME | DATETIME | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 2 | True / True |
| FROMUTC | FromUtcZone | value: DATETIME, timeZone: STRING | DATETIME | Default / Declared / Declared | 1 файлов / True | 4 / 5 / 2 | True / True |
| ISLEAPYEAR | IsLeapYear | year: INTEGER | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| DAYSINMONTH | DaysInMonth | year: INTEGER, month: INTEGER | INTEGER | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| STARTOFMONTH | StartOfMonth | value: DATE | DATE | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| ENDOFMONTH | EndOfMonth | value: DATE | DATE | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 1 | True / True |
| STARTOFYEAR | StartOfYear | value: DATE | DATE | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 1 | True / True |
| ENDOFYEAR | EndOfYear | value: DATE | DATE | Default / Declared / Declared | 1 файлов / True | 5 / 2 / 1 | True / True |
| ISOPEN | IsOpen | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 3 / 2 | True / True |
| FILEPATH | FilePath | file: FILE | STRING | Default / Declared / Declared | 1 файлов / True | 9 / 6 / 1 | True / True |
| FILEMODE | FileMode | file: FILE | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 3 / 2 | True / True |
| FILEPOS | FilePos | file: FILE | LONG | Default / Declared / Declared | 1 файлов / True | 2 / 4 / 2 | True / True |
| FILELENGTH | FileLength | file: FILE | LONG | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| LINENUMBER | LineNumber | file: FILE | LONG | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 1 | True / True |
| EOF | Eof | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 2 / 3 | True / True |
| READTOEND | ReadToEnd | file: FILE | STRING | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 1 | True / True |
| READTEXT | ReadText | file: FILE, count: INTEGER | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 3 / 1 | True / True |
| READLINE | ReadLine | file: FILE | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 9 / 3 | True / True |
| READLINES | ReadLines | file: FILE, count: INTEGER | STRING() | Default / Declared / Declared | 1 файлов / True | 3 / 2 / 1 | True / True |
| SKIPLINE | SkipLine | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| WRITE | Write | file: FILE, text: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 10 / 7 / 3 | True / True |
| WRITELINE | WriteLine | file: FILE, text: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 4 | True / True |
| NEWLINE | NewLine | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 12 / 2 / 1 | True / True |
| FLUSH | Flush | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 6 / 3 / 2 | True / True |
| SEEKFILE | SeekFile | file: FILE, position: LONG | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| REWIND | Rewind | file: FILE | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| FILEEXISTS | FileExists | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 9 / 2 | True / True |
| DIREXISTS | DirExists | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 10 / 2 | True / True |
| PATHEXISTS | PathExists | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 2 | True / True |
| READFILE | ReadFile | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 4 / 17 / 3 | True / True |
| READALLLINES | ReadAllLines | path: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 6 / 6 / 2 | True / True |
| WRITEFILE | WriteFile | path: STRING, text: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 6 / 49 / 2 | True / True |
| APPENDFILE | AppendFile | path: STRING, text: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 3 / 2 | True / True |
| FILESIZE | FileSize | path: STRING | LONG | Default / Declared / Declared | 1 файлов / True | 6 / 9 / 1 | True / True |
| FILEDATE | FileDate | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| FILECREATEDATE | FileCreateDate | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 1 / 1 | True / True |
| FILEACCESSEDDATE | FileAccessDate | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 1 / 1 | True / True |
| DIRDATE | DirDate | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 1 / 1 | True / True |
| GETFILES | GetFiles | path: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 1 | True / True |
| GETFILES | GetFilesPattern | path: STRING, pattern: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 1 | True / True |
| GETDIRS | GetDirs | path: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 1 | True / True |
| GETDIRS | GetDirsPattern | path: STRING, pattern: STRING | STRING() | Default / Declared / Declared | 1 файлов / True | 2 / 1 / 1 | True / True |
| COMBINEPATH | CombinePath | left: STRING, right: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 30 / 1 | True / True |
| FILENAME | FileName | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 6 / 6 / 1 | True / True |
| DIRNAME | DirName | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 2 / 3 / 1 | True / True |
| EXTNAME | ExtName | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 1 / 1 | True / True |
| FULLPATH | FullPath | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 5 / 3 / 2 | True / True |
| PATHROOT | PathRoot | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| ISABSOLUTEPATH | IsAbsolutePath | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 3 / 2 / 1 | True / True |
| MAKEDIR | MakeDir | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 23 / 1 | True / True |
| DELETEFILE | DeleteFile | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 25 / 1 | True / True |
| COPYFILE | CopyFile | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 1 | True / True |
| COPYFILE | CopyFileOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 5 / 4 / 1 | True / True |
| MOVEFILE | MoveFile | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| MOVEFILE | MoveFileOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| RENAMEFILE | RenameFile | path: STRING, newName: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 3 / 2 / 1 | True / True |
| DELETEDIR | DeleteDir | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 4 / 4 / 1 | True / True |
| DELETEDIRRECURSIVE | DeleteDirRecursive | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 3 / 15 / 1 | True / True |
| COPYDIR | CopyDir | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| COPYDIR | CopyDirOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| MOVEDIR | MoveDir | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 2 / 1 | True / True |
| RENAMEDIR | RenameDir | path: STRING, newName: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 3 / 2 / 1 | True / True |
| CURRENTDIR | CurrentDir |  | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 19 / 1 | True / True |
| CHANGEDIR | ChangeDir | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 2 / 3 / 1 | True / True |
| DIRSIZE | DirSize | path: STRING | LONG | Default / Declared / Declared | 1 файлов / True | 3 / 6 / 1 | True / True |
| FREESPACE | FreeSpace | path: STRING | LONG | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| TOTALFREESPACE | TotalFreeSpace | path: STRING | LONG | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| TOTALSPACE | TotalSpace | path: STRING | LONG | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| ISREADONLY | IsReadOnly | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 9 / 1 / 1 | True / True |
| ISHIDDEN | IsHidden | path: STRING | BOOLEAN | Default / Declared / Declared | 1 файлов / True | 3 / 1 / 1 | True / True |
| DRIVENAME | DriveName | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| DRIVEFORMAT | DriveFormat | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| DRIVETYPE | DriveType | path: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 3 / 1 | True / True |
| MAKETEMPFILE | MakeTempFile | prefix: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 26 / 2 | True / True |
| MAKETEMPDIR | MakeTempDir | prefix: STRING | STRING | Default / Declared / Declared | 1 файлов / True | 3 / 25 / 1 | True / True |
| INT | Int | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Integer | 1 файлов / True | 26 / 11 / 5 | True / True |
| STR | Str | value: DOUBLE | STRING | Numeric / PreserveNumericArguments / String | 1 файлов / True | 8 / 33 / 5 | True / True |
| VAL | Val | text: STRING | DOUBLE | Default / Declared / Declared | 1 файлов / True | 4 / 7 / 5 | True / True |
| CINT | CInt | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Declared | 1 файлов / True | 2 / 2 / 2 | True / True |
| CLNG | CLng | value: DOUBLE | LONG | Numeric / PreserveNumericArguments / Declared | 1 файлов / True | 2 / 2 / 2 | True / True |
| CDBL | CDbl | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / Declared | 1 файлов / True | 2 / 1 / 2 | True / True |
| CDEC | CDec | value: DOUBLE | DECIMAL | Numeric / PreserveNumericArguments / Declared | 1 файлов / True | 2 / 1 / 1 | True / True |
| RND | Rnd |  | DOUBLE | Default / Declared / Declared | 1 файлов / True | 6 / 4 / 3 | True / True |
| ABS | Abs | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 10 / 7 / 4 | True / True |
| SGN | Sgn | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Integer | 1 файлов / True | 3 / 2 / 1 | True / True |
| MIN | Min | left: DOUBLE, right: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | 1 файлов / True | 5 / 3 / 2 | True / True |
| MAX | Max | left: DOUBLE, right: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | 1 файлов / True | 5 / 3 / 1 | True / True |
| CLAMP | Clamp | value: DOUBLE, low: DOUBLE, high: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | 1 файлов / True | 5 / 4 / 1 | True / True |
| SIN | Sin | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 5 / 3 / 2 | True / True |
| COS | Cos | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| TAN | Tan | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| ATN | Atn | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 2 / 2 / 1 | True / True |
| ATN2 | Atn2 | y: DOUBLE, x: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| SQR | Sqr | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 5 / 4 / 2 | True / True |
| CBRT | Cbrt | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| EXP | Exp | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| LOG | Log | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 6 / 4 / 3 | True / True |
| LOG10 | Log10 | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| LOG2 | Log2 | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 4 / 2 / 1 | True / True |
| POW | Pow | value: DOUBLE, power: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 5 / 4 / 1 | True / True |
| FLOOR | Floor | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 4 / 2 / 3 | True / True |
| CEIL | Ceil | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 3 / 2 / 3 | True / True |
| ROUND | Round | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 5 / 11 / 3 | True / True |
| FIX | Fix | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 6 / 4 / 3 | True / True |
| ASIN | Asin | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 3 / 2 | True / True |
| ACOS | Acos | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 5 / 3 / 2 | True / True |
| ASINH | Asinh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| ACOSH | Acosh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 3 / 1 | True / True |
| ATANH | Atanh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 4 / 3 / 2 | True / True |
| SINH | Sinh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| COSH | Cosh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| TANH | Tanh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | 1 файлов / True | 3 / 2 / 1 | True / True |
| SETSIGN | SetSign | value: DOUBLE, signFrom: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | 1 файлов / True | 6 / 2 / 1 | True / True |

## 2026-10-08 — состояние после исправлений, Session 1121

Все находки LA26-001…014 получили исправления: см. LANGUAGE_REPAIR_STATUS_20261008.md. Реестр теперь содержит 305 дескрипторов: две ранее поддерживаемые формы LBOUND/UBOUND с dimension перенесены из связывателя. Предыдущие CSV и таблицы описывают исходную ревизию и не перезаписаны. Для затронутых функций подтверждены конкретные регрессионные сценарии; полная семантика всех перегрузок остаётся отдельным предметом проверки.

### 2026-10-08 — независимые Unicode и файловые проверки, Session 1123

Новые подтверждённые находки и текущие исправления: docs/archive/audits-through-20261011/LANGUAGE_RECHECK_20261008.md, LA26-015…018. Исправлены потеря малого остатка ARRAYAVERAGE (Session 1122), зависимость WRITE от FLUSH и лишние нулевые байты, переход DIRSIZE/COPYDIR по вложенным ссылкам, поздний отказ COPYDIR при вложенном назначении. Первоначальные JSON/CSV не заменены. Полнота всех перегрузок не заявлена; явный выбор кодировки остаётся отдельным необходимым расширением.
### Session 1124 — продолжение независимой перепроверки

LA26-019…021: исправлены папка файлового исполнения, физическое чтение POINT при CANVAS и потеря диапазона остановки IDE. Подробности и классификация устаревших проверок: LANGUAGE_RECHECK_20261008.md. Полный прогон 3596/3596, IDE 592/592; исходные JSON/CSV не перезаписаны. Новые языковые имена не добавлены, явная кодировка G01 остаётся отдельным расширением.

### Session 1130 — фактический текущий реестр и границы доказательств

CLI metadata текущей рабочей копии: 309 дескрипторов, 249 пользовательских имён (.artifacts/explicit-open-1130-metadata.json). G01 реализован и отдельно перепроверен; EXPLICIT_OPEN_RECHECK_20261008.md фиксирует два найденных и исправленных дефекта. Исторический CSV 20261007 не переписывается как будто это новый аудит. Полный прогон 3670/3670 не заменяет поэлементное заполнение неизвестных семантических клеток всех форм. Следующее семейство — REGEX, с отдельными свидетельствами для каждой формы.

### Session 1132 — отдельные доказательства 18 форм REGEX

REGEX_FORM_EVIDENCE_20261008.csv построена из актуального registry и содержит поформенные ссылки на проверки. Все 18 форм имеют обычные/пустые/ошибочные сценарии, паритет и реальный тайм-аут обоих движков. Прочие параметры и Unicode проверены дополнительно в явно перечисленных случаях; строки таблицы не объявляют доказанным весь набор возможных шаблонов. Общий исторический CSV не заменён. REGEX_RECHECK_20261008.md фиксирует RX26-001/002 исправленного кода и RX26-003 ошибочных примеров manual.

## Session 1133 — ограниченный срез DateTime

Текущий реестр DateTime имеет 46 дескрипторов и 33 имени. Проверены календарные границы, арифметика DATE/TIME, явный America/New_York и начальные значения DIM. 10/10 вариантов двух движков; все 46 форм проверенными не объявляются. Доказательства DATETIME_RECHECK_20261008.md.

## Session 1135 — доказательства всех форм DateTime

DATETIME_FORM_EVIDENCE_20261008.csv содержит 46 строк по текущему реестру, обычные вызовы, ожидаемые значения, связанный ID/тип, неверные аргументы и ссылки на конкретные граничные сценарии. Непроверенные отдельные границы явно отмечены. Исправлена DT26-003 больших сдвигов TIME, язык не расширялся. Исходная общая матрица 20261007 сохранена.

## Session 1136 — строковый поиск

14 форм Search/LEN/LEFT/RIGHT/MID, 57/57 адресных проверок. Доказательства STRING_SEARCH_FORM_EVIDENCE_20261008.csv, границы STRING_SEARCH_RECHECK_20261008.md. INSERT/REMOVE и остальные группы пока вне набора.

## Session 1137 — строковое построение

15 форм Fill/Array/Edit с независимыми результатами и типами, 53 проверки; вместе с поиском 110/110. Доказательства STRING_BUILD_FORM_EVIDENCE_20261008.csv, ограничения STRING_BUILD_RECHECK_20261008.md. Наличие тестов не доказывает произвольное выделение памяти.

## Session 1138 — обрезка, регистр и коды символов

13 форм имеют независимые проверки связанного ID/типа, результатов обоих движков и неверных аргументов. Вместе с поиском/построением — 148/148. Реестр String сверён: 42 уникальные формы, 42 строки доказательств, пропусков и лишних ID нет. Все 256 кодов CP866 проверены обоими движками относительно независимого системного кодировщика. Выбранные границы Unicode, пустые строки, культуры и ошибки MB3007 подтверждены; все возможные значения Unicode не перебраны. Исполнитель не менялся, новых дефектов не подтверждено.

Manual characters и trim-and-case уточняет четыре формы кодирования, шесть форм обрезки, UTF-16, пустой набор, Unicode-пробелы, независимость регистра от культуры и замещение изолированного первого суррогата в ASC/ASCW. Для обратимого преобразования Unicode уже есть ASCW/CHRW; новый дубль не требуется. Пример samples/functions/ascw/unicode_round_trip.axm прошёл CLI, код 0: 159, 1071, TRUE, 128512, 2. Подробности STRING_CASE_RECHECK_20261008.md и CSV 13 форм. Новых токенов и измерений быстродействия нет.

## Session 1139 — числовые формы и знак нуля

Все 31 дескриптор Numeric проверены по связанному ID/типу и результатам двух движков: 61 обычный вариант с четырьмя числовыми типами, 37 неверных аргументов. 132/132 адресных проверок с границами/преобразованиями; две проверки пула констант. NUM26-001: VM терял знак нуля при объединении DOUBLE-констант. Ключ теперь содержит битовое представление, одинаковые биты продолжают использовать общий индекс. Подробности NUMERIC_RECHECK_20261008.md и NUMERIC_FORM_EVIDENCE_20261008.csv. Manual SETSIGN и samples/functions/setsign/signed_zero.axm отражают договор. Новых токенов и измерений быстродействия нет. Все значения NaN/внутренние входы и все сочетания продвижения типов ещё не проверены.

## Session 1140 — смешанные типы и NUM26-002

NumericPromotionContractTests проверяет все 96 сочетаний INTEGER/LONG/DOUBLE/DECIMAL для MIN/MAX/CLAMP: 74 допустимых вызова имеют правильный общий тип аргументов/результата и результат обоих движков, 22 сочетания DECIMAL+DOUBLE отклоняются при связывании с одинаковыми координатами. CSV NUMERIC_PROMOTION_EVIDENCE_20261008.csv сохраняет каждое сочетание. Дополнительно проверены LONG.MinValue/MaxValue, DECIMAL порядка 9e27, точность целочисленного выбора, ABS/SETSIGN на минимальном LONG и явное преобразование.

Исходный прогон — 96/98: оба движка выбросили InvalidOperationException на CDBL(CDEC(2)). NUM26-002 P1: EvaluateCdbl использовал AsDouble, который намеренно не выполняет неявного преобразования DECIMAL. Исправлен именно явный CDBL: для DECIMAL используется явное приведение к double, другие ветви сохранены. RuntimeValue.AsDouble не расширялся. Это дефект существующей функции, а не отсутствие API. Итоговый набор с прежними числовыми проверками 232/232 (.artifacts/numeric-1140-fixed.log/TRX).

NumericNonFiniteOperandTests: NaN, +Infinity, -Infinity искусственно передаются в общий ToDoubleMathOperand и отклоняются через callback аргумента и RuntimeFailureException до вычисления, координаты сохранены; 3/3. Это проверка общей границы математических функций, не всех внутренних путей SGN/MIN/SETSIGN. Прямое получение таких значений через VAL уже запрещено прежним договором.

NUM26-003 P4: manual ошибочно обещал отсутствие потерь CDBL для любого целого типа. LONG выше 2^53 может округляться. Исправлен договор и добавлен inline-пример CDBL(CDEC(...)), смешанные аргументы и явный выбор DOUBLE. samples/functions/cdbl/decimal_precision.axm показывает 2/9007199254740992/TRUE/2; окончательный CLI-запуск проверяется после пересборки. Первый редакционный ориентир — Зельднер CDBL (288), пример о влиянии исходной точности; запасной — [Microsoft Learn Type Conversion Functions](https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/functions/type-conversion-functions). Семантика округления CINT/CLNG из VB не переносилась.

Новых имён, токенов или измерений быстродействия нет. Все значения и комбинации операций не доказаны. Следующий срез — неагрегатные формы массивов: границы, выделение, копирование/срезы, сравнение доступных средств с реальными пробелами API. Общий аудит и визуальная приёмка IDE продолжаются.

## Session 1141 — сведения и выборка массивов

15 форм сведений/клона/срезов проверены по ID/типу/рангу и результатам двух движков, 28 неверных аргументов и выбранные границы. AW26-001 P2: ARRAYSLICE/ARRAYTAKE/ARRAYSKIP игнорировали явное измерение одномерного массива. Обе реализации теперь проверяют dimension до одномерного пути. 12 вариантов недопустимых измерений дают MB3007 и перехватываются; итог 57/57. Manual и arraytake/explicit_axis.axm отражают договор, доказательства ARRAY_WINDOW_RECHECK и CSV 15 форм. Предельные пустые диапазоны и остальные преобразования пока не доказаны. Новых токенов/измерений быстродействия нет.

## Session 1142 — AW26-002: крайние пустые многомерные диапазоны

Подтверждён P1: ARRAYSLICE(a, INTEGER.MinValue, 0, dimension) и полный ARRAYSKIP измерения с верхней границей INTEGER.MaxValue аварийно выходили с OverflowException в обоих движках. В start-1/upper+1 происходило переполнение INTEGER, а последующая попытка вычислить длину ошибочного непустого диапазона падала. CLI-воспроизведение .artifacts/array-1142-skip-max.axm содержит всего один элемент; причина не в большом выделении.

Первый тест ошибочно объявлял исходный массив с нижней границей INTEGER.MinValue и получил законный отказ: минимальная граница зарезервирована для поискового признака. Это не дефект Axiom. После замены исходной границы на MinValue+1 все четыре параметризованные проверки падали с OverflowException (.artifacts/array-1142-valid-before.log/TRX). Не расширялось множество допустимых исходных индексов.

Общий BuiltinArrayHelpers.CreateEmptyWindowBounds вычисляет запрос нижней границы как LONG. Когда выбранная пустая пара не представима допустимыми INTEGER-индексами, используется 1 TO 0; иначе сохраняется прежняя пара lower/lower-1. Обе реализации ARRAYSLICE/ARRAYSKIP используют общий расчёт. ARRAYTAKE не менялся: допустимая минимальная исходная нижняя граница MinValue+1 позволяет построить пустую пару без переполнения.

ArrayEmptyBoundaryContractTests — четыре параметризованные проверки по шесть случаев: 24 сочетания, оба движка и оба измерения. Проверены TAKE около MinValue+1, SLICE с MinValue/MAX и нулевой длиной, SKIP у MAX, обычные отрицательные пустые границы, выделение, длина/ранг и сохранность другого измерения. В сочетании с предыдущими сведениями/срезами 61/61 (.artifacts/array-1142-fixed.log/TRX), без перехода VM на другой движок. Это не доказательство всех рангов и всех числовых индексов.

Manual arrays-functions и arrays-and-builtins прямо описывает исключительную пустую форму, зарезервированную нижнюю границу и inline-пример. Новый samples/functions/arrayskip/empty_at_max_index.axm рассчитан на ручную приёмку. Редакционные ориентиры сохраняются: Зельднер, ограничения массивов (396), текущая официальная Learn о массивах; семантика VB не переносилась. Новых токенов/инструкций/измерений быстродействия нет. Новая функция языка не требуется: исправлено существующее построение результата.

Следующий шаг — явно переданные измерения у остальных преобразований массива, затем FLATTEN/RESHAPE/CONCAT и поиск/динамическое изменение. Общий аудит и визуальная приёмка IDE продолжаются.

## Session 1143 — AW26-003: явное измерение остальных преобразований

Подтверждён P2 у десяти функций: ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE/ARRAYBINARYSEARCH/ARRAYPUSH/ARRAYPOP/ARRAYINSERT/ARRAYREMOVE/ARRAYCONCAT. Одномерный путь обходил ResolveTransformDimension, а короткие пути сортировки также обходили проверку. Исходные 56/56 проверок не получили ожидаемого отказа (.artifacts/array-1143-before.log/TRX). Это дефект существующего параметра, а не отсутствие API.

Обе реализации теперь разрешают явно переданное измерение сразу после проверки массива и повторно используют результат в многомерном пути. Вызовы без измерения сохраняют прежний одномерный договор и существующую проверку обязательного номера для многомерного массива. Многомерный номер не проверяется дважды. Для допустимого выделенного массива отказ по номеру происходит до изменения длины/порядка/элементов.

ArrayTransformAxisContractTests: 56 вариантов недопустимых осей 0/2/MAX/MIN на десяти операциях, включая пустую и одноэлементную одномерную сортировку. Каждый сравнивает Code/Line/Column/Span обоих движков, TRY/CATCH и сохранность исходного массива. Десять обычных операций проверены с опущенным номером и номером 1, обоими движками: отдельные независимые результаты сортировки/разворота/поиска/изменения/объединения. Четыре дополнительные проверки пустой/одноэлементной двумерной сортировки проверяют оси 0/3 и допустимую 2, сохраняя границы другого измерения. Набор нового класса 70/70 (.artifacts/array-1143-short.log/TRX); вместе с предыдущими сведениями/срезами до этих четырёх дополнительных проверок 127/127. Итог полного решения записывается отдельно. Переход VM на интерпретатор запрещён.

Manual arrays-and-builtins содержит параметр dimension для десяти отдельных форм, контекст/диагностику и inline-пример отказа до изменения. Страницы ARRAYPUSH/POP/INSERT/REMOVE в arrays-functions уточнены напрямую. Новый samples/functions/arraysort/reject_invalid_axis.axm подготовлен для ручной приёмки. Первый ориентир — Зельднер, пузырьковая сортировка и вывод до/после (147–148); запасной официальный Microsoft Learn Array.Sort с примерами VB. Алгоритм пузырьковой сортировки и семантика .NET не перенесены в Axiom. Новых имён/токенов/инструкций и измерений быстродействия нет.

Все обычные многомерные формы десяти функций и все ошибки ранга/формы значения этим набором не доказаны. Далее независимые формы FLATTEN/RESHAPE/CONCAT и поиск/динамические срезы. Общий аудит и визуальная приёмка IDE продолжаются.


### Session 1144 — ARRAYCONCAT у последнего индекса INTEGER

AW26-004 P1: обе реализации многомерного ARRAYCONCAT выполняли сложение границ в INTEGER. На двух одноэлементных массивах с нижней границей 2147483646 промежуточная сумма переполнялась, хотя итоговая верхняя граница 2147483647 допустима. При начале 2147483647 результат действительно непредставим, но вместо перехватываемой диагностики возникало OverflowException.

Вычисление переведено в LONG; перед созданием результата превышение INTEGER.MaxValue даёт MB3007. Исходные массивы не меняются. Новый ArrayConcatBoundaryContractTests: восемь вариантов, две оси и оба движка, проверяет независимые границы/значения и TRY/CATCH. До исправления 8/8 падали; после исправления совместный набор 139/139. Новый русскоязычный пример samples/functions/arrayconcat/maximum_index.axm и прямое объяснение на manual arrays-and-builtins.html#concat-bounds. Новых имён, токенов и измерений быстродействия нет.

Редакционный ориентир: Зельднер, ограничения массивов, с. 396; запасной жанровый ориентир — https://learn.microsoft.com/ru-ru/dotnet/visual-basic/programming-guide/language-features/arrays/ . Ограничения VB/QuickBASIC не перенесены в Axiom. Общая проверка ARRAYFLATTEN/ARRAYRESHAPE, пустых форм и представлений ещё не завершена.


## Session 1145 — порядок и форма ARRAYFLATTEN/ARRAYRESHAPE/ARRAYCONCAT

ArrayShapeContractTests проверяет 10 успешных сценариев и 3 отказа размеров с независимыми ожидаемыми значениями в обоих движках: порядок последнего измерения, перенос INTEGER из 2x3 в 3x2, разные/отрицательные границы, одномерные представления, независимость INTEGER-элементов результата от источника/образца, пустой выделенный FLATTEN и пустую форму RESHAPE, CONCAT по двум осям, объединение с пустой частью. Семь неверных типов и пять невыделенных аргументов сравнивают диагностики/координаты. Пять форм отдельно проверяют точный BuiltinId, тип INTEGER и ранг результата; CSV ARRAY_SHAPE_FORM_EVIDENCE_20261008.csv. Это не доказательство всех типов элементов, рангов и вложенных пользовательских структур.

AW26-005 P4: manual ограничивал source у ARRAYRESHAPE словом «плоский», хотя Binder и оба исполнителя поддерживают исходный массив любого ранга. Исправлено описание, добавлены прямые разделы FLATTEN/RESHAPE с параметрами, порядком, выделением, пустой формой, диагностикой и inline-примерами. samples/functions/arrayreshape/matrix_to_matrix.axm подтверждает перенос 11/12/13/21/22/23 без предварительного FLATTEN и сохранность исходного 11 после изменения результата. CLI код 0. Это уже существующая возможность, не пробел API; дополнительная функция преобразования таблицы не нужна.

Одномерный CONCAT с двумя непустыми частями компактен, с пустой частью сохраняет границы другой части (4 TO 6 подтверждено). Это существующий договор, не исправление исполнения. Runtime в этом этапе не менялся. Редакционный ориентир — Зельднер, массивы/границы и ограничения с.396; запасной текущий официальный Learn о массивах https://learn.microsoft.com/ru-ru/dotnet/visual-basic/programming-guide/language-features/arrays/ . Новых токенов, инструкций и измерений быстродействия нет.

Далее независимо проверить многомерные динамические ARRAYPUSH/POP/INSERT/REMOVE: форму добавляемых/удаляемых срезов, представления, отказ по несовместимости и крайние границы; затем поиск и недостающие возможности. Общий аудит продолжается.


### Session 1146 — ARRAYINSERT в пустую нестандартную форму

AW26-006 P1: ARRAYINSERT требовал index=1 для пустого выбранного измерения, но сохранял нижнюю границу пустой формы. При ARRAYTAKE(seed,0,dimension) с исходным началом -4 или 9 вставка обращалась вне массива и аварийно завершалась. Четыре проверки после исправления неверного имени переменной (ROW — встроенная константа) воспроизвели дефект обоих движков: .artifacts/array-1146-reproduce.log/TRX. Ошибка имени в первой пробе не считалась дефектом языка.

Обе реализации при пустом выбранном измерении создают границы 1 TO 1 и не сдвигают отсутствующие элементы; остальные границы сохраняются. Одномерный путь также согласован с index=1, включая пустую форму, полученную через ARRAYRESHAPE. Непустые пути сохранены. ArrayEmptyInsertContractTests: восемь параметризованных вариантов с двумя осями (16 сочетаний) и четыре одномерные проверки, оба движка, начала -4/9/MinValue+1/MaxValue. Совместный набор 181/181, без перехода на интерпретатор. Промежуточный прогон array-1146-target застал изменение только интерпретатора и не считается итоговой проверкой.

Manual arrays-functions.html#arrayinsert содержит прямое правило пустой формы и inline-пример. Добавлен русскоязычный samples/functions/arrayinsert/empty_dimension.axm. Редакционный ориентир — массивы и границы у Зельднера, запасной официальный Learn Array.Resize с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.resize?view=net-10.0 ; правила .NET не перенесены. Новых токенов/инструкций/имён и измерений быстродействия нет.

Далее продолжить обычные многомерные PUSH/POP/INSERT/REMOVE, совместимость формы значения, сохранность отказов и расширение у максимальной границы. Эти вопросы ещё не объявлены доказанными; общий аудит активен.


### Session 1147 — обычные многомерные динамические операции

ArrayDynamicShapeContractTests содержит 22 проверки. Восемь форм четырёх операций по двум осям проверяют независимо рассчитанные значения, отрицательные границы выбранного измерения, сохранение остальных границ 7 TO 8 и границы возвращаемого среза POP/REMOVE. Четыре отказа несовместимой формы/индекса сравнивают Code/Line/Column/Span обоих движков, TRY/CATCH и все четыре исходных INTEGER-элемента. Два случая проверяют представление того же массива как значение PUSH/INSERT: исходный срез переносится без порчи при сдвиге.

Четыре варианта engine/axis проверяют PUSH и INSERT у INTEGER.MaxValue: MB3007 перехватывается до изменения длины, границ и значений. Ещё четыре проверки запрещают изменение размера представления через каждую функцию, сохраняя массив-владелец. Внутри каждой проверки обычных операций/отказов работают оба движка; переход VM на интерпретатор запрещён. Итог нового класса 22/22, совместного набора 203/203 (.artifacts/array-1147-final.log/TRX), без ошибок и пропусков. Код Runtime не менялся; полное решение предыдущего этапа 4580/4580, повторный общий прогон в этом этапе не выполнялся. git diff --check чистый.

Нового дефекта исполнения в проверенных сценариях не обнаружено. Добавление/удаление строки или столбца уже выполняется ARRAYPUSH/POP/INSERT/REMOVE с номером измерения; отдельная функция для этой операции не нужна. Запускаемые multidimensional_by_column.axm в тематических папках уже существуют и были выполнены в Session 1146. Manual arrays-functions содержит соответствующие формы; новой семантики/токенов/инструкций/измерений быстродействия нет. Это не доказательство всех типов элементов, рангов, всех разновидностей представлений и крупных выделений памяти.

Далее независимая проверка поиска ARRAYCONTAINS/ARRAYINDEXOF/ARRAYLASTINDEXOF/ARRAYFINDALL/ARRAYBINARYSEARCH: отрицательные и крайние границы, отсутствие результата, повторы, координаты многомерного результата и пустые формы. Затем оставшиеся функции массивов и реальные пробелы API по общей матрице. Общий аудит продолжается.


### Session 1148 — независимый поиск в массивах

ArraySearchContractTests: 22 проверки обоих движков. Одномерные INTEGER-массивы с повтором проверены при нижних границах -4/5/MinValue+1/MaxValue-2; одиночный элемент у MaxValue найден без переполнения. INDEXOF/LASTINDEXOF/BINARYSEARCH возвращают исходный индекс, при отсутствии — lower-1, включая INTEGER.MinValue. FINDALL сохраняет найденные исходные индексы в компактном массиве. CONTAINS/COUNT дают независимые TRUE/FALSE/число.

Проверены двумерные координаты первого/последнего совпадения, порядок таблицы FINDALL 3x2, выделенные пустые координаты, сохранение двух столбцов пустого FINDALL и пустой исходный массив. Бинарный поиск по двум осям проверен на таблице, отсортированной по обеим осям: axis=1 даёт (-1,4), axis=2 — (-2,5). Это первая подходящая линия выбранного измерения, не обещание единого первого совпадения обычного обхода.

Шесть невыделенных аргументов и шесть несовместимых BOOLEAN-значений сравнивают диагностики/координаты обоих движков. Unicode STRING-проверка отличает Ёж/ёж/Еж и находит целый 😀, сохраняя логические индексы. Переход VM на интерпретатор запрещён. Новый класс 22/22; совместный набор 225/225 (.artifacts/array-1148-final.log/TRX), без ошибок/пропусков. Код Runtime не менялся; последний полный прогон предыдущего этапа 4580/4580, в этом этапе общий прогон не повторялся.

AW26-007 P4: таблица manual ошибочно обещала 0 при отсутствии совпадения независимо от нижней границы. Обе реализации уже возвращали lower-1. Исправлены три строки и прямое объяснение результата, многомерных координат, FINDALL, бинарного поиска по линиям и диагностик. Новый русскоязычный samples/functions/arrayindexof/negative_missing_index.axm прошёл CLI с кодом 0: -5/-5/-5/-4/-3. Существующий поиск не требует нового API для обнаружения первого/последнего/всех совпадений.

Редакционный ориентир по границам — Зельднер, с.396; самих современных функций у него нет, запасной жанровый ориентир — официальный Learn Array.IndexOf с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.indexof?view=net-10.0 . Семантика подтверждена Axiom, не перенесена из .NET. Новых имён/токенов/инструкций и измерений быстродействия нет. Все типы/ранги и виды представлений не объявлены доказанными.

Далее ARRAYDISTINCT и функции с пользовательскими функциями ARRAYEVERY/SOME/FILTER/MAP/REDUCE: пустые формы, число вызовов, порядок, крайние индексы и ошибки пользовательской функции. Затем незакрытые семейства общей матрицы и реестр отсутствующих возможностей. Общий аудит активен.


### Session 1149 — последний индекс пользовательской обработки массива

AW26-008 P1: одномерные быстрые пути BytecodeVm для ARRAYEVERY/SOME/FILTER/MAP/REDUCE выполняли index++ после обработки INTEGER.MaxValue. Переполненный индекс приводил к аварийному обращению вне массива. Пять проверок одноэлементного массива до исправления проходили в Interpreter и падали в BytecodeVm (.artifacts/array-1149-before.log/TRX).

Каждый из пяти обходов VM теперь завершает цикл при index==upperBound до увеличения. Ранние возвраты EVERY/SOME и проверка пустого диапазона сохранены; Interpreter уже использовал безопасный общий обход и не менялся. ArrayCallbackBoundaryContractTests проверяет пять операций у MAX и ровно один вызов пользовательской функции, пять пустых операций без единого вызова и два ранних завершения на первом решающем элементе. Оба движка проверены без перехода на интерпретатор. Новый класс 12/12; совместный набор 237/237 (.artifacts/array-1149-final.log/TRX), без ошибок/пропусков.

Manual arrays-and-builtins объясняет пять отдельных функций, аргументы, пустые результаты, раннее завершение и допустимый последний индекс; добавлен inline-пример. Новый русскоязычный samples/functions/arraymap/maximum_callback_index.axm проверен CLI, код 0: TRUE/FALSE/42/84/50; SHA256 Runtime в CLI совпал с актуальной библиотекой. Новых имён/токенов/инструкций и измерений быстродействия нет.

Редакционный ориентир — Зельднер, FUNCTION и возвращаемое значение (158–159); самих современных функций у него нет. Запасной жанровый ориентир — официальный Learn Array.TrueForAll с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.trueforall?view=net-10.0 . Семантика подтверждена кодом и независимыми тестами Axiom; правила SHARED и исторический способ возврата не перенесены.

Далее ARRAYDISTINCT и оставшиеся обычные/многомерные формы FILTER/MAP/REDUCE: порядок, изменение типа результата, ошибки пользовательской функции, несовместимые сигнатуры. Затем прочие незакрытые семейства общей матрицы и недостающие возможности. Все типы/ранги и формы представлений этим исправлением не доказаны. Общий аудит активен.


### Session 1150 — порядок преобразования и ошибка пользовательской функции

AW26-009 P1: BytecodeVm при ошибке синхронно вызываемой пользовательской функции немедленно находил внешний обработчик вызывающего кадра. InvokeBytecodeFunction ещё ожидал результат и затем выдавал ложную диагностику отсутствия RETURN. Независимая ARRAYMAP-проба проходила в Interpreter, падала в Bytecode (.artifacts/array-1150-error-mode.log/TRX); деление на ноль должно сохранять MB3001. Первоначальное имя Append конфликтовало с ключевым словом, исправлено на AccumulateText и не считалось дефектом.

ExecuteCurrentInstruction принимает необязательную глубину границы обработки ошибки. Синхронный InvokeBytecodeFunction передаёт глубину вызывающего стека; TryHandleRuntimeFailure обрабатывает только кадры выше этой границы. Если обработчика внутри функции нет, ошибка возвращается к инструкции встроенного вызова, где внешний обработчик доступен обычным путём. Состояние границы передаётся параметром, а не общим изменяемым полем. Вложенные вызовы и FINALLY сохраняют порядок.

ArrayCallbackOrderContractTests (13/13) проверяет четыре многомерных операции, порядок 1234, INTEGER→STRING MAP с сохранением границ, STRING-накопитель REDUCE, фильтр чётных элементов, DISTINCT, пять неподходящих сигнатур, исходный MB3001 и остановку после второго элемента, обработчик внутри функции с FINALLY, вложенный ARRAYREDUCE внутри MAP с FINALLY и необработанный исходный код/координаты. ArrayDistinctValueContractTests (4/4) проверяет соседние LONG/DECIMAL выше 2^53 без объединения, Unicode-регистр/эмодзи/NFC-NFD и пустой выделенный результат. Оба движка без перехода на интерпретатор; совместный набор 254/254 (.artifacts/array-1150-final.log/TRX).

Manual arrays-and-builtins содержит прямое правило передачи ошибки и inline-пример; новый русскоязычный samples/functions/arraymap/catch_callback_error.axm. Архитектурное правило границы синхронного вызова записано в ARCHITECTURE. Новых имён/токенов/инструкций и измерений быстродействия нет. Редакционный ориентир — Зельднер, FUNCTION 158–159; запасной официальный Learn Array.ConvertAll с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.convertall?view=net-10.0 . Семантика подтверждена Axiom; правила .NET не перенесены.

Далее сопоставить оставшиеся зарегистрированные Array-формы с накопленными свидетельствами, проверить ARRAYFILL и многомерные сортировки/развороты, затем перейти к незакрытым семействам общей матрицы и реальным отсутствующим возможностям. Обработка всех видов событий/всех ошибок пользовательской функции этим набором не доказана. Общий аудит активен.


### Session 1151 — сортировка на предельном индексе и реестр массивов

AW26-010 P1: общий GetStableSortedLine увеличивал INTEGER-индекс после обработки последнего элемента с индексом 2147483647. Переполнение приводило к обращению за границы массива. До исправления восемь независимых случаев ARRAYSORT/ARRAYTOSORTED по обеим осям падали в обоих движках. Цикл теперь завершает обход сразу после верхней границы.

ArraySortBoundaryContractTests содержит восемь проверок предельной границы; ArrayFillSortReverseContractTests содержит десять проверок сортировки и разворота по двум осям, сохранения исходника при копировании, заполнения через представление, пустого массива и единственного элемента на предельном индексе. Совместный адресный набор: 272/272. Полное решение: 4674/4674, девять TRX, без ошибок и пропусков, код процесса 0 (.artifacts/array-1151-full.log, .artifacts/test-results/array-1151-full/). git diff --check без ошибок.

Manual arrays-and-builtins прямо объясняет сортировку по осям, сохранение границ и отличие копирования от изменения исходника. Русскоязычный samples/functions/arraysort/maximum_line_index.axm выполнен актуальным CLI с кодом 0: копия 10/30, исходник до изменения 30, после сортировки 10/30. SHA256 библиотеки Runtime совпал с копией CLI. Редакционный ориентир: Зельднер, сортировка, страницы 147–148; запасной жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/system.array.sort?view=net-10.0 . Семантика проверена по Axiom.

ARRAY_REGISTRY_EVIDENCE_20261008.csv сопоставляет 38 зарегистрированных имён (53 регистрации в исходнике) с существующими наборами проверок. Это указатель свидетельств по именам, а не доказательство всех 53 форм вызова, типов, рангов и представлений. ARRAYFILL и сортировка/разворот по осям уже доступны существующим API; отдельные новые функции для проверенных сценариев не требуются. Новых токенов языка и измерений быстродействия нет.

Следующий шаг: проверить соответствие каждой из 53 зарегистрированных форм точным вызовам и результатам тестов, выделить неподтверждённые формы; затем продолжить незакрытые семейства общей матрицы и поиск полезных отсутствующих возможностей. Общий аудит остаётся активным.


### Session 1152 — проверка точности реестра свидетельств

Добавлен ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv: отдельная строка для каждой из 53 регистраций из BuiltinFunctions.Array.cs, имя, BuiltinId, число и имена параметров, строка исходника и кандидаты вызовов в тестах. Проверены число 53, число уникальных имён 38 и уникальность пары имя/число аргументов. Для всех форм найдены текстовые кандидаты. Это предварительный указатель: отрицательные проверки, интерполируемые программы и успешные вызовы пока не разделены; совпадение текста не доказывает выбранный Binder результат, ранг и фактическое исполнение.

Исправлена неточная ссылка ARRAYAVERAGE в ARRAY_REGISTRY_EVIDENCE_20261008.csv: ArrayAggregateContractTests не содержит прямых проверок среднего; фактическое свидетельство находится в AuditRemainingBehaviorTests. Прочитаны проверки остатка после сокращения, субнормального округления, предельных конечных значений и многомерного среднего. AuditRemainingBehaviorTests вместе с ArrayFillSortReverseContractTests повторно выполнены: 22/22, без ошибок и пропусков, код 0 (.artifacts/array-1152-evidence.log/TRX). Исполняемый код и поведение языка не менялись; повтор полного решения не требуется для этой правки указателей.

Следующий шаг: вручную проверить кандидаты всех 53 форм, записать конкретные успешные случаи, ожидаемый результат и проверку BuiltinId/типа, добавить независимые проверки непокрытых форм. После этого продолжить остальные семейства и отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1153 — успешные формы массивов без явной оси

Добавлен ArrayDefaultFormEvidenceTests: восемь форм ARRAYTOSORTED, ARRAYTOREVERSED, ARRAYSORT, ARRAYREVERSE, ARRAYPUSH, ARRAYINSERT, ARRAYPOP, ARRAYREMOVE без аргумента оси. Каждая проверяет точный BuiltinId, тип результата (одномерный INTEGER-массив, INTEGER или VOID), независимый ожидаемый порядок значений и исполнение обоих движков без перехода на интерпретатор. Исходный массив имеет границы 5..7 и значения 30/10/20; копирующие операции сохраняют первый исходный элемент, изменяющие дают ожидаемый порядок. Проверяется возврат 20 при POP и 10 при REMOVE(6).

Новый набор 8/8; вместе с ArrayWindowFormEvidenceTests — 65/65, без ошибок и пропусков, код 0 (.artifacts/array-1153-forms.log и .artifacts/array-1153-combined.log/TRX). Исполняемый код и договор языка не менялись. Дополнительный полный прогон решения для добавленных независимых тестов не выполнялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv внесены отдельные VerifiedEvidence и VerifiedScope: 23 из 53 форм подтверждены сочетанием точного связывания, типа и успешного ожидаемого вывода (15 форм границ/окон, 8 новых). Остальные 30 остаются кандидатами, а не считаются непроверенными вообще: их ранее накопленные проверки ещё требуют отдельного сопоставления. Все типы, ранги и представления этим результатом не доказаны. Новые языковые средства для данных сценариев не нужны: существующие формы работают.

Далее подтвердить остальные 30 форм, начиная с поиска и функций пользовательского преобразования, затем перейти к незакрытым семействам общей матрицы и отсутствующим возможностям. Общий аудит активен.


### Session 1154 — связывание форм поиска и координатных результатов

Добавлен ArraySearchFormEvidenceTests: 12 успешных случаев для семи зарегистрированных форм CONTAINS/COUNT/INDEXOF/LASTINDEXOF/FINDALL/BINARYSEARCH. Проверяются точный BuiltinId, скалярный или массивный тип, INTEGER-элементы и ранг координатного результата, независимые ожидаемые логические индексы и координаты в обоих движках без перехода на интерпретатор. Первое совпадение в одномерном массиве -4, последнее -3; многомерные координаты первого/последнего совпадения (-2,8)/(-1,7), FINDALL возвращает обе пары.

Первоначальная проба ошибочно ожидала многомерный ARRAYBINARYSEARCH без оси; Binder законно выдал MB2003. Это не дефект: многомерная форма требует dimension. Проба заменена успешным вызовом с осью 1; отдельная отрицательная проверка сохраняет MB2003 для отсутствующей оси. Итоговый набор вместе с ArraySearchContractTests: 35/35, без ошибок и пропусков, код 0 (.artifacts/array-1154-final.log/TRX). Исполняемый код/семантика не менялись; полный прогон не повторялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv подтверждено 30 из 53 форм в явно ограниченном объёме; 23 требуют дальнейшего сопоставления. Поиск координат уже доступен существующим API. Далее формы функций пользовательского преобразования, затем оставшиеся формы массивов и незакрытые семейства/отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1155 — типы результатов пользовательского преобразования массива

ArrayCallbackOrderContractTests дополнен шестью независимыми проверками точного BuiltinId и типа результата: EVERY/SOME возвращают BOOLEAN, FILTER/DISTINCT — одномерный INTEGER-массив, MAP с Label — двумерный STRING-массив, REDUCE со строковым начальным значением — STRING. Существующие успешные программы этих форм проверяют ожидаемые значения и порядок обхода; добавлены EVERY/SOME с ранней остановкой: FALSE/один вызов/след 1 и TRUE/два вызова/след 12 соответственно.

Совместно с ArrayCallbackBoundaryContractTests и ArrayDistinctValueContractTests выполнено 37/37 без ошибок и пропусков, код 0 (.artifacts/array-1155-callback.log/TRX). Включены прежние проверки передачи ошибки, вложенного вызова и FINALLY, пустых массивов, предельного индекса, точных LONG/DECIMAL и Unicode DISTINCT. Исполняемый код и семантика языка не менялись; полный прогон не повторялся.

Реестр ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv теперь содержит ограниченное подтверждение 36 из 53 форм. Тип возвращаемого значения MAP/REDUCE определяется пользовательской функцией/накопителем и уже поддерживается существующим API; добавлять отдельные STRING-варианты не требуется. Оставшиеся 17 форм: многомерные изменения/сортировки, заполнение, преобразование формы/соединение и агрегаты. Следующий шаг — точное сопоставление этих форм с существующими независимыми проверками и добавление отсутствующих проверок связывания; затем другие незакрытые семейства и отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1156 — завершена сверка 53 зарегистрированных форм массивов

ArrayRemainingFormEvidenceTests добавляет 17 проверок точного BuiltinId и типа/ранга результата для оставшихся форм: сортировка/разворот с осью, заполнение, многомерное изменение размера, FLATTEN/RESHAPE/CONCAT и четыре агрегата. Многомерные POP/REMOVE возвращают одномерный срез; MAP/RESHAPE/CONCAT не подменяются скалярным типом. Дополнительно INTEGER-матрица 1/2/0/4 исполняется обоими движками: SUM=7, MIN=0, MAX=4, AVERAGE=1.75, исходные значения сохранены.

Вместе с независимыми ArrayFillSortReverseContractTests, ArrayDynamicShapeContractTests, ArrayShapeContractTests, AuditRemainingBehaviorTests и ArrayAggregateContractTests выполнено 117/117, без ошибок и пропусков, код 0 (.artifacts/array-1156-remaining.log/TRX). Исполняемый код/семантика не менялись, полный прогон не повторялся. git diff --check без ошибок.

ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv содержит свидетельства точного связывания и успешного ожидаемого выполнения для всех 53 регистраций (38 имён). Это завершение сверки зарегистрированных форм в указанном тестами объёме, а не доказательство всех типов, рангов, представлений, размеров и возможных комбинаций. Предыдущие числовые/Unicode/пустые/предельные проверки остаются дополнительными свидетельствами. Новые дубли существующих операций массивов не нужны.

Следующий шаг общего аудита — вернуться к отсутствующим возможностям: независимо уточнить G02 (строгая проверка числового текста) и существующие альтернативы, не менять префиксный VAL; затем G03 двоичный доступ и G04 Unicode с проверкой реальных сценариев. Остальные семейства общей матрицы также остаются в объёме. Общий аудит активен.


### Session 1158 — ISNUMERIC: полный числовой текст

G02 реализован в узком объёме: ISNUMERIC(text AS STRING) AS BOOLEAN проверяет полный инвариантный десятичный разбор в конечный DOUBLE. Использует общий BuiltinStringHelpers.IsNumeric для Interpreter и BytecodeVm (NumberStyles.Float, InvariantCulture, double.IsFinite). Обычные краевые пробелы U+0020 и U+0009–U+000D, знак, точка и экспонента разрешены; запятая, групповые разделители, валютные знаки, формы &H/&O, Unicode-цифры, NaN/Infinity и переполнение не принимаются. Пустая строка FALSE. Округление очень малого числа в конечный ноль TRUE; проверка не обещает точности либо пригодности для INTEGER/LONG/DECIMAL. Префиксный VAL сохранён. Новая функция не вводит строгого преобразования либо типовых перегрузок.

BuiltinFunctionId.IsNumeric добавлен в конец enum, без изменения существующих числовых значений сериализации. Дескриптор CreateDefault требует STRING и возвращает BOOLEAN. Оба движка используют один обработчик. Обе схемы IDE обновлены в алфавитном порядке; форматирование и словарь проверяются отдельной проверкой IsNumericIsHighlightedAndFormattedAsBuiltin и общей синхронизацией реестра.

IsNumericContractTests: 30 числовых/текстовых случаев в en-US/ru-RU/tr-TR и обоих движках, четыре неверные формы аргументов, точный BuiltinId/BOOLEAN и сохранение VAL. Совместно с NumericTextAlternativeEvidenceTests адресный набор 44/44. Первоначальная сборка выявила отсутствующий using, исправлен; первоначальные две проверки IDE выявили порядок слова в схемах, исправлен. Это ошибки текущего внесения, не отдельные ранее существовавшие дефекты. Полный прогон решения запущен: .artifacts/numeric-1158-full.log/TRX; результат будет дописан после завершения.

Manual isnumeric.html содержит назначение, контекст, синтаксис, аргумент, результат, ограничения, диагностику и inline-пример. Обновлены навигация, поисковый/алфавитный индекс и ссылки из числовых разделов. Русскоязычный samples/functions/isnumeric/full_number_check.axm показывает полный текст против префикса VAL, ноль, переполнение, точку/запятую и округление к нулю; выполнение будет подтверждено актуальным CLI.

Имя выбрано по подтверждённому прецеденту Visual Basic/VBA, а не заявлено как функция классического QuickBASIC. Поиск в доступном тексте Зельднера не выявил ISNUMERIC; VAL не предоставляет полного предиката. Основной жанровый/именной ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.information.isnumeric?view=net-10.0 и VBA https://learn.microsoft.com/en-us/office/vba/language/reference/user-interface-help/isnumeric-function . Договор Axiom сознательно уже VB: только STRING, конечный инвариантный десятичный DOUBLE; числовые/BOOLEAN-аргументы и шестнадцатеричные/восьмеричные формы VB не перенесены.

После завершения проверок следующий шаг — G03: независимо проверить отсутствие общего двоичного доступа и реальные сценарии, затем G04 Unicode и прочие незакрытые семейства общей матрицы. Общий аудит активен.


### Session 1159 — исправлена полнота инструмента проверки покрытия

COV26-001 P2: BytecodeCoverageAudit перечислял enum и TryGetDescriptor(id), который возвращает только первый дескриптор ID. Поэтому перегрузки с тем же ID и другим числом аргументов не проверялись, хотя отчёт обещал все варианты. Теперь инструмент перечисляет BuiltinFunctions.All и TryGetDescriptors(name), проверяет каждую регистрацию и выводит число вариантов: текущий реестр 250 имён/310 вариантов.

COV26-002 P3: индекс распознавался только по kind «Функция» и полному совпадению title с именем. Заголовки «Функция PMAP/POINT» и шесть групповых записей kind «Функции» ошибочно давали 22 пропуска и две лишние записи. В групповых строках search-index.js добавлено явное поле functions с именами сущностей. Инструмент поддерживает это поле и нормализует префикс «Функция » одиночного заголовка. Неизвестное явно указанное имя по-прежнему является ошибкой; произвольные ключевые слова не считаются свидетельством документации.

BytecodeCoverageAuditContractTests проверяет групповой индекс/префикс/полный счёт вариантов, неизвестное имя и сохранение отказа при отсутствующем тематическом каталоге. Три проверки 3/3, код 0 (.artifacts/coverage-1159-contract.log/TRX). Исходник инструмента подключён в тестовый проект через Compile Link; исполняемый язык не менялся. git diff --check без ошибок. Полное решение после этой правки инструмента не повторялось.

Фактический audit-bytecode-coverage: все 310 вариантов имеют VM-путь; все 250 имён найдены в manual-индексе, лишних индексных имён нет. Общий FAIL сохраняется из-за 27 отсутствующих каталогов samples/functions/<имя> (.artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-191448.txt). Наличие каталога само по себе не доказывает запуск/содержание, а его отсутствие не означает отсутствие примеров в другом тематическом разделе.

GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит кандидаты для этих 27 имён: для 18 найден текст вызова в существующих samples, для девяти прямого текстового кандидата нет. Это не подтверждение выполнения: комментарии, ошибки, зависимые ресурсы и графический контекст требуют чтения/запуска. При чтении встретились старые CP1251-примеры; они не изменялись.

Следующий шаг: разобрать девять имён без прямого кандидата и 18 существующих графических кандидатов, добавлять только содержательные короткие тематические примеры, не дублировать большие игры ради зелёного отчёта. G03/G04 и остальные семейства общего аудита остаются в объёме. Общий аудит активен.


### Session 1161 — закрыты оставшиеся каталоги графических функций

Добавлены 18 коротких русскоязычных примеров: IMAGEWIDTH/HEIGHT/LOADIMAGE, SPRITEWIDTH/HEIGHT/COUNT/LOADSPRITE, LOADTILESET/LOADTILEMAP, MAPWIDTH/HEIGHT/LAYERCOUNT/TILE, RGB, SCREENISACTIVE/MODE/WIDTH/HEIGHT. Ресурсные примеры используют существующие walk_sheet.png и simple_map.tmj, не копируют ресурсы и не требуют графического окна. RGB(1,2,3)=66051. Размер изображения 32×32, кадр 16×16, четыре кадра, карта 2×2 и три слоя; MAPTILE(1,2,1)=2.

TilePropertyAcceptanceSampleTests теперь исполняет все 27 тематических программ в обоих движках через настоящий ProgramRunService/ProgramConsoleService и разрешение sourceFilePath, без перехода на другой движок. Дополнительная пара проверок различает исходный нулевой размер экрана и явный SCREEN 0 с размером 640×300. Итог 56/56, код 0 (.artifacts/graphics-1161-final.log/TRX). Первоначальные ошибки примеров — зарезервированные image/sprite как имена переменных и ошибочное ожидание обычной панели вывода после SCREEN 0 — исправлены, дефектами языка не считались. После отказа от явного SCREEN выявлено неверное ожидание размеров 640×300 до его вызова: RuntimeInputSnapshot действительно начинается с 0×0. Ожидание сверено с кодом, отдельно проверено состояние после SCREEN 0; исполнитель не менялся.

Manual screen-functions уточняет: до первого оператора SCREEN размеры 0×0, после SCREEN 0 — 640×300, SCREENISACTIVE=FALSE. PRINT после SCREEN 0 направляется в экранное текстовое окно. В шести графических разделах добавлены ссылки на примеры; индексные имена уже присутствовали. GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит подтверждённые пути для всех 27 исходных пропусков и пределы проверки. Визуальная отрисовка/масштабирование/все графические формы этими примерами не доказаны.

Редакционный ориентир по экранному режиму — Зельднер, SCREEN, страница 228; современный запасной жанровый ориентир по свойствам экрана — https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.devices.computer.screen?view=windowsdesktop-10.0 . Для ресурсов сохранён ориентир My.Resources, использованный в Session 1160. Исторические режимы/семантика VB в Axiom не перенесены.

Повторный audit-bytecode-coverage: PASS (.artifacts/graphics-1161-coverage.log и указанный в нём отчёт). 250 имён, 310 вариантов, 250 индексных имён, 250 тематических каталогов; пропусков VM/индекса/каталогов и лишних имён нет. Это структурная проверка, а не доказательство всех форм, типов и сценариев; отдельно подтверждены только явно перечисленные программы. git diff --check чистый. Полное решение после добавления этих примеров не повторялось; исполняемый код не менялся.

Следующий шаг — G03: проверить границы существующих файловых API и потребность в общем двоичном доступе; затем G04 Unicode и оставшиеся семейства общего аудита. Не добавлять новые средства по одному лишь отсутствию имени в реестре. Общий аудит активен.


### Session 1167 — формы неполных Windows-путей
PATH26-001 исправлен в общем NormalizeFileSystemPath. Восемь исходных случаев обоих движков до исправления 0/8; расширенный набор после исправления 19/19, полное решение 4960/4960. FULLPATH проверен по базе программы, корню без диска, drive-relative, UNC, точкам, пустой строке и Unicode-именам; READFILE/PATHROOT/DIRNAME проверены на локальном временном файле. COMBINEPATH сохраняет лексический договор. Пример drive_relative_scope.axm и manual обновлены. Это не исчерпывающая проверка каждого файлового API, сетевого доступа или всех путей Windows. Подробности RELATIVE_PATH_ALTERNATIVES_20261008.md.


## Текущая сверка — Session 1168, 2026-10-08

Сверен текущий реестр: 314 дескрипторов, 250 имён, 299 зарегистрированных ID из 302. Все формы совпали с актуальными метаданными CLI; поддержка VM, отсутствие повторных сигнатур и обе схемы подсветки подтверждены структурно. Старые снимки сохранены. Исторический LA26-список больше не превращается автоматически в текущий BROKEN: HistoricalIssue отделён от UNVERIFIED_PER_FORM.

Добавлен режим --inventory-only и экспорт текущей таблицы без запуска старых сценариев. Новые документы: audit/LANGUAGE_FUNCTION_MATRIX_20261008.csv, audit/LANGUAGE_REGISTRY_RECONCILIATION_20261008.json, audit/CURRENT_FORM_AUDIT_20261008.md. У 190 форм есть кандидаты ссылок на специализированные CSV, у 124 таких ссылок нет; это не показатели семантической полноты или числа дефектов. Доказательства G04, RNG26-001 и PATH26-001 перечислены с границами; упоминания в тестах не повышены до проверки формы.

Проверки: инструмент собран и выполнен с кодом 0; экспорт сверил все параметры и политики с metadata, SHA256 Semantics совпал. Последний полный прогон остаётся 4960/4960 из Session 1167, девять TRX, без ошибок/пропусков; в этой сессии полный набор не повторялся, поведение языка не менялось. Следующий шаг — независимые проверки SEEKFILE/FILEPOS/FILELENGTH/EOF и READTEXT/READLINE на кодировках, Unicode и позициях. Общий аудит активен.
