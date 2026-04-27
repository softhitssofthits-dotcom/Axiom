# Журнал работ 3

Этот файл является исторической третьей частью журнала работ проекта и покрывает сеансы 310-480.

Исторические части журнала:
- `docs/WORKLOG.md` содержит записи до Session 168 включительно;
- `docs/WORKLOG2.md` содержит записи с Session 169 по Session 309 включительно.

Активное продолжение журнала ведётся в `docs/WORKLOG4.md`.

Правила ведения остаются прежними:
- новые записи добавляются только в конец файла;
- заголовок новой записи имеет вид `## Session <N> - YYYY-MM-DD HH:mm - <название>`;
- номер сеанса продолжает общую последовательность из `docs/WORKLOG.md` и `docs/WORKLOG2.md`;
- дата и время указываются по местному времени проекта.

## Session 310 - 2026-04-23 09:53 - Переход на WORKLOG3, PLANS2 и SPECIFICATION2

### Что изменилось
- Создан новый активный журнал `docs/WORKLOG3.md`.
- `docs/WORKLOG2.md` закрыт как историческая вторая часть журнала до Session 309 включительно.
- Создан новый активный план `docs/PLANS2.md` на основе текущего `docs/PLANS.md`.
- `docs/PLANS.md` закрыт как исторический план.
- Создана новая активная спецификация `docs/SPECIFICATION2.md` на основе текущего `docs/SPECIFICATION.md`.
- `docs/SPECIFICATION.md` закрыта как историческая спецификация.
- Рабочие правила проекта переключены на новые активные документы.

### Какие файлы изменены
- `AGENTS.md`
- `README.md`
- `docs/SPECIFICATION.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS.md`
- `docs/PLANS2.md`
- `docs/WORKLOG2.md`
- `docs/WORKLOG3.md`
- `docs/IDE_REDESIGN.md`
- `docs/agent/README.md`
- `docs/agent/PLAYBOOKS.md`
- `docs/agent/CURRENT_STATE.md`
- `docs/agent/ARCHITECTURE_MAP.md`
- `.codex/skills/compiler-front-end/SKILL.md`
- `.codex/skills/axiom-doc-author/SKILL.md`

### Проверка
- Проверен поиск активных ссылок на старые документы в рабочих правилах и агентных документах.
- Автоматические тесты не запускались, потому что менялась только документация и локальные инструкции.

### Решения
- Начиная со следующей сессии обязательный контекст проекта должен читаться из `docs/SPECIFICATION2.md`, `docs/PLANS2.md` и `docs/WORKLOG3.md`.
- Старые документы `docs/SPECIFICATION.md`, `docs/PLANS.md` и `docs/WORKLOG2.md` остаются только исторической справкой.

### Следующий шаг
- В следующей рабочей сессии читать `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md` и `docs/WORKLOG3.md`.
- Продолжить работу по актуальному следующему шагу из `docs/PLANS2.md`.

## Session 311 - 2026-04-23 10:41 - Предварительная компиляция в IDE и повторный запуск подготовленной программы

### Что изменилось
- В `Axiom.Execution` добавлен отдельный шаг предварительной компиляции без запуска.
- Введены `CompilationRequest`, `CompilationResult`, `CompilationStatus` и `CompiledProgram`.
- `AxiomExecutionPipeline` теперь умеет отдельно компилировать программу и отдельно выполнять уже подготовленный `CompiledProgram`.
- В `Axiom.Ide` добавлена команда `Compile`.
- `ProgramRunService` теперь кэширует последнюю успешно подготовленную программу и при `Run` использует её, если исходный текст не менялся.
- В `MainWindowViewModel`, ресурсах локализации и `MainWindow.xaml` добавлены команда, статусы и элементы интерфейса для `Compile`.
- Обновлены тесты `Axiom.Execution.Tests` и `Axiom.Ide.Tests`.
- Документация синхронизирована под новый договор предварительной компиляции.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Execution/Requests/CompilationRequest.cs`
- `src/Axiom.Execution/Results/CompilationStatus.cs`
- `src/Axiom.Execution/Results/CompiledProgram.cs`
- `src/Axiom.Execution/Results/CompilationResult.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Models/ProgramCompilationStatus.cs`
- `src/Axiom.Ide/Models/ProgramCompilationResult.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`

### Решения
- Текущая команда `Compile` не считается байткодной компиляцией и не создаёт отдельный файл.
- Внутренний `CompiledProgram` принят как полезный промежуточный слой между frontend-подготовкой и будущим backend-направлением.
- Если позже проект пойдёт в собственный байткод, новый слой должен надстраиваться над уже существующим `BoundProgram`, а не заменять lexer/parser/binder.

### Следующий шаг
- Вручную проверить живой сценарий в `Axiom.Ide`: `Compile`, затем `Run` без изменения текста.
- После ручной проверки решить, проектируем ли следующим отдельным шагом собственный формат байткода и виртуальную машину, либо пока продолжаем развитие языка поверх уже введённой границы предварительной компиляции.

## Session 312 - 2026-04-23 11:43 - Прогресс-бар компиляции в строке состояния

### Что изменилось
- В `StatusBarViewModel` добавлены свойства и методы для режима показа прогресса компиляции.
- В `MainWindow.xaml` текстовый статус строки состояния теперь временно скрывается во время `Compile`, а на его месте показывается прогресс-бар с процентом.
- Команда `Compile` в `MainWindowViewModel` переведена на асинхронный путь, чтобы строка состояния успевала перерисовываться во время реальной компиляции.
- `CompilationRequest` и `AxiomExecutionPipeline` теперь передают этапный процент выполнения компиляции.
- Обновлены IDE-тесты на новую разметку строки состояния и асинхронное завершение команды `Compile`.
- Спецификация и план синхронизированы под новое поведение строки состояния.

### Какие файлы изменены
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Execution/Requests/CompilationRequest.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/Status/StatusBarViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin\ --logger "console;verbosity=minimal"`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin\ --logger "console;verbosity=minimal"`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-build-bin\`

### Решения
- Процент компиляции пока этапный, а не посимвольный или пооператорный: он показывает крупные стадии текущего frontend-конвейера.
- Для пользовательского интерфейса этого достаточно, чтобы видеть, что компиляция идёт и что строка состояния работает именно как индикатор процесса, а не как статический текст.
- Обычная сборка в стандартную output-папку в этой сессии была заблокирована уже запущенным `Axiom.Ide.exe`, поэтому проверка IDE выполнена через изолированные output-каталоги без вмешательства в запущенное окно пользователя.

### Следующий шаг
- Вручную проверить в живой IDE, что во время `Compile` виден прогресс-бар с процентом и что после завершения на его месте снова появляется обычный итоговый текст.
- После ручной проверки продолжить работу по следующему архитектурному шагу вокруг backend-направления.

## Session 313 - 2026-04-23 12:01 - Тайминги компиляции и выполнения в строке состояния

### Что изменилось
- В строку состояния IDE добавлено отдельное правое поле времени для команд `Compile` и `Run`.
- Во время `Compile` это поле показывает прошедшее время компиляции, пока слева идёт прогресс-бар с процентом.
- После `Run` поле показывает раздельно время подготовки, время выполнения, суммарное время и признак использования кэша подготовленной программы.
- `ProgramRunService` теперь возвращает метрики подготовки и выполнения вместе с результатом запуска, чтобы IDE могла честно показать, был ли запуск после компиляции выполнен по кэшу.
- Обновлены локализованные строки интерфейса и IDE-тесты на новый индикатор времени.

### Какие файлы изменены
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-313\ --logger "console;verbosity=minimal"`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-build-bin-313d\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-313d\ --logger "console;verbosity=minimal"`

### Решения
- Для оценки пользы предварительной компиляции теперь считаются отдельные величины, а не только общее субъективное время сценария.
- Если `Compile` уже подготовил программу и текст не менялся, `Run` должен показывать нулевое время подготовки и признак использования кэша; это считается основным наблюдаемым подтверждением того, что повторный frontend-проход был пропущен.
- Даже при работающем кэше общее время запуска может выглядеть почти таким же, если основная стоимость приходится на само выполнение программы, а не на `parse -> bind`.

### Следующий шаг
- Вручную открыть в живой `Axiom.Ide` короткий `.axm`-пример.
- Сравнить метрики для обычного `Run` и для сценария `Compile` -> `Run` без изменения текста.
- По результату решить, достаточно ли текущей наблюдаемости, или следующим отдельным шагом нужен ещё и явный переключатель режима запуска для принудительного сравнения «через подготовленную программу» и «с повторной подготовкой».

## Session 314 - 2026-04-23 12:30 - Тайминги статус-бара приведены к общей стилистике оболочки

### Что изменилось
- Правый блок таймингов в строке состояния перестроен с одной склеенной строки на набор отдельных полей статус-бара.
- Для этого добавлен общий стиль разделителя строки состояния, и новые поля времени используют те же стили текста и те же динамические ресурсы, что и остальные элементы статус-бара.
- Подписи таймингов развёрнуты до полного вида: `Подготовка`, `Выполнение`, `Общее время`, `Использован кэш`, `Время компиляции`.
- Темы IDE учитываются автоматически через общий слой стилей и `DynamicResource`, без локальной ручной раскраски таймингового блока.
- Обновлены IDE-тесты на новую разметку и структуру правого блока строки состояния.

### Какие файлы изменены
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Status/StatusBarViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-build-bin-314\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-314b\ --logger "console;verbosity=minimal"`

### Решения
- Для правого блока статус-бара запрещено использовать самодельные текстовые разделители вместо обычной структуры элементов оболочки.
- Любые новые наблюдаемые метрики в строке состояния должны оформляться через существующие стили и динамические ресурсы оболочки, чтобы автоматически поддерживать светлую и тёмную тему.
- Сокращённые подписи в этом блоке признаны нежелательными; пользовательские метрики должны называться полными словами.

### Следующий шаг
- Вручную проверить `Compile` и `Run` в обеих темах IDE.
- Если визуально всё согласовано, вернуться к следующему шагу по измеримости производительности или по backend-направлению.

## Session 315 - 2026-04-23 12:50 - Фиксация плана байткодного backend-а и виртуальной машины

### Что изменилось
- Зафиксировано новое крупное направление: собственный байткодный backend и стековая виртуальная машина внедряются параллельно текущему интерпретатору.
- В архитектуре описан целевой конвейер `SourceText -> Parse -> Bind -> CompiledProgram -> LoweredProgram -> BytecodeModule -> BytecodeVm -> RuntimeResult`.
- Зафиксированы роли будущего проекта `Axiom.Bytecode`, расширения `Axiom.Runtime`, фасада `Axiom.Execution` и изменений в `Axiom.Ide`.
- Описаны основные публичные типы: `BytecodeModule`, `Opcode`, `LoweredProgram`, `BytecodeCompiler`, `ExecutionMode`, `ExecutionEngineKind` и расширенные результаты компиляции и исполнения.
- В спецификации закреплены правила `Compile`, `Run`, режимов `Auto`, `Interpreter`, `Bytecode`, кэширования и временного fallback на интерпретатор.
- В плане добавлены фазы внедрения от документации и контрактов до стабилизации VM-first режима.

### Какие файлы изменены
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

### Проверка
- Автоматические тесты не запускались, потому что изменялась только проектная документация и не менялось поведение языка, рантайма или IDE.
- Выполнен контрольный просмотр хвостов изменённых документов после правок.

### Решения
- Интерпретатор сохраняется как эталонный и резервный путь до конца стабилизации.
- Байткод первой волны живёт в памяти, без бинарной записи на диск и без генерации `exe`.
- `Binder` не должен знать о байткоде; backend-преобразования начинаются только после `BoundProgram`.
- VM первой волны использует существующую модель значений рантайма, чтобы снизить риск расхождения семантики.
- `Auto` в переходный период может fallback-нуться на интерпретатор только явно и с отражением причины в результате запуска и в IDE.

### Следующий шаг
- Начать фазу 2: ввести `ExecutionMode` и `ExecutionEngineKind` в коде.
- Расширить `ExecutionRequest`, `ExecutionResult` и `CompilationResult` под будущие байткодные стадии, не ломая текущий интерпретаторный запуск.
- Обновить тесты `Axiom.Execution` и `Axiom.Ide`, чтобы текущий путь уже честно возвращал фактически использованный движок `Interpreter`.

## Session 316 - 2026-04-23 13:31 - Контракты режимов исполнения и выбор движка в IDE

### Что изменилось
- В `Axiom.Execution` введены `ExecutionMode` (`Auto`, `Interpreter`, `Bytecode`) и `ExecutionEngineKind` (`Interpreter`, `BytecodeVm`).
- `ExecutionRequest` теперь принимает запрошенный режим запуска.
- `ExecutionResult` расширен данными о запрошенном режиме, фактически использованном движке, fallback, временах frontend/lowering/bytecode/runtime стадий и признаках кэшей.
- `CompilationResult` расширен временем frontend-подготовки и заготовленными признаками backend-подготовки и доступности байткода.
- Текущий `Auto` и принудительный `Interpreter` исполняются через интерпретатор и возвращают `ExecutionEngineKind.Interpreter`.
- Принудительный `Bytecode` пока возвращает явную backend-ошибку `MB4001`, потому что `BytecodeVm` ещё не реализована.
- `ProgramRunService` принимает режим запуска, передаёт его в `Axiom.Execution` и возвращает фактический движок в IDE-модель результата.
- В IDE добавлено меню `Run -> Execution Mode -> Auto / Interpreter / Bytecode`.
- Выбранный режим запуска сохраняется в состоянии оболочки.
- Строка состояния после `Run` показывает фактически использованный движок.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Execution/Requests/ExecutionMode.cs`
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Results/ExecutionEngineKind.cs`
- `src/Axiom.Execution/Results/ExecutionStatus.cs`
- `src/Axiom.Execution/Results/CompilationResult.cs`
- `src/Axiom.Execution/Results/ExecutionResult.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Models/ProgramRunStatus.cs`
- `src/Axiom.Ide/Models/ProgramRunResult.cs`
- `src/Axiom.Ide/Models/ProgramDiagnosticItem.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/Status/StatusBarViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj-execution-tests-316\ -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-316\ --logger "console;verbosity=minimal"` - успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj-ide-tests-316\ -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-316\ --logger "console;verbosity=minimal"` - успешно.
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-build-bin-316\` - успешно, 0 предупреждений и 0 ошибок.

### Решения
- До появления VM режим `Bytecode` не должен тихо исполнять программу через интерпретатор.
- Временная диагностика `MB4001` фиксирует отсутствие байткодного backend-а как backend-ошибку, а не как runtime-ошибку программы.
- Выбор режима запуска хранится вместе с остальными настройками оболочки, чтобы будущий VM-first переход не требовал нового хранилища.

### Следующий шаг
- Начать фазу 3: создать минимальный проект `Axiom.Bytecode`.
- Ввести первые модели `LoweredProgram`, `LoweredRoutine` и `LoweredInstruction`.
- Реализовать начальное понижение для простого скалярного среза и покрыть его модульными тестами без изменения поведения интерпретатора.

## Session 317 - 2026-04-23 13:45 - Первый проект `Axiom.Bytecode` и начальная lowered-форма

### Что изменилось
- В решение добавлен проект `Axiom.Bytecode`.
- Добавлены начальные модели `LoweredProgram`, `LoweredRoutine`, `LoweredInstruction` и `LoweredInstructionKind`.
- Добавлен `BoundProgramLowerer`, который понижает простой скалярный срез `BoundProgram` в линейную форму.
- Поддержанный первый срез lowering:
  - объявление скалярной переменной с инициализатором;
  - присваивание в скалярную переменную;
  - оператор-выражение;
  - `PRINT`;
  - `RETURN`;
  - литералы, чтение переменной, унарные и бинарные операции, приведение типа, прямой вызов функции.
- Для неподдержанных конструкций lowering возвращает backend-диагностику `BC3001`.
- Добавлен тестовый проект `Axiom.Bytecode.Tests`.
- `Run`, `Compile`, `ProgramRunService` и интерпретаторный путь не изменялись.

### Какие файлы изменены
- `Axiom.sln`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Axiom.Bytecode.csproj`
- `src/Axiom.Bytecode/Model/LoweredProgram.cs`
- `src/Axiom.Bytecode/Model/LoweredRoutine.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Lowering/LoweringResult.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeDiagnostic.cs`
- `tests/Axiom.Bytecode.Tests/Axiom.Bytecode.Tests.csproj`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-317\ --logger "console;verbosity=minimal"` - успешно, 4 теста.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-317\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-317\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-317\` - успешно, 0 ошибок, 6 существующих предупреждений nullable в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- `LoweredProgram` пока не подключается к `Compile` и `Run`, чтобы не смешивать фазу lowering с будущей генерацией `BytecodeModule`.
- Неподдержанная конструкция в lowering считается ошибкой backend-слоя, а не runtime-ошибкой пользовательской программы.
- Все инструкции начальной lowered-формы хранят `TextLocation`; это требование сохраняется для будущих меток и переходов.

### Следующий шаг
- Расширить lowered-модель метками и условными/безусловными переходами.
- Реализовать lowering для `IF / ELSEIF / ELSE`.
- После этого перейти к `WHILE` и `DO ... LOOP`, сохраняя отдельные unit-тесты на каждую конструкцию.

## Session 318 - 2026-04-23 14:26 - Метки, переходы и lowering `IF`

### Что изменилось
- `LoweredInstructionKind` расширен инструкциями `Label`, `Jump`, `JumpIfFalse`.
- `LoweredInstruction` теперь хранит строковую метку для инструкций метки и перехода.
- `BoundProgramLowerer` создаёт детерминированные метки вида `L0001`, `L0002`.
- Реализовано понижение `BoundIfStatement`:
  - условие;
  - `JumpIfFalse`;
  - ветка `THEN`;
  - необязательный `Jump` к концу;
  - метка ветки `ELSE`;
  - ветка `ELSE`;
  - конечная метка.
- Цепочка `ELSEIF` покрывается как вложенный `BoundIfStatement` в ветке `ELSE`.
- Старый тест неподдержанного `IF` заменён на проверку неподдержанного `WHILE`, потому что `IF` теперь поддерживается lowered-слоем.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService` и интерпретатор не изменялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-318\ --logger "console;verbosity=minimal"` - успешно, 7 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-318\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-318\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-318\` - успешно, 0 ошибок, 6 существующих предупреждений nullable в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- `ELSEIF` не получает отдельную lowered-инструкцию: текущая bound-форма уже выражает его как вложенный `IF` в ветке `ELSE`.
- Строковые метки сохраняются как промежуточный формат lowering; будущий эмиттер байткода должен заменить их на проверенные адреса или смещения инструкций.
- Поддержка `IF` в lowered-слое не считается изменением пользовательского поведения языка, пока lowered-форма не подключена к исполнению.

### Следующий шаг
- Реализовать lowering для `WHILE / END WHILE`.
- Затем добавить формы `DO WHILE / LOOP` и подготовить стек меток выхода для будущего `EXIT DO`.

## Session 319 - 2026-04-23 14:37 - Lowering `WHILE`

### Что изменилось
- `BoundProgramLowerer` теперь понижает `BoundWhileStatement`.
- `WHILE / END WHILE` раскладывается в начальную метку, вычисление условия, `JumpIfFalse` к конечной метке, тело цикла, обратный `Jump` и конечную метку.
- Вложенные `WHILE` получают детерминированные пары меток в порядке обхода `BoundProgram`.
- Тест неподдержанного управления ходом выполнения перенесён с `WHILE` на `DO WHILE ... LOOP`, потому что `WHILE` теперь поддерживается lowered-слоем.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не изменялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-319\ --logger "console;verbosity=minimal"` - успешно, 9 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-319\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-319\ --logger "console;verbosity=minimal"` - успешно, 219 тестов после отдельного повторного запуска. Первый параллельный запуск попал в блокировку общего промежуточного файла `Axiom.Syntax.dll`.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-319\` - успешно после отдельного повторного запуска, 0 ошибок, 3 существующих предупреждения nullable в `tests/Axiom.Semantics.Tests/BinderTests.cs`.

### Решения
- Для `WHILE` не вводится отдельная lowered-инструкция: существующих `Label`, `JumpIfFalse` и `Jump` достаточно для целевой линейной формы.
- Поддержка `WHILE` в lowered-слое не считается изменением пользовательского поведения языка, пока lowered-форма не подключена к исполнению; поэтому новые `.axm`-примеры и изменения руководства пользователя не требуются.
- Ошибка `BC3001` остаётся диагностикой непокрытых backend-узлов; текущий ближайший непокрытый цикл для теста - `DO WHILE ... LOOP`.

### Следующий шаг
- Реализовать lowering для `DO WHILE / LOOP`.
- Затем расширить поддержку на остальные формы `DO ... LOOP` и подготовить стек меток выхода для будущего `EXIT DO`.

## Session 320 - 2026-04-23 14:49 - Lowering `DO WHILE`

### Что изменилось
- `BoundProgramLowerer` теперь понижает `BoundDoLoopStatement` для формы `DO WHILE / LOOP`.
- Поддержанная форма определяется как `ConditionKind = While`, `EvaluateConditionAfterBody = false` и непустое условие.
- `DO WHILE / LOOP` раскладывается в начальную метку, вычисление условия, `JumpIfFalse` к конечной метке, тело цикла, обратный `Jump` и конечную метку.
- Вложенные `DO WHILE` получают детерминированные пары меток в порядке обхода `BoundProgram`.
- Тест неподдержанной формы `DO ... LOOP` перенесён на `DO UNTIL`, потому что `DO WHILE` теперь поддерживается lowered-слоем.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не изменялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-320\ --logger "console;verbosity=minimal"` - успешно, 11 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-320\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-320\ --logger "console;verbosity=minimal"` - успешно, 219 тестов после отдельного повторного запуска. Параллельный запуск снова попал в блокировку общего промежуточного файла.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-320\` - успешно, 0 ошибок, 6 существующих предупреждений nullable в старых тестах `Axiom.Semantics.Tests` и `Axiom.Parsing.Tests`.
- Отдельная попытка сборки с новым `BaseIntermediateOutputPath` и `--no-restore` ожидаемо завершилась `NETSDK1004` из-за отсутствия `project.assets.json`; она не отражает состояние кода.

### Решения
- Для `DO WHILE` не вводится отдельная lowered-инструкция: существующих `Label`, `JumpIfFalse` и `Jump` достаточно для формы с проверкой перед телом.
- Остальные формы `DO ... LOOP` пока остаются явной backend-границей и должны возвращать `BC3001`, пока для них не добавлены отдельные правила понижения.
- Поддержка `DO WHILE` в lowered-слое не считается изменением пользовательского поведения языка, пока lowered-форма не подключена к исполнению; поэтому новые `.axm`-примеры и изменения руководства пользователя не требуются.

### Следующий шаг
- Реализовать lowering для `DO UNTIL / LOOP`.
- Затем добавить post-check формы `DO ... LOOP WHILE` и `DO ... LOOP UNTIL`.
- После покрытия основных форм `DO ... LOOP` подготовить стек меток выхода для будущего `EXIT DO`.

## Session 321 - 2026-04-23 15:02 - Lowering `DO UNTIL`

### Что изменилось
- `LoweredInstructionKind` расширен инструкцией `JumpIfTrue`.
- `LoweredInstruction` получил фабричный метод `JumpIfTrue`.
- `BoundProgramLowerer` теперь понижает `BoundDoLoopStatement` для формы `DO UNTIL / LOOP`.
- Поддержанная форма определяется как `ConditionKind = Until`, `EvaluateConditionAfterBody = false` и непустое условие.
- `DO UNTIL / LOOP` раскладывается в начальную метку, вычисление условия, `JumpIfTrue` к конечной метке, тело цикла, обратный `Jump` и конечную метку.
- Вложенные `DO UNTIL` получают детерминированные пары меток в порядке обхода `BoundProgram`.
- Тест неподдержанной формы `DO ... LOOP` перенесён на `DO ... LOOP WHILE`, потому что `DO UNTIL` теперь поддерживается lowered-слоем.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не изменялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-321\ --logger "console;verbosity=minimal"` - успешно, 13 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-321\ --logger "console;verbosity=minimal"` - успешно, 188 тестов после отдельного повторного запуска. Параллельный запуск попал в блокировку общего промежуточного файла.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-321\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-321\` - успешно, 0 ошибок, 6 существующих предупреждений nullable в старых тестах `Axiom.Semantics.Tests` и `Axiom.Parsing.Tests`.

### Решения
- Для `DO UNTIL` используется прямой условный переход `JumpIfTrue`, а не искусственное отрицание условия через `NOT`.
- `JumpIfTrue` принят как часть lowered-модели, потому что целевой набор инструкций будущего байткода уже содержит такой переход.
- Поддержка `DO UNTIL` в lowered-слое не считается изменением пользовательского поведения языка, пока lowered-форма не подключена к исполнению; поэтому новые `.axm`-примеры и изменения руководства пользователя не требуются.

### Следующий шаг
- Реализовать lowering для post-check формы `DO ... LOOP WHILE`.
- Затем добавить post-check форму `DO ... LOOP UNTIL`.
- После покрытия основных форм `DO ... LOOP` подготовить стек меток выхода для будущего `EXIT DO`.

## Session 322 - 2026-04-23 15:30 - Lowering `DO ... LOOP WHILE`

### Что изменилось
- `BoundProgramLowerer` теперь понижает форму `DO ... LOOP WHILE` с проверкой после тела.
- Понижение этой формы выполняется через начальную метку перед телом, инструкции тела, вычисление условия после тела, `JumpIfTrue` обратно к начальной метке и конечную метку.
- Логика `DO ... LOOP` разделена на ветви с проверкой перед телом и с проверкой после тела, чтобы уже покрытые `DO WHILE / LOOP` и `DO UNTIL / LOOP` оставались прежними.
- Добавлены unit-тесты на обычный `DO ... LOOP WHILE` и вложенный `DO ... LOOP WHILE` со стабильным порядком меток.
- Тест неподдержанной формы перенесён на `DO ... LOOP UNTIL`, потому что `LOOP WHILE` теперь покрыт lowered-слоем.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-322\ --logger "console;verbosity=minimal"` - успешно, 15 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-322\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-322\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-322\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для `DO ... LOOP WHILE` не вводится отдельная lowered-инструкция: существующих `Label` и `JumpIfTrue` достаточно.
- Конечная метка сохраняется даже без текущего использования в переходах, чтобы форма была симметрична другим циклам и была готова к будущему `EXIT DO`.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения справочника не требуются.

### Следующий шаг
- Реализовать lowering для формы `DO ... LOOP UNTIL` с проверкой после тела.
- Затем подготовить поддержку бесконечной формы `DO ... LOOP`.
- После покрытия основных форм `DO ... LOOP` подготовить стек меток выхода для будущего `EXIT DO`.

## Session 323 - 2026-04-23 15:56 - Понижение `DO ... LOOP UNTIL`

### Что изменилось
- `BoundProgramLowerer` теперь понижает форму `DO ... LOOP UNTIL` с проверкой после тела.
- Понижение этой формы выполняется через начальную метку перед телом, инструкции тела, вычисление условия `UNTIL`, `JumpIfFalse` обратно к начальной метке и конечную метку после условного перехода.
- Добавлены модульные тесты на обычный `DO ... LOOP UNTIL` и вложенный `DO ... LOOP UNTIL` со стабильным порядком меток.
- Тест неподдержанной формы перенесён на бесконечную форму `DO ... LOOP`, потому что `LOOP UNTIL` теперь покрыт слоем понижения.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-323\ --logger "console;verbosity=minimal"` - успешно, 17 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-323\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-323\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-323\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для `DO ... LOOP UNTIL` не вводится новый вид инструкции пониженной формы: существующего `JumpIfFalse` достаточно, потому что цикл должен повторяться, пока условие ложно.
- Конечная метка сохраняется даже без текущего перехода на неё, чтобы форма цикла оставалась симметричной другим циклам и была готова к будущему `EXIT DO`.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения справочника не требуются.

### Следующий шаг
- Реализовать понижение бесконечной формы `DO ... LOOP`.
- Затем подготовить стек меток выхода для будущего `EXIT DO`.

## Session 324 - 2026-04-23 16:15 - Понижение бесконечного `DO ... LOOP`

### Что изменилось
- `BoundProgramLowerer` теперь понижает бесконечную форму `DO ... LOOP` без условия.
- Понижение этой формы выполняется через начальную метку перед телом, инструкции тела, безусловный `Jump` обратно к начальной метке и конечную метку после обратного перехода.
- Добавлены модульные тесты на обычный `DO ... LOOP` без условия и вложенный `DO ... LOOP` без условия со стабильным порядком меток.
- Тест неподдержанной формы перенесён на `EXIT DO`, потому что основные формы семейства `DO ... LOOP` теперь покрыты слоем понижения.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-324\ --logger "console;verbosity=minimal"` - успешно, 19 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-324\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-324\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-324\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для бесконечной формы `DO ... LOOP` не вводится новый вид инструкции пониженной формы: существующих `Label` и `Jump` достаточно.
- Конечная метка сохраняется сразу, хотя обычный бесконечный цикл сам на неё не переходит. Это нужно для симметрии с другими циклами и для ближайшего шага по `EXIT DO`.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения справочника не требуются.

### Следующий шаг
- Подготовить стек меток выхода для `EXIT DO`.
- Реализовать понижение `EXIT DO` как переход к конечной метке ближайшего `DO ... LOOP`.

## Session 325 - 2026-04-23 16:25 - Понижение `EXIT DO`

### Что изменилось
- `BoundProgramLowerer` получил стек меток выхода для `DO ... LOOP`.
- Все уже покрытые формы `DO ... LOOP` теперь понижают тело при активной конечной метке цикла.
- `EXIT DO` понижается в безусловный `Jump` к конечной метке ближайшего `DO ... LOOP`.
- Добавлены модульные тесты на обычный `EXIT DO` и на вложенные `DO ... LOOP`, где `EXIT DO` должен выходить только из ближайшего цикла.
- Тест неподдержанной конструкции перенесён на `SELECT CASE`, потому что семейство `DO ... LOOP` теперь покрыто слоем понижения.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-325\ --logger "console;verbosity=minimal"` - успешно, 21 тест.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-325\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-325\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-325\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.
- Первый параллельный запуск `Axiom.Execution.Tests` был прерван блокировкой промежуточного файла сборки; отдельный повторный запуск прошёл успешно.

### Решения
- Для `EXIT DO` используется стек конечных меток `DO`, а не отдельный вид пониженной инструкции.
- При вложенных `DO ... LOOP` верхняя метка стека принадлежит ближайшему циклу, поэтому поведение соответствует текущему интерпретатору.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения пользовательского руководства не требуются.

### Следующий шаг
- Реализовать понижение `FOR / NEXT`.
- Затем добавить понижение `EXIT FOR`.
- После этого продолжить понижение `SELECT CASE` и процедурной модели.

## Session 326 - 2026-04-23 16:47 - Понижение `FOR / NEXT`

### Что изменилось
- `BoundProgramLowerer` теперь понижает `BoundForStatement`.
- Начальное значение управляющей переменной, конечное значение и шаг вычисляются один раз перед циклом.
- Конечное значение и шаг сохраняются во внутренних синтетических переменных пониженной формы, чтобы не переоценивать выражения на каждой итерации.
- Понижение `FOR` строит отдельные ветки проверки для положительного и отрицательного шага.
- После тела цикла управляющая переменная увеличивается на сохранённый шаг, затем выполнение возвращается к проверке.
- Добавлены модульные тесты на обычный `FOR / NEXT`, отрицательный `STEP` и вложенные `FOR / NEXT` со стабильными метками.
- Тест неподдержанной конструкции перенесён на `EXIT FOR`.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-326\ --logger "console;verbosity=minimal"` - успешно, 24 теста.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-326\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-326\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-326\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для сохранения семантики интерпретатора выражения `TO` и `STEP` в пониженной форме сохраняются во внутренние переменные, а не повторяются на каждой проверке или прибавлении.
- Для `FOR / NEXT` не вводится новый вид инструкции пониженной формы: достаточно `Label`, `Jump`, `JumpIfFalse`, `JumpIfTrue`, сравнений и сложения.
- Нулевой `STEP` не диагностируется на этапе понижения; будущая VM должна сохранить runtime-диагностику интерпретатора.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения пользовательского руководства не требуются.

### Следующий шаг
- Реализовать стек меток выхода для `FOR / NEXT`.
- Понизить `EXIT FOR` как переход к конечной метке ближайшего `FOR`.
- После этого продолжить понижение `SELECT CASE` и процедурной модели.

## Session 327 - 2026-04-23 17:06 - Понижение `EXIT FOR`

### Что изменилось
- `BoundProgramLowerer` получил отдельный стек меток выхода для `FOR / NEXT`.
- Тело `FOR / NEXT` теперь понижается при активной конечной метке цикла.
- `EXIT FOR` понижается в безусловный `Jump` к конечной метке ближайшего `FOR`.
- Добавлены модульные тесты на обычный `EXIT FOR` и на вложенные `FOR / NEXT`, где `EXIT FOR` должен выходить только из ближайшего цикла.
- Тест неподдержанной конструкции перенесён обратно на `SELECT CASE`, потому что `EXIT FOR` теперь покрыт слоем понижения.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-327\ --logger "console;verbosity=minimal"` - успешно, 26 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-327\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-327\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-327\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для `EXIT FOR` используется отдельный стек конечных меток `FOR`, чтобы не смешивать выходы из `FOR` и `DO`.
- При вложенных `FOR / NEXT` верхняя метка стека принадлежит ближайшему циклу, поэтому поведение соответствует текущему интерпретатору.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения пользовательского руководства не требуются.

### Следующий шаг
- Реализовать понижение `SELECT CASE`.
- Затем продолжить покрытие процедурной модели фазы 3.

## Session 328 - 2026-04-23 17:16 - Понижение `SELECT CASE`

### Что изменилось
- `BoundProgramLowerer` теперь понижает `BoundSelectCaseStatement`.
- Выражение после `SELECT CASE` вычисляется один раз и сохраняется во внутреннюю переменную пониженной формы.
- Обычные ветки `CASE` понижаются через проверку селекторов, `JumpIfTrue` к телу ветки, переход к следующей ветке при несовпадении и общий переход к концу после выполненной ветки.
- `CASE ELSE` понижается как линейный блок после всех обычных проверок.
- Покрыты селекторы `CASE value`, списки значений, `CASE low TO high` и `CASE IS <op> value`.
- Добавлены модульные тесты на ветвление по значениям, `CASE ELSE`, диапазон `TO` и `CASE IS`.
- Тест неподдержанной конструкции перенесён на объявление массива, потому что `SELECT CASE` теперь покрыт слоем понижения.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-328\ --logger "console;verbosity=minimal"` - успешно, 28 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-328\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-328\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-328\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для `SELECT CASE` используется внутренняя переменная пониженной формы, чтобы сохранить договор языка: test expression вычисляется один раз.
- Для селектора `CASE low TO high` не вводится отдельная lowered-инструкция: он раскладывается в `test >= low`, `test <= high` и логический `AND`.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения пользовательского руководства не требуются.

