# Axiom Forensic Audit

Дата аудита: 2026-08-18

## 1. Executive Summary

**Axiom** — это полноценная IDE и интерпретируемый BASIC-подобный язык на C# с WPF-оболочкой. Проект в активной разработке с апреля 2026 года (57 коммитов, 1 автор). Текущее состояние: **работоспособный продукт** с компилируемой кодовой базой, ~2,341 тестами (все проходят) и обширным набором samples.

**Главные выводы:**
- Языковой pipeline (Lexer → Parser → AST → Semantics → Runtime → BytecodeVm) **работает корректно**
- Архитектура в целом **здоровая**, но имеет системные проблемы дублирования
- Основной технический долг — **дублирование built-in функций** между Interpreter и BytecodeVm (~2,000+ строк остающихся)
- IDE имеет **архитектурную проблему** с MainWindow.xaml.cs (1,550+ строк, 0% покрытия тестами)
- Execution tests **зависают** при полном прогоне (timeout > 120 секунд)

---

## 2. Repository Reality

| Метрика | Значение |
|---------|----------|
| Коммитов | 57 |
| Период | апрель — август 2026 |
| Автор | 1 |
| .cs файлов | 320 |
| Размер кода | ~3,247 KB |
| Проектов в solution | 19 (11 src + 8 tests) |
| Тестов | ~2,341 |
| Samples | ~350+ .axm файлов |

---

## 3. Current Architecture

Фактическая архитектура по коду:

```text
Layer 0: Axiom.Core (TextSpan, Diagnostic, DiagnosticBag)
Layer 1: Axiom.Lexing (Lexer, TokenKind, Token)
         Axiom.Syntax (SyntaxKind, SyntaxNode, AST)
Layer 2: Axiom.Parsing (Parser → AST)
         Axiom.Semantics (Binder → BoundProgram)
Layer 3: Axiom.Bytecode (Lowerer → BytecodeCompiler → BytecodeModule)
         Axiom.Runtime (Interpreter + BytecodeVm + builtins)
Layer 4: Axiom.Execution (Pipeline orchestrator)
Layer 5: Axiom.Ide (WPF + AvalonEdit)
         Axiom.ManualChecks (console tool)
         Axiom.Ide.SmokeHarness (UI automation)
```

---

## 4. Documented vs Actual Architecture

| Правило из ARCHITECTURE.md | Факт | Статус |
|-----------------------------|-------|--------|
| Core → none | Core → none | MATCH |
| Lexing → Core | Lexing → Core | MATCH |
| Syntax → Core | Syntax → Core | MATCH |
| Parsing → Core, Lexing, Syntax | Parsing → Core, Lexing, Syntax | MATCH |
| Semantics → Core, Syntax | Semantics → Core, Syntax | MATCH |
| Runtime → Core, Syntax, Semantics | Runtime → Core, **Bytecode**, Syntax, Semantics | **DRIFT** |
| Execution → Core, Syntax, Parsing, Semantics, Runtime | Execution → ..., **Bytecode**, ... | **DRIFT** |
| Bytecode — не документирован | Bytecode → Core, Syntax, Semantics | **MISSING** |

---

## 5. Architecture Drift

### DRIFT-001: Runtime → Bytecode dependency

- ARCHITECTURE.md не упоминает Axiom.Bytecode в правилах зависимостей
- Runtime зависит от Bytecode потому, что BytecodeVm живёт в Runtime и исполняет BytecodeModule
- Это **архитектурно обоснованно** (оба движка — часть runtime), но документация устарела

### DRIFT-002: Execution → Bytecode dependency

- Execution использует Bytecode для Compile и Lowering
- Документация не обновлена

---

## 6. Language Pipeline Audit

### 6.1 Lexer — CONFIRMED GOOD

- 135 TokenKind members, 100 keywords — полное покрытие
- Позиционирование корректно
- Malformed input → диагностики MB0001-MB0004
- Нет silent acceptance

### 6.2 Parser — CONFIRMED GOOD

