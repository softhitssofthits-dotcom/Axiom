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

### 2026-08-18 - MainWindowViewModel разбит на partial-файлы

- `MainWindowViewModel.cs` (3,354 строк) разбит на 5 partial-файлов: основной (1,380), Debug (807), FileTree (754), Preferences (323), EventLog (129).
- Волна 3.1 завершена. Волна 3 плана улучшений полностью закрыта.
- Следующий шаг: продуктовая работа или волна 5 (тестирование графики).

### 2026-08-18 - графическая baseline-инфраструктура тестирования

- Создан `tests/Axiom.Runtime.Tests/Helpers/SoftwareFramebuffer.cs` с методами `ToBmp`, `CompareBmp`, `ComparePixels`.
- Создан `tests/Axiom.Runtime.Tests/GraphicsBaselineTests.cs` с 9 baseline-тестами графики.
- Покрыты: LINE (horizontal, diagonal), RECT (fill, box), CIRCLE, PSET, CLS, FILLRECT, VIEWPORT clipping.
- 884/884 тестов пройдено.
- Следующий шаг: расширять графические baseline-тесты (DRAW, PAINT, WINDOW, GET/PUT) или продуктовая работа.

### 2026-08-18 - forensic audit и recovery roadmap

- Проведён полный forensic audit проекта (28 проходов, 29 разделов).
- Создан `docs/AUDIT_REPORT.md` с полным отчётом.
- Создан `docs/RECOVERY_ROADMAP.md` с планом из 20 задач по 6 фазам.
- Project Health Score: 7.5/10.
- Следующие задачи по приоритету:
  1. [P1] TASK-001 — Fix String builtins divergence (BytecodeVm → BuiltinStringHelpers)
  2. [P1] TASK-002 — Split MainWindow.xaml.cs (1,550+ → 4 partial files)
  3. [P2] TASK-003 — Update ARCHITECTURE.md dependency rules
  4. [P2] TASK-004 — Fix hanging Execution/IDE tests
  5. [P2] TASK-005 — Add execution timeout
- Следующий шаг: реализация TASK-001.

### 2026-08-19 - TASK-001 и TASK-002 завершены

- TASK-001: String builtins divergence исправлен. BytecodeVm.Builtins.String.cs (454 → 243 строки, −211). Все строковые операции делегируют BuiltinStringHelpers.
- TASK-002: MainWindow.xaml.cs разбит (1,713 → 717 строк). Созданы MainWindow.Editor.cs (508), MainWindow.Diagnostics.cs (237), MainWindow.Output.cs (233).
- Суммарный эффект: −205 строк дублирования + −996 строк в MainWindow.xaml.cs.
- Build: 0 errors, 0 warnings. Тесты: 1,671/1,671 non-IDE pass.
- Следующий шаг: TASK-003 (Update ARCHITECTURE.md) или TASK-008 (Fix CanConvert Decimal→Double).

### 2026-08-19 - TASK-003, TASK-005, TASK-008 завершены

- TASK-003: ARCHITECTURE.md обновлён — Bytecode в правилах зависимостей, описание слоя добавлено.
- TASK-005: Execution timeout реализован — `TimeoutCancellationProvider`, `ExecutionRequest.CancellationProvider`, `ProgramRunService.Run(timeout)`.
- TASK-008: CanConvert Decimal→Double исправлен, тест обновлён.
- TASK-004: Hanging tests — корень: интерактивные samples в acceptance suite. Deferred.
- TASK-006: Environment.CurrentDirectory — защищён shell lock. Deferred.
- TASK-009: ArrayAccessExpressionSyntax — verified dead code. Deferred.
- TASK-010: Latent opcodes — verified. Deferred.
- Recovery roadmap: 5/9 задач завершены, 4 deferred с обоснованием.
- Build: 0 errors, 0 warnings. Тесты: 1,737/1,738 non-IDE pass.
- Следующий шаг: фильтрация интерактивных samples или продуктовая работа.

