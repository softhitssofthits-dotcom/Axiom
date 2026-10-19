# Реестр измерений BytecodeVm

Действующая команда — `benchmark-bytecode`. Она измеряет VM и полное время подготовки/исполнения, выполняет прогрев и повторы, выводит минимум, среднее, медиану и максимум. Стабильность результатов между повторами не заменяет независимое ожидание семантики.

```powershell
dotnet run --project src/Axiom.ManualChecks -- benchmark-bytecode --iterations 3 --warmup 1
```

Команды .NET выполнять последовательно. `profile-bytecode-opcodes` и `audit-bytecode-coverage` остаются отдельными средствами. Каталог существующих нагрузок `samples/smoke/engine-compare/` сохранён как точный путь; 52 файла перечислены ниже.

## Открытые измерения

| Направление | Что осталось |
|---|---|
| ISNUMERIC | Отдельная нагрузка и измерение функции |
| CODEPOINT/GRAPHEME | Измерение поддержанных форм LEN/LEFT/RIGHT/MID |
| Метаданные TILEMAP | Измерение функций свойств, слоёв, объектов и геометрии |
| Отсечение TILEMAP | Сравнить обычную Int128-ветвь и редкую BigInteger-компенсацию |
| Отладка | Измерить стоимость снимков и шагов на согласованной нагрузке |
| Специализированные стеки | Кандидат оптимизации; сначала измерения, затем отдельное решение |

Точное ARRAYAVERAGE измеряется инструментом [aggregate-perf](../scripts/aggregate-perf/README.md); выбранные размеры и биты результата проверяются независимо. Старые числовые замеры не объявляются результатом свежего прогона. В этом документном проходе нагрузки не изменялись и не запускались.

## Существующие файлы нагрузок

- `samples/smoke/engine-compare/array_callback_pipeline.axm`
- `samples/smoke/engine-compare/array_cube_rollup.axm`
- `samples/smoke/engine-compare/array_distinct_unique.axm`
- `samples/smoke/engine-compare/array_push_pop_growth.axm`
- `samples/smoke/engine-compare/array_search_probe.axm`
- `samples/smoke/engine-compare/array_shape_transform_mesh.axm`
- `samples/smoke/engine-compare/array_staging_roundtrip.axm`
- `samples/smoke/engine-compare/array_transform_chain.axm`
- `samples/smoke/engine-compare/arrayaverage_exact_cancellation.axm`
- `samples/smoke/engine-compare/bitwise_shift_mesh.axm`
- `samples/smoke/engine-compare/control_flow_branch_maze.axm`
- `samples/smoke/engine-compare/current_time_snapshot.axm`
- `samples/smoke/engine-compare/date_time_calendar_pipeline.axm`
- `samples/smoke/engine-compare/document_batch_repair.axm`
- `samples/smoke/engine-compare/drive_temp_storage_sweep.axm`
- `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm`
- `samples/smoke/engine-compare/file_batch_roundtrip.axm`
- `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm`
- `samples/smoke/engine-compare/file_management_roundtrip.axm`
- `samples/smoke/engine-compare/file_path_metadata_sweep.axm`
- `samples/smoke/engine-compare/high_rank_metadata.axm`
- `samples/smoke/engine-compare/input_record_intake.axm`
- `samples/smoke/engine-compare/maze_chase_grid_logic.axm`
- `samples/smoke/engine-compare/maze_chase_input_polling.axm`
- `samples/smoke/engine-compare/maze_chase_mixed_frame.axm`
- `samples/smoke/engine-compare/mixed_workload_pipeline.axm`
- `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm`
- `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`
- `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm`
- `samples/smoke/engine-compare/numeric_function_sweep.axm`
- `samples/smoke/engine-compare/open_file_stream_cycle.axm`
- `samples/smoke/engine-compare/print_output_gallery.axm`
- `samples/smoke/engine-compare/random_stream_snapshot.axm`
- `samples/smoke/engine-compare/regex_array_result_mesh.axm`
- `samples/smoke/engine-compare/regex_log_analysis.axm`
- `samples/smoke/engine-compare/regex_modes_pipeline.axm`
- `samples/smoke/engine-compare/routine_closure_pipeline.axm`
- `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm`
- `samples/smoke/engine-compare/string_array_loop_pipeline.axm`
- `samples/smoke/engine-compare/string_concat_accumulator.axm`
- `samples/smoke/engine-compare/string_primitive_scan.axm`
- `samples/smoke/engine-compare/temp_cleanup_probe.axm`
- `samples/smoke/engine-compare/temp_creation_write_cycle.axm`
- `samples/smoke/engine-compare/temp_dirsize_probe.axm`
- `samples/smoke/engine-compare/temp_filesize_probe.axm`
- `samples/smoke/engine-compare/temp_path_creation_probe.axm`
- `samples/smoke/engine-compare/temp_size_cleanup_cycle.axm`
- `samples/smoke/engine-compare/temp_size_probe.axm`
- `samples/smoke/engine-compare/temp_storage_roundtrip.axm`
- `samples/smoke/engine-compare/temp_writefile_probe.axm`
- `samples/smoke/engine-compare/type_record_roundtrip.axm`
- `samples/smoke/engine-compare/type_method_receiver_chain.axm`