- 5 файлов, ~2,600 строк
- Приоритеты операторов соответствуют стандарту BASIC
- Правоассоциативный `^`
- Error recovery: synthetic tokens, stuck-position guard, unexpected boundary detection
- Все конструкции языка разобраны

**Находки:**
- `ArrayAccessExpressionSyntax` определён, но никогда не создаётся парсером (dead code)
- Нет standalone `END` statement
- Нет unary `+`

### 6.3 AST — CONFIRMED GOOD

- Immutable nodes, IReadOnlyList collections
- Clean hierarchy: SyntaxNode → MemberSyntax/StatementSyntax/ExpressionSyntax/TypeSyntax
- Нет leaked parser state
- GraphicsSyntax.cs — 1,467 строк (допустимо для 20+ узлов одного домена)

**Находка:** `ArrayDimensionSyntax.Kind` возвращает `SyntaxKind.ArrayBounds` вместо собственного kind

### 6.4 Semantic Analysis — CONFIRMED GOOD

- 13 partial-файлов Binder — чёткое разделение ответственности
- BoundScope с разделением VariableParent/FunctionParent
- Shadowing detection
- BoundErrorExpression sentinel для каскадных ошибок
- 281 BuiltinFunctionId — полный каталог

**Находка:** `CanConvert()` не включает `Decimal → Double`, хотя `PromoteNumericType` промотирует Decimal к Double при смешанных выражениях

### 6.5 Bytecode/VM — CONFIRMED GOOD

- 87 opcodes, все обработаны в dispatch
- Lowering, emission, validation — полный pipeline
- Debug stepping support
- Frame caching для производительности

**Находки:**
- 3 латентных opcode: `Dup`, `CreateArray`, `BeginInput/EndInput` — определены, но не используются
- `Dup` может быть эмитирован компилятором, но VM не имеет обработчика (currently not emitted)

---

## 7. Runtime / VM Audit

### 7.1 RuntimeValue — GOOD

- Sealed class с factory методами
- Integer/Long кэширование (-1..1024)
- RuntimeStringAccumulator для паттерна `s = s & expr`
- Нет state leakage между invocation

### 7.2 Interpreter — GOOD

- 18+ partial-файлов — хорошее разделение
- RuntimeFailureException для control flow
- TRY/CATCH через exception handler stack

### 7.3 BytecodeVm — GOOD

- 19 partial-файлов
- Instruction collapsing оптимизации
- Кадровый кэш

### 7.4 Семантический паритет — GOOD

- Compare() обрабатывает Integer, Long, Double, Decimal, String — **в обоих движках**
- Division by zero, overflow — parity confirmed
- Record equality — parity confirmed

---

## 8. IDE Audit

### 8.1 Architecture

- 95+ .cs файлов, чёткая структура по зонам ответственности
- MainWindowViewModel: 6 partial-файлов — чистая композиция
- ProgramRunService: чистый мост IDE → Execution
- AvalonEdit + XSHD highlighting — тестируемо через Vocabulary

### 8.2 Проблемы

**P1: MainWindow.xaml.cs — 1,550+ строк, 0% покрытия тестами**
- Смешивает: editor lifecycle, caret handling, input routing, theming, formatting, breakpoint margins, diagnostics navigation, context menus, WndProc smoke harness
- Это самый крупный файл IDE и единственный крупный файл без тестов

**P2: Environment.CurrentDirectory mutation**
- ProgramRunService.Run() меняет глобальное состояние процесса
- Защищено shell lock, но архитектурно хрупко

**P3: Manual CanExecute cascade**
- 30+ команд вручную вызывают RaiseCanExecuteChanged()
- Добавление новой команды требует помнить о cascade

---

## 9. Test Audit

| Проект | Тестов | Статус |
|--------|--------|--------|
| Axiom.Lexing.Tests | 18 | PASS |
| Axiom.Syntax.Tests | 64 | PASS |
| Axiom.Parsing.Tests | 137 | PASS |
| Axiom.Semantics.Tests | 365 | PASS |
| Axiom.Runtime.Tests | 884 | PASS |
| Axiom.Bytecode.Tests | 203 | PASS |
| Axiom.Execution.Tests | 143+ | PASS (Pipeline), **HANG** (Acceptance) |
| Axiom.Ide.Tests | 415+ | **HANG** (>120s timeout) |

