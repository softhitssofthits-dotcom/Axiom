# Зарегистрированные формы свойств FILE

Session1254, 2026-10-09. Сверены строки Name/Id/Arity снимка1253 и шаблон проверки BoundCallExpression. Новые12 случаев дополняют исполнение прежних файловых сценариев статическим договором.

Пять вызовов напрямую разбираются Parser и связываются Binder: FILEPATH/FilePath→STRING, FILEMODE/FileMode→STRING, FILEPOS/FilePos→LONG, FILELENGTH/FileLength→LONG, SEEKFILE/SeekFile→BOOLEAN (позиция CLNG(0)). Проверяются сам BoundFunctionExpression.BuiltinId и тип BoundCallExpression, а не наличие имени в исходном тексте.

Семь неверных аргументов — неподходящий FILE каждой функции и BOOLEAN/STRING вместо позиции SEEKFILE. В обоих режимах SemanticFailure, непустые диагностики и совпадающие код/Span; Bytecode не использует запасной интерпретатор. Это отказ до выполнения, не доказательство работы BytecodeVm на неверном вызове.

Совместно34/34 с FileClosedPropertiesEvidenceTests/AppendBufferedSeekFailureEvidenceTests; .artifacts/test-results/file-property-forms-1254/file-property-forms-1254.trx. Ошибок/пропусков0. Дефект не найден, Runtime/поведение/руководство не менялись; новый запускаемый пример не требуется. Полный прогон после изменения только тестов не повторялся.

Предел: пять форм и выбранные неверные типы; исполнение доказывается отчётами1252/1253, не этой статической проверкой. NUM26-CAND-001 открыт. Далее ISOPEN/EOF/LINENUMBER/REWIND/FLUSH.
