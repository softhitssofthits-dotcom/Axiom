# Журнал работ 4

Этот файл является активным журналом работ проекта начиная с Session 832.

Исторические части журнала:
- `docs/WORKLOG.md` содержит записи до Session 168 включительно;
- `docs/WORKLOG2.md` содержит записи с Session 169 по Session 309 включительно;
- `docs/WORKLOG3.md` содержит записи с Session 310 по Session 831 включительно.

Правила ведения остаются прежними:
- новые записи добавляются только в конец файла;
- заголовок новой записи имеет вид `## Session <N> - YYYY-MM-DD HH:mm - <название>`;
- номер сеанса продолжает общую последовательность из `docs/WORKLOG.md`, `docs/WORKLOG2.md` и `docs/WORKLOG3.md`;
- дата и время указываются по местному времени проекта.

## Session 832 - 2026-05-13 22:09 - переписаны текстовые операторы SCREEN

Что сделано:
- продолжен редакционный проход по графическому разделу справочника;
- `docs/manual/graphics-text.html` переписан как страница операторов `CLS`, `COLOR` и `LOCATE`;
- начало страницы теперь объясняет текст в окне `SCREEN` через `PRINT`, строки, столбцы и текстовую ячейку `8 x 12`;
- для `COLOR` и `LOCATE` добавлены формальные записи с русскими метапеременными и таблицы параметров;
- примеры на странице переведены на русский вывод;
- таблица палитры упрощена до строгой справочной формы `индекс -> цвет`;
- видимое название `Текстовый слой SCREEN` заменено на `Текст на экране` в соседних ссылках, верхней навигации, меню manual и поисковом индексе;
- оставшиеся формулировки вроде `текстовый слой`, `пиксельная поверхность`, `графическая поверхность`, `Ручной пример: samples/...` проверены и убраны из затронутых графических страниц.

Изменённые файлы:
- `docs/manual/graphics-text.html`
- `docs/manual/characters.html`
- `docs/manual/data-and-input.html`
- `docs/manual/graphics-primitives.html`
- `docs/manual/graphics.html`
- `docs/manual/index.html`
- `docs/manual/input.html`
- `docs/manual/print.html`
- `docs/manual/screen-functions.html`
- `docs/manual/screen.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- для читателя manual лучше использовать видимое название `Текст на экране`, а не техническое `Текстовый слой SCREEN`;
- проверочные пути и технические термины должны быть ссылками и точными справочными словами, а не основным текстом;
- правка была редакционной: поведение языка, runnable `.axm`-примеры, автотесты, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` не менялись.

Проверки:
- `node --check docs\manual\scripts\manual.js` проходит;
- `node --check docs\manual\scripts\search-index.js` проходит;
- проверка локальных HTML-ссылок прошла: `OK: html pages 89, missing links 0`;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- продолжить содержательный проход по `docs/manual/graphics-primitives.html`: переписать страницу операторов рисования в том же стиле, убрать остатки технической подачи, проверить синтаксис и примеры `PSET`, `PRESET`, `LINE`, `CIRCLE` и `POINT`.

## Session 833 - 2026-05-13 22:13 - переписаны операторы рисования

Что сделано:
- продолжен редакционный проход по графическому разделу справочника;
- `docs/manual/graphics-primitives.html` переписан как страница операторов рисования `PSET`, `PRESET`, `LINE`, `CIRCLE` и функции `POINT`;
- добавлено объяснение координат, цветов и требования выбрать графический режим `SCREEN 1..10`;
- для `PSET`, `PRESET`, `LINE`, `CIRCLE` и `POINT` добавлены формальные записи с метапеременными и таблицы параметров;
- примеры на странице уточнены так, чтобы вывод был понятен читателю;
- в замечания добавлены палитра `0..255`, ошибка `MB3010` вне графического экрана и короткое сравнение с QuickBASIC по неподдержанным формам;
- `docs/manual/scripts/search-index.js` уточнён без видимой формулировки `пиксельные команды`.

Изменённые файлы:
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- страницу операторов рисования нужно читать как BASIC-справку по конкретным операторам, а не как описание внутреннего пиксельного слоя;
- правка была редакционной: поведение языка, runnable `.axm`-примеры, автотесты, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` не менялись.

Проверки:
- `node --check docs\manual\scripts\manual.js` проходит;
- `node --check docs\manual\scripts\search-index.js` проходит;
- проверка локальных HTML-ссылок прошла: `OK: html pages 89, missing links 0`;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- пройти `docs/manual/screen-functions.html`: привести функции `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH` и `SCREENHEIGHT` к той же справочной структуре и сверить формулировки с текущим договором `SCREEN 0` и графических режимов.

## Session 834 - 2026-05-13 22:34 - полная форма CIRCLE

Что сделано:
- перечитаны обязательные активные документы проекта и продолжена работа по более приоритетному запросу пользователя о `CIRCLE`;
- изучен BASIC-договор оператора `CIRCLE`: `STEP`, пропускаемые параметры, дуги через `start`/`end`, отрицательные углы с радиальными линиями и `aspect` для эллипса;
- `CIRCLE` расширен в parser, syntax, semantics, interpreter, lowering, bytecode-эмиссии, валидаторе байткода, `BytecodeVm` и host-слое IDE;
- `ProgramConsoleService` теперь хранит текущую графическую позицию и рисует дуги/эллипсы через общий путь `DrawCircle`;
- добавлен runnable sample `samples/graphics/primitives/circle_arcs_step_aspect.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/graphics-primitives.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/primitives/circle_arcs_step_aspect.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~CircleSupportsStepArcAndAspect|FullyQualifiedName~DrawCircle"` проходит;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~DrawCircle"` проходит;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ScreenCircleFullFormsAcceptanceSample"` проходит.

Решения:
- новая форма `CIRCLE` сохраняет BASIC-порядок параметров и не вводит отдельные имена для дуг или эллипсов;
- `STEP` у `CIRCLE` использует текущую графическую позицию, которую обновляют `PSET`, `LINE` и сам `CIRCLE`;
- отрицательные `start` и `end` не означают отрицательный угол дуги, а включают радиальную линию и используют модуль угла.

Следующий шаг:
- запустить широкий последовательный прогон затронутых тестовых проектов и сборки;
- после стабилизации вернуться к плановому редакционному проходу `docs/manual/screen-functions.html`.

## Session 835 - 2026-05-13 23:04 - CIRCLE доведён до проверенного состояния

Что сделано:
- завершена реализация полной BASIC-подобной формы `CIRCLE [STEP] (x, y), radius[, [color][, [start][, [end][, aspect]]]]`;
- подтверждены `STEP`, пропускаемые параметры, дуги через `start`/`end`, радиальные линии для отрицательных углов и `aspect` для эллипсов в parser, semantics, interpreter, bytecode VM и host-слое IDE;
- общий аудит положительных `.axm`-примеров получил дополнительные scripted input случаи для интерактивных `INKEY`, `KEYPRESS`, `ON KEY` и `ON MOUSE` примеров, чтобы графические примеры не зависали из-за отсутствующего ввода;
- подтверждён runnable sample `samples/graphics/primitives/circle_arcs_step_aspect.axm`;
- после финальных проверок оставлены актуальными спецификация, справочник, реестр покрытия bytecode и план.

Изменённые файлы:
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/primitives/circle_arcs_step_aspect.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj` проходит: 104 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит: 661 тест;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~DrawCircle"` проходит: 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ScreenCircleFullFormsAcceptanceSample"` проходит: 1 тест;
- `node --check docs\manual\scripts\manual.js` проходит;
- `node --check docs\manual\scripts\search-index.js` проходит;
- `dotnet build Axiom.sln` проходит.

Ограничения проверки:
- полный `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` не доведён до конца: отдельный существующий тест `OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm` и sample `samples/operators/on-key/stop_resume_handler.axm` зависают при scripted вводе и требуют отдельного разбора `ON KEY STOP/ON`.

Следующий точный шаг:
- разобраться с зависанием `samples/operators/on-key/stop_resume_handler.axm` в `Axiom.Execution.Tests`, затем повторить полный `Axiom.Execution.Tests`; после этого вернуться к плановому редакционному проходу по `docs/manual/screen-functions.html`.

## Session 836 - 2026-05-14 10:01 - стабилизирован scripted input для ON KEY STOP/ON

Что сделано:
- проверены журналы открытой IDE после ручного запуска `samples/operators/on-key/stop_resume_handler.axm`;
- ручной запуск в IDE прошёл успешно: вывод содержит приглашение, `LEFT`, `обработчик LEFT`, `готово`, статус `Success`;
- установлено, что IDE запускала sample в режиме `Interpreter`, поэтому ручная проверка подтверждает пользовательский сценарий, но не воспроизводит байткодный acceptance-путь;
- причина зависания `OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm` найдена в тестовом scripted input: очередь `LEFT`, `LEFT`, `ESC` была доступна сразу после регистрации обработчика, ещё до `ON KEY("LEFT"), STOP`, и boundary-диспетчер `BytecodeVm` мог обработать будущие клавиши раньше точки, которую sample имитирует как ручной ввод;
- scripted input для точечного теста и для общего audit-провайдера `samples/operators/on-key/stop_resume_handler.axm` синхронизирован с низкоуровневыми runtime-тестами: добавлены два пустых чтения перед реальными клавишами, чтобы первые реальные `LEFT`, `LEFT`, `ESC` появились только после установки `STOP`.

Изменённые файлы:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит: 1 тест;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~OnKey" --no-restore` проходит: 16 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~stop_resume_handler" --no-restore` проходит: 1 тест;
- полный `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` был остановлен после долгой паузы без вывода, поэтому его нужно повторить отдельно с диагностикой зависаний.

Решение:
- поведение языка и sample не менялись, изменена только модель тестового ввода для сценария, который в реальном IDE-потоке получает клавиши после вывода приглашения и установки `STOP`;
- новая `.axm`-проба и правка manual не требуются, потому что пользовательское поведение `ON KEY STOP/ON` не изменилось.

Следующий точный шаг:
- повторить полный `Axiom.Execution.Tests`; если он снова не завершится, запускать его с `--blame-hang` и искать следующий зависающий test case, затем вернуться к плановому редакционному проходу по `docs/manual/screen-functions.html`.

## Session 837 - 2026-05-14 11:40 - уточнён текст примера ON KEY STOP/ON

Что сделано:
- проверены свежие журналы IDE после ручного запуска `samples/operators/on-key/stop_resume_handler.axm` в режиме `Bytecode`;
- подтверждено, что запуск прошёл через `BytecodeVm` и завершился успешно: `requestedMode=Bytecode`, `actualEngine=BytecodeVm`, `status=Success`, диагностики отсутствуют;
- вывод байткодного запуска: приглашение, `LEFT`, `обработчик LEFT`, `готово`;
- текст runnable sample уточнён: вместо неясного «на паузе» теперь сказано «при отключённом обработчике», чтобы было понятно, что `ON KEY("LEFT"), STOP` отключает обработчик события, а не ставит всю программу на паузу;
- ожидаемая строка вывода в acceptance-тесте синхронизирована с новым текстом sample.

Изменённые файлы:
- `samples/operators/on-key/stop_resume_handler.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит: 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~stop_resume_handler" --no-restore` проходит: 1 тест.

Решение:
- поведение языка не менялось, поэтому справочник и реестр покрытия bytecode не требуют отдельной правки;
- sample должен говорить пользовательским языком: первое `LEFT` проверяет, что при отключённом обработчике клавиша доступна через `INKEY`, второе `LEFT` проверяет, что после `ON` обработчик снова вызывается.

Следующий точный шаг:
- повторить полный `Axiom.Execution.Tests`; если он снова не завершится, запускать его с `--blame-hang` и искать следующий зависающий test case, затем вернуться к плановому редакционному проходу по `docs/manual/screen-functions.html`.

## Session 838 - 2026-05-14 11:56 - полный Axiom.Execution.Tests стабилизирован

Что сделано:
- повторён полный `Axiom.Execution.Tests` с `--blame-hang --blame-hang-timeout 90s`;
- после снятия блокера `ON KEY STOP/ON` найдены и исправлены оставшиеся проблемы общего audit-ввода для интерактивных samples;
- для `samples/graphics/text/print_locate_color.axm` audit-провайдер теперь отдаёт две клавиши, потому что пример содержит два `SLEEP` без длительности;
- для `samples/graphics/input/text_screen_input.axm` audit-провайдер и точечный тест теперь отдают имя и возраст, потому что пример читает две переменные через `INPUT`;
- для `samples/functions/keydown/held_modifiers.axm` и `samples/functions/keydown/canonical_aliases.axm` audit-провайдер задаёт удерживаемые клавиши;
- для `samples/functions/mousex`, `mousey`, `mousebutton` и `mousepress` audit-провайдер теперь использует графический host с активируемым `SCREEN` и нужным состоянием мыши;
- mouse acceptance-тесты синхронизированы с текущим договором `SCREEN`: проверяют, что обычный `Output` пуст, а текст после `SCREEN 0` находится в `GraphicsOutput`.

Изменённые файлы:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~print_locate_color" --no-restore` проходит: 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ScreenZeroTextInputAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит: 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~text_screen_input|FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~canonical_aliases|FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~held_modifiers" --no-restore` проходит: 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MouseCoordinatesAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~MouseButtonAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~MousePressAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~OnMouseAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~OnMouseReleaseMoveAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~OnMouseWheelAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит: 6 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~mousex|FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~mousey|FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~mousebutton|FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~mousepress" --no-restore` проходит: 4 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --blame-hang --blame-hang-timeout 90s` проходит: 840 тестов.

Решение:
- проблема полного `Axiom.Execution.Tests` закрыта: набор больше не зависает и не падает;
- изменения относятся к тестовому scripted input и ожиданиям acceptance-тестов, поведение языка и runnable samples не менялись.

Следующий точный шаг:
- вернуться к плановому редакционному проходу по `docs/manual/screen-functions.html`, если не появится более срочный запрос.

## Session 839 - 2026-05-14 13:32 - STEP у PSET и LINE, позиционная форма POINT

Что сделано:
- после обязательного чтения `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` поднят текущий графический срез и сверены уже реализованные команды с QuickBASIC-линейкой;
- выяснено, что базовое графическое ядро уже есть (`SCREEN`, `CLS`, `COLOR`, `LOCATE`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `POINT`), но для исторической совместимости не хватало `STEP` у `PSET`/`PRESET` и `LINE`, формы `LINE -(...)` от текущей графической позиции и позиционной формы `POINT(number)`;
- синтаксис, binder, lowered-модель, bytecode-эмиссия, `BytecodeVm` и interpreter расширены на:
  - `PSET [STEP](x, y)[, color]`;
  - `PRESET [STEP](x, y)[, color]`;
  - `LINE [[STEP](x1, y1)]-[STEP](x2, y2)[, [color][, B|BF]]`;
  - `POINT(number)` со значениями `0..3` для чтения текущей графической позиции;
- host-договор `IGraphicsHost` расширен текущими foreground/background-цветами и текущей графической позицией, чтобы interpreter и `BytecodeVm` использовали один и тот же источник состояния;
- приведены к исторической модели цвета по умолчанию:
  - `PSET` без цвета теперь берёт текущий foreground;
  - `PRESET` без цвета теперь берёт текущий background;
  - `LINE` без цвета теперь берёт текущий foreground;
- добавлен runnable sample `samples/graphics/primitives/step_and_point_position.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/graphics-primitives.html`, `docs/manual/screen-functions.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Input.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/primitives/step_and_point_position.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/manual/screen-functions.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_PSetAndLineSupportStepAndPointPosition|FullyQualifiedName~Execute_CircleSupportsStepArcAndAspect|FullyQualifiedName~Execute_CompiledPSetAndLineSupportStepAndPointPosition|FullyQualifiedName~Execute_CompiledCircleSupportsStepArcAndAspect"` проходит: 4 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~GraphicsState_TracksCurrentPositionAndColors|FullyQualifiedName~DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius"` проходит: 2 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ScreenStepAndPointPositionAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ScreenPrimitivesAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ScreenCircleFullFormsAcceptanceSample_RunsOnBytecodeVm"` проходит: 3 теста;
- `node --check docs\manual\scripts\manual.js` проходит;
- `node --check docs\manual\scripts\search-index.js` проходит.

Решение:
- сначала выровнять историческое поведение уже существующих примитивов, а не начинать следующую большую графическую ветку с `PAINT` или `DRAW`;
- `POINT(number)` введён как часть уже известного BASIC-имени `POINT`, а не как новый built-in с отдельным названием;
- изменение цвета по умолчанию для `PSET`/`PRESET`/`LINE` закреплено сразу в runtime, VM, sample и manual, чтобы не оставлять расхождение между интерпретатором и справочником.

Следующий точный шаг:
- выбрать следующий самостоятельный графический срез: либо `PAINT` как базовую заливку, либо `DRAW` как графический макроязык;
- если перед этим нужнее координатная инфраструктура, спроектировать отдельным пакетом `VIEW`, `WINDOW` и `PMAP`.

## Session 840 - 2026-05-14 12:59 - исправлен IDE-путь графического состояния для STEP и POINT

Сделано:
- разобран ручной запуск `samples/graphics/primitives/step_and_point_position.axm` по свежему журналу `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260514-125143.log`;
- подтверждено, что sample реально выполнялся в `BytecodeVm`, а расхождение возникало не в parser/runtime-логике примитивов, а в IDE/execution-переходнике;
- найдена точная причина: `ExecutionRuntimeHostAdapter` и `IdeInputProvider` проксировали графические методы, но не проксировали свойства текущего графического состояния `CurrentForegroundColor`, `CurrentBackgroundColor`, `CurrentGraphicsX`, `CurrentGraphicsY`;
- из-за этого `BytecodeVm` в реальном IDE-пути читал дефолты `IGraphicsHost` (`15/0/0/0`), поэтому `POINT(0..3)`, `PSET STEP` и `LINE` от текущей точки работали как будто позиция и цвета не менялись;
- добавлено проксирование этих свойств в оба слоя;
- добавлены регрессионные проверки:
  - `IdeInputProvider_ProxiesGraphicsStateFromProgramConsoleService`;
  - `Run_GraphicsStepSample_CompletesOnBytecodeVmWithoutInterpreterFallback`;
  - `Run_WithBytecodeMode_UsesBytecodeVmForGraphicsStepAndPointState`;
- тестовый graphics-host в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` доведён до хранения текущих цветов и текущей графической позиции, чтобы такие ошибки больше не маскировались дефолтами тестового двойника.

Изменённые файлы:
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Run_GraphicsStepSample_CompletesOnBytecodeVmWithoutInterpreterFallback|FullyQualifiedName~IdeInputProvider_ProxiesGraphicsStateFromProgramConsoleService|FullyQualifiedName~GraphicsState_TracksCurrentPositionAndColors" --no-restore` проходит: 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForGraphicsStepAndPointState|FullyQualifiedName~ScreenStepAndPointPositionAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит: 2 теста.

Решение:
- ручное расхождение IDE против тестов было вызвано не старой сборкой и не логикой `ProgramConsoleService`, а неполным проксированием состояния через IDE/execution-адаптеры;
- для `SCREEN`-сценариев `ProgramRunResult.OutputText` по-прежнему может оставаться пустым, потому что `PRINT` внутри активного `SCREEN` уходит в графический канал, а не в обычный transcript; это не связано с исправленным багом `STEP`/`POINT`.

Следующий точный шаг:
- вручную повторить `samples/graphics/primitives/step_and_point_position.axm` в `Axiom.Ide` и убедиться по экрану, что теперь выводятся ожидаемые значения `point0=10 point1=20`, `after pset step=15,24`, `after line from current=30,40`, `after line step-step=45,60`, `pixel 10,20=6`, `pixel 15,24=6`;
- после подтверждения вернуться к следующему самостоятельному графическому срезу: `PAINT`, `DRAW` или координатный пакет `VIEW`/`WINDOW`/`PMAP`.

## Session 841 - 2026-05-14 17:07 - реализован прозрачный текстовый фон COLOR в SCREEN

Сделано:
- после обязательного чтения `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md` и плана `docs/План современная BASIC-модель текстового слоя SCREEN.txt` продолжена и завершена реализация современного договора текстового слоя `SCREEN`;
- подтверждён и доведён центральный host-контракт с тремя состояниями фона `Unchanged`, `Color`, `Transparent`, чтобы `COLOR foreground`, `COLOR foreground, background` и `COLOR foreground, TRANSPARENT` больше не делили один и тот же смысл;
- исправлена IDE-регрессия: `ProgramConsoleService` сохраняет последний числовой background для `PRESET`, даже когда текстовый фон переведён в прозрачный режим;
- добавлены адресные runtime- и `BytecodeVm`-проверки на непрозрачный фон, прозрачный фон и сохранение режима фона после `COLOR foreground`;
- словари подсветки `Axiom.xshd` и `Axiom.Dark.xshd` дополнены ключевым словом `TRANSPARENT`;
- добавлен runnable sample `samples/graphics/text/transparent_text.axm`, который показывает непрозрачную строку, прозрачную подпись поверх графики, ожидание `ESC` и завершение через `SCREEN 0`;
- обновлены `docs/manual/graphics-text.html`, `docs/manual/scripts/search-index.js`, `docs/SPECIFICATION3.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/text/transparent_text.axm`
- `docs/manual/graphics-text.html`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_ColorSupportsOpaqueTransparentAndUnchangedBackgroundModes|FullyQualifiedName~Execute_CompiledColorSupportsOpaqueTransparentAndUnchangedBackgroundModes"` проходит: 2 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~SetTextColor_TransparentBackgroundPreservesNumericBackgroundForPreset|FullyQualifiedName~WriteGraphicsText_PreservesCellColorsAndTransparentBackground|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes"` проходит: 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ScreenTransparentTextAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ScreenTextAcceptanceSample_RunsOnBytecodeVm"` проходит: 2 теста.

Решение:
- считать план по современному текстовому фону `SCREEN` реализованным на уровне языка, VM, IDE, acceptance-sample и manual;
- сохранять разделение между прозрачностью текстового фона и числовым background для графических операций как постоянный договор дальнейших графических задач.

Следующий точный шаг:
- вручную открыть `samples/graphics/text/transparent_text.axm` в `Axiom.Ide` и визуально подтвердить, что непрозрачная строка имеет синюю плашку, а строка `TRANSPARENT` лежит поверх красного прямоугольника без своей плашки;
- после ручного подтверждения вернуться к следующему самостоятельному графическому срезу: `PAINT`, `DRAW` или `VIEW`/`WINDOW`/`PMAP`.

## Session 842 - 2026-05-14 17:18 - отложенная прокрутка SCREEN 0 вынесена в отдельный файл, следующим срезом выбран PAINT

Сделано:
- после пользовательского подтверждения по скриншоту и журналу зафиксировано, что `transparent_text.axm` ведёт себя правильно и в интерпретаторе, и в `BytecodeVm`;
- обсуждение автопереноса и прокрутки не смешивалось с текущим графическим шагом: принято решение не расширять сейчас общий договор `SCREEN`;
- создан новый файл `docs/FUTURE.md` для отложенных и будущих задач, которые нельзя терять между сессиями, но которые не должны засорять активный ближайший план;
- в `docs/FUTURE.md` записана тема будущей вертикальной прокрутки `SCREEN 0` с отдельным договором и причиной, почему она отложена;
- в `docs/PLANS3.md` зафиксировано, что следующим самостоятельным графическим срезом выбран `PAINT`, а не дальнейшее расширение текстового слоя.

Изменённые файлы:
- `docs/FUTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- для `SCREEN 1..10` сохранить текущий договор без автопереноса и автопрокрутки;
- для `SCREEN 0` не потерять идею будущей прокрутки, но вести её как отдельную отложенную задачу;
- следующий активный графический шаг вести вокруг `PAINT` как следующего базового примитива после `PSET`, `PRESET`, `LINE` и `CIRCLE`.

Следующий точный шаг:
- открыть проектирование `PAINT` через обновление `docs/SPECIFICATION3.md`: зафиксировать синтаксис, минимальную семантику первого среза, ограничения, диагностику и acceptance-sample;
- после этого переходить к реализации.

## Session 843 - 2026-05-14 17:24 - зафиксирован договор первого графического среза PAINT

Сделано:
- после обязательного чтения `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` открыт следующий активный графический шаг вокруг `PAINT`;
- в `docs/SPECIFICATION3.md` добавлен первый пользовательский договор `PAINT` как базовой граничной заливки;
- зафиксированы формы `PAINT (x, y)`, `PAINT (x, y), fillColor` и `PAINT (x, y), fillColor, borderColor`;
- зафиксированы обязательные правила первого среза: только `SCREEN 1..10`, координаты как `LONG`, foreground по умолчанию от `COLOR`, граница по умолчанию из стартового пикселя, отсутствие действия при совпадении стартового пикселя с цветом заливки или границы, распространение только по четырём соседям;
- отдельно зафиксированы границы этого шага: без `STEP`, без шаблонов заливки, без отдельного высокого host-API и без смешивания с отложенной темой прокрутки `SCREEN 0`.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- сначала реализовать минимальный честный `PAINT`, который естественно продолжает уже существующие `PSET`, `PRESET`, `LINE`, `LINE ..., BF`, `CIRCLE` и `POINT`;
- не вводить пока расширенные формы, чтобы сквозная реализация в interpreter, `BytecodeVm` и IDE-host оставалась локальной и проверяемой.

Следующий точный шаг:
- провести реализацию `PAINT` по всей цепочке `lexer -> syntax -> parser -> binder -> lowering -> bytecode compiler -> interpreter -> BytecodeVm -> IDE host`;
- затем добавить automated tests, runnable sample, manual и обновление `docs/BYTECODE_COVERAGE.md`.

## Session 844 - 2026-05-14 18:18 - базовый PAINT реализован

Сделано:
- после обязательного чтения `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` продолжен активный графический шаг `PAINT`;
- ключевое слово `PAINT` добавлено в `lexer`, `syntax`, parser, binder, lowering, bytecode compiler, interpreter и `BytecodeVm`;
- базовая заливка реализована как общий runtime-хелпер поверх `GetPixel` и `SetPixel`, без нового высокого host-API;
- добавлены parser-, interpreter-, `BytecodeVm`-, IDE framebuffer- и execution-acceptance-проверки;
- добавлен runnable sample `samples/graphics/primitives/paint_fill_region.axm`;
- обновлены `docs/manual/graphics-primitives.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/GraphicsFloodFill.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/primitives/paint_fill_region.axm`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать первый базовый договор `PAINT` завершённым без `STEP` и шаблонной заливки;
- не смешивать этот шаг с отложенной темой прокрутки `SCREEN 0`.

Следующий точный шаг:
- последовательно прогнать целевые проверки;
- затем вручную проверить `samples/graphics/primitives/paint_fill_region.axm` в `Axiom.Ide`.

## Session 845 - 2026-05-14 19:02 - PAINT в IDE переведён на пакетную публикацию кадра

Сделано:
- после пользовательской ручной проверки подтверждено, что базовый `PAINT` в IDE визуально закрашивал область слишком медленно, с заметным распространением по слоям;
- узкое место локализовано в `ProgramConsoleService`: сама заливка работала корректно, но UI-путь не отделял массовое изменение framebuffer от публикации кадра;
- в `ProgramConsoleService` добавлен внутренний пакетный режим графического обновления для `PAINT`, чтобы заливка выполнялась целиком и только после этого планировалась визуальная публикация;
- повторно пройдены целевые IDE-проверки `Paint_FillsRegionWithoutRecoloringBorder` и `Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes`.

Изменённые файлы:
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\\Axiom.Ide.Tests\\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Paint_FillsRegionWithoutRecoloringBorder|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes" --no-restore` проходит.

Решение:
- считать медленную покадровую заливку дефектом первого host-пути, а не ожидаемым пользовательским поведением;
- не менять пользовательский синтаксис `PAINT`, а доводить только внутренний IDE-rendering.

Следующий точный шаг:
- отдельно добить execution acceptance для `samples/graphics/primitives/paint_fill_region.axm`, если эта ветка остаётся обязательной перед выбором следующего графического среза;
- затем вернуться к выбору следующего самостоятельного графического шага.

## Session 846 - 2026-05-14 19:18 - PAINT переведён на быстрый host-путь без пиксельных прокси-вызовов

Сделано:
- после повторной пользовательской оценки подтверждено, что одна только пакетная публикация кадра не дала заметного ускорения `PAINT`;
- реальный источник тормоза локализован в том, что runtime-заливка в IDE шла через цепочку `ExecutionRuntimeHostAdapter -> IdeInputProvider -> ProgramConsoleService` с отдельными `GetPixel` и `SetPixel` на каждый пиксель;
- в `IGraphicsHost` добавлен специализированный метод `Paint(x, y, fillColor, borderColor)` с fallback-реализацией по умолчанию через `GraphicsFloodFill`;
- interpreter и `BytecodeVm` переведены на `_host.Paint(...)` вместо прямого вызова flood-fill helper через делегаты `GetPixel`/`SetPixel`;
- `ExecutionRuntimeHostAdapter` и `IdeInputProvider` проксируют новый метод целиком, а `ProgramConsoleService` выполняет заливку локально на стороне IDE-host;
- повторно пройдены целевые runtime- и IDE-проверки.

Изменённые файлы:
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\\Axiom.Runtime.Tests\\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_PaintFillsBoundedRegion|FullyQualifiedName~Execute_CompiledPaintFillsBoundedRegion" --no-restore` проходит;
- `dotnet test tests\\Axiom.Ide.Tests\\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Paint_FillsRegionWithoutRecoloringBorder|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes" --no-restore` проходит.

Решение:
- считать именно межслойные пиксельные прокси-вызовы главным тормозом первого варианта `PAINT` в IDE;
- держать специализированный host-путь `Paint(...)` как постоянную оптимизацию для массовых графических операций.

Следующий точный шаг:
- снова вручную проверить `paint_fill_region.axm` в IDE;
- если ручная скорость уже устраивает, вернуться к execution acceptance или к следующему графическому срезу.

## Session 847 - 2026-05-14 18:44 - закрытие SCREEN во время SLEEP больше не оставляет IDE в состоянии выполнения

Сделано:
- после обязательного перечитывания `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` разобран новый дефект IDE по пользовательскому логу;
- по журналу `ide-session-20260514-182447.log` подтверждено, что после `screen.closed-by-user` выполнение не доходило до `run.pipeline`, то есть зависание происходило не в сбросе UI, а внутри самого runtime-ожидания;
- найдена причина: `ProgramConsoleService` корректно поднимал `IsCancellationRequested` при закрытии native-окна `SCREEN`, но `SLEEP milliseconds` в interpreter и `BytecodeVm`, а также `SLEEP` без аргумента в `BytecodeVm`, не прерывали ожидание по этому флагу;
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs` переведены на прерываемое ожидание короткими порциями по 10 мс с проверкой отмены между шагами;
- добавлены регрессионные runtime-тесты для обеих форм `SLEEP`: отмена во время `SLEEP` и отмена во время `SLEEP 1000` как для interpreter, так и для `BytecodeVm`;
- тестовые runtime-host'ы в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` дополнены явной реализацией `IRuntimeCancellationProvider`, чтобы проверка точно шла через тот же контракт, что и в IDE.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Keyboard.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "Execute_SleepWithoutDuration_StopsWhenCancellationIsRequested|Execute_SleepWithDuration_StopsWhenCancellationIsRequested|Execute_CompiledSleepWithoutDuration_StopsWhenCancellationIsRequested|Execute_CompiledSleepWithDuration_StopsWhenCancellationIsRequested"` проходит;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "Run_WhenHostCancellationIsRequested_ReturnsCancelled"` проходит.

Решение:
- считать этот дефект закрытым именно на уровне runtime-контракта отмены: IDE уже умела переводить `Cancelled` в выход из режима `Выполнение`, проблема была в том, что до `Cancelled` выполнение не доходило;
- не добавлять отдельный UI-обходной сброс состояния в `MainWindowViewModel`, пока корректно работает нормальная цепочка `screen.closed-by-user -> cancellation -> ProgramRunResult.Cancelled`.

Следующий точный шаг:
- вручную повторить пользовательский сценарий: открыть графический пример с `SLEEP`, закрыть окно `SCREEN` крестиком и убедиться, что IDE сразу возвращается в режим редактирования со статусом отменённого запуска;
- после ручного подтверждения вернуться к следующему графическому срезу.
## Session 848 - 2026-05-14 21:09 - живой сценарий отмены через закрытие SCREEN подтверждён по свежему логу IDE

Что сделано:
- после пользовательской ручной проверки просмотрен свежий журнал IDE `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260514-184821.log`;
- по этому журналу подтверждён штатный сценарий в обоих режимах выполнения на реальном примере `samples/graphics/primitives/paint_fill_region.axm`, который содержит `SLEEP`;
- в байткодном запуске журнал показывает полную цепочку `screen.closed-by-user -> screen.deactivated -> run.pipeline | status=Cancelled -> run.result | status=Cancelled`;
- в интерпретаторном запуске подтверждена та же цепочка отмены без зависания IDE и без продолжения выполнения после закрытия окна `SCREEN`;
- дополнительно видно, что обычные успешные запуски того же примера по-прежнему завершаются штатно через `run.pipeline | status=Success` и `run.result | status=Success`, то есть исправление не сломало нормальный путь завершения.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- просмотрен свежий пользовательский журнал IDE `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260514-184821.log`;
- подтверждены две пары запусков:
  - `BytecodeVm`: успешный запуск и запуск с пользовательским закрытием `SCREEN` во время `SLEEP`;
  - `Interpreter`: успешный запуск и запуск с пользовательским закрытием `SCREEN` во время `SLEEP`.

Решение:
- считать ручную приёмку дефекта по `SLEEP` и закрытию `SCREEN` завершённой не только по тестам, но и по живому пользовательскому сценарию в IDE;
- не возвращаться к этой ветке без нового сигнала из реального журнала или нового пользовательского дефекта.

Следующий точный шаг:
- вернуться к следующему самостоятельному графическому срезу после завершённых `PAINT` и `SLEEP`-исправлений;
- открыть в `docs/SPECIFICATION3.md` проектирование оператора `DRAW` как следующего кандидата из графической линии после базовых примитивов.

## Session 849 - 2026-05-14 21:09 - изменён приоритет графической ветки: текущий оператор доводится полностью перед следующим

Что сделано:
- по решению пользователя изменён рабочий приоритет для графических операторов: следующий оператор нельзя брать в работу, пока текущий не доведён до полного пользовательского договора;
- зафиксировано, что для графической ветки состояние «ограниченный первый срез закрыт» больше не считается достаточной точкой перехода к следующему оператору;
- активный план возвращён с проектирования `DRAW` на полную доводку `PAINT`, который сейчас реализован только как базовый срез без всех потенциальных форм.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- для графических операторов применять более строгий порядок, чем для обычных малых срезов языка: сначала полный договор одного оператора, затем реализация, проверки и manual, и только потом следующий оператор;
- не открывать `DRAW`, пока не будет явно решено и закрыто, какие формы `PAINT` входят в полный поддерживаемый договор проекта.

Следующий точный шаг:
- открыть в `docs/SPECIFICATION3.md` пересмотр договора `PAINT` и перечислить, какие оставшиеся формы должны войти в полную реализацию;
- после этого обновить план и только затем переходить к следующему этапу кода по `PAINT`.

## Session 850 - 2026-05-14 21:26 - полный договор PAINT зафиксирован в спецификации перед новым этапом реализации

Что сделано:
- после обязательного возврата к проектным документам подтверждено, что ветка `SCREEN` + `SLEEP` уже закрыта по живому логу, а активный хвост действительно смещён на полную доводку `PAINT`;
- в `docs/SPECIFICATION3.md` добавлен новый поздний блок `Обновление спецификации - 2026-05-14 - полный поддерживаемый договор PAINT`, который заменяет роль старого «первого среза» как актуального целевого договора;
- в полный договор `PAINT` явно включены все формы, которые раньше оставались вынесенными за скобки: `STEP`, строковая шаблонная заливка, четвёртый аргумент `фон` и BASIC-подобные пустые позиционные места для выхода к более поздним аргументам;
- отдельно зафиксированы честные проектные решения Axiom там, где не нужно притворяться побайтной QuickBASIC-совместимостью: строковый шаблон трактуется как байтовая маска шириной 8 бит и длиной до 64 символов, граница по умолчанию берётся из стартового пикселя, а текущая графическая позиция после `PAINT` обновляется в стартовую точку;
- `docs/PLANS3.md` синхронизирован с новым договором и теперь ведёт следующий этап уже на реализацию полного `PAINT`, а не на выбор следующего графического оператора.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- кодовые проверки не запускались, потому что задача в этой сессии была документной: сначала зафиксировать полный пользовательский договор `PAINT` перед следующей реализацией;
- перепроверены актуальные хвосты `docs/SPECIFICATION3.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md` и локальный исторический ориентир `.artifacts\manual\zeldner\zeldner_quickbasic_45.txt`.

Решение:
- считать следующий этап по `PAINT` уже не обсуждением границ, а реализацией зафиксированного полного договора;
- не возвращаться к `DRAW`, пока `PAINT` не будет доведён до состояния «договор + код + проверки + приёмочный пример + руководство пользователя + ручная приёмка».

Следующий точный шаг:
- реализовать полный договор `PAINT` в коде, включая `STEP`, строковую шаблонную заливку, аргумент `фон` и поздние позиционные аргументы;
- в той же задаче обновить автотесты, запускаемый пример, руководство пользователя и `docs/BYTECODE_COVERAGE.md`;
- затем вручную прогнать полный сценарий `PAINT` в `Axiom.Ide` и решить, закрыт ли оператор полностью.

## Session 851 - 2026-05-14 22:05 - полный PAINT синхронизирован с кодом и справочником