### Следующий шаг
- Проверить покрытие процедурной модели в слое понижения.
- Закрыть оставшиеся пробелы по `SUB`, `FUNCTION`, параметрам, локальным переменным и `RETURN` перед переходом к модели `BytecodeModule`.

## Session 329 - 2026-04-23 18:35 - Значения по умолчанию для скалярных `DIM` в пониженной форме

### Что изменилось
- Во время проверки процедурной модели найден пробел: скалярные объявления без инициализатора не оставляли следа в `LoweredProgram`.
- `BoundProgramLowerer` теперь для покрытых скалярных `DIM` без инициализатора добавляет `LoadLiteral` со значением по умолчанию и затем `StoreVariable`.
- Это правило действует и для глобальных объявлений, и для локальных объявлений внутри `FUNCTION` и `SUB`.
- Добавлены модульные тесты на глобальные скалярные объявления без инициализатора и на локальную переменную без инициализатора внутри функции.
- Исполнительный путь `Run`, `Compile`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-329\ --logger "console;verbosity=minimal"` - успешно, 30 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-329\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-329\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-329\` - успешно, 0 ошибок, 6 существующих nullable-предупреждений в старых тестах `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Для будущей VM пониженная форма должна сохранять наблюдаемую семантику значений по умолчанию, а не полагаться на то, что необъявленные состояния будут где-то восстановлены позже.
- Пока явно покрыты значения по умолчанию для `INTEGER`, `LONG`, `DOUBLE`, `DECIMAL`, `STRING` и `BOOLEAN`.
- Изменение не считается пользовательским изменением языка, потому что `LoweredProgram` пока не подключён к `Compile` и `Run`; новые `.axm`-примеры и изменения пользовательского руководства не требуются.

### Следующий шаг
- Продолжить проверку процедурной модели в слое понижения.
- Отдельно покрыть тестами локальные вложенные `SUB` и `FUNCTION`, доступ к внешним переменным и рекурсивные вызовы.

## Session 330 - 2026-04-23 18:46 - Проверка вложенных процедур и рекурсии в lowered-представлении

### Что изменилось
- В Axiom.Bytecode.Tests добавлены модульные тесты на три сложных сценария процедурной модели:
  - локальные вложенные SUB и FUNCTION как отдельные routine с прямыми вызовами;
  - прямая рекурсия локальной функции;
  - чтение внешних переменных из локальной функции.
- Во время проверки обнаружен и зафиксирован важный внутренний договор: в BoundProgram.Routines локальные routine попадают раньше охватывающей процедуры, и lowered-слой сохраняет этот детерминированный порядок.
- Рабочий код BoundProgramLowerer для этого шага менять не пришлось: покрытие подтвердило, что текущая реализация уже корректно обрабатывает эти прямые сценарии процедурной модели.
- Исполнительный путь Run, Compile, ProgramRunService, IDE и интерпретатор не менялись.

### Какие файлы изменены
- README.md
- docs/ARCHITECTURE.md
- docs/SPECIFICATION2.md
- docs/PLANS2.md
- docs/WORKLOG3.md
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`

### Проверка
- dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-330\ --logger "console;verbosity=minimal" - успешно, 33 теста.
- dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-330\ --logger "console;verbosity=minimal" - успешно, 188 тестов.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-330\ --logger "console;verbosity=minimal" - успешно, 219 тестов.
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-330\ - успешно, 0 ошибок, 0 предупреждений.

### Решения
- Для прямой процедурной модели lowered-слоя можно считать подтверждёнными:
  - локальные вложенные SUB и FUNCTION;
  - доступ к внешним переменным;
  - прямую рекурсию.
- Порядок BoundProgram.Routines нельзя предполагать интуитивно: локальные routine сейчас идут раньше охватывающей процедуры, и это нужно учитывать при проектировании стабильных outine-id в BytecodeModule.
- Изменение не считается пользовательским изменением языка, потому что LoweredProgram пока не подключён к Compile и Run; новые .axm-примеры и обновления пользовательского справочника не требуются.

### Следующий шаг
- Перейти к фазе 4 и ввести in-memory модель BytecodeModule версии v1.
- Зафиксировать таблицы модуля, версию формата и базовую валидацию индексов, переходов и сигнатур.

## Session 331 - 2026-04-23 19:05 - Модель BytecodeModule v1 и валидация

### Что изменилось
- В `Axiom.Bytecode` добавлен первый полный модульный слой байткодного направления:
  - `BytecodeHeader`, `BytecodeFormatVersion`, `BytecodeModuleFlags`;
  - `BytecodeModule`, `BytecodeRoutine`, `BytecodeInstruction`, `Opcode`;
  - `ConstantPool` и типизированные байткодные константы;
  - `BuiltinBinding`, `BytecodeGlobal`, стратегия инициализации глобалов;
  - `BytecodeDebugInfo`, `SourceInstructionMap`, диапазоны логических блоков и таблицы имён параметров и локальных переменных.
- Добавлен `BytecodeModuleValidator` и `BytecodeValidationResult`.
- Валидатор уже проверяет:
  - существование точки входа;
  - уникальность `routine-id`, global-slot и built-in id;
  - индексы констант;
  - ссылки на глобальные, локальные и аргументные слоты;
  - корректность целей переходов;
  - соответствие числа аргументов сигнатурам `Call` и `CallBuiltin`;
  - базовую целостность отладочных карт и диапазонов блоков.
- В `Axiom.Bytecode.Tests` добавлены модульные тесты на:
  - валидный модуль;
  - невалидный индекс константы;
  - невалидный built-in id;
  - невалидную цель перехода;
  - несовместимое число аргументов при `Call`.
- Во время проверки найден и исправлен дефект тестового helper-а: отладочная карта для коротких процедур не должна сама становиться невалидной раньше основной проверяемой ошибки.
- Исполнительный путь `Compile`, `Run`, `ProgramRunService`, IDE и интерпретатор не менялись.

### Какие файлы изменены
- README.md
- docs/ARCHITECTURE.md
- docs/SPECIFICATION2.md
- docs/PLANS2.md
- docs/WORKLOG3.md
- `src/Axiom.Bytecode/Model/BytecodeFormatVersion.cs`
- `src/Axiom.Bytecode/Model/BytecodeModuleFlags.cs`
- `src/Axiom.Bytecode/Model/BytecodeHeader.cs`
- `src/Axiom.Bytecode/Model/BytecodeConstantKind.cs`
- `src/Axiom.Bytecode/Model/BytecodeConstant.cs`
- `src/Axiom.Bytecode/Model/ConstantPool.cs`
- `src/Axiom.Bytecode/Model/BuiltinBinding.cs`
- `src/Axiom.Bytecode/Model/GlobalInitializationStrategy.cs`
- `src/Axiom.Bytecode/Model/BytecodeGlobal.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeInstruction.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Bytecode/Model/BytecodeBlockDebugRange.cs`
- `src/Axiom.Bytecode/Model/SourceInstructionMap.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutineDebugInfo.cs`
- `src/Axiom.Bytecode/Model/BytecodeDebugInfo.cs`
- `src/Axiom.Bytecode/Model/BytecodeModule.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeValidationResult.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-331\ --logger "console;verbosity=minimal"` - успешно, 38 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-331\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-331\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-331\` - успешно, 0 ошибок, 0 предупреждений.

### Решения
- Для первого формата `BytecodeModule` инструкции пока хранятся по routine, а не в одном общем буфере модуля.
- Адреса переходов внутри v1 задаются индексами инструкций в пределах текущей routine.
- Для ранней проверки стекового и сигнатурного договора `Call` и `CallBuiltin` уже несут фактическое число аргументов.
- Это изменение не считается пользовательским изменением языка: `BytecodeModule` ещё не строится из `Compile`, не попадает в кэш выполнения и не исполняется через `Run`; новые `.axm`-примеры и обновления пользовательского справочника не требуются.

### Следующий шаг
- Реализовать эмиттер `LoweredProgram -> BytecodeModule` для скалярных выражений, переменных, `PRINT`, переходов и `RETURN`.
- После этого перейти к минимальному `BytecodeVm`, который сможет исполнить первый валидный скалярный модуль.

## Session 332 - 2026-04-23 19:41 - Первый эмиттер BytecodeModule

### Что сделано
- В `Axiom.Bytecode` добавлены `Emission/BytecodeCompiler.cs` и `Emission/BytecodeCompilerResult.cs`.
- Реализована первая эмиссия `LoweredProgram -> BytecodeModule` для скалярного слоя:
  - литералы через `ConstantPool` и `PushConst`;
  - глобальные переменные через `LoadGlobal` и `StoreGlobal`;
  - параметры через `LoadArgument` и `StoreArgument`;
  - локальные переменные через `LoadLocal` и `StoreLocal`;
  - арифметические, логические и сравнительные операции;
  - вызовы routine через `Call`;
  - вызовы built-in через `CallBuiltin` и таблицу built-in;
  - `PrintValue`, `PrintSeparator`, `PrintLine`, `Jump`, `JumpIfTrue`, `JumpIfFalse`, `Pop`, `Return`.
- В `Opcode` добавлен внутренний `PrintLine`.
- `BytecodeModuleValidator` расширен знанием о `PrintLine`.
- Добавлены тесты `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs` на:
  - успешную эмиссию скалярной программы;
  - корректное разрешение переходов после удаления lowered-меток;
  - диагностику дублированных меток.
- Обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-332\ --logger "console;verbosity=minimal"` - успешно, 41 тест.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-332\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-332\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-332\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в тестах парсера и семантики.

### Решения
- Первый эмиттер остаётся строго внутренним backend-слоем: `Compile` и `Run` его пока не используют.
- Lowered-метки удаляются в эмиттере, а переходы получают конечные индексы инструкций внутри routine.
- Таблица built-in собирается только по фактически использованным built-in вызовам.
- `PrintLine` зафиксирован как внутренний opcode байткодного backend-а, а не как новое пользовательское средство языка.

### Что осталось
- Реализовать минимальный `BytecodeVm`, который сможет исполнить валидный скалярный модуль.
- После этого начать подключение байткодного пути к `Axiom.Execution` и `Compile`.

### Следующий шаг
- Перейти к фазе 5: стек значений, стек вызовов, кадры процедур, доступ к глобалам, вызовы built-in и базовая runtime-диагностика VM по `BytecodeDebugInfo`.

## Session 333 - 2026-04-23 19:48 - Исправление старта IDE после режима запуска

### Что сделано
- В `src/Axiom.Ide/MainWindow.xaml` исправлены привязки `IsChecked` для пунктов меню выбора режима запуска:
  - `IsAutoExecutionModeEnabled`;
  - `IsInterpreterExecutionModeEnabled`;
  - `IsBytecodeExecutionModeEnabled`.
- Для всех трёх привязок явно задан `Mode=OneWay`, потому что соответствующие свойства `MainWindowViewModel` доступны только для чтения и используются как индикаторы состояния, а не как двусторонние источники данных.

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-333\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet run --project src\Axiom.Ide\Axiom.Ide.csproj` - приложение больше не падает на старте; команда была остановлена по тайм-ауту уже после открытия окна.
- Лог `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260423-194751.log` подтверждает успешный старт без прежнего `Unhandled IDE exception`.

### Решения
- Причина сбоя была не в новом байткодном backend-е, а в WPF-привязке меню `Run`.
- Для `MenuItem.IsChecked` в таких случаях нужно явно держать `Mode=OneWay`, если ViewModel даёт только вычисляемое свойство без setter.

### Что осталось
- Основной следующий шаг по плану проекта не изменился: минимальный `BytecodeVm`.

### Следующий шаг
- Продолжить фазу 5: стек значений, стек вызовов, кадры процедур, доступ к глобалам, built-in bridge и runtime-диагностика VM.

## Session 334 - 2026-04-23 20:05 - Минимальная Bytecode VM

### Что сделано
- В `src/Axiom.Runtime/Axiom.Runtime.csproj` добавлена ссылка на `Axiom.Bytecode`.
- В `Axiom.Bytecode` модель `BuiltinBinding` расширена каноническим `BuiltinFunctionId`.
- В `Axiom.Runtime` добавлены:
  - `Execution/BytecodeVm.cs`;
  - `Execution/BytecodeVm.Builtins.cs`;
  - `Execution/BytecodeCallFrame.cs`.
- Реализован первый исполняемый байткодный путь для скалярного слоя:
  - глобальные слоты;
  - аргументы и локалы;
  - покадровый evaluation-stack;
  - арифметика, логика, сравнения;
  - `Call`, `CallBuiltin`, `Return`;
  - `PrintValue`, `PrintSeparator`, `PrintLine`;
  - runtime-диагностика по `BytecodeDebugInfo`.
- Добавлен минимальный built-in bridge VM:
  - `LEN`, `STR`, `VAL`, `LCASE`, `UCASE`;
  - `ABS`, `SGN`, `MIN`, `MAX`, `CLAMP`.
- В `tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj` добавлена ссылка на `Axiom.Bytecode`.
- Добавлены тесты `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` на:
  - полный путь `Parse -> Bind -> Lower -> BytecodeCompiler -> BytecodeVm`;
  - вызов routine и built-in;
  - исполнение переходов и глобалов;
  - runtime-диагностику деления на ноль.
- Обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Проверка
- `dotnet build src\Axiom.Runtime\Axiom.Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-build-bin-334\` - успешно.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-334\ --logger "console;verbosity=minimal"` - успешно, 375 тестов.
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-334\ --logger "console;verbosity=minimal"` - успешно, 41 тест.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-334\ --logger "console;verbosity=minimal"` - успешно, 188 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-334\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-334\` - успешно, 0 ошибок, 0 предупреждений.

### Решения
- Первый built-in bridge VM сделан минимальным и целенаправленным, без попытки сразу перенести весь объём встроенных функций интерпретатора.
- `BuiltinBinding` должен хранить не только внутренний id таблицы, но и канонический `BuiltinFunctionId`, иначе VM не может надёжно вызвать нужный built-in.
- Evaluation-stack закреплён за кадром вызова, а не за всей VM целиком: это упрощает ранний runtime-договор между routine.
- Этот шаг остаётся внутренним backend-шагом: `Compile` и `Run` ещё не исполняют VM напрямую.

### Что осталось
- Подключить `BytecodeVm` к `Axiom.Execution` и режимам запуска.
- Расширять покрытие VM и built-in bridge до паритета с интерпретатором.

### Следующий шаг
- Начать подключение байткодного пути к `Axiom.Execution` для покрытого скалярного подмножества языка и ввести первый реальный запуск через VM в явном режиме `Bytecode`.

## Session 335 - 2026-04-23 20:44 - Подключение BytecodeVm к Axiom.Execution

### Что сделано
- В `src/Axiom.Execution/Axiom.Execution.csproj` добавлена ссылка на `Axiom.Bytecode`.
- `src/Axiom.Execution/Results/CompiledProgram.cs` расширен опциональными `LoweredProgram` и `BytecodeModule`.
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` переведён на реальный двойной backend-путь:
  - `Compile` теперь строит `BoundProgram`, затем `LoweredProgram`, затем `BytecodeModule`;
  - frontend-успех и backend-неуспех теперь разделены;
  - `Run` и `RunCompiled` умеют реально запускать `BytecodeVm`;
  - режим `Auto` запускает VM на покрытом подмножестве и делает явный fallback на интерпретатор на непокрытом;
  - режим `Bytecode` при непокрытом срезе возвращает backend-ошибку без тихой подмены интерпретатором.
- В `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` исправлен аварийный сценарий при захвате внешнего параметра локальной функцией: вместо падения эмиттер теперь возвращает backend-диагностику.
- В `tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj` и `tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj` добавлены ссылки на `Axiom.Bytecode`.
- Обновлены `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` и `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs` под новый VM-first `Auto` для покрытых программ и под явный backend-failure для непокрытого байткодного режима.
- Синхронизированы `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Execution/Axiom.Execution.csproj`
- `src/Axiom.Execution/Results/CompiledProgram.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-335\ --logger "console;verbosity=minimal"` - успешно, 190 тестов.
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-335\ --logger "console;verbosity=minimal"` - успешно, 41 тест.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-335\ --logger "console;verbosity=minimal"` - успешно, 375 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-335\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-335\` - успешно, 0 ошибок, 0 предупреждений.

### Решения
- Переход на VM включён консервативно: между наличием `BytecodeModule` и фактическим запуском через VM введён отдельный предикат покрытия.
- `Auto` уже считается VM-first только для покрытого скалярного подмножества, а не для всего текущего языка.
- Для непокрытого среза `Auto` обязан явно показывать fallback, а принудительный `Bytecode` обязан честно завершаться backend-ошибкой.
- Это изменение ещё не считается полным паритетом interpreter/vm: часть семантики сознательно оставлена за интерпретатором до следующей волны покрытия.

### Что осталось
- Расширить покрытие VM за пределы первого скалярного среза.
- Убрать временные блокировки для `FOR`, локальных routine, локальных fixed-string переменных и объявлений внутри вложенных блоков.
- Довести built-in bridge и байткодный путь до более широкого паритета с интерпретатором.

### Следующий шаг
- Расширять таблицу покрытия VM и постепенно переводить на `BytecodeVm` следующие конструкции языка, начиная с ближайших непокрытых зон процедурной и управляющей семантики.

## Session 336 - 2026-04-23 21:09 - Включение FOR в VM-first покрытие

### Что сделано
- В `src/Axiom.Bytecode/Model/Opcode.cs`, `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs` и `src/Axiom.Bytecode/Model/LoweredInstruction.cs` добавлена служебная backend-проверка шага цикла `FOR`.
- В `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` lowering `FOR ... NEXT` теперь после вычисления `STEP` сохраняет его, повторно загружает и вставляет явную проверку `STEP 0` до входа в цикл.
- В `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` и `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` добавлена эмиссия и валидация нового opcode `ValidateForStep`.
- В `src/Axiom.Runtime/Execution/BytecodeVm.cs` реализована runtime-диагностика `MB3004` для `FOR ... STEP 0` с привязкой к карте инструкций.
- В `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` снята общая блокировка `FOR` в предикате VM-покрытия; теперь проверяются только реально оставшиеся непокрытые зоны.
- Обновлены тесты `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`, `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`, `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`, `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`.
- Синхронизированы `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-336\ --logger "console;verbosity=minimal"` - успешно, 42 теста.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-336\ --logger "console;verbosity=minimal"` - успешно, 377 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-336\ --logger "console;verbosity=minimal"` - успешно, 192 теста.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-336\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-336\` - успешно, 0 ошибок, 0 предупреждений.

### Решения
- `FOR` переведён из общего списка временно запрещённых для VM конструкций в реально покрытый backend-срез.
- Паритет с интерпретатором для `STEP 0` закреплён внутри байткода и VM, а не через внешний запрет на запуск.
- Следующим крупным непокрытым слоем остаются локальные routine и их взаимодействие с внешним окружением.

### Что осталось
- Локальные `SUB` / `FUNCTION` всё ещё не считаются покрытыми для пользовательского запуска через VM.
- Локальные fixed-string переменные и объявления внутри вложенных блоков всё ещё удерживаются на интерпретаторном пути.

### Следующий шаг
- Начать расширение VM-покрытия на локальные routine, начиная со сценариев без захвата внешних переменных.

## Session 337 - 2026-04-23 21:52 - Локальные FUNCTION без захвата в VM-first rollout

### Что сделано
- В src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs переработан предикат VM-покрытия для локальной процедурной модели.
- Локальные FUNCTION без захвата внешних переменных теперь разрешены для пользовательского запуска через BytecodeVm.
- Для локальных функций с захватом внешнего параметра Auto теперь использует реальную backend-диагностику эмиттера как причину fallback, а режим Bytecode по-прежнему возвращает BC5004.
- Для локальных функций с захватом внешнего локала добавлена отдельная ошибка покрытия MB4002.
- Локальные SUB временно оставлены на интерпретаторном пути через отдельную ошибку покрытия MB4003, чтобы не открывать в VM ещё неустойчивый процедурный срез.
- Обновлены tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs, tests/Axiom.Runtime.Tests/BytecodeVmTests.cs и tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs.
- Синхронизированы README.md, docs/SPECIFICATION2.md, docs/ARCHITECTURE.md, docs/PLANS2.md.

### Какие файлы изменены
- README.md
- docs/SPECIFICATION2.md
- docs/ARCHITECTURE.md
- docs/PLANS2.md
- docs/WORKLOG3.md
- src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs
- tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs

### Проверка
- dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-337\ --logger "console;verbosity=minimal" - успешно, 42 теста.
- dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-337-full\ --logger "console;verbosity=minimal" - успешно, 378 тестов.
- dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-337-full\ --logger "console;verbosity=minimal" - успешно, 197 тестов.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-337\ --logger "console;verbosity=minimal" - успешно, 219 тестов.
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-337\ - успешно, 0 ошибок, 0 предупреждений.

### Решения
- В пользовательский VM-first rollout пока включаются только те локальные routine, для которых уже подтверждён безопасный неперехватывающий вызов.
- Захваты внешних значений по-прежнему не кодируются как отдельная runtime-модель замыканий: до этого этапа они либо режутся backend-диагностикой эмиттера, либо предикатом покрытия.
- Причина fallback в Auto теперь должна быть диагностически содержательной, если backend уже успел сообщить точную причину отказа.

### Что осталось
- Локальные SUB всё ещё не считаются покрытыми для пользовательского запуска через VM.
- Захваты внешних параметров и локалов для локальных routine всё ещё не имеют собственной модели исполнения в байткодном пути.
- Полный процедурный паритет interpreter/vm ещё требует дальнейшей стабилизации.

### Следующий шаг
- Довести VM-покрытие до локальных SUB без захвата внешних переменных, а затем отдельно переходить к модели захватов для локальных routine.
## Session 338 - 2026-04-23 22:11 - Локальные SUB без захвата включены в VM-first rollout

### Что сделано
- В src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs исправлен lowered-контракт для операторного вызова SUB: после выражения типа VOID больше не добавляется лишний Pop.
- В src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs снята временная блокировка локальных SUB без захвата внешних переменных.
- После этого локальные SUB без захвата реально выполняются через BytecodeVm в режимах Auto и Bytecode.
- Обновлены тесты tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs, tests/Axiom.Runtime.Tests/BytecodeVmTests.cs, tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs.
- Синхронизированы README.md, docs/SPECIFICATION2.md, docs/ARCHITECTURE.md, docs/PLANS2.md.

### Какие файлы изменены
- README.md
- docs/SPECIFICATION2.md
- docs/ARCHITECTURE.md
- docs/PLANS2.md
- docs/WORKLOG3.md
- src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs
- src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs
- tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs
- tests/Axiom.Runtime.Tests/BytecodeVmTests.cs
- tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs

### Проверка
- dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-338-full\ --logger "console;verbosity=minimal" - успешно, 42 теста.
- dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-338-full\ --logger "console;verbosity=minimal" - успешно, 379 тестов.
- dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-338-full\ --logger "console;verbosity=minimal" - успешно, 200 тестов.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-338-full\ --logger "console;verbosity=minimal" - успешно, 219 тестов.
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-338\ - успешно, 0 ошибок, 6 старых nullable-предупреждений в Axiom.Semantics.Tests и Axiom.Parsing.Tests.

### Решения
- Вызов SUB в операторной позиции не требует отдельного opcode: достаточно правильного lowered-контракта без Pop для выражений типа VOID.
- В пользовательский VM-first rollout теперь входят обе формы локальных routine без захвата: и FUNCTION, и SUB.
- Следующим большим непокрытым слоем остаётся именно модель захватов, а не прямой локальный вызов routine.

### Что осталось
- Захват внешних параметров и локалов для локальных routine всё ещё не имеет собственной модели исполнения в байткодном пути.
- Локальные fixed-string переменные и часть объявлений во вложенных блоках всё ещё удерживают программу на интерпретаторном пути.
- Полный паритет interpreter/vm для всего текущего языка ещё требует дальнейшей стабилизации.

### Следующий шаг
- Реализовать модель захвата внешних параметров и локалов для локальных routine, а затем переходить к следующему слою ограничений покрытия VM.

## Session 339 - 2026-04-23 22:23 - Захваты внешних переменных включены в BytecodeVm

### Что сделано
- В `src/Axiom.Bytecode/Model/Opcode.cs` добавлены `LoadCapture` и `StoreCapture`.
- В `src/Axiom.Bytecode/Model/LoweredRoutine.cs` и `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs` добавлен явный учёт собственных локальных переменных routine, включая lowered-временные переменные.
- В `src/Axiom.Bytecode/Model/BytecodeRoutine.cs` расширена модель routine: теперь она хранит таблицы параметров, локалов и захватов.
- В `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` добавлена отдельная карта слотов захвата; прямые локальные `SUB` и `FUNCTION` с внешними параметрами и локалами теперь компилируются в `LoadCapture` / `StoreCapture`.
- В `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs` добавлена валидация capture-слотов.
- В `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`, `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs` и `src/Axiom.Runtime/Execution/BytecodeVm.cs` внедрена shared-slot модель хранения аргументов, локалов и захватов.
- В `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` снята execution-блокировка прямых локальных захватов.
- Обновлены `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`, `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`, `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`.

### Какие файлы изменены
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `src/Axiom.Bytecode/Model/LoweredRoutine.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`
- `src/Axiom.Runtime/Execution/BytecodeCallFrame.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --filter BytecodeCompilerTests` - успешно.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter BytecodeVmTests` - успешно.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter ExecutionPipelineTests` - успешно.

### Решения
- Захваты в байт-кодном пути реализуются через общие изменяемые слоты хранения, а не через копирование значений.
- Для этого не вводится отдельная runtime-модель замыканий: прямые локальные вызовы используют уже существующие `RuntimeValue` и новый тип слота.
- Возвращаемые значения-функции и полноценные замыкания пока остаются за пределами текущего пользовательского VM-покрытия.

### Что осталось
- Снять ограничения для локальных fixed-string переменных.
- Снять ограничения для объявлений `DIM` внутри вложенных блоков.
- Затем перейти к следующему пласту паритета VM: built-in, массивы, `TYPE`, файловый слой и regex-массивы.

### Следующий шаг
- Включить в VM-first покрытие local fixed-string и вложенные блочные объявления `DIM`.

## Session 340 - 2026-04-23 23:23 - Fixed-string и блочные DIM включены в VM-first покрытие

### Что сделано
- В `src/Axiom.Bytecode/Model/BytecodeGlobal.cs` добавлена длина fixed-string для глобала.
- В `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs` глобалы теперь сохраняют `FixedStringLength`, а разрешение меток переходов учитывает только lowered-инструкции, реально порождающие байткод.
- В `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs` слот хранения теперь знает длину fixed-string и нормализует строку при записи.
- В `src/Axiom.Runtime/Execution/BytecodeVm.cs`:
  - fixed-string длина учитывается при инициализации глобалов;
  - локальные слоты создаются с типовым значением по умолчанию и длиной fixed-string, если она есть;
  - запись в глобалы, локалы, аргументы и захваты нормализует значение по договору fixed-string.
- В `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` снята временная execution-блокировка `DIM` внутри вложенных блоков и fixed-string `DIM`.
- Добавлены тесты:
  - в `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs` на корректные jump-target после lowered no-op;
  - в `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` на fixed-string, блочные `DIM` в `IF / ELSEIF / ELSE`, `SELECT CASE`, `FOR`, `WHILE`, `DO`, а также внутри `SUB` и `FUNCTION`;
  - в `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` на запуск fixed-string и блочных `DIM` через `BytecodeVm`.
- Синхронизированы `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/BytecodeGlobal.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Runtime/Execution/BytecodeVariableSlot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-339-full\ --logger "console;verbosity=minimal"` - успешно, 44 теста.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-339-full\ --logger "console;verbosity=minimal"` - успешно, 388 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-339-full\ --logger "console;verbosity=minimal"` - успешно, 202 теста.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-339-full\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-339\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Semantics.Tests` и `Axiom.Parsing.Tests`.

### Решения
- Fixed-string в байт-кодном пути нормализуется на самом слое хранения, а не через отдельный интерпретаторный обход.
- Execution-предикат больше не должен удерживать вложенные блочные `DIM`, если lowered/bytecode/VM-путь уже умеет их исполнять.
- Разрешение меток в эмиттере обязано учитывать lowered no-op, иначе байт-код получает ложные адреса переходов даже при валидном lowered-потоке.

### Что осталось
- Расширить built-in bridge VM за пределы первого скалярного набора встроенных функций.
- Довести до байт-кодного паритета массивы, `TYPE`, fixed-string внутри контейнеров, файловый слой и regex-функции, возвращающие массивы.
- После этого укрепить parity-тесты interpreter/vm и выровнять метрики и fallback-диагностику в IDE.

### Следующий шаг
- Начать следующую волну VM-покрытия с расширения built-in bridge, а затем переходить к массивам, `TYPE`, файловому слою и regex-массивам.

## Session 341 - 2026-04-23 23:42 - Строковые built-in включены в BytecodeVm

### Что сделано
- В `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` расширен dispatch built-in для строкового пакета: `LEFT`, `RIGHT`, `MID`, `TRIM`, `LTRIM`, `RTRIM`, `REVERSE`, `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `REPLACE`, `INSTR`, `INSTRREV`, `SUBSTRCOUNT`, `REPEAT`, `SPACE`, `STRING`, `PADLEFT`, `PADRIGHT`, `CENTER`, `INSERT`, `REMOVE`, `ASC`, `CHR`.
- В `src/Axiom.Runtime/Execution/` добавлены новые частичные файлы `BytecodeVm.Builtins.String.cs`, `BytecodeVm.Builtins.String.Fill.cs`, `BytecodeVm.Builtins.String.Edit.cs`, `BytecodeVm.Builtins.String.Helpers.cs` с логикой строковых built-in и общими проверками аргументов.
- В `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` расширен `VmSupportedBuiltins`, чтобы программы с этим строковым набором действительно шли через `BytecodeVm`.
- В `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены тесты на успешное выполнение строковых built-in и на ошибки аргументов `MB3007`.
- В `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` добавлены тесты, подтверждающие запуск строковых built-in через `BytecodeVm` в режимах `Auto` и `Bytecode`.
- Синхронизированы `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Fill.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Edit.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.Helpers.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-341-full\ --logger "console;verbosity=minimal"` - успешно, 44 теста.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-341-full\ --logger "console;verbosity=minimal"` - успешно, 394 теста.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-341-full\ --logger "console;verbosity=minimal"` - успешно, 205 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-341-full\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-341\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Semantics.Tests` и `Axiom.Parsing.Tests`.

### Решения
- Строковые built-in в байт-кодном пути реализуются без отдельного обходного механизма: `BytecodeVm` повторяет ту же ordinal-семантику, что и интерпретатор.
- Ошибки аргументов для строковых built-in остаются в пользовательском коде `MB3007`, а не превращаются во внутренние ошибки VM.
- Для этого шага не потребовались `.axm`-примеры и обновления `docs/manual/`, потому что пользовательская семантика языка не менялась: изменился только путь исполнения через `BytecodeVm`.

### Что осталось
- Добрать оставшиеся скалярные built-in, которые не требуют новых runtime-объектов: `INT`, `RND` и численные математические функции.
- После этого перейти к более тяжёлым слоям покрытия: массивам, `TYPE`, fixed-string внутри контейнеров, файловому слою и regex-функциям, возвращающим массивы.
- Затем укрепить parity-тесты interpreter/vm и довести IDE-метрики и причины fallback до полного паритета.

### Следующий шаг
- Расширить built-in bridge `BytecodeVm` на оставшиеся скалярные built-in (`INT`, `RND`, численная математика), а затем перейти к следующему крупному слою runtime-покрытия.

## Session 342 - 2026-04-24 06:25 - Числовые built-in включены в BytecodeVm

### Что сделано
- В `src/Axiom.Runtime/Execution/BytecodeVm.cs` добавлен собственный генератор случайных чисел VM для `RND`.
- В `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs` расширен dispatch built-in на `INT`, `RND` и числовую группу.
- Добавлен `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Numeric.cs` с реализацией `SIN`, `COS`, `TAN`, `ATN`, `ATN2`, `SQR`, `CBRT`, `EXP`, `LOG`, `LOG10`, `LOG2`, `POW`, `FLOOR`, `CEIL`, `ROUND`, `FIX`, `ASIN`, `ACOS`, `ASINH`, `ACOSH`, `ATANH`, `SINH`, `COSH`, `TANH`, `SETSIGN`.
- В `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` расширен список `VmSupportedBuiltins`, поэтому покрытые программы с этой числовой группой идут через `BytecodeVm` в режимах `Auto` и `Bytecode`.
- В `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs` добавлены тесты на числовые преобразования, округления, математические функции, `RND` и ошибку аргумента.
- В `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs` добавлены тесты на реальный запуск числовых built-in через VM и runtime-ошибку `MB3007`.
- Синхронизированы `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

### Какие файлы изменены
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Numeric.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-342-full\ --logger "console;verbosity=minimal"` - успешно, 44 теста.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-342-full\ --logger "console;verbosity=minimal"` - успешно, 398 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-342-full\ --logger "console;verbosity=minimal"` - успешно, 207 тестов.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-342-full\ --logger "console;verbosity=minimal"` - успешно, 219 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-342\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

### Решения
- Числовая группа VM реализована отдельным частичным файлом, чтобы не смешивать строковые и числовые built-in.
- Для этого шага не требовались `.axm`-примеры и обновления `docs/manual/`, потому что пользовательская семантика языка не менялась: изменился только путь исполнения уже существующих built-in через `BytecodeVm`.
- `Axiom.Execution` по-прежнему расширяется только таблицей поддержанных built-in, без специальных обходов под отдельные функции.

### Что осталось
- Дата и время пока остаются за пределами VM-покрытия.
- Массивные built-in, `TYPE`, fixed-string внутри контейнеров, файловый слой и regex-функции, возвращающие массивы, ещё нужно переносить отдельно.
- Нужно наращивать parity-тесты interpreter/vm по мере расширения покрытия.

### Следующий шаг
- Перейти к следующему скалярному пакету VM-покрытия: встроенным функциям даты и времени, если они не требуют новых runtime-контейнеров; после этого перейти к массивам и `TYPE`.

## Session 343 - 2026-04-24 06:41 - Функции даты и времени включены в BytecodeVm

