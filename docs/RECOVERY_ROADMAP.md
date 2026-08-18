# Axiom Recovery Roadmap

Дата: 2026-08-18

## 1. Recovery Objective

Восстановить Axiom до стабильной архитектурной базы, устранив системное дублирование, обновив документацию и закрыв критические пробелы в тестировании.

## 2. Guiding Principles

1. Инкрементальные изменения вместо rewrite
2. Каждое изменение покрывается тестами
3. Документация обновляется вместе с кодом
4. Не трогать то, что работает корректно
5. Приоритет: корректность → консистентность → архитектура → тесты → производительность

## 3. Current Baseline

- Build: ✅ 0 errors, 0 warnings
- Tests: ✅ 1,614+ pass (excluding hanging tests)
- Hanging tests: ⚠️ Execution-Acceptance, IDE (>120s timeout)
- Language pipeline: ✅ Lexer → Parser → AST → Semantics → Runtime → BytecodeVm
- IDE: ✅ Functional, MainWindow.xaml.cs needs split
- Samples: ✅ 350+ .axm файлов
- Documentation: ⚠️ ARCHITECTURE.md stale on dependencies

## 4. Critical Blockers

| Blocker | Impact | Priority |
|---------|--------|----------|
| String builtins divergence | Behavioral inconsistency between engines | P1 |
| MainWindow.xaml.cs God file | Untestable, fragile | P1 |
| Hanging tests | Cannot verify full regression | P2 |
| No execution timeout | IDE can hang on user code | P2 |

## 5. Dependency Graph

```text
TASK-001 (String builtins) ← no dependencies
TASK-002 (MainWindow split) ← no dependencies
TASK-003 (ARCHITECTURE.md) ← no dependencies
TASK-004 (Hanging tests) ← no dependencies
TASK-005 (Execution timeout) ← no dependencies
TASK-006 (Environment.CurrentDirectory) ← no dependencies
TASK-007 (Keyword consolidation) ← no dependencies
TASK-008 (CanConvert fix) ← no dependencies
TASK-009..020 ← independent
```

All tasks are independent and can be executed in any order within their priority tier.

---

## 6. Phase 0 — BASELINE

Фиксация текущего состояния — **ЗАВЕРШЁН**.

Результаты:
- Build: 0 errors, 0 warnings
- 1,614+ тестов проходят
- Аудит проведён, отчёт в `docs/AUDIT_REPORT.md`

---

## 7. Phase 1 — CRITICAL CORRECTIONS (P0/P1)

### TASK-001: Fix String builtins divergence

**OBJECTIVE:** Устранить переимплементацию строковых built-in в BytecodeVm, делегировать BuiltinStringHelpers.

**CURRENT STATE:** `BytecodeVm.Builtins.String.cs` (454 строки) содержит inline reimplementations для Contains, StartsWith, EndsWith, Replace, Trim, Reverse вместо делегирования в `BuiltinStringHelpers`.

**TARGET STATE:** BytecodeVm.Builtins.String.cs делегирует все строковые операции в BuiltinStringHelpers, как это сделано для Numeric, Regex, FileSystem, DateTime, Array, PrintUsing, FileIO, Mouse.

**FILES:**
- `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs`
- `src/Axiom.Runtime/Builtins/BuiltinStringHelpers.cs`

**COMPONENTS:** Runtime, BytecodeVm

**PRECONDITIONS:** Нет

**DEPENDENCIES:** Нет

**IMPLEMENTATION STRATEGY:**
1. Для каждой переимплементированной функции (Contains, StartsWith, EndsWith, Replace, Trim, Reverse, TrimWhitespace) заменить inline код на вызов BuiltinStringHelpers
2. Сохранить BytecodeVm-специфичную обёртку с TextLocation для диагностики
3. Убедиться, что BuiltinStringHelpers экспортирует все нужные методы

**TEST STRATEGY:** Существующие 884 теста Runtime должны пройти без изменений.

**DOCUMENTATION IMPACT:** Нет (поведение не меняется)

**IDE IMPACT:** Нет

**RUNTIME IMPACT:** Нет (поведение идентично)

**REGRESSION RISKS:** LOW — behavioral parity already confirmed

**ACCEPTANCE CRITERIA:**
- `dotnet build Axiom.sln` — 0 errors
- `dotnet test tests/Axiom.Runtime.Tests` — 884/884 pass
- BytecodeVm.Builtins.String.cs < 200 строк

**VALIDATION COMMANDS:**
```powershell
dotnet build Axiom.sln
dotnet test tests/Axiom.Runtime.Tests/Axiom.Runtime.Tests.csproj --no-build
```

---

### TASK-002: Split MainWindow.xaml.cs

