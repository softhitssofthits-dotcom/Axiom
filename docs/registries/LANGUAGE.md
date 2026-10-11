# Реестр действующего языка

Сверено 11 октября 2026 года с загруженным реестром компилятора и `docs/manual/data/language-reference.json`: **271 имя встроенной функции, 335 форм вызова, 67 записей операторов/объявлений/форм инструкций, 95 ключевых слов, 14 встроенных типов и 24 операции выражений**.

Точные типы аргументов, результата и политики вызова хранятся в [снимке языка](../manual/data/language-reference.json). Пользовательские каталоги: [функции](../manual/function-reference.html), [операторы](../manual/operator-reference.html), [ключевые слова, типы и операции](../manual/keyword-reference.html).

Источники: `src/Axiom.Semantics/Symbols/Builtins/`, `BuiltinFunctionId.cs`, `src/Axiom.Syntax/SyntaxKind.cs`, синтаксические узлы и парсер. Выполнение: `src/Axiom.Runtime/Execution/BytecodeVm*`.

При изменении языка обновить тематическую страницу, получить inventory-only, пересобрать каталоги и выполнить `node scripts/check-manual.mjs --inventory .artifacts/language-inventory-current/inventory.json`. Контрольная сумма снимка выявляет изменение исходных файлов; самостоятельные ожидания тестов требуются для подтверждения семантики каждой формы.

Статусы непроверенных форм не превращаются автоматически в «сломано». См. [покрытие VM](../BYTECODE_COVERAGE.md) и [незавершённые работы](../plans/UNFINISHED_WORK.md).
