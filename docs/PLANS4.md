# План работ 4

Этот файл является активным планом проекта после перевода `docs/archive/plans/PLANS3.md` в исторический статус.
Новые записи добавляются только в конец файла.

## Текущий этап

- Текущий продуктовый фокус: modern graphics поверх `CANVAS`.
- Базовый оператор `VIEWPORT` уже реализован, задокументирован и покрыт тестами.
- Интеграционный пример `samples/graphics/game/maze_chase.axm` уже перенесён на `VIEWPORT` без изменения игровой логики.
- Параллельный инфраструктурный трек завершён (2026-10-05): по `docs/ELECTRON_IDE_PLAN.md` реализованы этапы 1-4 — headless-CLI `axiom` (`src/Axiom.Cli`), извлечение форматтера в `Axiom.Formatting`, Electron-IDE в `ide/` с e2e-сюитой (11/11 зелёные). WPF-IDE не изменена.

## Ближайшие задачи

1. Ручная приёмка Electron-IDE пользователем: запуск `run-ide.bat`, открытие реальной `.axm`-программы с `INPUT`, проверка живых диагностик и автоформата.
2. Провести ручную приёмку `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, чтобы подтвердить живую картинку мира, HUD и калибровочного overlay уже по новому viewport-договору.
3. После ручной приёмки выбрать следующий современный графический шаг не абстрактно, а по следующей реальной боли из `maze_chase`.
4. Если отдельный следующий оператор пока не нужен, рассматривать точечные улучшения уже существующего modern-graphics слоя только там, где они явно упрощают флагманский игровой пример.

Отложено (границы первой версии Electron-IDE, `docs/ELECTRON_IDE_PLAN.md` раздел 8):
- окно графики в Electron-IDE (сейчас графические программы дают понятную консольную диагностику);
- отладчик (брейкпоинты и шаги) в Electron-IDE через debug-протокол CLI;
- упаковка/инсталлятор (electron-builder) и CI-задача для e2e.

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
- Создан `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` с планом по 5 волнам.
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
- Создан `docs/archive/audits-legacy/AUDIT_REPORT.md` с полным отчётом.
- Создан `docs/archive/audits-legacy/RECOVERY_ROADMAP.md` с планом из 20 задач по 6 фазам.
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
- TASK-004: Hanging tests — фильтрация интерактивных samples из acceptance suite. DONE.
- TASK-006: Environment.CurrentDirectory — мутация устранена, NormalizeFileSystemPath принимает baseDirectory. DONE.
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

1. [P2] CALL-001 — DONE (добавлено ограничение глубины стека с MB3011)
2. [P2] CALL-003 — DONE (5 негативных acceptance-сэмплов созданы)
3. [P3] CALL-004 — DONE (документация BYREF-ограничения добавлена)
4. [P3] CALL-005 — DONE (тест взаимной рекурсии IsEven/IsOdd добавлен в InterpreterTests.Error.cs и BytecodeVmTests.cs)
5. [P3] CALL-006 — DONE (тест MB3006 для FUNCTION без RETURN добавлен, VM ловит на этапе валидации BC4054)

### 2026-08-19 - Deep String forensic audit и исправление STRING-001/STRING-002

- Проведён полный forensic-аудит строковой подсистемы (111 разделов).
- Health Score: 8.4/10.
- Исправлен STRING-001 [P1]: REVERSE fast path Unicode surrogate pair corruption.
- Исправлен STRING-002 [P3]: JOIN single-element VM parity.
- Добавлены 2 теста. Тесты: 897/897.
- Создан `docs/archive/audits-legacy/STRING_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или глубокий аудит массивов.

### 2026-08-20 - Deep Array forensic audit и исправление ARRAY-001..004

- Проведён полный forensic-аудит массивной подсистемы (5 параллельных направления).
- Health Score: 8.8/10.
- Исправлен ARRAY-001 [P1]: ARRAYCONCAT 1D empty-array bounds parity.
- Исправлен ARRAY-002 [P1]: Compare error handling parity (Interpreter crash → diagnostic).
- Исправлен ARRAY-003 [P3]: ROW/COLUMN/PI IDE highlighting.
- Исправлен ARRAY-004 [P3]: Error message formatting parity (parentheses).
- Тесты: 897/897 Runtime, 374/374 Semantics.
- Создан `docs/audit/ARRAY_SEMANTICS.md`.
- Следующий шаг: продуктовая работа.

### 2026-08-19 - Deep TYPE forensic audit и исправление TYPE-001..003

- Проведён полный forensic-аудит подсистемы TYPE (123 раздела).
- Health Score: 8.6/10.
- TYPE-001 [P3]: BytecodeVm type resolution linear scan → `BuildRecordTypeCache()` для O(1).
- TYPE-002 [P3]: 4 negative acceptance samples (cyclic, duplicate, unknown_field, invalid_type).
- TYPE-003 [P4]: Manual constructor overload resolution — уже задокументировано.
- Дополнительно: исправлен crash при дублированном TYPE (`DeclareTypeMethods` + `MethodsCompleted`).
- Build: 0 errors. Тесты: 897 + 374 + 143 + 51 = 1,465.
- Создан `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или INPUT/PRINT/DATA аудит.

### 2026-08-20 - Deep I/O forensic audit и исправление IO-005

- Проведён полный forensic audit I/O-подсистемы (112 раздела).
- Health Score: 8.5/10.
- IO-005 [P3]: I/O helpers вынесены в `BuiltinIOHelpers` (TryParseInputField, NormalizeInputField, CountInputFields).
- Build: 0 errors. Тесты: 897 + 149 = 1,046.
- Создан `docs/archive/audits-legacy/IO_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или реализация DATA/READ/RESTORE.

### 2026-08-20 - Deep File System / File I/O forensic audit