**OBJECTIVE:** Разбить MainWindow.xaml.cs (1,550+ строк) на partial-файлы по ответственности.

**CURRENT STATE:** Один файл смешивает editor lifecycle, caret handling, input routing, theming, formatting, breakpoint margins, diagnostics navigation, context menus, WndProc smoke harness.

**TARGET STATE:** 4 partial-файла:
- `MainWindow.xaml.cs` — core lifecycle, constructor, Loaded/Unloaded
- `MainWindow.Editor.cs` — editor events, caret, selection, formatting integration
- `MainWindow.Diagnostics.cs` — diagnostics navigation, breakpoint margins, debug rendering
- `MainWindow.Input.cs` — keyboard/mouse routing, output interaction, WndProc

**FILES:**
- `src/Axiom.Ide/MainWindow.xaml.cs` → split into 4 files

**COMPONENTS:** IDE

**PRECONDITIONS:** Нет

**DEPENDENCIES:** Нет

**IMPLEMENTATION STRATEGY:**
1. Определить границы ответственности по методам
2. Создать partial-файлы с правильными using directives
3. Переместить методы без изменения логики
4. Проверить компиляцию и тесты

**TEST STRATEGY:** Существующие 415 IDE тестов должны пройти.

**DOCUMENTATION IMPACT:** Обновить `docs/ARCHITECTURE.md` описание структуры IDE

**IDE IMPACT:** Нет пользовательского impact

**RUNTIME IMPACT:** Нет

**REGRESSION RISKS:** MEDIUM — code-behind splitting requires careful XAML binding preservation

**ACCEPTANCE CRITERIA:**
- `dotnet build Axiom.sln` — 0 errors
- `dotnet test tests/Axiom.Ide.Tests` — все тесты проходят
- MainWindow.xaml.cs < 500 строк

**VALIDATION COMMANDS:**
```powershell
dotnet build Axiom.sln
dotnet test tests/Axiom.Ide.Tests/Axiom.Ide.Tests.csproj --no-build
```

---

## 8. Phase 2 — ARCHITECTURAL STABILIZATION (P2)

### TASK-003: Update ARCHITECTURE.md dependency rules

**OBJECTIVE:** Обновить правила зависимостей в ARCHITECTURE.md для включения Axiom.Bytecode.

**CURRENT STATE:** ARCHITECTURE.md §5 не упоминает Axiom.Bytecode. Runtime → Bytecode и Execution → Bytecode зависимости не документированы.

**TARGET STATE:** Правила зависимостей включают Bytecode с обоснованием.

**FILES:**
- `docs/ARCHITECTURE.md`

**IMPLEMENTATION STRATEGY:**
1. Добавить описание Axiom.Bytecode в §4 (Назначение слоёв)
2. Обновить §5 (Границы между слоями) — добавить Bytecode в правила
3. Обновить §3 (Структура solution)

**ACCEPTANCE CRITERIA:** ARCHITECTURE.md точно отражает фактические зависимости.

---

### TASK-004: Investigate and fix hanging tests

**OBJECTIVE:** Выяснить причину зависания Execution-Acceptance и IDE тестов и исправить.

**CURRENT STATE:** Тесты зависают при прогоне (>120s timeout). Pipeline тесты проходят (143/143).

**TARGET STATE:** Все тесты проходят за <60s.

**FILES:**
- `tests/Axiom.Execution.Tests/AcceptanceProgramTests.cs`
- `tests/Axiom.Ide.Tests/` (multiple files)

**IMPLEMENTATION STRATEGY:**
1. Запустить Acceptance тесты с фильтром по одному тесту для идентификации зависающего
2. Проверить наличие блокирующих вызовов (UI dispatcher, interactive input)
3. Добавить timeout или mock для блокирующих зависимостей

**ACCEPTANCE CRITERIA:**
```powershell
dotnet test tests/Axiom.Execution.Tests --no-build -v q  # < 60s
dotnet test tests/Axiom.Ide.Tests --no-build -v q  # < 60s
```

---

### TASK-005: Add execution timeout

**OBJECTIVE:** Добавить таймаут выполнения пользовательских программ, чтобы бесконечный цикл не блокировал IDE.

**CURRENT STATE:** Нет таймаута. `WHILE TRUE: WEND` блокирует IDE навсегда.

**TARGET STATE:** Конфигурируемый таймаут (по умолчанию 30s) с чистой отменой и диагностикой.

