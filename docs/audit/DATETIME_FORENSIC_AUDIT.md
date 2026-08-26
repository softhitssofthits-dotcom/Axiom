# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## DATE / TIME ПОДСИСТЕМА

> Версия отчёта: актуальная (после устранения дефектов DT-001…DT-008).
> Предыдущая версия отчёта (описывавшая устаревшую «чисто строковую» модель) сохранена как `DATETIME_FORENSIC_AUDIT_old_version.md`.

---

## 1. Резюме

Подсистема DATE/TIME отвечает за получение текущих даты/времени, сборку и разборку компонентов, календарную арифметику, конвертацию между датой, временем и датой-временем, а также работу с часовыми поясами.

**Модель данных — нативные типы.** В системе типов Axiom существуют отдельные скалярные типы `DATE`, `TIME` и `DATETIME` (`TypeSymbol.Date/Time/DateTime`), хранящиеся как `DateOnly` / `TimeOnly` / `DateTime`. Это ключевые слова языка (`DIM x AS DATE`), они поддерживают операторы сравнения (`=`, `<`, `>`) и неявные преобразования со `STRING`.

**Состав:** 33 уникальные встроенные функции (46 регистраций `BuiltinFunctionId`: перегрузки с часовым поясом + нативные перегрузки арифметики). Все функции реализованы и паритетны в Interpreter и Bytecode VM.

**Состояние:** стабильное. После аудита устранены: дефект корректности со смешанными типами в `DATEDIFF` (DT-001), русские вкрапления в англоязычной диагностике (DT-002), расхождение диагностики Interpreter↔VM (DT-003), дублирование логики между движками (DT-005), пробелы в тестах (DT-008). Завершён DT-004: арифметика переведена на нативные типы, строковые формы `DATEADD`/`DATEDIFF`/`DATEPART` удалены полностью (дескрипторы, документация, примеры).

---

## 2. Что представляет собой эта подсистема

Функциональные группы:

| Группа | Функции |
|---|---|
| Текущие значения | `DATE`, `TIME`, `NOW` (+ перегрузки с `timeZone`) |
| UTC-значения | `UTCDATE`, `UTCTIME`, `UTCNOW` |
| Таймер | `TIMER` — секунды с местной полуночи (DOUBLE, с дробной частью) |
| Конструкторы | `DATESERIAL(year, month, day)`, `TIMESERIAL(hour, minute, second)` |
| Компоненты | `YEAR`, `MONTH`, `MONTHNAME`, `DAY`, `HOUR`, `MINUTE`, `SECOND`, `WEEKDAY`, `WEEKDAYNAME` |
| Арифметика | `DATEADD`, `DATEDIFF`, `DATEPART` |
| Конверсия | `DATEVALUE`, `TIMEVALUE`, `ISDATE` |
| Часовые пояса | `UTCOFFSET` (0/1/2 арг), `TOUTC`, `FROMUTC` (1/2 арг) |
| Календарные | `ISLEAPYEAR`, `DAYSINMONTH`, `STARTOFMONTH`, `ENDOFMONTH`, `STARTOFYEAR`, `ENDOFYEAR` |

Канонические форматы (единственный источник истины — `BuiltinDateTimeHelpers`):
- дата — `yyyy-MM-dd`;
- время — `HH:mm:ss`;
- дата-время — `yyyy-MM-dd HH:mm:ss`.

---

## 3. Полный список возможностей

### 3.1 Текущие значения

| Функция | Результат | Формат |
|---|---|---|
| `DATE()` / `DATE(timeZone)` | `DATE` | `yyyy-MM-dd` |
| `TIME()` / `TIME(timeZone)` | `TIME` | `HH:mm:ss` |
| `NOW()` / `NOW(timeZone)` | `DATETIME` | `yyyy-MM-dd HH:mm:ss` |
| `UTCDATE()` | `DATE` | `yyyy-MM-dd` |
| `UTCTIME()` | `TIME` | `HH:mm:ss` |
| `UTCNOW()` | `DATETIME` | `yyyy-MM-dd HH:mm:ss` |
| `TIMER()` | `DOUBLE` | секунды с местной полуночи |

### 3.2 Конструкторы

| Функция | Параметры | Результат |
|---|---|---|
| `DATESERIAL(year, month, day)` | `INTEGER × 3` | `DATE` |
| `TIMESERIAL(hour, minute, second)` | `INTEGER × 3` | `TIME` |

