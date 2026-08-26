# Журнал работ 2

Этот файл является исторической второй частью журнала работ проекта и покрывает сеансы 169-309.

`docs/WORKLOG.md` остаётся исторической первой частью журнала до сеанса 168 включительно. Активный журнал продолжения ведётся в `docs/WORKLOG4.md`.

Правила ведения остаются прежними:
- новые записи добавляются только в конец файла;
- заголовок новой записи имеет вид `## Session <N> - YYYY-MM-DD HH:mm - <название>`;
- номер сеанса продолжает общую последовательность из `docs/WORKLOG.md`;
- дата и время указываются по местному времени проекта.

## Session 169 - 2026-04-20 09:58 - Переход на WORKLOG2 и договор ARRAYCLONE/ARRAYSLICE

### Что изменилось
- Создан активный журнал `docs/WORKLOG2.md`.
- Зафиксировано, что `docs/WORKLOG.md` остаётся исторической первой частью журнала до сеанса 168 включительно.
- Обновлены рабочие правила проекта, чтобы дальнейшие записи велись в `docs/WORKLOG2.md`.
- Дополнительно синхронизированы действующие указания в `docs/PLANS.md` и `docs/IDE_REDESIGN.md`, чтобы они тоже вели к `docs/WORKLOG2.md` как к основному журналу.
- В спецификации, архитектурных заметках и плане зафиксирован ближайший договор для `ARRAYCLONE` и `ARRAYSLICE`.

### Какие файлы изменены
- `AGENTS.md`
- `README.md`
- `docs/WORKLOG2.md`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/IDE_REDESIGN.md`

### Проверка
- Автоматические тесты не запускались, потому что изменялись только проектные документы.

### Решения
- `docs/WORKLOG2.md` является активным журналом для всех новых записей.
- `docs/WORKLOG.md` больше не растёт и используется только как историческая справка.
- Нумерация сеансов продолжается без сброса: после Session 168 следующая запись получила номер 169.
- `ARRAYCLONE` должен сохранять состояние и границы исходного массива.
- `ARRAYSLICE` должен возвращать компактный результат `1 TO N`, а пустой результат — `1 TO 0`.

### Следующий точный шаг
- Реализовать `ARRAYCLONE` как первую чистую операцию над одномерным массивом.
- В этом же шаге подготовить общий путь создания массива произвольного типа элемента, чтобы затем проще добавить `ARRAYSLICE`.

## Session 170 - 2026-04-20 10:11 - ARRAYCLONE

### Что изменилось
- Реализована `ARRAYCLONE(array)` как первая чистая функция над одномерными массивами.
- Связыватель получил общий путь для встроенных функций массива, у которых реальный тип результата зависит от типа первого аргумента массива.
- В слое выполнения выделен общий помощник построения массива произвольного типа элемента, и на нём реализовано независимое клонирование массива.
- Обновлены автоматические проверки, пользовательская справка, проектные документы и запускаемые примеры для `ARRAYCLONE`.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayclone/basic_usage.axm`
- `samples/functions/arrayclone/unallocated_and_empty.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`

### Решения
- `ARRAYCLONE` сохраняет тип, состояние и границы исходного массива, но не разделяет с ним хранилище элементов.
- Общий путь связывания теперь готов для следующих функций массива, где сигнатура зависит от фактического типа аргумента.
- Общий путь построения массива произвольного типа элемента больше не привязан только к строковым результатам.

### Следующий точный шаг
- Реализовать `ARRAYSLICE` как следующую чистую операцию над одномерным массивом.
- Переиспользовать новый общий путь связывания и построения массива, не возвращаясь к частным решениям под отдельные типы.

## Session 171 - 2026-04-20 10:19 - ARRAYSLICE

### Что изменилось
- Реализована `ARRAYSLICE(array, start, count)` как следующая чистая функция над одномерными массивами.
- Функция создаёт независимый массив того же типа из части исходного массива и возвращает компактные границы результата.
- Добавлены проверки для невыделенного исходного массива, отрицательного количества элементов и выхода диапазона среза за границы.
- Обновлены автоматические проверки, пользовательская справка, проектные документы и запускаемые примеры для `ARRAYSLICE`.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayslice/basic_usage.axm`
- `samples/functions/arrayslice/zero_count.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYSLICE` использует логический индекс исходного массива, а не смещение от нуля.
- Непустой срез всегда возвращается в компактной форме `1 TO N`, независимо от нижней границы исходного массива.
- При `count = 0` результатом становится пустой выделенный массив `1 TO 0`, даже если `start` сам по себе не попадает в границы исходного массива.

### Следующий точный шаг
- Реализовать `ARRAYCONCAT(left, right)` как следующую чистую операцию над одномерными массивами.
- Переиспользовать уже готовый общий путь связывания и построения компактного массива.
## Session 172 - 2026-04-20 10:36 - Axiom smoke для восстановления битой копии PLANS

### Что изменилось
- Добавлен запускаемый smoke-пример `samples/smoke/repair_plans_mojibake.axm`.
- Пример не трогает исходный `docs/PLANS.md`: он сначала создаёт рабочую копию `docs/PLANS.axiom-work.md`, затем читает копию и восстанавливает только строки, похожие на типичную поломку вида В«UTF-8-текст однажды ошибочно прошёл через Windows-1251В».
- Внутри примера реализован целевой восстановитель на самом Axiom:
  - разбор текста по строкам через `SPLIT` и сборка через `JOIN`;
  - проверка подозрительных строк;
  - обратное получение байтов cp1251 через `ASC` и таблицу соответствий;
  - повторное декодирование потока как UTF-8 с помощью строковых операций, массивов и побитовых операций.
- Добавлена автоматическая проверка в `tests/Axiom.Runtime.Tests/InterpreterTests.cs`, которая запускает этот smoke-пример на маленьком искусственном файле со смешанным содержимым: две битые строки и одна уже исправная строка.

### Какие файлы изменены
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/WORKLOG2.md`

### Проверка
- Нужен прогон рантайм-тестов и отдельный запуск smoke-примера на реальной рабочей копии `docs/PLANS.axiom-work.md`.

### Решения
- Этот пример не претендует на роль универсального конвертера кодировок.
- Он целенаправленно проверяет, что текущий Axiom уже способен решать практически полезную задачу восстановления текста при конкретном типе поломки кодировки.
- Исходный `docs/PLANS.md` по-прежнему остаётся нетронутым.

### Следующий точный шаг
- Прогнать новый рантайм-тест.
- Затем выполнить `samples/smoke/repair_plans_mojibake.axm` через текущий конвейер выполнения и посмотреть, насколько хорошо он восстановил `docs/PLANS.axiom-work.md`.
## Session 173 - 2026-04-20 10:57 - Многопроходное восстановление битой копии PLANS

### Что изменилось
- В `samples/smoke/repair_plans_mojibake.axm` добавлена функция `RepairLine`, которая повторно декодирует одну строку до устойчивого результата и тем самым покрывает не только одинарную, но и двойную порчу вида В«UTF-8 -> Windows-1251 -> сохранение».
- В `tests/Axiom.Runtime.Tests/InterpreterTests.cs` обновлена проверка smoke-сценария: новая рабочая версия теста строит исходный файл программно и проверяет сразу два случая, одинарную и двойную порчу кодировки.
- Исходный `docs/PLANS.md` по-прежнему не изменялся; работа велась только через `docs/PLANS.axiom-work.md`.

### Какие файлы изменены
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- Отдельный прогон `samples/smoke/repair_plans_mojibake.axm` через `AxiomExecutionPipeline`: `STATUS=Success`, вывод `TRUE`, `1388`, `TRUE`
- Повторная проверка `rg -n 'ПР|СР|рР|вЂ|в„' C:\Axiom\docs\PLANS.axiom-work.md` больше не находит остаточных битых фрагментов.

### Решения
- Для реального файла понадобился именно многопроходный подход: часть строк в верхней части `PLANS.md` была повреждена повторно, поэтому одного прохода было недостаточно.
- Эксперимент подтверждает, что текущий Axiom уже способен решать прикладную задачу восстановления текста при конкретном типе поломки кодировки.

### Следующий точный шаг
- При отдельном подтверждении решить, переносим ли восстановленный текст из `docs/PLANS.axiom-work.md` обратно в `docs/PLANS.md`.
- Если перенос пока не нужен, вернуться к основному плану развития языка и следующему пункту из `docs/PLANS.md`.
## Session 174 - 2026-04-20 11:16 - Восстановление PLANS и синхронизация словаря IDE

### Что изменилось
- Основной `docs/PLANS.md` восстановлен из рабочей копии `docs/PLANS.axiom-work.md`; верхняя часть файла снова читается в UTF-8.
- В `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd` синхронизированы словари раскраски IDE с фактическим языком:
  - добавлены пропущенные служебные слова `APPEND`, `OPEN`, `CLOSE`, `OUTPUT`, `ERASE`, `REDIM`, `PRESERVE`;
  - тип `FILE` добавлен в типы;
  - добавлены встроенные функции строк, массивов, файлового ввода-вывода и файловой системы, которые уже есть в семантике языка.
- В `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs` форматировщик научен распознавать операторы `<<`, `>>`, `\` и `^`, чтобы `Classic` и `Modern` больше не ломали их и не пропускали.
- В тестах IDE добавлены проверки полного покрытия словаря подсветки и точечные проверки новых сценариев форматирования.

### Какие файлы изменены
- `docs/PLANS.md`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/WORKLOG2.md`

### Проверка
- Быстрая проверка `docs/PLANS.md` по UTF-8: заголовок `# План работ` и верхние разделы читаются корректно.
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`

### Решения
- Словарь IDE больше не поддерживается выборочно по мере всплывающих случаев; теперь тесты проверяют покрытие всех ключевых слов и всех встроенных функций относительно фактического синтаксиса и семантики.
- Режим `Modern` оставлен в текущем договоре: первая буква заглавная, остальные строчные, поэтому `COPYFILE` отображается как `Copyfile`, а не `CopyFile`.

### Следующий точный шаг
- Выполнить короткую ручную проверку в `Axiom.Ide` на `samples/smoke/repair_plans_mojibake.axm`, чтобы глазами подтвердить раскраску и переключение `Classic`/`Modern` для новых слов.
- После этого вернуться к основному плану развития массивов.
## Session 175 - 2026-04-20 11:38 - ARRAYCONCAT для одномерных массивов

### Что изменилось
- Реализована функция `ARRAYCONCAT(left, right)` как следующая чистая операция над одномерными массивами.
- Семантика теперь требует два одномерных массива одного и того же типа и возвращает результат того же типа.
- Рантайм создаёт новый независимый компактный массив: непустой результат получает границы `1 TO N`, а два выделенных пустых операнда дают выделенный пустой массив `1 TO 0`.
- Для невыделенного `left` или `right` добавлена диагностика времени выполнения `MB3005`; текст сообщения теперь различает имена аргументов.
- Обновлены пользовательская справка, индекс справки, спецификация, пример для ручной проверки и словарь подсветки IDE.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayconcat/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Для `ARRAYCONCAT` выбран строгий договор: конкатенация разрешена только между массивами одинакового типа, без неявных преобразований.
- Результат всегда компактен и не сохраняет нижние границы исходных массивов, что делает поведение согласованным с `ARRAYSLICE`.
- Пустой выделенный результат нормализуется к форме `1 TO 0`, чтобы все чистые одномерные операции возвращали одинаковую форму пустого массива.

### Следующий точный шаг
- Реализовать `ARRAYTOREVERSED(array)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор независимого результата, компактных границ и ошибки `MB3005` для невыделенного исходного массива.
## Session 176 - 2026-04-20 11:51 - ARRAYTOREVERSED для одномерных массивов

### Что изменилось
- Реализована функция `ARRAYTOREVERSED(array)` как следующая чистая операция над одномерными массивами.
- Семантика теперь принимает один одномерный массив и возвращает новый массив того же типа.
- Рантайм создаёт новый независимый компактный массив с обратным логическим порядком элементов.
- Для пустого выделенного исходного массива результат нормализуется к форме `1 TO 0`.
- Для невыделенного исходного массива добавлена диагностика времени выполнения `MB3005`.
- Обновлены пользовательская справка, индекс справки, спецификация, пример для ручной проверки, план и словарь подсветки IDE.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraytoreversed/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYTOREVERSED` сохраняет общий договор чистых одномерных операций: исходный массив не меняется, а результат всегда отделён от исходного хранилища.
- Нижние и верхние границы исходного массива не сохраняются: любой непустой результат нормализуется к компактной форме `1 TO N`.
- Пустой выделенный результат приводится к форме `1 TO 0`, чтобы поведение оставалось согласованным с `ARRAYSLICE` и `ARRAYCONCAT`.

### Следующий точный шаг
- Реализовать `ARRAYTAKE(array, count)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор независимого результата, компактных границ, пустого результата `1 TO 0`, ошибки `MB3005` для невыделенного массива и ошибки `MB3007` для отрицательного `count`.
## Session 177 - 2026-04-20 12:07 - ARRAYTAKE для одномерных массивов

### Что изменилось
- Реализована функция `ARRAYTAKE(array, count)` как следующая чистая операция над одномерными массивами.
- Семантика теперь принимает один одномерный массив и число элементов, затем возвращает новый массив того же типа.
- Рантайм создаёт новый независимый компактный массив из первых логических элементов исходного массива.
- Если `count = 0` или исходный массив выделен, но пуст, результат нормализуется к форме `1 TO 0`.
- Если `count` больше длины массива, функция мягко возвращает весь массив без ошибки.
- Для невыделенного исходного массива добавлена диагностика `MB3005`, для отрицательного `count` — `MB3007`.
- Обновлены пользовательская справка, индекс справки, спецификация, план, ворклог, пример для ручной проверки и словарь подсветки IDE.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraytake/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYTAKE` спроектирована как массивный аналог `LEFT`, а не как строгий частный случай `ARRAYSLICE`: слишком большой `count` не считается ошибкой.
- Результат всегда отделён от исходного хранилища и нормализуется к компактным границам `1 TO N`.
- Пустой выделенный результат приводится к форме `1 TO 0`, чтобы поведение оставалось согласованным с `ARRAYSLICE`, `ARRAYCONCAT` и `ARRAYTOREVERSED`.

### Следующий точный шаг
- Реализовать `ARRAYSKIP(array, count)` как следующую чистую операцию над одномерным массивом.
- Сохранить тот же общий договор независимого результата, компактных границ, пустого результата `1 TO 0`, ошибки `MB3005` для невыделенного массива и ошибки `MB3007` для отрицательного `count`.

## Session 178 - 2026-04-20 12:17 - ARRAYSKIP и ARRAYDISTINCT для одномерных массивов

### Что изменилось
- Реализованы функции `ARRAYSKIP(array, count)` и `ARRAYDISTINCT(array)` как следующий пакет чистых операций над одномерными массивами.
- Семантика теперь принимает новый вызов с пропуском начальной части массива и новый вызов с устранением повторов, в обоих случаях возвращая новый массив того же типа.
- Рантайм создаёт независимый компактный результат: `ARRAYSKIP` возвращает хвост массива после пропуска первых логических элементов, а `ARRAYDISTINCT` сохраняет только первые вхождения уникальных значений.
- Для `ARRAYSKIP` зафиксировано мягкое поведение на больших значениях `count`: если пропуск покрывает всю текущую длину массива, результат нормализуется к форме `1 TO 0` без ошибки.
- Для `ARRAYDISTINCT` выбран договор сохранения порядка первых появлений, чтобы результат был предсказуемым и пригодным для последующих операций перечисления.
- Обновлены автоматические проверки, пользовательская справка, индекс справки, спецификация, план, ворклог, запускаемые примеры и словарь подсветки IDE.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayskip/basic_usage.axm`
- `samples/functions/arraydistinct/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYSKIP` спроектирована как массивный аналог пропуска начальной части, а не как строгая ошибка при слишком большом `count`: пустой хвост считается корректным результатом.
- `ARRAYDISTINCT` не сортирует значения и не меняет их относительный порядок, а только удаляет повторные появления после первого.
- Для сравнения значений в `ARRAYDISTINCT` используется уже существующее правило равенства времени выполнения, чтобы массивные функции не вводили отдельную модель сравнения.

### Следующий точный шаг
- Реализовать согласованный пакет поисковых и счётных функций над одномерными массивами: `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`.
- До начала реализации письменно зафиксировать единый договор для логических индексов результата поиска и для случая отсутствия совпадения.

## Session 179 - 2026-04-20 12:34 - Уточнение договора поисковых функций массива

### Что изменилось
- Проведена точечная проверка уже реализованных функций одномерных массивов после обсуждения нижних границ массива.
- Подтверждено по текущей спецификации, справке, рантайму и тестам, что Axiom уже поддерживает произвольную нижнюю границу массива, а не только `1`.
- Зафиксирован проектный договор для следующего поискового пакета: `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`.
- Для отсутствия совпадения выбран не фиксированный индекс, а формула `LBOUND(array) - 1`, потому что фиксированные значения `0` и `-1` могут быть допустимыми индексами реального массива.
- В договор отдельно добавлено требование до реализации закрыть крайний случай переполнения для массива с нижней границей на минимуме диапазона `INTEGER`.

### Какие файлы изменены
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Выполнена ручная сверка спецификации, справки, рантайма и существующих тестов по реализованным функциям массива.
- Новые автоматические тесты не запускались, потому что в этом шаге код поведения не менялся.

### Решения
- Ранее реализованные функции `ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTOREVERSED`, `ARRAYTAKE`, `ARRAYSKIP` и `ARRAYDISTINCT` не пострадали от ошибочной устной формулировки про нижнюю границу `1`.
- Это подтверждается тем, что код либо сохраняет реальные границы исходного массива, либо явно читает `LowerBound` и `UpperBound`, а компактные границы `1 TO N` используются только как сознательная форма нового результирующего массива.
- Поисковый пакет будет реализован только группой, вместе с договором "не найдено" и защитой от переполнения.

### Следующий точный шаг
- Реализовать пакет `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF` и `ARRAYCOUNT` вместе с защитой для случая `LBOUND(array) - 1`.
- Сразу покрыть пакет тестами на массивы с нижними границами `1`, `0`, положительным сдвигом и отрицательным сдвигом.

## Session 180 - 2026-04-20 12:34 - Поисковый пакет одномерных массивов

### Что изменилось
- Реализован согласованный поисковый пакет одномерных массивов: `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF`, `ARRAYCOUNT`.
- В семантике второй аргумент этих функций теперь привязывается к типу элемента массива, а не к фиксированному служебному типу описателя.
- Рантайм возвращает логические индексы текущего массива, а при отсутствии совпадения для `ARRAYINDEXOF` и `ARRAYLASTINDEXOF` использует договор `LBOUND(array) - 1`.
- Для пустого выделенного массива пакет возвращает нормальные значения "не найдено" без перехода в ошибку.
- В этом же шаге закрыто ограничение на минимальную нижнюю границу массива: `DIM` и `REDIM` теперь запрещают нижнюю границу `-2147483648` и сообщают `MB3007`.
- Дополнительно приведён к безопасному обход индексов в рантайме для массивных циклов копирования и перечисления, чтобы крайние допустимые границы не создавали скрытого переполнения счётчиков цикла.
- Обновлены автоматические проверки, пользовательская справка, индекс справки, спецификация, план, ворклог, запускаемые примеры и словарь подсветки IDE.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraycontains/basic_usage.axm`
- `samples/functions/arraycount/basic_usage.axm`
- `samples/functions/arrayindexof/basic_usage.axm`
- `samples/functions/arraylastindexof/basic_usage.axm`
- `samples/operators/dim/minimum_lower_bound_error.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Для поискового пакета выбрано единое правило "не найдено": `FALSE`, `0` и `LBOUND(array) - 1` вместо фиксированных чисел `0` или `-1`.
- Ограничение на минимальную нижнюю границу массива введено на уровне выделения диапазона, а не как частная оговорка только внутри поисковых функций.
- По следующему шагу зафиксировано прямое продолжение из Axiom-подхода: `ARRAYFINDALL(array, value)`.

### Следующий точный шаг
- Реализовать `ARRAYFINDALL(array, value)` как следующую функцию одномерных массивов.
- Сразу вернуть результат как компактный массив индексов `INTEGER()` и покрыть случаи с границами `1`, `0`, положительным и отрицательным смещением.

## Session 181 - 2026-04-20 13:24 - ARRAYFINDALL

### Что изменилось
- Реализована `ARRAYFINDALL(array, value)` как прямое продолжение поискового набора одномерных массивов.
- В семантике второй аргумент функции привязывается к типу элемента исходного массива, а результат всегда описывается как `INTEGER()`.
- Рантайм собирает все логические индексы совпадений в новый компактный массив `1 TO N`, а при отсутствии совпадений возвращает выделенный пустой массив `1 TO 0`.
- Обновлены targeted tests, runnable sample, manual, specification и план, а также подсветка IDE для новой built-in функции.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayfindall/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYFINDALL` не вводит новую модель сравнения: используется то же правило равенства значений, что уже действует для `ARRAYCONTAINS`, `ARRAYINDEXOF`, `ARRAYLASTINDEXOF` и `ARRAYCOUNT`.
- В качестве публичного договора возвращается не первое или последнее совпадение, а полный набор логических индексов, но всегда в компактном массиве `INTEGER()`.

### Следующий точный шаг
- Зафиксировать договор и реализовать `ARRAYBINARYSEARCH(array, value)` как отдельный завершающий read-only подпакет этапа поиска.

## Session 182 - 2026-04-20 13:40 - ARRAYBINARYSEARCH

### Что изменилось
- Реализована `ARRAYBINARYSEARCH(array, value)` как завершающий read-only шаг поискового пакета одномерных массивов.
- В семантике второй аргумент функции привязывается к типу элемента массива, а для первого аргумента введена защита: `ARRAYBINARYSEARCH` принимает только числовые и строковые массивы.
- Рантайм выполняет бинарный поиск по уже отсортированному диапазону и возвращает первый логический индекс совпадения; если совпадения нет, возвращается договорное значение `LBOUND(array) - 1`.
- Обновлены runnable sample, user-facing manual и алфавитный каталог manual через `manual.js`; отдельно выяснено и зафиксировано, что пользователь смотрел другую рабочую копию (`E:\basic`), тогда как текущая реализация ведётся в `C:\Axiom`.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraybinarysearch/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- `ARRAYBINARYSEARCH` не вводит новую модель сравнения: используется существующее упорядоченное сравнение рантайма для чисел и строк.
- Для дубликатов публичный договор сделан детерминированным: функция возвращает первый логический индекс равного элемента, а не произвольное найденное совпадение.
- Проверка отсортированности массива в рантайм не добавлялась: это осознанное предусловие функции, чтобы не разрушать ожидаемую стоимость бинарного поиска.

### Следующий точный шаг
- Зафиксировать договор сортировки и реализовать `ARRAYTOSORTED(array)` как прямое продолжение массива преобразований и естественную опору для `ARRAYBINARYSEARCH`.

## Session 183 - 2026-04-20 13:58 - ARRAYTOSORTED

### Что изменилось
- Реализована `ARRAYTOSORTED(array)` как новая чистая операция над одномерными массивами.
- В семантике добавлена защита: `ARRAYTOSORTED` принимает только массивы с числовыми или строковыми элементами и возвращает массив того же типа.
- Рантайм строит новую отсортированную копию массива, сохраняет исходные логические границы и не изменяет исходное значение.
- Обновлены runnable sample, manual, индекс manual и спецификация под договор сохранения границ.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraytosorted/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Для `ARRAYTOSORTED` принят пользовательский договор сохранения исходных границ вместо нормализации к `1 TO N`.
- Функция не становится скрытой формой `ARRAYDISTINCT`: дубликаты сохраняются.
- Поддержка ограничена теми же типами с упорядоченным сравнением, что уже используются для `ARRAYBINARYSEARCH`: числа и строки.

### Следующий точный шаг
- Перейти к этапу mutating-операций и зафиксировать договор для `ARRAYFILL(array, value)` как следующего базового built-in над одномерным массивом.

## Session 184 - 2026-04-20 15:13 - ARRAYFILL, ARRAYREVERSE, ARRAYSORT

### Что изменилось
- Реализован первый mutating-блок одномерных массивов: `ARRAYFILL(array, value)`, `ARRAYREVERSE(array)` и `ARRAYSORT(array)`.
- Пакет оформлен как built-in процедуры (`VOID`): они вызываются отдельной инструкцией и меняют уже выделенный массив на месте.
- В семантике добавлена привязка значения `ARRAYFILL` к типу элемента массива и защита для `ARRAYSORT`: сортировка разрешена только для числовых и строковых массивов.
- Рантайм выполняет массовое заполнение, разворот и сортировку без изменения логических границ массива; для пустого выделенного массива `1 TO 0` все три процедуры допустимы и ничего не ломают.
- Обновлены manual, алфавитный каталог manual, runnable samples и спецификация под новый mutating-пакет.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayfill/basic_usage.axm`
- `samples/functions/arrayreverse/basic_usage.axm`
- `samples/functions/arraysort/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Mutating array built-ins сделаны процедурами, а не функциями с фиктивным `BOOLEAN`-результатом.
- Для всего блока принят единый договор: операции меняют массив на месте, но не меняют его текущие границы.
- `ARRAYSORT` делит ту же модель упорядоченного сравнения, что уже использует `ARRAYBINARYSEARCH` и `ARRAYTOSORTED`.

### Следующий точный шаг
- Зафиксировать договор и реализовать следующий mutating-блок `ARRAYPUSH(array, value)` и `ARRAYPOP(array)`.

## Session 185 - 2026-04-20 15:33 - ARRAYPUSH, ARRAYPOP

### Что изменилось
- Реализован mutating-блок `ARRAYPUSH(array, value)` и `ARRAYPOP(array)` для одномерных массивов.
- `ARRAYPUSH` оформлен как built-in-процедура: операция расширяет массив вправо, мутирует уже выделенный массив на месте, сохраняет нижнюю границу для непустого массива и принимает значение того же типа, что и элементы массива.
- `ARRAYPOP` оформлен как built-in-функция: операция возвращает последний элемент, сужает массив на месте и при удалении последнего значения нормализует массив к каноническому пустому диапазону `1 TO 0`.
- Для канонического пустого массива `1 TO 0` разрешён `ARRAYPUSH`, но запрещён `ARRAYPOP`; невыделенный массив по-прежнему сообщает `MB3005`, а ошибки пустого pop и переполнения границы сообщают `MB3007`.
- Синхронизированы спецификация, manual, алфавитный каталог manual, IDE highlighting и runnable samples.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arraypush/basic_usage.axm`
- `samples/functions/arraypop/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Для расширения и сужения массива принят правый mutating-договор: `ARRAYPUSH` добавляет значение в конец текущего диапазона, а `ARRAYPOP` удаляет и возвращает значение с правой границы.
- `ARRAYPOP` сделан функцией, а не процедурой с побочным результатом: язык уже поддерживает вычисление выражения отдельной инструкцией, поэтому результат можно либо использовать, либо сознательно проигнорировать.
- После полного опустошения массив нормализуется к каноническому пустому виду `1 TO 0`; это упрощает дальнейшие операции и делает поведение согласованным с уже принятым договором пустого выделенного массива.

### Следующий точный шаг
- Зафиксировать договор и реализовать indexed mutating-блок `ARRAYINSERT(array, index, value)` и `ARRAYREMOVE(array, index)`.
- До кода явно описать: работают ли операции в логических индексах массива, как сдвигаются элементы, как меняются границы и как именно выглядит переход к пустому массиву.
## Session 186 - 2026-04-20 15:38 - Исправление сборки ARRAYFINDALL для IDE

### Что изменилось
- Исправлена регрессия сборки в `Axiom.Runtime`, из-за которой `dotnet run --project C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj` завершался с `CS1503` и IDE не запускалась.
- В `EvaluateArrayFindAll` восстановлено корректное согласование типа для `CreateCompactArrayValue`: helper ожидает `ArrayTypeSymbol`, а не общий `TypeSymbol`.

### Какие файлы изменены
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj --no-restore`
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`

### Решения
- Это не изменение пользовательского договора массивов, а точечный compile-fix после предыдущего этапа; manual, specification и plan не менялись.
- Следующий шаг по плану остаётся прежним: indexed mutating-блок `ARRAYINSERT(array, index, value)` и `ARRAYREMOVE(array, index)`.

## Session 187 - 2026-04-20 15:49 - ARRAYINSERT, ARRAYREMOVE

### Что изменилось
- Реализован indexed mutating-блок `ARRAYINSERT(array, index, value)` и `ARRAYREMOVE(array, index)` для одномерных массивов.
- `ARRAYINSERT` оформлен как built-in-процедура: операция работает по логическим индексам, расширяет массив на месте, сдвигает хвост вправо и допускает вставку в хвост по позиции `UBOUND + 1`.
- `ARRAYREMOVE` оформлен как built-in-функция: операция возвращает удалённый элемент, сдвигает хвост влево и при удалении единственного элемента нормализует массив к каноническому пустому диапазону `1 TO 0`.
- Для binder добавлена типизация `value` у `ARRAYINSERT` по типу элемента массива, а результат `ARRAYREMOVE` выводится как тип элемента первого аргумента массива.
- Синхронизированы targeted tests, manual, manual index, specification, architecture, IDE highlighting и runnable samples под новый блок.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayinsert/basic_usage.axm`
- `samples/functions/arrayremove/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Операции индексированного изменения массива закреплены в логических индексах текущего диапазона, чтобы поведение оставалось согласованным с `LBOUND`, `UBOUND`, поисковым пакетом и `ARRAYSLICE`.
- `ARRAYINSERT` сделан процедурой, потому что его единственный пользовательский эффект — мутация массива.
- `ARRAYREMOVE` сделан функцией, потому что удаляемое значение естественно становится результатом выражения и это согласуется с уже введённым паттерном `ARRAYPOP`.

### Следующий точный шаг
- Перейти к этапу higher-order массивных функций и до кода зафиксировать минимальный первый срез без расширения объёма этапа.
- Не размораживать отложенные `ARRAYSHIFT` / `ARRAYUNSHIFT`, пока для них не появится отдельная предметная необходимость.

## Session 188 - 2026-04-20 16:07 - ARRAYSOME, ARRAYEVERY

### Что изменилось
- Зафиксирован минимальный стартовый higher-order-срез массива как predicate-подпакет `ARRAYEVERY(array, predicate)` и `ARRAYSOME(array, predicate)`.
- В семантике array-binding-путь расширен типизацией callback-аргумента по типу элемента массива: второй аргумент резолвится как `FUNCTION(T) AS BOOLEAN`.
- В runtime добавлен обход уже выделенного массива с вызовом переданного значения функции или замыкания по каждому элементу.
- `ARRAYEVERY` возвращает `TRUE` для каждого элемента и вакуозно `TRUE` для пустого выделенного массива `1 TO 0`; `ARRAYSOME` возвращает `TRUE` хотя бы для одного элемента и `FALSE` для пустого выделенного массива.
- Пустое значение функционального типа в callback сообщает `MB3009`, как и обычный прямой вызов функционального значения.
- Синхронизированы targeted tests, manual, manual index, specification, architecture, IDE highlighting и runnable samples.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `samples/functions/arrayevery/basic_usage.axm`
- `samples/functions/arraysome/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Первый higher-order-шаг намеренно ограничен predicate-подпакетом, чтобы не смешивать в одном изменении callback-механику, producer-массивы `ARRAYMAP`/`ARRAYFILTER` и аккумуляторный контракт `ARRAYREDUCE`.
- Callback в этом шаге получает только значение элемента массива; индекс и сам массив не добавлялись, чтобы не расширять контракт раньше необходимости.
- Для higher-order built-ins принят тот же путь значений функций и замыканий, который уже действует в языке; строковые имена callback-ов не вводятся.

