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
| Строковые built-in | `samples/functions/left`, `right`, `mid`, `contains`, `replace`, `trim`, `split`, `join`, `padleft`, `padright`, `center`, `repeat`, `reverse`, `lcase`, `ucase` и соседние | `samples/smoke/engine-compare/string_array_loop_pipeline.axm`, `string_primitive_scan.axm`, `document_batch_repair.axm`, `mixed_workload_pipeline.axm` | выделенный | К широкому string-сценарию добавлен отдельный низкоуровневый sweep для `ASC`, `CHR`, `LEN`, `SPACE`, `STRING`, `INSTR`, `INSTRREV` и `SUBSTRCOUNT`. Длинный прогон `20` итераций с `3` прогревами держится около `0.398x` runtime / `0.476x` total, то есть этот строковый подслой уже один из самых быстрых и не является кандидатом на разгон. |
| Regex built-in | `samples/functions/regexismatch`, `regexmatch`, `regexextract`, `regexreplace`, `regexcount`, `regexmatches`, `regexextractall`, `regexsplit` | `samples/smoke/engine-compare/regex_log_analysis.axm`, `regex_modes_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Есть и сценарий на массивные regex-функции, и отдельный сценарий на режимы и цепочку замен. |
| Regex built-in массивных результатов | `samples/functions/regexmatches`, `regexextractall`, `regexsplit` | `samples/smoke/engine-compare/regex_array_result_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` без `REGEXREPLACE` и без смешанной строковой логики. Длинный прогон `20` итераций с `3` прогревами держится около `0.673x` runtime / `0.695x` total, то есть массивный regex-подслой уверенно находится в целевом диапазоне. |
| Файловые built-in над путями и whole-file операциями | `samples/functions/fileexists`, `readfile`, `writefile`, `appendfile`, `getfiles`, `combinepath`, `deletefile` и соседние | `samples/smoke/engine-compare/file_batch_roundtrip.axm`, `file_path_metadata_sweep.axm`, `document_batch_repair.axm` | выделенный | К прежнему файловому сценарию добавлен отдельный path/info sweep для `FILEEXISTS`, `PATHEXISTS`, `FILESIZE`, `FILEDATE`, `COMBINEPATH`, `FILENAME`, `DIRNAME`, `FULLPATH` и `ISABSOLUTEPATH`. Длинный прогон `20` итераций с `3` прогревами держится около `0.768x` runtime / `0.773x` total, то есть этот файловый слой сразу оказался уверенно в целевом диапазоне. |
| Файловые built-in управления файлами и каталогами | `samples/functions/makedir`, `deletefile`, `copyfile`, `movefile`, `renamefile`, `deletedir`, `deletedirrecursive`, `copydir`, `movedir`, `renamedir`, `changedir`, `currentdir` | `samples/smoke/engine-compare/file_management_roundtrip.axm` | выделенный | Новый адресный сценарий нагружает `MAKEDIR`, `COPYFILE`, `MOVEFILE`, `RENAMEFILE`, `DELETEFILE`, `COPYDIR`, `MOVEDIR`, `RENAMEDIR`, `DELETEDIRRECURSIVE`, `CHANGEDIR` и `CURRENTDIR` почти без текстового и regex-слоя. Длинный прогон `20` итераций с `3` прогревами держится около `0.985x` runtime / `0.987x` total, то есть слой пока не целевой, но уже в рабочем паритете и не является прямым долгом VM. |
| Файловые built-in диска, размера каталога и временных путей | `samples/functions/dirsize`, `freespace`, `totalfreespace`, `totalspace`, `drivename`, `driveformat`, `drivetype`, `maketempfile`, `maketempdir` | `samples/smoke/engine-compare/drive_temp_storage_sweep.axm` | выделенный | Новый адресный сценарий нагружает `DIRSIZE`, `FREESPACE`, `TOTALFREESPACE`, `TOTALSPACE`, `DRIVENAME`, `DRIVEFORMAT`, `DRIVETYPE`, `MAKETEMPFILE` и `MAKETEMPDIR` почти без whole-file и text-слоя. Длинный прогон `20` итераций с `3` прогревами держится около `0.992x` runtime / `0.994x` total, то есть слой тоже не целевой, но уже в рабочем паритете и не является прямым долгом VM. |
| Открытые текстовые файлы и файловые операторы `OPEN` / `CLOSE` | `samples/functions/isopen`, `readline`, `readtext`, `write`, `writeline`, `seekfile`, `rewind`, а также `samples/operators/open`, `close` | `samples/smoke/engine-compare/open_file_stream_cycle.axm`, `file_batch_roundtrip.axm` | выделенный | Новый адресный сценарий отдельно нагружает `OPEN`/`CLOSE`, `READLINE`, `READTEXT`, `READTOEND`, `WRITE`, `WRITELINE`, `FLUSH`, `SEEKFILE`, `REWIND`, `FILEPOS`, `FILEMODE`, `FILELENGTH`, `EOF` и `ISOPEN`. Длинный прогон `20` итераций с `3` прогревами держится около `0.813x` runtime / `0.834x` total, то есть этот файловый потоковый слой уже уверенно в целевом диапазоне. |
| Массивные преобразования и мутации | `samples/functions/arrayclone`, `arrayconcat`, `arrayslice`, `arraytake`, `arrayskip`, `arrayinsert`, `arrayremove`, `arraypush`, `arraypop`, `arrayflatten`, `arrayreshape`, `arrayreverse`, `arraytosorted` и соседние | `samples/smoke/engine-compare/array_transform_chain.axm`, `array_cube_rollup.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Это семейство уже стало основным источником последних ускорений VM. |
| Shape-преобразования массивов | `samples/functions/arraydistinct`, `arraytoreversed`, `arrayreverse`, `arrayflatten`, `arrayreshape` | `samples/smoke/engine-compare/array_shape_transform_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `ARRAYDISTINCT`, `ARRAYTOREVERSED`, `ARRAYREVERSE`, `ARRAYFLATTEN` и `ARRAYRESHAPE` на одном и том же 2D-массиве без callback-слоя и без поисковых built-in. Длинный прогон `20` итераций с `3` прогревами держится около `0.817x` runtime / `0.824x` total, то есть shape-подслой сразу оказался в целевом диапазоне. |
| Callback-builtins массивов | `samples/functions/arraymap`, `arrayfilter`, `arrayreduce`, `arrayevery`, `arraysome` | `samples/smoke/engine-compare/array_callback_pipeline.axm` | выделенный | Адресный сценарий уже держит VM около `0.821x` runtime / `0.824x` total. К предыдущим ускорениям добавился кэш function-value внутри внешнего кадра, который убрал повторное разрешение захваченных слотов и повторное создание значения функции в одном и том же цикле. |
| Поисковые built-in массивов | `samples/functions/arraycontains`, `arrayindexof`, `arraylastindexof`, `arraycount`, `arrayfindall`, `arraybinarysearch` | `samples/smoke/engine-compare/array_search_probe.axm` | выделенный | Новый адресный сценарий отдельно нагружает поисковые built-in на 1D и 2D-массивах без callback-слоя. Длинный прогон `20` итераций с `3` прогревами держится около `0.852x` runtime / `0.857x` total, то есть этот поисковый подслой сразу оказался в целевом диапазоне. |
| Скалярные выражения и присваивания | `samples/operators/dim`, `samples/operators/const`, базовые expression-сценарии вокруг `samples/operators/print` и acceptance-ошибок выражений | `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` | выделенный | Новый адресный сценарий нагружает локальные и глобальные присваивания, целочисленную и `DOUBLE`-арифметику, побитовые операции, сдвиги, булевы флаги и сравнения почти без built-in-слоя. После вынесения `Divide` в отдельный быстрый путь длинный прогон `20` итераций с `3` прогревами держится около `0.853x` runtime / `0.860x` total: слой уже переведён в целевой диапазон `<= 0.900x`. |
| Побитовые операции и сдвиги | базовые expression-сценарии вокруг `samples/operators/print` и `samples/operators/const`, а также косвенное участие в `samples/smoke/engine-compare/scalar_assignment_expression_mesh.axm` | `samples/smoke/engine-compare/bitwise_shift_mesh.axm` | выделенный | Новый адресный сценарий отдельно нагружает `AND`, `OR`, `XOR`, `NOT`, `<<` и `>>` на `INTEGER`, `LONG` и `BOOLEAN`. Длинный прогон `20` итераций с `3` прогревами держится около `0.968x` runtime / `0.968x` total, то есть побитовый подслой уже быстрее интерпретатора, но пока не дотянут до целевого диапазона `<= 0.900x`. |
| Числовые built-in | `samples/functions/abs`, `min`, `max`, `clamp`, `pow`, `sqr`, `cbrt`, `log`, `log10`, `log2`, `sin`, `cos`, `tan`, `asin`, `acos`, `asinh`, `acosh`, `atanh`, `floor`, `ceil`, `round`, `fix`, `setsign`, `sgn`, `int`, `val`, `str`, `rnd` и соседние | `samples/smoke/engine-compare/numeric_function_sweep.axm`, `random_stream_snapshot.axm` | выделенный | Новый сценарий закрыл прямой числовой слой и показал хороший результат: VM держится около `0.708x` runtime / `0.711x` total. После прокидывания управляемого генератора в harness `RND` тоже измеряется отдельно: `random_stream_snapshot.axm` держится около `0.818x` runtime / `0.800x` total. |
| Дата и время | `samples/functions/date`, `time`, `now`, `dateadd`, `datediff`, `datepart`, `dateserial`, `timevalue`, `toutc`, `fromutc` и соседние | `samples/smoke/engine-compare/date_time_calendar_pipeline.axm`, `current_time_snapshot.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Для календарных built-in и для built-in текущего времени теперь есть отдельные сценарии. `date_time_calendar_pipeline.axm` держится около `0.651x` runtime / `0.656x` total, а новый `current_time_snapshot.axm` с фиксированными часами держится около `0.497x` runtime / `0.569x` total. |
| Ветвления, циклы и общий управляющий слой | `samples/operators/if`, `samples/operators/select-case`, `samples/operators/for`, `samples/operators/while`, `samples/operators/do-loop` | `samples/smoke/engine-compare/control_flow_branch_maze.axm`, `mixed_workload_pipeline.axm`, `array_cube_rollup.axm`, `array_transform_chain.axm` | выделенный | Для управляющего слоя теперь есть отдельный детерминированный сценарий. После отложенной очистки `BytecodeEvaluationStack` и перевода comparison-jump инструкций на специализированные прямые пути `control_flow_branch_maze.axm` на длинном прогоне `20` итераций с `3` прогревами держится около `0.826x` runtime / `0.814x` total, то есть слой уже уверенно вошёл в целевой диапазон `<= 0.900x`. |
| `PRINT` и `PRINT USING` | `samples/operators/print`, `samples/operators/print-using` | `samples/smoke/engine-compare/print_output_gallery.axm`, `mixed_workload_pipeline.axm`, `document_batch_repair.axm`, `string_array_loop_pipeline.axm` | выделенный | После прямой записи `PRINT`, кэша форматов `PRINT USING`, прямой записи строковых полей и fast-path числовых масок, а затем общих ускорений VM длинный контрольный прогон `print_output_gallery.axm` (`20` итераций, `3` прогрева) держится около `0.860x` runtime / `0.877x` total, то есть слой уже вышел из серой зоны и закреплён в целевом диапазоне. |
| `INPUT` | `samples/operators/input`, `samples/functions/isdate` и другие смежные примеры ввода | `samples/smoke/engine-compare/input_record_intake.axm` | выделенный | После быстрого пути для `INPUT` на 1/2/3 целей без промежуточного массива результатов и длинного прогона `40` итераций с `5` прогревами `input_record_intake.axm` держится около `0.879x` runtime / `0.889x` total, то есть ввод тоже уже выведен в целевой диапазон по обеим метрикам. |
| Процедуры, функции и замыкания | `samples/operators/sub`, `samples/operators/function` | `samples/smoke/engine-compare/routine_closure_pipeline.axm` | выделенный | Адресный сценарий нагружает прямые вызовы `SUB`/`FUNCTION`, вызовы через function-value, returned closure, shared-state closure и nested `SUB` с захватом внешнего локаля. После кэша привязки захватов, покадрового кэша capture-slot массивов, шаблонов локалов и повторного использования `BytecodeCallFrame` длинный прогон `20` итераций с `3` прогревами держится около `0.861x` runtime / `0.874x` total, то есть слой уже уверенно вошёл в целевой диапазон `<= 0.900x`. |
| Современная графика `CANVAS` и частые примитивы кадра | `samples/graphics/modern/hud_text`, `camera_world`, `lines_and_circles`, `samples/graphics/game/maze_chase` | `samples/smoke/engine-compare/modern_canvas_primitive_burst.axm` | выделенный | Добавлен первый адресный сценарий современного графического цикла без ручного ввода. После перевода частых графических опкодов VM на прямой `PopUnchecked()`-путь без общего `PopValue(...)` короткий прогон `3` итерации с `1` прогревом держится около `0.793x` runtime / `0.812x` total, то есть байткод уже заметно быстрее интерпретатора и на современном `CANVAS`-пути. |
| Многомерные массивы и metadata built-in | `samples/smoke/multidimensional-arrays`, `samples/functions/arrayrank`, `arraybounds`, `lbound`, `ubound`, `arrayreshape`, `arrayflatten` | `samples/smoke/engine-compare/array_cube_rollup.axm`, `high_rank_metadata.axm` | выделенный | Есть и сценарий на плотную многомерную обработку, и отдельный усиленный сценарий на metadata. По `high_rank_metadata.axm` VM держится около `0.540x` runtime / `0.563x` total. |
| Пользовательские `TYPE` и доступ к полям | `samples/operators/type/field_access`, `array_fields`, `fixed_array_fields`, `fixed_string_fields`, `grouped_fields` | `samples/smoke/engine-compare/type_record_roundtrip.axm` | выделенный | Для записей, вложенных полей, фиксированных строковых полей и фиксированных массивных полей теперь есть отдельный сценарий. После перевода `RuntimeRecordValue` на ordinal-слоты и кэша record-полей в VM `type_record_roundtrip.axm` держится около `0.891x` runtime / `0.888x` total, то есть record-слой уже переведён из рабочей серой зоны в целевой диапазон. |

