# План работ

`docs/PLANS.md` закрыт как историческая первая часть плана. Активный план ведётся в `docs/PLANS2.md`.


## Текущее направление

Развитие `Axiom.Ide` — перевод среды в полноценную оболочку в духе Thonny с вкладками документов, файловой панелью, нижней панелью `Output`/`Diagnostics`, строкой состояния, полноценным меню и панелью инструментов. Язык, среда выполнения, сквозной запуск и семантика в этом направлении **не меняются**.

Полный blueprint редизайна зафиксирован в `docs/IDE_REDESIGN.md`.

## Что уже завершено

1. Создано .NET-решение `Axiom.sln`.
2. Созданы проекты:
   - `Axiom.Core`
   - `Axiom.Lexing`
   - `Axiom.Syntax`
   - `Axiom.Parsing`
   - `Axiom.Semantics`
   - `Axiom.Runtime`
   - `Axiom.Ide`
   - тестовые проекты для lexer/parser/semantics/runtime
3. Настроены ссылки между проектами по `docs/ARCHITECTURE.md`.
4. Реализованы минимальные доменные типы в `Core`:
   - `TextSpan`
   - `TextLocation`
   - `Diagnostic`
   - `DiagnosticBag`
5. Реализован рабочий lexer в `Axiom.Lexing`:
   - `TokenKind`
   - `Token`
   - `Lexer`
6. Добавлены и проходят первые unit-тесты для лексического слоя.
7. Добавлен repo-local skill `compiler-front-end` для узких задач по compiler frontend workflow.
8. Реализованы базовый AST в `Axiom.Syntax` и минимально полноценный parser в `Axiom.Parsing`.
9. Добавлены и проходят parser unit-тесты на выражения, инструкции, top-level declarations и syntax diagnostics.

10. Реализован минимальный semantic layer в `Axiom.Semantics`:
   - `TypeSymbol`
   - `VariableSymbol`
   - `ParameterSymbol`
   - `FunctionSymbol`
   - `BoundScope`
   - `Binder`
   - `BoundProgram` и базовые bound nodes
11. Добавлены и проходят semantic unit-тесты на объявления, присваивания, вызовы, типы, условия, `FOR`, `WHILE`, `RETURN` и semantic diagnostics.
12. Реализован первый executable runtime в `Axiom.Runtime`:
   - `RuntimeValue` и `RuntimeArrayValue`
   - `RuntimeScope`
   - `CallFrame`
   - `IRuntimeHost`
   - `Interpreter`
   - `RuntimeResult`
13. Добавлены и проходят runtime unit-тесты на исполнение выражений, присваиваний, `IF`, `WHILE`, `FOR`, вызовы `SUB`/`FUNCTION`, built-ins и runtime diagnostics.
14. Реализован единый orchestration layer `Axiom.Execution`:
   - `ExecutionRequest`
   - `ExecutionResult`
   - `ExecutionStatus`
   - `AxiomExecutionPipeline`
   - host abstractions для input / output / diagnostics
15. Добавлены и проходят integration tests на `parse -> bind -> execute`, статусы выполнения, output capture, input-driven execution и сохранение diagnostics.
16. Завершён milestone M6:
   - `Axiom.Ide` подключён к `Axiom.Execution` через thin `ProgramRunService`
   - собран минимальный WPF shell `MainWindow` c `Open / Save / Run`
   - подключён `AvalonEdit` как editor control без продвинутой интеграции
   - output и diagnostics отображаются раздельно
   - добавлены и проходят non-UI tests для `ProgramRunService`
17. Завершён этап M7.1:
   - source mapping протянут из syntax/binder в `BoundNode.Location`
   - runtime diagnostics больше не используют общий synthetic `1:1`, когда исходная позиция доступна
   - `Axiom.Execution` стабильно возвращает mapped runtime diagnostics
   - `Axiom.Ide` отображает runtime line/column из реального source location без изменения UI-shell
18. Завершён этап M7.2:
   - `Axiom.Ide` получил первый рабочий UX для `INPUT`; исходный modal prompt подход позже был заменён более поздней shell-integrated реализацией
   - WPF-слой подключает `INPUT` через отдельный IDE-side adapter поверх существующего `IInputProvider`
   - execution boundary не менялся архитектурно и не превратился в консольную оболочку
   - пустая отправка и закрытие prompt трактуются как пустая строка, а invalid conversion по-прежнему идёт через runtime diagnostics
19. Завершён этап M7.3:
   - в `Axiom.Ide` добавлена централизованная syntax highlighting definition для `AvalonEdit`
   - первая палитра подсветки зафиксирована как нейтральная, читаемая и Visual-Studio-like по логике
   - добавлена безопасная навигация к diagnostic из списка ошибок без inline diagnostics и без разрастания code-behind
   - non-UI tests покрывают загрузку highlighting definition и валидацию navigation target
20. Завершён этап M8.1:
   - shared WPF resources вынесены в отдельный `ResourceDictionary`
   - `App.xaml` подключает UI resources централизованно
   - `MainWindow` переведён на shared brushes/styles/layout constants без изменения UX-логики
   - добавлена минимальная проверка загрузки shell resource dictionary
21. Завершён этап M8.2:
   - в `Axiom.Ide` добавлен маленький `Services/Persistence/` слой для recent files и последнего каталога open/save
   - recent files безопасно сохраняются и восстанавливаются между запусками без settings UI, session restore и rich shell state
   - `MainWindow` получил минимальный `Recent Files` блок, не превращаясь в multi-document IDE
   - `Open / Save / Run` и execution/runtime boundary остались функционально прежними
22. Добавлен acceptance regression layer для Axiom v0.1:
   - позитивный smoke sample сохранён в `samples/acceptance/smoke_v0_1.axm`
   - expected output для smoke scenario зафиксирован в репозитории
   - добавлен набор negative acceptance programs на базовые semantic/runtime failure paths
   - execution-level tests запускают эти sample programs автоматически
23. Закрыт этап M8 ручной стабилизацией shell convenience layer:
   - в живом `Axiom.Ide` вручную проверены сценарии recent files, missing recent entry и last-used path
   - подтверждено, что persistence ведёт себя штатно и не требует немедленного расширения до settings UI, session restore или rich shell state
   - Axiom v0.1 можно считать завершённой первой устойчивой базой проекта
24. Закрыт этап M9 / v0.1.5:
   - `PRINT` получил пустую форму, print-list через `;` и `,`, trailing separator semantics и современную мягкую separator model для `,` без console emulation
   - `INPUT` получил input-list, prompt forms, minimal multi-value parsing и предсказуемую runtime-диагностику на count/conversion mismatch
   - modal dialog-based `INPUT` UX убран из `Axiom.Ide` и сначала заменён на встроенную input panel внутри основного shell; текущий baseline позже уточнён до console-style input line в `Output`
   - parser приведён к anti-monolith правилу через разнос на partial files по зонам ответственности
   - build/test снова зелёные на полном solution после обновления lexer/parser/semantics/runtime/execution/IDE test coverage
25. Сформирован baseline catalog runnable `.axm` samples по текущему языку:
   - добавлены тематические примеры для реализованных operators/declarations, control-flow, expressions и built-in functions
   - добавлен отдельный `samples/smoke/v0_1_baseline.axm` для ручного walkthrough через `Axiom.Ide`
   - новая иерархия `samples/` больше не является пустым процессным правилом и должна поддерживаться при всех следующих language changes
26. Исправлен parser diagnostics text corruption для `Axiom.Ide`:
   - syntax diagnostics больше не содержат mojibake-строки вида `Ож...`
   - parser снова возвращает читаемые русские сообщения для `MB1001`-`MB1004`
   - добавлены regression tests на корректный русский текст parser diagnostics
27. Ужесточена validation built-in функций:
   - binder по-прежнему централизованно закрывает arity/type validation по сигнатурам `LEN`, `LEFT`, `RIGHT`, `MID`, `INT`, `STR`, `VAL`, `LCASE`, `UCASE`
   - runtime получил единый domain/value validation path для `LEFT`, `RIGHT`, `MID` и `INT` через предсказуемую runtime-диагностику `MB3007` вместо молчаливого прохождения или generic failure
   - edge-case semantics для `LEFT`/`RIGHT`/`MID`/`VAL` явно зафиксированы в спецификации и покрыты automated tests и runnable `.axm` samples
28. Уточнена validation non-positive count для string-slice built-ins:
   - `LEFT`, `RIGHT` и `MID` теперь требуют `count > 0`, а не `count >= 0`
   - literal-варианты `0`, `-0`, `00` и `-00` должны вести себя одинаково и давать `MB3007`
   - sample catalog расширен manual acceptance programs на zero-count error cases для `LEFT`, `RIGHT` и `MID`
29. Уточнена лексика числовых литералов:
   - forms с ведущими нулями вроде `00`, `001`, `00.5`, `01.25` больше не считаются допустимыми numeric literals
   - такие случаи теперь режутся в lexer слое диагностикой `MB0003`, а не доходят до runtime/semantic behavior
   - zero-count runtime validation для `LEFT`/`RIGHT`/`MID` сохраняется только для валидных выражений `0` и `-0`
30. Уточнён IDE UX для `INPUT` в console-style сторону:
   - отдельная input panel/card и кнопка `Submit` убраны из `Axiom.Ide`
   - `Output` теперь работает как единый transcript программы, а активный `INPUT` идёт через одну встроенную строку ввода
   - подтверждение ввода происходит по `Enter`, без отдельного confirm UI
   - `ProgramRunService` теперь прокидывает live output в тот же console service, чтобы `PRINT` и `INPUT` жили в одном IDE-side потоке
   - execution/runtime boundary при этом остался прежним: runtime знает только про `IInputProvider`/`IOutputWriter`, а console UX изолирован в `Services/Input`
31. Добавлен учебный гид по текущему языковому baseline:
   - создан `docs/LANGUAGE_GUIDE.md` как одна цельная книга по Axiom на русском языке
   - материал ориентирован на школьника и покрывает только реально поддерживаемые конструкции языка
   - примеры в книге сверены с `docs/SPECIFICATION.md` и existing runnable sample catalog из `samples/`
32. Закрыт следующий узкий language milestone после v0.1.5:
   - в язык добавлен `ELSEIF` как часть многоветвенного `IF`
   - nested `IF` отдельно перепроверен и подтверждён как уже работающий baseline без искусственного лимита вложенности
   - `ELSEIF` покрыт automated tests и runnable sample `samples/control-flow/if/elseif_chain.axm`
33. Синхронизирован IDE syntax highlighting после добавления `ELSEIF`:
   - `ELSEIF` добавлен в централизованную AvalonEdit highlighting definition
   - IDE-side regression test теперь проверяет наличие `ELSEIF` в embedded XSHD resource
34. Закрыт следующий узкий language milestone после v0.1.6:
   - в язык добавлен `EXIT FOR` как ранний выход из ближайшего `FOR ... NEXT`
   - `EXIT FOR` покрыт automated tests и runnable sample `samples/control-flow/for/exit_for.axm`
   - nested `FOR`/`WHILE` отдельно перепроверены и подтверждены как уже работающий baseline без искусственного лимита вложенности
   - IDE syntax highlighting и школьный учебник синхронизированы под `EXIT FOR` и вложенные циклы
35. Закрыт следующий узкий language milestone после v0.1.7:
   - оператор `DIM` теперь поддерживает групповые объявления переменных в одной строке
   - поддержаны формы `DIM i AS INTEGER, j AS INTEGER` и `DIM anotherVar, choice AS BOOLEAN, birthDate AS STRING`
   - правило группировки зафиксировано так: `AS TYPE` относится ко всей ближайшей группе имён слева
   - change покрыт обновлёнными parser/semantics/runtime/execution tests и runnable sample `samples/operators/dim/multiple_declarations.axm`
   - `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы под новый `DIM`
36. Закрыт следующий узкий language milestone после v0.1.8:
   - в язык добавлен минимальный baseline `DO WHILE ... LOOP`
   - реализована только pre-check форма без `DO UNTIL`, `LOOP WHILE`, `LOOP UNTIL` и `EXIT DO`
   - nested `DO WHILE` отдельно перепроверен automated tests внутри других циклов
   - IDE syntax highlighting и школьный учебник синхронизированы под `DO` и `LOOP`
   - change покрыт обновлёнными lexer/parser/semantics/runtime/execution/IDE tests и runnable sample `samples/control-flow/do/do_while_loop.axm`
37. Закрыт следующий узкий language milestone после v0.1.9:
   - в язык добавлен `EXIT DO` как ранний выход из ближайшего `DO ... LOOP`
   - `EXIT DO` допустим только внутри `DO ... LOOP` и даёт отдельную semantic diagnostic вне этого контекста
   - runtime подтверждает, что `EXIT DO` выходит именно из ближайшего `DO`, а не из вложенного `FOR` или `WHILE`
   - школьный учебник и спецификация синхронизированы под `EXIT DO`
   - change покрыт обновлёнными parser/semantics/runtime/execution tests и runnable sample `samples/control-flow/do/exit_do.axm`
38. Закрыт следующий узкий language milestone после v0.1.10:
   - семейство `DO ... LOOP` доведено до полного основного набора форм: `DO WHILE`, `DO UNTIL`, `LOOP WHILE`, `LOOP UNTIL` и `DO ... LOOP` без условия;
   - parser, semantics и runtime переведены на общую модель `DO ... LOOP`, чтобы pre-check, post-check и бесконечная формы вели себя предсказуемо и использовали один `EXIT DO`;
   - change покрыт обновлёнными lexer/parser/semantics/runtime/execution tests, acceptance test-прогоном sample-файлов и runnable samples `samples/control-flow/do/do_until_loop.axm`, `samples/control-flow/do/loop_while.axm`, `samples/control-flow/do/loop_until.axm`, `samples/control-flow/do/do_loop_exit.axm`;
   - `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы под весь baseline `DO ... LOOP`;
   - IDE highlighting синхронизирован с новым keyword `UNTIL`.
39. Закрыт следующий узкий language milestone после v0.1.11:
   - в язык добавлен полноценный QuickBASIC-подобный блок `SELECT CASE ... END SELECT`;
   - поддержаны формы `CASE value`, `CASE value1, value2`, `CASE low TO high`, `CASE IS <op> expr`, смешанные selector lists и `CASE ELSE`;
   - `SELECT CASE` вычисляет test expression один раз, идёт сверху вниз и выполняет только первую подошедшую ветку без fall-through;
   - baseline согласован с текущей системой типов: точные совпадения и `IS =` / `IS <>` работают для чисел, строк и `BOOLEAN`, а диапазоны и упорядоченные сравнения допустимы только для чисел;
   - change покрыт обновлёнными lexer/parser/semantics/runtime/execution/IDE tests и runnable samples `samples/control-flow/select_case/exact_values.axm`, `samples/control-flow/select_case/ranges_and_is.axm`, `samples/control-flow/select_case/string_values.axm`, `samples/control-flow/select_case/mixed_selectors.axm`;
   - `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы под baseline `SELECT CASE`.
40. Закрыт следующий узкий language milestone после v0.1.12:
   - общая модель сравнений языка расширена ordered string comparisons через `<`, `<=`, `>`, `>=`;
   - строковые сравнения зафиксированы как ordinal, то есть по кодам символов без локали;
   - `BOOLEAN` сознательно оставлен только с `=` и `<>`, без `>`, `<`, `>=`, `<=`;
   - `SELECT CASE` автоматически расширен под string ranges `CASE "A" TO "M"` и string comparisons `CASE IS > "M"`;
   - change покрыт обновлёнными semantics/runtime/execution tests и runnable samples `samples/expressions/comparisons/string_order.axm` и `samples/control-flow/select_case/string_ranges.axm`;
   - `docs/SPECIFICATION.md` и `docs/LANGUAGE_GUIDE.md` синхронизированы под новое правило сравнений.
41. Закрыт IDE follow-up по file manager панели:
   - `View -> File Manager` заменил старый `View -> Files Pane` в shell-меню;
   - левая панель переведена в lazy file-manager tree по дискам с click-to-open;
   - кнопка `Refresh` убрана из UI;
   - `.axm`, `.txt`, `.md` открываются как редактируемые документы;
   - IDE и XAML/unit tests синхронизированы под новый файловый baseline.
42. Закрыт UI/UX follow-up по File Manager:
   - expander дерева переведён на `+/-` индикатор;
   - добавлены отдельные иконки папки и файла в узлах дерева;
   - в шапку `File Manager` добавлена кнопка `X` для прямого закрытия панели;
   - открытие файлов из дерева переведено в editable baseline для текстовых форматов (включая `.json`, `.css` и др.), `.exe` блокируются и не открываются;
   - тесты и документация синхронизированы с новым поведением.
43. Закрыт UI/UX follow-up по нижней панели `Output` / `Diagnostics`:
   - добавлены `X`-кнопки закрытия на вкладках `Output` и `Diagnostics`;
   - `View -> Output` и `View -> Diagnostics` переведены в checkable toggles видимости вкладок;
   - при успешном `Run` `Output` автоматически открывается даже после ручного скрытия;
   - при запуске с ошибками `Diagnostics` автоматически открывается даже после ручного скрытия;
   - при скрытии обеих вкладок нижняя область и splitter полностью схлопываются в layout;
   - включён вертикальный resize нижней панели через `GridSplitter`;
   - для дерева `File Manager` закреплены `Auto`-scrollbars;
   - IDE tests и docs синхронизированы с новым baseline.
44. Закрыт bugfix по вертикальному скроллу `File Manager` при включённых `Output`/`Diagnostics`:
   - левая панель больше не растягивается на нижний pane и не уводит нижнюю кнопку скроллбара под `Output`/`Diagnostics`;
   - добавлен XAML regression test, что `File Manager` не перекрывает нижние row-области;
   - spec/architecture/worklog синхронизированы с зафиксированным поведением.
45. Закрыт UI follow-up по контекстному меню `File Manager`:
- правый клик по узлу сначала переносит selection на него и только потом открывает контекстное меню;
- контекстное меню получило рабочие команды `Открыть в Axiom IDE` / `Перейти в`, `Открыть во внешнем приложении`, `Показать скрытые файлы`, `Свойства`;
- скрытые файлы можно временно включать прямо из меню, а системные элементы по-прежнему фильтруются;
- selection узла переведён в контрастный инверсный режим;
- неподдержанные file-operations оставлены как disabled placeholders, без расширения redesign в create/rename/delete scope.
46. Закрыт узкий layout bugfix по границе `File Manager`:
- вертикальный splitter между `File Manager` и editor area больше не тянется через весь shell на `Output`/`Diagnostics`;
- граница визуально заканчивается на горизонтальном splitter нижней панели, как в референсе shell layout;
- добавлен XAML regression test на отсутствие `Grid.RowSpan="3"` у этого column-splitter.
47. Закрыт file-operations follow-up по `File Manager`:
- placeholder-пункты `Новый файл...`, `Новый каталог...`, `Вырезать`, `Скопировать`, `Вставить`, `Переименовать`, `Перенести в Корзину` переведены в рабочие shell-команды;
- create/rename используют текстовый prompt, delete в Корзину требует отдельного подтверждения `Да / Нет`;
- cut/copy/paste работают как файловые операции внутри дерева, а rename/move/delete синхронизируют открытые документы редактора;
- добавлены IDE viewmodel/XAML tests и отдельные temp-dir tests для `FileOperationsService`.
48. Закрыт UX follow-up по `File Manager`:
- пункт `Перенести в Корзину` переименован в `Удалить`, сохранив семантику отправки в Корзину после подтверждения;
- refresh после create/rename/cut/copy/paste/delete больше не схлопывает дерево до `Disk (C:)`: раскрытые ветки и рабочий selection восстанавливаются path-based reload логикой;
- добавлены regression tests на сохранение expanded-state после create/rename и на обновлённый delete label.
49. Закрыт follow-up по root cause сохранения expanded-state:
- `FileTreeNode` переведён на notifying-model (`ViewModelBase`), чтобы WPF реально синхронизировал `IsExpanded` между деревом и VM;
- добавлен regression test на `PropertyChanged` для `FileTreeNode.IsExpanded`;
- ручной smoke-pass в живом окне IDE по-прежнему нужен как финальная визуальная проверка.
50. Закрыт `View -> Status Bar` follow-up:
- placeholder-пункт `Status Bar` переведён в реальный checkable toggle, скрывающий/показывающий нижнюю строку состояния shell;
- видимость `Status Bar` теперь входит в persisted shell-state и восстанавливается при следующем запуске IDE;
- добавлены viewmodel/persistence/XAML regression tests на status bar visibility.

## Ближайшие задачи по порядку

1. Закрыть `Phase 9: Tests & smoke pass` как следующий узкий slice:
   - автоматическая часть уже повторно подтверждена через `dotnet build Axiom.sln` и `dotnet test Axiom.sln`;
   - вручную проверить сценарии восстановления `WindowState`/`File Manager`, новый find/replace dialog, lazy-дерево file manager и visual clean line-number gutter;
   - вручную проверить новый UX нижней панели: `X` на `Output`/`Diagnostics`, checkable toggles в `View`, автосхлопывание/автораскрытие pane и вертикальный resize;
   - отдельно проверить editable-открытие текстовых форматов (например `.json`/`.css`) и блокировку `.exe`;
   - выполнить ручной shell smoke pass по новому IDE baseline;
   - зафиксировать результат в `docs/WORKLOG2.md`.
2. После `Phase 9` решить, что остаётся в post-v0.2 polish:
   - оставить ли placeholder-пункты `View/Help` как есть;
   - нужен ли отдельный follow-up по shell convenience без расширения scope redesign.
3. Отдельно не расширять redesign обратно в language/runtime scope:
   - execution boundary остаётся прежней;
   - если всплывут новые задачи, брать только shell-level polishing или bugfix slices.
4. После функционального закрытия фаз 6-8 выполнить `Phase 9: Tests & smoke pass`:
   - расширить `tests/Axiom.Ide.Tests` по document workflow, recent-files menu, files pane и run UX;
   - отдельно прогнать ручной smoke-check через `dotnet run --project src/Axiom.Ide/Axiom.Ide.csproj`.
5. Считать консольный `INPUT` внутри `Output` locked baseline и не откатывать его обратно к panel/card-вводу без отдельного IDE milestone.
6. Использовать старый `AxiomNext.IDE` только как reference project:
   - можно заимствовать isolated WPF/AvalonEdit patterns, resource dictionaries и service-level workflow ideas;
   - нельзя переносить монолитный `MainWindow`, прямые зависимости на старый compiler/runtime и многооконную legacy-shell логику как есть.

## Обновление плана - 2026-04-20

1. `Phase 9: Tests & smoke pass` по текущему baseline `IDE Redesign (v0.2)` закрыт через автоматизированный `Axiom.Ide.SmokeHarness`:
   - smoke harness теперь прогоняет file manager, editable/non-editable open paths, `Find/Replace`, `Run -> Output`, `Run -> Diagnostics`, toggle поведения `Output`/`Diagnostics`/`Status Bar`, а также persisted shell-state для `WindowState` и `File Manager`;
   - ручной разовый smoke-pass больше не является обязательным единственным блокером для этого этапа.
2. Следующий практический шаг по IDE tooling:
   - решить, нужно ли расширять harness на context-menu/file-operations сценарии `File Manager`, или текущий shell-smoke baseline уже достаточно стабилен;
   - если расширение не нужно, считать следующий IDE шаг post-v0.2 polish / cleanup, а не продолжение `Phase 9`.
3. Отложенный higher-order polymorphic этап по массивам не смешивать с IDE tooling:
   - `ARRAYMAP(T -> U)` и `ARRAYREDUCE(A + T -> A)` остаются отдельным документированным language-slice на будущее.
7. В IDE milestone не менять `Axiom.Execution`, `Axiom.Runtime`, `Axiom.Semantics`, `Axiom.Parsing`, `Axiom.Lexing` и `Axiom.Core`, если это не требуется для устранения чисто IDE-интеграционного дефекта.
8. Обновлять `docs/WORKLOG2.md` после каждого завершённого IDE-подшага и синхронизировать `docs/ARCHITECTURE.md`/`docs/IDE_REDESIGN.md`, если меняется техническая структура shell.

## После этого

Следующий milestone (IDE Redesign v0.2) — фиксированный порядок работ:

### Phase 1: Documentation (выполнено)
- [x] Создан `docs/IDE_REDESIGN.md` с locked decisions, refs, milestone map
- [x] Обновлён `docs/ARCHITECTURE.md` под новый IDE shell baseline
- [x] Обновлён `docs/PLANS.md` с IDE milestone
- [x] Обновлён `docs/WORKLOG.md`

### Phase 2: Shell architecture refactor (выполнено)
- [x] Текущий IDE state разбит на child viewmodels и focused services
- [x] Введены `DocumentTabViewModel`, `FilesPaneViewModel`, `BottomPaneViewModel`, `StatusBarViewModel`
- [x] Введены document workspace service и prompt service
- [x] Shell перестал опираться на legacy-монолит `MainWindowViewModel`

### Phase 3: Document tabs (выполнено)
- [x] Введены document tabs и document-workspace flow
- [x] Работают New, Open, Save, Save All, Close Tab и last-tab fallback
- [x] Есть dedupe по пути, dirty state и save/discard/cancel prompt
- [x] `Save As...` доведён как отдельная явная UI-команда и shortcut

### Phase 4: New shell layout (выполнено)
- [x] `MainWindow` перестроен в shell с menu bar, toolbar, files pane, document tabs, bottom pane и status bar
- [x] Правая колонка recent files и `GroupBox`-каркас убраны
- [x] Ресурсы вынесены в focused resource dictionaries
- [ ] View/Help меню пока содержат placeholder-пункты без полного command wiring

### Phase 5: Editor commands (выполнено)
- [x] Подключён editor-command surface для active `AvalonEdit`
- [x] Добавлен editor context menu по правой кнопке
- [x] Внедрён custom `Find/Replace` dialog для `Find` / `Find Next` / `Find Previous` / replace-операций
- [x] Закрыты standard shortcuts для editor/search flow и shell-level `Save As`

### Phase 6: Recent files migration (выполнено)
- [x] Перенести recent files в `File > Open Recent`
- [x] Сохранить `RecentFilesService`, меняя только UI-поверхность
- [x] Обрабатывать missing recent entry через удаление из persistence

### Phase 7: Files pane (выполнено)
- [x] Root-path уже переключается по active document / last-used directory / `MyDocuments`
- [x] Добавить реальное current-folder tree вместо placeholder
- [x] Реализовать directories first, files second
- [x] Перевести левую панель в file-manager mode: lazy-expand tree, click-to-open, без `Refresh`

### Phase 8: Run/output/diagnostics UX (выполнено)
- [x] Auto-switch bottom pane на `Output` при run уже есть
- [x] Auto-switch на `Diagnostics` при failure уже есть
- [x] INPUT focus/state transitions доведены до shell-level baseline
- [x] Blocking rules во время активного run/`INPUT` введены для `Run` и document-navigation команд

### Phase 9: Tests & smoke pass (закрыто)
- [x] Расширить document tab workflow tests
- [x] Добавить files pane root/tree tests
- [x] Добавить search / recent-files menu tests
- [x] Добавить run UX / bottom pane blocking/focus tests
- [x] Добавить persistence tests для shell-state (`WindowState` + `View -> File Manager`) и XAML/code-behind проверки для нового find/replace dialog
- [x] Повторно подтвердить solution-level build/test после смежных регрессий вне IDE (`dotnet build Axiom.sln`, `dotnet test Axiom.sln`)
- [x] Закрыть shell smoke pass через автоматизированный `Axiom.Ide.SmokeHarness`

## Blockers

Пока явных блокеров нет.

## Deferred items

Отложено до после ядра:
- тёмная тема;
- автодополнение;
- сложная навигация по коду;
- отладчик;
- байткод или компиляция;
- session restore;
- Variables/Assistant panes;
- multiple themes / theme switching.

## Repository workflow baseline

Effective from `2026-04-15 18:13`:
- all evolving repository documents are append-only;
- chronology must be preserved from top to bottom;
- new records must be added only below existing records;
- every new `docs/WORKLOG2.md` entry must use `## Session <N> - YYYY-MM-DD HH:mm - <title>`;
- historical legacy entries stay as they are; do not invent timestamps for them.
## Update 2026-04-15 - IDE formatting modes

1. Зафиксировать baseline `Format -> Classic` как текущий IDE slice:
   - menu уже должно содержать `Classic` и `Modern`;
   - `Classic` уже должен автоформатировать строку при уходе с неё и при loss of focus;
   - formatting должен опираться на highlighting vocabulary, чтобы новые keywords/built-ins подхватывались без нового hardcoded list.
2. Следующий порядковый follow-up после этого slice:
   - довести `Modern` style (keywords/functions в `Title Case` вместо full uppercase) на той же service boundary;
   - вручную проверить `Classic` в живом `Axiom.Ide`: Enter, уход со строки стрелками/мышью, inline `REM`, malformed line, read-only/selection cases;
   - после manual smoke-pass вернуться к оставшемуся `Phase 9` shell smoke pass.

## Update 2026-04-15 - IDE diagnostics logging

1. Ввести в `Axiom.Ide` файловой session trace по образцу старого `AxiomNext.IDE`, чтобы расследовать плавающие shell-ошибки без ручного воспроизведения в отладчике.
2. Лог должен фиксировать как минимум:
   - запуск IDE;
   - `Run` request/result для активного документа;
   - console output/input events;
   - fallback-сценарий, когда `ProgramRunResult.OutputText` есть, а transcript в `Output` пустой;
   - unhandled exceptions.
3. Следующий шаг после внедрения trace:
   - вручную воспроизвести сценарий с пустым `Output` на проблемном примере и на рабочем примере;
   - сравнить session logs двух запусков;
   - если проблема подтвердится именно в shell-связке, добить точечный фикс до возврата к `Modern`.

## Update 2026-04-15 - save-before-run behavior

1. Зафиксировать IDE-правило: выполнение программы возможно только из сохранённого файла на диске.
2. `Run` не должен молча блокироваться на dirty/untitled документе:
   - IDE показывает пользователю явный prompt на сохранение;
   - после успешного `Save`/`Save As` выполняется уже дисковая версия файла;
   - при отказе или отмене сохранения запуск не производится.
3. Следующий шаг после внедрения:
   - вручную проверить сценарии `Untitled -> Run`, `dirty file -> Run`, `saved file -> Run`;
   - отдельно решить, нужен ли отдельный пункт меню для открытия/очистки `Logs`.

## Update 2026-04-15 - IDE input regression follow-up

1. После trace-логирования подтверждён конкретный дефект shell input flow: нижняя панель должна не только отображать `CurrentInputText`, но и возвращать пользовательский ввод обратно в `ProgramConsoleService`.
2. Ближайшая ручная проверка:
   - `samples/smoke/v0_1_baseline.axm` с вводом имени и возраста;
   - повторная сверка session log, чтобы `console.input.submit` фиксировал реальные значения, а не пустые строки.
3. После стабилизации `INPUT`-потока можно возвращаться к UX-пункту про меню логов.

## Update 2026-04-15 - Modern formatting mode

1. Завершить второй IDE formatting mode на уже существующем `Classic` formatter pipeline.
2. `Modern` должен:
   - использовать те же spacing и normalization rules;
   - менять только casing keywords, type names, `REM` и built-in function calls на `Title Case`;
   - переключаться из меню `Format` без disabled placeholder.
3. Следующий шаг после внедрения:
   - вручную проверить `Classic` и `Modern` на Enter, уходе со строки мышью и переходе стрелками;
   - отдельно добить UX вокруг `Logs` menu, если это останется актуально.

## Update 2026-04-15 - document-wide style rewrite

1. При смене `Classic` / `Modern` автоматически переписывать весь текст активной `.axm`-вкладки в выбранный стиль.
2. При активации другой уже открытой `.axm`-вкладки автоматически приводить её к текущему shell-wide formatting mode.
3. Не применять этот rewrite к не-Axiom документам (`.txt`, `.md`, прочие текстовые вкладки).
4. Следующий шаг после внедрения:
   - вручную проверить несколько открытых вкладок с `.axm` и `.txt`;
   - убедиться, что после style rewrite dirty-state и последующее `Save` ведут себя ожидаемо;
   - после этого можно вернуться к UX-вопросу про меню логов.

## Update 2026-04-15 - status bar path selection

1. Сделать path area в нижнем status bar выделяемой мышью и копируемой без перехода к режиму редактирования.
2. Сохранить визуальный характер status bar: путь остаётся read-only и служит только для просмотра/копирования.
3. Следующий шаг после внедрения:
   - вручную проверить выделение подстроки мышью и `Ctrl+C` на длинном пути;
   - оценить, нужен ли отдельный follow-up вроде `Copy Full Path` по правому клику.

## Update 2026-04-16 - IDE block auto-completion and structural indent

