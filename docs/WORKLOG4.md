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
## Session 493 - 2026-04-28 12:43 - добавлены предопределённые константы PI, ROW и COLUMN

Что сделано:
- в связывание выражений добавлены предопределённые константы `PI`, `ROW` и `COLUMN`, которые доступны без объявления и подставляются как литеральные значения;
- `ROW` и `COLUMN` одновременно разрешены в местах, где требуется целочисленная константа времени связывания, поэтому ими можно пользоваться в фиксированных границах и длинах;
- присваивание в `PI`, `ROW` и `COLUMN` теперь диагностируется как присваивание обычной константе;
- переобъявление этих имён через `DIM`, `CONST`, `SUB` и `FUNCTION` запрещено;
- обновлены пользовательское руководство и индекс руководства;
- добавлен runnable пример `samples/operators/const/predefined_constants.axm`.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.PredefinedConstants.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.ScopeHelpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Declarations.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/manual/variables-and-constants.html`
- `docs/manual/scripts/manual.js`
- `samples/operators/const/predefined_constants.axm`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- ожидаются проверки `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`;
- ожидаются проверки `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`;
- ожидается полный `dotnet test Axiom.sln`.

Решение:
- именованная форма `ROW` и `COLUMN` добавлена поверх числовой, а не вместо неё;
- `PI` оформлена как такая же предопределённая константа, а не как отдельная функция или новое ключевое слово.

Следующий шаг:
- после завершения проверок вернуться к отдельному договору по многомерным функциям, меняющим форму массива, и использовать `ROW` и `COLUMN` в примерах этой ветки.
## Session 494 - 2026-04-28 12:58 - добавлены наглядные главы manual по 1D и 2D массивам

Что сделано:
- в manual добавлены отдельные главы `docs/manual/arrays-1d-visual.html` и `docs/manual/arrays-2d-visual.html` с простым визуальным объяснением одномерных и двумерных массивов;
- в `docs/manual/variables-and-constants.html` добавлено описание предопределённых констант `PI`, `ROW` и `COLUMN`;
- `docs/manual/scripts/manual.js` обновлён для новых глав и новых индексных записей по встроенным константам;
- после ввода `ROW` переименованы старые тестовые и smoke-идентификаторы `row` в `rowView`, чтобы они не конфликтовали со встроенной константой в регистронезависимом языке.

Изменённые файлы этого шага:
- `docs/manual/arrays-1d-visual.html`
- `docs/manual/arrays-2d-visual.html`
- `docs/manual/arrays.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/scripts/manual.js`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `samples/smoke/multidimensional-arrays/partial_indexing_view.axm`
- `samples/smoke/multidimensional-arrays/range_indexing_view.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj` проходит;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит;
- полный `dotnet test Axiom.sln` проходит;
- `git diff --check` не показывает ошибок кроме предупреждений Windows Git о будущей нормализации `LF` в `CRLF`.

Решение:
- наглядные объяснения вынесены в отдельные главы, а не смешаны с сухим справочным описанием;
- пользовательские примеры теперь избегают имён `row` и `column` как переменных, чтобы не сталкиваться с новыми предопределёнными константами.

Следующий шаг:
- вернуться к отдельному договору по многомерным функциям, меняющим форму массива, начиная с `ARRAYSLICE`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYCONCAT`.
## Session 495 - 2026-04-28 14:51 - зафиксирован договор для ARRAY-функций, меняющих форму массива

Что сделано:
- в `docs/SPECIFICATION3.md` добавлен отдельный проектный договор для `ARRAYSLICE`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYCONCAT` на многомерных массивах;
- зафиксировано простое правило: для ранга выше `1` эти функции работают только по явно выбранному измерению и не уплощают массив неявно;
- отдельно закреплено, что результат таких операций является новым независимым массивом, а не представлением без копирования;
- зафиксировано, что `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT` и `ARRAYREMOVE` пока не входят в этот срез и остаются одномерными;
- в `docs/PLANS3.md` обновлён следующий кодовый шаг под этот договор.

Изменённые файлы этого шага:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- код, сборка и тесты не запускались, потому что в этой сессии изменена только проектная документация.

Решение:
- для функций, меняющих форму массива, выбран явный договор по измерению вместо скрытого плоского обхода;
- многомерные копирующие функции отделены от операций, которые меняют размер массива на месте, чтобы следующий кодовый шаг оставался небольшим и однозначным.

Следующий шаг:
- реализовать в коде договор для `ARRAYSLICE`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYCONCAT`, затем добавить тесты, manual и runnable `.axm`-примеры.

## Session 496 - 2026-04-28 15:10 - реализованы многомерные ARRAYSLICE, ARRAYTAKE, ARRAYSKIP и ARRAYCONCAT

Что сделано:
- в семантике вызовов снято жёсткое одномерное ограничение для `ARRAYSLICE`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYCONCAT` и добавлено требование явного аргумента `dimension` для массива ранга выше `1`;
- в таблицу встроенных функций добавлены формы вызова с `dimension`, сохранив старые одномерные формы без изменения;
- интерпретатор и `BytecodeVm` расширены на многомерные копирующие преобразования формы: срез, взятие начальной части, пропуск начальной части и склейку по выбранному измерению;
- для `ARRAYCONCAT` зафиксирована проверка совместимости видимой формы по всем измерениям, кроме выбранного;
- добавлены автоматические проверки семантики, интерпретатора и `BytecodeVm`;
- обновлены `docs/manual/arrays.html` и `docs/manual/scripts/manual.js`;
- добавлены runnable `.axm`-примеры `samples/functions/arrayslice/by_column.axm`, `samples/functions/arraytake/by_row.axm`, `samples/functions/arrayskip/by_row.axm` и `samples/functions/arrayconcat/by_column.axm`;
- `docs/SPECIFICATION3.md` и `docs/PLANS3.md` синхронизированы с фактически реализованным поведением.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arrayslice/by_column.axm`
- `samples/functions/arraytake/by_row.axm`
- `samples/functions/arrayskip/by_row.axm`
- `samples/functions/arrayconcat/by_column.axm`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- проходят `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`;
- проходят `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`;
- проходит полный `dotnet test Axiom.sln`.

Решение:
- для многомерных функций изменения формы выбран явный договор по измерению вместо скрытого плоского обхода;
- операции этого шага оставлены чистыми: они создают новый массив и не меняют исходный.