### 3.3 Компоненты

| Функция | Параметр | Результат |
|---|---|---|
| `YEAR`, `MONTH`, `DAY`, `WEEKDAY` | `DATE` | `INTEGER` |
| `HOUR`, `MINUTE`, `SECOND` | `TIME` | `INTEGER` |
| `MONTHNAME(month)` | `INTEGER` | `STRING` (англ. имя) |
| `WEEKDAYNAME(weekday)` | `INTEGER` | `STRING` (англ. имя) |

`WEEKDAY` нумерует с понедельника: `1` = Monday … `7` = Sunday. `WEEKDAYNAME(1)` = "Monday".

### 3.4 Арифметика

| Функция | Параметры | Результат |
|---|---|---|
| `DATEADD(interval, number, value)` | `STRING, INTEGER, DATE`/`TIME`/`DATETIME` | `DATE`/`TIME`/`DATETIME` (того же вида) |
| `DATEDIFF(interval, fromValue, toValue)` | `STRING, DATE`/`TIME`/`DATETIME` × 2 одного вида | `LONG` |
| `DATEPART(part, value)` | `STRING, DATE`/`TIME`/`DATETIME` | `INTEGER` |

Функции принимают **только нативные значения** `DATE`/`TIME`/`DATETIME` — строковые параметры не поддерживаются (для преобразования строк используйте `DATESERIAL`, `TIMESERIAL`, `DATEVALUE` или `TIMEVALUE`). Смешение временных видов в `DATEDIFF` не проходит проверку типов в Binder — ошибка компиляции `MB2003`.

Токены интервалов: `yyyy` (год), `q` (квартал), `m` (месяц), `d` (день), `h` (час), `n` (минута), `s` (секунда).
Токены частей `DATEPART`: дополнительно `y` (день года) и `w` (день недели).

### 3.5 Конверсия

| Функция | Параметр | Результат |
|---|---|---|
| `DATEVALUE(value)` | `STRING` | `DATE` |
| `TIMEVALUE(value)` | `STRING` | `TIME` |
| `ISDATE(value)` | `STRING` | `BOOLEAN` |

### 3.6 Часовые пояса

| Функция | Результат | Примечание |
|---|---|---|
| `UTCOFFSET()` / `UTCOFFSET(value)` / `UTCOFFSET(value, timeZone)` | `INTEGER` | смещение в минутах |
| `TOUTC(value)` / `TOUTC(value, timeZone)` | `DATETIME` | конвертация в UTC |
| `FROMUTC(value)` / `FROMUTC(value, timeZone)` | `DATETIME` | конвертация из UTC |

Идентификаторы зон: `"UTC"`, `"LOCAL"` или известное хосту имя `TimeZoneInfo`. DST обрабатывается: скачок времени (`IsInvalidTime`) и неоднозначное время (`IsAmbiguousTime`) дают ошибку `MB3007`.

### 3.7 Календарные

| Функция | Параметры | Результат |
|---|---|---|
| `ISLEAPYEAR(year)` | `INTEGER` | `BOOLEAN` |
| `DAYSINMONTH(year, month)` | `INTEGER, INTEGER` | `INTEGER` |
| `STARTOFMONTH(value)` / `ENDOFMONTH(value)` | `DATE` | `DATE` |
| `STARTOFYEAR(value)` / `ENDOFYEAR(value)` | `DATE` | `DATE` |

---

## 4. Модель типов

### 4.1 Нативные типы

| Тип | Хранение | `TypeSymbol` | Ключевое слово | `BytecodeScalarTypeCode` |
|---|---|---|---|---|
| `DATE` | `DateOnly` | `TypeSymbol.Date` | `DATE` | `Date` |
| `TIME` | `TimeOnly` | `TypeSymbol.Time` | `TIME` | `Time` |
| `DATETIME` | `DateTime` | `TypeSymbol.DateTime` | `DATETIME` | `DateTime` |

`RuntimeValue` поддерживает `FromDate/FromTime/FromDateTime` и `AsDate/AsTime/AsDateTime`, а также каноническое строковое представление (используется `PRINT` и преобразованием в строку).

### 4.2 Сравнение

`RuntimeValueHelpers` реализует равенство (`=`) и упорядочивание (`<`, `>`, `<=`, `>=`) для `DATE`/`TIME`/`DATETIME` через `DateOnly.CompareTo` / `TimeOnly.CompareTo` / `DateTime.CompareTo`.