1. Зафиксировать отдельный IDE slice для структурного автозавершения блоков в `.axm`-редакторе.
2. Первая версия должна покрыть:
   - автодобавление closing lines для `If ... Then`, `For`, `While`, `Select Case`, `Do`, `Do While`, `Do Until`;
   - автодобавление closing lines для `Sub ... End Sub` и `Function ... End Function`;
   - выравнивание closing line строго под opening line;
   - единый шаг внутреннего отступа `4` пробела;
   - сохранение рабочего уровня отступа при `Enter` внутри текущего блока и корректный возврат на уровень родителя или в нулевой уровень после выхода из вложенности.
3. Отдельно не переусложнять v1:
   - не делать IntelliSense и список подсказок;
   - не пытаться угадывать post-condition формы `Do ... Loop While/Until` заранее;
   - не тащить shell-level UX в parser/runtime.
4. Следующий шаг после фиксации спецификации:
   - спроектировать editor-side сервис или controller для определения block context и расчета автоотступа;
   - покрыть поведение automated tests на `Enter`, вложенные `If`, переход на уровень родителя, `Else`/`ElseIf`, `Case`, `Do`-варианты, `Sub`/`Function` и single-undo contract;
   - зафиксировать engineering rule: любая новая будущая парная блочная конструкция языка должна сразу подключаться к IDE auto-completion и test coverage;
   - только после этого переходить к реализации в `Axiom.Ide`.

## Update 2026-04-16 - IDE block auto-completion implementation

1. Реализован editor-side structural editing slice в `Axiom.Ide`:
   - отдельный `AxiomBlockEditingService`;
   - подключение в line-transition flow `MainWindow.xaml.cs`;
   - IDE tests и service tests на поддерживаемые block pairs и вложенный автоотступ.
2. Автоматически покрыты текущие поддерживаемые парные блоки:
   - `If ... End If`;
   - `For ... Next`;
   - `While ... End While`;
   - `Select Case ... End Select`;
   - `Do ... Loop`;
   - `Sub ... End Sub`;
   - `Function ... End Function`.
3. Проверки, уже выполненные в репозитории:
   - `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\``;
   - `dotnet build Axiom.sln -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\``;
   - `dotnet test Axiom.sln --no-build -p:ArtifactsRoot=C:\Axiom\.artifacts_isolated\``.
4. Следующий шаг после этой реализации:
   - выполнить ручной smoke pass в живом `Axiom.Ide` на `Enter`, уходе мышью со строки, вложенных `If`, `Select Case`, `Do`, `Sub`, `Function` и на одном `Undo` после автозавершения;
   - отдельно оценить, нужен ли следующий UX-slice для более точного post-condition редактирования `Loop While/Until`.

## Update 2026-04-16 - IDE branch dedent for If and Select Case

1. Для editor-side structural editing добавлено дополнительное UX-правило:
   - `ElseIf` и `Else` при выходе со строки автоматически выравниваются под родительский `If`;
   - `Case` и `Case Else` при выходе со строки автоматически выравниваются под родительский `Select Case`.
2. Automated tests должны покрывать:
   - branch-dedent после body line в `If`;
   - branch-dedent после body line в `Select Case`;
   - сохранение следующего body-indent уже от новой выровненной branch-строки.
3. Следующий шаг после этого follow-up:
   - вручную прогнать live-editor smoke pass на `ElseIf`, `Else`, `Case`, `Case Else`, включая nested blocks, mouse-driven уход со строки и `Undo`.

## Update 2026-04-16 - Nested If close detection inside branch bodies

1. В `AxiomBlockEditingService` исправлено определение уже существующего matching-close:
   - внешний `End If` больше не считается закрытием внутреннего вложенного `If`, если уровни отступа не совпадают;
   - тот же guarding rule применяется к stack-tracking для editor-side поиска открытых блоков.
2. Automated tests должны отдельно покрывать сценарий:
   - nested `If` внутри `ElseIf`-ветви, ниже которого уже существует внешний `End If`, но внутренний `End If` всё равно должен автоматически добавляться.
3. Следующий шаг:
   - вручную проверить в живом `Axiom.Ide`, что nested `If` внутри `ElseIf`/`Else` и nested `Select Case` внутри ветвей больше не теряют свои closing lines.

## Update 2026-04-16 - Repeated branch body insertion

1. Branch-строки `ElseIf`, `Else`, `Case`, `Case Else` теперь должны вести себя как полноценные branch headers и при уходе со строки:
   - не только выравниваться под родительский `If` / `Select Case`;
   - но и создавать пустую body-line даже если следующая строка уже занята существующим `End If`, `End Select` или другой branch-line.
2. Automated tests должны покрывать:
   - повторный `ElseIf` перед существующим `End If`;
   - повторный `Case` перед существующим `End Select`;
   - корректное сохранение caret-offset после branch-dedent и последующей IDE-side вставки.
3. Следующий шаг:
   - вручную прогнать в живом редакторе повторные `ElseIf` и `Case`, чтобы подтвердить UX не только на `Enter`, но и при уходе вниз стрелкой или мышью.

## Update 2026-04-16 - Select Case branch indentation correction

1. Уточнено окончательное UX-правило для `Select Case`:
   - `Case` и `Case Else` не стоят на одном уровне с `Select Case`;
   - они должны жить на один уровень глубже относительно строки `Select Case`;
   - тело каждого `Case` получает еще один дополнительный уровень отступа относительно самой строки `Case`.
2. Automated tests должны закреплять именно эту геометрию:
   - `Select Case` / `End Select` на одном уровне;
   - `Case` / `Case Else` на `+4` пробела внутрь;
   - содержимое `Case` еще на `+4` внутрь относительно `Case`.
3. Следующий шаг:
   - вручную прогнать nested `Select Case` в живом `Axiom.Ide`, чтобы убедиться, что и outer, и inner `Case` сохраняют эту иерархию при `Enter`, стрелках и уходе мышью.

## Update 2026-04-16 - Block scope migration plan

1. Целевое направление языка:
   - уйти от текущей модели, где отдельные scope есть только у top-level и `Sub` / `Function`;
   - перейти к C#-подобной блочной области видимости для `If`, `ElseIf`, `Else`, `For`, `While`, `Select Case`, `Case`, `Case Else`, `Do ... Loop`;
   - каждая такая конструкция должна открывать собственный локальный block scope для объявлений `Dim` и `Const`.
2. Что именно должно измениться по сравнению с текущим состоянием:
   - `Dim` внутри `If` / `For` / `While` / `Select Case` перестает жить во всей окружающей `Sub` / `Function`;
   - повторный `Dim x ...` в другой sibling-ветви допустим, если это уже другой block scope;
   - имя, объявленное внутри блока, после выхода из блока становится недоступным;
   - `For`-control variable должна либо принадлежать собственному scope цикла, либо использовать уже существующее имя только по явному правилу, которое нужно отдельно закрепить в спецификации.
3. Migration plan по шагам:
   - шаг 1: описать новую модель scope в `docs/SPECIFICATION.md` и отдельно перечислить breaking changes относительно текущего поведения;
   - шаг 2: расширить binder / `BoundScope` до настоящих block scopes для всех перечисленных statement families;
   - шаг 3: обновить diagnostics:
     - имя недоступно вне блока;
     - повторное объявление только внутри того же самого scope;
     - теневое объявление в дочернем блоке либо разрешить, либо отдельно запретить по явному правилу;
   - шаг 4: обновить runtime semantics для `Dim` внутри блоков, чтобы переменные действительно жили только в рамках runtime block frame, а не в scope всей процедуры;
   - шаг 5: добавить acceptance samples и automated tests на block-scope visibility, shadowing, `For`-переменные, sibling branches и nested blocks;
   - шаг 6: только после language-core изменений при необходимости адаптировать IDE diagnostics / editor UX под новые scope-правила.
4. Отдельный design question перед реализацией:
   - C#-модель требует явного объявления переменной (`int x` / `var x`);
   - Python-подобная модель разрешает появление локального имени по первому присваиванию без отдельного `Dim`;
   - до принятия решения спецификацию не менять на implicit locals автоматически, потому что это отдельный language change с большими последствиями для диагностики, typo-safety и совместимости.
5. Ближайший следующий шаг:
   - сначала сформулировать decision memo: остаемся ли строгими, как C#, с обязательным `Dim`, или сознательно проектируем hybrid model: C#-подобные block scopes + Python-подобное implicit local assignment.
6. Иллюстративные примеры для обсуждения до реализации:
   - текущая модель:
```mbas
IF flag THEN
    DIM x AS INTEGER = 1
END IF

PRINT x
```
     сейчас `x` видна и после `END IF`, потому что `IF` не создает scope;
   - целевая C#-подобная модель:
```mbas
IF flag THEN
    DIM x AS INTEGER = 1
    PRINT x
END IF

PRINT x
```
     строка `PRINT x` после `END IF` должна давать semantic diagnostic: имя `x` недоступно вне блока;
   - sibling branches:
```mbas
IF a = 1 THEN
    DIM message AS STRING = "one"
    PRINT message
ELSE
    DIM message AS STRING = "other"
    PRINT message
END IF
```
     в новой модели это должно быть допустимо, потому что ветви `THEN` и `ELSE` — разные block scopes;
   - `FOR` с локальной переменной тела:
```mbas
FOR i = 1 TO 3
    DIM localSum AS INTEGER = i * 10
    PRINT localSum
NEXT
```
     в новой модели `localSum` существует только внутри тела данного `FOR`;
   - после цикла:
```mbas
FOR i = 1 TO 3
    DIM localSum AS INTEGER = i * 10
NEXT

PRINT localSum
```
     строка `PRINT localSum` должна стать ошибкой видимости;
   - open question по `FOR`-переменной:
```mbas
FOR i = 1 TO 3
    PRINT i
NEXT

PRINT i
```
     нужно отдельно решить, должна ли `i` жить только внутри `FOR`, как в modern C#-стиле мышления, или оставаться доступной после `NEXT` по специальному правилу совместимости;
   - possible Python-hybrid variant:
```mbas
IF flag THEN
    total = 10
    PRINT total
END IF
```
     если заимствовать Python-подобное implicit local assignment, `total` могла бы появляться без `DIM`; если остаемся ближе к C#, здесь должна быть ошибка и нужно писать `DIM total AS INTEGER = 10`.

## Update 2026-04-16 - Block scope decision finalized

1. Принято окончательное language decision:
   - Axiom переходит на строгую C#-подобную block-scope модель;
   - язык всегда работает в explicit-declaration режиме;
   - Python-подобные implicit locals по присваиванию не входят в текущую спецификацию;
   - `Option Explicit On/Off` не поддерживается в текущем договоре языка и не влияет на текущее поведение.
2. Нормативные последствия решения:
   - `If`, `ElseIf`, `Else`, `For`, `While`, `Do ... Loop`, `Case`, `Case Else` открывают собственные block scopes;
   - `FOR`-control variable объявляется самой строкой `FOR`, живет только внутри данного цикла и не требует отдельного `DIM`;
   - локальные `Dim` / `Const` после выхода из блока становятся недоступными;
   - sibling branches могут повторно использовать одинаковые имена;
   - shadowing имени из enclosing local scope запрещено.
3. Следующий шаг после закрепления решения:
   - обновить binder / `BoundScope` под nested statement-level scopes;
   - затем обновить runtime block lifetime для `Dim` / `Const`;
   - затем добавить automated tests и runnable `.axm` acceptance samples на новые scope-правила;
   - только после этого переходить к IDE-side follow-up, если он потребуется для новых diagnostics.

## Update 2026-04-16 - Block scopes implemented

1. Реализация завершена в language core:
   - binder переведён на nested statement-level scopes;
   - runtime переведён на stack-based `RuntimeScope` с block lifetime;
   - `FOR`-control variable теперь объявляется самой строкой `FOR` и живёт только внутри цикла;
   - добавлены diagnostics для forbidden shadowing и проверки на недоступность имён после выхода из блока.
2. Regression coverage обновлено:
   - добавлены semantic tests на branch locals, sibling-name reuse, `FOR` visibility и shadowing;
   - обновлены runtime / execution tests под новую семантику `FOR`;
   - добавлены acceptance samples для positive и negative block-scope сценариев;
   - существующие `FOR` samples синхронизированы с новым правилом без предварительного `DIM`.
3. Следующий шаг:
   - вручную открыть новые samples в `Axiom.Ide` и проверить UX вокруг diagnostics:
     - `samples/smoke/block_scope_semantics.axm`;
     - `samples/smoke/block_scope_full_manual.axm`;
     - `samples/acceptance/negative/block_scope_visibility.axm`;
     - `samples/acceptance/negative/for_step_zero.axm`;
   - после manual smoke-pass синхронизировать при необходимости `docs/LANGUAGE_GUIDE.md` с новой моделью областей видимости и `FOR`.

## Update 2026-04-16 - Human-facing shadowing diagnostics

1. UX-решение зафиксировано:
   - сообщения о запрещённом shadowing должны оставаться человекопонятными и контекстными;
   - общий смысл ошибки один: имя уже объявлено во внешней области видимости;
   - вторая часть сообщения обязана называть текущую конструкцию, например `в блоке IF`, `в ветви ELSE`, `в цикле FOR`.
2. Engineering rule на будущее:
   - это не частное правило только для `IF` или возможного будущего `TYPE`;
   - любая новая блочная конструкция, которая вводит новый scope, должна сразу получать свой declaration-context label и тестовое покрытие для diagnostics;
   - возврат к абстрактным сообщениям без указания конструкции считается regression в DX/UX diagnostics.
3. Следующий шаг после текущего bugfix-slice:
   - вручную проверить текст `MB2016` в `Axiom.Ide` на `IF`, `FOR` и negative block-scope samples;
   - затем при появлении новой block construct расширять только централизованный список declaration contexts и соответствующие binder tests.

## Update 2026-04-16 - Language guide synced with block scopes

1. Документационный follow-up закрыт:
   - `docs/LANGUAGE_GUIDE.md` синхронизирован с текущей C#-подобной block-scope моделью языка;
   - главы `DIM`, `IF`, `SELECT CASE`, `FOR`, `WHILE`, `DO ... LOOP` теперь объясняют, где живут локальные переменные;
   - устаревшие учебные примеры с `DIM i` перед `FOR i = ...` убраны из руководства.
2. Что это даёт:
   - учебная книга больше не противоречит текущему поведению interpreter / binder;
   - читатель теперь видит не только синтаксис, но и практическое правило видимости имён внутри блоков.
3. Следующий шаг:
   - при следующем редакторском проходе вручную перечитать именно главы про `SUB` и `FUNCTION`, чтобы при желании так же явно подсветить routine-scope рядом с уже описанными block scopes.

## Update 2026-04-16 - DIM type inference from initializer

1. Реализовано расширение `DIM`:
   - теперь `DIM name = expression` поддерживается как краткая форма с выводом типа по инициализатору;
   - grouped-вариант `DIM x, y = expression` тоже поддержан и применяет один выведенный тип ко всей группе;
   - `CONST` не меняется: там тип по-прежнему задаётся явно.
2. Зафиксированные правила:
   - `10` -> `INTEGER`;
   - `1.5` -> `DOUBLE`;
   - `"Alice"` -> `STRING`;
   - `TRUE` / `FALSE` -> `BOOLEAN`;
   - `DIM name` без `AS` и без `=` теперь даёт отдельную semantic diagnostic `MB2017`.
3. Следующий шаг:
   - вручную открыть в `Axiom.Ide`:
     - `samples/operators/dim/inferred_type_from_initializer.axm`;
     - `samples/acceptance/negative/dim_requires_type_or_initializer.axm`;
   - затем при необходимости отдельно обсудить, нужен ли когда-нибудь аналогичный shorthand для `CONST`.

4. Документационная оговорка:
   - mixed-form запись `DIM count = 10, name = "Alice", ready = TRUE, price = 1.5` тоже считается официально поддержанной и должна оставаться явно описанной в spec и guide.

## Update 2026-04-16 - HTML manual baseline

1. Учебный контур документации переведён в новый формат:
   - внутри репозитория создан отдельный статический HTML-учебник в `docs/manual/`;
   - навигация построена как у классических технических онлайн-руководств: постоянное левое меню + центральная статья;
   - текущий набор страниц покрывает старт, данные и ввод, выражения и ветвление, циклы, routines, массивы, built-ins, диагностику, готовые программы и шпаргалку.
2. Документационное решение:
   - `docs/manual/index.html` становится основным человекочитаемым учебником для дальнейшего развития;
   - `docs/LANGUAGE_GUIDE.md` сохраняется как линейная markdown-версия и редакторский/исторический draft, но больше не считается основным публичным форматом курса.
3. Следующий шаг:
   - при следующем редакторском проходе либо углублять главы HTML-учебника примерами и иллюстрациями по `samples/`, либо отдельно решить, нужен ли IDE-side entry point вроде `Help -> Open Manual`.
## Update 2026-04-16 - Manual refactored toward reference format

1. Направление учебника уточнено:
   - HTML manual больше не должен быть только линейным курсом;
   - материал переводится в справочный формат с отдельными страницами по операторам и со сгруппированными, но пофункционально расписанными built-ins;
   - в структуру добавлен единый указатель с двумя фильтрами: по первой букве официального имени и по смысловому разделу.
2. Что уже заложено в текущем проходе:
   - отдельная страница по типам данных с формальными свойствами `INTEGER`, `DOUBLE`, `STRING`, `BOOLEAN`;
   - расширенный раздел по переменным и константам: идентификаторы, reserved words, некорректные примеры имён, явность объявлений;
   - отдельные страницы по `SELECT CASE`, `FOR`, `WHILE`, `DO ... LOOP`, `SUB`, `FUNCTION`, `RETURN`, массивам, строковым и числовым функциям.
3. Следующий шаг:
   - на следующем редакторском проходе привязать страницы manual к конкретным runnable-примерам из `samples/` и решить, нужно ли вынести в manual отдельную справочную страницу по diagnostics codes (`MB2xxx` / `MB3xxx`) с алфавитным индексом.

## Update 2026-04-16 - Manual chapters rewritten in unified reference style

1. Текущий editorial milestone по manual уточнён:
   - все основные HTML-главы приведены к одному reference-style формату;
   - отдельные статьи по `IF`, `SELECT CASE`, `FOR`, `WHILE`, `DO ... LOOP`, `SUB`, `FUNCTION`, `RETURN`, массивам и built-ins теперь не смешиваются с обзорными разделами;
   - старые агрегирующие страницы сохранены только как обзорные хабы и ведут на новые атомарные статьи.
2. Что уже зафиксировано как новый baseline manual:
   - вступление, справочник, отдельные operator/function pages и служебные главы (`diagnostics`, `examples`, `cheat-sheet`) переписаны в одном более академичном и справочном тоне;
   - формулировки по identifier length и по поддерживаемым control-flow формам приведены к текущему публичному контракту языка без лишних догадок.
3. Следующий шаг:
   - привязать ключевые manual-страницы к конкретным runnable `samples/` под каждый оператор и built-in;
   - затем при необходимости добавить отдельную индексную страницу по diagnostics codes и будущим подсистемам вроде файловых функций.

## Update 2026-04-17 - Data types chapter aligned with reference-style sample

1. Точечный editorial follow-up по `docs/manual/data-types.html` закрыт:
   - глава расширена от краткого обзора до более справочного формата;
   - добавлены классификация по роли в языке, сводная таблица типов, формы литералов, правила вывода типа в `DIM`, более формальное описание преобразований и ссылки на runnable `samples/`.
2. Принцип для дальнейших правок manual уточнен:
   - внешний референс можно использовать как ориентир по плотности и структуре материала;
   - но содержание нужно адаптировать к реальному публичному контракту Axiom, не копируя чужие категории и неподтвержденные детали.
3. Следующий шаг:
   - в том же стиле дочистить соседние справочные главы manual, начиная с `variables-and-constants.html` и страниц по вводу/выводу;
   - продолжить привязывать статьи к конкретным runnable `samples/`, чтобы manual оставался не только справочным, но и проверяемым через IDE.

## Update 2026-04-17 - LONG type and numeric overflow diagnostics

1. Языковой baseline расширен:
   - в числовую модель добавлен тип `LONG` как знаковое 64-битное целое;
   - целочисленные литералы теперь распределяются по диапазонам `INTEGER` / `LONG`, а литералы вне диапазона `LONG` и не-конечные `DOUBLE` получают syntax diagnostic `MB0004`;
   - runtime больше не допускает молчаливого переполнения числовой арифметики и сообщает `MB3008`.
2. Документация и acceptance-контур синхронизированы:
   - обновлены `docs/SPECIFICATION.md`, `docs/LANGUAGE_GUIDE.md`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html` и `docs/manual/scripts/manual.js`;
   - добавлены runnable samples для `LONG`-инференса и для overflow/error-сценариев.
3. Следующий шаг:
   - вручную открыть в `Axiom.Ide` и прогнать новые sample-программы:
     - `samples/operators/dim/long_inference.axm`;
     - `samples/expressions/numeric_literals/long_out_of_range_error.axm`;
     - `samples/expressions/arithmetic/long_overflow_error.axm`;
     - `samples/expressions/arithmetic/double_overflow_error.axm`;
   - после manual smoke-pass решить, нужен ли отдельный semantic diagnostic с более узким текстом именно для случаев явного `AS INTEGER` / `AS LONG`, когда значение не помещается в целевой тип.

## Update 2026-04-17 - DECIMAL type and explicit numeric range diagnostics

1. Языковой baseline расширен ещё на один числовой тип:
   - в Axiom добавлен `DECIMAL` как точный десятичный числовой тип по модели C# `decimal`;
   - в первой версии `DECIMAL` вводится только явно через `AS DECIMAL`, без изменения текущего type inference для литералов вида `1.5`, которые по-прежнему выводятся как `DOUBLE`;
   - арифметика и `INPUT` теперь поддерживают `DECIMAL`, а переполнение decimal-арифметики также останавливается с `MB3008`.
2. Семантика и диагностика уточнены:
   - разрешены неявные numeric widening-преобразования `INTEGER -> DECIMAL` и `LONG -> DECIMAL`;
   - автоматическое смешивание `DECIMAL` и `DOUBLE` сознательно не разрешается, чтобы сохранить предсказуемую модель чисел;
   - для случаев явного `AS INTEGER` / `AS LONG` / `AS DECIMAL`, когда константный литерал не помещается в целевой тип, добавлен semantic diagnostic `MB2018`.
3. Acceptance-контур и документация должны оставаться синхронными:
   - runnable samples для `DECIMAL` должны лежать в тематических подпапках `samples/operators/dim/` и `samples/expressions/...`;
   - manual и specification должны явно объяснять, что `DECIMAL` пока не участвует в `FOR`, а для точных денег/сумм выбирается осознанно через `AS DECIMAL`.
4. Следующий шаг:
   - вручную открыть в `Axiom.Ide` и прогнать:
     - `samples/operators/dim/decimal_explicit_declaration.axm`;
     - `samples/expressions/numeric_literals/decimal_out_of_range_error.axm`;
     - `samples/expressions/arithmetic/decimal_overflow_error.axm`;
   - затем отдельным follow-up решить, нужен ли в языке явный decimal literal suffix или отдельная функция/оператор преобразования наподобие `CDEC(...)`.
## Update 2026-04-17 - Math built-in package v1 implemented

1. Языковой milestone закрыт:
   - built-in слой переведён на metadata-driven модель с `BuiltinFunctionId`, дескрипторами built-ins и общим binder-path для numeric built-ins;
   - в первую волну math-pack добавлены `ABS`, `SGN`, `MIN`, `MAX`, `CLAMP`, `SIN`, `COS`, `TAN`, `ATN`, `ATN2`, `SQR`, `CBRT`, `EXP`, `LOG`, `LOG10`, `LOG2`, `POW`, `FLOOR`, `CEIL`, `ROUND`, `FIX`;
   - runtime-dispatch больше не завязан на string-switch и использует canonical built-in id.
2. Контракт и acceptance-контур синхронизированы:
   - обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/manual/numeric-functions.html`, `docs/manual/reference.html`, `docs/manual/scripts/manual.js`;
   - добавлены runnable samples в тематических подпапках `samples/functions/<function-name>/`, включая error-scenarios для `SQR`, `LOG`, `CLAMP` и `POW`;
   - binder/runtime/execution/IDE tests расширены под новые функции, numeric promotion, domain errors и различие `INT` vs `FIX`.
3. Следующий шаг:
   - вручную открыть в `Axiom.Ide` и прогнать минимум:
     - `samples/functions/abs/basic_usage.axm`;
     - `samples/functions/sqr/basic_usage.axm`;
     - `samples/functions/log/basic_usage.axm`;
     - `samples/functions/clamp/basic_usage.axm`;
     - `samples/functions/round/basic_usage.axm`;
     - `samples/functions/fix/basic_usage.axm`;
   - отдельно подтвердить, что `samples/functions/sqr/negative_error.axm`, `samples/functions/log/non_positive_error.axm` и `samples/functions/clamp/invalid_bounds_error.axm` показывают ожидаемые runtime diagnostics;
   - после IDE smoke-pass решить, нужен ли follow-up по второй волне scientific built-ins (`ASIN`, `ACOS`, `SINH`, `SETSIGN` и т.д.).

## Update 2026-04-17 - Automated math manual-check runner added

1. Для ручной валидации больше не нужно открывать каждый sample по одному:
   - добавлен отдельный console-runner `src/Axiom.ManualChecks/`, который использует существующий execution pipeline и сам прогоняет весь math-pack первой волны;
   - runner покрывает все положительные `basic_usage.axm` для реализованных математических built-ins, negative domain-scenarios из `samples/functions/...` и несколько inline semantic negative-cases по arity/type mismatch;
   - результат собирается в подробный текстовый отчёт и автоматически пишется во временный `.txt`-файл.
2. Новый рабочий контур для smoke-pass:
   - команда `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj` печатает путь к отчёту и сам отчёт;
   - при необходимости путь можно зафиксировать через `-- --out <path>`;
   - это не заменяет окончательный IDE UX-smoke-pass, но резко сокращает ручную проверку перед ним.
3. Следующий шаг:
   - попросить пользователя прогнать runner у себя и прислать отчёт;
   - затем, если отчёт чистый, переходить к planning/implementation второй волны scientific built-ins.

## Update 2026-04-17 - Scientific math built-ins second wave implemented

1. Вторая волна math-pack закрыта:
   - в язык добавлены `ASIN`, `ACOS`, `ASINH`, `ACOSH`, `ATANH`, `SINH`, `COSH`, `TANH`, `SETSIGN`;
   - для scientific built-ins зафиксированы domain rules и return-type policy: inverse/hyperbolic math возвращает `DOUBLE`, а `SETSIGN` сохраняет numeric-family первого аргумента;
   - runtime продолжает использовать общий metadata-driven built-in layer и те же diagnostics `MB3007`/`MB3008`.
2. Acceptance-контур расширен до полного покрытия второй волны:
   - добавлены runnable samples в `samples/functions/asin/`, `acos/`, `asinh/`, `acosh/`, `atanh/`, `sinh/`, `cosh/`, `tanh/`, `setsign/`;
   - добавлены focused negative samples для `ASIN`, `ACOS`, `ACOSH`, `ATANH`;
   - расширены `BinderTests`, `InterpreterTests`, `AcceptanceProgramTests`, `EditorFeatureTests` и console-runner `src/Axiom.ManualChecks/`.
3. Статус проверки:
   - `dotnet test Axiom.sln --no-restore -v minimal` проходит;
   - `dotnet run --project src/Axiom.ManualChecks/Axiom.ManualChecks.csproj` проходит с итогом `Passed: 44`, `Failed: 0`.
4. Следующий шаг:
   - решить, нужен ли отдельный follow-up для `CDEC(...)` или другого удобного входа в `DECIMAL`;
   - отдельно приоритизировать, оставляем ли IEEE/tuple-style math helpers (`DivRem`, `SinCos`, `ScaleB` и т.п.) вне языка надолго или заводим для них отдельный advanced-numeric milestone.

## Update 2026-04-18 - Repo-local documentation skill baseline

1. Documentation workflow переведен на явный repo-local skill:
   - добавляется `.codex/skills/axiom-doc-author/` как нормативный workflow для manual-авторинга;
   - skill фиксирует два режима работы: `reference` и `getting-started`;
   - skill закрепляет порядок источников истины `docs/SPECIFICATION.md` -> код -> `samples/` -> текущий `docs/manual/` -> исторические Microsoft-референсы только как стиль и структура;
   - skill запрещает неподтвержденные свойства языка и требует gap-reporting при расхождении `SPECIFICATION`, кода, samples и manual.
2. Человекочитаемый documentation baseline упрощен:
   - `docs/manual/` остается единственным поддерживаемым человекочитаемым форматом;
   - `docs/LANGUAGE_GUIDE.md` выводится из живой ветки документации и удаляется, чтобы не поддерживать параллельный markdown-курс;
   - все будущие редакторские изменения должны идти через manual-страницы и связанные репозиторные документы.
3. Информационная архитектура manual уточнена как постоянное требование:
   - алфавитный и тематический индекс уже существуют;
   - breadcrumbs и системный `See Also` считаются обязательной целевой моделью для следующих редакторских проходов;
   - если страница manual плохо обнаружима или не имеет понятных переходов, редакторский проход обязан либо исправить это, либо явно зафиксировать навигационный gap.
4. Следующий шаг:
   - выполнить первый редакторский проход уже через `axiom-doc-author` на ближайшем кластере справочных страниц и довязать их к runnable `samples/`;
   - в том же проходе оценить и при необходимости внедрить breadcrumbs и системный `See Also`, начиная с тех страниц, где они сильнее всего влияют на навигацию.

## Update 2026-04-18 - First skill-driven built-ins manual pass

1. Первый редакторский проход через `axiom-doc-author` зафиксирован на ближайшем справочном кластере:
   - обновлены `docs/manual/reference.html`, `docs/manual/string-functions.html` и `docs/manual/numeric-functions.html`;
   - на этих страницах внедрены breadcrumbs, явные блоки `См. также` и ссылки на runnable `samples/functions/...`;
   - устаревшие и смешанные по стилю формулировки о math-pack и built-ins заменены на формулировки, опирающиеся на текущую реализацию языка.
2. Для следующих reference-pass закрепляется локальный editorial baseline:
   - если страница manual описывает функции или операторы, она должна по возможности ссылаться на конкретные runnable samples;
   - примеры в prose и code blocks должны оставаться в IDE-friendly формате (`Print`, `If`, `Function`);
   - breadcrumbs и `См. также` считаются обязательной частью навигационной модели там, где страница участвует в справочном кластере.
3. В ходе этого прохода фактических конфликтов между `docs/SPECIFICATION.md`, кодом и `samples/` по строковым и числовым built-ins не обнаружено:
   - значит, следующий manual-pass можно направлять уже не на gap-fix по фактам, а на расширение той же навигационной и sample-driven модели на соседние страницы.
4. Следующий шаг:
   - продолжить skill-driven editorial pass на соседних справочных страницах, начиная с `docs/manual/variables-and-constants.html` и страниц по вводу/выводу;
   - отдельно дочистить оставшиеся элементы информационной архитектуры manual, включая русификацию и выравнивание catalog metadata в `docs/manual/scripts/manual.js`, если там ещё сохраняются смешанные или устаревшие описания.

## Update 2026-04-18 - Second skill-driven pass for declarations and I/O

1. Следующий редакторский проход через `axiom-doc-author` теперь закрыт и для соседнего reference-кластера:
   - обновлены `docs/manual/variables-and-constants.html`, `docs/manual/print.html`, `docs/manual/input.html`;
   - в этих главах внедрены breadcrumbs, блоки `См. также`, явные разделы ручной проверки и ссылки на runnable `samples/operators/...`;
   - prose и code-примеры выровнены под официальный русский стиль и IDE-friendly запись (`Dim`, `Print`, `Input`, `Const`).
2. Навигационная и catalog-модель manual стала согласованнее:
   - `docs/manual/scripts/manual.js` очищен от англоязычных и mixed-style описаний по declaration/I-O соседству и по ряду числовых функций;
   - алфавитный каталог теперь лучше совпадает по тону и формулировкам с самими manual-страницами.
3. По `DIM`, `CONST`, `PRINT` и `INPUT` снова не найдено расхождений между `docs/SPECIFICATION.md`, sample-программами и текущей реализацией:
   - значит, следующий шаг можно направлять уже не на этот reference-кластер, а на обзорные страницы, которые связывают главы manual между собой.
4. Следующий шаг:
   - выполнить skill-driven pass на обзорных и связующих manual-страницах, начиная с `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/expressions-and-branching.html`;
   - проверить, не требуют ли эти страницы отдельного уплотнения навигации, чтобы они не дублировали справочные главы, а связывали их через корректные переходы и `See Also`.

## Update 2026-04-18 - Overview hub pages aligned with manual navigation model