- Проведён полный forensic audit файловой подсистемы (135 раздела).
- Health Score: 8.8/10.
- READLINES duplication: вынесен в `BuiltinFileIOHelpers.EvaluateReadLines()`.
- Build: 0 errors. Тесты: 897/897.
- Создан `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md`.
- Следующий шаг: продуктовая работа или аудит системы ошибок (Error Handling, Level 10).

### 2026-08-19 - Deep Error Handling / Exceptions / Diagnostics forensic audit и исправление ERROR-003, ERROR-004

- Проведён полный forensic audit системы ошибок (135 раздела).
- Health Score: 8.5/10.
- ERROR-003 [P0]: `InvalidOperationException` из `BuiltinDateTimeHelpers` ловится и переводится в MB3007 (оба движка).
- ERROR-004 [P1]: `BytecodeEvaluationStack.Pop()` бросает `EvaluationStackUnderflowException`, VM переводит в MB3005.
- Создан `EvaluationStackUnderflowException.cs`.
- Build: 0 errors. Тесты: 897 + 94 + 21 = 1,012.
- Создан `docs/archive/audits-legacy/ERROR_HANDLING_AUDIT.md`.
- Следующий шаг: аудит модулей/namespaces/project structure (Level 11).

### 2026-08-19 - Deep Modules / Namespaces / Program Structure forensic audit

- Проведён полный forensic audit программной модели (130 раздела).
- Health Score: 8.5/10.
- **Axiom — строго однофайловый язык.** Программа = один `.axm` файл.
- Нет модулей, namespaces, imports, visibility modifiers, project file format.
- Все P2/P3 findings — feature gaps, не баги.
- Система стабильна для Bytecode/VM audit.
- Создан `docs/archive/audits-legacy/MODULES_AUDIT.md`.
- Следующий шаг: глубокий аудит Bytecode / VM (Level 12).

### 2026-08-19 - Deep Bytecode / VM Forensic + Performance Audit (Level 12) завершён

- Проведён полный forensic + performance аудит Bytecode VM по 10 фазам.
- Health Score: **8.4/10**.
- 48 benchmark scenarios: VM быстрее в 43/48 (90%), median speedup 0.63x.
- 1046/1046 тестов проходят (897 Runtime + 149 Execution Pipeline).
- Создан `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` с полным отчётом.
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
- Обновлены `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` и `docs/WORKLOG5.md`.
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
  - Документация: `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md`, раздел20
- Следующий шаг: доработать отчёт аудита до полного формата (122 раздела).

### 2026-08-19 - Форензик-аудит IDE завершён, начато исправление находок

Проведён глубокий аудит интеграции IDE с языком (уровень 13). Документ: `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md`.

**Исправлено (Session 1057):**
- **IDE-004** [P3]: Исправлена двойная классификация STRING в XSHD
- **IDE-005** [P3]: Добавлена подсветка операторов в оба XSHD
- **IDE-006** [P3]: Убраны COLUMN/ROW/PI из XSHD (не ключевые слова языка)
- **IDE-009** [P3]: Очистка диагностик при редактировании документа
- **IDE-001** [P2]: Устранено нарушение архитектуры SmartIdentifierService (инъекция парсинга)

**Запланировано (отложено):**
- **IDE-002** [P2]: Создать абстракцию отладчика вместо прямого использования BytecodeVmStopState — **ВЫПОЛНЕНО**
- **IDE-003** [P2]: Разделить ProgramConsoleService на сфокусированные сервисы — **ВЫПОЛНЕНО**
  - ✅ Извлечены: SessionManager (68 строк), ConsoleOutputService (124 строки), InputStateService (162 строки), InputPromptService (166 строк)
  - ✅ ProgramConsoleService сокращён с 2771 до 685 строк (фасад)
  - ✅ GraphicsHostService как отдельный класс — 2432 строки, выделен из partial-файла, фасад делегирует все IGraphicsHost-вызовы
  - ✅ NativeGraphicsScreenWindow decoupled: принимает GraphicsHostService + IGraphicsWindowInputSink
- **IDE-008** [P2]: Добавить команду отмены выполнения программы — **ВЫПОЛНЕНО**
- **TASK-007** [P2]: Консолидировать источник ключевых слов (5 мест → 1) — **ВЫПОЛНЕНО** (центральный реестр + тест XSHD)

**Не трогать:**
- ProgramRunService — стабилен, хорошо протестирован
- AxiomExecutionPipeline — ядро pipeline, тщательно протестировано
- DocumentWorkspaceService — чистый, хорошо протестирован

### 2026-08-20 - Complete Language Feature Inventory завершён

- Создан `docs/LANGUAGE_INVENTORY.md` — полная инвентаризация языка Axiom (79 разделов, 9,655 строк).
- Master Feature Registry: 356 LANG-features в 24 категориях.
- 21 подсистема с инвентарём, 29 матриц перекрёстных ссылок.
- Все источники сопоставлены: код, тесты, samples, документация, спецификация, аудиты.
- Следующий шаг: продуктовая работа или реализация следующего шага по плану.

### 2026-08-20 - Forensic-аудит REGEX подсистемы завершён

- Проведён полный forensic-аудит подсистемы REGEX.
- Создан `docs/audit/REGEX.md` (48 разделов, полная инвентаризация).
- 8 встроенных функций, 18 дескрипторов, полный паритет Interpreter ↔ VM.
- Health Score: 8.2/10. Главный вопрос: YES.
- Найденные проблемы (все P3/P4):
  - SYS-001: Отсутствие кэша regex в Interpreter
  - SYS-002: Дублирование валидации group/count
  - SYS-003: Regex не описан в SPECIFICATION3.md
  - SYS-004: Нет тестов с режимами m, s, x
  - SYS-005: Нет parity-тестов Interpreter vs VM
