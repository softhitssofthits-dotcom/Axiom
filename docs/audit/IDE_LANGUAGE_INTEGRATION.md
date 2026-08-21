# AXIOM — ГЛУБОКИЙ FORENSIC-АУДИТ
## ПОДСИСТЕМА: IDE / LANGUAGE INTEGRATION (ИНТЕГРАЦИЯ IDE И ЯЗЫКА)

**Дата:** 2026-08-21
**Уровень:** 15 — подсветка синтаксиса, форматирование, блочное редактирование, навигация по диагностикам, идентификаторы, запуск программ, отладчик, связь с pipeline

---

## 1. Резюме

Подсистема «IDE / Language Integration» отвечает за связь настольной WPF-IDE с языковым pipeline Axiom: лексером, парсером, семантикой, интерпретатором и байткодной VM. Это не автономная IDE-надстройка — **все операции с языком идут через единый `AxiomExecutionPipeline`**, и IDE не имеет собственного парсера или runtime.

**Главный вердикт: ДА, С УСЛОВИЯМИ** — подсистема достаточно стабильна, корректна и архитектурно понятна для перехода к следующей подсистеме, при условии что найденные архитектурные нарушения зафиксированы и дублирование ключевых слов начато к устранению.

**Ключевые факты:**
- IDE использует **реальный** парсер, binder, интерпретатор и VM — pipeline общий с CLI
- 156 исходных файлов C# в `src/Axiom.Ide/`, 38 тестовых файлов в `tests/Axiom.Ide.Tests/`
- Подсветка синтаксиса: 96 ключевых слов + 11 типов + 241 встроенная функция через XSHD
- Форматирование кода: два стиля (Classic/Modern), собственный токенизатор
- Блочное редактирование: автозавершение 9 типов блоков
- Навигация по диагностикам: клик → переход к строке/столбцу
- Отладчик: breakpoints, 4 режима шага, time-travel через `IProgramDebuggerState`
- Центральный реестр ключевых слов: `AxiomLanguageKeywords` в `Axiom.Core`
- Критическое дублирование: ключевые слова продублированы в 4+ местах
- `AxiomSmartIdentifierService` нарушает архитектурную границу

**Health Score: 7.2/10**

---

## 2. Что представляет собой эта подсистема

Подсистема интеграции IDE и языка — это слой компонентов, обеспечивающих:

1. **Подсветку синтаксиса** в редакторе AvalonEdit через XSHD-файлы
2. **Форматирование кода** с нормализацией регистра ключевых слов и расстановкой пробелов
3. **Блочное редактирование** — автозавершение конструкций при нажатии Enter
4. **Нормализацию идентификаторов** — приведение регистра переменных к объявлению
5. **Навигацию по диагностикам** — переход к месту ошибки в редакторе
6. **Запуск и компиляцию программ** — через `AxiomExecutionPipeline`
7. **Отладочное взаимодействие** — breakpoints, stepping, resume через `IProgramDebuggerState`
8. **Связь с pipeline** — единая точка входа `parse → bind → execute`

Подсистема не является изолированным IDE-модулем. Она пронизывает несколько слоёв: от XSHD-файлов редактора до адаптеров runtime.

---

## 3. Полный список возможностей

### SYS-001: Подсветка синтаксиса (XSHD)
- **Категория:** HIGHLIGHTING
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** N/A (визуальное оформление)
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** Два XSHD-файла (светлая и тёмная тема), 7 цветовых категорий
- **Tests:** `EditorFeatureTests` — 15+ тестов покрытия словаря
- **Samples:** N/A
- **Documentation:** Частично (IDE_FORENSIC_AUDIT.md)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `Axiom.xshd`, `Axiom.Dark.xshd`, `AxiomHighlighting.cs`, `AxiomHighlightingVocabulary.cs`

### SYS-002: Форматирование кода
- **Категория:** FORMATTING
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** Меню Format → Classic / Modern
- **Parser:** N/A (собственный токенизатор)
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AxiomCodeFormattingService` — два стиля, нормализация пробелов
- **Tests:** `CodeFormattingServiceTests` — 20+ тестов
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AxiomCodeFormattingService.cs`, `ICodeFormattingService.cs`, `CodeFormattingStyle.cs`

### SYS-003: Блочное редактирование
- **Категория:** STRUCTURAL_EDITING
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** Enter после заголовка блока
- **Parser:** N/A (regex-анализ строк)
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AxiomBlockEditingService` — 9 типов блоков, вставка body + closing
- **Tests:** `BlockEditingServiceTests` — 25+ тестов
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AxiomBlockEditingService.cs` (621 строка)

### SYS-004: Нормализация идентификаторов (Smart Identifiers)
- **Категория:** IDENTIFIER_NORMALIZATION
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** Меню Format → Smart Identifier
- **Parser:** Прямой вызов `Parser.Parse()` (**НАРУШЕНИЕ АРХИТЕКТУРНОЙ ГРАНИЦЫ**)
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AxiomSmartIdentifierService` — обход AST, замена регистра
- **Tests:** `SmartIdentifierServiceTests` — 4 теста
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED (с архитектурным дефектом)
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AxiomSmartIdentifierService.cs` (225 строк)

### SYS-005: Навигация по диагностикам
- **Категория:** DIAGNOSTIC_NAVIGATION
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** Клик по диагностике в панели
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AvalonEditDiagnosticNavigation`, `DiagnosticNavigationTargetFactory`
- **Tests:** `EditorFeatureTests` — 3 теста
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AvalonEditDiagnosticNavigation.cs`, `DiagnosticNavigationTargetFactory.cs`

### SYS-006: Компиляция и запуск программ
- **Категория:** EXECUTION
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** F5 (Run), Ctrl+F5 (Compile)
- **Parser:** Общий через `AxiomExecutionPipeline`
- **Binder:** Общий через `AxiomExecutionPipeline`
- **Interpreter:** Общий через `AxiomExecutionPipeline`
- **Bytecode VM:** Общий через `AxiomExecutionPipeline`
- **IDE:** `ProgramRunService` — компиляция, запуск, кэширование
- **Tests:** `ProgramRunServiceTests` — 10+ тестов
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `ProgramRunService.cs` (503 строки)

### SYS-007: Отладочное взаимодействие (Debugging Bridge)
- **Категория:** DEBUGGING
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** F10/F11/Shift+F11/Ctrl+F10
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A (только BytecodeVm)
- **Bytecode VM:** Полная поддержка через `BytecodeVmStopState`
- **IDE:** `BytecodeVmDebuggerAdapter`, `IProgramDebuggerState`
- **Tests:** `ProgramRunServiceTests` + `BytecodeVmTests`
- **Samples:** `samples/smoke/debug_step_controls.axm`
- **Documentation:** DEBUGGER.md (полный аудит)
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `BytecodeVmDebuggerAdapter.cs`, `IProgramDebuggerState.cs`

### SYS-008: Связывание текста редактора с ViewModel
- **Категория:** DATA_BINDING
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** N/A
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AvalonEditTextBinding` — two-way binding через DependencyProperty
- **Tests:** `AvalonEditTextBindingTests`
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AvalonEditTextBinding.cs` (99 строк)

### SYS-009: Центральный реестр ключевых слов
- **Категория:** LANGUAGE_REGISTRY
- **Тип:** RUNTIME FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** N/A
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `AxiomLanguageKeywords` в `Axiom.Core` — единый реестр
- **Tests:** `EditorFeatureTests.Highlighting_XshdKeywordsMatchCentralRegistry`
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `AxiomLanguageKeywords.cs` (69 строк)

