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
### 2026-08-26 - реализованы NIT-001…005 из аудита переменных (сессия 1113)

- По поручению пользователя реализованы рекомендации аудита `docs/audit/VARIABLES_SCOPE_AUDIT.md`: предупреждение `MB2045` (переменная + процедура), диагностика `MB2044` (поле TYPE без SELF), подсказки в `MB2001` (CALL), `MB2003` (ссылка на функцию), `MB2023` (похожее поле), двухпроходное связывание глобальных имён (порядок объявления перестал влиять).
- Следствие: параметры/локальные с именем более поздней глобальной теперь дают `MB2016`; обновлены сэмплы и тесты, полагавшиеся на старую порядковую зависимость.
- Все тестовые проекты зелёные (кроме 4 предсуществующих падений Ide.Tests, не связанных с правкой); ManualChecks 45/45.
- Задачи из «Ближайших задач» (modern graphics/maze_chase) не менялись — правка была точечной и не связана с графическим слоем.

### 2026-08-26 - счётчик FOR: реализованы P1, P2, P3 (сессия 1114)

- По поручению пользователя реализованы рекомендации `docs/audit/FOR_COUNTER_ANALYSIS.html`: ошибка `MB2042` (внутренний цикл переиспользует счётчик внешнего — внешний тихо завершался за одну итерацию), предупреждение `MB2043` (переиспользование переменной как счётчика; Warning, не блокирует), подсказки в `MB2009` (объявите счётчик через DIM) и `MB2010` (для счётчика используйте DIM, а не CONST), раздел «Значение счётчика после цикла» в `docs/manual/for.html` + строки MB2042/MB2043 в `diagnostics.html`.
- P3 — принят вариант A: два режима счётчика сохранены, семантика не менялась; компиляционный обход 618 сэмплов: MB2042 только в новом негативном сэмпле, существующий корпус не сломан. Вариант B (всегда локальные счётчики) остаётся открытым вопросом команды языка.
- Тесты: Semantics 409/409, Execution 968/968, Runtime 965/965, Bytecode 233/233, Lexing 18/18, Parsing 146/146, Syntax 66/66, ManualChecks 45/45; Ide.Tests 579/583 (4 предсуществующих падения канвас-хоста).
- Задачи modern-graphics не менялись.

### 2026-08-26 - счётчик FOR: принят и реализован вариант B (сессия 1115)

- Команда языка приняла решение: перейти на вариант B — одна модель счётчика вместо двух: счётчик FOR всегда локальный, цикл создаёт его сам. Причины: проще учить и объяснять; новичок не может случайно изменить свою переменную циклом; ловушка вложенных циклов исчезает на уровне контракта (MB2042 не нужен); ментальная модель совпадает с C#/Python/JS.
- Реализация: `BindForStatement` всегда объявляет локальный счётчик; занятое имя (`DIM`/`CONST`/параметр/внешний цикл) — ошибка `MB2016`, неподходящий тип занявшего имени — `MB2009`; `MB2042`/`MB2043` и `_activeForCounters` удалены; сообщения MB2016/MB2009 объясняют контракт «счётчик FOR всегда локальный».
- Миграция корпуса: 12 сэмплов с переиспользованием приведены к новой модели (в т.ч. `maze_chase.axm` — итог копируется внутри тела); удалены `for_nested_counter_reuse.axm` и `for_counter_reuse_warning.axm`; создан негативный `for_counter_must_be_local.axm`; `predeclared_counter.axm` переписан (`after=3`).
- Тесты: Semantics 409/409, Execution 969/969, Runtime 965/965, Bytecode 233/233, Lexing 18/18, Parsing 146/146, Syntax 66/66, ManualChecks 45/45; сборка 0 ошибок; Ide.Tests 579/583 (4 предсуществующих падения канвас-хоста). Компиляционный обход 617 сэмплов: FOR-ошибки только в двух намеренно негативных сэмплах. Живые пробы 8 сценариев — всё по контракту.
- Документация: `for.html`, `variables-scope.html`, `diagnostics.html` переписаны под единую модель; `SPECIFICATION3.md` — новое обновление «счётчик FOR всегда локальный», заменяющее договор от 2026-05-13; `FOR_COUNTER_ANALYSIS.html` и `VARIANT_B_ANALYSIS.html` помечены «решение принято и реализовано (1107)»; `VARIABLES_SCOPE_AUDIT.md` — NIT-002 закрыт вариантом B.
- Задачи modern-graphics не менялись.

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

## 2026-10-07 — сведение WIP-ветки block-scope с master (Session 1116)

- Ветка `local/wip-block-scope-for-counter` (block scope, счётчик FOR вариант B, NIT-001…005) слита с master; конфликтные документы и код разрешены, диагностики ветки перенумерованы в `MB2044`/`MB2045` (MB2040/2041 заняты/сожжены канонической историей), записи worklog ветки перенумерованы в 1109–1115, каноническая запись Electron-IDE восстановлена как 1101.
- `maze_chase.axm` восстановлен в корпусном UPPERCASE-стиле с минимальной миграцией под вариант B (без 8 заранее объявленных DIM-счётчиков); калибровочный и позитивный acceptance-тесты зелёные.
- Валидация: сборка 0 ошибок; все тестовые проекты зелёные, кроме 4 предсуществовавших падений Ide.Tests (база master); ManualChecks PASS.
- Ближайшие задачи не меняются: ручная приёмка Electron-IDE и следующий modern-graphics шаг по реальной боли из `maze_chase`.

## 2026-10-07 — независимая перепроверка библиотеки (Session 1117)

По заданию пользователя выполнен новый доказательный аудит текущей ревизии `2603b3d` с отдельным анализом недостающих возможностей. Отчёт дописан в `docs/audit/LANGUAGE_FUNCTIONS_FULL_AUDIT.md`; новый реестр — `LANGUAGE_RECHECK_DEFECTS_20261007.md`, анализ API — `LANGUAGE_API_GAPS_20261007.md`. Исправления реализации в рамках аудита не выполнялись.

При ближайшем исправляющем срезе порядок:

1. Исправить скалярный тип ARRAYSUM/ARRAYMIN/ARRAYMAX (LA26-001) и добавить проверки присваивания/арифметики/выведения типа в обоих движках.
2. Закрыть CLR-утечки ARRAYSUM, CINT(NaN), READTEXT(non-BMP) (LA26-002/006/011), включая TRY/CATCH/FINALLY и единицу count/байтовые позиции.
3. Исправить определение кодировки OPEN (LA26-010), затем границы CINT/CLNG и конечность VAL (LA26-003/004/005), паритет REVERSE (LA26-012).
4. Закрыть оставшиеся договоры агрегатов и записи: устойчивое среднее, ранги, неподдерживаемые типы, LF (LA26-007/008/009/013).
5. Согласовать registry/binder/metadata с реальными типами и формами LBOUND/UBOUND (LA26-014); закрывать неизвестные клетки семантической матрицы адресными тестами, а не упоминаниями имён.
6. После стабилизации текущих функций обсудить явную кодировку существующего файлового API. Строгий числовой предикат, двоичные файлы и Unicode-режимы остаются предложениями для отдельного решения, не одобренными реализациями.

Каждое исправление должно сопровождаться регрессионной проверкой, запускаемым примером, справочником и соответствующими реестрами. Существующие RANDOMIZE, преобразования, агрегаты, сдвиги, форматирование и ANDALSO/ORELSE повторно добавлять не требуется. Ручная приёмка IDE и графики остаётся ранее запланированным пользовательским направлением; аудит не объявляет её выполненной.

## 2026-10-07 — первый исправляющий срез аудита (Session 1118)

LA26-001/002 исправлены: скалярные типы ARRAYSUM/ARRAYMIN/ARRAYMAX и обработка переполнения ARRAYSUM через MB3008. Добавлены 24 проверки двух движков, обновлены два запускаемых примера и раздел справочника. Исходные свидетельства аудита сохранены.

Следующий шаг по согласованному порядку — LA26-006 (CINT(NaN)), затем LA26-011 (READTEXT с символами вне основной плоскости Unicode). Для READTEXT перед изменением нужно явно установить договор счётчика UTF-16 и сохранения позиции файла; не заменять повреждённые данные молча. Далее остаются LA26-010, границы преобразований и остальные пункты записи Session 1117. Предложения новых API не одобрены этим исправлением.

## 2026-10-07 — закрыт CINT(NaN), LA26-006 (Session 1119)

LA26-006 исправлен и проверен: 12 новых адресных проверок, полный Runtime.Tests 1006/1006, Execution.Tests 1023 PASS и один прежний LA26-007. Пример samples/functions/cint/nonfinite_catch.axm успешно выполнен; справочник и реестры обновлены.

Следующий шаг — LA26-011 (READTEXT). Пользователю задан вопрос: должен ли READTEXT(f, 1) для «😀А» вернуть целый emoji или сообщить ошибку недостаточной вместимости UTF-16 с сохранением позиции. Ответ пока не получен; договор не выбран по умолчанию. До ответа можно независимо исправлять LA26-010 и LA26-003/004/005. LA26-007 остаётся открытым и воспроизводится общим набором приёмочных проверок.

## 2026-10-08 — закрыты LA26-010/011 (Session 1120)

Ответ пользователя закреплён: READTEXT(f, 1) возвращает целое скалярное значение Unicode, включая эмодзи. Байтовые позиции сохранены; LEN остаётся в единицах UTF-16. Исправлено определение кодировки коротких файлов и первых байтов UTF-8 без BOM. 17 адресных проверок проходят; полный Runtime.Tests 1019/1019, Execution.Tests 1028 PASS и прежний LA26-007. Запускаемый пример: samples/functions/readtext/unicode_chunks.axm.

Следующий незавершённый срез: LA26-003/004/005 — границы CINT/CLNG и конечность VAL; затем LA26-012 (REVERSE) и LA26-007/008/009/013/014. Вопрос READTEXT закрыт ответом пользователя; ожидания ответа больше нет. Явная кодировка файлового API остаётся отдельным предложением, а не выполненной возможностью.

## 2026-10-08 — завершены исправления LA26-001…014 (Session 1121)

Все подтверждённые находки независимого аудита исправлены. Сводка с доказательствами и границами проверки: docs/audit/LANGUAGE_REPAIR_STATUS_20261008.md. Полный прогон: 3555 успешных проверок, четыре прежних падения IDE; дополнительно две проверки LF в обоих движках, три проверки подписей Electron, пересобранный математический инструмент 45/45.

Следующий этап: адресно проверять неизвестные клетки исходной матрицы, особенно числовые края, Unicode и файловые условия; затем проектировать явный выбор кодировки существующего файлового API по реальной необходимости. Не добавлять повторно существующие средства языка. Строгий числовой предикат и двоичные операции остаются отдельными предложениями. Четыре прежних падения IDE и живая приёмка Electron/графики остаются отдельными задачами. Рабочие развилки выбирать самостоятельно по разрешению пользователя, решения записывать в документы.


### Session 1122 — независимая перепроверка

Исправлена потеря малого остатка ARRAYAVERAGE после сокращения больших значений. Следующий шаг: независимые сценарии Unicode и файловой системы; отдельно оценить стоимость точного накопления на крупных массивах. Новые языковые функции добавлять только при подтверждённом пробеле и отсутствии существующего API.


### Session 1123 — независимая перепроверка Unicode и файлов

Закрыты новые подтверждённые находки LA26-016…018; подробности LANGUAGE_RECHECK_20261008.md. Продолжить адресную проверку неизвестных клеток матрицы, особенно границ кодировок и файловых дескрипторов; измерить стоимость точного ARRAYAVERAGE на крупных массивах. G01 остаётся отдельным проектированием явной кодировки, а не исправлением автоопределения. При текущем полном прогоне проверить прежние четыре падения IDE и фактические результаты новых примеров.

### Session 1123 — итоговая проверка и следующий конкретный шаг

Полное решение: 3581 успешная проверка, четыре прежних падения IDE. После уточнения расширенных путей Windows Execution.Tests 1070/1070 и итоговый обход каталогов 8/8. Новые примеры успешно выполнены. Следующий приоритет — источник файлового падения ProgramRunServiceTests.Run_SetsCurrentDirToSourceFileDirectoryDuringExecution: проверить базовый каталог при sourceFilePath, а не механически менять тест. Далее классифицировать два графических падения и текстовую проверку маркера редактора. Режим goal активен; измерение ARRAYAVERAGE и неизвестные клетки матрицы сохраняются в плане.
### Session 1124 — закрыта задолженность четырёх IDE-проверок

Исправлены базовая папка исполнения LA26-019, физическое чтение POINT LA26-020 и потеря диапазона остановки LA26-021; устаревшие ожидания канала вывода и маркеров форматирования обновлены с поведенческими проверками. Полное решение 3596/3596. Далее измерить стоимость точного ARRAYAVERAGE на крупных массивах; продолжить независимую проверку неизвестных клеток матрицы. G01, явная кодировка файлов, остаётся отдельным необходимым расширением. Живая визуальная приёмка графики и подсветки остановки ещё не подтверждена. Режим goal активен.

### Session 1125 — измерена цена точного ARRAYAVERAGE

Выполнены два прогона Release, результаты ARRAYAVERAGE_PERF_20261008.md. Подтверждены около 360–429 МБ суммарных выделений на миллион обычных/сокращающихся DOUBLE-элементов. Следующий конкретный шаг — оптимизация точного накопления: ограниченные знаковые целочисленные суммы мантисс по двоичным показателям, сборка BigInteger после обхода, доказательство границ и независимые проверки результата. Затем повторить тот же профиль. Семантика и однократное округление должны сохраниться. После этого вернуться к неизвестным клеткам аудита и отдельному проектированию G01. Режим goal активен.

### Session 1126 — оптимизация точного среднего завершена в проверенном объёме

Выделения на миллион обычных/сокращающихся значений уменьшены более чем в 10 000 раз; результат подтверждён независимым числовым эталоном и полным прогоном 3603/3603. Подробности ARRAYAVERAGE_PERF_20261008.md. Следующий приоритет — G01: спроектировать и реализовать явный выбор кодировки существующего файлового API, сохранив прежние формы и автоопределение по умолчанию. Сначала проверить текущие формы OPEN/READFILE/WRITEFILE/APPEND и BASIC-прецеденты, выбрать минимальный согласованный договор, добавить реальные файлы UTF-8/1251/UTF-16 и случаи дописывания. Затем продолжить неизвестные клетки аудита. Строгий числовой предикат и двоичный ввод-вывод не добавлять без отдельной необходимости. Режим goal активен.

### Session 1127 — G01 реализован для функций целого файла

READFILE, READALLLINES, WRITEFILE и APPENDFILE получили явный строковый аргумент кодировки; подробный договор EXPLICIT_TEXT_ENCODING_G01.md. Полное решение 3633/3633, прежние формы сохранены. WINDOWS-1251 не распознаёт Unicode BOM, чтобы корректные однобайтовые префиксы оставались текстом. G01 пока PARTIAL, не закрывать его целиком.

Следующий конкретный шаг — явная кодировка OPEN с тем же общим договором. Изучить текущую грамматику, способы передачи FILE-операндов и чтение по байтовым границам; выбрать и записать форму без повторного вопроса пользователю. Реализовать синтаксис, связывание, оба движка, RuntimeTextFile, BOM/FILEPOS/FILELENGTH, READTEXT/SEEKFILE и строгие OUTPUT/APPEND. Поддержать подсветку нового токена, если он появится, форматирование, manual и приёмочные программы. После G01 продолжить неизвестные клетки аудита; не объявлять ограниченный двоичный API обязательным. Режим goal активен.

### Session 1128 — готов исполнитель явной кодировки FILE

RuntimeTextFile подготовлен и проверен для четырёх кодировок: строгие INPUT/OUTPUT/APPEND, BOM/байтовые позиции, UTF-16 строки, скалярное чтение и отказ на неверной позиции. 12 новых проверок, полный прогон 3645/3645. G01 остаётся PARTIAL; языковая форма OPEN ещё не добавлена.

Выбран следующий синтаксис: OPEN path FOR mode AS fileVariable USING encoding. USING уже существует; новое резервирование encoding не требуется. Следующий конкретный шаг — необязательные UsingKeyword/EncodingExpression в синтаксисе OPEN, STRING-проверка в связывателе, передача выражения через lowerer и OpenFile, верификация количества/типов стека и обоих движков. Сохранить прежнюю инструкцию OpenFile без дополнительного аргумента; новую форму явно отмечать в операндах и учитывать при расчёте максимального стека. Обновить manual, форматирование, обе схемы подсветки и запускаемые примеры только вместе с работающим синтаксисом. Режим goal активен.

### Session 1129 — новая форма OPEN реализована

OPEN USING encoding подключён к обоим исполнителям, проверке типов, байткоду и справочнику; добавлен запускаемый пример UTF-16LE. Далее независимо проверить пользовательский READTEXT/SEEKFILE/LINENUM на UTF-16 и отказ строгой записи через новый OPEN, а также нехватку стека и сохранность старых операндов в верификаторе. Затем вернуться к неизвестным клеткам исходного аудита. Живая визуальная приёмка графики и остановки IDE остаётся неподтверждённой. Режим goal активен.

### Session 1130 — независимая проверка G01 выявила и устранила два дефекта

Языковые SEEKFILE/READTEXT/LINENUMBER и строгая запись проверены в обоих движках; две ошибки интеграции исправлены, итоговое решение 3670/3670. Выбранный объём G01 реализован. Далее проверить восемь средств REGEX по каждой форме: Unicode и единицы индексов, нулевые/пустые совпадения, группы, замены, split, недопустимый шаблон и ограничение выполнения. Перед исправлением сохранить результат обоих движков и определить, является ли находка дефектом, недостающей возможностью, существующей альтернативой или избыточным предложением. Затем продолжить даты/время и оставшиеся неизвестные клетки. Живая визуальная приёмка графики и остановки IDE ещё не подтверждена. Режим goal активен.

### Session 1131 — две ошибки REGEX исправлены, 18 форм проверены в основных случаях

RX26-001/002 подтверждены до исправления; итоговый набор 118 REGEX-проверок и всё решение 3794/3794. Обновлены справочник, подсветка и три запускаемые программы. Дополнительно исправлена слабость файловой проверки непарного суррогата: LEFT("😀",1) передаёт его кодировщику, CHR отклонял аргумент раньше.

Следующий шаг — реальные отказы по времени всех 18 форм REGEX, отрицательные group/count, координаты диагностики и культура. После этого перейти к датам/времени и прочим неизвестным клеткам. Не подменять доказательства тайм-аута тестом успешного count=1. Режим goal активен.

### Session 1132 — обязательные граничные случаи REGEX перепроверены

36 настоящих тайм-аутов всех 18 форм двух движков проходят; 163 быстрых варианта проходят; полный диапазон 18 пар диагностик совпадает. Четыре ошибочных примера manual исправлены с независимыми доказательствами. Последний полный прогон решения 3794/3794 по текущему коду исполнителя; после него добавлены проверки и исправлена документация. Наличие новых проверок не выдаётся за новый общий прогон.

Следующий конкретный шаг Session 1133: инвентаризировать все DateTime-дескрипторы и договор строковых форм, создать независимый набор обоих движков для DATESERIAL/TIMESERIAL, 29 февраля/вековых правил, конца месяца/года, DATEADD/DATEDIFF/DATEPART, культуры, UTC/TOUTC/FROMUTC, неоднозначного и отсутствующего локального времени. Отделить reserved ID от реально отсутствующей возможности. Сначала сохранить результаты и классификацию, затем исправлять подтверждённые ошибки. Режим goal активен.

### Session 1133 — календарные границы и DIM временных типов

DT26-001 исправлен; независимые календарные и DST-проверки 10/10. Далее: сверить manual DATESERIAL/TIMESERIAL/WEEKYEAR с текущим реестром, расширить строковый разбор, крайние UTC-преобразования и управляемые часы всех форм. Полное покрытие DateTime пока не заявляется.

### Session 1134 — разбор и управляемые часы

16/16 адресных вариантов; код исполнителя не менялся. Исправлена DT26-002 в manual. Далее составить доказательства по каждому из 46 дескрипторов DateTime: оставшиеся формы арифметики DATETIME, части, неверные типы/токены и координаты отказов. Общая проверка семейства не завершена.

### Session 1135 — дескрипторы DateTime и DT26-003

Все 46 форм имеют независимый обычный вызов обоих движков, проверку связанного ID/типа и неверных аргументов. DT26-003 большого сдвига TIME исправлен; подробные границы CSV/отчёт DateTime. Следующий логичный срез — строковые поиск/изменение: INSTR/INSTRREV/REPLACE/MID/LEFT/RIGHT и Unicode-позиции, все формы, пустые строки и крайние индексы. Продолжить неизвестные клетки исходной матрицы, не объявлять весь аудит завершённым.

### Session 1136 — строковый поиск

14 форм, 57/57 проверок; новых ошибок исполнителя не найдено. Далее INSERT/REMOVE, REPEAT/SPACE, JOIN/SPLIT и строковые массивы: все формы, пустые значения, Unicode, границы и ошибки типов. Общий аудит продолжается.

### Session 1137 — построение строк

15 форм Fill/Array/Edit, новый набор 53; вместе с поиском 110/110. Код исполнителя не менялся. Далее TRIM/LTRIM/RTRIM, LCASE/UCASE/REVERSE и ASC/ASCW/CHR/CHRW: все формы, Unicode/непарные суррогаты, культура и ошибки типов. Общая цель активна.

### Session 1138 — завершён выбранный строковый срез

Все 42 зарегистрированные строковые формы имеют обычные вызовы обоих движков и проверки ID/типа; выбранные границы — 148/148. Новых дефектов исполнителя не подтверждено. Следующий срез: числовые функции, сохранение INTEGER/LONG, крайние значения, NaN/бесконечности, области определения и согласованность диагностики. Сверить все формы Numeric с исходной матрицей, продолжить неизвестные клетки, не ограничиваться уже исправленными CINT/CLNG/VAL. Общий аудит и визуальная приёмка IDE продолжаются.

### Session 1139 — числовые формы и NUM26-001

31 дескриптор Numeric, 61 обычный вариант и 37 неверных типов подтверждены, знак нуля VM исправлен. Следующая проверка — смешанные числовые типы MIN/MAX/CLAMP, границы LONG/DECIMAL, внутренние неконечные входы и продвижение типов. После этого продолжить оставшиеся неизвестные клетки исходной матрицы, включая неагрегатные операции массивов. Общий аудит продолжается.

### Session 1140 — продвижение числовых типов

Все 96 сочетаний MIN/MAX/CLAMP проверены; CDBL(DECIMAL) исправлен, неточность manual устранена. Следующий пункт — неагрегатные формы массивов (границы/выделение/копирование/срезы), независимые результаты всех форм, ошибочные ранги и реальные отсутствующие возможности. Внутренние неконечные входы остальных нематематических функций ещё не объявлены проверенными; статистика RND остаётся вне доказательств. Общий аудит продолжается.

### Session 1141 — сведения и срезы

15 форм и 57/57 проверок; AW26-001 исправлен в трёх функциях срезов. Следующий шаг: пустые многомерные диапазоны у INTEGER.MinValue/MaxValue на маленьких массивах, явный dimension остальных преобразований. Затем продолжить ARRAYFLATTEN/RESHAPE/CONCAT и поиск/изменение. Не объявлять все ARRAY-функции проверенными по этому набору; продолжить реальные пробелы API и неизвестные клетки матрицы.

### Session 1142 — крайние пустые диапазоны

AW26-002 исправлен общим расчётом пустой пары в ARRAYSLICE/ARRAYSKIP. 24 граничных сочетания, совместный набор 61/61. Следующий пункт: явные измерения ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE/BINARYSEARCH и остальных преобразований, затем FLATTEN/RESHAPE/CONCAT и динамические операции. Всё семейство массивов пока не объявляется проверенным.

### Session 1143 — общий договор явного измерения

AW26-003 исправлен в десяти функциях обоих движков, 70/70 нового класса. Следующий пункт — независимые формы ARRAYFLATTEN/ARRAYRESHAPE/ARRAYCONCAT, сохранение порядка/границ, формы пустых массивов, представления и ошибки несовместимых размеров. Затем поиск/динамические многомерные срезы и реальные отсутствующие возможности. Обычные многомерные формы всех десяти функций пока не объявлены доказанными.


### Session 1144 — границы объединения исправлены

AW26-004: ARRAYCONCAT у INTEGER.MaxValue исправлен в обоих движках; 139 адресных проверок прошли. Далее продолжить независимые ARRAYFLATTEN/ARRAYRESHAPE/ARRAYCONCAT: порядок, пустые формы, представления и несовместимые размеры. Этот пункт остаётся открытым.


### Session 1145 — независимые формы преобразования

Подтверждены 13 сценариев порядка/формы/отказов, семь неверных типов, пять невыделенных аргументов и пять точных форм FLATTEN/RESHAPE/CONCAT. ARRAYRESHAPE принимает многомерный source уже сейчас; исправлен пробел manual AW26-005. Далее многомерные динамические ARRAYPUSH/POP/INSERT/REMOVE: форма срезов, представления, сохранность после отказа, крайние границы; затем поиск и реальные отсутствующие возможности. Все типы элементов и ранги не объявлены проверенными.


### Session 1146 — пустая форма ARRAYINSERT исправлена

AW26-006: первое добавление в пустое выбранное измерение создаёт 1 TO 1 в обоих движках. Далее обычные многомерные PUSH/POP/INSERT/REMOVE, совместимость формы, сохранность после отказа и расширение у максимальной границы. Общий аудит активен.


### Session 1147 — обычные динамические операции подтверждены

22 новые проверки и совместный набор 203/203 подтвердили четыре операции по двум осям, отдельные отказы, расширение у MAX и представления. Далее независимый поиск CONTAINS/INDEXOF/LASTINDEXOF/FINDALL/BINARYSEARCH: повторы, отсутствие, крайние границы, координаты и пустые формы. Затем оставшаяся часть Array и общая матрица пробелов API. Все типы/ранги не объявлены доказанными.


### Session 1148 — поиск и описание отсутствия

22 проверки поиска прошли, совместный набор 225/225. AW26-007: manual исправлен на lower-1 для одномерного отсутствия. Далее DISTINCT и EVERY/SOME/FILTER/MAP/REDUCE: пустота, порядок/число вызовов, крайние индексы и ошибки пользовательской функции. Затем другие открытые семейства общей матрицы и недостающие возможности. Все типы/ранги не объявлены доказанными.


### Session 1149 — переполнение пяти обходов VM исправлено

AW26-008: EVERY/SOME/FILTER/MAP/REDUCE останавливаются после последнего допустимого индекса. Далее DISTINCT и обычные/многомерные FILTER/MAP/REDUCE: порядок, тип результата, ошибки пользовательской функции и неверные сигнатуры. Затем общая матрица и реестр отсутствующих возможностей. Общий аудит активен.


### Session 1150 — ошибка функции проходит через границу вызова

AW26-009 исправлен, совместный набор 254/254. Далее сверить оставшиеся Array-формы со свидетельствами, проверить ARRAYFILL и многомерные сортировки/развороты. Затем другие семейства общей матрицы и отсутствующие возможности. Все виды событий/ошибок пользовательских функций не объявлены проверенными.


### Session 1151 — сортировка на предельном индексе и реестр массивов

AW26-010 P1: общий GetStableSortedLine увеличивал INTEGER-индекс после обработки последнего элемента с индексом 2147483647. Переполнение приводило к обращению за границы массива. До исправления восемь независимых случаев ARRAYSORT/ARRAYTOSORTED по обеим осям падали в обоих движках. Цикл теперь завершает обход сразу после верхней границы.

ArraySortBoundaryContractTests содержит восемь проверок предельной границы; ArrayFillSortReverseContractTests содержит десять проверок сортировки и разворота по двум осям, сохранения исходника при копировании, заполнения через представление, пустого массива и единственного элемента на предельном индексе. Совместный адресный набор: 272/272. Полное решение: 4674/4674, девять TRX, без ошибок и пропусков, код процесса 0 (.artifacts/array-1151-full.log, .artifacts/test-results/array-1151-full/). git diff --check без ошибок.

Manual arrays-and-builtins прямо объясняет сортировку по осям, сохранение границ и отличие копирования от изменения исходника. Русскоязычный samples/functions/arraysort/maximum_line_index.axm выполнен актуальным CLI с кодом 0: копия 10/30, исходник до изменения 30, после сортировки 10/30. SHA256 библиотеки Runtime совпал с копией CLI. Редакционный ориентир: Зельднер, сортировка, страницы 147–148; запасной жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/system.array.sort?view=net-10.0 . Семантика проверена по Axiom.

ARRAY_REGISTRY_EVIDENCE_20261008.csv сопоставляет 38 зарегистрированных имён (53 регистрации в исходнике) с существующими наборами проверок. Это указатель свидетельств по именам, а не доказательство всех 53 форм вызова, типов, рангов и представлений. ARRAYFILL и сортировка/разворот по осям уже доступны существующим API; отдельные новые функции для проверенных сценариев не требуются. Новых токенов языка и измерений быстродействия нет.

Следующий шаг: проверить соответствие каждой из 53 зарегистрированных форм точным вызовам и результатам тестов, выделить неподтверждённые формы; затем продолжить незакрытые семейства общей матрицы и поиск полезных отсутствующих возможностей. Общий аудит остаётся активным.


### Session 1152 — проверка точности реестра свидетельств

Добавлен ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv: отдельная строка для каждой из 53 регистраций из BuiltinFunctions.Array.cs, имя, BuiltinId, число и имена параметров, строка исходника и кандидаты вызовов в тестах. Проверены число 53, число уникальных имён 38 и уникальность пары имя/число аргументов. Для всех форм найдены текстовые кандидаты. Это предварительный указатель: отрицательные проверки, интерполируемые программы и успешные вызовы пока не разделены; совпадение текста не доказывает выбранный Binder результат, ранг и фактическое исполнение.

