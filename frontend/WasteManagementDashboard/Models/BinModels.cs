namespace WasteManagementDashboard.Models;

public record BinSummary(
    int Id,
    string Name,
    string Location,
    string Type,
    string Status,
    double CurrentFillLevel,
    double CapacityLiters,
    DateTime LastCollected,
    DateTime LastSensorReading
);

public record BinDetail(
    int Id,
    string Name,
    string Location,
    string Type,
    string Status,
    double CurrentFillLevel,
    double CapacityLiters,
    DateTime LastCollected,
    DateTime LastSensorReading,
    double Latitude,
    double Longitude,
    DateTime InstalledDate,
    IList<FillLevelReadingDto> RecentReadings
);

public record CreateBinRequest(
    string Name,
    string Location,
    double Latitude,
    double Longitude,
    string Type,
    double CapacityLiters
);

public record FillLevelReadingDto(
    double FillLevel,
    double? Temperature,
    double? BatteryLevel,
    DateTime RecordedAt
);