Следующий шаг:
- отдельно решить судьбу `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT` и `ARRAYREMOVE` для многомерных массивов и только после этого идти дальше по отложенным группам.

## Session 497 - 2026-04-28 15:30 - динамические ARRAY-операции отложены до будущих коллекций

Что сделано:
- в `docs/SPECIFICATION3.md` добавлена отдельная пометка, что `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT` и `ARRAYREMOVE` не получают многомерный договор в ветке массивов;
- зафиксировано, что возможное развитие этих операций переносится в будущую ветку коллекций, где динамическое изменение состава данных будет частью естественной модели;
- в `docs/PLANS3.md` обновлён следующий шаг: ветка массивов продолжает только те `ARRAY...`-функции, которые логично расширяются на многомерную форму без превращения массива в динамическую структуру;
- ближайшим проектным кандидатом для следующего отдельного договора отмечены чистые копирующие операции вроде `ARRAYTOREVERSED`, если для них будет выбран ясный договор по измерению или видимому порядку обхода.

Изменённые файлы этого шага:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- код, сборка и тесты не запускались, потому что в этом шаге изменена только проектная документация.

Решение:
- не смешивать в одной ветке фиксированную модель многомерного массива и будущую динамическую модель коллекций;
- считать `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT` и `ARRAYREMOVE` частью будущего разговора о коллекциях, а не частью текущего расширения многомерных массивов.

Следующий шаг:
- выбрать следующую группу `ARRAY...`-функций, которая естественно переносится на многомерные массивы без динамического изменения размера, и сначала зафиксировать для неё отдельный договор.
## Session 498 - 2026-04-28 15:34 - многомерный ARRAYTOREVERSED

Что сделано:
- зафиксирован отдельный договор для `ARRAYTOREVERSED` в `docs/SPECIFICATION3.md`: для массива ранга выше `1` функция теперь работает только по явно выбранному измерению;
- в семантике вызовов добавлена форма `ARRAYTOREVERSED(array, dimension)` и требование `dimension` для многомерного массива;
- в интерпретаторе и `BytecodeVm` реализован разворот только вдоль выбранного измерения с сохранением остальных видимых границ;
- обновлены `docs/manual/arrays.html`, `docs/manual/scripts/manual.js` и добавлен runnable пример `samples/functions/arraytoreversed/by_column.axm`;
- расширены automated tests для binder, интерпретатора и `BytecodeVm`.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arraytoreversed/by_column.axm`
- `docs/WORKLOG4.md`

Проверка:
- проходит полный `dotnet test Axiom.sln`.

Решение:
- для многомерной формы выбран простой договор «разворот по измерению», а не скрытый разворот всего плоского обхода;
- `ARRAYDISTINCT` и другие спорные группы по-прежнему не смешиваются с этим шагом.

Следующий шаг:
- после проверки выбрать следующую чистую многомерную `ARRAY...`-функцию без динамического изменения размера массива;
- ближайший кандидат — отдельный договор для `ARRAYDISTINCT`, только если будет заранее зафиксирована форма его одномерного результата.

## Session 499 - 2026-04-28 15:51 - пример ARRAYTOREVERSED в manual дополнен двумерным случаем

Что сделано:
- в `docs/manual/arrays.html` прямо в разделе `ARRAYTOREVERSED` добавлен короткий двумерный пример с `ARRAYTOREVERSED(board, COLUMN)`;
- рядом с примером добавлено простое пояснение, что результатом является новая таблица с теми же границами, но с обратным порядком столбцов.

Изменённые файлы этого шага:
- `docs/manual/arrays.html`
- `docs/WORKLOG4.md`

Проверка:
- код и тесты не запускались, потому что в этом шаге менялась только пользовательская документация.

Решение:
- не ограничиваться ссылкой на runnable sample, если пользователь может ожидать увидеть двумерную форму прямо на странице справки.

Следующий шаг:
- продолжать следующую многомерную `ARRAY...`-функцию только вместе с таким же явным примером в manual, если у неё появляется новая двумерная или многомерная форма вызова.

## Session 500 - 2026-04-28 15:54 - правило manual уточнено для новых форм вызова

Что сделано:
- в `AGENTS.md` уточнено постоянное правило по user-facing документации;
- зафиксировано, что новая форма вызова, новый многомерный режим, новый договор параметров или другое заметное пользовательское расширение существующей функции должны отражаться прямо на её странице manual, а не только в `samples/`, тестах, спецификации или краткой ссылке;
- отдельно закреплено, что для такой новой формы нужно добавлять встроенный пример на той же странице, если без него читателю легко запутаться.

Изменённые файлы этого шага:
- `AGENTS.md`
- `docs/WORKLOG4.md`

Проверка:
- код и тесты не запускались, потому что в этом шаге менялись только репозиторные инструкции и журнал работ.

Решение:
- считать явный пример на странице manual частью обязательного follow-up для заметных новых форм вызова, а не необязательной полировкой.

Следующий шаг:
- применять это правило ко всем следующим расширениям существующих операторов и встроенных функций.

## Session 501 - 2026-04-28 15:56 - переменные-массивы объяснены в главе о переменных

Что сделано:
- в `docs/manual/variables-and-constants.html` добавлено прямое объяснение, что запись вида `DIM values() AS INTEGER` объявляет именно переменную-массив;
- отдельно зафиксировано, что невыделенный массив не является отдельным синтаксическим видом объявления, а представляет собой состояние той же массивной переменной до первого `REDIM` или после `ERASE`;
- в главу добавлено пояснение, что присваивание вроде `reversed = ARRAYTOREVERSED(source)` помещает новый массив в уже существующую массивную переменную, а не «делает её массивом» заново.

Изменённые файлы этого шага:
- `docs/manual/variables-and-constants.html`
- `docs/WORKLOG4.md`

Проверка:
- код и тесты не запускались, потому что в этом шаге менялась только пользовательская документация.

Решение:
- не оставлять модель массивной переменной только в главе про массивы, если она нужна для понимания обычного `DIM` в главе про переменные.

Следующий шаг:
- при следующих документационных проходах синхронно проверять связанные обзорные главы, чтобы важное базовое понятие не оставалось объяснённым только в одной узкой статье.

## Session 502 - 2026-04-28 15:56 - различие между массивом целиком и элементом уточнено в manual

Что сделано:
- в `docs/manual/variables-and-constants.html` добавлено прямое пояснение, что запись вида `values = 5` недопустима для `DIM values() AS INTEGER`, потому что слева массивный тип `INTEGER()`, а справа скаляр `INTEGER`;
- в `docs/manual/arrays.html` рядом с разделом «Массив как значение» добавлено явное различие между присваиванием массива целиком и присваиванием отдельного элемента по индексу.

Изменённые файлы этого шага:
- `docs/manual/variables-and-constants.html`
- `docs/manual/arrays.html`
- `docs/WORKLOG4.md`

Проверка:
- код и тесты не запускались, потому что в этом шаге менялась только пользовательская документация.

Решение:
- считать ошибку `values = 5` правильным проявлением текущего договора типов, а не странностью печати или особым поведением `PRINT`.

Следующий шаг:
- при следующих правках manual отдельно проверять места, где массив может пониматься и как тип переменной, и как текущее значение этой переменной.

## Session 503 - 2026-04-28 16:05 - диагностика присваивания между массивом и скаляром уточнена

Что сделано:
- в `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs` присваивание вынесено на отдельную проверку несовместимости типов, чтобы различать общий случай `MB2003` и специальный случай «массив против скаляра»;
- в `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs` добавлены отдельные сообщения для двух направлений ошибки: когда переменной-массиву присваивают скаляр и когда скалярной переменной присваивают массив;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` добавлены проверки на оба уточнённых текста диагностики;
- в `docs/manual/diagnostics.html` добавлено явное описание `MB2003`, включая случаи присваивания массива и скаляра.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `docs/manual/diagnostics.html`
- `docs/WORKLOG4.md`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- дополнительно выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_Assignment"`.

