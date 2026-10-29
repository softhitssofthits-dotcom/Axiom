// Описания функций сверяются с docs/manual/data/language-reference.json.
(function(root, factory) {
  const api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.AxiomBuiltinHints = api;
})(typeof window !== "undefined" ? window : globalThis, function() {
  const hints = {
  "ABS": [
    "Возвращает модуль числа.",
    "Returns the absolute value of a number."
  ],
  "ACOS": [
    "Возвращает арккосинус числа.",
    "Returns the inverse cosine in radians."
  ],
  "ACOSH": [
    "Возвращает обратный гиперболический косинус числа.",
    "Returns the inverse hyperbolic cosine."
  ],
  "APPENDFILE": [
    "Дописывает текст в конец файла.",
    "Appends text to a file."
  ],
  "ARRAYAVERAGE": [
    "Возвращает среднее арифметическое числовых элементов массива.",
    "Returns the arithmetic mean of numeric array elements."
  ],
  "ARRAYBINARYSEARCH": [
    "Ищет первое совпадение двоичным поиском в отсортированном массиве.",
    "Finds the first match using binary search in a sorted array."
  ],
  "ARRAYBOUNDS": [
    "Возвращает таблицу нижних и верхних границ всех измерений.",
    "Returns a table of lower and upper bounds for every dimension."
  ],
  "ARRAYCLONE": [
    "Создаёт независимую копию видимой части массива.",
    "Creates an independent copy of the visible array region."
  ],
  "ARRAYCONCAT": [
    "Объединяет содержимое двух массивов в новую копию.",
    "Combines two arrays into a new copy."
  ],
  "ARRAYCONTAINS": [
    "Проверяет наличие значения среди видимых элементов массива.",
    "Checks whether the visible array elements contain a value."
  ],
  "ARRAYCOUNT": [
    "Подсчитывает совпадения значения в массиве.",
    "Counts matching values in an array."
  ],
  "ARRAYDISTINCT": [
    "Копирует первые появления различных значений в одномерный массив.",
    "Copies the first occurrence of each distinct value into a one-dimensional array."
  ],
  "ARRAYEVERY": [
    "Проверяет, удовлетворяют ли все элементы пользовательскому условию.",
    "Checks whether every element satisfies a user predicate."
  ],
  "ARRAYFILL": [
    "Заполняет видимые элементы массива одним значением.",
    "Fills the visible array elements with one value."
  ],
  "ARRAYFILTER": [
    "Копирует элементы, удовлетворяющие пользовательскому условию.",
    "Copies elements that satisfy a user predicate."
  ],
  "ARRAYFINDALL": [
    "Возвращает индексы всех совпадений значения в массиве.",
    "Returns the indices of all matching values."
  ],
  "ARRAYFLATTEN": [
    "Копирует все видимые элементы в одномерный массив.",
    "Copies all visible elements into a one-dimensional array."
  ],
  "ARRAYINDEXOF": [
    "Находит первое совпадение значения в массиве.",
    "Finds the first matching value in an array."
  ],
  "ARRAYINSERT": [
    "Вставляет элемент или срез перед заданным индексом массива.",
    "Inserts an element or slice before the specified array index."
  ],
  "ARRAYISALLOCATED": [
    "Проверяет, выделено ли хранилище элементов массива.",
    "Checks whether array element storage is allocated."
  ],
  "ARRAYLASTINDEXOF": [
    "Находит последнее совпадение значения в массиве.",
    "Finds the last matching value in an array."
  ],
  "ARRAYLENGTH": [
    "Возвращает число видимых элементов массива.",
    "Returns the number of visible array elements."
  ],
  "ARRAYMAP": [
    "Преобразует каждый элемент пользовательской функцией и возвращает новый массив.",
    "Transforms each element using a user function and returns a new array."
  ],
  "ARRAYMAX": [
    "Возвращает наибольшее значение массива.",
    "Returns the greatest array value."
  ],
  "ARRAYMIN": [
    "Возвращает наименьшее значение массива.",
    "Returns the least array value."
  ],
  "ARRAYPOP": [
    "Снимает последний элемент или срез выбранного измерения массива.",
    "Removes and returns the last element or slice of the selected dimension."
  ],
  "ARRAYPUSH": [
    "Добавляет элемент или срез в конец выбранного измерения массива.",
    "Appends an element or slice to the selected dimension."
  ],
  "ARRAYRANK": [
    "Возвращает число измерений массива.",
    "Returns the number of array dimensions."
  ],
  "ARRAYREDUCE": [
    "Последовательно объединяет элементы с накопленным значением пользовательской функцией.",
    "Combines elements with an accumulator using a user function."
  ],
  "ARRAYREMOVE": [
    "Удаляет и возвращает элемент или срез с заданным индексом массива.",
    "Removes and returns the element or slice at an array index."
  ],
  "ARRAYRESHAPE": [
    "Копирует последовательность элементов в новую форму, заданную массивом образца.",
    "Copies the element sequence into the shape of a template array."
  ],
  "ARRAYREVERSE": [
    "Меняет порядок элементов исходного массива на обратный.",
    "Reverses the original array in place."
  ],
  "ARRAYSKIP": [
    "Копирует остаток массива после пропуска указанного количества.",
    "Copies the remainder after skipping the specified count."
  ],
  "ARRAYSLICE": [
    "Копирует непрерывный диапазон элементов либо срезов массива.",
    "Copies a contiguous range of array elements or slices."
  ],
  "ARRAYSOME": [
    "Проверяет, удовлетворяет ли хотя бы один элемент пользовательскому условию.",
    "Checks whether at least one element satisfies a user predicate."
  ],
  "ARRAYSORT": [
    "Сортирует исходный массив; для записей можно выбрать поле и направление сортировки.",
    "Sorts the original array in place; record forms select a field and direction."
  ],
  "ARRAYSUM": [
    "Возвращает сумму числовых элементов массива.",
    "Returns the sum of numeric array elements."
  ],
  "ARRAYTAKE": [
    "Копирует начальную часть массива по указанному количеству.",
    "Copies the specified number of leading elements or slices."
  ],
  "ARRAYTOREVERSED": [
    "Возвращает копию с обратным порядком элементов.",
    "Returns a reversed array copy."
  ],
  "ARRAYTOSORTED": [
    "Возвращает отсортированную копию массива; для записей можно выбрать поле и направление сортировки.",
    "Returns a sorted array copy; record forms select a field and direction."
  ],
  "ASC": [
    "Возвращает код первого символа строки в CP866, если он представлен в этой таблице, иначе его Unicode-код.",
    "Returns the first character code in CP866 when available, otherwise its Unicode code."
  ],
  "ASCW": [
    "Возвращает Unicode-код первого символа строки без преобразования через CP866.",
    "Returns the first character Unicode code without CP866 conversion."
  ],
  "ASIN": [
    "Возвращает арксинус числа.",
    "Returns the inverse sine in radians."
  ],
  "ASINH": [
    "Возвращает обратный гиперболический синус числа.",
    "Returns the inverse hyperbolic sine."
  ],
  "ATANH": [
    "Возвращает обратный гиперболический тангенс числа.",
    "Returns the inverse hyperbolic tangent."
  ],
  "ATN": [
    "Возвращает арктангенс числа.",
    "Returns the inverse tangent in radians."
  ],
  "ATN2": [
    "Возвращает угол направления точки относительно положительной оси x.",
    "Returns the direction angle relative to the positive x axis."
  ],
  "CBRT": [
    "Возвращает кубический корень числа.",
    "Returns the cube root."
  ],
  "CDBL": [
    "Возвращает число, преобразованное в DOUBLE.",
    "Converts a number to DOUBLE."
  ],
  "CDEC": [
    "Возвращает число, преобразованное в DECIMAL.",
    "Converts a number to DECIMAL."
  ],
  "CEIL": [
    "Возвращает наименьшее целое значение, не меньшее аргумента.",
    "Returns the smallest integer value greater than or equal to the argument."
  ],
  "CENTER": [
    "Возвращает строку, дополнив её с обеих сторон до заданной ширины.",
    "Pads both sides of a string to the requested width."
  ],
  "CHANGEDIR": [
    "Меняет рабочую папку текущего запуска.",
    "Changes the working directory of the current run."
  ],
  "CHR": [
    "Создаёт строку с символом по коду CP866 для значений 0–255 или по Unicode-коду для больших значений.",
    "Creates a character from CP866 codes 0..255, or a Unicode code above that range."
  ],
  "CHRW": [
    "Создаёт строку с символом по Unicode-коду без подмены младшего диапазона таблицей CP866.",
    "Creates a character from its Unicode code without CP866 substitution."
  ],
  "CINT": [
    "Возвращает целую часть числа в типе INTEGER.",
    "Truncates a number to INTEGER."
  ],
  "CLAMP": [
    "Возвращает число, ограниченное заданным включительным диапазоном.",
    "Limits a number to an inclusive range."
  ],
  "CLNG": [
    "Возвращает целую часть числа в типе LONG.",
    "Truncates a number to LONG."
  ],
  "COMBINEPATH": [
    "Соединяет две строковые части пути.",
    "Combines two path strings."
  ],
  "CONTAINS": [
    "Проверяет, содержит ли строка указанный фрагмент.",
    "Checks whether a string contains a substring."
  ],
  "COPYDIR": [
    "Копирует дерево обычных файлов и каталогов.",
    "Copies a tree of regular files and directories."
  ],
  "COPYFILE": [
    "Копирует содержимое существующего файла в другой файл.",
    "Copies an existing file to another file."
  ],
  "COS": [
    "Возвращает косинус угла.",
    "Returns the cosine of an angle in radians."
  ],
  "COSH": [
    "Возвращает гиперболический косинус числа.",
    "Returns the hyperbolic cosine."
  ],
  "CURRENTDIR": [
    "Возвращает полный путь рабочей папки текущего запуска.",
    "Returns the full working directory path of the current run."
  ],
  "DATE": [
    "Возвращает текущую календарную дату.",
    "Returns the current calendar date, optionally in a selected time zone."
  ],
  "DATEADD": [
    "Возвращает дату или время после прибавления указанного числа интервалов.",
    "Adds a specified number of date or time intervals."
  ],
  "DATEDIFF": [
    "Возвращает число пересечённых границ указанного интервала между двумя значениями.",
    "Counts interval boundaries crossed between two values."
  ],
  "DATEPART": [
    "Возвращает выбранную календарную или временную часть значения.",
    "Returns a selected date or time component."
  ],
  "DATESERIAL": [
    "Возвращает дату, собранную из года, месяца и дня.",
    "Constructs a date from year, month and day."
  ],
  "DATETIMESERIAL": [
    "Возвращает дату и время, собранные из готовых значений DATE и TIME.",
    "Combines DATE and TIME values into DATETIME."
  ],
  "DATETIMEVALUE": [
    "Возвращает дату и время, прочитанные из строки в канонической записи или в формате обмена.",
    "Parses a canonical or interchange date-time string."
  ],
  "DATEVALUE": [
    "Возвращает календарную дату, прочитанную из строки или взятую из значения DATETIME.",
    "Extracts a calendar date from a string or DATETIME."
  ],
  "DAY": [
    "Возвращает номер дня внутри месяца.",
    "Returns the day of the month."
  ],
  "DAYSINMONTH": [
    "Возвращает количество дней в месяце.",
    "Returns the number of days in a month."
  ],
  "DELETEDIR": [
    "Удаляет пустой каталог.",
    "Deletes an empty directory."
  ],
  "DELETEDIRRECURSIVE": [
    "Удаляет каталог со всем его содержимым.",
    "Deletes a directory and its contents."
  ],
  "DELETEFILE": [
    "Удаляет файл по заданному пути.",
    "Deletes the file at a path."
  ],
  "DIRDATE": [
    "Возвращает местные дату и время последней записи в каталог.",
    "Returns the directory last-write time as local date and time."
  ],
  "DIRDATEUTC": [
    "Возвращает момент последнего изменения каталога на шкале UTC.",
    "Returns the directory last-write instant in UTC."
  ],
  "DIREXISTS": [
    "Проверяет, существует ли каталог по заданному пути.",
    "Checks whether a directory exists at a path."
  ],
  "DIRNAME": [
    "Возвращает полный путь родительского каталога.",
    "Returns the full parent directory path."
  ],
  "DIRSIZE": [
    "Возвращает суммарный размер обычных файлов дерева каталога в байтах.",
    "Returns the total regular file size in a directory tree, in bytes."
  ],
  "DRIVEFORMAT": [
    "Возвращает имя файловой системы диска.",
    "Returns the drive file system name."
  ],
  "DRIVENAME": [
    "Возвращает строковое имя корня диска.",
    "Returns the drive root name."
  ],
  "DRIVETYPE": [
    "Возвращает строковое название типа диска.",
    "Returns the drive type name."
  ],
  "ELAPSEDSECONDS": [
    "Возвращает прошедшее время в секундах: от начала запуска либо между двумя моментами.",
    "Returns elapsed seconds since the run began or between two instants."
  ],
  "ENDOFMONTH": [
    "Возвращает последний день месяца исходной даты.",
    "Returns the last day of the value's month."
  ],
  "ENDOFYEAR": [
    "Возвращает последний день года исходной даты.",
    "Returns the last day of the value's year."
  ],
  "ENDSWITH": [
    "Проверяет, заканчивается ли строка указанным фрагментом.",
    "Checks whether a string ends with a substring."
  ],
  "EOF": [
    "Проверяет, достигнута ли конечная байтовая позиция файла при чтении.",
    "Checks whether reading has reached the file's final byte position."
  ],
  "EXP": [
    "Возвращает значение экспоненты e в степени аргумента.",
    "Returns e raised to the argument."
  ],
  "EXTNAME": [
    "Возвращает расширение последнего компонента пути вместе с точкой.",
    "Returns the last path component extension including the dot."
  ],
  "FILEACCESSEDDATE": [
    "Возвращает местные дату и время последнего доступа к файлу.",
    "Returns the file last-access time as local date and time."
  ],
  "FILEACCESSEDDATEUTC": [
    "Возвращает момент последнего доступа к файлу на шкале UTC.",
    "Returns the file last-access instant in UTC."
  ],
  "FILECREATEDATE": [
    "Возвращает местные дату и время создания файла.",
    "Returns the file creation time as local date and time."
  ],
  "FILECREATEDATEUTC": [
    "Возвращает момент создания файла на шкале UTC.",
    "Returns the file creation instant in UTC."
  ],
  "FILEDATE": [
    "Возвращает местные дату и время последней записи в файл.",
    "Returns the file last-write time as local date and time."
  ],
  "FILEDATEUTC": [
    "Возвращает момент последней записи в файл на шкале UTC.",
    "Returns the file last-write instant in UTC."
  ],
  "FILEEXISTS": [
    "Проверяет, существует ли обычный файл по заданному пути.",
    "Checks whether a regular file exists at a path."
  ],
  "FILELENGTH": [
    "Возвращает текущую длину содержимого открытого файла в байтах.",
    "Returns the open file content length in bytes."
  ],
  "FILEMODE": [
    "Возвращает строковое имя режима открытого ресурса FILE.",
    "Returns the open FILE resource mode name."
  ],
  "FILENAME": [
    "Возвращает последнюю часть полного пути вместе с расширением.",
    "Returns the final path component including its extension."
  ],
  "FILEPATH": [
    "Возвращает полный путь открытого ресурса FILE.",
    "Returns the full path of an open FILE resource."
  ],
  "FILEPOS": [
    "Возвращает текущую байтовую позицию открытого файла.",
    "Returns the open file byte position."
  ],
  "FILESIZE": [
    "Возвращает физическую длину файла в байтах.",
    "Returns the physical file size in bytes."
  ],
  "FIX": [
    "Возвращает целую часть числа после удаления дробной части.",
    "Returns the number's integer part by truncating the fractional part."
  ],
  "FLOOR": [
    "Возвращает наибольшее целое значение, не превышающее аргумент.",
    "Returns the greatest integer value less than or equal to the argument."
  ],
  "FLUSH": [
    "Передаёт накопленные байты записи в файловый поток.",
    "Flushes buffered output bytes to the file stream."
  ],
  "FREESPACE": [
    "Возвращает число свободных байтов диска, доступных текущему пользователю.",
    "Returns free drive bytes available to the current user."
  ],
  "FROMINSTANT": [
    "Возвращает календарную дату-время, в которую заданный момент выглядит в выбранном поясе.",
    "Converts an instant to calendar date and time in a selected zone."
  ],
  "FROMUTC": [
    "Возвращает дату и время, преобразованные из UTC в выбранный пояс.",
    "Converts UTC date and time to a selected zone."
  ],
  "FULLPATH": [
    "Преобразует строку пути в полный путь.",
    "Resolves a path string to a full path."
  ],
  "GETDIRS": [
    "Возвращает массив полных путей каталогов непосредственно в указанном каталоге.",
    "Returns full paths of immediate child directories as an array."
  ],
  "GETFILES": [
    "Возвращает массив полных путей файлов непосредственно в указанном каталоге.",
    "Returns full paths of immediate child files as an array."
  ],
  "HOUR": [
    "Возвращает час времени суток.",
    "Returns the hour component."
  ],
  "IMAGEHEIGHT": [
    "Возвращает высоту загруженного изображения в пикселях или 0 для пустого значения изображения.",
    "Returns image height in pixels, or 0 for an empty image."
  ],
  "IMAGEWIDTH": [
    "Возвращает ширину загруженного изображения в пикселях или 0 для пустого значения изображения.",
    "Returns image width in pixels, or 0 for an empty image."
  ],
  "INKEY": [
    "Извлекает следующую клавишу из очереди ввода без ожидания и возвращает её строковое представление либо пустую строку, если очередь пуста.",
    "Consumes the next queued key without waiting; returns an empty string if none is queued."
  ],
  "INSERT": [
    "Возвращает строку, вставив фрагмент перед указанной позицией.",
    "Returns a string with a fragment inserted before a position."
  ],
  "INSTANTVALUE": [
    "Возвращает момент времени, прочитанный из текста с признаком UTC.",
    "Parses an instant from text with a UTC marker."
  ],
  "INSTR": [
    "Возвращает позицию первого вхождения фрагмента в строку.",
    "Returns the first substring match position."
  ],
  "INSTRREV": [
    "Возвращает позицию вхождения фрагмента, найденного при поиске справа налево.",
    "Returns a substring match position searching from right to left."
  ],
  "INT": [
    "Возвращает наибольшее целое, не превышающее число.",
    "Returns the greatest integer less than or equal to a number."
  ],
  "ISABSOLUTEPATH": [
    "Проверяет, полностью ли задан путь без обращения к рабочему каталогу.",
    "Checks whether a path is fully qualified without a working directory."
  ],
  "ISDATE": [
    "Возвращает признак допустимости канонической строки даты, времени или даты-времени.",
    "Checks whether text is a valid canonical date, time or date-time."
  ],
  "ISHIDDEN": [
    "Проверяет атрибут «скрытый» файла или каталога.",
    "Checks the hidden attribute of a file or directory."
  ],
  "ISLEAPYEAR": [
    "Возвращает признак високосности года.",
    "Checks whether a year is a leap year."
  ],
  "ISNUMERIC": [
    "Проверяет, можно ли прочитать строку целиком как число типа DOUBLE. Возвращает TRUE или FALSE.",
    "Checks whether the entire string can be parsed as DOUBLE."
  ],
  "ISOPEN": [
    "Проверяет, открыт ли ресурс FILE.",
    "Checks whether a FILE resource is open."
  ],
  "ISREADONLY": [
    "Проверяет атрибут «только для чтения» файла или каталога.",
    "Checks the read-only attribute of a file or directory."
  ],
  "JOIN": [
    "Соединяет элементы массива строк и возвращает одну строку.",
    "Joins a string array into one string."
  ],
  "KEYDOWN": [
    "Проверяет текущее удержание клавиши или всех клавиш сочетания, сохраняя события в очереди ввода.",
    "Checks whether a key or all keys in a combination are held, without consuming events."
  ],
  "KEYPRESS": [
    "Ищет непрочитанное нажатие клавиши, удаляет первое подходящее событие из очереди и сообщает, было ли оно найдено.",
    "Consumes the first matching queued key press and reports whether one was found."
  ],
  "LBOUND": [
    "Возвращает нижнюю границу выбранного измерения массива.",
    "Returns the lower bound of the selected array dimension."
  ],
  "LCASE": [
    "Возвращает строку, преобразовав буквы в нижний регистр.",
    "Converts letters to lowercase."
  ],
  "LEFT": [
    "Возвращает фрагмент заданной длины из начала строки.",
    "Returns the requested length from the beginning of a string."
  ],
  "LEN": [
    "Возвращает длину строки в выбранных единицах текста.",
    "Returns string length in the selected text units."
  ],
  "LINENUMBER": [
    "Возвращает номер строки в текущей позиции чтения.",
    "Returns the line number at the current file read position."
  ],
  "LOADIMAGE": [
    "Загружает файл изображения и возвращает ресурс, который можно измерять и рисовать на графической поверхности.",
    "Loads an image file as a resource for measuring and drawing."
  ],
  "LOADSPRITE": [
    "Загружает изображение как спрайтовый лист и разбивает его на прямоугольные кадры указанного размера.",
    "Loads a sprite sheet and divides it into frames of the specified size."
  ],
  "LOADTILEMAP": [
    "Принимает путь типа STRING, читает карту Tiled и возвращает значение типа TILEMAP.",
    "Loads a Tiled map from a STRING path and returns TILEMAP."
  ],
  "LOADTILESET": [
    "Загружает изображение с сеткой тайлов и возвращает TILESET.",
    "Loads an image containing a tile grid and returns TILESET."
  ],
  "LOG": [
    "Возвращает натуральный логарифм числа.",
    "Returns the natural logarithm."
  ],
  "LOG10": [
    "Возвращает десятичный логарифм числа.",
    "Returns the base-10 logarithm."
  ],
  "LOG2": [
    "Возвращает двоичный логарифм числа.",
    "Returns the base-2 logarithm."
  ],
  "LTRIM": [
    "Возвращает строку после удаления заданных символов с начала текста.",
    "Removes specified characters from the beginning of a string."
  ],
  "MAKEDIR": [
    "Создаёт каталог и недостающие родительские каталоги.",
    "Creates a directory and missing parent directories."
  ],
  "MAKETEMPDIR": [
    "Создаёт каталог со случайным именем во временной папке системы.",
    "Creates a randomly named directory in the system temporary directory."
  ],
  "MAKETEMPFILE": [
    "Создаёт пустой файл со случайным именем во временной папке системы.",
    "Creates an empty randomly named file in the system temporary directory."
  ],
  "MAPCOLUMN": [
    "Переводит мировую координату X в номер столбца карты.",
    "Converts world X to a map column number."
  ],
  "MAPHEIGHT": [
    "Возвращает высоту карты в клетках.",
    "Returns map height in cells."
  ],
  "MAPLAYERCOUNT": [
    "Возвращает число слоёв карты.",
    "Returns the map layer count."
  ],
  "MAPLAYERNAME": [
    "Возвращает имя слоя.",
    "Returns a layer name."
  ],
  "MAPLAYERPROPERTY": [
    "Возвращает пользовательское свойство слоя.",
    "Returns a custom layer property."
  ],
  "MAPLAYERTYPE": [
    "Возвращает вид слоя: TILE или OBJECT.",
    "Returns the layer kind: TILE or OBJECT."
  ],
  "MAPLAYERVISIBLE": [
    "Возвращает флаг видимости слоя.",
    "Returns layer visibility."
  ],
  "MAPOBJECTCLASS": [
    "Возвращает класс объекта из class или type.",
    "Returns an object's class or type field."
  ],
  "MAPOBJECTCOUNT": [
    "Возвращает число объектов слоя.",
    "Returns the layer object count."
  ],
  "MAPOBJECTHEIGHT": [
    "Возвращает высоту объекта.",
    "Returns object height."
  ],
  "MAPOBJECTKIND": [
    "Возвращает вид объекта: POINT или RECT.",
    "Returns the object kind: POINT or RECT."
  ],
  "MAPOBJECTNAME": [
    "Возвращает имя объекта.",
    "Returns an object name."
  ],
  "MAPOBJECTPROPERTY": [
    "Возвращает пользовательское свойство объекта.",
    "Returns a custom object property."
  ],
  "MAPOBJECTVISIBLE": [
    "Возвращает флаг видимости объекта.",
    "Returns object visibility."
  ],
  "MAPOBJECTWIDTH": [
    "Возвращает ширину объекта.",
    "Returns object width."
  ],
  "MAPOBJECTX": [
    "Возвращает координату X объекта.",
    "Returns the object X coordinate."
  ],
  "MAPOBJECTY": [
    "Возвращает координату Y объекта.",
    "Returns the object Y coordinate."
  ],
  "MAPPIXELHEIGHT": [
    "Возвращает высоту карты в пикселях.",
    "Returns map height in pixels."
  ],
  "MAPPIXELWIDTH": [
    "Возвращает ширину карты в пикселях.",
    "Returns map width in pixels."
  ],
  "MAPPROPERTY": [
    "Возвращает пользовательское свойство карты.",
    "Returns a custom map property."
  ],
  "MAPROW": [
    "Переводит мировую координату Y в номер строки карты.",
    "Converts world Y to a map row number."
  ],
  "MAPTILE": [
    "Возвращает номер тайла внутри тайлсета.",
    "Returns the tile number within its tileset."
  ],
  "MAPTILEFLAGS": [
    "Возвращает битовую маску отражений тайла в клетке.",
    "Returns the tile cell reflection bit mask."
  ],
  "MAPTILEHEIGHT": [
    "Возвращает высоту одной клетки в пикселях.",
    "Returns cell height in pixels."
  ],
  "MAPTILEPROPERTY": [
    "Возвращает свойство фактического тайла в клетке.",
    "Returns a property of the actual tile in a cell."
  ],
  "MAPTILESET": [
    "Возвращает номер тайлсета карты, к которому относится клетка.",
    "Returns the map tileset number used by a cell."
  ],
  "MAPTILESETCOUNT": [
    "Возвращает число тайлсетов карты.",
    "Returns the map tileset count."
  ],
  "MAPTILEWIDTH": [
    "Возвращает ширину одной клетки в пикселях.",
    "Returns cell width in pixels."
  ],
  "MAPWIDTH": [
    "Возвращает ширину карты в клетках.",
    "Returns map width in cells."
  ],
  "MAPWORLDX": [
    "Возвращает мировую координату левой границы столбца.",
    "Returns the world X coordinate of a column's left edge."
  ],
  "MAPWORLDY": [
    "Возвращает мировую координату верхней границы строки.",
    "Returns the world Y coordinate of a row's top edge."
  ],
  "MAX": [
    "Возвращает большее из двух чисел.",
    "Returns the greater of two numbers."
  ],
  "MID": [
    "Возвращает фрагмент строки, начиная с указанной позиции.",
    "Returns a string fragment starting at a position."
  ],
  "MIN": [
    "Возвращает меньшее из двух чисел.",
    "Returns the lesser of two numbers."
  ],
  "MINUTE": [
    "Возвращает минуту внутри часа.",
    "Returns the minute component."
  ],
  "MONTH": [
    "Возвращает номер месяца календарной даты.",
    "Returns the calendar month number."
  ],
  "MONTHNAME": [
    "Возвращает английское название месяца.",
    "Returns the English month name."
  ],
  "MOUSEBUTTON": [
    "Проверяет текущее удержание указанной кнопки мыши на активном графическом экране или холсте, не расходуя события очереди.",
    "Checks whether a mouse button is held on the active graphics surface without consuming events."
  ],
  "MOUSEPRESS": [
    "Ищет непрочитанное нажатие указанной кнопки мыши, удаляет первое подходящее событие и сообщает, было ли оно найдено.",
    "Consumes the first matching queued mouse press and reports whether one was found."
  ],
  "MOUSEX": [
    "Возвращает LONG с горизонтальной координатой указателя: в SCREEN — физические пиксели клиентской области, на CANVAS — мировая координата с учётом VIEWPORT и CAMERA. Требует графическую поверхность.",
    "Returns LONG: physical client pixels in SCREEN, or world X through VIEWPORT and CAMERA on CANVAS. Requires a graphics surface."
  ],
  "MOUSEY": [
    "Возвращает LONG с вертикальной координатой указателя: в SCREEN — физические пиксели клиентской области, на CANVAS — мировая координата с учётом VIEWPORT и CAMERA. Требует графическую поверхность.",
    "Returns LONG: physical client pixels in SCREEN, or world Y through VIEWPORT and CAMERA on CANVAS. Requires a graphics surface."
  ],
  "MOVEDIR": [
    "Переносит каталог по новому пути.",
    "Moves a directory to a new path."
  ],
  "MOVEFILE": [
    "Переносит существующий файл по новому пути.",
    "Moves an existing file to a new path."
  ],
  "NEWLINE": [
    "Записывает символ LF в открытый файл.",
    "Writes LF to an open file."
  ],
  "NOW": [
    "Возвращает текущую дату и время.",
    "Returns the current date and time, optionally in a selected time zone."
  ],
  "NOWINSTANT": [
    "Возвращает текущий момент времени на шкале UTC.",
    "Returns the current instant in UTC."
  ],
  "PADLEFT": [
    "Возвращает строку, дополнив её слева до заданной ширины.",
    "Pads a string on the left to the requested width."
  ],
  "PADRIGHT": [
    "Возвращает строку, дополнив её справа до заданной ширины.",
    "Pads a string on the right to the requested width."
  ],
  "PATHEXISTS": [
    "Проверяет, существует ли файл или каталог по заданному пути.",
    "Checks whether a file or directory exists at a path."
  ],
  "PATHROOT": [
    "Возвращает корень полного пути.",
    "Returns the root of a full path."
  ],
  "PMAP": [
    "Возвращает DOUBLE с преобразованной координатой SCREEN между логической шкалой WINDOW и пиксельной системой VIEW. Без WINDOW возвращает исходную координату.",
    "Converts a SCREEN coordinate between logical WINDOW units and VIEW pixels as DOUBLE. Without WINDOW, returns the original coordinate."
  ],
  "POINT": [
    "Возвращает индекс цвета пикселя в заданной точке либо выбранную координату текущей графической позиции SCREEN.",
    "Returns a SCREEN pixel color index or a selected component of the current graphics position."
  ],
  "POW": [
    "Возвращает первое число в степени второго.",
    "Raises the first number to the power of the second."
  ],
  "READALLLINES": [
    "Читает строки файла в одномерный массив строк.",
    "Reads file lines into a one-dimensional string array."
  ],
  "READFILE": [
    "Читает весь текст файла в строку.",
    "Reads an entire text file into a string."
  ],
  "READLINE": [
    "Читает одну строку открытого файла без её разделителя.",
    "Reads one open file line without its separator."
  ],
  "READLINES": [
    "Читает не более заданного числа строк открытого файла в массив.",
    "Reads up to the requested number of open file lines into an array."
  ],
  "READTEXT": [
    "Читает не более заданного числа скалярных значений Unicode.",
    "Reads up to the requested number of Unicode scalar values."
  ],
  "READTOEND": [
    "Читает оставшийся текст открытого файла в строку.",
    "Reads the remaining open file text into a string."
  ],
  "REGEXCOUNT": [
    "Возвращает количество совпадений с регулярным выражением.",
    "Returns the number of regular expression matches."
  ],
  "REGEXEXTRACT": [
    "Возвращает значение выбранной группы первого совпадения с регулярным выражением.",
    "Returns a selected group from the first regular expression match."
  ],
  "REGEXEXTRACTALL": [
    "Возвращает значения выбранной группы для всех совпадений с регулярным выражением как массив строк.",
    "Returns a selected group from every regular expression match as a string array."
  ],
  "REGEXISMATCH": [
    "Проверяет, содержит ли текст хотя бы одно совпадение с регулярным выражением.",
    "Checks whether text contains any regular expression match."
  ],
  "REGEXMATCH": [
    "Возвращает текст первого полного совпадения с регулярным выражением.",
    "Returns the first complete regular expression match text."
  ],
  "REGEXMATCHES": [
    "Возвращает все найденные совпадения с регулярным выражением как массив строк.",
    "Returns all regular expression matches as a string array."
  ],
  "REGEXREPLACE": [
    "Заменяет совпадения с регулярным выражением и возвращает полученную строку.",
    "Replaces regular expression matches and returns the resulting string."
  ],
  "REGEXSPLIT": [
    "Разбивает текст по совпадениям с регулярным выражением и возвращает полученные части как массив строк.",
    "Splits text at regular expression matches into a string array."
  ],
  "REMOVE": [
    "Возвращает строку, удалив заданное количество единиц текста с указанной позиции.",
    "Removes a count of text units starting at a position and returns the resulting string."
  ],
  "RENAMEDIR": [
    "Меняет имя каталога внутри его родительского каталога.",
    "Renames a directory within its parent directory."
  ],
  "RENAMEFILE": [
    "Меняет имя файла в его текущем каталоге.",
    "Renames a file within its current directory."
  ],
  "REPEAT": [
    "Возвращает строку, повторив исходный фрагмент указанное число раз.",
    "Repeats a string fragment the specified number of times."
  ],
  "REPLACE": [
    "Возвращает строку, заменив все точные вхождения одного фрагмента другим.",
    "Replaces all exact occurrences of a substring."
  ],
  "REPLACEMANY": [
    "Возвращает строку после одновременной замены нескольких фрагментов по двум массивам правил.",
    "Simultaneously replaces fragments using two arrays of rules."
  ],
  "REVERSE": [
    "Возвращает строку с обратным порядком кодов Unicode.",
    "Reverses Unicode codes in a string."
  ],
  "REWIND": [
    "Переставляет позицию открытого файла в начало содержимого.",
    "Moves the open file position to the beginning of its content."
  ],
  "RGB": [
    "Собирает полноцветное значение 0xRRGGBB из красной, зелёной и синей составляющих.",
    "Builds a 0xRRGGBB true color value from red, green and blue components."
  ],
  "RIGHT": [
    "Возвращает фрагмент заданной длины из конца строки.",
    "Returns the requested length from the end of a string."
  ],
  "RND": [
    "Возвращает очередное псевдослучайное число.",
    "Returns the next pseudorandom number."
  ],
  "ROUND": [
    "Возвращает число, округлённое до ближайшего целого.",
    "Rounds a number to the nearest integer."
  ],
  "RTRIM": [
    "Возвращает строку после удаления заданных символов с конца текста.",
    "Removes specified characters from the end of a string."
  ],
  "SCREENHEIGHT": [
    "Возвращает INTEGER с высотой текущей поверхности SCREEN или CANVAS в пикселях. Без открытой поверхности возвращает 0.",
    "Returns the current SCREEN or CANVAS height in pixels, or 0 without a surface."
  ],
  "SCREENISACTIVE": [
    "Возвращает BOOLEAN: TRUE только при активном графическом SCREEN 1..10; для SCREEN 0, CANVAS и отсутствующей поверхности возвращает FALSE.",
    "Returns TRUE only for active graphics SCREEN 1..10; FALSE for SCREEN 0, CANVAS or no surface."
  ],
  "SCREENMODE": [
    "Возвращает INTEGER с номером текущего режима SCREEN. Без SCREEN, в текстовом режиме и на CANVAS возвращает 0.",
    "Returns the current SCREEN mode number, or 0 without SCREEN, in text mode or on CANVAS."
  ],
  "SCREENWIDTH": [
    "Возвращает INTEGER с шириной текущей поверхности SCREEN или CANVAS в пикселях. Без открытой поверхности возвращает 0.",
    "Returns the current SCREEN or CANVAS width in pixels, or 0 without a surface."
  ],
  "SECOND": [
    "Возвращает секунду внутри минуты.",
    "Returns the second component."
  ],
  "SEEKFILE": [
    "Переставляет текущую позицию открытого файла на заданное байтовое смещение.",
    "Moves the open file position to a byte offset."
  ],
  "SETSIGN": [
    "Возвращает модуль первого числа со знаком второго числа.",
    "Returns the magnitude of the first number with the sign of the second."
  ],
  "SGN": [
    "Возвращает знак числа в виде −1, 0 или 1.",
    "Returns a number's sign as -1, 0 or 1."
  ],
  "SIN": [
    "Возвращает синус угла.",
    "Returns the sine of an angle in radians."
  ],
  "SINH": [
    "Возвращает гиперболический синус числа.",
    "Returns the hyperbolic sine."
  ],
  "SKIPLINE": [
    "Пропускает одну строку открытого файла.",
    "Skips one open file line."
  ],
  "SPACE": [
    "Возвращает строку из указанного количества пробелов.",
    "Returns a string of the specified number of spaces."
  ],
  "SPLIT": [
    "Разбивает строку по разделителю и возвращает массив полученных частей.",
    "Splits a string at a separator into an array."
  ],
  "SPRITECOUNT": [
    "Возвращает число полных кадров в переданном спрайтовом листе.",
    "Returns the complete frame count of a sprite sheet."
  ],
  "SPRITEHEIGHT": [
    "Возвращает высоту одного кадра переданного спрайта в пикселях.",
    "Returns sprite frame height in pixels."
  ],
  "SPRITEWIDTH": [
    "Возвращает ширину одного кадра переданного спрайта в пикселях.",
    "Returns sprite frame width in pixels."
  ],
  "SQR": [
    "Возвращает квадратный корень числа.",
    "Returns the square root."
  ],
  "STARTOFMONTH": [
    "Возвращает первый день месяца исходной даты.",
    "Returns the first day of the value's month."
  ],
  "STARTOFYEAR": [
    "Возвращает первый день года исходной даты.",
    "Returns the first day of the value's year."
  ],
  "STARTSWITH": [
    "Проверяет, начинается ли строка с указанного фрагмента.",
    "Checks whether a string begins with a substring."
  ],
  "STR": [
    "Возвращает строковую запись числа.",
    "Returns the text representation of a number."
  ],
  "STRING": [
    "Возвращает строку, повторив первую кодовую единицу заполнителя указанное число раз.",
    "Repeats the first UTF-16 code unit of a filler the specified number of times."
  ],
  "SUBSTRCOUNT": [
    "Возвращает количество неперекрывающихся вхождений фрагмента в строку.",
    "Counts non-overlapping occurrences of a substring."
  ],
  "TAN": [
    "Возвращает тангенс угла.",
    "Returns the tangent of an angle in radians."
  ],
  "TANH": [
    "Возвращает гиперболический тангенс числа.",
    "Returns the hyperbolic tangent."
  ],
  "TEXTHEIGHT": [
    "Возвращает INTEGER с высотой текста DRAWTEXT в пикселях: число строк умножается на 12. Каждая LF добавляет строку; для пустой строки возвращает 12. Требует CANVAS.",
    "Returns DRAWTEXT height in pixels: 12 per line, including 12 for empty text. Requires CANVAS."
  ],
  "TEXTWIDTH": [
    "Возвращает INTEGER с шириной самой длинной строки в пикселях при рисовании DRAWTEXT на CANVAS. Каждая единица UTF-16 занимает 8 пикселей; CR пропускается, LF разделяет строки. Для пустой строки возвращает 0. Требует CANVAS.",
    "Returns the longest DRAWTEXT line width in pixels: 8 per UTF-16 unit, CR ignored, LF separates lines; empty text gives 0. Requires CANVAS."
  ],
  "TILECOUNT": [
    "Возвращает число тайлов в переданном наборе.",
    "Returns the tile count of a tileset."
  ],
  "TILEHEIGHT": [
    "Возвращает высоту одного тайла переданного набора в пикселях.",
    "Returns tile height in pixels."
  ],
  "TILEWIDTH": [
    "Возвращает ширину одного тайла переданного набора в пикселях.",
    "Returns tile width in pixels."
  ],
  "TIME": [
    "Возвращает текущее время суток.",
    "Returns the current time of day, optionally in a selected time zone."
  ],
  "TIMER": [
    "Возвращает число секунд, прошедших с местной полуночи.",
    "Returns seconds since local midnight."
  ],
  "TIMESERIAL": [
    "Возвращает время суток, собранное из часа, минуты и секунды.",
    "Constructs a time from hour, minute and second."
  ],
  "TIMEVALUE": [
    "Возвращает время суток, прочитанное из строки или взятое из значения DATETIME.",
    "Extracts a time from a string or DATETIME."
  ],
  "TOINSTANT": [
    "Возвращает момент времени, соответствующий календарной дате-времени в заданном поясе.",
    "Converts calendar date and time in a selected zone to an instant."
  ],
  "TOTALFREESPACE": [
    "Возвращает общее число свободных байтов диска.",
    "Returns total free drive bytes."
  ],
  "TOTALSPACE": [
    "Возвращает полную ёмкость диска в байтах.",
    "Returns total drive capacity in bytes."
  ],
  "TOUTC": [
    "Возвращает дату и время, преобразованные из выбранного пояса в UTC.",
    "Converts date and time from a selected zone to UTC."
  ],
  "TRIM": [
    "Возвращает строку после удаления заданных символов с обоих краёв текста.",
    "Removes specified characters from both ends of a string."
  ],
  "UBOUND": [
    "Возвращает верхнюю границу выбранного измерения массива.",
    "Returns the upper bound of the selected array dimension."
  ],
  "UCASE": [
    "Возвращает строку, преобразовав буквы в верхний регистр.",
    "Converts letters to uppercase."
  ],
  "UTCDATE": [
    "Возвращает текущую дату по UTC.",
    "Returns the current UTC date."
  ],
  "UTCNOW": [
    "Возвращает текущую дату и время по UTC.",
    "Returns the current UTC date and time."
  ],
  "UTCOFFSET": [
    "Возвращает смещение выбранного часового пояса относительно UTC в минутах для текущего момента или заданных местных часов.",
    "Returns a time zone offset from UTC in minutes for now or specified local date and time."
  ],
  "UTCTIME": [
    "Возвращает текущее время суток по UTC.",
    "Returns the current UTC time of day."
  ],
  "VAL": [
    "Возвращает число, прочитанное из строки.",
    "Parses a number from a string."
  ],
  "WEEKDAY": [
    "Возвращает номер дня недели.",
    "Returns the day-of-week number."
  ],
  "WEEKDAYNAME": [
    "Возвращает английское название дня недели.",
    "Returns the English day-of-week name."
  ],
  "WRITE": [
    "Записывает строку в открытый файл без добавления разделителя строки.",
    "Writes text to an open file without a line separator."
  ],
  "WRITEFILE": [
    "Записывает текст в файл, заменяя прежнее содержимое.",
    "Writes text to a file, replacing its previous content."
  ],
  "WRITELINE": [
    "Записывает строку в открытый файл и добавляет символ LF.",
    "Writes text and LF to an open file."
  ],
  "YEAR": [
    "Возвращает год календарной даты.",
    "Returns the calendar year."
  ]
};
  function hint(name, locale) {
    const entry = hints[String(name || "").toUpperCase()];
    return entry ? entry[locale === "en" ? 1 : 0] : null;
  }
  // Группы сверены с разделами справочника и дескрипторами BuiltinFunctions.
  const categoryGroups = [
    ["ABS ACOS ACOSH ASIN ASINH ATANH ATN ATN2 CBRT CEIL CLAMP COS COSH EXP FIX FLOOR INT LOG LOG10 LOG2 MAX MIN POW RND ROUND SETSIGN SGN SIN SINH SQR TAN TANH", "встроенная математическая функция", "built-in mathematical function"],
    ["APPENDFILE CHANGEDIR COMBINEPATH COPYDIR COPYFILE CURRENTDIR DELETEDIR DELETEDIRRECURSIVE DELETEFILE DIRDATE DIRDATEUTC DIREXISTS DIRNAME DIRSIZE DRIVEFORMAT DRIVENAME DRIVETYPE EOF EXTNAME FILEACCESSEDDATE FILEACCESSEDDATEUTC FILECREATEDATE FILECREATEDATEUTC FILEDATE FILEDATEUTC FILEEXISTS FILELENGTH FILEMODE FILENAME FILEPATH FILEPOS FILESIZE FLUSH FREESPACE FULLPATH GETDIRS GETFILES ISABSOLUTEPATH ISHIDDEN ISOPEN ISREADONLY LINENUMBER MAKEDIR MAKETEMPDIR MAKETEMPFILE MOVEDIR MOVEFILE NEWLINE PATHEXISTS PATHROOT READALLLINES READFILE READLINE READLINES READTEXT READTOEND RENAMEDIR RENAMEFILE REWIND SEEKFILE SKIPLINE TOTALFREESPACE TOTALSPACE WRITE WRITEFILE WRITELINE", "встроенная функция работы с файлами и путями", "built-in file and path function"],
    ["ARRAYAVERAGE ARRAYBINARYSEARCH ARRAYBOUNDS ARRAYCLONE ARRAYCONCAT ARRAYCONTAINS ARRAYCOUNT ARRAYDISTINCT ARRAYEVERY ARRAYFILL ARRAYFILTER ARRAYFINDALL ARRAYFLATTEN ARRAYINDEXOF ARRAYINSERT ARRAYISALLOCATED ARRAYLASTINDEXOF ARRAYLENGTH ARRAYMAP ARRAYMAX ARRAYMIN ARRAYPOP ARRAYPUSH ARRAYRANK ARRAYREDUCE ARRAYREMOVE ARRAYRESHAPE ARRAYREVERSE ARRAYSKIP ARRAYSLICE ARRAYSOME ARRAYSORT ARRAYSUM ARRAYTAKE ARRAYTOREVERSED ARRAYTOSORTED LBOUND UBOUND", "встроенная функция работы с массивами", "built-in array function"],
    ["ASC ASCW CENTER CHR CHRW CONTAINS ENDSWITH INSERT INSTR INSTRREV JOIN LCASE LEFT LEN LTRIM MID PADLEFT PADRIGHT REMOVE REPEAT REPLACE REPLACEMANY REVERSE RIGHT RTRIM SPACE SPLIT STARTSWITH STRING SUBSTRCOUNT TRIM UCASE", "встроенная функция работы со строками", "built-in string function"],
    ["CDBL CDEC CINT CLNG STR VAL", "встроенная функция преобразования", "built-in conversion function"],
    ["DATE DATEADD DATEDIFF DATEPART DATESERIAL DATETIMESERIAL DATETIMEVALUE DATEVALUE DAY DAYSINMONTH ELAPSEDSECONDS ENDOFMONTH ENDOFYEAR FROMINSTANT FROMUTC HOUR INSTANTVALUE ISDATE ISLEAPYEAR MINUTE MONTH MONTHNAME NOW NOWINSTANT SECOND STARTOFMONTH STARTOFYEAR TIME TIMER TIMESERIAL TIMEVALUE TOINSTANT TOUTC UTCDATE UTCNOW UTCOFFSET UTCTIME WEEKDAY WEEKDAYNAME YEAR", "встроенная функция даты и времени", "built-in date and time function"],
    ["IMAGEHEIGHT IMAGEWIDTH LOADIMAGE LOADSPRITE LOADTILEMAP LOADTILESET MAPCOLUMN MAPHEIGHT MAPLAYERCOUNT MAPLAYERNAME MAPLAYERPROPERTY MAPLAYERTYPE MAPLAYERVISIBLE MAPOBJECTCLASS MAPOBJECTCOUNT MAPOBJECTHEIGHT MAPOBJECTKIND MAPOBJECTNAME MAPOBJECTPROPERTY MAPOBJECTVISIBLE MAPOBJECTWIDTH MAPOBJECTX MAPOBJECTY MAPPIXELHEIGHT MAPPIXELWIDTH MAPPROPERTY MAPROW MAPTILE MAPTILEFLAGS MAPTILEHEIGHT MAPTILEPROPERTY MAPTILESET MAPTILESETCOUNT MAPTILEWIDTH MAPWIDTH MAPWORLDX MAPWORLDY PMAP POINT RGB SCREENHEIGHT SCREENISACTIVE SCREENMODE SCREENWIDTH SPRITECOUNT SPRITEHEIGHT SPRITEWIDTH TEXTHEIGHT TEXTWIDTH TILECOUNT TILEHEIGHT TILEWIDTH", "встроенная графическая функция", "built-in graphics function"],
    ["INKEY KEYDOWN KEYPRESS", "встроенная функция клавиатурного ввода", "built-in keyboard input function"],
    ["ISNUMERIC", "встроенная функция проверки числовой строки", "built-in numeric text validation function"],
    ["MOUSEBUTTON MOUSEPRESS MOUSEX MOUSEY", "встроенная функция состояния мыши", "built-in mouse state function"],
    ["REGEXCOUNT REGEXEXTRACT REGEXEXTRACTALL REGEXISMATCH REGEXMATCH REGEXMATCHES REGEXREPLACE REGEXSPLIT", "встроенная функция регулярных выражений", "built-in regular expression function"]
  ];
  const categories = new Map();
  for (const [names, ru, en] of categoryGroups) {
    for (const name of names.split(' ')) categories.set(name, [ru, en]);
  }
  function category(name, locale, returnsValue = true) {
    const labels = categories.get(String(name || '').toUpperCase());
    if (!labels) return null;
    const label = labels[locale === 'en' ? 1 : 0];
    return returnsValue ? label : label.replace('функция', 'процедура').replace('function', 'procedure');
  }
  return { hints, hint, category };
});
