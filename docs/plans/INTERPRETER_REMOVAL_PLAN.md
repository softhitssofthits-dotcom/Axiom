# Отказ от интерпретатора

**Подробный план реализации для агента.** Дата: 2026-10-09, время Москвы. Исходная ревизия: `af0959985500750d2f644da6b5ffd3d116cc9823`, ветка `master`.

**Статус: подготовлен только план. Реализация не начата.** Пользователь разрешил составление документа; начало исполнения требует отдельного поручения. Этот документ должен позволять продолжить работу без истории чата.

**Цель:** оставить один путь исполнения Axiom: компилятор байткода и `BytecodeVm`; полностью удалить действующий исполнитель `Interpreter`, выбор двух режимов и инфраструктуру сравнения двух исполнителей, сохранив возможности языка и нынешней IDE.

**Архитектура:** текст `.axm` проходит лексический и синтаксический анализ, связывание, понижение и компиляцию в `BytecodeModule`; модуль выполняет `BytecodeVm`. Общие значения Runtime, встроенные функции, файловые и графические хосты остаются на C#/.NET. WPF-IDE сохраняется на время этого перехода.

**Стек:** C#/.NET 10, WPF/AvalonEdit, существующие проекты `Axiom.Bytecode`, `Axiom.Runtime`, `Axiom.Execution`; CLI и существующий незавершённый Electron-прототип остаются потребителями того же ядра.

Исполнять этапы последовательно. Навыки `writing-plans`, `software-architecture`, `systematic-debugging`, `verification-before-completion` использовать по назначению, если они доступны следующему агенту. Отсутствие навыка не разрешает пропустить требования документа. Создание подагентов необязательно; нельзя параллельно запускать команды MSBuild с общим `obj`. Не начинать реализацию автоматически после чтения плана.

## 1. Что именно означает «оставить только компилятор»

Конечный путь:

```text
.axm -> Lexer/Parser -> Binder/BoundProgram
     -> BoundProgramLowerer -> BytecodeCompiler -> BytecodeModule
     -> BytecodeVm -> результат, вывод, диагностика, графический хост
```

Компилятор создаёт инструкции, а VM выполняет их. Удаляется интерпретатор связанного дерева, не VM. Собственный байткод остаётся; генерация CIL, машинного кода и самостоятельных `.exe` не входят в эту задачу. Удаление интерпретатора не объявлять созданием компилятора в `.exe`.

`BoundProgram`, Binder, символы, типы, AST и пониженное представление нужны компилятору. Не удалять их из-за прежнего использования интерпретатором. Не создавать новый исполнитель дерева под другим именем, не оставлять скрытый режим или резервный вызов `Interpreter.Execute`.

## 2. Объём и ограничения

Входит в работу:

- перенос уникальных проверок интерпретатора на VM без потери ожидаемых результатов;
- проверка достаточности VM для поддерживаемого поведения и устранение подтверждённых пробелов до удаления;
- упрощение контрактов Execution и IDE до одного способа запуска;
- безопасное чтение старого состояния IDE, где выбран `Interpreter`;
- удаление исполнителя и только действительно неиспользуемых зависимостей;
- переработка текущих средств аудита и измерений для одного исполнителя;
- актуальные документы, руководство, проверочные программы, регрессия и проверка настоящей IDE.

Не входит в работу: новая Electron-оболочка, изменение визуала, новые операторы/имена функций, изменение точности `POW`, пересмотр языка, новая система проектов, упаковка `.exe`, новые библиотеки и общий рефакторинг VM. Существующий Electron-прототип незавершён; проверка его интеграции не означает доведение всей оболочки.

Прежняя авария testhost IDE отложена пользователем. Не запускать исследования аварии или сбор дампов ради этой задачи. Обычные обязательные проверки выполнять; новый сбой записывать честно. Зелёный повтор не объявлять исправлением причины первого сбоя.

Вопрос `POW` уже закрыт решением пользователя: готовый `Math.Pow`, `NaN` -> `MB3007`, бесконечность -> `MB3008`. Сохранить это решение и `PowDotNetContractTests`.

## 3. Подтверждённое исходное состояние

| Область | Что обнаружено | Следствие для работы |
|---|---|---|
| Исполнитель | 24 файла `src/Axiom.Runtime/Execution/Interpreter*.cs` | Удалять только после выполнения этапов 1–5 |
| Прежние проверки Runtime | 14 файлов `InterpreterTests*.cs`, 597 объявлений с Fact/Theory, включая одно под `#if false` | Нужен поштучный перенос, а не удаление проекта тестов |
| Зависимости | 235 файлов с отобранными упоминаниями режимов, окружений или сравнения движков | Совпадение текста не означает, что файл надо удалять |
| Конвейер | `RunCompiled` уже возвращает `BackendFailure` при отсутствии подходящего байткода | Автоматического резервного запуска интерпретатора в этом месте сейчас нет |
| Компиляция | `CompilationResult.Succeeded` отражает `CompilationStatus.Success`, независимо от `BackendSucceeded` | Нужно сделать общий успех компиляции честным, сохранив отдельный успех фронтенда |
| IDE | Режим хранится в `RecentFilesState.PreferredExecutionMode` | Старое состояние нельзя сбросить целиком |
| Отладка | `BytecodeVmDebuggerAdapter` уже управляет VM | Отладчик сохранить; новый отладчик не писать |
| Замыкания | `RuntimeFunctionValue` содержит `CapturedScope` и байткодные слоты | Удаление окружения требует отдельной проверки значений функций |
| Инструменты | `compare-engines`, аудит примеров, инвентаризация и aggregate-perf знают о двух движках | Их нужно перевести на VM и перестать обещать паритет двух исполнителей |

Числа — снимок исходников при составлении плана, не число обнаруженных xUnit-случаев и не доказательство всех входов. Перепроверить после изменения ревизии.

Приложения к плану:

1. [Зависимости по файлам](INTERPRETER_REMOVAL_DEPENDENCIES_20261009.csv): путь, категория, строки совпадений, исходный статус.
2. [Перенос прежних тестовых методов](INTERPRETER_REMOVAL_TEST_TRANSFER_20261009.csv): исходный файл/метод, атрибут, условие исходника и целевой файл/метод для каждого из 597 объявлений с Fact/Theory.

Это исходные снимки. Не переписывать их, выдавая новый поиск за прежний. В ходе исполнения создавать отдельные таблицы прогресса с исходным и новым именем, результатом проверки и причиной исключения. Номера строк служат для первого поиска; после правки искать метод по имени.

## 4. Карта файлов: сохранить, изменить, удалить

### Сохранить