Решение:
- оставить код ошибки `MB2003` прежним, но делать текст более предметным в тех местах, где общий шаблон «ожидался X, получен Y» скрывает реальную пользовательскую ошибку.

Следующий шаг:
- при следующих проходах по диагностике так же адресно уточнять сообщения там, где абстрактная формулировка мешает понять модель языка без чтения спецификации.

## Session 504 - 2026-04-28 16:12 - текст диагностики присваивания упрощён и привязан к имени переменной

Что сделано:
- в `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs` сообщения для присваивания между массивом и скаляром переписаны в более прямой форме без канцелярских формул про левую и правую стороны;
- в `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs` диагностика теперь подставляет имя целевой переменной или массива, чтобы сообщение ссылалось на конкретное имя из программы;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` обновлены проверки текста под новый, более короткий договор сообщений.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `docs/WORKLOG4.md`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- дополнительно выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_Assignment"`.

Решение:
- считать для пользовательских диагностик более полезной прямую формулировку через роль переменной и допустимое действие, чем абстрактное описание направлений сравнения типов.

Следующий шаг:
- при следующих уточнениях диагностики сначала проверять, отвечает ли текст на вопрос «что именно я сделал не так и что нужно изменить в коде», а уже потом — показывает ли он внутреннюю модель типов.

## Session 505 - 2026-04-28 17:20 - многомерный ARRAYDISTINCT и упрощение терминологии в manual

Что сделано:
- в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`, `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs` и `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` зафиксирован новый договор `ARRAYDISTINCT`: функция принимает массив или представление любого ранга, но её тип результата всегда `T()`, где `T` — тип элементов исходного массива;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` реализация `ARRAYDISTINCT` переведена на общий видимый обход многомерного массива и теперь возвращает новый компактный одномерный список уникальных значений в порядке первых видимых вхождений;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs`, `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены проверки многомерного договора `ARRAYDISTINCT`;
- добавлен runnable-пример `samples/functions/arraydistinct/multidimensional_values.axm`;
- в `docs/SPECIFICATION3.md`, `docs/PLANS3.md`, `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/scripts/manual.js` обновлено описание `ARRAYDISTINCT` и добавлен двумерный пример;
- в `docs/manual/variables-and-constants.html` и связанных местах manual уменьшен упор на формулировку «переменная-массив» и усилено объяснение через имя массива, массивный тип и текущее массивное значение.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `samples/functions/arraydistinct/multidimensional_values.axm`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/variables-and-constants.html`
- `docs/WORKLOG4.md`

Проверка:
- выполнен полный `dotnet test Axiom.sln`.

Решение:
- считать `ARRAYDISTINCT` честной многомерной функцией без аргумента `dimension`, потому что её смысл не в изменении формы по выбранному измерению, а в построении линейного списка уникальных значений из всего видимого обхода;
- для объяснений пользователю чаще использовать формулировки через имя массива и массивное значение, если термин «переменная-массив» мешает понять модель.

Следующий шаг:
- выбрать следующую чистую `ARRAY...`-функцию, которая логично масштабируется на многомерный массив без превращения его в коллекцию, и заранее зафиксировать её форму результата в таком же отдельном договоре.

## Session 506 - 2026-04-28 17:29 - многомерный ARRAYREVERSE и обязательный 2D-пример в manual

Что сделано:
- в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs` и `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` расширен договор `ARRAYREVERSE`: для массива ранга выше `1` вызов теперь требует явный `dimension`;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`, `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` реализован разворот многомерного массива на месте вдоль выбранного измерения с сохранением остальных видимых границ;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs`, `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены проверки явного `dimension` и фактического двумерного разворота;
- добавлен runnable пример `samples/functions/arrayreverse/by_column.axm`;
- в `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/scripts/manual.js` обновлено пользовательское описание `ARRAYREVERSE`, а на самой странице manual рядом с прежним 1D-примером добавлен отдельный 2D-пример по столбцам;
- в `docs/SPECIFICATION3.md` и `docs/PLANS3.md` зафиксирован новый договор и следующий шаг.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `samples/functions/arrayreverse/by_column.axm`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`;
- полный `dotnet test Axiom.sln` дошёл до IDE-проекта и упёрся в блокировку файлов со стороны уже запущенного `Axiom.Ide.exe`, но все релевантные тестовые проекты по изменённым слоям прошли.