### 4.3 Неявные преобразования

В `Binder.Helpers` разрешены двусторонние преобразования:

- `DATE ↔ STRING`, `TIME ↔ STRING`, `DATETIME ↔ STRING`;
- `DATE ↔ DATETIME`, `TIME ↔ DATETIME`.

**Ограничение (защита от молчаливого усечения):** присваивание полной даты-времени переменной типа `DATE` или `TIME` запрещено на этапе компиляции (`MB2003`) — и для нативного `DATETIME`, и для строкового литерала вида `yyyy-MM-dd HH:mm:ss`. Разрешены только расширяющие переходы `DATE → DATETIME` и `TIME → DATETIME`; явное извлечение части — через `DATEVALUE`/`TIMEVALUE`.

Фактическая конвертация на этапе выполнения — через `BuiltinDateTimeHelpers.TryConvertTemporalValue` (единая точка для Interpreter и VM).

---

## 5. Фактическая архитектура

| Слой | Файл | Роль |
|---|---|---|
| Семантика | `src/Axiom.Semantics/Symbols/Builtins/BuiltinFunctions.DateTime.cs` | 46 дескрипторов (имя, `BuiltinFunctionId`, тип возврата, параметры); строковые дескрипторы арифметики удалены, `DateAdd`/`DateDiff`/`DatePart` сохранены в enum как reserved для совместимости сериализации байткода |
| Общий слой | `src/Axiom.Runtime/Builtins/BuiltinDateTimeHelpers.cs` | Единый источник истины: форматы, токены, парсинг, форматирование, арифметика, валидация, часовые пояса, `EvaluateTimeBasedDiff`, `TryConvertTemporalValue` |
| Interpreter | `src/Axiom.Runtime/Execution/Interpreter.Builtins.DateTime.cs` (+`.Helpers.cs`) | Тонкие обёртки вычисления в AST-режиме |
| Bytecode VM | `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.DateTime.cs` | Тонкие обёртки вычисления в байткод-режиме |
| VM fast-path | `src/Axiom.Runtime/Execution/BytecodeVm.BuiltinDispatch.cs` | Делегаты-диспатч всех 40 функций |
| Часы | `src/Axiom.Runtime/Host/IRuntimeClock.cs` | `GetLocalNow()`, `GetLocalTimeZone()`, `TryResolveTimeZone()` |
| Значения | `src/Axiom.Runtime/Values/RuntimeValue.cs`, `RuntimeValueHelpers.cs` | хранение, сравнение, каноническое представление |

**Single Source of Truth** соблюдён: канонические форматы, токены, имена месяцев/дней недели, парсинг, форматирование, арифметика, валидация диапазонов, разрешение зон и конвертация UTC находятся в `BuiltinDateTimeHelpers`. Interpreter и VM оставляют только тонкие обёртки, различающиеся способом передачи позиции (`BoundCallExpression`+index против `TextLocation`).

---

## 6. Полный путь выполнения

```
Исходный код → Lexer → Parser → Syntax → Binder → BoundProgram
    ↓ (Interpreter)                          ↓ (Bytecode VM)
switch(BuiltinFunctionId)                    BuiltinDispatch (fast-path делегаты)
    ↓                                        ↓
Evaluate*Builtin (тонкая обёртка)            Evaluate*Builtin (тонкая обёртка)
    ↓                                        ↓
        BuiltinDateTimeHelpers (общий слой)
    ↓                                        ↓
RuntimeValue (Date/Time/DateTime)            RuntimeValue (Date/Time/DateTime)
    ↓                                        ↓
Host (IRuntimeClock / IRuntimeHost)          Host (IRuntimeClock / IRuntimeHost)
```

Конвертация `STRING ↔ DATE/TIME/DATETIME` на этапе вычисления — через `TryConvertTemporalValue` в `Interpreter.Expressions.cs` и `BytecodeVm.cs`.

---

## 7. Фактический семантический контракт

### 7.1 Ключевые правила

