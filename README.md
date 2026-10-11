# Axiom IDE

Axiom — BASIC-подобный язык с компиляцией в байт-код и настольная IDE на Electron. Ядро языка и служебный процесс написаны на C#/.NET 10; редактор использует Monaco, консоль — xterm.js.

## Подготовка и запуск

Текущий рабочий способ запуска из исходников требует Windows, .NET SDK 10 и Node.js/npm. Из корня репозитория выполнить последовательно:

```powershell
npm ci --prefix ide
npm ci --prefix ide/e2e
dotnet build src/Axiom.Cli -c Release
dotnet build src/Axiom.DesktopHost -c Debug
.\run-ide.bat
```

`run-ide.bat` проверяет наличие и свежесть DesktopHost, затем запускает Electron. Сборки .NET используют `.artifacts/bin` и `.artifacts/obj`; команды с общей промежуточной папкой выполняются последовательно. Поставка для компьютера без SDK/Node пока не принята.

## Работа с программой

Открыть файл `.axm` в IDE, проверить исходник, затем запустить его. Поддержаны отдельные документы, три темы (светлая, тёмная, QuickBASIC), диагностика во время набора, форматирование Classic/Modern, дополнение, вывод, `INPUT`, графика и трассировка. `CANVAS` открывается отдельным окном Electron; `SCREEN` — нативным окном служебного процесса. Язык исполняет `BytecodeVm`.

Сохранение проверяет исходную версию файла и пишет под исключительным дескриптором. Внешние изменения требуют решения пользователя. После аварии восстанавливаются завершённые правки, безымянные и отсутствующие документы, выбранная вкладка; программа автоматически не запускается. Восстановление не включает историю отмены и точки останова. Подробности: [результат сохранности документов](docs/reports/electron/ELECTRON_LOCKED_SAVE_RECOVERY_RESULT.md).

## Проверки

Каталог Electron содержит 85 сценариев в 27 файлах. Просмотр каталога не запускает приложение:

```powershell
node ide/e2e/runner.js --list
npm --prefix ide/e2e test
node ide/e2e/runner.js files.spec.js
```

Подготовка, изоляция и ограничения: [инструкция набора](ide/e2e/README.md). Последний сохранённый полный результат — 85/85 и 466 утверждений; этот результат относится к предыдущему этапу, а не к новому запуску на каждом компьютере.

Проверка документации и действующего реестра:

```powershell
dotnet run --project scripts/language-audit/Axiom.LanguageAudit.csproj -- C:/Axiom --inventory-only --output-dir .artifacts/language-inventory-current
node scripts/check-manual.mjs --inventory .artifacts/language-inventory-current/inventory.json
powershell -NoProfile -File scripts/ai-context.ps1 -StdoutOnly
```

Проверки языка и служебных компонентов: `dotnet test Axiom.sln` (последовательный запуск). Проверка CLI без исполнения: `dotnet run --project src/Axiom.Cli -- check samples/smoke/bytecode-only/compiler_execution.axm --json`.

## Структура репозитория

| Путь | Назначение |
|---|---|
| `src/` | Слои языка, BytecodeVm, Execution, общие службы и DesktopHost |
| `ide/` | Рабочая Electron IDE и проверки JavaScript |
| `ide/e2e/` | Сценарии приёмки Electron и сохранённые результаты |
| `tests/` | Проверки компонентов .NET |
| `samples/` | Запускаемые тематические программы `.axm` |
| `docs/manual/` | Пользовательский справочник с поиском |
| `docs/contracts/` | Действующие тематические договоры |
| `docs/plans/` | Текущий план Electron и список незавершённого |
| `docs/reports/` | Результаты завершённых этапов |
| `docs/registries/` | Реестры языка и документов |
| `docs/archive/` | История, прежние планы и результаты; не инструкции текущего запуска |
| `.artifacts/` | Сборки и временные результаты проверок |

## С чего продолжить

Прочитать [индекс](docs/INDEX.md), [текущий план](docs/PLANS4.md), [журнал](docs/WORKLOG5.md) и [список незавершённых работ](docs/plans/UNFINISHED_WORK.md). Следующий продуктовый этап — зависание интерфейса с доступной отменой; затем аппаратная приёмка графики и оставшиеся условия Electron. Дерево многофайловых проектов отложено.
