# AssetTrack — Architecture

Small, single-user-station **WPF desktop app** (.NET 10) for tracking company equipment
ownership. Portfolio-grade, not enterprise: one solution, one process, one local database.

## Projects

```
AssetTrack.sln
 src/AssetTrack.Core   domain entities, enums, business-rule services (no EF, no UI)
 src/AssetTrack.Data   EF Core DbContext, SQLite, migrations, seeding, repositories
 src/AssetTrack.App    WPF UI (MVVM: Views/UserControls + ViewModels), composition root
 tests/AssetTrack.Tests xUnit tests for Core services against EF Core InMemory
```

Dependency direction: `App -> Core, Data` and `Data -> Core`. Core has no outward
dependencies — it is the testable business-rule boundary.

## Data model

One database, four tables. History is modeled as a single append-only `ActivityLog`
rather than separate "allocation" / "return" / "scrap" tables — every lifecycle event
(created, allocated, returned, marked repair, marked scrap, user created) is one row.
This single table simultaneously powers: dashboard "recent activity", per-asset history,
per-user history, and the admin activity log — one write path, four read views, no
duplicated history bookkeeping.

- **User**: Id, Username (unique idx), PasswordHash, PasswordSalt, DisplayName, Role
  (Admin/Staff), Company, Division, City, IsActive, CreatedAtUtc
- **Asset**: Id, AssetTag (unique idx, e.g. `AT-1001`), Name, Category, SerialNumber
  (unique idx), Status (Available/Assigned/Repair/Scrap), CurrentUserId (nullable FK,
  idx), Company, Division, City, CreatedAtUtc
- **ActivityLog**: Id, TimestampUtc (idx), ActorUserId (who performed it), EventType
  (Created/Allocated/Returned/MarkedRepair/MarkedScrap/UserCreated), AssetId (nullable,
  idx), TargetUserId (nullable, idx — the holder), Condition (nullable), Notes
- Indexes also on Asset.Name, Asset.Status, Asset.CurrentUserId and
  User.Company/Division/City to keep search server(DB)-side and sub-linear.

SQLite file lives under `%LocalAppData%\AssetTrack\assettrack.db`, created and seeded
automatically on first run via `dbContext.Database.Migrate()` + a seed check.

## Business rules (enforced in Core services, inside EF transactions)

- **Allocate**: asset must be `Available`; sets `Assigned` + `CurrentUserId`; copies
  Company/Division/City from the target user; writes `Allocated` log row.
- **Return**: asset must be `Assigned`; condition decides next status
  (Good→Available, Damaged/NeedsRepair→Repair, Scrap→Scrap); clears `CurrentUserId`
  while the log row keeps who held it; writes `Returned` log row.
- **Scrap**: asset must be `Available` or `Repair`; sets `Scrap`; writes `MarkedScrap`
  log row. Scrapped/returned assets are never deleted — only status changes.
- **Create asset**: serial number and asset tag must be unique; starts `Available`.
- All three mutating operations run inside a single `DbContext` SaveChanges
  transaction so status + log write together or not at all.
- Services take `IDbContextFactory<AssetTrackDbContext>`, not a shared `DbContext`,
  and create/dispose one per call (`await using var db = await dbFactory.CreateDbContextAsync()`).
  A WPF sign-in session is long-lived and multiple ViewModels/dialogs can be loading
  at once (e.g. typing in a search box, or a dialog opening while a page is still
  loading) — EF Core does not allow concurrent operations on one `DbContext`
  instance, so a shared one would eventually crash under exactly that kind of
  overlap. This is the standard fix for a long-lived WPF/Blazor component graph.

## Auth & authorization

- Passwords: PBKDF2 (`Rfc2898DeriveBytes`, SHA-256, 100k iterations) + per-user random
  salt. No plaintext, no third-party auth library needed for 6 local demo accounts.
  Sign-in checks a missing/inactive account against a fixed dummy hash before failing,
  so the response time doesn't leak which usernames exist.
- Session: an in-process `ISessionContext` singleton holds the signed-in user for the
  life of the app (desktop app — no cookies/tokens needed).
- Roles: `Admin` (asset/user CRUD, allocate, return, scrap, activity log) vs `Staff`
  (own "My Assets" page + own dashboard summary only, reusing the same
  currently-assigned/full-history view an Admin sees for any user, scoped to
  themselves). Enforced twice: ViewModels gate which pages/buttons are reachable at
  all (`ShellViewModel.GoAssets` routes Staff to their own scoped page instead of the
  org-wide asset list), and every mutating `Ef*Service` method
  (`AllocateAsync`/`ReturnAsync`/`MarkScrapAsync`/`CreateAsync` on both assets and
  users) independently loads the acting user and rejects non-Admin callers via
  `AuthorizationGuard.IsAdminAsync` — a caller cannot reach a privileged write by
  bypassing the UI.

## Deviation from the Claude Design mockup

The mockup (`.claude/design/AssetTrack.html`) is a static prototype: signing in as a
staff user still renders the full admin dashboard, "Allocate asset" / "Record return"
buttons, and the company-wide user list, because the mock has no real authorization.
Per the brief's rule ("deviate only for a genuine security/usability issue, smallest
change, document it"): the **Staff** experience in the real app is scoped down —
dashboard shows only that user's own assets, no allocate/return/scrap actions, no
Users or Activity nav items. Visual language (colors, cards, typography) is unchanged.

## Search & performance

All list screens (Assets, Users) filter/sort/paginate via `IQueryable` `Where`/`OrderBy`
/`Skip`/`Take` translated to SQL — never `ToList()` before filtering. Combined with the
indexes above this keeps search fast without an in-memory cache or search engine,
appropriate for a small dataset (hundreds–low thousands of assets).

## Testing

xUnit against `Microsoft.EntityFrameworkCore.InMemory` covers the business rules above:
duplicate serial rejection, allocate-twice rejection, return-updates-status-by-condition,
scrap-preserves-history, unauthorized staff actions rejected.

## Security notes

Most classic web risks (CSRF, CSP, CORS, XSS-via-DOM) don't apply to a WPF desktop app
with no HTTP surface. What does apply and is implemented: parameterized queries only
(EF Core LINQ, no raw SQL), input validation on all forms, PBKDF2 password hashing, no
secrets in source control, no sensitive data in exceptions shown to the user (a generic
error is shown; details go to a local log file only), local SQLite file scoped to the
OS user profile.