### SYS-010: Политика форматирования документов
- **Кategorия:** FORMATTING_POLICY
- **Тип:** IDE FACILITY
- **Статус:** IMPLEMENTED
- **Пользовательский синтаксис:** N/A
- **Parser:** N/A
- **Binder:** N/A
- **Interpreter:** N/A
- **Bytecode VM:** N/A
- **IDE:** `DocumentFormattingPolicy` — проверка допустимости форматирования
- **Tests:** `DocumentFormattingPolicyTests`
- **Samples:** N/A
- **Documentation:** Частично
- **Текущий статус:** IMPLEMENTED
- **Уровень уверенности:** CONFIRMED
- **Доказательства:** `DocumentFormattingPolicy.cs` (17 строк)

---

## 4. Фактическая архитектура

### 4.1 Слои архитектуры

```
┌─────────────────────────────────────────────────────────────────────┐
│  СЛОЙ UI (WPF)                                                      │
│  MainWindow.xaml / MainWindow.*.cs (8 partial-файлов)               │
│  FindReplaceDialog.xaml                                             │
├─────────────────────────────────────────────────────────────────────┤
│  СЛОЙ VIEWMODEL                                                     │
│  MainWindowViewModel (7 partial-файлов, ~3460 строк)                │
│  DocumentTabViewModel, BottomPaneViewModel, StatusBarViewModel      │
│  FilesPaneViewModel, ViewModelBase                                  │
├─────────────────────────────────────────────────────────────────────┤
│  СЛОЙ СЕРВИСОВ                                                      │
│  ProgramRunService → AxiomExecutionPipeline                         │
│  ProgramConsoleService (вывод, графика, ввод, отмена)               │
│  DocumentFileService, FileTreeService, FileOperationsService        │
│  RecentFilesService, IdeInputProvider                               │
├─────────────────────────────────────────────────────────────────────┤
│  СЛОЙ РЕДАКТОРА (Language Integration)                              │
│  AxiomHighlighting / AxiomHighlightingVocabulary                    │
│  Axiom.xshd / Axiom.Dark.xshd                                       │
│  AxiomCodeFormattingService                                         │
│  AxiomBlockEditingService                                           │
│  AxiomSmartIdentifierService ← НАРУШАЕТ ГРАНИЦУ                     │
│  AvalonEditDiagnosticNavigation                                     │
│  AvalonEditTextBinding                                              │
│  EditorFindReplaceEngine / Workflow                                 │
│  BreakpointMargin                                                   │
├─────────────────────────────────────────────────────────────────────┤
│  СЛОЙ МОДЕЛЕЙ                                                       │
│  ProgramRunResult, ProgramCompilationResult                         │
│  ProgramDiagnosticItem, DocumentFileContent                         │
│  DebuggerStopInfo, IProgramDebuggerState                            │
├─────────────────────────────────────────────────────────────────────┤
│  МОСТ ВЫПОЛНЕНИЯ (Axiom.Execution)                                  │
│  AxiomExecutionPipeline                                             │
│  CompilationRequest / ExecutionRequest                              │
│  CompilationResult / ExecutionResult / CompiledProgram              │
├─────────────────────────────────────────────────────────────────────┤
│  ЯЗЫКОВОЙ PIPELINE                                                  │
│  Lexer → Parser → AST → Binder → BoundProgram                       │
│  BoundProgramLowerer → BytecodeCompiler → BytecodeModule            │
│  Interpreter / BytecodeVm                                           │
├─────────────────────────────────────────────────────────────────────┤
│  ЦЕНТРАЛЬНЫЙ РЕЕСТР                                                 │
│  AxiomLanguageKeywords (Axiom.Core)                                 │
│  BuiltinFunctions.All (Axiom.Core)                                  │
│  TokenKind / SyntaxKind (Axiom.Lexing / Axiom.Syntax)               │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 Ключевые файлы и их ответственности

| Файл | Строк | Ответственность |
|------|------:|-----------------|
| `AxiomHighlighting.cs` | 38 | Загрузка XSHD-определений подсветки |
| `AxiomHighlightingVocabulary.cs` | 45 | Парсинг XSHD в множества слов для проверки |
| `Axiom.xshd` | 380 | Светлая тема подсветки (96 keywords + 11 types + 241 builtins) |
| `Axiom.Dark.xshd` | ~380 | Тёмная тема подсветки |
| `AxiomCodeFormattingService.cs` | 612 | Форматирование строк и документов |
| `AxiomBlockEditingService.cs` | 621 | Автозавершение блоков при Enter |
| `AxiomSmartIdentifierService.cs` | 225 | Нормализация регистра идентификаторов |
| `AvalonEditDiagnosticNavigation.cs` | 89 | Навигация к месту диагностики |
| `DiagnosticNavigationTargetFactory.cs` | 96 | Вычисление offset по line:column |
| `ProgramRunService.cs` | 503 | Компиляция, запуск, отладочные шаги |
| `BytecodeVmDebuggerAdapter.cs` | 65 | Адаптер BytecodeVmStopState → IProgramDebuggerState |
| `IProgramDebuggerState.cs` | 60 | Интерфейс абстракции отладчика |
| `AvalonEditTextBinding.cs` | 99 | Two-way binding текста редактора |
| `AxiomLanguageKeywords.cs` | 69 | Центральный реестр ключевых слов |
| `DocumentFormattingPolicy.cs` | 17 | Политика допустимости форматирования |
| `ProgramDiagnosticItem.cs` | 60 | Модель диагностики для IDE |

### 4.3 Архитектурные границы

| Граница | Нарушена? | Доказательства |
|---------|-----------|----------------|
| IDE ↔ Parser | **ДА** | `AxiomSmartIdentifierService` вызывает `Parser.Parse()` напрямую |
| IDE ↔ Binder | Нет | IDE не вызывает Binder напрямую |
| IDE ↔ Interpreter | Нет | IDE использует `AxiomExecutionPipeline` |
| IDE ↔ BytecodeVm | Частично | `BytecodeVmDebuggerAdapter` знает `BytecodeVmStopState` |
| IDE ↔ Runtime | Нет | IDE не знает внутренности runtime |

---

## 5. Полный путь выполнения

### 5.1 Путь компиляции и запуска

```
Пользователь нажимает F5
    ↓
MainWindowViewModel.Debug.cs → ProgramRunService.Run()
    ↓
ProgramRunService.Compile()
    ↓
AxiomExecutionPipeline.Compile(CompilationRequest)
    ↓
Lexer.Tokenize() → Parser.Parse() → Binder.Bind()
    ↓
CompilationResult (Success / SyntaxFailure / SemanticFailure)
    ↓
ProgramRunService.Run() → AxiomExecutionPipeline.RunCompiled()
    ↓
ExecutionRequest (mode, breakpoints, input, output, cancellation)
    ↓
[Bytecode путь]                    [Interpreter путь]
BoundProgramLowerer                 Interpreter.Execute()
    ↓                                   ↓
BytecodeCompiler                    BoundProgram
    ↓                                   ↓
BytecodeModule                      RuntimeResult
    ↓
BytecodeVm.Execute()
    ↓
RuntimeResult
    ↓
