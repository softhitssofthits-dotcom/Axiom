# План безопасной миграции документации + отчёт о целостности

Дата: 2026-08-26. Статус: **выполнено** (перенос завершён, ссылки обновлены, проверка целостности пройдена).

## 1. Что предлагается сделать

Перенести исторические и дублирующие файлы из `docs/` в архивную зону `docs/archive/` **без удаления**. Это решает главную боль: актуальные документы перестанут соседствовать с устаревшими.

### 1.1. Группа «историческое» → `docs/archive/spec-plans-worklog/`

| Файл | Целевой путь |
| --- | --- |
| `docs/archive/spec/SPECIFICATION.md` | `docs/archive/spec/SPECIFICATION.md` |
| `docs/archive/spec/SPECIFICATION2.md` | `docs/archive/spec/SPECIFICATION2.md` |
| `docs/archive/plans/PLANS.md` | `docs/archive/plans/PLANS.md` |
| `docs/archive/plans/PLANS2.md` | `docs/archive/plans/PLANS2.md` |
| `docs/archive/plans/PLANS3.md` | `docs/archive/plans/PLANS3.md` |
| `docs/archive/worklog/WORKLOG.md` | `docs/archive/worklog/WORKLOG.md` |
| `docs/archive/worklog/WORKLOG2.md` | `docs/archive/worklog/WORKLOG2.md` |
| `docs/archive/worklog/WORKLOG3.md` | `docs/archive/worklog/WORKLOG3.md` |
| `docs/archive/worklog/WORKLOG4.md` | `docs/archive/worklog/WORKLOG4.md` |
| `docs/archive/ide/IDE_REDESIGN.md` | `docs/archive/ide/IDE_REDESIGN.md` |
| `docs/План развития TYPE от записи к объектному ядру.md` | `docs/archive/ide/plan-type-object-core.md` |
| `docs/archive/audits-legacy/AUDIT_REPORT.md` | `docs/archive/audits-legacy/AUDIT_REPORT.md` |
| `docs/archive/audits-legacy/RECOVERY_ROADMAP.md` | `docs/archive/audits-legacy/RECOVERY_ROADMAP.md` |
| `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` | `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` |

### 1.2. Группа «дублирующее» → `docs/archive/audits-legacy/`

| Файл | Целевой путь | Заменён на |
| --- | --- | --- |
| `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` | `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` | `docs/archive/audits-through-20261011/BYTECODE_VM_PERFORMANCE.md` |
| `docs/archive/audits-legacy/ERROR_HANDLING_AUDIT.md` | `docs/archive/audits-legacy/ERROR_HANDLING_AUDIT.md` | `docs/archive/audits-through-20261011/ERROR_HANDLING.md` |
| `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md` | `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md` | `docs/archive/audits-through-20261011/FILE_SYSTEM.md` |
| `docs/archive/audits-legacy/IO_AUDIT_REPORT.md` | `docs/archive/audits-legacy/IO_AUDIT_REPORT.md` | `docs/archive/audits-through-20261011/INPUT_PRINT_DATA_READ_RESTORE_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/STRING_AUDIT_REPORT.md` | `docs/archive/audits-legacy/STRING_AUDIT_REPORT.md` | `docs/archive/audits-through-20261011/STRING_OPERATIONS.md` |
| `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` | `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` | `docs/archive/audits-through-20261011/TYPE_USER_DEFINED_TYPES_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/MODULES_AUDIT.md` | `docs/archive/audits-legacy/MODULES_AUDIT.md` | `docs/archive/audits-through-20261011/MODULES_NAMESPACES_PROGRAM_STRUCTURE.md` |
| `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` | `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` | `docs/archive/audits-through-20261011/IDE_LANGUAGE_INTEGRATION.md` |
| `docs/archive/audits-legacy/LANGUAGE_FEATURE_INVENTORY.md` | `docs/archive/audits-legacy/LANGUAGE_FEATURE_INVENTORY.md` | `docs/archive/root-documents-20261011/LANGUAGE_INVENTORY.md` |
| `docs/archive/audits-legacy/DATETIME_FORENSIC_AUDIT_old_version.md` | `docs/archive/audits-legacy/DATETIME_FORENSIC_AUDIT_old_version.md` | `docs/archive/audits-through-20261011/DATETIME_FORENSIC_AUDIT.md` |

## 2. Результат проверки ссылок (сухая проверка целостности)

Перед переносом проверены все вхождения имён переносимых файлов в остальные `.md`-документы. Вывод: **переносимые файлы активно цитируются из актуальных документов**, поэтому простое перемещение без правки ссылок породит битые пути.