### Следующий точный шаг
- Зафиксировать договор и реализовать `ARRAYFILTER(array, predicate)` как следующий pure higher-order built-in поверх уже введённого predicate-механизма.
- До кода явно описать компактность результата `1 TO N`, поведение для пустого выделенного массива `1 TO 0` и связь нового шага с пока ещё не начатыми `ARRAYMAP` и `ARRAYREDUCE`.

## Session 189 - 2026-04-20 16:09 - Финализация ARRAYSOME и ARRAYEVERY

### Что сделано
- Добавлен недостающий runnable acceptance sample `samples/functions/arrayevery/basic_usage.axm`.
- Проверено, что парный sample `samples/functions/arraysome/basic_usage.axm` уже присутствует и соответствует договору predicate built-ins.
- Удалён случайно попавший в `docs/WORKLOG2.md` хвост patch-текста, чтобы сохранить документ в корректном append-only виде.
- Выполнена финальная валидация реализации `ARRAYEVERY` и `ARRAYSOME`.

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Статус predicate higher-order-подпакета подтверждён как завершённый: код, tests, manual, manual index и acceptance samples синхронизированы.
- Следующий шаг плана не меняется: после predicate-подпакета идти в `ARRAYFILTER(array, predicate)` как первый producer higher-order built-in.

### Следующий точный шаг
- Зафиксировать договор и реализовать `ARRAYFILTER(array, predicate)` как следующий pure higher-order built-in поверх уже введённого predicate-механизма.
- До кода явно описать компактность результата `1 TO N`, поведение для пустого выделенного массива `1 TO 0` и связь нового шага с пока ещё не начатыми `ARRAYMAP` и `ARRAYREDUCE`.

## Session 190 - 2026-04-20 16:18 - ARRAYFILTER

### Что сделано
- Зафиксирован и реализован `ARRAYFILTER(array, predicate)` как первый producer higher-order built-in поверх уже введённого predicate-механизма.
- В semantics добавлены `BuiltinFunctionId.ArrayFilter`, descriptor built-in и вывод callback-типа `FUNCTION(T) AS BOOLEAN` от типа элемента первого аргумента массива.
- В runtime добавлен dispatch `ARRAYFILTER` и read-only реализация, которая переиспользует существующий путь вызова function values / closures и строит новый компактный массив `1 TO N` или `1 TO 0`.
- Обновлены user-facing manual, manual index и IDE highlighting.
- Добавлен runnable acceptance sample `samples/functions/arrayfilter/basic_usage.axm`.

### Изменённые файлы
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arrayfilter/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Первый producer higher-order built-in намеренно сохранён в минимальном виде: callback принимает только значение элемента и возвращает `BOOLEAN`.
- `ARRAYFILTER` не наследует границы исходного массива; producer-результат всегда нормализуется к компактной форме `1 TO N` или `1 TO 0`.
- Шаг не расширялся до `ARRAYMAP` и `ARRAYREDUCE`, чтобы не смешивать в одном изменении predicate producer и будущую polymorphic result-type механику.

### Следующий точный шаг
- До кода зафиксировать договор `ARRAYMAP(array, selector)` и явно решить, будет ли первый вариант поддерживать только преобразование `T -> T` или потребует нового механизма вывода/result-type для `T -> U`.
- После фиксации договора реализовать `ARRAYMAP`, не размораживая в том же изменении `ARRAYREDUCE`.

## Session 191 - 2026-04-20 16:24 - Договор same-type ARRAYMAP

### Что сделано
- Зафиксирован минимальный договор `ARRAYMAP(array, selector)` как следующего higher-order producer built-in.
- Для первого варианта выбран контракт `FUNCTION(T) AS T` без polymorphic-расширения `T -> U`.
- Зафиксировано, что `ARRAYMAP` должен возвращать новый компактный массив `1 TO N` или `1 TO 0` и не мутировать исходный массив.

### Изменённые файлы
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Команды проверки не запускались: в этом шаге менялись только repository docs.

### Решения
- Первый ввод `ARRAYMAP` намеренно ограничен same-type преобразованием, чтобы не вводить новый механизм вывода типа результата built-in по callback-у.
- Общий случай `T -> U`, если понадобится, остаётся отдельным будущим решением и не смешивается с первым шагом `ARRAYMAP`.

### Следующий точный шаг
- Реализовать `ARRAYMAP(array, selector)` в semantics/runtime/manual/tests и добавить runnable acceptance sample.
- В этом шаге не расширять договор до `T -> U` и не смешивать реализацию `ARRAYMAP` с `ARRAYREDUCE`.

## Session 192 - 2026-04-20 16:29 - ARRAYMAP

### Что сделано
- Реализован `ARRAYMAP(array, selector)` как same-type higher-order producer built-in поверх уже введённого callback-механизма для массивов.
- В semantics добавлены `BuiltinFunctionId.ArrayMap`, descriptor built-in и вывод callback-типа `FUNCTION(T) AS T` от типа элемента первого аргумента массива.
- В runtime добавлен dispatch `ARRAYMAP`, общий unary callback-вызов для higher-order array built-ins и read-only реализация, которая собирает новый компактный массив `1 TO N` или `1 TO 0`.
- Обновлены user-facing manual, manual index и IDE highlighting.
- Добавлен runnable acceptance sample `samples/functions/arraymap/basic_usage.axm`.

### Изменённые файлы
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `samples/functions/arraymap/basic_usage.axm`

### Проверка
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `node --check C:\Axiom\docs\manual\scripts\manual.js`

### Решения
- Первый ввод `ARRAYMAP` сознательно оставлен same-type-операцией `FUNCTION(T) AS T`, чтобы не вводить новый polymorphic result-type inference в этом же шаге.
- `ARRAYMAP`, как и `ARRAYFILTER`, не наследует границы исходного массива; producer-результат всегда нормализуется к компактной форме `1 TO N` или `1 TO 0`.
- Следующий higher-order шаг переносится на `ARRAYREDUCE`, а общий случай `ARRAYMAP` с `T -> U` остаётся отложенным отдельным решением.

### Следующий точный шаг
- До кода зафиксировать договор `ARRAYREDUCE(array, seed, reducer)` как следующего higher-order built-in и явно описать минимальный accumulator-контракт.
- После фиксации договора реализовать `ARRAYREDUCE`, не расширяя в том же изменении `ARRAYMAP` до `T -> U`.

## Session 193 - 2026-04-20 16:40 - ARRAYREDUCE

### Done
- Зафиксирован и реализован `ARRAYREDUCE(array, seed, reducer)` как minimal same-type accumulator built-in.
- В semantics добавлены `BuiltinFunctionId.ArrayReduce`, descriptor built-in, вывод `seed` как типа элемента массива и вывод callback-типа `FUNCTION(T, T) AS T` от типа элемента первого аргумента.
- В runtime добавлен dispatch `ARRAYREDUCE`, обобщён higher-order callback-вызов на произвольное число аргументов и реализована read-only свёртка массива слева направо с возвратом `seed` для пустого выделенного массива.
- Обновлены manual, manual index и обзорная manual-страница, чтобы `ARRAYREDUCE` был доступен и через справочную статью по массивам, и через общий каталог.
- Добавлен runnable acceptance sample `samples/functions/arrayreduce/basic_usage.axm`.
- Выполнена финальная валидация шага через профильные test-проекты и `node --check` для manual index script.

### Files changed
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/arrayreduce/basic_usage.axm`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`

### Decisions
- Первый ввод `ARRAYREDUCE` остаётся same-type-контрактом `seed: T`, `reducer: FUNCTION(T, T) AS T`; отдельный тип аккумулятора не вводится.
- Runtime callback-путь теперь допускает unary и binary higher-order built-ins без нового специального слоя только для reducer-операций.
- Minimal higher-order array package на этом шаге покрывает три формы сценариев: predicate (`ARRAYEVERY`/`ARRAYSOME`), producer (`ARRAYFILTER`/`ARRAYMAP`) и accumulator (`ARRAYREDUCE`).

### Next step
- До кода отдельно зафиксировать, нужен ли второй higher-order этап с polymorphic result-type / accumulator-type machinery для `ARRAYMAP(array, selector)` как `FUNCTION(T) AS U` и `ARRAYREDUCE(array, seed, reducer)` как `FUNCTION(A, T) AS A`.
- Если такой этап действительно нужен, сначала описать его в specification и architecture, не смешивая это решение с уже завершённым minimal same-type пакетом higher-order built-ins.

## Session 194 - 2026-04-20 16:49 - Фиксация закрытого minimal higher-order пакета

### Done
- Перечитаны project docs и подтверждено, что minimal higher-order пакет массивов уже закрыт без незавершённых same-type хвостов.
- В документации отдельно зафиксировано, что текущий стабильный пакет завершён в составе `ARRAYEVERY`, `ARRAYSOME`, `ARRAYFILTER`, `ARRAYMAP`, `ARRAYREDUCE`.
- Polymorphic higher-order (`ARRAYMAP` как `T -> U` и `ARRAYREDUCE` как `A + T -> A`) сохранён как отдельный отложенный этап, чтобы не потерять его и не разморозить случайно в рамках текущего стабильного среза.

### Files changed
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Decisions
- Текущий higher-order API по массивам считается минимально стабильным и завершённым без дополнительных same-type задач.
- Polymorphic-расширение остаётся полезным, но не относится к текущему завершённому пакету и требует отдельного этапа с собственным договором по type inference.

### Next step
- Не трогать polymorphic higher-order в коде до отдельной документной фиксации этапа 2.
- При выборе следующей реализации идти уже в другой срез roadmap, не смешивая его с отложенным polymorphic higher-order.

## Session 195 - 2026-04-20 17:17 - Phase 9 validation и cleanup смежных regression tests

### Done
- Подтверждена автоматическая часть `Phase 9` для `IDE Redesign v0.2`: `dotnet build Axiom.sln` и `dotnet test Axiom.sln` проходят целиком, включая `Axiom.Ide.Tests`.
- По пути устранён смежный блокер вне IDE-slice: `Axiom.Semantics.Tests` падал на кластере array-builtins (`ARRAYCLONE`, `ARRAYSLICE`, `ARRAYCONCAT`, `ARRAYTOREVERSED`, `ARRAYPUSH`/`ARRAYPOP`, `ARRAYINSERT`/`ARRAYREMOVE`, `ARRAYMAP`, whole-array assignment).
- В `Binder.Expressions` скорректирован fallback result type для `ARRAYTOSORTED`/`ARRAYSORT`/`ARRAYBINARYSEARCH` при ordered-array ошибке, чтобы не плодить лишний каскадный `MB2003` на последующем присваивании.
- В `BinderTests` выровнены ожидания по индексам `GlobalStatements` для array-builtins после деклараций `DIM`, чтобы тесты проверяли реальные assignment/expression nodes, а не соседние declaration statements.
- `docs/PLANS.md` синхронизирован: автоматическая regression-часть зафиксирована как выполненная, ручной shell smoke pass оставлен единственным незакрытым шагом `Phase 9`.

### Files changed
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet test C:\Axiom\tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet build C:\Axiom\Axiom.sln -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\Axiom.sln -p:UseSharedCompilation=false`

### Decisions
- `Phase 9` пока нельзя считать полностью закрытым без живого ручного smoke pass в `Axiom.Ide`, даже несмотря на зелёные build/test проверки.
- Смежные regression failures в language-core были устранены сразу, потому что без общего зелёного `dotnet test Axiom.sln` статус текущего milestone оставался бы формально недоказанным.

### Next step
- Выполнить живой ручной shell smoke pass в `Axiom.Ide`: проверить persisted shell-state (`WindowState`, `View -> File Manager`), новый `Find/Replace`, UX нижней панели (`Output`/`Diagnostics`, toggles, resize) и базовые file-manager сценарии на реальном WPF окне.

## Session 196 - 2026-04-20 18:24 - Автоматизированный WPF smoke harness

### Done
- Добавлен отдельный проект `Axiom.Ide.SmokeHarness`, который поднимает реальный `Axiom.Ide.exe`, готовит временную workspace и прогоняет shell smoke-pass через UI Automation.
- В shell XAML закреплены automation hooks для главного окна, меню, `File Manager`, нижней панели, status bar и `FindReplaceDialog`, чтобы harness мог стабильно находить ключевые UI-элементы.
- В `MainWindow.xaml.cs` добавлен узкий `WM_APP` automation bridge для smoke-команд `Find/Replace`, `Close Tab`, `Run` и toggle shell-панелей; bridge только маршрутизирует уже существующие viewmodel-команды и свойства.
- В harness реализованы и доведены до зелёного состояния сценарии: editable open, `.exe` not-open path, `Find/Replace`, discard dirty tab, `Run -> Output`, `Run -> Diagnostics`, toggle `Output`/`Diagnostics`/`Status Bar`, persisted `WindowState` и скрытие/восстановление `File Manager`.
- README, architecture и plan docs синхронизированы с новым automation tooling.

### Files changed
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide.SmokeHarness/Axiom.Ide.SmokeHarness.csproj`
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet build C:\Axiom\Axiom.sln -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet run --project C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -- --keep-workspace`

### Decisions
- Для WPF shell-команд не стоит опираться только на UIA-вызов меню: для smoke automation оставлен узкий `WM_APP` bridge, а UIA используется как канал наблюдения и взаимодействия с визуальными элементами.
- Owned-dialog окна (`FindReplaceDialog`, message boxes) нужно искать через `RootElement.Descendants`, а не только через `RootElement.Children`.
- `Phase 9` больше не зависит от отдельного ручного smoke-pass как единственного финального шага: актуальный baseline зафиксирован automated harness-ом.

### Next step
- Решить, нужен ли отдельный follow-up на расширение smoke harness до context-menu/file-operations сценариев `File Manager`, или текущий shell baseline уже достаточен и следующий IDE шаг должен идти в post-v0.2 polish.

## Session 197 - 2026-04-20 18:47 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ smoke harness

### Done
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ project context пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE-slice пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ `Phase 9` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `Axiom.Ide.SmokeHarness`.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Axiom.Ide.SmokeHarness` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `File Manager`: harness пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ tree-based open, blocked `.exe`, `Find/Replace`, `Run -> Output`, `Run -> Diagnostics`, toggle пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ persisted shell-state.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ harness пїЅ related IDE regression tests пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### Files changed
- `docs/WORKLOG2.md`

### Validation
- `dotnet build C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet run --project C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -- --keep-workspace`

### Decisions
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ harness пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `File Manager` tree-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ drive-based пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ follow-up пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ smoke baseline пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ harness пїЅпїЅ context-menu/file-operations, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ post-v0.2 polish.

### Next step
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell-smoke baseline, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ slice пїЅпїЅ context-menu/file-operations `File Manager`.
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE smoke tooling stable baseline пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ post-v0.2 cleanup/polish.

## Session 198 - 2026-04-20 19:18 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ smoke harness пїЅпїЅ file-operations File Manager

### Done
- `Axiom.Ide.SmokeHarness` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ end-to-end smoke-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ context-menu/file-operations пїЅ `File Manager`.
- пїЅ пїЅпїЅпїЅпїЅпїЅ WPF пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Show hidden files`, `New File`, `New Directory`, `Rename`, `Copy/Paste`, `Cut/Paste` пїЅ `Delete`.
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ delete-confirmation пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ fallback: harness пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ UIA-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ confirmation dialog пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- README, architecture пїЅ plan docs пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ coverage baseline harness.

### Files changed
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet build C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -p:UseSharedCompilation=false`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`
- `dotnet run --project C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -- --keep-workspace`

### Decisions
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ confirmation dialogs пїЅ пїЅпїЅпїЅпїЅ WPF smoke slice пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UIA-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ automation-пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅ file-operations smoke baseline пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell пїЅ filesystem, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ UIA-lifecycle пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- Context-menu/file-operations follow-up пїЅпїЅ `File Manager` пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Next step
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE slice пїЅпїЅпїЅпїЅпїЅ post-v0.2 polish / cleanup, пїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ smoke harness пїЅ пїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## Session 199 - 2026-04-20 19:25 - Help About пїЅ post-v0.2 polish

### Done
- Placeholder-пїЅпїЅпїЅпїЅпїЅ `Help -> About` пїЅ `Axiom.Ide` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `IDialogPromptService`.
- пїЅ `MainWindowViewModel` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `AboutCommand`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `About Axiom IDE` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ status bar trace `About dialog opened.`.
- пїЅ `MainWindow.xaml` Help-пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ disabled placeholder: `About` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ automation hooks `HelpMenuItem` / `HelpAboutMenuItem`.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regression checks пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ view model пїЅ XAML, пїЅпїЅпїЅпїЅпїЅ `About` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ placeholder.
- `docs/SPECIFICATION.md` пїЅ `docs/PLANS.md` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ shell behavior.

### Files changed
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`

### Decisions
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ post-v0.2 polish `Help -> About` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ info-dialog пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ prompt-service пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ VM.
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `Help` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ entry point пїЅ HTML manual, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ already-closed smoke harness slice.

### Next step
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE slice пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `Help -> Open Manual`, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ shell/UX polish-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regression-first пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## Session 200 - 2026-04-20 19:37 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Find regex пїЅ пїЅпїЅпїЅпїЅпїЅ smoke

### Done
- пїЅпїЅпїЅ `Find/Replace` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regression slice пїЅпїЅ regex-пїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ `EditorFindReplaceEngineTests` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regex-find, invalid regex pattern, regex replacement пїЅ capture groups пїЅ regex replace-all.
- пїЅ `FindReplaceDialog.xaml` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ automation hook `UseRegexCheckBox`, пїЅ XAML-regression tests пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ smoke harness.
- `Axiom.Ide.SmokeHarness` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Find / Replace dialog + regex mode`: harness пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Regex`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regex replace-all пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ end-to-end пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- пїЅ smoke harness пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `--ide-exe <path>`, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ live WPF smoke пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Axiom.Ide.exe`, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ output-path пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- README, architecture пїЅ plan docs пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ regex coverage пїЅ override-path пїЅпїЅпїЅпїЅпїЅпїЅпїЅ harness.

### Files changed
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/EditorFindReplaceEngineTests.cs`
- `src/Axiom.Ide.SmokeHarness/Program.cs`
- `src/Axiom.Ide.SmokeHarness/IdeSmokeHarness.cs`
- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-test-find\ -p:UseAppHost=false`
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-ide-smoke\`
- `dotnet build C:\Axiom\src\Axiom.Ide.SmokeHarness\Axiom.Ide.SmokeHarness.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-harness-smoke\`
- `dotnet C:\Axiom\.artifacts\isolated-harness-smoke\Debug\net10.0-windows\Axiom.Ide.SmokeHarness.dll --keep-workspace --ide-exe C:\Axiom\.artifacts\isolated-ide-smoke\Debug\net10.0-windows\Axiom.Ide.exe`

### Decisions
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regex-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ XAML presence-check пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ unit-regressions пїЅпїЅ search engine, пїЅ пїЅпїЅпїЅпїЅпїЅ WPF smoke, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ regex-пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ literal-find.
- пїЅпїЅпїЅпїЅпїЅ `--ide-exe` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ harness пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Axiom.Ide.exe` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Next step
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE slice пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Help -> Open Manual`, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell/UX polish bugfix пїЅ пїЅпїЅпїЅ пїЅпїЅ regression-first пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## Session 201 - 2026-04-20 20:04 - Help Open Manual

### Done
- пїЅ `Axiom.Ide` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Help -> Open Manual`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `docs/manual/index.html` пїЅпїЅпїЅпїЅпїЅ shell Windows пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅ `MainWindowViewModel` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OpenManualCommand`, пїЅпїЅпїЅпїЅпїЅ manual-path пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ error-path, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ manual пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅ shell-open пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `IExternalNavigationService`; пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Open externally`, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Process.Start` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ view model.
- Help-пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Open Manual`, `About` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ XAML-regressions пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ automation hooks.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ unit/regression tests пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ manual, missing-manual error пїЅ repository path resolution.
- `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md` пїЅ `docs/WORKLOG2.md` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ shell behavior.

### Files changed
- `src/Axiom.Ide/Services/Shell/ExternalNavigationService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/App.xaml.cs`
- `src/Axiom.Ide/Axiom.Ide.csproj`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- `dotnet build C:\Axiom\src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-ide-help-manual\`
- `dotnet test C:\Axiom\tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-test-help-manual\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-test-help-manual-obj\ -p:UseAppHost=false`

### Decisions
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE baseline пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ HTML manual пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ help-viewer пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ docs.
- пїЅпїЅпїЅпїЅпїЅ navigation service пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Process.Start` пїЅ VM, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ entry points пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Next step
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDE slice пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ shell/UX polish пїЅпїЅпїЅ bugfix пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Help` baseline.

## Session 202 - 2026-04-20 20:13 - Исследование и договор пакета даты и времени

### Done
- Поднят обязательный project context из `AGENTS.md`, `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md`, `docs/PLANS.md` и `docs/WORKLOG2.md` перед началом нового language-slice.
- Проведено отдельное исследование по исторической поверхности даты и времени в Microsoft BASIC-family:
  - classic GW-BASIC / QuickBASIC: `DATE$`, `TIME$`, `TIMER`, а также statement-формы `DATE$ = ...` и `TIME$ = ...`;
  - более поздний Visual Basic-family surface: `NOW`, `DATEADD`, `DATEDIFF`, `DATEPART`, `DATESERIAL`, `TIMESERIAL`, `DATEVALUE`, `TIMEVALUE`, `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `WEEKDAYNAME`, `ISDATE`.
- Зафиксировано, что Axiom не должен переносить historical API буквально:
  - suffix-имена `DATE$` / `TIME$` не совпадают с текущей моделью идентификаторов языка;
  - statement-формы изменения системных часов хоста не подходят текущему interpreter-first scope.