- Следующий шаг: продуктовая работа или реализация следующего шага по плану.

### 2026-08-20 - Forensic-аудит TIMING / SLEEP подсистемы завершён

- Проведён полный forensic-аудит подсистемы TIMING / SLEEP (48 раздела).
- Создан `docs/audit/TIMING_SLEEP.md`.
- 4 features: SLEEP (длительность), SLEEP (key wait), TIMER(), IRuntimeClock.
- Полный паритет Interpreter ↔ BytecodeVm. 23 теста, все проходят.
- Health Score: 8.6/10. Главный вопрос: YES.
- Найденные проблемы (все P3/P4):
  - SYS-015 [P3]: Дублирование 3 методов между Interpreter и BytecodeVm (~42 строки)
  - SYS-007 [P4]: SLEEP компилируется с LONG, падает в runtime с >int.MaxValue
  - SYS-009 [P4]: TIMER midnight wrap не документирован
- Следующий шаг: продуктовая работа или реализация следующего шага по плану.

### 2026-08-21 - Forensic-аудит IF / ELSE / SELECT CASE завершён

- Проведён полный forensic-аудит подсистемы ветвления (48 раздела).
- Создан `docs/audit/IF_ELSE_SELECT_CASE.md`.
- 5 features: IF/ELSEIF/ELSE, SELECT CASE, блочная видимость, dead branch elimination, специализированные опкоды.
- Полный паритет Interpreter ↔ BytecodeVm. 50+ тестов, все проходят.
- Health Score: **9.0/10** (самая высокая среди всех аудитов). Главный вопрос: **YES**.
- Найденные проблемы (все P3/P4):
  - SYS-001 [P4]: GetSelectCaseComparisonType() — identity function
  - SYS-002 [P4]: Pop() vs PopUnchecked() непоследовательность
  - SYS-003 [P3]: Нет явных тестов DECIMAL/LONG в SELECT CASE
  - SYS-004 [P4]: Нет теста вложенного SELECT CASE
- Следующий шаг: продуктовая работа.

### 2026-08-22 - Forensic-аудит FOR / WHILE / DO / LOOP завершён

- Проведён полный forensic-аудит подсистемы циклов (48 раздела).
- Создан `docs/audit/FOR_WHILE_DO_LOOP.md`.
- 8 features: FOR...NEXT, WHILE, DO...LOOP (5 форм), EXIT FOR/DO, CONTINUE FOR/WHILE/DO, специализированные FOR-опкоды, ValidateForStep.
- Полный паритет Interpreter ↔ BytecodeVm. 100+ тестов, все проходят.
- Health Score: **9.1/10** (самая высокая среди всех аудитов). Главный вопрос: **YES**.
- Найденные проблемы (все P4):
  - SYS-001 [P4]: Нет явного теста WHILE с побочными эффектами
  - SYS-002 [P4]: Нет теста DO + TRY/CATCH + CONTINUE
- Следующий шаг: продуктовая работа.

### 2026-08-22 - Forensic-аудит SUB / FUNCTION / PARAMETERS / RETURN / CALL MODEL завершён

- Проведён полный forensic-аудит процедурной модели (48 раздела).
- Создан `docs/audit/SUB_FUNCTION_CALL_MODEL.md`.
- 11 features: SUB, FUNCTION, RETURN, BYVAL, BYREF, локальные procedures, closures, function values, TYPE методы, TYPE конструкторы, защита стека.
- Полный паритет Interpreter ↔ BytecodeVm. 75+ тестов, все проходят.
- Health Score: **9.0/10**. Главный вопрос: **YES**.
- Найденные проблемы (1 P4):
  - SYS-001 [P4]: FieldByRefStoragePlan и ArrayElementByRefStoragePlan — dead code
- Все предыдущие находки (CALL-001..006) уже закрыты.
- Следующий шаг: продуктовая работа.

### 2026-08-22 - Forensic-аудит STRING / STRING OPERATIONS завершён

- Проведён полный forensic-аудит строковой подсистемы (48 раздела).
- Создан `docs/audit/STRING_OPERATIONS.md`.
- 20 features: STRING тип, STRING * N, 44 встроенных функций, операторы + и &, сравнения, STR/VAL, ASC/CHR, SPLIT/JOIN.
- Полный паритет Interpreter ↔ BytecodeVm. 120 тестов + 51 sample.
- Health Score: **8.8/10**. Главный вопрос: **YES**.
- Найденные проблемы (все P4):
  - SYS-001 [P4]: Нет non-BMP Unicode тестов для MID/LEFT/RIGHT
  - SYS-002 [P4]: LEN behavior для non-BMP Unicode не документировано
- Предыдущие дефекты (STRING-001, STRING-002) уже исправлены.
- Следующий шаг: продуктовая работа.

### 2026-08-21 - Forensic-аудит MODULES / NAMESPACES / PROGRAM STRUCTURE завершён

- Проведён полный forensic-аудит программной модели (48 раздела).
- Создан `docs/audit/MODULES_NAMESPACES_PROGRAM_STRUCTURE.md`.
- **Axiom — строго однофайловый язык.** Программа = один `.axm` файл.
- Нет модулей, namespaces, imports, visibility modifiers, project file format.
- Все P2/P3 findings — feature gaps, не баги.
- Health Score: **8.6/10**. Главный вопрос: **YES, WITH CONDITIONS**.
- Найденные проблемы:
  - SYS-001 [P3]: `InvokeTypeInvariant` не проверяет `MaxCallDepth` (edge-case дефект)
  - SYS-002 [P3]: Нет runtime-проверки `HasExitDo` на верхнем уровне (edge-case дефект)
  - SYS-003..007 [P2/P3]: Feature gaps (multi-file, project format, import/module, visibility, namespace)
  - SYS-008..013 [P3/P4]: Пробелы в тестах и документации
