# План работ 4

Этот файл является активным планом проекта после перевода `docs/PLANS3.md` в исторический статус.
Новые записи добавляются только в конец файла.

## Текущий этап

- Текущий продуктовый фокус: modern graphics поверх `CANVAS`.
- Базовый оператор `VIEWPORT` уже реализован, задокументирован и покрыт тестами.
- Интеграционный пример `samples/graphics/game/maze_chase.axm` уже перенесён на `VIEWPORT` без изменения игровой логики.

## Ближайшие задачи

1. Провести ручную приёмку `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, чтобы подтвердить живую картинку мира, HUD и калибровочного overlay уже по новому viewport-договору.
2. После ручной приёмки выбрать следующий современный графический шаг не абстрактно, а по следующей реальной боли из `maze_chase`.
3. Если отдельный следующий оператор пока не нужен, рассматривать точечные улучшения уже существующего modern-graphics слоя только там, где они явно упрощают флагманский игровой пример.

## Журнал плана

### 2026-06-07 - выбран следующий modern-graphics шаг

- Следующим содержательным оператором выбран `VIEWPORT` для `CANVAS`.
- Основание выбора: `maze_chase` вручную компенсировал отсутствие прямоугольной области мира через `BACKGROUND_OFFSET_Y` и самодельные смещения.

### 2026-06-07 - договор VIEWPORT записан в спецификацию

- В `docs/SPECIFICATION3.md` зафиксированы формы `VIEWPORT` и `VIEWPORT (left, top)-(right, bottom)`.
- Зафиксирована связь с `CAMERA`, `RESETCAMERA` и правило обрезки modern-graphics операторов по viewport.

### 2026-06-07 - VIEWPORT доведён до рабочего состояния

- Языковой, runtime-, bytecode-, IDE- и manual-срез `VIEWPORT` завершён.
- Добавлен runnable-пример `samples/graphics/modern/viewport_hud.axm`.
- Следующим практическим шагом после закрытия `VIEWPORT` признан перенос `samples/graphics/game/maze_chase.axm` на новый договор.

### 2026-06-07 - страница modern graphics восстановлена

- `docs/manual/modern-graphics.html` восстановлена после повреждения `NUL`-байтами.
- Активный план проекта не изменился: после починки manual работа возвращена к переносу `maze_chase` на `VIEWPORT`.

### 2026-06-07 - maze_chase переведён на VIEWPORT и проверен acceptance-тестами

- `samples/graphics/game/maze_chase.axm` теперь рисует мир в мировых координатах от `(0, 0)`, а экранное размещение мира делает `VIEWPORT`.
- HUD и калибровочная боковая панель снова рисуются во всём `CANVAS` после `VIEWPORT` без аргументов.
- Для байткодного пути исправлен `VIEWPORT`, чтобы он принимал обычные целочисленные выражения `INTEGER|LONG`, как и остальной modern-graphics слой.
- Тестовый acceptance-хост синхронизирован с modern `CANVAS`-viewport, а ожидания `maze_chase` обновлены под новый координатный договор.
- Следующий точный шаг: ручная приёмка `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и выбор следующего современного графического шага по её результату.

### 2026-06-07 - починена проекция мира внутрь VIEWPORT для CANVAS

- У `ProgramConsoleService` найден дефект modern-отрисовки: `VIEWPORT` для `CANVAS` только обрезал вывод, но не добавлял экранное смещение viewport после `CAMERA`.
- Починка перевела `DRAWIMAGE`, `DRAWSPRITE`, `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWTEXT`, `DRAWCIRCLE` и `FILLCIRCLE` на договор `viewport origin + (world - camera)`.
- Следующий точный шаг не меняется по сути, но уточняется по содержанию: нужно заново открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и подтвердить, что теперь видно всё игровое поле без узкой мигающей полосы.

### 2026-06-07 - убрано мигание CANVAS-кадра между BEGINFRAME и PRESENT

- У `ProgramConsoleService` найден второй независимый дефект modern-отрисовки: во время активного кадра `BEGINFRAME ... PRESENT` коалесцированная публикация могла показать очищенный или частично дорисованный рабочий буфер.
- Починка перевела `CANVAS` на более жёсткий кадровый договор: пока кадр открыт, промежуточные auto-refresh-публикации блокируются, а видимый снимок меняется только на `PRESENT`.
- Следующий точный шаг теперь уже чисто приёмочный: заново открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и подтвердить, что исчезло не только обрезание поля, но и мигание мира вместе с HUD.

### 2026-06-07 - починён запуск IDE после повреждения ресурсов подсветки