`src/Axiom.Core/`, `src/Axiom.Lexing/`, `src/Axiom.Syntax/`, `src/Axiom.Parsing/`, `src/Axiom.Semantics/`, весь `src/Axiom.Bytecode/`; в Runtime — `BytecodeVm*.cs`, `BytecodeCallFrame`, байткодные слоты/состояния отладки, `Host/`, `Graphics/`, значения массивов/записей/файлов/дат/изображений, используемые общие реализации `Builtins/`.

`RuntimeResult`, `RuntimeFailureException`, `RuntimeCallChainFormatter` и `BuiltinArrayAggregateHelpers` нужны не только старому исполнителю. Упоминание слова Interpreter в комментарии не делает их устаревшим кодом.

В IDE сохранить `ProgramRunService`, `BytecodeVmDebuggerAdapter`, `GraphicsHostService`, `ProgramConsoleService`, клавиатурный/мышиный ввод, работу с документами, ресурсами и журналами. Новая оболочка — следующий отдельный проект.

### Изменить

| Группа | Точные исходные точки |
|---|---|
| Запрос/результат Execution | `src/Axiom.Execution/Requests/ExecutionRequest.cs`, `Requests/ExecutionMode.cs`, `Results/ExecutionResult.cs`, `Results/ExecutionEngineKind.cs` |
| Компиляция | `src/Axiom.Execution/Results/CompilationResult.cs`, `Results/CompilationStatus.cs`, `Results/CompiledProgram.cs`, `Pipeline/AxiomExecutionPipeline.cs` |
| IDE запуск | `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`, `ProgramRunService.cs`, `src/Axiom.Ide/Models/ProgramRunResult.cs` |
| IDE меню/состояние | `src/Axiom.Ide/MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs`, `ViewModels/MainWindowViewModel.Preferences.cs`, `Resources/ShellStrings.ru.xaml`, `Resources/ShellStrings.en.xaml`, `Services/Persistence/RecentFilesState.cs`, `RecentFilesSnapshot.cs`, `RecentFilesService.cs` |
| Замыкания | `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs`, `RuntimeValue.cs`; затем кандидаты `Environment/RuntimeScope.cs`, `Environment/CallFrame.cs` |
| Проверки | все строки приложения зависимостей в `tests/`; отдельно 14 прежних Runtime-файлов и их общий набор вспомогательных методов |
| Инструменты | `src/Axiom.ManualChecks/Program.cs`, `BytecodeCoverageAudit.cs`, `scripts/aggregate-perf/Program.cs`, `scripts/language-audit/Program.cs`, `SampleAudit.cs`, `Inventory.cs`, `Export-Audit.ps1`, `export-current-inventory.py`, `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs` |

### Удалить после проверок

24 файла `Interpreter*.cs`; затем `ExecutionMode.cs` и неиспользуемые параметры режима; `Interpreter` из `ExecutionEngineKind`; поля резервного исполнения; старые команды выбора движка и ресурсы меню. `RuntimeScope`/`CallFrame` удалять только после устранения всех реальных потребителей, включая тесты и представление функции. Не удалять `BytecodeCallFrame`.

`ExecutionEngineKind` оставить с `BytecodeVm`, а `ActualEngine` сохранить для наблюдаемости: при фронтенд-ошибке исполнитель отсутствует, при запуске VM явно указан. Удалить `RequestedMode`, поскольку выбирать больше нечего. Такое небольшое поле наблюдаемости не является второй веткой исполнения.

Таблица показывает основные точки, а CSV дополняет её всеми найденными потребителями. В частности, для изменения хранения предпочтений обязательно изменить `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`, а не только реализацию. Искать также тестовые заменители интерфейса.

В исходном enum `BytecodeVm` имел значение 1. При удалении предыдущего элемента явно записать `BytecodeVm = 1`, чтобы случайно не изменить числовое представление в потребителях. Если позже понадобится новая схема телеметрии, это отдельное изменение.

## 5. Правила для исполняющего агента

1. Прочитать `AGENTS.md`, `PROJECT.md` и обязательные актуальные документы; затем этот план и последние записи `WORKLOG5`. Если документы изменились, перепроверить список зависимостей.
2. Не делать глобальную замену `Interpreter` -> `BytecodeVm`: у них разные входные типы и служебные состояния. Компиляция `BoundProgram` в модуль обязательна.
3. Не удалять тест с непонятным падением. Сначала воспроизвести и понять, что он проверяет. Если VM не поддерживает нужный договор, исправить её отдельным маленьким изменением до удаления старого исполнителя.
4. Не смягчать ожидаемую диагностику, точность, байты файла, состояние массива, порядок событий и вывод ради зелёного теста. Смена названия исполнителя не меняет семантику языка.
5. При сомнении о семантике обращаться к спецификации, ручному примеру и буквальным ожидаемым результатам. Не выбирать автоматически поведение одного из двух исполнителей.
6. Новые временные файлы только в `.artifacts/`. Перед рекурсивным удалением проверять абсолютную границу собственного каталога; не удалять общий каталог артефактов.
7. Все `dotnet`-команды выполнять последовательно. Разные OutputPath не изолируют общий `obj`. Команды ниже запускать из `C:\Axiom`.
8. Изменения каждого этапа должны быть обозримыми. Исправление пробела VM, перенос тестов и удаление API — отдельные изменения. Не переписывать весь большой `BytecodeVm.cs` ради наведения порядка.
9. По завершении каждой существенной группы дописывать `WORKLOG5` с очередным Session, точным местным временем, командой, кодом завершения, результатом и следующим действием. Не придумывать заранее будущие номера Session.
10. Пользовательские изменения и ранее существовавшие удаления `.playwright-mcp` не включать в своё удаление кода. Не запускать `git reset --hard`, `git clean -fd` или массовое `Remove-Item` по результату поиска слова Interpreter.
11. Коммит этапов делать только в рамках разрешения на будущую реализацию; push — по отдельному или уже действующему поручению пользователя. Наличие этого плана не разрешает публиковать изменения.
12. Если этап не завершён, сохранить точку остановки в документах. Не объявлять всю работу законченной по одной зелёной сборке.

## 6. Этап 0. Зафиксировать исходное состояние и критерии сохранности

**Изменения:** только документы и снимки проверки в `.artifacts/interpreter-removal/`; рабочий код пока не менять.

