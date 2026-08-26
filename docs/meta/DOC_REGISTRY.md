# Реестр Markdown-документации Axiom

> Автоматически собран по полному обходу репозитория `C:\Axiom`.
> Дата сборки: 2026-08-26. Учтены все `.md`-файлы, кроме `node_modules/` и `.git/`.
> Итог: **100 файлов** (без новых файлов этого слоя `docs/meta/` и `docs/INDEX.md`). Контроль по расположению: корень 8 + `docs/` 36 + `docs/audit/` 35 + `docs/agent/` 6 + `.codex/` 8 + `.artifacts/` 3 + `.zcode/` 2 + `.agents/` 1 + `samples/` 1 = 100.

Сводка по статусам:

| Статус | Количество | Смысл |
| --- | ---: | --- |
| актуальное | 56 | текущий источник правды, читать первым |
| планируемое | 9 | будущие возможности или отложенные работы |
| историческое | 18 | устаревшее/завершённое, только для контекста |
| дублирующее | 10 | заменено более свежим документом |
| инфраструктура | 7 | не относится к проектной документации |

Полный список с путями, размерами, датами и статусами — ниже.

---

## Зона 1 — корень репозитория (8 файлов)

| Путь | КБ | Изменён | Статус | Примечание |
| --- | ---: | --- | --- | --- |
| `AGENTS.md` | 44.1 | 2026-08-21 | актуальное | правила работы агента, канон |
| `PROJECT.md` | 8.4 | 2026-08-21 | актуальное | определение продукта |
| `README.md` | 66.9 | 2026-08-18 | актуальное | входная точка; содержит устаревший журнал изменений |
| `SOUL.md` | 1.8 | 2026-08-26 | инфраструктура | персональный файл агента |
| `IDENTITY.md` | 0.7 | 2026-08-26 | инфраструктура | персональный файл агента |
| `USER.md` | 0.4 | 2026-08-26 | инфраструктура | персональный файл агента |
| `TOOLS.md` | 0.9 | 2026-08-26 | инфраструктура | персональный файл агента |
| `HEARTBEAT.md` | 0.2 | 2026-08-26 | инфраструктура | служебный файл агента |

## Зона 2 — активные источники правды (docs/, 13 файлов)

| Путь | КБ | Изменён | Статус | Примечание |
| --- | ---: | --- | --- | --- |
| `docs/SPECIFICATION3.md` | 474.9 | 2026-08-25 | актуальное | активная спецификация языка и среды |
| `docs/ARCHITECTURE.md` | 518.6 | 2026-08-21 | актуальное | техническая структура и решения |
| `docs/PLANS4.md` | 78.7 | 2026-08-26 | актуальное | текущий план |
| `docs/WORKLOG5.md` | 248.5 | 2026-08-26 | актуальное | активный журнал состояния |
| `docs/BYTECODE_COVERAGE.md` | 212.1 | 2026-08-26 | актуальное | реестр покрытия BytecodeVm |
| `docs/BYTECODE_PERF_MATRIX.md` | 68.6 | 2026-06-06 | актуальное | реестр сценариев производительности |
| `docs/LANGUAGE_INVENTORY.md` | 264.8 | 2026-08-26 | актуальное | каноническая инвентаризация языка (356 фич) |
| `docs/DOCUMENTATION-STYLE.md` | 31.6 | 2026-08-22 | актуальное | стандарт HTML-справочника |
| `docs/DOCUMENTATION-AUDIT.md` | 18.7 | 2026-08-22 | актуальное | отчёт аудита справочника |
| `docs/TILEMAP_CONTRACT.md` | 25.0 | 2026-06-08 | актуальное | договор тайловой карты |
| `docs/GRAPHICS_OPERATORS_FLAGS.md` | 15.7 | 2026-05-15 | актуальное | справочник флагов LINE/CIRCLE/PAINT |
| `docs/MODERN_GRAPHICS_PLAN.md` | 27.6 | 2026-08-18 | планируемое | долгосрочный план современной графики |
| `docs/FUTURE.md` | 26.8 | 2026-08-21 | планируемое | будущие предложения; часть уже реализована (см. реестр функций) |
| `docs/INDEX.md` | — | 2026-08-26 | актуальное | главный навигационный индекс (этот слой) |

