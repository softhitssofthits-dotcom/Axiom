# AXIOM — DEEP ARRAY / ARRAY-OPERATIONS FORENSIC AUDIT

## Health Score: 8.8/10

## Executive Summary

Проведён полный forensic-аудит массивной подсистемы Axiom. Проверены: типовая модель, runtime representation, 34 built-in функции, 3 оператора (DIM/REDIM/ERASE), индексирование, slicing, views, multi-dimensional arrays, TYPE array fields, интерпретатор, BytecodeVm, manual (14 страниц), тесты (126+ методов), samples (60+ файлов), engine-compare (8 сценариев).

**Главный вывод: Array subsystem Axiom фундаментально корректна и высококачественна.**

Массивная модель построена на flat `RuntimeValue[]` с row-major strides, произвольными bounds, view-based slicing и comprehensive multi-dimensional support. Общий слой `BuiltinArrayHelpers` (597 строк) обеспечивает единый контракт для обоих движков. 34 built-in функции полностью покрыты.

Найдено **4 подтверждённых дефекта** — все исправлены.

---

## Problem Inventory

| ID | Severity | Category | Component | Problem | Status |
|----|----------|----------|-----------|---------|--------|
| ARRAY-001 | P1 | Parity | Interpreter | ARRAYCONCAT 1D normalizes bounds to [1,N] when one side empty | FIXED |
| ARRAY-002 | P1 | Reliability | Interpreter | Compare crashes on unsupported types instead of diagnostic | FIXED |
| ARRAY-003 | P3 | IDE | Highlighting | ROW/COLUMN/PI predefined constants missing from highlighting | FIXED |
| ARRAY-004 | P3 | Parity | BytecodeVm | Error message indices lack parentheses unlike Interpreter | FIXED |

---

## ARRAY-001: ARRAYCONCAT 1D empty-array bounds parity

**Location:** `src/Axiom.Runtime/Execution/Interpreter.Builtins.Array.cs:815-821`

**Problem:** When one operand is empty, BytecodeVm clones the non-empty side (preserving its bounds), but Interpreter always normalizes to [1, N].

**Fix:** Added empty-array short-circuit to Interpreter matching BytecodeVm: when left is empty, clone right; when right is empty, clone left.

**Test:** Updated `Execute_ArrayConcatAllowsCanonicalEmptyOperands` to match new behavior.

---

## ARRAY-002: Compare error handling parity

**Location:** `src/Axiom.Runtime/Execution/Interpreter.Expressions.cs:423-426`

**Problem:** BytecodeVm catches `InvalidOperationException` from `RuntimeValueHelpers.Compare` and reports a clean diagnostic. Interpreter lets the exception propagate as an unhandled crash.

**Fix:** Added `CompareWithDiagnostics` method that wraps comparison with try-catch, used in expression comparisons and SELECT CASE. Original `Compare` kept as static for sort/binary-search delegates.

**Test:** Existing tests cover comparison behavior. Parity confirmed.

---

## ARRAY-003: ROW/COLUMN/PI IDE highlighting

**Location:** `src/Axiom.Ide/Editor/Highlighting/Axiom.xshd` and `Axiom.Dark.xshd`

**Problem:** `ROW`, `COLUMN`, `PI` predefined constants are documented in manual and used in code, but missing from IDE syntax highlighting.

**Fix:** Added `<Word>COLUMN</Word>`, `<Word>PI</Word>`, `<Word>ROW</Word>` to both light and dark highlighting themes.

---

## ARRAY-004: Error message formatting parity

**Location:** `src/Axiom.Runtime/Execution/BytecodeVm.Arrays.cs:640-667`

**Problem:** BytecodeVm formats array indices as `1, 2` while Interpreter formats as `(1, 2)`. Inconsistent user-facing error messages.

**Fix:** Updated BytecodeVm's `FormatArrayIndices` and `FormatArraySlices` to wrap in parentheses matching Interpreter format.

---

## Array Architecture

### Type Model

`ArrayTypeSymbol` = `(ElementType, Rank)`. No bounds in type. Bounds are runtime-only.

### Runtime Storage