Что сделано:
- перечитаны обязательные активные документы проекта и подтверждено, что активный хвост работы остаётся на полном закрытии `PAINT`, а не на переходе к `DRAW`;
- дополнительно сверен локальный текст книги Зельднера `.artifacts/manual/zeldner/zeldner_quickbasic_45.txt` по разделу `PAINT`, после чего пользовательским решением закреплён приоритет: если тема есть у Зельднера, именно он является главным ориентиром для описания, а `learn.microsoft.com` нужен только как запасной источник для тем вне книги;
- подтверждено прохождение незавершённого acceptance-теста `ScreenPaintStepAcceptanceSample_RunsOnBytecodeVm`;
- `docs/SPECIFICATION3.md` синхронизирован с фактическим поведением полного `PAINT`: пропущенная `краска` теперь описана как текущий цвет фона, строковый образец — как байтовая маска через `ASC`/IBM CP866, граница по умолчанию различается для сплошной и строковой формы, а текущая графическая позиция после `PAINT` обновляется в стартовую точку;
- `docs/manual/graphics-primitives.html` переписан в части `PAINT` под полную форму оператора: добавлены `STEP`, строковый образец, `фон`, уточнены значения по умолчанию и inline-пример для полной формы;
- `docs/manual/scripts/search-index.js` и `docs/BYTECODE_COVERAGE.md` обновлены под полную форму `PAINT`, а проверочные ссылки в manual переведены на `samples/operators/paint/step_default_fill.axm` и `samples/operators/paint/pattern_repaint.axm`.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter Parse_PaintStatementSupportsStepEmptySlotsAndBackground --no-restore` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "Execute_PaintFillsBoundedRegion|Execute_PaintStepUsesCurrentPositionAndBackgroundColorByDefault|Execute_PaintPatternAndBackgroundPatternRepaintRegion|Execute_CompiledPaintFillsBoundedRegion|Execute_CompiledPaintStepUsesCurrentPositionAndBackgroundColorByDefault|Execute_CompiledPaintPatternAndBackgroundPatternRepaintRegion" --no-restore` проходит;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "Paint_FillsRegionWithoutRecoloringBorder|Paint_PatternFillAndBackgroundPatternRepaintRegion" --no-restore` проходит;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "ScreenPaintAcceptanceSample_RunsOnBytecodeVm|ScreenPaintStepAcceptanceSample_RunsOnBytecodeVm" --no-restore` проходит;
- `node --check docs\manual\scripts\search-index.js` проходит;
- общий `git diff --check` по репозиторию по-прежнему шумит на старом незавершённом хвосте `docs/WORKLOG3.md`, который не относится к текущему шагу; по файлам текущей задачи отдельные новые пробельные ошибки не добавлялись.

Решение:
- считать главным источником описания графических операторов Зельднера там, где соответствующая тема реально присутствует в книге;
- считать старые формулировки manual и спецификации про «первый срез `PAINT`» документным отставанием, а не действующим договором;
- не открывать следующий графический оператор, пока текущий не закрыт по коду, тестам, runnable sample и manual.

Следующий точный шаг:
- если по `PAINT` не появится новый пользовательский дефект или новый сигнал из журнала IDE, считать оператор закрытым на текущем этапе;
- затем отдельной задачей открыть следующий графический оператор через проектный договор в `docs/SPECIFICATION3.md`;
- ближайший кандидат после закрытого `PAINT` — `DRAW`, но уже как новый самостоятельный шаг.

## Session 852 - 2026-05-14 22:25 - правило Зельднера закреплено для manual и примеров PAINT

Что сделано:
- по уточнению пользователя в `AGENTS.md` добавлено постоянное правило: если тема уже описана у Зельднера, именно книга Зельднера является главным редакционным ориентиром для `docs/manual/`, а `learn.microsoft.com` используется только как запасной жанровый ориентир;
- в то же правило отдельно включено требование по возможности брать примеры из книги Зельднера или делать runnable `.axm`-примеры по её мотивам;
- `.codex/skills/axiom-doc-author/SKILL.md` синхронизирован с этим приоритетом, чтобы последующие проходы по manual держали тот же источник примеров;
- раздел `PAINT` в `docs/manual/graphics-primitives.html` переписан ближе к страницам 238-240 книги Зельднера: определение оператора, форма записи, разбор частей записи, отдельное объяснение битового образца и крупный пример рыбки;
- добавлен runnable sample `samples/operators/paint/fish_fill.axm` по мотивам примера рыбки из книги;
- `docs/BYTECODE_COVERAGE.md` обновлён под новый набор runnable sample для полного `PAINT`.

Изменённые файлы:
- `AGENTS.md`
- `.codex/skills/axiom-doc-author/SKILL.md`
- `docs/manual/graphics-primitives.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `samples/operators/paint/fish_fill.axm`

Проверки:
- кодовые тесты не запускались, потому что задача в этом сеансе была редакторской и примеровочной, без изменения семантики `PAINT`;
- нужно отдельно проверить только формат правок и отсутствие новых пробельных ошибок.

Решение:
- считать новым постоянным проектным правилом, что manual по возможности должен брать не только подачу, но и мотивы примеров из книги Зельднера;
- для `PAINT` главным примером страницы теперь считать адаптированный книжный сюжет с рыбкой, а не только минимальные технические формы заливки.

Следующий точный шаг:
- прогнать форматные проверки по изменённым файлам и при отсутствии замечаний считать текущий документный проход по `PAINT` завершённым;
- если пользователь не попросит дополнительную доводку `PAINT`, только после этого открывать следующий графический оператор.

## Session 853 - 2026-05-15 00:35 - DRAW проведён как полный самостоятельный договор

Что сделано:
- перечитан активный проектный контекст и подтверждено, что после закрытого `PAINT` следующим графическим оператором должен быть именно `DRAW`, причём не как первый срез, а как полный поддерживаемый договор;
- `DRAW` добавлен в `TokenKind`, `SyntaxKind`, parser, binder, `BoundNodeKind`, `BoundDrawStatement`, lowering, `Opcode.Draw`, compiler emission и validator;
- для `DRAW` выделена отдельная runtime-подсистема `GraphicsDrawParser`, `GraphicsDrawCommand`, `GraphicsDrawExecutor`, `GraphicsDrawException`; interpreter и `BytecodeVm` теперь только проверяют `SCREEN`, получают строку и вызывают общий executor;
- проведены parser- и binder-проверки для строкового литерала, строковой переменной и составного строкового выражения;
- проведены runtime-проверки interpreter и `BytecodeVm` для `B`, `N`, `C`, `S`, `A`, `TA`, `M`, `P`, пустой строки и диагностик;
- для пользовательского договора `DRAW` добавлены строковая переменная и составное строковое выражение, но это было сделано через обычный синтаксис Axiom: `DIM commands AS STRING` и `STR(...)`, без ввода QuickBASIC-суффиксов `$`;
- `DRAW` добавлен в IDE highlighting, а заодно локально синхронизирован долг по `REPLACEMANY`, чтобы не оставлять заведомо неполный словарь подсветки;
- добавлены runnable sample в `samples/operators/draw/`: `square.axm`, `octagon.axm`, `blind_and_return.axm`, `rotated_spokes.axm`, `paint_segments.axm`, `dynamic_string.axm`;
- manual обновлён: в `docs/manual/graphics-primitives.html` добавлен отдельный раздел `DRAW`, а в `docs/manual/scripts/search-index.js` добавлена поисковая карточка `DRAW`;
- `docs/SPECIFICATION3.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы под полный договор `DRAW`.

Изменённые файлы:
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Conversion.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Execution/GraphicsDrawException.cs`
- `src/Axiom.Runtime/Execution/GraphicsDrawCommand.cs`
- `src/Axiom.Runtime/Execution/GraphicsDrawParser.cs`
- `src/Axiom.Runtime/Execution/GraphicsDrawExecutor.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/draw/square.axm`
- `samples/operators/draw/octagon.axm`
- `samples/operators/draw/blind_and_return.axm`
- `samples/operators/draw/rotated_spokes.axm`
- `samples/operators/draw/paint_segments.axm`
- `samples/operators/draw/dynamic_string.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore` — успешно после доводки строковых форм `DRAW` и составного выражения с `STR(...)`;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` — успешно после стабилизации ожиданий по `DRAW`;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore` — `DRAW`-часть подсветки закрыта, но прогон всё ещё падает на несвязанных проверках `MainWindow.xaml` и на старом долге словаря built-in, не открытом этой задачей;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` — прогон остаётся нестабилен на старых `PAINT` sample в общем positive-наборе: `pattern_repaint.axm`, `step_default_fill.axm`, `fish_fill.axm` сообщают `MB3010`, то есть это внешний графический acceptance-хвост вне текущего `DRAW`.

Решение:
- считать `DRAW` проведённым по основному коду и собственным parser/runtime/bytecode/sample/manual слоям, но не объявлять задачу закрытой на 100%, пока не будет отдельно разобрана внешняя нестабильность `Axiom.Ide.Tests` и общего acceptance-набора `Axiom.Execution.Tests`.

Следующий точный шаг:
- добить приёмочный хвост `DRAW`: повторить `Axiom.Ide.Tests` и `Axiom.Execution.Tests` после финальной синхронизации manual/scripts, затем выполнить `node --check` для manual-скриптов и вручную открыть новые sample `samples/operators/draw/` в `Axiom.Ide`.

## Session 854 - 2026-05-15 07:48 - исправлены регрессии DRAW в manual и acceptance-проверках

Что сделано:
- перечитан активный проектный контекст и подтверждено, что текущая задача не открывает новый оператор, а исправляет регрессии после полного прохода по `DRAW`;
- из активных материалов убраны ложные QuickBASIC-формы с суффиксом `$`: в manual больше не осталось пользовательского упоминания `STR$`, а строковые примеры `DRAW` оставлены только в синтаксисе Axiom с `DIM commands AS STRING` и `STR(...)`;
- `docs/manual/graphics-primitives.html` восстановлен в нормальной UTF-8-кодировке после битой ревизии и повторно синхронизирован: сохранён раздел `DRAW`, восстановлены ссылки на runnable sample `samples/operators/draw/`;
- `docs/manual/scripts/search-index.js` подтверждён в исправной UTF-8-кодировке и проходит синтаксическую проверку `node --check`;
- найден и закрыт реальный тестовый долг в `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`: прежняя проверка смешивала сохранение текущей графической позиции с незамкнутой заливкой и давала ложное ожидание, теперь тест использует замкнутый контур и проверяет именно host-договор графики;
- найден и закрыт реальный acceptance-долг в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`: общий positive-аудит не распознавал `samples/operators/paint/` и `samples/operators/draw/` как графические sample и запускал их без графического input-provider, из-за чего появлялись ложные `MB3010`;
- повторные прогоны подтвердили, что `DRAW`-sample и графические `PAINT`-sample снова проходят в `Axiom.Execution.Tests`.

Изменённые файлы:
- `docs/manual/graphics-primitives.html`
- `docs/manual/data-types-conversions.html`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверки:
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore` — после исправления графического host-теста падают только два несвязанных XAML-ожидания: `MainWindowXaml_UsesFocusedOutputContextMenu` и `MainWindowXaml_WiresEditorContextMenuAndFindCommands`;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` — успешно после исправления графического audit-provider;
- `node --check docs\manual\scripts\manual.js` — успешно;
- `node --check docs\manual\scripts\search-index.js` — успешно.

Решение:
- считать регрессию с ложными `$`-формами исправленной;
- считать порчу кодировки manual исправленной для затронутых активных файлов `graphics-primitives.html` и `search-index.js`;
- считать `DRAW` снова синхронизированным с manual, sample и acceptance-путём;
- не смешивать оставшиеся падения `Axiom.Ide.Tests` по `MainWindow.xaml` с задачей `DRAW`, потому что это отдельный IDE-хвост вне графического оператора.

Следующий точный шаг:
- отдельно разобрать и починить два оставшихся XAML-теста в `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`, не трогая уже закрытый договор `DRAW`;
- после этого, если потребуется, провести ручную приёмку `samples/operators/draw/` в `Axiom.Ide` и зафиксировать её отдельной записью.

## Session 855 - 2026-05-15 09:38 - проведён приёмочный аудит DRAW по семантике

Что сделано:
- перечитаны обязательные активные документы проекта, manual по графике и runtime-реализация `DRAW`, после чего подтверждено, что текущая задача не открывает новый оператор, а доводит уже реализованный `DRAW` до честного приёмочного состояния;
- подтверждено, что `STR` уже является штатной встроенной функцией Axiom и в активных материалах `DRAW` больше нет ложных QuickBASIC-форм `STR$`: примеры остаются только в синтаксисе Axiom с `STR(...)`;
- в `GraphicsDrawParser` закрыта реальная семантическая ошибка: `B` и `N` больше не съедаются командами `C`, `S`, `A`, `TA`, `P` и `X`, а дают явную runtime-диагностику `Модификатор B/N в DRAW может применяться только к команде движения или M.`;
- в `GraphicsDrawExecutor` выровнен договор `P f`: форма `DRAW "P4"` теперь создаёт `GraphicsPaintRequest` с границей по умолчанию, равной цвету заливки, как и обычный сплошной `PAINT`;
- в `GraphicsDrawExecutor` закрыт хвост по сырым переполнениям: directional move, относительный `M` и очень большие цвета теперь дают контролируемые `GraphicsDrawException`, а не выпускают наружу `OverflowException` или другие сырые исключения;
- существенно расширены parser-, binder-, interpreter- и `BytecodeVm`-тесты для `DRAW`: покрыты строгая семантика `B/N`, `P` с границей по умолчанию, `A0..A3`, `TA90`, `TA-90`, `TA0`, приоритет последней команды `A/TA`, current graphics position, unsupported `X`, overflow и строковые edge-case формы;
- добавлен runnable sample `samples/operators/draw/paint_default_border.axm` и он включён в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` как часть graphics-aware acceptance-пути `DRAW`;
- `docs/SPECIFICATION3.md`, `docs/manual/graphics-primitives.html`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы с фактическим итогом этого аудита.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsDrawParser.cs`
- `src/Axiom.Runtime/Execution/GraphicsDrawExecutor.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/draw/paint_default_border.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore` — падают только два несвязанных XAML-теста `MainWindowXaml_UsesFocusedOutputContextMenu` и `MainWindowXaml_WiresEditorContextMenuAndFindCommands`; к `DRAW` они не относятся;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` — успешно, `samples/operators/draw/` проходят через graphics-aware provider, ложных `MB3010` для valid `DRAW` sample нет;
- `node --check docs\manual\scripts\manual.js` — успешно;
- `node --check docs\manual\scripts\search-index.js` — успешно.

Решение:
- считать приёмочный хвост `DRAW` по семантике закрытым: подтверждены strict `B/N`, выровнен `P` default border, закрыты controlled overflow, усилены interpreter/VM tests и acceptance-путь;
- не считать оставшиеся два падения `Axiom.Ide.Tests` проблемой `DRAW`, потому что они проверяют `MainWindow.xaml`, а не parser/runtime/manual/sample-договор графического оператора.

Следующий точный шаг:
- отдельно разобрать два оставшихся XAML-теста в `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`, не смешивая их с уже доведённым `DRAW`;
- если потребуется отдельная ручная приёмка, открыть в `Axiom.Ide` sample `samples/operators/draw/paint_default_border.axm` вместе с остальными `samples/operators/draw/`.

## Session 856 - 2026-05-15 09:54 - исправлен демонстрационный sample DRAW P

Что сделано:
- пользовательский ручной запуск `samples/operators/draw/paint_default_border.axm` показал заливку всего экрана, и это было сверено с уже зафиксированным договором `DRAW "P4"`;
- подтверждено, что runtime `DRAW` работает корректно: форма `P4` берёт границу по умолчанию из цвета заливки, поэтому рамка другого цвета не обязана ограничивать flood-fill;
- исправлен именно sample `samples/operators/draw/paint_default_border.axm`: рамка переведена в цвет `4`, чтобы пример визуально демонстрировал bounded-fill внутри рамки того же цвета;
- уточнена подпись к sample в `docs/manual/graphics-primitives.html`;
- повторно выполнен `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore`, acceptance-путь снова успешен.

Изменённые файлы:
- `samples/operators/draw/paint_default_border.axm`
- `docs/manual/graphics-primitives.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` — успешно.

Решение:
- не менять код runtime `DRAW` по итогам ручного запуска этого sample;
- считать найденную проблему ошибкой демонстрационного примера, а не семантики `DRAW`.

Следующий точный шаг:
- при необходимости повторно открыть `samples/operators/draw/paint_default_border.axm` в `Axiom.Ide` и визуально подтвердить bounded-fill внутри рамки цвета `4`.

## Session 857 - 2026-05-15 09:59 - закрыт пограничный overflow в RoundToLong для DRAW

Что сделано:
- по дополнительной проверке `RoundToLong` добавлены interpreter- и `BytecodeVm`-тесты на граничные формы `DRAW "R9223372036854775807"` и `DRAW "L9223372036854775808"`;
- тесты подтвердили реальный хвост: правая граница выпускала сырой `OverflowException` из `checked((long)Math.Round(...))`, а левая граница проходила через пограничное округление без controlled-диагностики;
- в `GraphicsDrawExecutor.RoundToLong` защита усилена: проверка теперь идёт по уже округлённому значению, границы `long.MaxValue` и `long.MinValue` считаются переполнением координат, а сам cast дополнительно обёрнут в `try/catch` с переводом `OverflowException` в `GraphicsDrawException`;
- повторный `Axiom.Runtime.Tests` подтвердил controlled `MB3007` для обоих граничных сценариев без сырого `OverflowException`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsDrawExecutor.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` — успешно.

Решение:
- считать хвост `RoundToLong` для `DRAW` закрытым;
- дополнительной правки interpreter или `BytecodeVm` поверх общего runtime-исполнителя не требуется.

Следующий точный шаг:
- новых хвостов по `DRAW` больше не открывать без отдельного подтверждённого сценария; оставшиеся несвязанные падения по-прежнему локализованы в XAML-тестах IDE.

## Session 858 - 2026-05-15 10:20 - синхронизирована нормализация цветов DRAW с общим графическим договором

Что сделано:
- проведён точечный аудит нормализации цветов `DRAW` против активного графического договора проекта и подтверждено расхождение: `GraphicsDrawExecutor.NormalizeColor` использовал signed modulo, тогда как manual, `ScreenFramebuffer.NormalizeColorIndex` и `GraphicsFloodFill.NormalizeColor` уже работали по правилу `ABS(color) MOD 256`;
- принято единое проектное решение в пользу общего графического договора: `DRAW` не должен отличаться от `LINE`, `PAINT`, framebuffer и flood-fill по приведению цветовых индексов;
- в `GraphicsDrawExecutor.NormalizeColor(long value)` signed modulo заменён на нормализацию `Math.Abs(value) % 256`, поэтому `-1 -> 1`, `-2 -> 2`, `-255 -> 255`;
- обновлены interpreter- и `BytecodeVm`-тесты `DRAW`: теперь `DRAW "C-1 R1"` использует цвет `1`, а `DRAW "P-1,-2"` создаёт `fill=1`, `border=2`; сценарии `DRAW "P4"` и `DRAW "P4,15"` сохранены рабочими;
- `docs/SPECIFICATION3.md` и `docs/manual/graphics-primitives.html` уточнены так, чтобы `DRAW` явно следовал общему правилу `ABS(color) MOD 256`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsDrawExecutor.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore` — по-прежнему падают только два несвязанных XAML-теста `MainWindowXaml_UsesFocusedOutputContextMenu` и `MainWindowXaml_WiresEditorContextMenuAndFindCommands`.

Решение:
- считать нормализацию цветов `DRAW` синхронизированной с общим графическим договором проекта;
- не открывать отдельное исключение для `DRAW C/P` и не заводить скрытую несовместимость с `LINE` и `PAINT`.

Следующий точный шаг:
- новых хвостов по `DRAW` не открывать без нового подтверждённого сценария; оставшиеся падающие проверки по-прежнему относятся к XAML-слою IDE, а не к графическому договору `DRAW`.

## Session 859 - 2026-05-15 10:35 - базовый fallback DrawLine и DrawCircle добавлен в IGraphicsHost

Что сделано:
- перечитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md`, после чего задача локализована в общем host-договоре графики;
- подтверждено, что в `IGraphicsHost` уже был fallback только для `Paint`, тогда как default-методы `DrawLine` и `DrawCircle` оставались пустыми и требовали специального host-а даже для базовых графических инструкций;
- в `src/Axiom.Runtime/Host/IGraphicsHost.cs` добавлены стандартные реализации `DrawLine` и `DrawCircle`, работающие через общие внутренние алгоритмы рисования и `SetPixel`/`SetGraphicsCurrentPosition`;
- та же fallback-ветка покрывает `GraphicsLineKind.Normal`, `Box` и `FilledBox`, а также дуги и эллипсы `CIRCLE` с `relativeCenter`, `startAngle`, `endAngle`, `aspect`, `drawStartRadius` и `drawEndRadius`;
- в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` добавлены регрессионные проверки, которые запускают `LINE` и `CIRCLE` через минимальный host без собственной реализации этих методов и подтверждают, что графические инструкции теперь работают через общий default-host путь.

Изменённые файлы:
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawLineFallbackWorksWithoutSpecialGraphicsHost|FullyQualifiedName~DrawCircleFallbackWorksWithoutSpecialGraphicsHost|FullyQualifiedName~GraphicsStepAndPointState"` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests"` — успешно.

Решение:
- считать базовый графический fallback host-слоя выровненным: простые и тестовые реализации `IGraphicsHost` больше не обязаны дублировать алгоритмы `LINE` и `CIRCLE`, если им достаточно `SetPixel`/`GetPixel`;
- пользовательский договор языка не изменился: это инфраструктурное усиление существующего графического поведения, а не новый оператор или новая форма языка.

Следующий точный шаг:
- вернуться к уже выделенному отдельному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`, где остаются несвязанные XAML-ожидания;
- новых специальных графических хвостов не открывать без отдельного подтверждённого сценария.

## Session 860 - 2026-05-15 11:06 - DrawLineCore защищён от переполнения при удвоении error

Что сделано:
- проверены оба активных пути алгоритма линии Брезенхэма: `ProgramConsoleService.DrawLineCore` и общий fallback `IGraphicsHost.DrawLineCore`; в обоих местах подтверждён риск переполнения `long` на выражении `2 * error`;
- вместо локальной дублирующей защиты введён общий helper `GraphicsLineMath.SaturatingDouble(long)` в `src/Axiom.Runtime/Execution/GraphicsLineMath.cs`;
- `ProgramConsoleService.DrawLineCore` и default-реализация `IGraphicsHost.DrawLineCore` переведены на saturating-удвоение, которое:
  - возвращает `long.MaxValue`, если обычное удвоение переполнило бы верхнюю границу;
  - возвращает `long.MinValue`, если обычное удвоение переполнило бы нижнюю границу;
  - иначе возвращает обычное `value * 2`;
- адресные тесты обновлены так, чтобы проверять именно новую защиту без искусственного многомиллиардного прогона линии:
  - в `Axiom.Execution.Tests` подтверждён helper и сохранён fallback-путь `LINE`/`CIRCLE` через минимальный host;
  - в `ProgramConsoleServiceTests` добавлена прямая проверка saturating-поведения helper и повторно подтверждён весь профильный набор `ProgramConsoleService`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsLineMath.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawLineFallbackSurvivesErrorDoublingOverflow|FullyQualifiedName~DrawLineFallbackWorksWithoutSpecialGraphicsHost|FullyQualifiedName~DrawCircleFallbackWorksWithoutSpecialGraphicsHost"` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests"` — успешно.

Решение:
- считать переполнение на `2 * error` закрытым одинаково для IDE-host и для общего fallback-графического host-слоя;
- не переводить `error` на `double`, потому что saturating-арифметика на `long` сохраняет точный дискретный алгоритм линии и не меняет существующее поведение на обычных координатах.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- графический слой дальше трогать только при новом подтверждённом сценарии.

## Session 861 - 2026-05-15 11:18 - DrawCircleCore переведён на прямой rasterizer окружности и эллипса

Что сделано:
- проверена текущая реализация `ProgramConsoleService.DrawCircleCore` и подтверждено, что она строила окружность и эллипс через цепочку коротких отрезков с числом шагов, растущим пропорционально радиусу;
- для сохранения одинакового поведения IDE-host и общего fallback-контракта вынесен общий rasterizer `GraphicsEllipseRasterizer` в `src/Axiom.Runtime/Execution/GraphicsEllipseRasterizer.cs`;
- новый rasterizer строит окружность и эллипс напрямую по пикселям через midpoint-style алгоритм по симметричным точкам вместо полилинии из множества `DrawLineCore`-сегментов;
- `ProgramConsoleService.DrawCircleCore` и default-реализация `IGraphicsHost.DrawCircle` переведены на общий rasterizer;
- логика дуг, `STEP`, `aspect`, `drawStartRadius` и `drawEndRadius` сохранена: радиальные линии по-прежнему рисуются через `DrawLineCore`, а стартовая и конечная точки дуги вычисляются тем же геометрическим договором.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsEllipseRasterizer.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawCircleFallbackWorksWithoutSpecialGraphicsHost"` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests"` — успешно.

Решение:
- считать графический договор `CIRCLE` неизменным, а замену полилинейного построения на прямой rasterizer — внутренней оптимизацией реализации;
- не дублировать circle/ellipse-алгоритм между IDE и fallback-host, чтобы дальнейшие правки точности и производительности не расходились по двум разным копиям кода.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- новых графических оптимизаций не открывать без отдельного подтверждённого сценария или профиля.

## Session 862 - 2026-05-15 11:30 - лимит итераций CIRCLE привязан к экрану и длине дуги

Что сделано:
- в общий `GraphicsEllipseRasterizer` добавлен динамический лимит итераций для больших окружностей и эллипсов, чтобы построение не уходило в лишние проходы на радиусах, многократно превышающих экран;
- лимит теперь вычисляется через оценку периметра эллипса, долю дуги `sweep` и верхнюю границу по периметру экрана, поэтому полный круг и дуга используют один и тот же общий предохранитель без возврата к старому шаговому построению;
- ограничение применено прямо в обоих циклах rasterizer, а `ProgramConsoleService.DrawCircleCore` и fallback `IGraphicsHost.DrawCircle` переведены на новую сигнатуру общего вызова с размерами экрана;
- в `ProgramConsoleServiceTests` добавлена проверка `DrawCircle_IterationLimitTracksScreenPerimeter`, подтверждающая, что для очень большого радиуса лимит упирается в экранный периметр, а не растёт вместе с радиусом.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsEllipseRasterizer.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests"` — успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawCircleFallbackWorksWithoutSpecialGraphicsHost"` — успешно.

Решение:
- считать этот шаг внутренней оптимизацией rasterizer-а без изменения пользовательского договора `CIRCLE`;
- держать ограничение итераций общим для IDE-host и fallback-host, чтобы оба графических пути не расходились по производительности и предельным случаям.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- новые графические правки открывать только при появлении отдельного подтверждённого сценария.

## Session 863 - 2026-05-15 12:05 - CIRCLE проверяет конечность start и end

Что сделано:
- перечитаны обязательные активные документы проекта и продолжен именно текущий графический шаг, а не следующий общий хвост плана;
- в `src/Axiom.Runtime/Execution/GraphicsCircleValidation.cs` добавлен общий договор аргументов `CIRCLE`: `radius` должен быть конечным и неотрицательным, `aspect` — конечным и положительным, `start` и `end` при наличии тоже должны быть конечными числами;
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` переведены на новый валидатор и теперь сообщают `MB3007` не только для `radius/aspect`, но и для нечисловых или бесконечных `start/end`;
- `src/Axiom.Runtime/Host/IGraphicsHost.cs` и `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs` получили ту же защиту на host-уровне: невалидный внутренний вызов `DrawCircle(...)` теперь безопасно игнорируется вместо попадания в геометрию с `NaN` и бесконечностями;
- добавлены и подтверждены адресные тесты в `tests/Axiom.Runtime.Tests`, `tests/Axiom.Execution.Tests` и `tests/Axiom.Ide.Tests`;
- повреждённые строковые фрагменты в `Interpreter.Graphics.cs` и `BytecodeVm.Graphics.cs`, появившиеся при неудачной промежуточной правке, вычищены: итоговый diff по этим файлам содержит только нужное расширение валидации `CIRCLE`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsCircleValidation.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~CircleValidationRejectsNonFiniteAngles|FullyQualifiedName~CircleSupportsStepArcAndAspect|FullyQualifiedName~CompiledCircleValidationRejectsNonFiniteAngles|FullyQualifiedName~CompiledCircleSupportsStepArcAndAspect"` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_IgnoresNonFiniteAngles|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_RasterizesArcAndAspect|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius"` — успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawCircleFallbackWorksWithoutSpecialGraphicsHost|FullyQualifiedName~DrawCircleFallbackIgnoresNonFiniteAngles"` — успешно.

Решение:
- считать это внутренним усилением проверок `CIRCLE`, а не изменением пользовательского синтаксиса или новой формы языка;
- отдельный runnable negative sample для `NaN`/`Infinity` не добавлять, пока такой сценарий не достижим честным пользовательским кодом Axiom и не подтверждён отдельной языковой возможностью.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- графический слой дальше не трогать без нового подтверждённого сценария.

## Session 867 - 2026-05-15 12:34 - XAML-ожидания EditorFeatureTests синхронизированы с текущим MainWindow

Что сделано:
- перечитан хвост `tests\Axiom.Ide.Tests\EditorFeatureTests.cs` и сверены проблемные ожидания с актуальными `src\Axiom.Ide\MainWindow.xaml` и `src\Axiom.Ide\MainWindow.xaml.cs`;
- в тесте `MainWindowXaml_WiresEditorContextMenuAndFindCommands()` ожидание иконки обновлено до фактического pack URI `Icon="/Axiom.Ide;component/Assets/axiom.ico"`;
- в тесте `MainWindowXaml_UsesFocusedOutputContextMenu()` убрано устаревшее требование наличия `PreviewMouseRightButtonDown/Up` прямо в XAML, потому что эти обработчики сейчас подключаются через `InitializeOutputMouseInputHandlers()` в code-behind;
- подтверждено, что `OutputTextBox`, `OnOutputTextBoxContextMenuOpening`, `OutputContextMenu`, `OutputCopyMenuItem`, `OutputClearMenuItem` и остальной пользовательский контракт output menu остаются на месте.

Изменённые файлы:
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~EditorFeatureTests"` — успешно, `60/60`.

Решение:
- считать этот шаг исправлением устаревших строковых IDE-тестов, а не причиной для правки `MainWindow.xaml`;
- графический слой в этом шаге не менять, потому что подтверждённый красный сценарий относился только к XAML-ожиданиям тестов.

Следующий точный шаг:
- ждать нового подтверждённого сценария по IDE- или языковому слою вне графического хвоста `EditorFeatureTests`;
- графический слой дальше не трогать без нового подтверждённого сценария.

## Session 866 - 2026-05-15 12:28 - документация по флагам графических операторов собрана в отдельный developer-файл

Что сделано:
- перечитаны актуальные разделы `docs/manual/graphics-primitives.html`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md` и `README.md`, чтобы не плодить второй противоречащий источник по `LINE`, `CIRCLE` и `PAINT`;
- создан новый файл `docs/GRAPHICS_OPERATORS_FLAGS.md` с разработческим описанием:
  - пользовательских форм операторов;
  - значений по умолчанию для цветов и необязательных параметров;
  - правил `STEP`, относительных координат и обновления текущей графической позиции;
  - битовых флагов `LINE`, `CIRCLE` и `PAINT` в bytecode-слое;
  - соответствия между этими флагами, helper-структурами runtime и host-вызовами;
  - списка файлов и типов, которые нужно менять вместе при развитии графического договора;
- `README.md` дополнен ссылкой на новый developer-документ;
- `docs/ARCHITECTURE.md` дополнен указанием, что разработческий договор графического runtime-слоя ведётся в `docs/GRAPHICS_OPERATORS_FLAGS.md`.

Изменённые файлы:
- `docs/GRAPHICS_OPERATORS_FLAGS.md`
- `docs/ARCHITECTURE.md`
- `README.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- код и тесты в этом шаге не менялись, поэтому отдельный прогон не запускался;
- документ сверён с актуальными `docs/SPECIFICATION3.md`, `docs/manual/graphics-primitives.html`, `GraphicsInstructionArguments.cs`, `GraphicsPaintRequest.cs` и `IGraphicsHost.cs`.

Решение:
- считать `docs/GRAPHICS_OPERATORS_FLAGS.md` главным developer-ориентиром по внутренним флагам и параметрам графических операторов;
- сохранять разделение ролей: manual объясняет пользовательское поведение, спецификация фиксирует проектный договор, а новый файл помогает сопровождать bytecode/runtime/helper-слой.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- графический слой дальше не трогать без нового подтверждённого сценария.

## Session 865 - 2026-05-15 12:23 - графические тесты добраны по граничным сценариям

Что сделано:
- перечитаны обязательные активные документы проекта и уточнено текущее состояние графического покрытия: базовые проверки `LINE`, `CIRCLE` и `PAINT` уже были, но не хватало явных сценариев на клиппинг, очень большой радиус, старт заливки вне экрана и совместную проверку `STEP` + обновления текущей позиции;
- в `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs` добавлены проверки:
  - `DrawLine_ClipsPixelsOutsideScreenAndKeepsEndPosition()`;
  - `DrawCircle_LargeRadiusOutsideScreenStillUpdatesCenterPosition()`;
  - `Paint_StartOutsideScreenLeavesFramebufferAndPositionUnchanged()` в терминах фактического текущего договора, где позиция после `Paint(x, y, ...)` становится стартовой точкой вызова;
- существующая проверка `DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius()` усилена явной проверкой итоговой позиции центра;
- в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` добавлен сквозной сценарий `Run_WithBytecodeMode_GraphicsOperatorsMaintainPositionAcrossStepArcAndPaint()`, который подтверждает совместную работу `LINE STEP`, `CIRCLE STEP`, `PAINT STEP` и `POINT(0/1)` в `BytecodeVm`;
- по результатам прогона ожидания тестов приведены к реальному зафиксированному договору:
  - после `PAINT STEP (0, 0)` текущая графическая позиция становится стартовой точкой заливки;
  - для очень большого `CIRCLE` надёжно фиксируется отсутствие сбоя и обновление центра, а не обязательное попадание в конкретные граничные пиксели.

Изменённые файлы:
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.DrawLine_ClipsPixelsOutsideScreenAndKeepsEndPosition|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_LargeRadiusOutsideScreenStillUpdatesCenterPosition|FullyQualifiedName~ProgramConsoleServiceTests.Paint_StartOutsideScreenLeavesFramebufferAndPositionUnchanged|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius|FullyQualifiedName~ProgramConsoleServiceTests.Paint_FillsRegionWithoutRecoloringBorder"` — успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Run_WithBytecodeMode_GraphicsOperatorsMaintainPositionAcrossStepArcAndPaint|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForGraphicsStepAndPointState|FullyQualifiedName~Run_WithBytecodeMode_DrawLineFallbackWorksWithoutSpecialGraphicsHost|FullyQualifiedName~Run_WithBytecodeMode_DrawCircleFallbackWorksWithoutSpecialGraphicsHost"` — успешно.

Решение:
- считать этот шаг чистым расширением модульного и сквозного графического покрытия без изменения пользовательского договора языка;
- опираться дальше на уже подтверждённый тестами фактический договор `PAINT` по текущей позиции, а не на предположение, что позиция обязана сохраняться прежней при старте вне экрана.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- графический слой дальше не трогать без нового подтверждённого сценария.

## Session 864 - 2026-05-15 12:16 - разбор LINE/CIRCLE объединён, PAINT переведён на scanline flood fill

Что сделано:
- перечитаны обязательные активные документы проекта и затем локализованы два графических хвоста: дублирующий разбор аргументов `LINE`/`CIRCLE` в интерпретаторе и `BytecodeVm`, а также внутренний алгоритм `GraphicsFloodFill`;
- добавлен общий helper `src/Axiom.Runtime/Execution/GraphicsInstructionArguments.cs`, который:
  - декодирует флаги байткодных инструкций `LINE` и `CIRCLE`;
  - нормализует координаты `LINE` с учётом `STEP`, относительного начала и относительного конца;
  - нормализует углы `CIRCLE` и признаки `drawStartRadius`/`drawEndRadius`;
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` переведён на новый helper и больше не держит отдельный локальный разбор тех же флагов и координат;
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs` переведён на тот же helper, поэтому интерпретатор и `BytecodeVm` теперь собирают одинаковые аргументы `LINE` и `CIRCLE` одним и тем же кодом;
- `src/Axiom.Runtime/Execution/GraphicsFloodFill.cs` переведён с покомпонентного обхода через `Stack + HashSet visited` на итеративный scanline flood fill по горизонтальным сегментам, что уменьшает лишние проверки и память на больших заливках;
- в `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs` добавлена адресная проверка `Paint_FillsLargeRectangleInteriorWithIterativeScanlinePath()` для крупной прямоугольной области.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsInstructionArguments.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/GraphicsFloodFill.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_CircleValidationRejectsNonFiniteAngles|FullyQualifiedName~Execute_CompiledCircleValidationRejectsNonFiniteAngles|FullyQualifiedName~Execute_PaintFillsBoundedRegion|FullyQualifiedName~Execute_CompiledPaintFillsBoundedRegion|FullyQualifiedName~Execute_PaintPatternAndBackgroundPatternRepaintRegion|FullyQualifiedName~Execute_CompiledPaintPatternAndBackgroundPatternRepaintRegion"` — успешно;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.Paint_FillsLargeRectangleInteriorWithIterativeScanlinePath|FullyQualifiedName~ProgramConsoleServiceTests.Paint_FillsRegionWithoutRecoloringBorder|FullyQualifiedName~ProgramConsoleServiceTests.Paint_PatternFillAndBackgroundPatternRepaintRegion|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_RasterizesArcAndAspect|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius|FullyQualifiedName~ProgramConsoleServiceTests.DrawCircle_IgnoresNonFiniteAngles"` — успешно.

Решение:
- считать это внутренней оптимизацией и устранением дублирования, а не новым пользовательским поведением языка;
- сохранить единый путь разбора графических аргументов в runtime-слое, чтобы дальнейшие правки `LINE` и `CIRCLE` не расходились между интерпретатором и `BytecodeVm`.

Следующий точный шаг:
- вернуться к уже выделенному несвязанному хвосту `tests\Axiom.Ide.Tests\EditorFeatureTests.cs`;
- графический слой дальше не трогать без нового подтверждённого сценария.

## Session 868 - 2026-05-15 13:07 - графический договор LINE и CIRCLE досинхронизирован с документацией

Что сделано:
- перечитаны активные документы проекта и сверены свежие графические изменения по коду, тестам и sample: насыщающее сложение в Брезенхэме, координатный диапазон `-1000000000..1000000000`, ограничение `aspect <= 1000000`, клиппинг `GraphicsEllipseRasterizer` и правило полного контура для `CIRCLE ..., 0, 2 * PI`;
- `docs/SPECIFICATION3.md` уточнён по двум пользовательским договорам:
  - для `LINE` явно зафиксирован допустимый диапазон координат после `STEP`;
  - для `CIRCLE` зафиксированы диапазон координат центра, верхняя граница `aspect`, полная окружность при совпавших после нормализации углах и расширенный состав случаев `MB3007`;
- `docs/manual/graphics-primitives.html` обновлён под пользовательское описание новых ограничений и ожидаемого поведения: диапазон координат `LINE`/`CIRCLE`, полный оборот через `0` и `2 * PI`, правило радиальных линий при отрицательных углах и ссылка на новый runnable sample `samples/graphics/primitives/circle_full_turn.axm`;
- `docs/GRAPHICS_OPERATORS_FLAGS.md` синхронизирован с фактическим helper-путём `TryCreateLine(...)` / `TryCreateCircle(...)`, абсолютным центром `CIRCLE` после нормализации и новыми ограничениями по координатам и `aspect`;
- `docs/BYTECODE_COVERAGE.md` дополнен отдельной записью про этот графический срез, чтобы в активном реестре не терялись новые тесты, sample и негативный паритет.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/manual/graphics-primitives.html`
- `docs/GRAPHICS_OPERATORS_FLAGS.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln --no-restore` — успешно, `0` ошибок и `0` предупреждений.