- `WEEKDAY`/`WEEKDAYNAME` — нумерация с понедельника (`1`=Monday, `7`=Sunday); отлично от .NET `DayOfWeek` (Sunday=0), пересчёт в `GetMondayFirstWeekdayNumber`.
- `TIMER` — секунды с местной полуночи с дробной частью (тест: `14:05:06 → 50706`).
- `UTCOFFSET` — смещение в минутах (тест: зона +3 → `180`).
- `DATEADD("m", 1, DATEVALUE("2026-01-31"))` → `2026-02-28` (конец месяца).
- `DATEADD("s", 10, TIMEVALUE("23:59:55"))` → `00:00:05` (перенос через полночь, заворачивание).
- `DATEDIFF` для time-интервалов требует **одинакового вида** операндов; смешение вида не проходит проверку типов в Binder — ошибка компиляции `MB2003` (защита DT-001 усилена: раньше — только runtime `MB3007`).
- Присваивание полной даты-времени переменной `DATE`/`TIME` — ошибка компиляции `MB2003` («the … component would be silently truncated»); явный путь — `DATEVALUE`/`TIMEVALUE`.
- DST: скачок/неоднозначность времени → `MB3007`.

### 7.2 Ошибки

Ошибки аргументов на этапе выполнения — `MB3007`; сбой конвертации типов в рантайме — `MB3005`. Смешение временных видов в `DATEDIFF` и строковые аргументы арифметики отклоняются в Binder на этапе компиляции (`MB2003`). Ошибки выполнения пробрасываются как `RuntimeFailureException` и перехватываются через `TRY/CATCH`.

---

## 8. Interpreter ↔ Bytecode VM

Полный паритет по всем 33 функциям (результат, ошибки, состояние). Диагностика конвертации типов унифицирована (`Cannot convert type: {from} -> {to}.` в обоих движках). DST-логика после консолидации идёт через один метод-хелпер.

---

## 9. Анализ тестов

- **Interpreter** (`tests/Axiom.Runtime.Tests/InterpreterTests.DateTime.cs`) — happy path, неканонические форматы, диапазоны, DST, границы суток, дробный `TIMER`, смешанный `DATEDIFF`, `DATEADD q/yyyy`, нативные перегрузки `DATEADD`/`DATEDIFF`/`DATEPART`.
- **Bytecode VM** (`tests/Axiom.Runtime.Tests/BytecodeVmTests.cs`) — текущие значения, трансформации, именованные зоны, ошибки аргументов, DST-переходы, смешанный `DATEDIFF`, `DATEADD q/yyyy`, нативные перегрузки.
- **Binder** (`tests/Axiom.Semantics.Tests/BinderTests.cs`) — `Bind_BuiltinDateTimeNativeOverloadsReturnNativeTypes`, `Bind_BuiltinDateTimeStringValueArgumentsAreRejected`, `Bind_BuiltinDateTimeMixedTemporalKindsAreRejected`.

Полный прогон `Axiom.Runtime.Tests`: **958/958 passed** (0 failed); `Axiom.Semantics.Tests`: 376/376; `Axiom.Parsing.Tests`: 146/146.

---

## 10. Анализ Samples

16 файлов в `samples/functions/*` + 2 engine-compare (`current_time_snapshot.axm`, `date_time_calendar_pipeline.axm`) + 1 negative-sample (`datediff/mixed_temporal_kinds_error.axm`, фиксирует ошибку компиляции `MB2003` при смешении временных видов).

Все samples переведены на нативный стиль: `DIM x AS DATE/TIME/DATETIME` и конструкторы `DATESERIAL`/`TIMESERIAL`/`DATEVALUE`/`TIMEVALUE` вместо строковых литералов в `DATEADD`/`DATEDIFF`/`DATEPART`. Типизированный стиль демонстрирует `samples/functions/dateadd/typed_temporal_values.axm`.

---

## 11. Анализ документации

- 5 страниц `docs/manual/date-time-functions-*.html` описывают функции и канонические форматы.
- Нативные типы `DATE/TIME/DATETIME` документированы в разделе типов данных (`data-types*.html`).

---

## 12. Реестр проблем

