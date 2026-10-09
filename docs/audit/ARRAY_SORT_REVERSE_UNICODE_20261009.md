# Сортировка и обратный порядок Unicode

Session1277. Четыре новых групповых случая ArraySortReverseUnicodeEvidenceTests проверяют обе формы ARRAYTOSORTED/ARRAYTOREVERSED/ARRAYSORT/ARRAYREVERSE в обоих движках. Буквальный порядок UTF-16: составной акцент, предварительно составленный символ, старший суррогат, эмодзи, U+E000. Суррогат не исправляется; значения не нормализуются.

На видимом одномерном срезе ARRAYTOSORTED сохраняет границы -2..2, ARRAYTOREVERSED создаёт компактный результат1..5. Обе копии сохраняют источник. Изменение через ARRAYSORT/ARRAYREVERSE затрагивает только видимый участок, соседние строки остаются прежними.

Итог119/119, ошибок/пропусков0, оба движка без запасного интерпретатора. Вместе выполнены ArrayFillSortReverseContractTests, ArraySortBoundaryContractTests, ArrayTransformAxisContractTests, ArrayDefaultFormEvidenceTests, ArrayRemainingFormEvidenceTests: точные формы/типы, обе многомерные оси, неверные оси/короткие массивы, максимальный INTEGER-индекс. Результат: .artifacts/test-results/array-sort-reverse-1277/array-sort-reverse-1277-correct.trx.

Первый прогон88/92 и второй115/119 выявили неверные ожидания границ новой тестовой программы: сортированная копия сохраняет индексы, обратная копия компактна. Исправлен тест, Runtime не менялся. Дефект не найден, руководство/поведение не менялись, новый пример не требуется. Проверка ограничена перечисленными случаями. NUM26-CAND-001/IDE26-CAND-001 открыты. Далее ARRAYFILL/ARRAYCLONE.
