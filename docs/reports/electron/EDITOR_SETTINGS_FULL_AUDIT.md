# Полная проверка параметров редактора

Дата: 2026-10-14 12:18 (Москва). IDE 0.4.2; язык и компилятор 0.1.12.

Проверены все 261 поле каталога Monaco 0.56.0: 259 действующих настроек и 2 устаревших недоступных поля. Через элементы реального диалога выполнены 356 вариантов допустимых значений и 95 попыток ошибочного ввода. Для каждой действующей настройки проверены применение к фактическим параметрам Monaco, запись профиля, повторное открытие, отмена, сброс и восстановление после перезапуска Electron. Выполнены 17/17 сценариев Electron с 673 утверждениями; ошибок проверок нет. Отдельные проверки схемы профиля Node прошли 5/5.

## Что подтверждают проверки

Матрица сравнивает фактические значения через дескрипторы EditorOptions, службу конфигурации Monaco и параметры моделей. Одна только передача значения в updateOptions не считается успешной проверкой. Дополнительно пять сценариев проверяют видимое оформление, реальные действия ввода, 27 категорий дополнений и устаревшие дополнения, CodeLens, встроенные подсказки, продолжение строки и оформление диагностик. Три сценария диалога проверяют отказ записи профиля, локали, клавиатурное управление и проверку ввода; отдельный сценарий проверяет согласованность переводов.

## Исправления по результатам прогона

- Поля matchOnWordStartOnly и wordBasedSuggestionsOnlySameLanguage отсутствуют в действующем API. Они отключены с пояснениями на двух языках; сохранённый профиль их исключает. Используются действующие suggest.matchOnWordStartOnly и режимы wordBasedSuggestions.
- Исправлены исходные значения glyphMargin, multiCursorModifier, overtypeCursorStyle, autoIndent и wordBasedSuggestions; добавлен фактический режим offWithInlineSuggestions.
- При обновлении вложенной группы передаются исходные значения её соседних полей. Устранено незаметное изменение suggest.snippetsPreventQuickSuggestions и unicodeHighlight.includeStrings.
- inlineSuggest.fontFamily принимает произвольное имя шрифта.
- Профиль повторно применяется после создания контроллера встроенных дополнений, чтобы inlineCompletionsAccessibilityVerbose сохранялся после перезапуска.
- Для толщины курсора пояснены физические пиксели: при масштабе 125% три физических пикселя соответствуют 2,4 пикселя CSS.

## Границы проверки

Для всех действующих полей подтверждён договор диалога, профиля и фактической конфигурации редактора. Это не означает визуальную проверку каждого значения во всех возможных документах и на любом оборудовании. Проверки CodeLens, встроенных подсказок и продолжения строки используют временные тестовые провайдеры; наличие штатных провайдеров Axiom этим не утверждается. При завершении временного провайдера Monaco сообщает Canceled об отмене фонового запроса; это событие сохранено в доказательствах и отдельно допускается тестом. Ошибка отсутствующего метода disposeInlineCompletions в тестовом провайдере исправлена. Полный общий набор из 138 сценариев IDE не запускался. Два устаревших поля проверены на недоступность, а не на действие.

## Доказательства

- [Матрица всех полей](../../../ide/e2e/evidence/editor-settings-all-fields.json).
- [Объединённые результаты 17 сценариев](../../../ide/e2e/evidence/editor-settings-full-results.json).
- Воспроизведение: node --test ide/tests/editor-preferences.test.cjs; node ide/e2e/runner.js editor-settings-all.spec.js; node ide/e2e/runner.js editor-settings-behavior.spec.js; node ide/e2e/runner.js editor-settings.spec.js; node ide/e2e/runner.js i18n-parity. Матрица создаётся полным запуском всех восьми групп.

## Результат каждого поля

В колонке «Цикл» перечислены применение, сохранение, повторное открытие, отмена, сброс и перезапуск. Число вариантов отражает отдельные проверенные допустимые значения; для перечислений перебираются варианты, отличающиеся от исходного.

