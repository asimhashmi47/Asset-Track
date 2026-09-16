# Agent roster

Six specialists. **`principal-engineer` orchestrates and is the only role that modifies implementation code**; the rest report findings.

Full documentation, install steps and copy-paste usage prompts live in the [project README](../../README.md).

| Agent | Owns | Writes to |
|---|---|---|
| `principal-engineer` | Implementation, sequencing, fix loop, final integration | `.claude/plans/`, `.claude/context/`, `.claude/reports/` |
| `software-architect` | Architecture, boundaries, dependency direction | `.claude/architecture/`, `.claude/decisions/` |
| `qa-engineer` | Testing strategy, tests, regression gate | `.claude/qa/` |
| `security-engineer` | Attack surface, security gate | `.claude/security/` |
| `code-reviewer` | Code quality, performance, architecture adherence | `.claude/reviews/` |
| `real-user` | Usability, workflow friction, acceptance gate | `.claude/ux/` |

## Flow

PLAN → ARCHITECTURE CHECK → IMPLEMENT → TARGETED PARALLEL REVIEW → FIX → VERIFY → SHIP

## Spawning rule — do not activate all six by default

Default is **1 principal + the specialists the change actually warrants**:

- Simple UI change — `principal-engineer` + `real-user` + `code-reviewer`
- Auth / permissions change — `principal-engineer` + `security-engineer` + `qa-engineer` + `code-reviewer`
- Database-heavy change — `principal-engineer` + `software-architect` + `qa-engineer` + `code-reviewer`
- Large architectural change — `principal-engineer` + `software-architect` + `security-engineer` + `qa-engineer` + `code-reviewer`

Run independent reviews in parallel. Never parallelize work that depends on another agent's result.

## Shared rules every agent follows

1. Read `.claude/context/project-context.md` first; never re-derive known context.
2. Start from `git status` / `git diff` — inspect changed files first, never the whole repo.
3. No duplicate analysis, no repository-wide scans, no unrelated refactors.
4. Use the standard finding format; never inflate severity.
5. Durable output goes in `.claude/`, not the chat. Keep reports concise.
6. Stop when your responsibility is complete.