- Источник падения `Axiom.Ide` найден не в `StaticExtension`, а в загрузке AvalonEdit-подсветки: оба файла `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd` и `Axiom.Dark.xshd` были повреждены `NUL`-байтами.
- Ресурсы подсветки восстановлены, в них сохранён `VIEWPORT`, а правило для шестнадцатеричных литералов `&H...` добавлено обратно.
- Следующий точный шаг по проекту не меняется: после починки запуска нужно вручную открыть `Axiom.Ide` и провести живую приёмку `samples/graphics/game/maze_chase.axm`.

### 2026-06-07 - после VIEWPORT начат перенос части SCREEN-базы в CANVAS

- После ручной приёмки `maze_chase` шаг `VIEWPORT` считается закрытым и больше не является ближайшим техническим долгом.
- Следующий этап зафиксирован не как новый оператор, а как точечный перенос в `CANVAS` только тех исторических форм, которые реально нужны поверх уже готового modern-слоя.
- Очередь этого этапа записана в `docs/MODERN_GRAPHICS_PLAN.md`: сначала `POINT(x, y)`, затем `PSET` / `PRESET`, потом `PAINT`, затем `GET` / `PUT`.
- В эту волну сознательно не входят `VIEW`, `WINDOW`, `PMAP`, `VIEW PRINT`, `PALETTE`, `PCOPY` и экранные страницы.
- Первый шаг уже закрыт: `POINT(x, y)` теперь работает и при активном `CANVAS`, а `POINT(selector)` остаётся screen-only формой.
- Второй шаг теперь закрыт полностью: `PSET` и `PRESET` работают и при активном `CANVAS`, пишут в мировую точку под `CAMERA` и `VIEWPORT`, а старое поведение в `SCREEN` не меняется.
- Следующий точный шаг: перенести в `CANVAS` операторы `GET` / `PUT`.

### 2026-06-08 - базовая волна переноса в CANVAS закрыта

- `GET` и `PUT` перенесены в `CANVAS` и тем самым закрыли базовую выборочную волну `POINT`, `PSET`, `PRESET`, `PAINT`, `GET`, `PUT`.
- Перенос завершён без втягивания в `CANVAS` старых договоров `VIEW`, `WINDOW`, `PMAP`, `PALETTE`, `PCOPY` и экранных страниц.
- Следующий точный шаг больше не задаётся списком старых операторов. Его нужно выбирать уже по следующему практическому пробелу modern graphics или по реальной боли флагманских примеров.

### 2026-06-08 - мышь перенесена в CANVAS

- `MOUSEX`, `MOUSEY`, `MOUSEBUTTON`, `MOUSEPRESS` и `ON MOUSE` теперь работают и при активном `CANVAS`.
- Для `CANVAS` зафиксирован новый координатный договор: `MOUSEX()` / `MOUSEY()` и `x/y` обработчика `ON MOUSE` возвращают мировые координаты под текущими `VIEWPORT` и `CAMERA`.
- Runnable-примеры по мыши доведены до наглядного ручного формата: при движении мыши в `Axiom.Ide` координаты теперь видны прямо на холсте, а не только через итоговый `PRINT`.
- Следующий точный шаг: выбирать уже не перенос старого screen-оператора, а следующий реальный modern-слой. Ближайший кандидат по долгому плану: тайловая карта поверх уже закрытых `CANVAS`, `VIEWPORT`, камеры, индексных helper-операций и мыши.

### 2026-06-08 - ручная проверка mouse sample уточнена

- `samples/operators/on-mouse/canvas_world_handler.axm` и соседние canvas-примеры координат укрупнены до `CANVAS 480, 270`, чтобы HUD и строка удержания кнопок помещались в окне.
- Native-окно `CANVAS` теперь корректно снимает удержание кнопки при отпускании за пределами логического framebuffer и не снимает захват мыши, пока удерживается другая кнопка.
- Следующий точный шаг не меняется: после стабилизации мыши выбирать следующий слой modern graphics поверх уже готовой базы.

### 2026-06-08 - отдельный договор тайловой карты зафиксирован

- Полный договор следующего слоя современной графики вынесен в отдельный документ `docs/TILEMAP_CONTRACT.md`.
- Договор прямо опирается на модель Tiled, способ рисования в духе MonoGame/pygame и BASIC-стиль публичных имён Axiom.
- Следующий технический шаг теперь выбран не абстрактно, а по этому договору: сначала `TILESET` / `LOADTILESET` / `DRAWTILE`, затем `TILEMAP` / `LOADTILEMAP` / `DRAWTILEMAP`.

### 2026-06-08 - первый срез TILESET закрыт

