/* Каталог параметров Monaco 0.56.0. Типы и английские пояснения сверены с monaco.d.ts. */
(function (root) {
  const entries = [
  {
    "group": "appearance",
    "key": "lineNumbers",
    "ru": "Номера строк",
    "en": "Line numbers",
    "helpRu": "Показывает обычные, относительные номера или номера через интервал.",
    "helpEn": "Control the rendering of line numbers. If it is a function, it will be invoked when rendering a line number and the return value will be rendered. Otherwise, if it is a truthy, line numbers will be rendered normally (equivalent of using an identity function). Otherwise, line numbers will not be rendered. Defaults to `on`.",
    "kind": "select",
    "choices": [
      "on",
      "off",
      "relative",
      "interval"
    ]
  },
  {
    "group": "appearance",
    "key": "lineNumbersMinChars",
    "ru": "Ширина области номеров",
    "en": "Line numbers min chars",
    "helpRu": "Минимальное число знаков, зарезервированных слева для номера строки.",
    "helpEn": "Control the width of line numbers, by reserving horizontal space for rendering at least an amount of digits. Defaults to 5.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "selectOnLineNumbers",
    "ru": "Выделять строку нажатием на номер",
    "en": "Select on line numbers",
    "helpRu": "Щелчок по номеру выделяет всю соответствующую строку.",
    "helpEn": "Should the corresponding line be selected when clicking on the line number? Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "glyphMargin",
    "ru": "Поле значков слева",
    "en": "Glyph margin",
    "helpRu": "Оставляет место для значков точек останова и диагностик.",
    "helpEn": "Enable the rendering of the glyph margin. Defaults to true in vscode and to false in monaco-editor.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "lineDecorationsWidth",
    "ru": "Ширина поля обозначений",
    "en": "Line decorations width",
    "helpRu": "Ширина дополнительного поля слева от текста, в пикселях.",
    "helpEn": "The width reserved for line decorations (in px). Line decorations are placed between line numbers and the editor content. You can pass in a string in the format floating point followed by \"ch\". e.g. 1.3ch. Defaults to 10.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "renderWhitespace",
    "ru": "Показывать пробелы и табуляцию",
    "en": "Render whitespace",
    "helpRu": "Выберите, где отображать невидимые символы: везде, в выделении или на границах.",
    "helpEn": "Enable rendering of whitespace. Defaults to 'selection'.",
    "kind": "select",
    "choices": [
      "none",
      "boundary",
      "selection",
      "trailing",
      "all"
    ]
  },
  {
    "group": "appearance",
    "key": "renderControlCharacters",
    "ru": "Показывать управляющие символы",
    "en": "Render control characters",
    "helpRu": "Делает непечатные управляющие символы видимыми в тексте.",
    "helpEn": "Enable rendering of control characters. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "renderFinalNewline",
    "ru": "Номер последней пустой строки",
    "en": "Render final newline",
    "helpRu": "Определяет отображение завершающей строки после последнего перевода строки.",
    "helpEn": "Render last line number when the file ends with a newline. Defaults to 'on' for Windows and macOS and 'dimmed' for Linux.",
    "kind": "select",
    "choices": [
      "on",
      "off",
      "dimmed"
    ]
  },
  {
    "group": "appearance",
    "key": "renderLineHighlight",
    "ru": "Подсветка текущей строки",
    "en": "Render line highlight",
    "helpRu": "Выделяет фон текущей строки, поле номера или обе области.",
    "helpEn": "Enable rendering of current line highlight. Defaults to all.",
    "kind": "select",
    "choices": [
      "none",
      "gutter",
      "line",
      "all"
    ]
  },
  {
    "group": "appearance",
    "key": "renderLineHighlightOnlyWhenFocus",
    "ru": "Подсвечивать строку только при фокусе",
    "en": "Render line highlight only when focus",
    "helpRu": "Убирает выделение текущей строки, когда редактор не активен.",
    "helpEn": "Control if the current line highlight should be rendered only the editor is focused. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "renderValidationDecorations",
    "ru": "Обозначения ошибок и предупреждений",
    "en": "Render validation decorations",
    "helpRu": "Определяет, когда Monaco рисует подчёркивания диагностик.",
    "helpEn": "Should the editor render validation decorations. Defaults to editable.",
    "kind": "select",
    "choices": [
      "editable",
      "on",
      "off"
    ]
  },
  {
    "group": "appearance",
    "key": "showUnused",
    "ru": "Приглушать неиспользуемые имена",
    "en": "Show unused",
    "helpRu": "Приглушает сущности, помеченные диагностикой как неиспользуемые.",
    "helpEn": "Controls fading out of unused variables.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "showDeprecated",
    "ru": "Зачёркивать устаревшие сущности",
    "en": "Show deprecated",
    "helpRu": "Отображает зачёркивание для сущностей, отмеченных как устаревшие.",
    "helpEn": "Controls strikethrough deprecated variables.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "rulers",
    "ru": "Вертикальные линии колонок",
    "en": "Rulers",
    "helpRu": "Список номеров колонок в JSON, например [80, 120]. Пустой список убирает линии.",
    "helpEn": "Render vertical lines at the specified columns. Defaults to empty array.",
    "kind": "json",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "overviewRulerLanes",
    "ru": "Число дорожек обзорной полосы",
    "en": "Overview ruler lanes",
    "helpRu": "Сколько вертикальных дорожек использовать для отметок рядом с полосой прокрутки.",
    "helpEn": "The number of vertical lanes the overview ruler should render. Defaults to 3.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "overviewRulerBorder",
    "ru": "Граница обзорной полосы",
    "en": "Overview ruler border",
    "helpRu": "Рисует границу области отметок ошибок, выделений и других обозначений.",
    "helpEn": "Controls if a border should be drawn around the overview ruler. Defaults to `true`.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "hideCursorInOverviewRuler",
    "ru": "Скрывать курсор в обзорной полосе",
    "en": "Hide cursor in overview ruler",
    "helpRu": "Убирает отметку положения курсора справа от кода.",
    "helpEn": "Should the cursor be hidden in the overview ruler. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "padding.top",
    "ru": "Поле сверху",
    "en": "Padding top",
    "helpRu": "Свободное место перед первой строкой, в пикселях.",
    "helpEn": "Spacing between top edge of editor and first line.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "padding.bottom",
    "ru": "Поле снизу",
    "en": "Padding bottom",
    "helpRu": "Свободное место под последней строкой, в пикселях.",
    "helpEn": "Spacing between bottom edge of editor and last line.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "colorDecorators",
    "ru": "Показывать образцы цветов",
    "en": "Color decorators",
    "helpRu": "Показывает цветовые квадраты, если язык предоставляет сведения о цветах.",
    "helpEn": "Enable inline color decorators and color picker rendering.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "colorDecoratorsActivatedOn",
    "ru": "Открытие выбора цвета",
    "en": "Color decorators activated on",
    "helpRu": "Определяет, когда появляется выбор цвета: при нажатии или наведении.",
    "helpEn": "Controls what is the condition to spawn a color picker from a color dectorator",
    "kind": "select",
    "choices": [
      "clickAndHover",
      "click",
      "hover"
    ]
  },
  {
    "group": "appearance",
    "key": "colorDecoratorsLimit",
    "ru": "Предел числа образцов цвета",
    "en": "Color decorators limit",
    "helpRu": "Максимальное число цветовых образцов в документе.",
    "helpEn": "Controls the max number of color decorators that can be rendered in an editor at once.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "appearance",
    "key": "defaultColorDecorators",
    "ru": "Стандартное распознавание цветов",
    "en": "Default color decorators",
    "helpRu": "Включает встроенные образцы цветов для поддерживаемых Monaco форматов.",
    "helpEn": "Controls whether to use default color decorations or not using the default document color provider",
    "kind": "select",
    "choices": [
      "auto",
      "always",
      "never"
    ]
  },
  {
    "group": "appearance",
    "key": "semanticHighlighting.enabled",
    "ru": "Семантическая подсветка",
    "en": "Semantic highlighting enabled",
    "helpRu": "Раскрашивает имена по смыслу, если для языка зарегистрирован соответствующий провайдер.",
    "helpEn": "Controls whether the semanticHighlighting is shown for the languages that support it. true: semanticHighlighting is enabled for all themes false: semanticHighlighting is disabled for all themes 'configuredByTheme': semanticHighlighting is controlled by the current color theme's semanticHighlighting setting. Defaults to 'byTheme'.",
    "kind": "select",
    "choices": [
      true,
      false,
      "configuredByTheme"
    ]
  },
  {
    "group": "appearance",
    "key": "autoDetectHighContrast",
    "ru": "Учитывать высокий контраст системы",
    "en": "Auto detect high contrast",
    "helpRu": "Позволяет Monaco автоматически выбирать высококонтрастную тему при таком режиме Windows.",
    "helpEn": "If enabled, will automatically change to high contrast theme if the OS is using a high contrast theme. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "font",
    "key": "fontFamily",
    "ru": "Шрифт редактора",
    "en": "Font family",
    "helpRu": "Имя шрифта или список запасных шрифтов через запятую. Указанный шрифт должен быть установлен.",
    "helpEn": "The font family",
    "kind": "text",
    "choices": []
  },
  {
    "group": "font",
    "key": "fontSize",
    "ru": "Размер шрифта",
    "en": "Font size",
    "helpRu": "Размер текста редактора, в пикселях. Предпросмотр показывает результат до применения.",
    "helpEn": "The font size",
    "kind": "number",
    "choices": []
  },
  {
    "group": "font",
    "key": "fontWeight",
    "ru": "Толщина шрифта",
    "en": "Font weight",
    "helpRu": "Толщина: normal, bold или число от 100 до 900.",
    "helpEn": "The font weight",
    "kind": "text",
    "choices": []
  },
  {
    "group": "font",
    "key": "fontLigatures",
    "ru": "Лигатуры",
    "en": "Font ligatures",
    "helpRu": "Объединяет сочетания символов в специальные начертания шрифта. Можно включить, выключить или задать строку возможностей OpenType.",
    "helpEn": "Enable or disable ligatures, or select Custom to enter OpenType feature settings.",
    "kind": "json",
    "choices": []
  },
  {
    "group": "font",
    "key": "fontVariations",
    "ru": "Вариативный шрифт",
    "en": "Font variations",
    "helpRu": "Включает вариативные начертания шрифта либо позволяет задать строку настроек его осей.",
    "helpEn": "Enable variable font settings, or select Custom to enter font variation axes.",
    "kind": "json",
    "choices": []
  },
  {
    "group": "font",
    "key": "lineHeight",
    "ru": "Высота строки",
    "en": "Line height",
    "helpRu": "0 выбирает высоту автоматически; значения меньше 8 задают множитель размера шрифта, остальные значения задаются в пикселях.",
    "helpEn": "0 selects the height automatically. Values below 8 multiply the font size; values of 8 or more are pixels.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "font",
    "key": "letterSpacing",
    "ru": "Межбуквенный интервал",
    "en": "Letter spacing",
    "helpRu": "Дополнительное расстояние между символами, в пикселях.",
    "helpEn": "The letter spacing",
    "kind": "number",
    "choices": []
  },
  {
    "group": "typing",
    "key": "tabSize",
    "ru": "Ширина табуляции",
    "en": "Tab size",
    "helpRu": "Число пробелов, соответствующее одному знаку табуляции.",
    "helpEn": "The number of spaces a tab is equal to. This setting is overridden based on the file contents when `detectIndentation` is on. Defaults to 4.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "typing",
    "key": "insertSpaces",
    "ru": "Вставлять пробелы вместо табуляции",
    "en": "Insert spaces",
    "helpRu": "Клавиша Tab вставляет пробелы, а не символ табуляции.",
    "helpEn": "Insert spaces when pressing `Tab`. This setting is overridden based on the file contents when `detectIndentation` is on. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "detectIndentation",
    "ru": "Определять отступы по файлу",
    "en": "Detect indentation",
    "helpRu": "При открытии файла определяет ширину и вид отступов по его содержимому.",
    "helpEn": "Controls whether `tabSize` and `insertSpaces` will be automatically detected when a file is opened based on the file contents. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "trimAutoWhitespace",
    "ru": "Удалять лишние автоматические отступы",
    "en": "Trim auto whitespace",
    "helpRu": "Убирает неиспользованные пробелы, вставленные редактором автоматически.",
    "helpEn": "Remove trailing auto inserted whitespace. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "autoIndent",
    "ru": "Автоматические отступы",
    "en": "Auto indent",
    "helpRu": "Выбирает степень учёта предыдущей строки, скобок и правил языка.",
    "helpEn": "Controls whether the editor should automatically adjust the indentation when users type, paste, move or indent lines. Defaults to advanced.",
    "kind": "select",
    "choices": [
      "none",
      "keep",
      "brackets",
      "advanced",
      "full"
    ]
  },
  {
    "group": "typing",
    "key": "autoIndentOnPaste",
    "ru": "Выравнивать отступы при вставке",
    "en": "Auto indent on paste",
    "helpRu": "Подстраивает отступы вставленного фрагмента под место вставки.",
    "helpEn": "Boolean which controls whether to autoindent on paste",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "autoIndentOnPasteWithinString",
    "ru": "Выравнивать вставку внутри строки",
    "en": "Auto indent on paste within string",
    "helpRu": "Разрешает автоматическое выравнивание отступов также внутри строкового литерала.",
    "helpEn": "Boolean which controls whether to autoindent on paste within a string when autoIndentOnPaste is enabled.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "autoClosingBrackets",
    "ru": "Закрывать скобки автоматически",
    "en": "Auto closing brackets",
    "helpRu": "Выбирает, когда редактор вставляет вторую скобку пары.",
    "helpEn": "Options for auto closing brackets. Defaults to language defined behavior.",
    "kind": "select",
    "choices": [
      "always",
      "languageDefined",
      "beforeWhitespace",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "autoClosingQuotes",
    "ru": "Закрывать кавычки автоматически",
    "en": "Auto closing quotes",
    "helpRu": "Выбирает, когда редактор добавляет парную кавычку.",
    "helpEn": "Options for auto closing quotes. Defaults to language defined behavior.",
    "kind": "select",
    "choices": [
      "always",
      "languageDefined",
      "beforeWhitespace",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "autoClosingComments",
    "ru": "Закрывать комментарии автоматически",
    "en": "Auto closing comments",
    "helpRu": "Использует правила парных комментариев языка, если такие правила существуют.",
    "helpEn": "Options for auto closing comments. Defaults to language defined behavior.",
    "kind": "select",
    "choices": [
      "always",
      "languageDefined",
      "beforeWhitespace",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "autoClosingDelete",
    "ru": "Удалять автоматически вставленную пару",
    "en": "Auto closing delete",
    "helpRu": "При удалении открывающего знака определяет судьбу парного закрывающего знака.",
    "helpEn": "Options for pressing backspace near quotes or bracket pairs.",
    "kind": "select",
    "choices": [
      "always",
      "auto",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "autoClosingOvertype",
    "ru": "Проходить через закрывающий знак",
    "en": "Auto closing overtype",
    "helpRu": "Повторный ввод закрывающего знака может переместить курсор через уже вставленный знак.",
    "helpEn": "Options for typing over closing quotes or brackets.",
    "kind": "select",
    "choices": [
      "always",
      "auto",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "autoSurround",
    "ru": "Обрамлять выделение",
    "en": "Auto surround",
    "helpRu": "Ввод скобки или кавычки может обрамлять выделенный фрагмент парой знаков.",
    "helpEn": "Options for auto surrounding. Defaults to always allowing auto surrounding.",
    "kind": "select",
    "choices": [
      "languageDefined",
      "quotes",
      "brackets",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "stickyTabStops",
    "ru": "Перемещаться по ступеням отступа",
    "en": "Sticky tab stops",
    "helpRu": "Перемещение и удаление в начальных пробелах учитывает границы табуляции.",
    "helpEn": "Emulate selection behaviour of tab characters when using spaces for indentation. This means selection will stick to tab stops.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "useTabStops",
    "ru": "Учитывать границы табуляции",
    "en": "Use tab stops",
    "helpRu": "Определяет, следует ли применять позиции табуляции при редактировании отступов.",
    "helpEn": "Inserting and deleting whitespace follows tab stops.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "trimWhitespaceOnDelete",
    "ru": "Удалять пробелы при удалении",
    "en": "Trim whitespace on delete",
    "helpRu": "Удаление учитывает последовательности пробелов рядом с курсором.",
    "helpEn": "Controls whether the editor should automatically remove indentation whitespace when joining lines with Delete. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "formatOnType",
    "ru": "Форматировать при вводе",
    "en": "Format on type",
    "helpRu": "Вызывает провайдер форматирования при вводе предусмотренных им символов.",
    "helpEn": "Enable format on type. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "formatOnPaste",
    "ru": "Форматировать при вставке",
    "en": "Format on paste",
    "helpRu": "Вызывает провайдер форматирования для вставленного фрагмента.",
    "helpEn": "Enable format on paste. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "overtypeOnPaste",
    "ru": "Замещать текст при вставке",
    "en": "Overtype on paste",
    "helpRu": "В режиме замещения вставка может заменять имеющиеся символы.",
    "helpEn": "Controls whether paste in overtype mode should overwrite or insert.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "comments.insertSpace",
    "ru": "Пробел после знака комментария",
    "en": "Comments insert space",
    "helpRu": "Команда комментирования добавляет пробел между знаком комментария и текстом.",
    "helpEn": "Insert a space after the line comment token and inside the block comments tokens. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "comments.ignoreEmptyLines",
    "ru": "Пропускать пустые строки при комментировании",
    "en": "Comments ignore empty lines",
    "helpRu": "Команда добавления комментария оставляет пустые строки пустыми.",
    "helpEn": "Ignore empty lines when inserting line comments. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "emptySelectionClipboard",
    "ru": "Копировать строку без выделения",
    "en": "Empty selection clipboard",
    "helpRu": "Если ничего не выделено, копирование захватывает текущую строку.",
    "helpEn": "Copying without a selection copies the current line.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "copyWithSyntaxHighlighting",
    "ru": "Копировать с подсветкой",
    "en": "Copy with syntax highlighting",
    "helpRu": "Помещает в буфер обмена также оформленный вариант скопированного кода.",
    "helpEn": "Syntax highlighting is copied.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "dragAndDrop",
    "ru": "Перетаскивать выделенный текст",
    "en": "Drag and drop",
    "helpRu": "Разрешает перенос выделенного фрагмента мышью внутри редактора.",
    "helpEn": "Controls if the editor should allow to move selections via drag and drop. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "dropIntoEditor.enabled",
    "ru": "Вставка перетаскиванием",
    "en": "Drop into editor enabled",
    "helpRu": "Разрешает обработку содержимого, перетаскиваемого в редактор.",
    "helpEn": "Enable dropping into editor. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "dropIntoEditor.showDropSelector",
    "ru": "Выбор способа после перетаскивания",
    "en": "Drop into editor show drop selector",
    "helpRu": "Определяет, показывать ли выбор способа вставки после переноса содержимого.",
    "helpEn": "Controls if a widget is shown after a drop. Defaults to 'afterDrop'.",
    "kind": "select",
    "choices": [
      "afterDrop",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "pasteAs.enabled",
    "ru": "Выбор способа вставки",
    "en": "Paste as enabled",
    "helpRu": "Разрешает дополнительные способы вставки, предоставляемые провайдерами Monaco.",
    "helpEn": "Enable paste as functionality in editors. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "pasteAs.showPasteSelector",
    "ru": "Показывать способы после вставки",
    "en": "Paste as show paste selector",
    "helpRu": "Определяет, появляется ли выбор дополнительного способа вставки.",
    "helpEn": "Controls if a widget is shown after a drop. Defaults to 'afterPaste'.",
    "kind": "select",
    "choices": [
      "afterPaste",
      "never"
    ]
  },
  {
    "group": "typing",
    "key": "linkedEditing",
    "ru": "Связанное редактирование",
    "en": "Linked editing",
    "helpRu": "Согласованно меняет связанные диапазоны, если язык предоставляет их расположение.",
    "helpEn": "Enable linked editing. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "typing",
    "key": "renameOnType",
    "ru": "Переименование при вводе",
    "en": "Rename on type",
    "helpRu": "Включает согласованное изменение связанных имён при поддержке языка.",
    "helpEn": "deprecated, use linkedEditing instead",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "quickSuggestions.other",
    "ru": "Автоматические подсказки в коде",
    "en": "Quick suggestions other",
    "helpRu": "Определяет показ списка дополнений во время набора обычного кода.",
    "helpEn": "Quick suggestions other.",
    "kind": "select",
    "choices": [
      true,
      false,
      "on",
      "inline",
      "off",
      "offWhenInlineCompletions"
    ]
  },
  {
    "group": "suggestions",
    "key": "quickSuggestions.comments",
    "ru": "Автоматические подсказки в комментариях",
    "en": "Quick suggestions comments",
    "helpRu": "Определяет показ дополнений внутри комментариев.",
    "helpEn": "Quick suggestions comments.",
    "kind": "select",
    "choices": [
      true,
      false,
      "on",
      "inline",
      "off",
      "offWhenInlineCompletions"
    ]
  },
  {
    "group": "suggestions",
    "key": "quickSuggestions.strings",
    "ru": "Автоматические подсказки в строках",
    "en": "Quick suggestions strings",
    "helpRu": "Определяет показ дополнений внутри строковых литералов.",
    "helpEn": "Quick suggestions strings.",
    "kind": "select",
    "choices": [
      true,
      false,
      "on",
      "inline",
      "off",
      "offWhenInlineCompletions"
    ]
  },
  {
    "group": "suggestions",
    "key": "quickSuggestionsDelay",
    "ru": "Задержка автоматических подсказок",
    "en": "Quick suggestions delay",
    "helpRu": "Пауза перед показом списка дополнений, в миллисекундах.",
    "helpEn": "Quick suggestions show delay (in ms) Defaults to 10 (ms)",
    "kind": "number",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggestOnTriggerCharacters",
    "ru": "Подсказки после специальных символов",
    "en": "Suggest on trigger characters",
    "helpRu": "Запрашивает дополнения после символов, объявленных провайдером языка.",
    "helpEn": "Enable the suggestion box to pop-up on trigger characters. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "acceptSuggestionOnEnter",
    "ru": "Принимать дополнение клавишей Enter",
    "en": "Accept suggestion on enter",
    "helpRu": "Выбирает, принимает ли Enter подсказку или вставляет перевод строки.",
    "helpEn": "Accept suggestions on ENTER. Defaults to 'on'.",
    "kind": "select",
    "choices": [
      "on",
      "smart",
      "off"
    ]
  },
  {
    "group": "suggestions",
    "key": "acceptSuggestionOnCommitCharacter",
    "ru": "Принимать дополнение завершающим символом",
    "en": "Accept suggestion on commit character",
    "helpRu": "Разрешает принятие подсказки при вводе указанного провайдером завершающего символа.",
    "helpEn": "Accept suggestions on provider defined characters. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "tabCompletion",
    "ru": "Дополнение клавишей Tab",
    "en": "Tab completion",
    "helpRu": "Определяет работу Tab для дополнений и заготовок кода.",
    "helpEn": "Enable tab completion.",
    "kind": "select",
    "choices": [
      "on",
      "off",
      "onlySnippets"
    ]
  },
  {
    "group": "suggestions",
    "key": "snippetSuggestions",
    "ru": "Место заготовок в списке",
    "en": "Snippet suggestions",
    "helpRu": "Заготовки кода можно поместить в начало, конец, общий порядок или скрыть.",
    "helpEn": "Enable snippet suggestions. Default to 'true'.",
    "kind": "select",
    "choices": [
      "top",
      "bottom",
      "inline",
      "none"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggestSelection",
    "ru": "Запоминание выбранных дополнений",
    "en": "Suggest selection",
    "helpRu": "Определяет, какой вариант предварительно выбран при следующем появлении списка.",
    "helpEn": "The history mode for suggestions.",
    "kind": "select",
    "choices": [
      "first",
      "recentlyUsed",
      "recentlyUsedByPrefix"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggestFontSize",
    "ru": "Размер шрифта списка дополнений",
    "en": "Suggest font size",
    "helpRu": "Размер в пикселях; 0 использует размер шрифта редактора.",
    "helpEn": "The font size for the suggest widget. Defaults to the editor font size.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggestLineHeight",
    "ru": "Высота строки списка дополнений",
    "en": "Suggest line height",
    "helpRu": "Высота в пикселях; 0 использует высоту, выбранную редактором.",
    "helpEn": "The line height for the suggest widget. Defaults to the editor line height.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "matchOnWordStartOnly",
    "ru": "Совпадение только с начала слова",
    "en": "Match on word start only",
    "helpRu": "Фильтрует подсказки по началу слова вместо поиска совпадений внутри него.",
    "helpEn": "Controls whether suggestions allow matches in the middle of the word instead of only at the beginning",
    "kind": "boolean",
    "choices": [],
    "available": false,
    "unavailableRu": "Устаревшее поле API не используется работающей Monaco. Используйте поле фильтрации списка с начала слова в группе списка дополнений.",
    "unavailableEn": "This obsolete API field is not used by the running Monaco. Use the completion-list word-start filter instead."
  },
  {
    "group": "suggestions",
    "key": "suggest.insertMode",
    "ru": "Вставка или замещение дополнением",
    "en": "Suggest insert mode",
    "helpRu": "Определяет, вставлять ли дополнение или заменять имеющуюся часть слова.",
    "helpEn": "Overwrite word ends on accept. Default to false.",
    "kind": "select",
    "choices": [
      "insert",
      "replace"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggest.filterGraceful",
    "ru": "Мягкая фильтрация дополнений",
    "en": "Suggest filter graceful",
    "helpRu": "Сохраняет подходящие варианты при небольших расхождениях введённого текста.",
    "helpEn": "Enable graceful matching. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.snippetsPreventQuickSuggestions",
    "ru": "Не показывать подсказки внутри заготовки",
    "en": "Suggest snippets prevent quick suggestions",
    "helpRu": "Подавляет автоматическое дополнение во время заполнения заготовки кода.",
    "helpEn": "Prevent quick suggestions when a snippet is active. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.localityBonus",
    "ru": "Предпочитать ближайшие имена",
    "en": "Suggest locality bonus",
    "helpRu": "Повышает приоритет имён, встречающихся рядом с текущим местом в документе.",
    "helpEn": "Favors words that appear close to the cursor.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.shareSuggestSelections",
    "ru": "Общее запоминание выбора",
    "en": "Suggest share suggest selections",
    "helpRu": "Разрешает совместное использование истории выбора дополнений между редакторами.",
    "helpEn": "Enable using global storage for remembering suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.selectionMode",
    "ru": "Предварительный выбор дополнения",
    "en": "Suggest selection mode",
    "helpRu": "Определяет, когда список заранее выделяет вариант для принятия.",
    "helpEn": "Select suggestions when triggered via quick suggest or trigger characters",
    "kind": "select",
    "choices": [
      "always",
      "never",
      "whenTriggerCharacter",
      "whenQuickSuggestion"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggest.showIcons",
    "ru": "Значки в списке дополнений",
    "en": "Suggest show icons",
    "helpRu": "Показывает значки типов сущностей рядом с названиями.",
    "helpEn": "Enable or disable icons in suggestions. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showStatusBar",
    "ru": "Нижняя строка списка дополнений",
    "en": "Suggest show status bar",
    "helpRu": "Показывает дополнительные команды внизу списка подсказок.",
    "helpEn": "Enable or disable the suggest status bar.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.preview",
    "ru": "Предпросмотр дополнения",
    "en": "Suggest preview",
    "helpRu": "Показывает ещё не принятую часть выбранного дополнения в тексте редактора.",
    "helpEn": "Enable or disable the rendering of the suggestion preview.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.previewMode",
    "ru": "Способ предпросмотра дополнения",
    "en": "Suggest preview mode",
    "helpRu": "Определяет способ сопоставления вводимого слова с предварительным дополнением.",
    "helpEn": "Configures the mode of the preview.",
    "kind": "select",
    "choices": [
      "prefix",
      "subword",
      "subwordSmart"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggest.showInlineDetails",
    "ru": "Краткое описание рядом с дополнением",
    "en": "Suggest show inline details",
    "helpRu": "Показывает сведения о сущности в строке списка дополнений.",
    "helpEn": "Show details inline with the label. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.matchOnWordStartOnly",
    "ru": "Фильтрация списка с начала слова",
    "en": "Suggest match on word start only",
    "helpRu": "Ограничивает сопоставление вариантов в списке началом слова.",
    "helpEn": "Controls whether suggestions allow matches in the middle of the word instead of only at the beginning",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "hover.enabled",
    "ru": "Подсказки при наведении",
    "en": "Hover enabled",
    "helpRu": "Включает подсказки мышью или только при удержании клавиши-модификатора.",
    "helpEn": "Enable the hover. Defaults to 'on'.",
    "kind": "select",
    "choices": [
      "on",
      "off",
      "onKeyboardModifier"
    ]
  },
  {
    "group": "suggestions",
    "key": "hover.delay",
    "ru": "Задержка подсказки при наведении",
    "en": "Hover delay",
    "helpRu": "Время до появления всплывающего описания, в миллисекундах.",
    "helpEn": "Delay for showing the hover. Defaults to 300.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "hover.sticky",
    "ru": "Удерживать подсказку под мышью",
    "en": "Hover sticky",
    "helpRu": "Подсказка остаётся видимой, когда мышь переходит на её содержимое.",
    "helpEn": "Is the hover sticky such that it can be clicked and its contents selected? Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "hover.hidingDelay",
    "ru": "Задержка скрытия подсказки",
    "en": "Hover hiding delay",
    "helpRu": "Пауза перед исчезновением подсказки после ухода мыши, в миллисекундах.",
    "helpEn": "Controls how long the hover is visible after you hovered out of it. Require sticky setting to be true.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "hover.above",
    "ru": "Предпочитать подсказку над строкой",
    "en": "Hover above",
    "helpRu": "Размещает описание сверху, если для него достаточно места.",
    "helpEn": "Should the hover be shown above the line if possible? Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "hover.showLongLineWarning",
    "ru": "Предупреждение о длинной строке",
    "en": "Hover show long line warning",
    "helpRu": "Сообщает, если длинная строка ограничивает обработку или отображение.",
    "helpEn": "Should long line warning hovers be shown (tokenization skipped, rendering paused)? Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "parameterHints.enabled",
    "ru": "Подсказки параметров",
    "en": "Parameter hints enabled",
    "helpRu": "Показывает сигнатуру вызова и сведения о текущем аргументе.",
    "helpEn": "Enable parameter hints. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "parameterHints.cycle",
    "ru": "Циклический перебор сигнатур",
    "en": "Parameter hints cycle",
    "helpRu": "После последней сигнатуры переключение возвращается к первой.",
    "helpEn": "Enable cycling of parameter hints. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "wordBasedSuggestions",
    "ru": "Дополнения из текста документов",
    "en": "Word based suggestions",
    "helpRu": "Выбирает, из каких открытых документов брать слова для дополнения.",
    "helpEn": "Controls whether completions should be computed based on words in the document. Defaults to true.",
    "kind": "select",
    "choices": [
      "off",
      "currentDocument",
      "matchingDocuments",
      "allDocuments",
      "offWithInlineSuggestions"
    ]
  },
  {
    "group": "suggestions",
    "key": "wordBasedSuggestionsOnlySameLanguage",
    "ru": "Слова только из документов того же языка",
    "en": "Word based suggestions only same language",
    "helpRu": "Ограничивает источники слов документами с тем же языком.",
    "helpEn": "Controls whether word based completions should be included from opened documents of the same language or any language.",
    "kind": "boolean",
    "choices": [],
    "available": false,
    "unavailableRu": "Устаревшее поле API не используется работающей Monaco. Выбирайте документы того же языка в поле «Дополнения из текста документов».",
    "unavailableEn": "This obsolete API field is not used by the running Monaco. Choose matching documents in Word based suggestions instead."
  },
  {
    "group": "scroll",
    "key": "stickyScroll.enabled",
    "ru": "Закреплять заголовки блоков при прокрутке",
    "en": "Sticky scroll enabled",
    "helpRu": "Показывает заголовки текущих блоков над прокручиваемым кодом; выключите, чтобы убрать закреплённую область.",
    "helpEn": "Enable the sticky scroll",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "stickyScroll.maxLineCount",
    "ru": "Максимум закреплённых строк",
    "en": "Sticky scroll max line count",
    "helpRu": "Ограничивает высоту области закреплённых заголовков числом строк.",
    "helpEn": "Maximum number of sticky lines to show",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "stickyScroll.defaultModel",
    "ru": "Источник закреплённых заголовков",
    "en": "Sticky scroll default model",
    "helpRu": "Выбирает структуру языка, области сворачивания или уровни отступов.",
    "helpEn": "Model to choose for sticky scroll by default",
    "kind": "select",
    "choices": [
      "outlineModel",
      "foldingProviderModel",
      "indentationModel"
    ]
  },
  {
    "group": "scroll",
    "key": "stickyScroll.scrollWithEditor",
    "ru": "Горизонтальная прокрутка закреплённых строк",
    "en": "Sticky scroll scroll with editor",
    "helpRu": "Закреплённые заголовки смещаются вместе с кодом при горизонтальной прокрутке.",
    "helpEn": "Define whether to scroll sticky scroll with editor horizontal scrollbae",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.enabled",
    "ru": "Показывать миникарту",
    "en": "Minimap enabled",
    "helpRu": "Показывает уменьшенный обзор всего документа рядом с кодом.",
    "helpEn": "Enable the rendering of the minimap. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.autohide",
    "ru": "Автоматически скрывать миникарту",
    "en": "Minimap autohide",
    "helpRu": "Определяет, когда миникарта скрывается до взаимодействия с редактором.",
    "helpEn": "Control the rendering of minimap.",
    "kind": "select",
    "choices": [
      "none",
      "mouseover",
      "scroll"
    ]
  },
  {
    "group": "scroll",
    "key": "minimap.side",
    "ru": "Сторона миникарты",
    "en": "Minimap side",
    "helpRu": "Размещает обзор документа слева или справа от текста.",
    "helpEn": "Control the side of the minimap in editor. Defaults to 'right'.",
    "kind": "select",
    "choices": [
      "right",
      "left"
    ]
  },
  {
    "group": "scroll",
    "key": "minimap.size",
    "ru": "Высота миникарты",
    "en": "Minimap size",
    "helpRu": "Выбирает пропорциональное отображение, заполнение высоты или подгонку документа.",
    "helpEn": "Control the minimap rendering mode. Defaults to 'actual'.",
    "kind": "select",
    "choices": [
      "proportional",
      "fill",
      "fit"
    ]
  },
  {
    "group": "scroll",
    "key": "minimap.showSlider",
    "ru": "Ползунок миникарты",
    "en": "Minimap show slider",
    "helpRu": "Определяет, когда виден прямоугольник текущей области документа.",
    "helpEn": "Control the rendering of the minimap slider. Defaults to 'mouseover'.",
    "kind": "select",
    "choices": [
      "always",
      "mouseover"
    ]
  },
  {
    "group": "scroll",
    "key": "minimap.renderCharacters",
    "ru": "Символы на миникарте",
    "en": "Minimap render characters",
    "helpRu": "Показывает уменьшенные символы вместо цветовых блоков.",
    "helpEn": "Render the actual text on a line (as opposed to color blocks). Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.maxColumn",
    "ru": "Предел колонок миникарты",
    "en": "Minimap max column",
    "helpRu": "Ограничивает число отображаемых колонок текста.",
    "helpEn": "Limit the width of the minimap to render at most a certain number of columns. Defaults to 120.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.scale",
    "ru": "Масштаб миникарты",
    "en": "Minimap scale",
    "helpRu": "Масштаб уменьшенного изображения кода.",
    "helpEn": "Relative size of the font in the minimap. Defaults to 1.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.showRegionSectionHeaders",
    "ru": "Заголовки областей на миникарте",
    "en": "Minimap show region section headers",
    "helpRu": "Показывает заголовки разделов, предоставленные сворачиванием областей.",
    "helpEn": "Whether to show named regions as section headers. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.showMarkSectionHeaders",
    "ru": "Заголовки отметок на миникарте",
    "en": "Minimap show mark section headers",
    "helpRu": "Показывает заголовки, найденные по регулярному выражению отметок.",
    "helpEn": "Whether to show MARK: comments as section headers. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.sectionHeaderFontSize",
    "ru": "Размер заголовков миникарты",
    "en": "Minimap section header font size",
    "helpRu": "Размер шрифта названий разделов, в пикселях.",
    "helpEn": "Font size of section headers. Defaults to 9.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.sectionHeaderLetterSpacing",
    "ru": "Интервал букв заголовков миникарты",
    "en": "Minimap section header letter spacing",
    "helpRu": "Дополнительное расстояние между буквами названий разделов, в пикселях.",
    "helpEn": "Spacing between the section header characters (in CSS px). Defaults to 1.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "minimap.markSectionHeaderRegex",
    "ru": "Шаблон заголовков миникарты",
    "en": "Minimap mark section header regex",
    "helpRu": "Регулярное выражение для отметок разделов; применяется при включённых заголовках отметок.",
    "helpEn": "When specified, is used to create a custom section header parser regexp. Must contain a match group named 'label' (written as (?<label>.+)) that encapsulates the section header. Optionally can include another match group named 'separator'. To match multi-line headers like: // ========== // My Section // ========== Use a pattern like: ^={3,}\\n^\\/\\/ (?<label>[^\\n] ?)\\n^={3,}$",
    "kind": "text",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "smoothScrolling",
    "ru": "Плавная прокрутка",
    "en": "Smooth scrolling",
    "helpRu": "Плавно перемещает видимую область вместо мгновенных скачков.",
    "helpEn": "Enable that the editor animates scrolling to a position. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollBeyondLastLine",
    "ru": "Прокрутка ниже последней строки",
    "en": "Scroll beyond last line",
    "helpRu": "Позволяет поднять конец документа выше нижнего края редактора.",
    "helpEn": "Enable that scrolling can go one screen size after the last line. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollBeyondLastColumn",
    "ru": "Запас справа от текста",
    "en": "Scroll beyond last column",
    "helpRu": "Число дополнительных колонок для горизонтальной прокрутки.",
    "helpEn": "Enable that scrolling can go beyond the last column by a number of columns. Defaults to 5.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "revealHorizontalRightPadding",
    "ru": "Запас справа при переходе к курсору",
    "en": "Reveal horizontal right padding",
    "helpRu": "Дополнительное место справа от курсора при автоматическом показе позиции, в пикселях.",
    "helpEn": "When revealing the cursor, a virtual padding (px) is added to the cursor, turning it into a rectangle. This virtual padding ensures that the cursor gets revealed before hitting the edge of the viewport. Defaults to 30 (px).",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollOnMiddleClick",
    "ru": "Прокрутка средней кнопкой мыши",
    "en": "Scroll on middle click",
    "helpRu": "Разрешает прокрутку документа нажатием средней кнопки.",
    "helpEn": "Scroll editor on middle click",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollPredominantAxis",
    "ru": "Прокрутка по основной оси",
    "en": "Scroll predominant axis",
    "helpRu": "При диагональном жесте предпочитает направление с наибольшим смещением.",
    "helpEn": "Enable that the editor scrolls only the predominant axis. Prevents horizontal drift when scrolling vertically on a trackpad. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "inertialScroll",
    "ru": "Инерционная прокрутка",
    "en": "Inertial scroll",
    "helpRu": "Добавляет инерцию прокрутке; эффект зависит от устройства ввода и системы.",
    "helpEn": "Make scrolling inertial - mostly useful with touchpad on linux.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "mouseWheelScrollSensitivity",
    "ru": "Чувствительность колеса",
    "en": "Mouse wheel scroll sensitivity",
    "helpRu": "Множитель скорости обычной прокрутки колёсиком мыши.",
    "helpEn": "A multiplier to be used on the `deltaX` and `deltaY` of mouse wheel scroll events. Defaults to 1.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "fastScrollSensitivity",
    "ru": "Ускоренная прокрутка",
    "en": "Fast scroll sensitivity",
    "helpRu": "Множитель скорости ускоренной прокрутки.",
    "helpEn": "FastScrolling mulitplier speed when pressing `Alt` Defaults to 5.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "mouseWheelZoom",
    "ru": "Изменять шрифт с помощью Ctrl и колеса",
    "en": "Mouse wheel zoom",
    "helpRu": "Удерживание Ctrl при прокрутке колеса меняет размер текста редактора.",
    "helpEn": "Zoom the font in the editor when using the mouse wheel in combination with holding Ctrl. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.vertical",
    "ru": "Вертикальная полоса прокрутки",
    "en": "Scrollbar vertical",
    "helpRu": "Показывает полосу постоянно, автоматически или скрывает её.",
    "helpEn": "Render vertical scrollbar. Defaults to 'auto'.",
    "kind": "select",
    "choices": [
      "auto",
      "visible",
      "hidden"
    ]
  },
  {
    "group": "scroll",
    "key": "scrollbar.horizontal",
    "ru": "Горизонтальная полоса прокрутки",
    "en": "Scrollbar horizontal",
    "helpRu": "Показывает полосу постоянно, автоматически или скрывает её.",
    "helpEn": "Render horizontal scrollbar. Defaults to 'auto'.",
    "kind": "select",
    "choices": [
      "auto",
      "visible",
      "hidden"
    ]
  },
  {
    "group": "scroll",
    "key": "scrollbar.verticalScrollbarSize",
    "ru": "Ширина вертикальной полосы",
    "en": "Scrollbar vertical scrollbar size",
    "helpRu": "Ширина полосы прокрутки, в пикселях.",
    "helpEn": "Width in pixels for the vertical scrollbar. Defaults to 14 (px).",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.horizontalScrollbarSize",
    "ru": "Высота горизонтальной полосы",
    "en": "Scrollbar horizontal scrollbar size",
    "helpRu": "Высота полосы прокрутки, в пикселях.",
    "helpEn": "Height in pixels for the horizontal scrollbar. Defaults to 12 (px).",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.handleMouseWheel",
    "ru": "Обрабатывать колесо мыши",
    "en": "Scrollbar handle mouse wheel",
    "helpRu": "Редактор использует колесо для прокрутки своего содержимого.",
    "helpEn": "Listen to mouse wheel events and react to them by scrolling. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.alwaysConsumeMouseWheel",
    "ru": "Всегда удерживать колесо в редакторе",
    "en": "Scrollbar always consume mouse wheel",
    "helpRu": "Не передаёт прокрутку родительской области даже на границе документа.",
    "helpEn": "Always consume mouse wheel events (always call preventDefault() and stopPropagation() on the browser events). Defaults to true. NOTE : This option cannot be updated using `updateOptions()`",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.scrollByPage",
    "ru": "Прокручивать страницами при нажатии на полосу",
    "en": "Scrollbar scroll by page",
    "helpRu": "Нажатие вне ползунка перемещает на страницу вместо перехода к позиции.",
    "helpEn": "Scroll gutter clicks move by page vs jump to position. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "scrollbar.ignoreHorizontalScrollbarInContentHeight",
    "ru": "Не учитывать горизонтальную полосу в высоте текста",
    "en": "Scrollbar ignore horizontal scrollbar in content height",
    "helpRu": "Не добавляет высоту полосы к общей высоте содержимого.",
    "helpEn": "When set, the horizontal scrollbar will not increase content height. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "wordWrap",
    "ru": "Перенос длинных строк",
    "en": "Word wrap",
    "helpRu": "Выбирает перенос по ширине окна, заданной колонке или меньшему из этих значений.",
    "helpEn": "Control the wrapping of the editor. When `wordWrap` = \"off\", the lines will never wrap. When `wordWrap` = \"on\", the lines will wrap at the viewport width. When `wordWrap` = \"wordWrapColumn\", the lines will wrap at `wordWrapColumn`. When `wordWrap` = \"bounded\", the lines will wrap at min(viewport width, wordWrapColumn). Defaults to \"off\".",
    "kind": "select",
    "choices": [
      "off",
      "on",
      "wordWrapColumn",
      "bounded"
    ]
  },
  {
    "group": "scroll",
    "key": "wordWrapColumn",
    "ru": "Колонка переноса",
    "en": "Word wrap column",
    "helpRu": "Номер колонки для режима переноса по заданной ширине.",
    "helpEn": "Control the wrapping of the editor. When `wordWrap` = \"off\", the lines will never wrap. When `wordWrap` = \"on\", the lines will wrap at the viewport width. When `wordWrap` = \"wordWrapColumn\", the lines will wrap at `wordWrapColumn`. When `wordWrap` = \"bounded\", the lines will wrap at min(viewport width, wordWrapColumn). Defaults to 80.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "wrappingIndent",
    "ru": "Отступ перенесённых частей",
    "en": "Wrapping indent",
    "helpRu": "Выбирает отступ визуального продолжения длинной строки.",
    "helpEn": "Control indentation of wrapped lines. Can be: 'none', 'same', 'indent' or 'deepIndent'. Defaults to 'same' in vscode and to 'none' in monaco-editor.",
    "kind": "select",
    "choices": [
      "none",
      "same",
      "indent",
      "deepIndent"
    ]
  },
  {
    "group": "scroll",
    "key": "wrappingStrategy",
    "ru": "Способ расчёта переноса",
    "en": "Wrapping strategy",
    "helpRu": "Быстрый расчёт или точный расчёт по реальной ширине символов.",
    "helpEn": "Controls the wrapping strategy to use. Defaults to 'simple'.",
    "kind": "select",
    "choices": [
      "simple",
      "advanced"
    ]
  },
  {
    "group": "scroll",
    "key": "wrapOnEscapedLineFeeds",
    "ru": "Перенос на записи обратная косая черта и n",
    "en": "Wrap on escaped line feeds",
    "helpRu": "Разрешает визуальный перенос на текстовой записи экранированного перевода строки.",
    "helpEn": "Create a softwrap on every quoted \"\\n\" literal. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "wordBreak",
    "ru": "Разбиение слов при переносе",
    "en": "Word break",
    "helpRu": "Определяет перенос слов, в том числе восточноазиатских символов.",
    "helpEn": "Sets whether line breaks appear wherever the text would otherwise overflow its content box. When wordBreak = 'normal', Use the default line break rule. When wordBreak = 'keepAll', Word breaks should not be used for Chinese/Japanese/Korean (CJK) text. Non-CJK text behavior is the same as for normal.",
    "kind": "select",
    "choices": [
      "normal",
      "keepAll"
    ]
  },
  {
    "group": "scroll",
    "key": "wordWrapBreakBeforeCharacters",
    "ru": "Перенос перед символами",
    "en": "Word wrap break before characters",
    "helpRu": "Список символов, перед которыми допустим визуальный перенос строки.",
    "helpEn": "Configure word wrapping characters. A break will be introduced before these characters.",
    "kind": "text",
    "choices": []
  },
  {
    "group": "scroll",
    "key": "wordWrapBreakAfterCharacters",
    "ru": "Перенос после символов",
    "en": "Word wrap break after characters",
    "helpRu": "Список символов, после которых допустим визуальный перенос строки.",
    "helpEn": "Configure word wrapping characters. A break will be introduced after these characters.",
    "kind": "text",
    "choices": []
  },
  {
    "group": "selection",
    "key": "cursorStyle",
    "ru": "Вид курсора",
    "en": "Cursor style",
    "helpRu": "Вертикальная линия, блок или подчёркивание; доступны тонкие и контурные варианты.",
    "helpEn": "Control the cursor style in insert mode. Defaults to 'line'.",
    "kind": "select",
    "choices": [
      "line",
      "block",
      "underline",
      "line-thin",
      "block-outline",
      "underline-thin"
    ]
  },
  {
    "group": "selection",
    "key": "cursorBlinking",
    "ru": "Мигание курсора",
    "en": "Cursor blinking",
    "helpRu": "Выбирает способ мигания либо постоянное отображение курсора.",
    "helpEn": "Control the cursor animation style, possible values are 'blink', 'smooth', 'phase', 'expand' and 'solid'. Defaults to 'blink'.",
    "kind": "select",
    "choices": [
      "blink",
      "smooth",
      "phase",
      "expand",
      "solid"
    ]
  },
  {
    "group": "selection",
    "key": "cursorSmoothCaretAnimation",
    "ru": "Плавное перемещение курсора",
    "en": "Cursor smooth caret animation",
    "helpRu": "Включает анимацию перемещения курсора между позициями.",
    "helpEn": "Enable smooth caret animation. Defaults to 'off'.",
    "kind": "select",
    "choices": [
      "off",
      "explicit",
      "on"
    ]
  },
  {
    "group": "selection",
    "key": "cursorWidth",
    "ru": "Толщина курсора",
    "en": "Cursor width",
    "helpRu": "Ширина вертикального курсора в физических пикселях экрана; масштабирование Windows учитывается при отображении.",
    "helpEn": "Width of the vertical cursor in physical screen pixels. Windows display scaling is taken into account.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "cursorHeight",
    "ru": "Высота курсора",
    "en": "Cursor height",
    "helpRu": "Высота вертикального курсора, в пикселях; 0 оставляет автоматическое значение.",
    "helpEn": "Control the height of the cursor when cursorStyle is set to 'line'",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "overtypeCursorStyle",
    "ru": "Курсор режима замещения",
    "en": "Overtype cursor style",
    "helpRu": "Вид курсора при замене существующих символов вводимыми.",
    "helpEn": "Control the cursor style in overtype mode. Defaults to 'block'.",
    "kind": "select",
    "choices": [
      "line",
      "block",
      "underline",
      "line-thin",
      "block-outline",
      "underline-thin"
    ]
  },
  {
    "group": "selection",
    "key": "cursorSurroundingLines",
    "ru": "Запас строк вокруг курсора",
    "en": "Cursor surrounding lines",
    "helpRu": "Минимальное число видимых строк над и под курсором при перемещении.",
    "helpEn": "Controls the minimal number of visible leading and trailing lines surrounding the cursor. Defaults to 0.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "cursorSurroundingLinesStyle",
    "ru": "Когда сохранять запас строк",
    "en": "Cursor surrounding lines style",
    "helpRu": "Определяет, сохраняется ли запас также при установке курсора мышью.",
    "helpEn": "Controls when `cursorSurroundingLines` should be enforced Defaults to `default`, `cursorSurroundingLines` is not enforced when cursor position is changed by mouse.",
    "kind": "select",
    "choices": [
      "default",
      "all"
    ]
  },
  {
    "group": "selection",
    "key": "roundedSelection",
    "ru": "Скруглённое выделение",
    "en": "Rounded selection",
    "helpRu": "Скругляет углы окрашенной области выделенного текста.",
    "helpEn": "Render the editor selection with rounded borders. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "columnSelection",
    "ru": "Прямоугольное выделение",
    "en": "Column selection",
    "helpRu": "Мышь и клавиши выделяют колонки вместо последовательного диапазона текста.",
    "helpEn": "Enable that the selection with the mouse and keys is doing column selection. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "multiCursorModifier",
    "ru": "Клавиша нескольких курсоров",
    "en": "Multi cursor modifier",
    "helpRu": "Выбирает клавишу для добавления курсоров щелчками мыши.",
    "helpEn": "The modifier to be used to add multiple cursors with the mouse. Defaults to 'alt'",
    "kind": "select",
    "choices": [
      "ctrlCmd",
      "alt"
    ]
  },
  {
    "group": "selection",
    "key": "multiCursorMergeOverlapping",
    "ru": "Объединять пересекающиеся курсоры",
    "en": "Multi cursor merge overlapping",
    "helpRu": "Совпавшие или пересекающиеся выделения превращаются в одно.",
    "helpEn": "Merge overlapping selections. Defaults to true",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "multiCursorPaste",
    "ru": "Вставка при нескольких курсорах",
    "en": "Multi cursor paste",
    "helpRu": "Разносит строки вставки по курсорам или вставляет полный текст в каждую позицию.",
    "helpEn": "Configure the behaviour when pasting a text with the line count equal to the cursor count. Defaults to 'spread'.",
    "kind": "select",
    "choices": [
      "spread",
      "full"
    ]
  },
  {
    "group": "selection",
    "key": "multiCursorLimit",
    "ru": "Предел числа курсоров",
    "en": "Multi cursor limit",
    "helpRu": "Максимальное число одновременно активных текстовых курсоров.",
    "helpEn": "Controls the max number of text cursors that can be in an active editor at once.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "selectionHighlight",
    "ru": "Подсвечивать совпадения выделенного текста",
    "en": "Selection highlight",
    "helpRu": "Отмечает другие вхождения выделенного фрагмента в документе.",
    "helpEn": "Enable selection highlight. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "selectionHighlightMultiline",
    "ru": "Совпадения многострочного выделения",
    "en": "Selection highlight multiline",
    "helpRu": "Разрешает подсветку совпадений выделения, содержащего несколько строк.",
    "helpEn": "Enable selection highlight for multiline selections. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "selectionHighlightMaxLength",
    "ru": "Предел длины для подсветки совпадений",
    "en": "Selection highlight max length",
    "helpRu": "Максимальная длина выделения в символах; 0 снимает ограничение.",
    "helpEn": "Maximum length (in characters) for selection highlights. Set to 0 to have an unlimited length.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "occurrencesHighlight",
    "ru": "Подсвечивать использования имени",
    "en": "Occurrences highlight",
    "helpRu": "Показывает использования сущности под курсором при поддержке языка.",
    "helpEn": "Enable semantic occurrences highlight. Defaults to 'singleFile'. 'off' disables occurrence highlighting 'singleFile' triggers occurrence highlighting in the current document 'multiFile' triggers occurrence highlighting across valid open documents",
    "kind": "select",
    "choices": [
      "off",
      "singleFile",
      "multiFile"
    ]
  },
  {
    "group": "selection",
    "key": "occurrencesHighlightDelay",
    "ru": "Задержка подсветки использований",
    "en": "Occurrences highlight delay",
    "helpRu": "Пауза перед подсветкой использований имени, в миллисекундах.",
    "helpEn": "Controls delay for occurrences highlighting Defaults to 250. Minimum value is 0 Maximum value is 2000",
    "kind": "number",
    "choices": []
  },
  {
    "group": "selection",
    "key": "doubleClickSelectsBlock",
    "ru": "Выделять содержимое пары двойным щелчком",
    "en": "Double click selects block",
    "helpRu": "Двойной щелчок возле скобки или кавычки выделяет содержимое пары.",
    "helpEn": "Controls whether double-clicking next to a bracket or quote selects the content inside. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "smartSelect.selectLeadingAndTrailingWhitespace",
    "ru": "Включать крайние пробелы в умное выделение",
    "en": "Smart select select leading and trailing whitespace",
    "helpRu": "Расширение выделения может захватить пробелы по краям диапазона.",
    "helpEn": "Smart select select leading and trailing whitespace.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "smartSelect.selectSubwords",
    "ru": "Выделять части составных имён",
    "en": "Smart select select subwords",
    "helpRu": "Расширение выделения учитывает отдельные части составного имени.",
    "helpEn": "Smart select select subwords.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "selection",
    "key": "wordSeparators",
    "ru": "Разделители слов",
    "en": "Word separators",
    "helpRu": "Символы, используемые для переходов и выделений по словам.",
    "helpEn": "A string containing the word separators used when doing word navigation. Defaults to `~!@#$%^& ()-=+[{]}\\\\|;:\\'\",.<>/?",
    "kind": "text",
    "choices": []
  },
  {
    "group": "selection",
    "key": "wordSegmenterLocales",
    "ru": "Языки разделения на слова",
    "en": "Word segmenter locales",
    "helpRu": "Массив языковых тегов JSON, например [\"ja\", \"zh-CN\"], для распознавания границ слов.",
    "helpEn": "Locales used for segmenting lines into words when doing word related navigations or operations. Specify the BCP 47 language tag of the word you wish to recognize (e.g., ja, zh-CN, zh-Hant-TW, etc.). Defaults to empty array",
    "kind": "json",
    "choices": []
  },
  {
    "group": "selection",
    "key": "mouseStyle",
    "ru": "Указатель мыши над кодом",
    "en": "Mouse style",
    "helpRu": "Выбирает обычный указатель, текстовый курсор или указатель копирования.",
    "helpEn": "Control the mouse pointer style, either 'text' or 'default' or 'copy' Defaults to 'text'",
    "kind": "select",
    "choices": [
      "text",
      "default",
      "copy"
    ]
  },
  {
    "group": "selection",
    "key": "mouseMiddleClickAction",
    "ru": "Действие средней кнопки мыши",
    "en": "Mouse middle click action",
    "helpRu": "Позволяет открывать ссылки или переходить к определению средней кнопкой.",
    "helpEn": "Enables middle mouse button to open links and Go To Definition",
    "kind": "select",
    "choices": [
      "default",
      "openLink",
      "ctrlLeftClick"
    ]
  },
  {
    "group": "structure",
    "key": "folding",
    "ru": "Сворачивать блоки кода",
    "en": "Folding",
    "helpRu": "Разрешает скрывать тело процедуры, цикла и других блоков.",
    "helpEn": "Enable code folding. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "foldingStrategy",
    "ru": "Определение сворачиваемых блоков",
    "en": "Folding strategy",
    "helpRu": "Использует провайдер языка либо только уровни отступов.",
    "helpEn": "Selects the folding strategy. 'auto' uses the strategies contributed for the current document, 'indentation' uses the indentation based folding strategy. Defaults to 'auto'.",
    "kind": "select",
    "choices": [
      "auto",
      "indentation"
    ]
  },
  {
    "group": "structure",
    "key": "foldingHighlight",
    "ru": "Подсвечивать свёрнутые блоки",
    "en": "Folding highlight",
    "helpRu": "Выделяет фон строки, за которой скрыто содержимое блока.",
    "helpEn": "Enable highlight for folded regions. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "foldingImportsByDefault",
    "ru": "Сворачивать импорты по умолчанию",
    "en": "Folding imports by default",
    "helpRu": "Автоматически скрывает области импортов, если язык их предоставляет.",
    "helpEn": "Auto fold imports folding regions. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "foldingMaximumRegions",
    "ru": "Предел сворачиваемых областей",
    "en": "Folding maximum regions",
    "helpRu": "Ограничивает число вычисляемых областей сворачивания документа.",
    "helpEn": "Maximum number of foldable regions. Defaults to 5000.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "structure",
    "key": "showFoldingControls",
    "ru": "Показывать кнопки сворачивания",
    "en": "Show folding controls",
    "helpRu": "Кнопки слева видны всегда, при наведении или никогда.",
    "helpEn": "Controls whether the fold actions in the gutter stay always visible or hide unless the mouse is over the gutter. Defaults to 'mouseover'.",
    "kind": "select",
    "choices": [
      "always",
      "never",
      "mouseover"
    ]
  },
  {
    "group": "structure",
    "key": "unfoldOnClickAfterEndOfLine",
    "ru": "Раскрывать нажатием после конца строки",
    "en": "Unfold on click after end of line",
    "helpRu": "Щелчок после конца строки свёрнутого блока раскрывает его.",
    "helpEn": "Controls whether clicking on the empty content after a folded line will unfold the line. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "matchBrackets",
    "ru": "Подсвечивать парные скобки",
    "en": "Match brackets",
    "helpRu": "Показывает соответствующую скобку всегда, рядом с курсором или никогда.",
    "helpEn": "Enable highlighting of matching brackets. Defaults to 'always'.",
    "kind": "select",
    "choices": [
      "never",
      "near",
      "always"
    ]
  },
  {
    "group": "structure",
    "key": "bracketPairColorization.enabled",
    "ru": "Разные цвета вложенных скобок",
    "en": "Bracket pair colorization enabled",
    "helpRu": "Раскрашивает пары скобок в соответствии с глубиной вложенности.",
    "helpEn": "Enable or disable bracket pair colorization.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "bracketPairColorization.independentColorPoolPerBracketType",
    "ru": "Отдельные цвета для каждого вида скобок",
    "en": "Bracket pair colorization independent color pool per bracket type",
    "helpRu": "Использует независимую последовательность цветов для разных видов скобок.",
    "helpEn": "Use independent color pool per bracket type.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "guides.indentation",
    "ru": "Направляющие отступов",
    "en": "Guides indentation",
    "helpRu": "Рисует вертикальные линии для уровней отступа.",
    "helpEn": "Enable rendering of indent guides. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "structure",
    "key": "guides.highlightActiveIndentation",
    "ru": "Выделять текущий отступ",
    "en": "Guides highlight active indentation",
    "helpRu": "Подсвечивает направляющую отступа текущего блока.",
    "helpEn": "Enable highlighting of the active indent guide. Defaults to true.",
    "kind": "select",
    "choices": [
      true,
      false,
      "always"
    ]
  },
  {
    "group": "structure",
    "key": "guides.bracketPairs",
    "ru": "Направляющие пар скобок",
    "en": "Guides bracket pairs",
    "helpRu": "Соединяет открывающие и закрывающие скобки линиями вложенности.",
    "helpEn": "Enable rendering of bracket pair guides. Defaults to false.",
    "kind": "select",
    "choices": [
      true,
      false,
      "active"
    ]
  },
  {
    "group": "structure",
    "key": "guides.bracketPairsHorizontal",
    "ru": "Горизонтальные направляющие скобок",
    "en": "Guides bracket pairs horizontal",
    "helpRu": "Добавляет горизонтальные участки направляющих пар скобок.",
    "helpEn": "Enable rendering of vertical bracket pair guides. Defaults to 'active'.",
    "kind": "select",
    "choices": [
      true,
      false,
      "active"
    ]
  },
  {
    "group": "structure",
    "key": "guides.highlightActiveBracketPair",
    "ru": "Выделять текущую пару скобок",
    "en": "Guides highlight active bracket pair",
    "helpRu": "Подсвечивает направляющую пары, внутри которой находится курсор.",
    "helpEn": "Enable highlighting of the active bracket pair. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "links",
    "ru": "Открываемые ссылки в тексте",
    "en": "Links",
    "helpRu": "Разрешает взаимодействие со ссылками, найденными языковым провайдером.",
    "helpEn": "Enable detecting links and making them clickable. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "definitionLinkOpensInPeek",
    "ru": "Определение во встроенной карточке",
    "en": "Definition link opens in peek",
    "helpRu": "Открывает определение внутри редактора вместо прямого перехода.",
    "helpEn": "Controls whether the definition link opens element in the peek widget. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "peekWidgetDefaultFocus",
    "ru": "Фокус карточки определения",
    "en": "Peek widget default focus",
    "helpRu": "Выбирает первоначальный фокус: дерево результатов или редактор карточки.",
    "helpEn": "Controls whether to focus the inline editor in the peek widget by default. Defaults to false.",
    "kind": "select",
    "choices": [
      "tree",
      "editor"
    ]
  },
  {
    "group": "advanced",
    "key": "stablePeek",
    "ru": "Не закрывать карточку определений автоматически",
    "en": "Stable peek",
    "helpRu": "Сохраняет встроенную карточку при двойном щелчке или Escape.",
    "helpEn": "Keep peek editors open even when double-clicking their content or when hitting `Escape`. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "codeLens",
    "ru": "Показывать команды над кодом",
    "en": "CodeLens",
    "helpRu": "Отображает команды и сведения над объявлениями при наличии провайдера CodeLens.",
    "helpEn": "Show code lens Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "codeLensFontFamily",
    "ru": "Шрифт команд над кодом",
    "en": "CodeLens font family",
    "helpRu": "Шрифт строк CodeLens; пустое значение использует шрифт редактора.",
    "helpEn": "Code lens font family. Defaults to editor font family.",
    "kind": "text",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "codeLensFontSize",
    "ru": "Размер команд над кодом",
    "en": "CodeLens font size",
    "helpRu": "Размер шрифта CodeLens; 0 использует размер, выбранный Monaco.",
    "helpEn": "Code lens font size. Default to 90% of the editor font size",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "lightbulb.enabled",
    "ru": "Значок исправлений",
    "en": "Lightbulb enabled",
    "helpRu": "Показывает доступные действия провайдера исправлений на строке кода.",
    "helpEn": "Enable the lightbulb code action. The three possible values are `off`, `on` and `onCode` and the default is `onCode`. `off` disables the code action menu. `on` shows the code action menu on code and on empty lines. `onCode` shows the code action menu on code only.",
    "kind": "select",
    "choices": [
      "off",
      "onCode",
      "on"
    ]
  },
  {
    "group": "advanced",
    "key": "inlayHints.enabled",
    "ru": "Встроенные пояснения в коде",
    "en": "Inlay hints enabled",
    "helpRu": "Определяет видимость коротких пояснений типов и параметров от языкового провайдера.",
    "helpEn": "Enable the inline hints. Defaults to true.",
    "kind": "select",
    "choices": [
      "on",
      "off",
      "offUnlessPressed",
      "onUnlessPressed"
    ]
  },
  {
    "group": "advanced",
    "key": "inlayHints.fontSize",
    "ru": "Размер встроенных пояснений",
    "en": "Inlay hints font size",
    "helpRu": "Размер текста пояснений в пикселях; 0 использует автоматический размер.",
    "helpEn": "Font size of inline hints. Default to 90% of the editor font size.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlayHints.fontFamily",
    "ru": "Шрифт встроенных пояснений",
    "en": "Inlay hints font family",
    "helpRu": "Пустое значение использует основной шрифт редактора.",
    "helpEn": "Font family of inline hints. Defaults to editor font family.",
    "kind": "text",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlayHints.padding",
    "ru": "Поля встроенных пояснений",
    "en": "Inlay hints padding",
    "helpRu": "Добавляет свободное место вокруг встроенных пояснений.",
    "helpEn": "Enables the padding around the inlay hint. Defaults to false.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlayHints.maximumLength",
    "ru": "Предел длины встроенных пояснений",
    "en": "Inlay hints maximum length",
    "helpRu": "Ограничивает длину одного встроенного пояснения.",
    "helpEn": "Maximum length for inlay hints per line Set to 0 to have an unlimited length.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.enabled",
    "ru": "Продолжения внутри строки",
    "en": "Inline suggest enabled",
    "helpRu": "Показывает предварительное продолжение кода от отдельного провайдера дополнений.",
    "helpEn": "Enable or disable the rendering of automatic inline completions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.mode",
    "ru": "Сопоставление продолжений",
    "en": "Inline suggest mode",
    "helpRu": "Выбирает сопоставление по префиксу либо частичному совпадению.",
    "helpEn": "Configures the mode. Use `prefix` to only show ghost text if the text to replace is a prefix of the suggestion text. Use `subword` to only show ghost text if the replace text is a subword of the suggestion text. Use `subwordSmart` to only show ghost text if the replace text is a subword of the suggestion text, but the subword must start after the cursor position. Defaults to `prefix`.",
    "kind": "select",
    "choices": [
      "prefix",
      "subword",
      "subwordSmart"
    ]
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.showToolbar",
    "ru": "Панель продолжений",
    "en": "Inline suggest show toolbar",
    "helpRu": "Определяет, когда показывать команды предварительного продолжения.",
    "helpEn": "Inline suggest show toolbar.",
    "kind": "select",
    "choices": [
      "always",
      "onHover",
      "never"
    ]
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.syntaxHighlightingEnabled",
    "ru": "Подсветка предварительного продолжения",
    "en": "Inline suggest syntax highlighting enabled",
    "helpRu": "Раскрашивает ещё не принятое продолжение как код.",
    "helpEn": "Inline suggest syntax highlighting enabled.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.suppressSuggestions",
    "ru": "Скрывать список при продолжении",
    "en": "Inline suggest suppress suggestions",
    "helpRu": "Не показывает обычный список дополнений одновременно с продолжением внутри строки.",
    "helpEn": "Inline suggest suppress suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.minShowDelay",
    "ru": "Задержка продолжений",
    "en": "Inline suggest min show delay",
    "helpRu": "Минимальное время до показа продолжения, в миллисекундах.",
    "helpEn": "Inline suggest min show delay.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.suppressInSnippetMode",
    "ru": "Скрывать продолжения внутри заготовки",
    "en": "Inline suggest suppress in snippet mode",
    "helpRu": "Не предлагает продолжение при заполнении полей заготовки кода.",
    "helpEn": "Inline suggest suppress in snippet mode.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.keepOnBlur",
    "ru": "Сохранять продолжение без фокуса",
    "en": "Inline suggest keep on blur",
    "helpRu": "Оставляет предварительное продолжение после ухода фокуса из редактора.",
    "helpEn": "Does not clear active inline suggestions when the editor loses focus.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineSuggest.fontFamily",
    "ru": "Шрифт продолжений",
    "en": "Inline suggest font family",
    "helpRu": "Шрифт предварительного продолжения; default использует основной шрифт.",
    "helpEn": "Font family for inline suggestions.",
    "kind": "text",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "find.cursorMoveOnType",
    "ru": "Переход к результату во время поиска",
    "en": "Find cursor move on type",
    "helpRu": "Перемещает курсор к совпадению при вводе поискового запроса.",
    "helpEn": "Controls whether the cursor should move to find matches while typing.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "find.findOnType",
    "ru": "Поиск по мере ввода",
    "en": "Find find on type",
    "helpRu": "Обновляет совпадения сразу при изменении запроса в строке поиска.",
    "helpEn": "Controls whether the find widget should search as you type.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "find.seedSearchStringFromSelection",
    "ru": "Брать запрос из выделения",
    "en": "Find seed search string from selection",
    "helpRu": "Определяет, когда выделенный текст становится запросом поиска.",
    "helpEn": "Controls if we seed search string in the Find Widget with editor selection.",
    "kind": "select",
    "choices": [
      "never",
      "always",
      "selection"
    ]
  },
  {
    "group": "advanced",
    "key": "find.autoFindInSelection",
    "ru": "Автоматический поиск в выделении",
    "en": "Find auto find in selection",
    "helpRu": "Ограничивает поиск выделенной областью всегда или только для нескольких строк.",
    "helpEn": "Controls if Find in Selection flag is turned on in the editor.",
    "kind": "select",
    "choices": [
      "never",
      "always",
      "multiline"
    ]
  },
  {
    "group": "advanced",
    "key": "find.addExtraSpaceOnTop",
    "ru": "Место для строки поиска сверху",
    "en": "Find add extra space on top",
    "helpRu": "Освобождает пространство над кодом для панели поиска.",
    "helpEn": "Find add extra space on top.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "find.loop",
    "ru": "Циклический поиск",
    "en": "Find loop",
    "helpRu": "После последнего совпадения поиск возвращается к первому.",
    "helpEn": "Controls whether the search result and diff result automatically restarts from the beginning (or the end) when no further matches can be found",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "find.closeOnResult",
    "ru": "Закрывать поиск после перехода",
    "en": "Find close on result",
    "helpRu": "Закрывает панель поиска после явного перехода к найденному совпадению.",
    "helpEn": "Controls whether to close the Find Widget after an explicit find navigation command lands on a match.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.nonBasicASCII",
    "ru": "Выделять символы вне ASCII",
    "en": "Unicode highlight non basic ascii",
    "helpRu": "Предупреждает о символах вне основного набора ASCII; русские строки и комментарии тоже могут содержать такие символы.",
    "helpEn": "Controls whether all non-basic ASCII characters are highlighted. Only characters between U+0020 and U+007E, tab, line-feed and carriage-return are considered basic ASCII.",
    "kind": "select",
    "choices": [
      true,
      false,
      "inUntrustedWorkspace"
    ]
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.invisibleCharacters",
    "ru": "Выделять невидимые символы",
    "en": "Unicode highlight invisible characters",
    "helpRu": "Отмечает невидимые символы, которые могут мешать чтению кода.",
    "helpEn": "Controls whether characters that just reserve space or have no width at all are highlighted.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.ambiguousCharacters",
    "ru": "Выделять похожие символы",
    "en": "Unicode highlight ambiguous characters",
    "helpRu": "Отмечает знаки, похожие на другие символы, например латинские и кириллические буквы.",
    "helpEn": "Controls whether characters are highlighted that can be confused with basic ASCII characters, except those that are common in the current user locale.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.includeComments",
    "ru": "Проверять символы в комментариях",
    "en": "Unicode highlight include comments",
    "helpRu": "Применяет выбранные предупреждения о символах также к комментариям.",
    "helpEn": "Controls whether characters in comments should also be subject to unicode highlighting.",
    "kind": "select",
    "choices": [
      true,
      false,
      "inUntrustedWorkspace"
    ]
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.includeStrings",
    "ru": "Проверять символы в строках",
    "en": "Unicode highlight include strings",
    "helpRu": "Применяет выбранные предупреждения о символах также к строковым литералам.",
    "helpEn": "Controls whether characters in strings should also be subject to unicode highlighting.",
    "kind": "select",
    "choices": [
      true,
      false,
      "inUntrustedWorkspace"
    ]
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.allowedCharacters",
    "ru": "Разрешённые символы",
    "en": "Unicode highlight allowed characters",
    "helpRu": "JSON-объект разрешённых знаков, например {\"а\": true}. Эти знаки не получают предупреждений.",
    "helpEn": "Defines allowed characters that are not being highlighted.",
    "kind": "json",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "unicodeHighlight.allowedLocales",
    "ru": "Разрешённые языки символов",
    "en": "Unicode highlight allowed locales",
    "helpRu": "JSON-объект языковых тегов, например {\"ru\": true}, для исключения обычных букв этих языков.",
    "helpEn": "Unicode characters that are common in allowed locales are not being highlighted.",
    "kind": "json",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "accessibilitySupport",
    "ru": "Поддержка чтения с экрана",
    "en": "Accessibility support",
    "helpRu": "Автоматически определяет программу чтения с экрана или задаёт режим явно.",
    "helpEn": "Configure the editor's accessibility support. Defaults to 'auto'. It is best to leave this to 'auto'.",
    "kind": "select",
    "choices": [
      "auto",
      "off",
      "on"
    ]
  },
  {
    "group": "advanced",
    "key": "accessibilityPageSize",
    "ru": "Строк для чтения с экрана",
    "en": "Accessibility page size",
    "helpRu": "Число строк, одновременно доступных программе чтения с экрана.",
    "helpEn": "Controls the number of lines in the editor that can be read out by a screen reader",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "screenReaderAnnounceInlineSuggestion",
    "ru": "Озвучивать предварительное продолжение",
    "en": "Screen reader announce inline suggestion",
    "helpRu": "Немедленно сообщает программе чтения с экрана текст предложенного продолжения.",
    "helpEn": "Control whether a screen reader announces inline suggestion content immediately.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "inlineCompletionsAccessibilityVerbose",
    "ru": "Подробные сообщения о продолжениях",
    "en": "Inline completions accessibility verbose",
    "helpRu": "Добавляет подсказки доступности при показе продолжения внутри строки.",
    "helpEn": "Controls whether the accessibility hint should be provided to screen reader users when an inline completion is shown.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "tabFocusMode",
    "ru": "Tab переводит фокус вместо отступа",
    "en": "Tab focus mode",
    "helpRu": "Клавиша Tab перемещает фокус на следующий элемент интерфейса.",
    "helpEn": "Controls whether the editor / terminal receives tabs or defers them to the workbench for navigation.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "largeFileOptimizations",
    "ru": "Оптимизация больших файлов",
    "en": "Large file optimizations",
    "helpRu": "Monaco отключает затратные возможности для очень больших документов.",
    "helpEn": "Special handling for large files to disable certain memory intensive features. Defaults to true.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "maxTokenizationLineLength",
    "ru": "Предел длины строки для подсветки",
    "en": "Max tokenization line length",
    "helpRu": "Строки длиннее указанного числа символов не разбираются для синтаксической подсветки.",
    "helpEn": "Lines above this length will not be tokenized for performance reasons. Defaults to 20000.",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "stopRenderingLineAfter",
    "ru": "Предел отображения длинной строки",
    "en": "Stop rendering line after",
    "helpRu": "Число символов, после которого отображение строки ограничивается; -1 снимает предел.",
    "helpEn": "Performance guard: Stop rendering a line after x characters. Defaults to 10000. Use -1 to never stop rendering",
    "kind": "number",
    "choices": []
  },
  {
    "group": "advanced",
    "key": "unusualLineTerminators",
    "ru": "Необычные разделители строк",
    "en": "Unusual line terminators",
    "helpRu": "Выбирает действие при обнаружении нестандартных символов перевода строки.",
    "helpEn": "Remove unusual line terminators like LINE SEPARATOR (LS), PARAGRAPH SEPARATOR (PS). Defaults to 'prompt'.",
    "kind": "select",
    "choices": [
      "auto",
      "off",
      "prompt"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multiple",
    "ru": "Общее действие при нескольких целях перехода",
    "en": "Goto location multiple",
    "helpRu": "Переходит к определению, открывает карточку или совмещает оба действия при нескольких результатах.",
    "helpEn": "Goto location multiple.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleDefinitions",
    "ru": "Несколько определений",
    "en": "Goto location multiple definitions",
    "helpRu": "Выбирает способ показа нескольких определений, полученных от провайдера языка.",
    "helpEn": "Goto location multiple definitions.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleTypeDefinitions",
    "ru": "Несколько определений типа",
    "en": "Goto location multiple type definitions",
    "helpRu": "Выбирает способ показа нескольких определений типа, полученных от провайдера языка.",
    "helpEn": "Goto location multiple type definitions.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleDeclarations",
    "ru": "Несколько объявлений",
    "en": "Goto location multiple declarations",
    "helpRu": "Выбирает способ показа нескольких объявлений сущности.",
    "helpEn": "Goto location multiple declarations.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleImplementations",
    "ru": "Несколько реализаций",
    "en": "Goto location multiple implementations",
    "helpRu": "Выбирает способ показа нескольких реализаций сущности при поддержке языка.",
    "helpEn": "Goto location multiple implementations.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleReferences",
    "ru": "Несколько использований",
    "en": "Goto location multiple references",
    "helpRu": "Выбирает способ показа нескольких найденных использований сущности.",
    "helpEn": "Goto location multiple references.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "advanced",
    "key": "gotoLocation.multipleTests",
    "ru": "Несколько связанных проверок",
    "en": "Goto location multiple tests",
    "helpRu": "Выбирает способ показа связанных проверок, если провайдер языка их предоставляет.",
    "helpEn": "Goto location multiple tests.",
    "kind": "select",
    "choices": [
      "peek",
      "gotoAndPeek",
      "goto"
    ]
  },
  {
    "group": "suggestions",
    "key": "suggest.showMethods",
    "ru": "В списке: методы",
    "en": "Show methods",
    "helpRu": "Показывает дополнения категории «Методы», если провайдер языка их предоставляет.",
    "helpEn": "Show method-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showFunctions",
    "ru": "В списке: функции",
    "en": "Show functions",
    "helpRu": "Показывает дополнения категории «Функции», если провайдер языка их предоставляет.",
    "helpEn": "Show function-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showConstructors",
    "ru": "В списке: конструкторы",
    "en": "Show constructors",
    "helpRu": "Показывает дополнения категории «Конструкторы», если провайдер языка их предоставляет.",
    "helpEn": "Show constructor-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showDeprecated",
    "ru": "В списке: устаревшие сущности",
    "en": "Show deprecated",
    "helpRu": "Показывает дополнения категории «Устаревшие сущности», если провайдер языка их предоставляет.",
    "helpEn": "Show deprecated-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showFields",
    "ru": "В списке: поля",
    "en": "Show fields",
    "helpRu": "Показывает дополнения категории «Поля», если провайдер языка их предоставляет.",
    "helpEn": "Show field-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showVariables",
    "ru": "В списке: переменные",
    "en": "Show variables",
    "helpRu": "Показывает дополнения категории «Переменные», если провайдер языка их предоставляет.",
    "helpEn": "Show variable-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showClasses",
    "ru": "В списке: классы",
    "en": "Show classes",
    "helpRu": "Показывает дополнения категории «Классы», если провайдер языка их предоставляет.",
    "helpEn": "Show class-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showStructs",
    "ru": "В списке: структуры",
    "en": "Show structs",
    "helpRu": "Показывает дополнения категории «Структуры», если провайдер языка их предоставляет.",
    "helpEn": "Show struct-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showInterfaces",
    "ru": "В списке: интерфейсы",
    "en": "Show interfaces",
    "helpRu": "Показывает дополнения категории «Интерфейсы», если провайдер языка их предоставляет.",
    "helpEn": "Show interface-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showModules",
    "ru": "В списке: модули",
    "en": "Show modules",
    "helpRu": "Показывает дополнения категории «Модули», если провайдер языка их предоставляет.",
    "helpEn": "Show module-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showProperties",
    "ru": "В списке: свойства",
    "en": "Show properties",
    "helpRu": "Показывает дополнения категории «Свойства», если провайдер языка их предоставляет.",
    "helpEn": "Show property-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showEvents",
    "ru": "В списке: события",
    "en": "Show events",
    "helpRu": "Показывает дополнения категории «События», если провайдер языка их предоставляет.",
    "helpEn": "Show event-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showOperators",
    "ru": "В списке: операторы",
    "en": "Show operators",
    "helpRu": "Показывает дополнения категории «Операторы», если провайдер языка их предоставляет.",
    "helpEn": "Show operator-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showUnits",
    "ru": "В списке: единицы",
    "en": "Show units",
    "helpRu": "Показывает дополнения категории «Единицы», если провайдер языка их предоставляет.",
    "helpEn": "Show unit-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showValues",
    "ru": "В списке: значения",
    "en": "Show values",
    "helpRu": "Показывает дополнения категории «Значения», если провайдер языка их предоставляет.",
    "helpEn": "Show value-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showConstants",
    "ru": "В списке: константы",
    "en": "Show constants",
    "helpRu": "Показывает дополнения категории «Константы», если провайдер языка их предоставляет.",
    "helpEn": "Show constant-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showEnums",
    "ru": "В списке: перечисления",
    "en": "Show enums",
    "helpRu": "Показывает дополнения категории «Перечисления», если провайдер языка их предоставляет.",
    "helpEn": "Show enum-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showEnumMembers",
    "ru": "В списке: элементы перечислений",
    "en": "Show enum members",
    "helpRu": "Показывает дополнения категории «Элементы перечислений», если провайдер языка их предоставляет.",
    "helpEn": "Show enumMember-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showKeywords",
    "ru": "В списке: ключевые слова",
    "en": "Show keywords",
    "helpRu": "Показывает дополнения категории «Ключевые слова», если провайдер языка их предоставляет.",
    "helpEn": "Show keyword-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showWords",
    "ru": "В списке: слова документа",
    "en": "Show words",
    "helpRu": "Показывает дополнения категории «Слова документа», если провайдер языка их предоставляет.",
    "helpEn": "Show text-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showColors",
    "ru": "В списке: цвета",
    "en": "Show colors",
    "helpRu": "Показывает дополнения категории «Цвета», если провайдер языка их предоставляет.",
    "helpEn": "Show color-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showFiles",
    "ru": "В списке: файлы",
    "en": "Show files",
    "helpRu": "Показывает дополнения категории «Файлы», если провайдер языка их предоставляет.",
    "helpEn": "Show file-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showReferences",
    "ru": "В списке: ссылки",
    "en": "Show references",
    "helpRu": "Показывает дополнения категории «Ссылки», если провайдер языка их предоставляет.",
    "helpEn": "Show reference-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showFolders",
    "ru": "В списке: папки",
    "en": "Show folders",
    "helpRu": "Показывает дополнения категории «Папки», если провайдер языка их предоставляет.",
    "helpEn": "Show folder-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showTypeParameters",
    "ru": "В списке: параметры типов",
    "en": "Show type parameters",
    "helpRu": "Показывает дополнения категории «Параметры типов», если провайдер языка их предоставляет.",
    "helpEn": "Show typeParameter-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showIssues",
    "ru": "В списке: задачи",
    "en": "Show issues",
    "helpRu": "Показывает дополнения категории «Задачи», если провайдер языка их предоставляет.",
    "helpEn": "Show issue-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showUsers",
    "ru": "В списке: пользователи",
    "en": "Show users",
    "helpRu": "Показывает дополнения категории «Пользователи», если провайдер языка их предоставляет.",
    "helpEn": "Show user-suggestions.",
    "kind": "boolean",
    "choices": []
  },
  {
    "group": "suggestions",
    "key": "suggest.showSnippets",
    "ru": "В списке: заготовки кода",
    "en": "Show snippets",
    "helpRu": "Показывает дополнения категории «Заготовки кода», если провайдер языка их предоставляет.",
    "helpEn": "Show snippet-suggestions.",
    "kind": "boolean",
    "choices": []
  }
];
  root.EditorSettingsEntries = entries;
  if (typeof module !== "undefined") module.exports = entries;
})(typeof window === "undefined" ? globalThis : window);