### 2026-08-18 - Forensic audit Basic Language Core и исправление CORE-001

- Проведён forensic audit фундаментального слоя языка (Health Score: 8.2/10).
- Исправлен CORE-001: `checked` на `\` и `MOD` в `TryEvaluateGlobalIntegralConstantCore`.
- Добавлены тесты: циклические CONST, деление на ноль в CONST, сравнения (INTEGER/LONG/DOUBLE/STRING).
- Build: 0 errors. Тесты: 368/368 + 886/886.
- Recovery roadmap аудита:
  1. [P1] CORE-001 — done
  2. [P2] CORE-002 — вынести общие хелперы из Interpreter/BytecodeVm
  3. [P2] CORE-003 — консолидировать ValuesEqual (3 копии → 1)
  4. [P3] CORE-004 — задокументировать отсутствие short-circuit
  5. [P3] CORE-005 — задокументировать отсутствие Decimal-литералов
  6. [P3] CORE-006 — задокументировать отсутствие научной нотации
- Следующий шаг: CORE-002/003 (вынос общих хелперов) или продуктовая работа.

### 2026-08-19 - Recovery roadmap аудита: CORE-003, CORE-009, документация

- CORE-003: ValuesEqual/Compare/RecordsEqual консолидированы в `RuntimeValueHelpers` (-198 строк дублирования).
- CORE-009: Валидация shift count 0..63 в обоих движках + hot/fast paths.
- CORE-004/005/006/010: Документация обновлена (manual: short-circuit, Decimal, научная нотация, hex Long).
- Build: 0 errors. Тесты: 368 +886 +137 =1,391.
- Recovery roadmap: 9/10 пунктов закрыты. CORE-002 частично (overflow helpers — отдельная задача с delegate pattern).
- Следующий шаг: CORE-002 remainder (overflow helpers) или продуктовая работа.

### 2026-08-19 - CORE-002 завершён: overflow helpers вынесены

- `RuntimeArithmeticHelpers.cs` создан: EvaluateChecked*(4 метода) + Report*(2 метода) с delegate pattern.
- Interpreter и BytecodeVm делегируют в общий слой.
- Recovery roadmap аудита: **10/10 пунктов закрыты**.
- Build: 0 errors. Тесты: 368 +886 +137 +203 =1,594.
- Суммарный эффект выноса: ~-3,200 строк дублирования за все сессии.
- Следующий шаг: продуктовая работа или GitHub deploy.

### 2026-08-19 - реализован оператор CONTINUE

- Добавлен оператор `CONTINUE FOR` / `CONTINUE WHILE` / `CONTINUE DO` во все слои: lexer, parser, semantics, lowering, interpreter, BytecodeVm.
- Добавлены диагностики MB2037, MB2038, MB2039 для контекстных ошибок.
- Добавлено26 новых тестов (parser, syntax, semantics, runtime, execution, lowering).
- Добавлены3 runnable samples: `continue_for.axm`, `continue_while.axm`, `continue_do.axm`.
- Обновлен manual: новая страница `continue.html`, обновлены `for.html`, `while.html`, `do-loop.html`, `loops.html`, `diagnostics.html`, `manual.js`, `search-index.js`.
- Обновлена IDE-подсветка обеих тем.
- Build: 0 errors. Тесты: 143 +374 +891 +206 =1,614 (+26 новых).
- Следующий шаг: обновить `docs/BYTECODE_COVERAGE.md` для записи CONTINUE, затем продолжить продуктовую работу.

### 2026-08-19 - глубокий forensic-аудит модели вызовов (SUB/FUNCTION/call-frame)

- Проведён полный forensic-аудит процедурной модели языка: SUB, FUNCTION, RETURN, параметры, аргументы, фреймы, замыкания, рекурсия, встроенные функции.
- Health Score: 8.7/10.
- Модель фундаментально корректна: двухпроходная регистрация, единый FunctionSymbol, изолированные фреймы, корректные замыкания, BYVAL/BYREF, паритет интерпретатора и VM.
- Выявлены находки:
  - CALL-001 [P2]: нет защиты от переполнения стека (StackOverflowException без диагностики)
  - CALL-003 [P2]: 5 негативных acceptance-сэмплов (MB2005, MB2006, MB2007, MB2019, MB2020)
  - CALL-004 [P3]: ограничение BYREF для function values не полностью документировано
  - CALL-005 [P3]: нет теста взаимной рекурсии
  - CALL-006 [P3]: нет runtime-теста для MB3008 (FUNCTION без RETURN)
- Следующий шаг: исправление находок по порядку приоритета.

## Ближайшие задачи по аудиту вызовов

1. [P2] CALL-001 — добавить ограничение глубины стека вызовов с диагностикой MB3xxx
2. [P2] CALL-003 — создать 5 негативных acceptance-сэмплов: unknown_callable, return_value_in_sub, missing_return_value, sub_name_as_value, calling_non_function
3. [P3] CALL-004 — расширить документацию BYREF-ограничения в function.html
4. [P3] CALL-005 — добавить тест взаимной рекурсии (IsEven/IsOdd)
5. [P3] CALL-006 — добавить runtime-тест для MB3008 (FUNCTION без RETURN на исполняемом пути)

### 2026-08-19 - Deep String forensic audit и исправление STRING-001/STRING-002

- Проведён полный forensic-аудит строковой подсистемы (111 разделов).
- Health Score: 8.4/10.
- Исправлен STRING-001 [P1]: REVERSE fast path Unicode surrogate pair corruption.
- Исправлен STRING-002 [P3]: JOIN single-element VM parity.
- Добавлены 2 теста. Тесты: 897/897.
- Создан `docs/STRING_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или глубокий аудит массивов.