## Отдельные измерения GPU-CANVAS — 13 октября 2026 года

Существующие семьи modern_canvas_primitive_burst и modern_canvas_sprite_burst остаются нагрузками исполнения BytecodeVm. Для настоящего графического вывода добавлен `scripts/perf/CanvasGpuProbe/`: CPU-контроль и GPU-путь рисуют 500 спрайтов либо 825 тайлов с камерой и текстом в нативном окне 1024 × 768. Измеряются CPU-время процесса, аппаратный интервал команд, полное время кадра, выделения памяти, загрузки текстур и передачи GPU → CPU. Условия, показатели и пределы выводов записаны в `reports/graphics/CANVAS_GPU_MIGRATION.md`. Новые графические показатели не выдаются за ускорение VM и не меняют каталог engine-compare.

## Диагностические измерения SCREEN — 15 октября 2026 года

Перед возможным переносом SCREEN на GPU выполнены ограниченные измерения действующей графической службы: полностью невидимый LINE BF 3000×3000 занял 412–449 мс; выделения управляемой памяти при открытии SCREEN 8 составили около 83 млн байт, SCREEN 9 — около 147 млн байт. Обнаружен перебор исходной невидимой площади вместо отсечения. Это диагностические одиночные измерения, не новая семья engine-compare и не сравнение CPU/GPU. Каталог сценариев не изменён. Условия и ограничения — в локальном отчёте `reports/graphics/SCREEN_AUDIT_20261015.md`. Перед переносом нужны отдельные реальные нагрузки SCREEN с учётом страниц, палитры, текста и читающих операций.

## Отсечение невидимых фигур SCREEN — 15 октября 2026 года

После исправления LINE/B/BF тот же невидимый BF 3000×3000 через графическую службу занял 17,47 мс вместо 412–449 мс. Замер включает публикацию кадра и остаётся одиночным диагностическим измерением. Регрессионная проверка ограничивает число обращений к хосту для фигур с координатами до миллиарда; порога времени в тесте нет. Семьи engine-compare, выделение четырёх страниц и GPU-путь SCREEN не менялись. Условия и исходные результаты — в локальном отчёте `reports/graphics/SCREEN_AUDIT_20261015.md`.

## GPU-страницы SCREEN — 15 октября 2026 года

В IDE 0.6.0 страницы SCREEN создаются по обращению и хранятся на GPU. Обычная публикация копирует видимую страницу и накладывает кешированный текстовый слой на GPU. Проверка обычных кадров подтверждает ReadbackBytes=0 до явного чтения; неизменный текст не составляется повторно. POINT, GET/PUT и PAINT сохраняют явную синхронизацию необходимых данных, сложные дуги готовят набор координат на CPU. Это функциональное доказательство отсутствия передачи обычного кадра, а не сравнительный замер FPS или полной памяти. Каталог engine-compare не менялся. Устройство, проверки и пределы — в локальном отчёте `reports/graphics/SCREEN_DIRECTX_MIGRATION.md`.


## Нагрузки роста массива и ARRAYDISTINCT — 17 октября 2026 года

Добавлены два сценария семейства массивов: `array_push_pop_growth.axm` (12 раундов по 1500 добавлений и 500 снятий) и `array_distinct_unique.axm` (6000 уникальных значений, четыре прохода). Они закрепляют измерения операций, которые раньше проверялись только косвенно.

До исправления одиночные замеры Debug CLI давали около 9,1 с на 60 000 последовательных `ARRAYPUSH` и около 26,4 с на `ARRAYDISTINCT` с 20 000 уникальных значений. После перехода на ёмкость хранилища и хеш-множество те же сценарии укладываются примерно в 0,78 с и 0,4 с с учётом запуска CLI (около 0,32–0,42 с). Числа остаются диагностическими одиночными замерами, а не эталоном; существующие сценарии и их размеры не менялись.

## Нагрузка методов TYPE с составным получателем — 17 октября 2026 года

Добавлен сценарий `type_method_receiver_chain.axm`: 400 проходов по методу вложенного поля и 16 методам элементов массива записей, всего 6800 вызовов с адресуемым `SELF`. Сценарий закрепляет путь, который раньше шёл через копию записи и обратную запись, а теперь передаёт метод адрес поля или элемента с однократно вычисленными индексами.

