# AGENTS.md

## Purpose
This repository must be developed in a structured, document-driven way.

Your job is not only to write code.
Your job is to:
1. read project context first;
2. bootstrap missing project files and folders when needed;
3. keep project state in repository documents;
4. resume work in new sessions from repository documents instead of chat history;
5. keep implementation aligned with the project description.

Repository files are the source of truth.
Do not rely on chat memory as the source of truth.

Project documents may be written in Russian.
Treat Russian project documents as the authoritative source.
Do not translate them unless explicitly asked.

---

## Mandatory startup workflow

At the beginning of every session, do this in order:

1. Read `AGENTS.md`
2. Read `PROJECT.md`
3. If they exist, read:
   - `README.md`
   - `docs/SPECIFICATION3.md`
   - `docs/ARCHITECTURE.md`
   - `docs/PLANS4.md`
   - `docs/WORKLOG5.md`
   - `docs/BYTECODE_COVERAGE.md`
   - `docs/BYTECODE_PERF_MATRIX.md`
   - `versions.json`, `docs/VERSIONING.md` и последнюю запись `docs/CHANGELOG.md`
   - `docs/archive/spec/SPECIFICATION2.md`, `docs/archive/plans/PLANS3.md`, `docs/archive/plans/PLANS2.md`, `docs/archive/worklog/WORKLOG3.md`, `docs/archive/spec/SPECIFICATION.md`, `docs/archive/plans/PLANS.md`, `docs/archive/worklog/WORKLOG2.md`, `docs/archive/worklog/WORKLOG.md` only when older historical context is needed

Note: `docs/archive/`, `docs/reports/` and `docs/audit/` are kept only on the developer's disk and are not published to the repository (listed in `.gitignore`). They may be absent in a fresh clone; treat their absence as expected and do not recreate them from the repository alone.

Then:
1. briefly summarize what this project is;
2. state current status;
3. identify where work stopped;
4. identify the next logical step;
5. only then begin changes.

Do not start coding before reading the required project files.

---

## Bootstrap mode

Use Bootstrap mode if the repository is empty or key project files are missing.

If the following files do not exist, create them:

- `README.md`
- `docs/SPECIFICATION3.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS4.md`
- `docs/WORKLOG5.md`

If the following folders do not exist, create them:

- `docs/`
- `src/`
- `tests/`

Rules for Bootstrap mode:
- Use `PROJECT.md` as the product source of truth.
- Do not invent critical product requirements that are not supported by `PROJECT.md`.
- If some implementation detail is unknown, mark it clearly as an open question or placeholder.
- Create only the necessary structure.
- Do not generate random framework boilerplate unless the user asked for actual implementation.

At the end of Bootstrap mode:
1. show the created file tree;
2. explain what each created file is for in brief;
3. record the current state in `docs/WORKLOG5.md`;
4. record the next step in `docs/PLANS4.md`.

---

## Resume mode

Use Resume mode when the repository already contains project documents.

Rules for Resume mode:
- Read the required files first.
- Continue from the latest plan instead of inventing a new direction.
- Prefer the next unfinished item from `docs/PLANS4.md`.
- If documentation is stale, update it before or together with code changes.

---

## File roles

### `PROJECT.md`
Contains the product definition:
- what the project is;
- who it is for;
- goals;
- scope;
- constraints;
- preferred stack;
- направление продукта.

### `README.md`
Human-facing repository entry point for the GitHub main page:
- what the project is;
- why it exists and for whom;
- what the language already supports;
- quick start;
- repository layout.

This file is written for outside readers, not for internal workflow. Keep it
friendly and free of internal audit/plan links; deep internal documents stay in
`docs/` and are linked only from the small documentation section.

### `docs/DEVELOPMENT.md`
Developer and maintenance guide:
- how to build and run from sources;
- how to run tests, checks and version tooling;
- portable distribution and editor parameters;
- anything that used to live in the technical README.

When build, test or version commands change, update this file instead of
returning that raw technical detail to `README.md`.

### `docs/SPECIFICATION3.md`
Implementation source of truth.
For each feature or module, document:
- user stories;
- data model;
- API or actions;
- UI/screens if relevant;
- business rules;
- edge cases.

### `docs/ARCHITECTURE.md`
Technical structure and major decisions:
- stack;
- modules;
- layer boundaries;
- key architectural choices.

### `docs/PLANS4.md`
Current work plan:
- current milestone;
- next tasks in order;
- blockers;
- deferred items;
- append-only ordering from top to bottom for all evolving sections unless the document explicitly says otherwise.

### `docs/archive/plans/PLANS3.md`
Historical third part of planning:
- kept for historical context before `docs/PLANS4.md` became active;
- no new planning updates should be added there after the switch.