Решение:
- считать графический хвост по переполнениям, координатным ограничениям и полному обороту `CIRCLE` закрытым и синхронизированным по всем обязательным документам;
- не открывать новый графический подшаг без очередного подтверждённого пользовательского сценария.

Следующий точный шаг:
- выполнить контрольный `git status` и зафиксировать фактический состав изменённых файлов в итоговом отчёте;
- затем возвращаться только к следующему подтверждённому сценарию вне уже закрытого графического хвоста.

## Session 869 - 2026-05-15 18:15 - VIEW доведён до рабочего договора

Что сделано:
- перечитаны обязательные активные документы проекта и подтверждён текущий фокус только на графическом слое;
- ранее внесённый каркас `VIEW` доведён до рабочего пользовательского договора по parser, semantics, interpreter, `BytecodeVm`, execution-пути и IDE-host;
- добавлены целевые проверки на `VIEW` reset, `VIEW SCREEN`, относительный `VIEW`, rebasing текущей графической позиции и приёмочный путь через runnable sample `samples/operators/view/screen_and_relative.axm`;
- manual и проектные документы синхронизированы с фактическим поведением: добавлена страница `docs/manual/view.html`, обновлены графический обзор, поиск по manual, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`;
- в `docs/WORKLOG4.md` зафиксирован итог закрытия графического шага `VIEW`.

Изменённые файлы:
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/view/screen_and_relative.axm`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `docs/manual/view.html`
- `docs/manual/graphics.html`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln --no-restore` — успешно;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-build` — успешно;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-build` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-build --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForViewScreenAndRelativeCoordinates|FullyQualifiedName~ScreenViewAcceptanceSample_RunsOnBytecodeVm|DisplayName~screen_and_relative"` — успешно;
- `node --check docs/manual/scripts/manual.js` — успешно;
- `node --check docs/manual/scripts/search-index.js` — успешно;
- полный `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` в этой среде по-прежнему упирается в посторонний файловый sample `samples/smoke/engine-compare/file_management_roundtrip.axm` с `Access denied` в `.artifacts/manual-checks`; к договору `VIEW` это не относится.

Решение:
- считать графический шаг `VIEW` закрытым по критерию кода, автотестов, runnable sample, manual и проектной документации;
- не возвращаться к `LINE`, `CIRCLE`, `PAINT` и `DRAW` без нового подтверждённого сценария;
- не смешивать следующий графический шаг с `PMAP`, `GET` и `PUT`, пока не будет отдельного подтверждённого договора именно для `WINDOW`.

Следующий точный шаг:
- если фокус остаётся только на графике, открыть отдельный договор оператора `WINDOW` как следующий шаг поверх уже закрытого `VIEW`;
- до этого не расширять `VIEW` и не возвращаться к уже закрытым графическим операторам без нового подтверждённого сценария.

## Session 870 - 2026-05-16 17:00 - WINDOW доведён до рабочего договора

Что сделано:
- перечитаны обязательные активные документы проекта и подтверждено, что после закрытого `VIEW` следующим графическим шагом остаётся отдельный оператор `WINDOW`;
- ранее внесённый код `WINDOW` доведён до собираемого состояния: исправлена общая координатная математика `GraphicsCoordinateSpaceMath`, а тестовые host'ы `BytecodeVmTests`, `ExecutionPipelineTests` и `AcceptanceProgramTests` синхронизированы с новым host-договором `WindowEnabled` / `SetWindow(...)` / `ResetWindow()` и абсолютными пиксельными операциями;
- в runtime и `BytecodeVm` подтверждён пользовательский договор `WINDOW`: формы reset, `WINDOW SCREEN`, декартова форма без `SCREEN`, пересчёт `POINT(0..3)`, rebasing текущей позиции при смене `VIEW` / `WINDOW` и проектное решение Axiom, что `DRAW` использует ту же логическую систему окна и общую текущую графическую позицию;
- добавлены целевые автотесты в `Axiom.Parsing.Tests`, `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests`, `Axiom.Ide.Tests` и `Axiom.Execution.Tests`;
- добавлен runnable sample `samples/operators/window/cartesian_and_screen.axm`;
- в manual добавлена новая страница `docs/manual/window.html`, а также синхронизированы `docs/manual/graphics.html`, `docs/manual/view.html`, `docs/manual/graphics-primitives.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- `docs/ARCHITECTURE.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` обновлены под фактический итог шага `WINDOW`.

Изменённые файлы:
- `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs`
- `src/Axiom.Runtime/Host/GraphicsHostDrawing.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/GraphicsCoordinateValidation.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/window/cartesian_and_screen.axm`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/manual/window.html`
- `docs/manual/graphics.html`
- `docs/manual/view.html`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln --no-restore` — успешно;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "FullyQualifiedName~Parse_Window"` — успешно;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~Bind_Window"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~WindowSupportsDrawPointSelectorsAndViewRebase"` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.Window_"` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Window"` — успешно;
- `node --check docs/manual/scripts/manual.js` — ожидается в финальном прогоне вместе с `search-index.js`.

Решение:
- считать графический шаг `WINDOW` закрытым по критерию кода, автотестов, runnable sample, manual и проектной документации;
- держать `WINDOW` отдельным логическим слоем поверх `VIEW`, а `DRAW` — подчинённым той же общей текущей графической позиции, не возвращаясь к QuickBASIC-идее отдельной внутренней координатной модели только для `DRAW`.

Следующий точный шаг:
- если продолжаем именно графическую ветку, открыть отдельный договор `PMAP` поверх уже закрытых `VIEW` и `WINDOW`;
- `GET` и `PUT` пока не трогать и не смешивать с этим шагом.

## Session 871 - 2026-05-16 23:21 - PMAP доведён до рабочего договора

Что сделано:
- перечитан и зафиксирован договор `PMAP` по книге Зельднера и поверх уже закрытого проектного договора `VIEW` + `WINDOW`;
- `PMAP` добавлен как встроенная функция `DOUBLE <- (DOUBLE coordinate, INTEGER selector)` без новых методов `IGraphicsHost`;
- `GraphicsCoordinateSpaceMath` расширен double-точным линейным преобразованием для `PMAP`, при этом графические примитивы по-прежнему используют тот же вспомогательный слой, но с округлением к пикселю;
- интерпретатор и `BytecodeVm` получили одинаковую реализацию `PMAP`, включая `MB3010` без активного `SCREEN`, `MB3007` для селектора вне `0..3`, нечисловой координаты и недопустимо малого viewport при активном `WINDOW`;
- добавлены binder-, runtime-, `BytecodeVm`- и сквозные проверки на прямое и обратное преобразование, тождественное преобразование без активного `WINDOW`, декартов режим и сохранение дробной части;
- добавлен запускаемый пример `samples/functions/pmap/window_and_viewport_conversion.axm` и приёмочная проверка для запуска через `ExecutionMode.Bytecode`;
- справочник синхронизирован: добавлена страница `docs/manual/pmap.html`, `PMAP` включён в навигацию и поиск, а соседняя страница `screen-functions.html` одновременно исправлена по фактическому договору `POINT(number)` после шага `WINDOW`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Input.cs`
- `src/Axiom.Runtime/Host/GraphicsCoordinateSpace.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/functions/pmap/window_and_viewport_conversion.axm`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/manual/pmap.html`
- `docs/manual/graphics.html`
- `docs/manual/window.html`
- `docs/manual/view.html`
- `docs/manual/graphics-primitives.html`
- `docs/manual/screen-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build Axiom.sln --no-restore` — успешно;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-build --filter "FullyQualifiedName~Bind_PMap"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-build --filter "FullyQualifiedName~PMap"` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-build --filter "FullyQualifiedName~PMap"` — успешно;
- `node --check docs/manual/scripts/manual.js` — успешно;
- `node --check docs/manual/scripts/search-index.js` — успешно.

Решение:
- считать графический шаг `PMAP` закрытым по критерию кода, автотестов, запускаемого примера, справочника и проектной документации;
- держать `PMAP` чистой функцией преобразования поверх того же координатного договора `VIEW` + `WINDOW`, не превращая её в отдельную параллельную систему координат.

Следующий точный шаг:
- если продолжаем именно графическую ветку, открыть отдельный договор `GET` / `PUT` поверх уже закрытых `VIEW`, `WINDOW` и `PMAP`;
- не возвращаться к `PMAP` без нового подтверждённого сценария или расхождения в ручной проверке sample.

## Session 872 - 2026-05-17 00:58 - GET и PUT доведены до рабочего договора

Что сделано:
- перечитан обязательный активный контекст проекта и подтверждено, что после закрытого `PMAP` следующим графическим шагом действительно остаются отдельные операторы `GET` / `PUT`;
- найден и закрыт реальный execution-хвост: `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs` раньше не прокидывал `GetAbsolutePixel(...)` и `SetAbsolutePixel(...)`, из-за чего `GraphicsBufferTransfer` в `Axiom.Execution` читал нулевые пиксели и не возвращал буфер обратно в графический host;
- после этой правки адресный тест `Run_WithBytecodeMode_UsesBytecodeVmForGetPutTransfers` начал проходить без отката на интерпретатор;
- acceptance-sample `samples/operators/get/rectangle_capture.axm` и `samples/operators/put/default_xor_and_preset.axm` синхронизированы с graphics-aware harness: вывод перенесён в тот же графический сеанс без лишнего перехода через `SCREEN 0`;
- добавлена новая страница manual `docs/manual/get-put.html`, а обзор графики, страница операторов рисования, навигация и поисковый индекс обновлены так, чтобы `GET` и `PUT` были находимы как отдельный пользовательский договор;
- архитектура, реестр bytecode-покрытия и активный план синхронизированы с итоговым состоянием шага `GET` / `PUT`.

Изменённые файлы:
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `samples/operators/get/rectangle_capture.axm`
- `samples/operators/put/default_xor_and_preset.axm`
- `docs/manual/get-put.html`
- `docs/manual/graphics.html`
- `docs/manual/graphics-primitives.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/ARCHITECTURE.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForGetPutTransfers|FullyQualifiedName~GetAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~PutAcceptanceSample_RunsOnBytecodeVm" -l "console;verbosity=minimal"` — успешно;
- финальные проверки manual и соседних адресных тестов остаются следующим обязательным шагом этой же задачи.

Решение:
- считать графический шаг `GET` / `PUT` закрытым по коду, адресным автотестам, runnable sample, manual и проектной документации;
- держать `GET` / `PUT` поверх того же host-договора абсолютных пикселей и обычного двумерного числового массива, не вводя отдельный packed-формат изображения.

Следующий точный шаг:
- прогнать финальные проверки manual-навигации и соседние адресные тесты;
- затем вручную открыть `samples/operators/get/rectangle_capture.axm` и `samples/operators/put/default_xor_and_preset.axm` в `Axiom.Ide` для визуальной приёмки реального окна `SCREEN`.

## Session 873 - 2026-05-17 08:02 - smoke-приёмка GET и PUT через реальный SCREEN

Что сделано:
- перечитан обязательный активный контекст и подтверждено, что после Session 872 незакрытым хвостом оставалась именно живая приёмка `GET` / `PUT` в реальном `SCREEN`-окне IDE;
- в `ProgramConsoleService` добавлен trace/event-log сигнал `screen.output`, чтобы текстовый вывод внутри активного `SCREEN` можно было наблюдать через IDE log, а не только глазами;
- найден реальный живой дефект IDE-пути: `IdeInputProvider` реализовывал `IGraphicsHost`, но не прокидывал `GetAbsolutePixel(...)` и `SetAbsolutePixel(...)`, из-за чего `GET` / `PUT` в `ProgramRunService` через IDE читали нули;
- дефект закрыт, после чего добавлены адресные IDE-регрессии на `screen.output`, `GET` и `PUT` через `ProgramRunService`;
- `Axiom.Ide.SmokeHarness` расширен режимом `--get-put-only`, который копирует реальные sample `samples/operators/get/rectangle_capture.axm` и `samples/operators/put/default_xor_and_preset.axm` во временный workspace и проверяет ожидаемые `screen.output`-строки в trace реального `Axiom.Ide.exe`.

Изменённые файлы:
- `README.md`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.ScreenOutput_WritesGraphicsOutputTraceAndEventLogEntries|FullyQualifiedName~ProgramRunServiceTests.Run_GetAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm|FullyQualifiedName~ProgramRunServiceTests.Run_PutAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm"` — успешно;
- `dotnet build src/Axiom.Ide/Axiom.Ide.csproj --no-restore` — успешно;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --get-put-only` — успешно;
- `git diff --check -- README.md src/Axiom.Ide/Services/Input/ProgramConsoleService.cs src/Axiom.Ide/Services/Input/IdeInputProvider.cs src/Axiom.Ide.SmokeHarness/Program.cs src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` не показал пробельных ошибок; остались только предупреждения Git о будущей нормализации `LF -> CRLF`.

Решение:
- считать ручной хвост по `GET` / `PUT` закрытым через автоматизированную приёмку живого IDE-пути, а не держать его как внешний ручной долг;
- использовать `screen.output` и `--get-put-only` как постоянный приёмочный маршрут для регрессий графического `SCREEN`-слоя.

Следующий точный шаг:
- если продолжаем именно графическую ветку, сначала зафиксировать в `docs/PLANS3.md` следующий отдельный графический договор поверх уже закрытых `VIEW`, `WINDOW`, `PMAP`, `GET` и `PUT`, а потом открывать код;
- к `GET` / `PUT` возвращаться только при новом расхождении в `--get-put-only`, acceptance-sample или адресных IDE-тестах.

## Session 874 - 2026-05-17 08:14 - в плане выбран следующий графический договор

Что сделано:
- после закрытия smoke-приёмки `GET` / `PUT` повторно поднят активный графический контекст в `docs/SPECIFICATION3.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы выбрать следующий шаг не по инерции, а по уже зафиксированному проектному объёму;
- подтверждено, что следующий отдельный графический шаг в активной спецификации уже назван явно: текущий `SCREEN 10` использует безопасный размер `1280x720`, а адаптивный best-fit по фактической доступной области монитора остаётся следующим отдельным улучшением;
- в `docs/PLANS3.md` добавлено новое обновление плана, которое закрепляет именно этот шаг как следующий графический договор вместо открытия нового оператора рисования.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать следующим отдельным графическим договором не новый оператор поверх уже закрытых `VIEW`, `WINDOW`, `PMAP`, `GET` и `PUT`, а host-договор `SCREEN 10` как настоящий adaptive best-fit;
- до кода сначала формализовать точные правила выбора доступной области, возвращаемых размеров `SCREENWIDTH()` / `SCREENHEIGHT()` и поведения на многомониторной конфигурации.

Следующий точный шаг:
- открыть в `docs/SPECIFICATION3.md` отдельную запись про полный договор `SCREEN 10 best-fit`;
- только после этой спецификации переходить к реализации IDE-host, smoke-приёмке и sample для `SCREEN 10`.

## Session 875 - 2026-05-17 08:16 - зафиксирован договор SCREEN 10 best-fit

Что сделано:
- после выбора следующего графического шага открыт и формализован отдельный договор `SCREEN 10 best-fit` в `docs/SPECIFICATION3.md`;
- закреплено, что режим выбирает максимальный размер клиентской области `16:9` по рабочей области монитора, а не старое фиксированное `1280 x 720`;
- зафиксированы ответы на открытые вопросы следующего шага: берётся рабочая область без системных панелей, монитор выбирается по центру главного окна IDE с запасным переходом на основной монитор, `SCREENWIDTH()` / `SCREENHEIGHT()` возвращают фактический выбранный размер, `LOCATE` продолжает считать сетку `8 x 12` без дополнительной подгонки режима к кратности ячейки;
- активный план синхронизирован с тем, что следующий шаг теперь уже реализационный: `ProgramConsoleService`, оконный слой `SCREEN` и smoke-приёмка.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `git diff --check -- docs/SPECIFICATION3.md docs/PLANS3.md docs/WORKLOG4.md` — успешно; `Git` показал только предупреждения о будущей нормализации `LF -> CRLF` без пробельных ошибок.

Решение:
- считать `SCREEN 10 best-fit` закрытым на уровне проектного договора и готовым к отдельной реализации узким срезом;
- не смешивать этот шаг с новыми графическими операторами или пересмотром уже закрытой координатной семантики.

Следующий точный шаг:
- реализовать вычисление `SCREEN 10` по рабочей области выбранного монитора вместо жёсткого `1280 x 720`;
- покрыть шаг адресными IDE-тестами, smoke-приёмкой и runnable sample, затем синхронизировать `README.md` и `docs/manual/`.

## Session 876 - 2026-05-17 08:49 - SCREEN 10 переведён на адаптивный размер 16:9

Что сделано:
- перечитан активный контекст графической ветки и подтверждено, что после Session 875 открытым оставался именно реализационный шаг по размерной политике `SCREEN 10`;
- в `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs` старое локальное вычисление размеров заменено на отдельный `IScreenModeSizeProvider`, а новый `ScreenModeSizeProvider` выбирает для `SCREEN 10` рабочую область монитора по центру главного окна IDE, строит в ней наибольший вписываемый размер `16:9` и использует запасной размер `1280 x 720`, если host не смог определить геометрию;
- добавлены новые файлы `src/Axiom.Ide/Services/Input/IScreenModeSizeProvider.cs` и `src/Axiom.Ide/Services/Input/ScreenModeSizeProvider.cs`;
- адресные IDE- и execution-тесты синхронизированы под новый договор: `ProgramConsoleServiceTests`, `ProgramRunServiceTests`, `ScreenModeSizeProviderTests`, `AcceptanceProgramTests` и `ExecutionPipelineTests`;
- добавлен runnable sample `samples/graphics/screen/best_fit_dimensions.axm`, а `Axiom.Ide.SmokeHarness` расширен отдельным маршрутом `--screen10-only` для проверки фактических размеров и `LOCATE` через реальный `Axiom.Ide.exe`;
- `README.md`, страницы manual `screen.html`, `screen-functions.html`, `graphics.html`, `graphics-text.html`, реестр bytecode-покрытия и активный план синхронизированы с итоговым пользовательским договором `SCREEN 10`;
- по ходу валидации закрыты два живых дефекта интеграции: публичная доступность `IScreenModeSizeProvider` и отсутствующий `using Axiom.Ide.Graphics;` в `ScreenModeSizeProvider`.

Изменённые файлы:
- `README.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `docs/manual/graphics-text.html`
- `docs/manual/graphics.html`
- `docs/manual/screen-functions.html`
- `docs/manual/screen.html`
- `samples/graphics/screen/best_fit_dimensions.axm`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `src/Axiom.Ide/Services/Input/IScreenModeSizeProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/ScreenModeSizeProvider.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/ScreenModeSizeProviderTests.cs`

Проверки:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramConsoleServiceTests.ActivateScreen10_UsesProvidedBestFitSizeAndVisibleLocateGrid|FullyQualifiedName~ScreenModeSizeProviderTests|FullyQualifiedName~ProgramRunServiceTests.Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm"` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Screen10BestFitAcceptanceSample_RunsOnBytecodeVm"` — успешно;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --screen10-only` — успешно;
- `git diff --check` остаётся финальной завершающей проверкой после синхронизации документации.

Решение:
- считать `SCREEN 10` закрытым как отдельный графический договор: размеры режима теперь определяются фактической рабочей областью монитора, а `SCREENWIDTH()` / `SCREENHEIGHT()` и текстовая сетка `LOCATE` отражают именно этот размер;
- не открывать поверх этого шага новый оператор рисования, пока следующим отдельным графическим договором явно не будет зафиксирована координатная семантика мыши в `SCREEN`.

Следующий точный шаг:
- формализовать следующий отдельный графический договор: что именно возвращают `MOUSEX()` / `MOUSEY()` и координаты `ON MOUSE` относительно фактической клиентской области `SCREEN`, `VIEW`, `WINDOW` и выхода указателя за видимую область;
- после такой фиксации переходить к отдельной реализации и проверкам этого договора.

## Session 877 - 2026-05-26 08:53 - первая волна ремонта кодировки в коде и тестах

Что сделано:
- после обязательного перечитывания `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` поднят новый активный хвост из плана: ремонт кодировки в активных документах и пользовательских диагностических строках;
- в уже затронутых файлах кода исправлены подтверждаемые битые строки пользовательского уровня:
  - parser-сообщения ожиданий и неожиданных токенов в `src/Axiom.Parsing/Parser.Statements.cs`;
  - диагностики и подписи в `src/Axiom.Lexing/Lexer.cs`, `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`, `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`, `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`, `src/Axiom.Runtime/Execution/BytecodeVm*.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`, `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`;
  - видимые комментарии и строка статуса в `src/Axiom.Ide/MainWindow.xaml.cs` и `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`;
  - русские названия benchmark-сценариев в `src/Axiom.ManualChecks/Program.cs`.
- добавлен адресный тест в `tests/Axiom.Lexing.Tests/LexerTests.cs`, который фиксирует точное сообщение `MB0001` для недопустимого символа;
- в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` выровнен устаревший тест под текущий договор `Compile`: ошибка записи JSON snapshot теперь проверяется как backend-диагностика при `CompilationStatus.Success`, а не как несуществующий `BackendFailure`.

Изменённые файлы:
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Records.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj --no-restore` проходит;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore` проходит;
- `dotnet test tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj --no-restore --filter "FullyQualifiedName~Validate_InvalidConversionTypeCode_ReturnsDiagnostic"` проходит;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Compile_ReturnsDiagnosticWhenBytecodeSnapshotWriteFails"` проходит.

Решение:
- считать эту сессию первой узкой волной ремонта кодировки: выровнены живые пользовательские строки в коде и тестах без изменения семантики языка;
- массовую перепись больших mojibake-блоков в `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md` и старых участках `docs/PLANS3.md` не смешивать с этим кодовым проходом и брать отдельной следующей волной, только там, где смысл можно честно восстановить.

Следующий точный шаг:
- перейти ко второй волне ремонта кодировки и восстановить подтверждаемые битые блоки в `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md` и старых разделах `docs/PLANS3.md`;
- после этого снова прогнать точечные проверки и обновить журнал работ.

## Session 877 - 2026-05-17 22:54 - формализован координатный договор мыши в активном SCREEN

Что сделано:
- перечитан обязательный активный контекст (`AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`) и подтверждено, что после Session 876 открытым шагом остаётся именно формализация мышиного координатного договора;
- по коду повторно зафиксировано фактическое поведение в runtime и IDE: `MOUSEX()` / `MOUSEY()` читают host-координаты с проверкой `SCREEN`, `ON MOUSE` передаёт те же `x/y`, `RuntimeInputSnapshot` держит `-1/-1` как состояние «нет данных», `Output`-маршрут сбрасывает мышь через `ClearMouseState()` на `MouseLeave`, нативный `SCREEN`-маршрут работает по клиентским координатам окна и не сбрасывает координаты автоматически при выходе курсора;
- в `docs/SPECIFICATION3.md` добавлен отдельный блок `Обновление спецификации - 2026-05-17 - координатный договор мыши в активном SCREEN` с явной фиксацией связи `MOUSEX()`/`MOUSEY()` и параметров `ON MOUSE`, а также отношений к `VIEW`, `WINDOW`, `PMAP` и выходу указателя из видимой области;
- в `docs/PLANS3.md` добавлена синхронизация состояния, что документный договор координат мыши закрыт и следующий шаг — manual + адресные тесты.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `git diff --check -- docs/SPECIFICATION3.md docs/PLANS3.md docs/WORKLOG4.md` остаётся финальной проверкой после документной синхронизации.

Решение:
- считать договор координат мыши в активном `SCREEN` зафиксированным на уровне спецификации без изменения кода выполнения;
- держать этот шаг отдельным от новых операторов рисования и не смешивать его с расширением графического синтаксиса.

Следующий точный шаг:
- синхронизировать manual-страницы `docs/manual/mousex.html`, `docs/manual/mousey.html`, `docs/manual/on-mouse.html` с новым спецификационным договором про `SCREEN`, `VIEW`, `WINDOW` и поведение при выходе курсора;
- затем добавить/уточнить адресные runtime- и IDE-тесты на зафиксированное поведение (`-1/-1` после `MouseLeave` в `Output`, сохранение последней точки в нативном `SCREEN` без нового события, совпадение `ON MOUSE` `x/y` с чтениями `MOUSEX()`/`MOUSEY()` в момент вызова).

## Session 878 - 2026-05-22 10:08 - manual и тесты синхронизированы с координатным договором мыши

Что сделано:
- перечитан обязательный активный контекст проекта и подтверждено, что после Session 877 следующим шагом по плану оставались именно manual и адресные тесты для уже формализованного договора мыши;
- страницы `docs/manual/mousex.html` и `docs/manual/mousey.html` приведены к одному договору с `docs/SPECIFICATION3.md`: теперь там явно сказано, что функции работают только при активном `SCREEN`, возвращают клиентские координаты активной поверхности ввода, не проходят через `VIEW` / `WINDOW` / `PMAP`, а при отсутствии известных координат возвращают `-1`;
- страница `docs/manual/on-mouse.html` дополнена точными замечаниями о том, что параметры `x/y` совпадают с текущими чтениями `MOUSEX()` / `MOUSEY()`, что маршрут `Output` в IDE сбрасывает мышиное состояние в `-1/-1` на `MouseLeave`, а нативное `SCREEN`-окно удерживает последнюю известную точку до следующего события ввода;
- `docs/manual/scripts/search-index.js` синхронизирован с новыми формулировками, чтобы поиск по `SCREEN`, `VIEW`, `WINDOW`, клиентской области и координатам мыши приводил к обновлённым страницам;
- в `tests/Axiom.Runtime.Tests` добавлены две адресные проверки на совпадение `ON MOUSE` `x/y` с `MOUSEX()` / `MOUSEY()` в interpreter- и `BytecodeVm`-пути;
- в `tests/Axiom.Ide.Tests/NativeGraphicsScreenWindowTests.cs` добавлена проверка, что нативное `SCREEN`-окно сохраняет последнюю известную мышиную координату без нового события ввода.

Изменённые файлы:
- `docs/manual/mousex.html`
- `docs/manual/mousey.html`
- `docs/manual/on-mouse.html`
- `docs/manual/scripts/search-index.js`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/NativeGraphicsScreenWindowTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_OnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen|FullyQualifiedName~Execute_CompiledOnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen"` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~MouseMoveMessage_KeepsLastKnownCoordinatesUntilAnotherMouseInputArrives"` — успешно;
- `node --check docs/manual/scripts/search-index.js` — успешно;
- `git diff --check` остаётся финальной завершающей проверкой после обновления журналов.

Решение:
- считать мышиный координатный договор закрытым по спецификации, manual и адресным автотестам;
- не расширять этот шаг новыми пользовательскими формами ввода, пока отдельно не выбран следующий графический или событийный договор.

Следующий точный шаг:
- выбрать следующий отдельный шаг графической ветки после уже закрытых `SCREEN`, `VIEW`, `WINDOW`, `PMAP`, `GET`, `PUT` и мышиного координатного договора;
- если ближайший приоритет остаётся у справочника, продолжить редакционный проход по обзорным страницам `data-and-input.html` и `graphics.html`, чтобы они одинаково явно вели к уже закрытым договорам ввода и экрана.

## Session 879 - 2026-05-22 10:27 - обзорные страницы manual по вводу и графике подчистены

Что сделано:
- перечитан обязательный активный контекст проекта и подтверждено, что после Session 878 следующим логичным шагом оставался именно обзорный редакционный проход по `docs/manual/data-and-input.html` и `docs/manual/graphics.html`;
- `docs/manual/data-and-input.html` переписан так, чтобы обзор ввода явнее различал блокирующий строковый ввод `INPUT`, опрос текущего состояния устройства, чтение одноразовых событий из очереди и обработчики `ON KEY` / `ON MOUSE`;
- на странице `docs/manual/data-and-input.html` мышиный маршрут теперь прямо разводит роли `MOUSEX()` / `MOUSEY()`, `MOUSEBUTTON()`, `MOUSEPRESS()` и `ON MOUSE`, включая уже закрытый договор о том, что в графическом `SCREEN` мышиные координаты остаются клиентскими и не проходят через `VIEW` / `WINDOW` / `PMAP`;
- `docs/manual/graphics.html` перестроен как обзор уже закрытых слоёв графики: выбор режима `SCREEN`, экранные размеры и текстовая сетка, текстовый слой, примитивы, преобразование координат через `VIEW` / `WINDOW` / `PMAP`, блочный перенос через `GET` / `PUT` и отдельный слой клавиатуры и мыши поверх активной поверхности ввода;
- в `docs/manual/graphics.html` добавлен явный маршрут чтения раздела, чтобы обзор вёл к адресным страницам по режиму экрана, тексту, координатам, буферам и интерактивному вводу без смешения пользовательских договоров.

Изменённые файлы:
- `docs/manual/data-and-input.html`
- `docs/manual/graphics.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `git diff --check -- docs/manual/data-and-input.html docs/manual/graphics.html docs/PLANS3.md docs/WORKLOG4.md` запланирован как финальная проверка после завершения этого журнального шага.

Решение:
- считать этот проход чисто редакционным: он не меняет семантику языка, а только делает обзорные страницы согласованными с уже закрытыми договорами `SCREEN` и мыши;
- не обновлять `docs/manual/scripts/search-index.js` и `docs/manual/scripts/manual.js`, потому что discoverability для этих тем уже обеспечивается существующей навигацией и адресными страницами.

Следующий точный шаг:
- выбрать следующий отдельный пользовательский договор графической ветки после уже закрытых `SCREEN`, `VIEW`, `WINDOW`, `PMAP`, `GET`, `PUT` и мышиного слоя;
- если приоритет у manual сохранится, продолжить такой же обзорный проход по следующему соседнему тематическому узлу, где обзор ещё недостаточно явно ведёт к закрытым договорам.

## Session 880 - 2026-05-25 16:20 - аудит слабых мест перед дальнейшим развитием

Что сделано:
- перечитан обязательный активный контекст проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`;
- подтверждено текущее состояние: проект уже имеет развитое ядро языка, `BytecodeVm`, WPF IDE, графический слой, runnable samples, manual и служебные инструменты сопровождения;
- установлено, что предыдущая работа остановилась после редакционного прохода по обзорным manual-страницам ввода и графики, а ближайший записанный шаг был выбором следующего графического договора или продолжением редакционного прохода;
- выполнен обзор структуры solution, крупных файлов, временных артефактов, служебных инструментов, fallback-модели запуска и признаков mojibake в активных документах и коде;
- в `docs/PLANS3.md` добавлен отдельный план улучшений перед дальнейшим развитием.

Найденные слабые места:
- активные документы и пользовательские строки кода всё ещё содержат повреждённые русские фрагменты;
- `TestResults` обнаружены под `tests/`, хотя временные результаты должны жить в `.artifacts/`;
- `Axiom.ManualChecks` фактически стал важным инструментом сопровождения, но его `Program.cs` объединяет слишком много ролей;
- несколько производственных и тестовых файлов уже достаточно велики, чтобы затруднять дальнейшие точечные изменения;
- после удаления пользовательского режима `Auto` в модели результата запуска ещё видны fallback-поля, требующие отдельного решения;
- карта solution в README и архитектуре местами отстаёт от фактического состава проектов.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- код, поведение языка, samples и manual-страницы не менялись;
- полная сборка и тесты не запускались, потому что задача была аналитической и документной.

Решение:
- перед новым крупным развитием лучше сначала закрыть не новую языковую возможность, а короткую стабилизационную очередь: кодировка, временные артефакты, оформление `Axiom.ManualChecks`, карта solution, затем точечное дробление крупных файлов и fallback-модель.

Следующий точный шаг:
- начать с отдельной задачи «ремонт кодировки активных документов и пользовательских диагностик»: составить список повреждённых строк, исправить только подтверждаемые фрагменты, добавить точечные тесты на исправленные диагностики и обновить журнал работ.

## Session 881 - 2026-05-26 11:44 - вторая узкая волна ремонта кодировки в active docs

Что сделано:
- во второй узкой волне исправлены подтверждаемые повреждённые строки в уже открытых файлах `tests/Axiom.Runtime.Tests/InterpreterTests.cs`, `samples/smoke/repair_plans_mojibake.axm` и `docs/ARCHITECTURE.md`;
- в runtime-тесте и smoke-sample выровнены пользовательские строки с маркером `U+FFFD/U+FFFD`, чтобы они совпадали с фактическим символом замены, а не с битой последовательностью `U+043F/U+0457/U+0405`;
- в `docs/ARCHITECTURE.md` восстановлены читаемые активные блоки про `KEYDOWN()`, единый договор имён клавиш и одноразовую проверку `KEYPRESS()` по уже подтверждённым соседним разделам и текущему коду.

Изменённые файлы:
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/smoke/repair_plans_mojibake.axm`
- `docs/ARCHITECTURE.md`

Проверка:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_SmokeSampleReportsUnrecoverableReplacementMarkers"` проходит.

Решение:
- считать этот подшаг завершённым: исправлены только те строки и абзацы, смысл которых можно было подтвердить без догадок;
- не расширять эту же правку на сильно повреждённые исторические фрагменты `docs/PLANS3.md`, пока для них нет надёжного контекста восстановления.

Следующий точный шаг:
- продолжить вторую волну только по подтверждаемым mojibake-блокам в `docs/SPECIFICATION3.md` и в старых, но всё ещё читаемых разделах `docs/PLANS3.md`, не смешивая это с другими задачами стабилизации.

## Session 882 - 2026-05-26 12:19 - восстановлены активные TYPE-блоки в спецификации

Что сделано:
- в `docs/SPECIFICATION3.md` восстановлены два активных подтверждаемых блока, ранее повреждённых mojibake: про конструктор значений `TYPE` через `:=` и про явные режимы `BYREF` / `BYVAL`;
- текст восстановлен только по уже согласованному соседнему контексту спецификации, коду, тестам и предыдущим документным решениям, без добавления новой семантики;
- для активной спецификации убраны оставшиеся повреждённые последовательности из этого участка, чтобы раздел снова читался как источник проектного договора.

Изменённые файлы:
- `docs/SPECIFICATION3.md`

Проверка:
- `rg -n "U+00D0|U+00D1|U+0432/U+0402|U+043F/U+0457/U+0405|U+FFFD" docs/SPECIFICATION3.md` не находит совпадений;
- `git diff --check -- docs/SPECIFICATION3.md` проходит без ошибок содержимого.

Решение:
- считать восстановление этих TYPE-разделов завершённым, потому что их смысл подтверждался соседними частями активной спецификации и текущим состоянием реализации;
- не продолжать в этой же правке массовое восстановление сильно повреждённых исторических фрагментов, где без догадок уже нельзя гарантировать исходный смысл.

Следующий точный шаг:
- продолжить вторую волну ремонта кодировки только по тем фрагментам `docs/PLANS3.md`, где смысл можно надёжно восстановить по текущим активным документам и уже проверенному коду;
- если такой уверенности нет, оставить исторический блок нетронутым и перейти к следующей стабилизационной задаче из плана.

## Session 883 - 2026-05-26 12:38 - вторая волна ремонта кодировки остановлена на исторических блоках

Что сделано:
- выполнен повторный целевой просмотр оставшихся совпадений по mojibake-маркерам в `docs/PLANS3.md` и активных журналах;
- подтверждено, что после уже сделанных правок оставшиеся повреждённые совпадения в `docs/PLANS3.md` сосредоточены в старых плотных исторических блоках 2026-04-30 и 2026-05-01, где безопасного восстановления исходного смысла по одному только текущему контексту уже нет;
- для `docs/WORKLOG4.md` найденные совпадения относятся к буквальному упоминанию маркеров поиска и к уже исправленной строке про `U+FFFD/U+FFFD`, то есть не требуют дополнительного ремонта.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `rg -n "U+00D0|U+00D1|U+0432/U+0402|U+043F/U+0457/U+0405|U+FFFD" docs/PLANS3.md docs/WORKLOG4.md` подтверждает, что активные новые записи читаемы, а проблемные совпадения остались только в старых исторических блоках `docs/PLANS3.md` и в буквальных поисковых маркерах;
- повторный просмотр хвостов `docs/PLANS3.md` и `docs/WORKLOG4.md` подтверждает корректное append-only обновление.

Решение:
- считать текущую волну ремонта кодировки исчерпанной на честно подтверждаемых фрагментах и не продолжать ручное «восстановление по догадке» для старых исторических абзацев;
- перевести следующий рабочий шаг на соседнюю стабилизационную задачу про временные артефакты и быстрый репозиторный sanity-check.

Следующий точный шаг:
- разобраться, почему `dotnet test` оставляет `TestResults` под `tests/`, и настроить вынос результатов тестов в `.artifacts/test-results/`;
- затем добавить короткую проверку репозитория на временные каталоги вне `.artifacts/`.

## Session 884 - 2026-05-26 13:39 - маршрутизация результатов `dotnet test` переведена в `.artifacts`

Что сделано:
- подтверждено, что одних свойств `Directory.Build.props` недостаточно для переноса `trx`-результатов: `dotnet test` продолжал создавать `TestResults` рядом с тестовым проектом, поэтому временная попытка через MSBuild-свойства отменена;
- добавлен сценарий `scripts/dotnet-test-artifacts.ps1`, который запускает `dotnet test` с `--results-directory` и складывает результаты в `.artifacts/test-results/<имя-цели>/`;
- добавлен сценарий `scripts/repo-sanity.ps1`, который проверяет, что вне `.artifacts/` не остались временные каталоги `bin`, `obj`, `TestResults`, `.dotnet-home`, `.dotnet-cli` и `.tmp-build`;
- `README.md` обновлён под новый рабочий способ запуска тестов и под репозиторную sanity-проверку;
- старые каталоги `tests/**/TestResults` удалены перед повторной проверкой, чтобы убедиться, что новые запуски их больше не создают.

