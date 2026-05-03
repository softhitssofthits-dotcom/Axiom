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

## Матрица

| Семейство языка | Обычные sample | Производительный сценарий | Статус | Примечание |
| --- | --- | --- | --- | --- |
| Строковые built-in | `samples/functions/left`, `right`, `mid`, `contains`, `replace`, `trim`, `split`, `join`, `padleft`, `padright`, `center`, `repeat`, `reverse`, `lcase`, `ucase` и соседние | `samples/smoke/engine-compare/string_array_loop_pipeline.axm`, `document_batch_repair.axm`, `mixed_workload_pipeline.axm` | выделенный | Строковый слой уже хорошо нагружен и отдельно измеряется. |
| Regex built-in | `samples/functions/regexismatch`, `regexmatch`, `regexextract`, `regexreplace`, `regexcount`, `regexmatches`, `regexextractall`, `regexsplit` | `samples/smoke/engine-compare/regex_log_analysis.axm`, `regex_modes_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Есть и сценарий на массивные regex-функции, и отдельный сценарий на режимы и цепочку замен. |
| Файловые built-in над путями и whole-file операциями | `samples/functions/fileexists`, `readfile`, `writefile`, `appendfile`, `getfiles`, `combinepath`, `deletefile` и соседние | `samples/smoke/engine-compare/file_batch_roundtrip.axm`, `document_batch_repair.axm` | выделенный | Адресный файловый сценарий уже есть. |
| Открытые текстовые файлы и файловые операторы `OPEN` / `CLOSE` | `samples/functions/isopen`, `readline`, `readtext`, `write`, `writeline`, `seekfile`, `rewind`, а также `samples/operators/open`, `close` | `samples/smoke/engine-compare/file_batch_roundtrip.axm` | выделенный | Отдельный сценарий уже гоняет открытие, чтение, запись и позиционирование. |
| Массивные преобразования и мутации | `samples/functions/arrayclone`, `arrayconcat`, `arrayslice`, `arraytake`, `arrayskip`, `arrayinsert`, `arrayremove`, `arraypush`, `arraypop`, `arrayflatten`, `arrayreshape`, `arrayreverse`, `arraytosorted` и соседние | `samples/smoke/engine-compare/array_transform_chain.axm`, `array_cube_rollup.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Это семейство уже стало основным источником последних ускорений VM. |
| Callback-builtins массивов | `samples/functions/arraymap`, `arrayfilter`, `arrayreduce`, `arrayevery`, `arraysome`, `arrayfindall`, `arraycount`, `arraycontains`, `arrayindexof`, `arraylastindexof`, `arraybinarysearch` | `samples/smoke/engine-compare/array_callback_pipeline.axm` | выделенный | Адресный сценарий после следующего шага уже держит VM около `0.821x` runtime / `0.824x` total. К предыдущим ускорениям добавился кэш function-value внутри внешнего кадра, который убрал повторное разрешение захваченных слотов и повторное создание значения функции в одном и том же цикле. |
| Числовые built-in | `samples/functions/abs`, `min`, `max`, `clamp`, `pow`, `sqr`, `cbrt`, `log`, `log10`, `log2`, `sin`, `cos`, `tan`, `asin`, `acos`, `asinh`, `acosh`, `atanh`, `floor`, `ceil`, `round`, `fix`, `setsign`, `sgn`, `int`, `val`, `str`, `rnd` и соседние | `samples/smoke/engine-compare/numeric_function_sweep.axm`, `random_stream_snapshot.axm` | выделенный | Новый сценарий закрыл прямой числовой слой и показал хороший результат: VM держится около `0.708x` runtime / `0.711x` total. После прокидывания управляемого генератора в harness `RND` тоже измеряется отдельно: `random_stream_snapshot.axm` держится около `0.818x` runtime / `0.800x` total. |
| Дата и время | `samples/functions/date`, `time`, `now`, `dateadd`, `datediff`, `datepart`, `dateserial`, `timevalue`, `toutc`, `fromutc` и соседние | `samples/smoke/engine-compare/date_time_calendar_pipeline.axm`, `current_time_snapshot.axm`, `string_array_loop_pipeline.axm`, `mixed_workload_pipeline.axm` | выделенный | Для календарных built-in и для built-in текущего времени теперь есть отдельные сценарии. `date_time_calendar_pipeline.axm` держится около `0.651x` runtime / `0.656x` total, а новый `current_time_snapshot.axm` с фиксированными часами держится около `0.497x` runtime / `0.569x` total. |
| Ветвления, циклы и общий управляющий слой | `samples/operators/if`, `samples/operators/select-case`, `samples/operators/for`, `samples/operators/while`, `samples/operators/do-loop` | `samples/smoke/engine-compare/control_flow_branch_maze.axm`, `mixed_workload_pipeline.axm`, `array_cube_rollup.axm`, `array_transform_chain.axm` | выделенный | Для управляющего слоя теперь есть отдельный детерминированный сценарий. После ускорения equality-прыжков и их fast-path VM здесь держится около `0.894x` runtime / `0.913x` total, а значит отдельная просадка по ветвлениям и циклам уже снята. |
| `PRINT` и `PRINT USING` | `samples/operators/print`, `samples/operators/print-using` | `samples/smoke/engine-compare/print_output_gallery.axm`, `mixed_workload_pipeline.axm`, `document_batch_repair.axm`, `string_array_loop_pipeline.axm` | выделенный | Для вывода теперь есть отдельный адресный сценарий. `print_output_gallery.axm` держится около `0.941x` runtime / `0.955x` total, то есть и обычный `PRINT`, и `PRINT USING` уже ниже паритета без дополнительной VM-правки. |
| `INPUT` | `samples/operators/input`, `samples/functions/isdate` и другие смежные примеры ввода | `samples/smoke/engine-compare/input_record_intake.axm` | выделенный | После кэша метаданных `INPUT`-инструкции и безаллоцирующего разбора полей VM на адресном сценарии уже ниже паритета: около `0.892x` runtime / `0.928x` total. |
| Многомерные массивы и metadata built-in | `samples/smoke/multidimensional-arrays`, `samples/functions/arrayrank`, `arraybounds`, `lbound`, `ubound`, `arrayreshape`, `arrayflatten` | `samples/smoke/engine-compare/array_cube_rollup.axm`, `high_rank_metadata.axm` | выделенный | Есть и сценарий на плотную многомерную обработку, и отдельный усиленный сценарий на metadata. По `high_rank_metadata.axm` VM держится около `0.540x` runtime / `0.563x` total. |

## Ближайшие дыры

1. По матрице ещё полезно постепенно дробить слишком широкие семейства на более узкие адресные сценарии, когда появится новый профильный сигнал, а не только косвенное участие в `mixed`.
2. Прямые долги по `RND`, `PRINT`, `INPUT`, current-time, управлению и metadata сейчас уже закрыты ниже паритета, так что следующий шаг по матрице скорее в новом профильном семействе или в инфраструктурном аудите, а не в повторном углублении в уже закрытые сценарии.
3. После успешного аудита `audit-bytecode-coverage` можно целиться уже не в синхронизацию существующих реестров, а в новые benchmark-семейства или в новый профильный сигнал от реальных программ.