### `docs/archive/spec/SPECIFICATION2.md`
Historical second part of specification:
- kept for historical context before `docs/SPECIFICATION3.md` became active;
- no new updates should be added there after the switch.

### `docs/archive/spec/SPECIFICATION.md`
Historical first part of specification:
- kept for historical context before `docs/SPECIFICATION3.md` became active;
- no new updates should be added there after the switch.

### `docs/archive/plans/PLANS2.md`
Historical second part of planning:
- kept for historical context before `docs/archive/plans/PLANS3.md` became active;
- no new planning updates should be added there after the switch.

### `docs/archive/plans/PLANS.md`
Historical first part of planning:
- kept for historical context before `docs/archive/plans/PLANS2.md` became active;
- no new planning updates should be added there after the switch.

### `docs/archive/worklog/WORKLOG3.md`
Historical third part of session continuity:
- kept for sessions from 310 to 480;
- no new entries should be added there after `docs/WORKLOG5.md` is introduced;
- read it when context from sessions 310-480 is needed.

### `docs/archive/worklog/WORKLOG2.md`
Historical second part of session continuity:
- kept for sessions from 169 to 309;
- no new entries should be added there after `docs/WORKLOG5.md` is introduced;
- read it when context from sessions 169-309 is needed.

### `docs/archive/worklog/WORKLOG.md`
Historical session continuity log:
- kept for sessions up to and including Session 168;
- no new entries should be added there after `docs/WORKLOG5.md` is introduced;
- read it only when older historical context is needed.

### `docs/archive/worklog/WORKLOG4.md`
Historical fourth part of session continuity:
- kept for sessions from 481 to 1031;
- no new entries should be added there after `docs/WORKLOG5.md` is introduced;
- read it when context from sessions 481-1031 is needed.

### `docs/WORKLOG5.md`
Active session continuity log:
- what changed;
- which files changed;
- what was decided;
- what remains;
- exact next step;
- strict chronological ordering from top to bottom;
- new session entries must be appended only at the end of the file;
- starting from Session 1032, every new session entry must include a sequential session number plus exact local date and time in the heading.

### `docs/BYTECODE_COVERAGE.md`
Active registry of bytecode coverage:
- which user-visible operators, built-ins and language forms are covered by `BytecodeVm`;
- whether they have automated tests;
- whether they have runnable `.axm` samples;
- whether they are reflected in `docs/manual/`;
- whether the exact form has independent BytecodeVm expectations;
- whether the area is performance-hot or only functionally covered.

### `docs/BYTECODE_PERF_MATRIX.md`
Active registry of performance scenarios:
- which language families already have dedicated `samples/smoke/engine-compare/` scenarios;
- which families are covered only indirectly through mixed scenarios;
- which families still have no meaningful performance scenario;
- which gaps are blocked by current harness limits such as interactive input.

---

## Core working rules

### 0. Обязательное сопровождение версий

Единственный источник номеров — `versions.json`; порядок — `docs/VERSIONING.md`. Повышать только версии компонентов, чья реализация или пользовательский договор действительно изменились: изменение языка повышает язык и компилятор; изменение CLI или компилятора без изменения языка повышает компилятор; изменение Electron/служб IDE повышает IDE; совмещённая задача повышает все фактически затронутые номера. Изменение, запуск или проверка `.axm`-примера само по себе не меняет версию языка, компилятора или IDE. Правка проектной документации, журнала или правил версионирования также не повышает продуктовые версии. Обычно использовать `node scripts/versioning.mjs bump <language|compiler|ide|all> patch "Описание"`; новая подсистема/несовместимость до 1.0 — `minor`. Переход 1.0.0/MAJOR требует прямого решения автора. Повторные попытки внутри одной задачи не являются отдельной версией.

Перед завершением выполнить `node scripts/versioning.mjs check` и `node scripts/versioning.mjs check --base HEAD`, записать номера в WORKLOG и итоговый отчёт. При подготовке окружения подключить `.githooks/pre-commit` командой `node scripts/versioning.mjs install-hooks`; обработчик проверяет подготовленный набор. Производные Versions.props/AxiomVersionInfo.cs/npm-номера/бейджи и строка версий в README вручную не менять: их обновляет `bump`, а `check` обнаруживает расхождение. Примеры, документация, журналы, результаты проверок и изменения правил версионирования не требуют повышения продуктовых версий. Изменения реализации повышают только соответствующие ей компоненты.

### 1. Read before acting
Always read `PROJECT.md` before proposing project structure or implementation.

### 2. Minimal correct changes
Prefer small, scoped, reviewable changes.
Do not rewrite unrelated parts of the project.

### 3. No silent invention
If a missing detail is critical and cannot be inferred from repository files, do not present guesses as facts.
Mark them explicitly.