- В `docs/SPECIFICATION.md` зафиксирован staged contract пакета даты и времени:
  - Этап 1: `DATE()`, `TIME()`, `NOW()`, `TIMER()`;
  - Этап 2: `DATESERIAL`, `TIMESERIAL`, `YEAR`, `MONTH`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`;
  - arithmetic/validation-слой вынесен в отдельный следующий этап.
- В `docs/ARCHITECTURE.md` зафиксирована string-first архитектурная рамка:
  - без нового scalar-типа даты/времени на первом этапе;
  - с каноническими locale-independent форматами;
  - с отдельным runtime helper-слоем точного разбора/форматирования вместо host-dependent parsing shortcuts.
- В `docs/PLANS.md` обновлён следующий практический шаг: после этого research/design slice реализовывать Этап 1 пакета даты и времени.

### Files changed
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Validation
- Автоматические тесты не запускались, потому что в этом сеансе изменялись только проектные документы и контракт будущего language-slice.

### Decisions
- Первый пакет даты и времени в Axiom должен строиться поверх уже существующих scalar-типов и канонических строк, а не через немедленное введение отдельного `DATE` / `TIME` / `DATETIME` type.
- Canonical contract должен быть единым для всего языка и уже существующих file-system built-ins:
  - дата `yyyy-MM-dd`;
  - время `HH:mm:ss`;
  - дата и время `yyyy-MM-dd HH:mm:ss`.
- Locale-dependent parsing по Short Date / regional settings ОС специально не переносится из VBA в Axiom.
- Точный договор `WEEKDAY` оставлен отдельным мини-шагом перед кодом Этапа 2, чтобы не зашить случайную Sunday-first или ISO-first семантику без явного решения.

### Next step
- Реализовать Этап 1 пакета даты и времени: `DATE()`, `TIME()`, `NOW()`, `TIMER()`.
- В том же шаге обновить automated tests, runnable `.axm` samples и user-facing manual.

## Session 203 - 2026-04-20 20:45 - Реализация Stage 1 пакета даты и времени

### Summary
- Реализован первый runtime-пакет даты и времени: DATE(), TIME(), NOW(), TIMER().
- Для deterministic runtime-tests введён host clock contract IRuntimeClock, встроенный в IRuntimeHost.
- User-facing manual расширен отдельной страницей по функциям даты и времени, а manual index обновлён так, чтобы новые функции находились через навигацию и поиск.

### Files changed
- src/Axiom.Runtime/Host/IRuntimeClock.cs
- src/Axiom.Runtime/Host/IRuntimeHost.cs
- src/Axiom.Runtime/Host/NullRuntimeHost.cs
- src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/date/basic_usage.axm
- samples/functions/time/basic_usage.axm
- samples/functions/now/basic_usage.axm
- samples/functions/timer/basic_usage.axm
- docs/manual/date-time-functions.html
- docs/manual/index.html
- docs/manual/reference.html
- docs/manual/arrays-and-builtins.html
- docs/manual/file-system-functions.html
- docs/manual/scripts/manual.js

### Validation
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj - passed.
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-obj\ - passed.
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-obj\ - passed.
- Параллельный запуск нескольких dotnet test в общие .artifacts каталоги дал collision по output path; это было ограничение способа запуска, а не регрессия language slice.

### Decisions
- DATE(), TIME(), NOW() возвращают canonical locale-independent строки: yyyy-MM-dd, HH:mm:ss, yyyy-MM-dd HH:mm:ss.
- TIMER() возвращает DOUBLE в секундах от локальной полуночи и может содержать дробную часть.
- Формат дат файловых built-ins выровнен с тем же canonical datetime contract.

### Next step
- Зафиксировать точный contract для WEEKDAY.
- Реализовать Stage 2 пакета даты и времени: DATESERIAL, TIMESERIAL, YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, WEEKDAY с automated tests, acceptance samples и manual updates.
## Session 204 - 2026-04-20 22:22 - Реализация второго этапа пакета даты и времени

### Summary
- Реализованы встроенные функции DATESERIAL(), TIMESERIAL(), YEAR(), MONTH(), DAY(), HOUR(), MINUTE(), SECOND(), WEEKDAY().
- Для пакета даты и времени добавлен единый вспомогательный слой строгого разбора канонических форматов даты, времени и даты-времени без зависимости от локали хоста.
- Зафиксирован и реализован договор WEEKDAY(value): функция принимает каноническую дату или дату-время и возвращает 1..7, где 1 = понедельник, 7 = воскресенье.

### Files changed
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/dateserial/basic_usage.axm
- samples/functions/timeserial/basic_usage.axm
- samples/functions/year/basic_usage.axm
- samples/functions/month/basic_usage.axm
- samples/functions/day/basic_usage.axm
- samples/functions/hour/basic_usage.axm
- samples/functions/minute/basic_usage.axm
- samples/functions/second/basic_usage.axm
- samples/functions/weekday/basic_usage.axm
- docs/SPECIFICATION.md
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js

### Validation
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage2\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage2-obj\ - passed.
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage2\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage2-obj\ - passed.
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage2\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage2-obj\ - passed.
- После мелкой правки вспомогательного файла и документации тестовые проекты повторно прогнаны из изолированных каталогов и снова прошли успешно.

### Decisions
- DATESERIAL и TIMESERIAL на этом этапе строго проверяют диапазоны и календарную корректность, а не переносят переполненные компоненты в следующий месяц, день или час.
- YEAR, MONTH, DAY, WEEKDAY принимают только каноническую дату yyyy-MM-dd или каноническую дату-время yyyy-MM-dd HH:mm:ss.
- HOUR, MINUTE, SECOND принимают только каноническое время HH:mm:ss или каноническую дату-время yyyy-MM-dd HH:mm:ss.
- WEEKDAY не получает на этом этапе второй параметр настройки первого дня недели.

### Next step
- Перейти к следующему слою пакета даты и времени: DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE.
- Перед кодом зафиксировать точный договор арифметики и нормализации даты и времени, чтобы не смешивать строгий базовый слой со следующими расширениями.
## Session 205 - 2026-04-21 09:02 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ oh-my-codex пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### Done
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ project context пїЅпїЅ AGENTS.md, PROJECT.md, README.md, docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md пїЅ docs/WORKLOG2.md пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Yeachan-Heo/oh-my-codex пїЅпїЅпїЅ workflow/runtime-пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Codex CLI: README, demo, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ skills/agents, hook-пїЅпїЅпїЅпїЅпїЅпїЅ, wiki-пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ AGENTS.md.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ OMX:
  - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ workflow clarify -> plan -> execute -> verify;
  - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ durable-state-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ chat memory;
  - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ wiki/search-first knowledge layer пїЅпїЅпїЅ agent-friendly retrieval;
  - hook/notification пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ long-running agent sessions;
  - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/skills пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ ad hoc пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Files changed
- docs/WORKLOG2.md

### Validation
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ oh-my-codex: README, docs пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ GitHub-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Decisions
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ oh-my-codex пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Windows-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ default-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ macOS/Linux + Codex CLI, пїЅ native Windows/Codex App пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: project-local state, hook-driven lifecycle, skill catalog, role catalog пїЅ search-first wiki.
- пїЅпїЅпїЅ C:\Axiom пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ document-driven пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.

### Next step
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ oh-my-codex, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ wiki/memory пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ workflow-template пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ -> пїЅпїЅпїЅпїЅ -> пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ -> пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Axiom пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ date/time built-ins (DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE).
## Session 206 - 2026-04-21 09:08 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ agent layer пїЅпїЅпїЅпїЅпїЅпїЅ document-driven пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### Done
- пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ OMX-inspired retrieval layer пїЅ docs/agent/ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ source-controlled пїЅпїЅпїЅпїЅпїЅ:
  - docs/agent/README.md;
  - docs/agent/CURRENT_STATE.md;
  - docs/agent/ARCHITECTURE_MAP.md;
  - docs/agent/PLAYBOOKS.md.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ AGENTS.md, PROJECT.md, docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md пїЅ docs/WORKLOG2.md, пїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ re-entry path пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ + пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ + пїЅпїЅпїЅпїЅпїЅ hotspots + playbook-пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### Files changed
- docs/agent/README.md
- docs/agent/CURRENT_STATE.md
- docs/agent/ARCHITECTURE_MAP.md
- docs/agent/PLAYBOOKS.md
- docs/WORKLOG2.md
- docs/PLANS.md

### Validation
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: docs/agent/README.md, CURRENT_STATE.md, ARCHITECTURE_MAP.md, PLAYBOOKS.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Decisions
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ source-controlled пїЅпїЅпїЅпїЅпїЅ docs/agent/, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ .omx/, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- Agent layer пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ retrieval-oriented: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ summaries, пїЅпїЅпїЅпїЅпїЅ пїЅ playbook-пїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ Axiom пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### Next step
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ docs/agent/ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ retrieval-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ date/time built-ins (DATEADD, DATEDIFF, DATEPART, DATEVALUE, TIMEVALUE, ISDATE).
## Session 207 - 2026-04-21 09:34 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ DATEADD(), DATEDIFF(), DATEPART(), DATEVALUE(), TIMEVALUE(), ISDATE().
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `.axm`-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/dateadd/month_end_shift.axm
- samples/functions/datediff/day_boundary.axm
- samples/functions/datepart/day_of_year.axm
- samples/functions/datevalue/from_datetime.axm
- samples/functions/timevalue/from_datetime.axm
- samples/functions/isdate/canonical_datetime.axm
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage3\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage3-obj\ - passed.
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage3\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage3-obj\ - passed.
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage3\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage3-obj\ - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- DATEADD пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ yyyy, q, m, d, h, n, s пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- DATEDIFF пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ LONG пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- DATEPART пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ yyyy, q, m, d, y, w, h, n, s; DATEVALUE, TIMEVALUE пїЅ ISDATE пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: WEEKDAYNAME, ISLEAPYEAR, DAYSINMONTH пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

## Session 208 - 2026-04-21 09:45 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅ `AGENTS.md` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Codex пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `.axm`-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `basic_usage.axm`, `demo.axm`, `example.axm`, `test.axm`.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `docs/PLANS.md` пїЅ `docs/WORKLOG2.md`, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- AGENTS.md
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- docs/manual/date-time-functions.html
- docs/PLANS.md
- docs/WORKLOG2.md
- samples/functions/dateadd/month_end_shift.axm
- samples/functions/datediff/day_boundary.axm
- samples/functions/datepart/day_of_year.axm
- samples/functions/datevalue/from_datetime.axm
- samples/functions/timevalue/from_datetime.axm
- samples/functions/isdate/canonical_datetime.axm

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage3b\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage3b-obj\ - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `basic_usage.axm` пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `AGENTS.md`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Codex; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

## Session 209 - 2026-04-21 10:14 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅ AGENTS.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ BASIC.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ classic BASIC, QuickBASIC пїЅ Visual Basic, пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ Turbo Basic, QB64 пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- AGENTS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅ Axiom пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC, пїЅ пїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC.
## Session 210 - 2026-04-21 10:18 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ BASIC

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ AGENTS.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ BASIC пїЅпїЅ пїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- AGENTS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BASIC, пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
## Session 211 - 2026-04-21 10:28 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ MONTHNAME пїЅ WEEKDAYNAME

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `MONTHNAME()` пїЅ `WEEKDAYNAME()` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `.axm`-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/monthname/calendar_month_name.axm
- samples/functions/weekdayname/weekend_name.axm
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage4\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage4-obj\` - passed.
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage4\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage4-obj\` - passed.
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage4\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage4-obj\` - passed.
- `node --check docs/manual/scripts/manual.js` - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `MONTHNAME` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ: пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `MONTH`.
- `WEEKDAYNAME` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅ `WEEKDAY`: `1 = Monday`, `7 = Sunday`.
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`.
## Session 212 - 2026-04-21 11:00 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `ISLEAPYEAR()`, `DAYSINMONTH()`, `STARTOFMONTH()`, `ENDOFMONTH()`, `STARTOFYEAR()`, `ENDOFYEAR()`.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `.axm`-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/isleapyear/leap_year_true_false.axm
- samples/functions/daysinmonth/february_leap_year.axm
- samples/functions/startofmonth/from_datetime.axm
- samples/functions/endofmonth/leap_february.axm
- samples/functions/startofyear/from_datetime.axm
- samples/functions/endofyear/from_datetime.axm
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage5\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage5-obj\` - passed.
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage5\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage5-obj\` - passed.
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage5\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage5-obj\` - passed.
- `node --check docs/manual/scripts/manual.js` - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `ISLEAPYEAR` пїЅ `DAYSINMONTH` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅ `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom, пїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ BASIC.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: UTC, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ built-in пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
## Session 213 - 2026-04-21 11:48 - UTC-пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ UTC: `UTCDATE()`, `UTCTIME()`, `UTCNOW()`, `UTCOFFSET()`, `TOUTC()`, `FROMUTC()`.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ UTC пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ UTC пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TOUTC` пїЅ `FROMUTC` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/utcdate/current_utc_date.axm
- samples/functions/utctime/current_utc_time.axm
- samples/functions/utcnow/current_utc_now.axm
- samples/functions/utcoffset/current_local_offset.axm
- samples/functions/toutc/roundtrip_to_local.axm
- samples/functions/fromutc/roundtrip_to_utc.axm
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage6\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage6-obj\` - passed.
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage6\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage6-obj\` - passed.
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage6\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage6-obj\` - passed.
- `node --check docs/manual/scripts/manual.js` - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `UTCDATE`, `UTCTIME`, `UTCNOW` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ UTC пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ.
- `UTCOFFSET()` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UTC пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `INTEGER`.
- `TOUTC` пїЅ `FROMUTC` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ `yyyy-MM-dd HH:mm:ss`.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ UTC-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ BASIC пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.## Session 214 - 2026-04-21 12:09 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ UTC-пїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ UTC пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: `TOUTC()` пїЅ `FROMUTC()` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- `UTCOFFSET` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: `UTCOFFSET(<пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ>)`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- src/Axiom.Runtime/Host/IRuntimeClock.cs
- src/Axiom.Runtime/Host/NullRuntimeHost.cs
- src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/utcoffset/specific_local_datetime_offset.axm
- docs/manual/date-time-functions.html
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage7\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage7-obj\` - passed.
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage7\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage7-obj\` - passed.
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage7\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage7-obj\` - passed.
- `node --check docs/manual/scripts/manual.js` - passed.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `UTCOFFSET` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Axiom.
- `UTCOFFSET(<пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ>)` пїЅ `TOUTC(<пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ>)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `TimeZoneInfo` runtime-пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
## Session 215 - 2026-04-21 12:17 - Именованные часовые пояса и явный выбор пояса

### Кратко
- Реализован следующий цельный пакет развития даты и времени: именованные часовые пояса без ввода нового пользовательского типа.
- Пакет добавил перегрузки `DATE(<пояс>)`, `TIME(<пояс>)`, `NOW(<пояс>)`, `UTCOFFSET(<значение>, <пояс>)`, `TOUTC(<значение>, <пояс>)`, `FROMUTC(<значение>, <пояс>)`.
- Для разрешения поясов runtime-хост получил новый договор `TryResolveTimeZone(...)`, чтобы тесты могли подставлять собственные идентификаторы, а не зависеть от реестра поясов текущей машины.
- В одном изменении синхронизированы семантика, выполнение, автоматические тесты, запускаемые `.axm`-примеры, пользовательская справка, спецификация, архитектура, план и журнал.

### Изменённые файлы
- src/Axiom.Runtime/Host/IRuntimeClock.cs
- src/Axiom.Runtime/Host/NullRuntimeHost.cs
- src/Axiom.Execution/Hosting/ExecutionRuntimeHostAdapter.cs
- src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs
- src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/functions/date/named_timezone_current_date.axm
- samples/functions/time/named_timezone_current_time.axm
- samples/functions/now/named_timezone_current_datetime.axm
- samples/functions/utcoffset/named_timezone_specific_offset.axm
- samples/functions/toutc/named_timezone_to_utc.axm
- samples/functions/fromutc/named_timezone_from_utc.axm
- docs/manual/date-time-functions.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage8\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-semantics-datetime-stage8-obj\` - passed.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage8\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-datetime-stage8-obj\` - passed.
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage8\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-execution-datetime-stage8-obj\` - passed.
- `node --check docs\manual\scripts\manual.js` - pending.

### Решения
- Для пользовательского API не вводились отдельные новые имена поясовых функций: проект использует перегрузку уже существующих `DATE`, `TIME`, `NOW`, `UTCOFFSET`, `TOUTC`, `FROMUTC`.
- Псевдонимы `UTC` и `LOCAL` сделаны переносимыми, а остальные идентификаторы поясов оставлены в договоре хоста.
- Одноаргументная форма `UTCOFFSET(<пояс>)` намеренно не вводилась, потому что она конфликтовала бы с уже существующей одноаргументной строковой формой `UTCOFFSET(<дата-время>)`.

### Следующий шаг
- Следующий крупный шаг по пакету даты и времени — доли секунды и длительности.
- Если этот шаг упрётся в границы string-first модели, отдельно зафиксировать минимальное усиление внутреннего представления даты и времени перед кодом.

### Дополнение по проверке
- `node --check docs\manual\scripts\manual.js` - passed.
## Session 216 - 2026-04-21 12:38 - Исправление сборки IDE после пакета часовых поясов

### Кратко
- Исправлена регрессия сборки `Axiom.Ide`, появившаяся после добавления пакета именованных часовых поясов.
- Причиной были неверные именованные аргументы при создании служебных записей `IntervalTokenInfo` и `PartTokenInfo` в вспомогательном слое даты и времени.
- Выполнена точечная правка без изменения пользовательского поведения функций.

### Изменённые файлы
- src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs
- docs/WORKLOG2.md

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj` - passed.
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-build-fix\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-build-fix-obj\` - passed.

### Решения
- Для инициализации `IntervalTokenInfo` и `PartTokenInfo` оставлены позиционные булевы аргументы, чтобы не зависеть от регистра имени параметра конструктора и не ловить повторно `CS1739`.
- Так как это исправление только возвращает проект в собираемое состояние и не меняет договор языка, спецификация, справка, примеры и план развития не пересматривались.

### Следующий шаг
- Следующий содержательный шаг по пакету даты и времени остаётся прежним: при необходимости отдельно брать доли секунды и длительности как новый этап.
## Session 217 - 2026-04-21 13:16 - Универсализация примера восстановления битой кодировки

### Кратко
- Расширен запускаемый пример `samples/smoke/repair_plans_mojibake.axm`, чтобы он умел работать не только с `PLANS.md`, но и с `SPECIFICATION.md` и `ARCHITECTURE.md`.
- Главная логическая доработка: восстановление теперь выполняется не только на уровне целой строки, но и на уровне отдельных подозрительных фрагментов внутри строки.
- Пример дополнительно снабжён подробными русскими комментариями, объясняющими замысел, шаги программы и технику восстановления.

### Изменённые файлы
- samples/smoke/repair_plans_mojibake.axm
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- docs/WORKLOG2.md

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\isolated-runtime-mojibake-sample\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\isolated-runtime-mojibake-sample-obj\ --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture"` - passed.

### Решения
- Сценарий оставлен обратно совместимым: по умолчанию `documentKey = "PLANS"`, то есть прежний рабочий путь не сломан.
- Для `SPECIFICATION` и `ARCHITECTURE` добавлен тот же механизм через выбор документа в начале программы, без создания отдельной второй или третьей копии примера.
- Починка по фрагментам введена потому, что большие документы содержат смешанные строки, где уже исправленный русский текст соседствует с битым; перекодировка всей строки целиком в таких местах была слишком грубой.

### Следующий шаг
- Если потребуется, можно отдельно усилить эвристику распознавания ещё более редких вариантов порчи кодировки, но текущий шаг уже закрывает основной практический случай для `PLANS`, `SPECIFICATION` и `ARCHITECTURE`.


## Session 218 - 2026-04-21 13:45 - Исправление имени параметра в smoke-примере восстановления кодировки

### Кратко
- Исправлена диагностика `MB2016` в `samples/smoke/repair_plans_mojibake.axm`: параметры функций `ResolveSourcePath` и `ResolveWorkPath` переименованы так, чтобы не повторять внешнюю переменную `documentKey`.
- Правило языка о запрете затенения внешних имён не менялось; ошибка была в проверочном примере.
- Тесты запуска этого примера сделаны устойчивыми к ручному выбору документа в `.axm`-файле: перед запуском они явно выставляют `documentKey` для `PLANS`, `SPECIFICATION` или `ARCHITECTURE`.

### Изменённые файлы
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 3 tests.

### Решения
- Параметры переименованы в `selectedDocumentKey`, чтобы сохранить смысл кода и не ослаблять семантическое правило `MB2016`.
- Текущее значение `documentKey` в примере оставлено как есть, чтобы не сбрасывать ручной выбор документа.
- Для добавления записей в `docs/PLANS.md` и `docs/WORKLOG2.md` использовано хвостовое исправление через временный UTF-8 файл, потому что `docs/PLANS.md` содержит старые недопустимые UTF-8 байты и не открывается через `apply_patch`.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.

## Session 219 - 2026-04-21 13:52 - Ручные пути в smoke-примере восстановления кодировки

### Кратко
- `samples/smoke/repair_plans_mojibake.axm` больше не выбирает документ через `documentKey`.
- В начале примера теперь есть две явные строки: `путь_к_файлу_с_битой_кодировкой` и `путь_к_рабочей_копии`.
- Удалены функции `ResolveSourcePath` и `ResolveWorkPath`.
- Тесты запуска примера теперь подставляют именно эти две строки с путями, как это делает пользователь вручную.

### Изменённые файлы
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 3 tests.

### Решения
- Пример оставлен универсальным: теперь в него можно вставить любой путь к исходному файлу и любой путь к рабочей копии.
- Исходный файл по-прежнему не изменяется напрямую: исправление выполняется после `COPYFILE` только в рабочей копии.
- Договор языка и справочник не менялись, потому что это правка проверочного примера, а не изменение поведения Axiom.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 220 - 2026-04-21 13:55 - ASCII-имена путей в smoke-примере восстановления кодировки

### Кратко
- В `samples/smoke/repair_plans_mojibake.axm` русские имена переменных путей заменены на `sourcePath` и `workPath`.
- Пример по-прежнему позволяет вручную вставить любой исходный путь и путь к рабочей копии.
- Тестовая подстановка путей обновлена под новые ASCII-имена.

### Изменённые файлы
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 3 tests.

### Решения
- В пользовательских `.axm`-примерах для путей используем обычные ASCII-идентификаторы, даже если лексер технически допускает Unicode-буквы.
- Договор языка не менялся; это правка качества проверочного примера.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 221 - 2026-04-21 14:04 - Уточнение распознавания битой кодировки в smoke-примере

### Кратко
- В `samples/smoke/repair_plans_mojibake.axm` функция `LooksBroken` больше не считает достаточным совпадением одну пару символов вида `Р` / `С` / `в` плюс байт продолжения.
- Теперь пример проверяет полноценную UTF-8 последовательность, восстановленную из ошибочно прочитанных байтов Windows-1251.
- Это защищает уже нормальный русский UTF-8 текст с сочетаниями вроде `вёрстки`, `четвёртая`, `живёт` от повторной ошибочной перекодировки.
- Добавлен регрессионный тест, который запускает тот же `.axm`-пример на уже исправленном русском тексте и проверяет, что рабочая копия остаётся без изменений.

### Изменённые файлы
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 4 tests.

### Решения
- Исправление оставлено внутри проверочного `.axm`-примера, потому что договор языка, встроенные функции и выполнение `READFILE` / `WRITEFILE` не менялись.
- Сканирование текущего `docs/PLANS.axiom-work.md` показало, что начало файла уже записано нормальными кодовыми точками Unicode (`# План работ`), а не последовательностью `П...`; возможное отображение mojibake в консоли связано с кодовой страницей вывода.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 222 - 2026-04-21 14:08 - Подробные комментарии в smoke-примере восстановления кодировки

### Кратко
- В `samples/smoke/repair_plans_mojibake.axm` добавлены подробные пояснения справа от строк кода.
- Комментариями покрыты ручные пути, копирование в рабочую копию, чтение и запись файла, разбиение строк, подсчёт изменённых строк, обработка фрагментов, распознавание UTF-8 последовательностей и обратное восстановление байтов Windows-1251.
- Поведение примера не менялось.

### Изменённые файлы
- `samples/smoke/repair_plans_mojibake.axm`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 4 tests.

### Решения
- Для учебного smoke-примера допустимы более подробные комментарии, чем для обычного производственного кода, потому что этот файл пользователь открывает и правит вручную в IDE.
- `docs/PLANS.md` не обновлялся, потому что следующий содержательный шаг проекта не изменился.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 223 - 2026-04-21 14:21 - Резервное чтение Windows-1251 для READFILE

### Кратко
- Выяснено, почему `samples/smoke/repair_plans_mojibake.axm` печатал `TRUE`, `0`, `TRUE` на реальном `PLANS.md`: файл содержит недопустимые UTF-8 байты, которые прежний `READFILE` превращал в символы замены `пїЅ`; после такой потери байтов BASIC-пример уже не мог восстановить текст.
- `READFILE` и `READALLLINES` теперь сначала читают файл как строгий UTF-8, а при недопустимых UTF-8 байтах используют Windows-1251.
- `WRITEFILE` и `APPENDFILE` продолжают записывать UTF-8 без BOM.
- В `samples/smoke/repair_plans_mojibake.axm` рабочие переменные вынесены из циклов и объявляются в начале функций.
- Добавлен ручной пример `samples/functions/readfile/windows1251_fallback.axm` и файл-источник Windows-1251 для проверки резервного чтения.

### Изменённые файлы
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/smoke/repair_plans_mojibake.axm`
- `samples/functions/readfile/windows1251_fallback.axm`
- `samples/functions/readfile/windows1251_fallback_source.txt`
- `docs/manual/file-system-functions.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid|Execute_SmokeSampleRepairsMixedUtf8AndWindows1251Bytes|Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForArchitecture|Execute_SmokeSampleRepairsBrokenFragmentsInsideMixedLinesForSpecification" --logger "console;verbosity=normal"` - passed, 6 tests.

### Решения
- Резервное чтение добавлено именно в `READFILE` и `READALLLINES`, потому что эти функции используются для одношаговой обработки целых текстовых файлов.
- Двоичная файловая модель не вводилась: исправление ограничено текстовым чтением и не добавляет `OPEN ... FOR BINARY`.
- Ручной smoke-пример оставлен на прямых путях `sourcePath` и `workPath`; он по-прежнему меняет только рабочую копию.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
### Дополнительная проверка сборки
- `dotnet build Axiom.sln --no-restore` запустился, но завершился ошибкой копирования DLL в проект IDE: работающий процесс `Axiom.Ide (8916)` удерживал `Axiom.Runtime.dll` и `Axiom.Execution.dll`.
- Кодовые проекты до этого этапа были собраны, а целевые runtime-тесты прошли; для полной сборки IDE нужно закрыть запущенное окно Axiom.Ide и повторить build.
## Session 224 - 2026-04-21 14:31 - Исправление инициализации Windows-1251 в IDE

### Кратко
- Исправлено падение `IDE0001` с сообщением `The type initializer for 'Axiom.Runtime.Interpreter' threw an exception` после добавления резервного чтения Windows-1251.
- Причина была в статическом получении `Encoding.GetEncoding(1251)` до регистрации провайдера кодовых страниц в процессе IDE.
- `Interpreter` теперь создаёт кодировку Windows-1251 через отдельный helper, который сначала вызывает `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`.

### Изменённые файлы
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore --filter "Execute_BuiltinFileSystemReadsWritesAndChecksPaths|Execute_BuiltinReadFileFallsBackToWindows1251WhenUtf8IsInvalid|Execute_SmokeSampleRepairsMixedUtf8AndWindows1251Bytes|Execute_SmokeSampleRepairsMixedUtf8Cp1251MojibakeCopy|Execute_SmokeSampleLeavesValidRussianUtf8TextUnchanged" --logger "console;verbosity=normal"` - passed, 5 tests.
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore` - passed, 0 warnings, 0 errors.

### Решения
- Отдельный пакет поведения языка не менялся: это исправление инфраструктурной инициализации уже добавленного резервного чтения Windows-1251.
- Для проверки IDE достаточно пересобрать проект IDE и запустить новое окно, чтобы оно подхватило обновлённый `Axiom.Runtime.dll`.

### Следующий шаг
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 225 - 2026-04-21 14:42 - Проверка невосстановимых строк SPECIFICATION

### Кратко
- Проверены строки около 2914 в `docs/SPECIFICATION.md`, которые после восстановления остались со знаками вопроса.
- В файле на этих местах уже находятся буквальные байты `0x3F` (`?`) и управляющий символ `U+000B` перед `alue`, а не сохранённые байты Windows-1251 или UTF-8 mojibake.
- Такие строки нельзя восстановить алгоритмом перекодировки, потому что исходные русские символы уже заменены на `?` до текущего запуска repair-примера.

### Изменённые файлы
- `docs/WORKLOG2.md`

### Проверка
- Выполнена побайтовая проверка фрагментов `docs/SPECIFICATION.md` около строк 2914-2921 и смещений с `date-based`.

### Следующий шаг
- Для этих конкретных строк возможна только ручная реконструкция текста по смыслу или поиск более старой неповреждённой копии документа.
## Session 226 - 2026-04-21 14:49 - Логическое восстановление date-time разделов SPECIFICATION

### Кратко
- По просьбе пользователя повреждённый участок `docs/SPECIFICATION.md` вокруг строк 2914 и ниже восстановлен логически по `docs/manual/date-time-functions.html`, реализации рантайма и существующим примерам.
- Восстановлены разделы про `DATEADD`, `DATEDIFF`, `DATEPART`, `DATEVALUE`, `TIMEVALUE`, `ISDATE`, `MONTHNAME`, `WEEKDAYNAME`, `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR`.
- Восстановление ограничено подтверждёнными свойствами: канонические форматы, поддержанные токены, сохранение формы `DATEADD`, правила `DATEDIFF`, фиксированные английские имена месяцев и дней недели, диапазоны аргументов и границы месяца/года.

### Изменённые файлы
- `docs/SPECIFICATION.md`
- `docs/WORKLOG2.md`

