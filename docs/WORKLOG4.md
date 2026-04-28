# Журнал работ 4

Этот файл является активным журналом работ проекта начиная с Session 481.

Исторические части журнала:
- `docs/WORKLOG.md` содержит записи до Session 168 включительно;
- `docs/WORKLOG2.md` содержит записи с Session 169 по Session 309 включительно;
- `docs/WORKLOG3.md` содержит записи с Session 310 по Session 480 включительно.

Правила ведения остаются прежними:
- новые записи добавляются только в конец файла;
- заголовок новой записи имеет вид `## Session <N> - YYYY-MM-DD HH:mm - <название>`;
- номер сеанса продолжает общую последовательность из `docs/WORKLOG.md`, `docs/WORKLOG2.md` и `docs/WORKLOG3.md`;
- дата и время указываются по местному времени проекта.

## Session 481 - 2026-04-27 20:43 - переход на SPECIFICATION3, PLANS3 и WORKLOG4

Что сделано:
- созданы новые активные документы `docs/SPECIFICATION3.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`;
- `docs/SPECIFICATION2.md`, `docs/PLANS2.md` и `docs/WORKLOG3.md` переведены в исторические части проектной документации;
- `AGENTS.md`, `README.md` и связанные рабочие документы переведены на новые активные ссылки;
- документный процесс проекта перенаправлен на чтение и обновление `docs/SPECIFICATION3.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`.

Изменённые файлы:
- `AGENTS.md`
- `README.md`
- `docs/SPECIFICATION.md`
- `docs/SPECIFICATION2.md`
- `docs/SPECIFICATION3.md`
- `docs/PLANS.md`
- `docs/PLANS2.md`
- `docs/PLANS3.md`
- `docs/WORKLOG2.md`
- `docs/WORKLOG3.md`
- `docs/WORKLOG4.md`
- дополнительные агентные и справочные документы, где старые активные ссылки были заменены на новые.

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменяется только структура проектной документации и правила продолжения работы.

Решение:
- не раздувать дальше `docs/SPECIFICATION2.md`, `docs/PLANS2.md` и `docs/WORKLOG3.md`;
- считать `docs/SPECIFICATION3.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` новыми активными документами проекта;
- использовать предыдущие части только как исторический контекст.

Следующий шаг:
- продолжить новый цикл по ветке массивов уже через `docs/SPECIFICATION3.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`;
- первым содержательным шагом подготовить в `docs/SPECIFICATION3.md` проектное решение для первой поставки многомерных массивов.

## Session 482 - 2026-04-27 20:48 - проектный договор первого среза многомерных массивов

Что сделано:
- перечитаны обязательные активные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- сверены текущая спецификация, справочник и кодовая база по массивам: реализована только одномерная модель, а справочник `docs/manual/arrays.html` остаётся справочником именно по одномерным массивам;
- в `docs/SPECIFICATION3.md` добавлено отдельное проектное решение ближайшего кодового среза по многомерным массивам;
- в договоре зафиксированы:
  - синтаксис многомерных `DIM`, `REDIM`, `REDIM PRESERVE`, индексирования и типов массива в параметрах/результатах;
  - фиксированный ранг массива, плотная прямоугольная модель хранения и сохранение ссылочной семантики массива как значения;
  - многомерные формы `LBOUND`/`UBOUND`, поведение `ARRAYLENGTH` и `ARRAYISALLOCATED`;
  - границы первого шага: без подмассивов, без broadcasting, без обобщения всех `ARRAY...` built-in и без многомерных массивных полей внутри `TYPE`.
- `docs/PLANS3.md` обновлён так, чтобы следующим шагом уже была реализация syntax/parser/semantics/runtime/bytecode-каркаса по новому договору.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменён только проектный договор и рабочий план без изменений исполняемого кода.

Решение:
- не вводить на первом шаге новые пользовательские имена поверх BASIC-линейки и опереться на уже существующие формы `DIM`, `REDIM`, `REDIM PRESERVE`, `LBOUND`, `UBOUND`;
- не смешивать многомерное ядро с подмассивами и прочими более сложными производными возможностями;
- в первом кодовом срезе держать многомерные массивные поля внутри `TYPE` вне объёма, чтобы не размывать реализацию.

Следующий шаг:
- начать реализацию многомерных массивов с синтаксического и типового слоя: списки диапазонов, многомерные типы массива и проверки ранга;
- затем довести тот же договор до runtime и `BytecodeVm`;
- после появления реального поведения добавить automated tests, runnable `.axm`-примеры и обновить пользовательский manual уже по факту реализованной семантики.