**FILES:**
- `src/Axiom.Runtime/Execution/Interpreter.cs`
- `src/Axiom.Runtime/Execution/BytecodeVm.cs`
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`
- `src/Axiom.Execution/Pipeline/AxiomExecutionPipeline.cs`

**IMPLEMENTATION STRATEGY:**
1. Добавить `ExecutionTimeout` параметр в pipeline request
2. Проверять elapsed time в dispatch loop (каждые N итераций)
3. При timeout → RuntimeDiagnostic с кодом MB3xxx
4. IDE → настройка таймаута в меню

**ACCEPTANCE CRITERIA:**
- Программа с `WHILE TRUE: WEND` завершается с ошибкой через 30s
- Тест на timeout проходит

---

### TASK-006: Fix Environment.CurrentDirectory mutation

**OBJECTIVE:** Устранить мутацию глобального `Environment.CurrentDirectory` в ProgramRunService.Run().

**CURRENT STATE:** Run() меняет процесс-wide CurrentDirectory и восстанавливает в finally.

**TARGET STATE:** Рабочий каталог изолирован через `ProcessStartInfo` или аналогичный механизм.

**FILES:**
- `src/Axiom.Ide/Services/Execution/ProgramRunService.cs`

**ACCEPTANCE CRITERIA:** Environment.CurrentDirectory не меняется во время выполнения программы.

---

## 9. Phase 3 — LANGUAGE METADATA CONSOLIDATION

### TASK-007: Create canonical keyword source

**OBJECTIVE:** Создать единственный источник ключевых слов языка, устранив дублирование в 5 местах.

**CURRENT STATE:** Keywords в TokenKind, SyntaxKind, Lexer, Axiom.xshd, Axiom.Dark.xshd.

**TARGET STATE:** Единый source file, остальные генерируются или ссылаются.

**FILES:**
- `src/Axiom.Lexing/TokenKind.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Lexing/Lexer.cs`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd`
- `src/Axiom.Ide/Editor/Highlighting/Axiom.Dark.xshd`

**IMPLEMENTATION STRATEGY:**
1. TokenKind остаётся canonical source для enum members
2. SyntaxKind генерируется из TokenKind маппинга (eliminate manual duplication)
3. Lexer Keywords dictionary генерируется из TokenKind
4. XSHD файлы разделяют keyword list через shared resource или генерацию

**ACCEPTANCE CRITERIA:** Новое ключевое слово добавляется в 1 место вместо 5.

---

### TASK-008: Fix CanConvert Decimal→Double gap

**OBJECTIVE:** Добавить `Decimal → Double` в матрицу неявных преобразований.

**CURRENT STATE:** `CanConvert()` не включает Decimal→Double, хотя `PromoteNumericType` промотирует Decimal к Double.

**TARGET STATE:** `CanConvert` включает Decimal→Double.

**FILES:**
- `src/Axiom.Semantics/Binding/Binder.Helpers.cs`

**ACCEPTANCE CRITERIA:**
- `DIM x AS DOUBLE = 1.5d` компилируется без ошибок
- Тест на Decimal→Double присваивание проходит

---

## 10. Phase 4 — DEAD CODE CLEANUP

### TASK-009: Remove ArrayAccessExpressionSyntax

**OBJECTIVE:** Удалить мёртвый AST-узел, который никогда не создаётся парсером.

**FILES:**
- `src/Axiom.Syntax/Expressions.cs`
- `src/Axiom.Syntax/SyntaxKind.cs`

**ACCEPTANCE CRITERIA:** Build pass, no references to removed code.

---

### TASK-010: Remove or mark latent opcodes

**OBJECTIVE:** Удалить или пометить неиспользуемые opcodes: Dup, CreateArray, BeginInput/EndInput.

**FILES:**
- `src/Axiom.Bytecode/Model/Opcode.cs`

**ACCEPTANCE CRITERIA:** Build pass, no dead opcode entries.

---

## 11. Phase 5 — TEST SYSTEM

### TASK-011: Add MainWindow.xaml.cs test coverage

**OBJECTIVE:** Извлечь тестируемую логику из code-behind в сервисы и покрыть тестами.

**TARGET STATE:** >50% покрытия логики MainWindow через интерфейсы.

---

### TASK-012: Add E2E integration test

**OBJECTIVE:** Добавить сквозной тест open→compile→run→output через реальные сервисы.

**TARGET STATE:** Хотя бы 1 E2E тест, проходящий за <10s.

---

## 12. Phase 6 — PERFORMANCE

### TASK-013: Optimize lexer position tracking

**OBJECTIVE:** Оптимизировать вычисление line/column с O(T*N) до O(N).

**FILES:**
- `src/Axiom.Lexing/Lexer.cs`

**ACCEPTANCE CRITERIA:** Все 18 lexer тестов проходят. Позиции корректны.

---

## 13. Phase 7 — DOCUMENTATION CONSOLIDATION