ProgramRunResult → IDE (output, diagnostics, debugger state)
```

### 5.2 Путь отладки

```
Пользователь нажимает F10 (Step Over)
    ↓
MainWindowViewModel → ProgramRunService.StepOver(debuggerState)
    ↓
BytecodeVmDebuggerAdapter.StepOver()
    ↓
BytecodeVmStopState.StepOver()
    ↓
BytecodeVm (resume with step mode)
    ↓
RuntimeResult (with new StopState or completion)
    ↓
BytecodeVmDebuggerAdapter → ProgramRunResult
    ↓
IDE обновляет UI (line highlight, locals, call stack)
```

### 5.3 Путь подсветки

```
AvalonEdit загружает файл
    ↓
AxiomHighlighting.GetDefinition(theme)
    ↓
Загрузка Axiom.xshd / Axiom.Dark.xshd из embedded resources
    ↓
HighlightingLoader.Load() → IHighlightingDefinition
    ↓
AvalonEdit применяет regex-правила к тексту
    ↓
7 цветовых категорий: Comment, Keyword, String, Number, TypeName, BuiltIn, Operator
```

### 5.4 Путь форматирования

```
Пользователь теряет фокус со строки / нажимает Enter
    ↓
MainWindow.Editor.cs → TryFormatEditorLine()
    ↓
AxiomCodeFormattingService.FormatLine(lineText, style)
    ↓
SplitComment() → TryTokenize() → ClassifyTokens() → FormatTokens()
    ↓
Нормализация регистра (Classic = UPPER, Modern = Title)
    ↓
Нормализация пробелов вокруг операторов
    ↓
Отформатированная строка → AvalonEdit
```

### 5.5 Путь блочного редактирования

```
Пользователь нажимает Enter после "IF a = 0 THEN"
    ↓
MainWindow.Editor.cs → TryApplyBlockEditing()
    ↓
AxiomBlockEditingService.TryHandleLineTransition()
    ↓
AnalyzeLine() → regex-сопоставление заголовка блока
    ↓
[Если пустая строка после заголовка] → вставка body indent + closing
[Если непустая строка] → вставка branch body line
    ↓
Обновление Document → перемещение каретки
```

---

## 6. Фактический семантический контракт

### 6.1 Подсветка синтаксиса

**Вход:** текст `.axm` файла
**Обработка:** regex-правила XSHD
**Выход:** цветовые спаны для AvalonEdit

| Параметр | Допустимо | Недопустимо |
|----------|-----------|-------------|
| Ключевые слова | Регистронезависимые | N/A |
| Строки | `"..."` с `""` для экранирования | Многострочные строки |
| Числа | Десятичные, `&H` hex | Научная нотация, decimal-литералы |
| Комментарии | `'` и `REM` | Блочные комментарии |

**Побочные эффекты:** Нет
**Ошибки:** Нет (подсветка не генерирует ошибки)

### 6.2 Форматирование кода

**Вход:** строка текста
**Обработка:** токенизация + классификация + нормализация
**Выход:** отформатированная строка

| Параметр | Допустимо | Недопустимо |
|----------|-----------|-------------|
| Classic стиль | Все ключевые слова → UPPER | N/A |
| Modern стиль | Все ключевые слова → Title Case | N/A |
| Операторы | Пробелы вокруг бинарных | N/A |
| Строки | Сохраняются как есть | N/A |
| Комментарии | Нормализуется REM | N/A |

**Побочные эффекты:** Нет
**Ошибки:** Возвращает исходную строку при нераспознавании

### 6.3 Блочное редактирование

**Вход:** документ + номер предыдущей строки + номер текущей строки + позиция каретки
**Обработка:** regex-анализ заголовка блока
**Выход:** модифицированный документ + новая позиция каретки

| Параметр | Допустимо | Недопустимо |
|----------|-----------|-------------|
| Типы блоков | IF, FOR, WHILE, DO, SELECT, SUB, FUNCTION, TYPE, TRY | N/A |
| Вложенность | Корректно отслеживается | N/A |
| Уже закрытые блоки | Не вставляет дублирующий closing | N/A |

**Побочные эффекты:** Модифицирует `TextDocument`
**Ошибки:** Нет (возвращает `false` при невозможности)

### 6.4 Нормализация идентификаторов

**Вход:** текст программы + диапазон
**Обработка:** полный парсинг → обход AST → замена регистра
**Выход:** текст с нормализованными идентификаторами

| Параметр | Допустимо | Недопустимо |
|----------|-----------|-------------|
| Объявления | DIM, CONST, PARAMETER, SUB, FUNCTION, FOR | N/A |
| Ссылки | Все вхождения идентификатора | N/A |
| Регистр | Первое объявление побеждает | N/A |

**Побочные эффекты:** Нет (возвращает новый текст)
**Ошибки:** Нет (возвращает исходный текст при невозможности)

---

## 7. Анализ Parser

### 7.1 Общая связь с Parser

**IDE использует ТОТ ЖЕ парсер, что и CLI.** У IDE нет собственного парсера. Весь парсинг идёт через `AxiomExecutionPipeline` → `Parser.Parse()`.

**ИСКЛЮЧЕНИЕ:** `AxiomSmartIdentifierService` вызывает `Parser.Parse()` напрямую, обходя pipeline. Это не другой парсер, а прямой вызов того же парсера.

### 7.2 Проблемы

**SYS-001: `AxiomSmartIdentifierService` нарушает архитектурную границу**
- **Серьёзность:** P2
- **Категория:** ARCHITECTURE
- **Описание:** Сервис принимает `Func<string, ProgramSyntax>` и вызывает парсер напрямую, обходя `AxiomExecutionPipeline`. Это создаёт скрытую зависимость IDE от `Axiom.Parsing` и `Axiom.Syntax`.
- **Доказательства:** `AxiomSmartIdentifierService.cs:10` — `private readonly Func<string, ProgramSyntax> _parseSource;`
- **Последствия:** Нарушение принципа единственной ответственности; сложность замены парсера; невозможность кэширования результатов парсинга
- **Направление исправления:** Перенести нормализацию идентификаторов в pipeline или создать отдельный API в `Axiom.Core`

---

## 8. Анализ Binder

### 8.1 Связь с Binder

**IDE не вызывает Binder напрямую.** Вся семантическая проверка идёт через `AxiomExecutionPipeline.Compile()` → `Binder.Bind()`. Результаты семантического анализа (диагностики) передаются в IDE через `ProgramDiagnosticItem.FromDiagnostic()`.

### 8.2 Отсутствие информации о типах в IDE

**Критическая находка:** У IDE нет доступа к модели типов. Нет:
- автодополнения имён
- подсказок типов
- информации о символах
- «Перейти к определению»
- «Найти ссылки»

Это не баг, а feature gap. Семантическая информация, полученная при компиляции, не передаётся обратно в IDE для инструментария разработчика.

---

## 9. Анализ Interpreter

### 9.1 Связь с Interpreter

**IDE использует интерпретатор через `AxiomExecutionPipeline`.** Когда `ExecutionMode.Interpreter` выбран, pipeline вызывает `Interpreter.Execute()` напрямую.

### 9.2 Особенности

- Интерпретатор не поддерживает пошаговое выполнение (отладчик работает только с BytecodeVm)
- При запросе отладки с `ExecutionMode.Interpreter` происходит fallback на BytecodeVm
- `ProgramRunService` отслеживает fallback через `usedInterpreterFallback` и `interpreterFallbackReason`