### 4. Docs and code must match
If code changes behavior, structure, commands, or scope, update the relevant docs in the same task.

For this repository, when a task adds or changes:
- an operator;
- a built-in function;
- a documented callable form;
- or observable language behavior;

the same task must also update the user-facing manual in `docs/manual/`.

Minimum required manual follow-up:
- update the relevant reference page, or the nearest correct manual page if no separate page exists yet;
- update manual navigation/index metadata when the new entity should be discoverable through the manual index;
- do not leave the new behavior documented only in code comments, tests, `samples/`, `docs/SPECIFICATION3.md`, or chat output.
- if an existing operator or built-in receives a new callable form, a new multidimensional mode, a new parameter contract, or another user-visible capability, reflect that capability directly on the manual page itself, not only through an external `.axm` sample link or a brief mention elsewhere;
- when such a new callable form or multidimensional mode is important enough to confuse a reader without an example, add or update an inline manual example for that exact form on the same manual page.

### 4.0. IDE-подсветка должна идти в ногу с языком
Для этого репозитория каждая новая пользовательская языковая сущность должна в том же задании получать поддержку IDE-подсветки.

Постоянные требования:
- если задача добавляет или меняет оператор, встроенную функцию, тип данных или другой пользовательский синтаксический токен, в том же задании нужно обновить IDE-подсветку;
- нужно обновлять словарь Monaco, категории токенов и семантическую раскраску Electron: `ide/src/renderer/lang-features.js`, `semantic-colors.js` и связанные темы;
- сущность должна попасть в правильную категорию подсветки: операторы и ключевые слова, типы данных, встроенные функции;
- нужно обновить или добавить проверки в `ide/tests/` и `ide/e2e/specs/`, чтобы новые слова были покрыты через словарь Monaco, форматирование и реальный редактор;
- нельзя оставлять runnable `.axm`-примеры в состоянии, где новый оператор или новая функция уже работает в языке, но не раскрашивается в IDE.

### 4.1. Редакционная политика справочника

Для этого репозитория `docs/manual/` ведётся как формальный справочник самостоятельного языка Axiom. Внешние редакционные образцы, книги и материалы других языков не задают обязательного устройства страницы и не входят в пользовательские объяснения как сравнения.

Порядок работы:
- единый редакционный договор находится в `docs/manual/AUTHORING.md`;
- операционное применение договора выполняет локальный проектный навык `.opencode/skills/axiom-manual-editor/SKILL.md`; факультативный `axiom-technical-phrasing` помогает выбирать русские формулировки, но не задаёт семантику или шаблон статьи; каталог `.opencode/` исключён из Git, поэтому правила справочника действуют и без него;
- сторонние редакционные навыки из `.opencode/skills/` вспомогательны: при конфликте с проверенной семантикой Axiom, действующим договором справочника или прямым решением автора они уступают;
- структура конкретной страницы выбирается по смыслу темы, а обязательный набор сведений определяется разделом 4.1.b.

Границы источников:
- факты устанавливаются по спецификации, реестру, реализации, тестам и запускаемым примерам;
- внешние страницы допустимы как справочный материал для организации текста, но не как источник семантики Axiom;
- нельзя переносить в справочник неподтверждённое поведение, коды ошибок или формы вызова.

### 4.1.b. Обязательное содержание страниц manual

Для этого репозитория страница manual, которая описывает оператор, встроенную функцию, тип данных или небольшую тематическую группу тесно связанных сущностей, обязана содержать полный набор сведений. Порядок изложения, число подзаголовков и место примера определяются темой и потребностями читателя, а не единым шаблоном.

Обязательные сведения для страницы об одной сущности:
- что это за сущность, названное прямо и однозначно по смыслу;
- для чего она нужна и в каком контексте применяется;
- все реализованные формы синтаксиса, показанные раздельно;
- назначение и типовой договор каждого параметра или аргумента;
- выполняемое действие или возвращаемый результат;
- правила работы, ограничения и значимые пограничные случаи;
- диагностика по случаям, если сущность её выдаёт;
- короткий проверенный пример, а при детерминированном результате — ожидаемый вывод;
- содержательные перекрёстные ссылки, если есть связанные страницы.