Исправлена неточная ссылка ARRAYAVERAGE в ARRAY_REGISTRY_EVIDENCE_20261008.csv: ArrayAggregateContractTests не содержит прямых проверок среднего; фактическое свидетельство находится в AuditRemainingBehaviorTests. Прочитаны проверки остатка после сокращения, субнормального округления, предельных конечных значений и многомерного среднего. AuditRemainingBehaviorTests вместе с ArrayFillSortReverseContractTests повторно выполнены: 22/22, без ошибок и пропусков, код 0 (.artifacts/array-1152-evidence.log/TRX). Исполняемый код и поведение языка не менялись; повтор полного решения не требуется для этой правки указателей.

Следующий шаг: вручную проверить кандидаты всех 53 форм, записать конкретные успешные случаи, ожидаемый результат и проверку BuiltinId/типа, добавить независимые проверки непокрытых форм. После этого продолжить остальные семейства и отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1153 — успешные формы массивов без явной оси

Добавлен ArrayDefaultFormEvidenceTests: восемь форм ARRAYTOSORTED, ARRAYTOREVERSED, ARRAYSORT, ARRAYREVERSE, ARRAYPUSH, ARRAYINSERT, ARRAYPOP, ARRAYREMOVE без аргумента оси. Каждая проверяет точный BuiltinId, тип результата (одномерный INTEGER-массив, INTEGER или VOID), независимый ожидаемый порядок значений и исполнение обоих движков без перехода на интерпретатор. Исходный массив имеет границы 5..7 и значения 30/10/20; копирующие операции сохраняют первый исходный элемент, изменяющие дают ожидаемый порядок. Проверяется возврат 20 при POP и 10 при REMOVE(6).

Новый набор 8/8; вместе с ArrayWindowFormEvidenceTests — 65/65, без ошибок и пропусков, код 0 (.artifacts/array-1153-forms.log и .artifacts/array-1153-combined.log/TRX). Исполняемый код и договор языка не менялись. Дополнительный полный прогон решения для добавленных независимых тестов не выполнялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv внесены отдельные VerifiedEvidence и VerifiedScope: 23 из 53 форм подтверждены сочетанием точного связывания, типа и успешного ожидаемого вывода (15 форм границ/окон, 8 новых). Остальные 30 остаются кандидатами, а не считаются непроверенными вообще: их ранее накопленные проверки ещё требуют отдельного сопоставления. Все типы, ранги и представления этим результатом не доказаны. Новые языковые средства для данных сценариев не нужны: существующие формы работают.

Далее подтвердить остальные 30 форм, начиная с поиска и функций пользовательского преобразования, затем перейти к незакрытым семействам общей матрицы и отсутствующим возможностям. Общий аудит активен.


### Session 1154 — связывание форм поиска и координатных результатов

Добавлен ArraySearchFormEvidenceTests: 12 успешных случаев для семи зарегистрированных форм CONTAINS/COUNT/INDEXOF/LASTINDEXOF/FINDALL/BINARYSEARCH. Проверяются точный BuiltinId, скалярный или массивный тип, INTEGER-элементы и ранг координатного результата, независимые ожидаемые логические индексы и координаты в обоих движках без перехода на интерпретатор. Первое совпадение в одномерном массиве -4, последнее -3; многомерные координаты первого/последнего совпадения (-2,8)/(-1,7), FINDALL возвращает обе пары.

Первоначальная проба ошибочно ожидала многомерный ARRAYBINARYSEARCH без оси; Binder законно выдал MB2003. Это не дефект: многомерная форма требует dimension. Проба заменена успешным вызовом с осью 1; отдельная отрицательная проверка сохраняет MB2003 для отсутствующей оси. Итоговый набор вместе с ArraySearchContractTests: 35/35, без ошибок и пропусков, код 0 (.artifacts/array-1154-final.log/TRX). Исполняемый код/семантика не менялись; полный прогон не повторялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv подтверждено 30 из 53 форм в явно ограниченном объёме; 23 требуют дальнейшего сопоставления. Поиск координат уже доступен существующим API. Далее формы функций пользовательского преобразования, затем оставшиеся формы массивов и незакрытые семейства/отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1155 — типы результатов пользовательского преобразования массива

ArrayCallbackOrderContractTests дополнен шестью независимыми проверками точного BuiltinId и типа результата: EVERY/SOME возвращают BOOLEAN, FILTER/DISTINCT — одномерный INTEGER-массив, MAP с Label — двумерный STRING-массив, REDUCE со строковым начальным значением — STRING. Существующие успешные программы этих форм проверяют ожидаемые значения и порядок обхода; добавлены EVERY/SOME с ранней остановкой: FALSE/один вызов/след 1 и TRUE/два вызова/след 12 соответственно.

Совместно с ArrayCallbackBoundaryContractTests и ArrayDistinctValueContractTests выполнено 37/37 без ошибок и пропусков, код 0 (.artifacts/array-1155-callback.log/TRX). Включены прежние проверки передачи ошибки, вложенного вызова и FINALLY, пустых массивов, предельного индекса, точных LONG/DECIMAL и Unicode DISTINCT. Исполняемый код и семантика языка не менялись; полный прогон не повторялся.

Реестр ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv теперь содержит ограниченное подтверждение 36 из 53 форм. Тип возвращаемого значения MAP/REDUCE определяется пользовательской функцией/накопителем и уже поддерживается существующим API; добавлять отдельные STRING-варианты не требуется. Оставшиеся 17 форм: многомерные изменения/сортировки, заполнение, преобразование формы/соединение и агрегаты. Следующий шаг — точное сопоставление этих форм с существующими независимыми проверками и добавление отсутствующих проверок связывания; затем другие незакрытые семейства и отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1156 — завершена сверка 53 зарегистрированных форм массивов

ArrayRemainingFormEvidenceTests добавляет 17 проверок точного BuiltinId и типа/ранга результата для оставшихся форм: сортировка/разворот с осью, заполнение, многомерное изменение размера, FLATTEN/RESHAPE/CONCAT и четыре агрегата. Многомерные POP/REMOVE возвращают одномерный срез; MAP/RESHAPE/CONCAT не подменяются скалярным типом. Дополнительно INTEGER-матрица 1/2/0/4 исполняется обоими движками: SUM=7, MIN=0, MAX=4, AVERAGE=1.75, исходные значения сохранены.

Вместе с независимыми ArrayFillSortReverseContractTests, ArrayDynamicShapeContractTests, ArrayShapeContractTests, AuditRemainingBehaviorTests и ArrayAggregateContractTests выполнено 117/117, без ошибок и пропусков, код 0 (.artifacts/array-1156-remaining.log/TRX). Исполняемый код/семантика не менялись, полный прогон не повторялся. git diff --check без ошибок.

ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv содержит свидетельства точного связывания и успешного ожидаемого выполнения для всех 53 регистраций (38 имён). Это завершение сверки зарегистрированных форм в указанном тестами объёме, а не доказательство всех типов, рангов, представлений, размеров и возможных комбинаций. Предыдущие числовые/Unicode/пустые/предельные проверки остаются дополнительными свидетельствами. Новые дубли существующих операций массивов не нужны.

Следующий шаг общего аудита — вернуться к отсутствующим возможностям: независимо уточнить G02 (строгая проверка числового текста) и существующие альтернативы, не менять префиксный VAL; затем G03 двоичный доступ и G04 Unicode с проверкой реальных сценариев. Остальные семейства общей матрицы также остаются в объёме. Общий аудит активен.


### Session 1157 — независимая проверка альтернативы строгому числовому предикату

NumericTextAlternativeEvidenceTests проверяет девять строк обоими движками: 0, abc, 12abc, 1e999, -1e999, 1e-999, 0e999, .5, -12.5e2. Полная ASCII-грамматика числа через REGEXISMATCH отличает текстовый хвост, но принимает переполнение 1e999; VAL возвращает 0 для настоящего нуля, нечислового текста, переполнения и округления слишком малого значения. 12abc возвращает числовой префикс 12. Это действующий договор VAL, не новый дефект.

Адресный набор 9/9, без ошибок и пропусков, код 0 (.artifacts/numeric-1157-alternative.log/TRX). ParseVal в BuiltinStringHelpers.cs сначала пытается полный инвариантный разбор, затем префиксный, не принимает бесконечный результат и возвращает 0. BuiltinIOHelpers.TryParseInputField для DOUBLE уже проверяет полный ввод и конечность, но INPUT не является преобразованием произвольной имеющейся STRING. В зарегистрированных именах ISNUMERIC не найден. Регулярное выражение проверяет грамматику; успешное представление конечным DOUBLE требует дополнительной логики.

G02 сохраняется как полезная отсутствующая возможность, а не поломка VAL и не обязательное копирование библиотеки VB. Следующий шаг: проверить BASIC-прецедент имени ISNUMERIC и определить узкий договор полного инвариантного конечного DOUBLE для STRING, согласовать пробелы/подтекание к нулю/групповые разделители с существующими правилами. После этого реализовать предикат с обоими движками, подсветкой, manual и русскоязычным запускаемым примером. Строгое преобразование и новые типовые перегрузки не входят в выбранный шаг. Общий аудит активен.


### Session 1158 — ISNUMERIC: полный числовой текст

G02 реализован в узком объёме: ISNUMERIC(text AS STRING) AS BOOLEAN проверяет полный инвариантный десятичный разбор в конечный DOUBLE. Использует общий BuiltinStringHelpers.IsNumeric для Interpreter и BytecodeVm (NumberStyles.Float, InvariantCulture, double.IsFinite). Обычные краевые пробелы U+0020 и U+0009–U+000D, знак, точка и экспонента разрешены; запятая, групповые разделители, валютные знаки, формы &H/&O, Unicode-цифры, NaN/Infinity и переполнение не принимаются. Пустая строка FALSE. Округление очень малого числа в конечный ноль TRUE; проверка не обещает точности либо пригодности для INTEGER/LONG/DECIMAL. Префиксный VAL сохранён. Новая функция не вводит строгого преобразования либо типовых перегрузок.

BuiltinFunctionId.IsNumeric добавлен в конец enum, без изменения существующих числовых значений сериализации. Дескриптор CreateDefault требует STRING и возвращает BOOLEAN. Оба движка используют один обработчик. Обе схемы IDE обновлены в алфавитном порядке; форматирование и словарь проверяются отдельной проверкой IsNumericIsHighlightedAndFormattedAsBuiltin и общей синхронизацией реестра.

IsNumericContractTests: 30 числовых/текстовых случаев в en-US/ru-RU/tr-TR и обоих движках, четыре неверные формы аргументов, точный BuiltinId/BOOLEAN и сохранение VAL. Совместно с NumericTextAlternativeEvidenceTests адресный набор 44/44. Первоначальная сборка выявила отсутствующий using, исправлен; первоначальные две проверки IDE выявили порядок слова в схемах, исправлен. Это ошибки текущего внесения, не отдельные ранее существовавшие дефекты. Полный прогон решения запущен: .artifacts/numeric-1158-full.log/TRX; результат будет дописан после завершения.

Manual isnumeric.html содержит назначение, контекст, синтаксис, аргумент, результат, ограничения, диагностику и inline-пример. Обновлены навигация, поисковый/алфавитный индекс и ссылки из числовых разделов. Русскоязычный samples/functions/isnumeric/full_number_check.axm показывает полный текст против префикса VAL, ноль, переполнение, точку/запятую и округление к нулю; выполнение будет подтверждено актуальным CLI.

Имя выбрано по подтверждённому прецеденту Visual Basic/VBA, а не заявлено как функция классического QuickBASIC. Поиск в доступном тексте Зельднера не выявил ISNUMERIC; VAL не предоставляет полного предиката. Основной жанровый/именной ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.information.isnumeric?view=net-10.0 и VBA https://learn.microsoft.com/en-us/office/vba/language/reference/user-interface-help/isnumeric-function . Договор Axiom сознательно уже VB: только STRING, конечный инвариантный десятичный DOUBLE; числовые/BOOLEAN-аргументы и шестнадцатеричные/восьмеричные формы VB не перенесены.

После завершения проверок следующий шаг — G03: независимо проверить отсутствие общего двоичного доступа и реальные сценарии, затем G04 Unicode и прочие незакрытые семейства общей матрицы. Общий аудит активен.


Итог Session 1158: окончательная версия с запретом U+0000 прошла полное решение 4774/4774, девять TRX, без ошибок и пропусков, код 0 (.artifacts/numeric-1158-final-full.log/TRX). Адресный набор 49/49. IDE 601/601, включая словарь/форматирование новой функции. Русскоязычный full_number_check.axm выполнен актуальным CLI с кодом 0: FALSE/12/TRUE/FALSE/TRUE/FALSE/TRUE; SHA256 Runtime совпал с актуальной библиотекой. Отдельная CLI-проба U+0000/U+00A0/U+0009 дала FALSE/FALSE/TRUE. git diff --check чистый.

Общий audit-bytecode-coverage выполнен актуальной сборкой инструмента: все зарегистрированные варианты имеют путь VM; ISNUMERIC не указан среди пропусков manual или samples. Общий результат FAIL: 22 отсутствующих записи manual-индекса, 27 отсутствующих тематических каталогов samples и два нераспознанных заголовка «Функция PMAP»/«Функция POINT», относящиеся к графическим функциям. Полный отчёт .artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-191052.txt. Эти расхождения не исправлены данной числовой правкой и должны быть разобраны отдельно в общем аудите. Общий аудит не объявляется завершённым.

Дальше: G03/G04 и отдельная сверка отмеченных графических manual/sample-пропусков с фактическими страницами и тематическими примерами. G02 закрыт в выбранном узком объёме ISNUMERIC; строгое преобразование произвольного целевого типа не добавлялось.


### Session 1159 — исправлена полнота инструмента проверки покрытия

COV26-001 P2: BytecodeCoverageAudit перечислял enum и TryGetDescriptor(id), который возвращает только первый дескриптор ID. Поэтому перегрузки с тем же ID и другим числом аргументов не проверялись, хотя отчёт обещал все варианты. Теперь инструмент перечисляет BuiltinFunctions.All и TryGetDescriptors(name), проверяет каждую регистрацию и выводит число вариантов: текущий реестр 250 имён/310 вариантов.

COV26-002 P3: индекс распознавался только по kind «Функция» и полному совпадению title с именем. Заголовки «Функция PMAP/POINT» и шесть групповых записей kind «Функции» ошибочно давали 22 пропуска и две лишние записи. В групповых строках search-index.js добавлено явное поле functions с именами сущностей. Инструмент поддерживает это поле и нормализует префикс «Функция » одиночного заголовка. Неизвестное явно указанное имя по-прежнему является ошибкой; произвольные ключевые слова не считаются свидетельством документации.

BytecodeCoverageAuditContractTests проверяет групповой индекс/префикс/полный счёт вариантов, неизвестное имя и сохранение отказа при отсутствующем тематическом каталоге. Три проверки 3/3, код 0 (.artifacts/coverage-1159-contract.log/TRX). Исходник инструмента подключён в тестовый проект через Compile Link; исполняемый язык не менялся. git diff --check без ошибок. Полное решение после этой правки инструмента не повторялось.

Фактический audit-bytecode-coverage: все 310 вариантов имеют VM-путь; все 250 имён найдены в manual-индексе, лишних индексных имён нет. Общий FAIL сохраняется из-за 27 отсутствующих каталогов samples/functions/<имя> (.artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-191448.txt). Наличие каталога само по себе не доказывает запуск/содержание, а его отсутствие не означает отсутствие примеров в другом тематическом разделе.

GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит кандидаты для этих 27 имён: для 18 найден текст вызова в существующих samples, для девяти прямого текстового кандидата нет. Это не подтверждение выполнения: комментарии, ошибки, зависимые ресурсы и графический контекст требуют чтения/запуска. При чтении встретились старые CP1251-примеры; они не изменялись.

Следующий шаг: разобрать девять имён без прямого кандидата и 18 существующих графических кандидатов, добавлять только содержательные короткие тематические примеры, не дублировать большие игры ради зелёного отчёта. G03/G04 и остальные семейства общего аудита остаются в объёме. Общий аудит активен.


### Session 1160 — запускаемые примеры свойств TILESET и TILEMAP

Для девяти функций без прямого текстового кандидата добавлены короткие русскоязычные примеры в соответствующих samples/functions/: TILECOUNT/TILEWIDTH/TILEHEIGHT — atlas_property.axm; MAPPIXELWIDTH/MAPPIXELHEIGHT/MAPTILEWIDTH/MAPTILEHEIGHT — map_property.axm; MAPTILESET — cell_tileset_index.axm; MAPTILEFLAGS — changed_cell_flags.axm. Примеры используют существующие graphics/sprites/assets/walk_sheet.png (32×32, четыре тайла 16×16) и operators/drawtilemap/simple_map.tmj (2×2 клетки). Новые ресурсы/копии файлов не созданы.

TilePropertyAcceptanceSampleTests читает настоящие файлы .axm, запускает ProgramRunService с ProgramConsoleService и sourceFilePath, то есть использует реальный загрузчик ресурсов IDE и разрешение относительных путей. Все девять программ выполнены в Interpreter и Bytecode без перехода на другой движок: 18/18, код 0 (.artifacts/graphics-1160-acceptance.log/TRX). Ожидаемые результаты: TILECOUNT=4, TILEWIDTH/HEIGHT=16, MAPPIXELWIDTH/HEIGHT=32, MAPTILEWIDTH/HEIGHT=16; MAPTILESET для заполненной/пустой/внешней клетки 1/0/0; MAPTILEFLAGS до изменения/после SETMAPTILE с маской 3/вне карты 0/3/0. Графическое окно не требуется, визуальная отрисовка этим не доказана. CLI не загружает TILESET/TILEMAP, потому проверка сделана через IDE. Файл карты не перезаписывается.

Manual tilesets.html/tilemaps.html дополнен ссылками и объяснением запуска через IDE; семантика не менялась. Для современных ресурсов у Зельднера нет прямого раздела; ближайший официальный жанровый ориентир для доступа к ресурсам и их свойствам — https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/objects/my-resources-object . Семантика .NET-ресурсов в Axiom не переносилась.

Реестр GRAPHICS_SAMPLE_CANDIDATES_20261008.csv обновлён: девять VerifiedSample с точным объёмом подтверждения. Повторный audit-bytecode-coverage (.artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-192017.txt): 250 имён/310 вариантов, индекс полный, VM-пути полные; отсутствующих каталогов стало 18 вместо 27. Общий FAIL пока сохраняется. git diff --check чистый. Полное решение после добавления этих примеров не повторялось; исполняемый код не менялся.

Далее разобрать оставшиеся 18 графических кандидатов, сделать содержательные тематические примеры и подтвердить исполнение в нужном контексте. G03/G04 и прочие семейства общего аудита остаются в объёме. Общий аудит активен.


### Session 1161 — закрыты оставшиеся каталоги графических функций

Добавлены 18 коротких русскоязычных примеров: IMAGEWIDTH/HEIGHT/LOADIMAGE, SPRITEWIDTH/HEIGHT/COUNT/LOADSPRITE, LOADTILESET/LOADTILEMAP, MAPWIDTH/HEIGHT/LAYERCOUNT/TILE, RGB, SCREENISACTIVE/MODE/WIDTH/HEIGHT. Ресурсные примеры используют существующие walk_sheet.png и simple_map.tmj, не копируют ресурсы и не требуют графического окна. RGB(1,2,3)=66051. Размер изображения 32×32, кадр 16×16, четыре кадра, карта 2×2 и три слоя; MAPTILE(1,2,1)=2.

TilePropertyAcceptanceSampleTests теперь исполняет все 27 тематических программ в обоих движках через настоящий ProgramRunService/ProgramConsoleService и разрешение sourceFilePath, без перехода на другой движок. Дополнительная пара проверок различает исходный нулевой размер экрана и явный SCREEN 0 с размером 640×300. Итог 56/56, код 0 (.artifacts/graphics-1161-final.log/TRX). Первоначальные ошибки примеров — зарезервированные image/sprite как имена переменных и ошибочное ожидание обычной панели вывода после SCREEN 0 — исправлены, дефектами языка не считались. После отказа от явного SCREEN выявлено неверное ожидание размеров 640×300 до его вызова: RuntimeInputSnapshot действительно начинается с 0×0. Ожидание сверено с кодом, отдельно проверено состояние после SCREEN 0; исполнитель не менялся.

Manual screen-functions уточняет: до первого оператора SCREEN размеры 0×0, после SCREEN 0 — 640×300, SCREENISACTIVE=FALSE. PRINT после SCREEN 0 направляется в экранное текстовое окно. В шести графических разделах добавлены ссылки на примеры; индексные имена уже присутствовали. GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит подтверждённые пути для всех 27 исходных пропусков и пределы проверки. Визуальная отрисовка/масштабирование/все графические формы этими примерами не доказаны.

Редакционный ориентир по экранному режиму — Зельднер, SCREEN, страница 228; современный запасной жанровый ориентир по свойствам экрана — https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.devices.computer.screen?view=windowsdesktop-10.0 . Для ресурсов сохранён ориентир My.Resources, использованный в Session 1160. Исторические режимы/семантика VB в Axiom не перенесены.

Повторный audit-bytecode-coverage: PASS (.artifacts/graphics-1161-coverage.log и указанный в нём отчёт). 250 имён, 310 вариантов, 250 индексных имён, 250 тематических каталогов; пропусков VM/индекса/каталогов и лишних имён нет. Это структурная проверка, а не доказательство всех форм, типов и сценариев; отдельно подтверждены только явно перечисленные программы. git diff --check чистый. Полное решение после добавления этих примеров не повторялось; исполняемый код не менялся.

Следующий шаг — G03: проверить границы существующих файловых API и потребность в общем двоичном доступе; затем G04 Unicode и оставшиеся семейства общего аудита. Не добавлять новые средства по одному лишь отсутствию имени в реестре. Общий аудит активен.


### Session 1162 — G03: существующие пути сохранения байтов

BinaryFileAlternativeEvidenceTests выполняет COPYFILE и READFILE→WRITEFILE с явной WINDOWS-1251 для шести наборов: пустой файл, все 256 значений байта, те же значения после UTF-8/UTF-16LE/UTF-16BE BOM-последовательностей, повторные NUL/CR/LF/0x1A/0xFF/0x80/0x98. Для каждой программы проверены ожидаемый LEN, неизменность исходника, точные байты копии и повторной записи. Оба движка без перехода на другой: 12/12, код 0 (.artifacts/binary-1162-alternative.log/TRX).

Уточнение G03: прежнее «общего пути нет» слишком широко. COPYFILE уже копирует произвольные байты. Явная WINDOWS-1251 обратимо сохраняет проверенные байтовые наборы при чтении/записи целой STRING, не распознаёт Unicode BOM, один исходный байт даёт одну позицию строки. Эта возможность появилась после G01; вывод исходного реестра потребностей необходимо читать с этим уточнением. Это обходной путь через текстовую кодировку, а не числовой массив байтов: ASC использует CP866, ASCW возвращает Unicode-код, они не являются индексами байтов Windows-1251. READFILE/WRITEFILE держат весь файл/строку в памяти; позиционные бинарные операции и байтовый FILE-договор не появились.

G03 классифицирован точнее: сохранение и копирование данных уже доступны; прямое чтение/изменение массива значений 0..255 и позиционный двоичный интерфейс действительно отсутствуют. Для текущих сценариев ресурсов есть IMAGE/SPRITE/TILESET/TILEMAP-загрузчики, отдельная обязательная потребность в общем бинарном редактировании PROJECT.md не установлена. Новый OPEN FOR BINARY/GET/PUT не вводится по одному факту отсутствия имени; остаётся отложенным направлением при конкретном формате/сценарии. Это не регрессия текстового FILE.

Добавлен русскоязычный samples/functions/readfile/single_byte_round_trip.axm и его ресурс byte_values.bin (байты 0..255). Пример создаёт уникальный временный файл, сохраняет строку той же кодировкой, проверяет равенство и удаляет только этот файл. CLI выполнен из каталога примера, код 0: 256/TRUE/TRUE/TRUE. SHA256 Runtime совпал с актуальной библиотекой. Первоначальный запуск CLI из корня не нашёл относительный ресурс, поскольку CLI использует текущий каталог; это не ошибка READFILE. В IDE путь разрешается относительно запускаемого исходника.

Исполняемый код/семантика не менялись; полный прогон не повторялся. git diff --check чистый. Следующий шаг — G04: проверить полезность дополнительных Unicode-единиц и существующие альтернативы, отличая UTF-16-договор от дефекта. Общий аудит активен.


### Session 1163 — G04: Unicode-коды и составные символы

G04 проверен на 12 строках: пустая строка, кириллица, одиночный эмодзи, отдельный комбинируемый знак, семейная ZWJ-последовательность, флаги, модификатор цвета кожи, составная клавиша, смешанная строка, нечётная последовательность региональных индикаторов, Hangul Jamo и начальный комбинируемый знак. UnicodeUnitAlternativeEvidenceTests задаёт независимые ожидаемые количества единиц UTF-16, Unicode-кодов и графем. Существующий цикл ASCW(MID(...,2))→CHRW восстанавливает каждую проверенную корректную строку и считает Unicode-коды в обоих движках без перехода на другой. Отдельная проверка показывает, что этот цикл не сохраняет составной видимый символ при выделении первого кода; LEFT("😀",1) по существующему договору возвращает одну единицу UTF-16, ASCW для неё возвращает 65533. Это не регрессия UTF-16-функций.

Итог: 13/13, пропущенных и ошибочных проверок нет (.artifacts/unicode-1163-final.log/TRX). StringInfo.ParseCombiningCharacters совпал с независимыми ожидаемыми графемными границами на этом корпусе. Это проверка возможного средства реализации, а не появившаяся функция Axiom и не доказательство соответствия всей последней версии Unicode. Ориентиры: https://www.unicode.org/reports/tr29/ и https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo?view=net-10.0 .

Добавлен samples/functions/ascw/scalar_count_loop.axm. CLI с совпадающим SHA256 Runtime завершился с кодом 0: длина UTF-16 11, число Unicode-кодов 7, восстановление TRUE, равенство первого кода целому семейному эмодзи FALSE. Пример показывает уже доступный путь для корректных Unicode-строк; повреждённые суррогатные последовательности этим утверждением не охвачены.

Классификация G04 уточнена: обход Unicode-кодов уже возможен существующим API; отдельная полезная отсутствующая возможность — длина и выделение частей строки по графемам без разрыва комбинируемых знаков и ZWJ-последовательностей. Следующий шаг — согласованные дополнительные формы существующих LEN/LEFT/RIGHT/MID с явным выбором единицы, сохраняя прежние формы в UTF-16. Имена из семейства BASIC сохраняются; новые самостоятельные аналоги функций не нужны. Точный договор новых форм должен быть зафиксирован до изменения исполнителей. Новые формы пока не реализованы.

Исполняемый код не менялся, полный прогон решения не повторялся. Общий аудит остаётся активным.


### Session 1164 — явные единицы строковых функций

Реализованы дополнительные формы LEN(text, unit), LEFT(text, count, unit), RIGHT(text, count, unit), MID(text, start, count, unit). Допустимы UTF16/CODEPOINT/GRAPHEME без учёта регистра и без удаления пробелов. Существующие формы не изменены. count остаётся строго положительным, start начинается с 1, чрезмерная длина ограничивается концом строки. Неизвестный unit даёт MB3007; неверный тип отклоняется до исполнения. Общий BuiltinStringUnitHelpers используется обоими движками; идентификаторы добавлены в конец BuiltinFunctionId. Новые пользовательские имена и ключевые слова не введены.

Режим CODEPOINT сохраняет корректные суррогатные пары и отдельные повреждённые единицы без замены; для LEN подсчёт не создаёт массив границ. GRAPHEME использует StringInfo текущей платформы .NET. Нормализация, пиксельная ширина и соответствие всей самой новой версии Unicode не обещаны. Старые позиции INSTR остаются в UTF-16.

UnicodeStringUnitTests проверяет независимые длины, разные единицы выделения, семейный эмодзи/флаг/комбинируемый знак, пустую строку, INTEGER.MAX_VALUE, ошибки аргументов и повреждённые суррогаты в обоих движках без перехода на другой. Добавлены четыре unicode_units.axm в каталогах len/left/right/mid; все выполнены CLI с совпадающей библиотекой Runtime и кодом 0. В тесты дополнительно включено чтение этих реальных программ.

Manual string-functions-slices-and-length описывает новые формы непосредственно на странице, отдельно задаёт аргументы, ограничения, диагностику и короткие примеры. Поисковый индекс расширен Unicode-терминами. Оба файла подсветки сохраняют существующие имена; добавлены пояснения о строковом unit и проверки форматирования четырёх форм. Первичный редакционный ориентир — Зельднер, LEFT$/RIGHT$/MID$, страницы 298–300; запасные — официальные VB Strings.Len и Strings.Mid. Историческое разрешение count=0 из QuickBASIC не переносится в текущий договор Axiom.

Первоначально 24/24 целевых проверок прошли; расширенный целевой набор дал 44/44, но xUnit свёл два повреждённых суррогатных входа к одному идентификатору. Источники входов исправлены на числовые варианты, чтобы оба случая проверялись отдельно. Полный прогон выявил две устаревшие проверки числа аргументов LEN/RIGHT: исправлены на действительно лишние аргументы. Также обнаружен неправильный контекст каталога ресурса в общем прогоне single_byte_round_trip.axm из Session 1162: тестовый поставщик теперь получает каталог программы; READFILE не изменялся.

На момент записи полный прогон ещё выполняется, итоговый успех не заявлен. После его завершения нужен повторный прогон с исправленными проверками и актуальным подсчётом CODEPOINT, затем актуальная сборка инструмента покрытия. Общий аудит активен.


