# Проверка примеров в Electron IDE

Дата: 10 октября 2026 года. Ветка master; промежуточный коммит перед запуском: 37f34d9.

## Что проверено

Каждый из 768 файлов открыт и проверен через Check в настоящей оболочке Electron. Каждый файл также запускался через Run: исходники и ресурсы запускались из отдельной рабочей копии. Отрицательные примеры проверялись по предусмотренным сообщениям, положительные — по завершению, выводу и событиям служебного процесса. Для интерактивных файлов передавались ввод и клавиши. Статус успешного завершения сам по себе не доказывает качество изображения или все варианты взаимодействия.

656 программ завершились успешно. 53 отрицательных примера остановлены компилятором; 59 отрицательных примеров выдали предусмотренную ошибку при выполнении. Предусмотренные в заголовках коды MB совпали с фактическими. Отмена по тайм-ауту не включена в итог как успешная проверка: такие случаи повторены с нужными действиями или достаточным временем.

Отдельные проверки Electron подтверждают передачу клавиш из консоли, движение героя Maze Chase, передачу пикселей CANVAS, щелчок мыши и мировые координаты ON MOUSE. Заключительный полный набор Electron прошёл: 59 из 59 проверок. Набор JavaScript прошёл: 48 из 48 проверок.

Приёмка новой оболочки основана на пофайловом Check/Run в Electron и 59 проверках её интерфейса (280 утверждений). Дополнительный последовательный набор .NET прошёл 6112 из 6112 тестов без пропусков, но включает компоненты прежней WPF IDE и не является доказательством проверки новой оболочки. В текущей архитектуре CANVAS отображается в окне Electron, а SCREEN отображается в нативном окне DesktopHost; запуск SCREEN из Electron не означает переноса его изображения в окно Electron.

## Найденные и исправленные проблемы

- Check пропускал графическую операцию в неподходящем режиме. Добавлена MB2046 с анализом порядка, ветвей и прямых вызовов.
- Консоль Electron не передавала состояния и события клавиш для INKEY, KEYDOWN, KEYPRESS и SLEEP. Передача добавлена.
- CANVAS не передавал перемещения, кнопки и колесо мыши в служебный процесс. Передача добавлена с пересчётом пикселей отображения в пиксели холста.
- Первое окно SCREEN могло оставаться скрытым из-за параметров запуска служебного процесса. Добавлена проверка действительной видимости окна и повторный явный показ; исправлен Unicode-вызов обработки заголовка.
- После закрытия вкладки сохранялись диагностические отметки её модели. При закрытии отметки теперь удаляются.
- Смешанные CRLF/LF в сохранённом файле приводили к ложному отказу Run. Сравнение нормализует окончания строк, продолжая обнаруживать изменение содержимого.
- Проверки переведённых примеров ожидали старый английский вывод. Ожидания синхронизированы.
- Пример восстановления кодировки требовал внешний путь C:\Cheburnet. Теперь учебный исходник находится рядом с программой, рабочая копия создаётся во временной папке.

## Пофайловые результаты

После последнего исправления повторены Check и Run всего корпуса. В 63 запусках, где служебный процесс сообщил о созданном нативном окне SCREEN, подтверждена видимость окна. Сохранены 26 снимков холстов CANVAS. Для примера палитры и тяжёлого сценария Maze Chase потребовалось увеличить время ожидания до 60 секунд.

Системный инструмент не смог получить изображение нативного окна SCREEN и выполнить в нём физический щелчок: получение кадра завершалось по тайм-ауту, геометрия для щелчка была недоступна. Поэтому этот отчёт подтверждает запуск, диагностику и видимость SCREEN, но не утверждает, что вручную проверены все рисунки и все варианты мышиного взаимодействия в каждом нативном окне. Настоящий щелчок CANVAS с проверкой мировых координат подтверждён отдельной проверкой Electron.

В столбце вывода указано начало фактического текстового вывода. Графический текст может выводиться в окно SCREEN и отсутствовать в консоли. Подробные события, весь вывод и ввод сохранены в .artifacts/samples-electron/merged-runs.json. Снимки отдельных холстов находятся в .artifacts/samples-electron/shots/.