Постоянные требования к изложению:
- сущность определяется по смыслу, а не повторением одной и той же вступительной формулы на каждой странице; повторение названия ради шаблона запрещено, а ясное называние предмета обязательно;
- нельзя начинать страницу с абстрактной фразы вроде «открывает поверхность», «задаёт режим», «используется для работы», если из неё читатель не узнаёт, о какой сущности идёт речь;
- если страница описывает группу сущностей, в начале перечисляется, что именно собрано на странице, и отдельно указывается, что синтаксис ниже относится к разным сущностям;
- используется связная техническая проза полноценными предложениями, а не телеграфный стиль и не набор одинаковых карточек;
- у каждого предложения должна быть содержательная функция; тавтология, повтор заголовка во вступительном абзаце и пустые связки удаляются;
- пример располагается рядом с правилом, которое он поясняет, если это яснее для читателя; отдельный раздел примеров в конце страницы остаётся допустимым, но не обязательным;
- в каждом месте, где читатель может потерять предмет разговора, сущность называется явно, а не заменяется безличными словами `это`, `данный режим`, `данная форма`;
- для графики обязательно явно объяснять, в каких единицах измеряются координаты, от какой точки они считаются, что означает цвет, как ведёт себя сущность на границах экрана или холста и когда результат становится видимым;
- для файловых средств обязательно явно объяснять, с какими путями идёт работа, что происходит при отсутствии файла или каталога, что создаётся автоматически, а что нет;
- для строковых функций обязательно явно объяснять единицу измерения результата, поведение на пустой строке и поведение на выходе за границы;
- если есть несколько синтаксических форм, они должны быть визуально разделены и затем объяснены по отдельности; нельзя сваливать разные формы в один общий блок так, будто это одна конструкция;
- если аргумент имеет важный типовой договор, это надо писать прямо: `аргумент должен быть строкой`, `координаты должны быть целыми числами`, `ширина задаётся в пикселях` и так далее;
- если сущность выдаёт конкретные диагностики, они должны быть перечислены по случаям, а не только общим списком кодов.

Требования для обзорной страницы раздела:
- обзорная страница прямо сообщает, что это за раздел и какие группы сущностей в него входят;
- обзорная страница не подменяет атомарные страницы длинными смешанными списками синтаксиса;
- обзорная страница сначала объясняет структуру раздела, а затем направляет читателя к отдельным страницам операторов, функций и подсистем.

Запрещено независимо от композиции страницы:
- выдавать неподтверждённое поведение за договор языка;
- добавлять необоснованные сравнения с другими языками, исторические отступления и учебные задания;
- размывать справочный жанр разговорной или рекламной подачей.

Полный редакционный договор находится в `docs/manual/AUTHORING.md`; операционное применение этих правил выполняет проектный навык `.opencode/skills/axiom-manual-editor/SKILL.md`.

### 5. Validation after code changes
After code changes, run the most relevant checks available:
- tests
- lint
- typecheck
- build
- smoke-check

If checks cannot be run yet, state that explicitly.

### 5.0. Run the smallest relevant test group
For this repository, checks are grouped by layer and area. The catalog and selection rules are in `docs/TESTING.md`; the unified entry point is `scripts/test.ps1 <group>`.

Permanent requirements:
- identify the affected layer first, then run the smallest matching group (for example `lexer` for tokenization, `ide-unit-renderer` for a renderer module, `e2e-editor` for editor behavior);
- do not run the whole suite (`all`) for a local change unless the user explicitly asks for a full run or the change spans several layers;
- do not run unrelated layers "just in case": lexer tests are not evidence for IDE behavior and IDE scenarios are not evidence for parser changes;
- for a long Electron run use `-Out` and, when continuing an interrupted run, `--from <id>` so the result is not lost;
- report which groups were run and which were skipped.

### 5.1. .NET checks must not share `obj` in parallel
For this repository, do not run multiple `dotnet build`, `dotnet test`, `dotnet run` or other MSBuild-based commands in parallel when they can write to the same intermediate `obj` tree.

Permanent requirements:
- default to sequential `dotnet` validation commands;
- parallel `dotnet` validation is allowed only when every concurrent command has an explicitly isolated intermediate directory, not only a separate `OutputPath`;
- if isolation is needed, set a unique `BaseIntermediateOutputPath` or equivalent MSBuild intermediate path for each concurrent command;
- do not treat file-lock failures from shared `obj` as an acceptable normal workflow; prevent them by command ordering or true intermediate-directory isolation.

### 6. Worklog is mandatory
After every meaningful task, update `docs/WORKLOG5.md`.

### 7. Repository documents are append-only
All repository documents must be maintained from top to bottom.

Permanent requirements:
- never prepend new records to the top of `docs/WORKLOG5.md`, `docs/archive/worklog/WORKLOG4.md`, `docs/archive/worklog/WORKLOG3.md`, `docs/archive/worklog/WORKLOG2.md`, `docs/archive/worklog/WORKLOG.md`, `docs/PLANS4.md`, `docs/archive/plans/PLANS3.md`, `docs/archive/plans/PLANS2.md`, `docs/archive/plans/PLANS.md`, or any other evolving project document;
- add new records only below the existing records in the same document;
- preserve chronological reading order: earlier entries stay above later entries;
- do not silently reorder old records unless the task is explicitly to normalize document chronology.