Завершение проверки Session 1164: окончательный прогон .artifacts/unicode-1164-verified-full.log содержит 9 TRX, 4907/4907, ошибок 0, пропусков 0; 49 результатов относятся к UnicodeStringUnitTests/UnicodeUnitAlternativeEvidenceTests, включая реальные четыре .axm-программы в обоих движках и раздельные повреждённые суррогаты. Предыдущие неуспешные прогоны сохранены как свидетельство исправления проверок. Последняя ошибка файлового примера была исправлена установкой его рабочего каталога внутри существующего WorkingDirectoryLock с восстановлением каталога в finally. Передача resourceBaseDirectory сама по себе не влияет на файловый READFILE; предварительный вывод выше уточнён. Код языка по этому поводу не изменялся.

Axiom.ManualChecks пересобран последовательно после окончания тестов, сборка завершилась без ошибок. SHA256 библиотек Runtime у инструмента и CLI совпал с текущей библиотекой. audit-bytecode-coverage: PASS (.artifacts/unicode-1164-coverage.log), 250 имён и 314 зарегистрированных вариантов, полный индекс и тематические каталоги. Это структурное свидетельство; 49 Unicode-проверок — отдельное функциональное свидетельство. Все четыре unicode_units.axm повторно выполнены актуальным CLI с кодом 0. Проверены ссылки manual на эти файлы и git diff --check.

G04 в выбранном договоре длины и выделения фрагментов реализован и проверен. Редакционные запасные источники: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.strings.len?view=net-10.0 и https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.strings.mid?view=net-10.0 . Следующий шаг общего аудита — числовые границы устойчивого вычисления расстояния: независимо проверить SQR(x*x+y*y) при больших/малых DOUBLE, существующие средства и возможность короткой FUNCTION, прежде чем решать вопрос о HYPOT. Остальные семейства и требования общего аудита не объявлены завершёнными; цель активна.


### Session 1165 — устойчивое расстояние существующими средствами

Проверен кандидат HYPOT из LANGUAGE_API_GAPS_20261007.md. Прямая формула SQR(x*x+y*y) при x=3e200, y=4e200 даёт MB3008 на промежуточном умножении, хотя расстояние около 5e200 конечно. При x=3e-200, y=4e-200 оба квадрата округляются до 0 и формула возвращает 0. Это следует из текущей арифметики DOUBLE, а не поломки SQR.

Короткая пользовательская FUNCTION StableDistance использует scale=MAX(ABS(x),ABS(y)), отдельно обрабатывает нулевой вектор, делит координаты на scale и умножает scale на SQR суммы нормированных квадратов. StableDistanceAlternativeEvidenceTests подтверждает десять конечных пар: ноль, 3/4, отрицательные 3/4, большие и малые 3/4, 1e308/1e308, резко разные масштабы, DOUBLE.MaxValue с нулём, минимальное положительное значение с нулём и субнормальный треугольник 3/4/5. Допуск до четырёх соседних представимых DOUBLE задан независимо от алгоритма. Две проверки переполнения различают промежуточное умножение и действительно непредставимое расстояние 1.7e308/1.7e308. Проверена и потеря малой ненулевой величины прямой формулой.

Итог 13/13, оба движка без перехода на другой (.artifacts/numeric-1165-distance.log/TRX). Добавлен русскоязычный samples/functions/sqr/scaled_distance.axm, выполнен актуальным CLI с совпадающим SHA256 Runtime: 4.9999999999999995E+200 / 5E-200 / 0, код 0. Начальные черновики с научными литералами и однострочным IF исправлены по существующему синтаксису: показатель степени передаётся строкой в VAL, IF имеет END IF. Эти ошибки черновика не считались дефектами языка.

Кандидат HYPOT уточнён: устойчивое вычисление уже выразимо существующими ABS/MAX/SQR и FUNCTION. Новая встроенная функция на этом этапе не вводится; это возможное удобство при повторяемом сценарии и отдельном договоре точности. Устойчивость на проверенном корпусе не доказывает правильное округление для каждой пары DOUBLE, не расширяет диапазон представимых результатов и не задаёт договор для NaN/Infinity.

Manual numeric-functions-roots-rounding дополнен прямо в разделе SQR: промежуточное переполнение, потеря малой величины, масштабирование, inline-функция, диагностики и ссылка на программу. Главный редакционный ориентир — Зельднер, SQR, страницы 277–278; запасной жанровый — https://learn.microsoft.com/en-us/dotnet/api/system.math.sqrt?view=net-10.0 (включая пример VB). Платформенное возвращение NaN для отрицательного аргумента в Axiom не переносится: действует MB3007.

Исполняемый код языка и подсветка не менялись, новых токенов нет. Полное решение после этих проверок и примера не повторялось; последняя полная проверка Session 1164 — 4907/4907. git diff --check чистый. Следующий шаг — кандидат равномерного случайного целого в диапазоне: сначала проверить реальный сценарий, существующие средства и ограничения INT/RND/LONG, прежде чем вводить новое имя. Общий аудит активен.


### Session 1166 — случайные целые и повторный посев CLI

Проверен кандидат равномерного случайного целого в диапазоне. RandomIntegerAlternativeEvidenceTests: 13/13, оба движка без перехода на другой (.artifacts/random-1166-evidence.log/TRX). Управляемые допустимые значения RND 0/0.5/ближайшее DOUBLE ниже 1 дают правильные края 1..10. Полные INTEGER/LONG-ширины выявляют ожидаемое MB3008 арифметики, а INT при ширине 4294967296 не может вернуть значение 2147483648. Это ограничения текущих типов, не поломка RND.

Для полного INTEGER-домена уже работает width AS LONG и CLNG(FLOOR(RND()*width))+low. Для low=9007199254740992 прибавление offset до CLNG теряет соседнее значение, а преобразование смещения до сложения сохраняет 9007199254740993. Проверена известная восьмиточечная сетка: INT(RND()*3) распределяет равновесные входы как 3/3/2. Это свидетельство преобразования на заданном источнике, не статистическая оценка настоящего генератора. Пользовательская FUNCTION с отбрасыванием хвоста уже выразима, если сетка источника известна; её использование не доказывает универсальное равномерное преобразование произвольного IRuntimeRandom. Повторный посев в обоих движках проверен на отдельном реальном System.Random, внедрённом в запрос, без общего изменяемого генератора между тестами.

Обычный игровой сценарий подтверждён существующим snake_arrow_controls.axm: выбор столбца/строки через INT(RND()*cols)+1. Обязательная текущая потребность в полном равномерном LONG-диапазоне не установлена. Универсального целого случайного API в языке действительно нет, но оно остаётся отложенным удобством, требующим договора энтропии, границ и имени по BASIC. Новое имя не вводится.

Добавлен русскоязычный samples/functions/rnd/long_range_conversion.axm и прямое описание ограничений/порядка CLNG в manual numeric-functions-core. Первый реальный CLI-запуск показал FALSE при повторном посеве. Подтверждён RNG26-001 P2: ConsoleHost.NextRandomDouble использовал Random.Shared, а Reseed был пустым. Комментарий объявлял это ограничением консоли, но оно противоречит IRuntimeRandom, RANDOMIZE и обычному текстовому запуску; headless-контекст не требует отказа от генератора.

Пять регрессионных CLI-проверок с seed 0/42/43/INTEGER.MinValue/INTEGER.MaxValue сначала дали 0/5 (.artifacts/random-1166-cli-before.log/TRX). Проверки запускают настоящий CliApp.Run, сравнивают две серии внутри одного запуска и два независимых запуска с текущим System.Random(seed). ConsoleHost теперь владеет отдельным Random, а Reseed создаёт Random(seed). Интерфейс и словарь языка не расширены. Manual RANDOMIZE отражает повторяемость CLI и границу текущей версии платформы.

Редакционный ориентир — Зельднер, RND и формула игрового диапазона, страницы 284–285; запасной современный VB-ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.vbmath.rnd?view=net-10.0 . Платформенный договор https://learn.microsoft.com/en-us/dotnet/api/system.random.nextdouble?view=net-10.0 использован только для NextDouble; исторические аргументы RND(n) в Axiom не перенесены.

Полный прогон random-1166-full начат и ещё выполняется; успех исправления до результатов не объявлен. Следующий шаг — подтвердить пять CLI-проверок и реальный .axm-пример на собранном исправлении, записать итог полного решения. Затем проверить кандидат относительного пути: имеющиеся FULLPATH/COMBINEPATH/корневые функции и потребность проекта, не вводить API лишь из-за отсутствия имени. Общий аудит активен.


Завершение Session 1166: .artifacts/random-1166-full.log и 9 TRX подтверждают 4940/4940, ошибок 0, пропусков 0. Все 13 RandomIntegerAlternativeEvidenceTests и пять регрессионных случаев CLI прошли; полный набор CLI 28/28. RNG26-001 исправлен и проверен: одинаковые зерна повторяют последовательность внутри запуска и при новом CliApp.Run.

Повторный реальный запуск samples/functions/rnd/long_range_conversion.axm после сборки дал 722011773 / TRUE / TRUE, код 0; SHA256 Runtime CLI совпал с текущей библиотекой. До исправления последняя проверка повторяемости давала FALSE. git diff --check чистый. Программа является проверкой диапазона и посева, а не доказательством строгой равномерности.

Классификация кандидата: малые игровые индексы доступны через RND/INT; широкий INTEGER-домен и большие LONG-смещения — через существующие FLOOR/CLNG с правильным порядком преобразования; универсальный равномерный LONG×LONG→LONG API действительно отсутствует, но текущая обязательная потребность в нём не установлена. Он отложен до реального сценария и договора энтропии, границ и исторического имени. Исправление CLI относится к существующему RANDOMIZE, а не к новому генератору языка. Следующий шаг — относительные пути и существующие файловые альтернативы. Общий аудит активен.


### Session 1167 — каталог программы при разрешении Windows-путей

PATH26-001 P2: неполные Windows-пути ошибочно разрешались от процесса после Path.Combine. Подтверждены корневая запись на другом диске, drive-relative и UNC в обоих движках: первоначально 0/8, после Path.GetFullPath(expanded, baseDirectory) целевой расширенный набор 18/18. Добавлен samples/functions/fullpath/drive_relative_scope.axm, проверка настоящего файла и запускаемого исходника, прямое описание в manual и поисковом индексе. Подробности и пределы доказательства — audit/RELATIVE_PATH_ALTERNATIVES_20261008.md. Полный прогон path-1167-full пока выполняется; итог и CLI ещё требуется записать. Общий относительный маршрут между двумя абсолютными путями остаётся отсутствующим необязательным API; существующие FULLPATH/COMBINEPATH уже решают нормализацию известных ссылок. Общий аудит активен.


Завершение Session 1167: path-1167-full.log и 9 TRX подтверждают 4960/4960, ошибок 0, пропусков 0. Все 19 PathBaseResolutionEvidenceTests прошли, включая реальный запуск drive_relative_scope.axm в обоих движках; прежние восемь независимых случаев не проходили до исправления. PATH26-001 исправлен и подтверждён.

Актуальный CLI с совпадающим SHA256 Runtime выполнил samples/functions/fullpath/drive_relative_scope.axm с кодом 0: все шесть проверок TRUE, в том числе восстановление исходной папки и удаление созданной пустой временной папки. Проверены новые ссылки manual и git diff --check. Лексические D:/UNC-примеры не доказывают доступность второго диска или сетевого ресурса; фактический READFILE проверен на локальном временном файле.

Общий построитель относительного маршрута не добавлен: текущие разрешение и соединение ссылок доступны, отдельный необязательный API между двумя абсолютными путями отсутствует и отложен до сценария. Следующий шаг — сверить сквозную матрицу общего аудита с G04, RNG26-001 и PATH26-001, определить ещё не подтверждённые семейства по формам и поведению, а не только по наличию имён. Общий аудит активен.


### Session 1168 — текущий реестр и границы доказательств

Сверен текущий реестр: 314 дескрипторов, 250 имён, 299 зарегистрированных ID из 302. Все формы совпали с актуальными метаданными CLI; поддержка VM, отсутствие повторных сигнатур и обе схемы подсветки подтверждены структурно. Старые снимки сохранены. Исторический LA26-список больше не превращается автоматически в текущий BROKEN: HistoricalIssue отделён от UNVERIFIED_PER_FORM.

Добавлен режим --inventory-only и экспорт текущей таблицы без запуска старых сценариев. Новые документы: audit/LANGUAGE_FUNCTION_MATRIX_20261008.csv, audit/LANGUAGE_REGISTRY_RECONCILIATION_20261008.json, audit/CURRENT_FORM_AUDIT_20261008.md. У 190 форм есть кандидаты ссылок на специализированные CSV, у 124 таких ссылок нет; это не показатели семантической полноты или числа дефектов. Доказательства G04, RNG26-001 и PATH26-001 перечислены с границами; упоминания в тестах не повышены до проверки формы.

Проверки: инструмент собран и выполнен с кодом 0; экспорт сверил все параметры и политики с metadata, SHA256 Semantics совпал. Последний полный прогон остаётся 4960/4960 из Session 1167, девять TRX, без ошибок/пропусков; в этой сессии полный набор не повторялся, поведение языка не менялось. Следующий шаг — независимые проверки SEEKFILE/FILEPOS/FILELENGTH/EOF и READTEXT/READLINE на кодировках, Unicode и позициях. Общий аудит активен.


### Session 1169 — Unicode на границах буфера файлового чтения

Добавлены 36 независимых FilePositionBufferEvidenceTests: два движка без перехода на другой, UTF-8/UTF-16LE/UTF-16BE с BOM, emoji после 0/127/128/254/255/256 ASCII-символов, CRLF, кириллица, байтовые позиции и длина, LINENUMBER, EOF, REWIND/READTOEND и сохранение позиции после отрицательного/слишком большого SEEKFILE. Ожидаемые позиции выведены из известного формата байтов, а не из проверяемого RuntimeTextFile.

Первый прогон 0/36 остановился на неверном ожидании нового теста: READLINE в конце файла обязан выдавать MB3007; READTEXT возвращает пустую строку. По диагностике и общему EvaluateReadLine подтверждено, что это существующий договор, а не дефект буфера. Ожидание исправлено, код языка не изменён. Новый набор 36/36; вместе с ReadTextUnicodeContractTests, ExplicitTextEncodingTests, FileLineEndingContractTests и FileWriteUnicodeContractTests — 78/78, ошибок/пропусков 0, .artifacts/test-results/file-stream-1169/file-stream-1169.trx.

Точные формы и пределы записаны в audit/FILE_STREAM_FORM_EVIDENCE_20261008.csv. Не заявлена полнота всех файловых средств: изменяемые извне файлы, LONG-границы, весь набор режимов и неправильных типов требуют отдельной проверки. Семантика и словарь не менялись, новый .axm не требуется; существующий samples/functions/readtext/unicode_chunks.axm остаётся короткой ручной проверкой. Полный набор решения в этой сессии не повторялся. Следующий шаг — запись с SEEKFILE в OUTPUT: сохранение хвоста, буфер FLUSH, байтовые границы многобайтового текста и запрет позиционирования APPEND. Общий аудит активен.


### Session 1170 — позиционирование записи и байтовая ширина LF

Проверены OUTPUT/APPEND в UTF-8/UTF-16LE/UTF-16BE: 12 новых FileOutputPositionEvidenceTests (два движка, с отдельным FLUSH перед SEEKFILE и без него). Перезапись B на Z сохраняет C, REWIND и замена A на Q сохраняют хвост и длину, APPEND отклоняет SEEKFILE/REWIND с MB3007 и сохраняет позицию. После добавления Я, NEWLINE и пустого WRITELINE проверены точные байты Q😀ZCЯ\n\n с правильным BOM. Совместный набор файловых проверок 90/90, ошибок/пропусков 0; .artifacts/test-results/file-output-1170/file-output-1170.trx. Полное решение не повторялось.

Найден и исправлен дефект документации: manual называл LF всегда одним байтом и обещал безусловное увеличение FILELENGTH. Реально LF — один символ: один байт UTF-8/WINDOWS-1251, два UTF-16; перезапись внутри прежнего файла не обязана увеличивать длину. Runtime не изменён. Страница file-system-functions-open-files.html уточнена; добавлены inline UTF-16-пример и samples/functions/newline/utf16_byte_positions.axm. Актуальный CLI (SHA256 Runtime совпал) выполнил пример: позиции 0/2/4, длина 4, чтение/удаление TRUE, код 0. Первый вызов CLI без команды run показал справку; правильный запуск выполнен отдельно, не принят за проверку.

Редакционный ориентир: Зельднер, WRITE #, страницы 195–196 (локальный текст .artifacts/aggregate-fix-1118/zeldner.txt); запасной текущий жанровый ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.writeline?view=net-10.0 . Исторический CRLF и форматирование значений не перенесены: Axiom сохраняет свой LF и свои формы FILE. Навигация уже ведёт на существующие секции, новые имена не введены.

Граница доказательства: записи проверены на корректных границах символов; SEEKFILE принимает байтовую позицию и не гарантирует сохранение текста при перезаписи внутри кодированного символа. Следующий шаг — независимо проверить этот договор, различить допустимую байтовую перезапись и непреднамеренное повреждение текста, затем ошибки закрытия/сброса и изменяемые извне файлы. Общий аудит активен.


### Session 1171 — байтовая перезапись многобайтового текста

Байтовая перезапись Unicode проверена в 16 FileByteOverwriteEvidenceTests: два движка без перехода на другой; UTF-8, UTF-16LE/BE; WRITE("Z") внутри/в начале emoji. Независимые ожидаемые байты заданы литералами. SEEKFILE/WRITE сохраняют точно заданную байтовую замену и хвост; результат не обязан быть корректным текстом. Строгие READFILE/OPEN USING отклоняют полученный невалидный текст с MB3007. Уже переход к началу emoji не спасает от короткой замены: неперезаписанные байты остаются. Это ограничение байтового позиционирования, не доказанный дефект сохранения файла.

Manual file-system-functions-open-files.html теперь прямо объясняет нулевые байтовые позиции без BOM, границы SEEKFILE, сброс буфера, сохранение хвоста и риск замены разной байтовой длины. Существующая альтернатива: READFILE → REPLACE → WRITEFILE в той же кодировке; требуется память и нет обещания атомарной записи. Новый встроенный API не нужен для проверенного малого текстового сценария; специализированная потоковая замена Unicode действительно не предоставлена, но потребность в ней не установлена. Добавлены inline-пример и samples/functions/seekfile/multibyte_overwrite.axm.

Реальный CLI выполнил пример с кодом 0: строгая диагностика MB3007 после байтовой замены, затем полная запись AZB и удаление временного файла TRUE. SHA256 Runtime CLI совпал с текущей библиотекой. Редакционный ориентир — Зельднер, SEEK, страница 210; запасной текущий жанровый ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.seek?view=net-10.0 . Нумерация BASIC с 1 и режим APPEND из исторического описания в Axiom не перенесены.

Новый набор 16/16, совместный файловый набор 106/106 без ошибок и пропусков: .artifacts/test-results/file-bytes-1171/file-bytes-1171-neighbors.trx. Полное решение не повторялось, Runtime не менялся. Следующий шаг — ошибки сброса/закрытия файла: сохранение ресурса, диагностики и предотвращение повторной необработанной ошибки при завершении программы. Источниковая гипотеза ещё не считается дефектом; требуется воспроизведение. Общий аудит активен.


### Session 1172 — освобождение файлов после отказа записи

Подтверждён FILE26-001 P1: при ошибке сброса во время Dispose ресурс оставался открытым; CloseAllOpenFiles прерывался на первом отказе и не освобождал остальные файлы; явный CLOSE в обоих движках пропускал IOException вне TRY/CATCH языка. Воспроизведение — настоящая Windows-блокировка диапазона FileStream.Lock(0,4096) через отдельный читатель, а не подмена результата записи. Прямой набор до исправления 0/2, запуск через оба движка 0/4. Ошибки прежнего запуска мешали даже удалению собственного временного файла из-за оставшегося открытого ресурса.

RuntimeTextFile.Dispose теперь освобождает FileStream в finally и отмечает ресурс закрытым. CloseFile удаляет закрытый ресурс из реестра даже при отказе. CloseAllOpenFiles проходит весь набор: при прямом вызове сохраняет первую ошибку после освобождения остальных ресурсов, при завершении движка сообщает MB3007 для каждого отказа. Interpreter/BytecodeVm переводят ошибку явного CLOSE в MB3007/RuntimeFailureException; TRY/CATCH перехватывает её. Автоматический отказ даёт неуспешный RuntimeResult, не необработанное CLR-исключение.

После исправления FileCloseFailureEvidenceTests 2/2 и FileFinalizationFailureEvidenceTests 4/4. Проверены ISOPEN=FALSE, эксклюзивное повторное открытие после снятия блокировки, запись второго файла, обработка явного CLOSE и автоматическое завершение, без перехода Bytecode на интерпретатор. Проверки блокировки относятся к Windows; на других ОС их ветка не выполняется, переносимость отказа не заявляется. Полный file-close-1172-full выполняется, итог ещё требуется подтвердить.

Manual close.html описывает освобождение после ошибки и отсутствие гарантии сохранения всех данных. Добавлен samples/operators/close/write_failure_state.axm: два собственных временных файла, пауза и проверка закрытого состояния/второго текста. Обычный запуск примера не воспроизводит отказ без внешней блокировки; воспроизведение отказа обеспечено отдельными тестами. Редакционный ориентир — Зельднер CLOSE, страница 187; запасной текущий жанровый https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.fileclose?view=net-10.0 . Новые имена не добавлены, обе схемы подсветки уже содержат CLOSE/ISOPEN.

Следующий шаг — завершить полный прогон и реальный запуск примера, затем проверить повторный FLUSH после временного отказа и сохранение основной ошибки программы при дополнительном отказе автоматического закрытия. Общий аудит активен.


Завершение Session 1172, 2026-10-09 00:01: полный file-close-1172-full завершился с кодом 0; девять TRX подтверждают 5032/5032, ошибок 0, пропусков 0. Новые проверки настоящего Windows-отказа выполнены, а не пропущены платформенной веткой. FILE26-001 исправлен и подтверждён.

Повторный CLI-запуск samples/operators/close/write_failure_state.axm выполнен после полного прогона: SHA256 Runtime CLI совпадает с текущей библиотекой (CC6974281810B5810E209E915C40608B452017DAC150270CFF94653728317BDA), код 0, все пять проверок состояния/текста/удаления TRUE. Более ранний запуск на старой копии Runtime не использован как доказательство исправления. CLI-пример проверяет обычный путь; отказ воспроизведён шестью целевыми тестами. Отдельный отчёт — audit/FILE_CLOSE_FAILURE_20261008.md. git diff --check чистый.

Следующий шаг — повторный FLUSH после снятия временной блокировки, отсутствие дублирования данных и сохранение основной диагностики при дополнительном отказе автоматического закрытия. Общий аудит активен.


### Session 1173 — повторный FLUSH и сохранение основной ошибки

Проверено восстановление FLUSH после настоящей временной Windows-блокировки диапазона: OUTPUT/APPEND, два движка, короткий UTF-8-текст и запись 8196 байтов. Восемь новых случаев подтверждают MB3007 внутри CATCH, ISOPEN=TRUE, сохранение FILEPOS/FILELENGTH, успешное добавление текста и повторный/пустой FLUSH после снятия блокировки, точные байты без дублирования и сохранение APPEND-префикса. Два дополнительных случая подтверждают порядок APP1173/MB3007 при исходной ошибке программы и отказе автоматического закрытия, сохранение первичного текста и освобождение ресурса. Без перехода Bytecode на интерпретатор.

FileFinalizationFailureEvidenceTests: 14/14 с четырьмя прежними CLOSE-случаями; совместный файловый набор 120/120, ошибок/пропусков 0, .artifacts/test-results/file-flush-1173/file-flush-1173-neighbors.trx. Прямые Windows-ветки Lock/Unlock уточнены, предупреждения CA1416 устранены для изменённого писателя. Runtime не менялся. Полный набор решения в этой сессии не повторялся; последний полный прогон Session 1172 — 5032/5032.

Отчёт — audit/FILE_FLUSH_RECOVERY_20261009.md. Доказан отказ блокировки диапазона, не любой отказ носителя или атомарность сохранения. Повторный FLUSH доступен после ошибки самого FLUSH при открытом ресурсе; после отказа CLOSE ресурс уже закрыт. Новое средство языка не требуется для проверенного временного отказа. Следующий шаг — COPYDIR/MOVEDIR: вложенное назначение, совпадающие пути, точки повторного анализа и сохранение исходных данных при отказе. Общий аудит активен.


### Session 1174 — отказы копирования и переноса каталогов

Добавлены 18 независимых DirectoryFailureEvidenceTests в обоих движках без перехода Bytecode на интерпретатор: COPYDIR/MOVEDIR с совпадающим или вложенным назначением; COPYDIR с конфликтом вложенного файла после копирования корневого; защита существующей вложенной ссылки назначения от перезаписи; MOVEDIR при существующем назначении/отсутствующем родителе; перенос явно указанного соединения каталогов Windows. Вместе с восемью DirectoryTraversalContractTests получено 26/26, ошибок/пропусков 0, .artifacts/test-results/directory-1174/directory-1174.trx.

Подтверждены существующие ограничения: COPYDIR не атомарен и не откатывает уже скопированный корневой файл при позднем конфликте, но источник и конфликтный файл назначения остаются прежними. Вложенная ссылка назначения отклоняется, её цель не меняется. MOVEDIR не объединяет папки и не создаёт цепочку отсутствующих родителей; при переносе соединения Windows перемещает саму ссылку, не целевой каталог. Проверенные отказы сохраняют исходные данные; не заявлен откат любого отказа носителя.

Runtime не менялся. Manual file-system-functions-management.html уточнён для MOVEDIR: типы путей, родитель назначения, отсутствие объединения, диагностики, отличия ссылок от COPYDIR. Добавлен samples/functions/movedir/existing_destination.axm и inline-пример. Реальный CLI выполнил новый пример с кодом 0: MB3007, источник/назначение сохранены TRUE, собственные временные файлы и папки удалены. Полный набор решения в этой сессии не повторялся; последний полный — 5032/5032 из Session 1172.

Книга Зельднера проверена: NAME, страница 341, описывает перенос файла и явно не переносит целую директорию, поэтому не использована как источник семантики MOVEDIR. Ближайший текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.movedirectory?view=net-10.0 . Автоматическое создание назначения/overwrite из Visual Basic не перенесены в Axiom. Новых сущностей нет, словарь подсветки не менялся.

Следующий шаг — оборванные ссылки назначения и источник/назначение, меняющиеся между проверкой и обходом. Проверки текущей сессии относятся к существующим соединениям и обычным папкам; не считать их доказательством защиты от всех видов точек повторного анализа или гонок файловой системы. Общий аудит активен.


### Session 1175 — оборванные ссылки при копировании каталогов

Проверены оборванные ссылки: шесть DanglingDirectoryLinkEvidenceTests в двух движках без перехода Bytecode на интерпретатор. Реальное соединение каталогов с удалённой целью в назначении COPYDIR отклоняется MB3007, цель не создаётся; оборванная символическая ссылка на файл назначения также отклоняется, отсутствующий целевой файл не создаётся. Оборванная файловая ссылка источника пропускается DIRSIZE/COPYDIR. Источник и созданные ссылки сохранены до собственного удаления. На этой Windows атрибут ReparsePoint доступен без существующей цели; гипотеза обхода статического запрета не подтвердилась.

Новые случаи 6/6; вместе с двумя прежними классами каталогов — 32/32, ошибок/пропусков 0. .artifacts/test-results/dangling-1175/dangling-1175.trx. Ссылки реально созданы, не подменены обычными папками и не пропущены. Runtime не изменён, полный набор не повторялся; последний полный Session 1172 — 5032/5032. Отчёт — audit/DANGLING_LINK_EVIDENCE_20261009.md.

Из чтения BuiltinDirectoryHelpers следует ограничение проверки: атрибуты и использование строкового пути разделены; дерево не фиксируется целиком удерживаемыми дескрипторами. Не подтверждена защита от подмены другим процессом после проверки. Сам результат такой гонки в этой сессии не воспроизведён, поэтому не помечен подтверждённым дефектом. Не обещать изоляцию по зелёным статическим тестам и не добавлять для неё случайный новый API.

Следующий шаг — проверить корневые явно указанные оборванные ссылки и файловые COPYFILE/MOVEFILE: договор исходных/конечных ссылок, overwrite и сохранение данных при отказе. Исследование гонок остаётся отдельным незавершённым вопросом, требующим воспроизводимого контроля момента подмены. Общий аудит активен.


### Session 1176 — точные байты и ссылки файлового переноса

Проверены COPYFILE/MOVEFILE: 16 FileTransferFailureEvidenceTests в обоих движках без перехода Bytecode на интерпретатор. Двенадцать случаев покрывают конфликт существующего назначения без overwrite, отсутствующего родителя и явный overwrite TRUE; независимые байты включают NUL, 0xFF, 0x80, BOM-последовательность и CRLF, имя источника содержит кириллицу/emoji. При отказах источник и старое назначение сохранены, отсутствующая цепочка родителей не создаётся. При overwrite итоговые байты точны: COPYFILE сохраняет источник, MOVEFILE удаляет прежний путь после успеха.

Ещё четыре случая с настоящей символической ссылкой источника подтверждают различие: COPYFILE копирует содержимое цели в обычный файл, сохраняя ссылку; MOVEFILE переносит саму ссылку, не меняя её цель. Это наблюдение проверенной Windows, не полный договор всех файловых систем и видов ссылок. Исходный целевой файл в обоих сценариях неизменен.

Результат 16/16, ошибок/пропусков 0: .artifacts/test-results/file-transfer-1176/file-transfer-1176.trx. Runtime не менялся, полный набор решения не повторялся; последний полный Session 1172 — 5032/5032. Новый .axm не требуется, поскольку семантика не менялась. Ограничения отражены в audit/FILE_TRANSFER_EVIDENCE_20261009.md и CSV по формам. git diff --check проверяется при завершении.

