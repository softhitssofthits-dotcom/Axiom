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
   - `docs/PLANS3.md`
   - `docs/WORKLOG4.md`
   - `docs/BYTECODE_COVERAGE.md`
   - `docs/BYTECODE_PERF_MATRIX.md`
   - `docs/SPECIFICATION2.md`, `docs/PLANS2.md`, `docs/WORKLOG3.md`, `docs/SPECIFICATION.md`, `docs/PLANS.md`, `docs/WORKLOG2.md`, `docs/WORKLOG.md` only when older historical context is needed

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
- `docs/PLANS3.md`
- `docs/WORKLOG4.md`

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
3. record the current state in `docs/WORKLOG4.md`;
4. record the next step in `docs/PLANS3.md`.

---

## Resume mode

Use Resume mode when the repository already contains project documents.

Rules for Resume mode:
- Read the required files first.
- Continue from the latest plan instead of inventing a new direction.
- Prefer the next unfinished item from `docs/PLANS3.md`.
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
Human-facing repository entry point:
- what the project is;
- how to run it;
- how to test it;
- repository layout.

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

### `docs/PLANS3.md`
Current work plan:
- current milestone;
- next tasks in order;
- blockers;
- deferred items;
- append-only ordering from top to bottom for all evolving sections unless the document explicitly says otherwise.

### `docs/SPECIFICATION2.md`
Historical second part of specification:
- kept for historical context before `docs/SPECIFICATION3.md` became active;
- no new updates should be added there after the switch.

### `docs/SPECIFICATION.md`
Historical first part of specification:
- kept for historical context before `docs/SPECIFICATION3.md` became active;
- no new updates should be added there after the switch.

### `docs/PLANS2.md`
Historical second part of planning:
- kept for historical context before `docs/PLANS3.md` became active;
- no new planning updates should be added there after the switch.

### `docs/PLANS.md`
Historical first part of planning:
- kept for historical context before `docs/PLANS3.md` became active;
- no new planning updates should be added there after the switch.

### `docs/WORKLOG3.md`
Historical third part of session continuity:
- kept for sessions from 310 to 480;
- no new entries should be added there after `docs/WORKLOG4.md` is introduced;
- read it when context from sessions 310-480 is needed.

### `docs/WORKLOG2.md`
Historical second part of session continuity:
- kept for sessions from 169 to 309;
- no new entries should be added there after `docs/WORKLOG4.md` is introduced;
- read it when context from sessions 169-309 is needed.

### `docs/WORKLOG.md`
Historical session continuity log:
- kept for sessions up to and including Session 168;
- no new entries should be added there after `docs/WORKLOG4.md` is introduced;
- read it only when older historical context is needed.

### `docs/WORKLOG4.md`
Active session continuity log:
- what changed;
- which files changed;
- what was decided;
- what remains;
- exact next step;
- strict chronological ordering from top to bottom;
- new session entries must be appended only at the end of the file;
- starting from `2026-04-23 09:53` (local project time), every new session entry must include a sequential session number plus exact local date and time in the heading.

### `docs/BYTECODE_COVERAGE.md`
Active registry of bytecode coverage:
- which user-visible operators, built-ins and language forms are covered by `BytecodeVm`;
- whether they have automated tests;
- whether they have runnable `.axm` samples;
- whether they are reflected in `docs/manual/`;
- whether bytecode/interpreter parity is explicitly checked;
- whether the area is performance-hot or only functionally covered.

### `docs/BYTECODE_PERF_MATRIX.md`
Active registry of performance scenarios:
- which language families already have dedicated `samples/smoke/engine-compare/` scenarios;
- which families are covered only indirectly through mixed scenarios;
- which families still have no meaningful performance scenario;
- which gaps are blocked by current harness limits such as interactive input.

---

## Core working rules

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

### 4.1. Manual style must follow current Microsoft Visual Basic references by genre
For this repository, `docs/manual/` must be written not only from project facts, but also with an explicit editorial reference process.

Permanent requirements:
- before rewriting or creating a page in `docs/manual/`, first identify the closest **current** official Visual Basic page on `learn.microsoft.com` by genre;
- do not use one universal reference page for everything;
- functions must be compared to Visual Basic function reference pages;
- statements and control-flow constructs must be compared to Visual Basic statement pages such as `For...Next` and `Select...Case`;
- arrays and related conceptual pages must be compared to Visual Basic array overview and array-operation pages;
- use those Microsoft pages as a reference for:
  - the style of the opening explanation;
  - the balance between conceptual introduction and formal syntax;
  - the order and density of sections;
  - the tone of explanatory paragraphs;
  - the amount of guidance before examples.

Required editorial behavior:
- do not just copy the section skeleton mechanically;
- adapt the Microsoft-style introductory explanation to Axiom so the page first tells the reader what the construct is and why it exists, not only its syntax;
- for overview topics like arrays, loops, branching, procedures, types, file operations and similar core language topics, add a short concept-first opening explanation in the same spirit;
- when documenting a specific construct such as `FOR`, `SELECT CASE`, `PRINT`, `DIM`, `TYPE`, `REGEX...` or similar, search for the closest Visual Basic page of the same genre and follow that page's explanatory style;
- if Microsoft has both an overview page and an atomic statement/function page for the topic, use the overview page for the conceptual opening and the atomic page for syntax and section rhythm.

Source-of-truth boundary:
- current Microsoft Learn pages are editorial and structural references only;
- they are not allowed to override `PROJECT.md`, `docs/SPECIFICATION3.md`, code, tests or runnable samples;
- never copy Microsoft syntax or semantics into Axiom without checking that Axiom actually supports it;
- if Microsoft has a helpful introduction but Axiom differs semantically, keep the style and explanatory pattern, but rewrite the factual content for Axiom.

