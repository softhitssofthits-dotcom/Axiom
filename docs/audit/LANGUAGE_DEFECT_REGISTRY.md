# Реестр дефектов аудита языка Axiom (2026-10-06)

Полный отчёт: `docs/audit/LANGUAGE_FUNCTIONS_FULL_AUDIT.md`. Матрица функций: `docs/audit/LANGUAGE_FUNCTION_MATRIX.md`.
Воспроизводимые кейсы: `.artifacts/audit/cases/*` и протоколы `.artifacts/audit/out/*` (временные артефакты; ключевые репродьюсеры продублированы ниже).

Формат: ID | Severity | Category | Файл/функция | Репродьюсер | Ожидание | Факт (I=Interpreter, V=BytecodeVm) | Рекомендация | Confidence.

---

## AXD-001 — P1 — DEFECT (исполнение) — ИСПРАВЛЕН 2026-10-06 (Wave 0.1)

- Файл: `src/Axiom.Runtime/Execution/Interpreter.Statements.cs:591-628` (`ExecuteTry`).
- Репродьюсер:
  ```axiom
  TRY
      TRY
          ERROR "MB1111"
      CATCH inner
          ERROR inner
      FINALLY
          PRINT "inner-finally"
      END TRY
  CATCH outer
      PRINT "caught"
  END TRY
  ```
- Ожидание (по SPECIFICATION3 «FINALLY выполняется … перед выходом с необработанной ошибкой»): вывод `inner-finally` затем `caught`.
- Факт: I: `caught` (FINALLY пропущен). V: `inner-finally\ncaught` (корректно).
- Причина: исключение, брошенное из тела `CATCH` (строка 611), покидает `ExecuteTry` до вызова `ExecuteFinally` (строка 615).
- Фикс: исполнять `ExecuteFinally` в `finally`-блоке C# или перехватывать исключение CATCH-тела. Тест: rethrow-путь в обоих движках.
- Confidence: CONFIRMED.
- Статус: исправлен в `Interpreter.Statements.cs` (ExecuteTry): ошибка из тела CATCH захватывается, FINALLY исполняется, затем выброс. Тесты: `InterpreterTests.TryCatch` (2 новых), `ExecutionPipelineTests` (2 новых VM-паритета), sample `samples/operators/try/rethrow_from_catch_runs_finally.axm`; manual `try.html` и SPECIFICATION3 дополнены.

## AXD-002 — P1 — SEMANTIC DEFECT (молчаливая потеря данных)

- Файл: парсер expression-стейтментов (`src/Axiom.Parsing/`); проявление — любые builtin-вызовы в позиции оператора без скобок.
- Репродьюсер:
  ```axiom
  DIM f AS FILE
  OPEN p FOR OUTPUT AS f
  WRITELINE f "текст"   ' молча не пишет
  FLUSH f                ' молча не выполняется
  REWIND f               ' молча не выполняется
  CLOSE f
  PRINT READFILE(p)      ' пусто
  ```
- Ожидание: диагностика (вызов builtin-функции оператором без скобок невозможен) или исполнение.
- Факт: оба движка молча выбрасывают выражения (`5`, `x`, `ABS(-5)`, `FLUSH f`, `WRITELINE f "x"` — валидные no-op). Пользователь теряет данные без предупреждения. NB: `CLOSE f`/`OPEN … AS f` — операторы со своей грамматикой, работают; SUB `Name arg` даёт MB2019; несогласованность реакций.
- Фикс: правило «builtin-имя в начале оператора без `=`/без скобок → диагностика» (или предупреждение). Тест: `FLUSH f`, `WRITELINE f "x"`, `REWIND f` должны диагноститься.
- Confidence: CONFIRMED.
- Статус: исправлен 2026-10-06 (Wave 0.2): новая семантическая диагностика `MB2040` — оператор-выражение обязан быть вызовом; `FLUSH f` даёт две MB2040 вместо молчаливого no-op. Тесты в `BinderTests`, негативный sample `samples/acceptance/negative/expression_statement_no_effect.axm`, строка в manual `diagnostics.html`, раздел в SPECIFICATION3.

## AXD-003 — P2 — DEFECT (mojibake в диагностике)