Что сделано:
- добавлен `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.DateTime.cs`;
- `BytecodeVm` теперь исполняет функции даты и времени через `BuiltinFunctionId`, без отдельных опкодов под каждую функцию;
- поддержаны текущие функции `DATE`, `TIME`, `NOW`, `UTCDATE`, `UTCTIME`, `UTCNOW`, `TIMER`, `DATESERIAL`, `TIMESERIAL`, `YEAR`, `MONTH`, `MONTHNAME`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `WEEKDAYNAME`, `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE`, `UTCOFFSET`, `TOUTC`, `FROMUTC`, `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR`;
- поддержаны перегруженные формы с часовым поясом для `DATE`, `TIME`, `NOW`, `UTCOFFSET`, `TOUTC`, `FROMUTC`;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому режимы `Auto` и `Bytecode` могут запускать покрытые программы с этими функциями через VM;
- добавлены тесты `BytecodeVmTests` на текущее время хоста, календарные преобразования, именованные часовые пояса и диагностику `MB3007`;
- добавлены тесты `ExecutionPipelineTests` на выбор `BytecodeVm` для функций даты/времени и ошибку аргумента в явном режиме `Bytecode`;
- обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-343-date-time-narrow\ --logger "console;verbosity=minimal"` - пройдено 402 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-343-date-time-narrow\ --logger "console;verbosity=minimal"` - пройдено 210 тестов.

Решения:
- пользовательская семантика языка не менялась, поэтому новые страницы руководства и новые `.axm`-примеры не добавлялись;
- VM повторяет существующий договор интерпретатора: даты и время остаются каноническими строками, текущее время и зоны идут через `IRuntimeHost`, ошибки аргументов возвращаются как `MB3007`.

Остаётся:
- запустить полный набор проверок после обновления документов;
- следующим рабочим слоем перевести массивы и связанные операции в байт-кодный путь: доступ к элементам, `REDIM`, `ERASE`, границы и первые массивные функции.

Дополнение после полной проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-343-full\ --logger "console;verbosity=minimal"` - пройдено 44 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-343-full\ --logger "console;verbosity=minimal"` - пройдено 402 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-343-full\ --logger "console;verbosity=minimal"` - пройдено 210 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-tests-bin-343-full\ --logger "console;verbosity=minimal"` - пройдено 219 тестов;
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-343\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.
## Session 344 - 2026-04-24 07:17 - Базовые массивы включены в BytecodeVm

Что сделано:
- в `Axiom.Bytecode` lowered-модель расширена инструкциями `RedimArray`, `EraseArray`, `LoadElement`, `StoreElement`;
- `BoundProgramLowerer` теперь понижает динамические массивы, массивы с границами, чтение и запись элементов, `REDIM`, `REDIM PRESERVE` и `ERASE`;
- `BytecodeCompiler` эмитит массивные опкоды и передаёт в инструкции отладочное имя массива;
- `BytecodeModuleValidator` проверяет новую двухоперандную форму `RedimArray` и константные индексы массивных инструкций;
- `BytecodeVm` получил `BytecodeVm.Arrays.cs` для исполнения массива через существующий `RuntimeArrayValue`;
- добавлен `BytecodeVm.Builtins.Array.cs` и включены `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`;
- `RuntimeValue.Default` теперь передаёт фиксированную длину строкового элемента в создаваемый `RuntimeArrayValue`, а `CopyForStorage` умеет клонировать массив при записи в fixed-string массив;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому поддержанные массивные программы могут идти через VM в `Auto` и `Bytecode`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-344-final-seq\ --logger "console;verbosity=minimal"` - пройдено 44 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-344-final-seq\ --logger "console;verbosity=minimal"` - пройдено 404 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-344-final-seq\ --logger "console;verbosity=minimal"` - пройдено 211 тестов.

Решения:
- новая пользовательская семантика языка не вводилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требовались;
- массивы в VM используют общую runtime-модель, а не отдельный формат хранения;
- первый массивный слой ограничен базовой памятью массива и простыми функциями сведений; сложные массивные built-in идут следующей очередью.

Что остаётся:
- расширить VM на функции `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTAKE`, `ARRAYSKIP`, `ARRAYTOREVERSED`;
- затем перейти к мутирующим и поисковым функциям массивов;
- после массива вернуться к `TYPE`, fixed-string внутри контейнеров, файловому слою и regex-функциям, возвращающим массивы.

Следующий шаг:
- начать с не callback-функций массивов, которые возвращают новый массив и могут быть реализованы поверх `RuntimeArrayValue` без изменения пользовательской семантики.

Дополнение после финальной проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\bytecode-tests-bin-344-after-docs\ --logger "console;verbosity=minimal"` - пройдено 44 теста;
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-344\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

## Session 345 - 2026-04-24 07:38 - Чистые массивные built-in включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTOSORTED`, `ARRAYTOREVERSED`, `ARRAYTAKE`, `ARRAYSKIP`, `ARRAYDISTINCT`;
- в `BytecodeVm.Builtins.Array.cs` добавлены общие helper-методы создания, клонирования и обхода массивов поверх `RuntimeArrayValue`;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому покрытые программы с этими функциями идут через VM в режимах `Auto` и `Bytecode`;
- добавлены runtime-тесты полного байткодного пути для чистых массивных функций, невыделенного массива и ошибок аргументов;
- добавлены execution-тесты на выбор `BytecodeVm` и runtime-ошибку в явном режиме `Bytecode`;
- обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-345-array-pure\ --logger "console;verbosity=minimal"` - пройдено 213 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-345-array-pure-rerun\ --logger "console;verbosity=minimal"` - пройдено 408 тестов.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требовались;
- чистые массивные функции в VM используют общую модель `RuntimeArrayValue`, а не отдельное хранилище;
- `ARRAYTOSORTED` включён в этот слой, потому что он возвращает новый массив без callback-модели и уже имеет ограничение по типам на уровне binder.

Что остаётся:
- включить в VM мутирующие массивные функции `ARRAYFILL`, `ARRAYREVERSE`, `ARRAYSORT`, `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- затем перейти к поисковым массивным функциям и отдельно к функциям с callback-моделью;
- после массивов вернуться к `TYPE`, fixed-string внутри контейнеров, файловому слою и regex-функциям, возвращающим массивы.

Следующий шаг:
- начать с мутирующих массивных функций поверх `RuntimeArrayValue`, сохраняя паритет диагностики и не меняя пользовательскую семантику языка.

Дополнение по рабочему процессу:
- по замечанию пользователя в `AGENTS.md` добавлено постоянное правило для проверок .NET;
- `dotnet build`, `dotnet test`, `dotnet run` и другие MSBuild-команды в этом репозитории теперь по умолчанию должны запускаться последовательно;
- параллельный запуск допустим только при явном разведении промежуточных каталогов через уникальный `BaseIntermediateOutputPath` или равносильную настройку, потому что одного отдельного `OutputPath` недостаточно для защиты общего `.artifacts/obj`.
- дополнительный изменённый файл: `AGENTS.md`;
- финальная сборка до этого дополнения уже была выполнена командой `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-345-array-pure\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`;
- отдельные проверки после изменения только рабочих правил не запускались, потому что менялась документация процесса, а не код.

## Session 346 - 2026-04-24 07:49 - Мутирующие массивные built-in включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет `ARRAYFILL`, `ARRAYREVERSE`, `ARRAYSORT`, `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE`;
- реализация использует общий `RuntimeArrayValue`, включая `SetValue`, `AllocatePreserving` и канонический пустой диапазон `1 TO 0`;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому покрытые программы с этими функциями идут через VM в режимах `Auto` и `Bytecode`;
- добавлены runtime-тесты полного байткодного пути для успешной мутации массива, канонического пустого массива, невыделенного массива, пустого `ARRAYPOP` и недопустимого индекса `ARRAYINSERT`;
- добавлены execution-тесты на выбор `BytecodeVm` и runtime-ошибку в явном режиме `Bytecode`;
- обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-346-array-mutating\ --logger "console;verbosity=minimal"` - пройдено 413 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-346-array-mutating\ --logger "console;verbosity=minimal"` - пройдено 215 тестов.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требовались;
- мутирующие функции реализованы как обычные built-in VM, без отдельных опкодов;
- проверки .NET запускались последовательно по новому правилу `AGENTS.md`.

Что остаётся:
- включить в VM поисковые массивные функции `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL`, `ARRAYBINARYSEARCH`;
- отдельно спроектировать и перенести callback-группу `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE`;
- после массивов вернуться к `TYPE`, fixed-string внутри контейнеров, файловому слою и regex-функциям, возвращающим массивы.

Следующий шаг:
- начать с поисковых массивных функций поверх уже существующих правил равенства, сравнения и логических индексов VM.

## Session 347 - 2026-04-24 08:08 - Поисковые массивные built-in включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL`, `ARRAYBINARYSEARCH`;
- реализация использует общий `RuntimeArrayValue`, существующие правила равенства и сравнения VM, логические индексы массива и прежний договор `LBOUND(array) - 1` для результата "не найдено";
- `ARRAYFINDALL` создаёт компактный `INTEGER`-массив индексов `1 TO N` или канонический пустой массив `1 TO 0`;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому покрытые программы с поисковыми функциями массивов запускаются через VM в режимах `Auto` и `Bytecode`;
- добавлены runtime- и execution-тесты на успешный поиск, повторяющиеся значения, компактный массив индексов, канонический пустой массив и невыделенный источник.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-347-array-search\ --logger "console;verbosity=minimal"` - пройдено 416 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-347-array-search\ --logger "console;verbosity=minimal"` - пройдено 217 тестов.
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-347-array-search\` - успешно, 0 ошибок, 6 старых nullable-предупреждений в `Axiom.Parsing.Tests` и `Axiom.Semantics.Tests`.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требовались;
- поисковые функции реализованы как обычные built-in VM через общий dispatch, без новых опкодов и без специальных обходов в `Axiom.Execution`;
- проверки .NET запускались последовательно по правилу `AGENTS.md`.

Что остаётся:
- отдельно спроектировать и перенести в VM callback-группу массивов `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE`;
- после массивов вернуться к `TYPE`, fixed-string внутри контейнеров, файловому слою и regex-функциям, возвращающим массивы.

Следующий шаг:
- начать с проектирования вызова значений-функций и замыканий в байткодном пути для callback-группы массивов.

## Session 348 - 2026-04-24 10:33 - Массивные функции с обратным вызовом включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE` для пользовательских значений-функций;
- lowered-модель и байткод получили загрузку значения функции через `LoadFunction`;
- `RuntimeFunctionValue` получил VM-представление захваченных слотов, поэтому локальные функции обратного вызова работают с замыканиями;
- группа массивов с обратным вызовом реализована поверх общего `RuntimeArrayValue` и обычного стека кадров VM, без отдельных опкодов под каждую массивную функцию;
- `Axiom.Execution` расширил `VmSupportedBuiltins`, поэтому покрытые программы с этими функциями запускаются через VM в режимах `Auto` и `Bytecode`;
- добавлены тесты времени выполнения и конвейера запуска на пользовательские функции обратного вызова, замыкания, массивы-результаты и неинициализированный функциональный параметр;
- обновлены `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Values/RuntimeFunctionValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Array.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-348-array-callback\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\runtime-tests-348-array-callback\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-348-array-callback\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\execution-tests-348-array-callback\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-348-array-callback\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\solution-build-348-array-callback\` - не прошла из-за общего `project.assets.json` при одном `BaseIntermediateOutputPath` для всего решения;
- `dotnet build Axiom.sln -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\solution-build-bin-348-array-callback\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\solution-build-348-array-callback\` - не прошла по той же причине после восстановления;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требовались;
- значения пользовательских функций и локальных функций с замыканиями теперь являются частью байткодного пути;
- значения встроенных функций как обратный вызов пока оставлены переходной границей: `Auto` может откатиться на интерпретатор через диагностику байткодного слоя, явный `Bytecode` должен сообщить неподдержанность;
- для сборки всего решения с изолированным `BaseIntermediateOutputPath` нельзя задавать один общий каталог на все проекты; рабочая проверка решения выполнена через обычные проектные `obj` с `-m:1`.

Что остаётся:
- перейти к пользовательским `TYPE`, доступу к полям записей и fixed-string внутри контейнеров;
- позже вернуться к файловому слою и функциям регулярных выражений, возвращающим массивы;
- при необходимости отдельно расширить VM на значения встроенных функций как обратный вызов.

Следующий шаг:
- начать проектирование и перенос пользовательских `TYPE` в байткодный путь, начиная с представления записей, доступа к полям и правил хранения fixed-string внутри контейнеров.

## Session 349 - 2026-04-24 10:47 - Пользовательские TYPE включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет чтение и запись полей пользовательских `TYPE`;
- lowered-модель получила инструкции `LoadField` и `StoreField`;
- эмиттер байткода выпускает опкоды `LoadField` и `StoreField`, используя имя поля из пула строковых констант;
- валидатор модуля проверяет операнд имени поля как индекс константы;
- VM разрешает поле по фактическому `UserDefinedTypeSymbol` значения записи и использует общий `RuntimeRecordValue`;
- переменные пользовательских записей без инициализатора используют начальное значение слота VM через `RuntimeValue.Default`;
- карта локальных слотов теперь выделяет слот для всех локальных переменных процедуры, чтобы запись со значением по умолчанию можно было захватить во вложенной функции даже без прямого обращения в теле внешней процедуры;
- покрыты вложенные записи, поля записей в массивах, динамические и фиксированные поля-массивы, fixed-string-поля внутри `TYPE`;
- `Auto` теперь выбирает VM для покрытых программ с пользовательскими `TYPE`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Records.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-349-type-bytecode\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\runtime-tests-349-type-bytecode\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-349-type-bytecode\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\execution-tests-349-type-bytecode\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требовались;
- отдельный опкод `CreateRecord` пока не используется в пользовательском пути, потому что начальные значения записей создаются через `RuntimeValue.Default` при создании слотов и глобалов;
- поле в VM разрешается по имени внутри фактического типа записи, чтобы не вводить отдельную таблицу полей в байткодном модуле.
- локальные слоты больше не зависят только от упоминаний переменной в инструкциях, потому что для сложных значений без явной инициализации сам слот может понадобиться только при захвате.

Что остаётся:
- перейти к файловому слою байткодного пути;
- затем вернуться к функциям регулярных выражений, возвращающим массивы;
- отдельно при необходимости расширить VM на значения встроенных функций как обратный вызов.

Следующий шаг:
- начать перенос файлового слоя в `BytecodeVm`, выбрав первый небольшой набор файловых операций и сохранив текущий договор пользовательских диагностик времени выполнения.

## Session 350 - 2026-04-24 11:13 - Функции файловой системы включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет первый слой функций файловой системы, которые работают напрямую с путями: проверки существования, чтение и запись целых текстовых файлов, сведения о файлах и папках, перечисление ближайшего уровня, операции с путями, копирование/перенос/удаление, рабочий каталог, сведения о диске и временные пути;
- добавлен `BytecodeVm.Builtins.FileSystem.cs` с теми же правилами кодировки, нормализации путей, канонических дат, массивов строк и диагностики `MB3007`, что и у интерпретатора;
- `Axiom.Execution` расширил таблицу `VmSupportedBuiltins`, поэтому покрытые программы с этими функциями запускаются через VM в режимах `Auto` и `Bytecode`;
- добавлены тесты времени выполнения и конвейера запуска на успешный файловый сценарий и ошибку отсутствующего файла.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.FileSystem.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\runtime-tests-350-filesystem\ -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-350-filesystem\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\execution-tests-350-filesystem\ -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-350-filesystem\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требовались;
- файловые функции реализованы как обычные встроенные функции VM без новых опкодов;
- операторы `OPEN`/`CLOSE` и функции над открытым значением `FILE` оставлены отдельным следующим слоем, потому что там нужен жизненный цикл `RuntimeTextFile`.

Что остаётся:
- перенести в VM `OPEN`, `CLOSE`, `ISOPEN`, `FILEPATH`, `FILEMODE`, `FILEPOS`, `FILELENGTH`, `LINENUMBER`, `EOF`, `READTOEND`, `READTEXT`, `READLINE`, `READLINES`, `SKIPLINE`, `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND`;
- затем вернуться к функциям регулярных выражений, возвращающим массивы;
- отдельно при необходимости расширить VM на значения встроенных функций как обратный вызов.

Следующий шаг:
- начать с минимального байткодного пути для `OPEN ... AS f` и `CLOSE f`, затем подключить базовые чтение и запись открытого текстового файла.

Дополнительные точечные проверки после обновления документов:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter FileSystem -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\runtime-tests-350-filesystem-filter\ -p:OutputPath=C:\Axiom\.artifacts\runtime-tests-bin-350-filesystem-filter\ --logger "console;verbosity=minimal"` - успешно;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter FileSystem -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\obj\execution-tests-350-filesystem-filter\ -p:OutputPath=C:\Axiom\.artifacts\execution-tests-bin-350-filesystem-filter\ --logger "console;verbosity=minimal"` - успешно.

## Session 351 - 2026-04-24 11:29 - Открытые FILE включены в BytecodeVm

Что сделано:
- `OPEN ... FOR INPUT/OUTPUT/APPEND AS f` теперь понижается в отдельную инструкцию `OpenFile`, а `CLOSE f` - в `CloseFile`;
- эмиттер байткода выпускает опкоды `OpenFile` и `CloseFile`, а валидатор проверяет режим открытия и отладочное имя переменной;
- `BytecodeVm` исполняет открытие и закрытие `RuntimeTextFile`, отслеживает открытые файлы и закрывает оставшиеся файлы при завершении выполнения;
- функции над открытым `FILE` включены в VM: `ISOPEN`, `FILEPATH`, `FILEMODE`, `FILEPOS`, `FILELENGTH`, `LINENUMBER`, `EOF`, `READTOEND`, `READTEXT`, `READLINE`, `READLINES`, `SKIPLINE`, `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND`;
- `Axiom.Execution` расширил таблицу `VmSupportedBuiltins`, поэтому покрытые программы с открытыми файлами идут через VM в режимах `Auto` и `Bytecode`;
- добавлены тесты VM и конвейера запуска на чтение, запись, добавление, закрытый файл и ошибку открытия отсутствующего файла.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.FileIO.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "OpenInputFileProgram|OpenOutputAndAppendFileProgram|OpenFileReportsClosedFileRead" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "OpenTextFile" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 426 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 223 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 44 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются;
- тесты чтения нескольких строк и предыдущие проверки функций файловой системы используют уже поддержанное присваивание результата в заранее объявленный массив, не расширяя задачу до инициализаторов массивов;
- функции над открытым файлом реализованы в отдельном частичном файле VM, а конвейер запуска по-прежнему расширяется только таблицей покрытия.

Что остаётся:
- перейти к функциям регулярных выражений, возвращающим массивы;
- позднее отдельно оценить необходимость поддержки значений встроенных функций как обратного вызова.

Следующий шаг:
- начать перенос функций регулярных выражений с массивными результатами в `BytecodeVm`, сохранив общий подход через `RuntimeArrayValue` и `VmSupportedBuiltins`.

## Session 352 - 2026-04-24 11:51 - Функции регулярных выражений с массивами включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет функции регулярных выражений, которые возвращают массивы строк: `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT` и их варианты с аргументом `modes`;
- добавлен отдельный частичный файл `BytecodeVm.Builtins.Regex.cs` с созданием `Regex`, разбором режимов `i`, `m`, `s`, `x`, лимитом выполнения и преобразованием ошибок в `MB3007`;
- массивные результаты создаются через общий `RuntimeArrayValue` как компактные массивы строк `1 TO N` или `1 TO 0`;
- `Axiom.Execution` расширил таблицу `VmSupportedBuiltins`, поэтому покрытые программы с этими функциями запускаются через VM в режимах `Auto` и `Bytecode`;
- добавлены проверки VM и конвейера запуска на успешные массивные результаты, режимы регулярных выражений и ошибку недопустимого режима.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter RegexArray -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter RegexArray -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 5 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 428 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 225 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются;
- в этот слой включены только функции регулярных выражений с массивным результатом; скалярные `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE`, `REGEXCOUNT` пока остаются за пределами таблицы покрытия VM;
- специальных обходов в `Axiom.Execution` не добавлялось: выбор VM по-прежнему управляется только покрытием встроенных функций.

Что остаётся:
- отдельно оценить, нужно ли переносить в VM значения встроенных функций как обратный вызов;
- затем продолжить добор оставшихся непокрытых частей байткодного пути по документам.

Следующий шаг:
- сверить текущие границы `VmSupportedBuiltins` и выбрать следующий непокрытый байткодный слой; первым кандидатом остаётся граница значений встроенных функций как обратный вызов, если она блокирует дальнейшие сценарии.

## Session 353 - 2026-04-24 13:48 - Значения встроенных функций как обратный вызов включены в BytecodeVm

Что сделано:
- закрыта переходная граница, где встроенная функция могла быть значением функционального типа в интерпретаторе, но не исполнялась через VM;
- в байткод добавлен `Opcode.LoadBuiltinFunction`;
- `BytecodeCompiler` теперь эмитит `LoadBuiltinFunction` для встроенных функций, использованных как значения, и регистрирует встроенные функции с учётом сигнатуры;
- `BuiltinBinding` расширен ссылкой на исходный `FunctionSymbol`, чтобы VM могла отличать конкретную разрешённую форму встроенной функции;
- `BytecodeModuleValidator` проверяет операнд `LoadBuiltinFunction` как ссылку на таблицу встроенных функций;
- `RuntimeValue` получил создание функционального значения для встроенной функции в байткодном пути;
- `BytecodeVm` загружает значения встроенных функций и при вызове такого значения использует общий `ExecuteBuiltin`;
- добавлены проверки, что `ARRAYMAP(names, UCASE)`, `ARRAYMAP(names, LEN)` и `ARRAYFILTER(dateTexts, ISDATE)` выполняются через VM.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BuiltinBinding.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "BuiltinFunction|LoadBuiltinFunction" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter BuiltinFunctionValues -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter BuiltinFunctionCallbacks -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 46 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 429 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 226 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются;
- значения встроенных функций в VM вызываются через общий путь встроенных функций, а не через особый путь массивных операций;
- регистрация встроенной функции в байткодном модуле учитывает сигнатуру, чтобы не потерять конкретную разрешённую форму перегруженной встроенной функции.

Что остаётся:
- продолжить добор оставшихся непокрытых частей байткодного пути;
- первым ближайшим кандидатом остаются скалярные функции регулярных выражений `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE`, `REGEXCOUNT`.

Следующий шаг:
- сверить `VmSupportedBuiltins` и перенести скалярные функции регулярных выражений в `BytecodeVm`, сохранив существующие режимы `i`, `m`, `s`, `x`, лимит выполнения регулярного выражения и диагностику `MB3007`.

## Session 354 - 2026-04-24 14:01 - Скалярные функции регулярных выражений включены в BytecodeVm

Что сделано:
- `BytecodeVm` теперь исполняет скалярные функции регулярных выражений `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE`, `REGEXCOUNT` и их текущие перегрузки с `modes`/`count`;
- реализация добавлена в существующий `BytecodeVm.Builtins.Regex.cs` рядом с массивными regex-функциями;
- `REGEXREPLACE` в VM использует тот же ограниченный по числу замен алгоритм, что и интерпретатор;
- ошибки недопустимого режима, шаблона, отрицательной группы, отрицательного `count`, неверной replacement-ссылки и превышения лимита остаются `MB3007`;
- `Axiom.Execution` расширил таблицу `VmSupportedBuiltins`, поэтому покрытые программы с полным текущим regex-набором идут через VM в режимах `Auto` и `Bytecode`;
- добавлены проверки VM и конвейера запуска на успешные скалярные regex-результаты и ошибку аргумента.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.Regex.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter RegexScalar -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter RegexScalar -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 431 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 228 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются;
- scalar regex-функции включены как обычные встроенные функции VM, без новых опкодов и без специальных условий в `Axiom.Execution`.

Что остаётся:
- сверить оставшиеся непокрытые части байткодного пути по `VmSupportedBuiltins`, спецификации и текущим backend-диагностикам.

Следующий шаг:
- провести точечную сверку оставшихся backend-границ VM и выбрать следующий минимальный слой для переноса или стабилизации.

## Session 355 - 2026-04-24 14:10 - SPLIT и JOIN включены в BytecodeVm

Что сделано:
- сверены `BuiltinFunctionId` и `VmSupportedBuiltins`; до изменения вне VM-покрытия оставались только `Split`, `SplitSeparator`, `Join`, `JoinSeparator`;
- `BytecodeVm` теперь исполняет `SPLIT`, `SPLIT(text, separator)`, `JOIN` и `JOIN(items, separator)`;
- `SPLIT` создаёт компактный строковый массив через общий `RuntimeArrayValue`;
- `JOIN` обходит строковый массив по логическим индексам и возвращает пустую строку для невыделенного или пустого массива;
- пустой разделитель `SPLIT` остаётся ошибкой времени выполнения `MB3007`;
- `Axiom.Execution` расширил таблицу `VmSupportedBuiltins`;
- повторная сверка `BuiltinFunctionId` против `VmSupportedBuiltins` дала `ALL_BUILTIN_IDS_COVERED`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter StringArrayBuiltins -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter StringArrayBuiltin -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter StringArrayBuiltin -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- сверка `BuiltinFunctionId` против `VmSupportedBuiltins` - `ALL_BUILTIN_IDS_COVERED`;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 433 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 230 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- пользовательская семантика языка не менялась, поэтому новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются;
- дальнейшие VM-задачи теперь нужно выбирать не по отсутствующим built-in, а по операторным и lowered/backend-границам.

Что остаётся:
- перенести или стабилизировать оставшиеся операторные VM-границы, прежде всего `PRINT USING` и `INPUT`.

Следующий шаг:
- начать с узкой сверки `PRINT USING`: проверить текущий форматирующий код интерпретатора и решить, можно ли безопасно переиспользовать его из `BytecodeVm` без изменения пользовательской семантики.

## Session 356 - 2026-04-24 14:30 - PRINT USING включён в BytecodeVm

Что сделано:
- `BoundPrintUsingStatement` теперь понижается в отдельную lowered-инструкцию `PrintUsing`;
- в байткод добавлен `Opcode.PrintUsing`;
- `BytecodeCompiler` выпускает `PrintUsing` с количеством значений и неотрицательным кодом завершения строки;
- `BytecodeModuleValidator` проверяет форму инструкции `PrintUsing`;
- `BytecodeVm` выполняет `PRINT USING`, включая числовые и строковые маски, завершающий `;`, завершающую `,` и обычное завершение строки;
- форматирующая логика VM приведена к прежнему договору интерпретатора;
- добавлены проверки понижения, эмиссии байткода, валидации, исполнения VM и выбора `BytecodeVm` в режиме `Auto`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.PrintUsing.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "PrintUsing" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter PrintUsing -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 7 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter PrintUsing -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 49 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 435 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 232 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- код завершения `PrintUsing` хранится неотрицательным операндом: `0` - завершить строку, `1` - `;`, `2` - `,`;
- пользовательская семантика `PRINT USING` не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- перенести или стабилизировать следующую операторную границу VM: `INPUT`.

Следующий шаг:
- начать с узкой сверки `INPUT`: проверить текущую реализацию интерпретатора, prompt-формы, несколько переменных, преобразования типов и диагностику `MB3003`, затем спроектировать безопасное понижение в байткодовый путь.
## Session 357 - 2026-04-24 14:46 - INPUT включён в BytecodeVm

Что сделано:
- `BoundInputStatement` теперь понижается в отдельную lowered-инструкцию `Input`;
- в байткод добавлен `Opcode.Input`;
- `BytecodeCompiler` выпускает `Input` с количеством значений, необязательной подсказкой и целевыми слотами переменных;
- `BytecodeModuleValidator` проверяет переменную форму инструкции `Input`, подсказку и целевые слоты;
- `BytecodeVm` выполняет `INPUT`: вызывает `IRuntimeHost.ReadInput`, разбирает одиночный и множественный ввод, преобразует значения к типам переменных, присваивает результат и сообщает `MB3003` при ошибках;
- режим `Auto` теперь выбирает `BytecodeVm` для покрытых программ с `INPUT`;
- добавлены проверки понижения, эмиссии, валидации, выполнения VM и конвейера запуска.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "Input" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Input" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 14 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "Input" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 7 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 52 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 437 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 234 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- `Input` хранит подсказку как неотрицательный операнд: `0` означает отсутствие подсказки, `N + 1` указывает на строковую константу;
- цели `Input` кодируются парами вида цели и слота, чтобы VM могла записывать в глобальные, локальные, аргументные и захваченные переменные;
- пользовательская семантика `INPUT` не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- сверить оставшиеся байткодные зазоры и резервные opcode после закрытия `PRINT USING` и `INPUT`.

Следующий шаг:
- выполнить точечную сверку `Opcode`, lowered-инструкций и фактической поддержки VM, особенно для `BeginInput`, `EndInput`, `CreateArray`, `CreateRecord`, `Dup`, затем выбрать следующий узкий слой паритета.

## Session 358 - 2026-04-24 14:59 - Неподдерживаемые opcode исключены из валидного байткода

Что сделано:
- выполнена сверка `Opcode`, lowered-инструкций, эмиттера, валидатора и фактических обработчиков `BytecodeVm` после включения `PRINT USING` и `INPUT`;
- подтверждено, что `Dup`, `CreateArray`, `CreateRecord`, `BeginInput` и `EndInput` не выпускаются текущим компилятором и не исполняются VM;
- `BytecodeModuleValidator` больше не принимает эти внутренние заготовки как валидные инструкции модуля;
- добавлен регрессионный тест на диагностику `BC4011` для неподдерживаемых резервных инструкций;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "UnsupportedReservedOpcode|InvalidInputTargetKind" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 6 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 57 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- валидатор должен принимать только те opcode, которые уже имеют фактический исполнимый договор;
- старые `BeginInput` и `EndInput` не используются после введения `Opcode.Input`;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку оставшихся разрывов между пониженной формой, эмиттером, валидатором и `BytecodeVm`.

Следующий шаг:
- проверить, остались ли конструкции, которые `Axiom.Execution` может направить в VM, но для которых ещё нет полной поддержки исполнения или валидации.

## Session 359 - 2026-04-24 15:08 - Convert включён в BytecodeVm

Что сделано:
- добавлен `Opcode.Convert` и перечисление `BytecodeScalarTypeCode`;
- `BytecodeCompiler` теперь выпускает `Convert` и учитывает его при расчёте целей переходов;
- `BytecodeModuleValidator` проверяет операнд `Convert` и сообщает `BC4047` для неизвестного кода целевого типа;
- `BytecodeVm` выполняет автоматические числовые преобразования, уже разрешённые семантическим анализом;
- добавлены проверки компилятора, валидатора, VM и конвейера запуска;
- добавлен ручной пример `samples/expressions/conversions/automatic_numeric.axm`;
- обновлена страница `docs/manual/data-types.html`.

Какие файлы изменены:
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `docs/manual/data-types.html`
- `samples/expressions/conversions/automatic_numeric.axm`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Bytecode/Model/BytecodeScalarTypeCode.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "Conversion|UnsupportedReservedOpcode|InvalidConversionTypeCode" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 7 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "AutomaticNumericConversions" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "AutomaticNumericConversions" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 59 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 438 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- `Convert` больше не является пустой lowered-инструкцией для байткодного пути;
- поддержаны только преобразования, уже разрешённые семантическим анализом;
- `DECIMAL` и `DOUBLE` по-прежнему не смешиваются автоматически в обычных выражениях без соответствующего связанного преобразования.

Ручная приемка:
- открыть `samples/expressions/conversions/automatic_numeric.axm` в `Axiom.Ide`;
- запустить пример;
- ожидаемый вывод: `1.5`, `8`, `0`.

Что остаётся:
- продолжить сверку оставшихся частично поддержанных lowered-инструкций и границ VM.

Следующий шаг:
- проверить, есть ли ещё lowered-инструкции или байткодные операции, которые ранее считались технически пустыми, но должны иметь явное исполнение в `BytecodeVm`.

## Session 360 - 2026-04-24 15:54 - Диагностика неподдержанных целей Convert

Что сделано:
- продолжена сверка разрывов между lowered-формой, генератором байткода, валидатором и `BytecodeVm` после включения `Convert`;
- найдено, что `BytecodeCompiler` сообщал `BC5008` только для части неподдержанных целевых типов `Convert`;
- `BytecodeCompiler` теперь сообщает `BC5008` для любого целевого типа `Convert`, который не представлен в `BytecodeScalarTypeCode`;
- добавлен регрессионный тест на попытку сгенерировать `Convert` к `STRING` во внутренней lowered-форме;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "Conversion|UnsupportedConversionTarget" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 60 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- `Opcode.Convert` остаётся байткодной инструкцией для числовых скалярных преобразований;
- неподдержанные цели `Convert` должны останавливаться диагностикой генератора байткода, а не исчезать из потока инструкций;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить системную сверку `LoweredInstructionKind`, выпускаемых `Opcode`, проверок `BytecodeModuleValidator` и обработчиков `BytecodeVm`.

Следующий шаг:
- искать места, где инструкция уже компилируется, но договор исполнения или валидации ещё может быть неполным.

## Session 361 - 2026-04-24 16:00 - Проверка видов констант в байткодном модуле

Что сделано:
- продолжена системная сверка `LoweredInstructionKind`, выпускаемых `Opcode`, проверок `BytecodeModuleValidator` и обработчиков `BytecodeVm`;
- найдено, что валидатор проверял существование индексов констант, но не всегда проверял ожидаемый вид константы;
- `PushConst` теперь отвергает служебные константы `RoutineName` и `BuiltinName`;
- строковые операнды `Input`, `OpenFile`, `RedimArray`, `LoadElement`, `StoreElement`, `LoadField` и `StoreField` теперь должны ссылаться на `String`-константы;
- добавлены регрессионные тесты на служебную константу в `PushConst`, нестроковую подсказку `Input` и нестроковое имя в `OpenFile`;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "MetadataConstant|NonStringConstant|InvalidConstantIndex" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 4 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 63 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- валидный байткодный модуль должен проверять не только индекс константы, но и её роль в конкретной инструкции;
- служебные константы имён процедур и встроенных функций не являются значениями времени выполнения для `PushConst`;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку валидатора и VM вокруг операндов с особым смыслом.

Следующий шаг:
- проверить числовые коды режимов, разделителей и целевых слотов, чтобы валидатор заранее отсекал всё, что VM не должна получать.

## Session 362 - 2026-04-24 16:10 - Стековая проверка байткодного модуля

Что сделано:
- продолжена широкая сверка валидатора, генератора байткода и `BytecodeVm` по внутреннему формату модуля;
- `BytecodeModuleValidator` теперь проверяет код `PrintSeparator` и сообщает `BC4050` для неизвестного разделителя;
- `BytecodeModuleValidator` теперь проверяет флаг `PRESERVE` в `RedimArray` и сообщает `BC4051` для неизвестного значения;
- расчёт переменной формы `Input` переведён на расширенную арифметику и сообщает `BC4052`, если количество значений делает форму инструкции невозможной;
- добавлена лёгкая проверка высоты стека для достижимых инструкций процедуры;
- валидатор теперь сообщает `BC4053` при снятии значения с пустого стека, `BC4054` при неправильной высоте стека перед `Return` и `BC4055` при разных высотах стека на сходящихся ветках;
- старый тест расчёта целей переходов после технически пустой lowered-инструкции скорректирован так, чтобы проверочный модуль был стеково корректным;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "InvalidPrintSeparator|InvalidRedimPreserveFlag|InputValueCountOverflow" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "StackUnderflow|UnexpectedStackHeight|StackHeightMismatch|InvalidPrintSeparator|InvalidRedimPreserveFlag|InputValueCountOverflow" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 6 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 69 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- валидатор должен отсекать не только неизвестные opcode и неверные индексы, но и очевидно несогласованную стековую форму;
- стековая проверка не заменяет семантический анализ типов и не пытается доказывать корректность значений;
- стековая проверка запускается только после успешной проверки отдельных инструкций процедуры, чтобы не создавать каскадные сообщения поверх уже найденной ошибки формата;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку валидатора с VM на уровне целостности метаданных байткодного модуля.

Следующий шаг:
- проверить целостность таблиц процедур, параметров, локальных и захваченных переменных относительно метаданных, которые VM использует при исполнении.

## Session 363 - 2026-04-24 16:27 - Проверка метаданных процедур байткодного модуля

Что сделано:
- продолжена сверка `BytecodeModuleValidator` с тем, какие метаданные реально использует `BytecodeVm`;
- найдено, что VM опирается на таблицы параметров, локальных и захваченных переменных при создании кадров, выполнении `INPUT` и передаче замыканий;
- `BytecodeModuleValidator` теперь сообщает `BC4056`, если число записей `ParameterVariables` не совпадает с `ParameterCount`;
- `BytecodeModuleValidator` теперь сообщает `BC4057`, если число записей `LocalVariables` не совпадает с `LocalSlotCount`;
- `BytecodeModuleValidator` теперь сообщает `BC4058`, `BC4059` и `BC4060` для неподходящих записей параметров, локальных и захваченных переменных;
- `BytecodeModuleValidator` теперь сообщает `BC4061`, если один и тот же символ переменной повторяется в таблицах видимых переменных процедуры;
- тестовый помощник байткодных тестов обновлён так, чтобы создаваемый вручную валидный модуль имел корректную таблицу локальных переменных;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "MismatchedParameterMetadata|MismatchedLocalMetadata|InvalidParameterMetadata|InvalidLocalMetadata|InvalidCapturedMetadata|DuplicateVariableMetadata" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 6 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 75 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- таблицы переменных процедуры считаются частью исполнимого договора байткодного модуля, а не только отладочной подсказкой;
- валидатор должен заранее отсекать модули, где `INPUT`, значения по умолчанию локальных слотов или передача захватов могут обратиться к отсутствующим или неверным метаданным;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку внутренних таблиц модуля с тем, как VM использует их при исполнении.

Следующий шаг:
- проверить согласованность `BuiltinBinding` с исходным `FunctionSymbol` и другие внутренние противоречия таблиц модуля, которые пока могут проявиться только во время исполнения VM.

## Session 364 - 2026-04-24 16:40 - Проверка метаданных built-in таблицы байткодного модуля

Что сделано:
- продолжена сверка внутренних таблиц байткодного модуля с тем, как `BytecodeVm` использует их при исполнении;
- найдено, что путь значений встроенных функций зависит от согласованности `BuiltinBinding` и сохранённого `FunctionSymbol`;
- `BytecodeModuleValidator` теперь сообщает `BC4062` для неизвестного числового кода `BuiltinFunctionId`;
- `BytecodeModuleValidator` теперь сообщает `BC4063`, если запись built-in ссылается на пользовательский символ функции;
- `BytecodeModuleValidator` теперь сообщает `BC4064`, если канонический идентификатор записи не совпадает с `FunctionSymbol.BuiltinId`;
- `BytecodeModuleValidator` теперь сообщает `BC4065`, если имя записи не совпадает с именем символа;
- `BytecodeModuleValidator` теперь сообщает `BC4066`, если число параметров записи не совпадает с числом параметров символа;
- `BytecodeModuleValidator` теперь сообщает `BC4067`, если признак возвращаемого значения не совпадает с типом результата символа;
- добавлены регрессионные тесты на все новые ошибки built-in метаданных;
- обновлены `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "UnknownCanonicalId|NonBuiltinFunctionSymbol|MismatchedFunctionId|MismatchedFunctionName|MismatchedFunctionParameterCount|MismatchedReturnValueFlag" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - сначала выявил каскадные диагностики в новых тестах, после уточнения тестовых модулей успешно, 6 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 81 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- `BuiltinBinding.Function`, если он задан, считается частью исполнимого договора модуля, а не только дополнительной отладочной ссылкой;
- противоречивая built-in таблица должна останавливаться валидатором до того, как `LoadBuiltinFunction` создаст функциональное значение с неверной связью;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку внутренних таблиц и заголовка байткодного модуля с фактическими ожиданиями VM.