- Условия вердикта: закрыть SYS-001 и SYS-002 отдельной задачей.
- Система стабильна для перехода к следующей подсистеме.
- Следующий шаг: продуктовая работа.

### 2026-08-22 - Forensic-аудит DATA / READ / RESTORE завершён

- Проведён полный forensic-аудит проектирования подсистемы DATA / READ / RESTORE (48 раздела).
- Создан `docs/audit/DATA_READ_RESTORE.md`.
- Подсистема **полностью отсутствует** на всех уровнях: нет ключевых слов, нет синтаксических узлов, нет bound nodes, нет опкодов, нет runtime-компонентов, нет тестов, нет samples, нет документации.
- Health Score: **0/10** (не применимо — подсистема не существует).
- Оценка готовности к реализации: **8/10** — существующая архитектура полностью подходит.
- 5 features: DATA, READ, RESTORE, метки DATA, указатель чтения — все NOT_IMPLEMENTED.
- Предложен Recovery Roadmap из 8 этапов (проектирование → lexer/parser → binder → runtime → interpreter → bytecode/VM → IDE → samples/manual).
- Предложены 6 новых диагностических кодов (MB1040-MB1042, MB2040-MB2042, MB3012-MB3013).
- Главный вопрос: **NO** — подсистема полностью отсутствует.
- Следующий шаг: продуктовая работа или реализация DATA/READ/RESTORE по Recovery Roadmap.

### 2026-08-22 - Forensic-аудит GRAPHICS RENDERING CORRECTNESS завершён

- Проведён полный forensic-аудит подсистемы графического рендеринга (48 разделов).
- Создан `docs/audit/GRAPHICS_RENDERING_CORRECTNESS.md`.
- Подсистема **реализована полностью**: 188+ файлов, 34 опкода, 80+ samples, 2 режима (SCREEN + CANVAS).
- Health Score: **7.1/10**.
- Обнаружена 1 критическая проблема (P0): GetPixel в CANVAS не применяет камеру.
- Обнаружено 4 существенных проблемы (P1): LINE-валидатор, 4 нормализации цвета, STEP overflow, dispatcher bypass.
- Обнаружено 5 проблем P2 и 8 проблем P3.
- Все 6 engine-compare benchmarks показывают ≤1.000x (VM не медленнее interpreter).
- Главный вопрос: **YES, WITH CONDITIONS** — требуется исправление P0 и P1.
- Recovery Roadmap: 8 этапов (диагностика → корректность → семантика → архитектура → производительность → тесты → документация → подготовка).
- Следующий шаг: исправление P0/P1 проблем или продолжение аудита других подсистем.

### 2026-08-22 - План улучшений IDE/Language Integration: Фазы 0-1 завершены

- На основе аудита `docs/audit/IDE_LANGUAGE_INTEGRATION.md` создан пошаговый план `docs/audit/IDE_LANGUAGE_INTEGRATION_IMPROVEMENTS.md` (8 задач, 3 фазы).
- **Фаза 0 (P4/P3):** 3 задачи завершены — тесты Unicode форматтера, тесты глубокой вложенности блочного редактирования, верификация STRING в XSHD.
- **Фаза 1 (P3):** 3 задачи завершены — `BlockLineClassifier` в `Axiom.Core` (замена 27 regex), `Lexer.Lex()` в форматтере (замена собственного токенизатора), кэш парсинга в `SmartIdentifierService`.
- Архитектурный эффект: удалено ~260 строк дублированной логики, централизованы классификатор и токенизатор.
- **Фаза 2 (P2):** 2 задачи отложены — перенос нормализации идентификаторов в pipeline, генерация XSHD из центрального реестра.
- Тесты: 63/63 IDE pass (31 formatting + 28 block editing + 4 smart identifier).
- Следующий шаг: продуктовая работа или реализация Фазы 2.

### 2026-08-22 - План улучшений IDE/Language Integration: все фазы завершены

- **Фаза 2 (P2):** 2 задачи завершены — `IdentifierNormalizationService` в `Axiom.Execution` (удалена прямая зависимость IDE от `Axiom.Parsing`), скрипт генерации XSHD из центрального реестра.
- Дополнительно исправлены 2 бага: форматирование при нажатии Enter, каретка при переключении форматов.
- **Все 8 задач плана улучшений IDE/Language Integration завершены.**
- Архитектурный эффект: удалено ~260 строк дублированной логики, `AxiomSmartIdentifierService` сокращён с 247 до 47 строк, IDE потеряла прямую зависимость от `Axiom.Parsing`.
- Тесты: 71/71 IDE pass.
- Следующий шаг: продуктовая работа.

### 2026-08-22 - Подсветка выражений в интерполированных строках (IDE) завершена

- Задача: выражения внутри `{...}` строки `$"..."` должны подсвечиваться языком, при этом комментарии `'`/`REM` обязаны перекрывать `$"..."`; только xshd, обе темы, без правки AvalonEdit.
- Решение: комментарии и строки переведены с Rule на Span (Span-комментарии размещены первыми), `$"..."` — многострочный Span с вложенным Span-дыркой `{...}` и вложенной обычной строкой внутри дырки; слова вынесены в RuleSet `Code` и подключаются `<Import>`; добавлен цвет `InterpolationText` (светлая `#000000`, тёмная `#D4D4D4`).
- Тесты: +22 теста реального движка через `DocumentHighlighter` (комментарии против `$"..."`, подсветка дырок, `{{`, `\"`, вложенные строки, многострочность, порядок Span); `EditorFeatureTests.Highlighting` — 44/44.
- Изменённые файлы: `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`, `Axiom.Dark.xshd`, `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`.
- Попутно найдено (предсуществующее, вне задачи): в `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs` безвозвратно повреждены русские литералы (U+FFFD) — ~39 тестов падают на чистом HEAD; полный прогон набора зависает/роняет хост на том же классе.
- Следующий шаг: продуктовая работа; отдельной задачей — восстановление повреждённых литералов MainWindowViewModelTests.

