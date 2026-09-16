# Fix Loop Triage — Post-Build Review Gates

**Status: all 14 FIX NOW items implemented, verified by tests (36/36 passing, 6 new
authorization tests) and live app re-verification. See MILESTONE STATUS below.**

Findings from Architecture, Security, Code Review, QA, and Real-User agents (parallel,
scoped review of the initial AssetTrack build). Deduplicated below; each item lists every
agent that independently flagged it.

## FIX NOW

| # | Finding | Flagged by | Severity |
|---|---|---|---|
| 1 | No data-layer authorization check on mutating services | ARCH-01, SEC-01, CR-01, QA-03 | HIGH |
| 2 | Staff sees org-wide unfiltered data (dashboard/assets), contradicts brief | SEC-02 | HIGH |
| 3 | Session-long shared DbContext + fire-and-forget loads → concurrency crash risk | ARCH-02, CR-03 | HIGH |
| 4 | No global exception handling — DB errors crash app or fail silently | CR-02 | HIGH |
| 5 | **Dialog Save/Cancel buttons clipped off-screen (Add User unusable, Add Asset/Scrap clipped on error)** | Real-User | CRITICAL (functional) |
| 6 | Return/Scrap leave stale Division/City on the asset | CR-04 | MEDIUM |
| 7 | EfUserService.CreateAsync non-atomic (2 SaveChanges) | ARCH-03, CR-05, QA-04 | MEDIUM |
| 8 | Auth lockout never resets after expiry — repeated lockouts | QA-05 | LOW (real bug) |
| 9 | Scrap reason/notes captured but never displayed anywhere | Real-User | MEDIUM |
| 10 | Search boxes have no visible placeholder (regression from my own earlier fix) | Real-User | MEDIUM |
| 11 | "Since <date>" on asset detail shows CreatedAtUtc, not allocation date | Real-User | MEDIUM |
| 12 | Sign-out control is an unlabeled glyph | Real-User | LOW |
| 13 | Scrap confirmation doesn't name the holder or say "cannot be undone" clearly | Real-User | LOW |
| 14 | Docs (ARCHITECTURE.md, project-context.md) claim controls that don't exist | ARCH-01/04 | — (fixed as side effect of #1) |

## DEFERRED (documented, not blocking this milestone)

- QA-01 (allocation race / no concurrency token): real for multi-instance use; this is a
  single-user desktop app against a local file. Documented as a residual assumption.
- SEC-03 (demo credentials shipped unconditionally): acceptable per brief — "Demo
  credentials may be seeded because this is a portfolio demo." Already stated in README.
- SEC-04 (auth timing oracle), SEC-06 (hash fields on in-memory entities), SEC-07 (DB not
  encrypted at rest): low-value hardening for a local single-user demo app.
- QA-02/CR-07 (tag generation not atomic, unhandled DbUpdateException on collision): real
  but very low probability at this scale (sequential single-writer desktop app); noted as
  a residual risk rather than fixed now.
- QA-06/QA-07 (LIKE wildcard escaping, case-sensitive serial dedup): cosmetic edge cases.
- CR-06 (window-close vs sign-out ambiguity): minor UX, deferred.
- Real-User: no Role column in Users list, Role-picker help text, post-save scroll-to-new-
  row, "1 assets tracked" grammar, new-asset activity timestamp shows midnight, DatePicker
  format inconsistency with the rest of the app: cosmetic polish, deferred.
- ARCH-05/06, CR-08 (tag-gen client-side max, DashboardSummary tuple, dead enum value,
  unused categoryFilter param): low-value cleanup, deferred.

## REJECTED

- None — every CRITICAL/HIGH/MEDIUM finding was actionable and consistent with a small
  portfolio app's scope; nothing was inflated or out of scope.
