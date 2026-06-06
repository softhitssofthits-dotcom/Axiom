# Реестр производительных сценариев Bytecode VM

Этот файл не заменяет `docs/BYTECODE_COVERAGE.md`.

`docs/BYTECODE_COVERAGE.md` отвечает на вопрос «что вообще поддержано и проверено в `BytecodeVm`».

Этот файл отвечает на другой вопрос: «какие семейства языка реально представлены в нагрузочных сценариях `samples/smoke/engine-compare/`, по которым мы измеряем и ускоряем VM».

## Как использовать реестр

- После каждой задачи, которая добавляет новый `engine-compare` сценарий или меняет статус семейства языка в производительных замерах, этот файл нужно обновлять в той же задаче.
- Если семейство языка покрыто только косвенно через большой смешанный сценарий, это нужно писать честно, а не помечать как полностью закрытое.
- Если семейство пока нельзя нормально измерять текущим harness, это тоже нужно фиксировать явно, чтобы долг был виден.

## Статусы

- `выделенный` — есть отдельный адресный `engine-compare` сценарий именно под это семейство.
- `косвенный` — семейство заметно участвует в существующих сценариях, но без отдельного адресного прогона.
- `нет` — в текущих производительных сценариях семейство практически не представлено.
- `ограничено` — сценарий пока не добавлен из-за известного ограничения текущего `compare-engines`.

## Шкала производительного решения

- `<= 0.900x` — целевой уровень: байткодная VM уже даёт заметный выигрыш над интерпретатором, и это семейство не является срочным долгом.
- `0.900x .. 1.000x` — рабочий паритет: VM уже не хуже интерпретатора, но запас для дальнейшего разгона ещё есть.
- `> 1.000x` — прямой долг: VM на адресном сценарии пока медленнее интерпретатора и должна быть ближайшим кандидатом на оптимизацию.

## Матрица