### 2026-08-22 - Форматирование документа при открытии (IDE) завершено

- Задача: при открытии файла выражения внутри `{...}` интерполированных строк не приводились к стилю (например, `len` оставался в нижнем регистре), хотя форматирование строки при уходе курсора работало.
- Причина: при открытии форматирование не запускалось вовсе — только построчный триггер `TryFormatEditorLine`.
- Решение: событие `DocumentOpened` в `DocumentWorkspaceService` (только для новых вкладок) + подписка `FormatDocumentAfterOpen` в `MainWindowViewModel`: форматирование выбранным стилем (+ smart-identifiers), затем `MarkAsSaved()` без dirty-флага; dirty-вкладки и не-`.axm`/read-only пропускаются. Покрыты все пути открытия: диалог, recent files, дерево файлов.
- Тесты: новый файл `tests/Axiom.Ide.Tests/DocumentOpenFormattingTests.cs` — 3 теста (форматирование `{len(` → `{LEN(` при открытии; сохранение несохранённых правок при повторном открытии; пропуск не-`.axm`); регрессионный прогон CodeFormatting + DocumentOpenFormatting + Highlighting — 93/93.
- Файлы: `DocumentWorkspaceService.cs`, `MainWindowViewModel.cs`, `MainWindowViewModel.Formatting.cs`, `samples/expressions/interpolation_basic.axm` (`{LEN(name)}`), новый тест-файл.
- Следующий шаг: продуктовая работа; отдельной задачей — восстановление повреждённых литералов MainWindowViewModelTests.

### 2026-08-22 - Аудит и стандартизация HTML-документации manual завершены

- Создан нормативный стандарт `docs/DOCUMENTATION-STYLE.md`: типы страниц, каркас HTML, канонические названия и порядок разделов, нотация синтаксиса, терминологический словарь, правила текста, примеров, диагностики, ссылок и навигации, чек-лист готовности страницы.
- Создан отчёт `docs/DOCUMENTATION-AUDIT.md` с полной картой проблем и статистикой.
- Пилотные правки корпуса (35 файлов): унификация «Ручная проверка»/«Части записи»/«Аргументы», нормализация eyebrow-меток, легенда нотации в `getting-started.html`, достройка кодов диагностики (`MB0001`–`MB0002`, `MB1001`–`MB1004`, `MB2011`, `MB2014`, `MB2028`, `MB2029`, `MB2036`, класс `MB4xxx`), русификация примеров эталонных страниц, удаление BOM.
- Проверено: ссылки (383 на samples + 101 внутренняя) целы, коды диагностики соответствуют исходнику, каркас всех 102 страниц цел.
- Следующий шаг: масштабирование стандарта на семейства функций и графический кластер; `MB2030` на страницу `type.html`; аудитор синхронности таблицы кодов диагностики.

### 2026-08-22 - Масштабирование стандарта документации на семейства функций и графику завершено

- Второй этап переработки `docs/manual/` (30 файлов, суммарно 57 за оба этапа): `h3` «Аргументы» в семействах функций (129 подразделов на 20 страницах), примеры разнесены к своим функциям, 46 блоков «Результат:» только по подтверждённым выводам.
- Исправлены дефекты копипаста в заголовках на трёх страницах (TRIM/LTRIM/RTRIM, REVERSE, ASC/CHR, INT/VAL).
- Графический кластер (12 страниц): канонический порядок «Пример → Ручная проверка → Диагностика», «Когда использовать X» вместо «Для чего нужен оператор X», короткие канонические заголовки на `tilesets`/`tilemaps`.
- Найдено по исходнику: `MB2030` — мёртвый код, документировать нельзя; требуется решение (удалить или вернуть ограничение временных получателей TYPE).
- Открытый вопрос: формат вывода `DOUBLE`/`DECIMAL` через `PRINT` не подтверждён — числовые «Результат:» отложены.
- Все проверки пройдены: ссылки целы, коды существуют, структура каркаса и тегов в порядке.
- Следующий шаг: полнота правил графического кластера; «Ручная проверка» для страниц без раздела; подтверждение числового формата вывода; решение по `MB2030`; аудитор таблицы кодов.

### 2026-08-22 - Закрывающий этап переработки документации завершён

- Формат вывода `DOUBLE`/`DECIMAL` подтверждён прогоном `src/Axiom.ManualChecks` (45/45 PASS); «Результат:» достроены на четырёх числовых страницах.
- «Ручная проверка» добавлена на 8 страниц (`view`, `window`, `get-put`, `images`, `sprites`, `tilesets`, `tilemaps`, `screen-functions`) — итого 84 страницы с разделом.
- Усилены правила `view`; правила `window`/`images`/`sprites`/`get-put` вычитаны и признаны полными.
- `cheat-sheet.html`: интерполяция в сводке ввода-вывода + раздел «Чего здесь нет»; `examples.html`: два графических стартовых примера.
- Решение пользователя: `MB2030` отложен на самую последнюю задачу серии.
- Итог по корпусу: 56 блоков «Результат:» на 22 страницах, битых ссылок нет, коды и структура в порядке.
- Следующий шаг: последняя задача серии — решение по мёртвому `MB2030`; отдельно — аудитор синхронности таблицы кодов.

### 2026-08-22 - MB2030 удалён (задача серии закрыта)

