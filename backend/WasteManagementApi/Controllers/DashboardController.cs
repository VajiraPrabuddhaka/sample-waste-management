using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.DTOs;
using WasteManagementApi.Models;

namespace WasteManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly WasteManagementDbContext _db;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(WasteManagementDbContext db, ILogger<DashboardController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummary>> GetDashboard()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var bins = await _db.WasteBins.ToListAsync();
        var trucks = await _db.Trucks.ToListAsync();

        var today = DateTime.UtcNow.Date;
        var collections = await _db.Collections.ToListAsync();

        var todayCollections = collections.Count(c =>
            c.Status == CollectionStatus.Completed && c.CompletedAt.HasValue && c.CompletedAt.Value.Date == today);
        var pendingCollections = collections.Count(c => c.Status == CollectionStatus.Scheduled || c.Status == CollectionStatus.InProgress);
        var totalScheduled = collections.Count(c =>
            c.ScheduledAt.Date == today && c.Status != CollectionStatus.Cancelled);
        var completionRate = totalScheduled > 0 ? (double)todayCollections / totalScheduled * 100.0 : 0.0;

        var alerts = await _db.Alerts.ToListAsync();
        var openAlerts = alerts.Count(a => !a.IsAcknowledged);
        var criticalAlerts = alerts.Count(a => !a.IsAcknowledged && a.Severity == AlertSeverity.Critical);

        var activeBins = bins.Where(b => b.Status == BinStatus.Active).ToList();
        var avgFillLevel = activeBins.Count > 0 ? activeBins.Average(b => b.CurrentFillLevel) : 0.0;
        var binsNearFull = activeBins.Count(b => b.CurrentFillLevel >= 70.0);
        var binsOverThreshold = activeBins.Count(b => b.CurrentFillLevel >= 85.0);

        var activeTrucks = trucks.Count(t => t.Status != TruckStatus.OutOfService && t.Status != TruckStatus.Returning);

        var summary = new DashboardSummary(
            TotalBins: bins.Count,
            ActiveBins: activeBins.Count,
            BinsNearFull: binsNearFull,
            BinsOverThreshold: binsOverThreshold,
            TotalTrucks: trucks.Count,
            ActiveTrucks: activeTrucks,
            TodayCollections: todayCollections,
            PendingCollections: pendingCollections,
            OpenAlerts: openAlerts,
            CriticalAlerts: criticalAlerts,
            AverageFillLevel: Math.Round(avgFillLevel, 1),
            CollectionCompletionRate: Math.Round(completionRate, 1)
        );

        sw.Stop();
        _logger.LogInformation("GetDashboard: TotalBins={TotalBins}, OpenAlerts={OpenAlerts}, AvgFill={AvgFill}, Duration={Duration}ms",
            bins.Count, openAlerts, summary.AverageFillLevel, sw.ElapsedMilliseconds);

        return Ok(summary);
    }
}