## Session 483 - 2026-04-28 09:56 - завершён первый исполняемый срез многомерных массивов

Что сделано:
- завершён первый исполняемый срез многомерных массивов по всему тракту исполнения: parser, semantics, lowering, bytecode-эмиссия, валидатор bytecode-модуля, интерпретатор, `AxiomExecutionPipeline` и `BytecodeVm`;
- многомерные `DIM`, динамические формы `DIM array(,) AS ...`, `REDIM`, `REDIM PRESERVE`, многомерное индексирование и формы `LBOUND(array, dimension)` / `UBOUND(array, dimension)` доведены до рабочего состояния;
- `ARRAYLENGTH` и `ARRAYISALLOCATED` согласованы с многомерной моделью, а `ARRAYCLONE` намеренно оставлен одномерным и снова жёстко проверяется на этом ограничении;
- обновлены пользовательский manual и индекс manual, чтобы новое поведение было видно не только из тестов и спецификации;
- добавлен runnable приёмочный пример `samples/smoke/multidimensional-arrays/two_dimensional_grid.axm`.

Изменённые файлы:
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/smoke/multidimensional-arrays/two_dimensional_grid.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter FullyQualifiedName~Axiom.Execution.Tests.ExecutionPipelineTests` проходит;
- полный `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` всё ещё падает на двух посторонних приёмочных примерах `renamefile` и `renamedir` с диагностикой `MB3007` про требование простого имени без пути; это не связано с многомерными массивами.

Решение:
- считать первый исполняемый срез многомерных массивов завершённым;
- сохранить модель плотного прямоугольного массива как базу для следующих шагов ветки;
- не расширять автоматически все `ARRAY...`-функции на многомерные массивы без отдельного проектного решения по каждому следующему срезу.

Следующий шаг:
- начать второй проектный срез ветки массивов: подмассивы, срезы и представления;
- отдельно зафиксировать, какие `ARRAY...`-функции расширяются на многомерные массивы следующими;
- после фиксации договора переходить к реализации следующего parser/semantics/runtime/bytecode-среза уже для подмассивов и срезов.

## Session 484 - 2026-04-28 10:04 - исправлены acceptance-примеры RENAMEFILE и RENAMEDIR

Что сделано:
- разобрана причина падения двух приёмочных примеров `samples/functions/renamefile/basic_usage.axm` и `samples/functions/renamedir/basic_usage.axm`;
- подтверждено, что проблема была не в отсутствии каталога `.artifacts`, а в самих примерах: второй аргумент в `RENAMEFILE` и `RENAMEDIR` передавался как путь, хотя текущий договор этих функций требует простое новое имя без пути;
- примеры исправлены так, чтобы передавать только листовое имя файла или папки;
- дополнительно просмотрен путь сохранения документов в IDE: `DocumentFileService` использует стандартный `SaveFileDialog` и затем `File.WriteAllText`, без собственного запрета на запись в диск `C:`.

Изменённые файлы:
- `samples/functions/renamefile/basic_usage.axm`
- `samples/functions/renamedir/basic_usage.axm`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "DisplayName~renamefile/basic_usage.axm"` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "DisplayName~renamedir/basic_usage.axm"` проходит;
- полный `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` теперь тоже проходит целиком.

Решение:
- считать падение `renamefile` и `renamedir` исправленным на уровне acceptance-примеров;
- считать сообщение о невозможности сохранить файл прямо в корень `C:\` системным поведением Windows, а не отдельным ограничением со стороны Axiom IDE.

Следующий шаг:
- продолжить уже запланированный следующий срез ветки массивов: подмассивы, срезы и представления;
- отдельно решить, нужно ли для UX IDE добавлять более явное объяснение, когда Windows запрещает запись в системные каталоги и корень диска.

## Session 485 - 2026-04-28 10:04 - разобран запрет сохранения в корень C:

Что сделано:
- просмотрен путь сохранения документов в IDE: `Axiom.Ide` использует стандартный `SaveFileDialog`, а затем прямую запись через `File.WriteAllText`, без собственного запрета на диск `C:`;
- подтверждено, что в `Axiom.Ide.csproj` нет отдельного `app.manifest` и нет требований `requireAdministrator` или другого специального уровня прав;
- проверены права на `C:\`: для группы `BUILTIN\Пользователи` на корне диска есть только чтение и выполнение, без обычного разрешения на запись;
- подтверждено, что текущий пользовательский токен работает в неповышенном режиме: членство в `BUILTIN\Администраторы` помечено как `deny only`, то есть процесс запущен без повышения прав.

Проверка:
- просмотрены `src/Axiom.Ide/Services/Documents/DocumentFileService.cs` и `src/Axiom.Ide/Axiom.Ide.csproj`;
- сняты ACL корня `C:\` и состав текущего токена через `whoami /groups`.

Решение:
- считать отказ записи прямо в `C:\` системным ограничением текущего неповышенного запуска, а не регрессией пользовательской логики сохранения в `Axiom.Ide`;
- учитывать, что после переноса проекта мог измениться способ запуска IDE: например, потеряться ярлык или настройка запуска с повышением, привязанная к прежнему пути конкретного `.exe`.

Следующий шаг:
- при необходимости отдельно улучшить UX IDE и добавить более ясное сообщение о том, что запись запрещена именно системным местом назначения, а не самой IDE;
- основную работу по языку продолжать по плану с веткой подмассивов и срезов.

## Session 486 - 2026-04-28 10:25 - зафиксирован второй договор по подмассивам и представлениям

Что сделано:
- перечитаны обязательные активные документы проекта и сверена точка остановки после завершения первого исполняемого среза многомерных массивов;
- дополнительно поднят исторический контекст по уже существующим одномерным `ARRAYSLICE`, `ARRAYTAKE` и `ARRAYSKIP`, чтобы второй шаг не изобретал новую терминологию поверх уже сложившейся линии;
- в `docs/SPECIFICATION3.md` зафиксирован второй проектный договор ветки массивов: подмассивы вводятся как представления без копирования через частичное индексирование многомерного массива;
- в договоре закреплено, что одномерный подмассив должен естественно работать с уже существующими одномерными `ARRAY...`-функциями, а `REDIM`, `REDIM PRESERVE` и `ERASE` для представлений во втором срезе запрещаются;
- `docs/PLANS3.md` обновлён так, чтобы следующий кодовый шаг был уже не про выбор направления, а про реализацию semantics/runtime/bytecode-модели подмассивов.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты в этой сессии не запускались, потому что менялся только проектный договор и рабочий план без изменений исполняемого кода;
- в качестве актуального базового состояния использовано то, что после исправления `renamefile` и `renamedir` полный `Axiom.Execution.Tests` уже снова проходит целиком.

Решение:
- вторым реализуемым шагом ветки массивов считать подмассивы как представления без копирования, а не новый диапазонный синтаксис;
- не обобщать сразу весь набор многомерных copy-срезов и не вводить новые пользовательские имена, пока не реализован базовый договор представлений.

Следующий шаг:
- начать кодовую реализацию частичного индексирования многомерных массивов и результата по оставшемуся рангу на уровне semantics;
- затем довести ту же модель представлений до interpreter, lowering, `AxiomExecutionPipeline` и `BytecodeVm`;
- после появления работающего поведения закрыть шаг тестами, runnable `.axm`-примером и обновлением manual.
## Session 487 - 2026-04-28 10:46 - реализован базовый второй срез подмассивов

Что сделано:
- перечитаны обязательные документы проекта и восстановлена точка остановки по ветке массивов: после фиксации второго договора нужно было довести частичное индексирование до исполняемого среза;
- в `Axiom.Semantics` частичное индексирование многомерного массива переведено из ошибки в отдельную форму bound-выражения с результатом по оставшемуся рангу, а запись в элемент по-прежнему оставлена только для полного набора индексов;
- `RuntimeArrayValue` расширен до модели подмассива-представления без копирования: представление хранит зафиксированный префикс индексов, сохраняет границы оставшихся измерений и разделяет то же хранилище элементов;
- interpreter, lowering, bytecode-валидатор и `BytecodeVm` приведены к одной и той же семантике: частичное индексирование возвращает представление, одномерные `ARRAY...`-функции работают с ним как с обычным одномерным массивом, а `REDIM`, `REDIM PRESERVE` и `ERASE` на представлении завершаются `MB3007`;
- добавлены тесты в `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests` и `Axiom.Execution.Tests`, покрывающие тип результата, совместное хранилище представления, работу через `BytecodeVm` и диагностические запреты на `REDIM`/`ERASE`;
- обновлён manual в `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/scripts/manual.js`;
- добавлен runnable smoke-пример `samples/smoke/multidimensional-arrays/partial_indexing_view.axm`.

Изменённые файлы:
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/smoke/multidimensional-arrays/partial_indexing_view.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` проходит.

