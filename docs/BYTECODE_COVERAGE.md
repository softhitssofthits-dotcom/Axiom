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
| Арифметика и логика выражений: `+`, `-`, `*`, `/`, `\`, `MOD`, `^`, `AND`, `OR`, `XOR`, `NOT`, `EQV`, `IMP`, `<<`, `>>`, сравнения | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_ReportsDivisionByZero()`, `Execute_ReportsDivisionByZeroForIntegralOperators(...)`, `Execute_ReportsIntegerArithmeticOverflow()`, `Execute_ReportsLongArithmeticOverflow()`, `Execute_ReportsDoubleArithmeticOverflow()`, `Execute_CompiledProgramReportsDivisionByZeroWithSourceLocation()`, `Execute_CompiledIntegralDivisionReportsDivisionByZero()`, `Run_ReturnsRuntimeFailure()`, `Run_ReturnsRuntimeFailureForNumericOverflow(...)`, `Run_ReturnsSemanticFailureForBooleanOrderedComparison()`; acceptance: `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)`; широкий адресный benchmark `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` после вынесения `Divide` в отдельный быстрый путь держится около `0.853x` runtime / `0.860x` total, а отдельный bitwise/shift benchmark `samples/smoke/engine-compare/bitwise_shift_mesh.axm` держится около `0.968x` runtime / `0.968x` total для `AND`, `OR`, `XOR`, `NOT`, `<<` и `>>`, то есть побитовый подслой уже быстрее интерпретатора, но пока только в рабочем паритете |
| Объявления и присваивание: `DIM`, `CONST`, `=` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Run_ReturnsSemanticFailureAndDoesNotExecute()`, acceptance-ошибки `samples/acceptance/negative/undeclared_variable.axm`, `samples/acceptance/negative/dim_requires_type_or_initializer.axm`, а также `samples/operators/const/fixed_string_error.axm` с проверкой `MB2026`; адресный benchmark `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` теперь отдельно нагружает локальные и глобальные присваивания и после вынесения `Divide` в отдельный быстрый путь держится около `0.853x` runtime / `0.860x` total |
| Массивные объявления и изменение формы: `REDIM`, `ERASE`, индексирование, частичное индексирование | да | да | да | да | да | да | горячо пройдено | Негативные опоры: `Execute_UnallocatedDynamicArrayAccessReportsRuntimeState()`, `Execute_RedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_EraseOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_RedimRejectsNonCanonicalEmptyRange()`, `Execute_DimRejectsMinimumIntegerLowerBound()`, `Execute_RedimRejectsMinimumIntegerLowerBound()`, `Execute_CompiledRedimOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_CompiledEraseOnArrayViewReportsBuiltinArgumentDiagnostic()`, `Execute_CompiledUnallocatedArrayAccessReportsRuntimeState()`; 1D-путь `LoadElement`/`StoreElement` облегчен так, чтобы успешный доступ к элементу не тянул `ResolveLocation` и медленную диагностику, после шага `mixed` держится около `0.653x` runtime / `0.818x` total |
| Вывод: `PRINT` | да | да | да | да | да | да | горячо пройдено, runtime в целевом диапазоне | Негативные опоры через выражения и runtime-цепочки: `Execute_NestedRuntimeErrorReportsCallChain()`, `Run_ReturnsRuntimeFailure()`, acceptance-паритет `NegativeRuntimeAcceptanceProgram_HasStableDiagnosticParityBetweenBytecodeAndInterpreter(...)`; отдельные sample по формам `PRINT` есть, а после прямой записи скалярных значений в `_pendingOutput` и удешевления `PRINT USING` адресный benchmark `samples/smoke/engine-compare/print_output_gallery.axm` держится около `0.884x` runtime / `0.918x` total |
| Форматированный вывод: `PRINT USING` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_PrintUsingReportsStringFieldTypeMismatch()`, `Execute_PrintUsingReportsNumericFieldTypeMismatch()`, `Execute_CompiledPrintUsingReportsFieldTypeMismatch()`, `Run_WithBytecodeMode_ReportsPrintUsingRuntimeFailureFromVm()`; после общих ускорений VM и длинного контрольного прогона `print_output_gallery.axm` (`20` итераций, `3` прогрева) слой вывода держится около `0.860x` runtime / `0.877x` total |
| Ввод: `INPUT` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_ReportsInvalidInputConversion()`, `Execute_ReportsInvalidMultiInputHandling()`, `Execute_CompiledInputReportsConversionFailure()`, `Run_WithBytecodeMode_ReportsInputRuntimeFailureFromVm()`; после быстрого пути для `INPUT` на 1/2/3 целей без промежуточного массива результатов и длинного прогона `input_record_intake.axm` (`40` итераций, `5` прогревов) слой держится около `0.879x` runtime / `0.889x` total |
| Файловые операторы: `OPEN`, `CLOSE` | да | да | да | да | да | да | не приоритет | Негативные опоры: `Execute_CloseReportsClosedFile()`, `Execute_OpenReportsAlreadyOpenFileVariable()`, `Execute_CompiledOpenFileReportsClosedFileRead()`, `Run_WithBytecodeMode_ReportsOpenTextFileRuntimeFailureFromVm()` |
| Ветвление: `IF ... ELSE ... END IF`, `SELECT CASE` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `samples/acceptance/negative/if_requires_boolean.axm`, `Run_ReturnsSemanticFailureForSelectCaseRangeOnBoolean()`, общая semantic-проверка `Run_ReturnsSemanticFailureAndDoesNotExecute()`; после отложенной очистки `BytecodeEvaluationStack` и перевода `JumpIfEqual`/`JumpIfLess`/`JumpIfGreater...` на специализированные прямые пути адресный benchmark `samples/smoke/engine-compare/control_flow_branch_maze.axm` на длинном прогоне держится около `0.826x` runtime / `0.814x` total |
| Циклы: `FOR/NEXT`, `WHILE`, `DO/LOOP`, `EXIT FOR`, `EXIT DO` | да | да | да | да | да | да | горячо пройдено, целевой диапазон | Негативные опоры: `Execute_CompiledForLoopReportsStepZero()`, `Run_WithBytecodeMode_ReportsStepZeroAsRuntimeFailure()`, `Run_ReturnsSemanticFailureForExitForOutsideFor()`, `Run_ReturnsSemanticFailureForExitDoOutsideDo()`, acceptance `samples/acceptance/negative/for_step_zero.axm`; после отложенной очистки `BytecodeEvaluationStack` и прямых специализированных comparison-jump путей адресный benchmark `samples/smoke/engine-compare/control_flow_branch_maze.axm` на длинном прогоне держится около `0.826x` runtime / `0.814x` total |
| Процедуры и функции: `SUB`, `FUNCTION`, `RETURN`, `BYREF`, `BYVAL` | да | да | да | да | да | частично | горячо измерено, следующий договор | Негативные опоры: `samples/operators/function/fixed_string_parameter_error.axm`, `samples/operators/function/fixed_string_return_error.axm`, `samples/acceptance/negative/byref_requires_variable.axm`, диагностики `MB2026`, `MB2028`, `MB2029` и parser/binder/runtime/acceptance-проверки; добавлены runnable-примеры `samples/operators/sub/byref_record_argument.axm` и `samples/operators/sub/byval_record_copy.axm`, но текущая реализация пока сознательно ограничивает `BYREF` простыми переменными и не допускает его для first-class callback-вызовов; адресный benchmark `samples/smoke/engine-compare/routine_closure_pipeline.axm` остаётся косвенным покрытием слоя вызовов |
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
| `FILEEXISTS`, `DIREXISTS`, `PATHEXISTS`, `READFILE`, `READALLLINES`, `WRITEFILE`, `APPENDFILE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid()`, `Execute_BuiltinFileSystemReadsAllLines()`, `Execute_BuiltinFileSystemReadsAllLinesFromEmptyFile()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-text.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/fileexists|direxists|pathexists|readfile|readalllines|writefile|appendfile/`; новый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total |
| `FILESIZE`, `FILEDATE`, `FILECREATEDATE`, `FILEACCESSEDDATE`, `DIRDATE` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemReadsWritesAndChecksPaths()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-paths.html`; sample: `samples/functions/filesize|filedate|filecreatedate|fileaccesseddate|dirdate/`; новый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total |
| `GETFILES`, `GETDIRS`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `EXTNAME`, `FULLPATH`, `PATHROOT`, `ISABSOLUTEPATH` | да | да | да | да | да | да | горячо измерено, целевой диапазон | Тесты: `Execute_BuiltinFileSystemPathHelpersReturnExpectedValues()`, `Execute_BuiltinFileSystemEnumeratesFilesAndDirectories()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/getfiles|getdirs|combinepath|filename|dirname|extname|fullpath|pathroot|isabsolutepath/`; новый адресный benchmark `samples/smoke/engine-compare/file_path_metadata_sweep.axm` держится около `0.768x` runtime / `0.773x` total |
| `MAKEDIR`, `DELETEFILE`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEDIR`, `DELETEDIRRECURSIVE`, `COPYDIR`, `MOVEDIR`, `RENAMEDIR` | да | да | да | да | да | да | горячо пройдено, рабочий паритет | Тесты: `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesFiles()`, `Execute_BuiltinFileSystemCopiesMovesRenamesAndDeletesDirectories()`, `Execute_BuiltinFileSystemReportsCopyFileMissingSource()`, `Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Execute_CompiledFileSystemBuiltinReportsMissingFile()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/makedir|deletefile|copyfile|movefile|renamefile|deletedir|deletedirrecursive|copydir|movedir|renamedir/`; новый адресный benchmark `samples/smoke/engine-compare/file_management_roundtrip.axm` держится около `0.985x` runtime / `0.987x` total |
| `CHANGEDIR`, `CURRENTDIR`, `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `ISREADONLY`, `ISHIDDEN`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE`, `MAKETEMPDIR` | да | да | да | да | да | да | горячо пройдено, рабочий паритет | Тесты: `Execute_BuiltinFileSystemReturnsCurrentDirectorySizeAndFreeSpace()`, `Execute_BuiltinFileSystemChangesCurrentDirectoryAndDeletesDirectoryRecursively()`, `Execute_BuiltinFileSystemCreatesTemporaryFileAndDirectory()`, `Execute_BuiltinFileSystemReturnsExtendedDatesAndAttributes()`, `Execute_BuiltinFileSystemReturnsExtendedPathAndDriveInfo()`, `Execute_CompiledFileSystemBuiltinsProgram()`, `Run_WithBytecodeMode_UsesBytecodeVmForFileSystemBuiltins()`, `Run_WithBytecodeMode_ReportsFileSystemRuntimeFailureFromVm()`; manual: `docs/manual/file-system-functions-management.html`, `docs/manual/file-system-functions-paths.html`, `docs/manual/file-system-functions.html`; sample: `samples/functions/changedir|currentdir|dirsize|freespace|totalfreespace|totalspace|isreadonly|ishidden|drivename|driveformat|drivetype|maketempfile|maketempdir/`; адресные benchmark: `samples/smoke/engine-compare/file_management_roundtrip.axm` держится около `0.985x` runtime / `0.987x` total для `CHANGEDIR`/`CURRENTDIR`, а `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` держится около `0.992x` runtime / `0.994x` total для `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE` и `MAKETEMPDIR` |

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



## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-07 - TRY/CATCH/FINALLY

РЎС‚Р°С‚СѓСЃ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєРѕР№ С„РѕСЂРјС‹ `TRY ... CATCH ... FINALLY ... END TRY`:
- `VM`: РЅРµС‚;
- `РђРІС‚РѕС‚РµСЃС‚С‹`: РґР°, РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂРЅС‹Рµ РїСЂРѕРІРµСЂРєРё РґРѕР±Р°РІР»РµРЅС‹ РІ `tests/Axiom.Runtime.Tests/InterpreterTests.cs`;
- `Sample`: РґР°, `samples/operators/try/handled_runtime_error.axm`;
- `Manual`: РґР°, `docs/manual/try.html`;
- `РЎРєРІРѕР·РЅРѕР№ VM`: РЅРµС‚;
- `РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚`: РЅРµС‚;
- `РџСЂРѕС„РёР»СЊ`: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: РїРµСЂРІС‹Р№ РёСЃРїРѕР»РЅСЏРµРјС‹Р№ СЃСЂРµР· СЂРµР°Р»РёР·РѕРІР°РЅ С‚РѕР»СЊРєРѕ РІ РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂРµ. РћРЅ РїСЂРѕРІРµСЂСЏРµС‚ РїРµСЂРµС…РІР°С‚ runtime-РѕС€РёР±РєРё, РїРѕР»СЏ `ERROR.Code`, `ERROR.Line`, `ERROR.Column`, РІС‹РїРѕР»РЅРµРЅРёРµ `FINALLY` РїРѕСЃР»Рµ РѕР±С‹С‡РЅРѕРіРѕ С‚РµР»Р°, РїРѕСЃР»Рµ РѕР±СЂР°Р±РѕС‚Р°РЅРЅРѕР№ РѕС€РёР±РєРё Рё РїРµСЂРµРґ РЅРµРѕР±СЂР°Р±РѕС‚Р°РЅРЅРѕР№ РѕС€РёР±РєРѕР№. `BytecodeVm` Рё СЃРєРІРѕР·РЅРѕР№ РїР°СЂРёС‚РµС‚ РѕСЃС‚Р°СЋС‚СЃСЏ СЃР»РµРґСѓСЋС‰РёРј СЌС‚Р°РїРѕРј.

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
## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - VM-РїРѕРґРґРµСЂР¶РєР° TRY/CATCH/FINALLY Рё ERROR

РЎС‚Р°С‚СѓСЃ С„РѕСЂРј `TRY ... CATCH ... FINALLY ... END TRY` Рё `ERROR` РїРѕСЃР»Рµ Р±Р°Р№С‚РєРѕРґРЅРѕРіРѕ СЃСЂРµР·Р°:
- `VM`: РґР°;
- `РђРІС‚РѕС‚РµСЃС‚С‹`: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ acceptance-РїСЂРѕРІРµСЂРєРё `TryCatchAcceptanceSample_RunsOnBytecodeVm`, `ErrorAcceptanceSample_RunsOnBytecodeVm` Рё РѕР±С‰РёР№ РїРѕР»РѕР¶РёС‚РµР»СЊРЅС‹Р№ bytecode-Р°СѓРґРёС‚ samples;
- `Sample`: РґР°, `samples/operators/try/handled_runtime_error.axm` Рё `samples/operators/error/user_code_catch.axm`;
- `Manual`: РґР°, `docs/manual/try.html`, `docs/manual/error.html`, `docs/manual/error-handling.html`;
- `РЎРєРІРѕР·РЅРѕР№ VM`: РґР°, РѕР±Р° sample С‚РµРїРµСЂСЊ РѕР±СЏР·Р°РЅС‹ РїСЂРѕС…РѕРґРёС‚СЊ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· interpreter fallback;
- `РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚`: С‡Р°СЃС‚РёС‡РЅРѕ, Р±Р°Р·РѕРІР°СЏ runtime-РѕС€РёР±РєР° Рё РїРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєР°СЏ РѕС€РёР±РєР° РїСЂРѕРІРµСЂСЏСЋС‚СЃСЏ, СЂР°РЅРЅРёРµ РІС‹С…РѕРґС‹ С‡РµСЂРµР· `FINALLY` С‚СЂРµР±СѓСЋС‚ РѕС‚РґРµР»СЊРЅРѕР№ РґРѕСЃРІРµСЂРєРё;
- `РџСЂРѕС„РёР»СЊ`: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: VM-РїСѓС‚СЊ РёСЃРїРѕР»СЊР·СѓРµС‚ СЃР»СѓР¶РµР±РЅС‹Р№ СЃС‚РµРє РѕР±СЂР°Р±РѕС‚С‡РёРєРѕРІ РІРЅСѓС‚СЂРё `BytecodeCallFrame`. РћР±СЂР°Р±РѕС‚Р°РЅРЅР°СЏ РґРёР°РіРЅРѕСЃС‚РёРєР° СѓРґР°Р»СЏРµС‚СЃСЏ РёР· РёС‚РѕРіРѕРІРѕРіРѕ СЂРµР·СѓР»СЊС‚Р°С‚Р° Рё РїСЂРµРІСЂР°С‰Р°РµС‚СЃСЏ РІ Р·РЅР°С‡РµРЅРёРµ РІСЃС‚СЂРѕРµРЅРЅРѕРіРѕ С‚РёРїР° `ERROR`, РїРѕСЌС‚РѕРјСѓ РєРѕРЅС‚СЂР°РєС‚ `err.Code AS STRING`, `err.Message AS STRING`, `err.Line AS INTEGER`, `err.Column AS INTEGER` СЃРѕРІРїР°РґР°РµС‚ СЃ РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂРѕРј.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - RETURN С‡РµСЂРµР· FINALLY РІ VM

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `TRY/CATCH/FINALLY`:
- `RETURN` РёР· `TRY`/`CATCH` С‚РµРїРµСЂСЊ РІС‹РїРѕР»РЅСЏРµС‚ `FINALLY` РІ `BytecodeVm` РїРµСЂРµРґ С„Р°РєС‚РёС‡РµСЃРєРёРј РІС‹С…РѕРґРѕРј РёР· РїСЂРѕС†РµРґСѓСЂС‹;
- Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєР°СЏ РїСЂРѕРІРµСЂРєР°: `Run_WithBytecodeMode_RunsFinallyBeforeFunctionReturn`;
- РѕСЃС‚Р°РІС€РёР№СЃСЏ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅС‹Р№ Р·Р°Р·РѕСЂ: `EXIT FOR` Рё `EXIT DO` С‡РµСЂРµР· `FINALLY` РµС‰С‘ С‚СЂРµР±СѓСЋС‚ РѕС‚РґРµР»СЊРЅРѕРіРѕ lowering-РїСѓС‚Рё.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - EXIT FOR/EXIT DO С‡РµСЂРµР· FINALLY РІ VM

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `TRY/CATCH/FINALLY`:
- `EXIT FOR` Рё `EXIT DO` С‚РµРїРµСЂСЊ РЅРµ РѕР±С…РѕРґСЏС‚ `FINALLY`, РєРѕРіРґР° РїРµСЂРµС…РѕРґ РїРѕРєРёРґР°РµС‚ Р°РєС‚РёРІРЅС‹Р№ `TRY`/`CATCH`;
- Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёРµ РїСЂРѕРІРµСЂРєРё: `Run_WithBytecodeMode_RunsFinallyBeforeExitForLeavesTry`, `Run_WithBytecodeMode_DoesNotRunFinallyBeforeExitForInsideSameTry`, `Run_WithBytecodeMode_RunsFinallyBeforeExitDoLeavesTry`;
- СЃС‚Р°С‚СѓСЃ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅРѕРіРѕ РїР°СЂРёС‚РµС‚Р° РґР»СЏ СЂР°РЅРЅРёС… РІС‹С…РѕРґРѕРІ С‡РµСЂРµР· `FINALLY`: Р±Р°Р·РѕРІРѕ Р·Р°РєСЂС‹С‚ РґР»СЏ `RETURN`, `EXIT FOR`, `EXIT DO`; РѕСЃС‚Р°С‘С‚СЃСЏ РѕС‚РґРµР»СЊРЅР°СЏ РґРѕСЃРІРµСЂРєР° РІР»РѕР¶РµРЅРЅС‹С… РѕР±СЂР°Р±РѕС‚С‡РёРєРѕРІ Рё РѕС€РёР±РѕРє РІРЅСѓС‚СЂРё `FINALLY`.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - РІР»РѕР¶РµРЅРЅС‹Рµ FINALLY Рё IDE fallback

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `TRY/CATCH/FINALLY`:
- РІР»РѕР¶РµРЅРЅС‹Рµ `FINALLY` РїСЂРѕРІРµСЂРµРЅС‹ РЅР° РїРѕСЂСЏРґРѕРє РІС‹РїРѕР»РЅРµРЅРёСЏ РёР·РЅСѓС‚СЂРё РЅР°СЂСѓР¶Сѓ;
- РѕС€РёР±РєР° РІРЅСѓС‚СЂРё `FINALLY` РїСЂРѕРІРµСЂРµРЅР° РЅР° РїРµСЂРµС…РІР°С‚ РІРЅРµС€РЅРёРј `CATCH`;
- РІСЂРµРјРµРЅРЅС‹Р№ IDE fallback РїРѕ `MB4001` СЃРЅСЏС‚, РїРѕСЌС‚РѕРјСѓ РѕР±С‹С‡РЅС‹Р№ Р·Р°РїСѓСЃРє `TRY`/`ERROR` РІ СЂРµР¶РёРјРµ `Bytecode` РґРѕР»Р¶РµРЅ РѕСЃС‚Р°РІР°С‚СЊСЃСЏ РЅР° `BytecodeVm`;
- Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёРµ РїСЂРѕРІРµСЂРєРё: `Run_WithBytecodeMode_RunsNestedFinallyBlocksInsideOut`, `Run_WithBytecodeMode_AllowsOuterCatchToHandleErrorFromFinally`, `Run_WithBytecodeMode_RunsTryBlockOnBytecodeVm`.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - СЂСѓС‡РЅРѕР№ IDE-smoke TRY/ERROR

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєР°Рј `TRY/CATCH/FINALLY` Рё `ERROR`:
- `samples/operators/error/user_code_catch.axm` РІСЂСѓС‡РЅСѓСЋ РѕС‚РєСЂС‹С‚ Рё РІС‹РїРѕР»РЅРµРЅ РІ `Axiom.Ide`; РѕР¶РёРґР°РµРјС‹Р№ РІС‹РІРѕРґ `APP001`, `Manual failure`, `after` РїРѕРґС‚РІРµСЂР¶РґС‘РЅ;
- `samples/operators/try/handled_runtime_error.axm` РІСЂСѓС‡РЅСѓСЋ РѕС‚РєСЂС‹С‚ Рё РІС‹РїРѕР»РЅРµРЅ РІ `Axiom.Ide`; РѕР¶РёРґР°РµРјС‹Р№ РІС‹РІРѕРґ СЃ `MB3001`, СЃС‚СЂРѕРєРѕР№ `4`, СЃРѕРѕР±С‰РµРЅРёРµРј `Р”РµР»РµРЅРёРµ РЅР° РЅРѕР»СЊ.`, `cleanup` Рё `after` РїРѕРґС‚РІРµСЂР¶РґС‘РЅ;
- IDE-smoke СЃС‚Р°С‚СѓСЃ: РїРѕРґС‚РІРµСЂР¶РґС‘РЅ РІСЂСѓС‡РЅСѓСЋ 2026-05-08.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - ERROR err

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ERROR`:
- `VM`: РґР°, С„РѕСЂРјР° `ERROR err` РїРѕРЅРёР¶Р°РµС‚СЃСЏ РІ СЂРµР¶РёРј `RaiseError` РґР»СЏ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРіРѕ Р·РЅР°С‡РµРЅРёСЏ `ERROR`;
- `РђРІС‚РѕС‚РµСЃС‚С‹`: РґР°, РґРѕР±Р°РІР»РµРЅС‹ `Bind_ErrorStatementAcceptsCaughtRuntimeError`, `Execute_ErrorStatementRethrowsCaughtRuntimeError`, `Run_WithBytecodeMode_RethrowsCaughtRuntimeError`, `ErrorRethrowAcceptanceSample_RunsOnBytecodeVm`;
- `Sample`: РґР°, `samples/operators/error/rethrow_caught_error.axm`;
- `Manual`: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/error.html`, `docs/manual/error-handling.html`, `docs/manual/try.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- `РЎРєРІРѕР·РЅРѕР№ VM`: РґР°, acceptance sample РѕР±СЏР·Р°РЅ РІС‹РїРѕР»РЅСЏС‚СЊСЃСЏ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· interpreter fallback;
- `РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚`: С‡Р°СЃС‚РёС‡РЅРѕ, С‚РёРїРѕРІРѕРµ РїСЂР°РІРёР»Рѕ `ERROR 10` РѕСЃС‚Р°С‘С‚СЃСЏ РїСЂРѕРІРµСЂРµРЅРЅС‹Рј, Р° `ERROR err, message` СЃРѕР·РЅР°С‚РµР»СЊРЅРѕ РЅРµ РІРІРѕРґРёС‚СЃСЏ РєР°Рє РѕС‚РґРµР»СЊРЅР°СЏ С„РѕСЂРјР°;
- `РџСЂРѕС„РёР»СЊ`: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: `ERROR err` СЃРѕС…СЂР°РЅСЏРµС‚ РёСЃС…РѕРґРЅС‹Рµ `Code`, `Message`, `Line` Рё `Column`, РїРѕСЌС‚РѕРјСѓ РІРЅРµС€РЅРёР№ `CATCH` РІРёРґРёС‚ РјРµСЃС‚Рѕ РїРµСЂРІРѕРЅР°С‡Р°Р»СЊРЅРѕР№ РѕС€РёР±РєРё, Р° РЅРµ СЃС‚СЂРѕРєСѓ РїРѕРІС‚РѕСЂРЅРѕРіРѕ РїСЂРѕР±СЂРѕСЃР°.
## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-08 - SELECT CASE РіСЂР°РЅРёС‡РЅС‹Рµ samples

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `IF ... ELSE ... END IF`, `SELECT CASE`:
- `Sample`: РґРѕР±Р°РІР»РµРЅС‹ `samples/control-flow/select_case/boolean_values.axm`, `samples/control-flow/select_case/first_match.axm`, `samples/control-flow/select_case/no_match_no_else.axm` Рё negative sample `samples/acceptance/negative/select_case_invalid_types.axm`;
- `РђРІС‚РѕС‚РµСЃС‚С‹`: РґРѕР±Р°РІР»РµРЅС‹ parser-РїСЂРѕРІРµСЂРєРё РїСѓСЃС‚РѕРіРѕ `CASE`, РїРѕР·РґРЅРµРіРѕ `CASE` РїРѕСЃР»Рµ `CASE ELSE`, `CASE IS` Р±РµР· РѕРїРµСЂР°С‚РѕСЂР°; binder-РїСЂРѕРІРµСЂРєР° boolean-СЃРµР»РµРєС‚РѕСЂРѕРІ; execution-РїСЂРѕРІРµСЂРєРё РїРµСЂРІРѕР№ РїРѕРґРѕС€РµРґС€РµР№ РІРµС‚РІРё Рё РѕС‚СЃСѓС‚СЃС‚РІРёСЏ СЃРѕРІРїР°РґРµРЅРёСЏ Р±РµР· `CASE ELSE`; acceptance-РїСЂРѕРІРµСЂРєРё РЅРѕРІС‹С… samples;
- `РЎРєРІРѕР·РЅРѕР№ VM`: РїРѕР»РѕР¶РёС‚РµР»СЊРЅС‹Рµ samples РІС…РѕРґСЏС‚ РІ `ExecutionMode.Bytecode` Р±РµР· interpreter fallback;
- `Manual`: РѕР±РЅРѕРІР»РµРЅС‹ СЃСЃС‹Р»РєРё РІ `docs/manual/select-case.html` Рё `docs/manual/examples.html`.

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
- Негативный паритет: частично, базовое правило ошибки обработчика MB2034 покрыто, но отдельная адресная досверка неизвестных имён и более сложных комбинаций отключения ещё может быть расширена;
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

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - ON KEY("ANY")

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ON KEY`:
- VM: РґР°, Р·Р°РїР°СЃРЅРѕР№ РѕР±СЂР°Р±РѕС‚С‡РёРє `"ANY"` РІС‹Р±РёСЂР°РµС‚СЃСЏ РІ `BytecodeVm`, РµСЃР»Рё РґР»СЏ СЃРѕР±С‹С‚РёСЏ РЅРµС‚ С‚РѕС‡РЅРѕРіРѕ РѕР±СЂР°Р±РѕС‚С‡РёРєР° РєР»Р°РІРёС€Рё;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ РїСЂРѕРІРµСЂРєРё РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂР° Рё `BytecodeVm` РґР»СЏ РѕР±СЂР°Р±РѕС‚РєРё Р»СЋР±РѕР№ РєР»Р°РІРёС€Рё Рё РїСЂРёРѕСЂРёС‚РµС‚Р° С‚РѕС‡РЅРѕРіРѕ РѕР±СЂР°Р±РѕС‚С‡РёРєР° РЅР°Рґ `"ANY"`;
- Sample: РґР°, `samples/operators/on-key/any_key_handler.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/on-key.html`, `docs/manual/keydown.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `OnKeyAnyAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ sample РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РЅРµ С‚СЂРµР±СѓРµС‚СЃСЏ, РїРѕС‚РѕРјСѓ С‡С‚Рѕ СЃСЂРµР· РґРѕР±Р°РІР»СЏРµС‚ РїРѕР»РѕР¶РёС‚РµР»СЊРЅСѓСЋ С„РѕСЂРјСѓ РІС‹Р±РѕСЂР° РѕР±СЂР°Р±РѕС‚С‡РёРєР° Р±РµР· РЅРѕРІРѕР№ РґРёР°РіРЅРѕСЃС‚РёРєРё;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: `"ANY"` РѕСЃС‚Р°С‘С‚СЃСЏ РїСЃРµРІРґРѕРёРјРµРЅРµРј С‚РѕР»СЊРєРѕ РґР»СЏ `ON KEY`. `KEYDOWN("ANY")` Рё `KEYPRESS("ANY")` РЅРµ РїРѕР»СѓС‡Р°СЋС‚ РЅРѕРІРѕР№ СЃРїРµС†РёР°Р»СЊРЅРѕР№ СЃРµРјР°РЅС‚РёРєРё РІ СЌС‚РѕРј СЃСЂРµР·Рµ.

РЈС‚РѕС‡РЅРµРЅРёРµ: РѕР±СЂР°Р±РѕС‚С‡РёРє `"ANY"` РІС‹Р·С‹РІР°РµС‚СЃСЏ РґР»СЏ РІСЃРµС… РґРѕСЃС‚СѓРїРЅС‹С… СЃРѕР±С‹С‚РёР№, РєРѕС‚РѕСЂС‹Рµ РґРёСЃРїРµС‚С‡РµСЂ `ON KEY` РІС‹С‡РёС‚С‹РІР°РµС‚ РЅР° РіСЂР°РЅРёС†Рµ РѕРїРµСЂР°С‚РѕСЂР° Рё РґР»СЏ РєРѕС‚РѕСЂС‹С… РЅРµС‚ С‚РѕС‡РЅРѕРіРѕ РѕР±СЂР°Р±РѕС‚С‡РёРєР°. Р­С‚Рѕ СЃРѕС…СЂР°РЅСЏРµС‚ СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓСЋС‰СѓСЋ РјРѕРґРµР»СЊ `ON KEY`, РіРґРµ РґРёСЃРїРµС‚С‡РµСЂ РјРѕР¶РµС‚ РѕР±СЂР°Р±РѕС‚Р°С‚СЊ РЅРµСЃРєРѕР»СЊРєРѕ РЅР°РєРѕРїР»РµРЅРЅС‹С… СЃРѕР±С‹С‚РёР№ РїРѕРґСЂСЏРґ.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - РїР°СЂР°РјРµС‚СЂ РёРјРµРЅРё РєР»Р°РІРёС€Рё РІ ON KEY

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ON KEY`:
- VM: РґР°, `RegisterKeyHandler` РїРѕР»СѓС‡РёР» С„Р»Р°Рі РїРµСЂРµРґР°С‡Рё РёРјРµРЅРё РєР»Р°РІРёС€Рё, Р° `BytecodeVm` РІС‹Р·С‹РІР°РµС‚ РѕР±СЂР°Р±РѕС‚С‡РёРє СЃ Р°СЂРіСѓРјРµРЅС‚РѕРј `STRING`, РєРѕРіРґР° СЌС‚Рѕ С‚СЂРµР±СѓРµС‚СЃСЏ СЃРёРіРЅР°С‚СѓСЂРѕР№;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ РїСЂРѕРІРµСЂРєРё binder, РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂР° Рё `BytecodeVm` РґР»СЏ РѕР±СЂР°Р±РѕС‚С‡РёРєР° `SUB(keyName AS STRING)`;
- Sample: РґР°, `samples/operators/on-key/handler_key_name.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/on-key.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `OnKeyHandlerKeyNameAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ sample РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РґР°, binder РѕС‚РєР»РѕРЅСЏРµС‚ РѕР±СЂР°Р±РѕС‚С‡РёРє СЃ РїР°СЂР°РјРµС‚СЂРѕРј РЅРµ С‚РёРїР° `STRING`, Р° bytecode-РІР°Р»РёРґР°С‚РѕСЂ РїСЂРѕРІРµСЂСЏРµС‚ СЃРёРіРЅР°С‚СѓСЂСѓ РїРѕ С„Р»Р°РіСѓ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: С„РѕСЂРјР° РЅРµ РјРµРЅСЏРµС‚ РѕС‡РµСЂРµРґСЊ РєР»Р°РІРёС€ Рё РЅРµ РґРѕР±Р°РІР»СЏРµС‚ РЅРѕРІС‹Р№ opcode. РњРµРЅСЏРµС‚СЃСЏ С‚РѕР»СЊРєРѕ РѕРїРµСЂР°РЅРґ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµР№ РёРЅСЃС‚СЂСѓРєС†РёРё СЂРµРіРёСЃС‚СЂР°С†РёРё РѕР±СЂР°Р±РѕС‚С‡РёРєР°.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - РјРѕРґРёС„РёС†РёСЂРѕРІР°РЅРЅС‹Рµ РєР»Р°РІРёС€Рё С‡РµСЂРµР· KEYPRESS Рё KEYDOWN

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєР°Рј `KEYPRESS()` Рё `KEYDOWN()`:
- VM: Р±РµР· РЅРѕРІРѕРіРѕ opcode; СЃСѓС‰РµСЃС‚РІСѓСЋС‰Р°СЏ РјРѕРґРµР»СЊ СѓР¶Рµ РїСЂРёРЅРёРјР°РµС‚ СЃРѕСЃС‚РѕСЏРЅРёРµ СѓРґРµСЂР¶Р°РЅРёСЏ С‡РµСЂРµР· `IsKeyDown(...)` Рё СЃРѕР±С‹С‚РёСЏ С‡РµСЂРµР· `ReadKey()`;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ IDE-С‚РµСЃС‚С‹ РјР°СЂС€СЂСѓС‚РёР·Р°С‚РѕСЂР° Рё mapper-СЃР»РѕСЏ РґР»СЏ `CTRL`/`ALT`-РєР»Р°РІРёС€;
- Sample: РґР°, `samples/functions/keypress/modified_keypress.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/keypress.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `ModifiedKeyPressAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ sample РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РЅРµ С‚СЂРµР±СѓРµС‚СЃСЏ, РїРѕС‚РѕРјСѓ С‡С‚Рѕ СЃСЂРµР· РЅРµ РґРѕР±Р°РІР»СЏРµС‚ РЅРѕРІСѓСЋ РґРёР°РіРЅРѕСЃС‚РёРєСѓ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: СЃС‚СЂРѕРєРѕРІС‹Рµ СЃРѕС‡РµС‚Р°РЅРёСЏ РІРёРґР° `"CTRL+LEFT"` РЅР°РјРµСЂРµРЅРЅРѕ РЅРµ РІРІРѕРґСЏС‚СЃСЏ. РџСЂРѕРІРµСЂРєР° РєРѕРјР±РёРЅР°С†РёРё РѕСЃС‚Р°С‘С‚СЃСЏ РѕР±С‹С‡РЅС‹Рј РІС‹СЂР°Р¶РµРЅРёРµРј Axiom: `KEYDOWN("CTRL") AND KEYPRESS("LEFT")`.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - СЃС‚СЂРѕРєРѕРІС‹Рµ Р°РєРєРѕСЂРґС‹ РєР»Р°РІРёС€

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєР°Рј `KEYDOWN()`, `KEYPRESS()` Рё `ON KEY`:
- VM: РґР°, `BytecodeVm` РёСЃРїРѕР»СЊР·СѓРµС‚ РѕР±С‰РёР№ `KeyboardInputNormalizer.NormalizeChord(...)` РґР»СЏ `KEYDOWN()`, `KEYPRESS()` Рё СЂРµРіРёСЃС‚СЂР°С†РёРё `ON KEY`;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ РїСЂРѕРІРµСЂРєРё РЅРѕСЂРјР°Р»РёР·Р°С†РёРё Р°РєРєРѕСЂРґРѕРІ, interpreter- Рё VM-СЃС†РµРЅР°СЂРёРё РґР»СЏ `KEYPRESS("CTRL+ALT+DELETE")`, `KEYDOWN("CTRL+F+A+X")` Рё `ON KEY("CTRL+LEFT")`;
- Sample: РґР°, `samples/functions/keypress/modified_keypress.axm` РѕР±РЅРѕРІР»С‘РЅ РїРѕРґ `KEYPRESS("CTRL+LEFT")`, РґРѕР±Р°РІР»РµРЅ `samples/operators/on-key/chord_handler.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/keypress.html`, `docs/manual/keydown.html`, `docs/manual/on-key.html`, РѕР±Р·РѕСЂ РІРІРѕРґР° Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `ModifiedKeyPressAcceptanceSample_RunsOnBytecodeVm` Рё `OnKeyChordAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏСЋС‚ samples РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РЅРµ С‚СЂРµР±СѓРµС‚СЃСЏ, РїРѕС‚РѕРјСѓ С‡С‚Рѕ РїСѓСЃС‚С‹Рµ С‡Р°СЃС‚Рё Р°РєРєРѕСЂРґР° РёРіРЅРѕСЂРёСЂСѓСЋС‚СЃСЏ, Р° РЅРµРёР·РІРµСЃС‚РЅС‹Рµ РёРјРµРЅР° РѕСЃС‚Р°СЋС‚СЃСЏ РѕР±С‹С‡РЅС‹РјРё СЃС‚СЂРѕРєРѕРІС‹РјРё РєР»Р°РІРёС€Р°РјРё РїРѕ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРјСѓ РґРѕРіРѕРІРѕСЂСѓ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - СЂРµР¶РёРјС‹ ON KEY ON / STOP / OFF

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ON KEY`:
- VM: РґР°, РґРѕР±Р°РІР»РµРЅС‹ РёРЅСЃС‚СЂСѓРєС†РёРё `PauseKeyHandler` Рё `ResumeKeyHandler`, Р° `BytecodeVm` С…СЂР°РЅРёС‚ С„Р»Р°Рі Р°РєС‚РёРІРЅРѕСЃС‚Рё СЂРµРіРёСЃС‚СЂР°С†РёРё РѕР±СЂР°Р±РѕС‚С‡РёРєР°;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ parser-, binder-, interpreter- Рё VM-РїСЂРѕРІРµСЂРєРё РґР»СЏ `ON KEY(...), ON` Рё `ON KEY(...), STOP`;
- Sample: РґР°, `samples/operators/on-key/stop_resume_handler.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/on-key.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `OnKeyStopResumeAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ sample РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РЅРµ С‚СЂРµР±СѓРµС‚СЃСЏ, РїРѕС‚РѕРјСѓ С‡С‚Рѕ `ON` Рё `STOP` Р±РµР· СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµР№ СЂРµРіРёСЃС‚СЂР°С†РёРё СЏРІР»СЏСЋС‚СЃСЏ Р±РµР·РѕРїР°СЃРЅРѕР№ no-op-С„РѕСЂРјРѕР№;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚.