| Файл | Check | Run | Диагностика | Вывод или взаимодействие |
|---|---|---|---|---|
| `samples/acceptance/negative/array_push_upper_bound_overflow.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/acceptance/negative/block_scope_visibility.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2001 | Без текстового вывода |
| `samples/acceptance/negative/byref_requires_variable.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2028 | Без текстового вывода |
| `samples/acceptance/negative/call_operator_hint.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2001 | Без текстового вывода |
| `samples/acceptance/negative/calling_non_function.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2020 | Без текстового вывода |
| `samples/acceptance/negative/dim_requires_type_or_initializer.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2017 | Без текстового вывода |
| `samples/acceptance/negative/division_by_zero.axm` | Success | Ожидаемая ошибка выполнения | MB3001 | Без текстового вывода |
| `samples/acceptance/negative/expression_statement_no_effect.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2040 | Без текстового вывода |
| `samples/acceptance/negative/for_counter_must_be_local.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2016 | Без текстового вывода |
| `samples/acceptance/negative/for_step_zero.axm` | Success | Ожидаемая ошибка выполнения | MB3004 | Без текстового вывода |
| `samples/acceptance/negative/function_branch_without_return_value.axm` | Success | Ожидаемая ошибка выполнения | MB3006 | 10 |
| `samples/acceptance/negative/if_block_scope_escape.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2001 | Без текстового вывода |
| `samples/acceptance/negative/if_duplicate_else.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB1005 | Без текстового вывода |
| `samples/acceptance/negative/if_requires_boolean.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2008 | Без текстового вывода |
| `samples/acceptance/negative/if_shadowing_outer_variable.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2016 | Без текстового вывода |
| `samples/acceptance/negative/invalid_numeric_input.axm` | Success | Ожидаемая ошибка выполнения | MB3003 | Ввод: 1 ответов |
| `samples/acceptance/negative/leading_zero_literal.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB0003 | Без текстового вывода |
| `samples/acceptance/negative/missing_return_value.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2007 | Без текстового вывода |
| `samples/acceptance/negative/return_value_in_sub.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2006 | Без текстового вывода |
| `samples/acceptance/negative/scope_block_escape.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2001 | Без текстового вывода |
| `samples/acceptance/negative/scope_field_requires_self.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2044 | Без текстового вывода |
| `samples/acceptance/negative/scope_field_typo_suggestion.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2023 | Без текстового вывода |
| `samples/acceptance/negative/scope_shadow_declaration.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2016 | Без текстового вывода |
| `samples/acceptance/negative/select_case_boolean_range.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2015 | Без текстового вывода |
| `samples/acceptance/negative/select_case_duplicate_case_else.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB1006 | Без текстового вывода |
| `samples/acceptance/negative/select_case_else_before_case.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB1004 | Без текстового вывода |
| `samples/acceptance/negative/select_case_empty_selector.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB1003 | Без текстового вывода |
| `samples/acceptance/negative/select_case_invalid_types.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2003 | Без текстового вывода |
| `samples/acceptance/negative/sub_name_as_value.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2019 | Без текстового вывода |
| `samples/acceptance/negative/type_constructor_invariant_false.axm` | Success | Ожидаемая ошибка выполнения | MB3005 | Без текстового вывода |
| `samples/acceptance/negative/type_cyclic_reference.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2022 | Без текстового вывода |
| `samples/acceptance/negative/type_default_non_constant.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2027 | Без текстового вывода |
| `samples/acceptance/negative/type_duplicate_declaration.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2002 | Без текстового вывода |
| `samples/acceptance/negative/type_invalid_field_type.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2022 | Без текстового вывода |
| `samples/acceptance/negative/type_literal_mixed_modes.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2031 | Без текстового вывода |
| `samples/acceptance/negative/type_literal_positional_arity.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2032 | Без текстового вывода |
| `samples/acceptance/negative/type_method_overload_ambiguous.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2033 | Без текстового вывода |
| `samples/acceptance/negative/type_unknown_field.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2023 | Без текстового вывода |
| `samples/acceptance/negative/undeclared_variable.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2001 | Без текстового вывода |
| `samples/acceptance/negative/unknown_callable.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2005 | Без текстового вывода |
| `samples/acceptance/negative/wrong_argument_count.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2004 | Без текстового вывода |
| `samples/acceptance/smoke_v0_1.axm` | Success | Успешно | — | ============================== / Axiom v0.1 smoke test / ============================== / Enter your name: / Enter your age: / User: 1 / Age: 1 / Age ; Ввод: 2 ответов |
| `samples/control-flow/do/continue_do.axm` | Success | Успешно | — | 1 / 2 / 4 / 5 |
| `samples/control-flow/do/do_loop_exit.axm` | Success | Успешно | — | 0 / 1 / done |
| `samples/control-flow/do/do_until_loop.axm` | Success | Успешно | — | 0 / 1 / 2 |
| `samples/control-flow/do/do_while_loop.axm` | Success | Успешно | — | 0 / 1 / 2 / done |
| `samples/control-flow/do/exit_do.axm` | Success | Успешно | — | 0 / 1 / Готово |
| `samples/control-flow/do/loop_until.axm` | Success | Успешно | — | 0 / 1 / 2 |
| `samples/control-flow/do/loop_while.axm` | Success | Успешно | — | 0 / 1 / 2 |
| `samples/control-flow/for/basic_loop.axm` | Success | Успешно | — | 1 / 2 / 3 |
| `samples/control-flow/for/continue_for.axm` | Success | Успешно | — | 1 / 2 / 4 / 5 |
| `samples/control-flow/for/exit_for.axm` | Success | Успешно | — | 1 / 2 / Готово |
| `samples/control-flow/for/nested_loops.axm` | Success | Успешно | — | 1:1 / 1:2 / 2:1 / 2:2 |
| `samples/control-flow/for/predeclared_counter.axm` | Success | Успешно | — | 1 / 2 / 3 / После цикла: 3 |
| `samples/control-flow/for/predeclared_counter_invalid_type.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2009 | Без текстового вывода |
| `samples/control-flow/for/step_loop.axm` | Success | Успешно | — | 0 / 2 / 4 / 6 |
| `samples/control-flow/if/basic_if_else.axm` | Success | Успешно | — | не положительное |
| `samples/control-flow/if/elseif_chain.axm` | Success | Успешно | — | среднее значение |
| `samples/control-flow/return/function_value.axm` | Success | Успешно | — | 42 |
| `samples/control-flow/return/sub_early_exit.axm` | Success | Успешно | — | До досрочного RETURN / После блока IF |
| `samples/control-flow/select_case/boolean_values.axm` | Success | Успешно | — | enabled |
| `samples/control-flow/select_case/exact_values.axm` | Success | Успешно | — | two |
| `samples/control-flow/select_case/first_match.axm` | Success | Успешно | — | first |
| `samples/control-flow/select_case/mixed_selectors.axm` | Success | Успешно | — | Значение подошло |
| `samples/control-flow/select_case/no_match_no_else.axm` | Success | Успешно | — | after |
| `samples/control-flow/select_case/ranges_and_is.axm` | Success | Успешно | — | Хорошо |
| `samples/control-flow/select_case/string_ranges.axm` | Success | Успешно | — | После буквы M / Первая часть алфавита |
| `samples/control-flow/select_case/string_values.axm` | Success | Успешно | — | Сохранить файл |
| `samples/control-flow/while/basic_loop.axm` | Success | Успешно | — | 0 / 1 / 2 |
| `samples/control-flow/while/continue_while.axm` | Success | Успешно | — | 1 / 2 / 4 / 5 |
| `samples/expressions/arithmetic/basic_math.axm` | Success | Успешно | — | 7 / 3 |
| `samples/expressions/arithmetic/decimal_overflow_error.axm` | Success | Ожидаемая ошибка выполнения | MB3008 | Без текстового вывода |
| `samples/expressions/arithmetic/double_overflow_error.axm` | Success | Ожидаемая ошибка выполнения | MB3008 | Без текстового вывода |
| `samples/expressions/arithmetic/long_overflow_error.axm` | Success | Ожидаемая ошибка выполнения | MB3008 | Без текстового вывода |
| `samples/expressions/arrays/index_access.axm` | Success | Успешно | — | 30 |
| `samples/expressions/comparisons/basic_compare.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/expressions/comparisons/string_order.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/expressions/conversions/automatic_numeric.axm` | Success | Успешно | — | 1.5 / 8 / 0 |
| `samples/expressions/hex-literals/basic_usage.axm` | Success | Успешно | — | 255 / 2147483648 |
| `samples/expressions/interpolation_basic.axm` | Success | Успешно | — | Привет, Алиса! / Алиса, 25 лет, 98.5 баллов / Сумма: 26 / Студент: Алиса, Активен: TRUE / Результат: 985% / Длина имени: 5 символов |
| `samples/expressions/interpolation_format.axm` | Success | Успешно | — | Цена: 1234.50 / Код: 00042 / Доля: 85.6 % / Большое число: 1,234,567 / Научная запись: 8.560E-001 / Hex: 2A / Имя вправо: \|     Алиса\| / Имя влево: \|А |
| `samples/expressions/logical/basic_logic.axm` | Success | Успешно | — | FALSE / TRUE / TRUE |
| `samples/expressions/numeric_literals/decimal_out_of_range_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2018 | Без текстового вывода |
| `samples/expressions/numeric_literals/leading_zero_error.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB0003 | Без текстового вывода |
| `samples/expressions/numeric_literals/long_out_of_range_error.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB0004 | Без текстового вывода |
| `samples/expressions/string_concat/basic_concat.axm` | Success | Успешно | — | Hello, world |
| `samples/functions/abs/basic_usage.axm` | Success | Успешно | — | 5 / 10.5 |
| `samples/functions/acos/basic_usage.axm` | Success | Успешно | — | 0 / 1047 |
| `samples/functions/acos/out_of_range_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/acosh/basic_usage.axm` | Success | Успешно | — | 0 / 1317 |
| `samples/functions/acosh/less_than_one_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/appendfile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / Привет! |
| `samples/functions/appendfile/explicit_1251_append.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE / Ая |
| `samples/functions/arrayaverage/audit_large_finite_mean.axm` | Success | Успешно | — | Среднее должно равняться 1E+308: / 1E+308 |
| `samples/functions/arrayaverage/basic_usage.axm` | Success | Успешно | — | 4.25 |
| `samples/functions/arrayaverage/decimal_mean.axm` | Success | Успешно | — | 0.5 / TRUE |
| `samples/functions/arrayaverage/tiny_cancellation.axm` | Success | Успешно | — | Малый остаток сохранён: TRUE / Перестановка сохраняет результат: TRUE / 3.33333333333333E-309 |
| `samples/functions/arraybinarysearch/basic_usage.axm` | Success | Успешно | — | 6 / 9 / 4 |
| `samples/functions/arraybinarysearch/multidimensional_coordinates.axm` | Success | Успешно | — | 1 / 2 / 1 / 0 |
| `samples/functions/arraybounds/multidimensional_shape.axm` | Success | Успешно | — | 1 / 2 / 10 / 11 |
| `samples/functions/arrayclone/basic_usage.axm` | Success | Успешно | — | 5 / 6 / 2 / 10 / 99 / 20 / 20 |
| `samples/functions/arrayclone/unallocated_and_empty.axm` | Success | Успешно | — | FALSE / TRUE / 0 / 1 / 0 |
| `samples/functions/arrayconcat/basic_usage.axm` | Success | Успешно | — | 1 / 4 / 4 / 10 / 99 / 20 / 30 / 40 |
| `samples/functions/arrayconcat/by_column.axm` | Success | Успешно | — | 1 / 2 / 10 / 13 / 120 / 221 / 110 |
| `samples/functions/arrayconcat/maximum_index.axm` | Success | Успешно | — | Верхняя граница: 2147483647 / Последнее значение: 42 / Ошибка границы: MB3007 |
| `samples/functions/arraycontains/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/arraycontains/multidimensional_values.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/arraycount/basic_usage.axm` | Success | Успешно | — | 3 / 0 |
| `samples/functions/arraycount/multidimensional_values.axm` | Success | Успешно | — | 3 / 2 |
| `samples/functions/arraydistinct/basic_usage.axm` | Success | Успешно | — | 1 / 3 / 99 / 20 / 30 / 10 |
| `samples/functions/arraydistinct/multidimensional_values.axm` | Success | Успешно | — | 1 / 4 / 4 / 5 / 7 / 9 / 11 |
| `samples/functions/arrayevery/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE |
| `samples/functions/arrayevery/multidimensional_slice.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/arrayfill/basic_usage.axm` | Success | Успешно | — | 42 / 42 / 42 |
| `samples/functions/arrayfilter/basic_usage.axm` | Success | Успешно | — | 1 / 2 / 2 / 30 / 40 / 1 / 0 / 0 |
| `samples/functions/arrayfilter/multidimensional_values.axm` | Success | Успешно | — | 1 / 3 / 3 / 21 / 41 / 61 |
| `samples/functions/arrayfindall/basic_usage.axm` | Success | Успешно | — | TRUE / 1 / 3 / -3 / -1 / 1 / TRUE / 1 / 0 |
| `samples/functions/arrayfindall/multidimensional_coordinates.axm` | Success | Успешно | — | 1 / 3 / 1 / 2 / 1 / 6 / 2 / 6 / 2 / 7 / 1 / 0 / 1 / 2 |
| `samples/functions/arrayflatten/by_visible_order.axm` | Success | Успешно | — | 1 / 6 / 3 / 1 / 2 / 6 / 4 / 5 |
| `samples/functions/arrayindexof/basic_usage.axm` | Success | Успешно | — | 1 / -1 |
| `samples/functions/arrayindexof/multidimensional_coordinates.axm` | Success | Успешно | — | 1 / 6 / 1 / 0 |
| `samples/functions/arrayindexof/negative_missing_index.axm` | Success | Успешно | — | Индексы отсутствующего значения, ожидается -5: / -5 / -5 / -5 / Первое и последнее совпадения, ожидается -4 и -3: / -4 / -3 |
| `samples/functions/arrayinsert/basic_usage.axm` | Success | Успешно | — | 10 / 20 / 30 / 40 / 5 / 8 |
| `samples/functions/arrayinsert/empty_dimension.axm` | Success | Успешно | — | Границы строк после вставки: / 1 / 1 / Границы столбцов остались 7 и 8: / 7 / 8 / Вставленные значения: / 42 / 43 |
| `samples/functions/arrayinsert/multidimensional_by_column.axm` | Success | Успешно | — | 10 / 12 / 110 / 115 / 111 / 210 / 215 / 211 |
| `samples/functions/arrayisallocated/basic_usage.axm` | Success | Успешно | — | FALSE / TRUE |
| `samples/functions/arraylastindexof/basic_usage.axm` | Success | Успешно | — | -1 / -4 |
| `samples/functions/arraylastindexof/multidimensional_coordinates.axm` | Success | Успешно | — | 2 / 7 / 1 / 0 |
| `samples/functions/arraylength/basic_usage.axm` | Success | Успешно | — | 0 / 3 |
| `samples/functions/arraymap/basic_usage.axm` | Success | Успешно | — | 5 / 8 / 4 / 30 / 60 / 90 / 120 / 100 |
| `samples/functions/arraymap/catch_callback_error.axm` | Success; предупреждений: 1 | Успешно | — | Ошибка преобразования: / MB3001 / Число вызовов, ожидается 2: / 2 / Третий исходный элемент остался 3: / 3 |
| `samples/functions/arraymap/maximum_callback_index.axm` | Success | Успешно | — | TRUE / FALSE / 42 / 84 / 50 |
| `samples/functions/arraymap/multidimensional_slice.axm` | Success | Успешно | — | 2 / 3 / 5 / 5 / N20 / N40 |
| `samples/functions/arraymap/polymorphic_transform.axm` | Success | Успешно | — | 5 / 7 / item:10 / item:20 / item:30 |
| `samples/functions/arraymax/basic_usage.axm` | Success | Успешно | — | 40 |
| `samples/functions/arraymin/basic_usage.axm` | Success | Успешно | — | 3 |
| `samples/functions/arraymin/unsupported_singleton.axm` | Success | Успешно | — | Минимум отклонён: MB3007 / Максимум отклонён: MB3007 |
| `samples/functions/arraypop/basic_usage.axm` | Success | Успешно | — | 30 / 5 / 6 / 2 / 10 / 20 |
| `samples/functions/arraypop/multidimensional_by_column.axm` | Success | Успешно | — | 1 / 2 / 112 / 212 / 10 / 11 |
| `samples/functions/arraypush/basic_usage.axm` | Success | Успешно | — | 5 / 7 / 3 / 10 / 20 / 30 |
| `samples/functions/arraypush/multidimensional_by_column.axm` | Success | Успешно | — | 10 / 12 / 112 / 212 |
| `samples/functions/arraypush/upper_bound_overflow.axm` | Success | Успешно | — | MB3007 / 2147483647 / 42 |
| `samples/functions/arrayrank/basic_usage.axm` | Success | Успешно | — | 2 / 1 |
| `samples/functions/arrayreduce/basic_usage.axm` | Success | Успешно | — | 68 / 95 |
| `samples/functions/arrayreduce/multidimensional_slice.axm` | Success | Успешно | — | [10][20][30][40] / [20][40] |
| `samples/functions/arrayreduce/polymorphic_accumulator.axm` | Success | Успешно | — | 10 |
| `samples/functions/arrayremove/basic_usage.axm` | Success | Успешно | — | 20 / 10 / 30 / 40 / 5 / 7 |
| `samples/functions/arrayremove/multidimensional_by_column.axm` | Success | Успешно | — | 111 / 211 / 10 / 11 / 110 / 112 / 210 / 212 |
| `samples/functions/arrayreshape/from_sorted_flat.axm` | Success | Успешно | — | 1 / 2 / 10 / 12 / 1 / 2 / 3 / 4 / 5 / 6 |
| `samples/functions/arrayreshape/matrix_to_matrix.axm` | Success | Успешно | — | Значения новой таблицы: / 11 / 12 / 13 / 21 / 22 / 23 / Исходное значение осталось 11: / 11 |
| `samples/functions/arrayreverse/basic_usage.axm` | Success | Успешно | — | 5 / 8 / 40 / 10 |
| `samples/functions/arrayreverse/by_column.axm` | Success | Успешно | — | 112 / 111 / 110 / 212 / 211 / 210 |
| `samples/functions/arrayskip/basic_usage.axm` | Success | Успешно | — | 1 / 2 / 99 / 30 / 20 |
| `samples/functions/arrayskip/by_row.axm` | Success | Успешно | — | 2 / 4 / 10 / 11 / 211 / 310 |
| `samples/functions/arrayskip/empty_at_max_index.axm` | Success | Успешно | — | Элементов осталось: 0 / Ранг сохранён: 2 / Нижняя пустая граница: 1 / Верхняя пустая граница: 0 / Другое измерение сохранено: TRUE |
| `samples/functions/arrayslice/basic_usage.axm` | Success | Успешно | — | 1 / 2 / 2 / 20 / 99 / 30 / 30 |
| `samples/functions/arrayslice/by_column.axm` | Success | Успешно | — | 1 / 2 / 11 / 12 / 999 / 212 / 111 |
| `samples/functions/arrayslice/zero_count.axm` | Success | Успешно | — | TRUE / 0 / 1 / 0 |
| `samples/functions/arraysome/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/arraysome/multidimensional_slice.axm` | Success | Успешно | — | FALSE / TRUE |
| `samples/functions/arraysort/basic_usage.axm` | Success | Успешно | — | 5 / 9 / 10 / 30 |
| `samples/functions/arraysort/maximum_line_index.axm` | Success | Успешно | — | Отсортированная копия, ожидается 10 и 30: / 10 / 30 / Первое исходное значение осталось 30: / 30 / Исходный массив после сортировки, ожидается 10 и 30 |
| `samples/functions/arraysort/multidimensional_by_dimension.axm` | Success | Успешно | — | 2 / 4 / 6 / 1 / 3 / 5 |
| `samples/functions/arraysort/reject_invalid_axis.axm` | Success | Успешно | — | Неверное измерение: MB3007 / Первое значение после отказа: 30 / Первое значение после сортировки: 10 |
| `samples/functions/arraysum/audit_checked_overflow.axm` | Success | Успешно | — | Поймана ошибка: MB3008 / Обработка завершена. / Программа продолжает работу. |
| `samples/functions/arraysum/audit_scalar_result.axm` | Success | Успешно | — | Сумма должна равняться пяти: 5 / Минимум равен двум: 2 / Максимум равен трём: 3 / Сумма, увеличенная на единицу: 6 |
| `samples/functions/arraysum/basic_usage.axm` | Success | Успешно | — | 150 |
| `samples/functions/arraysum/multidimensional_totals.axm` | Success | Успешно | — | Сумма равна десяти: 10 / Минимум равен единице: 1 / Максимум равен четырём: 4 / Среднее равно двум с половиной: 2.5 |
| `samples/functions/arraytake/basic_usage.axm` | Success | Успешно | — | 1 / 2 / 99 / 20 / 10 |
| `samples/functions/arraytake/by_row.axm` | Success | Успешно | — | 1 / 2 / 10 / 11 / 211 / 110 |
| `samples/functions/arraytake/explicit_axis.axm` | Success | Успешно | — | Первое значение: 10 / Неверное измерение: MB3007 / Исходное значение сохранено: 10 |
| `samples/functions/arraytoreversed/basic_usage.axm` | Success | Успешно | — | 1 / 3 / 99 / 20 / 10 / 30 |
| `samples/functions/arraytoreversed/by_column.axm` | Success | Успешно | — | 1 / 2 / 10 / 12 / 112 / 111 / 110 / 212 |
| `samples/functions/arraytosorted/basic_usage.axm` | Success | Успешно | — | 5 / 9 / 10 / 10 / 20 / 20 / 30 |
| `samples/functions/arraytosorted/multidimensional_by_dimension.axm` | Success | Успешно | — | 4 / 3 / 1 / 2 |
| `samples/functions/asc/basic_usage.axm` | Success | Успешно | — | 65 / 159 / 219 / 128512 |
| `samples/functions/asc/empty_text_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/ascw/empty_text_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/ascw/scalar_count_loop.axm` | Success | Успешно | — | Длина в единицах UTF-16: 11 / Число Unicode-кодов: 7 / Исходная строка восстановлена: TRUE / Первый Unicode-код равен всему эмодзи: FALSE |
| `samples/functions/ascw/unicode_codepoint.axm` | Success | Успешно | — | 1071 / 9608 / 128512 |
| `samples/functions/ascw/unicode_round_trip.axm` | Success | Успешно | — | Код Я в CP866: 159 / Код Я в Unicode: 1071 / Символ é сохранён: TRUE / Код первого эмодзи: 128512 / Длина эмодзи в UTF-16: 2 |
| `samples/functions/asin/basic_usage.axm` | Success | Успешно | — | 0 / 524 |
| `samples/functions/asin/out_of_range_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/asinh/basic_usage.axm` | Success | Успешно | — | 0 / 881 |
| `samples/functions/atanh/basic_usage.axm` | Success | Успешно | — | 0 / 549 |
| `samples/functions/atanh/out_of_range_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/atn/basic_usage.axm` | Success | Успешно | — | 0 / 0.7853981633974483 |
| `samples/functions/atn2/basic_usage.axm` | Success | Успешно | — | 0 / 0.7853981633974483 |
| `samples/functions/cbrt/basic_usage.axm` | Success | Успешно | — | 3 / -2 |
| `samples/functions/cdbl/basic_usage.axm` | Success | Успешно | — | 3.5 |
| `samples/functions/cdbl/decimal_precision.axm` | Success | Успешно | — | Простое преобразование: 2 / После перехода в DOUBLE: 9007199254740992 / Исходное число сохранено: TRUE / Минимум после явного выбора типа: 2 |
| `samples/functions/cdec/basic_usage.axm` | Success | Успешно | — | 0.3 |
| `samples/functions/cdec/fixed_decimal_parser.axm` | Success | Успешно | — | Целое число: 9007199254740993 / Длинная дробь: 1.0000000000000000000000000001 / Отрицательное число: -0.1234567890123456789012345678 / Ограничение раз |
| `samples/functions/ceil/basic_usage.axm` | Success | Успешно | — | 4 / -3 |
| `samples/functions/center/basic_usage.axm` | Success | Успешно | — | [ A ] / [.A..] / [.AB..] |
| `samples/functions/center/empty_fill_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/changedir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/changedir/program_directory_scope.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE / Проверка рабочей папки / TRUE / TRUE / TRUE / TRUE |
| `samples/functions/chr/basic_usage.axm` | Success | Успешно | — | A / Я / █ / 128512 |
| `samples/functions/chr/invalid_code_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/chrw/invalid_code_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/chrw/unicode_character.axm` | Success | Успешно | — | Я / █ / 128512 |
| `samples/functions/cint/audit_truncation_boundary.axm` | Success | Успешно | — | 2147483647 / -2147483648 |
| `samples/functions/cint/basic_usage.axm` | Success | Успешно | — | 2 / -2 |
| `samples/functions/cint/nonfinite_catch.axm` | Success | Успешно | — | Поймана ошибка преобразования: MB3008 / Обработка завершена. / Программа продолжает работу. |
| `samples/functions/clamp/basic_usage.axm` | Success | Успешно | — | 1 / 5 / 10 |
| `samples/functions/clamp/invalid_bounds_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/clng/audit_upper_boundary.axm` | Success | Успешно | — | Проверяем верхнюю исключённую границу LONG. / Поймана ошибка: MB3008 / Проверка завершена. |
| `samples/functions/clng/basic_usage.axm` | Success | Успешно | — | 4000000000 |
| `samples/functions/combinepath/basic_usage.axm` | Success | Успешно | — | folder\notes.txt |
| `samples/functions/contains/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/copydir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / Hello |
| `samples/functions/copydir/reject_nested_destination.axm` | Success | Успешно | — | TRUE / Ожидается MB3007:  MB3007 / Вложенная копия не создана:  TRUE / Исходный текст сохранён:  TRUE / TRUE |
| `samples/functions/copyfile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / Привет |
| `samples/functions/cos/basic_usage.axm` | Success | Успешно | — | 1 / 0.5403023058681398 |
| `samples/functions/cosh/basic_usage.axm` | Success | Успешно | — | 1 / 1543 |
| `samples/functions/currentdir/basic_usage.axm` | Success | Успешно | — | C:\Axiom\.artifacts\samples-electron\workspace\samples\functions\currentdir / TRUE |
| `samples/functions/date/basic_usage.axm` | Success | Успешно | — | 2026-10-10 |
| `samples/functions/date/named_timezone_current_date.axm` | Success | Успешно | — | TRUE |
| `samples/functions/dateadd/large_time_shift.axm` | Success | Успешно | — | После большого сдвига часов ожидается 06:59:59: / 06:59:59 / После отрицательного сдвига от полуночи ожидается 16:00:00: / 16:00:00 |
| `samples/functions/dateadd/month_end_shift.axm` | Success | Успешно | — | 2026-02-28 |
| `samples/functions/dateadd/typed_temporal_values.axm` | Success | Успешно | — | 2026-02-28 / 14:06:59 / 2026-04-21 01:05:06 / 2 / 1 / 1 / 1 / 14 / 110 |
| `samples/functions/datediff/day_boundary.axm` | Success | Успешно | — | 1 |
| `samples/functions/datediff/mixed_temporal_kinds_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2003 | Без текстового вывода |
| `samples/functions/datepart/day_of_year.axm` | Success | Успешно | — | 111 |
| `samples/functions/dateserial/basic_usage.axm` | Success | Успешно | — | 2026-04-20 |
| `samples/functions/datevalue/from_datetime.axm` | Success | Успешно | — | 2026-04-20 |
| `samples/functions/day/basic_usage.axm` | Success | Успешно | — | 20 |
| `samples/functions/daysinmonth/february_leap_year.axm` | Success | Успешно | — | 29 |
| `samples/functions/deletedir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / FALSE / FALSE |
| `samples/functions/deletedirrecursive/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / FALSE |
| `samples/functions/deletefile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / FALSE / FALSE |
| `samples/functions/dirdate/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/direxists/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/dirname/basic_usage.axm` | Success | Успешно | — | TRUE |
| `samples/functions/dirsize/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / 6 |
| `samples/functions/dirsize/ordinary_nested_bytes.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / Суммарный размер, ожидается 6:  6 / TRUE |
| `samples/functions/driveformat/basic_usage.axm` | Success | Успешно | — | NTFS |
| `samples/functions/drivename/basic_usage.axm` | Success | Успешно | — | C:\ |
| `samples/functions/drivetype/basic_usage.axm` | Success | Успешно | — | Fixed |
| `samples/functions/endofmonth/leap_february.axm` | Success | Успешно | — | 2024-02-29 |
| `samples/functions/endofyear/from_datetime.axm` | Success | Успешно | — | 2026-12-31 |
| `samples/functions/endswith/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/eof/basic_usage.axm` | Success | Успешно | — | TRUE / [A] / [B] |
| `samples/functions/exp/basic_usage.axm` | Success | Успешно | — | 1 / 2.718281828459045 |
| `samples/functions/extname/basic_usage.axm` | Success | Успешно | — | .txt / [] |
| `samples/functions/fileaccesseddate/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/filecreatedate/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/filedate/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE |
| `samples/functions/fileexists/basic_usage.axm` | Success | Успешно | — | FALSE / TRUE / TRUE |
| `samples/functions/filelength/basic_usage.axm` | Success | Успешно | — | 0 / TRUE / 5 |
| `samples/functions/filemode/basic_usage.axm` | Success | Успешно | — | APPEND |
| `samples/functions/filename/basic_usage.axm` | Success | Успешно | — | notes.txt |
| `samples/functions/filepath/basic_usage.axm` | Success | Успешно | — | C:\Users\comp\AppData\Local\Temp\filepath_demo_m4yjblns.ia3 / C:\Users\comp\AppData\Local\Temp\filepath_demo_m4yjblns.ia3 |
| `samples/functions/filepos/basic_usage.axm` | Success | Успешно | — | TRUE / 0 / alpha / 7 |
| `samples/functions/filesize/basic_usage.axm` | Success | Успешно | — | TRUE / 12 |
| `samples/functions/fix/basic_usage.axm` | Success | Успешно | — | 3 / -3 / -4 |
| `samples/functions/floor/basic_usage.axm` | Success | Успешно | — | 3 / -4 |
| `samples/functions/flush/basic_usage.axm` | Success | Успешно | — | TRUE / 0 / TRUE / 1 / A |
| `samples/functions/freespace/basic_usage.axm` | Success | Успешно | — | TRUE |
| `samples/functions/fromutc/named_timezone_from_utc.axm` | Success | Успешно | — | TRUE |
| `samples/functions/fromutc/roundtrip_to_utc.axm` | Success | Успешно | — | TRUE / 2026-04-20 11:05:06 |
| `samples/functions/fullpath/basic_usage.axm` | Success | Успешно | — | TRUE |
| `samples/functions/fullpath/drive_relative_scope.axm` | Success | Успешно | — | Переход во временную папку: TRUE / Путь с буквой диска использует выбранную папку: TRUE / Путь от корня использует правильный корень: TRUE / Переход ч |
| `samples/functions/fullpath/null_character.axm` | Success | Успешно | — | Ошибка пути: MB3007 / Программа продолжает работу |
| `samples/functions/getdirs/basic_usage.axm` | Success; предупреждений: 1 | Успешно | — | 1 / data / TRUE / 0 |
| `samples/functions/getfiles/basic_usage.axm` | Success; предупреждений: 1 | Успешно | — | 1 / notes.txt / TRUE / 0 |
| `samples/functions/hour/basic_usage.axm` | Success | Успешно | — | 14 |
| `samples/functions/imageheight/loaded_resource_property.axm` | Success | Успешно | — | Результат IMAGEHEIGHT: / 32 |
| `samples/functions/imagewidth/loaded_resource_property.axm` | Success | Успешно | — | Результат IMAGEWIDTH: / 32 |
| `samples/functions/inkey/poll_keys.axm` | Success | Успешно | — | Щёлкните по панели вывода и нажмите LEFT. / LEFT; Клавиши: ArrowLeft |
| `samples/functions/insert/basic_usage.axm` | Success | Успешно | — | <abcd / ab-cd / abcd! |
| `samples/functions/insert/invalid_start_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/instr/basic_usage.axm` | Success | Успешно | — | 3 / 5 / 0 |
| `samples/functions/instr/empty_fragment.axm` | Success | Успешно | — | 1 / 4 / 0 |
| `samples/functions/instr/invalid_start_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/instr/utf16_positions.axm` | Success | Успешно | — | Длина строки в единицах UTF-16, ожидается 4: / 4 / Позиция буквы Б, ожидается 4: / 4 / Найденный эмодзи: / 😀 |
| `samples/functions/instrrev/basic_usage.axm` | Success | Успешно | — | 5 / 0 |
| `samples/functions/instrrev/empty_fragment.axm` | Success | Успешно | — | 4 |
| `samples/functions/instrrev/invalid_start_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/instrrev/start_position.axm` | Success | Успешно | — | 3 / 0 / 5 |
| `samples/functions/int/basic_usage.axm` | Success | Успешно | — | 12 |
| `samples/functions/int/overflow_error.axm` | Success | Ожидаемая ошибка выполнения | MB3008 | Без текстового вывода |
| `samples/functions/isabsolutepath/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE |
| `samples/functions/isabsolutepath/environment_expansion.axm` | Success | Успешно | — | Соединение без раскрытия: %TEMP%\данные.txt / Полный путь: C:\Users\comp\AppData\Local\Temp\данные.txt / Абсолютность после раскрытия: TRUE |
| `samples/functions/isdate/canonical_datetime.axm` | Success | Успешно | — | TRUE |
| `samples/functions/ishidden/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/isleapyear/leap_year_true_false.axm` | Success | Успешно | — | TRUE |
| `samples/functions/isnumeric/full_number_check.axm` | Success | Успешно | — | Строка 12abc является числом целиком: FALSE / Числовой префикс той же строки: 12 / Настоящий ноль допустим: TRUE / Переполнение недопустимо: FALSE / Д |
| `samples/functions/isopen/basic_usage.axm` | Success | Успешно | — | FALSE / TRUE / FALSE |
| `samples/functions/isreadonly/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/join/basic_usage.axm` | Success | Успешно | — | red/green/blue |
| `samples/functions/keydown/canonical_aliases.axm` | Success | Успешно | — | Удерживайте CTRL+PAGEUP. / CONTROL+PAGE UP / готово; Клавиши: Control+PageUp |
| `samples/functions/keydown/held_modifiers.axm` | Success | Успешно | — | Удерживайте CTRL+LEFT. / CTRL+LEFT / готово; Клавиши: Control+ArrowLeft |
| `samples/functions/keypress/modified_keypress.axm` | Success | Успешно | — | Удерживайте CTRL и нажмите LEFT. / CTRL+LEFT / готово; Клавиши: Control+ArrowLeft |
| `samples/functions/keypress/single_press.axm` | Success | Успешно | — | Нажмите LEFT один раз. / LEFT нажата / FALSE / готово; Клавиши: ArrowLeft |
| `samples/functions/lbound/basic_usage.axm` | Success | Успешно | — | 5 |
| `samples/functions/lcase/basic_usage.axm` | Success | Успешно | — | hello |
| `samples/functions/left/basic_usage.axm` | Success | Успешно | — | Hel |
| `samples/functions/left/negative_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/left/unicode_units.axm` | Success | Успешно | — | Первый составной символ: é / Первый Unicode-код: e |
| `samples/functions/left/zero_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/len/basic_usage.axm` | Success | Успешно | — | 5 |
| `samples/functions/len/unicode_units.axm` | Success | Успешно | — | Единицы UTF-16: 11 / Unicode-коды: 7 / Целые составные символы: 1 |
| `samples/functions/len/wrong_arity_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2004 | Без текстового вывода |
| `samples/functions/linenumber/basic_usage.axm` | Success | Успешно | — | TRUE / 1 / alpha / 2 / TRUE / 3 / TRUE |
| `samples/functions/linenumber/utf16_seek_recovery.axm` | Success | Успешно | — | TRUE / TRUE / Ожидаемая ошибка позиции: MB3007 / Позиция после отказа: 1 / TRUE / А / Следующая строка: 2 / TRUE / TRUE |
| `samples/functions/loadimage/loaded_resource_property.axm` | Success | Успешно | — | Результат LOADIMAGE: / 32 |
| `samples/functions/loadsprite/loaded_resource_property.axm` | Success | Успешно | — | Результат LOADSPRITE: / 4 |
| `samples/functions/loadtilemap/invalid_dimensions.axm` | Success | Успешно | — | Ошибка размеров карты: / MB3005 / LOADTILEMAP: Поле width должно быть неотрицательным. / Сохранена прежняя карта: / 1 |
| `samples/functions/loadtilemap/loaded_resource_property.axm` | Success | Успешно | — | Результат LOADTILEMAP: / 2 |
| `samples/functions/loadtilemap/loader_boundaries.axm` | Success | Успешно | — | Тайлсет и тайл: / 2 / 1 / Тайл и флаги после очистки: / 1 / 0 / Отказ для начала параллакса: / MB3005 / LOADTILEMAP: Поле parallaxoriginx карты поддер |
| `samples/functions/loadtilemap/metadata_validation.axm` | Success | Успешно | — | Слои карты: / 1 / Неподдержанный объект: / MB3005 / LOADTILEMAP: Объекты ellipse пока не поддерживаются. |
| `samples/functions/loadtilemap/nonfinite_layer.axm` | Success | Успешно | — | MB3005 / LOADTILEMAP: Поле offsetx слоя должно быть конечным числом DOUBLE. / 1 |
| `samples/functions/loadtilemap/unsupported_blend.axm` | Success | Успешно | — | MB3005 / LOADTILEMAP: Поле mode слоя поддерживает только строку normal. / 1 |
| `samples/functions/loadtilemap/unsupported_tileset_metadata.axm` | Success | Успешно | — | MB3005 / LOADTILEMAP: Метаданные wangsets пока не поддерживаются. / 0 |
| `samples/functions/loadtileset/arithmetic_boundaries.axm` | Success | Успешно | — | Тайлов в атласе: / 1 / Ошибка отступа: / MB3005 / Сохранено тайлов: / 1 |
| `samples/functions/loadtileset/loaded_resource_property.axm` | Success | Успешно | — | Результат LOADTILESET: / 4 |
| `samples/functions/log/basic_usage.axm` | Success | Успешно | — | 0 / 1 |
| `samples/functions/log/non_positive_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/log10/basic_usage.axm` | Success | Успешно | — | 0 / 3 |
| `samples/functions/log2/basic_usage.axm` | Success | Успешно | — | 0 / 3 |
| `samples/functions/ltrim/basic_usage.axm` | Success | Успешно | — | hello   / hello |
| `samples/functions/makedir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE |
| `samples/functions/maketempdir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE |
| `samples/functions/maketempfile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE |
| `samples/functions/mapcolumn/map_access.axm` | Success | Успешно | — | Результат MAPCOLUMN: / 2 |
| `samples/functions/mapheight/loaded_resource_property.axm` | Success | Успешно | — | Результат MAPHEIGHT: / 2 |
| `samples/functions/maplayercount/loaded_resource_property.axm` | Success | Успешно | — | Результат MAPLAYERCOUNT: / 3 |
| `samples/functions/maplayername/map_access.axm` | Success | Успешно | — | Результат MAPLAYERNAME: / Земля |
| `samples/functions/maplayerproperty/map_access.axm` | Success | Успешно | — | Результат MAPLAYERPROPERTY: / стена |
| `samples/functions/maplayertype/map_access.axm` | Success | Успешно | — | Результат MAPLAYERTYPE: / OBJECT |
| `samples/functions/maplayervisible/map_access.axm` | Success | Успешно | — | Результат MAPLAYERVISIBLE: / FALSE |
| `samples/functions/mapobjectclass/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTCLASS: / portal |
| `samples/functions/mapobjectcount/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTCOUNT: / 2 |
| `samples/functions/mapobjectheight/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTHEIGHT: / 16 |
| `samples/functions/mapobjectkind/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTKIND: / POINT |
| `samples/functions/mapobjectname/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTNAME: / Выход |
| `samples/functions/mapobjectproperty/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTPROPERTY: / переход |
| `samples/functions/mapobjectvisible/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTVISIBLE: / FALSE |
| `samples/functions/mapobjectwidth/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTWIDTH: / 8 |
| `samples/functions/mapobjectx/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTX: / 12.5 |
| `samples/functions/mapobjecty/map_access.axm` | Success | Успешно | — | Результат MAPOBJECTY: / 24 |
| `samples/functions/mappixelheight/map_property.axm` | Success | Успешно | — | Высота карты в пикселях: / 32 |
| `samples/functions/mappixelwidth/map_property.axm` | Success | Успешно | — | Ширина карты в пикселях: / 32 |
| `samples/functions/mapproperty/map_access.axm` | Success | Успешно | — | Результат MAPPROPERTY: / выход😀 |
| `samples/functions/maprow/map_access.axm` | Success | Успешно | — | Результат MAPROW: / 1 |
| `samples/functions/maptile/loaded_resource_property.axm` | Success | Успешно | — | Результат MAPTILE: / 2 |
| `samples/functions/maptileflags/changed_cell_flags.axm` | Success | Успешно | — | Маски отражения клетки: / 0 / 3 / 0 |
| `samples/functions/maptileheight/map_property.axm` | Success | Успешно | — | Высота клетки в пикселях: / 16 |
| `samples/functions/maptileproperty/map_access.axm` | Success | Успешно | — | Результат MAPTILEPROPERTY: / TRUE |
| `samples/functions/maptileset/cell_tileset_index.axm` | Success | Успешно | — | Номера тайлсетов для трёх клеток: / 1 / 0 / 0 |
| `samples/functions/maptilesetcount/map_access.axm` | Success | Успешно | — | Результат MAPTILESETCOUNT: / 1 |
| `samples/functions/maptilewidth/map_property.axm` | Success | Успешно | — | Ширина клетки в пикселях: / 16 |
| `samples/functions/mapwidth/loaded_resource_property.axm` | Success | Успешно | — | Результат MAPWIDTH: / 2 |
| `samples/functions/mapworldx/map_access.axm` | Success | Успешно | — | Результат MAPWORLDX: / 116 |
| `samples/functions/mapworldy/map_access.axm` | Success | Успешно | — | Результат MAPWORLDY: / 216 |
| `samples/functions/max/basic_usage.axm` | Success | Успешно | — | 10 / 21474836479 |
| `samples/functions/mid/basic_usage.axm` | Success | Успешно | — | ello |
| `samples/functions/mid/invalid_start_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/mid/out_of_range_start_behavior.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/functions/mid/unicode_units.axm` | Success | Успешно | — | Второй составной символ: 👨‍👩‍👧‍👦 / Два символа из середины: 👨‍👩‍👧‍👦é |
| `samples/functions/mid/zero_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/min/basic_usage.axm` | Success | Успешно | — | 3 / 100 |
| `samples/functions/minute/basic_usage.axm` | Success | Успешно | — | 5 |
| `samples/functions/month/basic_usage.axm` | Success | Успешно | — | 4 |
| `samples/functions/monthname/calendar_month_name.axm` | Success | Успешно | — | April |
| `samples/functions/mousebutton/button_states.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/functions/mousepress/button_presses.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/functions/mousex/canvas_world_position.axm` | Success | Успешно | — | 312 / 99; Клавиши: ArrowLeft, Space, Escape |
| `samples/functions/mousex/current_position.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/functions/mousey/canvas_world_position.axm` | Success | Успешно | — | 99 / 312; Клавиши: ArrowLeft, Space, Escape |
| `samples/functions/mousey/current_position.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/functions/movedir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / FALSE / TRUE |
| `samples/functions/movedir/existing_destination.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE / Ожидается MB3007: MB3007 / Источник сохранён: TRUE / Назначение сохранено: TRUE / TRUE / TRUE / TRUE / TRUE / TRUE |
| `samples/functions/movefile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / FALSE / TRUE |
| `samples/functions/movefile/same_file_path.axm` | Success | Успешно | — | TRUE / Перенос по тому же пути: TRUE / Файл сохранился: TRUE / Копирование должно дать MB3007: MB3007 / Текст после отказа сохранён: TRUE / Временный  |
| `samples/functions/newline/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / alpha /  / beta /  / TRUE |
| `samples/functions/newline/utf16_byte_positions.axm` | Success | Успешно | — | Начальная позиция: 0 / TRUE / После NEWLINE должно быть 2: 2 / TRUE / После WRITELINE должно быть 4: 4 / Длина текста должна быть 4: 4 / Два перевода  |
| `samples/functions/now/basic_usage.axm` | Success | Успешно | — | 2026-10-10 21:58:23 |
| `samples/functions/now/named_timezone_current_datetime.axm` | Success | Успешно | — | TRUE |
| `samples/functions/padleft/basic_usage.axm` | Success | Успешно | — | [  7] / 007 / word |
| `samples/functions/padleft/negative_width_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/padright/basic_usage.axm` | Success | Успешно | — | [7  ] / 7.. / word |
| `samples/functions/padright/empty_fill_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/pathexists/basic_usage.axm` | Success | Успешно | — | FALSE / TRUE / TRUE / TRUE |
| `samples/functions/pathroot/basic_usage.axm` | Success | Успешно | — | C:\ |
| `samples/functions/pmap/invalid_selector_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/pmap/window_and_viewport_conversion.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/functions/point/canvas_pixel_read.axm` | Success | Успешно | — | 0 / 15 |
| `samples/functions/point/invalid_selector_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/point/physical_canvas_coordinates.axm` | Success | Успешно | — | Цвет физической точки (ожидается 6): 6 / Точка вне холста (ожидается 0): 0; Клавиши: ArrowLeft |
| `samples/functions/pow/basic_usage.axm` | Success | Успешно | — | 8 / 3 |
| `samples/functions/pow/invalid_domain_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/pow/rounding_boundary.axm` | Success | Успешно | — | Минимальная положительная степень: 5E-324 / Результат на середине в этой среде: 5E-324 / Результат в проверенном интервале: TRUE / Ещё меньшая степень |
| `samples/functions/readalllines/basic_usage.axm` | Success; предупреждений: 1 | Успешно | — | 2 / alpha / beta |
| `samples/functions/readalllines/carriage_return.axm` | Success | Успешно | — | TRUE / TRUE / 3 / А/Б/В / TRUE |
| `samples/functions/readalllines/empty_file.axm` | Success; предупреждений: 1 | Успешно | — | TRUE / 0 / 1 / 0 |
| `samples/functions/readalllines/explicit_utf16_lines.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / Первая 😀 / Вторая |
| `samples/functions/readalllines/utf8_bom.axm` | Success; предупреждений: 1 | Успешно | — | 1 / alpha |
| `samples/functions/readfile/basic_usage.axm` | Success | Успешно | — | TRUE / Привет, мир! |
| `samples/functions/readfile/explicit_encoding_choice.axm` | Success | Успешно | — | TRUE / TRUE / В UTF-8: é / Те же байты в WINDOWS-1251: Г© |
| `samples/functions/readfile/single_byte_round_trip.axm` | Success | Успешно | — | Количество позиций в строке: / 256 / Запись той же однобайтовой кодировкой: / TRUE / Строка после повторного чтения совпадает: / TRUE / Удаление време |
| `samples/functions/readfile/windows1251_fallback.axm` | Success | Успешно | — | Привет |
| `samples/functions/readline/basic_usage.axm` | Success | Успешно | — | TRUE / alpha / beta |
| `samples/functions/readlines/basic_usage.axm` | Success; предупреждений: 1 | Успешно | — | 2 / alpha / beta / 1 / gamma |
| `samples/functions/readlines/maximum_count.axm` | Success | Успешно | — | TRUE / Прочитано строк: 2 / Первая строка / Вторая строка / TRUE |
| `samples/functions/readlines/zero_and_end.axm` | Success; предупреждений: 1 | Успешно | — | TRUE / 0 / 1 / 0 / 1 / TRUE / 0 / 1 / 0 |
| `samples/functions/readtext/basic_usage.axm` | Success | Успешно | — | TRUE / AБ / 3 / В |
| `samples/functions/readtext/maximum_count.axm` | Success | Успешно | — | TRUE / А😀 / Прочитано байт: 6 / TRUE |
| `samples/functions/readtext/unicode_chunks.axm` | Success | Успешно | — | TRUE / Эмодзи: 😀 / Позиция после эмодзи должна быть 4: 4 / Следующая буква: А / Файл прочитан: TRUE / Временный файл удалён: TRUE |
| `samples/functions/readtoend/basic_usage.axm` | Success | Успешно | — | TRUE / alpha / beta |
| `samples/functions/regexcount/basic_usage.axm` | Success | Успешно | — | 2 / 2 / 0 |
| `samples/functions/regexextract/basic_usage.axm` | Success | Успешно | — | 12 / [] |
| `samples/functions/regexextract/explicit_group_number.axm` | Success | Успешно | — | Группа 10: 42 / Без учёта регистра: 42 / Нет группы 1: TRUE |
| `samples/functions/regexextract/pattern_backslash.axm` | Success | Успешно | — | Группа цифр: 42 |
| `samples/functions/regexextractall/explicit_group_values.axm` | Success | Успешно | — | Количество: 2 / Первое значение: 12 / Второе значение: 34 |
| `samples/functions/regexextractall/group_values.axm` | Success | Успешно | — | 2 / 12 / 34 / a / b |
| `samples/functions/regexextractall/negative_group_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/regexismatch/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/regexismatch/invalid_pattern_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/regexismatch/unknown_mode_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/regexmatch/basic_usage.axm` | Success | Успешно | — | 42 / [] |
| `samples/functions/regexmatches/all_matches.axm` | Success | Успешно | — | 2 / 42 / 100 / 0 |
| `samples/functions/regexreplace/basic_usage.axm` | Success | Успешно | — | one # two # / one # two 34 / A-12 B-34 |
| `samples/functions/regexreplace/negative_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/regexreplace/stop_at_count.axm` | Success | Успешно | — | Изменён только первый символ: TRUE |
| `samples/functions/regexsplit/split_on_pattern.axm` | Success | Успешно | — | 3 / one / two / three |
| `samples/functions/remove/basic_usage.axm` | Success | Успешно | — | aef / abcde / abcdef |
| `samples/functions/remove/negative_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/renamedir/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE |
| `samples/functions/renamefile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/repeat/basic_usage.axm` | Success | Успешно | — | hahaha |
| `samples/functions/repeat/negative_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/replace/basic_usage.axm` | Success | Успешно | — | one three three / One two |
| `samples/functions/replace/empty_old_text_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/replacemany/multiple_tokens.axm` | Success | Успешно | — | Y X z / one two |
| `samples/functions/reverse/audit_surrogate_parity.axm` | Success | Успешно | — | Сохранилась ли исходная строка: TRUE |
| `samples/functions/reverse/basic_usage.axm` | Success | Успешно | — | dcba |
| `samples/functions/rewind/basic_usage.axm` | Success | Успешно | — | TRUE / alpha / TRUE / 0 / alpha |
| `samples/functions/rgb/packed_channels.axm` | Success | Успешно | — | Результат RGB: / 66051 |
| `samples/functions/right/basic_usage.axm` | Success | Успешно | — | llo |
| `samples/functions/right/negative_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/right/out_of_range_count_behavior.axm` | Success | Успешно | — | Hello |
| `samples/functions/right/unicode_units.axm` | Success | Успешно | — | Флаг целиком: 🇷🇺 / Длина флага в Unicode-кодах: 2 |
| `samples/functions/right/zero_count_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/rnd/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / 0.2533958321374171 |
| `samples/functions/rnd/long_range_conversion.axm` | Success | Успешно | — | Случайное значение: 722011773 / Значение внутри границ: TRUE / Повторный посев дал то же значение: TRUE |
| `samples/functions/round/basic_usage.axm` | Success | Успешно | — | 3 / -3 / 2 |
| `samples/functions/rtrim/basic_usage.axm` | Success | Успешно | — | hello / hello |
| `samples/functions/screenheight/text_mode_state.axm` | Success | Успешно | — | Результат SCREENHEIGHT: / 0 |
| `samples/functions/screenisactive/text_mode_state.axm` | Success | Успешно | — | Результат SCREENISACTIVE: / FALSE |
| `samples/functions/screenmode/text_mode_state.axm` | Success | Успешно | — | Результат SCREENMODE: / 0 |
| `samples/functions/screenwidth/text_mode_state.axm` | Success | Успешно | — | Результат SCREENWIDTH: / 0 |
| `samples/functions/second/basic_usage.axm` | Success | Успешно | — | 6 |
| `samples/functions/seekfile/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / ABZDE |
| `samples/functions/seekfile/multibyte_overwrite.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / Строгое чтение должно сообщить MB3007: MB3007 / TRUE / После полной записи должно быть AZB: AZB / Временный файл удалён: TRUE |
| `samples/functions/setsign/basic_usage.axm` | Success | Успешно | — | -5 / 5 / 10.5 |
| `samples/functions/setsign/signed_zero.axm` | Success | Успешно | — | Нули равны: TRUE / Положительный знак: 3 / Отрицательный знак: -3 / Угол ниже оси: TRUE / Угол выше оси: TRUE |
| `samples/functions/sgn/basic_usage.axm` | Success | Успешно | — | -1 / 0 / 1 |
| `samples/functions/sin/basic_usage.axm` | Success | Успешно | — | 0 / 0.8414709848078965 |
| `samples/functions/sinh/basic_usage.axm` | Success | Успешно | — | 0 / 1175 |
| `samples/functions/skipline/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / beta / TRUE |
| `samples/functions/space/basic_usage.axm` | Success | Успешно | — | [    ] |
| `samples/functions/split/basic_usage.axm` | Success | Успешно | — | 3 / red / green / blue |
| `samples/functions/split/empty_text.axm` | Success | Успешно | — | TRUE / 0 / 1 / 0 |
| `samples/functions/split/unicode_empty_parts.axm` | Success | Успешно | — | Количество частей, ожидается 5: / 5 / Соединённые части, ожидается \|А\|\|Б\|: / \|А\|\|Б\| |
| `samples/functions/spritecount/loaded_resource_property.axm` | Success | Успешно | — | Результат SPRITECOUNT: / 4 |
| `samples/functions/spriteheight/loaded_resource_property.axm` | Success | Успешно | — | Результат SPRITEHEIGHT: / 16 |
| `samples/functions/spritewidth/loaded_resource_property.axm` | Success | Успешно | — | Результат SPRITEWIDTH: / 16 |
| `samples/functions/sqr/basic_usage.axm` | Success | Успешно | — | 3 / 1.5 |
| `samples/functions/sqr/negative_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/sqr/scaled_distance.axm` | Success | Успешно | — | Большое расстояние: 4.9999999999999995E+200 / Малое расстояние: 5E-200 / Нулевой вектор: 0 |
| `samples/functions/startofmonth/from_datetime.axm` | Success | Успешно | — | 2026-04-01 |
| `samples/functions/startofyear/from_datetime.axm` | Success | Успешно | — | 2026-01-01 |
| `samples/functions/startswith/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / TRUE |
| `samples/functions/str/basic_usage.axm` | Success | Успешно | — | Value = 42 |
| `samples/functions/string/basic_usage.axm` | Success | Успешно | — | aaaaa |
| `samples/functions/string/empty_fill_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/substrcount/basic_usage.axm` | Success | Успешно | — | 2 / 2 / 0 |
| `samples/functions/substrcount/empty_fragment_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/functions/tan/basic_usage.axm` | Success | Успешно | — | 0 / 0.5463024898437905 |
| `samples/functions/tanh/basic_usage.axm` | Success | Успешно | — | 0 / 762 |
| `samples/functions/textheight/multiline_label.axm` | Success | Успешно | — | 24; Клавиши: ArrowLeft |
| `samples/functions/textwidth/hud_metrics.axm` | Success | Успешно | — | 72; Клавиши: ArrowLeft |
| `samples/functions/tilecount/atlas_property.axm` | Success | Успешно | — | Число тайлов: / 4 |
| `samples/functions/tileheight/atlas_property.axm` | Success | Успешно | — | Высота тайла в пикселях: / 16 |
| `samples/functions/tilewidth/atlas_property.axm` | Success | Успешно | — | Ширина тайла в пикселях: / 16 |
| `samples/functions/time/basic_usage.axm` | Success | Успешно | — | 21:58:44 |
| `samples/functions/time/named_timezone_current_time.axm` | Success | Успешно | — | TRUE |
| `samples/functions/timer/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / TRUE |
| `samples/functions/timeserial/basic_usage.axm` | Success | Успешно | — | 14:05:06 |
| `samples/functions/timevalue/from_datetime.axm` | Success | Успешно | — | 14:05:06 |
| `samples/functions/totalfreespace/basic_usage.axm` | Success | Успешно | — | TRUE |
| `samples/functions/totalspace/basic_usage.axm` | Success | Успешно | — | TRUE |
| `samples/functions/toutc/named_timezone_to_utc.axm` | Success | Успешно | — | TRUE |
| `samples/functions/toutc/roundtrip_to_local.axm` | Success | Успешно | — | TRUE / 2026-04-20 14:05:06 |
| `samples/functions/trim/basic_usage.axm` | Success | Успешно | — | hello / hello / hello / hello |
| `samples/functions/ubound/basic_usage.axm` | Success | Успешно | — | 7 |
| `samples/functions/ucase/basic_usage.axm` | Success | Успешно | — | HELLO |
| `samples/functions/utcdate/current_utc_date.axm` | Success | Успешно | — | 2026-10-10 |
| `samples/functions/utcnow/current_utc_now.axm` | Success | Успешно | — | 2026-10-10 18:58:45 |
| `samples/functions/utcoffset/current_local_offset.axm` | Success | Успешно | — | 180 |
| `samples/functions/utcoffset/named_timezone_specific_offset.axm` | Success | Успешно | — | TRUE |
| `samples/functions/utcoffset/specific_local_datetime_offset.axm` | Success | Успешно | — | TRUE |
| `samples/functions/utctime/current_utc_time.axm` | Success | Успешно | — | 18:58:46 |
| `samples/functions/val/basic_usage.axm` | Success | Успешно | — | 123.5 |
| `samples/functions/val/finite_results.axm` | Success | Успешно | — | Нечисловое значение даёт ноль: 0 / Бесконечность даёт ноль: 0 / Переполнение даёт ноль: 0 / Числовое начало равно двенадцати: 12 / Экспонента даёт сто |
| `samples/functions/val/invalid_text_behavior.axm` | Success | Успешно | — | 0 |
| `samples/functions/weekday/basic_usage.axm` | Success | Успешно | — | 1 |
| `samples/functions/weekdayname/weekend_name.axm` | Success | Успешно | — | Sunday |
| `samples/functions/write/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / left-right |
| `samples/functions/write/strict_encoding_recovery.axm` | Success | Успешно | — | TRUE / Ожидаемая ошибка записи: MB3007 / Позиция: 1 / TRUE / Сохранённый текст: Ая / TRUE / TRUE |
| `samples/functions/write/utf16_fragment_bytes.axm` | Success | Успешно | — | TRUE / Позиция после первой половины, ожидается 3:  3 / TRUE / Длина двух замен, ожидается 6:  6 / Две независимые замены:  TRUE / TRUE / Длина целого |
| `samples/functions/writefile/basic_usage.axm` | Success | Успешно | — | TRUE / Привет, мир! / TRUE / Новый текст |
| `samples/functions/writefile/reject_unrepresentable_text.axm` | Success | Успешно | — | TRUE / TRUE / Ожидается MB3007: MB3007 / Прежний текст сохранён: TRUE |
| `samples/functions/writeline/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / alpha / beta |
| `samples/functions/writeline/lf_byte_positions.axm` | Success | Успешно | — | TRUE / Позиция после буквы и LF равна трём: 3 / TRUE / Позиция после пустой строки равна четырём: 4 / Временный файл удалён: TRUE |
| `samples/functions/year/basic_usage.axm` | Success | Успешно | — | 2026 |
| `samples/graphics/game/maze_chase.axm` | Success; предупреждений: 2 | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/game/maze_chase_apply_calibration.axm` | Success | Успешно | — | Калибровка перенесена в maze_chase.axm. / Процедура InitPelletAlignment() полностью обновлена из файла калибровки. |
| `samples/graphics/game/paddle_ball.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/game/snake_arrow_controls.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/images/load_and_draw_image.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/images/missing_image_error.axm` | Success | Ожидаемая ошибка выполнения | MB3005 | Без текстового вывода |
| `samples/graphics/input/input_on_screen.axm` | Success | Успешно | — | Ввод: 1 ответов; Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/input/text_screen_input.axm` | Success | Успешно | — | Ввод: 2 ответов |
| `samples/graphics/modern/camera_world.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/modern/hud_text.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/modern/invalid_text_position_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/graphics/modern/lines_and_circles.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/modern/missing_canvas_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/graphics/modern/negative_radius_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/graphics/modern/rectangles_frame.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/modern/viewport_hud.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/mouse/mouse_events.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/primitives/circle_arcs_step_aspect.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/primitives/circle_full_turn.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/primitives/paint_fill_region.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/graphics/primitives/pset_line_circle.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/primitives/step_and_point_position.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/screen/best_fit_dimensions.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/graphics/screen/close_window_stops_run.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/screen/invalid_page_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/graphics/screen/mode_info.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/screen/text_scroll.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/graphics/sprites/invalid_frame_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/graphics/sprites/page_flip_walk.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/sprites/walk_cycle.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/text/print_locate_color.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/graphics/text/transparent_text.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/ampersand/mixed_scalar_concat.axm` | Success | Успешно | — | Привет, Иван тебе 7 лет. / flag=TRUE score=12.5 |
| `samples/operators/and/bitwise_usage.axm` | Success | Успешно | — | 2 / FALSE |
| `samples/operators/assignment/array_element.axm` | Success | Успешно | — | 7 |
| `samples/operators/assignment/basic_assignment.axm` | Success | Успешно | — | 15 |
| `samples/operators/circle/text_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/close/basic_usage.axm` | Success | Успешно | — | TRUE / Файл будет закрыт явно. |
| `samples/operators/close/write_failure_state.axm` | Success | Успешно | — | TRUE / TRUE / Путь первого файла: C:\Users\comp\AppData\Local\Temp\close_first_y4cuzx2a.ejw / Закрытие через три секунды. / Первый файл закрыт: TRUE /; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/color/text_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/const/basic_constant.axm` | Success | Успешно | — | Axiom |
| `samples/operators/const/fixed_string_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2026 | Без текстового вывода |
| `samples/operators/const/predefined_constants.axm` | Success | Успешно | — | 1 / 2 / 10 / 21 / TRUE / TRUE / 3 |
| `samples/operators/dim/array_declaration.axm` | Success | Успешно | — | 20 |
| `samples/operators/dim/basic_declaration.axm` | Success | Успешно | — | 42 |
| `samples/operators/dim/decimal_explicit_declaration.axm` | Success | Успешно | — | 10.30 / 5.15 / 10.30 |
| `samples/operators/dim/dynamic_array_declaration.axm` | Success; предупреждений: 1 | Успешно | — | dynamic array declared |
| `samples/operators/dim/fixed_string.axm` | Success | Успешно | — | [    ] / [AB  ] / [Alph] |
| `samples/operators/dim/inferred_type_from_initializer.axm` | Success | Успешно | — | 15 / ALICE / YES / 2 / 14 |
| `samples/operators/dim/long_inference.axm` | Success | Успешно | — | 21474836479 / 42949672958 |
| `samples/operators/dim/minimum_lower_bound_error.axm` | Success; предупреждений: 1 | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/dim/multiple_declarations.axm` | Success | Успешно | — | 3 / 4 / TRUE / TRUE / 2014-05-01 |
| `samples/operators/dim/temporal_defaults.axm` | Success | Успешно | — | Проход:  / 1 / Начальная дата:  / 0001-01-01 / Начальное время:  / 00:00:00 / Начальные дата и время:  / 0001-01-01 00:00:00 / Проход:  / 2 / Начальна |
| `samples/operators/dim/unused_value_warning.axm` | Success; предупреждений: 1 | Успешно | — | Предупреждение не мешает выполнению программы. |
| `samples/operators/draw/blind_and_return.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/dynamic_string.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/octagon.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/paint_default_border.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/paint_segments.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/rotated_spokes.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/draw/square.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/drawtile/basic_tileset.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/drawtilemap/culling_boundaries.axm` | Success | Успешно | — | Пиксель видимого тайла: / 12 / Далёкая карта обработана.; Клавиши: ArrowLeft |
| `samples/operators/drawtilemap/extreme_parallax.axm` | Success | Успешно | — | Невидимый слой: / 0 / Компенсированный слой: / 12; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/drawtilemap/visible_and_layer.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/eqv/basic_usage.axm` | Success | Успешно | — | TRUE / FALSE / -6 |
| `samples/operators/erase/basic_usage.axm` | Success | Успешно | — | 0 |
| `samples/operators/error/rethrow_caught_error.axm` | Success | Успешно | — | inner / APP003 / Original failure / 4 |
| `samples/operators/error/user_code_catch.axm` | Success | Успешно | — | APP001 / Manual failure / after |
| `samples/operators/function/basic_definition.axm` | Success | Успешно | — | 25 |
| `samples/operators/function/capture_outer_local.axm` | Success | Успешно | — | 23 |
| `samples/operators/function/first_class_value.axm` | Success | Успешно | — | 12 |
| `samples/operators/function/fixed_string_parameter_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2026 | Без текстового вывода |
| `samples/operators/function/fixed_string_return_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2026 | Без текстового вывода |
| `samples/operators/function/local_nested_call.axm` | Success | Успешно | — | 12 |
| `samples/operators/function/returned_closure.axm` | Success | Успешно | — | 15 / 7 |
| `samples/operators/function/shared_closure_state.axm` | Success | Успешно | — | 1 / 2 / 3 |
| `samples/operators/get/canvas_block_capture.axm` | Success | Успешно | — | 6 / 9 / 12 / 15; Клавиши: ArrowLeft |
| `samples/operators/get/rectangle_capture.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/operators/imp/basic_usage.axm` | Success | Успешно | — | FALSE / TRUE / -5 |
| `samples/operators/input/cancel_wait.axm` | Success | Успешно | — | Нажмите «Остановить», пока программа ожидает число. / Эта строка появляется только после подтверждённого ввода: 1; Ввод: 1 ответов |
| `samples/operators/input/multiple_variables.axm` | Success | Успешно | — | 1 1; Ввод: 1 ответов |
| `samples/operators/input/prompt_comma.axm` | Success | Успешно | — | Имя 1; Ввод: 1 ответов |
| `samples/operators/input/prompt_semicolon.axm` | Success | Успешно | — | Имя: 1; Ввод: 1 ответов |
| `samples/operators/input/single_variable.axm` | Success | Успешно | — | 1; Ввод: 1 ответов |
| `samples/operators/input/whole_string.axm` | Success | Успешно | — | Сохранённая строка: [1] / Длина в единицах UTF-16: 1; Ввод: 1 ответов |
| `samples/operators/integer-division/basic_usage.axm` | Success | Успешно | — | 3 / -3 / -3 / 10737418239 |
| `samples/operators/integer-division/division_by_zero_error.axm` | Success | Ожидаемая ошибка выполнения | MB3001 | Без текстового вывода |
| `samples/operators/mod/basic_usage.axm` | Success | Успешно | — | 1 / -1 / 1 / 1 |
| `samples/operators/mod/division_by_zero_error.axm` | Success | Ожидаемая ошибка выполнения | MB3001 | Без текстового вывода |
| `samples/operators/not/bitwise_usage.axm` | Success | Успешно | — | -1 / FALSE |
| `samples/operators/on-key/any_key_handler.axm` | Success | Успешно | — | Нажмите любую клавишу, затем нажмите ESC. / любая клавиша / готово; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-key/chord_handler.axm` | Success | Успешно | — | Удерживайте CTRL, нажмите LEFT, затем нажмите ESC. / CTRL+LEFT / готово; Клавиши: Control+ArrowLeft, Escape |
| `samples/operators/on-key/handler_key_name.axm` | Success | Успешно | — | Нажмите любую клавишу, затем нажмите ESC. / LEFT / готово; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-key/invalid_handler_parameter.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2034 | Без текстового вывода |
| `samples/operators/on-key/left_handler.axm` | Success | Успешно | — | Щёлкните по панели вывода, нажмите LEFT, затем нажмите ESC. / обработчик LEFT / обработчик LEFT / готово; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-key/stop_resume_handler.axm` | Success | Успешно | — | Нажмите LEFT при отключённом обработчике, затем LEFT после ON, затем ESC. / LEFT / обработчик LEFT / готово; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-mouse/canvas_world_handler.axm` | Success | Успешно | — | TRUE / 0 / 312 / 99 / TRUE / TRUE; Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-mouse/left_button_handler.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-mouse/release_move_handler.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/on-mouse/wheel_handler.axm` | Success; предупреждений: 1 | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/open/basic_usage.axm` | Success | Успешно | — | TRUE / TRUE / Первая строка / Вторая строка |
| `samples/operators/open/explicit_utf16_read.axm` | Success | Успешно | — | TRUE / Начальная позиция: 0 / Привет 😀 / TRUE / TRUE |
| `samples/operators/or/bitwise_usage.axm` | Success | Успешно | — | 7 / TRUE |
| `samples/operators/paint/canvas_region_fill.axm` | Success | Успешно | — | 6 / 9; Клавиши: ArrowLeft |
| `samples/operators/paint/empty_pattern_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/paint/fish_fill.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/paint/pattern_repaint.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/paint/step_default_fill.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/paint/text_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/palette/change_and_reset_colors.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/palette/text_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/palette/using_array_colors.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/palette/using_without_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/pcopy/invalid_page_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/pcopy/page_copy_buffer.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/pcopy/text_screen_error.axm` | SemanticFailure | Ожидаемая ошибка компиляции | MB2046 | Без текстового вывода |
| `samples/operators/power/basic_usage.axm` | Success | Успешно | — | 8 / 512 / -4 / 4 |
| `samples/operators/preset/canvas_background_pixel.axm` | Success | Успешно | — | 0 / 15 |
| `samples/operators/print/basic_expression.axm` | Success | Успешно | — | 7 |
| `samples/operators/print/console_line_boundaries.axm` | Success | Успешно | — | Первая строка /  / Начало: конец / Число: 42 / Завершено |
| `samples/operators/print/empty_print.axm` | Success | Успешно | — | До /  / После |
| `samples/operators/print/mixed_list.axm` | Success | Успешно | — | 12 3 |
| `samples/operators/print/separator_comma.axm` | Success | Успешно | — | A B C / 1 2 3 4 5 |
| `samples/operators/print/separator_semicolon.axm` | Success | Успешно | — | Привет, мир / 123 |
| `samples/operators/print/using_basic.axm` | Success | Успешно | — | Имя: Боб   \| / Итого:  12.50 / Флаг: Д |
| `samples/operators/pset/canvas_world_pixel.axm` | Success | Успешно | — | 12 / 0 |
| `samples/operators/put/canvas_block_restore.axm` | Success | Успешно | — | 6 / 9 / 12 / 15; Клавиши: ArrowLeft |
| `samples/operators/put/default_xor_and_preset.axm` | Success | Успешно | — | Без текстового вывода |
| `samples/operators/randomize/basic_usage.axm` | Success | Успешно | — | 0.6681064659115423 / 0.6681064659115423 |
| `samples/operators/redim/basic_usage.axm` | Success | Успешно | — | 10 / 30 / 0 |
| `samples/operators/redim/empty_array.axm` | Success | Успешно | — | TRUE / 0 / 1 / 0 |
| `samples/operators/redim/preserve_basic_usage.axm` | Success | Успешно | — | 20 / 30 / 0 |
| `samples/operators/setmaptile/cell_update.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space |
| `samples/operators/shift-left/basic_usage.axm` | Success | Успешно | — | 8 / 6 |
| `samples/operators/shift-right/basic_usage.axm` | Success | Успешно | — | 4 / 268435456 |
| `samples/operators/sleep/delay_and_key_wait.axm` | Success | Успешно | — | Пауза 500 мс... / Нажмите любую клавишу. / Готово; Клавиши: ArrowLeft |
| `samples/operators/sub/basic_call.axm` | Success | Успешно | — | ==Axiom== |
| `samples/operators/sub/byref_record_argument.axm` | Success | Успешно | — | 37 |
| `samples/operators/sub/byref_scalar_argument.axm` | Success | Успешно | — | 11 |
| `samples/operators/sub/byval_record_copy.axm` | Success | Успешно | — | Ada / 36 |
| `samples/operators/sub/capture_outer_local.axm` | Success | Успешно | — | 4 |
| `samples/operators/sub/local_nested_call.axm` | Success | Успешно | — | 2 |
| `samples/operators/try/handled_runtime_error.axm` | Success | Успешно | — | before / caught / MB3001 / 4 / Деление на ноль. / cleanup / after |
| `samples/operators/try/rethrow_from_catch_runs_finally.axm` | Success | Успешно | — | Внутренняя очистка / APP010 / После обработки ошибки |
| `samples/operators/type/array_elements.axm` | Success | Успешно | — | Ada / 36 / London / Alan / Grace / 1 / 2 |
| `samples/operators/type/array_field_views.axm` | Success | Успешно | — | 23 / 24 / 2 / 4 / 4 / 99 |
| `samples/operators/type/array_fields.axm` | Success | Успешно | — | 10 / 20 / FALSE / 7 / 9 / 9 / 0 |
| `samples/operators/type/const_fixed_array_fields.axm` | Success; предупреждений: 1 | Успешно | — | 0 / 3 / 30 |
| `samples/operators/type/const_fixed_array_fields_forward.axm` | Success; предупреждений: 1 | Успешно | — | 0 / 3 / 30 |
| `samples/operators/type/constructor_invariant.axm` | Success | Успешно | — | Ada / 20 |
| `samples/operators/type/constructor_new.axm` | Success | Успешно | — | Zero / 18 / Ada / 19 / 20 / RUS / Grace / 18 / 19 |
| `samples/operators/type/default_field_values.axm` | Success | Успешно | — | Ada / 36 / TRUE |
| `samples/operators/type/field_access.axm` | Success | Успешно | — | 0 / Ada / 36 / London |
| `samples/operators/type/fixed_array_fields.axm` | Success | Успешно | — | 0 / 10 / 30 / 10 / 20 / 3 |
| `samples/operators/type/fixed_multidimensional_array_fields.axm` | Success | Успешно | — | 0 / 10 / 30 / 4 / 10 / 20 |
| `samples/operators/type/fixed_string_fields.axm` | Success | Успешно | — | [     ] / [Ada  ] / [Analyst ] / [Architec] |
| `samples/operators/type/grouped_fields.axm` | Success | Успешно | — | 0 / Ada / Analyst / 99 / 20 / 7 |
| `samples/operators/type/method_overloads.axm` | Success | Успешно | — | 2 / Alpha / 7 / Beta |
| `samples/operators/type/method_receivers_field_and_array.axm` | Success | Успешно | — | 5 / 7 / 8 / 9 |
| `samples/operators/type/method_receivers_temporary.axm` | Success | Успешно | — | 1 / 2 / 3 / 4 / 6 |
| `samples/operators/type/methods_self.axm` | Success | Успешно | — | 5 / 5 / 8 / 8 |
| `samples/operators/type/multidimensional_array_fields.axm` | Success | Успешно | — | 10 / 20 / 4 / FALSE / 9 / 0 |
| `samples/operators/type/named_literals.axm` | Success | Успешно | — | Ada / 36 / Paris / 0 / 0 / TRUE |
| `samples/operators/type/partial_named_literal_defaults.axm` | Success | Успешно | — | Grace / 36 / TRUE |
| `samples/operators/type/positional_constructor.axm` | Success | Успешно | — | Ada / 36 / Paris / Grace / 40 / TRUE |
| `samples/operators/type/structural_equality.axm` | Success | Успешно | — | TRUE / FALSE / FALSE / TRUE |
| `samples/operators/view-print/invalid_rows_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/view-print/scrolling_region.axm` | Success | Успешно | — | Клавиши: ArrowLeft, Space, Escape |
| `samples/operators/view/invalid_bounds_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/view/screen_and_relative.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/window/cartesian_and_screen.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/operators/window/zero_size_error.axm` | Success | Ожидаемая ошибка выполнения | MB3007 | Без текстового вывода |
| `samples/operators/xor/basic_usage.axm` | Success | Успешно | — | 5 / TRUE |
| `samples/smoke/Recursion check.axm` | Success | Успешно | — | Recursion check / Factorial 5 = 120 / Fibonacci 8 = 21 |
| `samples/smoke/array_first_class_values.axm` | Success | Успешно | — | 11 / 20 |
| `samples/smoke/block_scope_full_manual.axm` | Success | Успешно | — | scope-smoke-start / if-branch / elseif-branch / else-branch / case-two / case-else / for-1 / for-2 / nested-if-2 / while-0 / while-1 / do-0 / do-1 / s |
| `samples/smoke/block_scope_semantics.axm` | Success | Успешно | — | branch / 1 / 2 / done |
| `samples/smoke/bytecode-only/compiler_execution.axm` | Success | Успешно | — | Сумма: 3 /  / Шаг: 1 / Шаг: 2 / MB3007 / Завершено |
| `samples/smoke/debug_step_controls.axm` | Success | Успешно | — | Начало программы / Начало процедуры / Проверка отладки / Конец процедуры / Внутри функции DoubleValue / Результат: 42 / Программа завершена |
| `samples/smoke/directory_link_boundaries.axm` | Success | Успешно | — | Сначала подготовьте проверочные ссылки указанной в начале файла командой. |
| `samples/smoke/editor_block_structures_manual.axm` | Success | Успешно | — | вложенный SELECT CASE / цикл DO WHILE1 / иначе в DO WHILE2 / ветвь DO UNTIL3 / иначе в DO UNTIL4 / цикл LOOP WHILE3 / цикл LOOP WHILE2 / последний шаг |
| `samples/smoke/engine-compare/array_callback_pipeline.axm` | Success | Успешно | — | 240 / 240 / 152 / 3665849 / 234 / 1104 / 1224 |
| `samples/smoke/engine-compare/array_cube_rollup.axm` | Success | Успешно | — | 3 / 1 / 24 / 5310000 / 86910000 / 829980000 / 182024 |
| `samples/smoke/engine-compare/array_search_probe.axm` | Success | Успешно | — | 93138 / 880 / 181 / 33 / FALSE |
| `samples/smoke/engine-compare/array_shape_transform_mesh.axm` | Success | Успешно | — | 13860 / 23310 / 9 / 0 / 9 |
| `samples/smoke/engine-compare/array_staging_roundtrip.axm` | Success | Успешно | — | 3132 |
| `samples/smoke/engine-compare/array_transform_chain.axm` | Success | Успешно | — | 2 / 1 / 24 / 101 / 1224 / 2424 / 727200 |
| `samples/smoke/engine-compare/arrayaverage_exact_cancellation.axm` | Success | Успешно | — | TRUE |
| `samples/smoke/engine-compare/bitwise_shift_mesh.axm` | Success | Успешно | — | 5204332 / 13546377868 / 18720 |
| `samples/smoke/engine-compare/control_flow_branch_maze.axm` | Success | Успешно | — | 97370 / 3132 / 9472 / 2469 |
| `samples/smoke/engine-compare/current_time_snapshot.axm` | Success | Успешно | — | 1350360 / 36080 / 148720 / 17450180 / 2026-10-1019:01:592026-1 / :01:592026-10-1019:01:59 |
| `samples/smoke/engine-compare/date_time_calendar_pipeline.axm` | Success | Успешно | — | 3759432 / 106352 / 159708 / MTATAWMMAFMSMFJTFTMMMSAS / MTATAWMMAFMSMFJTFTMMMSAS |
| `samples/smoke/engine-compare/document_batch_repair.axm` | Success; предупреждений: 1 | Успешно | — | TRUE / TRUE / TRUE / TRUE / 1000 / 0 / TRUE / 1200 / Русский абзац: данные, м / т, план и обычный хвост. |
| `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` | Success | Успешно | — | 5700 |
| `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm` | Success | Успешно | — | 2160 |
| `samples/smoke/engine-compare/file_batch_roundtrip.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRU |
| `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm` | Success | Успешно | — | 1680 / FALSE |
| `samples/smoke/engine-compare/file_management_roundtrip.axm` | Success | Успешно | — | 556 / FALSE / FALSE; Клавиши: ArrowLeft |
| `samples/smoke/engine-compare/file_path_metadata_sweep.axm` | Success | Успешно | — | TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRUE / TRU |
| `samples/smoke/engine-compare/high_rank_metadata.axm` | Success | Успешно | — | 2080 / 2080 / 4160 / 4160 / 7052 / 260 |
| `samples/smoke/engine-compare/input_record_intake.axm` | Success | Успешно | — | 180 / 180 / 0 / 0 / 1111; Ввод: 180 ответов |
| `samples/smoke/engine-compare/maze_chase_grid_logic.axm` | Success | Успешно | — | 190 / 279 / 6180970 / 1560 / 370; Клавиши: ArrowLeft |
| `samples/smoke/engine-compare/maze_chase_input_polling.axm` | Success | Успешно | — | 0 / 0 / 0 / 39600 |
| `samples/smoke/engine-compare/maze_chase_mixed_frame.axm` | Success | Успешно | — | 240 / 274 / 169807 / 1; Клавиши: ArrowLeft, Space, Escape |
| `samples/smoke/engine-compare/mixed_workload_pipeline.axm` | Success | Успешно | — | 188 / 188 / 260 / 58 / 116 / 260 / 260 / 171 / 424 / row section= \| item section \| row section= \| item sectio /  section // May 26 12:46 // May 27 12: |
| `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm` | Success | Успешно | — | Клавиши: ArrowLeft |
| `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm` | Success | Успешно | — | 107865 |
| `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm` | Success | Успешно | — | 1332000 |
| `samples/smoke/engine-compare/numeric_function_sweep.axm` | Success | Успешно | — | 33540 / 201292 / 1101029 / 34330; Клавиши: ArrowLeft |
| `samples/smoke/engine-compare/open_file_stream_cycle.axm` | Success | Успешно | — | 12802 |
| `samples/smoke/engine-compare/print_output_gallery.axm` | Success | Успешно | — | ROW1BETAAMOUNT12.475 18  1\| 12.48\|  -$5.53 / BBET    \ TRACE / ROW2GAMMAAMOUNT24.95 36  2\| 24.95\| -$11.05 / GGAM    \ TRACE / ROW3DELTAAMOUNT37.425 54 |
| `samples/smoke/engine-compare/random_stream_snapshot.axm` | Success | Успешно | — | 111228979 / 1002 / -2210 |
| `samples/smoke/engine-compare/regex_array_result_mesh.axm` | Success | Успешно | — | 3200 / 3200 / 3200 / code=warn205 / code=ERR101 / CD34 / EF56 / level=warn code / h=/runtime/core |
| `samples/smoke/engine-compare/regex_log_analysis.axm` | Success | Успешно | — | 800 / 1600 / 2400 / 2400 / ERR302 / ERR101 / 2002 / 1001 |
| `samples/smoke/engine-compare/regex_modes_pipeline.axm` | Success | Успешно | — | 1800 / 1800 / 3600 / 3600 / WARN-205 / AB12 |
| `samples/smoke/engine-compare/routine_closure_pipeline.axm` | Success | Успешно | — | 155520 / 88185 / 54240 / 333120 / 16320 |
| `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` | Success | Успешно | — | 116336 / 53162682 / 200614 / 296087 / 976 / R320 / FALSE |
| `samples/smoke/engine-compare/string_array_loop_pipeline.axm` | Success | Успешно | — | 180 / 204 / 204 / 28 / 37 / 180 / заголовок разд / строка готово  / блок готово аб / ril 22 10:34 // April 23 10:41 // April 24 10:48 |
| `samples/smoke/engine-compare/string_concat_accumulator.axm` | Success | Успешно | — | 13893 / xxxxx:3000 / LRLRLR:3000 |
| `samples/smoke/engine-compare/string_primitive_scan.axm` | Success | Успешно | — | 35100 |
| `samples/smoke/engine-compare/temp_cleanup_probe.axm` | Success | Успешно | — | 560 |
| `samples/smoke/engine-compare/temp_creation_write_cycle.axm` | Success | Успешно | — | 360 |
| `samples/smoke/engine-compare/temp_dirsize_probe.axm` | Success | Успешно | — | 34880; Клавиши: ArrowLeft |
| `samples/smoke/engine-compare/temp_filesize_probe.axm` | Success | Успешно | — | 16480 |
| `samples/smoke/engine-compare/temp_path_creation_probe.axm` | Success | Успешно | — | 480 |
| `samples/smoke/engine-compare/temp_size_cleanup_cycle.axm` | Success | Успешно | — | 3480 |
| `samples/smoke/engine-compare/temp_size_probe.axm` | Success | Успешно | — | 34320 |
| `samples/smoke/engine-compare/temp_storage_roundtrip.axm` | Success | Успешно | — | 3540 |
| `samples/smoke/engine-compare/temp_writefile_probe.axm` | Success | Успешно | — | 1200 |
| `samples/smoke/engine-compare/type_record_roundtrip.axm` | Success | Успешно | — | 1997710 / 2304500 / 366 / 0 / 8 |
| `samples/smoke/full_language_bytecode.axm` | Success | Успешно | — | full-smoke-start / input:1:1 / const-for:Axiom:6 / if:ok / while:0 / while:1 / do:2 / case:six / 3 / 1 / 8 / 8 / 4 / 5 / TRUE / FALSE / Name: Bob   \| ; Ввод: 1 ответов |
| `samples/smoke/identifiers/latin_names.axm` | Success | Успешно | — | Значение: 67 / Предел: 100; Клавиши: ArrowLeft, Space |
| `samples/smoke/identifiers/russian_name_rejected.axm` | SyntaxFailure | Ожидаемая ошибка компиляции | MB0001 | Без текстового вывода |
| `samples/smoke/multidimensional-arrays/array_first_group.axm` | Success | Успешно | — | 2 / 3 / 11 / 12 / 20 / 99 / TRUE / 4 / 8 / 8 |
| `samples/smoke/multidimensional-arrays/array_second_group.axm` | Success | Успешно | — | TRUE / TRUE / 2 / 3 / 5 / 5 / N20 / N40 / [10][20][30][40] |
| `samples/smoke/multidimensional-arrays/partial_indexing_view.axm` | Success | Успешно | — | 70 / 9 / 10 / 11 / 2 / 70 |
| `samples/smoke/multidimensional-arrays/range_indexing_view.axm` | Success | Успешно | — | 210 / 22 / 320 / 11 / 12 / 2 / 3 / 11 / 12 / 4 |
| `samples/smoke/multidimensional-arrays/two_dimensional_grid.axm` | Success | Успешно | — | TRUE / 1 / 2 / 10 / 11 / 7 / 9 / 9 / 0 |
| `samples/smoke/nested_runtime_diagnostic.axm` | Success | Ожидаемая ошибка выполнения | MB3001 | Без текстового вывода |
| `samples/smoke/repair_plans_mojibake.axm` | Success; предупреждений: 1 | Успешно | — | Путь к исправленной рабочей копии: C:\Users\comp\AppData\Local\Temp\axiom_repair_vnix5e5o.idu / Копирование исходного файла в рабочую копию: TRUE / Ис; Клавиши: ArrowLeft |
| `samples/smoke/resources/null_path.axm` | Success; предупреждений: 1 | Успешно | — | Отказ загрузки: MB3005 / Программа продолжает работу |
| `samples/smoke/smart_identifier_names.axm` | Success | Успешно | — | Скорость: 9 / Удвоенная скорость: 18 / Строка playerspeed остаётся без изменений.; Клавиши: ArrowLeft, Space |
| `samples/smoke/v0_1_baseline.axm` | Success | Успешно | — | -------------------- / Axiom sample catalog / -------------------- / Enter your name: / Enter your age: / minor / value1:1 / value2:6 / value3:12 / co; Ввод: 2 ответов |
| `samples/smoke/variables_name_collision_warning.axm` | Success; предупреждений: 1 | Успешно | — | 7 |
| `samples/smoke/variables_scope_blocks.axm` | Success; предупреждений: 2 | Успешно | — | if-branch / two / two / 1 / 2 / 10 / 11 |
| `samples/smoke/variables_scope_case.axm` | Success | Успешно | — | 10 / 6 / 3 |
| `samples/smoke/variables_scope_forward_global.axm` | Success | Успешно | — | 42 / 105 |
| `samples/smoke/variables_scope_global_local.axm` | Success | Успешно | — | 100 / 101 / 1 / 1 |