Решение:
- считать для многомерного `ARRAYREVERSE` тот же принцип, что и для многомерного `ARRAYTOREVERSED`: без скрытого уплощения, только по явно выбранному измерению, но в данном случае с изменением исходного массива на месте;
- считать обязательным редакторским правилом manual: если существующая функция получает 2D/3D-режим, на странице reference рядом со старым 1D-примером должен появляться отдельный расширенный пример, а не только изменение текста и sample-файла.

Следующий шаг:
- отдельно решить судьбу многомерного `ARRAYSORT` и `ARRAYTOSORTED`: либо закрепить для них явный договор по измерению, либо честно оставить сортировочную группу одномерной до отдельного решения.

## Session 507 - 2026-04-28 17:52 - ARRAYFLATTEN и ARRAYRESHAPE для многомерных массивов

Что сделано:
- в `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`, `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs` и `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs` доведены до рабочего договора новые встроенные функции `ARRAYFLATTEN` и `ARRAYRESHAPE`;
- в `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` binder расширен так, чтобы `ARRAYFLATTEN` принимал многомерный массив любого ранга и возвращал компактный `T()`, а `ARRAYRESHAPE(source, template)` принимал массивы разной формы, но одного типа элементов, и возвращал тип `template`;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`, `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` реализованы оба built-in: уплощение по видимому порядку обхода и обратная сборка по форме шаблона;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs`, `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены проверки новых функций, включая сценарий `ARRAYFLATTEN` → `ARRAYTOSORTED` → `ARRAYRESHAPE` и ошибку несовпадения числа элементов;
- добавлены runnable-примеры `samples/functions/arrayflatten/by_visible_order.axm` и `samples/functions/arrayreshape/from_sorted_flat.axm`;
- обновлены `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html`, `docs/manual/scripts/manual.js`, `docs/SPECIFICATION3.md` и `docs/PLANS3.md`: в manual добавлены отдельные 2D-примеры рядом с 1D, а также прямо записано, что `ARRAYSORT` и `ARRAYTOSORTED` пока остаются одномерными.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `samples/functions/arrayflatten/by_visible_order.axm`
- `samples/functions/arrayreshape/from_sorted_flat.axm`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнить `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- выполнить `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`.

Решение:
- считать `ARRAYFLATTEN` и `ARRAYRESHAPE` штатным проектным маршрутом для многомерных сценариев, где нужно временно перейти к линейному списку значений и потом вернуть форму;
- не расширять `ARRAYSORT` и `ARRAYTOSORTED` на многомерные массивы без отдельного явного договора о том, что именно в них считается сортируемым объектом.

Следующий шаг:
- либо выбрать следующую чистую функцию формы или обхода, которая естественно переносится на многомерные массивы;
- либо после стабилизации этой ветки перейти к проектированию будущих коллекций как отдельного направления языка.

## Session 508 - 2026-04-28 18:43 - первый слой семантической раскраски идентификаторов IDE

Что сделано:
- в `src/Axiom.Ide/Editor/Highlighting/` добавлен отдельный лёгкий слой семантической раскраски поверх существующей `xshd`-подсветки AvalonEdit;
- введены три категории пользовательских идентификаторов: переменная, константа и массив;
- `MainWindow` теперь подключает и снимает этот слой вместе с жизненным циклом `TextEditor`, а обновление после набора текста идёт через короткий debounce;
- в `src/Axiom.Ide/Resources/ShellColors.xaml` и `src/Axiom.Ide/Resources/ShellColors.Dark.xaml` добавлены отдельные кисти для светлой и тёмной темы;
- в `tests/Axiom.Ide.Tests/EditorFeatureTests.cs` и `tests/Axiom.Ide.Tests/ShellResourceTests.cs` добавлены проверки классификации и наличия новых ресурсов темы;
- в `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md` и `docs/PLANS3.md` зафиксирован новый IDE-договор.

Изменённые файлы этого шага:
- `src/Axiom.Ide/Editor/Highlighting/SemanticIdentifierKind.cs`
- `src/Axiom.Ide/Editor/Highlighting/SemanticIdentifierSpan.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticHighlightClassifier.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierColorizer.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierHighlighter.cs`
- `src/Axiom.Ide/MainWindow.SemanticHighlighting.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- нужно выполнить целевую сборку и `dotnet test` для `tests/Axiom.Ide.Tests`, чтобы подтвердить корректность нового слоя и ресурсов темы.

Решение:
- на этом шаге сознательно оставлена только цветовая дифференциация без курсива и без более мелких категорий, чтобы не перегрузить редактор и не раздувать первый семантический проход.

Следующий шаг:
- вручную оценить читаемость новых цветов в обеих темах и затем отдельно решить, нужен ли дополнительный визуальный признак вроде курсива для одной из категорий.

## Session 509 - 2026-04-28 18:51 - зафиксирована гарантия полноты статической подсветки built-in

Что сделано:
- после замечания о пропуске `ARRAYFLATTEN` и `ARRAYRESHAPE` в `.xshd` проверен активный комплект проектных документов на наличие явной гарантии полноты built-in подсветки IDE;
- в `docs/SPECIFICATION3.md` добавлено прямое правило, что статическая раскраска built-in является обязательной частью пользовательского договора IDE и должна оставаться синхронной с `BuiltinFunctions.All`;
- в `docs/ARCHITECTURE.md` зафиксирована архитектурная гарантия сопровождения двух `.xshd`-ресурсов и автоматической проверки через `AxiomHighlightingVocabulary`;
- в `docs/PLANS3.md` добавлено постоянное правило для следующих языковых шагов: новые built-in теперь должны сразу приносить и обновление статической IDE-подсветки.

Изменённые файлы этого шага:
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- код не менялся; дополнительный прогон тестов для этого документного шага не требовался.

Решение:
- считать полноту словаря built-in в `Axiom.xshd` и `Axiom.Dark.xshd` не факультативной доводкой, а обязательной частью сопровождения IDE.

Следующий шаг:
- оставить эту гарантию постоянной и дальше уже отдельно оценивать удобочитаемость новых цветов переменных, констант и массивов.

## Session 510 - 2026-04-28 19:03 - подсветка IDE переживает недописанный CONST и получила курсив

Что сделано:
- по журналу `ide-session-20260428-185134.log` подтверждено, что модальное падение вызывал новый семантический слой IDE при наборе одинокой строки `CONST`, а не обычное открытие готовых sample-файлов;
- в `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs` связывание `CONST` сделано устойчивым к отсутствующему имени: недописанное объявление больше не приводит к исключению из binder;
- в `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticHighlightClassifier.cs` добавлена мягкая деградация: при неуспешном полном связывании остаётся хотя бы синтаксическая раскраска объявлений и предопределённых констант;
- в `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierHighlighter.cs` и `src/Axiom.Ide/MainWindow.SemanticHighlighting.cs` добавлена защитная обработка исключений, чтобы сбой подсветки не мог показать пользователю модальное падение IDE;
- в `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierColorizer.cs` поверх цвета добавлен курсив для массивов и констант;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` и `tests/Axiom.Ide.Tests/EditorFeatureTests.cs` добавлены проверки на неполный `CONST`.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticHighlightClassifier.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierColorizer.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierHighlighter.cs`
- `src/Axiom.Ide/MainWindow.SemanticHighlighting.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`.

Решение:
- считать устойчивость IDE-подсветки к недописанному коду обязательной частью качества редактора, а не только побочным свойством binder;
- считать курсив допустимым вторым визуальным каналом для тех семантических категорий, которым уже назначен отдельный цвет.

Следующий шаг:
- вручную посмотреть новый вид переменных, массивов и констант в светлой и тёмной теме уже после включения курсива.

## Session 511 - 2026-04-28 19:21 - семантическая раскраска пользовательских имён удалена из IDE

Что сделано:
- по результатам ручной проверки светлой и тёмной темы принято решение убрать текущую семантическую раскраску переменных, констант и массивов;
- из `MainWindow` удалено подключение слоя `AxiomSemanticIdentifierHighlighter`;
- удалены экспериментальные классы раскраски пользовательских идентификаторов;
- из ресурсов светлой и тёмной темы убраны специальные кисти `SemanticVariableBrush`, `SemanticConstantBrush` и `SemanticArrayBrush`;
- из IDE-тестов убраны проверки отменённого слоя.

Изменённые файлы этого шага:
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Удалённые файлы этого шага:
- `src/Axiom.Ide/Editor/Highlighting/SemanticIdentifierKind.cs`
- `src/Axiom.Ide/Editor/Highlighting/SemanticIdentifierSpan.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticHighlightClassifier.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierColorizer.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomSemanticIdentifierHighlighter.cs`
- `src/Axiom.Ide/MainWindow.SemanticHighlighting.cs`

Проверка:
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`.

