# AssetTrack

A small WPF desktop app for tracking company equipment (laptops, monitors, cables, etc.) —
who has what, and the full history of every allocation, return, and disposal.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the design, data model, and business rules.

## Run it

Requires the .NET 10 SDK on Windows.

```bash
dotnet run --project src/AssetTrack.App/AssetTrack.App.csproj
```

On first run it creates and seeds a local SQLite database at
`%LocalAppData%\AssetTrack\assettrack.db`.

## Demo accounts

| Username | Password  | Role  |
|----------|-----------|-------|
| admin    | admin123  | Admin |
| arjun    | user123   | Staff |
| sara     | user123   | Staff |
| daniel   | user123   | Staff |
| lena     | user123   | Staff |
| rohit    | user123   | Staff (inactive) |

## Tests

```bash
dotnet test tests/AssetTrack.Tests/AssetTrack.Tests.csproj
```
