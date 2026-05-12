namespace WasteManagementApi.DTOs;

public record TruckSummary(
    int Id,
    string Name,
    string RegistrationNumber,
    string Status,
    double FuelLevelPercent,
    int BinsCollectedToday,
    string? CurrentRoute,
    DateTime LastUpdated
);

public record TruckDetail(
    int Id,
    string Name,
    string RegistrationNumber,
    string Status,
    double FuelLevelPercent,
    int BinsCollectedToday,
    string? CurrentRoute,
    DateTime LastUpdated,
    double CurrentLatitude,
    double CurrentLongitude,
    IList<CollectionSummary> RecentCollections
);

public record UpdateTruckLocationRequest(
    double Latitude,
    double Longitude
);
