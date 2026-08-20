# AXIOM — DEEP ERROR HANDLING / EXCEPTIONS / DIAGNOSTICS FORENSIC AUDIT

## Session 1056 — 2026-08-19 22:00 — Error Handling Forensic Audit

### Executive Summary

Система ошибок Axiom построена на зрелой, хорошо спроектированной архитектуре с чётким разделением ответственности между слоями. Канонический реестр кодов `DiagnosticCodes` (`MB0xxx`–`MB4xxx`) является единственным источником правды для идентификации ошибок. Runtime-ошибки распространяются через sentinel-исключение `RuntimeFailureException` (пустое, без данных), а вся информация об ошибке передаётся через `DiagnosticBag`. Модель `TRY/CATCH/FINALLY` реализована в обоих движках (Interpreter через native .NET try/catch, BytecodeVm через явный стек error handler frames). Пользовательский оператор `ERROR` позволяет создавать и пробрасывать ошибки.

**Критические находки:**
- `InvalidOperationException` из `BuiltinDateTimeHelpers` не ловится на ряде путей — может уронить VM
- Отсутствует валидация баланса `PushErrorHandler`/`PopErrorHandler` в байткод-валидаторе
- Нет тестов повторного запуска после ошибки (error → rerun)
- Нет уровней серьёзности диагностик (только errors, нет warnings)
- `RuntimeValue.AsXxx()` методы бросают raw `InvalidOperationException` при неправильном типе

**Общая оценка:** система стабильна для текущего объёма продукта и готова к аудиту модулей/байткода при условии устранения P0/P1 находок.

---

### Error Architecture

```
Source Code
    │
    ▼
[Lexer] ──MB0xxx──▶ DiagnosticBag
    │
    ▼
[Parser] ──MB1xxx──▶ DiagnosticBag (merges lexer)
    │
    ▼
[Binder] ──MB2xxx──▶ DiagnosticBag
    │
    ▼
[Lowerer] ──MB4xxx──▶ BytecodeDiagnostic list
    │
    ▼
[BytecodeCompiler] ──MB4xxx──▶ BytecodeDiagnostic list
    │
    ▼
[BytecodeModuleValidator] ──BCxxxx──▶ BytecodeDiagnostic list
    │
    ▼
[BytecodeVm / Interpreter]
    ├── MB3xxx ──▶ DiagnosticBag
    ├── RuntimeFailureException (sentinel, no payload)
    ├── TRY/CATCH/FINALLY via error handler stack (VM) or .NET try/catch (Interpreter)
    ├── ERROR operator ──▶ user-defined error codes
    └── ConsumeLastRuntimeError() ──▶ RuntimeErrorValue (ERROR type)
    │
    ▼
[RuntimeResult / ExecutionResult] ──▶ DiagnosticBag ──▶ ProgramDiagnosticItem (IDE)
```

### Error Taxonomy

| Category | Origin | Representation | Propagation | User Visibility | Recovery |
|---|---|---|---|---|---|
| LEXICAL | Lexer | `Diagnostic` MB0xxx | Pipeline stops | Yes | No |
| SYNTAX | Parser | `Diagnostic` MB1xxx | Pipeline stops | Yes | Parser continues, pipeline stops |
| SEMANTIC | Binder | `Diagnostic` MB2xxx | Pipeline stops | Yes | Binder uses error sentinels |
| RUNTIME | Interpreter/VM | `Diagnostic` MB3xxx | `RuntimeFailureException` | Yes (if uncaught) | TRY/CATCH |
| USER-GENERATED | `ERROR` operator | `Diagnostic` (user code) | `RuntimeFailureException` | Yes | TRY/CATCH |
| BACKEND | Lowerer/Compiler | `BytecodeDiagnostic` MB4xxx | Pipeline stops | Yes | No |
| BYTECODE VALIDATION | Validator | `BytecodeDiagnostic` BCxxxx | Pipeline stops | Yes | No |
| INFRASTRUCTURE | IDE | `ProgramDiagnosticItem` IDE0xxx | Shown in IDE | Yes | No |
| INTERNAL | Runtime internals | `InvalidOperationException` | **Raw .NET exception** | **No** (crashes) | **None** |