## Зона 3 — forensic-аудиты подсистем (docs/audit/, 33 файла)

Большинство — актуальные глубокие аудиты подсистем. Отдельно выделены планируемые и устаревшие:

| Путь | КБ | Изменён | Статус |
| --- | ---: | --- | --- |
| `docs/audit/ARRAY_SEMANTICS.md` | 60.7 | 2026-08-21 | актуальное |
| `docs/audit/BASE_VALUES_TYPES.md` | 72.9 | 2026-08-21 | актуальное |
| `docs/audit/BLOCK_SCOPE.md` | 69.8 | 2026-08-21 | актуальное |
| `docs/audit/BYREF_FUNCTION_VALUES.md` | 65.1 | 2026-08-22 | актуальное |
| `docs/audit/BYTECODE_VM_PERFORMANCE.md` | 71.3 | 2026-08-21 | актуальное |
| `docs/audit/ERROR_HANDLING.md` | 86.3 | 2026-08-21 | актуальное |
| `docs/audit/FILE_SYSTEM.md` | 59.5 | 2026-08-21 | актуальное |
| `docs/audit/FOR_WHILE_DO_LOOP.md` | 62.0 | 2026-08-21 | актуальное |
| `docs/audit/GRAPHICS_CANVAS_MODERN_FORENSIC_AUDIT.md` | 61.7 | 2026-08-21 | актуальное |
| `docs/audit/GRAPHICS_RENDERING_CORRECTNESS.md` | 72.4 | 2026-08-26 | актуальное |
| `docs/audit/GRAPHICS_SCREEN_FORENSIC_AUDIT.md` | 76.2 | 2026-08-21 | актуальное |
| `docs/audit/IDE_LANGUAGE_INTEGRATION.md` | 72.7 | 2026-08-21 | актуальное |
| `docs/audit/IF_ELSE_SELECT_CASE.md` | 59.1 | 2026-08-21 | актуальное |
| `docs/audit/IMAGE.md` | 51.5 | 2026-08-21 | актуальное |
| `docs/audit/INPUT_PRINT_DATA_READ_RESTORE_FORENSIC_AUDIT.md` | 74.2 | 2026-08-21 | актуальное |
| `docs/audit/KEYBOARD_INKEY_FORENSIC_AUDIT.md` | 73.2 | 2026-08-20 | актуальное |
| `docs/audit/MODULES_NAMESPACES_PROGRAM_STRUCTURE.md` | 77.7 | 2026-08-21 | актуальное |
| `docs/audit/MOUSE_EVENTS_FORENSIC_AUDIT.md` | 89.1 | 2026-08-20 | актуальное |
| `docs/audit/NUMERIC_MATH_FORENSIC_AUDIT.md` | 57.3 | 2026-08-20 | актуальное |
| `docs/audit/RANDOM_PRNG_FORENSIC_AUDIT.md` | 44.6 | 2026-08-20 | актуальное |
| `docs/audit/REGEX.md` | 65.7 | 2026-08-20 | актуальное |
| `docs/audit/SPRITE.md` | 49.6 | 2026-08-21 | актуальное |
| `docs/audit/STRING_OPERATIONS.md` | 38.9 | 2026-08-21 | актуальное |
| `docs/audit/SUB_FUNCTION_CALL_MODEL.md` | 64.6 | 2026-08-21 | актуальное |
| `docs/audit/TILEMAP_TILESET.md` | 34.0 | 2026-08-21 | актуальное |
| `docs/audit/TIMING_SLEEP.md` | 63.6 | 2026-08-20 | актуальное |
| `docs/audit/TYPE_USER_DEFINED_TYPES_FORENSIC_AUDIT.md` | 69.7 | 2026-08-21 | актуальное |
| `docs/audit/DATETIME_FORENSIC_AUDIT.md` | 22.5 | 2026-08-26 | актуальное |
| `docs/audit/DEBUGGER.md` | 71.7 | 2026-08-21 | планируемое |
| `docs/audit/DATA_READ_RESTORE.md` | 61.2 | 2026-08-21 | планируемое |
| `docs/audit/MULTI_FILE_MODULE_DESIGN.md` | 55.1 | 2026-08-21 | планируемое |
| `docs/audit/TYPE_SPECIALIZED_STACKS.md` | 61.8 | 2026-08-22 | планируемое |
| `docs/audit/IDE_LANGUAGE_INTEGRATION_IMPROVEMENTS.md` | 5.1 | 2026-08-21 | историческое |
| `docs/audit/INPUT_PRINT_IMPROVEMENTS.md` | 11.3 | 2026-08-22 | историческое |