### 2026-08-20 - Deep Array forensic audit и исправление ARRAY-001..004

- Проведён полный forensic-аудит массивной подсистемы (5 параллельных направления).
- Health Score: 8.8/10.
- Исправлен ARRAY-001 [P1]: ARRAYCONCAT 1D empty-array bounds parity.
- Исправлен ARRAY-002 [P1]: Compare error handling parity (Interpreter crash → diagnostic).
- Исправлен ARRAY-003 [P3]: ROW/COLUMN/PI IDE highlighting.
- Исправлен ARRAY-004 [P3]: Error message formatting parity (parentheses).
- Тесты: 897/897 Runtime, 374/374 Semantics.
- Создан `docs/ARRAY_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа.

### 2026-08-19 - Deep TYPE forensic audit и исправление TYPE-001..003

- Проведён полный forensic-аудит подсистемы TYPE (123 раздела).
- Health Score: 8.6/10.
- TYPE-001 [P3]: BytecodeVm type resolution linear scan → `BuildRecordTypeCache()` для O(1).
- TYPE-002 [P3]: 4 negative acceptance samples (cyclic, duplicate, unknown_field, invalid_type).
- TYPE-003 [P4]: Manual constructor overload resolution — уже задокументировано.
- Дополнительно: исправлен crash при дублированном TYPE (`DeclareTypeMethods` + `MethodsCompleted`).
- Build: 0 errors. Тесты: 897 + 374 + 143 + 51 = 1,465.
- Создан `docs/TYPE_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или INPUT/PRINT/DATA аудит.

### 2026-08-20 - Deep I/O forensic audit и исправление IO-005

- Проведён полный forensic audit I/O-подсистемы (112 раздела).
- Health Score: 8.5/10.
- IO-005 [P3]: I/O helpers вынесены в `BuiltinIOHelpers` (TryParseInputField, NormalizeInputField, CountInputFields).
- Build: 0 errors. Тесты: 897 + 149 = 1,046.
- Создан `docs/IO_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или реализация DATA/READ/RESTORE.

### 2026-08-20 - Deep File System / File I/O forensic audit

- Проведён полный forensic audit файловой подсистемы (135 раздела).
- Health Score: 8.8/10.
- READLINES duplication: вынесен в `BuiltinFileIOHelpers.EvaluateReadLines()`.
- Build: 0 errors. Тесты: 897/897.
- Создан `docs/FILE_SYSTEM_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или аудит системы ошибок (Error Handling, Level 10).