### 9.3 Доказательства

```csharp
// ProgramRunService.cs:243
usedInterpreterFallback: executionResult.UsedInterpreterFallback 
    || mode == ExecutionMode.Bytecode 
    && executionResult.ActualEngine == ExecutionEngineKind.Interpreter,
```

---

## 10. Анализ Bytecode / VM

### 10.1 Связь с BytecodeVm

IDE связывается с BytecodeVm через:
1. `AxiomExecutionPipeline.RunCompiled()` — запуск программы
2. `BytecodeVmStopState` — состояние остановки отладчика
3. `BytecodeVmDebuggerAdapter` — адаптер, скрывающий типы runtime

### 10.2 Архитектура отладочного моста

```
BytecodeVm.Execute()
    ↓ (остановка на breakpoint)
BytecodeVmStopState (snapshot, CanResume, CanStepBack)
    ↓
BytecodeVmDebuggerAdapter (IProgramDebuggerState)
    ↓
ProgramRunService (creates ProgramRunResult)
    ↓
MainWindowViewModel (updates UI)
```

### 10.3 Качество абстракции

`IProgramDebuggerState` — чёткая архитектурная граница. IDE не знает о `BytecodeVmStopState` напрямую. Адаптер корректно инкапсулирует:
- `Resume()`, `StepOver()`, `StepInto()`, `StepOut()`, `StepBack()`
- `StopInfo` (Line, Column, Opcode, CallDepth, CanStepBack)
- `CanResume`, `CanStepBack`

---

## 11. Анализ Runtime

### 11.1 Связь с Runtime

IDE использует runtime через:
1. `AxiomExecutionPipeline` — единая точка входа
2. `ExecutionRequest` — параметры выполнения (input, output, cancellation, mode)
3. `RuntimeResult` — результат выполнения
4. `IRuntimeHost` → `IdeInputProvider` — адаптер ввода

### 11.2 Адаптеры

| Адаптер | От | К | Назначение |
|---------|----|----|------------|
| `IdeInputProvider` | `IProgramConsoleService` | `IRuntimeHost` | Ввод с консоли IDE |
| `BytecodeVmDebuggerAdapter` | `BytecodeVmStopState` | `IProgramDebuggerState` | Отладочные операции |
| `ExecutionRuntimeHostAdapter` | IDE services | `IRuntimeHost` | Полный runtime host |

---

## 12. Взаимодействие с типами

### 12.1 Подсветка типов

11 типов данных подсвечиваются отдельной категорией `TypeName`:
`BOOLEAN`, `DECIMAL`, `DOUBLE`, `FILE`, `IMAGE`, `INTEGER`, `LONG`, `SPRITE`, `STRING`, `TILEMAP`, `TILESET`

### 12.2 Форматирование типов

Типы данных форматируются как ключевые слова:
- Classic: `INTEGER`, `STRING`, `BOOLEAN`
- Modern: `Integer`, `String`, `Boolean`

### 12.3 Проверка покрытия

Тест `Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes` проверяет, что все типы из `SyntaxKind` присутствуют в XSHD. Тест `Highlighting_XshdKeywordsMatchCentralRegistry` проверяет соответствие `AxiomLanguageKeywords`.

---

## 13. Взаимодействие со String

### 13.1 Строковые литералы в подсветке

Regex-правило: `"[^"]*"?` — подсветка строк с поддержкой `""` для экранирования.

### 13.2 Строковые функции в подсветке

Все 44 строковые функции присутствуют в категории `BuiltIn` XSHD.

### 13.3 Форматирование строк

`AxiomCodeFormattingService` корректно сохраняет содержимое строк при форматировании. Токенизатор распознаёт строки как `RawTokenKind.String` и не модифицирует их.

---

## 14. Взаимодействие с Array

### 14.1 Массивные функции в подсветке

Все 34 массивные функции присутствуют в категории `BuiltIn` XSHD.

### 14.2 Блочное редактирование массивов

`REDIM` и `REDIM PRESERVE` не являются блочными конструкциями, поэтому `AxiomBlockEditingService` их не обрабатывает. Это корректно.

---

## 15. Взаимодействие с TYPE

### 15.1 TYPE в блочном редактировании

`AxiomBlockEditingService` обрабатывает `TYPE` как блочную конструкцию:
- Заголовок: `TYPE MyType` → вставка `END TYPE`
- В тестах: `TryHandleLineTransition_EnterAfterTypeHeader_InsertsEndType`

### 15.2 SELF в подсветке

`SELF` присутствует в списке ключевых слов XSHD.

---

## 16. Взаимодействие с Functions / SUB

### 16.1 SUB/FUNCTION в блочном редактировании

`AxiomBlockEditingService` обрабатывает:
- `SUB Greet(name AS STRING)` → вставка `END SUB`
- `FUNCTION Add(a AS INTEGER, b AS INTEGER) AS INTEGER` → вставка `END FUNCTION`

### 16.2 Локальные SUB/FUNCTION

Локальные объявления `SUB`/`FUNCTION` внутри процедур также обрабатываются блочным редактированием.

---

## 17. Взаимодействие с Control Flow

### 17.1 Все конструкции блочного редактирования

| Конструкция | Заголовок | Closing | В тестах |
|-------------|-----------|---------|----------|
| IF...THEN | `IF a = 0 THEN` | `END IF` | Да |
| ELSEIF...THEN | (ветвление) | N/A | Да |
| ELSE | (ветвление) | N/A | Да |
| FOR...TO | `FOR i = 1 TO 10` | `NEXT` | Да |
| WHILE | `WHILE a < 10` | `END WHILE` | Да |
| DO WHILE | `DO WHILE a < 10` | `LOOP` | Да |
| DO UNTIL | `DO UNTIL a > 10` | `LOOP` | Да |
| DO | `DO` | `LOOP` | Да |
| SELECT CASE | `SELECT CASE value` | `END SELECT` | Да |
| CASE | (ветвление) | N/A | Да |
| CASE ELSE | (ветвление) | N/A | Да |
| TRY | `TRY` | `END TRY` | Да |
| CATCH | (ветвление) | N/A | Да |
| FINALLY | (ветвление) | N/A | Да |

### 17.2 Нормализация ветвлений

`TryNormalizeBranchIndent` корректно выравнивает `ELSEIF`, `ELSE`, `CASE`, `CASE ELSE`, `CATCH`, `FINALLY` относительно их родительского блока.

---

## 18. Взаимодействие с Error / Exception System

### 18.1 Диагностики в IDE

`ProgramDiagnosticItem` отображает:
- Код (MB0xxx, MB1xxx, MB2xxx, MB3xxx, MB4xxx)
- Стадию (Syntax, Semantic, Runtime, Backend, Infrastructure)
- Сообщение
- Позицию (Line, Column, Length)

### 18.2 Навигация к ошибке

`AvalonEditDiagnosticNavigation.NavigateToDiagnostic()` переходит к месту ошибки в редакторе. `DiagnosticNavigationTargetFactory` вычисляет offset по line:column.

### 18.3 Обработка ошибок pipeline

`ProgramRunService` обрабатывает:
- `CompilationStatus.SyntaxFailure` → `ProgramCompilationStatus.SyntaxFailure`
- `CompilationStatus.SemanticFailure` → `ProgramCompilationStatus.SemanticFailure`
- Исключения → `ProgramCompilationStatus.InfrastructureFailure` с локализованным сообщением

