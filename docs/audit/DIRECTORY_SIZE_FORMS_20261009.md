# Типы размера и текущего каталога

Session1271, 2026-10-09. DirectoryFormEvidenceTests дополнен5 случаями. Parser/Binder проверяют точные BuiltinId и типы BoundCallExpression: DIRSIZE/DirSize LONG, CURRENTDIR/CurrentDir STRING, CHANGEDIR/ChangeDir BOOLEAN. BOOLEAN вместо пути DIRSIZE/CHANGEDIR даёт SemanticFailure, непустые совпадающие коды/Span обоих режимов. Статическая проверка не выдаётся за выполнение BytecodeVm.

Совместно25/25 с DirectorySizeCurrentEvidenceTests, доказывающим исполнение обоих движков/точные байты/локальную смену каталога; .artifacts/test-results/directory-size-forms-1271/directory-size-forms-1271.trx. Ошибок/пропусков0. Runtime/руководство/поведение не менялись; новый пример не требуется. Полный прогон после изменения только тестов не повторялся.

Прочитан DriveInfoEvidenceTests: сверка с Win32_LogicalDisk вместо повторения DriveInfo реализации; свободное место проверяется диапазоном, не постоянным значением. Он ещё не выполнен на этом шаге. Далее шесть дисковых функций и точные формы. NUM26-CAND-001/IDE26-CAND-001 открыты.