### 2026-08-19 - Deep Error Handling / Exceptions / Diagnostics forensic audit и исправление ERROR-003, ERROR-004

- Проведён полный forensic audit системы ошибок (135 раздела).
- Health Score: 8.5/10.
- ERROR-003 [P0]: `InvalidOperationException` из `BuiltinDateTimeHelpers` ловится и переводится в MB3007 (оба движка).
- ERROR-004 [P1]: `BytecodeEvaluationStack.Pop()` бросает `EvaluationStackUnderflowException`, VM переводит в MB3005.
- Создан `EvaluationStackUnderflowException.cs`.
- Build: 0 errors. Тесты: 897 + 94 + 21 = 1,012.
- Создан `docs/ERROR_HANDLING_AUDIT.md`.
- Следующий шаг: аудит модулей/namespaces/project structure (Level 11).

### 2026-08-19 - Deep Modules / Namespaces / Program Structure forensic audit

- Проведён полный forensic audit программной модели (130 раздела).
- Health Score: 8.5/10.
- **Axiom — строго однофайловый язык.** Программа = один `.axm` файл.
- Нет модулей, namespaces, imports, visibility modifiers, project file format.
- Все P2/P3 findings — feature gaps, не баги.
- Система стабильна для Bytecode/VM audit.
- Создан `docs/MODULES_AUDIT.md`.
- Следующий шаг: глубокий аудит Bytecode / VM (Level 12).

### 2026-08-19 - Deep Bytecode / VM Forensic + Performance Audit (Level 12) завершён

- Проведён полный forensic + performance аудит Bytecode VM по 10 фазам.
- Health Score: **8.4/10**.
- 48 benchmark scenarios: VM быстрее в 43/48 (90%), median speedup 0.63x.
- 1046/1046 тестов проходят (897 Runtime + 149 Execution Pipeline).
- Создан `docs/BYTECODE_VM_AUDIT_REPORT.md` с полным отчётом.
- Ключевые находки:
  - PERF-001 [P1]: RuntimeValue boxing — 2 unbox + 0-1 box per arithmetic op
  - PERF-002 [P2]: No constant folding in lowering
  - PERF-003 [P2]: Negate/Not нет fast path (closure alloc)
  - PERF-004 [P3]: I/O regression (1.05-1.19x)
- Топ-3 оптимизации:
  1. Negate/Not fast paths (low risk, immediate benefit)
  2. ReferenceEquals short-circuit in ValuesEqual (low risk)
  3. Constant folding in lowering (medium risk)
- Следующий шаг: Phase A quick-win оптимизации или продуктовая работа.

### 2026-08-19 - Phase A quick-win оптимизации реализованы

- OPT-001: Negate fast path — `TryEvaluateHotNegate()` без closure allocation
- OPT-002: Not fast path — `TryEvaluateHotNot()` без closure allocation
- OPT-003: ReferenceEquals short-circuit в `ValuesEqual()` и `Compare()`
- Результаты: string_concat -25%, scalar_assignment -17%, bitwise_shift -9%
- Build: 0 errors. Тесты: 897 + 149 = 1046/1046. Benchmark: 48/48 PASS.
- Следующий шаг: Phase B (constant folding, dead code elimination) или продуктовая работа.

### 2026-08-19 - Верификация benchmarks: регрессий нет

- Все 5 ранее зафиксированных «регрессий» оказались шумом коротких прогонов (3 итерации, 1 прогрев).
- На длинных прогонах (10 итераций, 3 прогрева): 46/48 VM быстрее, 2/48 паритет, 0/48 регрессий.
- Обновлены `docs/BYTECODE_VM_AUDIT_REPORT.md` и `docs/WORKLOG5.md`.
- Следующий шаг: продуктовая работа или Phase B оптимизации.