---

## 19. State / Lifetime / Ownership

### 19.1 Состояние компиляции

`ProgramRunService` хранит `_compiledProgram` для кэширования:
- **Кто создаёт:** `Compile()` или `Run()`
- **Кто владеет:** `ProgramRunService`
- **Кто изменяет:** `Compile()`, `Run()`
- **Когда уничтожается:** При неудачной компиляции или при создании нового
- **Кэширование:** При совпадении текста программы (`string.Equals`)

### 19.2 Состояние отладчика

`BytecodeVmDebuggerAdapter` хранит `BytecodeVmStopState`:
- **Кто создаёт:** `BytecodeVm` при остановке
- **Кто владеет:** `BytecodeVmDebuggerAdapter`
- **Кто изменяет:** `Resume()`, `StepOver()`, и т.д. создают новый `RuntimeResult`
- **Когда уничтожается:** При завершении выполнения или новом запуске

### 19.3 Состояние редактора

`AvalonEditTextBinding` использует DependencyProperty с флагом `IsUpdating` для предотвращения циклических обновлений.

---

## 20. Edge Cases

### 20.1 Подсветка

| Сценарий | Поведение | Корректно? |
|----------|-----------|------------|
| Пустой файл | Нет подсветки | Да |
| Незакрытая строка | Подсвечивается до конца строки | Да (ограничение XSHD) |
| `REM` внутри строки | Не распознаётся как комментарий | Да |
| `&H` без цифр | Не подсвечивается как число | Да |
| Вложенные `""` в строках | Корректно обрабатывается regex | Да |

### 20.2 Форматирование

| Сценарий | Поведение | Корректно? |
|----------|-----------|------------|
| Пустая строка | Возвращает пустую строку | Да |
| Нераспознанная строка | Возвращает исходную | Да |
| `PRINT "rem stays"` | Строка не модифицируется | Да |
| `<<` как два `<` | `NormalizeCompoundShiftOperators` объединяет | Да |
| Префиксный `-` | Корректно определяется по контексту | Да |

### 20.3 Блочное редактирование

| Сценарий | Поведение | Корректно? |
|----------|-----------|------------|
| Уже закрытый блок | Не вставляет дублирующий closing | Да |
| Вложенные блоки | Корректно отслеживает стек | Да |
| `IF...THEN` на одной строке | Не обрабатывается (однострочный IF) | Да (по дизайну) |
| Пустая строка после заголовка | Вставляет body + closing | Да |
| Непустая строка после заголовка | Вставляет branch body line | Да |

### 20.4 Нормализация идентификаторов

| Сценарий | Поведение | Корректно? |
|----------|-----------|------------|
| Пустой документ | Возвращает пустую строку | Да |
| Нет объявлений | Возвращает исходный текст | Да |
| Регистронезависимое сравнение | Корректно через `OrdinalIgnoreCase` | Да |
| Комментарии и строки | Не модифицируются | Да |

---

## 21. Interpreter ↔ Bytecode VM

| Возможность | Interpreter | Bytecode VM | Одинаковый результат | Примечание |
|---|---|---|---|---|
| Компиляция | Да (через pipeline) | Да (через pipeline) | Да | Общий pipeline |
| Запуск | Да | Да | Да | Общий pipeline |
| Отладчик | **Нет** | Да | N/A | Только VM |
| Breakpoints | Нет | Да | N/A | Только VM |
| Step Over | Нет | Да | N/A | Только VM |
| Step Into | Нет | Да | N/A | Только VM |
| Step Out | Нет | Да | N/A | Только VM |
| Step Back | Нет | Да | N/A | Только VM |
| Диагностики | Да | Да | Да | Одинаковые |
| Вывод | Да | Да | Да | Одинаковый |

**Классификация:** Допустимое различие — отладчик является VM-only по архитектурному решению.

---

## 22. Анализ производительности

Подсистема интеграции не является hot path. Основные операции:
- Подсветка: regex-правила, выполняются AvalonEdit при изменении текста
- Форматирование: вызывается при потере фокуса со строки
- Блочное редактирование: вызывается при нажатии Enter
- Нормализация идентификаторов: вызывается при потере фокуса (опционально)
- Компиляция/запуск: вызывается по запросу пользователя

**Нет performance-критических участков в подсистеме интеграции.**

---

## 23. Качество Benchmark

Для подсистемы интеграции benchmarks не применимы. Подсистема работает в UI-потоке и не является вычислительно нагруженной.

---

## 24. Hot Paths и Bottlenecks

**Нет hot paths в подсистеме интеграции.** Все операции:
- Развратны пользователем (нажатие клавиш, клики)
- Работают в UI-потоке
- Не являются производительностно критичными

---

## 25. Архитектурные проблемы

### SYS-001: Нарушение архитектурной границы в SmartIdentifierService
- **Серьёзность:** P2
- **Категория:** ARCHITECTURE
- **Описание:** `AxiomSmartIdentifierService` принимает `Func<string, ProgramSyntax>` и вызывает парсер напрямую, создавая зависимость от `Axiom.Parsing` и `Axiom.Syntax`.
- **Доказательства:** `AxiomSmartIdentifierService.cs:4` — `using Axiom.Syntax;`
- **Последствия:** Нарушение принципа единственной ответственности; невозможность кэширования
- **Направление исправления:** Перенести API в `Axiom.Core` или `Axiom.Execution`

### SYS-002: Дублирование знаний о ключевых словах
- **Серьёзность:** P2
- **Категория:** ARCHITECTURE
- **Описание:** Ключевые слова языка продублированы в 4+ местах:
  1. `TokenKind` (Axiom.Lexing) — canonical source
  2. `AxiomLanguageKeywords` (Axiom.Core) — central registry
  3. `Axiom.xshd` / `Axiom.Dark.xshd` — XSHD подсветки
  4. `AxiomCodeFormattingService` — через `AxiomHighlightingVocabulary`
- **Доказательства:** `EditorFeatureTests.Highlighting_XshdKeywordsMatchCentralRegistry` проверяет синхронизацию
- **Последствия:** Риск рассинхронизации при добавлении новых ключевых слов
- **Направление исправления:** Генерация XSHD из `AxiomLanguageKeywords`

### SYS-003: Собственный токенизатор в форматтере
- **Серьёзность:** P3
- **Категория:** ARCHITECTURE
- **Описание:** `AxiomCodeFormattingService` имеет собственный токенизатор (`TryTokenize`), который не использует лексер языка. Это создаёт дублирование логики токенизации.
- **Доказательства:** `AxiomCodeFormattingService.cs:439-572` — собственный токенизатор
- **Последствия:** Рассинхронизация при изменении синтаксиса языка; невозможность обработки новых токенов
- **Направление исправления:** Использовать `Lexer` для токенизации или создать общий API

### SYS-004: Regex-анализ в блочном редактировании
- **Серьёзность:** P3
- **Категория:** ARCHITECTURE
- **Описание:** `AxiomBlockEditingService` использует 25+ regex-выражений для распознавания заголовков блоков. Это дублирует знания о синтаксисе, которые уже есть в парсере.
- **Доказательства:** `AxiomBlockEditingService.cs:11-37` — 25 regex-выражений
- **Последствия:** Рассинхронизация при изменении синтаксиса; сложность поддержки
- **Направление исправления:** Использовать парсер для определения типа строки или создать API в `Axiom.Core`

