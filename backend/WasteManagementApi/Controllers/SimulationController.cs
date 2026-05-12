using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.Services;

namespace WasteManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly SimulationStateService _simState;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(
        SimulationStateService simState,
        IServiceScopeFactory scopeFactory,
        ILogger<SimulationController> logger)
    {
        _simState = simState;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            ChaosMode = _simState.ChaosMode,
            AcceleratedMode = _simState.AcceleratedMode
        });
    }

    [HttpPut("chaos")]
    public IActionResult ToggleChaos()
    {
        _simState.ChaosMode = !_simState.ChaosMode;
        _logger.LogInformation("ChaosMode toggled: {ChaosMode}", _simState.ChaosMode);
        return Ok(new { ChaosMode = _simState.ChaosMode });
    }

    [HttpPut("accelerate")]
    public IActionResult ToggleAccelerate()
    {
        _simState.AcceleratedMode = !_simState.AcceleratedMode;
        _logger.LogInformation("AcceleratedMode toggled: {AcceleratedMode}", _simState.AcceleratedMode);
        return Ok(new { AcceleratedMode = _simState.AcceleratedMode });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WasteManagementDbContext>();
        var random = new Random();

        var bins = await db.WasteBins.ToListAsync();
        foreach (var bin in bins)
            bin.CurrentFillLevel = 20.0 + random.NextDouble() * 40.0;

        var alerts = await db.Alerts.Where(a => !a.IsAcknowledged).ToListAsync();
        foreach (var alert in alerts)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = "system-reset";
            alert.AcknowledgedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        _simState.ChaosMode = false;
        _simState.AcceleratedMode = false;

        _logger.LogInformation("Simulation reset: BinsReset={BinCount}, AlertsCleared={AlertCount}",
            bins.Count, alerts.Count);

        return Ok(new { message = "Simulation reset", binsReset = bins.Count, alertsCleared = alerts.Count });
    }
}