Одиночный замер Release CLI с учётом запуска процесса: около 0,8 с, вывод 80200 / 7200 / 400 / 10200. Число остаётся диагностическим ориентиром, а не эталоном: сравнительного замера до и после изменения не выполнялось, потому что прежняя реализация теряла изменения `SELF` и давала другой вывод, так что сравнение времени на разном результате некорректно. Каталог engine-compare теперь содержит 52 файла; остальные семьи не менялись.

## RegEx: ограничения существующих измерений — 18 октября 2026 года

Аудит [REGEX_AUDIT_2026-10-18.md](plans/REGEX_AUDIT_2026-10-18.md) выявил отсутствие отдельных нагрузок на массовый REGEXCOUNT, рост кэшей, отмену между успешными поисками и позднее изменение при REGEXREPLACE. Три существующих regex_*.axm сохранены без изменений и в этом аудите не запускались. Локальное измерение выделений подтверждает лишние MatchCollection/Match и предварительный обход замены; это не полный прогон benchmark-bytecode. Новые сценарии не добавлены; план измерений находится в разделе 7 аудита.

## RegEx: измерения после устранения причин — 18 октября 2026 года

Добавлен samples/smoke/engine-compare/regex_resource_mesh.axm и зарегистрирован в Axiom.ManualChecks: массовый подсчёт, поздняя буквальная замена, массивный результат и 512 динамических шаблонов. Каталог содержит 53 файла. Все четыре regex_ нагрузки прошли: benchmark-bytecode --filter regex_ --iterations 5 --warmup 2. Медианы VM: журнал 109,6 мс, режимы/замены 57,4 мс, массивы 121,7 мс, ресурсная нагрузка 318,3 мс. Это текущий ориентир; сравнение штатным инструментом до/после не выполнялось.

Отдельное измерение с сохранёнными DLL до/после (прогрев и пять повторов, без компиляции): REGEXCOUNT на 200 000 совпадений — 46 222 736 → 409 192 выделенных байт; поздняя REGEXREPLACE — 50 125 592 → 2 045 208; замена без изменения — 46 827 800 → 409 168; REGEXMATCHES — 62 224 344 → 17 428 120. Это выделения, а не пиковая память. Запрос отмены через 100 мс в тяжёлом примере обслужен за 104 мс вместо 6823 мс. Кэш ограничен 128 шаблонами, исходные modes не сохраняются. Результаты и ограничения: plans/REGEX_AUDIT_2026-10-18.md, раздел 10; локальные данные — .artifacts/regex-fixes. Ускорение всех программ не заявляется.

## Даты и время: раздельные измерения — 18 октября 2026 года

Аудит [DATE_TIME_AUDIT_2026-10-18.md](plans/DATE_TIME_AUDIT_2026-10-18.md) отметил, что существующие нагрузки смешивают календарную арифметику с чтением текущих часов, поэтому стоимость разбора текста, поиска пояса и чистой арифметики не была известна по отдельности. Добавлены три адресные нагрузки и зарегистрированы в `Axiom.ManualChecks`.

- `samples/smoke/engine-compare/temporal_text_parse_and_format.axm` — `DATEVALUE`, `TIMEVALUE`, `DATETIMEVALUE`, `INSTANTVALUE`, `ISDATE` и обратная запись значений в `STRING`. Текущее время не читается.
- `samples/smoke/engine-compare/temporal_time_zone_lookup.axm` — `UTCOFFSET`, `TOUTC`, `FROMUTC`, `TOINSTANT`, `FROMINSTANT` для трёх поясов, включая пояс с сезонным переходом.
- `samples/smoke/engine-compare/temporal_calendar_arithmetic.axm` — `DATEADD`, `DATEDIFF`, `DATEPART`, границы периода, високосность и арифметика времени суток без текста и поясов.

Запуск: `dotnet run --project src/Axiom.ManualChecks -- benchmark-bytecode --filter temporal_ --iterations 3 --warmup 1`. Медианы BytecodeVm на текущей машине: разбор и печать 11,1 мс, поиск пояса 2,6 мс, календарная арифметика 9,5 мс. Все три сценария прошли.

Числа — диагностический ориентир на текущем оборудовании, а не эталон: сравнительного замера до и после изменения не выполнялось, потому что новых оптимизаций в этой задаче не вводилось. Отдельного кеша поясов не добавлено: измерение не показало, что поиск пояса является узким местом. Каталог `engine-compare` содержит 56 файлов.

## Строки: адресные измерения аудита — 18 октября 2026 года

Запуск `dotnet run --project src/Axiom.ManualChecks -- benchmark-bytecode --filter string_audit --iterations 3 --warmup 1` завершился 6/6 успешно. Медианы BytecodeVm: Unicode-срезы — 1218,0 мс; общие префиксы замены — 72,3 мс; последовательные номера строк — 100,9 мс; отложенная файловая запись — 14,8 мс; полная проверка числового текста — 34,8 мс; строгие добавления и открытый файл — 34,4 мс.