РџСЂРёРјРµС‡Р°РЅРёРµ: `STOP` РІ СЃРѕСЃС‚Р°РІРµ `ON KEY(...), STOP` РЅРµ РѕСЃС‚Р°РЅР°РІР»РёРІР°РµС‚ РїСЂРѕРіСЂР°РјРјСѓ. Р­С‚Рѕ СЂРµР¶РёРј РїР°СѓР·С‹ РєРѕРЅРєСЂРµС‚РЅРѕРіРѕ РѕР±СЂР°Р±РѕС‚С‡РёРєР° РєР»Р°РІРёС€Рё.

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
## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - ON MOUSE

РќРѕРІР°СЏ РѕР±Р»Р°СЃС‚СЊ: РѕРїРµСЂР°С‚РѕСЂ `ON MOUSE(buttonNumber), ...`.

РЎРѕСЃС‚РѕСЏРЅРёРµ РїРѕРєСЂС‹С‚РёСЏ:
- VM: РґР°, `BytecodeVm` РїРѕРґРґРµСЂР¶РёРІР°РµС‚ СЂРµРіРёСЃС‚СЂР°С†РёСЋ, СЃРЅСЏС‚РёРµ, РїР°СѓР·Сѓ Рё РІРѕР·РѕР±РЅРѕРІР»РµРЅРёРµ РѕР±СЂР°Р±РѕС‚С‡РёРєРѕРІ С‡РµСЂРµР· РёРЅСЃС‚СЂСѓРєС†РёРё `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler` Рё `ResumeMouseHandler`;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ parser-, binder-, interpreter-, BytecodeVm-, execution-pipeline- Рё acceptance-РїСЂРѕРІРµСЂРєРё РґР»СЏ СЂРµРіРёСЃС‚СЂР°С†РёРё, РїРµСЂРµРґР°С‡Рё РїР°СЂР°РјРµС‚СЂРѕРІ СЃРѕР±С‹С‚РёСЏ, РїР°СѓР·С‹ Рё РІРѕР·РѕР±РЅРѕРІР»РµРЅРёСЏ;
- Runnable sample: РґР°, `samples/operators/on-mouse/left_button_handler.axm`;
- Manual: РґР°, РґРѕР±Р°РІР»РµРЅР° СЃС‚СЂР°РЅРёС†Р° `docs/manual/on-mouse.html`, РѕР±РЅРѕРІР»РµРЅС‹ РЅР°РІРёРіР°С†РёСЏ Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `Run_WithBytecodeMode_UsesBytecodeVmForOnMouse` Рё `OnMouseAcceptanceSample_RunsOnBytecodeVm` РїРѕРґС‚РІРµСЂР¶РґР°СЋС‚ РІС‹РїРѕР»РЅРµРЅРёРµ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РґР°, binder РѕС‚РєР»РѕРЅСЏРµС‚ РЅРµРґРѕРїСѓСЃС‚РёРјСѓСЋ СЃРёРіРЅР°С‚СѓСЂСѓ РѕР±СЂР°Р±РѕС‚С‡РёРєР° `ON MOUSE` С‡РµСЂРµР· `MB2035`, Р° bytecode-РІР°Р»РёРґР°С‚РѕСЂ РїСЂРѕРІРµСЂСЏРµС‚ С„Р»Р°Рі РїРµСЂРµРґР°С‡Рё СЃРѕР±С‹С‚РёСЏ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РїРѕС‚РѕРјСѓ С‡С‚Рѕ `ON MOUSE` СЏРІР»СЏРµС‚СЃСЏ РёРЅС‚РµСЂР°РєС‚РёРІРЅС‹Рј СЃРѕР±С‹С‚РёР№РЅС‹Рј СЃР»РѕРµРј, Р° РЅРµ РІС‹С‡РёСЃР»РёС‚РµР»СЊРЅРѕ РіРѕСЂСЏС‡РёРј С†РёРєР»РѕРј.