---

## 26. Дублирование логики

### 26.1 Ключевые слова

| Место | Формат | Количество | Источник |
|-------|--------|------------|----------|
| `TokenKind` | Enum | ~96 | Canonical |
| `AxiomLanguageKeywords` | HashSet | 96 | Manual sync |
| `Axiom.xshd` | XML | 96 | Manual sync |
| `Axiom.Dark.xshd` | XML | 96 | Manual sync |
| `AxiomHighlightingVocabulary` | Runtime parse | 96 | Auto from XSHD |

### 26.2 Встроенные функции

| Место | Формат | Количество | Источник |
|-------|--------|------------|----------|
| `BuiltinFunctionId` | Enum | 276 | Canonical |
| `Axiom.xshd` | XML | 241 | Manual sync |
| `Axiom.Dark.xshd` | XML | 241 | Manual sync |

### 26.3 Типы данных

| Место | Формат | Количество | Источник |
|-------|--------|------------|----------|
| `SyntaxKind` | Enum | 11 | Canonical |
| `AxiomLanguageKeywords` | HashSet | 11 | Manual sync |
| `Axiom.xshd` | XML | 11 | Manual sync |

### 26.4 Логика токенизации

| Место | Формат | Назначение |
|-------|--------|------------|
| `Lexer` | Полный лексер | Парсинг |
| `AxiomCodeFormattingService.TryTokenize` | Упрощённый | Форматирование |
| `AxiomBlockEditingService.AnalyzeLine` | Regex | Блочное редактирование |

---

## 27. Single Source of Truth

| Сущность | Канонический источник | Источник в IDE | Риск рассинхронизации |
|----------|----------------------|----------------|----------------------|
| Ключевые слова | `TokenKind` / `AxiomLanguageKeywords` | XSHD (ручное копирование) | **ВЫСОКИЙ** |
| Типы | `SyntaxKind` / `AxiomLanguageKeywords` | XSHD (ручное копирование) | **ВЫСОКИЙ** |
| Встроенные функции | `BuiltinFunctionId` | XSHD (ручное копирование) | **ВЫСОКИЙ** |
| Операторы | `TokenKind` | XSHD regex | СРЕДНИЙ |
| Диагностики | Коды в runtime/парсере | `ProgramDiagnosticItem.FromDiagnostic()` | НИЗКИЙ |
| Грамматика | Парсер | Собственный токенизатор форматтера | **СРЕДНИЙ** |
| Блочные конструкции | Парсер | 25 regex в `AxiomBlockEditingService` | **СРЕДНИЙ** |

---

## 28. Legacy / Dead Code

### 28.1 Кандидаты на удаление

Не обнаружено явных кандидатов на удаление. Все компоненты подсистемы активно используются.

### 28.2 Возможно устаревший код

Нет.

---

## 29. Надёжность и безопасность

### 29.1 Надёжность

| Риск | Оценка | Примечание |
|------|--------|------------|
| Повреждение данных | НИЗКИЙ | Форматирование работает на строковом уровне |
| Потеря состояния | НИЗКИЙ | Кэширование `_compiledProgram` корректно |
| Исключения в UI | СРЕДНИЙ | `HandleEditorInteractionException` ловит ошибки |
| Race condition | НИЗКИЙ | `IsUpdating` флаг предотвращает циклы |

### 29.2 Безопасность

| Риск | Оценка | Примечание |
|------|--------|------------|
| Path traversal | НИЗКИЙ | `ResolveResourceBaseDirectory` использует `Path.GetDirectoryName` |
| Resource exhaustion | НИЗКИЙ | Timeout через `TimeoutCancellationProvider` |
| Malformed input | НИЗКИЙ | Все компоненты обрабатывают некорректный ввод |

---

## 30. Анализ тестов

### 30.1 Покрытие тестами

| Компонент | Файл тестов | Количество | Тип |
|-----------|-------------|------------|-----|
| Подсветка | `EditorFeatureTests` | 15+ | Unit |
| Форматирование | `CodeFormattingServiceTests` | 20+ | Unit |
| Блочное редактирование | `BlockEditingServiceTests` | 25+ | Unit |
| Smart Identifiers | `SmartIdentifierServiceTests` | 4 | Unit |
| Навигация | `EditorFeatureTests` | 3 | Unit |
| Text Binding | `AvalonEditTextBindingTests` | 5+ | Unit |
| Запуск программ | `ProgramRunServiceTests` | 10+ | Integration |
| Политика форматирования | `DocumentFormattingPolicyTests` | 3+ | Unit |
| Shell ресурсы | `ShellResourceTests` | 5+ | Unit |

### 30.2 Качество тестов

Тесты **реально проверяют поведение**, а не только наличие кода:
- `FormatLine_Classic_UppercasesKeywordsAndAddsOperatorSpacing` — проверяет конкретный результат
- `TryHandleLineTransition_EnterAfterIfHeader_InsertsBodyIndentAndClosingLine` — проверяет модификацию документа
- `Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes` — проверяет полноту покрытия

---

## 31. Недостающие тесты

### 31.1 Обычные сценарии

| Сценарий | Статус |
|----------|--------|
| Форматирование многострочного документа | Есть |
| Блочное редактирование с вложенностью | Есть |
| Навигация к диагностике | Есть |
| Запуск с breakpoints | Есть |

### 31.2 Отрицательные сценарии

| Сценарий | Статус |
|----------|--------|
| Нераспознанная строка форматирования | Есть |
| Отсутствующая диагностика | Есть |
| Невалидный XSHD | Нет |
| Повреждённый документ | Нет |

### 31.3 Граничные случаи

| Сценарий | Статус |
|----------|--------|
| Пустой файл | Частично |
| Очень длинная строка | Нет |
| Unicode в идентификаторах | Нет |
| Максимальная вложенность блоков | Нет |

### 31.4 Регрессионные тесты

| Сценарий | Статус |
|----------|--------|
| Рассинхронизация XSHD и AxiomLanguageKeywords | Есть (`Highlighting_XshdKeywordsMatchCentralRegistry`) |
| Рассинхронизация XSHD и BuiltinFunctions | Есть (`Highlighting_VocabularyCoversAllBuiltInFunctions`) |

---

## 32. Анализ Samples

Для подсистемы интеграции IDE samples не применимы. Подсистема работает внутри IDE и не имеет пользовательского API, который можно было бы продемонстрировать в `.axm`-файле.

---

## 33. Анализ документации

### 33.1 Существующая документация

| Документ | Статус | Точность |
|----------|--------|----------|
| `IDE_FORENSIC_AUDIT.md` | Активен | Точно описывает состояние на 2026-08-19 |
| `IDE_REDESIGN.md` | Активен | Фазы 1-8 закрыты |
| `DEBUGGER.md` | Активен | Полный аудит отладчика |
| `ARCHITECTURE.md` | В основном точен | Не хватает деталей IDE |

### 33.2 Документация против кода

| Утверждение в документации | Соответствует коду? |
|-----------------------------|---------------------|
| «IDE использует реальный парсер» | Да |
| «AxiomSmartIdentifierService нарушает границу» | Да |
| «Ключевые слова дублированы в 5 местах» | Да (4 места подтверждены) |
| «STRING побеждает в XSHD за счёт порядка XML» | Да (TypeName идёт после Keyword) |

---

## 34. Интеграция с IDE