Сценарий `string_audit_append_encoding.axm` сопоставляет явный `APPENDFILE` с открытым `APPEND`, но один фиксированный размер не доказывает масштабирование при росте файла. История консоли пока не имеет нагрузки; числа выше являются ориентирами, а не сравнением до и после. Каталог `engine-compare` содержит 62 файла.

Повторные строгие вызовы `APPENDFILE` измерены отдельным стендом `.artifacts/strings-audit/append_call_scaling.axm`: 10K/20K/40K вызовов заняли 1 551,26/3 381,59/6 542,45 мс. Размеры файлов совпали с числом добавленных байт. Это измерение показывает стоимость повторной полной проверки и не является основанием для снятия проверки кодировки; открытая форма `APPEND` остаётся рекомендуемым путём для серии записей.

## История консоли IDE: адресное измерение — 19 октября 2026 года

Стенд `.artifacts/strings-audit/ConsoleHistoryBench` измерил `ConsoleOutputService` на 10K/20K/40K строк. Накопление с одним итоговым чтением снимка заняло 1,34/2,80/5,39 мс. Чтение полного `OutputText` после каждой строки заняло 25,01/194,89/849,35 мс и выделило 303,0 МБ/1,206 ГБ/4,812 ГБ. Итоговый текст и число строк во всех случаях совпали. Кеш неизменённого снимка не меняет стоимость первой публикации после каждой новой строки; переход на дельта-протокол требует отдельного проектного решения.

## Файловые алгоритмы: диагностические измерения — 19 октября 2026 года

Локальный стенд `.artifacts/filesystem-audit/` измерил SKIPLINE на ASCII-строках 1 000 000 и 4 000 000 символов: выделения текущего потока около 5,10 и 20,39 МБ, последний запуск 14,8 и 65,1 мс. Пропуск материализует и декодирует строку. 80 отдельных APPENDFILE к исходным 4 000 000 байтам: без явной кодировки 20,6 мс, с явной UTF-8 311,5 мс. Это одиночные диагностические запуски без прогрева/статистики; варианты имеют разный договор проверки, результаты не доказывают допустимость снятия строгой проверки. Повторное полное сканирование APPENDFILE уже отмечалось выше.

Новая семья engine-compare не добавлена. Для будущей проверки нужны отдельные нагрузки пропуска длинных строк, отмены обхода и масштабирования строгой дописи по двум осям: исходный размер и число вызовов. Подробности: [FILESYSTEM_AUDIT_2026-10-19.md](plans/FILESYSTEM_AUDIT_2026-10-19.md).

## Файловая реализация: пропуск и строгая допись — 19 октября 2026 года

После блочного сканера SKIPLINE та же локальная проба на 1 000 000/4 000 000 ASCII-символов дала около 16,1/12,4 КБ выделений текущего потока вместо 5,10/20,39 МБ. Время одиночной пробы — 5,1/27,1 мс; эти числа не являются статистическим сравнением скорости. Постоянный тест ограничивает выделения и отдельно проверяет много коротких строк. Строка из 16 777 217 символов пропускается успешно. Счётчик опросов DIRSIZE вырос с 6 для пустого дерева до 1006 для 1000 файлов; отдельные тесты доказывают именно отмену внутри операции.

Для строгой дописи выполнены две независимые оси: исходный размер и число добавлений одного ASCII-символа. Каждый вариант имел один прогрев и три измерения; ниже медиана времени всего исполнения, включая подготовку программы. Итоговый размер и всё содержимое сверялись отдельно после каждого запуска. Источник и журнал — .artifacts/filesystem-audit/ScalingProgram.cs и append-scaling.txt.

| Исходных байтов | Добавлений | Отдельные APPENDFILE с UTF-8, мс | Один OPEN APPEND USING UTF-8, мс |
|---|---|---|---|
| 1 000 000 | 20 | 20,30 | 2,00 |
| 1 000 000 | 80 | 79,54 | 2,11 |
| 4 000 000 | 20 | 83,65 | 4,97 |
| 4 000 000 | 80 | 305,57 | 4,80 |

Третий аргумент APPENDFILE и USING у OPEN задавали UTF-8. Это сопоставление двух способов записи с одинаковым результатом, а не обещание ускорения APPENDFILE или доказательство допустимости убрать проверку содержимого. Строгий договор сохранён. Постоянный пример open_append_series.axm и независимая проверка Execution закрепляют рекомендуемый путь серии записей. Новых сценариев engine-compare в этой задаче не добавлено; существующая string_audit_append_encoding.axm сохранена.