Решение:
- пользовательские переменные, константы и массивы больше не получают отдельную раскраску или курсив;
- текущий устойчивый слой IDE-подсветки остаётся статическим `.xshd`;
- исправление недописанного `CONST` в binder и гарантия полноты built-in подсветки остаются.

Следующий шаг:
- продолжить основную ветку работ по массивам и встроенным функциям.
## Session 512 - 2026-04-28 19:44 - координатная группа ARRAYINDEXOF / ARRAYLASTINDEXOF / ARRAYFINDALL для многомерных массивов

Изменения:
- `ARRAYINDEXOF` и `ARRAYLASTINDEXOF` переведены на многомерный координатный договор: для `1D` они по-прежнему возвращают скалярный `INTEGER`, а для массива ранга `2+` возвращают координаты найденного совпадения как `INTEGER()`;
- если в многомерном массиве совпадение отсутствует, обе функции теперь возвращают пустой выделенный массив `1 TO 0` вместо неявного плоского индекса;
- `ARRAYFINDALL` для `1D` сохранил старый результат `INTEGER()`, а для массива ранга `2+` теперь возвращает таблицу координат `INTEGER(,)`, где каждая строка описывает одно найденное совпадение;
- manual обновлён под новый пользовательский договор, а для ручной проверки добавлены отдельные `.axm`-примеры с координатным результатом.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arrayindexof/multidimensional_coordinates.axm`
- `samples/functions/arraylastindexof/multidimensional_coordinates.axm`
- `samples/functions/arrayfindall/multidimensional_coordinates.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`.

Решение:
- для координатной группы окончательно принят координатный результат вместо плоского индекса;
- для `ARRAYFINDALL` закреплена табличная форма результата `INTEGER(,)` для многомерного массива;
- порядок обхода остаётся стабильным видимым порядком массива и совпадает между интерпретатором, байткодной машиной, manual и примерами.

Следующий шаг:
- отдельно зафиксировать договор для сортировочной группы `ARRAYSORT`, `ARRAYTOSORTED`, `ARRAYBINARYSEARCH`;
- после этого переводить следующую безопасную многомерную подгруппу без скрытого уплощения.

## Session 513 - 2026-04-29 07:31 - зафиксирован договор сортировочной группы по измерению

Что сделано:
- перечитаны активные документы проекта `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- сверено, что после координатной группы массивов работа действительно остановилась перед сортировочной группой `ARRAYSORT`, `ARRAYTOSORTED`, `ARRAYBINARYSEARCH`;
- в `docs/SPECIFICATION3.md` добавлен отдельный многомерный договор сортировочной группы:
  - для массива ранга `2+` выбран явный аргумент `dimension`;
  - `ARRAYSORT` и `ARRAYTOSORTED` должны сортировать каждую видимую линию вдоль выбранного измерения отдельно;
  - `ARRAYBINARYSEARCH` должен искать по тем же линиям и возвращать координаты первого найденного совпадения как `INTEGER()`, а при отсутствии совпадения - пустой массив `1 TO 0`;
  - скрытое уплощение всего массива в этой группе по-прежнему запрещено, а для глобальной сортировки всей таблицы сохранена отдельная связка `ARRAYFLATTEN` -> `ARRAYTOSORTED` -> `ARRAYRESHAPE`;
- `docs/PLANS3.md` обновлён так, чтобы следующим шагом уже была реализация этого зафиксированного договора, а не повторное проектирование.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменён только проектный договор и рабочий план без изменений исполняемого кода.

Решение:
- считать для сортировочной группы принятой именно модель работы по выбранному измерению, а не через молчаливую сортировку всего многомерного массива как плоского списка;
- сохранить `ARRAYFLATTEN` -> `ARRAYTOSORTED` -> `ARRAYRESHAPE` как отдельный явный маршрут для сценария глобальной сортировки всех значений таблицы.