РџСЂРёРјРµС‡Р°РЅРёРµ: `ON MOUSE` РЅРµ РґРѕР±Р°РІР»СЏРµС‚ РЅРѕРІС‹Р№ host-РєРѕРЅС‚СЂР°РєС‚. РћРЅ РёСЃРїРѕР»СЊР·СѓРµС‚ С‚Сѓ Р¶Рµ РѕС‡РµСЂРµРґСЊ `ReadMouseButtonPress()`, С‡С‚Рѕ Рё `MOUSEPRESS()`.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-09 - ON MOUSE UP Рё MOVE

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ON MOUSE`:
- VM: РґР°, `BytecodeVm` СЂР°Р·Р»РёС‡Р°РµС‚ `Press`, `Release` Рё `Move` С‡РµСЂРµР· РѕРїРµСЂР°РЅРґ РІРёРґР° СЃРѕР±С‹С‚РёСЏ РІ РёРЅСЃС‚СЂСѓРєС†РёСЏС… СЂРµРіРёСЃС‚СЂР°С†РёРё, СЃРЅСЏС‚РёСЏ, РїР°СѓР·С‹ Рё РІРѕР·РѕР±РЅРѕРІР»РµРЅРёСЏ РѕР±СЂР°Р±РѕС‚С‡РёРєР°;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ parser-, binder-, interpreter-, BytecodeVm-, IDE-router/service- Рё execution-РїСЂРѕРІРµСЂРєРё РґР»СЏ `ON MOUSE(1, "UP")` Рё `ON MOUSE("MOVE")`;
- Runnable sample: РґР°, `samples/operators/on-mouse/release_move_handler.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/on-mouse.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `OnMouseReleaseMoveAcceptanceSample_RunsOnBytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ sample РІ `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РґР°, РЅРµРёР·РІРµСЃС‚РЅРѕРµ СЃС‚СЂРѕРєРѕРІРѕРµ РёРјСЏ СЃРѕР±С‹С‚РёСЏ РґРёР°РіРЅРѕСЃС‚РёСЂСѓРµС‚СЃСЏ РЅР° СЃРµРјР°РЅС‚РёС‡РµСЃРєРѕРј СѓСЂРѕРІРЅРµ С‡РµСЂРµР· РЅРѕРІС‹Р№ РґРѕРіРѕРІРѕСЂ `MB2036`;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РїРѕС‚РѕРјСѓ С‡С‚Рѕ СЌС‚Рѕ РёРЅС‚РµСЂР°РєС‚РёРІРЅС‹Р№ СЃР»РѕР№ РІРІРѕРґР°.