Изменённые файлы:
- `README.md`
- `scripts/dotnet-test-artifacts.ps1`
- `scripts/repo-sanity.ps1`

Проверка:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_SmokeSampleReportsUnrecoverableReplacementMarkers" --logger "trx;LogFileName=redirect-check.trx"` проходит, а файл результатов появляется в `.artifacts/test-results/Axiom.Runtime.Tests/redirect-check.trx`;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит и не находит временных каталогов вне `.artifacts/`;
- `git diff --check -- Directory.Build.props README.md scripts/dotnet-test-artifacts.ps1 scripts/repo-sanity.ps1` проходит; остаётся только предупреждение Git о будущем переводе `README.md` в `CRLF`.

Решение:
- считать задачу по выносу тестовых результатов и по базовой проверке временных каталогов закрытой на уровне репозиторного рабочего процесса;
- не пытаться дальше продавить это через глобальные MSBuild-свойства, раз надёжный и проверяемый контракт уже обеспечен обёрткой вокруг `dotnet test`.

Следующий точный шаг:
- пройтись по уже накопившимся изменениям `Axiom.ManualChecks` и `docs/manual/`, проверить их на соответствие текущему состоянию `SCREEN`, экранных функций и связанных справочных страниц, затем дописать `WORKLOG4` и `PLANS3` уже для этой волны выравнивания документации и ручных проверок.

## Session 885 - 2026-05-26 14:15 - повторно подтверждена согласованность `SCREEN 10` и мышиной семантики

Что сделано:
- повторно проверена уже накопленная ветка изменений вокруг `SCREEN 10`, экранных функций `SCREENWIDTH()` / `SCREENHEIGHT()`, справочных страниц `docs/manual/` и координатной семантики мыши в активном `SCREEN`, чтобы отделить живое текущее состояние от старых незавершённых заходов;
- подтверждено, что адресные IDE- и execution-проверки для адаптивного `SCREEN 10` проходят на текущем дереве, а `Axiom.Ide.SmokeHarness` по-прежнему успешно проходит узкий маршрут `--screen10-only` через реальный `Axiom.Ide.exe`;
- дополнительно подтверждено, что текущий договор мыши не расползается: runtime-проверки для `ON MOUSE("MOVE")` показывают совпадение координат обработчика с `MOUSEX()` / `MOUSEY()`, а `NativeGraphicsScreenWindowTests` сохраняет последнюю известную точку до нового события ввода;
- после тестов и живого smoke-пути повторно выполнен `scripts/repo-sanity.ps1`, чтобы убедиться, что новые прогоны не оставили временных каталогов вне `.artifacts/`.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenModeSizeProviderTests"` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm"` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~MouseMoveMessage_KeepsLastKnownCoordinatesUntilAnotherMouseInputArrives"` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Screen10BestFitAcceptanceSample_RunsOnBytecodeVm"` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~OnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen"` — успешно;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --screen10-only` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` — успешно, временные каталоги вне `.artifacts/` не найдены.

Решение:
- считать текущую волну повторной проверки закрытой: на сегодняшнем дереве договор `SCREEN 10`, связанный runnable sample, адресные автотесты, мышиная координатная семантика и узкий IDE smoke-маршрут согласованы между собой;
- не переписывать заново уже существующие записи Session 876 и соответствующие плановые блоки как будто это новая разработка, потому что в этом заходе был именно повторный контроль состояния, а не новая функциональная волна.

Следующий точный шаг:
- отдельно разобрать остаточные mojibake-подозрения в `src/Axiom.ManualChecks/Program.cs` и соседних ручных benchmark-названиях, чтобы понять, это реальный дефект файлового содержимого или только артефакт текущего консольного вывода, и после этого либо исправить подтверждённые строки, либо явно снять ложную тревогу.

## Session 886 - 2026-05-26 14:21 - снята ложная тревога по mojibake в `Axiom.ManualChecks`

Что сделано:
- проверен актуальный диапазон benchmark-имён в `src/Axiom.ManualChecks/Program.cs`: текущее файловое содержимое хранит нормальные русские строки для документных, файловых, строковых, regex-, массивных и управляющих сценариев, включая ранее подозрительные записи вокруг `regex_*`, `array_*`, `date_time_*` и `type_record_roundtrip`;
- отдельно сверено, что поиск маркеров перекодировки `U+00D0`, `U+00D1`, `U+043F/U+0457/U+0405`, `U+FFFD`, `U+0432/U+0402`, `U+00C3` по каталогу `src/Axiom.ManualChecks` больше ничего не находит, а значит подозрение по этому участку не подтверждается на уровне файлов репозитория;
- собран `src/Axiom.ManualChecks/Axiom.ManualChecks.csproj`, чтобы подтвердить, что текущий файл в рабочем дереве не только читаем, но и проходит обычную компиляцию без дополнительных правок;
- проверен `scripts/repo-sanity.ps1`: сам файл хранит корректную русскую строку `Sanity-check пройден: временные каталоги вне .artifacts не найдены.`, поэтому кракозябры в выводе этого сценария относятся к текущему терминальному декодированию, а не к порче содержимого репозитория.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `rg -n "U+00D0|U+00D1|U+043F/U+0457/U+0405|U+FFFD|U+0432/U+0402|U+00C3" src/Axiom.ManualChecks` — совпадений не найдено;
- `dotnet build src/Axiom.ManualChecks/Axiom.ManualChecks.csproj --no-restore` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` — успешно по сути проверки; строка успеха в терминале отображается с артефактами декодирования, но сам `scripts/repo-sanity.ps1` хранит корректный русский текст.

Решение:
- считать пункт плана про mojibake в `src/Axiom.ManualChecks/Program.cs` закрытым как ложную тревогу: рабочее дерево уже содержит нормальный текст, а путаница пришла из старого диффа и терминального отображения;
- не править `src/Axiom.ManualChecks/Program.cs` наугад только ради косметического совпадения с тем, как старое содержимое показывается в `git diff` через текущий терминал.

Следующий точный шаг:
- снять узкий инвентарь уже изменённых страниц `docs/manual/` и связанных проверок `Axiom.ManualChecks`, чтобы понять, какие из этих накопленных правок ещё требуют адресной валидации или документного добивания, а какие уже достаточно подтверждены текущими тестами и smoke-сценариями.

## Session 887 - 2026-05-26 21:47 - выровнен синтаксис страницы `ON MOUSE` в manual

Что сделано:
- в `docs/manual/on-mouse.html` расширен раздел `Синтаксис`: страница теперь явно перечисляет все подтверждённые формы `ON MOUSE(...), ON/STOP/OFF` для нажатия, отпускания `UP`, перемещения `MOVE` и колеса `WHEEL`, вместо прежнего неполного набора;
- в том же файле уточнён раздел `Режимы`, чтобы `ON`, `STOP` и `OFF` больше не читались как возможности только для формы нажатия кнопки;
- подтверждено по `docs/SPECIFICATION3.md` и уже существующим parser/runtime-тестам, что manual теперь совпадает с фактически поддержанными синтаксическими формами оператора.

Изменённые файлы:
- `docs/manual/on-mouse.html`
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `git diff --check -- docs/manual/on-mouse.html` — без ошибок содержимого; Git отдельно предупреждает, что в рабочем дереве `LF` при следующем касании Git будет нормализован в `CRLF`;
- `rg -n "ON MOUSE\\(" docs/manual/on-mouse.html` — подтверждает, что на странице теперь перечислены формы для `buttonNumber`, `buttonNumber, "UP"`, `"MOVE"` и `"WHEEL"`.

Решение:
- считать пункт инвентаризации по странице `ON MOUSE` закрытым: найденный дефект был именно в неполном manual-списке, а не в спецификации или реализации;
- не трогать навигацию `docs/manual/scripts/search-index.js`, потому что сам оператор `ON MOUSE` уже был discoverable, менялась только полнота описания на существующей странице.

Следующий точный шаг:
- пройтись по соседним страницам `docs/manual/mousex.html`, `docs/manual/mousey.html` и `docs/manual/screen-functions.html`, чтобы так же адресно подтвердить договор `MB3010`, `-1/-1`, сырой координатный слой и связь с активным `SCREEN`.

## Session 888 - 2026-05-26 21:53 - закрыт соседний manual-срез по `SCREEN` и мыши

Что сделано:
- перепроверены уже изменённые страницы `docs/manual/mousex.html`, `docs/manual/mousey.html`, `docs/manual/screen-functions.html`, `docs/manual/screen.html`, `docs/manual/graphics.html`, `docs/manual/graphics-text.html`, `docs/manual/data-and-input.html` и индекс `docs/manual/scripts/search-index.js`;
- подтверждено, что текущие тексты уже согласованы с активным договором `docs/SPECIFICATION3.md`: требование активного `SCREEN`, диагностика `MB3010`, возврат `-1` при неизвестной позиции, сырой координатный слой мыши без пересчёта через `VIEW` / `WINDOW` / `PMAP`, а также adaptive best-fit-поведение `SCREEN 10`;
- новых подтверждаемых manual-расхождений в этом связанном блоке не найдено, поэтому дополнительных правок в HTML и индексе не вносилось.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `rg -n "ON MOUSE|MOUSEX|MOUSEY|SCREEN 10|SCREENWIDTH\\(\\)|SCREENHEIGHT\\(\\)|VIEW|WINDOW|PMAP|MB3010|-1/-1" docs/manual/scripts/search-index.js docs/manual/index.html docs/manual/*.html` — проверка индекса и manual-покрытия; `rg` на Windows отдельно ругается на шаблон `docs/manual/*.html`, но по целевым файлам `index.html` и `search-index.js` выдаёт ожидаемые совпадения;
- выборочное чтение `docs/manual/*.html` с `UTF-8`-выводом — подтверждает корректное содержимое без новых расхождений со спецификацией.

Решение:
- считать текущий manual-срез вокруг `SCREEN`, экранных функций и мыши документно закрытым: после правки страницы `ON MOUSE` остальные затронутые страницы уже находятся в согласованном состоянии;
- не продолжать ручной поиск «ещё одного дефекта» внутри того же блока без нового сигнала из кода, smoke-проверок или пользовательского сценария.

Следующий точный шаг:
- перейти от manual-инвентаризации к оставшемуся связанному хвосту в `src/Axiom.Ide.SmokeHarness` и `src/Axiom.Ide`, чтобы проверить, не осталось ли там неподтверждённых рабочих изменений вокруг `SCREEN` и мыши, которые ещё не закрыты адресной валидацией.

## Session 889 - 2026-05-26 22:10 - подтверждён хвост `Axiom.Ide` и `Axiom.Ide.SmokeHarness`

Что сделано:
- просмотрены текущие diff-блоки `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`, `src/Axiom.Ide.SmokeHarness/Program.cs`, `src/Axiom.Ide/MainWindow.xaml.cs`, `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs` и `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`;
- подтверждено, что этот хвост состоит из уже проверенной поддержки отдельного `--screen10-only` smoke-прогона, перевода `ProgramConsoleService` на общий `IScreenModeSizeProvider` и локальных исправлений mojibake-строк в `Axiom.Ide`;
- поиск маркеров перекодировки по `src/Axiom.Ide` и `src/Axiom.Ide.SmokeHarness` не дал новых совпадений.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `rg -n "U+00D0|U+00D1|U+043F/U+0457/U+0405|U+FFFD|U+0432/U+0402|U+00C3" src/Axiom.Ide src/Axiom.Ide.SmokeHarness` — новых маркеров mojibake не найдено;
- `dotnet build src/Axiom.Ide/Axiom.Ide.csproj --no-restore` — успешно;
- `dotnet build src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore` — успешно.

Решение:
- считать текущий хвост `Axiom.Ide` и `Axiom.Ide.SmokeHarness` адресно подтверждённым: документно и технически он больше не выглядит как неподтверждённый шум рабочего дерева;
- не возвращаться к этой связке без нового сигнала из тестов, smoke-прогона или пользовательского сценария.

Следующий точный шаг:
- перейти к оставшейся волне изменений в `src/Axiom.Bytecode`, `src/Axiom.Runtime`, `src/Axiom.Execution`, `src/Axiom.Lexing`, `src/Axiom.Parsing`, `src/Axiom.Semantics` и связанных `tests/`, чтобы отделить уже подтверждённые функциональные правки от тех, которым ещё нужна адресная валидация или документное сопровождение.

## Session 890 - 2026-05-26 22:33 - адресно подтверждён языковой хвост диагностик и smoke-починки mojibake

Что сделано:
- последовательно прогнаны адресные тесты по оставшейся связке `Bytecode`/`Execution`/`Lexer`/`Runtime`, чтобы подтвердить точные русские диагностические сообщения и сценарии, затронутые текущей волной правок;
- отдельно подтверждено, что кривой вид русских строк в части выводов PowerShell остаётся эффектом терминального отображения, а не содержимого файлов: прямое UTF-8 чтение `tests/Axiom.Lexing.Tests/LexerTests.cs` показывает корректную строку `Недопустимый символ: '@'.`;
- закрыт хвост по smoke-сценариям `repair_plans_mojibake.axm`, чтобы не оставлять рядом с изменённым образцом недопроверенные рабочие изменения.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `dotnet test tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj --no-restore --filter "FullyQualifiedName~Validate_ConvertWithUnsupportedSourceStackValue_ReturnsDiagnostic"` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Compile_ReturnsDiagnosticWhenBytecodeSnapshotWriteFails"` — успешно;
- `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj --no-restore --filter "FullyQualifiedName~Lex_ReportsUnknownCharacter"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_CompiledOnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_OnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_BuiltinReplaceReportsEmptyOldText"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy"` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_SmokeSampleRepairsWindows1251ControlByteInsideMojibake"` — успешно.

Решение:
- считать текущую языковую волну вокруг русских диагностик, координат мыши и smoke-починки mojibake технически подтверждённой адресными прогонами;
- не возвращаться к этой группе изменений без нового сигнала из более широкого полного прогона, пользовательского сценария или обзора diff-блока.

Следующий точный шаг:
- проверить, не остаются ли после текущих `dotnet test` временные каталоги `TestResults` вне `.artifacts`, и если остаются, перенастроить репозиторную тестовую обвязку так, чтобы все такие артефакты уходили только в `.artifacts/`.

## Session 891 - 2026-05-26 22:35 - подтверждено отсутствие новых `TestResults` вне `.artifacts`

Что сделано:
- проверено, не появились ли после серии прямых адресных `dotnet test` новые каталоги `TestResults` вне `.artifacts`;
- перепроверено, что в репозитории уже есть защитный сценарий `scripts/repo-sanity.ps1`, и он по-прежнему подтверждает чистоту рабочего дерева по временным каталогам;
- снят быстрый инвентарь оставшихся крупных diff-блоков документации, чтобы не держать в плане уже закрытый инфраструктурный риск.

Изменённые файлы:
- `docs/WORKLOG4.md`
- `docs/PLANS3.md`

Проверка:
- `Get-ChildItem -Path . -Recurse -Directory -Filter TestResults | ForEach-Object { $_.FullName }` — новых каталогов `TestResults` не найдено;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` — успешно, временные каталоги вне `.artifacts` не обнаружены;
- `git diff --stat -- README.md docs/ARCHITECTURE.md docs/BYTECODE_COVERAGE.md docs/SPECIFICATION3.md` — остаётся крупный документный хвост, требующий отдельного прохода на согласованность.

Решение:
- считать риск повторного появления `TestResults` вне `.artifacts` на текущем шаге фактически закрытым: прямые targeted-прогоны его не воспроизводят, а существующая репозиторная проверка остаётся рабочей;
- не держать проверку `TestResults` в роли следующего шага, чтобы не крутиться на уже подтверждённой теме.

Следующий точный шаг:
- перейти к оставшемуся крупному документному хвосту в `README.md`, `docs/ARCHITECTURE.md`, `docs/BYTECODE_COVERAGE.md` и `docs/SPECIFICATION3.md`, чтобы понять, какие из этих изменений уже соответствуют подтверждённому состоянию кода и manual, а какие ещё требуют адресной сверки.

## Session 892 - 2026-05-26 22:52 - подтверждён крупный документный хвост и восстановлена `ARCHITECTURE`

Что сделано:
- проверены текущие diff-блоки `README.md`, `docs/ARCHITECTURE.md`, `docs/BYTECODE_COVERAGE.md` и `docs/SPECIFICATION3.md`;
- подтверждено, что добавленные в `README.md` команды тестового wrapper-сценария, sanity-проверки и маршрута `--screen10-only` опираются на существующие файлы и код;
- подтверждено, что новая запись `SCREEN 10` в `docs/BYTECODE_COVERAGE.md` соответствует текущим provider-, sample-, smoke- и test-опорам;
- в `docs/ARCHITECTURE.md` восстановлены оставшиеся повреждённые блоки `2026-05-10` про залитые прямоугольники, native `SCREEN`, сглаживание и ритм показа кадров, а также агрегированные журналы кадров и ввода.

Изменённые файлы:
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `rg -n "U+00D0|U+00D1|U+0432/U+0402|U+043F/U+0457/U+0405|U+FFFD|U+00C3|U+FFFDU+FFFDU+FFFDU+FFFD" README.md docs/ARCHITECTURE.md docs/BYTECODE_COVERAGE.md docs/SPECIFICATION3.md` — совпадений не найдено;
- `git diff --check -- README.md docs/ARCHITECTURE.md docs/BYTECODE_COVERAGE.md docs/SPECIFICATION3.md` — проходит, остаются только предупреждения Git о будущей нормализации LF/CRLF;
- адресный поиск по `screen10-only`, `best_fit_dimensions`, `IScreenModeSizeProvider`, `ScreenModeSizeProviderTests`, `screen.frame.summary` и `screen.input.summary` подтверждает, что документные утверждения опираются на существующие файлы кода, тестов, примеров и сценариев.

Решение:
- считать крупный документный хвост по `README.md`, `docs/ARCHITECTURE.md`, `docs/BYTECODE_COVERAGE.md` и `docs/SPECIFICATION3.md` закрытым на текущем дереве;
- не запускать .NET-тесты специально для этой правки, потому что в этом заходе менялся только восстановленный текст архитектурного документа и записи состояния проекта, а поведение языка, VM и IDE не менялось.

Следующий точный шаг:
- снять общий итоговый инвентарь оставшегося рабочего diff-а по коду, тестам, manual и samples, затем выбрать минимальную финальную последовательную валидацию перед сдачей этой стабилизационной волны.

## Session 893 - 2026-05-27 07:40 - зелёная финальная валидация стабилизационной волны

Что сделано:
- снят общий инвентарь оставшегося рабочего дерева после закрытия документного хвоста;
- общий прогон `dotnet test Axiom.sln` через `scripts/dotnet-test-artifacts.ps1` сначала выявил три реальные падающие проверки вокруг `DRAW P` и радиальных линий `CIRCLE`;
- исправлен общий host-слой `GraphicsHostDrawing.DrawCircle`: уже вычисленные флаги радиальных линий больше не теряются при повторной нормализации аргументов `CIRCLE`;
- выровнены runtime-тесты `DRAW P`: seed-точка заливки явно переносится внутрь рамки через `BM44,44`, чтобы тест проверял заливку, а не случай старта на цвете границы.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/GraphicsInstructionArguments.cs`
- `src/Axiom.Runtime/Host/GraphicsHostDrawing.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `powershell -ExecutionPolicy Bypass -File .\scripts\dotnet-test-artifacts.ps1 tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_CompiledDrawSupportsMovementModifiersColorScaleAngleMoveAndPaint|FullyQualifiedName~Execute_DrawSupportsMovementModifiersColorScaleAngleMoveAndPaint" --logger "trx"` — успешно, 2/2;
- `powershell -ExecutionPolicy Bypass -File .\scripts\dotnet-test-artifacts.ps1 tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius" --logger "trx"` — успешно, 1/1;
- `powershell -ExecutionPolicy Bypass -File .\scripts\dotnet-test-artifacts.ps1 Axiom.sln --logger "trx"` — успешно, весь набор решения проходит;
- `powershell -ExecutionPolicy Bypass -File .\scripts\repo-sanity.ps1` — успешно, временные каталоги вне `.artifacts` не найдены;
- `git diff --check -- ...` по затронутым файлам — успешно, остаются только предупреждения Git о будущей нормализации LF/CRLF.

Решение:
- считать активные тестовые падения стабилизационной волны закрытыми;
- не добавлять новый `.axm`-пример и новую manual-страницу для исправления `CIRCLE`, потому что существующий договор уже описан в `docs/manual/graphics-primitives.html`, а runnable sample `samples/graphics/primitives/circle_arcs_step_aspect.axm` уже покрывает отрицательные углы, радиальные линии, `STEP` и `aspect`;
- не менять реестры bytecode-покрытия: покрытие и статус паритета не расширялись, исправлялась регрессия общего host-выполнения.

Следующий точный шаг:
- пройти финальный обзор рабочего дерева перед сдачей: убедиться, что все оставшиеся изменения связаны с одной стабилизационной волной, затем подготовить итоговый отчёт или, если пользователь попросит, стадирование/коммит.

## Session 894 - 2026-05-27 07:44 - снят финальный обзор рабочего дерева

Что сделано:
- после зелёного полного тестового прогона снят финальный обзор рабочего дерева;
- подтверждено, что в tracked-части остаётся 47 изменённых файлов, а в untracked-части — 6 новых файлов: `samples/graphics/screen/best_fit_dimensions.axm`, `scripts/dotnet-test-artifacts.ps1`, `scripts/repo-sanity.ps1`, `src/Axiom.Ide/Services/Input/IScreenModeSizeProvider.cs`, `src/Axiom.Ide/Services/Input/ScreenModeSizeProvider.cs`, `tests/Axiom.Ide.Tests/ScreenModeSizeProviderTests.cs`;
- `docs/PLANS3.md` обновлён так, чтобы следующий шаг больше не указывал на уже выполненный обзор.

Проверка:
- `git diff --check -- src/Axiom.Runtime/Execution/GraphicsInstructionArguments.cs src/Axiom.Runtime/Host/GraphicsHostDrawing.cs tests/Axiom.Runtime.Tests/InterpreterTests.cs tests/Axiom.Runtime.Tests/BytecodeVmTests.cs docs/PLANS3.md docs/WORKLOG4.md` — успешно, остаются только предупреждения Git о будущей нормализации LF/CRLF;
- `git status --short`, `git diff --stat` и `git ls-files --others --exclude-standard` использованы для финального инвентаря.

Решение:
- считать обзор рабочего дерева выполненным: оставшиеся изменения относятся к одной стабилизационной волне вокруг `SCREEN`, мыши, manual, runtime/bytecode-проверок и инфраструктуры тестовых артефактов;
- не запускать дополнительный общий `dotnet test` после этой записи, потому что после зелёного полного прогона менялись только документы состояния проекта.

Следующий точный шаг:
- подготовить итоговый отчёт по стабилизационной волне; если пользователь попросит, следующим отдельным действием можно стадировать и коммитить текущий набор изменений.

## Session 895 - 2026-05-27 07:45 - acceptance-sample `CIRCLE` уточнён под радиальные линии

Что сделано:
- существующий runnable sample `samples/graphics/primitives/circle_arcs_step_aspect.axm` уточнён в ожидаемом описании: отрицательные углы `CIRCLE` должны рисовать радиальные линии;
- это закрывает обязательный sample-след для исправления регрессии общего host-выполнения `CIRCLE`.

Изменённые файлы:
- `samples/graphics/primitives/circle_arcs_step_aspect.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- не добавлять отдельный новый sample, потому что текущий тематический пример уже содержит отрицательный угол, радиальную линию, `STEP` и `aspect`;
- не менять manual и bytecode-реестр дополнительно: manual уже описывает отрицательные углы `CIRCLE`, а реестр уже отмечает sample `circle_arcs_step_aspect.axm` как покрытие полной формы `CIRCLE`.

Следующий точный шаг:
- подготовить итоговый отчёт по стабилизационной волне; если пользователь попросит, следующим отдельным действием можно стадировать и коммитить текущий набор изменений.

## Session 896 - 2026-05-27 08:38 - ручная приёмка `CIRCLE` sample подтверждена

Что сделано:
- пользователь вручную запустил `samples/graphics/primitives/circle_arcs_step_aspect.axm` в `Axiom.Ide` и прислал снимок результата;
- по снимку подтверждены ожидаемые элементы sample: полная окружность, дуги, радиальные линии у отрицательных углов, `STEP`-смещение центра и эллипсы с `aspect`;
- дополнительных расхождений в визуальном результате не выявлено.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- ручная визуальная приёмка `samples/graphics/primitives/circle_arcs_step_aspect.axm` в `Axiom.Ide` подтверждена снимком пользователя.

Решение:
- считать ручную приёмку `samples/graphics/primitives/circle_arcs_step_aspect.axm` подтверждённой;
- не вносить дополнительные изменения в код, manual или sample по итогам этого снимка.

Следующий точный шаг:
- подготовить итоговый отчёт по стабилизационной волне; если пользователь попросит, следующим отдельным действием можно стадировать и коммитить текущий набор изменений.
## Session 897 - 2026-05-27 09:16 - подготовлена отправка стабилизационной волны на GitHub

Что сделано:
- по запросу пользователя продолжена сдача текущего набора изменений через Git;
- повторно проверены `git diff --check` и `powershell -ExecutionPolicy Bypass -File .\scripts\repo-sanity.ps1`;
- подтверждено, что рабочая ветка `master` связана с `origin/master`, а удалённый репозиторий указывает на GitHub;
- текущий набор изменений готовится к стадированию, коммиту и отправке в `origin`.

Проверка:
- `git diff --check` прошёл без ошибок пробелов; остались только предупреждения Git о будущей нормализации LF/CRLF;
- `powershell -ExecutionPolicy Bypass -File .\scripts\repo-sanity.ps1` прошёл: временные каталоги вне `.artifacts` не найдены.

Решение:
- отправить текущую стабилизационную волну одним коммитом, потому что предшествующие записи журнала уже связывают изменения в одну завершённую проверенную поставку.

Следующий точный шаг:
- после успешной отправки продолжать уже с чистого рабочего дерева и новой задачи пользователя.

## Session 898 - 2026-05-28 10:22 - аудит реализации графических операторов

Что сделано:
- по запросу пользователя проверено текущее состояние графической ветки после стабилизационной волны;
- снят инвентарь рабочего дерева: до документных записей оставалась только пользовательская правка `samples/smoke/repair_plans_mojibake.axm` с локальными рабочими путями, к графике она не относится и не изменялась;
- повторно прогнаны адресные проверки runtime, IDE и execution по `SCREEN 10`, `GET` / `PUT`, `VIEW`, `WINDOW`, `PMAP`, `DRAW`, `CIRCLE` и мышиному координатному договору;
- повторно пройдены smoke-маршруты реального `Axiom.Ide` для `SCREEN 10` и `GET` / `PUT`;
- `docs/BYTECODE_COVERAGE.md` дополнен уточнением по графическому негативному покрытию: `GET` / `PUT`, `PMAP` и `DRAW` уже имеют явные негативные опоры, а `VIEW`, `WINDOW` и полный `PAINT` остаются частично покрытыми именно из-за отсутствия отдельных отрицательных `.axm`-samples.

Изменённые файлы:
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_CompiledDrawSupportsMovementModifiersColorScaleAngleMoveAndPaint|FullyQualifiedName~Execute_DrawSupportsMovementModifiersColorScaleAngleMoveAndPaint|FullyQualifiedName~Execute_CompiledCircleSupportsStepArcAndAspect|FullyQualifiedName~Execute_CircleSupportsStepArcAndAspect|FullyQualifiedName~Execute_CompiledGetAndPutSupportWindowCoordinatesAndPreservePosition|FullyQualifiedName~Execute_GetAndPutSupportWindowCoordinatesAndPreservePosition|FullyQualifiedName~Execute_CompiledPMap|FullyQualifiedName~Execute_PMap|FullyQualifiedName~Execute_CompiledOnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen|FullyQualifiedName~Execute_OnMouseCoordinatesMatchMouseFunctionsDuringActiveScreen" --logger "trx;LogFileName=graphics-runtime.trx"` — успешно, 12 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenModeSizeProviderTests|FullyQualifiedName~Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm|FullyQualifiedName~MouseMoveMessage_KeepsLastKnownCoordinatesUntilAnotherMouseInputArrives|FullyQualifiedName~Run_GetAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm|FullyQualifiedName~Run_PutAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm|FullyQualifiedName~DrawCircle_StepUsesCurrentGraphicsPositionAndNegativeAngleDrawsRadius" --logger "trx;LogFileName=graphics-ide.trx"` — успешно, 9 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Screen10BestFitAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~GetAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~PutAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~PMapAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ScreenViewAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ScreenWindowAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForGetPutTransfers|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForPMapAcrossViewAndWindow|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForViewScreenAndRelativeCoordinates|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForWindowScreenCartesianAndDraw|FullyQualifiedName~Run_WithBytecodeMode_GraphicsOperatorsMaintainPositionAcrossStepArcAndPaint" --logger "trx;LogFileName=graphics-execution.trx"` — успешно, 13 тестов;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --screen10-only` — успешно;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --get-put-only` — успешно;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` — успешно, временные каталоги вне `.artifacts` не найдены.

Решение:
- считать текущую реализацию графических операторов и связанных функций проверенной по адресному набору и smoke-маршрутам;
- не открывать новый графический оператор без отдельного договора в спецификации;
- ближайшим графическим долгом, если пользователь продолжит эту ветку, считать отрицательные runnable `.axm`-samples для `VIEW`, `WINDOW` и полного `PAINT` либо отдельный договор прокрутки `SCREEN 0` из `docs/FUTURE.md`.

Следующий точный шаг:
- выбрать один из трёх вариантов: добавить отрицательные графические acceptance-samples для `VIEW`, `WINDOW` и `PAINT`; открыть договор прокрутки `SCREEN 0`; либо перейти к следующей неграфической стабилизационной задаче.

## Session 899 - 2026-05-28 10:38 - отрицательные sample для VIEW, WINDOW и PAINT

Что сделано:
- добавлены runnable `.axm`-samples отрицательного поведения для трёх уже закрытых графических договоров;
- `samples/operators/view/invalid_bounds_error.axm` проверяет, что `VIEW SCREEN (20, 20)-(10, 30)` даёт `MB3007`;
- `samples/operators/window/zero_size_error.axm` проверяет, что `WINDOW SCREEN (100, 200)-(100, 210)` даёт `MB3007`;
- `samples/operators/paint/empty_pattern_error.axm` проверяет, что пустой строковый образец полного `PAINT` даёт `MB3007`;
- в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` добавлена проверка `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity`, которая запускает эти sample в режимах `Bytecode` и `Interpreter`, сверяет `MB3007`, отсутствие fallback у `BytecodeVm` и наличие координат диагностики;
- `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` обновлены под закрытие этого графического долга.

Изменённые файлы:
- `samples/operators/view/invalid_bounds_error.axm`
- `samples/operators/window/zero_size_error.axm`
- `samples/operators/paint/empty_pattern_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity" --logger "trx;LogFileName=graphics-negative.trx"` — успешно, 3 теста.

Решение:
- считать отрицательные runnable sample для `VIEW`, `WINDOW` и полного `PAINT` добавленными и подключёнными к сквозной проверке паритета interpreter / `BytecodeVm`;
- не менять manual, потому что текущие ограничения уже описаны на существующих страницах, а этот шаг добавляет недостающий проверочный слой.

Следующий точный шаг:
- выполнить финальные проверки по затронутым файлам: `repo-sanity`, `git diff --check` и при необходимости повторить адресный `Axiom.Execution.Tests` по новому фильтру после документных записей.

## Session 900 - 2026-05-28 12:17 - выбран договор прокрутки SCREEN 0

Что сделано:
- по запросу пользователя открыт отдельный договор прокрутки `SCREEN 0`;
- перечитаны активные документы проекта, текущий договор `SCREEN 0`, запись `docs/FUTURE.md` про отложенную прокрутку и фактический путь вывода через `ProgramConsoleService` / `ScreenTextBuffer`;
- в `docs/SPECIFICATION3.md` зафиксирован целевой договор ближайшего кодового среза: вертикальная прокрутка только в явном `SCREEN 0`, без word wrap, без горизонтальной прокрутки и без изменения графических режимов `SCREEN 1..10`;
- в `docs/PLANS3.md` записан следующий кодовый шаг по реализации прокрутки;
- в `docs/FUTURE.md` добавлена отметка, что тема прокрутки активирована и перенесена в текущий план.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/FUTURE.md`
- `docs/WORKLOG4.md`

Решение:
- первым кодовым срезом делать только вертикальную прокрутку текстового окна `SCREEN 0`;
- `PRINT`, `PRINT USING` и подтверждённый `INPUT` должны использовать один договор перехода на новую строку и прокрутки;
- графические режимы остаются без автопрокрутки, потому что текст в них используется как координатная надпись поверх framebuffer.

Проверка:
- `git diff --check -- docs/SPECIFICATION3.md docs/PLANS3.md docs/FUTURE.md docs/WORKLOG4.md` прошёл без ошибок пробелов; остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- реализовать договор в `ProgramConsoleService` и `ScreenTextBuffer`, добавить IDE-регрессии и runnable sample в `samples/graphics/screen/`, затем обновить manual уже по факту работающего поведения.

## Session 901 - 2026-05-28 15:29 - реализована прокрутка SCREEN 0

Что сделано:
- в `ProgramConsoleService` и `ScreenTextBuffer` включена вертикальная прокрутка только для явного `SCREEN 0`: видимое окно ограничено сеткой `80 x 25`, новая строка после нижней границы удаляет верхнюю строку и очищает новую строку `25`;
- графические режимы `SCREEN 1..10` сохранены без автопрокрутки и без word wrap;
- добавлены IDE-регрессии для прокрутки `SCREEN 0`, пустого `PRINT` на последней строке, клиппинга длинной строки и контроля отсутствия прокрутки в графическом `SCREEN`;
- добавлена end-to-end проверка через `ProgramRunService` и acceptance-проверка `BytecodeVm` для нового runnable sample;
- добавлен runnable sample `samples/graphics/screen/text_scroll.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/FUTURE.md`, `docs/manual/screen.html`, `docs/manual/graphics-text.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Ide/Graphics/ScreenTextBuffer.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/graphics/screen/text_scroll.axm`
- `docs/SPECIFICATION3.md`
- `docs/FUTURE.md`
- `docs/manual/screen.html`
- `docs/manual/graphics-text.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- не расширять этот шаг до горизонтальной прокрутки, истории удалённых строк или новых экранных команд;
- считать прокрутку пользовательским договором только для `SCREEN 0`, а не для графических режимов.

Проверки:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenZero|FullyQualifiedName~GraphicsScreen_DoesNotAutoScrollTextPastVisibleRows|FullyQualifiedName~Run_ScreenZeroScrollAcceptanceProgram"` проходит: 8 тестов;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenZeroTextScrollAcceptanceSample"` проходит: 1 тест;
- `node --check docs/manual/scripts/search-index.js` проходит.

Что остаётся:
- живая ручная приёмка нового sample в реальном `Axiom.Ide`.

Следующий точный шаг:
- открыть `samples/graphics/screen/text_scroll.axm` в `Axiom.Ide` и подтвердить, что после двадцать пятой строки верхние строки уходят вверх, последняя строка очищается для следующего вывода, а графические режимы `SCREEN 1..10` не получили автопрокрутку.

## Session 902 - 2026-05-28 15:41 - ручная приёмка прокрутки SCREEN 0 подтверждена

Что сделано:
- пользователь вручную проверил `samples/graphics/screen/text_scroll.axm` в `Axiom.Ide`;
- подтверждено, что прокрутка в sample работает ожидаемо.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать задачу по прокрутке `SCREEN 0` полностью закрытой, включая живую ручную приёмку;
- не добавлять новые изменения в код, sample или manual по этой теме без отдельного нового запроса.

Проверки:
- ручной запуск `samples/graphics/screen/text_scroll.axm` в `Axiom.Ide` подтверждён пользователем.

Следующий точный шаг:
- ждать следующую отдельную задачу пользователя.

## Session 903 - 2026-05-28 16:00 - для SCREEN 0 добавлены история и правый скролбар

Что сделано:
- по новому запросу пользователя поведение `SCREEN 0` расширено от простой автопрокрутки до истории строк с ручной прокруткой;
- `ProgramConsoleService` перестал отбрасывать ранние строки `SCREEN 0`: теперь буфер хранит историю текущего сеанса и отдельно держит первую видимую строку окна;
- `ScreenFramebuffer` научен рисовать только выбранное видимое окно текста, а не весь накопленный буфер целиком;
- `NativeGraphicsScreenWindow` получил правую вертикальную полосу прокрутки, синхронизацию её диапазона с историей `SCREEN 0` и обработку ручной прокрутки через `WM_VSCROLL`;
- `GraphicsText` в IDE-сервисе теперь отражает именно текущую видимую область окна, поэтому IDE-регрессии проверяют не только факт накопления истории, но и фактический вид после ручной прокрутки;
- runnable sample `samples/graphics/screen/text_scroll.axm` обновлён под новый договор: после переполнения окно должно показать полосу прокрутки и позволить вернуться к ранним строкам;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/screen.html`, `docs/manual/graphics-text.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Ide/Graphics/NativeGraphicsScreenWindow.cs`
- `src/Axiom.Ide/Graphics/ScreenFramebuffer.cs`
- `src/Axiom.Ide/Graphics/ScreenTextBuffer.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/ScreenTextScrollState.cs`
- `tests/Axiom.Ide.Tests/NativeGraphicsScreenWindowTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `samples/graphics/screen/text_scroll.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/screen.html`
- `docs/manual/graphics-text.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- историю и правый скролбар давать только текстовому `SCREEN 0`, а графические режимы `SCREEN 1..10` оставить без ручной прокрутки и без изменения старого клиппинга;
- ручная прокрутка должна влиять только на видимую область окна, а не на семантику `LOCATE`, `PRINT`, `PRINT USING` и `INPUT`.

