using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.Models;
using WasteManagementApi.Telemetry;

namespace WasteManagementApi.Services;

public class AlertMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertMonitorService> _logger;
    private readonly WasteManagementMetrics _metrics;

    public AlertMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertMonitorService> logger,
        WasteManagementMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertMonitorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            await CheckAlertsAsync(stoppingToken);
        }

        _logger.LogInformation("AlertMonitorService stopped");
    }

    private async Task CheckAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WasteManagementDbContext>();

        var now = DateTime.UtcNow;
        var alertsCreated = 0;

        // Check active bins for full threshold and sensor timeout
        var activeBins = await db.WasteBins
            .Where(b => b.Status == BinStatus.Active)
            .ToListAsync(cancellationToken);

        // Get existing unacknowledged BinFull alerts to avoid duplicates
        var existingBinFullAlerts = await db.Alerts
            .Where(a => a.Type == AlertType.BinFull && !a.IsAcknowledged && a.BinId.HasValue)
            .Select(a => a.BinId!.Value)
            .ToHashSetAsync(cancellationToken);

        var existingSensorTimeoutAlerts = await db.Alerts
            .Where(a => a.Type == AlertType.SensorTimeout && !a.IsAcknowledged && a.BinId.HasValue)
            .Select(a => a.BinId!.Value)
            .ToHashSetAsync(cancellationToken);

        foreach (var bin in activeBins)
        {
            // BinFull check
            if (bin.CurrentFillLevel >= 85.0 && !existingBinFullAlerts.Contains(bin.Id))
            {
                var alert = new Alert
                {
                    Type = AlertType.BinFull,
                    Severity = AlertSeverity.Critical,
                    Message = $"{bin.Name} has exceeded the 85% fill threshold ({bin.CurrentFillLevel:F1}%)",
                    BinId = bin.Id,
                    IsAcknowledged = false,
                    CreatedAt = now
                };
                db.Alerts.Add(alert);
                alertsCreated++;

                _metrics.RecordAlertGenerated("BinFull", "Critical");
                using var activity = WasteManagementActivitySource.StartAlertGenerate("BinFull", "Critical", bin.Id);

                _logger.LogWarning("Alert created: Type=BinFull, Severity=Critical, BinId={BinId}, BinName={BinName}, FillLevel={FillLevel}",
                    bin.Id, bin.Name, bin.CurrentFillLevel);
            }

            // SensorTimeout check — no reading for > 5 minutes
            if ((now - bin.LastSensorReading).TotalMinutes > 5 && !existingSensorTimeoutAlerts.Contains(bin.Id))
            {
                var minutesSince = (int)(now - bin.LastSensorReading).TotalMinutes;
                var alert = new Alert
                {
                    Type = AlertType.SensorTimeout,
                    Severity = AlertSeverity.Warning,
                    Message = $"{bin.Name} sensor has not reported in {minutesSince} minutes",
                    BinId = bin.Id,
                    IsAcknowledged = false,
                    CreatedAt = now
                };
                db.Alerts.Add(alert);
                alertsCreated++;

                _metrics.RecordAlertGenerated("SensorTimeout", "Warning");
                using var activity = WasteManagementActivitySource.StartAlertGenerate("SensorTimeout", "Warning", bin.Id);

                _logger.LogWarning("Alert created: Type=SensorTimeout, Severity=Warning, BinId={BinId}, BinName={BinName}, MinutesSinceLastReading={Minutes}",
                    bin.Id, bin.Name, minutesSince);
            }
        }

        // Check for overdue collections
        var overdueThreshold = now.AddHours(-1);
        var overdueCollections = await db.Collections
            .Include(c => c.Bin)
            .Where(c => c.Status == CollectionStatus.Scheduled && c.ScheduledAt < overdueThreshold)
            .ToListAsync(cancellationToken);

        var existingOverdueAlerts = await db.Alerts
            .Where(a => a.Type == AlertType.CollectionOverdue && !a.IsAcknowledged)
            .ToListAsync(cancellationToken);

        foreach (var collection in overdueCollections)
        {
            // Avoid duplicate overdue alerts by checking existing ones
            bool alreadyAlerted = existingOverdueAlerts.Any(a =>
                a.BinId == collection.BinId && (now - a.CreatedAt).TotalHours < 2);

            if (!alreadyAlerted)
            {
                var hoursOverdue = (int)(now - collection.ScheduledAt).TotalHours;
                var alert = new Alert
                {
                    Type = AlertType.CollectionOverdue,
                    Severity = AlertSeverity.Warning,
                    Message = $"Collection for {collection.Bin?.Name ?? $"Bin #{collection.BinId}"} is {hoursOverdue}h overdue",
                    BinId = collection.BinId,
                    IsAcknowledged = false,
                    CreatedAt = now
                };
                db.Alerts.Add(alert);
                alertsCreated++;

                _metrics.RecordAlertGenerated("CollectionOverdue", "Warning");

                _logger.LogWarning("Alert created: Type=CollectionOverdue, Severity=Warning, CollectionId={CollectionId}, BinId={BinId}, HoursOverdue={Hours}",
                    collection.Id, collection.BinId, hoursOverdue);
            }
        }

        if (alertsCreated > 0)
            await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AlertMonitor cycle: AlertsCreated={AlertsCreated}, BinsChecked={BinsChecked}",
            alertsCreated, activeBins.Count);
    }
}