РџСЂРёРјРµС‡Р°РЅРёРµ: `MOUSEPRESS()` РЅР°РјРµСЂРµРЅРЅРѕ РЅРµ СЂР°СЃС€РёСЂСЏРµС‚СЃСЏ РЅР° `UP` Рё `MOVE`; РѕРЅР° РѕСЃС‚Р°С‘С‚СЃСЏ С„СѓРЅРєС†РёРµР№ РѕРґРЅРѕСЂР°Р·РѕРІРѕР№ РїСЂРѕРІРµСЂРєРё РЅР°Р¶Р°С‚РёСЏ РєРЅРѕРїРєРё.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - ON MOUSE WHEEL

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРµ `ON MOUSE`:
- VM: РґР°, `BytecodeVm` СЂР°Р·Р»РёС‡Р°РµС‚ СЃРѕР±С‹С‚РёРµ `Wheel` С‡РµСЂРµР· СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ РѕРїРµСЂР°РЅРґ РІРёРґР° СЃРѕР±С‹С‚РёСЏ РІ РёРЅСЃС‚СЂСѓРєС†РёСЏС… `RegisterMouseHandler`, `UnregisterMouseHandler`, `PauseMouseHandler` Рё `ResumeMouseHandler`;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ parser-, binder-, interpreter-, BytecodeVm-, IDE-router/service- Рё execution-РїСЂРѕРІРµСЂРєРё РґР»СЏ `ON MOUSE("WHEEL")`;
- Runnable sample: РґР°, `samples/operators/on-mouse/wheel_handler.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/on-mouse.html`, `docs/manual/data-and-input.html` Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, `OnMouseWheelAcceptanceSample_RunsOnBytecodeVm` Рё `Run_WithBytecodeMode_UsesBytecodeVmForOnMouseWheel` РІС‹РїРѕР»РЅСЏСЋС‚ СЃС†РµРЅР°СЂРёР№ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: С‡Р°СЃС‚РёС‡РЅРѕ, РЅРµРёР·РІРµСЃС‚РЅС‹Рµ РёРјРµРЅР° СЃРѕР±С‹С‚РёР№ РїРѕ-РїСЂРµР¶РЅРµРјСѓ РґРёР°РіРЅРѕСЃС‚РёСЂСѓСЋС‚СЃСЏ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРј РґРѕРіРѕРІРѕСЂРѕРј `MB2036`, РѕС‚РґРµР»СЊРЅР°СЏ РЅРѕРІР°СЏ РѕС€РёР±РєР° РґР»СЏ РєРѕР»РµСЃР° РЅРµ РІРІРѕРґРёР»Р°СЃСЊ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, СЌС‚Рѕ РёРЅС‚РµСЂР°РєС‚РёРІРЅС‹Р№ СЃР»РѕР№ РІРІРѕРґР°.

