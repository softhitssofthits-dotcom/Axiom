# Architecture Map

## Code Hotspots

### Language Frontend

- `src/Axiom.Lexing/` - tokenization
- `src/Axiom.Syntax/` - syntax tree and syntax node model
- `src/Axiom.Parsing/` - parser split by responsibility
- `src/Axiom.Semantics/` - symbols, scopes, binding, semantic diagnostics

### Runtime And Execution

- `src/Axiom.Runtime/` - runtime values, interpreter, built-ins, runtime diagnostics
- `src/Axiom.Execution/` - `parse -> bind -> execute` orchestration boundary

### IDE

- `src/Axiom.Ide/` - WPF shell, document handling, output/diagnostics, commands, services
- `src/Axiom.Ide.SmokeHarness/` - automated live WPF smoke runner

## Test Ownership Map

- `tests/Axiom.Lexing.Tests/` - lexer behavior
- `tests/Axiom.Parsing.Tests/` - parser behavior and syntax diagnostics
- `tests/Axiom.Semantics.Tests/` - binder rules and semantic diagnostics
- `tests/Axiom.Runtime.Tests/` - interpreter and built-in runtime behavior
- `tests/Axiom.Execution.Tests/` - end-to-end language pipeline
- `tests/Axiom.Ide.Tests/` - non-UI and shell-level IDE regressions

## Manual And Acceptance Samples

- `docs/manual/` - user-facing reference and getting-started docs
- `docs/manual/scripts/manual.js` - manual index/search metadata
- `samples/functions/<name>/` - runnable built-in acceptance samples
- `samples/operators/<name>/` - runnable operator acceptance samples
- `samples/control-flow/<name>/` - runnable control-flow acceptance samples
- `samples/expressions/<name>/` - runnable expression acceptance samples
- `samples/smoke/` - integrated runnable scenarios

## Fast Lookup By Task Type

### Add Or Change Built-In Function

Look at:

- `src/Axiom.Semantics/Symbols/BuiltinFunctionId.cs`
- `src/Axiom.Semantics/Symbols/Builtins/`
- `src/Axiom.Runtime/Execution/Interpreter.Builtins*.cs`
- `tests/Axiom.Semantics.Tests/`
- `tests/Axiom.Runtime.Tests/`
- `tests/Axiom.Execution.Tests/`
- `docs/manual/`
- `samples/functions/`

### Change Syntax Or Parsing

Look at:

- `src/Axiom.Syntax/`
- `src/Axiom.Parsing/`
- `tests/Axiom.Parsing.Tests/`
- `docs/SPECIFICATION2.md`

### Change Semantic Rules

Look at:

- `src/Axiom.Semantics/`
- `tests/Axiom.Semantics.Tests/`
- `docs/SPECIFICATION2.md`

### Change IDE Shell Behavior

Look at:

- `src/Axiom.Ide/`
- `tests/Axiom.Ide.Tests/`
- `src/Axiom.Ide.SmokeHarness/`
- `docs/IDE_REDESIGN.md`
- `docs/ARCHITECTURE.md`

## Retrieval Notes

- Prefer this file for orientation, not for detailed contracts.
- For exact language behavior, always fall back to `docs/SPECIFICATION2.md`.
- For exact session continuity, always fall back to `docs/WORKLOG3.md`.

