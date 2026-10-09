# Сквозная карта функций и хвостов Axiom

Единая таблица-источник: связывает каждую функцию или направление языка с фактическим состоянием, файлами-источниками и незакрытыми хвостами. Цель — не искать истину по разрозненным `FUTURE.md`, `PLANS*`, `audit/*` и `SPECIFICATION*`, а смотреть сюда.

Состояния:
- **реализовано** — есть в коде, тестах, примерах и (для пользовательских сущностей) в manual;
- **частично** — реализовано в одном движке или ограниченным срезом;
- **запланировано** — описано в плане/договоре, в коде ещё нет;
- **требует подтверждения** — статус по документам противоречив, нужна сверка с кодом.

Сверка выполнена 2026-08-26 по лексеру (`src/Axiom.Lexing`), реестру `docs/LANGUAGE_INVENTORY.md`, `docs/PLANS4.md`, `docs/WORKLOG5.md` и `docs/audit/`.

## 1. Ядро языка и исполнение

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| Лексер, парсер, AST, семантика (binder) | реализовано | `src/Axiom.Lexing/Parsing/Syntax/Semantics` | — |
| Интерпретатор (AST-walking) | реализовано | `src/Axiom.Runtime` | эталонный режим для сверки |
| BytecodeVm (байткод + VM) | реализовано | `src/Axiom.Bytecode`, `Axiom.Runtime` | режим по умолчанию; полный паритет достигнут для покрытого среза |
| Единый путь parse→bind→execute | реализовано | `src/Axiom.Execution` | — |
| Режимы исполнения Bytecode/Interpreter | реализовано | `Axiom.Execution` | режим `Auto` удалён 2026-04-25 |

## 2. Типы данных и значения

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| Скалярные типы (INTEGER/LONG/DOUBLE/DECIMAL/STRING/BOOLEAN) | реализовано | `docs/LANGUAGE_INVENTORY.md` | — |
| Нативные DATE/TIME/DATETIME | реализовано | `docs/PLANS4.md` (DT-004) | reserved enum-иды строковых форм — удалить при следующем несовместимом изменении байткода |
| Одномерные массивы, REDIM/ERASE | реализовано | `docs/audit/ARRAY_SEMANTICS.md` | — |
| Пользовательские TYPE (записи) | реализовано | `docs/audit/TYPE_USER_DEFINED_TYPES_FORENSIC_AUDIT.md` | — |
| TYPE-методы и конструкторы (`SELF`, `New`) | реализовано | `src/Axiom.Semantics` (138 вхождений) | — |
| Значения-функции и замыкания | реализовано | `docs/audit/BYREF_FUNCTION_VALUES.md`, `SUB_FUNCTION_CALL_MODEL.md` | — |
| Литералы массивов `{}` | запланировано | `docs/FUTURE.md` §1 | нет токенов `{`/`}` в лексере |
| Словари/MAP | запланировано | `docs/FUTURE.md` §4 | нет токенов |
| Многострочные строки `"""` | запланировано | `docs/FUTURE.md` §3 | нет тройных кавычек в лексере |

## 3. Управление потоком

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| IF/ELSEIF/ELSE, SELECT CASE | реализовано | `docs/audit/IF_ELSE_SELECT_CASE.md` | — |
| FOR/NEXT, WHILE, DO..LOOP (5 форм) | реализовано | `docs/audit/FOR_WHILE_DO_LOOP.md` | — |
| EXIT FOR/DO, CONTINUE FOR/WHILE/DO | реализовано | `docs/PLANS4.md` | — |
| TRY/CATCH/FINALLY, ERROR | реализовано | `docs/audit/ERROR_HANDLING.md` | — |
| FOR EACH | запланировано | `docs/FUTURE.md` §2 | нет ключевых слов `EACH`/`IN` |
| Сопоставление с образцом в SELECT | запланировано | `docs/FUTURE.md` §8 | — |

## 4. Процедуры

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| SUB/FUNCTION/RETURN, BYVAL/BYREF | реализовано | `docs/audit/SUB_FUNCTION_CALL_MODEL.md` | — |
| Локальные SUB/FUNCTION, рекурсия | реализовано | там же | — |
| Защита глубины стека (MB3011) | реализовано | `docs/PLANS4.md` (CALL-001) | — |
| Необязательные параметры (`= значение`) | запланировано | `docs/FUTURE.md` §6 | нет разбора значений по умолчанию |
| Лямбда-выражения | запланировано | `docs/FUTURE.md` §7 | нет токенов `=>` |

## 5. Встроенные функции (BuiltinFunctionId)