For `docs/WORKLOG5.md`, continuing the sequence from `docs/archive/worklog/WORKLOG.md`, `docs/archive/worklog/WORKLOG2.md`, `docs/archive/worklog/WORKLOG3.md` and `docs/archive/worklog/WORKLOG4.md`, every new entry:
- every new entry must use a heading in the form `## Session <N> - YYYY-MM-DD HH:mm - <title>`;
- `<N>` must be the next sequential session number after the highest existing numbered session;
- date and time are mandatory and must use the local project time zone;
- historical entries created before this rule may remain in the legacy format, but they must not be renumbered with invented timestamps.

### 8. Plans must stay current
If the next steps change, update `docs/PLANS4.md`.

### 8.1. Bytecode coverage registry must stay current
If a task changes bytecode coverage, parity, acceptance samples, manual coverage, or profiling status for a user-visible language form, update `docs/BYTECODE_COVERAGE.md` in the same task.

### 8.2. Bytecode performance matrix must stay current
If a task adds, removes, or materially changes a benchmark family, an `engine-compare` scenario, or the performance-scenario status of a language family, update `docs/BYTECODE_PERF_MATRIX.md` in the same task.

### 9. Architecture changes must be recorded
If technical structure changes materially, update `docs/ARCHITECTURE.md`.

### 9.1. Repository prose must stay Russian
For this repository, all new prose written by Codex in repository documents and final task reports must default to Russian.

Permanent requirements:
- do not insert unnecessary English words or anglicisms into Russian prose when there is a normal Russian equivalent;
- words like `milestone`, `helper layer`, `runtime slice`, `cleanup`, `polish`, `follow-up`, `retrieval layer` and similar mixed phrasing must be rewritten in normal Russian;
- English is allowed only where it is part of a code identifier, file path, command, API name, language keyword, or another exact technical token that should stay verbatim;
- if an older document already contains mixed-language prose, new additions must still prefer clean Russian instead of copying that style further.

### 9.2. Names of operators and built-ins must follow BASIC lineage
For this repository, names of new operators, built-in functions, and other user-visible callable language forms must be chosen by first studying the historical BASIC family.

Permanent requirements:
- before introducing a new user-visible name, inspect the corresponding naming in classic BASIC, QuickBASIC, Visual Basic, and when useful Turbo Basic, QB64, or other close BASIC-family lines;
- prefer the established BASIC-family name when it fits the intended behavior honestly and does not mislead the user about semantics;
- if the BASIC family has no suitable established name for the feature, prefer an already proven name from the author's older Axiom-related project or another explicitly acknowledged project-local precedent instead of inventing a brand-new label from scratch;
- do not copy names mechanically from unrelated languages when there is already a suitable BASIC-family precedent;
- do not invent unique new names only for originality if a clear BASIC-family name or an explicit project-local precedent already exists;
- if an inherited name is kept but the Axiom behavior is narrower or otherwise different, document that difference explicitly in repository docs and user-facing manual in the same task;
- if BASIC-family sources disagree, or if the fallback comes from an older project rather than historical BASIC itself, record the naming choice explicitly as a project decision instead of presenting it as an obvious default.

### 10. Runnable `.axm` acceptance samples are mandatory for language behavior changes
If a task changes observable language behavior, Codex must add or update a runnable `.axm` acceptance sample in the repository.

This is a permanent workflow rule for:
- new operators;
- new built-in functions;
- changes in semantics of existing language constructs;
- changes to `PRINT`, `INPUT`, control flow, expressions, and built-ins.

Mandatory requirements:
- automated tests alone are not enough when language behavior changes;
- add or update a short runnable `.axm` program that can be opened in the Electron IDE and run manually;
- place the sample in the correct thematic subfolder under `samples/`;
- use the sample as a manual acceptance test for the changed behavior;
- mention the sample explicitly in the final task report.

Required sample structure:
- `samples/operators/<operator-name>/`
- `samples/functions/<function-name>/`
- `samples/smoke/`
- when appropriate: `samples/control-flow/<feature-name>/`
- when appropriate: `samples/expressions/<feature-name>/`

Structure rules:
- one operator = its own folder, for example `samples/operators/print/`, `samples/operators/input/`, `samples/operators/if/`;
- one built-in function = its own folder, for example `samples/functions/len/`, `samples/functions/left/`, `samples/functions/val/`;
- inside each folder keep only tests related to that entity;
- if a sample covers mixed behavior, place it in the best-fitting primary folder or in `samples/smoke/` for integration scenarios;
- flat `samples/` layouts without thematic separation are forbidden for new language-behavior samples.