Следующий шаг — явно заданные оборванные ссылки корня COPYDIR и назначения COPYFILE/MOVEFILE, с overwrite/без него. Перенос существующей ссылки не доказывает поведения оборванной; гонки путей и отказ носителя остаются непроверенными. Уточнение пользовательского manual о файловых ссылках следует выполнить после проверки обеих сторон операции, чтобы не документировать половину договора. Общий аудит активен.


### Session 1177 — ссылки назначения COPYFILE/MOVEFILE

Проверены 16 FileDestinationLinkEvidenceTests: два движка, COPYFILE/MOVEFILE, существующая/отсутствующая цель символической ссылки назначения, overwrite FALSE/TRUE. Первый прогон 14/16: неверным оказалось предположение о безусловном отказе COPYFILE без overwrite при оборванной ссылке. Реальная операция создала отсутствующую цель; это не замена существующих байтов. Ожидание уточнено, сама ссылка и точные байты цели проверены отдельно. Runtime не менялся.

Подтверждён договор проверенной Windows: COPYFILE следует ссылке назначения, сохраняет ссылку и пишет в цель; при существующей цели без overwrite — MB3007, при отсутствующей создаёт цель даже без overwrite. MOVEFILE без overwrite отклоняет ссылку назначения независимо от существования цели; с overwrite заменяет саму ссылку обычным файлом и не меняет прежнюю цель. Источник после отказа сохранён; после успешного переноса прежний путь отсутствует. Вместе с FileTransferFailureEvidenceTests — 32/32, ошибок/пропусков 0, .artifacts/test-results/destination-link-1177/destination-link-1177.trx.

Manual file-system-functions-management.html дополнен правилами источника/назначения, типами параметров, отсутствующим родителем, диагностикой и двумя раздельными формами COPYFILE/MOVEFILE. Книга Зельднера NAME ранее проверена как ориентир перемещения файлов; подходящего FILECOPY-раздела в извлечённом тексте не найдено. Текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.copyfile?view=net-10.0 и ранее проверенная MoveFile. Факты ссылки подтверждены Axiom-тестами, не перенесены из Visual Basic. Новый пользовательский API не введён; отсутствие языка создания ссылок не требуется устранять ради этого случая. Автоматизированная подготовка использует реальные ссылки файловой системы.

Полный набор решения в этой сессии не повторялся, последний полный — 5032/5032 Session 1172. Новая .axm-программа не требуется, поскольку поведение языка не менялось и без внешней подготовки ссылки этот случай не воспроизводится. Следующий шаг — корневые явно заданные оборванные ссылки COPYDIR, затем свести подтверждённые файловые формы и оставить непроверенные гонки/отказы носителя отдельными вопросами. Общий аудит активен.


### Session 1178 — корневые ссылки COPYDIR и сводка файловых доказательств

Проверены четыре RootDirectoryLinkEvidenceTests: оба движка, настоящее соединение каталогов Windows с удалённой целью, явно указанное как источник/назначение COPYDIR. Источник даёт MB3007, цель источника не создаётся, но пустая папка назначения уже создана. Явное корневое назначение разрешается в отсутствующую цель: она создаётся, получает точный текст, соединение сохраняется. Источник неизменен. Это отличается от запрета вложенной ссылки назначения и не отменяет отсутствие отката COPYDIR.

Совместный набор шести классов переноса/каталогов — 68/68, ошибок/пропусков 0, .artifacts/test-results/root-link-1178/root-link-1178.trx. Runtime не менялся, полный набор не повторялся; последний полный результат остаётся 5032/5032 Session 1172. Manual file-system-functions-management.html прямо описывает оба корневых случая и возможное создание пустого назначения перед ошибкой. Редакционный текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.copydirectory?view=net-10.0 ; факты определены по Axiom, не по поведению Visual Basic. Подходящей исторической главы копирования дерева у Зельднера не установлено.

Создан audit/FILESYSTEM_AUDIT_STATUS_20261009.md: подтверждённые участки, ограниченность прогонов, FILE26-001 как исправленная поломка, имеющиеся альтернативы, отсутствующий общий позиционный двоичный режим как отложенный кандидат и отсутствие основания добавлять язык создания ссылок ради тестовой подготовки. Не заявлены полная защита гонок, всех файловых систем или отказов носителя. Новых сущностей/семантики нет; внешний сценарий ссылки подготовлен тестами, новая .axm-программа без возможности создать ссылку не дала бы независимого воспроизведения.

Следующий шаг — совпадающие пути COPYFILE/MOVEFILE, жёсткие ссылки как разные имена одного файла, overwrite и сохранение исходных байтов. Затем вернуться к сквозной матрице всего языка с сохранёнными пределами файловых доказательств. Общий аудит активен.


### Session 1179 — совпадающий файл и жёсткие ссылки

Добавлены 16 SameFileTransferEvidenceTests: восемь совпадающих путей и восемь настоящих жёстких ссылок Windows, два движка, COPYFILE/MOVEFILE, overwrite FALSE/TRUE. Жёсткая связь доказана записью через второе имя и чтением первого перед вызовом языка. Совпадающий путь: COPYFILE даёт MB3007 и сохраняет байты, MOVEFILE возвращает TRUE без изменения файла. Разные жёсткие имена: COPYFILE даёт MB3007 и сохраняет оба имени/байты; MOVEFILE возвращает TRUE при обоих overwrite, убирает имя источника и сохраняет байты назначения.

Первые ожидания MOVEFILE для разных жёстких имён были неверны: без overwrite ожидался конфликт, затем предполагалось сохранение обоих имён. Независимые проверки результата и существования путей показали успешный перенос имени. Ожидания уточнены; это измеренный договор проверенной Windows, не исправление Runtime. Итог нового класса 16/16; совместно с источниками/назначениями ссылок — 48/48, ошибок/пропусков 0, .artifacts/test-results/same-file-1179/same-file-1179.trx.

Manual file-system-functions-management.html уточнён: успех MOVEFILE не означает исчезновения пути при совпадающем пути; жёсткие имена одного файла отличаются от конфликта двух файлов. Добавлены inline-пример и samples/functions/movefile/same_file_path.axm. Актуальный CLI выполнил пример с кодом 0: MOVEFILE TRUE, сохранность текста TRUE, COPYFILE MB3007, сохранность после отказа TRUE, удаление TRUE. Runtime не менялся, полный набор не повторялся; последний полный — 5032/5032 Session 1172.

Редакционный ориентир — ранее прочитанный Зельднер NAME, страница 341; текущий жанровый https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.movefile?view=net-10.0 . Поведение жёстких ссылок определено по Axiom/Windows-тестам, не перенесено из исторического BASIC. Новый API определения тождественности файлов не вводится: текущие операции уже сохраняют байты в проверенных случаях, отдельная обязательная потребность не установлена.

Следующий шаг — вернуться к сквозной матрице всего языка, связать новые файловые доказательства с реальными формами и определить оставшиеся участки независимой проверки вне файловой системы. Пределы: другие ОС, гонки, отказ носителя и весь набор неправильных типов не доказаны. Общий аудит активен.


## Session 1180 - 2026-10-09 00:24 - актуальная сквозная матрица доказательств

Исправлен инструмент export-current-inventory.py: прежний фильтр только EVIDENCE_20261008.csv исключал новые файловые доказательства 9 октября. Добавлен обязательный --snapshot-date с проверкой календарной даты; собираются доказательства всех дат до снимка включительно. README дополнен новой командой. Исторические снимки не изменены. Обновлены inventory-only и metadata актуального CLI, экспортированы LANGUAGE_FUNCTION_MATRIX_20261009.csv и LANGUAGE_REGISTRY_RECONCILIATION_20261009.json.

Сверка всех сигнатур/политик успешна: 314 форм, 250 имён, 299 зарегистрированных ID из 302, резерв DateAdd/DateDiff/DatePart. Нет повторов сигнатур, отсутствующих диспетчеров VM или слов обеих схем подсветки. Кандидаты доказательств связаны с 209 формами, без кандидатов остаются 105 (ранее 190/124). Это указатели по ID/Arity, а не доказательство полноты семантики; UNVERIFIED_PER_FORM сохранён. Runtime не менялся, полный набор не повторялся.

Следующий шаг — разобрать 105 форм без кандидатов, выбрать независимые проверки строковых и числовых границ вне файловой системы. Отсутствие указателя не означает отсутствующую функцию или поломку. Общий аудит активен.


## Session 1181 - 2026-10-09 00:26 - проверка идентичности формы в доказательствах

При чтении 105 строк без кандидатов обнаружены ошибочные ID в новых таблицах файловых доказательств: COPYFILE/MOVEFILE/COPYDIR с Arity 3 были записаны как CopyFile/MoveFile/CopyDir вместо CopyFileOverwrite/MoveFileOverwrite/CopyDirOverwrite. Исправлены FILE_TRANSFER_FORM_EVIDENCE_20261009.csv и DIRECTORY_FORM_EVIDENCE_20261009.csv. Это исправление учёта доказательств, не реализации языка.

Экспорт теперь проверяет существование пары ID/Arity и соответствие Name, если имя указано. Две независимые отрицательные проверки в собственном временном каталоге подтвердили отказ до создания матрицы: неверная пара CopyFile/3 и чужое имя MOVEFILE для CopyFileOverwrite/3. Действующий экспорт успешно сверил все CSV доказательств и метаданные; пересчитанный снимок содержит 212 форм с кандидатами и 102 без них. Прежние числа 209/105 относятся к снимку до исправления ошибочных ID. Семантическая полнота не заявлена, UNVERIFIED_PER_FORM сохранён. git diff --check прошёл.

Следующий шаг — независимые проверки строковых/числовых форм из оставшихся 102 строк, включая явное отделение имеющихся тестов от отсутствующего указателя в матрице. Runtime не менялся, MSBuild-тесты не повторялись ради изменения Python-экспорта. Общий аудит активен.


## Session 1182 - 2026-10-09 00:27 - независимые числовые преобразования

Добавлен IntegralConversionEvidenceTests: 28 случаев, два движка, заранее заданные результаты усечения на ±2.5/±3.5, точных DECIMAL-границ INTEGER/LONG с половиной и выходом на единицу, точного большого LONG и потери единицы при промежуточном CDBL. Каждый случай проверяет TRY/FINALLY, продолжение, отсутствие запасного интерпретатора и внешних диагностик. Первый прогон 83/87: четыре SyntaxFailure из-за формы CDEC(-9223372036854775808). Подготовка значения исправлена на -CDEC(9223372036854775807)-CDEC(1); это отделяет проверку преобразования от отдельного договора литералов, не исправление Runtime.

Повторный набор с NumericConversionContractTests, NumericBoundaryContractTests, IsNumericContractTests и NumericTextAlternativeEvidenceTests — 87/87, ошибок/пропусков 0, .artifacts/test-results/conversion-1182/conversion-1182.trx. Проверены реальные существующие тесты ISNUMERIC/VAL и добавлены точные указатели NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv. ISNUMERIC уже обеспечивает полную проверку конечного числа и отличается от VAL по префиксам/переполнению: новой функции проверки строки не требуется. Допустимый underflow в ноль не является обещанием точности.

Новой поломки преобразований не установлено. Runtime и семантика языка не менялись; справочник уже описывает усечение к нулю, диапазон после усечения и возможное округление CDBL. Новая программа .axm не требуется для неизменной семантики. Полный набор не повторялся. Следующий шаг — числовые литералы нижней границы LONG как самостоятельный договор, затем формы STR/INT/CDEC и Unicode-формы. Общий аудит активен.


## Session 1183 - 2026-10-09 00:29 - нижняя граница LONG и литералы

Добавлен LongMinimumLiteralEvidenceTests: два случая движков проверяют три точных способа получить минимум LONG и верхнюю границу; две записи прямого отрицательного литерала проверяются в обоих движках на SyntaxFailure/MB0004, начало/длину диагностики и отсутствие вывода. Вместе с IntegralConversionEvidenceTests и NumericBoundaryContractTests — 38/38, ошибок/пропусков 0. Отдельно запущен весь Axiom.Lexing.Tests, результат сохранён в lexer-1183.trx.

Договор подтверждён исходником Lexer и существующим LexerTests.Lex_ReportsOutOfRangeIntegerLiteral: минус отдельный, положительный токен должен помещаться в LONG. Manual data-types-scalars.html уже отличает диапазон значения от допустимой формы литерала. Доступ к минимуму через -9223372036854775807 - 1 и шестнадцатеричную аналогичную арифметику точный. Создан LONG_MINIMUM_LITERAL_EVIDENCE_20261009.md с классификацией: не поломка, существующая альтернатива; специальная краткая форма отсутствует, но изменение не требуется для доступа к значению. Runtime/синтаксис не менялись, новая .axm-программа для изменения поведения не требуется.

Следующий шаг — независимые STR/INT/CDEC и добавленные ранее Unicode-формы LEN/LEFT/RIGHT/MID. Общий аудит активен.


## Session 1184 - 2026-10-09 00:31 - INT STR CDEC и региональные настройки

Добавлен NumericPresentationEvidenceTests: 13 заранее заданных выражений × три культуры en-US/ru-RU/tr-TR × оба движка, 78 случаев. INT проверен на половинах, нижней/верхней границе INTEGER после округления вниз и LONG за диапазоном. STR проверен на точке/знаке, точном LONG 2^53+1, DECIMAL LONG.MaxValue и 1/8. CDEC проверен на точном LONG, арифметически собранном DECIMAL.MaxValue, переполнении DOUBLE 1e29 и округлении 1e-29 в 0. Культура восстанавливается в finally; проверены отсутствие внешней диагностики/запасного интерпретатора и продолжение после перехваченного MB3008.

Все 78 новых прошли; вместе с IntegralConversionEvidenceTests и NumericPromotionContractTests — 206/206, ошибок/пропусков 0, .artifacts/test-results/numeric-presentation-1184/numeric-presentation-1184.trx. Добавлены точные ID/Arity в NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv, общая матрица пересчитана. Поломки текущих договоров не найдены. Runtime не менялся, полный набор не повторялся.

Классификация: округление вниз уже доступно через INT, сохранение большого целого при выводе — STR(LONG), точное получение проверенной границы DECIMAL — арифметика CDEC(LONG). Отсутствие универсального точного преобразования строки в DECIMAL не устранено этим набором и не объявлено обязательной новой функцией; VAL возвращает DOUBLE и потому не заменяет такую возможность во всех случаях. Малый DOUBLE при CDEC может стать нулём, это не проверка точности. Новая семантика и пользовательские имена не введены; существующая справочная страница описывает проверенные основные формы.

Следующий шаг — независимая проверка Unicode-форм LEN/LEFT/RIGHT/MID, затем оставшиеся формы без доказательств. Общий аудит активен.


## Session 1185 - 2026-10-09 00:33 - независимые границы Unicode-единиц

Добавлен UnicodeBoundaryEvidenceTests: восемь строк × оба движка, 16 случаев. Все позиции MID и соответствующие префиксы LEFT/суффиксы RIGHT сравниваются с заранее заданными частями, отдельно LEN и выход за конец. GRAPHEME: e с combining mark, семья ZWJ, флаги RI с нечётным остатком, тон кожи, keycap, начальный combining mark, Hangul, CRLF. CODEPOINT: combining отдельно, целая emoji-пара, высокие/низкие одиночные суррогаты и соседняя корректная пара. UTF16: обе половины emoji. Не используется StringInfo для вычисления ожидаемого результата.

Первоначальная подготовка ошибочно использовала CHRW для суррогатных кодов (получен RuntimeFailure); заменена на строки с C#-escape. Передача повреждённых строк в MemberData затем искажала данные: наблюдались три единицы вместо двух и несовпадение с половинами emoji. Теперь через MemberData передаются только режим/индекс, исходные повреждённые строки строятся внутри теста. При промежуточной правке исправлена ошибка синтаксиса объявления массива C#. Итог 16 новых вместе с UnicodeStringUnitTests и UnicodeUnitAlternativeEvidenceTests — 65/65, ошибок/пропусков 0, .artifacts/test-results/unicode-boundaries-1185/unicode-boundaries-1185.trx.

Создан UNICODE_UNIT_FORM_EVIDENCE_20261009.csv с точными LenUnits/LeftUnits/RightUnits/MidUnits и Arity. Семантика Runtime не менялась, существующие четыре unicode_units.axm исполняются с ожидаемым выводом в проверенном классе UnicodeStringUnitTests. Новых имён/семантики нет; manual ранее описывает эти формы. Не обещано покрытие всего Unicode и стабильность границ между версиями StringInfo/.NET. Полный набор решения не повторялся.

Классификация: целые составные символы и Unicode-коды уже доступны через явные формы, отдельные новые функции не нужны. Режим UTF16 намеренно может разделить пару; CODEPOINT сохраняет корректную пару и отдельно переносит одиночный суррогат, это не валидация корректности всей строки. Следующий шаг — пересчитать остаток матрицы и выбрать неподтверждённые формы вне файловых повторов. Общий аудит активен.


## Session 1186 - 2026-10-09 00:35 - независимый числовой договор RGB

Добавлен RgbIndependentEvidenceTests: 30 отдельных случаев обоих движков для чёрного/белого, трёх основных цветов, 1/2/3 и каждого компонента вне диапазона (-1, 256, LONG.MaxValue). Ожидаемые упакованные десятичные значения заданы буквально, без вызова общего помощника цвета. Ещё четыре теста неверных форм выполняют оба движка: два/четыре аргумента, дробный и строковый аргумент; подтверждена диагностика MB2 до исполнения. Функция проверяется без SCREEN/CANVAS, после перехваченного MB3007 программа продолжает работу.

Итог 34/34, ошибок/пропусков 0, .artifacts/test-results/rgb-1186/rgb-1186.trx. Создан GRAPHICS_VALUE_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Runtime не менялся, полного прогона не было. Это доказательство числового цвета 0xRRGGBB, не проверки пикселя, альфа-канала, палитры или отображения.

Классификация: числовое получение цвета уже доступно; отдельные новые функции упаковки не нужны для проверенных случаев. Экранные функции и интерактивный ввод не объявляются проверенными по этому результату. Следующий шаг — выделить оставшиеся формы по необходимому контексту и независимо проверить доступные свойства изображений/тайлов через существующие тестовые хосты. Общий аудит активен.


## Session 1187 - 2026-10-09 00:36 - реальные ресурсы IDE и свойства

Прочитан TilePropertyAcceptanceSampleTests: 27 запусков существующих .axm × оба движка и два явных SCREEN 0, заранее заданные строки, отсутствие запасного интерпретатора, настоящий ProgramConsoleService/ProgramRunService и загрузчик ресурсов. Повторный запуск — 56/56, ошибок/пропусков 0, .artifacts/test-results/resources-1187/resources-1187.trx. Независимо прочитаны PNG IHDR (32x32) и JSON simple_map.tmj: 2x2, клетки 16x16, два tilelayer и один objectgroup, данные [1,2,0,4]/[2,1,4,0], тайлсет четыре тайла. Размеры/число кадров согласуются с ожидаемыми результатами существующих примеров.

Создан GRAPHICS_RESOURCE_FORM_EVIDENCE_20261009.csv для фактически запущенных форм: изображения, спрайты, LOADTILESET с тремя аргументами, свойства тайлов/карт, клетки/маски, начальное состояние SCREEN и явный SCREEN 0. LOADTILESET с четырьмя/пятью аргументами не приписано доказательство от формы с тремя. Это воспроизведение заданного результата ресурса, не проверка отрисовки или всех форматов, повреждений и границ. Новых тестов, Runtime-изменений и новых семантических обещаний нет.

Классификация: основные свойства ресурсов уже доступны и проверяются через реальный загрузчик IDE; отсутствие указателя в прежней матрице было пробелом учёта, не отсутствием функций. Следующий шаг — отдельно проверить формы LOADTILESET margin/spacing и контекстные TEXTWIDTH/TEXTHEIGHT/POINT/PMAP. Общий аудит активен.


## Session 1188 - 2026-10-09 00:38 - LOADTILESET отступы и промежутки

Прочитаны и повторно выполнены парные Runtime-тесты Interpreter/BytecodeVm: TilesetWidthHeightAndCountReturnLoadedAndEmptySizes, DrawTileCallsModernGraphicsHostAndUpdatesCurrentPosition, TilesetOperationsReportDocumentedDiagnostics. Итог 6/6, ошибок/пропусков 0, .artifacts/test-results/tileset-1188/tileset-1188.trx. Ожидания независимы: атлас 34x18/тайл16x8/margin1 даёт4; атлас36x20/тайл16x8/margin1/spacing2 даёт4. Для36x24/тайл16x10 четвёртая область начинается19,13 и имеет16x10, фактический вызов рисующего хоста сравнивается с заданной записью. Отрицательный margin даётMB3007, несовместимая сетка35x24 даётMB3005.

GRAPHICS_RESOURCE_FORM_EVIDENCE_20261009.csv дополнен отдельными LoadTilesetMargin/Arity4 и LoadTilesetMarginSpacing/Arity5. Хост получает заранее созданный RuntimeImageValue, поэтому доказательство не распространяется на декодирование PNG, видимое изображение или весь набор отрицательных параметров/предельных LONG. Наличие теста поля вызова хоста не подменяет проверку окна. Реализация не менялась, полный набор не повторялся.

Классификация: поддержка атласов с рамкой и промежутками уже есть во всех трёх формах LOADTILESET, новой функции не требуется. Следующий шаг — TEXTWIDTH/TEXTHEIGHT/POINT/PMAP и отдельно интерактивные функции ввода. Общий аудит активен.


## Session 1189 - 2026-10-09 00:40 - контекст графических функций

Прочитаны парные Interpreter/BytecodeVm тесты TEXTWIDTH/TEXTHEIGHT, обеих POINT и PMAP. Основной набор шести методов × два движка — 12/12, отдельно CanvasOperationsReportDocumentedDiagnostics — 2/2; ошибок/пропусков 0, .artifacts/test-results/graphics-context-1189. TEXTWIDTH/HEIGHT имеют заданные24/12 и16/24 для одной/двух строк в тестовом хосте; без CANVAS даютMB3010. POINT(number) требует SCREEN даже при CANVAS; селекторы физических/логических координат проверены после WINDOW/VIEW. POINT(x,y) проверен на физическом чтении CANVAS без повторной проекции CAMERA/VIEWPORT и на SCREEN с WINDOW. PMAP имеет заранее заданные прямые/обратные результаты для0..3, дробные1.5/102.5, selector4 даётMB3007.

Создан GRAPHICS_CONTEXT_FORM_EVIDENCE_20261009.csv с отдельной записью каждой формы; матрица пересчитана. Размеры текста относятся к тестовому хосту, не к реальному шрифту IDE; пиксельные значения хоста не доказывают окно или весь диапазон полноцветного вывода. Runtime не менялся, полный набор не повторялся.

Классификация: измерение текста и преобразование координат уже доступны через текущие формы, новая функция для этих задач не нужна. Контекстные ограничения не являются доказанной поломкой. Следующий шаг — функции клавиатуры/мыши и оставшийся файловый учёт с конкретными независимыми ожидаемыми результатами. Общий аудит активен.


## Session 1190 - 2026-10-09 00:42 - клавиатура мышь и очередь

Прочитаны и выполнены конкретные Runtime-тесты удержания/очереди/псевдонимов/сочетаний клавиш, мыши и CAMERA/VIEWPORT: 31/31. Выявлена слабость теста KeyDownReadsHeldKeyStateWithoutConsumingQueuedKeys: очередь была пуста, название не доказывало непотребление. В BytecodeVmTests.cs и InterpreterTests.cs добавлена очередь A, ожидается её сохранение для INKEY. Первый повтор 1/2: в Interpreter забыта замена вспомогательного создания хоста, очередь всё ещё пуста. Подготовка исправлена на TestRuntimeHost с keys A и pressedKeys LEFT/CTRL; итог 2/2. Runtime не менялся.

Девять выбранных ProgramConsoleServiceTests — 9/9: прямой ввод очереди, удержание/отпускание, очистка состояния без потребления очереди, мышь и объединение движений без потери кнопок. Результаты .artifacts/test-results/input-1190. Создан INPUT_FORM_EVIDENCE_20261009.csv для семи реальных форм. Сохранение несовпавших событий и однократное потребление проверены; тест MouseFunctionsReturnDefaultValuesWithoutMouseProviderState фактически ожидает MB3010, поэтому название не используется как доказательство нулевых значений.

Классификация: удержание, нажатие и очередь уже имеют разные API; не требуется объединять их новой функцией. Предел — поставляемое состояние тестового хоста и прямые методы IDE, не реальные события ОС или фокус окна. Полный набор не повторялся, семантика не менялась. Следующий шаг — оставшиеся файловые формы и сведение кандидатов отсутствующих возможностей отдельно от пробелов доказательств. Общий аудит активен.


## Session 1191 - 2026-10-09 00:44 - пакет строк и свойства FILE

Добавлен FileLineBatchEvidenceTests: оба движка × UTF-8/UTF-16LE/UTF-16BE с BOM, шесть новых. Файл emoji CRLF/пустая CRLF/Я LF/Б без завершающего разделителя подготовлен напрямую; ожидаются две первые строки включая пустую, LINENUMBER3, SKIPLINE TRUE/LINENUMBER4, остаток одна Б, EOFTRUE, пустой массив READLINES на EOF и MB3007 SKIPLINE на EOF. READLINES0 не сдвигает FILEPOS0. FILEPATH совпадает с абсолютным подготовленным путём, FILEMODE равен INPUT. После завершения точные исходные байты неизменны, временный файл удалён в finally.

Все шесть прошли с первого запуска; вместе с FilePositionBufferEvidenceTests — 42/42, ошибок/пропусков 0, .artifacts/test-results/line-batch-1191/line-batch-1191.trx. Создан FILE_BATCH_FORM_EVIDENCE_20261009.csv для четырёх конкретных форм. Runtime не менялся; полный набор не повторялся. Не доказаны OUTPUT/APPEND/закрытые FILE и все ошибки типов этими строками матрицы.

Классификация: пакетное чтение, пропуск строки и свойства дескриптора уже доступны; новые функции не нужны для этого сценария. EOF и пустая строка не взаимозаменяемы, подтверждён существующий договор. Следующий шаг — оставшиеся свойства путей/файлов и независимая сводка кандидатов отсутствующих возможностей. Общий аудит активен.


## Session 1192 - 2026-10-09 00:46 - компоненты путей Windows

Добавлен PathComponentEvidenceTests: 12 случаев двух движков для шести заданных путей (Unicode с двумя расширениями, корень, каталог с разделителем, .., UNC, .hidden), плюс два случая классификации шести форм абсолютности. Ожидания заданы буквально; существование ресурсов не требуется. Первый прогон31/33: неверно ожидался завершающий разделитель у UNC PATHROOT. Сверка реализации Path.GetPathRoot и результата показала \\server\share без завершающего разделителя, тогда как диск возвращается C:\. Ожидание уточнено, Runtime не менялся.

Повторный прогон с PathBaseResolutionEvidenceTests — 33/33, ошибок/пропусков0, .artifacts/test-results/path-components-1192/path-components-1192.trx. Добавлен PATH_COMPONENT_FORM_EVIDENCE_20261009.csv для семи форм, включая ранее проверенные FULLPATH/COMBINEPATH. C:file и \file не полностью абсолютны; EXTNAME(.hidden) равен .hidden; корень DIRNAME пуст. Сетевой доступ к UNC не проверяется по строковому разбору. Не доказаны все недопустимые пути и переменные среды.

Классификация: разбор/нормализация путей доступны существующими API; вычисление относительного маршрута между двумя полными путями этим не заменено, остаётся отдельным ранее отложенным кандидатом. Полного прогона не было, новая семантика и имена не введены. Следующий шаг — свойства/перечисление/переименование файлов, затем единая классификация оставшихся возможностей. Общий аудит активен.


## Session 1193 - 2026-10-09 00:47 - перечисление файлов и каталогов

Добавлен DirectoryListingEvidenceTests: два движка × GETFILES/GETDIRS × одна/две аргументные формы, восемь случаев. Собственный временный корень содержит три файла и три папки A/B/кириллица emoji, внутри A отдельный файл. Проверены заданный отсортированный список абсолютных путей, фильтры *.txt/A*, отсутствие вложенного файла в верхнем уровне, пустой массив при *.missing, MB3007 для пустого шаблона и отсутствующего каталога. Исходные тексты проверены отдельно после исполнения, собственный корень удалён в finally.

Все восемь прошли с первого запуска; с PathComponentEvidenceTests — 22/22, ошибок/пропусков0, .artifacts/test-results/listing-1193/listing-1193.trx. Создан DIRECTORY_LISTING_FORM_EVIDENCE_20261009.csv с четырьмя точными формами ID/Arity, матрица пересчитана. Runtime не менялся, полного прогона не было.

Классификация: отбор и перечисление верхнего уровня уже доступны. Этот набор не подтверждает рекурсивный API, все шаблоны Windows или возможность обхода прав/ссылок; потребность расширять язык ради текущего сценария не установлена. Следующий шаг — свойства и переименование/удаление, затем единая сводка отсутствующих возможностей. Общий аудит активен.


## Session 1194 - 2026-10-09 00:49 - переименование удаление и существование

Добавлен FileRenameDeleteEvidenceTests: 12 случаев двух движков для файла/каталога и успеха/конфликта/имени с путём, два случая удаления. Подготовлены настоящие Unicode-имена и двоичные байты0/255/128; при конфликте целевой байт42. После отказа обе стороны отдельно прочитаны, при успехе прежний путь отсутствует и новые байты точны; подкаталог из недопустимого имени не создан. DELETEDIR непустого каталога даётMB3007, сразу отдельно подтверждена сохранность файла. После явного удаления DELETEFILE/DELETEDIR возвращаютTRUE, повторFALSE. MAKEDIR создаёт вложенный путь, DELETEDIRRECURSIVE удаляет дерево с пустой вложенной папкой и повторно даётFALSE. FILEEXISTS/DIREXISTS/PATHEXISTS различают файл и папку.

