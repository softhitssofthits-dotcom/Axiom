# WORKLOG

## Session 1 — bootstrap documentation

### Что сделано
- подготовлен `PROJECT.md` с зафиксированной целью проекта;
- подготовлен `README.md`;
- создан `docs/SPECIFICATION.md` со спецификацией языка Axiom v0.1;
- создан `docs/ARCHITECTURE.md` с базовой технической схемой;
- создан `docs/PLANS.md` с ближайшим milestone;
- создана структура `docs/`, `src/`, `tests/`.

### Что решено
- проект стартует как C#-реализация собственного BASIC-подобного языка;
- на стартовом этапе используется интерпретатор, а не native-компиляция;
- строгая совместимость с QuickBASIC не является целью первого этапа;
- язык и документы ведутся вокруг версии v0.1.

### Что ещё не сделано
- не создана .NET solution;
- не созданы csproj;
- не реализованы доменные типы ядра;
- не начата реализация lexer/parser/runtime;
- не начата реализация IDE.

### Следующий точный шаг
Создать .NET solution и пустые проекты для слоёв `Core`, `Lexing`, `Syntax`, `Parsing`, `Semantics`, `Runtime`, `Ide`, а также базовые тестовые проекты.

## Session 2 — solution scaffold and core diagnostics

### Что сделано
- создано решение `Axiom.sln`;
- созданы проекты `Axiom.Core`, `Axiom.Lexing`, `Axiom.Syntax`, `Axiom.Parsing`, `Axiom.Semantics`, `Axiom.Runtime`, `Axiom.Ide`;
- создана WPF-заготовка для `Axiom.Ide`;
- созданы тестовые проекты `Axiom.Lexing.Tests`, `Axiom.Parsing.Tests`, `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests`;
- настроены project references по архитектурной схеме;
- удалены шаблонные заглушки `Class1` и `UnitTest1`;
- в `Axiom.Core` реализованы `TextSpan`, `TextLocation`, `Diagnostic`, `DiagnosticBag`;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`.

### Что решено
- стартовый IDE-проект зафиксирован как WPF;
- solution хранится в классическом формате `.sln`, а не `.slnx`;
- базовый контракт диагностики языка вынесен в `Axiom.Core`;
- `Semantics` на данном этапе не зависит от `Parsing`, чтобы не вводить лишнюю связность раньше времени.

### Что ещё не сделано
- не реализованы сущности лексера `TokenKind`, `Token`, `Lexer`;
- не добавлены первые unit-тесты поверх новой Core/Lexing-основы;
- не начаты синтаксические узлы и parser-логика;
- IDE пока остаётся только проектной заготовкой без предметной функциональности.

### Следующий точный шаг
Реализовать минимальные сущности лексера `TokenKind`, `Token` и пустой каркас `Lexer`, затем добавить первые unit-тесты для лексического слоя и базовых диагностик.

## Session 3 — working lexer for Axiom v0.1

### Что сделано
- в `Axiom.Lexing` добавлен `TokenKind` с минимальным набором токенов для lexer-слоя;
- добавлен класс `Token` с видом токена, исходным текстом, span/location и literal value;
- реализован рабочий `Lexer`, который распознаёт:
  - идентификаторы;
  - ключевые слова;
  - INTEGER/DOUBLE/STRING/BOOLEAN literals;
  - арифметические и сравнительные операторы;
  - `(`, `)`, `,`;
  - комментарии `' ...` и `REM ...`;
  - значимые переводы строк;
  - `EOF`;
- добавлены диагностики на незакрытую строку и недопустимые символы;
- добавлены unit-тесты для lexer;
- обновлены `README.md`, `docs/PLANS.md`, `docs/WORKLOG.md`.

### Что решено
- ключевые слова распознаются без учёта регистра, при этом исходный текст токена сохраняется;
- `TRUE` и `FALSE` выдаются как boolean literal tokens;
- `REM` трактуется как комментарий только когда выступает именно как комментарный префикс, а не часть идентификатора;
- newline выдаётся отдельным token, чтобы позже parser мог использовать его как границу инструкций;
- недопустимые символы дают diagnostic `MB0001`, незакрытая строка — `MB0002`.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test Axiom.sln` — успешно;
- в пустых тестовых проектах `Parsing`, `Semantics`, `Runtime` тестов пока нет, поэтому `dotnet test` сообщает, что доступные тесты там отсутствуют.

### Что ещё не сделано
- не реализованы `SyntaxKind`, `SyntaxNode` и остальные синтаксические сущности;
- не начат parser;
- не начат semantic binder;
- runtime и IDE остаются на уровне каркаса.

### Следующий точный шаг
Подготовить минимальные синтаксические сущности в `Axiom.Syntax` и стартовый parser в `Axiom.Parsing`, используя уже готовый поток токенов lexer.

## Session 4 — repo-local compiler-front-end skill

### Что сделано
- создан локальный skill `compiler-front-end` в `.codex/skills/compiler-front-end`;
- переписан шаблонный `SKILL.md` в краткий repo-specific workflow для узких задач по lexer/parser/AST/diagnostics/binder;
- добавлены UI-метаданные skill в `agents/openai.yaml`;
- обновлены `docs/PLANS.md` и `docs/WORKLOG.md`.

### Что решено
- skill всегда начинает с чтения `AGENTS.md`, `PROJECT.md`, `README.md` и документов из `docs/`;
- skill требует продолжать работу от `docs/PLANS.md`, а не придумывать новый вектор;
- skill закрепляет минимальные и reviewable изменения, обязательные тесты и обязательные обновления `WORKLOG` и `PLANS`;
- skill прямо запрещает перескакивать в несвязанные слои без документированного основания.

### Проверка
- шаблон skill создан через `init_skill.py`;
- попытка запуска `quick_validate.py` показала, что в текущем окружении отсутствует Python-модуль `yaml`, поэтому автоматическая валидация этим скриптом сейчас недоступна;
- структура skill и обязательные файлы проверены вручную.

### Что ещё не сделано
- skill ещё не проходил отдельный forward-test на новых задачах;
- следующий технический шаг проекта по-прежнему относится к `Syntax` и `Parsing`, а не к самому skill.

### Следующий точный шаг
Использовать новый skill для реализации минимальных синтаксических сущностей в `Axiom.Syntax` и стартового parser в `Axiom.Parsing`.

## Session 5 — minimal AST and parser for Axiom v0.1

### Что сделано
- в `Axiom.Syntax` добавлены:
  - `SyntaxKind`, `SyntaxNode`, `SyntaxToken`;
  - `ProgramSyntax`, `MemberSyntax`, `GlobalStatementSyntax`;
  - базовые expression/statement/declaration nodes для v0.1;
- в `Axiom.Parsing` добавлен `Parser`, который:
  - читает уже готовый lexer-поток;
  - строит `ProgramSyntax`;
  - поддерживает top-level statements, `DIM`, `CONST`, `PRINT`, `INPUT`, `IF`, `FOR`, `WHILE`, `SUB`, `FUNCTION`, `RETURN`, assignment и call/expression statements;
  - разбирает выражения с precedence для `NOT`, unary `-`, `*`, `/`, `+`, `-`, comparisons, `AND`, `OR`;
  - не останавливается на первой syntax error и продолжает разбор, когда это возможно;
- добавлены parser unit-тесты на:
  - literal / parenthesized / binary precedence expressions;
  - `DIM`, assignment, `PRINT`;
  - `IF / ELSE / END IF`, `FOR / NEXT`, `WHILE / END WHILE`;
  - `SUB`, `FUNCTION` с `RETURN`;
  - diagnostics на missing `THEN`, missing `END IF`, unfinished expression;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`.

### Что решено
- AST хранит собственные syntax tokens, чтобы не тащить зависимость `Syntax` → `Lexing`, но при этом сохранить ключевые токены для диагностики и IDE;
- parser diagnostics зафиксированы на кодах:
  - `MB1001` — ожидался токен/конструкция;
  - `MB1002` — ожидался идентификатор;
  - `MB1003` — ожидалось выражение;
  - `MB1004` — неожиданный токен/граница блока;
- вне задачи не выходили: semantic analysis, runtime и IDE по-прежнему не начаты как рабочие подсистемы.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj` — успешно, 14/14;
- `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj` — успешно, 8/8;
- `dotnet test Axiom.sln` — успешно; при этом `Axiom.Runtime.Tests` и `Axiom.Semantics.Tests` пока содержат 0 тестов, и `dotnet test` честно сообщает об этом.

### Что ещё не сделано
- не реализован semantic binder;
- не добавлен runtime/interpreter;
- не начаты IDE-фичи поверх нового AST.

### Следующий точный шаг
Перейти к минимальному semantic layer: ввести symbol model для имён/типов, проверку объявлений и первые semantic diagnostics поверх уже готового `ProgramSyntax`.

## Session 6 — mandatory anti-monolith frontend architecture rule

### Что сделано
- в `AGENTS.md` добавлено постоянное обязательное правило репозитория, запрещающее монолитные файлы для language frontend;
- зафиксировано разделение ответственности между lexer, syntax, parser, semantic analysis и runtime execution;
- отдельно зафиксировано, что parser должен разделяться минимум по зонам `expressions`, `statements`, `declarations`, `helpers`, `recovery`;
- в `docs/ARCHITECTURE.md` добавлен отдельный архитектурный раздел против монолитных файлов и рекомендованная структура frontend-слоёв;
- в `docs/PLANS.md` уточнено, что следующий шаг по `Semantics` должен выполняться уже с этим обязательным правилом.

### Что решено
- правило оформлено не как рекомендация, а как mandatory repository rule для всех следующих работ по lexer, parser, syntax, semantics и runtime;
- parser не должен смешиваться с semantic analysis;
- semantic analysis не должен смешиваться с runtime execution;
- если файл растёт или начинает смешивать обязанности, код сначала нужно разбить, и только потом продолжать развитие функциональности.

### Проверка
- изменений в коде решения не было, поэтому `dotnet build` и `dotnet test` в этой сессии не запускались;
- изменения носят документационный и архитектурный характер.

### Что ещё не сделано
- сам semantic layer ещё не начат;
- правило пока только зафиксировано в документах и должно применяться на следующих реализациях.

### Следующий точный шаг
Начать `Axiom.Semantics` с соблюдением нового обязательного anti-monolith rule: ввести минимальные symbol types и binder-структуру в разделённых по ответственности файлах.

## Session 7 — minimal semantic layer and binder

### Что сделано
- в `Axiom.Semantics` добавлены раздельные по ответственности папки `Symbols`, `Scopes`, `BoundNodes`, `Binding`;
- реализованы базовые semantic entities:
  - `TypeSymbol`
  - `VariableSymbol`
  - `ParameterSymbol`
  - `FunctionSymbol`
  - `BoundScope`;
- реализован минимальный bound tree:
  - `BoundNode`
  - `BoundExpression`
  - `BoundStatement`
  - `BoundProgram`
  - набор bound nodes, достаточный для v0.1 binder-проверок;
- реализован `Binder`, который:
  - заранее регистрирует `SUB` и `FUNCTION`;
  - связывает global statements и тела процедур/функций отдельно от parser;
  - проверяет объявления, области видимости, типы, вызовы, `RETURN`, условия и базовые операторные правила;
- добавлены semantic unit-тесты для binder-слоя;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`.

### Что решено
- `Semantics` по-прежнему не зависит от `Parsing`: binder принимает уже готовый `ProgramSyntax`;
- глобальные функции и процедуры регистрируются до связывания исполняемого кода, поэтому вызов допускается до текстового объявления;
- глобальные и локальные переменные живут в разных scope-уровнях, при этом локальное имя может скрывать глобальное;
- отсутствие `RETURN` со значением в `FUNCTION` остаётся не semantic error отсутствия вообще, а runtime decision по текущей спецификации; binder проверяет только форму конкретного `RETURN`;
- в binder добавлены встроенные функции из спецификации как compile-time symbols, без runtime execution logic.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- первый параллельный запуск semantic tests пересёкся со сборкой и упёрся в файловую блокировку `Axiom.Semantics.dll`, это было не кодовое падение;
- повторный `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj` — успешно, 17/17;
- `dotnet test Axiom.sln` — успешно;
- `Axiom.Runtime.Tests` пока по-прежнему не содержит тестов, и `dotnet test` честно сообщает об этом.

### Что ещё не сделано
- не реализован runtime/interpreter;
- не подключено фактическое выполнение bound tree;
- IDE пока не интегрирована с новым semantic layer.

### Следующий точный шаг
Перейти к `Axiom.Runtime`: ввести minimal runtime value model, environment/call stack и исполнение `BoundProgram` для базовых конструкций v0.1.

## Session 8 — executable runtime for Axiom v0.1

### Что сделано
- в `Axiom.Runtime` добавлены разделённые по ответственности папки `Values`, `Environment`, `Host`, `Execution`;
- реализованы минимальные runtime entities:
  - `RuntimeValue`
  - `RuntimeArrayValue`
  - `RuntimeScope`
  - `CallFrame`
  - `IRuntimeHost`
  - `RuntimeResult`
  - `Interpreter`;
- interpreter исполняет:
  - top-level statements;
  - `DIM`, `CONST`, assignment;
  - `PRINT`, `INPUT`;
  - `IF / ELSE`, `WHILE`, `FOR`;
  - вызовы `SUB` и `FUNCTION`;
  - `RETURN`;
  - arithmetic, comparison, logical expressions;
  - string concatenation;
  - array access и array assignment;
- добавлены built-in функции `LEN`, `LEFT`, `RIGHT`, `MID`, `INT`, `STR`, `VAL`, `LCASE`, `UCASE`;
- добавлены runtime diagnostics:
  - `MB3001` — division by zero;
  - `MB3002` — array index out of bounds;
  - `MB3003` — INPUT conversion failure;
  - `MB3004` — STEP 0;
  - `MB3005` — invalid runtime state;
  - `MB3006` — missing return value in FUNCTION;
- добавлены runtime unit-тесты на исполнение выражений, присваиваний, циклов, вызовов, built-ins и runtime diagnostics;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`.

### Что решено
- runtime не смешивается с parser и binder: он исполняет только уже связанный `BoundProgram`;
- ввод и вывод идут через `IRuntimeHost`, чтобы будущий IDE-host мог подключиться без переписывания интерпретатора;
- структура runtime сразу разложена по focused files по anti-monolith rule;
- массивы поддерживаются с inclusive bounds в соответствии со спецификацией;
- параметры `SUB`/`FUNCTION` передаются по значению;
- runtime diagnostics уже структурированы, но пока используют синтетическую location `1:1`, потому что текущий bound model ещё не хранит source locations.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj` — успешно, 13/13;
- `dotnet test Axiom.sln` — успешно;
- проходят все текущие test suites:
  - lexer: 8/8
  - parser: 14/14
  - semantics: 17/17
  - runtime: 13/13

### Что ещё не сделано
- ещё нет единой публичной execution pipeline parse -> bind -> execute;
- runtime пока не подключён к минимальному IDE/host surface;
- точные source-based runtime locations ещё не доведены до bound layer.

### Следующий точный шаг
Перейти к milestone M5: собрать минимальный интегрированный pipeline запуска программы поверх уже готовых `Parsing`, `Semantics` и `Runtime`, не добавляя расширенные IDE-фичи.

## Session 9 — unified execution pipeline and integration boundary

### Что сделано
- добавлен новый проект `Axiom.Execution` как отдельный orchestration layer поверх `Parsing`, `Semantics` и `Runtime`;
- в `Axiom.Runtime` host contract разложен на `IInputProvider`, `IOutputWriter` и `IRuntimeHost`;
- в `Axiom.Execution` добавлены:
  - `ExecutionRequest`
  - `ExecutionResult`
  - `ExecutionStatus`
  - `IDiagnosticSink`
  - `AxiomExecutionPipeline`
  - internal adapter между execution host abstractions и `IRuntimeHost`;
- pipeline:
  - принимает исходный текст;
  - вызывает `Parser.Parse`;
  - при syntax diagnostics завершает выполнение со статусом `SyntaxFailure`;
  - вызывает `Binder.Bind`;
  - при semantic diagnostics завершает выполнение со статусом `SemanticFailure`;
  - вызывает `Interpreter.Execute`;
  - возвращает `Success` или `RuntimeFailure` вместе с output и diagnostics;
- добавлен тестовый проект `Axiom.Execution.Tests` с integration tests на pipeline;
- `Axiom.Ide` теперь может ссылаться на `Axiom.Execution` как на intended integration boundary, без добавления UI-логики в этой сессии;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`.

### Что решено
- единая точка входа в выполнение программы вынесена в отдельный слой, а не добавлена в `Runtime`, чтобы не смешивать orchestration с интерпретатором;
- pipeline не дублирует parser/binder/runtime логику, а только координирует их вызовы;
- host boundary для будущей IDE минимален и UI-независим: input, output и diagnostic sink разделены по ответственности;
- execution statuses теперь различают `Success`, `SyntaxFailure`, `SemanticFailure`, `RuntimeFailure`;
- anti-monolith rule соблюдён: request/result/hosting/pipeline разнесены по focused files.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` — успешно, 8/8;
- `dotnet test Axiom.sln` — успешно;
- суммарно проходят:
  - lexer: 8/8
  - parser: 14/14
  - semantics: 17/17
  - runtime: 13/13
  - execution: 8/8

### Что ещё не сделано
- `Axiom.Ide` ещё не использует pipeline через реальный run service;
- нет даже минимального non-UI application service поверх `Axiom.Execution`, который потом можно будет внедрить в WPF;
- runtime diagnostics по-прежнему ждут более точной source привязки со стороны bound model.

### Следующий точный шаг
Перейти к M6: подключить `Axiom.Ide` к `Axiom.Execution` через минимальный run service и начать тонкую интеграцию запуска текста программы, вывода и diagnostics без расширенных IDE-фич.

## Session 10 — minimal WPF IDE wired to Execution

### Что сделано
- в `Axiom.Ide` убраны лишние прямые project references на frontend/runtime-слои: IDE подключается к языковому ядру через `Axiom.Execution`;
- подключён `AvalonEdit` как минимальный editor control и добавлен focused binding helper для двусторонней привязки текста;
- добавлены focused IDE-слои:
  - `Services/Execution` с `IProgramRunService` и `ProgramRunService`;
  - `Services/Documents` с минимальным `Open / Save` boundary;
  - `Models` для UI-friendly result/diagnostic/document models;
  - `ViewModels` с `MainWindowViewModel`;
  - `Commands` для тонких WPF-команд;
- `App` теперь собирает зависимости и создаёт `MainWindow` с `MainWindowViewModel`, а `MainWindow.xaml.cs` остаётся тонким;
- реализован минимальный UI shell:
  - toolbar с `Open`, `Save`, `Run`;
  - editor area;
  - отдельная панель output;
  - отдельная панель diagnostics;
- сценарий `Run` теперь:
  - берёт текст из editor;
  - вызывает `ProgramRunService`;
  - показывает output;
  - показывает diagnostics с stage/code/message/line/column;
  - не смешивает WPF-слой с parser/binder/runtime;
- добавлен новый тестовый проект `Axiom.Ide.Tests` с минимальными tests для non-UI run service;
- обновлены `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`.

### Что решено
- milestone M6 закрыт минимальной интеграцией, а не расширением IDE до tabs/docking/debugger;
- IDE использует `AxiomExecutionPipeline` только через отдельный `ProgramRunService`, который маппит `ExecutionResult` в UI-friendly model;
- `AvalonEdit` подключён уже сейчас, но без syntax-highlighting customization, inline diagnostics и прочих тяжёлых editor features;
- output очищается на каждый новый run естественным образом через замену `OutputText` новым результатом;
- при неожиданных сбоях run service возвращает infrastructure-level diagnostic вместо падения UI;
- полноценная UI automation для WPF на этом шаге не добавлялась.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, 3/3;
- `dotnet test Axiom.sln` — успешно;
- автоматически покрыт только non-UI run service и существующие language layers; WPF UI полноценно автоматически не smoke-tested.

### Что ещё не сделано
- нет отдельного UX для интерактивного `INPUT` в IDE;
- runtime diagnostics всё ещё ждут более точной source-привязки из bound model;
- не начаты syntax highlighting, переход по diagnostics и другие editor conveniences следующего шага.

### Следующий точный шаг
Сфокусироваться на post-M6 stabilization: довести source-based runtime diagnostics и определить минимальный UX для `INPUT`, после чего добавить только базовую подсветку и безопасную навигацию по diagnostics поверх текущего AvalonEdit.

## Session 11 — startup binding fix for WPF shell

### Что сделано
- исправлен startup-crash в `Axiom.Ide`: binding `OutputText` в `MainWindow.xaml` переведён в `Mode=OneWay`, чтобы WPF не пытался писать в read-only VM property;
- повторно проверено поведение `dotnet run`: после фикса GUI-процесс остаётся запущенным вместо падения на старте.

### Что решено
- причина отсутствия окна была не в команде запуска, а в runtime-ошибке WPF binding ещё до показа окна;
- пока окно IDE запущено, следующий `dotnet build` может упираться в блокировку `Axiom.Ide.exe`, поэтому перед пересборкой приложение нужно закрывать.

### Проверка
- `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` — больше не падает с `InvalidOperationException`; процесс остаётся активным как ожидается для GUI app.

### Следующий точный шаг
Продолжать post-M6 stabilization по текущему плану: source-based runtime diagnostics и минимальный UX для `INPUT`.

## Session 12 — analysis of legacy WPF IDE as reference project

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом анализа;
- проанализирован старый reference project `C:\projects\Axiom\Axiom.Next\AxiomNext.IDE` без массового переноса кода;
- отдельно просмотрены:
  - `MainWindow.xaml` и partial-файлы `MainWindow.xaml.cs`, `MainWindow.Editor.cs`, `MainWindow.IntelliSense.cs`, `MainWindow.DiagnosticsTrace.cs`;
  - `AxiomEditorColorizer.cs` и `AxiomEditorErrorLineRenderer.cs`;
  - `IdeRecentFilesStore.cs`, `IdePreferencesStore.cs`, `EditorPreferencesWindow.*`;
  - `ProgramOutputWindow.*`, `RetroFileDialogWindow.*`, `Themes/RetroTheme.xaml`, `App.xaml.cs`;
- определены три группы:
  - reusable parts;
  - adapt-required parts;
  - do-not-port parts.

### Что решено
- старый проект полезен как reference по WPF shell/theming/AvalonEdit techniques, но не как source для прямого переноса `MainWindow`;
- прямой перенос нельзя делать для кода, который:
  - зависит от `AxiomNext.Compiler`, `AxiomNext.Runtime`, `AxiomNext.Semantics`, `AxiomNext.Syntax`, `AxiomNext.Diagnostics`;
  - тянет за собой `AxiomNextEditorLanguage`, `EditorAnalysisService`, `DiagnosticLanguage`, `DiagnosticSeverity`;
  - реализует старый многооконный Borland-style workspace, debugger placeholders и build/run orchestration прямо в code-behind;
- безопаснее всего переиспользовать только isolated patterns:
  - resource-dictionary theming;
  - recent-files persistence idea;
  - exception logging pattern в `App`;
  - отдельные AvalonEdit extension points вроде colorizer / line renderer, но только после переписывания под Axiom contracts.

### Проверка
- это аналитическая сессия без кодового переноса из старого IDE;
- сборка и тесты не запускались, потому что изменены только `docs/PLANS.md` и `docs/WORKLOG.md`.

### Что ещё не сделано
- не выбран конкретный кусок legacy UX для первого переноса в `Axiom.Ide`;
- не реализованы runtime source-locations, UX для `INPUT`, базовая syntax highlighting и navigation по diagnostics;
- не введён shared theme/resource dictionary в новом `Axiom.Ide`.

### Следующий точный шаг
Сначала продолжать текущий M7-план по runtime diagnostics и `INPUT`, а первым безопасным шагом selective reuse после этого считать не перенос `MainWindow`, а выделение shared `Theme`/resource dictionary и отдельного recent-files service по мотивам старого IDE.

## Session 13 — M7.1 source-based runtime diagnostics

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- проанализированы текущие contracts диагностики в `Core`, syntax/semantic diagnostics и runtime/execution/IDE adapters;
- найдено место потери source mapping: `SyntaxToken` уже хранил `TextLocation`, но bound model его не переносил, а runtime использовал общий synthetic `1:1`;
- в `Axiom.Semantics` добавлен `BoundNode.Location`, и binder начал проставлять source locations для bound expressions, statements, routines и program;
- в `Axiom.Runtime` report helpers переведены на использование bound locations вместо общего synthetic location там, где позиция исходника доступна;
- `Axiom.Execution` и `Axiom.Ide` оставлены на прежней границе, но теперь получают уже mapped runtime diagnostics;
- добавлены и обновлены тесты на runtime/execution/IDE adapter для source-based runtime positions.

### Что решено
- source mapping хранится не в WPF и не в execution shell, а в bound layer как в естественной границе между syntax/binder и runtime;
- runtime diagnostics для `MB3001`, `MB3002`, `MB3003`, `MB3004` и `MB3006` теперь привязываются к реальным позициям исходного текста, когда это возможно;
- execution pipeline не пришлось расширять новыми abstraction layers: он уже передавал `Diagnostic`, и после исправления bound/runtime source mapping этого оказалось достаточно;
- UI `Axiom.Ide` не потребовал переработки layout или новых features: diagnostics panel уже умела показывать `Line`/`Column`, ей понадобились только более качественные данные.

### Проверка
- `dotnet build C:\Axiom\Axiom.sln` — успешно;
- `dotnet test C:\Axiom\Axiom.sln` — успешно;
- проходят обновлённые проверки для runtime, execution и non-UI IDE adapter, включая source positions у runtime diagnostics.

### Что ещё не сделано
- минимальный UX для интерактивного `INPUT` в `Axiom.Ide` всё ещё не реализован;
- не начаты syntax highlighting и безопасная navigation по diagnostics в editor;
- runtime internal fallback diagnostics без естественной source anchor по-прежнему используют fallback location, что допустимо для purely internal invalid state.

### Следующий точный шаг
Реализовать следующий подшаг M7: минимальный UX для `INPUT` в `Axiom.Ide`, не раздувая приложение до консольной оболочки или старой IDE-архитектуры.

## Session 14 — M7.2 minimal INPUT UX in Axiom.Ide

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- проанализированы текущие input abstractions в `Axiom.Runtime` и `Axiom.Execution`:
  - `IInputProvider`
  - `IRuntimeHost`
  - `ExecutionRequest`
  - `ExecutionRuntimeHostAdapter`;
- в `Axiom.Ide` добавлен отдельный IDE-side input boundary:
  - `IProgramInputPromptService`
  - `IdeInputProvider`
  - `ProgramInputDialogService`;
- добавлен минимальный WPF prompt `InputDialogWindow`, который открывается на каждый `INPUT`-запрос и возвращает введённое значение обратно в execution pipeline;
- `ProgramRunService` теперь подключает `IdeInputProvider` к существующему `ExecutionRequest`, не меняя parser/binder/runtime internals;
- `App` обновлён так, чтобы собирать новый input prompt service через composition root;
- добавлены и обновлены tests:
  - execution tests на один и несколько `INPUT`;
  - execution tests на числовой ввод;
  - IDE adapter tests на строковый, числовой и последовательный ввод;
  - IDE adapter test на поведение пустого prompt value.

### Что решено
- для M7.2 выбран самый минимальный UX: отдельный modal prompt dialog на каждый `INPUT`, а не встроенная консольная панель или terminal emulator;
- WPF-диалог не знает parser/binder/runtime internals и живёт только на IDE-стороне;
- интеграция остаётся на прежней границе `Axiom.Ide -> ProgramRunService -> Axiom.Execution -> Axiom.Runtime`;
- закрытие prompt и пустая отправка трактуются одинаково: как пустая строка; отдельная cancellation-semantics на этом шаге не добавлялась;
- invalid `INPUT` conversion по-прежнему обрабатывается existing runtime diagnostic flow и использует source-based runtime locations из M7.1.

### Проверка
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:OutputPath=C:\Axiom\.artifacts\Axiom.Ide\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\Axiom.Ide\obj\` — успешно;
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\Axiom.Execution.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\Axiom.Execution.Tests\obj\` — успешно, 11/11;
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\Axiom.Ide.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\Axiom.Ide.Tests\obj\` — успешно, 8/8;
- стандартный `dotnet build C:\Axiom\Axiom.sln` в этой сессии упирался в файловую блокировку `C:\Axiom\src\Axiom.Ide\bin\Debug\net10.0-windows\Axiom.Ide.dll`, потому что уже запущенный `Axiom.Ide.exe` держал этот файл открытым; код проверен через отдельные output/obj paths без изменения архитектуры проекта;
- полноценная WPF UI automation на этом этапе по-прежнему не добавлялась.

### Что ещё не сделано
- не начаты syntax highlighting и безопасная navigation по diagnostics в editor;
- `INPUT` UX пока intentionally минимален и не пытается стать консольной сессией с history;
- следующий IDE-шаг по-прежнему должен избегать tabs, debugger, autocomplete, settings UI и старой IDE-архитектуры.

### Следующий точный шаг
Добавить только базовые editor conveniences поверх текущего `AvalonEdit`: минимальную подсветку синтаксиса и безопасную навигацию по diagnostics без inline-слоя и без расширенной IDE-логики.

## Session 15 — M7.3 centralized highlighting and safe diagnostic navigation

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- проанализирована текущая интеграция `AvalonEdit` в `Axiom.Ide`: редактор уже был подключён как text editor с двусторонним binding, но без централизованной подсветки и без активной навигации по diagnostics;
- добавлен централизованный highlighting layer для `AvalonEdit`:
  - `Editor/Highlighting/Axiom.xshd`
  - `Editor/Highlighting/AxiomHighlighting.cs`;
- палитра и категории подсветки зафиксированы централизованно, без разнесённых hardcoded brushes по UI-классам;
- первая версия подсвечивает:
  - `keywords`
  - `strings`
  - `numbers`
  - `comments`
  - `type names`
  - `built-in functions`;
- выбран спокойный baseline-стиль, близкий по логике к Visual Studio / C#-IDE, без декоративной схемы;
- добавлен отдельный navigation layer:
  - `Editor/Navigation/DiagnosticNavigationTarget`
  - `Editor/Navigation/DiagnosticNavigationTargetFactory`
  - `Editor/Navigation/AvalonEditDiagnosticNavigation`;
- `DataGrid` diagnostics теперь привязан к `SelectedDiagnostic`, а `AvalonEdit` получает navigation request через attached property вместо логики в `MainWindow.xaml.cs`;
- `MainWindowViewModel` расширен только минимально: хранит выбранный diagnostic и сбрасывает его при open/run сценариях;
- добавлены non-UI tests на:
  - загрузку centralized highlighting definition;
  - корректное построение navigation target;
  - защиту от missing/out-of-range diagnostic locations.

### Что решено
- подсветка не строится от случайных локальных цветов: categories и палитра живут в одном centralized definition;
- IDE не получила прямую зависимость на lexer/parser для первой версии highlighting; это сознательно сохранено как UI-only editor integration;
- безопасная навигация к diagnostic решена через focused editor adapter, а не через монолитный `MainWindow.xaml.cs`;
- навигация минимальна: выбор diagnostic переводит caret/selection к позиции, но не добавляет inline diagnostics, history, squiggles или semantic colorization;
- если diagnostic не содержит корректной source position, переход просто не выполняется и UI не падает.

### Проверка
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:OutputPath=C:\Axiom\.artifacts\M73\Axiom.Ide\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M73\Axiom.Ide\obj\` — успешно;
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\M73\Axiom.Ide.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M73\Axiom.Ide.Tests\obj\` — успешно, 12/12;
- WPF/AvalonEdit визуальное поведение подсветки и реального caret-jump не покрыто полноценной UI automation; автоматически покрыты только non-UI части highlighting/navigation.

### Что ещё не сделано
- не начаты semantic colorization, inline diagnostics, autocomplete, debugger, tabs или settings UI;
- не вынесены shared visual resources/theme dictionary в отдельный shell-level шаг;
- старый `AxiomNext.IDE` по-прежнему остаётся только reference-only источником patterns и не мигрируется напрямую.

### Следующий точный шаг
После закрытия M7 определить следующий минимальный shell-polish шаг для `Axiom.Ide`: вынести shared WPF theme/resources в отдельный dictionary и, если это всё ещё уместно, добавить маленький recent-files/persistence service без ухода в rich IDE.

## Session 16 — M8.1 shared WPF resources in ResourceDictionary

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- проанализированы текущие WPF resources в `Axiom.Ide`: большая часть shared UI значений всё ещё жила как literals прямо в `MainWindow.xaml`;
- создан отдельный `Resources/ShellResources.xaml`, в который вынесены:
  - shared brushes;
  - editor font resources;
  - базовые layout constants (`Thickness`, `CornerRadius`);
  - style для toolbar buttons;
  - style для editor host border;
  - style для read-only output text box;
  - style для diagnostics `DataGrid`;
- `App.xaml` теперь подключает этот dictionary централизованно через merged dictionaries;
- `MainWindow.xaml` переведён на shared resources без изменения сценариев `Open / Save / Run`, `INPUT`, diagnostics navigation или execution integration;
- добавлен небольшой test на загрузку `ShellResources.xaml` и наличие ключевых ресурсов.

### Что решено
- M8.1 не стал редизайном: вынесена только общая resource-структура, а не новая визуальная система;
- не вводились multiple themes, dark/light switching, settings UI или rich-IDE visual hierarchy;
- execution/runtime/parser/binder слои не затрагивались;
- app-level resource merge теперь является одной централизованной точкой подключения shared WPF shell resources.

### Проверка
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:OutputPath=C:\Axiom\.artifacts\M81\Axiom.Ide\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M81\Axiom.Ide\obj\` — успешно;
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\M81\Axiom.Ide.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M81\Axiom.Ide.Tests\obj\` — успешно, 13/13;
- полноценная UI automation и визуальное snapshot-тестирование на этом шаге не добавлялись; автоматически проверены XAML compilation и non-UI загрузка resource dictionary.

### Что ещё не сделано
- ещё не добавлен recent-files/persistence service следующего shell-polish шага;
- не начаты multiple themes, visual redesign, settings UI или другая расширенная визуальная система;
- IDE по-прежнему intentionally остаётся минимальной и не уходит в rich IDE territory.

### Следующий точный шаг
Перейти к M8.2: добавить маленький recent-files/persistence service для `Axiom.Ide`, не затрагивая execution/runtime boundaries и не включая tabs/project system/settings UI.

## Session 17 — M8.2 recent files and shell persistence

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- проанализирован текущий shell `Axiom.Ide`: `Open / Save` уже жили в `Services/Documents`, а `MainWindowViewModel` держал только текущий путь и UI-state без persistence;
- расширен document boundary:
  - `IDocumentFileService.OpenDocument(string? initialDirectoryPath)`
  - `IDocumentFileService.SaveDocument(..., string? initialDirectoryPath)`
  - `IDocumentFileService.LoadDocument(string filePath)` для безопасственного открытия recent entry без WPF-логики в view-model;
- добавлен маленький persistence слой `Services/Persistence/`:
  - `IRecentFilesService`
  - `RecentFilesService`
  - `RecentFilesState`
  - `RecentFilesSnapshot`;
- выбран и реализован минимальный persisted state:
  - список recent file paths;
  - последний каталог для `Open` / `Save` dialogs;
- persistence хранится в `%LocalAppData%\Axiom\Axiom.Ide\recent-files.json`, не затрагивает execution/runtime boundary и не превращается в settings/session system;
- логика recent files реализована безопасно:
  - новые файлы добавляются после успешного `Open` и `Save`;
  - дубликаты не копятся и existing entry переносится наверх;
  - список ограничен небольшим max-size;
  - отсутствующие файлы отфильтровываются при загрузке;
  - corrupted JSON не валит IDE и приводит к пустому snapshot;
- `MainWindowViewModel` получил минимальную интеграцию с recent-files service:
  - загрузка persisted recent files при старте;
  - команда `OpenRecentCommand`;
  - безопасное удаление missing recent entry;
  - использование last-used directory при вызове `Open` / `Save`;
- `MainWindow.xaml` расширен только минимально:
  - добавлен узкий `Recent Files` блок рядом с editor area;
  - выбранный recent file открывается отдельной кнопкой `Open Selected`;
  - shell не превратился в rich startup screen, tabs или document manager;
- `ShellResources.xaml` дополнен shared styles для recent files list, чтобы не разносить новые визуальные значения по локальному XAML;
- добавлены tests:
  - `RecentFilesServiceTests` на добавление, перемещение наверх, dedupe, max-limit, загрузку persisted данных, corrupted JSON и missing files;
  - `MainWindowViewModelTests` на загрузку recent files в shell и открытие выбранного recent entry через document boundary;
  - `ShellResourceTests` обновлён под новые shared resources.

### Что решено
- M8.2 трактуется строго как shell convenience, а не как начало универсальной системы настроек;
- сериализация/десериализация полностью изолирована в `Services/Persistence/` и не уходит ни в `MainWindow.xaml.cs`, ни в execution/runtime слои;
- для минимального UX выбран не menu bar и не startup page, а компактный `Recent Files` блок внутри уже существующего shell layout;
- отсутствующий recent file при попытке открытия не валит UI: entry удаляется из persisted state, а пользователю показывается статусное сообщение;
- last-used path хранится только как лёгкая подсказка для file dialogs и не становится общим app-state manager.

### Проверка
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:OutputPath=C:\Axiom\.artifacts\M82\Axiom.Ide\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M82\Axiom.Ide\obj\` — успешно;
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\M82\Axiom.Ide.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M82\Axiom.Ide.Tests\obj\` — успешно, 22/22;
- попытка собрать `C:\Axiom\Axiom.sln` через общий `BaseOutputPath/BaseIntermediateOutputPath` дала сбой на SDK-уровне для mixed `net10.0` / `net10.0-windows` артефактов, поэтому для этого шага оставлена уже проверенная стратегия project-level build/test с отдельными output paths;
- полноценная WPF UI automation и визуальная проверка real-window поведения recent files на этом шаге не добавлялись.

### Что ещё не сделано
- не проведён отдельный manual smoke-check реального WPF окна на сценарии recent files и missing recent entry;
- persistence по-прежнему намеренно не покрывает settings UI, theme persistence, session restore, tabs или project system;
- M8 всё ещё должен оставаться shell-cleanup milestone, а не переходом в rich IDE territory.

### Следующий точный шаг
Провести точечную ручную стабилизацию M8 shell convenience layer: проверить в живом `Axiom.Ide` сценарии recent files, missing recent entry и last-used path, после чего не расширять persistence дальше без нового документированного решения.

## Session 18 — acceptance smoke and negative regression samples

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы;
- сохранён официальный позитивный acceptance smoke sample в `samples/acceptance/smoke_v0_1.axm`;
- ожидаемый output для smoke scenario зафиксирован отдельно в `samples/acceptance/smoke_v0_1.expected.txt`, чтобы позитивный путь был проверяем и вручную, и автоматически;
- добавлен набор коротких negative acceptance programs в `samples/acceptance/negative/`:
  - `undeclared_variable.axm`
  - `if_requires_boolean.axm`
  - `division_by_zero.axm`
  - `invalid_numeric_input.axm`
  - `for_step_zero.axm`
  - `wrong_argument_count.axm`;
- в `tests/Axiom.Execution.Tests` добавлен `TestProgramLoader`, который находит root репозитория и читает acceptance programs напрямую из `samples/`;
- добавлен `AcceptanceProgramTests`, который:
  - запускает официальный smoke sample с вводом `Иван` и `45`;
  - сравнивает фактический output с зафиксированным expected output;
  - прогоняет negative acceptance programs и проверяет ожидаемые execution statuses/diagnostic codes;
  - отдельно проверяет runtime location для invalid numeric input;
- `README.md` обновлён: теперь в нём явно описаны acceptance samples и актуализирован текущий следующий шаг проекта.

### Что решено
- acceptance smoke test больше не живёт только в ручной переписке: он зафиксирован как репозиторный артефакт и автоматическая regression-проверка;
- выбран путь `samples/acceptance/`, а не временные manual notes, чтобы одни и те же `.axm` файлы подходили и для IDE smoke-run, и для execution-level regression suite;
- negative acceptance layer ограничен базовыми failure paths и не превращается в дублирование всех unit/integration tests нижних слоёв;
- позитивный smoke scenario теперь фактически подтверждает не только основные конструкции, но и набор built-ins, реально использованных в программе, включая `STR`.
- acceptance suite дополнительно зафиксировал, что не-boolean условие в `IF` сейчас диагностируется более точным кодом `MB2008`, а не общим `MB2003`.

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\M83\Axiom.Execution.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\M83\Axiom.Execution.Tests\obj\` — успешно, 18/18;
- acceptance smoke program прошёл end-to-end и совпал с ожидаемым output;
- negative acceptance programs вернули ожидаемые statuses и diagnostic codes.

### Что ещё не сделано
- acceptance samples пока не подключены к отдельному CLI runner, потому что проект по-прежнему идёт через interpreter-first + IDE-first стратегию;
- не добавлен отдельный golden-file runner поверх всего solution, поскольку на этом шаге достаточно execution-level suite;
- ручной smoke-check самих sample files в окне `Axiom.Ide` ещё можно добить отдельно при следующей ручной стабилизации shell.

### Следующий точный шаг
Вернуться к текущему плану M8: провести ручную стабилизацию shell convenience layer в живом `Axiom.Ide`, проверить recent files, missing recent entry и last-used path, не раздувая persistence в полноценную систему настроек.

## Session 19 — manual shell stabilization checkpoint for Axiom v0.1

### Что сделано
- зафиксировано пользовательское подтверждение, что ручная стабилизация живого WPF shell завершена;
- вручную проверенные сценарии `Axiom.Ide` включают:
  - recent files;
  - missing recent entry;
  - last-used path;
- репозиторные документы обновлены так, чтобы текущее состояние проекта больше не расходилось с фактическим статусом ручной проверки;
- закрытие M8 отражено в `README.md` и `docs/PLANS.md` как завершение Axiom v0.1 baseline.

### Что решено
- Axiom v0.1 считается завершённой первой устойчивой базой: язык, interpreter-first execution pipeline, acceptance regression layer и IDE доведены до рабочего состояния;
- ручная shell-стабилизация не используется как повод раздувать persistence в settings UI, session restore, theme persistence или project system;
- следующий шаг проекта должен начинаться не с немедленной новой фичи, а с отдельной фиксации scope следующего milestone в документации.

### Проверка
- отдельные автоматические команды в этой сессии не запускались, потому что изменение касается только актуализации документации по уже выполненной пользователем ручной проверке;
- source of truth в репозитории синхронизирован с фактическим статусом проекта.

### Что ещё не сделано
- не определён и не зафиксирован scope следующей версии после Axiom v0.1;
- не принято отдельное документированное решение о том, какие post-v0.1 возможности идут следующими.

### Следующий точный шаг
Отдельно определить и зафиксировать в документации scope следующего milestone после завершения Axiom v0.1, и только потом переходить к новым feature-изменениям.

## Session 20 — v0.1.5 QuickBASIC-like PRINT/INPUT refinement

### Что сделано
- перечитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- `docs/SPECIFICATION.md` обновлён только в части `PRINT` и `INPUT` под scope v0.1.5;
- `PRINT` переведён с одиночного `PRINT expression` на print-list модель:
  - поддержан пустой `PRINT`;
  - поддержаны `;`, `,`, mixed print-list forms и trailing separators;
  - добавлена минимальная 14-column print-zone логика без console emulation;
- `INPUT` переведён с одиночной переменной на input-list модель:
  - поддержаны `INPUT var`, `INPUT var1, var2`, `INPUT "Prompt"; var`, `INPUT "Prompt", var`;
  - добавлен minimal multi-value parsing по одной логической строке ввода;
  - count mismatch и conversion failure сведены к предсказуемой runtime-диагностике `MB3003`;
- lexer/parser/syntax обновлены под `;`, print-list и input-list;
- parser приведён к anti-monolith правилу: монолитный `Parser.cs` заменён на focused partial files по зонам ответственности;
- semantics/runtime обновлены под binding print-list и input targets list;
- host input boundary расширен минимально через `InputRequest` с prompt text и value count без протаскивания WPF в runtime;
- modal dialog-based `INPUT` UX удалён из `Axiom.Ide` и заменён на встроенную input panel внутри `MainWindow`;
- обновлены unit/integration/IDE tests по новым сценариям `PRINT`/`INPUT`;
- синхронизирован acceptance expected output для официального smoke sample.

### Что решено
- `PRINT ,` реализован не как историческая полная консольная модель, а как документированная фиксированная зона шириной 14 символов, что даёт предсказуемое и тестируемое поведение в текущей архитектуре;
- различие между `INPUT "Prompt"; var` и `INPUT "Prompt", var` синтаксически сохраняется, но в IDE-shell намеренно не разрастается в отдельную модель форматирования prompt;
- execution/runtime boundary остался UI-независимым: runtime знает только про `InputRequest`, а конкретная input panel живёт в IDE-side `Services/Input`;
- `INPUT` diagnostics продолжают ссылаться на location первого target identifier, а не на synthetic UI/runtime position.

### Проверка
- `dotnet build` — успешно;
- `dotnet test` — успешно, 118/118;
- отдельная UI automation реального WPF окна не добавлялась; вместо этого IDE-integrated flow покрыт non-UI/WPF-safe tests для `ProgramRunService` и `ProgramInputPanelService`.

### Что ещё не сделано
- не определён следующий milestone после v0.1.5;
- не вводились новые большие языковые фичи поверх закрытого `PRINT`/`INPUT` scope;
- не добавлялись terminal emulation, tabs, debugger, autocomplete и старая legacy IDE architecture.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий узкий milestone после v0.1.5, не размывая уже закрытый `PRINT`/`INPUT` scope.

## Session 21 — точечная корректировка `PRINT ,` semantics

### Что сделано
- перечитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом точечной правки в Resume mode;
- `docs/SPECIFICATION.md` обновлён только в части `PRINT`, чтобы убрать fixed print-zones и зафиксировать современное separator behavior для `,`;
- в runtime убрана 14-column print-zone логика, а `PRINT ,` переведён на простую separator semantics через один пробел;
- сохранены уже реализованные формы `PRINT`: пустой `PRINT`, `PRINT expr`, trailing separators и mixed print-list с `;` и `,`;
- обновлены runtime/execution tests на новое поведение `PRINT ,`, включая trailing comma и проверку отсутствия fixed-zone padding;
- `docs/PLANS.md` синхронизирован с новой семантикой `PRINT ,` внутри уже закрытого scope v0.1.5.

### Что решено
- Axiom не возвращается к DOS/QuickBASIC print-zones: `,` теперь трактуется как мягкий современный разделитель вывода, а не как переход к фиксированной колонке;
- `;` по-прежнему склеивает вывод без разделителя;
- trailing `,` по-прежнему подавляет автоматический перевод строки и оставляет тот же separator behavior.

### Проверка
- `dotnet build` — успешно;
- первый `dotnet test` завершился техническим file-lock конфликтом с параллельным `dotnet build`, без продуктовой ошибки;
- повторный последовательный `dotnet test` — успешно.

### Что ещё не сделано
- не выбран следующий milestone после закрытого v0.1.5 scope;
- не добавлялись новые языковые фичи поверх точечной корректировки `PRINT`.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий узкий milestone после уже закрытого v0.1.5, не размывая завершённую корректировку `PRINT ,`.

## Session 22 — постоянное workflow rule для runnable `.axm` samples

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- в `AGENTS.md` добавлено постоянное обязательное правило: каждая новая языковая фича, оператор, built-in функция или изменение семантики обязаны сопровождаться runnable `.axm` acceptance sample;
- в `docs/ARCHITECTURE.md` зафиксирован отдельный раздел про language acceptance samples, их назначение, структуру каталогов, naming rules и manual IDE usage;
- в `docs/PLANS.md` добавлено workflow-правило завершения языковых задач: code + automated tests + runnable `.axm` sample в правильной подпапке;
- `README.md` синхронизирован с новой структурой `samples/` и теперь явно различает исторический acceptance layer и новую обязательную иерархию language samples;
- подготовлена базовая иерархия каталогов `samples/operators/`, `samples/functions/`, `samples/smoke/`, `samples/control-flow/`, `samples/expressions/` для всех следующих language tasks.

### Что решено
- одного unit/integration test больше недостаточно для языковых изменений, если они меняют наблюдаемое поведение Axiom;
- runnable `.axm` sample становится не рекомендацией, а обязательным ritual/workflow rule проекта;
- sample обязан лежать в тематической подпапке, быть коротким, runnable вручную через `Axiom.Ide` и упоминаться в финальном отчёте задачи;
- плоская структура `samples/` для новых language-behavior samples запрещена.

### Проверка
- код языка и solution не менялись, поэтому `dotnet build` и `dotnet test` в этой сессии не запускались;
- изменения носят документационный и process-level характер.

### Что ещё не сделано
- исторические acceptance assets из раннего regression layer не мигрировались в новую структуру в рамках этой задачи;
- следующий языковой milestone после v0.1.5 по-прежнему не выбран.

### Следующий точный шаг
При следующем изменении поведения языка применять новое постоянное правило на практике: вместе с кодом и automated tests добавлять runnable `.axm` sample в правильную тематическую подпапку и указывать его в финальном отчёте.

## Session 23 — baseline catalog runnable `.axm` samples

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- создан и заполнен базовый каталог runnable `.axm` samples по новой тематической структуре `samples/`;
- добавлены короткие runnable examples для текущих operators/declarations:
  - `DIM`, `CONST`, assignment;
  - `PRINT`, `INPUT`;
  - `SUB`, `FUNCTION`;
- добавлены runnable examples для control-flow:
  - `IF`;
  - `FOR`;
  - `WHILE`;
  - `RETURN`;
- добавлены runnable examples для expression groups:
  - arithmetic;
  - comparisons;
  - logical expressions;
  - string concatenation;
  - array index access;
- добавлены runnable examples для всех текущих built-in functions:
  - `LEN`, `LEFT`, `RIGHT`, `MID`, `INT`, `STR`, `VAL`, `LCASE`, `UCASE`;
- добавлен отдельный baseline smoke walkthrough `samples/smoke/v0_1_baseline.axm`;
- `README.md` и `docs/PLANS.md` синхронизированы с фактом, что sample structure теперь реально populated, а не только описана как процессное правило.

### Что решено
- репозиторий теперь содержит не только исторический `samples/acceptance/`, но и полноценный иерархический каталог runnable language samples для ручного запуска через `Axiom.Ide`;
- новые `.axm` files сделаны короткими и предметными: один файл = одна основная возможность или маленький связанный набор;
- исторические acceptance assets не мигрировались в этой задаче, чтобы не смешивать создание baseline catalog с отдельным restructuring старых regression files.

### Проверка
- автоматический language harness для новых `.axm` samples не добавлялся, как и планировалось;
- `dotnet build` и `dotnet test` не запускались, потому что код solution не менялся: добавлены только runnable sample files и документы;
- структура каталогов и наличие `.axm` samples в тематических подпапках проверены локально.

### Что ещё не сделано
- старые файлы из `samples/acceptance/` пока не перенесены в новую иерархию;
- новые `.axm` samples пока не подключены к отдельному batch-runner, и это намеренно вне scope текущего шага.

### Следующий точный шаг
При следующем языковом изменении добавлять или обновлять уже существующие тематические `.axm` samples вместе с code/tests и, при необходимости, отдельно решить вопрос миграции исторического `samples/acceptance/` в новую структуру.

## Session 24 — readable Russian parser diagnostics in IDE

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- по скриншоту и коду подтверждено, что проблема была не в WPF rendering, а в испорченных string literals внутри `Axiom.Parsing`;
- в `Parser.cs`, `Parser.Helpers.cs` и `Parser.Statements.cs` заменены mojibake-строки вида `РћР¶...`/`РќРµ...` на нормальные русские parser diagnostics;
- добавлены regression tests в `tests/Axiom.Parsing.Tests/ParserTests.cs` на читаемые сообщения:
  - `Ожидалось выражение.`
  - `Ожидался идентификатор.`
  - `Неожиданный маркер конца блока: 'END'.`

### Что решено
- diagnostics grid в `Axiom.Ide` должен показывать нормальный русский текст для parser diagnostics вместо кракозябр;
- источник проблемы локализован в parser layer, а не в IDE binding, `DataGrid` или WPF encoding;
- bugfix ограничен parser diagnostics и не меняет grammar, semantics, runtime или sample catalog.

### Проверка
- обычный `dotnet build` / `dotnet test` на всём solution упёрся в file lock, потому что живой процесс `Axiom.Ide` держал `Axiom.Parsing.dll`;
- вместо остановки пользовательского IDE-процесса выполнены релевантные isolated checks:
  - `dotnet build tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\diag-fix\Axiom.Parsing.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\diag-fix\Axiom.Parsing.Tests\obj\` — успешно;
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj -p:OutputPath=C:\Axiom\.artifacts\diag-fix\Axiom.Parsing.Tests\bin\ -p:IntermediateOutputPath=C:\Axiom\.artifacts\diag-fix\Axiom.Parsing.Tests\obj\` — успешно, 23/23;
- остаточных mojibake literals в `src/Axiom.Parsing` после патча не найдено.

### Что ещё не сделано
- повторный full-solution build/test в стандартные `bin/obj` не прогонялся после закрытия живого окна `Axiom.Ide`;
- отдельный визуальный re-check в открытом IDE окне пользователь ещё может сделать поверх этого исправления.

### Следующий точный шаг
После закрытия живого процесса `Axiom.Ide` при необходимости повторить обычный full-solution `dotnet build` / `dotnet test`, а по проектному плану затем вернуться к отдельной фиксации следующего post-v0.1.5 milestone.

## Session 25 — built-ins validation hardening

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- проанализированы все текущие built-ins: `LEN`, `LEFT`, `RIGHT`, `MID`, `INT`, `STR`, `VAL`, `LCASE`, `UCASE`;
- сохранено текущее разделение responsibility layers:
  - arity/type validation остаётся в binder по сигнатурам built-ins;
  - domain/value validation усилена в runtime только там, где она зависит от фактических значений;
- runtime built-ins переведены на единый validation path с новой runtime-диагностикой `MB3007` для некорректных аргументов built-in функций;
- `LEFT` и `RIGHT` больше не пропускают отрицательный `count` молча: теперь это runtime diagnostic;
- `MID` теперь использует явную 1-based semantics для `start`, диагностирует `start < 1` и `count < 0`, а `start > LEN(text)` остаётся документированным edge case с пустой строкой без ошибки;
- `INT` теперь диагностирует overflow после `floor` через штатный runtime flow вместо generic exception;
- `VAL("abc")` зафиксирован как неошибочный edge case с результатом `0`;
- в runtime `PRINT` исправлен сопутствующий bug: печать пустой строки через выражение (`PRINT ""`, `PRINT MID(...out-of-range...)`) больше не теряется;
- `docs/SPECIFICATION.md` обновлён точечно: у всех реализованных built-ins теперь явно описаны сигнатуры, arity, типы, value/domain rules, edge cases и слой проверки;
- добавлены runnable `.axm` samples:
  - `samples/functions/right/negative_count_error.axm`
  - `samples/functions/right/out_of_range_count_behavior.axm`
  - `samples/functions/left/negative_count_error.axm`
  - `samples/functions/mid/invalid_start_error.axm`
  - `samples/functions/mid/out_of_range_start_behavior.axm`
  - `samples/functions/len/wrong_arity_error.axm`
  - `samples/functions/val/invalid_text_behavior.axm`
  - `samples/functions/int/overflow_error.axm`

### Что решено
- built-ins в Axiom v0.1.5 больше не должны молча принимать очевидно недопустимые значения аргументов там, где validation естественно относится к runtime;
- единый contract теперь такой:
  - arity/type errors -> semantic diagnostics;
  - invalid built-in value/range at runtime -> `MB3007`;
  - documented non-error edge cases (`LEFT/RIGHT` с большим `count`, `MID` с `start > LEN`, `VAL` на нечисловом тексте) остаются допустимым поведением без runtime failure;
- задача считается закрытой только вместе с automated tests и runnable `.axm` samples по новой структуре `samples/functions/...`.

### Проверка
- первый параллельный запуск `dotnet build`/`dotnet test` дал технический file-lock конфликт в `obj` и тем самым не считается продуктовой проблемой;
- после этого проверки прогнаны последовательно:
  - `dotnet build Axiom.sln` — успешно;
  - `dotnet test Axiom.sln` — успешно, 144/144 тестов;
- наличие новых runnable `.axm` samples в правильных подпапках `samples/functions/...` проверено локально.

### Что ещё не сделано
- отдельный следующий milestone после post-v0.1.5 hardening по-прежнему не выбран;
- automation harness для batch-run запуска новых `.axm` samples по-прежнему намеренно вне scope.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий узкий milestone после завершённого built-ins hardening, не размывая уже закрытые `PRINT`/`INPUT` и validation changes.

## Session 26 — strict non-positive count validation for LEFT/RIGHT/MID

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- по пользовательскому репорту перепроверен validation path для built-ins и подтверждено, что проблема относится к string-slice функциям `LEFT`, `RIGHT` и `MID`: раньше они принимали `count = 0`, а значит также молча пропускали `-0`, `00` и `-00`;
- runtime logic уточнена: `LEFT`, `RIGHT` и `MID` теперь требуют строго `count > 0`, а non-positive значения идут в единый runtime diagnostic flow `MB3007`;
- automated runtime tests расширены на literal-варианты `0`, `-0`, `00`, `-00` и `-1` для `LEFT`, `RIGHT` и `MID`, чтобы подтвердить одинаковое поведение для всех zero-like forms;
- `docs/SPECIFICATION.md` точечно синхронизирован с новой семантикой: `count <= 0` теперь документирован как invalid input для `LEFT`, `RIGHT` и `MID`;
- sample catalog дополнен runnable manual acceptance samples:
  - `samples/functions/right/zero_count_error.axm`
  - `samples/functions/left/zero_count_error.axm`
  - `samples/functions/mid/zero_count_error.axm`
- комментарии в существующих `negative_count_error.axm` для `LEFT` и `RIGHT` обновлены под новое правило `count > 0`.

### Что решено
- zero-like integer literals (`0`, `-0`, `00`, `-00`) больше не считаются допустимым `count` для `LEFT`, `RIGHT` и `MID`;
- rule для string-slice built-ins теперь единообразный и проще для пользователя: `count` должен быть строго больше нуля, а не "неотрицательный";
- остальные текущие built-ins отдельно перепроверены по этой оси: специальных zero-domain ограничений кроме `LEFT`/`RIGHT`/`MID` у них нет, поэтому их поведение не менялось.

### Проверка
- попытка запустить `dotnet build Axiom.sln` и `dotnet test Axiom.sln` в текущей sandbox-среде не дала надёжной продуктовой проверки: `dotnet` завершается с кодом ошибки ещё на раннем MSBuild/Restore path без диагностик компиляции проекта;
- повторная попытка с локальным `DOTNET_CLI_HOME` внутри репозитория и `--no-restore` не устранила проблему среды;
- поэтому в этой сессии автоматически подтверждена только консистентность внесённых файлов и test coverage на уровне исходников, но не выполнен полноценный успешный `dotnet build`/`dotnet test`;
- runnable `.axm` samples проверены локально как короткие целевые программы, пригодные для ручного открытия через `Axiom.Ide`.

### Что ещё не сделано
- не получен зелёный `dotnet build` / `dotnet test` в текущей sandbox-конфигурации, поэтому при первой нормальной среде без этого сбоя стоит повторить стандартную solution-level проверку;
- следующий узкий post-v0.1.5 milestone по-прежнему не выбран.

### Следующий точный шаг
При первой рабочей среде с нормальным `dotnet` повторить стандартные `dotnet build Axiom.sln` и `dotnet test Axiom.sln`, затем отдельно зафиксировать следующий узкий milestone после закрытого built-ins hardening и zero-count follow-up.

## Session 27 — leading-zero numeric literals are no longer valid

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- по пользовательскому репорту локализовано, что проблема не в `RIGHT`, а в lexer semantics числовых литералов: формы вроде `001` и `000+3` раньше молча превращались в обычные числа;
- `Axiom.Lexing` усилен точечно: lexer теперь выдаёт `MB0003` для numeric literals с ведущими нулями в целой части, кроме самого `0` и форм вида `0.xxx`;
- допустимые формы после уточнения:
  - `0`
  - `123`
  - `0.5`
  - `10.25`
- недопустимые формы после уточнения:
  - `00`
  - `001`
  - `00.5`
  - `01.25`
- добавлены lexer tests на invalid leading-zero literals и execution-level regression tests на `PRINT 001 + 1` и `PRINT RIGHT("Hello", 000+3)`;
- automated acceptance layer расширен новым negative sample `samples/acceptance/negative/leading_zero_literal.axm`;
- manual sample catalog расширен runnable file `samples/expressions/numeric_literals/leading_zero_error.axm`;
- прошлые runtime tests для `LEFT`/`RIGHT`/`MID` очищены от `00`/`-00`, потому что эти формы больше не являются валидными выражениями и должны отлавливаться раньше;
- комментарии в zero-count `.axm` samples скорректированы: теперь они относятся только к валидным выражениям `0` и `-0`.

### Что решено
- выражения вроде `001 + 1` и `RIGHT("Hello", 000+3)` больше не должны доходить до semantics/runtime как корректный код;
- leading-zero numeric literals теперь считаются syntax-level problem, а не runtime/domain case;
- runtime правило `count > 0` для `LEFT`/`RIGHT`/`MID` остаётся актуальным только для корректных выражений вроде `0` и `-0`.

### Проверка
- из-за той же текущей проблемы среды с `dotnet` в sandbox полноценный зелёный `dotnet build` / `dotnet test` снова не получен;
- стандартные и targeted `dotnet` команды завершались ошибкой слишком рано на MSBuild/Restore/VSTest path без нормальных продуктовых diagnostics;
- runnable `.axm` samples и соответствие изменённых тестов/документов новой семантике проверены локально по репозиторию.

### Что ещё не сделано
- не получен нормальный подтверждённый solution-level build/test в текущей sandbox-конфигурации;
- следующий узкий post-v0.1.5 milestone по-прежнему не выбран.

### Следующий точный шаг
При первой рабочей среде с нормальным `dotnet` повторить `dotnet build Axiom.sln` и `dotnet test Axiom.sln`, затем отдельно зафиксировать следующий узкий milestone после закрытого built-ins hardening, zero-count follow-up и lexical leading-zero fix.

## Session 28 — `INPUT` в IDE переведён в console-style UX

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- по пользовательскому запросу убран специальный panel/card UX для `INPUT` в `Axiom.Ide`: отдельное поле с кнопкой `Submit` больше не используется;
- в `Services/Input` добавлен новый `ProgramConsoleService`, который совмещает IDE-side prompt flow и live output transcript для панели `Output`;
- `ProgramRunService` теперь использует один и тот же console service как `IProgramInputPromptService` и `IOutputWriter`, поэтому `PRINT` и `INPUT` идут через общий поток внутри `Output`;
- `MainWindow.xaml` переделан: вместо input card теперь используется console-style input line внизу панели `Output`, а подтверждение ввода выполняется по `Enter`;
- `MainWindow.xaml.cs` получил только тонкий shell-level code-behind для автоскролла `Output` и фокуса на текущую строку ввода;
- non-UI IDE tests обновлены под новый UX:
  - старые `ProgramInputPanelService` tests заменены на `ProgramConsoleServiceTests`;
  - `ProgramRunServiceTests` и `MainWindowViewModelTests` переведены на новый console service;
- `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md` синхронизированы с новым поведением `INPUT` в IDE.

### Что решено
- базовый UX `INPUT` для `Axiom.Ide` теперь считается не panel-based, а console-style: prompt и история ввода/вывода живут в `Output`, активный ввод идёт одной строкой без отдельной кнопки;
- execution/runtime boundary не менялся по смыслу: язык и runtime по-прежнему не зависят от WPF и не знают о конкретной визуализации консоли в IDE;
- это изменение трактуется как IDE-shell UX change, а не как изменение семантики языка, поэтому новые `.axm` acceptance samples в этой задаче не требовались.

### Проверка
- `dotnet build Axiom.sln` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, 27/27;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj` — успешно, 42/42;
- `dotnet test Axiom.sln` — успешно.

### Что ещё не сделано
- реальная визуальная smoke-проверка живого WPF окна под ручной сценарий `INPUT` в этой сессии не проводилась;
- следующий отдельный post-v0.1.5 milestone по-прежнему не выбран.

### Следующий точный шаг
Отдельно зафиксировать следующий узкий milestone после стабилизации console-style `INPUT` UX, не расширяя IDE до полноценной терминальной эмуляции.

## Session 29 — добавлен учебник по текущему Axiom baseline

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- дополнительно просмотрен актуальный runnable sample catalog в `samples/`, чтобы опираться не только на спецификацию, но и на уже существующие короткие программы;
- создан новый файл `docs/LANGUAGE_GUIDE.md` — цельный русскоязычный учебник по Axiom для начинающего читателя в дружелюбной, школьной подаче;
- учебник покрывает только текущий поддерживаемый baseline языка:
  - `PRINT`, `INPUT`, `DIM`, `CONST`;
  - базовые типы `INTEGER`, `DOUBLE`, `STRING`, `BOOLEAN`;
  - присваивание, арифметику, сравнения, логику, конкатенацию строк;
  - `IF / ELSE / END IF`, `FOR / NEXT`, `WHILE / END WHILE`;
  - `SUB`, `FUNCTION`, `RETURN`;
  - одномерные массивы;
  - built-in функции `LEN`, `LEFT`, `RIGHT`, `MID`, `INT`, `STR`, `VAL`, `LCASE`, `UCASE`;
- структура книги собрана как единый вводный курс:
  - короткое вступление;
  - главы по каждой основной конструкции;
  - объяснения простыми словами;
  - маленькие runnable-примеры;
  - мини-задания “Попробуй сам”;
  - итоговая краткая шпаргалка;
- `docs/PLANS.md` синхронизирован: появление `docs/LANGUAGE_GUIDE.md` зафиксировано как новый закрытый документный baseline, а также добавлено правило поддерживать учебник в актуальном состоянии при следующих языковых изменениях.

### Что решено
- в репозитории теперь есть не только нормативная спецификация, но и отдельный human-friendly вход в язык, не смешанный с `docs/SPECIFICATION.md`;
- тон учебника выбран “в духе старых вводных книг по BASIC”, но без копирования чужого текста: акцент сделан на коротких объяснениях, понятных аналогиях и постепенном усложнении;
- guide сознательно ограничен только языком и текущим baseline, без превращения документа в полный IDE manual или reference по неподдерживаемым возможностям.

### Проверка
- выполнена ручная editorial-сверка содержания нового `docs/LANGUAGE_GUIDE.md` с `docs/SPECIFICATION.md` и существующими `.axm` примерами из `samples/`;
- отдельно проверено, что в учебнике присутствуют все запланированные крупные главы и все заявленные built-in функции;
- `dotnet build` и `dotnet test` в этой сессии не запускались, потому что задача была документной и не меняла код выполнения языка или IDE.

### Что ещё не сделано
- отдельный следующий узкий post-v0.1.5 milestone по развитию языка по-прежнему не выбран;
- сам учебник пока не проходил отдельную внешнюю педагогическую вычитку “на ребёнке” или на новом пользователе вне текущей сессии.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий узкий milestone развития языка, а при первом следующем языковом изменении синхронно обновлять `docs/SPECIFICATION.md`, sample catalog и `docs/LANGUAGE_GUIDE.md`.

## Session 30 — добавлен `ELSEIF` для многоветвенного `IF`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- отдельно перепроверено текущее поведение `IF` по коду, тестам и ручному пользовательскому сценарию:
  - nested `IF` уже работал и отдельной новой языковой фичей не являлся;
  - искусственный лимит вложенности сознательно не вводился;
- в `docs/SPECIFICATION.md` зафиксирована новая форма `IF / ELSEIF / ELSE / END IF`, а раздел дальнейшего развития очищен от `ELSEIF` как уже реализованной возможности;
- `docs/LANGUAGE_GUIDE.md` синхронизирован: учебная глава про `IF` и итоговая шпаргалка теперь показывают `ELSEIF` как штатную форму многоветвенной проверки;
- lexer расширен новым keyword `ELSEIF`;
- syntax/parsing слой точечно обновлён:
  - добавлен новый syntax node `ElseIfClauseSyntax`;
  - `IfStatementSyntax` теперь хранит список `ELSEIF`-веток;
  - parser умеет читать цепочки `ELSEIF ... THEN` до optional `ELSE` и `END IF`;
- semantic layer обновлён минимально:
  - `ELSEIF` chain понижается в уже существующую bound-модель через вложенный `BoundIfStatement` в else-ветке;
  - runtime-model и interpreter по смыслу не усложнялись отдельной новой сущностью;
- обновлены существующие automated tests:
  - lexer test на case-insensitive keyword recognition теперь включает `ELSEIF`;
  - parser test suite дополнен разбором `IF / ELSEIF / ELSE / END IF`;
  - semantics test suite подтверждает lowering `ELSEIF` chain в nested else-branch;
  - runtime и execution tests подтверждают корректное выполнение `ELSEIF`-цепочек;
- добавлен runnable acceptance sample `samples/control-flow/if/elseif_chain.axm`.

### Что решено
- следующим узким language milestone после v0.1.5 стал именно `ELSEIF`, без расползания сразу в `DO ... LOOP`, `EXIT FOR` и другие направления;
- nested `IF` официально трактуется как уже работающий baseline и не получает искусственного лимита вложенности;
- `ELSEIF` реализован как компактная форма многоветвенного `IF`, а не как отдельный новый execution primitive.

### Проверка
- успешно выполнены targeted automated tests:
  - `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj`
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- попытка прогнать `dotnet build Axiom.sln` в этой сессии завершилась не кодовой ошибкой, а file-lock проблемой: запущенный `Axiom.Ide.exe` держал DLL IDE-проекта и не дал пересобрать solution целиком;
- runnable sample `samples/control-flow/if/elseif_chain.axm` добавлен в правильную тематическую подпапку и пригоден для ручного запуска через `Axiom.Ide`.

### Что ещё не сделано
- полноценный зелёный `dotnet build Axiom.sln` / `dotnet test Axiom.sln` после этого изменения не подтверждён именно в этой сессии из-за внешнего lock со стороны уже запущенной IDE;
- следующий узкий post-v0.1.6 milestone по-прежнему не выбран.

### Следующий точный шаг
После закрытого `ELSEIF` milestone отдельно выбрать и зафиксировать следующий маленький language scope, а при первой сессии без lock от запущенного `Axiom.Ide.exe` повторить обычные solution-level `dotnet build Axiom.sln` и `dotnet test Axiom.sln`.

## Session 31 — syntax highlighting в IDE синхронизирован для `ELSEIF`

### Что сделано
- после пользовательской проверки live IDE зафиксировано, что `ELSEIF` уже работает как языковая конструкция, но не подсвечивается в редакторе `Axiom.Ide`;
- централизованная AvalonEdit highlighting definition `Axiom.xshd` обновлена: `ELSEIF` добавлен в список `Keyword`;
- IDE test suite точечно усилен без создания отдельного нового набора тестов:
  - в `EditorFeatureTests` добавлена проверка embedded XSHD resource на наличие `<Word>ELSEIF</Word>`;
- `docs/PLANS.md` синхронизирован, чтобы репозиторий явно фиксировал этот IDE-side follow-up после language milestone.

### Что решено
- `ELSEIF` теперь должен подсвечиваться в редакторе как обычное ключевое слово языка, на одном уровне с `IF`, `ELSE`, `THEN`, `FOR`, `WHILE` и остальными keywords;
- regression guard для syntax highlighting теперь не ограничивается только named colors, а отдельно страхует от пропуска новых keywords в embedded resource.

### Проверка
- source-level проверка выполнена напрямую по изменённым файлам:
  - в `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd` присутствует `<Word>ELSEIF</Word>`;
  - IDE regression test обновлён под это поведение;
- попытка выполнить `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` в этой сессии не дошла до запуска тестов из-за внешнего file lock: уже запущенный `Axiom.Ide.exe` удерживал `Axiom.Ide.dll`/`Axiom.Ide.exe` в `bin\Debug\net10.0-windows`.

### Что ещё не сделано
- первоначально полноценный зелёный прогон `Axiom.Ide.Tests` был заблокирован открытым экземпляром IDE, но после закрытия `Axiom.Ide.exe` проверка успешно повторена;
- общий следующий post-v0.1.6 milestone по развитию языка по-прежнему не выбран.

### Следующий точный шаг
При первой сессии без запущенного `Axiom.Ide.exe` повторить `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`, затем вернуться к выбору следующего узкого language milestone.

## Session 32 — школьный учебник уточнён для `ELSEIF` и вложенных `IF`

### Что сделано
- по пользовательскому запросу точечно обновлён раздел `## Условие IF` в `docs/LANGUAGE_GUIDE.md`;
- в учебной главе про `IF` оставлен пример с `ELSEIF`, но объяснение сделано более явным и дружелюбным;
- добавлен отдельный короткий пример вложенного `IF`, чтобы читатель видел не только цепочку `ELSEIF`, но и форму “условие внутри условия”;
- в блоках `Объяснение простыми словами`, `Попробуй сам`, `Запомни` и `Шпаргалка` явно зафиксировано:
  - что такое `ELSEIF`;
  - что `IF` можно вкладывать один в другой;
  - что специальный маленький лимит вложенности в Axiom не вводится.

### Что решено
- учебник теперь объясняет обе реальные формы ветвления проще и нагляднее:
  - многоветвенный `IF` через `ELSEIF`;
  - вложенный `IF` как отдельный понятный приём;
- для понимания этой темы больше не нужно идти в техническую спецификацию: базовое объяснение уже есть прямо в школьной книге.

### Проверка
- выполнена ручная editorial-проверка обновлённого раздела `IF` в `docs/LANGUAGE_GUIDE.md`;
- код, runtime и IDE в этой задаче не менялись, поэтому `dotnet build` и `dotnet test` дополнительно не запускались.

### Что ещё не сделано
- следующий узкий post-v0.1.6 milestone по развитию языка по-прежнему не выбран;
- сам учебник всё ещё не проходил отдельную внешнюю педагогическую вычитку на новом читателе.

### Следующий точный шаг
Вернуться к выбору следующего маленького language milestone, а при следующих языковых изменениях синхронно обновлять и `docs/LANGUAGE_GUIDE.md`, если меняется уже описанный синтаксис.

## Session 33 — добавлен `EXIT FOR`, подтверждены вложенные циклы и синхронизированы IDE/docs

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом работы в Resume mode;
- по пользовательскому запросу выбран и реализован следующий узкий milestone после `ELSEIF`: `EXIT FOR`;
- lexer расширен новым keyword `EXIT`, parser получил отдельный `ExitForStatementSyntax`, а syntax/bound model — соответствующие узлы для `EXIT FOR`;
- binder получил отдельную semantic-проверку контекста:
  - `EXIT FOR` допустим только внутри `FOR ... NEXT`;
  - вне `FOR` выдаётся semantic diagnostic `MB2013`;
- runtime execution model расширен минимально:
  - рядом с уже существующим signal `RETURN` добавлен focused signal для `EXIT FOR`;
  - `FOR` поглощает этот сигнал как выход из ближайшего цикла;
  - `WHILE` не поглощает его, а пробрасывает вверх, чтобы `EXIT FOR` корректно работал и внутри вложенных `WHILE` в теле `FOR`;
- отдельно перепроверена и закреплена поддержка вложенных циклов:
  - добавлены automated tests на nested `FOR`/`WHILE`;
  - добавлен runnable sample `samples/control-flow/for/nested_loops.axm`;
- существующие test suites точечно расширены без создания новых наборов тестов:
  - lexer tests теперь знают про `EXIT`;
  - parser tests покрывают `EXIT FOR` и вложенные циклы;
  - binder tests покрывают допустимый `EXIT FOR` и semantic failure вне `FOR`;
  - runtime и execution tests покрывают ранний выход из ближайшего `FOR` и nested loop execution;
  - IDE tests проверяют наличие `EXIT` в embedded highlighting resource;
- добавлен runnable acceptance sample `samples/control-flow/for/exit_for.axm`;
- IDE syntax highlighting синхронизирован: `EXIT` добавлен в централизованную AvalonEdit highlighting definition;
- `docs/SPECIFICATION.md` обновлён до v0.1.7:
  - `EXIT FOR` добавлен в supported baseline;
  - спецификация `FOR` и grammar расширены;
  - `EXIT FOR` убран из раздела дальнейшего развития;
- `docs/LANGUAGE_GUIDE.md` обновлён:
  - в главе про `FOR` объяснён `EXIT FOR`;
  - в главах про `FOR` и `WHILE` прямо зафиксировано, что циклы можно вкладывать друг в друга;
  - итоговая шпаргалка синхронизирована с новым syntax.

### Что решено
- `EXIT FOR` реализован как маленькое, изолированное расширение языка без перехода к более крупным control-flow возможностям вроде `DO ... LOOP` или `SELECT CASE`;
- вложенные циклы трактуются как уже работающий baseline языка и не получают искусственного лимита вложенности;
- IDE-side follow-up по новой языковой фиче делается сразу: новый keyword считается завершённым только после синхронизации highlighting и учебного guide.

### Проверка
- успешно выполнен полный solution-level прогон:
  - `dotnet test C:\Axiom\Axiom.sln`
- в прогон вошли:
  - `Axiom.Lexing.Tests` — 12/12;
  - `Axiom.Parsing.Tests` — 26/26;
  - `Axiom.Semantics.Tests` — 30/30;
  - `Axiom.Runtime.Tests` — 45/45;
  - `Axiom.Execution.Tests` — 31/31;
  - `Axiom.Ide.Tests` — 29/29;
- вручную runnable samples для этого milestone сохранены в правильной тематической подпапке `samples/control-flow/for/` и пригодны для открытия через `Axiom.Ide`.

### Что ещё не сделано
- следующий узкий post-v0.1.7 milestone развития языка пока не выбран;
- отдельная визуальная ручная проверка live WPF IDE для `EXIT FOR` в этой сессии не проводилась, хотя IDE-side highlighting и non-UI tests зелёные.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.7 language milestone, а при первом следующем языковом изменении снова синхронно обновлять `docs/SPECIFICATION.md`, `docs/LANGUAGE_GUIDE.md`, automated tests, IDE highlighting и runnable `.axm` samples.

## Session 34 — `DIM` расширен до групповых объявлений переменных

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом нового language slice в Resume mode;
- по пользовательскому запросу оператор `DIM` расширен до grouped declaration forms:
  - `DIM i AS INTEGER, j AS INTEGER`
  - `DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING`;
- правило синтаксиса зафиксировано так:
  - `AS TYPE` относится ко всей ближайшей группе имён слева;
  - если после группы указан `= expression`, это начальное значение относится ко всем переменным этой группы;
- syntax layer обновлён:
  - введён `VariableDeclaratorSyntax` для отдельного имени с optional array bounds;
  - `VariableDeclarationSyntax` теперь хранит группу declarator-ов с общим `AS TYPE` и optional initializer;
  - `DimStatementSyntax` теперь хранит список declaration group-ов в одном операторе `DIM`;
- parser обновлён под grouped `DIM` без добавления новых keywords и без изменения старых одиночных форм;
- binder обновлён:
  - один оператор `DIM` теперь разворачивается в несколько обычных variable declarations;
  - duplicate declaration checks по-прежнему работают для каждого имени отдельно;
- runtime/execution обновлены минимально:
  - добавлен bound-node для списка variable declarations;
  - execution проходит по нему как по последовательности обычных объявлений;
- существующие automated test suites точечно расширены:
  - parser tests проверяют grouped `DIM`;
  - semantics tests проверяют развертывание grouped declarations в typed variables;
  - runtime и execution tests проверяют фактическое исполнение программы с новой формой `DIM`;
- добавлен runnable acceptance sample `samples/operators/dim/multiple_declarations.axm`;
- `docs/SPECIFICATION.md` обновлён до v0.1.8, а `docs/LANGUAGE_GUIDE.md` синхронизирован под новый `DIM`.

### Что решено
- Axiom теперь поддерживает более удобный QuickBASIC-подобный стиль объявления нескольких переменных одной строкой, но без неявных типов;
- тип по-прежнему остаётся обязательным для каждой группы имён, то есть язык не вводит implicit-typing или variant-поведение;
- grouped `DIM` реализован как маленькое, локальное расширение уже существующей модели языка, без расползания в отдельную систему declarations или scope rules.

### Проверка
- добавлены и обновлены automated tests в:
  - `tests/Axiom.Parsing.Tests/ParserTests.cs`
  - `tests/Axiom.Semantics.Tests/BinderTests.cs`
  - `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
  - `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- added runnable sample:
  - `samples/operators/dim/multiple_declarations.axm`
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- успешно выполнен полный solution-level прогон:
  - `dotnet test C:\Axiom\Axiom.sln`

### Что ещё не сделано
- следующий узкий post-v0.1.8 milestone по развитию языка пока не выбран.

### Следующий точный шаг
После закрытия grouped `DIM` milestone отдельно выбрать и зафиксировать следующий маленький post-v0.1.8 language scope.

## Session 35 — добавлен минимальный baseline `DO WHILE ... LOOP`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом нового language slice в Resume mode;
- по пользовательскому запросу выбран и реализован следующий узкий milestone после grouped `DIM`: только форма `DO WHILE condition ... LOOP`;
- lexer расширен новыми keyword `DO` и `LOOP`;
- syntax layer обновлён:
  - добавлен `DoWhileStatementSyntax`;
  - `SyntaxKind` и token mapping синхронизированы с новой конструкцией;
- parser обновлён:
  - `DO WHILE ... LOOP` добавлен в dispatch списка инструкций;
  - реализован отдельный разбор pre-check цикла без расширения до других `DO/LOOP` форм;
  - `LOOP` добавлен в boundary handling, чтобы nested parsing и recovery вели себя предсказуемо;
- binder обновлён минимально:
  - добавлен `BoundDoWhileStatement`;
  - условие по-прежнему обязано быть `BOOLEAN`;
  - тело цикла связывается тем же образом, что и для других блоков;
- runtime/execution обновлён:
  - `DO WHILE` исполняется как цикл с проверкой условия перед каждой итерацией;
  - при ложном условии тело не выполняется ни разу;
  - сигналы `RETURN` и `EXIT FOR` проходят через новый цикл согласованно с уже существующей моделью;
- IDE syntax highlighting синхронизирован:
  - `DO` и `LOOP` добавлены в централизованную AvalonEdit highlighting definition;
- существующие automated test suites точечно расширены:
  - lexer tests знают про `DO` и `LOOP`;
  - parser tests покрывают базовый `DO WHILE` и его вложение в `FOR`;
  - semantics tests покрывают binding `DO WHILE`;
  - runtime и execution tests покрывают исполнение базового и вложенного `DO WHILE`;
  - IDE tests проверяют наличие `DO` и `LOOP` в embedded XSHD resource;
- добавлен runnable acceptance sample `samples/control-flow/do/do_while_loop.axm`;
- `docs/SPECIFICATION.md` обновлён до v0.1.9:
  - `DO WHILE / LOOP` добавлен в supported baseline;
  - grammar и раздел control-flow синхронизированы;
  - в future list перенесены только остальные формы семейства `DO/LOOP`, а не уже реализованная базовая форма;
- `docs/LANGUAGE_GUIDE.md` обновлён:
  - добавлена отдельная школьная глава про `DO WHILE`;
  - разделы про `FOR` и `WHILE` уточняют, что в них можно вкладывать `DO WHILE`;
  - итоговая шпаргалка синхронизирована с новым циклом.

### Что решено
- `DO WHILE ... LOOP` реализован как самый маленький полезный slice семейства `DO/LOOP`, без попытки сразу добавить все исторические варианты BASIC;
- текущий baseline языка сознательно не включает:
  - `DO UNTIL`;
  - `LOOP WHILE`;
  - `LOOP UNTIL`;
  - `EXIT DO`;
- вложенность `DO WHILE` не ограничивается искусственным лимитом и проверяется теми же общими правилами вложенных блоков, что уже действуют для `IF`, `FOR` и `WHILE`.

### Проверка
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj`
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
  - `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`
- успешно выполнен полный solution-level прогон:
  - `dotnet test C:\Axiom\Axiom.sln`
- успешно выполнена полная сборка solution:
  - `dotnet build C:\Axiom\Axiom.sln`
- runnable sample для milestone сохранён в правильной тематической подпапке:
  - `samples/control-flow/do/do_while_loop.axm`

### Что ещё не сделано
- следующий узкий post-v0.1.9 milestone развития языка пока не выбран;
- остальные формы семейства `DO/LOOP` пока сознательно остаются вне baseline и требуют отдельного решения, если к ним вернёмся позже.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.9 language scope, не расползаясь сразу в несколько языковых направлений.

## Session 36 — добавлен `EXIT DO` для раннего выхода из `DO ... LOOP`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед началом нового узкого language milestone в Resume mode;
- по пользовательскому запросу выбран и реализован следующий маленький шаг после baseline `DO WHILE`: команда `EXIT DO`;
- syntax layer обновлён:
  - добавлен `ExitDoStatementSyntax`;
  - `SyntaxKind` расширен новым statement-kind для `EXIT DO`;
- parser обновлён:
  - `EXIT` теперь разбирается как общий prefix для `EXIT FOR` и `EXIT DO`;
  - `EXIT DO` превращается в отдельный syntax node, а не в вариацию `EXIT FOR`;
- semantics/binder обновлён:
  - добавлен `BoundExitDoStatement`;
  - введён отдельный semantic context counter для `DO ... LOOP`;
  - `EXIT DO` допустим только внутри `DO ... LOOP` и вне этого контекста выдаёт отдельную semantic diagnostic `MB2014`;
- runtime/execution обновлён:
  - execution signal расширен новым focused case для `EXIT DO`;
  - ближайший `DO ... LOOP` поглощает этот signal как ранний выход;
  - `FOR` и `WHILE` не поглощают `EXIT DO`, а пробрасывают его вверх до ближайшего окружающего `DO`;
- отдельный IDE follow-up для highlighting не потребовался, потому что keywords `EXIT` и `DO` уже были синхронизированы в предыдущих milestone;
- существующие automated test suites точечно расширены:
  - parser tests знают про `EXIT DO`;
  - semantics tests покрывают корректный и ошибочный контекст для `EXIT DO`;
  - runtime tests подтверждают, что `EXIT DO` выходит именно из ближайшего `DO`;
  - execution tests покрывают success path и semantic failure path;
- добавлен runnable acceptance sample `samples/control-flow/do/exit_do.axm`;
- `docs/SPECIFICATION.md` обновлён до v0.1.10:
  - `EXIT DO` добавлен в правила `DO WHILE`;
  - grammar расширена `exitDoStmt`;
  - `EXIT DO` убран из списка будущих форм семейства `DO/LOOP`, потому что теперь он входит в baseline;
- `docs/LANGUAGE_GUIDE.md` обновлён:
  - в главе про `DO WHILE` добавлен школьный пример с `EXIT DO`;
  - итоговая шпаргалка синхронизирована с новым синтаксисом.

### Что решено
- `EXIT DO` реализован как маленькое продолжение уже существующего baseline `DO WHILE ... LOOP`, а не как повод расширять весь historical BASIC syntax;
- контекстная проверка для `EXIT DO` намеренно выделена в отдельную diagnostic, чтобы ошибки `EXIT FOR` и `EXIT DO` не смешивались;
- runtime model сохраняет предсказуемость: `EXIT FOR` завершает только ближайший `FOR`, а `EXIT DO` — только ближайший `DO`.

### Проверка
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- успешно выполнен полный solution-level прогон:
  - `dotnet test C:\Axiom\Axiom.sln`
- успешно выполнена полная сборка solution:
  - `dotnet build C:\Axiom\Axiom.sln`
- runnable sample для milestone сохранён в правильной тематической подпапке:
  - `samples/control-flow/do/exit_do.axm`

### Что ещё не сделано
- следующий узкий post-v0.1.10 milestone развития языка пока не выбран;
- формы `DO UNTIL`, `LOOP WHILE` и `LOOP UNTIL` по-прежнему остаются вне текущего baseline и требуют отдельного решения.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.10 language scope, не расползаясь сразу в несколько языковых направлений.

## Session 37 — очищены временные build-артефакты и логи из рабочей копии

### Что сделано
- по пользовательскому запросу выполнена уборка временных файлов и build-артефактов, которые не относятся к исходникам проекта;
- удалены корневые диагностические и build-логи:
  - `parser-build.log`
  - `parser-err.txt`
  - `parser-out.txt`
  - `parser-test.log`
  - `parsing-build.log`
  - `runtime-build.log`
  - `semantics-build.log`
  - `syntax-build.log`
- удалены временные служебные каталоги, созданные локальными прогонами:
  - `.artifacts/`
  - `.dotnet-home/`
  - `.dotnet-cli/`
  - `.tmp-build/`
- рекурсивно удалены generated-каталоги `bin/` и `obj/` внутри `src/` и `tests/`;
- проверено, что в корне репозитория после уборки остались только проектные директории и основные документы.

### Что решено
- build outputs и локальные временные каталоги не должны накапливаться в рабочем корне репозитория между сессиями;
- очистка выполнена без изменения исходников, спецификации языка, тестов и sample-файлов;
- `docs/PLANS.md` не менялся, потому что следующий шаг развития языка от этой уборки не изменился.

### Проверка
- после удаления выполнена ручная проверка корня репозитория;
- отдельно проверено, что под `src/` и `tests/` больше не осталось каталогов `bin/` и `obj/`.

### Что ещё не сделано
- следующий узкий post-v0.1.10 milestone развития языка по-прежнему не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.10 language scope, не расползаясь сразу в несколько языковых направлений.

## Session 38 — зафиксировано repo-level правило против накопления build-мусора

### Что сделано
- добавлен корневой `.gitignore` с правилами для generated build outputs, временных dotnet-каталогов, test/coverage outputs и локальных диагностических логов;
- добавлен корневой `Directory.Build.props`, который централизует локальные `bin/` и `obj/` в скрытый каталог `.artifacts/` в корне репозитория;
- `README.md` обновлён коротким разделом `Build hygiene`, чтобы правило было видно не только в конфигурации MSBuild;
- `docs/ARCHITECTURE.md` синхронизирован: build outputs теперь явно описаны как не-исходниковая часть рабочей копии.

### Что решено
- build-мусор больше не должен разрастаться по дереву `src/` и `tests/` после обычных `dotnet build`, `dotnet test` и `dotnet run`;
- допустимым местом для generated build outputs считается только централизованный скрытый каталог `.artifacts/`;
- локальные диагностические логи и временные dotnet-каталоги считаются временными и не входят в состав исходников проекта.

### Проверка
- выполнен `dotnet build C:\Axiom\Axiom.sln`;
- выполнен `dotnet test C:\Axiom\Axiom.sln`;
- после прогона проверено, что generated outputs создаются в `.artifacts/`, а каталоги `bin/` и `obj/` не возвращаются внутрь `src/` и `tests/`.

### Что ещё не сделано
- следующий узкий post-v0.1.10 milestone развития языка по-прежнему не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.10 language scope, не расползаясь сразу в несколько языковых направлений.

## Session 39 — завершены все основные формы `DO ... LOOP`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед новым language milestone в Resume mode;
- lexer расширен ключевым словом `UNTIL`;
- syntax/parser переведены с узкой формы `DoWhile` на общую модель `DO ... LOOP`:
  - `DO WHILE ... LOOP`
  - `DO UNTIL ... LOOP`
  - `DO ... LOOP WHILE ...`
  - `DO ... LOOP UNTIL ...`
  - `DO ... LOOP` без условия;
- semantics/binder переведены на общую bound-модель `DO ... LOOP` с флагами pre-check / post-check / until / infinite loop;
- runtime обновлён так, чтобы:
  - pre-check формы могли не входить в тело;
  - post-check формы всегда выполняли тело хотя бы один раз;
  - бесконечная форма завершалась через `EXIT DO` или `RETURN`;
  - `EXIT DO` по-прежнему выходил только из ближайшего `DO ... LOOP`;
- automated tests расширены в слоях `Lexing`, `Parsing`, `Semantics`, `Runtime`, `Execution`;
- acceptance layer расширен успешным прогоном новых sample-программ для `DO ... LOOP`;
- добавлены runnable `.axm` samples:
  - `samples/control-flow/do/do_until_loop.axm`
  - `samples/control-flow/do/loop_while.axm`
  - `samples/control-flow/do/loop_until.axm`
  - `samples/control-flow/do/do_loop_exit.axm`;
- `docs/SPECIFICATION.md` обновлён до v0.1.11;
- `docs/LANGUAGE_GUIDE.md` синхронизирован под полный baseline `DO ... LOOP`;
- `docs/PLANS.md` обновлён под новый закрытый milestone.

### Что решено
- семейство `DO ... LOOP` теперь закрыто как единый основной baseline, а не набор разрозненных частичных форм;
- форма без условия (`DO ... LOOP`) признана частью baseline и поддерживается официально;
- дальнейшие языковые шаги снова нужно выбирать как отдельный узкий milestone, не смешивая их с уже закрытым `DO ... LOOP`.

### Проверка
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- отдельный параллельный запуск нескольких `dotnet`-команд сначала дал file-lock в общей `.artifacts`, после чего прогоны были повторены последовательно и завершились успешно;
- после этого можно безопасно делать финальный full-solution прогон последовательно.

### Что ещё не сделано
- следующий узкий post-v0.1.11 milestone языка пока не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.11 language scope, не расползаясь сразу в несколько языковых направлений.

## Session 40 — синхронизирована IDE-подсветка для `UNTIL`

### Что сделано
- найден и исправлен пропуск в `AvalonEdit` highlighting definition: keyword `UNTIL` добавлен в embedded `Axiom.xshd`;
- IDE-side regression test обновлён так, чтобы ресурс подсветки явно требовал `UNTIL` рядом с `DO` и `LOOP`;
- `docs/PLANS.md` синхронизирован, чтобы закрытый milestone по `DO ... LOOP` учитывал и IDE highlighting follow-up.

### Что решено
- IDE больше не отстаёт от language baseline: `UNTIL` теперь подсвечивается так же, как остальные ключевые слова семейства `DO ... LOOP`.

### Проверка
- требуется targeted прогон `tests/Axiom.Ide.Tests`, чтобы подтвердить, что embedded highlighting resource загружается и содержит `UNTIL`.

### Что ещё не сделано
- следующий узкий post-v0.1.11 milestone языка по-прежнему не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.11 language scope, не расползаясь сразу в несколько языковых направлениях.

## Session 41 — реализован baseline `SELECT CASE`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и документы из `docs/` перед новым language milestone в Resume mode;
- lexer расширен ключевыми словами `SELECT`, `CASE` и `IS`;
- syntax/parser расширены под полноценный QuickBASIC-подобный блок `SELECT CASE ... END SELECT`;
- поддержаны формы:
  - `CASE value`
  - `CASE value1, value2, value3`
  - `CASE low TO high`
  - `CASE IS <op> expression`
  - смешанные selector lists
  - `CASE ELSE`;
- binder/semantics получили отдельную bound-модель `SELECT CASE` с вычислением test expression ровно один раз;
- зафиксировано текущее правило типов:
  - точные совпадения и `CASE IS =` / `CASE IS <>` работают для чисел, строк и `BOOLEAN` при совместимых типах;
  - диапазоны `CASE ... TO` и упорядоченные `CASE IS <`, `<=`, `>`, `>=` допустимы только для чисел;
  - для нечислового диапазона добавлена отдельная semantic diagnostic `MB2015`;
- runtime обновлён так, чтобы ветки проверялись сверху вниз и выполнялась только первая подошедшая ветка без fall-through;
- IDE syntax highlighting синхронизирован под `SELECT`, `CASE` и `IS`;
- automated tests обновлены в слоях `Lexing`, `Parsing`, `Semantics`, `Runtime`, `Execution`, `Ide`;
- добавлены runnable `.axm` samples:
  - `samples/control-flow/select_case/exact_values.axm`
  - `samples/control-flow/select_case/ranges_and_is.axm`
  - `samples/control-flow/select_case/string_values.axm`
  - `samples/control-flow/select_case/mixed_selectors.axm`;
- `docs/SPECIFICATION.md` обновлён до v0.1.12;
- `docs/LANGUAGE_GUIDE.md` синхронизирован под baseline `SELECT CASE`;
- `docs/PLANS.md` обновлён под новый закрытый milestone.

### Что решено
- `SELECT CASE` официально входит в текущий baseline языка, а не остаётся отложенной QuickBASIC-совместимой идеей;
- реализация сознательно не расширяет общую модель сравнений за пределы уже существующих правил языка: порядковые сравнения строк и `BOOLEAN` в `SELECT CASE` не вводились;
- acceptance samples для `SELECT CASE` вынесены в новую тематическую папку `samples/control-flow/select_case/`.

### Проверка
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj`
  - `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
  - `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`
- перед тестами отдельно подтверждено, что `dotnet build Axiom.sln` завершается успешно.

### Что ещё не сделано
- следующий узкий post-v0.1.12 milestone языка пока не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.12 language scope, не расползаясь сразу в несколько языковых направлениях.

## Session 42 — расширены строковые сравнения и string ranges в `SELECT CASE`

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md` и актуальные документы из `docs/` перед следующим language mini-step в Resume mode;
- общая модель сравнений языка расширена так, что `STRING` теперь поддерживает `=`, `<>`, `<`, `<=`, `>`, `>=` не только в `SELECT CASE`, но и в обычных выражениях `IF`, `PRINT` и других местах;
- binder обновлён так, чтобы ordered comparisons для строк были допустимы, а `BOOLEAN` по-прежнему принимал только `=` и `<>`;
- runtime `Compare(...)` расширен на строки через `StringComparison.Ordinal`;
- `SELECT CASE` автоматически расширен на:
  - string ranges `CASE "A" TO "M"`
  - string ordered comparisons `CASE IS > "M"` и аналогичные формы;
- semantic diagnostic `MB2015` сохранён, но его сообщение теперь относится к недопустимому типу диапазона в целом, а не только к нечисловому значению;
- automated tests обновлены в `Semantics`, `Runtime`, `Execution`;
- добавлены runnable `.axm` samples:
  - `samples/expressions/comparisons/string_order.axm`
  - `samples/control-flow/select_case/string_ranges.axm`;
- `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы под новое правило string comparisons;
- `docs/PLANS.md` обновлён под новый закрытый milestone.

### Что решено
- для строк выбран ordinal-порядок сравнения, а не locale-aware и не "словарный" порядок;
- `BOOLEAN` сознательно не получил искусственный порядок `FALSE < TRUE` и остался только с `=` / `<>`;
- расширение выполнено как общее правило языка, а не как специальный локальный хак только для `SELECT CASE`.

### Проверка
- успешно выполнены targeted test runs:
  - `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
  - `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
  - `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- после этого успешно выполнен полный прогон:
  - `dotnet test Axiom.sln`

### Что ещё не сделано
- следующий узкий post-v0.1.12 milestone языка пока не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.12 language scope, не расползаясь сразу в несколько языковых направлениях.

## Session 43 — добавлено оглавление в `LANGUAGE_GUIDE.md`

### Что сделано
- в начало `docs/LANGUAGE_GUIDE.md` добавлено кликабельное оглавление по основным главам;
- в оглавлении перечислены только те темы, которые уже реализованы в текущем состоянии Axiom;
- структура самих учебных глав не менялась: шаг был чисто навигационным и документационным.

### Что решено
- `LANGUAGE_GUIDE.md` теперь можно использовать как быстрый обзор уже закрытых language milestones без долгой прокрутки;
- оглавление размещено в начале документа сразу после вводного блока, чтобы оно было видно при открытии файла.

### Проверка
- выполнена ручная проверка структуры `docs/LANGUAGE_GUIDE.md` после правки;
- автоматические тесты не запускались, потому что change затрагивает только Markdown-документацию.

### Что ещё не сделано
- следующий узкий post-v0.1.12 milestone языка по-прежнему не выбран.

### Следующий точный шаг
Отдельно выбрать и зафиксировать следующий маленький post-v0.1.12 language scope, не расползаясь сразу в несколько языковых направлениях.

## Session — IDE Redesign v0.2 Phase 2-4: shell architecture, document tabs, new layout

### Что сделано
- **Phase 2: Shell architecture refactor**:
  - создан `Documents/DocumentTabViewModel.cs` — модель одной вкладки (FilePath, DisplayName, SourceText, IsDirty, IsUntitled);
  - создан `Documents/DocumentWorkspaceService.cs` — сервис управления вкладками (CreateNew, Open, Save, SaveAs, SaveAll, Close с save/discard/cancel prompt, dedupe по пути, last-tab fallback);
  - создан `Status/StatusBarViewModel.cs` — статус бар (line/column, path, dirty, run state);
  - создан `Output/BottomPaneViewModel.cs` — нижняя панель с Output/Diagnostics tabs (SelectedTabIndex, OutputText, Diagnostics collection, auto-select methods);
  - создан `Files/FilesPaneViewModel.cs` — файловая панель (RootPath, Nodes, IsVisible);
  - создан `Services/Prompts/DialogPromptService.cs` — WPF MessageBox для save/discard/cancel;
  - переработан `ViewModels/MainWindowViewModel.cs` — теперь координирует child viewmodels вместо монолитного состояния;
  - удалён legacy `MainWindowViewModel.Legacy.cs`;
  - обновлён `App.xaml.cs` с созданием нового ViewModel и prompt service;
  - обновлены тесты `MainWindowViewModelTests.cs` под новый API;
- **Phase 3: Document tabs workflow**:
  - добавлены команды `SaveAllCommand` и `CloseTabCommand`;
  - `ExitApplication` теперь обрабатывает dirty documents через prompt;
  - DocumentWorkspaceService поддерживает async close с save/discard/cancel;
  - last-tab fallback: при закрытии последней вкладки создаётся новая пустая;
- **Phase 4: New shell layout**:
  - создан `Resources/ShellColors.xaml` — light theme colors, brushes, layout constants;
  - создан `Resources/ShellStyles.xaml` — стили для menu, toolbar, buttons, tabs, status bar, output, diagnostics;
  - обновлён `App.xaml` — объединение resource dictionaries;
  - полностью перестроен `MainWindow.xaml`:
    - Menu Bar: File (New, Open, Save, Save As, Save All, Close Tab, Exit), Edit (Undo, Redo, Cut, Copy, Paste, Delete, Select All), Search (Find, Find Next, Find Previous), Run (Run), View (Files Pane, Output, Diagnostics, Status Bar), Help (About);
    - ToolBar: New, Open, Save, Run, Find;
    - Files Pane (left, 240px, GridSplitter, hideable);
    - Document Tabs (center, с IsDirty indicator);
    - Bottom Tabbed Pane (Output/Diagnostics, 220px, GridSplitter);
    - Status Bar (StatusText, LineColumn, Path);
    - INPUT inline в Output transcript;
  - обновлён `MainWindow.xaml.cs` — убраны ссылки на старые control names;
- **Документация**:
  - созданы `docs/IDE_REDESIGN.md`, обновлены `ARCHITECTURE.md`, `PLANS.md`, `WORKLOG.md`;

### Touched paths
- Созданы: `Documents/DocumentTabViewModel.cs`, `Documents/DocumentWorkspaceService.cs`, `Status/StatusBarViewModel.cs`, `Output/BottomPaneViewModel.cs`, `Files/FilesPaneViewModel.cs`, `Services/Prompts/DialogPromptService.cs`, `Resources/ShellColors.xaml`, `Resources/ShellStyles.xaml`, `docs/IDE_REDESIGN.md`
- Обновлены: `ViewModels/MainWindowViewModel.cs`, `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `tests/MainWindowViewModelTests.cs`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`
- Удалены: `ViewModels/MainWindowViewModel.Legacy.cs`

### Проверка
- `dotnet build Axiom.sln` — успешно, 0 errors;
- `dotnet test Axiom.sln` — все 230 тестов прошли (12 lexer + 35 parser + 42 semantics + 57 runtime + 52 execution + 32 IDE).

### Что ещё не сделано
- editor command surface (context menu, AvalonEdit SearchPanel) не подключена;
- files pane не имеет дерева файлов (только placeholder);
- recent files не перенесены в File > Open Recent menu;
- run auto-switching и blocking rules не реализованы;
- tests для document tab workflow, files pane, run UX нужно расширить.

### Следующий точный шаг
Phase 5: подключить editor-command surface — AvalonEdit SearchPanel (Ctrl+F), context menu с Edit commands, standard shortcuts.

---

## Session — IDE Redesign v0.2 kickoff (documentation phase)

### Что сделано
- прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`;
- получен план IDE redesign из `ПЛАН IDE.txt`;
- создан `docs/IDE_REDESIGN.md` — главный источник истины для всех IDE-redesign сессий:
  - зафиксированы locked decisions (Thonny UX reference, tabs, bottom pane, inline INPUT, recent in menu, files pane, no Variables/Assistant, light theme);
  - зафиксированы внешние референсы (Thonny, AvalonEdit, Notepad++);
  - описана целевая компоновка окна с ASCII-диаграммой;
  - описана menu map, command map, hotkeys;
  - описаны pane models (document tabs, files pane, bottom pane);
  - описана shell architecture (child viewmodels + focused services);
  - описан visual system (focused resource dictionaries);
  - зафиксирован milestone roadmap из 9 фаз;
  - зафиксирован agent protocol (что читать, что обновлять, какие тесты гонять, что не трогать);
  - зафиксированы критерии ручной приёмки;
- обновлён `docs/ARCHITECTURE.md`:
  - раздел 4.8 заменён с "minimal shell without tabs" на новый Thonny-style shell baseline;
  - добавлена композиция child viewmodels и focused services;
  - добавлены документ workflow, files pane, run/output/diagnostics behavior;
  - обновлён раздел 5 (границы запуска) с упоминанием shell-архитектуры;
  - обновлён раздел 10 (открытые вопросы) — закрыты вопросы про AvalonEdit и INPUT UX;
- обновлён `docs/PLANS.md`:
  - текущий milestone заменён на IDE Redesign v0.2;
  - добавлены 9 фаз с чекбоксами (Phase 1 documentation отмечен как выполненный);
  - расширены deferred items (session restore, Variables/Assistant, themes);
- обновлён `docs/WORKLOG.md` (этот файл).

### Что решено
- `docs/IDE_REDESIGN.md` становится главным документом для всех последующих IDE-сессий;
- языковые слои (Execution, Runtime, Semantics, Parsing, Lexing, Core) не меняются в этом milestone;
- единственный допустимый interaction с execution остаётся: `Axiom.Ide -> ProgramRunService -> AxiomExecutionPipeline`;
- следующий шаг — Phase 2: shell architecture refactor (разбиение на child viewmodels и focused services).

### Touched paths
- `docs/IDE_REDESIGN.md` — создан
- `docs/ARCHITECTURE.md` — обновлён раздел 4.8, 5, 10
- `docs/PLANS.md` — обновлён milestone и phases
- `docs/WORKLOG.md` — обновлён

### Проверка
- это документационная сессия без кодовых изменений;
- build и тесты не запускались.

### Следующий точный шаг
Phase 2: разбить текущий IDE state на child viewmodels и focused services, не меняя внешний вид радикально. Ввести `DocumentTabViewModel`, `FilesPaneViewModel`, `BottomPaneViewModel`, `StatusBarViewModel`, document workspace service, files pane service, prompt service.

## Session — IDE Redesign v0.2 plan sync after partial handoff

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и исходный `ПЛАН IDE.txt`;
- сверено фактическое состояние `Axiom.Ide` с документацией IDE redesign;
- подтверждено, что репозиторий уже закрыл phases 1-4 redesign, но `docs/PLANS.md` всё ещё описывал их как будущие шаги;
- найдено частичное расхождение между планом и кодом:
  - в коде уже есть заготовки под `SearchPanel`, новый shell и auto-switch `Output`/`Diagnostics`;
  - при этом `Find...`, editor context menu, `Open Recent`, реальное дерево files pane и blocking rules ещё не доведены;
- обновлён `docs/IDE_REDESIGN.md`:
  - добавлен текущий implementation checkpoint по фазам 1-8;
  - явно зафиксировано, что следующий узкий шаг — полное закрытие Phase 5;
- обновлён `docs/PLANS.md`:
  - раздел "Ближайшие задачи по порядку" синхронизирован под активный IDE milestone вместо старого post-v0.1.12 language follow-up;
  - phases 2-4 отмечены как выполненные;
  - phases 5-9 переведены в актуальные статусы `active / partial / pending`.

### Что решено
- текущий источник истины для продолжения работы по IDE — не `ПЛАН IDE.txt` сам по себе, а синхронизированная связка `docs/IDE_REDESIGN.md` + `docs/PLANS.md`;
- следующий агент не должен заново "планировать redesign с нуля": план уже зафиксирован, нужно брать узкий implementation slice `Phase 5`;
- частично начатые элементы фаз 7 и 8 не считаются закрытыми и не должны маскировать незавершённость milestone.

### Touched paths
- `docs/IDE_REDESIGN.md` — добавлен текущий checkpoint по статусу фаз
- `docs/PLANS.md` — обновлены ближайшие задачи и статусы фаз IDE redesign
- `docs/WORKLOG.md` — зафиксирована эта синхронизация

### Проверка
- это документационная сессия без кодовых изменений;
- build и тесты не запускались.

### Следующий точный шаг
Phase 5: довести editor-command surface до рабочего состояния — связать `Find...` с активным `AvalonEdit SearchPanel`, добавить editor context menu и убрать недоведённые command/search заглушки, не переходя пока к `Open Recent` и files tree.

## Session — IDE Redesign v0.2 Phase 5: editor commands, search wiring, Save As

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- закрыт сломанный кусок IDE search integration:
  - убран невалидный вызов `SearchPanel.Detach`;
  - `SearchPanelBehavior` переведён на корректную модель `Install` + reuse одного `SearchPanel` на editor instance;
  - добавлены helper-методы для `OpenSearchPanel`, `FindNext`, `FindPrevious`;
- обновлён `MainWindow.xaml`:
  - добавлены window-level `CommandBindings` для `ApplicationCommands.Find`, `SearchCommands.FindNext`, `SearchCommands.FindPrevious`;
  - добавлены `InputBindings` для `Ctrl+N`, `Ctrl+O`, `Ctrl+S`, `Ctrl+Shift+S`, `Ctrl+W`, `F5`, `Ctrl+F`, `F3`, `Shift+F3`;
  - `Search` menu теперь привязан к реальным командам поиска;
  - editor `AvalonEdit` получил attached behavior `InstallSearchPanel="True"`;
  - добавлено editor context menu с `Undo/Redo/Cut/Copy/Paste/Delete/Select All/Find/Find Next/Find Previous`;
  - input textbox в `Output` получил имя `ConsoleInputTextBox` для корректного focus path;
- обновлён `MainWindow.xaml.cs`:
  - code-behind упрощён до thin shell-level routing;
  - поиск теперь идёт через active editor, найденный в visual tree текущей вкладки;
  - focus на active input line внутри `Output` больше не опирается на `FindName` с несуществующим именем;
- обновлён `MainWindowViewModel.cs`:
  - введён отдельный `SaveAsCommand`;
  - `Save As...` больше не указывает ошибочно на обычный `Save`;
- обновлены tests:
  - `MainWindowViewModelTests` получил проверку `SaveAsCommand`;
  - `EditorFeatureTests` получил regression test на XAML wiring поиска и editor context menu;
- обновлены `docs/IDE_REDESIGN.md` и `docs/PLANS.md`:
  - `Phase 5` отмечена как выполненная;
  - следующий узкий шаг переключён на `Phase 6: Recent files migration`.

### Что решено
- `Phase 5` больше не считается частично начатой: editor search and command surface доведены до рабочего baseline;
- `Save As` больше не прячется за `Save` и не остаётся документным долгом из Phase 3;
- следующий implementation slice нужно брать по recent-files menu, а не возвращаться к `SearchPanel`.

### Touched paths
- `src/Axiom.Ide/Editor/SearchPanelBehavior.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet test Axiom.sln` — успешно

### Следующий точный шаг
Phase 6: реализовать рабочий `File > Open Recent` поверх уже существующего `RecentFilesService`, без возврата к отдельной sidebar-панели recent files.

## Session — IDE Redesign v0.2 Phase 6: recent files migration

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- закрыт `Phase 6` по recent files без возврата к отдельной sidebar-панели:
  - `RelayCommand` расширен до параметризуемой формы, чтобы shell мог открывать конкретный recent entry из menu item;
  - `DocumentWorkspaceService` получил отдельный `OpenDocumentFromPath(...)` для прямого открытия файла по известному пути с reuse существующей dedupe/open логики;
  - `MainWindowViewModel` теперь проецирует persistence snapshot в `ObservableCollection<RecentFileItem>`, держит `HasRecentFiles`, обновляет recent menu после open/save/save as/save all и обрабатывает `OpenRecentCommand`;
  - при открытии несуществующего recent file запись удаляется через `RecentFilesService.RemoveFile(...)`, а меню пересобирается из обновлённого snapshot;
  - `MainWindow.xaml` переведён с disabled placeholder на рабочий `File > Open Recent`, привязанный к `RecentFiles` и `OpenRecentCommand`;
- добавлены regression tests:
  - `MainWindowViewModelTests` покрывает projection recent snapshot, успешное открытие recent file и cleanup missing recent entry;
  - `EditorFeatureTests` проверяет XAML wiring для `Open Recent` menu;
- обновлены `docs/IDE_REDESIGN.md` и `docs/PLANS.md`: `Phase 6` отмечена как выполненная, следующий узкий шаг переключён на `Phase 7: Files pane`.

### Что решено
- recent files окончательно закреплены как menu-driven UX через `File > Open Recent`, а не как отдельная постоянная панель;
- persistence boundary не менялась: `RecentFilesService` остаётся единственным слоем хранения snapshot и last-used directory;
- следующий implementation slice нужно брать уже по files tree, а не возвращаться к recent-files migration.

### Touched paths
- `src/Axiom.Ide/Commands/RelayCommand.cs`
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet test Axiom.sln` — успешно

### Следующий точный шаг
Phase 7: довести левую `Files` pane до рабочего current-folder tree поверх уже существующего `FilesPaneViewModel`, добавить double-click open и refresh, не вводя project/workspace model.

## Session — IDE Redesign v0.2 Phase 7: files pane tree

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- закрыт `Phase 7` по files pane:
  - в `Files/` добавлен отдельный `FileTreeService`, который строит current-folder tree из `RootPath`, сортирует узлы как directories first / files second и безопасно переживает недоступные каталоги;
  - `FilesPaneViewModel` получил `ReplaceNodes(...)`, чтобы shell мог атомарно обновлять дерево при смене root и при ручном refresh;
  - `MainWindowViewModel` теперь держит files-pane service, загружает дерево при старте и после смены active document, а также публикует `RefreshFilesPaneCommand` и `OpenFilesPaneNodeCommand`;
  - открытие файла из левой панели идёт через уже существующий `DocumentWorkspaceService.OpenDocumentFromPath(...)`, поэтому dedupe/open flow и recent-files persistence не дублируются;
  - `MainWindow.xaml` переведён с placeholder `TreeView` на реальный `HierarchicalDataTemplate`, показывает текущий `RootPath`, даёт refresh-кнопку и открывает file-node по double-click;
- добавлены tests:
  - `FileTreeServiceTests` покрывает recursive tree build и порядок directories before files;
  - `MainWindowViewModelTests` покрывает initial files-pane load, refresh и open-from-tree flow;
  - `EditorFeatureTests` проверяет XAML wiring для files pane refresh/double-click tree.
- обновлены `docs/IDE_REDESIGN.md` и `docs/PLANS.md`: `Phase 7` отмечена как выполненная, следующий узкий шаг переключён на `Phase 8: Run/output/diagnostics UX`.

### Что решено
- files pane больше не является декоративным placeholder: это реальная shell-поверхность для навигации по текущей рабочей папке;
- filesystem-логика остаётся вынесенной из root viewmodel в focused service, как и было зафиксировано в архитектурных документах;
- следующий slice нужно брать уже по run/output/input UX и блокировкам, а не возвращаться к files tree.

### Touched paths
- `src/Axiom.Ide/Files/FileTreeService.cs`
- `src/Axiom.Ide/Files/FilesPaneViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/FileTreeServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet test Axiom.sln` — успешно

### Следующий точный шаг
Phase 8: довести `Run`/`Output`/`Diagnostics` UX — корректный focus на активную input line и блокировки повторного `Run` и destructive document actions во время активного run или ожидания `INPUT`.

## Session — IDE Redesign v0.2 Phase 8: run/output/input shell locks

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- закрыт `Phase 8` по shell-level run/input UX:
  - `MainWindowViewModel` теперь при переходе `ProgramConsoleService` в `IsWaitingForInput = true` принудительно возвращает нижнюю панель на вкладку `Output`, чтобы активная input line не пряталась за `Diagnostics`;
  - `RunCommand` и document-navigation команды (`New`, `Open`, `Open Recent`, open-from-files-pane, `Close Tab`, `Exit`) переведены на `CanExecute`, зависящий от shell lock state (`IsRunning || IsWaitingForInput`);
  - во время активного run/`INPUT` shell больше не допускает повторный `Run` и смену document context, а после завершения run команды автоматически разблокируются;
  - существующий code-behind focus path для `ConsoleInputTextBox` сохранён и теперь работает поверх более строгого shell state;
- добавлены tests:
  - `MainWindowViewModelTests` покрывает временную блокировку run/document-navigation команд во время исполнения;
  - `MainWindowViewModelTests` покрывает возврат на `Output` и доступность `SubmitInputCommand` при ожидании `INPUT`;
- обновлены `docs/IDE_REDESIGN.md` и `docs/PLANS.md`: `Phase 8` отмечена как выполненная, следующий точный шаг переключён на `Phase 9: Tests & smoke pass`.

### Что решено
- run/input UX перестал быть только визуальным соглашением: теперь это явное shell-level состояние, влияющее на доступность команд;
- `Output` остаётся главным рабочим контекстом для `INPUT`, а не просто табом, выбранным один раз в начале запуска;
- дальше нужно не расширять scope redesign, а добрать финальную регрессию и ручной smoke pass.

### Touched paths
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet test Axiom.sln` — успешно

### Следующий точный шаг
Phase 9: добрать финальные regression checks вокруг document-tab workflow и выполнить ручной shell smoke pass по redesign baseline.

## Session — startup fix for `dotnet run` in Axiom.Ide

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- исправлен startup crash, из-за которого `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj` выглядел как "ничего не происходит":
  - из `App.xaml` убран `StartupUri="MainWindow.xaml"`, потому что окно уже создаётся вручную в `App.OnStartup(...)`;
  - в `MainWindow.GetActiveEditor()` добавлена защита на раннюю фазу инициализации, когда `DocumentTabs` ещё не готов и `Find` command binding уже может спрашивать `CanExecute`;
- добавлены regression checks в `EditorFeatureTests`:
  - `App.xaml` больше не должен содержать `StartupUri`;
  - `MainWindow.xaml.cs` должен явно guard-ить `Find` routing до готовности `DocumentTabs`.

### Что решено
- причина silent-fail была не в `dotnet run`, а в раннем `NullReferenceException` внутри `MainWindow.xaml.cs` плюс конфликтующем двойном startup path;
- `Axiom.Ide` снова можно запускать напрямую через `dotnet run`, не дожидаясь отдельной publish/package-обвязки.

### Touched paths
- `src/Axiom.Ide/App.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` — процесс живёт после старта, окно не падает сразу

### Следующий точный шаг
Вернуться к `Phase 9: Tests & smoke pass` и добрать оставшийся ручной shell smoke pass по redesign baseline.

## Session — startup stabilization for `dotnet run` in Axiom.Ide

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/IDE_REDESIGN.md`;
- добит реальный startup-path `Axiom.Ide` до рабочего состояния:
  - из `App.xaml` уже был убран конфликтующий `StartupUri`, оставлено одно место создания окна — `App.OnStartup(...)`;
  - `MainWindow.xaml.cs` переведён на безопасную инициализацию shell shortcuts через code-behind, чтобы убрать XAML-cycle из `Window.InputBindings`;
  - `Open Recent` и files-pane double-click bindings переведены с `x:Reference RootWindow` на `RelativeSource`, чтобы окно вообще могло загрузиться без `XamlParseException`;
  - `DocumentWorkspaceService.ActiveDocument` сделан bindable-свойством с setter, чтобы `TabControl.SelectedItem="{Binding Documents.ActiveDocument}"` не падал на readonly binding и чтобы UI-переключение вкладок корректно отражалось в shell state;
- startup regression checks дополнены в `EditorFeatureTests`.

### Что решено
- команда `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` больше не умирает на старте;
- предыдущая проблема оказалась не одной, а цепочкой startup-bugs: ранний `Find` crash, затем XAML binding cycle, затем readonly `SelectedItem` binding.

### Touched paths
- `src/Axiom.Ide/App.xaml`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` — процесс остаётся живым после старта и получает `MainWindowHandle`, окно не падает сразу

### Следующий точный шаг
Вернуться к `Phase 9: Tests & smoke pass` и добрать оставшийся ручной shell smoke pass по redesign baseline.

## Session — shell layout fix after live UI review

### Что сделано
- после ручного запуска `Axiom.Ide` и живого просмотра окна исправлена критическая layout-regression в `MainWindow.xaml`;
- причина была в `DockPanel`: `StatusBar` стоял последним ребёнком и поэтому, по правилу `LastChildFill`, занимал всё оставшееся пространство окна вместо нижней полосы;
- `StatusBar` перенесён выше main content внутри `DockPanel`, а `DockPanel.LastChildFill="True"` оставлен явно, чтобы заполняющим элементом был именно центральный `Grid` с editor/files/output;
- добавлен regression test в `EditorFeatureTests`, который проверяет, что `StatusBar` остаётся docked снизу, а main grid идёт после него как fill-child.

### Что решено
- editor больше не должен быть зажат узкой колонкой слева из-за растянутого `StatusBar`;
- строка состояния (`status`, `Ln/Col`, `path`) больше не должна визуально “уезжать вправо” и ломать всю геометрию окна;
- старт и layout теперь одновременно стабильны: окно открывается и не разваливает основную компоновку на первом же показе.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` — окно стартует, процесс остаётся живым после показа

### Следующий точный шаг
Если базовая геометрия окна теперь нормальная, следующий шаг — отдельный thonny-like visual polish pass: меню/toolbar spacing, tab visuals, files pane density и более аккуратная shell typography.

## Session — quick access icon toolbar under menu

### Что сделано
- после живого сравнения с Thonny добавлен отдельный quick access toolbar во втором ряду под верхним меню;
- `MainWindow.xaml` переведён с текстовых toolbar-кнопок на компактный icon row:
  - `New`
  - `Open`
  - `Save`
  - `Run`
  - `Find`
- toolbar собран как простая shell-level горизонтальная панель, а не как тяжёлый WPF `ToolBarTray`, чтобы сохранить более плотную и предсказуемую геометрию, ближе к Thonny-style second row;
- в `ShellStyles.xaml` добавлены отдельные стили `QuickAccessButtonStyle` и `QuickAccessGlyphStyle` для компактных icon-кнопок с hover/pressed состояниями;
- `EditorFeatureTests` получил regression test на наличие quick access icon toolbar.

### Что решено
- второй ряд под меню теперь визуально работает как панель быстрого доступа, а не как набор широких текстовых кнопок;
- toolbar стал ближе к Thonny по плотности и общей роли: компактные пиктограммы для наиболее частых действий, без лишней перегруженности.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно
- `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` — окно стартует, процесс остаётся живым после показа

### Следующий точный шаг
Если продолжаем thonny-like visual pass, следующий логичный кусок — привести menu/toolbar spacing, tab visuals и files pane density к более цельному beginner-IDE виду.

## Session — editor context menu icons

### Что сделано
- editor context menu в `MainWindow.xaml` доведён до того же визуального языка, что и верхние dropdown menu;
- для команд `Undo`, `Redo`, `Cut`, `Copy`, `Paste`, `Delete`, `Select All`, `Find`, `Find Next`, `Find Previous` добавлены `MenuItem.Icon` с теми же Segoe MDL2 glyph-иконками, что уже используются в основном меню;
- context menu переведён на `DropDownMenuItemStyle`, чтобы правое меню не отличалось по плотности и выравниванию от остальных shell-меню;
- `EditorFeatureTests` дополнен regression test на наличие иконок и styled menu items в editor context menu.

### Что решено
- меню по правой кнопке больше не выглядит как визуально отдельный, недоделанный слой shell;
- iconography редактора теперь согласована между main menu и context menu, без ввода нового набора символов или отдельного стиля только для right-click меню.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- автоматическую build/test проверку в этой сессии стоит делать после закрытия запущенного `Axiom.Ide`, чтобы не упереться в lock на `.artifacts`;
- логически безопасный следующий прогон: `dotnet build Axiom.sln -c Release` и `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -c Release`.

### Следующий точный шаг
Плановый следующий шаг не меняется: закрыть `Phase 9: Tests & smoke pass`, а по UI — продолжать только узкие polish/fix slices без расползания redesign обратно в language/runtime scope.

## Session — tab close buttons and unsaved-close prompt

### Что сделано
- в `Axiom.Ide` добавлен крестик закрытия (`X`) в заголовок каждой document tab;
- `CloseTabCommand` расширен так, чтобы закрывать не только active tab, но и конкретный документ, переданный из tab header;
- `DocumentTabViewModel` теперь сам отслеживает dirty-state относительно последнего сохранённого текста, поэтому prompt на закрытие срабатывает от реального редактирования, а не только от ручной установки `IsDirty`;
- `MainWindowViewModel` подписан на изменения active document и его свойств, чтобы title/files-pane корректно реагировали на dirty/file-path changes;
- IDE tests расширены сценариями:
  - автоматическое выставление/снятие dirty-state;
  - закрытие конкретной clean tab по параметру;
  - cancel при закрытии dirty tab;
  - save-before-close для dirty tab;
- XAML regression test дополнен проверкой привязки tab close button к `CloseTabCommand`.

### Что решено
- поведение вкладок стало ближе к Thonny-style shell: пользователь может закрывать документ прямо из заголовка вкладки, а не только через меню/shortcut;
- unsaved-close flow теперь реально защищает изменения, потому что dirty-state поднимается при редактировании текста и сбрасывается только после успешного save.

### Touched paths
- `src/Axiom.Ide/Documents/DocumentTabViewModel.cs`
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `57/57` tests passed
- `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj` — окно стартует, получен `MainWindowHandle`, процесс затем остановлен

### Следующий точный шаг
Вернуться к незакрытому пункту `Phase 9` и выполнить ручной shell smoke pass уже с новым tab-close workflow: проверить крестик на clean/dirty tabs в живом `Axiom.Ide`, включая `Save`, `Discard` и `Cancel`.

## Session — files pane toggle collapses shell layout

### Что сделано
- в `MainWindow.xaml` скорректирован shell layout для `View -> Files Pane`;
- вертикальный splitter слева теперь скрывается вместе с files pane;
- `DocumentTabs` получили layout-trigger: когда `FilesPane.IsVisible = false`, редактор растягивается на все колонки верхней области и занимает место скрытой левой панели;
- дополнительно убран конфликт приоритета WPF: default-position `DocumentTabs` перенесён из локального `Grid.Column="2"` в style setters, чтобы `DataTrigger` реально мог перевести редактор в колонку `0`;
- `EditorFeatureTests` дополнен regression test на эту XAML-конфигурацию.

### Что решено
- снятая галочка `View -> Files Pane` больше не оставляет пустую зону слева;
- поведение стало ближе к Thonny: при скрытии файловой панели редактор действительно расширяется на всю доступную ширину.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `58/58` tests passed
- `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj` — окно стартует, получен `MainWindowHandle`, процесс затем остановлен

### Следующий точный шаг
Остаётся ручной shell smoke pass для `Phase 9`: живьём проверить переключение `View -> Files Pane`, работу крестика закрытия tab и диалог `Save / Discard / Cancel` в запущенном `Axiom.Ide`.

## Session — bottom pane focus and live caret status

### Что сделано
- в `MainWindowViewModel` доведено поведение нижней панели до более thonny-like baseline:
  - при любом неуспешном `Run` shell теперь не только переключается на `Diagnostics`, но и явно запрашивает focus этой панели;
  - в `View`-меню пункты `Output` и `Diagnostics` перестали быть заглушками и теперь реально активируют соответствующую вкладку нижнего pane;
- в `MainWindow.xaml.cs` сохранена thin UI-роль code-behind, но добита синхронизация shell-level состояния:
  - caret активного `AvalonEdit` отслеживается через `PositionChanged`, `SelectionChanged` tab control и focus editor;
  - `StatusBar.LineColumnText` теперь обновляется по реальному положению курсора активного документа;
  - focus requests для `Output`, `Diagnostics` и встроенной input line обрабатываются централизованно;
- IDE tests расширены:
  - исправлен regression test для run-status enum;
  - добавлены проверки на `ShowOutputCommand` / `ShowDiagnosticsCommand`;
  - XAML regression test теперь требует wiring `View -> Output` / `View -> Diagnostics`.

### Что решено
- нижняя панель стала не просто отображением состояния, а полноценной shell-областью: её можно переключать вручную из меню, а ошибки теперь сами выводят пользователя в `Diagnostics`;
- строка `Ln / Col` в статус-баре больше не должна оставаться статичной на `Ln 1, Col 1`, когда caret уже перемещён по коду.

### Touched paths
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `63/63` tests passed
- `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj` — окно стартует, получен `MainWindowHandle`, процесс затем остановлен

### Следующий точный шаг
Остаётся живой ручной shell smoke pass для `Phase 9`: отдельно прокликать в запущенном `Axiom.Ide` сценарии `View -> Output / Diagnostics`, автофокус diagnostics при ошибочном `Run` и обновление `Ln / Col` при перемещении caret мышью и клавиатурой.

## Session — diagnostics click navigation in editor

### Что сделано
- в `Axiom.Ide` доведён сценарий работы diagnostics pane:
  - `DiagnosticsDataGrid` теперь слушает выбор строки;
  - при выборе diagnostics entry shell берёт `Line / Column`, находит target через существующий `DiagnosticNavigationTargetFactory` и переводит active editor к этой позиции;
- `AvalonEditDiagnosticNavigation` расширен публичным helper-методом, чтобы одна и та же navigation-логика использовалась и для binding-based, и для code-behind сценария;
- в `MainWindow.xaml.cs` для editor включён `HighlightCurrentLine`, а current line получил мягкую подсветку и рамку через shell resources;
- в `ShellColors.xaml` добавлены отдельные ресурсы для визуального выделения строки, на которую IDE переводит caret после выбора diagnostics;
- regression tests обновлены под новый wiring diagnostics selection и current-line highlight.

### Что решено
- клик по ошибке в нижней панели больше не требует ручного поиска строки глазами: editor сам прокручивается к нужной позиции, ставит caret в правильный `Line / Column` и делает текущую строку заметной;
- navigation-логика осталась централизованной, а не расползлась по нескольким случайным кускам WPF-кода.

### Touched paths
- `src/Axiom.Ide/Editor/Navigation/AvalonEditDiagnosticNavigation.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `64/64` tests passed
- `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj` — окно стартует, получен `MainWindowHandle`, процесс затем остановлен
- живой клик по diagnostics entry в запущенном окне ещё не прогонялся вручную в этой сессии

### Следующий точный шаг
После сборки и тестов сделать короткий live smoke pass: открыть файл с синтаксическими ошибками, запустить `Run`, кликнуть по разным diagnostics entries и проверить переход caret/scroll/current-line highlight в editor.

## Session — diagnostic line highlight no longer leaks to line 1

### Что сделано
- в `MainWindow.xaml.cs` убран источник ложной подсветки первой строки:
  - editor больше не включает `HighlightCurrentLine` автоматически при загрузке;
  - current-line highlight теперь включается только после успешного перехода по выбранной diagnostics-строке;
  - если `SelectedDiagnostic` сбрасывается в `null`, активный editor очищает diagnostic-line highlight;
- regression test в `EditorFeatureTests` обновлён так, чтобы требовать именно отложенное включение подсветки, а не постоянный current-line highlight с момента загрузки editor.

### Что решено
- в пустом документе и в обычном файле без выбранной diagnostics больше не должна появляться ложная подсветка первой строки;
- подсветка строки остаётся только как часть сценария “кликнули по ошибке -> перешли к ошибочной строке”.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `64/64` tests passed

### Следующий точный шаг
Сделать короткий живой smoke pass в `Axiom.Ide`: проверить, что в пустом документе полоска исчезла, а после клика по diagnostics она появляется только на реально выбранной строке ошибки.

## Session — line number gutter widened and aligned with shell palette

### Что сделано
- в ShellStyles.xaml переопределён shell-стиль для AvalonEdit TextArea, чтобы левый gutter с номерами строк рендерился как отдельная светло-серая зона;
- увеличена минимальная ширина поля нумерации строк, чтобы цифры не выглядели сжатыми;
- в MainWindow.xaml задан явный LineNumbersForeground, синхронизированный с shell palette;
- regression tests обновлены под новый editor wiring и загрузку custom TextArea style.

### Что решено
- поле нумерации строк стало визуально чище, шире и ближе к референсу;
- левый gutter больше не сливается с белым фоном редактора.

### Touched paths
- src/Axiom.Ide/Resources/ShellStyles.xaml
- src/Axiom.Ide/MainWindow.xaml
- 	ests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/WORKLOG.md

### Проверка
- dotnet build Axiom.sln — успешно.
- dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build — успешно, 66/66.

### Следующий точный шаг
Сделать короткий live smoke pass в Axiom.Ide: проверить, что line-number gutter стал шире, получил серый shell background и не ломает current diagnostics navigation/highlight behavior.

## Session — shell-state persistence + find/replace dialog refresh

### Что сделано
- В persistence-слое IDE расширен snapshot shell-состояния:
  - добавлены `IsFilesPaneVisible` и `WindowState` в `RecentFilesSnapshot`/`RecentFilesState`;
  - добавлен enum `IdeWindowState` и метод `UpdateShellPreferences(...)` в `IRecentFilesService`;
  - `RecentFilesService` теперь сохраняет/восстанавливает видимость `Files Pane` и `WindowState` вместе с recent-files данными.
- `MainWindowViewModel` теперь:
  - восстанавливает `FilesPane.IsVisible` и preferred window state из persistence при старте;
  - сохраняет изменения видимости `Files Pane` и window state через `PersistShellState(...)`.
- `MainWindow.xaml.cs` получил shell-state wiring:
  - при закрытии окна сохраняется текущее `WindowState`;
  - при смене `DataContext` применяется восстановленное состояние окна.
- Поиск переведён с встроенного `AvalonEdit SearchPanel` на отдельный modeless dialog:
  - добавлены `FindReplaceDialog`, `FindReplaceOptions`, `FindReplaceActionEventArgs`, `EditorFindReplaceEngine`;
  - команды `Find` / `Find Next` / `Find Previous` и операции replace работают через новый dialog/engine;
  - сохранены дополнительные опции поиска (`Match case`, `Whole words`, `Regex`, направление).
- В editor gutter убраны декоративные пунктирные вертикальные линии через очистку `DottedLineMargin` при загрузке editor.
- Добавлены/обновлены IDE regression tests для shell-state persistence, нового find/replace UI wiring и отсутствия SearchPanel attach-лока.

### Что решено
- IDE больше не сбрасывает ключевые shell-настройки между запусками:
  - закрыли в `Minimized`/`Maximized` — следующий запуск открывается в том же состоянии;
  - состояние `View -> Files Pane` сохраняется и восстанавливается.
- Поиск теперь визуально ближе к референсу Thonny, при этом функционально сохраняет уже внедрённые фичи поиска/замены.
- Левый gutter редактора стал визуально чище без пунктирных вертикальных линий.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Editor/EditorFindReplaceEngine.cs`
- `src/Axiom.Ide/Editor/FindReplaceActionEventArgs.cs`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml.cs`
- `src/Axiom.Ide/Editor/FindReplaceOptions.cs`
- `src/Axiom.Ide/Services/Persistence/IdeWindowState.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно, `0` warnings / `0` errors.
- `dotnet test Axiom.sln --no-build` — успешно:
  - `Axiom.Lexing.Tests` `12/12`
  - `Axiom.Parsing.Tests` `35/35`
  - `Axiom.Semantics.Tests` `42/42`
  - `Axiom.Runtime.Tests` `57/57`
  - `Axiom.Execution.Tests` `52/52`
  - `Axiom.Ide.Tests` `72/72`

### Следующий точный шаг
Выполнить короткий ручной smoke pass в живом `Axiom.Ide` на сценарии:
1. закрытие/повторный запуск в `Minimized` и `Maximized`;
2. сохранение состояния `View -> Files Pane`;
3. визуальная проверка нового find/replace dialog и чистого line-number gutter.

## Session — file manager pane + view-only file support

### Что сделано
- `View -> File Manager` заменил старый `View -> Files Pane` в `MainWindow.xaml`, а левая панель лишилась кнопки `Refresh`.
- Левая панель переведена в lazy file-manager режим:
  - root-level строится по дискам;
  - дочерние узлы подгружаются только при раскрытии папки;
  - открытие файла выполняется по клику в дереве.
- Добавлен view-only режим документов для неподдерживаемых расширений:
  - `.axm`, `.txt`, `.md` открываются как обычные редактируемые вкладки;
  - прочие расширения открываются в `IsReadOnly = true` и не дают `Save` / `Save As`.
- Обновлены document/file boundaries:
  - `DocumentFileContent` теперь хранит `IsReadOnly`;
  - `DocumentFileService` определяет editable/view-only режим по расширению;
  - `DocumentWorkspaceService` отдаёт status-message с отметкой `view-only`.
- Расширены IDE-тесты под новый baseline:
  - tree expand/open wiring;
  - lazy children loading;
  - view-only открытие неподдерживаемого файла;
  - обновлены XAML regression checks.

### Что решено
- Левая область IDE больше соответствует file-manager UX из референса (tree with expand/collapse), без принудительного `Refresh`.
- IDE корректно открывает текстовые `.txt`/`.md` в AvalonEdit и не пытается давать редактирование для неподдерживаемых форматов.
- Сохранён прежний persistence baseline: видимость панели и window-state продолжают восстанавливаться между запусками.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Files/FilesPaneViewModel.cs`
- `src/Axiom.Ide/Files/FileTreeService.cs`
- `src/Axiom.Ide/Documents/DocumentTabViewModel.cs`
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `src/Axiom.Ide/Models/DocumentFileContent.cs`
- `src/Axiom.Ide/Services/Documents/DocumentFileService.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/FileTreeServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно.
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `74/74`.
- `dotnet test Axiom.sln --no-build` — успешно.

### Следующий точный шаг
Выполнить ручной smoke-pass в живом `Axiom.Ide`: проверить UX дерева (expand/collapse), открытие `.txt`/`.md`, view-only для неподдерживаемых расширений и восстановление состояния `View -> File Manager` после перезапуска.

## Session — file manager UX polish (`+/-`, icons, editable formats, `.exe` block, pane close button)

### Что сделано
- В `MainWindow.xaml` обновлён шаблон `TreeViewItem` для file manager:
  - expander переведён на `+/-` glyph (вместо треугольника);
  - добавлены разные иконки для папок и файлов в дереве;
  - в заголовок `File Manager` добавлена кнопка `X` с `CloseFilesPaneCommand`.
- В `MainWindowViewModel` добавлена команда `CloseFilesPaneCommand`, связанная с видимостью панели.
- Логика открытия файлов из tree обновлена:
  - `.exe` явно блокируются и не открываются в редакторе;
  - остальные форматы, включая `.json`, `.css` и другие текстовые файлы, открываются как editable документы.
- В `DocumentFileService` заменён старый view-only fallback:
  - оставлена блокировка `.exe`;
  - для остальных файлов возвращается `IsReadOnly = false`.
- Обновлены IDE tests:
  - сценарий non-`.axm` открытия проверяет editable-режим;
  - добавлен тест блокировки `.exe` в file manager;
  - добавлен тест закрытия панели через `CloseFilesPaneCommand`;
  - XAML-проверки дополнены на `CloseFilesPaneCommand`, `+/-` expander и иконки папка/файл.

### Что решено
- File Manager стал ближе к референсу по читаемости дерева (`+/-`, отдельные иконки узлов).
- Пользователь теперь может редактировать открытые из file manager текстовые форматы шире базового набора (`.axm`, `.txt`, `.md`).
- `.exe` больше не попадает в редактор как «кракозябры»: такие файлы блокируются до открытия.
- File Manager можно закрывать двумя способами: через меню `View` и через `X` в шапке панели.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Services/Documents/DocumentFileService.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `76/76`.

### Следующий точный шаг
Выполнить ручной smoke-pass в живом `Axiom.Ide` по новым сценариям File Manager: `+/-` раскрытие, визуальные иконки папка/файл, editable-открытие `.json`/`.css`, блокировка `.exe` и закрытие панели через `X`.

## Session — bottom pane controls polish (`Output/Diagnostics` close buttons, visibility toggles, resize/collapse)

### Что сделано
- В `MainWindow.xaml` обновлена нижняя панель:
  - `View -> Output` и `View -> Diagnostics` переведены в checkable-пункты (`IsChecked` на `BottomPane.IsOutputVisible` / `BottomPane.IsDiagnosticsVisible`);
  - для вкладок `Output` и `Diagnostics` добавлены кнопки закрытия `X` (`CloseOutputPaneCommand`, `CloseDiagnosticsPaneCommand`);
  - `BottomPaneTabs` и вертикальный splitter теперь скрываются при `BottomPane.IsVisible = false`;
  - добавлено явное `ResizeDirection="Rows"` / `ResizeBehavior="PreviousAndNext"` для ручного изменения высоты нижней панели.
- В `BottomPaneViewModel` добавлена защитная логика: во время активного `INPUT` вкладка `Output` не скрывается.
- В `ShellStyles.xaml` для дерева файлов включены `Auto`-scrollbars по вертикали и горизонтали.
- Расширены IDE тесты:
  - `MainWindowViewModelTests` покрывают закрытие `Output`/`Diagnostics`, авто-открытие при `Run` и блокировку закрытия `Output` при ожидании ввода;
  - `EditorFeatureTests` обновлены под checkable `View`-toggle UX и новые XAML-правила нижней панели; добавлен тест на scrollbars в `FilesTreeStyle`.
- Синхронизированы документы `SPECIFICATION`, `ARCHITECTURE`, `IDE_REDESIGN`, `PLANS`.

### Что решено
- Нижняя панель теперь управляется одинаково через меню `View` и через `X` на вкладках, без «залипания» скрытого состояния после перезапуска сценариев `Run`.
- При скрытии обеих нижних вкладок shell не оставляет пустую область: layout полностью схлопывает нижний ряд.
- UX `INPUT` остаётся стабильным: панель `Output` не исчезает во время ожидания пользовательского ввода.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Output/BottomPaneViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln` — успешно.
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` — успешно, `82/82`.
- `dotnet test Axiom.sln --no-build` — успешно.

### Следующий точный шаг
Выполнить ручной smoke-pass в живом `Axiom.Ide` для нового поведения нижней панели (`X`, checkable `View` toggles, auto-open on run/error, collapse/resize) и зафиксировать визуальные результаты.

## Session — file manager vertical scrollbar clipping fix (with visible bottom pane)

### Что сделано
- Исправлен layout в `MainWindow.xaml`: левая панель `File Manager` больше не растягивается на нижние rows (`Output`/`Diagnostics`), а живёт только в верхнем editor-ряду.
- Добавлен regression test в `EditorFeatureTests`, который фиксирует XAML-контракт: `File Manager` не должен иметь `Grid.RowSpan="3"` и перекрывать нижний pane.
- Обновлены `docs/SPECIFICATION.md` и `docs/ARCHITECTURE.md` с явным правилом про границу `File Manager` над нижней панелью.

### Что решено
- При включённых вкладках `Output` и `Diagnostics` вертикальный скролл дерева `File Manager` теперь полностью видим: нижняя кнопка скроллбара не уходит под нижний pane.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно, `83/83`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.

### Следующий точный шаг
Ручной smoke-pass в живом `Axiom.Ide`: визуально перепроверить `File Manager`-скролл, resize нижней панели и сценарии `Output/Diagnostics` после последних UX-правок.

## Session — File Manager context menu + inverse selection highlight

### Что сделано
- В `MainWindow.xaml` для узлов `File Manager` добавлено node-level context menu в стиле референса: рабочие пункты `Открыть в Axiom IDE` / `Перейти в`, `Открыть во внешнем приложении по умолчанию`, `Показать скрытые файлы`, `Свойства`, а неподдержанные file-operations оставлены disabled.
- В `MainWindow.xaml.cs` добавлен right-click selection flow: правый клик сначала выбирает `TreeViewItem`, но не открывает файл в редакторе автоматически.
- В `MainWindowViewModel` добавлены shell-команды для navigation/open/properties flow в `File Manager`, плюс поддержка toggled hidden-files режима.
- В `FilesPaneViewModel` и `FileTreeService` добавлен флаг `ShowHiddenFiles`; hidden-элементы теперь можно показывать из context menu, при этом системные элементы по-прежнему не выводятся.
- Выделение узла дерева сделано контрастным: выбранные файл/папка, включая их иконки, отображаются белым текстом на accent background.
- Обновлены IDE/XAML/file-tree tests под новый UX-контракт.

### Что решено
- `File Manager` получил ожидаемый desktop-style right-click UX без ложного double-action, когда вызов меню сам по себе открывает файл.
- Пользователь теперь может быстро перейти в каталог, открыть выбранный элемент внешне, временно показать hidden-файлы и посмотреть свойства узла прямо из панели дерева.
- Scope redesign не расширен в полноценный файловый менеджер записи: create/rename/delete/cut/copy/paste по-прежнему явно не поддерживаются и не выглядят как рабочие функции.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Files/FilesPaneViewModel.cs`
- `src/Axiom.Ide/Files/FileTreeService.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/FileTreeServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно, `88/88`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.

### Следующий точный шаг
Ручной smoke-pass в живом `Axiom.Ide`: проверить right-click context menu на файле и папке, toggle hidden-files, инверсное выделение выбранного узла и внешний open.

## Session — restore normal dotnet run for Axiom.Ide (validated)

### Что сделано
- повторно воспроизведён пользовательский сценарий запуска: cd C:\Axiom; dotnet run --project .\src\Axiom.Ide\Axiom.Ide.csproj;
- подтверждено, что причина падения была в MainWindow.xaml: контекстное меню File Manager, подвешенное на уровне TreeViewItem, давало циклическую зависимость в WPF binding-графе;
- контекстное меню перенесено на уровень самого TreeView, чтобы команды работали через FilesPane.SelectedNode, без popup-привязок к каждому TreeViewItem;
- обычный запуск IDE заново проверен после правки.

### Что решено
- команда cd C:\Axiom; dotnet run --project .\src\Axiom.Ide\Axiom.Ide.csproj снова штатно запускает IDE и больше не падает на InitializeComponent.

### Touched paths
- src/Axiom.Ide/MainWindow.xaml
- docs/WORKLOG.md

### Проверка
- dotnet run --project .\src\Axiom.Ide\Axiom.Ide.csproj — процесс IDE успешно стартует;
- dotnet build Axiom.sln — успешно;
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-build — успешно, 88/88.

### Следующий точный шаг
- ручной visual smoke-pass нового File Manager context menu уже в живом окне IDE.

## Session 44 - 2026-04-15 18:13 - repository document chronology rule

### Что сделано
- в AGENTS.md зафиксировано постоянное repository-level правило: все развивающиеся документы ведутся только сверху вниз, без добавления новых записей в начало файла;
- отдельно зафиксировано, что новые записи в docs/WORKLOG.md с этого момента обязаны идти в формате ## Session <N> - YYYY-MM-DD HH:mm - <title>;
- в docs/PLANS.md добавлен отдельный baseline-блок с тем же правилом, чтобы оно было видно не только в агентских инструкциях, но и в текущем рабочем плане;
- текущая запись добавлена в конец docs/WORKLOG.md как первый обязательный пример нового формата.

### Что решено
- порядок ведения проектных документов больше не является неявным соглашением;
- для docs/WORKLOG.md закреплены append-only хронология, обязательная нумерация и обязательные локальные дата/время;
- старые записи сохраняются как историческое наследие и не получают придуманных задним числом timestamp-значений.

### Touched paths
- AGENTS.md
- docs/PLANS.md
- docs/WORKLOG.md

### Проверка
- build/test не запускались: изменение документационное и workflow-only.

### Следующий точный шаг
- продолжить работу по Phase 9 и при следующем meaningful task записать новый WORKLOG entry уже строго по формату Session <N> - YYYY-MM-DD HH:mm - <title>.

## Session 45 - 2026-04-15 19:18 - files pane splitter boundary fix

### Что сделано
- в `MainWindow.xaml` исправлен column-splitter между `File Manager` и editor area: убран `Grid.RowSpan="3"`, поэтому вертикальная серая граница живёт только в верхнем editor-ряду;
- в `EditorFeatureTests` добавлен XAML regression test, который фиксирует отсутствие растягивания этого splitter в нижний pane;
- `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md` синхронизированы с новым визуальным контрактом shell layout.

### Что решено
- толстая вертикальная граница слева от editor area теперь должна доходить до горизонтальной границы нижней панели и не продолжаться внутри `Output`/`Diagnostics`.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно, `89/89`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.

### Следующий точный шаг
- выполнить короткий ручной visual smoke-pass в живом `Axiom.Ide` и убедиться, что вертикальная граница `File Manager` заканчивается на горизонтальной границе нижней панели.

## Session 46 - 2026-04-15 19:38 - file manager operations activation

### Что сделано
- в `Axiom.Ide` добавлен отдельный `IFileOperationsService` для файловых операций `create / rename / copy / move / recycle bin`;
- `IDialogPromptService` расширен shell-prompt методами для подтверждения `Да / Нет` и ввода имени, плюс добавлен `TextInputDialog` для `Новый файл...`, `Новый каталог...` и `Переименовать`;
- `MainWindowViewModel` получил рабочие команды `NewFilesPaneFileCommand`, `NewFilesPaneDirectoryCommand`, `CutFilesPaneNodeCommand`, `CopyFilesPaneNodeCommand`, `PasteIntoFilesPaneCommand`, `RenameFilesPaneNodeCommand`, `MoveFilesPaneNodeToRecycleBinCommand`;
- XAML контекстного меню `File Manager` переведён с disabled placeholders на реальные команды;
- rename/move/delete синхронизируют открытые документы редактора, а delete перед отправкой в Корзину закрывает затронутые вкладки через стандартный `Save / Discard / Cancel` workflow;
- добавлены regression tests для viewmodel/XAML и temp-dir tests для `FileOperationsService`.

### Что решено
- контекстное меню `File Manager` больше не является декоративным: из IDE теперь можно создавать файлы и папки, вырезать/копировать/вставлять элементы, переименовывать их и отправлять в Корзину с подтверждением.

### Touched paths
- `src/Axiom.Ide/Files/FileOperationsService.cs`
- `src/Axiom.Ide/Services/Prompts/DialogPromptService.cs`
- `src/Axiom.Ide/Services/Prompts/TextInputDialog.xaml`
- `src/Axiom.Ide/Services/Prompts/TextInputDialog.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/App.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/FileOperationsServiceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно, `98/98`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` — успешно.

### Следующий точный шаг
- выполнить ручной smoke-pass в живом `Axiom.Ide`: создать файл/папку, проверить rename, cut/copy/paste и confirm-delete в Корзину через `File Manager`.

## Session 47 - 2026-04-15 19:57 - file manager tree-state preservation

### Что сделано
- в `MainWindowViewModel` reload логика `File Manager` переведена на path-based восстановление expanded-state и selection после file operations;
- create, rename, cut/paste и delete теперь обновляют дерево без схлопывания ветки пользователя обратно до `Disk (C:)`;
- пункт контекстного меню `Перенести в Корзину` переименован в `Удалить`, при этом удаление по-прежнему отправляет элемент в Корзину после подтверждения;
- добавлены regression tests на сохранение раскрытого состояния дерева после create/rename и обновлён delete label в XAML test.

### Что решено
- refresh дерева больше не ломает рабочий контекст пользователя после действий над файлами и каталогами;
- delete UX приведён к ожидаемой формулировке `Удалить`, не меняя фактическую механику безопасного удаления через Корзину.

### Touched paths
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точный шаг
- выполнить короткий ручной smoke-pass в живом `Axiom.Ide` и убедиться, что после create/rename/delete/cut-paste пользователь остаётся в той же раскрытой ветке `File Manager`.

## Session 48 - 2026-04-15 20:15 - file tree notify-model fix

### Что сделано
- найден вероятный root cause несохраняющегося expanded-state: `FileTreeNode` был plain CLR-object без `PropertyChanged`;
- `FileTreeNode` переведён на notifying-модель через `ViewModelBase`, включая `IsExpanded`, `DisplayName`, `IsLoaded` и остальные свойства узла;
- добавлен regression test на то, что изменение `FileTreeNode.IsExpanded` теперь реально поднимает `PropertyChanged`.

### Что решено
- WPF-дерево больше не зависит от немой plain-model при синхронизации раскрытия веток и должно стабильнее переживать reload после file operations.

### Touched paths
- `src/Axiom.Ide/Files/FilesPaneViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точный шаг
- повторно проверить в живом `Axiom.Ide`, остаётся ли дерево раскрытым после create/rename/delete/cut-paste, теперь уже с notifying-model узлов.

## Session 49 - 2026-04-15 20:24 - status bar toggle activation

### Что сделано
- `View -> Status Bar` переведён из декоративного menu item в рабочий checkable toggle;
- сама нижняя `StatusBar` в `MainWindow.xaml` теперь скрывается/показывается через `StatusBar.IsVisible`;
- `StatusBarViewModel` расширен visibility-состоянием, а shell persistence теперь сохраняет и восстанавливает видимость status bar между запусками;
- добавлены regression tests на XAML binding, viewmodel persistence и `RecentFilesService` shell-state persistence для status bar.

### Что решено
- пункт `Status Bar` теперь управляет именно нижней строкой состояния shell, а не является пустой заглушкой;
- скрытие status bar влияет только на видимость нижней полосы с сообщением статуса, `Ln / Col` и путём файла, без изменения поведения редактора, запуска программы или diagnostics.

### Touched paths
- `src/Axiom.Ide/Status/StatusBarViewModel.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `102/102`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точный шаг
- выполнить короткий ручной smoke-pass в живом `Axiom.Ide`: проверить toggle `View -> Status Bar` и убедиться, что его состояние переживает перезапуск IDE.
## Session 50 - 2026-04-15 20:50 - classic editor formatting baseline

### Р§С‚Рѕ СЃРґРµР»Р°РЅРѕ
- РІ IDE РґРѕР±Р°РІР»РµРЅ baseline `Format` menu СЃ `Classic` toggle Рё disabled `Modern` placeholder;
- РІРЅРµРґСЂРµРЅ auto-format С‚РµРєСѓС‰РµР№ СЃС‚СЂРѕРєРё РїСЂРё СѓС…РѕРґРµ caret РЅР° РґСЂСѓРіСѓСЋ СЃС‚СЂРѕРєСѓ Рё РїСЂРё loss of keyboard focus editor;
- РґРѕР±Р°РІР»РµРЅ IDE-side formatter service СЃ `Classic` style: uppercasing keywords/type names, uppercasing built-in calls, spacing around operators, normalization `REM` comments, safe no-op РґР»СЏ malformed line;
- vocabulary formatter-а РїРµСЂРµРІРµРґРµРЅ РЅР° С‡С‚РµРЅРёРµ keywords/type names/built-ins РёР· `Axiom.xshd`, С‡С‚РѕР±С‹ syntax highlighting Рё formatting СЌРІРѕР»СЋС†РёРѕРЅРёСЂРѕРІР°Р»Рё РёР· РѕРґРЅРѕРіРѕ IDE source of truth;
- РґРѕР±Р°РІР»РµРЅС‹ regression tests РЅР° formatting service, XAML/code-behind wiring Рё default state `IsClassicFormattingEnabled`.

### Р§С‚Рѕ СЂРµС€РµРЅРѕ
- РїРµСЂРІС‹Р№ РІР°СЂРёР°РЅС‚ visual formatting РґР»СЏ BASIC-РєРѕРґР° С‚РµРїРµСЂСЊ Р¶РёРІС‘С‚ РЅР° shell/service boundary Рё РЅРµ С‚СЂРµР±СѓРµС‚ Р·Р°Р»РµР·Р°С‚СЊ РІ parser/runtime;
- РЅРѕРІС‹Рµ keywords/functions, РєРѕС‚РѕСЂС‹Рµ Р±СѓРґСѓС‚ РґРѕР±Р°РІР»РµРЅС‹ РІ IDE highlighting resource, РЅР°С‡РЅСѓС‚ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СѓС‡Р°СЃС‚РІРѕРІР°С‚СЊ РІ `Classic` formatting Р±РµР· РѕС‚РґРµР»СЊРЅРѕРіРѕ РґСѓР±Р»РёСЂРѕРІР°РЅРёСЏ СЃРїРёСЃРєРѕРІ;
- `Modern` РѕСЃС‚Р°РІР»РµРЅ РєР°Рє СЃР»РµРґСѓСЋС‰РёР№ isolated follow-up, Р±РµР· СЂР°Р·РјС‹РІР°РЅРёСЏ scope С‚РµРєСѓС‰РµРіРѕ slice.

### Touched paths
- `src/Axiom.Ide/Editor/Formatting/CodeFormattingStyle.cs`
- `src/Axiom.Ide/Editor/Formatting/ICodeFormattingService.cs`
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomHighlightingVocabulary.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### РџСЂРѕРІРµСЂРєР°
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` вЂ” СѓСЃРїРµС€РЅРѕ, `110/110`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` вЂ” СѓСЃРїРµС€РЅРѕ.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` вЂ” СѓСЃРїРµС€РЅРѕ.

### РЎР»РµРґСѓСЋС‰РёР№ С‚РѕС‡РЅС‹Р№ С€Р°Рі
- РІС‹РїРѕР»РЅРёС‚СЊ РєРѕСЂРѕС‚РєРёР№ manual smoke-pass РІ Р¶РёРІРѕРј `Axiom.Ide`: РїСЂРѕРІРµСЂРёС‚СЊ `Classic` РЅР° `Enter`, РїРµСЂРµС…РѕРґРµ СЃРѕ СЃС‚СЂРѕРєРё СЃС‚СЂРµР»РєР°РјРё/РјС‹С€СЊСЋ, inline `REM`, malformed line Рё cases СЃ selection/read-only document.
## Session 51 - 2026-04-15 21:03 - classic mouse-leave formatting fix

### Р§С‚Рѕ СЃРґРµР»Р°РЅРѕ
- РёСЃРїСЂР°РІР»РµРЅ РѕС‚РґРµР»СЊРЅС‹Р№ defect `Classic`: РїСЂРё СѓС…РѕРґРµ СЃРѕ СЃС‚СЂРѕРєРё РјС‹С€СЊСЋ formatting РЅРµ СЃСЂР°Р±Р°С‚С‹РІР°Р», РµСЃР»Рё caret РѕСЃС‚Р°РІР°Р»СЃСЏ РІ С‚РѕРј Р¶Рµ editor;
- РІ `MainWindow.xaml.cs` РґРѕР±Р°РІР»РµРЅ post-click hook РЅР° `PreviewMouseLeftButtonUp`, РєРѕС‚РѕСЂС‹Р№ РїРѕСЃР»Рµ Р·Р°РІРµСЂС€РµРЅРёСЏ РєР»РёРєР° СЃСЂР°РІРЅРёРІР°РµС‚ previous line СЃ current line Рё С„РѕСЂРјР°С‚РёСЂСѓРµС‚ РїРѕРєРёРЅСѓС‚СѓСЋ СЃС‚СЂРѕРєСѓ;
- РґРѕР±Р°РІР»РµРЅ regression test РЅР° РЅР°Р»РёС‡РёРµ mouse-up wiring РІ code-behind.

### Р§С‚Рѕ СЂРµС€РµРЅРѕ
- `Classic` С‚РµРїРµСЂСЊ РґРѕР»Р¶РµРЅ СЃСЂР°Р±Р°С‚С‹РІР°С‚СЊ РЅРµ С‚РѕР»СЊРєРѕ РЅР° `Enter` Рё keyboard navigation, РЅРѕ Рё РїСЂРё РѕР±С‹С‡РЅРѕРј РєР»РёРєРµ РјС‹С€СЊСЋ РІ РґСЂСѓРіСѓСЋ СЃС‚СЂРѕРєСѓ;
- Р·Р°С‰РёС‚Р° РѕС‚ formatting РІРѕ РІСЂРµРјСЏ active drag/selection СЃРѕС…СЂР°РЅРµРЅР°: formatting РёРґС‘С‚ РїРѕСЃР»Рµ РѕС‚РїСѓСЃРєР°РЅРёСЏ РєРЅРѕРїРєРё РјС‹С€Рё Рё С‚РѕР»СЊРєРѕ РєРѕРіРґР° selection РїСѓСЃС‚Р°.

### Touched paths
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG.md`

### РџСЂРѕРІРµСЂРєР°
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` вЂ” СѓСЃРїРµС€РЅРѕ, `110/110`.

### РЎР»РµРґСѓСЋС‰РёР№ С‚РѕС‡РЅС‹Р№ С€Р°Рі
- РІС‹РїРѕР»РЅРёС‚СЊ РєРѕСЂРѕС‚РєРёР№ manual smoke-pass РІ Р¶РёРІРѕРј `Axiom.Ide` Рё РїРѕС‚РѕРј РїРµСЂРµР№С‚Рё Рє СЂРµР°Р»РёР·Р°С†РёРё `Modern`.
## Session 52 - 2026-04-15 21:14 - run output fallback for unsaved editor content

### Р§С‚Рѕ СЃРґРµР»Р°РЅРѕ
- РїСЂРѕРІРµСЂРµРЅРѕ, С‡С‚Рѕ РЅРµСЃРѕС…СЂР°РЅС‘РЅРЅС‹Р№ РґРѕРєСѓРјРµРЅС‚ РЅРµ РґРѕР»Р¶РµРЅ Р±Р»РѕРєРёСЂРѕРІР°С‚СЊ `Run`: IDE Р·Р°РїСѓСЃРєР°РµС‚ С‚РµРєСѓС‰РёР№ `SourceText` Р°РєС‚РёРІРЅРѕР№ РІРєР»Р°РґРєРё, Р° РЅРµ С‚РѕР»СЊРєРѕ СЃРѕС…СЂР°РЅС‘РЅРЅС‹Р№ С„Р°Р№Р» СЃ РґРёСЃРєР°;
- РІРѕСЃРїСЂРѕРёР·РІРµРґРµРЅРѕ, С‡С‚Рѕ СЏРґСЂРѕ language/runtime РєРѕСЂСЂРµРєС‚РЅРѕ РёСЃРїРѕР»РЅСЏРµС‚ РїСЂРёРјРµСЂ СЃ `PRINT LEN(\"x\") + 1 ... IF a = 0 THEN ... END IF` Рё РІРѕР·РІСЂР°С‰Р°РµС‚ РІС‹РІРѕРґ `2` Рё `СЂР°РІРЅРѕ`;
- РІ `MainWindowViewModel.RunProgram` РґРѕР±Р°РІР»РµРЅ fallback: РµСЃР»Рё run-service РІРµСЂРЅСѓР» `OutputText`, РЅРѕ transcript РЅРёР¶РЅРµР№ РїР°РЅРµР»Рё РїРѕ РєР°РєРѕР№-С‚Рѕ РїСЂРёС‡РёРЅРµ РїСѓСЃС‚, IDE РІСЃС‘ СЂР°РІРЅРѕ РїРѕРєР°Р·С‹РІР°РµС‚ output;
- РґРѕР±Р°РІР»РµРЅ regression test РЅР° С†РµРїРѕС‡РєСѓ `RunCommand -> BottomPane.OutputText` РґР»СЏ СЃР»СѓС‡Р°СЏ РїСѓСЃС‚РѕРіРѕ console transcript.

### Р§С‚Рѕ СЂРµС€РµРЅРѕ
- РїСѓСЃС‚РѕР№ `Output` РїСЂРё СѓСЃРїРµС€РЅРѕРј Р·Р°РїСѓСЃРєРµ Р±РѕР»СЊС€Рµ РЅРµ РґРѕР»Р¶РµРЅ РІРѕР·РЅРёРєР°С‚СЊ РёР·-Р·Р° РїРѕС‚РµСЂРё IDE transcript-update;
- РїРѕРґРѕР·СЂРµРЅРёРµ РЅР° РЅРµСЃРѕС…СЂР°РЅС‘РЅРЅС‹Р№ С„Р°Р№Р» РєР°Рє РїСЂРёС‡РёРЅСѓ bug-а СЃРЅСЏС‚Рѕ: РїСЂРѕР±Р»РµРјР° Р±С‹Р»Р° РІ display/fallback path IDE, Р° РЅРµ РІ `Run` РїРѕ untitled document.

### Touched paths
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/WORKLOG.md`

### РџСЂРѕРІРµСЂРєР°
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` вЂ” СѓСЃРїРµС€РЅРѕ, `111/111`.

### РЎР»РµРґСѓСЋС‰РёР№ С‚РѕС‡РЅС‹Р№ С€Р°Рі
- РІС‹РїРѕР»РЅРёС‚СЊ РєРѕСЂРѕС‚РєРёР№ manual smoke-pass РІ `Axiom.Ide`: РїСЂРѕРіРЅР°С‚СЊ РїСЂРёРјРµСЂ СЃ `PRINT LEN(\"x\") + 1`, РЅРµ СЃРѕС…СЂР°РЅСЏСЏ РІРєР»Р°РґРєСѓ, Рё СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ `Output` РїРѕРєР°Р·С‹РІР°РµС‚ `2` Рё `СЂР°РІРЅРѕ`.

## Session 53 - 2026-04-15 21:30 - IDE diagnostics session logging

### Что сделано
- изучен старый референс `C:\projects\Axiom\Axiom.Next\AxiomNext.IDE` и перенесён общий подход с session log-файлом и app-level exception tracing;
- в `Axiom.Ide` добавлен новый trace boundary: `IIdeTraceService`, `NullIdeTraceService`, `FileIdeTraceService`;
- `App.xaml.cs` теперь открывает session log при старте IDE, логирует startup и пишет необработанные исключения в тот же файл;
- `ProgramConsoleService` логирует начало консольной сессии, вывод строк, запросы `INPUT` и подтверждение ввода;
- `ProgramRunService` логирует summary выполнения pipeline и unexpected execution exceptions;
- `MainWindowViewModel.RunProgram` логирует `run.request`, `run.result` и отдельное событие `run.output-fallback`, если вывод пришлось восстанавливать из `ProgramRunResult.OutputText`;
- добавлены регрессионные тесты на файловый trace service и на trace-покрытие console/run/viewmodel paths.

### Что решено
- у IDE теперь есть постоянный файловый след для расследования случаев, когда пользователь видит пустой `Output`, хотя runtime должен был что-то вывести;
- trace внедрён как отдельный IDE-layer и не меняет поведение языка, парсера или runtime.

### Touched paths
- `src/Axiom.Ide/Services/Diagnostics/IIdeTraceService.cs`
- `src/Axiom.Ide/Services/Diagnostics/NullIdeTraceService.cs`
- `src/Axiom.Ide/Services/Diagnostics/FileIdeTraceService.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/App.xaml.cs`
- `tests/Axiom.Ide.Tests/FileIdeTraceServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `115/115`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точечный шаг
- вручную воспроизвести проблемный сценарий в `Axiom.Ide`, открыть свежий файл из `Logs` и сравнить trace для неработающего примера и рабочего примера `PRINT LCASE("HeLLo")`.

## Session 54 - 2026-04-15 21:48 - save-before-run execution flow

### Что сделано
- исправлена инициализация `AvalonEditTextBinding` для пустых новых вкладок: binding теперь принудительно attach-ится при `OnEditorLoaded`, поэтому текст редактора не теряется в untitled-документах;
- `Run` в `MainWindowViewModel` переведён на save-before-run flow;
- если документ новый или dirty, IDE теперь не молчит, а показывает prompt на сохранение перед запуском;
- после успешного сохранения IDE перечитывает файл с диска через `IDocumentFileService.LoadDocument(...)` и запускает уже файловую версию текста, а не текущий editor buffer;
- если пользователь отказывается от сохранения или сохранение не удаётся, запуск отменяется без выполнения;
- добавлены регрессионные тесты на AvalonEdit binding и на save-before-run сценарии во viewmodel.

### Что решено
- правило запуска теперь соответствует ожидаемой логике редактора: сначала сохранить, потом выполнять файл по его реальному пути;
- запуск dirty/untitled документа больше не выглядит как молчаливый no-op.

### Touched paths
- `src/Axiom.Ide/Editor/AvalonEditTextBinding.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/AvalonEditTextBindingTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `118/118`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точечный шаг
- вручную проверить в живом `Axiom.Ide` три сценария: новый несохранённый документ, сохранённый-but-dirty документ и чистый сохранённый документ;
- после этого отдельно решить UX для открытия/очистки папки `Logs` из меню IDE.

## Session 55 - 2026-04-15 22:02 - bottom pane input sync fix

### Что сделано
- по trace-логу подтверждено, что runtime получал для обоих `INPUT` пустую строку, хотя сам execution pipeline и sample `v0_1_baseline.axm` корректны;
- причина найдена в IDE: `ConsoleInputTextBox` был привязан к `BottomPane.CurrentInputText`, но введённое значение не возвращалось обратно в `ProgramConsoleService.CurrentInputText`, из которого читает `SubmitInput()`;
- в `MainWindowViewModel.OnBottomPanePropertyChanged` добавлена обратная синхронизация `BottomPane.CurrentInputText -> ProgramConsoleService.CurrentInputText`;
- добавлен regression test, который воспроизводит `RequestInput -> ввести текст через BottomPane -> SubmitInput` и проверяет, что в runtime уходит реальный ввод пользователя.

### Что решено
- ошибка `MB3003` для `INPUT age` в `samples/smoke/v0_1_baseline.axm` была IDE-side bug, а не проблемой преобразования `45` в `INTEGER`;
- последовательные `INPUT` теперь должны получать введённые пользователем значения, а не пустую строку.

### Touched paths
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `119/119`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точечный шаг
- вручную перезапустить `samples/smoke/v0_1_baseline.axm` в `Axiom.Ide` и проверить сценарий `Иван` / `45`;
- отдельным follow-up решить UX для открытия и очистки `Logs` из меню IDE.

## Session 56 - 2026-04-15 22:14 - modern formatting mode

### Что сделано
- `AxiomCodeFormattingService` переведён на общий style-aware pipeline: spacing/tokenization остались общими, а casing теперь зависит от `CodeFormattingStyle`;
- реализован `Modern` стиль: keywords, type names, `REM` и built-in function calls теперь форматируются в `Title Case`, при этом plain identifiers не переименовываются;
- shell viewmodel больше не хранит hardcoded Classic-only toggle: добавлен выбранный `SelectedFormattingStyle` и команды переключения `Classic` / `Modern`;
- меню `Format` обновлено: `Modern` больше не disabled placeholder и реально переключает активный форматирующий режим;
- code-behind редактора теперь форматирует строку в соответствии с выбранным стилем, а не всегда в `Classic`.

### Что решено
- `Modern` реализован как ровно тот же formatter baseline, что и `Classic`, но с другим алгоритмом регистра символов;
- будущие keywords/type names/built-ins продолжают автоматически подхватываться из highlighting vocabulary и для `Classic`, и для `Modern`.

### Touched paths
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `121/121`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.

### Следующий точечный шаг
- вручную проверить `Classic` и `Modern` в живом `Axiom.Ide` на одинаковых строках (`dim a as integer`, `print len("x")+1`, `rem note`) при Enter, клике мышью в другую строку и уходе стрелками.

## Session 57 - 2026-04-15 22:29 - document-wide style rewrite

### Что сделано
- в `ICodeFormattingService` добавлен whole-document path, чтобы один и тот же formatter обслуживал и построчное, и документное преобразование;
- `MainWindowViewModel` теперь при переключении `Classic` / `Modern` сразу прогоняет весь активный документ через выбранный стиль;
- при смене активной вкладки shell автоматически приводит её к текущему formatting mode;
- rewrite ограничен только editable `.axm`-документами: `.txt`, `.md` и прочие вкладки не трогаются.

### Что решено
- массовое преобразование живёт на уровне shell/viewmodel, а не в code-behind редактора, поэтому оно работает одинаково и для уже открытых вкладок, и для последующей активации вкладок;
- dirty-state после реального изменения текста сохраняется как обычное пользовательское изменение и не требует отдельной специальной модели.

### Touched paths
- `src/Axiom.Ide/Editor/Formatting/ICodeFormattingService.cs`
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `125/125`.
- первый параллельный `dotnet test` упёрся в блокировку `Axiom.Ide.dll` другим процессом; повторный прогон после сборки прошёл успешно.

### Следующий точечный шаг
- вручную проверить сценарий с несколькими вкладками: `.axm` в `Classic`, `.axm` в `Modern`, соседний `.txt`, затем попереключать вкладки и убедиться, что rewrite применяется только к `.axm`.

## Session 58 - 2026-04-15 22:37 - selectable status bar path

### Что сделано
- путь активного документа в нижнем status bar переведён с `TextBlock` на read-only `TextBox`, чтобы его можно было выделять мышью и копировать;
- для path area добавлен отдельный `StatusBarPathTextBoxStyle`, чтобы визуально поле осталось частью status bar, а не обычным editor control;
- на path area добавлен tooltip с тем же полным путём, что и в `StatusBar.PathText`.

### Что решено
- задача закрыта чисто на XAML/style-уровне, без вмешательства в `StatusBarViewModel` и без изменения логики вычисления пути;
- копирование полного пути теперь доступно пользователю стандартными средствами Windows (`выделение`, `Ctrl+C`) без дополнительных команд меню.

### Touched paths
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `125/125`.

### Следующий точечный шаг
- вручную проверить на длинном пути, что выделение мышью удобно и что не нужен отдельный UX-follow-up вроде `Copy Full Path`.

## Session 59 - 2026-04-16 14:58 - block auto-completion UX specification

### Что сделано
- в `docs/SPECIFICATION.md` добавлена отдельная IDE-спецификация для block auto-completion и структурного автоотступа;
- зафиксированы триггеры поведения: основной через `Enter`, дополнительный при уходе на другую строку после редактирования;
- формально описаны правила вставки closing lines для `If ... Then`, `For`, `While`, `Select Case`, `Do`, `Do While`, `Do Until`;
- отдельно зафиксировано ограничение v1 для post-condition форм `Do ... Loop While/Until`: IDE не должна угадывать их автоматически заранее;
- зафиксированы правила структурного `Enter` внутри вложенных блоков: сохранение текущего уровня, возврат на уровень родителя и выход в нулевой уровень только после завершения всех окружающих блоков;
- описаны специальные правила для `ElseIf`, `Else`, `Case`, `Case Else`, чтобы они не создавали лишние closing blocks, но сохраняли читаемый внутренний отступ;
- `docs/PLANS.md` дополнен новым IDE follow-up по проектированию и последующей реализации этого UX-среза.

### Что решено
- будущая реализация считается shell/editor-level функцией IDE, а не изменением grammar, parser, semantics или runtime;
- v1 трактуется как структурное автозавершение блоков с автоотступом, а не как IntelliSense;
- базовый шаг внутреннего отступа для этого механизма зафиксирован как `4` пробела;
- nested editing должен ориентироваться на текущий структурный контекст, а не просто на предыдущую строку текста.

### Touched paths
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- код и тесты не запускались: в этом сеансе менялась только документация и фиксировался UX-контракт будущей реализации.

### Следующий точечный шаг
- спроектировать editor-side механизм определения текущего block context и автоотступа для `.axm`-документов;
- затем добавить automated tests на `Enter`, вложенные блоки, `Else`/`ElseIf`, `Case`, `Do`-варианты и single-undo поведение перед началом кода реализации.

## Session 60 - 2026-04-16 15:01 - auto-completion coverage expansion

### Что сделано
- перепроверены текущие поддерживаемые языковые блоки в `docs/SPECIFICATION.md`, чтобы убедиться, что список автозавершаемых конструкций IDE совпадает с реальной спецификацией языка;
- в IDE-правила block auto-completion добавлены пропущенные парные конструкции `SUB ... END SUB` и `FUNCTION ... END FUNCTION`;
- правило вложенности и structural indent расширено так, чтобы оно явно распространялось и на `Sub/End Sub`, и на `Function/End Function`;
- добавлено отдельное правило расширяемости: если в будущих версиях языка появится новая парная блочная конструкция вроде `TYPE ... END TYPE`, IDE должна автоматически считаться обязанной получить для нее такое же автозавершение, автоотступ и test coverage;
- `docs/PLANS.md` синхронизирован с этим расширением coverage.

### Что решено
- на текущем языке список IDE-автозавершения должен покрывать все реальные парные block constructs: `If`, `For`, `While`, `Select Case`, `Do`, `Sub`, `Function`;
- future block pairs не должны оставаться "ручными" по умолчанию: их IDE-поддержка считается обязательной частью языковой интеграции, а не необязательным follow-up.

### Touched paths
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- код и тесты не запускались: в этом сеансе уточнялось только документированное покрытие IDE-правил и их применимость к будущим конструкциям.

### Следующий точечный шаг
- при проектировании editor-side block context механизма сразу учитывать `Sub` и `Function` как полноценные структурные блоки;
- затем добавить automated tests на весь текущий набор парных конструкций и на правило "новая парная конструкция языка требует синхронного IDE auto-completion follow-up".

## Session 61 - 2026-04-16 15:14 - IDE block auto-completion implementation

### Что сделано
- в `src/Axiom.Ide/Editor/StructuralEditing/` добавлен `AxiomBlockEditingService`, который работает поверх `AvalonEdit` `TextDocument` и реализует:
  - автодобавление closing lines для `If`, `For`, `While`, `Select Case`, `Do`, `Sub`, `Function`;
  - structural indent для пустой следующей строки;
  - удержание caret на рабочем уровне вложенности;
  - защиту от дублирования closing block, если matching close уже существует ниже;
- `MainWindow.xaml.cs` подключён к новому structural editing flow на переходе между строками caret-ом и при уходе со строки мышью;
- добавлен новый набор IDE unit tests `BlockEditingServiceTests` на `If`, `Do`, `Sub`, `Function`, `ElseIf`, `Case Else`, вложенный отступ и line-exit без `Enter`;
- `EditorFeatureTests` обновлены, чтобы зафиксировать wiring нового service в code-behind;
- `docs/ARCHITECTURE.md` и `docs/PLANS.md` синхронизированы с новой IDE-технической структурой и фактом реализации.

### Что решено
- реализация осталась полностью на editor-shell уровне и не затронула parser/runtime;
- structural editing использует существующий `ICodeFormattingService` для casing closing lines, поэтому `Classic` и `Modern` продолжают вести себя согласованно;
- mouse-driven уход со строки тоже участвует в block auto-completion, а не только `Enter`.

### Touched paths
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `137/137`.
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно.
- первый параллельный `dotnet build` во время одновременного `dotnet test` упёрся в блокировку WPF temporary output; повторный последовательный прогон прошёл успешно.

### Следующий точечный шаг
- вручную прогнать новый editor UX в живом `Axiom.Ide` на nested blocks, `Undo`, мышиных переходах и `Classic/Modern` стиле;
- после smoke pass решить, нужен ли дополнительный follow-up для тонкой работы с `Loop While` / `Loop Until`.

## Session 62 - 2026-04-16 15:30 - IDE branch dedent for If and Select Case

### Что сделано
- в `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs` добавлено выравнивание branch-строк на уровень родительского контейнера:
  - `ElseIf` и `Else` теперь автоматически возвращаются на уровень соответствующего `If`;
  - `Case` и `Case Else` теперь автоматически возвращаются на уровень соответствующего `Select Case`;
  - после такого branch-dedent следующая строка тела продолжает получать body-indent уже от выровненной branch-строки;
- в `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs` добавлены регрессионные тесты на криво набранные внутри тела `ElseIf`, `Else`, `Case` и `Case Else`;
- `docs/SPECIFICATION.md` и `docs/PLANS.md` синхронизированы с новым UX-правилом branch-dedent для ветвей `If` и `Select Case`.

### Что решено
- branch-dedent применяется только к конструкциям, у которых есть sibling-ветви внутри одного и того же блока: `If` и `Select Case`;
- для `For`, `While`, `Do`, `Sub`, `Function` аналогичное поведение не вводилось, потому что у них нет промежуточных ветвей такого типа;
- вычисление целевого отступа делается editor-side через поиск ближайшего незакрытого родительского блока, без переноса этой логики в parser/runtime.

### Touched paths
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно, `141/141`;
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- первая параллельная попытка `dotnet build` вместе с `dotnet test` снова уткнулась в блокировку WPF temporary output; последовательный прогон прошёл успешно.

### Следующий точечный шаг
- вручную прогнать live-editor smoke pass на `ElseIf`, `Else`, `Case`, `Case Else`, nested blocks, mouse-driven уходе со строки и `Undo`;
- затем решить, нужен ли отдельный UX-follow-up для более умного редактирования `Loop While` / `Loop Until`.

## Session 63 - 2026-04-16 15:41 - Nested If close detection fix

### Что сделано
- в `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs` исправлена логика `HasMatchingClose` и editor-side stack tracking:
  - matching close теперь считается только для closing line того же типа и на том же уровне отступа, что и соответствующий opening block;
  - за счёт этого внешний `End If` больше не принимается за закрытие вложенного `If` внутри `ElseIf`/`Else`-ветви;
- в `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs` добавлен регрессионный тест на сценарий со nested `If` внутри `ElseIf`, когда ниже уже присутствует внешний `End If`, но внутренний closing line всё равно обязан автоматически вставляться;
- `docs/PLANS.md` синхронизирован с этим bugfix follow-up.

### Что решено
- базовый способ различать closing lines в editor-side structural editing теперь опирается на сочетание block kind + indentation level;
- этого достаточно для текущего UX-контракта IDE, потому что правила автозавершения уже привязаны к красивым структурным отступам и выравниванию closing lines под opening lines.

### Touched paths
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` --filter "TryHandleLineTransition_EnterAfterNestedIfInsideElseIf_InsertsInnerClosingLine"` — успешно;
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- первая параллельная попытка `dotnet build` вместе с `dotnet test` снова уткнулась в блокировку WPF temporary output; последовательный прогон прошёл успешно.

### Следующий точечный шаг
- вручную прогнать nested `If`/`Select Case` в живом `Axiom.Ide`, особенно внутри `ElseIf`, `Else`, `Case` и `Case Else`, чтобы подтвердить UX на реальном caret-flow;
- после этого решить, нужен ли ещё один follow-up для post-condition формы `Loop While` / `Loop Until`.

## Session 64 - 2026-04-16 16:50 - Repeated branch body insertion fix

### Что сделано
- в `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs` branch-строки `ElseIf`, `Else`, `Case`, `Case Else` научены вставлять body-line даже тогда, когда следующая строка уже занята существующим `End If`, `End Select` или другой branch/closing line;
- там же исправлен расчёт `newCaretOffset` после branch-dedent и последующей insertion-операции: теперь offset считается от уже обновлённого caret state, а не от исходного значения до editor-side правок строки;
- в `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs` добавлены регрессионные тесты на повторный `ElseIf` перед `End If` и повторный `Case` перед `End Select`;
- `docs/SPECIFICATION.md` и `docs/PLANS.md` синхронизированы с новым branch-body правилом.

### Что решено
- `ElseIf` может повторяться в одном `If`-блоке сколько угодно раз, и IDE теперь не должна “терять реакцию” на второй и последующие branch headers только потому, что ниже уже стоит closing line;
- тот же принцип распространяется на `Select Case`: новый `Case` не должен считаться “неактивным” только из-за уже существующего `End Select`.

### Touched paths
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` --filter "TryHandleLineTransition_MovingAwayFromRepeatedElseIf_InsertsBranchBodyBeforeClosingLine|TryHandleLineTransition_MovingAwayFromRepeatedCase_InsertsCaseBodyBeforeClosingLine"` — успешно;
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- в этом шаге параллельный WPF build/test конфликт уже не повторялся, потому что проверки гонялись последовательно после правки.

### Следующий точечный шаг
- вручную проверить в живом `Axiom.Ide`, что второй и последующие `ElseIf`/`Case` корректно отрабатывают при `Enter`, стрелке вниз и уходе мышью со строки;
- после smoke pass решить, нужен ли дополнительный UX-follow-up для мгновенной реакции branch-строки ещё до ухода caret с неё.

## Session 65 - 2026-04-16 17:03 - Select Case indentation correction

### Что сделано
- в `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs` уточнено правило branch-indent для `Select Case`: `Case` и `Case Else` теперь выравниваются не на уровень `Select Case`, а на один уровень глубже относительно него;
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs` обновлены под окончательную геометрию `Select Case`:
  - `Case` / `Case Else` ожидаются на `+4` пробела внутрь от `Select Case`;
  - тело каждого `Case` ожидается еще на `+4` пробела внутрь от строки `Case`;
- `docs/SPECIFICATION.md` и `docs/PLANS.md` синхронизированы с этим уточнением UX-контракта.

### Что решено
- окончательная договоренность по оформлению `Select Case` такая:
  - `Select Case` и `End Select` — на одном уровне;
  - `Case` / `Case Else` — на один уровень глубже;
  - содержимое case-body — еще на один уровень глубже;
- прежняя трактовка, в которой `Case` жил на одном уровне с `Select Case`, признана неверной для этого IDE UX.

### Touched paths
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\` --filter "TryHandleLineTransition_AfterCaseElse_IndentsCaseBody|TryHandleLineTransition_IndentedCase_ReindentsToSelectInnerLevelAndIndentsCaseBody|TryHandleLineTransition_IndentedCaseElse_ReindentsToSelectInnerLevelAndIndentsCaseBody|TryHandleLineTransition_MovingAwayFromRepeatedCase_InsertsCaseBodyBeforeClosingLine"` — успешно;
- `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\`` — успешно;
- первая параллельная попытка build/test снова уперлась в блокировку WPF temporary output, а последовательный прогон прошел успешно.

### Следующий точечный шаг
- вручную проверить в живом `Axiom.Ide` nested `Select Case`, чтобы подтвердить реальное поведение `Case` / `Case Else` при `Enter`, стрелках и уходе мышью;
- затем решить, нужен ли еще один UX-follow-up для более раннего auto-indent branch-строк до ухода caret.

## Session 66 - 2026-04-16 18:05 - Manual editor smoke sample added

### Что сделано
- в `samples/smoke/editor_block_structures_manual.axm` добавлен большой ручной smoke-sample для проверки editor-side block auto-completion и structural indent;
- sample специально включает:
  - `If / ElseIf / Else / End If`;
  - `Select Case / Case / Case Else / End Select`;
  - `For / Next`;
  - `While / End While`;
  - все основные формы `Do ... Loop`;
  - вызовы `Sub` и `Function`, а также их собственные внутренние блоки;
  - nested combinations вроде `If -> Select Case -> For -> If -> Do`, `Do Until -> Select Case`, `Function -> Select Case`.

### Что решено
- для ручной проверки editor UX удобнее держать один насыщенный `.axm`-сценарий в репозитории, чем каждый раз собирать комбинации блоков заново из чата;
- sample размещен в `samples/smoke/`, потому что это интеграционный editor smoke-сценарий, а не отдельная языковая фича.

### Touched paths
- `samples/smoke/editor_block_structures_manual.axm`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге добавлен только ручной smoke-sample без изменений runtime/parser/IDE-кода.

### Следующий точечный шаг
- открыть `samples/smoke/editor_block_structures_manual.axm` в `Axiom.Ide` или перепечатать его вручную и прогнать все block-editing сценарии на `Enter`, стрелках и уходе мышью;
- по итогам ручного smoke pass зафиксировать оставшиеся IDE UX-ошибки, если они ещё проявятся.

## Session 67 - 2026-04-16 18:47 - Block scope migration planning

### Что сделано
- проанализировано текущее состояние языка относительно областей видимости:
  - сейчас отдельные scope поддерживаются только для top-level и `Sub` / `Function`;
  - `If`, `For`, `While`, `Select Case` и `Do ... Loop` по текущей спецификации собственного scope не создают;
  - `Dim` внутри цикла/ветви поэтому семантически объявляет имя в scope всей текущей процедуры или файла, а runtime-поведение фактически повторно инициализирует это имя при каждом исполнении `Dim`;
- в `docs/PLANS.md` добавлен migration plan на переход к C#-подобной блочной области видимости для statement blocks;
- отдельно зафиксирован open design question: оставаться ли строгими, как C#, с обязательным `Dim`, или проектировать hybrid model с Python-подобным implicit local assignment.

### Что решено
- направление миграции выбрано в сторону C#-подобных block scopes;
- Python-подобное поведение "имя появляется по первому присваиванию без явного объявления" пока не принято в спецификацию и оставлено как отдельное архитектурное решение, потому что оно сильно влияет на diagnostics и typo-safety.

### Touched paths
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялись только план и журнал, без изменений кода.

### Следующий точечный шаг
- принять одно явное решение по модели объявления локальных переменных:
  - либо строгий C#-путь с обязательным `Dim` и новыми block scopes;
  - либо hybrid path: block scopes как в C#, но implicit locals по первому присваиванию как в Python;
- после этого уже обновлять `docs/SPECIFICATION.md` и только затем идти в binder/runtime implementation.

## Session 68 - 2026-04-16 18:48 - Scope plan examples added

### Что сделано
- migration plan по block scopes в `docs/PLANS.md` дополнен короткими примерами именно на синтаксисе Axiom;
- в план добавлены иллюстрации для:
  - текущей модели видимости;
  - целевой C#-подобной block-scope модели;
  - sibling branches в `If`;
  - локальных переменных внутри `For`;
  - открытого вопроса по времени жизни `For`-переменной;
  - возможного Python-hybrid варианта без явного `Dim`.

### Что решено
- дальнейшие language-design планы по scope и объявлениям переменных нужно формулировать с конкретными примерами на Axiom, а не только текстовыми тезисами;
- это особенно важно здесь, потому что C#-подобные block scopes и Python-подобные implicit locals — два разных источника идей, и без примеров их легко смешать.

### Touched paths
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялись только план и журнал.

### Следующий точечный шаг
- принять одно решение по модели локальных имен и затем перенести эти примеры в обновленную языковую спецификацию как normative behavior examples;
- после этого уже переходить к binder/runtime implementation block scopes.

## Session 69 - 2026-04-16 19:11 - Block scope decision finalized in spec

### Что сделано
- `docs/SPECIFICATION.md` обновлен с окончательным language decision:
  - Axiom принимает строгую C#-подобную block-scope модель;
  - `If`, `ElseIf`, `Else`, `For`, `While`, `Do ... Loop`, `Case`, `Case Else` создают собственные block scopes;
  - `FOR`-control variable объявляется самой строкой `FOR` и живет только внутри данного цикла;
  - язык всегда работает в explicit-declaration режиме, аналогичном `Option Explicit On`;
  - Python-подобные implicit locals по присваиванию и `Option Explicit Off` не входят в текущую спецификацию;
- в `docs/ARCHITECTURE.md` добавлена техническая заметка о будущих последствиях этого решения для binder/runtime;
- в `docs/PLANS.md` добавлен новый update, который снимает open design question и переводит следующий шаг в implementation mode.

### Что решено
- модель областей видимости больше не является open question;
- для текущего языка принят именно строгий C#-подобный путь, а не hybrid model;
- любые future compatibility ideas вокруг `Option Explicit Off` или implicit locals могут рассматриваться только как отдельное расширение после основной реализации, а не как часть текущего migration target.

### Touched paths
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялась только документация, без изменений кода.

### Следующий точечный шаг
- реализовать nested statement-level scopes в binder / `BoundScope`;
- затем реализовать runtime block lifetime для `Dim` / `Const`;
- затем добавить automated tests и acceptance samples, подтверждающие новые scope-правила на реальном коде Axiom.

## Session 70 - 2026-04-16 19:33 - Block scopes implemented in binder and runtime

### Что сделано
- в `Axiom.Semantics` реализованы nested statement-level scopes:
  - `BoundScope` расширен helper-методами для current-scope lookup и parent lookup;
  - binder открывает child scopes для `If` / `ElseIf` / `Else`, `While`, `Do ... Loop`, `Case`, `Case Else` и `For`;
  - `FOR`-control variable больше не требует предварительного `DIM`: symbol объявляется строкой `FOR`, живёт в loop-scope и исчезает после `NEXT`;
  - добавлена отдельная semantic diagnostic для forbidden shadowing имени из enclosing scope;
- в `Axiom.Runtime` реализован stack-based runtime scope model:
  - интерпретатор теперь ведёт отдельный стек `RuntimeScope`;
  - routine scope и block scopes работают согласованно;
  - branch bodies, `While`, `Do`, `Case` и каждая итерация тела `For` создают child runtime scopes;
  - loop control variable `FOR` хранится в отдельном loop-scope, а body locals пересоздаются на каждой итерации;
- regression coverage обновлено:
  - `BinderTests` дополнены сценариями на block-local visibility, sibling branch reuse, `FOR` variable lifetime и shadowing;
  - `ExecutionPipelineTests` дополнены сценариями на positive/negative block-scope behavior;
  - acceptance tests получили positive smoke sample и negative sample на недоступность локала после блока;
  - runtime tests и существующие `FOR`-ориентированные samples приведены к новой семантике;
- добавлены / обновлены runnable `.axm` samples:
  - новый positive smoke sample `samples/smoke/block_scope_semantics.axm`;
  - новый negative sample `samples/acceptance/negative/block_scope_visibility.axm`;
  - обновлены `FOR`-related samples и `smoke_v0_1`, чтобы не использовать устаревшее предварительное `DIM` для control variable.

### Что решено
- language core теперь соответствует зафиксированной C#-подобной scope-модели, а не только документации;
- старое поведение с заранее объявляемой `FOR`-переменной больше не считается нормативным;
- локалы блоков действительно живут только внутри своего блока и не протекают в sibling / outer code.

### Touched paths
- `src/Axiom.Semantics/Scopes/BoundScope.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/acceptance/smoke_v0_1.axm`
- `samples/acceptance/negative/for_step_zero.axm`
- `samples/acceptance/negative/block_scope_visibility.axm`
- `samples/control-flow/for/basic_loop.axm`
- `samples/control-flow/for/exit_for.axm`
- `samples/control-flow/for/nested_loops.axm`
- `samples/control-flow/for/step_loop.axm`
- `samples/smoke/v0_1_baseline.axm`
- `samples/smoke/block_scope_semantics.axm`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet build Axiom.sln`
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- `dotnet test Axiom.sln --no-build`

### Следующий точечный шаг
- вручную открыть в `Axiom.Ide` новые samples на scope diagnostics и UX:
  - `samples/smoke/block_scope_semantics.axm`
  - `samples/acceptance/negative/block_scope_visibility.axm`
  - `samples/acceptance/negative/for_step_zero.axm`
- после manual smoke-pass при необходимости синхронизировать `docs/LANGUAGE_GUIDE.md` с новой моделью областей видимости и `FOR`.

## Session 71 - 2026-04-16 20:09 - Full manual block-scope smoke sample added

### Что сделано
- добавлен единый runnable sample `samples/smoke/block_scope_full_manual.axm` для ручной проверки block-scope поведения в `Axiom.Ide`;
- sample покрывает:
  - `If`, `ElseIf`, `Else`;
  - `Select Case`, `Case`, `Case Else`;
  - `For` без предварительного `Dim` для control variable;
  - `While`;
  - `Do ... Loop`;
  - `Sub` и `Function` с внутренними block locals;
- в конец sample добавлены закомментированные negative probes, которые можно раскомментировать по одной для проверки scope-diagnostics после выхода из блока;
- `AcceptanceProgramTests` дополнен отдельным regression test на этот full smoke sample;
- `docs/PLANS.md` обновлён, чтобы в manual IDE pass фигурировал именно этот новый sample вместе с уже существующими negative scenarios.

### Что решено
- для ручной IDE-проверки теперь есть один основной сценарий, а не набор разрозненных файлов;
- positive path и negative visibility probes собраны рядом, чтобы легче было прогонять поведение редактора и рантайма в одной сессии.

### Touched paths
- `samples/smoke/block_scope_full_manual.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`

### Следующий точечный шаг
- открыть `samples/smoke/block_scope_full_manual.axm` в `Axiom.Ide`, прогнать positive path и затем раскомментировать negative probes по одной;
- отдельно убедиться, что `samples/acceptance/negative/block_scope_visibility.axm` и `samples/acceptance/negative/for_step_zero.axm` показывают ожидаемые diagnostics в UI diagnostics pane.

## Session 72 - 2026-04-16 20:31 - Shadowing diagnostics humanized and generalized

### Что сделано
- `MB2016` переведён на более человекопонятную модель: сообщение теперь говорит, что имя уже объявлено во внешней области видимости, и отдельно называет текущую конструкцию;
- в binder централизованы human-facing declaration-context labels для `IF`, `ELSEIF`, `ELSE`, `FOR`, `WHILE`, `DO`, `CASE`, `CASE ELSE` и параметров routines;
- добавлен regression test на контекст `FOR`, а существующий shadowing test закрепляет упоминание `IF`;
- `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md` синхронизированы так, чтобы это правило явно распространялось на любые будущие block constructs, а не только на текущий набор языка.

### Что решено
- приоритет diagnostics для shadowing теперь формулируется как DX-правило: сообщение должно быть понятным человеку, а не описывать проблему абстрактным термином без контекста;
- для любой будущей блочной конструкции достаточно зарегистрировать новый declaration-context label и добавить тесты, не придумывая новый тип ошибки и не переписывая шаблон диагностики.

### Touched paths
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`

### Следующий точечный шаг
- последовательно прогнать `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` без параллельной сборки;
- затем вручную проверить в `Axiom.Ide`, что `MB2016` читается по-человечески на сценариях с `IF` и `FOR`, а negative scope samples по-прежнему показывают ожидаемые diagnostics.

## Session 73 - 2026-04-16 20:38 - Manual smoke sample restored after negative probe run

### Что сделано
- `samples/smoke/block_scope_full_manual.axm` возвращён в каноническое positive-path состояние после ручного IDE-прогона с раскомментированными negative probes;
- acceptance test на этот sample снова проходит без semantic failures, потому что негативные строки снова оставлены в закомментированном виде.

### Что решено
- ручная IDE-проверка negative probes остаётся частью smoke workflow, но репозиторная версия sample должна храниться в runnable positive-path состоянии, чтобы automated acceptance tests оставались зелёными.

### Touched paths
- `samples/smoke/block_scope_full_manual.axm`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore`
- `dotnet test Axiom.sln --no-build`

### Следующий точечный шаг
- вручную проверить в `Axiom.Ide`, что текст `MB2016` читается по-человечески на сценариях с `IF` и `FOR`;
- при следующем ручном negative smoke-run не сохранять sample в репозиторном состоянии с раскомментированными probes.

## Session 74 - 2026-04-16 20:47 - Language guide synchronized with block-scope semantics

### Что сделано
- `docs/LANGUAGE_GUIDE.md` обновлён под текущую модель областей видимости:
  - в главу про переменные и `DIM` добавлено отдельное простое объяснение block scope;
  - главы `IF`, `SELECT CASE`, `FOR`, `WHILE`, `DO ... LOOP` дополнены правилами видимости локальных переменных;
  - из учебных примеров убраны устаревшие конструкции вида `DIM i` перед `FOR i = ...`;
  - глава про типичные ошибки дополнена примерами block-scope ошибок и конфликтов имён.

### Что решено
- учебная книга теперь объясняет ту же scope-модель, которую реально исполняют binder и runtime;
- руководство больше не учит старому поведению `FOR`-переменной и не оставляет block-scope как неявную деталь языка.

### Touched paths
- `docs/LANGUAGE_GUIDE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялась только документация.

### Следующий точечный шаг
- при следующем редакторском проходе вручную перечитать главы `SUB` и `FUNCTION` и решить, нужно ли так же явно подсветить routine-scope рядом с уже обновлёнными block scopes.

## Session 77 - 2026-04-16 21:21 - Mixed inferred DIM form documented

### Что сделано
- `docs/SPECIFICATION.md` дополнён явным правилом, что в одном операторе `DIM` можно смешивать несколько inferred groups, например `DIM count = 10, name = "Alice", ready = TRUE, price = 1.5`;
- `docs/LANGUAGE_GUIDE.md` дополнен коротким учебным примером этой mixed-form записи;
- `docs/PLANS.md` закрепляет это как официально поддержанную форму, которую не нужно заново обсуждать в следующей сессии.

### Что решено
- mixed-form shorthand для `DIM` больше не остаётся "случайно работающей" формой и теперь прямо входит в норму языка и в учебное описание;
- пользователь может опираться на неё как на официально задокументированную часть Axiom.

### Touched paths
- `docs/SPECIFICATION.md`
- `docs/LANGUAGE_GUIDE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялась только документация.

### Следующий точечный шаг
- вручную прогнать в `Axiom.Ide` positive sample `samples/operators/dim/inferred_type_from_initializer.axm` и убедиться, что mixed-form `DIM` ведёт себя так же, как описано в guide.

## Session 78 - 2026-04-16 21:37 - HTML manual baseline added

### Что сделано
- внутри репозитория создан новый каталог `docs/manual/` как отдельный статический учебник по Axiom;
- добавлены общие стили и единая навигация через:
  - `docs/manual/styles/manual.css`
  - `docs/manual/scripts/manual.js`
- добавлены первые основные страницы HTML-учебника:
  - `docs/manual/index.html`
  - `docs/manual/getting-started.html`
  - `docs/manual/data-and-input.html`
  - `docs/manual/expressions-and-branching.html`
  - `docs/manual/loops.html`
  - `docs/manual/procedures-and-functions.html`
  - `docs/manual/arrays-and-builtins.html`
  - `docs/manual/diagnostics.html`
  - `docs/manual/examples.html`
  - `docs/manual/cheat-sheet.html`
- тон учебника изменён с прежнего “школьного” на более спокойный и студенческий, при этом содержание оставлено в границах реально поддерживаемого Axiom baseline;
- `README.md` обновлён так, чтобы новый HTML-учебник был явно виден как основной способ чтения курса;
- в начало `docs/LANGUAGE_GUIDE.md` добавлена пометка, что markdown-версия теперь является линейным legacy/draft-представлением, а не основным публичным форматом;
- `docs/PLANS.md` синхронизирован с новым статусом учебника.

### Что решено
- основным человекочитаемым форматом учебника теперь считается `docs/manual/index.html`;
- курс дальше должен развиваться странично внутри `docs/manual/`, а не только как один длинный markdown-файл;
- `docs/LANGUAGE_GUIDE.md` не удаляется: он остаётся полезным как история и рабочий черновик для переносов/редакторских сверок.

### Touched paths
- `README.md`
- `docs/LANGUAGE_GUIDE.md`
- `docs/manual/index.html`
- `docs/manual/getting-started.html`
- `docs/manual/data-and-input.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/loops.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/diagnostics.html`
- `docs/manual/examples.html`
- `docs/manual/cheat-sheet.html`
- `docs/manual/styles/manual.css`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялась только документация и статические HTML/CSS/JS-ассеты учебника;
- структура страниц и относительные ссылки проверены вручную по файловой схеме.

### Следующий точечный шаг
- при следующем редакторском проходе либо расширить HTML-учебник привязками к конкретным runnable samples из `samples/`, либо отдельно решить, нужен ли удобный вход в учебник из `Axiom.Ide`.

## Session 76 - 2026-04-16 21:14 - DIM type inference from initializer implemented

### Что сделано
- `DIM` расширен формами с выводом типа из инициализатора:
  - `DIM value = 10`;
  - `DIM name = "Alice"`;
  - `DIM ready = TRUE`;
  - grouped-вариант `DIM left, right = 7`;
- parser и syntax обновлены так, чтобы `AS TYPE` у `DIM` был необязателен только при наличии `= expression`;
- binder научился:
  - использовать явный тип, если `AS TYPE` указан;
  - иначе выводить тип по инициализатору;
  - выдавать новую semantic diagnostic `MB2017`, если в `DIM` нет ни `AS`, ни `=`;
- добавлены parser / semantics / runtime / execution tests на новый `DIM`;
- добавлен runnable positive sample `samples/operators/dim/inferred_type_from_initializer.axm`;
- добавлен runnable negative sample `samples/acceptance/negative/dim_requires_type_or_initializer.axm`;
- `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы с новой формой `DIM`.

### Что решено
- Axiom получил C#-подобное облегчение записи объявлений только для `DIM`, но без потери строгой типизации;
- тип не становится "неизвестным": он выводится из инициализатора и дальше остаётся обычным строгим типом;
- shorthand не распространяется автоматически на `CONST`, чтобы change set оставался маленьким и предсказуемым.

### Touched paths
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/dim/inferred_type_from_initializer.axm`
- `samples/acceptance/negative/dim_requires_type_or_initializer.axm`
- `samples/smoke/block_scope_full_manual.axm`
- `docs/SPECIFICATION.md`
- `docs/LANGUAGE_GUIDE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore`
- `dotnet test Axiom.sln --no-build`

### Следующий точечный шаг
- вручную открыть новые `DIM` samples в `Axiom.Ide` и подтвердить:
  - positive run для `samples/operators/dim/inferred_type_from_initializer.axm`;
  - semantic diagnostic `MB2017` для `samples/acceptance/negative/dim_requires_type_or_initializer.axm`;
- затем при необходимости отдельно обсудить, нужен ли аналогичный shorthand когда-нибудь и для `CONST`.

## Session 75 - 2026-04-16 20:59 - FOR loop guide clarified for per-iteration DIM behavior

### Что сделано
- в главу `FOR` в `docs/LANGUAGE_GUIDE.md` добавлено прямое объяснение, что `DIM` внутри тела цикла выполняется заново на каждой итерации;
- туда же добавлены два коротких контрастных примера:
  - `DIM x` внутри `FOR` даёт `1 1 1`;
  - `DIM x` перед `FOR` даёт накопление `1 2 3`.

### Что решено
- руководство теперь явно отвечает на практический вопрос о том, копится ли значение локальной переменной между итерациями `FOR`;
- поведение Axiom в этом месте зафиксировано как C#-подобное и больше не остаётся неявным.

### Touched paths
- `docs/LANGUAGE_GUIDE.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этом шаге менялась только документация.

### Следующий точечный шаг
- при следующем редакторском проходе вручную перечитать главы `SUB` и `FUNCTION` и решить, нужно ли так же явно подсветить routine-scope рядом с уже обновлёнными block scopes.
## Session 79 - 2026-04-16 22:07 - HTML manual reorganized into reference-style structure

### Что сделано
- HTML manual перестроен из обзорного набора глав в более справочный формат:
  - добавлен отдельный алфавитный и тематический указатель `docs/manual/reference.html`;
  - навигация manual разбита так, чтобы по ключевым конструкциям существовали самостоятельные страницы;
  - `manual.js` расширен каталогом сущностей и двухуровневой фильтрацией по первой букве и по смысловому разделу.
- В manual добавлены новые страницы:
  - `docs/manual/data-types.html`;
  - `docs/manual/select-case.html`;
  - `docs/manual/for.html`;
  - `docs/manual/while.html`;
  - `docs/manual/do-loop.html`;
  - `docs/manual/sub.html`;
  - `docs/manual/function.html`;
  - `docs/manual/return.html`;
  - `docs/manual/arrays.html`;
  - `docs/manual/string-functions.html`;
  - `docs/manual/numeric-functions.html`.
- `docs/manual/variables-and-constants.html` расширен формальными правилами по идентификаторам:
  - допустимое начало имени;
  - допустимые символы;
  - регистронезависимость;
  - запрет reserved words;
  - примеры некорректных имён;
  - явная оговорка, что текущая публичная спецификация пока не фиксирует максимальную длину идентификатора.
- `docs/manual/index.html` синхронизирован с новой структурой: появилась ссылка на reference-режим и обновлён маршрут чтения.
- `docs/PLANS.md` обновлён под новый редакторский курс manual.

### Что решено
- Для учебника выбран гибридный формат:
  - операторы и управляющие конструкции получают отдельные страницы;
  - built-ins группируются по смыслу, но внутри страницы описываются поштучно;
  - спорные или нефиксированные свойства, например точный максимум длины идентификатора или byte-size `STRING`/`BOOLEAN`, не придумываются и помечаются как незафиксированные в публичном контракте.

### Touched paths
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/manual/data-types.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/select-case.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/arrays.html`
- `docs/manual/string-functions.html`
- `docs/manual/numeric-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/styles/manual.css`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические тесты не запускались, потому что в этом проходе менялись только HTML/CSS/JS-ассеты manual и проектная документация;
- после правок нужно выполнить локальную ссылочную проверку manual и убедиться, что все новые страницы доступны из sidebar и pager.

### Следующий точечный шаг
- на следующем проходе привязать страницы manual к конкретным `samples/` и, если потребуется, сделать для manual отдельный индекс diagnostic-кодов и раздел по будущим подсистемам вроде файловых функций.

## Session 80 - 2026-04-16 22:24 - Identifier length rule clarified against implementation

### Что сделано
- Уточнена формулировка по длине идентификаторов в спецификации и manual:
  - раньше manual говорил о том, что публичная спецификация не фиксирует отдельное числовое значение максимальной длины имени;
  - теперь это выражено точнее: Axiom не вводит собственного отдельного cap на длину идентификатора сверх ограничений реализации на .NET/C#.
- Проверен текущий lexer:
  - `IsIdentifierStart()` и `IsIdentifierPart()` описывают допустимые символы имени;
  - отдельного `MaxIdentifierLength` в текущем baseline не обнаружено.

### Что решено
- Для документации выбран более точный тезис:
  - не "длина неизвестна";
  - а "отдельный языковой лимит внутри Axiom не задаётся, дополнительного ужесточения относительно underlying implementation нет".

### Touched paths
- `docs/SPECIFICATION.md`
- `docs/manual/variables-and-constants.html`
- `docs/WORKLOG.md`

### Проверка
- ручная проверка `docs/SPECIFICATION.md` и `src/Axiom.Lexing/Lexer.cs`;
- код и тесты не менялись, поэтому автоматические проверки не запускались.

### Следующий точечный шаг
- продолжить переписывать manual в более академичном стиле референса, начиная с оставшихся базовых глав про операторы и циклы.

## Session 81 - 2026-04-16 23:00 - Manual chapters rewritten into unified reference-style set

### Что сделано
- Переписаны основные HTML-главы manual в единый более справочный и учебный стиль:
  - `docs/manual/getting-started.html`;
  - `docs/manual/if.html`;
  - `docs/manual/select-case.html`;
  - `docs/manual/for.html`;
  - `docs/manual/while.html`;
  - `docs/manual/do-loop.html`;
  - `docs/manual/sub.html`;
  - `docs/manual/function.html`;
  - `docs/manual/return.html`;
  - `docs/manual/arrays.html`;
  - `docs/manual/string-functions.html`;
  - `docs/manual/numeric-functions.html`;
  - `docs/manual/diagnostics.html`;
  - `docs/manual/examples.html`;
  - `docs/manual/cheat-sheet.html`.
- Старые сводные страницы приведены к новой роли обзорных хабов, чтобы manual не возвращался к формату "всё в одной статье":
  - `docs/manual/data-and-input.html`;
  - `docs/manual/expressions-and-branching.html`;
  - `docs/manual/loops.html`;
  - `docs/manual/procedures-and-functions.html`;
  - `docs/manual/arrays-and-builtins.html`.
- По содержанию manual зафиксирован следующий принцип:
  - отдельный оператор или конструкция получают собственную страницу;
  - grouped chapters остаются только как обзорные маршруты и ведут на атомарные статьи;
  - формулировки подгоняются под текущий публичный контракт Axiom, а не под незафиксированные догадки.

### Что решено
- Manual окончательно смещён от "детского линейного курса" к формату учебного справочника:
  - по ключевым конструкциям появилась отдельная точка входа;
  - по built-ins сохранена смысловая группировка, но внутри страницы каждая функция описывается отдельно;
  - обзорные страницы больше не конкурируют с reference-pages, а только помогают навигации.
- После проверки содержимого убран незафиксированный пример с `EXIT WHILE`: в текущем baseline manual опирается на документированные `EXIT FOR` и `EXIT DO`, не добавляя неподтверждённые конструкции.

### Touched paths
- `docs/manual/getting-started.html`
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/arrays.html`
- `docs/manual/string-functions.html`
- `docs/manual/numeric-functions.html`
- `docs/manual/diagnostics.html`
- `docs/manual/examples.html`
- `docs/manual/cheat-sheet.html`
- `docs/manual/data-and-input.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/loops.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнена локальная ссылочная проверка HTML manual: `HTML link check: OK`;
- автоматические тесты не запускались, потому что в этом проходе менялись только HTML-страницы manual и проектная документация.

### Следующий точечный шаг
- привязать manual-страницы к конкретным runnable-примерам из `samples/` по операторам и built-ins;
- затем при необходимости добавить отдельный индекс diagnostic-кодов и зарезервированные разделы под будущие файловые функции.

## Session 82 - 2026-04-17 10:13 - Data types chapter expanded from external reference pattern

### Что сделано
- переработана страница `docs/manual/data-types.html`;
- глава по типам данных расширена до более справочного формата:
  - добавлено объяснение, как Axiom practically делит типы;
  - добавлена сводная таблица по `INTEGER`, `DOUBLE`, `STRING`, `BOOLEAN`;
  - добавлен отдельный блок по формам литералов;
  - уточнены правила для `INTEGER`, `DOUBLE`, `STRING`, `BOOLEAN`;
  - добавлен раздел про вывод типа в `DIM`;
  - раздел преобразований типов переписан в более формальном виде;
  - добавлены прямые ссылки на runnable sample-файлы из `samples/`.
- обновлен `docs/PLANS.md` с зафиксированным направлением для следующих manual-правок.

### Что решено
- внешний учебный референс используется как ориентир по структуре и плотности информации, а не как шаблон для буквального копирования;
- для manual по Axiom нужно объяснять только те категории и свойства типов, которые реально подтверждены спецификацией и текущей реализацией;
- для `STRING` и `BOOLEAN` manual по-прежнему не придумывает искусственные byte-size детали вне публичного контракта языка.

### Touched paths
- `docs/manual/data-types.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические тесты не запускались, потому что в этом проходе менялась только HTML-документация;
- требуется локальная проверка самой страницы manual в браузере на корректную разметку и доступность добавленных ссылок.

### Следующий точечный шаг
- продолжить такой же reference-style редакторский проход по соседним manual-страницам, начиная с `variables-and-constants.html` и глав по вводу/выводу;
- по мере правок дальше привязывать статьи к конкретным runnable `samples/`, чтобы manual оставался связанным с реальными примерами из репозитория.

## Session 83 - 2026-04-17 11:41 - LONG type, overflow diagnostics and docs sync

### Что сделано
- Расширена числовая модель языка:
  - добавлен тип `LONG` в lexer, syntax, parser, semantics, runtime и IDE highlighting;
  - целочисленные литералы теперь лексически разделяются на `INTEGER` и `LONG` по диапазону;
  - литералы вне диапазона `LONG` и не-конечные `DOUBLE` теперь дают syntax diagnostic `MB0004`;
  - runtime-арифметика `INTEGER`, `LONG` и `DOUBLE` теперь останавливается на переполнении/не-конечном результате с diagnostic `MB3008`.
- Синхронизированы связанные языковые места:
  - implicit numeric widening обновлён до цепочки `INTEGER -> LONG -> DOUBLE`;
  - `FOR` теперь допускает control variable типов `INTEGER`, `LONG`, `DOUBLE`;
  - `INPUT` умеет читать значения в переменные типа `LONG`;
  - array-bound conversions и runtime comparisons тоже приведены к новой числовой модели.
- Обновлён acceptance-контур:
  - добавлен runnable positive sample `samples/operators/dim/long_inference.axm`;
  - добавлены negative/error samples `samples/expressions/numeric_literals/long_out_of_range_error.axm`, `samples/expressions/arithmetic/long_overflow_error.axm`, `samples/expressions/arithmetic/double_overflow_error.axm`;
  - расширены lexer/parser/semantics/runtime/execution/IDE tests под `LONG`, out-of-range literals и overflow diagnostics.
- Документация синхронизирована с новой моделью:
  - обновлены `docs/SPECIFICATION.md`, `docs/LANGUAGE_GUIDE.md`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html`, `docs/manual/scripts/manual.js`, `docs/PLANS.md`.

### Что решено
- Поведение `DIM a = 21474836479` больше не считается ошибкой само по себе: такой литерал теперь корректно выводится как `LONG`;
- ошибкой считается именно выход за диапазон конечного представления целевого числового уровня:
  - lexer-level для литералов вне `LONG` / конечного `DOUBLE`;
  - runtime-level для переполнения арифметики и не-конечных результатов;
- acceptance samples для language-behavior change остаются обязательной частью Definition of Done и теперь покрывают новый `LONG` baseline и overflow paths.

### Touched paths
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `samples/operators/dim/long_inference.axm`
- `samples/expressions/numeric_literals/long_out_of_range_error.axm`
- `samples/expressions/arithmetic/long_overflow_error.axm`
- `samples/expressions/arithmetic/double_overflow_error.axm`
- `docs/SPECIFICATION.md`
- `docs/LANGUAGE_GUIDE.md`
- `docs/manual/data-types.html`
- `docs/manual/diagnostics.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test tests/Axiom.Lexing.Tests/Axiom.Lexing.Tests.csproj --no-restore -v minimal` — успешно;
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore -v minimal` — успешно;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore -v minimal` — успешно;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore -v minimal` — успешно;
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore -v minimal` — успешно (`71/71`);
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore -v minimal` — успешно;
- `dotnet test Axiom.sln --no-restore -v minimal` — успешно.

### Следующий точечный шаг
- Вручную прогнать новые `.axm`-samples через `Axiom.Ide` и проверить, что:
  - `samples/operators/dim/long_inference.axm` выполняется без diagnostics;
  - `samples/expressions/numeric_literals/long_out_of_range_error.axm` даёт `MB0004`;
  - `samples/expressions/arithmetic/long_overflow_error.axm` и `samples/expressions/arithmetic/double_overflow_error.axm` дают `MB3008`.
- Если user-facing текст для явного `AS INTEGER` / `AS LONG` ещё покажется слишком общим, отдельным follow-up добавить более специальный semantic diagnostic для narrowing-out-of-range случаев.

## Session 84 - 2026-04-17 15:16 - DECIMAL type, explicit range diagnostics and docs sync

### Что сделано
- Расширена числовая модель языка:
  - добавлен тип `DECIMAL` в lexer, syntax, parser, semantics, runtime и IDE highlighting;
  - `DECIMAL` в первой версии доступен через явное объявление `AS DECIMAL`, без изменения текущего вывода типа для литералов с точкой, которые без явного типа остаются `DOUBLE`;
  - numeric widening обновлён до ветвей `INTEGER -> LONG -> DOUBLE` и `INTEGER/LONG -> DECIMAL`, а автоматическое смешивание `DECIMAL` и `DOUBLE` намеренно запрещено.
- Синхронизированы связанные языковые сценарии:
  - арифметика `+`, `-`, `*`, `/`, сравнения, `INPUT`, `STR`, `INT`, default values и runtime conversions теперь поддерживают `DECIMAL`;
  - `FOR` по-прежнему ограничен типами `INTEGER`, `LONG`, `DOUBLE`, то есть `DECIMAL` пока не используется как тип управляющей переменной;
  - для явного `AS INTEGER` / `AS LONG` / `AS DECIMAL`, когда константный литерал не помещается в целевой тип, добавлен semantic diagnostic `MB2018`.
- Обновлён acceptance-контур:
  - добавлен runnable positive sample `samples/operators/dim/decimal_explicit_declaration.axm`;
  - добавлены negative/error samples `samples/expressions/numeric_literals/decimal_out_of_range_error.axm` и `samples/expressions/arithmetic/decimal_overflow_error.axm`;
  - расширены lexer/parser/semantics/runtime/execution/IDE tests под `DECIMAL`, `MB2018` и decimal-overflow path.
- Документация синхронизирована:
  - обновлены `docs/SPECIFICATION.md`, `docs/LANGUAGE_GUIDE.md`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html`, `docs/PLANS.md`.

### Что решено
- Для Axiom выбран консервативный ввод `DECIMAL`: точный десятичный тип добавлен, но без каскадного усложнения модели литералов и без автоматической неявной конверсии между `DECIMAL` и `DOUBLE`;
- Порог переполнения и диагностика теперь различаются по этапам:
  - литералы вне диапазона `LONG` / конечного `DOUBLE` остаются syntax-level ошибками;
  - литерал, не помещающийся в явно заданный `INTEGER` / `LONG` / `DECIMAL`, даёт semantic diagnostic `MB2018`;
  - арифметическое переполнение `INTEGER`, `LONG`, `DECIMAL` и не-конечный `DOUBLE` дают runtime diagnostic `MB3008`.

### Touched paths
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `samples/operators/dim/decimal_explicit_declaration.axm`
- `samples/expressions/numeric_literals/decimal_out_of_range_error.axm`
- `samples/expressions/arithmetic/decimal_overflow_error.axm`
- `docs/SPECIFICATION.md`
- `docs/LANGUAGE_GUIDE.md`
- `docs/manual/data-types.html`
- `docs/manual/diagnostics.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- требуется повторно прогнать профильные `dotnet test`-наборы после финальной синхронизации документации и acceptance samples.

### Следующий точечный шаг
- Вручную прогнать новые `.axm`-samples через `Axiom.Ide` и подтвердить, что:
  - `samples/operators/dim/decimal_explicit_declaration.axm` выполняется без diagnostics;
  - `samples/expressions/numeric_literals/decimal_out_of_range_error.axm` даёт `MB2018`;
  - `samples/expressions/arithmetic/decimal_overflow_error.axm` даёт `MB3008`.
- Затем отдельным follow-up решить, нужен ли Axiom явный decimal-suffix или достаточно будущей helper-функции вроде `CDEC(...)`.
## Session 85 - 2026-04-17 22:01 - Math built-in package v1 and numeric built-in metadata layer

### Что сделано
- Обобщён built-in слой для числовых функций:
  - введены `BuiltinFunctionId` и descriptor-based регистрация built-ins;
  - `FunctionSymbol` теперь хранит canonical built-in id, а binder умеет связывать numeric built-ins через общую metadata-driven модель вместо точечных special-case для `INT` и `STR`;
  - runtime-dispatch переведён со string-switch на dispatch по built-in id, а числовые и строковые helper-ы разнесены по отдельным partial-файлам.
- Реализован math-pack первой волны:
  - добавлены `ABS`, `SGN`, `MIN`, `MAX`, `CLAMP`, `SIN`, `COS`, `TAN`, `ATN`, `ATN2`, `SQR`, `CBRT`, `EXP`, `LOG`, `LOG10`, `LOG2`, `POW`, `FLOOR`, `CEIL`, `ROUND`, `FIX`;
  - зафиксированы правила типов: `SGN -> INTEGER`, тригонометрия/логарифмы/степень/корни возвращают `DOUBLE`, `FLOOR`/`CEIL`/`ROUND`/`FIX` сохраняют numeric-family входа, а `INT` остаётся legacy floor в `INTEGER`;
  - domain errors идут через `MB3007`, overflow и не-конечные результаты через `MB3008`.
- Синхронизированы acceptance samples и документация:
  - добавлены runnable samples в `samples/functions/abs/`, `sgn/`, `min/`, `max/`, `clamp/`, `sin/`, `cos/`, `tan/`, `atn/`, `atn2/`, `sqr/`, `cbrt/`, `exp/`, `log/`, `log10/`, `log2/`, `pow/`, `floor/`, `ceil/`, `round/`, `fix/`;
  - добавлены focused error-samples `samples/functions/sqr/negative_error.axm`, `samples/functions/log/non_positive_error.axm`, `samples/functions/clamp/invalid_bounds_error.axm`, `samples/functions/pow/invalid_domain_error.axm`;
  - обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, manual pages по числовым функциям и справочнику, а также syntax highlighting в IDE.

### Что решено
- Для Axiom закреплена гибридная модель именования built-ins:
  - исторические BASIC-имена сохраняются там, где они естественны (`INT`, `SGN`, `SQR`, `ATN`);
  - новые функции добавляются короткими uppercase-именами в том же стиле (`ATN2`, `CBRT`, `CLAMP`, `LOG2`, `CEIL`, `ROUND`, `FIX`).
- `FIX` введён как отдельная функция для усечения к нулю, чтобы не ломать историческое поведение `INT`.
- `MIN` / `MAX` / `CLAMP` наследуют текущую numeric-promotion модель языка и по-прежнему не разрешают автоматическое смешивание `DECIMAL` и `DOUBLE`.

### Touched paths
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Numeric.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `samples/functions/abs/basic_usage.axm`
- `samples/functions/sgn/basic_usage.axm`
- `samples/functions/min/basic_usage.axm`
- `samples/functions/max/basic_usage.axm`
- `samples/functions/clamp/basic_usage.axm`
- `samples/functions/clamp/invalid_bounds_error.axm`
- `samples/functions/sin/basic_usage.axm`
- `samples/functions/cos/basic_usage.axm`
- `samples/functions/tan/basic_usage.axm`
- `samples/functions/atn/basic_usage.axm`
- `samples/functions/atn2/basic_usage.axm`
- `samples/functions/sqr/basic_usage.axm`
- `samples/functions/sqr/negative_error.axm`
- `samples/functions/cbrt/basic_usage.axm`
- `samples/functions/exp/basic_usage.axm`
- `samples/functions/log/basic_usage.axm`
- `samples/functions/log/non_positive_error.axm`
- `samples/functions/log10/basic_usage.axm`
- `samples/functions/log2/basic_usage.axm`
- `samples/functions/pow/basic_usage.axm`
- `samples/functions/pow/invalid_domain_error.axm`
- `samples/functions/floor/basic_usage.axm`
- `samples/functions/ceil/basic_usage.axm`
- `samples/functions/round/basic_usage.axm`
- `samples/functions/fix/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/numeric-functions.html`
- `docs/manual/reference.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test Axiom.sln --no-restore -v minimal` — успешно.

### Следующий точечный шаг
- Вручную открыть в `Axiom.Ide` и прогнать минимум:
  - `samples/functions/abs/basic_usage.axm`;
  - `samples/functions/sqr/basic_usage.axm`;
  - `samples/functions/log/basic_usage.axm`;
  - `samples/functions/clamp/basic_usage.axm`;
  - `samples/functions/round/basic_usage.axm`;
  - `samples/functions/fix/basic_usage.axm`;
- Отдельно убедиться, что `INT` по-прежнему отличается от `FIX` на отрицательных дробях, а error-samples для `SQR`, `LOG`, `CLAMP` и `POW` показывают ожидаемые runtime diagnostics.

## Session 86 - 2026-04-17 22:35 - Automated math manual-check runner

### Что сделано
- Добавлен отдельный repo-side runner для автоматической проверки math-pack:
  - создан проект `src/Axiom.ManualChecks` поверх `Axiom.Execution`;
  - runner прогоняет все реализованные математические built-ins первой волны через существующие `.axm`-samples;
  - дополнительно проверяет runtime negative-scenarios (`SQR`, `LOG`, `CLAMP`, `POW`) и несколько inline semantic negative-cases для wrong arity/type.
- Реализован автоматический отчёт:
  - runner пишет подробный текстовый отчёт во временный `.txt`-файл;
  - путь к файлу печатается в консоль;
  - поддержан явный override пути через `-- --out <path>`.
- Tooling подключён к репозиторию:
  - проект добавлен в `Axiom.sln`;
  - `README.md` дополнен короткой инструкцией по запуску runner-а.

### Что решено
- Для ручной проверки math milestone выбран не отдельный IDE-only чек-лист, а гибридный подход:
  - сначала быстрый автоматический runner на execution pipeline;
  - затем при необходимости точечный IDE smoke-pass уже по конкретным спорным кейсам.
- Runner сравнивает не только статусы и diagnostic codes, но и точные ожидаемые output-строки, чтобы отчёт сразу показывал реальный PASS/FAIL по контракту.

### Touched paths
- `src/Axiom.ManualChecks/Axiom.ManualChecks.csproj`
- `src/Axiom.ManualChecks/Program.cs`
- `Axiom.sln`
- `README.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj` — успешно, `Passed: 30`, `Failed: 0`;
- `dotnet test Axiom.sln --no-restore -v minimal` — успешно.

### Следующий точечный шаг
- Дать пользователю команду запуска runner-а и попросить прислать содержимое сгенерированного отчёта;
- после подтверждения перейти либо к исправлению найденных расхождений, либо к проектированию второй волны scientific built-ins.

## Session 87 - 2026-04-17 23:00 - Scientific math built-ins second wave

### Что сделано
- Реализована вторая волна математических built-ins:
  - добавлены `ASIN`, `ACOS`, `ASINH`, `ACOSH`, `ATANH`, `SINH`, `COSH`, `TANH`, `SETSIGN` в metadata-driven built-in layer;
  - расширены runtime helpers и domain checks для новых функций;
  - syntax highlighting пополнен новыми именами built-ins.
- Добавлен runnable acceptance-контур для новых функций:
  - созданы `samples/functions/asin/`, `acos/`, `asinh/`, `acosh/`, `atanh/`, `sinh/`, `cosh/`, `tanh/`, `setsign/`;
  - добавлены focused negative samples для `ASIN`, `ACOS`, `ACOSH`, `ATANH`;
  - console-runner `src/Axiom.ManualChecks/Program.cs` расширен до полного прогона обеих волн math-pack.
- Синхронизирована документация:
  - обновлены `docs/SPECIFICATION.md`, `docs/manual/numeric-functions.html`, `docs/manual/scripts/manual.js`, `docs/PLANS.md`.

### Что решено
- Для второй волны закреплён контракт:
  - `ASIN` и `ACOS` принимают только значения из диапазона `[-1, 1]`;
  - `ACOSH` требует `x >= 1`;
  - `ATANH` требует `-1 < x < 1`;
  - `SETSIGN(value, signFrom)` переносит знак второго аргумента на первый и сохраняет numeric-family первого аргумента.
- Для user-facing samples и runner chosen output сделан устойчивым через `ROUND(... * 1000)` там, где прямой вывод длинных `DOUBLE` не нужен для acceptance-смысла.

### Touched paths
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Numeric.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/functions/asin/basic_usage.axm`
- `samples/functions/asin/out_of_range_error.axm`
- `samples/functions/acos/basic_usage.axm`
- `samples/functions/acos/out_of_range_error.axm`
- `samples/functions/asinh/basic_usage.axm`
- `samples/functions/acosh/basic_usage.axm`
- `samples/functions/acosh/less_than_one_error.axm`
- `samples/functions/atanh/basic_usage.axm`
- `samples/functions/atanh/out_of_range_error.axm`
- `samples/functions/sinh/basic_usage.axm`
- `samples/functions/cosh/basic_usage.axm`
- `samples/functions/tanh/basic_usage.axm`
- `samples/functions/setsign/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/numeric-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- `dotnet test Axiom.sln --no-restore -v minimal` — успешно.
- `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj` — успешно, `Passed: 44`, `Failed: 0`.

### Следующий точечный шаг
- Решить, нужен ли follow-up по `CDEC(...)` или другому явному пути создания `DECIMAL`;
- затем отдельно принять продуктовое решение, остаются ли IEEE/tuple-style math helpers вне языка или получают собственный advanced-numeric milestone.

## Session 88 - 2026-04-18 09:22 - Repo-local documentation skill and manual-only baseline

### Что сделано
- Создан новый repo-local skill `axiom-doc-author` в `.codex/skills/axiom-doc-author/`:
  - добавлен `SKILL.md` с двумя явными режимами `reference` и `getting-started`;
  - добавлен `agents/openai.yaml` для локального отображения skill;
  - добавлен пакет редакционных материалов в `references/` по стилю, шаблонам страниц, навигации manual и локальным сравнениям с классическим BASIC и C#.
- В skill зафиксирован новый документационный контракт:
  - порядок источников истины `docs/SPECIFICATION.md` -> код -> `samples/` -> текущий `docs/manual/` -> исторические Microsoft-мануалы только как стиль и структура;
  - обязательный preflight по репозиторным документам, релевантным manual-страницам, коду и samples;
  - жёсткий запрет на выдумывание неподтверждённых свойств языка;
  - обязательный gap-reporting при расхождении `SPECIFICATION`, кода, samples и manual;
  - примеры только в IDE-friendly стиле `Print`, `If`, `Function`;
  - запрет на пустые секции.
- Из живой документационной ветки удалён `docs/LANGUAGE_GUIDE.md`.
- `README.md` и `docs/PLANS.md` синхронизированы под manual-only baseline:
  - `docs/manual/` оставлен единственным поддерживаемым человекочитаемым форматом;
  - прекращена предпосылка, что `LANGUAGE_GUIDE.md` всё ещё поддерживается как параллельная ветка;
  - в план явно добавлена обязанность следующих редакторских проходов улучшать навигацию manual, включая breadcrumbs и `See Also`.

### Что решено
- Для Axiom больше не поддерживаются две человекочитаемые ветки документации одновременно: дальнейшее развитие идёт только через `docs/manual/`.
- Исторические Microsoft PDF используются только как редакционный ориентир по жанру и структуре, а не как источник фактов о Axiom.
- Навигационная модель manual считается незавершённой, пока breadcrumbs и системный `See Also` не внедрены последовательно; это больше не скрытый quality issue, а явное требование будущих редакторских проходов.

### Touched paths
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/agents/openai.yaml`
- `.codex/skills/axiom-doc-author/references/microsoft-style-guide.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `.codex/skills/axiom-doc-author/references/getting-started-template.md`
- `.codex/skills/axiom-doc-author/references/manual-navigation-rules.md`
- `.codex/skills/axiom-doc-author/references/contextual-differences.md`
- `README.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `docs/LANGUAGE_GUIDE.md` (удалён)

### Проверка
- `python C:\Users\comp\.codex\skills\.system\skill-creator\scripts\quick_validate.py C:\Axiom\.codex\skills\axiom-doc-author`
- ручная сверка `agents/openai.yaml` с `SKILL.md`;
- ручная проверка, что `docs/LANGUAGE_GUIDE.md` больше не существует;
- ручная проверка `README.md` и нового append-only обновления в `docs/PLANS.md`.

### Следующий точечный шаг
- Использовать `axiom-doc-author` на первом целевом manual-pass:
  - довязать ближайшие справочные страницы к runnable `samples/`;
  - проверить, где сейчас нужнее всего breadcrumbs и `See Also`;
  - при первом же обнаруженном расхождении между manual, кодом и `SPECIFICATION` оформить отдельный documentation gap вместо молчаливой редакторской подгонки.

## Session 89 - 2026-04-18 12:25 - First built-ins manual pass through axiom-doc-author

### Что сделано
- Выполнен первый целевой manual-pass через repo-local skill `axiom-doc-author`:
  - обновлены `docs/manual/reference.html`, `docs/manual/string-functions.html` и `docs/manual/numeric-functions.html`;
  - на страницах добавлены breadcrumbs, блоки `См. также` и явные переходы к runnable `samples/functions/...`;
  - описания приведены к более официальной русской подаче без устаревших формулировок про "первую волну" math-pack там, где страница теперь должна описывать уже текущий подтвержденный baseline.
- Навигационная модель manual усилена на уровне оформления:
  - в `docs/manual/styles/manual.css` добавлены стили для breadcrumbs, resource-list и link-list;
  - страницы built-ins теперь можно использовать не только как справочник, но и как точку входа в ручную проверку через IDE.
- Строковый кластер дополнительно синхронизирован со style contract skill:
  - code-примеры переведены в IDE-friendly формат `Print ...`;
  - в явном виде зафиксированы пограничные случаи для `LEFT`, `RIGHT` и `MID`, включая runtime diagnostic `MB3007`.

### Что решено
- Для справочных manual-страниц по функциям закреплен практический baseline:
  - страница должна вести читателя не только к соседним статьям, но и к runnable acceptance-samples;
  - breadcrumbs и `См. также` больше не считаются необязательным оформлением, если страница входит в плотный reference-кластер.
- В этом проходе не обнаружено расхождений между `docs/SPECIFICATION.md`, кодом и `samples/` по строковым и числовым built-ins:
  - значит, правки были редакционными и навигационными, а не corrective по контракту языка.

### Touched paths
- `docs/manual/styles/manual.css`
- `docs/manual/reference.html`
- `docs/manual/string-functions.html`
- `docs/manual/numeric-functions.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Ручная сверка `docs/SPECIFICATION.md`, runtime/semantics кода и `samples/functions/...` для строковых и числовых built-ins.
- Проверка существования всех новых sample-links из `reference.html`, `string-functions.html` и `numeric-functions.html` через `Test-Path` — все ссылки резолвятся успешно.

### Следующий точечный шаг
- Продолжить тот же skill-driven reference-pass на `docs/manual/variables-and-constants.html` и страницах по вводу/выводу;
- отдельно привести к тому же editorial baseline catalog metadata и описания в `docs/manual/scripts/manual.js`, если там сохраняются англоязычные или устаревшие формулировки.

## Session 90 - 2026-04-18 12:51 - Declarations and I/O manual pass through axiom-doc-author

### Что сделано
- Выполнен следующий skill-driven manual-pass на соседнем reference-кластере:
  - обновлены `docs/manual/variables-and-constants.html`, `docs/manual/print.html` и `docs/manual/input.html`;
  - на всех трёх страницах добавлены breadcrumbs, блоки `См. также` и разделы ручной проверки с прямыми ссылками на runnable `samples/operators/...`;
  - тексты переведены из более учебной подачи в более справочную и официальную, без смешения русского текста с англоязычными редакционными хвостами.
- Каталог manual синхронизирован с обновлёнными страницами:
  - в `docs/manual/scripts/manual.js` выровнены описания по `ABS`, `ACOSH`, `ASINH`, `ATANH`, `CEIL`, `COSH`, `FLOOR`, `IDENTIFIER`, `ROUND`, `SETSIGN`, `SINH`, `TANH`;
  - убраны mixed-style формулировки вроде `numeric-family`, `inverse hyperbolic`, `reserved words` и похожие англоязычные вкрапления.
- Контракт skill дополнительно закреплён на уровне практики:
  - для `DIM`, `CONST`, `PRINT` и `INPUT` manual теперь не только объясняет синтаксис, но и ведёт к конкретным acceptance-samples;
  - примеры в тексте приведены к IDE-friendly записи `Dim`, `Print`, `Input`, `Const`.

### Что решено
- Для деклараций и ввода/вывода подтверждён тот же editorial baseline, что и для built-ins:
  - справочная глава должна иметь breadcrumbs, `См. также` и runnable sample-links;
  - catalogue metadata в `manual.js` должна совпадать по тону и фактам с самими статьями manual.
- В этом проходе не обнаружено расхождений между `docs/SPECIFICATION.md`, manual и sample-программами по `DIM`, `CONST`, `PRINT` и `INPUT`:
  - значит, текущий manual-pass был навигационным и редакционным, а не corrective по языковому контракту.

### Touched paths
- `docs/manual/variables-and-constants.html`
- `docs/manual/print.html`
- `docs/manual/input.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Ручная сверка `docs/SPECIFICATION.md` с manual-страницами и sample-программами по `DIM`, `CONST`, `PRINT`, `INPUT`.
- Проверка существования всех новых sample-links из `variables-and-constants.html`, `print.html` и `input.html` через `Test-Path` — все ссылки резолвятся успешно.
- Поиск остаточных mixed-style формулировок в затронутых manual-файлах и `docs/manual/scripts/manual.js` — явных совпадений не осталось.

### Следующий точечный шаг
- Перейти к обзорным manual-страницам, начиная с `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/expressions-and-branching.html`;
- проверить, не дублируют ли они уже существующие reference-главы без полезной навигационной роли, и при необходимости перевести их в более связующую, index-like подачу.

## Session 91 - 2026-04-18 12:57 - Overview hub pages wired into manual navigation

### Что сделано
- Выполнен следующий skill-driven manual-pass на обзорных и связующих страницах:
  - обновлены `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html`, `docs/manual/expressions-and-branching.html`;
  - на всех трёх страницах добавлены breadcrumbs, блоки `См. также`, объяснение роли обзорной главы и короткие наборы representative samples для быстрого IDE smoke-check.
- Обзорные страницы перестроены под связующую роль:
  - вместо полу-дублирования справочных глав они теперь объясняют, когда открывать страницу, какие статьи читать дальше и какими sample-файлами быстро подтвердить соседние темы;
  - для `data-and-input` связаны типы данных, объявления, `INPUT`, `PRINT`;
  - для `arrays-and-builtins` связаны массивы, строковые функции и числовые функции;
  - для `expressions-and-branching` связаны арифметика, логика, `IF` и `SELECT CASE`.
- Глобальная навигация manual синхронизирована с новым назначением страниц:
  - в `docs/manual/scripts/manual.js` добавлен отдельный блок `Обзорные маршруты`;
  - обзорные страницы теперь видны в левом меню и попадают в pager-order, вместо того чтобы оставаться слабо обнаружимыми файлами.

### Что решено
- Для overview-страниц закреплена отдельная роль внутри manual:
  - они не должны пересказывать справочник, а должны связывать подробные главы через контекст, переходы и representative samples;
  - если обзорная страница не участвует в общей навигационной модели, это считается information-architecture gap, а не допустимой мелочью оформления.
- В этом проходе не обнаружено link-расхождений по новым sample-ссылкам:
  - все representative sample-links на обновлённых обзорных страницах успешно резолвятся в существующие файлы под `samples/`.

### Touched paths
- `docs/manual/data-and-input.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Проверка существования всех новых sample-links из `data-and-input.html`, `arrays-and-builtins.html` и `expressions-and-branching.html` через `Test-Path` — все ссылки резолвятся успешно.
- Поиск остаточных англоязычных editorial-хвостов в обновлённых обзорных страницах и `docs/manual/scripts/manual.js` — новых совпадений по целевым паттернам не осталось.

### Следующий точечный шаг
- Проверить `docs/manual/index.html`, `docs/manual/getting-started.html` и при необходимости `docs/manual/cheat-sheet.html` на тот же editorial baseline;
- затем решить, нужен ли отдельный follow-up по breadcrumbs и `См. также` для более старых справочных страниц вроде `if.html`, `select-case.html`, `arrays.html`, `arithmetic-and-logic.html`.

## Session 92 - 2026-04-18 13:03 - Entry-point manual pages aligned with skill baseline

### Что сделано
- Выполнен следующий skill-driven pass на верхнем слое manual:
  - обновлены `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/cheat-sheet.html` и `docs/manual/examples.html`;
  - `index.html` теперь яснее разделяет учебный маршрут, обзорные хабы и короткий список runnable sample-файлов для первого запуска;
  - `getting-started.html` дополнена breadcrumbs, блоком показательных sample-файлов и более точной стартовой инструкцией без возврата к разговорной подаче;
  - `cheat-sheet.html` приведена к IDE-friendly форме записи примеров и встроена в ту же навигационную модель;
  - `examples.html` получила breadcrumbs, явный раздел о том, когда открывать страницу, ссылки на runnable `samples/...` и выровненные code-примеры в стиле `Dim` / `Print` / `Function`.
- Entry-point слой manual теперь лучше согласован по роли:
  - вводные страницы не только объясняют тему, но и ведут к существующим runnable `.axm`-файлам;
  - `examples.html` перестала быть изолированной страницей с набросками и стала нормальной точкой входа к практическим сценариям.

### Что решено
- Для верхнего слоя manual закреплён тот же editorial baseline, что и для reference- и overview-страниц:
  - breadcrumbs, `См. также` и sample-driven переходы считаются обязательной частью entry-point страниц, если они участвуют в маршруте чтения;
  - UI-метки IDE вроде `Run` и `Output` остаются допустимыми только как точные названия элементов интерфейса, а не как редакционный язык manual.
- В этом проходе не выявлено новых factual gaps между `docs/SPECIFICATION.md`, manual и доступными sample-файлами:
  - все добавленные ссылки на `.axm`-примеры с `index.html`, `getting-started.html` и `examples.html` успешно резолвятся в существующие файлы.

### Touched paths
- `docs/manual/index.html`
- `docs/manual/getting-started.html`
- `docs/manual/cheat-sheet.html`
- `docs/manual/examples.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Поиск остаточных mixed-style формулировок по целевым паттернам в `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/cheat-sheet.html` и `docs/manual/examples.html`.
- Проверка существования всех новых sample-links через `Test-Path` для `index.html`, `getting-started.html` и `examples.html` — все ссылки резолвятся успешно.

### Следующий точечный шаг
- Выполнить follow-up на более старых справочных страницах `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html` и `docs/manual/arithmetic-and-logic.html`;
- довести их до того же navigation baseline: breadcrumbs, `См. также`, runnable `samples/` и выровненный русский справочный тон.

## Session 93 - 2026-04-18 13:07 - Older reference pages aligned and arithmetic gap corrected

### Что сделано
- Выполнен следующий skill-driven manual-pass на более старых справочных страницах:
  - обновлены `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html` и `docs/manual/arithmetic-and-logic.html`;
  - на всех четырёх страницах добавлены breadcrumbs, блоки `См. также` и runnable sample-links в тематических подкаталогах `samples/control-flow/...`, `samples/expressions/...`, `samples/operators/...`;
  - встроенные code-примеры приведены к IDE-friendly записи `If`, `Select Case`, `Dim`, `Print`, `End If`, `End Select`.
- В reference-кластере уточнены подтверждённые правила языка:
  - `if.html` теперь явнее связывает ветвление с логическим условием, блочной областью видимости и negative-sample для не-логического условия;
  - `select-case.html` теперь лучше описывает точные значения, диапазоны, `CASE IS`, строковые сравнения и ограничение `BOOLEAN` до точных проверок;
  - `arrays.html` перестала ссылаться на устаревший baseline-язык и теперь описывает одномерные массивы как текущий поддерживаемый контракт;
  - `arithmetic-and-logic.html` дополнена sample-набором, диагностикой и более точным описанием сравнений и логики.

### Что решено
- В этом проходе обнаружен и сразу исправлен реальный documentation gap:
  - старая версия `docs/manual/arithmetic-and-logic.html` утверждала, что оператор `/` всегда возвращает `DOUBLE`;
  - после сверки с `docs/SPECIFICATION.md` и тестовым контуром manual приведён к фактическому контракту: `/` возвращает `DECIMAL`, если участвует `DECIMAL` и не участвует `DOUBLE`, иначе `DOUBLE`.
- Для этого кластера закреплён тот же baseline, что и для остальных справочных страниц:
  - старая страница считается незавершённой, если на ней нет breadcrumbs, `См. также` и runnable sample-переходов;
  - factual mismatch между старым manual и текущим контрактом языка должен фиксироваться явно, а не скрываться под редакторским переписыванием текста.

### Touched paths
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/arrays.html`
- `docs/manual/arithmetic-and-logic.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Сверка `docs/SPECIFICATION.md` с manual-страницами по разделам `IF`, `SELECT CASE`, массивам, арифметике, сравнениям и логике.
- Проверка существования всех новых sample-links через `Test-Path` — все ссылки из `if.html`, `select-case.html`, `arrays.html` и `arithmetic-and-logic.html` резолвятся успешно.
- Поиск остаточных mixed-style формулировок по целевым паттернам в затронутом reference-кластере после правок.

### Следующий точечный шаг
- Продолжить follow-up на старых справочных страницах циклов и подпрограмм: `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`, `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/return.html`;
- проверить, где там ещё не хватает breadcrumbs, `См. также`, runnable `samples/` и явных диагностических блоков.

## Session 94 - 2026-04-18 13:24 - Loop and routine reference pages aligned with current contract

### Что сделано
- Выполнен следующий skill-driven manual-pass на старых справочных страницах циклов и подпрограмм:
  - обновлены `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`, `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/return.html`;
  - страницы переведены в единый reference-формат с breadcrumbs, разделами `Сводка`, `Синтаксис`, `Замечания`, `Примеры`, `Диагностика`, `См. также` и sample-driven блоками ручной проверки;
  - code-примеры выровнены под IDE-friendly стиль `For`, `While`, `Do`, `Sub`, `Function`, `Return`, `Print`, `Input`.
- Каталог manual синхронизирован с этим проходом:
  - в `docs/manual/scripts/manual.js` обновлены описания записей `DO ... LOOP`, `FOR`, `FUNCTION`, `RETURN`, `SUB`, `WHILE`, чтобы каталог не отставал по тону и фактам от самих manual-страниц.

### Что решено
- В этом проходе найден и сразу устранен реальный documentation gap:
  - старая версия `docs/manual/for.html` ограничивала управляющую переменную `FOR` типами `INTEGER` и `DOUBLE`;
  - после сверки с `docs/SPECIFICATION.md`, binder/runtime и tests manual приведен к фактическому контракту: поддерживаются `INTEGER`, `LONG` и `DOUBLE`.
- Для reference-кластера процедур и функций закреплен единый фактологический baseline:
  - `SUB` документируется как подпрограмма без возвращаемого значения;
  - `FUNCTION` документируется как конструкция с обязательным `RETURN expression`;
  - `RETURN` описывается как самостоятельный оператор с разной семантикой в `SUB` и `FUNCTION`, включая `MB2006`, `MB2007`, `MB2012` и runtime `MB3006`.

### Touched paths
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Сверка `docs/SPECIFICATION.md`, relevant code (`Parser`, `Binder`, `Interpreter`) и tests по `FOR`, `WHILE`, `DO ... LOOP`, `SUB`, `FUNCTION`, `RETURN`.
- Проверка существования всех новых sample-links через `Test-Path` — все ссылки из обновленных страниц резолвятся успешно.
- Поиск остаточных mixed-style хвостов по ключевым шаблонам (`routine`, `pre-condition`, `post-condition`, `control variable`, `голый`) в затронутом кластере после правок.

### Следующий точечный шаг
- Выполнить такой же follow-up для оставшихся старых reference-страниц верхнего уровня `docs/manual/data-types.html` и `docs/manual/diagnostics.html`;
- затем проверить, не требуют ли `docs/manual/reference.html` и `docs/manual/scripts/manual.js` дополнительного выравнивания описаний под тот же reference-контракт.

## Session 95 - 2026-04-18 14:04 - Classic BASIC casing restored for new manual passes

### Что сделано
- Зафиксирован новый локальный documentation baseline в repo-local skill `axiom-doc-author`:
  - `SKILL.md` теперь требует для syntax-snippets и встроенных code-примеров стиль классического BASIC с uppercase-ключевыми словами, типами, логическими литералами и built-in вызовами;
  - reference-template в `references/reference-page-template.md` синхронизирован с этим же правилом и больше не показывает title-case пример вида `Print "Hello"`.
- Выполнен следующий skill-driven reference-pass уже под новым casing-правилом:
  - `docs/manual/data-types.html` переведена в единый reference-формат с breadcrumbs, разделами `Сводка`, `Синтаксис`, `Замечания`, `Примеры`, `Диагностика`, `См. также` и sample-driven блоком ручной проверки;
  - встроенные code-примеры на этой странице оформлены в classic-uppercase стиле (`DIM`, `PRINT`, `IF`, `END IF`, `VAL`, `STR`) без возврата к mixed-case подаче;
  - factual-only подача выровнена по `docs/SPECIFICATION.md`, acceptance samples и тестовому контуру без неподтвержденных утверждений про внутреннее runtime-представление типов.

### Что решено
- Пользовательский редакционный разворот оформлен как новое правило rollout, а не как немедленный массовый откат:
  - начиная с этой сессии новые manual-pass выполняются только в classic-uppercase стиле;
  - уже переписанные ранее страницы с title-case примерами не исправляются по одной в каждом проходе, а переносятся в отдельный финальный retro-sweep после завершения основного цикла обновления manual.
- Для `data-types.html` дополнительно устранены старые editorial-проблемы:
  - прежняя версия страницы была ближе к свободной обзорной статье и содержала mixed-style prose без явных breadcrumbs, `См. также` и отдельного диагностического блока;
  - новая версия закрепляет только подтвержденный контракт по шести базовым типам, выводу типа в `DIM`, разрешенным автоматическим преобразованиям и кодам `MB0004`, `MB2018`, `MB3008`.

### Touched paths
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `docs/manual/data-types.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Сверка `docs/SPECIFICATION.md` по разделам литералов, типов данных, `DIM`, преобразований типов и формата диагностики.
- Сверка acceptance-тестов и sample-набора по `DIM`, numeric literal overflow, `DECIMAL`, `STR`, `VAL` и runtime numeric overflow.
- Ручная проверка того, что все sample-links, добавленные на `docs/manual/data-types.html`, указывают на существующие `.axm`-файлы из `samples/operators/dim/`, `samples/expressions/` и `samples/functions/`.

### Следующий точечный шаг
- Выполнить такой же reference-pass для `docs/manual/diagnostics.html` уже в classic-uppercase baseline;
- после закрытия оставшихся manual-страниц отдельным финальным проходом выполнить mass retro-sweep по ранее переписанным title-case страницам.

## Session 96 - 2026-04-18 14:14 - Diagnostics page rewritten in plain Russian reference style

### Что сделано
- Дополнительно уточнен редакционный стандарт локального skill `axiom-doc-author`:
  - заголовок предварительной проверки, разделы шаблонов и основные рабочие формулировки переведены на естественный русский язык;
  - из правил и шаблонов убраны слова вроде `preflight`, `runnable`, `IDE-friendly`, `change set`, `documentation baseline` и аналогичные англицизмы там, где им есть нормальная русская замена;
  - reference- и getting-started-шаблоны теперь используют русские названия секций.
- Выполнен следующий справочный проход по `docs/manual/diagnostics.html`:
  - старая обзорно-пояснительная страница заменена на единый справочный формат с breadcrumbs, разделами `Сводка`, `Формат сообщения`, `Замечания`, `Примеры`, `Диагностика`, `См. также`;
  - на страницу добавлены подтвержденные правила чтения кода ошибки, схема префиксов `MB0xxx` ... `MB3xxx`, таблицы часто встречающихся кодов и блок ручной проверки с отрицательными примерами из `samples/`;
  - встроенные примеры переведены в классическую запись заглавными буквами.

### Что решено
- Пользовательское требование про нормальный преподавательский русский закреплено не как разовая правка одной страницы, а как новое правило дальнейших редакторских проходов:
  - внутренние инструкции skill теперь сами подталкивают к русским формулировкам;
  - новая `diagnostics.html` не использует слова вроде `baseline`, `workflow`, `runtime` и `built-in` в авторском тексте страницы.
- Для диагностической главы зафиксирован подтвержденный набор кодов, который уже опирается на спецификацию, тесты и существующие тематические страницы:
  - ошибки чтения и разбора: `MB0004`;
  - смысловые ошибки: `MB2001`, `MB2004`, `MB2006`, `MB2007`, `MB2008`, `MB2009`, `MB2010`, `MB2012`, `MB2013`, `MB2016`, `MB2017`, `MB2018`;
  - ошибки во время выполнения: `MB3001`, `MB3002`, `MB3003`, `MB3004`, `MB3006`, `MB3007`, `MB3008`.

### Touched paths
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `.codex/skills/axiom-doc-author/references/getting-started-template.md`
- `docs/manual/diagnostics.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Сверка `docs/SPECIFICATION.md` по разделам `Ошибки`, `Формат диагностики`, `INPUT`, числовой модели и встроенных функций с ограничениями области значений.
- Сверка automated tests по кодам `MB0004`, `MB2001`, `MB2004`, `MB2006`, `MB2007`, `MB2008`, `MB2009`, `MB2010`, `MB2013`, `MB2016`, `MB2017`, `MB2018`, `MB3001`, `MB3002`, `MB3003`, `MB3004`, `MB3007`, `MB3008`.
- Ручная проверка того, что все новые ссылки на отрицательные `.axm`-примеры в `docs/manual/diagnostics.html` указывают на существующие файлы.

### Следующий точечный шаг
- Продолжить новые manual-проходы уже в том же русском редакционном стандарте и проверить, где рядом с `diagnostics.html` ещё остались англицизмы в обзорных и справочных главах;
- после завершения основного цикла отдельно выполнить финальный массовый проход по старым title-case страницам и заодно дочистить там англицизмы.

## Session 97 - 2026-04-18 14:16 - Redundant Axiom wording removed from nearby page titles

### Что сделано
- В `axiom-doc-author` закреплено отдельное правило заголовков:
  - если страница уже находится внутри `Axiom Manual`, название языка не нужно повторять в каждом `h1` и служебной подписи;
  - предпочтительным видом считаются краткие заголовки вроде `Оператор PRINT`, `Оператор INPUT`, `Типы данных`, `Диагностика`.
- Это правило сразу применено к ближайшим уже затронутым справочным страницам:
  - на `docs/manual/print.html` и `docs/manual/input.html` убрано лишнее `в Axiom` из заголовков;
  - на `docs/manual/data-types.html`, `docs/manual/diagnostics.html`, `docs/manual/variables-and-constants.html` и `docs/manual/arithmetic-and-logic.html` сокращены заголовки и связанные подписи;
  - в `variables-and-constants.html` заодно сокращены внутренняя ссылка и подзаголовок `Описание переменной`.

### Что решено
- Пользовательское замечание оформлено как общее редакционное правило, а не как частный вкус к двум страницам:
  - повтор названия языка теперь считается лишним, если контекст manual уже и так очевиден;
  - сохранение имени `Axiom` остается допустимым только там, где без него теряется смысл, например в общем названии руководства или при сравнении с другим языком.

### Touched paths
- `.codex/skills/axiom-doc-author/SKILL.md`
- `docs/manual/print.html`
- `docs/manual/input.html`
- `docs/manual/data-types.html`
- `docs/manual/diagnostics.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/arithmetic-and-logic.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Поиск по ближайшему справочному кластеру на шаблоны заголовков с лишним `в Axiom`.
- Ручная проверка обновленных `h1`, `topbar__meta` и внутренних ссылок на затронутых страницах.

### Следующий точечный шаг
- Продолжить тот же редакторский проход по соседним страницам и при каждой новой правке убирать лишнее повторение названия языка;
- в финальном массовом проходе дочистить оставшиеся страницы manual, где такой шаблон ещё сохранится.

## Session 98 - 2026-04-18 14:23 - Entry pages cleaned up for concise Russian wording and classic BASIC examples

### Что сделано
- Продолжен редакторский проход по соседним обзорным и сводным страницам manual:
  - обновлена `docs/manual/getting-started.html`: сокращены `topbar__meta` и `h1`, переведены в uppercase примеры с `PRINT` и `REM`, а формулировки сделаны чуть короче и проще;
  - обновлена `docs/manual/loops.html`: добавлены breadcrumbs, убрано лишнее `в Axiom` и переписано вступление в более спокойной справочной манере;
  - обновлена `docs/manual/string-functions.html`: сокращен `h1`, переведены в uppercase примеры по `LEN`, `LEFT`, `RIGHT`, `MID`, `LCASE`, `UCASE`, а формулировка про `MID` теперь объясняет нумерацию с единицы без англицизма;
  - обновлена `docs/manual/cheat-sheet.html`: сокращен `h1`, а основные syntax-snippets по `DIM`, `CONST`, `PRINT`, `INPUT`, `IF`, `SELECT CASE`, циклам, подпрограммам и массивам переведены в классическую uppercase-запись.

### Что решено
- Для обзорных и вводных страниц закрепляется тот же краткий принцип заголовков, что и для справочного кластера: если контекст языка уже очевиден, не нужно повторять `Axiom` в `h1` и короткой служебной подписи.
- Для текстов вводного уровня и коротких сводок закрепляется тот же языковой принцип: при возможности говорить по-русски просто, а не заменять это выражениями вроде `manual`, `baseline` или `1-based`.

### Touched paths
- `docs/manual/getting-started.html`
- `docs/manual/loops.html`
- `docs/manual/string-functions.html`
- `docs/manual/cheat-sheet.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Поиск по обновленному кластеру на остатки `в Axiom`, `Print`, `If`, `For`, `While`, `Do`, `Loop`, `Input`, `Function`, `Return`, `LCase`, `UCase` в code-примерах.
- Ручная проверка того, что в `loops.html` появились breadcrumbs, а в `string-functions.html` и `cheat-sheet.html` примеры теперь оформлены в классическом BASIC-стиле.

### Следующий точечный шаг
- Продолжить тот же проход на `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/examples.html`, чтобы выровнять язык обзорных и переходных страниц;
- после завершения основного manual-цикла вернуться к отдельному финальному массовому проходу по ранее переписанным title-case страницам.

## Session 99 - 2026-04-18 14:38 - Remaining overview cluster rewritten in plain Russian

### Что сделано
- Продолжен редакторский проход по оставшемуся обзорному кластеру manual:
  - обновлены `docs/manual/data-and-input.html` и `docs/manual/arrays-and-builtins.html`;
  - сокращены вступительные формулировки, уточнены маршруты чтения, а подписи к ссылкам и спискам примеров переписаны в более ровном русском стиле;
  - на `data-and-input.html` блок примеров выровнен по объявлениям и вводу/выводу, а на `arrays-and-builtins.html` уточнена роль строковых и числовых функций как отдельных справочных групп.
- Переписана `docs/manual/examples.html`:
  - все встроенные примеры переведены в классическую BASIC-запись заглавными буквами;
  - текст страницы сделан короче и спокойнее, без лишней разговорности;
  - в примере с массивом и циклом зафиксирован и исправлен реальный documentation gap: счетчик `i` теперь явно объявляется через `DIM i AS INTEGER`, поскольку неявное объявление переменных в Axiom не поддерживается.
- Обновлен `docs/manual/scripts/manual.js`:
  - пункт навигации для `examples.html` переименован в `Примеры программ`;
  - в указателе убраны еще два явных англицизма: `Legacy floor` для `INT` и `1-based` в описании `MID`.

### Что решено
- Для обзорных и практических страниц закреплено то же правило, что и для справочного кластера:
  - проза должна звучать как учебное объяснение, а не как внутренняя заметка о workflow и baseline;
  - встроенный код в manual должен быть в classic-uppercase тоне и не возвращаться к title-case записи.
- Исправление примера с `FOR` закрепляет важную норму дальнейших проходов:
  - даже на учебных страницах нельзя оставлять примеры, которые зависят от неявных правил, отсутствующих в языке;
  - если manual и implementation расходятся даже в мелком кодовом фрагменте, это нужно считать пробелом в описании и исправлять явно.

### Touched paths
- `docs/manual/data-and-input.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/examples.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Ручная сверка с `docs/SPECIFICATION.md` по правилу обязательного явного объявления переменных перед использованием.
- Ручная проверка того, что новые и обновленные ссылки на `.axm`-файлы в трех страницах указывают на существующие sample-файлы.
- Поиск по обновленному кластеру на остатки title-case BASIC-записи внутри code-блоков.

### Следующий точечный шаг
- Довести оставшиеся связующие страницы, прежде всего `docs/manual/procedures-and-functions.html`, и при необходимости дочистить `docs/manual/index.html`;
- после этого перейти к отдельному финальному массовому проходу по ранее переписанным title-case страницам.

## Session 100 - 2026-04-18 15:03 - Procedures and functions overview tied into the manual route

### Что сделано
- Завершён следующий связующий редакторский проход по manual:
  - полностью переписана `docs/manual/procedures-and-functions.html` как обзорная глава с breadcrumbs, коротким маршрутом чтения, карточками перехода к `SUB`, `FUNCTION` и `RETURN`, примером в классической BASIC-записи и блоком runnable sample-links;
  - обновлена `docs/manual/index.html`: добавлен обзорный маршрут по процедурам и функциям, курс больше не ведёт в этот кластер только через разрозненные ссылки, а формулировки на главной стали короче и ровнее по-русски;
  - в `docs/manual/scripts/manual.js` добавлен пункт навигации `procedures-and-functions`, чтобы новая обзорная глава входила в левое меню и в postраничный маршрут manual.

### Что решено
- Основной цикл выравнивания обзорных и связующих страниц manual на этом этапе считается закрытым:
  - оставшаяся крупная работа теперь уже не про новые обзорные главы, а про завершающую массовую чистку ранее переписанных страниц;
  - этот финальный проход должен одновременно вернуть классическую uppercase-запись на старые title-case примеры и убрать оставшиеся англицизмы из уже обновлённых справочных страниц.
- Для информационной архитектуры manual закреплена ещё одна норма:
  - обзорная глава не должна оставаться вне навигации; если страница нужна как связующий узел, она должна сразу быть видна в левом меню и входить в общий маршрут чтения.

### Touched paths
- `docs/manual/procedures-and-functions.html`
- `docs/manual/index.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Ручная проверка новых и обновлённых ссылок на `.axm`-файлы в `procedures-and-functions.html`; все четыре пути существуют.
- Поиск по `docs/manual/procedures-and-functions.html` и `docs/manual/index.html` на остатки `routine`, `print-list`, `prompt-`, `floor`, `хаб` и шаблон `Готовые программы`; совпадений не осталось.
- Проверка того, что `docs/manual/scripts/manual.js` теперь содержит пункт `procedures-and-functions`.

### Следующий точечный шаг
- Перейти к финальному массовому проходу по ранее переписанным страницам, где ещё остались title-case примеры и накопившиеся англицизмы.
- Начать этот завершающий проход со страниц `sub.html`, `function.html`, `return.html`, `print.html`, `input.html`, страниц циклов и ранее обновлённых страниц по встроенным функциям, чтобы выровнять весь manual под одну классическую BASIC-запись.

## Session 101 - 2026-04-18 15:17 - First final cleanup pass returned uppercase BASIC to procedures, I/O, and loops

### Что сделано
- Выполнен первый возвратный проход по ранее переписанным справочным страницам:
  - в `docs/manual/sub.html`, `docs/manual/function.html` и `docs/manual/return.html` синтаксис, примеры и короткие пояснения приведены к классической BASIC-записи заглавными буквами;
  - в `docs/manual/print.html` и `docs/manual/input.html` кодовые блоки и видимые формулировки очищены от старой mixed-case записи и служебных англоязычных выражений;
  - в `docs/manual/for.html`, `docs/manual/while.html` и `docs/manual/do-loop.html` цикловые конструкции тоже возвращены к записи `FOR`, `NEXT`, `WHILE`, `DO`, `LOOP`, `EXIT`, `PRINT`, `INPUT`, `DIM`, `AS`.

### Что решено
- Для завершающей редакторской чистки manual закреплён практический критерий готовности:
  - страница считается доведённой только тогда, когда в видимом тексте больше нет случайной title-case записи ключевых слов и лишних англицизмов в пояснениях;
  - имена sample-файлов и пути остаются техническими, но сами учебные примеры и подписи внутри manual должны быть оформлены в классической BASIC-записи и простом русском стиле.

### Touched paths
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/return.html`
- `docs/manual/print.html`
- `docs/manual/input.html`
- `docs/manual/for.html`
- `docs/manual/while.html`
- `docs/manual/do-loop.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Поиск по затронутому кластеру на остатки видимых слов `baseline`, `runtime`, `runnable`, `smoke-check`, `prompt-`, `multi-value` и старой mixed-case записи; после правок остались только технические идентификаторы вроде `print-list` в HTML-якорях.
- Ручная проверка существования sample-ссылок, на которые опираются страницы:
  - `samples/operators/sub/basic_call.axm`
  - `samples/operators/function/basic_definition.axm`
  - `samples/control-flow/return/function_value.axm`
  - `samples/control-flow/return/sub_early_exit.axm`
  - `samples/operators/print/basic_expression.axm`
  - `samples/operators/input/single_variable.axm`
  - `samples/control-flow/for/basic_loop.axm`
  - `samples/control-flow/while/basic_loop.axm`
  - `samples/control-flow/do/do_while_loop.axm`

### Следующий точечный шаг
- Продолжить тот же завершающий проход на страницах `if.html`, `select-case.html`, `arrays.html`, `arithmetic-and-logic.html`, `variables-and-constants.html`, `data-types.html`, `diagnostics.html`, `string-functions.html` и `numeric-functions.html`.
- После этого сделать короткую общую проверку по `docs/manual/`, чтобы подтвердить, что в основном корпусе manual больше не осталось видимых title-case примеров и случайных англицизмов.

## Session 102 - 2026-04-18 15:55 - Branching, declarations, types, and diagnostics aligned to the final manual style

### Что сделано
- Выполнен следующий большой возвратный проход по manual:
  - страницы `docs/manual/if.html` и `docs/manual/select-case.html` переведены на ту же классическую BASIC-запись заглавными буквами в синтаксисе и примерах;
  - страницы `docs/manual/arrays.html`, `docs/manual/arithmetic-and-logic.html`, `docs/manual/variables-and-constants.html`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html` и `docs/manual/numeric-functions.html` очищены от остатков старой mixed-case записи и от видимых неровностей вроде `special-case`, `floor` в пояснительном тексте и формы `не-конечный`;
  - по итогам общей проверки дополнительно дочищены мелкие хвосты в `docs/manual/while.html`, `docs/manual/sub.html`, `docs/manual/function.html` и `docs/manual/cheat-sheet.html`, чтобы не оставлять их на отдельный проход.

### Что решено
- Основной корпус manual почти доведён до единого вида:
  - большие тематические кластеры больше не выглядят как отдельные стилистические островки; теперь главная работа смещается с больших переписываний на итоговую сквозную вычитку;
  - технические имена sample-файлов, названия функций вроде `FLOOR` и служебные идентификаторы остаются как есть, но видимый учебный текст и код внутри manual должны оставаться единообразными по русской подаче и записи ключевых слов.

### Touched paths
- `docs/manual/if.html`
- `docs/manual/select-case.html`
- `docs/manual/arrays.html`
- `docs/manual/arithmetic-and-logic.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/data-types.html`
- `docs/manual/diagnostics.html`
- `docs/manual/numeric-functions.html`
- `docs/manual/while.html`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/cheat-sheet.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Ручная проверка существования опорных sample-ссылок для этого прохода:
  - `samples/control-flow/if/basic_if_else.axm`
  - `samples/control-flow/select_case/exact_values.axm`
  - `samples/operators/dim/array_declaration.axm`
  - `samples/expressions/arithmetic/basic_math.axm`
  - `samples/operators/dim/basic_declaration.axm`
  - `samples/expressions/numeric_literals/decimal_out_of_range_error.axm`
  - `samples/acceptance/negative/if_requires_boolean.axm`
  - `samples/functions/str/basic_usage.axm`
- Общий поиск по `docs/manual/` на остатки `special-case`, `floor` в пояснительном тексте, `не-конечный`, `runtime`, `runnable`, `baseline`, `smoke-check` и похожие хвосты; после правок остались только технические пути, имена функций и образцов, а не видимые редакторские артефакты.

### Следующий точечный шаг
- Выполнить финальную сквозную вычитку manual, включая `index.html`, `getting-started.html`, `examples.html`, `cheat-sheet.html`, `string-functions.html` и подписи в `docs/manual/scripts/manual.js`.
- После этой вычитки зафиксировать, что основной корпус manual приведён к одной классической BASIC-записи и одному спокойному русскому тону.

## Session 103 - 2026-04-18 16:10 - Final editorial pass aligned the manual with the Microsoft-style reference tone

### Что сделано
- Выполнен завершающий редакторский проход по оставшимся вводным и обзорным страницам manual:
  - в `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/examples.html`, `docs/manual/cheat-sheet.html` и `docs/manual/string-functions.html` сокращены вводные формулировки, убраны разговорные обороты и выровнен ритм подачи;
  - в `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html`, `docs/manual/expressions-and-branching.html`, `docs/manual/procedures-and-functions.html`, `docs/manual/reference.html`, `docs/manual/diagnostics.html`, `docs/manual/data-types.html` и `docs/manual/numeric-functions.html` дополнительно дочищены обзорные описания и последние stylistic хвосты;
  - `docs/manual/scripts/manual.js` синхронизирован с этим же тоном в названиях пунктов навигации и кратких описаниях карточек указателя.

### Что решено
- Основной корпус manual теперь можно считать приведённым к одному редакционному стандарту:
  - справочные статьи, обзорные главы, вводные страницы и шпаргалка больше не расходятся по записи ключевых слов, плотности абзацев и тону формулировок;
  - для этой вычитки повторно использованы локальные Microsoft-референсы `Basic Language Reference (1989).pdf`, `Programmers Guide (1989).pdf` и `Getting Started 7.1 (1990).pdf` как стилистический ориентир по порядку подачи и построению предложений, без переноса оттуда фактов о Axiom.

### Touched paths
- `docs/manual/index.html`
- `docs/manual/getting-started.html`
- `docs/manual/examples.html`
- `docs/manual/cheat-sheet.html`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/data-and-input.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/reference.html`
- `docs/manual/diagnostics.html`
- `docs/manual/data-types.html`
- `docs/manual/numeric-functions.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Повторный общий поиск по `docs/manual/` на остатки редакторских хвостов вроде `runtime`, `runnable`, `baseline`, `smoke-check`, `special-case`, `не-конечный` и схожих формулировок; в видимом тексте manual эти хвосты больше не используются.
- Ручная проверка существования опорных sample-файлов, на которые ссылаются вводные и обзорные страницы:
  - `samples/operators/print/basic_expression.axm`
  - `samples/operators/input/single_variable.axm`
  - `samples/operators/dim/basic_declaration.axm`
  - `samples/functions/len/basic_usage.axm`
  - `samples/functions/str/basic_usage.axm`
  - `samples/control-flow/select_case/ranges_and_is.axm`
  - `samples/smoke/v0_1_baseline.axm`

### Следующий точечный шаг
- Считать общий manual-style pass завершённым и дальше поддерживать тот же стандарт точечно при новых изменениях языка и документации.
- При следующем содержательном изменении обновлять только затронутые статьи, навигацию и связанные записи в `docs/PLANS.md` и `docs/WORKLOG.md`.

## Session 104 - 2026-04-18 16:15 - Sidebar navigation gets independent vertical scrolling

### Что сделано
- Исправлена компоновка оболочки manual для desktop-режима:
  - в `docs/manual/styles/manual.css` боковая карточка навигации переведена на колонную компоновку с ограничением по высоте окна;
  - список тем `manual-nav` получил собственную вертикальную прокрутку, чтобы меню можно было листать независимо от основного текста страницы.

### Что решено
- Левое меню на `docs/manual/index.html` и остальных страницах manual снова работает как самостоятельная область навигации:
  - заголовок боковой панели остаётся на месте;
  - длинный список тем больше не требует прокручивать весь документ до нижней части страницы.

### Touched paths
- `docs/manual/styles/manual.css`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Визуальная проверка стилей и структуры показала, что desktop-оболочка manual теперь ограничивает высоту боковой панели и передаёт вертикальную прокрутку именно списку тем.
- Дополнительных кодовых тестов, сборки и lint-прохода не требовалось, потому что изменение затронуло только статический CSS manual.

### Следующий точечный шаг
- При следующих точечных правках manual-layout отдельно проверять поведение боковой навигации на desktop и mobile.

## Session 105 - 2026-04-18 16:19 - Main manual headings renamed to clearer Russian labels

### Что сделано
- На главной странице manual уточнены названия двух крупных блоков:
  - `Обзорные маршруты` заменено на `Обзорные главы`;
  - `Маршрут курса` заменено на `Последовательность изучения`.
- В той же правке на `docs/manual/index.html` скорректированы ближайшие формулировки, чтобы слово `маршрут` не выбивалось из общего учебного тона.
- В `docs/manual/scripts/manual.js` синхронизировано название соответствующего блока бокового меню.

### Что решено
- Главная страница manual теперь использует более естественные и понятные русские заголовки без искусственной метафоры маршрута.
- Терминология главной страницы и левой навигации снова совпадает между собой.

### Touched paths
- `docs/manual/index.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Поиск по `docs/manual/index.html` и `docs/manual/scripts/manual.js` подтвердил, что целевые заголовки заменены и боковое меню использует то же название блока.
- Сборка и тесты не запускались, потому что правка затронула только статический текст manual.

### Следующий точечный шаг
- При следующих редакторских правках manual проверять названия разделов не только на точность, но и на естественность для русского учебного текста.

## Session 106 - 2026-04-18 16:44 - PRINT USING added to the language baseline

### Что сделано
- расширен lexer, syntax, parser, binder и runtime для нового оператора `PRINT USING`;
- добавлен отдельный runtime-форматтер масок `PRINT USING` со строковыми и числовыми полями;
- обновлены parser-, semantic- и runtime-тесты под новый оператор;
- добавлен runnable acceptance sample `samples/operators/print/using_basic.axm` и acceptance-проверка для него;
- синхронизированы `docs/SPECIFICATION.md`, `docs/PLANS.md` и IDE syntax highlighting.

### Что решено
- `PRINT USING` трактуется как специализированная ветка `PRINT` с обязательным `USING formatExpression;`;
- форматирующая строка управляет литералами и масками, а только завершающий `;` или `,` после списка аргументов влияет на перевод строки;
- ключевое слово `USING` считается частью языка и подсвечивается в IDE наравне с остальными reserved words.

### Touched paths
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/InputOutputSyntax.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.PrintUsing.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/print/using_basic.axm`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнены `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj`, `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`, `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`, `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`;
- выполнена общая сборка `dotnet build Axiom.sln`;
- параллельный первый прогон показал только файловые блокировки артефактов сборки, после чего проверка была повторена последовательно и завершилась успешно.

### Следующий точечный шаг
- при следующем развитии `PRINT USING` расширять acceptance-сценарии и manual-справку синхронно с runtime-форматтером, не откладывая это на отдельный проход.

## Session 107 - 2026-04-18 17:00 - PRINT USING documented in the manual

### Что сделано
- страница `docs/manual/print.html` дополнена разделами по `PRINT USING`, строковым полям, числовым маскам, диагностике и ограничениям текущей реализации;
- в ручную проверку на той же странице добавлена ссылка на `samples/operators/print/using_basic.axm`;
- `docs/manual/scripts/manual.js` обновлён так, чтобы `PRINT USING` находился в алфавитном и тематическом указателе manual как отдельный элемент.

### Что решено
- пользовательская справка по `PRINT USING` теперь живёт в manual, а не только в технической спецификации;
- в manual описаны именно те маски и символы, которые реально поддерживаются текущим runtime: `!`, `&`, `\   \`, `#`, `0`, `.`, `+`, `-`, `,`, `$`, `*`, `^`, а также экранирование через `_`;
- справка явно фиксирует текущие ограничения, чтобы manual не обещал больше, чем умеет реализация.

### Touched paths
- `docs/manual/print.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- ручная проверка содержимого `docs/manual/print.html` и `docs/manual/scripts/manual.js` подтвердила, что `PRINT USING` добавлен в справочную главу и в указатель manual;
- кодовые тесты и сборка не запускались повторно, потому что задача затронула только пользовательскую документацию.

### Следующий точечный шаг
- при следующих изменениях форматтера `PRINT USING` обновлять manual одновременно с кодом и acceptance samples, чтобы справка не отставала от языка.

## Session 108 - 2026-04-18 18:11 - RND added as a minimal random math function

### Что сделано
- сравнен внешний файл `C:\projects\Axiom\Visitors\AxiomInterpreter.MathFunctions.cs` с нашей реализацией математических встроенных функций;
- из полезных идей выбран и внедрён минимальный вариант `RND()` как встроенная функция без аргументов, без новых синтаксических конструкций и без усложнения табличной модели привязки;
- `RND()` добавлен в `BuiltinFunctionId`, каталог встроенных функций и диспетчер выполнения во время исполнения;
- добавлены тесты для привязки, выполнения и приёмочного контура, а также запускаемый пример `samples/functions/rnd/basic_usage.axm`;
- консольный проверочный прогон математических функций расширен новым сценарием для `RND()`;
- синхронизированы `docs/SPECIFICATION.md`, `docs/manual/numeric-functions.html`, `docs/PLANS.md` и `docs/WORKLOG.md`.

### Что решено
- Axiom-подход с большим `if`-разветвлением и смешанным контрактом необязательных аргументов переносить в Axiom не нужно: наша табличная архитектура сильнее и лучше соответствует текущим правилам репозитория;
- в текущей версии языка `RND()` намеренно остаётся простой функцией без аргументов и возвращает `DOUBLE` в диапазоне `[0, 1)`;
- seed/reset-семантика и QuickBASIC-подобные специальные формы вроде повторного последнего значения осознанно не вводятся без отдельного документированного решения.

### Touched paths
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Numeric.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `src/Axiom.ManualChecks/Program.cs`
- `samples/functions/rnd/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/numeric-functions.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнены `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj`, `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj`, `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`, `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj` и `dotnet build Axiom.sln`;
- все проверки завершились успешно;
- запускаемый пример для ручной проверки добавлен в `samples/functions/rnd/`.

### Следующий точечный шаг
- держать `RND()` как минимальный вариант случайной функции и, если позже понадобится более подробный контракт, сначала зафиксировать его в спецификации и acceptance samples, а не вводить ad-hoc поведение сразу в runtime.

## Session 109 - 2026-04-18 18:28 - RND added to the manual index

### Что сделано
- исправлен недостающий шаг по manual для `RND()`: функция уже была описана в `docs/manual/numeric-functions.html`, но отсутствовала как отдельная запись в указателе;
- в `docs/manual/scripts/manual.js` добавлена отдельная карточка `RND` с переходом на `numeric-functions.html#rnd`;
- синхронизированы `docs/PLANS.md` и `docs/WORKLOG.md`.

### Что решено
- для manual этого проекта недостаточно только упомянуть встроенную функцию внутри большой статьи;
- если функция должна находиться через алфавитный и тематический указатель, запись в `docs/manual/scripts/manual.js` считается обязательной частью той же задачи.

### Touched paths
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- ручная проверка `docs/manual/scripts/manual.js` подтвердила наличие отдельной записи `RND` с переходом на `numeric-functions.html#rnd`;
- кодовые тесты и сборка не запускались повторно, потому что эта правка затронула только слой указателя manual.

### Следующий точечный шаг
- при следующих добавлениях встроенных функций обновлять статью manual и указатель в одном наборе правок, чтобы новые функции сразу находились и по странице, и через указатель.

## Session 110 - 2026-04-18 18:28 - RND wording cleaned up after anglicism remark

### Что сделано
- по замечанию пользователя в тексте manual для `RND()` заменена фраза `В текущем baseline...` на нормальную русскую формулировку;
- в свежих проектных записях по `RND()` заменены мои новые слова `baseline` и `stochastic` на русские эквиваленты там, где смысл не теряется.

### Что решено
- для новых текстов по проекту нельзя оправдываться внутренней технической привычкой: если мысль можно сказать по-русски, нужно писать по-русски;
- это замечание относится не только к страницам manual, но и к новым записям в `PLANS` и `WORKLOG`, когда они пишутся для людей, а не для кода.

### Touched paths
- `docs/manual/numeric-functions.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- ручная проверка обновлённых строк подтвердила, что в свежем тексте про `RND()` больше не используется слово `baseline`;
- кодовые тесты и сборка не запускались, потому что правка затронула только формулировки в документации.

### Следующий точечный шаг
- при следующих правках сначала проверять формулировку на простую русскую замену и только потом оставлять англоязычный термин, если без него действительно нельзя.

## Session 111 - 2026-04-18 18:28 - Documentation rules tightened after missing manual follow-up

### Что сделано
- проверен repo-local skill `axiom-doc-author`;
- подтверждено, что запрет на ненужные англицизмы в skill уже был, но теперь он отдельно распространён и на новые поясняющие записи в `docs/PLANS.md` и `docs/WORKLOG.md`;
- в skill добавлено прямое правило: после новой функции, оператора или изменения их публичного поведения manual должен обновляться в том же наборе правок;
- то же требование отдельно зафиксировано в `AGENTS.md` и `README.md`, чтобы оно было видно не только в skill, но и в общих правилах репозитория.

### Что решено
- отсутствие пользовательской справки после добавления функции или оператора теперь закреплено как нарушение рабочего процесса, а не как необязательный follow-up;
- обновление только кода, тестов, `samples/` и технической спецификации без синхронного обновления `docs/manual/` больше не считается достаточным завершением такой задачи;
- если новая сущность должна находиться через указатель manual, обновление `docs/manual/scripts/manual.js` тоже считается обязательной частью той же работы.

### Touched paths
- `.codex/skills/axiom-doc-author/SKILL.md`
- `AGENTS.md`
- `README.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнена ручная проверка обновлённых правил в skill и в репозиторных документах;
- кодовые тесты и сборка не запускались, потому что задача затронула только правила работы и документацию процесса.

### Следующий точечный шаг
- при следующем изменении языка сразу применять это правило на практике: код, тесты, `.axm`-пример, manual-страница и запись в указателе должны обновляться в одном наборе правок.
## Session 112 - 2026-04-18 18:45 - Procedure model review against Axiom

### Что сделано
- Прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`.
- Изучен внешний файл `C:\projects\Axiom\Visitors\AxiomInterpreter.Procedures.cs` и сопоставлен с текущей реализацией процедур и функций в `Axiom.Syntax`, `Axiom.Semantics` и `Axiom.Runtime`.
- В `docs/PLANS.md` зафиксированы выводы по тому, какие идеи из Axiom стоит перенять концептуально, а какие не подходят текущей архитектуре Axiom.

### Что решено
- Сильные стороны Axiom по теме процедур: лексический реестр вложенных процедур и функций, двухпроходная регистрация локальных `SUB`/`FUNCTION` перед выполнением тела и более насыщенный контекст вызова для диагностик.
- Эти идеи нельзя копировать буквально: у нас уже лучшее разделение слоёв, bound-модель и runtime signal для `RETURN`, поэтому монолитный visitor, строковая типизация и исключения как нормальный control-flow переносить не нужно.
- Если мы будем развивать процедуры дальше, самым полезным направлением будет отдельная метамодель объявлений процедур и функций и более богатая диагностика цепочки вызовов; поддержка вложенных процедур и функций с захватом внешних локалов должна обсуждаться как отдельное языковое изменение, а не как “автоматическое улучшение”.

### Touched paths
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- Код, тесты и сборка не запускались, потому что задача была исследовательской и не вносила изменений в язык или runtime.
- Сравнение выполнено вручную по коду Axiom и текущим файлам Axiom, связанным с parser/binder/runtime процедур.

### Следующий точечный шаг
- Если следующая задача затронет процедуры или функции, начать с документирования контракта локальных `SUB`/`FUNCTION`, лексической области видимости их имён и расширенного контекста вызова для диагностик, а уже затем переходить к реализации.

## Session 113 - 2026-04-18 19:00 - Local nested SUB and FUNCTION

### Что сделано
- прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждён переход от обзора к реализации локальных процедур и функций;
- в syntax/parsing добавлены локальные операторы объявления для `SUB` и `FUNCTION`, разрешённые только в прямом теле внешней процедуры или функции;
- binder переведён на раздельные цепочки видимости для переменных и имён процедур и функций, чтобы вложенные процедуры и функции видели лексически внешние имена процедур и функций, но не получали доступ к внешним локальным переменным;
- реализована двухпроходная модель для тела процедуры или функции: сначала регистрация локальных `SUB`/`FUNCTION`, затем связывание их тел и только потом связывание исполняемых инструкций;
- добавлены тесты parser/semantics/runtime/execution и runnable `.axm`-примеры для локальной подпрограммы и локальной функции;
- обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и manual-страницы `SUB`, `FUNCTION`, `RETURN`, обзор процедур и индекс manual.

### Что решено
- на этом этапе локальные `SUB`/`FUNCTION` поддерживаются без захвата внешних локальных переменных и параметров;
- вызов локальной процедуры или функции до её текстового объявления поддержан намеренно и закреплён тестами;
- локальные процедуры и функции остаются лексически локальными: снаружи внешней процедуры или функции их имена недоступны.

### Touched paths
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Scopes/BoundScope.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/sub/local_nested_call.axm`
- `samples/operators/function/local_nested_call.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/return.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`;
- подтверждено прохождение всех тестовых проектов, включая новые parser/semantics/runtime/execution проверки;
- acceptance samples `samples/operators/sub/local_nested_call.axm` и `samples/operators/function/local_nested_call.axm` добавлены и покрыты автоматическими execution tests.

### Следующий точечный шаг
- следующим отдельным этапом спроектировать поддержку захвата внешних локальных переменных для вложенных `SUB`/`FUNCTION`, включая изменение semantics и runtime lifetime модели.

## Session 114 - 2026-04-18 19:14 - Лексический захват для вложенных процедур и функций

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждён следующий незавершённый шаг по захвату внешних локальных переменных;
- `FunctionSymbol` расширен ссылкой на объявляющую внешнюю процедуру или функцию, чтобы во время выполнения можно было различать глобальные и локальные вызываемые символы;
- binder для тела процедуры или функции перестроен на текстовый порядок после предварительной регистрации локальных имён процедур и функций: это сохранило вызов до объявления, но дало корректный захват только уже объявленных внешних локальных имён;
- вызов локальной процедуры или функции во время выполнения переведён на создание `RuntimeScope` поверх активной области видимости объявляющей процедуры или функции, благодаря чему вложенные `SUB`/`FUNCTION` читают и изменяют захваченные внешние переменные и параметры;
- добавлены и обновлены тесты смысловой проверки, времени выполнения и сквозного выполнения, а также запускаемые примеры `samples/operators/sub/capture_outer_local.axm` и `samples/operators/function/capture_outer_local.axm`;
- синхронизированы `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, справочные страницы `SUB`, `FUNCTION`, обзор процедур и указатель справки.

### Что решено
- вложенные процедуры и функции теперь поддерживают лексический захват внешних параметров и локальных переменных без превращения их в значения первого класса;
- захват намеренно ограничен уже объявленными по тексту именами: объявления ниже локальной процедуры или функции остаются недоступны;
- вызов локальной процедуры или функции по-прежнему возможен до её текстового объявления, потому что регистрация локальных имён процедур и функций осталась отдельным первым проходом.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/sub/capture_outer_local.axm`
- `samples/operators/function/capture_outer_local.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/sub.html`
- `docs/manual/function.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнены `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`;
- выполнены `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore`;
- выполнены `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore`.

### Следующий точечный шаг
- следующим отдельным этапом улучшить диагностику времени выполнения для вложенных вызовов: показывать объявляющую процедуру или функцию и цепочку внешних вызовов в сообщениях об ошибках.

## Session 115 - 2026-04-18 19:39 - Диагностика времени выполнения для вложенных вызовов

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждён следующий незавершённый шаг по улучшению диагностик времени выполнения;
- `CallFrame` расширен хранением точки вызова, а вызов пользовательской процедуры или функции переведён на передачу этой точки в кадр вызова;
- диагностика времени выполнения сведена к общему помощнику, который дописывает в текст сообщения цепочку активных вызовов и для локальной процедуры или функции показывает внешнюю процедуру или функцию, внутри которой она объявлена;
- добавлены тесты времени выполнения и сквозного выполнения на ошибку внутри локальной функции, а также запускаемый пример `samples/smoke/nested_runtime_diagnostic.axm`;
- обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/manual/diagnostics.html` и `docs/PLANS.md`.

### Что решено
- координаты ошибки времени выполнения должны оставаться у реального проблемного выражения, а не переноситься на строку вызова;
- цепочка вызовов добавляется как текстовое расширение существующего сообщения диагностики, без изменения модели `Diagnostic`;
- для текущего этапа этого достаточно: задача про понятные сообщения времени выполнения закрыта без введения функций как значений языка.

### Затронутые пути
- `src/Axiom.Runtime/Environment/CallFrame.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.PrintUsing.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/smoke/nested_runtime_diagnostic.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/diagnostics.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнены `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore`;
- выполнены `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore`.

### Следующий точечный шаг
- если продолжать развитие процедур и функций, отдельно определить в спецификации, нужны ли процедуры и функции как значения языка и какая модель полноценных замыканий нам нужна.

## Session 124 - 2026-04-19 08:35 - Первый этап строковых функций

### Что сделано
- перечитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждено, что следующий языковой этап логично посвятить расширению строковых функций;
- в `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md` зафиксирован первый пакет строковых функций и правило, что встроенный каталог может иметь несколько форм вызова у одного имени без общей перегрузки пользовательских процедур и функций;
- `Axiom.Semantics` перестроен на тематический каталог built-ins: строковые функции, trim-группа, заполняющие строковые функции, преобразования и числовые функции разнесены по отдельным partial-файлам;
- binder научен выбирать нужную встроенную функцию по имени, числу аргументов и их типам, что позволило добавить `TRIM(text)`, `TRIM(text, chars)`, `LTRIM`, `RTRIM`, `REPEAT`, `SPACE`, `STRING`;
- runtime-разделение строковых built-ins расширено отдельными файлами для trim-операций, заполнения строк и вспомогательной логики;
- разбор выражений уточнён так, чтобы вызов `STRING(...)` корректно читался как вызов встроенной функции, даже при существующем ключевом слове типа `STRING`;
- добавлены новые binder/runtime tests, runnable `.axm` примеры и обновлена пользовательская справка `docs/manual/string-functions.html` вместе с индексом `docs/manual/scripts/manual.js`;
- подтверждён полный зелёный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Что решено
- имя `STRING` как встроенная функция в нашем языке допустимо, но для этого парсер должен принимать имена встроенных типов в выражении как потенциальное имя вызываемого значения;
- поддержку нескольких сигнатур сейчас получает только встроенный каталог; пользовательские `SUB` и `FUNCTION` по-прежнему остаются без общей перегрузки;
- первый этап строковых функций закрыт полностью вместе с тестами, примерами для IDE и пользовательской справкой.

### Затронутые пути
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Trim.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Fill.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Helpers.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/trim/`
- `samples/functions/ltrim/`
- `samples/functions/rtrim/`
- `samples/functions/reverse/`
- `samples/functions/repeat/`
- `samples/functions/space/`
- `samples/functions/string/`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- следующим отдельным этапом спроектировать и внедрить вторую волну строковых функций: проверку вхождения, начала и конца строки, замену фрагментов и, при согласованной модели, поиск позиции внутри строки.

## Session 118 - 2026-04-19 06:38 - Навык документации переведён на полный Microsoft-подобный manual-pass

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждено, что следующая документная задача касается уже не отдельных страниц, а общего стандарта manual;
- найден и пересмотрен repo-local навык `axiom-doc-author`, а также его справочные материалы и шаблоны;
- по локальным PDF-референсам Microsoft QuickBasic выделены редакционные паттерны, которые нужно воспроизводить в Axiom: короткая строка назначения в начале страницы, отдельный раздел `Синтаксис`, последующая расшифровка частей конструкции, затем замечания и только потом примеры;
- в `axiom-doc-author` добавлены прямые правила полного manual-pass по всему `docs/manual/`, жёсткие требования к разделу `Синтаксис`, запрет двусмысленных шаблонов вроде `TYPE`, `NAME`, `STATEMENTS` и требование явно расшифровывать метапеременные;
- обновлены вспомогательные reference-файлы навыка и его `agents/openai.yaml`, чтобы новый стандарт применялся не только к `SUB` и `FUNCTION`, а ко всем операторам, функциям, обзорным и вводным страницам manual.

### Что решено
- Microsoft-референсы используются как обязательный редакционный эталон структуры и подачи, но не как источник фактов о поведении Axiom;
- для Axiom по умолчанию предпочтительнее метапеременные вида `<имя>`, `<выражение>`, `<тип>`, чем двусмысленные заглушки, которые можно принять за ключевые слова языка;
- крупные страницы manual вроде `PRINT` или обзорных глав тоже должны начинаться с компактного справочного блока, а не сразу с длинного свободного объяснения.

### Затронутые пути
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/agents/openai.yaml`
- `.codex/skills/axiom-doc-author/references/microsoft-style-guide.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `.codex/skills/axiom-doc-author/references/getting-started-template.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен запуск `python C:\Users\comp\.codex\skills\.system\skill-creator\scripts\quick_validate.py C:\Axiom\.codex\skills\axiom-doc-author`, который сначала потребовал установку `PyYAML`, а затем был подготовлен к повторному запуску в UTF-8 режиме из-за системной кодировки Python на машине;
- дополнительно вручную перечитаны обновлённые файлы навыка после правок.

### Следующий точечный шаг
- следующим отдельным этапом пройти существующие страницы `docs/manual/` по операторам, функциям и обзорным разделам и привести их к новому единому справочному стандарту.

## Session 119 - 2026-04-19 06:48 - В навык документации добавлена нотация необязательных частей синтаксиса

### Что сделано
- по дополнительному замечанию уточнён редакционный контракт `axiom-doc-author`: необязательные части формального синтаксиса теперь должны явно помечаться квадратными скобками;
- в навык добавлены примеры вроде `FOR <счётчик> = <начало> TO <конец> [STEP <шаг>]` и `PRINT [<список вывода>] [;|,]`, чтобы это правило не оставалось только общим пожеланием;
- соответствующее уточнение внесено и в `microsoft-style-guide.md`, и в шаблон `reference`-страницы.

### Что решено
- наличие или отсутствие необязательной части должно быть видно уже в разделе `Синтаксис`, а не только объясняться словами в `Замечаниях`;
- если опущенная часть меняет поведение конструкции, это должно быть дополнительно пояснено в разделе `Части синтаксиса` или `Замечания`.

### Затронутые пути
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/references/microsoft-style-guide.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `python C:\Users\comp\.codex\skills\.system\skill-creator\scripts\quick_validate.py C:\Axiom\.codex\skills\axiom-doc-author`;
- вручную проверено, что правило квадратных скобок и примеры формального синтаксиса присутствуют в обновлённом `SKILL.md`.

### Следующий точечный шаг
- при полном manual-pass привести существующие страницы операторов и функций к новой нотации и отдельно проверить все необязательные части синтаксиса по страницам `FOR`, `PRINT`, `INPUT`, `FUNCTION`, `SUB`, `RETURN`, `IF`, `SELECT CASE`, `DO LOOP`, `WHILE`.

## Session 120 - 2026-04-19 06:52 - Уточнён синтаксис FOR/NEXT в навыке документации

### Что сделано
- по дополнительной проверке с `docs/SPECIFICATION.md` и `docs/manual/for.html` подтверждено, что в Axiom после `NEXT` имя счётчика не допускается;
- из формальных примеров навыка убрана неподдерживаемая схема `NEXT <счётчик>`;
- в `axiom-doc-author` добавлено прямое правило: не переносить даже привычные Microsoft-формы синтаксиса без проверки по реальному синтаксису Axiom.

### Что решено
- Microsoft-подобная подача остаётся редакционным стандартом, но конкретные формы синтаксиса всегда должны проверяться по спецификации языка;
- для `FOR ... NEXT` в текущем Axiom подтверждённая форма записи конца цикла — только `NEXT`.

### Затронутые пути
- `.codex/skills/axiom-doc-author/SKILL.md`
- `.codex/skills/axiom-doc-author/references/reference-page-template.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `python C:\Users\comp\.codex\skills\.system\skill-creator\scripts\quick_validate.py C:\Axiom\.codex\skills\axiom-doc-author`;
- вручную подтверждено по `docs/manual/for.html`, что страница уже фиксирует правило "после NEXT не указывается имя счетчика".

### Следующий точечный шаг
- при полном manual-pass проверять каждую формальную запись оператора или функции по `docs/SPECIFICATION.md` и по существующей подтверждённой manual-странице, прежде чем переносить в неё Microsoft-подобную форму подачи.

## Session 121 - 2026-04-19 07:00 - Ключевые страницы manual приведены к фактическому синтаксису

### Что сделано
- выполнен целевой синтаксический manual-pass по страницам `docs/manual/function.html`, `sub.html`, `for.html`, `if.html`, `do-loop.html`, `while.html`, `return.html`, `data-types.html`, `variables-and-constants.html`, `cheat-sheet.html`, `arrays.html`;
- в этих страницах формальные записи приведены к согласованной нотации: метапеременные показываются в угловых скобках, необязательные части — в квадратных, а реальные примеры отделены от формального синтаксиса;
- убраны ошибочные или двусмысленные формы вроде `FUNCTION Name(param1 AS TYPE, ...)`, `SUB Name(...)`, `RETURN expression`, `name = expression`, `name(index)`, а также англоязычные заглушки `statements`, `condition`, `startExpr`, `endExpr`, `stepExpr`;
- для страниц, где формальный синтаксис использует метапеременные, добавлены или уточнены разделы `Части синтаксиса`.

### Что решено
- стиль Microsoft используется как дисциплина справочной подачи, но каждая формальная запись проверяется по реальному синтаксису Axiom;
- `FOR ... NEXT` в manual закреплён только в подтверждённой форме `NEXT`, без имени счётчика;
- `RETURN` в справке теперь различает именно формы `RETURN <выражение>` и `RETURN`, а не абстрактное `RETURN expression`.

### Затронутые пути
- `docs/manual/function.html`
- `docs/manual/sub.html`
- `docs/manual/for.html`
- `docs/manual/if.html`
- `docs/manual/do-loop.html`
- `docs/manual/while.html`
- `docs/manual/return.html`
- `docs/manual/data-types.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/cheat-sheet.html`
- `docs/manual/arrays.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен контрольный поиск по `docs/manual/`, который больше не находит старые шаблоны `AS TYPE`, `RETURN expression`, `statements`, `condition`, `startExpr`, `endExpr`, `stepExpr`, `name(index)` в справочных синтаксических блоках;
- повторно выполнен `python C:\Users\comp\.codex\skills\.system\skill-creator\scripts\quick_validate.py C:\Axiom\.codex\skills\axiom-doc-author`.

### Следующий точечный шаг
- следующим отдельным этапом довести до того же стандарта обзорные manual-страницы, чтобы структура подачи была единообразной не только в атомарной справке, но и в переходных разделах manual.

## Session 122 - 2026-04-19 07:06 - Обзорные страницы manual выровнены по структуре

### Что сделано
- выполнен следующий слой manual-pass по обзорным страницам `docs/manual/procedures-and-functions.html`, `expressions-and-branching.html`, `loops.html`, `arrays-and-builtins.html`, `reference.html`;
- в этих главах добавлены явные разделы `Содержание`, `Сводка` и маршрутные блоки перехода, чтобы обзорные страницы читались как короткие навигационные главы, а не как свободные заметки;
- `reference.html` структурирован как страница-каталог с внутренними якорями, а обзорные главы по разделам теперь лучше согласованы с уже обновлёнными атомарными reference-страницами.

### Что решено
- обзорные страницы и атомарная справка должны различаться по роли, но не по редакционной дисциплине: и там, и там читатель должен быстро видеть назначение страницы и следующий маршрут чтения;
- краткая обзорная страница не должна дублировать подробную справку, но обязана иметь предсказуемую внутреннюю структуру.

### Затронутые пути
- `docs/manual/procedures-and-functions.html`
- `docs/manual/expressions-and-branching.html`
- `docs/manual/loops.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/reference.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- вручную подтверждено наличие новых разделов `Содержание`, `Сводка` и маршрутных блоков в обновлённых обзорных страницах;
- дополнительный обзорный поиск показал, что следующий слой выравнивания остаётся у страниц `data-and-input.html` и `examples.html`.

### Следующий точечный шаг
- следующим отдельным этапом довести до того же шаблона `data-and-input.html`, `examples.html` и при необходимости `index.html`, чтобы все обзорные manual-страницы стали единообразными по структуре.

## Session 123 - 2026-04-19 07:12 - Закрыт оставшийся manual-pass по домашней и обзорным страницам

### Что сделано
- доведены до того же редакционного шаблона страницы `docs/manual/data-and-input.html`, `docs/manual/examples.html` и `docs/manual/index.html`;
- в `data-and-input.html` добавлены явные разделы `Содержание`, `Сводка`, `Как читать раздел` и якоря для тематических переходов;
- в `examples.html` появились содержание, сводка, явный вход в подборку программ и отдельный блок о том, как с этими примерами работать;
- домашняя страница `index.html` теперь тоже имеет ту же внутреннюю логику: `Содержание`, `Сводка`, обзорные главы, маршрут старта и первые запускаемые примеры.

### Что решено
- manual должен быть единообразным не только в точном синтаксисе справочных статей, но и в обзорных и домашней страницах;
- внутренняя структура `Содержание -> Сводка -> маршрут/каталог -> примеры -> См. также` подошла не только атомарной справке, но и переходным страницам.

### Затронутые пути
- `docs/manual/data-and-input.html`
- `docs/manual/examples.html`
- `docs/manual/index.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- вручную проверено, что на всех трёх страницах появились явные блоки `Содержание` и стабильные внутренние якоря;
- выполнен контрольный поиск по `docs/manual/`, который показывает, что все ключевые обзорные страницы теперь содержат раздел `Содержание`.

### Следующий точечный шаг
- следующим отдельным этапом выполнить финальный сквозной просмотр `docs/manual/` на единообразие формулировок и коротких вступлений.

## Session 117 - 2026-04-18 22:47 - Функции как значения и полноценные замыкания

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждён следующий незавершённый шаг: реализовать функции как значения языка и полноценный захват внешних локальных данных;
- в синтаксис и разбор добавлены функциональные типы вида `FUNCTION(...) AS ...`, а вызов переведён на модель вызова произвольного целевого выражения, а не только имени функции;
- в связывателе добавлены значения функций, структурное сравнение функциональных типов, диагностика попытки использовать `SUB` как значение и диагностика вызова невызываемого значения;
- во время выполнения введено значение функции с захваченной внешней областью видимости, поэтому возвращённая локальная функция продолжает видеть и изменять внешние локальные переменные после завершения внешней функции;
- добавлены и обновлены автоматические проверки в `tests/Axiom.Parsing.Tests`, `tests/Axiom.Semantics.Tests`, `tests/Axiom.Runtime.Tests`, `tests/Axiom.Execution.Tests`, а также запускаемые примеры `samples/operators/function/first_class_value.axm`, `samples/operators/function/returned_closure.axm` и `samples/operators/function/shared_closure_state.axm`;
- обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, страницы пользовательского руководства по `FUNCTION`, `SUB`, `RETURN` и общему разделу о процедурах и функциях, а также индекс руководства.

### Что решено
- значением языка становятся только `FUNCTION`; подпрограммы `SUB` по-прежнему можно вызывать, но нельзя присваивать, передавать или возвращать как значение;
- функциональные типы требуют явного описания типов параметров и результата и могут использоваться в объявлениях переменных, параметров и результатов функций;
- замыкание захватывает живую внешнюю область видимости объявляющей функции, поэтому несколько вызовов одной и той же возвращённой функции работают с одним и тем же сохранённым состоянием.

### Затронутые пути
- `src/Axiom.Syntax/SyntaxNode.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/ProgramAndMembers.cs`
- `src/Axiom.Syntax/Expressions.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Environment/RuntimeScope.cs`
- `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/function/first_class_value.axm`
- `samples/operators/function/returned_closure.axm`
- `samples/operators/function/shared_closure_state.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/function.html`
- `docs/manual/procedures-and-functions.html`
- `docs/manual/return.html`
- `docs/manual/sub.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet build C:\Axiom\Axiom.sln --no-restore`;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore`;
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- если продолжать развитие этой линии, отдельным этапом спроектировать и внедрить анонимные функции, не смешивая эту работу с уже завершённой поддержкой именованных функций как значений и замыканий.

## Session 116 - 2026-04-18 19:50 - Редакционная чистка документации и полный прогон тестов

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждено, что после реализации цепочки вызовов осталось довести формулировки документации до русского редакционного стандарта;
- в свежих разделах `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md` и `docs/WORKLOG.md` убраны смешанные английские формулировки вроде `Update`, `runtime diagnostics`, `execution tests`, `manual` и похожих редакторских хвостов там, где для них есть нормальная русская замена;
- подтверждён полный зелёный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore` после завершения этих правок.

### Что решено
- кодовые имена сущностей вроде `CallFrame`, `RuntimeScope` и `Diagnostic` остаются только как технические идентификаторы, а поясняющий текст вокруг них должен оставаться русским;
- следующий шаг по развитию языка не меняется: текущая задача закрыта редакционно и проверочными прогонами, без расширения семантики процедур и функций.

### Затронутые пути
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- если продолжать развитие процедур и функций, отдельно определить в спецификации, нужны ли процедуры и функции как значения языка и какая модель полноценных замыканий нам нужна.

## Session 125 - 2026-04-19 08:52 - Второй этап строковых функций

### Что сделано
- добавлены встроенные функции `CONTAINS`, `STARTSWITH`, `ENDSWITH` и `REPLACE`;
- для новых функций заведены отдельные описания в каталоге встроенных функций и отдельная часть выполнения строкового поиска и замены;
- добавлены проверки семантического анализа и выполнения;
- добавлены запускаемые примеры в `samples/functions/contains/`, `samples/functions/startswith/`, `samples/functions/endswith/` и `samples/functions/replace/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/string-functions.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`.

### Что решено
- строковые проверки и замена используют строгое сравнение по кодам символов, с учётом регистра;
- пустой `fragment` для `CONTAINS`, `STARTSWITH` и `ENDSWITH` считается найденным;
- пустой `oldText` для `REPLACE` даёт диагностику времени выполнения `MB3007`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Search.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Search.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/contains/basic_usage.axm`
- `samples/functions/startswith/basic_usage.axm`
- `samples/functions/endswith/basic_usage.axm`
- `samples/functions/replace/basic_usage.axm`
- `samples/functions/replace/empty_old_text_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- согласовать и реализовать функции позиционного поиска в строке отдельным этапом.

## Session 126 - 2026-04-19 09:04 - Позиционный поиск по строке

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и подтверждён следующий шаг: реализовать позиционный поиск в строке;
- добавлена встроенная функция `INSTR` в формах `INSTR(text, fragment)` и `INSTR(start, text, fragment)`;
- `INSTR` возвращает позицию с нумерацией от 1, возвращает `0` при отсутствии совпадения и использует строгое сравнение по кодам символов с учётом регистра;
- для `start < 1` добавлена диагностика времени выполнения `MB3007`, а пустой фрагмент документирован и проверен отдельным примером;
- обновлены автоматические проверки, запускаемые `.axm`-примеры, спецификация, справочная страница строковых функций, указатель руководства и текущий план.

### Что решено
- прямой позиционный поиск закрыт через `INSTR`, без добавления отдельного ключевого слова и без изменения разбора выражений;
- обратный поиск и подсчёт вхождений не смешиваются с этим этапом и остаются возможным следующим малым шагом.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Search.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Search.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/instr/basic_usage.axm`
- `samples/functions/instr/empty_fragment.axm`
- `samples/functions/instr/invalid_start_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- обсудить и при необходимости реализовать следующий малый шаг строкового поиска: обратный поиск от конца строки и подсчёт непересекающихся вхождений.

## Session 127 - 2026-04-19 09:21 - Обратный поиск и подсчёт вхождений

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, а также рабочие навыки `compiler-front-end` и `axiom-doc-author`;
- добавлены встроенные строковые функции `INSTRREV(text, fragment)` и `SUBSTRCOUNT(text, fragment)`;
- `INSTRREV` ищет последнее вхождение фрагмента в строке, возвращает позицию с нумерацией от 1 или `0`, если совпадение не найдено;
- пустой фрагмент в `INSTRREV` возвращает `LEN(text) + 1`;
- `SUBSTRCOUNT` считает непересекающиеся вхождения слева направо;
- пустой фрагмент в `SUBSTRCOUNT` даёт диагностику времени выполнения `MB3007`;
- добавлены автоматические проверки семантического анализа и выполнения;
- добавлены запускаемые `.axm`-примеры в `samples/functions/instrrev/` и `samples/functions/substrcount/`;
- обновлены спецификация, справочная страница строковых функций, указатель справочника и план работ.

### Что решено
- имя `COUNT` не используется как слишком общее;
- для подсчёта вхождений выбрано имя `SUBSTRCOUNT`, потому что оно явно говорит о подсчёте фрагментов строки;
- форма `INSTRREV(start, text, fragment)` пока не добавлена, чтобы не вводить спорное поведение начальной позиции без отдельного решения.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Search.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Search.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/instrrev/basic_usage.axm`
- `samples/functions/instrrev/empty_fragment.axm`
- `samples/functions/substrcount/basic_usage.axm`
- `samples/functions/substrcount/empty_fragment_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- согласовать следующую малую группу строковых функций или отдельно решить, нужна ли форма обратного поиска от заданной позиции.

## Session 128 - 2026-04-19 09:29 - Начальная позиция для INSTRREV

### Что сделано
- добавлена форма `INSTRREV(start, text, fragment)`;
- первый аргумент закреплён как начальная позиция, от которой обратный поиск идёт влево;
- если начальная позиция больше длины строки, поиск начинается с конца строки без ошибки;
- если начальная позиция меньше 1, выполнение завершается диагностикой времени выполнения `MB3007`;
- для пустого фрагмента форма с двумя аргументами возвращает `LEN(text) + 1`, а форма с начальной позицией возвращает значение начальной позиции плюс 1, но не больше `LEN(text) + 1`;
- добавлены автоматические проверки семантического анализа и выполнения;
- добавлены запускаемые примеры `samples/functions/instrrev/start_position.axm` и `samples/functions/instrrev/invalid_start_error.axm`;
- обновлены спецификация, справочная страница строковых функций, указатель справочника и план работ.

### Что решено
- слишком большая начальная позиция в `INSTRREV` не считается ошибкой, потому что практически означает поиск от конца строки;
- слишком маленькая начальная позиция остаётся ошибкой, как и в `INSTR`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Search.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Search.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/instrrev/start_position.axm`
- `samples/functions/instrrev/invalid_start_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore`.

### Следующий точечный шаг
- после проверки выбрать следующую практическую группу строковых функций или перейти к другой области языка.

## Session 129 - 2026-04-19 09:51 - Редакционная зачистка документации

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и навык `axiom-doc-author`;
- обновлён `README.md`: смешанные англоязычные описания слоёв, проверочных примеров, чистоты сборки и проверочной программы математических функций заменены на русские формулировки;
- обновлена `docs/SPECIFICATION.md`: старые подписи `Arity`, `Domain rules`, `Invalid input`, `runtime diagnostic` заменены на русские термины, а повреждённый кодировкой раздел про форматирование IDE переписан нормальным русским текстом;
- в HTML-справочнике видимый заголовок `Axiom Manual` заменён на `Справочник Axiom`;
- в `docs/manual/string-functions.html` формальные записи строковых функций переведены на русские метапеременные в угловых скобках, а пояснения к `count`, `oldText`, `fill` заменены русскими описаниями;
- точечно обновлены соседние справочные страницы, где в видимом тексте оставались такие же следы.

### Что решено
- имена файлов, каталогов, проектов, пунктов меню и реальные имена функций не переводятся, потому что они должны совпадать с кодом и интерфейсом;
- старые исторические записи `PLANS` и `WORKLOG` не переписываются задним числом, чтобы не нарушать хронологию документов.

### Затронутые пути
- `README.md`
- `docs/SPECIFICATION.md`
- `docs/manual/*.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен поисковый проход по `README.md`, `docs/SPECIFICATION.md` и `docs/manual/` на основные старые англоязычные формулировки; оставшиеся совпадения относятся в основном к реальным именам файлов, каталогов, пунктов меню, CSS/JS-идентификаторам или историческим названиям вроде `QuickBASIC`.

### Следующий точечный шаг
- выбрать: продолжить редакционный проход по странице числовых функций и внутренним архитектурным документам или перейти к следующей малой группе строковых функций.

## Session 130 - 2026-04-19 12:20 - Зачистка числовых функций и архитектурной документации

### Что сделано
- повторно прочитаны `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md`, `docs/WORKLOG.md` и навык `axiom-doc-author`;
- обновлён `docs/manual/numeric-functions.html`: формы вызова и короткие описания функций переведены на русские метапеременные и русскую описательную подачу;
- обновлён `docs/ARCHITECTURE.md`: вычищены наиболее заметные англоязычные описательные формулировки в разделах про слои, файловую панель, форматирование IDE, трассировку диагностики и каталог встроенных функций;
- обновлён `docs/PLANS.md` с фиксацией следующего шага после завершения документационного прохода.

### Что решено
- реальные имена проектов, файлов, интерфейсов, меню, образцов имён и кодовых сущностей не переводятся, чтобы документация оставалась связанной с кодом и пользовательским интерфейсом;
- числовая страница справочника теперь оформляется в том же русском стиле метапеременных, что и уже выровненная страница строковых функций.

### Затронутые пути
- `docs/manual/numeric-functions.html`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен поисковый проход по `docs/manual/numeric-functions.html`, `docs/ARCHITECTURE.md` и `docs/SPECIFICATION.md` на основные старые англоязычные формулировки;
- `dotnet test` не запускался, потому что код выполнения языка и тесты не менялись.

### Следующий точечный шаг
- перейти к следующей малой группе строковых функций: `PADLEFT`, `PADRIGHT`, `INSERT`, `REMOVE`.
## Session 131 - 2026-04-19 12:42 - Финальная зачистка архитектурной документации

### Что сделано
- в `docs/ARCHITECTURE.md` дочищены оставшиеся описательные англоязычные формулировки в разделах про выполнение, IDE, тестовую стратегию, форматирование, структурное редактирование и блочную видимость;
- сохранены исходные имена каталогов, типов, вкладок, меню и классов там, где они нужны для прямой связи документации с кодом и интерфейсом;
- повторно проверены `docs/manual/numeric-functions.html` и `docs/ARCHITECTURE.md` поиском по старым шаблонам форм вызова и по остаточным англоязычным хвостам.

### Что решено
- страницу числовых функций и внутреннюю архитектурную документацию считаем дочищенными в рамках текущего редакционного прохода;
- следующую языковую задачу берём уже как отдельный функциональный этап, не смешивая её с дальнейшей стилистической правкой.

### Затронутые пути
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен поисковый проход по `docs/manual/numeric-functions.html` на старые англоязычные сигнатуры;
- выполнен поисковый проход по `docs/ARCHITECTURE.md` на остаточные описательные англоязычные формулировки;
- `dotnet test` не запускался, потому что менялась только документация.

### Следующий точечный шаг
- перейти к следующей малой группе строковых функций: `PADLEFT`, `PADRIGHT`, `INSERT`, `REMOVE`.

## Session 132 - 2026-04-19 12:45 - Реализация PADLEFT, PADRIGHT, INSERT и REMOVE

### Что сделано
- в `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`, `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Fill.cs`, `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Edit.cs` и `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs` добавлены идентификаторы и описатели встроенных строковых функций `PADLEFT`, `PADRIGHT`, `INSERT`, `REMOVE`;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Fill.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Edit.cs` и `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Helpers.cs` реализовано выполнение:
  - `PADLEFT` и `PADRIGHT` поддерживают форму без заполнителя и форму с необязательным заполнителем;
  - по умолчанию используется пробел;
  - из непустой строки заполнителя берётся только первый символ;
  - `INSERT` вставляет фрагмент перед позицией с единичной индексацией и при позиции правее конца строки добавляет фрагмент в конец;
  - `REMOVE` удаляет заданное количество символов с единичной индексацией, допускает `count = 0`, не падает при старте правее конца строки и мягко обрезает удаление по правой границе;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` добавлены проверки привязки новых встроенных функций, включая выбор нужной перегрузки `PADLEFT`/`PADRIGHT`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` добавлены проверки выполнения, правил дополнения и вставки, а также диагностик `MB3007` для отрицательной ширины, пустого заполнителя, недопустимой начальной позиции и отрицательного количества;
- добавлены ручные примеры в новых каталогах `samples/functions/padleft/`, `samples/functions/padright/`, `samples/functions/insert/`, `samples/functions/remove/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/string-functions.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`, чтобы новое поведение было видно в спецификации и справочнике.

### Что решено
- для `PADLEFT` и `PADRIGHT` параметр заполнителя принят как `STRING`, а не отдельный символьный тип, потому что в языке уже нет отдельного `CHAR`, а поведение с первым символом согласуется с уже существующей функцией `STRING`;
- для `INSERT` и `REMOVE` закреплена мягкая модель правой границы: слишком большая положительная позиция не даёт сбой, а превращается во вставку в конец или в отсутствие удаления;
- для `REMOVE` отрицательное количество запрещено, а нулевое количество рассматривается как допустимая пустая операция.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Fill.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Edit.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Fill.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Edit.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Helpers.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/padleft/basic_usage.axm`
- `samples/functions/padleft/negative_width_error.axm`
- `samples/functions/padright/basic_usage.axm`
- `samples/functions/padright/empty_fill_error.axm`
- `samples/functions/insert/basic_usage.axm`
- `samples/functions/insert/invalid_start_error.axm`
- `samples/functions/remove/basic_usage.axm`
- `samples/functions/remove/negative_count_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- первая попытка параллельного прогона `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore` вместе с `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore` упёрлась в блокировку файла сборки со стороны `VBCSCompiler`; это инфраструктурное ограничение артефактов, а не ошибка новой логики;
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore` — успешно;
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore` — успешно;
- `dotnet test Axiom.sln --no-restore` — успешно.

### Следующий точечный шаг
- перейти к следующей малой группе строковых функций: `ASC` и `CHR`.

## Session 133 - 2026-04-19 12:53 - Реализация ASC и CHR

### Что сделано
- в `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`, `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs` и новом файле `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Char.cs` добавлены идентификаторы и описания встроенных функций `ASC` и `CHR`;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` и новом файле `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Char.cs` реализовано выполнение:
  - `ASC` возвращает код первого символа Unicode;
  - `CHR` строит строку из одного символа Unicode;
  - пустая строка для `ASC` даёт `MB3007`;
  - для `CHR` запрещены отрицательные значения, коды выше `1114111` и суррогатный диапазон `55296..57343`;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` добавлена проверка, что `ASC` связывается как функция с результатом `INTEGER`, а `CHR` как функция с результатом `STRING`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` добавлены проверки обычного поведения Unicode-кодов и диагностик `MB3007` для пустой строки и недопустимых кодов;
- добавлены ручные примеры в каталогах `samples/functions/asc/` и `samples/functions/chr/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/string-functions.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`, чтобы новое поведение было видно в спецификации и справочнике.

### Что решено
- `ASC` и `CHR` закреплены как Unicode-функции, а не как работа с историческими основными и расширенными таблицами кодировки;
- для получения кода берётся именно первый символ Unicode, чтобы модель поведения совпадала с типом `STRING` в языке.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Char.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Char.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/asc/basic_usage.axm`
- `samples/functions/asc/empty_text_error.axm`
- `samples/functions/chr/basic_usage.axm`
- `samples/functions/chr/invalid_code_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнить `dotnet test Axiom.sln --no-restore`.

### Следующий точечный шаг
- реализовать `CENTER` как следующую самостоятельную строковую функцию форматирования, а после него отдельно спроектировать группу `SPLIT`/`JOIN`.

## Session 134 - 2026-04-19 13:07 - Реализация CENTER

### Что сделано
- в `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs` и `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Fill.cs` добавлены идентификаторы и сигнатуры встроенной функции `CENTER` в формах без заполнителя и с необязательным заполнителем;
- в `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs` и `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Fill.cs` реализовано выполнение `CENTER`:
  - по умолчанию используется пробел;
  - из непустой строки заполнителя берётся только первый символ;
  - если ширина меньше или равна длине строки, возвращается исходный текст;
  - при нечётной разнице между шириной поля и длиной текста лишний символ заполнения добавляется справа;
  - отрицательная ширина и пустой заполнитель дают `MB3007`;
- в `tests/Axiom.Semantics.Tests/BinderTests.cs` добавлена проверка выбора нужной формы `CENTER`;
- в `tests/Axiom.Runtime.Tests/InterpreterTests.cs` добавлены проверки обычного центрирования и диагностик для пустого заполнителя и отрицательной ширины;
- добавлены ручные примеры в каталоге `samples/functions/center/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/string-functions.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`, чтобы новое поведение было видно в спецификации и справочнике.

### Что решено
- `CENTER` закреплён в той же модели, что `PADLEFT` и `PADRIGHT`: заполнитель имеет тип `STRING`, но используется только его первый символ;
- правило распределения нечётного остатка выбрано явным: лишний символ заполнения добавляется справа.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Fill.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Fill.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/center/basic_usage.axm`
- `samples/functions/center/empty_fill_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнить `dotnet test Axiom.sln --no-restore`.

### Следующий точечный шаг
- отдельно спроектировать группу `SPLIT`/`JOIN` и решить, как она должна работать с массивами строк в текущей модели языка.

## Session 135 - 2026-04-19 13:19 - Зарезервированы SPLIT и JOIN в документации

### Что сделано
- в `docs/SPECIFICATION.md` добавлены отдельные записи для `SPLIT` и `JOIN` как для зарезервированных будущих функций, которые пока не входят в действующий контракт языка;
- в `docs/manual/string-functions.html` добавлен отдельный раздел о зарезервированных `SPLIT` и `JOIN`, чтобы справочник явно говорил, что эти имена закреплены на будущее, но вызывать их сейчас нельзя;
- в `docs/manual/scripts/manual.js` добавлены элементы указателя для `SPLIT` и `JOIN` с пометкой, что это запланированные, а не действующие функции;
- обновлён `docs/PLANS.md`, чтобы следующим этапом числилось не преждевременное внедрение `SPLIT`/`JOIN`, а развитие модели массивов.

### Что решено
- `SPLIT` и `JOIN` зафиксированы в документации как имена будущего этапа, а не как часть текущего рабочего набора встроенных функций;
- окончательные сигнатуры умышленно не зафиксированы заранее, чтобы не породить новый публичный контракт до завершения этапа по массивам.

### Затронутые пути
- `docs/SPECIFICATION.md`
- `docs/manual/string-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- код и поведение языка не менялись; выполнен документный проход без запуска `dotnet test`.

### Следующий точечный шаг
- перейти к отдельному планированию и развитию модели массивов, после чего вернуться к окончательному проектированию `SPLIT` и `JOIN`.
## Session 136 - 2026-04-19 14:15 - SELECT CASE приведён к справочному формату

### Что сделано
- в `docs/manual/select-case.html` полностью заменена старая учебниковая подача на справочную карточку в принятом жанре;
- добавлены разделы `Сводка`, `Синтаксис`, `Части синтаксиса`, `Замечания`, `Примеры`, `Диагностика` и `См. также`;
- на странице теперь отражены подтверждённые формы `CASE`: точное значение, список значений, диапазон `TO`, сравнение через `IS` и ветвь `CASE ELSE`;
- в примеры включены строковые варианты `SELECT CASE`, а в диагностику вынесены реальные коды `MB2003`, `MB2015` и `MB2001`;
- обновлён `docs/PLANS.md`, чтобы следующим отдельным этапом шли арифметические операции целочисленного деления и остатка.

### Что решено
- в этом проходе не трогаются описание и реализация арифметики: задача ограничена только выравниванием страницы `SELECT CASE` под принятый стандарт manual;
- поведение страницы теперь описывается по фактической спецификации, тестам и существующим примерам, без добавления неподтверждённых свойств.

### Затронутые пути
- `docs/manual/select-case.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- код языка и тесты не менялись; выполнен только документный проход без запуска `dotnet test`.

### Следующий точечный шаг
- спроектировать и внедрить арифметические операции целочисленного деления и остатка, а затем отразить их в спецификации, manual, тестах и `.axm`-примерах.

## Session 137 - 2026-04-19 14:50 - Целочисленное деление, MOD и восстановление manual.js

### Что сделано
- в `src/Axiom.Lexing/TokenKind.cs`, `src/Axiom.Syntax/SyntaxKind.cs` и `src/Axiom.Lexing/Lexer.cs` добавлены токены `BackslashToken` и `ModKeyword`, а также распознавание `\` и `MOD`;
- в `src/Axiom.Parsing/Parser.Helpers.cs` и `src/Axiom.Parsing/Parser.Expressions.cs` операторы `\` и `MOD` включены в арифметический синтаксис и приоритеты;
- в `src/Axiom.Semantics/Binding/Binder.Helpers.cs` и `src/Axiom.Semantics/Binding/Binder.Expressions.cs` закреплены правила для целочисленного деления и остатка: допускаются только `INTEGER` и `LONG`, результат повышается до `LONG`, если хотя бы один операнд имеет этот тип;
- в `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs` реализованы выполнение `\` и `MOD`, диагностика деления на ноль `MB3001` и переполнения `MB3008`;
- в `tests/Axiom.Lexing.Tests/LexerTests.cs`, `tests/Axiom.Parsing.Tests/ParserTests.cs`, `tests/Axiom.Semantics.Tests/BinderTests.cs` и `tests/Axiom.Runtime.Tests/InterpreterTests.cs` добавлены проверки новых операторов;
- добавлены ручные `.axm`-примеры в `samples/operators/integer-division/` и `samples/operators/mod/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/arithmetic-and-logic.html` и `docs/manual/scripts/manual.js`; отдельно восстановлена кодировка русского текста в левом меню и алфавитном указателе manual.

### Что решено
- базовый арифметический набор текущего этапа закрыт: сложение, вычитание, умножение, обычное деление, целочисленное деление и остаток теперь имеют согласованные правила в коде и документации;
- для `\` и `MOD` выбрана модель, совместимая с поведением C#: деление усечением к нулю и знак остатка по левому операнду.

### Затронутые пути
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/operators/integer-division/basic_usage.axm`
- `samples/operators/integer-division/division_by_zero_error.axm`
- `samples/operators/mod/basic_usage.axm`
- `samples/operators/mod/division_by_zero_error.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/arithmetic-and-logic.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test Axiom.sln --no-restore`;
- выполнен `node --check docs/manual/scripts/manual.js`.

### Следующий точечный шаг
- выбрать следующий самостоятельный этап языка вне базовой арифметики; если возвращаться именно к арифметике, сначала отдельно спроектировать будущие расширения, а затем вносить их в код и справочник.

## Session 138 - 2026-04-19 14:56 - Уточнены формулировки по арифметике

### Что сделано
- в `docs/manual/arithmetic-and-logic.html` переписаны две формулировки про `\` и `MOD`, чтобы описание было на естественном русском языке: вместо оборотов вида «принимает только INTEGER и LONG» теперь явно сказано, что целочисленное деление и остаток работают только с типами `INTEGER` и `LONG`;
- отдельно перепроверено, что в спецификации таких же неудачных формулировок для арифметики не осталось.

### Что решено
- по базовому набору арифметических выражений на текущем этапе пробелов не осталось: в языке есть `+`, `-`, `*`, `/`, `\`, `MOD`, унарный `-`, строковая конкатенация через `+`, сравнения и логические операторы;
- возможные будущие расширения арифметики теперь следует считать не забытыми дырами, а отдельными новыми возможностями языка.

### Затронутые пути
- `docs/manual/arithmetic-and-logic.html`
- `docs/WORKLOG.md`

### Проверка
- выполнена ручная сверка описания арифметики в manual и спецификации; код языка не менялся.

### Следующий точечный шаг
- выбирать следующий самостоятельный этап языка вне базового арифметического ядра.

## Session 139 - 2026-04-19 15:04 - Исследована расширенная арифметика QuickBASIC и PDS 7.1

### Что сделано
- по локальным справочникам Microsoft `Basic Language Reference (1989).pdf`, `Programmers Guide (1989).pdf` и `Getting Started 7.1 (1990).pdf` отдельно перепроверены операции возведения в степень, логико-побитовые операторы, литералы в шестнадцатеричной записи и возможные операции сдвига;
- подтверждено, что исторический BASIC уже поддерживал `^`, `NOT`, `AND`, `OR`, `XOR`, `EQV`, `IMP`, а также литералы с префиксом `&H`; подтверждено и отсутствие встроенных операторов сдвига в самом синтаксисе языка;
- в `docs/PLANS.md` добавлен новый проектный блок, чтобы следующий арифметический этап опирался не на воспоминания из чата, а на зафиксированные выводы из первичных источников.

### Что решено
- как наиболее здравое направление для Axiom зафиксирован гибридный путь: историческое BASIC-ядро (`^`, `&H`, целочисленные побитовые `NOT`/`AND`/`OR`/`XOR`) плюс современные сдвиги `<<` и `>>` как аккуратное расширение сверх QuickBASIC;
- операторы `EQV` и `IMP` признаны исторически подлинными, но не включены в ближайший обязательный объём: они полезны меньше, чем `XOR` и сдвиги, и требуют отдельного подтверждения перед реализацией.

### Затронутые пути
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен документный исследовательский проход по локальным PDF-источникам Microsoft; код языка и тесты в этом проходе не менялись.

### Следующий точечный шаг
- перейти от исследования к явному проектному решению по следующему арифметическому этапу и после подтверждения начать реализацию в порядке: `^`, `&H`, побитовые `NOT`/`AND`/`OR`/`XOR`, затем `<<` и `>>`.

## Session 140 - 2026-04-19 15:28 - Реализована расширенная арифметика `^`, `&H`, побитовые операции и сдвиги

### Что сделано
- в фронтенд добавлены новые токены и правила: `^`, `<<`, `>>`, ключевое слово `XOR` и шестнадцатеричные литералы `&H...`;
- в парсере зафиксированы правосторонняя ассоциативность `^` и новые приоритеты для степени, сдвигов и `XOR`;
- в семантике и интерпретаторе реализованы: оператор степени с результатом `DOUBLE`, побитовые `NOT`/`AND`/`OR`/`XOR` для `INTEGER` и `LONG`, а также сдвиги `<<` и `>>`;
- добавлены автоматические тесты для лексера, парсера, семантики и выполнения;
- добавлены ручные `.axm`-примеры по новым операторам и шестнадцатеричным литералам;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/arithmetic-and-logic.html` и `docs/manual/scripts/manual.js`.

### Что решено
- `^` принимает любые числовые операнды, вычисляется справа налево и всегда возвращает `DOUBLE`;
- `NOT`, `AND`, `OR`, `XOR` работают в двух режимах: логическом для `BOOLEAN` и побитовом для `INTEGER`/`LONG`;
- сдвиги `<<` и `>>` считаются современным расширением языка, а не восстановлением исторического синтаксиса QuickBASIC;
- литералы `&H...` пока трактуются как неотрицательные целочисленные значения `INTEGER` или `LONG`.

### Затронутые пути
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/operators/power/basic_usage.axm`
- `samples/operators/and/bitwise_usage.axm`
- `samples/operators/or/bitwise_usage.axm`
- `samples/operators/not/bitwise_usage.axm`
- `samples/operators/xor/basic_usage.axm`
- `samples/operators/shift-left/basic_usage.axm`
- `samples/operators/shift-right/basic_usage.axm`
- `samples/expressions/hex-literals/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/arithmetic-and-logic.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test Axiom.sln --no-restore`;
- выполнен `node --check docs/manual/scripts/manual.js`.

### Следующий точечный шаг
- если продолжать именно арифметическую ветку, отдельно решить судьбу `EQV`, `IMP` и более исторической модели знаковых шестнадцатеричных масок.

## Session 141 - 2026-04-19 16:09 - Реализованы EQV и IMP

### Что сделано
- в лексер, синтаксическое дерево и разбор добавлены ключевые слова `EQV` и `IMP`;
- в семантике оба оператора подключены к общей модели логико-побитовых операций;
- в интерпретаторе реализовано выполнение: `EQV` как равнозначность и побитовое отрицание `XOR`, `IMP` как импликация и побитовое `NOT` от левого операнда с последующим `OR`;
- обновлена подсветка IDE, чтобы `MOD`, `XOR`, `EQV` и `IMP` считались ключевыми словами;
- добавлены автоматические тесты для лексера, разбора, семантики, выполнения, подсветки и форматирования;
- добавлены ручные `.axm`-примеры в `samples/operators/eqv/` и `samples/operators/imp/`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/arithmetic-and-logic.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`.

### Что решено
- `EQV` и `IMP` входят в язык как исторические BASIC-операторы, но работают в той же аккуратной двухрежимной модели, что и `AND`/`OR`/`XOR`: логически для `BOOLEAN`, побитово для `INTEGER` и `LONG`;
- текущая модель `&H...` не менялась: это по-прежнему неотрицательное значение `INTEGER` или `LONG`, без исторического переосмысления старших битов как знака.

### Затронутые пути
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Lexing.Tests/LexerTests.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `samples/operators/eqv/basic_usage.axm`
- `samples/operators/imp/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/arithmetic-and-logic.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнить `dotnet test Axiom.sln --no-restore`;
- выполнить `node --check docs/manual/scripts/manual.js`.

### Следующий точечный шаг
- после проверки решить, закрываем ли арифметическую ветку на текущей простой модели `&H...` или отдельно проектируем историческую трактовку знаковых шестнадцатеричных масок.

## Session 142 - 2026-04-19 16:14 - Проверка EQV, IMP и правка унарного разбора

### Что сделано
- исправлена ошибка в тесте семантики: проверка смешивания `BOOLEAN` с целыми типами для `XOR`, `EQV` и `IMP` оформлена как параметризованный тест без лишнего атрибута;
- унарные `-` и `NOT` в разборе выражений поставлены между `^` и группой `*`, `/`, `\`, `MOD`, чтобы выражения вида `value \ -1` и `value MOD -1` разбирались корректно;
- добавлен тест разбора, который закрепляет порядок `^`, унарного минуса и умножения;
- ручные `.axm`-примеры для `\` и `MOD` дополнены отрицательным правым операндом.

### Затронутые пути
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `samples/operators/integer-division/basic_usage.axm`
- `samples/operators/mod/basic_usage.axm`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test Axiom.sln --no-restore` — успешно;
- выполнен `node --check docs/manual/scripts/manual.js` — успешно.

### Следующий точечный шаг
- арифметический набор можно считать закрытым на текущей модели; дальше нужно выбрать следующий самостоятельный этап языка либо отдельно запланировать историческую трактовку знаковых шестнадцатеричных масок `&H...`.

## Session 143 - 2026-04-19 17:01 - Реализованы функции регулярных выражений

### Что сделано
- добавлены встроенные функции `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE` и `REGEXCOUNT`;
- описание встроенных функций вынесено в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Regex.cs`;
- выполнение функций вынесено в `src/Axiom.Runtime/Execution/Interpreter.Builtins.Regex.cs`;
- поддержаны режимы `i`, `m`, `s`, `x`, диагностика неизвестного режима, диагностика недопустимого регулярного выражения и ограничение времени выполнения операции пятью секундами;
- `REGEXREPLACE` поддерживает ограничение количества замен и ссылки на группы в строке замены через `$1`, `$2` и последующие номера;
- добавлены автоматические тесты семантики и выполнения;
- добавлены ручные `.axm`-примеры для всех новых функций;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/regex-functions.html`, навигация справочника и обзорные страницы.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Regex.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Regex.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/regexismatch/basic_usage.axm`
- `samples/functions/regexismatch/invalid_pattern_error.axm`
- `samples/functions/regexismatch/unknown_mode_error.axm`
- `samples/functions/regexmatch/basic_usage.axm`
- `samples/functions/regexextract/basic_usage.axm`
- `samples/functions/regexreplace/basic_usage.axm`
- `samples/functions/regexreplace/negative_count_error.axm`
- `samples/functions/regexcount/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/regex-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/string-functions.html`
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test Axiom.sln --no-restore` — успешно;
- выполнен `node --check docs/manual/scripts/manual.js` — успешно.

### Следующий точечный шаг
- выбрать следующий самостоятельный этап языка: развитие массивов с последующим возвратом к `SPLIT`/`JOIN` и возможным функциям регулярных выражений, возвращающим набор значений.

## Session 144 - 2026-04-19 18:12 - Уточнена отложенная задача по регулярным выражениям

### Что сделано
- в `docs/PLANS.md` добавлена отдельная явная запись про будущие функции регулярных выражений, которые должны возвращать набор значений;
- уточнено, что такие функции нужно проектировать после развития массивов, вместе с возвращением к `SPLIT` и `JOIN`;
- зафиксировано, что до утверждения модели массивов не нужно закреплять окончательные имена и формы вызова этих функций.

### Проверка
- автоматические проверки не запускались: изменён только план работ и журнал.

### Следующий точечный шаг
- продолжить следующий самостоятельный этап развития языка, не забывая вернуться к функциям регулярных выражений с набором значений после развития массивов.

## Session 145 - 2026-04-19 18:14 - Зарезервированные функции регулярных выражений добавлены в справочник

### Что сделано
- в `docs/manual/regex-functions.html` добавлен блок будущих функций регулярных выражений, возвращающих набор значений;
- в справочнике зарезервированы имена `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT`;
- явно указано, что эти функции сейчас не реализованы, не входят в действующий набор встроенных функций и будут развиты после работы над массивами;
- `docs/manual/scripts/manual.js` обновлён, чтобы зарезервированные имена находились через указатель справочника;
- `docs/SPECIFICATION.md` и `docs/PLANS.md` уточнены под выбранные зарезервированные имена.

### Проверка
- `node --check docs/manual/scripts/manual.js` выполнен успешно.

### Следующий точечный шаг
- продолжить следующий самостоятельный этап развития языка.

## Session 146 - 2026-04-19 18:20 - Изучен референс файловых функций Axiom

### Что сделано
- изучен файл `C:\projects\Axiom\Visitors\AxiomInterpreter.FileFunctions.cs` как референс для будущих файловых функций Axiom;
- выделены полезные идеи: группировка операций, единое приведение путей к полному виду и подготовка отдельного места для будущих правил доступа к файлам и папкам;
- в `docs/PLANS.md` добавлен отдельный план первого безопасного этапа файловых функций.

### Что решено
- начинать не с операторов `OPEN`/`CLOSE` и файловых номеров, а с простых встроенных функций для текстовых файлов, папок и путей;
- не добавлять в первый этап функции, которым нужен возврат набора значений, пока не завершена работа над массивами;
- не добавлять в первый этап рекурсивное удаление, символьные ссылки, наблюдение за файлами и смену текущей папки программы.

### Проверка
- автоматические проверки не запускались: код языка пока не изменялся, обновлён только план работ и журнал.

### Следующий точечный шаг
- реализовать первый безопасный этап файловых функций в отдельных файлах семантики и среды выполнения, затем добавить проверки, справочник и ручные `.axm`-примеры.

## Session 147 - 2026-04-19 18:34 - Реализован первый этап функций файловой системы

### Что сделано
- добавлены встроенные функции `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `READFILE`, `WRITEFILE`, `APPENDFILE`, `FILESIZE`, `FILEDATE`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `EXTNAME`, `FULLPATH` и `MAKEDIR`;
- описание функций вынесено в `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`;
- выполнение функций вынесено в `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`;
- добавлены проверки семантики и выполнения для новых функций;
- добавлены ручные `.axm`-примеры для каждой новой функции в `samples/functions/`;
- добавлена страница `docs/manual/file-system-functions.html` и обновлена навигация справочника;
- обновлены `docs/SPECIFICATION.md` и `docs/PLANS.md`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/fileexists/basic_usage.axm`
- `samples/functions/direxists/basic_usage.axm`
- `samples/functions/pathexists/basic_usage.axm`
- `samples/functions/readfile/basic_usage.axm`
- `samples/functions/writefile/basic_usage.axm`
- `samples/functions/appendfile/basic_usage.axm`
- `samples/functions/filesize/basic_usage.axm`
- `samples/functions/filedate/basic_usage.axm`
- `samples/functions/combinepath/basic_usage.axm`
- `samples/functions/filename/basic_usage.axm`
- `samples/functions/dirname/basic_usage.axm`
- `samples/functions/extname/basic_usage.axm`
- `samples/functions/fullpath/basic_usage.axm`
- `samples/functions/makedir/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- выбрать следующий этап: второй слой файловой системы с безопасными правилами для удаления, копирования и переноса либо развитие массивов для будущих функций, возвращающих набор значений.

## Session 148 - 2026-04-19 18:51 - Реализован второй этап функций файловой системы

### Что сделано
- добавлены встроенные функции `DELETEFILE`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEDIR`, `COPYDIR`, `MOVEDIR` и `RENAMEDIR`;
- `COPYFILE`, `MOVEFILE` и `COPYDIR` получили формы с необязательным логическим аргументом перезаписи;
- `DELETEFILE` и `DELETEDIR` возвращают `FALSE`, если удаляемого файла или папки нет;
- `DELETEDIR` удаляет только пустую папку;
- `RENAMEFILE` и `RENAMEDIR` проверяют, что новое имя не содержит путь;
- добавлены автоматические проверки семантики и выполнения;
- добавлены ручные `.axm`-примеры для новых функций;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/file-system-functions.html`, `docs/manual/scripts/manual.js` и `docs/PLANS.md`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/deletefile/basic_usage.axm`
- `samples/functions/copyfile/basic_usage.axm`
- `samples/functions/movefile/basic_usage.axm`
- `samples/functions/renamefile/basic_usage.axm`
- `samples/functions/deletedir/basic_usage.axm`
- `samples/functions/copydir/basic_usage.axm`
- `samples/functions/movedir/basic_usage.axm`
- `samples/functions/renamedir/basic_usage.axm`
- `samples/functions/fileexists/basic_usage.axm`
- `samples/functions/direxists/basic_usage.axm`
- `samples/functions/writefile/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- продолжить именно файловую систему: спроектировать третий слой возможностей вокруг сведений о папках, размере папки, свободном месте и возможных правилах безопасного рекурсивного удаления.

## Session 149 - 2026-04-19 19:02 - Реализован третий безопасный слой функций файловой системы

### Что сделано
- добавлены встроенные функции `CURRENTDIR`, `DIRSIZE`, `FREESPACE`, `MAKETEMPFILE` и `MAKETEMPDIR`;
- среда выполнения получила вычисление текущей папки, рекурсивного размера папки, свободного места на диске и создание временного файла и временной папки;
- для `DIRSIZE` добавлена защита от обхода папок-ссылок и диагностика переполнения суммарного размера;
- для `MAKETEMPFILE` и `MAKETEMPDIR` добавлена проверка префикса, чтобы он был только префиксом имени, а не путём;
- расширены автоматические проверки семантики и выполнения, добавлены ручные `.axm`-примеры и обновлены спецификация, справочник и алфавитный указатель;
- создание временной папки дополнительно защищено от редкого совпадения имени с уже существующим файлом или папкой.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/currentdir/basic_usage.axm`
- `samples/functions/dirsize/basic_usage.axm`
- `samples/functions/freespace/basic_usage.axm`
- `samples/functions/maketempfile/basic_usage.axm`
- `samples/functions/maketempdir/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- продолжить файловую систему: отдельно спроектировать безопасное рекурсивное удаление папок и отдельно решить, нужен ли переход в другую текущую папку программы.

## Session 150 - 2026-04-19 19:20 - Добавлены рекурсивное удаление папок и смена текущей папки

### Что сделано
- добавлены встроенные функции `DELETEDIRRECURSIVE` и `CHANGEDIR`;
- `DELETEDIRRECURSIVE` удаляет папку вместе со всем содержимым и возвращает `FALSE`, если папки нет;
- `CHANGEDIR` меняет текущую папку программы, а последующие относительные пути начинают вычисляться от новой папки;
- расширены проверки семантики и выполнения, добавлены runnable `.axm`-примеры и обновлены спецификация, справочник и алфавитный указатель.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/changedir/basic_usage.axm`
- `samples/functions/deletedirrecursive/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- выбрать следующий шаг по файловой системе: перечисление файлов и папок, дополнительные сведения о файловой системе или операторная подсистема `OPEN`/`CLOSE`.

## Session 151 - 2026-04-19 19:34 - Добавлен расширенный скалярный слой файловой системы

### Что сделано
- добавлены встроенные функции `FILECREATEDATE`, `FILEACCESSEDDATE`, `DIRDATE`, `PATHROOT`, `ISABSOLUTEPATH`, `TOTALFREESPACE`, `TOTALSPACE`, `ISREADONLY`, `ISHIDDEN`, `DRIVENAME`, `DRIVEFORMAT` и `DRIVETYPE`;
- среда выполнения получила вычисление расширенных дат, признаков пути, атрибутов объекта файловой системы и сведений о диске;
- расширены проверки семантики и выполнения, добавлены runnable `.axm`-примеры и обновлены спецификация, справочник и алфавитный указатель.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/filecreatedate/basic_usage.axm`
- `samples/functions/fileaccesseddate/basic_usage.axm`
- `samples/functions/dirdate/basic_usage.axm`
- `samples/functions/pathroot/basic_usage.axm`
- `samples/functions/isabsolutepath/basic_usage.axm`
- `samples/functions/totalfreespace/basic_usage.axm`
- `samples/functions/totalspace/basic_usage.axm`
- `samples/functions/isreadonly/basic_usage.axm`
- `samples/functions/ishidden/basic_usage.axm`
- `samples/functions/drivename/basic_usage.axm`
- `samples/functions/driveformat/basic_usage.axm`
- `samples/functions/drivetype/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore --filter "BuiltinFileSystem|FileSystem"` - успешно;
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- перейти к следующему крупному этапу файловой системы: спроектировать и реализовать операторную подсистему `OPEN`/`CLOSE`.

## Session 152 - 2026-04-19 20:09 - Реализованы тип FILE и операторы OPEN / CLOSE

### Что сделано
- добавлены специальный тип `FILE`, операторы `OPEN` и `CLOSE`, а также встроенные функции `EOF`, `READLINE`, `WRITE`, `WRITELINE`;
- в синтаксисе, семантике и среде выполнения внедрена современная модель работы с текстовыми файлами через именованную переменную `FILE` вместо файловых номеров;
- обновлены спецификация, страницы справочника `Типы данных`, `Функции файловой системы`, созданы отдельные страницы `OPEN` и `CLOSE`, расширен алфавитный указатель;
- в документации отдельно зафиксировано, что `GETFILES` и `GETDIRS` остаются будущими функциями до отдельного этапа развития массивов.

### Затронутые пути
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/InputOutputSyntax.cs`
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/operators/open/basic_usage.axm`
- `samples/operators/close/basic_usage.axm`
- `samples/functions/eof/basic_usage.axm`
- `samples/functions/readline/basic_usage.axm`
- `samples/functions/write/basic_usage.axm`
- `samples/functions/writeline/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/data-types.html`
- `docs/manual/file-system-functions.html`
- `docs/manual/open.html`
- `docs/manual/close.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\Axiom.sln --no-restore` - языковые проекты прошли успешно, общий прогон остановился на блокировке файлов из-за запущенного `Axiom.Ide.exe`;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- продолжить развитие файловой подсистемы вокруг открытого `FILE`, а перечисление `GETFILES` / `GETDIRS` оставить отдельным будущим этапом до завершения развития массивов.

## Session 153 - 2026-04-19 20:40 - Добавлены функции состояния и буфера FILE

### Что сделано
- добавлены встроенные функции `ISOPEN`, `FILEPATH`, `FILEMODE` и `FLUSH` вокруг уже открытого типа `FILE`;
- среда выполнения для текстовых файлов переведена на явную буферизацию записи: `WRITE` и `WRITELINE` больше не сбрасывают поток автоматически, а `FLUSH` и `CLOSE` делают это явно;
- расширены тесты семантики и выполнения для новых функций и для сценария видимого изменения размера файла после `FLUSH`;
- обновлены спецификация и справочник, включая страницы `Типы данных` и `Функции файловой системы`, а также алфавитный указатель;
- добавлены runnable `.axm`-примеры для `ISOPEN`, `FILEPATH`, `FILEMODE` и `FLUSH`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/isopen/basic_usage.axm`
- `samples/functions/filepath/basic_usage.axm`
- `samples/functions/filemode/basic_usage.axm`
- `samples/functions/flush/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/data-types.html`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- если продолжать именно файловую подсистему, следующим отдельным этапом спроектировать позиционные возможности вокруг открытого `FILE`: нужна ли языку работа с текущей позицией, длиной файла и явным переходом по позиции.

## Session 154 - 2026-04-19 20:55 - Добавлены позиционные функции вокруг открытого FILE

### Что сделано
- добавлены встроенные функции `FILEPOS`, `FILELENGTH` и `SEEKFILE` для работы с текущей позицией и длиной уже открытого `FILE`;
- `RuntimeTextFile` переведён на явный поток с собственной буферизацией записи, чтобы `FILEPOS` и `FILELENGTH` корректно отражали и уже накопленные в буфере данные;
- для `SEEKFILE` выбрана строгая модель: позиция задаётся в байтах от начала файла, первая позиция равна `0`, переход разрешён только в диапазоне от `0` до текущей длины файла включительно, а режим `APPEND` не поддерживает явный переход;
- обновлены тесты семантики и выполнения, добавлены runnable `.axm`-примеры `samples/functions/filepos/basic_usage.axm`, `samples/functions/filelength/basic_usage.axm` и `samples/functions/seekfile/basic_usage.axm`;
- обновлены `docs/SPECIFICATION.md`, страницы справочника `docs/manual/file-system-functions.html`, `docs/manual/data-types.html` и указатель `docs/manual/scripts/manual.js`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/filepos/basic_usage.axm`
- `samples/functions/filelength/basic_usage.axm`
- `samples/functions/seekfile/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/data-types.html`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- если продолжаем файловую подсистему, следующим аккуратным шагом можно решить, нужен ли отдельный современный набор операций поблочного чтения и записи вокруг открытого `FILE`, либо пока остановиться на текущем текстовом позиционном наборе.

## Session 155 - 2026-04-19 21:14 - Добавлены READTOEND, READTEXT и REWIND для открытого FILE

### Что сделано
- добавлены встроенные функции `READTOEND`, `READTEXT` и `REWIND` вокруг уже открытого `FILE`;
- `RuntimeTextFile` расширен символьным чтением остатка текста и текстового фрагмента поверх байтовой позиционной модели;
- для текстового чтения введена строгая проверка UTF-8: если позиция попадает внутрь многобайтного символа, `READLINE`, `READTEXT` и `READTOEND` выдают диагностику времени выполнения;
- в документации явно зафиксировано разделение моделей: `FILEPOS`, `FILELENGTH`, `SEEKFILE`, `REWIND` работают с байтовой позицией, а `READLINE`, `READTEXT`, `READTOEND` читают текст по символам;
- обновлены тесты семантики и выполнения, добавлены runnable `.axm`-примеры `samples/functions/readtoend/basic_usage.axm`, `samples/functions/readtext/basic_usage.axm` и `samples/functions/rewind/basic_usage.axm`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/file-system-functions.html`, `docs/manual/data-types.html` и `docs/manual/scripts/manual.js`.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/readtoend/basic_usage.axm`
- `samples/functions/readtext/basic_usage.axm`
- `samples/functions/rewind/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/data-types.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- если продолжаем файловую подсистему, следующим аккуратным шагом можно решить, нужен ли отдельный современный набор более крупных операций чтения и записи вокруг открытого `FILE`, либо оставить файловую модель в текущем компактном текстовом виде.

## Session 156 - 2026-04-19 21:29 - Добавлены LINENUMBER, SKIPLINE и NEWLINE для открытого FILE

### Что сделано
- добавлены встроенные функции `LINENUMBER`, `SKIPLINE` и `NEWLINE` вокруг уже открытого `FILE`;
- модель `RuntimeTextFile` расширена отслеживанием текстового номера строки и служебной операцией пропуска строки с пересчётом после позиционных переходов;
- обновлены тесты семантики и выполнения, добавлены runnable `.axm`-примеры `samples/functions/linenumber/basic_usage.axm`, `samples/functions/skipline/basic_usage.axm` и `samples/functions/newline/basic_usage.axm`;
- обновлены `docs/SPECIFICATION.md`, `docs/manual/file-system-functions.html`, `docs/manual/data-types.html`, `docs/manual/scripts/manual.js`, а также зафиксирован предварительный список будущих файловых расширений, завязанных на массивы и двоичную модель.

### Затронутые пути
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/linenumber/basic_usage.axm`
- `samples/functions/skipline/basic_usage.axm`
- `samples/functions/newline/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/manual/file-system-functions.html`
- `docs/manual/data-types.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно;
- выполнен `node --check C:\Axiom\docs\manual\scripts\manual.js` - успешно.

### Следующий точечный шаг
- следующим логичным этапом остаётся усиление массивов как значения языка, потому что от него зависят отложенные файловые, строковые и регулярные функции с возвратом набора значений.

## Session 157 - 2026-04-19 21:44 - Спроектирован roadmap развития одномерных массивов

### Что сделано
- прочитаны актуальные документы проекта и подтверждена текущая точка остановки: следующим крупным этапом после файловой подсистемы должны стать массивы как полноценное значение языка;
- просмотрены текущие реализации и ограничения массивов в `Syntax`, `Semantics`, `Runtime` и пользовательском справочнике;
- отдельно разобраны референсные файлы массива из прошлого языка Axiom, чтобы извлечь удачную структуру подсистемы и не переносить спорные решения вслепую;
- в `docs/PLANS.md` добавлен подробный roadmap именно по одномерным массивам с поэтапной очередностью: foundation, управление формой, массивозависимые функции файловой системы и строк, чистые утилиты, поиск, мутации и higher-order пакет.

### Что решено
- Axiom не должен копировать Axiom буквально: из референса берётся семейная структура API, но не переносятся отрицательная индексация, `unsafe`-режимы и callback-имена строкой;
- фундаментальным шагом признан переход от массива как специального вида переменной к массиву как полноценному значению языка;
- для будущих пустых результатов зафиксировано проектное решение использовать выделенный пустой массив с границами `1 TO 0`, отдельно от невыделенного динамического массива;
- higher-order функции для Axiom в будущем должны строиться поверх уже существующих значений функций и замыканий, а не поверх строковых имён callback-ов.

### Затронутые пути
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- автоматические проверки не запускались, потому что в этой сессии изменялась только документация проекта;
- изменения носят планировочный характер и не меняют текущий исполняемый контракт языка.

### Следующий точечный шаг
- следующим рабочим срезом должен стать foundation-этап одномерных массивов: зафиксировать в спецификации контракт динамического массива как значения языка и затем реализовать минимальный слой `DIM items() AS T` + состояние выделения в `Syntax`, `Semantics` и `Runtime`.

## Session 158 - 2026-04-19 22:05 - Запущен foundation-этап динамических 1D-массивов

### Что сделано
- в `Syntax`, `Parsing`, `Semantics` и `Runtime` реализован первый foundation-срез динамического одномерного массива: теперь язык принимает форму `DIM arr() AS TYPE` без немедленного выделения диапазона;
- рантайм получил отдельное состояние невыделенного динамического массива; попытка индексировать такой массив до выделения теперь даёт runtime-диагностику `MB3005` вместо внутренней аварийной модели состояния;
- обновлены спецификация и пользовательская справка для нового контракта, добавлен runnable acceptance-пример `samples/operators/dim/dynamic_array_declaration.axm`;
- добавлены и обновлены parser-, binder- и runtime-тесты на новую форму объявления и на ошибку доступа к невыделенному массиву.

### Затронутые пути
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/manual/arrays.html`
- `docs/manual/diagnostics.html`
- `samples/operators/dim/dynamic_array_declaration.axm`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Проверка
- выполнен `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore` - успешно;
- выполнен `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore` - успешно.

### Что решено
- `DIM arr() AS TYPE` трактуется как объявление динамического, но ещё не выделенного массива;
- индексирование такого массива до будущего `REDIM` не разрешается и диагностируется как `MB3005`;
- это только первый foundation-подшаг, а не полный переход массива к модели first-class значения языка.

### Следующий точечный шаг
- следующим подшагом должен стать переход от массива как специального вида переменной к массиву как полноценному значению языка: отдельный array-type в `Semantics`, присваивание массива целиком, передача в параметры и возврат из `FUNCTION`; только после этого имеет смысл переходить к `REDIM`, `REDIM PRESERVE` и `ERASE`.

## Session 159 - 2026-04-20 07:54 - Arrays became first-class values

### What changed
- Introduced explicit array typing across the frontend with `ArrayTypeSyntax` and `ArrayTypeSymbol`.
- Promoted arrays from a special variable flag to a first-class language value model.
- Added whole-array assignment, array parameter passing, and array return from `FUNCTION`.
- Preserved the existing named-array indexing model and kept `DIM name() AS TYPE` as the declaration form.
- Added the runnable acceptance sample `samples/smoke/array_first_class_values.axm`.
- Updated the user manual pages for arrays and `FUNCTION`, plus repository specs and plans.

### Files changed
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/ProgramAndMembers.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/manual/arrays.html`
- `docs/manual/function.html`
- `samples/smoke/array_first_class_values.axm`
- `docs/PLANS.md`
- `docs/WORKLOG.md`

### Verification
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore`

### Decisions
- Type-position array syntax is `TYPE()`, for example `INTEGER()` in parameters and function results.
- Whole-array assignment and parameter passing currently use shared-reference semantics.
- General postfix indexing of arbitrary expressions is still deferred.

### Next exact step
- Implement `REDIM` as the next lifecycle operation for dynamic 1D arrays, then layer `REDIM PRESERVE`, `ERASE`, and informational array built-ins on top of that base.

## Session 160 - 2026-04-20 08:10 - REDIM for one-dimensional arrays

### What changed
- Added the `REDIM` statement as the first lifecycle operation for one-dimensional arrays.
- Implemented parser, binder, and runtime support for `REDIM name(lower TO upper)`.
- `REDIM` now allocates a previously unallocated dynamic array and reallocates an existing array without preserving old elements.
- Preserved the current shared-reference array model, so reallocating an aliased array affects every alias of the same runtime array.
- Added the runnable acceptance sample `samples/operators/redim/basic_usage.axm`.
- Updated the array manual page, manual index, specification, architecture notes, and plans.

### Files changed
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/operators/redim/basic_usage.axm`

### Verification
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`

### Decisions
- The first `REDIM` slice supports only the base form without `PRESERVE`.
- Bounds stay explicit as `lower TO upper`; shorthand forms remain deferred.
- Non-`PRESERVE` `REDIM` resets element storage instead of attempting partial retention.

### Next exact step
- Implement `REDIM PRESERVE` so arrays can change bounds while keeping the overlapping portion of existing elements.

## Session 161 - 2026-04-20 08:23 - REDIM PRESERVE for one-dimensional arrays

### What changed
- Added the `REDIM PRESERVE` form for one-dimensional arrays.
- Implemented parser, binder, and runtime support for `REDIM PRESERVE name(lower TO upper)`.
- `REDIM PRESERVE` now reallocates array storage while preserving the overlapping logical index range between old and new bounds.
- Kept the current shared-reference array model, so preserving through an alias affects every alias of the same runtime array.
- Added the runnable acceptance sample `samples/operators/redim/preserve_basic_usage.axm`.
- Updated the array manual page, manual index, specification, architecture notes, and plans.

### Files changed
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/operators/redim/preserve_basic_usage.axm`

### Verification
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Decisions
- `REDIM` and `REDIM PRESERVE` share the same statement node and bound node, with preservation modeled as an explicit flag instead of a separate lifecycle construct.
- Preservation copies the logical intersection of old and new bounds; it does not shift element values by physical offset.
- `REDIM PRESERVE` on an unallocated dynamic array behaves like an allocation and succeeds without a special error path.

### Next exact step
- Implement `ERASE` as the next lifecycle operation for one-dimensional arrays.

## Session 162 - 2026-04-20 08:29 - ERASE for one-dimensional arrays

### What changed
- Added the `ERASE` statement for one-dimensional arrays.
- Implemented parser, binder, and runtime support for `ERASE name`.
- `ERASE` now returns an array to the unallocated state, so indexing it again requires a later `REDIM` or `REDIM PRESERVE`.
- Kept the current shared-reference array model, so erasing through an alias affects every alias of the same runtime array.
- Added the runnable acceptance sample `samples/operators/erase/basic_usage.axm`.
- Updated the array manual page, manual index, specification, architecture notes, and plans.

### Files changed
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Parsing/Parser.Statements.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/BoundNodes/BoundStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/operators/erase/basic_usage.axm`

### Verification
- `dotnet test C:\Axiom\tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Decisions
- The first `ERASE` slice supports only one array name per statement.
- `ERASE` resets the array specifically to the unallocated state, not to a zero-length allocated range.
- Alias semantics stay consistent with `REDIM` and `REDIM PRESERVE`: lifecycle operations mutate the shared runtime array object in place.

### Next exact step
- Add the first informational array built-ins: `LBOUND`, `UBOUND`, `ARRAYLENGTH`, and `ARRAYISALLOCATED`.

## Session 163 - 2026-04-20 08:42 - Информационные встроенные функции одномерных массивов

### Что изменилось
- Добавлены встроенные функции `LBOUND`, `UBOUND`, `ARRAYLENGTH` и `ARRAYISALLOCATED` для одномерных массивов.
- В слое семантики добавлен отдельный путь связывания для информационных функций массива, чтобы они принимали массивы с любым типом элемента.
- В слое выполнения реализовано текущее поведение жизненного цикла:
  - `LBOUND` и `UBOUND` требуют уже выделенный массив и при обращении к невыделенному массиву сообщают `MB3005`;
  - `ARRAYLENGTH` для невыделенного массива возвращает `0`;
  - `ARRAYISALLOCATED` для невыделенного массива возвращает `FALSE`.
- Обновлены руководство пользователя, алфавитный указатель справки, спецификация, архитектурные заметки и план.
- Добавлены запускаемые примеры для каждой новой функции.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/functions/lbound/basic_usage.axm`
- `samples/functions/ubound/basic_usage.axm`
- `samples/functions/arraylength/basic_usage.axm`
- `samples/functions/arrayisallocated/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Для невыделенного массива `LBOUND` и `UBOUND` остаются недопустимыми, чтобы программа не принимала отсутствие диапазона за корректную границу.
- Для безопасной проверки состояния `ARRAYLENGTH` и `ARRAYISALLOCATED` специально допускаются на невыделенном массиве.
- Следующим шагом нужно отделить пустой, но выделенный массив от полностью невыделенного состояния, потому что без этого будущие функции, возвращающие массивы, не смогут корректно возвращать пустой результат.

### Следующий точный шаг
- Ввести в модель времени выполнения и в контракт языка пустой, но уже выделенный одномерный массив в нормальной форме `1 TO 0`.

## Session 164 - 2026-04-20 08:56 - Пустой выделенный одномерный массив

### Что изменилось
- Введено отдельное состояние пустого, но уже выделенного одномерного массива.
- Нормальная пустая форма закреплена как `1 TO 0`.
- `DIM name(1 TO 0) AS TYPE`, `REDIM name(1 TO 0)` и `REDIM PRESERVE name(1 TO 0)` теперь создают выделенный массив с длиной `0`.
- `ARRAYISALLOCATED` для такого массива возвращает `TRUE`, `ARRAYLENGTH` возвращает `0`, `LBOUND` возвращает `1`, а `UBOUND` возвращает `0`.
- `ERASE` не менялся по смыслу: он по-прежнему возвращает массив в невыделенное состояние.
- Другие обратные диапазоны, например `5 TO 3`, остаются ошибкой времени выполнения.
- Обновлены руководство, алфавитный указатель справки, спецификация, архитектурные заметки и план.
- Добавлен запускаемый пример `samples/operators/redim/empty_array.axm`.

### Какие файлы изменены
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/operators/redim/empty_array.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Пустой выделенный массив не равен невыделенному массиву: он имеет границы, нулевую длину и считается выделенным.
- Единственная допустимая пустая форма сейчас — `1 TO 0`.
- Поддержка пустого выделенного массива нужна до функций, которые возвращают массивы, чтобы пустой результат не смешивался с состоянием после `ERASE`.

### Следующий точный шаг
- Начать минимальный набор функций, возвращающих одномерные массивы, со строки: реализовать `SPLIT` и затем `JOIN`.

## Session 165 - 2026-04-20 09:05 - SPLIT и JOIN

### Что изменилось
- Добавлены строковые функции `SPLIT` и `JOIN`.
- `SPLIT` возвращает новый одномерный массив строк:
  - непустой результат получает границы `1 TO N`;
  - пустая исходная строка возвращает пустой выделенный массив `1 TO 0`;
  - пустые части между разделителями сохраняются;
  - пустой разделитель даёт диагностику времени выполнения `MB3007`.
- `JOIN` принимает массив `STRING()` и собирает элементы в строку.
- Для `JOIN` невыделенный массив и пустой выделенный массив возвращают пустую строку.
- Обновлены руководство пользователя, алфавитный указатель справки, обзорная страница раздела, спецификация, архитектурные заметки и план.
- Добавлены запускаемые примеры для `SPLIT` и `JOIN`.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.String.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/string-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/functions/split/basic_usage.axm`
- `samples/functions/split/empty_text.axm`
- `samples/functions/join/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Разделитель по умолчанию для обеих функций — запятая.
- `SPLIT("", separator)` возвращает пустой выделенный массив, а не массив из одной пустой строки.
- `JOIN` не считает невыделенный строковый массив ошибкой и возвращает пустую строку, чтобы его можно было безопасно применять к результатам и промежуточным массивам.

### Следующий точный шаг
- Реализовать файловые функции `GETFILES` и `GETDIRS`, возвращающие `STRING()`.

## Session 166 - 2026-04-20 09:23 - GETFILES и GETDIRS

### Что изменилось
- Добавлены файловые функции `GETFILES` и `GETDIRS`, возвращающие одномерный массив строк.
- Поддержаны формы `GETFILES(path)`, `GETFILES(path, pattern)`, `GETDIRS(path)` и `GETDIRS(path, pattern)`.
- Результат содержит полные пути файлов или папок ближайшего уровня указанной папки.
- Результат сортируется без учёта регистра.
- Пустой результат возвращается как выделенный пустой массив `1 TO 0`.
- Отсутствующая исходная папка, пустая маска и ошибки файловой системы дают диагностику времени выполнения `MB3007`.
- Обновлены справочник пользователя, указатель справки, спецификация, архитектурные заметки и план.
- Добавлены запускаемые примеры для `GETFILES` и `GETDIRS`.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/functions/getfiles/basic_usage.axm`
- `samples/functions/getdirs/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Маска по умолчанию для обеих функций — `*`.
- Перечисление не является рекурсивным.
- Пустая маска запрещена явно, чтобы не смешивать ошибочный вызов с корректным пустым результатом.
- Доступ к элементам результата по-прежнему выполняется через присваивание результата переменной массива и индексирование этой переменной.

### Следующий точный шаг
- Реализовать `READALLLINES`, возвращающую `STRING()` для строк текстового файла.
- После этого перейти к `READLINES` для чтения набора строк из уже открытого текстового файла.

## Session 167 - 2026-04-20 09:42 - READALLLINES

### Что изменилось
- Добавлена файловая функция `READALLLINES`, возвращающая одномерный массив строк.
- Функция читает весь текстовый файл как UTF-8 и возвращает строки без символов перевода строки.
- Непустой результат получает границы массива `1 TO N`.
- Пустой файл возвращается как выделенный пустой массив `1 TO 0`.
- Отсутствующий файл, недопустимый путь, отказ доступа и другие ошибки файловой системы дают диагностику времени выполнения `MB3007`.
- Обновлены справочник пользователя, указатель справки, спецификация, архитектурные заметки и план.
- Добавлены запускаемые примеры для обычного и пустого файла.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/file-system-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/functions/readalllines/basic_usage.axm`
- `samples/functions/readalllines/empty_file.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `READALLLINES` использует тот же способ чтения текста UTF-8, что и остальные текстовые файловые функции.
- Символы перевода строки не входят в элементы результата.
- Пустой файл считается корректным пустым результатом, а не ошибкой.

### Следующий точный шаг
- Реализовать `READLINES` для чтения набора строк из уже открытого текстового файла.
- Сохранить договор результата `STRING()`: непустой результат `1 TO N`, пустой результат `1 TO 0`.

## Session 168 - 2026-04-20 09:51 - READLINES

### Что изменилось
- Добавлена функция `READLINES`, возвращающая одномерный массив строк из уже открытого текстового файла.
- Функция читает до указанного числа следующих строк из файла, открытого через `OPEN ... FOR INPUT AS file`.
- Строки возвращаются без символов перевода строки.
- Непустой результат получает границы массива `1 TO N`.
- `count = 0` возвращает выделенный пустой массив `1 TO 0` и не меняет позицию чтения.
- Конец файла до чтения первой строки возвращает выделенный пустой массив `1 TO 0`.
- Отрицательный `count`, закрытый файл, неправильный режим файла и ошибки чтения дают диагностику времени выполнения `MB3007`.
- Обновлены справочник пользователя, указатель справки, спецификация, архитектурные заметки и план.
- Добавлены запускаемые примеры для обычного чтения, нулевого количества строк и конца файла.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.FileIO.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `src/Axiom.Runtime/Values/RuntimeTextFile.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/file-system-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/index.html`
- `docs/manual/reference.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `samples/functions/readlines/basic_usage.axm`
- `samples/functions/readlines/zero_and_end.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `READLINES` читает не ровно `count` строк, а до `count` строк, чтобы конец файла не считался ошибкой для пакетного чтения.
- `READLINES(f, 0)` является корректным пустым чтением и не сдвигает позицию файла.
- При ошибке чтения внутри набора строк позиция открытого файла восстанавливается к началу вызова.

### Следующий точный шаг
- Перейти к чистым операциям над одномерными массивами.
- Перед первой реализацией зафиксировать короткий контракт для `ARRAYCLONE` и `ARRAYSLICE`: какие границы сохраняются, когда возвращается компактный диапазон `1 TO N`, и как обрабатывается пустой результат.
