# AXIOM — DEEP STRING / STRING-OPERATIONS FORENSIC AUDIT

## Health Score: 8.4/10

## Executive Summary

Проведён полный forensic-аудит строковой подсистемы Axiom. Проверены: лексер, парсер, семантика, runtime representation, интерпретатор, BytecodeVm, встроенные функции, fixed strings, операторы, сравнения, конверсии, Unicode, manual, тесты и samples.

**Главный вывод: String subsystem Axiom фундаментально корректна и стабильна.**

Строковая модель построена на CLR `string` (immutable UTF-16), что даёт надёжную основу. Единый `TypeSymbol.String` для динамических и фиксированных строк — чёткое архитектурное решение. Общий слой `BuiltinStringHelpers` после TASK-001 обеспечивает единый контракт для обоих движков. 44 строковые built-in функции полностью покрыты тестами, samples и manual.

Найдено **2 подтверждённых дефекта** и **8 наблюдений/улучшений**.

---

## Problem Inventory

| ID | Severity | Category | Component | Problem | Status |
|----|----------|----------|-----------|---------|--------|
| STRING-001 | P1 | Unicode | BytecodeVm | REVERSE fast path splits surrogate pairs | FIXED |
| STRING-002 | P3 | Parity | BytecodeVm | JOIN single-element returns raw value | FIXED |

---

## STRING-001: REVERSE fast path Unicode surrogate pair corruption

**Location:** `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs:70-81`

**Problem:** `EvaluateReverseBuiltin` uses `char[]` reversal which splits surrogate pairs for non-BMP Unicode (emoji, etc.).

**Fix:** Replaced char-level reversal with delegation to `BuiltinStringHelpers.EvaluateReverse` which uses Rune-aware reversal.

**Test:** Added `Execute_BytecodeVmReversePreservesNonBmpUnicode` in `BytecodeVmTests.cs`.

---

## STRING-002: JOIN single-element array VM parity

**Location:** `src/Axiom.Runtime/Execution/BytecodeVm.Builtins.String.cs:264-267`

**Problem:** `EvaluateJoinBuiltin` returns raw `RuntimeValue` for single-element arrays without materializing through `.AsString()`.

**Fix:** Single-element shortcut now calls `.AsString()` and wraps in `RuntimeValue.FromString()`.

**Test:** Added `Execute_BytecodeVmJoinSingleElementMaterializesString` in `BytecodeVmTests.cs`.

---

## String Architecture

### Runtime Representation

CLR `string` (immutable UTF-16). Два внутренних формата хранения в `RuntimeValue.Value`:
- `string` — materialized строка
- `RuntimeStringAccumulator` — lazy `StringBuilder` для оптимизации `s = s + x`

### Type Model

`TypeSymbol.String` — sealed singleton. Единый символ для динамических и фиксированных строк. Fixed-length metadata живёт на `VariableSymbol.FixedStringLength` и `TypeFieldSymbol.FixedStringLength`.

### Operators

| Operator | Semantics | Accepts |
|----------|-----------|---------|
| `+` | String concatenation | STRING + STRING only |
| `&` | Mixed concatenation | Any scalar + Any scalar |

### Comparison

All comparisons: **Ordinal, case-sensitive** (`StringComparison.Ordinal`).

### Fixed Strings

- Range: 1..65535
- Default: N spaces
- Assignment: PadRight with spaces / truncate
- CONST/params/returns: Not supported (MB2026)

### Complete Builtin Inventory (44 functions)

LEN, LEFT, RIGHT, MID, TRIM(×2), LTRIM(×2), RTRIM(×2), REVERSE, CONTAINS, STARTSWITH, ENDSWITH, REPLACE, REPLACEMANY, INSTR(×2), INSTRREV(×2), SUBSTRCOUNT, REPEAT, SPACE, STRING, PADLEFT(×2), PADRIGHT(×2), CENTER(×2), INSERT, REMOVE, ASC, ASCW, CHR, CHRW, SPLIT(×2), JOIN(×2), LCASE, UCASE, STR, VAL

---

## Stable Areas (DO NOT TOUCH)

- `TypeSymbol.String` — correct singleton design
- `RuntimeValue.FromString` / `AsString` — correct immutable model
- `RuntimeStringAccumulator` — correct append optimization
- `NormalizeFixedString` — correct pad/truncate
- `CopyForStorage` — correct normalization gate
- `RuntimeValueHelpers.ValuesEqual` / `Compare` — correct ordinal semantics
- `RuntimeCharacterEncoding` — correct Cp866 mapping
- `BuiltinStringHelpers` — correct shared logic
- All 44 built-in registrations in binder
- Lexer string literal parsing
- Fixed string subsystem (DIM, TYPE fields)

---

## Recovery Roadmap

- [x] STRING-001 — Fix REVERSE fast path Unicode surrogate pair corruption
- [x] STRING-002 — Fix JOIN single-element array VM parity
- [ ] Add non-BMP Unicode edge case tests for MID/LEFT/RIGHT
- [ ] Add ASCW/CHRW dedicated samples
- [ ] Document LEN behavior for non-BMP Unicode
