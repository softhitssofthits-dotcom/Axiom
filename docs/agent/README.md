# Agent Layer

## Purpose

This folder is a small OMX-inspired retrieval layer for agent work in this repository.

It does not replace the main project documents.
It exists to make routine project context cheaper to load and safer to resume.

## Source Of Truth

If this folder conflicts with the main repository documents, the main documents win.

Primary sources of truth:

1. `AGENTS.md`
2. `PROJECT.md`
3. `docs/SPECIFICATION3.md`
4. `docs/ARCHITECTURE.md`
5. `docs/PLANS3.md`
6. `docs/WORKLOG4.md`
7. `docs/manual/`

## What Lives Here

- `CURRENT_STATE.md` - compact project snapshot for session startup
- `ARCHITECTURE_MAP.md` - search-first map of code hotspots and test ownership
- `PLAYBOOKS.md` - short repeatable workflows for common task shapes

## Update Rules

- Keep this layer compact and retrieval-oriented.
- Do not duplicate large chunks of `SPECIFICATION3.md` or `ARCHITECTURE.md`.
- Prefer links and summaries over long restatements.
- Update this layer when repeated agent work would benefit from faster project re-entry.
- Do not store temporary chat-only decisions here unless they were accepted into repository docs.

## Intended Workflow

1. Read the mandatory project documents from `AGENTS.md`.
2. Use this folder as a fast re-entry layer.
3. Follow the relevant playbook.
4. Make changes in code and canonical docs.
5. Record outcomes in `docs/WORKLOG4.md`.

## Non-Goals

- No hidden runtime state.
- No generated agent metadata.
- No replacement for `docs/PLANS3.md` or `docs/WORKLOG4.md`.
- No automatic orchestration layer or platform-specific hook dependency.