Seed examples of acceptable genre references:
- arrays overview: `https://learn.microsoft.com/ru-ru/dotnet/visual-basic/programming-guide/language-features/arrays/`
- `For...Next`: `https://learn.microsoft.com/ru-ru/dotnet/visual-basic/language-reference/statements/for-next-statement`
- `Select...Case`: `https://learn.microsoft.com/ru-ru/dotnet/visual-basic/language-reference/statements/select-case-statement`

If a suitable Russian Learn page is unavailable, prefer the nearest official English Visual Basic page on `learn.microsoft.com` rather than switching to an unrelated source.

### 4.1.a. Книга Зельднера является главным редакционным ориентиром manual

Для этого репозитория главное правило написания `docs/manual/` таково: если нужная тема, оператор, функция или экранный приём уже описаны в книге Зельднера, сначала ориентируйся именно на книгу Зельднера, а не на Microsoft Learn.

Постоянные требования:
- если тема описана у Зельднера, ближайший раздел книги должен быть первым редакционным ориентиром для структуры, ритма объяснения и выбора примера;
- `learn.microsoft.com` в таком случае остаётся только запасным жанровым ориентиром и не должен вытеснять подачу Зельднера;
- если у Зельднера есть удачный пример для этой темы, в manual по возможности нужно использовать именно этот пример или короткую Axiom-адаптацию по его мотивам;
- для операторов и функций, где книжный пример помогает понять поведение лучше сухого синтаксиса, вместе со страницей manual по возможности добавляй runnable `.axm`-пример по мотивам книги Зельднера;
- если темы у Зельднера нет, только тогда основной внешний редакционный ориентир можно брать с `learn.microsoft.com` по ближайшему жанру страницы;
- книга Зельднера остаётся редакционным и примерным ориентиром, но не отменяет проверку фактов по `PROJECT.md`, `docs/SPECIFICATION3.md`, коду, тестам и уже подтверждённым `samples/`;
- нельзя переносить из книги неподтверждённую семантику Axiom или большие дословные фрагменты текста; переносить нужно структуру объяснения и мотив примера, а не выдавать исторический текст за текущий договор проекта.

### 5. Validation after code changes
After code changes, run the most relevant checks available:
- tests
- lint
- typecheck
- build
- smoke-check

If checks cannot be run yet, state that explicitly.

### 5.1. .NET checks must not share `obj` in parallel
For this repository, do not run multiple `dotnet build`, `dotnet test`, `dotnet run` or other MSBuild-based commands in parallel when they can write to the same intermediate `obj` tree.

Permanent requirements:
- default to sequential `dotnet` validation commands;
- parallel `dotnet` validation is allowed only when every concurrent command has an explicitly isolated intermediate directory, not only a separate `OutputPath`;
- if isolation is needed, set a unique `BaseIntermediateOutputPath` or equivalent MSBuild intermediate path for each concurrent command;
- do not treat file-lock failures from shared `obj` as an acceptable normal workflow; prevent them by command ordering or true intermediate-directory isolation.

### 6. Worklog is mandatory
After every meaningful task, update `docs/WORKLOG4.md`.

### 7. Repository documents are append-only
All repository documents must be maintained from top to bottom.

Permanent requirements:
- never prepend new records to the top of `docs/WORKLOG4.md`, `docs/WORKLOG3.md`, `docs/WORKLOG2.md`, `docs/WORKLOG.md`, `docs/PLANS3.md`, `docs/PLANS2.md`, `docs/PLANS.md`, or any other evolving project document;
- add new records only below the existing records in the same document;
- preserve chronological reading order: earlier entries stay above later entries;
- do not silently reorder old records unless the task is explicitly to normalize document chronology.

For `docs/WORKLOG4.md`, continuing the sequence from `docs/WORKLOG.md`, `docs/WORKLOG2.md` and `docs/WORKLOG3.md`, every new entry:
- every new entry must use a heading in the form `## Session <N> - YYYY-MM-DD HH:mm - <title>`;
- `<N>` must be the next sequential session number after the highest existing numbered session;
- date and time are mandatory and must use the local project time zone;
- historical entries created before this rule may remain in the legacy format, but they must not be renumbered with invented timestamps.

### 8. Plans must stay current
If the next steps change, update `docs/PLANS3.md`.

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
- add or update a short runnable `.axm` program that can be opened in `Axiom.Ide` and run manually;
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

---

## Definition of done

A task is complete only if:
- the requested change is done;
- relevant validation was run when possible;
- relevant docs were updated;
- `docs/WORKLOG4.md` was updated;
- `docs/PLANS3.md` reflects the next step.

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
  - how to open and run it manually in `Axiom.Ide`.

If language behavior changed but the correct `.axm` sample is missing, the task is not done.

---

## Project-specific instructions

This repository is for a custom BASIC-like programming language and its own IDE.

High-level intent:
- recreate the feel of QuickBASIC, but not limit the project to a small first-version slice or strict historical compatibility;
- implement a clean modern BASIC-like language on C#;
- start with an interpreter and IDE, not native compilation;
- grow the language and IDE toward a complete usable product while keeping each change coherent and reviewable;
- treat Russian-language specification documents as normative.

Правила продуктового объёма:
- не считать старые ограничения первой версии продуктовой истиной; крупные возможности вроде `GOTO`, `GOSUB`, `ON ERROR`, графики, файлов, многофайловых модулей, ООП или полной совместимости с QuickBASIC могут проектироваться, когда они явно запланированы в документах;
- не переводить проект с C# на другой стек без явного решения в документах;
- не заменять интерпретаторную архитектуру компиляторной без явного архитектурного решения.

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
- update `docs/SPECIFICATION3.md` or `docs/PLANS3.md` if needed;
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