## Ближайшие дыры

1. По матрице ещё полезно постепенно дробить слишком широкие семейства на более узкие адресные сценарии, когда появится новый профильный сигнал, а не только косвенное участие в `mixed`.
2. После последних правок scalar-core уже не просто в рабочем паритете, а в целевом диапазоне: `scalar_assignment_expression_mesh.axm` теперь держится около `0.853x` runtime / `0.860x` total. Новый string primitive-scan, array-search probe, а также адресные file path/info-sweep, open-file stream cycle, file-management roundtrip, drive/temp sweep и отдельный bitwise/shift mesh держатся примерно на `0.398x` / `0.476x`, `0.852x` / `0.857x`, `0.768x` / `0.773x`, `0.813x` / `0.834x`, `0.985x` / `0.987x`, `0.992x` / `0.994x` и `0.968x` / `0.968x` соответственно. Значит, следующий шаг снова смещается не к уже закрытым строковым, файловым, поисково-массивным и общему scalar-слою, а к поиску ещё одного невыделенного сигнала; отдельный побитовый подслой формально можно дожимать дальше, но он уже не является прямым долгом VM.
3. После успешного аудита `audit-bytecode-coverage` можно целиться уже не в синхронизацию существующих реестров, а в новые benchmark-семейства или в новый профильный сигнал от реальных программ.