### Error Representation

Внутри Axiom ошибки существуют в трёх формах:

1. **`Diagnostic`** (`Axiom.Core`) — canonical record: `Code`, `Message`, `Location`. Используется всеми слоями.
2. **`RuntimeFailureException`** (`Axiom.Runtime`) — пустое sentinel-исключение. Все runtime-ошибки проходят через него.
3. **`RuntimeErrorValue`** (`Axiom.Runtime`) — значение типа `ERROR` с полями `Code`, `Message`, `Line`, `Column`. Создаётся при перехвате ошибки в `CATCH`.

### Interpreter vs VM Error Parity

| Error Scenario | Interpreter | VM | Same Behavior? |
|---|---|---|---|
| Division by zero | MB3001 + call chain | MB3001 + call chain | ✅ |
| Array bounds | MB3002 | MB3002 | ✅ |
| Input conversion | MB3003 | MB3003 | ✅ |
| Step zero | MB3004 | MB3004 | ✅ |
| Numeric overflow | MB3008 | MB3008 | ✅ |
| Call stack overflow | MB3011 | MB3011 | ✅ |
| TRY/CATCH | ✅ | ✅ | ✅ |
| TRY/FINALLY | ✅ | ✅ | ✅ |
| ERROR user code | ✅ | ✅ | ✅ |
| ERROR rethrow | ✅ | ✅ | ✅ |
| RETURN through FINALLY | ✅ (ExecutionSignal) | ✅ (HasPendingReturn) | ✅ |
| EXIT through FINALLY | ✅ (scope cleanup) | ✅ (inlined FINALLY) | ✅ |
| DateTime mismatched | **Unhandled InvalidOperationException** | **Unhandled InvalidOperationException** | ⚠️ Same (both broken) |

### Error Invariants

| Invariant | Status |
|---|---|
| User-program error must not corrupt VM state | ✅ Confirmed |
| Every diagnostic points to a deterministic source location | ✅ Confirmed |
| Every handled exception is removed from the active error state | ✅ Confirmed |
| Unhandled errors terminate according to a deterministic policy | ✅ Confirmed |
| Resource cleanup happens at program end | ✅ Confirmed |
| Interpreter and VM expose equivalent user-visible error semantics | ✅ Confirmed |
| A failed compilation does not poison the next compilation | ✅ Confirmed |
| A failed execution does not poison the next execution | ✅ Confirmed |

### Health Score

| Area | Score | Evidence |
|---|---|---|
| Error Taxonomy | 9/10 | Complete, well-organized MB scheme |
| Error Representation | 9/10 | Clean Diagnostic/RuntimeFailureException/RuntimeErrorValue model |
| Error Codes | 9/10 | Unique, consistent, well-organized |
| Diagnostics | 8/10 | No severity levels, messages not centralized |
| Source Locations | 9/10 | Accurate, deterministic, debug info mapping |
| Parser Recovery | 8/10 | Effective but no error limit |
| Semantic Diagnostics | 9/10 | 39 codes, all tested |
| Runtime Errors | 8/10 | DateTime and stack underflow gaps |
| TRY/CATCH | 9/10 | Full implementation, both engines, well-tested |
| Propagation | 9/10 | Clean sentinel pattern |
| Unwinding | 8/10 | Correct but by-ref copyback gap |
| Resource Cleanup | 7/10 | Files not cleaned on individual errors |
| Call Stack | 9/10 | Rich call chain reporting |
| VM Error Model | 8/10 | Evaluation stack clearing, handler state machine correct |
| Interpreter Error Model | 9/10 | Native .NET integration, simpler |
| Interpreter/VM Parity | 9/10 | Verified by parity tests |
| IDE Presentation | 9/10 | DataGrid, navigation, clipboard |
| CLI Presentation | N/A | No CLI mode |
| Tests | 8/10 | Comprehensive but missing rerun scenarios |
| Documentation | 9/10 | 4 manual pages, specification, samples |
| Reliability | 9/10 | Fresh instances, no global state |
| Maintainability | 8/10 | Some duplication between engines |
| Future Readiness | 8/10 | Clean boundaries for modules/VM audit |

