# Реестр измерений BytecodeVm

Действующая команда — `benchmark-bytecode`. Она измеряет VM и полное время подготовки/исполнения, выполняет прогрев и повторы, выводит минимум, среднее, медиану и максимум. Стабильность результатов между повторами не заменяет независимое ожидание семантики.

```powershell
dotnet run --project src/Axiom.ManualChecks -- benchmark-bytecode --iterations 3 --warmup 1
```

Команды .NET выполнять последовательно. `profile-bytecode-opcodes` и `audit-bytecode-coverage` остаются отдельными средствами. Каталог существующих нагрузок `samples/smoke/engine-compare/` сохранён как точный путь; 49 файлов перечислены ниже.

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

## Отдельные измерения GPU-CANVAS — 13 октября 2026 года

Существующие семьи modern_canvas_primitive_burst и modern_canvas_sprite_burst остаются нагрузками исполнения BytecodeVm. Для настоящего графического вывода добавлен `scripts/perf/CanvasGpuProbe/`: CPU-контроль и GPU-путь рисуют 500 спрайтов либо 825 тайлов с камерой и текстом в нативном окне 1024 × 768. Измеряются CPU-время процесса, аппаратный интервал команд, полное время кадра, выделения памяти, загрузки текстур и передачи GPU → CPU. Условия, показатели и пределы выводов записаны в `reports/graphics/CANVAS_GPU_MIGRATION.md`. Новые графические показатели не выдаются за ускорение VM и не меняют каталог engine-compare.
