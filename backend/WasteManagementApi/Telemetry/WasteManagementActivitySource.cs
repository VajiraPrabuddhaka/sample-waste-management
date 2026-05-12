using System.Diagnostics;

namespace WasteManagementApi.Telemetry;

public static class WasteManagementActivitySource
{
    public const string SourceName = "WasteManagementApi";
    private static readonly ActivitySource Source = new(SourceName);

    public static Activity? StartBinGetDetail(int binId, double fillLevel)
    {
        var activity = Source.StartActivity("bin.get-detail");
        activity?.SetTag("bin.id", binId);
        activity?.SetTag("bin.fill_level", fillLevel);
        return activity;
    }

    public static Activity? StartCollectionSchedule(int binId, int truckId, DateTime scheduledAt)
    {
        var activity = Source.StartActivity("collection.schedule");
        activity?.SetTag("bin.id", binId);
        activity?.SetTag("truck.id", truckId);
        activity?.SetTag("scheduled_at", scheduledAt.ToString("O"));
        return activity;
    }

    public static Activity? StartCollectionComplete(int collectionId, double durationMinutes)
    {
        var activity = Source.StartActivity("collection.complete");
        activity?.SetTag("collection.id", collectionId);
        activity?.SetTag("duration_minutes", durationMinutes);
        return activity;
    }

    public static Activity? StartAlertGenerate(string alertType, string severity, int? binId)
    {
        var activity = Source.StartActivity("alert.generate");
        activity?.SetTag("alert.type", alertType);
        activity?.SetTag("alert.severity", severity);
        if (binId.HasValue) activity?.SetTag("bin.id", binId.Value);
        return activity;
    }

    public static Activity? StartSensorRead(int binId, double fillLevel, double? batteryLevel)
    {
        var activity = Source.StartActivity("sensor.read");
        activity?.SetTag("bin.id", binId);
        activity?.SetTag("fill_level", fillLevel);
        if (batteryLevel.HasValue) activity?.SetTag("battery_level", batteryLevel.Value);
        return activity;
    }

    public static Activity? StartSensorSimulateCycle(int binCount)
    {
        var activity = Source.StartActivity("sensor.simulate-cycle");
        activity?.SetTag("bin.count", binCount);
        return activity;
    }
}