### 2026-08-19 - Phase B оптимизации реализованы

- B.1: Constant folding — `TryFoldUnary`, `TryFoldBinary`, `TryFoldConversion` в BoundProgramLowerer
- B.2: Dead code elimination — пропуск unreachable statements после Return/Jump
- B.3: Dead branch elimination — `IF TRUE/FALSE` → прямое выполнение нужной ветки
- Build: 0 errors. Тесты: 897 + 206 + 374 + 149 = 1626/1626. Benchmark: 48/48 PASS.
- Следующий шаг: Phase C (I/O fast paths) или продуктовая работа.

### 2026-08-20 - Phase C: inline fast paths для строковых built-in

- Добавлен `TryExecuteFastStringBuiltin` —10 inline fast paths: LEN, TRIM, LTRIM, RTRIM, LCASE, UCASE, CONTAINS, STARTSWITH, ENDSWITH, INSTR
- Build: 0 errors. Тесты: 897 + 149 = 1046/1046. Benchmark: 48/48 PASS.
- Следующий шаг: Phase D (type-specialized stacks) или продуктовая работа.

### 2026-08-20 - Phase D: integer cache expansion — не даёт эффекта, откачено

- Расширение кеша [-1,1024] → [-1,8192] не даёт значительного улучшения
- Откачено — кеш уже покрывал основной диапазон
- Для дальнейшего ускорения нужна глубокая архитектурная переработка (type-specialized stacks)
- Следующий шаг: продуктовая работа.

### 2026-08-20 - Глубокая архитектурная переработка VM запланирована (отложена)

- **OPT-PERF-ARCH-001**: Type-specialized stacks — параллельные `int[]`, `long[]`, `double[]`, `decimal[]`, `bool[]` стеки рядом с `RuntimeValue[]`
  - Цель: устранить boxing/unboxing для горячих арифметических операций
  - Ожидаемый эффект: дополнительные 20-40% на CPU-heavy сценариях
  - Сложность: высокая — затрагивает core VM loop, все арифметические/comparison opcodes, frame model
  - Статус: **ОТЛОЖЕНО** — требует отдельного проектирования
  - Документация: `docs/BYTECODE_VM_AUDIT_REPORT.md`, раздел20
- Следующий шаг: доработать отчёт аудита до полного формата (122 раздела).

### 2026-08-19 - Форензик-аудит IDE завершён, начато исправление находок

Проведён глубокий аудит интеграции IDE с языком (уровень 13). Документ: `docs/IDE_FORENSIC_AUDIT.md`.

**Исправлено (Session 1057):**
- **IDE-004** [P3]: Исправлена двойная классификация STRING в XSHD
- **IDE-005** [P3]: Добавлена подсветка операторов в оба XSHD
- **IDE-006** [P3]: Убраны COLUMN/ROW/PI из XSHD (не ключевые слова языка)
- **IDE-009** [P3]: Очистка диагностик при редактировании документа
- **IDE-001** [P2]: Устранено нарушение архитектуры SmartIdentifierService (инъекция парсинга)

**Запланировано (отложено):**
- **IDE-002** [P2]: Создать абстракцию отладчика вместо прямого использования BytecodeVmStopState — **ВЫПОЛНЕНО**
- **IDE-003** [P2]: Разделить ProgramConsoleService на сфокусированные сервисы — **ОТЛОЖЕНО**
- **IDE-008** [P2]: Добавить команду отмены выполнения программы — **ВЫПОЛНЕНО**
- **TASK-007** [P2]: Консолидировать источник ключевых слов (5 мест → 1) — **ВЫПОЛНЕНО** (центральный реестр + тест XSHD)

**Не трогать:**
- ProgramRunService — стабилен, хорошо протестирован
- AxiomExecutionPipeline — ядро pipeline, тщательно протестировано
- DocumentWorkspaceService — чистый, хорошо протестирован
