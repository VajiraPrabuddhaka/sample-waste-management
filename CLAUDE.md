# Smart Waste Management Dashboard — CLAUDE.md

## What this project is

A .NET 10 demo application showing OpenChoreo observability (structured logs, distributed traces, custom metrics). It simulates IoT waste bin sensors and collection truck fleets. Backend is a REST API; frontend is Blazor WebAssembly.

## Key ports

| Service  | URL                        |
|----------|----------------------------|
| Backend  | `http://localhost:5230`    |
| Frontend | `http://localhost:5231`    |
| OTLP     | `http://localhost:4317`    |
| Swagger  | `http://localhost:5230/openapi` |

## How to run

Both services must be running simultaneously — they are separate processes:

```bash
# Terminal 1 — backend
dotnet run --project backend/WasteManagementApi/

# Terminal 2 — frontend
dotnet run --project frontend/WasteManagementDashboard/
```

Build the whole solution from the repo root:

```bash
dotnet build
```

## Project structure

```
backend/WasteManagementApi/
  Controllers/        # BinsController, TrucksController, CollectionsController,
                      # AlertsController, DashboardController, SimulationController
  Data/               # WasteManagementDbContext (EF Core + SQLite), DbSeeder
  DTOs/               # Record types for all API request/response shapes
  Middleware/         # RequestLoggingMiddleware (correlation IDs, timing headers)
  Models/             # EF Core entities: WasteBin, Truck, Collection, Alert,
                      # FillLevelReading, Enums
  Services/           # SensorSimulatorService (background), AlertMonitorService (background),
                      # SimulationStateService (singleton state)
  Telemetry/          # WasteManagementMetrics (custom OTel meters), WasteManagementActivitySource (custom spans)
  Program.cs          # All DI registrations, middleware pipeline, Serilog, OpenTelemetry
  appsettings.json    # Serilog config, SQLite connection string

frontend/WasteManagementDashboard/
  Models/             # C# records mirroring backend DTOs (no navigation properties)
  Services/           # DashboardService, BinService, TruckService, CollectionService,
                      # AlertService, SimulationService — typed HttpClient wrappers
  Pages/              # Home.razor, Bins.razor, BinDetail.razor, Trucks.razor,
                      # Collections.razor, Alerts.razor
  Layout/             # MainLayout.razor, NavMenu.razor
  wwwroot/css/app.css # All custom CSS (no Bootstrap classes used; Bootstrap is present
                      # in wwwroot/lib/ but not relied upon)
  Program.cs          # HttpClient base address, DI registrations
```

## Important architectural decisions

### Backend

- **No EF Core migrations** — uses `db.Database.EnsureCreated()` in `DbSeeder`. For demos this is fine; don't add migrations unless deliberately switching.
- **SQLite database file** lives at `backend/WasteManagementApi/waste-management.db` (created at runtime). It is gitignored. Delete it to re-seed from scratch.
- **CORS** allows only `http://localhost:5231`. If you change the frontend port, update `Program.cs`.
- **`RequestLoggingMiddleware` runs before CORS** in the pipeline. It uses `Response.OnStarting()` and checks `Response.HasStarted` before writing headers — this is intentional to avoid writing to committed responses (OPTIONS preflight commits immediately).
- **OpenTelemetry OTLP exporter** points to `http://localhost:4317`. If no collector is running, export silently fails — the console exporter still works. Set `OTEL_EXPORTER_OTLP_ENDPOINT` env var to override.
- **Background workers** (`SensorSimulatorService`, `AlertMonitorService`) start automatically. Sensor cycle: 10s normal, 2s accelerated. Alert check: 15s. They use `IServiceScopeFactory` to create DbContext scopes.

### Frontend

- **HttpClient base address** is hardcoded to `http://localhost:5230` in `frontend/WasteManagementDashboard/Program.cs`. Update this if backend port changes.
- **All timer callbacks** are wrapped in `try { } catch { }` — this is required because `InvokeAsync(StateHasChanged)` throws `ObjectDisposedException` if a timer fires after a page component is disposed (navigation race condition). Do not remove these catch blocks.
- **`BinDetail.razor`** — the page class name and the `WasteManagementDashboard.Models.BinDetail` record have the same name. The field uses the fully qualified type `WasteManagementDashboard.Models.BinDetail?` to avoid the ambiguity. Keep this if renaming.
- **No Blazor code-behind** — all logic is inline `@code { }` blocks.

## Simulation controls

`GET/PUT /api/simulation/status|chaos|accelerate` and `POST /api/simulation/reset` control runtime behaviour without restarting:

| Mode        | Effect |
|-------------|--------|
| Chaos       | 50% sensor timeout rate, random API delays on 20% of requests |
| Accelerated | Sensor cycle drops from 10s → 2s |
| Reset       | Bins reset to 20–60% fill, all open alerts acknowledged, both modes off |

## Telemetry

**Serilog** — structured JSON to console. Config in `appsettings.json` under `"Serilog"`. EF Core and ASP.NET Core noise overridden to `Warning`.

**OpenTelemetry traces** — custom activity source name: `WasteManagementApi`. Key spans:
- `sensor.simulate-cycle`, `sensor.read`
- `bin.get-detail`
- `collection.schedule`, `collection.complete`
- `alert.generate`

**OpenTelemetry metrics** — meter name: `WasteManagementApi`. Instruments:
- `waste-management.collections.scheduled` / `waste-management.collections.completed` (counters)
- `waste-management.alerts.generated` / `waste-management.alerts.acknowledged` (counters)
- `waste-management.collection.duration_minutes` (histogram)
- `waste-management.bin.fill_level_at_collection` (histogram)
- `waste-management.bins.average_fill_level` (observable gauge)
- `waste-management.trucks.active` (observable gauge)

## Seed data

12 bins (various types/statuses), 4 trucks, 8 collections, 3 alerts, 288 fill-level readings (24h history per bin). Seeded only if the database is empty. Delete `waste-management.db` to re-seed.
