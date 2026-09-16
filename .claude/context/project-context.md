# AssetTrack — Project Context

**Purpose:** Small portfolio-grade WPF desktop app tracking company equipment ownership
(who has what asset, full allocation/return/scrap history). Not enterprise scope.

**Tech stack:** .NET 10, WPF (MVVM via CommunityToolkit.Mvvm), EF Core + SQLite,
Microsoft.Extensions.DependencyInjection, xUnit tests.

**Architecture (see ARCHITECTURE.md for detail):**
- `src/AssetTrack.Core` — entities, enums, service interfaces, PBKDF2 password hashing.
  No EF/UI dependency; the testable boundary.
- `src/AssetTrack.Data` — EF Core `AssetTrackDbContext`, entity configs/indexes,
  service implementations (`EfAssetService`, `EfUserService`, `EfAuthService`,
  `EfActivityLogService`), migrations, `DbSeeder`.
- `src/AssetTrack.App` — WPF UI: `Views/` (XAML + code-behind), `ViewModels/`,
  `Navigation/` (INavigationService, IDialogService — swap the shell's current page /
  open modal dialogs), `Converters/`, `Styles/` (Colors, Typography, Controls
  ResourceDictionaries), composition root in `App.xaml.cs`.
- `tests/AssetTrack.Tests` — xUnit against EF Core InMemory, covering Core business
  rules in `AssetTrack.Data.Services`.

**Database:** Single SQLite file at `%LocalAppData%\AssetTrack\assettrack.db`, created
via `Database.MigrateAsync()` + `DbSeeder.SeedAsync` on first run. One append-only
`ActivityLog` table (not separate allocation/return/scrap tables) drives dashboard
activity, asset history, user history, and the admin activity log from a single write
path. Indexes: `Asset.AssetTag`/`SerialNumber` (unique), `Asset.Name`/`Status`/
`CurrentUserId`, `User.Username` (unique), `User.Company`/`Division`/`City`,
`ActivityLog.TimestampUtc`/`AssetId`/`TargetUserId`.

**Auth model:** PBKDF2 (SHA-256, 100k iterations) + per-user salt, no plaintext ever
stored. In-process `ISessionContext` singleton holds the signed-in user (desktop app,
no cookies/tokens). Process-lifetime in-memory brute-force throttle in `EfAuthService`
(5 failed attempts → 30s lockout per username), via `ConcurrentDictionary`.

**Authorization model:** Two roles, `Admin` and `Staff`. Enforced in ViewModels
(`IsAdmin` gates nav items, action buttons) and re-checked in Data-layer services
(defense in depth). Staff: own dashboard + own asset history only, no allocate/
return/scrap/user-management/activity-log access. This intentionally diverges from
the static Claude Design mockup (which has no real RBAC) — documented in
ARCHITECTURE.md as an authorized deviation.

**Business rules (in `EfAssetService`, single `SaveChangesAsync` = implicit
transaction per operation):**
- Allocate: asset must be `Available`; sets `Assigned` + `CurrentUserId`; copies
  Company/Division/City from the target user; writes `Allocated` log row.
- Return: asset must be `Assigned`; condition decides next status
  (Good→Available, Damaged/NeedsRepair→Repair, Scrap→Scrap); clears `CurrentUserId`;
  writes `Returned` log row (log keeps who held it).
- Scrap: any status except already `Scrap`; writes `MarkedScrap` log row; previous
  owner preserved in the log, never deleted.
- Create asset: unique serial number enforced; auto-generated sequential `AssetTag`.

**Seed data:** 6 users (`admin`/admin123 = Admin; `arjun`/`sara`/`daniel`/`lena`/
`rohit` = Staff, all password `user123`, `rohit` seeded inactive) + 18 assets, mirrored
from the Claude Design mockup's demo data (`.claude/design/AssetTrack.html`).

**Current milestone:** Initial build complete (Phase 1–2 of original brief). Manually
verified live in the running app: login (admin + staff), dashboard stats/refresh,
asset list/search/detail/history, allocate → dashboard refresh, return → status-by-
condition, users list with live asset counts, staff-role UI restriction. All 16 xUnit
tests pass, solution builds with 0 warnings/errors.

**Known/fixed issues from initial build (do not re-report):**
- WPF cannot bind to named `ValueTuple` elements at runtime — fixed by replacing with
  a `CategoryBar` record (`ViewModels/CategoryBar.cs`).
- `DataGrid.InputBindings` don't reliably resolve `RelativeSource` bindings for
  double-click-to-open — replaced with a code-behind `MouseDoubleClick` handler in
  `AssetsListView.xaml.cs` / `UsersListView.xaml.cs`.

**Not yet done / open for this review pass:** No formal Architecture/Security/Code
Review/Real-User gate has been run yet. Repository is not yet a git repo (no commits,
no `git diff` available — treat the full `src/`/`tests/` tree as "the change" for
review scope, but avoid re-deriving what's already documented here).

**Constraints:** Small-project scope — no microservices, no CQRS, no event sourcing,
no unnecessary abstractions. Prefer the simplest production-quality solution.
