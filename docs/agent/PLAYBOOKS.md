# Playbooks

## 1. New Or Changed Built-In Function

Use this when adding or changing a user-visible built-in.

1. Read `docs/PLANS2.md` and `docs/WORKLOG3.md` for the active next step.
2. Fix the contract first in `docs/SPECIFICATION2.md` if it is not already explicit.
3. Update architecture notes only if the implementation shape changes materially.
4. Implement semantic registration and runtime behavior.
5. Add or update:
   - semantic tests
   - runtime tests
   - execution tests when end-to-end coverage matters
   - manual page in `docs/manual/`
   - `docs/manual/scripts/manual.js` when discoverability changes
   - runnable sample in `samples/functions/<name>/`
6. Run targeted validation.
7. Append the session result to `docs/WORKLOG3.md`.

## 2. Observable Language Behavior Change

Use this when semantics, syntax, operators, control flow, `PRINT`, `INPUT`, or built-ins change.

Required checklist:

- update the contract in `docs/SPECIFICATION2.md`
- add or update automated tests
- add or update a runnable `.axm` sample in the correct thematic folder
- update `docs/manual/`
- update `docs/manual/scripts/manual.js` if the new entity should be discoverable
- update `docs/WORKLOG3.md`
- keep `docs/PLANS2.md` aligned with the next step

## 3. IDE Shell Slice

Use this when the language core stays stable and the shell/UX changes.

1. Confirm the slice is really IDE-only.
2. Prefer a small regression-first change.
3. Touch `docs/IDE_REDESIGN.md` or `docs/ARCHITECTURE.md` if shell structure changed materially.
4. Add or update `tests/Axiom.Ide.Tests/`.
5. Use `Axiom.Ide.SmokeHarness` when the change needs live-shell evidence.
6. Record the result in `docs/WORKLOG3.md`.

## 4. Docs-Only Slice

Use this for planning, contracts, or architecture clarification without code edits.

1. Update the canonical doc, not just this agent layer.
2. Keep the change small and explicit.
3. Do not invent unsupported behavior.
4. Append the session to `docs/WORKLOG3.md`.
5. Run no tests unless a generated artifact or script changed.

## 5. Session Restart Shortcut

For fast safe resume:

1. `AGENTS.md`
2. `PROJECT.md`
3. `README.md`
4. `docs/SPECIFICATION2.md`
5. `docs/ARCHITECTURE.md`
6. `docs/PLANS2.md`
7. `docs/WORKLOG3.md`
8. `docs/agent/CURRENT_STATE.md`
9. `docs/agent/ARCHITECTURE_MAP.md`