`RuntimeArrayValue` — flat `RuntimeValue[]` with row-major strides:
- Arbitrary lower bounds per dimension
- View-based slicing (shares storage)
- FixedStringLength for STRING arrays
- Fast paths for 1D and 3D in BytecodeVm

### Indexing

1-based by default (compact factories set lower=1). Custom bounds supported. `int.MinValue` forbidden as lower bound.

### Assignment

Array-to-array variable assignment is **reference** (aliasing). Element set copies value through `CopyForStorage`.

### Views

Partial/range indexing creates views that share parent storage. Views cannot be resized (REDIM/ERASE rejected).

---

## Complete Builtin Inventory (34 functions)

| Category | Functions |
|----------|-----------|
| Metadata | LBOUND, UBOUND, ARRAYLENGTH, ARRAYISALLOCATED, ARRAYRANK, ARRAYBOUNDS |
| Copy/Transform | ARRAYCLONE, ARRAYSLICE, ARRAYCONCAT, ARRAYTOSORTED, ARRAYTOREVERSED, ARRAYTAKE, ARRAYSKIP, ARRAYFLATTEN, ARRAYRESHAPE, ARRAYDISTINCT |
| Search | ARRAYCONTAINS, ARRAYINDEXOF, ARRAYLASTINDEXOF, ARRAYCOUNT, ARRAYFINDALL, ARRAYBINARYSEARCH |
| Mutation | ARRAYFILL, ARRAYREVERSE, ARRAYSORT, ARRAYPUSH, ARRAYPOP, ARRAYINSERT, ARRAYREMOVE |
| Callback | ARRAYEVERY, ARRAYSOME, ARRAYFILTER, ARRAYMAP, ARRAYREDUCE |

---

## Interpreter vs BytecodeVm Parity

| Area | Status |
|------|--------|
| DIM/REDIM/ERASE | Identical semantics |
| Element access (Load/Store) | Identical semantics, VM has fast paths |
| ARRAYCONCAT 1D | **FIXED** — now identical |
| Sort/Compare | **FIXED** — Interpreter now handles errors |
| All 34 built-ins | Delegate to BuiltinArrayHelpers |
| Views/slicing | Identical semantics |
| Error messages | **FIXED** — now identical format |

---

## Stable Areas (DO NOT TOUCH)

- `RuntimeArrayValue` — correct model with views, multi-dim, fast paths
- `BuiltinArrayHelpers` — correct shared logic
- All 34 built-in registrations in binder
- View protection (REDIM/ERASE on views → MB3007)
- 1-based indexing default
- Callback built-ins with closures
- Coordinate search for multi-dimensional arrays

---

## Test Coverage

| Category | Tests |
|----------|-------|
| Core lifecycle (DIM/REDIM/ERASE) | 17 |
| Multidimensional arrays | 25+ |
| ARRAYCLONE | 3 |
| ARRAYSLICE | 4 |
| ARRAYCONCAT | 3 |
| ARRAYTOREVERSED | 4 |
| ARRAYTAKE/ARRAYSKIP | 8 |
| Shape builtins (dimension-aware) | 4 |
| ARRAYDISTINCT | 4 |
| ARRAYFLATTEN/ARRAYRESHAPE | 3 |
| ARRAYTOSORTED/ARRAYSORT | 4 |
| ARRAYFILL/ARRAYREVERSE | 7 |
| ARRAYPUSH/POP/INSERT/REMOVE | 17 |
| Callback builtins | 21 |
| Search builtins | 15 |
| BytecodeVm parity | 51+ |
| Execution pipeline | 19 |
| Acceptance | 15+ |
| **Total** | **126+** |

---

## Documentation Coverage

14 manual pages covering all array aspects. All functions in search index, navigation, IDE highlighting, bytecode coverage, performance matrix, specification, and samples. Complete coverage.

---

## Recovery Roadmap

- [x] ARRAY-001 — Fix ARRAYCONCAT 1D empty-array bounds parity
- [x] ARRAY-002 — Fix Compare error handling parity
- [x] ARRAY-003 — Add ROW/COLUMN/PI to IDE highlighting
- [x] ARRAY-004 — Fix error message formatting parity