### Проверка
- Прочитаны `docs/manual/date-time-functions.html`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs`, `src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs`, а также найденные примеры и тестовые ссылки по затронутым функциям.
- Проверено, что в восстановленном диапазоне строк 2895-3056 больше нет последовательностей `???`, управляющего символа `U+000B` и нулевого символа.

### Осталось
- Следующий повреждённый участок в `docs/SPECIFICATION.md` начинается ниже, с раздела про UTC и смещение часового пояса. Он не восстанавливался в этом проходе.
## Session 227 - 2026-04-21 14:54 - Логическое восстановление UTC-разделов SPECIFICATION

### Кратко
- Восстановлены повреждённые разделы `docs/SPECIFICATION.md` про UTC-пакет и расчёт локального смещения по конкретной дате-времени.
- Текст восстановлен по `docs/manual/date-time-functions.html`, реализации `Interpreter.Builtins.DateTime.cs` и `Interpreter.Builtins.DateTime.Helpers.cs`.
- Подтверждены формы `UTCDATE`, `UTCTIME`, `UTCNOW`, `UTCOFFSET`, `TOUTC`, `FROMUTC`, строгий формат `yyyy-MM-dd HH:mm:ss` для преобразований и диагностика для пропущенных или повторённых локальных моментов.
- Исправлена запись символа замены в разделе про резервное чтение Windows-1251: вместо повреждённого вида используется обозначение `U+FFFD`.

### Изменённые файлы
- `docs/SPECIFICATION.md`
- `docs/WORKLOG2.md`

### Проверка
- Проверено, что `docs/SPECIFICATION.md` больше не содержит явных следов повреждения: `???`, `U+000B`, `U+0000`, `пїЅ`, сам символ замены Unicode и типичные фрагменты битой перекодировки вида `а`, `е`, `и`, `о`, `П`, `С`, `Н`, `Д`. Обозначение `U+FFFD` осталось только как текстовое имя символа замены в разделе про резервное чтение Windows-1251.
- Автоматические тесты не запускались, потому что менялся только текст спецификации.

### Следующий шаг
- Если пользователь найдёт повреждённый фрагмент в другом документе, восстановить его тем же способом: сначала проверить байты, затем сверить смысл по справке, коду и примерам.

## Session 228 - 2026-04-21 15:26 - Ҹмная тема IDE и русские пункты меню

### Кратко
- Добавлена переключаемая тёмная тема IDE через `Вид -> Тема -> Ҹмная`; светлая тема оставлена доступной через тот же пункт меню.
- Выбранная тема сохраняется в состоянии оболочки вместе с недавними файлами, видимостью панелей и состоянием окна.
- Основное меню, контекстное меню редактора, заголовки нижней панели и заголовки таблицы диагностик переведены на русский язык.
- Добавлена отдельная тёмная палитра подсветки `.axm`; светлая подсветка сохранена отдельно.
- Словарь подсветки синхронизирован с уже существующими встроенными функциями даты и времени, чтобы IDE-тесты словаря не отставали от языка.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/App.xaml`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Services/Shell/IdeThemeResourceService.cs`
- `src/Axiom.Ide/Services/Persistence/IdeTheme.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Editor/Highlighting/AxiomHighlighting.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `src/Axiom.Ide/Axiom.Ide.csproj`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore` - прошло, 169 тестов.
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore` - прошло, 0 предупреждений, 0 ошибок.

### Осталось
- Полная локализация всех сообщений, диалогов и строки состояния в отдельные ресурсные файлы не вводилась; сейчас переведены именно пункты меню и видимые заголовки оболочки.
- Следующий содержательный шаг проекта остаётся прежним: отдельно добрать доли секунды и длительности в пакете даты и времени.
## Session 229 - 2026-04-21 16:14 - Доводка тёмной темы и переключение языка IDE

### Кратко
- Доведена тёмная тема оболочки IDE: тёмные рамки и служебные границы теперь заданы ресурсами самой оболочки, а не светлыми системными значениями.
- Добавлено явное переключение языка IDE через `Вид -> Язык -> Русский / English` с сохранением выбора между запусками.
- На ресурсные строки переведены главное меню, контекстные меню, заголовки нижней панели, заголовки диагностик и диалог `Найти и заменить`.
- Для `FindReplaceDialog` и `TextInputDialog` подключены общие тёмные стили оболочки, чтобы текст и рамки были читаемыми в тёмной теме.
- В диспетчере файлов переработан шаблон кнопки раскрытия узлов, чтобы знак `+/-` оставался видимым и в светлой, и в тёмной теме.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide/Services/Prompts/TextInputDialog.xaml`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `src/Axiom.Ide/Services/Shell/IdeLanguageResourceService.cs`
- `src/Axiom.Ide/Services/Persistence/IdeLanguage.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка изменённых XAML-словарей и окон прошла успешно через `[xml]` для:
  - `src/Axiom.Ide/MainWindow.xaml`
  - `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
  - `src/Axiom.Ide/Services/Prompts/TextInputDialog.xaml`
  - `src/Axiom.Ide/Resources/ShellStyles.xaml`
  - `src/Axiom.Ide/Resources/ShellColors.xaml`
  - `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
  - `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
  - `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `dotnet build` и `dotnet test` в этой среде не дали надёжного результата: локальный `.NET SDK 10.0.202` падает на шагах `Restore` / `_GetProjectReferenceTargetFrameworkProperties` с кодом возврата `1`, но без диагностик компиляции проекта. Логи сохранены в `.artifacts/build-ide.log` и `.artifacts/restore-ide.log`.

### Осталось
- Если следующий проход снова будет по оболочке IDE, добрать полную локализацию служебных сообщений и уйти от системных `MessageBox`, которые пока не подчиняются теме и языковым ресурсам оболочки.
## Session 230 - 2026-04-21 17:22 - Меню тёмной темы и путь в строке состояния

### Кратко
- Для главного меню, выпадающих меню и контекстных меню добавлены собственные шаблоны `MenuItem` и `ContextMenu`, чтобы убрать светлые системные рамки и светлые состояния наведения в тёмной теме.
- Поле пути документа в строке состояния получило двойной щелчок для `SelectAll()` и собственное контекстное меню для копирования пути.
- После визуальной проверки отдельно скорректированы горизонтальные разделители меню и цвет выделения пути в строке состояния: разделители теперь идут на всю ширину, а выделенный путь снова читается на тёмной теме.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `MainWindow.xaml`, `ShellStyles.xaml`, `ShellColors.xaml`, `ShellColors.Dark.xaml` прошла успешно через `[xml]`.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false` сначала не прошёл, потому что запущенный `Axiom.Ide.exe` блокировал `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Axiom.Ide.exe`.
- Повторная проверка прошла через отдельные каталоги сборки:
  - `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`

### Осталось
- Если следующий проход снова будет по IDE, добрать полную локализацию служебных сообщений и уйти от оставшихся системных `MessageBox`, которые пока не подчиняются теме и языковым ресурсам оболочки.

### Следующий шаг
- Продолжать по текущему плану IDE: добрать локализацию служебных сообщений и перевести оставшиеся стандартные диалоги на ресурсы и стили оболочки.

## Session 231 - 2026-04-21 17:36 - Доводка разделителей меню и выделения пути в статус-баре

### Кратко
- Исправлен оставшийся визуальный дефект разделителей в выпадающих и контекстных меню тёмной темы: разделитель теперь компенсирует внутренний `Padding` popup-контейнера и должен визуально доходить до полной ширины меню.
- Исправлена читаемость выделения пути активного документа в строке состояния: стиль поля пути теперь локально задаёт кисти для активного и неактивного выделения текста, а не полагается только на общие системные кисти темы.
- Старый проект по пути `E:\basic\src\Axiom.Ide` просмотрен как ориентир; прямого шаблона для исправления не нашлось, потому что там ещё не было собственных шаблонов меню.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml`, `src/Axiom.Ide/MainWindow.xaml` и `src/Axiom.Ide/Resources/ShellColors.Dark.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Разделители меню исправлены минимальной правкой стиля `Separator`, без перестройки шаблонов popup и без влияния на разделители строки состояния.
- Для читаемости выделения пути в статус-баре локально переопределены `SystemColors.HighlightBrushKey`, `SystemColors.HighlightTextBrushKey`, `SystemColors.InactiveSelectionHighlightBrushKey` и `SystemColors.InactiveSelectionHighlightTextBrushKey` внутри `StatusBarPathTextBoxStyle`.

### Следующий шаг
- Если продолжать именно оболочку IDE, следующий шаг остаётся прежним: добрать полную локализацию служебных сообщений и заменить оставшиеся системные `MessageBox`/стандартные окна на диалоги оболочки.

## Session 232 - 2026-04-21 17:49 - Возврат классической колонки значков и разделителей меню

### Кратко
- Исправлена моя прежняя ошибка интерпретации старого IDE: в текущей оболочке восстановлена классическая геометрия меню с отдельной колонкой пиктограмм, вертикальной линией после неё и горизонтальными разделителями, начинающимися уже справа от этой линии.
- Перестроены не только выпадающие меню верхней панели, но и вложенные подменю и контекстные меню, чтобы схема была единой во всей оболочке.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Левая колонка значков и вертикальная линия заданы прямо в шаблонах popup-контейнеров `MainMenuItemStyle`, `DropDownMenuItemStyle` и `ShellContextMenuStyle`.
- Стиль `Separator` теперь согласован с этой геометрией и рисует горизонтальный разделитель только в текстовой части меню.

### Следующий шаг
- Если работа по IDE продолжается, следующий шаг - визуально добрать оставшиеся мелкие несоответствия оболочки после перестройки меню и только потом возвращаться к локализации служебных сообщений.

## Session 233 - 2026-04-21 17:58 - Уточнение геометрии меню под старый вид IDE

### Кратко
- Убрана моя предыдущая самодельная схема с дополнительным внутренним отступом popup-контейнера меню.
- Геометрия всплывающих и контекстных меню приближена к старому IDE: фиксированная левая колонка значков, вертикальный разделитель на её границе и горизонтальные разделители без правого зазора.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В popup-меню убран общий внутренний `Margin` у списка пунктов.
- Линия разделителя теперь согласована с левой колонкой значков и тянется до правой рамки popup.

### Следующий шаг
- Если визуально это всё ещё не совпадёт со старым IDE, следующий шаг - переход на полный стандартный шаблон `MenuItem` WPF с заменой только цветовой палитры.

## Session 234 - 2026-04-21 19:01 - Перенос вертикального разделителя в шаблон пункта меню

### Кратко
- Исправлена ещё одна неверная попытка повторить старый вид меню: вертикальный разделитель больше не рисуется в фоне popup-контейнера.
- Разделитель перенесён в шаблон самого пункта меню, что ближе к поведению классического меню WPF и старого IDE.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Popup-контейнеры меню упрощены до рамки и фона.
- Вертикальная разделительная линия теперь задаётся колонкой `Grid.Column="1"` внутри `DropDownMenuItemStyle`.
- Горизонтальные разделители меню продолжают выравниваться по границе текстовой части меню.

### Следующий шаг
- Визуально сверить новый вариант со старым IDE; если расхождение останется, следующий шаг - заменить текущую упрощённую реализацию на полный стандартный шаблон `MenuItem` WPF.

## Session 235 - 2026-04-21 19:13 - Возврат активных стилей меню к стандартному WPF

### Кратко
- Сверка с `E:\basic\src\Axiom.Ide` подтвердила, что старый IDE не использовал собственный шаблон `MenuItem` для реального вида меню.
- Активные стили меню и контекстного меню в текущем проекте возвращены к стандартной WPF-модели: остались только шрифты, отступы, цвета и рамки, без кастомного `ControlTemplate` в рабочих стилях.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Для активных стилей `MainMenuItemStyle` и `DropDownMenuItemStyle` убраны привязки к кастомному шаблону меню; они снова задают только шрифты, отступы и цвета.
- `ShellContextMenuStyle` тоже оставлен без собственного шаблона контейнера.
- Стиль `Separator` возвращён к нейтральному виду без ручной геометрии.

### Следующий шаг
- Визуально сверить меню после возврата к стандартному WPF-рендерингу; если останутся расхождения, искать их уже в кистях и системных theme-ресурсах, а не в самих шаблонах меню.

## Session 236 - 2026-04-21 19:17 - Удаление стрелок прокрутки из меню IDE

### Кратко
- Выяснено, что еле заметные стрелки сверху и снизу меню были кнопками прокрутки стандартного `MenuScrollViewer` WPF, которые появились из-за моего переопределения ресурсов меню.
- Переопределение прокрутчика удалено, чтобы меню снова выглядело как в старом IDE без лишних стрелок.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Удалены `MenuScrollButton`, ресурс `MenuScrollViewer` и геометрии стрелок прокрутки меню.
- В тестах добавлена проверка, что этих ресурсов больше нет в `ShellStyles.xaml`.

### Следующий шаг
- Визуально проверить меню после удаления стрелок и только затем добирать оставшиеся реальные отличия от старого IDE.

## Session 237 - 2026-04-21 19:58 - Доводка тёмной темы для меню верхней панели

### Кратко
- По скриншотам выявлен последний крупный светлый артефакт тёмной темы: выпадающие меню верхней панели всё ещё брали стандартный светлый шаблон WPF.
- Активные стили `MainMenuItemStyle` и `DropDownMenuItemStyle` переведены на `ShellMenuItemStyle`, чтобы верхние меню, подменю и их состояния полностью подчинялись тёмной палитре оболочки.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Активные стили меню верхней панели больше не опираются на стандартный светлый рендеринг WPF.
- Для проверки зафиксировано, что `MainMenuItemStyle` и `DropDownMenuItemStyle` основаны на `ShellMenuItemStyle`.

### Следующий шаг
- Визуально проверить тёмную тему после этой правки и добрать только оставшиеся отдельные светлые артефакты, если они ещё проявятся.

## Session 238 - 2026-04-21 20:31 - Прямые ресурсы темы в шаблонах меню

### Кратко
- По повторному скриншоту стало видно, что светлое оформление меню сохранялось даже после включения тёмной шаблонной ветки.
- Исправлена вероятная причина: шаблоны меню больше не используют промежуточные кисти, созданные внутри `ShellStyles.xaml`; вместо этого они получают фон, текст, рамки и отключённые состояния напрямую через `DynamicResource` из активной тёмной палитры.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Удалены промежуточные кисти меню `Menu.Static.*`, `MenuItem.*` как активный источник цветов в шаблонах.
- Шаблоны используют прямые `DynamicResource`-ссылки к `MenuBackgroundBrush`, `BorderBrush`, `TextPrimaryBrush`, `MenuHighlightBrush`, `MenuHighlightBorderBrush` и `MenuDisabledTextBrush`.

### Следующий шаг
- Визуально проверить тёмную тему после этой правки и добрать только оставшиеся отдельные светлые артефакты, если они ещё проявятся.

## Session 239 - 2026-04-21 21:10 - Исправление ложного dirty-state и добивка строки состояния

### Кратко
- Убран источник ложной звёздочки `*` у только что открытого документа: начальная загрузка текста в AvalonEdit больше не должна считаться пользовательским редактированием.
- Для строки состояния и полос прокрутки добиты недостающие ресурсы тёмной и светлой темы, чтобы выделение пути и прокрутка не оставались со светлой системной раскраской.

### Изменённые файлы
- `src/Axiom.Ide/Editor/AvalonEditTextBinding.cs`
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/AvalonEditTextBindingTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml`, `src/Axiom.Ide/Resources/ShellColors.xaml` и `src/Axiom.Ide/Resources/ShellColors.Dark.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В `AvalonEditTextBinding.OnTextChanged(...)` синхронизация текста из модели в редактор обёрнута в локальный флаг `IsUpdating`, чтобы обратный `TextChanged` не переводил документ в dirty-state во время первоначальной загрузки.
- Добавлен STA-тест, который фиксирует контракт: предзагруженный документ после привязки к AvalonEdit остаётся чистым.
- Для строки состояния зафиксировано белое выделение текста пути и добавлены отдельные ресурсы прокрутки для обеих тем.

### Следующий шаг
- Вручную перепроверить в IDE, что ложная `*` больше не появляется при простом открытии файла, а полосы прокрутки и выделение пути в строке состояния действительно выглядят корректно в тёмной теме.

## Session 240 - 2026-04-21 21:21 - Удаление автоформатирования при открытии вкладки

### Кратко
- Подтверждено, что оставшаяся ложная звёздочка `*` возникала не из биндинга AvalonEdit, а из `OnActiveDocumentChanged()`: при простом открытии или переключении вкладки IDE автоматически форматировала весь `.axm`-документ и тем самым делала его dirty.
- Автоформатирование на активации вкладки убрано; форматирование остаётся только в явных сценариях смены стиля и дальнейших пользовательских действий.
- Для поля пути в строке состояния добавлен явный белый `Foreground` на время фокуса, чтобы текст был читаем при выделении независимо от поведения системных кистей Windows.

### Изменённые файлы
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Из `OnActiveDocumentChanged()` удалён вызов `ApplyFormattingStyleToActiveDocument()`.
- Тест на автоматическое переформатирование при активации вкладки заменён на обратный контракт: простая активация вкладки не должна менять текст и dirty-state документа.
- В `StatusBarPathTextBoxStyle` добавлен trigger по `IsKeyboardFocusWithin=True`, который принудительно ставит белый цвет текста и каретки.

### Следующий шаг
- Перепроверить в реальной IDE два пользовательских сценария: отсутствие `*` после открытия существующего `.axm` и читаемость выделенного пути в строке состояния.

## Session 241 - 2026-04-21 21:32 - Жёсткая палитра активного выделения пути и возврат светлых скроллов

### Кратко
- Для активного выделения пути в строке состояния убран плавающий вариант через системные цвета: теперь стиль задаёт белый фон выделения и голубой цвет выделенного текста напрямую.
- Для полос прокрутки тёмной темы отменена отдельная тёмная палитра; цвета возвращены к тем же светлым значениям, что и в обычной теме оболочки.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` и `src/Axiom.Ide/Resources/ShellColors.Dark.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В `StatusBarPathTextBoxStyle` для `SystemColors.HighlightBrushKey` и `SystemColors.InactiveSelectionHighlightBrushKey` задан белый фон выделения.
- Для `SystemColors.HighlightTextBrushKey` и `SystemColors.InactiveSelectionHighlightTextBrushKey` задан голубой цвет `StatusBarBackgroundColor`.
- В `ShellColors.Dark.xaml` значения `ScrollBar*Color` приведены к той же светлой палитре, что используется в обычной теме.

### Следующий шаг
- Вручную проверить в IDE, что активное выделение пути теперь реально читается, а вид полос прокрутки после возврата светлой палитры устраивает.

## Session 242 - 2026-04-21 21:39 - Возврат скроллов к штатному WPF-поведению

### Кратко
- По референсу `E:\basic\src\Axiom.Ide` подтверждено, что в старом IDE для полос прокрутки не было кастомного шаблона `ScrollBar`.
- Самодельный шаблон `ScrollBar`, `Thumb` и `RepeatButton` удалён из текущего IDE, потому что именно он сломал верхнюю/нижнюю кнопки, дорожку и перетаскивание бегунка.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Из `ShellStyles.xaml` полностью удалены `ScrollBarThumbStyle`, `ScrollBarLineButtonStyle` и глобальный стиль `TargetType="ScrollBar"`.
- В тестах зафиксировано, что кастомного шаблона `ScrollBar` в оболочке больше нет.

### Следующий шаг
- Вручную проверить в IDE обе темы и убедиться, что полосы прокрутки снова работают как в референсной версии.

## Session 243 - 2026-04-21 21:44 - Развод палитр светлой и тёмной тем для Diagnostics

### Кратко
- Найден источник регресса читаемости в светлой теме: общие `SystemColors.HighlightTextBrushKey` и `SystemColors.InactiveSelectionHighlightTextBrushKey` были перекрашены в белый цвет.
- Светлая тема возвращена к читаемому варианту, а для `Diagnostics` введены отдельные тематические кисти, чтобы тёмные правки больше не ломали белую тему.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellColors.xaml`
- `src/Axiom.Ide/Resources/ShellColors.Dark.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellColors.xaml`, `src/Axiom.Ide/Resources/ShellColors.Dark.xaml` и `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В `ShellColors.xaml` для светлой темы восстановлен читаемый цвет текста в системном выделении: `HighlightTextBrushKey` и `InactiveSelectionHighlightTextBrushKey` снова используют тёмный цвет.
- В `ShellColors.xaml` и `ShellColors.Dark.xaml` добавлены отдельные кисти `DiagnosticsSelectionBrush` и `DiagnosticsSelectionForegroundBrush`.
- В `DiagnosticsGridStyle` панель диагностики переведена на явные тематические кисти для `DataGridRow` и `DataGridCell`, чтобы выделение больше не зависело от случайных глобальных замен `SystemColors`.

### Следующий шаг
- Вручную проверить в IDE, что в светлой теме строка в `Diagnostics` теперь читается нормально, а в тёмной теме выделение остаётся контрастным.


## Session 244 - 2026-04-21 21:48 - Стабилизация цвета выбранных вкладок

### Кратко
- Подтверждён визуальный дефект вкладок: у выбранной вкладки состояние `IsMouseOver` перетирало состояние `IsSelected`, если курсор попадал в содержимое активной вкладки.
- Вкладки документов и нижней панели переведены на стабильное поведение: выбранная вкладка больше не меняет фон из-за движения мыши внутри её содержимого.

### Изменённые файлы
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В `DocumentTabItemStyle` состояние наведения ограничено условием `IsSelected=False` через `MultiTrigger`.
- За счёт этого фон выбранной вкладки теперь всегда берётся из `SelectedTabBrush`, а `TabHoverBrush` применяется только к невыбранным вкладкам.

### Следующий шаг
- Вручную проверить в IDE обе темы и убедиться, что вкладки документов и нижней панели больше не «плавают» по цвету при перемещении курсора между заголовком и содержимым.

## Session 245 - 2026-04-21 21:58 - Отказ от TextBox в строке состояния

### Кратко
- Подтверждено, что для строки пути в статус-баре нужен не механизм выделения текста, а надёжное копирование полного пути.
- Поле пути переведено с `TextBox` на обычный `TextBlock`, а копирование реализовано по двойному клику и через контекстное меню.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- XML-проверка `src/Axiom.Ide/MainWindow.xaml` и `src/Axiom.Ide/Resources/ShellStyles.xaml` через `[xml]` прошла успешно.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- В строке состояния `StatusBar.PathText` больше не отображается через `TextBox`; вместо этого используется `TextBlock` со стилем `StatusBarPathTextBlockStyle`.
- Двойной клик по пути теперь сразу копирует полный путь в буфер обмена.
- Контекстное меню у пути упрощено до явного действия `Копировать`, которое вызывает прямое копирование текста в буфер обмена без зависимости от выделения и команд `ApplicationCommands.Copy`.

### Следующий шаг
- Вручную проверить в IDE копирование пути по двойному клику и через контекстное меню в обеих темах.
## Session 246 - 2026-04-21 22:08 - ??????????? ??????????? ? ??????? ?????? ????
### ??????
- ????? ????? ?????????? ??????? ? ?????? ??????? ????: ??? ??? ??????? ????????? ?????, ??????????? ????? ??????? ? ?????? ??????.
- ?????? Diagnostics ???????? ??????? ??????????? ????????? ?????? ??????????? ? ????? ??????.
### ?????????? ?????
- src/Axiom.Ide/MainWindow.xaml
- src/Axiom.Ide/MainWindow.xaml.cs
- src/Axiom.Ide/Resources/ShellColors.xaml
- src/Axiom.Ide/Resources/ShellColors.Dark.xaml
- src/Axiom.Ide/Resources/ShellStyles.xaml
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### ????????
- XML-???????? src/Axiom.Ide/MainWindow.xaml, src/Axiom.Ide/Resources/ShellStyles.xaml, src/Axiom.Ide/Resources/ShellColors.xaml ? src/Axiom.Ide/Resources/ShellColors.Dark.xaml ????? [xml] ?????? ???????.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### ???????
- ????? MenuItemGlyphStyle ?????? ?? ?????????? ????? TextPrimaryBrush; ?????? ????? ???? ? ??? ?????? ?????? ????????? ????? MenuIconBrush, ???????? ???????? ??? ??????? ? ?????? ???.
- DiagnosticsDataGrid ??????? ?????????? ???????? ????? ? ??????????? ??????????? ???? ??????????.
- ? ????? ?????? ?????????? ????????? ?????? ???????????: Stage Code Line:Column Message, ? ??? ???????????????? ????????? ??? ????????? - Stage Code Message.
### ????????? ???
- ??????? ????????? ? IDE ??? ????: ??????????? ???? ?????????? ?????? ?? ???? ???????? ?????????, ? ??????????? ?????? ??????????? ???????? ? ?? ???????? ?????, ? ????? ??????????? ????.## Session 247 - 2026-04-21 22:16 - ?????? ??????????? ???? ? ?????? ???? ????????????
### ??????
- ??????????? ???? ???????? ?????????? ?? ??????????? ??????, ????? ?????? ??????? ????????? ??? ? ???? ?????? ? ?????? ????.
- ?????????????? ??????????? ???? ??????? ????? ?????? ? ????????? ? ???? ?? ?????, ??? ? ???????????? ?????????????? ?????.
### ?????????? ?????
- src/Axiom.Ide/Resources/ShellStyles.xaml
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### ????????
- XML-???????? src/Axiom.Ide/Resources/ShellStyles.xaml ????? [xml] ?????? ???????.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### ???????
- ShellContextMenuStyle ?????? ?????? ?? ?????? ?????, ?? ? ??????????? ControlTemplate, ????? ??????????? ???? ?? ??????????? ??????????? ??????? ?????? WPF.
- ?????????? ????? Separator ??????? ??????????? ?????? ? ?????? ?? BorderBrush ? ? ??? ?? ????-????????, ??? ? ??????, ??????? ?????????????? ??????????? ???? ????????? ? ????? ?? ???? ? ???????????? ?????.
### ????????? ???
- ??????? ????????? ? IDE ?????? ???? ?? ???? ????: ????????, ?????? ????????? ? ?????? ???????????; ???????????, ??? ????? ???????? ???? ?????? ????? ? ??? ??????????? ?????? ?? ???????? ??????? ??????.## Session 248 - 2026-04-21 22:28 - ????? ?????? popup-????
### ??????
- ???? ???????? ?????????? ?? ????? ?????? popup-?????????? ?????? ??????????? ?????????? ?????? XAML.
- ???????????? ????? gutter-???? ? ?????????????? ??????????? ???? ?????????????? ????? ????? ???????.
### ?????????? ?????
- src/Axiom.Ide/Resources/ShellStyles.xaml
- src/Axiom.Ide/Resources/ShellColors.xaml
- src/Axiom.Ide/Resources/ShellColors.Dark.xaml
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- tests/Axiom.Ide.Tests/ShellResourceTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### ????????
- XML-???????? src/Axiom.Ide/Resources/ShellStyles.xaml, src/Axiom.Ide/Resources/ShellColors.xaml ? src/Axiom.Ide/Resources/ShellColors.Dark.xaml ????? [xml] ?????? ???????.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### ???????
- ? ShellStyles.xaml ???????? ?????? ?????? ShellPopupMenuHostTemplate, ????? ??????? ?????? ???????? popup-????????? ??????? ????, ??????? ? ??????????? ????.
- ???????? ?????? ?????? ??????? MenuGutterSeparatorMargin ??? ????? gutter-???? ? ?????????????? ????????????.
- ???? ????? ???? ??????? ? ????????? ???????????? ?????? MenuSeparatorBrush, ????? ??? ????? ???? ??????????? ???????? ?? ????? ?????.
### ????????? ???
- ??????? ????????? ?????????? ?????????? ?????????? ? ??????????? ???? ? ????? ????? ? ??? ????????????? ?????? ?????????? ??????? MenuSeparatorBrush, ?? ????? ??? ????????? ????????? ???????.## Session 249 - 2026-04-21 22:34 - ??????? gutter-????? ? ?????????? ????
### ??????
- ?????????? ????????? ????? ???????? ???? ?? ????? popup-??????: ?????????? ???? ????? ?????????? ????? ???????????? ?????.
- ??????? ???? ?? ? ???????, ? ? ??????? ?????: ????? ?????????? ??????? OpaqueRect ?????? ??????????????? ??????????? popup-????.
### ?????????? ?????
- src/Axiom.Ide/Resources/ShellStyles.xaml
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### ????????
- XML-???????? src/Axiom.Ide/Resources/ShellStyles.xaml ????? [xml] ?????? ???????.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### ???????
- ? ShellPopupMenuHostTemplate ? ????? ???????????? ????? ???? ?????? Panel.ZIndex, ????? ??? ??????????? ?????? ??????? ??????? ???????????.
- ????????? ?????? ??????? ?? ????????; ????????? ?????? ??????? ?????.
- ???? ???????????? ????? ? ???? ?????????????? ??????????? ??-???????? ???????? ?? ???? ?????? MenuSeparatorBrush.
### ????????? ???
- ??????? ????????? ? IDE, ??? ?????? ? ??????????, ? ??????????? ???? ???????????? ?????????? ?????????? ????? ????? ? ??? ?????????????? ?????????? ???????? ??? ?? ?????.
## Session 250 - 2026-04-21 22:50 - мягкая подсветка текущей строки редактора
### Что сделал
- Добавил отдельные ресурсы EditorCurrentLineHighlight* и EditorCurrentLineBorder* для светлой и тёмной темы оболочки IDE.
- Переделал src/Axiom.Ide/MainWindow.xaml.cs: обычная подсветка текущей строки теперь всегда включена, а диагностическая только временно подменяет её ресурсы при переходе к ошибке.
- Обновил проверки IDE на наличие новых ресурсов темы и на использование нового метода ApplyCurrentLineHighlightStyle.
### Изменённые файлы
- src/Axiom.Ide/Resources/ShellColors.xaml
- src/Axiom.Ide/Resources/ShellColors.Dark.xaml
- src/Axiom.Ide/MainWindow.xaml.cs
- tests/Axiom.Ide.Tests/ShellResourceTests.cs
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### Проверка
- XML-проверка src/Axiom.Ide/Resources/ShellColors.xaml и src/Axiom.Ide/Resources/ShellColors.Dark.xaml - passed.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### Решения
- Обычная строка редактора и диагностическая строка теперь живут на разных наборах кистей и перьев, чтобы их можно было настраивать независимо для обеих тем.
- Диагностическая подсветка остаётся более заметной, а обычная текущая строка делается мягкой и фоновой, как в редакторах с постоянным выделением строки курсора.
### Следующий шаг
- Вручную проверить в IDE визуальный баланс обычной подсветки строки и диагностической подсветки в светлой и тёмной теме; при необходимости добрать оттенки.
## Session 251 - 2026-04-21 22:56 - убрана стартовая подсветка строки без фокуса
### Что сделал
- Скорректировал логику видимости обычной подсветки текущей строки в src/Axiom.Ide/MainWindow.xaml.cs.
- Теперь мягкая подсветка строки включается только когда редактор реально получил клавиатурный фокус; при потере фокуса она скрывается.
- Диагностическая подсветка строки сохранена отдельным исключением и продолжает работать при переходе к ошибке.
- Обновил текстовую проверку IDE-теста под новую логику видимости.
### Изменённые файлы
- src/Axiom.Ide/MainWindow.xaml.cs
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### Проверка
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### Решения
- Обычная подсветка текущей строки привязана к фокусу редактора, а не к самому факту открытия вкладки.
- Диагностическая подсветка остаётся самостоятельным режимом, чтобы строка ошибки по-прежнему была заметна.
### Следующий шаг
- Вручную проверить в IDE баланс трёх состояний: без фокуса, с обычным фокусом в редакторе и при переходе к ошибке из панели диагностики.
## Session 252 - 2026-04-21 23:05 - пиктограммы в файловом контекстном меню
### Что сделал
- Проверил сборку файлового контекстного меню в src/Axiom.Ide/MainWindow.xaml: оно уже использует общий стиль ShellContextMenuStyle.
- Добавил тематические MenuItem.Icon для пунктов файлового контекстного меню: открыть/перейти, открыть внешним приложением, показать скрытые файлы, создать файл, создать каталог, вырезать, копировать, вставить, переименовать, удалить и свойства.
- Для первого пункта добавил переключение пиктограммы по типу выбранного узла: для файла остаётся иконка открытия, для каталога показывается иконка папки.
- Обновил текстовые проверки MainWindow.xaml в IDE-тестах.
### Изменённые файлы
- src/Axiom.Ide/MainWindow.xaml
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### Проверка
- XML-проверка src/Axiom.Ide/MainWindow.xaml - passed.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### Решения
- Отдельный новый шаблон для файлового меню не нужен: меню уже сидит на глобальном стиле, не хватало только пиктограмм у самих пунктов.
- Иконка первого пункта файлового меню зависит от выбранного узла, чтобы пункт выглядел уместно и для файла, и для каталога.
### Следующий шаг
- Вручную проверить файловое контекстное меню в обеих темах и убедиться, что визуально оно теперь совпадает с остальными меню оболочки.
## Session 253 - 2026-04-21 23:07 - исправлена XAML-ошибка после добавления иконок файлового меню
### Что сделал
- Убрал дублирование свойства Style у TextBlock в иконке первого пункта файлового контекстного меню.
- Причина ошибки была в том, что для одного TextBlock одновременно были заданы и атрибут Style, и вложенный блок <TextBlock.Style>.
### Изменённые файлы
- src/Axiom.Ide/MainWindow.xaml
- docs/WORKLOG2.md
### Проверка
- XML-проверка src/Axiom.Ide/MainWindow.xaml - passed.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
- dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore - passed.
### Решения
- Для условной иконки первого пункта файлового меню нужно оставлять только вложенный <TextBlock.Style> с BasedOn, без второго присваивания Style через атрибут.
### Следующий шаг
- Продолжить ручную проверку файлового контекстного меню в обеих темах уже на рабочей сборке.
## Session 254 - 2026-04-21 23:18 - встроенное переименование в файловом дереве
### Что сделал
- Убрал диалоговое окно переименования для файлового дерева IDE.
- Добавил у `FileTreeNode` состояние встроенного редактирования имени: флаг режима переименования и текст редактирования.
- Перевёл команду `RenameFilesPaneNodeCommand` на вход в режим встроенного переименования прямо в узле дерева.
- Добавил подтверждение переименования по `Enter` и по потере фокуса, а отмену — по `Escape`.
- Обновил шаблон `TreeView`, чтобы имя узла переключалось между обычным `TextBlock` и встроенным `TextBox`.
- Добавил выбор основной части имени файла без расширения при входе в режим переименования.
- Обновил IDE-тесты под новый сценарий встроенного переименования.
### Изменённые файлы
- src/Axiom.Ide/Files/FilesPaneViewModel.cs
- src/Axiom.Ide/MainWindow.xaml
- src/Axiom.Ide/MainWindow.xaml.cs
- src/Axiom.Ide/ViewModels/MainWindowViewModel.cs
- tests/Axiom.Ide.Tests/EditorFeatureTests.cs
- tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs
- docs/SPECIFICATION.md
- docs/PLANS.md
- docs/WORKLOG2.md
### Проверка
- dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore - passed.
- dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\ - passed.
### Решения
- Для переименования в файловом дереве отдельный диалог больше не нужен: сценарий встроенного редактирования ближе к поведению проводника Windows и быстрее в работе.
- Подтверждение по потере фокуса оставлено включённым, чтобы переименование вело себя естественно при клике мышью вне поля редактирования.
### Следующий шаг
- Вручную проверить встроенное переименование файла и каталога в IDE в обеих темах и убедиться, что фокус, выделение имени и подтверждение по потере фокуса ощущаются предсказуемо.
## Session 255 - 2026-04-22 06:46 - расширенный журнал IDE для автоформатирования и тем

### Что сделал
- Проверил текущую инфраструктуру IDE и подтвердил, что базовый файловый трассировщик уже существовал, но почти не покрывал путь `переключение стиля -> переход курсора -> автоформатирование -> применение темы`.
- Расширил трассировку в `MainWindowViewModel`: теперь журнал фиксирует переключение классического и современного стиля форматирования, переключение темы оболочки, полное переформатирование активного `.axm`-документа, отсутствие изменений и пропуск форматирования для неподходящих файлов.
- Расширил трассировку в `MainWindow.xaml.cs`: добавил записи по загрузке редактора, получению и потере фокуса, переходам курсора между строками, блочному автодополнению, построчному автоформатированию и применению темы к открытым редакторам.
- Для проблемных обработчиков редактора добавил локальную защиту: исключение теперь записывается в журнал с контекстом документа и положения каретки, а путь к текущему файлу журнала выводится в строку состояния вместо немого закрытия IDE.
- Обновил тесты IDE под новые категории журнала и текстовые проверки наличия новой трассировки.
- Зафиксировал новый договор в `docs/SPECIFICATION.md` и обновил ближайший план расследования в `docs/PLANS.md`.

### Изменённые файлы
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore`