- Файл: `src/Axiom.Runtime/Execution/Interpreter.Builtins.FileSystem.cs:448, 567, 655, 872, 894, 915, 962` (7 строк).
- Репродьюсер: `RENAMEFILE("C:\путь\файл.txt", "C:\путь\имя.txt")` в Interpreter.
- Ожидание: «аргумент 'newName' должен быть простым именем без пути.» (как в VM через общий helper).
- Факт (I): «аргумент 'newName' РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїСЂРѕСЃС‚С‹Рј РёРјРµРЅРµРј Р±РµР· РїСѓС‚Рё.» — двойная кодировка UTF-8→CP1251 прямо в исходнике.
- Фикс: перекодировать 7 строковых литералов; регрессионный тест на русские тексты этих валидаций. V — корректен.
- Confidence: CONFIRMED (исходник + live).
- Статус: исправлен 2026-10-06 (Wave 1.4): все 8 mojibake-литералов перекодированы (включая обнаруженный при исправлении DIRSIZE-текст, которого не было в первоначальном списке из 7); тексты Interpreter теперь совпадают с VM; регрессионный тест `Execute_RenameFileWithPathArgumentReportsReadableRussianDiagnostic`.

## AXD-004 — P2 — SEMANTIC DEFECT (кодировочный контракт) — ИСПРАВЛЕН 2026-10-06 (Wave 1.5)

- Файлы: `src/Axiom.Runtime/Builtins/BuiltinFileSystemHelpers.cs:26-38` (`ReadTextFileWithFallback`) против `src/Axiom.Runtime/Values/RuntimeTextFile.cs` (lenient UTF-8).
- Репродьюсер (`.artifacts/audit/cases/enc.axm` + файлы bom.txt/cp1251.txt):
  - UTF-8+BOM: `READFILE` → BOM вырезан; `OPEN FOR INPUT` + `READTOEND` → LEN=5, **U+FEFF остаётся в потоке** (первый READLINE начинается с невидимого символа).
  - Windows-1251 («тест»): `READFILE` → «тест» (fallback); `OPEN FOR INPUT` → **runtime-ошибка** «не удалось прочитать текст как UTF-8».
- Ожидание: единый контракт чтения для обоих API.
- Фикс: единый helper декодирования (BOM-детект + общий fallback), зафиксировать контракт в SPECIFICATION3/manual. Оба движка ведут себя одинаково (общий код) — расхождение между API, не между движками.
- Confidence: CONFIRMED (эмпирически).
- Статус: исправлен (Wave 1.5): `RuntimeTextFile` при OPEN INPUT проверяет файл строгим UTF-8 (с пропуском BOM), при невалидном UTF-8 читает как Windows-1251 — тот же контракт, что у READFILE; FILEPOS/FILELENGTH/SEEKFILE/REWIND работают в содержательных байтах (BOM невидим). Тесты: `Execute_OpenInputStripsUtf8Bom`, `Execute_OpenInputFallsBackToWindows1251`.

## AXD-005 — P2 — API INCONSISTENCY (тексты диагностик I↔VM) — ИСПРАВЛЕН 2026-10-06 (Wave 1.1)

Подтверждённые пары (код одинаковый, тексты разные):

| Случай | Interpreter | BytecodeVm |
|---|---|---|
| `CLAMP(5,10,1)` | «аргумент 'high' must be greater than or equal to low.» (англ.) | «аргумент 'min' не должен быть больше аргумента 'max'.» (рус.) |
| `FOR … STEP 0` | «В цикле FOR для переменной 'i' недопустим STEP 0.» | «В цикле FOR недопустим STEP 0.» |
| `DIM a(5 TO 1)` (MB3005) | «…имеет недопустимые границы. В каждом измерении…» | «…имеет неверные границы. Для каждого измерения…» |
| `LBOUND(a, 2)` на 1D | «аргумент 'dimension' должен быть в диапазоне от 1 до 1.» | «…должен быть целым числом от 1 до 1.» |
| `-(-2147483648)` (MB3008) | «Результат операции унарного '-'…» | «Результат унарного '-'…» |
| `ABS(-2147483648)` (MB3008) | «Result of ABS(value) выходит за диапазон…» (англ. фрагмент) | «Результат ABS выходит за диапазон…» |