| Семейство языка | Обычные sample | Производительный сценарий | Статус | Примечание |
| --- | --- | --- | --- | --- |
| Строковые built-in | `samples/functions/left`, `right`, `mid`, `contains`, `replace`, `trim`, `split`, `join`, `padleft`, `padright`, `center`, `repeat`, `reverse`, `lcase`, `ucase` и соседние | `samples/smoke/engine-compare/string_array_loop_pipeline.axm`, `string_primitive_scan.axm`, `string_concat_accumulator.axm`, `document_batch_repair.axm`, `mixed_workload_pipeline.axm` | выделенный | К широкому string-сценарию добавлен отдельный низкоуровневый sweep для `ASC`, `CHR`, `LEN`, `SPACE`, `STRING`, `INSTR`, `INSTRREV` и `SUBSTRCOUNT`. Длинный прогон `20` итераций с `3` прогревами держится около `0.398x` runtime / `0.476x` total, а отдельный сценарий пользовательского накопления строк `string_concat_accumulator.axm` держится около `0.522x` runtime / `0.534x` total. Значит, и built-in-слой строк, и типовой цикл накопления строки уже уверенно находятся в целевом диапазоне. |
| Regex built-in | `samples/functions/regexismatch`, `regexmatch`, `regexextract`, `regexreplace`, `regexcount`, `regexmatches`, `regexextractall`, `regexsplit` | `samples/smoke/engine-compare/regex_log_analysis.axm`, `regex_modes_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Есть и сценарий на массивные regex-функции, и отдельный сценарий на режимы и цепочку замен. |
| Regex built-in массивных результатов | `samples/functions/regexmatches`, `regexextractall`, `regexsplit` | `samples/smoke/engine-compare/regex_array_result_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` без `REGEXREPLACE` и без смешанной строковой логики. Длинный прогон `20` итераций с `3` прогревами держится около `0.673x` runtime / `0.695x` total, то есть массивный regex-подслой уверенно находится в целевом диапазоне. |
| Файловые built-in над путями и whole-file операциями | `samples/functions/fileexists`, `readfile`, `writefile`, `appendfile`, `getfiles`, `combinepath`, `deletefile` и соседние | `samples/smoke/engine-compare/file_batch_roundtrip.axm`, `file_path_metadata_sweep.axm`, `document_batch_repair.axm`, `temp_storage_roundtrip.axm` | выделенный | К прежнему файловому сценарию добавлен отдельный path/info sweep для `FILEEXISTS`, `PATHEXISTS`, `FILESIZE`, `FILEDATE`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `FULLPATH` и `ISABSOLUTEPATH`. Базовый path/info-sweep держится около `0.768x` runtime / `0.773x` total, а после прямого fast-path для `COMBINEPATH` новый temp/file-срез `temp_storage_roundtrip.axm` на длинном контроле `40` итераций с `5` прогревами держится около `0.960x` runtime / `0.960x` total. Значит, path helper `COMBINEPATH` уже не выглядит прямым долгом сам по себе, но остаётся частью рабочего паритета внутри временного файлового roundtrip. |
| Файловые built-in управления файлами и каталогами | `samples/functions/makedir`, `deletefile`, `copyfile`, `movefile`, `renamefile`, `deletedir`, `deletedirrecursive`, `copydir`, `movedir`, `renamedir`, `changedir`, `currentdir` | `samples/smoke/engine-compare/file_management_roundtrip.axm`, `file_cleanup_navigation_probe.axm` | выделенный | Адресный сценарий переведён на уникальный временный корень через `MAKETEMPDIR`, чтобы убрать редкие межпрогонные конфликты по одним и тем же каталогам. После добавления в `BytecodeVm` прямого fast-path для file-management built-in длинный прогон `20` итераций с `3` прогревами по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-085758.txt` держится около `0.969x` runtime / `0.969x` total. Узкий сценарий `file_cleanup_navigation_probe.axm`, который после упрощения и адресных дожимов `CURRENTDIR`, `CHANGEDIR`, `DELETEFILE` и `DELETEDIRRECURSIVE` почти без примеси `COPY*` и `MOVE*` выделяет cleanup/navigation-подслой, на длинном прогоне `20/3` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104930.txt` держится уже около `0.816x` runtime / `0.818x` total. При этом широкий `file_management_roundtrip.axm` на отдельном контрольном прогоне `20/3` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-105022.txt` держится только около `0.996x` runtime / `0.996x` total. Значит, cleanup/navigation-подслой больше не является долгом VM, а широкий management-срез сейчас честно остаётся только в рабочем паритете. |
| Файловые built-in диска, размера каталога и временных путей | `samples/functions/dirsize`, `freespace`, `totalfreespace`, `totalspace`, `drivename`, `driveformat`, `drivetype`, `maketempfile`, `maketempdir` | `samples/smoke/engine-compare/drive_temp_storage_sweep.axm`, `drive_volume_metadata_sweep.axm`, `temp_storage_roundtrip.axm`, `temp_creation_write_cycle.axm`, `temp_path_creation_probe.axm`, `temp_writefile_probe.axm`, `temp_size_cleanup_cycle.axm`, `temp_size_probe.axm`, `temp_cleanup_probe.axm`, `temp_filesize_probe.axm`, `temp_dirsize_probe.axm` | выделенный | Старый mixed-сценарий `drive_temp_storage_sweep.axm` после очистки от строкового шума всё ещё держится около `0.921x` runtime / `0.921x` total, но дальнейший разрез показал, что внутри семейства уже видны разные подслои. `drive_volume_metadata_sweep.axm` для `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE` и `PATHROOT` держится около `0.724x` runtime / `0.736x` total. `temp_storage_roundtrip.axm` на длинном контроле `40/5` держится около `0.960x` / `0.960x`, свежий контроль `temp_creation_write_cycle.axm` держится около `0.937x` runtime / `0.939x` total, `temp_cleanup_probe.axm` около `0.924x` / `0.923x`, `temp_size_probe.axm` около `0.942x` / `0.942x`, `temp_filesize_probe.axm` около `0.884x` / `0.885x`, а `temp_dirsize_probe.axm` после прямого fast-path для `DIRSIZE` уже держится около `0.923x` / `0.923x`. Дополнительный разрез creation/write-половины показал, что `temp_path_creation_probe.axm` держится около `0.885x` runtime / `0.887x` total, а более длинный контроль `temp_writefile_probe.axm` (`40/5`) держится около `0.923x` / `0.923x`; попытка отдельного byte-cache для `WRITEFILE` была проверена и отклонена, потому что уводила этот же probe назад примерно к `0.976x` / `0.977x`. Смешанный `temp_size_cleanup_cycle.axm` после дожима тоже ушёл из прямого долга и на длинном контроле `40/5` держится около `0.983x` runtime / `0.982x` total. Значит, temp/file-семейство сейчас уже целиком находится в рабочем паритете без подтверждённого узкого отстающего подслоя. |
| Открытые текстовые файлы и файловые операторы `OPEN` / `CLOSE` | `samples/functions/isopen`, `readline`, `readtext`, `write`, `writeline`, `seekfile`, `rewind`, а также `samples/operators/open`, `close` | `samples/smoke/engine-compare/open_file_stream_cycle.axm`, `file_batch_roundtrip.axm` | выделенный | Новый адресный сценарий отдельно нагружает `OPEN`/`CLOSE`, `READLINE`, `READTEXT`, `READTOEND`, `WRITE`, `WRITELINE`, `FLUSH`, `SEEKFILE`, `REWIND`, `FILEPOS`, `FILEMODE`, `FILELENGTH`, `EOF` и `ISOPEN`. Длинный прогон `20` итераций с `3` прогревами держится около `0.813x` runtime / `0.834x` total, то есть этот файловый потоковый слой уже уверенно в целевом диапазоне. |
| Массивные преобразования и мутации | `samples/functions/arrayclone`, `arrayconcat`, `arrayslice`, `arraytake`, `arrayskip`, `arrayinsert`, `arrayremove`, `arraypush`, `arraypop`, `arrayflatten`, `arrayreshape`, `arrayreverse`, `arraytosorted` и соседние | `samples/smoke/engine-compare/array_transform_chain.axm`, `array_cube_rollup.axm`, `array_staging_roundtrip.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Это семейство уже стало основным источником последних ускорений VM. Дополнительный адресный сценарий `array_staging_roundtrip.axm` отдельно проверяет двухфазный staging через массивы строк и чисел без файлового слоя и на длинном прогоне `20` итераций с `3` прогревами держится около `0.474x` runtime / `0.580x` total. Значит, сам массивный staging-подслой сейчас один из самых быстрых и не является скрытым temp/file-хвостом. |
| Shape-преобразования массивов | `samples/functions/arraydistinct`, `arraytoreversed`, `arrayreverse`, `arrayflatten`, `arrayreshape` | `samples/smoke/engine-compare/array_shape_transform_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `ARRAYDISTINCT`, `ARRAYTOREVERSED`, `ARRAYREVERSE`, `ARRAYFLATTEN` и `ARRAYRESHAPE` на одном и том же 2D-массиве без callback-слоя и без поисковых built-in. Длинный прогон `20` итераций с `3` прогревами держится около `0.817x` runtime / `0.824x` total, то есть shape-подслой сразу оказался в целевом диапазоне. |
| Callback-builtins массивов | `samples/functions/arraymap`, `arrayfilter`, `arrayreduce`, `arrayevery`, `arraysome` | `samples/smoke/engine-compare/array_callback_pipeline.axm` | выделенный | Адресный сценарий уже держит VM около `0.821x` runtime / `0.824x` total. К предыдущим ускорениям добавился кэш function-value внутри внешнего кадра, который убрал повторное разрешение захваченных слотов и повторное создание значения функции в одном и том же цикле. |
| Поисковые built-in массивов | `samples/functions/arraycontains`, `arrayindexof`, `arraylastindexof`, `arraycount`, `arrayfindall`, `arraybinarysearch` | `samples/smoke/engine-compare/array_search_probe.axm` | выделенный | Новый адресный сценарий отдельно нагружает поисковые built-in на 1D и 2D-массивах без callback-слоя. Длинный прогон `20` итераций с `3` прогревами держится около `0.852x` runtime / `0.857x` total, то есть этот поисковый подслой сразу оказался в целевом диапазоне. |
| Скалярные выражения и присваивания | `samples/operators/dim`, `samples/operators/const`, базовые expression-сценарии вокруг `samples/operators/print` и acceptance-ошибок выражений | `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` | выделенный | Новый адресный сценарий нагружает локальные и глобальные присваивания, целочисленную и `DOUBLE`-арифметику, побитовые операции, сдвиги, булевы флаги и сравнения почти без built-in-слоя. После вынесения `Divide` в отдельный быстрый путь длинный прогон `20` итераций с `3` прогревами держится около `0.853x` runtime / `0.860x` total: слой уже переведён в целевой диапазон `<= 0.900x`. |
| Побитовые операции и сдвиги | базовые expression-сценарии вокруг `samples/operators/print` и `samples/operators/const`, а также косвенное участие в `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` | `samples/smoke/engine-compare/bitwise_shift_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `AND`, `OR`, `XOR`, `NOT`, `<<` и `>>` на `INTEGER`, `LONG` и `BOOLEAN`. Длинный прогон `20` итераций с `3` прогревами держится около `0.855x` runtime / `0.856x` total, то есть побитовый подслой уже переведён в целевой диапазон `<= 0.900x` и не является ближайшим долгом VM. |
| Числовые built-in | `samples/functions/abs`, `min`, `max`, `clamp`, `pow`, `sqr`, `cbrt`, `log`, `log10`, `log2`, `sin`, `cos`, `tan`, `asin`, `acos`, `asinh`, `acosh`, `atanh`, `floor`, `ceil`, `round`, `fix`, `setsign`, `sgn`, `int`, `val`, `str`, `rnd` и соседние | `samples/smoke/engine-compare/numeric_function_sweep.axm`, `random_stream_snapshot.axm` | выделенный | Новый сценарий закрыл прямой числовой слой и показал хороший результат: VM держится около `0.708x` runtime / `0.711x` total. После прокидывания управляемого генератора в harness `RND` тоже измеряется отдельно: `random_stream_snapshot.axm` держится около `0.818x` runtime / `0.800x` total. |
| Дата и время | `samples/functions/date`, `time`, `now`, `dateadd`, `datediff`, `datepart`, `dateserial`, `timevalue`, `toutc`, `fromutc` и соседние | `samples/smoke/engine-compare/date_time_calendar_pipeline.axm`, `current_time_snapshot.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Для календарных built-in и для built-in текущего времени теперь есть отдельные сценарии. `date_time_calendar_pipeline.axm` держится около `0.651x` runtime / `0.656x` total, а новый `current_time_snapshot.axm` с фиксированными часами держится около `0.497x` runtime / `0.569x` total. |
| Ветвления, циклы и общий управляющий слой | `samples/operators/if`, `samples/operators/select-case`, `samples/operators/for`, `samples/operators/while`, `samples/operators/do-loop` | `samples/smoke/engine-compare/control_flow_branch_maze.axm`, `mixed_workload_pipeline.axm`, `array_cube_rollup.axm`, `array_transform_chain.axm` | выделенный | Для управляющего слоя теперь есть отдельный детерминированный сценарий. После отложенной очистки `BytecodeEvaluationStack` и перевода comparison-jump инструкций на специализированные прямые пути `control_flow_branch_maze.axm` на длинном прогоне `20` итераций с `3` прогревами держится около `0.826x` runtime / `0.814x` total, то есть слой уже уверенно вошёл в целевой диапазон `<= 0.900x`. |
| `PRINT` и `PRINT USING` | `samples/operators/print`, `samples/operators/print-using` | `samples/smoke/engine-compare/print_output_gallery.axm`, `mixed_workload_pipeline.axm`, `document_batch_repair.axm`, `string_array_loop_pipeline.axm` | выделенный | После прямой записи `PRINT`, кэша форматов `PRINT USING`, прямой записи строковых полей и fast-path числовых масок, а затем общих ускорений VM длинный контрольный прогон `print_output_gallery.axm` (`20` итераций, `3` прогрева) держится около `0.860x` runtime / `0.877x` total, то есть слой уже вышел из серой зоны и закреплён в целевом диапазоне. |
| `INPUT` | `samples/operators/input`, `samples/functions/isdate` и другие смежные примеры ввода | `samples/smoke/engine-compare/input_record_intake.axm` | выделенный | После быстрого пути для `INPUT` на 1/2/3 целей без промежуточного массива результатов и длинного прогона `40` итераций с `5` прогревами `input_record_intake.axm` держится около `0.879x` runtime / `0.889x` total, то есть ввод тоже уже выведен в целевой диапазон по обеим метрикам. |
| Процедуры, функции и замыкания | `samples/operators/sub`, `samples/operators/function` | `samples/smoke/engine-compare/routine_closure_pipeline.axm` | выделенный | Адресный сценарий нагружает прямые вызовы `SUB`/`FUNCTION`, вызовы через function-value, returned closure, shared-state closure и nested `SUB` с захватом внешнего локаля. После кэша привязки захватов, покадрового кэша capture-slot массивов, шаблонов локалов и повторного использования `BytecodeCallFrame` длинный прогон `20` итераций с `3` прогревами держится около `0.861x` runtime / `0.874x` total, то есть слой уже уверенно вошёл в целевой диапазон `<= 0.900x`. |
| Игровая логика на сетке без графики | `samples/graphics/game/maze_chase` | `samples/smoke/engine-compare/maze_chase_grid_logic.axm` | выделенный | Новый адресный сценарий выделяет из `maze_chase` строковую карту лабиринта, клеточное движение, повороты, тоннели, выбор хода призрака и съедание точек без `CANVAS`, спрайтов и клавиатуры. Короткий прогон `3` итерации с `1` прогревом уже показал около `0.683x` runtime / `0.694x` total, то есть сама игровая VM-логика на этой сетке не является долгом и уже заметно быстрее интерпретатора. |
| Игровой ввод maze_chase без графики | `samples/graphics/game/maze_chase`, `samples/functions/keypress`, `samples/functions/keydown`, `samples/operators/on-key` | `samples/smoke/engine-compare/maze_chase_input_polling.axm` | выделенный | Новый адресный сценарий выделяет из `maze_chase` именно клавиатурный подслой без графики и без логики лабиринта: опрос `KEYPRESS`, `KEYDOWN`, проверку верхнего и нижнего регистра и типичный игровой цикл чтения направления. Длинный прогон `20` итераций с `3` прогревами держится около `0.739x` runtime / `0.742x` total, то есть и игровой ввод сейчас уже не является долгом `BytecodeVm`. |
| Смешанный игровой кадр maze_chase без ввода | `samples/graphics/game/maze_chase` | `samples/smoke/engine-compare/maze_chase_mixed_frame.axm` | выделенный | Новый адресный сценарий выделяет из `maze_chase` уже не только логику, а повторяемый кадр с `DRAWIMAGE`, `DRAWSPRITE`, `FILLCIRCLE`, `FILLRECT`, `DRAWLINE`, `DRAWTEXT`, HUD и детерминированным движением без живой клавиатуры. Короткий прогон `3` итерации с `1` прогревом показал около `0.732x` runtime / `0.735x` total, то есть и смешанный игровой кадр сейчас тоже не является долгом `BytecodeVm`. |
| Современная графика `CANVAS` и частые примитивы кадра | `samples/graphics/modern/hud_text`, `camera_world`, `lines_and_circles`, `samples/graphics/game/maze_chase` | `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm` | выделенный | Добавлен первый адресный сценарий современного графического цикла без ручного ввода. После перевода частых графических опкодов VM на прямой `PopUnchecked()`-путь без общего `PopValue(...)` короткий прогон `3` итерации с `1` прогревом держится около `0.793x` runtime / `0.812x` total, то есть байткод уже заметно быстрее интерпретатора и на современном `CANVAS`-пути. |
| Многомерные массивы и metadata built-in | `samples/smoke/multidimensional-arrays`, `samples/functions/arrayrank`, `arraybounds`, `lbound`, `ubound`, `arrayreshape`, `arrayflatten` | `samples/smoke/engine-compare/array_cube_rollup.axm`, `high_rank_metadata.axm` | выделенный | Есть и сценарий на плотную многомерную обработку, и отдельный усиленный сценарий на metadata. По `high_rank_metadata.axm` VM держится около `0.540x` runtime / `0.563x` total. |
| Пользовательские `TYPE` и доступ к полям | `samples/operators/type/field_access`, `array_fields`, `fixed_array_fields`, `fixed_string_fields`, `grouped_fields` | `samples/smoke/engine-compare/type_record_roundtrip.axm` | выделенный | Для записей, вложенных полей, фиксированных строковых полей и фиксированных массивных полей теперь есть отдельный сценарий. После перевода `RuntimeRecordValue` на ordinal-слоты и кэша record-полей в VM `type_record_roundtrip.axm` держится около `0.891x` runtime / `0.888x` total, то есть record-слой уже переведён из рабочей серой зоны в целевой диапазон. |