- [ ] Выполнить `git status --short`, `git rev-parse HEAD`, `git branch --show-current`; записать пользовательские изменения. Не переключать ветку с потерей рабочего дерева. При создании ветки использовать `codex/interpreter-removal`.
- [ ] Создать собственный каталог артефактов и сохранить список обнаруженных тестов каждого из девяти тестовых проектов через `dotnet test <project> --list-tests`. Для Runtime и Execution сохранить отдельные списки; Theory может разворачиваться в несколько случаев, 597 объявлений не использовать вместо обнаруженного количества.
- [ ] Выполнить исходную регрессию и сохранить TRX, код завершения, ошибки/пропуски. Сохранить размеры/сигнатуры реестра через `--inventory-only` без экспорта поверх исторических CSV.
- [ ] Открыть текущую WPF-IDE и подтвердить контрольные сценарии из раздела 15. Если исходная функция не работает, записать это до изменений; не приписывать переходу старую ошибку.
- [ ] Создать `docs/plans/INTERPRETER_REMOVAL_PROGRESS.csv` с исходными тестовыми методами, действием, целевой проверкой, артефактом и результатом. Исторические приложения не менять.

Точный список тестовых проектов: `Axiom.Lexing.Tests`, `Axiom.Syntax.Tests`, `Axiom.Parsing.Tests`, `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests`, `Axiom.Bytecode.Tests`, `Axiom.Execution.Tests`, `Axiom.Cli.Tests`, `Axiom.Ide.Tests`; пути `tests/<имя>/<имя>.csproj`. В конце нужен результат каждого проекта, не только Runtime.

```powershell
New-Item -ItemType Directory -Path .artifacts/interpreter-removal -Force
dotnet test Axiom.sln --logger trx --results-directory .artifacts/interpreter-removal/baseline
node --test ide/tests/lang-features.test.cjs
dotnet run --project scripts/language-audit/Axiom.LanguageAudit.csproj -- C:/Axiom --inventory-only
```

**Критерий завершения:** известны исходные возможности, тесты, текущая ревизия и реально успешные/неуспешные команды. Исторические 7353/7353 — восстановленная регрессия предыдущей сессии, не обещание исходного зелёного запуска. После добавления POW-тестов выполнялись адресные 198/198; новое полное число необходимо получить фактически.

## 7. Этап 1. Перенести проверки Runtime, пока оба исполнителя существуют

**Файлы:** 14 `tests/Axiom.Runtime.Tests/InterpreterTests*.cs`, новый `LanguageContractTests` с теми же тематическими частями; новый `tests/Axiom.Runtime.Tests/RuntimeVmContractRunner.cs`. Не добавлять зависимость Runtime-тестов от `Axiom.Execution`: уже есть пример прямой компиляции в `BytecodeVmTests.ExecuteProgram`.

- [ ] Сначала перенести одну арифметическую проверку и один отказ с `FINALLY`, сохранив буквальные ожидания.
- [ ] Создать общий путь `Parser -> Binder -> Lowerer -> BytecodeCompiler -> BytecodeVm` для перенесённых проверок. Понижение и компиляция должны проверяться явно.
- [ ] Переносить по одному тематическому файлу: вывести новый класс, сохранить каждый метод и его данные. Новый тестовый класс — частичный `LanguageContractTests`, без старого имени Interpreter. Новые методы не должны конфликтовать между тематическими частями.
- [ ] Для каждого перенесённого метода записать соответствие в таблицу прогресса. Старый метод удалять только после успешного нового запуска и проверки его ожиданий.
- [ ] Перенести прямые `Interpreter.Execute` в тестах клавиатуры, мыши, отмены и особых хостов; не забыть о вспомогательных методах в `InterpreterTests.TryCatch.cs`, хотя имя файла выглядит как узкая группа TRY.
- [ ] Провести поиск прямых вызовов в тестах вне класса InterpreterTests. Там тоже сохранить проверку; приложение по 597 объявлениям не включает автоматически все остальные потребители. Проверить `RuntimeValueTests` и прежние тесты окружений по таблице 235 файлов.
- [ ] Сверить все 597 объявлений с обнаружением xUnit: учесть Theory, параметры и данные; никакое число успешных случаев не заменяет поштучного соответствия. Выключенный вариант `Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy` под `#if false` не включать автоматически и не считать потерянным обнаруженным тестом; действующий одноимённый вариант ниже сохранить.

Полный образец исполнителя для переноса (пространства имён совпадают с существующими проектами):

```csharp
using Axiom.Bytecode;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

internal static class RuntimeVmContractRunner
{
    internal static (BoundProgram Program, TestRuntimeHost Host, RuntimeResult Result)
        Execute(string source, TestRuntimeHost host)
    {
        var syntax = Parser.Parse(source, out var parseDiagnostics);
        Assert.Empty(parseDiagnostics);
        var program = Binder.Bind(syntax, out var bindDiagnostics);
        Assert.DoesNotContain(bindDiagnostics,
            d => d.Severity != Axiom.Core.DiagnosticSeverity.Warning);
        var lowered = new BoundProgramLowerer().Lower(program);
        Assert.True(lowered.Succeeded,
            string.Join("\n", lowered.Diagnostics.Select(d => d.Message)));
        var compiled = new BytecodeCompiler().Compile(lowered.Program!);
        Assert.True(compiled.Succeeded,
            string.Join("\n", compiled.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(compiled.Module);
        var result = BytecodeVm.Execute(compiled.Module!, host);
        return (program, host, result);
    }
}
```

Существующие вспомогательные методы возвращают `BoundProgram`; сохранить его в кортеже, если тест исследует связанное дерево. Не подменять его `BytecodeModule` механически. Хост создавать прежним способом, например `new TestRuntimeHost(inputs)`, чтобы сохранить фиксированные часы, клавиши, мышь, ресурсы и отмену.

```powershell
dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter FullyQualifiedName~LanguageContractTests --logger trx --results-directory .artifacts/interpreter-removal/runtime-transfer
dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --logger trx --results-directory .artifacts/interpreter-removal/runtime-all
```

**Критерий завершения:** каждому исходному активному методу соответствует успешная VM-проверка с сохранённым ожидаемым результатом либо документированная проверка другого слоя; неактивные объявления отдельно объяснены. Название старого класса не является основанием для удаления проверок.

## 8. Этап 2. Перенести сквозные проверки и доказать отсутствие потери семантики

**Файлы:** `tests/Axiom.Execution.Tests/`, затронутые Runtime/Bytecode/IDE-тесты из приложения зависимостей; актуальный `docs/BYTECODE_COVERAGE.md`.

