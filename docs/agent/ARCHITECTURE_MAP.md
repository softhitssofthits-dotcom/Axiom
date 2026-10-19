# Карта исходников Axiom

| Область | Путь |
|---|---|
| Координаты и диагностики | `src/Axiom.Core/` |
| Токены и синтаксис | `src/Axiom.Lexing/`, `src/Axiom.Syntax/` |
| Разбор | `src/Axiom.Parsing/Parser.*.cs` |
| Связывание и формы функций | `src/Axiom.Semantics/`, `Symbols/Builtins/` |
| Понижение и байт-код | `src/Axiom.Bytecode/` |
| Исполнение и встроенные функции | `src/Axiom.Runtime/Execution/BytecodeVm*.cs` |
| Сквозной путь | `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs` |
| Форматирование | `src/Axiom.Formatting/` |
| Контракты документов | `src/Axiom.Ide.Contracts/` |
| Общие службы | `src/Axiom.Ide.Backend/` |
| Служебный процесс и запись | `src/Axiom.DesktopHost/`, `FileCommands.cs` |
| Связь и восстановление | `ide/src/main/backend-client.js`, `backend-ipc.js`, `recovery-store.js` |
| Редактор и документы | `ide/src/renderer/app.js`, `documents.js`, `document-save.js` |
| Вывод и ввод | `ide/src/renderer/execution-controller.js`, `program-keys.js` |
| Нативная графика | `src/Axiom.Ide.Backend/Graphics/`, `Services/Graphics/`, `src/Axiom.DesktopHost/` |
| Трассировка и состояние графики | `ide/src/renderer/trace-controller.js`, `execution-controller.js` |
| Элементы управления | `ide/src/renderer/ui-controls.js`, `design-system.css` |
| Electron-приёмка | `ide/e2e/specs/`, `SCENARIOS.json` |
| Проверки служебных модулей | `ide/tests/`, `tests/` |
| Справочник и полнота | `docs/manual/`, `scripts/check-manual.mjs` |

Точные договоры находятся в текущей спецификации, коде, примерах и проверках. Эта карта не является самостоятельной спецификацией.

## Файловая подсистема — 19 октября 2026 года

Тематические обработчики VM: BytecodeVm.Builtins.FileText.cs, Paths.cs, FileOperations.cs, Directories.cs, FileMetadata.cs и Storage.cs. Общие проверки — BytecodeVm.Builtins.FileSystem.cs; блочный сканер строк — Values/RuntimeTextFile.Lines.cs. Отдельного ускоренного файлового пути в BytecodeVm.cs больше нет: все формы используют единый договор.
