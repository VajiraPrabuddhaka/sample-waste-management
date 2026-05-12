namespace WasteManagementDashboard.Models;

public record CollectionSummary(
    int Id,
    int BinId,
    string BinName,
    int TruckId,
    string TruckName,
    string Status,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? FillLevelAtCollection,
    string? Notes
);

public record ScheduleCollectionRequest(
    int BinId,
    int TruckId,
    DateTime ScheduledAt,
    string? Notes
);