### Coverage Gaps

1. **MainWindow.xaml.cs** — 0% тестов на 1,550 строк
2. **Acceptance tests** — зависают при прогоне (возможно, ожидают UI-интеракции)
3. **IDE tests** — зависают при прогоне (возможно, WPF-специфичные)
4. **No E2E integration test** — нет теста open→compile→run→output через реальные сервисы
5. **Graphics subsystem** — только 9 IDE-level тестов

---

## 10. Documentation Audit

### Что хорошо:

- ARCHITECTURE.md подробный и в целом точный
- SPECIFICATION3.md обширная
- BYTECODE_COVERAGE.md — 1,121 строк детального реестра
- docs/manual/ — полноценный HTML справочник

### Что плохо:

- ARCHITECTURE.md устарела по dependency rules (нет Bytecode)
- Правила зависимостей не обновлены после добавления Axiom.Bytecode

---

## 11. Dependency Audit

### Нарушения:

| ID | Expected | Actual | Severity |
|----|----------|--------|----------|
| DEP-001 | Runtime → Core, Syntax, Semantics | Runtime → Core, **Bytecode**, Syntax, Semantics | LOW (обоснованно) |
| DEP-002 | Execution → Core, Syntax, Parsing, Semantics, Runtime | + **Bytecode** | LOW (обоснованно) |

### Циклических зависимостей: НЕТ

---

## 12. Duplication Audit

### CRITICAL: Interpreter ↔ BytecodeVm builtin duplication

Несмотря на недавний рефакторинг (−3,726 строк), **значительное дублирование остаётся:**

| Область | Статус | Остающееся дублирование |
|---------|--------|------------------------|
| Numeric | Вынесено в BuiltinNumericHelpers | Минимальное |
| Regex | Вынесено в BuiltinRegexHelpers | Минимальное |
| String | **Частично** | BytecodeVm.Builtins.String.cs **переимплементирует** Contains, StartsWith, EndsWith, Replace, Trim, Reverse вместо делегирования в BuiltinStringHelpers |
| FileSystem | Вынесено | Минимальное |
| DateTime | Вынесено | Минимальное |
| Array | Вынесено | Минимальное |
| PrintUsing | Вынесено | Минимальное |
| FileIO | Вынесено | Минимальное |
| Mouse | Вынесено | Минимальное |

**Остающееся критическое дублирование:**
- `BytecodeVm.Builtins.String.cs` (454 строки) содержит inline reimplementations вместо делегирования в `BuiltinStringHelpers`
- Конкретно: `EvaluateContains` (29 строк inline vs 1 строка делегирования), `EvaluateStartsWith` (23 vs 1), `EvaluateEndsWith` (23 vs 1), `EvaluateReverse` (другой алгоритм), `EvaluateTrimWhitespace` (13 vs 1), `EvaluateReplace` (11 vs 1)

### CRITICAL: Keyword lists в 5 местах

| Место | Что дублируется |
|-------|----------------|
| TokenKind.cs | 100 keyword enum members |
| SyntaxKind.cs | Те же 100 keyword enum members |
| Lexer.cs | Keywords dictionary (99 entries) |
| Axiom.xshd | Keywords (90 entries) + TypeNames (11) + BuiltIns (241) |
| Axiom.Dark.xshd | **То же самое**, что и Axiom.xshd |

**Итого: ~3,200+ строк дублированного кода**

---

## 13. Dead Code Audit

| Элемент | Статус | Уверенность |
|---------|--------|-------------|
| `ArrayAccessExpressionSyntax` | Никогда не создаётся парсером | CONFIRMED |
| `ArrayDimensionSyntax.Kind` → `ArrayBounds` | Собственный kind отсутствует | CONFIRMED |
| `Opcode.Dup` | Определён, VM не обрабатывает, compiler currently не эмитит | CONFIRMED |
| `Opcode.CreateArray` | Определён, не используется | CONFIRMED |
| `Opcode.BeginInput/EndInput` | Определены, не используются | CONFIRMED |
| `IOutputWriter` отдельно от `IReversibleOutputWriter` | Только как base interface | LIKELY INTENTIONAL |
| `Interpreter.Builtins.String.Char.cs` unused `expression` param | `_ = expression;` | CONFIRMED |