14/14 с первого запуска, ошибок/пропусков0, .artifacts/test-results/rename-delete-1194/rename-delete-1194.trx. Создан FILE_MUTATION_FORM_EVIDENCE_20261009.csv для девяти конкретных форм. Runtime не менялся, полный набор не повторялся. Пределы: не все недопустимые имена, ссылки/права/гонки/сбой носителя; вложенные файлы DELETEDIRRECURSIVE этим сценарием не проверены.

Классификация: переименование в том же каталоге и явные варианты удаления уже доступны; отказ удалить непустую папку DELETEDIR соответствует отдельной рекурсивной форме, новой функции не требуется. Следующий шаг — метаданные файлов, текстовые целофайловые формы и сводка оставшихся возможностей. Общий аудит активен.


## Session 1195 - 2026-10-09 00:51 - целый текстовый файл и кодировки

Прочитан ExplicitTextEncodingTests: реальные файлы, обе формы движков, UTF-8/UTF-16LE/UTF-16BE/Windows1251, заданные строки/байты, BOM и отказ на повреждённых входах/несовместимом BOM с сохранением байтов. Добавлен WholeFileDefaultEvidenceTests, два случая обоих движков: А/emoji/CRLF/пустаяLF/Б, READFILE сохраняет текст, READALLLINES даёт три элемента включая пустой, после пустой WRITEFILE READFILE пуст/READALLLINES0. Повторная запись/добавление сверены с буквально заданными11 UTF8-байтами, без BOM.

Первый прогон34/34; после усиления буквальных байтов повтор34/34, ошибок/пропусков0, .artifacts/test-results/whole-file-1195/whole-file-1195.trx. Создан WHOLE_TEXT_FORM_EVIDENCE_20261009.csv с восемью отдельными формами, матрица пересчитана. Runtime не менялся, полного прогона не было. Семантика без явной кодировки не объявлена строгой валидацией повреждённого текста; это отдельный существующий договор fallback.

Классификация: чтение/запись/добавление Unicode-текста и массив строк доступны; общего двоичного позиционного режима это не создаёт и строгие формы не заменяют гарантию атомарности. Новые функции не введены. Следующий шаг — временные пути/метаданные/диски и сведение действительно отсутствующих возможностей. Общий аудит активен.


## Session 1196 - 2026-10-09 00:52 - независимые метаданные файлов

Добавлен FileMetadataEvidenceTests: оба движка, настоящий файл с Unicode-именем и пятью заданными байтами. Прямо установлены разные местные Creation/Write/Access времена и Write время каталога, затем подготовка подтверждена чтением свойств до запуска языка. Ожидаемые строки дат заданы буквально, не вычислены производственным форматтером. Hidden/ReadOnly включены и сняты, ожидаются TRUE/TRUE и FALSE/FALSE. Каждая из семи функций на отсутствующем объекте даёт перехваченный MB3007. Исходные байты сохранены, атрибуты сбрасываются перед удалением своего временного корня.

Два новых комплексных случая прошли с первого запуска; вместе с FileRenameDeleteEvidenceTests —16/16, ошибок/пропусков0, .artifacts/test-results/metadata-1196/metadata-1196.trx. Создан FILE_METADATA_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Пределы — проверенная Windows и текущий том; не все часовые пояса/файловые системы, автоматическое обновление доступа, права и ссылки. Runtime не менялся, полного прогона не было.

Классификация: размеры в байтах, местные даты и признаки доступны существующим API. UTC-формы, изменение атрибутов из самого языка и иные расширения этим не добавлены и не объявлены обязательными без отдельной потребности. Следующий шаг — диски/временные файлы/текущий каталог и оставшийся CDBL, затем сведение отсутствующих возможностей. Общий аудит активен.


## Session 1197 - 2026-10-09 00:54 - текущий каталог и временные объекты

Добавлен DirectoryAndTempEvidenceTests: четыре случая обоих движков. CURRENTDIR равен базе запроса, CHANGEDIR меняет её на Unicode-каталог, относительный WRITEFILE создаёт файл только в нём. Отказ CHANGEDIR missing даёт MB3007 и сохраняет базу; Environment.CurrentDirectory до/после неизменен. MAKETEMPFILE/MAKETEMPDIR создают четыре разных пустых объекта в TEMP с уникальным префиксом; префикс с путём отклоняется. Перед удалением каждого созданного объекта проверяются абсолютный родитель TEMP и собственный уникальный префикс; каталоги удаляются без рекурсивного обхода.

4 новых с первого запуска; с PathBaseResolutionEvidenceTests —23/23, ошибок/пропусков0, .artifacts/test-results/temp-base-1197/temp-base-1197.trx. Создан TEMP_BASE_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Не доказаны конкурентное создание, защита подмены, все ошибки префиксов/прав и сбои носителя. Runtime не менялся, полного прогона не было.

Классификация: смена базы программы и обычное создание временных объектов уже доступны; отдельные новые функции не нужны для проверенного сценария. Создание имени проверкой существования перед созданием не заявлено как гарантия против гонок. Следующий шаг — сведения о дисках и CDBL, затем независимая сводка отсутствующих возможностей. Общий аудит активен.


## Session 1198 - 2026-10-09 07:18 - независимые сведения о диске и CDBL

Добавлен DriveInfoEvidenceTests: оба движка, корень TEMP-диска, несуществующий путь. Windows CIM Win32_LogicalDisk запрошен отдельным скрытым PowerShell-процессом, затем имя/формат/категория/полный размер Axiom сверены с этой независимой инвентаризацией. Для FREESPACE/TOTALFREESPACE доказаны только границы0..Size, не точное число или равенство доступного/общего свободного места. Не создаётся проверяемый отсутствующий путь. Проверенная машина: C:, NTFS, Fixed,511523409920 байт по CIM.

Повторно выполнен NumericPromotionContractTests.ExplicitDecimalToDoubleConversionCanRoundAndPreservesOrdinaryValues в обоих движках, прочитаны его заданные результаты. Два новых + два повторных —4/4, ошибок/пропусков0, .artifacts/test-results/drive-conversion-1198/drive-conversion-1198.trx. Создан DRIVE_FORM_EVIDENCE_20261009.csv, NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv дополнен CDbl/Arity1. Runtime не менялся.

Матрица имеет кандидаты доказательств для всех314 зарегистрированных форм, но SemanticStatus остаётся UNVERIFIED_PER_FORM: указатель не доказывает полные типы, все края, диагностики, ограничения хоста и независимость каждого исходного теста. Поэтому аудит не завершён достижением числа314. Пределы дискового набора — текущий локальный том, не сеть/квоты/неготовые или съёмные носители.

Следующий шаг — свежий полный прогон решения после накопленных тестовых изменений, затем проверка качества доказательств и единая классификация отсутствующих возможностей по исходной задаче. Общий аудит активен.


## Session 1199 - 2026-10-09 07:23 - полный прогон и классификация пробелов API

Запущен один dotnet test Axiom.sln --logger trx --results-directory .artifacts/test-results/audit-full-1199, вывод .artifacts/audit-full-1199.log. На момент записи восемь проектов прошли, Axiom.Execution.Tests ещё выполняется; живой terminal session48172. Итог решения пока не объявлен. Отдельные параллельные MSBuild-команды не запускаются.

Заново прочитана исходная задача из приложения пользователя: наличие проверок всех функций не заменяет аудит операторов, типов, процедур, диагностик и planned features. Создан API_GAP_STATUS_20261009.md: G01/G02/G04 представлены текущими реализованными формами с конкретными выполненными доказательствами; G03 и относительный путь остаются отсутствующими специализированными возможностями с отложенным проектированием; точный строковый DECIMAL требует отдельного воспроизведения/проверки альтернатив. HYPOT и ряд сценариев имеют проверенные пользовательские решения, создание ссылок не нужно для тестовой подготовки, ввод уже разделён по удержанию/событиям/очереди. Это не финальный вердикт зрелости библиотеки.

Обновлены inventory-only и metadata прямым запуском уже собранных DLL (без MSBuild/obj), затем снимки матрицы/сверки:314 формы/250 имён, все314 имеют кандидаты, нет дублей/пропущенного VM/слов подсветки; SemanticStatus UNVERIFIED_PER_FORM сохранён. GeneratedAt2026-10-09T07:22:09+03. Новый статус не подменяет полноту независимых результатов.

Следующий шаг — дождаться того же полного процесса, записать точные TRX-счётчики, затем проверить точный ввод DECIMAL и planned возможности по коду. Общий аудит активен.


### Завершение Session 1199 — 2026-10-09 07:23

Тот же terminal48172 завершился с кодом0. Девять TRX полных проектов:5332 всего,5332 passed,0 failed,0 notExecuted; Axiom.Execution.Tests2702/2702, длительность3m9s. Точные счётчики прочитаны из XML, .artifacts/audit-full-1199-summary.json. Предыдущая промежуточная запись о живом процессе отражает состояние ожидания; окончательный статус заменяет её. Полный набор после накопленных тестовых изменений подтверждён. git diff --check прошёл.

Следующий шаг — отдельное воспроизведение точного чтения DECIMAL из строки и проверка планируемых языковых средств по текущему коду. Кандидаты доказательств314/314 не объявляются полной семантической проверкой. Общий аудит активен.


## Session 1200 - 2026-10-09 07:25 - точный DECIMAL ввод и готовая строка

Добавлен DecimalTextEvidenceTests: четыре выбранные строки × оба движка и два отказа CDEC(STRING), десять новых случаев. INPUT в DECIMAL сохраняет буквально заданные 2^53+1, длинные дроби и DECIMAL.MaxValue. CDEC(VAL(text)) не равен первым трём точным значениям и даётMB3008 для последнего. CDEC переменной STRING отклонён с MB2 до исполнения. Исходники реестра/общего парсера подтверждают числовую CDEC и прямой decimal.TryParse в INPUT. FUTURE не содержит отдельно такой формы.

Сначала восемь новых +78 NumericPresentationEvidenceTests —86/86; после двух проверок связывания —88/88, ошибок/пропусков0, .artifacts/test-results/decimal-text-1200/decimal-text-1200.trx. Создан DECIMAL_TEXT_EVIDENCE_20261009.md с ожидаемыми/фактическими результатами и границами выводов. Runtime не менялся. Последний полный набор5332/5332 Session1199 предшествует этим десяти новым тестам и не выдаётся за повторный полный прогон.

Классификация: для интерактивного ввода есть существующий точный путь в проверенных случаях; для готовой STRING специализированного преобразования нет. VAL/CDEC не сломаны своим договором DOUBLE/числового аргумента. Сценарий чтения десятичных данных из файла требует проверки пользовательского FUNCTION-разбора и грамотного выбора грамматики перед введением новой сущности. Не заявлена точность INPUT для всех строк: избыточная дробная точность может округляться.

Следующий шаг — оценить простой пользовательский точный разбор десятичной строки на границах и отделить удобство новой функции от необходимой возможности; затем сверить planned средства языка с исходниками. Общий аудит активен.


## Session 1201 - 2026-10-09 07:30 - пользовательский разбор DECIMAL и его пределы

Добавлен samples/functions/cdec/fixed_decimal_parser.axm: обычная FUNCTION с DECIMAL-мантиссой и масштабом, без DOUBLE. FixedDecimalParserEvidenceTests проверяет 17 строк × оба движка: восемь точных результатов, семь ошибок ограниченной грамматики APP_DECIMAL, два переполнения MB3008. Значения 2^53+1, положительная/отрицательная границы DECIMAL и длинные дроби имеют буквально заданные ожидаемые результаты. С DecimalTextEvidenceTests выполнено44/44, ошибок/пропусков0, .artifacts/test-results/decimal-parser-1201/decimal-parser-1201.trx. Пример реально запущен CLI, код0, ожидаемые числа/MB3008.

Простой алгоритм не заменяет общий разбор: максимальное DECIMAL с суффиксом .0 переполняет мантиссу, хотя число допустимо; 29 дробных позиций переполняют масштаб. Формат намеренно без пробелов, группировки и экспоненты, правила округления сверх точности не введены. Для ограниченного формата есть пользовательская альтернатива; для общего преобразования готовой STRING специализированной функции пока нет. Не классифицировано как баг CDEC и не принято решение добавить новую форму. Отчёты DECIMAL_TEXT_EVIDENCE_20261009.md и API_GAP_STATUS_20261009.md дополнены.

Runtime не менялся, публичный договор языка не расширен; пример использует существующие FUNCTION/CDEC/ASCW/MID. Последний полный прогон5332/5332 Session1199 предшествует новым тестам1200–1201; целевой44/44 не выдаётся за полный. Следующий шаг — проверить планируемые средства языка по текущим исходникам и отделить отсутствие реализации от давнего документационного статуса. Общий аудит продолжается.


## Session 1202 - 2026-10-09 07:33 - проверка планируемых конструкций и свойств карты

Добавлен PlannedSyntaxEvidenceTests: 14 отрицательных форм × два режима, три положительные альтернативы × два режима. Исходные26/26, после проверки MAPPROPERTY/MAPLAYERPROPERTY/MAPTILEPROPERTY/MAPOBJECTPROPERTY итог34/34, ошибок/пропусков0, .artifacts/test-results/planned-syntax-1202/planned-syntax-1202.trx. Проверены отказ до исполнения, пустой вывод, диагностики MB0/MB1/MB2 и отсутствие перехода на другой движок. Точный код/качество сообщения не закреплены этим набором.

Литералы массивов/словарей, FOR EACH, тройные кавычки, параметры по умолчанию, лямбды, DATA/READ/RESTORE, LINE INPUT и четыре формы пользовательских свойств Tiled пока не работают. Проверены существующие альтернативы: явное заполнение массива и FOR/LBOUND/UBOUND, именованная FUNCTION для ARRAYMAP, функция-обёртка для заданного аргумента, CHRW(10) в строке. Альтернативы имеют ограниченный проверенный объём и не объявляют новые формы реализованными. TILEMAP не является словарём; размеры/номера/флаги карты не являются пользовательскими свойствами role/solid. Планируемый статус пользовательских свойств не опровергнут существующими MAP-функциями.

Создан PLANNED_FEATURE_EVIDENCE_20261009.md, зафиксированы источники Parser.Expressions/Declarations/Statements, TypeSymbol и реестр. Runtime/публичный договор не менялись, новых работающих сущностей нет. Полного прогона решения не было. Следующий шаг — проверить INPUT STRING на запятых/кавычках/пустой строке/пробелах и потребность в LINE INPUT; затем загрузку пользовательских свойств Tiled. Общий аудит активен.


## Session 1203 - 2026-10-09 07:37 - INPUT сохраняет полную строку

Добавлен InputStringEvidenceTests: 12 одиночных строк × два движка и пять случаев двух STRING × два движка,34 новых. Одна STRING сохраняет строку буквально, включая пустую строку, пробелы/табуляцию/NBSP, запятые, кавычки, Unicode/emoji. Несколько целей делят строку по обычным запятым и удаляют краевые пробелы; кавычки не экранируют запятую. Для "А,Б",В при двух целях подтверждён MB3003 и сохранение обеих прежних строк. Поставщик вызывается один раз, перехода на интерпретатор нет. С PlannedSyntaxEvidenceTests68/68; после закрепления точного кода отказа повтор68/68, ошибок/пропусков0, .artifacts/test-results/input-string-1203/input-string-1203.trx.

Создан INPUT_STRING_EVIDENCE_20261009.md и samples/operators/input/whole_string.axm. Первая подсказка примера имела неверное экранирование кавычек и не прошла разбор; исправлена. Текущий пример реально запущен CLI со строкой из10 UTF-16 единиц, включая по два краевых пробела и кавычки: вывод сохранён буквально, код0. Пример не доказывает графический ввод/EOF/отмену/предел длины. manual/input.html уже отражает проверенный договор.

Классификация LINE INPUT уточнена: синтаксическая форма отсутствует, но полный строковый ввод уже доступен через INPUT одной STRING. Для этого сценария новый оператор не требуется. Многопеременный INPUT не является CSV-разбором. Runtime и публичное поведение не менялись; полного прогона решения не было. Следующий шаг — загрузка пользовательских свойств Tiled и соответствие модели действующему договору. Общий аудит активен.


## Session 1204 - 2026-10-09 07:39 - свойства и объекты теряются в загрузчике Tiled

Добавлен TilemapMetadataEvidenceTests: настоящий LoadFromTiledJson читает контрольную TMJ и TMJ со свойствами карты/слоёв/тайла/объекта и объектом Выход. Независимые размеры, клетки, слои и разрешение пути атласа проверены; публичная модель одинакова с метаданными и без них. properties=42/objects=42 также игнорируются без ошибки. Изображение1×1 передано callback, PNG/окно/паритет движков этими двумя случаями не проверяются. Набор2/2, ошибок/пропусков0, .artifacts/test-results/tile-metadata-1204/tile-metadata-1204.trx.

TILE26-001 P2: не только отсутствуют четыре PROPERTY-функции, но загрузчик и модель не сохраняют пользовательские свойства и объекты. Это незавершённая утверждённая возможность и нарушение явного требования не терять неподдержанные данные молча. Код LoadLayers создаёт объектный слой с cells=null без чтения objects; свойства нигде не разобраны. Создан TILEMAP_METADATA_EVIDENCE_20261009.md, реестр дефектов дополнен. Текущая официальная спецификация Tiled сверена для формы properties/objects; продуктовый объём остаётся проектным.

В TILEMAP_CONTRACT добавлено уточнение статуса: основной слой/клетки уже есть, пользовательские свойства/объекты и соответствующие функции ещё отсутствуют. Полное закрытие требует модели, загрузки, нормализации, функций обоих движков и сопутствующего manual/подсветки/примеров; один отказ загрузки не подменяет реализацию всего договора. Runtime не менялся, полного прогона решения не было. Следующий шаг — детальная карта оставшегося утверждённого тайлового договора и проверки нормализации свойств/объектов до реализации. Общий аудит активен.


## Session 1205 - 2026-10-09 07:45 - сохранение метаданных Tiled в модели и загрузчике

Начато исправление TILE26-001 по утверждённому полному договору, без подмены реализации одним отказом загрузки. RuntimeTilemapMetadata разбирает properties и точечные/прямоугольные objects. Карта и слои хранят свойства, слои хранят неизменяемый список объектов, ссылка на тайлсет хранит свойства тайлов по номеру1-based. Inline и внешние TSJ поддержаны. Внешний JsonDocument освобождается после Clone. RuntimeTilemapObject хранит name/class/type/kind/visible/x/y/width/height/properties. Class имеет приоритет перед type; точка получает нулевые размеры.

Приняты явные правила текстового представления: имена свойств сравниваются Ordinal с учётом регистра, строки сохраняются буквально, BOOLEAN→TRUE/FALSE, JSON-числа сохраняют исходную запись без DOUBLE и потери цифр, структурное class-значение хранится как JSON. properties/objects неверного вида, повторные имена/номера, неподдержанные значения, неверные координаты/размеры, неподдержанные формы объектов и анимация тайла дают InvalidDataException. Номера тайлов сверяются с фактическим атласом; отрицательный/id=int.MaxValue отклонён.

Старые две проверки потери данных после изменения закономерно перестали проходить0/2: они сохранены как исходное свидетельство в TRX tile-metadata-1205-before. Ожидания пересмотрены на сохранение/отказ, набор расширен до17/17: inline/external, malformed,12 отрицательных входов, точная числовая строка/точка/класс. Полный Axiom.Runtime.Tests1070/1070, ошибок/пропусков0, .artifacts/test-results/runtime-1205/runtime-1205.trx. TilePropertyAcceptanceSampleTests58/58 с настоящим загрузчиком IDE и обоими движками, .artifacts/test-results/tile-ide-1205/tile-ide-1205.trx. Существующее предупреждение CS9113 в DocumentOpenFormattingTests не связано с изменением. Полного решения в1205 не запускали.

Добавлены metadata_map.tmj, unsupported_object.tmj и запускаемый samples/functions/loadtilemap/metadata_validation.axm. Реальный IDE-путь в тестах загружает прямоугольный объект и перехватывает MB3005 для эллипса. manual/tilemaps.html описывает текущую загрузку, правила нормализации и ограничения; новых имён языка пока нет, подсветка не расширялась. Редакционный поиск по извлечённому Зельднеру не нашёл Tiled/тайлов; обзорный запасной жанр Microsoft Learn «Объекты и классы» просмотрен, семантика классов VB в Axiom не переносилась.

TILE26-001 не объявлен полностью исправленным: функции доступа к свойствам/объектам/слоям и оставшийся геометрический договор ещё не зарегистрированы; GraphicsHostService заменяет подробный отказ загрузки null, язык сообщает общийMB3005. Следующий шаг — зарегистрировать утверждённые функции доступа в обоих движках, добавить полноценные проверки пустых значений/границ/изменения клетки, подсветку, manual и русские приёмочные примеры; затем улучшить передачу причины отказа и выполнить полный прогон. Общий аудит активен.


## Session 1206 - 2026-10-09 07:58 - функции свойств, объектов и геометрии тайловой карты

Добавлены21 функции утверждённого TILEMAP_CONTRACT: число тайлсетов, четыре свойства, три функции слоёв, девять объектных функций, четыре перевода координат. Имена сохраняют проектный прецедент MAP-группы; классический BASIC не имеет Tiled-слоя. Новые BuiltinFunctionId дописаны в конец enum без сдвига прежних чисел. Реестр и обе диспетчеризации используют общий BuiltinTilemapMetadataHelpers. Отсутствующие данные дают пустую STRING/0/FALSE; LONG-индексы проверены до int, MAPTILEPROPERTY следует изменённой клетке. Полуоткрытые границы MAPCOLUMN/ROW и0 для неверного номера MAPWORLDX/Y описаны явно.

Добавлены access_map.tmj и21 русских samples/functions/<имя>/map_access.axm. Реальный IDE-набор255/255:100 приёмочных запусков,130 буквально заданных проверок доступа/границ/изменения клетки,21 проверка форматирования и синхронизация словарей. Первый запуск выявил порядок XSHD123/125, слова отсортированы; итог255/255. Первая сборка выявила различие IReadOnlyList/ReadOnlySpan аргументов, адаптер исправлен. Неверные вызовы TileMetadataBindingTests88/88; PlannedSyntaxEvidenceTests после удаления четырёх устаревших отрицательных PROPERTY-форм26/26. Все наборы без ошибок/пропусков и перехода на другой движок.

manual/tilemaps.html получил21 отдельный раздел с аргументами/результатами/краями/диагностикой/полными примерами, поисковый индекс обновлён; обе темы подсветки синхронны. Ориентиры — отсутствие Tiled в извлечённом Зельднере и текущая официальная Information.TypeName как жанр функции чтения данных; семантика VB не переносилась. Добавлен TILEMAP_ACCESS_EVIDENCE_20261009.md и TILE_METADATA_FORM_EVIDENCE_20261009.csv. Свежие inventory-only/CLI metadata335 форм/271 имя,335 кандидатов, без дублей/пропусков диспетчеризации/подсветки. GeneratedAt07:56:11+03; SemanticStatus UNVERIFIED_PER_FORM сохранён.

Запущен единственный dotnet test Axiom.sln, .artifacts/test-results/tile-full-1206, terminal5696. На момент записи процесс подтверждён живым, итог решения пока не объявлен. TILE26-001 остаётся частично исправленным: доступ из языка реализован, но подробная причина отказа загрузки теряется в GraphicsHostService. Следующий шаг — дождаться текущего процесса и проверить все TRX, затем сохранить причину отказа загрузки и проверить оставшийся неподдержанный Tiled-объём. Общий аудит активен.


### Завершение Session 1206 — 2026-10-09 08:01

Тот же terminal5696 завершился с кодом0. Проверены девять TRX:5738 всего,5738 passed,0 failed,0 notExecuted; Runtime1070, Execution2896, IDE856. Точные счётчики и имена файлов сохранены в .artifacts/tile-full-1206-summary.json. Промежуточная запись о живом процессе заменяется этим окончательным результатом. Ссылки/разделы/примеры/слова обеих тем найдены для всех21 новых функций; git diff --check прошёл.

Сохранение данных и доступ через язык подтверждены полным набором, но TILE26-001 не объявлен целиком закрытым из-за потери причины отказа в GraphicsHostService. Следующий шаг — сохранить конкретную причину LOADTILEMAP и проверить её в обоих движках на неподдержанных/неверных данных; затем расширить проверку объявленного Tiled-объёма. Общий аудит остаётся активным,335 кандидатов доказательств не равны полной проверке всех форм.


## Session 1207 - 2026-10-09 08:06 - причина отказа LOADTILEMAP сохраняется

UnsupportedObjectKeepsReasonInLanguageCatch воспроизвёл потерю причины в обоих режимах:0/2, .artifacts/tile-reason-1207-before.log. Вместо сообщения об ellipse язык возвращал только общий отказ загрузки. GraphicsHostService теперь переводит ожидаемые исключения загрузчика в InvalidDataException с сохранением исходного текста/InnerException. Interpreter и BytecodeVm перехватывают этот договор и сообщаютMB3005 с исходным текстом, доступным err.Message; прежний null по недоступному ресурсу сохраняется.

Быстрый LOADTILEMAP в VM использует тот же EvaluateLoadTilemapBuiltin с точной позицией инструкции, а не повторяет загрузку через медленный путь после отказа. TilemapLoadFailureTests2/2: первый вызов с причиной перехвачен, следующий явный вызов успешен; ровно два обращения к хосту для двух команд, без перехода на интерпретатор. Адресный реальный IDE-набор232/232 включает оба новых сообщения и прежние проверки доступа/примеров. Обновлён metadata_validation.axm: печатает err.Message. manual/tilemaps.html описывает текст причины и отсутствие повторной загрузки; новых токенов/форм/идентификаторов нет.

Запущен один полный dotnet test Axiom.sln, terminal37971, .artifacts/test-results/tile-reason-full-1207. На момент записи процесс подтверждён живым; Runtime1070/1070, Execution выполняется. Итог всего решения не объявлен. Последний полный5738 Session1206 предшествует текущей правке. Следующий шаг — дождаться этого же процесса и сверить девять TRX; затем проверить оставшиеся неподдержанные/неверные Tiled-входы. Общий аудит активен.


### Завершение Session 1207 — 2026-10-09 08:10

Тот же terminal37971 завершился с кодом0. Девять TRX:5742 всего,5742 passed,0 failed,0 notExecuted; Runtime1070, Execution2898 (3m24s), IDE858. .artifacts/tile-reason-full-1207-summary.json содержит окончательные счётчики. Промежуточные записи о выполнении и неполная сводка по шести готовым TRX не являются итогом; окончательный результат подтверждён только после завершения процесса. git diff --check прошёл.

TILE26-001: подтверждённая потеря поддержанных свойств/объектов и причины отказа исправлена и проверена в пределах карточки. Это не объявление полного соответствия всем разновидностям Tiled. Следующий шаг — самостоятельные проверки неверных размеров и JSON-типов, особенно ArgumentException при построении карты/тайлсета, чтобы исключение .NET не выходило наружу вместо диагностики языка; затем остальные явно отложенные формы. Общий аудит активен.


## Session 1208 - 2026-10-09 08:17 - неверные поля Tiled дают диагностику языка

TilemapInvalidInputTests воспроизвёл12 вариантов × оба движка:6/24 passed,18 failed. TILE26-002 P2:16 случаев обычных неверных/отсутствующих полей дали InfrastructureFailure вместо CATCH/MB3005. TILE26-003 P2:infinite="да" принят в двух режимах. Исходный TRX/log сохранён в tile-invalid-1208-before. Это не заявлено как падение приложения IDE.

ReadMapInteger проверяет наличие/JSON-тип/INTEGER-диапазон/знак размеров, ReadMapArray — наличие и массив layers/tilesets. ValidateMapRoot требует BOOLEAN для заданного infinite. GraphicsHostService.LoadTilemap дополнительно переводит ожидаемые ArgumentException/KeyNotFoundException/FormatException вложенных данных в существующий договор InvalidDataException→MB3005, без blanket catch. Причина сохраняется. Новых функций/идентификаторов нет.

Набор расширен до40 случаев: ошибки слоя/тайлсета и неверная сетка реального PNG включены. Каждый отказ перехвачен, прежняя ширина карты1 сохранена, перехода на интерпретатор нет. Дополнительные16 случаев проверены после изменения и не выдаются за исходное воспроизведение. Адресный IDE-набор274/274; далее полный Runtime1070/1070 и полный IDE900/900, ошибок/пропусков0, .artifacts/tile-invalid-1208-summary.json. MSBuild-проверки последовательны. Полного решения в1208 не было;5742/5742 Session1207 предшествует текущим изменениям.

Создан TILEMAP_INVALID_FIELDS_20261009.md, добавлены samples/functions/loadtilemap/invalid_dimensions.axm и negative_dimensions.tmj, пример реально выполнен обоими движками через IDE. manual/tilemaps.html описывает обязательные поля/типы/знаки и прежнее значение после отказа. TILE26-002/003 исправлены в проверенном объёме. Следующий шаг — ранние отказы для всего явно отложенного Tiled-объёма; не считать поддержанными молча проигнорированные поля. Общий аудит активен.


## Session 1209 - 2026-10-09 08:25 - Явный отказ для отложенных возможностей Tiled

TILEMAP_UNSUPPORTED_FEATURES_20261009.md: исходные44 проверки дали30 успешных и14 ошибочных результатов. TILE26-004 P2 — потеря wangsets/terrains/tiles[].terrain встроенного и внешнего тайлсета; TILE26-005 P2 — capsule ошибочно принят как RECT. Исправлен ранний отказ через MB3005 с причиной, без расширения отложенного объёма языка. Наличие соответствующего поля, включая пустой массив, вызывает отказ.

