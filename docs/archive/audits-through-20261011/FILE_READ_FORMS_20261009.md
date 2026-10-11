# Формы и отказы чтения FILE

Session1257, 2026-10-09. Сверены RequireReadableFile/EvaluateReadToEnd/EvaluateReadLine/EvaluateSkipLine и прежние пакетные/буферные проверки. READLINE/SKIPLINE при EOF дают MB3007, READTOEND возвращает оставшийся текст; это различие не заменяется общим ожиданием.

Добавлены30 случаев в существующие матрицы:12 закрытых/неоткрытых,12 OUTPUT/APPEND с ожидающей Unicode-записью и6 статических. Закрытые вызовы дают MB3007/ISOPEN FALSE, неверный режим MB3007/ISOPEN TRUE с прежней позицией. После CLOSE точные байты и эксклюзивное открытие подтверждают сохранность/освобождение. Parser/Binder подтверждают Id ReadToEnd/ReadLine/SkipLine и STRING/STRING/BOOLEAN; неверный FILE даёт SemanticFailure одинакового кода/Span обоих режимов.

Совместно142/142 с FilePositionBufferEvidenceTests/FileLineBatchEvidenceTests; .artifacts/test-results/file-read-forms-1257/file-read-forms-1257.trx. Ошибок/пропусков0. Прежние INPUT-проверки охватывают UTF-8/UTF-16LE/BE, границы буфера, скаляры/пустые строки/EOF/остаток текста; не все кодировки/отказы носителя. Фактические движки новых случаев и отсутствие запасного интерпретатора проверены.

Дефект не найден, Runtime/руководство/поведение не менялись; новый запускаемый пример не требуется. Полный прогон после изменения только тестов не повторялся. NUM26-CAND-001 открыт. Далее WRITE/WRITELINE/NEWLINE.
