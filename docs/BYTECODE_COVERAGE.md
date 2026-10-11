# Реестр покрытия Bytecode VM

Этот файл фиксирует текущее покрытие пользовательских операторов, встроенных функций и связанных проверок для байткодного пути Axiom.

## Как использовать реестр

- Это не разовый отчёт, а постоянный рабочий реестр.
- После каждой задачи, которая меняет наблюдаемое поведение языка, покрытие `BytecodeVm`, acceptance-примеры, manual или паритет с интерпретатором, этот файл нужно обновлять в той же задаче.
- Если задача касается только внутренней оптимизации без изменения пользовательского поведения, здесь всё равно нужно обновить как минимум столбцы `Профиль` и `Примечание`, если изменился статус горячего участка или появились новые замеры.

## Что именно отслеживаем

Минимальный набор для строки реестра:

- `VM`:
  байткодный путь реально выполняет эту сущность без отката на интерпретатор.
- `Автотесты`:
  есть проверка поведения хотя бы на уровне `InterpreterTests`, `BytecodeVmTests`, `ExecutionPipelineTests` или acceptance-набора.
- `Sample`:
  есть runnable `.axm`-пример в `samples/`.
- `Manual`:
  поведение отражено в `docs/manual/`.
- `Сквозной VM`:
  есть явное подтверждение, что сценарий проходит через `ExecutionEngineKind.BytecodeVm` без fallback.
- `Негативный паритет`:
  есть проверка ошибок или стабильности диагностики между интерпретатором и байткодом, точечная или групповая.
- `Профиль`:
  нужно ли это сейчас ускорять по измерениям.

Рабочая шкала для столбца `Профиль`:

- `горячо измерено, целевой диапазон` — адресный benchmark уже держится на уровне `<= 0.900x`.
- `горячо пройдено, рабочий паритет` — адресный benchmark уже ниже `1.000x`, но ещё остаётся в диапазоне `0.900x .. 1.000x`.
- `горячо измерено, прямой долг` — адресный benchmark уже выделен и показывает `> 1.000x`, то есть VM на этом слое пока медленнее интерпретатора.
- `не приоритет` — явного профильного долга сейчас нет или слой пока не является ближайшей целью разгона.

Статусы:

- `да`:
  покрытие есть и подтверждено.
- `частично`:
  покрытие есть, но оно групповое, косвенное или требует точечной досверки.
- `проверить`:
  есть сильное основание считать, что покрытие есть, но это ещё не перенесено в явный учётный статус.
- `нет`:
  покрытия сейчас нет или оно не зафиксировано.

## Общие источники подтверждения

- Поведенческие тесты интерпретатора: `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- Поведенческие тесты VM: `tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`
- Сквозной запуск через `BytecodeVm`: `tests/Axiom.Execution.Tests/ExecutionPipelineTests.cs`
- Acceptance-примеры: `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- Общая отрицательная проверка паритета диагностики: `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)` в `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`

## Операторы и языковые формы

| Сущность | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Арифметика и логика выражений: `+`, `-`, `*`, `/`, `\`, `MOD`, `^`, `AND`, `OR`, `XOR`, `NOT`, `EQV`, `IMP`, `<<`, `>>`, сравнения | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_ReportsDivisionByZero()`, `Execute_ReportsDivisionByZeroForIntegralOperators(...)`, `Execute_ReportsIntegerArithmeticOverflow()`, `Execute_ReportsLongArithmeticOverflow()`, `Execute_ReportsDoubleArithmeticOverflow()`, `Execute_CompiledProgramReportsDivisionByZeroWithSourceLocation()`, `Execute_CompiledIntegralDivisionReportsDivisionByZero()`, `Run_ReturnsRuntimeFailure()`, `Run_ReturnsRuntimeFailureForNumericOverflow(...)`, `Run_ReturnsSemanticFailureForBooleanOrderedComparison()`; acceptance: `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)`; широкий адресный benchmark `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` после вынесения `Divide` в отдельный быстрый путь держится около `0.853x` runtime / `0.860x` total, а отдельный bitwise/shift benchmark `samples/smoke/engine-compare/bitwise_shift_mesh.axm` держится около `0.855x` runtime / `0.856x` total для `AND`, `OR`, `XOR`, `NOT`, `<<` и `>>`, то есть побитовый подслой уже тоже переведён в целевой диапазон |
| Объявления и присваивание: `DIM`, `CONST`, `=` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Run_ReturnsSemanticFailureAndDoesNotExecute()`, acceptance-ошибки `samples/acceptance/negative/undeclared_variable.axm`, `samples/acceptance/negative/dim_requires_type_or_initializer.axm`, а также `samples/operators/const/fixed_string_error.axm` с проверкой `MB2026`; адресный benchmark `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` теперь отдельно нагружает локальные и глобальные присваивания и после вынесения `Divide` в отдельный быстрый путь держится около `0.853x` runtime / `0.860x` total |
| Массивные объявления и изменение формы: `REDIM`, `ERASE`, индексирование, частичное индексирование | да | да | да | да | да | да | горячо пройдено | Негативные опоры: `Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()`, `Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_RedimRejectsNonCanonicalEmptyRange()`, `Execute_DimRejectsMinimumIntegerLowerBound()`, `Execute_RedimRejectsMinimumIntegerLowerBound()`, `Execute_CompiledRedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_CompiledEraseOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_CompiledUnallocatedArrayAccessReportsRuntimeState()`; 1D-путь `LoadElement`/`StoreElement` облегчен так, чтобы успешный доступ к элементу не тянул `ResolveLocation` и медленную диагностику, после шага `mixed` держится около `0.653x` runtime / `0.818x` total, а отдельный сценарий `samples/smoke/engine-compare/array_staging_roundtrip.axm` для двухфазного staging через массивы строк и чисел держится около `0.474x` runtime / `0.580x` total |
| Вывод: `PRINT` | да | да | да | да | да | да | горячо пройдено, runtime в целевом диапазоне | Негативные опоры через выражения и runtime-цепочки: `Execute_NestedRuntimeErrorReportsCallChain()`, `Run_ReturnsRuntimeFailure()`, acceptance-паритет `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)`; отдельные sample по формам `PRINT` есть, а после прямой записи скалярных значений в `_pendingOutput` и удешевления `PRINT USING` адресный benchmark `samples/smoke/engine-compare/print_output_gallery.axm` держится около `0.884x` runtime / `0.918x` total |
| Форматированный вывод: `PRINT USING` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_PrintUsingReportsStringFieldTypeMismatch()`, `Execute_PrintUsingReportsNumericFieldTypeMismatch()`, `Execute_CompiledPrintUsingReportsFieldTypeMismatch()`, `Run_WithBytecodeMode_ReportsPrintUsingRuntimeFailureFromVm()`; после общих ускорений VM и длинного контрольного прогона `print_output_gallery.axm` (`20` итераций, `3` прогрева) слой вывода держится около `0.860x` runtime / `0.877x` total |
| Ввод: `INPUT` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_ReportsInvalidInputConversion()`, `Execute_ReportsInvalidMultiInputHandling()`, `Execute_CompiledInputReportsConversionFailure()`, `Run_WithBytecodeMode_ReportsInputRuntimeFailureFromVm()`; после быстрого пути для `INPUT` на 1/2/3 целей без промежуточного массива результатов и длинного прогона `input_record_intake.axm` (`40` итераций, `5` прогревов) слой держится около `0.879x` runtime / `0.889x` total |
| Файловые операторы: `OPEN`, `CLOSE` | да | да | да | да | да | да | не приоритет | Негативные опоры: `Execute_CloseReportsClosedFile()`, `Execute_OpenReportsAlreadyOpenFileVariable()`, `Execute_CompiledOpenFileReportsClosedFileRead()`, `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()` |
| Ветвление: `IF ... ELSE ... END IF`, `SELECT CASE` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `samples/acceptance/negative/if_requires_boolean.axm`, `Run_ReturnsSemanticFailureForSelectCaseRangeOnBoolean()`, общая semantic-проверка `Run_ReturnsSemanticFailureAndDoesNotExecute()`; после отложенной очистки `BytecodeEvaluationStack` и перевода `JumpIfEqual`/`JumpIfLess`/`JumpIfGreater...` на специализированные прямые пути адресный benchmark `samples/smoke/engine-compare/control_flow_branch_maze.axm` на длинном прогоне держится около `0.826x` runtime / `0.814x` total |
| Циклы: `FOR/NEXT`, `WHILE`, `DO/LOOP`, `EXIT FOR`, `EXIT DO`, `CONTINUE FOR`, `CONTINUE WHILE`, `CONTINUE DO` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_CompiledForLoopReportsStepZero()`, `Run_WithBytecodeMode_ReportsStepZeroAsRuntimeFailure()`, `Run_ReturnsSemanticFailureForExitForOutsideFor()`, `Run_ReturnsSemanticFailureForExitDoOutsideDo()`, `Run_ReturnsSemanticFailureForContinueForOutsideFor()`, `Run_ReturnsSemanticFailureForContinueWhileOutsideWhile()`, `Run_ReturnsSemanticFailureForContinueDoOutsideDo()`, acceptance `samples/acceptance/negative/for_step_zero.axm`; после отложенной очистки `BytecodeEvaluationStack` и прямых специализированных comparison-jump путей адресный benchmark `samples/smoke/engine-compare/control_flow_branch_maze.axm` на длинном прогоне держится около `0.826x` runtime / `0.814x` total |
| Процедуры и функции: `SUB`, `FUNCTION`, `RETURN`, `BYREF`, `BYVAL` | да | да | да | да | да | частично | горячо измерено, целевой диапазон | Негативные опоры: `samples/operators/function/fixed_string_parameter_error.axm`, `samples/operators/function/fixed_string_return_error.axm`, `samples/acceptance/negative/byref_requires_variable.axm`, диагностики `MB2026`, `MB2028`, `MB2029` и parser/binder/runtime/acceptance-проверки; добавлены runnable-примеры `samples/operators/sub/byref_scalar_argument.axm`, `samples/operators/sub/byref_record_argument.axm` и `samples/operators/sub/byval_record_copy.axm`; интерпретаторный паритет для `BYREF` простой переменной теперь отдельно закреплён runtime-тестами `Execute_ByRefScalarParameterMutatesCallerVariable()`, `Execute_CompiledByRefScalarParameterMutatesCallerVariable()`, acceptance-подключением `samples/operators/sub/byref_scalar_argument.axm` и живой интеграционной проверкой `MazeChaseAcceptanceSample_MovesPlayerInBothEngines`, но текущая реализация всё ещё сознательно ограничивает `BYREF` простыми переменными и не допускает его для first-class callback-вызовов; адресный benchmark `samples/smoke/engine-compare/routine_closure_pipeline.axm` теперь уже прямо покрывает слой вызовов и держится около `0.861x` runtime / `0.874x` total |
| Замыкания и захват внешних локалей | да | да | да | да | да | частично | горячо измерено, целевой диапазон | Позитивные VM- и acceptance-проверки есть (`capture_outer_local`, `returned_closure`, `shared_closure_state`), а отдельный benchmark `samples/smoke/engine-compare/routine_closure_pipeline.axm` теперь адресно нагружает захват внешних локалей, shared-state closure и вызов function-value; после снятия словарного пути резолва захватов и повторного использования кадров этот слой тоже держится в целевом диапазоне около `0.861x` runtime / `0.874x` total, хотя отдельной адресной отрицательной проверки именно по замыканиям в реестре пока всё ещё не выделено |
| Пользовательские типы: `TYPE`, доступ к полям, массивные поля, формы `TypeName(...)`, методы и `SELF` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Негативные опоры: acceptance-ошибки фиксированных строк и сигнатур (`samples/operators/const/fixed_string_error.axm`, `samples/operators/function/fixed_string_parameter_error.axm`, `samples/operators/function/fixed_string_return_error.axm` с `MB2026`), `samples/acceptance/negative/type_default_non_constant.axm` с `MB2027`, `samples/acceptance/negative/type_literal_mixed_modes.axm` с `MB2031`, `samples/acceptance/negative/type_literal_positional_arity.axm` с `MB2032`, `samples/acceptance/negative/type_method_overload_ambiguous.axm` с `MB2033`, `samples/acceptance/negative/type_constructor_invariant_false.axm` с `MB3005`, плюс общая semantic-проверка `Run_ReturnsSemanticFailureAndDoesNotExecute()`; позитивные execution/acceptance-покрытия есть для `field_access`, `named_literals`, `positional_constructor`, `default_field_values`, `partial_named_literal_defaults`, `structural_equality`, `array_fields`, `array_field_views`, `multidimensional_array_fields`, `fixed_array_fields`, `fixed_multidimensional_array_fields`, `grouped_fields`, `methods_self`, `method_receivers_field_and_array`, `method_receivers_temporary`, `method_overloads`, `constructor_new` и `constructor_invariant`; пустая, именованная и позиционная формы `TypeName(...)`, значения полей по умолчанию, пользовательские `SUB New(...)` с перегрузками по арности и сигнатуре, специальные формы `FUNCTION IsValid() AS BOOLEAN`, структурное сравнение и обычные методы `TYPE` с перегрузкой по имени и сигнатуре отдельно проверены в parser/binder/runtime/`BytecodeVm` и acceptance-цепочке, при этом именованная форма `TypeName(Field := ...)` остаётся литералом записи даже при наличии конструкторов, неименованная форма при наличии хотя бы одного `SUB New(...)` полностью переходит в конструкторный путь, инвариант выполняется только после успешного `SUB New(...)`, а выбор и конструктора, и обычного метода идёт по арности, затем по совместимости типов и затем по точности совпадения без преобразования; manual-покрытие синхронизировано через `docs/manual/type.html` и `docs/manual/data-types-records-and-arrays.html`, а адресный benchmark `samples/smoke/engine-compare/type_record_roundtrip.axm` после перевода `RuntimeRecordValue` на ordinal-слоты и кэша record-полей в VM держится около `0.891x` runtime / `0.888x` total |

## Встроенные функции массивов

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`, `ARRAYRANK`, `ARRAYBOUNDS` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_ArrayMetadataBuiltinsReportRankAndVisibleBounds()`, `Execute_ArrayBoundsReportsUnallocatedArrayState()`, `Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()`, `Execute_CompiledArrayBoundsReportsUnallocatedArrayState()`, `Execute_CompiledPureArrayBuiltinsReportArgumentErrors()`, `Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayMetadataBuiltins()`, `Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/lbound|ubound|arraylength|arrayisallocated|arrayrank|arraybounds/`; усиленный benchmark `samples/smoke/engine-compare/high_rank_metadata.axm` держит metadata-слой около `0.540x` runtime / `0.563x` total |
| `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTAKE`, `ARRAYSKIP` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_ArrayCloneCopiesBoundsAndElementsIntoIndependentArray()`, `Execute_ArraySliceReturnsCompactIndependentSlice()`, `Execute_ArrayConcatReturnsCompactIndependentArray()`, `Execute_CompiledArrayCloneCopiesMultidimensionalVisibleShape()`, `Execute_CompiledArrayShapeBuiltinsSupportExplicitDimensionForMultidimensionalArrays()`, `Execute_CompiledPureArrayBuiltinsReportArgumentErrors()`, `Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()`, `Execute_CompiledArrayConcatReportsIncompatibleMultidimensionalShapes()`, `Run_WithBytecodeMode_UsesBytecodeVmForPureArrayBuiltins()`, `Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-slices.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arrayclone|arrayslice|arrayconcat|arraytake|arrayskip/`; 1D-цепочка переведена на прямое копирование диапазонов, после пакета `mixed` держится около `0.680x` runtime / `0.826x` total, а `string_array_loop` около `0.593x` runtime / `0.736x` total |
| `ARRAYTOREVERSED`, `ARRAYREVERSE`, `ARRAYDISTINCT`, `ARRAYFLATTEN`, `ARRAYRESHAPE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_ArrayToReversedReturnsCompactIndependentArray()`, `Execute_CompiledArrayToReversedSupportsMultidimensionalDimension()`, `Execute_CompiledArrayReverseSupportsMultidimensionalDimension()`, `Execute_CompiledArrayFlattenAndReshape()`, `Execute_CompiledArrayReshapeReportsMismatchedVisibleElementCount()`, `Execute_CompiledPureArrayBuiltinsReportArgumentErrors()`, `Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayFlattenAndReshape()`, `Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-slices.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arraytoreversed|arrayreverse|arraydistinct|arrayflatten|arrayreshape/`; новый адресный benchmark `samples/smoke/engine-compare/array_shape_transform_mesh.axm` на длинном прогоне `20` итераций с `3` прогревами держится около `0.817x` runtime / `0.824x` total, то есть shape-подслой массивов тоже уже в целевом диапазоне |
| `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`, `ARRAYFINDALL`, `ARRAYBINARYSEARCH` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_CompiledArraySearchBuiltins()`, `Execute_CompiledMultidimensionalArrayBinarySearchBuiltinReturnsCoordinateVectorOrEmptyArray()`, `Execute_CompiledArrayCoordinateSearchBuiltinsReturnCoordinateVectorsAndTables()`, `Execute_CompiledArrayContainsAndCountScanMultidimensionalVisibleElements()`, `Execute_CompiledArraySearchBuiltinsReportUnallocatedSource()`, `Run_WithBytecodeMode_UsesBytecodeVmForArraySearchBuiltins()`, `Run_WithBytecodeMode_ReportsArraySearchBuiltinRuntimeError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arraycontains|arrayindexof|arraylastindexof|arraycount|arrayfindall|arraybinarysearch/`; новый адресный benchmark `samples/smoke/engine-compare/array_search_probe.axm` на длинном прогоне `20` итераций с `3` прогревами держится около `0.852x` runtime / `0.857x` total, то есть поисковый подслой массивов сразу оказался в целевом диапазоне |
| `ARRAYFILL`, `ARRAYSORT`, `ARRAYTOSORTED` | да | да | да | да | да | да | не приоритет | Тесты: `Execute_CompiledMutatingArrayBuiltins()`, `Execute_CompiledArrayFillMutatesMultidimensionalVisibleElements()`, `Execute_CompiledArraySearchBuiltins()`, `Execute_CompiledMutatingArrayBuiltinsReportUnallocatedSource()`, `Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()`, `Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForArraySearchBuiltins()`, `Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-diagnostics.html`; sample: `samples/functions/arrayfill|arraysort|arraytosorted/` |
| `ARRAYPUSH`, `ARRAYPOP`, `ARRAYINSERT`, `ARRAYREMOVE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_CompiledMutatingArrayBuiltins()`, `Execute_CompiledMultidimensionalDynamicArrayBuiltinsWorkAlongExplicitDimension()`, `Execute_CompiledMultidimensionalArrayInsertReportsIncompatibleValueShape()`, `Execute_CompiledMutatingArrayBuiltinsReportUnallocatedSource()`, `Execute_CompiledMutatingArrayBuiltinsReportEmptyPop()`, `Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()`, `Run_WithBytecodeMode_UsesBytecodeVmForMutatingArrayBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForMultidimensionalDynamicArrayBuiltins()`, `Run_WithBytecodeMode_ReportsMutatingArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-diagnostics.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arraypush|arraypop|arrayinsert|arrayremove/`; 1D-мутации переведены сначала на прямые сдвиги диапазонов, затем `ARRAYPUSH`/`ARRAYPOP` ещё и на прямой append/remove-last путь; локально это держит `mixed` около `0.694x` runtime / `0.828x` total и `string_array_loop` около `0.560x` runtime / `0.758x` total, то есть дальнейший большой выигрыш уже вероятнее искать в следующем массивном пакете |
| `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_CompiledArrayCallbackBuiltinsUseFunctionValuesAndClosures()`, `Execute_CompiledArrayCallbackBuiltinsTraverseMultidimensionalVisibleElementsAndPreserveMappedShape()`, `Execute_CompiledArrayFilterTraversesMultidimensionalVisibleElementsIntoCompactResult()`, `Execute_CompiledArrayMapPreservesMultidimensionalEmptyVisibleShape()`, `Execute_CompiledArrayCallbackBuiltinsUseBuiltinFunctionValues()`, `Execute_CompiledArrayCallbackBuiltinsReportEmptyFunctionValue()`, `Run_WithBytecodeMode_UsesBytecodeVmForArrayCallbackBuiltins()`, `Run_WithBytecodeMode_ReportsPureArrayBuiltinArgumentError()`; manual: `docs/manual/arrays-functions.html`, `docs/manual/arrays-multidimensional.html`; sample: `samples/functions/arrayevery|arraysome|arrayfilter|arraymap|arrayreduce/`; после пакета с прямыми 1D-ветками, снятием `params`, переиспользованием кадров без локалов, кэшированием прямой ссылки на `BytecodeRoutine` и кэшем function-value внутри внешнего кадра адресный benchmark `samples/smoke/engine-compare/array_callback_pipeline.axm` уже держится около `0.821x` runtime / `0.824x` total, а значит callback-слой не просто ниже паритета, а уже даёт заметный выигрыш |

## Встроенные строковые функции

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `LEN`, `LEFT`, `RIGHT`, `MID` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_CompiledStringSliceAndTrimBuiltins()`, `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-slices-and-length.html`; sample: `samples/functions/len|left|right|mid/`; адресный benchmark `samples/smoke/engine-compare/string_primitive_scan.axm` держится около `0.398x` runtime / `0.476x` total для `LEN`, а широкий string-layer по `string_array_loop_pipeline.axm` остаётся около `0.514x` runtime / `0.718x` total |
| `TRIM`, `LTRIM`, `RTRIM`, `LCASE`, `UCASE`, `REVERSE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_BuiltinTrimVariants()`, `Execute_CompiledStringSliceAndTrimBuiltins()`, `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-trim-and-case.html`; sample: `samples/functions/trim|ltrim|rtrim|lcase|ucase|reverse/`; `TRIM` недавно чинился по корректности, а `LCASE`/`UCASE`/`REVERSE` получили быстрый возврат исходного `RuntimeValue` для no-op-случаев |
| `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `INSTR`, `INSTRREV`, `SUBSTRCOUNT` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_CompiledStringSearchBuiltins()`, `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-search-and-replace.html`; sample: `samples/functions/contains|startswith|endswith|instr|instrrev|substrcount/`; адресный benchmark `samples/smoke/engine-compare/string_primitive_scan.axm` держится около `0.398x` runtime / `0.476x` total для `INSTR`, `INSTRREV` и `SUBSTRCOUNT` |
| `REPLACE`, `INSERT`, `REMOVE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_CompiledStringSearchBuiltins()`, `Execute_CompiledStringFillAndEditBuiltins()`, `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/replace|insert|remove/` |
| `REPEAT`, `SPACE`, `STRING`, `PADLEFT`, `PADRIGHT`, `CENTER` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinStringUsesFirstFillCharacter()`, `Execute_BuiltinStringReportsEmptyFill()`, `Execute_CompiledStringFillAndEditBuiltins()`, `Execute_CompiledStringBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/repeat|space|string|padleft|padright|center/`; VM-путь получил быстрые возвраты для ширины `<= LEN(text)`, а новый адресный benchmark `samples/smoke/engine-compare/string_primitive_scan.axm` держится около `0.398x` runtime / `0.476x` total для `SPACE` и `STRING`; широкий string-layer по `string_array_loop_pipeline.axm` остаётся около `0.514x` runtime / `0.718x` total |
| `ASC`, `CHR` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_CompiledStringCharBuiltins()`, `Execute_CompiledChrReportsInvalidCode()`, `Run_SupportsStringBuiltinsOnBytecodeVm()`, `Run_WithBytecodeMode_ReportsStringBuiltinArgumentError()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/asc|chr/`; новый адресный benchmark `samples/smoke/engine-compare/string_primitive_scan.axm` держится около `0.398x` runtime / `0.476x` total |
| `SPLIT`, `JOIN` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_CompiledStringArrayBuiltins()`, `Execute_CompiledStringArrayBuiltinReportsEmptySeparator()`, `Run_WithBytecodeMode_UsesBytecodeVmForStringArrayBuiltins()`, `Run_WithBytecodeMode_ReportsStringArrayBuiltinArgumentError()`; manual: `docs/manual/string-functions-build-and-split.html`; sample: `samples/functions/split|join/`; `JOIN` получил быстрый путь для одноэлементного массива, а `CallBuiltin` снял лишнюю обвязку для no-location built-in |

## Встроенные функции регулярных выражений

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXCOUNT` | да | да | да | да | да | да | не приоритет | Тесты: `Execute_CompiledRegexScalarBuiltinReportsInvalidArgument()`, `Run_WithBytecodeMode_UsesBytecodeVmForRegexScalarBuiltins()`, `Run_WithBytecodeMode_ReportsRegexScalarBuiltinArgumentError()`; manual: `docs/manual/regex-functions-match-and-extract.html`, `docs/manual/regex-functions-replace-and-count.html`; sample: `samples/functions/regexismatch|regexmatch|regexextract|regexcount/` |
| `REGEXMATCHES`, `REGEXEXTRACTALL`, `REGEXSPLIT` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_CompiledRegexArrayBuiltinReportsInvalidMode()`, `Run_WithBytecodeMode_UsesBytecodeVmForRegexArrayBuiltins()`, `Run_WithBytecodeMode_ReportsRegexArrayBuiltinArgumentError()`; manual: `docs/manual/regex-functions-arrays-and-split.html`, `docs/manual/regex-functions-match-and-extract.html`; sample: `samples/functions/regexmatches|regexextractall|regexsplit/`; адресный benchmark `samples/smoke/engine-compare/regex_array_result_mesh.axm` держится около `0.673x` runtime / `0.695x` total |
| `REGEXREPLACE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_CompiledRegexScalarBuiltinReportsInvalidArgument()`, `Run_WithBytecodeMode_UsesBytecodeVmForRegexScalarBuiltins()`, `Run_WithBytecodeMode_ReportsRegexScalarBuiltinArgumentError()` и адресные проверки в `InterpreterTests`/`BytecodeVmTests`; manual: `docs/manual/regex-functions-replace-and-count.html`; sample: `samples/functions/regexreplace/`; есть недавние no-op-оптимизации и контрольные замеры по `RegexReplaceModes` |

## Встроенные числовые функции

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `INT`, `STR`, `VAL` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_BuiltinStrAndVal()`, `Execute_BuiltinValReturnsZeroForInvalidText()`, `Execute_BuiltinIntReportsOverflowAfterFloor()`, `Execute_CompiledNumericConversionAndRoundingBuiltins()`, `Execute_CompiledNumericBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForAutomaticNumericConversions()`, `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`; manual: `docs/manual/numeric-functions-conversion-and-legacy.html`, `docs/manual/data-types-conversions.html`, `docs/manual/numeric-functions.html`; sample: `samples/functions/int|str|val/`; адресный benchmark `samples/smoke/engine-compare/numeric_function_sweep.axm` держит числовую группу около `0.708x` runtime / `0.711x` total |
| `RND`, `ABS`, `SGN`, `MIN`, `MAX`, `CLAMP`, `SETSIGN` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_BuiltinRndReturnsDoubleInUnitInterval()`, `Execute_MathBuiltinsReturnExpectedOutput(...)`, `Execute_BuiltinSetSignPreservesDecimalType()`, `Execute_CompiledRndReturnsValueInHalfOpenRange()`, `Execute_CompiledNumericMathBuiltins()`, `Execute_CompiledNumericBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForAutomaticNumericConversions()`, `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`, `Run_UsesProvidedRuntimeRandomForRndBuiltin()`; manual: `docs/manual/numeric-functions-core.html`, `docs/manual/numeric-functions.html`; sample: `samples/functions/rnd|abs|sgn|min|max|clamp|setsign/`; числовой слой держится около `0.708x` runtime / `0.711x` total на `numeric_function_sweep.axm`, а `RND` отдельно измерен через `samples/smoke/engine-compare/random_stream_snapshot.axm` и держится около `0.818x` runtime / `0.800x` total |
| `SIN`, `COS`, `TAN`, `ATN`, `ATN2`, `ASIN`, `ACOS`, `ASINH`, `ACOSH`, `ATANH`, `SINH`, `COSH`, `TANH` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_MathBuiltinsReturnExpectedOutput(...)`, `Execute_MathBuiltinsReportDomainErrors(...)`, `Execute_HyperbolicMathBuiltinsReportDoubleOverflow()`, `Execute_CompiledNumericMathBuiltins()`, `Execute_CompiledNumericBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`; manual: `docs/manual/numeric-functions-trigonometry.html`, `docs/manual/numeric-functions.html`; sample: `samples/functions/sin|cos|tan|atn|atn2|asin|acos|asinh|acosh|atanh|sinh|cosh|tanh/`; адресный benchmark `samples/smoke/engine-compare/numeric_function_sweep.axm` показал около `0.708x` runtime / `0.711x` total |
| `SQR`, `CBRT`, `EXP`, `LOG`, `LOG10`, `LOG2`, `POW`, `FLOOR`, `CEIL`, `ROUND`, `FIX` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_MathBuiltinsReturnExpectedOutput(...)`, `Execute_MathBuiltinsReportDomainErrors(...)`, `Execute_MathBuiltinsReportDoubleOverflow()`, `Execute_BuiltinFixDiffersFromIntForNegativeFraction()`, `Execute_CompiledNumericConversionAndRoundingBuiltins()`, `Execute_CompiledNumericMathBuiltins()`, `Execute_CompiledNumericBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForAutomaticNumericConversions()`, `Run_WithBytecodeMode_ReportsNumericBuiltinArgumentError()`; manual: `docs/manual/numeric-functions-roots-rounding.html`, `docs/manual/numeric-functions-conversion-and-legacy.html`, `docs/manual/numeric-functions.html`; sample: `samples/functions/sqr|cbrt|exp|log|log10|log2|pow|floor|ceil|round|fix/`; адресный benchmark `samples/smoke/engine-compare/numeric_function_sweep.axm` показал около `0.708x` runtime / `0.711x` total |

## Встроенные функции даты и времени

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `DATE`, `TIME`, `NOW`, `UTCDATE`, `UTCTIME`, `UTCNOW`, `TIMER` | да | да | да | да | да | да | горячо измерено | Тесты: `Execute_DateTimeBuiltinsReturnCurrentValues()`, `Execute_TimerReturnsNonNegativeNumber()`, `Execute_CompiledDateTimeCurrentBuiltins()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`, `Run_UsesProvidedRuntimeClockForCurrentDateTimeBuiltins()`; manual: `docs/manual/date-time-functions-current.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/date|time|now|utcdate|utctime|utcnow|timer/`; nullary VM-путь переведён на `NoLocation`, а после прокидывания управляемых часов в `ExecutionRequest` адресный benchmark `samples/smoke/engine-compare/current_time_snapshot.axm` держится около `0.497x` runtime / `0.569x` total |
| `DATESERIAL`, `TIMESERIAL`, `YEAR`, `MONTH`, `MONTHNAME`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `WEEKDAYNAME` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_DateTimeSerialAndPartsBuiltins()`, `Execute_DateTimePartsRejectNonCanonicalDates()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`; manual: `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/dateserial|timeserial|year|month|monthname|day|hour|minute|second|weekday|weekdayname/`; `samples/smoke/engine-compare/date_time_calendar_pipeline.axm` держит около `0.651x` runtime / `0.656x` total |
| `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_DateTimeArithmeticAndValidationBuiltins()`, `Execute_DateTimeArithmeticRejectsUnsupportedInterval()`, `Execute_TimeValueRejectsNonCanonicalInput()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`; manual: `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/dateadd|datediff|datepart|datevalue|timevalue|isdate/`; `ISDATE` переведён на прямой `UnaryNoLocation`-путь; добавлены тесты `Execute_DateDiffReportsMixedTemporalKinds()` / `Execute_CompiledDateDiffReportsMixedTemporalKinds()` (смешение времени и даты-времени в `DATEDIFF` с time-интервалами даёт ошибку компиляции `MB2003`) и `Execute_DateAddSupportsQuarterAndYearIntervals()` / `Execute_CompiledDateAddSupportsQuarterAndYearIntervals()` (интервалы `q` и `yyyy`); negative-sample `samples/functions/datediff/mixed_temporal_kinds_error.axm` фиксирует ошибку смешанного временного вида (`MB2003`) |
| `DATEADD`/`DATEDIFF`/`DATEPART` — нативные перегрузки для `DATE`/`TIME`/`DATETIME` (`DateAddDateValue`/`DateAddTimeValue`/`DateAddDateTimeValue`, `DateDiffDateValues`/`DateDiffTimeValues`/`DateDiffDateTimeValues`, `DatePartDateValue`/`DatePartTimeValue`/`DatePartDateTimeValue`) | да | да | да | да | да | да | функционально пройдено | Нативные перегрузки выбираются в Binder по точному совпадению типов аргументов; строковые формы удалены — строковые аргументы отклоняются на этапе привязки (`MB2003`), для строк обязательны `DATESERIAL`/`TIMESERIAL`/`DATEVALUE`/`TIMEVALUE`. Тесты: `Bind_BuiltinDateTimeNativeOverloadsReturnNativeTypes()`, `Bind_BuiltinDateTimeStringValueArgumentsAreRejected()`, `Bind_BuiltinDateTimeMixedTemporalKindsAreRejected()`, `Execute_DateAddAcceptsNativeTemporalValues()` / `Execute_CompiledDateAddAcceptsNativeTemporalValues()`, `Execute_DateDiffAcceptsNativeTemporalValues()` / `Execute_CompiledDateDiffAcceptsNativeTemporalValues()`, `Execute_DatePartAcceptsNativeTemporalValues()` / `Execute_CompiledDatePartAcceptsNativeTemporalValues()`, `Run_SupportsNativeDateTimeBuiltinsOnBytecodeVm()` (без fallback на интерпретатор); негативные: `Execute_DateAddNativeTimeValueReportsKindError()`, `Execute_DateDiffNativeDateValuesReportIntervalKindError()`, `Execute_DatePartNativeTimeValueReportsKindError()`, `Execute_DateDiffNativeMixedTemporalKindsReportsSameKindError()` и их `Execute_Compiled*`-пары; manual: `docs/manual/date-time-functions-arithmetic-and-validation.html` (секции нативных форм и диагностики); sample: `samples/functions/dateadd/typed_temporal_values.axm` + acceptance-тест `DateAddTypedTemporalValuesSample_RunsSuccessfully()`; для поддержки `DIM d AS DATE` лексер и парсер дополнены ключевыми словами `DATE`/`TIME`/`DATETIME` |
| `UTCOFFSET`, `TOUTC`, `FROMUTC`, `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_DateTimeUtcBuiltins()`, `Execute_DateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_DateTimeUtcBuiltinsSupportSeasonalNamedTimeZones()`, `Execute_DateTimeCalendarHelpers()`, `Execute_CompiledDateTimeCurrentBuiltins()`, `Execute_CompiledDateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`; manual: `docs/manual/date-time-functions-time-zones.html`, `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/utcoffset|toutc|fromutc|isleapyear|daysinmonth|startofmonth|endofmonth|startofyear|endofyear/`; `samples/smoke/engine-compare/date_time_calendar_pipeline.axm` держит около `0.651x` runtime / `0.656x` total; добавлены VM-тесты DST-переходов `Execute_CompiledToUtcReportsInvalidLocalDateTimeDuringSkippedTransition()` и `Execute_CompiledUtcOffsetReportsAmbiguousLocalDateTimeDuringRepeatedTransition()` (паритет с Interpreter по `IsInvalidTime`/`IsAmbiguousTime`) |

## Встроенные функции файловой системы и файлового ввода-вывода

| Функции | VM | Автотесты | Sample | Manual | Сквозной VM | Негативный паритет | Профиль | Примечание |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ISOPEN`, `FILEPATH`, `FILEMODE`, `FILEPOS`, `FILELENGTH`, `LINENUMBER`, `EOF` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_FileStateBuiltins()`, `Execute_FilePositionAndLengthForInput()`, `Execute_OpenReadLineAndEof()`, `Execute_CompiledOpenInputFileProgram()`, `Execute_CompiledOpenFileReportsClosedFileRead()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`, `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`, `docs/manual/data-types-strings-and-files.html`; sample: `samples/functions/isopen|filepath|filemode|filepos|filelength|linenumber|eof/`; `ISOPEN` переведён на прямой `UnaryNoLocation`-путь, а новый адресный benchmark `samples/smoke/engine-compare/open_file_stream_cycle.axm` держится около `0.813x` runtime / `0.834x` total |
| `READTOEND`, `READTEXT`, `READLINE`, `READLINES`, `SKIPLINE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_ReadToEndReturnsRemainingText()`, `Execute_ReadTextCountsCharactersWhilePositionStaysByteBased()`, `Execute_ReadLinesReadsUpToCountAndAdvancesPosition()`, `Execute_ReadLinesReturnsEmptyArrayForZeroCountAndEndOfFile()`, `Execute_CompiledOpenInputFileProgram()`, `Execute_CompiledOpenFileReportsClosedFileRead()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`, `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`; sample: `samples/functions/readtoend|readtext|readline|readlines|skipline/`; новый адресный benchmark `samples/smoke/engine-compare/open_file_stream_cycle.axm` держится около `0.813x` runtime / `0.834x` total |
| `WRITE`, `WRITELINE`, `NEWLINE`, `FLUSH`, `SEEKFILE`, `REWIND` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_FilePositionAndSeekForOutput()`, `Execute_RewindMovesFileBackToBeginning()`, `Execute_CompiledOpenOutputAndAppendFileProgram()`, `Execute_SeekFileReportsAppendMode()`, `Execute_RewindReportsAppendMode()`, `Execute_CompiledOpenFileReportsClosedFileRead()`, `Run_WithBytecodeMode_UsesBytecodeVmForOpenTextFile()`, `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-open-files.html`, `docs/manual/open.html`, `docs/manual/close.html`; sample: `samples/functions/write|writeline|newline|flush|seekfile|rewind/`; новый адресный benchmark `samples/smoke/engine-compare/open_file_stream_cycle.axm` держится около `0.813x` runtime / `0.834x` total |
| `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `READFILE`, `READALLLINES`, `WRITEFILE`, `APPENDFILE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid()`, `Execute_BuiltinFileSystemReadsAllLines()`, `Execute_BuiltinFileSystemReadsAllLinesFromEmptyFile()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/fileexists|direxists|pathexists|readfile|readalllines|writefile|appendfile/`; базовый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total, а отдельный длинный `samples/smoke/engine-compare/temp_writefile_probe.axm` держится около `0.923x` runtime / `0.923x` total; отдельный byte-cache для `WRITEFILE` проверялся и отклонён как ложный путь, потому что уводил этот же probe назад примерно к `0.976x` / `0.977x` |
| `FILESIZE`, `FILEDATE`, `FILECREATEDATE`, `FILEACCESSEDDATE`, `DIRDATE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-paths.html`; sample: `samples/functions/filesize|filedate|filecreatedate|fileaccesseddate|dirdate/`; базовый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total, а отдельный `samples/smoke/engine-compare/temp_filesize_probe.axm` держится около `0.884x` runtime / `0.885x` total |
| `GETFILES`, `GETDIRS`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `EXTNAME`, `FULLPATH`, `PATHROOT`, `ISABSOLUTEPATH` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemPathHelpersReturnExpectedValues()`, `Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/getfiles|getdirs|combinepath|filename|dirname|extname|fullpath|pathroot|isabsolutepath/`; базовый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total; отдельный `PATHROOT`-срез `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm` держится около `0.724x` runtime / `0.736x` total, `temp_storage_roundtrip.axm` на длинном контроле `40/5` держится около `0.960x` runtime / `0.960x` total, а ещё более узкий `samples/smoke/engine-compare/temp_path_creation_probe.axm` держится около `0.998x` runtime / `0.996x` total |
| `MAKEDIR`, `DELETEFILE`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEDIR`, `DELETEDIRRECURSIVE`, `COPYDIR`, `MOVEDIR`, `RENAMEDIR` | да | да | да | да | да | да | горячо измерено, рабочий паритет | Тесты: `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles()`, `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories()`, `Execute_BuiltinFileSystemReportsCopyFileMissingSource()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Execute_CompiledDeleteFileReturnsFalseForMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`, `Run_WithBytecodeMode_ReportsCopyFileMissingSourceFromVm()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/makedir|deletefile|copyfile|movefile|renamefile|deletedir|deletedirrecursive|copydir|movedir|renamedir/`; после добавления прямого fast-path в `BytecodeVm` адресный benchmark `samples/smoke/engine-compare/file_management_roundtrip.axm` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-085758.txt` держится около `0.969x` runtime / `0.969x` total, а очищенный `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm` после последовательных адресных дожимов `CURRENTDIR`, `CHANGEDIR`, `DELETEFILE` и `DELETEDIRRECURSIVE` держится около `0.816x` runtime / `0.818x` total для cleanup/navigation-подслоя; широкий `file_management_roundtrip.axm` на отдельном контрольном прогоне `20/3` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-105022.txt` при этом остаётся только около `0.996x` runtime / `0.996x` total; отдельной правкой fast-path для `DELETEFILE` выровнен с интерпретатором на повторном удалении отсутствующего файла (`FALSE`); попытка убрать предварительные `Exists` у `COPYFILE` / `MOVEFILE` / `RENAMEFILE` и соседних операций отдельно проверялась и отклонена как ложный путь |
| `CHANGEDIR`, `CURRENTDIR`, `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `ISREADONLY`, `ISHIDDEN`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE`, `MAKETEMPDIR` | да | да | да | да | да | да | горячо измерено, рабочий паритет | Тесты: `Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace()`, `Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()`, `Execute_BuiltinFileSystemReportsMissingDirectoryForChangeDir()`, `Execute_BuiltinFileSystemCreatesTemporaryFileAndDirectory()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/changedir|currentdir|dirsize|freespace|totalfreespace|totalspace|isreadonly|ishidden|drivename|driveformat|drivetype|maketempfile|maketempdir/`; адресные benchmark: `samples/smoke/engine-compare/file_management_roundtrip.axm` после добавления прямого fast-path держится около `0.969x` runtime / `0.969x` total для `CHANGEDIR`/`CURRENTDIR`, а после адресных дожимов `CURRENTDIR`, `CHANGEDIR`, `DELETEFILE` и `DELETEDIRRECURSIVE` отдельный `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm` держится около `0.816x` runtime / `0.818x` total и по профилю `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-103832.txt` сверху показывает именно `CURRENTDIR`, `CHANGEDIR`, `DELETEDIRRECURSIVE`, `DELETEFILE`; широкий `file_management_roundtrip.axm` на контрольных прогонах `20/3` пока не поднялся выше почти паритета, так что выигрыш честно подтверждён именно на узком cleanup/navigation-срезе; `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm` держится около `0.724x` runtime / `0.736x` total для `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE` и `PATHROOT`; `samples/smoke/engine-compare/temp_storage_roundtrip.axm` на длинном контроле `40/5` держится около `0.960x` runtime / `0.960x` total; после прямого fast-path для `DIRSIZE` отдельный `samples/smoke/engine-compare/temp_dirsize_probe.axm` держится около `0.923x` runtime / `0.923x` total, а смешанный `samples/smoke/engine-compare/temp_size_cleanup_cycle.axm` на длинном контроле `40/5` держится около `0.983x` runtime / `0.982x` total; старый mixed `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` теперь полезен скорее как интеграционный срез и держится около `0.921x` runtime / `0.921x` total |

## Что ещё нужно довести сверх текущих галочек

Это уже не про реализацию самих функций, а про качество сопровождения покрытия.

1. Нужна точечная досверка строк `Негативный паритет`.
   Сейчас для многих групп стоит `частично`, потому что есть общий отрицательный набор и стабильная паритетная проверка диагностики, но не всегда есть явная адресная привязка к каждой отдельной функции или оператору.

2. Нужна точечная досверка строк `Manual` и `Sample` на уровне отдельных имён.
   По крупным группам покрытие очевидно, но для рабочего реестра полезно дойти до режима, где каждая строка ссылается на конкретную страницу manual и конкретную папку в `samples/`.

3. Нужен полуавтоматический аудит на рассинхронизацию.
   Следующий полезный шаг после появления этого реестра — сделать небольшой служебный сценарий, который сравнивает:
   - список visible built-in из `src/Axiom.Semantics/Symbols/Builtins/`,
   - наличие VM-диспетчеризации,
   - наличие sample-папок,
   - наличие manual-страниц или упоминаний в индексе.

4. Нужно поддерживать столбец `Профиль` в живом состоянии.
   Не все функции надо ускорять, но после каждой оптимизационной задачи надо явно переносить сюда новый статус и его положение на шкале `<= 0.900x`, `0.900x .. 1.000x` или `> 1.000x`.

5. Нужно постепенно разукрупнять слишком широкие строки.
   Этот первый выпуск реестра нарочно группирует близкие функции, чтобы быстро получить рабочую картину. По мере работы полезно делить самые крупные строки на более точные, когда именно они становятся предметом отдельной задачи.



## Обновление реестра - 2026-05-07 - TRY/CATCH/FINALLY

Статус пользовательской формы `TRY ... CATCH ... FINALLY ... END TRY`:
- `VM`: нет;
- `Автотесты`: да, интерпретаторные проверки добавлены в `tests/Axiom.Runtime.Tests/InterpreterTests.cs`;
- `Sample`: да, `samples/operators/try/handled_runtime_error.axm`;
- `Manual`: да, `docs/manual/try.html`;
- `Сквозной VM`: нет;
- `Негативный паритет`: нет;
- `Профиль`: не приоритет.

Примечание: первый исполняемый срез реализован только в интерпретаторе. Он проверяет перехват runtime-ошибки, поля `ERROR.Code`, `ERROR.Line`, `ERROR.Column`, выполнение `FINALLY` после обычного тела, после обработанной ошибки и перед необработанной ошибкой. `BytecodeVm` и сквозной паритет остаются следующим этапом.

Дополнение 2026-05-07: IDE получила временный пользовательский откат для обычного запуска. Если выбран режим `Bytecode`, но выполнение останавливается только на `MB4001`, оболочка повторяет запуск через интерпретатор и помечает результат как interpreter fallback. Это не является VM-покрытием: в строке `TRY/CATCH/FINALLY` по-прежнему остаются `VM: нет` и `Сквозной VM: нет`, а задача реализации байткодного пути остаётся открытой.
## Обновление реестра - 2026-05-08 - ERROR

Статус пользовательского оператора `ERROR`:
- `VM`: нет;
- `Автотесты`: да, lexer/parser/binder/runtime/execution/IDE проверки добавлены или обновлены;
- `Sample`: да, `samples/operators/error/user_code_catch.axm`;
- `Manual`: да, `docs/manual/error.html` и `docs/manual/error-handling.html`;
- `Сквозной VM`: нет;
- `Негативный паритет`: нет;
- `Профиль`: не приоритет.

Примечание: `ERROR` реализован в интерпретаторе и использует тот же путь диагностики времени выполнения, что системные ошибки. В строгом байткодном пути `ERROR` и `TRY/CATCH/FINALLY` остаются неподдержанными и не считаются покрытием `BytecodeVm`. Acceptance-аудит временно исключает `samples/operators/error/` до реализации VM-поддержки.
## Обновление реестра - 2026-05-08 - VM-поддержка TRY/CATCH/FINALLY и ERROR

Статус форм `TRY ... CATCH ... FINALLY ... END TRY` и `ERROR` после байткодного среза:
- `VM`: да;
- `Автотесты`: да, обновлены acceptance-проверки `TryCatchAcceptanceSample_RunsOnBytecodeVm`, `ErrorAcceptanceSample_RunsOnBytecodeVm` и общий положительный bytecode-аудит samples;
- `Sample`: да, `samples/operators/try/handled_runtime_error.axm` и `samples/operators/error/user_code_catch.axm`;
- `Manual`: да, `docs/manual/try.html`, `docs/manual/error.html`, `docs/manual/error-handling.html`;
- `Сквозной VM`: да, оба sample теперь обязаны проходить через `ExecutionMode.Bytecode` без interpreter fallback;
- `Негативный паритет`: частично, базовая runtime-ошибка и пользовательская ошибка проверяются, ранние выходы через `FINALLY` требуют отдельной досверки;
- `Профиль`: не приоритет.

Примечание: VM-путь использует служебный стек обработчиков внутри `BytecodeCallFrame`. Обработанная диагностика удаляется из итогового результата и превращается в значение встроенного типа `ERROR`, поэтому контракт `err.Code AS STRING`, `err.Message AS STRING`, `err.Line AS INTEGER`, `err.Column AS INTEGER` совпадает с интерпретатором.

## Обновление реестра - 2026-05-08 - RETURN через FINALLY в VM

Дополнение к строке `TRY/CATCH/FINALLY`:
- `RETURN` из `TRY`/`CATCH` теперь выполняет `FINALLY` в `BytecodeVm` перед фактическим выходом из процедуры;
- автоматическая проверка: `Run_WithBytecodeMode_RunsFinallyBeforeFunctionReturn`;
- оставшийся отрицательный зазор: `EXIT FOR` и `EXIT DO` через `FINALLY` ещё требуют отдельного lowering-пути.

## Обновление реестра - 2026-05-08 - EXIT FOR/EXIT DO через FINALLY в VM

Дополнение к строке `TRY/CATCH/FINALLY`:
- `EXIT FOR` и `EXIT DO` теперь не обходят `FINALLY`, когда переход покидает активный `TRY`/`CATCH`;
- автоматические проверки: `Run_WithBytecodeMode_RunsFinallyBeforeExitForLeavesTry`, `Run_WithBytecodeMode_DoesNotRunFinallyBeforeExitForInsideSameTry`, `Run_WithBytecodeMode_RunsFinallyBeforeExitDoLeavesTry`;
- статус отрицательного паритета для ранних выходов через `FINALLY`: базово закрыт для `RETURN`, `EXIT FOR`, `EXIT DO`; остаётся отдельная досверка вложенных обработчиков и ошибок внутри `FINALLY`.

## Обновление реестра - 2026-05-08 - вложенные FINALLY и IDE fallback

Дополнение к строке `TRY/CATCH/FINALLY`:
- вложенные `FINALLY` проверены на порядок выполнения изнутри наружу;
- ошибка внутри `FINALLY` проверена на перехват внешним `CATCH`;
- временный IDE fallback по `MB4001` снят, поэтому обычный запуск `TRY`/`ERROR` в режиме `Bytecode` должен оставаться на `BytecodeVm`;
- автоматические проверки: `Run_WithBytecodeMode_RunsNestedFinallyBlocksInsideOut`, `Run_WithBytecodeMode_AllowsOuterCatchToHandleErrorFromFinally`, `Run_WithBytecodeMode_RunsTryBlockOnBytecodeVm`.

## Обновление реестра - 2026-05-08 - ручной IDE-smoke TRY/ERROR

Дополнение к строкам `TRY/CATCH/FINALLY` и `ERROR`:
- `samples/operators/error/user_code_catch.axm` вручную открыт и выполнен в `Axiom.Ide`; ожидаемый вывод `APP001`, `Manual failure`, `after` подтверждён;
- `samples/operators/try/handled_runtime_error.axm` вручную открыт и выполнен в `Axiom.Ide`; ожидаемый вывод с `MB3001`, строкой `4`, сообщением `Деление на ноль.`, `cleanup` и `after` подтверждён;
- IDE-smoke статус: подтверждён вручную 2026-05-08.

## Обновление реестра - 2026-05-08 - ERROR err

Дополнение к строке `ERROR`:
- `VM`: да, форма `ERROR err` понижается в режим `RaiseError` для существующего значения `ERROR`;
- `Автотесты`: да, добавлены `Bind_ErrorStatementAcceptsCaughtRuntimeError`, `Execute_ErrorStatementRethrowsCaughtRuntimeError`, `Run_WithBytecodeMode_RethrowsCaughtRuntimeError`, `ErrorRethrowAcceptanceSample_RunsOnBytecodeVm`;
- `Sample`: да, `samples/operators/error/rethrow_caught_error.axm`;
- `Manual`: да, обновлены `docs/manual/error.html`, `docs/manual/error-handling.html`, `docs/manual/try.html` и поисковый индекс;
- `Сквозной VM`: да, acceptance sample обязан выполняться через `ExecutionMode.Bytecode` без interpreter fallback;
- `Негативный паритет`: частично, типовое правило `ERROR 10` остаётся проверенным, а `ERROR err, message` сознательно не вводится как отдельная форма;
- `Профиль`: не приоритет.

Примечание: `ERROR err` сохраняет исходные `Code`, `Message`, `Line` и `Column`, поэтому внешний `CATCH` видит место первоначальной ошибки, а не строку повторного проброса.
## Обновление реестра - 2026-05-08 - SELECT CASE граничные samples

Дополнение к строке `IF ... ELSE ... END IF`, `SELECT CASE`:
- `Sample`: добавлены `samples/control-flow/select_case/boolean_values.axm`, `samples/control-flow/select_case/first_match.axm`, `samples/control-flow/select_case/no_match_no_else.axm` и negative sample `samples/acceptance/negative/select_case_invalid_types.axm`;
- `Автотесты`: добавлены parser-проверки пустого `CASE`, позднего `CASE` после `CASE ELSE`, `CASE IS` без оператора; binder-проверка boolean-селекторов; execution-проверки первой подошедшей ветви и отсутствия совпадения без `CASE ELSE`; acceptance-проверки новых samples;
- `Сквозной VM`: положительные samples входят в `ExecutionMode.Bytecode` без interpreter fallback;
- `Manual`: обновлены ссылки в `docs/manual/select-case.html` и `docs/manual/examples.html`.

## Обновление реестра - 2026-05-08 - INKEY()

Дополнение к встроенным функциям ввода:
- `VM`: да, `INKEY()` выполняется в `BytecodeVm` через общий dispatch встроенных функций;
- `Интерпретатор`: да, функция читает клавишу через `IRuntimeHost.ReadKey()`;
- `Автотесты`: да, добавлены проверки типа `STRING`, ошибки аргументов, интерпретатора, `BytecodeVm`, execution pipeline без fallback и IDE-очереди и acceptance-запуска sample;
- `Sample`: да, `samples/functions/inkey/poll_keys.axm`;
- `Manual`: да, `docs/manual/inkey.html`, навигация manual, поиск и обзор ввода обновлены;
- `Сквозной VM`: да, `Run_WithBytecodeMode_UsesBytecodeVmForInKey` закрепляет `ExecutionMode.Bytecode` без interpreter fallback;
- `Профиль`: не приоритет, функция относится к интерактивному вводу и не является производительным горячим участком.
## Обновление реестра - 2026-05-08 - ON KEY

Статус операторного слоя клавиатурных событий:
- VM: да, ON KEY понижается в StatementBoundary, RegisterKeyHandler и UnregisterKeyHandler, после чего исполняется в BytecodeVm;
- Интерпретатор: да, обработчики работают через общий буфер клавиатуры и тот же runtime-host, что и INKEY();
- Автотесты: да, добавлены parser-, binder-, interpreter-, BytecodeVm-, execution-pipeline- и acceptance-проверки, включая сценарии перехвата совпавшей клавиши и сохранения несовпавшей клавиши для INKEY();
- Sample: да, samples/operators/on-key/left_handler.axm;
- Manual: да, docs/manual/on-key.html, а docs/manual/inkey.html дополнен описанием совместной работы с ON KEY;
- Сквозной VM: да, Run_WithBytecodeMode_UsesBytecodeVmForOnKey и OnKeyAcceptanceSample_RunsOnBytecodeVm закрепляют путь ExecutionMode.Bytecode без interpreter fallback;
- Негативный паритет: да, ошибка обработчика `MB2034` теперь подтверждена отдельным runnable sample `samples/operators/on-key/invalid_handler_parameter.axm` и execution-проверкой одинаковой семантической диагностики в режимах `Interpreter` и `Bytecode`; отдельная досверка неизвестных имён и сложных комбинаций отключения остаётся возможным расширением, но уже не обязательным хвостом паритета;
- Профиль: не приоритет, оператор относится к интерактивному поведению между операторами, а не к вычислительно горячим участкам.
## Обновление реестра - 2026-05-09 - KEYDOWN()

Форма: встроенная функция `KEYDOWN(keyText)`.

Статус покрытия:
- VM: да, `KEYDOWN()` выполняется в `BytecodeVm` через общий dispatch встроенных функций;
- Интерпретатор: да, функция читает текущее состояние клавиши через `IKeyboardInputProvider.IsKeyDown(...)`;
- Автотесты: да, добавлены binder-, interpreter-, BytecodeVm-, execution-pipeline-, IDE-service- и acceptance-проверки;
- Runnable sample: да, `samples/functions/keydown/held_modifiers.axm`;
- Manual: да, `docs/manual/keydown.html`, навигация и поисковый индекс обновлены;
- Parity: да, `KEYDOWN()` покрыт в интерпретаторе и VM без interpreter fallback;
- Производительность: функциональное покрытие, отдельный производительный сценарий не требуется для этого первого среза.

Пользовательский договор: `KEYDOWN()` не читает очередь клавиш и не влияет на `INKEY()` или `ON KEY`; сочетания клавиш задаются логическими выражениями Axiom.
## Обновление реестра - 2026-05-09 - канонические имена клавиш и алиасы

Дополнение к строкам `INKEY()`, `ON KEY` и `KEYDOWN()`:
- VM: да, `BytecodeVm` использует тот же `KeyboardInputNormalizer`, что и интерпретатор;
- Автотесты: да, добавлены проверки нормализатора, interpreter/VM-сценарии для `KEYDOWN("CONTROL")`, `KEYDOWN("PAGE UP")`, `KEYDOWN("ESCAPE")` и `ON KEY("ESCAPE")` при событии `ESC`;
- Sample: да, добавлен runnable sample `samples/functions/keydown/canonical_aliases.axm`;
- Manual: да, таблица канонических имён и алиасов размещена в `docs/manual/keydown.html#key-names`, страницы `INKEY()` и `ON KEY` ссылаются на неё;
- Сквозной VM: да, `KeyDownCanonicalAliasesAcceptanceSample_RunsOnBytecodeVm` закрепляет выполнение sample через `ExecutionMode.Bytecode` без interpreter fallback;
- Профиль: не приоритет, нормализация относится к интерактивному вводу.

Примечание: алиасы расширяют пользовательский договор строковых имён, но не добавляют новые opcode и не меняют lowering. Это общий слой сравнения имён на границе клавиатурного ввода.

## Обновление реестра - 2026-05-09 - KEYPRESS()

Форма: встроенная функция `KEYPRESS(keyText)`.

Статус покрытия:
- VM: да, `KEYPRESS()` выполняется в `BytecodeVm` через общий dispatch встроенных функций;
- Интерпретатор: да, функция использует общий клавиатурный буфер и `KeyboardInputNormalizer`;
- Автотесты: да, добавлены semantic-, interpreter-, BytecodeVm-, execution-pipeline- и acceptance-проверки;
- Runnable sample: да, `samples/functions/keypress/single_press.axm`;
- Manual: да, добавлена `docs/manual/keypress.html`, обновлены навигация, поиск и обзор ввода;
- Сквозной VM: да, `Run_WithBytecodeMode_UsesBytecodeVmForKeyPress` и `KeyPressAcceptanceSample_RunsOnBytecodeVm` закрепляют путь `ExecutionMode.Bytecode` без interpreter fallback;
- Профиль: не приоритет, функция относится к интерактивному вводу.

Пользовательский договор: `KEYPRESS()` возвращает `TRUE` один раз на одно подходящее событие нажатия и удаляет это событие из очереди; неподходящие клавиши сохраняют порядок и остаются доступны для `INKEY()`, `KEYPRESS()` или `ON KEY`.

## Обновление реестра - 2026-05-09 - ON KEY("ANY")

Дополнение к строке `ON KEY`:
- VM: да, запасной обработчик `"ANY"` выбирается в `BytecodeVm`, если для события нет точного обработчика клавиши;
- Автотесты: да, добавлены проверки интерпретатора и `BytecodeVm` для обработки любой клавиши и приоритета точного обработчика над `"ANY"`;
- Sample: да, `samples/operators/on-key/any_key_handler.axm`;
- Manual: да, обновлены `docs/manual/on-key.html`, `docs/manual/keydown.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `OnKeyAnyAcceptanceSample_RunsOnBytecodeVm` выполняет sample в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: не требуется, потому что срез добавляет положительную форму выбора обработчика без новой диагностики;
- Профиль: не приоритет.

Примечание: `"ANY"` остаётся псевдоименем только для `ON KEY`. `KEYDOWN("ANY")` и `KEYPRESS("ANY")` не получают новой специальной семантики в этом срезе.

Уточнение: обработчик `"ANY"` вызывается для всех доступных событий, которые диспетчер `ON KEY` вычитывает на границе оператора и для которых нет точного обработчика. Это сохраняет уже существующую модель `ON KEY`, где диспетчер может обработать несколько накопленных событий подряд.

## Обновление реестра - 2026-05-09 - параметр имени клавиши в ON KEY

Дополнение к строке `ON KEY`:
- VM: да, `RegisterKeyHandler` получил флаг передачи имени клавиши, а `BytecodeVm` вызывает обработчик с аргументом `STRING`, когда это требуется сигнатурой;
- Автотесты: да, добавлены проверки binder, интерпретатора и `BytecodeVm` для обработчика `SUB(keyName AS STRING)`;
- Sample: да, `samples/operators/on-key/handler_key_name.axm`;
- Manual: да, обновлены `docs/manual/on-key.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `OnKeyHandlerKeyNameAcceptanceSample_RunsOnBytecodeVm` выполняет sample в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: да, binder отклоняет обработчик с параметром не типа `STRING`, а bytecode-валидатор проверяет сигнатуру по флагу;
- Профиль: не приоритет.

Примечание: форма не меняет очередь клавиш и не добавляет новый opcode. Меняется только операнд существующей инструкции регистрации обработчика.

## Обновление реестра - 2026-05-09 - модифицированные клавиши через KEYPRESS и KEYDOWN

Дополнение к строкам `KEYPRESS()` и `KEYDOWN()`:
- VM: без нового opcode; существующая модель уже принимает состояние удержания через `IsKeyDown(...)` и события через `ReadKey()`;
- Автотесты: да, добавлены IDE-тесты маршрутизатора и mapper-слоя для `CTRL`/`ALT`-клавиш;
- Sample: да, `samples/functions/keypress/modified_keypress.axm`;
- Manual: да, обновлены `docs/manual/keypress.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `ModifiedKeyPressAcceptanceSample_RunsOnBytecodeVm` выполняет sample в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: не требуется, потому что срез не добавляет новую диагностику;
- Профиль: не приоритет.

Примечание: строковые сочетания вида `"CTRL+LEFT"` намеренно не вводятся. Проверка комбинации остаётся обычным выражением Axiom: `KEYDOWN("CTRL") AND KEYPRESS("LEFT")`.

## Обновление реестра - 2026-05-09 - строковые аккорды клавиш

Дополнение к строкам `KEYDOWN()`, `KEYPRESS()` и `ON KEY`:
- VM: да, `BytecodeVm` использует общий `KeyboardInputNormalizer.NormalizeChord(...)` для `KEYDOWN()`, `KEYPRESS()` и регистрации `ON KEY`;
- Автотесты: да, добавлены проверки нормализации аккордов, interpreter- и VM-сценарии для `KEYPRESS("CTRL+ALT+DELETE")`, `KEYDOWN("CTRL+F+A+X")` и `ON KEY("CTRL+LEFT")`;
- Sample: да, `samples/functions/keypress/modified_keypress.axm` обновлён под `KEYPRESS("CTRL+LEFT")`, добавлен `samples/operators/on-key/chord_handler.axm`;
- Manual: да, обновлены `docs/manual/keypress.html`, `docs/manual/keydown.html`, `docs/manual/on-key.html`, обзор ввода и поисковый индекс;
- Сквозной VM: да, `ModifiedKeyPressAcceptanceSample_RunsOnBytecodeVm` и `OnKeyChordAcceptanceSample_RunsOnBytecodeVm` выполняют samples в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: не требуется, потому что пустые части аккорда игнорируются, а неизвестные имена остаются обычными строковыми клавишами по существующему договору;
- Профиль: не приоритет.

## Обновление реестра - 2026-05-09 - режимы ON KEY ON / STOP / OFF

Дополнение к строке `ON KEY`:
- VM: да, добавлены инструкции `PauseKeyHandler` и `ResumeKeyHandler`, а `BytecodeVm` хранит флаг активности регистрации обработчика;
- Автотесты: да, добавлены parser-, binder-, interpreter- и VM-проверки для `ON KEY(...), ON` и `ON KEY(...), STOP`;
- Sample: да, `samples/operators/on-key/stop_resume_handler.axm`;
- Manual: да, обновлены `docs/manual/on-key.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm` выполняет sample в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: не требуется, потому что `ON` и `STOP` без существующей регистрации являются безопасной no-op-формой;
- Профиль: не приоритет.

Примечание: `STOP` в составе `ON KEY(...), STOP` не останавливает программу. Это режим паузы конкретного обработчика клавиши.

## Обновление реестра - 2026-05-09 - MOUSEX(), MOUSEY() и MOUSEBUTTON()

Формы: встроенные функции MOUSEX(), MOUSEY() и MOUSEBUTTON(buttonNumber).

Статус покрытия:
- VM: да, все три функции выполняются в BytecodeVm через общий dispatch встроенных функций;
- Интерпретатор: да, функции читают состояние мыши через IMouseInputProvider;
- Автотесты: да, добавлены semantic-, interpreter-, BytecodeVm-, execution-pipeline-, IDE-service- и IDE-router-проверки;
- Runnable sample: да, samples/functions/mousex/current_position.axm, samples/functions/mousey/current_position.axm, samples/functions/mousebutton/button_states.axm;
- Manual: да, добавлены docs/manual/mousex.html, docs/manual/mousey.html, docs/manual/mousebutton.html, обновлены навигация, поиск и обзор ввода;
- Сквозной VM: да, Run_WithBytecodeMode_UsesBytecodeVmForMouseFunctions, MouseCoordinatesAcceptanceSample_RunsOnBytecodeVm и MouseButtonAcceptanceSample_RunsOnBytecodeVm закрепляют путь ExecutionMode.Bytecode без interpreter fallback;
- Профиль: не приоритет, функции относятся к интерактивному опросу состояния, а не к вычислительно горячим участкам.

Примечание: это только опросный срез мыши. Очередь mouse-событий, колесо и отдельные обработчики пока не входят в покрываемый пользовательский договор.
## Обновление реестра - 2026-05-09 - MOUSEPRESS()

- MOUSEPRESS(buttonNumber) теперь покрыт BytecodeVm и interpreter на одном договоре очереди mouse-событий;
- автоматические проверки добавлены в 	ests/Axiom.Semantics.Tests, 	ests/Axiom.Runtime.Tests, 	ests/Axiom.Execution.Tests и 	ests/Axiom.Ide.Tests;
- runnable sample добавлен: samples/functions/mousepress/button_presses.axm;
- manual-покрытие добавлено: docs/manual/mousepress.html, плюс обновлены docs/manual/data-and-input.html, docs/manual/scripts/manual.js и docs/manual/scripts/search-index.js;
- область относится к пользовательскому вводу и не является вычислительно горячей, поэтому в реестре помечается как в первую очередь функционально покрытая.
## Обновление реестра - 2026-05-09 - ON MOUSE

Новая область: оператор `ON MOUSE(buttonNumber), ...`.

Состояние покрытия:
- VM: да, `BytecodeVm` поддерживает регистрацию, снятие, паузу и возобновление обработчиков через инструкции `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler` и `ResumeMouseHandler`;
- Автотесты: да, добавлены parser-, binder-, interpreter-, BytecodeVm-, execution-pipeline- и acceptance-проверки для регистрации, передачи параметров события, паузы и возобновления;
- Runnable sample: да, `samples/operators/on-mouse/left_button_handler.axm`;
- Manual: да, добавлена страница `docs/manual/on-mouse.html`, обновлены навигация и поисковый индекс;
- Сквозной VM: да, `Run_WithBytecodeMode_UsesBytecodeVmForOnMouse` и `OnMouseAcceptanceSample_RunsOnBytecodeVm` подтверждают выполнение через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: да, binder отклоняет недопустимую сигнатуру обработчика `ON MOUSE` через `MB2035`, а bytecode-валидатор проверяет флаг передачи события;
- Профиль: не приоритет, потому что `ON MOUSE` является интерактивным событийным слоем, а не вычислительно горячим циклом.

Примечание: `ON MOUSE` не добавляет новый host-контракт. Он использует ту же очередь `ReadMouseButtonPress()`, что и `MOUSEPRESS()`.

## Обновление реестра - 2026-05-09 - ON MOUSE UP и MOVE

Дополнение к строке `ON MOUSE`:
- VM: да, `BytecodeVm` различает `Press`, `Release` и `Move` через операнд вида события в инструкциях регистрации, снятия, паузы и возобновления обработчика;
- Автотесты: да, добавлены parser-, binder-, interpreter-, BytecodeVm-, IDE-router/service- и execution-проверки для `ON MOUSE(1, "UP")` и `ON MOUSE("MOVE")`;
- Runnable sample: да, `samples/operators/on-mouse/release_move_handler.axm`;
- Manual: да, обновлены `docs/manual/on-mouse.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `OnMouseReleaseMoveAcceptanceSample_RunsOnBytecodeVm` выполняет sample в `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: да, неизвестное строковое имя события диагностируется на семантическом уровне через новый договор `MB2036`;
- Профиль: не приоритет, потому что это интерактивный слой ввода.

Примечание: `MOUSEPRESS()` намеренно не расширяется на `UP` и `MOVE`; она остаётся функцией одноразовой проверки нажатия кнопки.

## Обновление реестра - 2026-05-10 - ON MOUSE WHEEL

Дополнение к строке `ON MOUSE`:
- VM: да, `BytecodeVm` различает событие `Wheel` через существующий операнд вида события в инструкциях `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler` и `ResumeMouseHandler`;
- Автотесты: да, добавлены parser-, binder-, interpreter-, BytecodeVm-, IDE-router/service- и execution-проверки для `ON MOUSE("WHEEL")`;
- Runnable sample: да, `samples/operators/on-mouse/wheel_handler.axm`;
- Manual: да, обновлены `docs/manual/on-mouse.html`, `docs/manual/data-and-input.html` и поисковый индекс;
- Сквозной VM: да, `OnMouseWheelAcceptanceSample_RunsOnBytecodeVm` и `Run_WithBytecodeMode_UsesBytecodeVmForOnMouseWheel` выполняют сценарий через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: частично, неизвестные имена событий по-прежнему диагностируются существующим договором `MB2036`, отдельная новая ошибка для колеса не вводилась;
- Профиль: не приоритет, это интерактивный слой ввода.

Примечание: `MOUSEPRESS()` не расширялся на колесо. Событие `Wheel` доступно только через `ON MOUSE`, а величина прокрутки передаётся первым параметром обработчика.

## Обновление реестра - 2026-05-10 - SCREEN и графические команды

Новая область: графический режим `SCREEN`, текстовый слой, пиксельные примитивы и функции сведений о текущем режиме.

Состояние покрытия:
- VM: да, `BytecodeVm` поддерживает `SCREEN`, `CLS`, `COLOR`, `LOCATE`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `POINT`, `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH`, `SCREENHEIGHT`;
- Автотесты: частично, обновлены runtime-проверки мышиного договора и сохранена сборочная проверка VM; отдельные parser/execution/IDE-тесты графики остаются ближайшим долгом;
- Runnable sample: да, добавлены `samples/graphics/screen/mode_info.axm`, `samples/graphics/text/print_locate_color.axm`, `samples/graphics/input/input_on_screen.axm`, `samples/graphics/mouse/mouse_events.axm`, `samples/graphics/primitives/pset_line_circle.axm`;
- Manual: да, добавлена страница `docs/manual/screen.html`, обновлены навигация и поисковый индекс;
- Сквозной VM: частично, байткодная поддержка реализована без отката на интерпретатор, но отдельный execution-тест на `ExecutionMode.Bytecode` для графических программ ещё нужно добавить;
- Профиль: не приоритет, потому что первый графический слой интерактивный и host-зависимый.

## Обновление реестра - 2026-05-10 - SCREEN samples в BytecodeVm

Дополнение к области `SCREEN` и графических команд:
- Автотесты: да, добавлены acceptance-проверки `ScreenModeInfoAcceptanceSample_RunsOnBytecodeVm`, `ScreenTextAcceptanceSample_RunsOnBytecodeVm`, `ScreenInputAcceptanceSample_RunsOnBytecodeVm`, `ScreenPrimitivesAcceptanceSample_RunsOnBytecodeVm` и `ScreenMouseAcceptanceSample_RunsOnBytecodeVm`.
- Сквозной VM: да, все пять `samples/graphics/` запускаются через `ExecutionMode.Bytecode` без отката на интерпретатор.
- Уточнение покрытия: таблица `BytecodeVm.SupportsBuiltin(...)` теперь включает `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH`, `SCREENHEIGHT` и `POINT`, поэтому execution pipeline больше не отсекает графические программы с этими built-in как непокрытые.

## Обновление реестра - 2026-05-10 - ON MOUSE в пустых циклах

Дополнение к строкам `ON MOUSE`, `ON KEY` и `SCREEN`:
- VM: да, `BytecodeVm` теперь получает `StatementBoundary` на итерациях `WHILE`, `FOR` и `DO ... LOOP`, поэтому обработчики событий выполняются во время пустых циклов ожидания, а не только после выхода из них;
- Интерпретатор: да, interpreter вызывает тот же маршрут обработки клавиатурных и мышиных обработчиков на итерациях циклов;
- Автотесты: да, добавлен regression-тест `Execute_OnMouseRunsDuringEmptyDoLoop`;
- Runnable sample: да, обновлён `samples/graphics/mouse/mouse_events.axm`;
- Manual: да, уточнён `docs/manual/on-mouse.html`;
- Сквозной VM: да, существующий `ScreenMouseAcceptanceSample_RunsOnBytecodeVm` остаётся сквозной проверкой графического mouse-sample, а новый runtime-тест фиксирует именно пустой цикл.
## Обновление реестра - 2026-05-10 - игровой SCREEN sample

Дополнение к области `SCREEN`, графических команд и клавиатурного ввода:
- Runnable sample: да, добавлен `samples/graphics/game/paddle_ball.axm`;
- Автотесты: да, добавлен `ScreenGameAcceptanceSample_RunsOnBytecodeVm`;
- Сквозной VM: да, пример запускается через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Покрываемый ручной сценарий: движение платформы через `KEYDOWN("LEFT")` и `KEYDOWN("RIGHT")`, выход через `KEYPRESS("ESC")`, переключение паузы через одноразовое событие `KEYPRESS("SPACE")`, отрисовка платформы через `LINE ..., BF` и мяча через `CIRCLE`;
- Профиль: не приоритет, пример интерактивный и предназначен для ручной проверки графики и ввода, а не для производительного сравнения.
## Обновление реестра - 2026-05-10 - разнесён manual по графике

Дополнение к области `SCREEN` и графических команд:
- Manual: да, прежняя общая страница `docs/manual/screen.html` разделена на обзор `docs/manual/graphics.html`, справочную страницу `docs/manual/screen.html`, страницу текстового слоя `docs/manual/graphics-text.html`, страницу примитивов `docs/manual/graphics-primitives.html` и страницу функций состояния `docs/manual/screen-functions.html`;
- Навигация: да, обновлены `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`, `docs/manual/index.html` и `docs/manual/reference.html`;
- Покрытие поведения: не менялось, обновление относится только к справочнику и уточняет уже существующие `SCREEN`, `CLS`, `COLOR`, `LOCATE`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `POINT`, `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH` и `SCREENHEIGHT`;
- Runnable sample: без изменений, существующие примеры в `samples/graphics/` теперь прямо указаны на профильных страницах.

## Обновление реестра - 2026-05-10 - исправлена приёмка COLOR в SCREEN

Дополнение к области `SCREEN` и текстового слоя:
- Автотесты: да, добавлены IDE-проверки сохранения цветов ячеек, правила `COLOR foreground` без сброса фона, порядка палитры BGRA и масштабирования кадра renderer-ом;
- Runnable sample: да, обновлён `samples/graphics/text/print_locate_color.axm`, теперь он прямо покрывает `COLOR 14, 1` и последующий `COLOR 15` с прежним фоном;
- Manual: без изменения структуры, существующая страница `docs/manual/graphics-text.html` уже описывала правило «если фон не указан, текущий фон не меняется»;
- Сквозной VM: статус не меняется, потому что исправление находится в IDE-host и framebuffer-отрисовке, а не в понижении или исполнении байткода.

## Обновление реестра - 2026-05-10 - русский текст и 256 цветов SCREEN

Дополнение к области `SCREEN` и графических команд:
- Автотесты: да, добавлены IDE-проверки вывода русских букв в framebuffer и расширенной палитры `0..255`;
- Runnable sample: да, обновлён `samples/graphics/text/print_locate_color.axm`, теперь он содержит русскую строку и цветовые индексы выше `15`;
- Manual: да, обновлены `docs/manual/graphics-text.html` и `docs/manual/graphics-primitives.html`: палитра описана как `0..255`, с классическими цветами `0..15`, RGB-кубом `16..231`, серой шкалой `232..255` и правилом `ABS(color) MOD 256`;
- Сквозной VM: существующий `ScreenTextAcceptanceSample_RunsOnBytecodeVm` продолжает запускать текстовый графический sample через `ExecutionMode.Bytecode`; изменение относится к host-отрисовке framebuffer и не требует новых bytecode-инструкций.

## Обновление реестра - 2026-05-10 - блочная псевдографика SCREEN

Дополнение к области `SCREEN` и текстового графического слоя:
- Автотесты: да, добавлены IDE-проверки полного блока `█`, экранного кода `CHR(219)` и затенённых блоков `▓`, `▒`, `░` вместе с кодами `178`, `177`, `176`;
- Runnable sample: да, обновлён `samples/graphics/text/print_locate_color.axm`, теперь он выводит расширенную палитру `16..255` через `PRINT CHR(219);` в ячейках `SCREEN`;
- Manual: да, обновлена `docs/manual/graphics-text.html` с описанием поддержанных блочных псевдосимволов и примером палитры;
- Сквозной VM: да, существующий `ScreenTextAcceptanceSample_RunsOnBytecodeVm` продолжает запускать этот пример через `ExecutionMode.Bytecode`; изменение находится в IDE-host rasterizer и не требует новых инструкций VM;
- Профиль: не приоритет, это визуальное отображение текстового слоя, а не вычислительно горячий слой VM.

## Обновление реестра - 2026-05-10 - CHR и ASC с CP866-диапазоном

Дополнение к строковым функциям `CHR` и `ASC`:
- VM: да, `BytecodeVm` использует общий `RuntimeCharacterEncoding` и возвращает CP866-символы для `CHR(0..255)`;
- Интерпретатор: да, использует тот же слой;
- Автотесты: да, обновлены runtime-, VM- и execution-проверки для `CHR(159)`, `CHR(219)`, `ASC("Я")`, `ASC("█")` и Unicode-кода выше `255`;
- Runnable sample: да, обновлены `samples/functions/chr/basic_usage.axm`, `samples/functions/asc/basic_usage.axm` и `samples/graphics/text/print_locate_color.axm`;
- Manual: да, обновлены `docs/manual/string-functions-build-and-split.html` и `docs/manual/graphics-text.html`;
- Сквозной VM: да, execution-проверки подтверждают выполнение через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Профиль: не приоритет, изменение относится к строковой совместимости и визуальным символам, а не к новому производительному сценарию.

## Обновление реестра - 2026-05-11 - отдельная таблица символов CHR и ASC в manual

Дополнение к строковым функциям `CHR` и `ASC`:
- Manual: да, помимо `docs/manual/string-functions-build-and-split.html` и `docs/manual/graphics-text.html`, добавлена отдельная страница `docs/manual/characters.html` с полной таблицей ASCII/IBM CP866 для кодов `0..255`;
- Навигация: да, обновлены `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`, `docs/manual/reference.html` и `docs/manual/index.html`;
- Поведение языка, VM, автотесты и runnable samples не менялись: это уточнение пользовательского справочника по уже реализованному договору.
## Обновление реестра - 2026-05-11 - оператор SLEEP

Новая область: оператор временной остановки `SLEEP`.

Состояние покрытия:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `Opcode.Sleep`;
- Интерпретатор: да, interpreter выполняет тот же пользовательский договор;
- Автотесты: да, добавлены parser-, binder-, interpreter- и `BytecodeVm`-проверки для `SLEEP 0`, `SLEEP` без аргумента и отрицательной длительности;
- Runnable sample: да, добавлен `samples/operators/sleep/delay_and_key_wait.axm`;
- Manual: да, добавлена страница `docs/manual/sleep.html`, обновлены `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js` и `docs/manual/reference.html`;
- Сквозной VM: да, добавлен acceptance-тест `SleepAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, runtime- и VM-проверки фиксируют `MB3007` для отрицательной длительности;
- Профиль: не приоритет, оператор предназначен для ожидания и интерактивных сценариев, а не для производительного сравнения.

## Обновление реестра - 2026-05-11 - SCREEN 0 как текстовое окно

Дополнение к области `SCREEN`:
- VM: да, `BytecodeVm` теперь трактует инструкцию `SCREEN 0` как включение экранного текстового вывода, а не как возврат к служебной вкладке `Output`;
- Интерпретатор: да, использует тот же host-договор `IsScreenOutputActive`;
- Автотесты: да, добавлены IDE-проверки `ActivateScreenZero_UsesNativeTextWindowWithoutEnablingGraphicsMode` и `ScreenZeroInput_DoesNotEchoPromptToOutputPane`, а также acceptance-проверка `ScreenZeroTextInputAcceptanceSample_RunsOnBytecodeVm`;
- Runnable sample: да, добавлен `samples/graphics/input/text_screen_input.axm`;
- Manual: да, обновлены `docs/manual/screen.html`, `docs/manual/graphics.html` и `docs/manual/graphics-text.html`;
- Сквозной VM: да, новый sample выполняется через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: прежний договор `MB3010` сохраняется для графических команд вне `SCREEN 1..10`;
- Профиль: не приоритет, потому что это интерактивная маршрутизация host-ввода и вывода, а не вычислительно горячий слой VM.
## Обновление реестра - 2026-05-11 - оператор &

Новая область: строковая конкатенация смешанных скалярных значений через оператор `&`.

Состояние покрытия:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `Opcode.Concat`;
- Интерпретатор: да, interpreter выполняет тот же пользовательский договор через отображаемое строковое представление значений;
- Автотесты: да, добавлены lexer-, parser-, binder-, interpreter- и `BytecodeVm`-проверки для `&`;
- Runnable sample: да, добавлен `samples/operators/ampersand/mixed_scalar_concat.axm`;
- Manual: да, обновлена страница `docs/manual/arithmetic-and-logic.html` и поисковый индекс справочника;
- Сквозной VM: да, sample добавлен в acceptance-проверку `ScalarOperatorAcceptanceSamples_RunOnBytecodeVm`;
- Негативный договор: да, недопустимые сочетания типов остаются в существующей диагностике `MB2003`;
- Профиль: не приоритет, оператор покрывает удобство построения строк и участвует в скалярных выражениях, но отдельный производительный сценарий сейчас не требуется.
## Обновление реестра - 2026-05-12 - Unicode-пара ASCW и CHRW

Дополнение к строковым функциям `CHR` и `ASC`:
- VM: да, `BytecodeVm` поддерживает новые built-in `ASCW` и `CHRW` через общий маршрут встроенных функций;
- Интерпретатор: да, использует ту же модель Unicode-кода символа;
- Автотесты: да, добавлены semantics-, interpreter-, `BytecodeVm`- и execution-проверки для отличия `ASCW`/`CHRW` от CP866-договора `ASC`/`CHR`, а также ошибки `MB3007` для пустого текста и недопустимого кода;
- Runnable sample: да, добавлены `samples/functions/ascw/unicode_codepoint.axm` и `samples/functions/chrw/unicode_character.axm`, а негативные примеры `empty_text_error.axm` и `invalid_code_error.axm` включены в acceptance-проверки;
- Manual: да, обновлены `docs/manual/string-functions-build-and-split.html`, `docs/manual/characters.html`, навигация и поисковый индекс;
- Сквозной VM: да, acceptance-проверки запускают новые samples через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: да, runtime-диагностика `MB3007` проверяется в интерпретаторе и VM;
- Профиль: не приоритет, потому что это уточнение строковой совместимости, а не новый производительный сценарий.

## Обновление реестра - 2026-05-12 - накопление строк через конкатенацию

Дополнение к строковым выражениям `&` и `STRING + STRING`:
- VM: да, `BytecodeVm` получил специализированные инструкции `AppendStringLocal`, `AppendStringGlobal`, `AppendStringArgument` и `AppendStringCapture` для шаблона `s = s & expr` / `s = s + expr`;
- Интерпретатор: да, распознаёт тот же шаблон в присваивании и использует внутренний строковый накопитель;
- Автотесты: да, добавлены проверки сохранения семантики для `&`, строкового `+` и случая `t = s` перед последующим `s = s & ...`;
- Runnable sample: да, добавлен `samples/smoke/engine-compare/string_concat_accumulator.axm`;
- Manual: не требуется, потому что публичная семантика языка и набор операторов не изменились;
- Сквозной VM: да, новый `engine-compare`-сценарий проходит через `BytecodeVm` без отката на интерпретатор;
- Профиль: горячий сценарий выделен отдельно, а повторный длинный прогон `compare-engines --iterations 20 --warmup 3 --filter "string_concat_accumulator.axm"` теперь держится около `0.522x` runtime / `0.534x` total, то есть строковое накопление уже глубоко в целевом диапазоне.

## Обновление реестра - 2026-05-12 - REPLACEMANY

Новая область: встроенная строковая функция `REPLACEMANY(text, oldItems, newItems)`.

Состояние покрытия:
- VM: да, `BytecodeVm` поддерживает `REPLACEMANY` через быстрый маршрут встроенных функций и общий `RuntimeStringReplaceMany`;
- Интерпретатор: да, использует тот же runtime-алгоритм после проверки массивов правил;
- Автотесты: да, добавлены semantics-, interpreter-, `BytecodeVm`- и execution-проверки для выбора самого длинного правила, равной длины правил, отсутствия совпадений и runtime-диагностики `MB3007`;
- Runnable sample: да, добавлен `samples/functions/replacemany/multiple_tokens.axm`;
- Manual: да, обновлены `docs/manual/string-functions-search-and-replace.html`, `docs/manual/string-functions.html` и поисковый индекс;
- Сквозной VM: да, sample `samples/functions/replacemany/multiple_tokens.axm` запускается в execution-проверке через текущий путь исполнения без отката на другой договор;
- Негативный паритет: да, interpreter- и VM-проверки фиксируют `MB3007` для недопустимой таблицы правил;
- Профиль: горячий реальный сценарий зафиксирован через `samples/smoke/repair_plans_mojibake.axm`, где сотни последовательных `REPLACE` заменены одним `REPLACEMANY`-проходом.

## Обновление реестра - 2026-05-13 - заранее объявленный счётчик FOR

Дополнение к строке циклов `FOR/NEXT`:
- VM: да, `BytecodeVm` использует тот же `VariableSymbol` для заранее объявленного через `DIM` счётчика и обычный путь `StoreVariable`/`LoadVariable`;
- Интерпретатор: да, `BoundForStatement.DeclaresVariable` различает счётчик, созданный самим `FOR`, и счётчик, заранее объявленный через `DIM`;
- Автотесты: да, добавлены проверки binder-, interpreter- и `BytecodeVm`-поведения для `DIM i AS INTEGER` перед `FOR i = ...`;
- Runnable sample: да, добавлен `samples/control-flow/for/predeclared_counter.axm`;
- Manual: да, обновлены `docs/manual/for.html` и обзор `docs/manual/loops.html`;
- Сквозной VM: да, acceptance-набор control-flow запускает новый пример через общий путь исполнения;
- Негативный паритет: да, семантическая ошибка `MB2009` для заранее объявленного счётчика неподдерживаемого типа теперь подтверждена отдельным runnable sample `samples/control-flow/for/predeclared_counter_invalid_type.axm` и execution-проверкой одинаковой диагностики в режимах `Interpreter` и `Bytecode`;
- Профиль: не приоритет, поведение использует уже существующий цикл и не добавляет новый производительный сценарий.

## Обновление реестра - 2026-05-13 - полная форма CIRCLE

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` выполняет расширенную инструкцию `Circle` с флагами `STEP`, наличия цвета, начального угла, конечного угла и `aspect`;
- Интерпретатор: да, использует тот же договор аргументов и ту же host-границу `DrawCircle`;
- Автотесты: да, добавлены parser-, interpreter-, `BytecodeVm`- и IDE framebuffer-проверки для `CIRCLE STEP`, пропущенного цвета, дуг, отрицательного угла с радиальной линией и `aspect`;
- Runnable sample: да, добавлен `samples/graphics/primitives/circle_arcs_step_aspect.axm`;
- Manual: да, обновлена страница `docs/manual/graphics-primitives.html` и поисковый индекс;
- Сквозной VM: да, добавлен acceptance-тест `ScreenCircleFullFormsAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, `MB3010` для отсутствующего графического режима теперь подтверждён runnable sample `samples/operators/circle/text_screen_error.axm`, а проверки `MB3007` для радиуса, координат и `aspect` закрыты поздними runtime- и VM-проверками этого же графического среза;
- Профиль: не приоритет, потому что изменение относится к интерактивной графической отрисовке, а не к вычислительно горячему сценарию VM.

## Обновление реестра - 2026-05-14 - STEP у PSET и LINE, позиционная форма POINT

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` теперь выполняет `PSET [STEP]`, `PRESET [STEP]`, `LINE -(...)`, `LINE STEP(...)-...`, `LINE ...-STEP(...)` и форму `POINT(number)`;
- Интерпретатор: да, использует тот же договор текущей графической позиции и те же правила цветов по умолчанию;
- Автотесты: да, добавлены runtime- и `BytecodeVm`-проверки для `STEP` у `PSET`/`LINE`, для `POINT(0..3)` и для цветов по умолчанию от `COLOR`, а в IDE добавлена проверка открытого графического состояния;
- Runnable sample: да, добавлен `samples/graphics/primitives/step_and_point_position.axm`;
- Manual: да, обновлены `docs/manual/graphics-primitives.html`, `docs/manual/screen-functions.html` и поисковый индекс;
- Сквозной VM: проверить, после добавления acceptance-теста sample должен выполняться через `ExecutionMode.Bytecode` без отката;
- Негативный паритет: да, selector вне диапазона `0..3` теперь подтверждён runnable sample `samples/functions/point/invalid_selector_error.axm`, а договор `MB3010` без графического режима остаётся тем же для общей области `SCREEN`;
- Профиль: не приоритет, это выравнивание графического пользовательского договора, а не производительный подслой.

## Обновление реестра - 2026-05-14 - прозрачный текстовый фон COLOR

Дополнение к текстовому слою `SCREEN`:
- VM: да, `BytecodeVm` поддерживает три режима фона для `COLOR`: сохранить прежний, задать числовой цвет и включить `TRANSPARENT`;
- Интерпретатор: да, использует тот же трёхсостоянийный договор и тот же host-API `SetTextColor(foreground, backgroundMode, background)`;
- Автотесты: да, добавлены parser-проверка формы `COLOR ..., TRANSPARENT`, runtime- и `BytecodeVm`-проверки сохранения прозрачности и непрозрачного фона, IDE-проверка сохранения последнего числового background для `PRESET` и acceptance-проверка нового screen-sample;
- Runnable sample: да, добавлен `samples/graphics/text/transparent_text.axm`;
- Manual: да, обновлены `docs/manual/graphics-text.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлен acceptance-тест `ScreenTransparentTextAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, прежняя диагностика `MB3010` для вызова вне `SCREEN 1..10` теперь подтверждена runnable sample `samples/operators/color/text_screen_error.axm`; новая форма по-прежнему не вводит отдельной новой ошибки;
- Профиль: не приоритет, изменение относится к визуальному договору текстового слоя, а не к производительному сценарию.

## Обновление реестра - 2026-05-14 - базовый PAINT

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` поддерживает `PAINT (x, y)`, `PAINT (x, y), fillColor` и `PAINT (x, y), fillColor, borderColor`;
- Интерпретатор: да, использует тот же runtime-алгоритм заливки по четырём соседям;
- Автотесты: да, добавлены parser-, interpreter-, `BytecodeVm`- и IDE-проверки базовой заливки;
- Runnable sample: да, добавлен `samples/graphics/primitives/paint_fill_region.axm`;
- Manual: да, обновлены `docs/manual/graphics-primitives.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлен acceptance-тест `ScreenPaintAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, `MB3010` для вызова без графического режима теперь подтверждён runnable sample `samples/operators/paint/text_screen_error.axm`;
- Профиль: не приоритет, потому что это интерактивная графическая операция, а не горячий вычислительный сценарий.

## Обновление реестра - 2026-05-28 - прокрутка SCREEN 0

Дополнение к области `SCREEN` и текстового слоя:
- VM: да, `BytecodeVm` проходит сценарий прокрутки `SCREEN 0` через тот же host-договор экранного текстового вывода;
- Интерпретатор: да, использует тот же путь `WriteGraphicsText` и тот же договор перехода на новую строку;
- Автотесты: да, добавлены IDE-проверки `ScreenZero_ScrollsVisibleTextWhenNewLinePassesLastRow`, `ScreenZero_ManualScrollShowsOlderHistoryAndReturnsToBottom`, `ScreenZero_EmptyPrintOnLastRowScrollsToBlankLastRow`, `ScreenZero_ClipsLongTextWithoutVerticalScroll`, `GraphicsScreen_DoesNotAutoScrollTextPastVisibleRows`, оконная проверка `ScreenZeroVerticalScrollMessage_ShowsOlderHistoryInVisibleWindow`, end-to-end проверка `Run_ScreenZeroScrollAcceptanceProgram_KeepsLatestVisibleLinesOnBytecodeVm` и acceptance-проверка `ScreenZeroTextScrollAcceptanceSample_RunsOnBytecodeVm`;
- Runnable sample: да, добавлен `samples/graphics/screen/text_scroll.axm`;
- Manual: да, обновлены `docs/manual/screen.html`, `docs/manual/graphics-text.html` и `docs/manual/scripts/search-index.js` под историю `SCREEN 0` и правую полосу прокрутки в окне IDE;
- Сквозной VM: да, новый sample выполняется через `ExecutionMode.Bytecode` без отката на интерпретатор;
- Негативный паритет: да, новой runtime-диагностики здесь и не требуется: контраст между прокруткой `SCREEN 0` и отсутствием автопрокрутки в `SCREEN 1..10` закреплён IDE-регрессиями и acceptance-сценарием `samples/graphics/screen/text_scroll.axm`;
- Профиль: не приоритет, потому что шаг меняет пользовательский договор текстового окна, а не горячий вычислительный участок VM.

## Обновление реестра - 2026-05-14 - полный PAINT

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` теперь выполняет полную форму `PAINT [STEP] (x, y)[, [краска][, [граница][, фон]]]`, включая `STEP`, пустые позиционные места, строковую краску и четвёртый аргумент `фон`;
- Интерпретатор: да, использует тот же договор аргументов и тот же `GraphicsPaintRequest` для сплошной и шаблонной заливки;
- Автотесты: да, добавлены parser-, interpreter-, `BytecodeVm`-, IDE- и execution-проверки для `PAINT STEP`, заливки текущим цветом фона по умолчанию, шаблонной перекраски и повторной шаблонной заливки поверх старого фона;
- Runnable sample: да, добавлены `samples/operators/paint/fish_fill.axm`, `samples/operators/paint/step_default_fill.axm` и `samples/operators/paint/pattern_repaint.axm`;
- Manual: да, обновлены `docs/manual/graphics-primitives.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлены acceptance-тесты `ScreenPaintAcceptanceSample_RunsOnBytecodeVm` и `ScreenPaintStepAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, `MB3010` вне графического режима теперь подтверждён runnable sample `samples/operators/paint/text_screen_error.axm`, а поздний negative sample `samples/operators/paint/empty_pattern_error.axm` отдельно закрывает `MB3007` для недопустимого строкового образца;
- Профиль: не приоритет, потому что это интерактивная графическая операция, а не производительный сценарий VM;
- Уточнение договора: при пропущенной `краске` используется текущий цвет фона, строковый образец читается как байты через `ASC`/IBM CP866, бит `1` рисуется текущим foreground-цветом, бит `0` — текущим background-цветом, а текущая графическая позиция после `PAINT` обновляется в стартовую точку.

## Обновление реестра - 2026-05-15 - полный DRAW

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `Draw` и использует общий runtime-исполнитель DRAW-строки после проверки активного графического режима;
- Интерпретатор: да, использует тот же общий runtime-исполнитель `GraphicsDrawExecutor` без дублирования parser-логики DRAW-строки;
- Автотесты: да, добавлены parser-, binder-, interpreter- и `BytecodeVm`-проверки для строкового литерала, строковой переменной, составного выражения, базовых перемещений, `B`, `N`, `C`, `S`, `A`, `TA`, `M`, `P` и runtime-диагностик;
- Runnable sample: да, добавлены `samples/operators/draw/square.axm`, `samples/operators/draw/octagon.axm`, `samples/operators/draw/blind_and_return.axm`, `samples/operators/draw/rotated_spokes.axm`, `samples/operators/draw/paint_segments.axm` и `samples/operators/draw/dynamic_string.axm`;
- Manual: да, обновлены `docs/manual/graphics-primitives.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлены acceptance-проверки `ScreenDrawAcceptanceSamples_RunOnBytecodeVm` для тематических sample `DRAW`;
- Негативный паритет: да, поздний приёмочный аудит `DRAW` уже подтвердил controlled-диагностики для `MB3010` и `MB3007`, включая неизвестные команды, отрицательные длины, неверные форматы и историческую форму `X`;
- Профиль: не приоритет, это интерактивная графическая операция, а не горячий вычислительный сценарий VM;
- Проектное исключение: историческая форма `X/VARPTR$` сознательно не поддерживается и диагностируется явно, а современная замена — собирать итоговую DRAW-строку обычными строковыми выражениями Axiom.
- Axiom не вводит QuickBASIC-суффиксы `$` в имена переменных или built-in ради `DRAW`; для динамической строки используются обычные Axiom-формы вроде `DIM commands AS STRING` и `STR(...)`.

## Обновление реестра - 2026-05-15 - приёмочный аудит DRAW

Уточнение состояния области `DRAW` после приёмочного аудита:
- VM: да, `BytecodeVm` и интерпретатор теперь одинаково фиксируют строгую диагностику, если `B` или `N` стоят перед `C`, `S`, `A`, `TA`, `P` или `X`; модификаторы больше не теряются молча;
- Автотесты: да, добавлены проверки на строгую семантику `B/N`, на `P f` с границей по умолчанию, на `A0..A3`, `TA90`, `TA-90`, `TA0`, на переполнение координат, на очень большие цвета, на текущую графическую позицию и на строковые edge-case формы;
- Runnable sample: да, добавлен `samples/operators/draw/paint_default_border.axm` для ручной проверки формы `P f` с границей по умолчанию;
- Manual: да, раздел `DRAW` в `docs/manual/graphics-primitives.html` уточнён под строгий запрет `B/N` перед немув-командами, под фактическую экранную геометрию `TA` и под правило `P f` с границей, равной цвету заливки;
- Негативный паритет: да, runtime-путь больше не выпускает наружу сырые `OverflowException`, `FormatException`, `IndexOutOfRangeException` или `ArgumentOutOfRangeException` в проверенных сценариях `DRAW`;
- Сквозной VM: да, acceptance-путь `samples/operators/draw/` проходит целиком, включая новый sample `paint_default_border.axm`.

## Обновление реестра - 2026-05-15 - CIRCLE отвергает нечисловые и бесконечные углы

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` и интерпретатор теперь одинаково проверяют не только `radius` и `aspect`, но и конечность `start`/`end` перед вызовом host-слоя;
- Интерпретатор: да, использует общий helper `GraphicsCircleValidation` и сообщает тот же `MB3007`, если `start` или `end` не являются конечными числами;
- Автотесты: да, добавлены проверки `Execute_CircleValidationRejectsNonFiniteAngles()`, `Execute_CompiledCircleValidationRejectsNonFiniteAngles()`, `DrawCircle_IgnoresNonFiniteAngles(...)` и `Run_WithBytecodeMode_DrawCircleFallbackIgnoresNonFiniteAngles()`;
- Runnable sample: нет, отдельный отрицательный `.axm` для `NaN`/`Infinity` не добавлялся, потому что текущий пользовательский слой языка не даёт честно довести такие `DOUBLE` до вызова `CIRCLE`;
- Manual: да, `docs/manual/graphics-primitives.html` и спецификация `CIRCLE` уточнены под требование конечности `start`, `end` и `aspect`;
- Сквозной VM: да, fallback-путь `IGraphicsHost.DrawCircle` и IDE-host одинаково игнорируют невалидные внутренние вызовы без специальной реализации графического host-а;
- Негативный паритет: да, ошибка `MB3007` теперь явно покрывает и нечисловые/бесконечные углы дуги;
- Профиль: не приоритет, это внутреннее упрочнение договора аргументов без изменения производительного сценария.

## Обновление реестра - 2026-05-15 - LINE и CIRCLE насыщены по координатам и полному обороту

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` и интерпретатор теперь используют общий путь `GraphicsInstructionArguments.TryCreateLine(...)` / `TryCreateCircle(...)`, который отвергает координаты вне диапазона `-1000000000..1000000000`, ограничивает `aspect <= 1000000` и трактует совпавшие после нормализации `start`/`end` как полный контур `CIRCLE`;
- Интерпретатор: да, fallback `IGraphicsHost.DrawLine` и IDE-host используют насыщающее сложение в алгоритме Брезенхэма, а `GraphicsEllipseRasterizer` клиппирует вывод по границам текущего `SCREEN`;
- Автотесты: да, добавлены проверки `Execute_LineReportsOutOfRangeCoordinates()`, `Execute_CircleReportsOutOfRangeCoordinatesAndAspect()`, `Execute_CompiledLineReportsOutOfRangeCoordinates()`, `Execute_CompiledCircleReportsOutOfRangeCoordinatesAndAspect()`, `DrawLine_DoesNotOverflowWhenErrorDoublingSaturates()`, `DrawCircle_FullTurnAnglesRasterizeFullCircle()`, `GraphicsEllipseRasterizer_RasterizeClipsPlotPixelsToScreen()`, `Run_WithBytecodeMode_DrawLineFallbackSurvivesErrorDoublingOverflow()` и `ScreenCircleFullTurnAcceptanceSample_RunsOnBytecodeVm`;
- Runnable sample: да, добавлен `samples/graphics/primitives/circle_full_turn.axm`;
- Manual: да, обновлены `docs/SPECIFICATION3.md`, `docs/manual/graphics-primitives.html` и `docs/GRAPHICS_OPERATORS_FLAGS.md`;
- Сквозной VM: да, новый sample полного оборота `CIRCLE` запускается через `ExecutionMode.Bytecode` без отката;
- Негативный паритет: да, `MB3007` теперь явно покрывает выход координат `LINE`/`CIRCLE` за поддержанный диапазон и чрезмерный `aspect`, а форма `CIRCLE ..., 0, 2 * PI` фиксирована как полный контур, а не пустая дуга;
- Профиль: не приоритет, это упрочнение графического договора и защит от переполнения, а не новый горячий benchmark.

## Обновление реестра - 2026-05-15 - оператор VIEW

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `View`, различает `VIEW` reset, абсолютную форму `VIEW SCREEN` и относительную форму `VIEW`;
- Интерпретатор: да, использует тот же host-договор `SetViewport(...)` / `ResetViewport()` и те же правила пересчёта координат;
- Автотесты: да, добавлены parser-, binder-, interpreter-, `BytecodeVm`-, IDE- и execution-проверки `Parse_ViewResetStatement()`, `Parse_ViewStatementSupportsScreenAndOptionalBorder()`, `Bind_ViewResetStatement()`, `Bind_ViewStatementBindsCoordinatesAndOptionalColors()`, `Bind_ViewStatementSupportsEmptyFillSlot()`, `Execute_ViewSupportsAbsoluteAndRelativeViewportCoordinates()`, `Execute_CompiledViewSupportsAbsoluteAndRelativeViewportCoordinates()`, `Viewport_AbsoluteAndRelativeCoordinatesTranslatePixelsAndPreservePhysicalPosition()` и `Run_WithBytecodeMode_UsesBytecodeVmForViewScreenAndRelativeCoordinates()`;
- Runnable sample: да, добавлен `samples/operators/view/screen_and_relative.axm`;
- Manual: да, добавлены `docs/manual/view.html` и обновления в `docs/manual/graphics.html`, `docs/manual/graphics-primitives.html`, `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлены acceptance-проверки `ScreenViewAcceptanceSample_RunsOnBytecodeVm()` и автоматический audit-путь для `samples/operators/view/screen_and_relative.axm`;
- Негативный паритет: да, отдельный runnable sample `samples/operators/view/invalid_bounds_error.axm` позднее подтвердил одинаковую диагностику `MB3007` в interpreter и `BytecodeVm`;
- Профиль: не приоритет, потому что `VIEW` меняет границы и систему координат framebuffer, а не является горячим вычислительным циклом.

## Обновление реестра - 2026-05-16 - оператор WINDOW

Дополнение к области `SCREEN` и операторов рисования:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `Window`, различает `WINDOW` reset, `WINDOW SCREEN` и декартову форму `WINDOW`;
- Интерпретатор: да, использует тот же host-договор `SetWindow(...)` / `ResetWindow()` и тот же пересчёт текущей физической точки через общий координатный helper;
- Автотесты: да, добавлены parser-, binder-, interpreter-, `BytecodeVm`-, IDE- и execution-проверки `Parse_WindowResetStatement()`, `Parse_WindowStatementSupportsScreenCoordinates()`, `Bind_WindowResetStatement()`, `Bind_WindowStatementBindsCoordinatesAsDouble()`, `Execute_WindowSupportsDrawPointSelectorsAndViewRebase()`, `Execute_CompiledWindowSupportsDrawPointSelectorsAndViewRebase()`, `Window_ScreenAndCartesianCoordinatesTranslatePixelsAndPreservePhysicalPosition()` и `Run_WithBytecodeMode_UsesBytecodeVmForWindowScreenCartesianAndDraw()`;
- Runnable sample: да, добавлен `samples/operators/window/cartesian_and_screen.axm`;
- Manual: да, добавлена страница `docs/manual/window.html` и обновлены `docs/manual/graphics.html`, `docs/manual/view.html`, `docs/manual/graphics-primitives.html`, `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлена acceptance-проверка `ScreenWindowAcceptanceSample_RunsOnBytecodeVm()` для `samples/operators/window/cartesian_and_screen.axm`;
- Негативный паритет: да, отдельный runnable sample `samples/operators/window/zero_size_error.axm` позднее подтвердил одинаковую диагностику `MB3007` в interpreter и `BytecodeVm`;
- Профиль: не приоритет, потому что `WINDOW` меняет координатное отображение и текущую графическую позицию, а не является горячим вычислительным сценарием VM;
- Проектное решение Axiom: `DRAW` подчиняется активному `WINDOW` и делит общую текущую графическую позицию с остальными графическими операторами, а `POINT(0/1)` возвращает физические координаты viewport, тогда как `POINT(2/3)` возвращает логические координаты активного окна.

## Обновление реестра - 2026-05-16 - функция PMAP

Дополнение к области `SCREEN`, `VIEW` и `WINDOW`:
- VM: да, `BytecodeVm` выполняет `PMAP` как встроенную функцию и возвращает `DOUBLE` для обоих направлений преобразования;
- Интерпретатор: да, использует тот же helper `GraphicsCoordinateSpaceMath` и тот же пользовательский договор аргументов;
- Автотесты: да, добавлены binder-, interpreter-, `BytecodeVm`- и execution-проверки `Bind_PMapFunction_ReturnsDouble()`, `Bind_PMapFunction_RequiresNumericCoordinateAndIntegerSelector()`, `Execute_PMapUsesWindowAndViewportCoordinatesAndPreservesFractions()`, `Execute_PMapRejectsSelectorOutsideRange()`, `Execute_CompiledPMapUsesWindowAndViewportCoordinatesAndPreservesFractions()`, `Execute_CompiledPMapRejectsSelectorOutsideRange()` и `Run_WithBytecodeMode_UsesBytecodeVmForPMapAcrossViewAndWindow()`;
- Запускаемый пример: да, добавлен `samples/functions/pmap/window_and_viewport_conversion.axm`;
- Справочник: да, добавлена страница `docs/manual/pmap.html` и обновлены `docs/manual/graphics.html`, `docs/manual/window.html`, `docs/manual/view.html`, `docs/manual/graphics-primitives.html`, `docs/manual/screen-functions.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлена acceptance-проверка `PMapAcceptanceSample_RunsOnBytecodeVm()` для `samples/functions/pmap/window_and_viewport_conversion.axm`;
- Негативный паритет: да, interpreter и VM одинаково сообщают `MB3007` для селектора вне диапазона `0..3` и для некорректной преобразуемой координаты;
- Профиль: не приоритет, потому что `PMAP` выполняет единичное координатное преобразование и не является самостоятельным горячим сценарием VM;
- Проектное решение Axiom: `PMAP` строится поверх уже закрытого `VIEW` + `WINDOW`, работает в системе физического viewport, без активного `WINDOW` ведёт себя как тождественное преобразование и сохраняет дробный результат без округления к пикселю.

## Обновление реестра - 2026-05-17 - операторы GET и PUT

Дополнение к области `SCREEN`, `VIEW`, `WINDOW` и `PMAP`:
- VM: да, `BytecodeVm` выполняет отдельные инструкции `Get` и `Put` и использует общий helper `GraphicsBufferTransfer` для вычисления прямоугольника, формы буфера и действий `PSET` / `PRESET` / `AND` / `OR` / `XOR`;
- Интерпретатор: да, использует тот же helper без отдельного второго договора packed-буфера;
- Автотесты: да, добавлены parser-, binder-, runtime-, `BytecodeVm`- и execution-проверки для обычной формы, `STEP`, `WINDOW`-координат, `PRESET`, нормализации значений буфера и несовпадения формы массива;
- Запускаемый пример: да, добавлены `samples/operators/get/rectangle_capture.axm` и `samples/operators/put/default_xor_and_preset.axm`;
- Справочник: да, добавлена страница `docs/manual/get-put.html` и обновлены `docs/manual/graphics.html`, `docs/manual/graphics-primitives.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, execution-проверки `Run_WithBytecodeMode_UsesBytecodeVmForGetPutTransfers()`, `GetAcceptanceSample_RunsOnBytecodeVm()` и `PutAcceptanceSample_RunsOnBytecodeVm()` проходят без отката на интерпретатор;
- Профиль: не приоритет, потому что текущий шаг фиксирует пользовательский договор поблочного переноса и общую host-границу абсолютных пикселей, а не новый самостоятельный benchmark-сценарий;
- Проектное решение Axiom: пользовательский буфер `GET` / `PUT` — это обычный двумерный числовой массив, первая размерность задаёт ширину, вторая — высоту, действие `PUT` по умолчанию равно `XOR`, а `PRESET` трактуется как `255 - source` после нормализации `ABS(TRUNC(value)) MOD 256`.

## Обновление реестра - 2026-05-17 - SCREEN 10 выбирает наибольший вписываемый размер 16:9

Дополнение к области `SCREEN`, текстовой сетки и информационных функций `SCREEN...`:
- VM: да, `BytecodeVm` и интерпретатор используют один и тот же host-договор `ActivateScreen(10)` и получают фактические размеры режима через общий provider, поэтому `SCREENWIDTH()` / `SCREENHEIGHT()` и расчёт сетки `LOCATE` больше не завязаны на старое фиксированное `1280 x 720`;
- Интерпретатор: да, `ProgramConsoleService` вынес выбор размеров `SCREEN` в отдельный `IScreenModeSizeProvider`, а для `SCREEN 10` использует рабочую область выбранного монитора, режим `16:9` и запасной путь `1280 x 720`, если host не смог определить геометрию;
- Автотесты: да, добавлены `ActivateScreen10_UsesProvidedBestFitSizeAndVisibleLocateGrid`, `ScreenModeSizeProviderTests`, `Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm`, `Screen10BestFitAcceptanceSample_RunsOnBytecodeVm`, а execution/IDE fake-host-таблицы синхронизированы под детерминированный surrogate `1918 x 1079`;
- Запускаемый пример: да, добавлен `samples/graphics/screen/best_fit_dimensions.axm`;
- Справочник: да, обновлены `docs/manual/screen.html`, `docs/manual/screen-functions.html`, `docs/manual/graphics.html`, `docs/manual/graphics-text.html` и `README.md`;
- Сквозной VM: да, адресные IDE- и execution-проверки проходят, а `Axiom.Ide.SmokeHarness` получил отдельный маршрут `--screen10-only`, который подтверждает фактические `SCREENWIDTH()` / `SCREENHEIGHT()`, расчёт `LOCATE` и финальный вывод через реальный `Axiom.Ide.exe`;
- Профиль: не приоритет, потому что шаг уточняет host-геометрию режима `SCREEN 10`, а не добавляет новый вычислительно горячий сценарий;
- Проектное решение Axiom: `SCREEN 10` выбирает рабочую область монитора по центру главного окна IDE, строит в ней максимально возможный клиентский прямоугольник `16:9`, не подгоняет размеры под кратность текстовой ячейки `8 x 12`, а `LOCATE` рассчитывает видимую сетку от фактических пикселей.

## Обновление реестра - 2026-05-28 - аудит графического покрытия

Уточнение после адресного аудита графической ветки:
- `SCREEN 10`, `GET` / `PUT`, `VIEW`, `WINDOW`, `PMAP`, `DRAW`, `CIRCLE` и мышиный координатный договор повторно подтверждены через адресные runtime-, IDE- и execution-проверки, а также через smoke-маршруты `--screen10-only` и `--get-put-only` реального `Axiom.Ide`;
- для `GET` / `PUT` негативный сценарий несовпадения формы буфера уже явно покрыт в interpreter- и `BytecodeVm`-проверках `Execute_GetRejectsBufferShapeMismatch()` и `Execute_CompiledGetRejectsBufferShapeMismatch()`;
- для `PMAP` негативный сценарий селектора вне `0..3` уже явно покрыт в interpreter- и `BytecodeVm`-проверках `Execute_PMapRejectsSelectorOutsideRange()` и `Execute_CompiledPMapRejectsSelectorOutsideRange()`;
- для `DRAW` негативный паритет уже доведён до `да` предыдущим приёмочным аудитом: проверены неизвестные команды, отрицательные длины, неверные форматы, историческая форма `X/VARPTR$`, dangling `B`/`N`, strict `B`/`N` перед немув-командами и controlled overflow;
- для `VIEW`, `WINDOW` и полного `PAINT` остаётся честный статус `частично`: позитивные и общие runtime-диагностики покрыты, но отдельные отрицательные `.axm`-samples для некорректного viewport/window и недопустимых шаблонных аргументов `PAINT` пока не добавлялись;
- новых пользовательских форм или нового покрытия VM этот аудит не вводит, поэтому статус профиля для графики остаётся `не приоритет`.

## Обновление реестра - 2026-05-28 - отрицательные sample для VIEW, WINDOW и PAINT

Дополнение к областям `VIEW`, `WINDOW` и полного `PAINT`:
- Runnable sample: да, добавлены `samples/operators/view/invalid_bounds_error.axm`, `samples/operators/window/zero_size_error.axm` и `samples/operators/paint/empty_pattern_error.axm`;
- Автотесты: да, добавлена acceptance-проверка `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity`, которая запускает эти три sample в режимах `Bytecode` и `Interpreter`;
- Сквозной VM: да, для всех трёх sample подтверждён запуск через `ExecutionEngineKind.BytecodeVm` без fallback;
- Негативный паритет: да для выбранных отрицательных сценариев `VIEW`, `WINDOW` и полного `PAINT`: `VIEW` с недопустимыми границами, `WINDOW` с нулевой шириной и `PAINT` с пустым строковым образцом одинаково дают `MB3007` в interpreter и `BytecodeVm`;
- Manual: без изменений, потому что страницы `docs/manual/view.html`, `docs/manual/window.html` и `docs/manual/graphics-primitives.html` уже описывают соответствующие ограничения; этот шаг добавил недостающие runnable-проверки, а не новый пользовательский договор.

## Обновление реестра - 2026-05-28 - оператор VIEW PRINT

Дополнение к области `SCREEN`, `VIEW` и текстового слоя:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `ViewPrint`, различает сброс `VIEW PRINT` и форму `VIEW PRINT top TO bottom`;
- Интерпретатор: да, использует тот же host-договор `SetTextViewport(...)` / `ResetTextViewport()`;
- Автотесты: да, есть parser-, binder-, interpreter-, `BytecodeVm`-, IDE- и execution-проверки для сброса, установки строкового диапазона, прокрутки внутри него и ошибок некорректных строк;
- Runnable sample: да, добавлен `samples/operators/view-print/scrolling_region.axm`;
- Manual: да, обновлены `docs/manual/view.html`, `docs/manual/graphics.html`, `docs/manual/graphics-text.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, добавлена acceptance-проверка `ViewPrintAcceptanceSample_RunsOnBytecodeVm`, а общий audit-путь получает `ESC` для `samples/operators/view-print/`;
- Негативный паритет: да, отдельный runnable sample `samples/operators/view-print/invalid_rows_error.axm` позднее подтвердил одинаковую диагностику `MB3007` в interpreter и `BytecodeVm`;
- Профиль: не приоритет, потому что оператор меняет текстовое окно вывода, а не горячий вычислительный участок VM.

## Обновление реестра - 2026-05-28 - VIEW PRINT получил отрицательный sample-паритет

Дополнение к области `SCREEN`, `VIEW` и текстового слоя:
- Runnable sample: да, добавлен `samples/operators/view-print/invalid_rows_error.axm` для неверного диапазона строк;
- Автотесты: да, `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` теперь включает `VIEW PRINT` рядом с отрицательными sample для `VIEW`, `WINDOW` и `PAINT`;
- Сквозной VM: да, новый negative sample прогоняется через `ExecutionEngineKind.BytecodeVm` без fallback в том же execution-теcте;
- Негативный паритет: да, неверный диапазон строк в `VIEW PRINT` теперь подтверждён отдельным runnable sample и одинаковой диагностикой `MB3007` в interpreter и `BytecodeVm`;
- Manual: без изменений, потому что пользовательский договор оператора уже был отражён в `docs/manual/view.html`, а этот шаг добавил недостающую runnable-негативную проверку.

## Обновление реестра - 2026-05-28 - PMAP получил отрицательный sample-паритет

Дополнение к области `SCREEN`, `VIEW`, `WINDOW` и `PMAP`:
- Runnable sample: да, добавлен `samples/functions/pmap/invalid_selector_error.axm` для селектора вне диапазона `0..3`;
- Автотесты: да, `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` теперь включает этот sample рядом с отрицательными графическими sample;
- Сквозной VM: да, sample прогоняется через `ExecutionEngineKind.BytecodeVm` без fallback в той же паритетной execution-проверке;
- Негативный паритет: да, неверный селектор `PMAP` теперь подтверждён отдельным runnable sample и одинаковой диагностикой `MB3007` в interpreter и `BytecodeVm`;
- Manual: без изменений, потому что `docs/manual/pmap.html` уже описывает диагностику селектора вне диапазона, а этот шаг добавил недостающую runnable-негативную проверку.

## Обновление реестра - 2026-05-28 - оператор PALETTE

Дополнение к области `SCREEN` и палитры:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `Palette`, различает сброс палитры и форму `PALETTE index, color`;
- Интерпретатор: да, использует тот же договор хоста `ResetPalette()` / `SetPaletteColor(index, color)`;
- Автотесты: да, добавлены проверки парсера, связывателя, интерпретатора, `BytecodeVm`, IDE и слоя запуска для сброса, переназначения одного индекса, диагностики без графического `SCREEN` и переназначения цветов уже нарисованных пикселей в `ProgramConsoleService`;
- Запускаемый пример: да, добавлен `samples/operators/palette/change_and_reset_colors.axm`;
- Справочник: да, добавлен `docs/manual/palette.html`, обновлены `docs/manual/graphics.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PaletteAcceptanceSample_RunsOnBytecodeVm` запускает пример через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор, а общий позитивный аудит получает `ESC` для `samples/operators/palette/`;
- Негативный паритет: да, диагностика времени выполнения `MB3010` для отсутствующего графического режима теперь подтверждена не только тестами интерпретатора и `BytecodeVm`, но и отдельным runnable sample `samples/operators/palette/text_screen_error.axm` с общей parity-проверкой interpreter/`BytecodeVm`;
- Профиль: не приоритет, потому что `PALETTE` меняет таблицу отображения цветов, а не вводит горячий вычислительный цикл VM;
- Проектное решение Axiom: пиксели кадрового буфера хранят цветовые индексы, поэтому `PALETTE index, color` немедленно меняет вид уже нарисованных пикселей, а `POINT(x, y)` продолжает возвращать сохранённый индекс.

## Обновление реестра - 2026-05-31 - форма PALETTE USING

Дополнение к оператору `PALETTE`:
- VM: да, та же инструкция `Palette` теперь различает `0` значений для сброса, `2` значения для `PALETTE index, color` и `1` значение для `PALETTE USING array`;
- Интерпретатор: да, применяет видимые элементы одномерного числового массива к индексам палитры `0..255` через существующий договор хоста `SetPaletteColor(index, color)`;
- Автотесты: да, добавлены проверки парсера, связывателя, интерпретатора, `BytecodeVm` и слоя запуска для формы `PALETTE USING`;
- Запускаемый пример: да, добавлен `samples/operators/palette/using_array_colors.axm`;
- Справочник: да, обновлены `docs/manual/palette.html`, `docs/manual/graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PaletteUsingAcceptanceSample_RunsOnBytecodeVm` запускает пример через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, отсутствие `SCREEN` для `PALETTE USING` теперь подтверждено отдельным runnable sample `samples/operators/palette/using_without_screen_error.axm` с общей parity-проверкой interpreter/`BytecodeVm`; текущий положительный договор по одномерному массиву остаётся без изменений.
- Профиль: не приоритет, потому что операция ограничена максимум первыми `256` элементами палитры.

## Обновление реестра - 2026-05-31 - оператор PCOPY

Дополнение к области `SCREEN` и экранных страниц:
- VM: да, `BytecodeVm` выполняет отдельную инструкцию `PCopy` и делегирует копирование страниц договору хоста `CopyScreenPage(sourcePage, destinationPage)`;
- Интерпретатор: да, использует тот же договор хоста и ту же проверку активного графического `SCREEN`;
- Автотесты: да, есть проверки парсера, связывателя, интерпретатора, `BytecodeVm`, IDE и слоя запуска для `SCREEN` с активной/видимой страницей, копирования пикселей и палитры страницы, диагностики без графического `SCREEN` и неверного номера страницы;
- Запускаемые примеры: да, позитивный пример `samples/operators/pcopy/page_copy_buffer.axm`, отрицательный пример `samples/graphics/screen/invalid_page_error.axm`, а также отрицательные примеры `samples/operators/pcopy/invalid_page_error.axm` и `samples/operators/pcopy/text_screen_error.axm`;
- Справочник: да, добавлен `docs/manual/pcopy.html`, обновлены `docs/manual/screen.html`, `docs/manual/graphics.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PCopyAcceptanceSample_RunsOnBytecodeVm` запускает позитивный пример через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор, а `GraphicsNegativeAcceptanceSamples_HaveStableDiagnosticParity` включает неверный номер страницы;
- Негативный паритет: да, неверная страница `SCREEN 1, , 4, 0`, неверная страница `PCOPY 4, 0` и `PCOPY` в текстовом `SCREEN 0` подтверждены отдельными запускаемыми примерами и одинаковыми диагностиками в интерпретаторе и `BytecodeVm`;
- Профиль: не приоритет, потому что `PCOPY` является копированием целой графической страницы через хост, а не горячим вычислительным циклом VM.

## Обновление реестра - 2026-06-01 - IMAGE и DRAWIMAGE

Дополнение к области `SCREEN` и современных графических изображений:
- VM: да, `BytecodeVm` поддерживает тип `IMAGE`, встроенные функции `LOADIMAGE`, `IMAGEWIDTH`, `IMAGEHEIGHT` и отдельную инструкцию `DrawImage` без отката на интерпретатор;
- Интерпретатор: да, использует тот же договор хоста `LoadImage(...)` / `DrawImage(...)` и ту же диагностику, что VM;
- Автотесты: да, добавлены проверки парсера, связывателя, интерпретатора, `BytecodeVm`, слоя запуска, хоста IDE и передачи базового каталога ресурса из IDE-запуска;
- Запускаемые примеры: да, позитивный пример `samples/graphics/images/load_and_draw_image.axm` и отрицательный пример `samples/graphics/images/missing_image_error.axm`;
- Справочник: да, добавлен `docs/manual/images.html`, обновлены `docs/manual/graphics.html`, `docs/manual/screen.html`, `docs/manual/data-types.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `DrawImageAcceptanceSample_RunsOnBytecodeVm` запускает позитивный пример через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, отсутствующий файл изображения подтверждён отдельным запускаемым примером и одинаковой диагностикой `MB3005` в интерпретаторе и `BytecodeVm`; пустой путь, пустой `IMAGE`, неверный размер и отсутствие `SCREEN` покрыты runtime- и VM-проверками;
- Профиль: не приоритет, потому что первый срез проверяет функциональную интеграцию загрузки и вывода изображения через хост, а отдельный производительный сценарий не добавлялся.

## Обновление реестра - 2026-06-02 - SPRITE и DRAWSPRITE

Дополнение к области `SCREEN` и современных графических спрайтов:
- VM: да, `BytecodeVm` поддерживает тип `SPRITE`, встроенные функции `LOADSPRITE`, `SPRITEWIDTH`, `SPRITEHEIGHT`, `SPRITECOUNT` и отдельную инструкцию `DrawSprite` без отката на интерпретатор;
- Интерпретатор: да, использует тот же договор хоста `LoadSprite(...)` / `DrawSprite(...)`, общий полноцветный слой кадрового буфера и те же диагностики, что VM;
- Автотесты: да, добавлены проверки парсера, связывателя, интерпретатора, `BytecodeVm`, слоя запуска, хоста IDE, IDE-словаря подсветки и форматирования для новых имён;
- Запускаемые примеры: да, позитивный пример `samples/graphics/sprites/walk_cycle.axm`, отрицательный пример `samples/graphics/sprites/invalid_frame_error.axm` и постоянный ресурс `samples/graphics/sprites/assets/walk_sheet.png`;
- Справочник: да, добавлен `docs/manual/sprites.html`, обновлены `docs/manual/graphics.html`, `docs/manual/data-types.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `DrawSpriteAcceptanceSample_RunsOnBytecodeVm` запускает позитивный пример через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, неверный номер кадра подтверждён отдельным запускаемым примером и одинаковой диагностикой `MB3007` в интерпретаторе и `BytecodeVm`; пустой путь, неверный размер кадра, отсутствующий файл, пустой `SPRITE`, лист без доступных кадров, неверный размер вывода и отсутствие `SCREEN` покрыты runtime- и VM-проверками;
- Профиль: не приоритет, потому что срез проверяет функциональную интеграцию спрайтового листа через хост, а отдельный производительный сценарий не добавлялся.

## Обновление реестра - 2026-06-02 - кадровый сценарий SPRITE через PCOPY

Дополнение к областям `SPRITE` / `DRAWSPRITE` и `PCOPY`:
- VM: да, новый пользовательский сценарий не вводит новую инструкцию, а подтверждает совместную работу уже поддержанных `DrawSprite` и `PCopy` в одном байткодном запуске;
- Автотесты: да, `DrawSpritePageFlipAcceptanceSample_RunsOnBytecodeVm` запускает `samples/graphics/sprites/page_flip_walk.axm` через `ExecutionMode.Bytecode`, проверяет `ExecutionEngineKind.BytecodeVm`, отсутствие отката на интерпретатор, графический вывод и два вызова `DRAWSPRITE`;
- Запускаемые примеры: да, добавлен `samples/graphics/sprites/page_flip_walk.axm`, который готовит кадр спрайта на скрытой странице `1`, переносит его на видимую страницу `0` через `PCOPY` и завершается по `ESC`;
- Справочник: да, обновлены `docs/manual/pcopy.html`, `docs/manual/sprites.html`, `docs/manual/graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, acceptance-тест фиксирует байткодный запуск, а живой маршрут `Axiom.Ide.SmokeHarness --sprite-only` дополнительно проверяет этот сценарий в реальном IDE-хосте, где `PCOPY` переносит полноцветный слой кадрового буфера;
- Негативный паритет: не менялся, потому что сценарий положительный и новых диагностик не вводит;
- Профиль: не приоритет, отдельный производительный сценарий не добавлялся.

## Обновление реестра - 2026-06-05 - второй срез современной графики CANVAS

Дополнение к области современного `CANVAS`:
- VM: да, `BytecodeVm` выполняет `CAMERA`, `RESETCAMERA`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE` и `DRAWTEXT` без отката на интерпретатор;
- Интерпретатор: да, использует тот же договор хоста `SetCanvasCamera(...)`, `ResetCanvasCamera()`, `DrawCanvasLine(...)`, `DrawCanvasCircle(...)`, `FillCanvasCircle(...)` и `DrawCanvasText(...)`;
- Автотесты: да, есть parser-, binder-, interpreter-, `BytecodeVm`-, execution- и IDE-проверки для новых операторов второго среза;
- Запускаемые примеры: да, добавлены `samples/graphics/modern/hud_text.axm`, `samples/graphics/modern/camera_world.axm`, `samples/graphics/modern/lines_and_circles.axm` и главный интеграционный пример `samples/graphics/game/maze_chase.axm`;
- Справочник: да, должны быть согласованы `docs/manual/modern-graphics.html`, `docs/manual/graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, используются acceptance-тесты `ModernHudTextAcceptanceSample_RunsOnBytecodeVm`, `ModernCameraAcceptanceSample_RunsOnBytecodeVm`, `ModernLinesAndCirclesAcceptanceSample_RunsOnBytecodeVm` и `MazeChaseAcceptanceSample_RunsOnBytecodeVm`;
- Негативный паритет: да, отсутствие `CANVAS`, отрицательный радиус и выход координат за поддерживаемый диапазон теперь подтверждены не только runtime- и VM-проверками, но и отдельными runnable sample `samples/graphics/modern/missing_canvas_error.axm`, `negative_radius_error.axm`, `invalid_text_position_error.axm` с общей parity-проверкой interpreter/`BytecodeVm`;
- Профиль: не приоритет, пока не выделен отдельный `engine-compare` сценарий для современного графического цикла.

## Обновление реестра - 2026-06-06 - профиль IMAGE, SPRITE и современного CANVAS подтянут

Дополнение к областям `IMAGE` / `DRAWIMAGE`, `SPRITE` / `DRAWSPRITE` и современного `CANVAS`:
- VM: да, `BytecodeVm` получил безопасный быстрый путь для built-in `RGB`, `LOADIMAGE`, `LOADSPRITE`, прямой просмотр аргументов через `PeekUnchecked()` и быстрый integer/long-путь в `DRAWIMAGE` / `DRAWSPRITE` без лишних общих конверсий на успешном пути;
- Интерпретатор: да, поведение не менялось, паритет по пользовательскому договору сохраняется;
- Автотесты: да, существующие parser-, binder-, execution- и acceptance-проверки не требовали смены, потому что правка была только производительной;
- Запускаемые примеры: да, для современного графического профиля теперь опорными runnable-сценариями остаются `samples/graphics/modern/hud_text.axm`, `camera_world.axm`, `lines_and_circles.axm`, `samples/graphics/game/maze_chase.axm` и оба производительных smoke-сценария `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm`, `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`;
- Сквозной VM: да, длинный контрольный прогон `compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"` прошёл успешно без отката на интерпретатор;
- Негативный паритет: не менялся;
- Профиль: да, область больше не является слепой зоной. По длинному прогону быстрые примитивы `CANVAS` держатся около `0.980x` runtime / `0.976x` total, а image/sprite-путь после ускорений подтянут до примерно `1.033x` runtime / `1.031x` total. Значит, `DRAWIMAGE` / `DRAWSPRITE` уже почти доведены до паритета, но ещё остаются отдельным небольшим производительным долгом VM.

## Обновление реестра - 2026-06-06 - профиль современной графики стал опкодным

Дополнение к областям `IMAGE` / `DRAWIMAGE`, `SPRITE` / `DRAWSPRITE` и современного `CANVAS`:
- VM: да, в `BytecodeVm` добавлен быстрый успешный путь для `Opcode.Convert`, который снимает значение через `PopUnchecked()` и не делает ранний `ResolveLocation(...)`, пока не нужен медленный диагностический откат;
- Интерпретатор: да, пользовательский договор не менялся, правка затронула только байткодный быстрый путь;
- Автотесты: да, существующие parser-, binder-, execution- и acceptance-проверки продолжают проходить без изменения поведения;
- Запускаемые примеры: да, основными runnable-сценариями для этой области остаются `samples/graphics/modern/hud_text.axm`, `camera_world.axm`, `lines_and_circles.axm`, `samples/graphics/game/maze_chase.axm`, `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm` и `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`;
- Сквозной VM: да, новый служебный профиль `profile-bytecode-opcodes --filter "modern_canvas_sprite_burst.axm"` проходит через реальный `BytecodeVm` без отката на интерпретатор и даёт частоты инструкций поверх того же smoke-сценария;
- Негативный паритет: не менялся;
- Профиль: да, область теперь видна на уровне отдельных опкодов. Для `modern_canvas_sprite_burst.axm` основная нагрузка пришлась на `PushConst`, `LoadLocal`, `Add`, `Convert`, `Multiply` и `Modulo`, тогда как сами `DrawSprite` и `DrawImage` заняли лишь небольшую долю общего числа инструкций. Значит, следующий шаг должен дожимать не только графические вызовы, но и окружающую арифметику спрайтового цикла.

## Обновление реестра - 2026-06-06 - modern graphics согласован по стеку и ускорен на целочисленном пути

Дополнение к областям `IMAGE` / `DRAWIMAGE`, `SPRITE` / `DRAWSPRITE` и современного `CANVAS`:
- VM: да, `BytecodeVm` теперь согласован с lowering по контракту `INTEGER|LONG` для `CANVAS`, `CAMERA`, `DRAWRECT`, `FILLRECT`, `DRAWLINE`, `DRAWCIRCLE`, `FILLCIRCLE`, `DRAWTEXT`, `DRAWIMAGE` и `DRAWSPRITE`; лишний жёсткий `AsLong()` для modern graphics пути убран;
- Валидатор байткода: да, `BytecodeModuleValidator` теперь принимает для этих modern graphics опкодов не только `LONG`, но и целочисленно совместимый стек `INTEGER|LONG`;
- Автотесты: да, добавлен адресный lowering-тест `Lower_ModernGraphicsIntegerArguments_SkipsRedundantLongConversions`, а адресные bytecode runtime-тесты modern canvas продолжают проходить на том же пользовательском договоре;
- Запускаемые примеры: да, опорные runnable и performance-сценарии не менялись и остаются `samples/graphics/modern/hud_text.axm`, `camera_world.axm`, `lines_and_circles.axm`, `samples/graphics/game/maze_chase.axm`, `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm`, `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`;
- Сквозной VM: да, `profile-bytecode-opcodes` и длинние прогоны `compare-engines` проходят через реальный `BytecodeVm` без отката на интерпретатор;
- Негативный паритет: не менялся;
- Профиль: да, после новой правки `Convert` в `modern_canvas_sprite_burst.axm` упал с `9762` до `80`, а длинний прогон `modern_canvas_` теперь держит примерно `0.823x` / `0.826x` для примитивов и `0.932x` / `0.934x` для sprite/image-пути. Значит, современная графика больше не является слоем, где bytecode VM отстаёт от интерпретатора на адресном benchmark-срезе.

## Обновление реестра - 2026-06-06 - metadata built-in IMAGE и SPRITE тоже ускорены адресным fast-path

Дополнение к областям `IMAGE` / `DRAWIMAGE`, `SPRITE` / `DRAWSPRITE` и современного `CANVAS`:
- VM: да, `BytecodeVm` теперь имеет отдельный fast-path не только для `LOADIMAGE` / `LOADSPRITE`, но и для `IMAGEWIDTH`, `IMAGEHEIGHT`, `SPRITEWIDTH`, `SPRITEHEIGHT`, `SPRITECOUNT`;
- Автотесты: да, адресные bytecode runtime-тесты размеров `IMAGE` и `SPRITE` продолжают проходить после перевода этих built-in на прямой путь;
- Запускаемые примеры: да, к прежним адресным performance-сценариям добавлен `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm`;
- Сквозной VM: да, новый metadata-сценарий проходит через реальный `BytecodeVm` без отката на интерпретатор;
- Негативный паритет: не менялся;
- Профиль: да, metadata built-in современной графики теперь тоже измерены отдельно. По длиннему прогону `20` итераций с `3` прогревами `modern_graphics_metadata_sweep.axm` держится около `0.447x` runtime / `0.455x` total. Значит, и metadata-подслой современной графики больше не является долгом VM.

## Обновление реестра - 2026-06-06 - игровой клавиатурный подслой maze_chase измерен отдельно

Дополнение к областям клавиатурного ввода `KEYPRESS`, `KEYDOWN`, `ON KEY`, `INKEY` и к интеграционному примеру `maze_chase`:
- VM: да, `BytecodeVm` выполняет адресный игровой сценарий `samples/smoke/engine-compare/maze_chase_input_polling.axm` без отката на интерпретатор;
- Интерпретатор: да, используется тот же пользовательский договор опроса клавиш, включая сочетание `KEYPRESS(...)`, `KEYDOWN(...)`, `UCASE(...)` и `LCASE(...)` в игровом цикле;
- Автотесты: да, область клавиатурного ввода уже покрыта runtime-, execution- и acceptance-проверками, а новый шаг меняет не семантику, а профиль и адресный benchmark;
- Запускаемые примеры: да, у игрового примера `samples/graphics/game/maze_chase.axm` теперь есть три адресных производительных среза: `maze_chase_grid_logic.axm`, `maze_chase_input_polling.axm` и `maze_chase_mixed_frame.axm`;
- Справочник: да, пользовательский договор `KEYPRESS`, `KEYDOWN` и `ON KEY` уже отражён в `docs/manual/`;
- Сквозной VM: да, benchmark-case `Игровой ввод: maze_chase без графики` в `src/Axiom.ManualChecks/Program.cs` использует отдельный scripted input-provider и реально прогоняет этот слой через `BytecodeVm`;
- Негативный паритет: да, отдельные runtime/acceptance-проверки клавиатурного слоя уже есть и новым шагом не менялись;
- Профиль: да, область теперь измерена отдельным игровым сценарием. Длинный прогон `20` итераций с `3` прогревами держится около `0.739x` runtime / `0.742x` total, а опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-224029.txt` показывает верх по `KEYPRESS`, `UCASE`, `LCASE`, `KEYDOWN`, `PushConst`, `LoadLocal` и ветвлениям. Значит, и игровой клавиатурный подслой `maze_chase` сейчас не является долгом VM.
## Обновление реестра - 2026-06-06 - текстовые helper-функции современного CANVAS

Дополнение к области современного `CANVAS`:
- VM: да, `BytecodeVm` выполняет `TEXTWIDTH` и `TEXTHEIGHT` без отката на интерпретатор и требует активный `CANVAS`, как и `DRAWTEXT`;
- Интерпретатор: да, использует тот же helper `CanvasTextMetrics` и тот же договор `MB3010` без активного `CANVAS`;
- Автотесты: да, добавлены binder-, interpreter-, `BytecodeVm`- и execution-проверки `Bind_TextMetricsFunctions_ReturnInteger`, `Bind_TextMetricsFunctions_RequireOneStringArgument`, `Execute_TextMetricsReturnCanvasPixelSize`, `Execute_CompiledTextMetricsReturnCanvasPixelSize`, `TextWidthAcceptanceSample_RunsOnBytecodeVm`, `TextHeightAcceptanceSample_RunsOnBytecodeVm`;
- Запускаемые примеры: да, добавлены `samples/functions/textwidth/hud_metrics.axm` и `samples/functions/textheight/multiline_label.axm`;
- Справочник: да, согласованы `docs/manual/modern-graphics.html`, `docs/manual/scripts/search-index.js` и `docs/manual/scripts/manual.js`;
- Сквозной VM: да, оба acceptance-сценария запускаются через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, отсутствие `CANVAS` для `TEXTWIDTH` и `TEXTHEIGHT` отдельно подтверждено одинаковой диагностикой `MB3010` в interpreter и `BytecodeVm`;

## Обновление реестра - 2026-06-07 - VIEWPORT для современного CANVAS

Дополнение к области современного `CANVAS`:
- VM: да, `BytecodeVm` выполняет `VIEWPORT` без отката на интерпретатор;
- Интерпретатор: да, использует тот же host-договор `SetViewport(...)` / `ResetViewport()` и тот же modern-only helper обрезки прямоугольника по `CANVAS`;
- Автотесты: да, добавлены parser-, binder-, lowering-, emission-, interpreter-, `BytecodeVm`- и IDE-проверки для форм `VIEWPORT` и `VIEWPORT (left, top)-(right, bottom)`;
- Запускаемые примеры: да, добавлен runnable-пример `samples/graphics/modern/viewport_hud.axm`;
- Справочник: да, обновлены `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Негативный паритет: да, interpreter и VM одинаково сообщают `MB3010` без активного `CANVAS` и `MB3007`, если после обрезки по холсту viewport становится пустым.
- Профиль: не приоритет, потому что это короткие helper-вызовы для HUD-разметки, а не отдельный горячий benchmark-сценарий.

## Обновление реестра - 2026-06-07 - VIEWPORT доведён до флагманского примера maze_chase

Дополнение к области современного `CANVAS` и к интеграционному примеру `maze_chase`:
- VM: да, `BytecodeVm` теперь принимает для `VIEWPORT` тот же целочисленный стек `INTEGER|LONG`, что и остальные modern-graphics операторы, без лишнего жёсткого `AsLong()` на успешном пути;
- Интерпретатор: да, пользовательский договор не менялся, а интеграционный пример `samples/graphics/game/maze_chase.axm` теперь использует тот же viewport-договор, что и standalone sample `viewport_hud.axm`;
- Автотесты: да, добавлен адресный runtime-тест `Execute_CompiledViewportAcceptsIntegerExpressionsFromProgramState`, а acceptance-набор `MazeChaseAcceptanceSample_*` снова проходит после перевода игры на `VIEWPORT`;
- Запускаемые примеры: да, главным runnable-примером для живого viewport-сценария теперь являются и `samples/graphics/modern/viewport_hud.axm`, и `samples/graphics/game/maze_chase.axm`;
- Сквозной VM: да, `MazeChaseAcceptanceSample_RunsOnBytecodeVm`, `MazeChaseAcceptanceSample_SpaceShowsPauseHud`, `MazeChaseAcceptanceSample_LowerKShowsCalibrationHints`, `MazeChaseAcceptanceSample_TWritesAllCalibrationValues` и `MazeChaseApplyCalibrationSample_RewritesInitPelletAlignmentFromCalibrationFile` проходят через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, `VIEWPORT` сохраняет те же диагностики `MB3010` и `MB3007`, а acceptance-хост синхронизирован с договором modern `CANVAS`, чтобы тестовая среда больше не жила по старому `SCREEN`-ограничителю;
- Профиль: не приоритет, потому что текущий шаг закрывал не производительность, а интеграционную координатную согласованность `VIEWPORT` на флагманском примере.

## Обновление реестра - 2026-06-07 - POINT(x, y) перенесён в CANVAS

Дополнение к областям `POINT` и современного `CANVAS`:
- VM: да, `BytecodeVm` теперь выполняет форму `POINT(x, y)` и при активном `SCREEN`, и при активном `CANVAS`, без отката на интерпретатор;
- Интерпретатор: да, форма `POINT(x, y)` теперь требует не только `SCREEN`, а любую активную рисуемую поверхность `SCREEN` или `CANVAS`; форма `POINT(selector)` остаётся screen-only;
- Автотесты: да, добавлены interpreter- и `BytecodeVm`-проверки `Execute_PointReadsPhysicalCanvasPixelsWithoutViewportOrCameraReprojection`, `Execute_CompiledPointReadsPhysicalCanvasPixelsWithoutViewportOrCameraReprojection`, `Execute_PointSelectorStillRequiresScreenWhenCanvasIsActive`, `Execute_CompiledPointSelectorStillRequiresScreenWhenCanvasIsActive`;
- Запускаемые примеры: да, добавлен runnable sample `samples/functions/point/canvas_pixel_read.axm`;
- Справочник: да, обновлены `docs/manual/graphics-primitives.html`, `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PointCanvasAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, `POINT(selector)` при активном `CANVAS` по-прежнему даёт `MB3010`, а `POINT(x, y)` без активного `SCREEN` и без активного `CANVAS` сохраняет ту же диагностику отсутствия подходящей поверхности;
- Профиль: не приоритет, потому что это адресное чтение пикселя для отладки и простых проверок, а не отдельный горячий benchmark.

## Обновление реестра - 2026-06-07 - PSET перенесён в CANVAS

Дополнение к областям `PSET`, `PRESET` и современного `CANVAS`:
- VM: да, `BytecodeVm` теперь выполняет `PSET` и при активном `SCREEN`, и при активном `CANVAS`, без отката на интерпретатор;
- Интерпретатор: да, форма `PSET` теперь требует не только `SCREEN`, а любую активную рисуемую поверхность `SCREEN` или `CANVAS`; `PRESET` остаётся screen-only;
- Автотесты: да, добавлены interpreter- и `BytecodeVm`-проверки `Execute_PSetWritesCanvasPixelAtPhysicalViewportPoint`, `Execute_PSetUsesCanvasWorldCoordinatesWithViewportAndCamera`, `Execute_PSetStillRequiresScreenOrCanvas`, `Execute_PResetStillRequiresScreenWhenCanvasIsActive`, `Execute_CompiledPSetWritesCanvasPixelAtPhysicalViewportPoint`, `Execute_CompiledPSetUsesCanvasWorldCoordinatesWithViewportAndCamera`, `Execute_CompiledPSetStillRequiresScreenOrCanvas`, `Execute_CompiledPResetStillRequiresScreenWhenCanvasIsActive`, а в IDE добавлен тест `Canvas_PSetProjectsWorldPixelIntoViewport`;
- Запускаемые примеры: да, добавлен runnable sample `samples/operators/pset/canvas_world_pixel.axm`;
- Справочник: да, обновлены `docs/manual/graphics-primitives.html`, `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PSetCanvasAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, `PSET` без активного `SCREEN` и без активного `CANVAS` сообщает `MB3010`, а `PRESET` при активном `CANVAS` по-прежнему сохраняет ту же диагностику screen-only формы;
- Профиль: не приоритет, потому что это точечная пиксельная запись для отладки и простых процедурных эффектов, а не отдельный горячий benchmark.

## Обновление реестра - 2026-06-08 - PRESET перенесён в CANVAS

Дополнение к областям `PRESET`, `PSET` и современного `CANVAS`:
- VM: да, `BytecodeVm` теперь выполняет `PRESET` и при активном `SCREEN`, и при активном `CANVAS`, без отката на интерпретатор;
- Интерпретатор: да, форма `PRESET` теперь требует не только `SCREEN`, а любую активную рисуемую поверхность `SCREEN` или `CANVAS`;
- Автотесты: да, добавлены interpreter- и `BytecodeVm`-проверки `Execute_PResetWritesCanvasBackgroundPixelAtPhysicalViewportPoint`, `Execute_PResetUsesCanvasWorldCoordinatesWithViewportAndCamera`, `Execute_PResetStillRequiresScreenOrCanvas`, `Execute_CompiledPResetWritesCanvasBackgroundPixelAtPhysicalViewportPoint`, `Execute_CompiledPResetUsesCanvasWorldCoordinatesWithViewportAndCamera`, `Execute_CompiledPResetStillRequiresScreenOrCanvas`;
- Запускаемые примеры: да, добавлен runnable sample `samples/operators/preset/canvas_background_pixel.axm`;
- Справочник: да, обновлены `docs/manual/graphics-primitives.html`, `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `PResetCanvasAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, `PRESET` без активного `SCREEN` и без активного `CANVAS` сообщает `MB3010`;
- Профиль: не приоритет, потому что это точечная пиксельная запись для стирания и простых процедурных эффектов, а не отдельный горячий benchmark.

## Обновление реестра - 2026-06-08 - PAINT для CANVAS

- VM: да, `BytecodeVm` теперь принимает базовую числовую форму `PAINT` не только на `SCREEN`, но и при активном `CANVAS`;
- Автотесты: да, добавлены interpreter-, `BytecodeVm`-, IDE- и execution-проверки заливки области `CANVAS` под `CAMERA` и `VIEWPORT`, а также отдельные негативные проверки вызова без активной графической поверхности;
- Runnable sample: да, добавлен `samples/operators/paint/canvas_region_fill.axm`;
- Manual: да, обновлены `docs/manual/graphics-primitives.html`, `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Негативный паритет: да, вызов `PAINT` без `SCREEN` и без `CANVAS` одинаково сообщает `MB3010` в interpreter и `BytecodeVm`;
- Уточнение договора: в `CANVAS` оператор `PAINT` принимает мировые координаты, проходит через `CAMERA` и `VIEWPORT`, выполняет индексную заливку цветами `0..255`, сохраняет форму `STEP` и после выполнения обновляет текущую графическую позицию до стартовой мировой точки.

## Обновление реестра - 2026-06-08 - GET и PUT для CANVAS

- VM: да, `BytecodeVm` теперь выполняет `GET` и `PUT` и при активном `SCREEN`, и при активном `CANVAS`, без отката на интерпретатор;
- Интерпретатор: да, обе формы теперь требуют любую активную рисуемую поверхность `SCREEN` или `CANVAS`, а не только `SCREEN`;
- Автотесты: да, добавлены interpreter-, `BytecodeVm`-, IDE- и execution-проверки чтения и записи блока `CANVAS` под `CAMERA` и `VIEWPORT`, а также отдельные негативные проверки вызова без активной графической поверхности;
- Runnable sample: да, добавлены `samples/operators/get/canvas_block_capture.axm` и `samples/operators/put/canvas_block_restore.axm`;
- Manual: да, обновлены `docs/manual/get-put.html`, `docs/manual/modern-graphics.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `GetCanvasAcceptanceSample_RunsOnBytecodeVm` и `PutCanvasAcceptanceSample_RunsOnBytecodeVm` подтверждают оба sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, вызов `GET` или `PUT` без `SCREEN` и без `CANVAS` одинаково сообщает `MB3010` в interpreter и `BytecodeVm`;
- Уточнение договора: в `CANVAS` операторы `GET` и `PUT` принимают мировые координаты, проходят через `CAMERA` и `VIEWPORT`, но пользовательский буфер остаётся обычным двумерным индексным массивом `0..255`, а действие `PUT` по умолчанию остаётся `XOR`.

## Обновление реестра - 2026-06-08 - MOUSE для CANVAS

Дополнение к строкам `MOUSEX()`, `MOUSEY()`, `MOUSEBUTTON()`, `MOUSEPRESS()` и `ON MOUSE`:
- VM: да, `BytecodeVm` теперь принимает мышиные функции и `ON MOUSE` не только при активном `SCREEN`, но и при активном `CANVAS`;
- Интерпретатор: да, runtime-слой переведён с `RequireScreenActive(...)` на общий договор активной рисуемой поверхности там, где это нужно для мыши;
- Автотесты: да, добавлены interpreter- и `BytecodeVm`-проверки мировых координат `MOUSEX()` / `MOUSEY()` в `CANVAS`, передачи тех же координат в `ON MOUSE`, а также их совпадения с чтениями внутри обработчика;
- Runnable sample: да, добавлены `samples/functions/mousex/canvas_world_position.axm`, `samples/functions/mousey/canvas_world_position.axm` и `samples/operators/on-mouse/canvas_world_handler.axm`; 2026-06-08 эти примеры укрупнены до `CANVAS 480, 270`, чтобы координаты и удержание кнопок были видны при ручной проверке;
- Manual: да, обновлены `docs/manual/mousex.html`, `docs/manual/mousey.html`, `docs/manual/mousebutton.html`, `docs/manual/mousepress.html`, `docs/manual/on-mouse.html`, `docs/manual/keyboard-mouse-events.html`, `docs/manual/data-and-input.html` и `docs/manual/scripts/search-index.js`;
- Сквозной VM: да, `MouseCoordinatesCanvasAcceptanceSample_RunsOnBytecodeVm`, `MouseYCoordinatesCanvasAcceptanceSample_RunsOnBytecodeVm` и `OnMouseCanvasAcceptanceSample_RunsOnBytecodeVm` подтверждают mouse sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, вызов мышиных функций и `ON MOUSE` без `SCREEN` и без `CANVAS` по-прежнему сообщает `MB3010`;
- Уточнение договора: в `SCREEN` координаты мыши остаются экранными, а в `CANVAS` `MOUSEX()` / `MOUSEY()` и параметры `x/y` обработчика `ON MOUSE` возвращают мировые координаты под `VIEWPORT` и `CAMERA`.

## Обновление реестра - 2026-06-08 - TILESET, LOADTILESET и DRAWTILE

Дополнение к областям `TILESET`, `LOADTILESET`, `TILECOUNT`, `TILEWIDTH`, `TILEHEIGHT` и `DRAWTILE`:
- VM: да, `BytecodeVm` поддерживает тип `TILESET`, все три формы `LOADTILESET`, функции размеров тайла и отдельную инструкцию `DrawTile` без отката на интерпретатор;
- Интерпретатор: да, runtime-слой поддерживает тот же пользовательский договор `TILESET` и те же диагностики, что и байткодный путь;
- Автотесты: да, добавлены и проходят interpreter- и `BytecodeVm`-проверки `Execute_TilesetWidthHeightAndCountReturnLoadedAndEmptySizes`, `Execute_CompiledTilesetWidthHeightAndCountReturnLoadedAndEmptySizes`, `Execute_DrawTileCallsModernGraphicsHostAndUpdatesCurrentPosition`, `Execute_CompiledDrawTileCallsModernGraphicsHostAndUpdatesCurrentPosition`, `Execute_TilesetOperationsReportDocumentedDiagnostics` и `Execute_CompiledTilesetOperationsReportDocumentedDiagnostics`;
- Runnable sample: да, используется `samples/operators/drawtile/basic_tileset.axm`;
- Manual: да, добавлена страница `docs/manual/tilesets.html`, а навигация и обзорные страницы manual синхронизированы с этим срезом;
- Сквозной VM: да, `DrawTileAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, ошибки `MB3005`, `MB3007` и `MB3010` покрыты interpreter- и `BytecodeVm`-проверками для пустого пути, неверной сетки, пустого тайлсета, неверного `tileIndex` и вызова `DRAWTILE` без `CANVAS`;
- Уточнение договора: `tileIndex` в `DRAWTILE` является `1`-основанным, тайлы идут слева направо и сверху вниз, а `TILECOUNT`, `TILEWIDTH` и `TILEHEIGHT` для пустого значения `TILESET` возвращают `0`;
- Профиль: не приоритет, потому что этот шаг закрывал пользовательский договор и документацию первого тайлового среза, а не отдельный горячий benchmark.

## Обновление реестра - 2026-06-08 - TILEMAP, LOADTILEMAP и отрисовка карты

Дополнение к областям `TILEMAP`, `LOADTILEMAP`, `MAPWIDTH`, `MAPHEIGHT`, `MAPPIXELWIDTH`, `MAPPIXELHEIGHT`, `MAPTILEWIDTH`, `MAPTILEHEIGHT`, `MAPLAYERCOUNT`, `DRAWTILEMAP` и `DRAWTILELAYER`:
- VM: да, `BytecodeVm` поддерживает тип `TILEMAP`, загрузку карты, функции размеров и отдельные инструкции `DrawTilemap` / `DrawTileLayer` без отката на интерпретатор;
- Интерпретатор: да, runtime-слой поддерживает тот же пользовательский договор `TILEMAP`, включая пустое значение для функций размеров и ошибку при попытке рисовать пустую карту;
- Автотесты: да, добавлены и проходят parser-, binder-, interpreter- и `BytecodeVm`-проверки объявления `TILEMAP`, функций `MAP*`, отрисовки видимых слоёв, явной отрисовки скрытого слоя и ошибки объектного слоя;
- Runnable sample: да, добавлены `samples/operators/drawtilemap/visible_and_layer.axm` и карта `samples/operators/drawtilemap/simple_map.tmj`;
- Manual: да, добавлена страница `docs/manual/tilemaps.html`, обновлены `docs/manual/modern-graphics.html`, `docs/manual/graphics.html`, `docs/manual/data-types.html`, `docs/manual/reference.html`, `docs/manual/scripts/manual.js` и `docs/manual/scripts/search-index.js`;
- IDE-подсветка: да, `TILEMAP`, `DRAWTILEMAP`, `DRAWTILELAYER`, `LOADTILEMAP` и функции `MAP*` уже внесены в светлую и тёмную схемы подсветки и покрыты `tests/Axiom.Ide.Tests/`;
- Сквозной VM: да, `DrawTilemapAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: частично закрыт в этом срезе для объектного слоя через interpreter- и `BytecodeVm`-проверки; полный набор ошибок загрузки внешних файлов будет расширяться вместе со следующими `TILEMAP`-функциями;
- Уточнение договора: `DRAWTILEMAP` рисует только видимые тайловые слои, `DRAWTILELAYER` рисует указанный тайловый слой независимо от флага видимости, а оба оператора не меняют текущую графическую позицию;
- Профиль: пока не приоритет, потому что этот шаг закрывал функциональный договор и сквозную VM-приёмку, а не отдельный горячий benchmark.

## Обновление реестра - 2026-06-08 - MAPTILE, MAPTILESET, MAPTILEFLAGS и SETMAPTILE

Дополнение к области `TILEMAP`:
- VM: да, `BytecodeVm` выполняет функции `MAPTILE`, `MAPTILESET`, `MAPTILEFLAGS` и оператор `SETMAPTILE` без отката на интерпретатор;
- Интерпретатор: да, runtime-слой поддерживает тот же пользовательский договор чтения, записи и очистки клетки карты;
- Автотесты: да, добавлены parser-, binder-, interpreter-, `BytecodeVm`-, IDE- и execution-проверки для чтения клеток, записи клетки, очистки клетки и документированной диагностики записи;
- Runnable sample: да, добавлен `samples/operators/setmaptile/cell_update.axm`;
- Manual: да, обновлены `docs/manual/tilemaps.html`, обзорные страницы manual и `docs/manual/scripts/search-index.js`;
- IDE-подсветка: да, `MAPTILE`, `MAPTILESET`, `MAPTILEFLAGS` и `SETMAPTILE` внесены в светлую и тёмную схемы подсветки и покрыты `tests/Axiom.Ide.Tests/`;
- Сквозной VM: да, `SetMapTileAcceptanceSample_RunsOnBytecodeVm` подтверждает sample через `ExecutionEngineKind.BytecodeVm` без отката на интерпретатор;
- Негативный паритет: да, запись в объектный слой, запись за пределы карты, неверный тайлсет, неверный тайл и недопустимые флаги покрываются одинаковым runtime-договором interpreter и `BytecodeVm`;
- Уточнение договора: `MAPTILE*` при чтении пустой клетки, пустой карты, объектного слоя или координат вне карты возвращает `0`; `SETMAPTILE` для записи требует существующую клетку тайлового слоя, а пара `0, 0` очищает клетку;
- Профиль: пока не приоритет, потому что шаг закрывает функциональный договор изменения карты, а не отдельный горячий benchmark.

## 2026-10-06 — аудит языка (Session 1102): статус отрицательного паритета диагностик

Полный аудит (`docs/audit/LANGUAGE_FUNCTIONS_FULL_AUDIT.md`, реестр дефектов `docs/audit/LANGUAGE_DEFECT_REGISTRY.md`) уточняет статус столбца «Негативный паритет» для всей библиотеки:

- Значения и stdout валидных программ: паритет Interpreter↔VM подтверждён по всем ~700 кейсам батарей и 581 выполнимому sample — расхождений нет.
- Коды диагностик: одно расхождение — функция без RETURN (Interpreter `MB3006` с цепочкой вызовов и выполнением тела, VM `BC4054` без вывода тела) — AXD-020a.
- Тексты диагностик: 6+ подтверждённых расхождений формулировок (CLAMP, STEP 0, DIM-границы, LBOUND-dimension, унарный минус, ABS-overflow) — AXD-005; 7 текстов Interpreter в файловой подсистеме — mojibake — AXD-003.
- Локализации (line:col) ошибок аргументов builtin: систематическое расхождение (Interpreter указывает на аргумент, VM — на вызов) — AXD-006.
- Цепочки вызовов в runtime-диагностике: расхождение глубины/детализации в обе стороны (TYPE-инвариант, `<global>`-кадр, именование локальных функций) — AXD-007.
- Исполнение TRY: один семантический дефект Interpreter (FINALLY пропускается при повторном выбросе из CATCH, AXD-001) — VM ведёт себя по спецификации.

Вывод для реестра: столбец «Негативный паритет» следует считать зелёным только там, где проверяются код+сообщение+локализация; проверок на тексты/локализации/цепочки в тестовом наборе сейчас нет (test gap AXD-017). Дорожная карта закрытия — Wave 1 плана `docs/PLANS4.md` (2026-10-06).

## 2026-10-06 — Session 1103: закрытие AXD-001/002/003 из аудита

- AXD-001: FINALLY при ошибке из тела CATCH (включая `ERROR err`) теперь исполняется в Interpreter так же, как в BytecodeVm; паритет подтверждён автотестами в обоих движках и sample `samples/operators/try/rethrow_from_catch_runs_finally.axm`.
- AXD-002: новая диагностика `MB2040` (оператор-выражение без вызова) выполняется на этапе привязки, поэтому движко-независима; негативный sample `samples/acceptance/negative/expression_statement_no_effect.axm`.
- AXD-003: 8 mojibake-текстов диагностики Interpreter файловой подсистемы приведены к тем же формулировкам, что уже использовал VM; регрессионный тест на читаемый русский текст RENAMEFILE.
- Статус «Негативный паритет» из заметки выше остаётся открытым для текстов/локализаций/цепочек (AXD-005/006/007) — это следующий шаг Wave 1.

## 2026-10-06 — Session 1104: Wave 1 — единый договор диагностик движков

- Тексты шести расходившихся диагностик унифицированы (CLAMP, STEP 0, MB3005, LBOUND-dimension, унарный минус, ABS); паритет-тест `Run_UnifiedDiagnosticTextsMatchInBothEngines` фиксирует код+текст в обоих движках.
- Локации builtin-диагностик унифицированы на вызов (договор VM); Interpreter приведён.
- Цепочки вызовов: общий `RuntimeCallChainFormatter`; VM именует локальные функции по `BytecodeRoutine.Symbol`, `<global>`-кадр исключён. Остаток — инвариант TYPE (см. DEFECT_REGISTRY AXD-007).
- Новый код `MB3012` NumericDomain для оператора `^` (домен вместо переполнения), общий helper в обоих движках; `audit-diagnostics-table` — PASS.
- Кодировочный договор `OPEN FOR INPUT` = `READFILE` (строгий UTF-8 + BOM-skip, fallback Windows-1251) — общий код обоих движков в `RuntimeTextFile`.
- Дифференциальный sweep samples: DIFF 8 → 2 (остаток — инвариант TYPE; второй — ложное срабатывание от случайных temp-имён).

## 2026-10-07 — Session 1105: закрытие остатка AXD-007 и начало Wave 3

- Цепочка вызовов при инварианте TYPE унифицирована: `ValidateTypeInvariant` несёт идентификатор процедуры конструктора, VM синтезирует её кадр в цепочке; паритет-тест в обоих движках.
- Фолбэк-свитч VM сокращён до 15 живых variadic-ID — пользовательские договоры не изменились (подтверждено sweep 608 samples: SAME=590, DIFF=1 — ложное срабатывание от temp-имён).
- `ToInt32Checked` и расположение `EvaluateIntBuiltin` приведены к целевой архитектуре «общий helper + тонкий движок».

## 2026-10-07 — Session 1106: Wave 2 — подсветка из реестра и контракт non-BMP строк

- Подсветка builtins теперь генерируется из реестра: `Generate-XshdKeywords.ps1 -Write` строит BuiltIn-секцию обеих схем из `axiom metadata`; sync-тесты `HighlightingVocabularySyncTests` не дадут новому builtin появиться без подсветки (закрыт AXD-016; устранена накопившаяся пропажа STRING).
- Договор строковых единиц зафиксирован тестами `InterpreterTests.StringNonBmp`: LEN/LEFT/MID/REMOVE — UTF-16 code units, ASC/ASCW/CHR — Unicode scalars, REVERSE — паросохраняющий; любое изменение договора теперь осознанное.
- Тонкие filesystem builtins (DRIVE*, FILEDATE*, DIRSIZE, FREESPACE*, ISREADONLY/ISHIDDEN, temp-функции) получили smoke-покрытие и фиксацию русских текстов диагностики (`InterpreterTests.FileSystemDiagnostics`).

## 2026-10-07 — Session 1107: Wave 5 — документационные контракты

- Поведение языка не менялось. В manual зафиксированы: LF-концы строк WRITELINE/NEWLINE, UTF-16-единицы строк (LEN/фрагменты), правила чтения VAL. SPECIFICATION3 дополнена датированным разделом о строковых/числовых договорах.
- Исправлены ошибки аудита: сдвиги `<<`/`>>` существуют и покрыты таблицей приоритетов manual; -ассоциативность и WEEKDAY Monday-first были задокументированы ранее. Столбец Manual реестра подтверждён повторной проверкой.

## 2026-10-07 — Session 1108: Wave 4 — новые пользовательские сущности

- Новые builtins: CINT, CLNG, CDBL, CDEC (страница «INT, STR и VAL»), ARRAYSUM, ARRAYMIN, ARRAYMAX, ARRAYAVERAGE (раздел «Агрегаты» страницы массивов). Все исполняются обоими движками через общие helpers; coverage-audit подтверждает полноту (samples + search-index + VM-пути).
- Новые операторы: RANDOMIZE (страница randomize.html, новый Opcode.ReseedRandom с посевом через IRuntimeRandom.Reseed — оба движка управляются одним источником), ANDALSO/ORELSE (короткое замыкание: в VM — двухметочная схема JumpIfTrue/JumpIfFalse, в Interpreter — ленивое вычисление; BOOLEAN-only).
- NEXT принял опциональное имя счётчика (QB-наследие, игнорируется).
- Подсветка перегенерирована из реестра (249 builtins), словарь форматирования авто; sync-тесты зелёные.

## 2026-10-07 — Session 1116: сведение WIP-ветки — счётчик FOR всегда локальный (вариант B)

- Договор циклов `FOR/NEXT` переведён на единую модель (заменяет раздел реестра от 2026-05-13 «заранее объявленный счётчик FOR»): `FOR` всегда создаёт собственный локальный счётчик; занятое во внешней области имя — `MB2016`, неподходящий тип занявшего имени — `MB2009`; обращение к счётчику после `NEXT` — `MB2001`. Проверка семантическая, движко-независима.
- Диагностика «оператор-выражение без вызова» сохраняет `MB2040` (Wave 0); бывшие `MB2040`/`MB2041` ветки перенумерованы в `MB2044` (поле TYPE без SELF) и `MB2045` (предупреждение о совпадении имени переменной и процедуры) — семантический слой, оба движка получают их одинаково через общий биндер.
- Сэмплы: `maze_chase.axm` мигрирован минимально (удалены 8 заранее объявленных DIM-счётчиков, стилевые перезаписи отклонены); негативные опоры `samples/acceptance/negative/for_counter_must_be_local.axm`, `scope_field_requires_self.axm`, `scope_block_escape.axm`, `scope_field_typo_suggestion.axm`, `scope_shadow_declaration.axm`, `call_operator_hint.axm`; smoke-опоры `samples/smoke/variables_scope_*.axm`, `variables_name_collision_warning.axm`.
- Паритет: оба движка исполняют мигрированный корпус; maze_chase-acceptance (позитивный прогон BytecodeVm + калибровка InitPelletAlignment) зелёные.

## 2026-10-07 — Session 1117: независимая проверка библиотеки и границ паритета

- Реестр проверен автоматически: 303 дескриптора, 249 имён, 290 зарегистрированных ID из 293; все объявленные варианты имеют VM-dispatch, Interpreter-ID найдены. DateAdd/DateDiff/DatePart сохранены намеренно для сериализации. Наличие пути не означает семантическую корректность.
- Выполнены 449 независимых сценариев в двух движках и разбор 634 `.axm`-файлов; 345 автономных примеров запущены попарно. Подробные результаты и непроверенные клетки перегрузок: `docs/audit/LANGUAGE_FUNCTION_MATRIX_20261007.csv`, `LANGUAGE_SAMPLES_20261007.csv`, `LANGUAGE_AUDIT_EVIDENCE_20261007.json`.
- Обнаружен семантический разрыв REVERSE на одиночном суррогате; одинаковые ошибки двух движков подтверждены для типов/рангов агрегатов, checked-суммы, среднего, CINT/CLNG/VAL, определения UTF-8, READTEXT и концов строк. Реестр LA26-001…014 находится в `LANGUAGE_RECHECK_DEFECTS_20261007.md`.
- Добавлены воспроизводители: `samples/functions/arraysum/audit_scalar_result.axm`, `audit_checked_overflow.axm`; `samples/functions/cint/audit_truncation_boundary.axm`; `samples/functions/clng/audit_upper_boundary.axm`; `samples/functions/arrayaverage/audit_large_finite_mean.axm`; `samples/functions/reverse/audit_surrogate_parity.axm`. Это отрицательные свидетельства исходного поведения, а не отметка об исправлении.
- Штатная `audit-bytecode-coverage` дала FAIL по указателю и каталогам графических примеров, при отсутствии недостающих VM-вариантов. Девять графических built-in не имеют прямых `.axm`-упоминаний во всём корпусе; подробности в разделе 31 нового отчёта. Графическая/интерактивная приёмка новым автономным инструментом не подменялась.
- Подсветка всех 249 имён присутствует в обеих схемах; metadata отдельно теряет полиморфные политики и формы LBOUND/UBOUND с dimension. Полный паритет всех значений и диагностик не объявлен. Новых benchmark-семейств нет, performance matrix не менялась.

## 2026-10-07 — исправление LA26-001/002 (Session 1118)

ARRAYSUM/ARRAYMIN/ARRAYMAX возвращают скаляр типа элементов. ArrayAggregateContractTests проверяет INTEGER/LONG/DOUBLE/DECIMAL, строковые минимум/максимум, арифметику, автоматическое определение типа, положительное и отрицательное переполнение суммы INTEGER/LONG/DECIMAL, TRY/CATCH/FINALLY и необработанную MB3008 с позицией вызова. Все 24 проверки выполняются попарно в Interpreter и BytecodeVm без перехода на интерпретатор.

Примеры samples/functions/arraysum/audit_scalar_result.axm и audit_checked_overflow.axm обновлены как проверки исправленного поведения; справочник docs/manual/arrays-and-builtins.html содержит присваивание скалярного результата и обработку MB3008. Новых имён и синтаксических токенов нет. Результаты исходного аудита сохранены как исторический снимок; остальные LA26 этим изменением не закрываются. Новых сценариев измерения быстродействия нет.

## 2026-10-07 — CINT(NaN), LA26-006 (Session 1119)

В Interpreter и BytecodeVm подтверждены MB3008 на CINT(NaN), TRY/CATCH/FINALLY, продолжение после обработки и позиция необработанной диагностики: NumericConversionContractTests, 4 проверки без перехода на интерпретатор. IntegerConversionFiniteTests содержит 8 прямых проверок общей реализации: NaN, обе бесконечности, ноль, дроби обоих знаков и точные границы INTEGER. Прямые проверки не зависят от будущего исправления VAL.

Запускаемый пример: samples/functions/cint/nonfinite_catch.axm. Справочник: numeric-functions-conversion-and-legacy.html; устаревшее утверждение об отсутствии CINT/CLNG/CDBL исправлено на странице data-types-conversions.html. Новых токенов и сценариев измерения быстродействия нет.

## 2026-10-07 — LA26-010/011: READTEXT по символам Unicode (Session 1120)

Общая реализация RuntimeTextFile читает целые скалярные значения Unicode и сохраняет байтовые FILEPOS. Исправлена проверка первых байтов при определении UTF-8/Windows-1251. ReadTextUnicodeTests: 13 прямых проверок (границы буфера, пары UTF-16, комбинируемый знак, UTF-8 с BOM/без BOM, короткие Windows-1251, сохранение позиции после ошибки). ReadTextUnicodeContractTests: 4 проверки Interpreter/BytecodeVm с BOM/без BOM, count=0/1/2, EOF, FILEPOS, REWIND, SEEKFILE, MB3007 и FINALLY. Переход на интерпретатор не допускается проверками.

Запускаемый пример samples/functions/readtext/unicode_chunks.axm показывает точный сценарий «😀А», позиции и удаление своего временного файла. Справочник уточнён в file-system-functions-open-files.html и open.html. Новых имён/токенов нет; сценарии измерения быстродействия не менялись. Исторические свидетельства аудита сохранены.

## 2026-10-08 — LA26-003/004/005/007/008/009/012/013/014 (Session 1121)

NumericBoundaryContractTests проверяет границы CINT/CLNG и конечность VAL попарно в двух движках. LongConversionExceptionalTests проверяет CLNG напрямую на NaN, обеих бесконечностях и обеих внешних границах, независимо от VAL. AuditRemainingBehaviorTests проверяет большое среднее, многомерные агрегаты, логические одиночные массивы и изолированные единицы UTF-16 в REVERSE. TextFileLineEndingTests и FileLineEndingContractTests проверяют реальные байты LF, позиции и оба движка. Метаданные проверяются в CliAppTests; потребитель подписей — ide/tests/lang-features.test.cjs.

Обновлены прежние воспроизводители CINT, CLNG, ARRAYAVERAGE и REVERSE; добавлены samples/functions/val/finite_results.axm, arraysum/multidimensional_totals.axm, arraymin/unsupported_singleton.axm, writeline/lf_byte_positions.axm. Справочник обновлён по каждому поведению. Две формы LBOUND/UBOUND перенесены из связывателя в реестр: дескрипторов теперь 305, набор исполняемых форм не расширился. Новых токенов нет. Сценарии измерения быстродействия не менялись, ускорение не заявлено.

### Независимая перепроверка среднего — Session 1122

Обнаружен и исправлен дополнительный числовой дефект: ARRAYAVERAGE для значений 1e308, -1e308, 1e-308 терял малый остаток при масштабировании и возвращал 0. Это ошибка существующей функции, а не потребность в новом API. Корректная проверка через VAL воспроизвела FALSE в обоих движках до исправления; первоначальная запись экспоненциальных литералов без VAL не считается доказательством.

Теперь значения после преобразования в DOUBLE точно накапливаются в целых единицах 2^-1074 через BigInteger. Деление и округление к ближайшему чётному выполняются один раз; результат DOUBLE, правила преобразования INTEGER/LONG/DECIMAL до накопления сохранены. Добавлены проверки перестановки элементов, округления субнормальных значений и максимальных конечных значений обоих знаков. Пример: samples/functions/arrayaverage/tiny_cancellation.axm. Быстродействие нового алгоритма пока не измерялось.
### Session 1123 — согласованная запись Unicode и обычный обход каталогов

WRITE и WRITELINE кодируют аргумент каждого вызова отдельно в UTF-8 без BOM; буфер FILE хранит байты. FILEPOS/FILELENGTH рассчитаны по этим же байтам. Изолированная единица UTF-16 заменяется U+FFFD, целая пара сохраняется. FLUSH не объединяет половины из разных вызовов и не меняет итоговое содержимое.

DIRSIZE и COPYDIR не переходят по вложенным FileAttributes.ReparsePoint и не учитывают/копируют их как обычные файлы. Корневой каталог допускается явно через ссылку. COPYDIR заранее отвергает совпадающее или вложенное назначение через MB3007, включая существующие соединения каталогов в пути. Вложенные ссылки назначения не используются для записи. Существующие папки объединяются, overwrite относится к файлам. Допустимая операция не является транзакцией: при прочих файловых ошибках ранее скопированные файлы не откатываются.

Доказательства: TextFileUnicodeBoundaryTests, FileWriteUnicodeContractTests, DirectoryTraversalContractTests; 100 воспроизводимых наборов READTEXT, OUTPUT/APPEND, с FLUSH/без FLUSH, циклические и внешние реальные ссылки Windows, вложенный путь через ссылку и обычный соседний путь. Приёмочные программы: samples/functions/write/utf16_fragment_bytes.axm, samples/functions/copydir/reject_nested_destination.axm, samples/functions/dirsize/ordinary_nested_bytes.axm, samples/smoke/directory_link_boundaries.axm. Для последней применяется scripts/language-audit/Prepare-DirectoryLinkSample.ps1. Manual обновлён в этой же задаче. Новых языковых имён и измерений быстродействия в этом срезе нет.
### Session 1124 — проверка настоящего хоста IDE

Файловые функции, CURRENTDIR, CHANGEDIR и OPEN в обоих движках используют рабочую папку текущего запуска; проверены повторный запуск скомпилированного кода и продолжение остановленного VM. WorkingDirectoryContractTests 5/5, пример samples/functions/changedir/program_directory_scope.axm. POINT(x, y) в CANVAS читает физические координаты через GetAbsolutePixel; два варианта ExecutionContextRegressionTests проверяют настоящий ProgramConsoleService вместо подставного графического хоста. Примеры physical_canvas_coordinates.axm и canvas_block_restore.axm, manual обновлён. Полное решение 3596/3596; IDE 592/592. Новых имён, токенов или измерений быстродействия нет.

### Session 1125 — проверенный сценарий быстродействия ARRAYAVERAGE

Добавлен и выполнен выделенный samples/smoke/engine-compare/arrayaverage_exact_cancellation.axm. Interpreter и BytecodeVm дают TRUE без перехода на другой движок; CLI также даёт TRUE. scripts/aggregate-perf проверяет биты среднего обычных, сокращающихся, субнормальных и нулевых DOUBLE-массивов на трёх размерах. Это измерение текущей реализации, новых форм вызова и изменений семантики нет. Справочник существующего договора остаётся действующим. Профиль: docs/audit/ARRAYAVERAGE_PERF_20261008.md.

### Session 1126 — точное среднее без большого целого на каждом элементе

Семантика ARRAYAVERAGE сохранена; оба движка используют общие Int128-группы мантисс и прежнее окончательное округление. ArrayAverageExactnessTests добавляет 500 наборов с фиксированным зерном и их перестановками, независимый поиск соседних DOUBLE, границы малых/больших хранилищ, суммы группы шире UInt64, субнормальные половинные случаи и отрицательный ноль. Полное решение 3603/3603. Запущены tiny_cancellation.axm и arrayaverage_exact_cancellation.axm; новых callable-форм, токенов или пользовательской семантики нет, действующий manual остаётся верным. Статус быстродействия обновлён в BYTECODE_PERF_MATRIX.

### Session 1127 — четыре формы явной кодировки целого файла

READFILE/READALLLINES с двумя аргументами, WRITEFILE/APPENDFILE с тремя поддерживаются Interpreter и BytecodeVm. Дополнительный аргумент encoding — STRING; зарегистрированы четыре новых дескриптора, общее число 309. Отдельные 22 проверки двух движков подтверждают точные байты, единственный BOM, внешние файлы с BOM/без BOM, строгость, сохранность после отказа, UTF-8 на границе буфера и обычные BOM-подобные префиксы Windows-1251. Новые формы не допускают обратного шага как файловые операции. Явная кодировка OPEN пока не покрыта и не поддерживается.

Manual и его поиск обновлены. Примеры: readfile/explicit_encoding_choice.axm, readalllines/explicit_utf16_lines.axm, writefile/reject_unrepresentable_text.axm, appendfile/explicit_1251_append.axm; все выполнены CLI и включены в приёмочный набор VM. Обе схемы IDE содержат прежние имена; четыре проверки форматирования явных форм и проверки синхронизации словаря проходят. Итоговое решение 3633/3633. Новых измерений быстродействия файлового семейства нет.

### Session 1128 — подготовка FILE без расширения покрытия языка

RuntimeTextFile на уровне C# поддерживает явную кодировку и корректное UTF-16-чтение. 12 адресных Runtime-проверок и полное решение 3645/3645. Это пока не покрытие новой формы OPEN в BytecodeVm: парсер/связыватель/байткод ещё не передают кодировку. Прежние пользовательские формы сохранены, реестр остаётся с 309 дескрипторами. Новые языковые примеры и manual OPEN появятся на следующем этапе вместе с работающей формой USING encoding. Быстродействие FILE не измерялось.

### Session 1129 — OPEN USING encoding

Новая форма INPUT/OUTPUT/APPEND реализована в обоих движках без перехода на Interpreter. ExplicitTextEncodingTests проверяет все четыре кодировки, точные байты, позиции без BOM, скалярное чтение, отказ неверного имени и повреждённого UTF-16, неизменность файла и освобождение потока. BinderTests проверяет три режима и требование STRING, BytecodeModuleValidatorTests отвергает неправильный тип и признак кодировки. Подсветка использует существующий USING в обеих схемах, форматирование сохраняет переменную encoding. Manual OPEN, его поиск и samples/operators/open/explicit_utf16_read.axm обновлены; пример выполнен CLI. Отдельного измерения быстродействия новой формы нет.

### Session 1130 — отрицательные случаи OPEN USING

ExplicitTextEncodingTests: 32 варианта; добавлены UTF-16LE/BE, CR/LF/CRLF, байт 0A внутри Ċ/ਁ, эмодзи, нечётная позиция, середина пары, нулевой READTEXT, REWIND, LINENUMBER и строгая запись OUTPUT/APPEND с проверкой точных байтов. Исходные 6 падений выявили два исключения вне диагностики, исправленные в общем BuiltinFileIOHelpers. BytecodeModuleValidatorTests теперь отдельно принимает старую/новую формы OpenFile с глубиной стека 2/3 и отвергает 0/1/2 доступных значения через BC4053. Примеры write/strict_encoding_recovery.axm и linenumber/utf16_seek_recovery.axm, manual FILE и OPEN обновлены. Новых сущностей подсветки и измерений быстродействия нет.

### Session 1131 — независимая проверка 18 форм REGEX

RegexIndependentContractTests 118 вариантов: восемь имён/18 форм, обычные значения и типы результатов, пустой ввод, отсутствие совпадения, неверный pattern, неверный тип text, неизвестный modes, i/m/s/x, пустые совпадения, захваты разделителя, половины UTF-16, отсутствующие/явно нумерованные группы, строки замены и count. Исправлены RX26-001/002 общего BuiltinRegexHelpers. 118 REGEX и 32 уточнённых файловых проверки проходят в обоих движках без перехода на другой движок. Реальные отказы по пятиисекундному пределу каждой формы ещё требуют отдельной проверки.

Новые программы: regexextract/explicit_group_number.axm, regexextractall/explicit_group_values.axm, regexreplace/stop_at_count.axm; все прошли CLI и общий приёмочный набор. Manual четырёх страниц REGEX обновлён; обе схемы подсветки и три проверки форматирования охватывают изменённые существующие функции. Полное решение 3794/3794. Новых имён, кодов операций и отдельных измерений быстродействия нет.

### Session 1132 — настоящие отказы по времени всех форм REGEX

36/36 проверок реального DefaultTimeout=5 секунд для 18 форм обоих движков: MB3007 перехватывается программой. Быстрый набор 163/163 включает пустой pattern для каждой формы, group=0, отрицательные group/count, типы аргументов и культуры. Отдельный один тест проверяет совпадение Code/Line/Column/Span всех 18 пар диагностик без перехода VM на Interpreter. Текущий набор REGEX 199 вариантов выполнен частями; новый общий прогон не заявляется, код исполнителя после полного 3794/3794 не менялся.

Новый sample regexextract/pattern_backslash.axm прошёл CLI. Исправлены четыре неправильно экранированных примера manual, таблица REGEX_FORM_EVIDENCE_20261008.csv отражает каждую форму и границы доказательства. Новых токенов/функций/измерений быстродействия нет.

## Session 1133 — DIM DATE/TIME/DATETIME

Исправлена BC3001 для объявления временного скаляра без инициализатора. Два движка проверены на начальные значения и повторное объявление в цикле (DateTimeIndependentContractTests). Manual dim.html и samples/operators/dim/temporal_defaults.axm отражают форму. Нового измерения быстродействия нет.

## Session 1134 — строки и часы DateTime

16/16 адресных вариантов DateTimeIndependentContractTests; добавлены разбор в четырёх культурах, ошибочные зоны, четыре крайних UTC-преобразования и текущие формы на управляемых часах. VM не переходит на интерпретатор. Manual частей даты исправлен по реестру. Новых языковых форм нет.

## Session 1135 — дескрипторы DateTime и большие сдвиги

Все 46 форм проверены на обычный вызов обоими движками, точный связанный ID и тип; 69 вариантов неверных аргументов/арности сравнивают диагностики и Span. Проверены все токены арифметики DATETIME и 13 пар отказов. DT26-003 устраняет промежуточное переполнение TIME DATEADD, manual и large_time_shift.axm обновлены. Доказательства DATETIME_FORM_EVIDENCE_20261008.csv. Наличие обычных вызовов не доказывает все возможные границы.

## Session 1136 — строковый поиск и срезы

14 форм по точному ID/типу и результатам двух движков, 32 проверки неверных типов и 11 граничных вариантов; 57/57. Manual уточнён, instr/utf16_positions.axm прошёл CLI. Исполнитель не менялся. Доказательства STRING_SEARCH_RECHECK_20261008.md и CSV.

## Session 1137 — построение и разделение строк

15 форм Fill/Array/Edit проверены по ID/типу и результатам двух движков, 32 неверных аргумента и граничные случаи. Новый набор 53, вместе с поиском 110/110. Manual и split/unicode_empty_parts.axm обновлены, CLI код 0. Исполнитель не менялся; доказательства STRING_BUILD_RECHECK_20261008.md/CSV.

## Session 1138 — обрезка, регистр и коды символов

13 форм имеют независимые проверки связанного ID/типа, результатов обоих движков и неверных аргументов. Вместе с поиском/построением — 148/148. Реестр String сверён: 42 уникальные формы, 42 строки доказательств, пропусков и лишних ID нет. Все 256 кодов CP866 проверены обоими движками относительно независимого системного кодировщика. Выбранные границы Unicode, пустые строки, культуры и ошибки MB3007 подтверждены; все возможные значения Unicode не перебраны. Исполнитель не менялся, новых дефектов не подтверждено.

Manual characters и trim-and-case уточняет четыре формы кодирования, шесть форм обрезки, UTF-16, пустой набор, Unicode-пробелы, независимость регистра от культуры и замещение изолированного первого суррогата в ASC/ASCW. Для обратимого преобразования Unicode уже есть ASCW/CHRW; новый дубль не требуется. Пример samples/functions/ascw/unicode_round_trip.axm прошёл CLI, код 0: 159, 1071, TRUE, 128512, 2. Подробности STRING_CASE_RECHECK_20261008.md и CSV 13 форм. Новых токенов и измерений быстродействия нет.

## Session 1139 — числовые формы и знак нуля

Все 31 дескриптор Numeric проверены по связанному ID/типу и результатам двух движков: 61 обычный вариант с четырьмя числовыми типами, 37 неверных аргументов. 132/132 адресных проверок с границами/преобразованиями; две проверки пула констант. NUM26-001: VM терял знак нуля при объединении DOUBLE-констант. Ключ теперь содержит битовое представление, одинаковые биты продолжают использовать общий индекс. Подробности NUMERIC_RECHECK_20261008.md и NUMERIC_FORM_EVIDENCE_20261008.csv. Manual SETSIGN и samples/functions/setsign/signed_zero.axm отражают договор. Новых токенов и измерений быстродействия нет. Все значения NaN/внутренние входы и все сочетания продвижения типов ещё не проверены.

## Session 1140 — смешанные типы и NUM26-002

NumericPromotionContractTests проверяет все 96 сочетаний INTEGER/LONG/DOUBLE/DECIMAL для MIN/MAX/CLAMP: 74 допустимых вызова имеют правильный общий тип аргументов/результата и результат обоих движков, 22 сочетания DECIMAL+DOUBLE отклоняются при связывании с одинаковыми координатами. CSV NUMERIC_PROMOTION_EVIDENCE_20261008.csv сохраняет каждое сочетание. Дополнительно проверены LONG.MinValue/MaxValue, DECIMAL порядка 9e27, точность целочисленного выбора, ABS/SETSIGN на минимальном LONG и явное преобразование.

Исходный прогон — 96/98: оба движка выбросили InvalidOperationException на CDBL(CDEC(2)). NUM26-002 P1: EvaluateCdbl использовал AsDouble, который намеренно не выполняет неявного преобразования DECIMAL. Исправлен именно явный CDBL: для DECIMAL используется явное приведение к double, другие ветви сохранены. RuntimeValue.AsDouble не расширялся. Это дефект существующей функции, а не отсутствие API. Итоговый набор с прежними числовыми проверками 232/232 (.artifacts/numeric-1140-fixed.log/TRX).

NumericNonFiniteOperandTests: NaN, +Infinity, -Infinity искусственно передаются в общий ToDoubleMathOperand и отклоняются через callback аргумента и RuntimeFailureException до вычисления, координаты сохранены; 3/3. Это проверка общей границы математических функций, не всех внутренних путей SGN/MIN/SETSIGN. Прямое получение таких значений через VAL уже запрещено прежним договором.

NUM26-003 P4: manual ошибочно обещал отсутствие потерь CDBL для любого целого типа. LONG выше 2^53 может округляться. Исправлен договор и добавлен inline-пример CDBL(CDEC(...)), смешанные аргументы и явный выбор DOUBLE. samples/functions/cdbl/decimal_precision.axm показывает 2/9007199254740992/TRUE/2; окончательный CLI-запуск проверяется после пересборки. Первый редакционный ориентир — Зельднер CDBL (288), пример о влиянии исходной точности; запасной — [Microsoft Learn Type Conversion Functions](https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/functions/type-conversion-functions). Семантика округления CINT/CLNG из VB не переносилась.

Новых имён, токенов или измерений быстродействия нет. Все значения и комбинации операций не доказаны. Следующий срез — неагрегатные формы массивов: границы, выделение, копирование/срезы, сравнение доступных средств с реальными пробелами API. Общий аудит и визуальная приёмка IDE продолжаются.

## Session 1141 — сведения и выборка массивов

15 форм сведений/клона/срезов проверены по ID/типу/рангу и результатам двух движков, 28 неверных аргументов и выбранные границы. AW26-001 P2: ARRAYSLICE/ARRAYTAKE/ARRAYSKIP игнорировали явное измерение одномерного массива. Обе реализации теперь проверяют dimension до одномерного пути. 12 вариантов недопустимых измерений дают MB3007 и перехватываются; итог 57/57. Manual и arraytake/explicit_axis.axm отражают договор, доказательства ARRAY_WINDOW_RECHECK и CSV 15 форм. Предельные пустые диапазоны и остальные преобразования пока не доказаны. Новых токенов/измерений быстродействия нет.

## Session 1142 — AW26-002: крайние пустые многомерные диапазоны

Подтверждён P1: ARRAYSLICE(a, INTEGER.MinValue, 0, dimension) и полный ARRAYSKIP измерения с верхней границей INTEGER.MaxValue аварийно выходили с OverflowException в обоих движках. В start-1/upper+1 происходило переполнение INTEGER, а последующая попытка вычислить длину ошибочного непустого диапазона падала. CLI-воспроизведение .artifacts/array-1142-skip-max.axm содержит всего один элемент; причина не в большом выделении.

Первый тест ошибочно объявлял исходный массив с нижней границей INTEGER.MinValue и получил законный отказ: минимальная граница зарезервирована для поискового признака. Это не дефект Axiom. После замены исходной границы на MinValue+1 все четыре параметризованные проверки падали с OverflowException (.artifacts/array-1142-valid-before.log/TRX). Не расширялось множество допустимых исходных индексов.

Общий BuiltinArrayHelpers.CreateEmptyWindowBounds вычисляет запрос нижней границы как LONG. Когда выбранная пустая пара не представима допустимыми INTEGER-индексами, используется 1 TO 0; иначе сохраняется прежняя пара lower/lower-1. Обе реализации ARRAYSLICE/ARRAYSKIP используют общий расчёт. ARRAYTAKE не менялся: допустимая минимальная исходная нижняя граница MinValue+1 позволяет построить пустую пару без переполнения.

ArrayEmptyBoundaryContractTests — четыре параметризованные проверки по шесть случаев: 24 сочетания, оба движка и оба измерения. Проверены TAKE около MinValue+1, SLICE с MinValue/MAX и нулевой длиной, SKIP у MAX, обычные отрицательные пустые границы, выделение, длина/ранг и сохранность другого измерения. В сочетании с предыдущими сведениями/срезами 61/61 (.artifacts/array-1142-fixed.log/TRX), без перехода VM на другой движок. Это не доказательство всех рангов и всех числовых индексов.

Manual arrays-functions и arrays-and-builtins прямо описывает исключительную пустую форму, зарезервированную нижнюю границу и inline-пример. Новый samples/functions/arrayskip/empty_at_max_index.axm рассчитан на ручную приёмку. Редакционные ориентиры сохраняются: Зельднер, ограничения массивов (396), текущая официальная Learn о массивах; семантика VB не переносилась. Новых токенов/инструкций/измерений быстродействия нет. Новая функция языка не требуется: исправлено существующее построение результата.

Следующий шаг — явно переданные измерения у остальных преобразований массива, затем FLATTEN/RESHAPE/CONCAT и поиск/динамическое изменение. Общий аудит и визуальная приёмка IDE продолжаются.

## Session 1143 — AW26-003: явное измерение остальных преобразований

Подтверждён P2 у десяти функций: ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE/ARRAYBINARYSEARCH/ARRAYPUSH/ARRAYPOP/ARRAYINSERT/ARRAYREMOVE/ARRAYCONCAT. Одномерный путь обходил ResolveTransformDimension, а короткие пути сортировки также обходили проверку. Исходные 56/56 проверок не получили ожидаемого отказа (.artifacts/array-1143-before.log/TRX). Это дефект существующего параметра, а не отсутствие API.

Обе реализации теперь разрешают явно переданное измерение сразу после проверки массива и повторно используют результат в многомерном пути. Вызовы без измерения сохраняют прежний одномерный договор и существующую проверку обязательного номера для многомерного массива. Многомерный номер не проверяется дважды. Для допустимого выделенного массива отказ по номеру происходит до изменения длины/порядка/элементов.

ArrayTransformAxisContractTests: 56 вариантов недопустимых осей 0/2/MAX/MIN на десяти операциях, включая пустую и одноэлементную одномерную сортировку. Каждый сравнивает Code/Line/Column/Span обоих движков, TRY/CATCH и сохранность исходного массива. Десять обычных операций проверены с опущенным номером и номером 1, обоими движками: отдельные независимые результаты сортировки/разворота/поиска/изменения/объединения. Четыре дополнительные проверки пустой/одноэлементной двумерной сортировки проверяют оси 0/3 и допустимую 2, сохраняя границы другого измерения. Набор нового класса 70/70 (.artifacts/array-1143-short.log/TRX); вместе с предыдущими сведениями/срезами до этих четырёх дополнительных проверок 127/127. Итог полного решения записывается отдельно. Переход VM на интерпретатор запрещён.

Manual arrays-and-builtins содержит параметр dimension для десяти отдельных форм, контекст/диагностику и inline-пример отказа до изменения. Страницы ARRAYPUSH/POP/INSERT/REMOVE в arrays-functions уточнены напрямую. Новый samples/functions/arraysort/reject_invalid_axis.axm подготовлен для ручной приёмки. Первый ориентир — Зельднер, пузырьковая сортировка и вывод до/после (147–148); запасной официальный Microsoft Learn Array.Sort с примерами VB. Алгоритм пузырьковой сортировки и семантика .NET не перенесены в Axiom. Новых имён/токенов/инструкций и измерений быстродействия нет.

Все обычные многомерные формы десяти функций и все ошибки ранга/формы значения этим набором не доказаны. Далее независимые формы FLATTEN/RESHAPE/CONCAT и поиск/динамические срезы. Общий аудит и визуальная приёмка IDE продолжаются.


### Session 1144 — ARRAYCONCAT у последнего индекса INTEGER

AW26-004 P1: обе реализации многомерного ARRAYCONCAT выполняли сложение границ в INTEGER. На двух одноэлементных массивах с нижней границей 2147483646 промежуточная сумма переполнялась, хотя итоговая верхняя граница 2147483647 допустима. При начале 2147483647 результат действительно непредставим, но вместо перехватываемой диагностики возникало OverflowException.

Вычисление переведено в LONG; перед созданием результата превышение INTEGER.MaxValue даёт MB3007. Исходные массивы не меняются. Новый ArrayConcatBoundaryContractTests: восемь вариантов, две оси и оба движка, проверяет независимые границы/значения и TRY/CATCH. До исправления 8/8 падали; после исправления совместный набор 139/139. Новый русскоязычный пример samples/functions/arrayconcat/maximum_index.axm и прямое объяснение на manual arrays-and-builtins.html#concat-bounds. Новых имён, токенов и измерений быстродействия нет.

Редакционный ориентир: Зельднер, ограничения массивов, с. 396; запасной жанровый ориентир — https://learn.microsoft.com/ru-ru/dotnet/visual-basic/programming-guide/language-features/arrays/ . Ограничения VB/QuickBASIC не перенесены в Axiom. Общая проверка ARRAYFLATTEN/ARRAYRESHAPE, пустых форм и представлений ещё не завершена.


## Session 1145 — порядок и форма ARRAYFLATTEN/ARRAYRESHAPE/ARRAYCONCAT

ArrayShapeContractTests проверяет 10 успешных сценариев и 3 отказа размеров с независимыми ожидаемыми значениями в обоих движках: порядок последнего измерения, перенос INTEGER из 2x3 в 3x2, разные/отрицательные границы, одномерные представления, независимость INTEGER-элементов результата от источника/образца, пустой выделенный FLATTEN и пустую форму RESHAPE, CONCAT по двум осям, объединение с пустой частью. Семь неверных типов и пять невыделенных аргументов сравнивают диагностики/координаты. Пять форм отдельно проверяют точный BuiltinId, тип INTEGER и ранг результата; CSV ARRAY_SHAPE_FORM_EVIDENCE_20261008.csv. Это не доказательство всех типов элементов, рангов и вложенных пользовательских структур.

AW26-005 P4: manual ограничивал source у ARRAYRESHAPE словом «плоский», хотя Binder и оба исполнителя поддерживают исходный массив любого ранга. Исправлено описание, добавлены прямые разделы FLATTEN/RESHAPE с параметрами, порядком, выделением, пустой формой, диагностикой и inline-примерами. samples/functions/arrayreshape/matrix_to_matrix.axm подтверждает перенос 11/12/13/21/22/23 без предварительного FLATTEN и сохранность исходного 11 после изменения результата. CLI код 0. Это уже существующая возможность, не пробел API; дополнительная функция преобразования таблицы не нужна.

Одномерный CONCAT с двумя непустыми частями компактен, с пустой частью сохраняет границы другой части (4 TO 6 подтверждено). Это существующий договор, не исправление исполнения. Runtime в этом этапе не менялся. Редакционный ориентир — Зельднер, массивы/границы и ограничения с.396; запасной текущий официальный Learn о массивах https://learn.microsoft.com/ru-ru/dotnet/visual-basic/programming-guide/language-features/arrays/ . Новых токенов, инструкций и измерений быстродействия нет.

Далее независимо проверить многомерные динамические ARRAYPUSH/POP/INSERT/REMOVE: форму добавляемых/удаляемых срезов, представления, отказ по несовместимости и крайние границы; затем поиск и недостающие возможности. Общий аудит продолжается.


### Session 1146 — ARRAYINSERT в пустую нестандартную форму

AW26-006 P1: ARRAYINSERT требовал index=1 для пустого выбранного измерения, но сохранял нижнюю границу пустой формы. При ARRAYTAKE(seed,0,dimension) с исходным началом -4 или 9 вставка обращалась вне массива и аварийно завершалась. Четыре проверки после исправления неверного имени переменной (ROW — встроенная константа) воспроизвели дефект обоих движков: .artifacts/array-1146-reproduce.log/TRX. Ошибка имени в первой пробе не считалась дефектом языка.

Обе реализации при пустом выбранном измерении создают границы 1 TO 1 и не сдвигают отсутствующие элементы; остальные границы сохраняются. Одномерный путь также согласован с index=1, включая пустую форму, полученную через ARRAYRESHAPE. Непустые пути сохранены. ArrayEmptyInsertContractTests: восемь параметризованных вариантов с двумя осями (16 сочетаний) и четыре одномерные проверки, оба движка, начала -4/9/MinValue+1/MaxValue. Совместный набор 181/181, без перехода на интерпретатор. Промежуточный прогон array-1146-target застал изменение только интерпретатора и не считается итоговой проверкой.

Manual arrays-functions.html#arrayinsert содержит прямое правило пустой формы и inline-пример. Добавлен русскоязычный samples/functions/arrayinsert/empty_dimension.axm. Редакционный ориентир — массивы и границы у Зельднера, запасной официальный Learn Array.Resize с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.resize?view=net-10.0 ; правила .NET не перенесены. Новых токенов/инструкций/имён и измерений быстродействия нет.

Далее продолжить обычные многомерные PUSH/POP/INSERT/REMOVE, совместимость формы значения, сохранность отказов и расширение у максимальной границы. Эти вопросы ещё не объявлены доказанными; общий аудит активен.


### Session 1147 — обычные многомерные динамические операции

ArrayDynamicShapeContractTests содержит 22 проверки. Восемь форм четырёх операций по двум осям проверяют независимо рассчитанные значения, отрицательные границы выбранного измерения, сохранение остальных границ 7 TO 8 и границы возвращаемого среза POP/REMOVE. Четыре отказа несовместимой формы/индекса сравнивают Code/Line/Column/Span обоих движков, TRY/CATCH и все четыре исходных INTEGER-элемента. Два случая проверяют представление того же массива как значение PUSH/INSERT: исходный срез переносится без порчи при сдвиге.

Четыре варианта engine/axis проверяют PUSH и INSERT у INTEGER.MaxValue: MB3007 перехватывается до изменения длины, границ и значений. Ещё четыре проверки запрещают изменение размера представления через каждую функцию, сохраняя массив-владелец. Внутри каждой проверки обычных операций/отказов работают оба движка; переход VM на интерпретатор запрещён. Итог нового класса 22/22, совместного набора 203/203 (.artifacts/array-1147-final.log/TRX), без ошибок и пропусков. Код Runtime не менялся; полное решение предыдущего этапа 4580/4580, повторный общий прогон в этом этапе не выполнялся. git diff --check чистый.

Нового дефекта исполнения в проверенных сценариях не обнаружено. Добавление/удаление строки или столбца уже выполняется ARRAYPUSH/POP/INSERT/REMOVE с номером измерения; отдельная функция для этой операции не нужна. Запускаемые multidimensional_by_column.axm в тематических папках уже существуют и были выполнены в Session 1146. Manual arrays-functions содержит соответствующие формы; новой семантики/токенов/инструкций/измерений быстродействия нет. Это не доказательство всех типов элементов, рангов, всех разновидностей представлений и крупных выделений памяти.

Далее независимая проверка поиска ARRAYCONTAINS/ARRAYINDEXOF/ARRAYLASTINDEXOF/ARRAYFINDALL/ARRAYBINARYSEARCH: отрицательные и крайние границы, отсутствие результата, повторы, координаты многомерного результата и пустые формы. Затем оставшиеся функции массивов и реальные пробелы API по общей матрице. Общий аудит продолжается.


### Session 1148 — независимый поиск в массивах

ArraySearchContractTests: 22 проверки обоих движков. Одномерные INTEGER-массивы с повтором проверены при нижних границах -4/5/MinValue+1/MaxValue-2; одиночный элемент у MaxValue найден без переполнения. INDEXOF/LASTINDEXOF/BINARYSEARCH возвращают исходный индекс, при отсутствии — lower-1, включая INTEGER.MinValue. FINDALL сохраняет найденные исходные индексы в компактном массиве. CONTAINS/COUNT дают независимые TRUE/FALSE/число.

Проверены двумерные координаты первого/последнего совпадения, порядок таблицы FINDALL 3x2, выделенные пустые координаты, сохранение двух столбцов пустого FINDALL и пустой исходный массив. Бинарный поиск по двум осям проверен на таблице, отсортированной по обеим осям: axis=1 даёт (-1,4), axis=2 — (-2,5). Это первая подходящая линия выбранного измерения, не обещание единого первого совпадения обычного обхода.

Шесть невыделенных аргументов и шесть несовместимых BOOLEAN-значений сравнивают диагностики/координаты обоих движков. Unicode STRING-проверка отличает Ёж/ёж/Еж и находит целый 😀, сохраняя логические индексы. Переход VM на интерпретатор запрещён. Новый класс 22/22; совместный набор 225/225 (.artifacts/array-1148-final.log/TRX), без ошибок/пропусков. Код Runtime не менялся; последний полный прогон предыдущего этапа 4580/4580, в этом этапе общий прогон не повторялся.

AW26-007 P4: таблица manual ошибочно обещала 0 при отсутствии совпадения независимо от нижней границы. Обе реализации уже возвращали lower-1. Исправлены три строки и прямое объяснение результата, многомерных координат, FINDALL, бинарного поиска по линиям и диагностик. Новый русскоязычный samples/functions/arrayindexof/negative_missing_index.axm прошёл CLI с кодом 0: -5/-5/-5/-4/-3. Существующий поиск не требует нового API для обнаружения первого/последнего/всех совпадений.

Редакционный ориентир по границам — Зельднер, с.396; самих современных функций у него нет, запасной жанровый ориентир — официальный Learn Array.IndexOf с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.indexof?view=net-10.0 . Семантика подтверждена Axiom, не перенесена из .NET. Новых имён/токенов/инструкций и измерений быстродействия нет. Все типы/ранги и виды представлений не объявлены доказанными.

Далее ARRAYDISTINCT и функции с пользовательскими функциями ARRAYEVERY/SOME/FILTER/MAP/REDUCE: пустые формы, число вызовов, порядок, крайние индексы и ошибки пользовательской функции. Затем незакрытые семейства общей матрицы и реестр отсутствующих возможностей. Общий аудит активен.


### Session 1149 — последний индекс пользовательской обработки массива

AW26-008 P1: одномерные быстрые пути BytecodeVm для ARRAYEVERY/SOME/FILTER/MAP/REDUCE выполняли index++ после обработки INTEGER.MaxValue. Переполненный индекс приводил к аварийному обращению вне массива. Пять проверок одноэлементного массива до исправления проходили в Interpreter и падали в BytecodeVm (.artifacts/array-1149-before.log/TRX).

Каждый из пяти обходов VM теперь завершает цикл при index==upperBound до увеличения. Ранние возвраты EVERY/SOME и проверка пустого диапазона сохранены; Interpreter уже использовал безопасный общий обход и не менялся. ArrayCallbackBoundaryContractTests проверяет пять операций у MAX и ровно один вызов пользовательской функции, пять пустых операций без единого вызова и два ранних завершения на первом решающем элементе. Оба движка проверены без перехода на интерпретатор. Новый класс 12/12; совместный набор 237/237 (.artifacts/array-1149-final.log/TRX), без ошибок/пропусков.

Manual arrays-and-builtins объясняет пять отдельных функций, аргументы, пустые результаты, раннее завершение и допустимый последний индекс; добавлен inline-пример. Новый русскоязычный samples/functions/arraymap/maximum_callback_index.axm проверен CLI, код 0: TRUE/FALSE/42/84/50; SHA256 Runtime в CLI совпал с актуальной библиотекой. Новых имён/токенов/инструкций и измерений быстродействия нет.

Редакционный ориентир — Зельднер, FUNCTION и возвращаемое значение (158–159); самих современных функций у него нет. Запасной жанровый ориентир — официальный Learn Array.TrueForAll с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.trueforall?view=net-10.0 . Семантика подтверждена кодом и независимыми тестами Axiom; правила SHARED и исторический способ возврата не перенесены.

Далее ARRAYDISTINCT и оставшиеся обычные/многомерные формы FILTER/MAP/REDUCE: порядок, изменение типа результата, ошибки пользовательской функции, несовместимые сигнатуры. Затем прочие незакрытые семейства общей матрицы и недостающие возможности. Все типы/ранги и формы представлений этим исправлением не доказаны. Общий аудит активен.


### Session 1150 — порядок преобразования и ошибка пользовательской функции

AW26-009 P1: BytecodeVm при ошибке синхронно вызываемой пользовательской функции немедленно находил внешний обработчик вызывающего кадра. InvokeBytecodeFunction ещё ожидал результат и затем выдавал ложную диагностику отсутствия RETURN. Независимая ARRAYMAP-проба проходила в Interpreter, падала в Bytecode (.artifacts/array-1150-error-mode.log/TRX); деление на ноль должно сохранять MB3001. Первоначальное имя Append конфликтовало с ключевым словом, исправлено на AccumulateText и не считалось дефектом.

ExecuteCurrentInstruction принимает необязательную глубину границы обработки ошибки. Синхронный InvokeBytecodeFunction передаёт глубину вызывающего стека; TryHandleRuntimeFailure обрабатывает только кадры выше этой границы. Если обработчика внутри функции нет, ошибка возвращается к инструкции встроенного вызова, где внешний обработчик доступен обычным путём. Состояние границы передаётся параметром, а не общим изменяемым полем. Вложенные вызовы и FINALLY сохраняют порядок.

ArrayCallbackOrderContractTests (13/13) проверяет четыре многомерных операции, порядок 1234, INTEGER→STRING MAP с сохранением границ, STRING-накопитель REDUCE, фильтр чётных элементов, DISTINCT, пять неподходящих сигнатур, исходный MB3001 и остановку после второго элемента, обработчик внутри функции с FINALLY, вложенный ARRAYREDUCE внутри MAP с FINALLY и необработанный исходный код/координаты. ArrayDistinctValueContractTests (4/4) проверяет соседние LONG/DECIMAL выше 2^53 без объединения, Unicode-регистр/эмодзи/NFC-NFD и пустой выделенный результат. Оба движка без перехода на интерпретатор; совместный набор 254/254 (.artifacts/array-1150-final.log/TRX).

Manual arrays-and-builtins содержит прямое правило передачи ошибки и inline-пример; новый русскоязычный samples/functions/arraymap/catch_callback_error.axm. Архитектурное правило границы синхронного вызова записано в ARCHITECTURE. Новых имён/токенов/инструкций и измерений быстродействия нет. Редакционный ориентир — Зельднер, FUNCTION 158–159; запасной официальный Learn Array.ConvertAll с VB-примером https://learn.microsoft.com/en-us/dotnet/api/system.array.convertall?view=net-10.0 . Семантика подтверждена Axiom; правила .NET не перенесены.

Далее сопоставить оставшиеся зарегистрированные Array-формы с накопленными свидетельствами, проверить ARRAYFILL и многомерные сортировки/развороты, затем перейти к незакрытым семействам общей матрицы и реальным отсутствующим возможностям. Обработка всех видов событий/всех ошибок пользовательской функции этим набором не доказана. Общий аудит активен.


### Session 1151 — сортировка на предельном индексе и реестр массивов

AW26-010 P1: общий GetStableSortedLine увеличивал INTEGER-индекс после обработки последнего элемента с индексом 2147483647. Переполнение приводило к обращению за границы массива. До исправления восемь независимых случаев ARRAYSORT/ARRAYTOSORTED по обеим осям падали в обоих движках. Цикл теперь завершает обход сразу после верхней границы.

ArraySortBoundaryContractTests содержит восемь проверок предельной границы; ArrayFillSortReverseContractTests содержит десять проверок сортировки и разворота по двум осям, сохранения исходника при копировании, заполнения через представление, пустого массива и единственного элемента на предельном индексе. Совместный адресный набор: 272/272. Полное решение: 4674/4674, девять TRX, без ошибок и пропусков, код процесса 0 (.artifacts/array-1151-full.log, .artifacts/test-results/array-1151-full/). git diff --check без ошибок.

Manual arrays-and-builtins прямо объясняет сортировку по осям, сохранение границ и отличие копирования от изменения исходника. Русскоязычный samples/functions/arraysort/maximum_line_index.axm выполнен актуальным CLI с кодом 0: копия 10/30, исходник до изменения 30, после сортировки 10/30. SHA256 библиотеки Runtime совпал с копией CLI. Редакционный ориентир: Зельднер, сортировка, страницы 147–148; запасной жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/system.array.sort?view=net-10.0 . Семантика проверена по Axiom.

ARRAY_REGISTRY_EVIDENCE_20261008.csv сопоставляет 38 зарегистрированных имён (53 регистрации в исходнике) с существующими наборами проверок. Это указатель свидетельств по именам, а не доказательство всех 53 форм вызова, типов, рангов и представлений. ARRAYFILL и сортировка/разворот по осям уже доступны существующим API; отдельные новые функции для проверенных сценариев не требуются. Новых токенов языка и измерений быстродействия нет.

Следующий шаг: проверить соответствие каждой из 53 зарегистрированных форм точным вызовам и результатам тестов, выделить неподтверждённые формы; затем продолжить незакрытые семейства общей матрицы и поиск полезных отсутствующих возможностей. Общий аудит остаётся активным.


### Session 1153 — успешные формы массивов без явной оси

Добавлен ArrayDefaultFormEvidenceTests: восемь форм ARRAYTOSORTED, ARRAYTOREVERSED, ARRAYSORT, ARRAYREVERSE, ARRAYPUSH, ARRAYINSERT, ARRAYPOP, ARRAYREMOVE без аргумента оси. Каждая проверяет точный BuiltinId, тип результата (одномерный INTEGER-массив, INTEGER или VOID), независимый ожидаемый порядок значений и исполнение обоих движков без перехода на интерпретатор. Исходный массив имеет границы 5..7 и значения 30/10/20; копирующие операции сохраняют первый исходный элемент, изменяющие дают ожидаемый порядок. Проверяется возврат 20 при POP и 10 при REMOVE(6).

Новый набор 8/8; вместе с ArrayWindowFormEvidenceTests — 65/65, без ошибок и пропусков, код 0 (.artifacts/array-1153-forms.log и .artifacts/array-1153-combined.log/TRX). Исполняемый код и договор языка не менялись. Дополнительный полный прогон решения для добавленных независимых тестов не выполнялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv внесены отдельные VerifiedEvidence и VerifiedScope: 23 из 53 форм подтверждены сочетанием точного связывания, типа и успешного ожидаемого вывода (15 форм границ/окон, 8 новых). Остальные 30 остаются кандидатами, а не считаются непроверенными вообще: их ранее накопленные проверки ещё требуют отдельного сопоставления. Все типы, ранги и представления этим результатом не доказаны. Новые языковые средства для данных сценариев не нужны: существующие формы работают.

Далее подтвердить остальные 30 форм, начиная с поиска и функций пользовательского преобразования, затем перейти к незакрытым семействам общей матрицы и отсутствующим возможностям. Общий аудит активен.


### Session 1154 — связывание форм поиска и координатных результатов

Добавлен ArraySearchFormEvidenceTests: 12 успешных случаев для семи зарегистрированных форм CONTAINS/COUNT/INDEXOF/LASTINDEXOF/FINDALL/BINARYSEARCH. Проверяются точный BuiltinId, скалярный или массивный тип, INTEGER-элементы и ранг координатного результата, независимые ожидаемые логические индексы и координаты в обоих движках без перехода на интерпретатор. Первое совпадение в одномерном массиве -4, последнее -3; многомерные координаты первого/последнего совпадения (-2,8)/(-1,7), FINDALL возвращает обе пары.

Первоначальная проба ошибочно ожидала многомерный ARRAYBINARYSEARCH без оси; Binder законно выдал MB2003. Это не дефект: многомерная форма требует dimension. Проба заменена успешным вызовом с осью 1; отдельная отрицательная проверка сохраняет MB2003 для отсутствующей оси. Итоговый набор вместе с ArraySearchContractTests: 35/35, без ошибок и пропусков, код 0 (.artifacts/array-1154-final.log/TRX). Исполняемый код/семантика не менялись; полный прогон не повторялся.

В ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv подтверждено 30 из 53 форм в явно ограниченном объёме; 23 требуют дальнейшего сопоставления. Поиск координат уже доступен существующим API. Далее формы функций пользовательского преобразования, затем оставшиеся формы массивов и незакрытые семейства/отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1155 — типы результатов пользовательского преобразования массива

ArrayCallbackOrderContractTests дополнен шестью независимыми проверками точного BuiltinId и типа результата: EVERY/SOME возвращают BOOLEAN, FILTER/DISTINCT — одномерный INTEGER-массив, MAP с Label — двумерный STRING-массив, REDUCE со строковым начальным значением — STRING. Существующие успешные программы этих форм проверяют ожидаемые значения и порядок обхода; добавлены EVERY/SOME с ранней остановкой: FALSE/один вызов/след 1 и TRUE/два вызова/след 12 соответственно.

Совместно с ArrayCallbackBoundaryContractTests и ArrayDistinctValueContractTests выполнено 37/37 без ошибок и пропусков, код 0 (.artifacts/array-1155-callback.log/TRX). Включены прежние проверки передачи ошибки, вложенного вызова и FINALLY, пустых массивов, предельного индекса, точных LONG/DECIMAL и Unicode DISTINCT. Исполняемый код и семантика языка не менялись; полный прогон не повторялся.

Реестр ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv теперь содержит ограниченное подтверждение 36 из 53 форм. Тип возвращаемого значения MAP/REDUCE определяется пользовательской функцией/накопителем и уже поддерживается существующим API; добавлять отдельные STRING-варианты не требуется. Оставшиеся 17 форм: многомерные изменения/сортировки, заполнение, преобразование формы/соединение и агрегаты. Следующий шаг — точное сопоставление этих форм с существующими независимыми проверками и добавление отсутствующих проверок связывания; затем другие незакрытые семейства и отсутствующие возможности общего аудита. Общий аудит активен.


### Session 1156 — завершена сверка 53 зарегистрированных форм массивов

ArrayRemainingFormEvidenceTests добавляет 17 проверок точного BuiltinId и типа/ранга результата для оставшихся форм: сортировка/разворот с осью, заполнение, многомерное изменение размера, FLATTEN/RESHAPE/CONCAT и четыре агрегата. Многомерные POP/REMOVE возвращают одномерный срез; MAP/RESHAPE/CONCAT не подменяются скалярным типом. Дополнительно INTEGER-матрица 1/2/0/4 исполняется обоими движками: SUM=7, MIN=0, MAX=4, AVERAGE=1.75, исходные значения сохранены.

Вместе с независимыми ArrayFillSortReverseContractTests, ArrayDynamicShapeContractTests, ArrayShapeContractTests, AuditRemainingBehaviorTests и ArrayAggregateContractTests выполнено 117/117, без ошибок и пропусков, код 0 (.artifacts/array-1156-remaining.log/TRX). Исполняемый код/семантика не менялись, полный прогон не повторялся. git diff --check без ошибок.

ARRAY_DESCRIPTOR_EVIDENCE_20261008.csv содержит свидетельства точного связывания и успешного ожидаемого выполнения для всех 53 регистраций (38 имён). Это завершение сверки зарегистрированных форм в указанном тестами объёме, а не доказательство всех типов, рангов, представлений, размеров и возможных комбинаций. Предыдущие числовые/Unicode/пустые/предельные проверки остаются дополнительными свидетельствами. Новые дубли существующих операций массивов не нужны.

Следующий шаг общего аудита — вернуться к отсутствующим возможностям: независимо уточнить G02 (строгая проверка числового текста) и существующие альтернативы, не менять префиксный VAL; затем G03 двоичный доступ и G04 Unicode с проверкой реальных сценариев. Остальные семейства общей матрицы также остаются в объёме. Общий аудит активен.


### Session 1158 — ISNUMERIC: полный числовой текст

G02 реализован в узком объёме: ISNUMERIC(text AS STRING) AS BOOLEAN проверяет полный инвариантный десятичный разбор в конечный DOUBLE. Использует общий BuiltinStringHelpers.IsNumeric для Interpreter и BytecodeVm (NumberStyles.Float, InvariantCulture, double.IsFinite). Обычные краевые пробелы U+0020 и U+0009–U+000D, знак, точка и экспонента разрешены; запятая, групповые разделители, валютные знаки, формы &H/&O, Unicode-цифры, NaN/Infinity и переполнение не принимаются. Пустая строка FALSE. Округление очень малого числа в конечный ноль TRUE; проверка не обещает точности либо пригодности для INTEGER/LONG/DECIMAL. Префиксный VAL сохранён. Новая функция не вводит строгого преобразования либо типовых перегрузок.

BuiltinFunctionId.IsNumeric добавлен в конец enum, без изменения существующих числовых значений сериализации. Дескриптор CreateDefault требует STRING и возвращает BOOLEAN. Оба движка используют один обработчик. Обе схемы IDE обновлены в алфавитном порядке; форматирование и словарь проверяются отдельной проверкой IsNumericIsHighlightedAndFormattedAsBuiltin и общей синхронизацией реестра.

IsNumericContractTests: 30 числовых/текстовых случаев в en-US/ru-RU/tr-TR и обоих движках, четыре неверные формы аргументов, точный BuiltinId/BOOLEAN и сохранение VAL. Совместно с NumericTextAlternativeEvidenceTests адресный набор 44/44. Первоначальная сборка выявила отсутствующий using, исправлен; первоначальные две проверки IDE выявили порядок слова в схемах, исправлен. Это ошибки текущего внесения, не отдельные ранее существовавшие дефекты. Полный прогон решения запущен: .artifacts/numeric-1158-full.log/TRX; результат будет дописан после завершения.

Manual isnumeric.html содержит назначение, контекст, синтаксис, аргумент, результат, ограничения, диагностику и inline-пример. Обновлены навигация, поисковый/алфавитный индекс и ссылки из числовых разделов. Русскоязычный samples/functions/isnumeric/full_number_check.axm показывает полный текст против префикса VAL, ноль, переполнение, точку/запятую и округление к нулю; выполнение будет подтверждено актуальным CLI.

Имя выбрано по подтверждённому прецеденту Visual Basic/VBA, а не заявлено как функция классического QuickBASIC. Поиск в доступном тексте Зельднера не выявил ISNUMERIC; VAL не предоставляет полного предиката. Основной жанровый/именной ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.information.isnumeric?view=net-10.0 и VBA https://learn.microsoft.com/en-us/office/vba/language/reference/user-interface-help/isnumeric-function . Договор Axiom сознательно уже VB: только STRING, конечный инвариантный десятичный DOUBLE; числовые/BOOLEAN-аргументы и шестнадцатеричные/восьмеричные формы VB не перенесены.

После завершения проверок следующий шаг — G03: независимо проверить отсутствие общего двоичного доступа и реальные сценарии, затем G04 Unicode и прочие незакрытые семейства общей матрицы. Общий аудит активен.


Дополнение Session 1158: системный double.TryParse принимает завершающий U+0000. Пять независимых HiddenCharacters-проверок до защиты дали 4/5: строка «12» + CHRW(0) ошибочно считалась полной числовой строкой. ISNUMERIC теперь явно отвергает U+0000 в любой позиции. После защиты адресный набор 49/49. Подтверждены допустимость U+0009/U+000D и отказ для U+00A0/U+2000; manual уточнён. Первый полный прогон завершился с кодом 0 до этой дополнительной защиты; итоговый полный прогон окончательного кода запущен отдельно (.artifacts/numeric-1158-final-full.log/TRX), его результат пока ожидается.


Итог Session 1158: окончательная версия с запретом U+0000 прошла полное решение 4774/4774, девять TRX, без ошибок и пропусков, код 0 (.artifacts/numeric-1158-final-full.log/TRX). Адресный набор 49/49. IDE 601/601, включая словарь/форматирование новой функции. Русскоязычный full_number_check.axm выполнен актуальным CLI с кодом 0: FALSE/12/TRUE/FALSE/TRUE/FALSE/TRUE; SHA256 Runtime совпал с актуальной библиотекой. Отдельная CLI-проба U+0000/U+00A0/U+0009 дала FALSE/FALSE/TRUE. git diff --check чистый.

Общий audit-bytecode-coverage выполнен актуальной сборкой инструмента: все зарегистрированные варианты имеют путь VM; ISNUMERIC не указан среди пропусков manual или samples. Общий результат FAIL: 22 отсутствующих записи manual-индекса, 27 отсутствующих тематических каталогов samples и два нераспознанных заголовка «Функция PMAP»/«Функция POINT», относящиеся к графическим функциям. Полный отчёт .artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-191052.txt. Эти расхождения не исправлены данной числовой правкой и должны быть разобраны отдельно в общем аудите. Общий аудит не объявляется завершённым.

Дальше: G03/G04 и отдельная сверка отмеченных графических manual/sample-пропусков с фактическими страницами и тематическими примерами. G02 закрыт в выбранном узком объёме ISNUMERIC; строгое преобразование произвольного целевого типа не добавлялось.


### Session 1159 — исправлена полнота инструмента проверки покрытия

COV26-001 P2: BytecodeCoverageAudit перечислял enum и TryGetDescriptor(id), который возвращает только первый дескриптор ID. Поэтому перегрузки с тем же ID и другим числом аргументов не проверялись, хотя отчёт обещал все варианты. Теперь инструмент перечисляет BuiltinFunctions.All и TryGetDescriptors(name), проверяет каждую регистрацию и выводит число вариантов: текущий реестр 250 имён/310 вариантов.

COV26-002 P3: индекс распознавался только по kind «Функция» и полному совпадению title с именем. Заголовки «Функция PMAP/POINT» и шесть групповых записей kind «Функции» ошибочно давали 22 пропуска и две лишние записи. В групповых строках search-index.js добавлено явное поле functions с именами сущностей. Инструмент поддерживает это поле и нормализует префикс «Функция » одиночного заголовка. Неизвестное явно указанное имя по-прежнему является ошибкой; произвольные ключевые слова не считаются свидетельством документации.

BytecodeCoverageAuditContractTests проверяет групповой индекс/префикс/полный счёт вариантов, неизвестное имя и сохранение отказа при отсутствующем тематическом каталоге. Три проверки 3/3, код 0 (.artifacts/coverage-1159-contract.log/TRX). Исходник инструмента подключён в тестовый проект через Compile Link; исполняемый язык не менялся. git diff --check без ошибок. Полное решение после этой правки инструмента не повторялось.

Фактический audit-bytecode-coverage: все 310 вариантов имеют VM-путь; все 250 имён найдены в manual-индексе, лишних индексных имён нет. Общий FAIL сохраняется из-за 27 отсутствующих каталогов samples/functions/<имя> (.artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-191448.txt). Наличие каталога само по себе не доказывает запуск/содержание, а его отсутствие не означает отсутствие примеров в другом тематическом разделе.

GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит кандидаты для этих 27 имён: для 18 найден текст вызова в существующих samples, для девяти прямого текстового кандидата нет. Это не подтверждение выполнения: комментарии, ошибки, зависимые ресурсы и графический контекст требуют чтения/запуска. При чтении встретились старые CP1251-примеры; они не изменялись.

Следующий шаг: разобрать девять имён без прямого кандидата и 18 существующих графических кандидатов, добавлять только содержательные короткие тематические примеры, не дублировать большие игры ради зелёного отчёта. G03/G04 и остальные семейства общего аудита остаются в объёме. Общий аудит активен.


### Session 1160 — запускаемые примеры свойств TILESET и TILEMAP

Для девяти функций без прямого текстового кандидата добавлены короткие русскоязычные примеры в соответствующих samples/functions/: TILECOUNT/TILEWIDTH/TILEHEIGHT — atlas_property.axm; MAPPIXELWIDTH/MAPPIXELHEIGHT/MAPTILEWIDTH/MAPTILEHEIGHT — map_property.axm; MAPTILESET — cell_tileset_index.axm; MAPTILEFLAGS — changed_cell_flags.axm. Примеры используют существующие graphics/sprites/assets/walk_sheet.png (32×32, четыре тайла 16×16) и operators/drawtilemap/simple_map.tmj (2×2 клетки). Новые ресурсы/копии файлов не созданы.

TilePropertyAcceptanceSampleTests читает настоящие файлы .axm, запускает ProgramRunService с ProgramConsoleService и sourceFilePath, то есть использует реальный загрузчик ресурсов IDE и разрешение относительных путей. Все девять программ выполнены в Interpreter и Bytecode без перехода на другой движок: 18/18, код 0 (.artifacts/graphics-1160-acceptance.log/TRX). Ожидаемые результаты: TILECOUNT=4, TILEWIDTH/HEIGHT=16, MAPPIXELWIDTH/HEIGHT=32, MAPTILEWIDTH/HEIGHT=16; MAPTILESET для заполненной/пустой/внешней клетки 1/0/0; MAPTILEFLAGS до изменения/после SETMAPTILE с маской 3/вне карты 0/3/0. Графическое окно не требуется, визуальная отрисовка этим не доказана. CLI не загружает TILESET/TILEMAP, потому проверка сделана через IDE. Файл карты не перезаписывается.

Manual tilesets.html/tilemaps.html дополнен ссылками и объяснением запуска через IDE; семантика не менялась. Для современных ресурсов у Зельднера нет прямого раздела; ближайший официальный жанровый ориентир для доступа к ресурсам и их свойствам — https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/objects/my-resources-object . Семантика .NET-ресурсов в Axiom не переносилась.

Реестр GRAPHICS_SAMPLE_CANDIDATES_20261008.csv обновлён: девять VerifiedSample с точным объёмом подтверждения. Повторный audit-bytecode-coverage (.artifacts/manual-checks/axiom-bytecode-coverage-audit-20261008-192017.txt): 250 имён/310 вариантов, индекс полный, VM-пути полные; отсутствующих каталогов стало 18 вместо 27. Общий FAIL пока сохраняется. git diff --check чистый. Полное решение после добавления этих примеров не повторялось; исполняемый код не менялся.

Далее разобрать оставшиеся 18 графических кандидатов, сделать содержательные тематические примеры и подтвердить исполнение в нужном контексте. G03/G04 и прочие семейства общего аудита остаются в объёме. Общий аудит активен.


### Session 1161 — закрыты оставшиеся каталоги графических функций

Добавлены 18 коротких русскоязычных примеров: IMAGEWIDTH/HEIGHT/LOADIMAGE, SPRITEWIDTH/HEIGHT/COUNT/LOADSPRITE, LOADTILESET/LOADTILEMAP, MAPWIDTH/HEIGHT/LAYERCOUNT/TILE, RGB, SCREENISACTIVE/MODE/WIDTH/HEIGHT. Ресурсные примеры используют существующие walk_sheet.png и simple_map.tmj, не копируют ресурсы и не требуют графического окна. RGB(1,2,3)=66051. Размер изображения 32×32, кадр 16×16, четыре кадра, карта 2×2 и три слоя; MAPTILE(1,2,1)=2.

TilePropertyAcceptanceSampleTests теперь исполняет все 27 тематических программ в обоих движках через настоящий ProgramRunService/ProgramConsoleService и разрешение sourceFilePath, без перехода на другой движок. Дополнительная пара проверок различает исходный нулевой размер экрана и явный SCREEN 0 с размером 640×300. Итог 56/56, код 0 (.artifacts/graphics-1161-final.log/TRX). Первоначальные ошибки примеров — зарезервированные image/sprite как имена переменных и ошибочное ожидание обычной панели вывода после SCREEN 0 — исправлены, дефектами языка не считались. После отказа от явного SCREEN выявлено неверное ожидание размеров 640×300 до его вызова: RuntimeInputSnapshot действительно начинается с 0×0. Ожидание сверено с кодом, отдельно проверено состояние после SCREEN 0; исполнитель не менялся.

Manual screen-functions уточняет: до первого оператора SCREEN размеры 0×0, после SCREEN 0 — 640×300, SCREENISACTIVE=FALSE. PRINT после SCREEN 0 направляется в экранное текстовое окно. В шести графических разделах добавлены ссылки на примеры; индексные имена уже присутствовали. GRAPHICS_SAMPLE_CANDIDATES_20261008.csv содержит подтверждённые пути для всех 27 исходных пропусков и пределы проверки. Визуальная отрисовка/масштабирование/все графические формы этими примерами не доказаны.

Редакционный ориентир по экранному режиму — Зельднер, SCREEN, страница 228; современный запасной жанровый ориентир по свойствам экрана — https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.devices.computer.screen?view=windowsdesktop-10.0 . Для ресурсов сохранён ориентир My.Resources, использованный в Session 1160. Исторические режимы/семантика VB в Axiom не перенесены.

Повторный audit-bytecode-coverage: PASS (.artifacts/graphics-1161-coverage.log и указанный в нём отчёт). 250 имён, 310 вариантов, 250 индексных имён, 250 тематических каталогов; пропусков VM/индекса/каталогов и лишних имён нет. Это структурная проверка, а не доказательство всех форм, типов и сценариев; отдельно подтверждены только явно перечисленные программы. git diff --check чистый. Полное решение после добавления этих примеров не повторялось; исполняемый код не менялся.

Следующий шаг — G03: проверить границы существующих файловых API и потребность в общем двоичном доступе; затем G04 Unicode и оставшиеся семейства общего аудита. Не добавлять новые средства по одному лишь отсутствию имени в реестре. Общий аудит активен.


### Session 1162 — G03: существующие пути сохранения байтов

BinaryFileAlternativeEvidenceTests выполняет COPYFILE и READFILE→WRITEFILE с явной WINDOWS-1251 для шести наборов: пустой файл, все 256 значений байта, те же значения после UTF-8/UTF-16LE/UTF-16BE BOM-последовательностей, повторные NUL/CR/LF/0x1A/0xFF/0x80/0x98. Для каждой программы проверены ожидаемый LEN, неизменность исходника, точные байты копии и повторной записи. Оба движка без перехода на другой: 12/12, код 0 (.artifacts/binary-1162-alternative.log/TRX).

Уточнение G03: прежнее «общего пути нет» слишком широко. COPYFILE уже копирует произвольные байты. Явная WINDOWS-1251 обратимо сохраняет проверенные байтовые наборы при чтении/записи целой STRING, не распознаёт Unicode BOM, один исходный байт даёт одну позицию строки. Эта возможность появилась после G01; вывод исходного реестра потребностей необходимо читать с этим уточнением. Это обходной путь через текстовую кодировку, а не числовой массив байтов: ASC использует CP866, ASCW возвращает Unicode-код, они не являются индексами байтов Windows-1251. READFILE/WRITEFILE держат весь файл/строку в памяти; позиционные бинарные операции и байтовый FILE-договор не появились.

G03 классифицирован точнее: сохранение и копирование данных уже доступны; прямое чтение/изменение массива значений 0..255 и позиционный двоичный интерфейс действительно отсутствуют. Для текущих сценариев ресурсов есть IMAGE/SPRITE/TILESET/TILEMAP-загрузчики, отдельная обязательная потребность в общем бинарном редактировании PROJECT.md не установлена. Новый OPEN FOR BINARY/GET/PUT не вводится по одному факту отсутствия имени; остаётся отложенным направлением при конкретном формате/сценарии. Это не регрессия текстового FILE.

Добавлен русскоязычный samples/functions/readfile/single_byte_round_trip.axm и его ресурс byte_values.bin (байты 0..255). Пример создаёт уникальный временный файл, сохраняет строку той же кодировкой, проверяет равенство и удаляет только этот файл. CLI выполнен из каталога примера, код 0: 256/TRUE/TRUE/TRUE. SHA256 Runtime совпал с актуальной библиотекой. Первоначальный запуск CLI из корня не нашёл относительный ресурс, поскольку CLI использует текущий каталог; это не ошибка READFILE. В IDE путь разрешается относительно запускаемого исходника.

Исполняемый код/семантика не менялись; полный прогон не повторялся. git diff --check чистый. Следующий шаг — G04: проверить полезность дополнительных Unicode-единиц и существующие альтернативы, отличая UTF-16-договор от дефекта. Общий аудит активен.


### Session 1163 — G04: Unicode-коды и составные символы

G04 проверен на 12 строках: пустая строка, кириллица, одиночный эмодзи, отдельный комбинируемый знак, семейная ZWJ-последовательность, флаги, модификатор цвета кожи, составная клавиша, смешанная строка, нечётная последовательность региональных индикаторов, Hangul Jamo и начальный комбинируемый знак. UnicodeUnitAlternativeEvidenceTests задаёт независимые ожидаемые количества единиц UTF-16, Unicode-кодов и графем. Существующий цикл ASCW(MID(...,2))→CHRW восстанавливает каждую проверенную корректную строку и считает Unicode-коды в обоих движках без перехода на другой. Отдельная проверка показывает, что этот цикл не сохраняет составной видимый символ при выделении первого кода; LEFT("😀",1) по существующему договору возвращает одну единицу UTF-16, ASCW для неё возвращает 65533. Это не регрессия UTF-16-функций.

Итог: 13/13, пропущенных и ошибочных проверок нет (.artifacts/unicode-1163-final.log/TRX). StringInfo.ParseCombiningCharacters совпал с независимыми ожидаемыми графемными границами на этом корпусе. Это проверка возможного средства реализации, а не появившаяся функция Axiom и не доказательство соответствия всей последней версии Unicode. Ориентиры: https://www.unicode.org/reports/tr29/ и https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo?view=net-10.0 .

Добавлен samples/functions/ascw/scalar_count_loop.axm. CLI с совпадающим SHA256 Runtime завершился с кодом 0: длина UTF-16 11, число Unicode-кодов 7, восстановление TRUE, равенство первого кода целому семейному эмодзи FALSE. Пример показывает уже доступный путь для корректных Unicode-строк; повреждённые суррогатные последовательности этим утверждением не охвачены.

Классификация G04 уточнена: обход Unicode-кодов уже возможен существующим API; отдельная полезная отсутствующая возможность — длина и выделение частей строки по графемам без разрыва комбинируемых знаков и ZWJ-последовательностей. Следующий шаг — согласованные дополнительные формы существующих LEN/LEFT/RIGHT/MID с явным выбором единицы, сохраняя прежние формы в UTF-16. Имена из семейства BASIC сохраняются; новые самостоятельные аналоги функций не нужны. Точный договор новых форм должен быть зафиксирован до изменения исполнителей. Новые формы пока не реализованы.

Исполняемый код не менялся, полный прогон решения не повторялся. Общий аудит остаётся активным.


### Session 1164 — явные единицы строковых функций

Реализованы дополнительные формы LEN(text, unit), LEFT(text, count, unit), RIGHT(text, count, unit), MID(text, start, count, unit). Допустимы UTF16/CODEPOINT/GRAPHEME без учёта регистра и без удаления пробелов. Существующие формы не изменены. count остаётся строго положительным, start начинается с 1, чрезмерная длина ограничивается концом строки. Неизвестный unit даёт MB3007; неверный тип отклоняется до исполнения. Общий BuiltinStringUnitHelpers используется обоими движками; идентификаторы добавлены в конец BuiltinFunctionId. Новые пользовательские имена и ключевые слова не введены.

Режим CODEPOINT сохраняет корректные суррогатные пары и отдельные повреждённые единицы без замены; для LEN подсчёт не создаёт массив границ. GRAPHEME использует StringInfo текущей платформы .NET. Нормализация, пиксельная ширина и соответствие всей самой новой версии Unicode не обещаны. Старые позиции INSTR остаются в UTF-16.

UnicodeStringUnitTests проверяет независимые длины, разные единицы выделения, семейный эмодзи/флаг/комбинируемый знак, пустую строку, INTEGER.MAX_VALUE, ошибки аргументов и повреждённые суррогаты в обоих движках без перехода на другой. Добавлены четыре unicode_units.axm в каталогах len/left/right/mid; все выполнены CLI с совпадающей библиотекой Runtime и кодом 0. В тесты дополнительно включено чтение этих реальных программ.

Manual string-functions-slices-and-length описывает новые формы непосредственно на странице, отдельно задаёт аргументы, ограничения, диагностику и короткие примеры. Поисковый индекс расширен Unicode-терминами. Оба файла подсветки сохраняют существующие имена; добавлены пояснения о строковом unit и проверки форматирования четырёх форм. Первичный редакционный ориентир — Зельднер, LEFT$/RIGHT$/MID$, страницы 298–300; запасные — официальные VB Strings.Len и Strings.Mid. Историческое разрешение count=0 из QuickBASIC не переносится в текущий договор Axiom.

Первоначально 24/24 целевых проверок прошли; расширенный целевой набор дал 44/44, но xUnit свёл два повреждённых суррогатных входа к одному идентификатору. Источники входов исправлены на числовые варианты, чтобы оба случая проверялись отдельно. Полный прогон выявил две устаревшие проверки числа аргументов LEN/RIGHT: исправлены на действительно лишние аргументы. Также обнаружен неправильный контекст каталога ресурса в общем прогоне single_byte_round_trip.axm из Session 1162: тестовый поставщик теперь получает каталог программы; READFILE не изменялся.

На момент записи полный прогон ещё выполняется, итоговый успех не заявлен. После его завершения нужен повторный прогон с исправленными проверками и актуальным подсчётом CODEPOINT, затем актуальная сборка инструмента покрытия. Общий аудит активен.


Завершение проверки Session 1164: окончательный прогон .artifacts/unicode-1164-verified-full.log содержит 9 TRX, 4907/4907, ошибок 0, пропусков 0; 49 результатов относятся к UnicodeStringUnitTests/UnicodeUnitAlternativeEvidenceTests, включая реальные четыре .axm-программы в обоих движках и раздельные повреждённые суррогаты. Предыдущие неуспешные прогоны сохранены как свидетельство исправления проверок. Последняя ошибка файлового примера была исправлена установкой его рабочего каталога внутри существующего WorkingDirectoryLock с восстановлением каталога в finally. Передача resourceBaseDirectory сама по себе не влияет на файловый READFILE; предварительный вывод выше уточнён. Код языка по этому поводу не изменялся.

Axiom.ManualChecks пересобран последовательно после окончания тестов, сборка завершилась без ошибок. SHA256 библиотек Runtime у инструмента и CLI совпал с текущей библиотекой. audit-bytecode-coverage: PASS (.artifacts/unicode-1164-coverage.log), 250 имён и 314 зарегистрированных вариантов, полный индекс и тематические каталоги. Это структурное свидетельство; 49 Unicode-проверок — отдельное функциональное свидетельство. Все четыре unicode_units.axm повторно выполнены актуальным CLI с кодом 0. Проверены ссылки manual на эти файлы и git diff --check.

G04 в выбранном договоре длины и выделения фрагментов реализован и проверен. Редакционные запасные источники: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.strings.len?view=net-10.0 и https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.strings.mid?view=net-10.0 . Следующий шаг общего аудита — числовые границы устойчивого вычисления расстояния: независимо проверить SQR(x*x+y*y) при больших/малых DOUBLE, существующие средства и возможность короткой FUNCTION, прежде чем решать вопрос о HYPOT. Остальные семейства и требования общего аудита не объявлены завершёнными; цель активна.


### Session 1165 — устойчивое расстояние существующими средствами

Проверен кандидат HYPOT из LANGUAGE_API_GAPS_20261007.md. Прямая формула SQR(x*x+y*y) при x=3e200, y=4e200 даёт MB3008 на промежуточном умножении, хотя расстояние около 5e200 конечно. При x=3e-200, y=4e-200 оба квадрата округляются до 0 и формула возвращает 0. Это следует из текущей арифметики DOUBLE, а не поломки SQR.

Короткая пользовательская FUNCTION StableDistance использует scale=MAX(ABS(x),ABS(y)), отдельно обрабатывает нулевой вектор, делит координаты на scale и умножает scale на SQR суммы нормированных квадратов. StableDistanceAlternativeEvidenceTests подтверждает десять конечных пар: ноль, 3/4, отрицательные 3/4, большие и малые 3/4, 1e308/1e308, резко разные масштабы, DOUBLE.MaxValue с нулём, минимальное положительное значение с нулём и субнормальный треугольник 3/4/5. Допуск до четырёх соседних представимых DOUBLE задан независимо от алгоритма. Две проверки переполнения различают промежуточное умножение и действительно непредставимое расстояние 1.7e308/1.7e308. Проверена и потеря малой ненулевой величины прямой формулой.

Итог 13/13, оба движка без перехода на другой (.artifacts/numeric-1165-distance.log/TRX). Добавлен русскоязычный samples/functions/sqr/scaled_distance.axm, выполнен актуальным CLI с совпадающим SHA256 Runtime: 4.9999999999999995E+200 / 5E-200 / 0, код 0. Начальные черновики с научными литералами и однострочным IF исправлены по существующему синтаксису: показатель степени передаётся строкой в VAL, IF имеет END IF. Эти ошибки черновика не считались дефектами языка.

Кандидат HYPOT уточнён: устойчивое вычисление уже выразимо существующими ABS/MAX/SQR и FUNCTION. Новая встроенная функция на этом этапе не вводится; это возможное удобство при повторяемом сценарии и отдельном договоре точности. Устойчивость на проверенном корпусе не доказывает правильное округление для каждой пары DOUBLE, не расширяет диапазон представимых результатов и не задаёт договор для NaN/Infinity.

Manual numeric-functions-roots-rounding дополнен прямо в разделе SQR: промежуточное переполнение, потеря малой величины, масштабирование, inline-функция, диагностики и ссылка на программу. Главный редакционный ориентир — Зельднер, SQR, страницы 277–278; запасной жанровый — https://learn.microsoft.com/en-us/dotnet/api/system.math.sqrt?view=net-10.0 (включая пример VB). Платформенное возвращение NaN для отрицательного аргумента в Axiom не переносится: действует MB3007.

Исполняемый код языка и подсветка не менялись, новых токенов нет. Полное решение после этих проверок и примера не повторялось; последняя полная проверка Session 1164 — 4907/4907. git diff --check чистый. Следующий шаг — кандидат равномерного случайного целого в диапазоне: сначала проверить реальный сценарий, существующие средства и ограничения INT/RND/LONG, прежде чем вводить новое имя. Общий аудит активен.


### Session 1166 — случайные целые и повторный посев CLI

Проверен кандидат равномерного случайного целого в диапазоне. RandomIntegerAlternativeEvidenceTests: 13/13, оба движка без перехода на другой (.artifacts/random-1166-evidence.log/TRX). Управляемые допустимые значения RND 0/0.5/ближайшее DOUBLE ниже 1 дают правильные края 1..10. Полные INTEGER/LONG-ширины выявляют ожидаемое MB3008 арифметики, а INT при ширине 4294967296 не может вернуть значение 2147483648. Это ограничения текущих типов, не поломка RND.

Для полного INTEGER-домена уже работает width AS LONG и CLNG(FLOOR(RND()*width))+low. Для low=9007199254740992 прибавление offset до CLNG теряет соседнее значение, а преобразование смещения до сложения сохраняет 9007199254740993. Проверена известная восьмиточечная сетка: INT(RND()*3) распределяет равновесные входы как 3/3/2. Это свидетельство преобразования на заданном источнике, не статистическая оценка настоящего генератора. Пользовательская FUNCTION с отбрасыванием хвоста уже выразима, если сетка источника известна; её использование не доказывает универсальное равномерное преобразование произвольного IRuntimeRandom. Повторный посев в обоих движках проверен на отдельном реальном System.Random, внедрённом в запрос, без общего изменяемого генератора между тестами.

Обычный игровой сценарий подтверждён существующим snake_arrow_controls.axm: выбор столбца/строки через INT(RND()*cols)+1. Обязательная текущая потребность в полном равномерном LONG-диапазоне не установлена. Универсального целого случайного API в языке действительно нет, но оно остаётся отложенным удобством, требующим договора энтропии, границ и имени по BASIC. Новое имя не вводится.

Добавлен русскоязычный samples/functions/rnd/long_range_conversion.axm и прямое описание ограничений/порядка CLNG в manual numeric-functions-core. Первый реальный CLI-запуск показал FALSE при повторном посеве. Подтверждён RNG26-001 P2: ConsoleHost.NextRandomDouble использовал Random.Shared, а Reseed был пустым. Комментарий объявлял это ограничением консоли, но оно противоречит IRuntimeRandom, RANDOMIZE и обычному текстовому запуску; headless-контекст не требует отказа от генератора.

Пять регрессионных CLI-проверок с seed 0/42/43/INTEGER.MinValue/INTEGER.MaxValue сначала дали 0/5 (.artifacts/random-1166-cli-before.log/TRX). Проверки запускают настоящий CliApp.Run, сравнивают две серии внутри одного запуска и два независимых запуска с текущим System.Random(seed). ConsoleHost теперь владеет отдельным Random, а Reseed создаёт Random(seed). Интерфейс и словарь языка не расширены. Manual RANDOMIZE отражает повторяемость CLI и границу текущей версии платформы.

Редакционный ориентир — Зельднер, RND и формула игрового диапазона, страницы 284–285; запасной современный VB-ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.vbmath.rnd?view=net-10.0 . Платформенный договор https://learn.microsoft.com/en-us/dotnet/api/system.random.nextdouble?view=net-10.0 использован только для NextDouble; исторические аргументы RND(n) в Axiom не перенесены.

Полный прогон random-1166-full начат и ещё выполняется; успех исправления до результатов не объявлен. Следующий шаг — подтвердить пять CLI-проверок и реальный .axm-пример на собранном исправлении, записать итог полного решения. Затем проверить кандидат относительного пути: имеющиеся FULLPATH/COMBINEPATH/корневые функции и потребность проекта, не вводить API лишь из-за отсутствия имени. Общий аудит активен.


Завершение Session 1166: .artifacts/random-1166-full.log и 9 TRX подтверждают 4940/4940, ошибок 0, пропусков 0. Все 13 RandomIntegerAlternativeEvidenceTests и пять регрессионных случаев CLI прошли; полный набор CLI 28/28. RNG26-001 исправлен и проверен: одинаковые зерна повторяют последовательность внутри запуска и при новом CliApp.Run.

Повторный реальный запуск samples/functions/rnd/long_range_conversion.axm после сборки дал 722011773 / TRUE / TRUE, код 0; SHA256 Runtime CLI совпал с текущей библиотекой. До исправления последняя проверка повторяемости давала FALSE. git diff --check чистый. Программа является проверкой диапазона и посева, а не доказательством строгой равномерности.

Классификация кандидата: малые игровые индексы доступны через RND/INT; широкий INTEGER-домен и большие LONG-смещения — через существующие FLOOR/CLNG с правильным порядком преобразования; универсальный равномерный LONG×LONG→LONG API действительно отсутствует, но текущая обязательная потребность в нём не установлена. Он отложен до реального сценария и договора энтропии, границ и исторического имени. Исправление CLI относится к существующему RANDOMIZE, а не к новому генератору языка. Следующий шаг — относительные пути и существующие файловые альтернативы. Общий аудит активен.


### Session 1167 — каталог программы при разрешении Windows-путей

PATH26-001 P2: неполные Windows-пути ошибочно разрешались от процесса после Path.Combine. Подтверждены корневая запись на другом диске, drive-relative и UNC в обоих движках: первоначально 0/8, после Path.GetFullPath(expanded, baseDirectory) целевой расширенный набор 18/18. Добавлен samples/functions/fullpath/drive_relative_scope.axm, проверка настоящего файла и запускаемого исходника, прямое описание в manual и поисковом индексе. Подробности и пределы доказательства — audit/RELATIVE_PATH_ALTERNATIVES_20261008.md. Полный прогон path-1167-full пока выполняется; итог и CLI ещё требуется записать. Общий относительный маршрут между двумя абсолютными путями остаётся отсутствующим необязательным API; существующие FULLPATH/COMBINEPATH уже решают нормализацию известных ссылок. Общий аудит активен.


Завершение Session 1167: path-1167-full.log и 9 TRX подтверждают 4960/4960, ошибок 0, пропусков 0. Все 19 PathBaseResolutionEvidenceTests прошли, включая реальный запуск drive_relative_scope.axm в обоих движках; прежние восемь независимых случаев не проходили до исправления. PATH26-001 исправлен и подтверждён.

Актуальный CLI с совпадающим SHA256 Runtime выполнил samples/functions/fullpath/drive_relative_scope.axm с кодом 0: все шесть проверок TRUE, в том числе восстановление исходной папки и удаление созданной пустой временной папки. Проверены новые ссылки manual и git diff --check. Лексические D:/UNC-примеры не доказывают доступность второго диска или сетевого ресурса; фактический READFILE проверен на локальном временном файле.

Общий построитель относительного маршрута не добавлен: текущие разрешение и соединение ссылок доступны, отдельный необязательный API между двумя абсолютными путями отсутствует и отложен до сценария. Следующий шаг — сверить сквозную матрицу общего аудита с G04, RNG26-001 и PATH26-001, определить ещё не подтверждённые семейства по формам и поведению, а не только по наличию имён. Общий аудит активен.


## Session 1169 — позиции файлового чтения и границы буфера

Добавлены 36 независимых FilePositionBufferEvidenceTests: два движка без перехода на другой, UTF-8/UTF-16LE/UTF-16BE с BOM, emoji после 0/127/128/254/255/256 ASCII-символов, CRLF, кириллица, байтовые позиции и длина, LINENUMBER, EOF, REWIND/READTOEND и сохранение позиции после отрицательного/слишком большого SEEKFILE. Ожидаемые позиции выведены из известного формата байтов, а не из проверяемого RuntimeTextFile.

Первый прогон 0/36 остановился на неверном ожидании нового теста: READLINE в конце файла обязан выдавать MB3007; READTEXT возвращает пустую строку. По диагностике и общему EvaluateReadLine подтверждено, что это существующий договор, а не дефект буфера. Ожидание исправлено, код языка не изменён. Новый набор 36/36; вместе с ReadTextUnicodeContractTests, ExplicitTextEncodingTests, FileLineEndingContractTests и FileWriteUnicodeContractTests — 78/78, ошибок/пропусков 0, .artifacts/test-results/file-stream-1169/file-stream-1169.trx.

Точные формы и пределы записаны в audit/FILE_STREAM_FORM_EVIDENCE_20261008.csv. Не заявлена полнота всех файловых средств: изменяемые извне файлы, LONG-границы, весь набор режимов и неправильных типов требуют отдельной проверки. Семантика и словарь не менялись, новый .axm не требуется; существующий samples/functions/readtext/unicode_chunks.axm остаётся короткой ручной проверкой. Полный набор решения в этой сессии не повторялся. Следующий шаг — запись с SEEKFILE в OUTPUT: сохранение хвоста, буфер FLUSH, байтовые границы многобайтового текста и запрет позиционирования APPEND. Общий аудит активен.


## Session 1170 — OUTPUT/APPEND и переводы строк UTF-16

Проверены OUTPUT/APPEND в UTF-8/UTF-16LE/UTF-16BE: 12 новых FileOutputPositionEvidenceTests (два движка, с отдельным FLUSH перед SEEKFILE и без него). Перезапись B на Z сохраняет C, REWIND и замена A на Q сохраняют хвост и длину, APPEND отклоняет SEEKFILE/REWIND с MB3007 и сохраняет позицию. После добавления Я, NEWLINE и пустого WRITELINE проверены точные байты Q😀ZCЯ\n\n с правильным BOM. Совместный набор файловых проверок 90/90, ошибок/пропусков 0; .artifacts/test-results/file-output-1170/file-output-1170.trx. Полное решение не повторялось.

Найден и исправлен дефект документации: manual называл LF всегда одним байтом и обещал безусловное увеличение FILELENGTH. Реально LF — один символ: один байт UTF-8/WINDOWS-1251, два UTF-16; перезапись внутри прежнего файла не обязана увеличивать длину. Runtime не изменён. Страница file-system-functions-open-files.html уточнена; добавлены inline UTF-16-пример и samples/functions/newline/utf16_byte_positions.axm. Актуальный CLI (SHA256 Runtime совпал) выполнил пример: позиции 0/2/4, длина 4, чтение/удаление TRUE, код 0. Первый вызов CLI без команды run показал справку; правильный запуск выполнен отдельно, не принят за проверку.

Редакционный ориентир: Зельднер, WRITE #, страницы 195–196 (локальный текст .artifacts/aggregate-fix-1118/zeldner.txt); запасной текущий жанровый ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.writeline?view=net-10.0 . Исторический CRLF и форматирование значений не перенесены: Axiom сохраняет свой LF и свои формы FILE. Навигация уже ведёт на существующие секции, новые имена не введены.

Граница доказательства: записи проверены на корректных границах символов; SEEKFILE принимает байтовую позицию и не гарантирует сохранение текста при перезаписи внутри кодированного символа. Следующий шаг — независимо проверить этот договор, различить допустимую байтовую перезапись и непреднамеренное повреждение текста, затем ошибки закрытия/сброса и изменяемые извне файлы. Общий аудит активен.


## Session 1171 — байтовая перезапись Unicode

Байтовая перезапись Unicode проверена в 16 FileByteOverwriteEvidenceTests: два движка без перехода на другой; UTF-8, UTF-16LE/BE; WRITE("Z") внутри/в начале emoji. Независимые ожидаемые байты заданы литералами. SEEKFILE/WRITE сохраняют точно заданную байтовую замену и хвост; результат не обязан быть корректным текстом. Строгие READFILE/OPEN USING отклоняют полученный невалидный текст с MB3007. Уже переход к началу emoji не спасает от короткой замены: неперезаписанные байты остаются. Это ограничение байтового позиционирования, не доказанный дефект сохранения файла.

Manual file-system-functions-open-files.html теперь прямо объясняет нулевые байтовые позиции без BOM, границы SEEKFILE, сброс буфера, сохранение хвоста и риск замены разной байтовой длины. Существующая альтернатива: READFILE → REPLACE → WRITEFILE в той же кодировке; требуется память и нет обещания атомарной записи. Новый встроенный API не нужен для проверенного малого текстового сценария; специализированная потоковая замена Unicode действительно не предоставлена, но потребность в ней не установлена. Добавлены inline-пример и samples/functions/seekfile/multibyte_overwrite.axm.

Реальный CLI выполнил пример с кодом 0: строгая диагностика MB3007 после байтовой замены, затем полная запись AZB и удаление временного файла TRUE. SHA256 Runtime CLI совпал с текущей библиотекой. Редакционный ориентир — Зельднер, SEEK, страница 210; запасной текущий жанровый ориентир https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.seek?view=net-10.0 . Нумерация BASIC с 1 и режим APPEND из исторического описания в Axiom не перенесены.

Новый набор 16/16, совместный файловый набор 106/106 без ошибок и пропусков: .artifacts/test-results/file-bytes-1171/file-bytes-1171-neighbors.trx. Полное решение не повторялось, Runtime не менялся. Следующий шаг — ошибки сброса/закрытия файла: сохранение ресурса, диагностики и предотвращение повторной необработанной ошибки при завершении программы. Источниковая гипотеза ещё не считается дефектом; требуется воспроизведение. Общий аудит активен.


## Session 1172 — CLOSE и ошибки автоматического завершения

Подтверждён FILE26-001 P1: при ошибке сброса во время Dispose ресурс оставался открытым; CloseAllOpenFiles прерывался на первом отказе и не освобождал остальные файлы; явный CLOSE в обоих движках пропускал IOException вне TRY/CATCH языка. Воспроизведение — настоящая Windows-блокировка диапазона FileStream.Lock(0,4096) через отдельный читатель, а не подмена результата записи. Прямой набор до исправления 0/2, запуск через оба движка 0/4. Ошибки прежнего запуска мешали даже удалению собственного временного файла из-за оставшегося открытого ресурса.

RuntimeTextFile.Dispose теперь освобождает FileStream в finally и отмечает ресурс закрытым. CloseFile удаляет закрытый ресурс из реестра даже при отказе. CloseAllOpenFiles проходит весь набор: при прямом вызове сохраняет первую ошибку после освобождения остальных ресурсов, при завершении движка сообщает MB3007 для каждого отказа. Interpreter/BytecodeVm переводят ошибку явного CLOSE в MB3007/RuntimeFailureException; TRY/CATCH перехватывает её. Автоматический отказ даёт неуспешный RuntimeResult, не необработанное CLR-исключение.

После исправления FileCloseFailureEvidenceTests 2/2 и FileFinalizationFailureEvidenceTests 4/4. Проверены ISOPEN=FALSE, эксклюзивное повторное открытие после снятия блокировки, запись второго файла, обработка явного CLOSE и автоматическое завершение, без перехода Bytecode на интерпретатор. Проверки блокировки относятся к Windows; на других ОС их ветка не выполняется, переносимость отказа не заявляется. Полный file-close-1172-full выполняется, итог ещё требуется подтвердить.

Manual close.html описывает освобождение после ошибки и отсутствие гарантии сохранения всех данных. Добавлен samples/operators/close/write_failure_state.axm: два собственных временных файла, пауза и проверка закрытого состояния/второго текста. Обычный запуск примера не воспроизводит отказ без внешней блокировки; воспроизведение отказа обеспечено отдельными тестами. Редакционный ориентир — Зельднер CLOSE, страница 187; запасной текущий жанровый https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.filesystem.fileclose?view=net-10.0 . Новые имена не добавлены, обе схемы подсветки уже содержат CLOSE/ISOPEN.

Следующий шаг — завершить полный прогон и реальный запуск примера, затем проверить повторный FLUSH после временного отказа и сохранение основной ошибки программы при дополнительном отказе автоматического закрытия. Общий аудит активен.


Завершение Session 1172, 2026-10-09 00:01: полный file-close-1172-full завершился с кодом 0; девять TRX подтверждают 5032/5032, ошибок 0, пропусков 0. Новые проверки настоящего Windows-отказа выполнены, а не пропущены платформенной веткой. FILE26-001 исправлен и подтверждён.

Повторный CLI-запуск samples/operators/close/write_failure_state.axm выполнен после полного прогона: SHA256 Runtime CLI совпадает с текущей библиотекой (CC6974281810B5810E209E915C40608B452017DAC150270CFF94653728317BDA), код 0, все пять проверок состояния/текста/удаления TRUE. Более ранний запуск на старой копии Runtime не использован как доказательство исправления. CLI-пример проверяет обычный путь; отказ воспроизведён шестью целевыми тестами. Отдельный отчёт — audit/FILE_CLOSE_FAILURE_20261008.md. git diff --check чистый.

Следующий шаг — повторный FLUSH после снятия временной блокировки, отсутствие дублирования данных и сохранение основной диагностики при дополнительном отказе автоматического закрытия. Общий аудит активен.


## Session 1173 — повторный сброс после блокировки

Проверено восстановление FLUSH после настоящей временной Windows-блокировки диапазона: OUTPUT/APPEND, два движка, короткий UTF-8-текст и запись 8196 байтов. Восемь новых случаев подтверждают MB3007 внутри CATCH, ISOPEN=TRUE, сохранение FILEPOS/FILELENGTH, успешное добавление текста и повторный/пустой FLUSH после снятия блокировки, точные байты без дублирования и сохранение APPEND-префикса. Два дополнительных случая подтверждают порядок APP1173/MB3007 при исходной ошибке программы и отказе автоматического закрытия, сохранение первичного текста и освобождение ресурса. Без перехода Bytecode на интерпретатор.

FileFinalizationFailureEvidenceTests: 14/14 с четырьмя прежними CLOSE-случаями; совместный файловый набор 120/120, ошибок/пропусков 0, .artifacts/test-results/file-flush-1173/file-flush-1173-neighbors.trx. Прямые Windows-ветки Lock/Unlock уточнены, предупреждения CA1416 устранены для изменённого писателя. Runtime не менялся. Полный набор решения в этой сессии не повторялся; последний полный прогон Session 1172 — 5032/5032.

Отчёт — audit/FILE_FLUSH_RECOVERY_20261009.md. Доказан отказ блокировки диапазона, не любой отказ носителя или атомарность сохранения. Повторный FLUSH доступен после ошибки самого FLUSH при открытом ресурсе; после отказа CLOSE ресурс уже закрыт. Новое средство языка не требуется для проверенного временного отказа. Следующий шаг — COPYDIR/MOVEDIR: вложенное назначение, совпадающие пути, точки повторного анализа и сохранение исходных данных при отказе. Общий аудит активен.


## Session 1174 — COPYDIR/MOVEDIR и сохранение данных

Добавлены 18 независимых DirectoryFailureEvidenceTests в обоих движках без перехода Bytecode на интерпретатор: COPYDIR/MOVEDIR с совпадающим или вложенным назначением; COPYDIR с конфликтом вложенного файла после копирования корневого; защита существующей вложенной ссылки назначения от перезаписи; MOVEDIR при существующем назначении/отсутствующем родителе; перенос явно указанного соединения каталогов Windows. Вместе с восемью DirectoryTraversalContractTests получено 26/26, ошибок/пропусков 0, .artifacts/test-results/directory-1174/directory-1174.trx.

Подтверждены существующие ограничения: COPYDIR не атомарен и не откатывает уже скопированный корневой файл при позднем конфликте, но источник и конфликтный файл назначения остаются прежними. Вложенная ссылка назначения отклоняется, её цель не меняется. MOVEDIR не объединяет папки и не создаёт цепочку отсутствующих родителей; при переносе соединения Windows перемещает саму ссылку, не целевой каталог. Проверенные отказы сохраняют исходные данные; не заявлен откат любого отказа носителя.

Runtime не менялся. Manual file-system-functions-management.html уточнён для MOVEDIR: типы путей, родитель назначения, отсутствие объединения, диагностики, отличия ссылок от COPYDIR. Добавлен samples/functions/movedir/existing_destination.axm и inline-пример. Реальный CLI выполнил новый пример с кодом 0: MB3007, источник/назначение сохранены TRUE, собственные временные файлы и папки удалены. Полный набор решения в этой сессии не повторялся; последний полный — 5032/5032 из Session 1172.

Книга Зельднера проверена: NAME, страница 341, описывает перенос файла и явно не переносит целую директорию, поэтому не использована как источник семантики MOVEDIR. Ближайший текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.movedirectory?view=net-10.0 . Автоматическое создание назначения/overwrite из Visual Basic не перенесены в Axiom. Новых сущностей нет, словарь подсветки не менялся.

Следующий шаг — оборванные ссылки назначения и источник/назначение, меняющиеся между проверкой и обходом. Проверки текущей сессии относятся к существующим соединениям и обычным папкам; не считать их доказательством защиты от всех видов точек повторного анализа или гонок файловой системы. Общий аудит активен.


## Session 1175 — оборванные ссылки DIRSIZE/COPYDIR

Проверены оборванные ссылки: шесть DanglingDirectoryLinkEvidenceTests в двух движках без перехода Bytecode на интерпретатор. Реальное соединение каталогов с удалённой целью в назначении COPYDIR отклоняется MB3007, цель не создаётся; оборванная символическая ссылка на файл назначения также отклоняется, отсутствующий целевой файл не создаётся. Оборванная файловая ссылка источника пропускается DIRSIZE/COPYDIR. Источник и созданные ссылки сохранены до собственного удаления. На этой Windows атрибут ReparsePoint доступен без существующей цели; гипотеза обхода статического запрета не подтвердилась.

Новые случаи 6/6; вместе с двумя прежними классами каталогов — 32/32, ошибок/пропусков 0. .artifacts/test-results/dangling-1175/dangling-1175.trx. Ссылки реально созданы, не подменены обычными папками и не пропущены. Runtime не изменён, полный набор не повторялся; последний полный Session 1172 — 5032/5032. Отчёт — audit/DANGLING_LINK_EVIDENCE_20261009.md.

Из чтения BuiltinDirectoryHelpers следует ограничение проверки: атрибуты и использование строкового пути разделены; дерево не фиксируется целиком удерживаемыми дескрипторами. Не подтверждена защита от подмены другим процессом после проверки. Сам результат такой гонки в этой сессии не воспроизведён, поэтому не помечен подтверждённым дефектом. Не обещать изоляцию по зелёным статическим тестам и не добавлять для неё случайный новый API.

Следующий шаг — проверить корневые явно указанные оборванные ссылки и файловые COPYFILE/MOVEFILE: договор исходных/конечных ссылок, overwrite и сохранение данных при отказе. Исследование гонок остаётся отдельным незавершённым вопросом, требующим воспроизводимого контроля момента подмены. Общий аудит активен.


## Session 1176 — COPYFILE/MOVEFILE и ссылки источника

Проверены COPYFILE/MOVEFILE: 16 FileTransferFailureEvidenceTests в обоих движках без перехода Bytecode на интерпретатор. Двенадцать случаев покрывают конфликт существующего назначения без overwrite, отсутствующего родителя и явный overwrite TRUE; независимые байты включают NUL, 0xFF, 0x80, BOM-последовательность и CRLF, имя источника содержит кириллицу/emoji. При отказах источник и старое назначение сохранены, отсутствующая цепочка родителей не создаётся. При overwrite итоговые байты точны: COPYFILE сохраняет источник, MOVEFILE удаляет прежний путь после успеха.

Ещё четыре случая с настоящей символической ссылкой источника подтверждают различие: COPYFILE копирует содержимое цели в обычный файл, сохраняя ссылку; MOVEFILE переносит саму ссылку, не меняя её цель. Это наблюдение проверенной Windows, не полный договор всех файловых систем и видов ссылок. Исходный целевой файл в обоих сценариях неизменен.

Результат 16/16, ошибок/пропусков 0: .artifacts/test-results/file-transfer-1176/file-transfer-1176.trx. Runtime не менялся, полный набор решения не повторялся; последний полный Session 1172 — 5032/5032. Новый .axm не требуется, поскольку семантика не менялась. Ограничения отражены в audit/FILE_TRANSFER_EVIDENCE_20261009.md и CSV по формам. git diff --check проверяется при завершении.

Следующий шаг — явно заданные оборванные ссылки корня COPYDIR и назначения COPYFILE/MOVEFILE, с overwrite/без него. Перенос существующей ссылки не доказывает поведения оборванной; гонки путей и отказ носителя остаются непроверенными. Уточнение пользовательского manual о файловых ссылках следует выполнить после проверки обеих сторон операции, чтобы не документировать половину договора. Общий аудит активен.


## Session 1177 — файловые ссылки назначения

Проверены 16 FileDestinationLinkEvidenceTests: два движка, COPYFILE/MOVEFILE, существующая/отсутствующая цель символической ссылки назначения, overwrite FALSE/TRUE. Первый прогон 14/16: неверным оказалось предположение о безусловном отказе COPYFILE без overwrite при оборванной ссылке. Реальная операция создала отсутствующую цель; это не замена существующих байтов. Ожидание уточнено, сама ссылка и точные байты цели проверены отдельно. Runtime не менялся.

Подтверждён договор проверенной Windows: COPYFILE следует ссылке назначения, сохраняет ссылку и пишет в цель; при существующей цели без overwrite — MB3007, при отсутствующей создаёт цель даже без overwrite. MOVEFILE без overwrite отклоняет ссылку назначения независимо от существования цели; с overwrite заменяет саму ссылку обычным файлом и не меняет прежнюю цель. Источник после отказа сохранён; после успешного переноса прежний путь отсутствует. Вместе с FileTransferFailureEvidenceTests — 32/32, ошибок/пропусков 0, .artifacts/test-results/destination-link-1177/destination-link-1177.trx.

Manual file-system-functions-management.html дополнен правилами источника/назначения, типами параметров, отсутствующим родителем, диагностикой и двумя раздельными формами COPYFILE/MOVEFILE. Книга Зельднера NAME ранее проверена как ориентир перемещения файлов; подходящего FILECOPY-раздела в извлечённом тексте не найдено. Текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.copyfile?view=net-10.0 и ранее проверенная MoveFile. Факты ссылки подтверждены Axiom-тестами, не перенесены из Visual Basic. Новый пользовательский API не введён; отсутствие языка создания ссылок не требуется устранять ради этого случая. Автоматизированная подготовка использует реальные ссылки файловой системы.

Полный набор решения в этой сессии не повторялся, последний полный — 5032/5032 Session 1172. Новая .axm-программа не требуется, поскольку поведение языка не менялось и без внешней подготовки ссылки этот случай не воспроизводится. Следующий шаг — корневые явно заданные оборванные ссылки COPYDIR, затем свести подтверждённые файловые формы и оставить непроверенные гонки/отказы носителя отдельными вопросами. Общий аудит активен.


## Session 1178 — корневые ссылки COPYDIR

Проверены четыре RootDirectoryLinkEvidenceTests: оба движка, настоящее соединение каталогов Windows с удалённой целью, явно указанное как источник/назначение COPYDIR. Источник даёт MB3007, цель источника не создаётся, но пустая папка назначения уже создана. Явное корневое назначение разрешается в отсутствующую цель: она создаётся, получает точный текст, соединение сохраняется. Источник неизменен. Это отличается от запрета вложенной ссылки назначения и не отменяет отсутствие отката COPYDIR.

Совместный набор шести классов переноса/каталогов — 68/68, ошибок/пропусков 0, .artifacts/test-results/root-link-1178/root-link-1178.trx. Runtime не менялся, полный набор не повторялся; последний полный результат остаётся 5032/5032 Session 1172. Manual file-system-functions-management.html прямо описывает оба корневых случая и возможное создание пустого назначения перед ошибкой. Редакционный текущий жанровый ориентир: https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.copydirectory?view=net-10.0 ; факты определены по Axiom, не по поведению Visual Basic. Подходящей исторической главы копирования дерева у Зельднера не установлено.

Создан audit/FILESYSTEM_AUDIT_STATUS_20261009.md: подтверждённые участки, ограниченность прогонов, FILE26-001 как исправленная поломка, имеющиеся альтернативы, отсутствующий общий позиционный двоичный режим как отложенный кандидат и отсутствие основания добавлять язык создания ссылок ради тестовой подготовки. Не заявлены полная защита гонок, всех файловых систем или отказов носителя. Новых сущностей/семантики нет; внешний сценарий ссылки подготовлен тестами, новая .axm-программа без возможности создать ссылку не дала бы независимого воспроизведения.

Следующий шаг — совпадающие пути COPYFILE/MOVEFILE, жёсткие ссылки как разные имена одного файла, overwrite и сохранение исходных байтов. Затем вернуться к сквозной матрице всего языка с сохранёнными пределами файловых доказательств. Общий аудит активен.


## Session 1179 — совпадающий файл и жёсткие ссылки

Добавлены 16 SameFileTransferEvidenceTests: восемь совпадающих путей и восемь настоящих жёстких ссылок Windows, два движка, COPYFILE/MOVEFILE, overwrite FALSE/TRUE. Жёсткая связь доказана записью через второе имя и чтением первого перед вызовом языка. Совпадающий путь: COPYFILE даёт MB3007 и сохраняет байты, MOVEFILE возвращает TRUE без изменения файла. Разные жёсткие имена: COPYFILE даёт MB3007 и сохраняет оба имени/байты; MOVEFILE возвращает TRUE при обоих overwrite, убирает имя источника и сохраняет байты назначения.

Первые ожидания MOVEFILE для разных жёстких имён были неверны: без overwrite ожидался конфликт, затем предполагалось сохранение обоих имён. Независимые проверки результата и существования путей показали успешный перенос имени. Ожидания уточнены; это измеренный договор проверенной Windows, не исправление Runtime. Итог нового класса 16/16; совместно с источниками/назначениями ссылок — 48/48, ошибок/пропусков 0, .artifacts/test-results/same-file-1179/same-file-1179.trx.

Manual file-system-functions-management.html уточнён: успех MOVEFILE не означает исчезновения пути при совпадающем пути; жёсткие имена одного файла отличаются от конфликта двух файлов. Добавлены inline-пример и samples/functions/movefile/same_file_path.axm. Актуальный CLI выполнил пример с кодом 0: MOVEFILE TRUE, сохранность текста TRUE, COPYFILE MB3007, сохранность после отказа TRUE, удаление TRUE. Runtime не менялся, полный набор не повторялся; последний полный — 5032/5032 Session 1172.

Редакционный ориентир — ранее прочитанный Зельднер NAME, страница 341; текущий жанровый https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.movefile?view=net-10.0 . Поведение жёстких ссылок определено по Axiom/Windows-тестам, не перенесено из исторического BASIC. Новый API определения тождественности файлов не вводится: текущие операции уже сохраняют байты в проверенных случаях, отдельная обязательная потребность не установлена.

Следующий шаг — вернуться к сквозной матрице всего языка, связать новые файловые доказательства с реальными формами и определить оставшиеся участки независимой проверки вне файловой системы. Пределы: другие ОС, гонки, отказ носителя и весь набор неправильных типов не доказаны. Общий аудит активен.


## Session 1182 - 2026-10-09 00:27 - независимые числовые преобразования

Добавлен IntegralConversionEvidenceTests: 28 случаев, два движка, заранее заданные результаты усечения на ±2.5/±3.5, точных DECIMAL-границ INTEGER/LONG с половиной и выходом на единицу, точного большого LONG и потери единицы при промежуточном CDBL. Каждый случай проверяет TRY/FINALLY, продолжение, отсутствие запасного интерпретатора и внешних диагностик. Первый прогон 83/87: четыре SyntaxFailure из-за формы CDEC(-9223372036854775808). Подготовка значения исправлена на -CDEC(9223372036854775807)-CDEC(1); это отделяет проверку преобразования от отдельного договора литералов, не исправление Runtime.

Повторный набор с NumericConversionContractTests, NumericBoundaryContractTests, IsNumericContractTests и NumericTextAlternativeEvidenceTests — 87/87, ошибок/пропусков 0, .artifacts/test-results/conversion-1182/conversion-1182.trx. Проверены реальные существующие тесты ISNUMERIC/VAL и добавлены точные указатели NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv. ISNUMERIC уже обеспечивает полную проверку конечного числа и отличается от VAL по префиксам/переполнению: новой функции проверки строки не требуется. Допустимый underflow в ноль не является обещанием точности.

Новой поломки преобразований не установлено. Runtime и семантика языка не менялись; справочник уже описывает усечение к нулю, диапазон после усечения и возможное округление CDBL. Новая программа .axm не требуется для неизменной семантики. Полный набор не повторялся. Следующий шаг — числовые литералы нижней границы LONG как самостоятельный договор, затем формы STR/INT/CDEC и Unicode-формы. Общий аудит активен.


## Session 1183 - 2026-10-09 00:29 - нижняя граница LONG и литералы

Добавлен LongMinimumLiteralEvidenceTests: два случая движков проверяют три точных способа получить минимум LONG и верхнюю границу; две записи прямого отрицательного литерала проверяются в обоих движках на SyntaxFailure/MB0004, начало/длину диагностики и отсутствие вывода. Вместе с IntegralConversionEvidenceTests и NumericBoundaryContractTests — 38/38, ошибок/пропусков 0. Отдельно запущен весь Axiom.Lexing.Tests, результат сохранён в lexer-1183.trx.

Договор подтверждён исходником Lexer и существующим LexerTests.Lex_ReportsOutOfRangeIntegerLiteral: минус отдельный, положительный токен должен помещаться в LONG. Manual data-types-scalars.html уже отличает диапазон значения от допустимой формы литерала. Доступ к минимуму через -9223372036854775807 - 1 и шестнадцатеричную аналогичную арифметику точный. Создан LONG_MINIMUM_LITERAL_EVIDENCE_20261009.md с классификацией: не поломка, существующая альтернатива; специальная краткая форма отсутствует, но изменение не требуется для доступа к значению. Runtime/синтаксис не менялись, новая .axm-программа для изменения поведения не требуется.

Следующий шаг — независимые STR/INT/CDEC и добавленные ранее Unicode-формы LEN/LEFT/RIGHT/MID. Общий аудит активен.


## Session 1184 - 2026-10-09 00:31 - INT STR CDEC и региональные настройки

Добавлен NumericPresentationEvidenceTests: 13 заранее заданных выражений × три культуры en-US/ru-RU/tr-TR × оба движка, 78 случаев. INT проверен на половинах, нижней/верхней границе INTEGER после округления вниз и LONG за диапазоном. STR проверен на точке/знаке, точном LONG 2^53+1, DECIMAL LONG.MaxValue и 1/8. CDEC проверен на точном LONG, арифметически собранном DECIMAL.MaxValue, переполнении DOUBLE 1e29 и округлении 1e-29 в 0. Культура восстанавливается в finally; проверены отсутствие внешней диагностики/запасного интерпретатора и продолжение после перехваченного MB3008.

Все 78 новых прошли; вместе с IntegralConversionEvidenceTests и NumericPromotionContractTests — 206/206, ошибок/пропусков 0, .artifacts/test-results/numeric-presentation-1184/numeric-presentation-1184.trx. Добавлены точные ID/Arity в NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv, общая матрица пересчитана. Поломки текущих договоров не найдены. Runtime не менялся, полный набор не повторялся.

Классификация: округление вниз уже доступно через INT, сохранение большого целого при выводе — STR(LONG), точное получение проверенной границы DECIMAL — арифметика CDEC(LONG). Отсутствие универсального точного преобразования строки в DECIMAL не устранено этим набором и не объявлено обязательной новой функцией; VAL возвращает DOUBLE и потому не заменяет такую возможность во всех случаях. Малый DOUBLE при CDEC может стать нулём, это не проверка точности. Новая семантика и пользовательские имена не введены; существующая справочная страница описывает проверенные основные формы.

Следующий шаг — независимая проверка Unicode-форм LEN/LEFT/RIGHT/MID, затем оставшиеся формы без доказательств. Общий аудит активен.


## Session 1185 - 2026-10-09 00:33 - независимые границы Unicode-единиц

Добавлен UnicodeBoundaryEvidenceTests: восемь строк × оба движка, 16 случаев. Все позиции MID и соответствующие префиксы LEFT/суффиксы RIGHT сравниваются с заранее заданными частями, отдельно LEN и выход за конец. GRAPHEME: e с combining mark, семья ZWJ, флаги RI с нечётным остатком, тон кожи, keycap, начальный combining mark, Hangul, CRLF. CODEPOINT: combining отдельно, целая emoji-пара, высокие/низкие одиночные суррогаты и соседняя корректная пара. UTF16: обе половины emoji. Не используется StringInfo для вычисления ожидаемого результата.

Первоначальная подготовка ошибочно использовала CHRW для суррогатных кодов (получен RuntimeFailure); заменена на строки с C#-escape. Передача повреждённых строк в MemberData затем искажала данные: наблюдались три единицы вместо двух и несовпадение с половинами emoji. Теперь через MemberData передаются только режим/индекс, исходные повреждённые строки строятся внутри теста. При промежуточной правке исправлена ошибка синтаксиса объявления массива C#. Итог 16 новых вместе с UnicodeStringUnitTests и UnicodeUnitAlternativeEvidenceTests — 65/65, ошибок/пропусков 0, .artifacts/test-results/unicode-boundaries-1185/unicode-boundaries-1185.trx.

Создан UNICODE_UNIT_FORM_EVIDENCE_20261009.csv с точными LenUnits/LeftUnits/RightUnits/MidUnits и Arity. Семантика Runtime не менялась, существующие четыре unicode_units.axm исполняются с ожидаемым выводом в проверенном классе UnicodeStringUnitTests. Новых имён/семантики нет; manual ранее описывает эти формы. Не обещано покрытие всего Unicode и стабильность границ между версиями StringInfo/.NET. Полный набор решения не повторялся.

Классификация: целые составные символы и Unicode-коды уже доступны через явные формы, отдельные новые функции не нужны. Режим UTF16 намеренно может разделить пару; CODEPOINT сохраняет корректную пару и отдельно переносит одиночный суррогат, это не валидация корректности всей строки. Следующий шаг — пересчитать остаток матрицы и выбрать неподтверждённые формы вне файловых повторов. Общий аудит активен.


## Session 1186 - 2026-10-09 00:35 - независимый числовой договор RGB

Добавлен RgbIndependentEvidenceTests: 30 отдельных случаев обоих движков для чёрного/белого, трёх основных цветов, 1/2/3 и каждого компонента вне диапазона (-1, 256, LONG.MaxValue). Ожидаемые упакованные десятичные значения заданы буквально, без вызова общего помощника цвета. Ещё четыре теста неверных форм выполняют оба движка: два/четыре аргумента, дробный и строковый аргумент; подтверждена диагностика MB2 до исполнения. Функция проверяется без SCREEN/CANVAS, после перехваченного MB3007 программа продолжает работу.

Итог 34/34, ошибок/пропусков 0, .artifacts/test-results/rgb-1186/rgb-1186.trx. Создан GRAPHICS_VALUE_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Runtime не менялся, полного прогона не было. Это доказательство числового цвета 0xRRGGBB, не проверки пикселя, альфа-канала, палитры или отображения.

Классификация: числовое получение цвета уже доступно; отдельные новые функции упаковки не нужны для проверенных случаев. Экранные функции и интерактивный ввод не объявляются проверенными по этому результату. Следующий шаг — выделить оставшиеся формы по необходимому контексту и независимо проверить доступные свойства изображений/тайлов через существующие тестовые хосты. Общий аудит активен.


## Session 1187 - 2026-10-09 00:36 - реальные ресурсы IDE и свойства

Прочитан TilePropertyAcceptanceSampleTests: 27 запусков существующих .axm × оба движка и два явных SCREEN 0, заранее заданные строки, отсутствие запасного интерпретатора, настоящий ProgramConsoleService/ProgramRunService и загрузчик ресурсов. Повторный запуск — 56/56, ошибок/пропусков 0, .artifacts/test-results/resources-1187/resources-1187.trx. Независимо прочитаны PNG IHDR (32x32) и JSON simple_map.tmj: 2x2, клетки 16x16, два tilelayer и один objectgroup, данные [1,2,0,4]/[2,1,4,0], тайлсет четыре тайла. Размеры/число кадров согласуются с ожидаемыми результатами существующих примеров.

Создан GRAPHICS_RESOURCE_FORM_EVIDENCE_20261009.csv для фактически запущенных форм: изображения, спрайты, LOADTILESET с тремя аргументами, свойства тайлов/карт, клетки/маски, начальное состояние SCREEN и явный SCREEN 0. LOADTILESET с четырьмя/пятью аргументами не приписано доказательство от формы с тремя. Это воспроизведение заданного результата ресурса, не проверка отрисовки или всех форматов, повреждений и границ. Новых тестов, Runtime-изменений и новых семантических обещаний нет.

Классификация: основные свойства ресурсов уже доступны и проверяются через реальный загрузчик IDE; отсутствие указателя в прежней матрице было пробелом учёта, не отсутствием функций. Следующий шаг — отдельно проверить формы LOADTILESET margin/spacing и контекстные TEXTWIDTH/TEXTHEIGHT/POINT/PMAP. Общий аудит активен.


## Session 1188 - 2026-10-09 00:38 - LOADTILESET отступы и промежутки

Прочитаны и повторно выполнены парные Runtime-тесты Interpreter/BytecodeVm: TilesetWidthHeightAndCountReturnLoadedAndEmptySizes, DrawTileCallsModernGraphicsHostAndUpdatesCurrentPosition, TilesetOperationsReportDocumentedDiagnostics. Итог 6/6, ошибок/пропусков 0, .artifacts/test-results/tileset-1188/tileset-1188.trx. Ожидания независимы: атлас 34x18/тайл16x8/margin1 даёт4; атлас36x20/тайл16x8/margin1/spacing2 даёт4. Для36x24/тайл16x10 четвёртая область начинается19,13 и имеет16x10, фактический вызов рисующего хоста сравнивается с заданной записью. Отрицательный margin даётMB3007, несовместимая сетка35x24 даётMB3005.

GRAPHICS_RESOURCE_FORM_EVIDENCE_20261009.csv дополнен отдельными LoadTilesetMargin/Arity4 и LoadTilesetMarginSpacing/Arity5. Хост получает заранее созданный RuntimeImageValue, поэтому доказательство не распространяется на декодирование PNG, видимое изображение или весь набор отрицательных параметров/предельных LONG. Наличие теста поля вызова хоста не подменяет проверку окна. Реализация не менялась, полный набор не повторялся.

Классификация: поддержка атласов с рамкой и промежутками уже есть во всех трёх формах LOADTILESET, новой функции не требуется. Следующий шаг — TEXTWIDTH/TEXTHEIGHT/POINT/PMAP и отдельно интерактивные функции ввода. Общий аудит активен.


## Session 1189 - 2026-10-09 00:40 - контекст графических функций

Прочитаны парные Interpreter/BytecodeVm тесты TEXTWIDTH/TEXTHEIGHT, обеих POINT и PMAP. Основной набор шести методов × два движка — 12/12, отдельно CanvasOperationsReportDocumentedDiagnostics — 2/2; ошибок/пропусков 0, .artifacts/test-results/graphics-context-1189. TEXTWIDTH/HEIGHT имеют заданные24/12 и16/24 для одной/двух строк в тестовом хосте; без CANVAS даютMB3010. POINT(number) требует SCREEN даже при CANVAS; селекторы физических/логических координат проверены после WINDOW/VIEW. POINT(x,y) проверен на физическом чтении CANVAS без повторной проекции CAMERA/VIEWPORT и на SCREEN с WINDOW. PMAP имеет заранее заданные прямые/обратные результаты для0..3, дробные1.5/102.5, selector4 даётMB3007.

Создан GRAPHICS_CONTEXT_FORM_EVIDENCE_20261009.csv с отдельной записью каждой формы; матрица пересчитана. Размеры текста относятся к тестовому хосту, не к реальному шрифту IDE; пиксельные значения хоста не доказывают окно или весь диапазон полноцветного вывода. Runtime не менялся, полный набор не повторялся.

Классификация: измерение текста и преобразование координат уже доступны через текущие формы, новая функция для этих задач не нужна. Контекстные ограничения не являются доказанной поломкой. Следующий шаг — функции клавиатуры/мыши и оставшийся файловый учёт с конкретными независимыми ожидаемыми результатами. Общий аудит активен.


## Session 1190 - 2026-10-09 00:42 - клавиатура мышь и очередь

Прочитаны и выполнены конкретные Runtime-тесты удержания/очереди/псевдонимов/сочетаний клавиш, мыши и CAMERA/VIEWPORT: 31/31. Выявлена слабость теста KeyDownReadsHeldKeyStateWithoutConsumingQueuedKeys: очередь была пуста, название не доказывало непотребление. В BytecodeVmTests.cs и InterpreterTests.cs добавлена очередь A, ожидается её сохранение для INKEY. Первый повтор 1/2: в Interpreter забыта замена вспомогательного создания хоста, очередь всё ещё пуста. Подготовка исправлена на TestRuntimeHost с keys A и pressedKeys LEFT/CTRL; итог 2/2. Runtime не менялся.

Девять выбранных ProgramConsoleServiceTests — 9/9: прямой ввод очереди, удержание/отпускание, очистка состояния без потребления очереди, мышь и объединение движений без потери кнопок. Результаты .artifacts/test-results/input-1190. Создан INPUT_FORM_EVIDENCE_20261009.csv для семи реальных форм. Сохранение несовпавших событий и однократное потребление проверены; тест MouseFunctionsReturnDefaultValuesWithoutMouseProviderState фактически ожидает MB3010, поэтому название не используется как доказательство нулевых значений.

Классификация: удержание, нажатие и очередь уже имеют разные API; не требуется объединять их новой функцией. Предел — поставляемое состояние тестового хоста и прямые методы IDE, не реальные события ОС или фокус окна. Полный набор не повторялся, семантика не менялась. Следующий шаг — оставшиеся файловые формы и сведение кандидатов отсутствующих возможностей отдельно от пробелов доказательств. Общий аудит активен.


## Session 1191 - 2026-10-09 00:44 - пакет строк и свойства FILE

Добавлен FileLineBatchEvidenceTests: оба движка × UTF-8/UTF-16LE/UTF-16BE с BOM, шесть новых. Файл emoji CRLF/пустая CRLF/Я LF/Б без завершающего разделителя подготовлен напрямую; ожидаются две первые строки включая пустую, LINENUMBER3, SKIPLINE TRUE/LINENUMBER4, остаток одна Б, EOFTRUE, пустой массив READLINES на EOF и MB3007 SKIPLINE на EOF. READLINES0 не сдвигает FILEPOS0. FILEPATH совпадает с абсолютным подготовленным путём, FILEMODE равен INPUT. После завершения точные исходные байты неизменны, временный файл удалён в finally.

Все шесть прошли с первого запуска; вместе с FilePositionBufferEvidenceTests — 42/42, ошибок/пропусков 0, .artifacts/test-results/line-batch-1191/line-batch-1191.trx. Создан FILE_BATCH_FORM_EVIDENCE_20261009.csv для четырёх конкретных форм. Runtime не менялся; полный набор не повторялся. Не доказаны OUTPUT/APPEND/закрытые FILE и все ошибки типов этими строками матрицы.

Классификация: пакетное чтение, пропуск строки и свойства дескриптора уже доступны; новые функции не нужны для этого сценария. EOF и пустая строка не взаимозаменяемы, подтверждён существующий договор. Следующий шаг — оставшиеся свойства путей/файлов и независимая сводка кандидатов отсутствующих возможностей. Общий аудит активен.


## Session 1192 - 2026-10-09 00:46 - компоненты путей Windows

Добавлен PathComponentEvidenceTests: 12 случаев двух движков для шести заданных путей (Unicode с двумя расширениями, корень, каталог с разделителем, .., UNC, .hidden), плюс два случая классификации шести форм абсолютности. Ожидания заданы буквально; существование ресурсов не требуется. Первый прогон31/33: неверно ожидался завершающий разделитель у UNC PATHROOT. Сверка реализации Path.GetPathRoot и результата показала \\server\share без завершающего разделителя, тогда как диск возвращается C:\. Ожидание уточнено, Runtime не менялся.

Повторный прогон с PathBaseResolutionEvidenceTests — 33/33, ошибок/пропусков0, .artifacts/test-results/path-components-1192/path-components-1192.trx. Добавлен PATH_COMPONENT_FORM_EVIDENCE_20261009.csv для семи форм, включая ранее проверенные FULLPATH/COMBINEPATH. C:file и \file не полностью абсолютны; EXTNAME(.hidden) равен .hidden; корень DIRNAME пуст. Сетевой доступ к UNC не проверяется по строковому разбору. Не доказаны все недопустимые пути и переменные среды.

Классификация: разбор/нормализация путей доступны существующими API; вычисление относительного маршрута между двумя полными путями этим не заменено, остаётся отдельным ранее отложенным кандидатом. Полного прогона не было, новая семантика и имена не введены. Следующий шаг — свойства/перечисление/переименование файлов, затем единая классификация оставшихся возможностей. Общий аудит активен.


## Session 1193 - 2026-10-09 00:47 - перечисление файлов и каталогов

Добавлен DirectoryListingEvidenceTests: два движка × GETFILES/GETDIRS × одна/две аргументные формы, восемь случаев. Собственный временный корень содержит три файла и три папки A/B/кириллица emoji, внутри A отдельный файл. Проверены заданный отсортированный список абсолютных путей, фильтры *.txt/A*, отсутствие вложенного файла в верхнем уровне, пустой массив при *.missing, MB3007 для пустого шаблона и отсутствующего каталога. Исходные тексты проверены отдельно после исполнения, собственный корень удалён в finally.

Все восемь прошли с первого запуска; с PathComponentEvidenceTests — 22/22, ошибок/пропусков0, .artifacts/test-results/listing-1193/listing-1193.trx. Создан DIRECTORY_LISTING_FORM_EVIDENCE_20261009.csv с четырьмя точными формами ID/Arity, матрица пересчитана. Runtime не менялся, полного прогона не было.

Классификация: отбор и перечисление верхнего уровня уже доступны. Этот набор не подтверждает рекурсивный API, все шаблоны Windows или возможность обхода прав/ссылок; потребность расширять язык ради текущего сценария не установлена. Следующий шаг — свойства и переименование/удаление, затем единая сводка отсутствующих возможностей. Общий аудит активен.


## Session 1194 - 2026-10-09 00:49 - переименование удаление и существование

Добавлен FileRenameDeleteEvidenceTests: 12 случаев двух движков для файла/каталога и успеха/конфликта/имени с путём, два случая удаления. Подготовлены настоящие Unicode-имена и двоичные байты0/255/128; при конфликте целевой байт42. После отказа обе стороны отдельно прочитаны, при успехе прежний путь отсутствует и новые байты точны; подкаталог из недопустимого имени не создан. DELETEDIR непустого каталога даётMB3007, сразу отдельно подтверждена сохранность файла. После явного удаления DELETEFILE/DELETEDIR возвращаютTRUE, повторFALSE. MAKEDIR создаёт вложенный путь, DELETEDIRRECURSIVE удаляет дерево с пустой вложенной папкой и повторно даётFALSE. FILEEXISTS/DIREXISTS/PATHEXISTS различают файл и папку.

14/14 с первого запуска, ошибок/пропусков0, .artifacts/test-results/rename-delete-1194/rename-delete-1194.trx. Создан FILE_MUTATION_FORM_EVIDENCE_20261009.csv для девяти конкретных форм. Runtime не менялся, полный набор не повторялся. Пределы: не все недопустимые имена, ссылки/права/гонки/сбой носителя; вложенные файлы DELETEDIRRECURSIVE этим сценарием не проверены.

Классификация: переименование в том же каталоге и явные варианты удаления уже доступны; отказ удалить непустую папку DELETEDIR соответствует отдельной рекурсивной форме, новой функции не требуется. Следующий шаг — метаданные файлов, текстовые целофайловые формы и сводка оставшихся возможностей. Общий аудит активен.


## Session 1195 - 2026-10-09 00:51 - целый текстовый файл и кодировки

Прочитан ExplicitTextEncodingTests: реальные файлы, обе формы движков, UTF-8/UTF-16LE/UTF-16BE/Windows1251, заданные строки/байты, BOM и отказ на повреждённых входах/несовместимом BOM с сохранением байтов. Добавлен WholeFileDefaultEvidenceTests, два случая обоих движков: А/emoji/CRLF/пустаяLF/Б, READFILE сохраняет текст, READALLLINES даёт три элемента включая пустой, после пустой WRITEFILE READFILE пуст/READALLLINES0. Повторная запись/добавление сверены с буквально заданными11 UTF8-байтами, без BOM.

Первый прогон34/34; после усиления буквальных байтов повтор34/34, ошибок/пропусков0, .artifacts/test-results/whole-file-1195/whole-file-1195.trx. Создан WHOLE_TEXT_FORM_EVIDENCE_20261009.csv с восемью отдельными формами, матрица пересчитана. Runtime не менялся, полного прогона не было. Семантика без явной кодировки не объявлена строгой валидацией повреждённого текста; это отдельный существующий договор fallback.

Классификация: чтение/запись/добавление Unicode-текста и массив строк доступны; общего двоичного позиционного режима это не создаёт и строгие формы не заменяют гарантию атомарности. Новые функции не введены. Следующий шаг — временные пути/метаданные/диски и сведение действительно отсутствующих возможностей. Общий аудит активен.


## Session 1196 - 2026-10-09 00:52 - независимые метаданные файлов

Добавлен FileMetadataEvidenceTests: оба движка, настоящий файл с Unicode-именем и пятью заданными байтами. Прямо установлены разные местные Creation/Write/Access времена и Write время каталога, затем подготовка подтверждена чтением свойств до запуска языка. Ожидаемые строки дат заданы буквально, не вычислены производственным форматтером. Hidden/ReadOnly включены и сняты, ожидаются TRUE/TRUE и FALSE/FALSE. Каждая из семи функций на отсутствующем объекте даёт перехваченный MB3007. Исходные байты сохранены, атрибуты сбрасываются перед удалением своего временного корня.

Два новых комплексных случая прошли с первого запуска; вместе с FileRenameDeleteEvidenceTests —16/16, ошибок/пропусков0, .artifacts/test-results/metadata-1196/metadata-1196.trx. Создан FILE_METADATA_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Пределы — проверенная Windows и текущий том; не все часовые пояса/файловые системы, автоматическое обновление доступа, права и ссылки. Runtime не менялся, полного прогона не было.

Классификация: размеры в байтах, местные даты и признаки доступны существующим API. UTC-формы, изменение атрибутов из самого языка и иные расширения этим не добавлены и не объявлены обязательными без отдельной потребности. Следующий шаг — диски/временные файлы/текущий каталог и оставшийся CDBL, затем сведение отсутствующих возможностей. Общий аудит активен.


## Session 1197 - 2026-10-09 00:54 - текущий каталог и временные объекты

Добавлен DirectoryAndTempEvidenceTests: четыре случая обоих движков. CURRENTDIR равен базе запроса, CHANGEDIR меняет её на Unicode-каталог, относительный WRITEFILE создаёт файл только в нём. Отказ CHANGEDIR missing даёт MB3007 и сохраняет базу; Environment.CurrentDirectory до/после неизменен. MAKETEMPFILE/MAKETEMPDIR создают четыре разных пустых объекта в TEMP с уникальным префиксом; префикс с путём отклоняется. Перед удалением каждого созданного объекта проверяются абсолютный родитель TEMP и собственный уникальный префикс; каталоги удаляются без рекурсивного обхода.

4 новых с первого запуска; с PathBaseResolutionEvidenceTests —23/23, ошибок/пропусков0, .artifacts/test-results/temp-base-1197/temp-base-1197.trx. Создан TEMP_BASE_FORM_EVIDENCE_20261009.csv, матрица пересчитана. Не доказаны конкурентное создание, защита подмены, все ошибки префиксов/прав и сбои носителя. Runtime не менялся, полного прогона не было.

Классификация: смена базы программы и обычное создание временных объектов уже доступны; отдельные новые функции не нужны для проверенного сценария. Создание имени проверкой существования перед созданием не заявлено как гарантия против гонок. Следующий шаг — сведения о дисках и CDBL, затем независимая сводка отсутствующих возможностей. Общий аудит активен.


## Session 1198 - 2026-10-09 07:18 - независимые сведения о диске и CDBL

Добавлен DriveInfoEvidenceTests: оба движка, корень TEMP-диска, несуществующий путь. Windows CIM Win32_LogicalDisk запрошен отдельным скрытым PowerShell-процессом, затем имя/формат/категория/полный размер Axiom сверены с этой независимой инвентаризацией. Для FREESPACE/TOTALFREESPACE доказаны только границы0..Size, не точное число или равенство доступного/общего свободного места. Не создаётся проверяемый отсутствующий путь. Проверенная машина: C:, NTFS, Fixed,511523409920 байт по CIM.

Повторно выполнен NumericPromotionContractTests.ExplicitDecimalToDoubleConversionCanRoundAndPreservesOrdinaryValues в обоих движках, прочитаны его заданные результаты. Два новых + два повторных —4/4, ошибок/пропусков0, .artifacts/test-results/drive-conversion-1198/drive-conversion-1198.trx. Создан DRIVE_FORM_EVIDENCE_20261009.csv, NUMERIC_CONVERSION_FORM_EVIDENCE_20261009.csv дополнен CDbl/Arity1. Runtime не менялся.

Матрица имеет кандидаты доказательств для всех314 зарегистрированных форм, но SemanticStatus остаётся UNVERIFIED_PER_FORM: указатель не доказывает полные типы, все края, диагностики, ограничения хоста и независимость каждого исходного теста. Поэтому аудит не завершён достижением числа314. Пределы дискового набора — текущий локальный том, не сеть/квоты/неготовые или съёмные носители.

Следующий шаг — свежий полный прогон решения после накопленных тестовых изменений, затем проверка качества доказательств и единая классификация отсутствующих возможностей по исходной задаче. Общий аудит активен.


## Session 1200 - 2026-10-09 07:25 - точный DECIMAL ввод и готовая строка

Добавлен DecimalTextEvidenceTests: четыре выбранные строки × оба движка и два отказа CDEC(STRING), десять новых случаев. INPUT в DECIMAL сохраняет буквально заданные 2^53+1, длинные дроби и DECIMAL.MaxValue. CDEC(VAL(text)) не равен первым трём точным значениям и даётMB3008 для последнего. CDEC переменной STRING отклонён с MB2 до исполнения. Исходники реестра/общего парсера подтверждают числовую CDEC и прямой decimal.TryParse в INPUT. FUTURE не содержит отдельно такой формы.

Сначала восемь новых +78 NumericPresentationEvidenceTests —86/86; после двух проверок связывания —88/88, ошибок/пропусков0, .artifacts/test-results/decimal-text-1200/decimal-text-1200.trx. Создан DECIMAL_TEXT_EVIDENCE_20261009.md с ожидаемыми/фактическими результатами и границами выводов. Runtime не менялся. Последний полный набор5332/5332 Session1199 предшествует этим десяти новым тестам и не выдаётся за повторный полный прогон.

Классификация: для интерактивного ввода есть существующий точный путь в проверенных случаях; для готовой STRING специализированного преобразования нет. VAL/CDEC не сломаны своим договором DOUBLE/числового аргумента. Сценарий чтения десятичных данных из файла требует проверки пользовательского FUNCTION-разбора и грамотного выбора грамматики перед введением новой сущности. Не заявлена точность INPUT для всех строк: избыточная дробная точность может округляться.

Следующий шаг — оценить простой пользовательский точный разбор десятичной строки на границах и отделить удобство новой функции от необходимой возможности; затем сверить planned средства языка с исходниками. Общий аудит активен.


### Session 1201 — пользовательский разбор DECIMAL

FixedDecimalParserEvidenceTests: 17 входов × два движка, без перехода на интерпретатор; с DecimalTextEvidenceTests44/44. Запускаемый samples/functions/cdec/fixed_decimal_parser.axm использует существующие пользовательские функции и числовые/строковые операции. Это доказательство ограниченной пользовательской альтернативы, не новой встроенной формы CDEC(STRING). Пределы мантиссы/масштаба проверены явно; отдельного измерения быстродействия нет.


### Session 1203 — строковый INPUT

InputStringEvidenceTests:34 новых случая обоих движков; одиночная STRING сохраняется буквально, две цели разделяют поля, при неверном числе полей MB3003 и исходные значения сохранены. С PlannedSyntaxEvidenceTests68/68, без перехода на другой движок. samples/operators/input/whole_string.axm реально выполнен CLI; отдельного измерения быстродействия и проверки окна SCREEN нет.


### Session 1205 — LOADTILEMAP с метаданными

TilePropertyAcceptanceSampleTests58/58, настоящий загрузчик IDE в обоих режимах без перехода: metadata_validation.axm подтверждает загрузку поддержанного объекта и MB3005 для эллипса. Прямой загрузчик17/17 и весь Runtime1070/1070. Модель сохраняет свойства/объекты, но новые пользовательские функции ещё не зарегистрированы. manual/tilemaps.html обновлён. Отдельное быстродействие не измерялось.


### Session 1206 — функции свойств, объектов и координат TILEMAP

Все21 новые формы имеют обе диспетчеризации, собственные русские samples/functions/<имя>/map_access.axm и разделы manual/tilemaps.html/поискового индекса. Реальный IDE-набор255/255, неверные вызовы88/88; проверены буквальные результаты, пустые карты, крайние индексы и изменение/очистка клетки. TILE_METADATA_FORM_EVIDENCE_20261009.csv и инвентаризация335 форм обновлены. Это ограниченные семантические проверки, не доказательство всех входов. Полный прогон решения пока выполняется.


### Завершение Session 1206 — 2026-10-09 08:01

Тот же terminal5696 завершился с кодом0. Проверены девять TRX:5738 всего,5738 passed,0 failed,0 notExecuted; Runtime1070, Execution2896, IDE856. Точные счётчики и имена файлов сохранены в .artifacts/tile-full-1206-summary.json. Промежуточная запись о живом процессе заменяется этим окончательным результатом. Ссылки/разделы/примеры/слова обеих тем найдены для всех21 новых функций; git diff --check прошёл.

Сохранение данных и доступ через язык подтверждены полным набором, но TILE26-001 не объявлен целиком закрытым из-за потери причины отказа в GraphicsHostService. Следующий шаг — сохранить конкретную причину LOADTILEMAP и проверить её в обоих движках на неподдержанных/неверных данных; затем расширить проверку объявленного Tiled-объёма. Общий аудит остаётся активным,335 кандидатов доказательств не равны полной проверке всех форм.


### Session 1207 — отказ LOADTILEMAP

Причина ресурсного ellipse проходит до CATCH обоих движков; адресный IDE-набор232/232. TilemapLoadFailureTests2/2 доказывает по одному обращению к хосту на явно записанную команду и успех следующего вызова после отказа. metadata_validation.axm печатает причину. Полный набор после изменения ещё выполняется; весь отложенный Tiled-объём не объявлен проверенным.


### Завершение Session 1207 — 2026-10-09 08:10

Тот же terminal37971 завершился с кодом0. Девять TRX:5742 всего,5742 passed,0 failed,0 notExecuted; Runtime1070, Execution2898 (3m24s), IDE858. .artifacts/tile-reason-full-1207-summary.json содержит окончательные счётчики. Промежуточные записи о выполнении и неполная сводка по шести готовым TRX не являются итогом; окончательный результат подтверждён только после завершения процесса. git diff --check прошёл.

TILE26-001: подтверждённая потеря поддержанных свойств/объектов и причины отказа исправлена и проверена в пределах карточки. Это не объявление полного соответствия всем разновидностям Tiled. Следующий шаг — самостоятельные проверки неверных размеров и JSON-типов, особенно ArgumentException при построении карты/тайлсета, чтобы исключение .NET не выходило наружу вместо диагностики языка; затем остальные явно отложенные формы. Общий аудит активен.


### Session 1208 — LOADTILEMAP с неверными полями

TilemapInvalidInputTests40 случаев обеих движков, реальные файлы и PNG; все дают CATCH/MB3005 и сохраняют прежнюю ширину. Адресный IDE274/274, полные Runtime1070/1070 и IDE900/900. invalid_dimensions.axm/negative_dimensions.tmj и manual обновлены. Реестр335 форм не изменён; отдельное быстродействие не измерялось. Полного нового прогона решения не было.


### Session 1209 — TILE26-004/005: ранние отказы

Подтверждены и исправлены молчаливое игнорирование wangsets/terrains/tiles[].terrain и принятие capsule как RECT. Эти возможности остаются отложенными: наличие поля метаданных, даже пустого, и форма capsule дают MB3005 с причиной в обоих движках, для встроенного и внешнего тайлсета. Адресные проверки320/320 и запускаемый samples/functions/loadtilemap/unsupported_tileset_metadata.axm. Подробности: docs/audit/TILEMAP_UNSUPPORTED_FEATURES_20261009.md. Это исправление договора отказа, а не новые API; весь формат Tiled не объявлен проверенным.


### Session 1210 — TILE26-006/007: числа в параметрах слоя

Поля opacity/offsetx/offsety/parallaxx/parallaxy требуют конечного DOUBLE; opacity — от0 до1 включительно. Неверные поля дают MB3005 с причиной, прежняя карта сохраняется. Отрицательные конечные смещения и параллакс допустимы. До изменения14/16 случаев ошибочно приняты; исправление проверяется в обоих движках и видах слоя. Новый пример samples/functions/loadtilemap/nonfinite_layer.axm. Подробности docs/audit/TILEMAP_LAYER_NUMERIC_20261009.md. Это проверка существующих параметров, новых API и измерений быстродействия нет.


Итог Session1210: полный Runtime1070/1070, полный IDE992/992; ошибок и пропусков0, оба процесса завершились кодом0. Итоговый набор TilemapLayerNumericTests44/44 вошёл в полный IDE, включая точные причины и оба вида слоя. nonfinite_layer.axm выполнен обоими движками через IDE. Сводка .artifacts/layer-numeric-1210-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг остаётся mode/tintcolor, геометрия тайлсета и большие конечные координаты при рисовании.


### Session 1211 — TILE26-008/009: настройки отображения слоя

Первый объём допускает отсутствующий mode либо строку normal; отсутствующий tintcolor либо непрозрачный белый #FFFFFF/#FFFFFFFF. Ненейтральное смешивание и окрашивание не реализованы и теперь вызывают MB3005 с причиной вместо молчаливой потери поведения. Неверные типы полей тоже отклоняются, прежняя карта сохраняется, оба вида слоя/движка. Уточнённое исходное воспроизведение24/32 ошибочных принятия; белый цвет исключён из дефектов как нейтральный. Итоговый набор44 случая и samples/functions/loadtilemap/unsupported_blend.axm. Подробности docs/audit/TILEMAP_LAYER_APPEARANCE_20261009.md. Новых API и измерений быстродействия нет.


Итог Session1211: адресные проверки412/412; полный Runtime1070/1070 и полный IDE1038/1038. Ошибок и пропусков0, процессы завершились кодом0. TilemapLayerAppearanceTests44/44 и unsupported_blend.axm обоих движков вошли в проверки. Сводка .artifacts/layer-appearance-1211-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг — геометрия тайлсетов и большие конечные координаты при рисовании; общий аудит языка остаётся активным.


### Session 1212 — TILE26-010/011: геометрия атласа

Исправлены переполнение удвоенного margin, суммы полезной длины со spacing и шага сетки. Промежуточная арифметика long применяется общей моделью ручной загрузки и Tiled-тайлсета. Невозможный большой отступ отклоняется, один тайл с большим неиспользуемым spacing принимается. До исправления4/4 ошибочных результатов реального LOADTILESET в обоих движках. Пять прямых проверок геометрии и расширенные8 проверок обоих загрузчиков. Пример samples/functions/loadtileset/arithmetic_boundaries.axm; руководство обновлено. Подробности docs/audit/TILESET_ARITHMETIC_20261009.md. Новых имён языка и измерений быстродействия нет.


Итог Session1212: полный Runtime1075/1075 и полный IDE1048/1048, ошибок и пропусков0, процессы завершились кодом0. Пять новых прямых проверок и восемь проверок реальной загрузки вошли в полные наборы, arithmetic_boundaries.axm выполнен обоими движками. Сводка .artifacts/tileset-arithmetic-1212-summary.json. Проверки MSBuild последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Следующий шаг — tileoffset и большие координаты рисования; общий аудит языка не завершён.


### Session 1213 — TILE26-012/013: границы видимости

При допустимых координатах DRAWTILEMAP0,0 подтверждены4 потери пикселя для компенсированных tileoffset/offset слоя и4 InfrastructureFailure для далёкого слоя±100000000000. Экстенты и промежуточные номера клетки теперь long, приведение int выполняется после ограничения размерами слоя. Входы вне графического диапазона±1000000000 исключены из дефектов; диапазон оператора не расширен. Пример samples/operators/drawtilemap/culling_boundaries.axm и manual обновлены. Полный Runtime1079/1079; подробности docs/audit/TILEMAP_CULLING_BOUNDARIES_20261009.md. Крайние DOUBLE/произведения/long-суммы ещё требуют независимой проверки, общего доказательства всех координат нет.


Итог Session1213: полный Runtime1079/1079 и полный IDE1058/1058, ошибок и пропусков0, процессы завершились кодом0. Новые10 IDE-проверок прошли:8 подтверждённых граничных случаев и2 запуска culling_boundaries.axm; видимый пиксель совпадает с независимым DRAWTILE(12), далёкий слой завершается нормально. Сводка .artifacts/tile-culling-1213-summary.json. MSBuild-проверки последовательны; git diff --check прошёл. Полный набор всего решения в этой сессии не запускался. Книга Зельднера:графический PUT и пример мяча, строки8844–8911 извлечённого текста, только редакционный мотив. Общий аудит языка активен; крайние DOUBLE-произведения/long-суммы остаются следующей независимой проверкой.


### Session 1214 — TILE26-014/015: крайние преобразования слоя

Конечные крайние смещения и параллакс больше не насыщаются отдельно до long перед вычитанием:точная компенсация сохраняется, относительная арифметика Int128, редкие большие величины BigInteger. Переполненное camera*parallax не заменяется0; слой отсекается. Исходные28/28 ошибочных результатов обоих движков/осей исправлены в адресном38/38, итоговый набор80 случаев обоих операторов и2 запуска extreme_parallax.axm проверяются полным решением. Округление отдельных величин от нуля сохранено. Подробности docs/audit/TILEMAP_EXTREME_TRANSFORMS_20261009.md. Новых API нет, полного доказательства всех форм языка нет.


### Session 1215 — завершение текущего тайлового этапа

См. docs/audit/TILEMAP_LOADER_CLOSURE_20261009.md: адресный набор494/494; строгие границы загрузки и выбор firstgid проверены обоими движками. Добавлен samples/functions/loadtilemap/loader_boundaries.axm и описание в manual/tilemaps.html. Отложенные особенности Tiled отклоняются с MB3005, а не объявляются реализованными. Следующий раздел общего аудита пока не начинать. Точка возобновления и сводная таблица — AUDIT_HANDOFF.md в корне репозитория.

## Session 1218 — GRAPHEME на повреждённых UTF-16 строках

UnicodeMalformedGraphemeEvidenceTests подтверждает LEN/LEFT/RIGHT/MID с явной единицей: восемь наборов непарных суррогатов, точные части, префиксы/суффиксы и предельный INTEGER в обоих движках. Фактический BytecodeVm проверен явно. Три группы отрицательных границ UTF16/CODEPOINT/GRAPHEME дают одинаковый MB3007. Совместно 71/71; подробности docs/audit/UNICODE_MALFORMED_GRAPHEME_20261009.md. Это адресное доказательство паритета, не полный Unicode-корпус; Runtime и производительные сценарии не менялись.

## Session 1219 — INSTR/INSTRREV на границах UTF-16

UnicodeSearchBoundaryEvidenceTests: 48 новых проверок обоих движков для четырёх форм, непарных суррогатов/половин пары, стартов внутри пары, пустого фрагмента и отсутствия нормализации. Фактический BytecodeVm проверен. Совместно105/105. Подробности docs/audit/UNICODE_SEARCH_BOUNDARIES_20261009.md. Полнота семейства не объявлена; Runtime и производительные сценарии не менялись.

## Session 1220 — целочисленные границы и отказ NUL

36 новых числовых случаев подтверждают соседние DOUBLE границ LONG и усечение DECIMAL; совместно146/146. PATH26-002: общий обработчик отклоняет NUL до раскрытия переменных;5 групп ошибок в обоих движках и2 запуска null_character.axm, совместно56/56. Руководство обновлено, новых токенов/форм нет. Подробности docs/audit/PATH_NULL_CHARACTER_20261009.md. Полный прогон ещё выполняется; производительность не измерялась.

## Session 1221 — NUL в путях четырёх ресурсов

В обоих движках8 исходных ошибочных загрузок префикса LOADIMAGE/LOADSPRITE/LOADTILESET/LOADTILEMAP исправлены до раскрытия переменных. Добавлены2 запуска null_path.axm,4 страницы руководства. Адресно186/186; полный IDE пока выполняется. Вложенные TMJ/TSJ-пути этим набором не доказаны. Отчёт docs/audit/RESOURCE_NULL_PATH_20261009.md. Новых инструкций и производительных сценариев нет.

## Session 1222 — вложенные пути LOADTILEMAP

NestedResourceNullPathEvidenceTests подтверждает MB3005 для NUL в3 местах TMJ/TSJ image/source, оба движка,6 новых случаев. Совместно16/16. Нет нового поведения/инструкций/измерений; полный IDE1228 относится к предыдущей ревизии тестов. Отчёт docs/audit/NESTED_RESOURCE_NULL_PATH_20261009.md.

## Session 1223 — REVERSE на повреждённых строках

ReverseMalformedUnicodeEvidenceTests:16 новых случаев обоих движков с буквальными ожидаемыми последовательностями UTF16; исходная переменная и длина проверены. Совместно54/54. Отчёт docs/audit/REVERSE_MALFORMED_UNICODE_20261009.md. Реализация, руководство и производительные сценарии не менялись; все Unicode-строки не доказаны.

## Session 1224 — окружение в путях

PathEnvironmentEvidenceTests подтверждает известную/неизвестную переменную в обоих движках: FULLPATH,READFILE,FILEEXISTS,FILENAME,DIRNAME,EXTNAME,ISABSOLUTEPATH и лексический COMBINEPATH.6 новых случаев с2 запусками environment_expansion.axm, совместно27/27. Manual ISABSOLUTEPATH уточнён, Runtime/производительность не менялись. Отчёт docs/audit/PATH_ENVIRONMENT_20261009.md.

## Session 1225 — неверный вид объекта и атрибуты каталога

FileMetadataKindEvidenceTests:12 новых случаев обоих движков,4 функции файла на каталоге/DIRDATE на файле, атрибуты Hidden/ReadOnly каталога. Совместно20/20. Дефект не обнаружен, байты/атрибуты сохранены, Runtime и производительные сценарии не менялись. Отчёт docs/audit/FILE_METADATA_KIND_20261009.md.

## Session 1226 — STR/VAL на краях DOUBLE

42 новых случая обоих движков/3 культур, буквальный текст и знак нуля; совместно160/160. Цепочка VAL→STR→VAL не объявлена независимым доказательством битового разбора всех чисел. Runtime/производительность не менялись. Отчёт docs/audit/DOUBLE_TEXT_BOUNDARIES_20261009.md.

## Session 1227 — BOM явных файловых форм

ExplicitBomBoundaryEvidenceTests:36 новых случаев чтения2 форм и2 движков, только/повторный/оборванный BOM и6 несовпадений. Совместно68/68 с прежними явными записью/дописыванием. Дефект не обнаружен, Runtime/производительность не менялись; все ошибочные последовательности не доказаны. Отчёт docs/audit/EXPLICIT_BOM_BOUNDARIES_20261009.md.


### Session 1228 — READLINES на границах

Оба движка: неоткрытый/закрытый FILE, ноль, отрицательный count с сохранением позиции и INTEGER.MaxValue на коротком/пустом файле. FILE26-002 исправлен в общем RuntimeTextFile;22/22 совместно. Руководство и samples/functions/readlines/maximum_count.axm обновлены; новых форм нет. Полный прогон ожидается.


### Session 1229 — READTEXT/2

Максимальный INTEGER на пустом/коротком UTF-8, оба движка, точные текст/позиция/EOF, файл сохранён. FILE26-003 исправлен; совместно22/22 с прежними Unicode/пакетными проверками и2 запусками maximum_count.axm. Руководство обновлено.


### Session 1230 — DECIMAL-границы

CDEC/FLOOR/CEIL/ROUND/FIX/SGN: точный INPUT DECIMAL, соседние половины, минимальная дробь, большие значения и пределы; оба движка. Совместно58/58, дефекта нет. Это адресное доказательство перечисленных границ, не полное закрытие всех входных типов.


### Session 1231 — CDEC DOUBLE→DECIMAL

Диапазон/малые значения/ноль,30 новых случаев обоих движков; FINALLY и позиция MB3008 проверены, совместно118/118. VAL используется для подготовки, не заявлено независимое доказательство каждого бита. Поведение не менялось.


### Session 1232 — CDBL DECIMAL→DOUBLE

Точные целые около2^53 с обоими знаками, минимальная дробь сохраняет ненулевой результат/знак;32 новых случая обоих движков, совместно132/132. Не доказана точная битовая дробь для всех DECIMAL; поведение не менялось.


### Session 1233 — ASC/CHR и Unicode-формы

Оба движка: переход255/256, края суррогатов/Unicode, кириллица/CP866, первые правильные/непарные суррогаты.20 новых случаев, совместно58/58 с прежней полной таблицей256 кодов CP866 и отказами. Поведение не менялось; это не перебор всех Unicode-кодов.


### Session 1234 — шесть форм обрезки

TRIM/LTRIM/RTRIM с1/2 аргументами:25 Unicode-пробелов и5 сохраняемых, явные UTF-16-наборы и повреждённый результат.62 новых случая обоих движков, совместно100/100. Поведение не менялось; членство пробелов задано буквально.


### Session 1235 — REPLACE/REPLACEMANY

UTF-16-половины, NUL, длинное правило/дубликаты/порядок, отсутствие каскада и сохранность таблиц.6 новых групповых случаев обоих движков, совместно63/63; поведение не менялось.


### Session 1236 — SPLIT/JOIN

Половины UTF-16-пары/NUL/пустые элементы/целый разделитель;6 новых групповых случаев обоих движков, совместно59/59 с прежними формами, рангами и границами массивов. Поведение не менялось; JOIN без разделителя использует запятую.


### Session 1237 — GETFILES/GETDIRS

Точные Unicode-маски/буквальный порядок при tr-TR/NUL;20 новых случаев обоих движков, совместно28/28 с прежними однопараметрическими и обычными формами. Байты сохранены, поведение не менялось.


### Session 1238 — COPYFILE/MOVEFILE

Настоящий FileShare.None источника/приёмника и отсутствующий источник, overwrite=TRUE, сохранность/освобождение.12 новых случаев обоих движков, совместно44/44 с прежними двух-/трёхаргументными формами и ссылками. Поведение не менялось; атомарность при отказе носителя не объявлена.


### Session 1239 — RENAMEFILE/RENAMEDIR

Пустое/пробельное имя, точки, разделители, NUL;28 новых случаев обоих движков, сохранность исходного/соседнего объекта и FINALLY. Совместно42/42 с прежними успешными/конфликтными случаями. Поведение не менялось.


### Session 1240 — временные объекты

MAKETEMPFILE/MAKETEMPDIR:13 неверных префиксов Windows,52 новых случая обоих движков, отсутствие созданных объектов с собственным уникальным префиксом, FINALLY. Совместно56/56 с прежними уникальными пустыми объектами. Поведение не менялось.


### Session 1241 — INT/ABS/SQR

Границы INTEGER/LONG/DECIMAL, минимальные DOUBLE, знак нуля, точный корень степени2, FINALLY.30 новых случаев обоих движков, совместно152/152; поведение не менялось. Не заявлена точность всех корней.


### Session 1242 — MIN/MAX/CLAMP

Точные соседние пределы LONG/DECIMAL, минимальная дробь, вырожденные/обратные границы;6 новых групповых случаев обоих движков, совместно130/130 с матрицей типов. Поведение не менялось, точность всех DOUBLE не объявлена.


### Session 1243 — LOG/LOG10/LOG2/EXP/POW

38 окончательных случаев обоих движков, совместно160/160: степени2/малые величины/домены/переполнение/знаки нуля. Точность середины POW(2,-1075) НЕ закрыта, NUM26-CAND-001 открыт; ограниченный тест проверяет только интервал. Runtime не менялся.


### Session 1244 — CBRT и тригонометрия

Знаки нуля/опорные значения/границы ASIN/ACOS/ATN2 и интервалы CBRT крайних DOUBLE;44 новых случая обоих движков, совместно166/166. Поведение не менялось; последний бит произвольных результатов не доказан, NUM26-CAND-001 открыт.


### Session 1245 — гиперболические функции

SINH/COSH/TANH/ASINH/ACOSH/ATANH: нули/малые/крайние величины и домены,48 новых случаев обоих движков, совместно170/170. Поведение не менялось; доказаны интервалы, не все последние биты. NUM26-CAND-001 открыт.


### Session 1246 — SETSIGN/RND

64 сочетания крайних значений/четырёх источников знака и2 управляемых RND, оба движка; совместно103/103. Поведение не менялось. Статистическая равномерность/дефектные хозяева/биты нуля DECIMAL не доказаны.

## Session 1247 — агрегаты массивов и DECIMAL

Подтверждён ARRAY26-001 P2: ARRAYAVERAGE массива DECIMAL выбрасывал InvalidOperationException вне диагностики языка, поскольку RuntimeValue.AsDouble не принимает DECIMAL. Общая реализация теперь явно преобразует DECIMAL перед точным накоплением DOUBLE. Новые24 случая проверяют пустые/невыделенные массивы, крайние LONG/DECIMAL, промежуточное переполнение суммы, двумерные DOUBLE и сохранность источника; включая decimal_mean.axm в обоих движках. Совместно67/67; независимые проверки точного среднего Runtime5/5. Уточнено руководство: невыделенный массив MB3005, выделенный пустой MB3007. Полный прогон запущен, его итог пока не установлен.

Отчёт docs/audit/ARRAY_AGGREGATE_BOUNDARIES_20261009.md. Снимок1247:109 ограниченных форм из335,226 ещё не рассмотрены. Далее строковые ARRAYMIN/ARRAYMAX и представления массивов: Unicode, порядок сравнения и границы видимой области. NUM26-CAND-001 открыт, общий аудит/goal активны.

### Итог полного прогона Session1247

После исправления ARRAY26-001 полный dotnet test Axiom.sln --no-restore завершён успешно:6930/6930, ошибок/пропусков0, девять отдельных TRX в .artifacts/test-results/full-1247. Включены прежние исправления READTEXT/READLINES/путей и новые проверки. Это автоматическая регрессия, а не закрытие всех335 форм или открытого NUM26-CAND-001. Далее Unicode строковых агрегатов/представления; goal активен.

## Session 1248 — Unicode и видимая область агрегатов

ArrayAggregateUnicodeViewEvidenceTests:4 новых групповых случая обоих движков. ARRAYMIN/ARRAYMAX сравнивают UTF-16: канонически разные строки, суррогатная пара против U+E000, отдельные половины, префикс пары, пустая строка/NUL и строка с конечным NUL. Двумерный DECIMAL-срез и вложенное частичное индексирование читают только видимые элементы, сохраняют источник; четыре агрегата и точное среднее0.5. Совместно52/52, ошибок/пропусков0. Первые прогоны исправляли ошибки теста: ROW/COLUMN — существующие константы; сумма DECIMAL печатается4.00 с сохранённым масштабом. Runtime/поведение не менялись, новый пример не требуется. Отчёт docs/audit/ARRAY_AGGREGATE_UNICODE_VIEWS_20261009.md. Снимок1248 сохраняет109 ограниченных форм из335,226 ещё не рассмотрены. Далее LCASE/UCASE: прежние независимые/поимённые проверки и Unicode за пределами BMP, смешанные непарные суррогаты. NUM26-CAND-001 открыт, goal активен.

## Session 1249 — регистр Unicode за пределами BMP

CaseSupplementaryUnicodeEvidenceTests:2 новых групповых случая обоих движков, простые соответствия U+10400/U+10428 и U+03C2/U+03A3 сверены с официальным UnicodeData. Проверены длина UTF-16, смешанные непарные суррогаты/NUL, комбинируемый акцент, неизменность эмодзи и предельного скаляра. Совместно40/40 с прежними поимёнными/независимыми проверками, включая три культуры. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CASE_SUPPLEMENTARY_UNICODE_20261009.md. Снимок1249:111 ограниченных форм из335,224 ещё не рассмотрены. Далее REPEAT/SPACE/STRING/PADLEFT/PADRIGHT/CENTER: единицы UTF-16 и заполнение, после чтения прежних доказательств. NUM26-CAND-001 открыт, goal активен.

## Session 1250 — заполнение строк и непарные суррогаты

StringFillMalformedUnicodeEvidenceTests:2 новых групповых случая обоих движков,19 буквальных ожиданий каждый. REPEAT сохраняет весь фрагмент, STRING/явное заполнение используют первую единицу UTF-16, SPACE/неявное заполнение используют пробел; проверены непарные половины/NUL, пустая строка, ширина меньше/равна длине и нечётное дополнение CENTER справа. Совместно55/55 с прежними независимыми/поимёнными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/STRING_FILL_MALFORMED_UNICODE_20261009.md. Снимок1250:120 ограниченных форм из335,215 ещё не рассмотрены. Далее INSERT/REMOVE: границы UTF-16/непарные половины и предельные позиции после сверки прежнего договора. NUM26-CAND-001 открыт, goal активен.

## Session 1251 — редактирование единиц UTF-16

StringEditMalformedUnicodeEvidenceTests:2 новых групповых случая обоих движков,13 буквальных ожиданий каждый. INSERT/REMOVE разделяют и вновь соединяют суррогатную пару, сохраняют непарные половины/NUL, работают с комбинируемым акцентом и предельными позициями/количествами без переполнения индекса. Совместно55/55 с прежними независимыми/поимёнными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/STRING_EDIT_MALFORMED_UNICODE_20261009.md. Снимок1251:122 ограниченные формы из335,213 ещё не рассмотрены. Далее открытые файловые формы FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE: прежние доказательства, закрытые состояния и сохранность позиции. NUM26-CAND-001 открыт, goal активен.

## Session 1252 — свойства закрытого FILE и позиция Unicode

FileClosedPropertiesEvidenceTests:20 новых случаев, оба движка, неоткрытый/закрытый FILE и FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE. MB3007 перехватывается, FINALLY/продолжение выполняются, ISOPEN остаётся FALSE, исходные UTF-8-байты сохранены и дескриптор доступен эксклюзивно. Совместно62/62 с FilePositionBufferEvidenceTests/FileLineBatchEvidenceTests, включая UTF-8/UTF-16LE/BE на границах буфера и сохранность позиции после неверного SEEKFILE. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_CLOSED_PROPERTIES_20261009.md. Снимок1252:127 ограниченных форм из335,208 ещё не рассмотрены. Далее свойства/позиционирование OUTPUT/APPEND: FileOutputPositionEvidenceTests и сохранность после отказа APPEND. NUM26-CAND-001 открыт, goal активен.

## Session 1253 — отказ SEEKFILE после буферизованного APPEND

AppendBufferedSeekFailureEvidenceTests:2 новых случая обоих движков, UTF-8 без BOM, запись😀 до запрещённого SEEKFILE(0/LONG.MaxValue), FINALLY/сохранность позиции6 и длины6, последующая запись NUL+Б до9. Итоговые9 байтов буквальные; дескриптор освобождён. Совместно34/34 с прежними OUTPUT/APPEND и закрытыми состояниями, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/APPEND_BUFFERED_SEEK_FAILURE_20261009.md. Снимок1253 сохраняет127 ограниченных форм из335. Далее FILEPATH/FILEMODE/FILEPOS/FILELENGTH/SEEKFILE: проверить статические типы/неверные аргументы по зарегистрированным формам, затем оставшиеся файловые формы. NUM26-CAND-001 открыт, goal активен.

## Session 1254 — зарегистрированные формы свойств FILE

FilePropertyFormEvidenceTests:12 новых случаев, пять точных BuiltinId/типов результатов через Parser/Binder и семь неверных аргументов с SemanticFailure/совпадающими местами диагностики обоих режимов. FILEPATH/FILEMODE возвращают STRING, FILEPOS/FILELENGTH — LONG, SEEKFILE — BOOLEAN. Совместно34/34 с закрытыми состояниями и APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_PROPERTY_FORMS_20261009.md. Снимок1254 сохраняет127 ограниченных форм из335. Далее ISOPEN/EOF/LINENUMBER/REWIND/FLUSH: прежние проверки, типы/неподходящие режимы и сохранность состояния. NUM26-CAND-001 открыт, goal активен.

## Session 1255 — неподходящий режим FILE

FileModeStateEvidenceTests:12 новых случаев обоих движков, FLUSH в INPUT, EOF/LINENUMBER в OUTPUT/APPEND и REWIND в APPEND. MB3007/FINALLY сохраняют ISOPEN TRUE/позицию и буфер Unicode; CLOSE даёт FALSE, точные байты и эксклюзивный дескриптор подтверждены. Совместно60/60 с чтением на границах буфера и OUTPUT/APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_MODE_STATE_20261009.md. Снимок1255:132 ограниченные формы из335,203 ещё не рассмотрены. Далее те же пять форм: закрытые состояния/точные статические типы и неверные аргументы, после сверки прежних доказательств. NUM26-CAND-001 открыт, goal активен.

## Session 1256 — закрытые состояния и формы FILE

Расширены FileClosedPropertiesEvidenceTests/FilePropertyFormEvidenceTests:26 новых случаев (16 закрытых/неоткрытых и10 статических). EOF/LINENUMBER/REWIND/FLUSH на закрытом FILE дают MB3007, FINALLY подтверждает ISOPEN FALSE; байты/дескриптор сохранены. Точные Id/типы пяти функций проверены Parser/Binder: LINENUMBER LONG, остальные BOOLEAN; неверный FILE даёт SemanticFailure одинакового места. Совместно70/70 с неподходящими режимами, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_STATE_FORMS_20261009.md. Снимок1256 сохраняет132 ограниченные формы из335. Далее READTOEND/READLINE/SKIPLINE: прежние пакетные/Unicode проверки и закрытые/неверные режимы, точные формы. NUM26-CAND-001 открыт, goal активен.

## Session 1257 — формы и отказы чтения FILE

Три существующие матрицы расширены READTOEND/READLINE/SKIPLINE:30 новых случаев,12 закрытых/неоткрытых,12 неподходящих OUTPUT/APPEND и6 статических. Точные BuiltinId/типы (STRING/STRING/BOOLEAN), неверный FILE, MB3007/FINALLY/позиция/буфер/байты и освобождение дескриптора подтверждены. Совместно142/142 с прежними INPUT Unicode/буферными/пакетными проверками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_READ_FORMS_20261009.md. Снимок1257:135 ограниченных форм из335,200 ещё не рассмотрены. Далее WRITE/WRITELINE/NEWLINE: прежние Unicode/перезапись и точные формы/неподходящие состояния. NUM26-CAND-001 открыт, goal активен.

## Session 1258 — формы и отказы записи FILE

Три матрицы расширены WRITE/WRITELINE/NEWLINE:26 новых случаев,12 закрытых/неоткрытых,6 INPUT и8 статических. Все три возвращают BOOLEAN; неверные FILE/текст отвергаются до выполнения. MB3007/FINALLY/позиция/байты/освобождение подтверждены. Совместно144/144 с LF, непарными суррогатами и OUTPUT/APPEND, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/FILE_WRITE_FORMS_20261009.md. Снимок1258:138 ограниченных форм из335,197 ещё не рассмотрены. Далее READALLLINES: прежние WholeFileDefaultEvidenceTests и границы Unicode/пустых строк/отсутствующего файла. NUM26-CAND-001 открыт, goal активен.

## Session 1259 — границы READALLLINES по буквальным байтам

ReadAllLinesBoundaryEvidenceTests:8 новых случаев обоих движков, пустой файл/LF/двойной CRLF/Unicode с NUL и одиночным CR. Выделенность/границы1 TO N, отсутствие лишней конечной строки, MB3007 отсутствующего файла с сохранением прежнего массива, байты/дескриптор подтверждены. Совместно10/10. Первый тест ошибочно использовал STR(BOOLEAN), затем ожидание одиночного CR как разделителя расходилось с ReadDecodedLines: функция делит по LF и удаляет конечный CR части. Тест фиксирует наблюдение; полный договор одиночного/конечного CR пока не установлен. Runtime не менялся. Отчёт docs/audit/READALLLINES_BOUNDARIES_20261009.md. Снимок1259:139 ограниченных форм из335. Далее установить договор одиночного/конечного CR по спецификации/READLINE и проверить явную форму/тип READALLLINES. NUM26-CAND-001 открыт, goal активен.

## Session 1260 — исправление CR в READALLLINES

Сверены историческая спецификация2 строки2179–2182 и ReadLineCore: строки не содержат переводов, потоковое чтение распознаёт CR/LF/CRLF. Подтверждён FILE26-004 P2: ReadDecodedLines сохранял внутренний CR и не разделял две пустые строки CR CR. Общий помощник заменён StringReader.ReadLine, конечный разделитель не создаёт лишнюю строку. Двенадцать новых/расширенных байтовых случаев обоих движков плюс WholeFileDefault —14/14. Пример samples/functions/readalllines/carriage_return.axm добавлен, но ещё требует запуска обоими движками; руководство/спецификацию/реестр дефектов нужно обновить до завершения исправления. Общий полный прогон после Runtime-правки также остаётся. NUM26-CAND-001 открыт, goal активен. Следующий шаг — закончить обязательную документацию и проверку примера FILE26-004, затем формы/явная кодировка READALLLINES.

## Session 1261 — документация и пример FILE26-004

Руководство READALLLINES дополнено прямым договором CR/LF/CRLF, пустых/конечных строк, отсутствующего файла и примером. carriage_return.axm автоматически запущен обоими движками в собственных каталогах, созданный файл удалён;16/16 совместно с байтовыми случаями. Факты основаны на договоре Axiom/READLINE; запасной жанровый ориентир Microsoft.VisualBasic.FileSystem.LineInput прочитан2026-10-09, книга Зельднера локально недоступна. Полная регрессия запущена: .artifacts/test-results/full-1261, процесс23642 ещё выполняется; не запускать второй MSBuild параллельно. FILE26-004 требует итог полной регрессии, затем считается исправленным в проверенном объёме. Далее точные формы/явная кодировка READALLLINES. NUM26-CAND-001 открыт, goal активен.

## Session 1263 — восстановление полной регрессии и открытая стабильность IDE

Общий процесс23642 завершился exit1: Execution3854/3854, IDE прерван после1213 успешных. С --blame-crash отдельный IDE повторно прерван после1180; Sequence указал два незавершённых теста (не доказательство причины). Тайловый класс с --blame-crash прерван после81; сам пример отдельно2/2. Без сборщика дампов с --diag тайловый класс82/82 и весь IDE1234/1234, exit0. Восемь Completed проектов исходного прогона плюс отдельный полный IDE дают7083/7083 проверок, девять TRX; это восстановленная регрессия, не успешная исходная общая команда. Журналы/Sequence сохранены в .artifacts/test-results/ide-*-1263.

FILE26-004 функционально исправлен: Runtime/руководство/спецификация/пример16/16 и полные наборы подтверждены. Открыт IDE26-CAND-001: непредвиденное завершение хост-процесса, причина не установлена, успешный повтор не закрывает стабильность. NUM26-CAND-001 тоже открыт. Следующий шаг — READALLLINES с явной кодировкой/точные формы; изучить журналы прерывания IDE без повторения зелёных наборов без нового основания. Goal активен.

## Session 1264 — UTF-16 и точные формы READALLLINES

ReadAllLinesEncodingFormEvidenceTests:6 новых случаев,4 исполнения UTF-16LE/BE обоих движков и2 статических точных формы. Буквальные байты с BOM/U+0D41/CR/CRLF/эмодзи/LF дают три строки, не принимают байт CR внутри символа за разделитель, сохраняют байты/дескриптор. Parser/Binder подтверждают ReadAllLines/ReadAllLinesEncoding и массив STRING. Совместно20/20 с границами/примером, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/READALLLINES_ENCODING_FORMS_20261009.md. Снимок1264:140 ограниченных форм из335,195 ещё не рассмотрены. Далее бинарные формы READBYTE/WRITEBYTE: прежние BinaryFileAlternativeEvidenceTests и числовые границы/режимы. NUM26-CAND-001 и IDE26-CAND-001 открыты, goal активен.

## Session 1265 — крайние LONG и существующие байтовые способы

Уточнение плана1264: READBYTE/WRITEBYTE не существуют в текущем языке/матрице, добавление новых сущностей не входит в аудит. Прочитаны BinaryFileAlternativeEvidenceTests/FileByteOverwriteEvidenceTests: байты сохраняются COPYFILE и однобайтовой явной WINDOWS-1251, SEEKFILE задаёт байтовую позицию и может повредить текст при перезаписи. SeekLongBoundaryEvidenceTests:4 новых случая MIN/MAX LONG обоих движков, MB3007/FINALLY/позиция2 послеА, следующий😀 до6, EOF FALSE и байты/дескриптор сохранены. Совместно32/32, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SEEK_LONG_BINARY_BOUNDARIES_20261009.md. Снимок1265 сохраняет140 ограниченных форм из335. Далее оставшиеся DIREXISTS/PATHEXISTS/MAKEDIR: прежние проверки неправильного вида объекта и сохранность Unicode-путей. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1266 — виды объектов и Unicode-каталоги

DirectoryObjectKindEvidenceTests:2 новых групповых случая обоих движков. Файл é😀 и каталог e+комбинируемый акцент😀 различаются; DIREXISTS/PATHEXISTS корректны, MAKEDIR поверх файла MB3007 сохраняет байты, вложенное создание автоматически создаёт родителей, повторный вызов успешен. Собственные каталоги .artifacts удалены по одному без рекурсивного удаления. Совместно13/13 с DirectoryAndTemp/PathNullCharacter, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_OBJECT_KINDS_20261009.md. Снимок1266:143 ограниченные формы из335,192 ещё не рассмотрены. Далее DELETEDIR/DELETEDIRRECURSIVE: ошибки непустого/неверного объекта и сохранность соседей, после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1267 — удаление каталогов и соседнее дерево

DirectoryDeleteBoundaryEvidenceTests:2 новых групповых случая обоих движков. На файле обе функции FALSE, DELETEDIR непустого дерева MB3007/FINALLY сохраняет файл; DELETEDIRRECURSIVE удаляет только собственное дерево, повтор/отсутствующий путь FALSE, канонически другой соседний Unicode-каталог сохраняет точные байты. Перед рекурсивным вызовом абсолютный target проверен внутри уникального root .artifacts. Очистка по одному файлу/каталогу. Совместно16/16 с FileRenameDeleteEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_DELETE_BOUNDARIES_20261009.md. Снимок1267:145 ограниченных форм из335,190 ещё не рассмотрены. Далее COPYDIR/MOVEDIR: прежние DirectoryTraversalFailureEvidenceTests, вложенность/неверный вид/сохранность. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1268 — COPYDIR/MOVEDIR и неправильный вид

Уточнены имена прежних доказательств: DirectoryFailureEvidenceTests и DirectoryTraversalContractTests (DirectoryTraversalFailureEvidenceTests отсутствует). DirectoryTransferKindEvidenceTests:12 новых случаев обоих движков, файл вместо источника/назначения, COPYDIR обычный/overwrite TRUE и MOVEDIR. MB3007/FINALLY сохраняют оба точных содержимых и число объектов на разных Unicode-путях. Совместно38/38 с вложенностью/частичными конфликтами/ссылками, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_TRANSFER_KINDS_20261009.md. Снимок1268:148 ограниченных форм из335,187 ещё не рассмотрены. Далее статические формы каталогов/неверные аргументы, затем оставшиеся DIRSIZE/CURRENTDIR/CHANGEDIR. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1269 — точные формы каталогов

DirectoryFormEvidenceTests:18 новых случаев,8 точных BuiltinId/BOOLEAN через Parser/Binder и10 неверных аргументов с SemanticFailure/совпадающими местами обоих режимов. Покрыты DIREXISTS/PATHEXISTS/MAKEDIR/DELETEDIR/DELETEDIRRECURSIVE/COPYDIR две формы/MOVEDIR. Совместно34/34 с тремя новыми матрицами исполнения каталогов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_FORMS_20261009.md. Снимок1269 сохраняет148 ограниченных форм из335. Далее DIRSIZE/CURRENTDIR/CHANGEDIR: прежние обходы/локальный текущий каталог и неверные объекты. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1270 — байтовый DIRSIZE и локальный текущий каталог

DirectorySizeCurrentEvidenceTests:2 новых групповых случая обоих движков, вложенные файлы4+3 байта дают DIRSIZE7/3/7; CHANGEDIR относительный/родитель работает, попытка смены на файл MB3007 сохраняет каталог программы, DIRSIZE файла MB3007. Environment.CurrentDirectory процесса неизменён, оба содержимых сохранены. Совместно14/14 с DirectoryAndTemp/DirectoryTraversalContract, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DIRECTORY_SIZE_CURRENT_20261009.md. Снимок1270:151 ограниченная форма из335,184 ещё не рассмотрены. Далее точные формы DIRSIZE/CURRENTDIR/CHANGEDIR и оставшиеся дисковые функции/пути. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1271 — типы размера и текущего каталога

DirectoryFormEvidenceTests дополнен5 случаями: точные DirSize LONG/CurrentDir STRING/ChangeDir BOOLEAN и неверные BOOLEAN-пути с SemanticFailure одинакового места обоих режимов. Совместно25/25 с исполнением размера/смены каталога, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Прочитан DriveInfoEvidenceTests: независимый Win32_LogicalDisk для размера/формата/типа, свободное место проверяется интервалом; ещё не запущен в этом шаге. Отчёт docs/audit/DIRECTORY_SIZE_FORMS_20261009.md. Снимок1271 сохраняет151 ограниченную форму из335. Далее шесть дисковых функций: независимый текущий реестр, точные типы/неверные аргументы и отсутствие созданного пути. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1272 — дисковые формы и независимый реестр Windows

DriveFormEvidenceTests:6 новых групповых случаев, точные BuiltinId/типы (размеры LONG, имя/формат/тип STRING) и BOOLEAN вместо пути отвергается SemanticFailure одинакового места обоих режимов. DriveInfoEvidenceTests реально выполнен на Windows: размер/формат/тип сверены с Win32_LogicalDisk, свободное место в0..Size, путь не создаётся. Совместно8/8, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DRIVE_FORMS_INVENTORY_20261009.md. Снимок1272:157 ограниченных форм из335,178 ещё не рассмотрены. Далее PATHROOT/COMBINEPATH/CHANGEPATHEXTENSION/RELATIVEPATH: прочитать прежние независимые пути и выбрать реальные пробелы Windows/Unicode. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1273 — SUBSTRCOUNT и неперекрывающийся Unicode

Уточнение плана1272: CHANGEPATHEXTENSION/RELATIVEPATH отсутствуют в текущем языке, существующие формы путей уже имеют ограниченные доказательства. SubStrCountUnicodeEvidenceTests:2 новые групповые проверки обоих движков, неперекрывающиеся UTF-16 совпадения, отдельные суррогаты/NUL, отсутствие нормализации и пустой фрагмент MB3007 с FINALLY. Совместно59/59 с StringSearchIndependentContractTests/StringSearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SUBSTRCOUNT_UNICODE_20261009.md. Снимок1273:158 ограниченных форм из335,177 ещё не рассмотрены. Далее реальные ARRAYCOUNT/ARRAYINDEXOF/ARRAYLASTINDEXOF/ARRAYCONTAINS: прежние доказательства и сравнение строк Unicode/чисел. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1274 — поиск Unicode и точных чисел в массивах

ArraySearchUnicodeNumericEvidenceTests:4 новые групповые проверки обоих движков. Целые UTF-16 значения отличают NUL/отдельный суррогат/эмодзи/составные акценты; видимый срез исключает внешнее совпадение, источник сохраняется. Соседние LONG выше2^53 и DECIMAL у максимума различаются в ARRAYCOUNT/ARRAYCONTAINS/ARRAYINDEXOF/ARRAYLASTINDEXOF. Первые три запуска числовой части отклонены из-за ошибочных форм теста (суффиксы L/D и строка в CLNG/CDEC); исправлены на подтверждённые литералы LONG и контекстные DECIMAL .0. Итог39/39 с ArraySearchContractTests/ArraySearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SEARCH_UNICODE_NUMERIC_20261009.md. Снимок1274:162 ограниченные формы из335,173 ещё не рассмотрены. Далее ARRAYFINDALL/ARRAYBINARYSEARCH: независимые координаты и Unicode/точные числа, после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

Уточнение счётчика по фактическому CSV1274:161 форма SCOPED_EVIDENCE и174 NOT_REVIEWED_IN_RESUMED_PASS, всего335. Числа162/173 выше ошибочны; предыдущий CSV1273 содержит157/178, а не158/177. При дальнейшем учёте использовать группировку CSV, а не перенос чисел из текста.

## Session 1275 — координаты и двоичный поиск Unicode

ArrayFindBinaryUnicodeEvidenceTests:4 новых групповых случая обоих движков. ARRAYBINARYSEARCH две формы на заранее упорядоченных UTF-16 строках с NUL/суррогатом/эмодзи/U+E000 возвращает буквальные логические индексы; соседние LONG выше2^53 различаются, первый дубликат найден, отсутствие даёт нижнюю границу минус1. ARRAYFINDALL возвращает координаты только видимого среза и целых строк, без нормализации/изменения источника. Совместно39/39 с ArraySearchContractTests/ArraySearchFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_FIND_BINARY_UNICODE_20261009.md. Снимок1275:164 ограниченных форм из335,171 ещё не рассмотрены (группировка фактического CSV). Далее ARRAYDISTINCT: прежние доказательства порядка/границ и Unicode/точных чисел. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1276 — уникальные значения Unicode в видимом срезе

ArrayDistinctUnicodeViewEvidenceTests:2 новые групповые проверки обоих движков. ARRAYDISTINCT видимого двумерного среза сохраняет порядок первых целых UTF-16 значений (суррогаты/NUL/эмодзи/пустая строка), исключает внешний элемент; результат1..5 независим от источника. Соседние DECIMAL у максимума различаются. Совместно27/27 с ArrayDistinctValueContractTests/ArrayCallbackOrderContractTests, ошибок/пропусков0. Первоначальные2 отказа были ошибкой ожидания форматирования DECIMAL: максимальные значения выводятся без .0; исправлено ожидание, не поведение. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_DISTINCT_UNICODE_VIEW_20261009.md. Снимок1276:165 ограниченных форм из335,170 ещё не рассмотрены. Далее ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE: прочитать прежние независимые границы/оси и выбрать пробел Unicode. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1277 — сортировка и обратный порядок Unicode

ArraySortReverseUnicodeEvidenceTests:4 новых групповых случая обоих движков, четыре функции с пропущенной/явной осью. Сортировка целых UTF-16 значений различает акценты/суррогат/эмодзи/U+E000; ARRAYTOSORTED сохраняет границы -2..2, ARRAYTOREVERSED даёт1..5. Копии сохраняют источник; ARRAYSORT/ARRAYREVERSE через срез меняют только видимые элементы. Первые два прогона имели ошибочные ожидания границ теста, исправлены по коду и прежнему договору. Итог119/119 с ArrayFillSortReverseContractTests/ArraySortBoundaryContractTests/ArrayTransformAxisContractTests/ArrayDefaultFormEvidenceTests/ArrayRemainingFormEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SORT_REVERSE_UNICODE_20261009.md. Снимок1277:173 ограниченных форм из335,162 ещё не рассмотрены. Далее ARRAYFILL и ARRAYCLONE: прежние типы/границы и сохранность Unicode/независимость копии. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1278 — заполнение и независимая копия Unicode

ArrayFillCloneUnicodeEvidenceTests:2 новые групповые проверки обоих движков. ARRAYFILL записывает суррогаты, разделённые NUL, только в видимый двумерный срез, сохраняет соседей. ARRAYCLONE сохраняет границы среза и создаёт независимую копию; заполнение копии не меняет владельца. Копия нераспределённого массива остаётся нераспределённой; ARRAYFILL такого источника MB3005 сохраняет состояние.31/31 с ArrayFillSortReverseContractTests/ArrayRemainingFormEvidenceTests; отдельно57/57 ArrayWindowFormEvidenceTests для точных форм/типов/неверных аргументов, суммарно88/88, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_FILL_CLONE_UNICODE_20261009.md. Снимок1278:175 ограниченных форм из335,160 ещё не рассмотрены. Далее метаданные LBOUND/UBOUND/ARRAYBOUNDS/ARRAYRANK/ARRAYLENGTH/ARRAYISALLOCATED: пустые и нераспределённые массивы, крайние индексы, точные результаты после чтения прежних проверок. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1279 — крайние границы и состояние массивов

ArrayMetadataBoundaryEvidenceTests:2 новые групповые проверки обоих движков. LBOUND/UBOUND обе формы точно возвращают крайние индексы -2147483647..-2147483646 и2147483646..2147483647. ARRAYBOUNDS создаёт независимый массив, изменение границы результата не меняет источник. Пустой выделенный массив сохраняет ранг2 и границы, длина0; нераспределённый даёт FALSE/0/2, ARRAYBOUNDS MB3005 сохраняет прежний результат. Совместно59/59 с ArrayWindowFormEvidenceTests для точных форм/типов/неверных аргументов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_METADATA_BOUNDARIES_20261009.md. Снимок1279:183 ограниченных форм из335,152 ещё не рассмотрены. Далее ARRAYSLICE/ARRAYTAKE/ARRAYSKIP: прежние доказательства и крайние count/start/Unicode/сохранность исходных элементов. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1280 — крайние размеры окон Unicode-массивов

ArrayWindowUnicodeCountEvidenceTests:4 новых групповых случая обоих движков с пропущенной/явной осью. ARRAYTAKE с максимальным count копирует только3 Unicode-элемента и не меняет источник; ARRAYSKIP даёт выделенный пустой1..0; ARRAYSLICE последнего INTEGER-индекса успешен, огромный диапазон MB3007 сохраняет прежний результат/источник. Совместно65/65 с ArrayWindowFormEvidenceTests/ArrayEmptyBoundaryContractTests (точные формы/типы/неверные аргументы и многомерные крайние пустые границы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_WINDOW_UNICODE_COUNTS_20261009.md. Снимок1280:189 ограниченных форм из335,146 ещё не рассмотрены. Далее ARRAYFLATTEN/ARRAYRESHAPE: видимые Unicode-элементы, порядок и независимость формы, после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1281 — форма и порядок Unicode-массивов

ArrayShapeUnicodeEvidenceTests:2 новые групповые проверки обоих движков. ARRAYFLATTEN читает только видимый срез в порядке последнего измерения; сохраняет суррогат с NUL/составной акцент/эмодзи/предварительно составленный символ, результат1..4. ARRAYRESHAPE использует границы видимого шаблона9..10/-2..-1, независим от источника и шаблона. Несовпадение4/8 элементов MB3007 сохраняет прежний результат. Совместно51/51 с ArrayShapeContractTests/ArrayRemainingFormEvidenceTests, включая точные формы/типы/пустые и нераспределённые случаи, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_SHAPE_UNICODE_20261009.md. Снимок1281:191 ограниченных форм из335,144 ещё не рассмотрены. Далее ARRAYCONCAT: прежние границы/совместимость, Unicode и точные числа. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1282 — объединение видимых Unicode-массивов

ArrayConcatUnicodeViewEvidenceTests:2 новые групповые проверки обоих движков. ARRAYCONCAT две формы объединяет целые Unicode-значения видимых срезов без внешних элементов; одномерный результат1..4, двумерный0..1/6..9. Изменение результата не меняет источник и повторно скопированный элемент. Совместно129/129 с ArrayConcatBoundaryContractTests/ArrayShapeContractTests/ArrayRemainingFormEvidenceTests/ArrayTransformAxisContractTests (точные формы/типы, оси, INTEGER-пределы/переполнение, пустые/нераспределённые/несовместимые аргументы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CONCAT_UNICODE_20261009.md. Снимок1282:193 ограниченных форм из335,142 ещё не рассмотрены. Далее ARRAYPUSH/ARRAYPOP/ARRAYINSERT/ARRAYREMOVE: прежние границы/пустые формы, сохранность Unicode при изменении/возврате. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1283 — добавление и удаление Unicode-элементов

ArrayMutationUnicodeEvidenceTests:4 новые групповые проверки обоих движков с пропущенной/явной осью. ARRAYPUSH/ARRAYINSERT сохраняют целые Unicode-значения и сдвигают элементы; ARRAYREMOVE/ARRAYPOP возвращают точные строки, восстанавливают исходные границы/значения. Неверный крайний индекс MB3007 сохраняет массив.113/113 с ArrayEmptyInsertContractTests/ArrayDefaultFormEvidenceTests/ArrayRemainingFormEvidenceTests/ArrayTransformAxisContractTests; отдельно22/22 ArrayDynamicShapeContractTests для многомерных осей/отказов/крайних границ, суммарно135/135, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_MUTATION_UNICODE_20261009.md. Снимок1283:201 ограниченных форм из335,134 ещё не рассмотрены. Далее ARRAYEVERY/ARRAYSOME/ARRAYFILTER/ARRAYMAP/ARRAYREDUCE: прежние порядок/пустые случаи и Unicode/точные числа в пользовательских функциях. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1284 — Unicode в пользовательских функциях обхода

ArrayCallbackUnicodeEvidenceTests:2 новые групповые проверки обоих движков. Целые строки с суррогатом/NUL/разными акцентами/эмодзи передаются пользовательским функциям только из видимого среза. ARRAYEVERY/ARRAYSOME вызывают предикат2/1 раз, ARRAYFILTER/ARRAYMAP/ARRAYREDUCE4 раза; результат и строка накопления точны, источники независимы.23/23 с ArrayCallbackOrderContractTests (точные формы/типы, подписи, порядок/ошибки/FINALLY); отдельно12/12 ArrayCallbackBoundaryContractTests (пустые массивы/максимальный индекс), суммарно35/35, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CALLBACK_UNICODE_20261009.md. Снимок1284:206 ограниченных форм из335,129 ещё не рассмотрены. Далее числовые пользовательские функции LONG/DECIMAL для ARRAYMAP/ARRAYREDUCE, затем оставшиеся реальные формы из CSV. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1285 — точные числа в пользовательских функциях массивов

ArrayCallbackExactNumericEvidenceTests:2 новые групповые проверки обоих движков. ARRAYMAP передаёт соседние LONG выше2^53 и соседние DECIMAL у максимума точно; ARRAYREDUCE даёт точный LONG18014398509481985 и DECIMAL-1. Переполнение первого вызова пользовательской функции MB3008 прекращает обход после1 вызова, сохраняет прежний результат/источник. Совместно37/37 с Unicode/порядком/границами пользовательских функций, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/ARRAY_CALLBACK_EXACT_NUMERIC_20261009.md. Снимок1285 сохраняет206 ограниченных форм из335,129 ещё не рассмотрены. Далее реальные REGEXCOUNT/REGEXMATCH и связанные формы: прежние независимые доказательства, Unicode UTF-16 и недопустимые шаблоны. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1287 — подтверждённый результат REGEXCOUNT

Завершён именно процесс17948:201/201 за3 минуты, exit0, ошибок/пропусков0; реальные тайм-ауты всех18 форм и диагностическая сверка прошли, включая новые2 Unicode-случая REGEXCOUNT. RegexCountFormEvidenceTests добавляет2 статических случая точных RegexCount/RegexCountModes INTEGER; отдельный прогон4/4 с Unicode, без повторения трёхминутной группы. Итоговый отчёт дополнен docs/audit/REGEX_COUNT_MALFORMED_UNICODE_20261009.md. Дефект не найден, Runtime/поведение не менялись. Снимок1287:208 ограниченных форм из335,127 ещё не рассмотрены. Далее REGEXISMATCH/REGEXMATCH: точные формы и возврат отдельных UTF-16 совпадений/NUL/режимы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1288 — точные формы и совпадения Unicode

RegexMatchUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/BOOLEAN/STRING через Parser/Binder и2 исполнения обоих движков). REGEXMATCH возвращает точный суррогат/NUL, REGEXISMATCH не исправляет разорванную пару и не нормализует акценты, включая режимы. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0. Прежние обычные результаты/неверные аргументы/пустые/режимы/реальные тайм-ауты подтверждены прогоном201/201 Session1287 и без изменения Runtime не повторены. Дефект не найден, поведение не менялось. Отчёт docs/audit/REGEX_MATCH_UNICODE_20261009.md. Снимок1288:212 ограниченных форм из335,123 ещё не рассмотрены. Далее REGEXEXTRACT/REGEXEXTRACTALL: точные формы/группы и целые UTF-16 значения. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1289 — извлечение Unicode-групп

RegexExtractUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/STRING/одномерный STRING-массив и2 исполнения обоих движков). Группы возвращают точные суррогаты/NUL; явная группа10 возвращает целую пару, отсутствующая1 пуста; REGEXEXTRACTALL сохраняет порядок и создаёт выделенный пустой результат без нормализации акцента. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты уже подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_EXTRACT_UNICODE_20261009.md. Снимок1289:216 ограниченных форм из335,119 ещё не рассмотрены. Далее REGEXMATCHES/REGEXSPLIT: точные массивные формы, нулевые совпадения и UTF-16 границы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1290 — массивы совпадений и UTF-16-разделители

RegexArrayUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/одномерный STRING-массив и2 исполнения обоих движков). REGEXMATCHES возвращает суррогаты/NUL и3 нулевых совпадения эмодзи; REGEXSPLIT пустым шаблоном делит между UTF-16 единицами, захваченные разделители сохраняются, включая NUL/суррогаты. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_ARRAY_UNICODE_20261009.md. Снимок1290:220 ограниченных форм из335,115 ещё не рассмотрены. Далее четыре формы REGEXREPLACE: точное связывание, Unicode/замены/числовой count. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1291 — замены Unicode и крайние count

RegexReplaceUnicodeFormEvidenceTests:6 новых случаев (4 точных BuiltinId/STRING и2 исполнения обоих движков). Четыре формы сохраняют отдельные UTF-16 единицы/NUL, ссылки на группы, count1/MAX и нулевые совпадения; замена не нормализует акценты, count0 сохраняет строку без проверки шаблона/режимов по прежнему договору. Совместно7/7 с RegexDiagnosticParityProbeTests, ошибок/пропусков0; прежние ошибки/тайм-ауты подтверждены201/201 Session1287. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/REGEX_REPLACE_UNICODE_20261009.md. Снимок1291:224 ограниченных форм из335,111 ещё не рассмотрены. Все18 REGEX-форм теперь имеют ограниченные доказательства, не полное закрытие. Далее даты/числа DATESERIAL/TIMESERIAL/ISLEAPYEAR/DAYSINMONTH: прежние независимые календарные проверки и границы. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1292 — крайние аргументы календарных функций

CalendarExtremeArgumentEvidenceTests:36 новых случаев (крайние MIN/MAX INTEGER каждого9 аргументных положений четырёх функций, оба движка). DATESERIAL/TIMESERIAL/ISLEAPYEAR/DAYSINMONTH дают MB3007 и выполняют FINALLY без нормализации недопустимых календарных чисел. Совместно172/172 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests (точные формы/типы, григорианские правила1900/2000/2100 и допустимые пределы), ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CALENDAR_EXTREME_ARGUMENTS_20261009.md. Снимок1292:228 ограниченных форм из335,107 ещё не рассмотрены. Далее DATEADD/DATEDIFF/DATEPART три типа: прочитать прежние доказательства и выбрать числовой пробел полных диапазонов/интервалов. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1293 — полный диапазон арифметики дат

DateArithmeticRangeEvidenceTests:2 новые групповые проверки обоих движков. DATEDIFF полного DATETIME-диапазона даёт точные LONG±315537897599 секунд, DATE3652058 дней, yyyy9998; DATEPART крайних даты/времени точен. DATEADD крайних INTEGER в DATE даёт MB3007 и сохраняет результат; TIME оборачивается по суткам в03:14:07/20:45:52. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для9 точных форм/типов и прежних границ, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/DATE_ARITHMETIC_FULL_RANGE_20261009.md. Снимок1293:237 ограниченных форм из335,98 ещё не рассмотрены. Далее DATEVALUE/TIMEVALUE/ISDATE: Unicode/встроенный NUL/неверные форматы и сохранность результата, после чтения прежнего разбора. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1294 — Unicode в строках даты и времени

TemporalTextUnicodeEvidenceTests:14 новых случаев обоих движков (7 строк). DATEVALUE/TIMEVALUE отвергают NUL с последующим текстом, эмодзи/суррогат, полноширинные/арабские цифры и Unicode-минус с MB3007, сохраняя прежние DATE/TIME; ISDATE FALSE. Совместно150/150 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для точных форм/типов и канонических форматов, ошибок/пропусков0. Дефект не найден в проверенном объёме, Runtime/поведение не менялись. Отчёт docs/audit/TEMPORAL_TEXT_UNICODE_20261009.md. Снимок1294:240 ограниченных форм из335,95 ещё не рассмотрены. Далее отдельный конечный NUL/пробелы и договор точного разбора перед оставшимися частями даты. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1295 — конечный NUL и пробелы временных строк

TemporalTextUnicodeEvidenceTests дополнен14 случаями: конечный NUL после DATE/TIME/DATETIME, начальный/конечный ASCII-пробел и NBSP. Все отвергаются ISDATE FALSE и DATEVALUE/TIMEVALUE MB3007 с сохранением прежних значений обоих движков.28/28, ошибок/пропусков0; новый дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/TEMPORAL_TEXT_TAILS_20261009.md. Снимок1295 сохраняет240 ограниченных форм из335,95 ещё не рассмотрены. Далее YEAR/MONTH/DAY/HOUR/MINUTE/SECOND/WEEKDAY/MONTHNAME/WEEKDAYNAME и STARTOF/ENDOF: реальные границы и канонические имена после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1296 — части даты и крайние периоды

CalendarPartsBoundaryEvidenceTests:2 новые групповые проверки обоих движков при tr-TR с восстановлением культуры. Части DATE/TIME точны на крайних значениях, WEEKDAY0001-01-01=1 и9999-12-31=5; имена January/December/Monday/Sunday канонические, недопустимые крайние числа MB3007. STARTOF/ENDOF крайних месяцев/лет не выходят за диапазон. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для13 точных форм/типов, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CALENDAR_PARTS_BOUNDARIES_20261009.md. Снимок1296:253 ограниченных форм из335,82 ещё не рассмотрены. Далее DATE/TIME/NOW/UTC/TIMER: фиксированный независимый источник времени, границы суток и явные зоны после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1297 — фиксированная полночь и TIMER

CurrentTimeMidnightEvidenceTests:2 новые групповые проверки обоих движков на фиксированном2000-03-01 00:00:00.125 +03:00. Локальные DATE/TIME/NOW и UTC-варианты/явнаяUTC различают соседние даты, строковое время до секунд, TIMER0.125 точно. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для10 точных форм/типов, ошибок/пропусков0. Первый build отклонён из-за неполной реализации IRuntimeClock в новом тесте, исправлен только тест. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/CURRENT_TIME_MIDNIGHT_20261009.md. Снимок1297:263 ограниченных форм из335,72 ещё не рассмотрены. Далее UTCOFFSET/TOUTC/FROMUTC: точные формы, явные зоны/крайний DATETIME и Unicode-идентификаторы после чтения прежних доказательств. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1298 — часовые пояса у пределов DATETIME

TimeZoneUnicodeBoundaryEvidenceTests:2 новые групповые проверки обоих движков с ControlledClock+03/UTC. UTCOFFSET точен, UTC сохраняет крайние DATETIME; TOUTC минимального/FROMUTC максимального в+03 дают MB3007 и сохраняют результат. Неверные Unicode/NUL имена зон отвергнуты контролируемым resolver, не выдаётся за проверку системного resolver. Совместно138/138 с DateTimeIndependentContractTests/DateTimeFormEvidenceTests для7 точных форм/типов и прежних реальных DST случаев America/New_York, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/TIME_ZONE_UNICODE_BOUNDARIES_20261009.md. Снимок1298:270 ограниченных форм из335,65 ещё не рассмотрены. Далее системный resolver Unicode/NUL зон и оставшиеся экранные формы из CSV. NUM26-CAND-001/IDE26-CAND-001 открыты, goal активен.

## Session 1299 — системные зоны Unicode и точка остановки

SystemTimeZoneUnicodeEvidenceTests:24 новых случая обоих движков,4 неверных имени зон (конечный NUL/эмодзи/одиночный суррогат/составной акцент) для UTCOFFSET/TOUTC/FROMUTC через реальный системный resolver Windows без подмены часов. MB3007/FINALLY сохраняют DATETIME. Совместно26/26 с TimeZoneUnicodeBoundaryEvidenceTests, ошибок/пропусков0. Дефект не найден, Runtime/поведение не менялись. Отчёт docs/audit/SYSTEM_TIME_ZONE_UNICODE_20261009.md. Снимок1299:270 ограниченных форм из335,65 ещё не рассмотрены. По просьбе пользователя аудит останавливается после обновления документов, коммита и push; goal будет приостановлен. Следующий шаг нового запуска: INKEY/KEYDOWN/KEYPRESS и мышь/экранные формы после чтения прежних независимых доказательств. NUM26-CAND-001/IDE26-CAND-001 остаются открытыми.


## Session 1301 — клавиатура и UTF-16

INKEY/KEYDOWN/KEYPRESS проверены обоими движками через независимый поставщик, без перехода на интерпретатор:целые эмодзи/NUL/суррогаты, сохранение очереди, отсутствие поставщика, точные формы/типы и неверные аргументы. Совместный набор20/20; отчёт docs/audit/KEYBOARD_UNICODE_20261009.md. Реализация/руководство/примеры не менялись, быстродействие и реальные события ОС не проверялись.


## Session 1302 — границы мыши и состояния экрана

MOUSEX/MOUSEY/MOUSEBUTTON/MOUSEPRESS и SCREENISACTIVE/SCREENMODE/SCREENWIDTH/SCREENHEIGHT проверены обоими движками без перехода на интерпретатор,точные типы и формы подтверждены. Итог19/19; отчёт docs/audit/MOUSE_SCREEN_STATE_20261009.md. Только заданные поставщики,не реальные устройства или быстродействие. Реализация/руководство/примеры не менялись.


## Session 1303 — графические запросы

TEXTWIDTH/TEXTHEIGHT, POINT обеих форм и PMAP: UTF-16 метрики, пределы селекторов/тождественных координат, контекст/FINALLY и точные типы проверены обоими движками без перехода. Итоги19/19 и Runtime12/12; отчёт docs/audit/GRAPHICS_QUERY_BOUNDARIES_20261009.md. Реализация/руководство/примеры не менялись; быстродействие не измерялось.


## Session 1304 — базовые строки и RGB

LEN/LEFT/RIGHT/MID/CONTAINS/STARTSWITH/ENDSWITH/RGB: точные формы/типы, отрицательные аргументы и независимые граничные результаты подтверждены93/93. Новые2 случая проверяют оба движка без перехода, UTF-16/NUL и крайние счётчики. Отчёт docs/audit/LEGACY_STRING_RGB_20261009.md. Реализация/руководство/примеры не менялись, измерений быстродействия нет.


## Session 1305 — поштучная сверка ресурсов

41 форма IMAGE/SPRITE/TILESET/TILEMAP сверена с отдельными доказательствами обоих движков; текущий IDE252/252 и Runtime1079/1079. Реестр RESOURCE_FORM_RECONCILIATION_20261009.csv сохраняет предел каждой строки. Снимок1305 содержит335 ограниченных форм с точным совпадением исходной матрицы; это не полное доказательство семантики. Реализация/руководство/примеры не менялись.

## Session 1308 — примеры POW и границ PRINT

AuditContinuationSampleTests проверяет два запускаемых примера обоими движками: 4/4, без перехода BytecodeVm на интерпретатор. console_line_boundaries.axm закрепляет завершённые, пустые и соединённые строки; rounding_boundary.axm показывает выбранную границу DOUBLE без обещания точного округления середины. Руководство PRINT/POW синхронизировано. Реализация BytecodeVm и набор токенов не менялись; исправлен только CLI-получатель завершённых строк. Ограничение POW сохраняется.

## Session 1311 — соответствие POW готовой функции .NET

PowDotNetContractTests проверяет оба движка без перехода на интерпретатор: конечные результаты побитово совпадают с Math.Pow в текущей среде, NaN даёт MB3007, бесконечность даёт MB3008. Включены середина субнормальной границы, уход к нулю, знак нуля, дробная степень, максимальный DOUBLE и недопустимый домен. Пример rounding_boundary.axm и руководство уточняют принятый договор. Новых токенов, архитектуры и сценариев быстродействия нет.

## Session 1316 — AXD-020a: функция без RETURN приведена к единому договору MB3006

По итогам переноса проверок Runtime на VM (этап 1 плана отказа от интерпретатора) закрыт дефект AXD-020a: программа с FUNCTION, чья выполнившаяся ветвь завершается без RETURN со значением, больше не отклоняется на этапе валидации модуля.

- VM: высотная проверка модуля (BC4054) больше не отвергает пустой стек у Return в невойд-функции — сценарий легален и приводит к рантайм-диагностике MB3006 «Функция 'X' завершилась без RETURN со значением.»; типовой контроль стека допускал пустой стек и ранее. Остальные несовпадения высот (например, VOID-процедура с лишним значением) по-прежнему отвергаются.
- Текст MB3006 в BytecodeVm выровнен с Interpreter (единая формулировка).
- Тесты: перенесённый LanguageContractTests.Execute_ReportsMissingReturnValue (адресный набор 86/86) и обновлённый BytecodeVmTests.Execute_CompiledReportsMissingReturnValue (компиляция успешна, ошибка приходит в рантайме, тело функции выполняется). Валидаторные ожидания Bytecode.Tests 242/242 не менялись.
- Независимый пример: samples/acceptance/negative/function_branch_without_return_value.axm.
- Артефакты: .artifacts/interpreter-removal/stage1b (TRX/логи); сводка — STAGE1B_NOTES.md.

## Session 1318 — миграция проверок Runtime завершена; MB3005 и ARRAYPUSH приведены к единому договору

Перенос 597 проверок `InterpreterTests` на `BytecodeVm` завершён (596 активных + 1 отключённая `#if false`); старый класс `InterpreterTests` удалён. По ходу закрыты два расхождения:

- AXD-021: текст MB3005 для невыделенного массива в `BytecodeVm` приведён к договору Interpreter — «Функция {F}: аргумент '{arg}' содержит массив, который ещё не выделен.» (имена `array`/`source`/`template`/`value`/`left`/`right`).
- AXD-022: `ARRAYPUSH` при расширении у верхней границы INTEGER больше не выбрасывает CLR `OverflowException` — предварительная проверка даёт MB3007 (как Interpreter); независимый пример `samples/acceptance/negative/array_push_upper_bound_overflow.axm`.

Тесты: `LanguageContractTests` 681/681; полный `Axiom.Runtime.Tests` 1079/1079; решение 7387/7387 (артефакты `.artifacts/interpreter-removal/stage1e|stage1f`). Валидатор и прочие наборы не менялись.


## Покрытие после отказа от интерпретатора

Единственный исполнитель — `BytecodeVm`. Прежний столбец паритета хранит историческое доказательство переноса, а не действующую проверку второго движка. Текущие тесты проверяют ожидаемый вывод, типы, коды и места диагностики, состояние и побочные эффекты VM.

Все 597 записей исходного реестра Runtime сопоставлены с `LanguageContractTests.*`; 596 активных деклараций перенесены, одна исходно отключённая остаётся отключённой. В 594 записях последовательность строк с `Assert` совпала; в оставшихся трёх отличия относятся к вынесенным проверкам парсера/связывателя и границе общего вспомогательного кода. Проверки результата сохранены. Общий `RuntimeVmContractRunner` дополнительно проверяет понижение и компиляцию.

Перенос: `docs/plans/INTERPRETER_REMOVAL_PROGRESS.csv` и `INTERPRETER_REMOVAL_EXECUTION_PROGRESS.csv`. Новый пример: `samples/smoke/bytecode-only/compiler_execution.axm`; независимая проверка вывода — `BytecodeOnlyContractTests.CompilerExecutionSampleHasExpectedOutput`. Отсутствие старых типов/API проверяют `BytecodeOnlyArchitectureTests` и `BytecodeOnlyContractTests`. Итоговые числа и проверки UI: `docs/plans/INTERPRETER_REMOVAL_RESULT.md`.


## Session 1325 - 2026-10-10 00:37 - Завершение отказа от интерпретатора

Переход завершён: 6032/6032, девять проектов. Сопоставление с интерпретатором теперь историческое; текущие проверки используют самостоятельные ожидания VM. Итоговая таблица 158 файлов — docs/plans/INTERPRETER_REMOVAL_EXECUTION_FINAL.csv. Реестр 335 форм согласован с актуальными CLI metadata и обеими подсветками.

Добавлены и проверены samples/smoke/bytecode-only/compiler_execution.axm и samples/functions/arraypush/upper_bound_overflow.axm. Второй подтверждает MB3007 и сохранность массива после отказа расширения при предельной верхней границе; BytecodeOnlyContractTests проверяет тот же договор независимо. Обновлён русскоязычный samples/smoke/debug_step_controls.axm; шаги и повтор с точкой подтверждены в настоящей IDE. Это не доказательство каждой перегрузки; границы указаны в итоговой сводке.

## Session 1327 - 2026-10-10 08:22 - INPUT и отдельный процесс IDE

Отмена во время ожидания INPUT проверяется до преобразования/присваивания: отдельный Runtime-тест и настоящий DesktopHost подтверждают Cancelled без ложной MB3003. Прежний тест неверного подтверждённого ввода остаётся зелёным. Ручная проверка — samples/operators/input/cancel_wait.axm, описание — manual/input.html. Новых токенов нет; словари подсветки неизменны. Процессные проверки DesktopHost дополнительно проверяют compiler_execution.axm, Stop цикла, ошибки компиляции и пошаговую отладку; графическая приёмка Electron ещё не выполнена. Новых измерений производительности нет.


## Session 1343 - 2026-10-10 19:27 - Алфавит имён на входе компилятора

Не-ASCII символы в пользовательских именах отклоняются до исполнения с MB0001. IdentifierAlphabetTests проверяет переменные, константы, параметры, типы и выражения интерполяции; русский текст строк и комментариев компилируется. Приёмка: samples/smoke/identifiers/latin_names.axm и russian_name_rejected.axm. Договор отражён в manual и обеих схемах подсветки; команды VM и измерения производительности не менялись.

## Дополнение - 2026-10-10 - Графическая диагностика до генерации байткода

Заведомо неверный графический контекст теперь отклоняется компилятором с MB2046. Это изменяет стадию приёмки отрицательных примеров SCREEN 0 перед CIRCLE/COLOR/PAINT/PCOPY и графических операций без выбора подходящей поверхности. MB3010 сохраняется в BytecodeVm для состояния, неизвестного при компиляции; отдельные проверки защиты самой VM связывают программу без предварительного анализа графического режима. Примеры обновлены; полный пофайловый прогон Electron ещё выполняется. Добавлен samples/operators/dim/unused_value_warning.axm для предупреждения MB2047; исполнение языка и покрытие команд байткода не изменены.

Продолжение 2026-10-10: все 768 samples выполнены через Run Electron с отдельными повторами ожиданий. Найденные проблемы относятся к оболочке и передаче ввода, а не к расширению инструкций BytecodeVm. Консольные INKEY/KEYDOWN/KEYPRESS/ON KEY/SLEEP и мышиные события CANVAS теперь проходят через реальный InputStateService. Пофайловая таблица: docs/SAMPLES_ELECTRON_AUDIT.md. После исправления видимости native SCREEN выполняется заключительный повтор. Перечень сценариев производительности не изменён; большее время ожидания двух существующих сценариев не является новым измерением производительности.

Заключительный повтор 2026-10-10: все 768 файлов проверены и запущены через Electron; 656 успешных программ и 112 предусмотренных отрицательных результатов. Набор Electron 59/59; дополнительный набор .NET 6112/6112. Команды BytecodeVm не расширялись. SCREEN сохраняет нативное окно DesktopHost, CANVAS отображается в Electron. Граница визуальной и физической проверки SCREEN изложена в docs/SAMPLES_ELECTRON_AUDIT.md.

## Session 1358 - Диапазон и представление MB2047

Анализ неиспользуемого значения указывает на имя DIM/CONST, не меняя диапазон исполнения всего оператора. Electron представляет MB2047 как совет, а не ошибку или жёлтое предупреждение. Пример samples/operators/dim/unused_value_warning.axm и manual/diagnostics.html актуализированы; проверка Electron запускает этот пример и подтверждает исчезновение совета после PRINT spare. Присваивание без чтения по-прежнему оставляет совет. Семантические проверки диапазонов успешны. Покрытие инструкций BytecodeVm, паритет выполнения и измерения производительности не изменены.


## 2026-10-11 — Полнота пользовательского справочника

Справочник docs/manual/ сверён с действующим реестром: все 271 встроенные функции и 335 форм вызова имеют собственные карточки и ссылки на тематические описания и запускаемые примеры. Каталог операторов включает 67 операторов, объявлений и форм инструкций; дополнительные каталоги охватывают все 95 ключевых слов, 14 типов и 24 операции выражений. Сверка выполняется scripts/check-manual.mjs с выгрузкой Axiom.LanguageAudit. Это уточнение охвата документации: оно не изменяет покрытие BytecodeVm, наличие проверок равенства движков или статус измерений производительности.