1. Обзорные manual-страницы теперь переведены в явный navigation-hub формат:
   - обновлены `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html`, `docs/manual/expressions-and-branching.html`;
   - каждая из страниц теперь объясняет, когда её открывать, к каким справочным главам переходить дальше, и даёт короткий набор representative samples для IDE smoke-check;
   - страницы больше не выглядят как случайные полу-дубли справочника, а выполняют связующую роль между детальными manual-главами.
2. Глобальная discoverability manual улучшена:
   - `docs/manual/scripts/manual.js` теперь включает отдельный блок навигации для обзорных маршрутов;
   - благодаря этому обзорные страницы появились в левом меню и вошли в pager-order, а не остались полускрытыми файлами вне общей навигационной модели.
3. В этом проходе подтверждено, что representative sample-links на обзорных страницах резолвятся в существующие `samples/` без дополнительных gap-фиксов:
   - значит, следующий шаг можно направлять уже на оставшиеся обзорные и entry-point страницы manual, а не на исправление link rot внутри текущего кластера.
4. Следующий шаг:
   - проверить `docs/manual/index.html`, `docs/manual/getting-started.html` и при необходимости `docs/manual/cheat-sheet.html` на тот же editorial baseline;
   - отдельно оценить, не нужен ли follow-up по унификации breadcrumbs или `See Also` на уже существующих старых страницах вроде `if.html`, `select-case.html`, `arrays.html`, `arithmetic-and-logic.html`.

## Update 2026-04-18 - Entry-point pages aligned with the same editorial baseline

1. Верхний слой manual теперь доведён до того же skill-driven стандарта, что и справочные и обзорные страницы:
   - обновлены `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/cheat-sheet.html` и `docs/manual/examples.html`;
   - `index.html` теперь явнее разводит учебный маршрут, обзорные хабы и быстрые runnable-примеры из `samples/`;
   - `getting-started.html` и `examples.html` получили breadcrumbs, блоки с запускаемыми примерами и более ровную официальную русскую подачу;
   - `cheat-sheet.html` приведена к IDE-friendly записи примеров и встроена в ту же навигационную модель через breadcrumbs и `См. также`.
2. Для entry-point страниц закреплён тот же локальный baseline:
   - вводные главы должны не дублировать справочник, а помогать выбрать маршрут чтения и дать короткий путь к runnable sample-файлам;
   - встроенные code-примеры на верхнем уровне manual должны оставаться в IDE-friendly стиле (`Dim`, `Print`, `Input`, `Function`, `If`);
   - UI-метки IDE вроде `Run` и `Output` допускаются только как точные названия элементов интерфейса, а не как редакционный язык страницы.
3. В этом проходе не обнаружено новых documentation gaps между `docs/SPECIFICATION.md`, текущим manual и доступными sample-файлами:
   - все добавленные sample-links на entry-point страницах резолвятся в существующие `.axm`-файлы;
   - значит, следующий шаг можно направлять не на link-fix, а на добор старых reference-страниц до той же навигационной модели.
4. Следующий шаг:
   - выполнить follow-up по старым справочным страницам, начиная с `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html` и `docs/manual/arithmetic-and-logic.html`;
   - проверить, где на этих страницах ещё отсутствуют breadcrumbs, `См. также`, ссылки на runnable `samples/` и выровненная русская справочная подача.

## Update 2026-04-18 - Older reference pages brought to the same navigation model

1. Старые справочные страницы по ветвлению, массивам и выражениям теперь доведены до того же reference-baseline:
   - обновлены `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html` и `docs/manual/arithmetic-and-logic.html`;
   - на всех четырёх страницах добавлены breadcrumbs, блоки `См. также` и наборы runnable sample-links для ручной проверки через IDE;
   - code-примеры приведены к IDE-friendly записи `If`, `Select Case`, `Dim`, `Print`, `End If`, `End Select`.
2. В ходе этого прохода зафиксирован и устранён старый documentation gap:
   - `docs/manual/arithmetic-and-logic.html` раньше утверждала, что оператор `/` всегда возвращает `DOUBLE`;
   - после сверки с `docs/SPECIFICATION.md` и тестовым контуром manual приведён к реальному контракту: `/` возвращает `DECIMAL` при участии `DECIMAL` без `DOUBLE`, иначе `DOUBLE`.
3. Для `IF` и `SELECT CASE` дополнительно закреплены подтверждённые ограничения:
   - `IF` документируется как блочная конструкция с логическим условием и блочной областью видимости;
   - `SELECT CASE` теперь явнее описывает списки значений, диапазоны, `CASE IS` и ограничения для `BOOLEAN`.
4. Следующий шаг:
   - продолжить follow-up на оставшихся старых справочных страницах циклов и подпрограмм, начиная с `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`, `docs/manual/sub.html`, `docs/manual/function.html` и `docs/manual/return.html`;
   - отдельно проверить, не требуют ли эти страницы такого же явного слоя sample-links и диагностических секций.

## Update 2026-04-18 - Loop and routine reference cluster aligned with current language contract

1. Следующий skill-driven reference-pass теперь закрыт и для кластера циклов и подпрограмм:
   - обновлены `docs/manual/for.html`, `docs/manual/while.html`, `docs/manual/do-loop.html`, `docs/manual/sub.html`, `docs/manual/function.html` и `docs/manual/return.html`;
   - эти страницы переведены в единый reference-формат с breadcrumbs, явными разделами `Сводка`, `Синтаксис`, `Замечания`, `Примеры`, `Диагностика`, `См. также` и runnable sample-links;
   - `docs/manual/scripts/manual.js` синхронизирован с новыми справочными формулировками по этому кластеру.
2. В ходе прохода зафиксирован и устранен реальный documentation gap:
   - старая страница `docs/manual/for.html` утверждала, что управляющая переменная `FOR` может быть только `INTEGER` или `DOUBLE`;
   - после сверки с binder/runtime и tests manual приведен к фактическому контракту: `FOR` поддерживает `INTEGER`, `LONG` и `DOUBLE`.
3. Для кластера `SUB` / `FUNCTION` / `RETURN` дополнительно закреплены подтвержденные ограничения:
   - параметры по-прежнему документируются как pass-by-value;
   - `RETURN` без выражения в `FUNCTION` документируется как `MB2007`, а завершение функции без возвращаемого значения по выполненной ветви как runtime `MB3006`;
   - `SUB` не документируется как источник возвращаемого значения и допускает только `RETURN` без выражения.
4. Следующий шаг:
   - выполнить такой же унифицирующий follow-up для оставшихся старых reference-страниц верхнего уровня, в первую очередь `docs/manual/data-types.html` и `docs/manual/diagnostics.html`;
   - затем проверить, не требуют ли `reference.html` и `manual.js` дополнительного выравнивания описаний под новый reference-контракт без возврата к mixed-style prose.

## Update 2026-04-18 - Classic BASIC casing restored as the manual writing baseline

1. Documentation baseline для встроенных code-примеров и синтаксических блоков меняется без немедленного retro-sweep по уже переписанным страницам:
   - в локальном skill `axiom-doc-author` зафиксирован стиль классического BASIC для manual-примеров и syntax-snippets;
   - новым нормальным видом для manual считаются uppercase-ключевые слова, типы, логические литералы и built-in вызовы вроде `DIM`, `PRINT`, `IF`, `END IF`, `FUNCTION`, `FOR`, `NEXT`, `LEN`, `TRUE`;
   - Modern/title-case запись вроде `Print`, `If`, `Function` больше не считается целевым documentation baseline.
2. Rollout выполняется в два шага, а не через немедленный массовый откат:
   - начиная с текущего прохода, все новые или заново переписываемые manual-страницы оформляются уже в classic-uppercase стиле;
   - ранее переписанные страницы с title-case примерами не трогаются точечно в каждом новом pass, а выносятся в отдельный финальный retro-sweep после завершения основного цикла выравнивания manual.
3. Следующий reference-pass продолжает тот же маршрут, но уже под новым casing-baseline:
   - `docs/manual/data-types.html` переводится в единый reference-формат с breadcrumbs, `См. также`, sample-driven ручной проверкой и uppercase-примерами;
   - после него следующим целевым follow-up остаётся `docs/manual/diagnostics.html`, а затем отдельный завершающий sweep по старым title-case reference- и entry-point страницам.

## Update 2026-04-18 - Manual wording shifts to plain Russian without needless anglicisms

1. Для новых редакторских проходов закрепляется дополнительное языковое правило:
   - вместо слов вроде `baseline`, `follow-up`, `workflow`, `built-in`, `runtime`, `runnable`, `sample-driven` и похожих выражений нужно по возможности использовать естественные русские соответствия;
   - подача должна звучать не как внутренняя заметка разработчиков, а как нормальное преподавательское объяснение для читателя manual.
2. Это правило применяется сразу в двух слоях:
   - во внутреннем skill `axiom-doc-author` и его шаблонах, чтобы англицизмы не возвращались через рабочие инструкции;
   - в новых manual-страницах, начиная с `docs/manual/diagnostics.html`, где справочная подача должна оставаться официальной, но понятной студенту.
3. Следующий шаг после этой точки:
   - продолжить тот же проход по `docs/manual/diagnostics.html` и соседним страницам уже в русском редакционном стандарте;
   - в финальном отдельном проходе не только вернуть uppercase-ключевые слова на старых страницах, но и дочистить там накопившиеся англицизмы.

## Update 2026-04-18 - Page titles should not repeat the language name without need

1. В справочных заголовках manual закрепляется ещё одно правило краткости:
   - если страница уже находится внутри `Axiom Manual`, не нужно повторять название языка в заголовке и подзаголовке без особой причины;
   - предпочтительная форма: `Оператор PRINT`, `Оператор INPUT`, `Типы данных`, `Диагностика`, а не `... в Axiom` или `... Axiom`.
2. Это правило применяется сразу к ближайшему уже обновленному кластеру:
   - точечно исправляются заголовки и связанные подписи на `print.html`, `input.html`, `data-types.html`, `diagnostics.html`, `variables-and-constants.html`, `arithmetic-and-logic.html`;
   - более широкий проход по остальным страницам с таким же лишним повтором названия языка остается частью будущей общей чистки manual.

## Update 2026-04-18 - Entry pages continue moving to concise Russian wording and uppercase BASIC examples

1. Следующий manual-проход идет по соседним обзорным и сводным страницам:
   - `docs/manual/getting-started.html`, `docs/manual/loops.html`, `docs/manual/string-functions.html` и `docs/manual/cheat-sheet.html` приводятся к тому же редакционному стандарту, что и новый reference-кластер;
   - на этих страницах убираются лишние повторы `Axiom` в `h1` и служебных подписях;
   - встроенные syntax-snippets и короткие примеры на этих страницах переводятся в классическую запись заглавными буквами.
2. В редакционной подаче дополнительно уточняется новая норма:
   - в обзорных главах не нужно держать внутреннюю лексику вроде `manual`, `baseline` или `1-based`, если мысль можно сказать проще и по-русски;
   - подача должна оставаться спокойной, справочной и понятной читателю, а не звучать как внутренняя пометка к кодовой базе.
3. Следующий шаг:
   - продолжить тот же проход на соседних обзорных главах `data-and-input.html`, `arrays-and-builtins.html` и `examples.html`;
   - после завершения основного manual-цикла выполнить отдельный финальный массовый проход по ранее переписанным title-case страницам.

## Update 2026-04-18 - Remaining overview cluster aligned with plain Russian wording

1. Обзорные и практические страницы по соседнему кластеру доводятся до того же спокойного справочного стандарта:
   - обновляются `docs/manual/data-and-input.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/examples.html`;
   - на этих страницах уплотняется язык, убираются остатки title-case кода и ненужных англицизмов, а подача становится ближе к нормальному учебному объяснению;
   - `docs/manual/scripts/manual.js` синхронизируется с этими изменениями там, где в указателе и навигации еще оставались выражения вроде `Legacy floor` и `1-based`.
2. Для страницы `examples.html` дополнительно действует правило фактической точности:
   - примеры должны опираться на реальный контракт языка, а не на привычки из других BASIC-диалектов;
   - если в учебном примере используется счетчик, он должен быть явно объявлен, поскольку неявное объявление переменных в Axiom не поддерживается.
3. Следующий шаг:
   - довести оставшиеся обзорные и связующие страницы, прежде всего `docs/manual/procedures-and-functions.html` и при необходимости `docs/manual/index.html`;
   - после этого перейти к отдельному финальному массовому проходу по ранее переписанным title-case страницам.

## Update 2026-04-18 - Procedures and functions overview integrated into the manual route

1. Следующий связующий проход по manual доводит до общего редакционного стандарта оставшийся обзорный узел по подпрограммам и функциям:
   - переписывается `docs/manual/procedures-and-functions.html` как короткая обзорная глава с breadcrumbs, понятным маршрутом чтения, примером в стиле классического BASIC и ссылками на runnable `.axm`-файлы;
   - `docs/manual/index.html` синхронизируется с этой структурой, чтобы обзорная глава по `SUB` / `FUNCTION` / `RETURN` входила в общий маршрут чтения, а на главной оставалось меньше лишних повторов и англицизмов;
   - `docs/manual/scripts/manual.js` дополняется пунктом навигации для `procedures-and-functions.html`, чтобы страница не оставалась вне левого меню и постраничных переходов.
2. Этот проход закрепляет два практических правила для оставшейся ручной чистки:
   - обзорная страница должна не дублировать справку, а связывать соседние атомарные статьи и вести к ним читателя коротким и понятным русским текстом;
   - если новый обзорный узел появляется в manual, он должен сразу входить в навигационную модель, а не существовать отдельно от меню и pager.
3. Следующий шаг:
   - перейти к финальному массовому проходу по ранее переписанным страницам, где ещё остались title-case примеры и накопившиеся англицизмы;
   - начать этот завершающий проход с уже обновлённых справочных страниц по `SUB`, `FUNCTION`, `RETURN`, `PRINT`, `INPUT`, циклам и встроенным функциям, чтобы привести весь manual к одной записи ключевых слов и одному русскому тону.

## Update 2026-04-18 - First final cleanup pass returned uppercase BASIC to the procedures, I/O, and loops cluster

1. Первый возвратный проход по ранее переписанным справочным страницам закрепляет единый вид примеров и формулировок:
   - в `docs/manual/sub.html`, `docs/manual/function.html`, `docs/manual/return.html`, `docs/manual/print.html`, `docs/manual/input.html`, `docs/manual/for.html`, `docs/manual/while.html` и `docs/manual/do-loop.html` синтаксис и примеры возвращаются к классической BASIC-записи заглавными буквами;
   - на тех же страницах убираются остаточные англоязычные слова и служебные выражения вроде `baseline`, `runtime`, `runnable`, `smoke-check`, `prompt-` и других подобных формулировок там, где они ещё попадали в видимый текст.
2. Этот проход закрепляет редакционное правило для завершающей чистки всего manual:
   - даже если страница уже была однажды переписана в справочном стиле, она не считается законченной, пока синтаксис, примеры и короткие пояснения не приведены к одной классической BASIC-записи и спокойной русской подаче без лишних англицизмов;
   - имена sample-файлов и технические пути остаются как есть, но видимый учебный текст и код внутри manual должны быть русскими по тону и единообразными по записи ключевых слов.
3. Следующий шаг:
   - продолжить тот же завершающий проход на страницах `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html`, `docs/manual/arithmetic-and-logic.html`, `docs/manual/variables-and-constants.html`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html`, `docs/manual/string-functions.html` и `docs/manual/numeric-functions.html`;
   - после этого сделать короткую общую проверку по `docs/manual/`, чтобы убедиться, что видимых title-case примеров и случайных англицизмов в основном корпусе manual больше не осталось.

## Update 2026-04-18 - Final cleanup reached the branching, declarations, types, and diagnostics cluster

1. Следующий возвратный проход доводит до того же редакционного стандарта следующий крупный блок manual:
   - в `docs/manual/if.html`, `docs/manual/select-case.html`, `docs/manual/arrays.html`, `docs/manual/arithmetic-and-logic.html`, `docs/manual/variables-and-constants.html`, `docs/manual/data-types.html`, `docs/manual/diagnostics.html` и `docs/manual/numeric-functions.html` примеры и синтаксис выравниваются под классическую BASIC-запись заглавными буквами;
   - в этом же проходе убираются остаточные англоязычные слова и неровные формулировки вроде `special-case`, `floor` в пояснительном тексте и формы `не-конечный`.
2. Общая вычитка после этого прохода показывает новое состояние manual:
   - основные справочные и обзорные страницы уже почти целиком очищены от видимых title-case примеров и лишних англицизмов;
   - на следующем шаге уже не нужен ещё один большой тематический кластер: требуется короткая итоговая проверка всего `docs/manual/` и при необходимости точечная синхронизация `docs/manual/scripts/manual.js` и отдельных обзорных страниц.
3. Следующий шаг:
   - выполнить финальную сквозную вычитку manual, включая `index.html`, `getting-started.html`, `examples.html`, `cheat-sheet.html`, `string-functions.html` и навигационные подписи в `docs/manual/scripts/manual.js`;
   - после этой вычитки зафиксировать, что основной корпус manual приведён к одной классической BASIC-записи и одному спокойному русскому тону.

## Update 2026-04-18 - Final editorial pass aligned the manual with the established Microsoft-style reference tone

1. Финальный редакторский проход завершает вычитку оставшихся обзорных и вводных страниц manual:
   - `docs/manual/index.html`, `docs/manual/getting-started.html`, `docs/manual/examples.html`, `docs/manual/cheat-sheet.html`, `docs/manual/string-functions.html` и обзорные страницы разделов выровнены по той же плотной и официальной подаче, что и справочные статьи;
   - `docs/manual/scripts/manual.js` синхронизирован с этим тоном в навигационных названиях и кратких описаниях карточек каталога.
2. Для этого прохода дополнительно использован внешний стилистический ориентир:
   - повторно просмотрены локальные PDF `Basic Language Reference (1989).pdf`, `Programmers Guide (1989).pdf` и `Getting Started 7.1 (1990).pdf`;
   - по ним закреплён целевой ритм: краткое назначение, затем синтаксис или маршрут чтения, затем замечания, примеры и переходы к соседним темам без разговорной интонации и без лишних повторов.
3. Следующий шаг:
   - считать основной корпус `docs/manual/` приведённым к единому редакционному стандарту и дальше поддерживать этот стандарт точечно при новых изменениях языка;
   - при следующем содержательном изменении языка или manual обновлять только затронутые статьи, навигацию и связанные записи в `docs/WORKLOG2.md`, не открывая новый общий стиль-проход без необходимости.

## Update 2026-04-18 - Manual sidebar gets its own vertical scroll on desktop

1. После завершения общего редакторского прохода выявлен отдельный дефект компоновки manual:
   - при длинном меню слева прокрутка навигации фактически зависела от прокрутки всей страницы;
   - это мешало использовать боковое меню как постоянный справочный навигатор на `docs/manual/index.html` и соседних страницах.
2. Для исправления достаточно точечной правки оболочки manual:
   - в `docs/manual/styles/manual.css` боковой контейнер получает ограничение по высоте окна и внутреннюю вертикальную прокрутку именно для списка тем;
   - заголовок боковой панели остаётся закреплённым внутри карточки, а основное содержимое страницы прокручивается независимо.
3. Следующий шаг:
   - поддерживать ту же схему прокрутки боковой панели на следующих точечных изменениях manual-layout;
   - при будущих правках вёрстки manual проверять отдельно desktop- и mobile-поведение боковой навигации.

## Update 2026-04-18 - Main manual page uses clearer Russian section names

1. На главной странице manual уточняется терминология разделов:
   - заголовок `Обзорные маршруты` заменяется на более естественное `Обзорные главы`;
   - заголовок `Маршрут курса` заменяется на `Последовательность изучения`.
2. Та же правка синхронизируется с оболочкой manual:
   - в `docs/manual/index.html` убираются ближайшие формулировки с лишним словом `маршрут`, если они звучат искусственно;
   - в `docs/manual/scripts/manual.js` обновляется название блока боковой навигации.
3. Следующий шаг:
   - при дальнейшей редактуре manual предпочитать простые и учебно-понятные русские названия разделов без искусственных метафор;
   - новые заголовки и подписи проверять по той же логике на главной странице, в боковом меню и в связанных обзорных главах.

## Update 2026-04-18 - PRINT USING implementation

### Что сделано
- язык расширен оператором `PRINT USING` с отдельной syntax/binding/runtime-цепочкой;
- добавлены unit-тесты для parser, binder и runtime;
- добавлен runnable acceptance sample `samples/operators/print/using_basic.axm`;
- спецификация синхронизирована с новым оператором и ключевым словом `USING`.

### Что ещё осталось
- при следующем содержательном проходе по manual добавить или обновить пользовательскую страницу справки по `PRINT USING`, если оператор войдёт в публичную IDE-справку текущего milestone;
- при расширении формата отдельно покрыть acceptance-сценариями более сложные маски `*`, `$`, `^` и ошибочные случаи нехватки аргументов.

### Следующий точечный шаг
- после завершения текущей задачи держать `PRINT USING` как baseline и при следующих изменениях `PRINT`/`INPUT` обновлять связанные samples, тесты и справку вместе, а не раздельно.

## Update 2026-04-18 - Manual chapter for PRINT USING

### Что сделано
- страница `docs/manual/print.html` расширена до полноценной справки по `PRINT USING`;
- в manual зафиксированы все реально поддерживаемые строковые и числовые маски текущей runtime-реализации;
- алфавитный указатель manual теперь содержит отдельный элемент `PRINT USING`.

### Что ещё осталось
- при следующем развитии форматтера синхронно обновлять manual, особенно если появится отдельная логика для `0`, локализованной валюты или новых масок;
- при желании можно позже вынести `PRINT USING` в отдельную короткую обзорную страницу, но пока достаточно общей главы `PRINT`.

### Следующий точечный шаг
- поддерживать `docs/manual/print.html` как каноническую пользовательскую справку по обычному и форматированному выводу, не оставляя детали масок только в `docs/SPECIFICATION.md`.

## Update 2026-04-18 - RND added as the first random math function

### Что сделано
- математический набор расширен встроенной функцией `RND()` без новых синтаксических правил;
- `RND()` возвращает `DOUBLE` в диапазоне `[0, 1)` и встроен в уже существующую табличную модель встроенных функций без ослабления границ между слоями;
- добавлены автоматические тесты, запускаемый пример `samples/functions/rnd/basic_usage.axm` и обновлён консольный проверочный прогон математических функций.

### Что ещё осталось
- если позже потребуется QuickBASIC-подобная расширенная модель случайных чисел, сначала нужно отдельно зафиксировать её контракт в спецификации, а уже потом обсуждать формы вроде инициализации генератора и повторного возврата последнего значения;
- при следующем расширении математического набора не смешивать обычные числовые функции и состояние генератора в разрозненных ветках: держать всё через ту же таблицу `BuiltinFunctionId` и тесты.

### Следующий точечный шаг
- держать `RND()` как минимальный вариант случайной функции и при следующем содержательном расширении math-пакета оценить, нужен ли отдельный документированный сценарий инициализации генератора или текущего варианта без аргументов достаточно.

## Update 2026-04-18 - RND synchronized with the manual index

### Что сделано
- `RND()` дополнительно доведён до слоя указателя manual, а не только до общей статьи `docs/manual/numeric-functions.html`;
- `docs/manual/scripts/manual.js` теперь содержит отдельную карточку `RND` с переходом на `numeric-functions.html#rnd`.

### Что ещё осталось
- при следующих изменениях встроенных функций проверять manual не только на наличие текста в профильной статье, но и на наличие функции в алфавитном указателе, если она должна находиться как отдельный элемент справки.

### Следующий точечный шаг
- для новых встроенных функций дальше синхронно обновлять три слоя manual: профильную статью, ссылки на примеры внутри неё и запись в `docs/manual/scripts/manual.js`.

## Update 2026-04-18 - Manual wording for RND normalized to Russian

### Что сделано
- в пользовательском тексте по `RND()` убрана формулировка с `baseline`;
- в свежих плановых записях по этой функции заменены служебные англоязычные слова на нормальные русские выражения.

### Что ещё осталось
- исторические записи в `docs/PLANS.md` и `docs/WORKLOG.md`, появившиеся до этого замечания, можно отдельно дочистить позднее, если нужен целевой проход именно по внутренней терминологии проектных журналов.

### Следующий точечный шаг
- в новых пользовательских текстах и свежих project-log записях сразу выбирать русскую формулировку, если смысл можно передать без англицизма.

## Update 2026-04-18 - Documentation workflow rules tightened for new functions and operators

### Что сделано
- repo-local skill `axiom-doc-author` дополнен прямым правилом: после новой функции, оператора или изменения их публичного поведения manual должен обновляться в том же наборе правок;
- в skill отдельно усилено правило про отказ от англицизмов не только для `docs/manual/`, но и для новых поясняющих записей в `docs/PLANS.md` и `docs/WORKLOG2.md`;
- те же требования отдельно закреплены в `AGENTS.md` и `README.md`, чтобы правило не зависело только от skill.

### Что ещё осталось
- дальше нужно просто соблюдать это правило без дополнительных напоминаний: новая функция или оператор не считаются закрытой задачей, если пользовательская справка и указатель manual отстают.

### Следующий точечный шаг
- при следующем языковом изменении проверять четыре вещи как обязательный минимум: код, тесты, `.axm`-пример и `docs/manual/` вместе с `docs/manual/scripts/manual.js`, если сущность должна находиться через указатель.
## Update 2026-04-18 - Procedure model review against Axiom

### Что сделано
- Изучен файл `C:\projects\Axiom\Visitors\AxiomInterpreter.Procedures.cs` и сопоставлен с текущими слоями `Axiom.Syntax`, `Axiom.Semantics` и `Axiom.Runtime`.
- Зафиксированы сильные идеи из Axiom: лексический реестр вложенных `SUB`/`FUNCTION`, двухпроходная регистрация локальных процедур перед выполнением тела и более богатый контекст вызова для диагностики.

### Что решено
- Переносить реализацию Axiom напрямую в Axiom не нужно: монолитный visitor-файл, строковые имена типов и исключения как штатный механизм `Exit Function`/`Exit Sub` хуже сочетаются с нашей текущей архитектурой bound tree и runtime signals.
- Наиболее ценный переносимый опыт — не код как таковой, а модель будущего расширения процедур: отдельная метамодель объявлений процедур и функций, лексическая область видимости их имён и расширенный контекст вызова для диагностик.
- Захват внешних локальных переменных вложенными процедурами пока нельзя считать решённым по умолчанию: это отдельное изменение смысловой проверки и выполнения, и тогда оно ещё расходилось с зафиксированной моделью вызова поверх глобальной области видимости.

### Что ещё осталось
- Если будет отдельная задача на развитие процедур, сначала нужно явно зафиксировать контракт локальных `SUB`/`FUNCTION` и policy по visibility/capture, а уже потом менять parser, binder и runtime.

### Следующий точечный шаг
- При следующей языковой задаче, связанной с процедурами, начинать с короткой проектной заметки в `docs/SPECIFICATION.md` и `docs/ARCHITECTURE.md` про символы процедур и функций, локальные объявления и расширенный контекст вызова для диагностик, не переходя сразу к реализации замыканий.

## Update 2026-04-18 - Local nested SUB/FUNCTION implemented

### Что сделано
- в язык добавлены локальные `SUB` и `FUNCTION` внутри другой процедуры или функции;
- локальные объявления процедур и функций теперь регистрируются первым проходом по телу внешней процедуры или функции, поэтому их можно вызывать до текстового объявления;
- вложенные процедуры и функции видят лексически внешние имена процедур и функций, но не получают доступ к локальным переменным и параметрам внешней процедуры или функции;
- обновлены parser, binder, acceptance samples и пользовательское руководство.

### Что ещё осталось
- захват внешних локальных переменных для вложенных процедур и функций пока не поддерживается;
- richer call-frame и call-site diagnostics для цепочки вложенных вызовов пока не внедрены;
- отдельная языковая спецификация для полноценного поведения вложенных процедур и функций с захватом окружения ещё не оформлена.

### Следующий точечный шаг
- отдельным изменением спроектировать и реализовать захват внешних локальных переменных для вложенных `SUB`/`FUNCTION`, не смешивая эту работу с уже завершённым базовым слоем локальных объявлений процедур и функций.

## Обновление 2026-04-18 - Лексический захват для вложенных SUB и FUNCTION