### Решения
- Для расследования текущего вылета не нужен новый отдельный подсистемный лог: достаточно расширить уже существующий `FileIdeTraceService`, чтобы он покрывал редакторные события, где реально возникает сбой.
- Контекст редактора в журнале должен быть прикреплён к каждой подозрительной записи, иначе по одному только тексту исключения трудно восстановить, какой документ и какой переход строки его вызвал.
- Локальная защита обработчиков выбрана осознанно: на этом этапе важнее не потерять трассу и дать пользователю повторить сценарий несколько раз подряд, чем снова получить немой выход процесса.

### Следующий шаг
- Воспроизвести в `Axiom.Ide` или `Axiom.Ide.SmokeHarness` сценарий переключения между классическим и современным стилем, навигации по строкам и автоматической расстановки пробелов, затем разобрать свежий журнал и pinpoint-исправить первую падающую последовательность.
## Session 256 - 2026-04-22 07:11 - исправление навигационного автоформатирования и ложной блочной правки

### Что сделал
- Разобрал свежий журнал `C:\Axiom\.artifacts\bin\Axiom.Ide\Debug\net10.0-windows\Logs\ide-session-20260422-065818.log` и подтвердил два отдельных дефекта: в журнале были только `editor.mouse.line-transition`, а переходы стрелками не давали `editor.caret.line-transition`; кроме того, при простой навигации мышью появлялись записи `editor.block-edit.applied`, после которых в документ вставлялись лишние строки.
- Исправил обработчик перемещения каретки в `MainWindow.xaml.cs`: теперь он привязывается к активному `TextEditor`, поэтому уход со строки клавиатурой снова должен запускать построчное форматирование.
- Убрал вызов `TryApplyBlockEditing(...)` из обычных переходов между строками мышью и кареткой. Структурная блочная правка больше не должна самопроизвольно вставлять пустые строки и закрывающие конструкции при простом перемещении по коду.
- Доработал `AxiomCodeFormattingService`: если пользователь разорвал операторы сдвига пробелами, форматтер снова собирает их в `<<` и `>>` без внутреннего пробела.
- Добавил и обновил тесты, чтобы закрепить новое поведение редактора и форматтера.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore` - passed.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Причина ложных вставок строк оказалась общей для нескольких блочных конструкций, а не только для `Select Case`: проблема сидела в самом факте вызова блочной автоправки на любой смене строки.
- Для текущего расследования важнее предсказуемая навигация и чистый журнал, чем прежняя попытка автоматически достраивать блоки на простом уходе со строки.

### Следующий шаг
- Вручную проверить в IDE, что переход стрелками снова форматирует покинутую строку.
- Вручную проверить, что `Select Case`, `If`, `ElseIf`, `Case`, `Sub`, `Function` и другие блоки больше не вставляют лишние строки при кликах и переходах.
- Снять следующий журнал и убедиться, что при обычной навигации больше не появляется `editor.block-edit.applied`.
## Session 257 - 2026-04-22 07:22 - возврат блочного автодополнения по Enter

### Что сделал
- После пользовательского отчёта проверил текущую обвязку редактора и подтвердил, что прошлое исправление убрало ложную блочную правку с навигации, но вместе с ней отключило и нормальное автодополнение блоков по `Enter`.
- Добавил в `MainWindow.xaml.cs` обработчик `PreviewKeyDown` для редактора и связал блочное автодополнение с нажатием `Enter` через отложенный вызов после фактического создания новой строки.
- Оставил обычную навигацию по строкам отдельным сценарием: мышь и стрелки по-прежнему выполняют только построчное форматирование, без вставки `End If`, `End Select`, `Next` и подобных строк.
- Расширил тесты к сервису блочной правки прямыми сценариями `For` -> `Next` и `Select Case` -> `End Select`, а в IDE-тестах зафиксировал наличие новой клавиатурной обвязки.

### Изменённые файлы
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore` - passed.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Блочное автодополнение сохранено, но теперь живёт только на явном действии пользователя `Enter`, а не на любой смене строки.
- Такое разделение оставляет предсказуемой навигацию по коду и одновременно возвращает привычное достраивание конструкций при вводе.

### Следующий шаг
- Вручную проверить в IDE, что `Enter` снова достраивает `End If`, `End Select`, `Next`, `End Sub`, `End Function` и `Loop`.
- Убедиться, что клики мышью и переходы стрелками по-прежнему не вставляют лишние строки.
- Если редкий вылет IDE снова проявится, снять новый журнал и продолжить расследование уже на этой версии поведения редактора.
## Session 258 - 2026-04-22 07:31 - возврат полного форматирования по команде меню для активного BASIC-документа

### Что сделал
- Сверил пользовательский отчёт с документами проекта и подтвердил проектный договор: ложная звёздочка при открытии была вызвана автоформатированием в `OnActiveDocumentChanged()`, а не ручной командой `Format -> Classic/Modern`.
- Проверил текущую реализацию `MainWindowViewModel` и нашёл слишком узкое условие: полное форматирование по команде меню применялось только к сохранённым файлам `*.axm`, но пропускало новый `Untitled.axm`.
- Расширил правило применимости форматирования: явная команда стиля теперь работает и для несохранённого BASIC-документа, оставаясь отключённой для `.txt`, `.md` и других нефункциональных текстовых вкладок.
- Добавил тест, который фиксирует полное переформатирование `Untitled.axm` по команде `UseModernFormattingCommand`.

### Изменённые файлы
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj --no-restore` - passed.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.

### Решения
- Полное форматирование по команде меню возвращено только как явное действие пользователя; автоматическое форматирование на открытии и активации вкладки не возвращается.
- Звёздочка `*` после ручного полного форматирования считается нормальным dirty-state, а звёздочка сразу после открытия файла по-прежнему считается багом.

### Следующий шаг
- Вручную проверить в IDE, что `Format -> Classic` и `Format -> Modern` сразу переписывают весь текст и для `Untitled.axm`, и для сохранённого `.axm`.
- Отдельно убедиться, что простое открытие существующего файла всё ещё не поднимает ложную звёздочку в заголовке вкладки.
## Session 259 - 2026-04-22 08:13 - ребрендинг репозитория в Axiom и переход на C:\Axiom

### Что сделал
- Выполнил сквозной ребрендинг репозитория: имя `MyBasic` заменено на `Axiom` в решении, проектах, пространствах имён, IDE-ресурсах, документации, manual и тестовых проектах.
- Перенёс рабочую копию проекта в `C:\Axiom` и сделал этот каталог каноническим корнем репозитория; исходный `C:\basic` оставлен только как контрольный снимок для сверки и потому, что текущая среда ещё стартовала из него.
- Перевёл BASIC-расширение с `.mbas` на `.axm` в коде, документации, samples, acceptance-примерах, подсветке и тестах без сохранения обратной совместимости по старому расширению.
- Для IDE добавил общий корень рабочего каталога `C:\Axiom`: при отсутствии `LastDirectoryPath` файловая панель и диалоги открытия/сохранения теперь стартуют именно из `C:\Axiom`, а не из пользовательских `My Documents`.
- Добавил IDE-тесты на сценарий без сохранённой истории каталогов и на `Save As` для нового `Untitled.axm`, чтобы новый корень был закреплён автоматически.

### Изменённые файлы
- корневые файлы и структура репозитория: `Axiom.sln`, `src/Axiom.*`, `tests/Axiom.*`, `samples/**/*.axm`
- IDE и поведение рабочего каталога: `src/Axiom.Ide/Services/Persistence/IdeWorkspaceDefaults.cs`, `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`, `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- IDE-тесты: `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- документация проекта: `README.md`, `docs/SPECIFICATION.md`, `docs/PLANS.md`, `docs/WORKLOG.md`, `docs/WORKLOG2.md`, `docs/manual/`

### Проверка
- `rg -n --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/.artifacts/**' "MyBasic|mybasic|MYBASIC|\\.mbas|C:\\basic" C:\Axiom` - рабочие вхождения в коде, ресурсах, тестах и samples устранены; остаточные совпадения допускаются только в явных заметках о миграции внутри `docs/`.
- `dotnet build Axiom.sln` - passed.
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\` - passed.
- `dotnet test Axiom.sln --no-build` - падает на уже существующих до ребрендинга тестах в `Axiom.Semantics.Tests`, `Axiom.Runtime.Tests` и `Axiom.Ide.Tests`; те же самые падения воспроизводятся и на исходном `C:\basic\MyBasic.sln`.

### Решения
- Массовая текстовая замена выполнена повторно через побайтовую замену поверх свежей копии из `C:\basic`, чтобы не ломать исходные кодировки русских текстов, smoke-образцов и специальных файловых тестов.
- Каноническим именем продукта и репозитория теперь считается только `Axiom`; старое имя `MyBasic` оставлено лишь в старой копии каталога вне рабочего дерева `C:\Axiom`.
- Каноническое расширение языка теперь только `.axm`; совместимость с `.mbas` в репозитории и IDE сознательно не сохраняется.

### Следующий шаг
- Вручную проверить `Axiom.Ide` из `C:\Axiom`, что файловая панель, новый `Untitled.axm` и `Save As` действительно стартуют из `C:\Axiom`.
- После этого вернуться к уже существующему хвосту красных тестов рантайма, семантики и IDE, который воспроизводился ещё до ребрендинга.

## Session 260 - 2026-04-22 08:40 - единое правило применимости форматирования в IDE

### Что сделал
- Прочитал свежий журнал IDE и подтвердил причину пользовательского сценария: для файла с неподдерживаемым расширением команда `Format -> Classic/Modern` писала `format.document.skip`, но построчное автоформатирование редактора всё равно продолжало менять отдельные строки при переходах курсора.
- Вынес общее правило применимости форматирования в `DocumentFormattingPolicy`, чтобы одно и то же условие использовали и `MainWindowViewModel`, и код окна редактора.
- Подключил это правило к автологике редактора: построчное автоформатирование и блочное автодополнение больше не должны срабатывать для документов, которые не считаются форматируемыми Axiom-документами.
- Добавил регрессионные IDE-тесты на само правило и проверку, что код окна действительно использует его.
- Обновил спецификацию и план проекта, чтобы зафиксировать новый договор без частичного автоформатирования для неподдерживаемых расширений.

### Какие файлы изменены
- `src/Axiom.Ide/Editor/Formatting/DocumentFormattingPolicy.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/DocumentFormattingPolicyTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Нужен прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`.

### Решения
- Полное форматирование, построчное автоформатирование и блочное автодополнение теперь обязаны опираться на один и тот же признак «это редактируемый `.axm`/`Untitled.axm` документ AxiomВ».
- Для исторического `.mbas` и любых других не-Axiom расширений IDE больше не должна менять регистр операторов по кускам после `format.document.skip`.

### Следующий шаг
- Вручную воспроизвести пользовательский сценарий через `File Manager`: открыть файл, переименовать его, переключить `Classic/Modern` и сверить поведение отдельно для `.axm` и для неподдерживаемого расширения по свежему журналу IDE.

## Session 261 - 2026-04-22 08:54 - аудит одномерных массивов

### Что сделал
- Прочитал актуальные проектные документы и сверил их с кодом, тестами, справкой и каталогом runnable-примеров по одномерным массивам.
- Подтвердил, что массивы реализованы не как узкая `DIM`-фича, а как отдельная подсистема языка:
  - массив является значением языка с явным типом `T()`;
  - поддерживаются выделенное, невыделенное и каноническое пустое состояние `1 TO 0`;
  - сохраняется ссылочная семантика при присваивании и передаче параметров;
  - реализованы `REDIM`, `REDIM PRESERVE`, `ERASE`, информационные built-in, pure/read-only операции, mutating-операции, поисковый пакет и minimal higher-order пакет.
- Отдельно подтвердил, что договор по одномерным массивам закреплён не только в коде, но и в `docs/manual/arrays.html`, автоматических тестах и тематических `.axm`-примерах.
- Зафиксировал вывод для текущего состояния языка: базовая потребность в новых функциях для одномерных массивов как самостоятельной подсистемы в целом закрыта; незакрытыми остаются только более тяжёлые и уже осознанно отложенные расширения общего API.

### Какие файлы просмотрены
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `src/Axiom.Runtime/Environment/RuntimeScope.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `samples/functions/array*/`

### Проверка
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`

### Решения
- Для одномерных массивов уже существует широкий законченный пакет функций, достаточный не только для минимального ядра языка, но и для практического использования:
  - жизненный цикл массива;
  - копирование и производные новые массивы;
  - поиск, сортировка и преобразования;
  - локальная мутация по месту;
  - first-class и higher-order сценарии в минимальном стабильном виде.
- Не стоит объявлять массивный API незавершённым только потому, что ещё не добавлены все возможные варианты LINQ-подобных или контейнерных функций.
- Осмысленно отложенными остаются только более дорогие расширения общего дизайна:
  - polymorphic higher-order формы `ARRAYMAP` (`T -> U`) и `ARRAYREDUCE` (`A + T -> A`);
  - более общий postfix indexing произвольных выражений вместо текущего named-array indexing;
  - возможные дополнительные API для вставки целых диапазонов, диапазонного удаления и более общего поиска по predicate, если они действительно понадобятся в реальных программах.

### Следующий шаг
- По плану проекта ближайший рабочий шаг остаётся в IDE.
- По массивам следующий содержательный шаг нужен только при явном решении расширять уже стабильный пакет до более общего polymorphic API или до richer syntax поверх текущей модели.
## Session 262 - 2026-04-22 09:12 - полиморфные ARRAYMAP и ARRAYREDUCE

### Что сделано
- Реализован полиморфный `ARRAYMAP(array, selector)`:
  - binder теперь принимает `selector` в форме `FUNCTION(T) AS U`;
  - результирующий тип built-in выводится как `U()`;
  - сохранена нормализация результата к компактным границам `1 TO N` или `1 TO 0`.
- Реализован полиморфный `ARRAYREDUCE(array, seed, reducer)`:
  - binder теперь принимает `reducer` в форме `FUNCTION(A, T) AS A`;
  - результирующий тип built-in выводится как `A`;
  - `seed` повторно согласуется с типом аккумулятора по тем же правилам приведения, что и обычные выражения.
- Runtime `ARRAYMAP` переведён на использование уже выведенного типа результата bound call вместо типа исходного массива.
- Обновлены binder/runtime тесты под полиморфные сценарии и сохранены same-type регрессии.
- Обновлена пользовательская справка по массивам и индекс manual.
- Добавлены runnable acceptance samples:
  - `samples/functions/arraymap/polymorphic_transform.axm`
  - `samples/functions/arrayreduce/polymorphic_accumulator.axm`

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Array.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/manual/arrays.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `samples/functions/arraymap/polymorphic_transform.axm`
- `samples/functions/arrayreduce/polymorphic_accumulator.axm`

### Проверка
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\test-obj\`

### Решения
- Полиморфия введена точечно только для `ARRAYMAP` и `ARRAYREDUCE`, без общего механизма параметров типа по всему языку.
- Для `ARRAYMAP` сохранён запрет на вложенные массивы в результате, потому что массивы массивов в текущем поколении языка не поддерживаются.
- Для `ARRAYREDUCE` аккумулятор выводится из `seed` и сигнатуры `reducer`, а не фиксируется типом элемента массива.

### Следующий шаг
- Вернуться к текущему активному направлению по IDE из `docs/PLANS.md`; массивный higher-order слой на этом этапе закрыт.
## Session 263 - 2026-04-22 09:51 - ручная проверка пройдена

### Что подтверждено
- Пользователь вручную проверил полиморфные `ARRAYMAP` и `ARRAYREDUCE` в IDE; поведение подтверждено как рабочее.
- Пользователь вручную проверил сценарий IDE с форматированием и переименованием; регрессия не воспроизводится, поведение подтверждено как рабочее.

### Решение
- Ручные acceptance-проверки по двум активным направлениям считаются закрытыми.
- Отдельный шаг плана, требовавший именно этой ручной проверки, больше не является открытым блокером.

### Следующий шаг
- Брать следующую содержательную задачу проекта без возврата к этим ручным check-point, если не появится новый сигнал о регрессии.

## Session 264 - 2026-04-22 10:06 - поиск и замена в IDE приведены к модели Notepad++

### Что сделано
- Диалог поиска и замены перестроен под двухвкладочную модель `Найти` / `Замена` вместо старой формы с комбинированной кнопкой.
- Из интерфейса убрана кнопка `ReplaceAndFind`; вместо неё оставлены отдельные действия `Найти далее`, `Заменить`, `Заменить всё`, `Закрыть`.
- Направление поиска переведено с пары переключателей на отдельный флаг обратного направления, ближе к поведению Notepad++.
- Сценарий `Replace` вынесен в отдельный workflow-слой и изменён на более предсказуемый:
  - если текущее совпадение ещё не выбрано, команда сначала только находит следующее совпадение;
  - если совпадение уже выбрано, команда заменяет его и переходит к следующему;
  - если дальше совпадений нет, это отдельно отражается в строке состояния.
- Обновлены русские и английские подписи диалога.
- Добавлены тесты на новый workflow поиска/замены и обновлены XAML-проверки формы.

### Какие файлы изменены
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml.cs`
- `src/Axiom.Ide/Editor/EditorFindReplaceWorkflow.cs`
- `src/Axiom.Ide/Editor/FindReplaceActionEventArgs.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFindReplaceWorkflowTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/EditorFindReplaceEngineTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Решения
- За референс взята общая модель диалога Notepad++: отдельные вкладки `Find` / `Replace`, отдельные действия вместо объединённой кнопки и явный флаг обратного направления.
- Прозрачность и другие вспомогательные украшения из Notepad++ сознательно не переносились.

### Следующий шаг
- Вручную проверить в живой `Axiom.Ide` сценарии `Найти далее`, `Заменить`, `Заменить всё`, обратное направление и режим `Regex`, чтобы подтвердить, что новое поведение удобно и не даёт регрессий в реальном окне.

## Session 265 - 2026-04-22 10:21 - оформление окна поиска в тёмной теме поправлено

### Что сделано
- Для окна поиска и замены добавлены отдельные стили вкладок диалога, чтобы в тёмной теме они больше не тянули светлый системный шаблон и не рисовали белую полосу под вкладками.
- Блок параметров поиска переведён с системного `GroupBox` на собственную рамку диалога, поэтому тяжёлая светлая окантовка исчезла.
- Окно расширено по ширине и высоте, увеличена колонка кнопок, чтобы кнопка `Закрыть` больше не обрезалась.
- Подпись обратного направления поиска сокращена до `Искать назад`, чтобы она стабильно помещалась в диалоге.
- Тесты ресурсов и XAML-проверки обновлены под новые стили и структуру окна.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Решения
- Для диалога поиска введены собственные `DialogTabControlStyle`, `DialogTabItemStyle` и `DialogSectionBorderStyle`, чтобы не зависеть от системного оформления `TabControl` и `GroupBox`.
- За визуальный ориентир оставлены тёмные вкладки нижней панели IDE, а не стандартные WPF-контролы.

### Следующий шаг
- Вручную проверить в тёмной теме `Axiom.Ide`, что вкладки, разделитель под ними, блок параметров и кнопка `Закрыть` теперь выглядят корректно без ручного растягивания окна.

## Session 266 - 2026-04-22 10:33 - мелкая доводка окна поиска

### Что сделано
- Блок параметров поиска больше не растягивается на всё свободное место по высоте: строка с рамкой переведена на размер по содержимому.
- Всем четырём кнопкам действия в окне поиска задана одинаковая ширина `168`, поэтому `Закрыть` больше не должна выглядеть короче остальных.
- XAML-проверка обновлена, чтобы закрепить одинаковую ширину кнопок и автоподбор высоты блока параметров.

### Какие файлы изменены
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Следующий шаг
- Вручную проверить окно поиска в светлой и тёмной теме: рамка параметров должна быть компактной, а кнопки справа должны быть одной ширины.

## Session 267 - 2026-04-22 10:54 - исправлен отступ кнопки Закрыть

### Что сделано
- Исправлена причина смещения кнопки `Закрыть`: она наследовала общий отступ из `ToolBarButtonStyle`, потому что в отличие от трёх верхних кнопок не имела локального `Margin`.
- `DialogButtonStyle` теперь явно сбрасывает `Margin` в `0`, а `CloseButton` дополнительно закреплён с `Margin="0"`.
- XAML-проверка обновлена, чтобы кнопка `Закрыть` больше не могла снова унаследовать внешний отступ.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Следующий шаг
- Вручную открыть окно поиска и проверить, что у `Закрыть` видна правая граница, а левая граница стоит в одной вертикали с остальными кнопками.

## Session 268 - 2026-04-22 10:57 - стили окна поиска централизованы

### Что сделано
- Размеры и выравнивание кнопок поиска вынесены из локального XAML в стили:
  - `FindReplaceActionButtonStyle`;
  - `FindReplaceLastActionButtonStyle`.
- Размер и вертикальное выравнивание полей `Найти` / `Заменить на` вынесены в `FindReplaceTextBoxStyle`.
- Окантовка блока параметров поиска вынесена в `FindReplaceOptionsBorderStyle`.
- XAML окна поиска больше не содержит ручных `Width="168"`, `Height="26"` и `Height="28"` на конкретных кнопках и полях.
- Тесты обновлены так, чтобы закреплять именно использование общих стилей, а не копирование размеров в каждом контроле.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Решение
- Для поиска и замены принят стильный слой с ролевыми стилями вместо локальных размеров на каждом элементе.

### Следующий шаг
- Вручную проверить окно поиска: внешний вид должен остаться тем же, но кнопки, поля и рамка теперь управляются едиными стилями.

## Session 269 - 2026-04-22 10:59 - общий стиль вкладок IDE

### Что сделано
- Выделен общий `ShellTabItemStyle` для самих вкладок.
- Вкладки документов, нижней панели `Вывод` / `Диагностика` и диалога поиска `Найти` / `Замена` теперь используют один стиль вкладки.
- `DocumentTabItemStyle` оставлен как совместимый алиас поверх `ShellTabItemStyle`, чтобы не ломать существующие ссылки.
- Для контейнеров вкладок выделен общий `ShellTabControlStyle`, от которого наследуются специализированные контейнеры.
- `DialogTabControlStyle` сохранён как отдельный контейнерный стиль, потому что диалог поиска использует вкладки только как переключатель режима без содержимого вкладок.
- Тесты обновлены под общий стиль вкладок.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`