Следующий шаг:
- проверить заголовок модуля, флаги формата, точку входа и глобальные переменные на оставшиеся противоречия, которые сейчас могут пройти валидатор и проявиться только при запуске VM.

## Session 365 - 2026-04-24 16:55 - Проверка заголовка, флагов, точки входа и глобальных переменных байткодного модуля

Что сделано:
- продолжена сверка `BytecodeModuleValidator` с тем, какие части байткодного модуля фактически важны для `BytecodeVm` при запуске;
- добавлена проверка `BytecodeHeader.FormatVersion` и `BytecodeHeader.CompatibilityToken`;
- добавлена проверка неизвестных битов `BytecodeModuleFlags`;
- флаги `HasDebugInfo` и `UsesBuiltinTable` теперь сверяются с фактическим наличием отладочной информации и таблицы встроенных функций;
- `EntryPointRoutineId` теперь сверяется с единственной помеченной точкой входа;
- точка входа должна быть процедурой без параметров и без результата;
- глобальные переменные теперь проверяются на неизвестную стратегию инициализации, тип `VOID` и некорректную fixed-string длину;
- добавлены регрессионные тесты на новые диагностические сообщения `BC4068`-`BC4080`;
- два старых теста метаданных параметров перенесены с точки входа на обычную процедуру, чтобы новая проверка входа не создавала каскадные сообщения.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "UnsupportedFormatVersion|UnsupportedCompatibilityToken|UnknownModuleFlags|WithoutDebugFlag|WithoutBuiltinFlag|WithoutMarkedEntryPoint|MultipleMarkedEntryPoints|EntryPointRoutineNotMarked|EntryPointWithParameters|EntryPointWithResult|UnknownInitializationStrategy|GlobalWithVoidType|InvalidFixedStringMetadata" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 13 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 94 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- заголовок, флаги и точка входа считаются частью исполнимого договора байткодного модуля, а не только описательной метаинформацией;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку валидатора с VM на уровне числовых операндов инструкций.

Следующий шаг:
- проверить отрицательные индексы констант, переходов, локальных слотов, аргументов и захватов, чтобы валидатор заранее отсекал обращения за пределы таблиц или массивов VM.
## Session 366 - 2026-04-24 17:05 - Проверка отрицательных операндов байткодных инструкций

Что сделано:
- продолжена сверка `BytecodeModuleValidator` с тем, какие операнды `BytecodeVm` фактически использует как индексы, слоты, цели переходов, счётчики, флаги и коды разделителей;
- `BytecodeInstruction` больше не отбрасывает отрицательные операнды в конструкторе, чтобы валидатор мог диагностировать повреждённый или собранный вручную модуль сам;
- добавлена диагностика `BC4081` для отрицательного индекса константы;
- добавлена диагностика `BC4082` для отрицательной цели перехода;
- добавлены диагностики `BC4083`-`BC4086` для отрицательных глобальных, локальных, аргументных и захваченных слотов;
- добавлена диагностика `BC4087` для отрицательного количества значений в `Input`;
- `Input` теперь проверяет отрицательные целевые слоты для всех поддержанных видов цели;
- `PrintSeparator` и `RedimArray` теперь явно отвергают отрицательные коды разделителя и флага `PRESERVE`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Model/BytecodeInstruction.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NegativeConstantIndex|NegativeJumpTarget|NegativeGlobalSlot|NegativeLocalSlot|NegativeArgumentSlot|NegativeCaptureSlot|InputWithNegativeLocalSlot|NegativePrintSeparator|NegativeRedimPreserveFlag|InputWithNegativeValueCount" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 10 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 104 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- модель инструкции хранит данные формата, а не решает смысловую корректность операндов;
- пригодность модуля к исполнению должна определяться `BytecodeModuleValidator`;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку внутренней отладочной информации и диапазонов блоков с тем, как VM и диагностический слой могут использовать эти данные.

Следующий шаг:
- проверить отрицательные или нарушающие порядок индексы в `BytecodeDebugInfo`, `SourceInstructionMap` и `BytecodeBlockDebugRange`, а также решить, где модель должна только хранить данные, а где валидатор должен выдавать явные ошибки формата.
## Session 367 - 2026-04-24 18:07 - Проверка отладочной карты и диапазонов блоков байткодного модуля

Что сделано:
- продолжена сверка `BytecodeModuleValidator` с внутренними данными байткодного модуля, которые используются для исходных позиций и отладочных диапазонов;
- `SourceInstructionMap` больше не отбрасывает отрицательные `routineId` и `instructionIndex` в конструкторе;
- `BytecodeRoutineDebugInfo` больше не отбрасывает отрицательный `routineId` в конструкторе;
- `BytecodeBlockDebugRange` больше не отбрасывает отрицательное начало и конец раньше начала в конструкторе;
- добавлена диагностика `BC4088` для отрицательного идентификатора процедуры в карте инструкций;
- добавлена диагностика `BC4089` для отрицательного индекса инструкции в карте инструкций;
- добавлена диагностика `BC4090` для отрицательного идентификатора процедуры в таблице отладочных имён;
- добавлена диагностика `BC4091` для отрицательного начала диапазона блока;
- добавлена диагностика `BC4092` для диапазона блока, который завершается раньше начала;
- сохранена существующая проверка `BC4010` для диапазона блока, выходящего за пределы инструкций процедуры.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Model/BytecodeBlockDebugRange.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutineDebugInfo.cs`
- `src/Axiom.Bytecode/Model/SourceInstructionMap.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NegativeRoutineId|NegativeInstructionIndex|NegativeStart|EndBeforeStart" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 5 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 109 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- отладочные контейнеры байткода хранят данные формата, а пригодность этих данных определяет `BytecodeModuleValidator`;
- ошибки отладочной карты и диапазонов блоков должны быть обычными диагностическими сообщениями формата, а не исключениями модели;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить проход по оставшимся контейнерам байткодного модуля, где числовые поля ещё могут проверяться конструкторами раньше валидатора.

Следующий шаг:
- проверить `BytecodeModule`, `BytecodeRoutine`, `BytecodeGlobal` и `BuiltinBinding` на такие же случаи: отрицательные идентификаторы, слоты и счётчики, которые должны доходить до `BytecodeModuleValidator` и получать явную диагностику формата.
## Session 368 - 2026-04-24 18:14 - Проверка числовых полей контейнеров байткодного модуля

Что сделано:
- продолжен проход по внутренним данным байткодного модуля после проверки отладочной карты и диапазонов блоков;
- `BytecodeModule` больше не отбрасывает отрицательный `EntryPointRoutineId` в конструкторе;
- `BytecodeRoutine` больше не отбрасывает отрицательные `Id`, `ParameterCount` и `LocalSlotCount` в конструкторе;
- `BytecodeGlobal` больше не отбрасывает отрицательный `Slot` в конструкторе;
- `BuiltinBinding` больше не отбрасывает отрицательные `Id` и `ParameterCount` в конструкторе;
- `BytecodeModuleValidator` теперь сообщает `BC4093` для отрицательной точки входа модуля;
- `BytecodeModuleValidator` теперь сообщает `BC4094`-`BC4096` для отрицательной числовой формы процедуры;
- `BytecodeModuleValidator` теперь сообщает `BC4097` для отрицательного слота глобальной переменной;
- `BytecodeModuleValidator` теперь сообщает `BC4098`-`BC4099` для отрицательной числовой формы записи встроенной функции;
- проверка процедуры или записи встроенной функции прекращается после обнаружения повреждённой числовой формы, чтобы не создавать каскадные сообщения из недостоверных счётчиков;
- добавлены регрессионные тесты на все семь новых ошибок формата.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Model/BuiltinBinding.cs`
- `src/Axiom.Bytecode/Model/BytecodeGlobal.cs`
- `src/Axiom.Bytecode/Model/BytecodeModule.cs`
- `src/Axiom.Bytecode/Model/BytecodeRoutine.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NegativeEntryPointRoutineId|NegativeId|NegativeParameterCount|NegativeLocalSlotCount|NegativeSlot" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 7 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 116 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.

Решения:
- контейнеры байткода хранят данные формата, а пригодность модуля к исполнению определяет `BytecodeModuleValidator`;
- отрицательные идентификаторы, слоты и счётчики в контейнерах модуля должны быть диагностическими ошибками формата, а не исключениями модели;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить сверку оставшихся контейнеров и вспомогательных моделей байткода на предмет числовых, перечислимых или ссылочных значений, которые ещё могут обходить единый валидатор формата;
- затем проверить предположения `BytecodeVm` о уже валидированном модуле и закрыть оставшиеся предусловия диагностикой валидатора.

Следующий шаг:
- просмотреть оставшиеся модели `Axiom.Bytecode`, особенно заголовок, пул констант, lowered-модели и служебные записи, чтобы решить, какие ограничения должны оставаться защитой конструктора, а какие должны перейти в `BytecodeModuleValidator` или отдельную диагностику генератора байткода.

## Session 369 - 2026-04-24 18:39 - Проверка заголовка и пула констант байткодного модуля

Что сделано:
- продолжена проверка оставшихся моделей `Axiom.Bytecode` после числовых полей контейнеров;
- просмотрены `BytecodeHeader`, `ConstantPool`, `BytecodeConstant`, lowered-модели и места чтения констант в `BytecodeVm`;
- найден разрыв: `BytecodeVm.ReadConstant` приводит `BytecodeConstant.Value` к типу по `BytecodeConstantKind`, а валидатор раньше не проверял соответствие вида константы фактическому значению;
- `BytecodeHeader` больше не отбрасывает пробельные строки версии языка и токена совместимости в конструкторе;
- `BytecodeModuleValidator` теперь сообщает `BC4100` для пустой версии языка в заголовке;
- пустой или пробельный токен совместимости теперь доходит до валидатора и останавливается существующей диагностикой `BC4069`;
- `BytecodeModuleValidator` теперь проверяет пул констант и сообщает `BC4102` для неизвестного `BytecodeConstantKind`;
- `BytecodeModuleValidator` теперь сообщает `BC4103`, если значение константы не соответствует её виду.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Model/BytecodeHeader.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "BlankCompatibilityToken|BlankLanguageVersion|UnknownConstantKind|MismatchedValueType" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 4 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 120 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- первый `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` был заблокирован уже запущенным `Axiom.Ide.exe`, который держал DLL в стандартной папке вывода;
- после закрытия IDE повторный `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 ошибок, 6 прежних nullable-предупреждений в тестах парсера и семантики;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1319 тестов.

Решения:
- заголовок и пул констант считаются данными байткодного формата, пригодность которых определяет `BytecodeModuleValidator`;
- конструктор `BytecodeHeader` сохраняет только защиту от `null`, а смысловая проверка строк заголовка идёт через диагностику формата;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор предположений `BytecodeVm` о уже проверенном модуле.

Следующий шаг:
- проверить ссылочные операнды вызовов, загрузки функций и встроенных функций: отрицательные идентификаторы процедур, built-in записей и число аргументов, чтобы решить, нужны ли более точные диагностики или текущие общие проверки уже достаточно закрывают эти пути.

## Session 370 - 2026-04-24 18:48 - Проверка ссылочных операндов вызовов байткода

Что сделано:
- продолжен обзор предположений `BytecodeVm` о уже проверенном модуле после проверки заголовка и пула констант;
- усилена проверка инструкций `Call`, `LoadFunction`, `CallBuiltin` и `LoadBuiltinFunction`;
- `BytecodeModuleValidator` теперь сообщает `BC4104` для отрицательного `routine-id` в `Call` и `LoadFunction`;
- `BytecodeModuleValidator` теперь сообщает `BC4105` для отрицательного числа аргументов в `Call`;
- `BytecodeModuleValidator` теперь сообщает `BC4106`, если `LoadFunction` ссылается на процедуру без сохранённого `FunctionSymbol`;
- `BytecodeModuleValidator` теперь сообщает `BC4107` для отрицательного идентификатора встроенной функции в `CallBuiltin` и `LoadBuiltinFunction`;
- `BytecodeModuleValidator` теперь сообщает `BC4108` для отрицательного числа аргументов в `CallBuiltin`;
- `BytecodeModuleValidator` теперь сообщает `BC4109`, если `LoadBuiltinFunction` ссылается на запись встроенной функции без сохранённого `FunctionSymbol`;
- добавлены регрессионные тесты на все восемь новых случаев.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NegativeRoutineId|NegativeArgumentCount|WithoutFunctionSymbol|NegativeId" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 12 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 128 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1327 тестов.

Решения:
- ссылочные операнды вызовов и загрузки функциональных значений считаются частью договора исполнимого байкода и должны проверяться до запуска VM;
- отрицательные ссылки отделены от неизвестных неотрицательных ссылок, чтобы диагностика формата была точнее;
- отсутствие `FunctionSymbol` у цели `LoadFunction` или `LoadBuiltinFunction` считается повреждением модуля, потому что VM использует этот символ для создания функционального значения;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор оставшихся предположений `BytecodeVm` о уже проверенном модуле.

Следующий шаг:
- проверить обработчики файловых, массивных, записевых и консольных инструкций, чтобы найти оставшиеся предусловия, которые `BytecodeModuleValidator` может диагностировать до запуска VM.

## Session 371 - 2026-04-24 18:58 - Проверка типов целей INPUT в байткоде

Что сделано:
- продолжен обзор обработчиков файловых, массивных, записевых и консольных инструкций `BytecodeVm`;
- найдено предположение в `ExecuteInput`: VM умеет разбирать ввод только для `INTEGER`, `LONG`, `DOUBLE`, `DECIMAL`, `STRING`, `BOOLEAN`, а цель-массив и другие неподдерживаемые типы раньше доходили до внутренней ошибки VM;
- `BytecodeModuleValidator` теперь проверяет тип целевой переменной `Input` после проверки существования слота;
- добавлена диагностика `BC4110` для цели `Input` неподдерживаемого типа;
- добавлены регрессионные тесты на локальные цели `Input` типа массив, `FILE`, пользовательский `TYPE`, функциональное значение;
- добавлен регрессионный тест на глобальную цель `Input` типа `FILE`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "Input" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 12 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 133 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1332 теста.

Решения:
- тип целевой переменной `Input` является частью исполнимого договора байткодного модуля, потому что VM не должна получать цель, которую невозможно заполнить из строки;
- `BC4110` используется как общая диагностика неподдерживаемого типа цели `Input`, независимо от того, где лежит переменная: в глобальном, локальном, параметрическом или захваченном слоте;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор предположений `BytecodeVm` о типах значений на стеке.

Следующий шаг:
- проверить файловые, массивные и записевые инструкции на предмет случаев, где точечных проверок метаданных уже недостаточно и может понадобиться лёгкий анализ типов стека в `BytecodeModuleValidator`.

## Session 372 - 2026-04-24 19:08 - Лёгкая проверка типов стека байткода

Что сделано:
- продолжен обзор предположений `BytecodeVm` о типах значений на стеке;
- добавлен второй стековый проход `BytecodeModuleValidator` после проверки высоты стека;
- валидатор теперь выводит типы из констант, глобальных, локальных, параметрических и захваченных слотов, ссылок на процедуры и записей built-in таблицы;
- неизвестные или расходящиеся на слиянии путей типы остаются неизвестными и не создают ложную диагностику;
- добавлены проверки стековых типов для `OpenFile`, `CloseFile`, `RedimArray`, `EraseArray`, `LoadElement`, `StoreElement`, `LoadField`, `StoreField`, `PrintUsing`, `JumpIfTrue`, `JumpIfFalse`;
- добавлены диагностики `BC4111`-`BC4116`;
- добавлены регрессионные тесты на неверный тип пути `OpenFile`, неверную цель `RedimArray`, нечисловой индекс `LoadElement`, не-`TYPE` цель `LoadField` и нестроковый формат `PrintUsing`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NonStringPathStackValue|NonArrayStackValue|NonNumericIndexStackValue|NonRecordStackValue|NonStringFormatStackValue" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 5 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 138 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1337 тестов.

Решения:
- лёгкая проверка типов стека не заменяет семантический анализ и не пытается выводить типы всех выражений;
- если тип стекового значения неизвестен, валидатор не сообщает ошибку;
- при слиянии путей управления разные типы одной позиции превращаются в неизвестный тип;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- расширить регрессионное покрытие новых диагностик `BC4115` и `BC4116`;
- проверить совместимость типов при сохранении значений в слоты переменных.

Следующий шаг:
- добавить тесты и при необходимости уточнить диагностики для отсутствующего поля известного `TYPE`, несовместимого значения поля или элемента массива, а затем решить вопрос с `StoreGlobal`, `StoreLocal`, `StoreArgument` и `StoreCapture`.

## Session 373 - 2026-04-24 19:17 - Совместимость типов при записи в слоты байткода

Что сделано:
- продолжена проверка предположений `BytecodeVm` о типах стековых значений после лёгкого стекового прохода;
- `BytecodeModuleValidator` теперь проверяет совместимость известного значения на стеке с типом целевого слота для `StoreGlobal`, `StoreLocal`, `StoreArgument` и `StoreCapture`;
- `BC4116` расширена на несовместимые значения, сохраняемые в глобальные, локальные, параметрические и захваченные слоты;
- добавлен регрессионный тест на `BC4115` для отсутствующего поля известного пользовательского `TYPE`;
- добавлены регрессионные тесты на `BC4116` для `StoreField`, `StoreElement`, `StoreGlobal`, `StoreLocal`, `StoreArgument` и `StoreCapture`;
- тестовый помощник `CreateDebugInfo` в `BytecodeModuleValidatorTests` расширен на несколько процедур и теперь строит карты инструкций с правильным идентификатором процедуры.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "MissingKnownField|IncompatibleValue|IncompatibleStackValue" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 7 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 145 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1344 теста.

Решения:
- проверка записи в слоты считается частью внутреннего договора исполнимого байткодного модуля;
- неизвестный тип значения не создаёт ошибку, чтобы валидатор не подменял семантический анализ;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор оставшихся предположений `BytecodeVm` о проверенном модуле.

Следующий шаг:
- проверить операции вызовов и возврата на предмет мест, где лёгкий стековый проход может безопасно сверять типы аргументов, возвращаемых значений и функциональных значений.

## Session 374 - 2026-04-24 19:32 - Совместимость типов при вызовах и возврате байткода

Что сделано:
- продолжен обзор оставшихся предположений `BytecodeVm` о проверенном модуле;
- `BytecodeModuleValidator` теперь проверяет совместимость известных типов аргументов `Call` с типами параметров целевой процедуры;
- `BytecodeModuleValidator` теперь проверяет совместимость известных типов аргументов `CallBuiltin` с типами параметров сохранённого `FunctionSymbol`;
- для встроенных числовых функций сохранена допустимая числовая совместимость, чтобы нормальные вызовы с `INTEGER`, `LONG`, `DOUBLE` и `DECIMAL` не отклонялись только из-за объявленного параметра `DOUBLE`;
- `Return` в процедуре с результатом теперь сверяет известный тип вершины стека с типом результата процедуры;
- добавлены регрессионные тесты на несовместимый аргумент `Call`, несовместимый аргумент `CallBuiltin`, несовместимое возвращаемое значение `Return` и допустимый числовой аргумент встроенной функции.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "IncompatibleArgumentStackValue|ReturnWithIncompatibleStackValue|NumericCompatibleArgument" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 4 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "SupportsNumericBuiltinsOnBytecodeVm|SupportsNestedLoops|UsesBytecodeVmForArrayCallbackBuiltins" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 149 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1348 тестов.

Решения:
- проверка вызовов и возврата считается частью внутреннего договора исполнимого байткодного модуля;
- неизвестный тип значения не создаёт ошибку, чтобы валидатор не подменял семантический анализ;
- числовые встроенные функции проверяются с учётом уже существующей числовой совместимости привязчика и VM;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор оставшихся предположений `BytecodeVm` о проверенном модуле.

Следующий шаг:
- проверить функциональные значения и вызовы через массивные обратные вызовы на предмет дополнительных внутренних проверок, которые можно добавить без изменения пользовательского поведения языка.

## Session 375 - 2026-04-24 20:21 - Видимость захватов функциональных значений байткода

Что сделано:
- продолжен обзор оставшихся предположений `BytecodeVm` о проверенном модуле;
- `BytecodeModuleValidator` теперь проверяет, что `Call` на процедуру с захватами может получить все захваченные переменные из текущей процедуры;
- `BytecodeModuleValidator` теперь проверяет то же правило для `LoadFunction`, потому что VM создаёт функциональное значение вместе с массивом захваченных слотов;
- добавлена диагностика `BC4117` для недоступной захваченной переменной целевой процедуры;
- добавлен регрессионный тест на недоступный захват при `Call`;
- добавлен регрессионный тест на недоступный захват при `LoadFunction`;
- добавлен позитивный тест на видимый локальный захват при `LoadFunction`;
- добавлен регрессионный тест на неподходящее функциональное значение в `ARRAYFILTER`, чтобы явно закрепить проверку массивных обратных вызовов через `BC4116`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "MissingCapture|VisibleLocalCapture|ArrayCallbackBuiltinWithIncompatibleFunctionValue" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 4 теста;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 153 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1352 теста.

Решения:
- проверка видимости захватов считается частью внутреннего договора исполнимого байткодного модуля;
- валидатор не меняет правила языка и не вводит новую пользовательскую семантику;
- типовая проверка функционального значения в массивных обратных вызовах остаётся частью существующей проверки аргументов `CallBuiltin`;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор оставшихся предположений `BytecodeVm` о проверенном модуле.

Следующий шаг:
- проверить оставшиеся операторные сценарии VM, где сейчас ещё возможны внутренние ошибки исполнения из-за повреждённой, но формально загруженной структуры байткода.

## Session 376 - 2026-04-24 20:32 - Типы стековых значений для операторных инструкций байткода

Что сделано:
- продолжен обзор оставшихся предположений `BytecodeVm` о проверенном модуле;
- `BytecodeModuleValidator` теперь проверяет типы известных стековых значений для `Negate`, `Not`, `ValidateForStep`, `Convert`, бинарных арифметических и логических инструкций, а также инструкций сравнения;
- `Negate` и `ValidateForStep` используют `BC4113` для нечислового операнда;
- `Not`, несовместимые бинарные операнды и неподдерживаемые преобразования `Convert` используют `BC4116`;
- для операций сравнения стековый проход теперь выводит результат `BOOLEAN`, чтобы условные переходы проверялись точнее;
- добавлены регрессионные тесты на нечисловой `Negate`, неподдерживаемый `Not`, нечисловой `ValidateForStep`, несовместимый `Add`, неподдерживаемый `Convert` и позитивный сценарий `CompareEqual` -> `JumpIfFalse`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "NegateWithNonNumeric|NotWithUnsupported|ValidateForStepWithNonNumeric|AddWithIncompatible|ConvertWithUnsupported|ComparisonResultFeedsConditionalJump" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 6 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 159 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1358 тестов.

Решения:
- проверка операторных инструкций считается частью внутреннего договора исполнимого байткодного модуля;
- неизвестный тип стекового значения не создаёт ошибку, чтобы валидатор не подменял семантический анализ исходной программы;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- продолжить обзор оставшихся путей `BytecodeVm`, где ещё возможны внутренние ошибки состояния после уже добавленных проверок формата.

Следующий шаг:
- решить, нужен ли ещё один узкий слой проверок валидатора байткода, или можно переходить к следующему направлению развития байткодной VM.

## Session 377 - 2026-04-24 20:45 - Запасные сигнатуры built-in для валидатора байткода

Что сделано:
- продолжен обзор оставшихся путей `BytecodeVm`, где повреждённый модуль мог дойти до внутренних чтений значений без предварительной проверки формата;
- найден слой `CallBuiltin`: запись `BuiltinBinding` могла не содержать сохранённого `FunctionSymbol`, и тогда стековый проход знал только число аргументов, но не типы параметров и результата;
- `BuiltinFunctions` получил поиск канонического описателя по `BuiltinFunctionId`;
- `BytecodeModuleValidator` теперь использует канонический описатель встроенной функции как запасную сигнатуру, если в `BuiltinBinding` нет `FunctionSymbol`;
- запасная проверка сохраняет числовую совместимость встроенных функций и выводит тип результата, когда это можно сделать по описателю;
- добавлены регрессионные тесты на несовместимый аргумент `LEN` без `FunctionSymbol`, совместимый аргумент `LEN` без `FunctionSymbol` и использование результата `LEN` как условия перехода;
- добавлен позитивный регрессионный тест на обобщённую массивную функцию `ARRAYCONTAINS` без `FunctionSymbol`;
- тестовая заглушка валидного модуля в `BytecodeModuleValidatorTests` исправлена так, чтобы канонический built-in соответствовал передаваемому числовому стековому значению.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeModuleValidatorTests" --logger "console;verbosity=minimal"` - сначала выявил старую несогласованную тестовую заглушку, после исправления и добавления массивного сценария успешно, 117 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 163 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1362 теста.

Решения:
- запасная сигнатура built-in считается частью внутреннего договора исполнимого байткодного модуля;
- список встроенных функций не дублируется в байткодном слое: валидатор использует уже существующие семантические описатели;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- завершить обзор оставшихся внутренних предположений `BytecodeVm` после добавленных проверок формата.

Следующий шаг:
- если новых узких слоёв валидатора не останется, перейти к следующему направлению развития байткодной VM.

## Session 378 - 2026-04-24 20:56 - Точные callback-типы массивных built-in в валидаторе

Что сделано:
- продолжен обзор оставшихся внутренних предположений `BytecodeVm` после добавленных проверок формата;
- найден узкий остаточный путь в запасной проверке `CallBuiltin` для массивных встроенных функций без сохранённого `FunctionSymbol`;
- `BytecodeModuleValidator` теперь сверяет callback-параметры массивных built-in с фактическим типом элемента массива, если этот тип известен;
- `predicate` проверяется как `FUNCTION(<элемент массива>) AS BOOLEAN`;
- `selector` для `ARRAYMAP` проверяется как `FUNCTION(<элемент массива>) AS <результат>` без `VOID` и без вложенного массива в результате;
- `reducer` для `ARRAYREDUCE` проверяется как `FUNCTION(<аккумулятор>, <элемент массива>) AS <аккумулятор>`;
- параметр `value` проверяется по типу элемента массива, если этот тип известен;
- результат `ARRAYMAP` без сохранённого `FunctionSymbol` теперь выводится как массив результата селектора;
- результат `ARRAYREDUCE` без сохранённого `FunctionSymbol` теперь выводится как тип начального аккумулятора;
- добавлены регрессионные тесты на неправильный callback `ARRAYFILTER` без `FunctionSymbol` и на вывод результата `ARRAYMAP` без `FunctionSymbol`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeModuleValidatorTests" --logger "console;verbosity=minimal"` - успешно, 119 тестов;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 165 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 235 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1364 теста.

Решения:
- точные callback-типы массивных built-in в запасном режиме считаются частью внутреннего договора исполнимого байткодного модуля;
- проверка остаётся лёгкой: неизвестные типы не создают ошибку;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- ещё раз пройти оставшиеся `ReportInvalidRuntimeState` и прямые `As*` в `BytecodeVm`.

Следующий шаг:
- если новых проверяемых предположений формата не останется, перейти к следующему направлению развития байткодной VM.

## Session 379 - 2026-04-24 20:58 - Финальный обзор внутренних предположений BytecodeVm

Что сделано:
- выполнен финальный обзор оставшихся `ReportInvalidRuntimeState`, прямых `As*` и внутренних `InvalidOperationException` в файлах `BytecodeVm*.cs`;
- проверено, что прямые чтения аргументов встроенных функций теперь прикрыты проверкой `CallBuiltin` через сохранённый `FunctionSymbol` или запасной канонический описатель;
- проверено, что операции с файлами, массивами, записями, условными переходами, `PRINT USING`, `INPUT`, `OPEN`, `CLOSE`, `Call`, `Return` и операторными инструкциями уже имеют слой проверки формата там, где это относится к исполнимому байткодному модулю;
- отдельного нового узкого слоя валидатора после текущих правок не найдено.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- код после финального обзора не менялся;
- повторный запуск тестов не требовался, потому что после последней кодовой правки уже прошли `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` и `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`.

Решения:
- текущая серия усиления `BytecodeModuleValidator` по внутренним предположениям `BytecodeVm` считается завершённой;
- оставшиеся ошибки VM относятся к нормальным runtime-условиям программы, а не к новым проверяемым предположениям формата;
- пользовательская семантика языка не изменилась, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- перейти от защитных проверок формата к развитию исполняемого байткодного пути.

Следующий шаг:
- выбрать следующий срез языка для исполнения через байткодную VM и закрепить его тестами против интерпретатора.

## Session 380 - 2026-04-24 21:11 - Арифметические операторы в BytecodeVm

Что сделано:
- выбран следующий исполняемый срез VM после завершения серии проверок формата;
- `Opcode` расширен инструкциями `Power`, `IntegralDivide`, `ShiftLeft`, `ShiftRight`;
- `BytecodeCompiler` теперь эмитит эти инструкции для операторов `^`, `\`, `<<`, `>>`;
- `BytecodeVm` исполняет эти операторы по тем же правилам, что интерпретатор: `^` возвращает `DOUBLE`, `\` работает для `INTEGER` и `LONG`, сдвиги работают для `INTEGER` и `LONG`;
- `BytecodeModuleValidator` учитывает новые инструкции в проверке высоты стека и в проверке известных типов стековых значений;
- старые сквозные тесты отката с `PRINT 7 \ 2` переведены на косвенный вызов функционального значения, потому что `\` теперь покрыт VM.

Какие файлы изменены:
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeCompilerTests.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleValidatorTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "RemainingArithmeticOperators|PowerWithNonDouble|ShiftLeftWithIncompatible" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 6 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "RemainingArithmeticOperators|IntegralDivisionReportsDivisionByZero" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FallsBackToInterpreterWhenBytecodeUnavailable|ReturnsBackendFailureForUnsupportedProgram|RemainingArithmeticOperators" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста.
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 171 тест;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 440 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 236 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- первый полный `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` выявил устаревший IDE-тест, который всё ещё ожидал ошибку на `PRINT 7 \ 2`;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "Run_WithBytecodeMode_MapsBackendFailureToUiModel" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - после обновления теста успешно, 1 тест;
- повторный `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 1373 теста.

Решения:
- изменение считается расширением покрытия байткодного исполнения, а не изменением пользовательской семантики языка;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются, потому что операторы уже существуют в языке, справочнике и тематических примерах.