Проверки:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenZero|FullyQualifiedName~GraphicsScreen_DoesNotAutoScrollTextPastVisibleRows|FullyQualifiedName~NativeGraphicsScreenWindowTests"` проходит: 24 теста;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore` проходит: 436 тестов;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~ScreenZeroTextScrollAcceptanceSample"` проходит: 1 тест.
- `node --check docs/manual/scripts/search-index.js` проходит;
- `git diff --check` по затронутым файлам не нашёл ошибок пробелов; остались только предупреждения Git о будущей нормализации LF/CRLF.

Что остаётся:
- живая ручная приёмка появления полосы прокрутки и возврата к ранним строкам в реальном окне `Axiom.Ide`.

Следующий точный шаг:
- открыть `samples/graphics/screen/text_scroll.axm` в `Axiom.Ide` и визуально подтвердить, что справа появляется полоса прокрутки и через неё можно вернуться к ранним строкам без поломки обычного вывода внизу окна.

## Session 904 - 2026-05-28 17:40 - VIEW PRINT выведен в справочник

Что сделано:
- продолжена разработка `VIEW PRINT` по запросу пользователя;
- подтверждено, что parser, binder, interpreter, `BytecodeVm`, IDE-host и sample для `VIEW PRINT` уже присутствуют в рабочем дереве;
- закрыт недостающий пользовательский слой: `docs/manual/view.html` описывает формы `VIEW PRINT` и `VIEW PRINT top TO bottom`, границы строк, прокрутку внутри диапазона, сброс и диагностику;
- `docs/manual/graphics.html`, `docs/manual/graphics-text.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js` обновлены для навигации, поиска и соседних ссылок;
- добавлена явная сквозная acceptance-проверка `ViewPrintAcceptanceSample_RunsOnBytecodeVm` для `samples/operators/view-print/scrolling_region.axm`;
- общий audit-путь позитивных sample теперь отдаёт `ESC` для `samples/operators/view-print/`, чтобы проверочный пример не зависал в цикле ожидания клавиши;
- `docs/SPECIFICATION3.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` обновлены под фактический договор и состояние покрытия.

Изменённые файлы:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/manual/view.html`
- `docs/manual/graphics.html`
- `docs/manual/graphics-text.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать `VIEW PRINT` частью графического текстового слоя `SCREEN 1..10`, независимой от пиксельного viewport обычного `VIEW`;
- не расширять этот шаг до новых команд управления прокруткой, истории строк в графических режимах или горизонтального поведения;
- отдельный отрицательный `.axm`-sample для неверного диапазона `VIEW PRINT` оставить возможным следующим улучшением, потому что runtime- и VM-негативные проверки уже есть, а текущий шаг закрывает discoverability и позитивную приёмку.

Проверки:
- первая попытка `ViewPrintAcceptanceSample_RunsOnBytecodeVm` с `--no-restore` не дошла до выполнения из-за отсутствующего локального пакета `xunit.analyzers 1.18.0`;
- повтор с восстановлением пакетов выявил реальный дефект bytecode-валидации: `Opcode.ViewPrint` не был включён в верхний dispatch валидатора и давал `BC4011`;
- после минимальной правки валидатора следующий прогон выявил недостающий fake-host слой в `QueueKeyboardInputProvider`: acceptance-host не реализовывал `SetTextViewport`, хотя реальный IDE-host уже реализовывал его;
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` обновлён: `Opcode.ViewPrint` валидируется как инструкция с одним операндом и корректно участвует в расчёте стека;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` обновлён: fake-host теперь принимает корректный диапазон `VIEW PRINT` по числу строк текущего `SCREEN`;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~ViewPrintAcceptanceSample_RunsOnBytecodeVm" --logger "trx;LogFileName=view-print-acceptance.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~scrolling_region" --logger "trx;LogFileName=view-print-audit.trx"` проходит: 1 тест;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит, временные каталоги вне `.artifacts` не найдены;
- `git diff --check -- ...` по затронутым файлам проходит, остаются только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- выполнить проверки из списка выше; после зелёного результата вручную открыть `samples/operators/view-print/scrolling_region.axm` в `Axiom.Ide` и подтвердить, что прокручиваются только строки `10..15`, а строки вне диапазона остаются на месте.

## Session 905 - 2026-05-28 19:22 - ручная приёмка VIEW PRINT подтверждена

Что сделано:
- пользователь вручную запустил `samples/operators/view-print/scrolling_region.axm` в `Axiom.Ide`;
- подтверждено, что фактический результат совпадает с ожидаемым описанием: верхняя строка содержит `VIEW PRINT sample finished`, строка `VIEW PRINT viewport:` остаётся перед окном прокрутки, строки `10..15` показывают последние строки вывода viewport, а строка `ESC - выход` ниже диапазона остаётся на месте.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- ручная визуальная приёмка `samples/operators/view-print/scrolling_region.axm` в `Axiom.Ide` подтверждена пользователем.

Решение:
- считать позитивный `VIEW PRINT` sample принятым вручную;
- не менять код, sample или manual по итогам этой проверки.

Следующий точный шаг:
- если продолжать `VIEW PRINT`, добавить отдельный отрицательный runnable `.axm`-sample для неверного диапазона строк и подключить его к паритетной проверке interpreter / `BytecodeVm`; иначе переходить к следующей графической или стабилизационной задаче.

## Session 906 - 2026-05-28 19:36 - VIEW PRINT получил отрицательный sample-паритет

Что сделано:
- добавлен runnable negative sample `samples/operators/view-print/invalid_rows_error.axm` для неверного диапазона строк `VIEW PRINT`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` обновлён: `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` теперь включает и `VIEW PRINT`, поэтому один и тот же sample прогоняется через `Interpreter` и `BytecodeVm`;
- `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` обновлены под закрытый статус негативного sample-паритета.

Изменённые файлы:
- `samples/operators/view-print/invalid_rows_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать отрицательный runnable sample и паритетную execution-проверку для неверного диапазона `VIEW PRINT` закрытыми;
- не менять код runtime, manual и спецификацию, потому что пользовательское поведение оператора не изменялось.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~ViewPrintAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity" --logger "trx;LogFileName=view-print-negative-parity.trx"` проходит: 5 тестов;
- `git diff --check -- "samples/operators/view-print/invalid_rows_error.axm" "tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs"` не нашёл ошибок пробелов; Git вывел только стандартное предупреждение о будущей нормализации LF/CRLF.

Следующий точный шаг:
- если продолжать графический слой, выбрать следующий оператор или режим, у которого ещё нет отдельного runnable negative sample и явной parity-проверки interpreter / `BytecodeVm`; ветку `VIEW PRINT` считать закрытой.

## Session 907 - 2026-05-28 21:23 - PMAP получил отрицательный sample-паритет

Что сделано:
- продолжена графическая стабилизационная ветка после закрытия `VIEW PRINT`;
- выбран следующий узкий пробел: у `PMAP` уже были runtime-проверки неверного селектора, но не было отдельного runnable negative sample;
- добавлен `samples/functions/pmap/invalid_selector_error.axm`, который вызывает `PMAP(1, 4)` в графическом режиме и ожидает `MB3007`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` обновлён: `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` теперь прогоняет этот sample через `Bytecode` и `Interpreter`;
- `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` обновлены под закрытый статус негативного sample-паритета `PMAP`.

Изменённые файлы:
- `samples/functions/pmap/invalid_selector_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать отрицательный runnable sample и паритетную execution-проверку для неверного селектора `PMAP` закрытыми;
- не менять runtime, manual и спецификацию, потому что пользовательское поведение функции не изменялось, а `docs/manual/pmap.html` уже описывает ошибку селектора вне `0..3`.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity" --logger "trx;LogFileName=pmap-negative-parity.trx"` — успешно, 5 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` — успешно, временные каталоги вне `.artifacts` не найдены;
- `git diff --check -- "samples/functions/pmap/invalid_selector_error.axm" "tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs" "docs/BYTECODE_COVERAGE.md" "docs/PLANS3.md" "docs/WORKLOG4.md"` — ошибок пробелов нет, остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- выбрать следующий графический элемент без отдельного negative sample либо перейти к неграфической стабилизационной задаче; ветку `PMAP` считать закрытой.

## Session 908 - 2026-05-28 22:11 - выбран следующий графический оператор

Что сделано:
- перечитаны обязательные активные документы проекта и текущие записи по графической ветке;
- проверен фактический набор реализованных графических операторов и функций в parser, справочнике и sample-структуре;
- выбран следующий самостоятельный графический оператор: `PALETTE`.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- `PALETTE` выбран как следующий логичный BASIC-подобный оператор после закрытых примитивов, координатных преобразований, блочного переноса и текстового окна `VIEW PRINT`;
- `PCOPY` пока не выбран, потому что он потребовал бы сначала проектировать видеостраницы, которых в текущей модели `SCREEN` нет;
- первый шаг по `PALETTE` должен быть проектным: зафиксировать точные формы оператора, сброс палитры и границы возможной формы `PALETTE USING`.

Проверки:
- тесты не запускались, потому что код и поведение языка в этой сессии не менялись.

Следующий точный шаг:
- открыть договор `PALETTE` в `docs/SPECIFICATION3.md`, затем реализовать его сквозным срезом parser/semantics/runtime/bytecode/IDE-host с sample, manual и реестром покрытия.

## Session 909 - 2026-05-29 00:02 - PALETTE доведён до рабочего договора

Что сделано:
- реализован и закрыт первый срез `PALETTE`: форма `PALETTE` сбрасывает палитру, форма `PALETTE index, color` переназначает один индекс палитры;
- добавлена недостающая привязка `PaletteStatementSyntax` к location в binder, чтобы семантика и runtime-диагностика получали корректную позицию оператора;
- добавлены runtime- и `BytecodeVm`-проверки вызова `ResetPalette()` / `SetPaletteColor(index, color)` и ошибки `MB3010` без активного графического `SCREEN`;
- добавлена IDE-проверка, что `ProgramConsoleService` меняет видимое BGRA-отображение уже нарисованного пикселя и возвращает его после `ResetPalette()`;
- добавлен runnable sample `samples/operators/palette/change_and_reset_colors.axm` и execution-проверка `PaletteAcceptanceSample_RunsOnBytecodeVm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/palette.html`, `docs/manual/graphics.html`, `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы по этому срезу:
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/palette/change_and_reset_colors.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/palette.html`
- `docs/manual/graphics.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- имя `PALETTE` оставлено по BASIC-линейке и разделу Зельднера `PALETTE, PALETTE USING`;
- `PALETTE USING` не входит в первый срез и зафиксирован как отдельная будущая возможность;
- пиксели framebuffer остаются индексными: `PALETTE` меняет отображение индекса, а `POINT(x, y)` возвращает сохранённый индекс.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 2 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 2 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 4 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 3 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~palette"` проходит: 1 тест;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит;
- `git diff --check` ошибок пробелов не нашёл, остались только предупреждения Git о будущей нормализации LF/CRLF;
- первый `dotnet build Axiom.sln --no-restore` снова упал на известном `NETSDK1064` по `xunit.analyzers 1.18.0` в части тестовых проектов, после `dotnet restore Axiom.sln` повторный `dotnet build Axiom.sln --no-restore` прошёл без предупреждений и ошибок.

Следующий точный шаг:
- вручную открыть `samples/operators/palette/change_and_reset_colors.axm` в `Axiom.Ide`, запустить и подтвердить, что линия меняет видимый цвет после `PALETTE 1, 196`, а затем возвращается к стандартному цвету после `PALETTE`.

## Session 910 - 2026-05-29 00:32 - проверена кодировка PLANS3

Что сделано:
- проверен `docs/PLANS3.md` как UTF-8, отдельно от вывода PowerShell;
- найден старый повреждённый блок с невосстановимыми маркерами битой кодировки;
- вместо нечитаемого фрагмента оставлена короткая служебная пометка без догадок о потерянном тексте;
- буквальные поисковые маркеры в `docs/PLANS3.md` и `docs/WORKLOG4.md` переписаны в ASCII-форме.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- не восстанавливать историческую запись по памяти или предположениям;
- свежие записи по `PALETTE` оставить без смысловых изменений.

Проверка:
- проверка UTF-8 и поиск маркеров битой кодировки выполняются после записи этой сессии отдельной командой.

Следующий точный шаг:
- завершить повторный аудит `PALETTE` и ручную визуальную приёмку `samples/operators/palette/change_and_reset_colors.axm` в `Axiom.Ide`.

## Session 911 - 2026-05-31 18:16 - реализован PALETTE USING

Что сделано:
- проверено, что базовый `PALETTE` уже был реализован и закрыт в сессии 909;
- реализована форма `PALETTE USING array` для одномерного числового массива;
- парсер распознаёт `PALETTE USING`, связыватель проверяет массивность, одномерность и числовой тип элемента;
- интерпретатор и `BytecodeVm` применяют видимые элементы массива к индексам палитры от `0` до `255` через существующий договор хоста `SetPaletteColor(index, color)`;
- добавлен запускаемый пример `samples/operators/palette/using_array_colors.axm`;
- исправлен сценарный ввод старого приёмочного теста `PaletteAcceptanceSample_RunsOnBytecodeVm`: пример содержит два `SLEEP` без длительности и финальное ожидание `ESC`, поэтому тесту нужны три клавиши;
- обновлены спецификация, план, справочник и реестр покрытия байткодного пути.

Изменённые файлы по этому срезу:
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/palette/using_array_colors.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/palette.html`
- `docs/manual/graphics.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- `PALETTE USING` сохраняет BASIC-линейное имя и не вводит новое пользовательское обозначение;
- нижняя граница массива не считается индексом палитры: первый видимый элемент всегда задаёт индекс `0`;
- лишние элементы после первых `256` игнорируются, а недостающие индексы палитры остаются без изменения.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 3 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 4 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Palette"` проходит: 6 тестов;
- `dotnet build tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~PaletteAcceptanceSample_RunsOnBytecodeVm"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~PaletteUsingAcceptanceSample_RunsOnBytecodeVm"` проходит: 1 тест;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `dotnet build Axiom.sln --no-restore` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Ограничение проверки:
- широкий запуск `Axiom.Execution.Tests` с фильтром `FullyQualifiedName~Palette` несколько раз упирался в `MSB4166` и тайм-аут рабочих узлов; прямые приёмочные тесты `PaletteAcceptanceSample_RunsOnBytecodeVm` и `PaletteUsingAcceptanceSample_RunsOnBytecodeVm` после отдельной сборки проходят.

Следующий точный шаг:
- вручную открыть `samples/operators/palette/change_and_reset_colors.axm` и `samples/operators/palette/using_array_colors.axm` в `Axiom.Ide`, запустить и подтвердить видимое изменение палитры и сброс;
- после этого выбрать следующий графический оператор отдельным проектным решением.

## Session 912 - 2026-05-31 21:41 - очищены свежие русские формулировки

Что сделано:
- выполнена обещанная редакционная чистка свежих проектных документов после проверенного среза `PALETTE USING` / `PCOPY`;
- в новых блоках спецификации, архитектуры, плана и реестра покрытия заменены смешанные формулировки вроде `host-договор`, `fallback`, `page flipping`, `framebuffer`, `runnable sample`, `parser` и `binder` на русские или уже принятые проектные формулировки;
- связанные страницы справочника `graphics.html`, `screen.html`, `pcopy.html` и поисковый индекс выровнены там, где новая графическая подача говорила о кадровом буфере и экранных страницах;
- старые исторические записи с прежними терминами не переписывались массово, чтобы не раздувать редакционную правку и не менять историю проекта.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/manual/graphics.html`
- `docs/manual/pcopy.html`
- `docs/manual/screen.html`
- `docs/manual/scripts/search-index.js`

Решение:
- считать правку только редакционной: поведение языка, код, тесты, `.axm`-примеры и покрытие `BytecodeVm` не менялись;
- для свежей русской проектной прозы использовать `договор хоста`, `хост IDE`, `кадровый буфер`, `запускаемый пример`, `приёмочная проверка`, `откат на интерпретатор` и похожие русские формы вместо смешанных англо-русских оборотов.

Проверки:
- `node --check docs/manual/scripts/search-index.js` проходит;
- `git diff --check -- "docs/SPECIFICATION3.md" "docs/ARCHITECTURE.md" "docs/PLANS3.md" "docs/WORKLOG4.md" "docs/BYTECODE_COVERAGE.md" "docs/manual/screen.html" "docs/manual/pcopy.html" "docs/manual/graphics.html" "docs/manual/scripts/search-index.js"` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF;
- адресный поиск `host-договор|page flipping|page-flipping` подтвердил, что в свежих блоках `PALETTE` / `PCOPY` эти целевые формулировки убраны; найденные остатки относятся к более старым историческим записям.

Следующий точный шаг:
- вернуться к предметной графической ветке: вручную открыть `samples/operators/palette/change_and_reset_colors.axm` и `samples/operators/palette/using_array_colors.axm` в `Axiom.Ide`, запустить и подтвердить видимое изменение палитры и сброс; после этого выбирать следующий графический оператор отдельным проектным решением.

## Session 913 - 2026-05-31 22:18 - проверена полнота договора PCOPY

Что сделано:
- перечитаны обязательные активные документы проекта и выполнена предметная сверка `PCOPY` по реализации, спецификации, справочнику, примерам и приёмочным проверкам;
- подтверждено, что фактический хост IDE копирует и цветовые индексы пикселей, и палитру: `ScreenFramebuffer.CopyFrom(...)` копирует оба массива, а `ProgramConsoleServiceTests.ScreenPages_CopyScreenPageCopiesPixelsAndPaletteToVisiblePage` проверяет видимый результат;
- позитивный пример `samples/operators/pcopy/page_copy_buffer.axm` уточнён так, чтобы он одновременно проверял сохранение пиксельного индекса через `POINT` и копирование палитры через `PALETTE 1, 196`;
- добавлены отрицательные запускаемые примеры `samples/graphics/screen/invalid_page_error.axm` и `samples/operators/pcopy/text_screen_error.axm`;
- приёмочная проверка `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` расширена на неверную страницу `SCREEN` и `PCOPY` в текстовом `SCREEN 0`;
- `docs/manual/pcopy.html`, `docs/manual/screen.html`, `docs/manual/graphics.html`, `docs/manual/scripts/search-index.js`, `docs/SPECIFICATION3.md` и `docs/BYTECODE_COVERAGE.md` синхронизированы с уточнённым договором.

Изменённые файлы:
- `samples/operators/pcopy/page_copy_buffer.axm`
- `samples/operators/pcopy/text_screen_error.axm`
- `samples/graphics/screen/invalid_page_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/manual/pcopy.html`
- `docs/manual/screen.html`
- `docs/manual/graphics.html`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать договор `PCOPY` полным на текущем уровне `0..3` страниц: страницы `SCREEN`, копирование пикселей и палитры, запрет вне графического `SCREEN` и отрицательный паритет закрыты;
- отдельный отрицательный пример для каждой комбинации неверной страницы пока не нужен, потому что текущие примеры проверяют границу договора без дублирования.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~PCopyAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity" --logger "trx;LogFileName=pcopy-contract-parity.trx"` проходит: 9 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback&DisplayName~pcopy" --logger "trx;LogFileName=pcopy-positive-audit.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~CopyScreenPageCopiesPixelsAndPalette" --logger "trx;LogFileName=pcopy-ide-palette.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "FullyQualifiedName~PCopy" --logger "trx;LogFileName=pcopy-parsing.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~PCopy" --logger "trx;LogFileName=pcopy-semantics.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~PCopy" --logger "trx;LogFileName=pcopy-runtime.trx"` проходит: 4 теста;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj --no-restore` проходит: 45 проверок;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит, временные каталоги вне `.artifacts` не найдены;
- `git diff --check` по затронутым файлам не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- вручную открыть в `Axiom.Ide` и визуально проверить `samples/operators/pcopy/page_copy_buffer.axm`, `samples/operators/palette/change_and_reset_colors.axm` и `samples/operators/palette/using_array_colors.axm`; после этого выбирать следующий графический оператор только отдельным проектным решением.

## Session 914 - 2026-05-31 23:34 - закрыта ручная приёмка PALETTE и PCOPY

Что сделано:
- пользователь подтвердил ручную проверку `samples/operators/palette/change_and_reset_colors.axm`, `samples/operators/palette/using_array_colors.axm` и `samples/operators/pcopy/page_copy_buffer.axm` в `Axiom.Ide`;
- договоры `PALETTE`, `PALETTE USING` и `PCOPY` закрыты на текущем уровне ручной приёмки;
- в `docs/PLANS3.md` добавлен подробный план следующего графического направления: современный слой `IMAGE` / `DRAWIMAGE`, затем `SPRITE` / `DRAWSPRITE`, затем отдельные игровые удобства только после проверки первых двух срезов.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- не возвращаться к `PALETTE` и `PCOPY` без новой ошибки или нового пользовательского требования;
- не брать ближайшим шагом `BLOAD` и `BSAVE`, потому что это DOS-ориентированный слой работы с участками памяти, а не лучший путь к современной игровой графике;
- следующим направлением считать проектирование первого современного графического договора: `IMAGE`, `LOADIMAGE`, `IMAGEWIDTH`, `IMAGEHEIGHT`, `DRAWIMAGE`.

Проверки:
- поведение языка и код не менялись;
- `git diff --check -- "docs/PLANS3.md" "docs/WORKLOG4.md"` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` прошёл: временные каталоги вне `.artifacts` не найдены.

Следующий точный шаг:
- открыть договор первого современного графического среза в `docs/SPECIFICATION3.md`, окончательно зафиксировать тип `IMAGE`, загрузку, отрисовку, работу с полноцветными пикселями, относительными путями и взаимодействие с `VIEW`, `WINDOW`, `PALETTE`, `GET`, `PUT` и `PCOPY`; затем переходить к реализации.

## Session 915 - 2026-05-31 23:40 - открыт договор IMAGE и DRAWIMAGE

Что сделано:
- открыт целевой договор первого современного графического среза в `docs/SPECIFICATION3.md`;
- зафиксированы тип `IMAGE`, функции `LOADIMAGE`, `IMAGEWIDTH`, `IMAGEHEIGHT` и оператор `DRAWIMAGE`;
- сверены ближайшие BASIC-семейные ориентиры: в QuickBASIC 4.5 современного слоя изображений не было, а QB64 Phoenix использует `_LOADIMAGE` и `_PUTIMAGE`; для Axiom выбраны имена `LOADIMAGE` и `DRAWIMAGE` без подчёркивания и без модели числовых дескрипторов;
- зафиксированы правила относительных путей, полноцветного слоя кадрового буфера, альфа-композиции, диагностики, взаимодействия с `VIEW`, `WINDOW`, `PALETTE`, `GET`, `PUT`, `POINT` и `PCOPY`;
- `docs/PLANS3.md` обновлён: следующим шагом теперь является реализация первого `IMAGE`-среза, а не дальнейшее проектирование.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- первый современный графический кодовый срез ограничивается `IMAGE`, `LOADIMAGE`, `IMAGEWIDTH`, `IMAGEHEIGHT` и `DRAWIMAGE`;
- тип `IMAGE` должен быть настоящим ссылочным типом языка, а не числовым дескриптором;
- декодирование файлов изображений должно идти через абстракцию хоста, чтобы не привязывать `Axiom.Runtime` к WPF;
- `SPRITE`, `LOADSPRITE`, `DRAWSPRITE`, столкновения и тайловые карты остаются следующими договорами после реализации и ручной приёмки изображений.

Проверки:
- поведение языка и код не менялись;
- `git diff --check -- "docs/SPECIFICATION3.md" "docs/PLANS3.md" "docs/WORKLOG4.md"` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` прошёл: временные каталоги вне `.artifacts` не найдены.

Следующий точный шаг:
- начать реализацию первого современного графического среза по договору `IMAGE` / `DRAWIMAGE`: лексер, синтаксис, парсер, семантика, runtime, байткод, слой запуска, хост IDE, автоматические проверки, запускаемый пример, справочник и реестр покрытия байткодной виртуальной машины.

## Session 916 - 2026-06-01 23:40 - реализован IMAGE и DRAWIMAGE

Что сделано:
- реализован первый современный графический срез: тип `IMAGE`, функции `LOADIMAGE`, `IMAGEWIDTH`, `IMAGEHEIGHT` и оператор `DRAWIMAGE`;
- лексер, синтаксис, парсер, связыватель, интерпретатор, `BytecodeVm`, понижение, компиляция байткода, проверка байткодного модуля, слой запуска и хост IDE синхронизированы;
- добавлен `RuntimeImageValue` для ширины, высоты и неизменяемых BGRA-пикселей изображения;
- `ProgramConsoleService` загружает изображения средствами Windows/WPF, разрешает относительные пути от каталога сохранённого `.axm`-файла и рисует изображение в полноцветный слой кадрового буфера;
- `ScreenFramebuffer` теперь хранит индексный слой, палитру и полноцветный слой, применяет альфа-канал при `DRAWIMAGE`, возвращает ближайший индекс стандартной палитры через `POINT` и копирует полноцветный слой через `PCOPY`;
- добавлены запускаемые примеры `samples/graphics/images/load_and_draw_image.axm` и `samples/graphics/images/missing_image_error.axm`;
- добавлена страница справочника `docs/manual/images.html`, обновлены обзор графики, страница `SCREEN`, страница типов, навигация и поиск справочника;
- обновлены `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md` и `docs/BYTECODE_COVERAGE.md`.

Изменённые файлы:
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Input.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Runtime/Values/RuntimeImageValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Graphics/ScreenFramebuffer.cs`
- `src/Axiom.Ide/Services/Input/IProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ProgramKeyboardInputRouterTests.cs`
- `tests/Axiom.Ide.Tests/ProgramMouseInputRouterTests.cs`
- `samples/graphics/images/load_and_draw_image.axm`
- `samples/graphics/images/missing_image_error.axm`
- `docs/manual/images.html`
- `docs/manual/graphics.html`
- `docs/manual/screen.html`
- `docs/manual/data-types.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `README.md`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/WORKLOG4.md`

Решения:
- `LOADIMAGE` и `DRAWIMAGE` оставлены как Axiom-имена без QB64-подчёркивания и без числовых дескрипторов;
- `DRAWIMAGE` пишет полноцветные пиксели в отдельный слой кадрового буфера, а старые индексные операторы при записи в точку снова делают её индексной;
- `PALETTE` не влияет на полноцветные пиксели, а `POINT` и `GET` по ним возвращают ближайший индекс стандартной палитры;
- относительные пути к изображениям при запуске из IDE считаются от каталога сохранённого файла программы;
- производительный сценарий в `samples/smoke/engine-compare/` не добавлялся, поэтому `docs/BYTECODE_PERF_MATRIX.md` не менялся.

Проверки:
- `dotnet build Axiom.sln --no-restore` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "FullyQualifiedName~Image|FullyQualifiedName~DrawImage" --logger "trx;LogFileName=image-parsing.trx"` проходит: 3 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~Image|FullyQualifiedName~DrawImage" --logger "trx;LogFileName=image-semantics.trx"` проходит: 4 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Image|FullyQualifiedName~DrawImage" --logger "trx;LogFileName=image-runtime.trx"` проходит: 6 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Image|FullyQualifiedName~DrawImage|FullyQualifiedName~ResourceBaseDirectory|FullyQualifiedName~SourceFilePath" --logger "trx;LogFileName=image-ide-rerun.trx"` проходит: 5 тестов;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --no-build --results-directory .artifacts/test-results/Axiom.Execution.Tests --filter "FullyQualifiedName~DrawImageAcceptanceSample|FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity" --logger "trx;LogFileName=image-execution-rerun.trx"` проходит: 10 тестов;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj --no-restore` проходит: 45 проверок;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Ограничения проверки:
- первый широкий запуск группы `Axiom.Execution.Tests` с фильтром `DrawImageAcceptanceSample|GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` завис из-за нехватки одной клавиши в scripted-вводе позитивного примера; после добавления `SPACE` перед `ESC` повторный запуск прошёл;
- ручная визуальная проверка в `Axiom.Ide` ещё не выполнена.

Следующий точный шаг:
- вручную открыть `samples/graphics/images/load_and_draw_image.axm` в `Axiom.Ide`, запустить и подтвердить, что значок IDE выводится в натуральном размере и в масштабе `64 x 64`; затем вручную проверить `samples/graphics/images/missing_image_error.axm` как отрицательный пример `LOADIMAGE`.

## Session 917 - 2026-06-01 23:49 - закрыта ручная приёмка IMAGE и открыт договор SPRITE

Что сделано:
- пользователь подтвердил ручную проверку `samples/graphics/images/load_and_draw_image.axm` в `Axiom.Ide`: пример правильно выводит две картинки;
- пользователь подтвердил ручную проверку `samples/graphics/images/missing_image_error.axm` в `Axiom.Ide`: отрицательный пример показывает сообщение об ошибке;
- в `docs/SPECIFICATION3.md` статус `IMAGE` / `DRAWIMAGE` дополнен ручной приёмкой;
- открыт следующий проектный договор для спрайтового среза: `SPRITE`, `LOADSPRITE`, `SPRITEWIDTH`, `SPRITEHEIGHT`, `SPRITECOUNT` и `DRAWSPRITE`;
- в `docs/PLANS3.md` зафиксировано, что `IMAGE` закрыт, а следующим кодовым шагом становится реализация `SPRITE` / `DRAWSPRITE`.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- считать первый современный графический срез `IMAGE` / `DRAWIMAGE` закрытым на текущем уровне;
- первый спрайтовый срез должен работать как спрайтовый лист с одинаковыми кадрами, нулевой нумерацией кадров и обходом кадров слева направо и сверху вниз;
- физика, столкновения, тайловые карты, объектная модель, поворот, зеркальное отражение, фильтры и именованные анимации не входят в ближайший срез.

Проверки:
- код не менялся;
- после записи документов нужно выполнить `git diff --check` по затронутым документам.

Следующий точный шаг:
- реализовать договор `SPRITE` / `DRAWSPRITE` по цепочке лексер, синтаксис, парсер, семантика, runtime, байткод, слой запуска, хост IDE, автоматические проверки, запускаемый пример, справочник и реестр покрытия байткодной виртуальной машины.

## Session 918 - 2026-06-01 23:59 - исправлена IDE-поддержка IMAGE-слов

Что сделано:
- по замечанию пользователя найден пропущенный IDE-слой нового `IMAGE`-среза;
- в `Axiom.xshd` и `Axiom.Dark.xshd` добавлен `DRAWIMAGE` как ключевое слово;
- в эти же словари добавлен тип `IMAGE`;
- в список встроенных функций подсветки добавлены `LOADIMAGE`, `IMAGEWIDTH` и `IMAGEHEIGHT`;
- тест покрытия словаря подсветки обновлён так, чтобы `IMAGE` больше не считался отсутствующим типом;
- добавлен отдельный тест на наличие `DRAWIMAGE`, `IMAGE`, `LOADIMAGE`, `IMAGEWIDTH` и `IMAGEHEIGHT` в словаре подсветки;
- добавлен тест форматирования, который проверяет классическое и современное отображение новых слов.

Изменённые файлы:
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- это исправление IDE-поддержки уже реализованного языка, а не изменение пользовательской семантики `IMAGE` / `DRAWIMAGE`;
- новые `.axm`-примеры, справочная страница языка и реестр покрытия `BytecodeVm` не требуются, потому что runtime-поведение не менялось;
- будущий `SPRITE`-срез должен обновлять IDE-словарь подсветки и форматирования сразу вместе с лексером и семантикой.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Highlighting_VocabularyContainsImageWords|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes|FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions|FullyQualifiedName~FormatLine_FormatsImageKeywordsTypesAndBuiltIns" --logger "trx;LogFileName=image-ide-vocabulary-formatting.trx"` проходит: 4 теста;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `git diff --check` по изменённым IDE-словарям и тестам не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- продолжить реализацию `SPRITE` / `DRAWSPRITE`, начиная с лексера, синтаксиса и IDE-словарей одновременно, чтобы новый пользовательский словарь сразу был виден в редакторе.

## Session 919 - 2026-06-02 00:55 - реализован первый срез SPRITE / DRAWSPRITE

Что сделано:
- реализован договор спрайтового листа: тип `SPRITE`, функции `LOADSPRITE`, `SPRITEWIDTH`, `SPRITEHEIGHT`, `SPRITECOUNT` и оператор `DRAWSPRITE`;
- добавлены ключевые слова `SPRITE` и `DRAWSPRITE` в лексер, синтаксис, парсер, типовую систему и IDE-словарь;
- интерпретатор и `BytecodeVm` получили общий runtime-договор пустого спрайта, подсчёта кадров, проверки номера кадра, натурального вывода и масштабирования;
- host-слой IDE загружает спрайтовый лист через тот же путь, что `LOADIMAGE`, и рисует выбранный кадр в полноцветный слой кадрового буфера;
- добавлены проверки парсера, связывателя, интерпретатора, `BytecodeVm`, слоя запуска, IDE-хоста, подсветки и форматирования;
- добавлены runnable-примеры `samples/graphics/sprites/walk_cycle.axm`, `samples/graphics/sprites/invalid_frame_error.axm` и постоянный ресурс `samples/graphics/sprites/assets/walk_sheet.png`;
- добавлена страница справочника `docs/manual/sprites.html`, обновлены графический обзор, страница типов, навигация, поисковый индекс, план и реестр покрытия bytecode.

Изменённые файлы:
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/GraphicsSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Input.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Runtime/Values/RuntimeSpriteValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Host/IGraphicsHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Graphics.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `src/Axiom.Ide/Graphics/ScreenFramebuffer.cs`
- `src/Axiom.Ide/Services/Input/IdeInputProvider.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `samples/graphics/sprites/walk_cycle.axm`
- `samples/graphics/sprites/invalid_frame_error.axm`
- `samples/graphics/sprites/assets/walk_sheet.png`
- `docs/manual/sprites.html`
- `docs/manual/graphics.html`
- `docs/manual/data-types.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- кадры `SPRITE` нумеруются с нуля и обходятся слева направо, сверху вниз;
- лист меньше размера кадра считается загруженным, но без доступных кадров: `SPRITEWIDTH` / `SPRITEHEIGHT` сохраняют заданный размер, `SPRITECOUNT` возвращает `0`, а `DRAWSPRITE` сообщает `MB3005`;
- `DRAWSPRITE` использует тот же полноцветный слой, альфа-наложение, `VIEW`, `WINDOW`, `POINT`, `GET` и `PCOPY` договор, что `DRAWIMAGE`;
- `docs/BYTECODE_PERF_MATRIX.md` не обновлялся, потому что отдельный производительный сценарий не добавлялся.

Проверки:
- `dotnet build src/Axiom.Runtime/Axiom.Runtime.csproj --no-restore /m:1` проходит;
- `dotnet build src/Axiom.Ide/Axiom.Ide.csproj --no-restore /m:1` проходит;
- `dotnet build tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore /m:1` проходит;
- `dotnet build tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore /m:1` проходит;
- `dotnet build tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore /m:1` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSprite" --logger "trx;LogFileName=sprite-parsing.trx"` проходит: 2 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite" --logger "trx;LogFileName=sprite-semantics.trx"` проходит: 3 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite" --logger "trx;LogFileName=sprite-runtime.trx"` проходит: 6 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSprite|FullyQualifiedName~GraphicsNegativeAcceptanceSamples" --logger "trx;LogFileName=sprite-execution.trx"` проходит: 11 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes|FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions" --logger "trx;LogFileName=sprite-ide.trx"` проходит: 6 тестов;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `dotnet build Axiom.sln --no-restore /m:1` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF;
- широкий `dotnet build Axiom.sln --no-restore` был остановлен по таймауту 120 секунд с `MSB4166` на дочерних узлах MSBuild; после этого затронутые проекты собраны последовательно с `/m:1`.

Следующий точный шаг:
- вручную открыть `samples/graphics/sprites/walk_cycle.axm` в `Axiom.Ide`, запустить в режиме `Bytecode` и подтвердить два кадра спрайта и финальную строку вывода; после ручной приёмки решить следующий графический договор поверх `IMAGE` / `SPRITE`.

## Session 920 - 2026-06-02 06:57 - SPRITE проверки доведены до зелёного состояния

Что сделано:
- по команде пользователя продолжена проверка уже реализованного среза `SPRITE` / `DRAWSPRITE`;
- исправлен пропуск в bytecode-lowering: объявление переменной `SPRITE` без инициализатора теперь использует неявное пустое значение слота так же, как `IMAGE`;
- повторены адресные тесты парсера, семантики, runtime, execution и IDE;
- полный последовательный `dotnet build Axiom.sln --no-restore /m:1` проходит;
- проверены `manual.js`, `search-index.js` и `git diff --check`.

Изменённые файлы:
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `docs/WORKLOG4.md`

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSprite" --logger "trx;LogFileName=sprite-parsing.trx"` проходит: 2 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite" --logger "trx;LogFileName=sprite-semantics.trx"` проходит: 3 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite" --logger "trx;LogFileName=sprite-runtime.trx"` сначала выявил проблему bytecode-lowering для пустого `SPRITE`, после исправления проходит: 6 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSprite|FullyQualifiedName~GraphicsNegativeAcceptanceSamples" --logger "trx;LogFileName=sprite-execution.trx"` проходит: 11 тестов;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Sprite|FullyQualifiedName~Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes|FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions" --logger "trx;LogFileName=sprite-ide.trx"` проходит: 6 тестов;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `dotnet build Axiom.sln --no-restore /m:1` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Решение:
- считать автоматическую проверку первого среза `SPRITE` / `DRAWSPRITE` закрытой; ручная приёмка в живой IDE остаётся отдельным следующим шагом.

Следующий точный шаг:
- вручную открыть `samples/graphics/sprites/walk_cycle.axm` в `Axiom.Ide`, выбрать режим `Bytecode`, запустить пример и подтвердить два кадра спрайта и финальную строку `DRAWSPRITE sample finished`; после подтверждения зафиксировать ручную приёмку и выбирать следующий графический договор.

## Session 921 - 2026-06-02 09:18 - закрыта IDE-приёмка SPRITE

Что сделано:
- продолжена работа от последнего стопа: автоматические проверки `SPRITE` уже были зелёными, оставалась живая приёмка `walk_cycle.axm` в IDE;
- в `Axiom.Ide.SmokeHarness` добавлен узкий маршрут `--sprite-only`;
- маршрут копирует `samples/graphics/sprites/walk_cycle.axm` и `samples/graphics/sprites/assets/walk_sheet.png` во временную рабочую область `.artifacts/ui-smoke/`, открывает пример в реальном `Axiom.Ide.exe`, запускает его в режиме `Bytecode`, отправляет клавишу продолжения для `SLEEP` и `ESC` для выхода;
- проверочный маршрут подтверждает `screen.output | sprite frames=4`, `screen.output | DRAWSPRITE sample finished`, закрытие `SCREEN` и трассу `run.pipeline` с `requestedMode=Bytecode` и `actualEngine=BytecodeVm`;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/SPECIFICATION3.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore /m:1` проходит;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --sprite-only` проходит;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `powershell -ExecutionPolicy Bypass -File scripts/repo-sanity.ps1` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Ограничения проверки:
- первый повтор `--sprite-only` после добавления проверки движка не прошёл из-за неверного ожидания формата трассы `run.completed`; фактическая строка статуса пишется как `run.pipeline`, после синхронизации проверки повторный прогон прошёл.