РџСЂРёРјРµС‡Р°РЅРёРµ: `MOUSEPRESS()` РЅРµ СЂР°СЃС€РёСЂСЏР»СЃСЏ РЅР° РєРѕР»РµСЃРѕ. РЎРѕР±С‹С‚РёРµ `Wheel` РґРѕСЃС‚СѓРїРЅРѕ С‚РѕР»СЊРєРѕ С‡РµСЂРµР· `ON MOUSE`, Р° РІРµР»РёС‡РёРЅР° РїСЂРѕРєСЂСѓС‚РєРё РїРµСЂРµРґР°С‘С‚СЃСЏ РїРµСЂРІС‹Рј РїР°СЂР°РјРµС‚СЂРѕРј РѕР±СЂР°Р±РѕС‚С‡РёРєР°.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - SCREEN Рё РіСЂР°С„РёС‡РµСЃРєРёРµ РєРѕРјР°РЅРґС‹

РќРѕРІР°СЏ РѕР±Р»Р°СЃС‚СЊ: РіСЂР°С„РёС‡РµСЃРєРёР№ СЂРµР¶РёРј `SCREEN`, С‚РµРєСЃС‚РѕРІС‹Р№ СЃР»РѕР№, РїРёРєСЃРµР»СЊРЅС‹Рµ РїСЂРёРјРёС‚РёРІС‹ Рё С„СѓРЅРєС†РёРё СЃРІРµРґРµРЅРёР№ Рѕ С‚РµРєСѓС‰РµРј СЂРµР¶РёРјРµ.