- Причина: дублирование текстов валидации per-engine (общие helpers есть не везде).
- Фикс: единые сообщения в общих helper'ах; паритет-тесты текстов. Поймано также sweep'ом samples (`for_step_zero.axm`, `clamp/invalid_bounds_error.axm`).
- Confidence: CONFIRMED (6+ пар).
- Статус: исправлен (Wave 1.1): все шесть пар унифицированы (CLAMP — русский текст с именами дескриптора low/high; STEP 0 — краткий текст; MB3005 — точная формулировка Interpreter; LBOUND/UBOUND-dimension — формулировка Interpreter; унарный минус — стиль «Результат операции …»; ABS — «Результат ABS»). Паритет-тест: `Run_UnifiedDiagnosticTextsMatchInBothEngines`.

## AXD-006 — P2 — API INCONSISTENCY (локализации диагностик) — ИСПРАВЛЕН 2026-10-06 (Wave 1.2)

- Для всех проверок аргументов builtin (MB3007/MB3008): Interpreter указывает координаты **аргумента**, VM — **имени вызова** (пример: `PRINT SQR(-1)`: I → 1:11, V → 1:7). Не влияет на код/текст; нарушает предсказуемость IDE-перехода и паритет.
- Фикс: зафиксировать в spec один вариант (рекомендация: аргумент — информативнее) и синхронизировать.
- Confidence: CONFIRMED (систематически, ~все MB3007-кейсы батарей).
- Статус: исправлен (Wave 1.2) с обратным решением: эталоном выбрана локация ВЫЗОВА (вариант VM) — пер-аргументные локации VM недостижимы без изменения lowering (аргументы исполняются предыдущими инструкциями, fast-paths их не размечают), а текст диагностике имя аргумента и так называет. `GetBuiltInArgumentLocation` в Interpreter теперь возвращает локацию вызова; обновлены ожидания локаций в `InterpreterTests`.

## AXD-007 — P2 — API INCONSISTENCY (цепочки вызовов) — ИСПРАВЛЕН 2026-10-06 (Wave 1.3 + Session 1105)

- TYPE-инвариант: I добавляет «Цепочка вызовов: процедура 'Positive.New'…», V — без цепочки.
- `nested_runtime_diagnostic.axm`: I: «локальная функция 'Inner' … объявлена внутри функции 'Outer'»; V: плоское «процедура 'Inner'» + лишний кадр «процедура '<global>'».
- `maze_chase.axm`/`snake_arrow_controls.axm`: V добавляет кадр `<global>`, I — нет.
- Фикс: единый рендерер цепочки (общий helper), единый набор кадров, единые именования «функция/процедура/локальная функция».
- Confidence: CONFIRMED (4+ примера из поставки).
- Статус: в основном исправлен (Wave 1.3): общий форматтер `RuntimeCallChainFormatter`; VM использует `BytecodeRoutine.Symbol` и теперь именует локальные функции как Interpreter («локальная функция 'Inner', объявлена внутри функции 'Outer'»); кадр `<global>` в цепочку больше не входит ни в одном движке. Проверено дифференциально: nested_runtime_diagnostic, snake_arrow_controls — SAME. Остаток закрыт (Session 1105): эмиттер передаёт идентификатор процедуры конструктора вторым операндом `ValidateTypeInvariant` (lowering → emission → валидатор допускает 1-2 операнда), VM синтезирует кадр конструктора в цепочке; тип-инвариантный sample теперь SAME; паритет-тест `Run_TypeInvariantFailureReportsConstructorCallChainInBothEngines`.

## AXD-008 — P2 — SEMANTIC DEFECT (категория ошибки `^`) — ИСПРАВЛЕН 2026-10-06 (Wave 1.6)

