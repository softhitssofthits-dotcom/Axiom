# План улучшения архитектуры проекта Axiom

Дата создания: 2026-08-18
Статус: активный

## Обзор проблемы

Проект Axiom накопил ряд архитектурных и процессных проблем, которые мешают долгосрочной поддерживаемости:

1. **Утечки неуправляемых ресурсов** в графическом слое IDE (GCHandle, COM-объекты D3D11).
2. **Масштабное дублирование бизнес-логики** между Interpreter и BytecodeVm (~8,000-10,000 строк).
3. **Бог-объекты**: MainWindowViewModel (3,460 строк, 53 команды), BytecodeVm (20 partial-файлов, ~11,420 строк).
4. **Устаревшие ссылки** в документах `docs/agent/` на PLANS3 вместо PLANS4.
5. **Мёртвый тест** в ProgramRunServiceTests.cs:746 (без `[Fact]`).
6. **Разрозненные диагностические коды** — 62 константы разбросаны по 5 файлам.
7. **Отсутствие тестов** для Axiom.Syntax и графического конвейера.

## Приоритеты

План разделен на волны. Каждая волна — самостоятельна, можно прерываться между ними.

**Порядок выполнения:**
1. Волна 1 (безопасность) → немедленно, критично
2. Волна 4.1 (документы) → параллельно, тривиально
3. Волна 3.2 (диагностические коды) → подготовка к рефакторингу
4. Волна 2 (built-in слой) → самая крупная и важная работа
5. Волна 3 (архитектура) → по мере освобождения контекста
6. Волна 5 (тестирование) → в фоне, когда позволяет время

---

## Волна 1: Безопасность ресурсов (немедленно)

### 1.1. GCHandle утечка в NativeGraphicsScreenWindow

**Файл:** `src/Axiom.Ide/Graphics/NativeGraphicsScreenWindow.cs`

**Проблема:**
- Строка 99: `GCHandle.Alloc(this)` — аллокация в конструкторе.
- Строка 525: `GCHandle.Free()` — только в `HandleDestroyed()`.
- Нет финализатора → если Dispose() не вызван, GCHandle утечёт навсегда.
- Нет try/finally → если `_renderer.Dispose()` (строка 521) выбросит, `GCHandle.Free` не выполнится.
- Нет disposed-флага → двойной Dispose может вызвать проблемы.

**Исправление:**
1. Добавить поле `private GCHandle _windowHandle;` и `private bool _disposed;`.
2. В конструкторе сохранять `GCHandle` в поле вместо прямого `SetWindowLongPtr`.
3. В `HandleDestroyed` обернуть cleanup в try/finally.
4. Добавить финализатор `~NativeGraphicsScreenWindow()` как страховку.
5. В `Dispose()` добавить disposed-флаг и вызов `GC.SuppressFinalize(this)`.

### 1.2. Direct3D11SwapChainRenderer.Dispose без изоляции

**Файл:** `src/Axiom.Ide/Graphics/Direct3D11SwapChainRenderer.cs`

**Проблема:**
- Строки 170-177: пять последовательных `.Dispose()` без try/finally.
- Если первый выбросит, остальные ресурсы не освободятся.

**Исправление:**
1. Обернуть каждый Dispose в отдельный try/catch.
2. Добавить disposed-флаг.

### 1.3. Мёртвый тест

**Файл:** `tests/Axiom.Ide.Tests/ProgramRunServiceTests.cs:746`

**Проблема:** Метод `Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm` не имеет `[Fact]`.

**Исправление:** Добавить `[Fact]`.

---

## Волна 2: Единый слой built-in функций (5-8 сессий)

Самая крупная и важная архитектурная задача. Цель — вынести общую бизнес-логику из пар Interpreter/BytecodeVm в единый слой.

### Архитектура решения

Создать `src/Axiom.Runtime/Builtins/` — общую папку для shared builtin-логики.