- Решение пользователя: удалить мёртвую диагностику, ограничение временных получателей TYPE не возвращать.
- Исходник: удалены константа `InvalidTypeMethodReceiver` из `DiagnosticCodes.cs` и метод `ReportInvalidTypeMethodReceiver` из `Binder.Diagnostics.cs`.
- `docs/SPECIFICATION3.md`: добавлено финальное датированное обновление об удалении; недопустимые получатели TYPE-методов покрываются `MB2024`/`MB2023`.
- Валидация: сборка 0 ошибок; Semantics 374/374, ExecutionPipeline 149/149, Lexing 18/18, Syntax 66/66. Три ранее существующих падения (Runtime, Bytecode, Parsing) воспроизведены на чистом исходнике — вне задачи. `AcceptanceProgramTests` роняет хост — ранее известная инфраструктурная проблема.
- Следующий шаг: продуктовая работа; кандидаты — повреждённые литералы `MainWindowViewModelTests`, падение хоста `AcceptanceProgramTests`, три ранее существующих падающих теста, аудитор таблицы кодов.

### 2026-08-22 - Исправление ранее существующих падений тестов и аудитор таблицы кодов

- **MainWindowViewModelTests**: 45 повреждённых русских литералов (U+FFFD) восстановлены (44 автоматически + 1 вручную по ресурсу). Попутно исправлен баг эха ввода (`InputPromptService.SubmitInput` читал текст после сброса состояния) и копипаст-баг теста OpenManual. Результат: 113/113.
- **AcceptanceProgramTests**: bare `SLEEP` → `SLEEP 500` в 10 sample-файлах. Результат: 797/797 (было краш хоста).
- **Parsing**: удалён отладочный тест `Debug_InterpolatedStringMultipleTokens` (всегда падал через `Assert.True(false)`).
- **Bytecode**: обновлено ожидание `Lower_IfWithElse_EmitsBranchAndEndLabels` (константное свёртывание `1=1`). Результат: 203/203.
- **Runtime**: заменена программа в `Execute_ResumeFromSourcePositionBreakpointCanStopAgainOnSameLine` (константное свёртывание `2+3`). Результат: 928/928.
- **Аудитор таблицы кодов**: создан `src/Axiom.ManualChecks/DiagnosticsTableAudit.cs` + команда `audit-diagnostics-table`. Добавлены 16 недостающих кодов в `docs/manual/diagnostics.html`. Прогон: PASS (59 пользовательских кодов синхронизированы).
- Итого: Semantics 374, Parsing 146, Lexing 18, Syntax 66, Bytecode 203, Runtime 928, Execution 797, IDE 577/581 (4 ранее существующих).
- Следующий шаг: продуктовая работа; оставшиеся 4 ранее существующих падения IDE-тестов.

### 2026-08-25 - Спецификаторы формата в интерполированных строках

- Реализован синтаксис `{выражение:формат}`, `{выражение,выравнивание}`, `{выражение,выравнивание:формат}` внутри `$"..."`.
- Поддерживаемые спецификаторы: `F`, `N`, `E`, `P`, `X`, `D`, `G` (с опциональной точностью). Выравнивание: положительное = вправо, отрицательное = влево.
- Изменены: лексер, парсер, AST, bound tree, binder, интерпретатор, байткод (lowering + VM), подсветка IDE (оба .xshd).
- Пример: `samples/expressions/interpolation_format.axm`.
- Валидация: сборка 0 ошибок/0 warnings; все тест-сьюты без регрессий.
- Следующий шаг: добавить runtime-тесты спецификаторов формата; обновить manual.

### 2026-08-25 - устранены дефекты DATE/TIME по forensic-аудиту

- `DATEDIFF` с time-интервалами (`h`/`n`/`s`) больше не смешивает виды значений: время + дата-время теперь даёт ошибку аргумента `MB3007` вместо молчаливой разности по времени суток.
- Дублированная логика DATE/TIME (~300 строк) консолидирована в `BuiltinDateTimeHelpers.cs`; в обоих движках остались только тонкие обёртки, различающиеся передачей позиции.
- Русские вкрапления в DATE/TIME-диагностике переведены на английский; сообщения конвертации типов унифицированы между движками (`Cannot convert type: {from} -> {to}.`).
- Добавлены VM-тесты DST-переходов (`TOUTC`/`UTCOFFSET`), тесты смешанного `DATEDIFF` и тесты `DATEADD` с `q`/`yyyy` в обоих движках; добавлен negative-sample `samples/functions/datediff/mixed_temporal_kinds_error.axm` и обновлён manual.
- Валидация: сборка 0 ошибок (3 ранее существовавших предупреждения); Runtime 944/944, Execution 947/947, остальные сьюты без новых падений (IDE — те же 4 ранее существовавших).
- Следующий шаг: ручная приёмка negative-sample в `Axiom.Ide`; пункт P5 (нативные Date/Time/DateTime в аргументах `DATEADD`/`DATEDIFF`/`DATEPART`) отложен до отдельного проектного решения.

### 2026-08-26 - Manual: нативные типы DATE/TIME/DATETIME в разделе типов данных

- Документация типов `DATE`/`TIME`/`DATETIME` добавлена в manual: `data-types.html`, `data-types-scalars.html`, `data-type-limits.html`, `data-types-conversions.html`, `date-time-functions.html`.
- Факты сверены с кодом: ключевые слова, `TypeSymbol`, канонические форматы, сравнение через `CompareTo`, неявные преобразования и сигнатуры `DATEVALUE`/`TIMEVALUE`.
- HTML проверен на баланс парных тегов и целостность относительных ссылок (временный скрипт в `.artifacts/`).
- Код, тесты и samples не менялись.
- Следующий шаг: ручная приёмка `samples/functions/datediff/mixed_temporal_kinds_error.axm` в `Axiom.Ide`; отдельным решением — нативные типы в аргументах `DATEADD`/`DATEDIFF`/`DATEPART` (P5).

### 2026-08-26 - DT-004: нативные DATE/TIME/DATETIME в DATEADD/DATEDIFF/DATEPART завершён