### Решение
- Единым считается стиль самого `TabItem`. Контейнеры `TabControl` могут оставаться специализированными, если у них разная структура содержимого.

### Следующий шаг
- Вручную проверить, что `Вывод`, `Диагностика`, `Найти` и `Замена` визуально совпадают по стилю вкладки в светлой и тёмной теме.

## Session 270 - 2026-04-22 11:10 - иконка приложения Axiom IDE

### Что сделано
- Добавлен исходный SVG-макет иконки приложения в `src/Axiom.Ide/Assets/axiom-icon.svg`.
- Сгенерирован Windows `.ico` с наборами размеров для окна, панели задач и исполняемого файла: `src/Axiom.Ide/Assets/axiom.ico`.
- Проект `Axiom.Ide` подключает иконку через `ApplicationIcon`.
- Главное окно IDE явно использует `Icon="Assets/axiom.ico"`.
- Тесты IDE дополнены проверкой, что иконка приложения подключена в проекте и в главном окне.

### Какие файлы изменены
- `src/Axiom.Ide/Axiom.Ide.csproj`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Assets/axiom-icon.svg`
- `src/Axiom.Ide/Assets/axiom.ico`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`

### Решение
- Иконка хранится как исходный SVG и как готовый `.ico`, потому что WPF-проекту и Windows-оболочке нужен именно `.ico`, а SVG остаётся редактируемым исходником.

### Следующий шаг
- Вручную запустить `Axiom.Ide` и проверить отображение иконки в заголовке окна, на панели задач и у собранного приложения.

## Session 271 - 2026-04-22 11:20 - иконка и локализация окна О программе

### Что сделано
- Информационные сообщения IDE переведены с системного `MessageBox` на собственный `InformationDialog`.
- В `InformationDialog` добавлена иконка `src/Axiom.Ide/Assets/axiom.ico` в заголовок окна и в область сообщения.
- Текст окна `О программе` больше не зашит на английском в `MainWindowViewModel`.
- В `ShellStrings.ru.xaml` и `ShellStrings.en.xaml` добавлены строки `About.Title`, `About.Message` и `About.Status`.
- `ShowAbout` выбирает строки по текущему `SelectedLanguage`, поэтому при русском языке показывает русский текст, а при английском - английский.
- Тесты обновлены для проверки русской и английской версии окна `О программе`, ключей локализации и использования иконки в `InformationDialog`.

### Какие файлы изменены
- `src/Axiom.Ide/Services/Prompts/InformationDialog.xaml`
- `src/Axiom.Ide/Services/Prompts/InformationDialog.xaml.cs`
- `src/Axiom.Ide/Services/Prompts/DialogPromptService.cs`
- `src/Axiom.Ide/Services/Shell/IdeLanguageResourceService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`

### Решение
- Для окна `О программе` используется общий информационный диалог с иконкой Axiom, а языковые строки берутся из ресурсных словарей по выбранному языку IDE.

### Следующий шаг
- Вручную открыть `Справка -> О программе` в русской и английской локализации и проверить иконку, заголовок и текст сообщения.

## Session 272 - 2026-04-22 11:33 - временные статусы диалогов IDE

### Что сделано
- В `InformationDialog` картинка внутри окна заменена с растянутого `.ico` на векторную XAML-иконку, повторяющую SVG-исходник. `.ico` оставлен только как иконка заголовка окна.
- Статус окна `О программе` разделён на `About.StatusOpened` и `About.StatusClosed`.
- `ShowAbout` теперь выставляет статус `открыто` перед показом модального окна и статус `закрыто` после его закрытия.
- Временные статусы поиска и замены помечаются отдельным флагом и очищаются при закрытии окна поиска.
- Добавлен общий ресурс `Status.Ready`, чтобы после закрытия окна поиска статус возвращался в `Готово.` / `Ready.` по текущей локализации.
- Тесты обновлены для проверки векторной иконки, статусов `О программе`, ключей локализации и очистки статуса поиска при закрытии окна.

### Какие файлы изменены
- `src/Axiom.Ide/Services/Prompts/InformationDialog.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`

### Проверка
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --no-restore -p:UseSharedCompilation=false -p:BaseOutputPath=C:\Axiom\.artifacts\ide-test-bin\ -p:BaseIntermediateOutputPath=C:\Axiom\.artifacts\ide-test-obj\`
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-validation-bin\`

### Решение
- Статусы, связанные с модальными или временными окнами, не должны оставаться как постоянное состояние после закрытия окна.

### Следующий шаг
- Вручную проверить `Справка -> О программе` и окно поиска: после закрытия не должно оставаться сообщения `окно открыто` или `Совпадений не найдено`.

## Session 273 - 2026-04-22 11:55 - Умный идентификатор в IDE

### Что изменилось
- В `Axiom.Ide` добавлена опциональная функция `Умный идентификатор`, которая восстанавливает канонический регистр уже объявленных пользовательских имён.
- Для этой функции выделен отдельный сервис `src/Axiom.Ide/Editor/Identifiers/AxiomSmartIdentifierService.cs`.
- Сервис использует существующий parser/AST Axiom, а не текстовые эвристики:
  - собирает канонические имена по объявлениям;
  - затем нормализует только `IdentifierToken` в нужной строке или во всём документе.
- В текущем срезе канонический вид имени берётся из объявлений `DIM`, `CONST`, `SUB`, `FUNCTION`, параметров и управляющей переменной `FOR`.
- Опция подключена в меню `Format` как чекбокс `Умный идентификатор`.
- Состояние чекбокса сохраняется в shell-state IDE и восстанавливается между запусками.
- При включённой опции нормализация подключена в двух местах:
  - при уходе со строки в редакторе;
  - при полном форматировании документа командами `Format -> Classic` и `Format -> Modern`.
- Добавлены отдельные IDE-тесты для сервиса, сохранения настройки и влияния опции на полное форматирование документа.

### Какие файлы изменены
- `src/Axiom.Ide/Editor/Identifiers/AxiomSmartIdentifierService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/Services/Persistence/RecentFilesState.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesSnapshot.cs`
- `src/Axiom.Ide/Services/Persistence/IRecentFilesService.cs`
- `src/Axiom.Ide/Services/Persistence/RecentFilesService.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/SmartIdentifierServiceTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/RecentFilesServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~SmartIdentifierServiceTests|FullyQualifiedName~Constructor_DisablesSmartIdentifierByDefault|FullyQualifiedName~UseModernFormattingCommand_AlsoNormalizesIdentifiersWhenSmartIdentifierEnabled|FullyQualifiedName~ToggleSmartIdentifierCommand_SwitchesPreferenceAndPersistsShellState|FullyQualifiedName~UpdateShellPreferences_PersistsFilesPaneAndStatusBarVisibilityAndWindowState|FullyQualifiedName~GetSnapshot_LoadsPersistedShellPreferences" -p:UseSharedCompilation=false`
- Дополнительно полный прогон `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false` показал 5 уже существующих падений вне этого среза: один строковый тест с mojibake-ожиданием удаления файла и четыре старых хрупких проверки XAML/строковых шаблонов.

### Решения
- Функция сделана именно опциональной и по умолчанию выключенной, чтобы не менять существующее поведение редактора внезапно.
- Канонический регистр берётся из объявления, а не из случайного первого использования.
- Строки, комментарии, ключевые слова, встроенные функции и необъявленные имена не переписываются.

### Следующий точный шаг
- Вручную проверить `Format -> Умный идентификатор` в живой `Axiom.Ide`: уход со строки и полное форматирование `Classic` / `Modern` на `.axm`-документе с разным регистром одного и того же имени.

## Session 274 - 2026-04-22 12:06 - блокировка меню оболочки во время окна поиска

### Что изменилось
- Уточнено поведение `Find / Replace` в оболочке IDE.
- Окно поиска оставлено немодальным относительно редактора, но на время его видимости верхнее меню и панель быстрых кнопок теперь блокируются.
- После закрытия окна поиска меню и быстрые кнопки автоматически снова включаются.
- Строковые IDE-тесты обновлены под новые имена элементов оболочки и под более устойчивую проверку trace-вызовов в `MainWindow.xaml.cs`.

### Какие файлы изменены
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~MainWindowXaml_ExposesAutomationHooksForSmokeHarness|FullyQualifiedName~MainWindowCodeBehind_TracksCaretAndBottomPaneFocus" -p:UseSharedCompilation=false`

### Решения
- Для этого UX-среза не вводился модальный `ShowDialog()`, потому что окно поиска должно по-прежнему работать вместе с редактором.
- Блокировка ограничена shell-chrome уровнем: верхнее меню и быстрые кнопки выключаются, но работа с редактором и самим окном поиска сохраняется.

### Следующий точный шаг
- Вручную проверить в живой `Axiom.Ide`, что при открытом `Find / Replace` pull-down меню и быстрые кнопки не активируются до закрытия окна.

## Session 275 - 2026-04-22 12:18 - перевод окна поиска на модальное поведение владельца

### Что изменилось
- По уточнению пользователя предыдущая промежуточная реализация признана неверной.
- Ручная блокировка только верхнего меню и панели быстрых кнопок убрана.
- Окно `Find / Replace` переведено на настоящее модальное поведение владельца через `ShowDialog()` при `Owner = MainWindow`.
- Начальный фокус поиска и выделение текста перенесены в жизненный цикл самого окна поиска, чтобы они работали и при модальном показе.
- Строковые IDE-тесты скорректированы под новый модальный сценарий вместо самодельной блокировки оболочки.

### Какие файлы изменены
- `src/Axiom.Ide/MainWindow.xaml`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-modal-validation-bin\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~MainWindowXaml_ExposesAutomationHooksForSmokeHarness|FullyQualifiedName~MainWindowCodeBehind_TracksCaretAndBottomPaneFocus" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-modal-test-run\`

### Решения
- Для этого сценария выбран не отдельный запрет на клики по оболочке, а штатный механизм модального окна владельца.
- Эталон поведения — тот же UX, что уже используется у окна `О программе`.

### Следующий точный шаг
- Вручную проверить в живой `Axiom.Ide`, что `Find / Replace` теперь действительно ведёт себя как `О программе`: владелец не принимает ввод и не раскрывает меню до закрытия диалога.

## Session 276 - 2026-04-22 12:55 - аудит IDE и кодировка строк

### Что изменилось
- Проведён критический аудит `Axiom.Ide`.
- В `MainWindowViewModel.cs` восстановлены русские строки диалогов создания и удаления; файл сохранён в UTF-8 с BOM.
- Полный прогон `Axiom.Ide.Tests` теперь даёт 3 оставшихся падения, связанных со строковыми XAML-тестами.

### Какие файлы изменены
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~MoveFilesPaneNodeToRecycleBinCommand_ConfirmsAndDeletesSelectedFile" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-audit-targeted-test2\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-audit-full-test\`

### Решения
- Главный технический долг IDE сейчас сосредоточен в `MainWindow.xaml`, `MainWindow.xaml.cs` и `MainWindowViewModel.cs`.

### Следующий точный шаг
- Разделить shell-логику `MainWindow` и затем стабилизировать IDE-тесты.

## Session 277 - 2026-04-22 13:26 - декомпозиция shell IDE и стабилизация XAML-проверок

### Что изменилось
- `MainWindow` разрезан на partial-файлы по ответственности: файловая панель, поиск/замена и визуально-древесные helper-методы вынесены из общего `MainWindow.xaml.cs`.
- `MainWindowViewModel` переведён на partial-структуру, а логика переключения `Classic` / `Modern`, применения форматирования и опции `Умный идентификатор` вынесена в `MainWindowViewModel.Formatting.cs`.
- Для `FindReplaceDialog` размеры окна вынесены в общий стиль `FindReplaceDialogWindowStyle`, а разметка перестала опираться на лишние жёсткие ширины колонок там, где достаточно `Auto` и уже существующих стилей кнопок.
- Строковые IDE-тесты перепривязаны к новой структуре partial-файлов и к устойчивым признакам XAML-шаблонов, а не к случайным литералам старого монолита.

### Какие файлы изменены
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/MainWindow.FilesPane.cs`
- `src/Axiom.Ide/MainWindow.Search.cs`
- `src/Axiom.Ide/MainWindow.VisualTree.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.Formatting.cs`
- `src/Axiom.Ide/Editor/FindReplaceDialog.xaml`
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~FindReplaceDialogXaml_ContainsExpectedControls|FullyQualifiedName~ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators|FullyQualifiedName~ShellStylesXaml_UsesCustomTemplatesForMenuAndContextMenu" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-audit-targeted-xaml\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-full-after-shell-pass-3\`

### Решения
- В этом проходе приоритет отдан не косметике, а снижению архитектурного риска: shell-логика IDE больше не держится в одном гигантском code-behind-файле.
- XAML-проверки сохранены, но переведены на более устойчивую модель, чтобы дальнейшие рефакторинги не ломались из-за буквального расположения строк в одном файле.

### Следующий точный шаг
- Собрать создание shell-зависимостей в один явный composition root и затем убрать наиболее рискованные UI-пути с `async void` и синхронным ожиданием задач.
## Session 278 - 2026-04-22 13:44 - корень композиции shell и уборка блокирующих ожиданий

### Что изменилось
- В `Axiom.Ide` добавлен `IdeShellCompositionRoot`, чтобы `App.xaml.cs` больше не создавал вручную весь граф shell-зависимостей.
- `MainWindow` переведён на явный конструктор с `ICodeFormattingService`, `AxiomSmartIdentifierService` и `AxiomBlockEditingService`, а `MainWindowViewModel` больше не создаёт внутри себя shell- и editor-зависимости.
- `IDialogPromptService` и `DocumentWorkspaceService` возвращены к синхронному модальному пути закрытия документов; в shell-коде больше нет `async void` и `GetAwaiter().GetResult()`.
- `MainWindowViewModelTests` обновлены под явное внедрение зависимостей, что делает тестовую сборку оболочки более детерминированной.

### Какие файлы изменены
- `src/Axiom.Ide/App.xaml.cs`
- `src/Axiom.Ide/MainWindow.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `src/Axiom.Ide/Services/Prompts/DialogPromptService.cs`
- `src/Axiom.Ide/Services/Shell/IdeShellCompositionRoot.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-pass-4\`

### Решения
- Для shell IDE выбрана явная композиция сервисов вместо скрытых `new` в окне и view model, чтобы дальнейший рефакторинг не растаскивал зависимости по разным точкам.
- На путях закрытия документов выбрано не имитировать асинхронность, а опереться на настоящую модальную модель окна и синхронные shell-диалоги.

### Следующий точный шаг
- Пройтись по `Resources/ShellStyles.xaml` и убрать оставшееся дублирование в шаблонах меню и контекстного меню.

## Session 279 - 2026-04-22 13:57 - устранение дублирования popup-каркаса меню

### Что изменилось
- В `ShellStyles.xaml` общий каркас submenu-popup вынесен в `ShellPopupMenuScrollViewerTemplate` и `ShellPopupMenuScrollViewerStyle`.
- `ShellTopLevelHeaderTemplate` и `ShellSubmenuHeaderTemplate` теперь используют один и тот же scrollviewer-шаблон вместо дублирования `Grid` / `Canvas` / `Rectangle` / `ItemsPresenter`.
- Строковые IDE-проверки обновлены под новую структуру стилей в `EditorFeatureTests.cs`.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-style-dedupe-build\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators|FullyQualifiedName~ShellStylesXaml_UsesCustomTemplatesForMenuAndContextMenu" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-shell-style-dedupe-tests\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false`

### Решения
- Повтор в popup-ветках меню убран не через слишком абстрактный общий `MenuItem`-шаблон, а через общий `ScrollViewer`-каркас, чтобы не смешивать разную геометрию верхнего меню и вложенных подменю.
- Сбой полного `dotnet test` с отдельным `OutputPath` признан инфраструктурным WPF-эффектом временного `_wpftmp`-проекта; корректность правки подтверждена полным обычным прогоном тестов.

### Следующий точный шаг
- Пройтись по shell-локализации и унифицировать показ диалогов и строк статуса между ресурсами, fallback-строками и prompt-сервисом.

## Session 280 - 2026-04-22 14:18 - метрики меню и локализация shell-статусов

### Что изменилось
- Исправлена вертикальная метрика меню в `ShellStyles.xaml`, чтобы русские буквы с нижними выносными элементами не подрезались в верхнем меню и выпадающих пунктах.
- `MainMenuStyle` получил немного больший вертикальный отступ, верхнеуровневые `MenuItem` теперь центрируют заголовок по вертикали явно, а submenu-шаблоны используют `MinHeight` вместо жёсткой высоты.
- В `MainWindowViewModel` вынесены в языковые ресурсы shell-строки для `Close/Exit`, `Manual` и `Run`; добавлен helper для локализованных форматируемых сообщений.
- `ShellStrings.ru.xaml` и `ShellStrings.en.xaml` дополнены соответствующими ключами, а тесты обновлены под русское значение по умолчанию там, где это поведение теперь стало пользовательским.

### Какие файлы изменены
- `src/Axiom.Ide/Resources/ShellStyles.xaml`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-build\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~ShellStylesXaml_UsesClassicMenuGutterAndIndentedSeparators|FullyQualifiedName~RunProgram_SetsOutputAndStatus|FullyQualifiedName~RunProgram_WithDiagnostics_SelectsAndFocusesDiagnostics|FullyQualifiedName~RunProgram_PromptsToSaveDirtyDocumentBeforeRunning|FullyQualifiedName~RunProgram_WhenUserDeclinesSaveBeforeRun_DoesNotRun|FullyQualifiedName~CloseTabCommand_DirtyDocumentAndPromptCancel_KeepsTabOpen|FullyQualifiedName~OpenManualCommand_OpensManualThroughExternalNavigationService|FullyQualifiedName~OpenManualCommand_WhenManualIsMissing_ShowsPromptError" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-targeted\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-menu-localization-full\`

### Решения
- Исправление читаемости меню сделано через метрики и выравнивание, а не через случайный подбор шрифта или уменьшение размера текста.
- Локализация оболочки пока расширяется по зонам ответственности: сначала наиболее заметные общесистемные сценарии shell, затем файловые и сервисные ветки.

### Следующий точный шаг
- Продолжить локализацию shell-оболочки в файловом менеджере, свойствах, переименовании, удалении и остальных статусных сообщениях `MainWindowViewModel`.

## Session 281 - 2026-04-22 15:07 - второй проход по локализации файловой панели

### Что изменилось
- `MainWindowViewModel` переведён на ресурсные строки в файловом срезе оболочки:
  - недоступный элемент в `Recent Files`;
  - переход по каталогу;
  - открытие во внешнем приложении;
  - создание файла и каталога;
  - вырезание, копирование и вставка;
  - удаление в Корзину;
  - переименование;
  - показ скрытых файлов;
  - предупреждение о блокировке `.exe` в редакторе.
- Окно `Свойства` файлового узла больше не использует жёстко зашитые английские подписи; текст свойств теперь собирается через активный языковой словарь shell.
- `ShellStrings.ru.xaml` и `ShellStrings.en.xaml` дополнены ключами для файлового менеджера, а `MainWindowViewModelTests` обновлены под локализованные заголовки и статусы.