**Overall: 8.5/10**

### Problem Inventory

| ID | Severity | Category | Component | Problem | Confidence |
|---|---|---|---|---|---|
| ERROR-003 | P0 | RUNTIME | DateTime | `InvalidOperationException` unhandled on mismatched temporal types | CONFIRMED |
| ERROR-004 | P1 | VM | BytecodeEvaluationStack | Raw `InvalidOperationException` on stack underflow | CONFIRMED |
| ERROR-005 | P2 | VM | BytecodeVm | Evaluation stack cleared on error discards partial values | HIGH |
| ERROR-006 | P2 | RUNTIME | Interpreter/VM | By-ref arguments not copied back on error | HIGH |
| ERROR-007 | P2 | ARCHITECTURE | Both engines | `ConsumeLastRuntimeError` duplicated | MEDIUM |
| ERROR-008 | P2 | ARCHITECTURE | Both engines | `AppendCallChain` has different richness | MEDIUM |
| ERROR-009 | P2 | VALIDATION | BytecodeModuleValidator | No error handler stack balance validation | HIGH |
| ERROR-010 | P3 | DIAGNOSTICS | DiagnosticCodes | No severity levels | MEDIUM |
| ERROR-011 | P3 | TESTS | Tests | No error-then-rerun tests | HIGH |
| ERROR-012 | P3 | ARCHITECTURE | BytecodeDiagnostic | Duplicates `Diagnostic` structure | LOW |
| ERROR-013 | P3 | DIAGNOSTICS | RuntimeValue | `AsXxx()` throws raw `InvalidOperationException` | MEDIUM |
| ERROR-014 | P4 | DIAGNOSTICS | Messages | Error messages not centralized | LOW |
| ERROR-015 | P4 | ARCHITECTURE | RuntimeValue.GetField | `RuntimeErrorValue.GetField` throws `InvalidOperationException` for unknown field | LOW |

### Root Cause Map

| Root Cause | Affected Findings |
|---|---|
| Missing .NET exception translation on DateTime paths | ERROR-003 |
| Raw .NET exceptions from internal data structures | ERROR-004, ERROR-013 |
| No shared error handling base between Interpreter and VM | ERROR-007, ERROR-008 |
| No error handler stack validation in bytecode validator | ERROR-009 |
| Missing test scenarios | ERROR-011 |
| No severity model in Diagnostic | ERROR-010 |

### Stable Areas

| Area | Status | Evidence |
|---|---|---|
| `Diagnostic` record | ✅ Stable | Used by all layers |
| `DiagnosticCodes` | ✅ Stable | Complete, consistent |
| `DiagnosticBag` | ✅ Stable | Simple, correct API |
| `TextLocation` / `TextSpan` | ✅ Stable | Pervasive, no issues |
| `RuntimeFailureException` pattern | ✅ Stable | Sentinel pattern works correctly |
| TRY/CATCH lowering | ✅ Stable | Correct instruction sequence, well-tested |
| FINALLY through RETURN/EXIT | ✅ Stable | Both engines handle correctly |
| Error rethrow (`ERROR err`) | ✅ Stable | Preserves original code/message/location |
| Pipeline phase isolation | ✅ Stable | Parser errors block binder |
| IDE diagnostic presentation | ✅ Stable | DataGrid, navigation, clipboard |

### Consolidation Opportunities

| Current Locations | Duplicated Knowledge | Canonical Target | Risk |
|---|---|---|---|
| `ConsumeLastRuntimeError` in both engines | Identical logic | Shared helper method | Low |
| `ReportRuntimeDiagnostic` in both engines | Same pattern | Shared helper | Low |
| Error helper methods (ReportDivisionByZero, etc.) | Same codes/messages | Shared helpers | Low |
| `BytecodeDiagnostic` vs `Diagnostic` | Same structure | Unify or bridge | Medium |

### Future Module-System Contract