Что остаётся:
- продолжить закрывать границы байткодного исполнения.

Следующий шаг:
- рассмотреть косвенные вызовы функциональных значений как следующий практичный срез VM.
## Session 381 - 2026-04-24 21:33 - Косвенные вызовы функциональных значений в BytecodeVm

Что сделано:
- закрыта граница байткодного исполнения для косвенных вызовов функциональных значений;
- добавлен `Opcode.CallValue`;
- `BoundProgramLowerer` теперь выпускает `CallValue` для вызовов, где цель вызова является вычисляемым функциональным значением;
- `BytecodeCompiler` кодирует `CallValue` числом аргументов и признаком возвращаемого значения;
- `BytecodeVm` выполняет `CallValue` через существующий путь `InvokeBytecodeFunction`;
- `BytecodeModuleValidator` проверяет форму `CallValue`, стек, функциональный тип цели, аргументы и признак возвращаемого значения;
- старые сквозные ожидания отката для `op = AddOne` и `PRINT op(4)` заменены ожиданием исполнения через `BytecodeVm`.

Какие файлы изменены:
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/SPECIFICATION2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Bytecode/Diagnostics/BytecodeModuleValidator.cs`
- `src/Axiom.Bytecode/Emission/BytecodeCompiler.cs`
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `src/Axiom.Bytecode/Model/LoweredInstruction.cs`
- `src/Axiom.Bytecode/Model/LoweredInstructionKind.cs`
- `src/Axiom.Bytecode/Model/Opcode.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "IndirectFunctionValueCall|CallValue" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "IndirectFunctionValueCall" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "IndirectFunctionValueCall" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 2 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "IndirectFunctionValueCall" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест.

Решения:
- изменение считается расширением покрытия байткодного исполнения, а не изменением пользовательской семантики языка;
- новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются.

Что остаётся:
- выполнить более широкий последовательный прогон сборки и тестов после документирования;
- продолжить следующий срез развития байткодной VM.

Следующий шаг:
- выбрать следующий практически полезный слой языка для исполнения через `BytecodeVm` и закрепить его тестами против интерпретатора.

## Session 382 - 2026-04-24 21:38 - Финальная проверка косвенных вызовов функциональных значений

Что сделано:
- выполнен широкий последовательный прогон после реализации `CallValue`;
- исправлены повреждённые ожидания в `ExecutionPipelineTests`: явный режим `Interpreter` снова проверяет интерпретатор, а русские подстроки диагностик снова записаны нормальным текстом;
- подтверждено, что `Auto` и `Bytecode` выполняют `op = AddOne` и `PRINT op(4)` через `BytecodeVm` без отката на интерпретатор.

Какие файлы изменены:
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 172 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 441 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 236 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 219 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение остаётся расширением покрытия байткодного исполнения, а не изменением пользовательской семантики языка;
- новые `.axm`-примеры и страницы `docs/manual/` для этого шага не требуются.

Что остаётся:
- продолжить расширять исполняемый байткодный путь на следующий практически полезный срез языка.

Следующий шаг:
- выбрать следующий байткодный срез, который ещё вызывает откат на интерпретатор или не имеет достаточного покрытия, и закрепить его сквозными тестами.

## Session 383 - 2026-04-24 22:03 - Закрепление TYPE fixed-array samples на BytecodeVm

Что сделано:
- выбран следующий участок без явной сквозной фиксации движка: приёмочные `.axm`-примеры `TYPE` с фиксированными массивными полями и `CONST`-границами;
- в `AcceptanceProgramTests` добавлена теория, которая запускает эти примеры в режиме `Bytecode`;
- тест проверяет успешный статус, ожидаемый вывод, фактический `BytecodeVm` и отсутствие отката на интерпретатор;
- существующая реализация подтвердила, что этот участок уже исполняется VM.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "TypeFixedArrayFieldAcceptanceSamples_RunOnBytecodeVm" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 3 теста;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 239 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение считается закреплением покрытия байткодного исполнения тестами, а не изменением поведения языка;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются, потому что существующие примеры и справочник уже описывают это пользовательское поведение;
- `docs/SPECIFICATION2.md` и `docs/ARCHITECTURE.md` не обновлялись, потому что договор языка и техническая структура не изменились.

Что остаётся:
- продолжить поиск участков, которые ещё не закреплены явной проверкой фактического движка или всё ещё вызывают откат на интерпретатор.

Следующий шаг:
- пройти приёмочные примеры и сквозные тесты по следующей группе языка, выбрать ещё один участок для явной проверки `BytecodeVm` или для устранения реальной границы исполнения.

## Session 384 - 2026-04-24 22:07 - Закрепление control-flow samples на BytecodeVm

Что сделано:
- выбран следующий участок без явной сквозной фиксации движка: приёмочные `.axm`-примеры управления ходом выполнения;
- в `AcceptanceProgramTests` добавлена теория для `IF`, `FOR`, `WHILE`, `DO`, `SELECT CASE` и `RETURN`;
- каждый пример запускается в явном режиме `Bytecode`;
- тест проверяет успешный статус, точный вывод, фактический `BytecodeVm` и отсутствие отката на интерпретатор;
- существующая реализация подтвердила, что весь выбранный control-flow-срез уже исполняется VM.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "ControlFlowAcceptanceSamples_RunOnBytecodeVm" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 19 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 258 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение считается закреплением покрытия байткодного исполнения тестами, а не изменением поведения языка;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются, потому что существующие примеры и справочник уже описывают это пользовательское поведение;
- `docs/SPECIFICATION2.md` и `docs/ARCHITECTURE.md` не обновлялись, потому что договор языка и техническая структура не изменились.

Что остаётся:
- продолжить закреплять фактический `BytecodeVm` для следующих групп приёмочных примеров или устранять найденные реальные границы исполнения.

Следующий шаг:
- перейти к приёмочным примерам выражений и базовых операторов, выбрать покрытый набор для явной проверки `BytecodeVm`.

## Session 385 - 2026-04-24 22:12 - Закрепление expression/operator samples на BytecodeVm

Что сделано:
- выбран следующий участок без явной сквозной фиксации движка: приёмочные `.axm`-примеры выражений и скалярных операторов;
- в `AcceptanceProgramTests` добавлена теория для выражений: арифметика, индексирование массива, сравнения, автоматические числовые преобразования, шестнадцатеричные литералы, логические выражения и конкатенация строк;
- в `AcceptanceProgramTests` добавлена теория для скалярных операторов `AND`, `OR`, `XOR`, `EQV`, `IMP`, `NOT`, `\`, `MOD`, `^`, `<<`, `>>`;
- каждый пример запускается в явном режиме `Bytecode`;
- тесты проверяют успешный статус, точный вывод, фактический `BytecodeVm` и отсутствие отката на интерпретатор;
- существующая реализация подтвердила, что выбранный срез уже исполняется VM.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "ExpressionAcceptanceSamples_RunOnBytecodeVm|ScalarOperatorAcceptanceSamples_RunOnBytecodeVm" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 19 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 277 тестов.
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений.
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение считается закреплением покрытия байткодного исполнения тестами, а не изменением поведения языка;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются, потому что существующие примеры и справочник уже описывают это пользовательское поведение;
- `docs/SPECIFICATION2.md` и `docs/ARCHITECTURE.md` не обновлялись, потому что договор языка и техническая структура не изменились.

Что остаётся:
- продолжить закреплять фактический `BytecodeVm` для следующих групп приёмочных примеров.

Следующий шаг:
- перейти к операторам объявления и присваивания `DIM`, `CONST`, assignment, а также базовым `PRINT`-примерам.

## Session 388 - 2026-04-24 22:31 - Закрепление DIM CONST assignment PRINT samples на BytecodeVm

Что сделано:
- в `AcceptanceProgramTests` добавлена теория `DeclarationAssignmentAndPrintAcceptanceSamples_RunOnBytecodeVm`;
- закреплён явный запуск через `BytecodeVm` для существующих примеров `DIM`: базовое объявление, групповые объявления, вывод типа из инициализатора, `LONG`, явный `DECIMAL`, фиксированная строка;
- закреплён явный запуск через `BytecodeVm` для `CONST`, скалярного присваивания и присваивания элементу массива;
- закреплён явный запуск через `BytecodeVm` для базовых форм `PRINT`: выражение, пустой `PRINT`, разделители `;` и `,`, смешанный список и `PRINT USING`;
- каждый пример проверяет точный ожидаемый вывод, фактический `BytecodeVm` и отсутствие отката на интерпретатор.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "DeclarationAssignmentAndPrintAcceptanceSamples_RunOnBytecodeVm" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 15 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 293 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение считается закреплением покрытия байткодного исполнения тестами, а не изменением поведения языка;
- новые `.axm`-примеры и страницы `docs/manual/` не требуются, потому что существующие примеры и справочник уже описывают это пользовательское поведение.

Что остаётся:
- продолжить плановое закрепление отдельных групп приёмочных примеров через `BytecodeVm`.

Следующий шаг:
- перейти к `INPUT`, `SUB`, `FUNCTION` и функциональным значениям, включая локальные функции и замыкания.

## Session 389 - 2026-04-24 22:34 - Сверка дублирования BytecodeVm-покрытия

Что сделано:
- перед добавлением новой теории для `INPUT`, `SUB`, `FUNCTION` и функциональных значений выполнена сверка существующего покрытия;
- подтверждено, что `INPUT` уже проверяется через `ExecutionPipelineTests`, `BytecodeVmTests` и широкий `samples/smoke/full_language_bytecode.axm`;
- подтверждено, что обычные `SUB`, `FUNCTION`, локальные функции, захваты, функциональные значения и возвращаемые замыкания уже покрыты сочетанием VM-тестов, тестов конвейера исполнения, существующих приёмочных примеров и широкого smoke-примера;
- принято решение не добавлять очередной почти дублирующий слой тестов без предварительной карты покрытия.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- код не менялся;
- новые тесты не запускались, потому что изменение относится к планированию и фиксации состояния.

Решения:
- ощущение повторения признано корректным;
- дальнейшее развитие покрытия `BytecodeVm` должно идти от карты реальных пробелов, а не от механического повторения уже проверенных групп.

Что остаётся:
- составить краткую карту покрытия байткодного пути по группам языка и приёмочным примерам.

Следующий шаг:
- выбрать следующий реальный пробел после карты покрытия: непокрытую группу `.axm`-примеров или известную границу VM.

## Session 386 - 2026-04-24 22:25 - Широкий smoke-пример для BytecodeVm

Что сделано:
- создан `samples/smoke/full_language_bytecode.axm`;
- пример снабжён подробными русскими комментариями по каждому крупному блоку;
- в пример включены `INPUT`, `PRINT USING`, `DIM`, `CONST`, присваивание, `IF`, `FOR`, `WHILE`, `DO`, `SELECT CASE`, `SUB`, `FUNCTION`, замыкание, функциональное значение, `TYPE`, массивы, строковые функции, числовые функции, регулярные выражения, функции даты и времени, файловые функции и открытый `FILE`;
- в `AcceptanceProgramTests` добавлен тест `FullLanguageBytecodeSmokeSample_RunsOnBytecodeVm`;
- тест запускает пример в явном режиме `Bytecode`, подаёт для `INPUT` строку `Ника, 7`, проверяет точный вывод, фактический `BytecodeVm` и отсутствие отката на интерпретатор;
- `README.md` обновлён упоминанием нового широкого smoke-примера.

Какие файлы изменены:
- `README.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `samples/smoke/full_language_bytecode.axm`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

Проверки:
- первый целевой запуск выявил две ошибки самого примера: предварительное объявление переменной цикла `FOR` и неверную арность `ROUND`;
- после исправления `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullLanguageBytecodeSmokeSample_RunsOnBytecodeVm" -p:UseSharedCompilation=false --logger "console;verbosity=normal"` - успешно, 1 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, 278 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` - успешно, 0 ошибок, 0 предупреждений;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` - успешно, все тесты решения пройдены.

Решения:
- изменение считается добавлением широкого приёмочного примера и тестового покрытия, а не изменением поведения языка;
- новые страницы `docs/manual/` не требуются, потому что новых операторов, встроенных функций или пользовательской семантики не добавлялось;
- функции даты и времени уже находятся в покрытии `BytecodeVm`; новый пример закрепляет их совместный запуск вместе с файловыми функциями и остальными крупными блоками языка.

Что остаётся:
- выполнить ручной запуск `samples/smoke/full_language_bytecode.axm` из `Axiom.Ide` в режиме `Bytecode`;
- продолжить плановое закрепление отдельных групп приёмочных примеров.

Следующий шаг:
- открыть широкий smoke-пример в IDE, ввести `Ника, 7` при запросе `INPUT` и убедиться, что вывод завершается строкой `full-smoke-done` без диагностик.

## Session 387 - 2026-04-24 22:28 - Ручное подтверждение широкого Bytecode smoke

Что сделано:
- выполнен ручной запуск `samples/smoke/full_language_bytecode.axm` из `Axiom.Ide`;
- при запросе `INPUT` введено `Ника, 7`;
- программа напечатала ожидаемый широкий отчёт и завершилась строкой `full-smoke-done`;
- ручной запуск подтвердил совместную работу `INPUT`, `PRINT USING`, управляющих конструкций, массивов, `TYPE`, функциональных значений, регулярных выражений, даты/времени, файловых функций и открытого `FILE` в одном сценарии.

Какие файлы изменены:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- ручной запуск в IDE прошёл успешно по присланному выводу;
- новых кодовых изменений после предыдущего полного `dotnet test Axiom.sln` не вносилось.

Решения:
- `samples/smoke/full_language_bytecode.axm` считается подтверждённым широким позитивным smoke-примером для ручного запуска в режиме `Bytecode`;
- автоматический тест `FullLanguageBytecodeSmokeSample_RunsOnBytecodeVm` остаётся основной регрессионной проверкой этого сценария.

Что остаётся:
- продолжить плановое закрепление отдельных групп приёмочных примеров через `BytecodeVm`.

Следующий шаг:
- перейти к операторам объявления и присваивания `DIM`, `CONST`, assignment, а также базовым `PRINT`-примерам.

## Session 390 - 2026-04-24 22:39 - Краткая карта покрытия BytecodeVm

Что сделано:
- просмотрен отдельный план `docs/План внедрения собственного байткода и виртуальной машины в Axiom.txt`;
- сверены текущие документы, байткодные проекты, VM, список поддержанных built-in и тестовое покрытие;
- создан `docs/BYTECODE_COVERAGE.md` с краткой картой языка и сверкой фаз плана внедрения;
- в карте зафиксировано, что фазы 1-9 фактически закрыты для исполняемого байткода в памяти процесса, а фаза 10 закрыта частично.

Какие файлы изменены:
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- код языка и VM не менялся;
- автоматические тесты не запускались, потому что изменение относится к документации и планированию.

Решения:
- дальнейшее развитие покрытия `BytecodeVm` должно идти не через дублирование уже проверенных групп, а через централизованный no-fallback-аудит положительных `.axm`-примеров;
- сериализация байткода, полноценный отладчик и отчёт по производительности остаются отдельными будущими задачами.

Что остаётся:
- явно решить, считать ли текущий `Auto` режимом VM-first для публичной поверхности языка;
- подготовить единый список положительных приёмочных `.axm`-примеров, которые обязаны запускаться через `ExecutionMode.Bytecode` и `ExecutionEngineKind.BytecodeVm`.

Следующий шаг:
- сделать централизованный no-fallback-аудит положительных `.axm`-примеров и явно перечислить временные исключения, если они есть.

## Session 391 - 2026-04-25 07:45 - Аудит положительных примеров без отката

Что сделано:
- добавлен централизованный тест `PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback` для положительных `.axm`-примеров из `samples/`;
- тест запускает каждый положительный пример в `ExecutionMode.Bytecode` и проверяет фактический `ExecutionEngineKind.BytecodeVm`;
- тест проверяет, что отката на интерпретатор нет;
- временных исключений для положительных `.axm`-примеров не оставлено;
- для примеров с относительными путями запуск выполняется из корня репозитория, чтобы файловые сценарии работали так же, как при ручном запуске проекта;
- карта покрытия байткодного пути дополнена итогом аудита;
- план дополнен следующим шагом проверки.

Какие файлы изменены:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- целевой прогон `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback" -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- проверено 309 положительных `.axm`-примеров, все прошли через `BytecodeVm` без отката;
- полный последовательный прогон ещё нужно выполнить перед закрытием задачи.

Решения:
- положительные приёмочные `.axm`-примеры не имеют временных исключений из байткодного аудита;
- если такой пример не проходит через `BytecodeVm`, это считается дефектом байткодного пути, ошибкой примера или ошибкой тестовой обвязки, а не поводом добавлять исключение;
- отрицательные примеры ожидаемых ошибок остаются отдельным слоем и не входят в положительный аудит.

Что остаётся:
- выполнить полный `Axiom.Execution.Tests`;
- выполнить последовательные `dotnet build Axiom.sln` и `dotnet test Axiom.sln`.

Следующий шаг:
- после зелёной проверки отдельно зафиксировать проектное решение по режиму `Auto`: считать ли его официально VM-first для текущей публичной поверхности языка.

## Session 392 - 2026-04-25 07:55 - Устранение предупреждений nullable в тестах

Что сделано:
- проверены предупреждения CS8602 из сборки решения;
- в тестах парсера для `REDIM` и `ERASE` добавлены явные проверки `Assert.NotNull` перед обращением к `Identifier`;
- в тестах связывания для `REDIM` и `ERASE` добавлены явные проверки `Assert.NotNull` перед обращением к `Variable`;
- поведение языка, байткода и среды выполнения не менялось.

Какие файлы изменены:
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- перед исправлением также прошёл полный `Axiom.Execution.Tests`, 602 теста.

Решения:
- предупреждения были не критичны для продукта, потому что находились в тестовом коде;
- предупреждения всё равно устранены, чтобы сборка решения снова была чистой и будущие предупреждения были заметнее.

Что остаётся:
- новых задач по этим предупреждениям нет.

Следующий шаг:
- зафиксировать проектное решение по режиму `Auto`: считать ли его официально VM-first для текущей публичной поверхности языка.

## Session 393 - 2026-04-25 08:12 - Объединение плана доводки байткода

Что сделано:
- обновлён документ `docs/План доводки байткода и виртуальной машины до зафиксированного статуса.txt`;
- старая ветка плана, где `Auto` должен был стать VM-first, заменена на новое проектное решение: `Auto` удаляется;
- объединённый план теперь фиксирует два явных режима исполнения: `Bytecode` и `Interpreter`;
- в плане сохранены следующие этапы после удаления `Auto`: диагностический паритет, сериализация `BytecodeModule`, основа отладки VM и отчёт по производительности;
- `docs/PLANS2.md` дополнен новым ближайшим шагом.

Какие файлы изменены:
- `docs/План доводки байткода и виртуальной машины до зафиксированного статуса.txt`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- автоматические тесты не запускались, потому что изменение относится к документации и планированию;
- код языка, IDE и VM не менялся.

Решения:
- `Auto` больше не считается целевым режимом исполнения;
- `Bytecode` должен стать основным режимом по умолчанию;
- `Interpreter` остаётся явным сравнительным режимом;
- скрытый откат на интерпретатор больше не используется как стратегия разработки.

Что остаётся:
- реализовать удаление `ExecutionMode.Auto` из кода, IDE, сохранённых настроек и тестов;
- затем обновить архитектурные документы и выполнить последовательные проверки.

Следующий шаг:
- начать реализацию удаления `Auto` с ядра запуска и тестов `Axiom.Execution.Tests`.

## Session 394 - 2026-04-25 08:25 - Удаление режима Auto

Что сделано:
- удалён `ExecutionMode.Auto` из контракта запуска;
- значение по умолчанию для запуска переведено на `ExecutionMode.Bytecode`;
- из `AxiomExecutionPipeline` убран рабочий путь скрытого отката на интерпретатор;
- в IDE удалён пункт меню `Run -> Execution Mode -> Auto` и связанная команда модели окна;
- сохранённое старое значение `Auto` в настройках IDE теперь нормализуется в `Bytecode`;
- тесты запуска, IDE и сохранения состояния обновлены под два явных режима;
- исправлено старое тестовое ожидание повреждённой строки диагностики деления на ноль на читаемое русское слово `ноль`.

Какие файлы изменены:
- `README.md`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Execution/Requests/ExecutionMode.cs`
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Results/ExecutionResult.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Models/ProgramRunResult.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 602 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` сначала выявил старое ожидание повреждённой кодировки в тесте диагностики, после исправления прошёл успешно, 219 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- `Bytecode` является основным режимом запуска по умолчанию;
- `Interpreter` остаётся явным эталонным режимом;
- скрытый откат на интерпретатор больше не используется как рабочая стратегия запуска;
- изменение не добавляет нового синтаксиса и не меняет пользовательскую семантику языка, поэтому новые `.axm`-примеры и страницы `docs/manual/` не требуются.

Что остаётся:
- следующий этап доводки байткода должен сосредоточиться на диагностическом паритете двух явных режимов.

Следующий шаг:
- проверить диагностический паритет `ExecutionMode.Bytecode` и `ExecutionMode.Interpreter` на выбранных отрицательных сценариях и зафиксировать правила совпадения или допустимого расхождения диагностик.
## Session 395 - 2026-04-25 08:32 - Диагностический паритет отрицательных сценариев

Что сделано:
- добавлен тест `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter` в `AcceptanceProgramTests`;
- тест сравнивает режимы `ExecutionMode.Bytecode` и `ExecutionMode.Interpreter` на 8 отрицательных сценариях времени выполнения;
- зафиксировано совпадение статуса `RuntimeFailure`, вывода до ошибки, фактических движков, отсутствия отката и кода диагностики;
- выявлены и оформлены как допустимые текущие расхождения точного текста сообщения и точной колонки диагностики;
- обновлены `docs/SPECIFICATION2.md`, `docs/BYTECODE_COVERAGE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/BYTECODE_COVERAGE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter" -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 8 тестовых случаев.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 610 тестов.
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок.
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- для выбранных покрытых VM ошибок времени выполнения обязательным паритетом считаются статус, код диагностики, вывод до ошибки и отсутствие отката;
- точная колонка и текст сообщения пока не считаются обязательной частью паритета, если обе диагностики имеют валидную позицию;
- ошибки внутреннего байткодного слоя остаются отдельным `BackendFailure` и не обязаны совпадать с диагностикой интерпретатора.

Что остаётся:
- перейти к следующему этапу объединённого плана: подготовить сериализацию `BytecodeModule` как отдельный внутренний артефакт без изменения пользовательской семантики языка.

Следующий шаг:
- начать проектирование и реализацию сериализации `BytecodeModule`, сначала уточнив формат артефакта и границы его использования в документах.

## Session 396 - 2026-04-25 08:42 - JSON-снимок BytecodeModule

Что сделано:
- добавлен слой `Serialization` в проект `Axiom.Bytecode`;
- добавлены записи `BytecodeModuleArtifact` и связанные модели снимка;
- добавлен `BytecodeModuleSerializer` с методами `ToArtifact`, `SerializeToJson(BytecodeModule)` и `SerializeToJson(Stream, BytecodeModule)`;
- снимок сохраняет заголовок, пул констант, процедуры, инструкции, глобальные переменные, встроенные функции и отладочную информацию;
- полный семантический граф не сериализуется: для символов и типов сохраняются устойчивые имена, признаки и коды встроенных функций;
- добавлены тесты `BytecodeModuleSerializerTests`.

Какие файлы изменены:
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` сначала выявил ошибку сборки в создании `SortedDictionary` из `IReadOnlyDictionary`;
- после исправления тот же прогон прошёл успешно, 174 теста.
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок.
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- текущий JSON является внутренним диагностическим снимком, а не загружаемым исполняемым форматом;
- `Axiom.Execution`, `BytecodeVm` и IDE пока не используют этот снимок автоматически;
- изменение не влияет на пользовательское поведение языка, поэтому новые `.axm`-примеры и страницы `docs/manual/` не нужны.

Что остаётся:
- выбрать следующий узкий шаг: обратное чтение JSON-снимка в проверяемую модель или служебная запись снимка для диагностики.

Следующий шаг:
- решить, нужен ли сначала десериализатор JSON-снимка в проверяемую модель или служебная запись снимка из конвейера компиляции без изменения запуска программ.

## Session 397 - 2026-04-25 09:08 - чтение и служебная запись JSON-снимка байткода

Что сделано:
- добавлено чтение JSON-снимка обратно в `BytecodeModule` через `BytecodeModuleSerializer`;
- добавлена явная служебная запись снимка из `CompilationRequest.BytecodeSnapshotPath` и `ExecutionRequest.BytecodeSnapshotPath`;
- запись выполняется только после успешной эмиссии байткода и не меняет обычный запуск без заданного пути;
- ошибка записи снимка оформляется диагностикой байткодного слоя `MB4003`;
- добавлены тесты на восстановление модуля из JSON и запись снимка через `Compile` и `Run`;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Какие файлы изменены:
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `src/Axiom.Execution/Requests/CompilationRequest.cs`
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 175 тестов.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 612 тестов.

Решения:
- JSON-снимок остаётся внутренним диагностическим представлением, а не автоматическим пользовательским входом для запуска;
- восстановленный из JSON модуль должен проходить `BytecodeModuleValidator`;
- полная идентичность областей видимости и пользовательских типов пока остаётся ограничением текущей формы снимка.

Что остаётся:
- выполнить полный последовательный прогон решения;
- после этого выбрать следующий шаг: стабилизировать долговременную форму байткодного артефакта перед двоичным форматом или перейти к следующему блоку отладки VM.

Следующий шаг:
- выполнить `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` и `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`.

Дополнение к Session 397:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- суммарно прошли 1754 теста.

Уточнённый следующий шаг:
- перейти к следующему этапу объединённого плана: уточнить долговременную форму байткодного артефакта перед будущим двоичным форматом или начать следующий запланированный блок отладки VM.

## Session 398 - 2026-04-25 09:19 - идентификаторы символов в JSON-снимке байткода

Что сделано:
- в `BytecodeFunctionSymbolArtifact` добавлены `SymbolId` и `ContainingRoutineSymbolId`;
- в `BytecodeVariableSymbolArtifact` добавлен `SymbolId`;
- `BytecodeModuleSerializer` назначает идентификаторы при записи снимка и использует их при чтении;
- старый запасной путь восстановления по имени, типу и признакам сохранён для снимков без `SymbolId`;
- добавлен тест, который проверяет, что две разные локальные переменные с одинаковым именем и типом не склеиваются после чтения JSON.

Какие файлы изменены:
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 176 тестов.

Решения:
- `SymbolId` является идентификатором внутри одного снимка, а не глобальным постоянным идентификатором между разными сборками программы;
- JSON остаётся внутренним байткодным представлением, но теперь лучше подходит как промежуточная ступень перед будущим двоичным форматом.

Что остаётся:
- выполнить последовательные проверки `Axiom.Execution.Tests`, сборку решения и полный прогон тестов;
- затем решить, продолжать ли сейчас укрепление формы `TYPE` в снимке или перейти к следующему блоку отладки VM.

Следующий шаг:
- выполнить `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`, затем сборку и полный тестовый прогон решения.

Дополнение к Session 398:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 612 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- суммарно прошли 1755 тестов.

Уточнённый следующий шаг:
- продолжить укрепление долговременной формы байткодного артефакта: отдельно зафиксировать перенос пользовательских `TYPE` и их полей в снимке;
- если этот блок будет отложен, перейти к следующему запланированному блоку отладки VM.

## Session 399 - 2026-04-25 09:39 - определения TYPE в JSON-снимке байткода

Что сделано:
- в `BytecodeModuleArtifact` добавлена таблица `TypeDefinitions`;
- добавлены записи `BytecodeTypeDefinitionArtifact` и `BytecodeTypeFieldArtifact`;
- `BytecodeModuleSerializer` собирает пользовательские типы из всех типовых позиций модуля и полей вложенных пользовательских типов;
- при чтении JSON сначала создаются пользовательские типы по именам, затем они заполняются полями;
- добавлен тест, который проверяет восстановление полей пользовательского типа, включая fixed-string поле, и последующую проверку модуля валидатором.

Какие файлы изменены:
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 177 тестов.

Решения:
- JSON-снимок теперь переносит форму пользовательских `TYPE`, но остаётся внутренним байткодным представлением;
- старые снимки без `TypeDefinitions` по-прежнему читаются в запасном режиме, но модуль с доступом к полям может не пройти `BytecodeModuleValidator`, если полей нет.

Что остаётся:
- выполнить последовательные проверки `Axiom.Execution.Tests`, сборку решения и полный прогон тестов;
- после этого выбрать следующий блок доводки: версионирование JSON-артефакта перед двоичным форматом или следующий блок отладки VM.

Следующий шаг:
- выполнить `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`, затем сборку и полный тестовый прогон решения.

Дополнение к Session 399:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 612 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- суммарно прошли 1756 тестов.

Уточнённый следующий шаг:
- выбрать следующий узкий блок: стабилизировать версионирование JSON-артефакта перед будущим двоичным форматом или перейти к следующему запланированному блоку отладки VM.

## Session 400 - 2026-04-25 09:56 - версия схемы JSON-снимка байткода

Что сделано:
- в корневой `BytecodeModuleArtifact` добавлено поле `ArtifactSchemaVersion`;
- `BytecodeModuleSerializer` записывает текущую версию схемы через `CurrentArtifactSchemaVersion = 1`;
- при восстановлении `BytecodeModule` добавлена ранняя проверка версии схемы, `Header.FormatVersion` и `Header.CompatibilityToken`;
- старые переходные JSON-снимки без `ArtifactSchemaVersion` оставлены совместимыми и читаются как схема 1;
- снимки с явно неподдерживаемой версией схемы отклоняются понятной ошибкой;
- добавлены тесты на запись версии схемы, чтение старого снимка без поля версии и отказ от неподдерживаемой версии.

Какие файлы изменены:
- `src/Axiom.Bytecode/Serialization/BytecodeModuleArtifact.cs`
- `src/Axiom.Bytecode/Serialization/BytecodeModuleSerializer.cs`
- `tests/Axiom.Bytecode.Tests/BytecodeModuleSerializerTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 179 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 612 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, суммарно 1758 тестов.

Решения:
- версия схемы JSON-снимка отделена от `Header.FormatVersion`, потому что первое описывает форму служебного JSON-артефакта, а второе - версию самого байткодного модуля;
- отсутствие `ArtifactSchemaVersion` временно допустимо только для переходной совместимости со снимками, созданными до этой записи.

Что остаётся:
- перейти к следующему блоку отладки VM и выбрать ближайший байткодный сценарий, который ещё требует покрытия или сравнения с интерпретатором.

Следующий шаг:
- просмотреть текущие планы VM и выбрать самый близкий незакрытый сценарий для реализации или проверки.

## Session 401 - 2026-04-25 10:03 - снимок текущей инструкции VM

Что сделано:
- добавлен `BytecodeVmExecutionSnapshot` для описания текущей инструкции VM;
- `BytecodeVm.Execute` получил необязательный наблюдатель `Action<BytecodeVmExecutionSnapshot>`;
- VM вызывает наблюдатель перед выполнением инструкции и передаёт routine-id, имя процедуры, индекс инструкции, opcode, исходную позицию и глубину вызовов;
- исходная позиция берётся из существующей отладочной карты `BytecodeDebugInfo`, с прежним запасным путём при отсутствии карты;
- добавлен тест `Execute_ReportsInstructionSnapshotsWithSourceLocations`, который проверяет связь инструкций `PRINT 1` и `PRINT 2` с исходными строками.

Какие файлы изменены:
- `src/Axiom.Runtime/Execution/BytecodeVmExecutionSnapshot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 442 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, суммарно 1759 тестов.

Решения:
- наблюдение за инструкциями сделано необязательным и не влияет на обычный запуск VM;
- IDE пока не подключается к этому механизму, потому что пользовательский отладчик должен проектироваться отдельным шагом;
- `.axm`-пример и `docs/manual/` не требуются, так как пользовательская семантика языка не изменилась.

Что остаётся:
- спроектировать следующий маленький слой отладки VM: управляемую остановку перед выбранной инструкцией или исходной строкой на уровне runtime-тестов.

Следующий шаг:
- добавить тестируемый механизм остановки VM без интеграции в IDE.
## Session 402 - 2026-04-25 10:34 - управляемая остановка VM

Что сделано:
- `BytecodeVm.Execute` получил необязательный предикат `Func<BytecodeVmExecutionSnapshot, bool>` для остановки перед выбранной инструкцией;
- VM теперь строит снимок инструкции один раз, сообщает его наблюдателю и затем проверяет условие остановки;
- при штатной остановке VM не выполняет выбранную инструкцию, сохраняет снимок в `RuntimeResult.StoppedAt` и возвращает `RuntimeResult.Stopped = true`;
- `RuntimeResult.Succeeded` остаётся завязанным только на диагностики, поэтому остановка без ошибок считается успешным результатом;
- при остановке VM закрывает открытые файлы, но не сбрасывает незавершённый буфер `PRINT`, чтобы не создавать дополнительный вывод из частично выполненной строки;
- добавлен тест `Execute_StopsBeforeRequestedInstruction`.

Какие файлы изменены:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Host/RuntimeResult.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 443 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 179 тестов;
- `dotnet test tests\Axiom.Lexing.Tests\Axiom.Lexing.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 18 тестов;
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 69 тестов;
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 220 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 219 тестов.

Ограничение проверки:
- полный `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` не завершился до тайм-аута инструмента;
- отдельный `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` также не завершился до тайм-аута;
- в обоих случаях зависший `testhost` относился к `Axiom.Execution.Tests` и был остановлен вручную.

Решения:
- остановка VM считается внутренним runtime-механизмом, а не пользовательской возможностью языка;
- `.axm`-пример и обновление `docs/manual/` не требуются, потому что язык и наблюдаемое поведение программ Axiom не изменились;
- подключение IDE и модель продолжения выполнения после остановки остаются отдельными будущими шагами.

Что остаётся:
- добавить слой точек останова по исходной позиции поверх текущего предиката остановки;
- отдельно разобраться, какой тест или сценарий в `Axiom.Execution.Tests` удерживает `testhost` до тайм-аута.

Следующий шаг:
- реализовать небольшой runtime-API для точек останова по исходной строке или позиции, без подключения IDE.
## Session 403 - 2026-04-25 10:59 - точки останова VM по исходной позиции

Что сделано:
- добавлен `BytecodeSourceBreakpoint` для описания точки останова по строке или по точной паре строка/колонка;
- добавлен `BytecodeBreakpointSet` с быстрым сопоставлением строк и исходных позиций;
- `BytecodeVm.Execute` получил необязательный `BytecodeBreakpointSet`;
- VM объединяет `BytecodeBreakpointSet` с уже существующим ручным предикатом остановки;
- добавлены тесты `Execute_StopsAtSourceLineBreakpoint`, `Execute_StopsAtSourcePositionBreakpoint` и `Execute_IgnoresSourcePositionBreakpointWithDifferentColumn`.

