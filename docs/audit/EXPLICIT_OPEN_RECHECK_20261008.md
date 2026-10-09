# Независимая проверка потоковой явной кодировки

Исходная проверка Session 1130: .artifacts/explicit-open-1130-probe.log и одноимённый каталог TRX. До исправления 26/32 прошли, шесть упали. Это новые дефекты интеграции G01, а не нехватка новых функций. Оба исполнителя ошибаются одинаково; паритет сам по себе не доказывает корректность.

## EO26-001 — неперехватываемая ошибка строгой записи — P1

Воспроизведение: объявить FILE, открыть OUTPUT USING "WINDOWS-1251", вызвать WRITE/WRITELINE с "😀" внутри TRY/CATCH. Ожидается MB3007 в CATCH, сохранение позиции и буфера. Фактически EncoderFallbackException выходит наружу из AxiomExecutionPipeline.Run в Interpreter и BytecodeVm. Источник: RuntimeTextFile.Write вызывает строгий GetBytes; BuiltinFileIOHelpers.IsFileIoException не включает исключения кодирования. Непарный UTF-16 в Unicode-кодировке требует той же защиты; отдельное доказательство после устранения первого отказа — полный перебор в OpenStrictWriteFailurePreservesBufferedContentPositionAndLength.

## EO26-002 — неперехватываемая ошибка LINENUMBER — P1

Воспроизведение: открыть корректный UTF-16LE/BE через INPUT USING, выполнить SEEKFILE(f, 1), затем LINENUMBER(f) внутри TRY/CATCH. Ожидается MB3007 и FILEPOS=1. Фактически InvalidOperationException выходит из Run в обоих движках, по два случая каждой кодировки. Источник: BuiltinFileIOHelpers.EvaluateLineNumber обращается к LineNumber без перехвата, хотя CountLinesBeforePosition теперь проверяет выравнивание UTF-16.

Проверяемые исправления должны быть локальными: общий перевод только ожидаемых файловых/кодировочных отказов в диагностику, без глобального подавления исключений. Новое API не требуется.

## Исправление и итоговая проверка

Рабочая копия основана на 2603b3d, ветка master, изменения не закоммичены. В BuiltinFileIOHelpers.IsFileIoException добавлены только EncoderFallbackException/DecoderFallbackException; EvaluateLineNumber переводит ожидаемые файловые отказы в MB3007. Глобальный перехват исключений не расширялся.

После исправления ExplicitTextEncodingTests 32/32: UTF-16LE/BE, правильные строки с Ċ/ਁ и эмодзи, CR/LF/CRLF, FILEPOS, LINENUMBER, SEEKFILE в середину пары и нечётную позицию, нулевой READTEXT, REWIND; строгие OUTPUT/APPEND всех кодировок и точные байты после отказа. Отдельно пять дополнительных проверок верификатора принимают прежнюю/новую форму OpenFile с глубиной стека 2/3 и отвергают недостаток значений через BC4053. Bytecode.Tests 240/240.

Полное решение .artifacts/test-results/explicit-open-1130-full/: 3670/3670, падений и пропусков нет. CLI metadata: 309 дескрипторов, 249 имён, .artifacts/explicit-open-1130-metadata.json. Новые русскоязычные примеры samples/functions/write/strict_encoding_recovery.axm и samples/functions/linenumber/utf16_seek_recovery.axm прошли актуальную CLI с кодом 0; их журналы .artifacts/explicit-open-1130-*-sample-final.log. Первые пробные CLI-запуски использовали старую копию Runtime в выходном каталоге CLI до её пересборки; они не являются результатом исправленной реализации. Оставленные ими две собственные временные папки удалены после проверки точных путей и содержимого.

Четыре кодировки и G01 реализованы в выбранном объёме. Находки EO26-001/002 FIXED. Из этого не следует полнота остальных 309 перегрузок или всего языка: исходная матрица сохраняет неизвестные клетки. Следующий самостоятельный срез — регулярные выражения, включая Unicode, пустые совпадения и ограничения выполнения.