- Репродьюсер: `PRINT (-8) ^ (1.0 / 3.0)`.
- Ожидание: ошибка домена (как у `POW(-8, 0.333…)` → MB3007 «аргумент 'value' cannot be used with the specified exponent»).
- Факт: MB3008 «Результат операции '^' выходит за диапазон DOUBLE» — вводит в заблуждение (результат не «за диапазоном», а не существует).
- Фикс: доменная диагностика для `^` с отрицательным основанием и нецелой степенью. I и V одинаковы.
- Confidence: CONFIRMED.
- Статус: исправлен (Wave 1.6): новый код `MB3012` NumericDomain и общий helper `RuntimeArithmeticHelpers.EvaluatePowerOperation` в обоих движках; текст «Оператор '^' не определён для отрицательного основания и нецелой степени.»; POW сохраняет свою MB3007. Паритет-тест: `Run_PowerDomainErrorMatchesInBothEngines`.

## AXD-020a — P2 — API INCONSISTENCY (функция без RETURN)

- Репродьюсер: `FUNCTION NoRet() AS INTEGER / PRINT "side" / END FUNCTION` + вызов.
- Факт: I — MB3006 «Функция 'NoRet' завершилась без RETURN со значением» + печатает «side» (тело успевает выполниться) + цепочка вызовов; V — BC4054 «Инструкция Return … ожидает высоту стека 1» до вывода тела.
- Фикс: единый код (рекомендация MB3006) и единая точка обнаружения; паритет-тест.
- Confidence: CONFIRMED.

## AXD-009 — P3 — SEMANTIC DEFECT (ассоциативность `^`) — СНЯТ 2026-10-07 (ошибка аудита)

- `2 ^ 3 ^ 2 = 512` (правая), QB/VB — левая (`(2^3)^2 = 64`).
- Confidence: CONFIRMED (поведение), НО документационная часть находки оказалась ложной: правая ассоциативность уже была задокументирована в `docs/manual/arithmetic-and-logic.html` («вычисляется справа налево: 2 ^ 3 ^ 2 вычисляется как 2 ^ (3 ^ 2)»); аудит не проверил эту страницу. Остаётся только отличие от QB/VB — осознанное документированное решение, изменения не требуются.

## AXD-010 — P3 — API INCONSISTENCY (язык сообщений)

- Numeric-helpers — английские тексты («must be greater than or equal to 0», «Result of …»), string/file — русские. Единый стиль отсутствует; продукт русскоязычный.
- Фикс: единый словарь сообщений (предположительно русский), пройтись по всем helper'ам.
- Confidence: CONFIRMED.

## AXD-011 — P3 — SEMANTIC DEFECT (смешанная Unicode-модель)

- `LEN("😀")=2` (code units), но `ASC("😀")=128512` (scalar) и `REVERSE("😀😎")="😎😀"` (пары сохраняются); при этом `LEFT("a😀b",2)`, `INSERT("😀",2,"x")`, `REMOVE("a😀b",2,1)`, `PADLEFT("😀",3,"😀")` возвращают строки с разорванными суррогатными парами.
- Фикс: зафиксировать в spec договор «UTF-16 code units» для LEN/индексации, пометить в manual функции, чувствительные к non-BMP; либо перейти на scalar-единицы согласованно.
- Confidence: CONFIRMED.

## AXD-012 — P3 — SEMANTIC DEFECT (VAL)

- `VAL("1,5")=15` (запятая игнорируется как разделитель тысяч — сюрприз для ru-локали), `VAL("&HFF")=0` (QB: 255), `VAL("1e2")=100` (экспонента есть в VAL, нет в литералах языка).
- Фикс: зафиксировать контракт VAL в spec/manual; рассмотреть отвержение запятой внутри числа.
- Confidence: CONFIRMED.

## AXD-013 — P3 — API INCONSISTENCY (LEFT/RIGHT/MID count=0)

- `LEFT(s, 0)` → MB3007 «должен быть больше 0»; в QB — пустая строка. Пустой префикс невозможно получить без TRY.
- Рекомендация: принять текущий контракт (0 запрещён) и отразить в manual явно; или разрешить 0.
- Confidence: CONFIRMED.

## AXD-014 — P3 — API INCONSISTENCY (локализация дат) — ЧАСТИЧНО СНЯТ 2026-10-07 (ошибка аудита)

- `MONTHNAME(1)="January"`, `WEEKDAYNAME(1)="Monday"` — invariant-английские; `WEEKDAY` — Monday-first (VB: Sunday-first).
- Confidence: CONFIRMED (поведение), НО пункт «нумерация упомянута только в тексте ошибки» был ложным: Monday-first схема `1 = Monday, 7 = Sunday` уже задокументирована на странице `date-time-functions-parts-and-serial.html`. Остаётся только вопрос локализации английских имён — решение за пользователем (Wave 4/5).

