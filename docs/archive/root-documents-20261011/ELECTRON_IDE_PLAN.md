# План: Electron-IDE для Axiom по образцу IDE проекта X#

> Дата: 2026-10-04. Статус: план утверждён пользователем, реализация начинается в отдельной сессии.
> Этот документ — точка входа для той сессии. Шаблон-источник: IDE проекта X# (`C:\X_Sharp\ide`;
> фактически шаблон лежит в `C:\X__sharp\ide`).
>
> **Статус выполнения (2026-10-05, Session 1101): этапы 1-4 реализованы.**
> CLI: `src/Axiom.Cli` (тесты `tests/Axiom.Cli.Tests`, 22/22); форматтер: `src/Axiom.Formatting`;
> Electron-IDE: `ide/` + `run-ide.bat`; e2e: `ide/e2e` (11/11). Полный прогон решения зелёный.
> Отклонения: бинарь CLI живут в `.artifacts/bin/Axiom.Cli/...` (правило репозитория), а не в
> `src/Axiom.Cli/bin/...`; `fmt --line` не реализован — в Axiom нет построчного форматирования,
> автоформат IDE работает по всему документу; freshness-guard сверяет git-ревизию сборки с HEAD.

## 1. Контекст и цель

В проекте X# собрана настольная IDE на Electron + Monaco Editor + xterm.js, которая общается
с компилятором через headless-CLI (`xsc check/run/fmt/metadata`), имеет консоль запуска
(node-pty/ConPTY), панель проблем, живые диагностики при наборе, темы, i18n ru/en и e2e-тесты
на Playwright (`_electron`). Цель — построить IDE того же типа и той же архитектуры для языка
Axiom в `C:\Axiom\ide\`.

Существующий WPF-IDE (`src/Axiom.Ide`) при этом НЕ изменяется и НЕ удаляется: он остаётся
полноценной средой с отладчиком и графическим окном.

## 2. Принятые решения (утверждены пользователем 2026-10-04)

1. **Строить параллельно.** Новая Electron-IDE живёт в `C:\Axiom\ide\`, WPF-IDE остаётся как есть.
2. **Графика отложена.** В новой IDE запускаются текстовые программы (PRINT/INPUT); графические
   программы (SCREEN/CANVAS) остаются в WPF-IDE. Окно графики в Electron — отдельный будущий этап.
3. **Отладчик отложен.** Брейкпоинты и шаги остаются в WPF-IDE. Позже можно добавить через
   debug-протокол CLI.

## 3. Ключевое архитектурное отличие (почему нужен этап 1)

IDE X# держится на headless-CLI: `xsc check <file> --json`, `xsc run <file>`, `xsc fmt`,
`xsc metadata`, `xsc --version`. У Axiom CLI нет — точка интеграции сегодня только библиотечная:
`AxiomExecutionPipeline` (`src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`,
`CompilationRequest`/`ExecutionRequest`, хосты `Axiom.Runtime.Host`).

Поэтому сначала строится тонкий headless-CLI поверх существующего пайплайна (этап 1), затем
портируется оболочка IDE (этапы 2–3). Примерно 70% кода X#-IDE (главный процесс, IPC, pty-пайп,
xterm-консоль, панель проблем, настройки, i18n, e2e-харнесс) переиспользуется почти как есть.

## 4. Этап 1 — headless CLI `Axiom.Cli` (exe `axiom`)

- Новый проект `src/Axiom.Cli` (net10.0, консольный): команды
  `run <file>`, `check <file> [--json]`, `check --stdin --json`, `fmt <file>` / `fmt --stdin`,
  `metadata`, `--version`, `--help`.
- Контракт потоков как у `xsc`: stdout — ТОЛЬКО вывод программы; stderr — диагностики
  компилятора; коды возврата: 0 — успех, 1 — ошибки компиляции, 2 — неверный usage/файл не найден.
- `ConsoleHost` — реализация `IRuntimeHost` для консоли: stdout/stdin (INPUT с промптом);
  клавиатура/мышь деградируют (INKEY → пусто, KEYDOWN → false); графика — понятная
  рантайм-диагностика «graphics is not supported in console mode». Поведение сверять с
  `IdeInputProvider` (`src/Axiom.Ide/Services/Input/`) и использованием хостов в ManualChecks.
- `AXIOM_IDE_DIAGNOSTICS_PATH` — side-channel: если переменная задана, диагностики пишутся в
  указанный файл, а не в stderr, чтобы в терминал IDE попадал только вывод программы
  (тот же приём, что в X#: `XSHARP_IDE_DIAGNOSTICS_PATH`).
- `check --json`: `{ok, diagnostics:[{line, col, length, code, message}]}` — маппинг из
  `DiagnosticBag`/`CompilationResult`; `TextLocation` уже 1-based, длина берётся из `TextSpan`.
- `metadata`: `{keywords, typeNames, builtins:[{name, params:[{name,type}], returnType}]}`
  — из `Axiom.Core/AxiomLanguageKeywords.cs` (единственный источник правды по словам) и реестра
  `BuiltinFunctionDescriptor` (`src/Axiom.Semantics/Symbols/`). Док-строк у встроенных нет —
  hover в IDE строится по сигнатурам.
- `--version` + git-ревизия: паттерн `XSharp.Cli.csproj` (target перед `GetAssemblyAttributes`
  выполняет `git rev-parse HEAD` и кладёт в `AssemblyMetadata`); `metadata.sourceRevision` —
  для freshness-guard IDE.
- `fmt`: чистые классы `src/Axiom.Ide/Editor/Formatting/` (`LineFormatter` и связанные, без
  WPF-зависимостей) извлечь в UI-независимый проект (например, `Axiom.Formatting`);
  `Axiom.Ide` и `Axiom.Cli` ссылаются на него. Изменение зафиксировать в ARCHITECTURE.md.
- Тесты: новый проект `tests/Axiom.Cli.Tests` (xunit) — контрактная таблица потоков/кодов,
  run с подменой stdin/stdout (in-process через хосты + 1–2 процессных теста), metadata-снапшот,
  version. Прогон — `scripts/dotnet-test-artifacts.ps1 Axiom.sln` (последовательные dotnet-команды).
- Проекты добавить в `Axiom.sln` (sln классический, правка вручную).

## 5. Этап 2 — каркас `C:\Axiom\ide\` (порт оболочки)

- `package.json` (`axiom-ide`, версия 0.1.0; electron, monaco-editor, @xterm/xterm + fit-addon,
  node-pty, @vscode/codicons).
- `src/main/main.js` — single instance, sandbox/contextIsolation, window-state, хуки логирования.
- `src/main/ensure-axiom.js` — резолв `src/Axiom.Cli/bin/Release/net10.0/axiom.exe`
  (затем Debug), проверка версии И совпадения git-ревизии с HEAD, автосборка
  `dotnet build src/Axiom.Cli -c Release` при устаревании (паттерн `ide/src/main/xsc-ensure.js`).
- `src/main/ipc.js` — все точки вызова CLI: check/fmt/run/metadata/--version (замена `xsc` → `axiom`
  во всех местах; в X# это `ide/src/main/ipc.js:126,214,400,434,450,538,557,591`).
- `src/main/pty-output.js` — backpressure-пайп вывода pty (переносится как есть);
  `ide-logger.js`, `last-dir.js`, `preload.js` — без изменений по сути.
- `scripts/prepare.js` (копия Monaco в `src/renderer/vs`), `scripts/ensure-deps.js`
  (самолечение `npm install` по сигнатуре зависимостей).
- `run-ide.bat` в корне `C:\Axiom` — запуск в один клик (проверка Node, ensure-axiom,
  ensure-deps, prepare, electron).

## 6. Этап 3 — рендерер: редактор и панели

- Порт `index.html` + `styles.css` + `design-system.css` (ребрендинг акцента), `app.js` с
  адаптацией под Axiom: фильтр файлов `*.axm`, проект = папка, run-target = **активный
  сохранённый файл** (конвенции main-файла у Axiom нет, WPF-IDE тоже запускает активный документ),
  шаблон нового файла (`PRINT "Hello, world"`).
- Monarch-грамматика BASIC: `ignoreCase: true`, комментарии `'` и REM, строковые литералы и
  интерполяция `$"..."`, hex-литералы `&H`, числа; language config — автозакрытие кавычек
  (скобочных блоков в BASIC нет). Словари — из `axiom metadata` + захардкоженный FALLBACK_DICTS.