Изменены RuntimeTilemapValue.cs, RuntimeTilemapMetadata.cs, добавлены TilemapUnsupportedFeatureTests и запускаемый unsupported_tileset_metadata.axm с unsupported_wangsets.tmj, обновлены TilePropertyAcceptanceSampleTests и manual/tilemaps.html. Адресные проверки320/320; пример выполнен обоими движками. Полные Runtime/IDE проверяются последовательно. Следующий шаг — независимая проверка остальных параметров слоя и тайлсета, включая режимы смешивания и геометрию изображения; общую проверку языка не считать завершённой.


Проверки Session 1209 завершены: полный Runtime1070/1070 и полный IDE946/946, ошибок и пропусков нет; процессы завершились кодом0. Сводка .artifacts/tile-unsupported-1209-summary.json. Проверки MSBuild выполнены последовательно. git diff --check прошёл. Полный набор всего решения в этой сессии не запускался.


## Session 1210 - 2026-10-09 08:30 - Числовые границы параметров слоя Tiled

До исправления16 реальных проверок загрузки через IDE дали2 успешных и14 ошибочных результатов. TILE26-006 P2 — opacity вне[0,1] принимается; TILE26-007 P2 — 1e400/-1e400 сохраняются как бесконечность в opacity/offset/parallax. Добавлен ReadLayerNumber с проверкой конечности DOUBLE, отдельная проверка диапазона opacity. Это исправление поддержанного договора, а не новый API.

Изменены RuntimeTilemapValue.cs, TilemapLayerNumericTests, TilePropertyAcceptanceSampleTests, manual/tilemaps.html. Добавлен русскоязычный samples/functions/loadtilemap/nonfinite_layer.axm с nonfinite_layer.tmj. Первые адресные344/344 прошли; итоговый новый набор расширен до44 случаев обоих движков и типов слоя, с точным текстом причины и положительными контрольными значениями. Полные проверки выполняются последовательно. Отчёт docs/audit/TILEMAP_LAYER_NUMERIC_20261009.md. Следующий шаг — mode/tintcolor и геометрия тайлсета, затем большие конечные координаты при рисовании. Общий аудит не завершён.


Итог Session1210: полный Runtime1070/1070, полный IDE992/992; ошибок и пропусков0, оба процесса завершились кодом0. Итоговый набор TilemapLayerNumericTests44/44 вошёл в полный IDE, включая точные причины и оба вида слоя. nonfinite_layer.axm выполнен обоими движками через IDE. Сводка .artifacts/layer-numeric-1210-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг остаётся mode/tintcolor, геометрия тайлсета и большие конечные координаты при рисовании.


## Session 1211 - 2026-10-09 08:34 - Настройки отображения слоёв Tiled

Уточнённое исходное воспроизведение:8/32 успешно,24/32 ошибочно. TILE26-008 P2 — молчаливое принятие ненейтрального/неверного mode; TILE26-009 P2 — цветной/неверный tintcolor. Начальные4 дополнительных отказа для белого цвета исключены из дефектов после проверки нейтрального поведения, оба исходных TRX сохранены. Это отсутствие возможности вне первого объёма и нарушение явного отказа, а не основание вводить новые функции языка.

ValidateLayerAppearance допускает только отсутствующий mode/normal и отсутствующий tintcolor/#FFFFFF/#FFFFFFFF, с учётом нейтральности непрозрачного белого. Остальные значения дают MB3005 с конкретной причиной и сохраняют прежнюю карту. Изменены RuntimeTilemapValue.cs, добавлены TilemapLayerAppearanceTests44 случая, unsupported_blend.axm/unsupported_blend.tmj, обновлены TilePropertyAcceptanceSampleTests и manual/tilemaps.html. Отчёт docs/audit/TILEMAP_LAYER_APPEARANCE_20261009.md. Проверки выполняются последовательно. Следующий шаг — геометрия тайлсета и большие конечные координаты рисования; общий аудит активен.


Итог Session1211: адресные проверки412/412; полный Runtime1070/1070 и полный IDE1038/1038. Ошибок и пропусков0, процессы завершились кодом0. TilemapLayerAppearanceTests44/44 и unsupported_blend.axm обоих движков вошли в проверки. Сводка .artifacts/layer-appearance-1211-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг — геометрия тайлсетов и большие конечные координаты при рисовании; общий аудит языка остаётся активным.


## Session 1212 - 2026-10-09 08:39 - Переполнение геометрии регулярного атласа

TILE26-010/011 P2: реальный PNG32×32 с margin=2147483647 и тайлом17×17 ошибочно даёт4 тайла; один тайл32×32 со spacing=2147483647 ошибочно отклоняется. До исправления0/4 успешно в двух движках. Общая RuntimeTilesetValue переведена на промежуточную long-арифметику длины, шага и координат, без новых API.

Добавлены TilesetArithmeticTests5 случаев, TilesetArithmeticBoundaryTests8 случаев обоих движков/обоих загрузчиков, samples/functions/loadtileset/arithmetic_boundaries.axm и карты large_margin.tmj/large_spacing.tmj. Обновлены TilePropertyAcceptanceSampleTests и manual/tilesets.html. Полный Runtime1075/1075; полный IDE выполняется последовательно. Отчёт docs/audit/TILESET_ARITHMETIC_20261009.md. Следующий шаг — экстремальный tileoffset и видимый диапазон карты при больших конечных координатах. Общий аудит активен.


Итог Session1212: полный Runtime1075/1075 и полный IDE1048/1048, ошибок и пропусков0, процессы завершились кодом0. Пять новых прямых проверок и восемь проверок реальной загрузки вошли в полные наборы, arithmetic_boundaries.axm выполнен обоими движками. Сводка .artifacts/tileset-arithmetic-1212-summary.json. Проверки MSBuild последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг — tileoffset и большие координаты рисования; общий аудит языка не завершён.


## Session 1213 - 2026-10-09 08:47 - Числовые границы видимости тайловой карты

Первоначальные8 отказов оказались корректной диагностикой координат вне±1000000000; они исключены из дефектов. Проверки повторены с DRAWTILEMAP0,0 и большими смещениями слоя. Уточнённый исходный опыт0/8 успешно:4 потерянных пикселя(12→0) при взаимной компенсации tileoffset и offset слоя,4 InfrastructureFailure для смещения слоя±100000000000. Отдельный контроль со смещением40000000000 до изменения проходит. Все исходные TRX сохранены раздельно.

TILE26-012/013 P2:экстенты tileoffset+размер переполняют int, номер клетки приводится к int до ограничения. RuntimeTilemapValue хранит экстенты long, TilemapRenderSupport возвращает long из деления и приводит номер после ограничения. Добавлены TilemapExtentBoundaryTests4 случая, TilemapCullingBoundaryTests8 случаев и2 запуска примера, culling_boundaries.axm с4 картами; обновлён manual/tilemaps.html. Полный Runtime1079/1079, полный IDE выполняется последовательно. Отчёт docs/audit/TILEMAP_CULLING_BOUNDARIES_20261009.md. Следующий шаг — крайние конечные offset/parallax и произведение камеры на параллакс при допустимых координатах оператора. Общий аудит активен.


Итог Session1213: полный Runtime1079/1079 и полный IDE1058/1058, ошибок и пропусков0, процессы завершились кодом0. Новые10 IDE-проверок прошли:8 подтверждённых граничных случаев и2 запуска culling_boundaries.axm; видимый пиксель совпадает с независимым DRAWTILE(12), далёкий слой завершается нормально. Сводка .artifacts/tile-culling-1213-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Книга Зельднера:графический PUT и пример мяча, строки8844–8911 извлечённого текста, только редакционный мотив. Общий аудит языка активен; крайние DOUBLE-произведения/long-суммы остаются следующей независимой проверкой.


## Session 1214 - 2026-10-09 08:55 - Крайние смещения и параллакс

Исходный реальный набор0/28 успешно:20 ошибок при крайних смещениях/long-суммах/компенсации и8 неверных пикселей12 вместо0 при camera*parallax→Infinity. TILE26-014/015 P2. Все координаты операторов/камеры допустимы, поля JSON конечные. TilemapRenderSupport вычисляет относительное начало через Int128, для больших конечных значений — точное вычитание округлённых BigInteger; бесконечное произведение приводит к отсечению, а не0. Округление половин от нуля сохранено.

Первый адресный набор38/38 прошёл; итоговые80 случаев покрывают обе оси/движка/оператора, отрицательную компенсацию и половинные значения. Добавлены TilemapExtremeTransformTests, extreme_parallax.axm с20 короткими картами extreme_<номер>_<ось>.tmj, обновлён manual/tilemaps.html. Отчёт docs/audit/TILEMAP_EXTREME_TRANSFORMS_20261009.md. Полный Axiom.sln выполняется одним процессом, окончательная сводка только после завершения. Быстродействие не измерялось. Следующий шаг — parallaxorigin карты и оставшаяся геометрия тайлсета, затем непроверенные формы общей инвентаризации Unicode/файлов/чисел. Общий аудит активен.


Завершение Session1214: тот же процесс51040 завершился с кодом0. Проверены9 TRX, всего6033/6033 успешно, ошибок и пропусков0; Runtime1079, Execution2898, IDE1140. Сводка .artifacts/full-1214-summary.json. Новый набор80 случаев и2 запуска примера прошёл. Пользователь попросил завершить только текущую тайловую часть, затем сделать сводный MD, commit/push и точку продолжения для другого компьютера; следующий раздел аудита до возобновления не начинать.


### Session 1215 — завершение текущего тайлового этапа

См. docs/audit/TILEMAP_LOADER_CLOSURE_20261009.md: адресный набор494/494; строгие границы загрузки и выбор firstgid проверены обоими движками. Добавлен samples/functions/loadtilemap/loader_boundaries.axm и описание в manual/tilemaps.html. Отложенные особенности Tiled отклоняются с MB3005, а не объявляются реализованными. Следующий раздел общего аудита пока не начинать. Точка возобновления и сводная таблица — AUDIT_HANDOFF.md в корне репозитория.


### Session 1216 — точка передачи

Последний завершённый раздел: тайлы, Session1215. Следующий раздел ещё не начат. Полная сводная таблица и порядок возобновления находятся в AUDIT_HANDOFF.md в корне. После получения master на другом компьютере прочитать этот файл, затем выбирать следующую недоказанную форму из общей матрицы; не объявлять весь аудит закрытым по числу общих тестов.

### Session 1218 — возобновление после передачи

Закрыт адресный участок GRAPHEME на восьми повреждённых строках и неверных границах: 71/71 совместно, дефект не обнаружен. См. docs/audit/UNICODE_MALFORMED_GRAPHEME_20261009.md. Далее — независимые ожидания INSTR/INSTRREV для старта внутри пары UTF-16, непарного суррогата и пустого фрагмента. Полнота Unicode, файлов и чисел не объявлена.

### Session 1219 — поиск на границах UTF-16

INSTR/INSTRREV проверены 48 новыми случаями, совместно105/105, дефект не обнаружен. Отчёт docs/audit/UNICODE_SEARCH_BOUNDARIES_20261009.md. Следующий участок — сверить числовые CSV и тесты CINT/CLNG/CDEC, затем проверить непокрытые половинные значения и границы округления. Не повторять уже доказанные случаи и не вводить строковую CDEC без отдельного основания.

### Session 1220 — непрерывный аудит и PATH26-002

Числовые соседние границы:146/146. Ошибочный приём NUL исправлен до раскрытия переменных окружения; адресный набор56/56. Отчёт docs/audit/PATH_NULL_CHARACTER_20261009.md. Полный прогон выполняется; следующим шагом зафиксировать его итог, затем доказать сохранность файлов при отказе WRITEFILE/DELETEFILE на пути с NUL. Режим goal активен, общий аудит не завершён.

Уточнение Session1220: процесс53619 завершился с кодом1 из-за сборки IDE без актуального восстановления зависимостей; Execution3009/3009 прошёл. Запущен последовательный повтор dotnet test Axiom.sln с restore, процесс83732, результаты .artifacts/test-results/full-1220-restored. Пока выполняется, итог не объявлен. PathNullMutationEvidenceTests уже добавлен (6 случаев сохранности WRITEFILE/APPENDFILE/DELETEFILE), войдёт в повторный полный прогон. После завершения сверить TRX и продолжить кандидаты ISABSOLUTEPATH/ResolveResourcePath; режим goal остаётся активным.

Завершение Session1220: процесс83732 завершился с кодом0. Девять TRX:6228/6228, ошибок и пропусков0; Execution3015, IDE1218. Сводка .artifacts/full-1220-restored-summary.json. Шесть PathNullMutationEvidenceTests вошли в полный результат: WRITEFILE/APPENDFILE/DELETEFILE с NUL в обоих движках отказали MB3007 и сохранили настоящие двоичные байты. Ошибка сборки IDE без restore устранена восстановлением зависимостей, исходные проекты не менялись. Следующая проверка — отдельный обработчик пути графических ресурсов.

### Session 1221 — отдельные пути ресурсов

PATH26-003 воспроизведён0/8, исправлен, адресно186/186. Полный IDE-набор ещё выполняется. Следующий участок — вложенные пути Tiled (image/source с NUL), затем сверка оставшихся форм чисел и Unicode. ISABSOLUTEPATH не объявлять дефектом без собственного договора проверки корректности. Отчёт docs/audit/RESOURCE_NULL_PATH_20261009.md.

Завершение Session1221: полный IDE-набор1228/1228, ошибок/пропусков0, процесс43181 завершился с кодом0; .artifacts/test-results/resource-null-1221/ide-full-1221.trx. Общий аудит продолжается, новая проверка вложенных путей выполняется отдельно.

### Session 1222 — вложенные пути проверены

Три места NUL в TMJ/TSJ × оба движка:6 новых, совместно16/16, дефект не обнаружен. Отчёт docs/audit/NESTED_RESOURCE_NULL_PATH_20261009.md. Далее REVERSE и построение строк: сверить существующие доказательства, выбрать непокрытые смешанные суррогаты. Полнота ресурсов не объявлена.

### Session 1223 — REVERSE и учёт возобновлённых доказательств

16 новых случаев REVERSE, совместно54/54, дефект не найден. Снимок docs/audit/RESUMED_FORM_PROGRESS_20261009_1223.csv сохраняет23 адресно рассмотренных формы из335 с явными пределами;312 не рассмотрены именно в новом проходе, прежние доказательства не отменены. Далее — переменные окружения и пути, обычные/неизвестные переменные с реальным файлом и оба движка. Общий аудит открыт.

### Session 1224 — раскрытие окружения проверено

4 новых независимых случая и2 запуска примера, совместно27/27. PATH26-004 исправлен только в руководстве ISABSOLUTEPATH; Runtime не менялся. Отчёт docs/audit/PATH_ENVIRONMENT_20261009.md. Далее файловые сведения при путанице файла/каталога, сверка существующих тестов до новых ожиданий.

### Session 1225 — вид объекта и сведения

12 новых случаев, совместно20/20; неверный файл/каталог корректно дают MB3007, атрибуты каталога поддержаны. Отчёт docs/audit/FILE_METADATA_KIND_20261009.md. Далее STR/VAL на отрицательном нуле и крайних DOUBLE, три культуры, знак проверять отдельно от равенства нулей.

### Session 1226 — числовой текст и текущий учёт

42 новых случая STR/VAL на краях DOUBLE и знаке нуля, совместно160/160. Дефекта нет. Актуальный снимок RESUMED_FORM_PROGRESS_20261009_1226.csv:37 адресных форм из335 с явными ограничениями. Далее READFILE(path, encoding): существующие проверки, BOM, неверная кодировка; не повторять уже доказанные случаи.

### Session 1227 — явная кодировка и BOM

36 новых случаев, совместно68/68, дефект не найден. Текущий снимок RESUMED_FORM_PROGRESS_20261009_1227.csv:41 адресно рассмотренная форма с пределами. Далее FileLineBatchEvidenceTests и READLINES на неверном count/закрытом FILE, включая сохранность позиции после отказа.


### Session 1228 — предел READLINES

FILE26-002 исправлен, адресно22/22. Полный прогон ожидается. Далее READTEXT/READBYTES, независимые границы количества и отсутствие преждевременного выделения памяти на коротком файле.


### Session 1229 — предел READTEXT

FILE26-003 исправлен,22/22. Дождаться Runtime TextFile. Далее выбрать неподтверждённую числовую форму по адресному снимку; READBYTES отсутствует в текущем языке.


### Session 1230 — точное округление

24 новых случая, совместно58/58; дефекта нет. Снимок1230:49 адресных форм. Далее CDEC из DOUBLE: предел диапазона, малые величины и ноль, сначала сверить старые доказательства.


### Session 1231 — CDEC из DOUBLE

30 новых случаев, совместно118/118; дефекта нет. Далее CDBL из точного DECIMAL, границы2^53 и минимальные дроби; сверить прежние проверки продвижения типов.


### Session 1232 — CDBL и переход к Unicode

32 новых случая, совместно132/132; дефекта нет. Далее ASC/CHR: договор UTF-16, суррогаты/пустая строка/числовые границы, сначала сверить существующие проверки.


### Session 1233 — символьные коды

20 новых случаев, совместно58/58, дефекта нет. Далее TRIM/LTRIM/RTRIM: Unicode-пробелы/явный набор/суррогатные границы; сначала прочитать прежние доказательства.


### Session 1234 — обрезка Unicode

62 новых случая, совместно100/100, дефекта нет. Далее REPLACE/REPLACEMANY: повреждённые пары/пустой фрагмент/каскадирование; сначала сверить прежние доказательства.


### Session 1235 — замена строк

6 новых групповых случаев, совместно63/63, дефекта нет. Далее SPLIT/JOIN: половина пары/NUL/пустые элементы после сверки прежних доказательств.


### Session 1236 — разделение строк и файловые перечисления

6 новых групповых случаев, совместно59/59; дефекта нет. Далее GETFILES/GETDIRS: прочитать DirectoryListingEvidenceTests/DirectoryTraversalContractTests, выбрать реальный пробел масок/Unicode-имён.


### Session 1237 — Unicode-маски перечисления

20 новых случаев, совместно28/28; дефекта нет. Далее COPYFILE/MOVEFILE: SameFileTransferEvidenceTests/FileTransferFailureEvidenceTests, сохранность реальных файлов на непроверенных формах.


### Session 1238 — отказы переноса файлов

12 новых случаев, совместно44/44; дефекта нет. Далее FileRenameDeleteEvidenceTests и RENAMEFILE/RENAMEDIR: простые/неверные имена, сохранность. Затем TEMP-формы по адресному снимку.


### Session 1239 — неверное имя переименования

28 новых случаев, совместно42/42, дефекта нет. Далее MAKETEMPFILE/MAKETEMPDIR: старые проверки и границы префикса; не подменять TEMP процесса для параллельных тестов.


### Session 1240 — временные префиксы

52 новых случая, совместно56/56, дефекта нет. Далее INT/ABS/SQR: прочитать прежние независимые проверки, выбрать предельные значения/знаки без дублирования обычных случаев.


### Session 1241 — основные числовые границы

30 новых случаев, совместно152/152; дефекта нет. Далее MIN/MAX/CLAMP: точные крайние DECIMAL/LONG, сначала матрица продвижения и прежние значения.


### Session 1242 — числовой выбор

6 новых групповых случаев, совместно130/130; дефекта нет. Далее LOG/LOG10/LOG2/EXP/POW: точные степени двойки, малые величины, прежние домены/переполнение.


### Session 1243 — логарифмы и точность POW

160/160 ограниченных проверок; NUM26-CAND-001 остаётся открытым: POW(2,-1075) не даёт математически ожидаемое округление к0, наследуя Math.Pow. Перед закрытием аудита определить требование точности, не считать ограниченный тест доказательством правильного округления. Далее CBRT/тригонометрические формы, независимые точные значения/домены/знаки нуля.


### Session 1244 — тригонометрические границы

44 новых случая, совместно166/166, дефекта нет. Далее SINH/COSH/TANH/ASINH/ACOSH/ATANH: крайние/малые значения/домены/нули. NUM26-CAND-001 точности POW сохраняется.


### Session 1245 — гиперболические границы

48 новых случаев, совместно170/170, дефекта нет. Далее SETSIGN/RND: прежние знаки/переполнение/управляемый генератор; затем ARRAYSUM/ARRAYMIN/ARRAYMAX/ARRAYAVERAGE. NUM26-CAND-001 сохраняется.


### Session 1246 — знаки и генератор

66 новых случаев, совместно103/103, дефекта нет. Далее ARRAYSUM/ARRAYMIN/ARRAYMAX/ARRAYAVERAGE: ArrayAggregateContractTests и реализации перед точными/пустыми/переполненными наборами. NUM26-CAND-001 открыт.

## Session 1247 — агрегаты массивов и DECIMAL

Подтверждён ARRAY26-001 P2: ARRAYAVERAGE массива DECIMAL выбрасывал InvalidOperationException вне диагностики языка, поскольку RuntimeValue.AsDouble не принимает DECIMAL. Общая реализация теперь явно преобразует DECIMAL перед точным накоплением DOUBLE. Новые24 случая проверяют пустые/невыделенные массивы, крайние LONG/DECIMAL, промежуточное переполнение суммы, двумерные DOUBLE и сохранность источника; включая decimal_mean.axm в обоих движках. Совместно67/67; независимые проверки точного среднего Runtime5/5. Уточнено руководство: невыделенный массив MB3005, выделенный пустой MB3007. Полный прогон запущен, его итог пока не установлен.

Отчёт docs/audit/ARRAY_AGGREGATE_BOUNDARIES_20261009.md. Снимок1247:109 ограниченных форм из335,226 ещё не рассмотрены. Далее строковые ARRAYMIN/ARRAYMAX и представления массивов: Unicode, порядок сравнения и границы видимой области. NUM26-CAND-001 открыт, общий аудит/goal активны.

### Итог полного прогона Session1247

После исправления ARRAY26-001 полный dotnet test Axiom.sln --no-restore завершён успешно:6930/6930, ошибок/пропусков0, девять отдельных TRX в .artifacts/test-results/full-1247. Включены прежние исправления READTEXT/READLINES/путей и новые проверки. Это автоматическая регрессия, а не закрытие всех335 форм или открытого NUM26-CAND-001. Далее Unicode строковых агрегатов/представления; goal активен.

## Session 1248 — Unicode и видимая область агрегатов

ArrayAggregateUnicodeViewEvidenceTests:4 новых групповых случая обоих движков. ARRAYMIN/ARRAYMAX сравнивают UTF-16: канонически разные строки, суррогатная пара против U+E000, отдельные половины, префикс пары, пустая строка/NUL и строка с конечным NUL. Двумерный DECIMAL-срез и вложенное частичное индексирование читают только видимые элементы, сохраняют источник; четыре агрегата и точное среднее0.5. Совместно52/52, ошибок/пропусков0. Первые прогоны исправляли ошибки теста: ROW/COLUMN — существующие константы; сумма DECIMAL печатается4.00 с сохранённым масштабом. Runtime/поведение не менялись, новый пример не требуется. Отчёт docs/audit/ARRAY_AGGREGATE_UNICODE_VIEWS_20261009.md. Снимок1248 сохраняет109 ограниченных форм из335,226 ещё не рассмотрены. Далее LCASE/UCASE: прежние независимые/поимённые проверки и Unicode за пределами BMP, смешанные непарные суррогаты. NUM26-CAND-001 открыт, goal активен.

## Session 1249 — регистр Unicode за пределами BMP

CaseSupplementaryUnicodeEvidenceTests:2 новых групповых случая обоих движков, простые соответствия U+10400/U+10428 и U+03C2/U+03A3 сверены с официальным UnicodeData. Проверены длина UTF-16, смешанные непарные суррогаты/NUL, комбинируемый акцент, неизменность эмодзи и предельного скаляра. Совместно40/40 с прежними поимёнными/независимыми проверками, включая три культуры. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CASE_SUPPLEMENTARY_UNICODE_20261009.md. Снимок1249:111 ограниченных форм из335,224 ещё не рассмотрены. Далее REPEAT/SPACE/STRING/PADLEFT/PADRIGHT/CENTER: единицы UTF-16 и заполнение, после чтения прежних доказательств. NUM26-CAND-001 открыт, goal активен.

## Session 1250 — заполнение строк и непарные суррогаты

StringFillMalformedUnicodeEvidenceTests:2 новых групповых случая обоих движков,19 буквальных ожиданий каждый. REPEAT сохраняет весь фрагмент, STRING/явное заполнение используют первую единицу UTF-16, SPACE/неявное заполнение используют пробел; проверены непарные половины/NUL, пустая строка, ширина меньше/равна длине и нечётное дополнение CENTER справа. Совместно55/55 с прежними независимыми/поимёнными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/STRING_FILL_MALFORMED_UNICODE_20261009.md. Снимок1250:120 ограниченных форм из335,215 ещё не рассмотрены. Далее INSERT/REMOVE: границы UTF-16/непарные половины и предельные позиции после сверки прежнего договора. NUM26-CAND-001 открыт, goal активен.

## Session 1251 — редактирование единиц UTF-16

StringEditMalformedUnicodeEvidenceTests:2 новых групповых случая обоих движков,13 буквальных ожиданий каждый. INSERT/REMOVE разделяют и вновь соединяют суррогатную пару, сохраняют непарные половины/NUL, работают с комбинируемым акцентом и предельными позициями/количествами без переполнения индекса. Совместно55/55 с прежними независимыми/поимёнными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/STRING_EDIT_MALFORMED_UNICODE_20261009.md. Снимок1251:122 ограниченные формы из335,213 ещё не рассмотрены. Далее открытые файловые формы FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE: прежние доказательства, закрытые состояния и сохранность позиции. NUM26-CAND-001 открыт, goal активен.

## Session 1252 — свойства закрытого FILE и позиция Unicode

FileClosedPropertiesEvidenceTests:20 новых случаев, оба движка, неоткрытый/закрытый FILE и FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE. MB3007 перехватывается, FINALLY/продолжение выполняются, ISOPEN остаётся FALSE, исходные UTF-8-байты сохранены и дескриптор доступен эксклюзивно. Совместно62/62 с FilePositionBufferEvidenceTests/FileLineBatchEvidenceTests, включая UTF-8/UTF-16LE/BE на границах буфера и сохранность позиции после неверного SEEKFILE. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_CLOSED_PROPERTIES_20261009.md. Снимок1252:127 ограниченных форм из335,208 ещё не рассмотрены. Далее свойства/позиционирование OUTPUT/APPEND: FileOutputPositionEvidenceTests и сохранность после отказа APPEND. NUM26-CAND-001 открыт, goal активен.

## Session 1253 — отказ SEEKFILE после буферизованного APPEND

AppendBufferedSeekFailureEvidenceTests:2 новых случая обоих движков, UTF-8 без BOM, запись😀 до запрещённого SEEKFILE(0/LONG.MaxValue), FINALLY/сохранность позиции6 и длины6, последующая запись NUL+Б до9. Итоговые9 байтов буквальные; дескриптор освобождён. Совместно34/34 с прежними OUTPUT/APPEND и закрытыми состояниями, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/APPEND_BUFFERED_SEEK_FAILURE_20261009.md. Снимок1253 сохраняет127 ограниченных форм из335. Далее FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE: проверить статические типы/неверные аргументы по зарегистрированным формам, затем оставшиеся файловые формы. NUM26-CAND-001 открыт, goal активен.

## Session 1254 — зарегистрированные формы свойств FILE

FilePropertyFormEvidenceTests:12 новых случаев, пять точных BuiltinId/типов результатов через Parser/Binder и семь неверных аргументов с SemanticFailure/совпадающими местами диагностики обоих режимов. FILEPATH/FILEMODE возвращают STRING, FILEPOS/FILELENGTH — LONG, SEEKFILE — BOOLEAN. Совместно34/34 с закрытыми состояниями и APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_PROPERTY_FORMS_20261009.md. Снимок1254 сохраняет127 ограниченных форм из335. Далее ISOPEN/EOF/LINENUMBER/REWIND/FLUSH: прежние проверки, типы/неподходящие режимы и сохранность состояния. NUM26-CAND-001 открыт, goal активен.

## Session 1255 — неподходящий режим FILE

FileModeStateEvidenceTests:12 новых случаев обоих движков, FLUSH в INPUT, EOF/LINENUMBER в OUTPUT/APPEND и REWIND в APPEND. MB3007/FINALLY сохраняют ISOPEN TRUE/позицию и буфер Unicode; CLOSE даёт FALSE, точные байты и эксклюзивный дескриптор подтверждены. Совместно60/60 с чтением на границах буфера и OUTPUT/APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_MODE_STATE_20261009.md. Снимок1255:132 ограниченные формы из335,203 ещё не рассмотрены. Далее те же пять форм: закрытые состояния/точные статические типы и неверные аргументы, после сверки прежних доказательств. NUM26-CAND-001 открыт, goal активен.

## Session 1256 — закрытые состояния и формы FILE

Расширены FileClosedPropertiesEvidenceTests/FilePropertyFormEvidenceTests:26 новых случаев (16 закрытых/неоткрытых и10 статических). EOF/LINENUMBER/REWIND/FLUSH на закрытом FILE дают MB3007, FINALLY подтверждает ISOPEN FALSE; байты/дескриптор сохранены. Точные Id/типы пяти функций проверены Parser/Binder: LINENUMBER LONG, остальные BOOLEAN; неверный FILE даёт SemanticFailure одинакового места. Совместно70/70 с неподходящими режимами, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_STATE_FORMS_20261009.md. Снимок1256 сохраняет132 ограниченные формы из335. Далее READTOEND/READLINE/SKIPLINE: прежние пакетные/Unicode проверки и закрытые/неверные режимы, точные формы. NUM26-CAND-001 открыт, goal активен.