- `DATEADD`/`DATEDIFF`/`DATEPART` принимают нативные `DATE`/`TIME`/`DATETIME` без round-trip через строку; строковые формы сохранены без изменений.
- Binder получил выбор перегрузки по точному совпадению типов аргументов (впервые активирована ветка нескольких перегрузок одной арности); аргументы связываются один раз.
- Возврат нативных форм `DATEADD`: `DATE → DATE`, `TIME → TIME`, `DATETIME → DATETIME`; `DATEDIFF` — `LONG`, `DATEPART` — `INTEGER`.
- Лексер и парсер дополнены ключевыми словами `DATE`/`TIME`/`DATETIME` (объявление `DIM d AS DATE` и вызовы `DATE()`/`TIME()` работают).
- Тесты: Semantics +2, Runtime +14, Execution +3; полный прогон решения зелёный (IDE — те же 4 ранее существовавших падения).
- Следующий шаг: ручная приёмка `samples/functions/dateadd/typed_temporal_values.axm` и `mixed_temporal_kinds_error.axm` в `Axiom.Ide`; отдельно — адресный профиль производительности нативных перегрузок в `samples/smoke/engine-compare/`.

### 2026-08-26 - DT-004: удаление строковых форм (завершение)

- Выполнено: строковые формы DATEADD/DATEDIFF/DATEPART удалены из кода, документации и примеров; enum-иды DateAdd/DateDiff/DatePart reserved.
- Защита DT-001 усилена: смешение временных видов теперь compile-time MB2003 (Binder), тесты переведены на прямой bind.
- Samples и acceptance зелёные (Execution 950/950 — включая engine-compare паритет).
- Документация (manual, аудит, inventory, coverage) приведена к нативному API.
- Осталось (опционально): удалить reserved-иды при следующем несовместимом изменении байткода.

### 2026-08-26 - Запрет усекающего присваивания даты-времени в DATE/TIME

- Выполнено: DIM/CONST/присваивание полной даты-времени (нативный DATETIME или строковый литерал) в DATE/TIME -> ошибка компиляции MB2003.
- Разрешены: расширение DATE->DATETIME и TIME->DATETIME; дата-только строка в DATE; время-только строка в TIME; явные DATEVALUE/TIMEVALUE.
- Тесты: +12 (Semantics 389/389); регрессий нет (Runtime 958/958, Execution 950/950).
- Известный остаток: runtime-строка (DIM d AS DATE = s, где s - переменная STRING) всё ещё может усечься в рантайме - статически не обнаружить; закрытие потребует runtime-проверки конверсии (затронет и аргументы функций - отдельное решение).

### 2026-08-26 - графический аудит Этап 1 закрыт (P0 GetPixel + 3×P1)

- Выполнены TASK-G-01..05 из `docs/audit/GRAPHICS_RENDERING_CORRECTNESS.md`: P0 GetPixel-камера, LINE-валидатор, консолидация нормализации цвета, STEP overflow, dispatcher для absolute-pixel.
- Сборка 0 ошибок; Bytecode 210/210 (+7), Runtime 962/962 (+4), Ide 579/583 (4 pre-existing).
- Вынесено отдельно: VM-паритетные тесты STEP, переключение `ScreenFramebuffer.NormalizeColorIndex` на общий helper, SYS-G-016 (13 опкодов в `TryGetStackEffect`).
- Следующий шаг: Этап 2 — стабилизация семантики (TASK-G-06 CIRCLE default color, G-07 ApplyPaletteUsing, G-08 DRAWTILEMAP/DRAWTILELAYER позиция, G-09 PAINT пустой паттерн).

### 2026-08-26 - графический аудит Этап 2 закрыт (стабилизация семантики)

- Выполнены TASK-G-06..09: CIRCLE default color задокументирован (manual); ApplyPaletteUsing прерывает выполнение (RuntimeFailureException); DRAWTILEMAP/DRAWTILELAYER обновляют позицию; PAINT пустой паттерн валидируется.
- Сборка 0 ошибок; Bytecode 210/210, Runtime 965/965, Ide 579/583 (4 pre-existing).
- Следующий шаг: Этап 3 — архитектурное улучшение (TASK-G-10..15).

### 2026-08-26 - графический аудит Этап 3 закрыт (валидатор + переименование)

- Выполнены TASK-G-10 (`TryGetStackEffect` для 13 опкодов) и TASK-G-11 (`mouseEventKind` → `flags`).
- Сборка 0 ошибок; Bytecode 233/233 (+23), Runtime 965/965, Ide 579/583 (4 pre-existing).
- Осталось (Этапы 4-7, по аудиту «можно отложить»): оптимизации (G-16..19), консолидация helpers (G-12..14), dead code (G-15), паритетные тесты, бенчмарки.

### 2026-08-26 - исправлена регрессия Execution-теста (ABS-типизация)

- Полная верификация вскрыла падение `ScreenGameAcceptanceSample_RunsOnBytecodeVm` (BC4116 на `dy = -ABS(dy)`).
- Причина: латентный баг `GetBuiltinReturnType` — возвращал объявленный Double у ABS, игнорируя `SameAsFirstArgument`; вскрыт после включения type-walker'а для графических опкодов.
- Исправлено (политики SameAsFirstArgument/PromotedNumeric применяются из дескриптора). Execution 950/950, новых падений нет.
### 2026-10-05 - завершены этапы 1-4 плана Electron-IDE

