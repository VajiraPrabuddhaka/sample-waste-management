# Smart Waste Management Dashboard — Implementation Plan

## Project Overview

A .NET 10 sample application demonstrating OpenChoreo observability capabilities through a Smart Waste Management system. The application simulates waste bin sensors, collection trucks, and scheduling — generating rich telemetry data (structured logs, distributed traces, custom metrics).

## Architecture

```
┌─────────────────────────┐         ┌──────────────────────────────────┐
│  Frontend               │  HTTP   │  Backend                         │
│  (Blazor WebAssembly)   │ ──────► │  (.NET 10 Web API)               │
│                         │         │                                  │
│  • Dashboard page       │         │  Controllers/                    │
│  • Bins page            │         │   ├─ BinsController              │
│  • Trucks page          │         │   ├─ TrucksController            │
│  • Collections page     │         │   ├─ CollectionsController       │
│  • Alerts page          │         │   ├─ AlertsController            │
│                         │         │   └─ DashboardController         │
│  localhost:5231         │         │                                  │
│                         │         │  Background Workers/             │
│                         │         │   ├─ SensorSimulatorService      │
│                         │         │   └─ AlertMonitorService         │
│                         │         │                                  │
│                         │         │  Observability/                  │
│                         │         │   ├─ Serilog (structured logs)   │
│                         │         │   ├─ OpenTelemetry (traces)      │
│                         │         │   └─ Custom Metrics (OTLP)       │
│                         │         │                                  │
│                         │         │  SQLite + EF Core                │
│                         │         │  localhost:5230                   │
└─────────────────────────┘         └──────────────────────────────────┘
```

## Key Configuration Values

| Setting                    | Value                        |
|----------------------------|------------------------------|
| Backend HTTP URL           | `http://localhost:5230`       |
| Frontend HTTP URL          | `http://localhost:5231`       |
| SQLite DB file             | `waste-management.db`                 |
| OTLP endpoint              | `http://localhost:4317`       |
| Alert threshold            | 85% fill level               |
| Sensor sim interval        | 10 seconds                   |
| Alert check interval       | 15 seconds                   |
| Sensor timeout probability | 5% (normal), 50% (chaos)     |

---

# Phase 1: Project Scaffolding

## P1-T1: Install .NET 10 SDK and Verify
- **Description**: Ensure .NET 10 SDK is installed and available
- **Dependencies**: None
- **Complexity**: S
- **Actions**:
  1. Check if `dotnet --version` returns 10.x
  2. If not installed, guide user to install from https://dotnet.microsoft.com/download/dotnet/10.0
  3. Verify with `dotnet --list-sdks`
- **Completion Criteria**: `dotnet --version` returns 10.0.x

## P1-T2: Create Backend Web API Project
- **Description**: Scaffold the .NET 10 Web API project in `backend/WasteManagementApi/`
- **Dependencies**: P1-T1
- **Complexity**: S
- **Actions**:
  1. Run: `dotnet new webapi -n WasteManagementApi -o backend/WasteManagementApi --framework net10.0`
  2. Verify project file targets `net10.0`
  3. Remove any generated WeatherForecast example files
- **Files Created**:
  - `backend/WasteManagementApi/WasteManagementApi.csproj`
  - `backend/WasteManagementApi/Program.cs`
  - `backend/WasteManagementApi/appsettings.json`
  - `backend/WasteManagementApi/appsettings.Development.json`
  - `backend/WasteManagementApi/Properties/launchSettings.json`
- **Completion Criteria**: `dotnet build backend/WasteManagementApi/` succeeds

## P1-T3: Create Frontend Blazor WebAssembly Project
- **Description**: Scaffold the Blazor WASM project in `frontend/WasteManagementDashboard/`
- **Dependencies**: P1-T1
- **Complexity**: S
- **Actions**:
  1. Run: `dotnet new blazorwasm -n WasteManagementDashboard -o frontend/WasteManagementDashboard --framework net10.0`
  2. Verify project file targets `net10.0`
- **Files Created**:
  - `frontend/WasteManagementDashboard/WasteManagementDashboard.csproj`
  - `frontend/WasteManagementDashboard/Program.cs`
  - `frontend/WasteManagementDashboard/wwwroot/` (static assets)
  - `frontend/WasteManagementDashboard/Pages/` (default pages)
  - `frontend/WasteManagementDashboard/Layout/` (layout files)
- **Completion Criteria**: `dotnet build frontend/WasteManagementDashboard/` succeeds

## P1-T4: Create Solution File and Git Setup
- **Description**: Create a solution file linking both projects, add .gitignore
- **Dependencies**: P1-T2, P1-T3
- **Complexity**: S
- **Actions**:
  1. Run: `dotnet new sln -n WasteManagement` (in repo root)
  2. Run: `dotnet sln add backend/WasteManagementApi/WasteManagementApi.csproj`
  3. Run: `dotnet sln add frontend/WasteManagementDashboard/WasteManagementDashboard.csproj`
  4. Run: `dotnet new gitignore` (in repo root)
  5. Initialize git repo if not already: `git init`
- **Files Created**:
  - `WasteManagement.sln`
  - `.gitignore`
- **Completion Criteria**: `dotnet build` at repo root succeeds (builds both projects)

---

# Phase 2: Backend Data Layer