Следующий шаг:
- перевести `ARRAYSORT`, `ARRAYTOSORTED` и `ARRAYBINARYSEARCH` на этот договор в semantics, interpreter и `BytecodeVm`;
- после появления реального поведения обновить manual и добавить runnable `.axm`-примеры для новой многомерной семантики.
## Session 514 - 2026-04-29 07:48 - реализована многомерная сортировочная группа по измерению

Что сделано:
- переведены `ARRAYSORT`, `ARRAYTOSORTED` и `ARRAYBINARYSEARCH` на многомерный договор с явным аргументом `dimension` в дескрипторах built-in и в binder;
- для `ARRAYBINARYSEARCH` закреплён координатный результат `INTEGER()` для массива ранга `2+`, при этом одномерный режим со скалярным индексом сохранён без изменения;
- в interpreter и `BytecodeVm` добавлена общая модель работы по видимым линиям выбранного измерения:
  - `ARRAYSORT(array, dimension)` сортирует каждую линию на месте;
  - `ARRAYTOSORTED(array, dimension)` создаёт независимую копию той же формы и сортирует линии в ней;
  - `ARRAYBINARYSEARCH(array, value, dimension)` ищет по тем же линиям и возвращает координаты первого совпадения в виде `INTEGER()`, а при отсутствии совпадения - пустой массив `1 TO 0`;
- добавлены и обновлены автоматические тесты в `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests` и `Axiom.Execution.Tests`;
- обновлён пользовательский manual в `docs/manual/arrays.html` и индекс `docs/manual/scripts/manual.js`;
- добавлены runnable `.axm`-примеры:
  - `samples/functions/arraysort/multidimensional_by_dimension.axm`;
  - `samples/functions/arraytosorted/multidimensional_by_dimension.axm`;
  - `samples/functions/arraybinarysearch/multidimensional_coordinates.axm`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arraysort/multidimensional_by_dimension.axm`
- `samples/functions/arraytosorted/multidimensional_by_dimension.axm`
- `samples/functions/arraybinarysearch/multidimensional_coordinates.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`;
- выполнен `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalSortingGroup"`;
- полный `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` по-прежнему падает на двух старых acceptance-сценариях `samples/functions/arrayreshape/from_sorted_flat.axm` и `samples/functions/arrayflatten/by_visible_order.axm` с `MB4001`; эта проблема не появилась из текущей сортировочной правки.

Решение:
- считать сортировочную группу завершённой как отдельный многомерный срез без скрытого уплощения массива;
- считать `ROW` и `COLUMN` для двумерного случая пользовательским именованием того же явного `dimension`, уже согласованным между manual, тестами и рантаймом.

Следующий шаг:
- отдельно решить судьбу динамической группы `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` для многомерных массивов: либо явно оставить её одномерной, либо зафиксировать модель работы по строкам/столбцам до кодовой реализации;
- после этого продолжать следующую подгруппу только уже с заранее записанным договором, как и в сортировочной ветке.

## Session 515 - 2026-04-29 08:13 - восстановлен шаг назад для программ с массивами в bytecode-отладке

Что сделано:
- разобран свежий журнал IDE `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-080218.log` и подтверждено, что в сценарии с `samples/functions/arraysort/multidimensional_by_dimension.axm` шаг назад был недоступен с самой первой остановки;
- найдена причина в `BytecodeVm`: снимок обратимого состояния не умел копировать значения типа `ARRAY`, из-за чего при наличии массива в глобалах, локальных слотах или стеке обратимая история сразу обнулялась;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлено создание снимка для массивов через клон `RuntimeArrayValue`, поэтому bytecode-отладка больше не отключает `StepBack` только из-за `DIM ... (...) AS ...`;
- добавлены регрессионные тесты:
  - `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` для программы с массивом в состоянии виртуальной машины;
  - `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` для сценария запуска через сервис IDE;
- разобран новый журнал `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-080944.log` и подтверждено, что после правки `canStepBack=True` появляется уже на следующих остановках программы с массивом.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_StepBackRemainsAvailableWhenProgramStateContainsArray"`;
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~StepBack_WithArrayState_RemainsAvailable"`;
- одна попытка параллельного запуска этих `dotnet test` дала ожидаемую блокировку общего `obj`; это не дефект продукта, а нарушение репозиторного правила о последовательных MSBuild-проверках.

Решение:
- считать поломку «шаг назад отключается в отладке программ с массивами» исправленной;
- отдельно зафиксировать наблюдение, что `StepOver` и `StepBack` пока имеют разную гранулярность: шаг вперёд останавливается по смене исходной строки, а шаг назад восстанавливает сохранённые снимки отдельных байткод-команд внутри той же строки.

Следующий шаг:
- при отдельной работе над отладчиком решить, нужно ли выравнивать гранулярность `StepBack` под модель исходных строк, а не под каждую внутреннюю байткод-команду;
- по основной продуктовой ветке вернуться к договору динамической группы массивов `ARRAYPUSH` / `ARRAYPOP` / `ARRAYINSERT` / `ARRAYREMOVE`.

## Session 516 - 2026-04-29 08:18 - выровнена гранулярность шага назад с шагом вперёд

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` обратный шаг переведён с восстановления ближайшего сырого снимка на поиск предыдущей остановки по исходной строке;
- `StepBack` теперь пропускает внутренние байткод-команды той же строки и возвращается к предыдущей видимой пользовательской точке остановки, как это делает `StepOver` при движении вперёд;
- в `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs` свойство `CanStepBack` теперь учитывает не просто наличие любого предыдущего снимка, а наличие реальной предыдущей строковой остановки;
- обновлены и расширены тесты:
  - `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` теперь проверяет строковую симметрию повторного `StepBack` и отсутствие остановок на внутренних шагах выражения `PRINT values(1)`;
  - `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` закрепляет то же поведение на уровне сервиса IDE;
  - `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs` приведён к новой модели очистки вывода и перехода на предыдущую строку.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~StepBack"`;
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~StepBack"`.

Решение:
- считать для bytecode-отладки принятой строковую симметрию между `StepOver` и `StepBack`;
- не показывать пользователю при обратном ходе внутренние точки вроде `LoadGlobal`, `PushConst`, `LoadElement`, если шаг вперёд на тех же строках их не показывает.

