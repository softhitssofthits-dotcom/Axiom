# Точные формы каталогов

Session1269, 2026-10-09. Сверены восемь строк Name/Id/Arity снимка1268. DirectoryFormEvidenceTests добавляет18 случаев:8 статических корректных форм и10 неверных аргументов.

Parser/Binder проверяют BoundFunctionExpression.BuiltinId и результат BOOLEAN для DIREXISTS/PATHEXISTS/MAKEDIR/DELETEDIR/DELETEDIRRECURSIVE/COPYDIR без флага/с TRUE/MOVEDIR. Неверные пути BOOLEAN и строка вместо флага overwrite дают SemanticFailure с непустыми совпадающими кодами/Span в обоих режимах. Это отказ до исполнения, не доказательство выполнения BytecodeVm на неверном вызове.

Совместно34/34 с DirectoryObjectKindEvidenceTests/DirectoryDeleteBoundaryEvidenceTests/DirectoryTransferKindEvidenceTests; .artifacts/test-results/directory-forms-1269/directory-forms-1269.trx. Ошибок/пропусков0. Исполнение доказывают отдельные матрицы обоих движков с байтами/Unicode/сохранностью, статическая проверка дополняет их.

Дефект не найден, Runtime/руководство/поведение не менялись; новый пример не требуется. Полный прогон после изменения только тестов не повторялся. Предел: выбранные типовые аргументы, не все ошибки носителя/прав/ссылок. NUM26-CAND-001/IDE26-CAND-001 открыты. Далее DIRSIZE/CURRENTDIR/CHANGEDIR.
