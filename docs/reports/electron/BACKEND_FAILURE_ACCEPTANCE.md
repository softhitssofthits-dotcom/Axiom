# Отказы подготовки и пригодность кэша — 16 октября 2026 года

## Что проверено

Задача продолжает приёмку подготовки исполнения после успешного запуска. Разделены три разных результата: ошибка семантики, BackendFailure компиляционного конвейера и InfrastructureFailure общей службы. Один результат не выдаётся за другой.

Воспроизводимый BackendFailure получен настоящим AxiomExecutionPipeline: BytecodeSnapshotPath указывает на существующий каталог вместо файла. Запись JSON-снимка завершается диагностикой MB4003. Это отказ сохранения снимка после формирования модуля в компиляторе, а не доказательство воспроизведения внутреннего дефекта понижения или выпуска инструкций.

## Проверки конвейера

Новый BackendFailureRecoveryTests содержит два сценария:

1. После успешно подготовленной и исполненной программы компиляция другой программы завершается MB4003. FrontendSucceeded остаётся true, но Succeeded/BackendSucceeded/BytecodeAvailable — false; возвращённый частичный CompiledProgram не содержит BytecodeModule. Его явная передача RunCompiled завершается BackendFailure/MB4001, без вывода, отладочной остановки и запуска VM. Инструкция WRITEFILE в проверяемой программе не создаёт файл. Следующий нормальный Run того же исходника выполняется через BytecodeVm и записывает ожидаемый текст.
2. После успешного Run непосредственно вызывается Run с неудачной записью снимка. Получены BackendFailure/MB4003, пустой вывод, отсутствие ActualEngine и отсутствие проверочного файла. Повтор без отказа даёт точные строки TRUE и 29 и правильные байты файла.

Две новые и две существующие проверки записи снимка прошли **4/4**:

```powershell
dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~BackendFailureRecoveryTests|FullyQualifiedName~ExecutionPipelineTests.Compile_ReturnsDiagnosticWhenBytecodeSnapshotWriteFails|FullyQualifiedName~ExecutionPipelineTests.CompileSnapshotFailureIsNotFullCompilationSuccess" --verbosity minimal
```

## Проверки общей службы DesktopHost

Добавлен tests/Axiom.Ide.Backend.Tests, напрямую ссылающийся на Axiom.Ide.Backend и не зависящий от Axiom.Ide с прежним редактором. Windows/WPF требуется самой общей службе; тест создаёт ProgramConsoleService в отдельном STA-потоке без запуска окон редактора. Проект включён в Axiom.sln.

CompilationCacheRecoveryTests сначала подтверждает пригодный кэш после успешных Compile/Run. Далее проверяются две ветви:

- семантический отказ MB2001;
- контролируемое IOException в синхронном IProgress.Report(90), до вызова BytecodeCompiler: служба возвращает InfrastructureFailure/IDE0003.

После каждого отказа Run прежнего корректного исходника проходит с UsedCompiledCache=false и точным ожидаемым выводом. Следующий Run уже использует новый кэш. Отказы не подменяют возвращаемые результаты конвейера; используется действующий публичный progress-параметр службы. Итог — **2/2**:

```powershell
dotnet test tests/Axiom.Ide.Backend.Tests/Axiom.Ide.Backend.Tests.csproj --no-restore --verbosity minimal
```

Первый запуск нового проекта выявил пропущенный using System.IO в самом тесте; импорт добавлен, окончательный запуск прошёл. Продуктовый код не исправлялся.

## Вывод и границы

Проверено шесть адресных случаев .NET. Реализация выдержала выбранные отказы, нового продуктового дефекта не подтверждено. ProgramRunService.Compile по исходнику сохраняет результат только при compilationResult.Succeeded, иначе присваивает null; catch также очищает кэш.

DesktopHost не предоставляет параметр BytecodeSnapshotPath. Поэтому MB4003 не воспроизводился через Electron/службу: в ядре подтверждён BackendFailure, в общей службе — очистка после SemanticFailure и InfrastructureFailure. Ветвь BackendFailure службы при внутреннем отказе понижения/эмиссии не выдана за сквозную приёмку. Для неё нужен отдельный реальный воспроизводимый случай; специальных переключателей отказа в продукт не добавляли. Строки IDE15/IDE29 остаются частичными с уточнёнными доказательствами.

Новые оконные сценарии и языковые формы не добавлялись; каталог Electron остаётся 425. Полный набор решения и Electron не запускался. Существующие программы samples/smoke/bytecode-only/compiler_execution.axm и samples/operators/input/name_age_transcript.axm не менялись и в этом этапе отдельно не запускались.

Версии без нового повышения: язык 0.1.15, компилятор 0.1.15, IDE 0.6.3. Изменены проверки и документы; продуктовая реализация сохранена. Предыдущие локальные изменения не затронуты. Коммит и push не выполнялись.