Следующий шаг:
- при желании отдельно решить, нужен ли в будущем ещё один, более низкоуровневый режим отладки по внутренним байткод-командам;
- по основной продуктовой ветке вернуться к договору динамической группы массивов `ARRAYPUSH` / `ARRAYPOP` / `ARRAYINSERT` / `ARRAYREMOVE`.

## Session 517 - 2026-04-29 08:20 - восстановлен шаг назад через `ARRAYSORT` и другие обратимые built-in

Что сделано:
- разобран свежий журнал IDE `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-081921.log` и подтверждено, что после остановки на `ARRAYSORT` и первом `PRINT` кнопка шага назад гасла не из-за новой строковой модели, а из-за разрыва всей обратимой истории после built-in;
- найдено старое правило в `src/Axiom.Runtime/Execution/BytecodeVm.cs`: после любого built-in-вызова выполнялся `InvalidateReverseExecutionHistory()`, даже если built-in полностью обратим снимком состояния виртуальной машины;
- введено различие между обратимыми и необратимыми built-in:
  - обратимые built-in, такие как `ARRAYSORT` и другие чисто внутрипроцессные операции над значениями языка, больше не обнуляют историю;
  - необратимые built-in, связанные со случайностью, текущим временем и файловой системой, по-прежнему разрывают историю шага назад;
- добавлены регрессионные тесты на сценарий с `ARRAYSORT`, где отладчик должен уметь идти назад от `PRINT` к строке `ARRAYSORT`, а затем и к присваиваниям элементов массива.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~StepBack"`;
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~StepBack"`.

Решение:
- считать принятой модель, в которой `StepBack` не теряет историю на обратимых built-in вроде `ARRAYSORT`;
- сохранять разрыв истории только на genuinely необратимых операциях, где снимок VM не может честно восстановить внешний источник состояния.

Следующий шаг:
- при необходимости отдельно покрыть этой же логикой другие пользовательские сценарии вокруг built-in, которые меняют только память VM, но раньше случайно рвали историю;
- по основной продуктовой ветке вернуться к договору динамической группы массивов `ARRAYPUSH` / `ARRAYPOP` / `ARRAYINSERT` / `ARRAYREMOVE`.

## Session 518 - 2026-04-29 08:37 - исправлена порча массива после `StepBack` и нового продолжения через `ARRAYSORT`

Что сделано:
- по свежему журналу `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-082733.log` подтверждено, что строковая траектория `StepBack` уже правильная, но после возврата и нового продолжения пример `multidimensional_by_dimension.axm` печатал `0, 2, 6, 0, 1, 3` вместо ожидаемых `2, 4, 6, 1, 3, 5`;
- локализована причина в механизме снимков `BytecodeVm`: при сохранении и восстановлении снимка один и тот же массив, одновременно лежащий в локальной переменной и на вычислительном стеке, терял общую ссылочную идентичность и превращался в две независимые копии;
- из-за этого после `StepBack` вызов `ARRAYSORT` сортировал массив-аргумент на стеке, но не ту же самую структуру, которую потом читали `PRINT board(...)`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` снимки переведены на согласованное клонирование с сохранением общих ссылок внутри одного snapshot как при создании, так и при восстановлении;
- добавлены регрессионные тесты на сценарий `StepBack` -> `Continue` -> `ARRAYSORT` -> правильный вывод без зануления элементов.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_StepBackThenResumePreservesArraySortResults|FullyQualifiedName~Execute_StepBackAcrossArraySortReturnsToPreviousProgramLines|FullyQualifiedName~Execute_CompiledMultidimensionalSortingGroupBuiltinsSortAlongExplicitDimension"`;
- выполнен `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~StepBack_ThenContinue_PreservesArraySortResults|FullyQualifiedName~StepBack_AcrossArraySort_ReturnsToArraySortAndAssignments"`.

Решение:
- считать исправленной именно отладочную порчу состояния массива после `StepBack` и нового продолжения;
- считать, что для snapshot-логики `BytecodeVm` важно сохранять не только значения, но и общие ссылочные связи между локалами, глобалами, захватами и вычислительным стеком внутри одного снимка.

Следующий шаг:
- получить уже новый ручной прогон примера в IDE и при необходимости сверить следующий журнал, что теперь совпадают и траектория `StepBack`, и фактический вывод `ARRAYSORT`;
- затем вернуться к основной продуктовой ветке с договором динамической группы массивов `ARRAYPUSH` / `ARRAYPOP` / `ARRAYINSERT` / `ARRAYREMOVE`.

## Session 519 - 2026-04-29 08:48 - `ARRAYFILTER` переведён на многомерный договор с компактным результатом

Что сделано:
- в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs` договор `ARRAYFILTER` переведён с модели «тот же тип, что у входного массива» на явный результат `T()`, то есть на новый компактный одномерный массив элементов исходного типа;
- в `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` снят старый запрет ранга для `ARRAYFILTER`, поэтому binder теперь принимает многомерные массивы и срезы и выводит для них результат `INTEGER()`, `STRING()` и так далее по типу элементов;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` фильтрация переведена на обход видимых координатных наборов `ForEachArrayIndexSetAscending(...)`, а результат создаётся как новый компактный одномерный массив, а не как массив ранга исходного аргумента;
- добавлены и обновлены автоматические проверки:
  - `tests/Axiom.Semantics.Tests/BinderTests.cs` закрепляет, что `ARRAYFILTER(values(2 TO 3, 4 TO 6), IsOdd)` связывается как вызов `ARRAYFILTER` с результатом `INTEGER()`;
  - `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` проверяют один и тот же многомерный сценарий на интерпретаторе и в bytecode-исполнении;
