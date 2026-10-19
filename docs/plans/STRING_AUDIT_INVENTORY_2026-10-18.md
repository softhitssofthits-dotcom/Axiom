# Строковые формы: приложение к аудиту 18 октября 2026 года

Снимок из `docs/manual/data/language-reference.json` на ревизии `9c1b436`. Это карта зарегистрированных связей, а не отметка прохождения всех форм. Включены все формы семейства, если хотя бы одна форма имеет STRING в аргументах или результате. Обобщённые функции массивов и инструкции учитываются отдельно в основном отчёте.

[Основной аудит и план](STRING_AUDIT_2026-10-18.md).

Семейств: **135**; форм: **187**. Основное строковое семейство с ASC/ASCW/CHR/CHRW: **32** имён, **46** форм.

| Имя / форма реестра | Аргументы | Результат | Справочник |
|---|---|---|---|
| APPENDFILE / AppendFile | path: STRING; text: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#appendfile) |
| APPENDFILE / AppendFileEncoding | path: STRING; text: STRING; encoding: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#appendfile) |
| ASC / Asc | text: STRING | INTEGER | [Описание](../manual/characters.html#contract) |
| ASCW / AscW | text: STRING | INTEGER | [Описание](../manual/characters.html#contract) |
| CENTER / Center | text: STRING; width: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#center) |
| CENTER / CenterFill | text: STRING; width: INTEGER; fill: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#center) |
| CHANGEDIR / ChangeDir | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#changedir) |
| CHR / Chr | code: INTEGER | STRING | [Описание](../manual/characters.html#contract) |
| CHRW / ChrW | code: INTEGER | STRING | [Описание](../manual/characters.html#contract) |
| COMBINEPATH / CombinePath | left: STRING; right: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#combinepath) |
| CONTAINS / Contains | text: STRING; fragment: STRING | BOOLEAN | [Описание](../manual/string-functions-search-and-replace.html#contains) |
| COPYDIR / CopyDir | source: STRING; destination: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#copydir) |
| COPYDIR / CopyDirOverwrite | source: STRING; destination: STRING; overwrite: BOOLEAN | BOOLEAN | [Описание](../manual/file-system-functions-management.html#copydir) |
| COPYFILE / CopyFile | source: STRING; destination: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#copyfile) |
| COPYFILE / CopyFileOverwrite | source: STRING; destination: STRING; overwrite: BOOLEAN | BOOLEAN | [Описание](../manual/file-system-functions-management.html#copyfile) |
| CURRENTDIR / CurrentDir | Нет аргументов | STRING | [Описание](../manual/file-system-functions-management.html#currentdir) |
| DATE / Date | Нет аргументов | DATE | [Описание](../manual/date-time-functions-current.html#date) |
| DATE / DateInZone | timeZone: STRING | DATE | [Описание](../manual/date-time-functions-current.html#date) |
| DATEADD / DateAddDateValue | interval: STRING; number: INTEGER; value: DATE | DATE | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#dateadd) |
| DATEADD / DateAddTimeValue | interval: STRING; number: INTEGER; value: TIME | TIME | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#dateadd) |
| DATEADD / DateAddDateTimeValue | interval: STRING; number: INTEGER; value: DATETIME | DATETIME | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#dateadd) |
| DATEDIFF / DateDiffDateValues | interval: STRING; fromValue: DATE; toValue: DATE | LONG | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datediff) |
| DATEDIFF / DateDiffTimeValues | interval: STRING; fromValue: TIME; toValue: TIME | LONG | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datediff) |
| DATEDIFF / DateDiffDateTimeValues | interval: STRING; fromValue: DATETIME; toValue: DATETIME | LONG | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datediff) |
| DATEPART / DatePartDateValue | part: STRING; value: DATE | INTEGER | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datepart) |
| DATEPART / DatePartTimeValue | part: STRING; value: TIME | INTEGER | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datepart) |
| DATEPART / DatePartDateTimeValue | part: STRING; value: DATETIME | INTEGER | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datepart) |
| DATETIMEVALUE / DateTimeValue | value: STRING | DATETIME | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datetimevalue) |
| DATEVALUE / DateValue | value: STRING | DATE | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datevalue) |
| DATEVALUE / DateValueFromDateTime | value: DATETIME | DATE | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#datevalue) |
| DELETEDIR / DeleteDir | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#deletedir) |
| DELETEDIRRECURSIVE / DeleteDirRecursive | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#deletedirrecursive) |
| DELETEFILE / DeleteFile | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#deletefile) |
| DIRDATE / DirDate | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#dirdate) |
| DIRDATEUTC / DirDateUtc | path: STRING | UTCDATETIME | [Описание](../manual/file-system-functions-paths.html#dirdateutc) |
| DIREXISTS / DirExists | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#direxists) |
| DIRNAME / DirName | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#dirname) |
| DIRSIZE / DirSize | path: STRING | LONG | [Описание](../manual/file-system-functions-management.html#dirsize) |
| DRIVEFORMAT / DriveFormat | path: STRING | STRING | [Описание](../manual/file-system-functions-management.html#driveformat) |
| DRIVENAME / DriveName | path: STRING | STRING | [Описание](../manual/file-system-functions-management.html#drivename) |
| DRIVETYPE / DriveType | path: STRING | STRING | [Описание](../manual/file-system-functions-management.html#drivetype) |
| ENDSWITH / EndsWith | text: STRING; fragment: STRING | BOOLEAN | [Описание](../manual/string-functions-search-and-replace.html#endswith) |
| EXTNAME / ExtName | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#extname) |
| FILEACCESSEDDATE / FileAccessDate | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#fileaccesseddate) |
| FILEACCESSEDDATEUTC / FileAccessDateUtc | path: STRING | UTCDATETIME | [Описание](../manual/file-system-functions-paths.html#fileaccesseddateutc) |
| FILECREATEDATE / FileCreateDate | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#filecreatedate) |
| FILECREATEDATEUTC / FileCreateDateUtc | path: STRING | UTCDATETIME | [Описание](../manual/file-system-functions-paths.html#filecreatedateutc) |
| FILEDATE / FileDate | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#filedate) |
| FILEDATEUTC / FileDateUtc | path: STRING | UTCDATETIME | [Описание](../manual/file-system-functions-paths.html#filedateutc) |
| FILEEXISTS / FileExists | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#fileexists) |
| FILEMODE / FileMode | file: FILE | STRING | [Описание](../manual/file-system-functions-open-files.html#filemode) |
| FILENAME / FileName | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#filename) |
| FILEPATH / FilePath | file: FILE | STRING | [Описание](../manual/file-system-functions-open-files.html#filepath) |
| FILESIZE / FileSize | path: STRING | LONG | [Описание](../manual/file-system-functions-paths.html#filesize) |
| FREESPACE / FreeSpace | path: STRING | LONG | [Описание](../manual/file-system-functions-management.html#freespace) |
| FROMINSTANT / FromInstant | value: UTCDATETIME | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#frominstant) |
| FROMINSTANT / FromInstantZone | value: UTCDATETIME; timeZone: STRING | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#frominstant) |
| FROMUTC / FromUtc | value: DATETIME | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#fromutc) |
| FROMUTC / FromUtcZone | value: DATETIME; timeZone: STRING | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#fromutc) |
| FULLPATH / FullPath | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#fullpath) |
| GETDIRS / GetDirs | path: STRING | STRING() | [Описание](../manual/file-system-functions-paths.html#getdirs) |
| GETDIRS / GetDirsPattern | path: STRING; pattern: STRING | STRING() | [Описание](../manual/file-system-functions-paths.html#getdirs) |
| GETFILES / GetFiles | path: STRING | STRING() | [Описание](../manual/file-system-functions-paths.html#getfiles) |
| GETFILES / GetFilesPattern | path: STRING; pattern: STRING | STRING() | [Описание](../manual/file-system-functions-paths.html#getfiles) |
| INKEY / InKey | Нет аргументов | STRING | [Описание](../manual/inkey.html) |
| INSERT / Insert | text: STRING; start: INTEGER; fragment: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#insert) |
| INSTANTVALUE / InstantValue | value: STRING | UTCDATETIME | [Описание](../manual/date-time-functions-time-zones.html#instantvalue) |
| INSTR / InStr | text: STRING; fragment: STRING | INTEGER | [Описание](../manual/string-functions-search-and-replace.html#instr) |
| INSTR / InStrFrom | start: INTEGER; text: STRING; fragment: STRING | INTEGER | [Описание](../manual/string-functions-search-and-replace.html#instr) |
| INSTRREV / InStrRev | text: STRING; fragment: STRING | INTEGER | [Описание](../manual/string-functions-search-and-replace.html#instrrev) |
| INSTRREV / InStrRevFrom | start: INTEGER; text: STRING; fragment: STRING | INTEGER | [Описание](../manual/string-functions-search-and-replace.html#instrrev) |
| ISABSOLUTEPATH / IsAbsolutePath | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-paths.html#isabsolutepath) |
| ISDATE / IsDate | value: STRING | BOOLEAN | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#isdate) |
| ISHIDDEN / IsHidden | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#ishidden) |
| ISNUMERIC / IsNumeric | text: STRING | BOOLEAN | [Описание](../manual/isnumeric.html#isnumeric) |
| ISREADONLY / IsReadOnly | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#isreadonly) |
| JOIN / Join | items: STRING() | STRING | [Описание](../manual/string-functions-build-and-split.html#join) |
| JOIN / JoinSeparator | items: STRING(); separator: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#join) |
| KEYDOWN / KeyDown | keyText: STRING | BOOLEAN | [Описание](../manual/keydown.html) |
| KEYPRESS / KeyPress | keyText: STRING | BOOLEAN | [Описание](../manual/keypress.html) |
| LCASE / LCase | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#lcase) |
| LEFT / Left | text: STRING; count: INTEGER | STRING | [Описание](../manual/string-functions-slices-and-length.html#left) |
| LEFT / LeftUnits | text: STRING; count: INTEGER; unit: STRING | STRING | [Описание](../manual/string-functions-slices-and-length.html#left) |
| LEN / Len | text: STRING | INTEGER | [Описание](../manual/string-functions-slices-and-length.html#len) |
| LEN / LenUnits | text: STRING; unit: STRING | INTEGER | [Описание](../manual/string-functions-slices-and-length.html#len) |
| LOADIMAGE / LoadImage | path: STRING | IMAGE | [Описание](../manual/images.html#loadimage) |
| LOADSPRITE / LoadSprite | path: STRING; frameWidth: LONG; frameHeight: LONG | SPRITE | [Описание](../manual/sprites.html#loadsprite) |
| LOADTILEMAP / LoadTilemap | path: STRING | TILEMAP | [Описание](../manual/tilemaps.html#loadtilemap) |
| LOADTILESET / LoadTileset | path: STRING; tileWidth: LONG; tileHeight: LONG | TILESET | [Описание](../manual/tilesets.html#loadtileset) |
| LOADTILESET / LoadTilesetMargin | path: STRING; tileWidth: LONG; tileHeight: LONG; margin: LONG | TILESET | [Описание](../manual/tilesets.html#loadtileset) |
| LOADTILESET / LoadTilesetMarginSpacing | path: STRING; tileWidth: LONG; tileHeight: LONG; margin: LONG; spacing: LONG | TILESET | [Описание](../manual/tilesets.html#loadtileset) |
| LTRIM / LTrimWhitespace | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#ltrim) |
| LTRIM / LTrimCharacters | text: STRING; chars: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#ltrim) |
| MAKEDIR / MakeDir | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#makedir) |
| MAKETEMPDIR / MakeTempDir | prefix: STRING | STRING | [Описание](../manual/file-system-functions-management.html#maketempdir) |
| MAKETEMPFILE / MakeTempFile | prefix: STRING | STRING | [Описание](../manual/file-system-functions-management.html#maketempfile) |
| MAPLAYERNAME / MapLayerName | tilemap: TILEMAP; layerIndex: LONG | STRING | [Описание](../manual/tilemaps.html#maplayername) |
| MAPLAYERPROPERTY / MapLayerProperty | tilemap: TILEMAP; layerIndex: LONG; name: STRING | STRING | [Описание](../manual/tilemaps.html#maplayerproperty) |
| MAPLAYERTYPE / MapLayerType | tilemap: TILEMAP; layerIndex: LONG | STRING | [Описание](../manual/tilemaps.html#maplayertype) |
| MAPOBJECTCLASS / MapObjectClass | tilemap: TILEMAP; layerIndex: LONG; objectIndex: LONG | STRING | [Описание](../manual/tilemaps.html#mapobjectclass) |
| MAPOBJECTKIND / MapObjectKind | tilemap: TILEMAP; layerIndex: LONG; objectIndex: LONG | STRING | [Описание](../manual/tilemaps.html#mapobjectkind) |
| MAPOBJECTNAME / MapObjectName | tilemap: TILEMAP; layerIndex: LONG; objectIndex: LONG | STRING | [Описание](../manual/tilemaps.html#mapobjectname) |
| MAPOBJECTPROPERTY / MapObjectProperty | tilemap: TILEMAP; layerIndex: LONG; objectIndex: LONG; name: STRING | STRING | [Описание](../manual/tilemaps.html#mapobjectproperty) |
| MAPPROPERTY / MapProperty | tilemap: TILEMAP; name: STRING | STRING | [Описание](../manual/tilemaps.html#mapproperty) |
| MAPTILEPROPERTY / MapTileProperty | tilemap: TILEMAP; layerIndex: LONG; column: LONG; row: LONG; name: STRING | STRING | [Описание](../manual/tilemaps.html#maptileproperty) |
| MID / Mid | text: STRING; start: INTEGER; count: INTEGER | STRING | [Описание](../manual/string-functions-slices-and-length.html#mid) |
| MID / MidUnits | text: STRING; start: INTEGER; count: INTEGER; unit: STRING | STRING | [Описание](../manual/string-functions-slices-and-length.html#mid) |
| MONTHNAME / MonthName | month: INTEGER | STRING | [Описание](../manual/date-time-functions-parts-and-serial.html#monthname) |
| MOVEDIR / MoveDir | source: STRING; destination: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#movedir) |
| MOVEFILE / MoveFile | source: STRING; destination: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#movefile) |
| MOVEFILE / MoveFileOverwrite | source: STRING; destination: STRING; overwrite: BOOLEAN | BOOLEAN | [Описание](../manual/file-system-functions-management.html#movefile) |
| NOW / Now | Нет аргументов | DATETIME | [Описание](../manual/date-time-functions-current.html#now) |
| NOW / NowInZone | timeZone: STRING | DATETIME | [Описание](../manual/date-time-functions-current.html#now) |
| PADLEFT / PadLeft | text: STRING; width: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#padleft) |
| PADLEFT / PadLeftFill | text: STRING; width: INTEGER; fill: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#padleft) |
| PADRIGHT / PadRight | text: STRING; width: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#padright) |
| PADRIGHT / PadRightFill | text: STRING; width: INTEGER; fill: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#padright) |
| PATHEXISTS / PathExists | path: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#pathexists) |
| PATHROOT / PathRoot | path: STRING | STRING | [Описание](../manual/file-system-functions-paths.html#pathroot) |
| READALLLINES / ReadAllLines | path: STRING | STRING() | [Описание](../manual/file-system-functions-text.html#readalllines) |
| READALLLINES / ReadAllLinesEncoding | path: STRING; encoding: STRING | STRING() | [Описание](../manual/file-system-functions-text.html#readalllines) |
| READFILE / ReadFile | path: STRING | STRING | [Описание](../manual/file-system-functions-text.html#readfile) |
| READFILE / ReadFileEncoding | path: STRING; encoding: STRING | STRING | [Описание](../manual/file-system-functions-text.html#readfile) |
| READLINE / ReadLine | file: FILE | STRING | [Описание](../manual/file-system-functions-open-files.html#readline) |
| READLINES / ReadLines | file: FILE; count: INTEGER | STRING() | [Описание](../manual/file-system-functions-open-files.html#readlines) |
| READTEXT / ReadText | file: FILE; count: INTEGER | STRING | [Описание](../manual/file-system-functions-open-files.html#readtext) |
| READTOEND / ReadToEnd | file: FILE | STRING | [Описание](../manual/file-system-functions-open-files.html#readtoend) |
| REGEXCOUNT / RegexCount | text: STRING; pattern: STRING | INTEGER | [Описание](../manual/regex-functions-replace-and-count.html#regexcount) |
| REGEXCOUNT / RegexCountModes | text: STRING; pattern: STRING; modes: STRING | INTEGER | [Описание](../manual/regex-functions-replace-and-count.html#regexcount) |
| REGEXEXTRACT / RegexExtract | text: STRING; pattern: STRING; group: INTEGER | STRING | [Описание](../manual/regex-functions-match-and-extract.html#regexextract) |
| REGEXEXTRACT / RegexExtractModes | text: STRING; pattern: STRING; group: INTEGER; modes: STRING | STRING | [Описание](../manual/regex-functions-match-and-extract.html#regexextract) |
| REGEXEXTRACTALL / RegexExtractAll | text: STRING; pattern: STRING; group: INTEGER | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexextractall) |
| REGEXEXTRACTALL / RegexExtractAllModes | text: STRING; pattern: STRING; group: INTEGER; modes: STRING | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexextractall) |
| REGEXISMATCH / RegexIsMatch | text: STRING; pattern: STRING | BOOLEAN | [Описание](../manual/regex-functions-match-and-extract.html#regexismatch) |
| REGEXISMATCH / RegexIsMatchModes | text: STRING; pattern: STRING; modes: STRING | BOOLEAN | [Описание](../manual/regex-functions-match-and-extract.html#regexismatch) |
| REGEXMATCH / RegexMatch | text: STRING; pattern: STRING | STRING | [Описание](../manual/regex-functions-match-and-extract.html#regexmatch) |
| REGEXMATCH / RegexMatchModes | text: STRING; pattern: STRING; modes: STRING | STRING | [Описание](../manual/regex-functions-match-and-extract.html#regexmatch) |
| REGEXMATCHES / RegexMatches | text: STRING; pattern: STRING | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexmatches) |
| REGEXMATCHES / RegexMatchesModes | text: STRING; pattern: STRING; modes: STRING | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexmatches) |
| REGEXREPLACE / RegexReplace | text: STRING; pattern: STRING; replacement: STRING | STRING | [Описание](../manual/regex-functions-replace-and-count.html#regexreplace) |
| REGEXREPLACE / RegexReplaceModes | text: STRING; pattern: STRING; replacement: STRING; modes: STRING | STRING | [Описание](../manual/regex-functions-replace-and-count.html#regexreplace) |
| REGEXREPLACE / RegexReplaceCount | text: STRING; pattern: STRING; replacement: STRING; count: INTEGER | STRING | [Описание](../manual/regex-functions-replace-and-count.html#regexreplace) |
| REGEXREPLACE / RegexReplaceModesCount | text: STRING; pattern: STRING; replacement: STRING; modes: STRING; count: INTEGER | STRING | [Описание](../manual/regex-functions-replace-and-count.html#regexreplace) |
| REGEXSPLIT / RegexSplit | text: STRING; pattern: STRING | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexsplit) |
| REGEXSPLIT / RegexSplitModes | text: STRING; pattern: STRING; modes: STRING | STRING() | [Описание](../manual/regex-functions-arrays-and-split.html#regexsplit) |
| REMOVE / Remove | text: STRING; start: INTEGER; count: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#remove) |
| RENAMEDIR / RenameDir | path: STRING; newName: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#renamedir) |
| RENAMEFILE / RenameFile | path: STRING; newName: STRING | BOOLEAN | [Описание](../manual/file-system-functions-management.html#renamefile) |
| REPEAT / Repeat | text: STRING; count: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#repeat) |
| REPLACE / Replace | text: STRING; oldText: STRING; newText: STRING | STRING | [Описание](../manual/string-functions-search-and-replace.html#replace) |
| REPLACEMANY / ReplaceMany | text: STRING; oldItems: STRING(); newItems: STRING() | STRING | [Описание](../manual/string-functions-search-and-replace.html#replacemany) |
| REVERSE / Reverse | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#reverse) |
| RIGHT / Right | text: STRING; count: INTEGER | STRING | [Описание](../manual/string-functions-slices-and-length.html#right) |
| RIGHT / RightUnits | text: STRING; count: INTEGER; unit: STRING | STRING | [Описание](../manual/string-functions-slices-and-length.html#right) |
| RTRIM / RTrimWhitespace | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#rtrim) |
| RTRIM / RTrimCharacters | text: STRING; chars: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#rtrim) |
| SPACE / Space | count: INTEGER | STRING | [Описание](../manual/string-functions-build-and-split.html#space) |
| SPLIT / Split | text: STRING | STRING() | [Описание](../manual/string-functions-build-and-split.html#split) |
| SPLIT / SplitSeparator | text: STRING; separator: STRING | STRING() | [Описание](../manual/string-functions-build-and-split.html#split) |
| STARTSWITH / StartsWith | text: STRING; fragment: STRING | BOOLEAN | [Описание](../manual/string-functions-search-and-replace.html#startswith) |
| STR / Str | value: DOUBLE | STRING | [Описание](../manual/numeric-functions-conversion-and-legacy.html#str) |
| STRING / StringFill | count: INTEGER; fill: STRING | STRING | [Описание](../manual/string-functions-build-and-split.html#string) |
| SUBSTRCOUNT / SubStrCount | text: STRING; fragment: STRING | INTEGER | [Описание](../manual/string-functions-search-and-replace.html#substrcount) |
| TEXTHEIGHT / TextHeight | text: STRING | INTEGER | [Описание](../manual/modern-graphics.html) |
| TEXTWIDTH / TextWidth | text: STRING | INTEGER | [Описание](../manual/modern-graphics.html) |
| TIME / Time | Нет аргументов | TIME | [Описание](../manual/date-time-functions-current.html#time) |
| TIME / TimeInZone | timeZone: STRING | TIME | [Описание](../manual/date-time-functions-current.html#time) |
| TIMEVALUE / TimeValue | value: STRING | TIME | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#timevalue) |
| TIMEVALUE / TimeValueFromDateTime | value: DATETIME | TIME | [Описание](../manual/date-time-functions-arithmetic-and-validation.html#timevalue) |
| TOINSTANT / ToInstant | value: DATETIME | UTCDATETIME | [Описание](../manual/date-time-functions-time-zones.html#toinstant) |
| TOINSTANT / ToInstantZone | value: DATETIME; timeZone: STRING | UTCDATETIME | [Описание](../manual/date-time-functions-time-zones.html#toinstant) |
| TOTALFREESPACE / TotalFreeSpace | path: STRING | LONG | [Описание](../manual/file-system-functions-management.html#totalfreespace) |
| TOTALSPACE / TotalSpace | path: STRING | LONG | [Описание](../manual/file-system-functions-management.html#totalspace) |
| TOUTC / ToUtc | value: DATETIME | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#toutc) |
| TOUTC / ToUtcZone | value: DATETIME; timeZone: STRING | DATETIME | [Описание](../manual/date-time-functions-time-zones.html#toutc) |
| TRIM / TrimWhitespace | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#trim) |
| TRIM / TrimCharacters | text: STRING; chars: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#trim) |
| UCASE / UCase | text: STRING | STRING | [Описание](../manual/string-functions-trim-and-case.html#ucase) |
| UTCOFFSET / UtcOffset | Нет аргументов | INTEGER | [Описание](../manual/date-time-functions-time-zones.html#utcoffset) |
| UTCOFFSET / UtcOffsetValue | value: DATETIME | INTEGER | [Описание](../manual/date-time-functions-time-zones.html#utcoffset) |
| UTCOFFSET / UtcOffsetValueZone | value: DATETIME; timeZone: STRING | INTEGER | [Описание](../manual/date-time-functions-time-zones.html#utcoffset) |
| VAL / Val | text: STRING | DOUBLE | [Описание](../manual/numeric-functions-conversion-and-legacy.html#val) |
| WEEKDAYNAME / WeekdayName | weekday: INTEGER | STRING | [Описание](../manual/date-time-functions-parts-and-serial.html#weekdayname) |
| WRITE / Write | file: FILE; text: STRING | BOOLEAN | [Описание](../manual/file-system-functions-open-files.html#write) |
| WRITEFILE / WriteFile | path: STRING; text: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#writefile) |
| WRITEFILE / WriteFileEncoding | path: STRING; text: STRING; encoding: STRING | BOOLEAN | [Описание](../manual/file-system-functions-text.html#writefile) |
| WRITELINE / WriteLine | file: FILE; text: STRING | BOOLEAN | [Описание](../manual/file-system-functions-open-files.html#writeline) |
