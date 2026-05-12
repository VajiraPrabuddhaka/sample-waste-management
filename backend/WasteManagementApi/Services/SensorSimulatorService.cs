using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Data;
using WasteManagementApi.Models;
using WasteManagementApi.Telemetry;

namespace WasteManagementApi.Services;

public class SensorSimulatorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorSimulatorService> _logger;
    private readonly WasteManagementMetrics _metrics;
    private readonly SimulationStateService _simState;
    private readonly Random _random = new();

    public SensorSimulatorService(
        IServiceScopeFactory scopeFactory,
        ILogger<SensorSimulatorService> logger,
        WasteManagementMetrics metrics,
        SimulationStateService simState)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
        _simState = simState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorSimulatorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = _simState.AcceleratedMode ? 2 : 10;
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);

            await SimulateSensorCycleAsync(stoppingToken);
        }

        _logger.LogInformation("SensorSimulatorService stopped");
    }

    private async Task SimulateSensorCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WasteManagementDbContext>();

        var activeBins = await db.WasteBins
            .Where(b => b.Status == BinStatus.Active)
            .ToListAsync(cancellationToken);

        using var cycleActivity = WasteManagementActivitySource.StartSensorSimulateCycle(activeBins.Count);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var readings = new List<FillLevelReading>();

        // Sensor timeout probability: 5% normal, 50% chaos
        double timeoutProbability = _simState.ChaosMode ? 0.50 : 0.05;

        foreach (var bin in activeBins)
        {
            // Simulate sensor timeout
            if (_random.NextDouble() < timeoutProbability)
            {
                _logger.LogWarning("SensorTimeout: BinId={BinId}, BinName={BinName} - sensor did not respond",
                    bin.Id, bin.Name);
                continue;
            }

            var previousLevel = bin.CurrentFillLevel;

            // Was recently collected? Reset to low level
            var timeSinceCollection = DateTime.UtcNow - bin.LastCollected;
            if (timeSinceCollection.TotalMinutes < 15 && previousLevel > 20.0)
            {
                bin.CurrentFillLevel = 5.0 + _random.NextDouble() * 10.0;
            }
            else
            {
                // Gradual increase +0.5% to +2%
                var increment = 0.5 + _random.NextDouble() * 1.5;
                bin.CurrentFillLevel = Math.Min(100.0, bin.CurrentFillLevel + increment);
            }

            bin.LastSensorReading = DateTime.UtcNow;

            var reading = new FillLevelReading
            {
                BinId = bin.Id,
                FillLevel = Math.Round(bin.CurrentFillLevel, 1),
                Temperature = Math.Round(15.0 + _random.NextDouble() * 15.0, 1),
                BatteryLevel = Math.Round(60.0 + _random.NextDouble() * 40.0, 1),
                RecordedAt = DateTime.UtcNow
            };
            readings.Add(reading);

            using var readActivity = WasteManagementActivitySource.StartSensorRead(bin.Id, reading.FillLevel, reading.BatteryLevel);

            _logger.LogInformation("SensorReading: BinId={BinId}, BinName={BinName}, FillLevel={FillLevel}, PreviousFillLevel={PreviousFillLevel}, Delta={Delta:F1}",
                bin.Id, bin.Name, reading.FillLevel, Math.Round(previousLevel, 1), reading.FillLevel - previousLevel);
        }

        db.FillLevelReadings.AddRange(readings);
        await db.SaveChangesAsync(cancellationToken);

        sw.Stop();

        // Update gauge metrics
        if (activeBins.Count > 0)
        {
            var avgFillLevel = activeBins.Average(b => b.CurrentFillLevel);
            _metrics.UpdateAverageFillLevel(avgFillLevel);
        }

        _logger.LogInformation("SensorSimulateCycle: BinsProcessed={BinsProcessed}, ReadingsCreated={ReadingsCreated}, Duration={Duration}ms",
            activeBins.Count, readings.Count, sw.ElapsedMilliseconds);
    }
}