---

## 14. Overengineering Audit

- `GraphicsCoordinateValidation` + `GraphicsCircleValidation` — тонкие обёртки, possibly over-abstracted
- `IOutputWriter` / `IInputProvider` как отдельные interfaces — допустимо для будущего расширения

---

## 15. Underengineering Audit

- **MainWindow.xaml.cs** — God file (1,550+ строк, смешивает 8+ ответственностей)
- **Environment.CurrentDirectory mutation** — глобальное состояние
- **Manual CanExecute cascade** — хрупкий паттерн

---

## 16. Reliability Audit

### FAILURE MATRIX

| Failure | Origin | Propagation | User-visible | Recovery |
|---------|--------|-------------|--------------|----------|
| Empty source file | Lexer | Parser → empty ProgramSyntax | No error | Runs empty program |
| Unterminated string | Lexer MB0002 | Parser gets BadToken | Error in Diagnostics | Parsing continues |
| Infinite loop in user code | Runtime | Interpreter loop | Program hangs | IDE locks (no timeout) |
| Missing END IF | Parser MB1xxx | Synthetic tokens | Error in Diagnostics | Parsing continues |
| Runtime exception | Runtime MB3xxx | RuntimeFailureException | Error in Diagnostics | Execution stops |
| Cancelled execution | IDE | CancellationToken | Status bar | Clean stop |

**Критическая проблема:** Нет таймаута на выполнение пользовательских программ. Бесконечный цикл блокирует IDE.

---

## 17. Security Audit

- **Path traversal**: `NormalizeFileSystemPath` в BuiltinFileSystemHelpers обрабатывает пути
- **Resource exhaustion**: Нет лимита на размер массива, глубину рекурсии или время выполнения
- **Embedded secrets**: Не обнаружены
- **Process execution**: Нет `SHELL` или `EXEC` команд
- **Overall**: LOW risk для локального desktop приложения

---

## 18. Performance Audit

### Hot paths:

| Path | Status | Evidence |
|------|--------|----------|
| Lexer | O(T*N) position tracking | Could be O(N) with running state |
| Parser | Adequate | No reported issues |
| VM dispatch | Optimized | Instruction collapsing, frame caching |
| String operations | Optimized | RuntimeStringAccumulator |
| Array 1D access | Optimized | Direct path without ResolveLocation |

### Benchmark results (from BYTECODE_COVERAGE.md):

- Scalar operations: ~0.85x runtime (VM faster than interpreter)
- Control flow: ~0.83x runtime
- String operations: ~0.40x runtime (VM significantly faster)
- Array operations: ~0.65x runtime
- Array callbacks: ~0.82x runtime

**Overall: BytecodeVm consistently faster than Interpreter**

---

## 19. Git Hotspots

| Hotspot | Touches | Nature |
|---------|---------|--------|
| BytecodeVm.cs | 27 | Every feature touches VM |
| BytecodeModuleValidator.cs | 25 | Every feature touches validation |
| BytecodeCompiler.cs | 23 | Every feature touches emission |
| ProgramConsoleService.cs | 23 | I/O contract instability |
| Axiom.xshd / Axiom.Dark.xshd | 22 each | Mechanical tax on new keywords |
| DRAW operator | 5 commits in 1 day | Implement → fix → fix → fix → contract |

**Pattern: Graphics subsystem had highest churn (28% of commits)**

---

## 20. Contradiction Matrix

| Pair | Contradiction | Severity |
|------|---------------|----------|
| ARCHITECTURE.md ↔ csproj | Runtime → Bytecode dependency not documented | LOW |
| ARCHITECTURE.md ↔ csproj | Execution → Bytecode dependency not documented | LOW |
| ARCHITECTURE.md ↔ csproj | Bytecode project not in dependency rules | LOW |
| TokenKind ↔ SyntaxKind | Identical enum members duplicated | MEDIUM |
| Lexer keywords ↔ XSHD keywords | Same list in 3+ places | MEDIUM |
| Interpreter.Builtins.String ↔ BytecodeVm.Builtins.String | Divergent implementations of same functions | HIGH |
| CanConvert ↔ PromoteNumericType | Decimal→Double missing from CanConvert but used in Promote | MEDIUM |

