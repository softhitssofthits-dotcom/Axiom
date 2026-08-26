# AXIOM — DEEP MODULES / NAMESPACES / PROGRAM STRUCTURE FORENSIC AUDIT

## Session 1056 — 2026-08-19 23:30 — Modules / Namespaces / Program Structure Forensic Audit

### Executive Summary

Axiom — строго однофайловый язык. Программа = один `.axm` файл. Нет модулей, нет namespaces, нет imports, нет visibility modifiers, нет project file format. Это не баг, а текущий языковой контракт, зафиксированный в `PROJECT.md` как "система проектов из множества модулей" — отдельное направление, требующее проектирования.

Pipeline: `string SourceText → Parser.Parse → ProgramSyntax → Binder.Bind → BoundProgram → Lower → BytecodeModule → Execute`. На каждом этапе ровно одна единица компиляции.

Entry point — глобальные операторы (top-level statements), выполняемые последовательно сверху вниз. Нет `SUB Main`.

Symbol table — единый `_globalScope` с nested lexical scopes. Все top-level SUB/FUNCTION/TYPE регистрируются в `_globalScope` перед привязкой тел. Forward references работают.

**Вердикт: YES.** Модель программы стабильна для текущего объёма. Отсутствие модулей/namespaces — feature gap, не дефект.

---

### Definition of Axiom Program

Программа Axiom = одна строка `string SourceText`. Нет file paths, нет module names, нет project descriptors.

| Layer | Program Representation |
|---|---|
| Syntax | `ProgramSyntax` — flat list of `MemberSyntax` (GlobalStatement, Sub, Function, Type) |
| Semantics | `BoundProgram` — `GlobalStatements` + `Routines` |
| Execution | `CompiledProgram` — SourceText + Syntax + BoundProgram + LoweredProgram + BytecodeModule |
| Runtime | `Interpreter.Execute(BoundProgram)` or `BytecodeVm.Execute(BytecodeModule)` |

### Source File Model

`.axm` файл — plain text, UTF-8, расширение `.axm`. Файл = программа. Нет headers, нет module declarations, нет imports.

Члены программы (`MemberSyntax`):
- `GlobalStatementSyntax` — любой оператор на верхнем уровне
- `SubDeclarationSyntax` — `SUB ... END SUB`
- `FunctionDeclarationSyntax` — `FUNCTION ... END FUNCTION`
- `TypeDeclarationSyntax` — `TYPE ... END TYPE`

### Module / Namespace / Import Model

**Не существуют.** Нет `MODULE`, `NAMESPACE`, `IMPORT`, `USE`, `FROM`, `ALIAS`, `PUBLIC`, `PRIVATE`, `INTERNAL`, `EXPORT` keywords в lexer (100 keywords, ни одного модульного).

### Entry Point

Глобальные операторы (top-level statements). Нет `SUB Main`.
- Interpreter: `ExecuteProgram()` → `ExecuteStatements(Program.GlobalStatements)`
- BytecodeVm: entry point routine с именем `"<global>"`, `id: 0`, `isEntryPoint: true`

### Symbol Registration

Multi-pass в `BindProgram()`:

1. `DeclareTypeNames()` — регистрирует имена TYPE (пустые символы)
2. `PrepareTypeDeclarations()` — разрешает поля TYPE, вычисляет глобальные integral constants
3. `DeclareFunctions()` — регистрирует все top-level SUB/FUNCTION в `_globalScope`
4. `DeclareTypeMethods()` — регистрирует методы, конструкторы, инварианты TYPE
5. Main loop — привязывает тела

Forward references работают благодаря multi-pass.

### Visibility Model

Нет visibility modifiers. Есть внутренние флаги:
- `VariableSymbol.IsGlobal` — `true` если объявлена на верхнем уровне
- `FunctionSymbol.IsLocal` — `true` если `ContainingRoutine != null`

### Scope Chain

`BoundScope` с тремя parent pointers:
- `EnclosingScope` — direct parent
- `VariableParent` — для variable lookup chain
- `FunctionParent` — для function lookup chain

Case-insensitive (`StringComparer.OrdinalIgnoreCase`). Shadowing разрешён с диагностикой MB2016.

### Determinism

Build детерминистичен. Нет filesystem dependencies, нет dictionary ordering issues. Recompilation после ошибки — clean state (fresh Binder, fresh DiagnosticBag).

### Health Score

| Area | Score |
|---|---|
| Program Model | 9/10 |
| Source File Model | 9/10 |
| Symbol Registration | 9/10 |
| Resolution | 9/10 |
| Entry Point | 9/10 |
| Determinism | 10/10 |
| Tests | 8/10 |
| Documentation | 8/10 |
| Future Readiness | 7/10 |

**Overall: 8.5/10**

### Problem Inventory

| ID | Severity | Category | Problem | Confidence |
|---|---|---|---|---|
| MODULE-001 | P2 | PROGRAM | No multi-file support (feature gap) | CONFIRMED |
| MODULE-002 | P2 | PROGRAM | No project file format (feature gap) | CONFIRMED |
| MODULE-003 | P2 | PROGRAM | No import/module system (feature gap) | CONFIRMED |
| MODULE-004 | P3 | PROGRAM | No visibility modifiers (feature gap) | CONFIRMED |
| MODULE-005 | P3 | PROGRAM | No namespace system (feature gap) | CONFIRMED |
| MODULE-006 | P3 | DIAGNOSTICS | No warning for declaration-only programs | LOW |
| MODULE-007 | P4 | IDE | File tree is raw filesystem browser | LOW |

**Все P2/P3 findings — feature gaps, не bugs.** Система стабильна для текущего однофайлового контракта.

### Stable Areas

| Area | Status |
|---|---|
| Single-file compilation pipeline | ✅ Stable |
| Symbol registration (multi-pass) | ✅ Stable |
| Scope chain | ✅ Stable |
| Entry point (global statements) | ✅ Stable |
| Built-in function registration | ✅ Stable |
| TYPE two-phase construction | ✅ Stable |
| Error sentinel pattern | ✅ Stable |

### Program-Structure Invariants

| Invariant | Status |
|---|---|
| Every program is a single source text string | ✅ Confirmed |
| Every program has exactly one entry point | ✅ Confirmed |
| All top-level symbols share one global scope | ✅ Confirmed |
| Forward references work for functions and types | ✅ Confirmed |
| Build is deterministic | ✅ Confirmed |
| Recompilation after error produces clean state | ✅ Confirmed |
| IDE and pipeline use the same single-file model | ✅ Confirmed |

### Future Bytecode / VM Contract

Bytecode/VM audit должен проверить:
- Routine IDs (0 = entry point, 1..N = user functions)
- Global slots для глобальных переменных
- Type IDs для user-defined types
- Function IDs для вызовов
- Entry point validation (`<global>` routine)
- Single-module assumption в BytecodeModule

### Вердикт

**YES** — модель программы стабильна для перехода к Bytecode/VM audit. Отсутствие модулей/namespaces — feature gap, не blocker.
