# ПЛАН УЛУЧШЕНИЙ — IDE / Language Integration

**Основание:** `docs/archive/audits-through-20261011/IDE_LANGUAGE_INTEGRATION.md` (2026-08-21)
**Health Score:** 7.2/10
**Создан:** 2026-08-21

---

## Статус задач

| # | Фаза | Задача | ID аудита | Приоритет | Статус |
|---|------|--------|-----------|-----------|--------|
| 1 | 0 | Тесты Unicode для форматтера | SYS-007 | P4 | ✅ ЗАВЕРШЕНА |
| 2 | 0 | Тесты максимальной вложенности блочного редактирования | SYS-008 | P4 | ✅ ЗАВЕРШЕНА |
| 3 | 0 | Верифицировать STRING только в TypeName XSHD | SYS-005 | P3 | ✅ ЗАВЕРШЕНА (было исправлено ранее) |
| 4 | 1 | BlockLineClassifier вместо 25 regex | SYS-004 | P3 | ✅ ЗАВЕРШЕНА |
| 5 | 1 | Использовать лексер в форматтере | SYS-003 | P3 | ✅ ЗАВЕРШЕНА |
| 6 | 1 | Кэш парсинга в SmartIdentifierService | SYS-006 | P3 | ✅ ЗАВЕРШЕНА |
| 7 | 2 | Перенос нормализации идентификаторов в pipeline | SYS-001 | P2 | ✅ ЗАВЕРШЕНА |
| 8 | 2 | Генерация XSHD из центрального реестра | SYS-002 | P2 | ✅ ЗАВЕРШЕНА |

---

## Фаза 0 — Низко висящие фрукты

### Задача 1: Тесты Unicode для форматтера (SYS-007)

**Проблема:** `AxiomCodeFormattingService` не имеет тестов с Unicode-символами.
**Ожидаемый результат:** Тесты с кириллическими идентификаторами, emoji в строках, non-BMP символами.
**Файл:** `tests/Axiom.Ide.Tests/CodeFormattingServiceTests.cs`

### Задача 2: Тесты максимальной вложенности (SYS-008)

**Проблема:** `AxiomBlockEditingService` не имеет тестов с глубокой вложенностью.
**Ожидаемый результат:** Тесты с 10+ уровнями вложенности IF/FOR/WHILE/DO/SELECT.
**Файл:** `tests/Axiom.Ide.Tests/BlockEditingServiceTests.cs`

### Задача 3: Верифицировать STRING в XSHD (SYS-005)

**Проблема:** `STRING` может попадать и в Keyword, и в TypeName в XSHD.
**Ожидаемый результат:** `STRING` только в TypeName. Тест `Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes` проходит.
**Файлы:** `Axiom.xshd`, `Axiom.Dark.xshd`

---

## Фаза 1 — Архитектурные P3

### Задача 4: BlockLineClassifier (SYS-004)

**Проблема:** 25+ regex в `AxiomBlockEditingService` дублируют знания парсера.
**Решение:** Создать `BlockLineClassifier` в `Axiom.Core` с методом `ClassifyLine(string)`.
**Зависимости:** Нет.

### Задача 5: Lexer adapter для форматтера (SYS-003)

**Проблема:** Собственный токенизатор форматтера (130 строк) дублирует логику `Lexer`.
**Решение:** Адаптер `LexerTokenAdapter`, оборачивающий `Lexer.Tokenize()`.
**Зависимости:** Нет.

### Задача 6: Кэш парсинга (SYS-006)

**Проблема:** `AxiomSmartIdentifierService` делает полный парсинг при каждой нормализации.
**Решение:** Кэшировать результат по hash текста.
**Зависимости:** Нет.

---

## Фаза 2 — Архитектурные P2

### Задача 7: Pipeline normalization (SYS-001)

**Проблема:** `AxiomSmartIdentifierService` принимает `Func<string, ProgramSyntax>`.
**Решение:** Перенести нормализацию в `Axiom.Core` или `Axiom.Execution`.
**Зависимости:** Задача 6.

### Задача 8: XSHD generation (SYS-002)

**Проблема:** Ключевые слова дублированы в 4+ местах.
**Решение:** Source generator или pre-build скрипт для генерации XSHD из `AxiomLanguageKeywords`.
**Зависимости:** Нет.

---

## Граф зависимостей

```
Задача 1 ─ независима
Задача 2 ─ независима
Задача 3 ─ независима
Задача 4 ─ независима
Задача 5 ─ независима
Задача 6 ─ независима
Задача 7 → зависит от Задачи 6
Задача 8 ─ независима
```

---

## Что не следует трогать

- `IProgramDebuggerState` — стабильная абстракция
- `AxiomExecutionPipeline` — единая точка входа
- `AvalonEditTextBinding` — простой и корректный binding
- `DiagnosticNavigationTargetFactory` — чистая функция
- Тесты подсветки — автоматическая проверка синхронизации работает