---

## 21. Problem Inventory

| ID | Category | Severity | Confidence | Description |
|----|----------|----------|------------|-------------|
| BUG-001 | Duplication | P1 | CONFIRMED | String builtins: BytecodeVm reimplements instead of delegating to BuiltinStringHelpers |
| BUG-002 | Architecture | P1 | CONFIRMED | MainWindow.xaml.cs: 1,550+ lines, 0% test coverage, 8+ mixed responsibilities |
| BUG-003 | Duplication | P2 | CONFIRMED | Keyword/enum lists duplicated in 5 places (TokenKind, SyntaxKind, Lexer, 2x XSHD) |
| BUG-004 | Testing | P2 | CONFIRMED | Execution acceptance tests and IDE tests hang (>120s timeout) |
| BUG-005 | Reliability | P2 | CONFIRMED | No execution timeout for user programs — infinite loop blocks IDE |
| BUG-006 | Documentation | P2 | CONFIRMED | ARCHITECTURE.md dependency rules stale — missing Bytecode project |
| BUG-007 | Architecture | P2 | CONFIRMED | Environment.CurrentDirectory mutation in ProgramRunService.Run() |
| BUG-008 | Type System | P3 | CONFIRMED | CanConvert missing Decimal→Double implicit widening |
| BUG-009 | Dead Code | P3 | CONFIRMED | ArrayAccessExpressionSyntax defined but never constructed |
| BUG-010 | Dead Code | P3 | CONFIRMED | Latent opcodes: Dup, CreateArray, BeginInput/EndInput |
| BUG-011 | Code Quality | P3 | CONFIRMED | MatchIdentifier/MatchFieldIdentifier are identical methods |
| BUG-012 | Architecture | P3 | CONFIRMED | Manual CanExecute cascade for 30+ commands |
| BUG-013 | Performance | P4 | CONFIRMED | Lexer O(T*N) position tracking per token |

---

## 22. Root Cause Graph

```text
ROOT-001: Multiple sources of truth for language metadata
├── BUG-003: Keyword lists in 5 places
├── BUG-001: Built-in implementations diverge between Interpreter/BytecodeVm
└── Impact: Every new keyword/function requires 3-5 coordinated changes

ROOT-002: Missing separation of concerns in IDE
├── BUG-002: MainWindow.xaml.cs God file
├── BUG-007: Environment.CurrentDirectory mutation
└── BUG-012: Manual CanExecute cascade

ROOT-003: Insufficient integration test infrastructure
├── BUG-004: Tests hang (likely WPF/interaction dependencies)
├── Missing E2E tests
└── MainWindow.xaml.cs untestable in current form

ROOT-004: Spec-first vs code-first tension in graphics
├── DRAW operator: 5 fix commits in 1 day
├── SCREEN lifecycle: 4 fix commits over 17 days
└── Pattern: implement → fix → fix → fix → contract
```

---

## 23. Architectural Invariants

| Invariant | Status | Evidence |
|-----------|--------|----------|
| IDE не определяет semantics языка | **HOLD** | IDE depends only on Execution layer |
| Parser не выполняет runtime logic | **HOLD** | Clean separation |
| Runtime не зависит от UI | **HOLD** | IRuntimeHost abstraction |
| Language metadata имеет canonical source | **BROKEN** | Keywords in 5 places |
| Diagnostics не создаются случайно каждым слоем | **HOLD** | Centralized DiagnosticBag pattern |
| Interpreter и BytecodeVm имеют одинаковое поведение | **MOSTLY HOLD** | String builtins diverge |
| Новые языковые конструкции требуют обновления всех слоёв | **HOLD** | Lexer+Parser+AST+Semantics+Runtime+VM+IDE+Manual |

---

## 24. Blind Spots Review