- Срез `TILESET` / `LOADTILESET` / `DRAWTILE` и функции `TILECOUNT`, `TILEWIDTH`, `TILEHEIGHT` уже реализованы и теперь синхронизированы с manual, активной спецификацией и реестром покрытия `BytecodeVm`.
- Отдельно зафиксирован пользовательский договор первого тайлового шага: `DRAWTILE` работает только при активном `CANVAS`, `tileIndex` является `1`-основанным, а пустой `TILESET` остаётся допустимым значением переменной для size-функций.
- Следующий точный шаг по `docs/TILEMAP_CONTRACT.md`: перейти к tilemap-слою `TILEMAP` / `LOADTILEMAP` / `DRAWTILEMAP` / `DRAWTILELAYER`, не возвращаясь к уже закрытому тайлсет-срезу без новой отдельной причины.

### 2026-06-08 - IDE-подсветка новых языковых сущностей стала обязательной

- Для tile-слоя исправлена IDE-раскраска: `TILESET`, `LOADTILESET`, `TILECOUNT`, `TILEWIDTH`, `TILEHEIGHT`, `DRAWTILE` добавлены в светлую и тёмную схемы подсветки, а также в IDE-тесты.
- В `AGENTS.md` зафиксировано постоянное правило: новый оператор, встроенная функция, тип или другой пользовательский токен не считается завершённым, пока не добавлен в `Axiom.Ide`-подсветку и не покрыт проверками `tests/Axiom.Ide.Tests/`.
- Следующий точный шаг не меняется: переходить к `TILEMAP` / `LOADTILEMAP` / `DRAWTILEMAP` / `DRAWTILELAYER` и в том же наборе правок сразу добавлять им IDE-подсветку.

### 2026-06-08 - IDE-подсветка TILEMAP закрыта

- В `Axiom.Ide` добавлены слова `TILEMAP`, `DRAWTILEMAP`, `DRAWTILELAYER`, `LOADTILEMAP`, `MAPWIDTH`, `MAPHEIGHT`, `MAPPIXELWIDTH`, `MAPPIXELHEIGHT`, `MAPTILEWIDTH`, `MAPTILEHEIGHT`, `MAPLAYERCOUNT`.
- Светлая и тёмная схемы подсветки, словарь IDE и форматирование строк теперь синхронно распознают первый `TILEMAP`-слой.
- Следующий точный шаг по `docs/TILEMAP_CONTRACT.md`: довести исполнение `LOADTILEMAP`, `DRAWTILEMAP`, `DRAWTILELAYER` и `MAP*` в interpreter и `BytecodeVm`, затем закрыть runtime-тесты.

### 2026-06-08 - первый срез TILEMAP исполнен

- `TILEMAP`, `LOADTILEMAP`, функции `MAPWIDTH`, `MAPHEIGHT`, `MAPPIXELWIDTH`, `MAPPIXELHEIGHT`, `MAPTILEWIDTH`, `MAPTILEHEIGHT`, `MAPLAYERCOUNT`, операторы `DRAWTILEMAP` и `DRAWTILELAYER` теперь доведены до interpreter, `BytecodeVm`, parser/binder-тестов, runtime-тестов, сквозного runnable sample и manual.
- Подсветка IDE для этого набора была закрыта раньше и остаётся синхронизированной: новых публичных имён в этом шаге не добавлялось.
- Следующий точный шаг по `docs/TILEMAP_CONTRACT.md`: переходить к чтению клеток карты `MAPTILE`, `MAPTILESET`, `MAPTILEFLAGS` и записи `SETMAPTILE`, включая IDE-подсветку, runtime/VM-паритет, sample и manual в одном наборе правок.

### 2026-06-08 - чтение и запись клеток TILEMAP исполнены

- `MAPTILE`, `MAPTILESET`, `MAPTILEFLAGS` и `SETMAPTILE` доведены до parser, binder, interpreter, `BytecodeVm`, IDE-подсветки, тестов, runnable sample и manual.
- Функции чтения возвращают `0` для пустой клетки, пустой карты, объектного слоя или координат вне карты; оператор `SETMAPTILE` пишет только существующую клетку тайлового слоя и очищает её формой `0, 0`.
- Следующий точный шаг по `docs/TILEMAP_CONTRACT.md`: перейти к свойствам карты, слоёв и тайлов, не расширяя уже закрытый срез чтения/записи клеток без отдельной причины.

### 2026-08-18 - архитектурный анализ и план улучшений