## P2-T1: Install Backend NuGet Packages
- **Description**: Add all required NuGet packages to the backend project
- **Dependencies**: P1-T2
- **Complexity**: S
- **Actions** (run from `backend/WasteManagementApi/`):
  ```bash
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  dotnet add package Microsoft.EntityFrameworkCore.Design
  dotnet add package Serilog.AspNetCore
  dotnet add package Serilog.Sinks.Console
  dotnet add package Serilog.Enrichers.Span
  dotnet add package OpenTelemetry.Extensions.Hosting
  dotnet add package OpenTelemetry.Instrumentation.AspNetCore
  dotnet add package OpenTelemetry.Instrumentation.Http
  dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
  dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
  dotnet add package OpenTelemetry.Exporter.Console
  ```
- **Completion Criteria**: `dotnet restore backend/WasteManagementApi/` succeeds, all packages listed in `.csproj`

## P2-T2: Create Domain Models
- **Description**: Create all EF Core entity models
- **Dependencies**: P2-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Models/WasteBin.cs`
  - `backend/WasteManagementApi/Models/Truck.cs`
  - `backend/WasteManagementApi/Models/Collection.cs`
  - `backend/WasteManagementApi/Models/Alert.cs`
  - `backend/WasteManagementApi/Models/FillLevelReading.cs`
  - `backend/WasteManagementApi/Models/Enums.cs`

### Model Definitions:

**Enums** (`Models/Enums.cs`):
```csharp
namespace WasteManagementApi.Models;

public enum BinType { General, Recycling, Organic, Glass, Paper }
public enum BinStatus { Active, Inactive, Maintenance }
public enum TruckStatus { Available, OnRoute, Collecting, Returning, OutOfService }
public enum CollectionStatus { Scheduled, InProgress, Completed, Cancelled }
public enum AlertSeverity { Info, Warning, Critical }
public enum AlertType { BinFull, SensorTimeout, CollectionOverdue, TruckIssue }
```

**WasteBin** (`Models/WasteBin.cs`):
```csharp
namespace WasteManagementApi.Models;

public class WasteBin
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;        // e.g., "BIN-001"
    public string Location { get; set; } = string.Empty;     // e.g., "Main Street & 5th Ave"
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public BinType Type { get; set; }
    public BinStatus Status { get; set; }
    public double CurrentFillLevel { get; set; }             // 0.0 - 100.0
    public double CapacityLiters { get; set; }               // e.g., 1100
    public DateTime LastCollected { get; set; }
    public DateTime LastSensorReading { get; set; }
    public DateTime InstalledDate { get; set; }

    public ICollection<FillLevelReading> FillLevelReadings { get; set; } = [];
    public ICollection<Collection> Collections { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
}
```

**Truck** (`Models/Truck.cs`):
```csharp
namespace WasteManagementApi.Models;

public class Truck
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;         // e.g., "TRUCK-01"
    public string RegistrationNumber { get; set; } = string.Empty;
    public TruckStatus Status { get; set; }
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public double FuelLevelPercent { get; set; }              // 0 - 100
    public int BinsCollectedToday { get; set; }
    public string? CurrentRoute { get; set; }
    public DateTime LastUpdated { get; set; }

    public ICollection<Collection> Collections { get; set; } = [];
}
```

**Collection** (`Models/Collection.cs`):
```csharp
namespace WasteManagementApi.Models;