## Ближайшие дыры

1. По матрице ещё полезно постепенно дробить слишком широкие семейства на более узкие адресные сценарии, когда появится новый профильный сигнал, а не только косвенное участие в `mixed`.
2. После последних правок scalar-core уже не просто в рабочем паритете, а в целевом диапазоне: `scalar_assignment_expression_mesh.axm` теперь держится около `0.853x` runtime / `0.860x` total. Новый string primitive-scan, string concat accumulator, array-search probe, а также адресные file path/info-sweep, open-file stream cycle, drive-volume metadata, temp-storage roundtrip, отдельный bitwise/shift mesh, management-roundtrip, игровой ввод `maze_chase` и новый mixed-frame `maze_chase` держатся примерно на `0.398x` / `0.476x`, `0.522x` / `0.534x`, `0.852x` / `0.857x`, `0.768x` / `0.773x`, `0.813x` / `0.834x`, `0.724x` / `0.736x`, `0.960x` / `0.960x`, `0.855x` / `0.856x`, `0.996x` / `0.996x`, `0.739x` / `0.742x` и `0.732x` / `0.735x` соответственно. Значит, среди уже выделенных неграфических адресных семейств прямого долга `> 1.000x` больше не осталось, а `maze_chase` уже разложен на три отдельные быстрые подсистемы: сетка, ввод и смешанный кадр.
3. После успешного аудита `audit-bytecode-coverage` можно целиться уже не в синхронизацию существующих реестров, а в новые benchmark-семейства или в новый профильный сигнал от реальных программ.

