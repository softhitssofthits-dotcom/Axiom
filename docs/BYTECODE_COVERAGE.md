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
| Циклы: `FOR/NEXT`, `WHILE`, `DO/LOOP`, `EXIT FOR`, `EXIT DO` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_CompiledForLoopReportsStepZero()`, `Run_WithBytecodeMode_ReportsStepZeroAsRuntimeFailure()`, `Run_ReturnsSemanticFailureForExitForOutsideFor()`, `Run_ReturnsSemanticFailureForExitDoOutsideDo()`, acceptance `samples/acceptance/negative/for_step_zero.axm`; после отложенной очистки `BytecodeEvaluationStack` и прямых специализированных comparison-jump путей адресный benchmark `samples/smoke/engine-compare/control_flow_branch_maze.axm` на длинном прогоне держится около `0.826x` runtime / `0.814x` total |
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
| `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_DateTimeArithmeticAndValidationBuiltins()`, `Execute_DateTimeArithmeticRejectsUnsupportedInterval()`, `Execute_TimeValueRejectsNonCanonicalInput()`, `Execute_CompiledDateTimePartsAndArithmeticBuiltins()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`; manual: `docs/manual/date-time-functions-arithmetic-and-validation.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/dateadd|datediff|datepart|datevalue|timevalue|isdate/`; `ISDATE` переведён на прямой `UnaryNoLocation`-путь |
| `UTCOFFSET`, `TOUTC`, `FROMUTC`, `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` | да | да | да | да | да | да | горячо пройдено | Тесты: `Execute_DateTimeUtcBuiltins()`, `Execute_DateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_DateTimeUtcBuiltinsSupportSeasonalNamedTimeZones()`, `Execute_DateTimeCalendarHelpers()`, `Execute_CompiledDateTimeCurrentBuiltins()`, `Execute_CompiledDateTimeUtcBuiltinsSupportNamedTimeZones()`, `Execute_CompiledDateTimeBuiltinsReportArgumentErrors()`, `Run_WithBytecodeMode_UsesBytecodeVmForCurrentDateTimeBuiltins()`, `Run_WithBytecodeMode_UsesBytecodeVmForDateTimePartsAndArithmeticBuiltins()`, `Run_WithBytecodeMode_ReportsDateTimeBuiltinArgumentError()`; manual: `docs/manual/date-time-functions-time-zones.html`, `docs/manual/date-time-functions-parts-and-serial.html`, `docs/manual/date-time-functions.html`; sample: `samples/functions/utcoffset|toutc|fromutc|isleapyear|daysinmonth|startofmonth|endofmonth|startofyear|endofyear/`; `samples/smoke/engine-compare/date_time_calendar_pipeline.axm` держит около `0.651x` runtime / `0.656x` total |

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
