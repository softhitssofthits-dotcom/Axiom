

# Независимая матрица реестра — 2026-10-09

Получена из загруженного реестра, включая перегрузки. Тип результата при специальной политике определяется связывателем; объявленный тип не является окончательным. Упоминания в тестах, примерах и справочнике — карта поиска, не доказательство выполнения. Семантический статус каждой строки: UNVERIFIED_PER_FORM. Историческая находка хранится отдельно и не означает текущий дефект.

Дескрипторов: 335; имён: 271; ID: 323; незарегистрированные ID: DateAdd, DateDiff, DatePart.

| Имя | ID | Параметры | Объявленный результат | Связывание / операнды / результат | BytecodeVm | Тесты / примеры / manual | Подсветка светлая / тёмная |
|---|---|---|---|---|---|---|---|
| LBOUND | LBound | array: INTEGER() | INTEGER | Array / Declared / Declared | True | 41 / 57 / 9 | True / True |
| LBOUND | LBound | array: INTEGER(), dimension: INTEGER | INTEGER | Array / Declared / Declared | True | 41 / 57 / 9 | True / True |
| UBOUND | UBound | array: INTEGER() | INTEGER | Array / Declared / Declared | True | 41 / 59 / 10 | True / True |
| UBOUND | UBound | array: INTEGER(), dimension: INTEGER | INTEGER | Array / Declared / Declared | True | 41 / 59 / 10 | True / True |
| ARRAYLENGTH | ArrayLength | array: INTEGER() | INTEGER | Array / Declared / Declared | True | 38 / 57 / 6 | True / True |
| ARRAYISALLOCATED | ArrayIsAllocated | array: INTEGER() | BOOLEAN | Array / Declared / Declared | True | 21 / 14 / 4 | True / True |
| ARRAYRANK | ArrayRank | array: INTEGER() | INTEGER | Array / Declared / Declared | True | 11 / 5 / 4 | True / True |
| ARRAYBOUNDS | ArrayBounds | array: INTEGER() | INTEGER(,) | Array / Declared / Declared | True | 8 / 2 / 5 | True / True |
| ARRAYCLONE | ArrayClone | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | True | 9 / 5 / 2 | True / True |
| ARRAYSLICE | ArraySlice | array: INTEGER(), start: INTEGER, count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 6 / 4 / 4 | True / True |
| ARRAYSLICE | ArraySlice | array: INTEGER(), start: INTEGER, count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 6 / 4 / 4 | True / True |
| ARRAYTOSORTED | ArrayToSorted | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | True | 10 / 6 / 1 | True / True |
| ARRAYTOSORTED | ArrayToSorted | array: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 10 / 6 / 1 | True / True |
| ARRAYTOREVERSED | ArrayToReversed | array: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | True | 9 / 4 / 1 | True / True |
| ARRAYTOREVERSED | ArrayToReversed | array: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 9 / 4 / 1 | True / True |
| ARRAYTAKE | ArrayTake | array: INTEGER(), count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 15 / 8 / 2 | True / True |
| ARRAYTAKE | ArrayTake | array: INTEGER(), count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 15 / 8 / 2 | True / True |
| ARRAYSKIP | ArraySkip | array: INTEGER(), count: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 7 / 6 / 2 | True / True |
| ARRAYSKIP | ArraySkip | array: INTEGER(), count: INTEGER, dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 7 / 6 / 2 | True / True |
| ARRAYFLATTEN | ArrayFlatten | array: INTEGER() | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | True | 9 / 5 / 1 | True / True |
| ARRAYRESHAPE | ArrayReshape | source: INTEGER(), template: INTEGER() | INTEGER() | Array / Declared / SameAsSecondArgument | True | 8 / 4 / 1 | True / True |
| ARRAYDISTINCT | ArrayDistinct | array: INTEGER() | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | True | 7 / 3 / 1 | True / True |
| ARRAYCONTAINS | ArrayContains | array: INTEGER(), value: INTEGER | BOOLEAN | Array / Declared / Declared | True | 8 / 5 / 1 | True / True |
| ARRAYINDEXOF | ArrayIndexOf | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | True | 7 / 5 / 1 | True / True |
| ARRAYLASTINDEXOF | ArrayLastIndexOf | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | True | 7 / 5 / 1 | True / True |
| ARRAYCOUNT | ArrayCount | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / Declared | True | 7 / 4 / 1 | True / True |
| ARRAYFINDALL | ArrayFindAll | array: INTEGER(), value: INTEGER | INTEGER() | Array / Declared / ArrayFindAllCoordinateResult | True | 7 / 3 / 1 | True / True |
| ARRAYBINARYSEARCH | ArrayBinarySearch | array: INTEGER(), value: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | True | 8 / 4 / 1 | True / True |
| ARRAYBINARYSEARCH | ArrayBinarySearch | array: INTEGER(), value: INTEGER, dimension: INTEGER | INTEGER | Array / Declared / ArrayIndexCoordinateResult | True | 8 / 4 / 1 | True / True |
| ARRAYFILL | ArrayFill | array: INTEGER(), value: INTEGER | VOID | Array / Declared / Declared | True | 5 / 2 / 1 | True / True |
| ARRAYREVERSE | ArrayReverse | array: INTEGER() | VOID | Array / Declared / Declared | True | 8 / 3 / 1 | True / True |
| ARRAYREVERSE | ArrayReverse | array: INTEGER(), dimension: INTEGER | VOID | Array / Declared / Declared | True | 8 / 3 / 1 | True / True |
| ARRAYSORT | ArraySort | array: INTEGER() | VOID | Array / Declared / Declared | True | 10 / 4 / 1 | True / True |
| ARRAYSORT | ArraySort | array: INTEGER(), dimension: INTEGER | VOID | Array / Declared / Declared | True | 10 / 4 / 1 | True / True |
| ARRAYPUSH | ArrayPush | array: INTEGER(), value: INTEGER | VOID | Array / Declared / Declared | True | 8 / 6 / 5 | True / True |
| ARRAYPUSH | ArrayPush | array: INTEGER(), value: INTEGER, dimension: INTEGER | VOID | Array / Declared / Declared | True | 8 / 6 / 5 | True / True |
| ARRAYPOP | ArrayPop | array: INTEGER() | INTEGER | Array / Declared / ArrayDynamicSliceResult | True | 9 / 5 / 4 | True / True |
| ARRAYPOP | ArrayPop | array: INTEGER(), dimension: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | True | 9 / 5 / 4 | True / True |
| ARRAYINSERT | ArrayInsert | array: INTEGER(), index: INTEGER, value: INTEGER | VOID | Array / Declared / Declared | True | 8 / 5 / 5 | True / True |
| ARRAYINSERT | ArrayInsert | array: INTEGER(), index: INTEGER, value: INTEGER, dimension: INTEGER | VOID | Array / Declared / Declared | True | 8 / 5 / 5 | True / True |
| ARRAYREMOVE | ArrayRemove | array: INTEGER(), index: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | True | 9 / 4 / 4 | True / True |
| ARRAYREMOVE | ArrayRemove | array: INTEGER(), index: INTEGER, dimension: INTEGER | INTEGER | Array / Declared / ArrayDynamicSliceResult | True | 9 / 4 / 4 | True / True |
| ARRAYEVERY | ArrayEvery | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | BOOLEAN | Array / Declared / Declared | True | 7 / 5 / 1 | True / True |
| ARRAYSOME | ArraySome | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | BOOLEAN | Array / Declared / Declared | True | 7 / 5 / 1 | True / True |
| ARRAYFILTER | ArrayFilter | array: INTEGER(), predicate: FUNCTION(INTEGER) AS BOOLEAN | INTEGER() | Array / Declared / ArrayOfFirstArrayElementType | True | 8 / 5 / 1 | True / True |
| ARRAYMAP | ArrayMap | array: INTEGER(), selector: FUNCTION(INTEGER) AS INTEGER | INTEGER() | Array / Declared / ArrayMapSelectorReturnArray | True | 10 / 8 / 1 | True / True |
| ARRAYREDUCE | ArrayReduce | array: INTEGER(), seed: INTEGER, reducer: FUNCTION(INTEGER, INTEGER) AS INTEGER | INTEGER | Array / Declared / ArrayReduceAccumulator | True | 8 / 7 / 1 | True / True |
| ARRAYCONCAT | ArrayConcat | left: INTEGER(), right: INTEGER() | INTEGER() | Array / Declared / SameAsFirstArgument | True | 8 / 6 / 1 | True / True |
| ARRAYCONCAT | ArrayConcat | left: INTEGER(), right: INTEGER(), dimension: INTEGER | INTEGER() | Array / Declared / SameAsFirstArgument | True | 8 / 6 / 1 | True / True |
| ARRAYSUM | ArraySum | array: INTEGER() | INTEGER | Array / Declared / ElementTypeOfFirstArrayArgument | True | 8 / 4 / 1 | True / True |
| ARRAYMIN | ArrayMin | array: INTEGER() | INTEGER | Array / Declared / ElementTypeOfFirstArrayArgument | True | 7 / 4 / 1 | True / True |
| ARRAYMAX | ArrayMax | array: INTEGER() | INTEGER | Array / Declared / ElementTypeOfFirstArrayArgument | True | 7 / 4 / 1 | True / True |
| ARRAYAVERAGE | ArrayAverage | array: INTEGER() | DOUBLE | Array / Declared / Double | True | 6 / 6 / 1 | True / True |
| INKEY | InKey |  | STRING | Default / Declared / Declared | True | 7 / 8 / 8 | True / True |
| KEYDOWN | KeyDown | keyText: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 5 / 8 | True / True |
| KEYPRESS | KeyPress | keyText: STRING | BOOLEAN | Default / Declared / Declared | True | 6 / 26 / 7 | True / True |
| MOUSEX | MouseX |  | LONG | Default / Declared / Declared | True | 12 / 5 / 7 | True / True |
| MOUSEY | MouseY |  | LONG | Default / Declared / Declared | True | 12 / 5 / 7 | True / True |
| MOUSEBUTTON | MouseButton | buttonNumber: INTEGER | BOOLEAN | Default / Declared / Declared | True | 9 / 2 / 7 | True / True |
| MOUSEPRESS | MousePress | buttonNumber: INTEGER | BOOLEAN | Default / Declared / Declared | True | 7 / 1 / 7 | True / True |
| SCREENISACTIVE | ScreenIsActive |  | BOOLEAN | Default / Declared / Declared | True | 3 / 3 / 1 | True / True |
| SCREENMODE | ScreenMode |  | INTEGER | Default / Declared / Declared | True | 10 / 3 / 1 | True / True |
| SCREENWIDTH | ScreenWidth |  | INTEGER | Default / Declared / Declared | True | 12 / 6 / 3 | True / True |
| SCREENHEIGHT | ScreenHeight |  | INTEGER | Default / Declared / Declared | True | 12 / 6 / 3 | True / True |
| TEXTWIDTH | TextWidth | text: STRING | INTEGER | Default / Declared / Declared | True | 6 / 1 / 2 | True / True |
| TEXTHEIGHT | TextHeight | text: STRING | INTEGER | Default / Declared / Declared | True | 6 / 1 / 2 | True / True |
| POINT | PointPosition | number: INTEGER | INTEGER | Default / Declared / Declared | True | 20 / 20 / 12 | True / True |
| POINT | Point | x: LONG, y: LONG | INTEGER | Default / Declared / Declared | True | 20 / 20 / 12 | True / True |
| PMAP | PMap | coordinate: DOUBLE, selector: INTEGER | DOUBLE | Default / Declared / Declared | True | 7 / 2 / 9 | True / True |
| LOADIMAGE | LoadImage | path: STRING | IMAGE | Default / Declared / Declared | True | 10 / 11 / 5 | True / True |
| IMAGEWIDTH | ImageWidth | image: IMAGE | INTEGER | Default / Declared / Declared | True | 9 / 5 / 3 | True / True |
| IMAGEHEIGHT | ImageHeight | image: IMAGE | INTEGER | Default / Declared / Declared | True | 9 / 3 / 3 | True / True |
| LOADSPRITE | LoadSprite | path: STRING, frameWidth: LONG, frameHeight: LONG | SPRITE | Default / Declared / Declared | True | 10 / 12 / 5 | True / True |
| SPRITEWIDTH | SpriteWidth | sprite: SPRITE | INTEGER | Default / Declared / Declared | True | 6 / 2 / 2 | True / True |
| SPRITEHEIGHT | SpriteHeight | sprite: SPRITE | INTEGER | Default / Declared / Declared | True | 6 / 2 / 2 | True / True |
| SPRITECOUNT | SpriteCount | sprite: SPRITE | INTEGER | Default / Declared / Declared | True | 6 / 5 / 2 | True / True |
| LOADTILESET | LoadTileset | path: STRING, tileWidth: LONG, tileHeight: LONG | TILESET | Default / Declared / Declared | True | 11 / 6 / 5 | True / True |
| LOADTILESET | LoadTilesetMargin | path: STRING, tileWidth: LONG, tileHeight: LONG, margin: LONG | TILESET | Default / Declared / Declared | True | 11 / 6 / 5 | True / True |
| LOADTILESET | LoadTilesetMarginSpacing | path: STRING, tileWidth: LONG, tileHeight: LONG, margin: LONG, spacing: LONG | TILESET | Default / Declared / Declared | True | 11 / 6 / 5 | True / True |
| LOADTILEMAP | LoadTilemap | path: STRING | TILEMAP | Default / Declared / Declared | True | 21 / 42 / 5 | True / True |
| TILECOUNT | TileCount | tileset: TILESET | INTEGER | Default / Declared / Declared | True | 9 / 3 / 3 | True / True |
| TILEWIDTH | TileWidth | tileset: TILESET | INTEGER | Default / Declared / Declared | True | 14 / 1 / 3 | True / True |
| TILEHEIGHT | TileHeight | tileset: TILESET | INTEGER | Default / Declared / Declared | True | 14 / 1 / 3 | True / True |
| MAPWIDTH | MapWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 11 / 7 / 2 | True / True |
| MAPHEIGHT | MapHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 2 / 2 | True / True |
| MAPPIXELWIDTH | MapPixelWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| MAPPIXELHEIGHT | MapPixelHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| MAPTILEWIDTH | MapTileWidth | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| MAPTILEHEIGHT | MapTileHeight | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| MAPLAYERCOUNT | MapLayerCount | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 5 / 3 / 2 | True / True |
| MAPTILE | MapTile | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | True | 8 / 3 / 4 | True / True |
| MAPTILESET | MapTileset | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | True | 6 / 2 / 4 | True / True |
| MAPTILEFLAGS | MapTileFlags | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG | INTEGER | Default / Declared / Declared | True | 7 / 2 / 4 | True / True |
| MAPTILESETCOUNT | MapTilesetCount | tilemap: TILEMAP | INTEGER | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPPROPERTY | MapProperty | tilemap: TILEMAP, name: STRING | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPLAYERPROPERTY | MapLayerProperty | tilemap: TILEMAP, layerIndex: LONG, name: STRING | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPTILEPROPERTY | MapTileProperty | tilemap: TILEMAP, layerIndex: LONG, column: LONG, row: LONG, name: STRING | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTPROPERTY | MapObjectProperty | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG, name: STRING | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPLAYERNAME | MapLayerName | tilemap: TILEMAP, layerIndex: LONG | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPLAYERTYPE | MapLayerType | tilemap: TILEMAP, layerIndex: LONG | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPLAYERVISIBLE | MapLayerVisible | tilemap: TILEMAP, layerIndex: LONG | BOOLEAN | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTCOUNT | MapObjectCount | tilemap: TILEMAP, layerIndex: LONG | INTEGER | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTNAME | MapObjectName | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTCLASS | MapObjectClass | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTKIND | MapObjectKind | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | STRING | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTVISIBLE | MapObjectVisible | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | BOOLEAN | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTX | MapObjectX | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTY | MapObjectY | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTWIDTH | MapObjectWidth | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPOBJECTHEIGHT | MapObjectHeight | tilemap: TILEMAP, layerIndex: LONG, objectIndex: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPCOLUMN | MapColumn | tilemap: TILEMAP, originX: DOUBLE, worldX: DOUBLE | INTEGER | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPROW | MapRow | tilemap: TILEMAP, originY: DOUBLE, worldY: DOUBLE | INTEGER | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPWORLDX | MapWorldX | tilemap: TILEMAP, originX: DOUBLE, column: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| MAPWORLDY | MapWorldY | tilemap: TILEMAP, originY: DOUBLE, row: LONG | DOUBLE | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| RGB | Rgb | red: LONG, green: LONG, blue: LONG | INTEGER | Default / Declared / Declared | True | 9 / 26 / 7 | True / True |
| LEN | Len | text: STRING | INTEGER | Default / Declared / Declared | True | 31 / 43 / 8 | True / True |
| LEN | LenUnits | text: STRING, unit: STRING | INTEGER | Default / Declared / Declared | True | 31 / 43 / 8 | True / True |
| LEFT | Left | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | True | 66 / 45 / 20 | True / True |
| LEFT | LeftUnits | text: STRING, count: INTEGER, unit: STRING | STRING | Default / Declared / Declared | True | 66 / 45 / 20 | True / True |
| RIGHT | Right | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | True | 48 / 29 / 13 | True / True |
| RIGHT | RightUnits | text: STRING, count: INTEGER, unit: STRING | STRING | Default / Declared / Declared | True | 48 / 29 / 13 | True / True |
| MID | Mid | text: STRING, start: INTEGER, count: INTEGER | STRING | Default / Declared / Declared | True | 15 / 16 / 4 | True / True |
| MID | MidUnits | text: STRING, start: INTEGER, count: INTEGER, unit: STRING | STRING | Default / Declared / Declared | True | 15 / 16 / 4 | True / True |
| LCASE | LCase | text: STRING | STRING | Default / Declared / Declared | True | 5 / 5 / 4 | True / True |
| UCASE | UCase | text: STRING | STRING | Default / Declared / Declared | True | 8 / 6 / 4 | True / True |
| REVERSE | Reverse | text: STRING | STRING | Default / Declared / Declared | True | 7 / 2 / 3 | True / True |
| ASC | Asc | text: STRING | INTEGER | Default / Declared / Declared | True | 8 / 8 / 6 | True / True |
| ASCW | AscW | text: STRING | INTEGER | Default / Declared / Declared | True | 10 / 7 / 6 | True / True |
| CHR | Chr | code: INTEGER | STRING | Default / Declared / Declared | True | 15 / 33 / 12 | True / True |
| CHRW | ChrW | code: INTEGER | STRING | Default / Declared / Declared | True | 51 / 8 / 7 | True / True |
| TRIM | TrimWhitespace | text: STRING | STRING | Default / Declared / Declared | True | 7 / 18 / 5 | True / True |
| TRIM | TrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | True | 7 / 18 / 5 | True / True |
| LTRIM | LTrimWhitespace | text: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| LTRIM | LTrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| RTRIM | RTrimWhitespace | text: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| RTRIM | RTrimCharacters | text: STRING, chars: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 2 | True / True |
| CONTAINS | Contains | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | True | 53 / 6 / 2 | True / True |
| STARTSWITH | StartsWith | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | True | 32 / 3 / 2 | True / True |
| ENDSWITH | EndsWith | text: STRING, fragment: STRING | BOOLEAN | Default / Declared / Declared | True | 12 / 2 / 2 | True / True |
| REPLACE | Replace | text: STRING, oldText: STRING, newText: STRING | STRING | Default / Declared / Declared | True | 23 / 8 / 11 | True / True |
| REPLACEMANY | ReplaceMany | text: STRING, oldItems: STRING(), newItems: STRING() | STRING | Default / Declared / Declared | True | 7 / 2 / 2 | True / True |
| INSTR | InStr | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | True | 8 / 7 / 3 | True / True |
| INSTR | InStrFrom | start: INTEGER, text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | True | 8 / 7 / 3 | True / True |
| INSTRREV | InStrRev | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | True | 6 / 5 / 2 | True / True |
| INSTRREV | InStrRevFrom | start: INTEGER, text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | True | 6 / 5 / 2 | True / True |
| SUBSTRCOUNT | SubStrCount | text: STRING, fragment: STRING | INTEGER | Default / Declared / Declared | True | 6 / 4 / 2 | True / True |
| REPEAT | Repeat | text: STRING, count: INTEGER | STRING | Default / Declared / Declared | True | 19 / 2 / 2 | True / True |
| SPACE | Space | count: INTEGER | STRING | Default / Declared / Declared | True | 10 / 7 / 3 | True / True |
| STRING | StringFill | count: INTEGER, fill: STRING | STRING | Default / Declared / Declared | True | 223 / 211 / 54 | True / True |
| PADLEFT | PadLeft | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | True | 6 / 3 / 2 | True / True |
| PADLEFT | PadLeftFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | True | 6 / 3 / 2 | True / True |
| PADRIGHT | PadRight | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | True | 7 / 2 / 2 | True / True |
| PADRIGHT | PadRightFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | True | 7 / 2 / 2 | True / True |
| CENTER | Center | text: STRING, width: INTEGER | STRING | Default / Declared / Declared | True | 7 / 3 / 2 | True / True |
| CENTER | CenterFill | text: STRING, width: INTEGER, fill: STRING | STRING | Default / Declared / Declared | True | 7 / 3 / 2 | True / True |
| INSERT | Insert | text: STRING, start: INTEGER, fragment: STRING | STRING | Default / Declared / Declared | True | 10 / 5 / 3 | True / True |
| REMOVE | Remove | text: STRING, start: INTEGER, count: INTEGER | STRING | Default / Declared / Declared | True | 19 / 4 / 2 | True / True |
| SPLIT | Split | text: STRING | STRING() | Default / Declared / Declared | True | 13 / 8 / 10 | True / True |
| SPLIT | SplitSeparator | text: STRING, separator: STRING | STRING() | Default / Declared / Declared | True | 13 / 8 / 10 | True / True |
| JOIN | Join | items: STRING() | STRING | Default / Declared / Declared | True | 107 / 10 / 3 | True / True |
| JOIN | JoinSeparator | items: STRING(), separator: STRING | STRING | Default / Declared / Declared | True | 107 / 10 / 3 | True / True |
| REGEXISMATCH | RegexIsMatch | text: STRING, pattern: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 4 / 2 | True / True |
| REGEXISMATCH | RegexIsMatchModes | text: STRING, pattern: STRING, modes: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 4 / 2 | True / True |
| REGEXMATCH | RegexMatch | text: STRING, pattern: STRING | STRING | Default / Declared / Declared | True | 6 / 1 / 2 | True / True |
| REGEXMATCH | RegexMatchModes | text: STRING, pattern: STRING, modes: STRING | STRING | Default / Declared / Declared | True | 6 / 1 / 2 | True / True |
| REGEXEXTRACT | RegexExtract | text: STRING, pattern: STRING, group: INTEGER | STRING | Default / Declared / Declared | True | 8 / 3 / 2 | True / True |
| REGEXEXTRACT | RegexExtractModes | text: STRING, pattern: STRING, group: INTEGER, modes: STRING | STRING | Default / Declared / Declared | True | 8 / 3 / 2 | True / True |
| REGEXMATCHES | RegexMatches | text: STRING, pattern: STRING | STRING() | Default / Declared / Declared | True | 7 / 6 / 2 | True / True |
| REGEXMATCHES | RegexMatchesModes | text: STRING, pattern: STRING, modes: STRING | STRING() | Default / Declared / Declared | True | 7 / 6 / 2 | True / True |
| REGEXEXTRACTALL | RegexExtractAll | text: STRING, pattern: STRING, group: INTEGER | STRING() | Default / Declared / Declared | True | 9 / 7 / 2 | True / True |
| REGEXEXTRACTALL | RegexExtractAllModes | text: STRING, pattern: STRING, group: INTEGER, modes: STRING | STRING() | Default / Declared / Declared | True | 9 / 7 / 2 | True / True |
| REGEXSPLIT | RegexSplit | text: STRING, pattern: STRING | STRING() | Default / Declared / Declared | True | 7 / 5 / 2 | True / True |
| REGEXSPLIT | RegexSplitModes | text: STRING, pattern: STRING, modes: STRING | STRING() | Default / Declared / Declared | True | 7 / 5 / 2 | True / True |
| REGEXREPLACE | RegexReplace | text: STRING, pattern: STRING, replacement: STRING | STRING | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| REGEXREPLACE | RegexReplaceModes | text: STRING, pattern: STRING, replacement: STRING, modes: STRING | STRING | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| REGEXREPLACE | RegexReplaceCount | text: STRING, pattern: STRING, replacement: STRING, count: INTEGER | STRING | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| REGEXREPLACE | RegexReplaceModesCount | text: STRING, pattern: STRING, replacement: STRING, modes: STRING, count: INTEGER | STRING | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| REGEXCOUNT | RegexCount | text: STRING, pattern: STRING | INTEGER | Default / Declared / Declared | True | 7 / 5 / 2 | True / True |
| REGEXCOUNT | RegexCountModes | text: STRING, pattern: STRING, modes: STRING | INTEGER | Default / Declared / Declared | True | 7 / 5 / 2 | True / True |
| DATE | Date |  | DATE | Default / Declared / Declared | True | 12 / 8 / 13 | True / True |
| DATE | DateInZone | timeZone: STRING | DATE | Default / Declared / Declared | True | 12 / 8 / 13 | True / True |
| TIME | Time |  | TIME | Default / Declared / Declared | True | 11 / 11 / 13 | True / True |
| TIME | TimeInZone | timeZone: STRING | TIME | Default / Declared / Declared | True | 11 / 11 / 13 | True / True |
| NOW | Now |  | DATETIME | Default / Declared / Declared | True | 10 / 4 / 2 | True / True |
| NOW | NowInZone | timeZone: STRING | DATETIME | Default / Declared / Declared | True | 10 / 4 / 2 | True / True |
| UTCDATE | UtcDate |  | DATE | Default / Declared / Declared | True | 8 / 4 / 1 | True / True |
| UTCTIME | UtcTime |  | TIME | Default / Declared / Declared | True | 8 / 4 / 1 | True / True |
| UTCNOW | UtcNow |  | DATETIME | Default / Declared / Declared | True | 9 / 2 / 1 | True / True |
| TIMER | Timer |  | DOUBLE | Default / Declared / Declared | True | 8 / 3 / 1 | True / True |
| DATESERIAL | DateSerial | year: INTEGER, month: INTEGER, day: INTEGER | DATE | Default / Declared / Declared | True | 11 / 7 / 3 | True / True |
| TIMESERIAL | TimeSerial | hour: INTEGER, minute: INTEGER, second: INTEGER | TIME | Default / Declared / Declared | True | 11 / 7 / 3 | True / True |
| YEAR | Year | value: DATE | INTEGER | Default / Declared / Declared | True | 6 / 4 / 3 | True / True |
| MONTH | Month | value: DATE | INTEGER | Default / Declared / Declared | True | 6 / 7 / 2 | True / True |
| MONTHNAME | MonthName | month: INTEGER | STRING | Default / Declared / Declared | True | 7 / 5 / 1 | True / True |
| DAY | Day | value: DATE | INTEGER | Default / Declared / Declared | True | 6 / 5 / 5 | True / True |
| HOUR | Hour | value: TIME | INTEGER | Default / Declared / Declared | True | 6 / 4 / 2 | True / True |
| MINUTE | Minute | value: TIME | INTEGER | Default / Declared / Declared | True | 6 / 4 / 1 | True / True |
| SECOND | Second | value: TIME | INTEGER | Default / Declared / Declared | True | 48 / 15 / 2 | True / True |
| WEEKDAY | Weekday | value: DATE | INTEGER | Default / Declared / Declared | True | 6 / 5 / 1 | True / True |
| WEEKDAYNAME | WeekdayName | weekday: INTEGER | STRING | Default / Declared / Declared | True | 7 / 4 / 1 | True / True |
| DATEADD | DateAddDateValue | interval: STRING, number: INTEGER, value: DATE | DATE | Default / Declared / Declared | True | 9 / 8 / 2 | True / True |
| DATEADD | DateAddTimeValue | interval: STRING, number: INTEGER, value: TIME | TIME | Default / Declared / Declared | True | 9 / 8 / 2 | True / True |
| DATEADD | DateAddDateTimeValue | interval: STRING, number: INTEGER, value: DATETIME | DATETIME | Default / Declared / Declared | True | 9 / 8 / 2 | True / True |
| DATEDIFF | DateDiffDateValues | interval: STRING, fromValue: DATE, toValue: DATE | LONG | Default / Declared / Declared | True | 8 / 5 / 2 | True / True |
| DATEDIFF | DateDiffTimeValues | interval: STRING, fromValue: TIME, toValue: TIME | LONG | Default / Declared / Declared | True | 8 / 5 / 2 | True / True |
| DATEDIFF | DateDiffDateTimeValues | interval: STRING, fromValue: DATETIME, toValue: DATETIME | LONG | Default / Declared / Declared | True | 8 / 5 / 2 | True / True |
| DATEPART | DatePartDateValue | part: STRING, value: DATE | INTEGER | Default / Declared / Declared | True | 8 / 3 / 2 | True / True |
| DATEPART | DatePartTimeValue | part: STRING, value: TIME | INTEGER | Default / Declared / Declared | True | 8 / 3 / 2 | True / True |
| DATEPART | DatePartDateTimeValue | part: STRING, value: DATETIME | INTEGER | Default / Declared / Declared | True | 8 / 3 / 2 | True / True |
| DATEVALUE | DateValue | value: STRING | DATE | Default / Declared / Declared | True | 8 / 4 / 2 | True / True |
| TIMEVALUE | TimeValue | value: STRING | TIME | Default / Declared / Declared | True | 8 / 2 / 2 | True / True |
| ISDATE | IsDate | value: STRING | BOOLEAN | Default / Declared / Declared | True | 8 / 5 / 1 | True / True |
| UTCOFFSET | UtcOffset |  | INTEGER | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| UTCOFFSET | UtcOffsetValue | value: DATETIME | INTEGER | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| UTCOFFSET | UtcOffsetValueZone | value: DATETIME, timeZone: STRING | INTEGER | Default / Declared / Declared | True | 8 / 7 / 2 | True / True |
| TOUTC | ToUtc | value: DATETIME | DATETIME | Default / Declared / Declared | True | 8 / 6 / 2 | True / True |
| TOUTC | ToUtcZone | value: DATETIME, timeZone: STRING | DATETIME | Default / Declared / Declared | True | 8 / 6 / 2 | True / True |
| FROMUTC | FromUtc | value: DATETIME | DATETIME | Default / Declared / Declared | True | 8 / 5 / 2 | True / True |
| FROMUTC | FromUtcZone | value: DATETIME, timeZone: STRING | DATETIME | Default / Declared / Declared | True | 8 / 5 / 2 | True / True |
| ISLEAPYEAR | IsLeapYear | year: INTEGER | BOOLEAN | Default / Declared / Declared | True | 8 / 2 / 1 | True / True |
| DAYSINMONTH | DaysInMonth | year: INTEGER, month: INTEGER | INTEGER | Default / Declared / Declared | True | 8 / 2 / 1 | True / True |
| STARTOFMONTH | StartOfMonth | value: DATE | DATE | Default / Declared / Declared | True | 8 / 2 / 1 | True / True |
| ENDOFMONTH | EndOfMonth | value: DATE | DATE | Default / Declared / Declared | True | 7 / 2 / 1 | True / True |
| STARTOFYEAR | StartOfYear | value: DATE | DATE | Default / Declared / Declared | True | 7 / 2 / 1 | True / True |
| ENDOFYEAR | EndOfYear | value: DATE | DATE | Default / Declared / Declared | True | 8 / 2 / 1 | True / True |
| ISOPEN | IsOpen | file: FILE | BOOLEAN | Default / Declared / Declared | True | 10 / 4 / 3 | True / True |
| FILEPATH | FilePath | file: FILE | STRING | Default / Declared / Declared | True | 13 / 6 / 1 | True / True |
| FILEMODE | FileMode | file: FILE | STRING | Default / Declared / Declared | True | 20 / 3 / 2 | True / True |
| FILEPOS | FilePos | file: FILE | LONG | Default / Declared / Declared | True | 18 / 12 / 3 | True / True |
| FILELENGTH | FileLength | file: FILE | LONG | Default / Declared / Declared | True | 12 / 4 / 2 | True / True |
| LINENUMBER | LineNumber | file: FILE | LONG | Default / Declared / Declared | True | 12 / 3 / 2 | True / True |
| EOF | Eof | file: FILE | BOOLEAN | Default / Declared / Declared | True | 13 / 3 / 3 | True / True |
| READTOEND | ReadToEnd | file: FILE | STRING | Default / Declared / Declared | True | 14 / 4 / 1 | True / True |
| READTEXT | ReadText | file: FILE, count: INTEGER | STRING | Default / Declared / Declared | True | 10 / 5 / 2 | True / True |
| READLINE | ReadLine | file: FILE | STRING | Default / Declared / Declared | True | 11 / 11 / 3 | True / True |
| READLINES | ReadLines | file: FILE, count: INTEGER | STRING() | Default / Declared / Declared | True | 6 / 3 / 1 | True / True |
| SKIPLINE | SkipLine | file: FILE | BOOLEAN | Default / Declared / Declared | True | 6 / 2 / 1 | True / True |
| WRITE | Write | file: FILE, text: STRING | BOOLEAN | Default / Declared / Declared | True | 25 / 11 / 3 | True / True |
| WRITELINE | WriteLine | file: FILE, text: STRING | BOOLEAN | Default / Declared / Declared | True | 11 / 8 / 4 | True / True |
| NEWLINE | NewLine | file: FILE | BOOLEAN | Default / Declared / Declared | True | 32 / 4 / 1 | True / True |
| FLUSH | Flush | file: FILE | BOOLEAN | Default / Declared / Declared | True | 15 / 4 / 2 | True / True |
| SEEKFILE | SeekFile | file: FILE, position: LONG | BOOLEAN | Default / Declared / Declared | True | 11 / 4 / 2 | True / True |
| REWIND | Rewind | file: FILE | BOOLEAN | Default / Declared / Declared | True | 11 / 3 / 1 | True / True |
| FILEEXISTS | FileExists | path: STRING | BOOLEAN | Default / Declared / Declared | True | 10 / 10 / 2 | True / True |
| DIREXISTS | DirExists | path: STRING | BOOLEAN | Default / Declared / Declared | True | 9 / 15 / 3 | True / True |
| PATHEXISTS | PathExists | path: STRING | BOOLEAN | Default / Declared / Declared | True | 6 / 2 / 2 | True / True |
| READFILE | ReadFile | path: STRING | STRING | Default / Declared / Declared | True | 16 / 30 / 4 | True / True |
| READFILE | ReadFileEncoding | path: STRING, encoding: STRING | STRING | Default / Declared / Declared | True | 16 / 30 / 4 | True / True |
| READALLLINES | ReadAllLines | path: STRING | STRING() | Default / Declared / Declared | True | 12 / 8 / 2 | True / True |
| READALLLINES | ReadAllLinesEncoding | path: STRING, encoding: STRING | STRING() | Default / Declared / Declared | True | 12 / 8 / 2 | True / True |
| WRITEFILE | WriteFile | path: STRING, text: STRING | BOOLEAN | Default / Declared / Declared | True | 13 / 65 / 4 | True / True |
| WRITEFILE | WriteFileEncoding | path: STRING, text: STRING, encoding: STRING | BOOLEAN | Default / Declared / Declared | True | 13 / 65 / 4 | True / True |
| APPENDFILE | AppendFile | path: STRING, text: STRING | BOOLEAN | Default / Declared / Declared | True | 8 / 4 / 2 | True / True |
| APPENDFILE | AppendFileEncoding | path: STRING, text: STRING, encoding: STRING | BOOLEAN | Default / Declared / Declared | True | 8 / 4 / 2 | True / True |
| FILESIZE | FileSize | path: STRING | LONG | Default / Declared / Declared | True | 8 / 10 / 1 | True / True |
| FILEDATE | FileDate | path: STRING | STRING | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| FILECREATEDATE | FileCreateDate | path: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 1 | True / True |
| FILEACCESSEDDATE | FileAccessDate | path: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 1 | True / True |
| DIRDATE | DirDate | path: STRING | STRING | Default / Declared / Declared | True | 5 / 1 / 1 | True / True |
| GETFILES | GetFiles | path: STRING | STRING() | Default / Declared / Declared | True | 12 / 3 / 1 | True / True |
| GETFILES | GetFilesPattern | path: STRING, pattern: STRING | STRING() | Default / Declared / Declared | True | 12 / 3 / 1 | True / True |
| GETDIRS | GetDirs | path: STRING | STRING() | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| GETDIRS | GetDirsPattern | path: STRING, pattern: STRING | STRING() | Default / Declared / Declared | True | 4 / 1 / 1 | True / True |
| COMBINEPATH | CombinePath | left: STRING, right: STRING | STRING | Default / Declared / Declared | True | 4 / 39 / 4 | True / True |
| FILENAME | FileName | path: STRING | STRING | Default / Declared / Declared | True | 9 / 6 / 1 | True / True |
| DIRNAME | DirName | path: STRING | STRING | Default / Declared / Declared | True | 6 / 4 / 1 | True / True |
| EXTNAME | ExtName | path: STRING | STRING | Default / Declared / Declared | True | 6 / 1 / 1 | True / True |
| FULLPATH | FullPath | path: STRING | STRING | Default / Declared / Declared | True | 8 / 8 / 2 | True / True |
| PATHROOT | PathRoot | path: STRING | STRING | Default / Declared / Declared | True | 6 / 4 / 1 | True / True |
| ISABSOLUTEPATH | IsAbsolutePath | path: STRING | BOOLEAN | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| MAKEDIR | MakeDir | path: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 33 / 2 | True / True |
| DELETEFILE | DeleteFile | path: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 41 / 3 | True / True |
| COPYFILE | CopyFile | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | True | 10 / 5 / 2 | True / True |
| COPYFILE | CopyFileOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | True | 10 / 5 / 2 | True / True |
| MOVEFILE | MoveFile | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | True | 6 / 3 / 1 | True / True |
| MOVEFILE | MoveFileOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | True | 6 / 3 / 1 | True / True |
| RENAMEFILE | RenameFile | path: STRING, newName: STRING | BOOLEAN | Default / Declared / Declared | True | 5 / 2 / 1 | True / True |
| DELETEDIR | DeleteDir | path: STRING | BOOLEAN | Default / Declared / Declared | True | 7 / 10 / 2 | True / True |
| DELETEDIRRECURSIVE | DeleteDirRecursive | path: STRING | BOOLEAN | Default / Declared / Declared | True | 6 / 18 / 1 | True / True |
| COPYDIR | CopyDir | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | True | 8 / 4 / 1 | True / True |
| COPYDIR | CopyDirOverwrite | source: STRING, destination: STRING, overwrite: BOOLEAN | BOOLEAN | Default / Declared / Declared | True | 8 / 4 / 1 | True / True |
| MOVEDIR | MoveDir | source: STRING, destination: STRING | BOOLEAN | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| RENAMEDIR | RenameDir | path: STRING, newName: STRING | BOOLEAN | Default / Declared / Declared | True | 5 / 2 / 1 | True / True |
| CURRENTDIR | CurrentDir |  | STRING | Default / Declared / Declared | True | 7 / 21 / 2 | True / True |
| CHANGEDIR | ChangeDir | path: STRING | BOOLEAN | Default / Declared / Declared | True | 6 / 5 / 2 | True / True |
| DIRSIZE | DirSize | path: STRING | LONG | Default / Declared / Declared | True | 7 / 8 / 1 | True / True |
| FREESPACE | FreeSpace | path: STRING | LONG | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| TOTALFREESPACE | TotalFreeSpace | path: STRING | LONG | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| TOTALSPACE | TotalSpace | path: STRING | LONG | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| ISREADONLY | IsReadOnly | path: STRING | BOOLEAN | Default / Declared / Declared | True | 11 / 1 / 1 | True / True |
| ISHIDDEN | IsHidden | path: STRING | BOOLEAN | Default / Declared / Declared | True | 5 / 1 / 1 | True / True |
| DRIVENAME | DriveName | path: STRING | STRING | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| DRIVEFORMAT | DriveFormat | path: STRING | STRING | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| DRIVETYPE | DriveType | path: STRING | STRING | Default / Declared / Declared | True | 5 / 3 / 1 | True / True |
| MAKETEMPFILE | MakeTempFile | prefix: STRING | STRING | Default / Declared / Declared | True | 5 / 34 / 2 | True / True |
| MAKETEMPDIR | MakeTempDir | prefix: STRING | STRING | Default / Declared / Declared | True | 5 / 32 / 4 | True / True |
| INT | Int | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Integer | True | 74 / 11 / 5 | True / True |
| STR | Str | value: DOUBLE | STRING | Numeric / PreserveNumericArguments / String | True | 11 / 33 / 5 | True / True |
| VAL | Val | text: STRING | DOUBLE | Default / Declared / Declared | True | 23 / 14 / 6 | True / True |
| ISNUMERIC | IsNumeric | text: STRING | BOOLEAN | Default / Declared / Declared | True | 4 / 1 / 3 | True / True |
| CINT | CInt | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Declared | True | 7 / 4 / 3 | True / True |
| CLNG | CLng | value: DOUBLE | LONG | Numeric / PreserveNumericArguments / Declared | True | 21 / 4 / 3 | True / True |
| CDBL | CDbl | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / Declared | True | 5 / 2 / 2 | True / True |
| CDEC | CDec | value: DOUBLE | DECIMAL | Numeric / PreserveNumericArguments / Declared | True | 21 / 4 / 3 | True / True |
| RND | Rnd |  | DOUBLE | Default / Declared / Declared | True | 10 / 5 / 3 | True / True |
| ABS | Abs | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 18 / 8 / 5 | True / True |
| SGN | Sgn | value: DOUBLE | INTEGER | Numeric / PreserveNumericArguments / Integer | True | 8 / 2 / 1 | True / True |
| MIN | Min | left: DOUBLE, right: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | True | 10 / 4 / 3 | True / True |
| MAX | Max | left: DOUBLE, right: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | True | 11 / 4 / 3 | True / True |
| CLAMP | Clamp | value: DOUBLE, low: DOUBLE, high: DOUBLE | DOUBLE | Numeric / PromoteNumericArguments / PromotedNumeric | True | 9 / 4 / 2 | True / True |
| SIN | Sin | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 8 / 3 / 2 | True / True |
| COS | Cos | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 5 / 2 / 1 | True / True |
| TAN | Tan | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 5 / 2 / 1 | True / True |
| ATN | Atn | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 4 / 2 / 1 | True / True |
| ATN2 | Atn2 | y: DOUBLE, x: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 3 / 1 | True / True |
| SQR | Sqr | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 9 / 5 / 2 | True / True |
| CBRT | Cbrt | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 5 / 2 / 1 | True / True |
| EXP | Exp | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 2 / 1 | True / True |
| LOG | Log | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 9 / 4 / 3 | True / True |
| LOG10 | Log10 | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 2 / 1 | True / True |
| LOG2 | Log2 | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 7 / 2 / 1 | True / True |
| POW | Pow | value: DOUBLE, power: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 10 / 5 / 1 | True / True |
| FLOOR | Floor | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 9 / 3 / 4 | True / True |
| CEIL | Ceil | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 6 / 2 / 3 | True / True |
| ROUND | Round | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 8 / 11 / 3 | True / True |
| FIX | Fix | value: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 9 / 4 / 3 | True / True |
| ASIN | Asin | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 3 / 2 | True / True |
| ACOS | Acos | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 8 / 3 / 2 | True / True |
| ASINH | Asinh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 5 / 2 / 1 | True / True |
| ACOSH | Acosh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 3 / 1 | True / True |
| ATANH | Atanh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 7 / 3 / 2 | True / True |
| SINH | Sinh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 2 / 1 | True / True |
| COSH | Cosh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 6 / 2 / 1 | True / True |
| TANH | Tanh | value: DOUBLE | DOUBLE | Numeric / ConvertNumericArgumentsToDouble / Double | True | 5 / 2 / 1 | True / True |
| SETSIGN | SetSign | value: DOUBLE, signFrom: DOUBLE | DOUBLE | Numeric / PreserveNumericArguments / SameAsFirstArgument | True | 15 / 3 / 1 | True / True |