| Параметр | Название | Допустимые варианты | Ошибочный ввод | Цикл | Зависимость |
|---|---|---:|---:|---|---|
| lineNumbers | Номера строк | 3 | 0 | Пройден | Параметр Monaco |
| lineNumbersMinChars | Ширина области номеров | 1 | 2 | Пройден | Параметр Monaco |
| selectOnLineNumbers | Выделять строку нажатием на номер | 1 | 0 | Пройден | Параметр Monaco |
| glyphMargin | Поле значков слева | 1 | 0 | Пройден | Параметр Monaco |
| lineDecorationsWidth | Ширина поля обозначений | 1 | 2 | Пройден | Параметр Monaco |
| renderWhitespace | Показывать пробелы и табуляцию | 4 | 0 | Пройден | Параметр Monaco |
| renderControlCharacters | Показывать управляющие символы | 1 | 0 | Пройден | Параметр Monaco |
| renderFinalNewline | Номер последней пустой строки | 2 | 0 | Пройден | Параметр Monaco |
| renderLineHighlight | Подсветка текущей строки | 3 | 0 | Пройден | Параметр Monaco |
| renderLineHighlightOnlyWhenFocus | Подсвечивать строку только при фокусе | 1 | 0 | Пройден | Параметр Monaco |
| renderValidationDecorations | Обозначения ошибок и предупреждений | 2 | 0 | Пройден | Параметр Monaco |
| showUnused | Приглушать неиспользуемые имена | 1 | 0 | Пройден | Параметр Monaco |
| showDeprecated | Зачёркивать устаревшие сущности | 1 | 0 | Пройден | Параметр Monaco |
| rulers | Вертикальные линии колонок | 1 | 1 | Пройден | Параметр Monaco |
| overviewRulerLanes | Число дорожек обзорной полосы | 1 | 2 | Пройден | Параметр Monaco |
| overviewRulerBorder | Граница обзорной полосы | 1 | 0 | Пройден | Параметр Monaco |
| hideCursorInOverviewRuler | Скрывать курсор в обзорной полосе | 1 | 0 | Пройден | Параметр Monaco |
| padding.top | Поле сверху | 1 | 2 | Пройден | Параметр Monaco |
| padding.bottom | Поле снизу | 1 | 2 | Пройден | Параметр Monaco |
| colorDecorators | Показывать образцы цветов | 1 | 0 | Пройден | Параметр Monaco |
| colorDecoratorsActivatedOn | Открытие выбора цвета | 2 | 0 | Пройден | Параметр Monaco |
| colorDecoratorsLimit | Предел числа образцов цвета | 1 | 2 | Пройден | Параметр Monaco |
| defaultColorDecorators | Стандартное распознавание цветов | 2 | 0 | Пройден | Параметр Monaco |
| semanticHighlighting.enabled | Семантическая подсветка | 2 | 0 | Пройден | Параметр Monaco |
| autoDetectHighContrast | Учитывать высокий контраст системы | 1 | 0 | Пройден | Параметр Monaco |
| fontFamily | Шрифт редактора | 1 | 1 | Пройден | Параметр Monaco |
| fontSize | Размер шрифта | 1 | 2 | Пройден | Параметр Monaco |
| fontWeight | Толщина шрифта | 1 | 1 | Пройден | Параметр Monaco |
| fontLigatures | Лигатуры | 1 | 0 | Пройден | Параметр Monaco |
| fontVariations | Вариативный шрифт | 1 | 0 | Пройден | Параметр Monaco |
| lineHeight | Высота строки | 1 | 2 | Пройден | Параметр Monaco |
| letterSpacing | Межбуквенный интервал | 1 | 2 | Пройден | Параметр Monaco |
| tabSize | Ширина табуляции | 1 | 2 | Пройден | Параметр Monaco |
| insertSpaces | Вставлять пробелы вместо табуляции | 1 | 0 | Пройден | Параметр Monaco |
| detectIndentation | Определять отступы по файлу | 1 | 0 | Пройден | Параметр Monaco |
| trimAutoWhitespace | Удалять лишние автоматические отступы | 1 | 0 | Пройден | Параметр Monaco |
| autoIndent | Автоматические отступы | 4 | 0 | Пройден | Параметр Monaco |
| autoIndentOnPaste | Выравнивать отступы при вставке | 1 | 0 | Пройден | Параметр Monaco |
| autoIndentOnPasteWithinString | Выравнивать вставку внутри строки | 1 | 0 | Пройден | Параметр Monaco |
| autoClosingBrackets | Закрывать скобки автоматически | 3 | 0 | Пройден | Параметр Monaco |
| autoClosingQuotes | Закрывать кавычки автоматически | 3 | 0 | Пройден | Параметр Monaco |
| autoClosingComments | Закрывать комментарии автоматически | 3 | 0 | Пройден | Параметр Monaco |
| autoClosingDelete | Удалять автоматически вставленную пару | 2 | 0 | Пройден | Параметр Monaco |
| autoClosingOvertype | Проходить через закрывающий знак | 2 | 0 | Пройден | Параметр Monaco |
| autoSurround | Обрамлять выделение | 3 | 0 | Пройден | Параметр Monaco |
| stickyTabStops | Перемещаться по ступеням отступа | 1 | 0 | Пройден | Параметр Monaco |
| useTabStops | Учитывать границы табуляции | 1 | 0 | Пройден | Параметр Monaco |
| trimWhitespaceOnDelete | Удалять пробелы при удалении | 1 | 0 | Пройден | Параметр Monaco |
| formatOnType | Форматировать при вводе | 1 | 0 | Пройден | Нужен языковой провайдер |
| formatOnPaste | Форматировать при вставке | 1 | 0 | Пройден | Нужен языковой провайдер |
| overtypeOnPaste | Замещать текст при вставке | 1 | 0 | Пройден | Параметр Monaco |
| comments.insertSpace | Пробел после знака комментария | 1 | 0 | Пройден | Параметр Monaco |
| comments.ignoreEmptyLines | Пропускать пустые строки при комментировании | 1 | 0 | Пройден | Параметр Monaco |
| emptySelectionClipboard | Копировать строку без выделения | 1 | 0 | Пройден | Параметр Monaco |
| copyWithSyntaxHighlighting | Копировать с подсветкой | 1 | 0 | Пройден | Параметр Monaco |
| dragAndDrop | Перетаскивать выделенный текст | 1 | 0 | Пройден | Параметр Monaco |
| dropIntoEditor.enabled | Вставка перетаскиванием | 1 | 0 | Пройден | Параметр Monaco |
| dropIntoEditor.showDropSelector | Выбор способа после перетаскивания | 1 | 0 | Пройден | Параметр Monaco |
| pasteAs.enabled | Выбор способа вставки | 1 | 0 | Пройден | Параметр Monaco |
| pasteAs.showPasteSelector | Показывать способы после вставки | 1 | 0 | Пройден | Параметр Monaco |
| linkedEditing | Связанное редактирование | 1 | 0 | Пройден | Нужен языковой провайдер |
| renameOnType | Переименование при вводе | 1 | 0 | Пройден | Нужен языковой провайдер |
| quickSuggestions.other | Автоматические подсказки в коде | 5 | 0 | Пройден | Параметр Monaco |
| quickSuggestions.comments | Автоматические подсказки в комментариях | 5 | 0 | Пройден | Параметр Monaco |
| quickSuggestions.strings | Автоматические подсказки в строках | 5 | 0 | Пройден | Параметр Monaco |
| quickSuggestionsDelay | Задержка автоматических подсказок | 1 | 2 | Пройден | Параметр Monaco |
| suggestOnTriggerCharacters | Подсказки после специальных символов | 1 | 0 | Пройден | Параметр Monaco |
| acceptSuggestionOnEnter | Принимать дополнение клавишей Enter | 2 | 0 | Пройден | Параметр Monaco |
| acceptSuggestionOnCommitCharacter | Принимать дополнение завершающим символом | 1 | 0 | Пройден | Параметр Monaco |
| tabCompletion | Дополнение клавишей Tab | 2 | 0 | Пройден | Параметр Monaco |
| snippetSuggestions | Место заготовок в списке | 3 | 0 | Пройден | Параметр Monaco |
| suggestSelection | Запоминание выбранных дополнений | 2 | 0 | Пройден | Параметр Monaco |
| suggestFontSize | Размер шрифта списка дополнений | 1 | 2 | Пройден | Параметр Monaco |
| suggestLineHeight | Высота строки списка дополнений | 1 | 2 | Пройден | Параметр Monaco |
| matchOnWordStartOnly | Совпадение только с начала слова | 0 | 0 | Недоступен; профиль исключает | Устаревший API |
| suggest.insertMode | Вставка или замещение дополнением | 1 | 0 | Пройден | Параметр Monaco |
| suggest.filterGraceful | Мягкая фильтрация дополнений | 1 | 0 | Пройден | Параметр Monaco |
| suggest.snippetsPreventQuickSuggestions | Не показывать подсказки внутри заготовки | 1 | 0 | Пройден | Параметр Monaco |
| suggest.localityBonus | Предпочитать ближайшие имена | 1 | 0 | Пройден | Параметр Monaco |
| suggest.shareSuggestSelections | Общее запоминание выбора | 1 | 0 | Пройден | Параметр Monaco |
| suggest.selectionMode | Предварительный выбор дополнения | 3 | 0 | Пройден | Параметр Monaco |
| suggest.showIcons | Значки в списке дополнений | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showStatusBar | Нижняя строка списка дополнений | 1 | 0 | Пройден | Параметр Monaco |
| suggest.preview | Предпросмотр дополнения | 1 | 0 | Пройден | Параметр Monaco |
| suggest.previewMode | Способ предпросмотра дополнения | 2 | 0 | Пройден | Параметр Monaco |
| suggest.showInlineDetails | Краткое описание рядом с дополнением | 1 | 0 | Пройден | Параметр Monaco |
| suggest.matchOnWordStartOnly | Фильтрация списка с начала слова | 1 | 0 | Пройден | Параметр Monaco |
| hover.enabled | Подсказки при наведении | 2 | 0 | Пройден | Параметр Monaco |
| hover.delay | Задержка подсказки при наведении | 1 | 2 | Пройден | Параметр Monaco |
| hover.sticky | Удерживать подсказку под мышью | 1 | 0 | Пройден | Параметр Monaco |
| hover.hidingDelay | Задержка скрытия подсказки | 1 | 2 | Пройден | Параметр Monaco |
| hover.above | Предпочитать подсказку над строкой | 1 | 0 | Пройден | Параметр Monaco |
| hover.showLongLineWarning | Предупреждение о длинной строке | 1 | 0 | Пройден | Параметр Monaco |
| parameterHints.enabled | Подсказки параметров | 1 | 0 | Пройден | Параметр Monaco |
| parameterHints.cycle | Циклический перебор сигнатур | 1 | 0 | Пройден | Параметр Monaco |
| wordBasedSuggestions | Дополнения из текста документов | 4 | 0 | Пройден | Параметр Monaco |
| wordBasedSuggestionsOnlySameLanguage | Слова только из документов того же языка | 0 | 0 | Недоступен; профиль исключает | Устаревший API |
| suggest.showMethods | В списке: методы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showFunctions | В списке: функции | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showConstructors | В списке: конструкторы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showDeprecated | В списке: устаревшие сущности | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showFields | В списке: поля | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showVariables | В списке: переменные | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showClasses | В списке: классы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showStructs | В списке: структуры | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showInterfaces | В списке: интерфейсы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showModules | В списке: модули | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showProperties | В списке: свойства | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showEvents | В списке: события | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showOperators | В списке: операторы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showUnits | В списке: единицы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showValues | В списке: значения | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showConstants | В списке: константы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showEnums | В списке: перечисления | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showEnumMembers | В списке: элементы перечислений | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showKeywords | В списке: ключевые слова | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showWords | В списке: слова документа | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showColors | В списке: цвета | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showFiles | В списке: файлы | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showReferences | В списке: ссылки | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showFolders | В списке: папки | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showTypeParameters | В списке: параметры типов | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showIssues | В списке: задачи | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showUsers | В списке: пользователи | 1 | 0 | Пройден | Параметр Monaco |
| suggest.showSnippets | В списке: заготовки кода | 1 | 0 | Пройден | Параметр Monaco |
| stickyScroll.enabled | Закреплять заголовки блоков при прокрутке | 1 | 0 | Пройден | Параметр Monaco |
| stickyScroll.maxLineCount | Максимум закреплённых строк | 1 | 2 | Пройден | Параметр Monaco |
| stickyScroll.defaultModel | Источник закреплённых заголовков | 2 | 0 | Пройден | Параметр Monaco |
| stickyScroll.scrollWithEditor | Горизонтальная прокрутка закреплённых строк | 1 | 0 | Пройден | Параметр Monaco |
| minimap.enabled | Показывать миникарту | 1 | 0 | Пройден | Параметр Monaco |
| minimap.autohide | Автоматически скрывать миникарту | 2 | 0 | Пройден | Параметр Monaco |
| minimap.side | Сторона миникарты | 1 | 0 | Пройден | Параметр Monaco |
| minimap.size | Высота миникарты | 2 | 0 | Пройден | Параметр Monaco |
| minimap.showSlider | Ползунок миникарты | 1 | 0 | Пройден | Параметр Monaco |
| minimap.renderCharacters | Символы на миникарте | 1 | 0 | Пройден | Параметр Monaco |
| minimap.maxColumn | Предел колонок миникарты | 1 | 2 | Пройден | Параметр Monaco |
| minimap.scale | Масштаб миникарты | 1 | 2 | Пройден | Параметр Monaco |
| minimap.showRegionSectionHeaders | Заголовки областей на миникарте | 1 | 0 | Пройден | Параметр Monaco |
| minimap.showMarkSectionHeaders | Заголовки отметок на миникарте | 1 | 0 | Пройден | Параметр Monaco |
| minimap.sectionHeaderFontSize | Размер заголовков миникарты | 1 | 2 | Пройден | Параметр Monaco |
| minimap.sectionHeaderLetterSpacing | Интервал букв заголовков миникарты | 1 | 2 | Пройден | Параметр Monaco |
| minimap.markSectionHeaderRegex | Шаблон заголовков миникарты | 1 | 1 | Пройден | Параметр Monaco |
| smoothScrolling | Плавная прокрутка | 1 | 0 | Пройден | Параметр Monaco |
| scrollBeyondLastLine | Прокрутка ниже последней строки | 1 | 0 | Пройден | Параметр Monaco |
| scrollBeyondLastColumn | Запас справа от текста | 1 | 2 | Пройден | Параметр Monaco |
| revealHorizontalRightPadding | Запас справа при переходе к курсору | 1 | 2 | Пройден | Параметр Monaco |
| scrollOnMiddleClick | Прокрутка средней кнопкой мыши | 1 | 0 | Пройден | Параметр Monaco |
| scrollPredominantAxis | Прокрутка по основной оси | 1 | 0 | Пройден | Параметр Monaco |
| inertialScroll | Инерционная прокрутка | 1 | 0 | Пройден | Параметр Monaco |
| mouseWheelScrollSensitivity | Чувствительность колеса | 1 | 2 | Пройден | Параметр Monaco |
| fastScrollSensitivity | Ускоренная прокрутка | 1 | 2 | Пройден | Параметр Monaco |
| mouseWheelZoom | Изменять шрифт с помощью Ctrl и колеса | 1 | 0 | Пройден | Параметр Monaco |
| scrollbar.vertical | Вертикальная полоса прокрутки | 2 | 0 | Пройден | Параметр Monaco |
| scrollbar.horizontal | Горизонтальная полоса прокрутки | 2 | 0 | Пройден | Параметр Monaco |
| scrollbar.verticalScrollbarSize | Ширина вертикальной полосы | 1 | 2 | Пройден | Параметр Monaco |
| scrollbar.horizontalScrollbarSize | Высота горизонтальной полосы | 1 | 2 | Пройден | Параметр Monaco |
| scrollbar.handleMouseWheel | Обрабатывать колесо мыши | 1 | 0 | Пройден | Параметр Monaco |
| scrollbar.alwaysConsumeMouseWheel | Всегда удерживать колесо в редакторе | 1 | 0 | Пройден | Параметр Monaco |
| scrollbar.scrollByPage | Прокручивать страницами при нажатии на полосу | 1 | 0 | Пройден | Параметр Monaco |
| scrollbar.ignoreHorizontalScrollbarInContentHeight | Не учитывать горизонтальную полосу в высоте текста | 1 | 0 | Пройден | Параметр Monaco |
| wordWrap | Перенос длинных строк | 3 | 0 | Пройден | Параметр Monaco |
| wordWrapColumn | Колонка переноса | 1 | 2 | Пройден | Параметр Monaco |
| wrappingIndent | Отступ перенесённых частей | 3 | 0 | Пройден | Параметр Monaco |
| wrappingStrategy | Способ расчёта переноса | 1 | 0 | Пройден | Параметр Monaco |
| wrapOnEscapedLineFeeds | Перенос на записи обратная косая черта и n | 1 | 0 | Пройден | Параметр Monaco |
| wordBreak | Разбиение слов при переносе | 1 | 0 | Пройден | Параметр Monaco |
| wordWrapBreakBeforeCharacters | Перенос перед символами | 1 | 1 | Пройден | Параметр Monaco |
| wordWrapBreakAfterCharacters | Перенос после символов | 1 | 1 | Пройден | Параметр Monaco |
| cursorStyle | Вид курсора | 5 | 0 | Пройден | Параметр Monaco |
| cursorBlinking | Мигание курсора | 4 | 0 | Пройден | Параметр Monaco |
| cursorSmoothCaretAnimation | Плавное перемещение курсора | 2 | 0 | Пройден | Параметр Monaco |
| cursorWidth | Толщина курсора | 1 | 2 | Пройден | Параметр Monaco |
| cursorHeight | Высота курсора | 1 | 2 | Пройден | Параметр Monaco |
| overtypeCursorStyle | Курсор режима замещения | 5 | 0 | Пройден | Параметр Monaco |
| cursorSurroundingLines | Запас строк вокруг курсора | 1 | 2 | Пройден | Параметр Monaco |
| cursorSurroundingLinesStyle | Когда сохранять запас строк | 1 | 0 | Пройден | Параметр Monaco |
| roundedSelection | Скруглённое выделение | 1 | 0 | Пройден | Параметр Monaco |
| columnSelection | Прямоугольное выделение | 1 | 0 | Пройден | Параметр Monaco |
| multiCursorModifier | Клавиша нескольких курсоров | 1 | 0 | Пройден | Параметр Monaco |
| multiCursorMergeOverlapping | Объединять пересекающиеся курсоры | 1 | 0 | Пройден | Параметр Monaco |
| multiCursorPaste | Вставка при нескольких курсорах | 1 | 0 | Пройден | Параметр Monaco |
| multiCursorLimit | Предел числа курсоров | 1 | 2 | Пройден | Параметр Monaco |
| selectionHighlight | Подсвечивать совпадения выделенного текста | 1 | 0 | Пройден | Параметр Monaco |
| selectionHighlightMultiline | Совпадения многострочного выделения | 1 | 0 | Пройден | Параметр Monaco |
| selectionHighlightMaxLength | Предел длины для подсветки совпадений | 1 | 2 | Пройден | Параметр Monaco |
| occurrencesHighlight | Подсвечивать использования имени | 2 | 0 | Пройден | Нужен языковой провайдер |
| occurrencesHighlightDelay | Задержка подсветки использований | 1 | 2 | Пройден | Нужен языковой провайдер |
| doubleClickSelectsBlock | Выделять содержимое пары двойным щелчком | 1 | 0 | Пройден | Параметр Monaco |
| smartSelect.selectLeadingAndTrailingWhitespace | Включать крайние пробелы в умное выделение | 1 | 0 | Пройден | Параметр Monaco |
| smartSelect.selectSubwords | Выделять части составных имён | 1 | 0 | Пройден | Параметр Monaco |
| wordSeparators | Разделители слов | 1 | 1 | Пройден | Параметр Monaco |
| wordSegmenterLocales | Языки разделения на слова | 1 | 1 | Пройден | Параметр Monaco |
| mouseStyle | Указатель мыши над кодом | 2 | 0 | Пройден | Параметр Monaco |
| mouseMiddleClickAction | Действие средней кнопки мыши | 2 | 0 | Пройден | Параметр Monaco |
| folding | Сворачивать блоки кода | 1 | 0 | Пройден | Параметр Monaco |
| foldingStrategy | Определение сворачиваемых блоков | 1 | 0 | Пройден | Параметр Monaco |
| foldingHighlight | Подсвечивать свёрнутые блоки | 1 | 0 | Пройден | Параметр Monaco |
| foldingImportsByDefault | Сворачивать импорты по умолчанию | 1 | 0 | Пройден | Параметр Monaco |
| foldingMaximumRegions | Предел сворачиваемых областей | 1 | 2 | Пройден | Параметр Monaco |
| showFoldingControls | Показывать кнопки сворачивания | 2 | 0 | Пройден | Параметр Monaco |
| unfoldOnClickAfterEndOfLine | Раскрывать нажатием после конца строки | 1 | 0 | Пройден | Параметр Monaco |
| matchBrackets | Подсвечивать парные скобки | 2 | 0 | Пройден | Параметр Monaco |
| bracketPairColorization.enabled | Разные цвета вложенных скобок | 1 | 0 | Пройден | Параметр Monaco |
| bracketPairColorization.independentColorPoolPerBracketType | Отдельные цвета для каждого вида скобок | 1 | 0 | Пройден | Параметр Monaco |
| guides.indentation | Направляющие отступов | 1 | 0 | Пройден | Параметр Monaco |
| guides.highlightActiveIndentation | Выделять текущий отступ | 2 | 0 | Пройден | Параметр Monaco |
| guides.bracketPairs | Направляющие пар скобок | 2 | 0 | Пройден | Параметр Monaco |
| guides.bracketPairsHorizontal | Горизонтальные направляющие скобок | 2 | 0 | Пройден | Параметр Monaco |
| guides.highlightActiveBracketPair | Выделять текущую пару скобок | 1 | 0 | Пройден | Параметр Monaco |
| links | Открываемые ссылки в тексте | 1 | 0 | Пройден | Нужен языковой провайдер |
| definitionLinkOpensInPeek | Определение во встроенной карточке | 1 | 0 | Пройден | Нужен языковой провайдер |
| peekWidgetDefaultFocus | Фокус карточки определения | 1 | 0 | Пройден | Нужен языковой провайдер |
| stablePeek | Не закрывать карточку определений автоматически | 1 | 0 | Пройден | Нужен языковой провайдер |
| codeLens | Показывать команды над кодом | 1 | 0 | Пройден | Нужен языковой провайдер |
| codeLensFontFamily | Шрифт команд над кодом | 1 | 1 | Пройден | Нужен языковой провайдер |
| codeLensFontSize | Размер команд над кодом | 1 | 2 | Пройден | Нужен языковой провайдер |
| lightbulb.enabled | Значок исправлений | 2 | 0 | Пройден | Нужен языковой провайдер |
| inlayHints.enabled | Встроенные пояснения в коде | 3 | 0 | Пройден | Нужен языковой провайдер |
| inlayHints.fontSize | Размер встроенных пояснений | 1 | 2 | Пройден | Нужен языковой провайдер |
| inlayHints.fontFamily | Шрифт встроенных пояснений | 1 | 1 | Пройден | Нужен языковой провайдер |
| inlayHints.padding | Поля встроенных пояснений | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlayHints.maximumLength | Предел длины встроенных пояснений | 1 | 2 | Пройден | Нужен языковой провайдер |
| inlineSuggest.enabled | Продолжения внутри строки | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.mode | Сопоставление продолжений | 2 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.showToolbar | Панель продолжений | 2 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.syntaxHighlightingEnabled | Подсветка предварительного продолжения | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.suppressSuggestions | Скрывать список при продолжении | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.minShowDelay | Задержка продолжений | 1 | 2 | Пройден | Нужен языковой провайдер |
| inlineSuggest.suppressInSnippetMode | Скрывать продолжения внутри заготовки | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.keepOnBlur | Сохранять продолжение без фокуса | 1 | 0 | Пройден | Нужен языковой провайдер |
| inlineSuggest.fontFamily | Шрифт продолжений | 1 | 1 | Пройден | Нужен языковой провайдер |
| find.cursorMoveOnType | Переход к результату во время поиска | 1 | 0 | Пройден | Параметр Monaco |
| find.findOnType | Поиск по мере ввода | 1 | 0 | Пройден | Параметр Monaco |
| find.seedSearchStringFromSelection | Брать запрос из выделения | 2 | 0 | Пройден | Параметр Monaco |
| find.autoFindInSelection | Автоматический поиск в выделении | 2 | 0 | Пройден | Параметр Monaco |
| find.addExtraSpaceOnTop | Место для строки поиска сверху | 1 | 0 | Пройден | Параметр Monaco |
| find.loop | Циклический поиск | 1 | 0 | Пройден | Параметр Monaco |
| find.closeOnResult | Закрывать поиск после перехода | 1 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.nonBasicASCII | Выделять символы вне ASCII | 2 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.invisibleCharacters | Выделять невидимые символы | 1 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.ambiguousCharacters | Выделять похожие символы | 1 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.includeComments | Проверять символы в комментариях | 2 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.includeStrings | Проверять символы в строках | 2 | 0 | Пройден | Параметр Monaco |
| unicodeHighlight.allowedCharacters | Разрешённые символы | 1 | 1 | Пройден | Параметр Monaco |
| unicodeHighlight.allowedLocales | Разрешённые языки символов | 1 | 1 | Пройден | Параметр Monaco |
| accessibilitySupport | Поддержка чтения с экрана | 2 | 0 | Пройден | Параметр Monaco |
| accessibilityPageSize | Строк для чтения с экрана | 1 | 2 | Пройден | Параметр Monaco |
| screenReaderAnnounceInlineSuggestion | Озвучивать предварительное продолжение | 1 | 0 | Пройден | Параметр Monaco |
| inlineCompletionsAccessibilityVerbose | Подробные сообщения о продолжениях | 1 | 0 | Пройден | Параметр Monaco |
| tabFocusMode | Tab переводит фокус вместо отступа | 1 | 0 | Пройден | Параметр Monaco |
| largeFileOptimizations | Оптимизация больших файлов | 1 | 0 | Пройден | Параметр Monaco |
| maxTokenizationLineLength | Предел длины строки для подсветки | 1 | 2 | Пройден | Параметр Monaco |
| stopRenderingLineAfter | Предел отображения длинной строки | 1 | 2 | Пройден | Параметр Monaco |
| unusualLineTerminators | Необычные разделители строк | 2 | 0 | Пройден | Параметр Monaco |
| gotoLocation.multiple | Общее действие при нескольких целях перехода | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleDefinitions | Несколько определений | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleTypeDefinitions | Несколько определений типа | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleDeclarations | Несколько объявлений | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleImplementations | Несколько реализаций | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleReferences | Несколько использований | 2 | 0 | Пройден | Нужен языковой провайдер |
| gotoLocation.multipleTests | Несколько связанных проверок | 2 | 0 | Пройден | Нужен языковой провайдер |


## Уточнение границы прежней проверки — 2026-10-14 19:14

Этот отчёт фиксирует проверку параметров Monaco, сохранения профиля и отдельных действий. Метка «Пройден» в старой таблице не означает, что для каждого пункта был наблюдён самостоятельный результат в редакторе. Для новой поэлементной проверки штатной IDE без тестовых провайдеров используется отдельный отчёт [EDITOR_SETTINGS_LIVE_AUDIT.md](EDITOR_SETTINGS_LIVE_AUDIT.md).
