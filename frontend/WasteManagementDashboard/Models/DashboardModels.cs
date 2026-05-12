namespace WasteManagementDashboard.Models;

public record DashboardSummary(
    int TotalBins,
    int ActiveBins,
    int BinsNearFull,
    int BinsOverThreshold,
    int TotalTrucks,
    int ActiveTrucks,
    int TodayCollections,
    int PendingCollections,
    int OpenAlerts,
    int CriticalAlerts,
    double AverageFillLevel,
    double CollectionCompletionRate
);

public record SimulationStatus(
    bool ChaosMode,
    bool AcceleratedMode
);