| Группа | Состояние | Источники |
| --- | --- | --- |
| Строковые (~44) | реализовано | `docs/audit/STRING_OPERATIONS.md` |
| Числовые и математические | реализовано | `docs/audit/NUMERIC_MATH_FORENSIC_AUDIT.md` |
| Дата и время | реализовано | `docs/audit/DATETIME_FORENSIC_AUDIT.md` |
| Массивы | реализовано | `docs/audit/ARRAY_SEMANTICS.md` |
| Регулярные выражения | реализовано | `docs/audit/REGEX.md` |
| Файловая система и файлы | реализовано | `docs/audit/FILE_SYSTEM.md` |
| Единый слой built-in (общий хелпер-слой) | реализовано | `docs/archive/audits-legacy/IMPROVEMENT_PLAN.md` волна 2 | дублирование Interpreter/VM устранено (−2 870 строк) |

## 6. Ввод-вывод

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| PRINT, PRINT USING, INPUT | реализовано | `docs/audit/INPUT_PRINT_DATA_READ_RESTORE_FORENSIC_AUDIT.md` | — |
| Интерполяция строк `$"..."` (+ спецификаторы формата) | реализовано | `docs/PLANS4.md` (2026-08-25) | реализовано в форме `$"..."`, а не `${}` из `FUTURE.md` §5 |
| DATA / READ / RESTORE | запланировано | `docs/audit/DATA_READ_RESTORE.md` | подсистема полностью отсутствует (Health 0/10), есть дорожная карта из 8 этапов |
| LINE INPUT | запланировано | `docs/audit/INPUT_PRINT_...` | есть ключевое слово `LINE`, но нет разбора формы |

## 7. Графика

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| SCREEN-путь (PSET/LINE/CIRCLE/PAINT/GET/PUT/VIEW/WINDOW/PALETTE/PCOPY/PMAP) | реализовано | `docs/audit/GRAPHICS_SCREEN_FORENSIC_AUDIT.md` | — |
| CANVAS-путь (DRAW*/FILL*/BEGINFRAME/PRESENT/CAMERA/VIEWPORT) | реализовано | `docs/audit/GRAPHICS_CANVAS_MODERN_FORENSIC_AUDIT.md` | — |
| IMAGE/DRAWIMAGE, SPRITE/DRAWSPRITE | реализовано | `docs/audit/IMAGE.md`, `SPRITE.md` | — |
| TILESET/TILEMAP/DRAWTILE*/MAP*/SETMAPTILE | реализовано | `docs/audit/TILEMAP_TILESET.md`, `docs/TILEMAP_CONTRACT.md` | — |
| Корректность рендеринга | частично | `docs/audit/GRAPHICS_RENDERING_CORRECTNESS.md` | Этапы 1–3 закрыты; Этапы 4–7 (оптимизации, консолидация helpers, dead code, parity-тесты, бенчмарки) отложены |
| Свойства карты/слоёв/тайлов | запланировано | `docs/PLANS4.md` (TILEMAP_CONTRACT) | следующий шаг по договору тайловой карты |

## 8. События и ввод

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| Клавиатура (INKEY/KEYDOWN/KEYPRESS/ON KEY) | реализовано | `docs/audit/KEYBOARD_INKEY_FORENSIC_AUDIT.md` | — |
| Мышь (MOUSE*/ON MOUSE) | реализовано | `docs/audit/MOUSE_EVENTS_FORENSIC_AUDIT.md` | — |
| Обработчики событий `ON ... DO` | частично | `docs/FUTURE.md` §10 | реализованы `ON KEY`/`ON MOUSE`, но не `ON TIMER` (нет токена) |

## 9. Тайминг и прочее

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| SLEEP, TIMER() | реализовано | `docs/audit/TIMING_SLEEP.md` | — |
| Отладчик (точки останова, шаги) | частично | `docs/audit/DEBUGGER.md`, `src/Axiom.Runtime` (224 вхождения) | абстракция отладчика есть, полный контракт шагов — по аудиту |

## 10. Модульность и проект

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| Однофайловая программа (`.axm`) | реализовано | `docs/audit/MODULES_NAMESPACES_PROGRAM_STRUCTURE.md` | — |
| Многофайловые модули | запланировано | `docs/audit/MULTI_FILE_MODULE_DESIGN.md`, `docs/FUTURE.md` §9 | проектирование есть, реализации нет |
| Namespaces / visibility | запланировано | там же | — |

## 11. Производительность BytecodeVm

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| Быстрые пути Negate/Not/ReferenceEquals | реализовано | `docs/audit/BYTECODE_VM_PERFORMANCE.md` | — |
| Constant folding, dead code elimination | реализовано | `docs/PLANS4.md` (Phase B) | — |
| Строковые inline fast paths | реализовано | Phase C | — |
| Type-specialized stacks (int[]/long[]/double[]…) | запланировано | `docs/audit/TYPE_SPECIALIZED_STACKS.md` | отложено, требует отдельного проектирования |

## 12. IDE