- [ ] Отделить проверки конкретного результата от проверок паритета. В Theory с `Interpreter` и `Bytecode` сохранить один реальный запуск VM. Две одинаковые строки `InlineData(Bytecode)` не являются переносом.
- [ ] Для теста, который проверял только равенство исполнителей, добавить независимое ожидание: конкретный вывод, код/позиция диагностики, UTF-16-единицы, байты файла, форма массива, порядок вызовов. Не использовать сам VM-результат как ожидаемый.
- [ ] Сохранить проверки `ActualEngine == BytecodeVm`, отсутствие скрытого запуска и реальность исполнения. Пока поля fallback существуют, продолжать проверять их; позже заменить смысл на проверку полного модуля и единственного пути.
- [ ] Проверить локальные процедуры/замыкания, рекурсию, BYREF и массивные представления отдельно от списка built-in. Наличие 335 форм не доказывает покрытие всех конструкций и всех входов.
- [ ] Выполнить адресные группы и полный Execution. Если VM нарушает существующий договор — сначала короткое воспроизведение, исправление, руководство и `.axm`; затем продолжить перенос. Не объявлять потерю функции допустимой ради удаления интерпретатора.

Минимальный образец независимой проверки, сохраняемый после удаления параметра режима:

```csharp
[Fact]
public void RunPreservesEmptyAndJoinedPrintLines()
{
    var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
        "PRINT \"Первая\"\nPRINT\nPRINT \"Начало\";\nPRINT \"Конец\""));
    Assert.True(result.Succeeded);
    Assert.Empty(result.Diagnostics);
    Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
    Assert.Equal(new[] { "Первая", "", "НачалоКонец" }, result.Output);
}
```

```powershell
dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --logger trx --results-directory .artifacts/interpreter-removal/execution-transfer
```

**Критерий завершения:** таблица переноса не содержит необъяснённых потерянных проверок; поддерживаемые конструкции реально исполняются VM. Отдельный пробел VM может потребовать нескольких сессий. Это не разрешает агенту пропустить его.

## 9. Этап 3. Сделать успех компиляции однозначным

**Файлы:** `CompilationStatus.cs`, `CompilationResult.cs`, `AxiomExecutionPipeline.cs`, `ProgramRunService.cs`, `ProgramCompilationResult.cs`, соответствующие `ExecutionPipelineTests`/`ProgramRunServiceTests`, CLI-тесты.

Сейчас возможен `CompilationStatus.Success` при неудачном понижении, эмиссии или записи запрошенного снимка. При одном исполнителе нельзя сообщать пользователю «компиляция успешна», если запускать нечего.

- [ ] Добавить к `CompilationStatus` отдельный `BackendFailure`; сохранить `SyntaxFailure` и `SemanticFailure`.
- [ ] `Succeeded` должен означать успешную полную компиляцию, `FrontendSucceeded` — успешный разбор/связывание даже при ошибке следующего этапа. Не заменять оба свойства одним флагом.
- [ ] Ошибки понижения, эмиссии, проверки покрытия и записи запрошенного снимка должны быть видимы в результате и в IDE, сохранять код/позицию и не запускать программу. Диагностики фронтенда и backend хранить отдельно и объединять для показа без дублей.
- [ ] Кэш в `ProgramRunService` обновлять только полноценным результатом; после неудачной компиляции нельзя запустить предыдущий исходник под видом нового. Сохранить метрики и различие повторной компиляции/повторного запуска.
- [ ] Переписать проверку `Compile_ReturnsDiagnosticWhenBytecodeSnapshotWriteFails`: ожидать полную неудачу backend, успешный фронтенд и прежний `MB4003`. Её текущий `Success` — именно изменяемый договор API, а не доказательство правильного общего успеха.
- [ ] Сохранить отдельную команду CLI `check`: выяснить по `CheckSource`, что она сейчас проверяет; проверка буфера не должна исполнять программу и создавать её файлы. Если это проверка только синтаксиса/семантики, оставить её такой и ясно документировать отличие от полной компиляции.

Целевые выражения контракта результата:

```csharp
public bool FrontendSucceeded =>
    Status is CompilationStatus.Success or CompilationStatus.BackendFailure;
public bool Succeeded => Status == CompilationStatus.Success
    && BackendSucceeded && BytecodeAvailable
    && CompiledProgram?.BytecodeModule is not null;
```

Тест для ошибки снимка создавать с каталогом под `.artifacts/`, не `Path.GetTempPath`. Ожидать `BackendFailure`, `FrontendSucceeded == true`, `Succeeded == false`, `MB4003`; проверить, что повторный Run нового текста не использует старый кэш. Предупреждения Binder не превращать в ошибку.

Образец точной проверки результата компиляции (добавить в `ExecutionPipelineTests`; каталог удалять только свой):

```csharp
[Fact]
public void CompileSnapshotFailureIsNotFullCompilationSuccess()
{
    var root = new DirectoryInfo(AppContext.BaseDirectory);
    while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md")))
        root = root.Parent;
    Assert.NotNull(root);
    var directory = Path.Combine(root.FullName, ".artifacts",
        "snapshot-failure-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var result = new AxiomExecutionPipeline().Compile(
            new CompilationRequest("PRINT 3", bytecodeSnapshotPath: directory));
        Assert.Equal(CompilationStatus.BackendFailure, result.Status);
        Assert.True(result.FrontendSucceeded);
        Assert.False(result.Succeeded);
        Assert.False(result.BackendSucceeded);
        Assert.Equal("MB4003", Assert.Single(result.BackendDiagnostics).Code);
    }
    finally
    {
        Directory.Delete(directory);
    }
}
```

```powershell
dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter FullyQualifiedName~ExecutionPipelineTests
dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --filter FullyQualifiedName~ProgramRunServiceTests
dotnet test tests/Axiom.Cli.Tests/Axiom.Cli.Tests.csproj
```

**Критерий завершения:** нет успешной полной компиляции без исполняемого модуля; ошибка backend видна пользователю, а фронтенд остаётся отдельно наблюдаемым.

## 10. Этап 4. Снять режим с IDE и мигрировать сохранённое состояние

**Файлы:** IDE-группы раздела 4, `MainWindowViewModel.Debug.cs`, `RecentFilesServiceTests`, `MainWindowViewModelTests`, `ProgramRunServiceTests`, `IdeSmokeHarness.cs`.