Решение:
- считать первый срез `SPRITE` / `DRAWSPRITE` закрытым также на уровне живой IDE-приёмки;
- отдельный производительный сценарий не добавлялся, поэтому `docs/BYTECODE_PERF_MATRIX.md` не менялся.

Следующий точный шаг:
- выбрать следующий графический договор поверх `IMAGE` / `SPRITE`: либо удобство игрового кадра на базе существующих страниц `SCREEN` и `PCOPY`, либо следующий минимальный спрайтовый срез вроде именованных анимаций; сначала зафиксировать договор в `docs/SPECIFICATION3.md`, только затем переходить к коду.

## Session 922 - 2026-06-02 13:35 - выбран договор игрового кадра на страницах SCREEN

Что сделано:
- продолжена работа от последнего зафиксированного стопа: первый срез `SPRITE` / `DRAWSPRITE` уже закрыт по коду, автоматическим проверкам, запускаемым примерам, справочнику, реестру покрытия и живой IDE-приёмке;
- следующим графическим шагом выбран не новый оператор и не именованные анимации, а минимальный договор игрового кадра на уже существующих страницах `SCREEN` и `PCOPY`;
- в `docs/SPECIFICATION3.md` зафиксирован пользовательский маршрут: готовить кадр на скрытой странице, рисовать `DRAWIMAGE` / `DRAWSPRITE`, затем публиковать страницу через `PCOPY`;
- в договоре явно отложены `BEGINFRAME`, `ENDFRAME`, `FLIP`, `BUFFER`, `CANVAS`, именованные анимации, объектная модель спрайтов, столкновения, тайловые карты и сценовая модель;
- `docs/PLANS3.md` обновлён так, чтобы следующий кодовый шаг был проверочным примером и приёмкой этого кадрового маршрута, а не преждевременным расширением языка.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- сначала проверить существующую BASIC-подобную модель `SCREEN`-страниц и `PCOPY` вместе с современными `IMAGE` / `SPRITE`;
- новые пользовательские имена для кадрового цикла не вводить до подтверждённого недостатка текущей модели.

Проверки:
- код, поведение языка и запускаемые примеры в этом шаге не менялись;
- после записи документов нужно выполнить `git diff --check` по затронутым файлам.

Следующий точный шаг:
- добавить короткий запускаемый пример игрового кадра на скрытой странице с `DRAWSPRITE` и `PCOPY`, подключить его к сквозной проверке `BytecodeVm`, затем обновить справочник и `docs/BYTECODE_COVERAGE.md` по факту добавленной приёмки.

## Session 923 - 2026-06-02 13:49 - проверочный кадр SPRITE через PCOPY

Что сделано:
- продолжена работа от зафиксированного стопа по игровому кадру на существующих страницах `SCREEN`;
- добавлен запускаемый пример `samples/graphics/sprites/page_flip_walk.axm`, который готовит кадры спрайта на скрытой странице `1`, переносит их на видимую страницу `0` через `PCOPY` и ждёт `ESC`;
- добавлен сквозной acceptance-тест `DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm` для запуска нового примера через `ExecutionMode.Bytecode` без отката на интерпретатор;
- `Axiom.Ide.SmokeHarness --sprite-only` расширен копированием `page_flip_walk.axm` и проверкой второго спрайтового сценария;
- чтение журнала в `Axiom.Ide.SmokeHarness` синхронизировано с текущим JSONL-каталогом `%LOCALAPPDATA%\Axiom\Logs\Events`, потому что старый поиск рядом с `Axiom.Ide.exe` больше не видел события IDE;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/manual/pcopy.html`, `docs/manual/sprites.html`, `docs/manual/graphics.html`, `docs/manual/scripts/search-index.js`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`.

Изменённые файлы:
- `samples/graphics/sprites/page_flip_walk.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/pcopy.html`
- `docs/manual/sprites.html`
- `docs/manual/graphics.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- новый пользовательский сценарий не требует новых операторов: достаточно существующих `SCREEN`-страниц, `DRAWSPRITE` и `PCOPY`;
- сквозная байткодная приёмка считается закрытой на уровне `Axiom.Execution`, но живая IDE-приёмка нового сценария отложена до стабилизации первого сценария `walk_cycle.axm` в `--sprite-only`.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm" --logger "trx;LogFileName=sprite-page-flip-execution.trx"` проходит: 1 тест;
- `dotnet build src/Axiom.Ide/Axiom.Ide.csproj --no-restore /m:1` проходит;
- `dotnet build src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore /m:1` проходит;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `git diff --check` не нашёл ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Ограничения проверки:
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --sprite-only` пока не проходит: маршрут блокируется на первом, уже существующем примере `walk_cycle.axm` до перехода к новому `page_flip_walk.axm`;
- свежие журналы `%LOCALAPPDATA%\Axiom\Logs\Events\ide-events-20260602-142213.jsonl` и `ide-events-20260602-142414.jsonl` показывают `command.executed` и `screen.activated` для `walk_cycle.axm`, но не содержат ожидаемый `screen.output` за время ожидания.

Следующий точный шаг:
- стабилизировать живой `Axiom.Ide.SmokeHarness --sprite-only`: выяснить, почему `walk_cycle.axm` в реальном IDE-хосте иногда останавливается после `screen.activated`, затем повторить маршрут и подтвердить новый `page_flip_walk.axm` через строки `page flip frames=4` и `SPRITE PCOPY sample finished`.

## Session 924 - 2026-06-02 17:54 - правило русскоязычных пошаговых примеров

Что сделано:
- пользователь вручную проверил `samples/graphics/sprites/page_flip_walk.axm` в `Axiom.Ide` и подтвердил, что пример работает нормально;
- по замечанию пользователя в `AGENTS.md` добавлено постоянное правило: новые и обновляемые `.axm`-примеры должны использовать русские пользовательские строки и пояснения, локальные комментарии рядом с ключевыми операторами и паузы для визуальных или пошаговых сценариев;
- `samples/graphics/sprites/page_flip_walk.axm` переписан под это правило: английские строки `page flip frames=` и `SPRITE PCOPY sample finished` заменены русскими сообщениями, добавлены две паузы `SLEEP`, а ключевые операторы снабжены русскими пояснениями;
- `DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm` обновлён под две паузы и русские итоговые строки;
- ожидания `Axiom.Ide.SmokeHarness --sprite-only` для второго сценария обновлены под русские строки и пошаговое продвижение после `SLEEP`.

Изменённые файлы:
- `AGENTS.md`
- `samples/graphics/sprites/page_flip_walk.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- правило русскоязычных пошаговых `.axm`-примеров считается незыблемым рабочим правилом проекта;
- старые примеры приводятся к нему при ближайшей содержательной правке соответствующего файла, без отдельного массового переписывания прямо сейчас.

Проверки:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm"` проходит: 1 тест;
- `dotnet build src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore /m:1` проходит;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит.

Следующий точный шаг:
- продолжить со стабилизации живого `Axiom.Ide.SmokeHarness --sprite-only`: выяснить, почему первый сценарий `walk_cycle.axm` иногда не доходит от `screen.activated` до `screen.output`, затем повторить полный маршрут с новым `page_flip_walk.axm`.

## Session 925 - 2026-06-02 18:49 - живой smoke SPRITE стабилизирован

Что сделано:
- продолжена работа с зафиксированного блокера `Axiom.Ide.SmokeHarness --sprite-only`;
- `samples/graphics/sprites/walk_cycle.axm` приведён к новому правилу примеров: русские сообщения, локальные комментарии рядом с ключевыми операторами, `SCREEN 1` вместо полноразмерного `SCREEN 8`, две пошаговые паузы для просмотра кадров;
- acceptance-тест `DrawSpriteAcceptanceSample_RunsOnBytecodeVm` обновлён под русские строки и две паузы;
- `Axiom.Ide.SmokeHarness` научен читать текущий JSONL-журнал IDE из `%LOCALAPPDATA%\Axiom\Logs\Events`, не ломаясь на старом текстовом `.log` с Windows-путями;
- для живого smoke harness постоянные ручные sample копируются во временную область `.artifacts/ui-smoke/` с отключёнными ручными паузами, чтобы автоматизация не зависела от ручного фокуса native-окна `SCREEN`;
- `--sprite-only` теперь проходит оба сценария: `walk_cycle.axm` и `page_flip_walk.axm`, включая проверку `DRAWSPRITE` + `PCOPY` через реальный IDE-хост.

Изменённые файлы:
- `samples/graphics/sprites/walk_cycle.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- постоянные `.axm`-примеры остаются удобными для ручного просмотра с `SLEEP`;
- smoke harness может использовать временную копию без ручного ожидания клавиши, если это нужно для автоматизации живого IDE-хоста.

Проверки:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~DrawSpriteAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm"` проходит: 2 теста;
- `dotnet build src/Axiom.Ide/Axiom.Ide.csproj --no-restore /m:1` проходит;
- `dotnet build src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore /m:1` проходит;
- `dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj --no-restore -- --sprite-only` проходит;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит.

Следующий точный шаг:
- не вводить новые графические имена без отдельного договора; следующим рабочим действием может быть либо новый спроектированный графический срез поверх `IMAGE` / `SPRITE`, либо постепенное приведение старых визуальных `.axm`-примеров к правилу русскоязычных пошаговых примеров при ближайших содержательных правках.

## Session 926 - 2026-06-02 19:33 - большой план современной графики

Что сделано:
- по решению пользователя создан отдельный документ `docs/MODERN_GRAPHICS_PLAN.md`;
- в документе зафиксирована концепция двух графических слоёв: совместимый BASIC-слой и современный графический слой;
- совместимый слой оставляет `SCREEN`, `PSET`, `LINE`, `CIRCLE`, `PAINT`, `DRAW`, `GET`, `PUT`, `PALETTE`, `PCOPY`, `POINT` и связанный текстовый вывод как поддерживаемую историческую линию;
- современный слой выбран главным направлением для будущих 2D-игр и визуальных программ на Axiom;
- в план внесены будущие срезы: современный кадр, `RGB`, очистка цветом, прямоугольники, текст, камера, анимации, столкновения и тайловые карты;
- `IMAGE` / `DRAWIMAGE` и `SPRITE` / `DRAWSPRITE` отмечены как уже реализованные первые части современного направления;
- `README.md`, `docs/ARCHITECTURE.md`, `docs/SPECIFICATION3.md` и `docs/PLANS3.md` подключены к новому документу, чтобы следующий графический код начинался с выбора пункта этого плана.

Изменённые файлы:
- `docs/MODERN_GRAPHICS_PLAN.md`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- исторические BASIC-операторы пока не удаляются и не объявляются устаревшими как поведение;
- их роль в новых игровых материалах понижается: новый основной путь должен идти через современный графический слой;
- новые пользовательские графические имена вроде `CANVAS`, `BEGINFRAME`, `PRESENT`, `RGB`, `DRAWRECT` и `FILLRECT` пока считаются кандидатами плана, а не утверждённым синтаксисом;
- перед кодом нужно отдельное уточнение договора в `docs/SPECIFICATION3.md`.

Проверки:
- код не менялся;
- после записи документов нужно выполнить проверку пробелов через `git diff --check`.

Следующий точный шаг:
- выбрать первый современный графический срез из `docs/MODERN_GRAPHICS_PLAN.md` и зафиксировать в `docs/SPECIFICATION3.md` точный договор имён, синтаксиса, диагностики, tests, manual и samples; основной кандидат: современное окно или режим рисования, начало кадра, показ кадра, `RGB`, очистка цветом, `DRAWRECT` и `FILLRECT` с интеграцией `DRAWIMAGE` и `DRAWSPRITE`.

## Session 927 - 2026-06-02 21:13 - договор первого современного графического среза

Что сделано:
- продолжена работа от последнего стопа: большой план современной графики уже был создан, но первый конкретный кодовый договор ещё не был выбран;
- в `docs/SPECIFICATION3.md` зафиксирован первый современный графический срез: `CANVAS`, `BEGINFRAME`, `PRESENT`, `RGB`, `CLS color`, `DRAWRECT`, `FILLRECT`, а также работа `DRAWIMAGE` и `DRAWSPRITE` внутри современного кадра;
- закреплено, что `CANVAS` и исторический `SCREEN` в первом срезе являются взаимоисключающими режимами, а `PCOPY`, `PALETTE`, `VIEW`, `WINDOW`, `GET`, `PUT`, `POINT` и старые примитивы остаются частью `SCREEN`;
- в `docs/PLANS3.md` добавлен следующий точный шаг реализации по слоям языка, runtime, `BytecodeVm`, IDE, справочника и запускаемых примеров.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- `RGB` выбран как BASIC-узнаваемое имя для полноцветного значения `0xRRGGBB`;
- `CANVAS`, `BEGINFRAME` и `PRESENT` записаны как проектное решение Axiom из `docs/MODERN_GRAPHICS_PLAN.md`, а не как исторические QuickBASIC-имена;
- `CLS color` разрешается только в современном `CANVAS`, чтобы не менять палитровый договор исторического `SCREEN`.

Проверки:
- код и наблюдаемое поведение языка в этом шаге не менялись;
- `git diff --check -- docs/SPECIFICATION3.md docs/PLANS3.md docs/WORKLOG4.md` проходит; Git выводит только предупреждения о будущей нормализации LF/CRLF.

Следующий точный шаг:
- начать реализацию первого современного графического среза с лексера, синтаксиса, парсера, IDE-словарей и семантических контрактов для `CANVAS`, `BEGINFRAME`, `PRESENT`, `RGB`, `CLS color`, `DRAWRECT` и `FILLRECT`.

## Session 928 - 2026-06-04 21:11 - локальный AI-контекст и пилот Headroom

Что сделано:
- после обязательного перечитывания активных документов проекта отдельно разобрана идея внешнего сжатия контекста для агентных сессий над `Axiom`;
- напрямую проверены актуальные источники `Headroom`: README проекта, документация по proxy, MCP, ограничениям и оптимизации кэша;
- для репозитория добавлен локальный скрипт `scripts/ai-context.ps1`, который собирает компактный бриф из активных документов и умеет добавлять тематические фрагменты по ключевым словам;
- добавлен документ `docs/agent/HEADROOM_PILOT.md` с конкретным планом пилота `Headroom` в двух режимах: `headroom wrap codex` и MCP-инструменты;
- обновлён `docs/agent/CURRENT_STATE.md`, чтобы короткий ручной снимок состояния больше не вёл в старую ветку date/time и отражал реальный современный графический шаг;
- `docs/agent/README.md` и `README.md` дополнены инструкциями по локальному AI-контексту и связанным агентным документам.

Изменённые файлы:
- `scripts/ai-context.ps1`
- `docs/agent/HEADROOM_PILOT.md`
- `docs/agent/CURRENT_STATE.md`
- `docs/agent/README.md`
- `README.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- `Headroom` рассматривается для `Axiom` как внешний слой агентной разработки, а не как часть продуктового roadmap языка и IDE;
- даже без внешней зависимости в репозитории должен быть локальный минимальный аналог: короткий бриф по активным документам и быстрый тематический срез по ключевым словам;
- временный вывод скрипта должен жить только в `.artifacts/`, поэтому путь по умолчанию выбран как `.artifacts/ai-context/session-brief.md`.

Проверки:
- целевой запуск `powershell -ExecutionPolicy Bypass -File scripts/ai-context.ps1` ещё предстоит выполнить после добавления файлов;
- целевой запуск `powershell -ExecutionPolicy Bypass -File scripts/ai-context.ps1 -Topic CANVAS,RGB,DRAWRECT` тоже запланирован как ближайшая проверка;
- внешние продуктовые тесты не запускались, потому что этот шаг не меняет язык, runtime или IDE.

Следующий точный шаг:
- прогнать оба варианта `scripts/ai-context.ps1`, проверить формат и полезность брифа, затем зафиксировать результат;
- после этого вернуться к активной продуктовой задаче по реализации `CANVAS`, `BEGINFRAME`, `PRESENT`, `RGB`, `CLS color`, `DRAWRECT` и `FILLRECT`.

## Session 929 - 2026-06-04 21:23 - проверен запуск AI-контекста

Что сделано:
- `scripts/ai-context.ps1` прогнан в основном режиме и подтвердил запись файла `.artifacts/ai-context/session-brief.md`;
- отдельно прогнан тематический режим `-Topic CANVAS,RGB,DRAWRECT`;
- по результатам первых прогонов исправлены практические проблемы совместимости с Windows PowerShell: скрипт пересохранён в UTF-8 с BOM, убран вызов `Path.GetRelativePath`, недопустимый для текущего окружения, и переписаны строки markdown с обратными кавычками, которые ломали парсер;
- итоговый бриф теперь остаётся компактным, а тематический режим действительно добавляет выборочные фрагменты вместо полного перечитывания документов.

Изменённые файлы:
- `scripts/ai-context.ps1`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- локальный AI-контекст считается рабочим вспомогательным слоем и может использоваться отдельно от внешнего `Headroom`;
- обязательный путь вывода через `.artifacts/` подтверждён реальным запуском и остаётся договором по умолчанию.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/ai-context.ps1` проходит и пишет `.artifacts/ai-context/session-brief.md`;
- `powershell -ExecutionPolicy Bypass -File scripts/ai-context.ps1 -StdoutOnly -Topic CANVAS,RGB,DRAWRECT` проходит;
- `git diff --check` не показал ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF и о посторонних изменениях рабочего дерева вне этой задачи.

Следующий точный шаг:
- если продолжаем линию внешнего сжатия контекста, запускать пилот `Headroom` по сценарию из `docs/agent/HEADROOM_PILOT.md`;
- по основной продуктовой линии вернуться к реализации `CANVAS`, `BEGINFRAME`, `PRESENT`, `RGB`, `CLS color`, `DRAWRECT` и `FILLRECT`.

## Session 930 - 2026-06-05 02:52 - синхронизация второго CANVAS-среза и maze_chase

Что сделано:
- после обязательного перечитывания активных документов и сверки кода подтверждено, что второй современный графический срез уже реализован в языке, runtime, `BytecodeVm` и IDE-хосте;
- исправлен главный игровой sample `samples/graphics/game/maze_chase.axm`: локальная `Clamp(...)` была убрана, а вызовы переведены на встроенную `CLAMP(...)`, чтобы не конфликтовать с регистронезависимым именованием языка;
- синхронизированы manual и проектные документы под фактический договор второго среза: линии, круги, HUD-текст, камера и роль `maze_chase` как флагманского современного примера;
- подготовлен повторный адресный acceptance-прогон для `hud_text`, `camera_world`, `lines_and_circles` и `maze_chase`.

Изменённые файлы:
- `docs/manual/modern-graphics.html`
- `docs/manual/graphics.html`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/MODERN_GRAPHICS_PLAN.md`
- `docs/PLANS3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/WORKLOG4.md`
- `samples/graphics/game/maze_chase.axm`

Решения:
- исторические графические операторы не удаляются и не «осовремениваются» по месту; они остаются отдельным совместимым слоем;
- современный путь развивается отдельно вокруг `CANVAS`, кадров, примитивов, текста и камеры;
- `maze_chase` становится главным живым примером современного графического слоя и практической опорой для дальнейших игровых шагов.

Проверки:
- после документной синхронизации нужно повторить адресный `dotnet test` для четырёх acceptance-примеров второго среза;
- `node --check docs/manual/scripts/manual.js` и `node --check docs/manual/scripts/search-index.js` должны подтвердить корректность manual-скриптов;
- затем стоит отдельно прогнать `git diff --check` по затронутым файлам.

Следующий точный шаг:
- завершить адресный acceptance-прогон `ModernHudTextAcceptanceSample_RunsOnBytecodeVm`, `ModernCameraAcceptanceSample_RunsOnBytecodeVm`, `ModernLinesAndCirclesAcceptanceSample_RunsOnBytecodeVm` и `MazeChaseAcceptanceSample_RunsOnBytecodeVm`;
- если всё проходит, отдельным следующим решением решить, нужен ли короткий `samples/smoke/engine-compare/` сценарий на основе того же современного графического цикла.

## Session 931 - 2026-06-05 03:13 - приёмка второго CANVAS-среза завершена

Что сделано:
- повторён адресный `dotnet test` для `ModernHudTextAcceptanceSample_RunsOnBytecodeVm`, `ModernCameraAcceptanceSample_RunsOnBytecodeVm`, `ModernLinesAndCirclesAcceptanceSample_RunsOnBytecodeVm` и `MazeChaseAcceptanceSample_RunsOnBytecodeVm`;
- по результатам первого прогона найден и исправлен типовой конфликт в `samples/graphics/game/maze_chase.axm`: локальные значения камеры и ghost-distance больше не зависят от built-in `CLAMP(...)` и `ABS(...)`, а вычисляются через явные целочисленные helper-функции `ClampInt(...)` и `AbsInt(...)`;
- после исправления sample все четыре acceptance-сценария прошли через `BytecodeVm`;
- отдельно подтверждено, что `node --check docs/manual/scripts/manual.js` и `node --check docs/manual/scripts/search-index.js` проходят;
- `git diff --check` по затронутым файлам не показал ошибок пробелов, остались только предупреждения Git о будущей нормализации LF/CRLF.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- считать второй современный графический срез принятым не только по документам, но и по адресной байткодной приёмке;
- `maze_chase` остаётся главным живым примером современного графического слоя и теперь проходит адресную acceptance-проверку без типовых обходов и без отката на интерпретатор.

Проверки:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -v minimal --filter "FullyQualifiedName~ModernHudTextAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ModernCameraAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~ModernLinesAndCirclesAcceptanceSample_RunsOnBytecodeVm|FullyQualifiedName~MazeChaseAcceptanceSample_RunsOnBytecodeVm"` проходит: 4 из 4;
- `node --check docs/manual/scripts/manual.js` проходит;
- `node --check docs/manual/scripts/search-index.js` проходит;
- `git diff --check -- docs/manual/modern-graphics.html docs/manual/graphics.html docs/manual/scripts/search-index.js docs/SPECIFICATION3.md docs/MODERN_GRAPHICS_PLAN.md docs/PLANS3.md docs/BYTECODE_COVERAGE.md docs/WORKLOG4.md samples/graphics/game/maze_chase.axm` не находит ошибок пробелов, выводит только предупреждения Git о будущей нормализации LF/CRLF.

Следующий точный шаг:
- отдельным решением выбрать, нужен ли короткий детерминированный `samples/smoke/engine-compare/` сценарий на основе `maze_chase`;
- если такой сценарий пока не нужен, следующую продуктовую задачу брать уже поверх готового современного слоя `CANVAS`, а не возвращаться к базовому кадровому договору.

## Session 932 - 2026-06-05 07:16 - исправлен BYREF скаляра и подтверждён паритет maze_chase

Что сделано:
- найден и исправлен дефект интерпретатора в `BYREF` для простой переменной: путь разрешения аргумента теперь поднимает owning-scope исходной переменной и создаёт настоящую ссылку на её хранилище, а не локальную копию;
- в `Interpreter.Helpers` добавлены перегрузки чтения и присваивания переменной с явным `RuntimeScope`, чтобы `BYREF` мог менять исходный scope вызывающей стороны;
- добавлен runnable-пример `samples/operators/sub/byref_scalar_argument.axm` и ссылка на него в `docs/manual/sub.html`;
- добавлены адресные runtime-тесты `Execute_ByRefScalarParameterMutatesCallerVariable()` и `Execute_CompiledByRefScalarParameterMutatesCallerVariable()`;
- acceptance-цепочка пополнена примером `samples/operators/sub/byref_scalar_argument.axm`;
- адресный прогон `MazeChaseAcceptanceSample_MovesPlayerInBothEngines` подтвердил, что после исправления герой двигается и в `Interpreter`, и в `BytecodeVm`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/sub/byref_scalar_argument.axm`
- `docs/manual/sub.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- считать корневой дефект интерпретаторного `BYREF` для простых скалярных переменных закрытым;
- считать подтверждённым практический эффект исправления на живом игровом сценарии `samples/graphics/game/maze_chase.axm`;
- не расширять этот шаг на поля, элементы массива и вызовы через function-value по `BYREF`: они остаются отдельным следующим договором.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~MazeChaseAcceptanceSample_MovesPlayerInBothEngines" --logger "trx;LogFileName=maze-chase-both-engines.trx"` проходит: 2 теста;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~TypeDefaultAndEqualityAcceptanceSamples_RunOnBytecodeVm&DisplayName~byref_scalar_argument" --logger "trx;LogFileName=byref-scalar-acceptance.trx"` проходит: 1 тест;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~ByRefScalar|FullyQualifiedName~ByRefRecord" --logger "trx;LogFileName=byref-runtime-parity.trx"` проходит: 4 теста.

Следующий точный шаг:
- решить, выделять ли из `samples/graphics/game/maze_chase.axm` отдельный детерминированный `samples/smoke/engine-compare/` сценарий современного графического цикла;
- если производительный сценарий пока не нужен, следующим отдельным продуктовым решением выбирать уже не базовую графику, а следующий общий игровой helper или следующий живой игровой пример поверх `CANVAS`.

## Session 933 - 2026-06-05 10:49 - точки maze_chase привязаны к реальным тоннелям

Что сделано:
- по пользовательскому замечанию разобран живой пример `samples/graphics/game/maze_chase.axm`, где съедаемые точки визуально уходили вверх и местами залезали в стены лабиринта;
- для фона `art/Background.gif` подтверждено, что реальные оси тоннелей не совпадают с простым центром логической клетки `16 x 16`;
- в sample добавлены таблицы `pelletCenterRows(1 TO 36)` и `pelletCenterCols(1 TO 28)`, которые задают измеренные центры горизонтальных и вертикальных проходов;
- отрисовка точек переведена на `PelletPixelX(...)` и `PelletPixelY(...)`, поэтому горизонтальные точки теперь сидят по центру тоннелей, а проблемные вертикальные точки больше не попадают внутрь стен;
- сам пример подробно прокомментирован по-русски почти построчно, чтобы код можно было читать как пошаговое объяснение всей игровой сцены;
- для локальной проверки геометрии сохранены диагностические оверлеи в `.artifacts/maze_chase_overlay_current.png` и `.artifacts/maze_chase_overlay_adjusted.png`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- не менять игровую механику и не перестраивать координатную сетку лабиринта;
- считать картинку `Background.gif` визуальным источником истины для положения тоннелей и отдельно подгонять центры pellet-точек под реальные проходы;
- комментарии в runnable sample держать русскими и локальными, рядом с действиями, чтобы пример был понятен без прыжков по файлу.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~MazeChaseAcceptanceSample"` проходит: 3 теста;
- `git diff --check -- samples/graphics/game/maze_chase.axm docs/PLANS3.md docs/WORKLOG4.md` не должен показывать ошибок пробелов; остаются только предупреждения Git о будущей нормализации LF/CRLF.

Ограничения проверки:
- в этой сессии не выполнялась ручная визуальная приёмка через `Axiom.Ide`, поэтому финальное подтверждение положения всех точек на фоне ещё лучше сделать глазами в IDE.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и вручную проверить выравнивание точек на фоне `Background.gif`;
- после визуальной приёмки вернуться к решению, нужен ли отдельный `samples/smoke/engine-compare/` сценарий современного графического цикла.

## Session 934 - 2026-06-05 11:23 - комментарии InitPelletAlignment упрощены для ручной подгонки

Что сделано:
- по просьбе пользователя переписаны комментарии внутри `SUB InitPelletAlignment()` в `samples/graphics/game/maze_chase.axm`;
- вместо технических формулировок комментарии теперь коротко объясняют, какой именно ряд или столбец тоннелей вы сейчас двигаете руками;
- рядом с таблицами добавлены прямые подсказки по направлению подбора: для `pelletCenterRows(...)` большее число опускает точку ниже, а для `pelletCenterCols(...)` большее число сдвигает точку правее;
- сами числовые значения выравнивания не менялись, чтобы не сбить текущую ручную настройку пользователя.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/WORKLOG4.md`

Решения:
- в этой зоне примера полезнее комментарии-навигация по лабиринту, чем комментарии про абстрактную геометрию клетки;
- ручная подгонка pellet-точек должна опираться на быстрые и короткие подписи в стиле строк `maze(...)`.

Проверки:
- кодовая логика не менялась, поэтому тесты не запускались;
- рекомендуется открыть `samples/graphics/game/maze_chase.axm` в IDE и продолжить ручную настройку уже по новым комментариям.

Следующий точный шаг:
- вручную подогнать нужные `pelletCenterRows(...)` и `pelletCenterCols(...)` по визуальному результату в `Axiom.Ide`;
- после визуальной приёмки решить, нужен ли отдельный `samples/smoke/engine-compare/` сценарий современного графического цикла.

## Session 935 - 2026-06-05 11:36 - maze_chase переведён на полный обзор лабиринта

Что сделано:
- по новому замечанию пользователя выяснено, что ручная настройка точек неудобна из-за прежнего окна `CANVAS 480 x 270`: лабиринт не помещался целиком, а часть сдвигов уходила за пределы видимой области;
- в `samples/graphics/game/maze_chase.axm` размеры холста изменены на полный обзор карты: ширина теперь равна полной ширине лабиринта, а высота вмещает весь лабиринт и HUD без обрезания;
- процедура `DrawMazeWorld()` переведена в статичный режим без прокрутки по камере, чтобы все строки и все столбцы карты были видны одновременно;
- весь игровой мир сдвинут ниже HUD через новый `WorldOffsetY()`, поэтому верхние ряды лабиринта больше не перекрываются текстовой панелью;
- расчёт Y-координат для персонажей, точек и фоновой картинки приведён к тому же вертикальному смещению.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- в текущем этапе важнее удобная визуальная подгонка pellet-точек, чем сохранение прокрутки камеры в этом конкретном sample;
- полный статичный обзор лучше подходит для ручной правки `pelletCenterRows(...)` и `pelletCenterCols(...)`, потому что результат любой цифры виден сразу на всём поле.

Проверки:
- нужно повторить адресный acceptance-прогон `MazeChaseAcceptanceSample`, чтобы подтвердить, что sample по-прежнему исполняется штатно;
- после этого sample стоит снова открыть в `Axiom.Ide` и уже по новому полному обзору вручную довести точки.

Следующий точный шаг:
- вручную подогнать `pelletCenterRows(...)` и `pelletCenterCols(...)` в `Axiom.Ide`, пока точки не лягут в нужные тоннели;
- после завершения подгонки решить, нужно ли возвращать камеру в `maze_chase` или вынести прокрутку в отдельный демонстрационный пример.

## Session 936 - 2026-06-05 11:40 - acceptance maze_chase синхронизирован с полным обзором

Что сделано:
- после перевода `samples/graphics/game/maze_chase.axm` на полный обзор лабиринта адресный тест `MazeChaseAcceptanceSample_RunsOnBytecodeVm` упал не по логике игры, а по жёстко зашитому старому размеру окна `480 x 270`;
- в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` ожидание `CanvasActivations` обновлено на новый фактический размер `448 x 606`;
- пользовательский договор примера при этом не менялся: тест просто приведён в соответствие с новым режимом показа без прокрутки.

Изменённые файлы:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`

Решения:
- считать новую геометрию окна частью текущего sample-договора для ручной подгонки pellet-точек;
- не возвращать старое ожидание теста, потому что именно большой полный обзор сейчас нужен пользователю для визуальной настройки.

Проверки:
- после этой синхронизации нужно повторить `MazeChaseAcceptanceSample`, чтобы подтвердить, что acceptance снова зелёный.

Следующий точный шаг:
- повторить адресный `MazeChaseAcceptanceSample`;
- затем открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и продолжить ручную подгонку точек по полному обзору поля.
## Session 937 - 2026-06-05 12:33 - maze_chase актёры привязаны к центрам точек

Что сделано:
- в `samples/graphics/game/maze_chase.axm` добавлена пауза по `SPACE`: повторное нажатие снимает паузу, а HUD показывает строку `Пауза. Нажмите SPACE, чтобы продолжить.`;
- размер холста примера изменён на `1024 x 660`, чтобы лабиринт оставался целиком видимым в более нормальном широком окне;
- функции `PlayerPixelX()`, `PlayerPixelY()`, `GhostPixelX()` и `GhostPixelY()` теперь считают координаты через общие функции `EntityPixelX(...)` и `EntityPixelY(...)` с передачей строки и столбца актёра;
- `EntityPixelX(...)` и `EntityPixelY(...)` больше не используют простой центр клетки `TILE \ 2`, а берут старт и цель из `PelletPixelX(...)` и `PelletPixelY(...)`, то есть герой и призрак двигаются по тем же ручным центрам тоннелей, что и точки;
- для бокового туннеля сохранён отдельный плавный выход за край, чтобы актёр не пролетал визуально через всю карту при переносе;
- в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` обновлён ожидаемый размер `CanvasActivations` и добавлен acceptance-тест `MazeChaseAcceptanceSample_SpaceShowsPauseHud()`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- не переносить автоматически пиксельную эвристику в `pelletCenterRows(...)` и `pelletCenterCols(...)`: для текущего `Background.gif` автоматический анализ тонких голубых стен даёт нестабильные крайние значения, поэтому окончательная настройка точек остаётся ручной визуальной операцией;
- оставить `BACKGROUND_OFFSET_Y = 45` как ручной сдвиг фоновой подложки, найденный пользователем, но синхронизировать персонажей именно с точками, чтобы герой не ходил ниже визуального ряда, который он съедает.

Проверки:
- первая попытка `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~MazeChaseAcceptanceSample"` упёрлась в локальную MSBuild/NuGet-проблему отсутствующего fallback-каталога `C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`;
- повторный адресный прогон с обходом `/p:DisableImplicitNuGetFallbackFolder=true` прошёл: `MazeChaseAcceptanceSample` — 4 теста, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и глазами проверить верхний ряд, второй горизонтальный ряд, центральный тоннель и строку старта героя уже с новой логикой движения по центрам точек;
- если отдельные ряды или столбцы всё ещё попадают в стенки, дальше менять только соответствующие `pelletCenterRows(...)` и `pelletCenterCols(...)`, потому что актёры теперь автоматически используют те же координаты.

## Session 938 - 2026-06-05 13:10 - maze_chase получил режим записи ручной калибровки

Что сделано:
- в `samples/graphics/game/maze_chase.axm` добавлен режим ручной калибровки по клавише `K`;
- в калибровочном режиме игра замораживает механику, а стрелки двигают выбранную строку или столбец точек вместо героя;
- прямо поверх лабиринта выводятся подсказки: `K` выключает режим, `C` переключает строку и столбец, `W/S` выбирают номер, стрелки двигают выбранную линию, `R` записывает текущее значение;
- оверлей рисует направляющую линию по выбранной строке или столбцу и показывает точную строку присваивания, которую можно перенести в `InitPelletAlignment()`;
- по `R` текущая строка присваивания дописывается во временный файл `.artifacts/maze_chase_pellet_calibration.txt`;
- в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` добавлен тест `MazeChaseAcceptanceSample_KShowsCalibrationHints()`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- временные результаты калибровки не записывать в корень репозитория, а хранить только в `.artifacts/`;
- не менять автоматически значения `pelletCenterRows(...)` и `pelletCenterCols(...)` без визуальной приёмки пользователя;
- после ручной калибровки переносить сохранённые строки из временного файла в `SUB InitPelletAlignment()` отдельной точечной правкой.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 5 тестов, 0 ошибок.

Следующий точный шаг:
- запустить `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, включить калибровку клавишей `K`, сохранить удачные значения клавишей `R`, затем перенести содержимое `.artifacts/maze_chase_pellet_calibration.txt` в `SUB InitPelletAlignment()`.

## Session 939 - 2026-06-05 13:21 - вход в калибровку maze_chase принимает маленькую k

Что сделано:
- по пользовательскому замечанию проверен вход в режим калибровки `samples/graphics/game/maze_chase.axm`;
- найден практический источник сбоя: в живом графическом окне обычное нажатие латинской `k` приходит как маленький символ, а sample проверял только `KEYPRESS("K")`;
- добавлена локальная функция `CalibrationHotKeyPressed(...)`, которая для буквенных команд калибровки принимает и заглавную, и маленькую букву;
- вход в калибровку и команды `C`, `W`, `S`, `R` переведены на эту функцию, поэтому теперь работают также `k`, `c`, `w`, `s`, `r`;
- acceptance-тест калибровки переименован и теперь подаёт именно маленькую `k`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- не менять глобальную семантику `KEYPRESS()` для всего языка, потому что одиночные символы могут быть регистрозависимыми;
- исправить конкретный игровой sample локальным helper-слоем для горячих клавиш калибровки.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 5 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, нажать обычную латинскую `k` без `Shift` и проверить появление оверлея калибровки;
- после подтверждения продолжить ручную подгонку строк и столбцов точек, сохраняя удачные значения клавишей `r`.

## Session 940 - 2026-06-05 14:01 - maze_chase получил боковую панель, сетку и Shift+R

Что сделано:
- из `.artifacts/maze_chase_pellet_calibration.txt` прочитан первый пользовательский проход центрирования точек;
- в `SUB InitPelletAlignment()` перенесены последние сохранённые значения для строк `pelletCenterRows(4..33)` и столбцов `pelletCenterCols(1)`, `pelletCenterCols(2)`, `pelletCenterCols(3)`, `pelletCenterCols(13)`;
- калибровочная панель подсказок перенесена из верхней части экрана в боковую область слева от лабиринта, чтобы больше не закрывать верхний ряд точек;
- добавлена тонкая калибровочная сетка поверх игрового поля с постоянным шагом `8` пикселей;
- добавлен режим `Shift+R`, который дописывает во временный файл полный снимок всех текущих `pelletCenterRows(...)` и `pelletCenterCols(...)`;
- в боковую панель добавлена строка статуса, чтобы после `R` или `Shift+R` было видно, что именно записалось;
- acceptance-проверка `MazeChaseAcceptanceSample` расширена тестом полной записи по `Shift+R` и проверкой боковой панели/сетки.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- при переносе дублирующихся строк калибровки использовать последнее значение в файле как наиболее свежее;
- оставить файл `.artifacts/maze_chase_pellet_calibration.txt` временным журналом ручной работы, а постоянные значения вносить в `InitPelletAlignment()` отдельными проходами.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, включить калибровку клавишей `k`, выполнить следующий визуальный проход по точкам и после него нажать `Shift+R`;
- затем прочитать свежий полный снимок из `.artifacts/maze_chase_pellet_calibration.txt` и перенести новые значения в `SUB InitPelletAlignment()`.

