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
    AND: ['Логическое И: результат истинен, когда истинны оба операнда. Применяется к BOOLEAN и к целым для побитового И.', 'Logical AND: true when both operands are true. Also performs bitwise AND on integers.'],
    APPEND: ['Режим открытия файла в OPEN: записи добавляются в конец существующего файла.', 'File open mode in OPEN: new records are appended to the end of an existing file.'],
    AS: ['Вводит тип: в объявлении задаёт тип переменной, в заголовке подпрограммы — тип параметра или результата.', 'Introduces a type: the variable type in a declaration, or a parameter or result type in a routine header.'],
    BEGINFRAME: ['Открывает кадр графического вывода вместе с PRESENT. Между ними рисуется содержимое одного кадра.', 'Opens a graphics frame together with PRESENT; the drawing between them belongs to one frame.'],
    BYREF: ['Передаёт аргумент по ссылке: подпрограмма работает с той же переменной, и изменения видны вызывающему коду. В FOR EACH задаёт адресацию текущего элемента массива.', 'Passes an argument by reference so the routine works on the caller variable itself. In FOR EACH it selects the current array element.'],
    BYVAL: ['Передаёт аргумент по значению: подпрограмма получает копию. В FOR EACH задаёт копию элемента и действует по умолчанию.', 'Passes an argument by value so the routine gets a copy. In FOR EACH it copies the element and is the default.'],
    CAMERA: ['Задаёт положение камеры графической сцены: смещение и масштаб отображения.', 'Sets the graphics camera position: offset and display scale.'],
    CANVAS: ['Создаёт холст заданного размера для рисования в современном графическом режиме.', 'Creates a canvas of the given size for drawing in the modern graphics mode.'],
    CASE: ['Ветвь выбора в SELECT CASE: сравнивает значение с образцом или диапазоном.', 'A SELECT CASE branch that compares the value with a pattern or range.'],
    CATCH: ['Ветвь TRY, которая получает ошибку времени выполнения и обрабатывает её.', 'A TRY branch that receives and handles a runtime error.'],
    CIRCLE: ['Рисует окружность или эллипс в графическом режиме SCREEN.', 'Draws a circle or ellipse in SCREEN graphics mode.'],
    CLOSE: ['Закрывает файл, открытый через OPEN, и сохраняет буфер на диск.', 'Closes a file opened with OPEN and flushes its buffer to disk.'],
    CLS: ['Очищает экран или графическую поверхность, при необходимости заданным цветом.', 'Clears the screen or graphics surface, optionally with a given color.'],
    COLOR: ['Задаёт цвет текста и фона для последующего вывода.', 'Sets the text foreground and background color for later output.'],
    CONST: ['Объявляет константу: значение задаётся один раз и не может быть изменено.', 'Declares a constant: the value is set once and cannot be changed.'],
    CONTINUE: ['Переходит к следующему проходу цикла, пропуская остаток тела: CONTINUE FOR, CONTINUE WHILE или CONTINUE DO.', 'Jumps to the next loop iteration, skipping the rest of the body: CONTINUE FOR, CONTINUE WHILE or CONTINUE DO.'],
    DICTIONARY: ['Тип типизированного словаря: DICTIONARY(тип ключа, тип значения) задаёт ключ и значение всех записей. Ключом служит STRING, INTEGER или LONG. Слово зарезервировано: как имя не используется.', 'Typed dictionary type: DICTIONARY(key type, value type) sets the key and value of every entry. The key is STRING, INTEGER or LONG. The word is reserved and cannot be used as a name.'],
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
    EACH: ['Ключевое слово, вводящее обход массива в заголовке FOR EACH. Зарезервировано: как имя не используется.', 'Keyword that introduces array iteration in a FOR EACH header. Reserved: it cannot be used as a name.'],
    ELSE: ['Ветвь IF, которая выполняется, когда условие ложно.', 'The IF branch that runs when the condition is false.'],
    ELSEIF: ['Дополнительное условие в IF, проверяемое после ложного предыдущего.', 'An additional IF condition checked after a previous false one.'],
    END: ['Закрывает составную конструкцию: END IF, END WHILE, END SUB, END FUNCTION, END TYPE, END SELECT, END TRY.', 'Closes a compound construct: END IF, END WHILE, END SUB, END FUNCTION, END TYPE, END SELECT, END TRY.'],
    ERROR: ['Оператор, который создаёт ошибку времени выполнения с заданным кодом и сообщением.', 'A statement that raises a runtime error with a given code and message.'],
    EQV: ['Логическая эквивалентность: результат истинен, когда операнды совпадают.', 'Logical equivalence: true when both operands have the same value.'],
    ERASE: ['Освобождает память динамического массива, сохраняя его имя и тип.', 'Releases a dynamic array storage while keeping its name and type.'],
    EXIT: ['Досрочно завершает цикл: EXIT FOR или EXIT DO.', 'Leaves a loop early: EXIT FOR or EXIT DO.'],
    FALSE: ['Логическое значение «ложь».', 'The logical value false.'],
    TRUE: ['Логическое значение «истина».', 'The logical value true.'],
    FILLCIRCLE: ['Рисует заполненный круг.', 'Draws a filled circle.'],
    FILLRECT: ['Рисует заполненный прямоугольник.', 'Draws a filled rectangle.'],
    FINALLY: ['Ветвь TRY, которая выполняется всегда: и при ошибке, и без неё.', 'The TRY branch that always runs, with or without an error.'],
    FOR: ['Открывает цикл: числовой FOR … TO … STEP или обход массива FOR EACH … IN. Закрывается словом NEXT.', 'Opens a loop: numeric FOR … TO … STEP, or array iteration FOR EACH … IN. Closed by NEXT.'],
    FUNCTION: ['Объявляет функцию, возвращающую значение через RETURN или присваиванием имени.', 'Declares a function that returns a value through RETURN or by assigning its name.'],
    GET: ['Копирует область экрана в буфер изображения.', 'Copies a screen region into an image buffer.'],
    IF: ['Открывает условную конструкцию; в одной строке допускает короткую форму.', 'Opens a conditional construct and allows a single-line short form.'],
    IMP: ['Логическая импликация: результат ложен только когда первый операнд истинен, а второй ложен.', 'Logical implication: false only when the first operand is true and the second is false.'],
    IMPORT: ['Подключает другой модуль проекта и его открытые имена.', 'Imports another project module and its public names.'],
    IN: ['Ключевое слово заголовка FOR EACH: отделяет переменную цикла от массива-источника. Зарезервировано: как имя не используется.', 'Keyword of the FOR EACH header that separates the loop variable from the source array. Reserved: it cannot be used as a name.'],
    INPUT: ['Читает значения, введённые пользователем, в переменные.', 'Reads user-entered values into variables.'],
    IS: ['Сравнение с образцом в SELECT CASE, например IS > 10.', 'A pattern comparison in SELECT CASE, for example IS > 10.'],
    KEY: ['Указывает клавишу в обработчике ON KEY и связанных конструкциях.', 'Names a key in an ON KEY handler and related constructs.'],
    LINE: ['Рисует отрезок в графическом режиме SCREEN.', 'Draws a line segment in SCREEN graphics mode.'],
    LOCATE: ['Ставит курсор текстового вывода в заданную строку и столбец.', 'Moves the text output cursor to a given row and column.'],
    LOOP: ['Закрывает цикл DO … LOOP, при необходимости с условием WHILE или UNTIL.', 'Closes a DO … LOOP cycle, optionally with a WHILE or UNTIL condition.'],
    MOD: ['Остаток от целочисленного деления.', 'The remainder of integer division.'],
    MOUSE: ['Указывает кнопку мыши в обработчике ON MOUSE и связанных конструкциях.', 'Names a mouse button in an ON MOUSE handler and related constructs.'],
    NEXT: ['Закрывает цикл FOR или FOR EACH; необязательное имя должно совпадать с переменной цикла.', 'Closes a FOR or FOR EACH loop; the optional name must match the loop variable.'],
    NOT: ['Логическое отрицание; для целых выполняет побитовое дополнение.', 'Logical negation; on integers it performs a bitwise complement.'],
    OFF: ['Выключает обработчик события, например ON KEY … OFF.', 'Turns an event handler off, for example ON KEY … OFF.'],
    ON: ['Открывает оператор обработчика события: ON KEY или ON MOUSE.', 'Starts an event handler statement: ON KEY or ON MOUSE.'],
    OPEN: ['Открывает файл для чтения, записи или добавления.', 'Opens a file for reading, writing or appending.'],
    OR: ['Логическое ИЛИ: результат истинен, когда истинен хотя бы один операнд.', 'Logical OR: true when at least one operand is true.'],
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
    SCREEN: ['Включает графический режим с заданным разрешением.', 'Switches to a graphics mode with the given resolution.'],
    SELECT: ['Открывает выбор по значению SELECT CASE.', 'Opens a value-based SELECT CASE choice.'],
    SELF: ['Ссылка на текущую запись внутри метода TYPE.', 'A reference to the current record inside a TYPE method.'],
    SETMAPTILE: ['Изменяет тайл в ячейке тайловой карты.', 'Changes the tile in a tilemap cell.'],
    SMOOTH: ['Включает или выключает сглаживание графических примитивов.', 'Enables or disables smoothing of graphics primitives.'],
    SLEEP: ['Приостанавливает программу на заданное число миллисекунд.', 'Pauses the program for the given number of milliseconds.'],
    STEP: ['Шаг числового цикла FOR или направление сортировки по полю в ARRAYSORT и ARRAYTOSORTED.', 'The numeric FOR step, or the sort direction for field sorting in ARRAYSORT and ARRAYTOSORTED.'],
    STOP: ['Останавливает обработчик события, например ON KEY … STOP.', 'Stops an event handler, for example ON KEY … STOP.'],
    SUB: ['Объявляет процедуру, которая выполняет действия и не возвращает значение.', 'Declares a procedure that performs actions and returns no value.'],
    THEN: ['Отделяет условие IF от действий.', 'Separates the IF condition from its actions.'],
    TO: ['Задаёт верхнюю границу диапазона: в FOR, в границах массива и в срезе.', 'Sets the upper bound of a range: in FOR, in array bounds and in a slice.'],
    TRANSPARENT: ['Признак прозрачного цвета при выводе изображения.', 'Marks a transparent color when drawing an image.'],
    TRY: ['Открывает блок обработки ошибок с ветвями CATCH и FINALLY.', 'Opens an error-handling block with CATCH and FINALLY branches.'],
    TYPE: ['Объявляет пользовательский тип-запись с полями и методами.', 'Declares a user-defined record type with fields and methods.'],
    UNTIL: ['Условие цикла DO … LOOP UNTIL: повтор, пока условие ложно.', 'The DO … LOOP UNTIL condition: repeat while the condition is false.'],
    USING: ['Вводит строку формата в PRINT USING.', 'Introduces the format string in PRINT USING.'],
    VIEW: ['Задаёт прямоугольную область вывода в графическом режиме.', 'Sets a rectangular output region in graphics mode.'],
    VIEWPORT: ['Задаёт область вывода для камеры графической сцены.', 'Sets the output viewport for the graphics camera.'],
    WHILE: ['Цикл WHILE … END WHILE или условие цикла DO … LOOP WHILE.', 'A WHILE … END WHILE loop, or the DO … LOOP WHILE condition.'],
    WINDOW: ['Задаёт систему координат графической области.', 'Sets the coordinate system of the graphics region.'],
    XOR: ['Исключающее ИЛИ: результат истинен, когда операнды различаются.', 'Exclusive OR: true when the operands differ.'],
  };

  function hint(name, locale) {
    const entry = HINTS[String(name || '').toUpperCase()];
    if (!entry) return null;
    return locale === 'en' ? entry[1] : entry[0];
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

  return { hints: HINTS, hint, memberHints: MEMBER_HINTS, memberHint };
});