## Session 1257 — формы и отказы чтения FILE

Три существующие матрицы расширены READTOEND/READLINE/SKIPLINE:30 новых случаев,12 закрытых/неоткрытых,12 неподходящих OUTPUT/APPEND и6 статических. Точные BuiltinId/типы (STRING/STRING/BOOLEAN), неверный FILE, MB3007/FINALLY/позиция/буфер/байты и освобождение дескриптора подтверждены. Совместно142/142 с прежними INPUT Unicode/буферными/пакетными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_READ_FORMS_20261009.md. Снимок1257:135 ограниченных форм из335,200 ещё не рассмотрены. Далее WRITE/WRITELINE/NEWLINE: прежние Unicode/перезапись и точные формы/неподходящие состояния. NUM26-CAND-001 открыт, goal активен.

## Session 1258 — формы и отказы записи FILE

Три матрицы расширены WRITE/WRITELINE/NEWLINE:26 новых случаев,12 закрытых/неоткрытых,6 INPUT и8 статических. Все три возвращают BOOLEAN; неверные FILE/текст отвергаются до выполнения. MB3007/FINALLY/позиция/байты/освобождение подтверждены. Совместно144/144 с LF, непарными суррогатами и OUTPUT/APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_WRITE_FORMS_20261009.md. Снимок1258:138 ограниченных форм из335,197 ещё не рассмотрены. Далее READALLLINES: прежние WholeFileDefaultEvidenceTests и границы Unicode/пустых строк/отсутствующего файла. NUM26-CAND-001 открыт, goal активен.

## Session 1259 — границы READALLLINES по буквальным байтам

ReadAllLinesBoundaryEvidenceTests:8 новых случаев обоих движков, пустой файл/LF/двойной CRLF/Unicode с NUL и одиночным CR. Выделенность/границы1 TO N, отсутствие лишней конечной строки, MB3007 отсутствующего файла с сохранением прежнего массива, байты/дескриптор подтверждены. Совместно10/10. Первый тест ошибочно использовал STR(BOOLEAN), затем ожидание одиночного CR как разделителя расходилось с ReadDecodedLines: функция делит по LF и удаляет конечный CR части. Тест фиксирует наблюдение; полный договор одиночного/конечного CR пока не установлен. Runtime не менялся. Отчёт docs/audit/READALLLINES_BOUNDARIES_20261009.md. Снимок1259:139 ограниченных форм из335. Далее установить договор одиночного/конечного CR по спецификации/READLINE и проверить явную форму/тип READALLLINES. NUM26-CAND-001 открыт, goal активен.

## Session 1260 — исправление CR в READALLLINES

Сверены историческая спецификация2 строки2179–2182 и ReadLineCore: строки не содержат переводов, потоковое чтение распознаёт CR/LF/CRLF. Подтверждён FILE26-004 P2: ReadDecodedLines сохранял внутренний CR и не разделял две пустые строки CR CR. Общий помощник заменён StringReader.ReadLine, конечный разделитель не создаёт лишнюю строку. Двенадцать новых/расширенных байтовых случаев обоих движков плюс WholeFileDefault —14/14. Пример samples/functions/readalllines/carriage_return.axm добавлен, но ещё требует запуска обоими движками; руководство/спецификацию/реестр дефектов нужно обновить до завершения исправления. Общий полный прогон после Runtime-правки также остаётся. NUM26-CAND-001 открыт, goal активен. Следующий шаг — закончить обязательную документацию и проверку примера FILE26-004, затем формы/явная кодировка READALLLINES.

## Session 1261 — документация и пример FILE26-004

Руководство READALLLINES дополнено прямым договором CR/LF/CRLF, пустых/конечных строк, отсутствующего файла и примером. carriage_return.axm автоматически запущен обоими движками в собственных каталогах, созданный файл удалён;16/16 совместно с байтовыми случаями. Факты основаны на договоре Axiom/READLINE; запасной жанровый ориентир Microsoft.VisualBasic.FileSystem.LineInput прочитан2026-10-09, книга Зельднера локально недоступна. Полная регрессия запущена: .artifacts/test-results/full-1261, процесс23642 ещё выполняется; не запускать второй MSBuild параллельно. FILE26-004 требует итог полной регрессии, затем считается исправленным в проверенном объёме. Далее точные формы/явная кодировка READALLLINES. NUM26-CAND-001 открыт, goal активен.

## Session 1262 — прерывание IDE в полной регрессии

Полный прогон1261 не успешен: TRX IDE Failed после1213 прошедших случаев, причина «Сбой хост-процесса теста», без упавшего именованного теста. Семь других проектов Completed; Execution ещё выполняется (проверен живой testhost.exe10604 дочерний vstest11856, команда Axiom.Execution.Tests), общий процесс23642 жив. Не перезапускать полный прогон/другой MSBuild параллельно. Следующий шаг — дождаться23642, сохранить итог и отдельно повторить IDE с диагностикой сбоя; FILE26-004 пока не закрыт полной регрессией. Пример16/16 уже подтверждён. NUM26-CAND-001 открыт, goal активен.

## Session 1263 — восстановление полной регрессии и открытая стабильность IDE

Общий процесс23642 завершился exit1: Execution3854/3854, IDE прерван после1213 успешных. С --blame-crash отдельный IDE повторно прерван после1180; Sequence указал два незавершённых теста (не доказательство причины). Тайловый класс с --blame-crash прерван после81; сам пример отдельно2/2. Без сборщика дампов с --diag тайловый класс82/82 и весь IDE1234/1234, exit0. Восемь Completed проектов исходного прогона плюс отдельный полный IDE дают7083/7083 проверок, девять TRX; это восстановленная регрессия, не успешная исходная общая команда. Журналы/Sequence сохранены в .artifacts/test-results/ide-*-1263.

FILE26-004 функционально исправлен: Runtime/руководство/спецификация/пример16/16 и полные наборы подтверждены. Открыт IDE26-CAND-001: непредвиденное завершение хост-процесса, причина не установлена, успешный повтор не закрывает стабильность. NUM26-CAND-001 тоже открыт. Следующий шаг — READALLLINES с явной кодировкой/точные формы; изучить журналы прерывания IDE без повторения зелёных наборов без нового основания. Goal активен.

## Session 1264 — UTF-16 и точные формы READALLLINES

ReadAllLinesEncodingFormEvidenceTests:6 новых случаев,4 исполнения UTF-16LE/BE обоих движков и2 статических точных формы. Буквальные байты с BOM/U+0D41/CR/CRLF/эмодзи/LF дают три строки, не принимают байт CR внутри символа за разделитель, сохраняют байты/дескриптор. Parser/Binder подтверждают ReadAllLines/ReadAllLinesEncoding и массив STRING. Совместно20/20 с границами/примером, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/READALLLINES_ENCODING_FORMS_20261009.md. Снимок1264:140 ограниченных форм из335,195 ещё не рассмотрены. Далее бинарные формы READBYTE/WRITEBYTE: прежние BinaryFileAlternativeEvidenceTests и числовые границы/режимы. NUM26-CAND-001 и IDE26-CAND-001 открыты, goal активен.

## Session 1265 — крайние LONG и существующие байтовые способы

Уточнение плана1264: READBYTE/WRITEBYTE не существуют в текущем языке/матрице, добавление новых сущностей не входит в аудит. Прочитаны BinaryFileAlternativeEvidenceTests/FileByteOverwriteEvidenceTests: байты сохраняются COPYFILE и однобайтовой явной WINDOWS-1251, SEEKFILE задаёт байтовую позицию и может повредить текст при перезаписи. SeekLongBoundaryEvidenceTests:4 новых случая MIN/MAX LONG обоих движков, MB3007/FINALLY/позиция2 послеА, следующий😀 до6, EOF FALSE и байты/дескриптор сохранены. Совместно32/32, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SEEK_LONG_BINARY_BOUNDARIES_20261009.md. Снимок1265 сохраняет140 ограниченных форм из335. Далее оставшиеся DIREXISTS/PATHEXISTS/MAKEDIR: прежние проверки неправильного вида объекта и сохранность Unicode-путей. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1266 — виды объектов и Unicode-каталоги

DirectoryObjectKindEvidenceTests:2 новых групповых случая обоих движков. Файл é😀 и каталог e+комбинируемый акцент😀 различаются; DIREXISTS/PATHEXISTS корректны, MAKEDIR поверх файла MB3007 сохраняет байты, вложенное создание автоматически создаёт родителей, повторный вызов успешен. Собственные каталоги .artifacts удалены по одному без рекурсивного удаления. Совместно13/13 с DirectoryAndTemp/PathNullCharacter, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_OBJECT_KINDS_20261009.md. Снимок1266:143 ограниченные формы из335,192 ещё не рассмотрены. Далее DELETEDIR/DELETEDIRRECURSIVE: ошибки непустого/неверного объекта и сохранность соседей, после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1267 — удаление каталогов и соседнее дерево

DirectoryDeleteBoundaryEvidenceTests:2 новых групповых случая обоих движков. На файле обе функции FALSE, DELETEDIR непустого дерева MB3007/FINALLY сохраняет файл; DELETEDIRRECURSIVE удаляет только собственное дерево, повтор/отсутствующий путь FALSE, канонически другой соседний Unicode-каталог сохраняет точные байты. Перед рекурсивным вызовом абсолютный target проверен внутри уникального root .artifacts. Очистка по одному файлу/каталогу. Совместно16/16 с FileRenameDeleteEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_DELETE_BOUNDARIES_20261009.md. Снимок1267:145 ограниченных форм из335,190 ещё не рассмотрены. Далее COPYDIR/MOVEDIR: прежние DirectoryTraversalFailureEvidenceTests, вложенность/неверный вид/сохранность. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1268 — COPYDIR/MOVEDIR и неправильный вид

Уточнены имена прежних доказательств: DirectoryFailureEvidenceTests и DirectoryTraversalContractTests (DirectoryTraversalFailureEvidenceTests отсутствует). DirectoryTransferKindEvidenceTests:12 новых случаев обоих движков, файл вместо источника/назначения, COPYDIR обычный/overwrite TRUE и MOVEDIR. MB3007/FINALLY сохраняют оба точных содержимых и число объектов на разных Unicode-путях. Совместно38/38 с вложенностью/частичными конфликтами/ссылками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_TRANSFER_KINDS_20261009.md. Снимок1268:148 ограниченных форм из335,187 ещё не рассмотрены. Далее статические формы каталогов/неверные аргументы, затем оставшиеся DIRSIZE/CURRENTDIR/CHANGEDIR. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1269 — точные формы каталогов

DirectoryFormEvidenceTests:18 новых случаев,8 точных BuiltinId/BOOLEAN через Parser/Binder и10 неверных аргументов с SemanticFailure/совпадающими местами обоих режимов. Покрыты DIREXISTS/PATHEXISTS/MAKEDIR/DELETEDIR/DELETEDIRRECURSIVE/COPYDIR две формы/MOVEDIR. Совместно34/34 с тремя новыми матрицами исполнения каталогов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_FORMS_20261009.md. Снимок1269 сохраняет148 ограниченных форм из335. Далее DIRSIZE/CURRENTDIR/CHANGEDIR: прежние обходы/локальный текущий каталог и неверные объекты. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1270 — байтовый DIRSIZE и локальный текущий каталог

DirectorySizeCurrentEvidenceTests:2 новых групповых случая обоих движков, вложенные файлы4+3 байта дают DIRSIZE7/3/7; CHANGEDIR относительный/родитель работает, попытка смены на файл MB3007 сохраняет каталог программы, DIRSIZE файла MB3007. Environment.CurrentDirectory процесса неизменён, оба содержимых сохранены. Совместно14/14 с DirectoryAndTemp/DirectoryTraversalContract, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_SIZE_CURRENT_20261009.md. Снимок1270:151 ограниченная форма из335,184 ещё не рассмотрены. Далее точные формы DIRSIZE/CURRENTDIR/CHANGEDIR и оставшиеся дисковые функции/пути. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1271 — типы размера и текущего каталога

DirectoryFormEvidenceTests дополнен5 случаями: точные DirSize LONG/CurrentDir STRING/ChangeDir BOOLEAN и неверные BOOLEAN-пути с SemanticFailure одинакового места обоих режимов. Совместно25/25 с исполнением размера/смены каталога, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Прочитан DriveInfoEvidenceTests: независимый Win32_LogicalDisk для размера/формата/типа, свободное место проверяется интервалом; ещё не запущен в этом шаге. Отчёт docs/audit/DIRECTORY_SIZE_FORMS_20261009.md. Снимок1271 сохраняет151 ограниченную форму из335. Далее шесть дисковых функций: независимый текущий реестр, точные типы/неверные аргументы и отсутствие созданного пути. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1272 — дисковые формы и независимый реестр Windows

DriveFormEvidenceTests:6 новых групповых случаев, точные BuiltinId/типы (размеры LONG, имя/формат/тип STRING) и BOOLEAN вместо пути отвергается SemanticFailure одинакового места обоих режимов. DriveInfoEvidenceTests реально выполнен на Windows: размер/формат/тип сверены с Win32_LogicalDisk, свободное место в0..Size, путь не создаётся. Совместно8/8, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DRIVE_FORMS_INVENTORY_20261009.md. Снимок1272:157 ограниченных форм из335,178 ещё не рассмотрены. Далее PATHROOT/COMBINEPATH/CHANGEPATHEXTENSION/RELATIVEPATH: прочитать прежние независимые пути и выбрать реальные пробелы Windows/Unicode. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1273 — SUBSTRCOUNT и неперекрывающийся Unicode

Уточнение плана1272: CHANGEPATHEXTENSION/RELATIVEPATH отсутствуют в текущем языке, существующие формы путей уже имеют ограниченные доказательства. SubStrCountUnicodeEvidenceTests:2 новые групповые проверки обоих движков, неперекрывающиеся UTF-16 совпадения, отдельные суррогаты/NUL, отсутствие нормализации и пустой фрагмент MB3007 с FINALLY. Совместно59/59 с StringSearchIndependentContractTests/StringSearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SUBSTRCOUNT_UNICODE_20261009.md. Снимок1273:158 ограниченных форм из335,177 ещё не рассмотрены. Далее реальные ARRAYCOUNT/ARRAYINDEXOF/ARRAYLASTINDEXOF/ARRAYCONTAINS: прежние доказательства и сравнение строк Unicode/чисел. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1274 — поиск Unicode и точных чисел в массивах

ArraySearchUnicodeNumericEvidenceTests:4 новые групповые проверки обоих движков. Целые UTF-16 значения отличают NUL/отдельный суррогат/эмодзи/составные акценты; видимый срез исключает внешнее совпадение, источник сохраняется. Соседние LONG выше2^53 и DECIMAL у максимума различаются в ARRAYCOUNT/ARRAYCONTAINS/ARRAYINDEXOF/ARRAYLASTINDEXOF. Первые три запуска числовой части отклонены из-за ошибочных форм теста (суффиксы L/D и строка в CLNG/CDEC); исправлены на подтверждённые литералы LONG и контекстные DECIMAL .0. Итог39/39 с ArraySearchContractTests/ArraySearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SEARCH_UNICODE_NUMERIC_20261009.md. Снимок1274:162 ограниченные формы из335,173 ещё не рассмотрены. Далее ARRAYFINDALL/ARRAYBINARYSEARCH: независимые координаты и Unicode/точные числа, после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

Уточнение счётчика по фактическому CSV1274:161 форма SCOPED_EVIDENCE и174 NOT_REVIEWED_IN_RESUMED_PASS, всего335. Числа162/173 выше ошибочны; предыдущий CSV1273 содержит157/178, а не158/177. При дальнейшем учёте использовать группировку CSV, а не перенос чисел из текста.

## Session 1275 — координаты и двоичный поиск Unicode

ArrayFindBinaryUnicodeEvidenceTests:4 новых групповых случая обоих движков. ARRAYBINARYSEARCH две формы на заранее упорядоченных UTF-16 строках с NUL/суррогатом/эмодзи/U+E000 возвращает буквальные логические индексы; соседние LONG выше2^53 различаются, первый дубликат найден, отсутствие даёт нижнюю границу минус1. ARRAYFINDALL возвращает координаты только видимого среза и целых строк, без нормализации/изменения источника. Совместно39/39 с ArraySearchContractTests/ArraySearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_FIND_BINARY_UNICODE_20261009.md. Снимок1275:164 ограниченных форм из335,171 ещё не рассмотрены (группировка фактического CSV). Далее ARRAYDISTINCT: прежние доказательства порядка/границ и Unicode/точных чисел. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1276 — уникальные значения Unicode в видимом срезе

ArrayDistinctUnicodeViewEvidenceTests:2 новые групповые проверки обоих движков. ARRAYDISTINCT видимого двумерного среза сохраняет порядок первых целых UTF-16 значений (суррогаты/NUL/эмодзи/пустая строка), исключает внешний элемент; результат1..5 независим от источника. Соседние DECIMAL у максимума различаются. Совместно27/27 с ArrayDistinctValueContractTests/ArrayCallbackOrderContractTests, ошибок/пропусков0. Первоначальные2 отказа были ошибкой ожидания форматирования DECIMAL: максимальные значения выводятся без .0; исправлено ожидание, не поведение. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_DISTINCT_UNICODE_VIEW_20261009.md. Снимок1276:165 ограниченных форм из335,170 ещё не рассмотрены. Далее ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE: прочитать прежние независимые границы/оси и выбрать пробел Unicode. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1277 — сортировка и обратный порядок Unicode

ArraySortReverseUnicodeEvidenceTests:4 новых групповых случая обоих движков, четыре функции с пропущенной/явной осью. Сортировка целых UTF-16 значений различает акценты/суррогат/эмодзи/U+E000; ARRAYTOSORTED сохраняет границы -2..2, ARRAYTOREVERSED даёт1..5. Копии сохраняют источник; ARRAYSORT/ARRAYREVERSE через срез меняют только видимые элементы. Первые два прогона имели ошибочные ожидания границ теста, исправлены по коду и прежнему договору. Итог119/119 с ArrayFillSortReverseContractTests/ArraySortBoundaryContractTests/ArrayTransformAxisContractTests/ArrayDefaultFormEvidenceTests/ArrayRemainingFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SORT_REVERSE_UNICODE_20261009.md. Снимок1277:173 ограниченных форм из335,162 ещё не рассмотрены. Далее ARRAYFILL и ARRAYCLONE: прежние типы/границы и сохранность Unicode/независимость копии. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1278 — заполнение и независимая копия Unicode

ArrayFillCloneUnicodeEvidenceTests:2 новые групповые проверки обоих движков. ARRAYFILL записывает суррогаты, разделённые NUL, только в видимый двумерный срез, сохраняет соседей. ARRAYCLONE сохраняет границы среза и создаёт независимую копию; заполнение копии не меняет владельца. Копия нераспределённого массива остаётся нераспределённой; ARRAYFILL такого источника MB3005 сохраняет состояние.31/31 с ArrayFillSortReverseContractTests/ArrayRemainingFormEvidenceTests; отдельно57/57 ArrayWindowFormEvidenceTests для точных форм/типов/неверных аргументов, суммарно88/88, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_FILL_CLONE_UNICODE_20261009.md. Снимок1278:175 ограниченных форм из335,160 ещё не рассмотрены. Далее метаданные LBOUND/UBOUND/ARRAYBOUNDS/ARRAYRANK/ARRAYLENGTH/ARRAYISALLOCATED: пустые и нераспределённые массивы, крайние индексы, точные результаты после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1279 — крайние границы и состояние массивов

ArrayMetadataBoundaryEvidenceTests:2 новые групповые проверки обоих движков. LBOUND/UBOUND обе формы точно возвращают крайние индексы -2147483647..-2147483646 и2147483646..2147483647. ARRAYBOUNDS создаёт независимый массив, изменение границы результата не меняет источник. Пустой выделенный массив сохраняет ранг2 и границы, длина0; нераспределённый даёт FALSE/0/2, ARRAYBOUNDS MB3005 сохраняет прежний результат. Совместно59/59 с ArrayWindowFormEvidenceTests для точных форм/типов/неверных аргументов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_METADATA_BOUNDARIES_20261009.md. Снимок1279:183 ограниченных форм из335,152 ещё не рассмотрены. Далее ARRAYSLICE/ARRAYTAKE/ARRAYSKIP: прежние доказательства и крайние count/start/Unicode/сохранность исходных элементов. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1280 — крайние размеры окон Unicode-массивов

ArrayWindowUnicodeCountEvidenceTests:4 новых групповых случая обоих движков с пропущенной/явной осью. ARRAYTAKE с максимальным count копирует только3 Unicode-элемента и не меняет источник; ARRAYSKIP даёт выделенный пустой1..0; ARRAYSLICE последнего INTEGER-индекса успешен, огромный диапазон MB3007 сохраняет прежний результат/источник. Совместно65/65 с ArrayWindowFormEvidenceTests/ArrayEmptyBoundaryContractTests (точные формы/типы/неверные аргументы и многомерные крайние пустые границы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_WINDOW_UNICODE_COUNTS_20261009.md. Снимок1280:189 ограниченных форм из335,146 ещё не рассмотрены. Далее ARRAYFLATTEN/ARRAYRESHAPE: видимые Unicode-элементы, порядок и независимость формы, после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1281 — форма и порядок Unicode-массивов

ArrayShapeUnicodeEvidenceTests:2 новые групповые проверки обоих движков. ARRAYFLATTEN читает только видимый срез в порядке последнего измерения; сохраняет суррогат с NUL/составной акцент/эмодзи/предварительно составленный символ, результат1..4. ARRAYRESHAPE использует границы видимого шаблона9..10/-2..-1, независим от источника и шаблона. Несовпадение4/8 элементов MB3007 сохраняет прежний результат. Совместно51/51 с ArrayShapeContractTests/ArrayRemainingFormEvidenceTests, включая точные формы/типы/пустые и нераспределённые случаи, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SHAPE_UNICODE_20261009.md. Снимок1281:191 ограниченных форм из335,144 ещё не рассмотрены. Далее ARRAYCONCAT: прежние границы/совместимость, Unicode и точные числа. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1282 — объединение видимых Unicode-массивов

ArrayConcatUnicodeViewEvidenceTests:2 новые групповые проверки обоих движков. ARRAYCONCAT две формы объединяет целые Unicode-значения видимых срезов без внешних элементов; одномерный результат1..4, двумерный0..1/6..9. Изменение результата не меняет источник и повторно скопированный элемент. Совместно129/129 с ArrayConcatBoundaryContractTests/ArrayShapeContractTests/ArrayRemainingFormEvidenceTests/ArrayTransformAxisContractTests (точные формы/типы, оси, INTEGER-пределы/переполнение, пустые/нераспределённые/несовместимые аргументы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CONCAT_UNICODE_20261009.md. Снимок1282:193 ограниченных форм из335,142 ещё не рассмотрены. Далее ARRAYPUSH/ARRAYPOP/ARRAYINSERT/ARRAYREMOVE: прежние границы/пустые формы, сохранность Unicode при изменении/возврате. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1283 — добавление и удаление Unicode-элементов

ArrayMutationUnicodeEvidenceTests:4 новые групповые проверки обоих движков с пропущенной/явной осью. ARRAYPUSH/ARRAYINSERT сохраняют целые Unicode-значения и сдвигают элементы; ARRAYREMOVE/ARRAYPOP возвращают точные строки, восстанавливают исходные границы/значения. Неверный крайний индекс MB3007 сохраняет массив.113/113 с ArrayEmptyInsertContractTests/ArrayDefaultFormEvidenceTests/ArrayRemainingFormEvidenceTests/ArrayTransformAxisContractTests; отдельно22/22 ArrayDynamicShapeContractTests для многомерных осей/отказов/крайних границ, суммарно135/135, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_MUTATION_UNICODE_20261009.md. Снимок1283:201 ограниченных форм из335,134 ещё не рассмотрены. Далее ARRAYEVERY/ARRAYSOME/ARRAYFILTER/ARRAYMAP/ARRAYREDUCE: прежние порядок/пустые случаи и Unicode/точные числа в пользовательских функциях. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1284 — Unicode в пользовательских функциях обхода

ArrayCallbackUnicodeEvidenceTests:2 новые групповые проверки обоих движков. Целые строки с суррогатом/NUL/разными акцентами/эмодзи передаются пользовательским функциям только из видимого среза. ARRAYEVERY/ARRAYSOME вызывают предикат2/1 раз, ARRAYFILTER/ARRAYMAP/ARRAYREDUCE4 раза; результат и строка накопления точны, источники независимы.23/23 с ArrayCallbackOrderContractTests (точные формы/типы, подписи, порядок/ошибки/FINALLY); отдельно12/12 ArrayCallbackBoundaryContractTests (пустые массивы/максимальный индекс), суммарно35/35, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CALLBACK_UNICODE_20261009.md. Снимок1284:206 ограниченных форм из335,129 ещё не рассмотрены. Далее числовые пользовательские функции LONG/DECIMAL для ARRAYMAP/ARRAYREDUCE, затем оставшиеся реальные формы из CSV. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1285 — точные числа в пользовательских функциях массивов

ArrayCallbackExactNumericEvidenceTests:2 новые групповые проверки обоих движков. ARRAYMAP передаёт соседние LONG выше2^53 и соседние DECIMAL у максимума точно; ARRAYREDUCE даёт точный LONG18014398509481985 и DECIMAL-1. Переполнение первого вызова пользовательской функции MB3008 прекращает обход после1 вызова, сохраняет прежний результат/источник. Совместно37/37 с Unicode/порядком/границами пользовательских функций, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CALLBACK_EXACT_NUMERIC_20261009.md. Снимок1285 сохраняет206 ограниченных форм из335,129 ещё не рассмотрены. Далее реальные REGEXCOUNT/REGEXMATCH и связанные формы: прежние независимые доказательства, Unicode UTF-16 и недопустимые шаблоны. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1286 — REGEXCOUNT и ожидание результата

Добавлены2 групповых случая RegexCountMalformedUnicodeEvidenceTests обоих движков: UTF-16/NUL/суррогаты/пустой шаблон/отсутствие нормализации/неперекрывающиеся совпадения. Запущен совместный прогон RegexIndependentContractTests/RegexDiagnosticParityProbeTests, включая реальные пятисекундные тайм-ауты всех18 форм. Процесс17948 и testhost13260 подтверждены живыми, итог ещё ожидается; повторный dotnet не запускать. Отчёт docs/audit/REGEX_COUNT_MALFORMED_UNICODE_20261009.md пока промежуточный. Статус CSV1285 не меняется:206 ограниченных форм/129 не рассмотрены. Следующий шаг: дождаться именно процесса17948 и записать итог, затем точные формы REGEXCOUNT и REGEXISMATCH/REGEXMATCH. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1287 — подтверждённый результат REGEXCOUNT

Завершён именно процесс17948:201/201 за3 минуты, exit0, ошибок/пропусков0; реальные тайм-ауты всех18 форм и диагностическая сверка прошли, включая новые2 Unicode-случая REGEXCOUNT. RegexCountFormEvidenceTests добавляет2 статических случая точных RegexCount/RegexCountModes INTEGER; отдельный прогон4/4 с Unicode, без повторения трёхминутной группы. Итоговый отчёт дополнен docs/audit/REGEX_COUNT_MALFORMED_UNICODE_20261009.md. Дефект не найден, Runtime/поведение не менялись. Снимок1287:208 ограниченных форм из335,127 ещё не рассмотрены. Далее REGEXISMATCH/REGEXMATCH: точные формы и возврат отдельных UTF-16 совпадений/NUL/режимы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1288 — точные формы и совпадения Unicode

RegexMatchUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/BOOLEAN/STRING через Parser/Binder и2 исполнения обоих движков). REGEXMATCH возвращает точный суррогат/NUL, REGEXISMATCH не исправляет разорванную пару и не нормализует акценты, включая режимы. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0. Прежние обычные результаты/неверные аргументы/пустые/режимы/реальные тайм-ауты подтверждены прогоном201/201 Session1287 и без изменения Runtime не повторены. Дефект не найден, поведение не менялось. Отчёт docs/audit/REGEX_MATCH_UNICODE_20261009.md. Снимок1288:212 ограниченных форм из335,123 ещё не рассмотрены. Далее REGEXEXTRACT/REGEXEXTRACTALL: точные формы/группы и целые UTF-16 значения. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1289 — извлечение Unicode-групп

RegexExtractUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/STRING/одномерный STRING-массив и2 исполнения обоих движков). Группы возвращают точные суррогаты/NUL; явная группа10 возвращает целую пару, отсутствующая1 пуста; REGEXEXTRACTALL сохраняет порядок и создаёт выделенный пустой результат без нормализации акцента. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты уже подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_EXTRACT_UNICODE_20261009.md. Снимок1289:216 ограниченных форм из335,119 ещё не рассмотрены. Далее REGEXMATCHES/REGEXSPLIT: точные массивные формы, нулевые совпадения и UTF-16 границы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1290 — массивы совпадений и UTF-16-разделители

RegexArrayUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/одномерный STRING-массив и2 исполнения обоих движков). REGEXMATCHES возвращает суррогаты/NUL и3 нулевых совпадения эмодзи; REGEXSPLIT пустым шаблоном делит между UTF-16 единицами, захваченные разделители сохраняются, включая NUL/суррогаты. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_ARRAY_UNICODE_20261009.md. Снимок1290:220 ограниченных форм из335,115 ещё не рассмотрены. Далее четыре формы REGEXREPLACE: точное связывание, Unicode/замены/числовой count. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1291 — замены Unicode и крайние count

RegexReplaceUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/STRING и2 исполнения обоих движков). Четыре формы сохраняют отдельные UTF-16 единицы/NUL, ссылки на группы, count1/MAX и нулевые совпадения; замена не нормализует акценты, count0 сохраняет строку без проверки шаблона/режимов по прежнему договору. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_REPLACE_UNICODE_20261009.md. Снимок1291:224 ограниченных форм из335,111 ещё не рассмотрены. Все18 REGEX-форм теперь имеют ограниченные доказательства, не полное закрытие. Далее даты/числа DATESERIAL/TIMESERIAL/ISLEAPYEAR/DAYSINMONTH: прежние независимые календарные проверки и границы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1292 — крайние аргументы календарных функций