## Session 941 - 2026-06-05 18:13 - сетка maze_chase доведена до реальных границ фона

Что сделано:
- по пользовательскому замечанию проверен калибровочный оверлей `samples/graphics/game/maze_chase.axm`;
- найдено, что сетка рисовалась от логического `WorldOffsetY()`, а не от фактического положения `Background.gif`, поэтому сверху и снизу не совпадала с подложкой;
- сетка и вертикальная направляющая переведены на новые helper-функции `BackgroundDrawTop()` и `BackgroundDrawBottom()`, которые используют реальное положение фоновой картинки;
- цвет сетки сделан более приглушённым;
- длинные статусные сообщения калибровки переведены на двухстрочный вывод через `SetCalibrationStatusText(...)`, чтобы не наползать на игровое поле;
- acceptance-проверка `MazeChaseAcceptanceSample` синхронизирована с новым двухстрочным статусом после `Shift+R`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- в калибровочном оверлее ориентироваться на фактические границы фоновой подложки, а не на абстрактный мир без ручного сдвига `BACKGROUND_OFFSET_Y`;
- длинные сообщения панели калибровки всегда держать в двух строках, чтобы она оставалась читаемой.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и проверить глазами, что сетка теперь покрывает поле сверху донизу;
- затем продолжить ручную подгонку точек и при необходимости снова сохранить полный снимок через `Shift+R`.

## Session 942 - 2026-06-05 18:56 - maze_chase перенёс свежую калибровку и убрал догоняющие повороты

Что сделано:
- повторно прочитан `.artifacts/maze_chase_pellet_calibration.txt` и в `samples/graphics/game/maze_chase.axm` перенесены последние сохранённые значения `pelletCenterRows(5) = 12`, `pelletCenterRows(13) = 47`, `pelletCenterCols(6) = 15`, `pelletCenterCols(21) = 0`, `pelletCenterCols(27) = 5`;
- в `UpdatePlayerInput()` оставлено только чтение стрелок через `KEYDOWN(...)`, а стрелочные `KEYPRESS(...)` убраны из обычного игрового управления;
- acceptance-проверка `MazeChaseAcceptanceSample_LowerKShowsCalibrationHints()` обновлена под новое значение строки 5;
- acceptance-проверка движения героя переведена на удержание `RIGHT`, чтобы проверять именно живое управление, а не старую очередь одиночных событий.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- корень запаздывания поворотов для `maze_chase` найден в сочетании двух механизмов: sample одновременно читал стрелки через `KEYDOWN(...)` и через `KEYPRESS(...)`, а рантайм при `KEYPRESS()` честно сохраняет несъеденные клавиши обратно в очередь;
- глобальную семантику клавиатуры для всего языка в этой задаче не менять, а исправить именно игровой sample, где для поворота в коридоре нужен приоритет актуально зажатой стрелки.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и продолжить ручную подгонку точек уже после переноса этих значений;
- после следующего прохода снова нажать `Shift+R`, чтобы сохранить полный снимок, и затем перенести новые числа в `InitPelletAlignment()`;
- если запаздывание клавиш проявится и в других интерактивных программах, вынести это уже в отдельную задачу на пересмотр общей очереди клавиатуры.

## Session 943 - 2026-06-05 19:23 - maze_chase перенёс второй пакет свежих координат

Что сделано:
- повторно прочитан `.artifacts/maze_chase_pellet_calibration.txt` и из него взяты именно последние значения по каждому индексу;
- в `samples/graphics/game/maze_chase.axm` дополнительно перенесены `pelletCenterRows(23) = 66`, `pelletCenterCols(1) = 12`, `pelletCenterCols(2) = 13`, `pelletCenterCols(18) = 3`, `pelletCenterCols(28) = 6`;
- адресный прогон `MazeChaseAcceptanceSample` выполнен после переноса координат.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- причиной того, что в прошлый раз часть чисел могла не примениться визуально, оказался не пропуск самой процедуры переноса, а то, что в файле калибровки были более новые записи для некоторых индексов, появившиеся уже после предыдущего переноса;
- для `maze_chase` дальше нужно каждый раз переносить не весь журнал подряд, а последнюю запись по каждому индексу.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и проверить глазами области, связанные с новыми значениями `pelletCenterRows(23)`, `pelletCenterCols(1)`, `pelletCenterCols(2)`, `pelletCenterCols(18)` и `pelletCenterCols(28)`;
- после следующего прохода снова сохранить полный снимок через `Shift+R` и ещё раз перенести последние значения из `.artifacts/maze_chase_pellet_calibration.txt`.

## Session 944 - 2026-06-05 20:33 - калибровка maze_chase перенесена рядом с файлом игры

Что сделано:
- найден последний полный файл калибровки `maze_chase_pellet_calibration.txt` и помещён рядом с игрой: `samples/graphics/game/maze_chase_pellet_calibration.txt`;
- проверено, что значения из этого файла совпадают с текущими присваиваниями `pelletCenterRows(...)` и `pelletCenterCols(...)` в `SUB InitPelletAlignment()`;
- `samples/graphics/game/maze_chase.axm` теперь сохраняет калибровку через `COMBINEPATH(CURRENTDIR(), "maze_chase_pellet_calibration.txt")`, без `.artifacts`;
- `ProgramRunService` на время запуска программы выставляет `Environment.CurrentDirectory` в папку открытого `.axm` и затем восстанавливает прежнюю рабочую папку;
- acceptance-проверки `MazeChaseAcceptanceSample` запускают пример из папки игры, чтобы тестовая запись `Shift+R` работала с тем же путём, что и живой запуск IDE;
- добавлен IDE-тест, который проверяет, что `CURRENTDIR()` во время запуска из IDE указывает на папку `sourceFilePath`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `samples/graphics/game/maze_chase_pellet_calibration.txt`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- рабочий файл ручной калибровки `maze_chase` теперь хранится рядом с `maze_chase.axm`, а не в `.artifacts`;
- для программ, запущенных из IDE с известным `sourceFilePath`, `CURRENTDIR()` должен быть папкой открытого `.axm`, чтобы относительные файловые операции были предсказуемыми для пользователя.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Run_SetsCurrentDirToSourceFileDirectoryDuringExecution|FullyQualifiedName~Run_SetsResourceBaseDirectoryFromSourceFilePath" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 2 теста, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, выполнить следующий проход ручной калибровки и после `Shift+R` проверять файл `samples/graphics/game/maze_chase_pellet_calibration.txt`.

## Session 945 - 2026-06-05 21:39 - цепочка калибровки maze_chase защищена от тестовой перезаписи и растяжения окна

Что сделано:
- по пользовательскому замечанию проверена вся цепочка координат `samples/graphics/game/maze_chase.axm`: фон, точки, герой, призрак, сетка и калибровочные направляющие сейчас используют общий `WorldOffsetY()`;
- подтверждено, что `samples/graphics/game/maze_chase_pellet_calibration.txt` после `Shift+R` сам по себе не читается программой при старте: значения применяются только после переноса в `SUB InitPelletAlignment()`;
- найден реальный дефект в acceptance-цепочке: `MazeChaseAcceptanceSample_ShiftRWritesAllCalibrationValues()` запускал `Shift+R` из папки игры и мог перезаписывать рабочий файл ручной калибровки рядом с `maze_chase.axm`;
- тест `Shift+R` переведён на временный рабочий каталог `.artifacts/tests/...`, поэтому автотест больше не трогает `samples/graphics/game/maze_chase_pellet_calibration.txt`;
- после запуска `MazeChaseAcceptanceSample` проверено, что живой файл калибровки остался с прежними `Length = 1768` и `LastWriteTime = 05.06.2026 21:20:43`;
- найден второй визуальный дефект: `Direct3D11SwapChainRenderer.ScaleFramePixels(...)` растягивал кадр до размера окна независимо по X и Y, из-за чего `CANVAS` мог вытягивать лабиринт, героя и призрака;
- renderer исправлен: кадр теперь масштабируется с сохранением пропорций и центрируется с чёрными полями, если окно имеет другую форму;
- добавлен тест `ScreenRenderer_PreservesFrameAspectRatioWhenBackBufferIsWider()`;
- в комментариях `maze_chase.axm` старые упоминания временного файла калибровки заменены на описание файла рядом с игрой.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `src/Axiom.Ide/Graphics/Direct3D11SwapChainRenderer.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- рабочий файл ручной калибровки `samples/graphics/game/maze_chase_pellet_calibration.txt` должен изменяться только при живом запуске пользователя, а не при acceptance-тестах;
- для ручной пиксельной калибровки современный графический хост IDE обязан сохранять пропорции кадра, иначе пользователь видит не те координаты, которые реально рисует программа;
- автоматическое чтение файла калибровки при старте пока не добавлялось, по текущему пользовательскому решению перенос чисел в `InitPelletAlignment()` остаётся ручным шагом Codex после сохранения снимка.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ScreenRenderer" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 4 теста, 0 ошибок;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок;
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Run_SetsCurrentDirToSourceFileDirectoryDuringExecution|FullyQualifiedName~Run_SetsResourceBaseDirectoryFromSourceFilePath" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 2 теста, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и проверить глазами, что поле, герой и призрак больше не вытянуты;
- выполнить новый ручной проход калибровки, нажать `Shift+R`, убедиться, что изменился `samples/graphics/game/maze_chase_pellet_calibration.txt`;
- после этого перенести полный свежий снимок из этого файла в `SUB InitPelletAlignment()`.

## Session 946 - 2026-06-05 21:54 - перенесён контрольный сдвиг столбца maze_chase

Что сделано:
- по пользовательскому сигналу после `Shift+R` прочитан свежий файл `samples/graphics/game/maze_chase_pellet_calibration.txt`;
- подтверждено, что файл изменён в `2026-06-05 21:51:15` и содержит полный снимок калибровки;
- найдено последнее значение для проверочной правки: `pelletCenterCols(26) = 2`;
- в `samples/graphics/game/maze_chase.axm` в `SUB InitPelletAlignment()` перенесено это значение вместо прежнего `pelletCenterCols(26) = 7`;
- выполнена адресная acceptance-проверка `MazeChaseAcceptanceSample`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- при переносе сохраняется правило «последняя запись для индекса побеждает», поэтому повторное `pelletCenterCols(26) = 2` в конце файла считается финальным значением;
- если визуальный сдвиг не проявится после запуска обновлённого `maze_chase.axm`, следующим шагом нужно проверять не путь сохранения файла, а фактическую цепочку от `pelletCenterCols(...)` до `PelletPixelX(...)`, отрисовки точек и отображения кадра IDE.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть обновлённый `samples/graphics/game/maze_chase.axm` в `Axiom.Ide` и визуально проверить, сдвинулся ли столбец 26;
- если он не сдвинулся, проверить, не запускает ли IDE старое содержимое открытой вкладки, а затем разобрать цепочку вычисления `PelletPixelX(...)` и отрисовки точек.

## Session 947 - 2026-06-05 22:08 - полная запись калибровки maze_chase переведена на t

Что сделано:
- по пользовательскому замечанию подтверждено, что прежний `Shift+R` мог восприниматься как обычная запись `R`, из-за чего в рабочий файл уходила только одна выбранная линия;
- в `samples/graphics/game/maze_chase.axm` полная запись таблицы калибровки перенесена на отдельную клавишу `t`;
- одиночная запись выбранной линии оставлена на отдельной клавише `r`;
- боковые подсказки калибровочного режима обновлены: `r: записать линию`, `t: записать всё`;
- значение `pelletCenterCols(26)` возвращено к `7`, потому что `pelletCenterCols(26) = 2` было одиночной допиской, а не настоящим полным снимком калибровки;
- acceptance-проверка полной записи переименована и переведена на нажатие `t` без `SHIFT`.

Изменённые файлы:
- `samples/graphics/game/maze_chase.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- `Shift+R` больше не использовать в ручной калибровке `maze_chase`;
- новая рабочая схема: `r` дописывает текущую линию, `t` перезаписывает файл полным снимком всей таблицы;
- после следующего ручного прохода переносить значения в `InitPelletAlignment()` только из файла, сохранённого клавишей `t`.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 6 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, включить калибровку клавишей `k`, сделать ручные правки и нажать `t`;
- затем проверить, что `samples/graphics/game/maze_chase_pellet_calibration.txt` перезаписан полным снимком, и перенести значения в `SUB InitPelletAlignment()`.
## Session 948 - 2026-06-05 22:52 - перенос калибровки maze_chase вынесен в отдельный Axiom-скрипт

Что сделано:
- добавлен новый файл `samples/graphics/game/maze_chase_apply_calibration.axm`, который считывает `samples/graphics/game/maze_chase_pellet_calibration.txt`, полностью пересобирает `SUB InitPelletAlignment()` и перезаписывает `samples/graphics/game/maze_chase.axm`;
- в скрипте оставлены подробные русские комментарии почти после каждой строки, чтобы его можно было читать как временный служебный инструмент;
- добавлен acceptance-тест `MazeChaseApplyCalibrationSample_RewritesInitPelletAlignmentFromCalibrationFile` в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`;
- в скрипте исправлены две реальные проблемы первого прогона: конфликт имени `StartsWith` со встроенной функцией `STARTSWITH` и некорректное приведение `FIX(VAL(...))` к `INTEGER`;
- после исправлений адресный прогон нового сценария и существующих проверок `MazeChaseAcceptanceSample` прошёл успешно.

Изменённые файлы:
- `samples/graphics/game/maze_chase_apply_calibration.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- автоматическую подгрузку `maze_chase_pellet_calibration.txt` при старте `maze_chase.axm` не добавлять;
- для дальнейшей работы принята новая цепочка: в игре нажать `t`, потом отдельно запустить `maze_chase_apply_calibration.axm`;
- новый скрипт переписывает весь блок `InitPelletAlignment()` целиком, чтобы не искать «реальные отличия» по отдельным строкам.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~MazeChaseApplyCalibrationSample_RewritesInitPelletAlignmentFromCalibrationFile|FullyQualifiedName~MazeChaseAcceptanceSample" /p:DisableImplicitNuGetFallbackFolder=true` проходит: 7 тестов, 0 ошибок.

Следующий точный шаг:
- открыть `samples/graphics/game/maze_chase.axm` в `Axiom.Ide`, включить калибровку клавишей `k`, сделать новые правки и нажать `t`;
- после этого запустить `samples/graphics/game/maze_chase_apply_calibration.axm`, чтобы обновить `InitPelletAlignment()` по свежему файлу калибровки;
- затем снова запустить игру и визуально проверить новый проход.

## Session 949 - 2026-06-05 23:32 - ручная приёмка maze_chase подтверждена пользователем

Что сделано:
- по сообщению пользователя зафиксировано, что `samples/graphics/game/maze_chase.axm` вручную проверен в `Axiom.Ide` и работает корректно;
- активный следующий шаг в документах переведён с ручной калибровки `maze_chase` на продуктовый выбор следующего направления для современного графического слоя.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- считать текущую визуальную доводку и ручную приёмку `maze_chase` завершённой;
- не держать ручную калибровку точек активной задачей, пока не появится новое замечание по самому примеру;
- следующую развилку выбирать уже на уровне продукта: отдельный детерминированный `samples/smoke/engine-compare/` для сравнения движков или новый общий игровой helper поверх `CANVAS`.

Проверки:
- новых автоматических проверок не запускалось, потому что изменение было только документным и основано на ручной пользовательской приёмке.

Следующий точный шаг:
- решить, нужен ли сейчас отдельный сценарий `samples/smoke/engine-compare/` на основе современного графического цикла `maze_chase`;
- если нет, выбрать следующий общий игровой helper или следующий живой игровой пример для развития современного графического слоя.

## Session 950 - 2026-06-05 23:57 - современная графика получила адресный engine-compare и быстрый путь VM

Что сделано:
- добавлен новый производительный сценарий `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm`, который детерминированно нагружает `CANVAS`, `BEGINFRAME`, `PRESENT`, `CAMERA`, `RESETCAMERA`, `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE` и `DRAWTEXT`;
- `src/Axiom.ManualChecks/Program.cs` расширен графическим benchmark-host, чтобы `compare-engines` мог честно исполнять современные графические сценарии через `IGraphicsHost`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` частые современные графические опкоды переведены на прямой путь `frame.EvaluationStack.PopUnchecked()` вместо общего `PopValue(...)`;
- сначала первый адресный прогон показал почти полный паритет современного графического runtime (`1.006x`), после локального fast-path повторный прогон дал уже заметное преимущество `BytecodeVm`.

Изменённые файлы:
- `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- современный графический цикл `CANVAS` больше не считать непроверенной производительной зоной: теперь у него есть собственный адресный `engine-compare`;
- первым быстрым путём для графики считать не переписывание host-рендерера, а снижение накладных расходов VM на частых графических опкодах;
- следующим кандидатом на отдельный графический замер считать уже не примитивы кадра, а спрайтовый и image-путь `DRAWIMAGE` / `DRAWSPRITE`.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 3 --warmup 1 --filter "modern_canvas_primitive_burst.axm"`:
  - первый прогон до fast-path: runtime ratio `1.006x`, total ratio `0.984x`;
  - повторный прогон после fast-path: runtime ratio `0.793x`, total ratio `0.812x`.

Следующий точный шаг:
- решить, нужен ли отдельный `engine-compare` сценарий для `DRAWIMAGE` / `DRAWSPRITE` и спрайтовой нагрузки;
- если да, добавить короткий детерминированный сценарий со спрайтами и снова снять сравнение `Interpreter` против `BytecodeVm`.

## Session 951 - 2026-06-06 00:12 - добавлен отдельный sprite/image engine-compare для современной графики

Что сделано:
- добавлен новый сценарий `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`, который детерминированно нагружает `LOADIMAGE`, `LOADSPRITE`, `DRAWIMAGE`, `DRAWSPRITE`, `CAMERA`, `RESETCAMERA`, `BEGINFRAME`, `PRESENT` и HUD-текст без ручного ввода;
- `src/Axiom.ManualChecks/Program.cs` расширен вторым modern-graphics benchmark-case и поддержкой синтетической image/sprite-растеризации в `BenchmarkGraphicsHost`, чтобы `compare-engines` мерил не пустые графические вызовы, а реальную запись пикселей;
- адресный короткий прогон `compare-engines --iterations 3 --warmup 1 --filter "modern_canvas_sprite_burst.axm"` сначала показал рабочий паритет около `0.947x` runtime / `0.960x` total;
- затем длинный контрольный прогон `compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_sprite_burst.axm"` показал уже устойчивое число около `1.056x` runtime / `1.032x` total, то есть на спрайтовой и image-нагрузке `BytecodeVm` пока ещё чуть медленнее интерпретатора;
- быстрая локальная попытка дополнительно ужать `DRAWIMAGE` / `DRAWSPRITE` в `BytecodeVm.Graphics` была проверена отдельным повторным длинным прогоном и не дала улучшения, поэтому она откатана и в рабочем дереве оставлен только уже подтверждённый fast-path для частых примитивов кадра.

Изменённые файлы:
- `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- второй отдельный benchmark для современной графики считать добавленным и рабочим;
- современную графику больше не считать одной неделимой зоной: быстрые примитивы кадра уже закрыты, а `DRAWIMAGE` / `DRAWSPRITE` теперь выделены как отдельный производительный долг;
- не держать в коде непроверенную микрооптимизацию, если длинный прогон не подтверждает её пользу.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 3 --warmup 1 --filter "modern_canvas_sprite_burst.axm"`: runtime ratio `0.947x`, total ratio `0.960x`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_sprite_burst.axm"`: runtime ratio `1.056x`, total ratio `1.032x`;
- повторный длинный прогон после локальной попытки ужать `DRAWIMAGE` / `DRAWSPRITE` подтвердил, что эта попытка не улучшает результат, поэтому правка откатана до обновления документов.

Следующий точный шаг:
- профилировать путь `DRAWIMAGE` / `DRAWSPRITE` на `modern_canvas_sprite_burst.axm` уже как отдельную задачу;
- затем найти измеримый быстрый путь в VM или в benchmark-host-растеризации и повторить длинный адресный прогон.

## Session 952 - 2026-06-06 00:54 - image/sprite-путь современной графики дополнительно ускорен

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` добавлен `PeekUnchecked()`, чтобы безопасно смотреть аргументы fast-path без преждевременного разрушения стека;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен безопасный fast-path для built-in `RGB`, `LOADIMAGE` и `LOADSPRITE`, а таблица `BuiltinExecutionInfo` теперь хранит `CanonicalId` для такого адресного пути;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` `DRAWIMAGE` и `DRAWSPRITE` переведены на прямой integer/long-путь для координат, размеров и номера кадра без лишних общих `AsLong()`-конверсий на успешном случае;
- в `src/Axiom.ManualChecks/Program.cs` `BenchmarkGraphicsHost.DrawImageRegion(...)` переписан на отдельный 1:1 путь без масштабирования и на инкрементальный fixed-point путь для масштабирования вместо деления на каждом пикселе;
- короткий адресный прогон по `modern_canvas_sprite_burst.axm` дал рабочее ускорение до примерно `0.957x` runtime / `0.984x` total, а длинный контрольный прогон `modern_canvas_` закрепил итог около `1.033x` runtime / `1.031x` total для sprite/image-пути и около `0.980x` runtime / `0.976x` total для fast-primitive-пути.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- не откатывать fast-path built-in: после перевода на `PeekUnchecked()` он стал безопасным и не ломает обычный путь VM;
- считать текущий шаг полезным и измеримым, потому что отдельный sprite/image benchmark сдвинут вниз от прежних `1.056x` / `1.032x` до примерно `1.033x` / `1.031x` на длинном прогоне;
- не объявлять `DRAWIMAGE` / `DRAWSPRITE` полностью закрытыми по производительности, пока длинный контрольный прогон ещё оставляет небольшой хвост около трёх процентов.

Проверки:
- `dotnet build Axiom.sln`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 10 --warmup 2 --filter "modern_canvas_"`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 10 --warmup 2 --filter "modern_canvas_sprite_burst.axm"`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"`

Следующий точный шаг:
- снять более точечный профиль остатка в `modern_canvas_sprite_burst.axm`, чтобы разложить хвост между VM-диспетчеризацией `DRAWIMAGE` / `DRAWSPRITE` и хостовым `DrawImageRegion(...)`;
- после этого делать уже только следующую измеримую правку, а не ещё одну слепую серию микрооптимизаций.

## Session 953 - 2026-06-06 02:10 - modern_canvas_sprite_burst разложен до уровня опкодов

Что сделано:
- в `src/Axiom.ManualChecks/Program.cs` добавлена отдельная команда `profile-bytecode-opcodes`, которая компилирует выбранный `engine-compare`-сценарий в байткод, запускает его через `BytecodeVm` с наблюдателем инструкций и пишет отчёт по частотам опкодов, built-in и процедур;
- профиль `modern_canvas_sprite_burst.axm` показал, что основной хвост идёт не от самих `DrawImage` / `DrawSprite`, а от окружающих `PushConst`, `LoadLocal`, `Add`, `Convert`, `Multiply` и `Modulo`;
- после этого в `src/Axiom.Runtime/Execution/BytecodeVm.cs` ускорен успешный путь `Opcode.Convert`: он теперь снимает значение через `frame.EvaluationStack.PopUnchecked()` и не делает ранний `ResolveLocation(...)`, пока не нужен медленный откат;
- спорный integer fast-path для самих `DRAWIMAGE` / `DRAWSPRITE` дополнительно не возвращался: длинный прогон до этого уже показал, что он не даёт устойчивого выигрыша.

Изменённые файлы:
- `src/Axiom.ManualChecks/Program.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решения:
- считать новый опкодный профиль рабочим инструментом для следующих шагов по современной графике;
- считать быстрые примитивы `CANVAS` уже устойчиво ускоренными;
- не считать image/sprite-путь закрытым, пока длинные прогоны по `modern_canvas_sprite_burst.axm` не дадут повторяемое значение ниже `1.000x`.

Проверки:
- `dotnet build Axiom.sln`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "modern_canvas_sprite_burst.axm"`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_sprite_burst.axm"`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"`
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_sprite_burst.axm"`

Итоги измерений:
- опкодный профиль `modern_canvas_sprite_burst.axm`: `PushConst = 26211`, `LoadLocal = 23969`, `Add = 14160`, `Convert = 9762`, `Multiply = 9520`, `Modulo = 8120`, `DrawSprite = 1440`, `DrawImage = 960`;
- отдельный длинный прогон после ускорения `Convert` один раз дал около `0.988x` runtime / `0.979x` total;
- полный длинный прогон `modern_canvas_` на той же версии дал около `0.874x` runtime / `0.899x` total для `modern_canvas_primitive_burst.axm` и около `1.044x` runtime / `1.040x` total для `modern_canvas_sprite_burst.axm`;
- повторный длинный одиночный прогон `modern_canvas_sprite_burst.axm` дал около `1.060x` runtime / `1.054x` total, поэтому устойчивый выигрыш для image/sprite-пути пока не подтверждён.

Следующий точный шаг:
- расширить `profile-bytecode-opcodes` для `modern_canvas_sprite_burst.axm` до разбивки по видам `Convert` и по смешанным числовым комбинациям вокруг `Add`, `Multiply` и `Modulo`;
- затем сделать только одну следующую измеримую правку в горячей арифметике спрайтового цикла и снова повторить длинный контрольный прогон.

## Session 954 - 2026-06-06 02:38 - modern graphics переведён на целочисленный bytecode-путь

Что сделано:
- в `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` modern graphics lowering переведён на `LowerModernGraphicsIntegralExpression(...)`, чтобы не оставлять лишний `Convert(Long)` там, где исходное выражение уже `INTEGER` или `LONG`;
- в `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` modern graphics опкоды `CANVAS`, `CAMERA`, `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE`, `DRAWTEXT`, `DRAWIMAGE` и `DRAWSPRITE` переведены на проверку целочисленного стека `INTEGER|LONG`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs` тот же modern graphics путь VM доведён до контракта `INTEGER|LONG`, чтобы runtime не требовал жёстко `LONG` там, где lowering уже отдаёт `INTEGER`;
- в `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs` добавлен адресный тест на отсутствие лишних `Convert(Long)` в modern graphics lowering.

Изменённые файлы:
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Graphics.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj --filter "FullyQualifiedName~BoundProgramLowererTests"`: пройдено `39/39`;
- `dotnet build Axiom.sln`: успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_CompiledCanvas"`: пройдено `4/4`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "modern_canvas_sprite_burst.axm"`: `Convert` упал с `9762` до `80`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_sprite_burst.axm"`: около `0.981x` runtime / `1.013x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"`: `modern_canvas_primitive_burst.axm` около `0.823x` runtime / `0.826x` total, `modern_canvas_sprite_burst.axm` около `0.932x` runtime / `0.934x` total.

Решения:
- считать лишние `Convert(Long)` вокруг modern graphics снятым системным долгом lowering/validator/runtime;
- считать modern sprite/image-путь больше не отстающим от интерпретатора на полном адресном прогоне `modern_canvas_`;
- не возвращать экспериментальный integer fast-path для самих `DRAWIMAGE` / `DRAWSPRITE`, потому что устойчивый выигрыш пришёл от вычищения лишних конверсий и согласования стека.

Следующий точный шаг:
- если продолжать разгон современной графики, смотреть уже не на `Convert(Long)`, а на горячую арифметику `Add` / `Multiply` / `Modulo` в sprite/image-сценарии;
- иначе переходить к следующему производительному долгу VM вне уже выровненного modern graphics-пути.

## Session 955 - 2026-06-06 02:54 - metadata built-in современной графики получили быстрый bytecode-путь

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен прямой fast-path для built-in `IMAGEWIDTH`, `IMAGEHEIGHT`, `SPRITEWIDTH`, `SPRITEHEIGHT` и `SPRITECOUNT`, чтобы они не шли через общий `CallBuiltin` и не собирали лишний общий dispatch;
- там же добавлены узкие helper-методы `GetImageWidthFast`, `GetImageHeightFast`, `GetSpriteWidthFast`, `GetSpriteHeightFast`, `GetSpriteCountFast` поверх уже хранимых runtime-значений `IMAGE` и `SPRITE`;
- добавлен новый адресный benchmark `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm`, который детерминированно нагружает metadata built-in современной графики без ручного ввода;
- `src/Axiom.ManualChecks/Program.cs` расширен новым benchmark-case для этого metadata-сценария.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_CompiledImageWidthAndHeightReturnLoadedAndEmptySizes|FullyQualifiedName~Execute_CompiledSpriteWidthHeightAndCountReturnLoadedAndEmptySizes"`: пройдено `2/2`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_graphics_metadata_sweep.axm"`: около `0.447x` runtime / `0.455x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"`: `modern_canvas_primitive_burst.axm` около `0.896x` runtime / `0.907x` total, `modern_canvas_sprite_burst.axm` около `1.003x` runtime / `1.011x` total;
- два отдельных повторных длинных прогона `modern_canvas_sprite_burst.axm` дали около `0.834x` / `0.846x` и около `0.911x` / `0.936x`, то есть image/sprite-путь снова вернулся в рабочую зону быстрее интерпретатора;
- `dotnet build Axiom.sln`: успешно.

Решения:
- считать metadata built-in современной графики отдельным горячим подслоем, который теперь тоже ускорен и измерен адресным сценарием;
- считать текущий набор modern graphics операторов и связанных built-in в `BytecodeVm` доведённым до рабочего ускоренного состояния: примитивы, image/sprite-путь и metadata-путь все имеют отдельные быстрые ветки и отдельные измерения;
- не добавлять сейчас ещё один спекулятивный fast-path для `DRAWIMAGE` / `DRAWSPRITE`, пока следующий профильный сигнал не покажет новый устойчивый хвост.

Следующий точный шаг:
- если продолжать оптимизацию modern graphics, смотреть уже только на новый измеримый хвост из живого игрового сценария или на отдельную горячую арифметику вокруг sprite/image-циклов;
- иначе переключаться на следующий производительный долг VM вне уже ускоренного слоя современной графики.

## Session 956 - 2026-06-06 07:20 - maze_chase разложен до отдельного benchmark-а логики сетки

Что сделано:
- добавлен новый сценарий `samples/smoke/engine-compare/maze_chase_grid_logic.axm`, который выделяет из `samples/graphics/game/maze_chase.axm` только строковую карту, движение по клеткам, тоннели, выбор хода призрака и съедание точек без `CANVAS`, спрайтов и клавиатуры;
- `src/Axiom.ManualChecks/Program.cs` расширен новым benchmark-case `Игровая логика: сетка maze_chase без графики`;
- `docs/BYTECODE_PERF_MATRIX.md` и `docs/PLANS3.md` обновлены под новый выделенный игровой сценарий и новый вывод по долгу VM.

Изменённые файлы:
- `samples/smoke/engine-compare/maze_chase_grid_logic.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать сеточную игровую логику `maze_chase` подтверждённо не являющейся текущим долгом `BytecodeVm`, потому что в выделенном сценарии VM уже заметно быстрее интерпретатора;
- не искать причину возможной медлительности живой игры внутри `CanMove`, `ChooseGhostDirection`, `StepWalker` и съедания точек без нового сигнала, потому что их адресный прогон уже хороший;
- следующий шаг по `maze_chase`, если он понадобится, направлять уже в сторону смешанного графического кадра или ввода, а не обратно в чистую логику лабиринта.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 3 --warmup 1 --filter "maze_chase_grid_logic.axm"`:
  - около `0.683x` runtime / `0.694x` total.

Следующий точный шаг:
- если брать ближайший долг вне графики, перейти к `samples/smoke/engine-compare/bitwise_shift_mesh.axm`;
- если продолжать именно по `maze_chase`, выделить уже отдельный детерминированный сценарий на смешанный графический кадр без живой клавиатуры.

## Session 957 - 2026-06-06 07:37 - смешанный кадр maze_chase подтверждён быстрым в BytecodeVm

Что сделано:
- доведён до рабочего состояния новый адресный сценарий `samples/smoke/engine-compare/maze_chase_mixed_frame.axm`: исправлены вызовы процедур `SUB` на форме с `()`, а цикл benchmark-а переведён на базовую форму `NEXT`, чтобы сценарий корректно проходил frontend;
- подтверждено, что benchmark-case `Современная графика: смешанный кадр maze_chase` в `src/Axiom.ManualChecks/Program.cs` теперь реально исполняется и даёт успешный замер;
- `docs/BYTECODE_PERF_MATRIX.md` и `docs/PLANS3.md` обновлены по новому игровому графическому срезу.

Изменённые файлы:
- `samples/smoke/engine-compare/maze_chase_mixed_frame.axm`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать `maze_chase` уже дважды проверенным адресными сценариями: и по сеточной логике, и по смешанному кадру без ввода `BytecodeVm` сейчас быстрее интерпретатора;
- не держать `maze_chase` в ближайшем долге VM без нового живого сигнала, потому что подтверждённого проигрывающего среза в игре сейчас нет;
- следующий практический шаг снова смещается к `bitwise_shift_mesh.axm` или к другому семейству без отдельного benchmark-а.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 3 --warmup 1 --filter "maze_chase_mixed_frame.axm"`:
  - около `0.732x` runtime / `0.735x` total.

Следующий точный шаг:
- если брать ближайший долг вне графики, перейти к `samples/smoke/engine-compare/bitwise_shift_mesh.axm`;
- если продолжать именно по игровым сценариям, искать уже новый живой пример, у которого ещё нет отдельного адресного `engine-compare`.

## Session 958 - 2026-06-06 07:41 - bitwise_shift_mesh снят с ближайшего долга VM

Что сделано:
- перепроверен уже снятый длинный benchmark `samples/smoke/engine-compare/bitwise_shift_mesh.axm` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-073910.txt`;
- подтверждено, что длинний прогон `20` итераций с `3` прогревами держит около `0.855x` runtime / `0.856x` total, то есть побитовый подслой уже вошёл в целевой диапазон;
- `docs/BYTECODE_PERF_MATRIX.md` и `docs/PLANS3.md` синхронизированы с новым фактическим числом вместо старого `0.968x` / `0.968x`.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать `bitwise_shift_mesh.axm` уже не просто рабочим паритетом, а подтверждённо хорошим адресным результатом `BytecodeVm`;
- не держать побитовые операции и сдвиги ближайшим внеигровым долгом VM;
- считать, что и `maze_chase`, и `bitwise_shift_mesh` сейчас закрыты как кандидаты на ближайший разгон.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "bitwise_shift_mesh.axm"`:
  - около `0.855x` runtime / `0.856x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "bitwise_shift_mesh.axm"`:
  - профиль подтверждает низкоуровневую нагрузку в `LoadLocal`, `StoreLocal`, `PushConst`, `And`, `ShiftLeft`, `Xor`, `Or`, `Convert`, `Modulo` и `ShiftRight`.

Следующий точный шаг:
- искать следующий производительный долг `BytecodeVm` уже в другом семействе языка, у которого ещё нет отдельного адресного benchmark-а или у которого есть только косвенное участие в смешанных сценариях;
- к `maze_chase` и к побитовому подслою возвращаться только по новому измеримому сигналу.

## Session 959 - 2026-06-06 07:44 - string_concat_accumulator подтверждён длинным прогоном

Что сделано:
- снят длинный benchmark `samples/smoke/engine-compare/string_concat_accumulator.axm` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-074415.txt`;
- подтверждено, что прогон `20` итераций с `3` прогревами держит около `0.522x` runtime / `0.534x` total;
- `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы с новым устойчивым числом вместо старых коротких ориентиров.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать строковое накопление через `s = s & expr` и `s = s + expr` уже подтверждённо быстрым слоем `BytecodeVm`;
- не держать `string_concat_accumulator.axm` новым кандидатом на разгон VM;
- если смотреть только на уже измеренные адресные семейства, то ближе всего к следующему measured-шагу теперь остаются хостовые файловые сценарии около `0.985x .. 0.994x`.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "string_concat_accumulator.axm"`:
  - около `0.522x` runtime / `0.534x` total.

Следующий точный шаг:
- либо искать следующий ещё невыделенный benchmark-family;
- либо брать `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` или `samples/smoke/engine-compare/file_management_roundtrip.axm` как ближайшие уже измеренные слои к границе `1.000x`.

## Session 960 - 2026-06-06 07:48 - drive_temp_storage_sweep очищен от строкового шума

Что сделано:
- снят опкодный профиль старой версии `samples/smoke/engine-compare/drive_temp_storage_sweep.axm`, который показал лишнюю нагрузку не на disk/temp built-in, а на строковую сборку payload через `CHR`, `STR` и `TRIM`;
- сам сценарий упрощён: запись временных файлов переведена на заранее подготовленные строки без локальной функции `BuildPayload(...)`;
- после этого повторный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-074737.txt` показал уже ожидаемый верх по `WRITEFILE`, `DIRSIZE`, `FREESPACE`, `MAKETEMPFILE`, `MAKETEMPDIR`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE` и `PATHROOT`;
- повторный длинный benchmark `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-074752.txt` дал около `0.921x` runtime / `0.921x` total вместо старых `0.992x` / `0.994x`.

Изменённые файлы:
- `samples/smoke/engine-compare/drive_temp_storage_sweep.axm`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать старое число `drive_temp_storage_sweep.axm` завышенным из-за нечистого benchmark-а, а не из-за явного долга `BytecodeVm`;
- считать этот disk/temp-подслой уже заметно лучше прежней серой зоны и не брать его следующим объектом слепой микрооптимизации;
- считать, что среди уже измеренных файловых семейств ближе всего к следующему measured-шагу теперь остаётся `file_management_roundtrip.axm`.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "drive_temp_storage_sweep.axm"`:
  - старый профиль показал строковый шум в `CHR`, `STR`, `TRIM`;
  - новый профиль после правки показал уже реальные disk/temp built-in;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "drive_temp_storage_sweep.axm"`:
  - около `0.921x` runtime / `0.921x` total.

Следующий точный шаг:
- если брать ближайший measured-кандидат среди уже выделенных семейств, перейти к `samples/smoke/engine-compare/file_management_roundtrip.axm`;
- если снова расширять матрицу, искать следующий ещё невыделенный benchmark-family.