## AXD-015 — P3 — ARCHITECTURE DEFECT (дублирование движков) — ЧАСТИЧНО ИСПРАВЛЕН 2026-10-06 (Session 1105)

- Полный дубль switch в `BytecodeVm.Builtins.cs` (нужен только для 15 variadic-ID); `ToInt32Checked` ×2; `EvaluateIntBuiltin` в строковом файле Interpreter; параллельные Graphics/Keyboard/Mouse-файлы; дубли текстов валидации (корень AXD-005).
- Рекомендация: свести к «тонкий движок + общий helper» (образец — FileIO). Confidence: CONFIRMED (исходники).
- Статус: частично исправлен (Session 1105): фолбэк-свитч VM сокращён до 15 живых variadic-ID (остальные case были недостижимы); `ToInt32Checked` сведён к общим перегрузкам в `RuntimeArithmeticHelpers` (движковые методы делегируют); `EvaluateIntBuiltin` перенесён из `Interpreter.Builtins.String.cs` в `Interpreter.Builtins.Numeric.cs`. Осталось: параллельные Graphics/Keyboard/Mouse-файлы и хвосты дублей валидации — по мере касания соответствующих подсистем.

## AXD-016 — P3 — IDE GAP (подсветка без синхронизации) — ИСПРАВЛЕН 2026-10-07 (Session 1106)

- BuiltIn-слова в `Axiom.xshd`/`Axiom.Dark.xshd` — вручную (генератор `Generate-XshdKeywords.ps1` не охватывал BuiltIn-секцию); третья копия — `Axiom.Formatting/TokenClassifier.cs` (`AxiomFormattingVocabulary.BuiltIns`). Реестр `BuiltinFunctions` IDE не потребляется.
- Рекомендация: генерация BuiltIn-секций из реестра + тест полноты. Confidence: CONFIRMED.
- Статус: исправлен (Session 1106): генератор дополнен BuiltIn-секцией из `axiom metadata` (реестр BuiltinFunctions) и режимом `-Write`; рассинхрон, уже накопившийся в xshd (в BuiltIn отсутствовала STRING), устранён прогоном генератора; добавлены sync-тесты `HighlightingVocabularySyncTests` (светлая/тёмная схема и словарь форматирования обязаны совпадать с реестром).

## AXD-017 — P3 — TEST GAP — ЧАСТИЧНО ЗАКРЫТ 2026-10-07 (Session 1106)

- ~45 filesystem builtins с 1-4 упоминаниями в тестах; отсутствие паритет-тестов текстов/локализаций/цепочек; non-BMP-строки не покрыты; кодировочные тесты только для READFILE-пути.
- Confidence: CONFIRMED.
- Статус: частично закрыт (Sessions 1104-1106): паритет-тесты текстов/локализаций/цепочек добавлены в `ExecutionPipelineTests`; non-BMP-контракт строк зафиксирован `InterpreterTests.StringNonBmp` (UTF-16 units, скаляры ASC/CHR, паросохраняющий REVERSE); кодировки OPEN INPUT — `InterpreterTests.FileIO`; тонкие filesystem builtins (DRIVE*, FILEDATE, DIRSIZE, FREESPACE, temp-функции) — `InterpreterTests.FileSystemDiagnostics` + фиксирующие тесты читаемых русских текстов всех исправленных mojibake-валидаций. Осталось: полный per-function разброс по всем ~45 (текущее покрытие — smoke-уровень).

## AXD-018 — P3 — MISSING ESSENTIAL/PLANNED (языковые пробелы) — ЧАСТИЧНО СНЯТ 2026-10-07 (ошибка аудита)