Для каждой области — класс-хелпер, принимающий `TextLocation`:
- `BuiltinNumericHelpers.cs`
- `BuiltinRegexHelpers.cs`
- `BuiltinStringHelpers.cs`
- `BuiltinFileSystemHelpers.cs`
- `BuiltinDateTimeHelpers.cs`
- `BuiltinPrintUsingHelpers.cs`
- `BuiltinArrayHelpers.cs`
- `BuiltinFileIOHelpers.cs`
- `BuiltinMouseHelpers.cs`

**Паттерн:** Interpreter получает thin-wrapper, извлекающий `TextLocation` из `BoundCallExpression` и делегирующий в общий слой. BytecodeVm делегирует напрямую.

### Порядок переноса

| Шаг | Область | Объём | Сложность |
|-----|---------|-------|-----------|
| 2.1 | Numeric | ~12 KB | низкая |
| 2.2 | Regex | ~5 KB | низкая |
| 2.3 | String | ~10 KB | средняя |
| 2.4 | FileSystem | ~34 KB | средняя |
| 2.5 | DateTime | ~38 KB | средняя |
| 2.6 | PrintUsing | ~18 KB | средняя |
| 2.7 | Array | ~68 KB | высокая |
| 2.8 | FileIO + Mouse + Keyboard | ~28 KB | средняя |
| 2.9 | Обновить тесты паритета | — | средняя |

**Ожидаемый эффект:** сокращение ~8,000-10,000 строк дублирования; каждое изменение семантики — в одном месте.

---

## Волна 3: Улучшение архитектуры (2-3 сессии)

### 3.1. Разбить MainWindowViewModel

**Файл:** `src/Axiom.Ide/ViewModels/MainWindowViewModel.cs` (3,354 строки, 53 команды, 13 зависимостей)

Выделить:
- `FileTreeOperationsViewModel` — 13 команд файлового дерева
- `DebugExecutionViewModel` — 13 команд отладки
- `PreferencesViewModel` — тема, язык, режим выполнения

### 3.2. Единый реестр диагностических кодов

**Проблема:** 62 `private const string` разбросаны по 5 файлам.

**Исправление:** Создать `src/Axiom.Core/DiagnosticCodes.cs` — единый публичный статический класс со всеми кодами. Все слои ссылаются на него.

### 3.3. Создать Axiom.Syntax.Tests

**Проблема:** `Axiom.Syntax` (3,517 строк) — единственный проект frontend без тестов.

**Исправление:** Создать `tests/Axiom.Syntax.Tests/` с тестами на SyntaxKind, конструирование узлов, посетители.

### 3.4. Разбить InterpreterTests.cs

**Файл:** `tests/Axiom.Runtime.Tests/InterpreterTests.cs` (10,083 строки)

Разбить на partial-файлы по тематике: строковые, числовые, массивные, файловые, графические.

---

## Волна 4: Обновление документов (1 сессия)

### 4.1. docs/agent/ — ссылки PLANS3 → PLANS4

**Затронутые файлы:**
- `docs/agent/CURRENT_STATE.md` — 4 ссылки
- `docs/agent/README.md` — ~3 ссылки
- `docs/agent/PLAYBOOKS.md` — ~4 ссылки
- `docs/archive/state-before-20261011/docs/agent/HEADROOM_PILOT.md` — ~2 ссылки
- `docs/archive/state-before-20261011/docs/agent/EXTERNAL_TOOLS_EVALUATION.md` — ~6 ссылок

### 4.2. Ротация WORKLOG4

При превышении 700KB архивировать ранние сессии в WORKLOG4_ARCHIVE.md.

---

## Волна 5: Тестирование графики (2-3 сессии, низкий приоритет)

### 5.1. Программный рендер-хост для тестов

Headless framebuffer, отрисовка в Bitmap.

### 5.2. Baseline-тесты для SCREEN

Сравнение с эталонным изображением.

---

## Текущий статус

| Волна | Статус |
|-------|--------|
| 1. Безопасность ресурсов | завершена |
| 2. Единый слой built-in | завершена |
| 3. Архитектура | завершена |
| 4. Документы | завершена |
| 5. Тестирование графики | завершена |