- обновлены пользовательские материалы:
  - `docs/SPECIFICATION3.md` теперь явно фиксирует многомерный договор `ARRAYFILTER`;
  - `docs/manual/arrays.html` и `docs/manual/scripts/manual.js` отражают, что `ARRAYFILTER` принимает видимые элементы массива любого ранга и всегда возвращает компактный `T()`;
  - добавлен runnable acceptance-пример `samples/functions/arrayfilter/multidimensional_values.axm`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arrayfilter/multidimensional_values.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~ArrayFilter"`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayFilter|FullyQualifiedName~ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape"`.

Решение:
- считать `ARRAYFILTER` частью уже многомерной callback-группы массивов, с тем же порядком обхода видимых элементов, что и у `ARRAYEVERY`, `ARRAYSOME` и `ARRAYMAP`;
- считать нормой, что `ARRAYFILTER` не сохраняет исходный ранг и форму, а всегда собирает новый компактный одномерный результат `1 TO N` или пустой `1 TO 0`.

Следующий шаг:
- по основной ветке массивов больше не возвращаться автоматически к динамической группе `ARRAYPUSH` / `ARRAYPOP` / `ARRAYINSERT` / `ARRAYREMOVE`, потому что она уже отдельно отложена до будущей коллекционной ветки;
- следующим отдельным шагом по массивам проверить оставшиеся пользовательские пробелы в многомерной callback-группе, прежде всего acceptance- и manual-покрытие для `ARRAYEVERY` и `ARRAYSOME`, и после этого выбрать следующую реально незакрытую подгруппу массивов.

## Session 520 - 2026-04-29 08:58 - закрыты пользовательские многомерные примеры для `ARRAYEVERY` и `ARRAYSOME`

Что сделано:
- в `docs/manual/arrays.html` добавлены прямые многомерные inline-примеры для `ARRAYEVERY` и `ARRAYSOME`, чтобы пользователь видел не только текстовое упоминание любого ранга, но и точную runnable-форму со срезом;
- в профильные папки acceptance-примеров добавлены:
  - `samples/functions/arrayevery/multidimensional_slice.axm`
  - `samples/functions/arraysome/multidimensional_slice.axm`
- в обзорный список образцов на странице массивов добавлены ссылки на оба новых многомерных примера;
- поведение оставлено без изменений: это документный и acceptance-проход поверх уже существующей многомерной реализации callback-группы.

Изменённые файлы:
- `docs/manual/arrays.html`
- `samples/functions/arrayevery/multidimensional_slice.axm`
- `samples/functions/arraysome/multidimensional_slice.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape|FullyQualifiedName~ArrayEveryAndSome"`.

Решение:
- считать для `ARRAYEVERY` и `ARRAYSOME` обязательным не только текстовое описание многомерного договора, но и отдельный runnable-пример в собственной тематической подпапке;
- считать закрытым ближайший пользовательский пробел многомерной callback-группы именно на уровне manual и acceptance-материалов.

Следующий шаг:
- проверить, не осталось ли такого же пользовательского пробела у `ARRAYMAP` и `ARRAYREDUCE`, где многомерное поведение уже реализовано и тестируется, но отдельные профильные runnable-примеры могут быть слабее, чем у остальных функций группы;
- после этого выбрать следующую реально незакрытую подгруппу массивов по той же схеме.

## Session 521 - 2026-04-29 09:00 - закрыты пользовательские многомерные примеры для `ARRAYMAP` и `ARRAYREDUCE`

Что сделано:
- в `docs/manual/arrays.html` добавлены явные многомерные inline-примеры для `ARRAYMAP` и `ARRAYREDUCE`, чтобы на самой странице было видно сохранение формы у `ARRAYMAP` и стабильный порядок обхода у `ARRAYREDUCE`;
- в профильные подпапки acceptance-примеров добавлены:
  - `samples/functions/arraymap/multidimensional_slice.axm`
  - `samples/functions/arrayreduce/multidimensional_slice.axm`
- в обзорный список образцов на странице массивов добавлены ссылки на оба новых многомерных примера;
- поведение исполнения не менялось: это документный и acceptance-проход над уже существующей многомерной callback-группой.

Изменённые файлы:
- `docs/manual/arrays.html`
- `samples/functions/arraymap/multidimensional_slice.axm`
- `samples/functions/arrayreduce/multidimensional_slice.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayMap|FullyQualifiedName~ArrayReduce|FullyQualifiedName~ArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape"`.

Решение:
- считать для `ARRAYMAP` и `ARRAYREDUCE` обязательным тот же уровень пользовательской наглядности, что уже был доведён для `ARRAYEVERY`, `ARRAYSOME` и `ARRAYFILTER`;
- считать многомерную callback-группу массивов закрытой не только по коду, но и по профильным runnable-примерам в `samples/functions/`.

Следующий шаг:
- проверить, не остались ли в активной ветке массивов другие подгруппы, где многомерный договор уже реализован, но manual и acceptance-слой ещё показывают в основном одномерные сценарии;
- если такого пробела больше нет, выбрать следующую реально незакрытую подгруппу массивов уже не в callback-семействе.

## Session 522 - 2026-04-29 09:03 - закрыты пользовательские многомерные примеры для `ARRAYCONTAINS` и `ARRAYCOUNT`

Что сделано:
- в `docs/manual/arrays.html` добавлены прямые многомерные inline-примеры для `ARRAYCONTAINS` и `ARRAYCOUNT`, чтобы описание работы с видимыми элементами массива любого ранга не оставалось только в одном абзаце;
- в профильные подпапки acceptance-примеров добавлены:
  - `samples/functions/arraycontains/multidimensional_values.axm`
  - `samples/functions/arraycount/multidimensional_values.axm`
- в обзорный список образцов на странице массивов добавлены ссылки на оба новых многомерных примера;
- поведение исполнения не менялось: документный слой приведён к уже существующей многомерной реализации и её рантайм-тестам.

Изменённые файлы:
- `docs/manual/arrays.html`
- `samples/functions/arraycontains/multidimensional_values.axm`
- `samples/functions/arraycount/multidimensional_values.axm`

Проверка:
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayContainsAndCountScanMultidimensionalVisibleElements|FullyQualifiedName~CompiledArrayContainsAndCountScanMultidimensionalVisibleElements"`.

Решение:
- считать для многомерных проверочных функций `ARRAYCONTAINS` и `ARRAYCOUNT` обязательным тот же уровень пользовательской наглядности, что уже был добавлен для callback-группы и координатного поиска;
- считать этот пробел закрытым именно на уровне manual и runnable-примеров, без новой кодовой ветки.

Следующий шаг:
- если при следующем быстром обзоре больше не найдётся таких же документных пробелов, перейти от выравнивания manual к следующей реально незакрытой кодовой подгруппе массивов;
- отдельным кандидатом на такой переход остаётся решение, какая функциональная ветка после уже реализованных многомерных форм действительно требует нового кода, а не только дополнительной документации.
