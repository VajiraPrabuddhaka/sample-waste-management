namespace WasteManagementApi.Models;

public class Truck
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;         // e.g., "TRUCK-01"
    public string RegistrationNumber { get; set; } = string.Empty;
    public TruckStatus Status { get; set; }
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }
    public double FuelLevelPercent { get; set; }              // 0 - 100
    public int BinsCollectedToday { get; set; }
    public string? CurrentRoute { get; set; }
    public DateTime LastUpdated { get; set; }

    public ICollection<Collection> Collections { get; set; } = [];
}