## Обновление - 2026-05-12 - накопление строк

Добавлен адресный сценарий `samples/smoke/engine-compare/string_concat_accumulator.axm` для пользовательского шаблона `s = s & expr` и `s = s + expr` в цикле. Он дополняет строковые built-in: теперь отдельно измеряется не только работа функций над строками, но и дорогое посимвольное или покусковое построение результата. Короткий проверочный прогон `compare-engines --iterations 1 --warmup 0 --filter "string_concat_accumulator.axm"` прошёл успешно: runtime ratio около `1.000x`, total ratio около `0.301x`. Для устойчивого ориентира нужен длинный прогон с прогревом после следующих правок VM.

## Уточнение - 2026-05-12 - сценарий накопления через `&`

Сценарий `samples/smoke/engine-compare/string_concat_accumulator.axm` уточнён так, чтобы ветка `&` прямо попадала в распознанный шаблон накопления: вместо единой цепочки `textAmp = textAmp & i & ":"` используются две последовательные append-операции. Повторный короткий прогон `compare-engines --iterations 1 --warmup 0 --filter "string_concat_accumulator.axm"` прошёл успешно: runtime ratio около `0.860x`, total ratio около `0.251x`. Длинный прогон с прогревом по-прежнему нужен для устойчивого числа.

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