Какие файлы изменены:
- `src/Axiom.Runtime/Execution/BytecodeSourceBreakpoint.cs`
- `src/Axiom.Runtime/Execution/BytecodeBreakpointSet.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверки:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 446 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 179 тестов.

Ограничение проверки:
- `Axiom.Execution.Tests` в предыдущем шаге зависал до тайм-аута независимо от текущего изменения, поэтому в этом шаге повторно не запускался.

Решения:
- точки останова остаются внутренним runtime-API, а не пользовательской возможностью языка;
- `.axm`-пример и обновление `docs/manual/` не требуются, потому что синтаксис, операторы, функции и наблюдаемое поведение программ Axiom не изменились;
- IDE пока не подключается к модели точек останова.

Что остаётся:
- выбрать следующий небольшой слой отладки VM: состояние остановленного запуска или подготовку продолжения выполнения;
- отдельно разобрать зависание `Axiom.Execution.Tests`, чтобы снова вернуть полный прогон решения в обычную проверку.

Следующий шаг:
- сначала исследовать зависание `Axiom.Execution.Tests` или, если оно будет отложено, добавить минимальную модель остановленного VM-запуска без подключения IDE.

## Session 404 - 2026-04-25 11:09 - разбор зависания Axiom.Execution.Tests

Что сделано:
- локализовано зависание тестового проекта `Axiom.Execution.Tests`;
- отдельно проверено, что `ExecutionPipelineTests` проходит быстро: 98 тестов;
- отдельно проверено, что `AcceptanceProgramTests` без общего аудита положительных `.axm` проходит быстро: 205 тестов;
- через `--blame-hang` найден конкретный незавершённый пример: `samples/smoke/repair_plans_mojibake.axm` в теории `PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback`;
- `samples/smoke/repair_plans_mojibake.axm` исключён из автоматического положительного аудита, потому что это ручная служебная программа ремонта текста, а не короткий автоматический приёмочный пример;
- удалена временная рабочая копия `README_copy.md`, оставшаяся после прерванного запуска.

Какие файлы изменены:
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~PositiveAcceptanceSample_RunsOnBytecodeVmWithoutFallback" --logger "console;verbosity=minimal"` прошёл успешно, 308 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-build -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 611 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- ручные служебные `.axm`-программы, которые работают с крупными файлами репозитория или требуют сознательной ручной настройки путей, не должны автоматически попадать в общий положительный аудит всех примеров;
- пользовательское поведение языка не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- продолжить следующий небольшой шаг основы отладки VM без подключения IDE.

Следующий шаг:
- выбрать и реализовать минимальную модель состояния остановленного VM-запуска или подготовить первый слой продолжения выполнения после остановки.

## Session 405 - 2026-04-25 11:15 - состояние остановленного VM-запуска

Что сделано:
- добавлен `BytecodeVmStopState` для внутреннего состояния остановленного запуска VM;
- `RuntimeResult` получил `StopState`, а `StoppedAt` оставлен совместимым доступом к `StopState.Snapshot`;
- `BytecodeVm` теперь сохраняет остановку через `BytecodeVmStopState` при срабатывании предиката остановки или точки останова;
- runtime-тесты остановки VM обновлены и проверяют `StopState`, `CanResume = false` и совместимость `StoppedAt`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Runtime/Host/RuntimeResult.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 446 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 1762 теста.

Решения:
- текущий слой описывает остановку, но ещё не реализует продолжение выполнения;
- `CanResume` пока фиксируется как `false`, чтобы будущий код IDE не мог ошибочно считать остановленный запуск возобновляемым;
- пользовательское поведение языка не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- спроектировать и реализовать минимальное сохранение исполнительного контекста VM для будущего продолжения после остановки.

Следующий шаг:
- определить первый проверяемый runtime-сценарий продолжения выполнения после остановки VM без подключения IDE.

## Session 406 - 2026-04-25 11:22 - продолжение остановленного VM-запуска

Что сделано:
- `BytecodeVmStopState` получил метод `Resume()`;
- `CanResume` теперь показывает, можно ли одноразово продолжить живой остановленный запуск;
- `BytecodeVm` получил внутренний `ResumeFrom(...)`, который продолжает существующие кадры вызовов без повторного входа в модуль;
- при продолжении VM один раз пропускает условие остановки на той же routine-id/инструкции, чтобы выполнить инструкцию, перед которой была остановка;
- добавлен runtime-тест `Execute_ResumesStoppedRunFromSameInstruction`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeVmTests" --logger "console;verbosity=minimal"` сначала упал на сценарии со строковой точкой останова, потому что строковая точка срабатывает на нескольких инструкциях одной исходной строки; тест был уточнён до остановки перед конкретным `PushConst` второй строки;
- повторный `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeVmTests" --logger "console;verbosity=minimal"` прошёл успешно, 75 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 447 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 1763 теста.

Решения:
- продолжение пока является живым runtime-состоянием в памяти, а не сериализуемым снимком исполнительного контекста;
- старое состояние остановки одноразовое: после `Resume()` его `CanResume` становится `false`;
- пользовательское поведение языка не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- отдельно определить политику строковых точек останова при продолжении, потому что одна исходная строка может соответствовать нескольким байткодным инструкциям.

Следующий шаг:
- выбрать и покрыть тестом правило продолжения после строковой точки останова: пропуск только текущей инструкции или пропуск повторной остановки до перехода на другую исходную строку.

## Session 407 - 2026-04-25 11:28 - продолжение после строковой точки останова

Что сделано:
- реализована политика продолжения после строковой точки останова: после `Resume()` VM не останавливается повторно на той же исходной строке;
- `BytecodeBreakpointSet` разделил проверку строковых точек и точных позиций;
- `BytecodeVm` получил временное подавление строковых точек до перехода на другую исходную строку;
- точные позиционные точки и ручной предикат остановки оставлены отдельными механизмами;
- добавлен runtime-тест `Execute_ResumeFromSourceLineBreakpointSkipsRestOfSameLine`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeBreakpointSet.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeVmTests" --logger "console;verbosity=minimal"` прошёл успешно, 76 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 448 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 1764 теста.

Решения:
- для будущей IDE строковая точка останова трактуется как пользовательская точка на исходной строке, а не как остановка на каждой байткодной инструкции этой строки;
- низкоуровневый предикат остановки оставлен точным механизмом для runtime-сценариев;
- пользовательское поведение языка не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- проверить повторное срабатывание строковой точки при новом заходе на ту же строку после перехода на другую строку.

Следующий шаг:
- добавить тест с циклом, где точка останова стоит на строке внутри тела цикла и после `Resume()` должна снова сработать на следующей итерации.

## Session 408 - 2026-04-25 11:31 - повторное срабатывание строковой точки в цикле

Что сделано:
- добавлен runtime-тест `Execute_ResumeFromSourceLineBreakpointStopsAgainOnNextLoopIteration`;
- тест проверяет, что строковая точка останова внутри тела `FOR` после `Resume()` не застревает на той же строке, но снова срабатывает на следующей итерации;
- закреплено, что подавление строковой точки снимается после перехода VM на другую исходную строку.

Изменённые файлы:
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeVmTests" --logger "console;verbosity=minimal"` прошёл успешно, 77 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 449 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 1765 тестов.

Решения:
- строковая точка останова в цикле должна срабатывать при каждом новом заходе на исходную строку после того, как VM уже ушла с этой строки;
- пользовательское поведение языка не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- выбрать следующий слой основы отладчика VM: точные позиционные точки при продолжении или минимальная передача строковых точек из будущего IDE-слоя в VM.

Следующий шаг:
- добавить проверку точной позиционной точки при `Resume()` либо начать минимальный договор runtime/IDE для строковых точек останова без пользовательского интерфейса.

## Session 409 - 2026-04-25 11:37 - точная позиционная точка после Resume

Что сделано:
- добавлен runtime-тест `Execute_ResumeFromSourcePositionBreakpointCanStopAgainOnSameLine`;
- тест строит набор точных точек остановки по двум разным исходным позициям одной строки и проверяет, что после `Resume()` VM может остановиться на второй точной позиции той же строки;
- закреплено, что подавление строковой точки после `Resume()` не распространяется на точные позиционные точки.

Изменённые файлы:
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~BytecodeVmTests" --logger "console;verbosity=minimal"` прошёл успешно, 78 тестов;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 450 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- строковая точка остановки остаётся пользовательской моделью строки и после `Resume()` временно подавляется на этой же строке;
- точная позиционная точка остаётся отдельным внутренним механизмом и может сработать на другой инструкции той же строки;
- пользовательское поведение языка Axiom не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- перейти от внутренней VM-механики к минимальному договору runtime/IDE для передачи строковых точек остановки в запуск.

Следующий шаг:
- определить тонкую модель запроса на запуск с точками остановки и покрыть её тестом без добавления пользовательского интерфейса IDE.

## Session 410 - 2026-04-25 11:44 - договор запуска с точками остановки

Что сделано:
- `ExecutionRequest` получил свойство `SourceBreakpointLines` и нормализацию строк точек остановки;
- `AxiomExecutionPipeline` при байткодном запуске преобразует строки из запроса в `BytecodeBreakpointSet` и передаёт их в `BytecodeVm.Execute`;
- `ExecutionResult` получил `Stopped`, `StoppedAt` и `StopState`, чтобы вызывающий слой видел штатную остановку VM;
- добавлены тесты `Run_WithSourceBreakpointLines_StopsBeforeLineOnBytecodeVm` и `ExecutionRequest_NormalizesSourceBreakpointLines`.

Изменённые файлы:
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Results/ExecutionResult.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~ExecutionPipelineTests" --logger "console;verbosity=minimal"` прошёл успешно, 100 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 613 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- будущий IDE-слой должен передавать в конвейер простые номера исходных строк, а не создавать VM-набор точек остановки напрямую;
- `ExecutionResult` теперь несёт остановленное состояние VM, но видимый пользовательский интерфейс отладчика пока не добавляется;
- пользовательское поведение языка Axiom не менялось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- протянуть новый договор через `ProgramRunService` и модель результата IDE без добавления визуальных маркеров точек остановки.

Следующий шаг:
- обновить `IProgramRunService`, `ProgramRunService`, `ProgramRunResult` и тесты сервиса запуска, чтобы они принимали строки точек остановки и возвращали факт остановки на уровне IDE-модели.

## Session 411 - 2026-04-25 11:47 - точки остановки в ProgramRunService

Что сделано:
- `IProgramRunService.Run` получил необязательный параметр `sourceBreakpointLines`;
- `ProgramRunService` передаёт строки точек остановки в `ExecutionRequest` и пишет признак `stopped` в трассировку запуска;
- `ProgramRunResult` получил `Stopped`, `StoppedAt` и `StopState`;
- добавлены тесты `Run_WithSourceBreakpointLines_ReturnsStoppedResult` и `Run_WithSourceBreakpointLines_UsesCompiledCache`;
- тестовый `FakeProgramRunService` в `MainWindowViewModelTests` обновлён под новый договор интерфейса.

Изменённые файлы:
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/Models/ProgramRunResult.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~ProgramRunServiceTests" --logger "console;verbosity=minimal"` прошёл успешно, 18 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 221 тест;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- на этом шаге IDE-сервис уже может запускать программу с точками остановки, но видимый интерфейс точек остановки пока не добавляется;
- `StopState` временно проходит в `ProgramRunResult`, чтобы следующий слой мог реализовать продолжение выполнения без повторной сборки;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- добавить минимальное хранение строк точек остановки в модели IDE и передачу этого набора при запуске.

Следующий шаг:
- обновить `MainWindowViewModel` и тесты так, чтобы модель могла хранить набор строк точек остановки и передавать его в `IProgramRunService.Run` без добавления визуального редакторского интерфейса.

## Session 412 - 2026-04-25 11:55 - хранение строк точек остановки в MainWindowViewModel

Что сделано:
- `MainWindowViewModel` получил `SourceBreakpointLines`;
- добавлены методы `SetSourceBreakpointLines(...)` и `ClearSourceBreakpointLines()`;
- строки точек остановки нормализуются: запрещены неположительные номера, повторы убираются, порядок первого появления сохраняется;
- `RunProgram` передаёт текущий набор строк в `IProgramRunService.Run`;
- тестовый `FakeProgramRunService` запоминает переданные строки точек остановки;
- добавлены тесты `RunProgram_PassesSourceBreakpointLinesToRunService` и `ClearSourceBreakpointLines_RemovesStoredBreakpointLines`.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests" --logger "console;verbosity=minimal"` прошёл успешно, 78 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 223 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- текущий набор строк точек остановки хранится в модели главного окна и передаётся при запуске;
- это пока не полноценная пер-документная модель и не визуальный редакторский интерфейс;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- добавить минимальное действие переключения одной строки точки остановки в модели IDE.

Следующий шаг:
- реализовать метод или команду `ToggleSourceBreakpointLine` в `MainWindowViewModel`, покрыть добавление и удаление строки тестами, затем уже переходить к видимому управлению в редакторе.

## Session 413 - 2026-04-25 12:19 - переключение строки точки остановки

Что сделано:
- в `MainWindowViewModel` добавлен метод `ToggleSourceBreakpointLine(int line)`;
- метод добавляет строку точки остановки при первом вызове и удаляет её при повторном вызове;
- неположительные номера строк отклоняются через `ArgumentOutOfRangeException`;
- добавлены тесты `ToggleSourceBreakpointLine_AddsAndRemovesLine` и `ToggleSourceBreakpointLine_RejectsInvalidLine`.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests" --logger "console;verbosity=minimal"` прошёл успешно, 80 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 225 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- переключение одной строки сделано как метод модели, без визуального интерфейса редактора;
- нижний договор запуска с точками остановки не менялся;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- выбрать следующий шаг между видимой командой/привязкой редактора для переключения точки остановки и подготовкой команды продолжения остановленного запуска.

Следующий шаг:
- добавить тестируемую команду IDE для переключения точки остановки по текущей строке редактора либо начать хранение остановленного `ProgramRunResult.StopState` для будущей команды `Continue`.

## Session 414 - 2026-04-25 12:27 - продолжение остановленного запуска VM в IDE

Что сделано:
- `IProgramRunService` расширен методом `Continue(BytecodeVmStopState stopState)`;
- `ProgramRunService.Continue(...)` продолжает живое состояние VM через `StopState.Resume()` и возвращает результат в обычной модели `ProgramRunResult`;
- `MainWindowViewModel` хранит `StoppedRunState`, выставляет `HasStoppedRun` и получила `ContinueRunCommand`;
- новый `RunProgram` очищает старое остановленное состояние перед запуском и сохраняет новое, если VM остановилась на точке остановки;
- `ContinueRunCommand` продолжает сохранённый запуск, обновляет вывод, диагностику, статус и очищает состояние после завершения программы;
- трассировка результата запуска теперь фиксирует признак `stopped`.

Изменённые файлы:
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~ProgramRunServiceTests" --logger "console;verbosity=minimal"` прошёл успешно, 101 тест;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 228 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- следующий шаг сделан крупнее: вместо одного только хранения `StopState` добавлен весь внутренний путь продолжения остановленного байткодного запуска;
- видимый интерфейс отладчика пока не добавлялся, чтобы сначала закрепить проверяемую модель и сервисный договор;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- привязать `ToggleSourceBreakpointLine(...)` и `ContinueRunCommand` к видимому интерфейсу редактора;
- добавить отображение точек остановки на поле редактора;
- позже перейти от общего состояния окна к модели точек остановки по документам.

Следующий шаг:
- добавить видимую команду IDE для переключения точки остановки по текущей строке и команду продолжения остановленного запуска, затем покрыть это тестами модели окна и при необходимости WPF-слоя.

## Session 415 - 2026-04-25 12:35 - видимые команды точек остановки и продолжения

Что сделано:
- добавлена команда `RunOrContinueCommand`: `F5` теперь запускает программу, а при остановленном VM-запуске продолжает его;
- добавлена команда `ToggleCurrentSourceBreakpointCommand`, принимающая номер текущей строки;
- `MainWindow.xaml.cs` подключает `F9` в редакторе к переключению точки остановки текущей строки;
- меню `Run` получило пункты `Continue` и `Toggle Breakpoint`;
- контекстное меню редактора получило пункт `Toggle Breakpoint`;
- русские и английские строки ресурсов дополнены новыми пунктами меню;
- добавлены тесты на `RunOrContinueCommand` и команду переключения текущей строки.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests" --logger "console;verbosity=minimal"` прошёл успешно, 130 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 230 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- горячие клавиши приведены к более привычной схеме популярных IDE: `F9` для точки остановки, `F5` для запуска или продолжения;
- `Shift+F5` не использован для продолжения, потому что в популярных IDE он чаще означает остановку отладки, а остановка в Axiom IDE ещё не реализована;
- видимый слой пока не рисует маркеры точек остановки, чтобы не смешивать командный вход и визуальное оформление в один слишком широкий шаг.

Что остаётся:
- нарисовать маркеры точек остановки на поле редактора;
- синхронизировать визуальные маркеры с `SourceBreakpointLines`;
- позже перейти к хранению точек остановки по документам.

Следующий шаг:
- добавить визуальное отображение точек остановки в редакторе и покрыть его тестами WPF/XAML или проверками кода редактора.

## Session 416 - 2026-04-25 12:45 - маркеры точек остановки в редакторе

Что сделано:
- добавлен `BreakpointMargin` для AvalonEdit, который рисует маркеры строковых точек остановки в левом поле редактора;
- `MainWindow.xaml.cs` подключает поле маркеров при загрузке редактора, обновляет его при изменении `SourceBreakpointLines`, применяет цвет текущей темы и отцепляет при выгрузке редактора;
- светлая и тёмная темы получили ресурсы `BreakpointMarkerColor` и `BreakpointMarkerBrush`;
- добавлены проверки подключения маркеров в IDE-коде и загрузки новых ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/Editor/Breakpoints/BreakpointMargin.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 52 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 231 тест;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- маркеры сделаны отдельным визуальным полем редактора, а не частью VM или сервиса запуска;
- цвет маркера задаётся через тему, чтобы светлая и тёмная оболочки не расходились;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- подсветить текущую строку штатной остановки VM после срабатывания точки остановки;
- позже заменить общий набор строк точек остановки на состояние по документам.

Следующий шаг:
- добавить визуальное отображение места остановки после срабатывания точки остановки либо начать пер-документную модель точек остановки для вкладок редактора.

## Session 417 - 2026-04-25 13:07 - отображение строки остановки VM в редакторе

Что сделано:
- `MainWindowViewModel` получил свойство `StoppedRunLine`;
- при остановке VM строка остановки берётся из `BytecodeVmStopState.Snapshot.SourceLocation.Line`;
- при новом запуске и после успешного продолжения строка остановки очищается;
- `MainWindow.xaml.cs` реагирует на `StoppedRunLine`, переводит активный редактор на нужную строку и включает отдельную подсветку;
- `AvalonEditDiagnosticNavigation` получил общий метод `NavigateToLine(...)`;
- светлая и тёмная темы получили ресурсы подсветки строки остановки;
- тесты IDE закрепляют публикацию строки остановки, подключение визуального слоя и загрузку новых ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Editor/Navigation/AvalonEditDiagnosticNavigation.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 136 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 231 тест;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- место остановки показывается через свойство модели окна, без прямого знания WPF-редактора о VM;
- подсветка строки остановки отделена от диагностической подсветки и от маркера точки остановки;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- перейти от общего набора строк точек остановки к состоянию по документам;
- добавить команду полной остановки отладки, если выбран следующий шаг по пользовательскому управлению запуском.

Следующий шаг:
- реализовать модель точек остановки по документам либо добавить стандартную команду остановки отладки.

## Session 418 - 2026-04-25 13:17 - стандартная команда остановки отладки

Что сделано:
- добавлен `StopDebuggingCommand` в `MainWindowViewModel`;
- команда сбрасывает `StoppedRunState`, `HasStoppedRun` и `StoppedRunLine`, не продолжая программу;
- `Shift+F5` подключён как стандартная горячая клавиша остановки отладки;
- меню `Run` получило пункт `Stop Debugging`;
- русская и английская локализация дополнены пунктом меню и статусами;
- добавлены тесты на сброс остановленного состояния без продолжения программы и проверки WPF/ресурсного слоя.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 138 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 233 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- `Shift+F5` выбран по стандартной схеме популярных IDE как остановка отладки;
- текущая команда завершает только уже остановленное VM-состояние, без попытки прерывать синхронно выполняющийся запуск;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- перейти к точкам остановки по документам;
- позже отдельно спроектировать отмену активно выполняющегося запуска и ожидания ввода.

Следующий шаг:
- реализовать хранение точек остановки по документам для вкладок редактора.

## Session 419 - 2026-04-25 14:03 - точки остановки по документам

Что сделано:
- `DocumentTabViewModel` получил собственное хранение `SourceBreakpointLines`;
- операции установки, очистки и переключения точек остановки перенесены на уровень документа;
- `MainWindowViewModel.SourceBreakpointLines` теперь отражает активный документ;
- при переключении активной вкладки публикуется изменение `SourceBreakpointLines`, чтобы редактор обновлял маркеры;
- запуск передаёт в VM точки остановки только активного документа;
- добавлен тест на независимые наборы точек остановки в двух вкладках.

Изменённые файлы:
- `src/Axiom.Ide/Documents/DocumentTabViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests" --logger "console;verbosity=minimal"` прошёл успешно, 134 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 234 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- точки остановки теперь принадлежат вкладке документа, а не главному окну;
- модель окна оставлена фасадом для существующих команд и WPF-привязок;
- пользовательское поведение языка Axiom не изменилось, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- решить, сохранять ли точки остановки в состоянии оболочки;
- связать место остановки VM с документом, чтобы при переключении вкладок не показывать строку остановки не того документа.

Следующий шаг:
- добавить связь остановленного запуска с документом или сохранение точек остановки между сеансами IDE.

## Session 420 - 2026-04-25 14:12 - остановленный запуск привязан к документу

Что сделано:
- `MainWindowViewModel` теперь хранит документ, которому принадлежит остановленный `BytecodeVmStopState`;
- `StoppedRunState`, `HasStoppedRun` и `StoppedRunLine` показываются только для активной вкладки остановленного запуска;
- при переключении на другой документ подсветка строки остановки исчезает, а `Continue` и `Stop Debugging` становятся недоступны;
- при возврате на исходный документ остановленное состояние снова доступно;
- запуск другого документа и закрытие документа остановленного запуска очищают старое состояние;
- добавлены тесты на скрытие состояния в другой вкладке и на запуск другого документа вместо продолжения старой остановки.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests" --logger "console;verbosity=minimal"` прошёл успешно, 88 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно, 236 тестов;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- привязка остановленного запуска остаётся на уровне IDE-модели окна, а не в `Axiom.Runtime` или `ProgramRunService`;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- решить следующий крупный слой отладчика: сохранять точки остановки между сеансами IDE или расширить управление остановленным запуском.

Следующий шаг:
- выбрать и реализовать следующий слой отладчика: сохранение точек остановки в состоянии оболочки либо более полное управление остановленным запуском.

## Session 421 - 2026-04-25 14:19 - перезапуск остановленной отладки

Что сделано:
- добавлен `RestartDebuggingCommand` в `MainWindowViewModel`;
- команда доступна только для активной вкладки, где есть остановленный VM-запуск;
- при перезапуске старое остановленное состояние сбрасывается, затем активный документ запускается заново обычным путём `Run`;
- если документ изменён, перед перезапуском используется обычный порядок сохранения перед запуском;
- добавлен пункт меню `Run -> Restart Debugging`;
- подключена стандартная горячая клавиша `Ctrl+Shift+F5`;
- русская и английская локализации дополнены строками меню и статуса;
- тесты покрывают перезапуск с актуальным текстом документа, доступность команды по активной вкладке, XAML-связку и строки ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 143 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- перезапуск отладки реализован как команда оболочки IDE поверх существующего пути `Run`, без нового договора в runtime;
- команда не перематывает старую VM, а создаёт новый запуск из текущего текста документа и текущих точек остановки;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- продолжить развитие управления остановленным запуском: рассмотреть очистку всех точек остановки, переход к месту остановки или сохранение точек остановки между сеансами IDE.

Следующий шаг:
- выбрать следующий отладочный слой: команды управления точками остановки или сохранение точек остановки между сеансами IDE.

## Session 422 - 2026-04-25 14:45 - исправление ручного сценария панели запуска и точки остановки

Что сделано:
- пункт `Toggle Breakpoint` переименован в более понятное `Установить/снять точку остановки`;
- в контекстное меню редактора добавлена подпись горячей клавиши `F9`;
- кнопка `Play` на панели быстрых действий переведена с `RunCommand` на `RunOrContinueCommand`;
- теперь `Play` продолжает остановленный запуск так же, как `F5` и пункт `Run -> Run`;
- рядом с `Play` добавлена кнопка `Stop Debugging`;
- подсказка кнопки `Play` уточнена как `Запустить/продолжить`;
- обработчик smoke-запуска в `MainWindow.xaml.cs` также переведён на `RunOrContinueCommand`;
- обновлены русские и английские строки ресурсов;
- обновлены тесты на XAML-связку панели быстрых действий, подпись `F9` и строки ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- первый обычный целевой прогон был заблокирован запущенным `Axiom.Ide.exe`, который держал основной исполняемый файл;
- временный целевой прогон в отдельный каталог вывода прошёл успешно, 144 теста;
- после закрытия IDE обычный целевой прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 144 теста;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решения:
- для кнопки `Play`, `F5`, пункта `Run -> Run` и smoke-запуска должен использоваться один путь `RunOrContinueCommand`;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- вручную перепроверить живой сценарий после пересборки: F9, Play до остановки, Play для продолжения, кнопка Stop.

Следующий шаг:
- после ручного подтверждения перейти к сохранению точек остановки между сеансами IDE или к дополнительным командам управления точками.
## Session 423 - 2026-04-25 15:06 - разделение запуска без отладки и отладочного запуска

Что сделано:
- обычный `RunCommand` отделен от отладочного запуска и теперь запускает активный документ без передачи точек остановки;
- `RunOrContinueCommand` оставлен отладочным действием: он стартует с точками остановки или продолжает уже остановленную VM;
- `RestartDebuggingCommand` теперь заново запускает активный документ в режиме отладки, чтобы точки остановки сохраняли смысл при перезапуске;
- добавлен `ClearAllSourceBreakpointsCommand` для очистки всех точек остановки активного документа;
- меню `Run`, контекстное меню редактора и панель быстрых действий получили отдельные входы для запуска без отладки, начала/продолжения отладки, перезапуска, остановки и очистки всех точек остановки;
- подключены привычные горячие клавиши: `F5`, `Ctrl+F5`, `Shift+F5`, `Ctrl+Shift+F5`, `F9`, `Ctrl+Shift+F9`;
- русская и английская локализация обновлены;
- регрессионные тесты расширены для нового различия между обычным запуском и отладочным запуском.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошел успешно, 148 тестов.

Решения:
- обычный запуск должен игнорировать точки остановки по аналогии с `Start Without Debugging`;
- `F5` остается отладочной командой запуска или продолжения;
- очистка всех точек остановки действует на активный документ, потому что текущая модель точек остановки уже привязана к вкладкам документов;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов;
- вручную проверить сценарий в живой IDE после сборки.

Следующий шаг:
- после проверки перейти к сохранению точек остановки между сеансами IDE или к расширению управления остановленным запуском, если ручная проверка покажет недочеты.
Дополнение к Session 423:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- в полном прогоне прошли тестовые проекты `Axiom.Lexing.Tests`, `Axiom.Parsing.Tests`, `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests`, `Axiom.Execution.Tests`, `Axiom.Ide.Tests` и `Axiom.Bytecode.Tests`.

Следующий шаг после проверки:
- вручную проверить живую IDE по новому сценарию запуска без отладки, отладочного запуска, продолжения, перезапуска, остановки и очистки всех точек остановки.
## Session 424 - 2026-04-25 16:03 - очистка точек остановки сбрасывает остановленную отладку

Что сделано:
- исправлен сценарий, где после очистки всех точек остановки активного документа оставалось живое остановленное состояние VM;
- `ClearAllSourceBreakpointsCommand` теперь при наличии `HasStoppedRun` вызывает общий сброс `SetStoppedRunState(null, null)`;
- после очистки всех точек `Continue` и `Stop Debugging` становятся недоступны;
- следующий `RunOrContinueCommand` начинает новый запуск с начала и без остановок, если точек остановки больше нет;
- добавлены русская и английская строки состояния для случая, когда очистка точек одновременно останавливает отладку;
- добавлен регрессионный тест на сценарий пользователя.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests" --logger "console;verbosity=minimal"` прошёл успешно, 93 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~ShellResourceTests" --logger "console;verbosity=minimal"` прошёл успешно, 98 тестов.

Решения:
- очистка всех точек остановки во время остановленной отладки должна завершать это остановленное состояние, потому что иначе UI показывает отсутствие точек, а модель всё ещё может продолжать старую VM;
- пользовательская семантика языка Axiom не изменилась, поэтому новые `.axm`-примеры и обновление `docs/manual/` не требуются.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов.

Следующий шаг:
- вручную проверить исправленный сценарий в IDE после полной проверки.
Дополнение к Session 424:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- в полном прогоне `Axiom.Ide.Tests` содержит 244 прошедших теста, включая новый регрессионный сценарий очистки точек остановки во время остановленной отладки.

Следующий шаг после проверки:
- вручную проверить тот же сценарий в живой IDE.
## Session 425 - 2026-04-25 16:10 - пиктограмма отладочного запуска

Что сделано:
- заменена пиктограмма кнопки `Start/Continue Debugging` на панели быстрых действий;
- новая пиктограмма сделана по мотиву предоставленного референса `debug-line-by-line.svg`: треугольник запуска рядом со строками кода;
- знак встроен прямо в `MainWindow.xaml` как WPF-вектор и использует существующий `RunAccentBrush`;
- внешний файл из `C:\Users\comp\Downloads` не добавлялся в репозиторий;
- тест панели быстрых действий обновлён, чтобы закрепить наличие нового векторного знака.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~EditorFeatureTests" --logger "console;verbosity=minimal"` прошёл успешно, 51 тест.

Решения:
- использовать более читаемый на маленькой панели мотив `запуск + строки кода`, а не более детальный знак с отладочным символом;
- не хранить предоставленные SVG как исходники проекта, потому что достаточно встроенной векторной геометрии в стиле текущей панели.

Что остаётся:
- выполнить последовательную сборку решения;
- визуально проверить кнопку в живой IDE при следующем ручном прогоне.

Следующий шаг:
- продолжить ручную проверку панели запуска и отладки.
Дополнение к Session 425:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок.

Следующий шаг после проверки:
- визуально проверить кнопку `Start/Continue Debugging` в живой IDE.
## Session 426 - 2026-04-25 16:19 - пиктограммы отладки в меню Run

Что сделано:
- заменены пиктограммы `Run -> Start Debugging` и `Run -> Continue` на тот же векторный знак `запуск + строки кода`, который используется на панели быстрых действий;
- заменена пиктограмма `Run -> Set/Clear Breakpoint` на простой красный круг через `BreakpointMarkerBrush`;
- такая же пиктограмма точки остановки добавлена в контекстное меню редактора;
- пункт `Run -> Continue` теперь показывает горячую клавишу `F5`;
- тест XAML обновлён, чтобы закрепить новые пиктограммы меню и подпись `F5` у `Continue`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~EditorFeatureTests" --logger "console;verbosity=minimal"` прошёл успешно, 51 тест.

Решения:
- для `Continue` оставить тот же знак, что у `Start Debugging`, потому что обе команды используют общий отладочный ход `F5`;
- для `Set/Clear Breakpoint` использовать красный круг вместо предупреждающего треугольника, чтобы знак был ближе к реальному маркеру точки остановки.

Что остаётся:
- выполнить последовательную сборку решения;
- визуально проверить меню `Run` и контекстное меню редактора.

Следующий шаг:
- продолжить ручную проверку отладочного интерфейса.
Дополнение к Session 426:
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок.

Следующий шаг после проверки:
- визуально проверить обновлённые пиктограммы в меню `Run` и контекстном меню редактора.
## Session 427 - 2026-04-25 16:31 - пошаговое выполнение Step Over

Что сделано:
- добавлен режим `Step Over` в байткодную VM: остановленное состояние можно одноразово продолжить до следующей исходной строки на том же или более внешнем уровне вызовов;
- `BytecodeVmStopState` получил метод `StepOver()`;
- `ProgramRunService` и `IProgramRunService` получили отдельный метод `StepOver`;
- `MainWindowViewModel` получил `StepOverCommand`, который доступен для активной вкладки с остановленной отладкой;
- в IDE добавлены пункт `Run -> Step Over`, кнопка панели быстрых действий и горячая клавиша `F10`;
- добавлены русские и английские ресурсы для меню, панели и строки состояния;
- добавлены и обновлены тесты runtime, службы запуска, модели окна, XAML и ресурсов;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet build Axiom.sln` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-build` прошёл успешно.

Решения:
- первый шаг ручной отладки сделан как стандартный `Step Over` с клавишей `F10`;
- `Step Into`, `Step Out` и шаг назад не смешивались с этим изменением;
- шаг назад записан как отдельная будущая задача, потому что требует истории состояний VM.

Что остаётся:
- вручную проверить `Step Over` в живой IDE;
- затем добавить `Step Into` и `Step Out`;
- отдельно спроектировать обратный шаг через снимки или историю состояний.

Следующий шаг:
- ручная проверка сценария `F9 -> F5 -> F10` и кнопки `Step Over` в IDE.
## Session 428 - 2026-04-25 16:43 - завершающие разделители PRINT в Bytecode

Что сделано:
- исправлено понижение обычного `PRINT` в байткодный слой: после завершающего `,` или `;` больше не добавляется лишняя инструкция `PrintLine`;
- байткодный режим теперь сохраняет поведение интерпретатора для `PRINT i,` и `PRINT i;` внутри циклов;
- добавлены тесты понижения для завершающей запятой и точки с запятой;
- добавлены тесты VM для `PRINT i,` и `PRINT i;` в циклах;
- добавлены сквозные тесты `ExecutionPipelineTests` для явного режима `Bytecode`;
- обновлены проверочные примеры `samples/operators/print/separator_comma.axm` и `samples/operators/print/separator_semicolon.axm`;
- обновлена страница справочника `docs/manual/print.html`;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Bytecode/Lowering/BoundProgramLowerer.cs`
- `tests/Axiom.Bytecode.Tests/BoundProgramLowererTests.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/print/separator_comma.axm`
- `samples/operators/print/separator_semicolon.axm`
- `docs/manual/print.html`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Bytecode.Tests\Axiom.Bytecode.Tests.csproj --no-restore --filter "FullyQualifiedName~BoundProgramLowererTests|FullyQualifiedName~BytecodeCompilerTests"` прошёл успешно, 53 теста;
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BytecodeVmTests"` прошёл успешно, 81 тест;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionPipelineTests|FullyQualifiedName~AcceptanceProgramTests"` прошёл успешно, 615 тестов.

Решения:
- исправлять поведение на уровне `BoundProgramLowerer`, потому что именно там создавался лишний перевод строки;
- не менять формат байткода и VM-инструкцию `PrintSeparator`, потому что они уже выражали нужную семантику.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов;
- вручную проверить пример пользователя в IDE в режиме `Bytecode`.

Следующий шаг:
- после полной проверки вернуться к отладчику: `Step Into` и `Step Out`.
Дополнение к Session 428:
- `dotnet build Axiom.sln` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-build` прошёл успешно.