Naming and content rules:
- file names must be short, readable, and behavior-oriented;
- generic names like `basic_usage.axm`, `demo.axm`, `example.axm`, `test.axm` are forbidden for new samples;
- examples for `print`: `basic_expression.axm`, `empty_print.axm`, `separator_semicolon.axm`, `separator_comma.axm`, `mixed_list.axm`;
- examples for `input`: `single_variable.axm`, `multiple_variables.axm`, `prompt_semicolon.axm`, `prompt_comma.axm`;
- smoke and integration scenarios must live under `samples/smoke/`;
- each sample must stay short, focused, and manually runnable in the IDE;
- one file should validate one primary behavior or one very small related set of behaviors;
- if needed, expected behavior may be documented at the top of the file in comments;
- acceptance samples complement automated tests and do not replace them.

### 10.1. Примеры `.axm` должны быть русскоязычными и пошагово понятными

Для всех новых и обновляемых запускаемых `.axm`-примеров действует постоянное правило:
- пользовательские строки вывода, подсказки, комментарии и пояснения внутри примера пишутся на русском языке;
- английский язык допускается только там, где его требует сам язык Axiom, имя оператора, встроенной функции, путь к файлу, формат файла или другой точный технический токен;
- если пример показывает визуальное, графическое, интерактивное или пошаговое поведение, он должен использовать `SLEEP`, ожидание клавиши или другой уже существующий механизм паузы, чтобы пользователь успевал рассмотреть результат;
- комментарии должны быть не только в заголовке файла: рядом с ключевыми операторами нужно кратко пояснять, что делает этот шаг;
- строки вроде `PRINT "sample finished"`, `PRINT "page flip frames="` и другие пользовательские сообщения в примерах запрещены; они должны быть заменены нормальными русскими фразами;
- старые примеры нужно приводить к этому правилу при ближайшей содержательной правке соответствующего файла.

---

## Definition of done

A task is complete only if:
- the requested change is done;
- relevant validation was run when possible;
- relevant docs were updated;
- `docs/WORKLOG5.md` was updated;
- `docs/PLANS4.md` reflects the next step.

If a task changes bytecode coverage, parity status, sample status, manual status, or profiling status for a user-visible language form, it is complete only if:
- `docs/BYTECODE_COVERAGE.md` was updated too.

If a task changes language behavior, it is complete only if:
- code changes are done;
- automated tests were added or updated where appropriate;
- a runnable `.axm` acceptance sample was added or updated in the correct thematic subfolder;
- the relevant user-facing manual page in `docs/manual/` was added or updated when the task introduces or changes an operator, built-in function, or other user-visible language behavior;
- `docs/manual/scripts/manual.js` was updated too when the new or changed entity should be discoverable through the manual index;
- the final report explicitly lists:
  - which automated tests were added or updated;
  - which `.axm` sample was added or updated;
  - the exact sample subfolder;
  - how to open and run it manually in the Electron IDE.

If language behavior changed but the correct `.axm` sample is missing, the task is not done.

---

## Project-specific instructions

This repository is for a custom BASIC-like programming language and its own IDE.

High-level intent:
- recreate the feel of QuickBASIC, but not limit the project to a small first-version slice or strict historical compatibility;
- implement a clean modern BASIC-like language on C#;
- develop the compiler, BytecodeVm and Electron IDE together;
- grow the language and IDE toward a complete usable product while keeping each change coherent and reviewable;
- treat Russian-language specification documents as normative.

Правила продуктового объёма:
- не считать старые ограничения первой версии продуктовой истиной; крупные возможности вроде графики, файлов, многофайловых модулей, ООП или полной совместимости с QuickBASIC могут проектироваться, когда они явно запланированы в документах;
- не переводить проект с C# на другой стек без явного решения в документах;
- сохранять компилятор и BytecodeVm в .NET; Electron представляет интерфейс и не исполняет язык самостоятельно.

Implementation preference:
- prioritize language core and diagnostics before advanced IDE features;
- prefer maintainable architecture over quick hacks;
- keep bootstrap implementation minimal and reviewable.

Required repository architecture rule for language frontend:
- monolithic files are forbidden for language frontend code in this repository;
- lexer, parser, syntax, binder/semantic analysis, and runtime execution must stay separated by responsibility;
- parser code must be split at least by responsibility zones:
  - expressions
  - statements
  - declarations
  - helpers
  - recovery
- parser must not be mixed with semantic analysis;
- semantic analysis must not be mixed with runtime execution;
- when a file grows too large or starts mixing responsibilities, Codex must split the code first and only then continue adding functionality;
- this is a permanent mandatory repository rule for all future work on lexer, parser, syntax, semantics, and runtime.

---

## Правило работы с современными моделями OpenAI

Для GPT-5.5 и последующих близких моделей рабочие инструкции в этом репозитории должны быть ориентированы на результат, а не на избыточное пошаговое управление.