### Что сделано
- вложенные `SUB` и `FUNCTION` теперь захватывают параметры и локальные переменные внешней процедуры или функции по лексической цепочке;
- связыватель переведён на связывание тела процедуры или функции в текстовом порядке после предварительной регистрации локальных имён процедур и функций, чтобы захват зависел от реального места объявления;
- вызов локальной процедуры или функции во время выполнения теперь стартует от области видимости активного внешнего вызова объявляющей процедуры или функции, а не только от глобальной области видимости;
- добавлены новые тесты смысловой проверки, времени выполнения и сквозного выполнения, а также запускаемые `.axm`-примеры для захвата внешних локальных данных;
- обновлены `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и справочные страницы по `SUB`/`FUNCTION`.

### Что ещё осталось
- локальные процедуры и функции всё ещё не являются значениями первого класса и не поддерживают полноценные замыкания как объекты языка;
- внешние локальные имена, объявленные ниже по тексту после локальной процедуры или функции, по-прежнему недоступны и это ограничение пока намеренное;
- более подробная диагностика кадров вызова и мест вызова для цепочки вложенных вызовов ещё не внедрена.

### Следующий точечный шаг
- следующим отдельным этапом улучшить диагностику вложенных вызовов: показывать объявляющую процедуру или функцию, цепочку внешних вызовов и место разрешения локальной процедуры или функции в сообщениях времени выполнения.

## Обновление 2026-04-18 - Диагностика времени выполнения для вложенных вызовов процедур и функций

### Что сделано
- в диагностику времени выполнения добавлена цепочка активных вызовов для ошибок внутри вложенных процедур и функций;
- для локальной процедуры или функции сообщение теперь показывает внешнюю процедуру или функцию, внутри которой она объявлена;
- кадр вызова хранит место вызова и передаёт его в текст цепочки;
- добавлены тесты времени выполнения и сквозного выполнения, а также запускаемый пример `samples/smoke/nested_runtime_diagnostic.axm`;
- обновлены спецификация, архитектура и справочная глава по диагностике.

### Что ещё осталось
- локальные процедуры и функции по-прежнему не являются значениями языка первого класса;
- полноценная модель замыканий как значений языка всё ещё не спроектирована;
- цепочка вызовов пока существует как текстовое расширение сообщения, а не как отдельная структурированная модель диагностики.

### Следующий точечный шаг
- если продолжать развитие процедур и функций, отдельно зафиксировать в спецификации, нужны ли процедуры и функции как значения языка, и какой должна быть модель полноценных замыканий.

## Обновление 2026-04-18 - Функции как значения и полноценные замыкания

### Что сделано
- в язык добавлены функциональные типы вида `FUNCTION(...) AS ...` для переменных, параметров и типов результата функций;
- обычные и локальные функции стали значениями языка: их можно присваивать, передавать и возвращать;
- вызов переведён на модель вызова целевого выражения, поэтому работают сценарии вроде вызова функции из переменной и немедленного вызова возвращаемой функции;
- возвращаемые локальные функции сохраняют захваченную внешнюю область видимости и продолжают работать после завершения внешней функции;
- добавлены новые автоматические проверки и запускаемые примеры в `samples/operators/function/`.

### Что ещё осталось
- в языке пока нет анонимных функций и отдельного синтаксиса литералов функции;
- подпрограммы `SUB` по-прежнему не являются значениями;
- нет отдельной модели сравнения значений функции и специальных пользовательских операций над ними.

### Следующий точечный шаг
- если продолжать развитие этой линии, отдельным этапом спроектировать и внедрить анонимные функции, не смешивая эту работу с уже завершённой поддержкой именованных функций как значений и замыканий.

## Обновление 2026-04-18 - Документация и полная проверка после диагностики вложенных вызовов

### Что сделано
- свежие разделы `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md` и `docs/WORKLOG.md`, связанные с вложенными процедурами, функциями и диагностикой времени выполнения, дополнительно вычищены от смешанных английских формулировок;
- выполнен полный прогон `dotnet test C:\Axiom\Axiom.sln --no-restore` после завершения правок.

### Что ещё осталось
- процедуры и функции по-прежнему не являются значениями языка первого класса;
- полноценная модель замыканий как значений языка по-прежнему не спроектирована;
- цепочка вызовов пока остаётся текстовой частью сообщения, а не отдельной структурированной моделью диагностики.

### Следующий точечный шаг
- если продолжать развитие процедур и функций, отдельно зафиксировать в спецификации, нужны ли процедуры и функции как значения языка, и какой должна быть модель полноценных замыканий.

## Обновление 2026-04-19 - Полный manual-pass под Microsoft-подобный справочный стандарт

### Что сделано
- уточнён scope локального навыка `axiom-doc-author`: теперь он должен выравнивать не отдельные страницы, а весь `docs/manual/` под единый справочный стандарт в духе Microsoft QuickBasic;
- в навык добавлены прямые правила для раздела `Синтаксис`: отделять формальную запись от запускаемого примера, явно помечать метапеременные, не использовать двусмысленные шаблоны вроде `TYPE`, `NAME`, `STATEMENTS`;
- дополнительно зафиксировано правило нотации необязательных частей синтаксиса: в формальной записи они должны показываться в квадратных скобках;
- reference-шаблон и вводный шаблон обновлены так, чтобы любая страница manual начиналась с компактного справочного блока и сохраняла единый порядок подачи.

### Что ещё осталось
- выполнить отдельный полный проход по существующим страницам `docs/manual/` и привести операторы, встроенные функции, обзорные главы и указатель к новому стандарту;
- на этом проходе отдельно вычистить все синтаксические блоки, где условные обозначения сейчас могут быть приняты за реальные ключевые слова языка.
- на том же проходе проверить, что необязательные части во всех операторах и формах вызова действительно отражены в квадратных скобках, где это уместно.

### Следующий точечный шаг
- следующим отдельным этапом пройти весь `docs/manual/` по страницам операторов и функций и привести их к единому Microsoft-подобному справочному жанру без переноса неподтверждённых фактов из исторической документации.

## Обновление 2026-04-19 - Синтаксический manual-pass по ключевым страницам справки

### Что сделано
- приведены к согласованной нотации и фактическому синтаксису Axiom страницы `FUNCTION`, `SUB`, `FOR`, `IF`, `DO ... LOOP`, `WHILE`, `RETURN`, `Типы данных`, `Переменные и константы`, `Шпаргалка`, `Одномерные массивы`;
- из справки убраны двусмысленные заглушки вида `TYPE`, `statements`, `condition`, `RETURN expression`, `name = expression`, где они могли быть восприняты как реальные слова синтаксиса языка;
- в ключевые справочные страницы добавлены или уточнены разделы `Части синтаксиса`, чтобы метапеременные и необязательные части не оставались без расшифровки.

### Что ещё осталось
- продолжить такой же редакционный проход по соседним страницам обзоров и более длинным справочным главам, где синтаксис уже фактически верен, но структура подачи ещё не везде доведена до полного Microsoft-подобного reference-формата;
- отдельным проходом проверить связность навигации между обновлёнными страницами и обзорными главами.

### Следующий точечный шаг
- следующим отдельным этапом пройти обзорные страницы `procedures-and-functions.html`, `expressions-and-branching.html`, `loops.html`, `arrays-and-builtins.html`, `reference.html` и выровнять их под тот же редакционный стандарт без повторного появления псевдосинтаксиса.

## Обновление 2026-04-19 - Обзорные страницы manual выровнены под новый стандарт

### Что сделано
- обзорные страницы `procedures-and-functions.html`, `expressions-and-branching.html`, `loops.html`, `arrays-and-builtins.html`, `reference.html` приведены к более предсказуемой структуре: добавлены `Содержание`, компактные `Сводка` и явные маршрутные разделы;
- обзорные главы теперь лучше согласованы с reference-страницами и не начинают тему с свободного разрозненного текста;
- указатель `reference.html` тоже получил явную внутреннюю структуру, чтобы его было легче читать как навигационную страницу, а не только как интерактивный каталог.

### Что ещё осталось
- довести до того же стандарта остальные обзорные и переходные manual-страницы, прежде всего `data-and-input.html` и `examples.html`;
- после этого можно сделать финальный редакционный проход по всему `docs/manual/` уже на уровне единообразия формулировок, а не синтаксических каркасов.

### Следующий точечный шаг
- следующим отдельным этапом пройти `data-and-input.html`, `examples.html` и при необходимости `index.html`, чтобы все обзорные страницы manual были оформлены по одному и тому же редакционному шаблону.

## Обновление 2026-04-19 - Завершение сквозного manual-pass по обзорным страницам

### Что сделано
- оставшиеся обзорные и переходные страницы `data-and-input.html`, `examples.html` и `index.html` приведены к тому же каркасу: `Содержание`, `Сводка`, явные маршрутные блоки и устойчивые якоря внутри страницы;
- внутренняя навигация manual теперь выровнена не только в атомарных справочных статьях, но и в домашней и обзорных главах.

### Что ещё осталось
- выполнить финальный сквозной просмотр `docs/manual/` уже не на уровне структуры, а на уровне единообразия формулировок, коротких вступлений и блоков `См. также`;
- при нужде дополнительно подчистить отдельные статьи по тону и длине абзацев, не меняя их фактического содержания.

### Следующий точечный шаг
- следующим отдельным этапом выполнить финальный редакционный просмотр всего `docs/manual/` и довести единообразие формулировок до конца.

## Обновление 2026-04-19 - Первый этап расширения строковых функций

### Что сделано
- в `docs/SPECIFICATION.md` зафиксирован первый пакет новых строковых функций: `TRIM`, `LTRIM`, `RTRIM`, `REVERSE`, `REPEAT`, `SPACE`, `STRING`;
- в `docs/SPECIFICATION.md` и `docs/ARCHITECTURE.md` отдельно закреплено, что встроенный каталог может содержать несколько форм вызова у одного имени, например `TRIM(text)` и `TRIM(text, chars)`, но пользовательские процедуры и функции при этом не получают перегрузку.

### Что ещё осталось
- перестроить каталог встроенных функций в `Axiom.Semantics` на тематические partial-файлы вместо одного общего списка;
- научить binder выбирать нужную форму встроенной функции по имени, числу аргументов и их типам;
- реализовать новые строковые built-ins в `Axiom.Runtime` с тем же разделением по тематическим partial-файлам;
- добавить автоматические тесты, runnable `.axm` примеры и обновить `docs/manual/`.

### Следующий точечный шаг
- следующим шагом внедрить новый каталог built-ins и после этого добавить в язык рабочие реализации `TRIM`, `LTRIM`, `RTRIM`, `REVERSE`, `REPEAT`, `SPACE` и `STRING`.

## Обновление 2026-04-19 - Первый этап строковых функций реализован

### Что сделано
- каталог встроенных функций в `Axiom.Semantics` разнесён по тематическим partial-файлам и больше не держит всю строковую и числовую логику в одном списке;
- binder научился выбирать нужную форму встроенной функции по имени, числу аргументов и их типам, что позволило поддержать пары вроде `TRIM(text)` и `TRIM(text, chars)`;
- реализованы новые строковые функции `TRIM`, `LTRIM`, `RTRIM`, `REVERSE`, `REPEAT`, `SPACE`, `STRING`;
- разбор выражений расширен так, чтобы вызов `STRING(...)` читался как допустимое выражение, несмотря на совпадение с именем встроенного типа;
- добавлены автоматические тесты, runnable `.axm` примеры и обновлена справка по строковым функциям.

### Что ещё осталось
- второй этап строковых функций: поиск, проверка вхождения, начала и конца строки, замена фрагментов;
- при необходимости позже можно расширить этот пакет функциями поиска с позицией и диапазоном, но без спешки и без разрастания одного файла built-ins.

### Следующий точечный шаг
- следующим отдельным этапом спроектировать и внедрить вторую волну строковых функций: `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `REPLACE` и, если модель будет согласована, семейство поиска позиции в строке.

## Обновление 2026-04-19 - Второй этап строковых функций реализован

### Что сделано
- реализованы строковые функции `CONTAINS`, `STARTSWITH`, `ENDSWITH` и `REPLACE`;
- функции поиска и проверки используют строгое сравнение по кодам символов, с учётом регистра;
- для `CONTAINS`, `STARTSWITH` и `ENDSWITH` пустой `fragment` считается найденным и возвращает `TRUE`;
- для `REPLACE` пустой `oldText` считается ошибкой времени выполнения `MB3007`, чтобы не вводить неочевидное поведение вставки между символами;
- добавлены автоматические проверки, запускаемые `.axm` примеры и разделы в справочнике.

### Что ещё осталось
- отдельно согласовать модель функций, которые возвращают позицию найденного фрагмента: нумерация с 1, поведение при отсутствии совпадения и нужность начальной позиции поиска;
- после согласования добавить такую семью функций отдельным этапом, не смешивая её с уже закрытыми проверками и заменой.

### Следующий точечный шаг
- спроектировать позиционный поиск по строке и выбрать точную форму вызова перед реализацией.

## Обновление 2026-04-19 - Позиционный поиск по строке реализован

### Что сделано
- реализована встроенная функция `INSTR` в двух формах: `INSTR(text, fragment)` и `INSTR(start, text, fragment)`;
- поиск использует нумерацию позиций с 1 и возвращает `0`, если совпадение не найдено;
- форма с `start` начинает поиск с указанной позиции и выдаёт диагностику времени выполнения `MB3007`, если `start < 1`;
- пустой фрагмент возвращает текущую позицию поиска, если она не больше `LEN(text) + 1`;
- добавлены автоматические проверки, запускаемые `.axm`-примеры и справочная запись в руководстве.

### Что ещё осталось
- при дальнейшем развитии строкового пакета отдельно решить, нужны ли функции обратного поиска и подсчёта вхождений;
- не расширять текущий шаг неограниченно: `INSTR` закрывает прямой позиционный поиск, а дополнительные варианты лучше добавлять отдельными малыми задачами.

### Следующий точечный шаг
- обсудить и при необходимости реализовать следующую пару строковых функций поиска: обратный поиск от конца строки и подсчёт непересекающихся вхождений.

## Обновление 2026-04-19 - Обратный поиск и подсчёт вхождений реализованы

### Что сделано
- реализованы строковые функции `INSTRREV(text, fragment)` и `SUBSTRCOUNT(text, fragment)`;
- `INSTRREV` возвращает позицию последнего найденного фрагмента с нумерацией от 1 или `0`, если совпадение не найдено;
- пустой фрагмент в `INSTRREV` возвращает позицию `LEN(text) + 1`;
- `SUBSTRCOUNT` считает непересекающиеся вхождения фрагмента слева направо;
- пустой фрагмент в `SUBSTRCOUNT` даёт диагностику времени выполнения `MB3007`;
- добавлены автоматические проверки, запускаемые `.axm`-примеры и справочные записи.

### Что ещё осталось
- не добавлена отдельная форма `INSTRREV(start, text, fragment)`: её лучше проектировать отдельно, если понадобится поиск назад от заданной позиции;
- дальнейшее расширение строкового набора нужно вести такими же малыми шагами, без разрастания одного общего файла встроенных функций.

### Следующий точечный шаг
- согласовать следующую малую группу строковых функций после закрытия базового поиска: либо поиск назад от заданной позиции, либо новая группа, если она нужнее для практических программ.

## Обновление 2026-04-19 - Начальная позиция для INSTRREV согласована

### Что сделано
- для `INSTRREV` добавлена форма `INSTRREV(start, text, fragment)`;
- первый аргумент означает начальную позицию, от которой поиск идёт назад;
- если начальная позиция больше длины строки, поиск мягко начинается с конца строки;
- если начальная позиция меньше 1, выполнение завершается диагностикой `MB3007`;
- поведение пустого фрагмента закреплено отдельно для формы с двумя аргументами и формы с начальной позицией.

### Что ещё осталось
- базовая семья позиционного поиска теперь закрыта: прямой поиск, обратный поиск и подсчёт непересекающихся вхождений реализованы;
- дальнейшее расширение строковых функций лучше продолжать отдельными малыми группами.

### Следующий точечный шаг
- выбрать следующую практическую группу строковых функций для реализации или перейти к другой области языка.

## Обновление 2026-04-19 - Редакционная зачистка документации от лишних англицизмов

### Что сделано
- в `README.md` заменены смешанные англоязычные описания слоёв, проверочных примеров, чистоты сборки и проверочной программы математических функций на русские формулировки;
- в `docs/SPECIFICATION.md` вычищены старые подписи вроде `Arity`, `Domain rules`, `Invalid input`, `runtime diagnostic`, а также переписан повреждённый кодировкой раздел про форматирование IDE;
- в HTML-справочнике видимый заголовок `Axiom Manual` заменён на `Справочник Axiom`;
- в странице строковых функций формальные записи приведены к русским метапеременным в угловых скобках, а пояснения больше не используют `count`, `oldText`, `fill` как обычные русские слова.

### Что ещё осталось
- при следующих редакционных проходах отдельно выровнять страницы числовых функций и внутренние архитектурные документы, где ещё встречаются допустимые технические имена и старые проектные формулировки;
- не менять исторические записи `docs/PLANS.md` и `docs/WORKLOG.md` задним числом, а добавлять новые записи уже в русском стиле.

### Следующий точечный шаг
- после этой зачистки можно либо продолжить второй редакционный проход по числовым функциям, либо перейти к следующей малой группе строковых функций.

## Обновление 2026-04-19 - Зачистка числовых функций и архитектурной документации

### Что сделано
- в `docs/manual/numeric-functions.html` формальные записи и краткие описания приведены к русским метапеременным: вместо `value`, `text`, `x`, `low`, `high`, `signFrom` используются русские обозначения аргументов;
- в `docs/ARCHITECTURE.md` убраны наиболее заметные англоязычные описательные хвосты в разделах про слои, файловую панель, форматирование IDE, трассировку диагностики и каталог встроенных функций;
- структура внутренних документов выровнена так, чтобы английскими оставались только реальные имена проектов, файлов, интерфейсов, меню и кодовых сущностей.

### Что ещё осталось
- в архитектурной документации ещё могут оставаться отдельные технические имена внутри старых исторических разделов, если они обозначают конкретные типы, имена файлов, образцы имён или принятые названия режимов;
- после редакционного прохода по документации следующую языковую задачу лучше брать уже как отдельный функциональный этап, не смешивая её с дальнейшей стилистической правкой.

### Следующий точечный шаг
- перейти к следующей малой группе строковых функций и спроектировать `PADLEFT`, `PADRIGHT`, `INSERT` и `REMOVE` с тем же набором изменений: код, тесты, `.axm`-примеры, спецификация и справочник.
## Обновление 2026-04-19 - Финальная зачистка архитектурной документации

### Что сделано
- в `docs/ARCHITECTURE.md` дочищены оставшиеся описательные англоязычные хвосты вроде `failure`, `line-formatting`, `sample`, `naming conventions`, `runtime source mapping`, `drag-drop`, `multi-select` и сходных формулировок;
- сохранён принцип: реальные имена каталогов, типов, вкладок, пунктов меню и классов остаются в исходном виде, а поясняющий текст приводится к русской подаче.

### Что ещё осталось
- редакционная зачистка по документации на этом проходе завершена; дальше лучше не смешивать стилистику и новую функциональность в одном шаге.

### Следующий точечный шаг
- перейти к следующей малой группе строковых функций и спроектировать `PADLEFT`, `PADRIGHT`, `INSERT` и `REMOVE` с кодом, тестами, `.axm`-примерами, спецификацией и справочником.

## Обновление 2026-04-19 - Реализация PADLEFT, PADRIGHT, INSERT и REMOVE

### Что сделано
- добавлены встроенные строковые функции `PADLEFT`, `PADRIGHT`, `INSERT` и `REMOVE` в семантический каталог и выполнение интерпретатора;
- для `PADLEFT` и `PADRIGHT` закреплены две формы вызова: без явного заполнителя и с необязательным заполнителем, где по умолчанию используется пробел, а из непустой строки заполнителя берётся только первый символ;
- для `INSERT` и `REMOVE` закреплена единичная индексация позиций, мягкое поведение на выход за правую границу строки и диагностические правила `MB3007` для недопустимых значений;
- добавлены автоматические тесты, ручные `.axm`-примеры, спецификация и страница справочника с индексом.

### Что ещё осталось
- следующая малая группа строковых функций ещё не начата;
- символьные функции `ASC` и `CHR` остаются ближайшим практическим кандидатом на следующий этап.

### Следующий точечный шаг
- спроектировать и внедрить `ASC` и `CHR` с тем же полным комплектом изменений: код, тесты, `.axm`-примеры, спецификация и справочник.

## Обновление 2026-04-19 - Реализация ASC и CHR

### Что сделано
- добавлены встроенные строковые функции `ASC` и `CHR` в семантический каталог и выполнение интерпретатора;
- поведение закреплено как работа с кодами символов Unicode, а не с историческими основными и расширенными однобайтовыми таблицами;
- добавлены автоматические тесты, ручные `.axm`-примеры, спецификация и справочная страница.

### Что ещё осталось
- следующая малая группа строковых функций ещё не выбрана окончательно;
- для функций, возвращающих массивы, потребуется отдельный этап проектирования, чтобы не смешивать простые строковые функции с расширением модели значений.

### Следующий точечный шаг
- реализовать `CENTER` как следующую самостоятельную строковую функцию форматирования, а после него отдельно спроектировать группу `SPLIT`/`JOIN`.

## Обновление 2026-04-19 - Реализация CENTER

### Что сделано
- добавлена встроенная строковая функция `CENTER` в двух формах: с шириной и с необязательным заполнителем;
- поведение закреплено рядом с `PADLEFT` и `PADRIGHT`, включая правило, что при нечётной разнице ширины лишний символ заполнения идёт справа;
- добавлены автоматические тесты, ручные `.axm`-примеры, спецификация и справочная страница.

### Что ещё осталось
- следующая малая группа строковых функций пока не реализована;
- функции, возвращающие массивы, по-прежнему требуют отдельного проектирования, чтобы не смешивать простые строковые операции с расширением модели значений.

### Следующий точечный шаг
- отдельно усилить модель массивов: типы, передачу по параметрам, возврат из функций и общий контракт встроенных операций над массивами;
- после этого вернуться к `SPLIT`/`JOIN` как к отдельному этапу и утвердить их окончательные сигнатуры и семантику.

## Обновление 2026-04-19 - Резервирование SPLIT и JOIN до этапа массивов

### Что сделано
- `SPLIT` и `JOIN` внесены в спецификацию и справочник как зарезервированные будущие функции;
- прямо зафиксировано, что в текущей версии языка они не реализованы и не входят в действующий публичный контракт;
- форма вызова осознанно не закреплялась заранее, чтобы не породить переделку после расширения модели массивов.

### Что ещё осталось
- языку нужен отдельный этап усиления массивов;
- после этого можно будет вернуться к реализации `SPLIT` и `JOIN` без временных компромиссов.

### Следующий точечный шаг
- перейти к отдельному планированию и развитию модели массивов, а функции `SPLIT` и `JOIN` держать в статусе зарезервированных до завершения этого этапа.
## Обновление 2026-04-19 - SELECT CASE приведён к справочному формату

### Что сделано
- страница `docs/manual/select-case.html` переписана в принятом справочном жанре: `Сводка`, `Синтаксис`, `Части синтаксиса`, `Замечания`, `Примеры`, `Диагностика`, `См. также`;
- на странице теперь явно отражены все подтверждённые формы селекторов: точное значение, список значений, диапазон `TO`, сравнение через `IS` и ветвь `CASE ELSE`;
- в примеры добавлены и строковые варианты, а в диагностике перечислены реальные коды ошибок, уже закреплённые в языке.

### Что ещё осталось
- справочная страница приведена в порядок, но следующий функциональный этап по арифметике ещё не начат;
- отдельная реализация целочисленного деления, остатка и `MOD` пока ждёт следующего рабочего прохода.

### Следующий точечный шаг
- спроектировать и внедрить арифметические операции целочисленного деления и остатка, включая синтаксис, семантику, диагностику, тесты, `.axm`-примеры и обновление manual.

## Обновление 2026-04-19 - Целочисленное деление и MOD

### Что сделано
- в язык добавлены операторы `\` и `MOD` с полным проходом через лексер, парсер, семантику и интерпретатор;
- поведение закреплено в автоматических тестах, `.axm`-примерах, спецификации и справочнике;
- `docs/manual/scripts/manual.js` восстановлен после порчи кодировки, чтобы левое меню и алфавитный указатель снова показывали нормальный русский текст.

### Что ещё осталось
- обязательный базовый арифметический набор на текущем этапе закрыт: сложение, вычитание, умножение, обычное деление, целочисленное деление и остаток теперь описаны и реализованы;
- если расширять арифметику дальше, лучше делать это отдельным решением, а не смешивать с уже закрытым базовым набором.

### Следующий точечный шаг
- выбрать следующий самостоятельный этап языка вне базового арифметического ядра; если возвращаться именно к арифметике, сначала отдельно спроектировать будущие расширения и только потом внедрять их в код и документацию.

## Обновление 2026-04-19 - Исследование расширенной арифметики по QuickBASIC и PDS 7.1

### Что сделано
- по исходной документации Microsoft отдельно перепроверены возведение в степень, побитовые операции, целочисленные литералы в разных системах счисления и возможные операции сдвига;
- подтверждено, что в QuickBASIC и BASIC PDS 7.1 уже были `^`, логико-побитовые операторы `NOT`, `AND`, `OR`, `XOR`, `EQV`, `IMP`, а также шестнадцатеричные литералы с префиксом `&H`;
- подтверждено, что встроенных операторов сдвига в историческом BASIC не было, поэтому эта часть может рассматриваться только как современное расширение языка, а не как восстановление старого поведения.

### Что ещё осталось
- нужно выбрать точный рабочий объём следующего этапа, чтобы не смешать историческую совместимость с чрезмерным заимствованием синтаксиса из C#;
- отдельно нужно решить, включать ли в ближайший этап только практически полезные расширения (`^`, `&H`, `AND`/`OR`/`XOR`/`NOT` для целых чисел, `<<`, `>>`) или сразу тянуть более редкие исторические операторы `EQV` и `IMP`;
- после выбора объёма потребуется отдельное проектирование приоритетов, правил типов, диагностики, примеров manual и `.axm`-принятия.

### Следующий точечный шаг
- зафиксировать проектное решение для следующего арифметического этапа в таком порядке: `^`, шестнадцатеричные литералы `&H`, побитовые целочисленные `NOT`/`AND`/`OR`/`XOR`, затем современные сдвиги `<<` и `>>`; `EQV` и `IMP` пока держать как возможный следующий исторический слой, но не включать без отдельного подтверждения.

## Обновление 2026-04-19 - Расширенная арифметика: `^`, `&H`, побитовые операции и сдвиги

### Что сделано
- в язык добавлены оператор степени `^`, шестнадцатеричные литералы `&H`, побитовые режимы `NOT`/`AND`/`OR`, новый оператор `XOR`, а также сдвиги `<<` и `>>`;
- поведение закреплено в лексере, парсере, семантике, интерпретаторе, автоматических тестах, `.axm`-примерах, спецификации и manual;
- справочная страница арифметики переписана в принятом формате и расширена новыми примерами и ссылками на тематические образцы.

### Что ещё осталось
- исторические операторы `EQV` и `IMP` пока остаются вне ближайшего обязательного объёма;
- модель шестнадцатеричных литералов пока зафиксирована как неотрицательная запись `INTEGER`/`LONG` без отдельной исторической семантики знаковых масок.

### Следующий точечный шаг
- если продолжать именно арифметическую ветку, отдельно спроектировать, нужны ли языку `EQV` и `IMP`, и нужно ли приближать поведение `&H` к более исторической модели знаковых побитовых масок.

## Обновление 2026-04-19 - EQV и IMP

### Что сделано
- в язык добавлены исторические логико-побитовые операторы `EQV` и `IMP`;
- `EQV` работает как равнозначность для `BOOLEAN` и как побитовое отрицание `XOR` для `INTEGER` и `LONG`;
- `IMP` работает как импликация для `BOOLEAN` и как побитовое `NOT` от левого операнда с последующим `OR` для `INTEGER` и `LONG`;
- справочник, спецификация, автоматические тесты, подсветка IDE и ручные `.axm`-примеры обновлены вместе с кодом.

### Что ещё осталось
- модель шестнадцатеричных литералов `&H...` пока остаётся простой: неотрицательные значения `INTEGER` или `LONG`, без исторической трактовки знаковых побитовых масок.

### Следующий точечный шаг
- если продолжать именно арифметическую ветку, отдельно решить, нужна ли языку историческая модель знаковых масок для `&H`; если нет, арифметический этап можно считать закрытым и переходить к следующей подсистеме языка.

## Обновление 2026-04-19 - Проверка EQV, IMP и унарного минуса

### Что сделано
- проверка реализации `EQV` и `IMP` доведена до успешного результата;
- исправлено правило разбора унарных `-` и `NOT`: они связываются сильнее `*`, `/`, `\`, `MOD`, но слабее `^`, как уже описано в справочнике;
- ручные примеры для `\` и `MOD` дополнены случаями с отрицательным правым операндом.

### Что ещё осталось
- обязательный арифметический набор на текущем этапе закрыт;
- историческая трактовка знаковых шестнадцатеричных масок `&H...` остаётся отдельным возможным решением, а не незавершённой частью текущей реализации.

### Следующий точечный шаг
- выбрать следующий самостоятельный этап развития языка вне закрытого арифметического набора либо отдельно запланировать историческую модель `&H...`, если она действительно нужна.

## Обновление 2026-04-19 - Реализация функций регулярных выражений

### Что сделано
- добавлен отдельный набор встроенных функций регулярных выражений: `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE`, `REGEXCOUNT`;
- функции оформлены отдельными файлами в семантике и среде выполнения, без расширения единого большого файла встроенных функций;
- поддержаны режимы `i`, `m`, `s`, `x`, проверка недопустимого регулярного выражения и ограничение времени выполнения операции;
- добавлены автоматические тесты, ручные `.axm`-примеры, спецификация и отдельная страница справочника.

### Что ещё осталось
- функции, которым нужен возврат массива совпадений или частей строки, сознательно не добавлены до отдельного этапа развития массивов;
- `SPLIT` и `JOIN` остаются зарезервированными до завершения этого этапа.

### Следующий точечный шаг
- выбрать следующий самостоятельный этап языка: развитие массивов с последующим возвратом к `SPLIT`/`JOIN` и возможным функциям регулярных выражений, возвращающим набор значений.

## Обновление 2026-04-19 - Отложенная задача по регулярным выражениям, возвращающим набор значений

### Что нужно не забыть
- после развития модели массивов вернуться к функциям регулярных выражений, которые возвращают не одно значение, а набор значений;
- отдельно спроектировать формы вызова, тип результата и поведение для поиска всех совпадений, извлечения групп из всех совпадений и разделения строки по регулярному выражению;
- имена `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` уже зарезервированы в справочнике как будущие функции;
- до утверждения модели массивов не закреплять окончательные формы вызова и тип результата таких функций, чтобы потом не переделывать поддерживаемое поведение языка.

### Связанные отложенные задачи
- развитие массивов;
- возвращение к `SPLIT` и `JOIN`;
- добавление функций `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` для набора совпадений и частей строки.

## Обновление 2026-04-19 - План функций файловой системы

### Что берём из референса Axiom
- разделение файловых возможностей на понятные группы: проверка существования, чтение и запись текстовых файлов, работа с папками, разбор путей и сведения о файлах;
- единое приведение пути к полному виду перед выполнением операции;
- отдельное место в коде для будущих правил доступа к файловой системе, чтобы позднее можно было ограничивать чтение, запись и удаление без переделки всех функций.

### Первый этап реализации
- `FILEEXISTS(<путь>) AS BOOLEAN` - проверяет, существует ли файл;
- `DIREXISTS(<путь>) AS BOOLEAN` - проверяет, существует ли папка;
- `PATHEXISTS(<путь>) AS BOOLEAN` - проверяет, существует ли файл или папка;
- `READFILE(<путь>) AS STRING` - читает текстовый файл целиком;
- `WRITEFILE(<путь>, <текст>) AS BOOLEAN` - создаёт или перезаписывает текстовый файл;
- `APPENDFILE(<путь>, <текст>) AS BOOLEAN` - добавляет текст в конец файла;
- `FILESIZE(<путь>) AS LONG` - возвращает размер файла в байтах;
- `FILEDATE(<путь>) AS STRING` - возвращает дату последнего изменения файла;
- `COMBINEPATH(<левая часть>, <правая часть>) AS STRING` - соединяет части пути;
- `FILENAME(<путь>) AS STRING` - возвращает имя файла;
- `DIRNAME(<путь>) AS STRING` - возвращает имя папки из пути;
- `EXTNAME(<путь>) AS STRING` - возвращает расширение файла;
- `FULLPATH(<путь>) AS STRING` - возвращает полный путь;
- `MAKEDIR(<путь>) AS BOOLEAN` - создаёт папку, если её ещё нет.

### Что не добавлять в первый этап
- функции, возвращающие набор строк или путей: `READLINES`, `GETFILES`, `GETDIRS`; они должны быть спроектированы после развития массивов;
- удаление, копирование, перенос и переименование файлов и папок; эти операции требуют отдельного решения по безопасному поведению и сообщениям об ошибках;
- рекурсивное удаление папок, символьные ссылки, наблюдение за изменениями файлов и смена текущей папки программы;
- операторную модель `OPEN`/`CLOSE` с файловыми номерами; это отдельная языковая подсистема, а первый этап должен остаться набором простых встроенных функций.

### Следующий точечный шаг
- реализовать первый безопасный этап файловых функций в отдельных файлах семантики и среды выполнения, добавить автоматические проверки, справочник и ручные `.axm`-примеры.

## Обновление 2026-04-19 - Реализован первый этап функций файловой системы

### Что сделано
- добавлены встроенные функции `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `READFILE`, `WRITEFILE`, `APPENDFILE`, `FILESIZE`, `FILEDATE`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `EXTNAME`, `FULLPATH` и `MAKEDIR`;
- описание функций вынесено в отдельный файл семантики, выполнение - в отдельный файл среды выполнения;
- добавлены автоматические проверки семантики и выполнения;
- добавлены ручные `.axm`-примеры для каждой новой функции;
- обновлены спецификация, справочник и навигация справочника.

### Что остаётся отложенным
- функции, возвращающие набор строк или путей, должны ждать развития массивов;
- удаление, копирование, перенос и переименование файлов и папок требуют отдельного проектного решения;
- `OPEN`/`CLOSE` с файловыми номерами остаются отдельной языковой подсистемой, а не частью текущего набора функций.

### Следующий точечный шаг
- выбрать следующий этап: либо развивать второй слой файловой системы с безопасными правилами для удаления, копирования и переноса, либо вернуться к массивам, чтобы затем добавить функции, возвращающие набор значений.

## Обновление 2026-04-19 - Реализован второй этап функций файловой системы