Решение:
- считать базовый второй срез подмассивов закрытым в форме частичного индексирования без копирования и с сохранением границ;
- не расширять в этом же шаге язык до диапазонного синтаксиса внутри списка индексов и не обобщать автоматически все `ARRAY...`-функции на многомерные формы без отдельного договора.

Следующий шаг:
- зафиксировать третий договор ветки массивов для диапазонных срезов внутри индексного списка и для следующего расширения многомерных операций;
- после фиксации договора переходить к следующему parser/semantics/runtime/bytecode-срезу уже для диапазонных срезов.

## Session 488 - 2026-04-28 10:55 - зафиксирован третий договор диапазонных срезов массивов

Что сделано:
- перечитаны обязательные активные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- восстановлена точка остановки: базовый второй срез подмассивов уже реализован, а следующим шагом требовалось зафиксировать договор диапазонных срезов внутри индексного списка;
- в `docs/SPECIFICATION3.md` добавлен третий проектный договор ветки массивов: диапазоны `lower TO upper` внутри индексирования, сочетание фиксированных индексов и диапазонов, ранг результата, модель представления без копирования, диагностика и осознанно отложенные возможности;
- в `docs/PLANS3.md` добавлен текущий следующий кодовый шаг по parser/semantics/runtime/байткодному срезу диапазонных представлений.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что изменён только проектный договор и план без исполняемого кода.

