(function () {
    const nav = [
        {
            title: "Старт",
            items: [
                { id: "home", href: "index.html", label: "Главная" },
                { id: "reference", href: "reference.html", label: "Алфавитный указатель" },
                { id: "getting-started", href: "getting-started.html", label: "Старт и первая программа" }
            ]
        },
        {
            title: "Обзорные главы",
            items: [
                { id: "data-and-input", href: "data-and-input.html", label: "Данные и ввод" },
                { id: "expressions-and-branching", href: "expressions-and-branching.html", label: "Выражения и ветвление" },
                { id: "arrays-and-builtins", href: "arrays-and-builtins.html", label: "Массивы и встроенные функции" },
                { id: "procedures-and-functions", href: "procedures-and-functions.html", label: "Процедуры и функции" }
            ]
        },
        {
            title: "Данные и объявления",
            items: [
                { id: "data-types", href: "data-types.html", label: "Типы данных" },
                { id: "type", href: "type.html", label: "Оператор TYPE" },
                { id: "variables-constants", href: "variables-and-constants.html", label: "Переменные и константы" }
            ]
        },
        {
            title: "Операторы и управление",
            items: [
                { id: "print", href: "print.html", label: "Оператор PRINT" },
                { id: "input", href: "input.html", label: "Оператор INPUT" },
                { id: "open", href: "open.html", label: "Оператор OPEN" },
                { id: "close", href: "close.html", label: "Оператор CLOSE" },
                { id: "arithmetic-logic", href: "arithmetic-and-logic.html", label: "Арифметика и логика" },
                { id: "if", href: "if.html", label: "Оператор IF" },
                { id: "select-case", href: "select-case.html", label: "Оператор SELECT CASE" },
                { id: "for", href: "for.html", label: "Цикл FOR" },
                { id: "while", href: "while.html", label: "Цикл WHILE" },
                { id: "do-loop", href: "do-loop.html", label: "Цикл DO ... LOOP" },
                { id: "sub", href: "sub.html", label: "Подпрограмма SUB" },
                { id: "function", href: "function.html", label: "Функция FUNCTION" },
                { id: "return", href: "return.html", label: "Оператор RETURN" },
                { id: "arrays", href: "arrays.html", label: "Массивы" }
            ]
        },
        {
            title: "Встроенные функции",
            items: [
                { id: "string-functions", href: "string-functions.html", label: "Строковые функции" },
                { id: "regex-functions", href: "regex-functions.html", label: "Регулярные выражения" },
                { id: "file-system-functions", href: "file-system-functions.html", label: "Файловая система" },
                { id: "date-time-functions", href: "date-time-functions.html", label: "Дата и время" },
                { id: "numeric-functions", href: "numeric-functions.html", label: "Числовые и математические" }
            ]
        },
        {
            title: "Практика и справка",
            items: [
                { id: "diagnostics", href: "diagnostics.html", label: "Диагностика" },
                { id: "examples", href: "examples.html", label: "Примеры программ" },
                { id: "cheat-sheet", href: "cheat-sheet.html", label: "Шпаргалка" }
            ]
        }
    ];

    const catalogEntries = [
        { title: "ПОДМАССИВЫ", label: "Частичное индексирование многомерного массива", href: "arrays.html#partial-indexing", kind: "Приём", category: "Массивы", letter: "П", description: "Показывает, как получить подмассив-представление без копирования, сохранить его границы и использовать его вместе с одномерными ARRAY-функциями." },
        { title: "ДИАПАЗОННЫЕ СРЕЗЫ", label: "Диапазонное индексирование многомерного массива", href: "arrays.html#partial-indexing", kind: "Приём", category: "Массивы", letter: "Д", description: "Описывает форму array(i, lower TO upper), которая возвращает подмассив-представление с сохранёнными границами выбранного диапазона." },
        { title: "ABS", label: "Модуль числа", href: "numeric-functions.html#abs", kind: "Функция", category: "Числовые функции", letter: "A", description: "Возвращает абсолютное значение числа." },
        { title: "ACOS", label: "Арккосинус", href: "numeric-functions.html#acos", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет арккосинус аргумента." },
        { title: "ACOSH", label: "Обратный гиперболический косинус", href: "numeric-functions.html#acosh", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет обратный гиперболический косинус." },
        { title: "AND", label: "Логическое и побитовое И", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "A", description: "Работает как логическое И для BOOLEAN и как побитовое И для INTEGER и LONG." },
        { title: "ARRAY", label: "Массивы", href: "arrays.html", kind: "Конструкция", category: "Массивы", letter: "A", description: "Описывает одномерные и многомерные массивы первого среза с включительными границами по измерениям." },
        { title: "ARRAYCLONE", label: "Независимая копия массива", href: "arrays.html#arrayclone", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый массив того же типа и видимой формы, не разделяя хранилище элементов с исходным массивом или срезом." },
        { title: "ARRAYCONCAT", label: "Объединение двух массивов", href: "arrays.html#arrayconcat", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый компактный массив из элементов левого и правого аргументов одного типа." },
        { title: "ARRAYCONTAINS", label: "Проверка наличия совпадения", href: "arrays.html#arraycontains", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает TRUE, если уже выделенный массив или видимый срез содержит хотя бы одно совпадение со значением поиска." },
        { title: "ARRAYBINARYSEARCH", label: "Бинарный поиск в массиве", href: "arrays.html#arraybinarysearch", kind: "Функция", category: "Массивы", letter: "A", description: "Ищет первое совпадение в уже отсортированном массиве и возвращает логический индекс или значение левее диапазона." },
        { title: "ARRAYFILL", label: "Заполнение массива значением", href: "arrays.html#arrayfill", kind: "Процедура", category: "Массивы", letter: "A", description: "Заполняет все видимые элементы уже выделенного массива или среза одним значением и меняет их на месте." },
        { title: "ARRAYCOUNT", label: "Число совпадений в массиве", href: "arrays.html#arraycount", kind: "Функция", category: "Массивы", letter: "A", description: "Подсчитывает количество видимых элементов массива или среза, равных значению поиска." },
        { title: "ARRAYDISTINCT", label: "Уникальные элементы массива", href: "arrays.html#arraydistinct", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый компактный массив без повторов и сохраняет порядок первых вхождений." },
        { title: "ARRAYEVERY", label: "Predicate для всех элементов", href: "arrays.html#arrayevery", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает TRUE, если callback FUNCTION(T) AS BOOLEAN истинен для каждого элемента массива." },
        { title: "ARRAYFILTER", label: "Фильтрация массива", href: "arrays.html#arrayfilter", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый компактный массив из элементов, для которых callback FUNCTION(T) AS BOOLEAN возвращает TRUE." },
        { title: "ARRAYFINDALL", label: "Все индексы совпадений", href: "arrays.html#arrayfindall", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает новый компактный массив INTEGER() со всеми логическими индексами совпадений." },
        { title: "ARRAYINDEXOF", label: "Первый индекс совпадения", href: "arrays.html#arrayindexof", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает логический индекс первого совпадения или значение левее диапазона массива, если совпадение отсутствует." },
        { title: "ARRAYISALLOCATED", label: "Проверка выделения массива", href: "arrays.html#arrayisallocated", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает TRUE, если массив уже выделен, и FALSE, если он ещё не имеет доступного диапазона индексов." },
        { title: "ARRAYINSERT", label: "Вставка элемента по индексу", href: "arrays.html#arrayinsert", kind: "Процедура", category: "Массивы", letter: "A", description: "Вставляет значение по логическому индексу, сдвигает хвост вправо и расширяет уже выделенный массив на месте." },
        { title: "ARRAYLASTINDEXOF", label: "Последний индекс совпадения", href: "arrays.html#arraylastindexof", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает логический индекс последнего совпадения или значение левее диапазона массива, если совпадение отсутствует." },
        { title: "ARRAYLENGTH", label: "Число элементов массива", href: "arrays.html#arraylength", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает текущее число элементов массива; для невыделенного массива даёт 0." },
        { title: "ARRAYMAP", label: "Преобразование массива", href: "arrays.html#arraymap", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый компактный массив, применяя selector FUNCTION(T) AS U к каждому элементу исходного массива." },
        { title: "ARRAYPOP", label: "Снятие последнего элемента массива", href: "arrays.html#arraypop", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает последний элемент уже выделенного массива и укорачивает сам массив на один логический индекс." },
        { title: "ARRAYPUSH", label: "Добавление элемента в конец массива", href: "arrays.html#arraypush", kind: "Процедура", category: "Массивы", letter: "A", description: "Расширяет уже выделенный массив вправо на один элемент и записывает туда переданное значение." },
        { title: "ARRAYREMOVE", label: "Удаление элемента по индексу", href: "arrays.html#arrayremove", kind: "Функция", category: "Массивы", letter: "A", description: "Удаляет элемент по логическому индексу, возвращает его и сужает уже выделенный массив на месте." },
        { title: "ARRAYREDUCE", label: "Свёртка массива", href: "arrays.html#arrayreduce", kind: "Функция", category: "Массивы", letter: "A", description: "Сворачивает массив в одно значение, применяя reducer FUNCTION(A, T) AS A слева направо от seed." },
        { title: "ARRAYREVERSE", label: "Разворот массива на месте", href: "arrays.html#arrayreverse", kind: "Процедура", category: "Массивы", letter: "A", description: "Меняет порядок элементов уже выделенного массива на обратный, сохраняя его границы." },
        { title: "ARRAYSOME", label: "Predicate хотя бы для одного", href: "arrays.html#arraysome", kind: "Функция", category: "Массивы", letter: "A", description: "Возвращает TRUE, если callback FUNCTION(T) AS BOOLEAN истинен хотя бы для одного элемента массива." },
        { title: "ARRAYSKIP", label: "Пропуск начальной части массива", href: "arrays.html#arrayskip", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый компактный массив из элементов после пропуска первых count значений." },
        { title: "ARRAYSLICE", label: "Срез массива", href: "arrays.html#arrayslice", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый массив из части исходного и возвращает компактные границы 1 TO N или 1 TO 0." },
        { title: "ARRAYSORT", label: "Сортировка массива на месте", href: "arrays.html#arraysort", kind: "Процедура", category: "Массивы", letter: "A", description: "Сортирует уже выделенный числовой или строковый массив по неубыванию и сохраняет его границы." },
        { title: "ARRAYTAKE", label: "Первые элементы массива", href: "arrays.html#arraytake", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый массив из первых элементов исходного массива и возвращает компактные границы 1 TO N или 1 TO 0." },
        { title: "ARRAYTOSORTED", label: "Отсортированная копия массива", href: "arrays.html#arraytosorted", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новую отсортированную копию числового или строкового массива и сохраняет его границы." },
        { title: "ARRAYTOREVERSED", label: "Разворот массива", href: "arrays.html#arraytoreversed", kind: "Функция", category: "Массивы", letter: "A", description: "Создаёт новый массив с обратным порядком элементов и компактными границами 1 TO N или 1 TO 0." },
        { title: "APPENDFILE", label: "Добавление текста в файл", href: "file-system-functions.html#text-files", kind: "Функция", category: "Файловая система", letter: "A", description: "Добавляет текст в конец файла." },
        { title: "ASC", label: "Код первого символа", href: "string-functions.html#asc", kind: "Функция", category: "Строковые функции", letter: "A", description: "Возвращает код первого символа Unicode в строке." },
        { title: "ASIN", label: "Арксинус", href: "numeric-functions.html#asin", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет арксинус аргумента." },
        { title: "ASINH", label: "Обратный гиперболический синус", href: "numeric-functions.html#asinh", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет обратный гиперболический синус." },
        { title: "ATANH", label: "Обратный гиперболический тангенс", href: "numeric-functions.html#atanh", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет обратный гиперболический тангенс." },
        { title: "ATN", label: "Арктангенс", href: "numeric-functions.html#atn", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет арктангенс угла в радианах." },
        { title: "ATN2", label: "Арктангенс по двум координатам", href: "numeric-functions.html#atn2", kind: "Функция", category: "Числовые функции", letter: "A", description: "Вычисляет угол по координатам y и x с учётом квадранта." },
        { title: "BOOLEAN", label: "Логический тип", href: "data-types.html#boolean", kind: "Тип данных", category: "Типы данных", letter: "B", description: "Хранит значения TRUE и FALSE." },
        { title: "CBRT", label: "Кубический корень", href: "numeric-functions.html#cbrt", kind: "Функция", category: "Числовые функции", letter: "C", description: "Вычисляет кубический корень." },
        { title: "CEIL", label: "Округление вверх", href: "numeric-functions.html#ceil", kind: "Функция", category: "Числовые функции", letter: "C", description: "Округляет число вверх." },
        { title: "CENTER", label: "Центрирование строки", href: "string-functions.html#center", kind: "Функция", category: "Строковые функции", letter: "C", description: "Центрирует строку в поле заданной ширины." },
        { title: "CHR", label: "Символ по коду", href: "string-functions.html#chr", kind: "Функция", category: "Строковые функции", letter: "C", description: "Строит строку из одного символа Unicode по числовому коду." },
        { title: "CLAMP", label: "Ограничение диапазоном", href: "numeric-functions.html#clamp", kind: "Функция", category: "Числовые функции", letter: "C", description: "Ограничивает значение указанным диапазоном." },
        { title: "COMBINEPATH", label: "Соединение частей пути", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "C", description: "Соединяет две части пути." },
        { title: "CONTAINS", label: "Проверка наличия фрагмента", href: "string-functions.html#contains", kind: "Функция", category: "Строковые функции", letter: "C", description: "Проверяет, содержит ли строка указанный фрагмент." },
        { title: "COPYDIR", label: "Копирование папки", href: "file-system-functions.html#dir-operations", kind: "Функция", category: "Файловая система", letter: "C", description: "Копирует папку вместе с содержимым." },
        { title: "COPYFILE", label: "Копирование файла", href: "file-system-functions.html#file-operations", kind: "Функция", category: "Файловая система", letter: "C", description: "Копирует файл." },
        { title: "CONST", label: "Объявление констант", href: "variables-and-constants.html#const", kind: "Оператор", category: "Декларации", letter: "C", description: "Создаёт именованное неизменяемое значение." },
        { title: "COS", label: "Косинус", href: "numeric-functions.html#cos", kind: "Функция", category: "Числовые функции", letter: "C", description: "Вычисляет косинус угла в радианах." },
        { title: "COSH", label: "Гиперболический косинус", href: "numeric-functions.html#cosh", kind: "Функция", category: "Числовые функции", letter: "C", description: "Вычисляет гиперболический косинус." },
        { title: "CHANGEDIR", label: "Смена текущей папки", href: "file-system-functions.html#environment", kind: "Функция", category: "Файловая система", letter: "C", description: "Меняет текущую папку программы." },
        { title: "CLOSE", label: "Закрытие текстового файла", href: "close.html", kind: "Оператор", category: "Ввод и вывод", letter: "C", description: "Закрывает ранее открытый файл типа FILE." },
        { title: "CURRENTDIR", label: "Текущая папка программы", href: "file-system-functions.html#environment", kind: "Функция", category: "Файловая система", letter: "C", description: "Возвращает текущую папку программы." },
        { title: "DECIMAL", label: "Точный десятичный тип", href: "data-types.html#decimal", kind: "Тип данных", category: "Типы данных", letter: "D", description: "Используется для точных десятичных вычислений." },
        { title: "DIM", label: "Объявление переменных", href: "variables-and-constants.html#dim", kind: "Оператор", category: "Декларации", letter: "D", description: "Объявляет переменную, массив или фиксированную строку вида STRING * N." },
        { title: "DELETEDIR", label: "Удаление пустой папки", href: "file-system-functions.html#dir-operations", kind: "Функция", category: "Файловая система", letter: "D", description: "Удаляет пустую папку." },
        { title: "DELETEDIRRECURSIVE", label: "Рекурсивное удаление папки", href: "file-system-functions.html#dir-operations", kind: "Функция", category: "Файловая система", letter: "D", description: "Удаляет папку вместе со всем содержимым." },
        { title: "DELETEFILE", label: "Удаление файла", href: "file-system-functions.html#file-operations", kind: "Функция", category: "Файловая система", letter: "D", description: "Удаляет файл." },
        { title: "DATE", label: "Текущая дата", href: "date-time-functions.html#date", kind: "Функция", category: "Дата и время", letter: "D", description: "Возвращает текущую локальную дату или дату указанного часового пояса в формате yyyy-MM-dd." },
        { title: "DATEADD", label: "Сдвиг даты и времени", href: "date-time-functions.html#dateadd", kind: "Функция", category: "Дата и время", letter: "D", description: "Сдвигает каноническую дату, время или дату-время на целое число интервалов." },
        { title: "DATEDIFF", label: "Разница по границам интервала", href: "date-time-functions.html#datediff", kind: "Функция", category: "Дата и время", letter: "D", description: "Возвращает signed LONG с количеством пересечённых границ интервала." },
        { title: "DATEPART", label: "Извлечение части по токену", href: "date-time-functions.html#datepart", kind: "Функция", category: "Дата и время", letter: "D", description: "Извлекает token-based часть из канонической даты, времени или даты-времени." },
        { title: "DATESERIAL", label: "Построение даты из компонентов", href: "date-time-functions.html#dateserial", kind: "Функция", category: "Дата и время", letter: "D", description: "Строит каноническую дату yyyy-MM-dd из года, месяца и дня." },
        { title: "DATEVALUE", label: "Каноническая дата из текста", href: "date-time-functions.html#datevalue", kind: "Функция", category: "Дата и время", letter: "D", description: "Возвращает каноническую дату yyyy-MM-dd из канонической даты или даты-времени." },
        { title: "DAYSINMONTH", label: "Число дней в месяце", href: "date-time-functions.html#daysinmonth", kind: "Функция", category: "Дата и время", letter: "D", description: "Возвращает число дней в указанном месяце указанного года." },
        { title: "DAY", label: "День месяца из даты", href: "date-time-functions.html#day", kind: "Функция", category: "Дата и время", letter: "D", description: "Извлекает день месяца из канонической даты или даты-времени." },
        { title: "DIRDATE", label: "Дата изменения папки", href: "file-system-functions.html#file-info", kind: "Функция", category: "Файловая система", letter: "D", description: "Возвращает дату и время последнего изменения папки." },
        { title: "DIREXISTS", label: "Проверка папки", href: "file-system-functions.html#exists", kind: "Функция", category: "Файловая система", letter: "D", description: "Проверяет, существует ли папка." },
        { title: "DIRNAME", label: "Папка из пути", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "D", description: "Возвращает имя папки из полного пути." },
        { title: "DIRSIZE", label: "Суммарный размер папки", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "D", description: "Считает размер файлов в папке и вложенных папках." },
        { title: "DRIVEFORMAT", label: "Формат файловой системы", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "D", description: "Возвращает строковое имя файловой системы диска." },
        { title: "DRIVENAME", label: "Имя диска", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "D", description: "Возвращает имя диска, к которому относится путь." },
        { title: "DRIVETYPE", label: "Тип диска", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "D", description: "Возвращает строковое имя типа диска." },
        { title: "DO ... LOOP", label: "Цикл общего назначения", href: "do-loop.html", kind: "Оператор", category: "Циклы", letter: "D", description: "Поддерживает пред-, пост- и безусловные формы цикла DO ... LOOP." },
        { title: "DOUBLE", label: "Вещественный тип", href: "data-types.html#double", kind: "Тип данных", category: "Типы данных", letter: "D", description: "64-битное число с плавающей точкой." },
        { title: "EOF", label: "Признак конца файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "E", description: "Показывает, достигнут ли конец открытого файла." },
        { title: "ENDSWITH", label: "Проверка конца строки", href: "string-functions.html#endswith", kind: "Функция", category: "Строковые функции", letter: "E", description: "Проверяет, заканчивается ли строка указанным фрагментом." },
        { title: "ENDOFMONTH", label: "Конец месяца", href: "date-time-functions.html#endofmonth", kind: "Функция", category: "Дата и время", letter: "E", description: "Возвращает каноническую дату последнего дня месяца." },
        { title: "ENDOFYEAR", label: "Конец года", href: "date-time-functions.html#endofyear", kind: "Функция", category: "Дата и время", letter: "E", description: "Возвращает каноническую дату последнего дня года." },
        { title: "EXP", label: "Экспонента", href: "numeric-functions.html#exp", kind: "Функция", category: "Числовые функции", letter: "E", description: "Вычисляет e в степени x." },
        { title: "EQV", label: "Логическая и побитовая равнозначность", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "E", description: "Работает как равнозначность для BOOLEAN и как побитовое отрицание XOR для INTEGER и LONG." },
        { title: "ERASE", label: "Сброс массива", href: "arrays.html#erase", kind: "Оператор", category: "Массивы", letter: "E", description: "Возвращает массив или динамическое массивное поле записи в невыделенное состояние до следующего REDIM." },
        { title: "EXTNAME", label: "Расширение файла", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "E", description: "Возвращает расширение файла." },
        { title: "FILEACCESSEDDATE", label: "Дата доступа к файлу", href: "file-system-functions.html#file-info", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает дату и время последнего доступа к файлу." },
        { title: "FILECREATEDATE", label: "Дата создания файла", href: "file-system-functions.html#file-info", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает дату и время создания файла." },
        { title: "FILEDATE", label: "Дата изменения файла", href: "file-system-functions.html#file-info", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает дату последнего изменения файла." },
        { title: "FILEEXISTS", label: "Проверка файла", href: "file-system-functions.html#exists", kind: "Функция", category: "Файловая система", letter: "F", description: "Проверяет, существует ли файл." },
        { title: "FILELENGTH", label: "Длина открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает текущую длину открытого файла в байтах." },
        { title: "FILEMODE", label: "Режим открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает режим открытого файла: INPUT, OUTPUT или APPEND." },
        { title: "FILENAME", label: "Имя файла", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает имя файла из пути." },
        { title: "FILEPATH", label: "Полный путь открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает полный путь открытого файла." },
        { title: "FILEPOS", label: "Позиция открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает текущую позицию открытого файла в байтах от начала." },
        { title: "FILESIZE", label: "Размер файла", href: "file-system-functions.html#file-info", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает размер файла в байтах." },
        { title: "FILE", label: "Тип открытого файла", href: "data-types.html#file", kind: "Тип данных", category: "Типы данных", letter: "F", description: "Хранит состояние открытого текстового файла." },
        { title: "FIX", label: "Отсечение к нулю", href: "numeric-functions.html#fix", kind: "Функция", category: "Числовые функции", letter: "F", description: "Отбрасывает дробную часть числа к нулю." },
        { title: "FLUSH", label: "Сброс буфера записи файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "F", description: "Принудительно записывает буфер открытого файла на диск." },
        { title: "FLOOR", label: "Округление вниз", href: "numeric-functions.html#floor", kind: "Функция", category: "Числовые функции", letter: "F", description: "Округляет число вниз." },
        { title: "FOR", label: "Счётный цикл", href: "for.html", kind: "Оператор", category: "Циклы", letter: "F", description: "Выполняет цикл со счётчиком и шагом STEP." },
        { title: "FROMUTC", label: "Перевод даты-времени UTC в другой пояс", href: "date-time-functions.html#fromutc", kind: "Функция", category: "Дата и время", letter: "F", description: "Переводит каноническую дату-время UTC в локальную дату-время хоста или в явно указанный часовой пояс." },
        { title: "FREESPACE", label: "Свободное место на диске", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает доступное свободное место на диске в байтах." },
        { title: "FULLPATH", label: "Полный путь", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "F", description: "Возвращает полный путь." },
        { title: "GETDIRS", label: "Перечисление папок", href: "file-system-functions.html#enumeration", kind: "Функция", category: "Файловая система", letter: "G", description: "Возвращает массив полных путей папок ближайшего уровня." },
        { title: "GETFILES", label: "Перечисление файлов", href: "file-system-functions.html#enumeration", kind: "Функция", category: "Файловая система", letter: "G", description: "Возвращает массив полных путей файлов ближайшего уровня." },
        { title: "FUNCTION", label: "Функция с результатом", href: "function.html", kind: "Конструкция", category: "Процедуры и функции", letter: "F", description: "Определяет функцию с параметрами и возвращаемым значением." },
        { title: "HOUR", label: "Час из времени", href: "date-time-functions.html#hour", kind: "Функция", category: "Дата и время", letter: "H", description: "Извлекает час из канонического времени или даты-времени." },
        { title: "IDENTIFIER", label: "Правила имён", href: "variables-and-constants.html#identifiers", kind: "Правило языка", category: "Декларации", letter: "I", description: "Описывает допустимые имена и ограничения на зарезервированные слова." },
        { title: "IF", label: "Условный оператор", href: "if.html", kind: "Оператор", category: "Ветвление", letter: "I", description: "Выполняет ветку по логическому условию." },
        { title: "IMP", label: "Логическая и побитовая импликация", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "I", description: "Работает как импликация для BOOLEAN и как побитовое NOT левого операнда с последующим OR для INTEGER и LONG." },
        { title: "INPUT", label: "Ввод данных", href: "input.html", kind: "Оператор", category: "Ввод и вывод", letter: "I", description: "Считывает данные пользователя в переменные." },
        { title: "INSERT", label: "Вставка фрагмента", href: "string-functions.html#insert", kind: "Функция", category: "Строковые функции", letter: "I", description: "Вставляет фрагмент в строку по указанной позиции." },
        { title: "ISABSOLUTEPATH", label: "Проверка абсолютного пути", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "I", description: "Показывает, является ли путь абсолютным." },
        { title: "ISDATE", label: "Проверка канонического date/time текста", href: "date-time-functions.html#isdate", kind: "Функция", category: "Дата и время", letter: "I", description: "Возвращает TRUE, если строка является канонической датой, временем или датой-временем Axiom." },
        { title: "ISLEAPYEAR", label: "Проверка високосного года", href: "date-time-functions.html#isleapyear", kind: "Функция", category: "Дата и время", letter: "I", description: "Возвращает TRUE для високосного года и FALSE для невисокосного." },
        { title: "ISHIDDEN", label: "Скрытый атрибут", href: "file-system-functions.html#object-flags", kind: "Функция", category: "Файловая система", letter: "I", description: "Проверяет скрытый атрибут файла или папки." },
        { title: "ISOPEN", label: "Проверка открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "I", description: "Показывает, связан ли FILE сейчас с открытым файлом." },
        { title: "ISREADONLY", label: "Атрибут только для чтения", href: "file-system-functions.html#object-flags", kind: "Функция", category: "Файловая система", letter: "I", description: "Проверяет атрибут только для чтения у файла или папки." },
        { title: "INT", label: "Округление вниз до INTEGER", href: "numeric-functions.html#int", kind: "Функция", category: "Числовые функции", letter: "I", description: "Округляет число вниз и возвращает INTEGER." },
        { title: "INTEGER", label: "Целочисленный тип", href: "data-types.html#integer", kind: "Тип данных", category: "Типы данных", letter: "I", description: "32-битное знаковое целое число." },
        { title: "INSTR", label: "Позиция фрагмента", href: "string-functions.html#instr", kind: "Функция", category: "Строковые функции", letter: "I", description: "Ищет первое вхождение фрагмента в строке." },
        { title: "INSTRREV", label: "Последнее вхождение фрагмента", href: "string-functions.html#instrrev", kind: "Функция", category: "Строковые функции", letter: "I", description: "Ищет фрагмент справа налево и возвращает его позицию." },
        { title: "JOIN", label: "Сборка строки из массива", href: "string-functions.html#join", kind: "Функция", category: "Строковые функции", letter: "J", description: "Собирает элементы строкового массива в одну строку." },
        { title: "LBOUND", label: "Нижняя граница массива", href: "arrays.html#lbound", kind: "Функция", category: "Массивы", letter: "L", description: "Возвращает нижнюю границу выделенного массива; для многомерной формы может принимать номер измерения." },
        { title: "LCASE", label: "Нижний регистр", href: "string-functions.html#lcase", kind: "Функция", category: "Строковые функции", letter: "L", description: "Преобразует строку к нижнему регистру." },
        { title: "LEFT", label: "Левый фрагмент строки", href: "string-functions.html#left", kind: "Функция", category: "Строковые функции", letter: "L", description: "Возвращает первые символы строки." },
        { title: "LEN", label: "Длина строки", href: "string-functions.html#len", kind: "Функция", category: "Строковые функции", letter: "L", description: "Возвращает количество символов в строке." },
        { title: "LINENUMBER", label: "Номер текущей строки файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "L", description: "Возвращает номер следующей текстовой строки в открытом файле." },
        { title: "LOG", label: "Натуральный логарифм", href: "numeric-functions.html#log", kind: "Функция", category: "Числовые функции", letter: "L", description: "Вычисляет натуральный логарифм." },
        { title: "LOG10", label: "Десятичный логарифм", href: "numeric-functions.html#log10", kind: "Функция", category: "Числовые функции", letter: "L", description: "Вычисляет логарифм по основанию 10." },
        { title: "LOG2", label: "Двоичный логарифм", href: "numeric-functions.html#log2", kind: "Функция", category: "Числовые функции", letter: "L", description: "Вычисляет логарифм по основанию 2." },
        { title: "LONG", label: "Расширенный целочисленный тип", href: "data-types.html#long", kind: "Тип данных", category: "Типы данных", letter: "L", description: "64-битное знаковое целое число." },
        { title: "LTRIM", label: "Обрезка слева", href: "string-functions.html#ltrim", kind: "Функция", category: "Строковые функции", letter: "L", description: "Убирает пробелы или символы только с левого края строки." },
        { title: "MAX", label: "Максимум", href: "numeric-functions.html#max", kind: "Функция", category: "Числовые функции", letter: "M", description: "Возвращает большее из двух значений." },
        { title: "MAKEDIR", label: "Создание папки", href: "file-system-functions.html#folders", kind: "Функция", category: "Файловая система", letter: "M", description: "Создаёт папку, если её ещё нет." },
        { title: "MAKETEMPDIR", label: "Создание временной папки", href: "file-system-functions.html#environment", kind: "Функция", category: "Файловая система", letter: "M", description: "Создаёт новую временную папку и возвращает её полный путь." },
        { title: "MAKETEMPFILE", label: "Создание временного файла", href: "file-system-functions.html#environment", kind: "Функция", category: "Файловая система", letter: "M", description: "Создаёт новый временный файл и возвращает его полный путь." },
        { title: "MID", label: "Подстрока по позиции", href: "string-functions.html#mid", kind: "Функция", category: "Строковые функции", letter: "M", description: "Возвращает фрагмент строки по позиции и длине." },
        { title: "MIN", label: "Минимум", href: "numeric-functions.html#min", kind: "Функция", category: "Числовые функции", letter: "M", description: "Возвращает меньшее из двух значений." },
        { title: "MOD", label: "Остаток от деления", href: "arithmetic-and-logic.html#mod", kind: "Оператор", category: "Арифметика и логика", letter: "M", description: "Возвращает остаток от деления для операндов INTEGER и LONG." },
        { title: "MOVEDIR", label: "Перенос папки", href: "file-system-functions.html#dir-operations", kind: "Функция", category: "Файловая система", letter: "M", description: "Переносит папку." },
        { title: "MOVEFILE", label: "Перенос файла", href: "file-system-functions.html#file-operations", kind: "Функция", category: "Файловая система", letter: "M", description: "Переносит файл." },
        { title: "NEWLINE", label: "Запись пустой строки в файл", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "N", description: "Добавляет в открытый файл только перевод строки." },
        { title: "NOW", label: "Текущие дата и время", href: "date-time-functions.html#now", kind: "Функция", category: "Дата и время", letter: "N", description: "Возвращает текущие локальные дату и время или текущие дату и время указанного часового пояса в формате yyyy-MM-dd HH:mm:ss." },
        { title: "MINUTE", label: "Минута из времени", href: "date-time-functions.html#minute", kind: "Функция", category: "Дата и время", letter: "M", description: "Извлекает минуту из канонического времени или даты-времени." },
        { title: "MONTH", label: "Месяц из даты", href: "date-time-functions.html#month", kind: "Функция", category: "Дата и время", letter: "M", description: "Извлекает номер месяца из канонической даты или даты-времени." },
        { title: "MONTHNAME", label: "Имя месяца", href: "date-time-functions.html#monthname", kind: "Функция", category: "Дата и время", letter: "M", description: "Возвращает фиксированное английское имя месяца по номеру от 1 до 12." },
        { title: "NOT", label: "Логическое и побитовое НЕ", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "N", description: "Работает как логическое НЕ для BOOLEAN и как побитовое НЕ для INTEGER и LONG." },
        { title: "OR", label: "Логическое и побитовое ИЛИ", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "O", description: "Работает как логическое ИЛИ для BOOLEAN и как побитовое ИЛИ для INTEGER и LONG." },
        { title: "OPEN", label: "Открытие текстового файла", href: "open.html", kind: "Оператор", category: "Ввод и вывод", letter: "O", description: "Открывает текстовый файл в переменную типа FILE." },
        { title: "PADLEFT", label: "Дополнение слева", href: "string-functions.html#padleft", kind: "Функция", category: "Строковые функции", letter: "P", description: "Дополняет строку слева до нужной ширины." },
        { title: "PADRIGHT", label: "Дополнение справа", href: "string-functions.html#padright", kind: "Функция", category: "Строковые функции", letter: "P", description: "Дополняет строку справа до нужной ширины." },
        { title: "PATHEXISTS", label: "Проверка файла или папки", href: "file-system-functions.html#exists", kind: "Функция", category: "Файловая система", letter: "P", description: "Проверяет, существует ли файл или папка." },
        { title: "PATHROOT", label: "Корень пути", href: "file-system-functions.html#paths", kind: "Функция", category: "Файловая система", letter: "P", description: "Возвращает корневую часть полного пути." },
        { title: "POW", label: "Возведение в степень", href: "numeric-functions.html#pow", kind: "Функция", category: "Числовые функции", letter: "P", description: "Возводит число в степень." },
        { title: "PRINT", label: "Вывод значений", href: "print.html", kind: "Оператор", category: "Ввод и вывод", letter: "P", description: "Выводит текст, числа и результаты выражений." },
        { title: "PRINT USING", label: "Форматированный вывод", href: "print.html#print-using", kind: "Форма оператора", category: "Ввод и вывод", letter: "P", description: "Выводит значения по строке формата." },
        { title: "REMOVE", label: "Удаление фрагмента", href: "string-functions.html#remove", kind: "Функция", category: "Строковые функции", letter: "R", description: "Удаляет часть строки по позиции и количеству символов." },
        { title: "READALLLINES", label: "Чтение всех строк файла", href: "file-system-functions.html#text-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Возвращает строки текстового файла как массив STRING(); при недопустимом UTF-8 читает Windows-1251." },
        { title: "READBYTE", label: "Чтение байта (зарезервировано)", href: "file-system-functions.html#future-enumeration", kind: "Будущая функция", category: "Файловая система", letter: "R", description: "Запланирована для отдельного двоичного режима файловой подсистемы." },
        { title: "READFILE", label: "Чтение текстового файла", href: "file-system-functions.html#text-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Читает текстовый файл целиком; при недопустимом UTF-8 использует Windows-1251." },
        { title: "READLINE", label: "Чтение строки из открытого файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Читает одну строку из открытого текстового файла." },
        { title: "READLINES", label: "Чтение набора строк", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Возвращает следующие строки открытого текстового файла как массив STRING()." },
        { title: "READTEXT", label: "Чтение фрагмента текста", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Читает заданное число символов из открытого файла." },
        { title: "READTOEND", label: "Чтение остатка файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Читает весь оставшийся текст от текущей позиции до конца файла." },
        { title: "REPEAT", label: "Повторение строки", href: "string-functions.html#repeat", kind: "Функция", category: "Строковые функции", letter: "R", description: "Повторяет строку указанное число раз." },
        { title: "REPLACE", label: "Замена фрагментов", href: "string-functions.html#replace", kind: "Функция", category: "Строковые функции", letter: "R", description: "Заменяет все найденные фрагменты другой строкой." },
        { title: "REWIND", label: "Переход в начало файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "R", description: "Переводит открытый файл в начало." },
        { title: "REGEXCOUNT", label: "Подсчёт совпадений по образцу", href: "regex-functions.html#regexcount", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Считает количество совпадений с регулярным выражением." },
        { title: "REGEXEXTRACTALL", label: "Группы из всех совпадений", href: "regex-functions.html#regexextractall", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Возвращает значения указанной группы из всех найденных совпадений как массив STRING()." },
        { title: "REGEXEXTRACT", label: "Извлечение группы", href: "regex-functions.html#regexextract", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Возвращает указанную группу из первого найденного совпадения." },
        { title: "REGEXISMATCH", label: "Проверка совпадения по образцу", href: "regex-functions.html#regexismatch", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Проверяет, есть ли в строке совпадение с регулярным выражением." },
        { title: "REGEXMATCH", label: "Первое совпадение по образцу", href: "regex-functions.html#regexmatch", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Возвращает первое найденное совпадение целиком." },
        { title: "REGEXMATCHES", label: "Все совпадения", href: "regex-functions.html#regexmatches", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Возвращает все найденные совпадения как массив STRING()." },
        { title: "REGEXREPLACE", label: "Замена по образцу", href: "regex-functions.html#regexreplace", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Заменяет совпадения с регулярным выражением." },
        { title: "REGEXSPLIT", label: "Разделение по образцу", href: "regex-functions.html#regexsplit", kind: "Функция", category: "Регулярные выражения", letter: "R", description: "Разделяет строку по регулярному выражению и возвращает массив STRING()." },
        { title: "RENAMEDIR", label: "Переименование папки", href: "file-system-functions.html#dir-operations", kind: "Функция", category: "Файловая система", letter: "R", description: "Переименовывает папку." },
        { title: "RENAMEFILE", label: "Переименование файла", href: "file-system-functions.html#file-operations", kind: "Функция", category: "Файловая система", letter: "R", description: "Переименовывает файл." },
        { title: "RETURN", label: "Возврат из процедуры или функции", href: "return.html", kind: "Оператор", category: "Процедуры и функции", letter: "R", description: "Завершает SUB или возвращает значение из FUNCTION." },
        { title: "REDIM", label: "Выделение и смена диапазона массива", href: "arrays.html#redim", kind: "Оператор", category: "Массивы", letter: "R", description: "Выделяет диапазон элементов для массива или динамического массивного поля записи; форма REDIM PRESERVE сохраняет пересечение старого и нового диапазона, а 1 TO 0 задаёт пустой выделенный массив." },
        { title: "REVERSE", label: "Разворот строки", href: "string-functions.html#reverse", kind: "Функция", category: "Строковые функции", letter: "R", description: "Возвращает строку в обратном порядке символов." },
        { title: "RIGHT", label: "Правый фрагмент строки", href: "string-functions.html#right", kind: "Функция", category: "Строковые функции", letter: "R", description: "Возвращает последние символы строки." },
        { title: "RND", label: "Псевдослучайное число", href: "numeric-functions.html#rnd", kind: "Функция", category: "Числовые функции", letter: "R", description: "Возвращает псевдослучайное значение DOUBLE в диапазоне [0, 1)." },
        { title: "ROUND", label: "Округление к ближайшему", href: "numeric-functions.html#round", kind: "Функция", category: "Числовые функции", letter: "R", description: "Округляет число по правилу половин от нуля." },
        { title: "RTRIM", label: "Обрезка справа", href: "string-functions.html#rtrim", kind: "Функция", category: "Строковые функции", letter: "R", description: "Убирает пробелы или символы только с правого края строки." },
        { title: "SECOND", label: "Секунда из времени", href: "date-time-functions.html#second", kind: "Функция", category: "Дата и время", letter: "S", description: "Извлекает секунду из канонического времени или даты-времени." },
        { title: "SELECT CASE", label: "Многовариантное ветвление", href: "select-case.html", kind: "Оператор", category: "Ветвление", letter: "S", description: "Сравнивает одно выражение с ветвями CASE и выбирает первую подходящую." },
        { title: "SEEKFILE", label: "Переход по позиции файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "S", description: "Переводит открытый файл на указанную позицию в байтах." },
        { title: "SETSIGN", label: "Перенос знака", href: "numeric-functions.html#setsign", kind: "Функция", category: "Числовые функции", letter: "S", description: "Переносит знак второго аргумента на первый." },
        { title: "SGN", label: "Знак числа", href: "numeric-functions.html#sgn", kind: "Функция", category: "Числовые функции", letter: "S", description: "Возвращает -1, 0 или 1 как INTEGER." },
        { title: "SIN", label: "Синус", href: "numeric-functions.html#sin", kind: "Функция", category: "Числовые функции", letter: "S", description: "Вычисляет синус угла в радианах." },
        { title: "SINH", label: "Гиперболический синус", href: "numeric-functions.html#sinh", kind: "Функция", category: "Числовые функции", letter: "S", description: "Вычисляет гиперболический синус." },
        { title: "SPACE", label: "Строка из пробелов", href: "string-functions.html#space", kind: "Функция", category: "Строковые функции", letter: "S", description: "Возвращает строку из заданного количества пробелов." },
        { title: "SKIPLINE", label: "Пропуск строки файла", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "S", description: "Пропускает одну строку в открытом файле и возвращает TRUE." },
        { title: "SPLIT", label: "Разделение строки в массив", href: "string-functions.html#split", kind: "Функция", category: "Строковые функции", letter: "S", description: "Разбивает строку на одномерный массив строк." },
        { title: "SQR", label: "Квадратный корень", href: "numeric-functions.html#sqr", kind: "Функция", category: "Числовые функции", letter: "S", description: "Вычисляет квадратный корень неотрицательного числа." },
        { title: "STARTSWITH", label: "Проверка начала строки", href: "string-functions.html#startswith", kind: "Функция", category: "Строковые функции", letter: "S", description: "Проверяет, начинается ли строка указанным фрагментом." },
        { title: "STARTOFMONTH", label: "Начало месяца", href: "date-time-functions.html#startofmonth", kind: "Функция", category: "Дата и время", letter: "S", description: "Возвращает каноническую дату первого дня месяца." },
        { title: "STARTOFYEAR", label: "Начало года", href: "date-time-functions.html#startofyear", kind: "Функция", category: "Дата и время", letter: "S", description: "Возвращает каноническую дату первого дня года." },
        { title: "STR", label: "Число в строку", href: "numeric-functions.html#str", kind: "Функция", category: "Числовые функции", letter: "S", description: "Преобразует число в строковое представление." },
        { title: "STRING", label: "Строковый тип", href: "data-types.html#string", kind: "Тип данных", category: "Типы данных", letter: "S", description: "Хранит текстовые данные в кодировке Unicode; форма STRING * N задаёт фиксированную длину хранения." },
        { title: "STRING (функция)", label: "Заполнение строки символом", href: "string-functions.html#string-fill", kind: "Функция", category: "Строковые функции", letter: "S", description: "Создаёт строку заданной длины из повторяющегося символа." },
        { title: "SUB", label: "Подпрограмма без результата", href: "sub.html", kind: "Конструкция", category: "Процедуры и функции", letter: "S", description: "Определяет подпрограмму, которую вызывают как отдельный оператор." },
        { title: "SUBSTRCOUNT", label: "Подсчёт вхождений фрагмента", href: "string-functions.html#substrcount", kind: "Функция", category: "Строковые функции", letter: "S", description: "Считает непересекающиеся вхождения фрагмента в строке." },
        { title: "TAN", label: "Тангенс", href: "numeric-functions.html#tan", kind: "Функция", category: "Числовые функции", letter: "T", description: "Вычисляет тангенс угла в радианах." },
        { title: "TANH", label: "Гиперболический тангенс", href: "numeric-functions.html#tanh", kind: "Функция", category: "Числовые функции", letter: "T", description: "Вычисляет гиперболический тангенс." },
        { title: "TIME", label: "Текущее время", href: "date-time-functions.html#time", kind: "Функция", category: "Дата и время", letter: "T", description: "Возвращает текущее локальное время или время указанного часового пояса в формате HH:mm:ss." },
        { title: "TIMESERIAL", label: "Построение времени из компонентов", href: "date-time-functions.html#timeserial", kind: "Функция", category: "Дата и время", letter: "T", description: "Строит каноническое время HH:mm:ss из часа, минуты и секунды." },
        { title: "TIMER", label: "Секунды с локальной полуночи", href: "date-time-functions.html#timer", kind: "Функция", category: "Дата и время", letter: "T", description: "Возвращает количество секунд, прошедших с локальной полуночи текущего дня." },
        { title: "TIMEVALUE", label: "Каноническое время из текста", href: "date-time-functions.html#timevalue", kind: "Функция", category: "Дата и время", letter: "T", description: "Возвращает каноническое время HH:mm:ss из канонического времени или даты-времени." },
        { title: "TOUTC", label: "Перевод даты-времени в UTC", href: "date-time-functions.html#toutc", kind: "Функция", category: "Дата и время", letter: "T", description: "Переводит каноническую локальную дату-время или дату-время указанного часового пояса в каноническую дату-время UTC." },
        { title: "TOTALFREESPACE", label: "Общее свободное место на диске", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "T", description: "Возвращает общее свободное место на диске в байтах." },
        { title: "TOTALSPACE", label: "Полный размер диска", href: "file-system-functions.html#dir-info", kind: "Функция", category: "Файловая система", letter: "T", description: "Возвращает полный размер диска в байтах." },
        { title: "TRIM", label: "Обрезка строки по краям", href: "string-functions.html#trim", kind: "Функция", category: "Строковые функции", letter: "T", description: "Убирает пробелы или указанные символы с двух краёв строки." },
        { title: "TYPE", label: "Пользовательский тип записи", href: "type.html", kind: "Оператор", category: "Типы данных", letter: "T", description: "Объявляет пользовательский тип записи с именованными и групповыми полями, доступом через точку, использованием в массивах, динамическими и фиксированными массивными полями, фиксированными строками STRING * N, а также CONST-границами фиксированных полей." },
        { title: "UBOUND", label: "Верхняя граница массива", href: "arrays.html#ubound", kind: "Функция", category: "Массивы", letter: "U", description: "Возвращает верхнюю границу выделенного массива; для многомерной формы может принимать номер измерения." },
        { title: "UCASE", label: "Верхний регистр", href: "string-functions.html#ucase", kind: "Функция", category: "Строковые функции", letter: "U", description: "Преобразует строку к верхнему регистру." },
        { title: "UTCDATE", label: "Текущая дата в UTC", href: "date-time-functions.html#utcdate", kind: "Функция", category: "Дата и время", letter: "U", description: "Возвращает текущую дату в UTC в формате yyyy-MM-dd." },
        { title: "UTCNOW", label: "Текущие дата и время в UTC", href: "date-time-functions.html#utcnow", kind: "Функция", category: "Дата и время", letter: "U", description: "Возвращает текущие дату и время в UTC в формате yyyy-MM-dd HH:mm:ss." },
        { title: "UTCOFFSET", label: "Смещение от UTC", href: "date-time-functions.html#utcoffset", kind: "Функция", category: "Дата и время", letter: "U", description: "Возвращает текущее локальное смещение хоста или смещение для указанной даты-времени, в том числе для явно заданного часового пояса." },
        { title: "UTCTIME", label: "Текущее время в UTC", href: "date-time-functions.html#utctime", kind: "Функция", category: "Дата и время", letter: "U", description: "Возвращает текущее время в UTC в формате HH:mm:ss." },
        { title: "WEEKDAY", label: "Номер дня недели", href: "date-time-functions.html#weekday", kind: "Функция", category: "Дата и время", letter: "W", description: "Возвращает номер дня недели по схеме 1 = понедельник, 7 = воскресенье." },
        { title: "WEEKDAYNAME", label: "Имя дня недели", href: "date-time-functions.html#weekdayname", kind: "Функция", category: "Дата и время", letter: "W", description: "Возвращает фиксированное английское имя дня недели по нумерации от понедельника, от 1 до 7." },
        { title: "YEAR", label: "Год из даты", href: "date-time-functions.html#year", kind: "Функция", category: "Дата и время", letter: "Y", description: "Извлекает год из канонической даты или даты-времени." },
        { title: "VAL", label: "Строка в число", href: "numeric-functions.html#val", kind: "Функция", category: "Числовые функции", letter: "V", description: "Пытается прочитать число из строки и при ошибке возвращает 0." },
        { title: "WHILE", label: "Предусловный цикл", href: "while.html", kind: "Оператор", category: "Циклы", letter: "W", description: "Проверяет условие перед каждой итерацией цикла." },
        { title: "WRITE", label: "Запись в открытый файл", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "W", description: "Записывает текст в открытый файл без перевода строки." },
        { title: "WRITEBYTE", label: "Запись байта (зарезервировано)", href: "file-system-functions.html#future-enumeration", kind: "Будущая функция", category: "Файловая система", letter: "W", description: "Запланирована для отдельного двоичного режима файловой подсистемы." },
        { title: "WRITEFILE", label: "Запись текстового файла", href: "file-system-functions.html#text-files", kind: "Функция", category: "Файловая система", letter: "W", description: "Создаёт или перезаписывает текстовый файл." },
        { title: "WRITELINE", label: "Запись строки в открытый файл", href: "file-system-functions.html#open-files", kind: "Функция", category: "Файловая система", letter: "W", description: "Записывает текст в открытый файл и завершает строку." },
        { title: "XOR", label: "Логическое и побитовое исключающее ИЛИ", href: "arithmetic-and-logic.html#logical-bitwise", kind: "Оператор", category: "Арифметика и логика", letter: "X", description: "Работает как логическое XOR для BOOLEAN и как побитовое XOR для INTEGER и LONG." },
        { title: "Возведение в степень (^)", label: "Оператор степени", href: "arithmetic-and-logic.html#power", kind: "Оператор", category: "Арифметика и логика", letter: "В", description: "Возводит левый операнд в степень правого и возвращает DOUBLE." },
        { title: "Целочисленное деление (\\)", label: "Деление с отбрасыванием дробной части", href: "arithmetic-and-logic.html#integer-division", kind: "Оператор", category: "Арифметика и логика", letter: "Ц", description: "Делит значения типов INTEGER и LONG и отбрасывает дробную часть к нулю." },
        { title: "Сдвиг влево (<<)", label: "Побитовый сдвиг влево", href: "arithmetic-and-logic.html#shifts", kind: "Оператор", category: "Арифметика и логика", letter: "С", description: "Сдвигает биты INTEGER или LONG влево." },
        { title: "Сдвиг вправо (>>)", label: "Побитовый сдвиг вправо", href: "arithmetic-and-logic.html#shifts", kind: "Оператор", category: "Арифметика и логика", letter: "С", description: "Сдвигает биты INTEGER или LONG вправо." },
        { title: "Шестнадцатеричный литерал (&H)", label: "Литерал целого числа по основанию 16", href: "arithmetic-and-logic.html#hex-literals", kind: "Форма литерала", category: "Арифметика и логика", letter: "Ш", description: "Задаёт неотрицательное INTEGER или LONG в форме &H..." }
    ];

    const currentPageId = document.body?.dataset.page ?? "";
    const navHost = document.getElementById("manual-nav");
    const pagerHost = document.getElementById("page-pager");
    const referenceHost = document.getElementById("reference-catalog");
    const letterFiltersHost = document.getElementById("reference-letter-filters");
    const categoryFiltersHost = document.getElementById("reference-category-filters");
    const flatNavItems = nav.flatMap(section => section.items);

    function renderNav() {
        if (!navHost) {
            return;
        }

        navHost.className = "manual-nav";
        navHost.innerHTML = "";

        for (const section of nav) {
            const sectionElement = document.createElement("section");
            sectionElement.className = "manual-nav__section";

            const titleElement = document.createElement("h3");
            titleElement.className = "manual-nav__section-title";
            titleElement.textContent = section.title;
            sectionElement.appendChild(titleElement);

            const listElement = document.createElement("ul");
            listElement.className = "manual-nav__list";

            for (const item of section.items) {
                const listItem = document.createElement("li");
                const link = document.createElement("a");
                link.className = "manual-nav__link";

                if (item.id === currentPageId) {
                    link.classList.add("is-active");
                    link.setAttribute("aria-current", "page");
                }

                link.href = item.href;
                link.textContent = item.label;
                listItem.appendChild(link);
                listElement.appendChild(listItem);
            }

            sectionElement.appendChild(listElement);
            navHost.appendChild(sectionElement);
        }
    }

    function renderPager() {
        if (!pagerHost) {
            return;
        }

        const currentIndex = flatNavItems.findIndex(item => item.id === currentPageId);
        if (currentIndex < 0) {
            pagerHost.innerHTML = "";
            return;
        }

        const previous = currentIndex > 0 ? flatNavItems[currentIndex - 1] : null;
        const next = currentIndex < flatNavItems.length - 1 ? flatNavItems[currentIndex + 1] : null;

        pagerHost.innerHTML = "";

        if (previous) {
            const previousLink = document.createElement("a");
            previousLink.className = "pager__link pager__link--prev";
            previousLink.href = previous.href;
            previousLink.textContent = "< " + previous.label;
            pagerHost.appendChild(previousLink);
        }

        if (next) {
            const nextLink = document.createElement("a");
            nextLink.className = "pager__link pager__link--next";
            nextLink.href = next.href;
            nextLink.textContent = next.label + " >";
            pagerHost.appendChild(nextLink);
        }
    }

    function setupNavToggle() {
        const toggle = document.querySelector("[data-nav-toggle]");
        if (!(toggle instanceof HTMLButtonElement)) {
            return;
        }

        toggle.addEventListener("click", () => {
            document.body.classList.toggle("nav-open");
        });

        navHost?.addEventListener("click", event => {
            const target = event.target;
            if (target instanceof HTMLAnchorElement) {
                document.body.classList.remove("nav-open");
            }
        });
    }

    function renderReferenceCatalog() {
        if (!referenceHost || !letterFiltersHost || !categoryFiltersHost) {
            return;
        }

        const letters = Array.from(new Set(catalogEntries.map(entry => entry.letter))).sort((left, right) => left.localeCompare(right, "ru"));
        const categories = Array.from(new Set(catalogEntries.map(entry => entry.category))).sort((left, right) => left.localeCompare(right, "ru"));
        let activeLetter = "ALL";
        let activeCategory = "ALL";

        const renderButtons = (host, values, getLabel, onSelect) => {
            host.innerHTML = "";

            for (const value of values) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "filter-chip";
                button.dataset.value = value;
                button.textContent = getLabel(value);
                button.addEventListener("click", () => onSelect(value));
                host.appendChild(button);
            }
        };

        const updateFilterState = () => {
            for (const button of letterFiltersHost.querySelectorAll("button")) {
                button.classList.toggle("is-active", button.dataset.value === activeLetter);
            }

            for (const button of categoryFiltersHost.querySelectorAll("button")) {
                button.classList.toggle("is-active", button.dataset.value === activeCategory);
            }
        };

        const updateCatalog = () => {
            referenceHost.innerHTML = "";

            const filteredEntries = catalogEntries.filter(entry => {
                const matchesLetter = activeLetter === "ALL" || entry.letter === activeLetter;
                const matchesCategory = activeCategory === "ALL" || entry.category === activeCategory;
                return matchesLetter && matchesCategory;
            });

            if (filteredEntries.length === 0) {
                const empty = document.createElement("p");
                empty.className = "empty-state";
                empty.textContent = "По выбранным фильтрам ничего не найдено.";
                referenceHost.appendChild(empty);
                updateFilterState();
                return;
            }

            filteredEntries.sort((left, right) => left.title.localeCompare(right.title, "ru"));

            for (const entry of filteredEntries) {
                const card = document.createElement("article");
                card.className = "reference-card";

                const title = document.createElement("h3");
                title.className = "reference-card__title";
                const link = document.createElement("a");
                link.href = entry.href;
                link.textContent = entry.title;
                title.appendChild(link);
                card.appendChild(title);

                const meta = document.createElement("p");
                meta.className = "reference-card__meta";
                meta.textContent = entry.kind + " • " + entry.category;
                card.appendChild(meta);

                const label = document.createElement("p");
                label.className = "reference-card__label";
                label.textContent = entry.label;
                card.appendChild(label);

                const description = document.createElement("p");
                description.className = "reference-card__description";
                description.textContent = entry.description;
                card.appendChild(description);

                referenceHost.appendChild(card);
            }

            updateFilterState();
        };

        renderButtons(letterFiltersHost, ["ALL", ...letters], value => value === "ALL" ? "Все" : value, value => {
            activeLetter = value;
            updateCatalog();
        });

        renderButtons(categoryFiltersHost, ["ALL", ...categories], value => value === "ALL" ? "Все разделы" : value, value => {
            activeCategory = value;
            updateCatalog();
        });

        updateCatalog();
    }

    renderNav();
    renderPager();
    setupNavToggle();
    renderReferenceCatalog();
})();