- [ ] Удалить меню выбора исполнителя, обе команды `UseInterpreterExecutionMode`/`UseBytecodeExecutionMode`, `SelectedExecutionMode` и завязанные ресурсы/проверки. Обычный запуск и отладка всегда используют VM.
- [ ] Убрать параметр режима из `IProgramRunService.Run`, реализации и вызывающих. Сохранить breakpoints, остановку перед первой инструкцией, путь исходника, время ожидания, отмену, состояние отладки и ресурсный каталог.
- [ ] В той же группе изменить все реализации интерфейса и тестовые заменители, включая вложенные `FakeProgramRunService` в IDE-тестах. Этап должен компилироваться; нельзя оставить несовместимый интерфейс до последнего этапа.
- [ ] Удалить `RequestedExecutionMode`/fallback из `ProgramRunResult` и журналов. `ActualEngine`, метрики, состояние остановки и ошибки сохранить. Не удалять журнал целиком из-за одной строки режима.
- [ ] Старые JSON со значениями `Interpreter`, `Bytecode`, `Auto`, неизвестным значением, регистровым вариантом и отсутствующим полем должны открываться без сброса остальных настроек. Новые записи режима больше не нужны. Использовать существующий десериализатор, не писать второй механизм хранения.
- [ ] В `RecentFilesSnapshot` убрать типизированный режим. `RecentFilesState.PreferredExecutionMode` можно удалить, если текущий JSON-десериализатор игнорирует неизвестное поле; это доказать тестом. Если используется строгий разбор, добавить узкую миграцию этого поля, а не режим исполнения.
- [ ] Сохранить файлы, последнее место открытия, язык, тему, состояние окна, форматирование и другие существующие настройки. Проверить чтение старого состояния и повторное сохранение, включая штатную обработку повреждённого файла.
- [ ] В SmokeHarness заменить сценарий выбора Interpreter проверкой обычного запуска, сохранения/восстановления настроек и записи события запуска VM. Не вырезать весь сценарий журнала.

**Критерий завершения:** пользователь видит один способ выполнения; старое состояние сохраняет остальные данные; breakpoints и шаг назад работают. Краткое снижение количества тестов допустимо только за счёт исчезнувшего переключателя, с записанной причиной.

Образец проверки сохранности старого JSON для `RecentFilesServiceTests` после удаления поля режима. Использовать существующие `using Axiom.Ide.Services.Persistence` и `System.Text.Json`:

```csharp
[Theory]
[InlineData("Interpreter")]
[InlineData("Bytecode")]
[InlineData("Auto")]
[InlineData("interpreter")]
[InlineData("unknown")]
[InlineData(null)]
public void OldEnginePreferenceDoesNotResetOtherPreferences(string? oldMode)
{
    var root = new DirectoryInfo(AppContext.BaseDirectory);
    while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md")))
        root = root.Parent;
    Assert.NotNull(root);
    var directory = Path.Combine(root.FullName, ".artifacts",
        "old-engine-state-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var statePath = Path.Combine(directory, "state.json");
    var state = new Dictionary<string, object?>
    {
        ["RecentFiles"] = Array.Empty<string>(),
        ["Theme"] = "Dark", ["Language"] = "English",
        ["SmartIdentifierEnabled"] = true,
        ["IsFilesPaneVisible"] = false, ["IsStatusBarVisible"] = true,
        ["WindowState"] = "Normal"
    };
    if (oldMode is not null) state["PreferredExecutionMode"] = oldMode;
    File.WriteAllText(statePath, JsonSerializer.Serialize(state));
    try
    {
        var service = new RecentFilesService(statePath);
        var snapshot = service.GetSnapshot();
        Assert.Equal(IdeTheme.Dark, snapshot.Theme);
        Assert.Equal(IdeLanguage.English, snapshot.Language);
        Assert.True(snapshot.SmartIdentifierEnabled);
        Assert.False(snapshot.IsFilesPaneVisible);
        service.UpdateShellPreferences(false, true, IdeWindowState.Normal,
            IdeTheme.Dark, IdeLanguage.English, true);
        using var saved = JsonDocument.Parse(File.ReadAllText(statePath));
        Assert.False(saved.RootElement.TryGetProperty("PreferredExecutionMode", out _));
        Assert.Equal(IdeTheme.Dark, new RecentFilesService(statePath).GetSnapshot().Theme);
    }
    finally
    {
        File.Delete(statePath);
        Directory.Delete(directory);
    }
}
```

Это минимальный образец; дополнительно расширить существующие проверки настоящих путей недавних файлов и последнего каталога. Они не заменяются пустым массивом примера.

## 11. Этап 5. Упростить Execution и удалить исполнитель дерева

**Файлы:** Execution-группа раздела 4; Runtime-группа; все оставшиеся потребители API из CSV.

- [ ] Удалить параметр `mode` и свойство `Mode` из `ExecutionRequest`; обновить все именованные и позиционные вызовы. Не смещать аргументы позиционного вызова незаметно: при сомнении перевести его в именованный.
- [ ] Синхронно убрать использование удаляемого API из scripts/ManualChecks/SmokeHarness и всех тестов. На этом этапе инструменты должны хотя бы собираться; не оставлять ссылки до этапа 6. Здесь выполнить механическую миграцию вызовов, а этап 6 завершает смысл отчётов и измерений.
- [ ] Удалить `RunThroughInterpreter`, `RequestedMode`, fallback-поля, параметры и ветви; удалить `ExecutionMode.cs`. В `ExecutionEngineKind` оставить `BytecodeVm`. Все resume/step-контракты сохранить.
- [ ] При отсутствии модуля/поддержки оставить честную `ExecutionStatus.BackendFailure`; проверку покрытия не удалять только потому, что резервного пути больше нет. `CompiledProgram.BoundProgram`/Syntax сохранять, пока они нужны компиляции, отладке или диагностике.
- [ ] Сначала удалить реальные вызовы старого API, затем 24 файла исполнителя. Использовать точный проверенный список `git rm -- <path>` по этапу, не шаблон удаления всего Runtime.
- [ ] Из `RuntimeFunctionValue` удалить конструктор с `RuntimeScope` и `CapturedScope`; сохранить `Function`, `BytecodeRoutine`, захваченные слоты и binding ID. Из `RuntimeValue` убрать только перегрузку `FromFunction(FunctionSymbol, RuntimeScope)`, не общее значение функции/тип функции.
- [ ] Найти потребителей `RuntimeScope`/`CallFrame` в src/tests/scripts после предыдущего шага; лишь при нуле реальных потребителей удалить эти файлы. Провести проверки callback-массивов, замыканий, BYREF, рекурсии и TYPE.
- [ ] Обновить комментарии про общий договор двух движков там, где код остаётся; не переписывать его алгоритм без обнаруженной причины.

Образец поиска остаточных зависимостей:

```powershell
rg -n 'Interpreter.Execute|RunThroughInterpreter|ExecutionMode|InterpreterFallback|CapturedScope|\bRuntimeScope\b|\bCallFrame\b' src tests scripts ide -g '*.cs' -g '*.js' -g '*.cjs' -g '*.ps1' -g '*.py' -g '*.xaml' -g '!**/node_modules/**'
```

Код `rg` 1 означает отсутствие совпадений, а не ошибку запуска. Код 2 означает ошибку поиска. Исторический JSON/отчёт и слово в документе не являются оставшимся исполнителем. Найденный активный вызов нельзя объявить историческим.