Постоянные требования:
- сохранять обязательный документный процесс этого файла как источник проектного контракта;
- перед крупной правкой явно понимать ожидаемый результат, критерии готовности, допустимые побочные эффекты, нужные проверки и форму итогового отчёта;
- не добавлять длинные процедурные указания, если уже достаточно проектного контракта, плана и критериев готовности;
- не повышать сложность рассуждения, объём поиска или число проверок механически: делать это только когда задача действительно сложная или есть измеримый риск ошибки;
- для задач с инструментами предпочитать точное применение существующих правил, минимальные изменения, проверку результата и обновление документов вместо расширения промпта новыми частными запретами;
- если внешний гайд OpenAI конфликтует с `AGENTS.md`, `PROJECT.md` или русской проектной документацией, приоритет остаётся за документами репозитория до явного решения пользователя.

---

## Правило компактного агентного режима и экономии токенов

Для этого репозитория экономия контекста считается не отдельной косметикой, а частью инженерной дисциплины агентной работы.

Постоянные требования:
- обязательный стартовый перечень документов из этого файла остаётся обязательным в начале новой сессии; никакие вспомогательные сокращённые слои не имеют права заменять это требование;
- после обязательного стартового чтения для повторного входа в уже известный контекст нужно предпочитать локальный компактный слой `docs/agent/` и `scripts/ai-context.ps1`, а не снова без необходимости проталкивать в модель целиком огромные документы;
- если задача тематическая, сначала сужать контекст по теме, файлу, оператору, built-in, тестовому проекту или sample, и только потом читать большой документ или широкий лог полностью;
- длинные выводы инструментов нельзя бездумно проталкивать в контекст целиком, если сначала можно получить более дешёвый срез через `rg`, `Select-String`, `Get-Content -Tail`, фильтр конкретного теста, конкретного sample или конкретного файла;
- при работе с `git diff`, логами `dotnet test`, большими search-результатами и журналами IDE сначала нужно получить короткий ориентир: первый сбой, верхние совпадения, хвост файла, адресный проект, адресный sample или адресную строку `Негативный паритет`, а не весь массив сразу;
- если внешний инструмент умеет сжимать контекст, это не даёт права сжимать пользовательский исходный код, договор языка или проектные решения без явной необходимости: в этом репозитории код, спецификация и пользовательские диагностические детали важнее номинальной экономии токенов;
- внешние системы задач и памяти не должны становиться вторым источником правды рядом с `docs/PLANS4.md` и `docs/WORKLOG5.md`; если они используются, они могут только помогать извлекать или раскладывать уже существующий проектный договор;
- если внешний инструмент имеет разные наборы MCP-инструментов или режимы загрузки, для `Axiom` нужно предпочитать минимальный достаточный набор, а не полный режим по умолчанию;
- для Windows-нужд этого репозитория нельзя делать обязательным workflow, который полноценно работает только через Bash-хуки, Unix-shell или Linux-only interception; такие инструменты допустимы только как необязательный ускоритель или как отдельный WSL-контур;
- перед добавлением в проект нового внешнего агентного слоя нужно явно ответить на четыре вопроса в документации: что он экономит, что он может исказить, почему локальный `docs/agent/`-слой этого уже не покрывает, и как он вписывается в правило единственного источника правды.

Предпочтительный локальный порядок работы после обязательного старта:
1. Прочитать обязательные документы по этому `AGENTS.md`.
2. Для повторного обзора состояния использовать `scripts/ai-context.ps1` и `docs/agent/CURRENT_STATE.md`.
3. Для маршрутизации по коду использовать `docs/agent/ARCHITECTURE_MAP.md`.
4. Для типовых задач использовать `docs/agent/PLAYBOOKS.md`.
5. Только если этого недостаточно, поднимать тяжёлый документ, большой лог или внешний инструмент сжатия.

---

## Default handling of common user requests

If the user says:
“Read AGENTS.md and set up the project”
then:
- enter Bootstrap mode if setup docs are missing;
- otherwise enter Resume mode.

If the user says:
“Continue the project”
then:
- enter Resume mode automatically.

If the user says:
“Start implementing feature X”
then:
- read project files first;
- update `docs/SPECIFICATION3.md` or `docs/PLANS4.md` if needed;
- then implement.

---

## Output style
Be concise and factual.
Report:
- what you read;
- what you created or changed;
- what remains;
- what the next step is.


---

## Локальные временные артефакты

Временные файлы, каталоги, журналы, снимки, проверочные данные и результаты ручных запусков должны создаваться только внутри `.artifacts/`.

Запрещено оставлять в корне репозитория временные элементы вроде `axiom_sample_*`, отладочные файлы, промежуточные каталоги или данные ручных проверок. Если временный объект нужен для проверки примера, теста или ручного запуска, путь должен явно указывать на `.artifacts/`, чтобы весь каталог можно было удалить без риска снести исходники, документацию или постоянные проверочные примеры.