Решение:
- использовать для диапазонных срезов уже существующее слово `TO`, а не вводить новое пользовательское имя;
- считать результат диапазонного индексирования представлением без копирования, которое разделяет хранилище с исходным массивом;
- не включать в этот срез `STEP`, открытые диапазоны, перестановку осей и обобщение всех копирующих `ARRAY...`-функций.

Следующий шаг:
- начать кодовую реализацию диапазонных срезов с расширения syntax/parser на элементы индексного списка вида одиночного выражения или `lower TO upper`;
- затем расширить семантику, интерпретатор, понижение, проверку байткодного модуля и `BytecodeVm`;
- после появления поведения добавить автоматические тесты, runnable `.axm`-пример и обновить manual.

## Session 489 - 2026-04-28 11:17 - реализован третий срез диапазонных срезов массивов

Что сделано:
- реализован синтаксис диапазона `lower TO upper` внутри индексного списка массива;
- добавлены `RangeExpressionSyntax` и связанный bound-узел для диапазонного индекса;
- семантика теперь возвращает подмассив нужного ранга для сочетания одиночных индексов, диапазонов и неуказанных хвостовых измерений;
- запрещено присваивание напрямую в диапазонный срез;
- `RuntimeArrayValue` переведён на модель представления с зафиксированными хранилищными индексами и видимыми измерениями, что покрывает и частичные, и диапазонные срезы;
- интерпретатор, понижение, эмиттер, проверка байт-кодного модуля и `BytecodeVm` поддерживают диапазонные представления без копирования;
- обновлён пользовательский manual и индекс manual;
- добавлен runnable smoke-пример `samples/smoke/multidimensional-arrays/range_indexing_view.axm`.