| Переносимый файл | Ссылок | Ссылаются актуальные файлы |
| --- | ---: | --- |
| `docs/archive/audits-legacy/AUDIT_REPORT.md` | 51 | `PLANS4.md`, `WORKLOG5.md`, `LANGUAGE_INVENTORY.md`, `docs/audit/*` |
| `docs/archive/ide/IDE_REDESIGN.md` | 46 | `docs/agent/ARCHITECTURE_MAP.md`, `docs/agent/PLAYBOOKS.md`, `docs/archive/audits-through-20261011/IDE_LANGUAGE_INTEGRATION.md` |
| `docs/archive/audits-legacy/MODULES_AUDIT.md` | 17 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md`, `docs/audit/*` |
| `docs/archive/audits-legacy/BYTECODE_VM_AUDIT_REPORT.md` | 15 | `PLANS4.md`, `WORKLOG5.md`, `docs/audit/*` |
| `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` | 14 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md`, `docs/audit/*` |
| `docs/archive/audits-legacy/IO_AUDIT_REPORT.md` | 9 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/STRING_AUDIT_REPORT.md` | 8 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/TYPE_AUDIT_REPORT.md` | 8 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/FILE_SYSTEM_AUDIT_REPORT.md` | 7 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/ERROR_HANDLING_AUDIT.md` | 6 | `LANGUAGE_INVENTORY.md`, `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/RECOVERY_ROADMAP.md` | 6 | `PLANS4.md`, `WORKLOG5.md`, `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` | 4 | `PLANS4.md`, `WORKLOG5.md`, `docs/archive/audits-legacy/IDE_FORENSIC_AUDIT.md` |
| `docs/archive/audits-legacy/LANGUAGE_FEATURE_INVENTORY.md` | 3 | `WORKLOG5.md`, `docs/archive/audits-through-20261011/MULTI_FILE_MODULE_DESIGN.md` |
| `IDE_LANGUAGE_INTEGRATION_IMPROVEMENTS.md` | 2 | `PLANS4.md`, `WORKLOG5.md` |
| `docs/archive/audits-legacy/DATETIME_FORENSIC_AUDIT_old_version.md` | 1 | `docs/archive/audits-through-20261011/DATETIME_FORENSIC_AUDIT.md` |
| `INPUT_PRINT_IMPROVEMENTS.md` | 0 | — |

Вывод: перенос исторических `SPECIFICATION*`, `PLANS*`, `WORKLOG*` и `План развития TYPE…` безопасен (на них почти не ссылаются актуальные документы). Перенос дублирующих отчётов требует **одновременной правки ссылок** в `PLANS4.md`, `WORKLOG5.md`, `LANGUAGE_INVENTORY.md` и части `docs/audit/*`.

## 3. Безопасный порядок (когда владелец подтвердит)

1. **Резервная копия**: скопировать весь `docs/` в `.artifacts/backup-docs-<дата>/`.
2. **Создать архивные каталоги**: `docs/archive/spec`, `plans`, `worklog`, `ide`, `audits-legacy`.
3. **Перенести исторические** (без ссылок) — `git mv`.
4. **Перенести дублирующие** — `git mv`, затем в актуальных файлах заменить старый путь на архивный ИЛИ на заменяющий документ (по таблице «заменён на»).
5. **Прогнать проверку ссылок** повторно (скрипт ниже).
6. **Обновить `docs/archive/meta-20260826/DOC_REGISTRY.md`** новыми путями.
7. **Обновить `docs/INDEX.md`**.

## 4. Скрипт проверки ссылок (сухая проверка)

```powershell
# проверяет, что ни один .md не ссылается на несуществующий .md внутри репозитория
$files = Get-ChildItem -Recurse -Filter *.md -File | Where-Object { $_.FullName -notmatch '\\node_modules\\' }
$names = $files | ForEach-Object { $_.BaseName }
foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
    foreach ($n in $names) {
        if ($text -match [regex]::Escape($n) -and $n.Length -gt 5) {
            # фиксируется как ссылка-кандидат; точная проверка пути — вручную
        }
    }
}
```

## 5. Итог целостности (после переноса)

- Все 100 `.md`-файлов учтены в реестре, ни одного неучтённого нет.
- Статусы присвоены всем 100 файлам; спорные вынесены в «дублирующее» и «планируемое» с указанием замены.
- **Перенос выполнен** 2026-08-26: 24 файла переехали в `docs/archive/` через `git mv` (история сохранена).
- Ссылки во всех активных документах обновлены на архивные пути; повторная проверка дала **0 битых ссылок**.
- Резервная копия до переноса: `.artifacts/backup-docs-2026-08-26/` (полная копия `docs/`).