РЎРѕСЃС‚РѕСЏРЅРёРµ РїРѕРєСЂС‹С‚РёСЏ:
- VM: РґР°, `BytecodeVm` РїРѕРґРґРµСЂР¶РёРІР°РµС‚ `SCREEN`, `CLS`, `COLOR`, `LOCATE`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `POINT`, `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH`, `SCREENHEIGHT`;
- РђРІС‚РѕС‚РµСЃС‚С‹: С‡Р°СЃС‚РёС‡РЅРѕ, РѕР±РЅРѕРІР»РµРЅС‹ runtime-РїСЂРѕРІРµСЂРєРё РјС‹С€РёРЅРѕРіРѕ РґРѕРіРѕРІРѕСЂР° Рё СЃРѕС…СЂР°РЅРµРЅР° СЃР±РѕСЂРѕС‡РЅР°СЏ РїСЂРѕРІРµСЂРєР° VM; РѕС‚РґРµР»СЊРЅС‹Рµ parser/execution/IDE-С‚РµСЃС‚С‹ РіСЂР°С„РёРєРё РѕСЃС‚Р°СЋС‚СЃСЏ Р±Р»РёР¶Р°Р№С€РёРј РґРѕР»РіРѕРј;
- Runnable sample: РґР°, РґРѕР±Р°РІР»РµРЅС‹ `samples/graphics/screen/mode_info.axm`, `samples/graphics/text/print_locate_color.axm`, `samples/graphics/input/input_on_screen.axm`, `samples/graphics/mouse/mouse_events.axm`, `samples/graphics/primitives/pset_line_circle.axm`;
- Manual: РґР°, РґРѕР±Р°РІР»РµРЅР° СЃС‚СЂР°РЅРёС†Р° `docs/manual/screen.html`, РѕР±РЅРѕРІР»РµРЅС‹ РЅР°РІРёРіР°С†РёСЏ Рё РїРѕРёСЃРєРѕРІС‹Р№ РёРЅРґРµРєСЃ;
- РЎРєРІРѕР·РЅРѕР№ VM: С‡Р°СЃС‚РёС‡РЅРѕ, Р±Р°Р№С‚РєРѕРґРЅР°СЏ РїРѕРґРґРµСЂР¶РєР° СЂРµР°Р»РёР·РѕРІР°РЅР° Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ, РЅРѕ РѕС‚РґРµР»СЊРЅС‹Р№ execution-С‚РµСЃС‚ РЅР° `ExecutionMode.Bytecode` РґР»СЏ РіСЂР°С„РёС‡РµСЃРєРёС… РїСЂРѕРіСЂР°РјРј РµС‰С‘ РЅСѓР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РїРѕС‚РѕРјСѓ С‡С‚Рѕ РїРµСЂРІС‹Р№ РіСЂР°С„РёС‡РµСЃРєРёР№ СЃР»РѕР№ РёРЅС‚РµСЂР°РєС‚РёРІРЅС‹Р№ Рё host-Р·Р°РІРёСЃРёРјС‹Р№.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - SCREEN samples РІ BytecodeVm

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN` Рё РіСЂР°С„РёС‡РµСЃРєРёС… РєРѕРјР°РЅРґ:
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ acceptance-РїСЂРѕРІРµСЂРєРё `ScreenModeInfoAcceptanceSample_RunsOnBytecodeVm`, `ScreenTextAcceptanceSample_RunsOnBytecodeVm`, `ScreenInputAcceptanceSample_RunsOnBytecodeVm`, `ScreenPrimitivesAcceptanceSample_RunsOnBytecodeVm` Рё `ScreenMouseAcceptanceSample_RunsOnBytecodeVm`.
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, РІСЃРµ РїСЏС‚СЊ `samples/graphics/` Р·Р°РїСѓСЃРєР°СЋС‚СЃСЏ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ.
- РЈС‚РѕС‡РЅРµРЅРёРµ РїРѕРєСЂС‹С‚РёСЏ: С‚Р°Р±Р»РёС†Р° `BytecodeVm.SupportsBuiltin(...)` С‚РµРїРµСЂСЊ РІРєР»СЋС‡Р°РµС‚ `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH`, `SCREENHEIGHT` Рё `POINT`, РїРѕСЌС‚РѕРјСѓ execution pipeline Р±РѕР»СЊС€Рµ РЅРµ РѕС‚СЃРµРєР°РµС‚ РіСЂР°С„РёС‡РµСЃРєРёРµ РїСЂРѕРіСЂР°РјРјС‹ СЃ СЌС‚РёРјРё built-in РєР°Рє РЅРµРїРѕРєСЂС‹С‚С‹Рµ.

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
## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - СЂР°Р·РЅРµСЃС‘РЅ manual РїРѕ РіСЂР°С„РёРєРµ

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN` Рё РіСЂР°С„РёС‡РµСЃРєРёС… РєРѕРјР°РЅРґ:
- Manual: РґР°, РїСЂРµР¶РЅСЏСЏ РѕР±С‰Р°СЏ СЃС‚СЂР°РЅРёС†Р° `docs/manual/screen.html` СЂР°Р·РґРµР»РµРЅР° РЅР° РѕР±Р·РѕСЂ `docs/manual/graphics.html`, СЃРїСЂР°РІРѕС‡РЅСѓСЋ СЃС‚СЂР°РЅРёС†Сѓ `docs/manual/screen.html`, СЃС‚СЂР°РЅРёС†Сѓ С‚РµРєСЃС‚РѕРІРѕРіРѕ СЃР»РѕСЏ `docs/manual/graphics-text.html`, СЃС‚СЂР°РЅРёС†Сѓ РїСЂРёРјРёС‚РёРІРѕРІ `docs/manual/graphics-primitives.html` Рё СЃС‚СЂР°РЅРёС†Сѓ С„СѓРЅРєС†РёР№ СЃРѕСЃС‚РѕСЏРЅРёСЏ `docs/manual/screen-functions.html`;
- РќР°РІРёРіР°С†РёСЏ: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`, `docs/manual/index.html` Рё `docs/manual/reference.html`;
- РџРѕРєСЂС‹С‚РёРµ РїРѕРІРµРґРµРЅРёСЏ: РЅРµ РјРµРЅСЏР»РѕСЃСЊ, РѕР±РЅРѕРІР»РµРЅРёРµ РѕС‚РЅРѕСЃРёС‚СЃСЏ С‚РѕР»СЊРєРѕ Рє СЃРїСЂР°РІРѕС‡РЅРёРєСѓ Рё СѓС‚РѕС‡РЅСЏРµС‚ СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ `SCREEN`, `CLS`, `COLOR`, `LOCATE`, `PSET`, `PRESET`, `LINE`, `CIRCLE`, `POINT`, `SCREENISACTIVE`, `SCREENMODE`, `SCREENWIDTH` Рё `SCREENHEIGHT`;
- Runnable sample: Р±РµР· РёР·РјРµРЅРµРЅРёР№, СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ РїСЂРёРјРµСЂС‹ РІ `samples/graphics/` С‚РµРїРµСЂСЊ РїСЂСЏРјРѕ СѓРєР°Р·Р°РЅС‹ РЅР° РїСЂРѕС„РёР»СЊРЅС‹С… СЃС‚СЂР°РЅРёС†Р°С….

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - РёСЃРїСЂР°РІР»РµРЅР° РїСЂРёС‘РјРєР° COLOR РІ SCREEN

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN` Рё С‚РµРєСЃС‚РѕРІРѕРіРѕ СЃР»РѕСЏ:
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ IDE-РїСЂРѕРІРµСЂРєРё СЃРѕС…СЂР°РЅРµРЅРёСЏ С†РІРµС‚РѕРІ СЏС‡РµРµРє, РїСЂР°РІРёР»Р° `COLOR foreground` Р±РµР· СЃР±СЂРѕСЃР° С„РѕРЅР°, РїРѕСЂСЏРґРєР° РїР°Р»РёС‚СЂС‹ BGRA Рё РјР°СЃС€С‚Р°Р±РёСЂРѕРІР°РЅРёСЏ РєР°РґСЂР° renderer-РѕРј;
- Runnable sample: РґР°, РѕР±РЅРѕРІР»С‘РЅ `samples/graphics/text/print_locate_color.axm`, С‚РµРїРµСЂСЊ РѕРЅ РїСЂСЏРјРѕ РїРѕРєСЂС‹РІР°РµС‚ `COLOR 14, 1` Рё РїРѕСЃР»РµРґСѓСЋС‰РёР№ `COLOR 15` СЃ РїСЂРµР¶РЅРёРј С„РѕРЅРѕРј;
- Manual: Р±РµР· РёР·РјРµРЅРµРЅРёСЏ СЃС‚СЂСѓРєС‚СѓСЂС‹, СЃСѓС‰РµСЃС‚РІСѓСЋС‰Р°СЏ СЃС‚СЂР°РЅРёС†Р° `docs/manual/graphics-text.html` СѓР¶Рµ РѕРїРёСЃС‹РІР°Р»Р° РїСЂР°РІРёР»Рѕ В«РµСЃР»Рё С„РѕРЅ РЅРµ СѓРєР°Р·Р°РЅ, С‚РµРєСѓС‰РёР№ С„РѕРЅ РЅРµ РјРµРЅСЏРµС‚СЃСЏВ»;
- РЎРєРІРѕР·РЅРѕР№ VM: СЃС‚Р°С‚СѓСЃ РЅРµ РјРµРЅСЏРµС‚СЃСЏ, РїРѕС‚РѕРјСѓ С‡С‚Рѕ РёСЃРїСЂР°РІР»РµРЅРёРµ РЅР°С…РѕРґРёС‚СЃСЏ РІ IDE-host Рё framebuffer-РѕС‚СЂРёСЃРѕРІРєРµ, Р° РЅРµ РІ РїРѕРЅРёР¶РµРЅРёРё РёР»Рё РёСЃРїРѕР»РЅРµРЅРёРё Р±Р°Р№С‚РєРѕРґР°.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - СЂСѓСЃСЃРєРёР№ С‚РµРєСЃС‚ Рё 256 С†РІРµС‚РѕРІ SCREEN

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN` Рё РіСЂР°С„РёС‡РµСЃРєРёС… РєРѕРјР°РЅРґ:
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ IDE-РїСЂРѕРІРµСЂРєРё РІС‹РІРѕРґР° СЂСѓСЃСЃРєРёС… Р±СѓРєРІ РІ framebuffer Рё СЂР°СЃС€РёСЂРµРЅРЅРѕР№ РїР°Р»РёС‚СЂС‹ `0..255`;
- Runnable sample: РґР°, РѕР±РЅРѕРІР»С‘РЅ `samples/graphics/text/print_locate_color.axm`, С‚РµРїРµСЂСЊ РѕРЅ СЃРѕРґРµСЂР¶РёС‚ СЂСѓСЃСЃРєСѓСЋ СЃС‚СЂРѕРєСѓ Рё С†РІРµС‚РѕРІС‹Рµ РёРЅРґРµРєСЃС‹ РІС‹С€Рµ `15`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/graphics-text.html` Рё `docs/manual/graphics-primitives.html`: РїР°Р»РёС‚СЂР° РѕРїРёСЃР°РЅР° РєР°Рє `0..255`, СЃ РєР»Р°СЃСЃРёС‡РµСЃРєРёРјРё С†РІРµС‚Р°РјРё `0..15`, RGB-РєСѓР±РѕРј `16..231`, СЃРµСЂРѕР№ С€РєР°Р»РѕР№ `232..255` Рё РїСЂР°РІРёР»РѕРј `ABS(color) MOD 256`;
- РЎРєРІРѕР·РЅРѕР№ VM: СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ `ScreenTextAcceptanceSample_RunsOnBytecodeVm` РїСЂРѕРґРѕР»Р¶Р°РµС‚ Р·Р°РїСѓСЃРєР°С‚СЊ С‚РµРєСЃС‚РѕРІС‹Р№ РіСЂР°С„РёС‡РµСЃРєРёР№ sample С‡РµСЂРµР· `ExecutionMode.Bytecode`; РёР·РјРµРЅРµРЅРёРµ РѕС‚РЅРѕСЃРёС‚СЃСЏ Рє host-РѕС‚СЂРёСЃРѕРІРєРµ framebuffer Рё РЅРµ С‚СЂРµР±СѓРµС‚ РЅРѕРІС‹С… bytecode-РёРЅСЃС‚СЂСѓРєС†РёР№.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - Р±Р»РѕС‡РЅР°СЏ РїСЃРµРІРґРѕРіСЂР°С„РёРєР° SCREEN

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN` Рё С‚РµРєСЃС‚РѕРІРѕРіРѕ РіСЂР°С„РёС‡РµСЃРєРѕРіРѕ СЃР»РѕСЏ:
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ IDE-РїСЂРѕРІРµСЂРєРё РїРѕР»РЅРѕРіРѕ Р±Р»РѕРєР° `в–€`, СЌРєСЂР°РЅРЅРѕРіРѕ РєРѕРґР° `CHR(219)` Рё Р·Р°С‚РµРЅС‘РЅРЅС‹С… Р±Р»РѕРєРѕРІ `в–“`, `в–’`, `в–‘` РІРјРµСЃС‚Рµ СЃ РєРѕРґР°РјРё `178`, `177`, `176`;
- Runnable sample: РґР°, РѕР±РЅРѕРІР»С‘РЅ `samples/graphics/text/print_locate_color.axm`, С‚РµРїРµСЂСЊ РѕРЅ РІС‹РІРѕРґРёС‚ СЂР°СЃС€РёСЂРµРЅРЅСѓСЋ РїР°Р»РёС‚СЂСѓ `16..255` С‡РµСЂРµР· `PRINT CHR(219);` РІ СЏС‡РµР№РєР°С… `SCREEN`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅР° `docs/manual/graphics-text.html` СЃ РѕРїРёСЃР°РЅРёРµРј РїРѕРґРґРµСЂР¶Р°РЅРЅС‹С… Р±Р»РѕС‡РЅС‹С… РїСЃРµРІРґРѕСЃРёРјРІРѕР»РѕРІ Рё РїСЂРёРјРµСЂРѕРј РїР°Р»РёС‚СЂС‹;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ `ScreenTextAcceptanceSample_RunsOnBytecodeVm` РїСЂРѕРґРѕР»Р¶Р°РµС‚ Р·Р°РїСѓСЃРєР°С‚СЊ СЌС‚РѕС‚ РїСЂРёРјРµСЂ С‡РµСЂРµР· `ExecutionMode.Bytecode`; РёР·РјРµРЅРµРЅРёРµ РЅР°С…РѕРґРёС‚СЃСЏ РІ IDE-host rasterizer Рё РЅРµ С‚СЂРµР±СѓРµС‚ РЅРѕРІС‹С… РёРЅСЃС‚СЂСѓРєС†РёР№ VM;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, СЌС‚Рѕ РІРёР·СѓР°Р»СЊРЅРѕРµ РѕС‚РѕР±СЂР°Р¶РµРЅРёРµ С‚РµРєСЃС‚РѕРІРѕРіРѕ СЃР»РѕСЏ, Р° РЅРµ РІС‹С‡РёСЃР»РёС‚РµР»СЊРЅРѕ РіРѕСЂСЏС‡РёР№ СЃР»РѕР№ VM.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-10 - CHR Рё ASC СЃ CP866-РґРёР°РїР°Р·РѕРЅРѕРј

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРѕРІС‹Рј С„СѓРЅРєС†РёСЏРј `CHR` Рё `ASC`:
- VM: РґР°, `BytecodeVm` РёСЃРїРѕР»СЊР·СѓРµС‚ РѕР±С‰РёР№ `RuntimeCharacterEncoding` Рё РІРѕР·РІСЂР°С‰Р°РµС‚ CP866-СЃРёРјРІРѕР»С‹ РґР»СЏ `CHR(0..255)`;
- РРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ: РґР°, РёСЃРїРѕР»СЊР·СѓРµС‚ С‚РѕС‚ Р¶Рµ СЃР»РѕР№;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ runtime-, VM- Рё execution-РїСЂРѕРІРµСЂРєРё РґР»СЏ `CHR(159)`, `CHR(219)`, `ASC("РЇ")`, `ASC("в–€")` Рё Unicode-РєРѕРґР° РІС‹С€Рµ `255`;
- Runnable sample: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `samples/functions/chr/basic_usage.axm`, `samples/functions/asc/basic_usage.axm` Рё `samples/graphics/text/print_locate_color.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/string-functions-build-and-split.html` Рё `docs/manual/graphics-text.html`;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, execution-РїСЂРѕРІРµСЂРєРё РїРѕРґС‚РІРµСЂР¶РґР°СЋС‚ РІС‹РїРѕР»РЅРµРЅРёРµ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РёР·РјРµРЅРµРЅРёРµ РѕС‚РЅРѕСЃРёС‚СЃСЏ Рє СЃС‚СЂРѕРєРѕРІРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё Рё РІРёР·СѓР°Р»СЊРЅС‹Рј СЃРёРјРІРѕР»Р°Рј, Р° РЅРµ Рє РЅРѕРІРѕРјСѓ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕРјСѓ СЃС†РµРЅР°СЂРёСЋ.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-11 - РѕС‚РґРµР»СЊРЅР°СЏ С‚Р°Р±Р»РёС†Р° СЃРёРјРІРѕР»РѕРІ CHR Рё ASC РІ manual

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє СЃС‚СЂРѕРєРѕРІС‹Рј С„СѓРЅРєС†РёСЏРј `CHR` Рё `ASC`:
- Manual: РґР°, РїРѕРјРёРјРѕ `docs/manual/string-functions-build-and-split.html` Рё `docs/manual/graphics-text.html`, РґРѕР±Р°РІР»РµРЅР° РѕС‚РґРµР»СЊРЅР°СЏ СЃС‚СЂР°РЅРёС†Р° `docs/manual/characters.html` СЃ РїРѕР»РЅРѕР№ С‚Р°Р±Р»РёС†РµР№ ASCII/IBM CP866 РґР»СЏ РєРѕРґРѕРІ `0..255`;
- РќР°РІРёРіР°С†РёСЏ: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js`, `docs/manual/reference.html` Рё `docs/manual/index.html`;
- РџРѕРІРµРґРµРЅРёРµ СЏР·С‹РєР°, VM, Р°РІС‚РѕС‚РµСЃС‚С‹ Рё runnable samples РЅРµ РјРµРЅСЏР»РёСЃСЊ: СЌС‚Рѕ СѓС‚РѕС‡РЅРµРЅРёРµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєРѕРіРѕ СЃРїСЂР°РІРѕС‡РЅРёРєР° РїРѕ СѓР¶Рµ СЂРµР°Р»РёР·РѕРІР°РЅРЅРѕРјСѓ РґРѕРіРѕРІРѕСЂСѓ.
## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-11 - РѕРїРµСЂР°С‚РѕСЂ SLEEP