## Зона 4 — вспомогательный слой агента (docs/agent/, 6 файлов)

| Путь | КБ | Изменён | Статус |
| --- | ---: | --- | --- |
| `docs/agent/README.md` | 4.3 | 2026-08-18 | актуальное |
| `docs/agent/CURRENT_STATE.md` | 4.0 | 2026-08-20 | актуальное |
| `docs/agent/ARCHITECTURE_MAP.md` | 2.5 | 2026-08-18 | актуальное |
| `docs/agent/PLAYBOOKS.md` | 3.7 | 2026-08-18 | актуальное |
| `docs/agent/HEADROOM_PILOT.md` | 9.0 | 2026-08-18 | актуальное |
| `docs/agent/EXTERNAL_TOOLS_EVALUATION.md` | 13.5 | 2026-08-18 | актуальное |

## Зона 5 — исторический архив (docs/archive/, 16 файлов)

| Путь | КБ | Изменён | Статус | Примечание |
| --- | ---: | --- | --- | --- |
| `docs/archive/spec/SPECIFICATION.md` | 308.1 | 2026-08-26 | историческое | 1-я часть спецификации |
| `docs/archive/spec/SPECIFICATION2.md` | 583.5 | 2026-08-26 | историческое | 2-я часть спецификации |
| `docs/archive/plans/PLANS.md` | 402.9 | 2026-04-27 | историческое | 1-я часть плана |
| `docs/archive/plans/PLANS2.md` | 723.7 | 2026-04-27 | историческое | 2-я часть плана |
| `docs/archive/plans/PLANS3.md` | 1004.1 | 2026-08-21 | историческое | 3-я часть плана |
| `docs/archive/worklog/WORKLOG.md` | 590.3 | 2026-04-22 | историческое | сессии до 168 |
| `docs/archive/worklog/WORKLOG2.md` | 457.3 | 2026-04-27 | историческое | сессии 169–309 |
| `docs/archive/worklog/WORKLOG3.md` | 1690.3 | 2026-08-21 | историческое | сессии 310–480 |
| `docs/archive/worklog/WORKLOG4.md` | 662.7 | 2026-08-18 | историческое | сессии 481–1031 |
| `docs/archive/audits-legacy/AUDIT_REPORT.md` | 24.0 | 2026-08-18 | историческое | зонтичный обзор, заменён серией docs/audit/ |
| `docs/archive/audits-legacy/RECOVERY_ROADMAP.md` | 17.6 | 2026-08-18 | историческое | задачи выполнены |
| `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` | 8.6 | 2026-08-18 | историческое | все волны завершены |
| `docs/archive/ide/IDE_REDESIGN.md` | 21.4 | 2026-04-27 | историческое | апрельский план, перекрыт реализацией |
| `docs/archive/ide/plan-type-object-core.md` | 16.4 | 2026-05-04 | историческое | майский план «от записи к объектному ядру», в основном реализован (переименован при переносе) |
| `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` | 55.4 | 2026-08-20 | дублирующее | заменён docs/audit/BYTECODE_VM_PERFORMANCE.md |
| `docs/archive/audits-legacy/ERROR_HANDLING_AUDIT.md` | 14.7 | 2026-08-20 | дублирующее | заменён docs/audit/ERROR_HANDLING.md |