### Какие файлы изменены
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-build\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter "FullyQualifiedName~NavigateFilesPaneNodeCommand_SwitchesRootToSelectedDirectory|FullyQualifiedName~NewFilesPaneDirectoryCommand_CreatesFolderInSelectedDirectory|FullyQualifiedName~FilesPaneShowHiddenFilesChange_ReloadsTreeWithHiddenFlag|FullyQualifiedName~ShowFilesPaneNodePropertiesCommand_UsesPromptServiceForSelectedNode|FullyQualifiedName~MoveFilesPaneNodeToRecycleBinCommand_ConfirmsAndDeletesSelectedFile|FullyQualifiedName~OpenFilesPaneNodeCommand_DoesNotOpenExecutableFiles" -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-tests\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-files-localization-full\`

### Решения
- Локализация файлового менеджера продолжена тем же способом, что и для `Run / Manual / Close`: через ресурсные ключи и форматируемые shell-сообщения, а не через новые разрозненные литералы.
- Текст окна `Свойства` оставлен в `MainWindowViewModel`, а не вынесен в отдельный сервис, потому что это чистая оболочечная сборка представления без отдельной бизнес-логики.

### Следующий точный шаг
- Продолжить локализацию в `DocumentWorkspaceService` и связанных shell-сервисах, чтобы убрать оставшиеся английские статусы открытия, сохранения и общего сохранения документов.

## Session 282 - 2026-04-22 15:37 - локализация документных статусов и prompt сохранения

### Что изменилось
- `DocumentWorkspaceService` переведён на ресурсные строки для `Open`, `Save`, `Save As` и `Save All`; сообщения больше не зашиты английскими литералами и выбираются по текущему языку IDE.
- `MainWindowViewModel` синхронизирован с тем же набором ключей для fallback-статусов документных команд и для создания нового документа.
- `WpfDialogPromptService` теперь локализует заголовок и текст prompt-а `Save / Discard / Cancel` через активные ресурсы оболочки.
- Обновлены ресурсные словари `ShellStrings.ru.xaml` и `ShellStrings.en.xaml`, а тесты дополнены проверками новых ключей и фактического переключения статуса на английский.

### Какие файлы изменены
- `src/Axiom.Ide/Documents/DocumentWorkspaceService.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Services/Prompts/DialogPromptService.cs`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-document-localization-tests\`

### Решения
- Для документного workflow выбран тот же ресурсно-ориентированный путь, что и для остальной shell-локализации: сервисы больше не возвращают пользовательские строки как англоязычные fallback-литералы.
- Для `Save / Discard / Cancel` не вводился отдельный сервис локализации prompt-ов; `WpfDialogPromptService` читает активные ключи напрямую из ресурсов приложения, чтобы диалог автоматически следовал текущему языку IDE.

### Следующий точный шаг
- Вручную проверить в живой `Axiom.Ide`, что статусы `Open / Save / Save All` и prompt сохранения действительно переключаются между русским и английским вместе с языком оболочки.

## Session 283 - 2026-04-22 15:41 - подтверждение ручной проверки локализации документного workflow

### Что изменилось
- Пользователь вручную проверил живую `Axiom.Ide` после локализации документного workflow.
- Подтверждено, что статусы `Open / Save / Save All` корректно переключаются между русским и английским.
- Подтверждено, что prompt сохранения тоже корректно следует текущему языку IDE.
- `docs/PLANS.md` обновлён: шаг ручной проверки закрыт, зафиксирован следующий локализационный проход по оставшимся глобальным shell-строкам вне `MainWindowViewModel`.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Ручная проверка в живой `Axiom.Ide`: переключение языка IDE подтверждено для статусов `Open / Save / Save All` и prompt сохранения.

### Решения
- Локализация документного workflow считается завершённой не только по тестам, но и по ручной проверке в рабочей оболочке.
- Следующий проход имеет смысл сместить с document workflow на оставшиеся глобальные fallback-строки и инфраструктурные сообщения shell-слоя.

### Следующий точный шаг
- Пройтись по `App.xaml.cs`, fallback-заголовкам prompt-сервисов и инфраструктурным сообщениям `ProgramRunService`, чтобы убрать последние разрозненные англоязычные shell-литералы.

## Session 284 - 2026-04-22 16:04 - локализация глобальных shell-сообщений и заголовков IDE

### Что изменилось
- `IdeLanguageResourceService` расширен helper-методами для чтения строк по текущим ресурсам приложения; это позволило убрать разрозненные shell-литералы из `App.xaml.cs`, `DialogPromptService`, `ProgramRunService`, `StatusBarViewModel` и `MainWindow.Search.cs`.
- `InformationDialog.xaml` и `TextInputDialog.xaml` переведены на `DynamicResource` для стандартного заголовка и кнопки подтверждения, так что типовые prompt-диалоги теперь следуют активному языку оболочки без отдельных ручных строк.
- `MainWindowViewModel` переведён на ресурсные шаблоны заголовка окна IDE; одновременно синхронизирован `About` и добавлены проверки на заголовок окна и новые ключи словаря.
- Обновлены `ShellStrings.ru.xaml` и `ShellStrings.en.xaml`, а IDE-тесты подстроены под новый ресурсный путь для статуса `Готово.` после поиска, заголовков окна и глобальных prompt-ключей.

### Какие файлы изменены
- `src/Axiom.Ide/Services/Shell/IdeLanguageResourceService.cs`
- `src/Axiom.Ide/Services/Prompts/DialogPromptService.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Ide/App.xaml.cs`
- `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs`
- `src/Axiom.Ide/Status/StatusBarViewModel.cs`
- `src/Axiom.Ide/MainWindow.Search.cs`
- `src/Axiom.Ide/Services/Prompts/InformationDialog.xaml`
- `src/Axiom.Ide/Services/Prompts/TextInputDialog.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.ru.xaml`
- `src/Axiom.Ide/Resources/ShellStrings.en.xaml`
- `tests/Axiom.Ide.Tests/MainWindowViewModelTests.cs`
- `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs`
- `tests/Axiom.Ide.Tests/EditorFeatureTests.cs`
- `tests/Axiom.Ide.Tests/ShellResourceTests.cs`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build src\Axiom.Ide\Axiom.Ide.csproj -nodeReuse:false -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-final-localization-build-7\`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj -nodeReuse:false -p:UseSharedCompilation=false -p:OutputPath=C:\Axiom\.artifacts\ide-final-localization-pass-9\`
- Результат тестов: `208/208` зелёные.

### Решения
- Для глобальных shell-сообщений выбран тот же ресурсно-ориентированный путь, что и для `MainWindowViewModel`, чтобы локализация не зависела от того, в каком именно слое рождается пользовательский текст.
- Заголовок окна IDE тоже переведён на ресурсные ключи, хотя текст сейчас одинаков в обоих языках; это убирает ещё одну особую ветку и держит весь shell в одном механизме.

### Следующий точный шаг
- Вручную проверить в живой `Axiom.Ide`, что `About`, стандартные `Info/Input`-диалоги, строка состояния после `Find / Replace` и глобальные shell-сообщения корректно переключаются между русским и английским.

## Session 285 - 2026-04-22 16:34 - подтверждение ручной проверки глобальной shell-локализации

### Что изменилось
- Пользователь вручную проверил живую `Axiom.Ide` после прохода по глобальным shell-сообщениям.
- Подтверждено, что `About`, стандартные `Info/Input`-диалоги, строка состояния после `Find / Replace` и глобальные shell-сообщения корректно переключаются между русским и английским.
- `docs/PLANS.md` обновлён: шаг ручной проверки закрыт, следующим действием зафиксирован короткий остаточный аудит пользовательских строк в `src/Axiom.Ide`.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Ручная проверка в живой `Axiom.Ide`: переключение языка подтверждено для глобальных shell-сценариев последнего прохода.

### Решения
- Глобальная shell-локализация считается закрытой по автоматическим проверкам и ручному smoke-check.
- Перед переходом к следующему крупному пункту стоит сделать короткий контрольный поиск пользовательских строк вне ресурсных словарей, чтобы не оставить очевидных хвостов в оболочке.

### Следующий точный шаг
- Сделать остаточный аудит `src/Axiom.Ide` на явные пользовательские строки вне `Resources/ShellStrings.*.xaml` и после него перейти к следующему незавершённому пункту плана.

## Session 286 - 2026-04-22 16:38 - закрытие остаточного аудита shell-локализации

### Что изменилось
- Пользователь подтвердил, что остаточный аудит `src/Axiom.Ide` после локализационных проходов выглядит нормально.
- Очевидных пользовательских строк, которые нужно немедленно переносить в `Resources/ShellStrings.*.xaml`, не осталось.
- `docs/PLANS.md` обновлён: локализационный хвост закрыт, следующим шагом выбран разбор старой незакрытой отметки `Phase 4` про placeholder-пункты `View/Help` меню.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Ручный остаточный аудит пользователем: явных проблем после локализации не найдено.
- Автоматические проверки не запускались, потому что код не менялся.

### Решения
- Shell-локализация считается завершённой в текущем объёме.
- Следующий переход должен начинаться не с новой локализации, а с проверки фактически оставшегося незакрытого пункта плана.

### Следующий точный шаг
- Сверить старую незакрытую отметку `Phase 4` про placeholder-пункты `View/Help` меню с текущей реализацией `MainWindow` и либо закрыть её как устаревшую, либо добить недостающий command wiring.

## Session 287 - 2026-04-22 16:40 - закрытие старой отметки View/Help

### Что изменилось
- Пользователь подтвердил, что `View/Help` в живой `Axiom.Ide` открывает проектную документацию.
- Старая незакрытая отметка `Phase 4` про placeholder-пункты `View/Help` признана устаревшей и закрытой ручной проверкой.
- `docs/PLANS.md` обновлён: локализационно-shell проход завершён, дальнейшая работа должна начинаться с выбора следующего содержательного пункта общего плана или отложенных IDE-задач.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Ручная проверка в живой `Axiom.Ide`: `View/Help` открывает проектную документацию.
- Автоматические проверки не запускались, потому что код не менялся.

### Решения
- Дополнительный command wiring для `View/Help` не требуется.
- Текущий shell-локализационный хвост и связанная старая отметка `Phase 4` считаются закрытыми.

### Следующий точный шаг
- Перед новым кодовым изменением выбрать следующий содержательный пункт из общего плана или из отложенных IDE-задач.

## Session 288 - 2026-04-22 16:47 - уточнение будущих IDE-направлений

### Что изменилось
- Пользователь уточнил состояние отложенных IDE-задач: тёмная тема готова, переключение тем работает, автодополнение остаётся задачей далёкого будущего.
- `docs/PLANS.md` дополнен актуальным будущим списком IDE-направлений, чтобы не потерять крупные пункты после закрытия текущего shell-прохода.
- В будущем IDE-порядке зафиксированы: восстановление сессии, сложная навигация по коду, отладчик, панель `Variables`, панель `Assistant`.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Автоматические проверки не запускались, потому что код не менялся.

### Решения
- Автодополнение сейчас не планировать в ближайшую работу.
- Панель `Variables` считать зависимой от отладчика или следующей после него.
- Панель `Assistant` оставить самой поздней из перечисленных IDE-задач.

### Следующий точный шаг
- При выборе следующего IDE-направления ориентироваться на порядок: восстановление сессии, сложная навигация по коду, отладчик, `Variables`, `Assistant`.

## Session 289 - 2026-04-22 16:55 - проверка справочника массивов

### Что изменилось
- Проведена сверка фактического списка одномерных массивных built-ins с пользовательским справочником.
- Подтверждено, что `docs/manual/arrays.html` и `docs/manual/scripts/manual.js` уже содержат все массивные функции из семантического слоя.
- Найден и исправлен малый документационный пропуск в обзорной странице `docs/manual/arrays-and-builtins.html`: в список показательных примеров добавлены `LBOUND`, `UBOUND`, `ARRAYLENGTH`, `ARRAYISALLOCATED`.
- Карточка маршрута по одномерным массивам на обзорной странице уточнена: теперь она прямо говорит не только про объявление, границы и доступ, но и про информационные функции и операции над массивами.

### Какие файлы изменены
- `docs/manual/arrays-and-builtins.html`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Сверка `docs/manual/arrays.html`, `docs/manual/arrays-and-builtins.html` и `docs/manual/scripts/manual.js` на полный список массивных функций.
- `node --check docs/manual/scripts/manual.js`
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Array" --no-restore -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Array" --no-restore -p:UseSharedCompilation=false`
- Дополнительно запускался `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~Array" --no-restore -p:UseSharedCompilation=false`; подходящих тестовых случаев под этот фильтр в проекте нет.

### Решения
- Новых функций массива добавлять не потребовалось: кодовая поверхность одномерных массивов уже закрывает текущий договор.
- Обзорная страница справочника должна содержать ссылки и на информационные функции массива, потому что они являются частью пользовательского массива API наравне с операциями `ARRAY*`.

### Следующий точный шаг
- Считать справочник и реализацию одномерных массивов закрытыми в текущем объёме.
- Следующую работу выбирать как отдельную задачу по IDE или языку; к массивам возвращаться только при новом конкретном дефекте или новом запланированном расширении.

## Session 290 - 2026-04-22 17:22 - базовые пользовательские типы TYPE

### Что изменилось
- Изучены исторические ориентиры по пользовательским типам в QuickBASIC, Visual Basic/VBA и присланный старый файл `C:/projects/Axiom/Visitors/AxiomInterpreter.Types.cs`.
- Для Axiom выбран первый устойчивый объём `TYPE ... END TYPE`: верхнеуровневые записи, поля `fieldName AS fieldType`, использование пользовательского типа в `DIM`, параметрах и результате `FUNCTION`, доступ к полям через точку, вложенные пользовательские типы и копирование записи при присваивании целиком.
- Добавлены лексические, синтаксические, семантические и runtime-узлы для пользовательских типов и доступа к полям.
- Добавлены диагностики `MB2021`-`MB2024` для неизвестного типа, неизвестного поля, доступа к полю не у записи и циклических вложенных полей.
- IDE обновлена для подсветки `TYPE`, форматирования `record.field` без пробелов и автозакрытия блока `END TYPE`.
- Добавлен runnable-пример `samples/operators/type/field_access.axm`.
- Справочник обновлён: добавлена страница `docs/manual/type.html`, навигация `docs/manual/scripts/manual.js`, упоминание в `docs/manual/data-types.html` и диагностики в `docs/manual/diagnostics.html`.
- `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md` синхронизированы с новым языковым поведением.

### Какие файлы изменены
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/ProgramAndMembers.cs`
- `src/Axiom.Syntax/Expressions.cs`
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Parsing/Parser.Expressions.cs`
- `src/Axiom.Parsing/Parser.Helpers.cs`
- `src/Axiom.Semantics/Symbols/Symbols.cs`
- `src/Axiom.Semantics/BoundNodes/BoundExpressions.cs`
- `src/Axiom.Semantics/BoundNodes/BoundNodeKind.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Runtime/Values/RuntimeRecordValue.cs`
- `src/Axiom.Runtime/Values/RuntimeValue.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `src/Axiom.Ide/Editor/Formatting/AxiomCodeFormattingService.cs`
- `src/Axiom.Ide/Editor/StructuralEditing/AxiomBlockEditingService.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/type/field_access.axm`
- `docs/manual/type.html`
- `docs/manual/scripts/manual.js`
- `docs/manual/data-types.html`
- `docs/manual/diagnostics.html`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet build Axiom.sln --no-restore`
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter Type --logger "console;verbosity=minimal"`
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter Type --logger "console;verbosity=minimal"`
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter Type --logger "console;verbosity=minimal"`
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter TypeFieldAccessAcceptanceSample --logger "console;verbosity=minimal"`
- `dotnet test tests\Axiom.Ide.Tests\Axiom.Ide.Tests.csproj --filter BlockEditing --logger "console;verbosity=minimal"`
- `node --check docs\manual\scripts\manual.js`
- Полный `dotnet test Axiom.sln --no-restore --logger "console;verbosity=minimal"` запускался отдельно и не прошёл по уже не связанным с `TYPE` падениям: файловые чтения с BOM/пустым файлом, repair-smoke проверки кодировок, `FROMUTC` и ожидаемая диагностика `DATE`.

### Решения
- В первый срез не включать массивы внутри `TYPE`, методы, константы, уровни доступа, `FILE`-поля, функциональные поля, локальные объявления `TYPE` и групповой синтаксис полей.
- Семантику записей сделать значимой, а не ссылочной: присваивание целой записи и передача через пользовательскую процедуру копируют значение.
- Имена и базовая форма оставлены в линии QuickBASIC/VBA `TYPE ... END TYPE`; более богатую модель VB.NET `Structure` в текущий Axiom не переносить.

### Следующий точный шаг
- Открыть `samples/operators/type/field_access.axm` в живой `Axiom.Ide` и вручную запустить пример.
- После ручной проверки следующий языковой срез выбирать отдельной задачей; не смешивать базовые записи с массивами и методами внутри `TYPE`.

## Session 291 - 2026-04-22 17:29 - снятие старой рамки первой версии

### Что изменилось
- Пользователь уточнил, что старая рамка первой версии больше не является продуктовой истиной и должна быть удалена из документов.
- `AGENTS.md` обновлён: правила больше не запрещают крупные возможности из-за старой рамки, а требуют явного проектного решения перед их реализацией.
- `PROJECT.md` обновлён: вместо ограниченного объёма первой версии зафиксирован объём полноценного продукта и отдельно проектируемые направления.
- `README.md` обновлён: описание проекта теперь говорит о готовой среде и последовательном развитии продукта, без старой ограничительной рамки.
- `docs/SPECIFICATION.md` обновлён: заголовок и начальные разделы больше не привязаны к первой версии как к ограничителю; раздел грамматики переименован в сводную грамматику.
- `docs/ARCHITECTURE.md` обновлён: архитектурная рамка описывает текущую интерпретаторную модель и полноценную IDE без прежней ограничительной формулировки.
- `docs/PLANS.md` обновлён: текущее направление и дальнейшие шаги теперь исходят из полноценного продукта.
- `docs/WORKLOG.md` очищен от прямых упоминаний старой рамки, которые могли продолжать задавать неверный курс.

### Какие файлы изменены
- `AGENTS.md`
- `PROJECT.md`
- `README.md`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG.md`
- `docs/WORKLOG2.md`

### Проверка
- Поиск прямого старого маркера ограниченной первой версии по `AGENTS.md`, `PROJECT.md`, `README.md` и `docs`.
- Поиск старых англоязычных заголовков объёма по `AGENTS.md`, `PROJECT.md`, `README.md` и `docs`.
- Поиск старых ограничительных формулировок по `PROJECT.md`, `README.md`, `docs/SPECIFICATION.md`, `docs/ARCHITECTURE.md` и `docs/PLANS.md`.
- Автоматические сборочные тесты не запускались, потому что код и наблюдаемое поведение языка не менялись.

### Решения
- Axiom дальше ведётся как полноценный продукт, а не как ограниченный первый выпуск.
- Исторические номера вроде `v0.1` могут оставаться именами уже завершённых этапов, но не должны ограничивать будущий объём продукта.
- Крупные возможности проектируются явно и последовательно, без автоматического запрета из-за старой рамки.

### Следующий точный шаг
- Продолжить развитие пользовательских типов после ручной проверки `samples/operators/type/field_access.axm`.
- Ближайший практический языковой срез: массивы пользовательских типов и доступ к полям элементов массива вида `people(i).Name`, с предварительным обновлением спецификации и справочника.

## Session 292 - 2026-04-22 17:45 - массивы пользовательских типов

### Что изменилось
- Закрыт следующий срез `TYPE ... END TYPE`: пользовательский тип записи теперь официально используется как тип элемента одномерного массива.
- Закреплён доступ к полям элементов массива: `people(i).Name`, включая чтение, запись и вложенные поля.
- В среде выполнения элемент массива теперь сохраняет запись через `CopyForStorage`, поэтому присваивание вида `people(1) = first` копирует запись и не связывает элемент массива с исходной переменной.
- Добавлен запускаемый пример `samples/operators/type/array_elements.axm`.
- Справочник обновлён на страницах `docs/manual/type.html` и `docs/manual/arrays.html`; указатель `docs/manual/scripts/manual.js` уточняет описание `TYPE`.
- `docs/SPECIFICATION.md` и `docs/ARCHITECTURE.md` синхронизированы с новым поведением.
- Дополнительно очищен оставшийся старый ограничительный текст в `docs/IDE_REDESIGN.md` и уточнена запись проверки в `docs/WORKLOG2.md`.

### Какие файлы изменены
- `src/Axiom.Runtime/Values/RuntimeArrayValue.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/type/array_elements.axm`
- `docs/manual/type.html`
- `docs/manual/arrays.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/IDE_REDESIGN.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter TypeArrayElementFieldAccess --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter TypeArrayElementFieldAccess --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~TypeArray" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`
- `dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter TypeArrayElementsAcceptanceSample --logger "console;verbosity=minimal" -p:UseSharedCompilation=false`
- `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false`
- `node --check docs\manual\scripts\manual.js`
- Поиск старых ограничительных формулировок по `AGENTS.md`, `PROJECT.md`, `README.md` и `docs`.

### Решения
- Массивы пользовательских типов поддержаны только как массивы записей; массивы внутри самого объявления `TYPE` остаются отдельным будущим срезом.
- Массив как контейнер остаётся ссылочным значением при присваивании массива целиком.
- Запись внутри элемента массива хранится по правилам значения: целая запись копируется при записи в элемент, а поле элемента можно менять через обычный доступ с точкой.

### Следующий точный шаг
- Открыть `samples/operators/type/array_elements.axm` в живой `Axiom.Ide` и вручную запустить пример.
- После ручной проверки выбрать следующий отдельный срез развития `TYPE`: массивы внутри записей, групповые поля или более богатую процедурную работу с записями.

## Session 293 - 2026-04-22 21:10 - динамические массивные поля в TYPE

### Что изменилось
- В пользовательских типах записи разрешены динамические массивные поля вида fieldName() AS TYPE.
- Индексирование массива обобщено до массивного выражения, поэтому теперь работают формы user.Scores(1) и people(1).Scores(2).
- REDIM, REDIM PRESERVE и ERASE расширены до массивных полей записи, включая поля у элементов массива записей.
- Добавлен запускаемый пример samples/operators/type/array_fields.axm.
- Справочник обновлён на страницах docs/manual/type.html и docs/manual/arrays.html; указатель docs/manual/scripts/manual.js синхронизирован с новым поведением.
- docs/SPECIFICATION.md и docs/ARCHITECTURE.md дополнены описанием нового среза.

### Какие файлы изменены
- src/Axiom.Syntax/Statements.cs
- src/Axiom.Parsing/Parser.Statements.cs
- src/Axiom.Semantics/BoundNodes/BoundExpressions.cs
- src/Axiom.Semantics/BoundNodes/BoundStatements.cs
- src/Axiom.Semantics/Binding/Binder.Expressions.cs
- src/Axiom.Semantics/Binding/Binder.Statements.cs
- src/Axiom.Semantics/Binding/Binder.cs
- src/Axiom.Runtime/Execution/Interpreter.Expressions.cs
- src/Axiom.Runtime/Execution/Interpreter.Helpers.cs
- src/Axiom.Runtime/Execution/Interpreter.Statements.cs
- tests/Axiom.Parsing.Tests/ParserTests.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/array_fields.axm
- docs/manual/type.html
- docs/manual/arrays.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Parse_RedimStatementWithTypeFieldTarget|FullyQualifiedName~Parse_RedimStatementWithIndexedTypeFieldTarget|FullyQualifiedName~Parse_EraseStatementWithIndexedTypeFieldTarget|FullyQualifiedName~Parse_TypeDeclarationWithDynamicArrayField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_TypeDynamicArrayField|FullyQualifiedName~Bind_TypeDynamicArrayFieldOnArrayElement" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_TypeDynamicArrayField|FullyQualifiedName~Execute_TypeDynamicArrayFieldOnArrayElement" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter TypeArrayFieldsAcceptanceSample_RunsSuccessfully --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false

### Решения
- Внутри TYPE поддержана только динамическая массивная форма без фиксированного диапазона.
- Для доступа к массиву больше не требуется, чтобы его целевое выражение было только именем переменной; это единое правило теперь покрывает и поля записи.
- TYPE как значение по-прежнему копируется целиком, но массивное поле внутри записи остаётся массивным значением со ссылочной семантикой самого массива.

### Следующий точный шаг
- Открыть samples/operators/type/array_fields.axm в живой Axiom.Ide и вручную запустить пример.
- После ручной проверки выбирать следующий отдельный срез по TYPE: фиксированные массивы в полях записи, групповые поля или уточнение семантики копирования записей с массивными полями.


## Session 294 - 2026-04-22 19:02 - фиксированные массивные поля TYPE

### Что изменено
- В `TYPE` разрешены фиксированные массивные поля вида `fieldName(lower TO upper) AS TYPE` наряду со скалярными полями и динамическими массивными полями.
- Связыватель теперь вычисляет константные границы таких полей при завершении объявления `TYPE`, сохраняет их в `TypeFieldSymbol` и сразу отклоняет неконстантные или неверные диапазоны.
- Значение записи по умолчанию автоматически создаёт выделенный массив для фиксированного поля, поэтому `DIM user AS Person` сразу даёт доступ к `user.Scores(i)` без отдельного `REDIM`.
- Копирование записи целиком теперь клонирует фиксированное массивное поле вместе с элементами, но динамические массивные поля внутри записи сохраняют прежнюю ссылочную семантику массива.
- `REDIM`, `REDIM PRESERVE` и `ERASE` теперь диагностируют ошибку `MB2025`, если их применить к фиксированному массивному полю записи.
- Проверка циклических пользовательских типов расширена и на массивные поля записи, чтобы рекурсивная форма не проходила через массивный путь.

### Какие файлы изменены
- src/Axiom.Semantics/Symbols/Symbols.cs
- src/Axiom.Semantics/Binding/Binder.cs
- src/Axiom.Semantics/Binding/Binder.Helpers.cs
- src/Axiom.Semantics/Binding/Binder.Statements.cs
- src/Axiom.Runtime/Values/RuntimeArrayValue.cs
- src/Axiom.Runtime/Values/RuntimeRecordValue.cs
- tests/Axiom.Parsing.Tests/ParserTests.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/fixed_array_fields.axm
- docs/manual/type.html
- docs/manual/arrays.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests\Axiom.Parsing.Tests\Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Parse_TypeDeclarationWithFixedArrayField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Semantics.Tests\Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_TypeFixedArrayField|FullyQualifiedName~Bind_ReportsRedimAndEraseForFixedTypeArrayField|FullyQualifiedName~Bind_ReportsRecursiveTypeThroughArrayField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Runtime.Tests\Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_TypeFixedArrayField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests\Axiom.Execution.Tests\Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~TypeFixedArrayFieldsAcceptanceSample_RunsSuccessfully" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false

### Решения
- Границы фиксированного массивного поля внутри `TYPE` пока ограничены целочисленными константами в самом объявлении; имена `CONST` в этот шаг не включались.
- Диапазон фиксированного поля считается частью формы записи, поэтому для него не допускаются `REDIM`, `REDIM PRESERVE` и `ERASE`.
- Динамическое массивное поле внутри записи сохраняет уже существующую ссылочную семантику массива; фиксированное поле копируется вместе с записью как часть её значения.

### Следующий точный шаг
- Следующим отдельным срезом развить групповые объявления полей внутри `TYPE` и затем снова сверить форму с исторической линией QuickBASIC / Visual Basic.

## Session 295 - 2026-04-22 19:51 - групповые объявления полей TYPE

### Что сделано
- В синтаксис TYPE добавлена групповая форма полей с общей TypeClause: Name, Alias AS STRING, Scores(), Flags(1 TO 2) AS INTEGER.
- TypeFieldSyntax переведён на список деклараторов, а парсер TYPE теперь разбирает запятые внутри строки поля по той же общей модели, что и декларации переменных.
- Связыватель завершения TYPE разворачивает групповые строки в отдельные TypeFieldSymbol, сохраняя все существующие правила для динамических и фиксированных массивных полей.
- Добавлены тесты парсера, binder, runtime и acceptance sample для новой формы.
- Добавлен ручной пример samples/operators/type/grouped_fields.axm.
- Обновлены docs/manual/type.html, docs/manual/arrays.html, docs/manual/scripts/manual.js, а также хвосты docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md.

### Изменённые файлы
- src/Axiom.Syntax/ProgramAndMembers.cs
- src/Axiom.Parsing/Parser.Declarations.cs
- src/Axiom.Semantics/Binding/Binder.cs
- tests/Axiom.Parsing.Tests/ParserTests.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/grouped_fields.axm
- docs/manual/type.html
- docs/manual/arrays.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Parse_TypeDeclarationAndFieldAccess|FullyQualifiedName~Parse_TypeDeclarationWithDynamicArrayField|FullyQualifiedName~Parse_TypeDeclarationWithFixedArrayField|FullyQualifiedName~Parse_TypeDeclarationWithGroupedFields" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_TypeFixedArrayField|FullyQualifiedName~Bind_TypeGroupedFields" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_TypeFixedArrayField|FullyQualifiedName~Execute_TypeGroupedFields" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~TypeFixedArrayFieldsAcceptanceSample_RunsSuccessfully|FullyQualifiedName~TypeGroupedFieldsAcceptanceSample_RunsSuccessfully" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false

### Решения
- Групповая строка TYPE семантически не создаёт особый вид поля: после связывания каждое имя существует как обычное отдельное поле записи.
- Динамическое поле-массив внутри группы сохраняет прежнюю ссылочную семантику; это подтверждено рантайм-тестом и вынесено в ручной пример.
- Для выбора формы зафиксирована смешанная историческая линия: сам TYPE и массивные поля держатся Microsoft BASIC / VBA, а распространение одного AS на группу выбрано по линии Visual Basic, потому что оно лучше подходит для компактных объявлений полного продукта.

### Следующий точный шаг
- Вручную проверить samples/operators/type/grouped_fields.axm в Axiom.Ide, затем отдельно решить поддержку CONST в границах фиксированных полей записи.

## Session 296 - 2026-04-22 20:16 - CONST в границах фиксированных полей TYPE

### Что сделано
- Для фиксированных полей-массивов внутри TYPE добавлена поддержка верхнеуровневых CONST в границах диапазона.
- Binder теперь в раннем проходе собирает доступные значения CONST, необходимые только для границ TYPE, не меняя общую модель видимости обычных верхнеуровневых выражений.
- В вычисление границ включены литералы, ссылки на ранее объявленные CONST, унарные + и -, а также бинарные +, -, *, \ и MOD.
- Добавлены binder/runtime/acceptance-тесты и ручной пример samples/operators/type/const_fixed_array_fields.axm.
- Обновлены docs/manual/type.html, docs/manual/arrays.html, docs/manual/scripts/manual.js, а также хвосты docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md.

### Изменённые файлы
- src/Axiom.Semantics/Binding/Binder.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/const_fixed_array_fields.axm
- docs/manual/type.html
- docs/manual/arrays.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_TypeFixedArrayField|FullyQualifiedName~Bind_TypeFixedArrayFieldWithConstBounds|FullyQualifiedName~Bind_TypeGroupedFields" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_TypeFixedArrayField|FullyQualifiedName~Execute_TypeFixedArrayFieldWithConstBounds|FullyQualifiedName~Execute_TypeGroupedFields" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~TypeFixedArrayFieldsAcceptanceSample_RunsSuccessfully|FullyQualifiedName~TypeConstFixedArrayFieldsAcceptanceSample_RunsSuccessfully|FullyQualifiedName~TypeGroupedFieldsAcceptanceSample_RunsSuccessfully" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- node --check docs\manual\scripts\manual.js
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false

### Решения
- Для границ фиксированных полей записи поддержаны только верхнеуровневые CONST, объявленные выше по файлу; это ограничение зафиксировано как текущая проектная семантика.
- Специальный ранний проход по CONST не объявляет имена в обычной области видимости заранее, а существует только для вычисления границ TYPE.
- Историческая опора этого шага — Type statement (VBA), где для фиксированных полей записи допускаются numeric literals или constants.

### Следующий точный шаг
- Вручную проверить samples/operators/type/const_fixed_array_fields.axm в Axiom.Ide, затем выбрать следующий языковой хвост по TYPE или вернуться к незавершённым задачам по массивам и справочнику.

## Session 297 - 2026-04-22 20:16 - снятие ограничения на порядок CONST для TYPE

### Что сделано
- Для фиксированных полей-массивов внутри TYPE снято ограничение на порядок объявления CONST: теперь такие границы работают и тогда, когда константы объявлены ниже TYPE в том же файле.
- Binder переведён с линейного накопления уже встреченных констант на отдельное разрешение зависимостей между подходящими верхнеуровневыми CONST.
- Добавлены новые binder/runtime/acceptance-сценарии и ручной пример samples/operators/type/const_fixed_array_fields_forward.axm.
- Обновлены docs/manual/type.html, docs/manual/arrays.html, а также хвосты docs/SPECIFICATION.md, docs/ARCHITECTURE.md, docs/PLANS.md.
- Параллельно зафиксирован компактный отбор оставшихся исторически полезных форм TYPE по QuickBasic / Visual Basic и старому Action.

### Изменённые файлы
- src/Axiom.Semantics/Binding/Binder.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/type/const_fixed_array_fields_forward.axm
- docs/manual/type.html
- docs/manual/arrays.html
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_TypeFixedArrayFieldWithConstBoundsDeclaredLater|FullyQualifiedName~Bind_TypeFixedArrayFieldWithConstBounds" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_TypeFixedArrayFieldWithConstBoundsDeclaredLater|FullyQualifiedName~Execute_TypeFixedArrayFieldWithConstBounds" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~TypeConstFixedArrayFieldsForwardAcceptanceSample_RunsSuccessfully|FullyQualifiedName~TypeConstFixedArrayFieldsAcceptanceSample_RunsSuccessfully" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- node --check docs\manual\scripts\manual.js
- dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false