РќРѕРІР°СЏ РѕР±Р»Р°СЃС‚СЊ: РѕРїРµСЂР°С‚РѕСЂ РІСЂРµРјРµРЅРЅРѕР№ РѕСЃС‚Р°РЅРѕРІРєРё `SLEEP`.

РЎРѕСЃС‚РѕСЏРЅРёРµ РїРѕРєСЂС‹С‚РёСЏ:
- VM: РґР°, `BytecodeVm` РІС‹РїРѕР»РЅСЏРµС‚ РѕС‚РґРµР»СЊРЅСѓСЋ РёРЅСЃС‚СЂСѓРєС†РёСЋ `Opcode.Sleep`;
- РРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ: РґР°, interpreter РІС‹РїРѕР»РЅСЏРµС‚ С‚РѕС‚ Р¶Рµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєРёР№ РґРѕРіРѕРІРѕСЂ;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ parser-, binder-, interpreter- Рё `BytecodeVm`-РїСЂРѕРІРµСЂРєРё РґР»СЏ `SLEEP 0`, `SLEEP` Р±РµР· Р°СЂРіСѓРјРµРЅС‚Р° Рё РѕС‚СЂРёС†Р°С‚РµР»СЊРЅРѕР№ РґР»РёС‚РµР»СЊРЅРѕСЃС‚Рё;
- Runnable sample: РґР°, РґРѕР±Р°РІР»РµРЅ `samples/operators/sleep/delay_and_key_wait.axm`;
- Manual: РґР°, РґРѕР±Р°РІР»РµРЅР° СЃС‚СЂР°РЅРёС†Р° `docs/manual/sleep.html`, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/scripts/manual.js`, `docs/manual/scripts/search-index.js` Рё `docs/manual/reference.html`;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, РґРѕР±Р°РІР»РµРЅ acceptance-С‚РµСЃС‚ `SleepAcceptanceSample_RunsOnBytecodeVm`;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РґР°, runtime- Рё VM-РїСЂРѕРІРµСЂРєРё С„РёРєСЃРёСЂСѓСЋС‚ `MB3007` РґР»СЏ РѕС‚СЂРёС†Р°С‚РµР»СЊРЅРѕР№ РґР»РёС‚РµР»СЊРЅРѕСЃС‚Рё;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РѕРїРµСЂР°С‚РѕСЂ РїСЂРµРґРЅР°Р·РЅР°С‡РµРЅ РґР»СЏ РѕР¶РёРґР°РЅРёСЏ Рё РёРЅС‚РµСЂР°РєС‚РёРІРЅС‹С… СЃС†РµРЅР°СЂРёРµРІ, Р° РЅРµ РґР»СЏ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕРіРѕ СЃСЂР°РІРЅРµРЅРёСЏ.

## РћР±РЅРѕРІР»РµРЅРёРµ СЂРµРµСЃС‚СЂР° - 2026-05-11 - SCREEN 0 РєР°Рє С‚РµРєСЃС‚РѕРІРѕРµ РѕРєРЅРѕ

Р”РѕРїРѕР»РЅРµРЅРёРµ Рє РѕР±Р»Р°СЃС‚Рё `SCREEN`:
- VM: РґР°, `BytecodeVm` С‚РµРїРµСЂСЊ С‚СЂР°РєС‚СѓРµС‚ РёРЅСЃС‚СЂСѓРєС†РёСЋ `SCREEN 0` РєР°Рє РІРєР»СЋС‡РµРЅРёРµ СЌРєСЂР°РЅРЅРѕРіРѕ С‚РµРєСЃС‚РѕРІРѕРіРѕ РІС‹РІРѕРґР°, Р° РЅРµ РєР°Рє РІРѕР·РІСЂР°С‚ Рє СЃР»СѓР¶РµР±РЅРѕР№ РІРєР»Р°РґРєРµ `Output`;
- РРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ: РґР°, РёСЃРїРѕР»СЊР·СѓРµС‚ С‚РѕС‚ Р¶Рµ host-РґРѕРіРѕРІРѕСЂ `IsScreenOutputActive`;
- РђРІС‚РѕС‚РµСЃС‚С‹: РґР°, РґРѕР±Р°РІР»РµРЅС‹ IDE-РїСЂРѕРІРµСЂРєРё `ActivateScreenZero_UsesNativeTextWindowWithoutEnablingGraphicsMode` Рё `ScreenZeroInput_DoesNotEchoPromptToOutputPane`, Р° С‚Р°РєР¶Рµ acceptance-РїСЂРѕРІРµСЂРєР° `ScreenZeroTextInputAcceptanceSample_RunsOnBytecodeVm`;
- Runnable sample: РґР°, РґРѕР±Р°РІР»РµРЅ `samples/graphics/input/text_screen_input.axm`;
- Manual: РґР°, РѕР±РЅРѕРІР»РµРЅС‹ `docs/manual/screen.html`, `docs/manual/graphics.html` Рё `docs/manual/graphics-text.html`;
- РЎРєРІРѕР·РЅРѕР№ VM: РґР°, РЅРѕРІС‹Р№ sample РІС‹РїРѕР»РЅСЏРµС‚СЃСЏ С‡РµСЂРµР· `ExecutionMode.Bytecode` Р±РµР· РѕС‚РєР°С‚Р° РЅР° РёРЅС‚РµСЂРїСЂРµС‚Р°С‚РѕСЂ;
- РќРµРіР°С‚РёРІРЅС‹Р№ РїР°СЂРёС‚РµС‚: РїСЂРµР¶РЅРёР№ РґРѕРіРѕРІРѕСЂ `MB3010` СЃРѕС…СЂР°РЅСЏРµС‚СЃСЏ РґР»СЏ РіСЂР°С„РёС‡РµСЃРєРёС… РєРѕРјР°РЅРґ РІРЅРµ `SCREEN 1..10`;
- РџСЂРѕС„РёР»СЊ: РЅРµ РїСЂРёРѕСЂРёС‚РµС‚, РїРѕС‚РѕРјСѓ С‡С‚Рѕ СЌС‚Рѕ РёРЅС‚РµСЂР°РєС‚РёРІРЅР°СЏ РјР°СЂС€СЂСѓС‚РёР·Р°С†РёСЏ host-РІРІРѕРґР° Рё РІС‹РІРѕРґР°, Р° РЅРµ РІС‹С‡РёСЃР»РёС‚РµР»СЊРЅРѕ РіРѕСЂСЏС‡РёР№ СЃР»РѕР№ VM.
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