## Обновление - 2026-06-06 - maze_chase получил отдельный адресный benchmark ввода

Для игрового примера `samples/graphics/game/maze_chase.axm` добавлен третий адресный сценарий `samples/smoke/engine-compare/maze_chase_input_polling.axm`. Он выделяет именно клавиатурный подслой без графики и без логики движения по карте: `KEYPRESS`, `KEYDOWN`, проверку верхнего и нижнего регистра, а также типичный игровой цикл опроса направления.

Чтобы этот сценарий измерялся без ручного участия, `Axiom.ManualChecks` получил benchmark-case `Игровой ввод: maze_chase без графики` с отдельным scripted input-provider.

Длинный прогон `20` итераций с `3` прогревами дал около `0.739x` runtime / `0.742x` total, а опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-224029.txt` показал, что верхняя нагрузка сидит не в графике, а в `KEYPRESS`, `UCASE`, `LCASE`, `KEYDOWN` и соседней управляющей логике.

Практический вывод теперь такой:
- `maze_chase` уже разложен на три отдельные измеряемые подсистемы: сеточная логика, клавиатурный ввод и смешанный графический кадр;
- по всем трём адресным срезам `BytecodeVm` уже быстрее интерпретатора;
- возвращаться к modern graphics или к `maze_chase` ради слепого разгона больше не нужно, пока не появится новый живой профильный сигнал из ещё невыделенного игрового подслоя.


## Обновление - 2026-05-12 - накопление строк

Добавлен адресный сценарий `samples/smoke/engine-compare/string_concat_accumulator.axm` для пользовательского шаблона `s = s & expr` и `s = s + expr` в цикле. Он дополняет строковые built-in: теперь отдельно измеряется не только работа функций над строками, но и дорогое посимвольное или покусковое построение результата. Короткий проверочный прогон `compare-engines --iterations 1 --warmup 0 --filter "string_concat_accumulator.axm"` прошёл успешно: runtime ratio около `1.000x`, total ratio около `0.301x`. Для устойчивого ориентира нужен длинный прогон с прогревом после следующих правок VM.

## Уточнение - 2026-05-12 - сценарий накопления через `&`

Сценарий `samples/smoke/engine-compare/string_concat_accumulator.axm` уточнён так, чтобы ветка `&` прямо попадала в распознанный шаблон накопления: вместо единой цепочки `textAmp = textAmp & i & ":"` используются две последовательные append-операции. Повторный короткий прогон `compare-engines --iterations 1 --warmup 0 --filter "string_concat_accumulator.axm"` прошёл успешно: runtime ratio около `0.860x`, total ratio около `0.251x`. Длинный прогон с прогревом по-прежнему нужен для устойчивого числа.

## Обновление - 2026-06-06 - накопление строк подтверждено длинным прогоном

Повторный длинный прогон `samples/smoke/engine-compare/string_concat_accumulator.axm` (`20` итераций, `3` прогрева) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-074415.txt` дал уже устойчивое число около `0.522x` runtime / `0.534x` total.

Практический вывод теперь такой:
- строковое накопление через `s = s & expr` и `s = s + expr` в цикле уже не просто в рабочем паритете, а глубоко в целевом диапазоне;
- возвращаться к этому сценарию как к долгу VM не нужно;
- следующий производительный поиск имеет смысл вести уже вне строкового накопления.

## Обновление - 2026-06-06 - drive_temp_storage_sweep очищен от строкового шума