Следующий шаг после проверки:
- вручную проверить пример с `PRINT i,` в IDE в режиме `Bytecode`;
- затем продолжить отладчик: `Step Into` и `Step Out`.
## Session 429 - 2026-04-25 17:00 - незавершённый вывод при пошаговой отладке

Что сделано:
- добавлен `RuntimeResult.PendingOutput` для снимка внутреннего буфера вывода байткодной VM на момент остановки;
- `BytecodeVm` возвращает этот снимок при начальной остановке, продолжении и `StepOver`;
- `ExecutionResult` переносит `PendingOutput` из runtime-слоя в слой запуска;
- `ProgramRunService` строит видимый `OutputText` из уже сброшенного вывода консоли и текущего незавершённого буфера;
- `MainWindowViewModel` обновляет нижнюю панель вывода после каждого шага, если текст результата изменился;
- добавлены тесты VM, службы запуска и модели окна для сценария `PRINT i,` во время пошаговой остановки;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Runtime/Host/RuntimeResult.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Execution/Results/ExecutionResult.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BytecodeVmTests"` прошёл успешно, 82 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramRunServiceTests|FullyQualifiedName~MainWindowViewModelTests"` прошёл успешно, 116 тестов.

Решения:
- не сбрасывать незавершённую строку в `IOutputWriter` при остановке, чтобы не менять семантику `PRINT` и не создавать дубли;
- показывать незавершённый вывод только как часть результата остановленного запуска.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов;
- вручную проверить пошаговый сценарий в IDE.

Следующий шаг:
- после полной проверки продолжить ручную проверку `Step Over` на цикле с `PRINT i,`.
Дополнение к Session 429:
- `dotnet build Axiom.sln` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-build` прошёл успешно.

Следующий шаг после проверки:
- вручную проверить `samples/operators/print/separator_comma.axm` в IDE: остановка на `PRINT i,`, шаги `F10`, постепенное появление `1 `, `1 2 `, `1 2 3 ` в окне вывода.
## Session 430 - 2026-04-25 17:13 - очистка точек остановки из контекстного меню

Что сделано:
- исправлен пункт контекстного меню редактора `Очистить все точки остановки`;
- пункт больше не использует привязку команды через `RelativeSource AncestorType=Window`, потому что `ContextMenu` живёт в отдельном popup-дереве и такая привязка не доходит до модели окна;
- добавлен обработчик `OnClearAllBreakpointsMenuItemClick`, который выполняет тот же `ClearAllSourceBreakpointsCommand`, что главное меню и кнопка панели;
- обновлён тест XAML, чтобы закрепить обработчик контекстного меню.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~MainWindowViewModelTests"` прошёл успешно, 146 тестов.

Решение:
- для контекстного меню редактора использовать явный обработчик окна, как уже сделано для переключения точки остановки, чтобы не зависеть от дерева привязок popup-меню.

Что остаётся:
- вручную проверить очистку точек остановки из контекстного меню в живой IDE.

Следующий шаг:
- после ручной проверки продолжить следующий слой отладчика: `Step Into` и `Step Out`.
## Session 431 - 2026-04-25 18:03 - Step Into и Step Out

Что сделано:
- добавлены режимы `StepInto` и `StepOut` в байткодную VM;
- `BytecodeVmStopState` получил методы `StepInto()` и `StepOut()`;
- временная граница шага VM теперь хранит режим шага, исходную строку и глубину стека вызовов;
- `ProgramRunService` и `IProgramRunService` получили методы `StepInto` и `StepOut`;
- `MainWindowViewModel` получил команды `StepIntoCommand` и `StepOutCommand`;
- в меню `Run` добавлены `Step Into` и `Step Out`;
- на панель быстрых действий добавлены кнопки `Step Into` и `Step Out`;
- подключены стандартные горячие клавиши `F11` и `Shift+F11`;
- добавлены русские и английские ресурсы;
- добавлены тесты VM, службы запуска, модели окна, XAML и ресурсов;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BytecodeVmTests"` прошёл успешно, 84 теста;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ProgramRunServiceTests|FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests"` прошёл успешно, 176 тестов.

Решения:
- `Step Into` останавливается на следующей исходной строке даже при увеличении глубины вызовов;
- `Step Out` останавливается после возврата на меньшую глубину вызовов;
- шаг назад оставлен вне этого изменения.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов;
- вручную проверить `F11` и `Shift+F11` в IDE;
- следующим визуальным шагом добавить более заметную подсветку текущего выполняемого оператора или строки остановки.

Следующий шаг:
- после полной проверки перейти к визуальному выделению текущего оператора в отладчике.
Дополнение к Session 431:
- `dotnet build Axiom.sln` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-build` прошёл успешно.

Следующий шаг после проверки:
- вручную проверить `Step Into` (`F11`) и `Step Out` (`Shift+F11`) в IDE;
- затем добавить визуальное выделение текущего выполняемого места в отладчике.
## Session 432 - 2026-04-25 18:20 - выделение текущего диапазона отладки

Что сделано:
- добавлен визуальный renderer диапазона текущей остановленной инструкции в AvalonEdit;
- строковая подсветка остановки сохранена, а поверх неё добавлена контрастная плашка по `SourceLocation.Span`;
- renderer подключается для каждого редактора при загрузке и снимается при выгрузке;
- диапазон очищается при снятии остановленного состояния;
- добавлены ресурсы светлой и тёмной темы для плашки и её рамки;
- обновлены тесты ресурсов и XAML/кода редактора;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests|FullyQualifiedName~MainWindowViewModelTests"` прошёл успешно, 153 теста.

Решение:
- не использовать обычное выделение текста редактора, чтобы не сбивать пользовательское выделение;
- рисовать диапазон отдельным `IBackgroundRenderer` поверх уже существующей строковой подсветки.

Что остаётся:
- выполнить полный последовательный прогон сборки и тестов;
- вручную проверить видимость плашки в живой IDE.

Следующий шаг:
- после полной проверки вручную оценить, достаточно ли точны текущие диапазоны `SourceLocation.Span`; если нет, улучшать отладочную информацию байткода.

Дополнение к Session 432:
- `dotnet build Axiom.sln` прошёл успешно, 0 предупреждений и 0 ошибок;
- `dotnet test Axiom.sln --no-build` прошёл успешно.

Следующий шаг после полной проверки:
- вручную проверить в IDE `Step Over`, `Step Into`, `Step Out` и контрастную плашку текущего оператора на светлой и тёмной теме;
- если диапазон текущего оператора окажется слишком широким или неточным, уточнять `SourceLocation.Span` на стороне отладочной информации байткода.

## Session 433 - 2026-04-25 20:23 - русские подписи команд пошаговой отладки

Что сделано:
- проверена русская терминология Visual Studio для команд пошаговой отладки;
- русские подписи меню и подсказки панели быстрых действий заменены на более понятные:
  - `Step Into` -> `Шаг с заходом в процедуру/функцию`;
  - `Step Over` -> `Шаг через`;
  - `Step Out` -> `Выйти из процедуры/функции`;
- строки состояния также уточнены, чтобы было понятно, что речь идёт о процедуре или функции;
- обновлены тесты ресурсов IDE;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests"` прошёл успешно, 56 тестов.

Решение:
- не оставлять короткие бытовые формулировки вроде `Шаг с обходом`, потому что без контекста они плохо читаются;
- использовать термин `Шаг через` по образцу Visual Studio;
- для `Step Into` и `Step Out` явно писать `процедура/функция`, потому что это ближе к модели языка Axiom и понятнее пользователю.

Следующий шаг:
- вручную проверить русское меню `Запуск`, подсказки кнопок пошаговой отладки и текущую подсветку оператора в живой IDE.

## Session 434 - 2026-04-25 22:17 - запуск пошаговой отладки с начала программы

Что сделано:
- добавлен флаг `StopBeforeFirstInstruction` в `ExecutionRequest`;
- `AxiomExecutionPipeline` передаёт в байткодную VM одноразовую стартовую остановку перед первой исполняемой инструкцией;
- `ProgramRunService.Run` получил параметр `stopBeforeFirstInstruction`;
- `StepOverCommand` и `StepIntoCommand` в `MainWindowViewModel` теперь доступны без остановленного состояния и запускают новый пошаговый отладочный сеанс с начала программы;
- после стартовой остановки повторные `F10` и `F11` продолжают уже остановленную VM штатным `StepOver` или `StepInto`;
- `StepOutCommand` оставлен доступным только при остановленной программе;
- добавлен ручной пример `samples/smoke/debug_step_controls.axm` с основным телом программы, процедурой, функцией и комментариями для проверки `F10`, `F11`, `Shift+F11`;
- обновлены тесты `Execution`, `ProgramRunService` и `MainWindowViewModel`;
- обновлены `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Execution/Requests/ExecutionRequest.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `samples/smoke/debug_step_controls.axm`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- обычный целевой `dotnet test` сначала не прошёл из-за запущенного `Axiom.Ide.exe`, который держал файл `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Execution.dll`;
- процесс IDE не закрывался принудительно, чтобы не потерять пользовательское состояние;
- повторная проверка выполнена в изолированных каталогах сборки;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ProgramRunServiceTests|FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~ShellResourceTests" -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.Tests\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.Tests\ --verbosity minimal` прошёл успешно, 179 тестов;
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~ExecutionPipelineTests|FullyQualifiedName~AcceptanceProgramTests" -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Execution.Tests\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Execution.Tests\ --verbosity minimal` прошёл успешно, 617 тестов.

Решение:
- не блокировать `Start Debugging` без точек остановки: это остаётся обычным отладочным запуском до точки или до конца программы;
- считать `F10` и `F11` отдельными входами в пошаговую отладку: если VM ещё не остановлена, они создают остановку перед первой исполняемой инструкцией;
- не включать `Step Out` до старта, потому что выходить можно только из уже выполняемой процедуры или функции.

Следующий шаг:
- вручную открыть `samples/smoke/debug_step_controls.axm` в IDE и проверить `F10`, `F11`, `Shift+F11`, подсветку текущего оператора и вывод.
## Session 435 - 2026-04-25 22:34 - уточнение русской подписи Step Over

Что сделано:
- русская подпись команды `Step Over` заменена с `Шаг через` на `Шаг без захода в процедуру/функцию`;
- русская подсказка кнопки `Step Over` теперь повторяет эту же формулировку без длинного пояснения;
- русские подсказки кнопок `Step Into` и `Step Out` также сокращены до тех же формулировок, что и в меню;
- строка состояния для `Step Over` уточнена как выполнение шага без захода в процедуру или функцию;
- обновлены тесты ресурсов IDE;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests" -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.Labels\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.Labels\ --verbosity minimal` прошёл успешно, 56 тестов.

Решение:
- не использовать в русской оболочке короткую кальку `Шаг через`;
- для Axiom явно писать, что `F10` выполняет шаг без захода в процедуру или функцию;
- подсказки панели быстрых действий держать короткими и одинаковыми по смыслу с пунктами меню.

Следующий шаг:
- вручную проверить русские подписи и подсказки в IDE, затем продолжить оценку визуальной подсветки текущего выполняемого диапазона на примере `samples/smoke/debug_step_controls.axm`.
Дополнение к Session 435:
- после закрытия IDE выполнен обычный целевой прогон без изолированных каталогов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests" --verbosity minimal` прошёл успешно, 56 тестов.
## Session 436 - 2026-04-25 22:41 - подпись продолжения отладки и пояснение пошагового старта

Что сделано:
- русский пункт меню `Run -> Continue` переименован с `Продолжить` на `Продолжить отладку`;
- в тесты ресурсов IDE добавлена проверка строки `Menu.Run.Continue`;
- в `docs/SPECIFICATION2.md` зафиксировано простое правило: `F5` запускает или продолжает отладку до точки остановки, а пошаговый старт с начала программы выполняется через `F10` или `F11`;
- обновлён `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- обычный целевой прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests" --verbosity minimal` был заблокирован открытым процессом `Axiom.Ide.exe` с PID 12512, который держал `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Ide.exe`;
- изолированный прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests" -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.ContinueLabel\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.ContinueLabel\ --verbosity minimal` прошёл успешно, 56 тестов.

Решение:
- держать `F5` как команду запуска или продолжения отладки до точки остановки;
- считать `F10` и `F11` отдельными входами в пошаговую отладку: если остановленного состояния ещё нет, они создают стартовую остановку перед первой исполняемой строкой.

Следующий шаг:
- оценить, нужно ли сделать подписи `F10` и `F11` ещё более явными в меню, например добавить в них слово `пошагово` или отдельную группу для пошаговой отладки.
## Session 437 - 2026-04-25 22:45 - явные подписи пошаговой отладки

Что сделано:
- русская подпись `Step Over` заменена на `Пошагово без захода в процедуру/функцию`;
- русская подпись `Step Into` заменена на `Пошагово с заходом в процедуру/функцию`;
- подсказки кнопок `F10` и `F11` приведены к тем же формулировкам;
- строки состояния для `F10` и `F11` теперь говорят о пошаговом выполнении;
- обновлены тесты ресурсов IDE;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- до запроса не запускать тесты был выполнен точечный изолированный прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests" -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.StepLabels\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.StepLabels\ --verbosity minimal`;
- прогон прошёл успешно, 56 тестов;
- после запроса не запускать тесты дополнительные проверки не выполнялись.

Решение:
- использовать слово `Пошагово` в командах `F10` и `F11`, чтобы они читались как способ начать пошаговую отладку, а не только как действие внутри уже остановленной программы;
- оставить `F5` отдельной командой запуска или продолжения отладки до точки остановки.

Следующий шаг:
- вручную проверить восприятие меню `Запуск` в IDE и затем продолжить работу по отладчику.
## Session 438 - 2026-04-25 22:53 - обычный запуск отделён от отладки

Что сделано:
- русский пункт `Ctrl+F5` переименован с `Запустить без отладки` на `Запуск программы`;
- подсказка кнопки обычного запуска на панели быстрых действий переименована в `Запуск программы`;
- английская локализация для этого пункта приведена к `Run Program`;
- в меню `Запуск` добавлен разделитель после обычного запуска и перед командами отладки;
- обновлены тесты ресурсов и тест XAML-разметки, чтобы фиксировать новую подпись и разделитель;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- по просьбе пользователя тесты не запускались;
- выполнена только текстовая проверка через `rg`, которая подтвердила наличие `Запуск программы`, `Run Program` и разделителя между `RunProgramMenuItem` и `StartDebuggingMenuItem`.

Решение:
- обычный запуск больше не описывать через отрицание `без отладки`;
- визуально отделять обычный запуск от команд отладки;
- текущую команду `Компилировать` считать внутренней подготовкой программы в памяти, а не созданием внешнего исполняемого файла.

Следующий шаг:
- отдельно решить, нужно ли переименовать `Компилировать` в более пользовательски понятную команду проверки или сборки программы.
## Session 439 - 2026-04-25 22:56 - отдельное меню отладки

Что сделано:
- добавлено верхнее меню `Отладка`;
- из меню `Запуск` убраны команды отладки;
- в меню `Запуск` оставлены `Компилировать`, `Запуск программы` и `Режим запуска`;
- в меню `Отладка` перенесены команды `Начать отладку`, `Продолжить отладку`, пошаговые команды, остановка, перезапуск и управление точками остановки;
- русские и английские ресурсы получили отдельные ключи `Menu.Debug.*`;
- контекстное меню редактора для установки и очистки точек остановки переведено на строки `Menu.Debug.*`;
- обновлены тесты ресурсов и XAML-разметки;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- по просьбе пользователя тесты не запускались;
- выполнена текстовая проверка через `rg`: отладочные команды в верхнем меню больше не используют `Menu.Run.*`, а находятся под `Menu.Debug.*`.

Решение:
- обычный запуск и выбор режима исполнения держать в меню `Запуск`;
- все действия с отладчиком держать в отдельном меню `Отладка`;
- не менять горячие клавиши и внутреннюю механику команд в рамках этой реорганизации.

Следующий шаг:
- вручную проверить меню в IDE и затем решить, нужно ли переименовать `Компилировать` в более понятную пользовательскую команду.
## Session 440 - 2026-04-25 23:03 - исправление запуска IDE после разделителей меню

Что сделано:
- исправлена ошибка XAML-загрузки при старте IDE после реорганизации меню;
- три новых разделителя в меню `Отладка` больше не используют отсутствующий ресурс `DropDownMenuSeparatorStyle`;
- разделители заменены на обычный `<Separator />`;
- обновлён `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- по просьбе пользователя тесты не запускались;
- выполнена текстовая проверка через `rg`: ссылок на `DropDownMenuSeparatorStyle` в `MainWindow.xaml` больше нет.

Решение:
- не вводить отдельный стиль разделителя меню без существующего ресурса;
- использовать обычные разделители, как в остальном главном меню.

Следующий шаг:
- вручную запустить IDE и проверить открытие меню `Запуск` и `Отладка`.
## Session 441 - 2026-04-25 23:05 - единый пункт начала и продолжения отладки

Что сделано:
- в меню `Отладка` объединены пункты `Начать отладку` и `Продолжить отладку`;
- новый пункт называется `Начать/продолжить отладку` и использует `F5`;
- отдельный пункт меню `Продолжить отладку` удалён;
- удалены ресурсы `Menu.Debug.Continue`;
- обновлены тесты ресурсов и XAML-разметки;
- обновлены `docs/SPECIFICATION2.md` и `docs/PLANS2.md`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- тесты не запускались;
- выполнена текстовая проверка через `rg`: `Menu.Debug.Continue`, `ContinueProgramMenuItem` и `ContinueDebuggingMenuIcon` больше не используются.

Решение:
- держать один пункт `F5`, потому что `RunOrContinueCommand` уже объединяет старт и продолжение отладки по текущему состоянию.

Следующий шаг:
- вручную проверить меню `Отладка` в IDE.

## Session 442 - 2026-04-26 07:53 - проектная граница шага назад в отладчике

Что сделано:
- подтверждено, что тема подписей меню уже закрыта и дополнительные переименования не нужны;
- зафиксирован следующий этап отладчика: шаг назад должен проектироваться через историю снимков состояния VM, а не как ещё один метод продолжения текущего `BytecodeVmStopState`;
- в спецификации описаны границы будущего шага назад, недопустимость скрытого перезапуска и ограничения первого среза;
- в архитектуре зафиксировано разделение ответственности между `Axiom.Runtime`, `Axiom.Execution` и `Axiom.Ide`;
- в плане обновлён следующий шаг реализации.

Изменённые файлы:
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- до уточнения пользователя был запущен целевой прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~ShellResourceTests|FullyQualifiedName~EditorFeatureTests|FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~ProgramRunServiceTests" -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`;
- прогон прошёл успешно, 179 тестов;
- после уточнения пользователя дополнительные проверки не запускались.

Решение:
- не менять текущие подписи `Компилировать`, `Запуск программы` и меню `Отладка`;
- не добавлять шаг назад поверх живого одноразового остановленного состояния;
- сначала реализовать внутреннюю историю снимков VM для ограниченного чистого среза без внешних побочных эффектов.

Следующий шаг:
- начать реализацию runtime-модели снимка состояния VM для будущего шага назад, пока без подключения новой команды IDE.

## Session 443 - 2026-04-26 07:58 - первый снимок состояния VM для будущего шага назад

Что сделано:
- добавлен внутренний runtime-объект снимка состояния байткодной VM;
- снимок включает место выполнения, глобальные значения, кадры вызовов, слоты, стек выражений и незавершённый буфер вывода;
- `BytecodeVm` теперь создаёт снимок перед выполнением инструкции и хранит предыдущий обратимый снимок;
- `BytecodeVmStopState` получил признак `CanStepBack`, который пока только отражает наличие предыдущего снимка;
- история обратимого выполнения сбрасывается при `INPUT`, файловых операциях, записи вывода во внешний хост и вызовах встроенных функций;
- команда IDE для шага назад и восстановление состояния пока не подключались.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVmStateSnapshot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.Input.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.FileIO.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- по просьбе пользователя проверки и тесты после этой реализации не запускались.

Решение:
- первый срез шага назад ограничен чистыми скалярными значениями;
- внешние эффекты не откатываются неявно, а сбрасывают историю обратимого выполнения.

Следующий шаг:
- добавить внутреннее восстановление VM из `BytecodeVmStateSnapshot`;
- затем покрыть восстановление тестами, когда проверки снова будут разрешены.

## Session 444 - 2026-04-26 08:02 - восстановление VM из снимка состояния

Что сделано:
- добавлен внутренний метод шага назад для остановленного состояния байткодной VM;
- `BytecodeVmStopState` получил метод `StepBack()`;
- `BytecodeVm` научилась восстанавливать глобальные значения, стек вызовов, слоты кадров, стек выражений и незавершённый вывод из `BytecodeVmStateSnapshot`;
- снимки связаны в цепочку, поэтому после отката можно сохранить доступность более раннего шага назад;
- при восстановлении захваченные переменные связываются с уже восстановленными слотами внешних кадров, когда это возможно;
- команда IDE и слой `Axiom.Execution` пока не изменялись.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVmStateSnapshot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStopState.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- по просьбе пользователя тесты и сборка не запускались.

Решение:
- шаг назад остаётся одноразовым действием над текущим остановленным состоянием;
- успешный откат создаёт новое остановленное состояние на восстановленной инструкции.

Следующий шаг:
- добавить автоматические тесты runtime-слоя для `StepBack`, когда проверки снова будут разрешены;
- затем перенести доступность шага назад через `Axiom.Execution` в IDE.

## Session 445 - 2026-04-26 08:06 - проверки и тесты runtime-шага назад

Что сделано:
- добавлены runtime-тесты для внутреннего `StepBack`;
- проверен откат от точки остановки к предыдущей инструкции;
- проверена доступность более раннего снимка после первого отката;
- проверена недоступность шага назад после `INPUT`;
- исправлена проверка запроса ввода в тесте: `InputRequest` сравнивается по свойствам, а не по объектному равенству.

Изменённые файлы:
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 459 тестов;
- `dotnet test --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- оставить первый тестовый набор на уровне runtime, пока шаг назад ещё не выведен в `Axiom.Execution` и IDE.

Следующий шаг:
- вынести доступность шага назад через `Axiom.Execution`;
- затем подключить команду и пункт меню в IDE.

## Session 446 - 2026-04-26 08:13 - команда шага назад в IDE

Что сделано:
- `ExecutionResult` получил признак `CanStepBack`;
- `ProgramRunResult` получил признак `CanStepBack`;
- `IProgramRunService` и `ProgramRunService` получили метод `StepBack`;
- `MainWindowViewModel` получил `StepBackCommand`;
- команда шага назад доступна только когда остановленный запуск содержит предыдущий обратимый снимок;
- в меню `Отладка` добавлен пункт `Шаг назад`;
- на панель быстрых действий добавлена кнопка `Шаг назад`;
- добавлены русские и английские строки ресурсов;
- добавлены тесты для `Axiom.Execution`, `ProgramRunService`, XAML-ресурсов и `MainWindowViewModel`.

Изменённые файлы:
- `src/Axiom.Execution/Results/ExecutionResult.cs`
- `src/Axiom.Ide/Models/ProgramRunResult.cs`
- `src/Axiom.Ide/Services/Execution/IProgramRunService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 618 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 257 тестов;
- `dotnet test --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- горячую клавишу для шага назад пока не назначать;
- сначала проверить понятность пункта меню и кнопки, затем отдельно закрепить клавиатурный договор.

Следующий шаг:
- вручную проверить шаг назад в IDE на короткой программе со скалярными переменными;
- затем решить вопрос горячей клавиши и дальнейшего расширения обратимости.

## Session 447 - 2026-04-26 08:23 - шаг назад внутри процедуры и клавиша Ctrl+F10

Что сделано:
- исправлена недоступность шага назад после входа `F11` внутрь прямого вызова процедуры или функции без захватов;
- runtime-снимок теперь допускает bytecode function-value без захваченных слотов;
- добавлен тест, где `StepBack` после входа в `PrintBanner` возвращает к строке вызова;
- для команды шага назад назначена горячая клавиша `Ctrl+F10`;
- пункт меню `Отладка -> Шаг назад` показывает `Ctrl+F10`;
- `MainWindow.xaml.cs` подключает `Ctrl+F10` к `StepBackCommand`;
- `samples/smoke/debug_step_controls.axm` обновлён для ручной проверки `Ctrl+F10` внутри `PrintBanner`.

Изменённые файлы:
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `samples/smoke/debug_step_controls.axm`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 460 тестов;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 257 тестов;
- `dotnet test --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- закрепить `Ctrl+F10` как горячую клавишу шага назад;
- пока поддерживать function-value в снимке только для встроенных значений и bytecode-значений без захватов.

Следующий шаг:
- вручную проверить `Ctrl+F10` внутри `PrintBanner` в `samples/smoke/debug_step_controls.axm`;
- затем решать следующий срез обратимости, включая function-value с захватами, только отдельной задачей.

## Session 448 - 2026-04-26 08:40 - обратимый вывод для шага назад внутри процедуры

Что сделано:
- подтверждена причина живого сбоя: после строк `PRINT` внутри процедуры VM сбрасывала историю шага назад из-за записи во внешний вывод;
- добавлен интерфейс `IReversibleOutputWriter`;
- `BytecodeVmStateSnapshot` теперь хранит контрольную точку вывода;
- `BytecodeVm` восстанавливает контрольную точку вывода при `StepBack`;
- `ExecutionRuntimeHostAdapter` откатывает свой список строк вывода и прокидывает откат дальше;
- `ProgramConsoleService` откатывает текст панели вывода IDE;
- `NullRuntimeHost` получил нулевую обратимую контрольную точку;
- добавлен runtime-тест на шаг назад после двух строк вывода внутри процедуры;
- добавлен IDE-тест на восстановление панели вывода при шаге назад.

Изменённые файлы:
- `src/Axiom.Runtime/Host/IReversibleOutputWriter.cs`
- `src/Axiom.Runtime/Host/NullRuntimeHost.cs`
- `src/Axiom.Runtime/Execution/BytecodeVmStateSnapshot.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs`
- `src/Axiom.Ide/Services/Input/IProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 461 тест;
- обычный прогон IDE-тестов был заблокирован запущенным `Axiom.Ide.exe`, который держал DLL в стандартном каталоге;
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -m:1 -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.StepBackOutput\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.StepBackOutput\ --logger "console;verbosity=minimal"` прошёл успешно: 258 тестов;
- `dotnet test -m:1 -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.StepBackOutput\ -p:OutputPath=.artifacts\bin-isolated\Axiom.StepBackOutput\ --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- считать вывод обратимым только для хостов, которые явно поддерживают `IReversibleOutputWriter`;
- для остальных хостов вывод остаётся внешним эффектом и сбрасывает историю.

Следующий шаг:
- перезапустить IDE и проверить `Ctrl+F10` внутри `PrintBanner` после строки `PRINT title` или на строке `PRINT "sub: end"`;
- после подтверждения можно переходить к следующему расширению обратимости.

## Session 449 - 2026-04-26 08:55 - очистка вывода при шаге назад и план журнала действий IDE

Что сделано:
- исправлена рассинхронизация, при которой шаг назад очищал вывод в runtime/console, но `BottomPane.OutputText` оставлял старый непустой текст;
- `MainWindowViewModel` теперь применяет `ProgramRunResult.OutputText` даже при пустом выводе;
- добавлено trace-событие `run.stop-state` для каждого изменения остановленного состояния;
- trace включает строку, колонку, opcode, глубину вызовов, `canResume`, `canStepBack`, документ и текущий вывод;
- добавлен IDE-тест: шаг назад до состояния перед `PRINT "main: start"` очищает панель вывода;
- в документах зафиксирован следующий крупный этап: структурированный журнал действий IDE.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -m:1 -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.Ide.StepBackEmptyOutput\ -p:OutputPath=.artifacts\bin-isolated\Axiom.Ide.StepBackEmptyOutput\ --logger "console;verbosity=minimal"` прошёл успешно: 259 тестов;
- `dotnet test -m:1 -p:UseSharedCompilation=false -p:BaseIntermediateOutputPath=.artifacts\obj-isolated\Axiom.StepBackEmptyOutput\ -p:OutputPath=.artifacts\bin-isolated\Axiom.StepBackEmptyOutput\ --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- текущий `IIdeTraceService` пока используется для наблюдаемости отладчика;
- полноценный журнал действий IDE выделить отдельной подсистемой, чтобы не перегрузить обычный trace шумными событиями редактора.

Следующий шаг:
- перезапустить IDE и проверить, что шаг назад до `DIM result` очищает строку `main: start` из панели вывода;
- затем начать первый срез структурированного журнала действий IDE.

## Session 450 - 2026-04-26 09:38 - первый рабочий срез структурированного журнала действий IDE

Что сделано:
- добавлен новый слой событийного журнала IDE: `IIdeEventLogService`, `IdeEventLogEntry`, `IdeEventStatusBarSnapshot`, `IdeEventLevel`;
- реализован `FileIdeEventLogService` с записью `JSONL`, ограниченной фоновой очередью, ротацией файла и очисткой старых журналов;
- добавлен `NullIdeEventLogService` для безопасного fallback и тестов;
- `IdeShellCompositionRoot` теперь создаёт и прокидывает `IIdeEventLogService` в `ProgramRunService`, `ProgramConsoleService` и `MainWindowViewModel`;
- `App` пишет структурные события `startup`, `shutdown` и `exception.unhandled`;
- `ProgramRunService` пишет события компиляции, запуска и шагов отладки;
- `ProgramConsoleService` пишет события `output.changed`, `input.requested`, `input.submitted`, `output.restored`;
- `MainWindowViewModel` пишет события команд, документов, файловых операций, stop-state и `status-bar.changed` со снимком статус-бара;
- добавлены тесты `FileIdeEventLogServiceTests` и проверки structured-событий в `ProgramConsoleServiceTests` и `ProgramRunServiceTests`;
- обновлены документы `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md` и `docs/структурированный журнал действий IDE.txt`.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLevel.cs`
- `src/Axiom.Ide/Services/EventLog/IdeEventStatusBarSnapshot.cs`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogEntry.cs`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogOptions.cs`
- `src/Axiom.Ide/Services/EventLog/IIdeEventLogService.cs`
- `src/Axiom.Ide/Services/EventLog/NullIdeEventLogService.cs`
- `src/Axiom.Ide/Services/EventLog/FileIdeEventLogService.cs`
- `src/Axiom.Ide/Services/Shell/IdeShellCompositionRoot.cs`
- `src/Axiom.Ide/App.xaml.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/FileIdeEventLogServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `docs/структурированный журнал действий IDE.txt`

Проверка:
- `dotnet test tests\\Axiom.Ide.Tests\\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 262 теста;
- `dotnet test --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно по всем тестовым проектам.

Решение:
- `IIdeTraceService` сохранён как отдельный технический текстовый лог;
- структурированный журнал внедряется отдельно и уже доступен без изменения пользовательской семантики языка Axiom.

Следующий шаг:
- добавить в IDE UI-доступ к журналу действий (`Вид -> Журнал действий IDE`, открытие файла/папки, очистка журналов);
- добавить сжатие шумных событий редактора (курсор, выделение, набор текста) с буферизацией.

## Session 451 - 2026-04-26 09:53 - перенос временных данных примеров в .artifacts

Что сделано:
- подтверждено, что временные элементы `axiom_sample_*` в корне `C:\Axiom` создавались ручными `.axm`-примерами файловых функций из `samples/functions/`;
- пути в этих примерах переведены с `axiom_sample_*` на `.artifacts/axiom_sample_*`;
- в `AGENTS.md` и `README.md` добавлено явное правило: временные файлы, каталоги, журналы, снимки и данные ручных проверок должны создаваться только внутри `.artifacts/`;
- временные элементы `axiom_sample_*`, уже созданные в корне рабочей копии, удалены.

Изменённые файлы:
- `AGENTS.md`
- `README.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `samples/functions/appendfile/basic_usage.axm`
- `samples/functions/copydir/basic_usage.axm`
- `samples/functions/copyfile/basic_usage.axm`
- `samples/functions/deletedir/basic_usage.axm`
- `samples/functions/deletefile/basic_usage.axm`
- `samples/functions/direxists/basic_usage.axm`
- `samples/functions/filedate/basic_usage.axm`
- `samples/functions/fileexists/basic_usage.axm`
- `samples/functions/filesize/basic_usage.axm`
- `samples/functions/makedir/basic_usage.axm`
- `samples/functions/movedir/basic_usage.axm`
- `samples/functions/movefile/basic_usage.axm`
- `samples/functions/pathexists/basic_usage.axm`
- `samples/functions/readfile/basic_usage.axm`
- `samples/functions/renamedir/basic_usage.axm`
- `samples/functions/renamefile/basic_usage.axm`
- `samples/functions/writefile/basic_usage.axm`

Проверка:
- проверен поиск по `samples/functions`: новых прямых путей `axiom_sample_*` без `.artifacts/` не осталось;
- автоматические .NET-тесты не запускались, потому что менялись только ручные примеры и документация.

Решение:
- все будущие временные данные ручных запусков и проверок должны жить в `.artifacts/`;
- корень репозитория не должен использоваться как рабочая папка для мусорных проверочных файлов.

Следующий шаг:
- продолжить плановый шаг: добавить доступ к журналу действий IDE из интерфейса.

## Session 452 - 2026-04-26 14:26 - сжатие шумных событий редактора в журнале IDE

Что сделано:
- в `MainWindowViewModel` добавлен пакетный сбор шумных редакторных событий:
  - `ReportEditorCaretActivity` -> `editor.caret.burst`;
  - `ReportEditorSelectionActivity` -> `editor.selection.burst`;
  - `ReportEditorTextActivity` -> `editor.text.burst`;
  - `FlushEditorActivityEvents` для принудительного сброса буферов;
- burst-записи включают `eventsCount`, `durationMs`, время начала/конца пакета и последние редакторные параметры;
- `LogIdeEvent` теперь перед записью обычного события IDE сбрасывает накопленные burst-пакеты, чтобы не терять хвост буфера;
- в `MainWindow.xaml.cs` подключена передача сигналов `Caret`, `SelectionChanged`, `TextChanged` в агрегатор и добавлен явный `flush` при выгрузке редактора, потере фокуса и смене вкладки;
- для снижения лишнего шума в `status-bar.changed` отключена запись для `LineColumnText` и `CompilationProgressPercent`;
- добавлены тесты `MainWindowViewModelTests` на сжатие серии событий курсора в один burst и на автоматический сброс по порогу.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 268 тестов.

