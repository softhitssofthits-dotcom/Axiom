# Рабочая документация Axiom

Действующий продукт: Electron IDE и компилятор/BytecodeVm на .NET. Для продолжения читать текущие документы ниже; архив предназначен для истории.

| Вопрос | Документ |
|---|---|
| Что за продукт и его границы? | [PROJECT.md](../PROJECT.md) |
| Как подготовить и запустить? | [README.md](../README.md) |
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