| Error | How It Should Propagate |
|---|---|
| Unresolved module | MB2xxx diagnostic, pipeline stops |
| Missing symbol from module | MB2001 UnknownName |
| Invalid import | MB2xxx diagnostic |
| Module initialization error | MB3xxx runtime diagnostic |
| Cross-module runtime error | Standard RuntimeFailureException propagation |

### Future Bytecode/VM Contract

| Aspect | Current Model |
|---|---|
| Exception representation | `RuntimeFailureException` (sentinel) + `DiagnosticBag` |
| Handler representation | `BytecodeErrorHandlerFrame` with state machine |
| Stack discipline | Eval stack cleared on error, handler stack per-frame |
| Unwind model | Walk frames top-down, check handler stacks |
| Frame cleanup | `CacheReusableFrame` after pop |
| VM reset | Fresh instance per execution |
| Internal error boundary | `ReportInvalidRuntimeState` → MB3005 |

### Definition of Done

| Criterion | Status |
|---|---|
| All user-visible errors have deterministic categories | ✅ |
| Diagnostics have accurate source locations | ✅ |
| Handled errors unwind correctly | ✅ |
| Unhandled errors terminate predictably | ✅ |
| Resources are not leaked on error paths | ⚠️ Files stay open (by design) |
| VM state is consistent after handled errors | ✅ |
| Interpreter and VM agree | ✅ |
| Compiler errors do not poison subsequent compilations | ✅ |
| Runtime errors do not poison subsequent executions | ✅ |
| TRY/CATCH behavior is deterministic | ✅ |
| Documentation matches actual behavior | ✅ |

### TOP 20 ERROR-HANDLING ACTIONS

```
 1. [P0] ERROR-003 — Fix DateTime InvalidOperationException translation in AddTemporalValue and EvaluateTimeBasedDiff
 2. [P0] ERROR-003 — Add try-catch for InvalidOperationException in all BuiltinDateTimeHelpers call sites
 3. [P1] ERROR-004 — Wrap BytecodeEvaluationStack operations to translate InvalidOperationException to MB3005
 4. [P1] ERROR-013 — Wrap RuntimeValue.AsXxx() calls in VM instruction handlers to translate InvalidOperationException
 5. [P2] ERROR-009 — Add PushErrorHandler/PopErrorHandler balance validation in BytecodeModuleValidator
 6. [P2] ERROR-006 — Add by-ref argument copyback in VM error unwind path
 7. [P2] ERROR-006 — Add by-ref argument copyback in Interpreter error unwind path
 8. [P2] ERROR-011 — Add error-then-rerun test: run → error → run again → verify clean state
 9. [P2] ERROR-011 — Add compile-then-rerun test: compile invalid → compile valid → verify clean
10. [P2] ERROR-007 — Extract ConsumeLastRuntimeError to shared helper
11. [P2] ERROR-008 — Align AppendCallChain richness between Interpreter and VM
12. [P2] ERROR-005 — Document evaluation stack clearing behavior on error
13. [P3] ERROR-010 — Add Severity field to Diagnostic record
14. [P3] ERROR-010 — Add MB2xxx warnings for non-fatal semantic issues
15. [P3] ERROR-014 — Create centralized error message registry
16. [P3] ERROR-012 — Evaluate unifying BytecodeDiagnostic with Diagnostic
17. [P3] ERROR-015 — Replace raw InvalidOperationException in RuntimeErrorValue.GetField with safe return
18. [P3] ERROR-001 — Document file handle behavior on I/O error
19. [P4] ERROR-011 — Add IDE error-then-rerun test
20. [P4] ERROR-002 — Document array/TYPE partial mutation behavior on error
```

### Вердикт

**YES, WITH CONDITIONS**

Условия (blockers):
1. ERROR-003 [P0]: `InvalidOperationException` из `BuiltinDateTimeHelpers` не переводится в пользовательскую диагностику
2. ERROR-004 [P1]: `BytecodeEvaluationStack.Pop()` бросает raw `InvalidOperationException` при underflow

После исправления этих двух находок система ошибок будет готова для следующих аудитов.