### 34.1 Подсветка синтаксиса

**Реализовано:** 7 цветовых категорий, 2 темы, полное покрытие ключевых слов и built-in функций.

**Проблема:** `STRING` попадает и в список типов, и в список ключевых слов в XSHD. Тип побеждает за счёт порядка XML (TypeName идёт после Keyword). Это не баг, но может вводить в заблуждение при чтении XSHD.

### 34.2 Форматирование кода

**Реализовано:** Два стиля (Classic/Modern), нормализация пробелов, обработка комментариев.

**Проблема:** Собственный токенизатор не покрывает все токены языка (например, `:=`, `&`).

### 34.3 Блочное редактирование

**Реализовано:** 9 типов блоков, ветвления, вложенность.

**Проблема:** 25 regex-выражений дублируют знания о синтаксисе.

### 34.4 Автодополнение

**Не реализовано.** Нет автодополнения имён, нет подсказок типов.

### 34.5 Навигация

**Реализовано:** Навигация по диагностикам. **Не реализовано:** «Перейти к определению», «Найти ссылки».

### 34.6 Отладчик

**Реализовано:** Breakpoints, 4 режима шага, time-travel. **Только для BytecodeVm.**

---

## 35. Зависимости от Host / Platform

| Зависимость | Тип | Примечание |
|-------------|-----|------------|
| WPF | UI framework | Windows-only |
| AvalonEdit | Редактор | Third-party, MIT license |
| .NET 10 | Runtime | Требует .NET 10 |
| Windows | OS | WPF не кроссплатформенный |

**Все зависимости — HOST LIMITATION, не LANGUAGE DEFECT.**

---

## 36. Карта корневых причин

### ROOT-SYS-001: Дублирование знаний о языке
- ├── SYS-002: Ключевые слова в 4+ местах
- ├── SYS-003: Собственный токенизатор форматтера
- └── SYS-004: Regex-анализ блочного редактирования

**Единая причина:** Отсутствие единого API для получения знаний о языке (токены, ключевые слова, грамматика) из кода.

### ROOT-SYS-002: Нарушение архитектурных границ
- └── SYS-001: SmartIdentifierService вызывает парсер напрямую

**Единая причина:** Отсутствие API в `Axiom.Core` или `Axiom.Execution` для операций с AST, не связанных с выполнением.

---

## 37. Стабильные области

### ЧТО РАБОТАЕТ И ЧТО НЕ СЛЕДУЕТ ТРОГАТЬ

1. **`IProgramDebuggerState`** — чёткая архитектурная граница между IDE и runtime. Абстракция корректная и стабильная.

2. **`AxiomExecutionPipeline`** — единая точка входа для компиляции и выполнения. IDE не дублирует логику pipeline.

3. **`AvalonEditTextBinding`** — простой и корректный two-way binding с защитой от циклов.

4. **`DiagnosticNavigationTargetFactory`** — чистая функция, корректно вычисляет offset по line:column.

5. **`ProgramDiagnosticItem`** — корректная модель диагностики с автоматическим определением стадии по коду.

6. **Тесты подсветки** — `Highlighting_VocabularyCoversAllLanguageKeywordsAndTypes` и `Highlighting_XshdKeywordsMatchCentralRegistry` обеспечивают автоматическую проверку синхронизации.

---

## 38. Возможности консолидации

### ЛОГИКУ, КОТОРУЮ СЛЕДУЕТ ОБЪЕДИНИТЬ

**Текущие места:**
- `TokenKind` (Axiom.Lexing)
- `AxiomLanguageKeywords` (Axiom.Core)
- `Axiom.xshd` / `Axiom.Dark.xshd` (Axiom.Ide)
- `AxiomHighlightingVocabulary` (Axiom.Ide)

**Дублируемая логика:** Списки ключевых слов, типов, встроенных функций

**Единый источник истины:** `AxiomLanguageKeywords` (Axiom.Core) + `BuiltinFunctions.All` (Axiom.Core)

**Направление миграции:** Генерация XSHD из `AxiomLanguageKeywords` при сборке или через source generator

**Риски:** Сложность генерации XSHD с regex-правилами; необходимость сохранения ручного контроля над цветами

---

## 39. Возможности разделения ответственности

### ЛОГИКУ, КОТОРУЮ СЛЕДУЕТ РАЗДЕЛИТЬ

1. **Токенизация форматтера** → вынести в общий API `Axiom.Core` или использовать `Lexer`
2. **Анализ блочных конструкций** → вынести в API `Axiom.Core` или использовать `Parser`
3. **Нормализация идентификаторов** → перенести в `Axiom.Execution` или `Axiom.Core`

---

## 40. Кандидаты на удаление

Не обнаружено. Все компоненты подсистемы активно используются.

---

## 41. Готовность к дальнейшему развитию

### 41.1 Готовность к автодополнению

**Низкая.** Нет модели символов, нет информации о типах в IDE. Для реализации автодополнения потребуется:
- API в Binder для получения списка символов
- Передача информации о типах через pipeline
- Новый слой в IDE для отображения подсказок

### 41.2 Готовность к «Перейти к определению»

**Низкая.** Нет связи между использованием идентификатора и его объявлением. Для реализации потребуется:
- API в Binder для получения location declarations
- Индексация символов

### 41.3 Готовность к рефакторингу

**Низкая.** Нет модели символов, нет информации о зависимостях.

### 41.4 Готовность к многомодульности

**Средняя.** `AxiomExecutionPipeline` работает с одним файлом. Для многомодульности потребуется:
- Расширение pipeline для обработки нескольких файлов
- Система проектов в IDE
- Разрешение зависимостей между модулями

---

## 42. Реестр проблем

| ID | Серьёзность | Категория | Компонент | Проблема | Доказательства | Корневая причина | Уверенность |
|---|---|---|---|---|---|---|---|
| SYS-001 | P2 | ARCHITECTURE | SmartIdentifierService | Нарушение архитектурной границы | `using Axiom.Syntax;` | Отсутствие API в Core | CONFIRMED |
| SYS-002 | P2 | ARCHITECTURE | XSHD + Keywords | Дублирование ключевых слов в 4+ местах | Тесты проверяют синхронизацию | Ручное копирование | CONFIRMED |
| SYS-003 | P3 | ARCHITECTURE | FormattingService | Собственный токенизатор | 130 строк кода | Отсутствие общего API | CONFIRMED |
| SYS-004 | P3 | ARCHITECTURE | BlockEditingService | 25 regex дублируют синтаксис | 25 regex-выражений | Отсутствие API парсера | CONFIRMED |
| SYS-005 | P3 | SEMANTICS | XSHD | STRING в Keyword и TypeName | Порядок XML | Ручное управление | CONFIRMED |
| SYS-006 | P3 | PERFORMANCE | SmartIdentifierService | Полный парсинг при нормализации | `_parseSource(sourceText)` | Нет инкрементального парсинга | HIGH |
| SYS-007 | P4 | TESTS | FormattingService | Нет тестов Unicode | Отсутствие тестов | Не реализовано | HIGH |
| SYS-008 | P4 | TESTS | BlockEditingService | Нет тестов максимальной вложенности | Отсутствие тестов | Не реализовано | HIGH |

---

## 43. Recovery Roadmap

### ЭТАП 0 — Базовая диагностика и измерения
- Измерить время компиляции программ через IDE
- Зафиксировать текущее состояние тестов
- Покрытие: все компоненты