Обновление ARCHITECTURE.md — TASK-003.
Остальная документация актуальна.

---

## 14. Phase 8 — TECHNICAL DEBT

### TASK-014: Remove MatchIdentifier/MatchFieldIdentifier duplication

**FILES:**
- `src/Axiom.Parsing/Parser.Helpers.cs`

---

### TASK-015: Fix ArrayDimensionSyntax.Kind

**FILES:**
- `src/Axiom.Syntax/SyntaxKind.cs`
- `src/Axiom.Syntax/Statements.cs`

---

### TASK-016: Remove unused expression parameter in EvaluateChr/EvaluateChrW

**FILES:**
- `src/Axiom.Runtime/Execution/Interpreter.Builtins.String.Char.cs`

---

### TASK-017: Centralize diagnostic message strings

**FILES:**
- Multiple files in `src/Axiom.Runtime/Execution/`

---

## 15. Phase 9 — PERFORMANCE

TASK-013 (lexer optimization).

---

## 16. Phase 10 — FUTURE ARCHITECTURE

### TASK-018: Consider merging XSHD files

### TASK-019: Add centralized CanExecute mechanism

### TASK-020: Evaluate GraphicsCoordinateValidation over-abstraction

---

## 17. Definition of Done

Axiom считается восстановленным до стабильной архитектурной базы, когда:

1. ✅ Архитектурные границы соблюдаются (DAG, no cycles)
2. ✅ Language pipeline согласован (Lexer→Parser→AST→Semantics→Runtime→VM)
3. ⚠️ Runtime semantics согласованы (String builtins — TASK-001)
4. ⚠️ Regressions покрыты (hanging tests — TASK-004)
5. ⚠️ Documentation matches implementation (ARCHITECTURE.md — TASK-003)
6. ✅ IDE integration consistent
7. ⚠️ Duplicate sources of truth устранены (keywords — TASK-007)
8. ⚠️ Critical debt закрыт (MainWindow.xaml.cs — TASK-002)

---

## 18. Validation Strategy

1. Build: `dotnet build Axiom.sln` → 0 errors, 0 warnings
2. Unit tests: `dotnet test` per project → all pass
3. Smoke: `dotnet run --project src/Axiom.Ide.SmokeHarness` → all scenarios pass
4. Manual: Open samples in IDE, verify behavior
5. Performance: Compare Interpreter vs BytecodeVm benchmarks

---

## 19. Final Acceptance Criteria

Проект считается восстановленным, когда:

- Build: 0 errors, 0 warnings
- Tests: все проходят за <120s
- ARCHITECTURE.md: точное отражение фактических зависимостей
- Keyword duplication: ≤2 места (TokenKind + auto-derived)
- String builtins: единственный source of truth
- MainWindow.xaml.cs: <500 строк
- Execution timeout: реализован и протестирован
- E2E test: хотя бы 1 сквозной тест

---

## 20. TOP 20 ACTIONS

1. **[P1] TASK-001** — Fix String builtins divergence: delegate BytecodeVm to BuiltinStringHelpers
2. **[P1] TASK-002** — Split MainWindow.xaml.cs into 4 partial files
3. **[P2] TASK-003** — Update ARCHITECTURE.md dependency rules for Bytecode
4. **[P2] TASK-004** — Investigate and fix hanging Execution/IDE tests
5. **[P2] TASK-005** — Add execution timeout for user programs
6. **[P2] TASK-006** — Fix Environment.CurrentDirectory mutation
7. **[P2] TASK-007** — Create canonical keyword source (eliminate 5-place duplication)
8. **[P2] TASK-008** — Fix CanConvert Decimal→Double gap
9. **[P3] TASK-009** — Remove dead ArrayAccessExpressionSyntax
10. **[P3] TASK-010** — Remove or mark latent opcodes (Dup, CreateArray, BeginInput/EndInput)
11. **[P3] TASK-011** — Add MainWindow.xaml.cs test coverage
12. **[P3] TASK-012** — Add E2E integration test
13. **[P3] TASK-013** — Optimize lexer position tracking to O(N)
14. **[P3] TASK-014** — Remove MatchIdentifier/MatchFieldIdentifier duplication
15. **[P3] TASK-015** — Fix ArrayDimensionSyntax.Kind to use own SyntaxKind
16. **[P3] TASK-016** — Remove unused `expression` parameter in EvaluateChr/EvaluateChrW
17. **[P3] TASK-017** — Centralize diagnostic message strings
18. **[P4] TASK-018** — Consider merging XSHD files into single shared source
19. **[P4] TASK-019** — Add centralized CanExecute mechanism for IDE commands
20. **[P4] TASK-020** — Evaluate GraphicsCoordinateValidation over-abstraction