- Нет: экспоненциальной записи литералов, суффиксов типов, `&O`/`&B`, esc-последовательностей (спецсимволы только через CHR), RANDOMIZE, AndAlso/OrElse, CINT/CLNG/CDBL/CDEC/CSTR, binary I/O (GET/PUT/OPEN FOR BINARY), DATA/READ/RESTORE (запланировано), LINE INPUT (запланировано), массивных литералов/FOR EACH/MAP/лямбды (запланировано в FUTURE.md).
- Классификация: запланированное — PLANNED FEATURE; остальное — MISSING ESSENTIAL (кодировка/binary-чтение) или OPTIONAL (суффиксы, esc).
- Confidence: CONFIRMED с поправкой 2026-10-07: пункт «SHL/SHR отсутствуют» был ложным — операторы сдвига в языке существуют под именами `<<` и `>>` (проверено исполнением: `1 << 3 = 8`, `16 >> 2 = 4` в обоих движках; приоритет отражён в `arithmetic-and-logic.html`); аудит сопоставил имена, а не операторы. Выбор кодировки при OPEN закрыт осознанно единым детектором (Wave 1.5).

## AXD-019 — P4 — SAMPLES

- Неидемпотентные примеры: `renamefile`, `renamedir`, `fileexists`, `pathexists`, `movedir` (фиксированные имена файлов; первый и повторный запуск дают разные выводы). `filepath/basic_usage` недетерминирован для дифференциальных прогонов (случайные temp-имена).
- Confidence: CONFIRMED.

## AXD-020 — P4 — DOCUMENTATION GAP — ЗАКРЫТ 2026-10-07 (Session 1107)

- Не отражены в manual/spec: ассоциативность `^`; WEEKDAY Monday-first; контракт VAL; Unicode-единицы строк; контракт кодировок READFILE/OPEN и BOM; LF-концы строк NEWLINE; отсутствие esc-последовательностей; строковая форма `ERROR "код"` (не числовая как QB); отказ от QB-идиомы `FuncName = value`; отсутствие `:`-разделителя операторов; LF-формат чисел PRINT.
- Confidence: CONFIRMED.
- Статус: закрыт (Session 1107): LF-концы строк добавлены в `file-system-functions-open-files.html` (WRITELINE/NEWLINE); Unicode-единицы — в `string-functions-slices-and-length.html` (LEN); правила чтения VAL — в `numeric-functions-conversion-and-legacy.html`; кодировочный контракт READFILE/OPEN — в SPECIFICATION3 (раздел Wave 1, Session 1104); ассоциативность `^` и WEEKDAY Monday-first оказались уже задокументированными (см. AXD-009/AXD-014); строковая форма ERROR документирована на странице `error.html`; esc-последовательности, `FuncName = value` и `:`-разделитель зафиксированы в SPECIFICATION3 настоящим датированным разделом.

---

## Не-дефекты, зафиксированные как контракты (чтобы не «переоткрывать»)

- `ROUND` — половины от нуля (задокументировано в manual — отличие от VB banker's осознанное).
- `/0` для DOUBLE — runtime-ошибка (Infinity недостижим по дизайну).
- `\`/`MOD` — только целые операнды (нужен `INT()`); сужения запрещены entirely.
- Multi-value `INPUT` — разделитель запятая (QB-семантика).
- `DATEVALUE/TIMEVALUE` — строгий канонический ISO-формат.
- RANDOMIZE отсутствует (кандидат SHOULD).
- Массивы: копия при присваивании/ARRAYCLONE, разделяемое тело при передаче в процедуру (задокументировано в spec).
- `RANDOMIZE`-независимость RND: seed управляется хостом.
- Отсутствие `GOTO`/меток и `:`-разделителя — принципиальная построчность языка.


## AXD-021 — P4 — API INCONSISTENCY (текст невыделенного массива) — ОТКРЫТ 2026-10-07 (Wave 4)

- Файлы: `Interpreter.Builtins.Array.cs` / `BytecodeVm.Builtins.Array.cs` (RequireAllocatedArrayArgument).
- Репродьюсер: `DIM u() AS INTEGER` + `PRINT ARRAYSUM(u)`.
- Ожидание: одинаковый текст в обоих движках (код MB3005 совпадает).
- Факт: Interpreter — «Функция ARRAYSUM: аргумент 'array' содержит массив, который ещё не выделен.»; VM — «Массив 'array' объявлен, но ещё не выделен.»
- Confidence: CONFIRMED. Хвост семейства AXD-005, всплыл при тестировании агрегатов Wave 4.