### ЭТАП 1 — Исправление критических проблем корректности
- Нет критических проблем корректности (P0/P1)

### ЭТАП 2 — Стабилизация семантики
- Нет проблем семантики

### ЭТАП 3 — Архитектурное улучшение
- **TASK-IDE-001:** Перенести нормализацию идентификаторов из `AxiomSmartIdentifierService` в API `Axiom.Core` или `Axiom.Execution`, устранив прямую зависимость от парсера
- **TASK-IDE-002:** Создать API в `Axiom.Core` для получения типа строки (заголовок блока, ветвление, закрытие) на основе парсера, заменив 25 regex в `AxiomBlockEditingService`
- **TASK-IDE-003:** Рассмотреть генерацию XSHD из `AxiomLanguageKeywords` при сборке

### ЭТАП 4 — Оптимизация производительности
- Нет критических проблем производительности

### ЭТАП 5 — Усиление тестирования
- **TASK-IDE-004:** Добавить тесты Unicode для форматтера
- **TASK-IDE-005:** Добавить тесты максимальной вложенности для блочного редактирования
- **TASK-IDE-006:** Добавить тесты edge cases для SmartIdentifierService

### ЭТАП 6 — Обновление документации
- **TASK-IDE-007:** Обновить `IDE_FORENSIC_AUDIT.md` с учётом новых находок
- **TASK-IDE-008:** Добавить секцию «Интеграция IDE и языка» в `ARCHITECTURE.md`

### ЭТАП 7 — Обновление IDE
- Нет необходимых обновлений IDE

### ЭТАП 8 — Подготовка к дальнейшему развитию
- **TASK-IDE-009:** Спроектировать API для модели символов (автодополнение, «Перейти к определению»)
- **TASK-IDE-010:** Спроектировать передачу семантической информации из Binder в IDE

---

## 44. Граф зависимостей задач

```
TASK-IDE-001 (SmartIdentifier API)
    ↓
TASK-IDE-009 (Model Symbols API)

TASK-IDE-002 (Block Analysis API)
    ↓
TASK-IDE-010 (Semantic Info Transfer)

TASK-IDE-003 (XSHD Generation)
    ↓ (независимо)

TASK-IDE-004..006 (Tests)
    ↓ (независимо)

TASK-IDE-007..008 (Docs)
    ↓ (независимо)
```

---

## 45. Definition of Done

Подсистема «IDE / Language Integration» считается готовой, когда:

1. **Корректность:** Все компоненты корректно обрабатывают валидный и невалидный ввод
2. **Edge Cases:** Пустые файлы, Unicode, максимальная вложенность обрабатываются корректно
3. **Error Handling:** Ошибки компиляции и выполнения корректно отображаются в IDE
4. **Interpreter:** Программы запускаются через интерпретатор без ошибок
5. **VM:** Программы запускаются через BytecodeVm без ошибок
6. **Отладчик:** Breakpoints, stepping, resume работают корректно
7. **Тестирование:** Все компоненты покрыты unit-тестами
8. **Документация:** Архитектура и интеграция документированы
9. **IDE:** Подсветка, форматирование, блочное редактирование работают корректно
10. **Производительность:** Нет значительных задержек в UI

---

## 46. ТОП-20 проблем корректности и архитектуры

| # | ID | Серьёзность | Проблема |
|---|---|---|---|
| 1 | SYS-001 | P2 | SmartIdentifierService нарушает архитектурную границу |
| 2 | SYS-002 | P2 | Дублирование ключевых слов в 4+ местах |
| 3 | SYS-003 | P3 | Собственный токенизатор форматтера |
| 4 | SYS-004 | P3 | 25 regex дублируют синтаксис в блочном редактировании |
| 5 | SYS-005 | P3 | STRING в Keyword и TypeName XSHD |
| 6 | SYS-006 | P3 | Полный парсинг при нормализации идентификаторов |
| 7 | SYS-007 | P4 | Нет тестов Unicode для форматтера |
| 8 | SYS-008 | P4 | Нет тестов максимальной вложенности |

---

## 47. ТОП-20 задач по производительности

Для подсистемы интеграции IDE и языка нет критических проблем производительности. Все операции:
- Развратны пользователем
- Работают в UI-потоке
- Не являются вычислительно нагруженными

---

## 48. Итоговая оценка здоровья подсистемы

| Критерий | Оценка | Объяснение |
|----------|--------|------------|
| Корректность | 8/10 | Все компоненты корректно работают; нет P0/P1 проблем |
| Семантика | 8/10 | Подсветка, форматирование, блочное редактирование корректны |
| Архитектура | 6/10 | Нарушение границы в SmartIdentifierService; дублирование ключевых слов |
| Runtime | 8/10 | Чёткий мост через AxiomExecutionPipeline |
| Interpreter | 8/10 | Корректная интеграция через pipeline |
| Bytecode VM | 9/10 | Отличная абстракция через IProgramDebuggerState |
| Производительность | 8/10 | Нет проблем; UI-bound операции |
| Память | 8/10 | Кэширование _compiledProgram корректно |
| Error handling | 8/10 | Корректная обработка ошибок pipeline |
| Tests | 7/10 | Хорошее покрытие; пробелы в Unicode и edge cases |
| Samples | N/A | Не применимо для подсистемы интеграции |
| Documentation | 7/10 | Есть аудит; не хватает деталей в ARCHITECTURE.md |
| IDE | 8/10 | Подсветка, форматирование, блочное редактирование работают |
| Maintainability | 7/10 | Дублирование усложняет поддержку |
| Reliability | 8/10 | Стабильная работа; корректная обработка ошибок |
| Future readiness | 5/10 | Нет модели символов; низкая готовность к автодополнению |

**Средний балл: 7.2/10**

---

## ФИНАЛЬНЫЙ КОНТРОЛЬНЫЙ ВОПРОС

> Можно ли считать подсистему «IDE / Language Integration» достаточно стабильной, корректной и архитектурно понятной для перехода к следующей подсистеме?

**ДА, С УСЛОВИЯМИ.**

---

## Основные blockers

1. **SYS-001:** `AxiomSmartIdentifierService` нарушает архитектурную границу — необходимо зафиксировать как технический долг и запланировать исправление
2. **SYS-002:** Дублирование ключевых слов — необходимо обеспечить автоматическую проверку синхронизации (тесты уже есть, но генерация XSHD — будущее улучшение)

## Что нужно сделать в первую очередь

1. Зафиксировать архитектурные нарушения в `docs/PLANS4.md`
2. Обновить `docs/ARCHITECTURE.md` секцией «Интеграция IDE и языка»
3. Обновить `docs/WORKLOG5.md` результатами аудита

## Что можно отложить

1. Генерация XSHD из AxiomLanguageKeywords (TASK-IDE-003)
2. API для модели символов (TASK-IDE-009)
3. Передача семантической информации в IDE (TASK-IDE-010)
4. Тесты Unicode и максимальной вложенности (TASK-IDE-004..006)

## Что не следует трогать

1. **`IProgramDebuggerState`** — стабильная, корректная абстракция
2. **`AxiomExecutionPipeline`** — единая точка входа, работает корректно
3. **`AvalonEditTextBinding`** — простой и корректный binding
4. **`DiagnosticNavigationTargetFactory`** — чистая функция, работает корректно
5. **Тесты подсветки** — автоматическая проверка синхронизации ключевых слов