| Функция | Состояние | Источники | Хвосты |
| --- | --- | --- | --- |
| WPF-оболочка, AvalonEdit, панели | реализовано | `docs/audit/IDE_LANGUAGE_INTEGRATION.md` | — |
| Подсветка синтаксиса (обе темы) | реализовано | `src/Axiom.Ide/Editor/Highlighting` | — |
| Форматирование при открытии | реализовано | `docs/PLANS4.md` | — |
| Разбиение бог-объектов | реализовано | `docs/PLANS4.md` волна 3 | — |
| Тесты IDE | частично | `tests/Axiom.Ide.Tests` | 4 ранее существовавших падения (по WORKLOG5) |

---

## Открытые хвосты (сводный список)

1. **DATA / READ / RESTORE** — не реализовано, есть дорожная карта из 8 этапов в `docs/audit/DATA_READ_RESTORE.md`.
2. **Многофайловые модули / namespaces** — только проектирование.
3. **Графический аудит Этапы 4–7** — отложены (оптимизации, консолидация helpers, dead code, parity-тесты, бенчмарки).
4. **4 падения IDE-тестов** — ранее существовавшие, по `docs/WORKLOG5.md`.
5. **Reserved enum-иды** строковых форм DATEADD/DATEDIFF/DATEPART — удалить при следующем несовместимом изменении байткода.
6. **Type-specialized stacks** — отложенная глубокая оптимизация VM.
7. **`FUTURE.md` §5** — интерполяция `${}` реализована как `$"..."`; предложение устарело и требует пометки.
8. **`FUTURE.md` §10** — `ON TIMER` не реализован (только `ON KEY`/`ON MOUSE`).

## Правило сопровождения

- При изменении поведения языка: обновить эту таблицу, `docs/LANGUAGE_INVENTORY.md`, соответствующий `docs/audit/*`, manual и `.axm`-пример — в одном наборе правок.
- Новое «запланировано» фиксируется только после сверки с кодом (см. `docs/meta/CLASSIFICATION.md` §4).

### Уточнение Session 1130 — явная кодировка текстового API

Реализованы четыре дополнительные формы READFILE/READALLLINES/WRITEFILE/APPENDFILE и OPEN ... USING encoding. Строгие отказы записи и неверное выравнивание LINENUMBER переводятся в MB3007. Историческое утверждение выше о четырёх падениях IDE больше не описывает текущую копию: последние общие прогоны проходили полностью. Доказательства текущего изменения — ExplicitTextEncodingTests и EXPLICIT_OPEN_RECHECK_20261008.md; прежние утверждения о полноте не заменяют независимую проверку всех перегрузок.


### Проверка текущего состояния — Session 1202

PLANNED_FEATURE_EVIDENCE_20261009.md и PlannedSyntaxEvidenceTests подтверждают отказ14 предложенных форм в обоих режимах, итог34/34 с ограниченными существующими альтернативами. Пользовательские свойства Tiled MAPPROPERTY/MAPLAYERPROPERTY/MAPTILEPROPERTY/MAPOBJECTPROPERTY пока отсутствуют: существующие размеры, номера и флаги тайлов этот пункт не закрывают. Это планируемые возможности, не автоматически регрессии или обязательство реализовать весь FUTURE. LINE INPUT требует отдельной оценки отличий от INPUT STRING.


### Уточнение LINE INPUT — Session 1203

INPUT_STRING_EVIDENCE_20261009.md: 34 новых случая, с отрицательными формами68/68 в обоих движках. LINE INPUT как синтаксис отсутствует, но INPUT одной STRING уже сохраняет полную логическую строку, включая запятые/кавычки/краевые пробелы/пустой текст. Для этого сценария отдельная сущность не нужна. Несколько целей используют обычное разделение запятыми с обрезкой пробелов; это не CSV с кавычками. Графический ввод и EOF этим набором не проверены.


### Session 1204 — TILE26-001 P2, не исправлено

TILEMAP_METADATA_EVIDENCE_20261009.md: два выполненных случая настоящего загрузчика подтверждают, что пользовательские properties и objects не попадают в модель; неверный тип этих полей также не замечается. Это незавершённая часть утверждённого TILEMAP_CONTRACT и нарушение требования не терять неподдержанные данные молча. Отказы PROPERTY-имён подтверждены отдельно в обоих движках. Полное исправление требует модели/загрузки/функций и сопутствующей документации; никакого исправления Runtime в1204 нет.


### Session 1206 — доступ из языка

21 утверждённая функция чтения свойств/слоёв/объектов/геометрии подключена к обоим движкам. TILEMAP_ACCESS_EVIDENCE_20261009.md: выбранный IDE-набор255/255 с реальным загрузчиком и всеми21 приёмочными примерами; неверные вызовы88/88. Прежние утверждения об отсутствии четырёх PROPERTY-функций больше не описывают текущий код. Данные уже сохраняются и доступны; подробная причина отказа загрузки остаётся отдельным незавершённым пунктом. Полный прогон решения выполняется, общий аудит не завершён.
