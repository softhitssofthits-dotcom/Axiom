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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- в `tests/Axiom.Semantics.Tests/BinderTests.cs`, `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены проверки новых функций, включая сценарий `ARRAYFLATTEN` > `ARRAYTOSORTED` > `ARRAYRESHAPE` и ошибку несовпадения числа элементов;
- добавлены runnable-примеры `samples/functions/arrayflatten/by_visible_order.axm` и `samples/functions/arrayreshape/from_sorted_flat.axm`;
- обновлены `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html`, `docs/manual/scripts/manual.js`, `docs/SPECIFICATION3.md` и `docs/PLANS3.md`: в manual добавлены отдельные 2D-примеры рядом с 1D, а также прямо записано, что `ARRAYSORT` и `ARRAYTOSORTED` пока остаются одномерными.

Изменённые файлы этого шага:
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
  - tests/Axiom.Runtime.Tests/BytecodeVmTests.cs для программы с массивом в состоянии виртуальной машины;
  - `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` для сценария запуска через сервис IDE;
- разобран новый журнал `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-080944.log` и подтверждено, что после правки `canStepBack=True` появляется уже на следующих остановках программы с массивом.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
  - tests/Axiom.Runtime.Tests/BytecodeVmTests.cs теперь проверяет строковую симметрию повторного `StepBack` и отсутствие остановок на внутренних шагах выражения `PRINT values(1)`;
  - `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` закрепляет то же поведение на уровне сервиса IDE;
  - `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs` приведён к новой модели очистки вывода и перехода на предыдущую строку.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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
  - tests/Axiom.Semantics.Tests/BinderTests.cs закрепляет, что `ARRAYFILTER(values(2 TO 3, 4 TO 6), IsOdd)` связывается как вызов `ARRAYFILTER` с результатом `INTEGER()`;
  - tests/Axiom.Runtime.Tests/InterpreterTests.cs и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` проверяют один и тот же многомерный сценарий на интерпретаторе и в bytecode-исполнении;
- обновлены пользовательские материалы:
  - `docs/SPECIFICATION3.md` теперь явно фиксирует многомерный договор `ARRAYFILTER`;
  - `docs/manual/arrays.html` и `docs/manual/scripts/manual.js` отражают, что `ARRAYFILTER` принимает видимые элементы массива любого ранга и всегда возвращает компактный `T()`;
  - добавлен runnable acceptance-пример `samples/functions/arrayfilter/multidimensional_values.axm`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
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

## Session 523 - 2026-04-29 09:58 - пользовательские многомерные пробелы в основном закрыты, выбран следующий кодовый срез

Что сделано:
- перечитаны обязательные активные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- сверены текущие active-array договоры, manual и acceptance-слой после последних документных добивок по `ARRAYEVERY`, `ARRAYSOME`, `ARRAYMAP`, `ARRAYREDUCE`, `ARRAYCONTAINS` и `ARRAYCOUNT`;
- зафиксировано, что по активной ветке многомерных массивов основной пользовательский долг в основном закрыт: callback-группа, координатная группа, сортировочная группа и чистые копирующие операции уже доведены не только кодом и тестами, но и отдельными manual- и runnable-примерами;
- в `docs/PLANS3.md` следующий шаг переведён с документного обзора на кодовую подгруппу `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`, где для ранга `2+` ещё нет завершённого общего исполняемого договора.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменены только план и журнал работ без правок исполняемого кода.

Решение:
- считать текущие пользовательские многомерные пробелы по массивам в active-array ветке в основном закрытыми;
- не продолжать по инерции ещё один чисто manual-проход, пока не появится новая реальная пользовательская дыра;
- следующим кодовым кандидатом считать именно динамическую группу `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`, так как здесь ещё нужно принять и реализовать многомерный runtime-договор, а не просто дописать примеры.

Следующий шаг:
- коротко зафиксировать в `docs/SPECIFICATION3.md` договор многомерной динамической группы `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- затем перейти к кодовой реализации этого среза в semantics/runtime/`BytecodeVm`;
- после появления реального поведения обновить tests, `docs/manual/` и runnable `.axm`-примеры.

## Session 524 - 2026-04-29 10:02 - зафиксирован многомерный договор динамической группы массивов

Что сделано:
- в `docs/SPECIFICATION3.md` добавлен новый проектный договор для `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` по массивам ранга `2+`;
- прежняя отсрочка этой группы до отдельной коллекционной ветки явно заменена новым решением для active-array ветки;
- зафиксировано, что операции работают по явно выбранному измерению, не по одиночной ячейке, а по согласованному срезу:
  - `ARRAYPUSH` и `ARRAYINSERT` принимают `value` ранга `N - 1` с видимыми границами, согласованными со всеми невыбранными измерениями;
  - `ARRAYPOP` и `ARRAYREMOVE` возвращают новый независимый массив ранга `N - 1` и одновременно сужают исходный массив по выбранному измерению;
- отдельно закреплены ограничения без неявного broadcasting и без скрытого уплощения, чтобы следующий кодовый шаг имел один точный runtime-договор.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменялась только проектная документация, без правок исполняемого кода.

Решение:
- считать многомерный договор динамической группы массивов принятым и достаточным для перехода к реализации;
- не возвращаться к старой формулировке про обязательный перенос этой группы в коллекции, пока не появится отдельное проектное решение, отменяющее текущий договор.

Следующий шаг:
- реализовать многомерные `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` в binder, interpreter и `BytecodeVm`;
- затем добавить тесты на согласование формы `value`, на возвращаемый результат ранга `N - 1` и на ошибки `MB3005` / `MB3007`;
- после этого обновить `docs/manual/` и runnable `.axm`-примеры в профильных папках `samples/functions/`.

## Session 525 - 2026-04-29 11:07 - многомерная динамическая группа массивов доведена до кода, тестов и manual

Что сделано:
- в семантике расширены built-in-дескрипторы и binder для многомерных форм `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`:
  - `ARRAYPUSH` и `ARRAYINSERT` теперь принимают `value` ранга `N - 1` для массива ранга `2+`;
  - `ARRAYPOP` и `ARRAYREMOVE` теперь возвращают результат ранга `N - 1` вместо скаляра для массива ранга `2+`;
- в `Interpreter` и `BytecodeVm` реализована работа этой группы по явно выбранному измерению с проверкой формы `value`, с возвратом удалённого среза и с изменением границ только по выбранному измерению;
- добавлено текущее ограничение реализации: resize-операции этой группы пока работают только с уже выделенным корневым массивом и отклоняют подмассивы-представления;
- в `docs/manual/arrays.html` добавлены прямые описания и inline-примеры многомерных форм;
- добавлены runnable acceptance-примеры:
  - `samples/functions/arraypush/multidimensional_by_column.axm`
  - `samples/functions/arraypop/multidimensional_by_column.axm`
  - `samples/functions/arrayinsert/multidimensional_by_column.axm`
  - `samples/functions/arrayremove/multidimensional_by_column.axm`
- в `docs/SPECIFICATION3.md` договор уточнён под фактическую реализацию: отдельно зафиксировано текущее ограничение на подмассивы-представления.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/manual/arrays.html`
- `samples/functions/arraypush/multidimensional_by_column.axm`
- `samples/functions/arraypop/multidimensional_by_column.axm`
- `samples/functions/arrayinsert/multidimensional_by_column.axm`
- `samples/functions/arrayremove/multidimensional_by_column.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_BuiltinArrayPushAndPop|FullyQualifiedName~Bind_BuiltinArrayInsertAndRemove|FullyQualifiedName~Bind_MultidimensionalDynamicArrayBuiltinsUseSliceContract|FullyQualifiedName~Bind_MultidimensionalArrayPushRequiresSliceValueShape"`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_ArrayPushAppendsValueAndPreservesLowerBound|FullyQualifiedName~Execute_ArrayInsertShiftsTailAndPreservesLowerBound|FullyQualifiedName~Execute_ArrayRemoveReturnsRemovedElementAndShiftsTail|FullyQualifiedName~Execute_MultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension|FullyQualifiedName~Execute_MultidimensionalArrayInsertReportsIncompatibleValueShape|FullyQualifiedName~Execute_CompiledMultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension|FullyQualifiedName~Execute_CompiledMultidimensionalArrayInsertReportsIncompatibleValueShape"`;
- выполнен `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayPush|FullyQualifiedName~ArrayPop|FullyQualifiedName~ArrayInsert|FullyQualifiedName~ArrayRemove"`;
- выполнен `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins|FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalDynamicArrayBuiltins|FullyQualifiedName~Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError"`.

Решение:
- считать многомерную динамическую группу `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` доведённой до уровня «код + тесты + manual + runnable acceptance»;
- считать текущее ограничение на подмассивы-представления осознанной границей этой реализации, а не случайным пробелом документации.

Следующий шаг:
- отдельно решить, должно ли ограничение на подмассивы-представления остаться постоянным правилом для resize-операций массива;
- если нужна поддержка представлений, сначала спроектировать общее хранилище массива и aliasing для корня и представлений;
- если такая поддержка не нужна, переходить к следующей реально незакрытой кодовой подгруппе массивов.

## Session 526 - 2026-04-29 11:23 - ограничение на подмассивы-представления закреплено как постоянное правило

Что сделано:
- по явному решению пользователя в `docs/SPECIFICATION3.md` зафиксировано, что ограничение на подмассивы-представления для `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` остаётся постоянным правилом текущей модели массивов;
- в `docs/PLANS3.md` закрыт открытый вопрос о том, нужна ли отдельная архитектурная ветка для resize-операций через представления именно ради этой динамической группы;
- следующая работа по массивам возвращена к обычному выбору новой реально незакрытой кодовой подгруппы, без обязательного возврата к теме представлений.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменялись только проектные документы без новых правок исполняемого кода.

Решение:
- считать запрет resize-операций через подмассивы-представления постоянной границей текущего договора массивов;
- не планировать для этой темы отдельную реализацию, пока пользователь или проектная документация явно не откроют новую языковую возможность поверх представлений.

Следующий шаг:
- выбрать следующую реально незакрытую кодовую подгруппу array-ветки вне уже завершённой динамической четвёрки;
- если такая подгруппа не очевидна сразу, сначала сделать короткий обзор активных ARRAY-функций и зафиксировать следующий кодовый срез в `docs/PLANS3.md`.

## Session 527 - 2026-04-29 11:27 - просканирована active-array ветка после закрытия динамической группы

Что сделано:
- выполнен короткий обзор `docs/SPECIFICATION3.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/manual/arrays.html`, списка array-built-in в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs` и тематических подпапок `samples/functions/`;
- сверено, какие многомерные договоры уже доведены до уровня «code + tests + manual + runnable sample», а какие темы по массивам всё ещё остаются только как отдельные будущие возможности;
- в `docs/PLANS3.md` зафиксирован вывод обзора и выбран следующий честный кандидат на новый кодовый срез.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии был только обзор документации и текущего каталога built-in без изменения исполняемого кода.

Решение:
- считать active-array ветку по текущему набору пользовательски видимых `ARRAY...`-функций в основном закрытой;
- не считать текущим долгом темы, которые уже явно вынесены в следующий уровень возможностей: специальные built-in для числа измерений и полной формы массива, диапазоны со `STEP`, перестановку осей и другие усиленные формы представлений;
- считать ближайшим честным новым кодовым срезом metadata-подгруппу массива, а не ещё одну недостающую правку в уже существующем наборе array-built-in.

Следующий шаг:
- если продолжать именно массивную ветку, сначала коротко зафиксировать в `docs/SPECIFICATION3.md` договор metadata-built-in для числа измерений и полной формы массива;
- после этого переходить к реализации в semantics/runtime/`BytecodeVm` и к обычному комплекту tests/manual/`samples/functions/`.

## Session 528 - 2026-04-29 11:35 - структурная переработка справочника и добавление статического поиска

Что сделано:
- выполнена структурная переработка справочника `docs/manual/` без изменения пользовательской семантики языка;
- раздел «Массивы» разложен на тематические страницы:
  - `docs/manual/arrays.html` (обзор),
  - `docs/manual/arrays-1d.html`,
  - `docs/manual/arrays-2d.html`,
  - `docs/manual/arrays-multidimensional.html`,
  - `docs/manual/arrays-dynamic.html`,
  - `docs/manual/arrays-indexing.html`,
  - `docs/manual/arrays-slices.html`,
  - `docs/manual/arrays-functions.html`,
  - `docs/manual/arrays-diagnostics.html`;
- добавлен отдельный статический живой поиск по справочнику:
  - `docs/manual/search.html`,
  - `docs/manual/scripts/search-index.js`,
  - `docs/manual/scripts/search.js`;
- `docs/manual/scripts/manual.js` переработан и теперь отвечает только за меню, подсветку текущей страницы, пейджер и мобильное открытие/закрытие навигации;
- обновлены `docs/manual/index.html` и `docs/manual/reference.html` под новую структуру и быстрый вход в поиск;
- в `docs/manual/index.html` исправлена нормативная ссылка на активную спецификацию: `docs/SPECIFICATION3.md`.

Измененные файлы:
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/manual/scripts/search.js`
- `docs/manual/styles/manual.css`
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/manual/search.html`
- `docs/manual/arrays.html`
- `docs/manual/arrays-1d.html`
- `docs/manual/arrays-2d.html`
- `docs/manual/arrays-multidimensional.html`
- `docs/manual/arrays-dynamic.html`
- `docs/manual/arrays-indexing.html`
- `docs/manual/arrays-slices.html`
- `docs/manual/arrays-functions.html`
- `docs/manual/arrays-diagnostics.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена проверка ссылок внутри `docs/manual/*.html` (включая новые страницы и ссылки на `samples/`): отсутствующие пути не обнаружены.

Решение:
- закрепить иерархическую схему справочника как базовую: «обзор раздела -> подстраницы -> единая навигация -> отдельный поиск».

Следующий шаг:
- продолжить такую же структурную декомпозицию для других перегруженных разделов справочника (в первую очередь `data-types` и `numeric-functions`) с сохранением текущей семантики языка.

## Session 529 - 2026-04-29 11:51 - унификация терминологии SUB на «процедура» во всём manual

Что сделано:
- в `docs/manual/` завершена терминологическая унификация: для `SUB` в заголовках, ссылках и описаниях используется «процедура»;
- удалены остаточные формы слова «подпрограмма» в разных падежах из пользовательского текста справочника.

Измененные файлы:
- `docs/manual/sub.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/type.html`
- `docs/manual/cheat-sheet.html`
- `docs/WORKLOG4.md`

Проверка:
- выполнен поиск по `docs/manual/` на вхождения `подпрограмм*`: совпадений не найдено.

Решение:
- закрепить термин «процедура» как основной для `SUB` в пользовательском справочнике.

Следующий шаг:
- продолжать структурную переработку остальных крупных разделов справочника по утверждённой схеме «обзор + подстраницы + поиск».
## Session 530 - 2026-04-29 12:07 - metadata-built-in массивов доведены до полного продукта

Что сделано:
- в `docs/SPECIFICATION3.md` зафиксирован договор новых built-in `ARRAYRANK` и `ARRAYBOUNDS` для числа измерений и полной видимой формы массива;
- реализация добавлена в semantics, interpreter и `BytecodeVm`, а затем доведена до полного bytecode-пути через обновление `AxiomExecutionPipeline`;
- manual обновлён по новой разложенной структуре раздела массивов: описания и примеры внесены в `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`, `docs/manual/arrays-diagnostics.html`, а записи поиска обновлены в `docs/manual/scripts/search-index.js`;
- в IDE-подсветку добавлены `ARRAYRANK` и `ARRAYBOUNDS`;
- добавлены runnable-примеры:
  - `samples/functions/arrayrank/basic_usage.axm`
  - `samples/functions/arraybounds/multidimensional_shape.axm`

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `docs/manual/arrays-functions.html`
- `docs/manual/arrays-multidimensional.html`
- `docs/manual/arrays-diagnostics.html`
- `docs/manual/scripts/search-index.js`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `samples/functions/arrayrank/basic_usage.axm`
- `samples/functions/arraybounds/multidimensional_shape.axm`

Проверка:
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_BuiltinArrayInformationFunctions|FullyQualifiedName~Bind_BuiltinArrayMetadataFunctions"` — пройдено;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds|FullyQualifiedName~Execute_ArrayBoundsReportsUnallocatedArrayState|FullyQualifiedName~Execute_CompiledArrayMetadataBuiltinsReportRankAndVisibleBounds|FullyQualifiedName~Execute_CompiledArrayBoundsReportsUnallocatedArrayState"` — пройдено после уточнения проверки текста диагностики;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForArrayMetadataBuiltins"` — пройдено;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions"` — пройдено.

Решение:
- считать metadata-подгруппу массивов закрытой полностью: код, тесты, manual, поиск, IDE-подсветка и runnable acceptance-примеры согласованы;
- вносить новые описания в manual по тематическим подстраницам переработанной структуры, а не возвращать их в старые перегруженные страницы.

Следующий шаг:
- если продолжать именно массивную ветку, брать уже новый кодовый срез — расширенное диапазонное индексирование со `STEP` для представлений и срезов;
- если продолжать общую работу по справочнику, переносить ту же структурную схему на другие крупные разделы manual.
## Session 531 - 2026-04-29 15:53 - декомпозиция manual для data-types и numeric-functions

Что сделано:
- перечитаны обязательные активные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- завершена тематическая декомпозиция `docs/manual/data-types.html` на обзор и подстраницы `data-types-scalars.html`, `data-types-strings-and-files.html`, `data-types-records-and-arrays.html`, `data-types-conversions.html`;
- завершена тематическая декомпозиция `docs/manual/numeric-functions.html` на обзор и подстраницы `numeric-functions-conversion-and-legacy.html`, `numeric-functions-core.html`, `numeric-functions-trigonometry.html`, `numeric-functions-roots-rounding.html`;
- `docs/manual/scripts/manual.js`, `docs/manual/reference.html` и `docs/manual/scripts/search-index.js` обновлены так, чтобы новая структура была видна в меню, быстрых переходах и статическом поиске;
- семантика языка не менялась: правки касались только формы подачи и навигации user-facing manual.

Изменённые файлы:
- `docs/manual/data-types.html`
- `docs/manual/data-types-scalars.html`
- `docs/manual/data-types-strings-and-files.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/manual/data-types-conversions.html`
- `docs/manual/numeric-functions.html`
- `docs/manual/numeric-functions-conversion-and-legacy.html`
- `docs/manual/numeric-functions-core.html`
- `docs/manual/numeric-functions-trigonometry.html`
- `docs/manual/numeric-functions-roots-rounding.html`
- `docs/manual/reference.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена полная проверка относительных `href` внутри `docs/manual/*.html`: отсутствующих путей не обнаружено (`OK`);
- сборка и `dotnet`-тесты не запускались, потому что в этой сессии менялся только user-facing manual и навигация по нему.

Решение:
- закреплять для крупных справочных глав единую схему «обзор -> подстраницы -> навигация -> поиск»;
- числовые функции и типы данных чисто структурно считать закрытыми — дальше их не нужно делить ещё раз без новой продуктовой причины.

Следующий шаг:
- продолжить структурную нормализацию следующей крупной страницы manual, где она ещё не переведена на эту хему;
- первым практическим кандидатом честно считать `docs/manual/file-system-functions.html`, как одну из самых крупных ещё неразложенных глав.
## Session 532 - 2026-04-29 16:09 - файловый раздел manual приведён к логической структуре и поиску

Что сделано:
- переработан раздел `docs/manual/file-system-functions.html`: вместо одной перегруженной страницы сделан обзор раздела и отдельные тематические подстраницы `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions-open-files.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions-management.html`;
- на подстраницах файловые функции разложены по смыслу: проверка существования и целые текстовые файлы, открытые `FILE`, пути и сведения, изменяющие операции и временные объекты;
- для каждой основной файловой функции добавлена однотипная подача «форма вызова -> что делает -> короткий пример», а диагностические сведения вынесены в отдельные завершающие блоки страниц;
- обновлены `docs/manual/scripts/manual.js` и `docs/manual/reference.html`, чтобы новые подстраницы были доступны через обычную навигацию;
- расширен `docs/manual/scripts/search-index.js`: добавлены отдельные записи для файловых функций и операторов `OPEN` / `CLOSE`, чтобы поиск находил конкретные имена вроде `SEEKFILE`, `READLINE`, `WRITE`, `MAKETEMPDIR`.

Изменённые файлы:
- `docs/manual/file-system-functions.html`
- `docs/manual/file-system-functions-text.html`
- `docs/manual/file-system-functions-open-files.html`
- `docs/manual/file-system-functions-paths.html`
- `docs/manual/file-system-functions-management.html`
- `docs/manual/reference.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/manual/arithmetic-and-logic.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных `href` во всех `docs/manual/*.html` с учётом якорей; отсутствующих целевых файлов не найдено (`OK`);
- отдельно подтверждено, что `docs/manual/scripts/search-index.js` содержит записи для `SEEKFILE`, `READLINE`, `OPEN`, `CLOSE` и других файловых сущностей;
- `dotnet`-сборка и тесты не запускались, потому что в этой сессии менялся только пользовательский справочник и навигационный индекс manual.

Решение:
- считать обязательным для крупных справочных страниц принцип логической группировки по теме: связанные операции должны жить вместе, а несвязанные не должны смешиваться в одной общей простыне;
- считать обязательной пользовательскую подачу для функций и операторов в manual: краткое назначение, форма вызова, короткий пример, а диагностические замечания отдельно в конце.

Следующий шаг:
- продолжить тот же редакторский проход по другим крупным страницам manual, где структура ещё не доведена до этого стандарта, начиная с `docs/manual/date-time-functions.html`.
## Session 533 - 2026-04-29 16:15 - из обзорных страниц manual убрана служебная редакторская подача

Что сделано:
- после пользовательского замечания переписаны обзорные страницы `docs/manual/file-system-functions.html`, `docs/manual/arrays.html`, `docs/manual/data-types.html`, `docs/manual/numeric-functions.html`;
- из текстов убраны редакторские формулировки вроде «обзор раздела», «карта тем», «эта страница задает карту раздела» и другие фразы про устройство самого справочника;
- заголовки и вводные тексты заменены на нормальные пользовательские формулировки по предмету: короткие названия страниц, обычные разделы `Разделы`, `Примеры`, `Общие правила`, без рассказа читателю о том, как документация была разложена.

Изменённые файлы:
- `docs/manual/file-system-functions.html`
- `docs/manual/arrays.html`
- `docs/manual/data-types.html`
- `docs/manual/numeric-functions.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- повторно выполнена локальная проверка относительных `href` во всех `docs/manual/*.html`; ошибок не найдено (`OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этой правке менялся только пользовательский текст manual.

Решение:
- считать недопустимыми в manual формулировки, которые описывают структуру или редакторскую организацию справочника вместо конструкций языка;
- для обзорных страниц использовать предметные названия и пользовательские разделы, а не служебную мета-лексику.

Следующий шаг:
- продолжать переработку крупных страниц manual уже с учётом двух правил сразу: логическая группировка материала и отсутствие редакторской мета-подачи.
## Session 534 - 2026-04-29 16:31 - массивный блок manual начал перевод на Microsoft-подобный справочный жанр

Что сделано:
- после замечаний о стиле и подаче за основу взят не один образец, а несколько близких страниц Microsoft/VBA по жанрам: материалы по массивам и граничным функциям (`LBound`, `UBound`, `ReDim`, `Erase`), а также общая подача операторов и функций;
- переписаны страницы `docs/manual/arrays.html`, `docs/manual/arrays-1d.html`, `docs/manual/arrays-2d.html`, `docs/manual/arrays-dynamic.html`, `docs/manual/arrays-indexing.html`, `docs/manual/arrays-multidimensional.html`;
- вместо телеграфной подачи добавлены полноценные разделы вроде `Сводка`, `Синтаксис`, `Части записи`, `Замечания`, `Пример`, `Ручная проверка`, `См. также`, чтобы страницы объясняли конструкцию нормальным справочным языком.

Изменённые файлы:
- `docs/manual/arrays.html`
- `docs/manual/arrays-1d.html`
- `docs/manual/arrays-2d.html`
- `docs/manual/arrays-dynamic.html`
- `docs/manual/arrays-indexing.html`
- `docs/manual/arrays-multidimensional.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных `href` для обновлённых страниц массива; ошибок не найдено (`OK`);
- `dotnet`-сборка и тесты не запускались, потому что менялся только текст manual.

Решение:
- больше не использовать для manual обрывочную подачу вида `Назначение -> одна строка`, `Правила -> три коротких пункта` там, где читателю нужно полноценное объяснение;
- при дальнейшем проходе по manual сверять стиль каждой страницы с ближайшим по жанру эталоном Microsoft/VBA, а не с одной случайной страницей.

Следующий шаг:
- завершить переработку остальных страниц массива в том же жанре, затем продолжить такой же проход по следующим разделам справочника.
## Session 535 - 2026-04-29 16:47 - массивный блок manual доведён до цельной справочной подачи

Что сделано:
- переписаны страницы `docs/manual/arrays-slices.html`, `docs/manual/arrays-functions.html`, `docs/manual/arrays-diagnostics.html`, `docs/manual/arrays-1d-visual.html`, `docs/manual/arrays-2d-visual.html`;
- для срезов и представлений убрана заготовочная подача из пары фраз, вместо неё добавлены нормальные разделы `Сводка`, `Синтаксис`, `Части записи`, `Замечания`, `Пример`, `Ручная проверка`, `См. также`;
- страница `docs/manual/arrays-functions.html` переведена из режима простого списка сигнатур в режим отдельных справочных карточек по функциям: `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`, `ARRAYRANK`, `ARRAYBOUNDS`, `ARRAYSLICE`, `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- на страницах `arrays-1d-visual` и `arrays-2d-visual` упрощён и очеловечен язык: убраны сухие и неудачные формулировки, оставлены только пояснения по самой конструкции массива.

Изменённые файлы:
- `docs/manual/arrays-slices.html`
- `docs/manual/arrays-functions.html`
- `docs/manual/arrays-diagnostics.html`
- `docs/manual/arrays-1d-visual.html`
- `docs/manual/arrays-2d-visual.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- повторно выполнена локальная проверка относительных ссылок для обновлённых страниц блока массивов; битых переходов не найдено;
- `dotnet`-сборка и тесты не запускались, потому что в этой сессии менялся только текст manual.

Решение:
- считать обязательным для справочных страниц по функциям правило «одна функция — отдельное человеческое объяснение с примером», а не общий список сигнатур с пояснением ниже;
- считать визуальные страницы допустимыми только как поясняющее дополнение к основной справке, а не как место для обрывочных технических формулировок.

Следующий шаг:
- перейти к следующему крупному разделу manual, начиная с `docs/manual/date-time-functions.html`, и проводить ту же жанровую нормализацию по нескольким близким страницам Microsoft/VBA.
## Session 536 - 2026-04-29 16:56 - на странице срезов разведены синтаксис диапазона и функция ARRAYSLICE

Что сделано:
- после пользовательской проверки исправлена страница `docs/manual/arrays-slices.html`, где были смешаны две разные темы: срез как форма записи `array(2 TO 4)` и функция `ARRAYSLICE`;
- в разделе `Сводка` прямо сказано, что эта глава описывает не функцию `ARRAYSLICE`, а запись с диапазоном индексов;
- в `Синтаксис` и `Части записи` заменён расплывчатый пример с `view` на нейтральный результат среза;
- добавлен отдельный короткий раздел `Функция ARRAYSLICE`, который явно отправляет читателя к странице `docs/manual/arrays-functions.html`;
- из `Ручная проверка` на странице срезов убраны примеры самой функции `ARRAYSLICE`, чтобы на этой странице остались только примеры представлений и диапазонного индексирования.

Изменённые файлы:
- `docs/manual/arrays-slices.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных ссылок для `docs/manual/arrays-slices.html`; битых переходов не найдено (`OK`);
- `dotnet`-сборка и тесты не запускались, потому что менялся только текст manual.

Решение:
- считать ошибкой manual смешение синтаксической формы языка и отдельной встроенной функции без явного разведения ролей;
- для похожих случаев дальше сначала называть саму сущность страницы, а уже потом давать примеры и связанные ссылки.

Следующий шаг:
- при следующем проходе по manual дополнительно вылавливать места, где синтаксис, built-in и наглядные страницы перемешаны между собой;
- затем переходить к следующему крупному разделу, начиная с `docs/manual/date-time-functions.html`.
## Session 537 - 2026-04-29 17:05 - раздел даты и времени переведён на логическую подачу

Что сделано:
- перегруженная страница `docs/manual/date-time-functions.html` переписана в обзор раздела с каноническими форматами и картой подстраниц;
- добавлены тематические страницы `docs/manual/date-time-functions-current.html`, `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/date-time-functions-time-zones.html`;
- функции даты и времени разведены по смысловым группам: текущие значения, сборка и календарные части, арифметика и проверка, UTC и часовые пояса;
- обновлены `docs/manual/scripts/manual.js`, `docs/manual/reference.html` и `docs/manual/scripts/search-index.js`, чтобы новые страницы и сами функции находились через меню, указатель и глобальный поиск.

Изменённые файлы:
- `docs/manual/date-time-functions.html`
- `docs/manual/date-time-functions-current.html`
- `docs/manual/date-time-functions-parts-and-serial.html`
- `docs/manual/date-time-functions-arithmetic-and-validation.html`
- `docs/manual/date-time-functions-time-zones.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/manual/reference.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных ссылок для новых страниц раздела даты и времени и `reference.html`; битых переходов не найдено (`OK`);
- `dotnet`-сборка и тесты не запускались, потому что менялся только пользовательский manual.

Решение:
- считать недопустимой для manual подачу, где на одной странице без группировки смешаны текущие значения, построение, арифметика, проверка и часовые пояса;
- для крупных functional-разделов повторять ту же схему: обзорная страница раздела и тематические подстраницы по смысловым группам функций.

Следующий шаг:
- тем же способом пройти следующий перегруженный раздел manual, прежде всего `docs/manual/string-functions.html`, затем `docs/manual/regex-functions.html`.

## Session 538 - 2026-04-29 17:14 - строковый раздел переведён на логическую подачу

Что сделано:
- перегруженная страница `docs/manual/string-functions.html` переписана в обзор раздела со ссылками на тематические подстраницы;
- добавлены страницы `docs/manual/string-functions-slices-and-length.html`, `docs/manual/string-functions-trim-and-case.html`, `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions-build-and-split.html`;
- строковые функции разведены по смысловым группам: длина и фрагменты, обрезка и регистр, поиск и замена, построение строк и переход к массивам строк;
- обновлены `docs/manual/scripts/manual.js`, `docs/manual/reference.html` и `docs/manual/scripts/search-index.js`, чтобы новые страницы и имена функций находились через меню, быстрые переходы и глобальный поиск;
- отдельно исправлена синтаксическая ошибка в `docs/manual/scripts/search-index.js`, из-за которой новый строковый блок не мог корректно подключиться к индексу поиска.

Изменённые файлы:
- `docs/manual/string-functions.html`
- `docs/manual/string-functions-slices-and-length.html`
- `docs/manual/string-functions-trim-and-case.html`
- `docs/manual/string-functions-search-and-replace.html`
- `docs/manual/string-functions-build-and-split.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/manual/reference.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных ссылок для `docs/manual/string-functions.html`, всех новых строковых подстраниц и `docs/manual/reference.html`; битых переходов не найдено (`OK`);
- выполнена проверка синтаксиса `docs/manual/scripts/search-index.js` через `node`; файл читается без ошибок (`search-index.js: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- для крупных функциональных разделов manual сохранять ту же схему, что уже применена к файловым функциям, массивам и дате со временем: сначала логическая группировка, затем на тематических страницах короткое человеческое описание, форма вызова, замечания и пример;
- считать обязательным добавление имён самих built-in-функций в глобальный поиск при любом таком переразложении раздела.

Следующий шаг:
- перейти к `docs/manual/regex-functions.html` и соседним страницам, разложить раздел по смысловым группам и привести подачу к тому же жанру;
- затем отдельным проходом проверить, не осталось ли ещё ненайденных имён функций и операторов в глобальном поиске manual.

## Session 539 - 2026-04-29 17:22 - раздел регулярных выражений переведён на логическую подачу

Что сделано:
- страница `docs/manual/regex-functions.html` переписана в раздел с человеческой вводной, режимами, общими замечаниями и ссылками на тематические подстраницы;
- добавлены страницы `docs/manual/regex-functions-match-and-extract.html`, `docs/manual/regex-functions-arrays-and-split.html`, `docs/manual/regex-functions-replace-and-count.html`;
- функции регулярных выражений разведены по смысловым группам: проверка и первое совпадение, массивные результаты и разбиение, замена и подсчёт;
- обновлены `docs/manual/scripts/manual.js`, `docs/manual/reference.html` и `docs/manual/scripts/search-index.js`, чтобы новые страницы и имена `REGEX...`-функций находились через меню, быстрые переходы и глобальный поиск;
- по пути исправлена некорректная HTML-ссылка в `docs/manual/string-functions.html`, где раньше оставалась Markdown-запись вместо обычного перехода.

Изменённые файлы:
- `docs/manual/regex-functions.html`
- `docs/manual/regex-functions-match-and-extract.html`
- `docs/manual/regex-functions-arrays-and-split.html`
- `docs/manual/regex-functions-replace-and-count.html`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/manual/reference.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных ссылок для `docs/manual/regex-functions.html`, всех новых `regex`-подстраниц, `docs/manual/reference.html` и `docs/manual/string-functions.html`; битых переходов не найдено (`href-check: OK`);
- выполнена проверка синтаксиса `docs/manual/scripts/search-index.js` через `node`; файл читается без ошибок (`search-index.js: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- для разделов со встроенными функциями продолжать схему `раздел -> тематические подстраницы -> отдельные пользовательские блоки по функциям`, а не собирать всё на одной странице;
- считать обязательным, что при таком переразложении имена самих функций должны сразу попадать в глобальный поиск, иначе раздел остаётся недоделанным.

Следующий шаг:
- перейти к страницам операторов управления `if`, `select-case`, `for`, `while`, `do-loop` и привести их подачу к тому же жанру, сверяясь уже с близкими страницами Microsoft/VBA для операторов;
- затем отдельным проходом продолжить вылавливать пробелы в глобальном поиске manual.

## Session 540 - 2026-04-29 17:54 - страницы операторов управления приведены к единой справочной подаче

Что сделано:
- переписаны страницы `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`;
- для `IF`, `SELECT CASE`, `FOR`, `WHILE` и `DO ... LOOP` введена единая структура: краткое человеческое объяснение, затем `Синтаксис`, `Части записи`, `Замечания`, `Пример`, `Ручная проверка`, `Диагностика`, `См. также`;
- убраны перегруженные оглавления, промежуточные редакторские разделы и разностильные пояснения, из-за которых операторные страницы воспринимались не как цельная справка по языку, а как смесь учебника и внутренней заготовки;
- на странице `docs/manual/while.html` дополнительно поправлена HTML-запись примера, чтобы знак сравнения был корректно закодирован в разметке.

Изменённые файлы:
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена локальная проверка относительных ссылок для всех пяти страниц операторов управления; битых переходов не найдено (`href-check: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- считать правильной для операторных страниц такую же жанровую подачу, как и для функций: сначала короткое назначение конструкции, затем точная форма записи, затем практические замечания и один ясный пример;
- не возвращаться на этих страницах к раздутым оглавлениям и к редакторским пояснениям про саму страницу вместо описания конструкции языка.

Следующий шаг:
- перейти к страницам `docs/manual/print.html`, `docs/manual/input.html`, `docs/manual/open.html`, `docs/manual/close.html` и привести их к той же форме;
- затем отдельным проходом продолжить вылавливать пропуски в глобальном поиске manual.

## Session 541 - 2026-04-29 17:57 - циклы вынесены в отдельный раздел навигации и заголовки операторов уточнены

Что сделано:
- в `docs/manual/scripts/manual.js` раздел `Управление выполнением` сокращён до ветвления, а `FOR`, `WHILE`, `DO ... LOOP` перенесены в отдельный навигационный раздел `Циклы`;
- на страницах `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html` уточнены заголовки, подзаголовки и метки разделов, чтобы читатель видел полное назначение конструкции: `Оператор IF`, `Оператор SELECT CASE`, `Оператор цикла FOR`, `Оператор цикла WHILE ... END WHILE`, `Оператор цикла DO ... LOOP`;
- обновлён `docs/manual/scripts/search-index.js`, чтобы глобальный поиск показывал новые названия операторных страниц и новую классификацию по разделам `Управление выполнением` и `Циклы`.

Изменённые файлы:
- `docs/manual/scripts/manual.js`
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена проверка синтаксиса `docs/manual/scripts/search-index.js` через `node`; файл читается без ошибок (`search-index.js: OK`);
- выполнена локальная проверка относительных ссылок для обновлённых страниц операторов; битых переходов не найдено (`href-check: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- для manual считать более естественной группировку, где `IF` и `SELECT CASE` относятся к ветвлению и управлению выполнением, а `FOR`, `WHILE`, `DO ... LOOP` образуют самостоятельный циклический раздел;
- в заголовках операторных страниц предпочитать полное название конструкции с её ролью, а не оставлять одно ключевое слово без пояснения.

Следующий шаг:
- перейти к страницам `docs/manual/print.html`, `docs/manual/input.html`, `docs/manual/open.html`, `docs/manual/close.html` и привести их заголовки и подачу к тому же жанру;
- затем продолжить сверку остальных страниц manual с близкими жанрами Microsoft/VBA.

## Session 542 - 2026-04-29 18:03 - страницы PRINT, INPUT, OPEN и CLOSE приведены к единой справочной подаче

Что сделано:
- переписаны страницы `docs/manual/print.html`, `docs/manual/input.html`, `docs/manual/open.html`, `docs/manual/close.html`;
- для операторов `PRINT`, `INPUT`, `OPEN`, `CLOSE` введена единая структура: краткое назначение, `Синтаксис`, `Части записи`, `Замечания`, `Пример`, `Ручная проверка`, `Диагностика`, `См. также`;
- со страницы `PRINT` убрана избыточная дробность и редакторская подача, при этом сохранены ключевые пользовательские сведения про список вывода, завершающие разделители и `PRINT USING`;
- в `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js` уточнены названия страниц: `Оператор PRINT`, `Оператор INPUT`, `Оператор OPEN`, `Оператор CLOSE`, чтобы меню, поиск и сами заголовки manual совпадали между собой.

Изменённые файлы:
- `docs/manual/print.html`
- `docs/manual/input.html`
- `docs/manual/open.html`
- `docs/manual/close.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена проверка синтаксиса `docs/manual/scripts/search-index.js` через `node`; файл читается без ошибок (`search-index.js: OK`);
- выполнена локальная проверка относительных ссылок для страниц `print.html`, `input.html`, `open.html`, `close.html`; битых переходов не найдено (`href-check: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- для базовых операторов ввода-вывода применять ту же нормальную справочную форму, что уже введена для ветвления, циклов и функциональных разделов;
- считать желательным, чтобы название страницы в manual, подпись в меню и имя в поиске совпадали буквально, без лишнего разнобоя между `PRINT` и `Оператор PRINT`.

Следующий шаг:
- перейти к страницам `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/return.html` и выровнять их по тому же жанру;
- затем продолжить проход по оставшимся базовым страницам manual.

## Session 543 - 2026-04-29 18:06 - страницы SUB, FUNCTION и RETURN приведены к единой справочной подаче

Что сделано:
- переписаны страницы `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/return.html`;
- для тем `SUB`, `FUNCTION` и `RETURN` введена та же единая структура, что и на других уже переработанных страницах manual: краткое назначение, `Синтаксис`, `Части записи`, `Замечания`, `Пример`, `Ручная проверка`, `Диагностика`, `См. также`;
- убраны перегруженные техническими деталями вводные блоки и разностильные служебные разделы, чтобы страницы читались как нормальная пользовательская справка по языковой конструкции;
- в `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js` уточнены названия страниц: `Оператор объявления процедуры SUB`, `Оператор объявления функции FUNCTION`, `Оператор RETURN`.

Изменённые файлы:
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена проверка синтаксиса `docs/manual/scripts/search-index.js` через `node`; файл читается без ошибок (`search-index.js: OK`);
- выполнена локальная проверка относительных ссылок для страниц `sub.html`, `function.html`, `return.html`; битых переходов не найдено (`href-check: OK`);
- `dotnet`-сборка и тесты не запускались, потому что в этом шаге менялся только пользовательский manual.

Решение:
- различать в заголовках роль конструкции, а не оставлять только ключевое слово: объявление процедуры, объявление функции, оператор возврата;
- не смешивать на одной странице избыточную внутреннюю техническую детализацию с базовым объяснением конструкции, если эту же мысль можно подать короче и понятнее.

Следующий шаг:
- перейти к оставшимся обзорным и связующим страницам manual, прежде всего `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/examples.html`, `docs/manual/cheat-sheet.html`, и проверить их на старые служебные формулировки и несогласованные заголовки;
- затем отдельным проходом продолжить доочистку поиска и быстрых переходов там, где ещё всплывут старые названия.

## Session 544 - 2026-04-29 18:38 - синтаксис manual очищен от абстрактных плейсхолдеров и сверён тестами

Что сделано:
- перечитаны обязательные проектные документы `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md` и подтверждён текущий фокус сессии: приведение справочника к нормальной русской справочной подаче с параллельной сверкой по реальному поведению языка;
- на страницах `docs/manual/arrays-1d.html`, `docs/manual/arrays-2d.html`, `docs/manual/arrays-dynamic.html`, `docs/manual/if.html`, `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`, `docs/manual/select-case.html`, `docs/manual/sub.html`, `docs/manual/function.html` убраны абстрактные шаблоны синтаксиса и частей записи вроде `statements`, `parameterName`, `selector`, `<имя>` и заменены на конкретные формы с реальными именами переменных, вызовами и инструкциями;
- на страницах `docs/manual/sub.html` и `docs/manual/function.html` синтаксис объявления дополнительно доведён до согласованной формы с обязательным `AS` у параметров в самом примере, чтобы текст страницы не спорил с показанной записью;
- ранее начатые правки по примерам и комментариям в `.axm`-образцах сохранены, а текущий проход сфокусирован именно на том, чтобы manual не показывал читателю абстрактные заглушки вместо реальной записи языка.

Изменённые файлы:
- `docs/manual/arrays-1d.html`
- `docs/manual/arrays-2d.html`
- `docs/manual/arrays-dynamic.html`
- `docs/manual/if.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/select-case.html`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен поиск по `docs/manual` на остатки абстрактных плейсхолдеров `parameterList`, `argumentList`, `parameterTypeList`, `statements`, `<имя>`, `<границы>` и родственных форм; для текущего пакета страниц совпадения убраны;
- выполнен `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "DisplayName~samples/operators/sub|DisplayName~samples/operators/function|DisplayName~samples/operators/type|DisplayName~samples/control-flow/return|DisplayName~samples/control-flow/if|DisplayName~samples/control-flow/for|DisplayName~samples/control-flow/while|DisplayName~samples/control-flow/do|DisplayName~samples/control-flow/select_case|DisplayName~samples/operators/redim|DisplayName~samples/operators/dim|DisplayName~samples/operators/erase"`;
- целевой прогон прошёл полностью: 89 тестов из 89.

Решение:
- для пользовательских страниц manual считать недопустимыми абстрактные синтаксические заглушки там, где можно показать реальную форму записи языка;
- проверять не только внешний вид страницы, но и то, что пример синтаксиса не спорит с реальным поведением интерпретатора и acceptance-примерами.

Следующий шаг:
- перейти к обзорным и связующим страницам `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/examples.html`, `docs/manual/cheat-sheet.html` и убрать из них старые служебные формулировки;
- затем продолжить сплошную вычитку manual на несогласованные заголовки, пропуски в глобальном поиске и места, где синтаксис всё ещё подан не как реальный пользовательский пример.

## Session 545 - 2026-04-29 18:42 - обзорные страницы и шпаргалка очищены от служебной подачи

Что сделано:
- на страницах `docs/manual/index.html`, `docs/manual/procedures-and-functions.html`, `docs/manual/reference.html` переписаны заголовочные и вводные формулировки так, чтобы они говорили о языке и о том, чем полезна страница читателю, а не о внутреннем устройстве справочника;
- на главной странице `docs/manual/index.html` карточка раздела массивов теперь описывает содержание темы по существу, без фраз вроде «разделён на обзор»;
- в `docs/manual/procedures-and-functions.html` убраны слова `обзор` и служебные описания связующей страницы, а подача приведена к нормальной пользовательской формулировке;
- в `docs/manual/cheat-sheet.html` абстрактные шаблоны с угловыми скобками заменены на реальные короткие образцы для `IF`, `SELECT CASE`, `FOR`, `WHILE`, `DO ... LOOP` и нескольких встроенных функций.

Изменённые файлы:
- `docs/manual/index.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/reference.html`
- `docs/manual/cheat-sheet.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен поиск по обновлённым обзорным страницам на слова и шаблоны `Обзор`, `карта`, `карты тем`, `задаёт карту`, а также на остатки абстрактных форм вида `<условие>`, `<инструкции>`, `<выражение>`; для текущего пакета совпадений не осталось;
- выполнена ручная перечитка начала `index.html`, `procedures-and-functions.html` и `cheat-sheet.html`, чтобы убедиться, что после правки страницы читаются как нормальная справка, а не как внутренняя заготовка.

Решение:
- для обзорных и связующих страниц manual считать нежелательными формулировки, которые описывают структуру самого справочника вместо языка;
- для шпаргалки считать предпочтительными реальные короткие формы записи, даже если страница остаётся краткой и конспективной.

Следующий шаг:
- пройти страницы `docs/manual/arrays-slices.html`, `docs/manual/type.html`, `docs/manual/data-types.html`, `docs/manual/file-system-functions.html` и проверить, не остались ли там разделы с телеграфной или редакторской подачей;
- затем продолжить вычитку global search manual на пропуски по именам функций и операторов.
## Session 546 - 2026-04-29 19:02 - исправлен справочный синтаксис на страницах процедур, массивов, TYPE и базового ввода-вывода

Что сделано:
- страницы `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/arrays-1d.html`, `docs/manual/arrays-2d.html`, `docs/manual/arrays-dynamic.html`, `docs/manual/type.html`, `docs/manual/input.html`, `docs/manual/print.html` переписаны целиком, чтобы убрать сломанные заголовки, редакторские заготовки и примеры, которые подменяли собой раздел `Синтаксис`;
- для `SUB`, `FUNCTION`, одномерных и двумерных массивов, динамических массивов, `TYPE`, `INPUT` и `PRINT` раздел `Синтаксис` приведён к нормальной справочной форме: обязательные части показаны как обязательные, необязательные — в квадратных скобках, а пояснения вынесены в `Части записи`;
- на страницах `SUB` и `FUNCTION` устранено расхождение между текстом и синтаксисом по обязательному `AS` в объявлениях параметров;
- на странице `TYPE` убраны угловые скобки с псевдоплейсхолдерами из основных форм записи и заново разложены части синтаксиса, доступ к полям и массивы записей;
- на страницах `INPUT` и `PRINT` синтаксис больше не оформлен как набор частных примеров: для `INPUT` показана общая форма с необязательным приглашением, для `PRINT` разделены пустая форма, список вывода и `PRINT USING`.

Изменённые файлы:
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/arrays-1d.html`
- `docs/manual/arrays-2d.html`
- `docs/manual/arrays-dynamic.html`
- `docs/manual/type.html`
- `docs/manual/input.html`
- `docs/manual/print.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен автоматический проход по `docs/manual` на остатки комментариев и примерной подачи внутри разделов `Синтаксис`; для текущего пакета проблемных страниц таких совпадений не осталось;
- выполнен дополнительный поиск по характерным остаткам старой подачи вроде `'<имя>'`, комментариев `Объявление` внутри синтаксиса и повреждённых заголовков `????`; для обновлённого пакета совпадения не найдены.

Решение:
- если страница уже попала в состояние со смешением справочного синтаксиса, примеров и частично повреждённого текста, выгоднее переписать её целиком в едином жанре, чем чинить по строкам и оставлять скрытые несогласованности;
- раздел `Синтаксис` в manual считать отдельным контрактом страницы: в нём не должно быть учебных комментариев, сюжетных примеров и случайных имён, которые выглядят как фрагмент программы вместо формы записи.

Следующий шаг:
- пройти `docs/manual/arrays-slices.html`, `docs/manual/file-system-functions.html`, `docs/manual/data-types.html` и соседние страницы на тот же предмет: где страница рассказывает тему общими словами, но не даёт нормальной формы записи конкретной функции или оператора;
- затем отдельно пройти `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`, чтобы проверить полноту глобального поиска по именам файловых функций и другим уже существующим страницам.


## Session 547 - 2026-04-29 19:20 - ?????????? ???????? ?? ??????, ????? ?????? ? ?????? ???????? ???????

??? ???????:
- ?????????? ???????? `docs/manual/arrays-slices.html`: ?????? ??? ????? ?????????, ??? ????? ????, ??? ????? ?????????????, ??? ???????? ????? ?????????????? ? ?????????? ? ??? ???????? ?????????? `ARRAYSLICE`;
- ?????????? ???????? ???????? `docs/manual/file-system-functions.html` ? `docs/manual/data-types.html`, ????? ?????? ????????? ? ???????????? ?????? ? ???????? ?????????? ????????, ?????????? ? ????????? ??????? ??????;
- ? `docs/manual/scripts/manual.js` ?????? ????????? ??????? `????? ???????`, ?????? ? ???? ?????? ?????????? ?????? `???????`;
- ? `docs/manual/scripts/search-index.js` ?????? ???????? ? ?????????????? `?????` ? `?????` ??? ???????? ????????, ? ???????? ??????? ???????? ??????? ????????? ???????? ?????? `seekfile` ? ????????? ???????;
- ????????????? ?????????, ??? `SEEKFILE` ???????????? ? ????????? ??????? ? ????? ?? ????? `file-system-functions-open-files.html#seekfile`.

?????????? ?????:
- `docs/manual/arrays-slices.html`
- `docs/manual/file-system-functions.html`
- `docs/manual/data-types.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/scripts/search-index.js`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- ???????? ????? ?? ????? ?????????, `manual.js` ? `search-index.js` ?? ??????? ???? `?????`, `?????`, `????????`, `????? ???????`; ??? ???????? ?????? ?????????? ?? ????????;
- ???????? ????? ?? `docs/manual/scripts/search-index.js` ?? `SEEKFILE`, `READFILE`, `WRITEFILE`, `FILEEXISTS`, `READLINE`; ??????? ?????? ? ??????? ????????????.

???????:
- ???????? ???????? manual ?? ?????? ????????? ?????????? ???????????; ??? ?????? ????????? ???? ????? ? ?????? ???????? ??????? ? ????????? ?????????;
- ???????? ??? ????? ????? ??????? ??? ???????? ? ???? ?????????, ?? ?????? ?????: ????? ?????????????? ? ?????????? ? ????? ??????? `ARRAYSLICE`.

????????? ???:
- ?????? `docs/manual/file-system-functions-open-files.html`, `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions-management.html` ? ???????? ????????? ??????? ? ??? ?? ?????????? ?????? ??? ?????? ????? `????? ??????:`;
- ????? ????????? ??? ????? `search.html` ?? ???????? ? ?????? ?????????? ???????? ???????? ????? `loops.html`, `expressions-and-branching.html`, `arrays-and-builtins.html`, `data-and-input.html`.

## Session 548 - 2026-04-29 20:18 - файловые функции приведены к карточкам и сверены с кодом

Что сделано:
- полностью переписаны страницы `docs/manual/file-system-functions-open-files.html`, `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions-management.html`;
- для каждой файловой функции страница теперь даёт отдельную карточку с назначением, разделом `Синтаксис`, пояснением частей записи и отдельным примером, а общие замечания и диагностика вынесены в конец страницы;
- на странице функций для открытого файла устранена сломанная разметка после неудачной прошлой правки и отдельно нормально описан `SEEKFILE`, включая ограничение по режиму `APPEND`;
- после сверки с `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs` исправлены реальные расхождения manual с кодом: в документации добавлены необязательные формы `GETFILES(directoryPath[, mask])`, `GETDIRS(directoryPath[, mask])`, `COPYFILE(sourcePath, targetPath[, overwrite])`, `MOVEFILE(sourcePath, targetPath[, overwrite])`, `COPYDIR(sourcePath, targetPath[, overwrite])`.

Изменённые файлы:
- `docs/manual/file-system-functions-open-files.html`
- `docs/manual/file-system-functions-text.html`
- `docs/manual/file-system-functions-paths.html`
- `docs/manual/file-system-functions-management.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнен поиск по обновлённым страницам на остатки старой подачи вроде `Форма вызова`, редакторских слов и сломанной HTML-разметки; для этого пакета таких следов не осталось;
- подтверждено по `docs/manual/scripts/search-index.js`, что имена `SEEKFILE`, `REWIND`, `READLINE`, `READLINES`, `WRITEFILE`, `APPENDFILE`, `FILEEXISTS`, `GETFILES`, `GETDIRS`, `MAKEDIR`, `DELETEFILE`, `CHANGEDIR`, `MAKETEMPFILE` присутствуют в глобальном поиске;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~File|FullyQualifiedName~Dir|FullyQualifiedName~ReadLine|FullyQualifiedName~ReadLines|FullyQualifiedName~ReadText|FullyQualifiedName~ReadToEnd|FullyQualifiedName~SeekFile|FullyQualifiedName~Rewind|FullyQualifiedName~Flush|FullyQualifiedName~NewLine"`: пройдено 52 из 52 тестов;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~File|FullyQualifiedName~Dir|FullyQualifiedName~ReadLine|FullyQualifiedName~ReadToEnd"`: пройдено 6 из 6 тестов;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~File|FullyQualifiedName~Dir|FullyQualifiedName~ReadLine|FullyQualifiedName~ReadLines|FullyQualifiedName~ReadText|FullyQualifiedName~ReadToEnd|FullyQualifiedName~SeekFile|FullyQualifiedName~Rewind|FullyQualifiedName~Flush|FullyQualifiedName~NewLine"`: пройдено 8 из 8 тестов.

Решение:
- файловые страницы manual больше не считаются сводными полотнами; базовой единицей подачи для функций теперь считается отдельная карточка функции, а не общий список сигнатур сверху и пояснения где-то ниже;
- при дальнейшей вычитке manual синтаксис нужно сверять не только с уже существующим текстом страницы, но и с реальными сигнатурами built-in-функций в семантике, чтобы не повторять расхождения по необязательным параметрам.

Следующий шаг:
- пройти следующие крупные блоки manual тем же способом: оставшиеся страницы по строковым, числовым, регулярным и датовым функциям на предмет старой телеграфной подачи, пропущенных необязательных параметров и слабых примеров;
- отдельным проходом начать перевод и вычитку комментариев в `.axm`-примерах, чтобы образцы в `samples/` тоже были оформлены по-русски и могли служить нормальной частью документации.

## Session 549 - 2026-04-29 20:39 - строковые, регулярные и датовые страницы выровнены по справочному синтаксису

Что сделано:
- переписаны `docs/manual/string-functions.html`, `docs/manual/string-functions-slices-and-length.html`, `docs/manual/string-functions-trim-and-case.html`, `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions-build-and-split.html`;
- на этих страницах синтаксис приведён к нормальной справочной форме с возвращаемым результатом и квадратными скобками для необязательных параметров, а не к набору разрозненных перегрузок без пояснений;
- переписаны `docs/manual/date-time-functions-current.html` и `docs/manual/date-time-functions-time-zones.html`, чтобы формы `DATE([timeZone])`, `TIME([timeZone])`, `NOW([timeZone])`, `UTCOFFSET([value[, timeZone]])`, `TOUTC(value[, timeZone])`, `FROMUTC(value[, timeZone])` были показаны именно как технический синтаксис;
- переписана `docs/manual/regex-functions-match-and-extract.html`, где формы `REGEXISMATCH`, `REGEXMATCH` и `REGEXEXTRACT` теперь записаны через необязательный `modes`, а не просто перечислены отдельными строками как набор примеров;
- для строковых страниц выполнена сверка с `BuiltinFunctions.String.Basic.cs`, `BuiltinFunctions.String.Trim.cs`, `BuiltinFunctions.String.Search.cs`, `BuiltinFunctions.String.Fill.cs`, `BuiltinFunctions.String.Char.cs`, `BuiltinFunctions.String.Array.cs`, а для датовых и регулярных страниц — с `BuiltinFunctions.DateTime.cs` и `BuiltinFunctions.Regex.cs`.

Изменённые файлы:
- `docs/manual/string-functions.html`
- `docs/manual/string-functions-slices-and-length.html`
- `docs/manual/string-functions-trim-and-case.html`
- `docs/manual/string-functions-search-and-replace.html`
- `docs/manual/string-functions-build-and-split.html`
- `docs/manual/date-time-functions-current.html`
- `docs/manual/date-time-functions-time-zones.html`
- `docs/manual/regex-functions-match-and-extract.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- выполнена сверка сигнатур страниц с кодом built-in-функций по строковым, регулярным и датовым дескрипторам;
- выполнен поиск по обновлённым страницам на остатки служебных формулировок вроде `Форма вызова`, а также на старую редакторскую подачу; для текущего пакета такие остатки убраны;
- изменений в интерпретаторе, семантике и байткоде этим шагом не вносилось, поэтому отдельный прогон `dotnet test` не требовался.

Решение:
- для справочных страниц built-in-функций закреплён тот же принцип, что и для файловых функций: перегрузки и необязательные параметры должны собираться в один понятный технический синтаксис, а не разъезжаться на несколько строк без объяснения;
- обзорные страницы тематических блоков нужно писать как ввод в возможности языка, а не как склад голых списков имён.

Следующий шаг:
- пройти оставшиеся страницы по числовым функциям, общей странице регулярных выражений и общей странице даты и времени, где ещё видна телеграфная или редакторская подача;
- затем перейти к переводу и вычитке комментариев в `.axm`-примерах, начиная с тех разделов, которые уже приведены в нормальный вид в manual.

## Session 550 - 2026-04-29 20:41 - ???????? ???????? manual ?????????? ? ?????????? ???

??? ???????:
- ????????? ?????????? `docs/manual/numeric-functions.html`, `docs/manual/numeric-functions-conversion-and-legacy.html`, `docs/manual/numeric-functions-core.html`, `docs/manual/numeric-functions-trigonometry.html`, `docs/manual/numeric-functions-roots-rounding.html`;
- ?? ???????? ???????? ???????? ??????? ?????? ?????? ?????? ?? ????????? ??????? ??? ? ?????????? ?????????????, ? ?????? ?? ????????? ??????? ????????, ???????? ??????? ? ???????? ??????????;
- ???????? `INT/STR/VAL`, ??????? ??????????, ????????????? ? ?????????? ?????????? ?? ?? ?? ?????, ??? ??? ???????????? ? ???????? ? ????????? ????????: ????????? ???????? ???????, `?????????`, ????????? ?????? ??????, ????????? ???, ??? ??? ????????????? ?????, ? ????????? ??????;
- ?? ????????? ??????? ?????? ???????????? ????? `????? ??????`, ????? ??????? ???????? ??? ?????????? ? ?????? ???????????? ????? ??? ??, ??? ???? ?? ?????? ? ??????????.

?????????? ?????:
- `docs/manual/numeric-functions.html`
- `docs/manual/numeric-functions-conversion-and-legacy.html`
- `docs/manual/numeric-functions-core.html`
- `docs/manual/numeric-functions-trigonometry.html`
- `docs/manual/numeric-functions-roots-rounding.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- ????????? ?????? ???????? ? ????????? ????? ? `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Numeric.cs` ? `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Conversion.cs`;
- ??????????? ???????? HTML-???????? ????? ??????, ????? ?????????, ??? `RND` ??????? ??? ??????????, `INT` ?????? ??? ???????????? `INTEGER`, ? ??????????? ??? `ASIN`, `ACOS`, `ACOSH`, `ATANH`, `SQR`, `LOG`, `LOG10`, `LOG2` ? `POW` ?? ???????????? ???????? ???????? built-in;
- ????????? ? ??????????????, ????????? ? ???????? ???? ????? ?? ?????????, ??????? ????????? ?????? `dotnet test` ?? ??????????.

???????:
- ???????? ?????? manual ?????? ???? ?????????? ? ??????? ??????????? ?????: ??????? ?????????? ???????????? ???????? ???????, ????? ??????????? ?????????, ? ?? ???????? ? ?? ??????????;
- ??? ???????? ?????????????? ??????? ???????? ??? ?? ???????, ??? ? ??? ????????? ????????: ???? ???? ??????? ???????, ??? ?? ?????? ??????????? ????? ??????? ??? ????????? ? ????????? ? ????? ? ?????.

????????? ???:
- ?????? ???????? ???????? `docs/manual/date-time-functions.html` ? `docs/manual/regex-functions.html`, ?????? ??? ?????????? ???????????? ? ??????????? ?????? ? ??? ????????????? ????????? ????????? ???????????;
- ????? ??????? ? `samples/` ? ?????? ??????? ???????????? ? `.axm`-???????? ?? ??????? ????, ??????? ? ??? ??????????? ???????? manual.

## Session 551 - 2026-04-29 20:52 - ???????? ???????? ???? ? ?????????? ????????? ?????????

??? ???????:
- ?????????? `docs/manual/date-time-functions.html` ? `docs/manual/regex-functions.html`, ????? ?????? ???????? ????????, ????????? ??????? ? ???????? ??????? ? ?????????? ?????????? ??????;
- ????????????? ?????????? `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/regex-functions-arrays-and-split.html`, `docs/manual/regex-functions-replace-and-count.html`, ?????? ??? ????? ? ??????????? ???????? ??? ?? ??? ?????????? ? ?????? ?????? ?????;
- ?? ???? ????????? ????????? ??????? ? ?????? ????? ? ??????????? ? ??????????? ???????? ??? ?????????????? ?????????? ???, ??? ??? ?????, ? ??????? ????? ?????? ??? ????????? ???????? ? ???????????, ??????????? ? ?????????.

?????????? ?????:
- `docs/manual/date-time-functions.html`
- `docs/manual/date-time-functions-parts-and-serial.html`
- `docs/manual/date-time-functions-arithmetic-and-validation.html`
- `docs/manual/regex-functions.html`
- `docs/manual/regex-functions-arrays-and-split.html`
- `docs/manual/regex-functions-replace-and-count.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- ????????? ?????? ???? ?????? ? `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs` ? `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Regex.cs`;
- ????????????, ??? ? manual ????????? ???????? ????? `DATE([timeZone])`, `TIME([timeZone])`, `NOW([timeZone])`, `UTCOFFSET([value[, timeZone]])`, `REGEXMATCHES(text, pattern[, modes])`, `REGEXEXTRACTALL(text, pattern, groupNumber[, modes])`, `REGEXSPLIT(text, pattern[, modes])`, `REGEXREPLACE(text, pattern, replacement[, modes][, count])`, `REGEXCOUNT(text, pattern[, modes])`;
- ????????? ? ???? ????? ???? ????? ?? ????, ??????? ????????? ?????? `dotnet test` ?? ??????????.

???????:
- ???????? ???????? ?? ???? ? ?????????? ?????????? ?????? ????? ????????? ??????????? ?????, ? ?? ???????? ??? ???????????? ??????? ??? ?????? ??? ??? ?????????;
- ??? ??????? ?????????? ???????? ??? ?? ???????, ??? ? ??? ????????? ??? ???????????? ???????? manual: ???? ??? ?????????? ??????? ???????? ????????? ?????? ???? ?????? ? ???? ???????? ?????????? ?????, ? ?? ?????????? ???????? ??????????.

????????? ???:
- ??????? ? `samples/` ? ?????? ??????????????? ??????? ???????????? ? `.axm`-???????? ?? ??????? ????, ??????? ? ??? ??????????? ???????? manual;
- ??????????? ??? ????? ??????? ???????? ???????, ??? ??????????? ?????? ??? ??? ??? ?????? ?? ?????????? ?????? ????? ??????? ??? ?????????.

## Session 552 - 2026-04-29 20:50 - ????????? ??????????? ??????? ? ???????? ???????? ??????? ???????? BytecodeVm

??? ???????:
- ???????? ???????? ?????? `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- --out C:\Axiom\.artifacts\manual-checks\manual-check-report.txt`, ????? ????????? ?? ?????? ????? manual, ?? ? ???? runnable `.axm`-??????? ???????????;
- ? ???? ??????? ?????? ? ????????? ?????????? ????? `samples/functions/rnd/basic_usage.axm` ? ?????? ????????: ?????? ???????? ?????? ?????? ?? ????????? ??????, ? `Axiom.ManualChecks` ?????? ?????? ???????????? ?????;
- ? `src/Axiom.ManualChecks/Program.cs` ????????? ??????????? ???????? ??? `RND`, ??????? ?????????? ???????? ??????? ?????? ??? ????? ?? 0 ???????????? ?? 1 ?? ???????????? ? ?????????? `CultureInfo.InvariantCulture`, ????? smoke-???????? ?? ???????? ?? ?????? Windows;
- ? `samples/functions/rnd/basic_usage.axm` ??????? ??????? ??????????? ?? ?????????? ??????, ????? ?????? ?? ?????? ?????? ??? ?????? ???, ??? ??????? ?????????? ? ???? ????????? ????????;
- ???????? `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` ? ????????????, ??? ????? ???? ????? acceptance-???????? ????????, ?? ??? ????????????? ??????? ?? ??? ?? ??????????? ? ?????? `BytecodeVm` ??? ??????: `samples/functions/arrayflatten/by_visible_order.axm` ? `samples/functions/arrayreshape/from_sorted_flat.axm` ??????????? ???????????? `MB4001`;
- ?????????????? ???????? ????????? `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayFlatten|FullyQualifiedName~ArrayReshape"` ????????????, ??? ???? built-in `ARRAYFLATTEN` ? `ARRAYRESHAPE` ? ???????? ????????, ? ????????? ???????? ????????? ?????? ? ??????? ????-??????? ?????? acceptance-?????????.

?????????? ?????:
- `src/Axiom.ManualChecks/Program.cs`
- `samples/functions/rnd/basic_usage.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- --out C:\Axiom\.artifacts\manual-checks\manual-check-report.txt` ? ????? ??????????? `RND` ???????? 45 ?? 45 ?????? smoke-????????;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` ? ???????? 547 ?? 549, ???????? 2 ??????? ?? `ARRAYFLATTEN` ? `ARRAYRESHAPE` ? ?????? `BytecodeVm` ??? ??????;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~ArrayFlatten|FullyQualifiedName~ArrayReshape"` ? ???????? 6 ?? 6.

???????:
- ??????? ? ??????? ???????? ????? ???????????? ???????? ???????????, ????? ???? ??????? ????? ????? ?????????? ? ??????????? ??????? ? ??????????? ????????;
- ????????? ??????? `ARRAYFLATTEN` ? `ARRAYRESHAPE` ??????? ?? ????????? ????????????, ? ???????? ?????????? ???????? ????-??????? ??????, ?????? ??? acceptance-???? ??????? ?????? ??? ????????????????? ??????, ? ??????????? built-in ??? ???? ??? ??????? ?????????? ???????.

????????? ???:
- ???????? ?????????, ?????? `samples/functions/arrayflatten/by_visible_order.axm` ? `samples/functions/arrayreshape/from_sorted_flat.axm` ???????? `MB4001` ? ?????? ????-?????? ???????, ? ??????? ???? ?????? ???? ? lowering, ???? ? ????????????? VM-????????;
- ????? ????? ?????????? ??????????????? ?????? ? ??????? `.axm`-???????? ?? ??? ??????????? ???????? manual.

## Session 553 - 2026-04-29 20:56 - ?????? ?????? BytecodeVm ?? ARRAYFLATTEN ? ARRAYRESHAPE ? acceptance-????????

??? ???????:
- ????????? ??????? ???? ?????????? ??????? `AcceptanceProgramTests`: `samples/functions/arrayflatten/by_visible_order.axm` ? `samples/functions/arrayreshape/from_sorted_flat.axm` ?? ??????????? ? ?????? `BytecodeVm` ?? ??????, ??? built-in ??????????? ? ????????, ? ??????, ??? `AxiomExecutionPipeline` ?? ?????? ?? ????????? ????-?????? ???????;
- ? `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` ? ?????? `VmSupportedBuiltins` ????????? `BuiltinFunctionId.ArrayFlatten` ? `BuiltinFunctionId.ArrayReshape`;
- ???????? ???????? ????????????? ???? `Run_WithBytecodeMode_UsesBytecodeVmForArrayFlattenAndReshape` ? `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`, ????? ? ??????? ???????? ?????????? ????? ????????????? `BytecodeVm` ? ????? ??????? ???????? ????????? ??????? ?????;
- ????????? acceptance-?????? ??????????, ??? ??? ??????? ?????? ????????????? ???????? ??? ?????? ?? ?????????????.

?????????? ?????:
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Run_WithBytecodeMode_UsesBytecodeVmForArrayFlattenAndReshape"` ? ??????? 1 ?? 1;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` ? ???????? 549 ?? 549;
- ? ???? ?????? ?? ???????? ??????? ?????????????? `MSB3026` ? ??????? ????? ? `.artifacts\obj` ??-?? ??????? ??????? ???????? `dotnet test`; ??? ?? ??????? ?????????, ?? ???????????? ??????? ??????????? ? ????????????? ??????? MSBuild-??????? ?????? ?????????????????.

???????:
- ????????? ?????? ??????? ???????? ??? ???????? ????????????? VM-????????, ? ?? ????????????? ?????????? built-in ? ????-?????? ????????;
- ??? ?????????? `.axm`-???????? ?????????????? ????????????????? ??????? `dotnet`-???????? ??? ????????????? ?????? ??????, ????? ?? ??????? ?????? ?????????????? ? ???????? ??????????.

????????? ???:
- ?????????? ??????????????? ?????? ? ??????? runnable `.axm`-???????? ?? ??? ??????????? ???????? manual;
- ???????? ??????? ?? ?????? ?? ????????????? ????? ???????? ?????????? built-in ? `BytecodeVm` ? ????? ??????? `VmSupportedBuiltins`, ???? ??? ????????? acceptance-???????? ???????? ????? `MB4001`.

## Session 554 - 2026-04-29 21:03 - ??????? ????????? ????? ???????????? ? runnable-???????? ?? ?????????? ? ?????????? ???????????

??? ???????:
- ?????????? ? ?????????? ??????????? ? ????????? ???????????? ?????? `.axm`-????????: `samples/operators/input/single_variable.axm`, `samples/operators/input/prompt_semicolon.axm`, `samples/operators/input/prompt_comma.axm`, `samples/operators/input/multiple_variables.axm`, `samples/control-flow/while/basic_loop.axm`, `samples/control-flow/if/basic_if_else.axm`, `samples/control-flow/if/elseif_chain.axm`, `samples/control-flow/for/basic_loop.axm`, `samples/control-flow/for/step_loop.axm`, `samples/control-flow/for/nested_loops.axm`, `samples/control-flow/for/exit_for.axm`, `samples/control-flow/do/loop_while.axm`, `samples/control-flow/do/loop_until.axm`, `samples/control-flow/do/exit_do.axm`, `samples/control-flow/select_case/exact_values.axm`, `samples/control-flow/select_case/string_values.axm`;
- ?? ???? ???? ?????? ?????? ?????????? ????? ???? `Expected:` ? ???????? ?? ???????? ??????? ?????????: ??? ?????? ?????????? ??????, ????? ??????? ?????? ?????? ??????? ???, ??? ??? ?????, ? ????? ????? ?????????;
- ??? ????????????? ???????? `INPUT` ???????? ????????, ??? ??? ????????????? ??? ??????? ??????? ? ?????????? ???????? ?????, ? ??? ?????? ? ????????? ????????? ?????????? ??????? ?????????? ?????? ?? ???????.

?????????? ?????:
- `samples/operators/input/single_variable.axm`
- `samples/operators/input/prompt_semicolon.axm`
- `samples/operators/input/prompt_comma.axm`
- `samples/operators/input/multiple_variables.axm`
- `samples/control-flow/while/basic_loop.axm`
- `samples/control-flow/if/basic_if_else.axm`
- `samples/control-flow/if/elseif_chain.axm`
- `samples/control-flow/for/basic_loop.axm`
- `samples/control-flow/for/step_loop.axm`
- `samples/control-flow/for/nested_loops.axm`
- `samples/control-flow/for/exit_for.axm`
- `samples/control-flow/do/loop_while.axm`
- `samples/control-flow/do/loop_until.axm`
- `samples/control-flow/do/exit_do.axm`
- `samples/control-flow/select_case/exact_values.axm`
- `samples/control-flow/select_case/string_values.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` ? ???????? 549 ?? 549;
- ????????????? `INPUT`-??????? ??????????? ?? ???????????, ?????? ??? ??? ?? ?????? ??????? ??????? ?????, ?? ??? ???? ?????? ?? ???????, ???????? ?????? ???????????.

???????:
- ??? runnable-???????? ?? ?????????? ? ?????????? ??????????? ???????? ??? ?? ????????, ??? ??? ????? ??????????? ? built-in-????????: ??????????? ?????? ????????? ?????????? ??????? ? ????????? ?????????, ? ?? ?????? ????????? ???????????? ??????? `Expected:`;
- ??? ?????????? ??????? `samples/` ????????????? ??????? ????? ???????? ?? ??????????????: ?????? ?????? ???????? ?????????? ?? ??????? ?????, ?????? ? ?????? ???????? ?????????? ??????.

????????? ???:
- ?????????? ????? ?? ?????? ?? ?????????? ???????? ?????? `.axm`-????????, ??? ??? ???????? ????? ?????????? ? ?????? ?????: `PRINT`, `DIM`, ????????? ??????? ? ????????? built-in;
- ??? ???? ????????? ???????: ??????? ?????? ????????????, ????? ???????????????? ?????????????? ?????? ??? ????????, ??????? ?????? ? acceptance-?????.

## Session 555 - 2026-04-29 21:09 - ??????? ????? runnable-???????? ?? PRINT, DIM ? REDIM

??? ???????:
- ?????????? ? ?????????? ??????????? ? ????????? ?????? `.axm`-????????: `samples/operators/print/using_basic.axm`, `samples/operators/print/separator_semicolon.axm`, `samples/operators/print/separator_comma.axm`, `samples/operators/print/mixed_list.axm`, `samples/operators/print/empty_print.axm`, `samples/operators/print/basic_expression.axm`, `samples/operators/dim/multiple_declarations.axm`, `samples/operators/dim/inferred_type_from_initializer.axm`, `samples/operators/dim/dynamic_array_declaration.axm`, `samples/operators/dim/decimal_explicit_declaration.axm`, `samples/operators/dim/basic_declaration.axm`, `samples/operators/dim/array_declaration.axm`, `samples/operators/redim/basic_usage.axm`, `samples/operators/redim/preserve_basic_usage.axm`, `samples/operators/redim/empty_array.axm`;
- ?? ???? ?????? ?????? ?????????? ? ??????????? ????? ???? `Expected:` ? `Foundation-stage ...`, ?????? ??? ????????? ?????????? ??????? ????????? ? ???, ??? ?????? ????????????? ????????? ? ????? ????? ?????????;
- ??? `REDIM` ? `REDIM PRESERVE` ???????? ???????? ????? ??????? ? ????????? ????????? ????? ????????? ????????? ???????, ????? ???? ??? ??????? ?? ?????? ??? ????, ?? ? ??? ????????? ??????? ??? ????????????.

?????????? ?????:
- `samples/operators/print/using_basic.axm`
- `samples/operators/print/separator_semicolon.axm`
- `samples/operators/print/separator_comma.axm`
- `samples/operators/print/mixed_list.axm`
- `samples/operators/print/empty_print.axm`
- `samples/operators/print/basic_expression.axm`
- `samples/operators/dim/multiple_declarations.axm`
- `samples/operators/dim/inferred_type_from_initializer.axm`
- `samples/operators/dim/dynamic_array_declaration.axm`
- `samples/operators/dim/decimal_explicit_declaration.axm`
- `samples/operators/dim/basic_declaration.axm`
- `samples/operators/dim/array_declaration.axm`
- `samples/operators/redim/basic_usage.axm`
- `samples/operators/redim/preserve_basic_usage.axm`
- `samples/operators/redim/empty_array.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` ? ???????? 549 ?? 549;
- ??? ????? ??????????????? ???????? ?? ??????? ?? ?????????, ???????? ?????? ??????????? ? ?????????? ?????.

???????:
- ????? ???????? ?? `PRINT`, `DIM` ? `REDIM` ?????? ????????????? ???? ?? ?????????, ??????? ??? ??????????? ? manual: ??????? ??????? ?????????? ??????????, ????? ????????? ?????????, ??? ???????????? ???????? ???????;
- ??????? ?? ??????????? ? ????????????????? ???????? ?????? ????? ???????? ??? ?????? ??????? ? ????????????, ?????? ??? ??????????? ????????? ????? ????????, ? ?? ?????? ??????????? ????? ??????????.

????????? ???:
- ?????????? ?????? ?? `samples/` ?? ????????? ??????? ?????? ? ???? ?? ??????????: ????????? ??????? ? ????????? built-in, ??? ??? ???????? ????? ?????????? ????? ? ??????? ?????? ????????????;
- ????? ??????? ?????? ????????? ??? ?? ???????: ??????? ?????? ????????????, ????? ???????????????? acceptance-?????? ??????????????? ????????.

## Session 556 - 2026-04-29 21:15 - вычищен широкий пакет runnable-примеров по строковым функциям и массивным built-in

Что сделано:
- переведены и переписаны комментарии в широком пакете `.axm`-примеров по строковым функциям и массивным built-in: `samples/functions/ucase/basic_usage.axm`, `samples/functions/lcase/basic_usage.axm`, `samples/functions/len/basic_usage.axm`, `samples/functions/left/basic_usage.axm`, `samples/functions/right/basic_usage.axm`, `samples/functions/mid/basic_usage.axm`, `samples/functions/trim/basic_usage.axm`, `samples/functions/ltrim/basic_usage.axm`, `samples/functions/rtrim/basic_usage.axm`, `samples/functions/reverse/basic_usage.axm`, `samples/functions/repeat/basic_usage.axm`, `samples/functions/string/basic_usage.axm`, `samples/functions/space/basic_usage.axm`, `samples/functions/arrayfill/basic_usage.axm`, `samples/functions/arraysort/basic_usage.axm`, `samples/functions/arraytosorted/basic_usage.axm`, `samples/functions/arrayreverse/basic_usage.axm`, `samples/functions/arraytoreversed/basic_usage.axm`, `samples/functions/arraydistinct/basic_usage.axm`, `samples/functions/arraydistinct/multidimensional_values.axm`, `samples/functions/arraybinarysearch/basic_usage.axm`, `samples/functions/arraytake/basic_usage.axm`, `samples/functions/arrayskip/basic_usage.axm`, `samples/functions/arraypop/basic_usage.axm`, `samples/functions/arrayevery/basic_usage.axm`, `samples/functions/arraysome/basic_usage.axm`, `samples/functions/arrayfilter/basic_usage.axm`, `samples/functions/arraymap/basic_usage.axm`, `samples/functions/arraymap/polymorphic_transform.axm`, `samples/functions/arrayreduce/basic_usage.axm`, `samples/functions/arrayreduce/polymorphic_accumulator.axm`, `samples/functions/arrayflatten/by_visible_order.axm`, `samples/functions/arrayreshape/from_sorted_flat.axm`, `samples/functions/mid/zero_count_error.axm`, `samples/functions/left/negative_count_error.axm`, `samples/functions/right/negative_count_error.axm`, `samples/functions/string/empty_fill_error.axm`;
- из этих файлов убраны английские шапки вида `Expected:` и черновые технические пометки вроде `manual acceptance sample`, вместо них добавлены нормальные русские пояснения: что делает функция, что именно показывает сценарий и какой вывод или какая диагностика ожидается;
- отдельно исправлены битые по кодировке комментарии в примерах `ARRAYFLATTEN` и `ARRAYRESHAPE`, чтобы они снова читались нормально прямо в `Axiom.Ide`;
- в массивах дополнительно усилены пояснения там, где важно понять смысл примера: какие built-in работают на месте, какие создают независимую копию, где возвращается компактный массив, а где предикат или преобразование захватывает внешнее состояние.

Изменённые файлы:
- `samples/functions/ucase/basic_usage.axm`
- `samples/functions/lcase/basic_usage.axm`
- `samples/functions/len/basic_usage.axm`
- `samples/functions/left/basic_usage.axm`
- `samples/functions/right/basic_usage.axm`
- `samples/functions/mid/basic_usage.axm`
- `samples/functions/trim/basic_usage.axm`
- `samples/functions/ltrim/basic_usage.axm`
- `samples/functions/rtrim/basic_usage.axm`
- `samples/functions/reverse/basic_usage.axm`
- `samples/functions/repeat/basic_usage.axm`
- `samples/functions/string/basic_usage.axm`
- `samples/functions/space/basic_usage.axm`
- `samples/functions/arrayfill/basic_usage.axm`
- `samples/functions/arraysort/basic_usage.axm`
- `samples/functions/arraytosorted/basic_usage.axm`
- `samples/functions/arrayreverse/basic_usage.axm`
- `samples/functions/arraytoreversed/basic_usage.axm`
- `samples/functions/arraydistinct/basic_usage.axm`
- `samples/functions/arraydistinct/multidimensional_values.axm`
- `samples/functions/arraybinarysearch/basic_usage.axm`
- `samples/functions/arraytake/basic_usage.axm`
- `samples/functions/arrayskip/basic_usage.axm`
- `samples/functions/arraypop/basic_usage.axm`
- `samples/functions/arrayevery/basic_usage.axm`
- `samples/functions/arraysome/basic_usage.axm`
- `samples/functions/arrayfilter/basic_usage.axm`
- `samples/functions/arraymap/basic_usage.axm`
- `samples/functions/arraymap/polymorphic_transform.axm`
- `samples/functions/arrayreduce/basic_usage.axm`
- `samples/functions/arrayreduce/polymorphic_accumulator.axm`
- `samples/functions/arrayflatten/by_visible_order.axm`
- `samples/functions/arrayreshape/from_sorted_flat.axm`
- `samples/functions/mid/zero_count_error.axm`
- `samples/functions/left/negative_count_error.axm`
- `samples/functions/right/negative_count_error.axm`
- `samples/functions/string/empty_fill_error.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests` — пройдено 549 из 549;
- поведение самих неинтерактивных примеров этим шагом не менялось, менялись только комментарии, поясняющие шапки и испорченные кодировкой строки.

Решение:
- широкий пакет строковых и массивных примеров теперь выглядит как нормальный обучающий материал, а не как набор обрывочных англоязычных пометок для внутренней проверки;
- для примеров по массивам зафиксирован единый стиль комментариев: сначала смысл built-in, затем ожидаемый результат, затем при необходимости короткая ремарка о независимой копии, сохранении границ или захвате внешнего состояния.

Следующий шаг:
- продолжить такой же широкий проход по следующему связанному пакету `samples/functions/`, где ещё остались английские шапки и слабые пояснения: дато-временные функции, остаток строковых сценариев с ошибками и многомерные массивные примеры;
- после очередного пакета снова подтвердить результат последовательным acceptance-прогоном.

## Session 557 - 2026-04-29 21:23 - вычищены дато-временные примеры и остаточные англоязычные комментарии в samples/functions

Что сделано:
- переведены и переписаны комментарии в дато-временном пакете runnable-примеров: `samples/functions/utctime/current_utc_time.axm`, `samples/functions/utcnow/current_utc_now.axm`, `samples/functions/utcdate/current_utc_date.axm`, `samples/functions/time/named_timezone_current_time.axm`, `samples/functions/now/named_timezone_current_datetime.axm`, `samples/functions/toutc/roundtrip_to_local.axm`, `samples/functions/toutc/named_timezone_to_utc.axm`, `samples/functions/fromutc/roundtrip_to_utc.axm`, `samples/functions/fromutc/named_timezone_from_utc.axm`, `samples/functions/startofmonth/from_datetime.axm`, `samples/functions/endofmonth/leap_february.axm`, `samples/functions/startofyear/from_datetime.axm`, `samples/functions/endofyear/from_datetime.axm`, `samples/functions/isleapyear/leap_year_true_false.axm`;
- вычищены оставшиеся англоязычные шапки в нескольких диагностических строковых сценариях: `samples/functions/len/wrong_arity_error.axm`, `samples/functions/left/zero_count_error.axm`, `samples/functions/right/zero_count_error.axm`, `samples/functions/mid/invalid_start_error.axm`, `samples/functions/mid/out_of_range_start_behavior.axm`, `samples/functions/repeat/negative_count_error.axm`;
- добраны отдельные хвосты по массивным built-in: переведён комментарий в `samples/functions/arraypush/basic_usage.axm`, переведён комментарий в `samples/functions/arrayreverse/by_column.axm`, исправлена испорченная строка в `samples/functions/arrayisallocated/basic_usage.axm`;
- обновлены `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы новая точка остановки по очистке runnable-примеров была зафиксирована в активных документах.

Изменённые файлы:
- `samples/functions/utctime/current_utc_time.axm`
- `samples/functions/utcnow/current_utc_now.axm`
- `samples/functions/utcdate/current_utc_date.axm`
- `samples/functions/time/named_timezone_current_time.axm`
- `samples/functions/now/named_timezone_current_datetime.axm`
- `samples/functions/toutc/roundtrip_to_local.axm`
- `samples/functions/toutc/named_timezone_to_utc.axm`
- `samples/functions/fromutc/roundtrip_to_utc.axm`
- `samples/functions/fromutc/named_timezone_from_utc.axm`
- `samples/functions/startofmonth/from_datetime.axm`
- `samples/functions/endofmonth/leap_february.axm`
- `samples/functions/startofyear/from_datetime.axm`
- `samples/functions/endofyear/from_datetime.axm`
- `samples/functions/isleapyear/leap_year_true_false.axm`
- `samples/functions/len/wrong_arity_error.axm`
- `samples/functions/left/zero_count_error.axm`
- `samples/functions/right/zero_count_error.axm`
- `samples/functions/mid/invalid_start_error.axm`
- `samples/functions/mid/out_of_range_start_behavior.axm`
- `samples/functions/repeat/negative_count_error.axm`
- `samples/functions/arraypush/basic_usage.axm`
- `samples/functions/arrayreverse/by_column.axm`
- `samples/functions/arrayisallocated/basic_usage.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- проверка будет выполнена после завершения пакета правок через `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests`.

Решение:
- продолжать чистку runnable-примеров крупными связанными пакетами, а не по одному файлу, чтобы стиль комментариев оставался последовательным;
- временные и календарные примеры теперь считать приведёнными к тому же русскому формату пояснений, что и уже вычищенные строковые и массивные сценарии.

Следующий шаг:
- пройти оставшиеся одиночные англоязычные шапки вне текущего пакета, прежде всего в `samples/operators/` и `samples/smoke/`;
- затем повторить поисковую проверку по `samples/functions/` и добрать всё, что ещё осталось со старыми `Expected:` и похожими техническими пометками;
- после этого снова подтвердить состояние последовательным acceptance-прогоном.

## Session 558 - 2026-04-29 21:28 - добраны одиночные старые шапки в operator- и smoke-примерах

Что сделано:
- вычищен последний найденный остаток старой англоязычной шапки в `samples/functions/right/out_of_range_count_behavior.axm`;
- переписаны короткие комментарии в operator-примерах `samples/operators/assignment/basic_assignment.axm`, `samples/operators/assignment/array_element.axm`, `samples/operators/const/basic_constant.axm`, `samples/operators/mod/basic_usage.axm`, `samples/operators/integer-division/basic_usage.axm`;
- переведены и нормализованы пояснения в smoke-примерах `samples/smoke/block_scope_semantics.axm`, `samples/smoke/v0_1_baseline.axm`, `samples/smoke/multidimensional-arrays/array_first_group.axm`, `samples/smoke/block_scope_full_manual.axm`, включая завершающий блок отрицательных проб;
- `docs/PLANS3.md` и `docs/WORKLOG4.md` дополнены новой точкой остановки после добора одиночных хвостов.

Изменённые файлы:
- `samples/functions/right/out_of_range_count_behavior.axm`
- `samples/operators/assignment/basic_assignment.axm`
- `samples/operators/assignment/array_element.axm`
- `samples/operators/const/basic_constant.axm`
- `samples/operators/mod/basic_usage.axm`
- `samples/operators/integer-division/basic_usage.axm`
- `samples/smoke/block_scope_semantics.axm`
- `samples/smoke/v0_1_baseline.axm`
- `samples/smoke/multidimensional-arrays/array_first_group.axm`
- `samples/smoke/block_scope_full_manual.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- после этих правок нужно повторно прогнать `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests`.

Решение:
- не оставлять одиночные старые шапки в видимых каталогах `samples/operators/` и `samples/smoke/`, даже если основной пакет уже был вычищен раньше;
- считать широкий каталог runnable-примеров ценным пользовательским материалом, поэтому стиль пояснений должен быть последовательным не только внутри `samples/functions/`, но и в smoke- и operator-папках.

Следующий шаг:
- повторить поисковый проход по `samples/` на остаточные `Expected:` и похожие технические шапки;
- если что-то ещё осталось, закрыть это последним коротким пакетом и снова подтвердить acceptance-прогоном;
- затем можно переходить к следующему содержательному пакету по приоритету пользователя.

## Session 559 - 2026-04-29 21:34 - добраны выраженческие примеры и старые шапки у цикла DO

Что сделано:
- переписаны комментарии в оставшихся найденных выраженческих примерах: `samples/expressions/string_concat/basic_concat.axm`, `samples/expressions/comparisons/string_order.axm`, `samples/expressions/comparisons/basic_compare.axm`, `samples/expressions/arrays/index_access.axm`, `samples/expressions/arithmetic/decimal_overflow_error.axm`, `samples/expressions/arithmetic/basic_math.axm`, `samples/expressions/logical/basic_logic.axm`, `samples/expressions/conversions/automatic_numeric.axm`, `samples/expressions/numeric_literals/decimal_out_of_range_error.axm`;
- вычищены старые шапки `Expected output:` в примерах `samples/control-flow/do/do_while_loop.axm`, `samples/control-flow/do/do_until_loop.axm`, `samples/control-flow/do/do_loop_exit.axm`;
- в `do_loop_exit.axm` дополнительно выровнен регистр ключевых слов и общий стиль записи, чтобы пример выглядел так же, как остальные современные runnable-файлы;
- `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены новой точкой остановки после этого пакета.

Изменённые файлы:
- `samples/expressions/string_concat/basic_concat.axm`
- `samples/expressions/comparisons/string_order.axm`
- `samples/expressions/comparisons/basic_compare.axm`
- `samples/expressions/arrays/index_access.axm`
- `samples/expressions/arithmetic/decimal_overflow_error.axm`
- `samples/expressions/arithmetic/basic_math.axm`
- `samples/expressions/logical/basic_logic.axm`
- `samples/expressions/conversions/automatic_numeric.axm`
- `samples/expressions/numeric_literals/decimal_out_of_range_error.axm`
- `samples/control-flow/do/do_while_loop.axm`
- `samples/control-flow/do/do_until_loop.axm`
- `samples/control-flow/do/do_loop_exit.axm`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- после этого пакета нужно снова прогнать `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~AcceptanceProgramTests`.

Решение:
- старые выраженческие примеры и исторические варианты `DO` тоже должны подчиняться общему правилу каталога: короткое русское пояснение вместо англоязычной шапки с пересказом вывода;
- допустимо заодно выравнивать регистр ключевых слов в таких файлах, если это не меняет поведение и уменьшает визуальный разнобой.

Следующий шаг:
- снова повторить поисковый проход по всему `samples/` на остаточные `Expected:` и похожие технические шапки;
- если поиск ещё что-то найдёт, закрыть это последним коротким пакетом и снова подтвердить acceptance-прогоном;
- если хвостов не останется, считать текущую волну вычитки runnable-примеров завершённой.

## Session 560 - 2026-04-29 21:40 - regex-страница дополнена таблицами по синтаксису шаблонов

Что сделано:
- по запросу пользователя проверено, как регулярные выражения реально исполняются в Axiom: шаблон передаётся в стандартный `System.Text.RegularExpressions.Regex`, а локально документируемыми режимами остаются `i`, `m`, `s`, `x`;
- на `docs/manual/regex-functions.html` добавлен новый справочный раздел по синтаксису шаблонов с таблицами по буквальным символам, классам, экранированию, повторителям, группам, якорям, альтернативам, обратным ссылкам и lookaround-конструкциям;
- в этом же разделе отдельно пояснено, что повторитель относится к предыдущему элементу, а для шаблона `[,;]\s*` прямо расшифровано значение `*` как «ноль или больше повторений предыдущего элемента»;
- на `docs/manual/regex-functions-arrays-and-split.html` рядом с примером `REGEXSPLIT` добавлено отдельное текстовое пояснение к шаблону `[,;]\s*`.

Изменённые файлы:
- `docs/manual/regex-functions.html`
- `docs/manual/regex-functions-arrays-and-split.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялась только пользовательская документация manual без изменений исполняемого кода.

Решение:
- считать regex-страницу обязанной описывать не только режимы и сами built-in-функции, но и основной рабочий синтаксис шаблонов, раз Axiom напрямую опирается на стандартный движок `.NET Regex`;
- пояснения к таким примерам, как `\s*`, размещать не только в общей таблице, но и рядом с конкретным примером, где эта конструкция реально используется.

Следующий шаг:
- после этой точечной правки вернуться к следующему содержательному направлению проекта;
- если новых локальных замечаний по manual не появится, не зацикливаться на regex-странице дальше и переключиться на следующий приоритетный пакет работ.

## Session 561 - 2026-04-29 21:52 - правило жанрового Microsoft/VB-референса закреплено для будущих manual-pass

Что сделано:
- по новому замечанию пользователя уточнён постоянный редакционный договор manual: использовать нужно не одну запомненную страницу и не общий абстрактный "Microsoft-стиль", а ближайшую текущую страницу Visual Basic на `learn.microsoft.com` по жанру документируемой темы;
- в `AGENTS.md` добавлено явное репозиторное правило: для каждой страницы `docs/manual/` искать ближайший Visual Basic-референс по жанру, использовать его как редакционный ориентир и адаптировать вводное объяснение темы под Axiom;
- в `C:\Axiom\.codex\skills\axiom-doc-author\SKILL.md` закреплено, что текущие страницы Microsoft Learn теперь обязательны как живой жанровый ориентир наряду с локальными историческими PDF;
- в `C:\Axiom\.codex\skills\axiom-doc-author\references\microsoft-style-guide.md` добавлено прямое требование брать из Microsoft не только секции и синтаксис, но и нормальные вводные объяснения конструкции, переписанные под реальные факты Axiom;
- в правило включены и стартовые официальные примеры жанрового подбора: обзор массивов, `For...Next` и `Select...Case` на Microsoft Learn.

Изменённые файлы:
- `AGENTS.md`
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/references/microsoft-style-guide.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялись только внутренние правила документирования и рабочие документы без правок исполняемого кода.

Решение:
- считать жанровый подбор ближайшей текущей Visual Basic-страницы на `learn.microsoft.com` обязательной частью каждого серьёзного прохода по `docs/manual/`;
- считать вводное объяснение темы таким же обязательным редакционным элементом, как и блок `Синтаксис`, если тема по жанру требует нормального концептуального входа для читателя.

Следующий шаг:
- после фиксации этого правила вернуться к следующему содержательному направлению проекта;
- при любом следующем manual-pass уже применять новый договор автоматически, без повторного отдельного напоминания.
## Session 562 - 2026-04-29 22:00 - усилены вводные обзорные страницы manual

Что сделано:
- продолжен manual-pass уже по закреплённому правилу жанрового Microsoft/VB-референса: от Visual Basic бралась только редакционная манера вводного объяснения темы, а факты, синтаксис и примеры оставались строго по правилам Axiom;
- переписана страница `docs/manual/getting-started.html`: усилены вводный абзац, стартовый ориентир, пояснение по устройству файла программы, комментариям и первым ручным запускам;
- переписана страница `docs/manual/data-types.html`: восстановлен нормальный русский текст вместо испорченного кодировочного хвоста, добавлены вводное объяснение роли типов, обзор модели типов Axiom, краткая сводка по основным типам и понятный маршрут по соседним страницам;
- усилена обзорная страница `docs/manual/procedures-and-functions.html`: добавлено более ясное вводное описание роли `SUB` и `FUNCTION`, уточнён общий ориентир по процедурам, функциям, `RETURN`, локальным объявлениям и функциональным типам;
- обновлены `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы эта волна manual-правок была зафиксирована как отдельный содержательный шаг.

Изменённые файлы:
- `docs/manual/getting-started.html`
- `docs/manual/data-types.html`
- `docs/manual/procedures-and-functions.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялась только пользовательская документация manual без изменений исполняемого кода.

Решение:
- считать обзорные страницы manual обязанными сначала вводить читателя в тему нормальным объяснением, а уже затем разветвлять его на синтаксис, примеры и соседние справочные страницы;
- для таких обзорных страниц держать жёсткое правило: стилистический ориентир можно брать у Visual Basic, но синтаксис, поведение и ограничения описываются только по Axiom.

Следующий шаг:
- продолжить такую же редакторскую переработку на следующих крупных входных страницах manual, прежде всего `data-and-input.html`, `control-flow.html` и `operators.html`;
- после обзорных страниц отдельно проверить, каким справочным страницам отдельных операторов и встроенных функций тоже не хватает короткого вводного абзаца перед разделом синтаксиса.
## Session 563 - 2026-04-29 22:09 - усилены обзорные входы manual по данным, выражениям, циклам и указателю

Что сделано:
- продолжен manual-pass по следующему пакету обзорных страниц с тем же правилом: у Visual Basic бралась только редакционная подача обзорной темы, а фактическое содержание оставалось строго Axiom-специфичным;
- переработана страница `docs/manual/data-and-input.html`: добавлено вводное объяснение связки типов, объявлений и ввода-вывода, а также короткий сквозной пример маршрута `DIM` -> `INPUT` -> `PRINT`;
- усилена страница `docs/manual/expressions-and-branching.html`: добавлены вводный обзор роли выражений и ветвления, отдельный ориентир с примером `IF`, а также более ясная связь между арифметикой, логикой и выбором ветви;
- расширена страница `docs/manual/loops.html`: добавлены объяснение выбора между `FOR`, `WHILE` и `DO ... LOOP`, краткий ориентир с контрастным примером и блок ссылок `См. также`;
- переработан `docs/manual/reference.html`: указатель теперь объясняет, когда лучше пользоваться им, а когда обзорными страницами, и даёт короткий маршрут по manual вместо простого длинного списка ссылок;
- обновлены `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать эту волну редакторской переработки отдельно от предыдущей.

Изменённые файлы:
- `docs/manual/data-and-input.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/loops.html`
- `docs/manual/reference.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялась только пользовательская документация manual без изменений исполняемого кода.

Решение:
- считать обзорные страницы manual не просто списком ссылок, а точкой входа, которая сначала объясняет назначение раздела и типичный маршрут чтения;
- для обзорных страниц по-прежнему держать жёсткое правило: стилистический ориентир можно брать у Visual Basic, но синтаксис, поведение и ограничения описываются только по Axiom.

Следующий шаг:
- перейти к следующей волне manual-pass уже на уровне отдельных справочных страниц операторов, где перед разделом синтаксиса ещё не хватает короткого вводного объяснения;
- в первую очередь проверить `if.html`, `select-case.html`, `for.html`, `while.html`, `do-loop.html`, `input.html` и `print.html`.
## Session 564 - 2026-04-29 22:18 - усилены вводные абзацы на справочных страницах ключевых операторов

Что сделано:
- продолжен manual-pass уже не по обзорным страницам, а по отдельным справочным статьям, чтобы и на уровне конкретных операторов manual перестал начинаться сразу с сухого синтаксиса;
- на `docs/manual/if.html` добавлен вводный абзац о роли условного ветвления и короткое практическое пояснение в разделе замечаний;
- на `docs/manual/select-case.html` добавлен вводный абзац о сценариях выбора между несколькими значениями одного выражения и короткое пояснение, когда `SELECT CASE` удобнее цепочки однотипных проверок;
- на `docs/manual/for.html`, `docs/manual/while.html` и `docs/manual/do-loop.html` добавлены вводные абзацы по смыслу каждой формы цикла и краткие практические пояснения о типичных случаях выбора;
- на `docs/manual/input.html` и `docs/manual/print.html` добавлены вводные абзацы по роли пользовательского ввода и базового вывода, а также короткие практические пояснения в разделах замечаний;
- обновлены `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы эта волна точечной редакторской доводки была зафиксирована отдельно.

Изменённые файлы:
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/input.html`
- `docs/manual/print.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялась только пользовательская документация manual без изменений исполняемого кода.

Решение:
- считать короткий вводный абзац перед синтаксисом обязательным не только для обзорных страниц, но и для ключевых справочных статей по наиболее часто используемым конструкциям;
- по-прежнему использовать Visual Basic только как редакционный ориентир по ритму объяснения, не перенося оттуда синтаксис или поведение в фактическое описание Axiom.

Следующий шаг:
- продолжить такую же точечную доводку на следующих справочных страницах, где всё ещё не хватает краткого вводного контекста;
- в первую очередь пройти `function.html`, `sub.html`, `return.html`, `type.html`, `arrays-indexing.html` и ближайшие страницы по встроенным функциям.
## Session 565 - 2026-04-29 22:28 - усилены вводные абзацы на страницах процедур, функций и индексирования массивов

Что сделано:
- продолжен manual-pass по следующему пакету справочных страниц, уже после операторов управления и ввода-вывода;
- на `docs/manual/function.html` добавлен вводный абзац о роли вычисляющей функции и короткое практическое пояснение в разделе замечаний о случаях, где результат должен встраиваться в выражения;
- на `docs/manual/sub.html` добавлен вводный абзац о роли процедуры как именованного действия без возвращаемого значения и короткое практическое пояснение о вынесении повторяющегося кода в отдельный блок;
- на `docs/manual/return.html` добавлен вводный абзац о роли `RETURN` как механизма выхода из `SUB` и возврата результата из `FUNCTION`, а также короткое практическое пояснение о снижении лишней вложенности;
- на `docs/manual/arrays-indexing.html` добавлен вводный абзац о различии между получением одного элемента и представления на часть массива, а также короткое практическое пояснение о выборе полной и частичной формы индексирования;
- обновлены `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать эту волну точечной редакторской доводки отдельно.

Изменённые файлы:
- `docs/manual/function.html`
- `docs/manual/sub.html`
- `docs/manual/return.html`
- `docs/manual/arrays-indexing.html`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- автоматические тесты не запускались, потому что в этой сессии менялась только пользовательская документация manual без изменений исполняемого кода.

Решение:
- считать короткий вводный абзац и практическое пояснение выбора полезными не только для операторов управления, но и для страниц по процедурам, функциям и массивам;
- по-прежнему использовать Visual Basic только как редакционный ориентир по структуре и тону объяснения, не перенося оттуда синтаксис или семантику в фактическое описание Axiom.

Следующий шаг:
- продолжить такую же точечную доводку на соседних страницах по массивам и встроенным функциям, где ещё не хватает краткого вводного контекста;
- в первую очередь пройти `arrays-slices.html`, `arrays-functions.html`, `numeric-functions.html` и `string-functions.html`.
## Session 566 - 2026-04-29 22:52 - устранено подвисание IDE на длительном запуске программы

Что сделано:
- перечитаны обязательные активные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`;
- разобран пользовательский сценарий с `samples/smoke/repair_plans_mojibake.axm` и просмотрен свежий журнал IDE `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-223802.log`;
- журнал подтвердил, что запуск не падал с ошибкой: в нём есть `run.request`, `console.begin` и первый `console.output`, после чего новые записи не появлялись, то есть окно зависало во время самого выполнения;
- найдена архитектурная причина: `MainWindowViewModel` вызывал `_programRunService.Run(...)`, а также операции `Continue` и `Step...` синхронно в UI-потоке WPF;
- команды запуска и отладки переведены на асинхронный путь: в реальном приложении выполнение уходит в фоновую задачу, а в тестовой среде без `Application.Current` сохраняется синхронный путь для детерминированности;
- в `docs/ARCHITECTURE.md` зафиксирована новая граница ответственности между WPF-оболочкой, фоновым запуском и `ProgramConsoleService`.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln` проходит;
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj` проходит, 285/285.

Решение:
- считать зависание окна при длинном запуске не проблемой конкретной `repair_plans_mojibake.axm`, а архитектурной ошибкой оболочки: тяжёлое выполнение нельзя держать в UI-потоке;
- считать текущую правку исправлением отзывчивости IDE, а не ускорением самого runtime: отдельная оценка производительности самой программы ещё нужна.

Следующий шаг:
- отдельно промерить `samples/smoke/repair_plans_mojibake.axm` в `Bytecode`-режиме и понять, что именно даёт основную длительность;
- после этого решить, нужен ли следующий шаг по отмене длинного запуска, индикатору прогресса или оптимизации runtime/VM.

## Session 567 - 2026-04-29 23:35 - исследована медлительность байткодного режима на `repair_plans_mojibake.axm`

Что сделано:
- перечитаны обязательные активные документы проекта и продолжено расследование уже после правки UI-отзывчивости IDE;
- просмотрены два последних журнала IDE: `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-225518.log` и `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260429-230130.log`;
- журналы подтвердили разницу режимов: в байткоде выполнение останавливается после первого `console.output`, а в интерпретаторе тот же сценарий успешно завершается;
- собран отдельный воспроизводитель в `.artifacts/repro-bytecode/`, который запускает `samples/smoke/repair_plans_mojibake.axm` вне IDE на копии `docs/WORKLOG4.md` внутри `.artifacts`;
- воспроизводитель подтвердил: интерпретатор стабильно заканчивает сценарий примерно за 24 секунды, а байткод после двух минут всё ещё не завершает его;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` сначала отключено создание обратимых снимков состояния вне отладки, а затем отключено и обязательное создание `BytecodeVmExecutionSnapshot` на каждой инструкции в обычном запуске без наблюдателя и без отладочных условий;
- добавлен счётчик инструкций для диагностического профиля воспроизводителя: после оптимизации байткод стал заметно легче по памяти и быстрее по числу инструкций, но горячие места всё ещё находятся в строковых посимвольных циклах примера.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `.artifacts/repro-bytecode/repro-bytecode.csproj`
- `.artifacts/repro-bytecode/Program.cs`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` проходит, 659/659;
- изолированный запуск воспроизводителя подтверждает: режим `Interpreter` завершает сценарий успешно, режим `Bytecode` после оптимизаций больше не раздувает память как раньше, но всё ещё не укладывается в 120 секунд на полном файле.

Решение:
- считать доказанным, что проблема в пользовательском сценарии уже не сводится к подвисанию UI и не выглядит как прежний чисто отладочный перерасход памяти;
- считать установленным, что байткодная VM всё ещё слишком медленна на длинной посимвольной обработке строк с частыми вызовами `LEN`, `MID`, `ASC` и связанных функций, хотя лишние отладочные накладные расходы обычного запуска уже убраны.

Следующий шаг:
- снять более точный профиль горячих операций байткодной VM на строковых built-in и понять, что даёт главный вклад: диспетчеризация инструкций, вызовы built-in или копирование `RuntimeValue`;
- после этого выбрать либо ещё одну целевую оптимизацию VM, либо защитный механизм IDE для слишком долгих байткодных запусков.

## Session 568 - 2026-04-29 23:51 - ускорен байткодный проход `repair_plans_mojibake.axm`

Что сделано:
- продолжено расследование после сессии 567 и расширен временный воспроизводитель `.artifacts/repro-bytecode/Program.cs` счётчиками по opcode и встроенным функциям;
- профиль подтвердил частые строковые операции `LEN`, `MID`, `ASC`, но главный скрытый источник стоимости найден в `BytecodeVm.ResolveLocation`: повторное получение `TextLocation` каждый раз заново линейно обходило `BytecodeDebugInfo.InstructionMap`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен кэш исходных позиций по паре `routineId` и `instructionIndex`;
- в `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` словарь `VisibleVariables` переведён на ленивое создание, чтобы обычные вызовы маленьких пользовательских функций не создавали отладочную структуру без необходимости;
- полный запуск `samples/smoke/repair_plans_mojibake.axm` через временный воспроизводитель в режиме `Bytecode` теперь завершается успешно.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `.artifacts/repro-bytecode/Program.cs`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` проходит, 659/659;
- `dotnet build C:\Axiom\Axiom.sln` проходит без предупреждений и ошибок;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- bytecode` завершается успешно: `RuntimeMs` около 32859, `TotalMs` около 33602, диагностик 0, строк вывода 4, рабочая копия около 305927 байт;
- контрольный запуск `dotnet run --no-build --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- interpreter` также завершается успешно: `RuntimeMs` около 42537, `TotalMs` около 43788.

Решение:
- считать текущую причину медленного байткодного режима найденной и исправленной для данного сценария: обычное исполнение больше не платит линейным обходом отладочной карты на каждой горячей инструкции;
- оставить временный воспроизводитель в `.artifacts/`, потому что он является диагностическим артефактом, а не постоянным примером языка.

Следующий шаг:
- собрать и вручную проверить тот же сценарий через `Axiom.Ide` в режиме `Bytecode`, чтобы подтвердить пользовательский путь после консольного воспроизводителя;
- если IDE-путь подтвердится, вернуться к основной плановой ветке проекта.

## Session 569 - 2026-04-30 00:24 - уточнена производительность байткодной VM в Debug и Release

Что сделано:
- продолжено расследование после пользовательского уточнения, что на скриншотах интерпретатор выполнялся быстрее байткода;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлены короткие пути для частых бинарных операций, сравнений, строковых built-in `LEN`, `ASC`, `MID`, `LEFT`, `RIGHT`, а также прямое чтение и запись горячих локальных слотов и параметров;
- предрасчёт runtime-констант сделан безопасным: `RoutineName` и `BuiltinName` остаются служебными константами пула и не ломают создание VM;
- в `src/Axiom.Runtime/Values/RuntimeValue.cs` добавлено кэширование булевых и малых целых значений, а обычные строки без фиксированной длины больше не копируются при сохранении в слот;
- расширенный временный воспроизводитель `.artifacts/repro-bytecode/Program.cs` использован для повторного сравнения `Interpreter`, `Bytecode` и профиля горячих инструкций.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- `.artifacts/repro-bytecode/Program.cs`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` проходит, 659/659;
- `dotnet build C:\Axiom\Axiom.sln` проходит без предупреждений и ошибок;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- interpreter`: `RuntimeMs` около 24894, `TotalMs` около 25515;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- bytecode`: `RuntimeMs` около 27267, `TotalMs` около 28084;
- `dotnet run -c Release --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- interpreter`: `RuntimeMs` около 8699, `TotalMs` около 9140;
- `dotnet run -c Release --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- bytecode`: `RuntimeMs` около 5633, `TotalMs` около 6006.

Решение:
- считать подтверждённым, что текущая байткодная VM больше не зависает на сценарии `repair_plans_mojibake.axm`;
- считать подтверждённым, что в отладочной сборке `Debug` байткод на этом сценарии всё ещё может быть медленнее интерпретатора, но в `Release` показывает ожидаемый выигрыш;
- не считать это изменением поведения языка: правки касаются только внутреннего исполнения VM и хранения runtime-значений.

Следующий шаг:
- проверить фактический способ запуска пользовательской `Axiom.Ide`: если IDE запускается из `Debug`, сравнение скоростей будет заведомо более шумным и неблагоприятным для стековой VM;
- после этого решить, достаточно ли запускать/поставлять IDE в `Release`, или нужна отдельная крупная оптимизация байткода для отладочной сборки, например специализированный `SELECT CASE` по целочисленным константам или уменьшение стоимости вызовов маленьких пользовательских функций.

## Session 570 - 2026-04-30 09:47 - добавлен постоянный набор сравнительных прогонов движков

Что сделано:
- перечитаны обязательные документы проекта, затем подтверждено по журналам IDE, что пользовательский запуск шёл обычным `Run`/`Ctrl+F5`, а не через режим отладки;
- в `src/Axiom.ManualChecks/Program.cs` добавлен новый режим `compare-engines`, который прогоняет один и тот же `.axm` через `Interpreter` и `Bytecode`, сравнивает статус, вывод и диагностику, а также пишет времена исполнения в отчёт под `.artifacts/manual-checks/`;
- в `samples/smoke/engine-compare/` добавлены постоянные runnable-сценарии `document_batch_repair.axm`, `regex_log_analysis.axm`, `array_cube_rollup.axm` и `high_rank_metadata.axm`;
- сценарий высокого ранга использован и как ручная проверка текущего факта: в реализации массива нет отдельного явного верхнего предела ранга, проверяется только условие `rank >= 1`;
- в `docs/manual/arrays-multidimensional.html`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md` и `docs/PLANS3.md` зафиксированы новый сравнительный путь и текущее состояние договора по максимальной размерности массива.

Изменённые файлы:
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/document_batch_repair.axm`
- `samples/smoke/engine-compare/regex_log_analysis.axm`
- `samples/smoke/engine-compare/array_cube_rollup.axm`
- `samples/smoke/engine-compare/high_rank_metadata.axm`
- `docs/manual/arrays-multidimensional.html`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1` проходит успешно и пишет отчёт в `.artifacts/manual-checks/`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2` также проходит успешно и даёт более устойчивую картину по тем же четырём сценариям;
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- по этому прогону все четыре сценария завершаются одинаково в обоих движках без диагностик;
- смешанная картина производительности подтверждена уже на постоянном наборе: байткод выигрывает не на всех профилях нагрузки.

Решение:
- считать подтверждённым, что выигрыш байткода сейчас носит не универсальный, а профильный характер;
- считать зафиксированным, что вопрос о максимальной размерности массива нельзя выводить из C# автоматически: это отдельный договор Axiom, и в текущем состоянии явного верхнего лимита нет;
- использовать `compare-engines` как основной повторяемый ручной путь для дальнейших разговоров о скорости `Interpreter` и `Bytecode`.

Следующий шаг:
- расширять набор `samples/smoke/engine-compare/` только под новые реальные профили нагрузки, если понадобится отдельный замер по файловому API, regex-цепочкам или функциям изменения формы массива;
- затем выбрать следующий узкий профиль оптимизации байткодной VM уже по этим сравнительным отчётам, а не по одному частному сценарию.
## Session 571 - 2026-04-30 11:00 - запущен первый системный этап разгона Bytecode VM

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` вызов built-in переведён на единый дескрипторный слой: VM заранее строит обработчики по `builtin id`, использует быстрые тракты арности `0/1/2/3` и оставляет вариативный путь только как запасной общий механизм;
- значение built-in как функции теперь хранит прямую байткодную привязку в `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs` и `src/Axiom.Runtime/Values/RuntimeValue.cs`, поэтому косвенный вызов больше не делает линейный поиск по таблице built-in;
- общий тракт вызова пользовательских `FUNCTION` и `SUB` переведён на аренду аргументного буфера через `ArrayPool<RuntimeValue>`, чтобы сократить число краткоживущих выделений памяти в горячем цикле VM;
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` кэшированы видимая длина и шаги хранения многомерного массива, а плотные `GetValue`/`SetValue` и копирование пересечений больше не строят временные массивы индексов на каждом обращении;
- в `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` проверка пригодности built-in для байткодной VM переведена на единый слой `BytecodeVm.SupportsBuiltin(...)`;
- в `src/Axiom.ManualChecks/Program.cs` расширен постоянный сравнительный режим `compare-engines`, а в `samples/smoke/engine-compare/` добавлены новые runnable-сценарии `file_batch_roundtrip.axm`, `regex_modes_pipeline.axm` и `array_transform_chain.axm`, чтобы сравнивать движки не по одному примеру, а по нескольким классам нагрузки.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/file_batch_roundtrip.axm`
- `samples/smoke/engine-compare/regex_modes_pipeline.axm`
- `samples/smoke/engine-compare/array_transform_chain.axm`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1` проходит успешно и пишет отчёт в `.artifacts/manual-checks/`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2` проходит успешно и пишет итоговый отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-105519.txt`;
- по всем семи сценариям `compare-engines` подтверждён паритет статуса, вывода и диагностики между `Interpreter` и `Bytecode`.

Решение:
- считать первый системный этап разгона байткодной VM архитектурно запущенным и зафиксированным: у VM теперь есть единый слой built-in-вызовов, дешёвая передача аргументов и расширенный постоянный сравнительный стенд;
- не считать задачу ускорения завершённой: по текущему отчёту байткод всё ещё проигрывает на профилях массивов, плотных циклов и регулярных выражений, поэтому следующий шаг должен опираться на эти категории, а не на частные строковые улучшения.

Следующий шаг:
- отдельно профилировать и ускорять проигрывающие категории из `compare-engines`, прежде всего `array_cube_rollup.axm`, `array_transform_chain.axm`, `regex_log_analysis.axm` и `regex_modes_pipeline.axm`;
- уменьшить стоимость кадров и косвенных вызовов VM там, где исполнение ещё падает в вариативный запасной путь, и по данным переносить следующие горячие built-in в прямые дескрипторы.

## Session 572 - 2026-04-30 11:34 - ускорен прямой индексный путь массивов в Bytecode VM

Что сделано:
- продолжен системный разгон байткодной VM без изменения пользовательской семантики языка;
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` добавлены span-ориентированные перегрузки `ContainsIndices`, `ContainsLeadingIndices`, `GetValue`, `SetValue` и `CreateView`, чтобы внутренний код VM мог работать с индексами без промежуточных `int[]`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` выделен отдельный быстрый путь для обычного индексирования массива без диапазонных срезов;
- для типичных рангов `1..8` индексы теперь собираются через `stackalloc`, а аренда `ArrayPool<int>` сохраняется только для более крупных размерностей;
- прежний путь со `slice`-структурами и прежней диагностикой сохранён для диапазонного индексирования и ошибочных сценариев;
- сравнительный ручной прогон движков повторён после правки, новый отчёт записан в `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-113400.txt`.

Изменённые файлы:
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-113400.txt`;
- по этому прогону паритет статуса, вывода и диагностик между `Interpreter` и `Bytecode` сохранён по всем 7 сценариям;
- на массивном сценарии `samples/smoke/engine-compare/array_cube_rollup.axm` отношение `bytecode/interpreter` по runtime снизилось примерно до `1.69x` против около `2.09x` на предыдущем системном шаге.

Решение:
- считать этот шаг внутренней оптимизацией VM, а не изменением поведения языка;
- считать подтверждённым, что лишняя материализация индексных массивов действительно была частью стоимости плотных многомерных циклов в байткодном пути;
- не расширять текущую правку на более широкую переработку арифметики или regex без следующего профиля.

Следующий шаг:
- отдельно снять следующий предметный профиль по `array_cube_rollup.axm`, чтобы понять остаточную стоимость после снятия индексных выделений;
- затем выбрать один узкий следующий слой разгона: арифметический горячий путь массивных циклов или regex-builtins, которые всё ещё стабильно проигрывают интерпретатору.

## Session 573 - 2026-04-30 12:01 - ускорен доступ к глобальным слотам Bytecode VM

Что сделано:
- продолжен узкий разгон `BytecodeVm` после предыдущего шага по индексным массивам;
- для временного исследования использован артефактный воспроизводитель `C:\Axiom\.artifacts\repro-bytecode\Program.cs`, чтобы снять раскладку по opcode на `samples/smoke/engine-compare/array_cube_rollup.axm`;
- профиль показал, что после снятия лишних `int[]` основная масса инструкций теперь приходится на `LoadLocal`, `LoadGlobal`, переходы и простую арифметику, а built-in почти не участвуют;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` горячий путь глобальных слотов переведён на прямой индексный буфер `_globalsBySlot` и сопутствующие массивы метаданных по fixed-string длине;
- при этом карта `slot -> BytecodeGlobal` сохранена как холодная metadata-структура для целей `INPUT`, а словарь `_globals` оставлен для снимков и восстановления состояния;
- повторный сравнительный прогон по категории массивов записан в `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-120153.txt`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`
- временный артефакт `C:\Axiom\.artifacts\repro-bytecode\Program.cs`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --filter "Массивы"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-120153.txt`;
- по этому прогону паритет статуса, вывода и диагностик между `Interpreter` и `Bytecode` сохранён для `array_cube_rollup.axm` и `array_transform_chain.axm`;
- на `array_transform_chain.axm` байткод в этом прогоне быстрее интерпретатора по runtime примерно до отношения `0.684x`, а `array_cube_rollup.axm` остаётся шумным и на двух итерациях не даёт ещё устойчивого вывода о выигрыше.

Решение:
- считать этот шаг внутренней оптимизацией VM и корректировкой способа хранения глобалов, а не изменением поведения языка;
- считать важным результатом не только кодовую правку, но и уточнение профиля: после предыдущих ускорений доминируют уже базовые инструкции VM, а не built-in;
- не делать следующий широкий кодовый шаг вслепую по `array_cube_rollup.axm`, пока не будет более устойчивого замера именно этого сценария.

Следующий шаг:
- отдельно стабилизировать и повторить профиль `array_cube_rollup.axm`, чтобы уменьшить шум коротких сравнительных прогонов;
- затем выбрать следующий узкий слой оптимизации в плотных массивных циклах уже по устойчивым цифрам, а regex-ветку держать отдельным следующим направлением.

## Session 574 - 2026-04-30 13:18 - стабилизирован сравнительный прогон Bytecode VM перед следующим разгоном

Что сделано:
- в `src/Axiom.ManualChecks/Program.cs` режим `compare-engines` переведён с последовательного блочного прогона движков на междвижковое чередование измеряемых итераций с попеременной сменой стартующего движка;
- туда же добавлен отдельный параметр прогрева `--warmup`, чтобы убрать из рабочих чисел часть перекоса от холодного запуска;
- после правки последовательно выполнены `dotnet build`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, затем повторные сравнительные прогоны `compare-engines` по фильтру `Массивы` и отдельный целевой прогон `array_cube_rollup.axm`;
- целевой прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-131727.txt` зафиксировал более устойчивый ориентир для `samples/smoke/engine-compare/array_cube_rollup.axm`: runtime-отношение `bytecode/interpreter` около `1.37x` при `--iterations 8 --warmup 2`.

Изменённые файлы:
- `src/Axiom.ManualChecks/Program.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "Массивы"` проходит успешно и пишет отчёты `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-131657.txt` и `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-131712.txt`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-131727.txt`.

Решение:
- считать этот шаг не новой оптимизацией семантики VM, а стабилизацией измерительного контура для дальнейшего разгона Bytecode;
- считать рабочей опорной цифрой для следующего шага именно выделенный восьмиитерационный прогон `array_cube_rollup.axm`, а не более шумные короткие отчёты.

Следующий шаг:
- выбрать и реализовать один следующий узкий разгон горячего тракта `BytecodeVm` по стабилизированному сценарию `array_cube_rollup.axm`, в первую очередь в области базовой арифметики и смежных частых инструкций;
- после кодовой правки повторить тот же целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и сравнить изменение уже по одной и той же методике.

## Session 575 - 2026-04-30 13:25 - прямые обработчики горячих opcode проверены без убедимого выигрыша

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` из общего бинарного тракта выделены прямые обработчики `Add`, `Multiply` и `CompareGreater` для самых частых однотипных случаев `INTEGER`, `LONG`, `DOUBLE`, `DECIMAL` и строкового сложения;
- прямой путь оставляет прежний полный тракт как запасной для переполнений, неподдержанных сочетаний типов и всей уже существующей диагностики;
- после правки последовательно выполнены `dotnet build`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests` и два одинаковых целевых прогона `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёты `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-132411.txt` и `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-132425.txt`.

Решение:
- считать этот шаг полезным как уточнение профиля горячего тракта, но не считать его подтверждённым макроускорением `BytecodeVm` на массивном сценарии;
- считать новым главным сигналом то, что после выделения прямых обработчиков `Add`/`Multiply`/`CompareGreater` заметный проигрыш всё ещё остаётся, а значит следующий кандидат смещается к общей стоимости стека вычислений и операций кадра.

Следующий шаг:
- исследовать следующий узкий шаг вокруг `EvaluationStack` и частых `Push`/`Pop` в Bytecode VM, не смешивая это с отдельной regex-веткой;
- если в байткодной модели нет готовой оценки глубины стека, спроектировать минимальный способ добавить её или заменить текущий стек VM на более дешёвую внутреннюю структуру;
- после следующего шага повторить тот же целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и сравнить изменение уже по той же методике.

## Session 576 - 2026-04-30 13:29 - собственный стек Bytecode VM внедрён без подтверждённого выигрыша

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` добавлен внутренний массивный стек VM с прямыми `Push`/`Pop` и копированием содержимого снизу вверх только для снимков состояния;
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` переведён с `Stack<RuntimeValue>` на новый `BytecodeEvaluationStack`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` логика снимков состояния адаптирована к новому стеку без изменения пользовательской семантики и отладочного восстановления;
- после правки последовательно выполнены `dotnet build`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests` и целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-132903.txt`.

Решение:
- считать этот шаг корректной внутренней переработкой VM, но не считать его подтверждённым ускорением на опорном массивном сценарии;
- считать важным новым выводом, что следующий шаг нужно искать уже в более широкой стоимости операций стека и кадров, а не в одной замене контейнера.

Следующий шаг:
- отдельно исследовать и упростить связку `PopValue`/`Push`/снятие аргументов/возврат значения в Bytecode VM;
- затем повторить тот же целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и принимать решение по следующей оптимизации уже по тем же стабильным цифрам.

## Session 577 - 2026-04-30 13:43 - гипотеза про отладочные снимки снята, горячий стек слегка поджат

Что сделано:
- по коду `src/Axiom.Runtime/Execution/BytecodeVm.cs` и `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` отдельно подтверждено, что обычный запуск байткодной VM уже отделён от `StepBack` и обратимых снимков состояния: без брейкпоинтов и пошаговой отладки эта ветка не должна входить в горячий путь;
- после этого в `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` для частых `Push`/`Pop` включено агрессивное встраивание, а в `src/Axiom.Runtime/Execution/BytecodeVm.cs` часть горячих участков (`ExecuteBinaryInstruction`, прямые обработчики `Add`/`Multiply`/`CompareGreater`, `ExecutePrintUsing`, `RentArguments`) переведена на прямую работу со стеком без лишнего обхода через общий помощник `PopValue`;
- целевой прогон `array_cube_rollup.axm` повторён после этой правки.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-134247.txt`.

Решение:
- считать гипотезу про снимки отладки закрытой для обычного запуска `BytecodeVm`: она не объясняет текущий проигрыш на `array_cube_rollup.axm`;
- считать последнюю правку по `Push`/`Pop` полезным локальным уплотнением тракта, но не считать её достаточной для перелома общего результата на массивном сценарии.

Следующий шаг:
- продолжить исследование обычного тракта `BytecodeVm` вокруг частых операций кадров, аргументов и возвратов, не возвращаясь к уже закрытой гипотезе про снимки;
- затем снова повторить тот же целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и сверить эффект.

## Session 578 - 2026-04-30 13:49 - быстрый путь слотов кадров проверен на массивном сценарии

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` введён быстрый путь хранения для обычных значений без фиксированной строковой длины и без `TYPE`, чтобы слот не проходил каждый раз через общий `CopyForStorage(...)`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` путь `LoadCapture`/`StoreCapture` переведён на такой же прямой доступ к слотам, как у локалов и аргументов;
- после правки последовательно выполнены `dotnet build`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests` и целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-134859.txt`.

Решение:
- считать этот шаг ещё одной корректной локальной оптимизацией обычного тракта `BytecodeVm`, но не считать его достаточным для перелома результата на `array_cube_rollup.axm`;
- считать закрытыми ещё две узкие гипотезы: лишняя стоимость обычного хранения в слотах и лишняя стоимость косвенного доступа к захваченным слотам.

Следующий шаг:
- снять ещё один предметный профиль обычного запуска `array_cube_rollup.axm` и уже по нему решить, стоит ли следующим шагом менять создание кадров/аргументов, общую арифметическую ветку `LONG` или форму хранения локалов и аргументов шире, чем один слот.

## Session 579 - 2026-04-30 14:01 - горячие локали Bytecode VM вынесены из объектных слотов

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` локали кадра переведены на прямое хранение в `RuntimeValue[]` с отдельным хранением fixed-string метаданных, а объектные `BytecodeVariableSlot` для локалей теперь создаются лениво только когда локаль действительно нужна для захвата, снимка или словаря видимости;
- в `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` общий путь нормализации хранения вынесен в `NormalizeForStorage(...)`, чтобы и обычные слоты, и новое прямое хранение локалей использовали одну и ту же логику fixed-string и `TYPE`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` горячие `LoadLocal`/`StoreLocal`, восстановление снимков состояния и построение видимых локальных переменных адаптированы к новой модели хранения; в `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs` локальные цели `INPUT` переведены на новую размерность кадра;
- после правки повторены `dotnet build`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, затем целевой прогон `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и дополнительный opcode-профиль через `.artifacts/repro-bytecode`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-140050.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` проходит успешно и подтверждает, что по частотам всё ещё доминируют `LoadLocal`, `Add`, `CompareGreater`, переходы и операции массива.

Решение:
- считать этот шаг подтверждённым локальным ускорением обычного тракта `BytecodeVm`: на `array_cube_rollup.axm` runtime-отношение `bytecode/interpreter` улучшилось примерно до `1.52x`, то есть разрыв сократился по сравнению с предыдущим шагом;
- считать подтверждённой архитектурную идею отделения горячего хранения локалей от холодных слотов наблюдаемости: захваты и снимки сохранены, но обычный цикл больше не платит за объектный слот на каждом обращении к локали.

Следующий шаг:
- продолжить разгон следующего самого частого слоя обычного тракта `BytecodeVm` на `array_cube_rollup.axm`, в первую очередь вокруг арифметической ветки `LONG`, `Convert` и смежных переходов цикла;
- повторять сравнение по той же методике `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"`, чтобы держать решение на одной и той же измерительной базе.

## Session 580 - 2026-04-30 14:48 - шаблон `Convert(Long) + Add` сжат до внутреннего opcode

Что сделано:
- в `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` добавлен узкий peephole: lowered-последовательность `Convert(Long)` сразу перед бинарным `+` теперь сжимается в новый внутренний opcode `AddLongInteger`;
- в `src/Axiom.Bytecode/Model/Opcode.cs`, `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` и `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` добавлена поддержка нового opcode, включая проверку стека `Long + Integer -> Long` и расчёт его влияния на глубину вычислительного стека;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлено прямое выполнение `AddLongInteger` без отдельной инструкции `Convert`;
- в `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs` и `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs` добавлены проверки на сжатие шаблона и на корректность/некорректность стека нового opcode;
- после правки повторены `dotnet build`, `Axiom.Bytecode.Tests`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, затем целевой прогон `compare-engines` и opcode-профиль через `.artifacts/repro-bytecode`.

Изменённые файлы:
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` проходит успешно, 184/184;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-144745.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` проходит успешно и показывает сокращение общего числа инструкций до `489502`, исчезновение `Convert` из горячего списка и появление `AddLongInteger: 8640`.

Решение:
- считать этот шаг корректным внутренним уплотнением байткода: он убрал отдельную частую инструкцию преобразования и подтвердил это профилем, даже если по одному сравнительному прогону wall-clock выигрыш пока не отделяется от шума так же ясно, как в шаге с локалями;
- считать закрытой гипотезу про именно этот частый шаблон `INTEGER -> LONG -> Add`: теперь он уже не идёт через две отдельные инструкции VM.

Следующий шаг:
- идти в следующий слой лидеров `array_cube_rollup.axm`, в первую очередь вокруг сравнений и переходов цикла (`CompareGreater`, `JumpIfFalse`, `JumpIfTrue`, `ValidateForStep`);
- продолжать сверять не только `compare-engines`, но и opcode-профиль, чтобы следующий шаг снова давал не догадку, а проверяемое структурное изменение.

## Session 581 - 2026-04-30 15:07 - сравнение и переход цикла сжаты в прямые opcode ветвления

Что сделано:
- в `src/Axiom.Bytecode/Model/Opcode.cs` добавлены внутренние opcode `JumpIfLess`, `JumpIfLessOrEqual`, `JumpIfGreater`, `JumpIfGreaterOrEqual`, а в `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` описано их влияние на стек;
- в `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` добавлен peephole, который сжимает lowered-пары `Compare...` + `JumpIfTrue/JumpIfFalse` в прямое ветвление, чтобы убрать промежуточный булев результат;
- в `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` добавлены проверки операндов и графа переходов для новых opcode, а в `src/Axiom.Runtime/Execution/BytecodeVm.cs` реализовано их прямое выполнение по двум верхним значениям стека;
- в `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs` и `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs` добавлены проверки на сжатие шаблона и на корректность/ошибочность новых opcode;
- после первой попытки исправлен точечный дефект peephole-компилятора: доступ к `index + 1` теперь не выполняется на последней lowered-инструкции.

Изменённые файлы:
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` проходит успешно, 187/187;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-150258.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` проходит успешно и показывает сокращение общего числа инструкций до `451914`, исчезновение отдельных `CompareGreater` из горячего списка и появление прямых `JumpIfLessOrEqual: 18794` и `JumpIfGreater: 18794`.

Решение:
- считать этот шаг подтверждённым внутренним уплотнением горячего цикла Bytecode VM: отдельный булев результат сравнения больше не создаётся там, где он сразу же используется только для перехода;
- считать тему «сравнение плюс немедленный переход» закрытой для текущего опорного сценария, потому что она уже перенесена в прямые opcode ветвления и подтверждена opcode-профилем;
- зафиксировать, что по wall-clock `array_cube_rollup.axm` байткод пока всё ещё остаётся примерно на `1.52x` к интерпретатору, то есть следующий выигрыш нужно искать уже в оставшихся лидерах трассы, а не в этой связке.

Следующий шаг:
- продолжить разгон следующего плотного слоя `array_cube_rollup.axm`, в первую очередь вокруг `LoadLocal`, обычного `Add` и смежных операций массива;
- повторять сравнение по той же методике `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и обязательно сверять его вместе с opcode-профилем, чтобы отличать структурный прогресс от шума замеров.
## Session 582 - 2026-04-30 15:12 - доверенные доступы VM вынесены в горячий путь выполнения

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` обычный путь `ExecuteCurrentInstruction()` для `PushConst`, `LoadGlobal`, `StoreGlobal`, `LoadLocal`, `StoreLocal`, `LoadArgument`, `StoreArgument`, `LoadCapture` и `StoreCapture` переведён на прямой доступ к уже валидированным слотам и константам без повторной проверки границ на каждой инструкции;
- для глобалов и констант добавлены `ReadConstantUnchecked`, `LoadGlobalUnchecked` и `StoreGlobalUnchecked`, а локали, аргументы и захваты в горячем тракте теперь читаются и пишутся напрямую через структуры кадра;
- защитные методы с диагностикой оставлены в классе как недоверенный путь, но выведены из частого исполнения;
- после правки повторены `dotnet build`, `Axiom.Bytecode.Tests`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, целевой прогон `compare-engines` и opcode-профиль через `.artifacts/repro-bytecode`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` проходит успешно, 187/187;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-151128.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` проходит успешно и показывает ту же форму горячего профиля при общем `Instructions: 451914`.

Решение:
- считать подтверждённой гипотезу, что даже после валидации байткод-модуля горячий путь VM ещё платил заметную цену за повторные проверки доступа к слотам и константам;
- считать этот слой оптимизации закрытым для текущего сценария: доверенный путь уже отделён, а дальнейший заметный выигрыш теперь вероятнее искать в арифметике и операциях массива, а не в проверках индексов.

Следующий шаг:
- продолжить разгон `array_cube_rollup.axm` по оставшимся лидерам трассы, в первую очередь вокруг обычного `Add`, смежной `LONG`-арифметики и операций `LoadElement`/`StoreElement`;
- продолжать сверять тот же сценарий `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` вместе с opcode-профилем из `.artifacts/repro-bytecode`.
## Session 583 - 2026-04-30 15:18 - полный доступ к элементу массива больше не дублирует проверку границ

Что сделано:
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` добавлены внутренние методы `TryGetValue(...)` и `TrySetValue(...)`, которые за один проход проверяют полный набор индексов и сразу вычисляют линейный индекс;
- там же добавлено кэширование базового смещения фиксированных измерений представления массива, чтобы не пересчитывать уже зафиксированные координаты на каждом полном чтении и присваивании;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` прямой путь полного `LoadElement` и `StoreElement` переведён на эти методы, так что VM больше не делает отдельную проверку границ перед повторной проверкой внутри `RuntimeArrayValue`;
- после правки повторены `dotnet build`, `Axiom.Bytecode.Tests`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, два прогона `compare-engines` и opcode-профиль через `.artifacts/repro-bytecode`.

Изменённые файлы:
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` проходит успешно, 0 ошибок и 0 предупреждений;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` проходит успешно, 187/187;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` проходит успешно, 521/521;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` проходит успешно, 666/666;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` проходит успешно и пишет отчёты `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-151813.txt` и `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-151842.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` проходит успешно и показывает тот же общий профиль с `Instructions: 451914`.

Решение:
- считать подтверждённым структурное упрощение полного индексного доступа массива в VM: двойная проверка границ между `BytecodeVm` и `RuntimeArrayValue` снята;
- при этом считать wall-clock-эффект умеренным и шумным: один прогон дал около `1.599x`, второй вернулся примерно к `1.441x`, поэтому этот шаг не стоит трактовать как новый крупный перелом по времени выполнения;
- считать следующий основной резерв производительности смещённым дальше, в арифметику и оставшуюся общую стоимость массивных операций.

Следующий шаг:
- продолжить разгон `array_cube_rollup.axm` по оставшимся лидерам профиля, прежде всего вокруг обычного `Add`, `Multiply` и общей стоимости `LoadElement`/`StoreElement`;
- продолжать сверять те же сравнительные прогоны вместе с opcode-профилем, чтобы отфильтровывать шум замеров.
## Session 584 - 2026-04-30 15:26 - горячая арифметика Bytecode VM переведена на прямые ветки типов

Контекст:
- продолжен разгон `BytecodeVm` на опорном сценарии `samples/smoke/engine-compare/array_cube_rollup.axm`;
- предыдущий шаг упростил полный доступ к элементам массива, но wall-clock оставался шумным около `1.44x`-`1.60x` по runtime-отношению `bytecode/interpreter`;
- профиль после предыдущего шага оставался `Instructions: 451914`, а среди лидеров были `Add`, `Multiply`, `LoadElement`, `StoreElement`, `LoadLocal` и переходы цикла.

Что изменено:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` методы `TryEvaluateHotAdd(...)` и `TryEvaluateHotMultiply(...)` переписаны с общего `switch` на прямые ветки типов для `INTEGER`, `LONG`, `DOUBLE`, `DECIMAL` и строкового сложения;
- сохранён прежний полный путь диагностики: при переполнении или неподдержанном типе быстрый путь возвращает `false`, после чего VM использует обычное вычисление;
- в `src/Axiom.Runtime/Values/RuntimeValue.cs` добавлен кэш малых `LONG`-значений от `-1` до `1024`, чтобы уменьшить число короткоживущих объектов в плотных циклах и при начальных значениях.

Проверки:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - успешно, 0 предупреждений, 0 ошибок;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 187 тестов;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 666 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-152537.txt`, runtime ratio `1.221x`, total ratio `1.219x`;
- повторный `compare-engines` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-152606.txt`, runtime ratio `1.322x`, total ratio `1.317x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - успешно, `Instructions: 451914`, профиль команд не изменился.

Решения:
- считать шаг внутренней оптимизацией исполнения, не изменяющей язык, пользовательские документы руководства и `.axm`-примеры;
- зафиксировать, что текущий выигрыш получен без изменения формы байткода, за счёт удешевления уже существующих горячих операций.

Что остаётся:
- байткодная VM всё ещё медленнее интерпретатора на `array_cube_rollup.axm`, но разрыв в свежих прогонах сузился примерно до `1.22x`-`1.32x`;
- следующий кандидат для разгона: оставшиеся лидеры профиля `LoadLocal`, `PushConst`, `Jump`, `LoadGlobal`, `LoadElement`, `StoreElement` и возможное сжатие типовых цикловых последовательностей.

Точный следующий шаг:
- проверить, что выгоднее следующим: специализированный путь цикла со счётчиком или дальнейшее удешевление `LoadElement`/`StoreElement`, и снова сверять результат по тем же `compare-engines` и opcode-профилю.
## Session 585 - 2026-04-30 15:39 - горячий путь StoreElement больше не выполняет повторную запись массива

Контекст:
- продолжен разгон `BytecodeVm` на `samples/smoke/engine-compare/array_cube_rollup.axm`;
- после Session 584 профиль оставался `Instructions: 451914`, а среди лидеров были `LoadElement: 17301` и `StoreElement: 17298`;
- следующий шаг по плану предполагал проверить дальнейшее удешевление полного индексирования массива или цикловых шаблонов.

Что изменено:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` удалён повторный `array.SetValue(indices, value)` после успешного `array.TrySetValue(indices, value)`;
- `TrySetValue(...)` уже проверяет границы, вычисляет линейный индекс, выполняет `CopyForStorage(...)` и записывает элемент, поэтому второй вызов был лишней повторной работой в горячем `StoreElement`;
- в горячем полном индексировании `PopulateArrayIndices(...)`, `ExecuteDirectLoadElement(...)` и `ExecuteDirectStoreElement(...)` теперь используют прямой `EvaluationStack.Pop()` вместо `PopValue(...)`, потому что форма стека гарантируется валидированным байткодом;
- `ToArrayBound(...)` переписан на прямые проверки типа с быстрым первым случаем `INTEGER`.

Проверки:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - успешно, 0 предупреждений, 0 ошибок;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 187 тестов;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 666 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-153517.txt`, runtime ratio `1.284x`, total ratio `1.279x`;
- повторный `compare-engines` после удаления повторной записи и прямого снятия индексов - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-153830.txt`, runtime ratio `1.193x`, total ratio `1.191x`;
- контрольный `compare-engines` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-153902.txt`, runtime ratio `1.304x`, total ratio `1.305x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - успешно, `Instructions: 451914`, профиль команд не изменился.

Решения:
- считать правку внутренней оптимизацией VM без изменения языка, руководства и `.axm`-примеров;
- считать подтверждённым, что часть стоимости `StoreElement` была не только в проверке границ, но и в лишней повторной записи после уже успешного `TrySetValue(...)`.

Что остаётся:
- байткодная VM всё ещё медленнее интерпретатора на этом сценарии, но свежие прогоны находятся ближе к паритету, примерно в диапазоне `1.19x`-`1.30x`;
- оставшиеся лидеры профиля по-прежнему `LoadLocal`, `PushConst`, `Jump`, `LoadGlobal`, `LoadElement`, `StoreElement`, `Add` и `AddLongInteger`.

Точный следующий шаг:
- проверить специализированный путь полного индексирования для частых рангов `1` и `3` или альтернативно цикловой шаблон счётчика, выбирая тот вариант, который даст измеримый выигрыш без смешивания с изменениями поведения языка.
## Session 586 - 2026-04-30 15:46 - полный доступ к массивам ранга 1 и 3 получил специализированный путь

Контекст:
- продолжен разгон `BytecodeVm` на опорном сценарии `samples/smoke/engine-compare/array_cube_rollup.axm`;
- после Session 585 профиль оставался `Instructions: 451914`, а среди горячих операций были `LoadElement: 17301` и `StoreElement: 17298`;
- следующий шаг по плану предполагал проверить быстрый путь для частых рангов `1` и `3` без изменения поведения языка.

Что изменено:
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` добавлены внутренние методы `TryGetValue1`, `TrySetValue1`, `TryGetValue3` и `TrySetValue3`;
- эти методы выполняют проверку границ и расчёт линейного индекса без общего цикла по измерениям, но сохраняют поддержку представлений через уже существующие отображения видимых измерений в хранилище;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` обычные `LoadElement` и `StoreElement` без срезов переводятся на специализированные ветки для `array(i)` и `array(i, j, k)`;
- если полный доступ фактически оказывается ведущим индексом для массива большего ранга, чтение возвращается к прежнему универсальному пути создания представления.

Проверки:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - успешно, 0 предупреждений, 0 ошибок;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 187 тестов;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 666 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-154548.txt`, runtime ratio `1.090x`, total ratio `1.090x`;
- повторный `compare-engines` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-154607.txt`, runtime ratio `1.266x`, total ratio `1.260x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - успешно, `Instructions: 451914`, профиль команд не изменился.

Решения:
- считать шаг внутренней оптимизацией исполнения VM без изменения языка, справочника и runnable `.axm`-примеров;
- считать гипотезу о стоимости полного индексирования частично подтверждённой: один прогон приблизился к паритету `1.090x`, второй остался шумным `1.266x`, но оба находятся в лучшей зоне, чем ранние прогоны до удешевления массивного пути;
- не трактовать этот шаг как окончательную победу над интерпретатором: байткодная VM всё ещё может проигрывать на отдельных прогонах.

Что остаётся:
- горячий профиль по-прежнему возглавляют `LoadLocal`, `PushConst`, `Jump`, `LoadGlobal`, `Add`, `LoadElement`, `StoreElement` и `AddLongInteger`;
- следующий кандидат для узкой проверки: стоимость типового `FOR/NEXT`-цикла, локальных загрузок и констант, а не новые языковые возможности.

Точный следующий шаг:
- проверить, можно ли безопасно удешевить `LoadLocal`/`PushConst` или сжать типовой цикл `FOR/NEXT`, затем снова прогнать `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` и opcode-профиль.
Дополнение после финальной чистки:
- после удаления дублирования в `ExecuteDirectLoadElement(...)` повторно пройдены `dotnet build`, `Axiom.Bytecode.Tests`, `Axiom.Runtime.Tests` и `Axiom.Execution.Tests`;
- финальный целевой `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` прошёл успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-154918.txt`, runtime ratio `1.228x`, total ratio `1.188x`;
- финальный профиль `.artifacts/repro-bytecode` снова подтвердил `Instructions: 451914` и тот же набор горячих opcode.

## Session 587 - 2026-04-30 17:32 - локальный FOR/NEXT получил сжатые opcode и вывел байткод вперёд

Контекст:
- продолжен разгон `BytecodeVm` на опорном сценарии `samples/smoke/engine-compare/array_cube_rollup.axm`;
- после Session 586 профиль всё ещё оставался `Instructions: 451914`, а в плане следующим кандидатом были типовой `FOR/NEXT` или оставшиеся массовые `LoadLocal` и `PushConst`;
- пользователь отдельно подсветил гипотезу, что байткодному режиму важно продолжать отделять обычный запуск от любой лишней отладочной нагрузки.

Что изменено:
- в `src/Axiom.Bytecode/Model/Opcode.cs` и `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` добавлены внутренние opcode `JumpIfForDoneLocal` и `AdvanceForLoopLocal` вместе с их стековым контрактом;
- в `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` добавлено распознавание типового локального шаблона `FOR/NEXT`: проверка завершения и шаг счётчика теперь сжимаются при эмиссии в отдельные команды, если цикл работает по локальным слотам;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` новый путь выполняет проверку завершения цикла и инкремент напрямую по слотам кадра с быстрыми типовыми ветками и с сохранением fallback-пути для общей арифметики и сравнения;
- в `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` добавлена валидация операндов и типов для новых opcode, а в `tests/Axiom.Bytecode.Tests` добавлены проверки на сжатие и на типовые ошибки валидатора.

Проверки:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - успешно;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 191 тест;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 666 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-172907.txt`, runtime ratio `0.765x`, total ratio `0.767x`;
- повторный `compare-engines` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-172939.txt`, runtime ratio `0.921x`, total ratio `0.920x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - успешно, профиль `Instructions: 285800`, среди лидеров `LoadLocal`, `LoadGlobal`, `PushConst`, `JumpIfForDoneLocal`, `AdvanceForLoopLocal`, `Jump`, `LoadElement`, `StoreElement`, `Add`.

Решения:
- считать шаг внутренней оптимизацией VM и эмиттера без изменения языка, справочника и runnable `.axm`-примеров;
- считать подтверждённым, что именно структурное сжатие типового локального цикла способно перевести байткодный режим из состояния «почти паритет» в состояние «уже быстрее интерпретатора» на контрольном сценарии.

Что остаётся:
- после сжатия цикла главными кандидатами на следующий выигрыш остаются `LoadLocal`, `LoadGlobal`, `PushConst`, `LoadElement`, `StoreElement` и обычный `Add`;
- нужно отдельно удержать новое преимущество в следующих сериях замеров и не спутать его с шумом конкретного прогона.

Точный следующий шаг:
- продолжить разгон по оставшимся лидерам профиля, начиная с `LoadLocal`/`PushConst` и прямых чтений локалей, затем повторять те же `compare-engines` и opcode-профиль для подтверждения устойчивости выигрыша.
## Session 588 - 2026-04-30 17:40 - доступ к операндам инструкции и горячим слотам VM упрощён без смены байткода

Контекст:
- после Session 587 байткодный движок уже вышел вперёд на `array_cube_rollup.axm`, но профиль команд оставался прежним лидером по массовым операциям `LoadLocal`, `LoadGlobal` и `PushConst`;
- следующий шаг был выбран как узкая внутренняя оптимизация без новой формы opcode: снять лишнюю стоимость доступа к операндам инструкции и к самым частым чтениям кадра.

Что изменено:
- в `src/Axiom.Bytecode/Model/BytecodeInstruction.cs` инструкция переведена на внутреннее хранение операндов в `int[]`, а `GetOperand(...)` теперь читает их напрямую;
- в `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` методы `LoadLocalValue(...)` и `StoreLocalValue(...)` помечены как `AggressiveInlining`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` тем же образом помечены `ReadConstantUnchecked(...)`, `LoadGlobalUnchecked(...)` и `StoreGlobalUnchecked(...)`, чтобы упростить частые проходы через основной цикл исполнения.

Проверки:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - успешно;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 191 тест;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 666 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-174010.txt`, runtime ratio `0.861x`, total ratio `0.868x`;
- повторный `compare-engines` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-174027.txt`, runtime ratio `0.853x`, total ratio `0.853x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - успешно, профиль остался `Instructions: 285800`.

Решения:
- считать шаг чистым удешевлением горячего чтения данных VM и инструкции без изменения формы байткода и без изменения поведения языка;
- считать подтверждённым, что даже после структурного сжатия `FOR/NEXT` в системе остаётся измеримый выигрыш от локального упрощения модели инструкции и прямых служебных чтений.

Что остаётся:
- главные лидеры профиля всё ещё `LoadLocal`, `LoadGlobal`, `PushConst`, `LoadElement`, `StoreElement` и `Add`;
- следующий выигрыш уже, скорее всего, потребует более прицельного упрощения массовых веток исполнения, а не только общей инлайн-разметки.

Точный следующий шаг:
- продолжить с узкой проверки `LoadLocal` и `PushConst` в основном switch VM, затем снова прогонять `compare-engines` и профиль, чтобы увидеть, даёт ли это следующий измеримый слой выигрыша.

## Session 589 - 2026-04-30 18:21 - ??????? ?????????? ?????? ?????? ?? ???????? ????? ?????? ??? ?????????????

????????:
- ????? Session 588 ?????????? ?????? ??? ????????? ????????? ? ??????????????, ?? ???????????????? ???????? ? ?????? ???? ??????? ? ?????????? ?????? ? ??????? ??????? ?????????? ???? ?????????????;
- ??????? `array_cube_rollup.axm` ????? ?????????? ????? ???????? ?? `Instructions: 285800`, ??? ??? ????????? ??? ??????? ???? ?????? ?? ? ????? ????????, ? ? ????? ??????????? ????? ??????????.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ???????? `ExecuteCurrentInstruction()` ??????? ?? ??? ??????: ??????? ???? ??? ???????? ??????? ? ?????? ???? ??? ??????? ? ?????????????;
- ???? ? ??????? ??? ??????????? ??????????, ??????? ?????????, ????? ????????, ?????????? ????????? ???????????? ? ??????? ???????, VM ?????? ?? ????????? `NeedsExecutionSnapshot()`, ?? ?????? `BytecodeVmExecutionSnapshot`, ?? ?????? reversible-state-?????? ? ?? ???????? `ShouldStopBefore(...)`;
- ??? `switch` ?????????? ??????? ? ????????? ?????????? ?????, ????? ??? ?????? ???????????? ???? ? ??? ?? ??? ?????????? opcode ??? ???????????? ?????????.

????????:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 191 ????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-182024.txt`, runtime ratio `0.824x`, total ratio `0.831x`;
- ????????? `compare-engines` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-182040.txt`, runtime ratio `0.846x`, total ratio `0.843x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`.

???????:
- ??????? ???????? ? ???? ?????? ?????????? ??????? ? ??????? ?????????? ??????? ?????????????;
- ??????? ??? ?????????? ????????????? ??????????? ???????? ?????????? ? ??????? ??? ????????? ?????, ??????????? ? runnable `.axm`-????????.

??? ???????:
- ????? ????????? ??????? ? ?????? ????????? ??????? ?????? ??????? ?? ??? `LoadLocal`, `LoadGlobal`, `PushConst`, `LoadElement`, `StoreElement` ? `Add`;
- ????????? ??????? ?????? ??? ????? ????????? ???? ?????? ? ????? ???????? opcode, ? ?? ? ????? ???????? ????? ??????????.

?????? ????????? ???:
- ?????????? ? ??????????? ????????? `LoadLocal` ? `PushConst` ?????? ??? ??????????? ???????? ???? VM, ????? ????????? ?? ?? ????????????? ??????? ? ???????.

## Session 590 - 2026-04-30 18:25 - ??????? ?????? VM ?????????? ?? ?????? ???? ??? shared-slot ????????

????????:
- ????? Session 589 ??????? ?????????? ?????? ??? ?? ?????? ????? ?? ?????? ? ?????????? ???????, ?? ??????? ??????? ??????? ????????? `LoadLocal`;
- ? ??????? ???????? `array_cube_rollup.axm` ?????? ?? ??????? ????????? ????????????, ??????? ????????? ????? ???? ??? ?????? ?????? ? `BytecodeCallFrame`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` ???????? ???? ??????? shared-???????;
- `LoadLocalValue(...)` ? `StoreLocalValue(...)` ?????? ???? ???????? ????? `_locals`, ???? ? ????? ?? ?????? ?? ???? shared-slot;
- ??? ?????? `GetOrCreateLocalSlot(...)` ????????????? ????????????? shared-slot, ???? ???????????? ? ???????? ?????????????? ???? ? ??????????? ???? ????????? ???????? ? ???????? ?????????????.

????????:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 191 ????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-182500.txt`, runtime ratio `0.801x`, total ratio `0.803x`;
- ????????? `compare-engines` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-182518.txt`, runtime ratio `0.764x`, total ratio `0.769x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`.

???????:
- ??????? ??? ?????????? ???????????? ???????? ?????????? ??????? ??? ????????? ????? ???????? ? ??? ????????? ????????? shared-???????;
- ??????? ?????????????, ??? ???? ????? ????????? ??????? ? ??????? ? ??????? ??????? ?????????? ???????? ???? ????????????? ????? shared-slot.

??? ???????:
- ??????? ?????? ??????? ?? ??? `LoadLocal`, `LoadGlobal`, `PushConst`, `LoadElement`, `StoreElement` ? `Add`;
- ????????? ??????? ???????? ????? ?????? ? `PushConst` ? ? ?????????? ????????? ?????? ???????? ?????? ??????? ?? ?????? ????????? switch VM.

?????? ????????? ???:
- ?????????? ? `PushConst` ? ????????? ???????? ???????? ??????/????????, ????? ????????? ?? ?? ????????????? ??????? ? ???????.

## Session 591 - 2026-04-30 19:31 - ???????? 0 ? 1 ???????? ????????? ?????????? opcode

????????:
- ????? Session 590 ??????? ?????????? ???? ??? ??????? ??????? ?????????????, ?? ? ??????? `array_cube_rollup.axm` ????? ??????? ????????? `PushConst`;
- ????????? ?????????? ???????? ???? ?? ? ?????????? ?????, ? ? ??????????? ????? ?????? ????????????? ????????? `0` ? `1` ?????? ????????????? ????????.

??? ????????:
- ? `src/Axiom.Bytecode/Model/Opcode.cs` ? `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` ????????? ?????????? opcode `PushIntegerZero` ? `PushIntegerOne` ? ??? ?? ???????? ??????????, ??? ? ? `PushConst`;
- ? `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` `EmitLoadLiteral(...)` ?????? ?????????????? ????????????? ???????? `0` ? `1` ? ??? opcode, ?? ????? ???????????????? ????????? ?????;
- ? `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ????????? ?? ????????? ? ?????? ?????????? ??? ????????? ? ???? ????????;
- ? `tests/Axiom.Bytecode.Tests` ? `tests/Axiom.Runtime.Tests` ????????? ???????? ????? ???????? ? ??????? ?????????? ??? ????? ?????????? ?????.

????????:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj` - ???????, 194 ?????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-193057.txt`, runtime ratio `0.719x`, total ratio `0.722x`;
- ????????? `compare-engines` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-193112.txt`, runtime ratio `0.782x`, total ratio `0.785x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`, ??? ???? `PushConst` ???????? ?? `18065`, ? `PushIntegerOne` ???????? ? ???????? `1522`.

???????:
- ??????? `PushIntegerZero` ? `PushIntegerOne` ?????????? ???????????? ??????? ?????????, ? ?? ????? ???????????????? ???????????? ?????;
- ??????? ?????????????, ??? ????? ???? `PushConst` ????????????? ?????? ? ???????????? ????? ?????? ????? ????? ????????.

??? ???????:
- ??????? ??????? ??????? ?? ??? ??????? `LoadLocal`, ????? ???? `LoadGlobal`, `PushConst`, `JumpIfForDoneLocal`, `AdvanceForLoopLocal`, `LoadElement`, `Add` ? `StoreElement`;
- ????????? ??????? ??? ???????? ?????? ???? ? ?????????? ?????? `PushConst`, ???? ? `LoadGlobal`, ? ?? ????????? ??? ????????? ????????????? `0` ? `1`.

?????? ????????? ???:
- ??????????, ????? ?????? ???????? ???????? ? `PushConst` ????? ?????? `0` ? `1`, ? ??????, ???????? ?? ??????? ????????? ??? ????????? ?????? ????? ????? ???? ?????????? ? ?????????? ????????? `LoadGlobal`.

## Session 592 - 2026-04-30 21:50 - ????? top-level ???????? ?????????? ?? ????????? ????? entry routine

????????:
- ????? Session 591 ????? ????, ??? ?????????? ????????????? `PushConst` ?? ????? ????? `2`, `-1` ? `10` ??????? ???????????? ? ???????? ??? ???? benchmark;
- ???????????? ???????? ??????????, ??? ????????? ??? ????? ????? ?? ?? ??????? ?????????, ? ?? ?????? ????????? `LoadGlobal`, ??????? ????????? ??????? ??????? ???????.

??? ????????:
- ? `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` ???????? ?????????? ?????? lowered-?????????, ??????? ??????? top-level ???????, ???????????? ?????? ? `LoweredProgram.EntryPoint`;
- ????? ??????? ?????? ???????? ????????? ????? entry routine ? ????????? ??? `LoadLocal`/`StoreLocal`, ? ?? ??? `LoadGlobal`/`StoreGlobal`;
- ???? ??????? ???????????? ???? ?? ? ????? ?????? ?????????, optimization ?? ??????????? ? ??????? ????????? ??????? ?????????? ????;
- ??? metadata ????????? ?????? ???????? ?????????? ???????????? ??????? ??????????, ????? ?? ?????? ???????? ?????????? bytecode-??????;
- ? `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs` ????????? ???????? ?? ??? ????: ????? lift ???????? ? ????? ??????? ??????? ???????? ??????????.

????????? ?????:
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-214947.txt`, runtime ratio `0.752x`, total ratio `0.754x`;
- ????????? `compare-engines` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-214958.txt`, runtime ratio `0.625x`, total ratio `0.632x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ?????????? ?? `Instructions: 285800`, ? `LoadGlobal` ????? ?? ???????? opcode-??????.

???????:
- ??????? ???????? ?? `LoadGlobal` ???????? ?? ??????????????????? ????? ??????????, ? ????? ????? ??????? ?????? ?????????? ?????? ??? top-level ????????;
- ??????? ??? ?????? ?????????? ???????????? backend/VM ??? ????????? ???????????????? ????????? ?????, ??????????? ? runnable `.axm`-????????;
- ????????? ????????? ????????? ????? ???????? ?????? ????? ???? ??????? ? `.artifacts/git-backups/`.

??? ???????:
- ????? ?????? `LoadGlobal` ??????? ??????????? ??????? ??????? ???? `LoadLocal`, ? ?????????? ????????? ??????????? ???????? `PushConst`, `LoadElement`, `Add` ? `StoreElement`;
- ?????? ?????????? ???????? ???????? ?????? ??? ?? ? ????????, ? ? ????????? ? ????????? ??????? ????.

?????? ????????? ???:
- ????????? ?????????, ????? ?? ??? ????????? ????????? `LoadLocal` ? ??????? ???? VM ??? ????????? ????? ????????;
- ???? ??????? ??? ???????? ?????, ????????????? ?? `LoadElement` ??? ?? ????????? ??????? ????? ???????? ???????.

## Session 593 - 2026-04-30 22:08 - ??????? 3D-????? LoadElement ????????? ? ????????? ??? ??????????????

????????:
- ????? Session 592 ????????? ??????? ?????????? ??????? ????????? `LoadElement`, ? ??????? benchmark `samples/smoke/engine-compare/array_cube_rollup.axm` ????????? ?????? ????????? ?????? ? ???????;
- ??????? ???? ????????? ???????? ? ????? ??????? ??????? ???? ??? ??? ??????????? ????????? ??????? ????? `3`, ????? ??????, ????? ?? ???????????? ???? ? ????? ??????? ?????? ?????? ????????.

??? ????????:
- ? ???? ???? ??????? ? ????????? ????? ??????? 3D-????? ??? `LoadElement`/`StoreElement`, ?? ????? ????? ??????? ??? ?? ???????? ??????????? ?????? ????????;
- ?? ?????? ????????? ??? ????? ????????? ???????? ? ? ??????? ?????? ?? ?????????;
- ??????? ???? ??????? ? ????????? Session 592 ???? ??? ???????????? ????????? ??????????? `LoadLocal` ?? ??????? ??????.

????????:
- `dotnet build C:\Axiom\Axiom.sln --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- ?? ????? ???????? ???????? ???? ????? ?????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-220449.txt`, `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-220450.txt` ? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-220517.txt`;
- ?????? ????????? ??????? ? ?? ??????????? ??????????? ?????? ????, ??????? ??????????? ?? ???????.

???????:
- ??????? ???????? ? ???????? ???????? ?? ?????????? ???????? ???? ??? ????????? 3D-??????? ???????????????;
- ?? ????????? `BytecodeVm` ????? ??????? ?????? ????? ??????? ??? ??????????? ??????? ?? ??????????? ????????;
- ???????????? ???? ????????? ??? ????????????? ???????? ? ???????? ?????????? ????? ?????? ? ????????? ????? ????????? ??????????????.

??? ???????:
- `LoadElement` ? `StoreElement` ?? ??? ???????? ????????? opcode ? ??????? ???????;
- ??????? ???????? ???????? ??? ?? ??? ?????????? ????????? ???????? ?????, ? ??? ???? ?????? ???????? ?? ?????, `ToArrayBound(...)` ? ????? ????????? ???????.

?????? ????????? ???:
- ????????? ???? `LoadElement` ?? ?????????????? ???????? ? ?????????? ?????? ? ???????, ????? ?????????, ????? ?? ????????? ????????? `ToArrayBound(...)` ? ???????? `StoreElement` ??? ????? ???????? ?????????????;
- ???? ???? ???? ???? ?? ???? ?????????? ????????, ?????????? ? ?????????? ?????? ????????? ???????, ? ?? ?????????? ??????? ??????? ????? ?? 2D/3D-???????.

## Session 594 - 2026-04-30 22:13 - ???????? ????? integer-???? ???????? ???????

????????:
- ????? Session 593 ????? ????, ??? ????? ??????? 3D-????? ?? ??? ?????????? ????????? ???????? ? ?????????? ????? ???? ????? ? ????? ????????? ????;
- ????????? ???????? ???? ?????????? ?? ?? ????? ????????????? ?????????????, ? ?? ????????? ?????????????? ???????? ??????? ? `ToArrayBound(...)` ? ?? ???????? helper-?????? `RuntimeArrayValue`, ??????? ??????? ?????????? ?? `LoadElement` ? `StoreElement`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` `ToArrayBound(...)` ??????? ????? ??????? ??????? ???? ??? ?????? ??????? `INTEGER`, `LONG`, `DOUBLE` ? `DECIMAL`, ????? ???????? ??? ????? ? ?????????? ????????? `Int32`;
- ? `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` ?? ???????? ?????? `TryGetValue(...)`, `TryGetValue1(...)`, `TryGetValue3(...)`, `TrySetValue(...)`, `TrySetValue1(...)`, `TrySetValue3(...)`, `TryGetLinearIndex(...)`, `TryGetLinearIndex1(...)` ? `TryGetLinearIndex3(...)` ???????? `AggressiveInlining`;
- ?????? ????????? ??????? ???????????????? ????????? ???????? ? ?? ?????? ????? opcode ??? ????? ?????? ??? 2D/3D-????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-221210.txt`, runtime ratio `0.580x`, total ratio `0.589x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 12 --warmup 3 --filter "array_cube_rollup.axm"` - ???????, ?????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-221211.txt` ? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-221243.txt`, runtime ratio `0.792x` ? `0.757x`, total ratio `0.797x` ? `0.761x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`, ?????? ?? ??????? ?? ??????????;
- ?????? `dotnet build C:\Axiom\Axiom.sln --no-restore` ? ???? ?????? ???????? ? ???????? ??? ??? ??????????? `Axiom.Ide.exe`, ??????? ?????? `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Runtime.dll`.

???????:
- ??????? ?????? ????????????? ????? ???????????? integer-???????? ???????, ? ?? ????? ??????? ???????????? ?????? ??? 3D benchmark;
- ??????? ????? ???????? ?????????? ?????????? ????? ? ????? ????????? ? ?????????????? ??????? ????, ? ?? ? ??????????? ????????? ????? ??? 2D/3D-?????;
- ??????? ?????????? ??????? solution-build ??????? ?????????? ?????????? WPF-????????, ? ?? ????????? ???????? ?????????.

??? ???????:
- `LoadElement` ? `StoreElement` ?? ??? ???????? ? ???????? ??????? ?? ????? ??????????;
- ??????? ???? ??????????, ??? ?? ????????? ???? ?????? ????????, ?? ??? ?? ????? ???? ????? ????????? ? ????????.

?????? ????????? ???:
- ?????????? ? ????? ?????????? `LoadElement` ? `StoreElement`: ???????? ????????? ?????? ???????? ?? ????? ? ???????? ???????? ?????????????? ???? ?????? ???????;
- ???? ????????? ????????? ??? ?? ????? ?????? ???????????? ????????, ????????????? ?? `Add` ? ?????? ?????? ?????????????? opcode ??? ?? ????????? ????? ????? ????????? VM.

## Session 595 - 2026-04-30 22:18 - ? ??????? opcode ???? ?????? ?????????? Push

????????:
- ????? Session 594 ????? ?????, ??? ????? ????????? ???? ??????? ??? ??????? ???????, ? ????????? ??? ? `LoadElement`/`StoreElement` ??? ?? ?????? ???????? ???????????? ??? ???????? ????????;
- ????????? ???????? ???? ?????????? ?? ??? ???? ????? ???? ???????? ??????: ?????????? `EvaluationStack.Push(...)` ????????? ?????????? ? ???? ????? ?????? opcode, ???? ??? VM ??? ???? ?????? ?????????? ????????? `RuntimeValue`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ??????? ????? `PushConst`, `PushIntegerZero`, `PushIntegerOne`, `ExecuteAddInstruction(...)`, `ExecuteAddLongIntegerInstruction(...)`, `ExecuteMultiplyInstruction(...)` ? `ExecuteCompareGreaterInstruction(...)` ?????????? ?? `PushUnchecked(...)`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` ?? `PushUnchecked(...)` ?????????? ?????? ???????? ????? `LoadElement` ? ???????? ????????????? ???????, ??? ???????? ??? ?????????????? ????????? ???????????? ?????? VM;
- ?????? ?? ?????? ????????? ????????, ?? ?????? ????? opcode ? ?? ????????? ? ????? ????? ??????? ??? ?????? benchmark.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 666 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ?????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-221741.txt` ? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-221805.txt`, runtime ratio `0.735x` ? `0.827x`, total ratio `0.739x` ? `0.827x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`, ? ???????? ?????? ?? opcode ?? ??????????.

???????:
- ??????? ?????? ?????????? ????? ???????????? ????????? ???????? ???? ? ?????????? VM, ?????? ??? ??? ??????? ?????? ???????? ???????? ???, ??? ???????? ??? ????????? ????? VM;
- ?? ??????? ???? ??? ??????? ??????????????? ????????? ??????????????????: ??????? ????, ?? ?? ??????? ?????? ? ????????? ?? ??????? ????????;
- ??????? ????????? ????????? ????? ?????? ??? ?? ? ????????? ???????, ? ? ???????? ?????????? ? ??????????? ?????.

??? ???????:
- ?? opcode-??????? ???????? ?? ??? ???????? `LoadLocal`, `JumpIfForDoneLocal`, `PushConst`, `AdvanceForLoopLocal`, `LoadElement`, `Add`, `StoreElement`, `Multiply` ? `AddLongInteger`;
- ????????? ???? ??? ??????? ????????, ?? ?????? ?????? ?? ?????????? ???????????????? ??? ???????? ????, ??? ? ?????????? ??????????????? ????.

?????? ????????? ???:
- ??????? ? ?????????? ?????? ????????? ??????? ? ?????????, ????? ?? ??? ????????? `Add`, `AddLongInteger`, `Multiply` ? ????????? ?????? `FOR`-????? ?????? `AdvanceForLoopLocal` ? `JumpIfForDoneLocal` ??? ?????????? ????? ????????;
- ????????? ??? ?? ??????? `compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` ? ?????? opcode-??????? ????? ??????? ?????? ?????? ????.

## Session 596 - 2026-04-30 22:26 - ??????? ????? ????? ver1 ? ?????? ???? ver2

????????:
- ???????????? ??????? `docs/???? ?????????? ??????? Bytecode VM ver1.txt` ? ???????? ??????? ??? ? ??????????? ?????????? ?????????? ?????;
- ? ????? ??????? ? `PLANS3` ? `WORKLOG4` ??? ??? ??????? ????? ????? ??????????? VM, ?? ?? ???? ?????????? ??????????? ???????? ?????????, ??????? ?? ?????? ??????? ???????? ?????? ??????? ????? ?? ???, ??? ?? ??? ???????? ?????????.

??? ???????:
- `docs/???? ?????????? ??????? Bytecode VM ver1.txt` ????????? ? ??????????? ? ??????????? ???????? `docs/PLANS3.md`, `docs/WORKLOG4.md` ? ??????? ??????????? `BytecodeVm`;
- ???????? ???????????? built-in ???? VM: ???????????? ??????? ?????????????? ??????????? `BuiltinExecution` ? ???????? ?????? ?? ???????, ?? ????? ?????????????, ??? ? ?????? ???? ?????? built-in ??? ???????? ??????? ?? ??? ???? fallback-???????? ????? ?? `_builtins.Values`;
- ???? ?????? ?????? ?????? `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1`, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-222638.txt`, ????? ????????? ?? ???? `array_cube_rollup.axm`, ? ???? ?????????? ????????? ?????;
- ?? ????????? ????? ?????? ?????? ????? ???????? `docs/???? ?????????? ??????? Bytecode VM ver2.txt`.

?????? ?? ver1:
- ???????: ?????????? ??????????? ??????, ???????? ????????????? ??????????? built-in, ??????? ???? ???????? ????????? ???????? VM ? ?????? ????? ?????? ?? ????????/??????? ??????;
- ???????? ???????: ??????? ????????????? ???????? ?? ????? ??????, ?????? ????????? built-in fallback-????? ? ????????? ???? ??????? ??????;
- ?? ???????: ????????? ???? ????????? ???????????????? ???????/?????? VM ? ??????? ???????? ver1 ??? ????????? ??????? ???????? ?? ??? ??????? ??????.

??? ??????? ?????? ?????? ??????:
- ??????? ??? ??????? ?????????????? ?? `document_batch_repair.axm` ? `array_cube_rollup.axm`;
- ?????? ? ???????? ?? `regex_modes_pipeline.axm`;
- ?? ??? ??????????? ?? `file_batch_roundtrip.axm`, `regex_log_analysis.axm`, `array_transform_chain.axm` ? `high_rank_metadata.axm` ?? runtime ??? total;
- ?????? ver1 ?????? ?????? ????????? ????????? ???????????, ?? ????? ??????? ??? ???????????? ???????? ?????? ??????, ?? ???????? ??????????? ????????? ver2.

????????? ?????:
- `docs/???? ?????????? ??????? Bytecode VM ver2.txt`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-222638.txt`.

???????:
- ??????? `docs/???? ?????????? ??????? Bytecode VM ver1.txt` ??????????? ????????, ?? ??? ????????????? ??? ??????? ???????? ?????????? ?????;
- ??????? ?????????? ??????? ?? `docs/???? ?????????? ??????? Bytecode VM ver2.txt`, ??? ?????? ??? ???????????? ?????? ???????????? ??????? ?????????? ??????, ????????? built-in fallback-????? ? ??????? ??????????/?????;
- ?? ????????? ???????? ???????? ?? ????????? ?????????????? ?? ??????? ?????? ??????????? ?? ??? ???, ???? ??? ?? ?????????? ?????? ?????? `compare-engines`, ? ?? ???? ??????? benchmark.

??? ???????:
- ?????? ?????? fallback-???? built-in ? ????????? ?????? ????????? ? variadic built-in;
- ???????? ?? `Add`, `AddLongInteger`, `Multiply`, `JumpIfForDoneLocal` ? `AdvanceForLoopLocal` ??? ??? ?? ?????????? ?????? ?????? ???????;
- ????? ??????? ?????? ???? ??????? ???????????? ? ??????? `--filter`, ? ?????? `compare-engines`.

?????? ????????? ???:
- ??????? `docs/???? ?????????? ??????? Bytecode VM ver2.txt` ??? ??????? ??????? ?????;
- ?????? ??????????? ????? ver2 ????????? ? ??????? ?????????? built-in fallback-?????, ????? ?????????? ? ?????????? ? ??????? `FOR/NEXT`.

## Session 597 - 2026-04-30 22:32 - ????????? ????? ???????? ????????? ????????? ????????? ? ????? ? ????????

????????:
- ???????????? ???????? ????????? benchmark-????? ?? ?????? ????????? ?????????, ?? ? ??????????, ? ????? ????????????? ?????? ????? ?????, ? ?? ???? `FOR/NEXT`;
- ????? ???????????? ???????? ???????, ??? ? ???????? ????? ???????? ? ?????? ????-???????, ?????? ????? ???? ????? ??????? ? ??????????? ?????????.

??? ????????:
- ? `samples/smoke/engine-compare/string_array_loop_pipeline.axm` ???????? ????? ?????????? ???????? `compare-engines`;
- ???????? ?????? ????????? ???????, ????????? ????????? ?????? ? ????????? ???????? ?????? ????-???????, ?????????? `DATESERIAL`, `DATEADD`, `TIMESERIAL`, `MONTHNAME`, `WEEKDAYNAME`, ? ????? ????????? ?????? ????? `ARRAYCONCAT`, `ARRAYCLONE`, `ARRAYINSERT`, `ARRAYPUSH`, `ARRAYREMOVE`, `ARRAYPOP`, `ARRAYTAKE`, `ARRAYSKIP` ? ????? `ARRAYCONCAT`;
- ?????? ???? ?? ???????? ????????????? `FOR`, `WHILE`, `DO`, `DO WHILE`, `DO UNTIL` ? `EXIT DO`, ????? ????? benchmark ???????? ?? ???? ????, ? ????? ????????? ???? ??????????;
- ? `src/Axiom.ManualChecks/Program.cs` ???????? ???????? ? ????? ????? `GetBenchmarkCases()` ??? `????????? ???????: ????? ? ????-?????`.

??????????? ??????????:
- ??????? ??????? ?????? ????? ?? ????????? ??????? ? ?????????? ??????????, ??????? ????? ?????? ????????? ???????? ?????? ???????? ? ?????? digest ???? ?????????? ?? helper-??????? ? ???????? ???????? ??? ????????? ?????? ????????;
- ??? ????-??????? ???????????? ?????? ????????????????? built-in, ????? benchmark ?? ??????? ?? ??????? ????? ? ????????? ????????? ??? ???????? ????????? ???????.

????????? ?????:
- `samples/smoke/engine-compare/string_array_loop_pipeline.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "string_array_loop_pipeline.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-223215.txt`, runtime ratio `0.638x`, total ratio `0.703x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-223244.txt`, ????? ????? `8` ????????? ???????? ?????????.

??? ??????? ?????? ?????? ????? ?????????? ??????:
- ????? ????????? ???????? ??? ???????????? ??? ????????;
- ??????????? ???????? ? `array_cube_rollup.axm` ????? ???????? ? ???? ???????? ????????;
- `regex_log_analysis.axm` ?? ??? ????????? ? ??????? ?????? ????????? ????????????? ??? ver2;
- ??? ?????? ????????? ????? ??????? ????????: ?????? ?? ????????? ?? ?????? ???????? ????????? hot path, ?? ? ????????? ???????, ????? ?????????? ???? ? ????????????????? built-in ????-???????.

???????:
- ??????? ?????????? benchmark-?????? ???????? ????????? ?????, ? ?? ????????????? ??????????? ??? ?????? ???????;
- ??????? ????? ???????? ?????????? ?????? `compare-engines`, ?? ??????? ????? ??????? ?????????? ???????????;
- ?? ???????????? `NOW()`, `TIME()` ? ?????? ??????? ???????? ??????? ? benchmark-????????? ????????? ???????, ???? ?? ????? ???????? ?????????????????? ?????????????.

??? ???????:
- ???????????? ??? ??????????? ????? ??? ???????????? ????????? ?????? ??? ver2;
- ???????? ????????? ?????????? ???????? ?? `regex_log_analysis.axm` ? ????????? ??????????-???????? ???? VM;
- ?? ???????????? ? ?????? ????????? benchmark-only ???????? ???, ??? ????? ????? ??? ?????????? ????? ??????? ???????.

?????? ????????? ???:
- ?????????? ver2 ??? ?? ???????????? ?????? ? ?????? ????? ????????? ?????????? built-in fallback-????, ? ????? `Add`, `AddLongInteger`, `Multiply`, `JumpIfForDoneLocal` ? `AdvanceForLoopLocal`, ?????? ??? ???????? ? ?? ??????? ????????, ? ?? ??????? `compare-engines`.

## Session 598 - 2026-04-30 22:36 - ????? ???????? fallback built-in ?? CanonicalId ? VM

????????:
- ?? ????? ver2 ????? ?????????? ?????????? ?????? ????????? ?????????? ????? ???? ?????? ?????????? fallback-???? built-in, ?????? ??? ?????????? ? ?????????? ???????? ??????????-????????? ????;
- ???????? ???? ????????, ??? ???????? built-in-?????????? ? VM ??? ????? ?????????? ?????? ??????? ? `BuiltinExecution`, ?? ? ?????? ???? ?????? built-in ??? ???????? ??????? ?? ??? ????????? fallback-???????? ????? ?? `_builtins.Values`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ???????? ??????? `_builtinsByCanonicalId` ???? `BuiltinFunctionId -> BuiltinBinding`;
- ? `InvokeBytecodeFunction(...)` ???????? fallback `FirstOrDefault(candidate => candidate.CanonicalId == builtinId)` ?????? ?? ?????? ?????? ?? `_builtinsByCanonicalId`;
- ???????? ????? `builtin id -> BuiltinExecution` ? ?????? ?????? built-in ?? ??????? ?? ????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 667 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-223646.txt`, ?????? ????? ?? 8 ????????? ???????? ???????.

??? ??????? ?????????:
- ??? ?? ??? ?????????? ???????? ?????????? ????????, ??? ???????? ??? ??????? function-value ???? built-in;
- ???? ?? ????????? ?????? ????????????? ????? ver2: ?????? ? ??????? built-in-?????, ? ?????? built-in ??? ???????? ??????? ????????? ?? ?????? ???????, ? ?? ?? ???????? ?????.

???????:
- ??????? ???????? fallback built-in ?? `CanonicalId` ???????? ??? ??????????? ???? ??????? ?????;
- ?? ????????????? ???? ??? ?? ????????: ??? ???????? ????????????? ? ????????????????, ? ?? ? ???? ???????????????? ???????? ???????? ?? ????? benchmark;
- ??????? ????????? ????????? ??????? ??? ?? built-in ????, ? ?????????? ? ?????? ?????.

??? ???????:
- ?? ??????? ?????? ????? ????? ????????? ?????????? ???????? ?? ??? ???????? `regex_log_analysis.axm`;
- ?? ??????????? hot path VM ?????????? ??????????? ???????? `Add`, `AddLongInteger`, `Multiply`, `JumpIfForDoneLocal` ? `AdvanceForLoopLocal`.

?????? ????????? ???:
- ?????????? ? ?????????? ?????? ???????? ???? ver2 ? ????????? ????????? ?????????? ? ????, ?? ????? ??? ???? ?? ???? ?????? `compare-engines` ? ??????????? `regex_log_analysis.axm`.

## Session 599 - 2026-04-30 22:48 - ? ??????? helper-?????? VM ????? ????????? As-????????

????????:
- ????? Session 598 ?? ????? ver2 ????????? ??????? ?????????? ?????????? ? ?????? `FOR`-?????;
- ?????? ??????? ?? `array_cube_rollup.axm` ??-???????? ?????? ? ???????? `LoadLocal`, `JumpIfForDoneLocal`, `AdvanceForLoopLocal`, `Add`, `Multiply` ? `AddLongInteger`, ??????? ????????? ??? ???? ??????? ?????? ?? ????? ????? ??????? opcode-?????, ? ????? ??????????? ??? ?????????????? ???????? ??????.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ????????? ????????? `GetIntegerUnchecked(...)`, `GetLongUnchecked(...)`, `GetDoubleUnchecked(...)`, `GetDecimalUnchecked(...)`, `GetStringUnchecked(...)`, `GetBooleanUnchecked(...)` ? `AggressiveInlining`;
- ?? ??? ?????????? ??????? helper-????? `TryEvaluateHotAdd(...)`, `TryEvaluateHotMultiply(...)`, `TryEvaluateHotCompareGreater(...)`, `TryShouldTerminateLocalForLoop(...)`, `TryAdvanceLocalForLoop(...)` ? ????? ?????? ???????? ????????? ????;
- ? `ExecuteAddLongIntegerInstruction(...)` ???????????? ?????? ????????? ?????????? `ResolveLocation(...)`, ? ?????? `LONG + INTEGER` ???? ???? ???????? ?? ??? ??????????? ?????????? ????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 667 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-224736.txt`, ?????? ????? ?? 8 ????????? ???????? ???????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-224752.txt`, runtime ratio `0.670x`, total ratio `0.675x`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`, ? ?????? ?? ??????? ?? ????????.

??? ??????? ?????????:
- ??? ?????????????? ??? ??????????? ????????? ???????? ???? ??? ????????? ????? ????????: ??????? `array_cube_rollup.axm` ??????? ??????? ???????????? ???????? ??? ???????????????;
- ??????? ??????????, ??? ????????? ???????? ?? ? ????????? ??????, ? ? ???? ??? ???????????? ?????? ????????, ?????? ????? `LoadLocal` ? ??????? `FOR`-?????;
- ?????? `compare-engines` ??????? ?????????: ????????? ? ???????? ???????? ????????? ???? ??????, ?? regex-????? ?? ??? ??????? ?????? ????????? ????????????? ver2.

???????:
- ??????? ??????? ???????? ????????????? ? ?? ?????????? ?????? ??? ?????? ????????????????;
- ?? ?????????? ? ????? ????? ?????????????? ????????? ??? ??????????? ??????? ?????? ???? ?????????? ????????;
- ??????? ? ???????? ?????????? ?????? ????????? `LoadLocal`/`StoreLocal` ????? ? `JumpIfForDoneLocal` ? `AdvanceForLoopLocal`, ? ?? ?????? ?????????? ? ?????? ?? ?????.

??? ???????:
- ?? ??????? `array_cube_rollup.axm` ??????? ??????? `LoadLocal`, ? ?????? `FOR`-????? ?? ??? ???????? ????? ???????? ???? ??????;
- ?? ??????? ?????????? ?????? regex-???????? ??-???????? ?? ????????? ??????? ver2 ??????? ? ??????????.

?????? ????????? ???:
- ?????????, ????? ?? ??? ????????? ????????? `LoadLocal`/`StoreLocal` ? ????????? ???? `FOR` ??? ????????? ????????? ? ??? ?????????? ?????? opcode;
- ????? ?????????? ???? ????? ????????? ? ??????? `array_cube_rollup.axm`, ? ?????? `compare-engines`, ????? ?? ?????????? ????????? ??????? ?? ??????? ?????? ???????.

## Session 600 - 2026-04-30 22:55 - FOR-?????? ??????? ? ?????????????????? ????????? ?????

????????:
- ????? Session 599 ???????????? ???????? ???????, ??? ??????? ??????? ?????? ?????? ??????? ?????? `FOR`, ? ????????? ????? ?????????? ?? ???????? ???????????;
- ?? ??????? `array_cube_rollup.axm` ????? ??????????? ???? ?? ??? ??????? `LoadLocal`, `JumpIfForDoneLocal` ? `AdvanceForLoopLocal`, ??????? ???? ??????? ?? ??????? ????????? ?????, ? ????????? ?????? ????????? ?????? `FOR`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` ????????? ?????? `LoadForLoopLocalValues(...)`, `LoadForLoopAdvanceValues(...)` ? `StoreNumericLocalValueUnchecked(...)`;
- `ExecuteJumpIfForDoneLocalInstruction(...)` ? `ExecuteAdvanceForLoopLocalInstruction(...)` ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ?????????? ?? ??? ?????????????????? ???????? ?????;
- ?? ???? ????? ? ??????? `FOR`-???? ????????? ????????? ???????? `_hasSharedLocals`, ??????????? ????? ????????? ????????? ? ??????? ? ????????? ?????? ?????? ????? ????? ???????????? ???????? ??? ??? ?????????????? ????????? ???????? ?????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 667 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "array_cube_rollup.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-225428.txt`, runtime ratio `0.707x`, total ratio `0.713x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-225503.txt`, ?????? ????? ?? 8 ????????? ???????? ???????;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\array_cube_rollup.axm` - ???????, ??????? ??????? `Instructions: 285800`, ? ?????? ?? ??????? ?? ????????.

??? ??????? ?????????:
- ??? ????????? ?????????? ????????? ?????? `FOR`, ?? ????????? ?????? ???????? ????? ??????, ??? ?????????? ?????? helper-??????????;
- ??? ???? ????? ?????? ?? ????????: ?????? ????? ????? ????????, ? ????????? ???????? ????????? ????????? ???????????? ???????? ??? ???????????????;
- ??????? ????? ?????? ??? ?????: ???? ????? ??????? FOR-????????????? ???????? ???????? opcode ??????? `LoadLocal`, ?????? ????????? ???????? ??????? ????? ??? ? ????? ?????? ???????.

???????:
- ???????? ?????? ? ?????? ??? ?????????? ????????? ??????????? `FOR`, ? ?? ?????????? ? ?????? ??-?? ?????? ????????? ???????? ???????;
- ?? ??????????? ?? ?????? ?????, ???? ?????? `FOR` ? ???????? `LoadLocal` ?????????? ?????? ???????? ???????;
- ??????? ????????? ????????? ??????? ??? ????? ???? `LoadLocal`, ? ?? ??? ???? ?????????????????? ?????? `JumpIfForDoneLocal`.

??? ???????:
- `LoadLocal` ??-???????? ?????????? opcode ? ??????? `array_cube_rollup.axm`;
- regex-???????? ??????? ?????? ?? ??? ?? ???? ??????? ver2 ??????? ? ??????????, ???? ???? ????????? `FOR` ??? ?????? ???????.

?????? ????????? ???:
- ?????????, ????? ?? ??? ????????? ????????? ????? ???? `LoadLocal` ??? ?????????? ????????? shared-??????? ? ??? ???????? ? ??????????? ??????????;
- ????? ?????????? ???? ????? ????????? ??????? `array_cube_rollup.axm`, ?????? `compare-engines` ? ??????????? opcode-???????.

## Session 601 - 2026-04-30 23:04 - ???????? ?????? ????????? benchmark ?? ????????, regex, ????? ? SELECT CASE

????????:
- ???????????? ???????, ??? ?????? ?? ?????? ????? ????????? ??????, ? ?????? ????? ???? ????? `.axm`-????????, ??? ? ????? ????????? ????? ?????, ???????, regex, ??????, ????-????? ? ?????? ??????????? ????????;
- ???????? ???? ????? ?? ?????? `SELECT CASE`, ????? ?????????????? benchmark ???????? ?? ?????? ??????? ? ?????, ?? ? ???????? ????????? ?? ????????? ?????.

??? ????????:
- ? `samples/smoke/engine-compare/mixed_workload_pipeline.axm` ???????? ????? ?????? ????????? ????????;
- ???????? ???????? ???????? ? ????????? ???????, `FOR`, `WHILE`, `DO`, `DO WHILE`, `DO UNTIL`, `ARRAYCONCAT`, `ARRAYCLONE`, `ARRAYINSERT`, `ARRAYPUSH`, `ARRAYREMOVE`, `ARRAYPOP`, `ARRAYTAKE`, `ARRAYSKIP`, ????????? ???????, `REGEXREPLACE`, `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT`, ??????? ????-??????? ? ????????? `SELECT CASE` ? ???????????????? ??????;
- ? `src/Axiom.ManualChecks/Program.cs` ????? ???? ???????? ? `GetBenchmarkCases()` ??? `????????? ????????: ?????, ??????, regex, ???? ? SELECT CASE`.

????????? ?????:
- `samples/smoke/engine-compare/mixed_workload_pipeline.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1 --filter "mixed_workload_pipeline.axm"` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-230404.txt`, runtime ratio `0.766x`, total ratio `0.811x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-230415.txt`, ?????? ????? ?? 9 ????????? ???????? ???????.

??? ??????? ?????????:
- ????? ???????? ????????????? ????????? ???? ??????????????? ?????????? benchmark-?, ? ?? ??? ?????? ?????? ?????: ? ?? ???????????? ???????? ?????, ???????, ??????, regex, ????-????? ? `SELECT CASE`;
- ???????? ?????? ??????????, ??? ??????? ??? ????? ???? ?? ?? ???????, ?? ?????? ???????? ?????? ??? ????????? ???????? ?? runtime, ??? ?????? ???????? ???????? ???????? ??? ??????? ??????????? ??????;
- ?????? ???????? ?????? ????? ????? ? ???????? ???? ????????????: ???? ? ??????????????? ????????? ?????, ? ???? ????? ????????? ??????? ????????.

???????:
- ??????? `mixed_workload_pipeline.axm` ?????????? ?????? `compare-engines`, ? ?? ????????? ?????????????;
- ?? ???????? ?? ?????????????????? ????????, ?????? ??? ??? ?? ??? ????? ??? ??????????? ????????? ? ?????????;
- ???????????? ????? ????????? benchmark ??? ?????? ?? ???????? VM ??? ???? ????????? `FOR`-??????.

??? ???????:
- ?? ??????? ?????? ?? ??? ??????? ????????, ??? ??????? ???????????, ?????? ????? `regex_log_analysis.axm`, `file_batch_roundtrip.axm` ? ?????? ????? ????????? ?????????????? ??????;
- ??? ??????? ????????? ????? ?? ??? ??????????? ????????? `LoadLocal` ? ??????? regex- ? ?????????? ?????? ????????? ????????.

?????? ????????? ???:
- ?????????, ????? ?????? ??????? ? regex- ? ????????? ?????? ??????? ????? ????? ???? `regex_log_analysis.axm` ? `mixed_workload_pipeline.axm`;
- ????? ?????????? ???? ??????????? ????????? ? ???????? ?????? ?????? `mixed_workload_pipeline.axm`, ? ?????? `compare-engines`, ????? ?????????????? ???????? ????? ?????????, ?????????? ?? ???????? ????? ????.

## Session 602 - 2026-04-30 23:09 - ? Bytecode VM ???????? ??? regex-???????? ?? ????? ??????????

????????:
- ????? ?????????? `mixed_workload_pipeline.axm` ????? ?????, ??? ????????? ??????? ????? ??? ?? ? ????????? ??????? ?? `LoadLocal`, ? ? regex- ? ????????? ??????, ??????? ????? ? `regex_log_analysis.axm`, ? ????? ????????? ?????????????? ????????;
- ???????? ???? ????????, ??? regex built-in ? ?????????? VM ?????? ??? ?????? ??????? `Regex`, ???? ???? ? ????? ? ??? ?? ????????? ????? ? ????? ???????????? ?? ?? `pattern` ? `modes`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs` ???????? ???????-??? `Regex` ?????? ?????????? `BytecodeVm`;
- `CreateRegex(...)` ?????? ??????? ???? ??? ????????? ?????? ?? ????? `pattern + RegexOptions`, ? ?????? ??? ??????? ?????? ????? `Regex` ? ????? ??? ? ???;
- ??? ???? ?????? ? ???????? ?????? ??????? VM, ??????? ?? ?????? ??????? ???????? ?????????? ? ?? ????????? ????????? ????? ???????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "regex_log_analysis.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-230901-regex.txt` - ???????, runtime ratio `0.851x`, total ratio `0.842x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-230901-mixed.txt` - ???????, runtime ratio `0.838x`, total ratio `0.876x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-230939.txt`, ?????? ????? ?? 9 ????????? ???????? ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????.

??? ??????? ?????????:
- ???????? ?????????????: ????????? ?????? ?????????? regex-???????? ????????????? ???? ????? ????? ??????????? ??????, ? ?? ?????????? ?????? benchmark-?????;
- ???????? ??????? ???????? ???????? ? ?????? regex-????????, ? ????? ????????? ?????????????? benchmark;
- ?????? ???????? `compare-engines` ??????? ?????? ? ????????? ?? ????????? regex- ? ????????? ?????????, ?? ????? ?????? ????? ???? ?? ????????, ? ????????, ????????? ? ????????? ?????? ???????? ? ??????? ?????????.

???????:
- ???????? regex-??? ??? ????????? ??????????? VM, ? ?? ??? ????????? ?????????? ??? ?????????? benchmark;
- ??????? ????????? ????? ??? ?? ? ????????? ?????? `Regex`, ? ? ????????? ?????????? ????????? ???????? ? ??????????? regex built-in ?????? ?????????? VM;
- ?? ???????????? ? `LoadLocal` ??? ????? ?????? ????????, ?????? ??? ??????? ??? ??? ??? ????? ??????? ????? ?????? ?? ????????? ????????.

??? ???????:
- ?????? ????? ?? ??? ?????????? ??????????? ??????? ?? ????? regex- ? ????????? ?????????;
- ????? ?????????????? `mixed_workload_pipeline.axm` ??????? ?????? ????????, ?????? ??? ?? ????????? regex ? ???????, ????????, ????????? ? `SELECT CASE` ? ????? ?????????.

?????? ????????? ???:
- ????????? ????????? ?????????? ???????? ????? ? ????????? ?????????? ?????? ? `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT` ? ??????? built-in;
- ????? ?????????? ???? ????? ????????? ???????? `regex_log_analysis.axm` ? `mixed_workload_pipeline.axm`, ? ????? ?????? `compare-engines`.

## Session 603 - 2026-04-30 23:14 - ?????????? 1D-??????? VM ?????????? ?? ?????? ??????

????????:
- ????? Session 602 ????? ?????, ??? ????????? ?????? `Regex` ??? ?? ???????????? ???? regex-??????: ????? ? ??? ??????? ????????? ?????????? ????????? ???????? ??? `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT` ? ???????? built-in;
- ???????? helper-???? ????????, ??? ?????????? ?????????? ??????? ? ?????????? VM ????????? ????? `Allocate(...)` ? ??????????? ???? `SetValue(...)`, ?? ???? ? ?????? ???????, bounds-??????? ? ????? ????????????? ???????? ???? ???, ??? ???????? ??? ???????????? ????? built-in.

??? ????????:
- ? `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` ????????? ??????? `CreateCompactOneDimensional(...)`, ??????? ???????? ????? ????? ??????????? ??????? ? ????? ????????? `_elements`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` helper `CreateCompactArrayValue(...)` ???????? ?? ??? ???????;
- ?? ???? ????? regex-, ?????????, ???????? ? ?????? built-in, ???????????? ?????????? ?????????? ???????, ?????? ?? ?????? ?? ?????? ???? ????? `Allocate(...)` + `SetValue(...)` ?? ?????? ???????.

????????? ?????:
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "regex_log_analysis.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-231450-regex.txt` - ???????, runtime ratio `0.850x`, total ratio `0.836x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-231450-mixed.txt` - ???????, runtime ratio `0.791x`, total ratio `0.853x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-231440.txt`, ?????? ????? ?? 9 ????????? ???????? ???????.

??? ??????? ?????????:
- ??? ???????????? ??? ????? ??????? ?? regex- ? ?????????? built-in-????, ? ?? ??? ??????? ?????????? ??? ???? ????????;
- ????? ????????? ?????????????? benchmark ???? ? ???? ???????????? ???????? ???? ?? ?????? ???????? ???????;
- `regex_log_analysis.axm` ?????? ?? ???????? ????? ????????? ????????: ?? runtime ?? ??????????? ?? ???????? ???? ? ?????? ??????, ? ?? ???????? ????????? ??????? ??? ??????? ? ?????? ????????.

???????:
- ??????? ???? ?????????? ????????? ???????? ?????? ?????? ??????? ?????, ? ?? ?????????????? ????????????? ??????????;
- ???????, ??? ??????? ????? ????? ????? Session 603 ??? ??????? ????? ???????? ????????? ???????? ?????????? VM, ??? ?? ?????? ?????? ver2;
- ????????? ????? ???????? ??? ?? ?????????? ????????????? ??????? ??????, ? ?? ?? ?????? ??????? ?????? `FOR` ? `LoadLocal`.

??? ???????:
- `high_rank_metadata.axm` ??????? ????? ????????? ? ?????? ?????????, ??? ????????????? ???????????? ????????????????, ?? ??? ?? ??? ????? ??????? ??? ???????????;
- ?? ????????? ????????? ??????? ??????? ????? runtime ? total, ?????? ????? ??????? ????? ?????? ?? ? ????? ?????????? VM, ? ? ???????? ?????? ??????????/?????????.

?????? ????????? ???:
- ?????????? ????????? ?????????? ??????????? ??????? ?????? ??? ????? Session 603 ? ???? ? ???? ?? ??????????, ? ?? ?? ??????? ?????? hot-path ???????;
- ???? ??????????????? ?? ??????? ???????, ????????? ?????????? ??????? ????? ???? `high_rank_metadata.axm` ??? ??????????? ????? ??????, ???? ????????? ?????? ????? runtime ? total ???, ??? VM ??? ???????, ?? ???????? ????? ??? ?????.

## Session 604 - 2026-04-30 23:19 - ARRAYBOUNDS ???????? ?? ?????? 2D-??????, high-rank ???????? ??????? ??????

????????:
- ????? Session 603 ????????? ?????????? ??????????? ?????????? ???????? `high_rank_metadata.axm`, ?? ???? ??????, ???? ?? ??? ???????? ???????? ????? ??? ??? ??? ??? ?????????????;
- ?????? built-in ???????, ??? `ARRAYBOUNDS` ?? ??? ?????? ????????? ????????? integer-?????? ????? ????? ????, ???? ?? ?????? ??? ?????????? ????????? ????????? ????????????? ?????.

??? ????????:
- ? `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` ????????? ??????? `CreateCompactTwoDimensional(...)`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` `EvaluateArrayBoundsBuiltin(...)` ???????? ?? ?????? ?????? ?????????? `ARRAYBOUNDS` ????? ??? ???????;
- ??? ??????? ?????? ????? ???? ??? ?????????? ?????????? ?????????? ??? ????????? ???????????????? ?????????.

????????? ?????:
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter "high_rank_metadata.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-232015-highrank.txt` - ???????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-231932.txt`, ?????? ????? ?? 9 ????????? ???????? ???????.

??? ??????? ?????????:
- ?????? ???? `ARRAYBOUNDS` ??? ?? ???? ?????????, ?? `high_rank_metadata.axm` ??????? ??????? ????????? ?????????, ????? ?? ???? ?????? ???????? ????????? ?????????????? ???;
- ??? ???????? ????????????? ?????: ?????? ???? ????? ?????????????, ??? ????????? ????????? ????? ????? ?? ?? high-rank metadata, ? ?? ????? ?????????? ????????? ??????? ??????;
- ?? ?????? ?????? ??-???????? ?????? regex-, ????????? ? ????????? ?????? ???? ?????? ???????, ??? tiny high-rank benchmark.

???????:
- ???????? ?????? 2D-?????? `ARRAYBOUNDS` ? ?????? ??? ?????????? ????????? ?????????;
- ?? ???????????? `high_rank_metadata.axm` ??? ??????? ???????? ??? ????????????? ????????? ???????????;
- ??????? ???? ???????? ????????????? smoke-?????????, ? ?? ??????? benchmark-????????.

??? ???????:
- ? ?????? ?????? ?? ??? ???? ????????? ?????????? ?? ????? regex- ? ????????? ?????????;
- ????? ????????? benchmark ??????? ?????? ???????????? ?????????? ????? ?????????? `array_cube_rollup.axm`.

?????? ????????? ???:
- ????????? ? ?????????? ????????? ??????? ?????? ? ??????? ????????? ????? ?? ???, ? ?? ?? `high_rank_metadata.axm`;
- ??????????? ??????? ? ?????? `mixed_workload_pipeline.axm`, `regex_log_analysis.axm` ? `string_array_loop_pipeline.axm` ??? ???????? ?????????????? ?????????? ?????? ???????? ?????????? VM.

## Session 605 - 2026-04-30 23:23 - ????? ???? Convert ? ????????? ? VM ???????? ?? ?????? ??????

????????:
- ??????? `mixed_workload_pipeline.axm` ????? Session 604 ??????? ??? ?????? ???????? ???????? ????: ?????? ????? `Convert`, `CompareEqual`, `JumpIfTrue`, `Contains`, `Replace`, `Str` ? ???????? ????????-?????????? ????, ? ?? ?????? ????????? `FOR`-?????????;
- ??? ?????? ???????? ????????? ??? ?? ? ????????? ??????????? built-in-?????, ? ? ????? ???? VM, ??? `Convert` ? ????????? ????????? ?? ??? ?????? ????? ??????????? accessor-?.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` `ExecuteConvert(...)`, `ConvertValue(...)`, `IsZero(...)`, `ValuesEqual(...)` ? `Compare(...)` ?????????? ?? ??? ??????????? ?????????? ???????? ????? `GetIntegerUnchecked(...)`, `GetLongUnchecked(...)`, `GetDoubleUnchecked(...)`, `GetDecimalUnchecked(...)`, `GetStringUnchecked(...)`, `GetBooleanUnchecked(...)`;
- ????????? `Convert` ?????? ??????? ? ???? ????? `PushUnchecked(...)`;
- ????????? ?????????????? ? ????????? ?? ????????, ????????? ?????? ?????????? ???? ??????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-233000-mixed.txt` - ???????, runtime ratio `0.715x`, total ratio `0.845x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-233000-string.txt` - ???????, runtime ratio `0.702x`, total ratio `0.848x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 4 --warmup 1 --filter "regex_log_analysis.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-233000-regex.txt` - ???????, runtime ratio `0.815x`, total ratio `0.823x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 2 --warmup 1` - ???????, ????? `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-232355.txt`, ?????? ????? ?? 9 ????????? ???????? ???????.

??? ??????? ?????????:
- ????? ???? `Convert` ? ????????? ????????????? ??? ?????????????? ?????????? ??? ????????? ????????;
- ???????? ??????? ??????? ?????????? ?? ??? ???????? ?????? ?????????: regex, ????????? ????????? ? ????? ??????????????;
- ?????? ???????? ????? ?? ??? ??????? ?????? ?? ????? ?????????, ?? ????? Session 605 ??????? ?? ??????? ?????? ? ????? ???????? ??? ??????????? ????? ????????, ??? ? ?????? ver2.

???????:
- ???????? ?????? ??? ????? ??? ?? ??????????? ?????? VM, ? ?? ??? ??????? ??????????? ??? ???? `.axm`;
- ??????? ????????? ??????????? ??? ?? ?????????????? accessor-???? VM, ? ??????? built-in-?????????????????? ?????????? ??????????????? ????????;
- ???????????? `mixed_workload_pipeline.axm` ??? ???????? ???????????? ???????? ?????????? ?????, ? ?? ?????? ??? ?????????????? benchmark.

??? ???????:
- ? ?????? ???????? ??????? `regex_log_analysis.axm` ? `mixed_workload_pipeline.axm` ?? ??? ????? ??????? ??????? ???????? ????????;
- ?????? ????????? ??????? ????????? ????? ??? ? ?????? ?????????? ????????? built-in ? ??????? ???????, ? ?? ? ??????? opcode VM.

?????? ????????? ???:
- ????? ?????? ??????? `mixed_workload_pipeline.axm` ????? Session 605 ? ???? ? ??? ???????? built-in/Opcode-??????;
- ???????? ????????? ???? `Contains`, `Replace`, `Str`, `CompareEqual`, `JumpIfTrue` ? ???????? ???????? ????????, ??????? ????????? ???????? ????????? ????????? ????????.

## Session 606 - 2026-04-30 23:35 - ??????? ????? ?????????? built-in ? ???????? `CallBuiltin`

????????:
- ????? Session 605 ???????? ??????????????? ??????? `mixed_workload_pipeline.axm` ?????????? ???????: `LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Convert`, `CompareEqual`, `JumpIfTrue`;
- ?? built-in-????? ???? ?????, ??? ???????????? ???? ?????????, regex-, ???????? ? date/time built-in ??? ????? ????? ??????? `Get*Unchecked(...)`, ?? ? ????? dispatch-??????? ??? ????????? ??????? ????? ?????????????? ??????? ? `AsString()` / `AsInteger()` / `AsBoolean()`;
- ??? ?????? ????????? ???????? ????? ?? ????? ??????? ??????????? ??? ???? benchmark, ? ????????? ?????? ?????? ?????????? built-in ? ???????? ?? `CallBuiltin`.

??? ????????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` `ExecuteBuiltinCall(...)` ?????? ????? ????????? built-in ? ???? ????? `PushUnchecked(...)`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` ?????????? ?????????????? string/integer/boolean built-in ?????????? ? `AsString()` / `AsInteger()` / `AsBoolean()` ?? `GetStringUnchecked(...)` / `GetIntegerUnchecked(...)` / `GetBooleanUnchecked(...)`;
- ?????? ??????? ?????? (`VAL`, `LCASE`, `UCASE`, `WRITE`, `WRITEFILE` ? ??.), ????-????? ? ??????? ????? (`DATESERIAL`, `TIMESERIAL`, `DATEADD`, `WEEKDAY`, `MONTHNAME`, `TOUTC`, `FROMUTC` ? ??.), ? ????? ???????-??????? ???? (`FILEEXISTS`, `READFILE`, `GETFILES`, `COPYFILE`, `CHANGEDIR` ? ??.).

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 195 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-234600-mixed.txt` - ???????, runtime ratio `0.721x`, total ratio `0.853x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-234600-string.txt` - ???????, runtime ratio `0.624x`, total ratio `0.770x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "regex_log_analysis.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-234600-regex.txt` - ???????, runtime ratio `0.818x`, total ratio `0.822x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 2 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260430-234600-full.txt` - ???????, ?????? ????? ?? 9 ????????? ????????;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - ???????, ????????? ???????? ??????? ?? ??????????: `LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Convert`, `CompareEqual`, `JumpIfTrue`.

??? ??????? ?????????:
- ????? built-in-????? ????????????? ?????? ?????????: ???????? ??????? ??????? ?? `mixed`, `string` ? `regex` ?????????? ??????? ? ???????? ?????????? ????;
- ??? ???? ??????? `mixed_workload_pipeline.axm` ?? ???????? ????? ?? ?????????, ?? ???? ?? ????????? ???????????? ?????, ? ?? ??????? ????? ????????;
- ?????? ???????? ????? ?? ??? ????????????, ??? `regex_log_analysis.axm` ??????? ?????? ????????? ????? ????????, ?? ??? ??? ?????? ?????????? ??????? VM.

???????:
- ???????? ?????? ??? ????? ???????????????? ??? ??? built-in-???? VM, ? ?? ??? ??????? ????????? ??? ???? benchmark;
- ??????? ???? ????????? checked-accessor-?? ? built-in dispatch ? ???????? ????????;
- ?? ???????????? ? `LoadLocal` ??? `FOR` ??? ????? ????? ????????, ?????? ??? ??? ?????? ??? ???????????? ???????????.

??? ???????:
- ???????? ??????????????? ??????? `mixed_workload_pipeline.axm` ??-???????? ?????????? ? `LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Convert`, `CompareEqual`, `JumpIfTrue`;
- ?? built-in-??????? ??-???????? ??????? `Contains`, `Str`, `Replace`, `Right`, `Insert`, `RegexReplaceModes`, `TrimWhitespace`, ? ?? date/time ???? - `DateAdd`, `DateSerial`, `TimeSerial`, `Weekday`, `WeekdayName`, `Month`, `MonthName`, `Day`;
- ?????? ????????? ??? ????? ????? ???? ?? ?????? `PushConst`, ???? ?? ???????? ????????? ?????? ????????? ? ?????????, ?? ??? ?? ?????? ????????? ????????.

?????? ????????? ???:
- ????????? ??????? ? ????????? `PushConst` ?? `mixed_workload_pipeline.axm` ????? Session 606 ? ?????????, ???? ?? ??? ??? ???? ??-?????????? ????? ???????? ?????? `0`/`1`, ? ?? ???????? ??? ???? ????????;
- ???? ????????????? ???????? ???????? ???????, ????????????? ?? ?????? `CompareEqual` + `JumpIfTrue` + ???????? ????????? built-in (`Contains`, `Replace`, `TrimWhitespace`) ??? ?? ????????? ????? ???? ?????????????? ????????.

## Session 607 - 2026-05-01 21:39 - ????????? ? ????????? ?????????? ??????? ????? ? ?????? opcode

????????:
- ???????????? ???????? ???????? ?? ???????????? ? `PushConst` ? ??????? ????????? ?? ??????????, ?? ?????? ?? ?? ??? ? ????????-?????????? ???????? ??????? `mixed_workload_pipeline.axm`;
- ?????? `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????, ??? ?? ??? ??????? ???????? ????: ????? `Convert`/`CompareEqual` ????? `Get*Unchecked(...)`, ????? built-in-dispatch ? ????? ?????? ?????? `Compare... + JumpIfTrue/JumpIfFalse` ?????? ??? ????????? ??????? ? ???????? ????????;
- ?? ?????????? ????????? ???? ?????? ?? ????????? `=` / `<>` ? ?????????????? ????????-?????????? ??????? ??? ?? ????, ??????? ???? ????? ????????? ?????? ????????.

??? ????????:
- ? `src/Axiom.Bytecode/Model/Opcode.cs` ????????? ?????????? opcode `JumpIfEqual` ? `JumpIfNotEqual`;
- ? `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` ?? ????? ???????? ?????? ??????? comparison-????????? (`-2`);
- ? `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` peephole-?????? lowered-??? `Compare... + JumpIfTrue/JumpIfFalse` ????????? ?? `SyntaxKind.EqualsToken` ? `SyntaxKind.NotEqualsToken`, ??? ??? `=` ? `<>` ?????? ???? ????? ?????????? ? ?????? ????????? ??? ?????????????? ?????? ??????????;
- ? `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` ????? opcode ?????????? ? ????????? operand count, jump target, stack-flow ? compatibility-????????? ????? ????? ??? ?? ???????, ??? ? ? ??????? ???????? ?????????;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ????? ????? ??????????? ????? ??? ???????????? `ExecuteComparisonJumpInstruction(...)`, ??????????? ?? `Equal` ? `NotEqual`, ? ??????? `JumpIfTrue`/`JumpIfFalse` ?????? ?????????? ?? `GetBooleanUnchecked(...)` ?????? `AsBoolean()`;
- ? `tests/Axiom.Bytecode.Tests\BytecodeCompilerTests.cs` ???????? ???? ?? ?????? `CompareEqual` + `JumpIfTrue` ? `JumpIfEqual`, ? ? `tests/Axiom.Bytecode.Tests\BytecodeModuleValidatorTests.cs` - ???????? ?????????? ? ???????????? ????????? `JumpIfEqual`.

????????? ?????:
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj` - ???????, 198 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - ???????, 668 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-001500-mixed.txt` - ???????, runtime ratio `0.741x`, total ratio `0.895x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-001500-string.txt` - ???????, runtime ratio `0.576x`, total ratio `0.760x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 2 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-001500-full.txt` - ???????, ?????? ????? ?? 9 ????????? ????????;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - ???????, `Instructions: 47080`, ???????? ???????? ? `CompareEqual`/`JumpIfTrue` ?? `JumpIfEqual`/`JumpIfNotEqual`.

??? ??????? ?????????:
- ??? ??? ?? ????????????? ????????????????, ? ?????? ??????????? ?????? ??????????????? string/branch-????: `mixed_workload_pipeline.axm` ?????? ????????? ???????? `CompareEqual` ? ????? ?????? ???????? ?????????;
- ?? ??????? ???????? ?????? ???????? ???: `LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Jump`, `Convert`, `LoadArgument`, `JumpIfEqual`, `Modulo`, ? ?? built-in-??????? ?? ??? ????? `Contains`, `Str`, `Replace`, `Right`, `Insert`, `RegexReplaceModes`, `TrimWhitespace`;
- ?????? ???????? ????? ??????? ?????? ?? regex-????????, ?? ?? ?????????? ? ?????????? ???????? ???????? VM ?????????? ????????? ????????????.

???????:
- ??????? ???? `CompareEqual` + ???????????? ????????? ???????? ????????? ??????;
- ?? ???????????? ? `PushConst`, ?????? ??? ?? ??? ???????????? ?????????? ? ???????????? ???????? ???????? ??? ?????? ?? ??????? ??? ????? ?????????????? ???????;
- ????????? ??? ????? ??? ?? ?? ??????????????? equality-path, ? ?? ?????????? ????????? built-in, ??????? ?????? ??? ???? ????? ????? ?????? ?????????? `CompareEqual`.

??? ???????:
- ? ?????????????? ??????? `mixed_workload_pipeline.axm` ??????????? built-in ??-???????? ???????? `Contains: 650`, `Str: 376`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`;
- ????? ????? opcode ?? ??? ?????????? ? `LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Jump`, `Convert`, ?? ???? built-in-???? ?????? ??? ???????? ??? ????????? ????????, ??? ????? ?????? ????????? ? ?????????.

?????? ????????? ???:
- ????? ??? ?? opcode-, ? ?????????? ?????? ?? ????????? built-in `Contains`, `Replace`, `TrimWhitespace` ? `Str` ? `mixed_workload_pipeline.axm`, ????? ??????, ??? ?? ??? ????? ?? ?????? ???????, ? ???????? ?????????;
- ????? ????? ???? ????????? ????? ??? ?????? ? ???? ?????? ? ????????? ??? ?? ????? ????????: ???????? `mixed`, ???????? `string`, ?????? ???????? `compare-engines` ? ?????? ???????.

## Session 608 - 2026-05-01 21:48 - ????????? built-in ????????? ?????? ??? ??????????? ??? ??????? ????????

????????:
- ????? Session 607 ????????? ???? `CompareEqual` ? ???????????? ????????? ??? ??? ????, ? ? ???????? `mixed_workload_pipeline.axm` ????? built-in ???????? ????????? ?????????? `Contains`, `Str`, `Replace` ? `TrimWhitespace`;
- ?????? `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????, ??? ? ???? ???? ?? ??? ?? ?????? ? ????????? ??? ????????? ???????? ??????????: ?? ??? ???????? ?????? ? ?????????? ? dispatch built-in, ?? ?? ??????? ?????? ???????? ?????? `RuntimeValue`, ????? ????????? built-in ?????????? ?????????? ??? ??????? ????????;
- ???? ????????? ???????? ???????????? ???????????????? ????? ????????: ? `mixed_workload_pipeline.axm` ? `string_array_loop_pipeline.axm` ???? ????? ? `REPLACE(..., "  ", " ")`, `TRIM(...)` ? ?????????????? `STR(...)`, ??? ??????? ???? ??????? ???? ?? ?????? ??????, ???? ??????????? ??? ????????? ?????.

??? ????????:
- ? `src/Axiom.Runtime/Values/RuntimeValue.cs` ?????????:
  - ???????????? ????????? `RuntimeValue` ??? ????? `INTEGER` ? `LONG` ? ??? ?? ?????????, ??? ? ???????? ????;
  - ????????? ??????? ????????? ???????? ??? `""`, `"TRUE"` ? `"FALSE"`;
  - ?????????? ????? `ToDisplayRuntimeValue()`, ??????? ??? ?????? ?????????? ??????? `RuntimeValue`, ??? ????? ????? ? ?????? ?????????? ????, ? ??? ????????? ????? ?????? ????? ? ??????? ????????? ?????????????;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` ????????? ??????? built-in-?????? `EvaluateTrimWhitespaceBuiltin(...)`, `EvaluateLeftTrimWhitespaceBuiltin(...)`, `EvaluateRightTrimWhitespaceBuiltin(...)` ? `EvaluateReplaceBuiltin(...)`, ??????? ?????????? ???????? `RuntimeValue`, ???? ????????? ???????? ?? ?????????;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` `TRIM`, `LTRIM`, `RTRIM`, `REPLACE` ? `STR` ?????????? ?? ??? ??????? ????.

????????? ?????:
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - ???????;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - ???????;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - ???????, 521 ????;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - ???????, 668 ??????;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - ???????, 198 ??????;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-220700-mixed.txt` - ???????, runtime ratio `0.606x`, total ratio `0.815x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-220700-string.txt` - ???????, runtime ratio `0.653x`, total ratio `0.864x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 2 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-220700-full.txt` - ???????, ???? ????? ?? 9 ????????? ????????;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - ???????, ???????? opcode ? built-in ?? ????????? ?? ????????;
- ??????????? ?????? `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-221000-mixed.txt` - ???????, runtime ratio `0.640x`, total ratio `0.767x`;
- ??????????? ?????? `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-221000-string.txt` - ???????, runtime ratio `0.674x`, total ratio `0.788x`.

??? ??????? ?????????:
- ?? ??????? ?????????????? ??????? `mixed_workload_pipeline.axm` ??? ?????????????? ??????: ??? ??????? ??????? ??????? ????? ?????????? ??????????? ????? (`0.741x` runtime ? Session 607 ?????? `0.606x` ? `0.640x` ??????);
- ?? `string_array_loop_pipeline.axm` ?????? ????? ???????????: ??????? ??????? ???????? ??????? ??????????????, ?? ??????? ?? ???????? ????? ?? ??????? ? ??????????, ??? ?? `mixed`, ??????? ????? ??????? ???????? ?? ????? ??????? ???? ?????????, ? ?? ? ????? ?? ?????? ??????;
- ?????? ??????? `mixed_workload_pipeline.axm` ??????? ????? ??? ?? ?? ????? (`LoadLocal`, `PushConst`, `StoreLocal`, `Add`, `CallBuiltin`, `Jump`, `Convert`, `LoadArgument`, `JumpIfEqual`), ? ????? built-in ??-???????? ???????? `Contains`, `Str`, `Replace`, `Right`, `Insert`, `RegexReplaceModes`, `TrimWhitespace`, ??? ????????????: ??? ???????? ???????????? ????????? ?????, ? ?? ?????? ??? ?????????.

???????:
- ???????? ?????? ??? ????? ??? ?? ?????????? ?????? ???????? ??????????? ????????? built-in ? `STR`, ? ?? ??? ??????? ?????????? ??? ???? benchmark;
- ??????? ????? ???? ?????????? ???????? `RuntimeValue` ??? `STR`/`TRIM`/`REPLACE` ? ???????? ????????;
- ?? ???????????? ? `PushConst` ??? ??? ?????????? ???? `CompareEqual`, ?????? ??? ??????? ???????? ??????????????? ??????? ?????? ????? ????????? ???? ?? `Contains`, ???? ?? ????? ???????? ?????? `CallBuiltin`.

??? ???????:
- ?????????? built-in ? `mixed_workload_pipeline.axm` ??????? `Contains: 650`, ?? ??? ???? `Str: 376`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`;
- ?????? ???????? `compare-engines` ?? ??? ????? ?? `regex_log_analysis.axm` ? ?? ????? ????????? ????? `high_rank_metadata.axm`, ??????? ??? ?????? ?????????? ???? ??-???????? ?????? ??????? ???????? ???????, ??? ???????? ????? ????? ??? ?? ????.

?????? ????????? ???:
- ????????? ????????? ??????? ???? ????? Session 608: ???? ??? `Contains`, ???? ????? ?????? `CallBuiltin` ??? ?????? ???????? string-built-in;
- ????????? ????????? ??? ? ?????? ??????? ?? `mixed_workload_pipeline.axm`, ? ????? ???????????? ??? ?? `string_array_loop_pipeline.axm` ? ?????? ???????? `compare-engines`.

## Session 609 - 2026-05-01 22:02 - облегчён общий каркас CallBuiltin

Контекст:
- после Session 608 в `mixed_workload_pipeline.axm` крупнейшими built-in оставались `Contains`, `Str`, `Replace`, `Right`, `Insert`, `RegexReplaceModes`, `TrimWhitespace`;
- перед правкой было подтверждено, что `ExecuteBuiltinCall(...)` всё ещё использовал безусловное `ResolveLocation(...)`.

Что изменено:
- в `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` добавлены `NullaryNoLocation`, `UnaryNoLocation`, `BinaryNoLocation`, `TernaryNoLocation` и признак `RequiresLocation`;
- на эти варианты переведены `LEN`, `TRIM`, `LTRIM`, `RTRIM`, `REVERSE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `STR`, `ARRAYLENGTH`, `ARRAYISALLOCATED`, `ARRAYRANK`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен `BuiltinExecutionInfo[]`, а `ExecuteBuiltinCall(...)` и `ExecuteBuiltinFromArguments(...)` теперь вычисляют местоположение только при необходимости.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet build C:\Axiom\src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore` - успешно;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-build` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build` - успешно, 668 тестов;
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build` - успешно, 198 тестов;
- `...compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm"` - отчёты `220139` и `220225`, runtime ratio `0.740x` и `0.699x`;
- `...compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm"` - отчёт `220145`, runtime ratio `0.504x`;
- `...compare-engines --iterations 8 --warmup 2 --filter "regex_log_analysis.axm"` - отчёт `220233`, runtime ratio `0.884x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines` - успешно, полный набор из 9 сценариев проходит;
- `...repro-bytecode... mixed_workload_pipeline.axm` - `Instructions: 47080`, `CallBuiltin: 2925`.

Что показал результат:
- шаг дал честное удешевление общего built-in-диспетчера;
- на `mixed_workload_pipeline.axm` эффект получился умеренным и шумным, без нового крупного перелома;
- по профилю следующий потолок сидит уже не в каркасе вызова, а в `Contains`, `Replace`, `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes`.

Решения:
- оставить правку как общее удешевление `CallBuiltin`;
- не возвращаться в `PushConst`, `LoadLocal` и уже разобранный equality-слой без новой узкой гипотезы.

Точный следующий шаг:
- разобрать `Contains` как следующий предметный кандидат и затем, если понадобится, идти в `Replace`, `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes`.

## Session 610 - 2026-05-01 22:24 - `Contains` получил быстрые ветки для коротких строковых случаев

Контекст:
- после Session 609 в `docs/PLANS3.md` следующим предметным кандидатом был зафиксирован именно `Contains`, потому что общий каркас `CallBuiltin` уже был облегчён, а в профиле `mixed_workload_pipeline.axm` `Contains` всё ещё оставался самым частым строковым built-in;
- перед правкой было подтверждено, что `CONTAINS` в `BytecodeVm` уже не требует `TextLocation`, но внутри всё ещё безусловно идёт в общий `string.Contains(fragment, StringComparison.Ordinal)`.

Что изменено:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` `EvaluateContains(...)` получил быстрые ветки для пустого фрагмента, пустого текста, случая `fragment.Length > text.Length`, совпадения по ссылке, равной длины и односимвольного поиска;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` расширен тест поисковых built-in новыми граничными случаями `CONTAINS("", "x")`, `CONTAINS("Hello", "Hello")`, `CONTAINS("Hello", "Hello!")`, `CONTAINS("Hello", "o")`;
- в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` теми же случаями расширен compiled-тест, чтобы интерпретатор и байткодный путь продолжали совпадать по поведению.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 521 тест;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2224-mixed.txt` - успешно, runtime ratio `0.646x`, total ratio `0.830x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2224-string.txt` - успешно, runtime ratio `0.510x`, total ratio `0.730x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 2 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2224-full.txt` - успешно, полный набор из 9 сценариев проходит, но `mixed_workload_pipeline.axm` в короткой серии всё ещё шумит около паритета;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`.

Что показал результат:
- шаг оказался не просто логическим упрощением: на длинных адресных прогонах `mixed` и `string` виден заметный выигрыш байткода;
- при этом форма профиля почти не меняется, значит эффект действительно сидит в удешевлении самого `Contains`, а не в побочном сдвиге структуры нагрузки;
- короткий полный набор по-прежнему полезен как регрессия, но для выбора следующего шага всё ещё хуже длинных адресных прогонов из-за шума на `mixed` и малых сценариях.

Решения:
- считать узкий слой `Contains` исследованным и не возвращаться к нему без новой отдельной гипотезы;
- не обновлять manual и runnable `.axm`-примеры, потому что пользовательское поведение языка не менялось.

Что остаётся:
- следующий содержательный кандидат теперь уже не `Contains`, а соседняя строковая группа `Replace`, `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes`;
- при следующем шаге по-прежнему держать главным барьером `mixed_workload_pipeline.axm`, а `string_array_loop_pipeline.axm` использовать как соседний контрольный сценарий.

Точный следующий шаг:
- снять следующую узкую гипотезу уже по одному из built-in `Replace`, `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes` и повторить тот же набор проверок: `Runtime.Tests`, `Execution.Tests`, адресный `mixed`, адресный `string`, короткий полный `compare-engines` и свежий профиль `mixed`.
## Session 611 - 2026-05-01 22:36 - `Replace` получил быстрые no-op-ветки и был измерен отдельно

Контекст:
- после Session 610 слой `Contains` уже считался исследованным, а в профиле `mixed_workload_pipeline.axm` следующими заметными строковыми built-in оставались `Replace`, `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes`;
- просмотр самих smoke-сценариев показал, что `Replace` особенно правдоподобен как следующий кандидат: в `mixed_workload_pipeline.axm` и `string_array_loop_pipeline.axm` есть повторные циклы уплотнения `"  " -> " "`, где часть вызовов уже должна быстро завершаться без фактической замены.

Что изменено:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` `EvaluateReplaceBuiltin(...)` больше не уходит безусловно в общий `text.Replace(...)`:
  - пустой `oldText` по-прежнему даёт ту же runtime-диагностику `MB3007`;
  - если `oldText` и `newText` совпадают, сразу возвращается исходный `RuntimeValue`;
  - если исходный текст пуст, `oldText` длиннее текста или `IndexOf(oldText)` не находит совпадений, тоже сразу возвращается исходный `RuntimeValue`;
  - для односимвольной замены используется `string.Replace(char, char)`;
  - если замене подлежит вся строка целиком, результат берётся напрямую как `newText` без общего прохода по `Replace(string, string, Ordinal)`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` расширен тест `Execute_BuiltinReplaceUsesOrdinalCaseSensitiveRules()` случаями `REPLACE("hello", "he", "he")`, `REPLACE("hello", "hello", "done")`, `REPLACE("abba", "b", "c")`;
- в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` теми же случаями расширен compiled-тест для `REPLACE`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверки:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 522 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "mixed_workload_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2234-mixed.txt` - успешно, runtime ratio `0.678x`, total ratio `0.916x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 8 --warmup 2 --filter "string_array_loop_pipeline.axm" --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2234-string.txt` - успешно, runtime ratio `0.535x`, total ratio `0.722x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-build -- compare-engines --iterations 2 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-2234-full.txt` - успешно, весь набор из 9 сценариев проходит;
- первая попытка `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` упёрлась в конфликт записи в `C:\Axiom\.artifacts\obj\Axiom.Runtime\...` из-за параллельных .NET-команд;
- после этого профиль повторён последовательно той же командой и проходит успешно: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`.

Что показал результат:
- шаг подтверждается как полезное локальное удешевление `Replace`, особенно на адресных длинных прогонах;
- по `mixed_workload_pipeline.axm` эффект есть, но он более умеренный, чем у предыдущего шага по `Contains`, поэтому `Replace` лучше считать хорошим локальным улучшением, а не новым крупным переломом;
- короткий полный набор на этот раз тоже выглядит здоровее, чем после некоторых прежних шагов: `mixed_workload_pipeline.axm` уже ниже паритета и по runtime, и по total;
- форма профиля не меняется, значит этот шаг не меняет структуру исполнения, а только цену уже существующих вызовов `Replace`.

Решения:
- считать узкий слой `Replace` разобранным и не возвращаться к нему без новой отдельной гипотезы;
- отдельно зафиксировать вывод по процессу: .NET-прогоны для этого репозитория нельзя гонять параллельно, если они пишут в общий `obj`, иначе возникают конфликты записи.

Что остаётся:
- следующими содержательными кандидатами остаются `Right`, `Insert`, `TrimWhitespace`, `RegexReplaceModes`;
- для выбора следующего шага по-прежнему главнее длинные адресные прогоны `mixed` и `string`, чем короткий полный набор сам по себе.

Точный следующий шаг:
- взять одну следующую узкую гипотезу уже по `Right`, `Insert`, `TrimWhitespace` или `RegexReplaceModes` и повторить тот же набор проверок, но уже строго последовательными .NET-командами.
## Session 612 - 2026-05-01 22:42 - `Insert` получил быстрые ветки для префикса, суффикса и пустого фрагмента

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Edit.cs` и `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Edit.cs` `EvaluateInsert(...)` переписан на короткие ветки для пустого `fragment`, пустого `text`, вставки в начало и вставки в конец;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` сценарий `Execute_BuiltinInsertAndRemove()` расширен случаями `INSERT("", 5, "seed")` и `INSERT("abcd", 2, "")`;
- в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` `Execute_CompiledStringFillAndEditBuiltins()` расширен теми же граничными случаями, чтобы интерпретатор и байткодный путь оставались в одном контракте.

Какие проверки выполнены:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 522 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'mixed_workload_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-224112.txt`, runtime ratio `0.683x`, total ratio `0.827x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'string_array_loop_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-224123.txt`, runtime ratio `0.605x`, total ratio `0.783x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1 --warmup 1` - успешно, весь набор из 9 сценариев проходит, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-224134.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, профиль остаётся той же формы: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`.

Что показал результат:
- шаг дал полезное локальное удешевление на строковых сценариях и хорошо выглядит на длинных адресных прогонах;
- при этом форма профиля не изменилась, так что это именно уменьшение цены вызова `INSERT`, а не структурный сдвиг нагрузки;
- короткий полный `compare-engines` по-прежнему слишком шумный по `mixed_workload_pipeline.axm`, поэтому для выбора следующего шага важнее длинные адресные прогоны.

Что остаётся:
- следующими кандидатами на такой же узкий разбор остаются `Right`, `TrimWhitespace` и `RegexReplaceModes`.

Точный следующий шаг:
- взять одну следующую узкую гипотезу уже по `Right`, `TrimWhitespace` или `RegexReplaceModes`, снова прогнать последовательные `dotnet`-проверки и подтвердить эффект теми же адресными сценариями `mixed` и `string`.
## Session 613 - 2026-05-01 22:58 - `Right` перестал создавать новый `RuntimeValue` для полного возврата строки

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` добавлен `EvaluateRightBuiltin(RuntimeValue value, int count)`, который возвращает исходный `RuntimeValue`, если `count` покрывает всю строку, и создаёт новый только для реального хвостового среза;
- в `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` ветка `BuiltinFunctionId.Right` переключена на новый helper;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` добавлен сценарий `Execute_BuiltinRightHandlesExactLengthAndSingleCharacterSuffix()`;
- в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` `Execute_CompiledStringSliceAndTrimBuiltins()` расширен случаями `RIGHT("Hello", 5)` и `RIGHT("Hello", 1)`.

Какие проверки выполнены:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 523 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'mixed_workload_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-225727.txt`, runtime ratio `0.631x`, total ratio `0.813x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'string_array_loop_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-225745.txt`, runtime ratio `0.594x`, total ratio `0.782x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1 --warmup 1` - успешно, весь набор из 9 сценариев проходит, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-225755.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, профиль остаётся той же формы: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`.

Что показал результат:
- шаг дал полезное локальное удешевление именно на байткодном built-in `RIGHT` без изменения пользовательского поведения;
- длинные адресные прогоны выглядят лучше, чем после шага по `Insert`: особенно `mixed_workload_pipeline.axm` вернулся в более сильную выигрышную зону;
- форма opcode- и built-in-профиля не изменилась, значит это именно снижение цены существующего вызова, а не новая структурная оптимизация.

Что остаётся:
- следующими кандидатами на такой же узкий разбор остаются `TrimWhitespace` и `RegexReplaceModes`.

Точный следующий шаг:
- взять одну отдельную гипотезу уже по `TrimWhitespace` или `RegexReplaceModes` и повторить тот же последовательный набор проверок с подтверждением на `mixed` и `string`.
## Session 614 - 2026-05-01 23:06 - пакетно удешевлены trim-ветки, `INSERT`, `STARTSWITH`/`ENDSWITH` и разбор regex-режимов

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` `EvaluateTrimWhitespaceBuiltin`, `EvaluateLeftTrimWhitespaceBuiltin` и `EvaluateRightTrimWhitespaceBuiltin` переведены на собственный быстрый поиск границ пробельной обрезки, чтобы не создавать новую строку и не вызывать `Trim*()` там, где результат уже равен исходной строке;
- там же `EvaluateTrimWhitespace`, `EvaluateLeftTrimWhitespace`, `EvaluateRightTrimWhitespace` переписаны на тот же расчёт границ, а `EvaluateStartsWith` и `EvaluateEndsWith` получили короткие ветки для пустого фрагмента, слишком длинного фрагмента, совпадения по ссылке и случая равной длины;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Edit.cs` добавлен `EvaluateInsertBuiltin(RuntimeValue textValue, int start, RuntimeValue fragmentValue)`, чтобы на пустом фрагменте вернуть исходный `RuntimeValue`, а при пустом тексте вернуть уже готовый `fragmentValue`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` ветка `BuiltinFunctionId.Insert` переведена на новый helper;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs` добавлен `_regexModesCache`, чтобы повторные режимы вроде `"i"` не разбирать заново на каждом вызове, а `EvaluateRegexReplaceBuiltin(...)` теперь сразу возвращает исходный `RuntimeValue`, если `count == 0`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` расширены сценарии trim-вариантов и строкового поиска: добавлены неизменяющиеся строки, полностью пробельный `TRIM`, а также пустые, точные и слишком длинные фрагменты для `STARTSWITH`/`ENDSWITH`.

Какие проверки выполнены:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 523 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'mixed_workload_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-230539.txt`, runtime ratio `0.696x`, total ratio `0.840x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'string_array_loop_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-230551.txt`, runtime ratio `0.576x`, total ratio `0.763x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1 --warmup 1` - успешно, весь набор из 9 сценариев проходит, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-230601.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, профиль остаётся той же формы: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`.

Что показал результат:
- пакет дал хороший общий выигрыш на `string_array_loop_pipeline.axm` и не ломает полный сравнительный прогон;
- при этом на длинном адресном `mixed_workload_pipeline.axm` результат получился не лучше недавнего изолированного шага по `RIGHT`, то есть широкий пакет полезен как накопительное удешевление, но не как новый явный перелом по главному интегральному барьеру;
- профиль подтверждает, что структура нагрузки не меняется и основные горячие built-in остаются теми же.

Что остаётся:
- следующими наиболее предметными кандидатами остаются `RegexReplaceModes` и `TrimWhitespace`, уже как отдельные узкие шаги по главному сценарию `mixed`.

Точный следующий шаг:
- из оставшейся горячей пары взять сначала один наиболее частый и проверяемый тракт — `RegexReplaceModes` или `TrimWhitespace` — и повторить те же последовательные проверки с обязательным сравнением против текущих отчётов `...230539.txt` и `...230551.txt`.
## Session 615 - 2026-05-01 23:16 - `RegexReplaceModes` получил раннее определение no-op-замен

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs` `EvaluateRegexReplaceBuiltin(RuntimeValue ...)` переведён на путь, который после валидации `count` и создания `Regex` может вернуть исходный `RuntimeValue`, если литеральная замена без ссылок на группы фактически ничего не меняет;
- в том же файле `EvaluateRegexReplaceCore(...)` и вспомогательные пути `EvaluateRegexReplaceWithCachedRegex(...)` разделены так, чтобы `Regex` не создавался повторно и no-op-проверка выполнялась до тяжёлого `regex.Replace(...)`;
- добавлены helper’ы `ValidateRegexReplaceCount(...)`, `IsRegexReplaceGuaranteedNoOp(...)` и `ReplacementDependsOnMatch(...)`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` расширены regex-тесты двумя no-op-сценариями: отсутствие совпадений и совпадение, уже равное строке замены.

Какие проверки выполнены:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 523 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'mixed_workload_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-231535.txt`, runtime ratio `0.659x`, total ratio `0.785x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'string_array_loop_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-231549.txt`, runtime ratio `0.528x`, total ratio `0.717x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1 --warmup 1` - успешно, весь набор из 9 сценариев проходит, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-231558.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, профиль остаётся той же формы: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`.

Что показал результат:
- шаг заметно лучше предыдущего широкого пакета по главному сценарию `mixed_workload_pipeline.axm` и одновременно улучшает соседний строковый сценарий;
- особенно полезно, что `regex_modes_pipeline.axm` в коротком полном прогоне уже оказывается ниже паритета, то есть шаг удешевляет не только смешанный сценарий, но и профильный regex-конвейер;
- при этом структура нагрузки не меняется: это именно снижение цены вызова `RegexReplaceModes`, а не новый байткодный шаблон.

Что остаётся:
- следующим узким кандидатом остаётся `TrimWhitespace`.

Точный следующий шаг:
- взять `TrimWhitespace` как отдельный следующий разгон, снова прогнать последовательные проверки и сравнить результат прежде всего с текущими отчётами `...231535.txt` и `...231549.txt`.
## Session 616 - 2026-05-01 23:22 - `TRIM` восстановил правильную обработку хвостовых пробелов и получил новые контрольные замеры

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` исправлен ранний возврат в `EvaluateTrimWhitespace(...)` и `EvaluateTrimWhitespaceBuiltin(...)`: строка больше не считается неизменной только потому, что у неё нет ведущих пробелов;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены отдельные сценарии `TRIM("hello   ")` и `TRIM("   hello")`, чтобы покрыть оба односторонних пробельных случая;
- обновлён runnable-пример `samples/functions/trim/basic_usage.axm`;
- обновлена страница `docs/manual/string-functions-trim-and-case.html`: в секцию `TRIM` добавлен прямой пример с правой обрезкой, левой обрезкой и полностью пробельной строкой.

Какие проверки выполнены:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` - успешно, 523 теста;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` - успешно, 668 тестов;
- `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj --no-restore` - успешно;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'mixed_workload_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-232113.txt`, runtime ratio `0.796x`, total ratio `0.883x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 8 --warmup 2 --filter 'string_array_loop_pipeline.axm'` - успешно, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-232127.txt`, runtime ratio `0.572x`, total ratio `0.764x`;
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --iterations 1 --warmup 1` - успешно, весь набор из 9 сценариев проходит, отчёт `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260501-232138.txt`;
- `dotnet run --project C:\Axiom\.artifacts\repro-bytecode\repro-bytecode.csproj -- profile 0 0 samples\smoke\engine-compare\mixed_workload_pipeline.axm` - успешно, профиль остаётся той же формы: `Instructions: 47080`, `CallBuiltin: 2925`, `Contains: 650`, `Replace: 376`, `Right: 192`, `Insert: 188`, `RegexReplaceModes: 188`, `TrimWhitespace: 188`.

Что показал результат:
- шаг в первую очередь восстановил правильное пользовательское поведение `TRIM`, которое было испорчено прошлой внутренней оптимизацией;
- по скорости `string_array_loop_pipeline.axm` остаётся в хорошей выигрышной зоне, а `mixed_workload_pipeline.axm` после исправления контракта выглядит более шумным и умеренным, чем после шага по `RegexReplaceModes`;
- это полезно зафиксировать явно: корректность здесь важнее красивой локальной цифры, и новые замеры надо сравнивать уже с учётом восстановленного поведения `TRIM`.

Что остаётся:
- следующий шаг лучше выбирать уже не инерционно по старой верхушке, а по новому измерению `mixed_workload_pipeline.axm` после восстановления `TRIM`.

Точный следующий шаг:
- снять свежий ориентир по главным горячим built-in и opcode в `mixed_workload_pipeline.axm`, затем выбрать следующий узкий предметный кандидат уже по этим обновлённым цифрам.

## Session 617 - 2026-05-02 07:24 - введён постоянный реестр покрытия Bytecode VM

Что сделано:
- заново прочитан обязательный активный контекст проекта: AGENTS.md, PROJECT.md, README.md, docs/SPECIFICATION3.md, docs/ARCHITECTURE.md, docs/PLANS3.md, docs/WORKLOG4.md;
- собран и зафиксирован новый постоянный реестр docs/BYTECODE_COVERAGE.md для покрытия BytecodeVm по пользовательским операторам, встроенным функциям и связанным проверкам;
- в реестр внесены текущие укрупнённые строки по операторам, массивным built-in, строковым built-in, regex-группе, числовым built-in, дате/времени и файловым built-in;
- в реестр сразу добавлены дополнительные рабочие столбцы, которых не хватало в простом списке реализовано / тесты / sample / manual / паритет: сквозной запуск через VM без fallback, отрицательный паритет диагностики и профильный статус;
- AGENTS.md обновлён так, чтобы docs/BYTECODE_COVERAGE.md входил в обязательный стартовый контекст и в критерии готовности задач, которые меняют покрытие байткода, sample/manual-покрытие, паритет или профильный статус.

Изменённые файлы:
- AGENTS.md
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменены только процессные и учётные документы без правок исполняемого кода.

Решение:
- считать docs/BYTECODE_COVERAGE.md обязательным рабочим реестром проекта для всех следующих задач по BytecodeVm и пользовательскому языковому покрытию;
- больше не держать состояние покрытия VM только в разрозненных записях PLANS3 и WORKLOG4.

Следующий шаг:
- при каждом следующем шаге по языку и BytecodeVm обновлять docs/BYTECODE_COVERAGE.md в той же задаче;
- постепенно разукрупнять широкие строки реестра и доводить столбцы Негативный паритет, Manual и Sample от группового статуса частично до адресных подтверждений.

## Session 618 - 2026-05-02 08:00 - строковая группа в реестре получила адресные подтверждения

Что сделано:
- продолжена работа уже не по оптимизации кода, а по доведению нового реестра `docs/BYTECODE_COVERAGE.md` до практического рабочего состояния;
- для строковых built-in в реестре уточнены строки `LEN`/`LEFT`/`RIGHT`/`MID`, trim/case, `CONTAINS`/`STARTSWITH`/`ENDSWITH`/`INSTR`/`INSTRREV`/`SUBSTRCOUNT`, `REPLACE`/`INSERT`/`REMOVE`, `REPEAT`/`SPACE`/`STRING`/`PADLEFT`/`PADRIGHT`/`CENTER`, `ASC`/`CHR`, `SPLIT`/`JOIN` и `REGEXREPLACE`;
- вместо общих пометок туда добавлены ссылки на конкретные тестовые точки: `Execute_BuiltinTrimVariants()`, `Execute_BuiltinStringUsesFirstFillCharacter()`, `Execute_BuiltinStringReportsEmptyFill()`, `Execute_CompiledStringSliceAndTrimBuiltins()`, `Execute_CompiledStringSearchBuiltins()`, `Execute_CompiledStringFillAndEditBuiltins()`, `Execute_CompiledStringCharBuiltins()`, `Execute_CompiledStringArrayBuiltins()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`, `Run_WithBytecodeMode_UsesBytecodeVmForStringArrayBuiltins()` и `Run_WithBytecodeMode_UsesBytecodeVmForRegexScalarBuiltins()`;
- в тех же строках реестра добавлены конкретные manual-страницы `docs/manual/string-functions-slices-and-length.html`, `docs/manual/string-functions-trim-and-case.html`, `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions-build-and-split.html`, `docs/manual/regex-functions-replace-and-count.html` и тематические папки `samples/functions/`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этом шаге менялись только рабочие документы и реестр покрытия без правок исполняемого кода.

Решение:
- считать строковую группу первым доведённым примером того, как `docs/BYTECODE_COVERAGE.md` должен выглядеть в рабочем режиме: не только статусы, но и конкретные адреса подтверждения;
- при следующих задачах в реестре придерживаться той же плотности привязки, а не возвращаться к чисто обзорным формулировкам.

Следующий шаг:
- продолжить разукрупнение следующей крупной группы в `docs/BYTECODE_COVERAGE.md`, где пока ещё преобладают широкие укрупнённые строки со статусом `частично`;
- наиболее полезными кандидатами на следующий проход держать массивные built-in или файловые built-in.

## Session 619 - 2026-05-02 08:07 - массивные built-in в реестре получили адресные подтверждения

Что сделано:
- продолжено доведение `docs/BYTECODE_COVERAGE.md` до рабочего адресного формата уже по массивной группе;
- для metadata-подгруппы добавлены явные ссылки на тесты `Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()`, `Execute_ArrayBoundsReportsUnallocatedArrayState()`, `Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()` и `Run_WithBytecodeMode_UsesBytecodeVmForArrayMetadataBuiltins()`;
- для pure- и slice-подгруппы добавлены конкретные тесты по `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTAKE`, `ARRAYSKIP`, включая многомерные формы и execution-путь `Run_WithBytecodeMode_UsesBytecodeVmForPureArrayBuiltins()`;
- для групп `ARRAYTOREVERSED`/`ARRAYREVERSE`/`ARRAYDISTINCT`/`ARRAYFLATTEN`/`ARRAYRESHAPE`, поисковых built-in, `ARRAYFILL`/`ARRAYSORT`/`ARRAYTOSORTED`, `ARRAYPUSH`/`ARRAYPOP`/`ARRAYINSERT`/`ARRAYREMOVE` и callback-built-in добавлены адресные ссылки на VM-тесты, execution-тесты, central manual-страницы и соответствующие папки `samples/functions/`;
- в качестве manual-опор для массивной группы в реестре теперь прямо указаны `docs/manual/arrays-functions.html`, `docs/manual/arrays-slices.html`, `docs/manual/arrays-diagnostics.html` и `docs/manual/arrays-multidimensional.html`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этом шаге менялись только рабочие документы и реестр покрытия без правок исполняемого кода.

Решение:
- считать массивную группу вторым доведённым образцом адресного реестра после строковой;
- дальше придерживаться того же стандарта: широкие строки допустимы, но в примечании у них должны быть конкретные точки подтверждения, а не только общий комментарий.

Следующий шаг:
- продолжить тот же формат уточнения для следующей крупной группы в `docs/BYTECODE_COVERAGE.md`;
- наиболее полезным следующим кандидатом держать файловые built-in, потому что там ещё остаётся много укрупнённых строк без такой же плотной адресной привязки.

## Session 620 - 2026-05-02 08:09 - файловые built-in в реестре получили адресные подтверждения и возвращён `DELETEDIRRECURSIVE`

Что сделано:
- продолжено адресное уточнение `docs/BYTECODE_COVERAGE.md` уже по файловой группе;
- для open-file built-in добавлены конкретные ссылки на тесты `Execute_FileStateBuiltins()`, `Execute_FilePositionAndLengthForInput()`, `Execute_OpenReadLineAndEof()`, `Execute_CompiledOpenInputFileProgram()`, `Execute_CompiledOpenOutputAndAppendFileProgram()` и execution-путь `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`;
- для whole-file built-in, path/info built-in, перечислений файлов и папок, а также системной drive/path-группы добавлены адресные ссылки на интерпретаторные и VM-тесты вроде `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()` и `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`;
- в управленческую строку файловых операций в реестре возвращён `DELETEDIRRECURSIVE`: выяснилось, что он уже поддержан в manual, поисковом индексе и `samples/functions/deletedirrecursive/`, но отсутствовал в самом реестре покрытия;
- для всех файловых строк в реестре теперь указаны точные manual-страницы `docs/manual/file-system-functions-open-files.html`, `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions.html` и соответствующие папки `samples/functions/`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этом шаге менялись только рабочие документы и реестр покрытия без правок исполняемого кода.

Решение:
- считать файловую группу третьим доведённым адресным блоком реестра после строк и массивов;
- считать явное возвращение `DELETEDIRRECURSIVE` в реестр обязательной синхронизацией источников правды, а не побочной косметической правкой.

Следующий шаг:
- продолжить тот же формат уточнения для ещё одной укрупнённой функциональной группы в `docs/BYTECODE_COVERAGE.md`;
- наиболее полезными следующими кандидатами держать date/time built-in или числовые built-in.

## Session 621 - 2026-05-02 08:35 - date/time built-in в реестре получили адресные подтверждения

Что сделано:
- продолжено адресное уточнение `docs/BYTECODE_COVERAGE.md` по дато-временной группе;
- для подгруппы текущих значений добавлены ссылки на тесты `Execute_DateTimeBuiltinsReturnCurrentValues()`, `Execute_TimerReturnsNonNegativeNumber()`, `Execute_CompiledDateTimeCurrentBuiltins()` и execution-путь `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`;
- для serial/parts- и arithmetic/validation-подгрупп добавлены конкретные ссылки на `Execute_DateTimeSerialAndPartsBuiltins()`, `Execute_DateTimePartsRejectNonCanonicalDates()`, `Execute_DateTimeArithmeticAndValidationBuiltins()`, `Execute_DateTimeArithmeticRejectsUnsupportedInterval()`, `Execute_TimeValueRejectsNonCanonicalInput()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()` и `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`;
- для UTC/timezone- и calendar-helper-подгруппы добавлены адресные опоры на `Execute_DateTimeUtcBuiltins()`, `Execute_DateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_DateTimeUtcBuiltinsSupportSeasonalNamedTimeZones()`, `Execute_DateTimeCalendarHelpers()`, а также на соответствующие VM- и execution-тесты;
- в реестр для всех date/time-строк добавлены точные manual-страницы `docs/manual/date-time-functions-current.html`, `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/date-time-functions-time-zones.html` и тематические папки `samples/functions/`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этом шаге менялись только рабочие документы и реестр покрытия без правок исполняемого кода.

Решение:
- считать date/time-группу четвёртым доведённым адресным блоком реестра после строк, массивов и файловой системы;
- продолжать придерживаться того же стандарта: укрупнённые строки допустимы, если в примечании есть явные подтверждающие тесты, manual и sample-пути.

Следующий шаг:
- продолжить тот же подход по ещё одной укрупнённой функциональной группе в `docs/BYTECODE_COVERAGE.md`;
- наиболее полезным следующим кандидатом держать числовые built-in.

## Session 622 - 2026-05-02 08:39 - числовые built-in в реестре получили адресные подтверждения

Что сделано:
- продолжено адресное уточнение `docs/BYTECODE_COVERAGE.md` по числовой группе;
- для conversion/legacy-подгруппы добавлены ссылки на `Execute_BuiltinStrAndVal()`, `Execute_BuiltinValReturnsZeroForInvalidText()`, `Execute_BuiltinIntReportsOverflowAfterFloor()`, `Execute_CompiledNumericConversionAndRoundingBuiltins()` и execution-путь `Run_WithBytecodeMode_UsesBytecodeVmForAutomaticNumericConversions()`;
- для core-подгруппы добавлены адресные опоры на `Execute_BuiltinRndReturnsDoubleInUnitInterval()`, `Execute_MathBuiltinsReturnExpectedOutput(...)`, `Execute_BuiltinSetSignPreservesDecimalType()`, `Execute_CompiledRndReturnsValueInHalfOpenRange()` и `Execute_CompiledNumericMathBuiltins()`;
- для тригонометрической и roots/rounding-групп добавлены конкретные ссылки на доменные ошибки, переполнения и VM/Execution-проверки: `Execute_MathBuiltinsReportDomainErrors(...)`, `Execute_HyperbolicMathBuiltinsReportDoubleOverflow()`, `Execute_MathBuiltinsReportDoubleOverflow()`, `Execute_BuiltinFixDiffersFromIntForNegativeFraction()`, `Execute_CompiledNumericBuiltinsReportArgumentErrors()` и `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`;
- для всех числовых строк в реестре добавлены точные manual-страницы `docs/manual/numeric-functions-conversion-and-legacy.html`, `docs/manual/numeric-functions-core.html`, `docs/manual/numeric-functions-trigonometry.html`, `docs/manual/numeric-functions-roots-rounding.html`, `docs/manual/numeric-functions.html` и тематические папки `samples/functions/`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- сборка и тесты не запускались, потому что в этом шаге менялись только рабочие документы и реестр покрытия без правок исполняемого кода.

Решение:
- считать числовую группу пятым доведённым адресным блоком реестра после строк, массивов, файловой системы и date/time;
- считать крупную стадию разукрупнения реестра по основным функциональным разделам в целом завершённой.

Следующий шаг:
- перейти от разукрупнения больших групп к точечной досверке строк, где всё ещё стоит `частично` прежде всего по столбцу `Негативный паритет`;
- в отдельной служебной задаче рассмотреть полуавтоматический аудит на рассинхронизацию между built-in-списком, VM-диспетчеризацией, `samples/functions/` и manual-индексом.

## Session 623 - 2026-05-02 08:47 - добавлен аудит покрытого байткодом набора built-in и найден дрейф manual-индекса по ARRAY-группе

Что сделано:
- в `src/Axiom.ManualChecks/Program.cs` добавлена новая команда `audit-bytecode-coverage`, а в `src/Axiom.ManualChecks/BytecodeCoverageAudit.cs` вынесена логика полуавтоматического аудита покрытого байткодом набора built-in;
- аудит собирает объявленные built-in-семейства из семантических дескрипторов, проверяет для каждого варианта поддержку через `BytecodeVm.SupportsBuiltin(...)`, сверяет наличие каталога в `samples/functions/` и наличие function-entry в `docs/manual/scripts/search-index.js`;
- для manual-индекса добавлен отдельный построчный разбор `search-index.js`, чтобы отчёт не путал разделы и функции и давал именно список отсутствующих function-entry;
- первый рабочий отчёт записан в `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-coverage-audit-20260502-084637.txt`;
- отчёт подтвердил, что у `BytecodeVm` нет дыр по объявленным вариантам built-in, а `samples/functions/` полностью синхронизированы с семантическим списком;
- тем же отчётом зафиксирован реальный дрейф по discoverability manual: в `docs/manual/scripts/search-index.js` пока отсутствуют function-entry для части ARRAY-группы, в том числе `ARRAYCLONE`, `ARRAYCONCAT`, `ARRAYFLATTEN`, `ARRAYRESHAPE`, `ARRAYFILL`, `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE`, `ARRAYREVERSE`, `ARRAYSORT`, `ARRAYTOSORTED`, `ARRAYTOREVERSED`, `ARRAYTAKE`, `ARRAYSKIP`, `ARRAYDISTINCT`, `ARRAYBINARYSEARCH`, `ARRAYCONTAINS`, `ARRAYCOUNT`, `ARRAYFINDALL`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`.

Изменённые файлы:
- src/Axiom.ManualChecks/Program.cs
- src/Axiom.ManualChecks/BytecodeCoverageAudit.cs
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнена последовательная сборка `dotnet build C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj`;
- выполнен рабочий прогон `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- audit-bytecode-coverage`;
- итоговый отчёт сохранён в `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-coverage-audit-20260502-084637.txt`.

Решение:
- считать полуавтоматический аудит встроенным служебным инструментом проекта для дальнейшей синхронизации списка built-in, VM-покрытия, sample-слоя и manual-индекса;
- считать следующий обнаруженный долг не байткодным, а справочно-навигационным: нужно закрыть отсутствующие ARRAY-entry в `docs/manual/scripts/search-index.js` и отдельно решить, какие ссылки временно вести на старую обзорную страницу `docs/manual/arrays-and-builtins.html`, а какие уже переносить в текущие страницы массива.

Следующий шаг:
- обновить `docs/manual/scripts/search-index.js` по недостающим ARRAY built-in и затем повторно прогнать `audit-bytecode-coverage`;
- после этого использовать новый аудит как обязательную служебную проверку при следующих задачах по built-in, Bytecode VM и покрытию реестра.

## Session 624 - 2026-05-02 08:50 - ARRAY-группа добавлена в manual-индекс и аудит built-in-покрытия стал зелёным

Что сделано:
- в `docs/manual/scripts/search-index.js` добавлены недостающие function-entry для ARRAY-группы: `ARRAYSLICE`, `ARRAYCLONE`, `ARRAYCONCAT`, `ARRAYFLATTEN`, `ARRAYRESHAPE`, `ARRAYFILL`, callback-подгруппа, search-подгруппа, а также `ARRAYREVERSE`, `ARRAYSORT`, `ARRAYTOSORTED`, `ARRAYTOREVERSED`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYDISTINCT`;
- для `ARRAYSLICE` индекс теперь указывает на актуальный адрес `arrays-functions.html#arrayslice`, а для функций, которые пока ещё фактически описаны через старую обзорную страницу `docs/manual/arrays-and-builtins.html`, поиск теперь хотя бы честно выдаёт функцию и ведёт на существующий справочный материал;
- повторно запущен `audit-bytecode-coverage`, и новый отчёт в `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-coverage-audit-20260502-085012.txt` завершился полным `PASS`;
- итоговый зелёный отчёт подтвердил синхронизацию всех четырёх слоёв: семантического списка built-in, поддержки `BytecodeVm`, `samples/functions/` и function-entry manual-индекса.

Изменённые файлы:
- docs/manual/scripts/search-index.js
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнён повторный прогон `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- audit-bytecode-coverage`;
- итоговый отчёт сохранён в `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-coverage-audit-20260502-085012.txt` и завершился статусом `PASS`.

Решение:
- считать discoverability-слой manual по built-in-составу синхронизированным с текущим языком, sample-слоем и байткодным исполнением;
- считать следующий оставшийся долг по ARRAY-теме уже не синхронизационным, а редакционным: часть функций всё ещё опирается на старую обзорную страницу `docs/manual/arrays-and-builtins.html`, и это нужно разбирать отдельно от задач по покрытию Bytecode VM.

Следующий шаг:
- вернуться к точечной досверке строк `docs/BYTECODE_COVERAGE.md`, где по-прежнему стоит `частично`, прежде всего по столбцу `Негативный паритет`;
- отдельной задачей позднее решить, как структурно разнести ARRAY-функции со старой обзорной страницы по текущим страницам manual и навигации.

## Session 625 - 2026-05-02 09:36 - отрицательный паритет built-in-групп в реестре переведён с частичного на адресно подтверждённый

Что сделано:
- в `docs/BYTECODE_COVERAGE.md` пересмотрен столбец `Негативный паритет` для built-in-групп, и там, где ранее стояло `частично` только из-за неявной групповой уверенности, статус переведён в `да` на основании уже существующих адресных тестов;
- по массивным built-in в примечания добавлены явные отрицательные VM- и execution-опоры: `Execute_CompiledPureArrayBuiltinsReportArgumentErrors()`, `Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()`, `Execute_CompiledMutatingArrayBuiltinsReportUnallocatedSource()`, `Execute_CompiledMutatingArrayBuiltinsReportEmptyPop()`, `Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()`, `Execute_CompiledArraySearchBuiltinsReportUnallocatedSource()`, `Execute_CompiledArrayCallbackBuiltinsReportEmptyFunctionValue()`, а также `Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()`, `Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError()` и `Run_WithBytecodeMode_ReportsArraySearchBuiltinRuntimeError()`;
- по строковым и regex-группам добавлены явные отрицательные опоры `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Execute_CompiledChrReportsInvalidCode()`, `Execute_CompiledStringArrayBuiltinReportsEmptySeparator()`, `Execute_CompiledRegexScalarBuiltinReportsInvalidArgument()`, `Execute_CompiledRegexArrayBuiltinReportsInvalidMode()`, а также execution-тесты `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`, `Run_WithBytecodeMode_ReportsStringArrayBuiltinArgumentError()`, `Run_WithBytecodeMode_ReportsRegexScalarBuiltinArgumentError()` и `Run_WithBytecodeMode_ReportsRegexArrayBuiltinArgumentError()`;
- по числовым, date/time- и файловым built-in-группам добавлены такие же адресные опоры через `Execute_CompiledNumericBuiltinsReportArgumentErrors()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Execute_CompiledOpenFileReportsClosedFileRead()` и соответствующие execution-проверки `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()` и `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()`.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- дополнительные сборка и тесты не запускались, потому что этот шаг был документным и опирался на уже существующие адресные автотесты в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` и `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`.

Решение:
- считать built-in-слой реестра по отрицательному паритету в основном доведённым до адресного состояния;
- считать следующий рабочий остаток по столбцу `Негативный паритет` уже в первую очередь операторным и языкоформенным, а не built-in-слоем.

Следующий шаг:
- пройти следующим крупным блоком по языковым формам и операторам, где `Негативный паритет` всё ещё стоит как `частично`: выражения, объявления, ввод-вывод, файловые операторы, ветвления, циклы, процедуры, замыкания и пользовательские типы;
- отдельно не забывать про будущую редакционную консолидацию `docs/manual/arrays-and-builtins.html`, но держать её вне задач по покрытию отрицательного паритета.

## Session 626 - 2026-05-02 09:41 - операторный слой coverage-реестра доведён до адресного состояния, кроме отдельного хвоста по замыканиям

Что сделано:
- в `docs/BYTECODE_COVERAGE.md` пересмотрены верхние строки по операторам и языковым формам, и там, где раньше стояло широкое `частично`, теперь добавлены конкретные отрицательные опоры и выставлен статус `да`;
- по выражениям добавлены адресные опоры на `Execute_ReportsDivisionByZero()`, `Execute_ReportsDivisionByZeroForIntegralOperators(...)`, `Execute_ReportsIntegerArithmeticOverflow()`, `Execute_ReportsLongArithmeticOverflow()`, `Execute_ReportsDoubleArithmeticOverflow()`, `Execute_CompiledProgramReportsDivisionByZeroWithSourceLocation()`, `Execute_CompiledIntegralDivisionReportsDivisionByZero()`, `Run_ReturnsRuntimeFailure()`, `Run_ReturnsRuntimeFailureForNumericOverflow(...)` и `Run_ReturnsSemanticFailureForBooleanOrderedComparison()`;
- по `DIM`/`CONST` и массивному объявлению добавлены адресные semantic- и runtime-опоры через `Run_ReturnsSemanticFailureAndDoesNotExecute()`, acceptance-ошибки `undeclared_variable`, `dim_requires_type_or_initializer`, `fixed_string_error`, а также interpreter/VM-тесты `Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_RedimRejectsNonCanonicalEmptyRange()`, `Execute_DimRejectsMinimumIntegerLowerBound()`, `Execute_CompiledRedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_CompiledEraseOnArrayViewReportsBuiltinArgumentDiagnostic()` и `Execute_CompiledUnallocatedArrayAccessReportsRuntimeState()`;
- по `PRINT USING`, `INPUT`, `OPEN`/`CLOSE`, ветвлениям, циклам, процедурам/функциям и `TYPE` в реестр добавлены соответствующие адресные отрицательные тесты из `InterpreterTests`, `BytecodeVmTests`, `ExecutionPipelineTests` и `AcceptanceProgramTests`;
- единственным осознанно оставленным `частично` в верхнем слое осталась строка по замыканиям и захвату внешних локалей: позитивное покрытие и VM-путь есть, но отдельная адресная отрицательная проверка именно по этой сущности пока не выделена.

Изменённые файлы:
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- дополнительные сборка и тесты не запускались, потому что шаг был документным и опирался на уже существующие адресные тесты в `tests/Axiom.Runtime.Tests/InterpreterTests.cs`, `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`, `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` и `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`.

Решение:
- считать отдельную фазу приведения `docs/BYTECODE_COVERAGE.md` в актуальное рабочее состояние по сути завершённой;
- дальше обновлять реестр только вместе с реальными задачами по коду, а не продолжать самостоятельные проходы по документу.

Следующий шаг:
- вернуться к ускорению `BytecodeVm` и снова брать следующий горячий участок по свежим профилям `mixed_workload_pipeline.axm` и `string_array_loop_pipeline.axm`;
- отдельно в хвосте оставить только два не-блокирующих остатка: редакционную консолидацию `docs/manual/arrays-and-builtins.html` и адресную отрицательную проверку по замыканиям.

## Session 627 - 2026-05-02 09:52 - пакетное ускорение no-location built-in и строковых identity-путей в BytecodeVm

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.cs` переработан внутренний слой диспетчеризации built-in: для `Nullary/Unary/Binary/TernaryNoLocation` VM теперь использует отдельные arity-варианты и прямые no-location делегаты без фиктивного `TextLocation` и без лишней переходной обвязки;
- на этот путь переведены несколько частых scalar built-in без использования location: `TRIM`-варианты по символам, `REVERSE`, `INSTR`, `INSTRREV`, `JOIN`, `VAL`, `LCASE`, `UCASE` и смежные no-location-вызовы;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Fill.cs` добавлены быстрые возвраты исходного `RuntimeValue` для `PADLEFT`, `PADRIGHT` и `CENTER`, когда ширина не требует изменения строки, при сохранении прежней валидации `fill`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs` добавлены быстрые built-in-пути для `REVERSE`, `LCASE`, `UCASE` и `JOIN` с возвратом исходного значения в no-op-случаях и одноэлементном массиве;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` и `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` расширены проверки на no-op-случаи `PADLEFT`/`PADRIGHT`/`CENTER` и на одноэлементный `JOIN`.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs
- src/Axiom.Runtime/Execution/BytecodeVm.cs
- src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs
- src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Fill.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 3 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-1.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 3 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-1.txt`
  - повторный контроль `mixed`: `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-2.txt`

Результат замеров:
- `string_array_loop_pipeline.axm`: `0.514x` runtime и `0.718x` total в `axiom-engine-compare-string-20260502-1.txt`;
- `mixed_workload_pipeline.axm`: на коротком прогоне `0.793x` runtime и `0.896x` total, на повторном прогоне из 5 итераций `0.700x` runtime и `0.891x` total в `axiom-engine-compare-mixed-20260502-2.txt`;
- профиль эффекта получился честно смешанным: строковый сценарий заметно выиграл, а `mixed` улучшился не так резко и остаётся шумным, хотя всё ещё устойчиво быстрее интерпретатора.

Решение:
- считать шаг удачным как системное снижение накладных расходов `CallBuiltin` и как полезное усиление строкового горячего пути;
- считать, что следующий прирост уже логичнее искать не в очередной одиночной строковой функции, а в следующем общем слое частых scalar built-in или в другой горячей группе `mixed`.

Следующий шаг:
- продолжить ускорение `BytecodeVm` следующим пакетным шагом по общему пути built-in, а не возвращаться к документным проходам;
- отдельно проверить, какие ещё частые built-in можно безопасно перевести на прямой no-location-вызов, и не сместился ли главный остаточный узкий участок `mixed` в массивные callback-builtins или ветвления.

## Session 628 - 2026-05-02 09:57 - no-location-путь расширен на текущие date/time built-in и простые предикаты

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` на `NullaryNoLocation` переведены `RND`, `DATE`, `TIME`, `NOW`, `UTCDATE`, `UTCTIME`, `UTCNOW`, `TIMER`, `UTCOFFSET`;
- там же на `UnaryNoLocation` переведены `ISDATE` и `ISOPEN`;
- обновлены рабочие документы `docs/BYTECODE_COVERAGE.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать новый охват и контрольные замеры.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-3.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-2.txt`

Результат замеров:
- `mixed_workload_pipeline.axm`: `0.703x` runtime и `0.882x` total в `axiom-engine-compare-mixed-20260502-3.txt`;
- `string_array_loop_pipeline.axm`: `0.599x` runtime и `0.798x` total в `axiom-engine-compare-string-20260502-2.txt`;
- шаг оказался скорее стабилизирующим и системным: сильного нового скачка нет, но общий путь built-in ещё немного облегчается без потери корректности.

Решение:
- считать пакет полезным продолжением общей стратегии снятия накладных расходов `CallBuiltin`;
- считать, что следующий заметный прирост уже может лежать либо в следующем остатке scalar built-in, либо за пределами built-in-диспетчеризации как таковой.

Следующий шаг:
- продолжить анализ оставшихся scalar built-in, где `location` пока ещё протаскивается формально;
- если эффект на `mixed` перестанет меняться, переключиться на другой общий участок `BytecodeVm`, а не продолжать бесконечную микроразметку строкового слоя.

## Session 629 - 2026-05-02 10:03 - быстрые сравнения и условные переходы упрощены для смешанного сценария

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` быстрый путь сравнительных переходов переведён на прямой булев helper: `ExecuteComparisonJumpInstruction(...)` больше не создаёт промежуточный `RuntimeValue(Boolean)` и не извлекает его обратно через `GetBooleanUnchecked(...)`;
- на тот же helper переведён `ExecuteCompareGreaterInstruction(...)`;
- helper быстрых сравнений унифицирован: добавлена явная проверка совпадения типов и переход на unchecked-доступоры (`GetIntegerUnchecked`, `GetLongUnchecked`, `GetDoubleUnchecked`, `GetDecimalUnchecked`, `GetStringUnchecked`) вместо более тяжёлых `As...()` в самом горячем пути;
- старый `TryEvaluateHotCompareGreater(...)` после этого удалён как лишний.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-4.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-3.txt`

Результат замеров:
- `mixed_workload_pipeline.axm`: `0.711x` runtime и `0.834x` total в `axiom-engine-compare-mixed-20260502-4.txt`;
- `string_array_loop_pipeline.axm`: `0.617x` runtime и `0.809x` total в `axiom-engine-compare-string-20260502-3.txt`;
- шаг хорошо попал именно в общий управляющий слой `mixed`: runtime меняется умеренно, зато total-отношение стало заметно лучше относительно предыдущего замера `0.882x`.

Решение:
- считать это первым осмысленным выходом за пределы built-in-диспетчеризации в сторону общего пути ветвлений и сравнений;
- считать, что следующий заметный выигрыш вероятнее искать либо в оставшемся jump/comparison-слое, либо в плотных массивных цепочках `ARRAY...`, а не снова в одиночных строковых built-in.

Следующий шаг:
- посмотреть на `JumpIfTrue`/`JumpIfFalse` и соседние ветки общего управляющего пути, не осталось ли там такой же лишней обвязки;
- альтернативно, если нужен следующий более крупный пакет по данным сценариям, брать массивные callback- и mutating-builtins, которые в `mixed` и `string_array_loop` вызываются очень часто.

## Session 630 - 2026-05-02 14:31 - соседние JumpIfTrue и JumpIfFalse переведены на прямой стековый путь

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` `Opcode.JumpIfTrue` и `Opcode.JumpIfFalse` больше не идут через общий `PopValue(...)`, а вынесены в отдельный `ExecuteJumpIfBooleanInstruction(...)` с прямым `frame.EvaluationStack.Pop()` и прямой булевой проверкой;
- это добирает соседний jump-слой рядом с уже упрощёнными сравнительными переходами и выравнивает его по стоимости с другими горячими стековыми инструкциями, которые в VM давно работают без общей проверочной обвязки;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать новый статус управляющего пути и следующий сдвиг фокуса на массивные цепочки.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-5.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-4.txt`

Результат замеров:
- `mixed_workload_pipeline.axm`: `0.714x` runtime и `0.851x` total в `axiom-engine-compare-mixed-20260502-5.txt`;
- `string_array_loop_pipeline.axm`: `0.523x` runtime и `0.738x` total в `axiom-engine-compare-string-20260502-4.txt`;
- шаг подтверждает, что соседний jump-слой уже в основном дочищен: локальный выигрыш есть, но следующий крупный прирост вероятнее искать уже в плотных массивных цепочках, а не в дальнейшем дроблении одиночных булевых переходов.

Решение:
- считать локальную расчистку общего jump/comparison-пути завершённой до разумной глубины;
- следующий крупный пакет ускорения переносить в массивный слой `ARRAY...`, а не продолжать выжимать мелкие проценты из одиночных булевых переходов.

Следующий шаг:
- перейти к короткому профилирующему обзору плотных `ARRAY...`-цепочек в `mixed_workload_pipeline.axm` и `string_array_loop_pipeline.axm`;
- после этого брать следующий пакетный разгон уже по часто повторяющейся массивной группе, а не по одному opcode.

## Session 631 - 2026-05-02 15:14 - 1D-массивный пакет переведён на прямые копирования и сдвиги

Что сделано:
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` добавлены прямые 1D-пути `TryCopyOneDimensionalRangeTo(...)` и `TryMoveOneDimensionalRange(...)`, а также удешевлены `Clone()` и `AllocatePreserving(...)` для плотного одномерного случая;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` на эти пути переведены `ARRAYCLONE`, `ARRAYCONCAT`, `ARRAYTAKE`, `ARRAYSKIP`, а также 1D-сдвиги для `ARRAYINSERT` и `ARRAYREMOVE`;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать новый профиль горячей массивной группы и следующий шаг.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs
- src/Axiom.Runtime/Values/RuntimeArrayValue.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- по ходу проверки был один неудачный параллельный запуск `dotnet test` и один неудачный параллельный запуск `compare-engines`, оба упёрлись в общий `.artifacts\obj`; после этого проверки и замеры повторены строго последовательно, как требует репозиторий;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-5.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-6.txt`

Результат замеров:
- `mixed_workload_pipeline.axm`: `0.680x` runtime и `0.826x` total в `axiom-engine-compare-mixed-20260502-6.txt`;
- `string_array_loop_pipeline.axm`: `0.593x` runtime и `0.736x` total в `axiom-engine-compare-string-20260502-5.txt`;
- пакет попал именно в тот слой, который нужен: после локальной расчистки jump/comparison-пути следующий заметный выигрыш снова пришёл из плотной 1D-цепочки `ARRAY...`.

Решение:
- считать пакет удачным крупным шагом по массивному слою `BytecodeVm`, а не локальной микроправкой одной функции;
- дальше продолжать ускорение уже внутри оставшихся горячих массивных цепочек, а не возвращаться к строкам или к исчерпанному jump-слою.

Следующий шаг:
- посмотреть, какой из соседних массивных пакетов теперь даёт следующий измеримый эффект: `ARRAYPUSH`/`ARRAYPOP`, остаточные copy-heavy куски 1D-мутаторов или callback-массивные built-in;
- после выбора кандидата делать следующий пакет снова крупно и подтверждать его теми же двумя сценариями `mixed_workload_pipeline.axm` и `string_array_loop_pipeline.axm`.

## Session 632 - 2026-05-02 16:18 - `ARRAYPUSH` и `ARRAYPOP` добраны прямым append/remove-last путём

Что сделано:
- в `src/Axiom.Runtime/Values/RuntimeArrayValue.cs` добавлены `TryAppendOneDimensional(...)` и `TryPopOneDimensional(...)` для корневого одномерного массива без перехода через общий путь переразмещения;
- в `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` 1D-ветки `ARRAYPUSH` и `ARRAYPOP` переведены на эти быстрые пути с сохранением отката к общему механизму на случай неподходящей формы массива;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы зафиксировать честный эффект этого шага и следующую точку приложения усилий.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs
- src/Axiom.Runtime/Values/RuntimeArrayValue.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-6.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-7.txt`

Результат замеров:
- `string_array_loop_pipeline.axm`: `0.560x` runtime и `0.758x` total в `axiom-engine-compare-string-20260502-6.txt`;
- `mixed_workload_pipeline.axm`: `0.694x` runtime и `0.828x` total в `axiom-engine-compare-mixed-20260502-7.txt`;
- шаг полезен как локальное удешевление конца mutating-цепочки, но уже не даёт такого же сильного интегрального скачка, как предыдущий пакет по прямому копированию диапазонов и сдвигам.

Решение:
- считать `ARRAYPUSH`/`ARRAYPOP`-пакет завершённым и не дробить его дальше на ещё более мелкие оптимизации;
- основной следующий выигрыш искать уже в соседнем массивном слое, а не возвращаться к строкам или к доработке jump-пути.

Следующий шаг:
- определить следующий остаточный горячий массивный пакет для `mixed_workload_pipeline.axm`: callback-builtins или другой copy-heavy участок `ARRAY...`;
- после выбора снова делать крупный пакет и проверять его теми же двумя адресными сценариями.

## Session 633 - 2026-05-02 16:23 - 1D-индексирование массива переведено на быстрый успешный путь

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs` для `LoadElement` и `StoreElement` добавлен быстрый 1D-путь, который в успешном случае работает без `ResolveLocation`, `ReadDebugName` и общего диагностического слоя;
- если быстрый путь не подходит по типу индекса, форме массива или фактическому обращению, VM восстанавливает стек и откатывается в прежний медленный путь, так что пользовательское поведение и диагностика остаются прежними;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md`, чтобы отразить новый горячий слой и его свежие замеры.

Изменённые файлы:
- src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs
- docs/BYTECODE_COVERAGE.md
- docs/PLANS3.md
- docs/WORKLOG4.md

Проверка:
- первый прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` временно упал на компиляторной ошибке `CS0136` из-за пересечения локальных имён после рефакторинга; ошибка сразу исправлена;
- после исправления выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj` — 524 теста зелёные;
- выполнен последовательный прогон `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj` — 668 тестов зелёные;
- выполнены контрольные замеры:
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_array_loop_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-20260502-7.txt`
  - `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-8.txt`

Результат замеров:
- `string_array_loop_pipeline.axm`: `0.581x` runtime и `0.839x` total в `axiom-engine-compare-string-20260502-7.txt`;
- `mixed_workload_pipeline.axm`: `0.653x` runtime и `0.818x` total в `axiom-engine-compare-mixed-20260502-8.txt`;
- шаг хорошо попал именно в смешанный сценарий: после удешевления built-in-цепочки следующий заметный выигрыш пришёл из частого индексирования одномерных массивов внутри циклов.

Решение:
- считать успешный 1D-доступ к элементу новым разобранным горячим слоем VM;
- дальше искать следующий прирост уже за пределами дочищенных массивных мутаций и базового индексирования.

Следующий шаг:
- определить следующий остаточный горячий путь после built-in-цепочки и индексирования: строковая сборка массива, callback-массивные built-in или другой повторяющийся VM-слой в `mixed_workload_pipeline.axm`;
- после выбора снова делать пакетный шаг и подтверждать его теми же двумя адресными сценариями.

## Session 634 - 2026-05-02 16:42 - систематизированы производительные сценарии и добавлены новые benchmark-семейства

Что сделано:
- добавлен новый постоянный реестр `docs/BYTECODE_PERF_MATRIX.md`, который разводит функциональное покрытие `BytecodeVm` и покрытие производительных сценариев;
- `AGENTS.md` обновлён так, чтобы `docs/BYTECODE_PERF_MATRIX.md` входил в обязательный стартовый контекст и поддерживался в актуальном состоянии при изменении benchmark-семейств;
- в `samples/smoke/engine-compare/` добавлены два новых адресных сценария: `array_callback_pipeline.axm` для callback-builtins массивов и `numeric_function_sweep.axm` для числовой группы built-in;
- `src/Axiom.ManualChecks/Program.cs` обновлён: оба новых сценария включены в `compare-engines`;
- `README.md` дополнен кратким описанием различия между обычными runnable sample, `engine-compare`-сценариями, `docs/BYTECODE_COVERAGE.md` и `docs/BYTECODE_PERF_MATRIX.md`.

Изменённые файлы:
- `AGENTS.md`
- `README.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `samples/smoke/engine-compare/array_callback_pipeline.axm`
- `samples/smoke/engine-compare/numeric_function_sweep.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- ожидается последовательный прогон `Axiom.Runtime.Tests` и `Axiom.Execution.Tests`;
- ожидаются адресные прогоны `compare-engines` для `array_callback_pipeline.axm` и `numeric_function_sweep.axm`.

Решение:
- считать `docs/BYTECODE_COVERAGE.md` реестром функциональной готовности байткодного пути, а `docs/BYTECODE_PERF_MATRIX.md` отдельным реестром производительных семейств и нагрузочных сценариев;
- дальше возвращаться к ускорению VM уже по расширенной матрице сценариев, а не только по прежним строково-массивным нагрузкам.

Следующий шаг:
- прогнать новые benchmark-сценарии, зафиксировать их результаты в `.artifacts/manual-checks/` и после этого выбрать следующий кодовый слой оптимизации уже по более широкой матрице.

## Session 635 - 2026-05-02 17:08 - ????????????? ?????????? ????? benchmark-????????? ? ?????? ????????? ????????? ????????? VM

??? ???????:
- ???????? ????? ???????? benchmark-???????? `array_callback_pipeline.axm` ? `numeric_function_sweep.axm` ????? `compare-engines`;
- `docs/BYTECODE_COVERAGE.md` ???????: callback-??????? ?????????? ?? `?? ?????????` ? `?????? ????????`, ? ???????? built-in ???????? ????? ?? ?????????? ?????? ? ????????? ? ?????????? ????????;
- `docs/BYTECODE_PERF_MATRIX.md` ???????? ???????????? ???????????? ????? ???????;
- `docs/PLANS3.md` ??????? ???, ????? ????????? ??????? ??? ??? ????? ?? ? ????????? callback-????????.

????????? ?????:
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_callback_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-array-callback-20260502-1.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter numeric_function_sweep.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-numeric-20260502-1.txt`

???????:
- ??????? ???????? ???? ???????????? ??????? ??? ??????? VM;
- ??????? callback-builtins ???????? ????????? ???????? ????? ??????, ?????? ??? ?? ????????? ???????? ??? ???? ???? ??????????????.

????????? ???:
- ??????? ? ???? `BytecodeVm`, ????? ????????? ??????? callback-???????? ? ??????? ?????? ???????? ??? ?? ??????????? ?????? callback-??????? ? ?????? ?????????.

## Session 636 - 2026-05-02 17:15 - callback-builtins ???????? ?????? 1D-?????, ?? ????????? ????? ???? ???????? ? ?????? ???????

??? ???????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs` ??? `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE` ????????? ?????? 1D-????? ??? ?????? ?????? ??????? ????????;
- callback-?????? ?????? ???? built-in ?????????? ? `params RuntimeValue[]` ?? ?????? ????- ? ??????????????? `ReadOnlySpan<RuntimeValue>`-????;
- `docs/BYTECODE_COVERAGE.md` ? `docs/BYTECODE_PERF_MATRIX.md` ????????? ?? ??????????? ??????????? ????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_callback_pipeline.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-array-callback-20260502-4.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-10.txt`

???????:
- ??????? ????? ?? 1D-?????? ? ?????? `params` ???????? ??? ?????? ?????????? ????????;
- ?? ??????? ??? ??????????? ??? callback-builtins ??? ?????????? ?????????, ?????? ??? ???????? benchmark ?? ??? ???? ??????????????;
- ????????? ???? ?????? ? ????????? `InvokeBytecodeFunction` ? ???????? `BytecodeCallFrame` ?? ?????? ????????? callback-????????.

????????? ???:
- ??????? ? `BytecodeVm.cs` ? `BytecodeCallFrame.cs`, ????? ????????????? ????????? ? ????????????? ?? ?????? callback-????? ? ????? ????????? ????? ????????? ??? ???.

## Session 637 - 2026-05-02 17:18 - callback-??????? ???????? ???? ???????? ?? ???? ????????????????? ?????? ??? ???????

??? ???????:
- ? `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` ??????????? ?????? ??????? ????? ??????? ? ??????? `16` ?? ???????? ?????? ??????? `1`, ????? ????????? callback-??????? ?? ???????? ???????? ???????? ???? ??????????;
- ? `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` ???????? `Clear()` ??? ??????????? ????????????????? ?????;
- ? `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` ???????? ??????? `shared locals` ? `ResetNoLocals(...)` ??? ?????? ??? ???????;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ????????? ??????????? ? ????????? ????????????? ?????? ??? ??????? ?? `routine.Id`;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ?? ????? ??????????? ??????.

????????? ?????:
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_callback_pipeline.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-array-callback-20260502-5.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-12.txt`

???????:
- ??????? callback-builtins ???????? ???????????? ? ?????????? ????: ????? `0.901x` runtime / `0.903x` total ?? ???????? ????????;
- ??????? ????????? ?????? ??? ????? ?????: ?????? ??????????, ???????? ???????? ??????? ? ????????? ???? ?????? ????????? ???????.

????????? ???:
- ???? ?????????? ??????????? ? ????? ???? ??????? ? ????????, ???? ????? ????????? benchmark-????????? ?? `docs/BYTECODE_PERF_MATRIX.md`, ??? ??????? VM ??? ?? ????? ?????????.

## Session 638 - 2026-05-02 17:21 - callback-?????? ???????? ????? ??????????? ?????? ?????? ?? bytecode-?????????

??? ???????:
- ? `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs` ????????? ????????? ?????? ?????? ?? `BytecodeRoutine` ?????? ???????? ???????;
- ? `src/Axiom.Runtime/Values/RuntimeValue.cs` ???????? overload `FromBytecodeFunction(BytecodeRoutine, BytecodeVariableSlot[])`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` `LoadFunctionValue(...)` ?????? ?????? function-value ? ?????? ??????? ?? `BytecodeRoutine`, ? `InvokeBytecodeFunction(...)` ??????? ?????????? ? ? ?????? ?????, ??? ?????????????, ???????????? ? `_routinesBySymbol`;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ?? ????? ??????????? ??????.

????????? ?????:
- `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_callback_pipeline.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-array-callback-20260502-7.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-14.txt`

???????:
- ??????? ????? callback-???? ??? ??????? ???????????: ???????? callback-benchmark ?????? ????? `0.876x` runtime / `0.883x` total;
- ??????? ??????? callback-???? ??? ?????? ??????????? ? ?????? ????????, ?????????? ?? ??????????? ? ??????? ??????????, ??? ????????????? ?? ????????? benchmark-?????????.

????????? ???:
- ???? ???? ? ????????? ?????????? ??????????? ?????? ? ???????? ???????? ???????, ???? ????? ????????? ????????? ?? `docs/BYTECODE_PERF_MATRIX.md`, ??? ??? ??? ?????? ?????????? ???????? VM.

## Session 639 - 2026-05-02 17:30 - ????????? ???????? callback-function-value ????? ????? ?? ??????? ?????

??? ???????:
- ? `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` ???????? ??? ??????????? ???????? ??????? ?? `routineId`;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` `LoadFunctionValue(...)` ?????? ??????? ?????????? ???? ??? ? ?????? ????? ???????? ????????? ??????????? ????? ? ?????? ????? ???????? ???????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ?? ????? ???????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_callback_pipeline.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-array-callback-20260502-8.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-15.txt`

???????:
- ??????? ????????? ?????????? ??????????? ?????? ? ????????? ???????? callback-function-value ? ????? ? ??? ?? ??????? ????? ??????????? ?????? ??? ????????? ?????????;
- ??????? callback-???? ??? ?????? ???????????: ???????? benchmark ?????? ????? `0.821x` runtime / `0.824x` total.

????????? ???:
- ???? ??????????? ??? ?????? ? ???????????? ????????? ? built-in function-value, ???? ????????????? ?? ?????? benchmark-????????? ?? `docs/BYTECODE_PERF_MATRIX.md`.
## Session 640 - 2026-05-02 19:13 - closure formation ????????, ????????? ???? ? ??????? ???????? ? ????????? benchmark

??? ???????:
- ? `src/Axiom.ManualChecks/Program.cs` ???????? ????? ???????? ???????? `samples/smoke/engine-compare/date_time_calendar_pipeline.axm` ??? ????????????????? built-in ???? ? ???????;
- ? ????? `date_time_calendar_pipeline.axm` ????? ??????? ??????? ?????????? ???????? ????????? `DAYSINMONTH(year, month)` ? `ISLEAPYEAR(year)`, ????? benchmark ??????? ??????? ????, ? ?? ?????? ? ?????????????? ????????;
- ???????? ???????? ???? ???????????? callback-????????? ? `BytecodeVm`: user function-value ??? ?????????? ?? ??????? ?????, ??????? ????????? ?????????? ??????????? ?????? ? ???????? ?????, ? built-in function-value ???? ??????? ????????? ?????????????? ???? ??? ???????????? ???????????.

????????? ?????:
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/date_time_calendar_pipeline.axm`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter date_time_calendar_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-date-time-20260502-1.txt`

???????:
- ??????? closure formation ??? callback-???? ??? ? ???????? ???????????: ???????? ?????? ??? ???? ?????????? ????? user function-value ?? ??????? ?????;
- ??????? ????????????????? ????????? ???? ? ??????? ??????????? ? ????????? ?????????? ???? ? ??????????? ????? `0.651x` runtime / `0.656x` total;
- built-in function-value ???????? ??? ??????????????? ?????????? ?? ????????? ?????????? ??????????? ???????.

????????? ???:
- ????????????? ?? ????????? benchmark-????????? ?? `docs/BYTECODE_PERF_MATRIX.md`, ??? ???? ??? ?????? ?? ?????????? ?????????? ????????? ??? ???????? VM;
- ?????? ?????????? ??????? ??????????? ???? ? ??????????? ? ???????.
## Session 641 - 2026-05-02 19:19 - ??????????? ???? ??????? ? ????????? benchmark ? ??????? ?????????? ????????

??? ???????:
- ? `samples/smoke/engine-compare/control_flow_branch_maze.axm` ???????? ???????? ????????????????? ???????? ??? `FOR`, `WHILE`, `DO ... LOOP`, `IF/ELSEIF` ? `SELECT CASE` ? ??????????? ???????????? ?? ?????? built-in;
- ? `src/Axiom.ManualChecks/Program.cs` `compare-engines` ??????????????? ????? ???????? ??? ????????? benchmark-????????? ???????????? ????;
- `docs/BYTECODE_PERF_MATRIX.md` ? `docs/BYTECODE_COVERAGE.md` ????????? ???, ????? ????????? ? ????? ?????? ?? ????????? ?????? ???????? ?????????? ?????.

????????? ?????:
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/control_flow_branch_maze.axm`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter control_flow_branch_maze.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-control-flow-20260502-1.txt`

???????:
- ??????? ??????????? ???? ??????????? ? ????????? ?????????? ?????????;
- ??????? ??? ????????? ?????? ?????????? ?? ??????? ???????????, ?????? ??? ???????? benchmark ??????? ????? `1.042x` runtime / `1.043x` total, ?? ???? VM ????? ???? ??????? ???? ??????????????.

????????? ???:
- ???? ? ??? `BytecodeVm` ? ????????? ????????? ???????? ?????????, ???????? jump-????? ? ???????? ??????? ??????? ????????;
- ????? ??????? ?????? ????? ???????? `control_flow_branch_maze.axm`, ? ????? `mixed_workload_pipeline.axm`.
## Session 642 - 2026-05-02 19:22 - equality-?????? ????????, ??????????? ? metadata-???? ??????? ????????? ????????

??? ???????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` fast-path ????????????? ????????? ???????? ?? `CompareEqual` ? `CompareNotEqual`, ??????? `JumpIfEqual` ? `JumpIfNotEqual` ?????? ?? ?????? ? ????? `Compare(...)` ??? ?????????? ????????;
- `samples/smoke/engine-compare/control_flow_branch_maze.axm` ???????? ??????? ????? ???? ?????? ? ??????? ??????? ???????????? ???? ???? ????????;
- `samples/smoke/engine-compare/high_rank_metadata.axm` ?????? ?? ??????? ???????? smoke-???????? ?? ?????????????? metadata-benchmark, ????? ???? ??????????? metadata-???? ???? ??????? ??????? ????????? ?????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ???????????????? ? ?????? ????????? ????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `samples/smoke/engine-compare/control_flow_branch_maze.axm`
- `samples/smoke/engine-compare/high_rank_metadata.axm`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter control_flow_branch_maze.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-control-flow-20260502-2.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter mixed_workload_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-mixed-20260502-16.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter high_rank_metadata.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-high-rank-20260502-2.txt`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

???????:
- ??????? ??????????? ???? ???????? ?? ???????? ??????: ????? fast-path ??? equality-??????? ?? ???????? ????? `0.894x` runtime / `0.913x` total;
- ??????? ??????????? metadata-???? ???? ???????? ?? ?????????? ????????: ????????? `high_rank_metadata.axm` ???????? ????? `0.540x` runtime / `0.563x` total;
- ??????? ????????? ???????????????? ??????? ??? ?? ????? ????????? fast-path ? VM, ? ?????????? benchmark-harness ??? ??? ????????, ??????? ???? ?????????? ????????????? ??? ??????.

????????? ???:
- ???? ?????????? ????? benchmark-????????, ???? ? `compare-engines` ? ????????? ????????? scripted input ??? ??????????????? ???????? ???????;
- ?????? ??? ????? ?????????? ??????? ???????????? ? built-in function-value ??? ??????? ??? ????????? VM-????.
## Session 643 - 2026-05-02 19:32 - scripted input ???????? ? compare-engines, ????????? INPUT ??????? ???????? ????????

??? ???????:
- ? `src/Axiom.ManualChecks/Program.cs` benchmark-harness ??????? ????????? scripted input ????? `BenchmarkCase.InputLines` ? ????????? `QueueInputProvider`;
- ???????? ???????? ???????? `samples/smoke/engine-compare/input_record_intake.axm`, ??????? ????????? `INPUT`, ?????? ?????????? ????????, ?????????????? `INTEGER`, `IF/ELSEIF` ? `SELECT CASE` ????? ?????? ??????;
- ? `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs` acceptance-?????? ??????? ??? ?? ????? ??????? ????? ??? ?????? sample, ????? ?????????????? ???????? `samples/` ?? ????????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ?? ?????? benchmark-?????????.

????????? ?????:
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/input_record_intake.axm`
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter input_record_intake.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-input-20260502-1.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

???????:
- ??????? ??????????? harness ?? `INPUT` ??????: scripted input ?????? ????????? ????? ? `compare-engines`;
- ??????? `INPUT` ????? ????????? benchmark-?????????? ? ??????? ??????????? ????? `1.116x` runtime / `1.062x` total, ?? ???? ??? ??? ?? ?????? ????, ? ???????? ???????? ?? ?????????? ?????????.

????????? ???:
- ???? ???? ? VM-???? `INPUT` ? ?????????? ?????? multi-input ? ?????????????? ????????;
- ???? ????????? harness ??? ???, ??? ??? ??????????????? ???????? ???????, ????? ???????? `DATE` / `TIME` / `NOW` ????? ?? ???????? ?????????.
## Session 644 - 2026-05-02 21:26 - compare-engines ??????? ??????????? ????, current-time benchmark ?????? ?????? ???????

??? ???????:
- ? `src/Axiom.Execution/Requests/ExecutionRequest.cs` ???????? ?????????????? `IRuntimeClock`, ? `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs` ? `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` ?????? ??????????? ??? ? ?????????? ????? ???????;
- ? `src/Axiom.ManualChecks/Program.cs` benchmark-harness ??????? `BenchmarkCase.RuntimeClock`, ????????? ?????????? `FixedRuntimeClock` ? ??????????? ?????? ???????? `samples/smoke/engine-compare/current_time_snapshot.axm` ? ?????????????? ?????? ? ?????????????? ??????? ??????;
- ? `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` ????????? ???????? `Run_UsesProvidedRuntimeClockForCurrentDateTimeBuiltins()`, ????? `DATE`, `TIME`, `NOW`, `UTCDATE`, `UTCTIME`, `UTCNOW` ? `TIMER` ???????????? ?????????? ???? ????????????????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ???????????????? ? ????? current-time benchmark.

????????? ?????:
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/current_time_snapshot.axm`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter current_time_snapshot.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-current-time-20260502-1.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

???????:
- ??????? ??????????? harness ?? built-in ???????? ??????? ??????: `compare-engines` ?????? ????? ??????????????? ????????? ???? ? ??????? ????;
- ??????? ???????????? `DATE` / `TIME` / `NOW` ???????? ????????? ???????? ?????????: `current_time_snapshot.axm` ???????? ????? `0.497x` runtime / `0.569x` total ? ?? ??????? ??????????? VM-???????????.

????????? ???:
- ???? ?????????? ????? ??????????????????, ???????????? ??? ?? ? harness, ? ? VM-???? `INPUT`, ??? ????? ????????? ????????? ???????? ?? ??? ????? ????????? ????? ???????????? ??????????????;
- ????? ?????????? harness ?????? ?????? ??? ????????? ?????????? ??????? ???????????????? benchmark-?????????.
## Session 645 - 2026-05-03 07:42 - INPUT ? BytecodeVm ????????? ? ????? ???? ????????

??? ???????:
- ? `src/Axiom.Runtime/Execution/BytecodeVm.cs` ???????? ??? `INPUT`-?????????? ?? ???? `(routine-id, instruction-index)`, ????? ????????? ???????? ???? ?? ??????????? ?????? ?? ?? ???? ? prompt;
- ? `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs` ???????? ?????? `INPUT` ???????? ? `Split(',')` ?? ?????? ?? `ReadOnlySpan<char>` ? ??????????? ?????? ????????? ???????????? ????? ? ??? ?????????????? ??????? ?????;
- ????? ???????? ????????? ?? ??????????? `INPUT` ?????? ???????? ?????? ????????????? ? ???????? ????????? acceptance-???????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ??? ????? ?????????.

????????? ?????:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter input_record_intake.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-input-20260503-1.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter input_record_intake.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-input-20260503-2.txt`

???????:
- ??????? ?????? ???????????????? ???? ?? `INPUT` ????????: ????????? ???????? ?????? ???????? ????? `0.892x` runtime / `0.928x` total;
- ??????? ????????? ??????? ??? ?? ?????????? ??????????? ?????, ? ?????????? ??????? ?? ??? ????????? ?????????, ???? ?? ???????? ????? ?????????? ??????.

????????? ???:
- ???? ?????????? ?? benchmark-?????, ???????? ????????? ???????? ??? `PRINT` / `PRINT USING` ??? ?????? ???? ???????? ???????? ????;
- ????????? VM-?????? ????? ?????? ?????? ????? ?????? ??????? ?????? ??? ??????? ???????.
## Session 646 - 2026-05-03 07:52 - PRINT ? PRINT USING ???????? ???????? benchmark ? ??????? ??? ???? ????????? ???? ???????

??? ???????:
- ? `samples/smoke/engine-compare/` ???????? ???????? `print_output_gallery.axm`, ??????? ????????? ??????? `PRINT`, ?????? ?????? ? `;` ? `,`, ? ????? ????????? ? ???????? ????? `PRINT USING`;
- ? `src/Axiom.ManualChecks/Program.cs` ????? ???????? ??????????????? ? `compare-engines`, ??? ??? ????? ?????? ???????? ????????, ? ?? ?????? ??? ???????? ????? `mixed` ? ????????? ?????????;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` ? `docs/WORKLOG4.md` ????????? ??? ????? ???????? benchmark ??????.

????????? ?????:
- `samples/smoke/engine-compare/print_output_gallery.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

????????:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter print_output_gallery.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-print-20260503-1.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`

???????:
- ??????? ????????? `PRINT` / `PRINT USING` ???????? ????????? benchmark-?????????: ?????? ?????? ???????? ????? `0.941x` runtime / `0.955x` total;
- ???????, ??? ?? ???? ???? ????? ??????? ??????? ?? ??????: ????? ??? ???? ???????? ? ?? ??????? ??????????? VM-???????????.

????????? ???:
- ???? ?????????? benchmark-?????, ?????? ????????? ??????? ???????????? ????????? ??? ?????????? ? ?????????? ????????????? ????????;
- ??????? ??????????? ????? ?????? ?????? ????? ?????? ??????? ??????? ?? benchmark ??? ???????.
## Session 647 - 2026-05-03 08:11 - Harness получил детерминированный RND, а вероятностный benchmark закрыт напрямую

Что сделано:
- добавлен контракт `IRuntimeRandom`, а `IRuntimeHost`, `NullRuntimeHost`, `ExecutionRequest`, `ExecutionRuntimeHostAdapter`, `AxiomExecutionPipeline`, интерпретатор и `BytecodeVm` переведены на общий абстрактный источник `RND` вместо локальных `Random` внутри движков;
- в `src/Axiom.ManualChecks/Program.cs` `compare-engines` получил фабрику `RuntimeRandomFactory`, а в `samples/smoke/engine-compare/` добавлен новый сценарий `random_stream_snapshot.axm` с фиксированным зерном `24681357`;
- в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` добавлена детерминированная проверка `Run_UsesProvidedRuntimeRandomForRndBuiltin()`, а тестовые runtime-host реализаций в `InterpreterTests` и `BytecodeVmTests` доведены до нового контракта;
- отдельным прогоном подтверждено, что `audit-bytecode-coverage` остаётся зелёным и после расширения benchmark-harness.

Изменённые файлы:
- `src/Axiom.Runtime/Host/IRuntimeRandom.cs`
- `src/Axiom.Runtime/Host/IRuntimeHost.cs`
- `src/Axiom.Runtime/Host/NullRuntimeHost.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Numeric.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Numeric.cs`
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/smoke/engine-compare/random_stream_snapshot.axm`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter random_stream_snapshot.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-rnd-20260503-1.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- audit-bytecode-coverage --out C:\Axiom\.artifacts\manual-checks\axiom-bytecode-coverage-audit-20260503-1.txt`

Решение:
- считать ограничение harness по `RND` снятым: теперь вероятностный слой измеряется честно и держится около `0.818x` runtime / `0.800x` total;
- считать прямой числовой слой полностью закрытым отдельными сценариями, включая `RND`, без новой локальной просадки в VM.

Следующий шаг:
- если продолжать benchmark-линию, искать следующее новое семейство для адресного сценария или переходить к более широкому профильному аудиту реальных программ;
- инфраструктурные расширения harness делать уже только при появлении новой детерминизационной дыры, а не заранее.
## Session 648 - 2026-05-03 08:20 - Пользовательские TYPE получили отдельный benchmark и закрыли ещё одну дыру матрицы

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый сценарий `type_record_roundtrip.axm`, который нагружает пользовательские `TYPE`, вложенные поля, фиксированные строковые поля, фиксированные массивные поля и копирование записи целиком;
- в `src/Axiom.ManualChecks/Program.cs` новый сценарий зарегистрирован в `compare-engines`, так что record-слой теперь меряется отдельно, а не только через unit- и acceptance-примеры;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый адресный benchmark пользовательских типов.

Изменённые файлы:
- `samples/smoke/engine-compare/type_record_roundtrip.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter type_record_roundtrip.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-type-20260503-1.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter type_record_roundtrip.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-type-20260503-2.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать пользовательские `TYPE` и доступ к полям закрытым отдельным benchmark-сценарием: устойчивый длинный прогон держится около `0.969x` runtime / `0.965x` total;
- считать, что новый адресный сценарий не выявил нового горячего долга VM: record-слой уже слегка ниже паритета и сейчас скорее закрывает слепую зону матрицы, чем требует срочной оптимизации.

Следующий шаг:
- если продолжать benchmark-линию, выбирать следующее самостоятельное семейство, прежде всего процедуры/функции или замыкания как отдельный сценарий;
- кодовые оптимизации снова делать только после нового прямого сигнала от benchmark или профиля.
## Session 649 - 2026-05-03 08:43 - Шкала разгона переведена на цель 0.900x, а TYPE выведен в целевой диапазон

Что сделано:
- в `RuntimeRecordValue` словарь полей заменён на ordinal-хранилище `RuntimeValue[]`, а `TypeFieldSymbol` получил порядковый номер поля, который назначается в `UserDefinedTypeSymbol`;
- в `BytecodeVm` добавлен кэш record-полей по `(routine, instruction)`, а успешные `LoadField` / `StoreField` больше не тянут `ResolveLocation`, если диагностика не нужна;
- `docs/BYTECODE_PERF_MATRIX.md` и `docs/BYTECODE_COVERAGE.md` переведены на новую рабочую шкалу: целевой диапазон `<= 0.900x`, рабочий паритет `0.900x .. 1.000x`, прямой долг `> 1.000x`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Runtime/Values/RuntimeRecordValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Records.cs`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- адресный benchmark `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-type-20260503-4.txt`

Решение:
- считать record-путь `TYPE` больше не «слегка ниже интерпретатора», а выведенным в целевой диапазон: `type_record_roundtrip.axm` держится около `0.891x` runtime и `0.888x` total;
- считать новую шкалу `0.900x` официальным рабочим ориентиром для дальнейшего разгона VM и отделять ею целевые семейства от серой зоны.

Следующий шаг:
- идти в ближайшее семейство серой зоны, прежде всего в `PRINT` / `PRINT USING`, где адресный сценарий всё ещё держится около `0.941x` runtime и `0.955x` total;
- в `TYPE` без нового профильного сигнала не возвращаться, потому что этот слой уже закрыт по новой целевой шкале.
## Session 650 - 2026-05-03 09:26 - PRINT USING получил кэш формата и безаллоцирующий набор аргументов

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен кэш `PRINT USING`-форматов и обновлён путь `ExecutePrintUsing(...)`: значения теперь собираются через `ArrayPool<RuntimeValue>` и передаются форматтеру как `ReadOnlySpan<RuntimeValue>`;
- в `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs` парсинг формата переведён на кэш `GetOrParsePrintUsingFormat(...)`, а обработка аргументов перестроена на `ReadOnlySpan<RuntimeValue>` без массива кортежей `RuntimeValue + TextLocation`;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md` и `docs/PLANS3.md` с новым состоянием слоя вывода и свежими числами benchmark.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter print_output_gallery.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-print-20260503-2.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter print_output_gallery.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-print-20260503-3.txt`

Решение:
- считать путь вывода `PRINT` / `PRINT USING` после этой оптимизации выровненным до почти чистого паритета: длинный прогон держится около `0.998x` runtime и `1.002x` total;
- считать, что прямого профильного долга в выводе уже нет, но до целевого диапазона `<= 0.900x` это семейство всё ещё не доведено.

Следующий шаг:
- перейти к следующему осмысленному профилю разгона, где возможен заметный запас под новую шкалу, прежде всего к отдельному benchmark-срезу процедур/функций и замыканий;
- в `PRINT` / `PRINT USING` возвращаться только при новом сигнале из профиля или регрессии.
## Session 651 - 2026-05-03 09:48 - вывод PRINT и PRINT USING доведён до целевого runtime-диапазона

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` путь `Opcode.PrintValue` переведён с `ToDisplayString()` на прямую запись частых скалярных типов в `_pendingOutput` через `TryFormat` и типовые fast-path;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs` `PRINT USING` перестроен на прямую запись токенов в pending-output без промежуточной итоговой строки;
- строковые поля `PRINT USING` (`!`, `&`, `\   \`) переведены на прямой путь без временных строк;
- частый числовой путь `PRINT USING` для фиксированных масок переведён на форматирование в стековый буфер через `TryFormat`, чтобы не создавать промежуточные строки `F0/F2` на каждом выводе;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новые цифры слоя вывода.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter print_output_gallery.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-print-20260503-8.txt`

Решение:
- считать слой `PRINT` / `PRINT USING` по чистому исполнению выведенным в целевой диапазон: `print_output_gallery.axm` держится около `0.884x` runtime;
- считать, что по метрике `total` этот слой ещё не дошёл до строгой границы `<= 0.900x` и сейчас находится около `0.918x`, то есть остаётся в рабочей серой зоне.

Следующий шаг:
- если нужно добивать именно `total`, искать следующий резерв уже не только в VM-исполнении вывода, а и в цене подготовки/понижения для этого семейства;
- если главным критерием считать runtime VM, переключаться на следующий адресный benchmark для процедур/функций и замыканий.
## Session 652 - 2026-05-03 09:54 - процедуры и замыкания выделены в отдельный benchmark-срез

Что сделано:
- в `samples/smoke/engine-compare/` добавлен адресный сценарий `routine_closure_pipeline.axm`, который отдельно нагружает прямые вызовы `SUB`/`FUNCTION`, вызовы через function-value, returned closure, shared-state closure и nested `SUB` с захватом внешнего локаля;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как новое benchmark-семейство;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый профильный слой и его цифры.

Изменённые файлы:
- `samples/smoke/engine-compare/routine_closure_pipeline.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter routine_closure_pipeline.axm --iterations 5 --warmup 1 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-routines-20260503-2.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter routine_closure_pipeline.axm --iterations 10 --warmup 2 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-routines-20260503-3.txt`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать процедуры, функции и замыкания теперь отдельным измеряемым семейством в матрице производительности;
- считать, что новый слой не показал прямого долга VM: `routine_closure_pipeline.axm` держится около `0.928x` runtime и `0.932x` total, то есть уже в рабочем паритете, но ещё выше целевого диапазона `<= 0.900x`.

Следующий шаг:
- если продолжать расширять benchmark-матрицу, выбирать следующее ещё невыделенное семейство;
- если продолжать точечный разгон по серой зоне `0.900x .. 1.000x`, можно уже отдельно профилировать именно процедурно-замыкательный путь.
## Session 653 - 2026-05-03 10:12 - процедурно-замыкательный путь выведен в целевой диапазон

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен кэш привязки захватов `caller routine -> target routine`, чтобы повторные вызовы локальных процедур и создание closure больше не шли через общий словарь `VisibleVariables`;
- в `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлены покадровый кэш capture-slot массивов, шаблоны локалов по `BytecodeRoutine` и повторное использование `BytecodeCallFrame` после `RETURN`;
- benchmark-сценарий `samples/smoke/engine-compare/routine_closure_pipeline.axm` оставлен в исходной форме, а итоговая фиксация сделана более длинным прогоном, потому что короткие серии для этого семейства оказались шумными;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый статус слоя процедур и замыканий.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `samples/smoke/engine-compare/routine_closure_pipeline.axm`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter routine_closure_pipeline.axm --iterations 20 --warmup 3 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-routines-20260503-10.txt`

Решение:
- считать процедурно-замыкательный путь выведенным в целевой диапазон по обеим метрикам;
- итоговый длинный прогон `routine_closure_pipeline.axm` держится около `0.861x` runtime и `0.874x` total, поэтому возвращаться к этому слою как к срочному долгу больше не нужно.

Следующий шаг:
- если продолжать разгон уже измеренных семейств, идти в `PRINT`, `INPUT` или управляющий слой как в оставшуюся серую зону;
- если продолжать расширять benchmark-матрицу, выбирать следующее ещё невыделенное адресное семейство.
## Session 654 - 2026-05-03 10:27 - управляющий слой выведен в целевой диапазон

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` стек VM переведён с зануления слота на каждом `Pop()` на отложенную очистку при `Clear()`, чтобы снять лишнюю запись из горячего пути ветвлений, циклов и арифметики;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` comparison-jump инструкции (`JumpIfEqual`, `JumpIfNotEqual`, `JumpIfLess`, `JumpIfLessOrEqual`, `JumpIfGreater`, `JumpIfGreaterOrEqual`) переведены с общего диспетчера на специализированные прямые методы;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый статус управляющего слоя.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter control_flow_branch_maze.axm --iterations 20 --warmup 3 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-control-20260503-3.txt`

Решение:
- считать управляющий слой `IF` / `SELECT CASE` / циклы выведенным в целевой диапазон по обеим метрикам;
- итоговый длинный прогон `control_flow_branch_maze.axm` держится около `0.826x` runtime и `0.814x` total, поэтому возвращаться к этому слою как к срочному долгу больше не нужно.

Следующий шаг:
- если продолжать разгон уже измеренных семейств, идти в `PRINT` или `INPUT` как в оставшуюся серую зону;
- если продолжать расширять benchmark-матрицу, выбирать следующее ещё невыделенное адресное семейство.
## Session 655 - 2026-05-03 10:32 - `INPUT` добит, а `PRINT` подтверждён в целевом диапазоне длинными прогонами

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs` добавлен быстрый путь для `INPUT` на 1/2/3 целей без промежуточного массива результатов и без общего двухшагового разбора на самом частом пакетном сценарии;
- длинными контрольными прогонами перепроверены `PRINT` / `PRINT USING` и `INPUT`, чтобы снять влияние шума коротких серий и зафиксировать новые числа уже после общих ускорений VM;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый статус слоёв ввода и вывода.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter print_output_gallery.axm --iterations 20 --warmup 3 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-print-20260503-9.txt`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter input_record_intake.axm --iterations 40 --warmup 5 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-input-20260503-3.txt`

Решение:
- считать `PRINT` / `PRINT USING` подтверждённо выведенными в целевой диапазон: около `0.860x` runtime и `0.877x` total;
- считать `INPUT` выведенным в целевой диапазон: около `0.879x` runtime и `0.889x` total;
- считать, что по текущей матрице адресных benchmark-семейств явной серой зоны больше не осталось.

Следующий шаг:
- если продолжать benchmark-линию, искать новое ещё невыделенное адресное семейство или новую реальную программу для профильного сигнала;
- если продолжать разгон VM, сначала нужен новый подтверждённый долг, а не повторный возврат в уже закрытые слои.
## Session 656 - 2026-05-03 12:20 - выделено scalar-core семейство, и оно сразу показало новый прямой долг VM

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `scalar_assignment_expression_mesh.axm`, который отдельно нагружает скалярные выражения, присваивания, побитовые операции, сдвиги и сравнения без заметной зависимости от built-in-слоя;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как новое benchmark-семейство;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый профильный слой и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter scalar_assignment_expression_mesh.axm --iterations 20 --warmup 3 --out C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-scalar-20260503-2.txt`

Решение:
- считать scalar-core выражений и присваиваний новым выделенным benchmark-семейством;
- считать, что это семейство сразу показало новый прямой долг VM: около `1.072x` runtime и `1.070x` total.

Следующий шаг:
- если продолжать разгон VM, идти уже прямо в scalar-core как в ближайший подтверждённый долг;
- если продолжать только расширять матрицу, тогда искать следующее ещё невыделенное семейство уже как отдельную задачу, а не вместо этого долга.
## Session 657 - 2026-05-03 12:38 - scalar-core частично ускорен, но всё ещё остаётся прямым долгом

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` убрана лишняя синхронизация `_globals` из горячего пути `StoreGlobal`, добавлен кэшируемый флаг быстрого режима исполнения и горячие pop-пути переведены на `PopUnchecked()`;
- в `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs` добавлен `PopUnchecked()` для безопасного использования из проверенного байткодного hot path;
- в `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` и `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` уплощён путь хранения обычных скаляров без фиксированной строки и без `TYPE`, чтобы массовые присваивания не проходили через лишнюю общую нормализацию;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый промежуточный статус scalar-core.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeEvaluationStack.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter scalar_assignment_expression_mesh.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-scalar-20260503-6.txt`

Решение:
- считать, что scalar-core после первых профильных правок ускорен, но ещё не выведен даже в рабочий паритет;
- фиксировать новый промежуточный уровень около `1.059x` runtime и `1.058x` total вместо исходных `1.072x` / `1.070x`.

Следующий шаг:
- продолжать разгон именно scalar-core, а не возвращаться к уже закрытым benchmark-семействам;
- если понадобится более чистый сигнал, выделить внутри scalar-core ещё более узкий адресный под-сценарий для арифметического dispatch и присваиваний.
## Session 658 - 2026-05-03 12:42 - scalar-core почти доведён до паритета через специализацию dispatch

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` `Subtract` выведен в отдельный горячий путь по образцу `Add` и `Multiply`;
- там же частые scalar-сравнения `CompareEqual`, `CompareNotEqual`, `CompareLess`, `CompareLessOrEqual`, `CompareGreaterOrEqual` переведены с общего бинарного диспетчера на прямые специализированные методы;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый промежуточный статус scalar-core.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter scalar_assignment_expression_mesh.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-scalar-20260503-7.txt`

Решение:
- считать, что scalar-core после специализации dispatch почти доведён до паритета;
- фиксировать новый промежуточный уровень около `1.033x` runtime и `1.033x` total вместо прежних `1.059x` / `1.058x`.

Следующий шаг:
- продолжать точечный разгон именно scalar dispatch до паритета и ниже;
- если понадобится точнее локализовать остаток долга, выделить внутри scalar-core ещё более узкий benchmark на арифметический путь.
## Session 659 - 2026-05-03 13:07 - scalar-core выведен ниже единицы и переведён в рабочий паритет

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` из общего бинарного диспетчера дополнительно выведены `MOD`, `AND`, `OR`, `XOR`, `<<` и `>>`;
- для этих операций добавлены прямые быстрые пути по частым типам `INTEGER`, `LONG` и `BOOLEAN`, чтобы основной scalar benchmark меньше ходил в общий `ExecuteBinaryInstruction`;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый статус scalar-core.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter scalar_assignment_expression_mesh.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-scalar-20260503-8.txt`

Решение:
- считать scalar-core больше не прямым долгом VM;
- фиксировать новый уровень около `0.948x` runtime и `0.949x` total, то есть слой уже выведен в рабочий паритет, хотя ещё не дотянут до целевого `<= 0.900x`.

Следующий шаг:
- если продолжать разгон именно этого слоя, сначала выделить внутри scalar-core более узкий адресный benchmark на чистый арифметический dispatch;
- если переключаться на другой профильный слой, брать уже новый сигнал, а не возвращаться к ранее закрытым прямым долгам.
## Session 660 - 2026-05-03 14:24 - scalar-core добит до целевого диапазона через отдельный fast-path деления

Что сделано:
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` `Divide` выведен из общего бинарного диспетчера в отдельный горячий путь;
- для частых `DOUBLE`- и `DECIMAL`-делений добавлен прямой fast-path без общего `Opcode`-маршрута;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый статус scalar-core.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter scalar_assignment_expression_mesh.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-scalar-20260503-9.txt`

Решение:
- считать scalar-core выведенным в целевой диапазон;
- фиксировать новый уровень около `0.853x` runtime и `0.860x` total, то есть и по чистому исполнению, и по полной метрике слой уже быстрее интерпретатора с хорошим запасом.

Следующий шаг:
- искать новое ещё невыделенное benchmark-семейство или новый профильный сигнал;
- к scalar-core как к срочному долгу больше не возвращаться.
## Session 661 - 2026-05-03 14:32 - выделен файловый path/info benchmark, и он сразу оказался в целевом диапазоне

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `file_path_metadata_sweep.axm` для `FILEEXISTS`, `PATHEXISTS`, `FILESIZE`, `FILEDATE`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `FULLPATH`, `ISABSOLUTEPATH` и `GETFILES`;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный файловый path/info benchmark;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый профильный слой и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/file_path_metadata_sweep.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter file_path_metadata_sweep.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-file-path-20260503-1.txt`

Решение:
- считать файловый path/info слой отдельным benchmark-семейством;
- фиксировать, что он сразу показал хороший запас: около `0.768x` runtime и `0.773x` total.

Следующий шаг:
- искать следующее ещё невыделенное benchmark-семейство или реальный новый профильный сигнал;
- к файловым path/info built-in как к срочному кандидату на ускорение не возвращаться.
## Session 662 - 2026-05-03 14:55 - выделен benchmark файлового управления и подтверждён рабочий паритет

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `file_management_roundtrip.axm` для `MAKEDIR`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEFILE`, `COPYDIR`, `MOVEDIR`, `RENAMEDIR`, `DELETEDIRRECURSIVE`, `CHANGEDIR` и `CURRENTDIR`;
- сценарий сделан идемпотентным между прогонами: в начале он очищает только свой каталог `.artifacts/manual-checks/file-management-roundtrip`, чтобы длинные замеры не ломались на уже существующих целевых файлах и каталогах;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark файлового управления;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый файловый management-срез и его результат.

Изменённые файлы:
- `samples/smoke/engine-compare/file_management_roundtrip.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter file_management_roundtrip.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-file-management-20260503-1.txt`

Решение:
- считать файловое управление файлами и каталогами отдельным benchmark-семейством;
- фиксировать, что оно не выявило нового прямого долга: около `0.985x` runtime и `0.987x` total, то есть это рабочий паритет, но не целевой диапазон.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство или реальный новый профильный сигнал;
- к этому файловому management-срезу не возвращаться как к срочному кандидату на ускорение.
## Session 663 - 2026-05-03 14:59 - выделен benchmark для drive/temp built-in и подтверждён рабочий паритет

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `drive_temp_storage_sweep.axm` для `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE` и `MAKETEMPDIR`;
- первый вариант сценария показал нестабильный вывод из-за включения живых значений свободного места в итоговый `checksum`; сценарий сразу скорректирован так, чтобы проверять для `FREESPACE` и родственных built-in только контракт `> 0`, а не сами меняющиеся числа;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark файлового drive/temp слоя;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый профильный слой и его результат.

Изменённые файлы:
- `samples/smoke/engine-compare/drive_temp_storage_sweep.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter drive_temp_storage_sweep.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-drive-temp-20260503-1.txt`

Решение:
- считать файловые built-in диска, размера каталога и временных путей отдельным benchmark-семейством;
- фиксировать, что и этот слой не выявил нового прямого долга: около `0.992x` runtime и `0.994x` total, то есть это рабочий паритет, но не целевой диапазон.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство или реальный новый профильный сигнал;
- к drive/temp built-in не возвращаться как к срочному кандидату на ускорение.
## Session 664 - 2026-05-03 15:04 - выделен benchmark открытых файловых потоков, и он сразу оказался в целевом диапазоне

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `open_file_stream_cycle.axm` для `OPEN`/`CLOSE`, `READLINE`, `READTEXT`, `READTOEND`, `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND`, `FILEPOS`, `FILEMODE`, `FILELENGTH`, `EOF` и `ISOPEN`;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark открытых файловых потоков;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый файловый потоковый слой и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/open_file_stream_cycle.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter open_file_stream_cycle.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-open-stream-20260503-1.txt`

Решение:
- считать открытые файловые потоки отдельным benchmark-семейством;
- фиксировать, что новый слой сразу показал хороший запас: около `0.813x` runtime и `0.834x` total.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство уже вне файлового домена;
- к открытому файловому потоковому слою как к срочному кандидату на ускорение не возвращаться.
## Session 665 - 2026-05-03 15:15 - выделен benchmark низкоуровневых строковых примитивов, и он сразу показал большой запас

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `string_primitive_scan.axm` для `ASC`, `CHR`, `LEN`, `SPACE`, `STRING`, `INSTR`, `INSTRREV` и `SUBSTRCOUNT`;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark строковых примитивов;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый строковый primitive-срез и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/string_primitive_scan.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter string_primitive_scan.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-string-primitives-20260503-1.txt`

Решение:
- считать низкоуровневые строковые примитивы отдельным benchmark-семейством;
- фиксировать, что новый слой сразу показал очень хороший запас: около `0.398x` runtime и `0.476x` total.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство уже вне низкоуровневого строкового и файлового доменов;
- к строковым примитивам как к кандидату на ускорение не возвращаться.
## Session 666 - 2026-05-03 15:20 - побитовые операции и сдвиги выделены в отдельный benchmark

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `bitwise_shift_mesh.axm` для `AND`, `OR`, `XOR`, `NOT`, `<<` и `>>` на `INTEGER`, `LONG` и `BOOLEAN`;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark побитового scalar-подслоя;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый bitwise/shift-срез и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/bitwise_shift_mesh.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter bitwise_shift_mesh.axm --iterations 20 --warmup 3 | Tee-Object -FilePath C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-bitwise-20260503-1.txt`

Решение:
- считать побитовые операции и сдвиги отдельным benchmark-семейством;
- фиксировать, что новый слой уже быстрее интерпретатора, но пока не целевой: около `0.968x` runtime и `0.968x` total, то есть это рабочий паритет, а не прямой долг.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство;
- к побитовому подслою возвращаться только если появится желание адресно дожать рабочий паритет ближе к `0.900x`.
## Session 667 - 2026-05-03 18:29 - поисковые ARRAY-builtins выделены в отдельный benchmark

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `array_search_probe.axm` для `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL` и `ARRAYBINARYSEARCH` на 1D и 2D-массивах без callback-слоя;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark поискового массивного подслоя;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый array-search-срез и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/array_search_probe.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_search_probe.axm --iterations 20 --warmup 3`

Решение:
- считать поисковые built-in массивов отдельным benchmark-семейством;
- фиксировать, что новый слой сразу показал целевой результат: около `0.852x` runtime и `0.857x` total.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство;
- к поисковому массивному подслою как к кандидату на ускорение не возвращаться.
## Session 668 - 2026-05-03 18:36 - shape-преобразования массивов выделены в отдельный benchmark

Что сделано:
- в `samples/smoke/engine-compare/` добавлен новый адресный сценарий `array_shape_transform_mesh.axm` для `ARRAYDISTINCT`, `ARRAYTOREVERSED`, `ARRAYREVERSE`, `ARRAYFLATTEN` и `ARRAYRESHAPE` на одном и том же 2D-массиве;
- в `src/Axiom.ManualChecks/Program.cs` сценарий зарегистрирован в `compare-engines` как отдельный benchmark shape-подслоя массивов;
- `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `docs/PLANS3.md` и `docs/WORKLOG4.md` обновлены под новый array-shape-срез и его числа.

Изменённые файлы:
- `samples/smoke/engine-compare/array_shape_transform_mesh.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter array_shape_transform_mesh.axm --iterations 20 --warmup 3`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать shape-преобразования массивов отдельным benchmark-семейством;
- фиксировать, что новый слой сразу показал целевой результат: около `0.817x` runtime и `0.824x` total.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство;
- к shape-подслою массивов как к кандидату на ускорение не возвращаться.
## Session 669 - 2026-05-03 18:43 - массивные regex-результаты выделены в отдельный benchmark

Что сделано:
- добавлен адресный сценарий `samples/smoke/engine-compare/regex_array_result_mesh.axm` для `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` без `REGEXREPLACE` и без широкого смешанного regex-сценария;
- сценарий зарегистрирован в `src/Axiom.ManualChecks/Program.cs` как отдельный случай `compare-engines`;
- обновлены `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md` и `docs/PLANS3.md`, чтобы regex-подслой массивных результатов больше не числился только внутри широких mixed/log-профилей.

Изменённые файлы:
- `samples/smoke/engine-compare/regex_array_result_mesh.axm`
- `src/Axiom.ManualChecks/Program.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/BYTECODE_PERF_MATRIX.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- compare-engines --filter regex_array_result_mesh.axm --iterations 20 --warmup 3`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать `REGEXMATCHES` / `REGEXEXTRACTALL` / `REGEXSPLIT` отдельным benchmark-семейством;
- фиксировать, что новый regex-подслой сразу показал целевой результат: около `0.673x` runtime и `0.695x` total.

Следующий шаг:
- продолжать искать следующее ещё невыделенное benchmark-семейство;
- если оставаться рядом с regex, то уже выделять отдельно только скалярный подслой без массивных результатов.
## Session 670 - 2026-05-03 18:47 - повторный аудит built-in-реестров подтвердил полную синхронизацию

Что сделано:
- выполнен повторный запуск `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- audit-bytecode-coverage`;
- подтверждено, что служебный аудит по четырём слоям (`BuiltinFunctions`, `BytecodeVm.SupportsBuiltin(...)`, `samples/functions/`, `docs/manual/scripts/search-index.js`) по-прежнему проходит без замечаний;
- обновлён `docs/PLANS3.md`, чтобы built-in-реестры больше не считались отдельным незакрытым направлением работ.

Изменённые файлы:
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet run --project C:\Axiom\src\Axiom.ManualChecks\Axiom.ManualChecks.csproj -- audit-bytecode-coverage`

Решение:
- считать built-in-слой реестров синхронизированным на текущий момент;
- не возвращаться к отдельному ручному аудиту этих четырёх слоёв без нового языкового изменения или нового сигнала о рассинхронизации.

Следующий шаг:
- либо закрывать текущий цикл по байткодной оптимизации;
- либо, если нужен ещё один документный штрих, точечно дочистить остаточные `частично` в `docs/BYTECODE_COVERAGE.md`.
## Session 671 - 2026-05-03 19:33 - TYPE получил многомерные массивные поля

Что сделано:
- снято старое ограничение binder, из-за которого массивные поля внутри `TYPE` принимали только ранг `1`;
- `TypeFieldSymbol` переведён с одиночных fixed-bound полей на хранение списков нижних и верхних границ по всем измерениям, а сериализация байткодного модуля научена сохранять и восстанавливать такие поля;
- `RuntimeRecordValue` теперь создаёт и копирует фиксированные массивные поля записи любого поддержанного ранга;
- добавлены автоматические проверки binder/runtime/bytecode/execution для динамических и фиксированных многомерных полей записи;
- добавлены acceptance-примеры `samples/operators/type/multidimensional_array_fields.axm` и `samples/operators/type/fixed_multidimensional_array_fields.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html` и `docs/BYTECODE_COVERAGE.md`, чтобы пользовательский договор и реестр покрытия больше не содержали старое 1D-ограничение для полей `TYPE`.

Изменённые файлы:
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.Declarations.cs`
- `src/Axiom.Runtime/Values/RuntimeRecordValue.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/type/multidimensional_array_fields.axm`
- `samples/operators/type/fixed_multidimensional_array_fields.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать многомерные динамические и фиксированные массивные поля внутри `TYPE` полноценной частью текущего языкового договора;
- считать этот пласт закрытым одновременно по interpreter, bytecode VM, acceptance-сценариям и manual.

Следующий шаг:
- если продолжать расширение `TYPE`, искать уже не ранговый пробел, а следующую реальную асимметрию между записью и массивом;
- иначе возвращаться к этому месту только при новом пользовательском сигнале или новом документном требовании.
## Session 672 - 2026-05-03 21:22 - подмассивы и диапазонные представления через поля TYPE закреплены как отдельный контракт

Что сделано:
- проверено, что общий механизм частичного индексирования и диапазонных представлений массива уже распространяется и на путь `record.Field(...)`, если поле записи само имеет массивный тип;
- добавлены отдельные parser- и binder-проверки для форм `lineView = board.Cells(2)` и `window = board.Cells(2 TO 3, 3 TO 4)`;
- добавлены отдельные runtime- и bytecode-проверки, подтверждающие, что такие выражения возвращают именно представление на то же хранилище, а изменение через него видно в исходном поле записи;
- добавлен acceptance-пример `samples/operators/type/array_field_views.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html` и `docs/BYTECODE_COVERAGE.md`, чтобы этот путь был зафиксирован не только в общем массивном разделе, но и прямо в пользовательском договоре `TYPE`.

Изменённые файлы:
- `samples/operators/type/array_field_views.axm`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `docs/SPECIFICATION3.md`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать подмассивы и диапазонные представления через массивные поля `TYPE` не скрытым побочным эффектом общего массива, а отдельной публичной частью пользовательского договора;
- не планировать для этого места новую реализацию, пока не появится более глубокая асимметрия или новый продуктовый запрос.

Следующий шаг:
- если продолжать расширение `TYPE`, искать только новый реальный пользовательский пробел, а не повторно документировать уже существующие общие массивные правила;
- без нового сигнала ветку `TYPE` можно считать закрытой и не трогать дальше.
## Session 673 - 2026-05-03 22:16 - TYPE получил именованные литералы через :=

Что сделано:
- добавлен новый синтаксис `:=` для именованных элементов литерала записи в форме `TypeName(Field := expression, ...)`, включая пустую форму `TypeName()` и вложенные литералы пользовательских типов;
- parser научен разбирать именованные элементы вызова, binder отличает литерал `TYPE(...)` от обычного вызова и запрещает использовать `:=` как общий синтаксис именованных аргументов вне литерала записи;
- interpreter, lowering, сериализация байткодного модуля и `BytecodeVm` теперь выполняют именованные литералы записи без fallback, а lowering использует скрытый временный локал для последовательной сборки записи;
- добавлены parser/binder/runtime/bytecode/execution-проверки и acceptance-пример `samples/operators/type/named_literals.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html` и `docs/BYTECODE_COVERAGE.md`, чтобы новый пользовательский контракт был зафиксирован в спецификации, manual и реестре покрытия.

Изменённые файлы:
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Expressions.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Records.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/type/named_literals.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`

Решение:
- считать именованные литералы `TYPE(...)` через `:=` полноценной частью текущего пользовательского договора записей;
- считать синтаксис `:=` в этом шаге локальным только для литералов записи, а не общим механизмом именованных аргументов языка.

Следующий шаг:
- если развивать `TYPE` дальше, выбирать отдельный следующий пользовательский контракт и не расширять `:=` без нового проектного решения;
- в качестве ближайших кандидатов можно рассматривать инициализацию полей по объявлению или структурное сравнение записей.
## Session 674 - 2026-05-04 08:19 - TYPE получил значения полей по умолчанию и структурное сравнение

Что сделано:
- внутри `TYPE` разрешены объявления полей с собственным значением по умолчанию в форме `FieldName AS FieldType = expression`;
- binder теперь принимает только константные scalar/string-инициализаторы полей, сохраняет их в `TypeFieldSymbol` и сообщает `MB2027` для неконстантных, массивных и пользовательских полей;
- `RuntimeRecordValue`, interpreter, сериализация байткодного модуля, validator и `BytecodeVm` научены одинаково применять значения полей по умолчанию при `DIM`, `TypeName()` и частичном литерале `TypeName(Field := ...)`;
- для двух значений одного и того же `TYPE` включено структурное сравнение через `=` и `<>`, включая рекурсивное сравнение вложенных записей;
- для записей с массивными полями структурное сравнение в этом шаге осознанно не включалось: binder оставляет такой случай семантической ошибкой;
- добавлены parser/binder/runtime/bytecode/execution-проверки и acceptance-сценарии `samples/operators/type/default_field_values.axm`, `samples/operators/type/partial_named_literal_defaults.axm`, `samples/operators/type/structural_equality.axm`, а также отрицательный сценарий `samples/acceptance/negative/type_default_non_constant.axm`;
- обновлены `docs/SPECIFICATION3.md`, `docs/manual/type.html`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS3.md`, чтобы новый value-oriented контракт `TYPE` был зафиксирован не только в коде, но и в пользовательских документах и рабочих реестрах.

Изменённые файлы:
- `src/Axiom.Syntax/ProgramAndMembers.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Declarations.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Values/RuntimeRecordValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/type/default_field_values.axm`
- `samples/operators/type/partial_named_literal_defaults.axm`
- `samples/operators/type/structural_equality.axm`
- `samples/acceptance/negative/type_default_non_constant.axm`
- `docs/SPECIFICATION3.md`
- `docs/manual/type.html`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj`
- `dotnet test C:\Axiom\tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj`

Решение:
- считать значения полей по умолчанию полноценной частью текущего пользовательского договора `TYPE`, но пока только для скалярных и строковых полей;
- считать структурное сравнение `TYPE` полноценной частью value-oriented договора записей, но без автоматического глубокого сравнения массивных полей;
- не смешивать этот шаг с `BYREF` / `BYVAL`, методами и конструктором, чтобы следующий контракт ветки `TYPE` оставался отдельным и проверяемым.

Следующий шаг:
- закрепить явный договор передачи и возврата записей через общий механизм `BYREF` / `BYVAL`;
- после этого переходить к методам внутри `TYPE`, `SELF` и конструктору уже на зафиксированной модели передачи значений.

## Session 675 - 2026-05-04 08:55 - договор BYREF / BYVAL закреплён для значений TYPE

Что сделано:
- в lexer, syntax и parser добавлены ключевые слова `BYREF` и `BYVAL`, а сигнатуры пользовательских `SUB` / `FUNCTION` и типов `FUNCTION(...) AS ...` научены хранить режим передачи каждого параметра;
- в semantics и binder введён явный режим передачи параметра, добавлены диагностики `MB2028` и `MB2029`, а `BYREF` на первом шаге ограничен простыми переменными при прямом вызове именованных процедур и функций;
- в interpreter, lowering, bytecode compiler, serializer, validator и `BytecodeVm` проведён общий путь передачи `BYREF`, чтобы изменения аргумента были согласованы между interpreter и байткодным исполнением;
- обновлены ручные acceptance-примеры `samples/operators/sub/byref_record_argument.axm`, `samples/operators/sub/byval_record_copy.axm`, отрицательный сценарий `samples/acceptance/negative/byref_requires_variable.axm`, а также manual-страницы по процедурам и функциям.

Какие файлы менялись:
- `src/Axiom.Lexing/*`
- `src/Axiom.Syntax/*`
- `src/Axiom.Parsing/*`
- `src/Axiom.Semantics/*`
- `src/Axiom.Bytecode/*`
- `src/Axiom.Runtime/*`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/sub/byref_record_argument.axm`
- `samples/operators/sub/byval_record_copy.axm`
- `samples/acceptance/negative/byref_requires_variable.axm`
- `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/procedures-and-functions.html`, `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`, `docs/PLANS3.md`, `docs/BYTECODE_COVERAGE.md`, `docs/WORKLOG4.md`, `docs/ARCHITECTURE.md`

Ограничения шага:
- `BYREF` пока работает только для простых переменных при прямом вызове именованных пользовательских `SUB` и `FUNCTION`;
- составные цели `BYREF` и callback-вызовы через function-value с `BYREF` сознательно не поддержаны в этом шаге.

Проверка:
- полноценный прогон `dotnet test C:\Axiom\Axiom.sln -p:UseSharedCompilation=false` в текущей среде снова упирается в слой restore/инициализации `dotnet`, поэтому завершить надёжную компиляционную валидацию из этого сеанса не удалось;
- дополнительно выполнена ручная перечитка критических узлов `binder`, lowering, `BytecodeCompiler`, `BytecodeModuleValidator` и `BytecodeVm` после внесения маршрута `CallByRef`.

Что осталось:
- вернуть рабочий restore/сборку и прогнать целевые тестовые наборы по parser/semantics/runtime/execution;
- перейти к пользовательским методам внутри `TYPE` и модели `SELF`.

Следующий точный шаг: спроектировать пользовательские методы `TYPE` и модель `SELF` уже поверх зафиксированного договора передачи `BYREF` / `BYVAL`.
## Session 676 - 2026-05-06 20:40 - методы TYPE и SELF доведены до пользовательского договора

Что сделано:
- завершён и документирован пользовательский слой методов `TYPE`: внутри `TYPE` разрешены пользовательские `SUB` и `FUNCTION`, а внутри метода доступно скрытое имя `SELF`;
- добавлены runnable-пример `samples/operators/type/methods_self.axm` и отрицательный acceptance-сценарий `samples/acceptance/negative/type_method_receiver_requires_variable.axm`;
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs расширен положительной и отрицательной проверками для методов `TYPE` и их работы в обычном и байткодном режиме;
- обновлены manual-страницы `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html`, `docs/manual/procedures-and-functions.html` и индекс `docs/manual/scripts/search-index.js`, чтобы методы `TYPE` и `SELF` были видны в справке и находились через поиск;
- в `docs/SPECIFICATION3.md`, `docs/PLANS3.md`, `docs/BYTECODE_COVERAGE.md` и `docs/WORKLOG4.md` зафиксирован новый договор и его текущее ограничение по получателю метода.

Изменённые файлы:
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/type/methods_self.axm`
- `samples/acceptance/negative/type_method_receiver_requires_variable.axm`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/WORKLOG4.md`

Проверка:
- на этом шаге acceptance-слой и документация были доведены до готового состояния, после чего остался обязательный последовательный прогон parser, semantics, runtime и execution-наборов.

Что осталось:
- подтвердить код и acceptance-цепочку последовательными `dotnet test`;
- выбрать следующий договор объектного слоя `TYPE`: расширение допустимых получателей метода или отдельный пользовательский конструктор.

Следующий точный шаг:
- последовательно прогнать parser, semantics, runtime и execution-наборы и затем решить, расширяется ли вызов метода за пределы простой переменной `TYPE`.
## Session 677 - 2026-05-06 21:06 - незавершённые маршруты TYPE/SELF доведены до зелёной тестовой цепочки

Что сделано:
- после возобновления ветки через реальные `dotnet test` найдены и исправлены незавершённые технические хвосты в parser, semantics, bytecode emission и `BytecodeVm`, из-за которых договор `BYREF`, методы `TYPE` и `SELF` был документирован, но ещё не проходил все компиляционные и исполняющие проверки;
- в `src/Axiom.Parsing/Parser.Helpers.cs` добавлены пропущенные маппинги `ByRefKeyword` и `ByValKeyword`, чтобы parser принимал уже существующие токены lexer/syntax;
- в `src/Axiom.Semantics/Binding/Binder.Routines.cs` добавлен недостающий `using Axiom.Core;`, а в `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs` доведены два пользовательских договора: `BYREF` теперь выдаёт `MB2028` для обычных выражений вроде `number + 1`, а временный получатель `TYPE`-метода вроде `Counter(...).Increment()` корректно доходит до `MB2030` вместо ошибки разрешения имени;
- в `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` исправлен выбор источника `BYREF`-аргумента: для поднятых переменных точки входа локальный слот теперь имеет приоритет перед глобальным, поэтому `SELF` и обычный `BYREF` меняют именно ту запись, которую затем читает вызывающая сторона;
- в `src/Axiom.Runtime/Execution/BytecodeVm.cs` и `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` исправлены незавершённые детали маршрута `CallByRef`: устранены неверные helper-имена для глобальных слотов, ослаблена лишняя рантайм-проверка на тип параметра при уже собранных `BYREF`-слотах и добавлен безопасный copy-back `BYREF`-аргументов при возврате кадра.

Изменённые файлы:
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Routines.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `.artifacts/inspect-callbyref/` создавался только как временный диагностический стенд и затем удалён.

Проверка:
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false`

Что осталось:
- следующий продуктовый шаг по `TYPE` больше не технический, а договорный: выбрать расширение допустимых получателей метода или отдельную пользовательскую конструкцию инициализации.

Следующий точный шаг:
- решить, расширяется ли вызов метода за пределы простой переменной `TYPE`, или следующей отдельной веткой вводится пользовательский конструктор/инициализатор для `TYPE`.
## Session 678 - 2026-05-06 21:45 - выбран конструкторный договор TYPE без отдельной ООП-подсистемы

Что сделано:
- после обсуждения следующего шага по `TYPE` зафиксирован выбранный продуктовый путь: сначала не расширять получатели методов, а ввести простой конструкторный договор через уже существующую форму `TypeName(...)`;
- в `docs/SPECIFICATION3.md` оформлен полный пользовательский договор трёх режимов `TypeName()` / `TypeName(Field := ...)` / `TypeName(value1, value2, ...)` с границами первого шага, примерами, ограничениями и ожидаемыми диагностическими случаями;
- в `docs/PLANS3.md` обновлён текущий план: следующим реализуемым шагом становится позиционная полная инициализация `TYPE`, а не расширение получателей методов `SELF`.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- код и тесты в этом шаге не менялись, поэтому дополнительный `dotnet test` не запускался;
- проверена связность нового договора с уже действующими правилами `TYPE`, `BYREF` / `BYVAL`, методов `TYPE` и manual-обязательствами репозитория.

Что осталось:
- перевести новый договор `TypeName(value1, value2, ...)` из документации в parser, binder, lowering, байткод, acceptance-samples и manual;
- отдельно решить диагностические коды для смешанной формы и неверной арности позиционной инициализации `TYPE`.

Следующий точный шаг:
- реализовать позиционную форму `TypeName(value1, value2, ...)` с полной арностью полей и запретом смешивания с именованной формой `Field := ...`.
## Session 679 - 2026-05-06 21:57 - позиционная форма `TypeName(...)` реализована и доведена до acceptance-цепочки

Что сделано:
- в semantics для литералов `TYPE` добавлен новый позиционный режим `TypeName(value1, value2, ...)`, который связывает аргументы с полями по порядку объявления и сохраняет прежнее поведение пустой формы `TypeName()` и именованной формы `TypeName(Field := ...)`;
- добавлены отдельные пользовательские диагностики `MB2031` для смешивания позиционных и именованных аргументов и `MB2032` для неверной арности позиционной формы;
- обновлены parser-, binder-, interpreter-, `BytecodeVm`- и execution-проверки, чтобы новый договор шёл через всю цепочку исполнения;
- добавлены runnable sample `samples/operators/type/positional_constructor.axm` и отрицательные acceptance-сценарии `samples/acceptance/negative/type_literal_mixed_modes.axm`, `samples/acceptance/negative/type_literal_positional_arity.axm`;
- обновлены manual-страницы `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html`, поисковый индекс manual и coverage-документы.

Изменённые файлы:
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Diagnostics.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- `samples/operators/type/positional_constructor.axm`
- `samples/acceptance/negative/type_literal_mixed_modes.axm`
- `samples/acceptance/negative/type_literal_positional_arity.axm`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/manual/scripts/search-index.js`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false`

Что осталось:
- следующий продуктовый шаг по ветке `TYPE` снова договорный: расширение получателей методов либо отдельная пользовательская инициализация с более сильной семантикой.

Следующий точный шаг:
- выбрать, идёт ли следующая ветка в сторону методов на полях/элементах массива, или проектируется отдельный пользовательский конструктор/инициализатор с телом и проверкой инвариантов.

## Session 680 - 2026-05-06 22:15 - TYPE-методы расширены на поля и элементы массива

Что сделано:
- в binder договор TYPE-методов расширен с простой переменной до устойчивых получателей записи: поля пользовательского TYPE и полного доступа к элементу массива;
- в interpreter добавлено разрешение BYREF-хранилища для поля и элемента массива через ссылочную обвязку времени выполнения, а в lowering для BytecodeVm введён точечный copy-back-путь, который привязывает SELF к исходной lvalue-ячейке без открытия общего договора BYREF;
- добавлены binder/runtime/execution-проверки, runnable sample samples/operators/type/method_receivers_field_and_array.axm и обновлена manual-документация по TYPE.

Изменённые файлы:
- src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs
- src/Axiom.Semantics/Binding/Binder.Diagnostics.cs
- src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs
- src/Axiom.Runtime/Execution/Interpreter.Helpers.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Values/RuntimeReferenceStoredValue.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/method_receivers_field_and_array.axm
- docs/manual/type.html
- docs/manual/data-types-records-and-arrays.html
- docs/manual/scripts/search-index.js
- docs/SPECIFICATION3.md
- docs/PLANS3.md
- docs/BYTECODE_COVERAGE.md
- docs/WORKLOG4.md

Проверка:
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- временные получатели TYPE-методов по-прежнему не открыты и требуют отдельного продуктового договора;
- пользовательские конструкторы с телом и инвариантами остаются отдельной следующей веткой.

Следующий точный шаг:
- решить, открываем ли методы TYPE для временных выражений и результатов функций, или следующей отдельной веткой проектируем пользовательскую инициализацию/конструктор с телом.
## Session 681 - 2026-05-07 08:56 - выбран следующий договор TYPE для временных получателей методов

Что сделано:
- перечитаны обязательные документы продолжения: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`;
- восстановлена текущая точка остановки проекта: реализованы методы `TYPE` с `SELF`, позиционная форма `TypeName(...)` и вызов методов на полях/элементах массива, а следующий шаг оставался именно договорным;
- в `docs/SPECIFICATION3.md` зафиксирован следующий продуктовый шаг ветки `TYPE`: временные получатели методов `TYPE` через скрытое временное хранилище без перехода к ссылочной объектной модели;
- в `docs/PLANS3.md` обновлён активный план: следующая кодовая реализация должна открыть вызов методов у литералов `TYPE`, результатов функций и других временных выражений пользовательского типа, а не вводить отдельный конструктор с телом.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменён только проектный договор и рабочий план без изменений исполняемого кода.

Решение:
- не вводить сейчас `SUB New`, `INIT` и другую отдельную конструкторную подсистему для `TYPE`;
- следующим практическим шагом закрыть именно асимметрию вызова метода у временного значения `TYPE`, сохранив value-oriented семантику записи;
- трактовать `SELF` у временного получателя как скрытую временную запись, живущую только на время вызова.

Следующий шаг:
- реализовать binder/runtime/bytecode-договор временных TYPE-получателей без `MB2030` для корректных выражений пользовательского типа;
- затем добавить automated tests, runnable `.axm`-примеры и обновить manual и `docs/BYTECODE_COVERAGE.md` по факту работающего поведения.

## Session 682 - 2026-05-07 12:02 - временные получатели TYPE-методов реализованы

Что сделано:
- в binder снято ограничение на вызов метода `TYPE` только у assignable-получателя: теперь вызов разрешён и для временных выражений корректного пользовательского типа;
- в interpreter путь скрытого `SELF` расширен на временное хранилище, поэтому метод может менять временный получатель без записи обратно за пределы выражения;
- в lowering для `BytecodeVm` добавлен специальный маршрут скрытого временного `SELF`, который не ломает уже существующий copy-back для поля записи и элемента массива;
- добавлены и обновлены binder/runtime/execution-проверки;
- добавлен runnable sample `samples/operators/type/method_receivers_temporary.axm`, удалён устаревший отрицательный sample `samples/acceptance/negative/type_method_receiver_requires_variable.axm`;
- обновлены manual-страницы и `docs/BYTECODE_COVERAGE.md` под новый пользовательский договор.

Изменённые файлы:
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/type/method_receivers_temporary.axm`
- `samples/acceptance/negative/type_method_receiver_requires_variable.axm`
- `docs/manual/type.html`
- `docs/manual/data-types-records-and-arrays.html`
- `docs/manual/scripts/search-index.js`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false`

Что осталось:
- следующий шаг по ветке `TYPE` снова становится договорным, а не техническим: отдельная пользовательская инициализация/конструктор с телом либо другой новый value-oriented контракт.

Следующий точный шаг:
- если продолжать развитие `TYPE`, зафиксировать отдельный договор пользовательского конструктора/инициализации с телом и затем уже переводить его в parser/semantics/runtime/bytecode.

## Session 683 - 2026-05-07 12:03 - уточнён статус дополнительного bytecode-прогона

Что сделано:
- после завершения целевых semantics/runtime/execution-проверок дополнительно запущен `Axiom.Bytecode.Tests`, потому что задача меняла lowering;
- подтверждено, что новый договор временных TYPE-получателей не вносит новых падений в целевые пользовательские сценарии, но общий bytecode-набор по-прежнему содержит два старых несвязанных сбоя валидатора reserved-opcode-диагностик.

Проверка:
- `dotnet test tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj -p:UseSharedCompilation=false`

Результат проверки:
- текущий прогон падает на уже известных несвязанных тестах `BytecodeModuleValidatorTests.Validate_UnsupportedReservedOpcode_ReturnsDiagnostic(opcode: CreateRecord)` и `... (opcode: Dup)`;
- фактические коды диагностик там остаются `BC4012` и `BC4053` вместо ожидаемого в тесте `BC4011`, то есть это отдельный старый хвост validator-набора, а не регрессия временных TYPE-получателей.

Следующий шаг:
- считать текущую TYPE-ветку завершённой по целевым наборам и при отдельной необходимости разбирать validator-хвост reserved-opcode уже как самостоятельную задачу.

## Session 684 - 2026-05-07 12:14 - восстановлена кодировка реестра покрытия bytecode

Что сделано:
- после обновления строки `TYPE` в `docs/BYTECODE_COVERAGE.md` выявлена побочная перекодировка документа;
- файл восстановлен без массовой порчи содержимого, после чего в него повторно внесена только целевая строка про формы `TypeName(...)`, методы `SELF` и sample `method_receivers_temporary`;
- подтверждено, что diff по `docs/BYTECODE_COVERAGE.md` снова сводится к одной содержательной строке без шумовой перекодировки.

Что проверено:
- `docs/BYTECODE_COVERAGE.md` снова читается корректно;
- старый negative-sample `samples/acceptance/negative/type_method_receiver_requires_variable.axm` больше не фигурирует в активных тестах, manual и coverage-реестре;
- исторические упоминания этого sample сохранены только в старых записях `docs/WORKLOG4.md`, что соответствует append-only правилу.

Следующий шаг:
- считать ветку временных получателей `TYPE` закрытой и при следующем продолжении ветки проектировать отдельный договор пользовательского конструктора/инициализации с телом.


## Session 685 - 2026-05-07 12:32 - реализован пользовательский конструктор TYPE через SUB New

Что сделано:
- завершена кодовая ветка пользовательского конструктора TYPE: SUB New(...) теперь объявляется внутри записи, хранится отдельно от обычных методов и связывается через неименованный вызов TypeName(...);
- binder различает литеральный и конструкторный путь TypeName(...), запрещает FUNCTION New внутри TYPE и больше не падает на недопустимом объявлении конструктора;
- interpreter и lowering/BytecodeVm создают запись со значениями по умолчанию, затем выполняют тело SUB New(...) через скрытый SELF и возвращают итоговое значение записи;
- обновлены parser/semantics/runtime/execution-тесты, добавлен acceptance-sample samples/operators/type/constructor_new.axm;
- manual синхронизирован: обновлены docs/manual/type.html, docs/manual/data-types-records-and-arrays.html и индекс docs/manual/scripts/search-index.js;
- coverage-реестр docs/BYTECODE_COVERAGE.md обновлён точечно по строке TYPE без массовой перекодировки файла.

Что проверено:
- dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- следующий шаг по ветке TYPE снова становится договорным: нужно отдельно решить, нужны ли перегрузки и дополнительные инварианты поверх SUB New, или приоритет смещается на другую область языка.

Следующий точный шаг:
- если продолжать TYPE, зафиксировать в спецификации следующий самостоятельный договор поверх SUB New вместо автоматического наращивания объектной модели.
## Session 686 - 2026-05-07 12:56 - реализованы перегрузки SUB New и нулевая форма конструктора TYPE

Что сделано:
- конструкторный путь TYPE расширен до нескольких SUB New(...) в одном типе, если они различаются по числу пользовательских аргументов;
- TypeName() теперь может связываться с явным SUB New(), а при наличии набора конструкторов неименованная форма TypeName(...) больше не откатывается к позиционному литералу;
- binder хранит набор конструкторов TYPE, выбирает перегрузку по арности и выдаёт ошибку объявления для двух SUB New(...) с одинаковой пользовательской арностью;
- обновлены semantics/runtime/execution-тесты и acceptance-sample samples/operators/type/constructor_new.axm, который теперь покрывает нулевую, одноаргументную и двухаргументную формы конструктора, а также обход конструктора через именованный литерал;
- manual и индекс поиска обновлены под перегрузки SUB New по арности;
- coverage-реестр docs/BYTECODE_COVERAGE.md повторно синхронизирован точечно по строке TYPE.

Что проверено:
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- следующий шаг по TYPE снова требует отдельного договора: пользовательские инварианты после конструктора, перегрузка по полной сигнатуре или другой уровень объектной семантики.

Следующий точный шаг:
- если продолжать TYPE, зафиксировать в спецификации, нужен ли после арностных перегрузок отдельный пользовательский договор инвариантов и должна ли система когда-либо различать конструкторы одинаковой арности по типам.
## Session 687 - 2026-05-07 14:00 - реализованы перегрузки SUB New по сигнатуре TYPE

Что сделано:
- расширен binder для конструкторов TYPE: одинаковая пользовательская арность SUB New(...) теперь допустима, если сигнатуры параметров различаются по типам или режимам передачи;
- UserDefinedTypeSymbol хранит набор конструкторов по арности, а выбор перегрузки TypeName(...) идёт по совместимости сигнатуры и количеству точных совпадений типов;
- добавлена отдельная диагностика MB2033 для неоднозначного вызова конструктора, когда несколько перегрузок одинаково подходят;
- обновлены semantics/runtime/execution-тесты и acceptance-sample samples/operators/type/constructor_new.axm, который теперь показывает выбор между конструкторами одинаковой арности по типам параметров;
- manual, план, спецификация, worklog и coverage-реестр синхронизированы под новый пользовательский договор.

Что проверено:
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- следующий шаг по TYPE снова договорный: либо явные пользовательские инварианты после конструктора, либо перенос перегрузок на обычные методы TYPE, либо другая отдельная ветка объектной семантики.

Следующий точный шаг:
- если продолжать TYPE, зафиксировать, нужен ли после сигнатурных перегрузок отдельный пользовательский слой инвариантов или следующая приоритетная ветка — перегрузки обычных методов записи.

## Session 688 - 2026-05-07 14:16 - реализован инвариант TYPE после пользовательского конструктора

Что сделано:
- внутри TYPE введён специальный инвариант FUNCTION IsValid() AS BOOLEAN, который объявляется отдельно от обычных методов и автоматически запускается после успешного SUB New(...);
- binder различает корректную и некорректную форму IsValid, сохраняет инвариант в символе пользовательского типа и не допускает параметризованные или небулевы варианты специального члена;
- interpreter и lowering/BytecodeVm проверяют результат инварианта после конструктора и завершают создание runtime-ошибкой MB3005, если IsValid() возвращает FALSE;
- обновлены semantics/runtime/execution-тесты, добавлены positive sample samples/operators/type/constructor_invariant.axm и negative sample samples/acceptance/negative/type_constructor_invariant_false.axm;
- manual, спецификация, план и coverage-реестр синхронизированы под новый договор, при этом именованная форма TypeName(Field := ...) отдельно закреплена как путь без SUB New(...) и без IsValid().

Что проверено:
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- следующий шаг по TYPE снова становится отдельным договором: переносить ли сигнатурную перегрузку на обычные методы записи и как именно должен работать выбор между перегруженными instance-методами.

Следующий точный шаг:
- если продолжать TYPE, зафиксировать в спецификации правила перегрузки обычных методов по имени и сигнатуре, включая границы для SELF, BYREF и неоднозначных вызовов.

## Session 689 - 2026-05-07 17:40 - зафиксирован договор перегрузки обычных методов TYPE

Что сделано:
- в docs/SPECIFICATION3.md добавлен отдельный продуктовый договор для перегрузки обычных методов TYPE по имени и полной сигнатуре пользовательских параметров;
- отдельно закреплено, что скрытый SELF и тип результата не участвуют в выборе перегрузки, а специальные формы SUB New(...) и FUNCTION IsValid() AS BOOLEAN остаются вне обычного набора методов;
- правило выбора перегрузки согласовано с уже реализованным конструкторным путём: имя, затем арность, затем совместимость сигнатуры, затем число точных совпадений типов, а при остаточной неоднозначности — MB2033.

Что проверено:
- код и тесты не менялись, поэтому отдельный dotnet test в этом шаге не запускался;
- append-only документы docs/SPECIFICATION3.md, docs/PLANS3.md и docs/WORKLOG4.md обновлены согласованно.

Что осталось:
- следующий шаг уже не договорный, а реализационный: провести перегрузку обычных методов TYPE через binder, interpreter, lowering и BytecodeVm.

Следующий точный шаг:
- реализовать перегруженные обычные методы TYPE, затем добавить tests, sample samples/operators/type/method_overloads.axm, обновить manual и синхронизировать docs/BYTECODE_COVERAGE.md.

## Session 690 - 2026-05-07 17:55 - реализована перегрузка обычных методов TYPE

Что сделано:
- UserDefinedTypeSymbol переведён с одиночного метода на набор перегрузок по короткому имени, а Binder теперь различает обычные методы TYPE по полной сигнатуре пользовательских параметров;
- выбор метода через точку согласован с уже существующим конструкторным путём: имя, затем арность, затем совместимость сигнатуры, затем число точных совпадений типов, а при остаточной неоднозначности выдаётся MB2033;
- запрещён повтор обычного метода TYPE с той же пользовательской сигнатурой, даже если отличается только тип результата или форма SUB/FUNCTION;
- обновлены semantics/runtime/execution-тесты, добавлен positive sample samples/operators/type/method_overloads.axm и negative sample samples/acceptance/negative/type_method_overload_ambiguous.axm;
- manual, индекс поиска, план и coverage-реестр синхронизированы под новый пользовательский договор.

Что проверено:
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false

Что осталось:
- следующий шаг по этой ветке снова договорный: оставлять ли перегрузку только внутри TYPE или когда-либо распространять её на глобальные SUB и FUNCTION.

Следующий точный шаг:
- если продолжать тему перегрузок, отдельно зафиксировать границы механизма вне TYPE и только после этого решать, нужна ли реализация для глобальных вызовов.

## Session 691 - 2026-05-07 18:35 - переразложена страница TYPE в manual

Что сделано:
- полностью переписана [docs/manual/type.html](/C:/Axiom/docs/manual/type.html) без изменения семантики языка: материал разложен по отдельным блокам про объявление, формы создания записи, методы и SELF, перегрузку обычных методов, конструкторы и инвариант, value-oriented семантику, массивные поля и диагностику;
- убран прежний перегруженный формат с длинным общим списком замечаний, где договоры TYPE приходилось вылавливать среди несвязанных пунктов;
- пользовательская подача теперь лучше соответствует фактическому масштабу уже реализованной ветки TYPE.

Что проверено:
- код и тесты не менялись, поэтому dotnet test в этом шаге не запускался;
- визуально проверена новая структура разделов и сохранение всех основных пользовательских договоров страницы TYPE.

Что осталось:
- если дальше улучшать manual, такой же переразбор стоит делать и для других крупных страниц, где накопилось несколько разных языковых договоров в одном разделе.

Следующий точный шаг:
- при следующем manual-шаге выбрать следующую перегруженную страницу справочника и разложить её по той же схеме: обзор, основной договор, специальные формы, ограничения, ручная проверка и диагностика.
## Session 692 - 2026-05-07 19:04 - упрощена страница TYPE и добавлен пункт в левое меню

Что сделано:
- перечитаны обязательные проектные документы и текущие manual-страницы `type.html`, `data-types-records-and-arrays.html`, а также навигационный файл `docs/manual/scripts/manual.js`;
- подтверждено пользовательское замечание: страница `type.html` существовала, но не была включена в левое меню manual;
- `docs/manual/scripts/manual.js` обновлён: в разделе `Основы языка` добавлен отдельный пункт `Тип TYPE`;
- `docs/manual/type.html` переписана более простым языком: добавлены секции `Если совсем просто`, `Что такое метод`, `Что такое SELF`, `Что такое конструктор`, `Что такое инвариант`, а примеры сделаны более бытовыми и короткими;
- `docs/manual/data-types-records-and-arrays.html` дополнена более понятными вводными пояснениями и прямой ссылкой на подробную страницу `TYPE`.

Что проверено:
- код языка не менялся, поэтому dotnet test в этом шаге не запускался;
- вручную проверено содержимое `docs/manual/type.html`, `docs/manual/data-types-records-and-arrays.html` и `docs/manual/scripts/manual.js` после правок.

Что осталось:
- если продолжать редакционный проход по manual, следующий полезный шаг — тем же способом проверить соседние крупные страницы на скрытые подстраницы, сухое объяснение терминов и перегруженные обзорные блоки.

Следующий точный шаг:
- при следующем manual-шаге выбрать ещё одну крупную страницу или тематический раздел и проверить три вещи сразу: доступность через левое меню, простоту вводного объяснения и наличие явных определений для терминов, без которых новичку трудно читать страницу.
## Session 693 - 2026-05-07 20:15 - спроектирован первый срез обработки ошибок

Что сделано:
- прочитаны обязательные проектные документы для режима продолжения: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION3.md`, `docs/ARCHITECTURE.md`, `docs/PLANS3.md`, `docs/WORKLOG4.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`;
- проверены текущие упоминания `ON ERROR`, `RESUME`, `TRY`, `CATCH` и runtime-диагностик: активной языковой обработки ошибок пока нет, а `Resume` в архитектуре относится к отладчику VM;
- сверены BASIC-ориентиры именования: классическая линия использует `ON ERROR`, `RESUME`, `ERR`, `ERL`, а современный Visual Basic дополнительно имеет блочную форму `Try...Catch...Finally`;
- в `docs/SPECIFICATION3.md` зафиксирован первый договор Axiom: `TRY`, `CATCH`, `FINALLY`, `END TRY` как структурная альтернатива `ON ERROR GOTO`;
- в `docs/PLANS3.md` добавлен новый следующий участок работ по обработке ошибок.

Изменённые файлы:
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- код не менялся, поэтому `dotnet test` не запускался;
- выполнена документная проверка через чтение проектных документов и поиск существующих упоминаний ошибок.

Что осталось:
- реализовать первый синтаксический срез `TRY`/`CATCH`/`FINALLY`/`END TRY`;
- затем провести модель через binder, interpreter, lowering и `BytecodeVm`;
- добавить automated tests, runnable `.axm`-samples, manual и обновление `docs/BYTECODE_COVERAGE.md`.

Следующий точный шаг:
- начать с lexer/parser/syntax: добавить ключевые слова, syntax-узел и parser/recovery для блока `TRY ... CATCH ... FINALLY ... END TRY` без изменения runtime-поведения до появления bound-модели.

## Session 694 - 2026-05-07 21:03 - реализован синтаксический срез TRY/CATCH/FINALLY

Что сделано:
- в lexer добавлены ключевые слова `TRY`, `CATCH` и `FINALLY`;
- в `Axiom.Syntax` добавлены `TryStatementSyntax`, `CatchClauseSyntax` и `FinallyClauseSyntax`, а также новые `SyntaxKind`;
- parser научен разбирать `TRY ... CATCH name ... END TRY`, `TRY ... FINALLY ... END TRY` и `TRY ... CATCH name ... FINALLY ... END TRY`;
- parser теперь сообщает `MB1001`, если блок `TRY` не содержит ни `CATCH`, ни `FINALLY`;
- добавлены целевые lexer/parser-тесты.

Изменённые файлы:
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet build Axiom.sln -p:UseSharedCompilation=false`

Что осталось:
- `TRY` пока только синтаксически допустим; binder/runtime/BytecodeVm ещё не реализуют обработку ошибок;
- runnable `.axm`-samples, manual и `docs/BYTECODE_COVERAGE.md` нужно обновлять после появления исполняемого поведения.

Следующий точный шаг:
- реализовать binder-срез: bound-узел для `TRY`, локальную область `CATCH err`, тип значения ошибки с полями `Code`, `Message`, `Line`, `Column` и диагностику конфликтов имени catch-переменной.

## Session 695 - 2026-05-07 21:54 - реализован семантический срез TRY/CATCH/FINALLY

Что изменено:
- добавлен встроенный семантический тип `ERROR` с полями `Code AS INTEGER`, `Message AS STRING`, `Line AS INTEGER`, `Column AS INTEGER`;
- добавлен bound-узел `BoundTryStatement` и привязка `TryStatementSyntax`;
- `TRY`, `CATCH` и `FINALLY` привязываются в отдельных дочерних областях видимости;
- `CATCH err` объявляет локальную read-only переменную ошибки, доступную только внутри `CATCH`;
- доступ к полям встроенного типа `ERROR` подключён к существующему механизму точечного доступа;
- добавлены тесты семантики для успешного `TRY/CATCH`, `TRY/FINALLY`, утечки имени, конфликта имени и неизвестного поля ошибки.

Проверки:
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false`;
- `dotnet build Axiom.sln -p:UseSharedCompilation=false`.

Решения:
- пользовательская страница manual и `.axm`-пример не добавлялись в этом срезе, потому что оператор ещё не имеет исполняемого поведения в IDE;
- тип значения ошибки назван `ERROR`: это сохраняет связь с BASIC/VB-моделью `Err`, но не вводит старую форму `ON ERROR GOTO`.

Что остаётся:
- реализовать исполняемый срез в интерпретаторе;
- затем подключить байткод или явно зафиксировать временное ограничение байткодного пути;
- после появления исполняемого поведения добавить `.axm`-пример, manual и при необходимости обновить реестр покрытия байткода.

Следующий точный шаг:
- добавить runtime-значение ошибки и выполнение `BoundTryStatement` в интерпретаторе с гарантированным запуском `FINALLY`.

## Session 696 - 2026-05-07 22:09 - реализован исполняемый срез TRY/CATCH/FINALLY в интерпретаторе

Что изменено:
- добавлено runtime-значение `RuntimeErrorValue` для встроенного типа `ERROR`;
- `RuntimeValue` теперь умеет хранить и отображать значение ошибки;
- интерпретатор исполняет `BoundTryStatement`;
- обработанная runtime-диагностика удаляется из итогового результата и становится значением `CATCH err`;
- доступ `err.Code`, `err.Message`, `err.Line`, `err.Column` работает в runtime;
- `FINALLY` выполняется после обычного тела, после обработанной ошибки и перед необработанной ошибкой;
- добавлен sample `samples/operators/try/handled_runtime_error.axm`;
- добавлена страница manual `docs/manual/try.html`, навигация и поисковый индекс;
- acceptance-аудит временно исключает `samples/operators/try/`, потому что `BytecodeVm` ещё не поддерживает эту форму;
- `docs/BYTECODE_COVERAGE.md` обновлён статусом `TRY` без VM-покрытия.

Проверки:
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false`;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false`;
- `dotnet build Axiom.sln -p:UseSharedCompilation=false`.

Решения:
- первый исполняемый срез сознательно сделан интерпретаторным, чтобы быстро получить проверяемую модель `TRY/CATCH/FINALLY` и не смешивать её с отдельной задачей байткодного lowering;
- manual прямо отмечает временное ограничение байткодного пути;
- для editorial-стиля manual использованы официальные страницы Microsoft Learn по `Try...Catch...Finally` и `On Error` как жанровые ссылки, без переноса несовместимой семантики Visual Basic.

Что остаётся:
- реализовать lowering и выполнение `TRY/CATCH/FINALLY` в `BytecodeVm`;
- вернуть sample `samples/operators/try/handled_runtime_error.axm` в общий положительный bytecode-аудит;
- после VM-поддержки обновить `docs/BYTECODE_COVERAGE.md` со статусом паритета.

Следующий точный шаг:
- спроектировать представление обработчиков в bytecode: границы защищённого блока, вход в `CATCH`, слот значения `ERROR`, гарантированный запуск `FINALLY` и поведение при необработанной ошибке.

## Session 697 - 2026-05-07 22:41 - исправлена IDE-поддержка TRY/CATCH/FINALLY

Что изменено:
- в светлую и тёмную схемы подсветки добавлены `TRY`, `CATCH`, `FINALLY`, а также ранее отсутствовавшие `BYREF`, `BYVAL`, `SELF`;
- структурный редактор теперь вставляет `END TRY` после строки `TRY`;
- строки `CATCH err` и `FINALLY` выравниваются на уровень родительского `TRY`, а следующая строка получает отступ тела ветви;
- обычный запуск из IDE в режиме `Bytecode` при единственной backend-диагностике `MB4001` временно повторяется через интерпретатор, чтобы уже реализованный интерпретаторный `TRY` работал из редактора без ручного переключения режима;
- отладочный запуск с точками останова и остановкой перед первой инструкцией оставлен без отката на интерпретатор.

Изменённые файлы:
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `docs/SPECIFICATION3.md`
- `docs/PLANS3.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/WORKLOG4.md`

Проверка:
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false` проходит.

Решение:
- считать IDE-откат только временной пользовательской мерой до VM-поддержки `TRY/CATCH/FINALLY`;
- не считать этот откат байткодным покрытием и не возвращать sample `samples/operators/try/handled_runtime_error.axm` в общий положительный bytecode-аудит до реализации VM.

Следующий точный шаг:
- реализовать `TRY/CATCH/FINALLY` в lowering и `BytecodeVm`, затем убрать долг по `MB4001` для этой формы и обновить реестр покрытия.