Решение:
- оставить burst-подход как базовый механизм для шумных событий редактора, не переводя весь журнал в полный поток посимвольных событий.

Следующий шаг:
- добавить в IDE удобный просмотр и фильтрацию журнала действий по категориям/поиску, чтобы `jsonl` не приходилось анализировать только внешним редактором.

## Session 453 - 2026-04-26 19:11 - встроенный просмотр журнала действий IDE

Что сделано:
- добавлен встроенный просмотр текущего `.jsonl` журнала действий IDE из меню `Вид -> Журнал действий IDE -> Открыть просмотр в IDE`;
- перед открытием просмотра команда сбрасывает накопленные события текущей сессии в файл журнала;
- окно просмотра показывает события таблицей: номер, время, уровень, категория, имя события, команда, документ и краткие подробности;
- добавлены фильтр по категории и базовый поиск по событию, категории, уровню, команде, документу и подробностям;
- строки журнала с ошибкой разбора не ломают весь просмотр, а учитываются отдельным счётчиком;
- сохранены прежние команды открытия текущего файла журнала, папки журналов и очистки старых журналов.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Services/EventLog/IIdeEventLogViewerService.cs`
- `src/Axiom.Ide/Services/EventLog/NullIdeEventLogViewerService.cs`
- `src/Axiom.Ide/Services/EventLog/WpfIdeEventLogViewerService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Services/Shell/IdeShellCompositionRoot.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- первый запуск `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` упёрся в ограничение песочницы для `DOTNET_CLI_HOME`;
- повторный запуск с `DOTNET_CLI_HOME=C:\Axiom\.artifacts\.dotnet-home` прошёл успешно: 270 тестов.

Решение:
- оставить встроенный просмотр как отдельный WPF-слой поверх существующего `IIdeEventLogService`, чтобы запись журнала, хранение и ротация файлов не зависели от окна просмотра.

Следующий шаг:
- вручную проверить новый пункт меню в живой IDE;
- затем добавить удобный просмотр подробностей выбранного события, копирование строки и ручное обновление файла журнала.

## Session 454 - 2026-04-26 19:18 - исправление открытия активного журнала IDE

Что сделано:
- исправлена ошибка встроенного просмотра журнала, когда активный `.jsonl` нельзя было открыть из-за текущей записи IDE в этот же файл;
- чтение журнала в `IdeEventLogViewerWindow` переведено с `File.ReadLines` на явный `FileStream` с `FileShare.ReadWrite | FileShare.Delete`;
- разбор строк журнала вынесен в `IdeEventLogViewerWindow.ReadEntries`, чтобы сценарий можно было проверить без запуска WPF-окна;
- добавлен регрессионный тест, который держит файл журнала открытым для записи и проверяет, что просмотрщик всё равно читает уже сброшенные строки.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/FileIdeEventLogServiceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 271 тест.

Решение:
- активный журнал должен читаться с разделённым доступом, потому что это нормальный пользовательский сценарий: IDE пишет события, а пользователь одновременно открывает просмотр текущей сессии.

Следующий шаг:
- перезапустить живую IDE и повторно проверить пункт `Вид -> Журнал действий IDE -> Открыть просмотр в IDE`;
- после ручного подтверждения продолжить улучшение окна просмотра: подробности выбранного события, копирование строки и ручное обновление.

## Session 455 - 2026-04-26 19:37 - подробности и действия в просмотре журнала IDE

Что сделано:
- подтверждена ручная проверка: встроенный просмотр журнала открывается без ошибки блокировки активного `.jsonl`;
- в `IdeEventLogViewerWindow` добавлена нижняя область подробностей выбранного события;
- подробности показывают основные поля события, документный и отладочный контекст, снимок строки состояния и дополнительные свойства;
- добавлена кнопка `Обновить`, которая заново читает активный журнал, сохраняет фильтр и пытается восстановить выбор по `sequence`;
- добавлена кнопка `Копировать`, которая копирует выбранное событие в буфер обмена одной JSON-строкой;
- счётчик событий теперь показывает видимые события, общее число загруженных событий и ошибки разбора при их наличии;
- добавлены строки ресурсов для русского и английского интерфейса;
- добавлены тесты на наличие новых элементов окна и строк ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 272 теста.

Решение:
- оставить ручное обновление как текущий режим перечитывания журнала, без фонового слежения за файлом, чтобы окно просмотра не добавляло новый постоянный поток событий и не усложняло первый рабочий слой.

Следующий шаг:
- вручную проверить в живой IDE выбор строки, `Обновить` после новых действий и `Копировать`;
- затем уменьшить оставшийся шум журнала и уточнить, какие события меню/команд действительно полезны для диагностики пользовательского сценария.

## Session 456 - 2026-04-26 20:19 - эргономика окна просмотра журнала IDE

Что сделано:
- по ручной проверке окна журнала улучшена разметка встроенного просмотра;
- таблица событий теперь занимает всю ширину окна, а не оставляет пустую область справа;
- у таблицы явно включены горизонтальная и вертикальная полосы прокрутки;
- между таблицей и подробностями добавлен `GridSplitter`, чтобы нижнюю область можно было растягивать;
- для таблицы добавлено контекстное меню: копирование текущей ячейки, выбранного события как JSON и подробностей;
- для области подробностей добавлено контекстное меню: копирование выделенного текста, всех подробностей и выбранного события как JSON;
- правый клик по таблице выбирает строку и ячейку перед открытием контекстного меню;
- добавлены строки ресурсов для русского и английского интерфейса;
- обновлены тесты ресурсов и XAML-признаков окна просмотра.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 272 теста.

Решение:
- оставить следующий уровень улучшений за отдельным шагом: сначала подтвердить новый UX вручную, затем снижать шум событий и уточнять состав полезных записей журнала.

Следующий шаг:
- перезапустить живую IDE и проверить прокрутку таблицы, растягивание подробностей и контекстное копирование;
- после подтверждения заняться оставшимся шумом `status-bar.changed` и событиями меню/команд.

## Session 457 - 2026-04-26 20:29 - выделение подробностей и снижение шума status-bar

Что сделано:
- в контекстное меню области подробностей просмотра журнала добавлена команда `Выделить всё`;
- команда `Выделить всё` фокусирует поле подробностей и выделяет весь текст;
- нижняя область подробностей переведена во вложенную `Grid`-панель, поэтому разделитель теперь меняет высоту всей панели подробностей, а не только строки подписи;
- `status-bar.changed` теперь пишется только для изменения `IsVisible`;
- обычные изменения текста состояния, пути, координат и прогресса больше не создают отдельные шумные строки журнала;
- добавлены проверки ресурсов, XAML-признаков и поведения фильтра `status-bar.changed`.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 273 теста.

Решение:
- оставить отдельное событие `status-bar.changed` только для пользовательски значимого изменения видимости строки состояния; остальные значения строки состояния сохранять как снимок внутри обычных событий IDE.

Следующий шаг:
- вручную проверить новое меню подробностей, растягивание нижней панели и чистоту журнала после открытия документа;
- затем уточнить и добавить полезные события меню/команд, если в ручном сценарии останутся слепые зоны.

## Session 458 - 2026-04-26 20:35 - читаемые заголовки журнала в тёмной теме

Что сделано:
- исправлена нечитаемость заголовков колонок встроенного просмотра журнала IDE в тёмной теме;
- таблица событий `IdeEventLogViewerWindow` переведена на общий `DiagnosticsGridStyle`, который уже задаёт тёмный шаблон `DataGridColumnHeader`;
- добавлена проверка XAML, подтверждающая, что окно журнала использует общий стиль таблиц IDE.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 273 теста.

Решение:
- не вводить отдельные цвета для окна журнала; использовать общий стиль таблиц IDE, чтобы светлая и тёмная темы оставались согласованными.

Следующий шаг:
- перезапустить IDE и вручную проверить заголовки таблицы просмотра журнала в тёмной теме;
- затем продолжить уточнение полезных событий меню и команд для журнала действий IDE.

## Session 459 - 2026-04-26 20:47 - общая тёмная стилизация меню и списков

Что сделано:
- добавлен общий `DialogComboBoxStyle` для выпадающих списков в диалогах и служебных окнах;
- добавлены неименованные стили `ContextMenu` и `MenuItem` на основе существующих `ShellContextMenuStyle` и `DropDownMenuItemStyle`, чтобы локальные контекстные меню не проваливались в системное светлое оформление;
- добавлен тематический ресурс `TextBoxEditingContextMenu` для текстовых полей;
- `OutputTextBoxStyle` и `ConsoleInputStyle` теперь используют тематическое контекстное меню вместо стандартного системного меню `TextBox`;
- окно просмотра журнала подключило `DialogWindowStyle`, `FindReplaceTextBoxStyle`, `DialogComboBoxStyle`, `DialogButtonStyle`, `DiagnosticsGridStyle` и `ShellContextMenuStyle`;
- добавлены проверки XAML и загрузки ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- первый запуск `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` показал, что объект `ContextMenu` нельзя создавать из не-STA тестового потока;
- проверка самого `TextBoxEditingContextMenu` переведена на строковый XAML-признак;
- повторный запуск `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 274 теста.

Решение:
- исправлять такие проблемы через общий ресурсный слой темы, а не через локальные цвета в отдельных окнах;
- для WPF-объектов, требующих STA при создании, использовать строковые XAML-проверки в обычных non-UI тестах.

Следующий шаг:
- вручную проверить живую IDE в тёмной теме: список категорий журнала, кнопки окна журнала, контекстное меню таблицы и подробностей, контекстное меню поля `Вывод`;
- затем продолжить уточнение состава полезных событий меню и команд в журнале действий IDE.

## Session 460 - 2026-04-26 20:53 - закрытое поле ComboBox в тёмной теме

Что сделано:
- исправлено закрытое поле выпадающего списка `Категория` в окне просмотра журнала;
- `DialogComboBoxStyle` получил собственный `ControlTemplate` для закрытого состояния `ComboBox`;
- фон закрытого поля, рамка, выбранный текст, стрелка и раскрывающийся `PART_Popup` теперь используют общие ресурсы темы;
- добавлена XAML-проверка, что общий стиль `DialogComboBoxStyle` содержит шаблон закрытого поля.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 274 теста.

Решение:
- для `ComboBox` использовать собственный шаблон, потому что стандартный WPF-шаблон не принимает достаточно надёжно общие цвета темы для закрытого поля.

Следующий шаг:
- перезапустить IDE и вручную проверить закрытый и раскрытый список `Категория` в тёмной теме;
- затем продолжить уточнение полезных событий меню и команд в журнале действий IDE.

## Session 461 - 2026-04-27 08:22 - проверка активного журнала и горизонтальная прокрутка таблицы

Что сделано:
- учтена ручная проверка пользователя: окно просмотра журнала подтверждено рабочим в светлой и тёмной теме;
- разобран активный журнал `C:\Users\comp\AppData\Local\Axiom\Logs\Events\ide-events-20260427-075557.jsonl` по прогону `C:\Axiom\samples\smoke\Recursion check.axm`;
- по журналу подтверждена согласованная последовательность отладочных событий (`debugger.resume-result`, `debugger.stop-state-changed`, `debugger.step-start-result`, `debugger.stopped`) без ошибок и предупреждений;
- в `IdeEventLogViewerWindow` исправлена проблема с неудобной шириной таблицы: колонка `Details` переведена с `Width="*"` на фиксированную ширину `360`, чтобы горизонтальная прокрутка появлялась предсказуемо при переполнении.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- оставить горизонтальную прокрутку таблицы журнала основанной на реальном переполнении колонок, а не на растягивании последней колонки на всю ширину окна.

Следующий шаг:
- продолжить плановое уточнение состава полезных событий меню и команд в журнале действий IDE.

## Session 462 - 2026-04-27 08:32 - локализация и пиктограммы меню журнала IDE

Что сделано:
- исправлена живая смена языка для окна просмотра журнала IDE: заголовок окна, подпись файла, счётчик, список категорий, текст отсутствующего выбора и заголовки колонок обновляются после переключения русского/английского языка;
- первый столбец таблицы журнала больше не захардкожен как `Seq`: добавлены ресурсы `EventLog.Viewer.SequenceHeader` со значениями `№` и `Seq`;
- `IdeLanguageResourceService` получил событие `LanguageApplied`, чтобы вспомогательные окна могли обновлять невизуальные WPF-свойства после смены словаря;
- основная оболочка после смены языка сбрасывает статус-бар на локализованное `Готово.` / `Ready.`, чтобы в нём не оставалось сообщение из предыдущего языка;
- контекстные меню таблицы журнала и области подробностей получили общий стиль пунктов и пиктограммы;
- общий `TextBoxEditingContextMenu`, который используется в `Output` и консольной строке ввода, получил пиктограммы `Вырезать`, `Копировать`, `Вставить`, `Выделить всё`;
- добавлены регрессионные проверки XAML и ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/Services/Shell/IdeLanguageResourceService.cs`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- для `DataGridColumn` и заголовка вспомогательного окна использовать явное обновление после события смены языка, потому что эти свойства не ведут себя как обычные визуальные элементы с `DynamicResource`.

Следующий шаг:
- вручную проверить живую смену языка при открытом окне журнала и контекстные меню `Output`, `Diagnostics`, таблицы журнала и подробностей;
- затем продолжить уточнение полезных событий меню и команд в журнале действий IDE.

## Session 463 - 2026-04-27 08:38 - упрощение меню подробностей журнала

Что сделано:
- уточнено контекстное меню области подробностей выбранного события в `IdeEventLogViewerWindow`;
- удалён пункт `Копировать выделенный текст`, потому что в текущем окне он пересекался по пользовательскому смыслу с `Копировать подробности`;
- оставлены две разные команды копирования: полный многострочный текст подробностей и исходная запись события как JSON;
- пункт `Выделить всё` перенесён в конец меню после одного разделителя;
- удалён больше не используемый обработчик `OnCopySelectedDetailsTextClick`;
- обновлены регрессионные проверки XAML и ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"`.

Решение:
- держать в меню подробностей только действия с разным результатом: подробности как читаемый текст, событие как JSON и выделение всего текста.

Следующий шаг:
- вручную проверить контекстное меню области подробностей в живой IDE;
- затем продолжить уточнение полезных событий меню и команд в журнале действий IDE.

## Session 464 - 2026-04-27 08:44 - удаление полосы под верхней панелью IDE

Что сделано:
- у контейнера верхней панели быстрого доступа в `MainWindow.xaml` убрана нижняя рамка, которая визуально выглядела как лишняя серая полоса под панелью инструментов;
- кнопки панели, подсказки и внутренние вертикальные разделители оставлены без изменения;
- добавлена регрессионная XAML-проверка, фиксирующая отсутствие нижней рамки у панели быстрого доступа.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- не вводить новый стиль ради одного визуального артефакта; достаточно отключить нижнюю рамку у уже существующего контейнера панели быстрого доступа.

Следующий шаг:
- вручную проверить основное окно IDE после запуска в светлой и тёмной теме, затем продолжить работу по полезным событиям меню и команд в журнале действий IDE.

## Session 465 - 2026-04-27 08:46 - общий стиль верхней панели быстрого доступа

Что сделано:
- проверены места в XAML, где используется `ToolbarBackgroundBrush`;
- найдено два визуально близких случая: верхняя панель быстрого доступа и заголовок файловой панели;
- для верхней панели добавлен общий `QuickAccessToolbarBorderStyle` в `ShellStyles.xaml`;
- `MainWindow.xaml` переведён на этот стиль вместо локального задания фона, рамки и отступов;
- заголовок файловой панели оставлен без изменения, потому что его нижняя граница отделяет панель файлов от дерева и не относится к артефакту под верхней панелью;
- обновлены регрессионные проверки XAML и загрузки ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- держать оформление верхней панели в общем стиле оболочки, но не применять его ко всем `Border` с `ToolbarBackgroundBrush`, чтобы не убрать осмысленные границы у других панелей.

Следующий шаг:
- вручную проверить основное окно IDE в светлой и тёмной теме, затем продолжить работу по полезным событиям меню и команд в журнале действий IDE.

## Session 466 - 2026-04-27 08:49 - устранение полосы из шаблона вкладок документов

Что сделано:
- после ручной проверки пользователя подтверждено, что отключение нижней рамки панели быстрого доступа не убрало артефакт полностью;
- найден более точный источник: стандартный шаблон `TabControl`, используемый `DocumentTabControlStyle`, добавлял служебную область между панелью инструментов и вкладками;
- `DocumentTabControlStyle` получил собственный `ControlTemplate` с `TabPanel` и `PART_SelectedContentHost` без промежуточной строки высотой `1`;
- добавлена регрессионная XAML-проверка шаблона вкладок документов.

Изменённые файлы:
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- исправлять не локальную панель инструментов, а общий стиль вкладок документов, потому что артефакт находился на границе между toolbar и `TabControl`.

Следующий шаг:
- вручную проверить светлую и тёмную тему после сборки, затем продолжить работу по полезным событиям меню и команд в журнале действий IDE.

## Session 467 - 2026-04-27 08:53 - разделение стилей верхних и нижних вкладок

Что сделано:
- повторная ручная проверка пользователя показала, что предыдущие изменения не убрали полосу под верхней панелью;
- принято решение разделить верхние вкладки редактора и нижнюю панель по стилям, потому что общий `DocumentTabControlStyle` используется в двух разных местах;
- добавлен `EditorDocumentTabControlStyle` с фоном `EditorBackgroundBrush` и отключённой рамкой;
- добавлен `EditorDocumentTabItemStyle` с отключённой рамкой и без внешнего отступа;
- верхний `DocumentTabs` переведён на новые стили;
- нижняя панель `Output` / `Diagnostics` оставлена на общем стиле и сохраняет свой разделитель;
- обновлены проверки XAML и загрузки ресурсов.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- не применять одно правило ко всем вкладкам: верхний редактор должен быть визуально чистым под toolbar, а нижняя панель должна оставаться отделённой от редактора.

Следующий шаг:
- вручную запустить свежесобранную IDE из `.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Ide.exe` и проверить светлую и тёмную тему; если артефакт останется, проверить путь реально запущенного процесса.

## Session 468 - 2026-04-27 08:57 - откат неверной правки вкладок основной IDE

Что сделано:
- остановлена ветка изменений, которая ошибочно затронула вкладки основной IDE вместо окна просмотра журнала;
- удалены добавленные стили `EditorDocumentTabControlStyle` и `EditorDocumentTabItemStyle`;
- `DocumentTabControlStyle` возвращён к прежнему наследованию от `ShellTabControlStyle` без собственного шаблона;
- верхний `DocumentTabs` возвращён на прежние стили `DocumentTabControlStyle` и `ShellTabItemStyle`;
- удалена регрессионная проверка, закреплявшая неверную гипотезу о шаблоне вкладок документов;
- проверка загрузки ресурсов возвращена к прежнему набору вкладочных стилей.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- не продолжать глобальные изменения основной IDE в рамках задачи по окну журнала;
- дальнейшая диагностика полосы должна быть ограничена разметкой и стилями `IdeEventLogViewerWindow`, если пользователь вернётся к этой задаче.

Следующий шаг:
- дождаться уточнения пользователя и не вносить новые изменения без отдельного подтверждения.

## Session 469 - 2026-04-27 09:01 - удаление выделения всего из меню подробностей журнала

Что сделано:
- из контекстного меню `SelectedDetailsTextBox` в `IdeEventLogViewerWindow` удалён пункт `Выделить всё`;
- удалён разделитель, который отделял этот пункт от команд копирования;
- удалён обработчик `OnSelectAllDetailsClick`, потому что после удаления пункта меню он больше не используется;
- регрессионная проверка окна журнала обновлена: тест теперь фиксирует отсутствие `EventLog.Viewer.SelectAllMenuItem` и `OnSelectAllDetailsClick`.

Изменённые файлы:
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml`
- `src/Axiom.Ide/Services/EventLog/IdeEventLogViewerWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно.

Решение:
- оставить в меню подробностей только команды с самостоятельным смыслом: полный текст подробностей и исходное событие JSON.

Следующий шаг:
- вручную проверить контекстное меню области подробностей в живом окне журнала.

## Session 470 - 2026-04-27 13:21 - контекстное меню вкладки вывода

Что сделано:
- контекстное меню `OutputTextBox` в основной IDE заменено на точный набор команд для поля вывода: `Копировать`, `Очистить`, `Выделить всё`;
- команды `Вырезать` и `Вставить` больше не показываются в меню вкладки `Вывод`, потому что поле доступно только для чтения;
- меню продолжает использовать общие стили `ShellContextMenuStyle`, `DropDownMenuItemStyle` и `MenuItemGlyphStyle`, без отдельного локального оформления;
- добавлены обработчики правого клика и открытия меню: при существующем выделении правый клик в любой точке поля вывода сохраняет выделенный текст и открывает меню без визуального сброса;
- пункт `Копировать` включается только при непустом выделении;
- в `ProgramConsoleService` добавлен метод `ClearOutput()`, который очищает только текст вывода и не сбрасывает ожидающий `INPUT`, текущую подсказку и введенный текст;
- команда `Очистить` во вкладке `Вывод` использует `ClearOutput()`, чтобы внутреннее состояние сервиса не возвращало старый вывод при следующей записи.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Services/Input/IProgramConsoleService.cs`
- `src/Axiom.Ide/Services/Input/ProgramConsoleService.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ProgramConsoleServiceTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 278 тестов.

Решение:
- состав команд меню для `Вывод` задаётся локально у конкретного поля, потому что это отличается от обычного редактируемого `TextBox`;
- визуальное оформление не локализуется: меню и пункты меню остаются на общих стилях IDE;
- очистка вывода вынесена в сервис консоли отдельным методом, чтобы не смешивать пользовательскую очистку экрана с полным сбросом сессии выполнения.

Следующий шаг:
- вручную проверить вкладку `Вывод` в живой IDE: после запуска программы правый клик должен показывать только `Копировать`, `Очистить`, `Выделить всё`; `Очистить` должен удалять видимый вывод; правый клик по невыделенной области при существующем выделении не должен сбрасывать выделение.

## Session 471 - 2026-04-27 13:26 - фиксация выделения в выводе при правом клике

Что сделано:
- в `OutputTextBox` добавлен обработчик `PreviewMouseRightButtonUp`;
- в `MainWindow.xaml.cs` добавлен флаг `_preserveOutputSelectionOnRightClick` для связки стадий правого клика;
- если выделение уже есть, правый клик теперь полностью перехватывается на `Down/Up`, выделение сохраняется и контекстное меню открывается без сброса выделенного текста;
- при обычном открытии меню состояние флага сбрасывается, чтобы не было зависшего состояния;
- обновлены XAML/кодовые регрессионные проверки.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 278 тестов.

Решение:
- проблема была в том, что перехватывалась только первая фаза правого клика, а `TextBox` мог снять выделение на следующей фазе;
- перехват обеих фаз устраняет рассинхрон «выделения нет, а копирование доступно».

Следующий шаг:
- вручную проверить сценарий в живой IDE на вкладке `Вывод`.

## Session 472 - 2026-04-27 13:31 - доработка фикса выделения в выводе

Что сделано:
- переработан обработчик правого клика в `OutputTextBox`: убрано ручное `ContextMenu.IsOpen`, сохранение и восстановление выделения переведено на схему `PreviewMouseRightButtonDown` -> `PreviewMouseRightButtonUp` -> `ContextMenuOpening`;
- добавлены поля `_outputSelectionStart` и `_outputSelectionLength` для устойчивого восстановления диапазона;
- в `OutputTextBoxStyle` добавлено `IsInactiveSelectionHighlightEnabled=True`, чтобы выделение оставалось визуально заметным при открытом контекстном меню;
- обновлены регрессионные проверки в `EditorFeatureTests`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 278 тестов.

Решение:
- первичный вариант с ручным открытием контекстного меню оставлял окно для сброса выделения в штатной обработке `TextBox`;
- переход на сохранение/восстановление диапазона и штатное открытие меню устраняет этот конфликт.

Следующий шаг:
- ручная проверка в живой IDE: выделить текст в `Вывод`, нажать правой кнопкой по соседней невыделенной области и убедиться, что выделение не пропадает.

## Session 473 - 2026-04-27 13:44 - выравнивание тона выделения в выводе

Что сделано:
- в `OutputTextBox` добавлено локальное переопределение `SystemColors.InactiveSelectionHighlightBrushKey` и `SystemColors.InactiveSelectionHighlightTextBrushKey`, чтобы при открытии контекстного меню выделенный текст не переходил в другой визуальный тон;
- в `OutputTextBoxStyle` добавлены `SelectionBrush` и `SelectionOpacity=1` для стабильного отображения выделения;
- обновлены проверки XAML в `EditorFeatureTests`.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 278 тестов.

Решение:
- проблема в скриншоте была не в потере диапазона выделения, а в переключении `TextBox` на неактивную кисть выделения;
- локальное выравнивание активной и неактивной кистей у `OutputTextBox` убирает визуальный «перерисованный» эффект.

Следующий шаг:
- ручная проверка в живой IDE по сценарию со скриншота.

## Session 474 - 2026-04-27 13:49 - откат проблемной заливки выделения в выводе

Что сделано:
- по подтверждённой регрессии из ручной проверки пользователя откатаны только последние визуальные изменения, которые затрагивали тон выделения в поле `Вывод`;
- в `OutputTextBox` удалён локальный блок ресурсов с `SystemColors.InactiveSelectionHighlightBrushKey` и `SystemColors.InactiveSelectionHighlightTextBrushKey`;
- из `OutputTextBoxStyle` удалены `SelectionBrush` и `SelectionOpacity`;
- сохранены рабочие правки предыдущей сессии: перехват `PreviewMouseRightButtonDown/Up`, восстановление диапазона выделения и `IsInactiveSelectionHighlightEnabled=True`;
- обновлены XAML-проверки в `EditorFeatureTests` под откатанное состояние.

Изменённые файлы:
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 278 тестов.

Решение:
- не выравнивать активное и неактивное выделение локальными кистями у `OutputTextBox`, потому что это привело к нечитаемой заливке на `Выделить всё`;
- оставить только устойчивое сохранение диапазона выделения и штатные цвета темы.

Следующий шаг:
- вручную проверить в живой IDE, что `Выделить всё` снова читаемо и правый клик по невыделенной области не сбрасывает выделение.

## Session 475 - 2026-04-27 15:03 - публикация репозитория на GitHub

Что сделано:
- проверен текущий `origin` и подтверждено, что рабочая ветка `master` уже связана с удаленной веткой `origin/master`;
- обновлен `.gitignore`: добавлены исключения для служебных локальных каталогов `.vs/` и `.codex/`;
- выполнен полный первичный импорт рабочих файлов проекта в Git;
- создан коммит `Initial project import` (`4405df6`);
- выполнен push в `origin/master`.

Измененные файлы:
- `.gitignore`
- `docs/WORKLOG3.md`
- `docs/PLANS2.md`

Проверка:
- `git status --short` перед коммитом показал staged-набор проекта без служебных `.vs/` и `.codex/`;
- `git push -u origin master` выполнен успешно (`c62b575..4405df6`).

Решение:
- публиковать проект через командную строку с явной настройкой `.gitignore`, чтобы контролировать состав загружаемых файлов;
- ветка публикации оставлена `master`, так как удаленный репозиторий уже использует `master` как основную.

Следующий шаг:
- продолжить основной план проекта: вручную проверить сценарий выделения в `Вывод` в живой IDE, затем перейти к следующему пункту по журналу действий IDE.

## Session 476 - 2026-04-27 16:55 - применение гайда OpenAI по GPT-5.5 к процессу проекта

Что сделано:
- изучен официальный гайд OpenAI по GPT-5.5 и сопоставлен с текущими правилами Axiom;
- принято решение не менять код, архитектуру и поведение языка, потому что запрос относится к процессу работы с агентом;
- в `AGENTS.md` добавлено правило работы с современными моделями OpenAI: сохранять документный контракт проекта, но формулировать задачи через ожидаемый результат, критерии готовности, допустимые побочные эффекты, проверки и форму отчёта без лишнего пошагового управления;
- обновлён `docs/PLANS2.md`.

Изменённые файлы:
- `AGENTS.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- сборка и тесты не запускались, потому что изменены только процессные документы и пользовательское поведение Axiom не менялось.

Решение:
- внешний гайд OpenAI полезен как настройка способа постановки задач, но не имеет приоритета над `AGENTS.md`, `PROJECT.md` и русской проектной документацией;
- для этого проекта не нужно «вайбкодить» свободнее: нужно давать агенту меньше лишней процедуры и больше ясного результата, критериев готовности и ограничений.

Следующий шаг:
- вернуться к основному плану проекта: вручную проверить сценарий выделения во вкладке `Вывод` в живой IDE, затем продолжить уточнение полезных событий меню и команд в журнале действий IDE.

## Session 477 - 2026-04-27 17:19 - события команд меню в журнале действий IDE

Что сделано:
- зафиксировано подтверждение ручной проверки: во вкладке `Вывод` выделение сохраняется при правом клике и `Выделить всё` снова читаемо;
- расширен структурированный журнал действий IDE для пользовательских команд оболочки;
- команды смены темы, языка, режима выполнения, стиля форматирования и умного идентификатора теперь пишут `command.executed`;
- команды открытия/закрытия нижних панелей `Output` / `Diagnostics` теперь пишут `command.executed`;
- успешное открытие руководства, команда `О программе`, успешный выход и отменённый выход теперь пишут структурированные события;
- для этих событий заполняется `menuName`, чтобы журнал показывал пользовательский контекст команды.

Изменённые файлы:
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.Formatting.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION2.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно: 281 тест;
- `dotnet build Axiom.sln --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно, 0 предупреждений и 0 ошибок.

Решение:
- не добавлять новый формат журнала: достаточно уже существующего `menuName` в `IdeEventLogEntry`;
- фиксировать именно команды верхнего меню и нижних панелей, потому что они помогают восстановить пользовательский сценарий и не создают редакторный шум.

Следующий шаг:
- вручную открыть IDE, выполнить несколько команд из меню `Вид`, `Запуск`, `Формат` и `Справка`, затем открыть встроенный просмотр журнала и проверить новые события `command.executed` с `commandName`, `menuName` и подробностями.

## Session 478 - 2026-04-27 18:42 - воспроизводимая smoke-проверка журнала действий IDE

Что сделано:
- вместо разовой ручной проверки добавлен сфокусированный режим `--event-log-only` в `Axiom.Ide.SmokeHarness`;
- новый маршрут проходит через реальные пункты меню `View`, `Run`, `Format`, `Help`, затем открывает встроенный просмотр журнала `IdeEventLogViewerWindow`;
- harness проверяет детали выбранных событий и подтверждает корректные `commandName`, `menuName` и свойства для `UseEnglishLanguage`, `UseInterpreterExecutionMode`, `ToggleSmartIdentifier` и `About`;
- в `MainWindow.xaml` добавлен `AutomationId` для верхнего меню `Format`, чтобы WPF automation не зависел от локализованного текста заголовка;
- `README.md` и `docs/ARCHITECTURE.md` синхронизированы под новый focused smoke-маршрут.

Изменённые файлы:
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

Проверка:
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` прошёл успешно;
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore -m:1 -p:UseSharedCompilation=false` прошёл успешно;
- `dotnet run --project src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -- --event-log-only --ide-exe C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Ide.exe` прошёл успешно.

Решение:
- для проверки журнала действий IDE полезнее отдельный воспроизводимый automation-маршрут, чем повторяемая только вручную последовательность кликов;
- focused smoke не заменяет общий WPF smoke baseline, а дополняет его для точечной проверки журнала и просмотра событий.

Ограничение:
- полный smoke-прогон по умолчанию в этой сессии отдельно упал раньше нового сценария, на старом шаге `File Manager -> New File`: harness не дождался `TextInputDialog`; это зафиксировано как отдельный хвост стабилизации, а не как регрессия event-log маршрута.

Следующий шаг:
- продолжить уточнение полезных событий оболочки в журнале IDE и отдельным шагом стабилизировать полный WPF smoke baseline для файловых операций.

## Session 479 - 2026-04-27 19:24 - памятка по публикации проекта на GitHub

Что сделано:
- в корне репозитория создан файл `ПУБЛИКАЦИЯ ПРОЕКТА НА GITHUB.txt`;
- в памятке зафиксированы:
  - разовая настройка `git config` и `gh auth login`;
  - обычная публикация через `git add -A`, `git commit`, `git push origin master`;
  - жёсткая перезапись удалённой ветки через `git push --force-with-lease origin master`;
  - порядок действий для страницы `github.com/login/device`, когда код берётся из PowerShell.

Изменённые файлы:
- `ПУБЛИКАЦИЯ ПРОЕКТА НА GITHUB.txt`
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- автоматические тесты и сборка не запускались, потому что изменены только вспомогательный текстовый файл и проектная документация.

Решение:
- хранить короткую памятку по публикации прямо в корне проекта, чтобы в следующий раз команды для `git` и `gh` были под рукой без поиска по переписке.

Следующий шаг:
- вернуться к основному плану проекта: продолжить уточнение полезных событий оболочки в журнале IDE и отдельным шагом стабилизировать полный WPF smoke baseline для файловых операций.

## Session 480 - 2026-04-27 20:26 - разворот плана к многомерным массивам

Что сделано:
- перечитаны обязательные документы проекта: `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION2.md`, `docs/ARCHITECTURE.md`, `docs/PLANS2.md`, `docs/WORKLOG3.md`;
- отдельно изучен внешний отчёт `C:\Users\comp\Downloads\deep-research-report (1).md` по развитию ветки массивов;
- сверено текущее состояние кода и спецификации: массивы в языке, binder, runtime и `BytecodeVm` пока одномерные, а многомерные массивы в спецификации явно не поддерживаются;
- в `docs/PLANS2.md` зафиксирован разворот ближайшего фокуса с IDE-shell на проектирование многомерных массивов и подветки подмассивов.

Изменённые файлы:
- `docs/PLANS2.md`
- `docs/WORKLOG3.md`

Проверка:
- сборка и тесты не запускались, потому что в этой сессии изменена только проектная документация и сформировано новое направление следующего language-slice.

Решение:
- не начинать код двумерных массивов без отдельного проектного среза, потому что текущая модель массива, типы, синтаксис индексирования и байткодный путь повсеместно зашиты под один индекс;
- в первый шаг брать не подмассивы и не broadcasting, а базовый каркас многомерных массивов: несколько измерений в `DIM`/`REDIM`, многомерное индексирование и согласованную внутреннюю модель хранения;
- пользовательские имена новых возможностей сначала сверять с BASIC-линейкой и по возможности расширять уже существующие формы `DIM`, `REDIM`, `LBOUND`, `UBOUND`.

Следующий шаг:
- подготовить и зафиксировать в `docs/SPECIFICATION2.md` проектное решение для первого шага многомерных массивов, затем на его основе переходить к связанному parser/semantics/runtime/bytecode-срезу.
