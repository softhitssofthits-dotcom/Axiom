# Рабочая документация Axiom

Актуализация: 16 октября 2026 года. Действующий продукт: Electron IDE и компилятор/BytecodeVm на .NET. Для продолжения читать текущие документы ниже; архив предназначен только для истории.

## Основные источники

| Вопрос | Документ |
|---|---|
| Что за продукт и его границы? | [PROJECT.md](../PROJECT.md) |
| Как представить проект на GitHub? | [README.md](../README.md) |
| Как подготовить, запустить и проверять? | [DEVELOPMENT.md](DEVELOPMENT.md) |
| Какие проверки есть и какую группу запускать? | [TESTING.md](TESTING.md) |
| Как работать агенту? | [AGENTS.md](../AGENTS.md) |
| Какие версии и как их повышать? | [VERSIONING.md](VERSIONING.md), [CHANGELOG.md](CHANGELOG.md), [versions.json](../versions.json) |
| Какой договор реализован? | [SPECIFICATION3.md](SPECIFICATION3.md) |
| Как устроено? | [ARCHITECTURE.md](ARCHITECTURE.md) |

## Состояние, очередь и покрытие

| Вопрос | Документ |
|---|---|
| Где остановились и что дальше? | [PLANS4.md](PLANS4.md), [WORKLOG5.md](WORKLOG5.md) |
| Что осталось недоделанным? | [UNFINISHED_WORK.md](plans/UNFINISHED_WORK.md) |
| Действующая очередь оболочки Electron | [ELECTRON_IDE_PLAN.md](plans/ELECTRON_IDE_PLAN.md) |
| Какие сущности языка есть? | [Реестр языка](registries/LANGUAGE.md) |
| Что покрывает VM? | [BYTECODE_COVERAGE.md](BYTECODE_COVERAGE.md) |
| Какие измерения есть? | [BYTECODE_PERF_MATRIX.md](BYTECODE_PERF_MATRIX.md) |
| Где справочник? | [manual/index.html](manual/index.html), [правила сопровождения](manual/README.md) |

## Договоры, обмен и приёмка

| Вопрос | Документ |
|---|---|
| Как устроен обмен IDE/.NET? | [Протокол](plans/IDE_SHELL_PROTOCOL.md) |
| Какие условия Electron ещё не приняты? | [Матрица](plans/IDE_SHELL_FEATURE_MATRIX.csv) |
| Как проверить аппаратуру Electron? | [Мышь, системный DPI и мониторы](plans/ELECTRON_HARDWARE_ACCEPTANCE.md) |
| Где договор тайлов? | [TILEMAP.md](contracts/TILEMAP.md) |
| Где договор графики? | [GRAPHICS.md](contracts/GRAPHICS.md) |
| Где договор диалога настроек? | [EDITOR_SETTINGS_DIALOG.md](plans/EDITOR_SETTINGS_DIALOG.md) |
| Где результаты завершённых этапов? | Папка `reports/` — только на диске разработчика, в репозиторий не публикуется |

## Вспомогательный слой

| Вопрос | Документ |
|---|---|
| Краткий вход агента | [agent/README.md](agent/README.md) |
| Текущее состояние и точка остановки | [agent/CURRENT_STATE.md](agent/CURRENT_STATE.md) |
| Карта исходников | [agent/ARCHITECTURE_MAP.md](agent/ARCHITECTURE_MAP.md) |
| Типовые рабочие сценарии | [agent/PLAYBOOKS.md](agent/PLAYBOOKS.md) |
| Шаблоны записей | [templates/](templates/) |

## Папки

`contracts/` — действующие тематические договоры; `plans/` — очередь, приёмка и договоры оболочки; `registries/` — реестры; `agent/` — краткий вход; `manual/` — пользовательские статьи; `templates/` — шаблоны записей.

Папки `archive/`, `reports/` и `audit/` хранятся только на диске разработчика и в репозиторий не публикуются (внесены в `.gitignore`). Это исторические материалы, отчёты завершённых этапов и выгрузки проверок; они не являются инструкцией текущего запуска.

Обязательное начало сессии остаётся по [AGENTS.md](../AGENTS.md). Краткий агентный слой помогает после чтения основных источников, но не заменяет их.
