# Рабочая документация Axiom

Действующий продукт: Electron IDE и компилятор/BytecodeVm на .NET. Для продолжения читать текущие документы ниже; архив предназначен для истории.

| Вопрос | Документ |
|---|---|
| Что за продукт и его границы? | [PROJECT.md](../PROJECT.md) |
| Как представить проект на GitHub? | [README.md](../README.md) |
| Как подготовить и запустить, как проверять? | [DEVELOPMENT.md](DEVELOPMENT.md) |
| Как работать агенту? | [AGENTS.md](../AGENTS.md) |
| Какие версии и как их повышать? | [VERSIONING.md](VERSIONING.md), [CHANGELOG.md](CHANGELOG.md), [versions.json](../versions.json) |
| Какой договор реализован? | [SPECIFICATION3.md](SPECIFICATION3.md) |
| Как устроено? | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Где остановились и что дальше? | [PLANS4.md](PLANS4.md), [WORKLOG5.md](WORKLOG5.md) |
| Что осталось недоделанным? | [UNFINISHED_WORK.md](plans/UNFINISHED_WORK.md) |
| Какие сущности языка есть? | [Реестр языка](registries/LANGUAGE.md) |
| Что покрывает VM? | [BYTECODE_COVERAGE.md](BYTECODE_COVERAGE.md) |
| Какие измерения есть? | [BYTECODE_PERF_MATRIX.md](BYTECODE_PERF_MATRIX.md) |
| Как устроен обмен IDE/.NET? | [Протокол](plans/IDE_SHELL_PROTOCOL.md) |
| Какие условия Electron ещё не приняты? | [Матрица](plans/IDE_SHELL_FEATURE_MATRIX.csv) |
| Где справочник? | [manual/index.html](manual/index.html), [правила сопровождения](manual/README.md) |
| Где договор тайлов? | [TILEMAP.md](contracts/TILEMAP.md) |
| Где результаты последних этапов? | [SCREEN на DirectX](reports/graphics/SCREEN_DIRECTX_MIGRATION.md), [ревизия документов и нативный INPUT](reports/DOCUMENTATION_REVIEW_20261015.md) |
| Где результат действующей очереди Electron? | [Зависание, документы и поставка](reports/electron/ELECTRON_ACTIVE_QUEUE_RESULT.md) |
| Как проверить аппаратуру Electron? | [Мышь, системный DPI и мониторы](plans/ELECTRON_HARDWARE_ACCEPTANCE.md) |
| Что проверено при ревизии документов? | [Текущая ревизия 15 октября](reports/DOCUMENTATION_REVIEW_20261015.md), [предыдущая ревизия 11 октября](reports/DOCUMENTATION_REVIEW_20261011.md) |
| Где история и карта переносов? | [archive/README.md](archive/README.md), [реестр документов](registries/DOCUMENTS.csv) |

Папки: `contracts/` — действующие тематические договоры; `plans/` — очередь и приёмка; `reports/` — результаты; `registries/` — реестры; `agent/` — краткий вход; `manual/` — пользовательские статьи; `audit/bytecode-only/` — текущий экспорт проверок; `archive/` — исторические материалы.

Обязательное начало сессии остаётся по AGENTS.md. Краткий агентный слой помогает после чтения основных источников, не заменяет их.

Устойчивость подготовки запуска после аварии служебного процесса: [результат 16 октября](reports/electron/EXECUTION_PREPARATION_STABILITY.md).

Изоляция рабочих каталогов повторного запуска: [приёмка 16 октября](reports/electron/EXECUTION_DIRECTORY_ACCEPTANCE.md).

Отказы подготовки, неполный байт-код и очистка кэша: [приёмка 16 октября](reports/electron/BACKEND_FAILURE_ACCEPTANCE.md).

Принадлежность Run исходному документу при сохранении: [исправление 16 октября](reports/electron/RUN_DOCUMENT_STABILITY.md).

Диапазоны диагностик UTF-16, CRLF и символы вне BMP: [приёмка 16 октября](reports/electron/DIAGNOSTIC_RANGE_ACCEPTANCE.md).

Диагностика исполнявшегося снимка при смене вкладки/правке: [исправление 16 октября](reports/electron/EXECUTION_DIAGNOSTIC_STABILITY.md).

Устойчивость Restart и его отмена через Stop: [исправление 16 октября](reports/electron/TRACE_RESTART_STABILITY.md).

Граница файловых эффектов StepBack: [приёмка 16 октября](reports/electron/STEPBACK_FILE_BOUNDARY_ACCEPTANCE.md).

Частичный отказ COPYDIR и StepBack: [приёмка](reports/electron/STEPBACK_PARTIAL_FILE_ACCEPTANCE.md).

Графическая граница StepBack: [приёмка SCREEN и CANVAS](reports/electron/STEPBACK_GRAPHICS_ACCEPTANCE.md).

Единый реестр четырёх тем и уточнения оформления: [аудит и приёмка](reports/electron/THEMES_AUDIT.md).

Восстановление полной матрицы настроек: [группа appearance и дальнейшие проверки](reports/electron/EDITOR_SETTINGS_MATRIX_RECOVERY.md).

Устойчивость отказов команд отладки: [исправление и приёмка](reports/electron/DEBUG_COMMAND_FAILURE_STABILITY.md).