```powershell
dotnet build Axiom.sln --no-restore
dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore
dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore
```

**Критерий завершения:** старый исполнитель физически отсутствует, общий Runtime сохранён, нет активного режима Interpreter и резервного исполнения, полноценные программы запускаются/отлаживаются VM.

## 12. Этап 6. Привести инструменты к одному исполнителю

**Файлы:** инструменты из раздела 4; их README; `docs/BYTECODE_PERF_MATRIX.md`, `docs/BYTECODE_COVERAGE.md`; не исторические таблицы аудита.

- [ ] Убрать действующую команду `compare-engines` и записи результата Interpreter из ManualChecks. Добавить команду `benchmark-bytecode` для тех же семейств. Старую команду явно отклонять с понятным сообщением и ненулевым кодом; не молча выполнять другую команду.
- [ ] Сохранить подготовку данных, фиксированные часы/случайность, вход, ресурсы и собственные графические хосты существующих сценариев. Измерять отдельно подготовку, понижение, эмиссию, исполнение; не выдавать потерю подготовки из измерения за ускорение.
- [ ] Сохранить прогрев, повторы и абсолютные показатели; удалить отношения скорости к удалённому исполнителю. Для проверки ухудшения сравнивать новую VM с исходной VM той же машины/сценария. Не задавать в плане выдуманный универсальный порог скорости.
- [ ] Каталог `samples/smoke/engine-compare/` оставить на этом этапе, чтобы не ломать десятки ссылок; объяснить историческое имя в реестре. Его программы становятся нагрузками одной VM. Переименование не требуется для удаления исполнителя.
- [ ] `scripts/aggregate-perf` запускать только на VM, сохранить размеры/повторы/выделения и математические ожидания.
- [ ] В `language-audit/Program.cs`/`SampleAudit.cs` оставить один запуск VM. Убрать InterpreterResult/паритет и отчёты о двух запусках; отрицательный пример проверять по ожидаемой диагностике, а не объявлять любое падение нормальным.
- [ ] В `Inventory.cs` убрать поиск MissingInterpreterDispatch и строки текущей поддержки Interpreter; сохранить ID/arity/политики/VM-поддержку/обе подсветки/ручные примеры. Синхронно изменить экспортеры и текущую схему JSON, затем проверить их потребителей.
- [ ] Старые результаты `docs/audit/` не переписывать. Новый снимок создавать под новым уникальным именем. Одноисполнительный аудит не выдавать за новое доказательство прежнего паритета.

```powershell
dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- audit-bytecode-coverage --out .artifacts/interpreter-removal/coverage.txt
dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- profile-bytecode-opcodes --out .artifacts/interpreter-removal/opcodes.txt
dotnet run --project scripts/language-audit/Axiom.LanguageAudit.csproj -- C:/Axiom --inventory-only
```

Полный старый language-audit содержит исторические ожидания с известными расхождениями. Не запускать его как обязательный зелёный регрессионный набор и не ослаблять историю, чтобы получить exit0. Для новых одноисполнительных сценариев указать конкретные ожидания и классификацию.

**Критерий завершения:** все активные инструменты компилируются без старого API, измеряют VM и не требуют отсутствующего исполнителя; корпус сценариев и независимые ожидания сохранены.

Проверки новых команд после изменения ManualChecks:

```powershell
dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- benchmark-bytecode --out .artifacts/interpreter-removal/vm-benchmark.txt
dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj -- compare-engines
```

Первая команда должна завершиться с кодом 0 при успешном выполнении всех выбранных сценариев и сохранить абсолютные показатели VM. Вторая должна завершиться с ненулевым кодом и сообщением о прекращении поддержки сравнения; не считать этот ожидаемый отказ провалом регрессии. Поддержать прежние параметры повторов/прогрева, прочитав `BenchmarkOptions` и фактический разбор аргументов; не выдумывать имена параметров.

## 13. Этап 7. Документы, руководство и запускаемые примеры

**Файлы:** `PROJECT.md`, `README.md`, `docs/ARCHITECTURE.md`, `docs/SPECIFICATION3.md`, `docs/PLANS4.md`, `docs/WORKLOG5.md`, `docs/BYTECODE_COVERAGE.md`, `docs/BYTECODE_PERF_MATRIX.md`, `AUDIT_HANDOFF.md`, действующие страницы `docs/manual/`, `docs/ELECTRON_IDE_PLAN.md`.

- [ ] Дописать актуальное архитектурное решение: один компилятор байткода и одна VM, дерево не исполняется. Исторический старт с интерпретатора не выдавать за текущий путь.
- [ ] В README обновить актуальное описание запуска и компиляции; больше нет меню выбора исполнителя и сравнительных команд. Старые хроники оставить историческими, добавить явное актуальное уточнение.
- [ ] В manual найти все действующие указания на выбор движка и изменить их; перед правкой применить редакционные правила Зельднера/жанровые Microsoft Learn из AGENTS. Страницы функций с прежними доказательствами двух движков должны описывать текущий пользовательский договор, не историю реализации.
- [ ] Добавить короткий русский `samples/smoke/bytecode-only/compiler_execution.axm`: вычисление 1+2, вызов процедуры, цикл, пустой PRINT, перехваченная ошибка POW и FINALLY. Написать ожидаемый вывод и проверить независимые значения через Execution и CLI. Не добавлять скрытых файловых/сетевых изменений.
- [ ] Для ручной отладки использовать отдельный короткий сценарий с процедурами, вложенным вызовом и изменяемым значением; рядом с шагами комментарии, где ставить breakpoint. Для визуальных примеров использовать паузу/клавишу и уже существующие графические примеры, не создавать незаметно новую семантику.
- [ ] Обновить coverage: проверки одного исполнителя не называть паритетом; прежний паритет остаётся историческим свидетельством. Матрицу измерений обновить под `benchmark-bytecode` без фиктивных отношений скоростей.
- [ ] Уточнить требования будущей Electron-IDE: использовать единственный путь и существующий отладчик VM, сохранить возможности WPF, прототип не объявлять готовым.

Подсветку менять только при реальном изменении пользовательских токенов. Этот переход не вводит новый оператор и не требует искусственных правок обоих XSHD. При обнаружении отдельного изменения языка выполнять все требования AGENTS для manual/примеров/подсветки.

**Критерий завершения:** документы описывают реально завершённые этапы, пример можно запустить, старые исторические сведения не уничтожены, пользователь не получает обещания native `.exe`.

Точное содержимое будущего `compiler_execution.axm` (в этой сессии файл не создаётся):