### Что сделано
- добавлены функции `DELETEFILE`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEDIR`, `COPYDIR`, `MOVEDIR` и `RENAMEDIR`;
- `COPYFILE`, `MOVEFILE` и `COPYDIR` поддерживают необязательный логический аргумент перезаписи;
- `DELETEFILE` возвращает `FALSE`, если файла нет;
- `DELETEDIR` удаляет только пустую папку и возвращает `FALSE`, если папки нет;
- `RENAMEFILE` и `RENAMEDIR` принимают простое новое имя без пути, чтобы переименование не превращалось в скрытый перенос;
- обновлены автоматические проверки, ручные `.axm`-примеры, спецификация и справочник.

### Что остаётся по файловой системе
- отдельно спроектировать третий слой файловой системы: чтение сведений о папках, дисках и, при необходимости, безопасное рекурсивное удаление;
- отдельно решить, нужна ли языку операторная модель `OPEN`/`CLOSE` с файловыми каналами;
- не добавлять опасные операции без явных правил безопасности и понятной диагностики.

### Следующий точечный шаг
- продолжить именно файловую систему: спроектировать третий слой возможностей вокруг сведений о папках, размере папки, свободном месте и возможных правилах безопасного рекурсивного удаления.

## Обновление 2026-04-19 - Реализован третий безопасный слой функций файловой системы

### Что сделано
- добавлены функции `CURRENTDIR`, `DIRSIZE`, `FREESPACE`, `MAKETEMPFILE` и `MAKETEMPDIR`;
- третий слой пока остаётся безопасным и информационным: сведения о текущей папке, размере папки, свободном месте и создание временных объектов;
- `DIRSIZE` считает размер файлов рекурсивно, но не заходит в папки-ссылки и другие точки повторного анализа;
- для новых функций добавлены автоматические проверки, ручные `.axm`-примеры, спецификация и страница справочника;
- алфавитный указатель справочника расширен новыми файловыми функциями.

### Что ещё остаётся
- отдельно решить, нужен ли языку переход в другую текущую папку программы;
- отдельно спроектировать безопасное рекурсивное удаление папок;
- отдельно решить, нужна ли языку операторная подсистема `OPEN`/`CLOSE`.

### Следующий точечный шаг
- продолжить именно файловую систему без отката назад: либо спроектировать безопасное рекурсивное удаление, либо добавить следующий безопасный пакет сведений о дисках и путях.

## Обновление 2026-04-19 - Добавлены рекурсивное удаление папок и смена текущей папки

### Что сделано
- добавлены функции `DELETEDIRRECURSIVE` и `CHANGEDIR`;
- для `DELETEDIRRECURSIVE` выбрано поведение в духе C#: папка удаляется вместе со всем содержимым, а отсутствие папки даёт `FALSE`, как и у других удаляющих функций Axiom;
- для `CHANGEDIR` выбрано поведение смены текущей папки программы через модель, близкую к `Directory.SetCurrentDirectory`, с сохранением принятой в Axiom формы результата `BOOLEAN`;
- обновлены автоматические проверки, runnable `.axm`-примеры, спецификация, страница справочника и алфавитный указатель.

### Что ещё остаётся
- отдельно решить, нужны ли функции перечисления файлов и папок с возвратом набора значений;
- отдельно решить, нужна ли языку операторная подсистема `OPEN`/`CLOSE`;
- при желании можно позже добавить дополнительные сведения о дисках и атрибутах файловой системы.

### Следующий точечный шаг
- продолжить файловую систему в сторону перечисления и сведений о файловой системе либо перейти к следующей крупной подсистеме языка по приоритету проекта.

## Обновление 2026-04-19 - Добавлен расширенный скалярный слой файловой системы

### Что сделано
- добавлены функции `FILECREATEDATE`, `FILEACCESSEDDATE`, `DIRDATE`, `PATHROOT`, `ISABSOLUTEPATH`, `TOTALFREESPACE`, `TOTALSPACE`, `ISREADONLY`, `ISHIDDEN`, `DRIVENAME`, `DRIVEFORMAT` и `DRIVETYPE`;
- файловая подсистема получила расширенные сведения о времени, пути, атрибутах объекта и диске без изменения синтаксиса языка;
- обновлены автоматические проверки, runnable `.axm`-примеры, спецификация, страница справочника и алфавитный указатель.

### Что ещё остаётся
- перечисление файлов и папок с возвратом набора значений требует отдельного этапа развития модели результата на стороне языка;
- операторная подсистема `OPEN` / `CLOSE` остаётся следующим крупным самостоятельным этапом файловой системы;
- при необходимости позже можно добавить запись и чтение дополнительных атрибутов и сведений о ссылках.

### Следующий точечный шаг
- начать проектирование и реализацию операторной подсистемы `OPEN` / `CLOSE` с минимальным рабочим набором чтения и записи по файловому каналу.

## Обновление 2026-04-19 - Реализованы тип FILE и операторы OPEN / CLOSE

### Что сделано
- добавлен специальный тип `FILE` для открытых текстовых файлов;
- реализованы операторы `OPEN ... FOR INPUT|OUTPUT|APPEND AS <файл>` и `CLOSE <файл>`;
- добавлены встроенные функции `EOF`, `READLINE`, `WRITE`, `WRITELINE`;
- обновлены спецификация, справочник, алфавитный указатель и ручные runnable `.axm`-примеры;
- `GETFILES` и `GETDIRS` зафиксированы в документации как будущие функции, зарезервированные до отдельного этапа развития массивов.

### Что ещё остаётся
- перечисление файлов и папок остаётся отдельным этапом, потому что ему нужен устойчивый результат в виде набора значений;
- историческая модель файловых номеров в духе старого BASIC по-прежнему не принимается как основная;
- при необходимости следующим отдельным этапом можно расширять уже не только функции файловой системы, но и операторную модель чтения и записи вокруг открытого `FILE`.

### Следующий точечный шаг
- продолжить файловую подсистему без возврата к файловым номерам: приоритетно расширять документацию и поведение вокруг открытого `FILE`, а перечисление `GETFILES` / `GETDIRS` держать как будущий этап до завершения работы по массивам.

## Обновление 2026-04-19 - Добавлены функции состояния и буфера FILE

### Что сделано
- добавлены встроенные функции `ISOPEN`, `FILEPATH`, `FILEMODE` и `FLUSH` для работы с уже открытым `FILE`;
- модель текстового файла уточнена в современную сторону: `WRITE` и `WRITELINE` пишут в буфер, а `FLUSH` и `CLOSE` гарантированно сбрасывают его на диск;
- обновлены автоматические проверки, runnable `.axm`-примеры, спецификация, справочник и алфавитный указатель.

### Что ещё остаётся
- перечисление `GETFILES` и `GETDIRS` по-прежнему отложено до отдельного этапа развития массивов;
- вокруг открытого `FILE` ещё можно отдельно спроектировать позиционные операции, если они действительно нужны языку;
- историческая модель файловых номеров по-прежнему не рассматривается как основная.

### Следующий точечный шаг
- если продолжаем именно файловую подсистему, следующим аккуратным этапом спроектировать и обсудить позиционные возможности вокруг открытого `FILE`: нужна ли языку работа с текущей позицией, длиной файла и явным переходом по позиции.

## Обновление 2026-04-19 - Добавлены позиционные функции вокруг открытого FILE

### Что сделано
- добавлены встроенные функции `FILEPOS`, `FILELENGTH` и `SEEKFILE` вокруг уже открытого `FILE`;
- модель `RuntimeTextFile` переведена на явный поток с собственной буферизацией записи, чтобы позиция и длина файла были точными даже до вызова `FLUSH`;
- позиция считается в байтах от начала файла, первая позиция равна `0`;
- `SEEKFILE` разрешён для режимов `INPUT` и `OUTPUT`, но не поддерживается для `APPEND`;
- обновлены автоматические проверки, runnable `.axm`-примеры, спецификация, справочник и алфавитный указатель.

### Что ещё остаётся
- при желании можно отдельно добавить более узкие служебные функции вокруг открытого файла, например удобный переход в начало или в конец;
- перечисление `GETFILES` и `GETDIRS` по-прежнему отложено до отдельного этапа развития массивов;
- историческая модель файловых номеров по-прежнему не рассматривается как основная.

### Следующий точечный шаг
- если продолжаем файловую подсистему, следующим аккуратным шагом можно спроектировать современное чтение и запись блоков данных вокруг открытого `FILE` либо вернуться к отложенному перечислению файлов и папок после отдельного этапа по массивам.

## Обновление 2026-04-19 - Добавлены READTOEND, READTEXT и REWIND для открытого FILE

### Что сделано
- добавлены встроенные функции `READTOEND`, `READTEXT` и `REWIND` вокруг уже открытого `FILE`;
- текстовое чтение поверх позиционной модели уточнено явно: низкоуровневая позиция считается в байтах, а `READLINE`, `READTEXT` и `READTOEND` работают с символами текста;
- в спецификации и справочнике закреплено правило смешивания этих моделей: перед текстовым чтением позиция должна попадать на корректную границу символа UTF-8;
- `READTEXT` читает до заданного числа символов, `READTOEND` читает весь оставшийся текст, `REWIND` переводит файл в начало;
- обновлены автоматические проверки, runnable `.axm`-примеры, страницы справочника и алфавитный указатель.

### Что ещё остаётся
- если продолжать файловую подсистему, можно отдельно решить, нужен ли следующий небольшой слой удобных текстовых операций вокруг открытого `FILE`;
- перечисление `GETFILES` и `GETDIRS` по-прежнему отложено до отдельного этапа развития массивов;
- историческая модель файловых номеров по-прежнему не рассматривается как основная.

### Следующий точечный шаг
- если продолжаем именно файловую подсистему, следующим аккуратным шагом можно решить, нужен ли отдельный современный набор более крупных операций чтения и записи вокруг открытого `FILE`, либо пока оставить подсистему в текущем компактном текстовом виде.

## Обновление 2026-04-19 - Добавлен мини-пакет LINENUMBER, SKIPLINE и NEWLINE

### Что сделано
- добавлены встроенные функции `LINENUMBER`, `SKIPLINE` и `NEWLINE` вокруг уже открытого `FILE`;
- в текстовой модели `FILE` закреплена нумерация строк: первая строка имеет номер `1`, после `READLINE` и `SKIPLINE` номер сдвигается, после `SEEKFILE` и `REWIND` пересчитывается по текущей позиции;
- в спецификации и справочнике заранее зафиксирован проектный список следующих файловых направлений, чтобы он не потерялся: массивные результаты (`GETFILES`, `GETDIRS`, `READALLLINES`, `READLINES`) и будущая двоичная модель (`FOR BINARY`, `READBYTE`, `WRITEBYTE` и дальнейшие блочные операции);
- обновлены автоматические проверки, runnable `.axm`-примеры и алфавитный указатель справочника.

### Что ещё остаётся
- отдельным самостоятельным этапом усилить массивы как полноценное значение языка, потому что от этого напрямую зависят `GETFILES`, `GETDIRS`, `READALLLINES`, `READLINES`, а также уже зарезервированные строковые и регулярные функции;
- после этапа массивов отдельно спроектировать двоичную модель файловой подсистемы: режим `FOR BINARY`, побайтовое чтение и запись, затем при необходимости блочные операции;
- позже вернуться к дальнейшему расширению файловой подсистемы уже поверх этих двух опорных этапов, не смешивая их в один большой проход.

### Следующий точечный шаг
- если двигаться по зависимости подсистем, следующим логичным этапом должно стать именно усиление массивов как значения языка, потому что без него нельзя полноценно открыть отложенные файловые, строковые и регулярные функции.

## Обновление 2026-04-19 - Спроектирован roadmap развития одномерных массивов

### Что зафиксировано по текущему состоянию
- текущий Axiom поддерживает только базовый минимум одномерных массивов: объявление через `DIM <name>(<lower> TO <upper>) AS <type>`, индексный доступ и присваивание элементу;
- массив пока не является полноценным значением языка: нет отдельного value-oriented контракта для присваивания массива целиком, передачи в параметры, возврата из функций и устойчивого каталога встроенных функций, работающих с массивами как с результатом;
- именно это ограничение сейчас блокирует следующий этап файловой системы (`GETFILES`, `GETDIRS`, `READALLLINES`, `READLINES`), а также будущие строковые и регулярные функции, которым нужен возврат набора значений.

### Что стоит взять из референса Axiom
- брать не весь объём API сразу, а структуру подсистемы: отдельно фундамент массива, отдельно информационные функции, отдельно чистые утилиты, отдельно поиск, отдельно мутации и отдельно higher-order операции;
- сохранить сильную типизацию массива по типу элемента и явную модель границ вместо неявных универсальных контейнеров;
- заранее заложить поддержку пустого результата как нормального массива, а не как специального случая ошибки;
- различать операции, сохраняющие форму массива, и операции, создающие новый компактный результат.

### Что не нужно переносить в Axiom напрямую
- отрицательную индексацию вида `arr(-1)` не принимать в базовый план Axiom: это плохо сочетается с BASIC-стилем языка и усложняет будущий единый контракт для многомерных массивов;
- не переносить `unsafe`-режимы и отключаемую проверку границ;
- не переносить CLR-ориентированные вспомогательные контейнеры `Dictionary` / `List` как часть первой волны массивов;
- higher-order API не строить вокруг имён callback-ов строкой: у Axiom уже есть значения функций и замыкания, поэтому поздний HOF-этап должен опираться именно на них.

### Архитектурные решения для 1D этапа
- одномерные массивы должны перейти от модели `VariableSymbol.IsArray` к полноценной модели типа массива, пригодной для хранения в переменной, возврата из функции и передачи в аргументы;
- в рантайме нужно различать:
  - невыделенный динамический массив;
  - выделенный пустой массив;
  - выделенный непустой массив;
- для пустого выделенного результата зафиксировать нормальную форму `1 TO 0`, чтобы пустые результаты от `GETFILES`, `FILTER`, `SPLIT`, `READALLLINES` и похожих функций не требовали отдельной специальной семантики;
- операции, сохраняющие количество элементов и их позиционный смысл, должны по умолчанию сохранять исходные границы;
- операции, создающие новый компактный набор значений, должны возвращать массив с нормализованными границами `1 TO N`;
- в 1D-этапе не расширять грамматику до общего postfix-индексирования произвольного выражения; это отдельный следующий этап, нужный уже для массивов массивов и более сложной цепочки выражений.

### План развития одномерных массивов

### Этап 1. Фундамент: массив как значение языка
- ввести отдельную типовую модель массива в `Semantics`, чтобы массив больше не был только специальным видом переменной;
- определить минимальный динамический синтаксис объявления без немедленного выделения памяти: `DIM items() AS STRING`;
- разрешить присваивание массива целиком, возврат массива из `FUNCTION` и передачу массива в параметры;
- зафиксировать семантику копирования: базовый план исходит из разделяемой ссылочной модели массива с отдельной функцией явного клонирования позже;
- сохранить текущий контракт индексирования по имени массива без попытки сразу поддержать индексирование произвольного выражения.

### Этап 2. Жизненный цикл и форма 1D массива
- добавить `REDIM` для первичного выделения и изменения длины динамического массива;
- затем добавить `REDIM PRESERVE` как отдельное расширение поверх уже работающего `REDIM`;
- добавить `ERASE` для явного сброса массива в невыделенное состояние;
- добавить минимальный пакет информационных функций: `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`;
- для всех этих форм зафиксировать отдельные синтаксические, семантические и runtime-диагностики.

### Этап 3. Минимальный пакет, который разблокирует другие подсистемы
- после завершения foundation-этапа открыть массивозависимые функции файловой системы: `GETFILES`, `GETDIRS`, `READALLLINES`, `READLINES`;
- в тот же слой включить строковую пару `SPLIT` / `JOIN`, потому что она естественно использует тот же контракт результата;
- для регулярных выражений после этого же фундамента открыть `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT`;
- этот этап должен опираться только на уже утверждённую модель массива как значения и не тянуть за собой пока поисковый, мутирующий и higher-order пакет.

### Этап 4. Чистые 1D-утилиты
- добавить операции, создающие новые массивы без изменения исходного: `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTOSORTED`, `ARRAYTOREVERSED`, `ARRAYDISTINCT`, `ARRAYCLONE`;
- закрепить единое правило границ:
  - `ARRAYTOSORTED`, `ARRAYTOREVERSED`, `ARRAYCLONE` сохраняют границы;
  - `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYDISTINCT`, `SPLIT` и функции перечисления возвращают компактный `1 TO N`.

### Этап 5. Поиск и read-only операции
- добавить `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL`;
- `ARRAYBINARYSEARCH` рассматривать как отдельный завершающий подпакет этого этапа, а не как часть обязательного минимального ядра;
- все поисковые операции должны быть read-only и не менять исходный массив.

### Этап 6. Мутирующие операции
- добавить `ARRAYFILL`, `ARRAYREVERSE`, `ARRAYSORT`, `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- `ARRAYSHIFT` и `ARRAYUNSHIFT` оставить как поздний подпакет, если они действительно понадобятся поверх файловой и строковой подсистем;
- все мутирующие операции должны быть спроектированы уже после того, как будет окончательно утверждена ссылочная семантика присваивания массива целиком.

### Этап 7. Higher-order массивные функции
- добавить `ARRAYMAP`, `ARRAYFILTER`, `ARRAYREDUCE`, `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFOREACH`;
- эти функции должны принимать именно значения функций и замыкания Axiom, а не строковые имена callback-ов;
- `ARRAYFILTER` и похожие операции возвращают компактный `1 TO N`, а `ARRAYMAP` сохраняет исходные границы.

### Что сознательно откладывается после полного 1D этапа
- обобщённое индексирование произвольного выражения;
- двумерные и многомерные массивы как отдельный контракт `Rank > 1`;
- массивы массивов и прочая jagged-модель;
- специальные контейнерные функции поверх словарей, списков и смешанных структур.

### Следующий точечный шаг
- следующим рабочим срезом должен стать не весь пакет сразу, а foundation-этап: зафиксировать в спецификации контракт динамического одномерного массива как значения языка, включая `DIM items() AS T`, различие между невыделенным и пустым выделенным массивом, а затем реализовать этот слой в `Syntax`, `Semantics` и `Runtime`.

### Update 2026-04-19 - Foundation Stage Started In Code
- The first foundation slice is now implemented: `DIM arr() AS TYPE` is parsed, binding preserves missing bounds as the sign of an unallocated dynamic array, and runtime reports `MB3005` when code indexes that array before allocation.
- The contract is now documented in `docs/SPECIFICATION.md`, `docs/manual/arrays.html`, `docs/manual/diagnostics.html`, and backed by the acceptance sample `samples/operators/dim/dynamic_array_declaration.axm`.
- The foundation stage is still incomplete: arrays are not first-class values yet, so whole-array assignment, passing arrays as parameters, and returning arrays from `FUNCTION` are still deferred.

### Next Exact Step After This Foundation Slice
- The next substep should be to promote arrays to a first-class language value with an explicit array type in `Semantics`, plus whole-array assignment, parameter passing, and function return.
- Only after that should the project move to `REDIM`, `REDIM PRESERVE`, `ERASE`, and informational array built-ins.

### Update 2026-04-20 - Array Value Foundation Completed
- Arrays are now first-class language values: `Semantics` has an explicit array type, whole-array assignment works, arrays can be passed to parameters, and arrays can be returned from `FUNCTION`.
- Type-position syntax now supports array forms such as `INTEGER()` and `STRING()` for parameters and function result types, while variable declarations keep the existing `DIM name(...) AS TYPE` form.
- Runtime behavior for this stage uses shared-reference semantics for whole-array assignment and parameter passing.
- Manual pages and the runnable sample `samples/smoke/array_first_class_values.axm` now document the completed foundation slice.

### Next Exact Step After Array Value Foundation
- Implement `REDIM` as the next lifecycle operation for dynamic one-dimensional arrays, including initial allocation and resizing rules.
- Keep `REDIM PRESERVE`, `ERASE`, and informational built-ins as follow-up steps on top of a working `REDIM` contract.

### Update 2026-04-20 - REDIM Implemented
- The first lifecycle operation is now in code: `REDIM name(lower TO upper)` allocates an unallocated array and reallocates an existing array without preserving old elements.
- The current `REDIM` contract intentionally stays narrow:
  - only the base form is supported;
  - bounds are explicit through `lower TO upper`;
  - the shared-reference array model means reallocating an alias affects every alias of the same array.
- The contract is now documented in `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/manual/arrays.html`, and backed by the runnable sample `samples/operators/redim/basic_usage.axm`.

### Next Exact Step After REDIM
- Implement `REDIM PRESERVE` on top of the working `REDIM` base so arrays can change bounds while keeping the overlapping element range.
- After that, add `ERASE` and the first informational built-ins such as `LBOUND`, `UBOUND`, `ARRAYLENGTH`, and `ARRAYISALLOCATED`.

### Update 2026-04-20 - REDIM PRESERVE Implemented
- The second lifecycle operation is now in code: `REDIM PRESERVE name(lower TO upper)` changes array bounds while retaining the overlapping logical index range.
- The current `REDIM` family now has two explicit behaviors:
  - `REDIM` reallocates and resets all elements to default values;
  - `REDIM PRESERVE` reallocates and copies the intersection of old and new bounds.
- The shared-reference array model is unchanged, so preserving through an alias still mutates the shared runtime array object.
- The contract is now documented in `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/manual/arrays.html`, and backed by the runnable sample `samples/operators/redim/preserve_basic_usage.axm`.

### Next Exact Step After REDIM PRESERVE
- Implement `ERASE` as the next lifecycle operation for one-dimensional arrays.
- After `ERASE`, add the first informational built-ins such as `LBOUND`, `UBOUND`, `ARRAYLENGTH`, and `ARRAYISALLOCATED`.

### Update 2026-04-20 - ERASE Implemented
- The third lifecycle operation is now in code: `ERASE name` resets a one-dimensional array into the unallocated state.
- The current `ERASE` contract intentionally stays narrow:
  - one array name per statement;
  - target must already be an array;
  - aliasing stays visible because the shared runtime array object is mutated in place.
- The contract is now documented in `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/manual/arrays.html`, and backed by the runnable sample `samples/operators/erase/basic_usage.axm`.

### Next Exact Step After ERASE
- Add the first informational array built-ins: `LBOUND`, `UBOUND`, `ARRAYLENGTH`, and `ARRAYISALLOCATED`.
- Keep that slice focused on one-dimensional arrays and the already established allocated/unallocated lifecycle model.

### Обновление 2026-04-20 - Информационные встроенные функции массива реализованы
- Добавлены `LBOUND`, `UBOUND`, `ARRAYLENGTH` и `ARRAYISALLOCATED` для одномерных массивов.
- Текущий контракт зафиксирован так:
  - `LBOUND` и `UBOUND` требуют уже выделенный массив;
  - `ARRAYLENGTH` для невыделенного массива возвращает `0`;
  - `ARRAYISALLOCATED` для невыделенного массива возвращает `FALSE`.
- Этот срез намеренно остаётся только информационным и не добавляет новых изменяющих операций поверх массива.

### Следующий точный шаг после информационных функций массива
- Следующим рабочим срезом нужно ввести явную поддержку пустого, но уже выделенного массива как нормальной формы состояния.
- Целевой контракт этого шага:
  - массив с пустым результатом должен отличаться от невыделенного массива;
  - нормальная пустая форма должна использовать диапазон вида `1 TO 0`;
  - после этого можно безопасно переходить к функциям, которые возвращают массивы, например к будущим файловым и строковым операциям перечисления.

### Обновление 2026-04-20 - Пустой выделенный массив реализован
- В модели времени выполнения добавлено состояние пустого, но уже выделенного одномерного массива.
- Нормальная пустая форма закреплена как `1 TO 0`.
- `REDIM name(1 TO 0)`, `REDIM PRESERVE name(1 TO 0)` и `DIM name(1 TO 0) AS TYPE` теперь создают выделенный массив с длиной `0`.
- `ERASE` по-прежнему возвращает массив в невыделенное состояние.
- Любые другие обратные диапазоны остаются ошибкой.

### Следующий точный шаг после пустого выделенного массива
- Перейти к минимальному набору функций, которые возвращают одномерные массивы и разблокируют файловую и строковую подсистемы.
- Начать лучше с строковой пары `SPLIT` и `JOIN`, потому что она не требует доступа к файловой системе и хорошо проверяет контракт массивов как результата функции.
- После этого можно переносить тот же контракт на `GETFILES`, `GETDIRS`, `READALLLINES` и похожие операции перечисления.

### Обновление 2026-04-20 - SPLIT и JOIN реализованы
- Добавлены строковые функции `SPLIT` и `JOIN`.
- `SPLIT` возвращает новый массив строк:
  - непустой результат получает границы `1 TO N`;
  - пустая исходная строка возвращает пустой выделенный массив `1 TO 0`;
  - пустые части между разделителями сохраняются;
  - пустой разделитель запрещён.
- `JOIN` принимает строковый массив и собирает его элементы в строку:
  - разделитель по умолчанию — запятая;
  - невыделенный и пустой выделенный массивы возвращают пустую строку.

### Следующий точный шаг после SPLIT и JOIN
- Перейти к файловым функциям, которые возвращают одномерные массивы строк.
- Начать с `GETFILES` и `GETDIRS`, потому что они используют тот же результат `STRING()` и сразу разблокируют практические сценарии перечисления файлов и папок.
- После этого перейти к `READALLLINES` и `READLINES`.

### Обновление 2026-04-20 - GETFILES и GETDIRS реализованы
- Добавлены файловые функции `GETFILES` и `GETDIRS`, возвращающие `STRING()`.
- Поддержаны формы с одним аргументом пути и с двумя аргументами пути и маски.
- Для пустого результата используется выделенный пустой массив `1 TO 0`.
- Перечисление выполняется только по ближайшему уровню указанной папки и сортируется без учёта регистра.
- Пустая маска, отсутствующая исходная папка и ошибки файловой системы дают диагностику времени выполнения `MB3007`.

### Следующий точный шаг после GETFILES и GETDIRS
- Перейти к чтению строк файла в массив: `READALLLINES` для пути и затем `READLINES` для открытого текстового файла.
- Сохранить тот же договор результата `STRING()`: непустой результат `1 TO N`, пустой результат `1 TO 0`.

### Обновление 2026-04-20 - READALLLINES реализована
- Добавлена файловая функция `READALLLINES`, возвращающая `STRING()`.
- Функция читает весь текстовый файл как UTF-8 и возвращает строки без символов перевода строки.
- Для непустого файла результат получает границы `1 TO N`.
- Для пустого файла результатом становится выделенный пустой массив `1 TO 0`.
- Отсутствующий файл, недопустимый путь, отказ доступа и другие ошибки файловой системы дают диагностику времени выполнения `MB3007`.

### Следующий точный шаг после READALLLINES
- Перейти к `READLINES` для чтения набора строк из уже открытого текстового файла.
- Сохранить тот же договор результата `STRING()`: непустой результат `1 TO N`, пустой результат `1 TO 0`.
- Отдельно проверить поведение на конце файла и при запросе нулевого количества строк.

### Обновление 2026-04-20 - READLINES реализована
- Добавлена функция `READLINES`, возвращающая `STRING()` для уже открытого текстового файла.
- Функция читает до указанного числа следующих строк из файла, открытого через `OPEN ... FOR INPUT AS file`.
- Строки возвращаются без символов перевода строки.
- Для непустого результата используются границы `1 TO N`.
- `count = 0` и конец файла возвращают выделенный пустой массив `1 TO 0`.
- Отрицательный `count`, закрытый файл, неправильный режим файла и ошибки чтения дают диагностику времени выполнения `MB3007`.

### Следующий точный шаг после READLINES
- Завершить минимальный файлово-строковый срез функций, возвращающих массивы, и перейти к чистым операциям над одномерными массивами.
- Перед первой такой операцией зафиксировать короткий контракт для `ARRAYCLONE` и `ARRAYSLICE`: какие границы сохраняются, когда возвращается компактный диапазон `1 TO N`, и как обрабатывается пустой результат.

### Обновление 2026-04-20 - Договор ARRAYCLONE и ARRAYSLICE зафиксирован
- Зафиксирован ближайший договор для первых чистых операций над одномерными массивами.
- `ARRAYCLONE` должна создавать независимую копию массива того же типа и сохранять состояние исходного массива:
  - невыделенный массив остаётся невыделенным;
  - выделенный непустой массив сохраняет нижнюю и верхнюю границы;
  - выделенный пустой массив сохраняет форму `1 TO 0`.
- `ARRAYSLICE` должна создавать независимый срез:
  - `start` считается логическим индексом исходного массива;
  - непустой результат получает компактные границы `1 TO N`;
  - пустой результат получает форму `1 TO 0`.
- Для `ARRAYSLICE` отдельно зафиксированы ошибки: невыделенный исходный массив даёт `MB3005`, отрицательное `count` и выход диапазона среза за границы дают `MB3007`.

### Следующий точный шаг после договора ARRAYCLONE и ARRAYSLICE
- Реализовать `ARRAYCLONE` как первую чистую операцию над одномерным массивом.
- В этом же шаге подготовить общий путь создания массива произвольного типа элемента, чтобы затем проще добавить `ARRAYSLICE`.

### Обновление 2026-04-20 - Активный журнал перенесён в WORKLOG2
- Создан `docs/WORKLOG2.md` как активная вторая часть журнала работ.
- `docs/WORKLOG.md` остаётся исторической первой частью до Session 168 включительно.
- Все дальнейшие записи нужно вести в `docs/WORKLOG2.md`.
- Нумерация сеансов продолжается без сброса: первая запись в новом журнале — Session 169.

### Обновление 2026-04-20 - ARRAYCLONE реализована
- Добавлена первая чистая функция над одномерными массивами: `ARRAYCLONE`.
- Функция возвращает новый массив того же типа и сохраняет текущее состояние исходного массива:
  - невыделенный массив остаётся невыделенным;
  - выделенный непустой массив сохраняет исходные границы;
  - пустой выделенный массив сохраняет форму `1 TO 0`.
- Клон не разделяет хранилище элементов с исходным массивом.
- В этом же шаге подготовлен общий путь связывания и построения массива произвольного типа элемента, чтобы следующий срез не дублировал частные решения для `STRING()`.

### Следующий точный шаг после ARRAYCLONE
- Реализовать `ARRAYSLICE(array, start, count)` как следующую чистую операцию над одномерным массивом.
- Сохранить уже зафиксированный договор:
  - непустой результат должен быть компактным `1 TO N`;
  - пустой результат должен иметь форму `1 TO 0`;
  - невыделенный исходный массив должен давать `MB3005`;
  - отрицательный `count` и выход диапазона за границы должны давать `MB3007`.

### Обновление 2026-04-20 - ARRAYSLICE реализована
- Добавлена вторая чистая функция над одномерными массивами: `ARRAYSLICE`.
- Функция создаёт независимый массив того же типа из части исходного массива.
- Для непустого результата используются компактные границы `1 TO N`.
- Для `count = 0` возвращается выделенный пустой массив `1 TO 0`.
- Невыделенный исходный массив даёт `MB3005`, отрицательный `count` и выход диапазона за границы дают `MB3007`.

### Следующий точный шаг после ARRAYSLICE
- Реализовать `ARRAYCONCAT(left, right)` как следующую чистую операцию над одномерными массивами.
- Сохранить для неё тот же общий принцип:
  - результат должен быть независимым от исходных массивов;
  - непустой результат должен быть компактным `1 TO N`;
  - пустой результат должен использовать форму `1 TO 0`.

### Обновление 2026-04-20 - ARRAYCONCAT реализована
- Добавлена третья чистая функция над одномерными массивами: `ARRAYCONCAT(left, right)`.
- Функция принимает два одномерных массива одного и того же типа и возвращает новый независимый массив.
- Для непустого результата используются компактные границы `1 TO N`.
- Если оба исходных массива выделены, но не содержат элементов, результатом становится выделенный пустой массив `1 TO 0`.
- Невыделенный `left` или `right` даёт диагностику времени выполнения `MB3005`.

### Следующий точный шаг после ARRAYCONCAT
- Реализовать `ARRAYTOREVERSED(array)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор:
  - результат должен быть независимым от исходного массива;
  - непустой результат должен быть компактным `1 TO N`;
  - пустой результат должен иметь форму `1 TO 0`;
  - невыделенный исходный массив должен давать `MB3005`.

### Обновление 2026-04-20 - ARRAYTOREVERSED реализована
- Добавлена четвёртая чистая функция над одномерными массивами: `ARRAYTOREVERSED(array)`.
- Функция принимает один одномерный массив и возвращает новый независимый массив того же типа.
- Элементы результата идут в обратном логическом порядке относительно исходного массива.
- Для непустого результата используются компактные границы `1 TO N`.
- Если исходный массив выделен, но пуст, результатом становится выделенный пустой массив `1 TO 0`.
- Невыделенный исходный массив даёт диагностику времени выполнения `MB3005`.

### Следующий точный шаг после ARRAYTOREVERSED
- Реализовать `ARRAYTAKE(array, count)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор:
  - результат должен быть независимым от исходного массива;
  - непустой результат должен быть компактным `1 TO N`;
  - пустой результат должен иметь форму `1 TO 0`;
  - невыделенный исходный массив должен давать `MB3005`;
  - отрицательный `count` должен давать `MB3007`.

### Обновление 2026-04-20 - ARRAYTAKE реализована
- Добавлена пятая чистая функция над одномерными массивами: `ARRAYTAKE(array, count)`.
- Функция принимает один одномерный массив и число элементов, затем возвращает новый независимый массив того же типа.
- В результат попадают первые элементы исходного массива, начиная с его текущей нижней границы.
- Для непустого результата используются компактные границы `1 TO N`.
- Если `count = 0` или исходный массив выделен, но пуст, результатом становится выделенный пустой массив `1 TO 0`.
- Если `count` больше длины массива, функция мягко возвращает весь массив в компактной форме.
- Невыделенный исходный массив даёт `MB3005`, а отрицательный `count` даёт `MB3007`.