Опкодный профиль старой версии `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` показал, что сценарий слишком заметно мерил не файловый слой, а строковую подготовку payload: вверху сидели `CHR`, `STR` и `TRIM` из локальной функции `BuildPayload(...)`.

После этого сценарий упрощён: временные файлы теперь пишутся заранее подготовленными строками без лишней сборки текста внутри цикла. Новый профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-074737.txt` уже показывает сверху именно файловые и смежные built-in: `WRITEFILE`, `DIRSIZE`, `FREESPACE`, `MAKETEMPFILE`, `MAKETEMPDIR`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `PATHROOT`.

Повторный длинный прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-074752.txt` дал около `0.921x` runtime / `0.921x` total вместо прежних `0.992x` / `0.994x`.

Практический вывод теперь такой:
- сам слой disk/temp built-in выглядит лучше, чем подсказывало старое число;
- ближайший файловый measured-кандидат теперь скорее `file_management_roundtrip.axm`, а не `drive_temp_storage_sweep.axm`;
- к `drive_temp_storage_sweep.axm` есть смысл возвращаться уже только для точечного дожима от `0.921x` к целевому диапазону `<= 0.900x`, а не как к почти-проигрывающему слою.

## Обновление - 2026-06-06 - file_management_roundtrip стабилизирован и вернулся в прямой долг

Редкий пустой `Output` у `samples/smoke/engine-compare/file_management_roundtrip.axm` оказался не графическим режимом и не особым сбоем `SCREEN`. Локальный probe показал, что межпрогонный конфликт рождался внутри самого файлового сценария, когда разные запуски цеплялись за общий каталог под `.artifacts/manual-checks/file-management-roundtrip/`.

После этого сценарий переведён на уникальный временный корень через `MAKETEMPDIR("file-management-roundtrip_")`. Повторный probe перестал ловить межпрогонные конфликты, а официальный длинный прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-084645.txt` уже прошёл чисто и дал около `1.075x` runtime / `1.074x` total.

Новый адресный опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-084759.txt` показал, что верхняя нагрузка теперь действительно сидит в management-builtins:
- `DELETEFILE`: `270`;
- `DELETEDIRRECURSIVE`: `181`;
- `CHANGEDIR`: `90`;
- `WRITEFILE`: `61`;
- `CURRENTDIR`: `46`;
- `COPYDIR`, `COPYFILE`, `MOVEDIR`, `MOVEFILE`, `RENAMEDIR`, `RENAMEFILE`: по `45`.

Практический вывод теперь такой:
- нестабильность benchmark-а для этого сценария снята именно на уровне файлового окружения, а не через гипотезу про `SCREEN`;
- management-сценарий больше не надо считать «почти паритетом с шумом»;
- следующий точный шаг по уже измеренным файловым семействам должен идти в адресный разгон `DELETEFILE`, `DELETEDIRRECURSIVE`, `CHANGEDIR`, `CURRENTDIR`, `WRITEFILE` и соседних `COPY*` / `MOVE*` / `RENAME*`.

## Обновление - 2026-06-06 - file_management_roundtrip ускорен до рабочего паритета

После опкодного профиля management-builtins в `BytecodeVm` добавлен прямой fast-path для файловых built-in управления файлами и каталогами. На успешном пути VM теперь берёт аргументы прямо со стека `BytecodeEvaluationStack` и не проходит через общий `CallBuiltin`-dispatch, а диагностический `ResolveLocation(...)` остаётся только на ошибочном откате.

Адресные runtime-проверки по файловым built-in после этой правки прошли, а новый длинный прогон `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-085758.txt` уже дал около `0.969x` runtime / `0.969x` total вместо прежних `1.075x` / `1.074x`.

Практический вывод теперь такой:
- прямой долг `file_management_roundtrip.axm` снят;
- слой управления файлами и каталогами переведён в рабочий паритет;
- если продолжать measured-разгон вне графики, то ближайшим уже выделенным серым слоем теперь снова остаётся `drive_temp_storage_sweep.axm`, а не management-roundtrip.

## Обновление - 2026-06-06 - файловый disk/temp слой разложен на два отдельных адресных подслоя

После повторных колебаний `drive_temp_storage_sweep.axm` стало видно, что смешанный сценарий по-прежнему скрывает разные по характеру расходы. Поэтому файловое семейство разложено ещё на два адресных сценария:
- `samples/smoke/engine-compare/drive_volume_metadata_sweep.axm` для `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE` и `PATHROOT`;
- `samples/smoke/engine-compare/temp_storage_roundtrip.axm` для `MAKETEMPFILE`, `MAKETEMPDIR`, `MAKEDIR`, `COMBINEPATH`, `WRITEFILE`, `FILESIZE`, `DIRSIZE`, `DELETEFILE` и `DELETEDIRRECURSIVE`.

Длинние прогоны дали такой честный разрез:
- `drive_volume_metadata_sweep.axm`: около `0.724x` runtime / `0.736x` total;
- `temp_storage_roundtrip.axm`: на длинном контроле `40` итераций с `5` прогревами около `0.960x` runtime / `0.960x` total.

Опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-091743.txt` для `temp_storage_roundtrip.axm` показал, что в горячем ядре остаются прежде всего `COMBINEPATH` и `WRITEFILE`, а затем `DELETEDIRRECURSIVE`, `DELETEFILE`, `DIRSIZE`, `FILESIZE`, `MAKEDIR`, `MAKETEMPDIR` и `MAKETEMPFILE`.

Практический вывод теперь такой:
- сам drive/volume-подслой уже не просто в паритете, а уверенно в целевом диапазоне;
- серый слой этого файлового семейства сидит именно в temp/file roundtrip, а не в `DriveInfo`;
- следующий measured-дожим вне графики логично вести уже по `temp_storage_roundtrip.axm`, а не по старому смешанному `drive_temp_storage_sweep.axm`.

## Обновление - 2026-06-06 - `COMBINEPATH` дожал temp_storage_roundtrip до нижней границы целевого диапазона

Для `temp_storage_roundtrip.axm` в `BytecodeVm` добавлен прямой fast-path только для `COMBINEPATH`, без расширения на более шумный пакет временных файловых built-in. После этой правки короткие длинные прогоны ещё колебались, поэтому за опорный ориентир взят более длинный контроль `40` итераций с `5` прогревами: `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-093207.txt`.

Этот контроль дал около `0.960x` runtime / `0.960x` total. Значит, temp/file roundtrip всё ещё не выглядит глубоко быстрым слоем, но уже устойчиво держится лучше прямого долга и остаётся в рабочем паритете у самой границы целевого диапазона.

Практический вывод теперь такой:
- `COMBINEPATH` дал измеримый вклад именно на честном temp/file-срезе;
- расширять fast-path на весь temp/file пакет без более сильного сигнала пока не стоит;
- следующий точный кандидат внутри этого сценария теперь скорее `WRITEFILE`, а не ещё одна широкая пачка built-in сразу.

## Обновление - 2026-06-06 - `DIRSIZE` вытянул size/cleanup-подслой из прямого долга

После разреза temp/file-семейства на более узкие probe-сценарии стало видно, что `FILESIZE` уже отдельно держится около `0.884x` / `0.885x`, а `DIRSIZE` остаётся ближе к серой зоне: первый `temp_dirsize_probe.axm` держался около `0.987x` / `0.987x`.

Для `DIRSIZE` в `BytecodeVm` добавлен отдельный прямой fast-path. После этого `temp_dirsize_probe.axm` стал держаться около `0.923x` runtime / `0.923x` total, а смешанный `temp_size_cleanup_cycle.axm` на длинном контроле `40/5` ушёл с `1.058x` / `1.064x` до примерно `0.983x` / `0.982x`.

Практический вывод теперь такой:
- размерный подслой temp/file-семейства больше не является прямым долгом VM;
- `FILESIZE` и `DIRSIZE` уже не выглядят ближайшими узкими кандидатами на разгон;
- следующий measured-шаг внутри temp/file-срезов логично переносится к `temp_creation_write_cycle.axm`, где сверху остаются `COMBINEPATH` и `WRITEFILE`.

## Обновление - 2026-06-06 - отдельный `WRITEFILE`-probe не подтвердил нового долга

После разреза creation/write-половины temp/file-семейства добавлены два ещё более узких сценария:
- `samples/smoke/engine-compare/temp_path_creation_probe.axm` для `MAKETEMPFILE`, `MAKETEMPDIR`, `MAKEDIR` и `COMBINEPATH` почти без size/cleanup-слоя;
- `samples/smoke/engine-compare/temp_writefile_probe.axm` для повторной записи уже подготовленных временных путей.

Короткий прогон `20/3` для `temp_writefile_probe.axm` один раз дал шумный серый результат около `1.015x`, поэтому он не принят как опорный ориентир. Более длинний контроль `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-100158.txt` (`40` итераций, `5` прогревов) показал уже около `0.929x` runtime / `0.929x` total, а `temp_path_creation_probe.axm` держится около `0.998x` runtime / `0.996x` total.

Практический вывод теперь такой:
- отдельный хвост у `WRITEFILE` на длинном контроле не подтвердился;
- creation/write-половина temp/file-семейства остаётся в рабочем паритете, но без нового прямого долга `> 1.000x`;
- следующий measured-шаг вне графики уже полезнее искать не слепым дожимом `WRITEFILE`, а в другом ещё невыделенном неграфическом family или по новому профильному сигналу.

## Обновление - 2026-05-12 - табличная замена многих строк

Для строкового слоя добавлен пользовательский производительный рычаг `REPLACEMANY(text, oldItems, newItems)`: он не является отдельным `engine-compare`-сценарием, но закрывает реальный профильный сигнал из `samples/smoke/repair_plans_mojibake.axm`. Вместо сотен последовательных `REPLACE`, каждый из которых заново проходит по документу, восстановитель теперь применяет одну таблицу правил за один проход по строке.

Следующий полезный замер: повторить `samples/smoke/repair_plans_mojibake.axm` на полном рабочем документе и сравнить с предыдущими ручными числами около `40+` секунд в IDE. Если этот сценарий снова станет узким местом, следующим кандидатом будет кэширование подготовленных правил `REPLACEMANY` между вызовами одной и той же таблицы.

## Обновление - 2026-06-06 - современная графика изображений и спрайтов

Для современного графического слоя добавлен второй отдельный сценарий `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm`. Он детерминированно нагружает `LOADIMAGE`, `LOADSPRITE`, `DRAWIMAGE`, `DRAWSPRITE`, `CAMERA`, `RESETCAMERA`, `BEGINFRAME`, `PRESENT` и HUD-текст без ручного ввода.

Короткий прогон `compare-engines --iterations 3 --warmup 1 --filter "modern_canvas_sprite_burst.axm"` сначала показал рабочий паритет около `0.947x` runtime / `0.960x` total, но длинный контрольный прогон `20` итераций с `3` прогревами дал уже устойчивое число около `1.056x` runtime / `1.032x` total. Значит, примитивный `CANVAS`-путь уже находится в целевом диапазоне, а спрайтовый и image-путь пока ещё остаётся отдельным прямым долгом VM.

Практический вывод по матрице теперь такой:
- `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm` закрывает быстрые примитивы кадра и подтверждает заметное преимущество `BytecodeVm`;
- `samples/smoke/engine-compare/modern_canvas_sprite_burst.axm` закрывает отдельную семью `DRAWIMAGE` / `DRAWSPRITE`, но пока оставляет её в зоне следующей оптимизации;
- следующий профильный шаг для современной графики должен идти уже не через ещё один benchmark, а через измерение и ускорение именно VM/host-пути изображений и спрайтов.

## Обновление - 2026-06-06 - современная графика подтянута на image/sprite-пути

Для `BytecodeVm` добавлен безопасный быстрый путь для графических built-in `RGB`, `LOADIMAGE` и `LOADSPRITE`: теперь VM сначала смотрит аргументы через `PeekUnchecked()`, а снимает их со стека только на успешном пути. Для самих `DRAWIMAGE` и `DRAWSPRITE` добавлен прямой integer/long-путь без лишних общих `AsLong()`-конверсий на типичном случае benchmark-сценария.

Параллельно benchmark-host `BenchmarkGraphicsHost` получил более быстрый растеризатор `DrawImageRegion(...)`: отдельный прямой путь 1:1 без масштабирования и инкрементальный fixed-point путь для масштабирования вместо деления на каждом пикселе.

После этих правок длинный контрольный прогон `compare-engines --iterations 20 --warmup 3 --filter "modern_canvas_"` дал такие числа:
- `modern_canvas_primitive_burst.axm`: около `0.980x` runtime / `0.976x` total;
- `modern_canvas_sprite_burst.axm`: около `1.033x` runtime / `1.031x` total.

Практический вывод теперь такой:
- быстрые примитивы современного `CANVAS` остаются в рабочем целевом диапазоне и после новых правок;
- отдельный image/sprite-путь заметно подтянут относительно прежних `1.056x` / `1.032x`, но длинный прогон всё ещё оставляет небольшой хвост около трёх процентов;
- следующим шагом уже не нужен новый benchmark-сценарий: надо профилировать оставшуюся разницу именно в цепочке `BytecodeVm.Graphics -> DrawImage/DrawSprite`.

## Обновление - 2026-06-06 - добавлен профиль опкодов и ускорен горячий `Convert`

Для `Axiom.ManualChecks` добавлена отдельная команда `profile-bytecode-opcodes`. Она компилирует выбранный `engine-compare`-сценарий в байткод, запускает его через `BytecodeVm` с наблюдателем инструкций и пишет отдельный отчёт по частотам опкодов, built-in и процедур.

Профиль `modern_canvas_sprite_burst.axm` показал, что текущий хвост сидит не в самих вызовах `DRAWIMAGE` / `DRAWSPRITE`, а в окружающей арифметике benchmark-сценария:
- `PushConst`: `26211` наблюдений;
- `LoadLocal`: `23969`;
- `Add`: `14160`;
- `Convert`: `9762`;
- `Multiply`: `9520`;
- `Modulo`: `8120`;
- `DrawSprite`: `1440`;
- `DrawImage`: `960`.

После этого в `BytecodeVm` ускорен `Opcode.Convert`: успешные преобразования `INTEGER`, `LONG`, `DOUBLE` и `DECIMAL` теперь идут через прямой `PopUnchecked()` и без раннего `ResolveLocation(...)` на типичном успешном пути.

Контрольные числа после этой правки получились такими:
- отдельный длинный прогон `modern_canvas_sprite_burst.axm` один раз дал около `0.988x` runtime / `0.979x` total;
- повторный длинный одиночный прогон того же сценария дал около `1.060x` runtime / `1.054x` total;
- полный длинный прогон `modern_canvas_` дал около `0.874x` runtime / `0.899x` total для `modern_canvas_primitive_burst.axm` и около `1.044x` runtime / `1.040x` total для `modern_canvas_sprite_burst.axm`.

Практический вывод на текущем шаге такой:
- ускорение `Convert` полезно и локально подтверждается адресным прогоном;
- примитивный `CANVAS`-путь уже уверенно быстрее интерпретатора;
- image/sprite-путь всё ещё шумный и пока не даёт устойчиво повторяемого выигрыша на каждом длинном прогоне, поэтому остаётся отдельным производительным долгом;
- следующим шагом нужно профилировать уже не семейство `DRAWIMAGE` / `DRAWSPRITE` в целом, а конкретные частые формы `Convert`, `Add`, `Multiply` и `Modulo`, которые окружают спрайтовый цикл.

## Обновление - 2026-06-06 - лишний Convert(Long) вокруг modern graphics снят

Профиль `modern_canvas_sprite_burst.axm` после целочисленного lowering для modern graphics и согласования `BytecodeVm` с контрактом `INTEGER|LONG` резко очистился:
- `Convert` упал с `9762` до `80`;
- в hot-path остались в основном `PushConst`, `LoadLocal`, `Add`, `Multiply`, `Modulo`, `DrawSprite` и `DrawImage`.

Длинные контрольные прогоны `20` итераций с `3` прогревами после этой правки дали такой срез:
- `modern_canvas_primitive_burst.axm`: около `0.823x` runtime / `0.826x` total;
- `modern_canvas_sprite_burst.axm`: около `0.932x` runtime / `0.934x` total;
- отдельный длинний прогон только `modern_canvas_sprite_burst.axm`: около `0.981x` runtime / `1.013x` total.

Практический вывод теперь такой:
- быстрые примитивы `CANVAS` подтверждённо остаются в целевом диапазоне;
- отдельный image/sprite-путь больше не является прямым долгом VM, потому что на полном адресном прогоне `modern_canvas_` он уже быстрее интерпретатора;
- следующий возможный шаг по современной графике лежит уже не в `DRAWIMAGE` / `DRAWSPRITE` и не в `Convert(Long)`, а в окружающей арифметике sprite/image-цикла.

## Обновление - 2026-06-06 - metadata built-in современной графики выделен в отдельный быстрый подслой

Для современного графического слоя добавлен третий адресный сценарий `samples/smoke/engine-compare/modern_graphics_metadata_sweep.axm`. Он детерминированно нагружает `IMAGEWIDTH`, `IMAGEHEIGHT`, `SPRITEWIDTH`, `SPRITEHEIGHT` и `SPRITECOUNT` поверх пустых и загруженных `IMAGE` / `SPRITE`.

В `BytecodeVm` эти metadata built-in переведены на прямой fast-path без общего `CallBuiltin`-dispatch. Длинный прогон `20` итераций с `3` прогревами дал около `0.447x` runtime / `0.455x` total, то есть подслой оказался одним из самых быстрых во всей современной графике.

Практический вывод теперь такой:
- современная графика больше не состоит только из двух адресных семей: теперь отдельно измерены быстрые примитивы кадра, image/sprite-рисование и metadata built-in;
- metadata built-in `IMAGE` / `SPRITE` подтверждённо не являются новым долгом VM;
- по текущему набору modern graphics уже не осталось подтверждённого адресным benchmark-ом подслоя, который стабильно проигрывает интерпретатору.

## Обновление - 2026-06-06 - побитовый подслой тоже вошёл в целевой диапазон

Повторный длинный прогон `samples/smoke/engine-compare/bitwise_shift_mesh.axm` (`20` итераций, `3` прогрева) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-073910.txt` дал уже около `0.855x` runtime / `0.856x` total вместо прежних `0.968x` / `0.968x`.

Опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-073910.txt` при этом показал, что нагрузка действительно сидит в ожидаемом низкоуровневом слое: `LoadLocal`, `StoreLocal`, `PushConst`, `And`, `ShiftLeft`, `Xor`, `Or`, `Convert`, `Modulo` и `ShiftRight`.

Практический вывод теперь такой:
- отдельный побитовый benchmark больше не является прямым или даже пограничным долгом `BytecodeVm`;
- возвращаться к `bitwise_shift_mesh.axm` имеет смысл только по новому профильному сигналу или если понадобится уже адресный микродожим без смены общего приоритета;
- ближайший следующий долг VM нужно искать уже вне `maze_chase` и вне побитового подслоя.

## Обновление - 2026-06-06 - `array_staging_roundtrip` подтвердил, что staging через массивы не является хвостом

Для массива преобразований добавлен и измерен отдельный сценарий `samples/smoke/engine-compare/array_staging_roundtrip.axm`. Он намеренно не трогает файловый слой и нагружает только двухфазный staging через массивы строк и чисел: первичную запись в массивы, повторное чтение, вычисление контрольной суммы и обратную дозапись в те же ячейки.

Длинный прогон `20` итераций с `3` прогревами по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-094149.txt` дал около `0.474x` runtime / `0.580x` total.