```basic
' Один путь компиляции и выполнения: процедуры, цикл, строки и обработка ошибки.
SUB ShowStep(value AS INTEGER)
    PRINT "Шаг: "; value
END SUB

' Независимый арифметический результат и пустая строка.
PRINT "Сумма: "; 1 + 2
PRINT
FOR i = 1 TO 2
    ShowStep(i)
NEXT i

' Ошибка POW сохраняет договор Axiom, FINALLY выполняется.
TRY
    PRINT POW(-2, 0.5)
CATCH err
    PRINT err.Code
FINALLY
    PRINT "Завершено"
END TRY
```

Ожидаемые логические строки: `Сумма: 3`, пустая строка, `Шаг: 1`, `Шаг: 2`, `MB3007`, `Завершено`. Добавить проверку ровно этих шести строк и `ActualEngine == BytecodeVm`. CLI должен вывести те же строки с системными переводами строк. Пример не требует интерактивной паузы, поскольку оставляет текстовый результат.

## 14. Матрица обязательной сохранности

| Группа | Что обязательно проверить | Какое доказательство требуется |
|---|---|---|
| Фронтенд | синтаксис, Binder, предупреждения, точные позиции | прежние Parsing/Semantics-проверки; отсутствие исполнения при ошибке |
| Числа | типы/переполнение, DECIMAL-агрегаты, POW, знак нуля | буквальные ожидания и контракт Math.Pow, а не только отсутствие исключения |
| Строки | UTF-16, непарные суррогаты, NUL, CODEPOINT/GRAPHEME | независимые единицы/результаты, существующие Unicode-группы |
| Массивы | ранги, границы, пустые/невыделенные, представления, callback | значения, форма и сохранность после отказа |
| Процедуры | BYREF, локальные захваты, рекурсия, функции как значения | порядок и значения; сохранность связи захваченных ячеек |
| Ошибки | TRY/CATCH/FINALLY, ERR/диагностики, цепочка вызовов | код, позиция, FINALLY и возможность продолжения |
| Файлы | OPEN/CLOSE, EOF, кодировки, пути/NUL, освобождение | настоящие байты/сохранность/закрытие собственных файлов |
| Ввод/вывод | PRINT, INPUT, пустые/соединённые строки, prompts | точные строки и реальный CLI; подсказка не получает лишний перенос |
| Время/случайность | DATE/TIMER/зоны, RANDOMIZE/RND | фиксированные хосты и независимые ожидания |
| Графика | SCREEN/CANVAS, страницы, VIEW/WINDOW/PMAP, изображения/тайлы | прежние графические ожидания и настоящее WPF-окно |
| События | INKEY/KEYDOWN/KEYPRESS, мышь, обработчики | очередь/однократное потребление/живой ввод |
| Отмена/ресурсы | Stop/таймаут, финальные строки, cleanup | нет зависшего запуска, повторный запуск работает, ресурсы освобождены |
| Отладка | breakpoints, Continue, Into/Over/Out/Back | точная остановка, сохранение контекста, прежние VM-тесты и UI |
| Кэш/снимки | compile/run/recompile, ошибка snapshot | актуальный исходник, честный backend-статус, нет старого запуска |
| Настройки IDE | старый Interpreter/Auto/неизвестный режим | данные остальных настроек и документов сохранены |

У каждой строки должен быть путь к текущему тесту/примеру и результат в итоговом отчёте. Не считать таблицу проверенной только потому, что она напечатана в плане.

## 15. Этап 8. Полная регрессия и проверка настоящей IDE

- [ ] Сохранить полный список обнаруженных тестов после перехода. Сопоставить с исходным через таблицы переноса, отдельно объяснить исчезновение вторых наборов Theory и тестов удалённого переключателя. Не ожидать неизменного общего числа тестов.
- [ ] Запустить полное решение один раз после последнего изменения кода; получить все девять TRX, проверить выход процесса и каждый Outcome/Counters. Не складывать незавершённые проекты.
- [ ] Запустить метаданные Electron и проверку CLI. Компилятор/CLI не должен получать скрытый параметр Interpreter из незавершённой Electron-оболочки.
- [ ] Запустить WPF SmokeHarness по существующему руководству. Его сценарии не удалять из-за смены режима. Если среда UI недоступна, сохранить явный незавершённый пункт; не объявлять полную готовность без ручной проверки графики и отладки.
- [ ] Выполнить контрольные ручные действия ниже и записать результат с версией/путём исполняемого файла. Использовать именно текущую сборку, не оставшееся старое окно.
- [ ] Проверить поиск остаточного кода, таблицу переноса, ссылки документации и `git diff --check`; подготовить переносимую JSON/Markdown-сводку результатов.

Добавить одну защитную проверку отсутствия старого исполнителя в сборке Runtime, чтобы его случайно не вернули под прежним API:

```csharp
[Fact]
public void RuntimeAssemblyDoesNotExposeTreeInterpreter()
{
    Assert.Null(typeof(BytecodeVm).Assembly.GetType("Axiom.Runtime.Interpreter"));
}
```

Она подтверждает отсутствие типа, но не заменяет проверку скрытых путей, ссылок и поведения VM.

```powershell
dotnet test Axiom.sln --logger trx --results-directory .artifacts/interpreter-removal/final
node --test ide/tests/lang-features.test.cjs
dotnet run --project src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj
git diff --check
```

Контрольные действия в WPF:

1. Открыть/сохранить два документа, запустить один, остановить, запустить второй; убедиться, что выполняется текущий текст.
2. Проверить PRINT/INPUT и повторный запуск после ввода; сравнить с CLI-выводом нового примера.
3. Поставить breakpoint, выполнить Into/Over/Out/Continue/Back, остановить и повторить отладку. Проверить текущую строку и доступность команд, не только наличие кнопки.
4. Запустить прежние SCREEN и CANVAS-примеры, клавиатуру и мышь; увидеть изображение и реакцию ввода. После Stop закрыть/повторно открыть программу.
5. Открыть прежний JSON состояния с Interpreter; проверить документы/тему/язык/последний каталог и повторное сохранение.
6. Вызвать синтаксическую, семантическую и runtime-ошибку; проверить диагностику и навигацию к строке; затем успешно запустить исправленный текст.
7. Проверить страницу справочника, форматирование и журнал событий; отсутствие меню выбора исполнителя не должно ломать остальные команды.

## 16. Этап 9. Закрытие, передача и правила коммита