public class Collection
{
    public int Id { get; set; }
    public int BinId { get; set; }
    public int TruckId { get; set; }
    public CollectionStatus Status { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? FillLevelAtCollection { get; set; }
    public string? Notes { get; set; }

    public WasteBin Bin { get; set; } = null!;
    public Truck Truck { get; set; } = null!;
}
```

**Alert** (`Models/Alert.cs`):
```csharp
namespace WasteManagementApi.Models;

public class Alert
{
    public int Id { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BinId { get; set; }
    public int? TruckId { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public WasteBin? Bin { get; set; }
    public Truck? Truck { get; set; }
}
```

**FillLevelReading** (`Models/FillLevelReading.cs`):
```csharp
namespace WasteManagementApi.Models;

public class FillLevelReading
{
    public int Id { get; set; }
    public int BinId { get; set; }
    public double FillLevel { get; set; }                    // 0.0 - 100.0
    public double? Temperature { get; set; }                 // Celsius
    public double? BatteryLevel { get; set; }                // Sensor battery %
    public DateTime RecordedAt { get; set; }

    public WasteBin Bin { get; set; } = null!;
}
```

- **Completion Criteria**: All 6 files compile without errors

## P2-T3: Create DbContext and Configuration
- **Description**: Create EF Core DbContext with entity configurations
- **Dependencies**: P2-T2
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Data/WasteManagementDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Models;

namespace WasteManagementApi.Data;

public class WasteManagementDbContext : DbContext
{
    public WasteManagementDbContext(DbContextOptions<WasteManagementDbContext> options) : base(options) { }

    public DbSet<WasteBin> WasteBins => Set<WasteBin>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<FillLevelReading> FillLevelReadings => Set<FillLevelReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WasteBin>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(50);
            e.Property(b => b.Location).HasMaxLength(200);
            e.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<Truck>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(50);
            e.Property(t => t.RegistrationNumber).HasMaxLength(20);
            e.HasIndex(t => t.RegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<Collection>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Bin).WithMany(b => b.Collections).HasForeignKey(c => c.BinId);
            e.HasOne(c => c.Truck).WithMany(t => t.Collections).HasForeignKey(c => c.TruckId);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Message).HasMaxLength(500);
            e.HasOne(a => a.Bin).WithMany(b => b.Alerts).HasForeignKey(a => a.BinId);
            e.HasOne(a => a.Truck).WithMany().HasForeignKey(a => a.TruckId);
        });

        modelBuilder.Entity<FillLevelReading>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Bin).WithMany(b => b.FillLevelReadings).HasForeignKey(r => r.BinId);
            e.HasIndex(r => new { r.BinId, r.RecordedAt });
        });
    }
}
```

- **Completion Criteria**: Project compiles with DbContext

## P2-T4: Create Database Seeder
- **Description**: Create seed data class with realistic sample data
- **Dependencies**: P2-T3
- **Complexity**: L
- **Files Created**:
  - `backend/WasteManagementApi/Data/DbSeeder.cs`
- **Seed Data**:
  - **12 WasteBins**: Various types (General, Recycling, Organic, Glass, Paper), spread across locations with different fill levels (some near 85% threshold to trigger alerts soon)
  - **4 Trucks**: Different statuses (Available, OnRoute, Collecting, OutOfService)
  - **8 Collections**: Mix of Scheduled, InProgress, Completed, Cancelled
  - **3 Alerts**: One acknowledged, two unacknowledged (BinFull, SensorTimeout)
  - **288 FillLevelReadings**: 24 readings per bin for top 12 bins (one per hour for last 24h), showing gradual fill-level increase

The seeder should:
1. Check if data already exists before seeding
2. Use `EnsureCreated()` for SQLite (no migrations needed for demo)
3. Log the seeding process with structured logging

- **Completion Criteria**: Seeder compiles and creates realistic data

## P2-T5: Wire Up Data Layer in Program.cs
- **Description**: Register DbContext, run seeder on startup
- **Dependencies**: P2-T3, P2-T4
- **Complexity**: S
- **Files Modified**:
  - `backend/WasteManagementApi/Program.cs`
- **Actions**:
  1. Add `builder.Services.AddDbContext<WasteManagementDbContext>(...)` with SQLite connection string
  2. Add CORS policy allowing frontend origin (`http://localhost:5231`)
  3. Add seeder call after `app.Build()`
  4. Configure Kestrel to listen on port 5230
- **Completion Criteria**: Backend starts, creates `waste-management.db`, seed data exists

---

# Phase 3: Backend API Endpoints

## P3-T1: Create DTOs (Data Transfer Objects)
- **Description**: Create request/response DTOs for all endpoints
- **Dependencies**: P2-T2
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/DTOs/BinDtos.cs`
  - `backend/WasteManagementApi/DTOs/TruckDtos.cs`
  - `backend/WasteManagementApi/DTOs/CollectionDtos.cs`
  - `backend/WasteManagementApi/DTOs/AlertDtos.cs`
  - `backend/WasteManagementApi/DTOs/DashboardDtos.cs`

### DTO Definitions:

**BinDtos.cs**:
- `BinSummary` — Id, Name, Location, Type, Status, CurrentFillLevel, CapacityLiters, LastCollected, LastSensorReading
- `BinDetail` — All of BinSummary + Latitude, Longitude, InstalledDate, RecentReadings (list)
- `FillLevelReadingDto` — FillLevel, Temperature, BatteryLevel, RecordedAt

**TruckDtos.cs**:
- `TruckSummary` — Id, Name, RegistrationNumber, Status, FuelLevelPercent, BinsCollectedToday, CurrentRoute, LastUpdated
- `TruckDetail` — All of TruckSummary + CurrentLatitude, CurrentLongitude, RecentCollections (list)
- `UpdateTruckLocationRequest` — Latitude, Longitude

**CollectionDtos.cs**:
- `CollectionSummary` — Id, BinId, BinName, TruckId, TruckName, Status, ScheduledAt, StartedAt, CompletedAt, FillLevelAtCollection, Notes
- `ScheduleCollectionRequest` — BinId, TruckId, ScheduledAt, Notes
- `CompleteCollectionRequest` — FillLevelAtCollection, Notes

**AlertDtos.cs**:
- `AlertSummary` — Id, Type, Severity, Message, BinId, BinName, TruckId, TruckName, IsAcknowledged, AcknowledgedBy, CreatedAt, AcknowledgedAt
- `AcknowledgeAlertRequest` — AcknowledgedBy

**DashboardDtos.cs**:
- `DashboardSummary` — TotalBins, ActiveBins, BinsNearFull (>70%), BinsOverThreshold (>85%), TotalTrucks, ActiveTrucks, TodayCollections, PendingCollections, OpenAlerts, CriticalAlerts, AverageFillLevel, CollectionCompletionRate

All DTOs should be **records** for immutability.

- **Completion Criteria**: All DTOs compile

## P3-T2: Create BinsController
- **Description**: REST endpoints for waste bin operations
- **Dependencies**: P2-T3, P3-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Controllers/BinsController.cs`
- **Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/bins` | List all bins (with optional `?type=` and `?status=` filters) |
  | GET | `/api/bins/{id}` | Get bin detail with recent fill-level readings |
  | GET | `/api/bins/{id}/readings` | Get fill-level history (with optional `?hours=24` param) |
- **Logging**: Each endpoint logs request params, result count, and timing at `Information` level. Errors at `Error` level with exception details.
- **Completion Criteria**: Endpoints return seeded data correctly

## P3-T3: Create TrucksController
- **Description**: REST endpoints for truck operations
- **Dependencies**: P2-T3, P3-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Controllers/TrucksController.cs`
- **Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/trucks` | List all trucks (optional `?status=` filter) |
  | GET | `/api/trucks/{id}` | Get truck detail with recent collections |
  | PUT | `/api/trucks/{id}/location` | Update truck GPS location |
- **Completion Criteria**: Endpoints return seeded data correctly

## P3-T4: Create CollectionsController
- **Description**: REST endpoints for waste collection scheduling
- **Dependencies**: P2-T3, P3-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Controllers/CollectionsController.cs`
- **Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/collections` | List collections (optional `?status=` filter) |
  | GET | `/api/collections/{id}` | Get collection detail |
  | POST | `/api/collections` | Schedule new collection |
  | PUT | `/api/collections/{id}/start` | Mark collection as started |
  | PUT | `/api/collections/{id}/complete` | Mark collection as completed |
  | PUT | `/api/collections/{id}/cancel` | Cancel a collection |
- **Logging**: Log state transitions (Scheduled → InProgress → Completed) at `Information` level with all context.
- **Completion Criteria**: Can create and transition collections through all states

## P3-T5: Create AlertsController
- **Description**: REST endpoints for alert management
- **Dependencies**: P2-T3, P3-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Controllers/AlertsController.cs`
- **Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/alerts` | List alerts (optional `?acknowledged=true/false`, `?severity=`) |
  | GET | `/api/alerts/{id}` | Get alert detail |
  | PUT | `/api/alerts/{id}/acknowledge` | Acknowledge an alert |
  | PUT | `/api/alerts/acknowledge-all` | Acknowledge all open alerts |
- **Completion Criteria**: Can list, filter, and acknowledge alerts

## P3-T6: Create DashboardController and Request Logging Middleware
- **Description**: Dashboard summary endpoint and request/response logging middleware
- **Dependencies**: P2-T3, P3-T1
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Controllers/DashboardController.cs`
  - `backend/WasteManagementApi/Middleware/RequestLoggingMiddleware.cs`
- **DashboardController Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/dashboard` | Get dashboard summary statistics |
- **RequestLoggingMiddleware**:
  - Logs every HTTP request with: method, path, status code, duration (ms), correlation ID
  - Adds `X-Correlation-Id` header (from request or generates new GUID)
  - Adds `X-Request-Duration-Ms` response header
  - Log level: `Information` for 2xx, `Warning` for 4xx, `Error` for 5xx
- **Completion Criteria**: Dashboard returns aggregate stats; middleware logs all requests

---

# Phase 4: Backend Observability

## P4-T1: Configure Serilog Structured Logging
- **Description**: Replace default logging with Serilog, add structured logging enrichers
- **Dependencies**: P2-T1
- **Complexity**: M
- **Files Modified**:
  - `backend/WasteManagementApi/Program.cs`
  - `backend/WasteManagementApi/appsettings.json`
- **Configuration**:
  1. Add Serilog to `Program.cs` using `UseSerilog()`
  2. Configure console sink with structured JSON output
  3. Add enrichers: Machine name, thread ID, span ID (for trace correlation)
  4. Set minimum level: `Information` (override `Microsoft.EntityFrameworkCore` to `Warning`)
  5. Add `appsettings.json` Serilog section:
     ```json
     "Serilog": {
       "MinimumLevel": {
         "Default": "Information",
         "Override": {
           "Microsoft.AspNetCore": "Warning",
           "Microsoft.EntityFrameworkCore": "Warning",
           "System": "Warning"
         }
       },
       "WriteTo": [{ "Name": "Console" }],
       "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithSpanId"]
     }
     ```
- **Completion Criteria**: App starts with Serilog, logs are structured JSON with trace correlation

## P4-T2: Configure OpenTelemetry Tracing
- **Description**: Set up OpenTelemetry with OTLP exporter for distributed tracing
- **Dependencies**: P2-T1
- **Complexity**: M
- **Files Modified**:
  - `backend/WasteManagementApi/Program.cs`
- **Configuration**:
  1. Add OpenTelemetry tracing with:
     - ASP.NET Core instrumentation
     - HTTP client instrumentation
     - EF Core instrumentation
     - Custom activity source: `WasteManagementApi`
  2. Add OpenTelemetry metrics with:
     - ASP.NET Core instrumentation
     - Runtime instrumentation
     - Custom meter: `WasteManagementApi`
  3. Configure OTLP exporter (endpoint from env var `OTEL_EXPORTER_OTLP_ENDPOINT`, fallback to `http://localhost:4317`)
  4. Also add Console exporter for local dev visibility
  5. Set service name: `waste-management-api`
  6. Set service version from assembly
- **Completion Criteria**: Traces appear in console output, OTLP export configured

## P4-T3: Create Custom Metrics
- **Description**: Define custom metrics for business observability
- **Dependencies**: P4-T2
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Telemetry/WasteManagementMetrics.cs`
- **Custom Metrics**:
  ```csharp
  public class WasteManagementMetrics
  {
      public const string MeterName = "WasteManagementApi";
      private readonly Counter<long> _collectionsScheduled;
      private readonly Counter<long> _collectionsCompleted;
      private readonly Counter<long> _alertsGenerated;
      private readonly Counter<long> _alertsAcknowledged;
      private readonly Histogram<double> _collectionDuration;
      private readonly Histogram<double> _apiFillLevelAtCollection;
      private readonly ObservableGauge<double> _averageFillLevel;
      private readonly ObservableGauge<int> _activeTrucks;
      // Constructor creates all instruments from Meter
  }
  ```
  Register as singleton in DI.
- **Completion Criteria**: Metrics class compiles, registered in DI

## P4-T4: Instrument Controllers with Metrics
- **Description**: Add metric recording to all controller actions
- **Dependencies**: P4-T3, P3-T2, P3-T3, P3-T4, P3-T5
- **Complexity**: M
- **Files Modified**:
  - `backend/WasteManagementApi/Controllers/CollectionsController.cs` — record `_collectionsScheduled`, `_collectionsCompleted`, `_collectionDuration`
  - `backend/WasteManagementApi/Controllers/AlertsController.cs` — record `_alertsAcknowledged`
- **Completion Criteria**: Metrics are recorded on relevant operations

## P4-T5: Add Custom Activity Sources for Tracing
- **Description**: Add custom spans to controllers and services for detailed tracing
- **Dependencies**: P4-T2, P3-T2, P3-T3, P3-T4, P3-T5
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Telemetry/WasteManagementActivitySource.cs`
- **Files Modified**:
  - Controllers — wrap key operations in custom spans with tags (bin.id, truck.id, collection.status, etc.)
- **Custom Spans**:
  - `bin.get-detail` — tag: bin.id, bin.fill_level
  - `collection.schedule` — tag: bin.id, truck.id, scheduled_at
  - `collection.complete` — tag: collection.id, duration_minutes
  - `alert.generate` — tag: alert.type, alert.severity, bin.id
  - `sensor.read` — tag: bin.id, fill_level, battery_level
- **Completion Criteria**: Custom spans appear in trace output with correct tags

---

# Phase 5: Backend Background Workers

## P5-T1: Create SensorSimulatorService
- **Description**: Background service that simulates IoT sensor updates for all bins
- **Dependencies**: P2-T5, P4-T3, P4-T5
- **Complexity**: L
- **Files Created**:
  - `backend/WasteManagementApi/Services/SensorSimulatorService.cs`
- **Behavior**:
  1. Runs every **10 seconds**
  2. For each active bin:
     - Generate a new fill level (gradually increasing, +0.5% to +2% per tick, random)
     - If bin was recently collected, reset to low level (5-15%)
     - 5% chance of "sensor timeout" (skip reading, log warning)
     - Create a `FillLevelReading` record
     - Update bin's `CurrentFillLevel` and `LastSensorReading`
  3. Log each reading with structured data: `{BinId, BinName, FillLevel, PreviousFillLevel, Delta}`
  4. Create custom trace span `sensor.simulate-cycle` wrapping each full cycle
  5. Record timing metric for simulation cycle duration
- **Completion Criteria**: Service starts automatically, logs sensor readings every 10 seconds

## P5-T2: Create AlertMonitorService
- **Description**: Background service that monitors bin levels and generates alerts
- **Dependencies**: P2-T5, P4-T3, P4-T5
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Services/AlertMonitorService.cs`
- **Behavior**:
  1. Runs every **15 seconds**
  2. Check all active bins:
     - If `CurrentFillLevel >= 85%` and no unacknowledged `BinFull` alert exists → create `BinFull` alert (Critical)
     - If `LastSensorReading` is older than 5 minutes → create `SensorTimeout` alert (Warning)
  3. Check scheduled collections:
     - If `ScheduledAt` is more than 1 hour ago and status is still `Scheduled` → create `CollectionOverdue` alert (Warning)
  4. Log each alert creation at `Warning` level
  5. Record `_alertsGenerated` metric with tags for type and severity
- **Completion Criteria**: Alerts auto-generated when bins hit threshold

## P5-T3: Verify Backend End-to-End
- **Description**: Start the backend and verify all endpoints work
- **Dependencies**: P5-T1, P5-T2, P4-T4, P4-T5
- **Complexity**: S
- **Actions**:
  1. Run `dotnet run --project backend/WasteManagementApi/`
  2. Verify: `GET http://localhost:5230/api/dashboard` returns summary
  3. Verify: `GET http://localhost:5230/api/bins` returns 12 bins
  4. Verify: `GET http://localhost:5230/api/trucks` returns 4 trucks
  5. Verify: Console shows structured logs and trace output
  6. Wait 30 seconds, verify sensor readings and alerts are being generated
- **Completion Criteria**: All endpoints respond, background workers are running, telemetry visible in console

---

# Phase 6: Frontend Setup & Services

## P6-T1: Install Frontend NuGet Packages and Configure
- **Description**: Add packages and configure HTTP client
- **Dependencies**: P1-T3
- **Complexity**: S
- **Files Modified**:
  - `frontend/WasteManagementDashboard/Program.cs`
- **Actions**:
  1. No extra NuGet packages needed (Blazor WASM has `System.Net.Http.Json` built-in)
  2. Configure `HttpClient` base address to `http://localhost:5230` in `Program.cs`
- **Completion Criteria**: Frontend builds and `HttpClient` is configured

## P6-T2: Create Frontend DTO Models
- **Description**: Create matching DTO models for the frontend (mirroring backend DTOs)
- **Dependencies**: P3-T1
- **Complexity**: M
- **Files Created**:
  - `frontend/WasteManagementDashboard/Models/BinModels.cs`
  - `frontend/WasteManagementDashboard/Models/TruckModels.cs`
  - `frontend/WasteManagementDashboard/Models/CollectionModels.cs`
  - `frontend/WasteManagementDashboard/Models/AlertModels.cs`
  - `frontend/WasteManagementDashboard/Models/DashboardModels.cs`
- **Note**: These are the same record definitions as backend DTOs but in the `WasteManagementDashboard.Models` namespace. Only include properties needed for display (no navigation properties).
- **Completion Criteria**: All model files compile

## P6-T3: Create HTTP API Service Classes
- **Description**: Create typed service classes for each API area
- **Dependencies**: P6-T1, P6-T2
- **Complexity**: M
- **Files Created**:
  - `frontend/WasteManagementDashboard/Services/DashboardService.cs`
  - `frontend/WasteManagementDashboard/Services/BinService.cs`
  - `frontend/WasteManagementDashboard/Services/TruckService.cs`
  - `frontend/WasteManagementDashboard/Services/CollectionService.cs`
  - `frontend/WasteManagementDashboard/Services/AlertService.cs`
- **Each service**:
  - Takes `HttpClient` via constructor injection
  - Has async methods matching the backend endpoints
  - Uses `GetFromJsonAsync<T>` and `PostAsJsonAsync<T>` etc.
  - Example: `BinService.GetBinsAsync()`, `BinService.GetBinDetailAsync(int id)`
- **Register all as scoped services in `Program.cs`**
- **Completion Criteria**: All services compile and are registered in DI

## P6-T4: Configure Frontend Layout and Navigation
- **Description**: Update the Blazor layout with waste management themed navigation
- **Dependencies**: P1-T3
- **Complexity**: M
- **Files Modified**:
  - `frontend/WasteManagementDashboard/Layout/MainLayout.razor`
  - `frontend/WasteManagementDashboard/Layout/NavMenu.razor`
  - `frontend/WasteManagementDashboard/wwwroot/css/app.css`
- **Navigation Items**:
  - Dashboard (`/`) — icon: tachometer/gauge
  - Bins (`/bins`) — icon: trash
  - Trucks (`/trucks`) — icon: truck
  - Collections (`/collections`) — icon: calendar
  - Alerts (`/alerts`) — icon: bell
- **Styling**: Clean, professional look. Use a green/dark-green color scheme to match waste management branding. No CSS frameworks — just clean custom CSS.
- **Completion Criteria**: Navigation works, app has professional look

---

# Phase 7: Frontend Pages

## P7-T1: Create Dashboard Page
- **Description**: Summary dashboard with key metrics cards
- **Dependencies**: P6-T3, P6-T4
- **Complexity**: M
- **Files Modified**:
  - `frontend/WasteManagementDashboard/Pages/Home.razor` (replace default home page)
- **Layout**:
  - **Top row**: 4 metric cards — Total Bins (Active), Total Trucks (Active), Today's Collections, Open Alerts
  - **Second row**: 4 detail cards — Bins Near Full (>70%), Bins Over Threshold (>85%), Average Fill Level, Collection Completion Rate
  - Auto-refresh every 10 seconds using `Timer`
- **Completion Criteria**: Dashboard displays live data from backend

## P7-T2: Create Bins Page and BinDetail Page
- **Description**: Bin listing and detail view
- **Dependencies**: P6-T3, P6-T4
- **Complexity**: L
- **Files Created**:
  - `frontend/WasteManagementDashboard/Pages/Bins.razor`
  - `frontend/WasteManagementDashboard/Pages/BinDetail.razor`
- **Bins.razor**:
  - Table/grid of all bins: Name, Location, Type, Status, Fill Level (with color-coded progress bar: green <50%, yellow 50-84%, red >=85%)
  - Filter by bin type (dropdown)
  - Click row to navigate to detail
- **BinDetail.razor** (`/bins/{id:int}`):
  - Bin info card
  - Fill level history table (last 24h readings)
  - Current status and last collection info
- **Completion Criteria**: Can browse bins, see fill levels, view detail

## P7-T3: Create Trucks Page
- **Description**: Truck listing with status
- **Dependencies**: P6-T3, P6-T4
- **Complexity**: M
- **Files Created**:
  - `frontend/WasteManagementDashboard/Pages/Trucks.razor`
- **Layout**:
  - Table of trucks: Name, Registration, Status (with badge colors), Fuel Level (progress bar), Bins Collected Today, Current Route
  - Filter by status (dropdown)
  - Auto-refresh every 15 seconds
- **Completion Criteria**: Trucks display with live status updates

## P7-T4: Create Collections Page
- **Description**: Collection scheduling and management
- **Dependencies**: P6-T3, P6-T4
- **Complexity**: L
- **Files Created**:
  - `frontend/WasteManagementDashboard/Pages/Collections.razor`
- **Layout**:
  - **Schedule Form** (top): Dropdown for bin, dropdown for truck, datetime picker, notes field, "Schedule" button
  - **Collections Table**: All collections with Status badge, Bin Name, Truck Name, Scheduled At, actions (Start/Complete/Cancel depending on current status)
  - Filter by status (dropdown)
- **Completion Criteria**: Can schedule new collections and transition states

## P7-T5: Create Alerts Page
- **Description**: Alert listing and acknowledgment
- **Dependencies**: P6-T3, P6-T4
- **Complexity**: M
- **Files Created**:
  - `frontend/WasteManagementDashboard/Pages/Alerts.razor`
- **Layout**:
  - Filter toggle: All / Unacknowledged / Acknowledged
  - "Acknowledge All" button
  - Alert list: Severity badge (Info=blue, Warning=yellow, Critical=red), Type, Message, Related Bin/Truck, Created At, Acknowledged status
  - Click "Acknowledge" button on each alert
  - Auto-refresh every 10 seconds
- **Completion Criteria**: Can view and acknowledge alerts

## P7-T6: Clean Up Default Blazor Template Pages
- **Description**: Remove default Counter and Weather pages
- **Dependencies**: P7-T1
- **Complexity**: S
- **Files Deleted**:
  - `frontend/WasteManagementDashboard/Pages/Counter.razor`
  - `frontend/WasteManagementDashboard/Pages/Weather.razor`
- **Completion Criteria**: No default template pages remain

---

# Phase 8: Integration & Error Simulation

## P8-T1: Add Simulated Error Scenarios to Backend
- **Description**: Add an API endpoint to toggle chaos/error simulation mode
- **Dependencies**: P5-T1, P5-T2
- **Complexity**: M
- **Files Created**:
  - `backend/WasteManagementApi/Services/SimulationStateService.cs`
  - `backend/WasteManagementApi/Controllers/SimulationController.cs`
- **SimulationStateService** (singleton):
  - `bool ChaosMode` — when enabled: 50% sensor timeout rate, random API delays (100-2000ms), occasional 500 errors
  - `bool AcceleratedMode` — when enabled: sensors update every 2 seconds instead of 10
- **SimulationController Endpoints**:
  | Method | Route | Description |
  |--------|-------|-------------|
  | GET | `/api/simulation/status` | Get current simulation flags |
  | PUT | `/api/simulation/chaos` | Toggle chaos mode |
  | PUT | `/api/simulation/accelerate` | Toggle accelerated mode |
  | POST | `/api/simulation/reset` | Reset all bins to random fill levels (20-60%), clear all alerts |
- **Modify SensorSimulatorService** to check `SimulationStateService` flags
- **Add random delay middleware**: When chaos mode on, add 100-2000ms random delay to 20% of requests
- **Completion Criteria**: Chaos mode generates errors, sensor timeouts, and slow requests visible in traces

## P8-T2: Add Simulation Controls to Frontend Dashboard
- **Description**: Add simulation control panel to the dashboard
- **Dependencies**: P8-T1, P7-T1
- **Complexity**: M
- **Files Created**:
  - `frontend/WasteManagementDashboard/Services/SimulationService.cs`
- **Files Modified**:
  - `frontend/WasteManagementDashboard/Pages/Home.razor` — add simulation controls section
- **Controls**:
  - Toggle button: "Chaos Mode" (red when active)
  - Toggle button: "Accelerated Simulation" (yellow when active)
  - Button: "Reset Simulation" (resets bins and alerts)
  - Status indicators showing current mode
- **Completion Criteria**: Can toggle chaos/accelerated modes from frontend

## P8-T3: End-to-End Integration Verification
- **Description**: Verify full application works end-to-end
- **Dependencies**: P8-T1, P8-T2
- **Complexity**: M
- **Actions**:
  1. Start backend: `dotnet run --project backend/WasteManagementApi/`
  2. Start frontend: `dotnet run --project frontend/WasteManagementDashboard/`
  3. Open `http://localhost:5231` in browser
  4. Verify dashboard shows live data
  5. Navigate through all pages
  6. Schedule a collection, start it, complete it
  7. Enable chaos mode, verify errors appear in logs/traces
  8. Acknowledge alerts
  9. Reset simulation
- **Completion Criteria**: All features work end-to-end

---

# Phase 9: Final Testing & Documentation

## P9-T1: Add Swagger/OpenAPI Configuration
- **Description**: Ensure Swagger UI is available for API exploration
- **Dependencies**: P3-T6
- **Complexity**: S
- **Files Modified**:
  - `backend/WasteManagementApi/Program.cs` — ensure `AddEndpointsApiExplorer()` and `UseSwagger()` are configured (likely already there from template, but verify and make available in all environments, not just Development)
- **Completion Criteria**: Swagger UI accessible at `http://localhost:5230/swagger`

## P9-T2: Write README Documentation
- **Description**: Write comprehensive README with setup and usage instructions
- **Dependencies**: P8-T3
- **Complexity**: M
- **Files Created/Modified**:
  - `README.md` (repo root)
- **Content**:
  - Project overview and architecture diagram
  - Prerequisites (.NET 10 SDK)
  - How to run (backend + frontend commands)
  - API endpoints reference
  - Observability features explained
  - How to connect to OpenChoreo
  - Simulation controls explained
  - Screenshots placeholders
- **Completion Criteria**: README is comprehensive and accurate

## P9-T3: Final Cleanup and Verification
- **Description**: Final code cleanup and build verification
- **Dependencies**: P9-T1, P9-T2
- **Complexity**: S
- **Actions**:
  1. `dotnet build` at repo root — verify no warnings
  2. Remove any unused `using` statements
  3. Verify `.gitignore` covers `*.db`, `bin/`, `obj/`
  4. Verify all files are properly committed
- **Completion Criteria**: Clean build, clean git status

---

# File Mapping Reference

## Backend (`backend/WasteManagementApi/`)

| File | Created/Modified In |
|------|-------------------|
| `Program.cs` | P1-T2, P2-T5, P4-T1, P4-T2, P4-T3, P5-T1, P5-T2, P8-T1, P9-T1 |
| `appsettings.json` | P1-T2, P4-T1 |
| `Models/Enums.cs` | P2-T2 |
| `Models/WasteBin.cs` | P2-T2 |
| `Models/Truck.cs` | P2-T2 |
| `Models/Collection.cs` | P2-T2 |
| `Models/Alert.cs` | P2-T2 |
| `Models/FillLevelReading.cs` | P2-T2 |
| `Data/WasteManagementDbContext.cs` | P2-T3 |
| `Data/DbSeeder.cs` | P2-T4 |
| `DTOs/BinDtos.cs` | P3-T1 |
| `DTOs/TruckDtos.cs` | P3-T1 |
| `DTOs/CollectionDtos.cs` | P3-T1 |
| `DTOs/AlertDtos.cs` | P3-T1 |
| `DTOs/DashboardDtos.cs` | P3-T1 |
| `Controllers/BinsController.cs` | P3-T2, P4-T4, P4-T5 |
| `Controllers/TrucksController.cs` | P3-T3, P4-T5 |
| `Controllers/CollectionsController.cs` | P3-T4, P4-T4, P4-T5 |
| `Controllers/AlertsController.cs` | P3-T5, P4-T4, P4-T5 |
| `Controllers/DashboardController.cs` | P3-T6 |
| `Controllers/SimulationController.cs` | P8-T1 |
| `Middleware/RequestLoggingMiddleware.cs` | P3-T6 |
| `Telemetry/WasteManagementMetrics.cs` | P4-T3 |
| `Telemetry/WasteManagementActivitySource.cs` | P4-T5 |
| `Services/SensorSimulatorService.cs` | P5-T1, P8-T1 |
| `Services/AlertMonitorService.cs` | P5-T2 |
| `Services/SimulationStateService.cs` | P8-T1 |

## Frontend (`frontend/WasteManagementDashboard/`)

| File | Created/Modified In |
|------|-------------------|
| `Program.cs` | P1-T3, P6-T1, P6-T3 |
| `Models/BinModels.cs` | P6-T2 |
| `Models/TruckModels.cs` | P6-T2 |
| `Models/CollectionModels.cs` | P6-T2 |
| `Models/AlertModels.cs` | P6-T2 |
| `Models/DashboardModels.cs` | P6-T2 |
| `Services/DashboardService.cs` | P6-T3 |
| `Services/BinService.cs` | P6-T3 |
| `Services/TruckService.cs` | P6-T3 |
| `Services/CollectionService.cs` | P6-T3 |
| `Services/AlertService.cs` | P6-T3 |
| `Services/SimulationService.cs` | P8-T2 |
| `Layout/MainLayout.razor` | P6-T4 |
| `Layout/NavMenu.razor` | P6-T4 |
| `wwwroot/css/app.css` | P6-T4 |
| `Pages/Home.razor` | P7-T1, P8-T2 |
| `Pages/Bins.razor` | P7-T2 |
| `Pages/BinDetail.razor` | P7-T2 |
| `Pages/Trucks.razor` | P7-T3 |
| `Pages/Collections.razor` | P7-T4 |
| `Pages/Alerts.razor` | P7-T5 |

---

# Parallelization Guide

Tasks that can run in parallel (when using multiple agents):

| Parallel Group | Tasks | After |
|---------------|-------|-------|
| 1 | P1-T2, P1-T3 | P1-T1 |
| 2 | P2-T2, P3-T1 | P2-T1 |
| 3 | P3-T2, P3-T3, P3-T4, P3-T5 | P2-T3, P3-T1 |
| 4 | P4-T1, P4-T2 | P2-T1 (but both modify Program.cs — coordinate) |
| 5 | P5-T1, P5-T2 | P2-T5, P4-T3, P4-T5 |
| 6 | P6-T2, P6-T4 | P1-T3, P3-T1 |
| 7 | P7-T1, P7-T2, P7-T3, P7-T4, P7-T5 | P6-T3, P6-T4 |

**Serial bottlenecks**: `Program.cs` (modified by 9 tasks — must coordinate), DbContext depends on models, seeder depends on DbContext.

---

# Agent Execution Instructions

When executing this plan:

1. **Before starting a task**: Update `PROGRESS.md` — change `[ ]` to `[~]` for the task
2. **After completing a task**: Update `PROGRESS.md` — change `[~]` to `[x]` and update the phase summary count
3. **If blocked**: Update `PROGRESS.md` — change to `[!]` and add a note
4. **Build verification**: After each phase, run `dotnet build` to ensure no compilation errors
5. **Program.cs coordination**: Since many tasks modify `Program.cs`, always read the current file before modifying. Add new code in clearly separated sections with comments.
6. **Reference this plan**: Each task has exact file paths, model definitions, and endpoint signatures. Follow them precisely.
