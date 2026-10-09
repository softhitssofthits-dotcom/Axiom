# Закрытые состояния и зарегистрированные формы FILE

Session1256, 2026-10-09. Расширены существующие FileClosedPropertiesEvidenceTests четырьмя вызовами EOF/LINENUMBER/REWIND/FLUSH:16 новых случаев закрытого/неоткрытого FILE, оба движка. Перехват MB3007/FINALLY, ISOPEN FALSE, продолжение, буквальные UTF-8-байты и эксклюзивное открытие подтверждены. ISOPEN непосредственно проверяется после ошибки/после CLOSE, отказа для корректного FILE у него нет.

FilePropertyFormEvidenceTests дополнены пятью точными формами и пятью неверными аргументами: IsOpen/Eof/Rewind/Flush возвращают BOOLEAN, LineNumber LONG. Проверяются BoundFunctionExpression.BuiltinId и тип BoundCallExpression; неверный FILE даёт SemanticFailure с совпадающим кодом/Span обоих режимов. Статический отказ не выдаётся за выполнение BytecodeVm.

Совместно70/70 с FileModeStateEvidenceTests; .artifacts/test-results/file-state-forms-1256/file-state-forms-1256.trx, ошибок/пропусков0. Новых26 случаев. Дефект не найден, Runtime/руководство/поведение не менялись; новый запускаемый пример не требуется. Полный прогон после изменения только тестов не повторялся.

Предел: выбранные закрытые/неверные состояния, не все кодировки/отказы носителя. NUM26-CAND-001 открыт. Далее READTOEND/READLINE/SKIPLINE и их режимы/точные формы.