- Дополнение/hover: встроенные функции с сигнатурами из metadata, ключевые слова, типы.
- Консоль запуска: полный порт xterm-подсистемы (темы под светлое/тёмное, type-ahead hold,
  санитизация paste, Stop через kill pty, строка `[Завершено]`, тайминг в статус-баре).
- Живые диагностики при наборе (`check --stdin --json`, debounce 600 мс), панель «Проблемы»
  с маркерами Monaco и переходом по клику, панель «Структура» (SUB/FUNCTION/TYPE — лёгкий
  клиентский скан документа).
- Темы светлая/тёмная, i18n ru/en для UI. Диагностические сообщения Axiom изначально на русском —
  слой messageRu из X#-IDE не нужен.
- Автоформат (F3 и триггеры) — только если `fmt` из этапа 1 готов.

## 7. Этап 4 — e2e и документация

- `ide/e2e`: порт `runner.js`/`helpers.js` (Playwright `_electron`, изолированный userData на тест,
  подмена нативных диалогов, ожидания статуса/консоли, скриншоты-доказательства в
  `ide/e2e/evidence/`). Стартовый набор спеков: startup, табы, run/stop (программа с INPUT),
  problems, i18n-parity, freshness-guard.
- Документация по правилам AGENTS.md: README (запуск CLI и IDE), ARCHITECTURE.md
  (подсистемы Axiom.Cli, Axiom.Formatting, `ide\`), PLANS4.md, WORKLOG5 (очередной номер сессии),
  `docs/agent/CURRENT_STATE.md`.
- CI в репозитории Axiom отсутствует; добавление CI-задачи для e2e — опция на будущее,
  в текущий объём не входит.

## 8. Осознанно НЕ входит (границы первой версии)

- Отладчик (брейкпоинты, шаги) — остаётся в WPF-IDE.
- Графика SCREEN/CANVAS — остаётся в WPF-IDE; консольный run даёт понятную диагностику.
- Удаление или деградация WPF-IDE — запрещено решением №2 (раздел 2).
- Упаковка/инсталлятор (electron-builder).

## 9. Верификация

- После каждого этапа — зелёный прогон `powershell -ExecutionPolicy Bypass -File
  scripts/dotnet-test-artifacts.ps1 Axiom.sln` (существующие 2371+ тестов не деградируют;
  извлечение форматтера проверяется существующими Ide-тестами).
- Этап 1: контрактные тесты CLI по таблице потоков/кодов возврата (раздел 4).
- Этап 4: локальный прогон `node ide/e2e/runner.js` — все стартовые спеки зелёные,
  скриншоты-доказательства закоммичены в `ide/e2e/evidence/`.

## 10. Опорные материалы в C:\X_Sharp (шаблон-источник)

| Что | Где |
|-----|-----|
| Оболочка IDE (main/preload/ipc, pty, логгер) | `ide/src/main/*`, `ide/src/renderer/*` |
| Точки вызова компилятора | `ide/src/main/ipc.js`, `ide/src/main/xsc-ensure.js` |
| Встраивание git-ревизии в CLI | `src/XSharp.Cli/XSharp.Cli.csproj` (target `CaptureXSharpSourceRevision`) |
| Контракт CLI (потоки/коды возврата) | `AGENTS.md` раздел 2.1 |
| e2e-харнесс | `ide/e2e/runner.js`, `ide/e2e/helpers.js`, `ide/e2e/specs/*` |
| Лаунчер в один клик | `run-ide.bat`, `ide/scripts/ensure-xsc.js`, `ide/scripts/ensure-deps.js` |

## Уточнение направления — 2026-10-09, Session 1312

По новому уточнению пользователя целевая Electron-IDE должна сохранять возможности текущей WPF-IDE Axiom и получать другой внешний вид по ориентиру IDE X#. Копирование всей среды X# и ограничение итоговой оболочки только консольными программами не являются конечной целью. Отложенные в прежнем плане графика и отладчик должны быть включены в будущий план функциональной полноты до замены WPF-IDE. Точный способ переноса ещё не выбран; реализация сейчас не начата.

## Единственный исполнитель — 2026-10-10

Будущая оболочка использует компиляцию в байт-код и BytecodeVm через действующие CLI и Execution. Выбор интерпретатора и резервный исполнитель исключены. Существующие ввод, графика, диагностика, кэш и отладчик VM должны сохраняться при смене внешнего вида. Прототип Electron ещё не заменяет действующую WPF-IDE.


## Новый подробный план — 2026-10-10 07:59, Session 1326

Действующая точка планирования: docs/archive/plans-through-20261011/IDE_SHELL_REPLACEMENT_PLAN.md, IDE_SHELL_FEATURE_MATRIX.csv и IDE_SHELL_PROTOCOL.md. Новый план включает функциональную полноту перед заменой основной WPF-оболочки: документы, форматирование, диагностика, INPUT, графика, события, отладчик, настройки, журнал и поставка. Ограничения первого прототипа «графика/отладчик отложены» остаются историей, а не условиями завершения перехода.

Архитектура описана как проект следующей реализации: Electron представляет интерфейс, DesktopHost использует общие существующие службы .NET и единственную VM. Нативный графический хост сохраняется; полный старый редактор скрыто не запускается. Реализация по новому плану ещё не начата. Стартовать с этапа 0 нового плана, не копировать весь X Sharp повторно.
