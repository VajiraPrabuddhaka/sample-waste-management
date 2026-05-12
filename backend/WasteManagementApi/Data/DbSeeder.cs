using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Models;

namespace WasteManagementApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(WasteManagementDbContext db, ILogger logger)
    {
        db.Database.EnsureCreated();

        if (await db.WasteBins.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding database with sample data...");

        var now = DateTime.UtcNow;

        // 12 Waste Bins
        var bins = new List<WasteBin>
        {
            new() { Name = "BIN-001", Location = "Main Street & 5th Ave", Latitude = 51.5074, Longitude = -0.1278, Type = BinType.General, Status = BinStatus.Active, CurrentFillLevel = 87.5, CapacityLiters = 1100, LastCollected = now.AddDays(-2), LastSensorReading = now.AddMinutes(-2), InstalledDate = now.AddYears(-2) },
            new() { Name = "BIN-002", Location = "Park Road North", Latitude = 51.5080, Longitude = -0.1290, Type = BinType.Recycling, Status = BinStatus.Active, CurrentFillLevel = 62.3, CapacityLiters = 1100, LastCollected = now.AddDays(-1), LastSensorReading = now.AddMinutes(-3), InstalledDate = now.AddYears(-2) },
            new() { Name = "BIN-003", Location = "Town Square West", Latitude = 51.5060, Longitude = -0.1260, Type = BinType.Organic, Status = BinStatus.Active, CurrentFillLevel = 91.2, CapacityLiters = 660, LastCollected = now.AddDays(-3), LastSensorReading = now.AddMinutes(-1), InstalledDate = now.AddYears(-1) },
            new() { Name = "BIN-004", Location = "High Street", Latitude = 51.5090, Longitude = -0.1300, Type = BinType.General, Status = BinStatus.Active, CurrentFillLevel = 44.7, CapacityLiters = 1100, LastCollected = now.AddDays(-1), LastSensorReading = now.AddMinutes(-4), InstalledDate = now.AddYears(-3) },
            new() { Name = "BIN-005", Location = "Station Road", Latitude = 51.5050, Longitude = -0.1250, Type = BinType.Glass, Status = BinStatus.Active, CurrentFillLevel = 78.9, CapacityLiters = 500, LastCollected = now.AddDays(-4), LastSensorReading = now.AddMinutes(-2), InstalledDate = now.AddYears(-1) },
            new() { Name = "BIN-006", Location = "Church Lane", Latitude = 51.5070, Longitude = -0.1310, Type = BinType.Paper, Status = BinStatus.Active, CurrentFillLevel = 35.1, CapacityLiters = 1100, LastCollected = now.AddDays(-1), LastSensorReading = now.AddMinutes(-5), InstalledDate = now.AddMonths(-18) },
            new() { Name = "BIN-007", Location = "Market Place", Latitude = 51.5085, Longitude = -0.1245, Type = BinType.General, Status = BinStatus.Active, CurrentFillLevel = 88.4, CapacityLiters = 1100, LastCollected = now.AddDays(-2), LastSensorReading = now.AddMinutes(-3), InstalledDate = now.AddYears(-2) },
            new() { Name = "BIN-008", Location = "Riverside Walk", Latitude = 51.5040, Longitude = -0.1280, Type = BinType.Recycling, Status = BinStatus.Maintenance, CurrentFillLevel = 55.0, CapacityLiters = 1100, LastCollected = now.AddDays(-5), LastSensorReading = now.AddHours(-2), InstalledDate = now.AddYears(-3) },
            new() { Name = "BIN-009", Location = "School Street", Latitude = 51.5095, Longitude = -0.1265, Type = BinType.Organic, Status = BinStatus.Active, CurrentFillLevel = 71.8, CapacityLiters = 660, LastCollected = now.AddDays(-2), LastSensorReading = now.AddMinutes(-1), InstalledDate = now.AddMonths(-12) },
            new() { Name = "BIN-010", Location = "Sports Complex", Latitude = 51.5055, Longitude = -0.1320, Type = BinType.General, Status = BinStatus.Active, CurrentFillLevel = 28.3, CapacityLiters = 1100, LastCollected = now.AddDays(-1), LastSensorReading = now.AddMinutes(-6), InstalledDate = now.AddYears(-1) },
            new() { Name = "BIN-011", Location = "Library Road", Latitude = 51.5065, Longitude = -0.1235, Type = BinType.Paper, Status = BinStatus.Active, CurrentFillLevel = 82.1, CapacityLiters = 1100, LastCollected = now.AddDays(-2), LastSensorReading = now.AddMinutes(-2), InstalledDate = now.AddMonths(-24) },
            new() { Name = "BIN-012", Location = "Industrial Estate Gate", Latitude = 51.5030, Longitude = -0.1295, Type = BinType.Glass, Status = BinStatus.Inactive, CurrentFillLevel = 15.0, CapacityLiters = 500, LastCollected = now.AddDays(-7), LastSensorReading = now.AddHours(-6), InstalledDate = now.AddYears(-4) },
        };

        await db.WasteBins.AddRangeAsync(bins);
        await db.SaveChangesAsync();

        // 4 Trucks
        var trucks = new List<Truck>
        {
            new() { Name = "TRUCK-01", RegistrationNumber = "WM-1001", Status = TruckStatus.Available, CurrentLatitude = 51.5075, CurrentLongitude = -0.1285, FuelLevelPercent = 92.0, BinsCollectedToday = 0, CurrentRoute = null, LastUpdated = now.AddMinutes(-5) },
            new() { Name = "TRUCK-02", RegistrationNumber = "WM-1002", Status = TruckStatus.OnRoute, CurrentLatitude = 51.5082, CurrentLongitude = -0.1268, FuelLevelPercent = 67.5, BinsCollectedToday = 4, CurrentRoute = "Route-A-North", LastUpdated = now.AddMinutes(-2) },
            new() { Name = "TRUCK-03", RegistrationNumber = "WM-1003", Status = TruckStatus.Collecting, CurrentLatitude = 51.5091, CurrentLongitude = -0.1301, FuelLevelPercent = 45.2, BinsCollectedToday = 7, CurrentRoute = "Route-B-South", LastUpdated = now.AddMinutes(-1) },
            new() { Name = "TRUCK-04", RegistrationNumber = "WM-1004", Status = TruckStatus.OutOfService, CurrentLatitude = 51.5048, CurrentLongitude = -0.1257, FuelLevelPercent = 22.0, BinsCollectedToday = 0, CurrentRoute = null, LastUpdated = now.AddHours(-3) },
        };

        await db.Trucks.AddRangeAsync(trucks);
        await db.SaveChangesAsync();

        // 8 Collections
        var collections = new List<Collection>
        {
            new() { BinId = bins[0].Id, TruckId = trucks[1].Id, Status = CollectionStatus.Scheduled, ScheduledAt = now.AddHours(1), Notes = "High priority - over threshold" },
            new() { BinId = bins[2].Id, TruckId = trucks[1].Id, Status = CollectionStatus.Scheduled, ScheduledAt = now.AddHours(2), Notes = "Urgent - organic waste" },
            new() { BinId = bins[6].Id, TruckId = trucks[2].Id, Status = CollectionStatus.InProgress, ScheduledAt = now.AddHours(-1), StartedAt = now.AddMinutes(-30), Notes = "En route" },
            new() { BinId = bins[4].Id, TruckId = trucks[2].Id, Status = CollectionStatus.Scheduled, ScheduledAt = now.AddHours(3), Notes = null },
            new() { BinId = bins[1].Id, TruckId = trucks[0].Id, Status = CollectionStatus.Completed, ScheduledAt = now.AddDays(-1).AddHours(9), StartedAt = now.AddDays(-1).AddHours(9).AddMinutes(15), CompletedAt = now.AddDays(-1).AddHours(9).AddMinutes(45), FillLevelAtCollection = 78.5, Notes = "Completed on schedule" },
            new() { BinId = bins[3].Id, TruckId = trucks[0].Id, Status = CollectionStatus.Completed, ScheduledAt = now.AddDays(-1).AddHours(10), StartedAt = now.AddDays(-1).AddHours(10).AddMinutes(10), CompletedAt = now.AddDays(-1).AddHours(10).AddMinutes(35), FillLevelAtCollection = 52.1, Notes = null },
            new() { BinId = bins[9].Id, TruckId = trucks[1].Id, Status = CollectionStatus.Completed, ScheduledAt = now.AddDays(-2).AddHours(8), StartedAt = now.AddDays(-2).AddHours(8).AddMinutes(20), CompletedAt = now.AddDays(-2).AddHours(8).AddMinutes(50), FillLevelAtCollection = 89.2, Notes = "Bin was overfull" },
            new() { BinId = bins[7].Id, TruckId = trucks[3].Id, Status = CollectionStatus.Cancelled, ScheduledAt = now.AddDays(-1).AddHours(14), Notes = "Truck went out of service" },
        };

        await db.Collections.AddRangeAsync(collections);
        await db.SaveChangesAsync();

        // 3 Alerts
        var alerts = new List<Alert>
        {
            new() { Type = AlertType.BinFull, Severity = AlertSeverity.Critical, Message = "BIN-001 has exceeded the 85% fill threshold (87.5%)", BinId = bins[0].Id, IsAcknowledged = false, CreatedAt = now.AddHours(-1) },
            new() { Type = AlertType.BinFull, Severity = AlertSeverity.Critical, Message = "BIN-003 has exceeded the 85% fill threshold (91.2%)", BinId = bins[2].Id, IsAcknowledged = false, CreatedAt = now.AddHours(-2) },
            new() { Type = AlertType.SensorTimeout, Severity = AlertSeverity.Warning, Message = "BIN-008 sensor has not reported in over 2 hours", BinId = bins[7].Id, IsAcknowledged = true, AcknowledgedBy = "operator1", CreatedAt = now.AddHours(-3), AcknowledgedAt = now.AddHours(-2) },
        };

        await db.Alerts.AddRangeAsync(alerts);
        await db.SaveChangesAsync();

        // Fill level readings: 24 readings per bin for first 12 bins (one per hour for last 24h)
        var readings = new List<FillLevelReading>();
        var random = new Random(42);

        for (int binIdx = 0; binIdx < bins.Count; binIdx++)
        {
            var bin = bins[binIdx];
            double startLevel = Math.Max(5.0, bin.CurrentFillLevel - (24 * 1.5));
            for (int hour = 23; hour >= 0; hour--)
            {
                double increment = 0.5 + (random.NextDouble() * 1.5);
                double level = Math.Min(100.0, startLevel + ((23 - hour) * increment));
                readings.Add(new FillLevelReading
                {
                    BinId = bin.Id,
                    FillLevel = Math.Round(level, 1),
                    Temperature = Math.Round(18.0 + random.NextDouble() * 10.0, 1),
                    BatteryLevel = Math.Round(70.0 + random.NextDouble() * 30.0, 1),
                    RecordedAt = now.AddHours(-hour)
                });
            }
        }

        await db.FillLevelReadings.AddRangeAsync(readings);
        await db.SaveChangesAsync();

        logger.LogInformation("Database seeded successfully: {BinCount} bins, {TruckCount} trucks, {CollectionCount} collections, {AlertCount} alerts, {ReadingCount} readings",
            bins.Count, trucks.Count, collections.Count, alerts.Count, readings.Count);
    }
}