- По `docs/ELECTRON_IDE_PLAN.md` реализовано: headless-CLI `axiom` (`src/Axiom.Cli`), извлечение форматтера в `Axiom.Formatting`, каркас и рендерер Electron-IDE в `ide/`, лаунчер `run-ide.bat`, e2e-сюита Playwright (11 тестов, все зелёные).
- WPF-IDE не изменена и не деградирована; полный прогон решения зелёный (Ide — те же 4 ранее существовавших падения), CLI-тесты 22/22.
- Ключевые отклонения от плана: бинарь CLI в `.artifacts/bin/...` (репозиторный `Directory.Build.props`), `fmt --line` не реализован (построчного режима форматирования в Axiom нет), freshness-guard — по git-ревизии.
- Следующий шаг: ручная приёмка Electron-IDE пользователем через `run-ide.bat`.

## 2026-10-06 — после Session 1102: исправления по аудиту языка

Текущий приоритет: Wave 0 дорожной карты `docs/audit/LANGUAGE_FUNCTIONS_FULL_AUDIT.md` (раздел 40), затем Wave 1.

1. [x] W0.1 (P1, AXD-001): FINALLY при повторном выбросе из CATCH — исправлено 2026-10-06 (Session 1103): `Interpreter.Statements.cs ExecuteTry` захватывает ошибку тела CATCH, исполняет FINALLY, затем выбрасывает; 4 автотеста (2 Interpreter + 2 BytecodeVm), sample `samples/operators/try/rethrow_from_catch_runs_finally.axm`, manual `try.html`, раздел SPECIFICATION3.
2. [x] W0.2 (P1, AXD-002): решён запретом — новая диагностика `MB2040` (биндер, движко-независима): оператор-выражение обязан быть вызовом; `FLUSH f` даёт две MB2040; 3 теста BinderTests, негативный sample `samples/acceptance/negative/expression_statement_no_effect.axm`, строка в manual `diagnostics.html`, раздел SPECIFICATION3. Полный sweep samples не выявил ни одной сломанной программы.
3. [x] W1.1 (P2, AXD-005): единые тексты диагностик — шесть пар унифицированы; паритет-тест `Run_UnifiedDiagnosticTextsMatchInBothEngines`.
4. [x] W1.2 (P2, AXD-006): локализация builtin-диагностик — эталон выбран: локация вызова (договор VM); Interpreter приведён (`GetBuiltInArgumentLocation`).
5. [x] W1.3 (P2, AXD-007): общий форматтер `RuntimeCallChainFormatter`; VM именует локальные функции по `BytecodeRoutine.Symbol`, `<global>`-кадр исключён. Остаток закрыт (Session 1105): идентификатор конструктора передаётся операндом `ValidateTypeInvariant`, VM синтезирует кадр конструктора в цепочке; паритет-тест в `ExecutionPipelineTests`.
6. [x] W1.4 (P2, AXD-003): перекодированы все 8 mojibake-литералов (7 из аудита + DIRSIZE-текст, найденный при исправлении); регрессионный тест `Execute_RenameFileWithPathArgumentReportsReadableRussianDiagnostic`.
7. [x] W1.5 (P2, AXD-004): единый кодировочный контракт — `RuntimeTextFile` при OPEN INPUT проверяет строгий UTF-8 + пропускает BOM, при невалидном UTF-8 читает Windows-1251; FILEPOS/SEEKFILE контентно-относительны.
8. [x] W1.6 (P2, AXD-008): новый код `MB3012` NumericDomain + общий helper `EvaluatePowerOperation`; оператор `^` даёт доменную ошибку в обоих движках.
9. [x] Wave 3 начало (P3, AXD-015, Session 1105): фолбэк-свитч VM сокращён до 15 живых variadic-ID; `ToInt32Checked` сведён к общим перегрузкам `RuntimeArithmeticHelpers`; `EvaluateIntBuiltin` перенесён в `Interpreter.Builtins.Numeric.cs`.
10. [x] Wave 2 (Session 1106): генератор подсветки дополнен BuiltIn-секцией из `axiom metadata` + режим `-Write`; sync-тесты `HighlightingVocabularySyncTests` (xshd ×2 и словарь форматирования ↔ реестр); устранён накопившийся рассинхрон (STRING отсутствовала в BuiltIn); non-BMP-контракт строк — `InterpreterTests.StringNonBmp`; тонкие filesystem builtins и фиксация русских текстов диагностики — `InterpreterTests.FileSystemDiagnostics` (AXD-016 закрыт, AXD-017 в основном закрыт).
11. [x] Wave 5, документационные контракты (Session 1107): LF-концы строк (WRITELINE/NEWLINE) и Unicode-единицы строк (LEN) отражены в manual; правила чтения VAL добавлены в manual; датированный раздел в SPECIFICATION3 фиксирует строковые единицы, контракт VAL, LF, отсутствие esc-последовательностей, отказ от `FuncName = value` и построчность; исправлены ошибки аудита: `^`-ассоциативность и WEEKDAY Monday-first уже были в manual, операторы `<<`/`>>` существуют (AXD-009/014/018 частично сняты, AXD-020 закрыт).
12. [x] Wave 4 (Session 1108, одобрено пользователем): RANDOMIZE (Reseed в IRuntimeRandom + новый Opcode.ReseedRandom), CINT/CLNG/CDBL/CDEC (усечение к нулю, MB3008), ARRAYSUM/MIN/MAX/AVERAGE (общий helper, MIN/MAX со строками), ANDALSO/ORELSE (короткое замыкание: ленивый Interpreter + двухметочная схема в VM); попутно NEXT принял опциональное имя счётчика (QB-наследие); 13 тестов + паритетный Theory, 9 примеров, manual ×4 страницы + search-index, подсветка из реестра.
13. Отложено: полный per-function разброс тонких filesystem builtins (сейчас smoke-уровень); хвост Wave 3 — параллельные Graphics/Keyboard/Mouse-файлы движков; выравнивание текста unallocated-диагностики массива (хвост AXD-005).

Ручная приёмка Electron-IDE (из Session 1101) сохраняется как параллельный шаг пользователя.