### Следующий точный шаг после ARRAYTAKE
- Реализовать `ARRAYSKIP(array, count)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор:
  - результат должен быть независимым от исходного массива;
  - непустой результат должен быть компактным `1 TO N`;
  - пустой результат должен иметь форму `1 TO 0`;
  - невыделенный исходный массив должен давать `MB3005`;
  - отрицательный `count` должен давать `MB3007`.

### Обновление 2026-04-20 - ARRAYSKIP и ARRAYDISTINCT реализованы
- Добавлены шестая и седьмая чистые функции над одномерными массивами: `ARRAYSKIP(array, count)` и `ARRAYDISTINCT(array)`.
- `ARRAYSKIP` возвращает новый независимый хвост массива после пропуска первых логических элементов.
- `ARRAYDISTINCT` возвращает новый независимый массив без повторов и сохраняет порядок первых вхождений.
- Обе функции нормализуют непустой результат к компактным границам `1 TO N`.
- Для обеих функций пустой выделенный результат приводится к форме `1 TO 0`.
- Для `ARRAYSKIP` зафиксированы ошибки: невыделенный исходный массив даёт `MB3005`, отрицательный `count` даёт `MB3007`.
- Для `ARRAYDISTINCT` зафиксирована ошибка: невыделенный исходный массив даёт `MB3005`.
- В этом же шаге должны оставаться синхронными код, тесты, запускаемые примеры, подсветка IDE, пользовательская справка, спецификация и журналы проекта.

### Следующий точный шаг после ARRAYSKIP и ARRAYDISTINCT
- Реализовать следующий согласованный пакет поисковых и счётных функций над одномерными массивами:
  - `ARRAYCONTAINS(array, value)`
  - `ARRAYINDEXOF(array, value)`
  - `ARRAYLASTINDEXOF(array, value)`
  - `ARRAYCOUNT(array, value)`
- Для всего пакета заранее сохранить общий договор:
  - функции работают только с уже выделенным массивом;
  - сравнение значений должно совпадать с уже существующими правилами равенства во время выполнения;
  - поиск должен использовать логические индексы текущего массива, а не смещения от нуля;
  - отсутствие совпадения должно быть оформлено единообразно и явно зафиксировано в спецификации перед реализацией.

### Обновление 2026-04-20 - Договор поискового пакета уточнён
- Подтверждено по текущей спецификации, справке, рантайму и тестам, что одномерные массивы Axiom уже поддерживают произвольную нижнюю границу, а не только `1`.
- Из-за этого фиксированные специальные значения вроде `0` или `-1` нельзя использовать как универсальный признак отсутствия совпадения в индексных функциях.
- Для поискового пакета выбран договор: `ARRAYCONTAINS` возвращает `FALSE`, `ARRAYCOUNT` возвращает `0`, а `ARRAYINDEXOF` и `ARRAYLASTINDEXOF` при отсутствии совпадения возвращают `LBOUND(array) - 1`.
- До реализации пакета нужно одновременно закрыть крайний случай переполнения для массива с нижней границей на минимуме диапазона `INTEGER`.
- Отдельно подтверждено, что уже реализованные функции `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTOREVERSED`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYDISTINCT` не зависят от ложного предположения о нижней границе `1`: они либо сохраняют реальные границы исходного массива, либо используют `LowerBound` и `UpperBound` явно и только затем сознательно нормализуют новый результат к `1 TO N`.

### Следующий точный шаг после уточнения поискового договора
- Реализовать пакет `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF` и `ARRAYCOUNT` вместе, а не по одной функции.
- В этом же шаге сразу добавить защиту от переполнения для случая `LBOUND(array) - 1`.
- Закрыть пакет только вместе с автоматическими тестами, запускаемыми `.axm`-примерами, пользовательской справкой и обновлением индекса manual.

### Обновление 2026-04-20 - Поисковый пакет массива реализован
- Реализованы функции `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF` и `ARRAYCOUNT`.
- Пакет работает в логических индексах массива и не использует смещения от нуля как публичный договор результата.
- Для отсутствия совпадения `ARRAYINDEXOF` и `ARRAYLASTINDEXOF` возвращают `LBOUND(array) - 1`.
- Для пустого выделенного массива пакет возвращает согласованные значения "не найдено" без ошибки.
- Невыделенный массив по-прежнему даёт `MB3005`.
- В этом же шаге закрыто ограничение на минимальную нижнюю границу массива: `DIM` и `REDIM` теперь запрещают нижнюю границу `-2147483648` и сообщают `MB3007`.
- Дополнительно приведён к безопасному обход индексов в рантайме для операций массива, чтобы крайние допустимые границы не давали скрытого переполнения счётчиков цикла.

### Следующий точный шаг после поискового пакета массива
- Реализовать `ARRAYFINDALL(array, value)` как следующее прямое продолжение поискового набора.
- Сохранить тот же договор сравнения значений и те же логические индексы текущего массива.
- Результат `ARRAYFINDALL` вернуть как новый компактный массив `INTEGER()` с границами `1 TO N`, а при отсутствии совпадений — как выделенный пустой массив `1 TO 0`.

### Обновление 2026-04-20 - ARRAYFINDALL реализована
- Реализована `ARRAYFINDALL(array, value)` как прямое продолжение поискового набора.
- Функция работает в логических индексах исходного массива и возвращает их как новый компактный массив `INTEGER()`.
- При отсутствии совпадений и для пустого выделенного массива возвращается выделенный пустой массив `1 TO 0`.
- Невыделенный массив по-прежнему даёт `MB3005`.
- В этом же шаге синхронизированы код, targeted tests, запускаемый пример, пользовательская справка, спецификация, план и ворклог.

### Следующий точный шаг после ARRAYFINDALL
- Зафиксировать договор и реализовать `ARRAYBINARYSEARCH(array, value)` как отдельный завершающий read-only подпакет этапа поиска.
- До кода явно описать, что бинарный поиск требует уже отсортированный массив и какой именно договор "не найдено" он должен разделять с обычным поиском.

### Обновление 2026-04-20 - ARRAYBINARYSEARCH реализована
- Реализована `ARRAYBINARYSEARCH(array, value)` как отдельный завершающий read-only подпакет этапа поиска по одномерным массивам.
- Функция работает только с уже выделенными одномерными массивами числовых и строковых типов.
- Зафиксирован договор: исходный массив должен быть заранее отсортирован по неубыванию; при совпадении возвращается первый логический индекс, при отсутствии совпадения — `LBOUND(array) - 1`.
- Для discoverability синхронизированы manual, sample и алфавитный каталог `reference.html` через `docs/manual/scripts/manual.js`.

### Следующий точный шаг после ARRAYBINARYSEARCH
- Зафиксировать договор сортировки и реализовать `ARRAYTOSORTED(array)` как прямой producer для уже введённого `ARRAYBINARYSEARCH`.
- До кода явно описать: какие типы поддерживаются, сохраняется ли исходный массив, какие границы получает результат, как обрабатываются дубликаты и какой порядок сравнения используется для строк.

### Обновление 2026-04-20 - ARRAYTOSORTED реализована
- Реализована `ARRAYTOSORTED(array)` как чистая сортирующая функция над одномерным массивом.
- Зафиксирован договор: функция сохраняет текущие границы исходного массива, не меняет сам исходный массив и работает только для числовых и строковых типов элементов.
- Для discoverability синхронизированы manual, sample и алфавитный каталог `reference.html` через `docs/manual/scripts/manual.js`.

### Следующий точный шаг после ARRAYTOSORTED
- Перейти к этапу mutating-операций и зафиксировать договор для `ARRAYFILL(array, value)` как самого базового изменяющего built-in над одномерным массивом.
- До кода явно описать: мутирует ли функция уже выделенный массив на месте, как ведёт себя с пустым выделенным массивом, какой договор у невыделенного массива и как значение привязывается к типу элемента.

### Обновление 2026-04-20 - mutating transform-пакет массива реализован
- Реализованы процедуры `ARRAYFILL(array, value)`, `ARRAYREVERSE(array)` и `ARRAYSORT(array)` как первый согласованный mutating-блок одномерных массивов.
- Зафиксирован общий договор блока: операции меняют уже выделенный массив на месте, не меняют его границы, допускают пустой выделенный массив `1 TO 0` и сообщают `MB3005` для невыделенного массива.
- Для `ARRAYSORT` отдельно закреплено ограничение на числовые и строковые массивы; для `ARRAYFILL` значение привязывается к типу элемента массива.

### Следующий точный шаг после mutating transform-пакета
- Зафиксировать договор и реализовать следующий mutating-блок `ARRAYPUSH(array, value)` и `ARRAYPOP(array)`.
- До кода явно описать: как именно расширяется или сужается диапазон массива, что возвращает `ARRAYPOP`, что происходит на пустом выделенном массиве и как пакет взаимодействует с произвольной нижней границей.

### Обновление 2026-04-20 - mutating push/pop-блок массива реализован
- Реализованы `ARRAYPUSH(array, value)` и `ARRAYPOP(array)` как следующий mutating-блок одномерных массивов.
- `ARRAYPUSH` работает по месту, расширяет массив вправо, сохраняет текущую нижнюю границу для непустого массива и допускает канонический пустой массив `1 TO 0`.
- `ARRAYPOP` возвращает последний элемент, сужает массив по месту и при полном опустошении нормализует результат к выделенному пустому массиву `1 TO 0`.
- Для невыделенного массива обе операции сообщают `MB3005`; для `ARRAYPOP` на пустом выделенном массиве и для переполнения границы расширения используется `MB3007`.
- В этом же шаге синхронизированы targeted tests, runnable samples, manual, manual index и спецификация.

### Следующий точный шаг после mutating push/pop-блока
- Зафиксировать договор и реализовать indexed mutating-блок `ARRAYINSERT(array, index, value)` и `ARRAYREMOVE(array, index)`.
- До кода явно описать: используются ли логические индексы, как именно сдвигаются элементы, как меняются границы массива и как пакет ведёт себя при переходе к пустому массиву.

### Обновление 2026-04-20 - indexed mutating-блок массива реализован
- Реализованы `ARRAYINSERT(array, index, value)` и `ARRAYREMOVE(array, index)` как следующий mutating-блок одномерных массивов.
- Зафиксирован договор логических индексов: обе операции работают в текущих логических границах массива, а не в смещениях от нуля.
- `ARRAYINSERT` мутирует массив на месте, допускает вставку внутрь диапазона и по позиции сразу после текущего хвоста, сохраняет нижнюю границу непустого массива и расширяет верхнюю границу на `1`.
- `ARRAYREMOVE` возвращает удалённый элемент, мутирует массив на месте, сдвигает хвост влево и при удалении единственного элемента нормализует массив к канонической пустой форме `1 TO 0`.
- Для невыделенного массива обе операции сообщают `MB3005`; для недопустимого логического индекса, удаления из пустого выделенного массива и переполнения правой границы используется `MB3007`.
- В этом же шаге синхронизированы targeted tests, runnable samples, manual, manual index, specification, architecture и IDE highlighting.

### Следующий точный шаг после indexed mutating-блока
- Перейти к этапу higher-order массивных функций и до кода зафиксировать минимальный первый срез без расширения объёма этапа.
- В качестве следующей проектной развилки выбрать и описать стартовый подпакет внутри уже утверждённого этапа 7 так, чтобы он опирался на существующую модель массива-значения и не тянул за собой новые синтаксические контракты.

### Обновление 2026-04-20 - predicate higher-order-подпакет реализован
- В качестве минимального стартового среза этапа 7 выбран и реализован predicate-подпакет `ARRAYEVERY(array, predicate)` и `ARRAYSOME(array, predicate)`.
- Подпакет опирается только на уже существующие значения функций, функциональные типы и замыкания Axiom; новых синтаксических контрактов не добавлено.
- Оба built-in принимают callback только в форме `FUNCTION(T) AS BOOLEAN`, где `T` совпадает с типом элемента исходного массива.
- `ARRAYEVERY` и `ARRAYSOME` не меняют массив, работают в логических индексах текущего диапазона и фиксируют стартовый runtime/binder-механизм higher-order-вызова по массиву без перехода к более широкому API этапа 7.

### Следующий точный шаг после predicate higher-order-подпакета
- Перейти к следующему pure higher-order-срезу и зафиксировать контракт `ARRAYFILTER(array, predicate)` как первого producer built-in поверх уже введённого predicate-механизма.
- До кода явно описать: остаётся ли callback в форме `FUNCTION(T) AS BOOLEAN`, получает ли результат компактные границы `1 TO N`, как ведёт себя пустой выделенный массив `1 TO 0` и как этот шаг соотносится с уже отложенными `ARRAYMAP` и `ARRAYREDUCE`.

### Обновление 2026-04-20 - договор ARRAYFILTER зафиксирован
- Контракт `ARRAYFILTER(array, predicate)` зафиксирован в specification и architecture как следующий producer higher-order built-in.
- Подтверждён минимальный callback-контракт `FUNCTION(T) AS BOOLEAN` без индекса и без передачи самого массива.
- Подтверждён compact-result contract: непустой результат возвращается как `1 TO N`, пустой результат — как канонический выделенный массив `1 TO 0`.

### Следующий точный шаг после фиксации договора ARRAYFILTER
- Реализовать `ARRAYFILTER(array, predicate)` в semantics/runtime/manual/tests и добавить runnable acceptance sample.
- В том же шаге не размораживать `ARRAYMAP` и `ARRAYREDUCE`; оставить scope ограниченным первым producer built-in поверх уже введённого predicate-механизма.

### Обновление 2026-04-20 - ARRAYFILTER реализован
- Реализован `ARRAYFILTER(array, predicate)` как первый producer higher-order built-in поверх уже введённого predicate-механизма.
- Подтверждён итоговый контракт шага: callback остаётся в форме `FUNCTION(T) AS BOOLEAN`, исходный массив не мутируется, результат нормализуется к `1 TO N` или `1 TO 0`.
- В одном шаге синхронизированы semantics, runtime, IDE highlighting, tests, manual, manual index, acceptance sample и repository docs.

### Следующий точный шаг после ARRAYFILTER
- До кода зафиксировать договор `ARRAYMAP(array, selector)` и явно решить, будет ли первый вариант поддерживать только преобразование `T -> T` или потребует нового механизма вывода/result-type для `T -> U`.
- После фиксации договора реализовать `ARRAYMAP` так, чтобы не расширить незаметно scope этапа до полного polymorphic higher-order API и не смешать этот шаг с `ARRAYREDUCE`.

### Обновление 2026-04-20 - договор same-type ARRAYMAP зафиксирован
- Для первого варианта `ARRAYMAP(array, selector)` выбран и зафиксирован минимальный договор `FUNCTION(T) AS T`.
- Подтверждено, что первый ввод `ARRAYMAP` не включает polymorphic-преобразование `T -> U` и не требует нового result-type machinery.
- Подтверждено, что результат `ARRAYMAP` должен быть compact producer-результатом `1 TO N` или `1 TO 0`.

### Следующий точный шаг после фиксации same-type ARRAYMAP
- Реализовать `ARRAYMAP(array, selector)` в semantics/runtime/manual/tests и добавить runnable acceptance sample.
- В этом шаге не расширять договор до `T -> U` и не смешивать реализацию `ARRAYMAP` с `ARRAYREDUCE`.

### Обновление 2026-04-20 - ARRAYMAP реализован
- Реализован `ARRAYMAP(array, selector)` как same-type higher-order producer built-in.
- Подтверждён итоговый контракт шага: `selector` остаётся в форме `FUNCTION(T) AS T`, исходный массив не мутируется, результат нормализуется к `1 TO N` или `1 TO 0`.
- В одном шаге синхронизированы semantics, runtime, IDE highlighting, tests, manual, manual index, acceptance sample и repository docs.

### Следующий точный шаг после ARRAYMAP
- До кода зафиксировать договор `ARRAYREDUCE(array, seed, reducer)` как следующего higher-order built-in и отдельно описать минимальный accumulator-контракт без скрытого расширения в более общий polymorphic API этапа 7.
- После фиксации договора реализовать `ARRAYREDUCE`, не расширяя в том же изменении `ARRAYMAP` до `T -> U`.

### Обновление 2026-04-20 - договор same-type ARRAYREDUCE зафиксирован
- Для первого варианта `ARRAYREDUCE(array, seed, reducer)` выбран и зафиксирован минимальный accumulator-контракт `seed: T`, `reducer: FUNCTION(T, T) AS T`.
- Подтверждено, что первый ввод `ARRAYREDUCE` не включает polymorphic accumulator `A + T -> A` и не требует нового result-type machinery.
- Подтверждено, что для пустого выделенного массива `ARRAYREDUCE` должен возвращать исходный `seed`.

### Следующий точный шаг после фиксации same-type ARRAYREDUCE
- Реализовать `ARRAYREDUCE(array, seed, reducer)` в semantics/runtime/manual/tests и добавить runnable acceptance sample.
- В этом шаге не расширять `ARRAYREDUCE` до polymorphic accumulator-контракта и не расширять `ARRAYMAP` до `T -> U`.

### Обновление 2026-04-20 - ARRAYREDUCE реализован
- Реализован `ARRAYREDUCE(array, seed, reducer)` как minimal same-type accumulator built-in.
- Подтверждён итоговый контракт шага: `seed` остаётся типа `T`, `reducer` остаётся в форме `FUNCTION(T, T) AS T`, исходный массив не мутируется, пустой выделенный массив возвращает `seed`.
- В одном шаге синхронизированы semantics, runtime, IDE highlighting, tests, manual, manual index, acceptance sample и repository docs.

### Следующий точный шаг после ARRAYREDUCE
- До кода отдельно зафиксировать, нужен ли этап 2 для polymorphic higher-order built-ins: `ARRAYMAP(array, selector)` с `FUNCTION(T) AS U` и `ARRAYREDUCE(array, seed, reducer)` с `FUNCTION(A, T) AS A`.
- Если такой этап нужен, сначала описать result-type и accumulator-type machinery в specification и architecture, не смешивая это решение с уже завершённым minimal same-type пакетом higher-order массивов.

### Обновление 2026-04-20 - minimal higher-order пакет закрыт
- Подтверждено, что в рамках minimal stable пакета higher-order массивов незакрытых same-type задач не осталось.
- Стабильный пакет считается завершённым в составе `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE`.
- Polymorphic higher-order сознательно не входит в текущий пакет и не должен размораживаться без отдельной фиксации этапа 2.

### Следующий точный шаг после фиксации закрытого minimal higher-order пакета
- Сохранить polymorphic higher-order как отложенный отдельный этап: `ARRAYMAP` с `T -> U` и `ARRAYREDUCE` с `A + T -> A`.
- Не возвращаться к этой теме в коде до отдельной документной фиксации этапа 2 в specification и architecture.

## Обновление плана - 2026-04-20

1. Follow-up по расширению `Axiom.Ide.SmokeHarness` закрыт:
   - automated WPF smoke теперь покрывает context-menu/file-operations сценарии `File Manager`: `Show hidden files`, `New File`, `New Directory`, `Rename`, `Copy/Paste`, `Cut/Paste`, `Delete`;
   - успешность delete-сценария фиксируется по реальному изменению дерева и workspace, а не по жёсткому требованию исчезновения системного confirmation dialog из UI Automation.
2. Текущий IDE baseline считать достаточным для `IDE Redesign (v0.2)`:
   - `Phase 9` и его file-manager follow-up закрыты полностью на automated harness baseline;
   - отдельный дополнительный slice на это же context-menu/file-operations покрытие больше не нужен.
3. Следующий практический IDE шаг:
   - идти в post-v0.2 polish / cleanup;
   - не расширять redesign обратно в language/runtime scope без нового документированного решения.

## Обновление плана - 2026-04-20

1. Post-v0.2 polish продвинут ещё на один shell-level шаг:
   - placeholder-пункт `Help -> About` переведён в рабочую команду оболочки;
   - для команды зафиксированы regression checks на уровне `MainWindowViewModel` и `MainWindow.xaml`.
2. В post-v0.2 cleanup остаётся не расширение текущего smoke harness, а добор оставшихся shell-polish задач:
   - решить, нужен ли отдельный `Help -> Open Manual` entry point в IDE;
   - либо сосредоточиться на мелких shell bugfix/polish slice без возврата в большой redesign scope.
3. Следующий практический шаг:
   - либо добавить IDE-side вход в HTML manual из меню `Help`;
   - либо закрывать найденные мелкие UX/command placeholder хвосты тем же маленьким regression-first способом.

## Обновление плана - 2026-04-20

1. Regex-ветка `Find/Replace` теперь закрыта отдельным regression/smoke slice:
   - в `Axiom.Ide.Tests` добавлены engine-тесты на regex-find, invalid pattern, regex-replace и regex-replace-all;
   - `Axiom.Ide.SmokeHarness` теперь прожимает `Regex` в реальном `FindReplaceDialog` и подтверждает end-to-end замену по regex-паттерну.
2. Для live-smoke зафиксирован практический обход среды:
   - harness умеет принимать `--ide-exe <path>` и может гоняться по изолированной WPF build-папке, если стандартный `Axiom.Ide.exe` заблокирован другим процессом.
3. Следующий практический шаг после этого regex slice не меняется по сути:
   - либо добавить `Help -> Open Manual`;
   - либо брать следующий маленький shell polish / UX bugfix с таким же regression-first подходом.

## Обновление плана - 2026-04-20

1. Shell polish по меню `Help` продвинут дальше:
   - реализован `Help -> Open Manual`, открывающий локальный `docs/manual/index.html` во внешнем приложении по умолчанию;
   - добавлены regression tests на успешный path resolution, открытие manual через shell-navigation service и понятную ошибку при отсутствии manual.
2. Текущий хвост по `Help` теперь закрыт в минимально достаточном виде:
   - `About` работает как info-dialog;
   - `Open Manual` работает как entry point в существующий HTML manual.
3. Следующий практический IDE шаг:
   - брать следующий маленький shell/UX polish или bugfix slice вне уже закрытого `Help` baseline;
   - не расширять это изменение в большой встроенный help-viewer без отдельного документированного решения.

## Обновление плана - 2026-04-20