Практический вывод теперь такой:
- staging через массивы сам по себе не является производительным долгом `BytecodeVm`;
- прежний temp/file-хвост не был замаскирован чисто массивным промежуточным слоем;
- следующий measured-шаг вне графики полезнее искать уже не в массивном staging, а в другом ещё невыделенном неграфическом family или по новому профильному сигналу.

## Обновление - 2026-06-06 - shortcut нормализации путей не дал устойчивого выигрыша

После документной синхронизации был отдельно проверен ещё один внутренний рычаг VM: shortcut для `NormalizeFileSystemPath(...)`, который пропускал бы повторный `ExpandEnvironmentVariables + GetFullPath` на уже абсолютных канонических путях без `%` и без сегментов `.` / `..`.

Локальные числа сначала выглядели обнадёживающе:
- `file_management_roundtrip.axm` на длинном контроле `20/3` дал около `0.960x` runtime / `0.960x` total;
- `temp_path_creation_probe.axm` на длинном контроле `40/5` дал около `0.907x` runtime / `0.904x` total, а повторный прогон около `0.922x` / `0.923x`.

Но решающий длинный контроль `samples/smoke/engine-compare/temp_storage_roundtrip.axm` на `40` итерациях с `5` прогревами по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-102811.txt` дал уже около `1.011x` runtime / `1.012x` total, то есть смешанный temp/file-срез ушёл обратно в прямой долг.

Практический вывод теперь такой:
- shortcut нормализации путей нельзя считать подтверждённым улучшением;
- локальный выигрыш на отдельных path/management-срезах оказался недостаточно устойчивым;
- этот путь не стоит фиксировать в `BytecodeVm`, а следующий measured-шаг лучше искать уже не в shortcut-нормализации пути, а по другому профильному сигналу внутри файлового слоя.

## Обновление - 2026-06-06 - быстрый `CURRENTDIR` заметно подтянул cleanup/navigation-срез

После того как `file_cleanup_navigation_probe.axm` был отдельно выделен и очищен от лишнего строкового и path-шумa, следующим узким дожимом стала точка `CURRENTDIR` в fast-path `BytecodeVm`: на успешном пути ему больше не нужна повторная нормализация уже текущего каталога.

Точечные runtime-проверки файлового слоя прошли, а новый длинный прогон `20` итераций с `3` прогревами по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-103726.txt` дал уже около `0.917x` runtime / `0.918x` total вместо прежних примерно `0.968x` / `0.969x` на старой версии того же cleanup/navigation-среза.

Опкодный профиль `C:\Axiom\.artifacts\manual-checks\axiom-bytecode-opcode-profile-20260606-103832.txt` при этом показывает честный верх именно по нужному подслою: `CURRENTDIR`, `CHANGEDIR`, `DELETEDIRRECURSIVE`, `DELETEFILE`, а не по широкому mixed-roundtrip.

Практический вывод теперь такой:
- узкий fast-path для `CURRENTDIR` подтверждён отдельным адресным сценарием;
- cleanup/navigation-подслой файлов заметно подтянут и уже близок к целевому диапазону;
- следующим measured-кандидатом внутри этого подслоя теперь логично оставлять `CHANGEDIR`, а затем уже `DELETEFILE` и `DELETEDIRRECURSIVE`.

## Обновление - 2026-06-06 - `CHANGEDIR` тоже дожал узкий cleanup/navigation-подслой

После дожима `CURRENTDIR` следующим узким шагом внутри того же fast-path VM стал `CHANGEDIR`: на успешном пути ему больше не нужен отдельный предварительный `Directory.Exists(...)`, потому что отсутствие каталога можно отличить по `DirectoryNotFoundException`, сохранив прежний пользовательский договор для несуществующего пути.

Точечные runtime-проверки `CURRENTDIR` / `CHANGEDIR` прошли, а новый длинный прогон `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm` (`20` итераций, `3` прогрева) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104234.txt` дал уже около `0.900x` runtime / `0.901x` total.

Широкий `file_management_roundtrip.axm` после этого на отдельном прогоне `20/3` по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104325.txt` всё ещё держится только около `0.975x` runtime / `0.974x` total, так что этот выигрыш пока честно подтверждён именно на узком cleanup/navigation-срезе, а не на всём management-family сразу.

Практический вывод теперь такой:
- узкий fast-path `CHANGEDIR` подтверждён на адресном cleanup/navigation-сценарии;
- этот подслой практически вошёл в целевой диапазон по runtime и стоит прямо у границы по total;
- следующий measured-кандидат внутри него теперь уже логично переносится на `DELETEFILE` и `DELETEDIRRECURSIVE`.

## Обновление - 2026-06-06 - `DELETEFILE` и `DELETEDIRRECURSIVE` перевели cleanup/navigation-срез в уверенно быстрый режим

Следующим узким шагом внутри того же файлового fast-path стали `DELETEFILE` и `DELETEDIRRECURSIVE`: из успешного пути VM убрана лишняя предварительная проверка существования, а отсутствие файла или каталога теперь различается по исключениям самого `File.Delete(...)` и `Directory.Delete(...)`, без изменения пользовательского договора на возврат `FALSE`.

Точечные runtime-проверки файлового слоя снова прошли, а новый длинный прогон `samples/smoke/engine-compare/file_cleanup_navigation_probe.axm` (`20` итераций, `3` прогрева) по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-104930.txt` дал уже около `0.816x` runtime / `0.818x` total.

Отдельный широкий контроль `file_management_roundtrip.axm` после этого по отчёту `C:\Axiom\.artifacts\manual-checks\axiom-engine-compare-20260606-105022.txt` держится только около `0.996x` runtime / `0.996x` total, то есть management-family в целом ускорение почти не унаследовал и остаётся лишь около паритета.

Практический вывод теперь такой:
- `DELETEFILE` и `DELETEDIRRECURSIVE` подтверждены как полезный адресный дожим именно в cleanup/navigation-подслое;
- узкий cleanup/navigation-срез файлов теперь уже уверенно быстрее интерпретатора, а не только у границы цели;
- широкий `file_management_roundtrip` пока нельзя считать решённым этим же шагом, поэтому следующий measured-поиск полезнее переносить либо на другой файловый подслой, либо уже вне файлов.