- Проведён полный архитектурный анализ проекта (параллельно 6 направлений).
- Создан `docs/IMPROVEMENT_PLAN.md` с планом по 5 волнам.
- Выполнены волны 1 (безопасность ресурсов), 4.1 (документы), 3.2 (диагностические коды) и 2.1 (прототип общего built-in слоя).
- Следующий шаг: волна 3 плана — улучшение архитектуры (разбить MainWindowViewModel, создать Axiom.Syntax.Tests, разбить InterpreterTests.cs).

### 2026-08-18 - вынос Regex в общий слой исполнен

- `Interpreter.Builtins.Regex.cs` (354 → 148 строк) делегирует в `BuiltinRegexHelpers` для всех 8 функций: `IsMatch`, `Match`, `Extract`, `Matches`, `ExtractAll`, `Split`, `Replace`, `Count`.
- Удалены дублирующиеся: `RegexExecutionLimit`, `ParseRegexModes`, `ReportInvalidRegexMode`, `ReportRegexTimeout`, `ReplaceRegexMatches`.
- Следующий шаг: продолжить вынос built-in функций в общий слой — следующая область String, затем FileSystem, DateTime, Array.

### 2026-08-18 - вынос FileSystem в общий слой исполнен (BytecodeVm)

- `BytecodeVm.Builtins.FileSystem.cs` (1011 → 782 строк, −229) делегирует в `BuiltinFileSystemHelpers` для всех10 дублированных статических методов и3 полей кодировок.
- Удалены дублирующиеся: `NormalizeFileSystemPath`, `ReadTextFileWithFallback`, `StripUtf8Bom`, `CreateWindows1251TextEncoding`, `ReadDecodedLines`, `CopyDirectory`, `GetDirectorySize`, `CreateTemporaryFile`, `CreateTemporaryDirectory`, `IsFileSystemException`.
- Ссылки обновлены в `BytecodeVm.cs` и `BytecodeVm.FileIO.cs`.
- Для валидационных методов `EnsureFileExists` и `EnsureDirectoryExists` созданы тонкие обёртки с делегатом `ReportBuiltInArgumentError`.
- Следующий шаг: продолжить вынос built-in функций в общий слой — следующая область DateTime или повторить аналогичный вынос для `Interpreter.Builtins.FileSystem.cs`.

### 2026-08-18 - вынос Array в общий слой исполнен (BytecodeVm)

- `BytecodeVm.Builtins.Array.cs` (1958 → 1574 строк, −384) делегирует в `BuiltinArrayHelpers` для 23 дублированных методов (18 статических + 5 экземплярных).
- Сохранены BytecodeVm-специфичные: `CopyOneDimensionalArrayRange`, `CreateCompactArrayValue`, `CreateArrayValue`, `CloneArrayValue`, все `Evaluate*Builtin`, все валидационные методы с `TextLocation`.
- Исправлен дефект `BuiltinArrayHelpers.ValuesEqual` (fallback на `false` вместо несуществующего `Storage`).
- Следующий шаг: аналогичный вынос Array для `Interpreter.Builtins.Array.cs`, затем закрытие общего слоя built-in функций.

### 2026-08-18 - вынос Array в общий слой исполнен (Interpreter)

- `Interpreter.Builtins.Array.cs` (1492 → 1368 строк, −124) делегирует в `BuiltinArrayHelpers` для алгоритмов сортировки и бинарного поиска через `Func<RuntimeValue, RuntimeValue, int>` делегат.
- Добавлены в `BuiltinArrayHelpers`: `SortArrayAlongDimension`, `GetStableSortedLine`, `FindArrayBinarySearchIndex` (2 перегрузки), `FindArrayBinarySearchCoordinateSet`.
- `BytecodeVm.Builtins.Array.cs` (1574 → 1416 строк, −158) аналогично делегирует в `BuiltinArrayHelpers` с лямбдой захвата `TextLocation`.
- Суммарный эффект общего слоя built-in: −2,870 строк.
- Следующий шаг: волна 3 плана — улучшение архитектуры (разбить MainWindowViewModel, создать Axiom.Syntax.Tests, разбить InterpreterTests.cs).

### 2026-08-18 - Axiom.Syntax.Tests создан

- Создан `tests/Axiom.Syntax.Tests/` с 64 тестами: SyntaxToken, Expression, Statement, Declaration, SyntaxKind.
- Проект добавлен в `Axiom.sln`.

### 2026-08-18 - InterpreterTests разбит на partial-файлы

- `InterpreterTests.cs` (12,015 строк) разбит на 11 partial-файлов по тематике.
- Итого: 489 тестов, 875/875 пройдено, кодировка кириллицы сохранена.
- Волна 3.3 (Axiom.Syntax.Tests) и 3.4 (разбить InterpreterTests) завершены.
- Следующий шаг: 3.1 (разбить MainWindowViewModel) или продуктовая работа.