1. По прямому продуктовому запросу открыт отдельный language-design slice по пакету даты и времени:
   - историческая база изучена и зафиксирована по линии classic Microsoft BASIC (`DATE$`, `TIME$`, `TIMER`) и более поздней Visual Basic-family (`NOW`, `DATEADD`, `DATEDIFF`, `DATEPART`, `DATESERIAL`, `TIMESERIAL`, `DATEVALUE`, `TIMEVALUE`, `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `ISDATE`);
   - для Axiom выбран не literal-совместимый перенос, а suffixless canonical API без host-clock mutation statements.
2. Зафиксирована архитектурная граница первого поколения:
   - без нового scalar-типа даты/времени на первом этапе;
   - с каноническими строковыми форматами `yyyy-MM-dd`, `HH:mm:ss`, `yyyy-MM-dd HH:mm:ss`;
   - без locale-dependent parsing по настройкам ОС.
3. Следующий практический language шаг:
   - реализовать Этап 1 пакета даты и времени: `DATE()`, `TIME()`, `NOW()`, `TIMER()`;
   - в том же шаге добавить automated tests, runnable `.axm` acceptance samples и user-facing manual.
4. Следующий шаг после Этапа 1:
   - зафиксировать и реализовать Этап 2: `DATESERIAL`, `TIMESERIAL`, `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`;
   - отдельно перед кодом зафиксировать точный contract `WEEKDAY`, а не придумывать его локально в runtime.
5. Отдельно от первых двух этапов оставить более широкий arithmetic/validation слой:
   - `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE`;
   - modern helpers вроде `ISLEAPYEAR` и `DAYSINMONTH`, если они подтвердятся как реальные потребности после базового пакета.

## Обновление плана - 2026-04-20

1. Stage 1 пакета даты и времени закрыт как implementation slice:
   - в язык добавлены DATE(), TIME(), NOW(), TIMER();
   - добавлены binder/runtime/acceptance tests, runnable .axm samples и user-facing manual page docs/manual/date-time-functions.html;
   - manual index и навигация обновлены, чтобы новые built-ins находились через manual search и reference routes.
2. Следующий практический language step:
   - перед кодом отдельно зафиксировать точный contract WEEKDAY;
   - затем реализовать Stage 2: DATESERIAL, TIMESERIAL, YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY.
3. Постоянные требования для Stage 2 не меняются:
   - string-first/canonical formatting contract остаётся базой;
   - каждый новый built-in сопровождается automated tests, runnable acceptance sample в samples/functions/<name>/ и обновлением user-facing manual.
## Обновление плана - 2026-04-20

1. Второй этап пакета даты и времени закрыт как отдельный функциональный шаг:
   - реализованы DATESERIAL, TIMESERIAL, YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY;
   - добавлены проверки семантики, проверки исполнения, запускаемые примеры .axm и обновление пользовательской справки.
2. Итоговый договор второго этапа зафиксирован так:
   - строки даты и времени остаются каноническими и не зависят от локали;
   - DATESERIAL и TIMESERIAL строго проверяют диапазоны и календарную корректность вместо автоматического переноса переполненных компонентов;
   - WEEKDAY использует схему 1 = понедельник, 7 = воскресенье и не имеет второго параметра.
3. Следующий практический шаг по языку:
   - зафиксировать отдельный договор арифметики и явного разбора даты и времени;
   - затем реализовать следующий слой: DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE.
## Обновление плана - 2026-04-21

1. Для ускорения agent re-entry и локальной работы поверх текущего document-driven процесса добавлен минимальный retrieval layer в docs/agent/:
   - README.md как правило использования слоя;
   - CURRENT_STATE.md как короткий project snapshot;
   - ARCHITECTURE_MAP.md как карта hotspots и test ownership;
   - PLAYBOOKS.md как короткие repeatable workflows.
2. Этот слой не меняет источник истины по проекту:
   - основными документами по-прежнему остаются AGENTS.md, PROJECT.md, docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md, docs/WORKLOG2.md и docs/manual/.
3. Следующий продуктовый шаг не меняется:
   - перед кодом отдельно зафиксировать договор для следующего слоя date/time built-ins: DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE.
## Обновление плана - 2026-04-21

1. Следующий шаг по пакету даты и времени зафиксирован как отдельный этап арифметики и нормализации:
   - DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE.
2. Для этого шага выбран минимальный договор, совместимый с Axiom:
   - только канонический разбор;
   - без необязательных аргументов в стиле VBA;
   - без нового скалярного типа DATE/TIME/DATETIME;
   - с ограниченным и явно документированным набором обозначений интервалов и частей даты.
3. Практический следующий шаг в коде:
   - реализовать семантику, выполнение, тесты, справку и примеры для этого этапа в одном согласованном изменении.
4. Следующий шаг после этого этапа:
   - отдельно решить, нужен ли следующий вспомогательный набор функций вроде WEEKDAYNAME, ISLEAPYEAR, DAYSINMONTH, или текущий пакет даты и времени уже достаточно завершён для ближайшего этапа.
## Обновление плана - 2026-04-21

1. Слой арифметики и нормализации пакета даты и времени закрыт как отдельный этап реализации:
   - реализованы DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE;
   - добавлены проверки связывания, проверки выполнения, приёмочные тесты, запускаемые примеры в samples/functions/<name>/ и обновление пользовательской справки.
2. Текущий стабильный договор по дате и времени теперь включает три этапа:
   - Этап 1: DATE, TIME, NOW, TIMER;
   - Этап 2: DATESERIAL, TIMESERIAL, YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY;
   - этап арифметики и нормализации: DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE.
3. Следующий практический шаг по этому направлению:
   - отдельно решить, нужен ли следующий вспомогательный набор функций вроде WEEKDAYNAME, ISLEAPYEAR, DAYSINMONTH;
   - либо считать текущий пакет даты и времени достаточно завершённым и вернуться к следующему приоритетному языковому этапу.
## Обновление плана - 2026-04-21

1. Пакет текстовых имён поверх текущего слоя даты и времени закрыт как отдельный маленький шаг:
   - реализованы `MONTHNAME` и `WEEKDAYNAME`;
   - добавлены проверки связывания, проверки выполнения, приёмочные примеры и обновление пользовательской справки с указателем.
2. Текущий стабильный договор по дате и времени теперь включает не только числовые извлекатели, но и фиксированные текстовые имена:
   - `MONTHNAME` принимает номер месяца `1..12` и возвращает полное английское имя месяца;
   - `WEEKDAYNAME` принимает номер дня недели `1..7` по схеме от понедельника и возвращает полное английское имя дня недели.
3. Следующий шаг по этому направлению теперь снова является развилкой решения, а не незавершённой реализацией:
   - либо считать пакет даты и времени достаточно завершённым на текущем этапе;
   - либо отдельно зафиксировать и реализовать ещё один дополнительный набор вроде `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`.
## Обновление плана - 2026-04-21

1. Календарный пакет свойств и границ закрыт как отдельный функциональный шаг:
   - реализованы `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR`;
   - добавлены проверки связывания, проверки выполнения, приёмочные примеры, пользовательская справка и указатель справки.
2. Текущий стабильный договор по дате и времени теперь покрывает уже не только текущее время, разбор частей и арифметику, но и прикладную календарную работу:
   - календарные свойства года и месяца;
   - календарные границы месяца и года.
3. Следующий крупный шаг по современному направлению даты и времени уже можно брать не как мелкий helper-пакет, а как отдельный этап возможного изменения модели:
   - UTC и явные преобразования локального времени;
   - часовые пояса и смещения;
   - дробные секунды;
   - длительности и, при необходимости, более сильное внутреннее представление даты и времени, чем простая строка.
## Обновление плана - 2026-04-21

1. UTC-пакет даты и времени закрыт как отдельный функциональный шаг:
   - реализованы `UTCDATE`, `UTCTIME`, `UTCNOW`, `UTCOFFSET`, `TOUTC`, `FROMUTC`;
   - добавлены проверки связывания, проверки выполнения, приёмочные примеры, пользовательская справка и указатель справки.
2. Текущий стабильный договор по дате и времени теперь дополнительно покрывает:
   - чтение текущих значений в UTC;
   - явный перевод локальной канонической даты-времени в UTC и обратно;
   - получение текущего локального смещения от UTC в минутах.
3. Следующий крупный шаг по этому направлению:
   - перейти от текущего смещения хоста к полноценной модели смещений по конкретной дате и именованных часовых поясов;
   - затем отдельно добрать доли секунды и длительности.
## Обновление плана - 2026-04-21

1. Шаг смещений по конкретной дате закрыт как отдельное усиление пакета даты и времени:
   - `UTCOFFSET` получил форму с аргументом для канонической локальной даты-времени;
   - `TOUTC` и `FROMUTC` переведены с текущего смещения хоста на правила локального часового пояса для конкретной даты.
2. Текущий стабильный договор теперь дополнительно покрывает:
   - вычисление локального смещения по конкретной дате-времени;
   - корректный учёт сезонных переходов локального времени;
   - явную диагностику для пропущенных и неоднозначных локальных моментов.
3. Следующий крупный шаг по этому направлению:
   - вынести именованные часовые пояса и явный выбор пояса в отдельный пользовательский договор;
   - затем отдельно добрать доли секунды и длительности.
## Обновление плана - 2026-04-21

1. Шаг именованных часовых поясов закрыт как отдельный пользовательский пакет:
   - реализованы формы `DATE(<пояс>)`, `TIME(<пояс>)`, `NOW(<пояс>)`;
   - реализованы формы `UTCOFFSET(<значение>, <пояс>)`, `TOUTC(<значение>, <пояс>)`, `FROMUTC(<значение>, <пояс>)`;
   - добавлены проверки связывания, проверки выполнения, запускаемые примеры, пользовательская справка и указатель справки.
2. Текущий стабильный договор по дате и времени теперь дополнительно покрывает:
   - чтение текущих даты и времени в произвольно выбранном часовом поясе;
   - явный перевод между UTC и конкретным именованным поясом;
   - расчёт смещения по конкретной дате-времени не только для локального, но и для явно заданного пояса.
3. Следующий крупный шаг по этому направлению:
   - отдельно добрать доли секунды и длительности;
   - при необходимости затем вернуться к усилению внутренней модели даты и времени, если для этого шага уже перестанет хватать string-first договора.


## Обновление плана - 2026-04-21

1. Исправлен проверочный пример `samples/smoke/repair_plans_mojibake.axm`:
   - параметры `ResolveSourcePath` и `ResolveWorkPath` больше не повторяют имя внешней переменной `documentKey`;
   - правило языка о запрете затенения внешних имён не менялось.
2. Автоматические проверки примера усилены:
   - тесты теперь явно выставляют нужный `documentKey` для `PLANS`, `SPECIFICATION` и `ARCHITECTURE`;
   - проверки больше не зависят от того, какой документ выбран в самом `.axm`-примере для ручного запуска.
3. Следующий содержательный шаг остаётся прежним:
   - отдельно добрать доли секунды и длительности в пакете даты и времени;
   - при необходимости затем вернуться к усилению внутренней модели даты и времени.

## Обновление плана - 2026-04-21

1. Проверочный пример `samples/smoke/repair_plans_mojibake.axm` переведён на прямой ввод путей:
   - пользователь вручную задаёт путь к исходному файлу с битой кодировкой;
   - пользователь вручную задаёт путь к рабочей копии, в которой выполняется исправление.
2. Старый выбор через `documentKey` и функции `ResolveSourcePath` / `ResolveWorkPath` больше не используется.
3. Следующий содержательный шаг проекта остаётся прежним:
   - отдельно добрать доли секунды и длительности в пакете даты и времени;
   - при необходимости затем вернуться к усилению внутренней модели даты и времени.
## Обновление плана - 2026-04-21

1. В проверочном примере `samples/smoke/repair_plans_mojibake.axm` имена переменных путей приведены к ASCII:
   - `sourcePath` — путь к исходному файлу с битой кодировкой;
   - `workPath` — путь к рабочей копии.
2. Русские имена переменных удалены из примера и из тестовой подстановки.
3. Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-21

1. Проверочный пример `samples/smoke/repair_plans_mojibake.axm` дополнительно защищён от ложного распознавания уже нормального русского UTF-8 текста:
   - `LooksBroken` теперь ищет полноценную UTF-8 последовательность после восстановления байтов Windows-1251;
   - обычные слова с сочетаниями вроде `вёрстки`, `четвёртая`, `живёт` больше не считаются битой кодировкой.
2. Автоматическая проверка примера усилена:
   - добавлен тест, который запускает пример на уже исправленном русском тексте;
   - рабочая копия в этом случае должна остаться без изменений, а счётчик исправленных строк должен быть `0`.
3. Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-21

1. Для функций `READFILE` и `READALLLINES` добавлено резервное чтение Windows-1251 при недопустимых UTF-8 байтах:
   - это закрывает практический случай старых русскоязычных документов, где обычное UTF-8 чтение превращало байты в `пїЅ`;
   - `WRITEFILE` и `APPENDFILE` продолжают записывать UTF-8 без BOM.
2. Проверочный пример `samples/smoke/repair_plans_mojibake.axm` приведён ближе к стилю Axiom:
   - рабочие переменные больше не объявляются внутри циклов;
   - переменные цикла используются только для прохода по индексам, а временные байты и строки объявлены в начале соответствующих функций.
3. Добавлены проверки и ручной пример:
   - автоматический тест на `READFILE` с файлом Windows-1251;
   - автоматический тест smoke-примера на смешанных байтах UTF-8 и Windows-1251;
   - ручной пример `samples/functions/readfile/windows1251_fallback.axm`.
4. Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-21

1. Закрыт отдельный шаг по визуальной оболочке IDE:
   - добавлена переключаемая тёмная тема через `Вид -> Тема`;
   - выбранная тема сохраняется в состоянии оболочки;
   - основное меню, контекстное меню редактора, заголовки нижней панели и заголовки таблицы диагностик переведены на русский язык.
2. Подсветка синтаксиса синхронизирована с текущим набором встроенных функций даты и времени, чтобы форматирование IDE и тесты словаря подсветки не отставали от языка.
3. Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени; при необходимости затем вернуться к усилению внутренней модели даты и времени.
## Обновление плана - 2026-04-21

1. Закрыт текущий проход по доводке оболочки IDE:
   - в `Вид` появился отдельный пункт переключения языка `Русский / English`;
   - тёмная тема доведена для меню, нижней панели, диагностик, диалога поиска/замены, диалога текстового ввода и файлового дерева;
   - раскрыватели папок в диспетчере файлов получили собственный шаблон и больше не зависят от светлой системной темы.
2. Ближайший следующий шаг по оболочке IDE:
   - добрать полную локализацию служебных сообщений и заменить оставшиеся системные `MessageBox`/стандартные окна на диалоги оболочки, если пользователь продолжит работу именно по IDE.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени;
   - затем при необходимости вернуться к усилению внутренней модели даты и времени.
## Обновление плана - 2026-04-21

1. Закрыт ещё один проход по визуальной доводке IDE:
   - выпадающие меню и контекстные меню переведены на собственные шаблоны, чтобы в тёмной теме не проступали светлые состояния WPF;
   - горизонтальные разделители меню теперь тянутся на всю ширину без левого отступа;
   - путь активного документа в строке состояния получил двойной щелчок для полного выделения и собственное контекстное меню `Копировать / Выделить всё`.
2. Для строки состояния отдельно скорректирован цвет выделения пути:
   - выделение теперь остаётся контрастным относительно синей строки состояния;
   - текст внутри выделения должен оставаться читаемым в тёмной теме.
3. Ближайший следующий шаг по IDE не меняется:
   - добрать полную локализацию служебных сообщений и заменить оставшиеся системные `MessageBox`/стандартные окна на диалоги оболочки.

## Обновление плана - 2026-04-21

1. Закрыт ещё один точечный проход по тёмной теме IDE:
   - разделители во всплывающих и контекстных меню теперь компенсируют внутренний отступ popup и должны идти на всю ширину меню;
   - поле пути в строке состояния теперь задаёт собственные кисти текста и фона для активного и неактивного выделения, чтобы выделенный путь оставался читаемым.
2. Ближайший следующий шаг по IDE остаётся прежним:
   - добрать полную локализацию служебных сообщений и заменить оставшиеся системные `MessageBox`/стандартные окна на диалоги оболочки.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Закрыт ещё один проход по доводке оболочки IDE:
   - во всплывающих и контекстных меню восстановлена классическая левая колонка значков с вертикальной линией после неё;
   - горизонтальные разделители меню теперь начинаются от линии колонки значков, а не от самого левого края popup-контейнера.
2. Ближайший следующий шаг по IDE:
   - визуально добрать оставшиеся мелкие несоответствия оболочки после этой перестройки меню, если пользователь продолжит работу именно по интерфейсу IDE.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Уточнён текущий проход по доводке оболочки IDE:
   - убран лишний внутренний отступ popup-контейнеров меню;
   - геометрия колонки значков и разделителей меню приближена к классическому виду старого IDE.
2. Ближайший следующий шаг по IDE:
   - если после этой правки останутся расхождения со старым видом, заменить текущую самодельную геометрию меню на полный стандартный шаблон `MenuItem` WPF с адаптацией только цветов.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Ещё раз скорректирована геометрия меню IDE:
   - вертикальный разделитель вынесен из popup-контейнера в шаблон самого пункта меню;
   - popup-контейнеры упрощены до рамки и фона списка.
2. Ближайший следующий шаг по IDE:
   - визуально сверить новый вариант со старым IDE; если и он не совпадёт, перейти уже к полному копированию стандартного шаблона `MenuItem` WPF без промежуточных упрощений.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Подтверждён референс по старому IDE:
   - старое меню держалось на стандартном шаблоне WPF, а не на кастомном `ControlTemplate`;
   - активные стили текущего IDE возвращены к той же схеме.
2. Ближайший следующий шаг по IDE:
   - визуально проверить, что меню теперь действительно совпадает со старым вариантом по линиям и разделителям; если останутся отличия, искать их уже не в шаблонах меню, а в системных кистях и theme-ресурсах WPF.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Закрыт ещё один дефект визуальной оболочки IDE:
   - из всплывающих меню убраны лишние кнопки прокрутки сверху и снизу, появившиеся из-за переопределённого `MenuScrollViewer`.
2. Ближайший следующий шаг по IDE:
   - визуально проверить меню после удаления прокрутчика и при необходимости добрать уже только реальные расхождения со старым IDE.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Закрыт крупный остаток тёмной темы IDE:
   - активные стили выпадающих меню верхней панели переведены на тёмную шаблонную ветку оболочки;
   - меню верхней панели и контекстные меню теперь должны использовать одну палитру вместо смешения тёмной оболочки со светлой темой WPF.
2. Ближайший следующий шаг по IDE:
   - визуально проверить тёмную тему после этой правки и добрать только оставшиеся единичные светлые артефакты, если они ещё проявятся.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Исправлена ещё одна системная причина проблем тёмной темы IDE:
   - шаблоны меню больше не используют промежуточные кисти из `ShellStyles.xaml`, которые могли удерживать светлые значения;
   - меню переведены на прямые `DynamicResource`-ссылки к активной палитре темы.
2. Ближайший следующий шаг по IDE:
   - визуально проверить тёмную тему после этой правки и добрать только отдельные артефакты, если они ещё останутся.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Закрыт ещё один проход по оболочке IDE:
   - начальная привязка текста AvalonEdit больше не должна помечать только что открытый документ как изменённый;
   - в строке состояния выделение пути должно сохранять белый текст;
   - полосы прокрутки переведены на ресурсы активной темы.
2. Ближайший следующий шаг по IDE:
   - вручную перепроверить в работающей IDE два сценария: отсутствие ложной `*` сразу после открытия файла и итоговый вид вертикальных/горизонтальных полос прокрутки в тёмной теме.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Уточнён и закрыт источник ложного dirty-state:
   - автоформатирование больше не выполняется просто при активации или открытии вкладки;
   - белый цвет текста для поля пути в строке состояния теперь задаётся явно при фокусе, а не только через системные кисти выделения.
2. Ближайший следующий шаг по IDE:
   - вручную проверить именно пользовательский сценарий из IDE: открыть существующий `.axm`, убедиться в отсутствии `*`, затем выделить путь в строке состояния и проверить читаемость текста при выделении.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Добит ещё один точечный проход по оболочке IDE:
   - активное выделение пути в строке состояния теперь должно иметь белый фон и голубой текст;
   - для полос прокрутки тёмной темы убрана отдельная тёмная палитра и возвращены светлые цвета.
2. Ближайший следующий шаг по IDE:
   - вручную перепроверить только два визуальных сценария: читаемость активного выделения пути и устраивающий вид полос прокрутки после возврата светлой палитры.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Исправлен регресс по полосам прокрутки:
   - кастомный шаблон `ScrollBar` удалён;
   - оболочка вернулась к штатным WPF-скроллам по референсу из `E:\basic\src\Axiom.Ide`.
2. Ближайший следующий шаг по IDE:
   - вручную проверить, что в обеих темах снова работают верхняя и нижняя кнопки прокрутки, клики по дорожке и перетаскивание бегунка.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Исправлен ещё один визуальный регресс в IDE:
   - в светлой теме панель `Diagnostics` снова показывает читаемый текст в выделенной строке;
   - светлая и тёмная темы получили отдельные кисти выделения для `Diagnostics`, без перекраски всей светлой палитры.
2. Ближайший следующий шаг по IDE:
   - вручную перепроверить в запущенной IDE два сценария: читаемость выделенной строки `Diagnostics` в светлой теме и отсутствие новых визуальных расхождений в тёмной теме.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Исправлен ещё один визуальный регресс оболочки IDE:
   - выбранные вкладки больше не меняют цвет только из-за того, что курсор мыши попал в содержимое активной вкладки.
2. Ближайший следующий шаг по IDE:
   - вручную перепроверить обе темы на вкладках документов и на нижней панели `Вывод` / `Диагностика`, чтобы убедиться, что цвет выбранной вкладки теперь стабилен.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.

## Обновление плана - 2026-04-21

1. Исправлен ещё один дефект строки состояния:
   - путь активного файла больше не рисуется через `TextBox`, из-за которого выделение вело себя нестабильно в светлой и тёмной теме;
   - копирование пути сохранено через двойной клик и через контекстное меню.
2. Ближайший следующий шаг по IDE:
   - вручную проверить в обеих темах два сценария: двойной клик по пути копирует полный путь в буфер обмена, пункт контекстного меню `Копировать` делает то же самое.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.
## ?????????? ????? - 2026-04-21
1. ????????? ??? ???? ?????? ???????? IDE:
   - ? ?????? ???? ?????? ?????????? ???? ?????????? ?? ????????? ????? MenuIconBrush, ????? ????? ?????????? ?????? ?? ???????? ????? ?????? ?? ?????? ????;
   - ? ?????? Diagnostics ????????? ??????? ??????????? ????????? ??????????? ?? ???????? ????? ? ????? ??????????? ????.
2. ????????? ????????? ??? ?? IDE:
   - ??????? ????????? ? ????? ????? ??? ????????: ?????? ?????????? ? ??????????? ???? ???????? ??? ??????? ??????????; ????????? ?????? ??????????? ????????? ?????????? ? ????? ? ????????? ????.
3. ????? ????????? ?????????????? ??? ??????? ?? ????????:
   - ???????? ??????? ???? ??????? ? ???????????? ? ?????? ???? ? ???????.## ?????????? ????? - 2026-04-21
1. ????????? ??? ???? ?????? ?????? ???????? IDE:
   - ShellContextMenuStyle ????????? ?? ??????????? ControlTemplate, ????? ??????????? ???? ? ???? ??????????? ? ?????? ????????? ?? ?????? ??????? ????? ????????? ????;
   - ?????????????? ??????????? ???? ?????????? ?? BorderBrush, ????? ?? ??????? ????????? ? ???????????? ?????????????? ??????.
2. ????????? ????????? ??? ?? IDE:
   - ??????? ????????? ? ?????? ???? ??? ????????: ??????????? ??????????? ????, ??????????? ???? ?????? ????????? ? ??????????? ???? ??????????? ?????? ?? ?????????? ????? ???? ? ??????; ?????????????? ??????????? ????????? ????????? ?? ???? ? ???????????? ??????.
3. ????? ????????? ?????????????? ??? ??????? ?? ????????:
   - ???????? ??????? ???? ??????? ? ???????????? ? ?????? ???? ? ???????.## ?????????? ????? - 2026-04-21
1. ?????????? ??? ???? ??????????? ???????? ???????? IDE:
   - ?????? ?????????? ?????????? ???????? ?????? ????? ?????? ShellPopupMenuHostTemplate, ??????? ?????? ???????????? ? ??????????? ????, ? ???????????? ????;
   - ???????????? ????? ? ?????????????? ??????????? ??????? ? ?????? ???????? MenuSeparatorBrush ? MenuGutterSeparatorMargin.
2. ????????? ????????? ??? ?? IDE:
   - ??????? ?????????, ??? ????? ???????? ?? ????? ?????? ??? ???? ???? ????????????? ????????? ?? ?????? gutter-????? ? ?? ???? ???????????? ? ??????? ? ?????? ?????.
3. ????? ????????? ?????????????? ??? ??????? ?? ????????:
   - ???????? ??????? ???? ??????? ? ???????????? ? ?????? ???? ? ???????.## ?????????? ????? - 2026-04-21
1. ?????????? ????????? ?????? popup-??????? ????:
   - ???????????? ????? ? ?????????? ???? ?????????? ????? ?????????? ??????? ????? ? ShellPopupMenuHostTemplate;
   - ???? ???????????? ??-???????? ???????? ?? ????? ??????? MenuSeparatorBrush, ????? ? ???????????? ??????.
2. ????????? ????????? ??? ?? IDE:
   - ??????? ????????? ??? ???? ???? ? ?????? ???? ? ??????????? ??? ????: ???????????? ????? ????? ? ? ????????, ? ? ??????????? ????; ?????????????? ?????????? ????????? ????????? ?? ???? ? ???? ??????.
3. ????? ????????? ?????????????? ??? ??????? ?? ????????:
   - ???????? ??????? ???? ??????? ? ???????????? ? ?????? ???? ? ???????.
## Обновление плана - 2026-04-21

1. Исправлен ещё один визуальный дефект редактора IDE:
   - обычная текущая строка получила отдельные ресурсы для светлой и тёмной темы;
   - подсветка строки ошибки оставлена отдельной и более заметной, без смешения с обычной подсветкой курсора.
2. Ближайшая проверка по IDE:
   - вручную проверить в обеих темах обычную навигацию по строкам и переход к ошибке из Diagnostics, чтобы обычная и диагностическая подсветки выглядели различимо.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-21

1. Исправлен стартовый дефект подсветки строки редактора:
   - обычная подсветка строки больше не рисуется сразу после открытия документа без входа в редактор;
   - она появляется только после получения фокуса редактором, а диагностическая остаётся отдельным режимом.
2. Следующая ручная проверка по IDE:
   - проверить в обеих темах три состояния: только что открытая вкладка без фокуса, обычный вход курсором в редактор, переход к ошибке из Diagnostics.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-21

1. Доведено ещё одно меню оболочки IDE:
   - подтверждено, что файловое контекстное меню использует общий стиль ShellContextMenuStyle;
   - для его пунктов добавлены тематические пиктограммы действий.
2. Следующая ручная проверка по IDE:
   - проверить файловое контекстное меню в светлой и тёмной теме, включая пункт перехода для файла и для каталога, чтобы иконка у первого пункта менялась уместно.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.## ?????????? ????? - 2026-04-21
1. ???????? ????????? ?????????????? ? ???????? ????????? IDE:
   - ??????? ????????????? ?????? ?? ????????? ????????? ??????;
   - ??? ????? ??? ???????? ?????? ????????????? ????? ? ???? ??????, ??? ? ?????????? Windows.
2. ????????? ?????? ???????? ?? IDE:
   - ????????? ?????????? ?????????????? ????? ? ???????? ? ??????? ? ?????? ????, ??????? ????????????? ?? Enter, ?????? ?? Escape ? ????????????? ?? ?????? ??????.
3. ????? ????????? ?????????????? ??? ??????? ?? ????????:
   - ???????? ??????? ???? ??????? ? ???????????? ? ?????? ???? ? ???????.
## Обновление плана - 2026-04-21

1. Доведено поведение переименования в файловом менеджере IDE:
   - команда `Переименовать` больше не открывает отдельный диалог;
   - имя файла или каталога теперь редактируется прямо в узле дерева, как в проводнике Windows.
2. Следующая ручная проверка по IDE:
   - проверить встроенное переименование файла и каталога в светлой и тёмной теме, включая подтверждение по `Enter`, отмену по `Escape` и подтверждение по потере фокуса.
3. Общий следующий содержательный шаг проекта не меняется:
   - отдельно добрать доли секунды и длительности в пакете даты и времени.
## Обновление плана - 2026-04-22

1. Для IDE включена расширенная трассировка вокруг спорного сценария редактирования:
   - журнал теперь должен фиксировать переключение стиля форматирования, применение темы, переходы курсора между строками, блочное автодополнение, построчное автоформатирование и исключения обработчиков редактора;
   - при сбое обработчика путь к активному журналу должен оставаться видимым в строке состояния, чтобы не терять след воспроизведения.
2. Ближайший следующий шаг по IDE:
   - воспроизвести пользовательский сценарий в живой IDE или через `Axiom.Ide.SmokeHarness`, собрать свежий журнал и определить первую падающую последовательность `style/theme -> caret transition -> autoformat`.
3. После получения журнала:
   - исправить конкретный источник сбоя и отдельно проверить, что переключение между классическим и современным стилем снова полностью переформатирует активный `.axm`-документ без ложного dirty-state и без закрытия IDE.
## Обновление плана - 2026-04-22

1. По журналу подтверждены два дефекта навигационного редактирования:
   - переходы мышью вызывали построчное форматирование, а стрелки не вызывали его вовсе;
   - обычная навигация между строками могла запускать блочную автоправку и вставлять лишние пустые строки или закрывающие конструкции.
2. Исправление уже внесено в рабочую сборку:
   - обработчик перемещения каретки снова опирается на активный редактор, поэтому форматирование по уходу со строки должно работать и при клавиатурной навигации;
   - блочная автоправка отвязана от обычных переходов курсора;
   - форматтер строки схлопывает разорванные пробелами `<<` и `>>` обратно в канонический вид.
3. Следующий шаг по IDE:
   - вручную проверить в живой IDE и при необходимости через `Axiom.Ide.SmokeHarness`, что стрелки снова форматируют строку, `Select Case` и другие блоки больше не вставляют лишние строки при кликах и переходах, а журнал не пишет `editor.block-edit.applied` на обычной навигации.
4. После этой ручной проверки:
   - продолжить охоту за редким вылетом IDE уже на более чистом поведении редактора и снять следующий свежий журнал, если закрытие снова проявится.
## Обновление плана - 2026-04-22

1. После отвязки блочной правки от обычной навигации выяснился второй регресс:
   - автодополнение блоков перестало срабатывать по `Enter`, хотя пользователь ожидает именно такой сценарий для `If`, `Select Case`, `For` и других конструкций.
2. Исправление уже внесено:
   - блочная правка теперь запускается не на любой смене строки, а только после `Enter`;
   - построчное форматирование по стрелкам и мыши сохранено отдельно;
   - тестами дополнительно закреплены `For` -> `Next` и `Select Case` -> `End Select`.
3. Следующий шаг по IDE:
   - вручную проверить в живой IDE, что `Enter` снова достраивает `End If`, `End Select`, `Next` и другие закрывающие строки, а обычные клики и стрелки по-прежнему ничего лишнего не вставляют.
4. После этой проверки:
   - вернуться к редкому вылету IDE и, если он снова проявится, разобрать новый журнал уже на восстановленном поведении автодополнения.
## Обновление плана - 2026-04-22

1. Пользователь уточнил ещё один регресс поведения форматирования:
   - при выборе `Format -> Classic` или `Format -> Modern` весь текст активной вкладки должен сразу переводиться в выбранный стиль;
   - при этом нельзя возвращать старый баг, когда звёздочка `*` ложноположительно появлялась уже при простом открытии документа.
2. Подтверждённый источник старого бага не меняется:
   - проблема ложной звёздочки сидела в автоматическом форматировании при открытии и активации вкладки, а не в самой команде меню.
3. Исправление уже внесено:
   - явная команда форматирования снова допускается и для `Untitled.axm`, поэтому полное переписывание текста работает на новом несохранённом BASIC-документе;
   - автоматическое форматирование на открытии и переключении вкладки не возвращено.
4. Следующий шаг по IDE:
   - вручную проверить в живой IDE два сценария: `Untitled.axm` и сохранённый `.axm`, убеждаясь, что `Format -> Classic/Modern` сразу переписывает весь текст, а простое открытие файла по-прежнему не ставит ложную звёздочку.
## Обновление плана - 2026-04-22

1. Ребрендинг `MyBasic` -> `Axiom` завершён на уровне репозитория:
   - решение, проекты, пространства имён, ресурсы IDE, справка, документация и тестовые имена синхронизированы под `Axiom`;
   - расширение BASIC-файлов и примеров переведено на `.axm`;
   - стартовый рабочий корень IDE без сохранённой истории теперь фиксирован как `C:\Axiom`.
2. Ближайшая ручная проверка после ребрендинга:
   - открыть `C:\Axiom\Axiom.sln`, запустить `Axiom.Ide` и убедиться, что `Untitled.axm`, файловая панель и `Save As` стартуют из `C:\Axiom`.
3. Отдельный организационный шаг вне кода:
   - после завершения текущей сессии решить судьбу старой копии `C:\basic`; сейчас она оставлена как исходный контрольный снимок, потому что активная среда всё ещё открыта из этого каталога.

## Обновление плана - 2026-04-22

1. По свежему журналу IDE подтверждена причина несогласованного форматирования:
   - для неподдерживаемого расширения вроде `.mbas` команда `Format -> Classic/Modern` пишет `format.document.skip`, но прежняя версия редактора всё равно продолжала менять покинутые строки через построчное автоформатирование.
2. Исправление внесено в рабочую сборку:
   - правило применимости форматирования вынесено в общее место и теперь одинаково используется полным форматированием, построчным автоформатированием и блочным автодополнением.
3. Следующий шаг по IDE:
   - вручную проверить пользовательский сценарий: открыть файл через `File Manager`, переименовать его, затем переключить `Classic/Modern` и убедиться, что для `.axm` стиль меняется целиком, а для неподдерживаемого расширения IDE больше ничего не переписывает по строкам «сама по себе».
## Обновление плана - 2026-04-22

1. Закрыт отложенный language-slice полиморфных higher-order функций одномерных массивов:
   - `ARRAYMAP(array, selector)` теперь работает в форме `FUNCTION(T) AS U`;
   - `ARRAYREDUCE(array, seed, reducer)` теперь работает в форме `FUNCTION(A, T) AS A`.
2. Одновременно обновлены проверки, справка и runnable acceptance-примеры:
   - binder и runtime тесты покрывают полиморфные сценарии;
   - manual больше не описывает `ARRAYMAP` и `ARRAYREDUCE` как same-type-only;
   - добавлены отдельные примеры `samples/functions/arraymap/polymorphic_transform.axm` и `samples/functions/arrayreduce/polymorphic_accumulator.axm`.
3. Общий следующий шаг проекта не меняется:
   - вернуться к текущему IDE-направлению и вручную проверить пользовательский сценарий форматирования и переименования, уже зафиксированный в последних пунктах плана.
## Обновление плана - 2026-04-22

1. Ручная проверка двух активных срезов закрыта:
   - полиморфные `ARRAYMAP` и `ARRAYREDUCE` в `Axiom.Ide` подтверждены как рабочие;
   - сценарий IDE с форматированием и переименованием тоже подтверждён как рабочий.
2. Значит, устаревший пункт плана про обязательную ручную проверку больше не блокирует движение проекта.
3. Следующий логический шаг снова общий, без отдельного check-point:
   - брать следующую содержательную задачу по IDE или по языку, не возвращаясь к этой верификации без нового сигнала о регрессии.

## Обновление плана - 2026-04-22

1. По новому сигналу пользователя отдельным текущим срезом стал диалог поиска и замены в `Axiom.Ide`.
2. Уже внесено:
   - форма поиска переведена на двухвкладочную модель `Найти` / `Замена`, ближе к Notepad++;
   - комбинированная кнопка убрана;
   - логика `Replace` сделана предсказуемой и покрыта отдельными тестами workflow-слоя.
3. Ближайший следующий шаг по этому срезу:
   - вручную проверить в живой IDE сценарии `Найти далее`, `Заменить`, `Заменить всё`, `Regex` и обратное направление.
4. После ручной проверки:
   - либо закрыть тему поиска и замены как рабочую,
   - либо, если всплывёт новый дефект, добить уже конкретный сценарий без новой общей переделки окна.

## Обновление плана - 2026-04-22

1. По уточнению пользователя выделен отдельный визуальный подшаг по окну поиска в тёмной теме.
2. Уже внесено:
   - вкладки диалога переведены на собственный тёмный стиль без светлой системной полосы;
   - блок параметров поиска больше не использует тяжёлый `GroupBox`;
   - размеры окна и кнопочной колонки увеличены, подпись `Искать назад` укорочена.
3. Ближайший следующий шаг по этому срезу:
   - вручную проверить именно тёмную тему и убедиться, что вкладки выглядят как нижние вкладки `Вывод` / `Диагностика`, а кнопка `Закрыть` полностью видна.
4. Если после этой проверки останутся только мелкие визуальные шероховатости:
   - исправлять их точечно, без новой смены структуры диалога.

## Обновление плана - 2026-04-22

1. По последнему визуальному замечанию пользователя внесены точечные правки окна поиска:
   - рамка параметров больше не растягивается по высоте сверх содержимого;
   - все кнопки действия справа имеют одинаковую ширину.
2. Ближайший следующий шаг:
   - вручную проверить окно поиска в светлой и тёмной теме и подтвердить, что визуальные штрихи закрыты.
3. Если ручная проверка пройдёт:
   - закрыть тему внешнего вида поиска и перейти к проверке поведения `Найти далее`, `Заменить`, `Заменить всё`, `Regex` и обратного направления.

## Обновление плана - 2026-04-22

1. Последний визуальный дефект кнопки `Закрыть` исправлен:
   - убран унаследованный отступ, который сдвигал кнопку вправо и мог обрезать правую границу.
2. Ближайший следующий шаг:
   - вручную подтвердить выравнивание всех четырёх кнопок в окне поиска.
3. После подтверждения:
   - считать визуальную доводку окна поиска закрытой и перейти к проверке поведения поиска и замены.

## Обновление плана - 2026-04-22

1. Структура стилей окна поиска приведена к единой модели:
   - кнопки действий, поля ввода и рамка параметров теперь управляются отдельными ролевыми стилями;
   - локальные размеры на конкретных кнопках и полях убраны из XAML.
2. Ближайший следующий шаг:
   - вручную проверить, что после централизации стилей внешний вид окна не изменился в худшую сторону.
3. После проверки:
   - закрыть визуальную тему окна поиска и вернуться к проверке поведения поиска и замены.

## Обновление плана - 2026-04-22

1. Вкладки IDE унифицированы:
   - `Вывод`, `Диагностика`, `Найти` и `Замена` используют общий `ShellTabItemStyle`.
2. Контейнеры вкладок оставлены специализированными только по структуре:
   - нижняя панель хранит содержимое вкладок;
   - диалог поиска использует вкладки как переключатель режима.
3. Ближайший следующий шаг:
   - вручную проверить светлую и тёмную тему, чтобы убедиться, что общий стиль вкладок выглядит одинаково во всех местах.

## Обновление плана - 2026-04-22

1. По запросу пользователя добавлена иконка приложения `Axiom.Ide`:
   - исходник хранится в `src/Axiom.Ide/Assets/axiom-icon.svg`;
   - рабочий Windows-ресурс хранится в `src/Axiom.Ide/Assets/axiom.ico`;
   - проект подключает `.ico` через `ApplicationIcon`;
   - главное окно использует этот же ресурс через свойство `Icon`.
2. Проверки прошли:
   - тесты `Axiom.Ide.Tests`;
   - сборка `Axiom.Ide`.
3. Ближайший следующий шаг:
   - вручную запустить IDE и проверить иконку в заголовке окна, на панели задач и у собранного приложения.

## Обновление плана - 2026-04-22

1. Окно `О программе` приведено к текущей модели IDE:
   - вместо системного значка `MessageBox` используется иконка Axiom;
   - заголовок, текст сообщения и строка состояния локализованы через `ShellStrings.ru.xaml` и `ShellStrings.en.xaml`;
   - выбор текста привязан к текущему языку IDE.
2. Проверки прошли:
   - тесты `Axiom.Ide.Tests`;
   - сборка `Axiom.Ide`.
3. Ближайший следующий шаг:
   - вручную проверить `Справка -> О программе` при русском и английском языке IDE, затем вернуться к проверке оставшихся сценариев поиска и замены.

## Обновление плана - 2026-04-22

1. Исправлены два замечания по временным статусам и качеству иконки:
   - иконка внутри `О программе` теперь векторная, без растягивания `.ico`;
   - после закрытия `О программе` статус показывает закрытие окна, а не открытие;
   - после закрытия окна поиска временное сообщение поиска очищается до `Готово.` / `Ready.`.
2. Проверки прошли:
   - тесты `Axiom.Ide.Tests`;
   - сборка `Axiom.Ide` в отдельный каталог вывода, потому что запущенная IDE блокировала обычный выходной файл.
3. Ближайший следующий шаг:
   - вручную проверить оба сценария в живой IDE, затем продолжить проверку поведения `Найти далее`, `Заменить`, `Заменить всё`, `Regex` и обратного направления.

## Обновление плана - 2026-04-22

1. По новому запросу пользователя в отдельный IDE-срез вынесена нормализация регистра пользовательских имён.
2. Уже внесено:
   - в `Format` добавлен чекбокс `Умный идентификатор`;
   - опция сохраняется между запусками IDE;
   - для `.axm` и `Untitled.axm` при уходе со строки и при полном форматировании документа IDE приводит уже объявленные имена к каноническому виду объявления;
   - текущий срез покрывает объявления `DIM`, `CONST`, `SUB`, `FUNCTION`, параметры и управляющую переменную `FOR`.
3. Автоматическая проверка по новому срезу уже есть:
   - добавлены отдельные тесты сервиса нормализации имён;
   - добавлены тесты восстановления настройки и её влияния на полное форматирование документа.
4. Ближайший следующий шаг:
   - вручную открыть живую `Axiom.Ide`, включить `Format -> Умный идентификатор` и проверить два сценария:
     - уход со строки после ввода имени в другом регистре;
     - полное форматирование `Classic` / `Modern` на документе с уже разнобойным регистром идентификаторов.

## Обновление плана - 2026-04-22

1. По новому UX-сигналу пользователя уточнено поведение окна поиска:
   - `Find / Replace` не должен позволять открывать другие pull-down меню оболочки, пока сам открыт.
2. Уже внесено:
   - на время видимости окна поиска верхнее меню и панель быстрых кнопок временно блокируются;
   - после закрытия окна поиска они автоматически разблокируются.
3. Проверки прошли:
   - сборка `Axiom.Ide`;
   - точечные IDE-тесты на обновлённые XAML/code-behind привязки.
4. Ближайший следующий шаг:
   - вручную открыть `Find / Replace` в живой `Axiom.Ide` и убедиться, что редактором пользоваться можно, а верхние pull-down меню и быстрые кнопки до закрытия окна поиска не активируются.

## Обновление плана - 2026-04-22

1. Пользователь уточнил, что промежуточная ручная блокировка меню была неверной:
   - окно поиска должно вести себя как настоящее модальное окно владельца, аналогично `О программе`;
   - редактор тоже не должен принимать ввод, пока окно открыто.
2. Исправление уже внесено:
   - ручная блокировка shell-chrome убрана;
   - `Find / Replace` переведён на `ShowDialog()` с владельцем `MainWindow`;
   - начальный фокус и выделение в поле поиска перенесены в жизненный цикл самого диалога.
3. Проверки прошли:
   - сборка `Axiom.Ide` в отдельный каталог вывода;
   - точечные IDE-тесты на обновлённые XAML/code-behind привязки.
4. Ближайший следующий шаг:
   - вручную проверить в живой `Axiom.Ide`, что при открытом `Find / Replace` редактор, pull-down меню и прочие элементы владельца ведут себя так же, как при открытом окне `О программе`.

## Обновление плана - 2026-04-22

1. Проведён аудит `Axiom.Ide`.
2. Исправлена кодировка русских строк в `MainWindowViewModel.cs`.
3. Оставшиеся 3 падения `Axiom.Ide.Tests` относятся к хрупким XAML-проверкам.
4. Следующий шаг:
   - разделить shell-логику `MainWindow` и обновить хрупкие IDE-тесты.

## Обновление плана - 2026-04-22

1. Разделение shell-логики `MainWindow` на partial-файлы завершено:
   - файловая панель, поиск/замена и визуально-древесные helper-методы вынесены из общего `MainWindow.xaml.cs`;
   - formatting-срез `MainWindowViewModel` вынесен в отдельный partial-файл.
2. Разметка `FindReplaceDialog` сделана менее жёсткой:
   - размеры окна вынесены в стиль `FindReplaceDialogWindowStyle`;
   - правые колонки и подписи больше не держатся на лишних жёстких ширинах там, где можно опереться на `Auto` и уже существующие стили кнопок.
3. Хрупкие IDE-проверки стабилизированы:
   - обновлены строковые тесты на `FindReplaceDialog.xaml` и `ShellStyles.xaml`;
   - полный набор `Axiom.Ide.Tests` снова проходит полностью.
4. Следующий шаг:
   - убрать разрозненное создание shell-зависимостей между `App.xaml.cs`, `MainWindow.xaml.cs` и `MainWindowViewModel.cs`, а затем пройтись по UI-командам с `async void` и синхронными ожиданиями на главном потоке.
## Обновление плана - 2026-04-22

1. Разрозненное создание shell-зависимостей убрано:
   - добавлен `IdeShellCompositionRoot`;
   - `App.xaml.cs` теперь запускает главное окно через него;
   - `MainWindow` и `MainWindowViewModel` переведены на явное получение зависимостей.
2. Наиболее рискованный UI-хвост тоже убран:
   - из `Axiom.Ide` исчезли `async void` и `GetAwaiter().GetResult()` на путях закрытия вкладок и выхода;
   - prompt save/discard/cancel возвращён к обычному модальному shell-поведению.
3. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-pass-4\`
4. Следующий шаг:
   - пройтись по `Resources/ShellStyles.xaml` и убрать оставшееся дублирование шаблонов меню и контекстного меню, чтобы снизить точки расхождения оформления.

## Обновление плана - 2026-04-22

1. Повторяющийся popup-каркас submenu в `ShellStyles.xaml` убран:
   - добавлены `ShellPopupMenuScrollViewerTemplate` и `ShellPopupMenuScrollViewerStyle`;
   - `ShellTopLevelHeaderTemplate` и `ShellSubmenuHeaderTemplate` переведены на общий scrollviewer-каркас.
2. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-style-dedupe-build\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators|FullyQualifiedName~ShellStylesXaml_UsesCustomTemplatesForMenuAndContextMenu" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-style-dedupe-tests\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`
3. Замечание по инфраструктуре:
   - полный `dotnet test` с отдельным `OutputPath` на этом проходе упирался во временный WPF `_wpftmp`-проект, поэтому итоговая полная проверка подтверждена обычным прогоном без переопределения каталога вывода.
4. Следующий шаг:
   - пройтись по shell-локализации и показу диалогов, чтобы убрать смешение русских строк, resource-ключей и прямых вызовов prompt-сервиса в разных слоях.

## Обновление плана - 2026-04-22

1. Исправлена читаемость русских букв в меню:
   - увеличен вертикальный запас верхнего меню;
   - submenu-пункты переведены на `MinHeight`, чтобы не резать нижние выносные элементы букв.
2. Начат проход по shell-локализации:
   - `Close/Exit`, `Manual` и `Run` переведены на resource-ключи в `ShellStrings.ru/en.xaml`;
   - `MainWindowViewModel` теперь берёт эти статусы и заголовки через локализованный helper.
3. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-build\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators|FullyQualifiedName~RunProgram_SetsOutputAndStatus|FullyQualifiedName~RunProgram_WithDiagnostics_SelectsAndFocusesDiagnostics|FullyQualifiedName~RunProgram_PromptsToSaveDirtyDocumentBeforeRunning|FullyQualifiedName~RunProgram_WhenUserDeclinesSaveBeforeRun_DoesNotRun|FullyQualifiedName~CloseTabCommand_DirtyDocumentAndPromptCancel_KeepsTabOpen|FullyQualifiedName~OpenManualCommand_OpensManualThroughExternalNavigationService|FullyQualifiedName~OpenManualCommand_WhenManualIsMissing_ShowsPromptError" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-targeted\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-full\`
4. Следующий шаг:
   - продолжить локализацию shell-оболочки в файловом менеджере, свойствах, переименовании, удалении и остальных статусных сообщениях, чтобы убрать смешение английских и русских фраз по всему `MainWindowViewModel`.

## Обновление плана - 2026-04-22

1. Второй проход по shell-локализации завершён для файловой панели:
   - файловые операции, свойства узлов, скрытые файлы и недоступные элементы `Recent Files` переведены на resource-ключи;
   - окно `Свойства` больше не собирается из жёстко зашитых английских подписей.
2. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-build\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~NavigateFilesPaneNodeCommand_SwitchesRootToSelectedDirectory|FullyQualifiedName~NewFilesPaneDirectoryCommand_CreatesFolderInSelectedDirectory|FullyQualifiedName~FilesPaneShowHiddenFilesChange_ReloadsTreeWithHiddenFlag|FullyQualifiedName~ShowFilesPaneNodePropertiesCommand_UsesPromptServiceForSelectedNode|FullyQualifiedName~MoveFilesPaneNodeToRecycleBinCommand_ConfirmsAndDeletesSelectedFile|FullyQualifiedName~OpenFilesPaneNodeCommand_DoesNotOpenExecutableFiles" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-tests\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-full\`
3. Следующий шаг:
   - продолжить тот же локализационный проход в `DocumentWorkspaceService` и прочих shell-сервисах, чтобы убрать оставшиеся английские статусы `Open / Save / Save All` и довести оболочку до одного ресурсо-ориентированного стиля.

## Обновление плана - 2026-04-22

1. Локализация документного workflow доведена до конца:
   - `DocumentWorkspaceService` больше не возвращает английские литералы для `Open / Save / Save All`, а использует ресурсные ключи с учётом текущего языка IDE;
   - `MainWindowViewModel` синхронизирован с теми же ключами для fallback-статусов документных команд;
   - `WpfDialogPromptService` локализует заголовок и текст prompt-а `Save / Discard / Cancel` через активные ресурсы оболочки.
2. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-document-localization-tests\`
3. Следующий шаг:
   - вручную проверить в живой `Axiom.Ide`, что статусы `Open / Save / Save All` и prompt сохранения переключаются между русским и английским вместе с языком IDE.

## Обновление плана - 2026-04-22

1. Ручная проверка локализации документного workflow подтверждена:
   - статусы `Open / Save / Save All` корректно переключаются между русским и английским;
   - prompt сохранения тоже переключается вместе с языком IDE.
2. Ближайший следующий шаг:
   - пройтись по оставшимся глобальным shell-строкам и инфраструктурным сообщениям вне `MainWindowViewModel`:
     `App.xaml.cs`, default-title fallback-ветки prompt-сервисов и инфраструктурные сообщения `ProgramRunService`.
3. Цель следующего прохода:
   - убрать последние разрозненные англоязычные литералы из shell-оболочки и довести локализацию IDE до полностью согласованного состояния.

## Обновление плана - 2026-04-22

1. Проход по глобальной shell-локализации продолжен и расширен:
   - `App.xaml.cs`, `ProgramRunService`, `DialogPromptService`, `StatusBarViewModel` и `MainWindow.Search.cs` переведены на общий ресурсный путь для глобальных сообщений;
   - стандартные `InformationDialog` и `TextInputDialog` теперь используют те же resource-ключи для заголовка и кнопки подтверждения;
   - `MainWindowViewModel` переведён на ресурсные шаблоны заголовка окна IDE и убран разбросанный fallback для `About`.
2. Проверки прошли:
   - `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -nodeReuse:false -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-final-localization-build-7\`
   - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -nodeReuse:false -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-final-localization-pass-9\`
3. Следующий шаг:
   - вручную проверить в живой `Axiom.Ide`, что `About`, стандартные `Info/Input`-диалоги, строка состояния после `Find / Replace` и глобальные shell-сообщения тоже корректно переключаются между русским и английским.

## Обновление плана - 2026-04-22

1. Ручная проверка глобальной shell-локализации подтверждена:
   - `About`, стандартные `Info/Input`-диалоги и строка состояния после `Find / Replace` корректно переключаются между русским и английским;
   - глобальные shell-сообщения, затронутые последним проходом, ведут себя нормально в живой IDE.
2. Ближайший следующий шаг:
   - сделать короткий остаточный аудит `src/Axiom.Ide` на явные пользовательские строки вне ресурсных словарей и после этого перейти к следующему незавершённому пункту плана.

## Обновление плана - 2026-04-22

1. Остаточный аудит shell-локализации подтверждён пользователем:
   - очевидных пользовательских строк, требующих немедленного переноса в `Resources/ShellStrings.*.xaml`, не осталось;
   - локализационный хвост IDE считается закрытым.
2. Следующий пункт:
   - сверить старую незакрытую отметку `Phase 4` про placeholder-пункты `View/Help` меню с текущей реализацией `MainWindow` и либо закрыть её как устаревшую, либо добить недостающий command wiring.

## Обновление плана - 2026-04-22

1. Старая незакрытая отметка `Phase 4` про placeholder-пункты `View/Help` закрыта ручной проверкой:
   - `View/Help` открывает проектную документацию в живой `Axiom.Ide`;
   - дополнительный command wiring для этого пункта не требуется.
2. Следующий шаг:
   - считать текущий локализационно-shell проход закрытым и перед новым кодовым изменением выбрать следующий содержательный пункт из общего плана или из отложенных IDE-задач.

## Обновление плана - 2026-04-22

1. Уточнён список будущих IDE-направлений:
   - тёмная тема уже готова;
   - переключение тем уже работает;
   - автодополнение остаётся задачей далёкого будущего и сейчас не берётся.
2. В рамках будущей IDE-работы не потерять следующие направления:
   - восстановление сессии: вернуть после запуска IDE открытые вкладки, активный документ и рабочее состояние;
   - сложная навигация по коду: переход к объявлениям, список процедур и функций, поиск использований и другие code-navigation действия;
   - отладчик: пошаговое выполнение, остановки, продолжение выполнения и показ текущего места программы;
   - панель `Variables`: просмотр значений переменных во время отладки;
   - панель `Assistant`: встроенный помощник по ошибкам, выбранному коду и справке языка, но только после стабилизации ядра IDE и языка.
3. Предпочтительный порядок на будущее:
   - сначала восстановление сессии;
   - затем сложная навигация по коду;
   - затем отладчик;
   - затем панель `Variables`;
   - панель `Assistant` оставить самой поздней.

## Обновление плана - 2026-04-22

1. Проведена сверка состояния одномерных массивов:
   - фактический список встроенных массивных функций в семантике содержит `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED` и полный набор `ARRAY*`;
   - основная страница справочника `docs/manual/arrays.html` и алфавитный указатель `docs/manual/scripts/manual.js` уже содержали все функции массива;
   - в обзорной странице `docs/manual/arrays-and-builtins.html` не хватало ссылок на четыре информационные функции массива.
2. Исправление внесено:
   - в обзорную страницу добавлены ссылки на runnable-примеры `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`;
   - карточка маршрута по одномерным массивам уточнена так, чтобы явно упоминать информационные функции и операции над массивами.
3. Проверки прошли:
   - сверка `arrays.html`, `arrays-and-builtins.html` и `manual.js` на полный список массивных функций;
   - `node --check docs/manual/scripts/manual.js`;
   - `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Array" --no-restore -p:UseSharedCompilation=false`;
   - `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Array" --no-restore -p:UseSharedCompilation=false`.
4. Отдельно проверено:
   - `tests\Axiom.Execution.Tests` не содержит тестов, подходящих под фильтр `FullyQualifiedName~Array`, поэтому этот точечный прогон не выполнял тестовые случаи.
5. Следующий шаг:
   - считать реализацию и справочник одномерных массивов закрытыми в текущем объёме;
   - новую работу выбирать как отдельную содержательную задачу по IDE или языку, а к массивам возвращаться только при новом конкретном дефекте или новом запланированном расширении.

## Обновление плана - 2026-04-22

1. Для пользовательских типов выбран первый устойчивый объём `TYPE ... END TYPE`:
   - верхнеуровневое объявление записи;
   - поля в форме `fieldName AS fieldType`;
   - пользовательский тип в `DIM`, параметрах `SUB`/`FUNCTION` и результате `FUNCTION`;
   - чтение и запись полей через точку;
   - вложенные пользовательские типы;
   - копирование записи при присваивании целиком.
2. Сознательно отложены более широкие возможности, которые есть в разных ветках BASIC/VB или старом проекте:
   - массивы внутри `TYPE`;
   - методы, константы и уровни доступа внутри `TYPE`;
   - `FILE` и функциональные поля;
   - локальные объявления `TYPE` внутри процедур;
   - общий синтаксис групп полей вида `AS INTEGER X, Y`.
3. Следующий шаг после текущего прохода:
   - проверить пользовательские типы в живой `Axiom.Ide` на открытии и запуске примера `samples/operators/type/field_access.axm`;
   - затем выбирать следующий языковой срез только отдельной задачей, чтобы не смешивать базовые записи с массивами и методами внутри `TYPE`.

## Обновление плана - 2026-04-22

1. Продуктовая рамка обновлена:
   - старая установка на ограниченный первый выпуск больше не считается источником истины;
   - документы должны описывать Axiom как полноценный продукт, который развивается последовательными проверяемыми шагами;
   - крупные возможности больше не считаются запрещёнными только из-за прежней рамки первой версии, но должны проектироваться и фиксироваться явно перед реализацией.
2. Прямые упоминания старой рамки удалены из действующих документов и исторических записей, где они продолжали задавать текущий курс.
3. Следующий языковой шаг после базового `TYPE ... END TYPE`:
   - рассматривать расширение пользовательских типов без искусственного ограничения первой версии;
   - ближайший практический срез оставить прежним по инженерной пользе: массивы пользовательских типов и доступ вида `people(i).Name`;
   - перед кодом зафиксировать точный договор в спецификации и справочнике.

## Обновление плана - 2026-04-22

1. Ближайший языковой срез по пользовательским типам закрыт:
   - пользовательский тип записи можно использовать как тип элемента одномерного массива;
   - доступ к полям элементов массива вида `people(i).Name` закреплён тестами, спецификацией и справочником;
   - запись, присвоенная в элемент массива целиком, копируется как значение, поэтому исходная переменная и элемент массива дальше живут независимо.
2. Добавлен ручной проверочный пример:
   - `samples/operators/type/array_elements.axm`.
3. Проверки прошли:
   - `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter TypeArrayElementFieldAccess --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`;
   - `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter TypeArrayElementFieldAccess --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`;
   - `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~TypeArray" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`;
   - `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter TypeArrayElementsAcceptanceSample --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`;
   - `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false`;
   - `node --check docs\manual\scripts\manual.js`;
   - поиск старых ограничительных формулировок по `AGENTS.md`, `PROJECT.md`, `README.md` и `docs`.
4. Следующий шаг:
   - открыть `samples/operators/type/array_elements.axm` в живой `Axiom.Ide` и вручную запустить пример;
   - после ручной проверки выбрать следующий отдельный срез по `TYPE`: массивы внутри записей, групповые поля или более богатые формы процедурной работы с записями.

## Обновление плана - 2026-04-22

1. Следующий срез по `TYPE` закрыт:
   - внутри записи поддержаны динамические массивные поля вида `fieldName() AS TYPE`;
   - индексирование массива теперь работает и для массивного поля записи, включая форму `people(1).Scores(2)`;
   - `REDIM`, `REDIM PRESERVE` и `ERASE` принимают массивное поле записи как целевой массив.
2. Добавлен ручной проверочный пример:
   - `samples/operators/type/array_fields.axm`.
3. Проверки для этого среза:
   - точечные прогоны `Axiom.Parsing.Tests`, `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests` для новых сценариев;
   - `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false`;
   - `node --check docs\manual\scripts\manual.js`.
4. Следующий шаг:
   - вручную открыть `samples/operators/type/array_fields.axm` в `Axiom.Ide` и проверить сценарий целиком;
   - после этого выбирать следующий отдельный срез по `TYPE`: фиксированные массивы в полях записи, групповые поля или другие формы объявления полей.


## Обновление плана - 2026-04-22

1. Закрыт следующий срез по `TYPE`: фиксированные массивные поля вида `fieldName(lower TO upper) AS TYPE` теперь входят в рабочий контракт языка.
2. Следующий логический шаг по этой теме: групповые объявления полей внутри `TYPE`, чтобы поддержать компактные формы наподобие `X, Y AS INTEGER`, если они не ломают уже зафиксированную модель разбора.
3. После групповых полей отдельно сверить оставшиеся полезные формы из QuickBASIC / Visual Basic и старого проекта Action, чтобы дальнейшее расширение `TYPE` шло по исторически узнаваемой линии, а не случайными локальными решениями.

## Обновление плана - 2026-04-22

1. Закрыт следующий срез по TYPE: поддержаны групповые объявления полей с общим AS, включая смешанные формы со скалярными, динамическими и фиксированными массивными декларатороми в одной строке.
2. Историческая опора для этого шага зафиксирована так:
   - блок TYPE ... END TYPE и фиксированные поля-массивы продолжают держаться линии Microsoft BASIC / VBA;
   - семантика общей AS для группы выбрана по более удобной линии Visual Basic с распространением первого As на предшествующие имена в группе;
   - внутреннее представление полей сохраняет подход старого Action, где каждое поле живёт как отдельная запись метаданных.
3. Следующий точный шаг:
   - вручную открыть samples/operators/type/grouped_fields.axm в Axiom.Ide и подтвердить ожидаемый вывод;
   - после ручной проверки выбрать следующий отдельный срез по TYPE из ещё не закрытых исторически полезных форм; первым кандидатом считать поддержку CONST в границах фиксированных полей записи.

## Обновление плана - 2026-04-22

1. Закрыт следующий срез по TYPE: фиксированные массивные поля записи теперь принимают границы из ранее объявленных верхнеуровневых CONST.
2. Проверены и зафиксированы две сопутствующие семантики:
   - для вычисления границ работают простые целочисленные константные выражения;
   - имя CONST должно быть объявлено выше по файлу, чем использующий его TYPE.
3. Следующий точный шаг:
   - вручную открыть samples/operators/type/const_fixed_array_fields.axm в Axiom.Ide и подтвердить ожидаемый вывод;
   - после ручной проверки перейти к следующему ещё не закрытому исторически полезному срезу по TYPE, если он останется, либо вернуться к массивам и справочнику функций, как мы договаривались раньше.

## Обновление плана - 2026-04-22

1. Снят ещё один UX-барьер по TYPE: верхнеуровневые CONST в границах фиксированных полей работают независимо от порядка объявления в файле.
2. Короткий отбор оставшегося исторически полезного хвоста по TYPE теперь такой:
   - следующий сильный кандидат: фиксированные строки внутри TYPE, если мы подтверждаем их практическую пользу для файловых и структурных сценариев;
   - модульные формы Public / Private TYPE — отложить до явного шага по многофайловым модулям;
   - расширения из старого Action вроде методов внутри TYPE, List-полей и jagged-массивов — считать отдельной веткой развития, а не мелким хвостом текущей добивки.
3. Следующий точный шаг:
   - вручную открыть samples/operators/type/const_fixed_array_fields_forward.axm в Axiom.Ide и подтвердить ожидаемый вывод;
   - после ручной проверки отдельно решить, берём ли следующим срезом фиксированные строки в TYPE, либо возвращаемся к массивам и полноте справочника функций.

## Обновление плана - 2026-04-22

1. Закрыт следующий исторически полезный срез по строкам:
   - поддержаны фиксированные строки `STRING * N` в `DIM`;
   - поддержаны фиксированные строковые поля в `TYPE`;
   - зафиксированы правила пробельного значения по умолчанию, дополнения справа и обрезки справа.
2. Ближайший технический хвост после этого шага:
   - явно добить диагностику недопустимых контекстов, если где-то ещё допускается синтаксис `STRING * N` без реальной поддержки;
   - затем вернуться к ранее обещанному аудиту массивов и полноты справочника массивных функций.
3. Следующий точный шаг:
   - вручную открыть `samples/operators/dim/fixed_string.axm` и `samples/operators/type/fixed_string_fields.axm` в `Axiom.Ide`;
   - подтвердить ожидаемый вывод по дополнению и обрезке строк;
   - после ручной проверки решить, идём ли сразу в оставшийся хвост по массивам и справочнику, либо сначала жёстче фиксируем запреты на неподдержанные формы `STRING * N`.

## Обновление плана - 2026-04-22

1. Зафиксировано дополнительное архитектурное наблюдение по `STRING * N`:
   - текущая реализация даёт фиксированную семантику длины хранения;
   - текущая реализация не даёт выигрыша по памяти относительно обычного `STRING`;
   - для коротких значений фиксированная строка сейчас может даже занимать больше памяти из-за пробельного дополнения.
2. Отдельная будущая задача:
   - спроектировать более плотное хранение фиксированных строк, если оно действительно понадобится для записей, массивов, файловых или бинарных сценариев;
   - при таком шаге не менять пользовательский синтаксис `STRING * N`, а улучшать внутреннее представление рантайма.
3. Ближайший рабочий приоритет от этого не меняется:
   - сначала завершить явные границы поддержки и аудит массивов;
   - затем, отдельным решением, вернуться к memory-оптимизации фиксированных строк как к самостоятельному архитектурному срезу.

## Обновление плана - 2026-04-22

1. По `STRING * N` закрыт технический хвост неподдержанных контекстов:
   - прямая форма и вложенные варианты теперь явно отсекаются диагностикой `MB2026` в `CONST`, параметрах и типах результата `FUNCTION`, а также внутри функциональных типов;
   - парсер больше не втягивает `=` в выражение длины фиксированной строки, поэтому запрет в `CONST` проходит как целевая семантическая диагностика, а не как побочный синтаксический сбой;
   - добавлены отрицательные `.axm`-примеры для `CONST` и `FUNCTION`.
2. Следующий рабочий шаг:
   - начать разбиение `Binder` по зонам ответственности (`Binder.Expressions`, `Binder.Helpers`, `Binder.Statements`);
   - во время разбиения отдельно проверить и при необходимости нормализовать кодировку русских строк в `src/Axiom.Semantics/Binding/`, чтобы не возвращалась битая локализация диагностик.
## Обновление плана - 2026-04-22

1. После закрытия неподдержанных контекстов STRING * N выполнен следующий организационный шаг по binder:
   - диагностические методы вынесены из Binder.Helpers.cs в отдельный partial-файл Binder.Diagnostics.cs;
   - Binder.Helpers.cs очищен от диагностического слоя и снова содержит в основном служебную логику привязки типов, констант и преобразований;
   - попутно обновлён устаревший тест по DATE, который ожидал старую диагностику по количеству аргументов, хотя у функции уже есть строковая перегрузка с часовым поясом.
2. Ближайший следующий шаг:
   - продолжить разбиение оставшегося содержимого Binder.cs на более узкие partial-файлы по ответственности, в первую очередь для деклараций типов/процедур и привязки routine-level структуры;
   - отдельно пройтись по src/Axiom.Semantics/Binding/ и добить остаточный аудит кодировки русских строк, чтобы не осталось файлов со старой битой локализацией.

## Обновление плана - 2026-04-22

1. Разбиение binder продолжено ещё на один крупный слой:
   - `Binder.cs` теперь держит только корневую композицию и запуск `BindProgram`;
   - декларации типов, `CONST` для границ и объявления процедур уже вынесены в `Binder.Declarations.cs`;
   - привязка процедур и функций вынесена в `Binder.Routines.cs`;
   - вызовы, доступ к полям и массивам, а также привязка массивных built-in теперь живут в новом `Binder.InvocationExpressions.cs`.
2. Аудит папки `src/Axiom.Semantics/Binding/` на этом шаге дал такой результат:
   - отдельный проход по файлам не показал маркеров битой кириллицы в текущем наборе `Binding/*.cs`;
   - после вынесения invocation-слоя дополнительно проверено, что в текущих файлах не осталось заметных хвостов mojibake в строковых литералах и служебных сообщениях.
3. Следующий рабочий шаг:
   - продолжить разбиение `Binder.Helpers.cs`, отделив числовые/константные helper-методы от общей служебной логики привязки;
   - затем решить, выгоднее ли следующей отдельной волной резать `Binder.Statements.cs`, либо сначала добить оставшиеся локальные огрехи кодировки в других папках семантического слоя.

## Обновление плана - 2026-04-22

1. Разбиение binder продолжено ещё на три профильных слоя:
   - `Binder.TypeResolution.cs` теперь держит разрешение типов, fixed-string-валидацию и вычисление целочисленных констант для типовых сценариев;
   - `Binder.ScopeHelpers.cs` держит поиск переменных, работу с областями видимости и объявление локальных переменных;
   - `Binder.SyntaxHelpers.cs` держит текст операторов, вычисление `TextLocation` и разбор числового текста литералов.
2. После этого `Binder.Helpers.cs` заметно сжат и теперь содержит только действительно общие числовые helper-методы и локальную диагностику по `ARRAYMAP`.
3. Следующий рабочий шаг:
   - решить, оставляем ли компактный `Binder.Helpers.cs` как финальный общий узел без дальнейшего дробления;
   - затем перейти к следующему крупному кандидату, которым сейчас выглядит `Binder.Statements.cs`, если не всплывёт отдельная необходимость дочистить кодировку в других семантических файлах.

## Обновление плана - 2026-04-23

1. Разбиение `Binder.Statements.cs` выполнено:
   - сам `Binder.Statements.cs` теперь оставлен как компактная маршрутизация операторов;
   - объявления переменных, `CONST`, присваивания, `ERASE` и `REDIM` вынесены в `Binder.DeclarationStatements.cs`;
   - `PRINT`, `PRINT USING`, `INPUT`, `OPEN` и `CLOSE` вынесены в `Binder.IoStatements.cs`;
   - `IF`, `WHILE`, `DO`, `FOR`, `EXIT FOR`, `EXIT DO` и `RETURN` вынесены в `Binder.ControlFlowStatements.cs`;
   - вся логика `SELECT CASE` вынесена в `Binder.SelectCaseStatements.cs`.
2. После этого основные крупные файлы binder уже разделены по зонам ответственности. Самым большим оставшимся файлом остаётся `Binder.InvocationExpressions.cs`, но он сейчас цельный по смыслу: вызовы, доступ к массивам/полям и разрешение built-in.
3. Следующий рабочий шаг:
   - сделать короткий контрольный проход по `src/Axiom.Semantics/Binding/` на остаточные признаки битой русской кодировки и слишком крупные новые файлы;
   - затем вернуться к языковому плану: массивы и полнота справочника массивных функций.

## Обновление плана - 2026-04-23

1. Контрольный проход по `src/Axiom.Semantics/Binding/` после разбиения binder завершён:
   - явных маркеров битой русской кодировки в текущих `.cs`-файлах не найдено;
   - replacement-символов и C1-управляющих символов не найдено;
   - новые statement-файлы остаются разумного размера и разделены по ответственности.
2. Крупнейшие оставшиеся файлы binder:
   - `Binder.InvocationExpressions.cs`;
   - `Binder.Expressions.cs`;
   - `Binder.Declarations.cs`.
   Пока они выглядят логически цельными и не требуют немедленного разреза перед продолжением языковых задач.
3. Следующий рабочий шаг:
   - вернуться к массивам;
   - сверить реализованные массивные функции с пользовательским справочником и индексом справочника;
   - если найдётся недостающая полезная функция или недостающая документация, закрыть это отдельным небольшим срезом.

## Обновление плана - 2026-04-23

1. Аудит одномерных массивов и справочника завершён:
   - в коде найдено 30 пользовательских массивных имён: `LBOUND`, `UBOUND` и семейство `ARRAY...`;
   - все 30 имён есть в `docs/manual/arrays.html`;
   - все 30 имён есть в алфавитном указателе `docs/manual/scripts/manual.js`;
   - все 30 имён имеют папку ручных примеров в `samples/functions/`;
   - обзорная страница `docs/manual/arrays-and-builtins.html` содержит 30 ссылок на массивные примеры, и все эти ссылки ведут к существующим `.axm`-файлам.
2. По пути закрыт найденный качество-хвост:
   - в `src/Axiom.Runtime/Execution/` восстановлены повреждённые русские строки диагностик времени выполнения;
   - добавлена явная обработка UTF-8 BOM при `READFILE` и `READALLLINES`;
   - `FROMUTC` снова явно сообщает ошибку аргумента, если перевод из UTC выходит за поддерживаемый диапазон даты-времени.
3. Следующий рабочий шаг:
   - вручную открыть `samples/functions/readalllines/utf8_bom.axm` в `Axiom.Ide` и подтвердить ожидаемый вывод;
   - после этого выбрать следующий языковой срез из уже разблокированных массивами направлений, первым кандидатом считать функции регулярных выражений, возвращающие массивы.

## Обновление плана - 2026-04-23

1. Закрыт следующий срез по функциям регулярных выражений, возвращающим массивы:
   - `REGEXMATCHES(text, pattern[, modes]) AS STRING()` возвращает все совпадения целиком;
   - `REGEXEXTRACTALL(text, pattern, group[, modes]) AS STRING()` возвращает значения указанной группы из всех совпадений;
   - `REGEXSPLIT(text, pattern[, modes]) AS STRING()` разделяет строку по регулярному выражению.
2. Для всех трёх функций зафиксирована общая массивная семантика:
   - результат создаётся как новый выделенный массив строк;
   - непустой результат получает компактные границы `1 TO N`;
   - пустой результат получает канонический пустой диапазон `1 TO 0`.
3. Решение по именам:
   - в QuickBASIC нет полноценного встроенного слоя регулярных выражений;
   - Visual Basic исторически даёт более простую проверку через `Like`;
   - для Axiom оставлены уже зарезервированные имена в линии современных операций `Regex.Match`, `Regex.Matches` и `Regex.Split`, а `REGEXEXTRACTALL` оформлена как массивная пара к `REGEXEXTRACT`.
4. Справочник, указатель, подсветка IDE, автоматические проверки и ручные `.axm`-примеры обновлены вместе с реализацией.
5. Следующий рабочий шаг:
   - вручную открыть в `Axiom.Ide` новые примеры `samples/functions/regexmatches/all_matches.axm`, `samples/functions/regexextractall/group_values.axm`, `samples/functions/regexsplit/split_on_pattern.axm` и подтвердить ожидаемый вывод;
   - после ручной проверки выбрать следующий языковой срез: либо продолжить текстовые функции вокруг регулярных выражений, либо перейти к следующему практичному направлению из справочника после аудита массивов.

## Обновление плана - 2026-04-23

1. Служебный пример `samples/smoke/repair_plans_mojibake.axm` усилен для ремонта документов с битой кодировкой:
   - пример по-прежнему чинит восстановимые фрагменты вида `РџР»...`, `СЃР»...`, `вЂ”`;
   - дополнительно он считает строки с маркерами `пїЅ` или `�`, где исходные байты уже заменены символом замены.
2. Решение по восстановлению:
   - фрагменты `пїЅ` и `�` нельзя надёжно восстановить простой перекодировкой, потому что это уже не исходный текст, а след потери байтов;
   - программа должна обнаруживать такие строки и показывать их количество, чтобы дальше чинить их вручную по контексту или искать резервную копию.
3. Следующий рабочий шаг:
   - вручную прогнать `samples/smoke/repair_plans_mojibake.axm` на рабочей копии `docs/WORKLOG2.md`;
   - если счётчик невосстановимых строк не равен нулю, восстановить эти места по истории, резервной копии или смысловому контексту.

## Обновление плана - 2026-04-23

1. После ручного запуска `samples/smoke/repair_plans_mojibake.axm` получен результат `TRUE`, `1526`, `187`, `TRUE`; стало ясно, что числовой вывод неудобен для чтения в IDE.
2. Вывод служебного примера переведён на русские подписи:
   - `Копирование исходного файла в рабочую копию: TRUE/FALSE`;
   - `Исправлено строк с восстановимой битой кодировкой: <число>`;
   - `Строк с невосстановимыми маркерами пїЅ/U+FFFD: <число>`;
   - `Запись исправленной рабочей копии: TRUE/FALSE`.
3. Следующий рабочий шаг:
   - снова вручную запустить пример в `Axiom.Ide`;
   - по строке `Строк с невосстановимыми маркерами пїЅ/U+FFFD` решить, какие участки `WORKLOG2.md` восстанавливать вручную или из резервной копии.