| ID | Серьёзн. | Категория | Проблема | Статус |
|---|---|---|---|---|
| DT-001 | P2 | CORRECTNESS | Смешанные типы в `DATEDIFF` (time-интервал) молча игнорировали дату | ✅ Исправлено (MB3007) |
| DT-002 | P3 | ERROR/UX | Русский текст в англоязычной диагностике | ✅ Исправлено |
| DT-003 | P3 | VM | Расхождение текста диагностики Interpreter ↔ VM | ✅ Исправлено (унификация) |
| DT-004 | P3 | SEMANTICS/API | `DATEADD/DATEDIFF/DATEPART` на строковых параметрах (асимметрия с `YEAR`/`TOUTC`) | ✅ Исправлено: 9 нативных перегрузок; строковые формы удалены полностью |
| DT-005 | P3 | ARCHITECTURE | Дублирование ~300 строк между движками | ✅ Консолидировано |
| DT-006 | P4 | DOCS | Устаревший аудит-отчёт (строковая модель) | ✅ Исправлено (этот отчёт) |
| DT-007 | P4 | DOCS | Manual не описывал нативные типы | ✅ Исправлено |
| DT-008 | P4 | TESTS | Пробелы в тестах (VM DST, смешанный `DATEDIFF`, `DATEADD q/yyyy`) | ✅ Добавлено +6 |

---

## 13. Стабильные области (не трогать)

- `BuiltinDateTimeHelpers` — единый источник истины.
- Канонические форматы и наборы токенов интервалов/частей.
- Нумерация `WEEKDAY` с понедельника.
- DST-логика (`IsInvalidTime`/`IsAmbiguousTime`).
- `IRuntimeClock`-абстракция (детерминизм в тестах и benchmark).

---

## 14. Recovery Roadmap / остающиеся задачи

| Этап | Задача | Статус |
|---|---|---|
| 1 | DT-001 корректность | ✅ |
| 2 | DT-002/DT-003 диагностика | ✅ |
| 3 | DT-005 консолидация | ✅ |
| 4 | DT-008 тесты | ✅ |
| 5 | DT-006/DT-007 документация | ✅ |
| 6 | DT-004 миграция арифметики на нативные типы | ✅ Завершено |

**DT-004 (выполнено):** добавлены 9 нативных перегрузок `DATEADD`/`DATEDIFF`/`DATEPART` (`DateAddDateValue`/`TimeValue`/`DateTimeValue`, `DateDiffDateValues`/`TimeValues`/`DateTimeValues`, `DatePartDateValue`/`TimeValue`/`DateTimeValue`), оба движка покрыты тестами. Строковые формы удалены полностью (дескрипторы, документация, samples, тесты); `DateAdd`/`DateDiff`/`DatePart` остаются в `BuiltinFunctionId` как reserved для совместимости сериализации байткода. Смешение видов теперь отсекается в Binder на этапе компиляции (`MB2003`) — это усиливает защиту DT-001 (раньше — только runtime `MB3007`).

---

## 15. Итоговая оценка здоровья

| Аспект | Оценка | Пояснение |
|---|---|---|
| Корректность | 9/10 | Смешанный `DATEDIFF` исправлен; edge cases покрыты |
| Семантика | 9/10 | Чёткий контракт, канонические форматы, полная валидация |
| Архитектура | 9/10 | Единый слой `BuiltinDateTimeHelpers`, тонкие обёртки движков |
| Runtime | 9/10 | Чистый и полный общий слой |
| Interpreter | 9/10 | Полная реализация, корректная диагностика |
| Bytecode VM | 9/10 | Полная реализация, полный паритет, fast-path |
| Error handling | 9/10 | Единообразная диагностика (англ., MB3005/MB3007) |
| Tests | 9/10 | 958/958 runtime, DST, смешанные случаи и нативные перегрузки покрыты |
| Documentation | 9/10 | Manual и samples полностью переведены на нативный стиль |
| Maintainability | 9/10 | Дублирование устранено |
| Future readiness | 9/10 | Строковый интерфейс арифметики устранён; enum-иды reserved |

**Общий Health Score: ~9.0/10** (до устранения дефектов — ~8.2).

---

## 16. Главный контрольный вопрос

> Можно ли считать подсистему DATE/TIME достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**YES, WITH CONDITIONS**

**Основные blockers:** нет. DT-004 завершён, строковые формы арифметики удалены.

**Что сделать в первую очередь:** перейти к следующей подсистеме по плану (либо продолжить наблюдение за стабильностью нативного API в реальных программах).

**Что можно отложить:** переименование reserved-идов `DateAdd`/`DateDiff`/`DatePart` в `BuiltinFunctionId` (не влияет на поведение; потребуется только при следующем несовместимом изменении байткода).

**Что не следует трогать:** канонические форматы, токены, нумерацию `WEEKDAY`, DST-логику, `BuiltinDateTimeHelpers`, `IRuntimeClock`.