## Session 961 - 2026-06-06 08:57 - file_management_roundtrip переведён в рабочий паритет

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен прямой fast-path для file-management built-in, чтобы успешный путь шёл прямо со стека `BytecodeEvaluationStack` без общего `CallBuiltin`-dispatch;
- адресные runtime-проверки по файловым built-in прошли успешно;
- повторный длинный benchmark `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-085758.txt` дал около `0.969x` runtime / `0.969x` total вместо прежних `1.075x` / `1.074x`;
- `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы с новым статусом.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать management-срез файловых built-in больше не прямым долгом, а рабочим паритетом;
- считать, что среди уже выделенных неграфических адресных benchmark-семейств прямого долга `> 1.000x` сейчас не осталось;
- не возвращаться к `file_management_roundtrip.axm` без нового профильного сигнала или отдельного решения дожимать его уже от `0.969x` к `<= 0.900x`.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~WriteFile|FullyQualifiedName~DeleteFile|FullyQualifiedName~CopyFile|FullyQualifiedName~MoveFile|FullyQualifiedName~RenameFile|FullyQualifiedName~DeleteDirRecursive|FullyQualifiedName~CopyDir|FullyQualifiedName~MoveDir|FullyQualifiedName~RenameDir|FullyQualifiedName~CurrentDir|FullyQualifiedName~ChangeDir"`:
  - пройдено `4`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_management_roundtrip.axm"`:
  - около `0.969x` runtime / `0.969x` total.

Следующий точный шаг:
- если нужен ближайший measured-шаг вне графики, перейти к `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` как к ближайшему серому слою;
- если важнее новый сигнал, искать ещё неразложенное неграфическое benchmark-family.

## Session 962 - 2026-06-06 09:18 - disk/temp слой разложен на два честных адресных benchmark-а

Что сделано:
- в `samples/smoke/engine-compare/` добавлены два новых сценария: `drive_volume_metadata_sweep.axm` и `temp_storage_roundtrip.axm`;
- `src/Axiom.ManualChecks/Program.cs` обновлён, чтобы новые файловые сценарии участвовали в `compare-engines` и `profile-bytecode-opcodes`;
- длинний прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-091634.txt` показал около `0.724x` runtime / `0.736x` total для `drive_volume_metadata_sweep.axm`;
- длинний прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-091651.txt` показал около `0.961x` runtime / `0.960x` total для `temp_storage_roundtrip.axm`;
- отдельный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-091743.txt` показал, что внутри `temp_storage_roundtrip.axm` верх сидит в `COMBINEPATH` и `WRITEFILE`.

Изменённые файлы:
- `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm`
- `samples/smoke/engine-compare/temp_storage_roundtrip.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- не считать старый mixed `drive_temp_storage_sweep.axm` главным ориентиром для следующего measured-дожима;
- считать drive/volume-подслой уже снятым с повестки, потому что отдельный benchmark показал уверенное `0.724x`;
- считать новым ближайшим файловым серым слоем уже `temp_storage_roundtrip.axm`, а не весь прежний смешанный disk/temp-сценарий.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "drive_volume_metadata_sweep.axm"`:
  - около `0.724x` runtime / `0.736x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "temp_storage_roundtrip.axm"`:
  - около `0.961x` runtime / `0.960x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "temp_storage_roundtrip.axm"`:
  - верхние built-in: `COMBINEPATH`, `WRITEFILE`, затем `DELETEDIRRECURSIVE`, `DELETEFILE`, `DIRSIZE`, `FILESIZE`, `MAKEDIR`, `MAKETEMPDIR`, `MAKETEMPFILE`.

Следующий точный шаг:
- если продолжать measured-разгон вне графики, идти в `samples/smoke/engine-compare/temp_storage_roundtrip.axm`;
- если нужен другой сигнал, искать ещё неразложенное неграфическое benchmark-family.

## Session 963 - 2026-06-06 09:18 - `COMBINEPATH` отдельно дожал temp_storage_roundtrip

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` оставлен прямой fast-path для `COMBINEPATH` поверх уже существующего file-management fast-path;
- более широкая temp/file-пачка fast-path проверялась отдельно, но не дала устойчивого подтверждения и не была зафиксирована как итоговое решение;
- длинний контроль `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-093207.txt` по `samples/smoke/engine-compare/temp_storage_roundtrip.axm` (`40` итераций, `5` прогревов) дал около `0.960x` runtime / `0.960x` total;
- `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы под этот устойчивый ориентир.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать `COMBINEPATH` полезным локальным дожимом именно для честного temp/file-среза;
- не считать более широкий temp/file fast-path подтверждённым улучшением без устойчивого числа;
- считать, что ближайший measured-кандидат внутри `temp_storage_roundtrip.axm` теперь уже `WRITEFILE`.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~CombinePath|FullyQualifiedName~WriteFile|FullyQualifiedName~DeleteFile|FullyQualifiedName~DeleteDirRecursive|FullyQualifiedName~CurrentDir|FullyQualifiedName~ChangeDir"`:
  - пройдено `3`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 40 --warmup 5 --filter "temp_storage_roundtrip.axm"`:
  - около `0.960x` runtime / `0.960x` total.

Следующий точный шаг:
- если продолжать measured-разгон вне графики, идти в `samples/smoke/engine-compare/temp_storage_roundtrip.axm` и адресно смотреть `WRITEFILE`;
- если нужен другой сигнал, искать ещё неразложенное неграфическое benchmark-family.

## Session 964 - 2026-06-06 09:48 - `DIRSIZE` вытянул temp size/cleanup-подслой в паритет

Что сделано:
- temp/file-семейство дополнительно разложено на новые сценарии `temp_creation_write_cycle.axm`, `temp_size_cleanup_cycle.axm`, `temp_size_probe.axm`, `temp_cleanup_probe.axm`, `temp_filesize_probe.axm` и `temp_dirsize_probe.axm`;
- замеры показали такой срез: `temp_creation_write_cycle.axm` около `0.925x` / `0.920x`, `temp_cleanup_probe.axm` около `0.924x` / `0.923x`, `temp_size_probe.axm` около `0.942x` / `0.942x`, `temp_filesize_probe.axm` около `0.884x` / `0.885x`;
- длинный контроль `temp_size_cleanup_cycle.axm` (`40/5`) сначала подтвердил прямой долг около `1.058x` / `1.064x`;
- после этого в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен прямой fast-path для `DIRSIZE`;
- новый `temp_dirsize_probe.axm` после правки стал держаться около `0.923x` / `0.923x`, а `temp_size_cleanup_cycle.axm` на повторном длинном контроле `40/5` ушёл до примерно `0.983x` / `0.982x`.

Изменённые файлы:
- `samples/smoke/engine-compare/temp_creation_write_cycle.axm`
- `samples/smoke/engine-compare/temp_size_cleanup_cycle.axm`
- `samples/smoke/engine-compare/temp_size_probe.axm`
- `samples/smoke/engine-compare/temp_cleanup_probe.axm`
- `samples/smoke/engine-compare/temp_filesize_probe.axm`
- `samples/smoke/engine-compare/temp_dirsize_probe.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать, что size/cleanup-половина temp/file-семейства больше не является прямым долгом VM;
- считать `DIRSIZE` закрытым как ближайший узкий measured-кандидат;
- перенести следующий точный шаг к creation/write-половине, где сверху остаются `COMBINEPATH` и `WRITEFILE`, но `COMBINEPATH` уже частично дожат.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~DirSize|FullyQualifiedName~FreeSpace|FullyQualifiedName~FileSystemReturnsCurrentDirectorySizeAndFreeSpace|FullyQualifiedName~WriteFile|FullyQualifiedName~DeleteDirRecursive|FullyQualifiedName~CombinePath"`:
  - пройдено `2`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "temp_dirsize_probe.axm"`:
  - около `0.923x` runtime / `0.923x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 40 --warmup 5 --filter "temp_size_cleanup_cycle.axm"`:
  - около `0.983x` runtime / `0.982x` total.

Следующий точный шаг:
- если продолжать measured-разгон вне графики, идти в `samples/smoke/engine-compare/temp_creation_write_cycle.axm`;
- следующим узким кандидатом внутри него брать `WRITEFILE`.

## Session 965 - 2026-06-06 10:05 - длинный контроль снял ложный хвост у `WRITEFILE`

Что сделано:
- проверен более длинний контроль `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-100158.txt` для `samples/smoke/engine-compare/temp_writefile_probe.axm` с `40` итерациями и `5` прогревами;
- этот контроль показал уже около `0.929x` runtime / `0.929x` total, хотя короткий прогон `20/3` один раз шумел около `1.015x`;
- дополнительно подтверждено, что `samples/smoke/engine-compare/temp_path_creation_probe.axm` держится около `0.998x` runtime / `0.996x` total;
- `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы под этот более честный вывод.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать короткий серый результат у `temp_writefile_probe.axm` шумом, а не подтверждённым долгом VM;
- считать, что отдельного прямого хвоста у `WRITEFILE` на текущем temp/file-разрезе сейчас не видно;
- не продолжать слепой дожим temp/file-половины без нового устойчивого профильного сигнала.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "temp_writefile_probe.axm"`:
  - один шумный прогон дал около `1.015x` runtime / `1.015x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 40 --warmup 5 --filter "temp_writefile_probe.axm"`:
  - около `0.929x` runtime / `0.929x` total.

Следующий точный шаг:
- если продолжать measured-разгон вне графики, искать уже другой ещё невыделенный неграфический benchmark-family;
- к temp/file-слою возвращаться только по новому устойчивому сигналу, а не по старой гипотезе про `WRITEFILE`.

## Session 966 - 2026-06-06 10:15 - `array_staging_roundtrip` снял гипотезу про скрытый массивный хвост

Что сделано:
- доведена синхронизация реестров вокруг `samples/smoke/engine-compare/array_staging_roundtrip.axm`, который уже был подключён в `Axiom.ManualChecks`, но ещё не был отражён в активных документах;
- по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-094149.txt` зафиксирован результат около `0.474x` runtime / `0.580x` total;
- этим отдельным срезом подтверждено, что двухфазный staging через массивы строк и чисел сам по себе не является хвостом VM и не скрывает старый temp/file-долг.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать массивный staging-подслой уже уверенно быстрым;
- не возвращаться к гипотезе о скрытом массивном хвосте внутри temp/file-семейства;
- следующий measured-поиск долга вести уже в другом неграфическом family или по новому профильному сигналу.

Проверки:
- использован уже полученный длинный замер `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-094149.txt` для `samples/smoke/engine-compare/array_staging_roundtrip.axm`:
  - около `0.474x` runtime / `0.580x` total.

Следующий точный шаг:
- если продолжать measured-разгон вне графики, искать следующую отдельную дыру в coverage/perf-матрице, а не дожимать уже снятые temp/file и staging-гипотезы;
- к коду `BytecodeVm` возвращаться только под новый устойчивый замер или новый опкодный профиль.

## Session 967 - 2026-06-06 10:24 - верхние строки реестров досинхронизированы со строковым и процедурным слоями

Что сделано:
- верхняя строка строкового семейства в `docs/BYTECODE_PERF_MATRIX.md` дополнена отдельным адресным сценарием `samples/smoke/engine-compare/string_concat_accumulator.axm` и его устойчивым числом около `0.522x` runtime / `0.534x` total;
- строка `Процедуры и функции` в `docs/BYTECODE_COVERAGE.md` согласована с уже существующим адресным benchmark `samples/smoke/engine-compare/routine_closure_pipeline.axm`, который держится около `0.861x` runtime / `0.874x` total;
- `docs/PLANS3.md` синхронизирован так, чтобы следующий шаг уже искал новую дыру, а не старый документный несинхрон.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать, что верхние строки реестров теперь честно отражают уже измеренные строковый и процедурный подслои;
- не тратить следующий шаг на повторную досинхронизацию этих уже закрытых областей;
- искать новый внеграфический measured-долг уже по отдельному family или по новому профильному сигналу.

Проверки:
- использованы уже зафиксированные длинные замеры:
  - `samples/smoke/engine-compare/string_concat_accumulator.axm`: около `0.522x` runtime / `0.534x` total;
  - `samples/smoke/engine-compare/routine_closure_pipeline.axm`: около `0.861x` runtime / `0.874x` total.

Следующий точный шаг:
- если продолжать работу по VM вне графики, искать family без собственного адресного `engine-compare`-сценария;
- если по матрице явной новой дыры не видно, переходить к точечной досверке строк `Негативный паритет`, где пока ещё стоит `частично`.

## Session 968 - 2026-06-06 10:29 - shortcut нормализации путей отклонён после длинного temp/file-контроля

Что сделано:
- после документной синхронизации отдельно проверен внутренний shortcut в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.FileSystem.cs`, который должен был пропускать `ExpandEnvironmentVariables + GetFullPath` на уже абсолютных канонических путях;
- точечные runtime-тесты файлового слоя прошли успешно: `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~FileSystemPathHelpersReturnExpectedValues|FullyQualifiedName~BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles|FullyQualifiedName~BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories|FullyQualifiedName~BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|FullyQualifiedName~BuiltinFileSystemCreatesTemporaryFileAndDirectory|FullyQualifiedName~BuiltinFileSystemReturnsExtendedPathAndDriveInfo"` дал `6/6`;
- длинный `file_management_roundtrip.axm` после этой локальной правки показал около `0.960x` runtime / `0.960x` total;
- длинный `temp_path_creation_probe.axm` показал сначала около `0.907x` / `0.904x`, а повторно около `0.922x` / `0.923x`;
- но решающий длинный `temp_storage_roundtrip.axm` на чистом последовательном контроле `40/5` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-102811.txt` дал уже около `1.011x` runtime / `1.012x` total;
- после этого shortcut из `BytecodeVm.Builtins.FileSystem.cs` сразу убран и не оставлен в итоговом состоянии дерева.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.FileSystem.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать shortcut нормализации путей ложным локальным следом, а не подтверждённым улучшением VM;
- не держать его как рабочую оптимизацию, потому что честный temp/file-контроль ушёл обратно в прямой долг;
- следующий measured-шаг внутри файлового слоя искать уже не в `NormalizeFileSystemPath(...)`, а в другом узком подслое.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~FileSystemPathHelpersReturnExpectedValues|FullyQualifiedName~BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles|FullyQualifiedName~BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories|FullyQualifiedName~BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|FullyQualifiedName~BuiltinFileSystemCreatesTemporaryFileAndDirectory|FullyQualifiedName~BuiltinFileSystemReturnsExtendedPathAndDriveInfo"`:
  - пройдено `6`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_management_roundtrip.axm"`:
  - около `0.960x` runtime / `0.960x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 40 --warmup 5 --filter "temp_path_creation_probe.axm"`:
  - около `0.907x` runtime / `0.904x` total, повторно около `0.922x` / `0.923x`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 40 --warmup 5 --filter "temp_storage_roundtrip.axm"`:
  - около `1.011x` runtime / `1.012x` total на чистом последовательном контроле.

Следующий точный шаг:
- если продолжать разгон файлового слоя, снимать новый адресный профиль уже для `DELETEFILE`, `DELETEDIRRECURSIVE`, `CHANGEDIR` и `CURRENTDIR` как верхних built-in в `file_management_roundtrip`;
- если уходить от файлового слоя, брать новую внеграфическую дыру в матрице или досверять строки `Негативный паритет` со статусом `частично`.

## Session 969 - 2026-06-06 10:32 - cleanup/navigation-подслой файлов выделен в отдельный probe

Что сделано:
- добавлен новый сценарий `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm`, который нагружает прежде всего `DELETEFILE`, `DELETEDIRRECURSIVE`, `CHANGEDIR` и `CURRENTDIR` с минимальной примесью `COPY*`/`MOVE*`;
- `src/Axiom.ManualChecks/Program.cs` обновлён, чтобы этот сценарий участвовал в `compare-engines`;
- длинный прогон `20` итераций с `3` прогревами по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-103142.txt` дал около `0.968x` runtime / `0.969x` total;
- `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md` синхронизированы под новый подслой.

Изменённые файлы:
- `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать cleanup/navigation-подслой файлов теперь выделенным отдельно и честно измеренным;
- считать, что этот подслой уже в рабочем паритете, но всё ещё не в целевом диапазоне `<= 0.900x`;
- использовать его как следующую чистую точку для опкодного профиля, если продолжать дожимать файловый слой.

Проверки:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_cleanup_navigation_probe.axm"`:
  - около `0.968x` runtime / `0.969x` total.

Следующий точный шаг:
- если продолжать measured-разгон файлового слоя, снять `profile-bytecode-opcodes --filter "file_cleanup_navigation_probe.axm"` и уже по нему решать, что дожимать первым: `DELETEFILE`, `DELETEDIRRECURSIVE`, `CHANGEDIR` или `CURRENTDIR`;
- если уйти от файлового слоя, брать другую внеграфическую дыру в матрице или точечно досверять `Негативный паритет`.

## Session 970 - 2026-06-06 10:38 - `CURRENTDIR` заметно подтянул cleanup/navigation-подслой

Что сделано:
- новый `file_cleanup_navigation_probe.axm` дополнительно упрощён, чтобы меньше шумели `COMBINEPATH`, `STR`, `TRIM`, лишняя запись и лишние файловые пути;
- повторный опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-103510.txt` показал уже куда более честный верх по `CURRENTDIR`, `CHANGEDIR`, `DELETEDIRRECURSIVE`, `DELETEFILE`, при том что `MAKEDIR` и `WRITEFILE` остались только как поддерживающий фон сценария;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` для fast-path `CURRENTDIR` убрана лишняя повторная нормализация `Environment.CurrentDirectory`;
- точечные runtime-тесты `CURRENTDIR`/`CHANGEDIR` прошли: `3/3`;
- новый длинный прогон `20/3` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-103726.txt` дал около `0.917x` runtime / `0.918x` total вместо прежних примерно `0.968x` / `0.969x` у старой версии cleanup/navigation-среза.

Изменённые файлы:
- `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать узкий fast-path `CURRENTDIR` подтверждённым улучшением VM;
- считать cleanup/navigation-подслой файлов заметно подтянутым и почти доведённым до целевого диапазона;
- следующий measured-кандидат внутри этого же подслоя переносится на `CHANGEDIR`.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace|FullyQualifiedName~BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|FullyQualifiedName~BuiltinFileSystemReportsMissingDirectoryForChangeDir"`:
  - пройдено `3`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_cleanup_navigation_probe.axm"`:
  - около `0.917x` runtime / `0.918x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --filter "file_cleanup_navigation_probe.axm"`:
  - верхние built-in: `CURRENTDIR`, `MAKEDIR`, `CHANGEDIR`, `DELETEDIRRECURSIVE`, `DELETEFILE`, `WRITEFILE`.

Следующий точный шаг:
- если продолжать разгон файлового слоя, адресно дожимать `CHANGEDIR` в `file_cleanup_navigation_probe.axm`;
- если менять направление, брать другую внеграфическую дыру или точечно досверять `Негативный паритет`.

## Session 971 - 2026-06-06 10:44 - `CHANGEDIR` довёл cleanup/navigation-срез почти до цели

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` для fast-path `CHANGEDIR` убрана отдельная предварительная `Directory.Exists(...)`; успешный путь теперь сразу идёт в `Directory.SetCurrentDirectory(...)`, а отсутствие каталога отличаем через `DirectoryNotFoundException`;
- точечные runtime-тесты файлового слоя снова прошли: `3/3`;
- новый длинный прогон `file_cleanup_navigation_probe.axm` (`20/3`) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104234.txt` дал уже около `0.900x` runtime / `0.901x` total;
- отдельный широкий контроль `file_management_roundtrip.axm` (`20/3`) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104325.txt` дал около `0.975x` runtime / `0.974x` total, то есть локальный выигрыш пока не поднял весь management-срез.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать узкий fast-path `CHANGEDIR` подтверждённым улучшением внутри cleanup/navigation-подслоя;
- считать этот подслой практически доведённым до целевого диапазона;
- не считать весь `file_management_roundtrip` автоматически подтянутым, пока это не подтверждено отдельным честным замером.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|FullyQualifiedName~BuiltinFileSystemReportsMissingDirectoryForChangeDir|FullyQualifiedName~BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace"`:
  - пройдено `3`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_cleanup_navigation_probe.axm"`:
  - около `0.900x` runtime / `0.901x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_management_roundtrip.axm"`:
  - около `0.975x` runtime / `0.974x` total.

Следующий точный шаг:
- если продолжать дожим именно этого подслоя, смотреть `DELETEFILE` и `DELETEDIRRECURSIVE`;
- если уходить на другой фронт, брать новую внеграфическую дыру или точечно досверять `Негативный паритет`.

## Session 972 - 2026-06-06 10:51 - `DELETEFILE` и `DELETEDIRRECURSIVE` сняли узкий cleanup/navigation-хвост

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` для fast-path `DELETEFILE` и `DELETEDIRRECURSIVE` убраны лишние предварительные проверки `File.Exists(...)` и `Directory.Exists(...)`;
- отсутствие файла или каталога на пользовательском пути по-прежнему возвращает `FALSE`, но теперь определяется уже через `FileNotFoundException`, `DirectoryNotFoundException` и безопасный `IOException`-фильтр;
- точечные runtime-тесты файлового слоя снова прошли: `3/3`;
- новый длинный прогон `file_cleanup_navigation_probe.axm` (`20/3`) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104930.txt` дал около `0.816x` runtime / `0.818x` total;
- отдельный широкий контроль `file_management_roundtrip.axm` (`20/3`) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-105022.txt` дал только около `0.996x` runtime / `0.996x` total.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать узкий cleanup/navigation-подслой файлов больше не производительным долгом `BytecodeVm`;
- не считать весь `file_management_roundtrip` ускоренным этим же шагом, потому что широкий management-срез остался только около паритета;
- следующий measured-поиск долга переносить либо на другой файловый подслой, либо уже вне файлового семейства.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "Execute_CompiledFileSystemBuiltinReportsMissingFile|Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace"`:
  - пройдено `3`, не пройдено `0`;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_cleanup_navigation_probe.axm"`:
  - около `0.816x` runtime / `0.818x` total;
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines --iterations 20 --warmup 3 --filter "file_management_roundtrip.axm"`:
  - около `0.996x` runtime / `0.996x` total.

Следующий точный шаг:
- если оставаться в файловом слое, дробить уже не cleanup/navigation, а другой смешанный подслой внутри `file_management_roundtrip` или `temp_storage_roundtrip`;
- если уходить из файлового слоя, брать следующую внеграфическую дыру из матрицы или строки `Негативный паритет` со статусом `частично`.

## Session 973 - 2026-06-06 11:20 - файловый cleanup/navigation-хвост закрыт и синхронизирован в реестрах

Что сделано:
- досинхронизированы `docs/BYTECODE_PERF_MATRIX.md` и `docs/BYTECODE_COVERAGE.md` под финальные измерения файлового слоя: `file_cleanup_navigation_probe.axm` около `0.816x` runtime / `0.818x` total, `file_management_roundtrip.axm` около `0.996x` / `0.996x`, `temp_creation_write_cycle.axm` около `0.937x` / `0.939x`, `temp_writefile_probe.axm` около `0.923x` / `0.923x`;
- в реестрах явно зафиксировано, что полезные file fast-path для `CURRENTDIR`, `CHANGEDIR`, `DELETEFILE` и `DELETEDIRRECURSIVE` оставлены в коде, а два ложных пути отвергнуты: снятие `Exists` у `COPYFILE` / `MOVEFILE` / `RENAMEFILE` и отдельный byte-cache для `WRITEFILE`;
- в `docs/BYTECODE_COVERAGE.md` добавлен адресный bytecode-тест `Run_WithBytecodeMode_ReportsCopyFileMissingSourceFromVm()` как подтверждение негативного договора VM для отсутствующего исходника `COPYFILE`;
- в `docs/PLANS3.md` добавлена новая точка остановки: cleanup/navigation-хвост закрыт, а следующий measured-поиск нужно переносить уже не на слепой файловый дожим.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать файловый cleanup/navigation-хвост закрытым не только в коде, но и в проектных реестрах;
- считать широкий management-срез и temp/file-семейство текущим рабочим паритетом, а не отдельным подтверждённым долгом VM;
- не продолжать микрооптимизации файлового слоя без нового устойчивого профиля хуже `1.000x`.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_BuiltinFileSystemReportsCopyFileMissingSource|FullyQualifiedName~Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively|FullyQualifiedName~Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace|FullyQualifiedName~Execute_BuiltinFileSystemReportsMissingDirectoryForChangeDir"`:
  - пройдено `4`, не пройдено `0`;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Run_WithBytecodeMode_ReportsCopyFileMissingSourceFromVm"`:
  - пройдено `1`, не пройдено `0`.

Следующий точный шаг:
- искать следующий долг VM уже вне закрытого cleanup/navigation-подслоя: либо по новой внеграфической benchmark-семье, либо по строкам `Негативный паритет` со статусом `частично`;
- если возвращаться к файловому слою, сначала снимать новый адресный профиль другого смешанного подслоя, а не продолжать дожим уже закрытого хвоста.

## Session 974 - 2026-06-06 11:26 - modern graphics получил runnable negative-parity для второго среза

Что сделано:
- добавлены три отрицательных runnable sample для современного `CANVAS`: `samples/graphics/modern/missing_canvas_error.axm`, `samples/graphics/modern/negative_radius_error.axm`, `samples/graphics/modern/invalid_text_position_error.axm`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` расширен: `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity(...)` теперь прогоняет эти три сценария через interpreter и `BytecodeVm` с проверкой одинаковых `MB3010` / `MB3007`;
- `docs/BYTECODE_COVERAGE.md` обновлён: строка второго modern-graphics-среза переведена с `Негативный паритет: частично` на `да`.

Изменённые файлы:
- `samples/graphics/modern/missing_canvas_error.axm`
- `samples/graphics/modern/negative_radius_error.axm`
- `samples/graphics/modern/invalid_text_position_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать negative-parity хвост второго современного графического среза закрытым;
- не возвращаться к этой области ради новых отрицательных sample, пока не появится новый пользовательский договор или новая диагностика;
- следующий короткий долг брать уже из других строк `Негативный паритет: частично`.

Проверки:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity"`:
  - пройдено `13`, не пройдено `0`;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~Execute_CanvasOperationsReportDocumentedDiagnostics|FullyQualifiedName~Execute_CompiledCanvasOperationsReportDocumentedDiagnostics"`:
  - пройдено `2`, не пройдено `0`.

Следующий точный шаг:
- взять следующую строку `Негативный паритет: частично`, где уже есть runtime/VM-покрытие, но ещё не хватает runnable negative sample;
- если такой короткий хвост не найдётся без новой семантики, переключаться на новую benchmark-семью вне графики.

## Session 975 - 2026-06-06 22:01 - закреплён строгий компактный агентный режим

Что сделано:
- после обязательного перечитывания `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md` отдельно разобран запрос на экономию токенов через внешние агентные репозитории;
- проверено, что линия `Headroom` у проекта уже частично есть: `docs/agent/HEADROOM_PILOT.md`, `docs/agent/README.md`, `README.md` и локальный `scripts/ai-context.ps1`;
- в `AGENTS.md` добавлен постоянный раздел `Правило компактного агентного режима и экономии токенов`, который закрепляет приоритет локального `docs/agent/`-слоя, обязательное тематическое сужение контекста и запрет превращать внешние task/memory-инструменты во второй источник правды;
- устаревший `docs/agent/CURRENT_STATE.md` переписан под реальное текущее состояние проекта вместо старого шага с первым modern-graphics-срезом;
- добавлен `docs/agent/EXTERNAL_TOOLS_EVALUATION.md` с проектным решением по `Headroom`, `RTK`, `Task Master` и `Caveman`;
- `docs/agent/README.md`, `docs/agent/PLAYBOOKS.md` и `README.md` синхронизированы с новым агентным контуром.

Изменённые файлы:
- `AGENTS.md`
- `README.md`
- `docs/agent/CURRENT_STATE.md`
- `docs/agent/EXTERNAL_TOOLS_EVALUATION.md`
- `docs/agent/PLAYBOOKS.md`
- `docs/agent/README.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать локальный `docs/agent/`-слой главным практическим способом экономии контекста для `Axiom`;
- считать `Headroom` полезным внешним ускорителем для тяжёлых логов и сводок, но не обязательной частью проекта;
- считать `RTK` для `Axiom` полезным только частично, потому что его сильный Bash-hook путь плохо совпадает с Windows/PowerShell-контуром проекта;
- не принимать `Task Master` как основной task-контур проекта, чтобы не создавать второй активный журнал рядом с `docs/PLANS3.md` и `docs/WORKLOG4.md`;
- адаптировать из `Caveman` только принцип краткости agent-файлов, а не его стиль как готовую persona.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/ai-context.ps1 -StdoutOnly` проходит;
- `git diff --check` не нашёл ошибок пробелов.

Следующий точный шаг:
- при следующем process-oriented проходе дополнительно выровнять agent-файлы, где ещё осталась старая английская или избыточная форма;
- по продуктовой линии проекта продолжать активный технический шаг из `docs/PLANS3.md`: брать следующую строку `Негативный паритет: частично` или новую внеграфическую benchmark-семью.

## Session 976 - 2026-06-06 22:42 - maze_chase досинхронизирован по отдельному игровому вводу

Что сделано:
- досинхронизированы `docs/BYTECODE_PERF_MATRIX.md` и `docs/BYTECODE_COVERAGE.md` под новый адресный сценарий `samples/smoke/engine-compare/maze_chase_input_polling.axm`;
- в матрице производительности отдельно зафиксировано, что у `maze_chase` теперь есть три адресных среза: `maze_chase_grid_logic.axm`, `maze_chase_input_polling.axm` и `maze_chase_mixed_frame.axm`;
- в реестре покрытия отдельно зафиксирован игровой клавиатурный подслой `KEYPRESS` / `KEYDOWN` / `UCASE` / `LCASE` поверх `maze_chase`, включая benchmark-case `Игровой ввод: maze_chase без графики` из `src/Axiom.ManualChecks/Program.cs`;
- в `docs/PLANS3.md` добавлена новая точка остановки: и сетка, и ввод, и смешанный кадр `maze_chase` уже быстрее интерпретатора, поэтому слепой разгон игры больше не является честным следующим шагом.

Изменённые файлы:
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать текущий measured-разбор `maze_chase` завершённым на трёх адресных срезах;
- не продолжать микроразгон modern graphics или игрового ввода без нового живого профильного сигнала;
- следующий долг VM искать уже либо в ещё невыделенном подслое рядом с игрой, либо в новой benchmark-семье вне `maze_chase`.

Проверки:
- новые проверки не запускались, потому что в этом ходе менялась только документация;
- зафиксированы уже полученные числа из последних отчётов: `maze_chase_input_polling.axm` около `0.739x` runtime / `0.742x` total, опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-224029.txt`.

Следующий точный шаг:
- если продолжать именно игровую линию, сначала искать ещё невыделенный адресный подслой `maze_chase`, а не возвращаться к уже быстрым сетке, вводу или смешанному кадру;
- если такого подслоя нет, переносить следующий measured-поиск на новую внеграфическую benchmark-семью или другой свежий профильный сигнал.

## Session 977 - 2026-06-06 22:50 - `FOR` получил отдельный отрицательный sample для неподдерживаемого типа счётчика

Что сделано:
- добавлен runnable negative sample `samples/control-flow/for/predeclared_counter_invalid_type.axm` для ошибки `MB2009`, когда заранее объявленный счётчик `FOR` имеет тип `STRING`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` дополнен двумя адресными проверками: sample включён в общий `NegativeAcceptanceProgram_ReturnsExpectedDiagnostic(...)`, а также добавлена отдельная parity-проверка одинаковой семантической диагностики между режимами `Interpreter` и `Bytecode`;
- `docs/BYTECODE_COVERAGE.md` обновлён: строка заранее объявленного счётчика `FOR` переведена с `Негативный паритет: частично` на `да`;
- `docs/PLANS3.md` досинхронизирован под новую точку остановки и следующий короткий parity-хвост.

Изменённые файлы:
- `samples/control-flow/for/predeclared_counter_invalid_type.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать отрицательный parity-хвост по заранее объявленному счётчику `FOR` закрытым;
- следующий короткий шаг брать уже из другой строки `Негативный паритет: частично`.

Проверки:
- запланирован адресный прогон `tests/Axiom.Execution.Tests` по новому negative sample и semantic parity.

Следующий точный шаг:
- закрывать следующую короткую строку `Негативный паритет: частично`, где уже есть тесты и пользовательский договор, но ещё не хватает отдельного runnable negative sample.

## Session 978 - 2026-06-06 22:57 - `ON KEY` получил отдельный отрицательный sample для неверного обработчика

Что сделано:
- добавлен runnable negative sample `samples/operators/on-key/invalid_handler_parameter.axm` для ошибки `MB2034`, когда `ON KEY` получает `SUB` с параметром `INTEGER`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` дополнен двумя адресными проверками: sample включён в общий `NegativeAcceptanceProgram_ReturnsExpectedDiagnostic(...)`, а также добавлена отдельная parity-проверка одинаковой семантической диагностики между режимами `Interpreter` и `Bytecode`;
- `docs/BYTECODE_COVERAGE.md` обновлён: строка `ON KEY` переведена с `Негативный паритет: частично` на `да`;
- `docs/PLANS3.md` досинхронизирован под новую точку остановки и следующий короткий parity-хвост.

Изменённые файлы:
- `samples/operators/on-key/invalid_handler_parameter.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать отрицательный parity-хвост по `ON KEY` закрытым;
- следующий короткий шаг брать уже из другой строки `Негативный паритет: частично`.

Проверки:
- запланирован адресный прогон `tests/Axiom.Execution.Tests` по новому negative sample и semantic parity.

Следующий точный шаг:
- закрывать следующую короткую строку `Негативный паритет: частично`, где уже есть тесты и пользовательский договор, но ещё не хватает отдельного runnable negative sample.

## Session 979 - 2026-06-06 23:23 - графические negative-parity хвосты реестра закрыты

Что сделано:
- добавлены runnable negative sample `samples/operators/circle/text_screen_error.axm`, `samples/functions/point/invalid_selector_error.axm`, `samples/operators/color/text_screen_error.axm` и `samples/operators/paint/text_screen_error.axm`;
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` расширен: `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity(...)` теперь проверяет эти четыре графических сценария вместе с уже существующими отрицательными sample;
- `docs/BYTECODE_COVERAGE.md` досинхронизирован по графическому срезу: старые строки `частично` для `CIRCLE`, `POINT(number)`, `COLOR`, базового и полного `PAINT`, `SCREEN 0`, `DRAW`, `VIEW`, `WINDOW` и `VIEW PRINT` переведены на фактический статус `да`, где долг был уже закрыт поздними блоками или новым runnable sample.

Изменённые файлы:
- `samples/operators/circle/text_screen_error.axm`
- `samples/functions/point/invalid_selector_error.axm`
- `samples/operators/color/text_screen_error.axm`
- `samples/operators/paint/text_screen_error.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать именно графическую десятку хвостов из реестра закрытой;
- не продолжать слепой обход старых графических строк `частично`, пока не появится новый пользовательский договор, новая диагностика или новый графический оператор.

Проверки:
- `powershell -ExecutionPolicy Bypass -File scripts/dotnet-test-artifacts.ps1 tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity"`
- результат: `19/19` тестов пройдено.

Следующий точный шаг:
- возвращаться к основной линии развития современного графического слоя и новых операторов, а не к уже закрытому графическому parity-хвосту.
## Session 980 - 2026-06-06 23:54 - текстовые helper-функции CANVAS доведены до документации и реестров

Что сделано:
- доведена спецификация современного графического слоя: в `docs/SPECIFICATION3.md` добавлены `TEXTWIDTH(text)` и `TEXTHEIGHT(text)`, общий договор активного `CANVAS`, фиксированная сетка текста `8 x 12` и отдельный раздел по текстовым метрикам;
- обновлён user-facing manual `docs/manual/modern-graphics.html`: синтаксис, описание текстового договора, диагностика `MB3010` и ссылки на новые runnable-примеры;
- обновлены `docs/manual/scripts/search-index.js` и `docs/manual/scripts/manual.js`, чтобы `TEXTWIDTH` и `TEXTHEIGHT` находились через указатель и поиск;
- в `docs/BYTECODE_COVERAGE.md` добавлена отдельная запись по текстовым helper-функциям современного `CANVAS`;
- подготовлены к приёмке новые примеры `samples/functions/textwidth/hud_metrics.axm` и `samples/functions/textheight/multiline_label.axm` как опорные acceptance-сценарии этого шага.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/manual/modern-graphics.html`
- `docs/manual/scripts/search-index.js`
- `docs/manual/scripts/manual.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Решение:
- считать `TEXTWIDTH` и `TEXTHEIGHT` частью основного современного `CANVAS`-договора, а не внутренним helper без пользовательской документации;
- фиксировать текущую текстовую модель как намеренно простую: моноширинная ячейка `8 x 12`, `CHR(10)` как новая строка, `CHR(13)` игнорируется.

Проверки:
- запланирован последовательный адресный прогон `tests/Axiom.Runtime.Tests`, `tests/Axiom.Execution.Tests` и `tests/Axiom.Ide.Tests` по текстовым метрикам и подсветке built-in;
- binder-проверки `TEXTWIDTH` / `TEXTHEIGHT` уже пройдены отдельным адресным прогоном.

Следующий точный шаг:
- брать следующий компактный helper поверх `CANVAS` для HUD и экранных подложек;
- не уводить следующий шаг обратно в старые неграфические долги без нового прямого сигнала по продуктовой линии графики.

## Session 981 - 2026-06-07 00:03 - адресные тесты text metrics дочищены

Что сделано:
- исправлены четыре тестовых утверждения после изменения тестового контракта вывода: в двух runtime-тестах проверка возвращена на `host.Output`, а в двух acceptance-тестах оставлена на `result.Output`;
- подтверждён адресный контур для `TEXTWIDTH` и `TEXTHEIGHT` без новых правок runtime-кода и без изменения пользовательского договора функций.

Изменённые файлы:
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~TextMetrics|FullyQualifiedName~CanvasOperationsReportDocumentedDiagnostics"` -> `4/4`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~TextWidthAcceptanceSample|FullyQualifiedName~TextHeightAcceptanceSample"` -> `2/2`
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Highlighting_VocabularyContainsCanvasWords|FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions"` -> `2/2`

Следующий точный шаг:
- брать следующий маленький helper поверх `CANVAS` для HUD и экранных подложек, не возвращаясь в старые неграфические хвосты.
