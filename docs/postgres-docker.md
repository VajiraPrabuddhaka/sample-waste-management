# Running PostgreSQL with Docker

A quick way to get a local PostgreSQL instance for development without a system-level install.

## Start the container

```bash
docker run -d \
  --name waste-management-postgres \
  -e POSTGRES_USER=waste-management \
  -e POSTGRES_PASSWORD=waste-management123 \
  -e POSTGRES_DB=waste-management \
  -p 5432:5432 \
  postgres:17
```

## Verify it's running

```bash
docker ps
```

Or connect directly via psql:

```bash
docker exec -it waste-management-postgres psql -U waste-management -d waste-management
```

## Configure the backend

**Option A — `appsettings.json`**

Update `backend/WasteManagementApi/appsettings.json`:

```json
"DatabaseProvider": "postgres",
"ConnectionStrings": {
  "PostgresConnection": "Host=localhost;Port=5432;Database=waste-management;Username=waste-management;Password=waste-management123"
}
```

**Option B — environment variables (no file changes needed)**

```bash
DatabaseProvider=postgres \
ConnectionStrings__PostgresConnection="Host=localhost;Port=5432;Database=waste-management;Username=waste-management;Password=waste-management123" \
dotnet run --project backend/WasteManagementApi/
```

The schema and seed data are created automatically on first run.

## Container management

```bash
# Stop (data is preserved)
docker stop waste-management-postgres

# Start again
docker start waste-management-postgres

# Destroy completely (wipes all data)
docker rm -f waste-management-postgres
```

## Re-seed from scratch

Destroy the container, recreate it, then restart the backend:

```bash
docker rm -f waste-management-postgres
docker run -d \
  --name waste-management-postgres \
  -e POSTGRES_USER=waste-management \
  -e POSTGRES_PASSWORD=waste-management123 \
  -e POSTGRES_DB=waste-management \
  -p 5432:5432 \
  postgres:17
dotnet run --project backend/WasteManagementApi/
```