- [ ] Создать `docs/plans/INTERPRETER_REMOVAL_RESULT.md`: фактическая ревизия, изменения, перенос тестов, число обнаруженных/успешных/пропущенных случаев, команды и коды, результаты UI, оставшиеся ограничения.
- [ ] Дописать WORKLOG/PLANS/AUDIT_HANDOFF с завершённой или незавершённой точкой. Не оставлять следующий агент читать лишь исторический Session 1310.
- [ ] Проверить staged diff: только разрешённые файлы; никаких пользовательских удалений, node_modules, секретов и временных дампов. Каждый промежуточный коммит должен собираться и иметь актуальную точку продолжения.
- [ ] При разрешённом push: fetch, сверить расхождение, отправить без force, проверить совпадение HEAD/remote-tracking/`git ls-remote`. Не писать «отправлено» до подтверждения.

Готовность всей работы подтверждается одновременно:

- `Interpreter` и его исполнительные файлы отсутствуют в активном коде;
- `ExecutionMode`, переключатели и резервный путь отсутствуют, старая настройка читается безопасно;
- все исходные проверочные ожидания сохранены или имеют объяснённый перенос;
- полноценная компиляция не сообщает успех без пригодного модуля;
- общий Runtime, язык, графика, ввод, диагностика, отладка и кэш сохранены;
- инструменты измеряют единственную VM и не требуют второго движка;
- обязательные автоматические и реальные UI-проверки выполнены, ограничения названы;
- документы и руководство соответствуют коду.

## 17. Как продолжать после перерыва

Прочитать последнюю запись WORKLOG, этот план и таблицу прогресса. Найти первый незавершённый этап. Перепроверить рабочее дерево и результаты предыдущего этапа; не начинать заново уже доказанный участок. В конце каждого захода оставлять: текущий этап, точный следующий файл/метод, последнюю команду и результат, оставшуюся причину остановки.

Готовое поручение исполнителю:

> Прочитай AGENTS.md, PROJECT.md и обязательные документы, затем docs/plans/INTERPRETER_REMOVAL_PLAN.md. Реализуй только первый незавершённый этап после последней точки WORKLOG. Сохраняй язык, независимые ожидания тестов, графику и отладчик. Не удаляй Interpreter до переноса уникальных проверок и подготовки единственного VM-пути. Не меняй оболочку на Electron и не добавляй сборку native exe. После этапа выполни указанные последовательные проверки и запиши точную передачу состояния.

Это поручение — образец для будущего запуска, не команда выполнять реализацию в сессии составления плана.

## 18. Разбор затруднений без опасных сокращений

| Ситуация | Правильное действие | Недопустимое сокращение |
|---|---|---|
| Новый VM-тест не проходит компиляцию | Сохранить исходник, диагностику Lowerer/Compiler, проверить прежний договор и место отказа | Пропустить тест или выполнить его старым исполнителем |
| Значения VM отличаются от прежнего буквального ожидания | Проверить спецификацию и независимый результат, выделить минимальное воспроизведение | Записать фактический ответ VM как новое ожидание |
| Удаление режима ломает несколько проектов | Обновить их вызовы и заменители интерфейса в одном этапе | Удалить проекты из решения, отключить Compile Include или оставить вечный фиктивный режим |
| Число тестов уменьшилось | Сопоставить обнаруженные случаи и таблицу переноса; показать удалённые дубли/режимные проверки | Объявить любое снижение естественным результатом удаления Interpreter |
| Старые настройки не читаются | Проверить отдельное устаревшее поле и совместимость JSON | Удалить пользовательский файл состояния |
| Ошибка возникает только в WPF | Проверить хост, потоки UI, ввод/ресурсы и текущую сборку | Считать консольный зелёный тест доказательством графики |
| Недоступна UI-среда проверки | Завершить независимые части, записать точный незавершённый UI-пункт | Поставить всему переходу статус готово |
| Сбой testhost или ожидания | Сохранить исходный результат; допускается обоснованный обычный повтор без скрытия сбоя | Ослабить ожидание, отключить класс или начать отложенное исследование аварии |
| Не найден внешний потребитель изменяемого публичного API | Проверить весь репозиторий и описать изменение в отчёте | Обещать совместимость всех неизвестных внешних клиентов |
| Требуется новая функция языка, чтобы закрыть пробел | Зафиксировать конкретный вопрос и запросить решение об изменении объёма | Самостоятельно менять язык под удобство удаления старого кода |

Если доказательства не позволяют выбрать поведение, не продолжать зависимое удаление. При этом можно переносить независимые проверочные группы. Пользовательское решение требуется для изменения семантики/объёма, а не для обычного исправления найденной регрессии.

## 19. Самопроверка плана перед исполнением

- [ ] Ревизия и рабочее дерево сверены; первоначальные CSV соответствуют исходному состоянию либо дополнены новым снимком.
- [ ] Проверки Runtime, Execution, IDE, CLI, Bytecode и инструментов охвачены; активные сборки не потеряны.
- [ ] Есть отдельные этапы для уникальных тестов, backend-успеха, состояния IDE, API, замыканий, удаления и измерений.
- [ ] Порядок сохраняет сборку: при удалении интерфейса/enum все потребители обновляются в той же группе.
- [ ] Графика/ввод/отладка не заменены одним текстовым smoke-тестом.
- [ ] Исторические доказательства отделены от новых результатов; обнаружение тестов отделено от поиска деклараций.
- [ ] Готовые образцы кода сверены с именами существующих типов; дальнейшее изменение API учитывается по этапам.
- [ ] Реализация не начата без отдельного поручения; новая оболочка и native `.exe` не включены случайно.


## Session 1325 - 2026-10-10 00:37 - Завершение отказа от интерпретатора

План реализован локально. Ранее расположенные флажки относятся к исходному планированию; действующий статус ниже.

- [x] Этап 0: исходное состояние и границы изменений проверены.
- [x] Этап 1: перенос Runtime проверен, 597 деклараций сопоставлены.
- [x] Этап 2: самостоятельные ожидания VM сохранены, 158 файлов проверены.
- [x] Этап 3: полный успех компиляции и BackendFailure подтверждены.
- [x] Этап 4: выбор исполнителя удалён, прежний JSON совместим.
- [x] Этап 5: исполнитель дерева и старые API удалены.
- [x] Этап 6: инструменты и экспорты используют одну VM.
- [x] Этап 7: документация и запускаемые примеры обновлены.
- [x] Этап 8: 6032/6032 и восемь живых сценариев IDE прошли.
- [x] Этап 9: итоговая передача сохранена в документах; публикация не выполнялась.

Подробности, ограничения и происшествия: INTERPRETER_REMOVAL_RESULT.md. Переносимые доказательства: INTERPRETER_REMOVAL_VALIDATION_20261010.json; финальная таблица Execution: INTERPRETER_REMOVAL_EXECUTION_FINAL.csv. Следующий агент начинает с этих документов, а не с повторного удаления Interpreter. Самостоятельный native exe в этот план не входил.