Изменённые файлы:
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Expressions.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/smoke/multidimensional-arrays/range_indexing_view.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build Axiom.sln` проходит;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj` проходит;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj` проходит;
- `dotnet test tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj` проходит;
- полный `dotnet test Axiom.sln` проходит.

Решение:
- диапазонный срез внутри индексного списка использует исторически ожидаемое слово `TO`, без нового пользовательского имени;
- результат диапазонного среза остаётся представлением без копирования и разделяет хранилище с исходным массивом;
- `STEP`, открытые диапазоны, перестановка осей и копирующие многомерные функции остаются за пределами текущего среза.

Следующий шаг:
- подготовить следующий договор по массивам: определить, какие `ARRAY...`-функции можно расширять на многомерные представления, а какие должны оставаться одномерными до отдельного договора о форме, ранге и порядке обхода.
## Session 490 - 2026-04-28 11:26 - зафиксировано развитие ARRAY-функций до многомерных массивов

Прочитаны обязательные проектные документы и принято пользовательское направление: в конечном состоянии все `ARRAY...`-функции, для которых это логично, должны быть развиты до работы с двумерными и более общо многомерными массивами. Одномерные ограничения для таких функций считаются временным этапом.

Изменения:
- `docs/SPECIFICATION3.md` - добавлен договор по группам `ARRAY...`-функций: первая безопасная группа, группа с договором о форме и измерении, группа с договором о координатах и порядке;
- `docs/PLANS3.md` - добавлен ближайший порядок реализации многомерной поддержки для `ARRAYCLONE`, `ARRAYFILL`, `ARRAYCONTAINS` и `ARRAYCOUNT`.

Проверки:
- автоматические проверки не запускались, потому что изменение только документирует проектное решение и не меняет исполняемый код.

Точное место остановки:
- следующим шагом нужно реализовать первую очередь многомерных `ARRAY...`-функций: `ARRAYCLONE`, `ARRAYFILL`, `ARRAYCONTAINS` и `ARRAYCOUNT`, с проверками интерпретатора, виртуальной машины байт-кода, руководством и ручным `.axm`-примером.
## Session 491 - 2026-04-28 11:40 - реализована первая группа многомерных ARRAY-функций

Что сделано:
- `ARRAYCLONE`, `ARRAYFILL`, `ARRAYCONTAINS` и `ARRAYCOUNT` расширены на массивы и представления любого ранга;
- добавлен общий обход видимых индексных наборов в `RuntimeArrayValue`, чтобы функции одинаково работали с корневыми массивами, частичными представлениями и диапазонными срезами;
- интерпретатор и `BytecodeVm` используют один и тот же видимый обход для заполнения, поиска, подсчёта и копирования;
- семантическая проверка разрешает многомерные массивы только для этой первой группы, а остальные `ARRAY...`-функции пока оставляет одномерными;
- обновлены пользовательское руководство и индекс руководства;
- добавлен runnable smoke-пример `samples/smoke/multidimensional-arrays/array_first_group.axm`.

Изменённые файлы этого шага:
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `samples/smoke/multidimensional-arrays/array_first_group.axm`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит;
- полный `dotnet test Axiom.sln` проходит.

Решение:
- первая многомерная группа ограничена функциями, которые читают или меняют все видимые элементы без неоднозначного изменения формы;
- функции с координатным результатом, сортировкой, изменением формы или созданием нового ранга остаются за пределами этого шага до отдельного договора.

Следующий шаг:
- реализовать многомерную поддержку для `ARRAYEVERY`, `ARRAYSOME`, `ARRAYMAP` и `ARRAYREDUCE`, сохранив для `ARRAYMAP` форму и границы исходного видимого массива или среза.
## Session 492 - 2026-04-28 12:01 - реализована вторая группа многомерных ARRAY-функций

Что сделано:
- `ARRAYEVERY`, `ARRAYSOME`, `ARRAYMAP` и `ARRAYREDUCE` расширены на массивы и представления любого ранга;
- семантическая проверка больше не требует одномерности для этой callback-группы;
- `ARRAYEVERY`, `ARRAYSOME` и `ARRAYREDUCE` переведены на общий обход видимых индексных наборов;
- `ARRAYMAP` теперь создаёт новый массив той же видимой формы, с теми же границами и тем же рангом, что и исходный массив или представление;
- в `RuntimeArrayValue` добавлено централизованное создание массива по видимой форме, включая пустые многомерные формы представлений;
- обновлены старые acceptance-примеры `samples/functions/arraymap/`, чтобы они совпадали с новым наблюдаемым поведением;
- обновлены пользовательское руководство, индекс руководства и добавлен runnable smoke-пример `samples/smoke/multidimensional-arrays/array_second_group.axm`.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `samples/functions/arraymap/basic_usage.axm`
- `samples/functions/arraymap/polymorphic_transform.axm`
- `samples/smoke/multidimensional-arrays/array_second_group.axm`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит;
- полный `dotnet test Axiom.sln` проходит.

Решение:
- callback-группа расширена только для функций, которые обходят все видимые элементы и не требуют отдельного координатного результата;
- `ARRAYMAP` сохраняет видимую форму и для пустых многомерных представлений, вместо неявной нормализации к компактному одномерному результату.

Следующий шаг:
- перейти к отдельному договору для функций, меняющих форму массива: `ARRAYSLICE`, `ARRAYTAKE`, `ARRAYSKIP`, `ARRAYCONCAT`, `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- отдельно зафиксировать, какие из них получают многомерную семантику по измерению, а какие пока остаются одномерными.
