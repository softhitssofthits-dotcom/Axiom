# Current State

## Project

`Axiom IDE` is a custom BASIC-like language, interpreter, and WPF desktop IDE on C#/.NET.

## Stable Baseline

- Language core, interpreter, execution pipeline, and minimal IDE shell are already working.
- The repository follows a document-driven workflow.
- User-facing manual lives in `docs/manual/`.
- Runnable `.axm` acceptance samples are mandatory for language-behavior changes.

## Current Product Status

- Axiom v0.1 baseline is stabilized.
- IDE redesign baseline is also stabilized enough for the current scope.
- Stage 1 and Stage 2 of the date/time built-in package are implemented.

## Where Work Stopped

The last completed language slice added:

- `DATE()`
- `TIME()`
- `NOW()`
- `TIMER()`
- `DATESERIAL()`
- `TIMESERIAL()`
- `YEAR()`
- `MONTH()`
- `DAY()`
- `HOUR()`
- `MINUTE()`
- `SECOND()`
- `WEEKDAY()`

## Next Product Step

Before more code, the next date/time contract should be fixed in docs for:

- `DATEADD`
- `DATEDIFF`
- `DATEPART`
- `DATEVALUE`
- `TIMEVALUE`
- `ISDATE`

## Do-Not-Break Constraints

- Interpreter-first architecture stays in place.
- C# / .NET stays the stack.
- Frontend layers stay separated: lexer, syntax, parser, semantics, runtime, IDE.
- Parser remains split by responsibility zones.
- New user-visible language behavior must update tests, manual pages, samples, worklog, and plans.

## First Files To Read For Most Tasks

- `docs/PLANS2.md` for the active next step
- `docs/WORKLOG3.md` for the latest completed slice
- `docs/SPECIFICATION2.md` for behavior contracts
- `docs/ARCHITECTURE.md` for layer boundaries
- `docs/manual/` if behavior is user-visible

