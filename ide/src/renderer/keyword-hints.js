// Axiom IDE — пояснения к ключевым словам для подсказки при наведении.
// Каждое слово описано на русском и английском языках тем, что оно делает
// в языке Axiom. EACH и IN — зарезервированные ключевые слова заголовка
// FOR EACH: как имена они не используются.
(function(root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.AxiomKeywordHints = api;
})(typeof window !== 'undefined' ? window : globalThis, function() {
  const HINTS = {
    ANDALSO: ['Логическое И для BOOLEAN. Правый операнд вычисляется только при TRUE слева.', 'Logical AND for BOOLEAN. The right operand is evaluated only when the left operand is TRUE.'],
    ORELSE: ['Логическое ИЛИ для BOOLEAN. Правый операнд вычисляется только при FALSE слева.', 'Logical OR for BOOLEAN. The right operand is evaluated only when the left operand is FALSE.'],
    RANDOMIZE: ['С начальным INTEGER или LONG в диапазоне INTEGER задаёт состояние генератора RND. Повторное зерно повторяет последовательность. Без аргумента состояние не меняется.', 'With an INTEGER or LONG seed in the INTEGER range, sets the RND generator state. Reusing a seed repeats the sequence. Without an argument, leaves the state unchanged.'],
    AND: ["Логическое И для BOOLEAN или побитовое И для целых. Оба операнда вычисляются; для условного вычисления служит ANDALSO.","Logical AND for BOOLEAN or bitwise AND for integers. Both operands are evaluated; use ANDALSO for conditional evaluation."],
    APPEND: ['Режим открытия файла в OPEN: записи добавляются в конец существующего файла.', 'File open mode in OPEN: new records are appended to the end of an existing file.'],
    AS: ['Вводит тип: в объявлении задаёт тип переменной, в заголовке подпрограммы — тип параметра или результата.', 'Introduces a type: the variable type in a declaration, or a parameter or result type in a routine header.'],
    BEGINFRAME: ['Открывает кадр графического вывода вместе с PRESENT. Между ними рисуется содержимое одного кадра.', 'Opens a graphics frame together with PRESENT; the drawing between them belongs to one frame.'],
    BYREF: ['Передаёт аргумент по ссылке: подпрограмма работает с той же переменной, и изменения видны вызывающему коду. В FOR EACH задаёт адресацию текущего элемента массива.', 'Passes an argument by reference so the routine works on the caller variable itself. In FOR EACH it selects the current array element.'],
    BYVAL: ["Передаёт независимую копию значения, включая вложенные массивы, словари, множества и поля TYPE. Ресурсы и функции сохраняют свою идентичность. В FOR EACH задаёт копию элемента и действует по умолчанию.","Passes an independent value copy, including nested arrays, dictionaries, sets and TYPE fields. Resources and functions retain their identity. In FOR EACH it copies the element and is the default."],
    CAMERA: ["Смещает камеру CANVAS по мировым координатам X и Y. RESETCAMERA сбрасывает смещение.","Offsets the CANVAS camera by world X and Y coordinates. RESETCAMERA resets the offset."],
    CANVAS: ['Создаёт холст заданного размера для рисования в современном графическом режиме.', 'Creates a canvas of the given size for drawing in the modern graphics mode.'],
    CASE: ['Ветвь выбора в SELECT CASE: сравнивает значение с образцом или диапазоном.', 'A SELECT CASE branch that compares the value with a pattern or range.'],
    CATCH: ['Ветвь TRY, которая получает ошибку времени выполнения и обрабатывает её.', 'A TRY branch that receives and handles a runtime error.'],
    CIRCLE: ['Рисует окружность или эллипс в графическом режиме SCREEN.', 'Draws a circle or ellipse in SCREEN graphics mode.'],
    CLOSE: ['Закрывает файл, открытый через OPEN, и сохраняет буфер на диск.', 'Closes a file opened with OPEN and flushes its buffer to disk.'],
    CLS: ['Очищает экран или графическую поверхность, при необходимости заданным цветом.', 'Clears the screen or graphics surface, optionally with a given color.'],
    COLOR: ['Задаёт цвет текста и фона для последующего вывода.', 'Sets the text foreground and background color for later output.'],
    CONST: ['Объявляет константу: значение задаётся один раз и не может быть изменено.', 'Declares a constant: the value is set once and cannot be changed.'],
    CONTINUE: ['Переходит к следующему проходу цикла, пропуская остаток тела: CONTINUE FOR, CONTINUE WHILE или CONTINUE DO.', 'Jumps to the next loop iteration, skipping the rest of the body: CONTINUE FOR, CONTINUE WHILE or CONTINUE DO.'],
    DICTIONARY: ['Тип типизированного словаря: DICTIONARY(тип ключа, тип значения) задаёт ключ и значение всех записей. Ключом служит STRING, INTEGER или LONG.', 'Typed dictionary type: DICTIONARY(key type, value type) sets the key and value of every entry. The key is STRING, INTEGER or LONG.'],
    HASHSET: ['Тип типизированного множества уникальных значений: HASHSET(тип элемента) задаёт тип всех элементов. Элементом служит STRING, INTEGER или LONG. Повторное добавление не создаёт второй записи.', 'Typed set of unique values: HASHSET(element type) sets the type of every element. The element is STRING, INTEGER or LONG. Adding the same value twice does not create a second entry.'],
    DIM: ['Объявляет переменную, массив или их группу с типом и необязательным начальным значением.', 'Declares a variable, an array or a group of them with a type and an optional initial value.'],
    DO: ['Открывает цикл DO … LOOP с проверкой условия до или после тела.', 'Opens a DO … LOOP cycle with the condition checked before or after the body.'],
    DRAW: ['Выполняет графическую команду, записанную строкой, как в языке управления пером.', 'Runs a graphics command written as a string, in pen-control style.'],
    DRAWCIRCLE: ['Рисует окружность современными графическими средствами с заданным цветом.', 'Draws a circle with the modern graphics primitives and a given color.'],
    DRAWIMAGE: ['Выводит изображение в заданной точке с указанными размерами.', 'Draws an image at the given point with the given size.'],
    DRAWLINE: ['Рисует отрезок между двумя точками.', 'Draws a line segment between two points.'],
    DRAWRECT: ['Рисует прямоугольник по координатам и размерам.', 'Draws a rectangle from coordinates and size.'],
    DRAWSPRITE: ['Выводит кадр спрайта в заданной точке.', 'Draws one sprite frame at the given point.'],
    DRAWTILE: ['Выводит один тайл из набора тайлов в заданной точке.', 'Draws a single tile from a tileset at the given point.'],
    DRAWTILELAYER: ['Выводит один слой тайловой карты.', 'Draws one layer of a tilemap.'],
    DRAWTILEMAP: ['Выводит тайловую карту целиком.', 'Draws a whole tilemap.'],
    DRAWTEXT: ['Выводит строку текста в графическом режиме с заданным цветом.', 'Draws a text string in graphics mode with a given color.'],
    EACH: ['Ключевое слово, вводящее обход массива в заголовке FOR EACH.', 'Keyword that introduces array iteration in a FOR EACH header.'],
    ELSE: ['Ветвь IF, которая выполняется, когда условие ложно.', 'The IF branch that runs when the condition is false.'],
    ELSEIF: ['Дополнительное условие в IF, проверяемое после ложного предыдущего.', 'An additional IF condition checked after a previous false one.'],
    END: ['Закрывает составную конструкцию: END IF, END WHILE, END SUB, END FUNCTION, END TYPE, END SELECT, END TRY.', 'Closes a compound construct: END IF, END WHILE, END SUB, END FUNCTION, END TYPE, END SELECT, END TRY.'],
    ERROR: ['Оператор, который создаёт ошибку времени выполнения с заданным кодом и сообщением.', 'A statement that raises a runtime error with a given code and message.'],
    EQV: ['Логическая эквивалентность BOOLEAN или побитовая эквивалентность целых.', 'Boolean equivalence or bitwise equivalence of integers.'],
    ERASE: ['Освобождает память динамического массива, сохраняя его имя и тип.', 'Releases a dynamic array storage while keeping its name and type.'],
    EXIT: ['Досрочно завершает цикл: EXIT FOR или EXIT DO.', 'Leaves a loop early: EXIT FOR or EXIT DO.'],
    FALSE: ['Логическое значение «ложь».', 'The logical value false.'],
    TRUE: ['Логическое значение «истина».', 'The logical value true.'],
    FILLCIRCLE: ['Рисует заполненный круг.', 'Draws a filled circle.'],
    FILLRECT: ['Рисует заполненный прямоугольник.', 'Draws a filled rectangle.'],
    FINALLY: ['Ветвь TRY, которая выполняется всегда: и при ошибке, и без неё.', 'The TRY branch that always runs, with or without an error.'],
    FOR: ['Открывает цикл: числовой FOR … TO … STEP или обход массива FOR EACH … IN. Закрывается словом NEXT.', 'Opens a loop: numeric FOR … TO … STEP, or array iteration FOR EACH … IN. Closed by NEXT.'],
    FUNCTION: ["Объявляет функцию с параметрами и типом результата. RETURN возвращает значение совместимого типа.","Declares a function with parameters and a result type. RETURN supplies a value of a compatible type."],
    GET: ['Копирует область экрана в буфер изображения.', 'Copies a screen region into an image buffer.'],
    IF: ['Открывает условную конструкцию; в одной строке допускает короткую форму.', 'Opens a conditional construct and allows a single-line short form.'],
    IMP: ['Логическая импликация: результат ложен только когда первый операнд истинен, а второй ложен.', 'Logical implication: false only when the first operand is true and the second is false.'],
    IMPORT: ['Подключает другой модуль проекта и его открытые имена.', 'Imports another project module and its public names.'],
    IN: ['Ключевое слово заголовка FOR EACH: отделяет переменную цикла от массива-источника.', 'Keyword of the FOR EACH header that separates the loop variable from the source array.'],
    INPUT: ["Читает значения с консоли; LINE INPUT читает строку целиком. FOR INPUT в OPEN открывает текстовый файл для чтения.","Reads console values; LINE INPUT reads a whole line. FOR INPUT in OPEN opens a text file for reading."],
    IS: ['Сравнение с образцом в SELECT CASE, например IS > 10.', 'A pattern comparison in SELECT CASE, for example IS > 10.'],
    KEY: ['Указывает клавишу в обработчике ON KEY и связанных конструкциях.', 'Names a key in an ON KEY handler and related constructs.'],
    LINE: ["Рисует отрезок или прямоугольник в SCREEN. LINE INPUT читает строку в строковую переменную.","Draws a line or rectangle in SCREEN. LINE INPUT reads a line into a string variable."],
    LOCATE: ['Ставит курсор текстового вывода в заданную строку и столбец.', 'Moves the text output cursor to a given row and column.'],
    LOOP: ['Закрывает цикл DO … LOOP, при необходимости с условием WHILE или UNTIL.', 'Closes a DO … LOOP cycle, optionally with a WHILE or UNTIL condition.'],
    MOD: ['Остаток от целочисленного деления.', 'The remainder of integer division.'],
    MOUSE: ['Указывает кнопку мыши в обработчике ON MOUSE и связанных конструкциях.', 'Names a mouse button in an ON MOUSE handler and related constructs.'],
    NEXT: ['Закрывает цикл FOR или FOR EACH; необязательное имя должно совпадать с переменной цикла.', 'Closes a FOR or FOR EACH loop; the optional name must match the loop variable.'],
    NOT: ['Логическое отрицание; для целых выполняет побитовое дополнение.', 'Logical negation; on integers it performs a bitwise complement.'],
    OFF: ["Отключает обработчик ON KEY / ON MOUSE; SMOOTH OFF выключает сглаживание.","Disables an ON KEY / ON MOUSE handler; SMOOTH OFF disables smoothing."],
    ON: ["Регистрирует или включает обработчик ON KEY / ON MOUSE; в SMOOTH ON включает сглаживание.","Registers or enables an ON KEY / ON MOUSE handler; SMOOTH ON enables smoothing."],
    OPEN: ['Открывает файл для чтения, записи или добавления.', 'Opens a file for reading, writing or appending.'],
    OR: ["Логическое ИЛИ для BOOLEAN или побитовое ИЛИ для целых. Оба операнда вычисляются; для условного вычисления служит ORELSE.","Logical OR for BOOLEAN or bitwise OR for integers. Both operands are evaluated; use ORELSE for conditional evaluation."],
    OUTPUT: ['Режим открытия файла в OPEN: файл создаётся заново для записи.', 'File open mode in OPEN: the file is created anew for writing.'],
    PAINT: ['Заливает замкнутую область цветом в графическом режиме SCREEN.', 'Floods a closed region with a color in SCREEN graphics mode.'],
    PALETTE: ['Изменяет цвета палитры графического режима.', 'Changes palette colors of the graphics mode.'],
    PCOPY: ['Копирует содержимое одной графической страницы в другую.', 'Copies one graphics page onto another.'],
    PRESERVE: ['В REDIM PRESERVE сохраняет существующие элементы при изменении границ.', 'In REDIM PRESERVE keeps existing elements when bounds change.'],
    PRESENT: ['Показывает подготовленный кадр на экране вместе с BEGINFRAME.', 'Shows the prepared frame on screen together with BEGINFRAME.'],
    PRESET: ['Рисует точку цветом фона, то есть стирает её.', 'Draws a pixel in the background color, effectively erasing it.'],
    PRINT: ['Выводит значения на экран; PRINT USING применяет формат.', 'Writes values to the screen; PRINT USING applies a format.'],
    PRIVATE: ['Ограничивает доступность объявления текущим модулем.', 'Restricts a declaration to the current module.'],
    PUBLIC: ['Делает объявление доступным другим модулям проекта.', 'Makes a declaration available to other project modules.'],
    PSET: ['Рисует точку заданным цветом.', 'Draws a pixel with a given color.'],
    PUT: ['Выводит буфер изображения на экран.', 'Draws an image buffer onto the screen.'],
    REDIM: ['Изменяет границы динамического массива; с PRESERVE сохраняет элементы.', 'Changes dynamic array bounds; with PRESERVE it keeps the elements.'],
    RESETCAMERA: ['Возвращает камеру графической сцены в исходное положение.', 'Returns the graphics camera to its initial position.'],
    RETURN: ['Завершает подпрограмму и возвращает значение из функции.', 'Ends a routine and returns a value from a function.'],
    SCREEN: ['Выбирает режим SCREEN: 0 — текстовый, 1..10 — графические. Необязательные номера задают активную и видимую страницы.', 'Selects a SCREEN mode: 0 is text, 1..10 are graphics. Optional page numbers select active and visible pages.'],
    SELECT: ['Открывает выбор по значению SELECT CASE.', 'Opens a value-based SELECT CASE choice.'],
    SELF: ['Ссылка на текущую запись внутри метода TYPE.', 'A reference to the current record inside a TYPE method.'],
    SETMAPTILE: ['Изменяет тайл в ячейке тайловой карты.', 'Changes the tile in a tilemap cell.'],
    SMOOTH: ['Включает или выключает сглаживание графических примитивов.', 'Enables or disables smoothing of graphics primitives.'],
    SLEEP: ['Приостанавливает программу на 0..2147483647 миллисекунд. Без аргумента ждёт клавишу и удаляет её из очереди. SLEEP 0 сразу возвращает управление. Недопустимая длительность: MB3007.', 'Pauses for 0..2147483647 milliseconds. Without an argument, waits for a key and consumes it from the queue. SLEEP 0 returns immediately. Invalid duration: MB3007.'],
    STEP: ["Шаг числового FOR; относительные координаты SCREEN; направление сортировки записей STEP 1 или STEP -1 в ARRAYSORT / ARRAYTOSORTED.","Numeric FOR step; relative SCREEN coordinates; record sorting direction STEP 1 or STEP -1 in ARRAYSORT / ARRAYTOSORTED."],
    STOP: ['Останавливает обработчик события, например ON KEY … STOP.', 'Stops an event handler, for example ON KEY … STOP.'],
    SUB: ['Объявляет процедуру, которая выполняет действия и не возвращает значение.', 'Declares a procedure that performs actions and returns no value.'],
    THEN: ['Отделяет условие IF от действий.', 'Separates the IF condition from its actions.'],
    TO: ['Задаёт верхнюю границу диапазона: в FOR, в границах массива и в срезе.', 'Sets the upper bound of a range: in FOR, in array bounds and in a slice.'],
    TRANSPARENT: ['В COLOR сохраняет фон при выводе символов: рисуются только пиксели текста.', 'In COLOR preserves the background behind text: only glyph pixels are drawn.'],
    TRY: ['Открывает блок обработки ошибок с ветвями CATCH и FINALLY.', 'Opens an error-handling block with CATCH and FINALLY branches.'],
    TYPE: ['Объявляет пользовательский тип-запись с полями и методами.', 'Declares a user-defined record type with fields and methods.'],
    UNTIL: ['Условие цикла DO … LOOP UNTIL: повтор, пока условие ложно.', 'The DO … LOOP UNTIL condition: repeat while the condition is false.'],
    USING: ['Вводит строку формата в PRINT USING.', 'Introduces the format string in PRINT USING.'],
    VIEW: ['Задаёт прямоугольную область вывода в графическом режиме.', 'Sets a rectangular output region in graphics mode.'],
    VIEWPORT: ['Задаёт область вывода для камеры графической сцены.', 'Sets the output viewport for the graphics camera.'],
    WHILE: ['Цикл WHILE … END WHILE или условие цикла DO … LOOP WHILE.', 'A WHILE … END WHILE loop, or the DO … LOOP WHILE condition.'],
    WINDOW: ['Задаёт систему координат графической области.', 'Sets the coordinate system of the graphics region.'],
    XOR: ['Логическое исключающее ИЛИ для BOOLEAN или побитовое исключающее ИЛИ для целых.', 'Logical exclusive OR for BOOLEAN or bitwise exclusive OR for integers.'],
  };

  Object.assign(HINTS, {
  "+": [
    "Сложение чисел, соединение строк; унарный плюс сохраняет число.",
    "Numeric addition, string concatenation; unary plus preserves a number."
  ],
  "-": [
    "Вычитание; унарный минус меняет знак числа.",
    "Subtraction; unary minus negates a number."
  ],
  "*": [
    "Умножение чисел.",
    "Multiplies numbers."
  ],
  "/": [
    "Деление с результатом DOUBLE или DECIMAL по правилам типов.",
    "Division producing DOUBLE or DECIMAL according to operand types."
  ],
  "\\": [
    "Целочисленное деление INTEGER или LONG.",
    "Integer division for INTEGER or LONG."
  ],
  "^": [
    "Возведение в степень, результат DOUBLE. Связывается справа налево.",
    "Exponentiation with a DOUBLE result. Associates from right to left."
  ],
  "&": [
    "Соединяет текстовые представления скалярных значений.",
    "Concatenates text representations of scalar values."
  ],
  "=": [
    "Присваивает значение слева от инструкции; в выражении сравнивает на равенство.",
    "Assigns a value in a statement; compares for equality in an expression."
  ],
  "<>": [
    "Сравнение на неравенство.",
    "Compares for inequality."
  ],
  "<": [
    "Сравнение: строго меньше.",
    "Compares: strictly less than."
  ],
  "<=": [
    "Сравнение: меньше или равно.",
    "Compares: less than or equal."
  ],
  ">": [
    "Сравнение: строго больше.",
    "Compares: strictly greater than."
  ],
  ">=": [
    "Сравнение: больше или равно.",
    "Compares: greater than or equal."
  ],
  "<<": [
    "Побитовый сдвиг целого числа влево.",
    "Shifts integer bits to the left."
  ],
  ">>": [
    "Побитовый сдвиг целого числа вправо.",
    "Shifts integer bits to the right."
  ]
});

  function hint(name, locale) {
    const entry = HINTS[String(name || '').toUpperCase()];
    if (!entry) return null;
    const syntax = SYNTAX[String(name || '').toUpperCase()];
    return entry[locale === 'en' ? 1 : 0] + (syntax ? '\n\n```axiom\n' + syntax + '\n```' : '');
  }

  // Угловые скобки обозначают места для выражений и имён, многоточие — тело блока.
  const SYNTAX = {
  "SLEEP": "SLEEP\nSLEEP <milliseconds>",
  "RANDOMIZE": "RANDOMIZE\nRANDOMIZE <seed>",
  "AND": "<a> AND <b>",
  "ANDALSO": "<a> ANDALSO <b>",
  "OR": "<a> OR <b>",
  "ORELSE": "<a> ORELSE <b>",
  "XOR": "<a> XOR <b>",
  "EQV": "<a> EQV <b>",
  "IMP": "<a> IMP <b>",
  "NOT": "NOT <value>",
  "MOD": "<a> MOD <b>",
  "DIM": "DIM <name> AS <type>\nDIM <name> AS <type> = <value>\nDIM <name>[] AS <type> = [<values>]",
  "CONST": "CONST <name> = <value>",
  "AS": "DIM <name> AS <type>",
  "DICTIONARY": "DIM <name> AS DICTIONARY(<keyType>, <valueType>)",
  "HASHSET": "DIM <name> AS HASHSET(<elementType>)",
  "FOR": "FOR <i> = <start> TO <end> STEP <step>\n    ...\nNEXT\n\nFOR EACH <item> IN <array>[]\n    ...\nNEXT",
  "EACH": "FOR EACH <item> IN <array>[]\n    ...\nNEXT",
  "IN": "FOR EACH <item> IN <array>[]",
  "BYREF": "BYREF <name> AS <type>\nFOR EACH BYREF <item> IN <array>[]",
  "BYVAL": "BYVAL <name> AS <type>\nFOR EACH BYVAL <item> IN <array>[]",
  "NEXT": "NEXT\nNEXT <variable>",
  "STEP": "FOR <i> = <start> TO <end> STEP <step>\nARRAYSORT(<records>[], <field>, STEP <direction>)",
  "TO": "FOR <i> = <start> TO <end>\nDIM <array>[<lower> TO <upper>] AS <type>",
  "IF": "IF <condition> THEN\n    ...\nEND IF\n\nIF <condition> THEN <statement>",
  "THEN": "IF <condition> THEN <statement>",
  "ELSE": "IF <condition> THEN\n    ...\nELSE\n    ...\nEND IF",
  "ELSEIF": "ELSEIF <condition> THEN",
  "SELECT": "SELECT CASE <value>\nCASE <value>\n    ...\nCASE ELSE\n    ...\nEND SELECT",
  "CASE": "CASE <value>\nCASE <lower> TO <upper>\nCASE IS <comparison> <value>\nCASE ELSE",
  "IS": "CASE IS > <value>",
  "WHILE": "WHILE <condition>\n    ...\nEND WHILE",
  "DO": "DO WHILE <condition>\n    ...\nLOOP\n\nDO\n    ...\nLOOP UNTIL <condition>",
  "LOOP": "LOOP\nLOOP WHILE <condition>\nLOOP UNTIL <condition>",
  "UNTIL": "DO UNTIL <condition>\n    ...\nLOOP",
  "CONTINUE": "CONTINUE FOR\nCONTINUE WHILE\nCONTINUE DO",
  "EXIT": "EXIT FOR\nEXIT DO",
  "SUB": "SUB <name>(<parameters>)\n    ...\nEND SUB",
  "FUNCTION": "FUNCTION <name>(<parameters>) AS <type>\n    RETURN <value>\nEND FUNCTION",
  "RETURN": "RETURN\nRETURN <value>",
  "TYPE": "TYPE <name>\n    <field> AS <type>\nEND TYPE",
  "SELF": "SELF.<field>",
  "TRY": "TRY\n    ...\nCATCH <error>\n    ...\nFINALLY\n    ...\nEND TRY",
  "CATCH": "CATCH <error>",
  "FINALLY": "FINALLY\n    ...",
  "END": "END IF\nEND WHILE\nEND SUB\nEND FUNCTION\nEND TYPE\nEND SELECT\nEND TRY",
  "PRINT": "PRINT <values>\nPRINT USING <format>; <values>",
  "USING": "PRINT USING <format>; <values>",
  "REDIM": "REDIM <array>[<bounds>]\nREDIM PRESERVE <array>[<bounds>]",
  "PRESERVE": "REDIM PRESERVE <array>[<bounds>]",
  "ERASE": "ERASE <array>[]",
  "TRUE": "TRUE",
  "FALSE": "FALSE",
  "SMOOTH": "SMOOTH ON\nSMOOTH OFF",
  "INPUT": "INPUT <variable>\nINPUT <prompt>; <variable>",
  "OPEN": "OPEN <path> FOR INPUT AS <file>\nOPEN <path> FOR OUTPUT AS <file>\nOPEN <path> FOR APPEND AS <file>\nOPEN <path> FOR INPUT AS <file> USING <encoding>",
  "CLOSE": "CLOSE <file>",
  "ERROR": "ERROR <message>\nERROR <code>, <message>\nERROR <caughtError>",
  "SCREEN": "SCREEN <mode>\nSCREEN <mode>, , <activePage>, <visiblePage>",
  "CANVAS": "CANVAS <width>, <height>",
  "BEGINFRAME": "BEGINFRAME",
  "PRESENT": "PRESENT",
  "CAMERA": "CAMERA <x>, <y>",
  "VIEWPORT": "VIEWPORT\nVIEWPORT (<x1>, <y1>)-(<x2>, <y2>)",
  "RESETCAMERA": "RESETCAMERA",
  "VIEW": "VIEW\nVIEW (<x1>, <y1>)-(<x2>, <y2>)\nVIEW SCREEN (<x1>, <y1>)-(<x2>, <y2>)",
  "WINDOW": "WINDOW\nWINDOW (<x1>, <y1>)-(<x2>, <y2>)\nWINDOW SCREEN (<x1>, <y1>)-(<x2>, <y2>)",
  "CLS": "CLS\nCLS <color>",
  "COLOR": "COLOR <foreground>\nCOLOR <foreground>, <background>\nCOLOR <foreground>, TRANSPARENT",
  "LOCATE": "LOCATE <row>, <column>",
  "PALETTE": "PALETTE\nPALETTE <index>, <color>\nPALETTE USING <colors>[]",
  "PCOPY": "PCOPY <sourcePage>, <targetPage>",
  "PSET": "PSET (<x>, <y>)\nPSET (<x>, <y>), <color>\nPSET STEP (<dx>, <dy>), <color>",
  "PRESET": "PRESET (<x>, <y>)\nPRESET (<x>, <y>), <color>\nPRESET STEP (<dx>, <dy>), <color>",
  "GET": "GET (<x1>, <y1>)-(<x2>, <y2>), <buffer>[]",
  "PUT": "PUT (<x>, <y>), <buffer>[]\nPUT (<x>, <y>), <buffer>[], <mode>",
  "LINE": "LINE (<x1>, <y1>)-(<x2>, <y2>), <color>\nLINE (<x1>, <y1>)-(<x2>, <y2>), <color>, B\nLINE (<x1>, <y1>)-(<x2>, <y2>), <color>, BF\nLINE INPUT <variable>",
  "CIRCLE": "CIRCLE (<x>, <y>), <radius>\nCIRCLE (<x>, <y>), <radius>, <color>\nCIRCLE (<x>, <y>), <radius>, <color>, <startAngle>, <endAngle>, <aspect>",
  "PAINT": "PAINT (<x>, <y>)\nPAINT (<x>, <y>), <fillColor>\nPAINT (<x>, <y>), <fillColor>, <borderColor>",
  "DRAW": "DRAW <commands>",
  "DRAWLINE": "DRAWLINE <x1>, <y1>, <x2>, <y2>, <color>",
  "DRAWCIRCLE": "DRAWCIRCLE <x>, <y>, <radius>, <color>",
  "FILLCIRCLE": "FILLCIRCLE <x>, <y>, <radius>, <color>",
  "DRAWRECT": "DRAWRECT <x>, <y>, <width>, <height>, <color>",
  "FILLRECT": "FILLRECT <x>, <y>, <width>, <height>, <color>",
  "DRAWTEXT": "DRAWTEXT <text>, <x>, <y>, <color>",
  "DRAWIMAGE": "DRAWIMAGE <image>, <x>, <y>\nDRAWIMAGE <image>, <x>, <y>, <width>, <height>",
  "DRAWSPRITE": "DRAWSPRITE <sprite>, <frame>, <x>, <y>\nDRAWSPRITE <sprite>, <frame>, <x>, <y>, <width>, <height>",
  "DRAWTILE": "DRAWTILE <tileset>, <tile>, <x>, <y>",
  "DRAWTILEMAP": "DRAWTILEMAP <map>, <x>, <y>",
  "DRAWTILELAYER": "DRAWTILELAYER <map>, <layer>, <x>, <y>",
  "SETMAPTILE": "SETMAPTILE <map>, <layer>, <column>, <row>, <tileset>, <tile>[, <flags>]",
  "KEY": "ON KEY(<key>), <handler>\nON KEY(<key>), ON\nON KEY(<key>), STOP\nON KEY(<key>), OFF",
  "MOUSE": "ON MOUSE(<button>), <handler>\nON MOUSE(<button>, \"UP\"), <handler>",
  "ON": "ON KEY(<key>), <handler>\nON MOUSE(<button>), <handler>\nSMOOTH ON",
  "OFF": "ON KEY(<key>), OFF\nON MOUSE(<button>), OFF\nSMOOTH OFF",
  "STOP": "ON KEY(<key>), STOP\nON MOUSE(<button>), STOP",
  "APPEND": "OPEN <path> FOR APPEND AS <file>",
  "OUTPUT": "OPEN <path> FOR OUTPUT AS <file>",
  "PUBLIC": "PUBLIC SUB <name>(<parameters>)\nPUBLIC FUNCTION <name>(<parameters>) AS <type>",
  "PRIVATE": "PRIVATE SUB <name>(<parameters>)\nPRIVATE FUNCTION <name>(<parameters>) AS <type>",
  "IMPORT": "IMPORT <module>",
  "TRANSPARENT": "COLOR <foreground>, TRANSPARENT"
};

  const TYPE_HINTS = {
    BOOLEAN: ['Логический тип: TRUE или FALSE.', 'Boolean type: TRUE or FALSE.'],
    INTEGER: ['Знаковое 32-разрядное целое: −2147483648..2147483647.', 'Signed 32-bit integer: -2147483648..2147483647.'],
    LONG: ['Знаковое 64-разрядное целое: −9223372036854775808..9223372036854775807.', 'Signed 64-bit integer: -9223372036854775808..9223372036854775807.'],
    DOUBLE: ['Число двойной точности с плавающей точкой; дроби могут храниться приближённо.', 'Double-precision floating-point number; fractions may be approximate.'],
    DECIMAL: ['Десятичный числовой тип для вычислений с десятичными дробями.', 'Decimal numeric type for decimal fraction calculations.'],
    STRING: ['Строковый тип. Функции обработки текста различают единицы UTF16, CODEPOINT и GRAPHEME в поддержанных формах.', 'String type. Supported text function forms distinguish UTF16, CODEPOINT and GRAPHEME units.'],
    DATE: ['Календарная дата без времени суток и часового пояса.', 'Calendar date without time of day or time zone.'],
    TIME: ['Время суток с точностью до тика, без даты и часового пояса.', 'Time of day with tick precision, without a date or time zone.'],
    DATETIME: ['Календарные дата и время с точностью до тика, без часового пояса. Для извлечения частей служат DATEVALUE и TIMEVALUE.', 'Calendar date and time with tick precision and no time zone. Extract parts with DATEVALUE and TIMEVALUE.'],
    UTCDATETIME: ['Однозначный момент на шкале UTC. Текстовая запись содержит T и завершающий Z.', 'Unambiguous instant on the UTC timeline. Text uses T and a trailing Z.'],
    FILE: ['Ресурс текстового файла. OPEN открывает файл, CLOSE закрывает; состояние проверяет ISOPEN.', 'Text file resource. OPEN opens it, CLOSE closes it; ISOPEN checks its state.'],
    IMAGE: ['Ресурс изображения, загружаемый LOADIMAGE. Размеры IMAGEWIDTH и IMAGEHEIGHT измеряются в пикселях.', 'Image resource loaded by LOADIMAGE. IMAGEWIDTH and IMAGEHEIGHT measure pixels.'],
    SPRITE: ['Спрайтовый лист из LOADSPRITE, разделённый на кадры. DRAWSPRITE выводит выбранный кадр.', 'Sprite sheet loaded by LOADSPRITE and divided into frames. DRAWSPRITE draws a selected frame.'],
    TILESET: ['Набор тайлов из LOADTILESET. Размеры тайла задаются в пикселях.', 'Tile grid loaded by LOADTILESET. Tile dimensions are in pixels.'],
    TILEMAP: ['Карта Tiled из LOADTILEMAP: слои, клетки, объекты и свойства.', 'Tiled map loaded by LOADTILEMAP: layers, cells, objects and properties.'],
    DICTIONARY: HINTS.DICTIONARY,
    HASHSET: HINTS.HASHSET,
  };
  function typeHint(name, locale) {
    const entry = TYPE_HINTS[String(name || '').toUpperCase()];
    return entry ? entry[locale === 'en' ? 1 : 0] : null;
  }

  // Члены словаря объясняются отдельно от ключевых слов: Add, Exists, Remove,
  // Keys и TryGetValue вызываются со скобками, Count — свойство без скобок.
  const MEMBER_HINTS = {
    ADD: ['Метод словаря: добавляет новую запись. Повторный ключ сообщает ошибку MB3016.', 'Dictionary method: adds a new entry. A repeated key reports error MB3016.'],
    EXISTS: ['Метод словаря: проверяет наличие ключа и возвращает BOOLEAN.', 'Dictionary method: checks whether a key exists and returns BOOLEAN.'],
    REMOVE: ['Метод словаря: удаляет запись и возвращает BOOLEAN. Отсутствующий ключ даёт FALSE без ошибки.', 'Dictionary method: removes an entry and returns BOOLEAN. A missing key gives FALSE without an error.'],
    KEYS: ['Метод словаря: возвращает самостоятельный одномерный массив ключей в порядке добавления.', 'Dictionary method: returns an independent one-dimensional array of keys in insertion order.'],
    TRYGETVALUE: ['Метод словаря: ищет ключ без исключения. Результат BOOLEAN, второй аргумент получает значение или значение по умолчанию.', 'Dictionary method: looks up a key without an exception. The result is BOOLEAN and the second argument receives the value or its default.'],
    COUNT: ['Свойство словаря: число записей. Записывается без скобок.', 'Dictionary property: the number of entries. Written without parentheses.'],
  };

  function memberHint(name, locale) {
    const entry = MEMBER_HINTS[String(name || '').toUpperCase()];
    if (!entry) return null;
    return locale === 'en' ? entry[1] : entry[0];
  }

  // Члены множества описываются отдельно: у словаря Add не возвращает значения,
  // а у множества Add и Remove возвращают BOOLEAN, поэтому общий текст нельзя
  // выдавать за описание обоих типов.
  const SET_MEMBER_HINTS = {
    ADD: ['Метод множества: добавляет элемент и возвращает BOOLEAN. TRUE означает новое значение, FALSE — элемент уже был; повторное добавление не ошибка и не меняет порядок.', 'Set method: adds an element and returns BOOLEAN. TRUE means a new value, FALSE means the element was already present; a repeated addition is not an error and keeps the order.'],
    CONTAINS: ['Метод множества: проверяет наличие элемента и возвращает BOOLEAN, не изменяя состав.', 'Set method: checks whether an element is present and returns BOOLEAN without changing the contents.'],
    REMOVE: ['Метод множества: удаляет элемент и возвращает BOOLEAN. Отсутствующий элемент даёт FALSE без ошибки.', 'Set method: removes an element and returns BOOLEAN. A missing element gives FALSE without an error.'],
    TOARRAY: ['Метод множества: возвращает самостоятельный одномерный массив элементов в порядке первого добавления. Границы 1..Count, для пустого множества 1 TO 0.', 'Set method: returns an independent one-dimensional array of the elements in first-addition order. Bounds are 1..Count and 1 TO 0 for an empty set.'],
    UNIONWITH: ['Процедура множества: добавляет отсутствующие элементы другого множества того же типа и не возвращает значения. Порядок источника сохраняется.', 'Set procedure: adds the missing elements of another set of the same type and returns no value. The source order is preserved.'],
    INTERSECTWITH: ['Процедура множества: оставляет только общие элементы в прежнем порядке получателя и не возвращает значения.', 'Set procedure: keeps only the common elements in the previous receiver order and returns no value.'],
    COUNT: ['Свойство множества: число элементов. Записывается без скобок.', 'Set property: the number of elements. Written without parentheses.'],
  };

  function setMemberHint(name, locale) {
    const entry = SET_MEMBER_HINTS[String(name || '').toUpperCase()];
    if (!entry) return null;
    return locale === 'en' ? entry[1] : entry[0];
  }

  return { hints: HINTS, hint, typeHints: TYPE_HINTS, typeHint, memberHints: MEMBER_HINTS, memberHint,
    setMemberHints: SET_MEMBER_HINTS, setMemberHint };
});