### Решения
- Независимость от порядка объявления введена только для вычисления фиксированных границ полей TYPE; это не означает общего изменения модели видимости верхнеуровневых имён.
- Исторически этот шаг продолжает линию VBA Type statement, где допустимы constants в границах, но в Axiom теперь дополнительно снят искусственный линейный барьер предыдущей реализации.
- После короткого аудита следующим по-настоящему сильным кандидатом остаются фиксированные строки внутри TYPE, тогда как тяжёлые расширения старого Action не смешиваются с текущей базовой добивкой записей.

### Следующий точный шаг
- Вручную проверить samples/operators/type/const_fixed_array_fields_forward.axm в Axiom.Ide, затем отдельно принять решение по фиксированным строкам в TYPE либо по возврату к массивам и справочнику.

## Session 298 - 2026-04-22 20:54 - фиксированные строки STRING * N в DIM и TYPE

### Что сделано
- Поддержана форма фиксированной строки `STRING * N`:
  - в `DIM` для обычных переменных;
  - в полях пользовательских записей `TYPE`.
- Выбрана архитектура без нового самостоятельного типа рантайма:
  - парсер различает `STRING * N` отдельным синтаксическим узлом;
  - семантически это по-прежнему `STRING`;
  - длина хранится как метаданные символа переменной или поля записи.
- Зафиксировано и реализовано поведение хранения:
  - значение по умолчанию для фиксированной строки — строка из пробелов заданной длины;
  - короткое присваивание дополняется пробелами справа;
  - длинное присваивание обрезается справа.
- Тот же механизм доведён до рантайма объявлений, присваиваний, копирования записей и строковых массивов с такими метаданными.
- Добавлены парсерные, семантические, рантайм- и acceptance-тесты, а также два ручных примера:
  - `samples/operators/dim/fixed_string.axm`;
  - `samples/operators/type/fixed_string_fields.axm`.
- Обновлены страницы ручного руководства по `DIM`, `STRING` и `TYPE`, а также индекс `docs/manual/scripts/manual.js`.

### Изменённые файлы
- src/Axiom.Syntax/SyntaxKind.cs
- src/Axiom.Syntax/ProgramAndMembers.cs
- src/Axiom.Parsing/Parser.Declarations.cs
- src/Axiom.Semantics/Symbols/Symbols.cs
- src/Axiom.Semantics/Binding/Binder.cs
- src/Axiom.Semantics/Binding/Binder.Helpers.cs
- src/Axiom.Semantics/Binding/Binder.Statements.cs
- src/Axiom.Runtime/Values/RuntimeValue.cs
- src/Axiom.Runtime/Values/RuntimeArrayValue.cs
- src/Axiom.Runtime/Values/RuntimeRecordValue.cs
- src/Axiom.Runtime/Execution/Interpreter.Statements.cs
- src/Axiom.Runtime/Execution/Interpreter.Helpers.cs
- tests/Axiom.Parsing.Tests/ParserTests.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- tests/Axiom.Runtime.Tests/InterpreterTests.cs
- tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs
- samples/operators/dim/fixed_string.axm
- samples/operators/type/fixed_string_fields.axm
- docs/manual/variables-and-constants.html
- docs/manual/data-types.html
- docs/manual/type.html
- docs/manual/scripts/manual.js
- docs/SPECIFICATION.md
- docs/ARCHITECTURE.md
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --filter "FullyQualifiedName~Parse_DimStatementWithFixedLengthStringType|FullyQualifiedName~Parse_TypeDeclarationWithFixedLengthStringField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --filter "FullyQualifiedName~Bind_DimFixedLengthStringVariable|FullyQualifiedName~Bind_TypeFixedLengthStringField" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --filter "FullyQualifiedName~Execute_DimFixedLengthStringPadsAndTruncates|FullyQualifiedName~Execute_TypeFixedLengthStringFieldPadsAndTruncates" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --filter "FullyQualifiedName~TypeFixedStringFieldsAcceptanceSample_RunsSuccessfully|FullyQualifiedName~DimFixedStringAcceptanceSample_RunsSuccessfully" --logger "console;verbosity=minimal" -p:UseSharedCompilation=false
- node --check docs\manual\scripts\manual.js
- Полный `dotnet build Axiom.sln --no-restore -p:UseSharedCompilation=false` сейчас упирается в заблокированные файлы `Axiom.Ide.exe`, если IDE оставлена запущенной во время сборки.

### Решения
- Историческая форма взята из линии QuickBasic / Visual Basic: фиксированная длина задаётся как `STRING * N`, а не отдельным именем типа.
- Архитектурно Axiom не заводит для этой формы отдельный строковый тип времени выполнения: поведение фиксированной длины живёт в метаданных объявления и нормализации хранения.
- Практическая область поддержки на текущем шаге сознательно ограничена `DIM` и полями `TYPE`; при необходимости оставшиеся контексты будут либо явно поддержаны, либо закрыты диагностикой.

### Следующий точный шаг
- Вручную открыть `samples/operators/dim/fixed_string.axm` и `samples/operators/type/fixed_string_fields.axm` в `Axiom.Ide`, затем подтвердить ожидаемый вывод;
- после ручной проверки либо добить явные запреты на неподдержанные контексты `STRING * N`, либо вернуться к обещанному аудиту массивов и полноте справочника массивных функций.

## Session 299 - 2026-04-22 21:28 - пометка о будущей оптимизации памяти для фиксированных строк

### Что сделано
- После обсуждения поведения `STRING * N` отдельно зафиксировано, что текущая реализация:
  - обеспечивает фиксированную длину хранения на уровне семантики;
  - не является оптимизацией памяти по сравнению с обычным `STRING`;
  - для коротких значений может быть даже менее выгодной по памяти из-за пробельного дополнения.
- В `docs/PLANS.md` добавлена отдельная будущая задача на более плотное внутреннее хранение фиксированных строк без изменения пользовательского синтаксиса `STRING * N`.

### Изменённые файлы
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- Дополнительные автоматические проверки не требовались: изменены только проектные документы.

### Решения
- Memory-оптимизация фиксированных строк не смешивается с уже завершённым пользовательским контрактом `STRING * N`.
- Если к ней возвращаться, это должен быть отдельный архитектурный шаг с проверкой влияния на записи, массивы и файловые сценарии.

### Следующий точный шаг
- Держать текущий приоритет без изменений: ручная проверка примеров `fixed_string.axm` и `fixed_string_fields.axm`, затем возврат к хвосту по массивам и справочнику либо к жёсткой фиксации неподдержанных контекстов `STRING * N`.

## Session 300 - 2026-04-22 21:43 - запрет STRING * N в неподдержанных контекстах

### Что изменилось
- Закрыт хвост по фиксированным строкам вне реально поддержанных мест:
  - `STRING * N` теперь даёт `MB2026` в `CONST`;
  - `STRING * N` теперь даёт `MB2026` в параметрах и типе результата `FUNCTION`;
  - рекурсивно закрыты и вложенные формы внутри функциональных типов, чтобы запись вроде `FUNCTION(STRING * 4) AS INTEGER` не проходила молча как обычный `STRING`.
- В парсере фиксированной строки скорректирован разбор длины:
  - `=` после `STRING * N` больше не втягивается в выражение длины;
  - благодаря этому неподдержанная форма в `CONST` теперь доходит до binder и получает честную целевую диагностику вместо побочного синтаксического сбоя.
- Обновлены пользовательские страницы справочника по типам, переменным/константам и `FUNCTION`.
- Добавлены отрицательные acceptance-примеры `.axm` для неподдержанной формы в `CONST` и `FUNCTION`.

### Какие файлы изменены
- `src/Axiom.Parsing/Parser.Declarations.cs`
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.cs`
- `tests/Axiom.Parsing.Tests/ParserTests.cs`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/operators/const/fixed_string_error.axm`
- `samples/operators/function/fixed_string_parameter_error.axm`
- `samples/operators/function/fixed_string_return_error.axm`
- `docs/manual/data-types.html`
- `docs/manual/variables-and-constants.html`
- `docs/manual/function.html`
- `docs/SPECIFICATION.md`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "Parse_DimStatementWithFixedLengthStringType|Parse_ConstStatementWithFixedLengthStringType|Parse_FunctionDeclarationWithFixedLengthStringReturnType"`
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "Bind_ReportsFixedStringConstDeclaration|Bind_ReportsFixedStringFunctionParameter|Bind_ReportsFixedStringFunctionReturnType|Bind_ReportsNestedFixedStringInsideFunctionType|Bind_DimFixedLengthStringVariable|Bind_TypeFixedLengthStringField"`

### Решения
- На этом шаге форма `STRING * N` подтверждена только в двух местах: `DIM` и строковые поля `TYPE`.
- Во всех остальных типовых позициях пока выгоднее не расширять рантайм-метаданные и сигнатуры, а давать явный запрет `MB2026`.

### Следующий шаг
- Начать разбиение файлов binder по зонам ответственности и одновременно сделать аудит кодировки русских строк в `src/Axiom.Semantics/Binding/`.
## Session 301 - 2026-04-22 22:31 - вынос диагностик binder и зачистка helpers

### Что изменилось
- В src/Axiom.Semantics/Binding/ добавлен новый partial-файл Binder.Diagnostics.cs.
- Все методы Report... и генерация типовых диагностик binder вынесены из Binder.Helpers.cs в отдельный файл с нормальными русскими сообщениями.
- Binder.Helpers.cs после этого стал уже по назначению helper-слоем, без смешивания с крупным блоком текстов диагностик.
- Во время полного прогона semantic-тестов выявлен устаревший тест по DATE(1): он ожидал ошибку по числу аргументов, но текущая реализация DATE уже поддерживает строковый аргумент часового пояса, поэтому корректная диагностика там — несовместимость типа. Тест приведён в соответствие с реальной сигнатурой.

### Какие файлы изменены
- src/Axiom.Semantics/Binding/Binder.Diagnostics.cs
- src/Axiom.Semantics/Binding/Binder.Helpers.cs
- tests/Axiom.Semantics.Tests/BinderTests.cs
- docs/PLANS.md
- docs/WORKLOG2.md

### Проверка
- dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore
- dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FixedStringConstAcceptanceSample_ReturnsExpectedDiagnostic|FixedStringFunctionParameterAcceptanceSample_ReturnsExpectedDiagnostic|FixedStringFunctionReturnAcceptanceSample_ReturnsExpectedDiagnostic|NegativeAcceptanceProgram_ReturnsExpectedDiagnostic"
- dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "Parse_DimStatementWithFixedLengthStringType|Parse_ConstStatementWithFixedLengthStringType|Parse_FunctionDeclarationWithFixedLengthStringReturnType"

### Решения
- Для binder зафиксировано направление дальнейшего разбиения: diagnostics больше не должны жить вперемешку с helper-методами.
- Устаревшие тестовые ожидания по встроенным функциям нужно корректировать по фактическому набору перегрузок, а не по старому историческому состоянию.

### Следующий шаг
- Продолжить разбиение Binder.cs на более узкие partial-файлы и отдельно добить аудит кодировки по всей папке src/Axiom.Semantics/Binding/.

## Session 302 - 2026-04-22 22:32 - вынос деклараций, процедур и invocation-слоя binder

### Что изменилось
- `Binder.cs` дополнительно разгружен и теперь держит только общую композицию binder, поля состояния, конструктор и запуск `BindProgram`.
- Из бывшего монолита `Binder.cs` вынесены два новых профильных partial-файла:
  - `Binder.Declarations.cs` для объявления имён типов, подготовки `TYPE`, регистрации верхнеуровневых целочисленных `CONST` для фиксированных границ и объявления процедур/функций;
  - `Binder.Routines.cs` для привязки тел `SUB`/`FUNCTION`, локальных функций и routine-level структуры.
- Из `Binder.Expressions.cs` вынесен ещё один самостоятельный слой `Binder.InvocationExpressions.cs`, который теперь содержит:
  - вызовы значений и функций;
  - доступ к элементам массивов и полям записей;
  - привязку assignable-target для массивного доступа;
  - разрешение built-in вызовов, включая массивные и полиморфные формы.
- Во время этого выноса поправлен заметный битый хвост в строковом имени массива по умолчанию: вместо искажённой строки теперь используется нормальный русский текст `массива`.

### Какие файлы изменены
- `src/Axiom.Semantics/Binding/Binder.cs`
- `src/Axiom.Semantics/Binding/Binder.Declarations.cs`
- `src/Axiom.Semantics/Binding/Binder.Routines.cs`
- `src/Axiom.Semantics/Binding/Binder.Expressions.cs`
- `src/Axiom.Semantics/Binding/Binder.InvocationExpressions.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "Parse_DimStatementWithFixedLengthStringType|Parse_ConstStatementWithFixedLengthStringType|Parse_FunctionDeclarationWithFixedLengthStringReturnType"`
- Дополнительный текстовый аудит `src/Axiom.Semantics/Binding/*.cs`:
  - проверка на replacement-символы и C1-управляющие символы;
  - отдельный поиск заметных следов mojibake по текущим файлам после разбиения.

### Решения
- Для binder закреплена более жёсткая структура: корневой `Binder.cs` не должен снова превращаться в место для крупных блоков деклараций, routine-level логики или invocation-привязки.
- Invocation-слой выгодно держать отдельно от базовой арифметики и литеральных выражений, потому что именно там сходятся вызовы built-in, массивы, поля записей и assignable-target.
- Аудит `Binding/` на этом шаге не показал текущих следов битой кириллицы в рабочих `.cs`-файлах.

### Следующий шаг
- Продолжить разбиение `Binder.Helpers.cs`, чтобы отделить числовые/константные helper-методы от остальной служебной логики binder.

## Session 303 - 2026-04-22 22:33 - вынос type-resolution, scope и syntax helper-слоёв binder

### Что изменилось
- Из `Binder.Helpers.cs` вынесен отдельный слой `Binder.TypeResolution.cs`:
  - разрешение `TypeClauseSyntax` и `TypeSyntax`;
  - разбор и проверка `STRING * N`;
  - вычисление целочисленных константных выражений для типовых сценариев;
  - проверка циклических полей `TYPE`.
- Из того же файла вынесены ещё два маленьких, но устойчивых слоя:
  - `Binder.ScopeHelpers.cs` для поиска переменных, работы с областями видимости и scoped-деклараций;
  - `Binder.SyntaxHelpers.cs` для `GetLocation`, текста операторов, program/member/parameter location и разбора числового текста литералов.
- После этих выносов `Binder.Helpers.cs` сжат до компактного набора действительно общих числовых helper-методов и локальной диагностики `ARRAYMAP`.
- В `Binder.Helpers.cs` дополнительно убраны уже не нужные `using`.

### Какие файлы изменены
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`
- `src/Axiom.Semantics/Binding/Binder.TypeResolution.cs`
- `src/Axiom.Semantics/Binding/Binder.ScopeHelpers.cs`
- `src/Axiom.Semantics/Binding/Binder.SyntaxHelpers.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore`
- `dotnet test tests/Axiom.Parsing.Tests/Axiom.Parsing.Tests.csproj --no-restore --filter "Parse_DimStatementWithFixedLengthStringType|Parse_ConstStatementWithFixedLengthStringType|Parse_FunctionDeclarationWithFixedLengthStringReturnType"`

### Решения
- Типовое разрешение и fixed-string-валидация оказались самостоятельным слоем binder и больше не должны жить вперемешку с областями видимости или координатными helper-методами.
- После текущего шага дальнейшее дробление `Binder.Helpers.cs` уже не выглядит обязательным: файл стал маленьким и однородным.
- Следующий крупный кандидат на отдельный рефакторинг сейчас — `Binder.Statements.cs`.

### Следующий шаг
- Перейти к разбиению `Binder.Statements.cs`, если не появится более срочная локальная задача по семантическому слою.

## Session 304 - 2026-04-23 07:49 - разбиение statement-слоя binder

### Что изменилось
- `Binder.Statements.cs` сжат до маршрутизации операторов: `BindStatements` и `BindStatement`.
- Добавлены новые partial-файлы по зонам ответственности:
  - `Binder.DeclarationStatements.cs` для `DIM`, `CONST`, присваиваний, `ERASE` и `REDIM`;
  - `Binder.IoStatements.cs` для `PRINT`, `PRINT USING`, `INPUT`, `OPEN` и `CLOSE`;
  - `Binder.ControlFlowStatements.cs` для `IF`, `WHILE`, `DO`, `FOR`, `EXIT FOR`, `EXIT DO` и `RETURN`;
  - `Binder.SelectCaseStatements.cs` для всей логики `SELECT CASE` и его selector-форм.
- Набор файлов binder в архитектурной заметке обновлён под фактическую раскладку.

### Какие файлы изменены
- `src/Axiom.Semantics/Binding/Binder.Statements.cs`
- `src/Axiom.Semantics/Binding/Binder.DeclarationStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.IoStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.ControlFlowStatements.cs`
- `src/Axiom.Semantics/Binding/Binder.SelectCaseStatements.cs`
- `docs/ARCHITECTURE.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore`

### Решения
- `Binder.Statements.cs` больше не должен принимать тела отдельных операторов; его роль — маршрутизация синтаксического оператора к профильному файлу.
- `SELECT CASE` оставлен отдельным файлом, потому что внутри него есть собственная мини-семантика selector-форм, нормализации типов и проверок сравнения.
- Разбиение не меняет поведение языка, поэтому новые `.axm`-примеры и страницы справочника не требовались.

### Следующий шаг
- Сделать короткий контрольный проход по `src/Axiom.Semantics/Binding/` на остаточные признаки битой русской кодировки и слишком крупные файлы, затем вернуться к массивам и справочнику массивных функций.

## Session 305 - 2026-04-23 07:51 - контрольный аудит Binding после разбиения binder

### Что сделано
- Выполнен контрольный проход по `src/Axiom.Semantics/Binding/` после разбиения statement-слоя binder.
- Проверены размеры текущих `.cs`-файлов binder и распределение методов по новым partial-файлам.
- Отдельно проверены явные признаки битой русской кодировки, replacement-символы и C1-управляющие символы.

### Какие файлы изменены
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Поиск явных mojibake-маркеров по `src/Axiom.Semantics/Binding/*.cs` не нашёл совпадений.
- Проверка replacement-символов и C1-управляющих символов показала чистый результат для всех файлов `Binding/*.cs`.
- Предыдущий прогон `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore` после разбиения statement-слоя прошёл успешно.

### Решения
- Дополнительный разрез `Binder.InvocationExpressions.cs`, `Binder.Expressions.cs` или `Binder.Declarations.cs` прямо сейчас не обязателен: файлы крупные, но пока цельные по ответственности.
- Массово чинить старые исторические повреждения в `docs/WORKLOG2.md` в этом шаге не нужно, чтобы не смешивать аудит binder с отдельной миграцией документа.

### Следующий шаг
- Вернуться к массивам: сверить реализованные массивные функции с пользовательским справочником, навигацией справочника и текущими примерами.

## Session 306 - 2026-04-23 08:01 - аудит массивных функций и восстановление runtime-диагностик

### Что изменилось
- Сверен фактический набор массивных встроенных функций из `BuiltinFunctions.Array.cs` с пользовательским справочником:
  - `docs/manual/arrays.html`;
  - алфавитным указателем `docs/manual/scripts/manual.js`;
  - обзорной страницей `docs/manual/arrays-and-builtins.html`;
  - папками ручных примеров `samples/functions/`.
- Подтверждено, что все 30 массивных имён из кода присутствуют в справочнике, указателе и каталоге примеров.
- В `src/Axiom.Runtime/Execution/` восстановлены оставшиеся повреждённые русские строки диагностик времени выполнения.
- Для `READFILE` и `READALLLINES` добавлено удаление начального UTF-8 BOM из результата чтения.
- Для `FROMUTC` добавлена явная проверка выхода результата преобразования за поддерживаемый диапазон даты-времени, чтобы поведение не зависело от зажимающего поведения платформы.
- Тестовый helper для smoke-примера восстановления документов теперь регистронезависимо находит строки `DIM sourcePath` и `DIM workPath`.
- Добавлен ручной пример `samples/functions/readalllines/utf8_bom.axm`.

### Какие файлы изменены
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs`
- `src/Axiom.Runtime/Execution/Interpreter.FileIO.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Helpers.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Statements.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.Helpers.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/functions/readalllines/utf8_bom.axm`
- `docs/manual/file-system-functions.html`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- Сверка массива: 30 имён в коде, 30 карточек в `manual.js`, 30 якорей в `arrays.html`, 30 папок примеров в `samples/functions/`.
- Проверка ссылок `docs/manual/arrays-and-builtins.html`: 30 ссылок на массивные примеры, отсутствующих файлов нет.
- `node --check docs/manual/scripts/manual.js`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~ArrayInsert|FullyQualifiedName~ArrayRemove|FullyQualifiedName~SkipLine"`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~FromUtcReportsOutOfRange|FullyQualifiedName~BuiltinFileSystemReadsAllLines|FullyQualifiedName~SmokeSampleRepairs"`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter ReadAllLinesUtf8BomAcceptanceSample`
- Поиск mojibake-маркеров по `src/Axiom.Runtime/Execution/*.cs` не нашёл совпадений.

### Решения
- По одномерным массивам сейчас не найдено недостающих функций или недостающих справочных карточек.
- Повреждённые русские строки в runtime-диагностиках считать закрытым качественным хвостом для текущей папки `src/Axiom.Runtime/Execution/`.
- UTF-8 BOM при чтении текстового файла является служебной меткой и не должен попадать в результат `READFILE` или в первую строку `READALLLINES`.
- `FROMUTC` должен сам проверять выход за диапазон, а не полагаться на поведение `TimeZoneInfo.ConvertTimeFromUtc`.

### Следующий шаг
- Вручную открыть `samples/functions/readalllines/utf8_bom.axm` в `Axiom.Ide` и подтвердить вывод `1`, `alpha`.
- После ручной проверки перейти к следующему языковому срезу; первым кандидатом остаются функции регулярных выражений, возвращающие массивы.

## Session 307 - 2026-04-23 09:12 - массивные функции регулярных выражений

### Что изменилось
- Проверен текущий regex-срез: `REGEXISMATCH`, `REGEXMATCH`, `REGEXEXTRACT`, `REGEXREPLACE` и `REGEXCOUNT` уже были реализованы, а `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` оставались только зарезервированными в справочнике.
- Реализованы новые встроенные функции:
  - `REGEXMATCHES(text, pattern)` и `REGEXMATCHES(text, pattern, modes)`;
  - `REGEXEXTRACTALL(text, pattern, group)` и `REGEXEXTRACTALL(text, pattern, group, modes)`;
  - `REGEXSPLIT(text, pattern)` и `REGEXSPLIT(text, pattern, modes)`.
- Все три функции возвращают новый массив `STRING()` с компактными границами `1 TO N`; пустой результат возвращается как выделенный массив `1 TO 0`.
- `REGEXEXTRACTALL` использует те же правила группы, что и `REGEXEXTRACT`: группа `0` означает всё совпадение, отрицательная группа даёт `MB3007`, отсутствующая группа в отдельном совпадении даёт пустую строку в соответствующем элементе.
- Для `REGEXSPLIT` зафиксировано сохранение пустых частей при совпадениях в начале, в конце или рядом друг с другом; пустой исходный текст возвращает пустой массив.
- Решение по именам зафиксировано в спецификации: QuickBASIC не даёт готовой regex-линии, Visual Basic исторически имеет `Like`, а текущие имена оставлены по современной линии операций `Regex.Match`, `Regex.Matches`, `Regex.Split`, с `REGEXEXTRACTALL` как парой к `REGEXEXTRACT`.
- Обновлены справочник, алфавитный указатель, обзор массивов и подсветка IDE.
- Добавлены ручные `.axm`-примеры для новых функций.

### Какие файлы изменены
- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.Regex.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.cs`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.Regex.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`
- `tests/Axiom.Semantics.Tests/BinderTests.cs`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `samples/functions/regexmatches/all_matches.axm`
- `samples/functions/regexextractall/group_values.axm`
- `samples/functions/regexextractall/negative_group_error.axm`
- `samples/functions/regexsplit/split_on_pattern.axm`
- `docs/manual/regex-functions.html`
- `docs/manual/arrays-and-builtins.html`
- `docs/manual/scripts/manual.js`
- `docs/SPECIFICATION.md`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore --filter "FullyQualifiedName~BuiltinRegex" -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~BuiltinRegex" -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore --filter "FullyQualifiedName~Regex" -p:UseSharedCompilation=false`
- `dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-restore --filter "FullyQualifiedName~Highlighting_VocabularyCoversAllBuiltInFunctions" -p:UseSharedCompilation=false`
- `node --check docs/manual/scripts/manual.js`
- Проверка ссылок на `.axm` из `docs/manual/regex-functions.html` и `docs/manual/arrays-and-builtins.html` не нашла отсутствующих файлов.
- Полный прогон `dotnet test tests/Axiom.Semantics.Tests/Axiom.Semantics.Tests.csproj --no-restore -p:UseSharedCompilation=false` прошёл: 220 тестов.
- Полный прогон `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore -p:UseSharedCompilation=false` прошёл: 371 тест.
- Полный прогон `dotnet test tests/Axiom.Execution.Tests/Axiom.Execution.Tests.csproj --no-restore -p:UseSharedCompilation=false` прошёл: 184 теста.

### Решения
- `REGEXMATCHES`, `REGEXEXTRACTALL` и `REGEXSPLIT` больше не считаются будущими функциями справочника; это действующие встроенные функции.
- Новые функции относятся к строковому regex-разделу, но их результаты полностью встроены в модель одномерных массивов.
- Параллельный запуск нескольких тестовых проектов может временно блокировать общий `Axiom.Semantics.dll` через `VBCSCompiler`; для последовательных проверок использован `-p:UseSharedCompilation=false`.

### Следующий шаг
- Вручную открыть в `Axiom.Ide` новые примеры `samples/functions/regexmatches/all_matches.axm`, `samples/functions/regexextractall/group_values.axm`, `samples/functions/regexsplit/split_on_pattern.axm` и подтвердить ожидаемый вывод.
- После ручной проверки выбрать следующий языковой срез: продолжить текстовые функции вокруг регулярных выражений или перейти к следующему практичному разделу справочника после аудита массивов.

## Session 308 - 2026-04-23 09:32 - обнаружение невосстановимых маркеров кодировки

### Что изменилось
- Разобран случай повреждения вида `пїЅпїЅпїЅ...` в `docs/WORKLOG2.md`.
- Уточнено поведение служебного примера `samples/smoke/repair_plans_mojibake.axm`: восстановимые фрагменты mojibake он продолжает чинить, а строки с `пїЅ` или `�` теперь считает отдельно как потенциально невосстановимые.
- В вывод примера добавлено третье значение: число строк, где найден символ замены или его mojibake-вид.
- Добавлена автоматическая проверка, что такой маркер не преобразуется в случайный текст и не маскируется как успешно исправленная строка.

### Какие файлы изменены
- `samples/smoke/repair_plans_mojibake.axm`
- `tests/Axiom.Runtime.Tests/InterpreterTests.cs`
- `docs/PLANS.md`
- `docs/WORKLOG2.md`

### Проверка
- `dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-restore --filter "FullyQualifiedName~SmokeSample" -p:UseSharedCompilation=false`
- Проверка прошла: 6 тестов.

### Решения
- Фрагменты вида `План` остаются восстановимыми, потому что исходные UTF-8-байты ещё представлены ошибочно прочитанными символами Windows-1251.
- Фрагменты `пїЅ` и `�` считать потерей исходных байтов: это символ замены `U+FFFD` или его mojibake-форма, поэтому алгоритмически восстановить исходный русский текст уже нельзя без внешней копии или ручного восстановления по смыслу.
- Правильное поведение утилиты в таком случае: обнаружить проблему, посчитать затронутые строки и не выдавать это за успешный ремонт.

### Следующий шаг
- Вручную открыть `samples/smoke/repair_plans_mojibake.axm` в `Axiom.Ide`, запустить на рабочей копии `docs/WORKLOG2.md` и посмотреть третье число в выводе.
- Для строк, где остались `пїЅ` или `�`, искать резервную копию, историю изменений или восстанавливать текст по ближайшему контексту.