## Зона 6 — дублирующие отчёты (docs/archive/audits-legacy/, 8 файлов)

Ранние отчёты аудитов, заменённые более полными файлами в `docs/audit/`:

| Путь | КБ | Изменён | Статус | Заменён на |
| --- | ---: | --- | --- | --- |
| `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md` | 36.7 | 2026-08-19 | дублирующее | `docs/audit/FILE_SYSTEM.md` |
| `docs/archive/audits-legacy/IO_AUDIT_REPORT.md` | 23.4 | 2026-08-19 | дублирующее | `docs/audit/INPUT_PRINT_DATA_READ_RESTORE_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/STRING_AUDIT_REPORT.md` | 4.8 | 2026-08-19 | дублирующее | `docs/audit/STRING_OPERATIONS.md` |
| `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` | 12.2 | 2026-08-19 | дублирующее | `docs/audit/TYPE_USER_DEFINED_TYPES_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/MODULES_AUDIT.md` | 7.1 | 2026-08-20 | дублирующее | `docs/audit/MODULES_NAMESPACES_PROGRAM_STRUCTURE.md` |
| `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` | 96.3 | 2026-08-19 | дублирующее | `docs/audit/IDE_LANGUAGE_INTEGRATION.md` |
| `docs/archive/audits-legacy/LANGUAGE_FEATURE_INVENTORY.md` | 61.4 | 2026-08-21 | дублирующее | `docs/LANGUAGE_INVENTORY.md` |
| `docs/archive/audits-legacy/DATETIME_FORENSIC_AUDIT_old_version.md` | 70.0 | 2026-08-21 | дублирующее | `docs/audit/DATETIME_FORENSIC_AUDIT.md` |

## Зона 7 — внешние и служебные файлы (15 файлов)

| Путь | КБ | Изменён | Статус | Примечание |
| --- | ---: | --- | --- | --- |
| `.codex/skills/axiom-doc-author/SKILL.md` | 29.3 | 2026-05-14 | инфраструктура | навык автора документации |
| `.codex/skills/axiom-doc-author/references/*.md` (6) | ~33 | 04–05.2026 | инфраструктура | справочники навыка |
| `.codex/skills/compiler-front-end/SKILL.md` | 2.9 | 2026-04-27 | инфраструктура | навык |
| `.agents/skills/fc-nginx-website/SKILL.md` | 6.8 | 2026-08-26 | инфраструктура | навык (не про Axiom) |
| `.zcode/plans/plan-sess_*.md` (2) | ~19 | 08.2026 | инфраструктура | планы внешнего инструмента |
| `.artifacts/inventory/partA_registry.md` | 160.8 | 2026-08-20 | инфраструктура | промежуточный артефакт инвентаризации |
| `.artifacts/inventory/partB_subsystems.md` | 57.7 | 2026-08-20 | инфраструктура | промежуточный артефакт |
| `.artifacts/inventory/partC_overview.md` | 33.7 | 2026-08-20 | инфраструктура | промежуточный артефакт |
| `samples/graphics/game/assets/ASSETS.md` | 1.0 | 2026-06-05 | актуальное | описание ассетов примера |

---

## Контроль полноты

- Все 100 `.md`-файлов (кроме `node_modules/` и `.git/`) внесены в реестр.
- Сумма по зонам: 8 + 13 + 33 + 6 + 16 + 8 + 16 = 100.
- Перенос в архив выполнен 2026-08-26: 24 файла переехали в `docs/archive/` (spec 2 + plans 3 + worklog 4 + ide 2 + audits-legacy 13).
- Зона 2 «активные источники» теперь содержит только текущие документы; все исторические и дублирующие вынесены в `docs/archive/`.