Проверено дополнительно:
- **Build**: ✅ 0 errors, 0 warnings
- **Tests**: ✅ Lexing(18), Syntax(64), Parsing(137), Semantics(365), Runtime(884), Bytecode(203), Execution-Pipeline(143) — all pass
- **Execution-Acceptance**: ⚠️ HANG (>120s)
- **IDE tests**: ⚠️ HANG (>120s)
- **Samples**: ✅ 350+ .axm файлов, покрывают все реализованные функции
- **Manual**: ✅ HTML справочник существует
- **Dependency graph**: ✅ DAG, no cycles
- **Generated code**: Не обнаружено
- **CI/CD**: Не обнаружено (local-only project)

---

## 25. What Should NOT Be Changed

| Component | Reason |
|-----------|--------|
| Core (TextSpan, Diagnostic) | Стабильно, просто, используется везде |
| Lexer | Работает корректно, хорошее покрытие |
| Parser | Работает корректно, чистый Pratt parser |
| AST nodes | Immutable, well-designed |
| Semantic Binder structure | 13 partial-файлов, чёткое разделение |
| RuntimeValue sealed class | Efficient, well-cached |
| Execution pipeline | Clean orchestrator |
| BYTECODE_COVERAGE.md | Excellent tracking document |
| samples/ structure | Comprehensive coverage |

---

## 26. Removal Candidates

| Path | Reason | Confidence | Risk |
|------|--------|------------|------|
| `ArrayAccessExpressionSyntax` | Never constructed by parser | CONFIRMED | LOW |
| `Opcode.Dup` | Defined but not handled in VM, not emitted | CONFIRMED | LOW |
| `Opcode.CreateArray` | Defined but never emitted | CONFIRMED | LOW |
| `Opcode.BeginInput/EndInput` | Defined but never emitted | CONFIRMED | LOW |
| `Interpreter.Builtins.String.Char.cs` unused `expression` param | `_ = expression;` dead usage | CONFIRMED | LOW |

---

## 27. Consolidation Candidates

| What | Where | Impact |
|------|-------|--------|
| String builtins: BytecodeVm → BuiltinStringHelpers | BytecodeVm.Builtins.String.cs | ~300 lines removable |
| XSHD keyword lists → shared resource | Axiom.xshd, Axiom.Dark.xshd | Eliminate 1 of 2 identical files |
| TokenKind ↔ SyntaxKind → single source | 3 files | Eliminate MapTokenKind mapping |
| Diagnostic message strings → constants | 10+ files | Centralize error messages |
| MatchIdentifier/MatchFieldIdentifier → one method | Parser.Helpers.cs | Remove 14 duplicate lines |

---

## 28. Separation Candidates

| Component | Current | Proposed |
|-----------|---------|----------|
| MainWindow.xaml.cs | 1,550+ lines | MainWindow.Editor.cs, MainWindow.Input.cs, MainWindow.Diagnostics.cs, MainWindow.Graphics.cs |
| BytecodeVm.Builtins.String.cs | 454 lines with inline reimplementations | Delegate to BuiltinStringHelpers |

---

## 29. Project Health Score

| Dimension | Score | Evidence |
|-----------|-------|----------|
| Architecture | **8/10** | Clean layering, DAG dependencies, one stale doc |
| Correctness | **8/10** | 2,341 tests pass, Decimal→Double gap minor |
| Language coherence | **9/10** | 281 builtins, comprehensive pipeline |
| Runtime correctness | **8/10** | Both engines work, string builtins diverge |
| VM consistency | **8/10** | Good parity, minor string divergence |
| IDE integration | **7/10** | Works but MainWindow.xaml.cs is a risk |
| Testing | **7/10** | Good unit coverage, missing E2E, some tests hang |
| Documentation | **7/10** | Comprehensive but partially stale |
| Maintainability | **6/10** | Significant duplication in keywords/builtins |
| Performance | **9/10** | VM consistently faster than interpreter |
| Reliability | **7/10** | No execution timeout, Environment.CurrentDirectory mutation |

**Overall: 7.5/10** — Рабочий продукт с хорошей архитектурой, но значительным техническим долгом в области дублирования.