CalendarExtremeArgumentEvidenceTests:36 новых случаев (крайние MIN/MAX INTEGER каждого9 аргументных положений четырёх функций, оба движка). DATESERIAL/TIMESERIAL/ISLEAPYEAR/DAYSINMONTH дают MB3007 и выполняют FINALLY без нормализации недопустимых календарных чисел. Совместно172/172 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests (точные формы/типы, григорианские правила1900/2000/2100 и допустимые пределы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CALENDAR_EXTREME_ARGUMENTS_20261009.md. Снимок1292:228 ограниченных форм из335,107 ещё не рассмотрены. Далее DATEADD/DATEDIFF/DATEPART три типа: прочитать прежние доказательства и выбрать числовой пробел полных диапазонов/интервалов. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1293 — полный диапазон арифметики дат

DateArithmeticRangeEvidenceTests:2 новые групповые проверки обоих движков. DATEDIFF полного DATETIME-диапазона даёт точные LONG±315537897599 секунд, DATE3652058 дней, yyyy9998; DATEPART крайних даты/времени точен. DATEADD крайних INTEGER в DATE даёт MB3007 и сохраняет результат; TIME оборачивается по суткам в03:14:07/20:45:52. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для9 точных форм/типов и прежних границ, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DATE_ARITHMETIC_FULL_RANGE_20261009.md. Снимок1293:237 ограниченных форм из335,98 ещё не рассмотрены. Далее DATEVALUE/TIMEVALUE/ISDATE: Unicode/встроенный NUL/неверные форматы и сохранность результата, после чтения прежнего разбора. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1294 — Unicode в строках даты и времени

TemporalTextUnicodeEvidenceTests:14 новых случаев обоих движков (7 строк). DATEVALUE/TIMEVALUE отвергают NUL с последующим текстом, эмодзи/суррогат, полноширинные/арабские цифры и Unicode-минус с MB3007, сохраняя прежние DATE/TIME; ISDATE FALSE. Совместно150/150 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для точных форм/типов и канонических форматов, ошибок/пропусков0. Дефект не найден в проверенном объёме, Runtime/поведение не менялись. Отчёт docs/audit/TEMPORAL_TEXT_UNICODE_20261009.md. Снимок1294:240 ограниченных форм из335,95 ещё не рассмотрены. Далее отдельный конечный NUL/пробелы и договор точного разбора перед оставшимися частями даты. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1295 — конечный NUL и пробелы временных строк

TemporalTextUnicodeEvidenceTests дополнен14 случаями: конечный NUL после DATE/TIME/DATETIME, начальный/конечный ASCII-пробел и NBSP. Все отвергаются ISDATE FALSE и DATEVALUE/TIMEVALUE MB3007 с сохранением прежних значений обоих движков.28/28, ошибок/пропусков0; новый дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/TEMPORAL_TEXT_TAILS_20261009.md. Снимок1295 сохраняет240 ограниченных форм из335,95 ещё не рассмотрены. Далее YEAR/MONTH/DAY/HOUR/MINUTE/SECOND/WEEKDAY/MONTHNAME/WEEKDAYNAME и STARTOF/ENDOF: реальные границы и канонические имена после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1296 — части даты и крайние периоды

CalendarPartsBoundaryEvidenceTests:2 новые групповые проверки обоих движков при tr-TR с восстановлением культуры. Части DATE/TIME точны на крайних значениях, WEEKDAY0001-01-01=1 и9999-12-31=5; имена January/December/Monday/Sunday канонические, недопустимые крайние числа MB3007. STARTOF/ENDOF крайних месяцев/лет не выходят за диапазон. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для13 точных форм/типов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CALENDAR_PARTS_BOUNDARIES_20261009.md. Снимок1296:253 ограниченных форм из335,82 ещё не рассмотрены. Далее DATE/TIME/NOW/UTC/TIMER: фиксированный независимый источник времени, границы суток и явные зоны после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1297 — фиксированная полночь и TIMER

CurrentTimeMidnightEvidenceTests:2 новые групповые проверки обоих движков на фиксированном2000-03-01 00:00:00.125 +03:00. Локальные DATE/TIME/NOW и UTC-варианты/явнаяUTC различают соседние даты, строковое время до секунд, TIMER0.125 точно. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для10 точных форм/типов, ошибок/пропусков0. Первый build отклонён из-за неполной реализации IRuntimeClock в новом тесте, исправлен только тест. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CURRENT_TIME_MIDNIGHT_20261009.md. Снимок1297:263 ограниченных форм из335,72 ещё не рассмотрены. Далее UTCOFFSET/TOUTC/FROMUTC: точные формы, явные зоны/крайний DATETIME и Unicode-идентификаторы после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1298 — часовые пояса у пределов DATETIME

TimeZoneUnicodeBoundaryEvidenceTests:2 новые групповые проверки обоих движков с ControlledClock+03/UTC. UTCOFFSET точен, UTC сохраняет крайние DATETIME; TOUTC минимального/FROMUTC максимального в+03 дают MB3007 и сохраняют результат. Неверные Unicode/NUL имена зон отвергнуты контролируемым resolver, не выдаётся за проверку системного resolver. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для7 точных форм/типов и прежних реальных DST случаев America/New_York, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/TIME_ZONE_UNICODE_BOUNDARIES_20261009.md. Снимок1298:270 ограниченных форм из335,65 ещё не рассмотрены. Далее системный resolver Unicode/NUL зон и оставшиеся экранные формы из CSV. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1299 — системные зоны Unicode и точка остановки

SystemTimeZoneUnicodeEvidenceTests:24 новых случая обоих движков,4 неверных имени зон (конечный NUL/эмодзи/одиночный суррогат/составной акцент) для UTCOFFSET/TOUTC/FROMUTC через реальный системный resolver Windows без подмены часов. MB3007/FINALLY сохраняют DATETIME. Совместно26/26 с TimeZoneUnicodeBoundaryEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SYSTEM_TIME_ZONE_UNICODE_20261009.md. Снимок1299:270 ограниченных форм из335,65 ещё не рассмотрены. По просьбе пользователя аудит останавливается после обновления документов, коммита и push; goal будет приостановлен. Следующий шаг нового запуска: INKEY/KEYDOWN/KEYPRESS и мышь/экранные формы после чтения прежних независимых доказательств. NUM26-CAND-001/IDE26-CAND-001 остаются открытыми.


## Session 1301 — клавиатурные формы

INKEY/KEYDOWN/KEYPRESS получили ограниченные доказательства Unicode/точных форм и отказов связывания:20/20. Снимок1301:273/335,62 не рассмотрены. Далее MOUSEX/MOUSEY/MOUSEBUTTON/MOUSEPRESS и состояние экрана; после оставшихся форм сверка исходной матрицы, статусы NUM26-CAND-001/IDE26-CAND-001 и итоговая регрессия. Goal активен.


## Session 1302 — мышь и состояние экрана

Восемь форм получили ограниченные доказательства,19/19. Снимок1302:281/335,54 не рассмотрены. Далее TEXTWIDTH/TEXTHEIGHT и POINT/PMAP:UTF-16 метрики,крайние селекторы и координаты после чтения прежних доказательств. Goal активен.


## Session 1303 — графические запросы

Пять форм получили ограниченные доказательства:19/19 и прежний Runtime12/12. Снимок1303:286/335,49 не рассмотрены. Далее LEN/LEFT/RIGHT/MID/CONTAINS/STARTSWITH/ENDSWITH и RGB; затем сверить ресурсы/тайлы с уже завершёнными доказательствами без повторного тайлового аудита. Goal активен.


## Session 1304 — базовые строки и RGB

Восемь форм имеют ограниченные доказательства,93/93. Снимок1304:294/335,41 не рассмотрена. Далее сопоставить каждую ресурсную/тайловую форму с завершёнными независимыми доказательствами; затем решить статусы двух кандидатов и итоговую регрессию. Goal активен.


## Session 1305 — поштучная сверка завершена

Все335 форм имеют ограниченные доказательства;41 ресурсная/тайловая форма сверена отдельно,335 ключей совпадают с исходной матрицей. Далее разрешить статусы NUM26-CAND-001/IDE26-CAND-001, получить итог текущего полного прогона1306 и проверить остальные требования AUDIT_HANDOFF. Goal активен, общий аудит ещё не завершён.


## Session 1307 — уточнение объёма по указанию пользователя

Полная регрессия1306:7345/7345. Дополнительное расследование IDE26-CAND-001 прекращено по прямому указанию пользователя; кандидат OPEN_DEFERRED_BY_USER до естественного воспроизведения. Не запускать новые проверки именно ради этой аварии. Остались документирование фактического предела POW, исправление подтверждённого склеивания PRINT в CLI и итоговая проверка после него.

## Session 1310 — согласованный проход завершён

Оставшиеся 65 форм рассмотрены или поштучно сопоставлены с прежними доказательствами: всего 335 SCOPED_EVIDENCE. Текущий реестр/матрица/метаданные/ссылки сверены. CLI26-001 исправлен с тестами, руководством и русским примером. NUM26-CAND-001 классифицирован как известное ограничение Math.Pow без исправления точного округления. IDE26-CAND-001 отложен пользователем до естественного повторения; новых проверок ради аварии не запускать.

Итог после изменения CLI: исходная общая команда exit1, одна ошибка ожидания IDE; отдельный полный IDE exit0. Восстановленная регрессия 7353/7353, Electron metadata 3/3, сводка AUDIT_VALIDATION_20261009_1310.json. Одиночная нестабильность ожидания не объявлена исправленной. Согласованная цель завершена, универсальное закрытие всех входов не заявляется.

При следующей задаче выбрать отдельный договор: строгую точность POW, точный разбор DECIMAL либо иной конкретный пробел исходной матрицы. Нестабильное ожидание фоновой компиляции оставить отдельным вопросом инфраструктуры. Архитектура, имена и схемы подсветки не менялись. Коммит/push требуют отдельного поручения.

## Session 1311 — принятый договор POW

По прямому решению пользователя POW использует готовую System.Math.Pow, сохраняя ошибки Axiom. Конечный результат должен совпадать с .NET в той же среде, включая округление и знак нуля. NaN преобразуется в MB3007, бесконечность — в MB3008. Идентичность особых возвращаемых значений .NET не заявляется: диагностическая политика Axiom сохранена намеренно.

NUM26-CAND-001 закрыт как CLOSED_ACCEPTED_DOTNET_CONTRACT. Расхождение POW(2,-1075) с математическим округлением середины не устраняется специальной поправкой; принята точность готовой функции .NET. Исходное наблюдение остаётся в истории. Вычислительный код обоих движков уже соответствует решению и не менялся. PowDotNetContractTests сравнивает конечные результаты побитово с Math.Pow текущей среды и отдельно проверяет ошибки, FINALLY и продолжение.

## Session 1312 — будущие направления: один исполнитель и новая оболочка IDE

Пользователь попросил зафиксировать намерение позднее составить подробный план отказа от Interpreter в пользу компилятора байткода и BytecodeVm. Удаление кода сейчас не разрешено; сначала требуется отдельный план переноса уникальных проверок и сохранения возможностей.

Уточнена цель будущей IDE: сохранить возможности текущей WPF-IDE Axiom, изменив внешний вид и оболочку по визуальному ориентиру C:\X__sharp\ide. Перенос IDE X# целиком не является целевой задачей. Существующий ide/ уже входит в master; в local/wip-block-scope-for-counter есть прежняя версия. Старый ELECTRON_IDE_PLAN.md предусматривал отложенную графику и отладчик, поэтому его прежние ограничения не определяют окончательную полноту новой оболочки.

До реализации составить перечень возможностей текущей IDE и соответствий новой оболочке. Особое внимание: отладка BytecodeVm с точками останова и шагом назад; графический хост SCREEN/CANVAS и ввод; сохранность документов/настроек, диагностика, форматирование и справочник. Ядро языка должно оставаться на C#/.NET; способ связи Electron с исполнением и способ переноса графики выбрать в отдельном плане. Существующие ide/CLI — заготовка, не подтверждённая полная замена WPF. В этой сессии код не менялся.

## Session 1313 — подробный план отказа от интерпретатора

По запросу пользователя создан docs/plans/INTERPRETER_REMOVAL_PLAN.md («Отказ от интерпретатора»). Только план, реализация не начата. Конечная архитектура: компилятор байткода и BytecodeVm, без исполнителя дерева и выбора режимов. Новая Electron-оболочка и сборка native exe не входят в эту задачу.

Приложения: INTERPRETER_REMOVAL_DEPENDENCIES_20261009.csv (235 файлов поиска зависимостей), INTERPRETER_REMOVAL_TEST_TRANSFER_20261009.csv (597 объявлений Fact/Theory, одно выключено #if false). Документ определяет порядок переноса проверок, честный полный успех компиляции, миграцию состояния IDE, удаление API/исполнителя/старого окружения, переработку инструментов и итоговую UI-регрессию. Перед удалением общих частей обязательна проверка вызывающих. Следующий шаг — после отдельного поручения пользователя начать этап 0 по документу; сейчас код не менять.

Уточнение предыдущего обзора IDE: существующий Electron ide/ является незавершённым прототипом, редактор и консольный запуск не считаются готовыми без проверки. В будущей замене сохранить возможности WPF Axiom; X# — визуальный ориентир.

## Session 1314 - 2026-10-09 21:17 - Этап 0 отказа от интерпретатора (нативно)

ZCode-исполнитель не запустился (`Model request failed`, без побочных эффектов), поэтому этап 0 плана `INTERPRETER_REMOVAL_PLAN.md` выполнен нативно: снимок git, списки тестов девяти проектов, полная регрессия 7387/7387 (exit 0, 9 TRX), node-тест IDE и inventory-снимок — всё в `.artifacts/interpreter-removal/`; создан `docs/plans/INTERPRETER_REMOVAL_PROGRESS.csv` (597 строк, все `НЕ_ПЕРЕНЕСЕНО`). Уточнения таблицы переноса (GraphicsBaselineTests вне 597, хелперы в TryCatch) — в `STAGE0_NOTES.md` и WORKLOG Session 1314.

Следующий шаг: этап 1 — перенос проверок Runtime на путь BytecodeVm. Нужен рабочий исполнитель (повтор ZCode) либо решение пользователя о нативном продолжении. Ручная проверка §15 WPF-IDE — за человеком; коммит/push не выполнялись.

## Session 1315 - 2026-10-09 21:27 - Этап 1 отказа от интерпретатора: VM-раннер и первые переносы

ZCode снова недоступен (вторая одинаковая ошибка запуска) — продолжено нативно. Созданы `RuntimeVmContractRunner` и общий `TestRuntimeHost` (извлечён из TryCatch.cs), `LanguageContractTests.Helpers.cs`; перенесены первые две проверки (арифметика; FINALLY при необработанном отказе) с буквальными ожиданиями, старые версии удалены после успеха. Адресный прогон 2/2; полный Runtime.Tests 1079/1079, exit 0. Далее: перенос остальных тематических файлов; PROGRESS.csv и STAGE1_NOTES обновлены. Коммит/push не выполнялись.

## Session 1316 - 2026-10-09 21:49 - Этап 1 отказа от интерпретатора: Error/Print/Wave4/Type и фикс AXD-020a

Перенесены четыре малых файла проверок Runtime на `LanguageContractTests` (83 объявления), старые удалены после зелёных прогонов; адресный 86/86, Runtime.Tests 1079/1079, полное решение — 7387/7387; 9 TRX; exit 0; 21:42:51-21:48:08. Исправлен AXD-020a: VM больше не отклоняет функцию без RETURN на валидации модуля — ошибка приходит в рантайме MB3006 (единый текст с Interpreter); обновлён VM-тест, добавлен пример `samples/acceptance/negative/function_branch_without_return_value.axm`. PROGRESS.csv: перенесено 85, осталось 512. Далее: следующие тематические файлы (InterpreterTests.cs 83, String 90, Graphics 100, Array 126, DateTime 37, FileIO 28, FileSystem 29, остатки TryCatch 6, StringNonBmp 6, FileSystemDiagnostics 6 и GraphicsBaselineTests 9 вне таблицы) с переносом недостающих хелперов. Коммит/push не выполнялись.

## Session 1317 - 2026-10-09 21:55 - Этап 1: пять файлов и остаток TryCatch (116 проверок)

Перенесены `StringNonBmp` (6), `FileSystemDiagnostics` (6), `FileIO` (28), `FileSystem` (29, один выключенный), `DateTime` (37) и остаток `TryCatch` (8); 10 хелперов — в `LanguageContractTests.Helpers.cs`. Адресный 202/202, полный Runtime.Tests 1079/1079, exit 0. PROGRESS.csv: осталось 398 (198 перенесено + 1 отключённый). Следующие: `InterpreterTests.cs` (82), `String` (90), `Graphics` (100), `Array` (126). Коммит/push не выполнялись.

## Session 1318 - 2026-10-09 22:11 - Этап 1 отказа от интерпретатора завершён (Runtime)

Перенесены последние четыре файла (82+90+100+126) и внеплановый GraphicsBaselineTests (9); старый класс `InterpreterTests` удалён полностью. Исправлены AXD-021 (тексты MB3005 с именем функции/аргумента) и новый AXD-022 (`ARRAYPUSH` overflow → MB3007 вместо CLR-исключения). Адресный 681/681; полный Runtime.Tests 1079/1079; полное решение 7387/7387. PROGRESS.csv: 597/597 (596 + 1 отключённая). Далее: этап 2 — сквозные проверки Execution и следующих слоёв. Коммиты/push не выполнялись.

## Session 1319 - 2026-10-09 22:20 - Этап 2, фаза A: паритетные Theory → один запуск VM

Начат этап 2. Таблица `INTERPRETER_REMOVAL_EXECUTION_PROGRESS.csv` (158 файлов). Удалены все 240 строк `InlineData(ExecutionMode.Interpreter)` в 95 файлах `Execution.Tests` — в каждой Theory один реальный запуск VM с прежними независимыми ожиданиями; остаток таких строк — 0. Прогон Execution: **3916/3916, exit 0** (TRX `stage2a`). Далее фазы B/C: 145 mode-ссылок в телах и двойники; fallback-проверки сохраняются до этапа 5. Коммиты/push не выполнялись.

## Session 1320 - 2026-10-09 22:33 - Этап 2, фаза B завершена: Execution.Tests без запусков Interpreter

Фаза B: сведены к VM все циклы режимов (103), пары `first/second` (24), позиционные InlineData (4), удалены 3 двойника `Run_*InterpreterMode_*`; спецправки RND/Acceptance/RegexParity/NumericPromotion/DateTimeForm/PathNull/RandomInteger. `ExecutionMode.Interpreter` = 0; прогон **3097/3097, exit 0** (`stage2c`). Fallback/engine-проверки сохранены. Таблица EXECUTION_PROGRESS.csv: 158 × B_DONE. Далее: ревизия fallback/engine (§8-3), замыкания/рекурсия/BYREF, этапы 3–9. Коммиты/push не выполнялись.

## Session 1321 - 2026-10-09 22:41 - Этап 2 завершён

§8-3: 75 тернарников упрощены, `ExecutionEngineKind.Interpreter` = 0; fallback-проверки сохранены (408/16/262). §8-4: адресные группы рекурсии/BYREF/замыканий/представлений зелёные (Runtime 15/15, Execution 14/14); полный Execution **3097/3097**; полное решение **6328/6328, 9 TRX, exit 0**. Далее: этапы 3–9. Коммиты/push не выполнялись.

## Session 1322 - 2026-10-09 22:54 - Этап 3 завершён: честный успех компиляции

`CompilationStatus.BackendFailure` + честные `Succeeded`/`FrontendSucceeded`; IDE-маппинги и объединение диагностик; CLI `check` — полная проверка без исполнения. Тесты переписаны/добавлены; ExecutionPipelineTests 160/160, ProgramRunServiceTests 44/44, Cli 30/30, полный Execution 3098/3098, решение 6329/6329, 9 TRX, exit 0 — всё зелёное. Далее: этап 4 (IDE). Коммиты/push не выполнялись.

## Session 1323 - 2026-10-09 23:20 - Этап 4 завершён: IDE без выбора исполнителя

Меню/команды/состояние режима удалены; интерфейс `IProgramRunService` без `ExecutionMode`; журналы без `executionMode`; старые JSON совместимы (тест на 6 вариантов). Ide.Tests 931/931; решение 6026/6026, 9 TRX, exit 0. Далее: этап 5 (удаление Interpreter и fallback). Коммиты/push не выполнялись.


## Session 1325 - 2026-10-10 00:37 - Завершение отказа от интерпретатора

Переход к единственному компилятору байт-кода и BytecodeVm завершён по этапам 0–9. Текущее состояние и границы проверок: docs/plans/INTERPRETER_REMOVAL_RESULT.md; переносимые счётчики: INTERPRETER_REMOVAL_VALIDATION_20261010.json. Последняя регрессия 6032/6032 и восемь живых сценариев IDE успешны. Прежние записи «далее этап 5» являются историей.

Следующий шаг: просмотр итоговых изменений и, по отдельному поручению, коммит/публикация согласованного объёма без автоматического включения пользовательских удалений .playwright-mcp/* и временных артефактов. После этого отдельно проектировать новую оболочку, сохраняя функции WPF-IDE. Нестабильное ожидание фоновой компиляции остаётся известным ограничением; авария IDE по решению автора остаётся отложенной.


## Session 1326 - 2026-10-10 07:59 - Подробный план замены оболочки IDE

Следующее направление — замена основной оболочки IDE с сохранением функций Axiom. Подробный план: docs/plans/IDE_SHELL_REPLACEMENT_PLAN.md; матрица 52 возможностей: IDE_SHELL_FEATURE_MATRIX.csv; проект обмена: IDE_SHELL_PROTOCOL.md. Прежний ELECTRON_IDE_PLAN описывает ограниченный прототип и не задаёт конечную полноту новой среды.

Планирование выполнено, код новой реализации не начат. Следующий шаг по отдельному поручению: этап 0/исходная точка и изоляция данных, затем этап 1/макет и карта действий. Проект архитектуры — Electron + DesktopHost и общие службы .NET; не писать второй язык/графический движок. Графика и отладчик обязательны до переключения основного запуска. Удаление WPF-редактора предусмотрено только последним этапом после приёмки. Коммит/push и замена оболочки в текущей документной задаче не выполнялись.

## Session 1327 - 2026-10-10 08:22 - Реализация оболочки начата

Миграция VM и план закоммичены в 1091f29 по поручению автора; push не выполнялся. Этап 0: резервная копия, изоляция и свежие 6032/6032 + Electron 11/11 выполнены. Общие службы выделены, Ide.Tests 933/933. DesktopHost проходит пять процессных сценариев, но этап 3 остаётся открытым до канала CANVAS, проверки SCREEN, устойчивости транспорта и подписи сборки. Следующий шаг — эти недостающие проверки и мост Electron; выбор макета этапа 1 ещё не принят. Подробности и пути результатов: Session 1327 в WORKLOG5.md. После исправления отмены INPUT требуется общая регрессия. Старый WPF-редактор сохраняется до полной приёмки.

### Session 1328 - 2026-10-10 08:49 - Уточнённый внешний вид IDE

Макет исправлен по замечаниям автора: только пиктограммы в панели команд, без дополнительной вертикальной полосы, раскрывающиеся меню с переносом лишних пунктов в «…», отдельное большое окно CANVAS. Уточнение добавлено в docs/plans/IDE_SHELL_REPLACEMENT_PLAN.md. Продолжить реализацию с этим расположением и сохранить все функции Axiom; проверка HTML не означает завершения функциональных этапов. Незавершённая проверка BlockEditPlanner и повтор общей регрессии перечислены в Session 1328 WORKLOG5.md.

### Session 1329 - 2026-10-10 09:02 - Утверждённый макет перенесён в работающую оболочку

Автор подтвердил макет; принятое решение — docs/plans/IDE_SHELL_VISUAL_SPEC.md. Оформление Electron перенесено, CANVAS стал отдельным окном с настоящими кадрами и сохранением последнего кадра. BlockEditPlanner выделен в Axiom.Formatting; прежний WPF-адаптер и проверки сохранены. Свежая регрессия .NET 6039/6039, Node 22/22; результаты UI и промежуточная ошибка ожидания подробно отражены в Session 1329. Матрица функций обновлена без ложного закрытия строк.

Далее: через DesktopHost реализовать document.format/analyse/editTransition и metadata, проверять documentId/revision перед применением ответа в Monaco, сохранить undo/каретку и выполнить сценарии устаревших ответов. Затем недостающие файловые команды, графический ввод и полная приёмка отладчика/настроек/поставки. Старую WPF-оболочку пока не удалять.

### Session 1330 - 2026-10-10 09:24 - Общие команды документов подключены к Monaco

document.format/analyse/compile/editTransition и language.metadata работают через DesktopHost. Актуальность documentId/revision/модели/пути проверяется перед применением; старые ответы после правки, закрытия и SaveAs отвергаются. Структурное редактирование подключено к общему BlockEditPlanner с undo/кареткой. Форматирование и проверка работают с несохранённым буфером, запуск сохраняет прежнее требование сохранения. Итог: .NET 6039/6039, Node 29/29, Electron 21/21; промежуточные ошибки и исправления записаны в Session 1330 WORKLOG5.md.

Следующий незавершённый участок — этапы 5/6: «Сохранить всё», полноценное закрытие Save/Discard/Cancel, отсутствующие действия файлового дерева и операции файлов/каталогов. Затем завершить умные идентификаторы и функциональную приёмку этапа 7, графический ввод, отладчик, настройки и поставку. Полную замену и удаление WPF пока не объявлять завершёнными.

### Session 1331 - 2026-10-10 09:45 - Сохранение и закрытие новой оболочки

Реализованы «Сохранить всё», сохранение конкретной модели без ложной отметки при новой правке, Save/Discard/Cancel для вкладки и выхода, последовательное подтверждение закрытия проекта. Форматирование перед сохранением работает и в неактивной вкладке. Отброшенный при выходе текст не восстанавливается из настроек. Проверки: Node 32/32, Electron 28/28; подробности и границы доказательства — Session 1331 WORKLOG5.md.

Следующий незавершённый участок — этап 6: полноценное дерево файлов/каталогов, безопасное создание, переименование открытой модели, копирование/вырезание/вставка, корзина, свойства и внешние изменения. После него — умные идентификаторы и оставшаяся функциональная приёмка редактора, графический ввод, отладчик, настройки, поставка. До полной приёмки WPF не удалять.

### Session 1333 - 2026-10-10 10:03 - Подсветка и безопасное создание файла

Исправлен обрыв Monarch на запятых/двоеточиях/точках с запятой; проверен весь maze_chase.axm, интерполяция и видимые цвета категорий в обеих темах. Создание файла проекта использует исключительную запись без перезаписи существующего файла и не заменяет открытую несохранённую модель. Автор поручил коммит и отправку исправления до продолжения.

После отправки продолжить этап 6: иерархическое дерево файлов/каталогов, создание каталогов, переименование с согласованием открытых моделей, копирование/вырезание/вставка, корзина, свойства и внешние изменения. WPF пока сохранена.

### Session 1334 - 2026-10-10 14:28 - Уточнение модели файлов и исправление меню

Axiom сохраняет работу с отдельными файлами; многофайловые проекты не введены. Автор разрешил оставить «Открыть папку проекта», но развитие нового дерева папок пока отложено. Следующий участок — редактор отдельных файлов и оставшиеся визуальные замечания.

Из строки меню удалена надпись A / AXIOM. Колонка галочки занимает постоянное место и не сдвигает названия пунктов. Приёмка: 7 утверждений меню и 11 утверждений адаптивности/тем. Подробности — Session 1334 WORKLOG5.md.

### Session 1335 - 2026-10-10 14:41 - Восстановление форматирования и языка ошибок

Классический и современный режимы доступны через «Правка → Форматирование»; один Enter форматирует предыдущую строку и применяет общие правила блока с корректной кареткой. Язык уже показанных MB-сообщений и маркеров соответствует выбранному языку интерфейса без повторного запуска. Node 37/37, Electron 34/34, DesktopHost собран без ошибок и предупреждений.

Полная подробная локализация всех вариантов MB-сообщений ещё не завершена: сложные варианты используют объяснение кода с именами. Далее — точные формулировки и приёмка оставшихся возможностей редактора отдельных файлов, умные идентификаторы. Проектное дерево не развивать; WPF сохранить до полной приёмки.

### Session 1336 - 2026-10-10 14:54 - Цвета пользовательских имён

В обеих темах различаются переменные, процедуры/функции, явно объявленные массивы, CONST, операции и скобки. Классификация опирается на токены и объявления документа с локальными параметрами, не заменяя проверку компилятора. Уточнены названия автоматического форматирования и проверки ошибок при наборе. Настоящая ручная команда форматирует весь несохранённый документ в выбранном Classic/Modern без записи на диск; автоматические команды проверены отдельно. Далее — подробные переводы MB и оставшаяся приёмка редактора; проекты отложены.

### Session 1337 - 2026-10-10 15:07 - Построчное форматирование без лишних переключателей

По прямому уточнению автора проверка ошибок при наборе постоянно включена. Удалены автоматическое форматирование всего документа при сохранении и уходе фокуса и переключатель проверки; старые значения профиля игнорируются. Построчный Classic/Modern остаётся основным поведением; ручное форматирование всего буфера остаётся для вставленного/открытого текста. Меню и подменю не закрываются от ухода мыши, закрываются по выбору, щелчку снаружи либо Escape. Далее — оставшаяся приёмка редактора отдельных файлов и подробные переводы MB.