## Проверки оболочки Electron

Для Electron IDE использовать существующий набор `ide/e2e`, инструкцию `ide/e2e/README.md` и полный каталог `ide/e2e/SCENARIOS.json` (на 16 октября 2026 года — 456 сценариев). Не пересоздавать уже имеющиеся сценарии и общую автоматизацию при продолжении; сначала найти существующую проверку и дополнить её. Команда полного запуска: `npm --prefix ide/e2e test`. DesktopHost/CLI остаются служебными компонентами IDE и не исключаются из её проверок. После изменения состава обновить каталог командой `node ide/e2e/runner.js --list > ide/e2e/SCENARIOS.json`.

## Общие свойства интерфейса Electron

По поручению автора одинаковые элементы интерфейса должны использовать единые правила, а не собственные копии настроек:
- палитры оболочки, общие размеры пиктограмм и правила включённого/недоступного состояния задаются в ide/src/renderer/design-system.css; отдельные области используют эти переменные;
- aria-pressed, aria-expanded, aria-disabled, aria-checked и aria-label обновляются через ide/src/renderer/ui-controls.js; не копировать непосредственные назначения этих свойств в новые обработчики;
- создавать пиктограммные кнопки через UIControls.createIconButton, доступность менять через setEnabled, подсказку и доступное имя — через setLabel;
- условия доступности, состояние конкретного элемента и поведение команды остаются в её контроллере; не заменять их одним общим глобальным состоянием;
- новое общее свойство или тип одинаковых элементов сначала добавлять в общий слой и его проверки ide/tests/ui-controls.test.cjs, затем использовать в конкретной области;
- не объединять разные предметные договоры только из-за совпадения цвета или размера: подсветка синтаксиса, цвета графики программы и оформление оболочки имеют разные назначения.

## Действующая редакционная политика справочника — 11 октября 2026 года

По прямому поручению автора `docs/manual/` ведётся как формальный справочник самостоятельного языка Axiom. Разделы 4.1 и 4.1.b задают действующую политику справочника; порядок редактирования дополнительно описан в `docs/manual/AUTHORING.md` и навыке `.opencode/skills/axiom-manual-editor/SKILL.md`.

Пользовательские статьи не содержат сравнений с QuickBASIC, BASIC, Visual Basic или другими языками, происхождения названий и исторических отступлений. Правила Axiom из прежних сравнений сохраняются и излагаются прямо. Источниками фактов остаются спецификация, код, тесты и запускаемые примеры; семантика языка этим редакционным решением не меняется.

Обязательна полнота: отдельная карточка каждой встроенной функции, все зарегистрированные формы вызова, все операторы и формы инструкций, ключевые слова, типы и операции в выражениях. Перечень проверяется по реестру `BuiltinFunctions`, парсеру и синтаксическим узлам, а не по случайным упоминаниям в тексте.

Редакционный порядок, источники описаний, сборка каталогов и команды проверки закреплены в `docs/manual/README.md`. При изменениях справочника выполнять `node scripts/check-manual.mjs`; при изменениях реестра или синтаксиса сначала обновлять снимок и сверять его с загруженным реестром компилятора. Требования раздела 4.1.b к определению сущности, параметрам, ограничениям, диагностике и примерам сохраняются; обязательный одинаковый набор подзаголовков и единая вступительная формула отменены.


## Действующая карта документов — 11 октября 2026 года

По прямому поручению автора завершена одноразовая ревизия и архивирование старого массива. Активные источники: PROJECT, README, SPECIFICATION3, ARCHITECTURE, PLANS4, WORKLOG5, BYTECODE_COVERAGE, BYTECODE_PERF_MATRIX. Работа выполняется в Electron через DesktopHost и BytecodeVm. Полный текущий остаток находится в docs/plans/UNFINISHED_WORK.md; навигация — docs/INDEX.md. Журнал продолжает нумерацию с Session 1364, прежние записи до 1363 сохранены в docs/archive/worklog/WORKLOG5_pre1364.md. Старые архивные планы не задают текущую очередь и не должны возвращаться в активные документы. Новые записи активных журналов снова добавляются только в конец.

## Уточнение сопровождения версий — 2026-10-13

По решению автора правки docs/manual/ без изменения реализации языка, компилятора или IDE не требуют повышения продуктовых версий. Это правило имеет приоритет над прежним требованием повышения за изменение действующей документации применительно к справочнику. Изменение реализации по-прежнему требует повышения соответствующих компонентов и обновления справочника в той же задаче. Изменения самих правил версионирования и scripts/versioning.mjs не требуют повышения продуктовых версий. Уже опубликованные номера не понижаются.
