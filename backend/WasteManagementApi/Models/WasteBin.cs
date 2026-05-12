namespace WasteManagementApi.Models;

public class WasteBin
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;        // e.g., "BIN-001"
    public string Location { get; set; } = string.Empty;     // e.g., "Main Street & 5th Ave"
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public BinType Type { get; set; }
    public BinStatus Status { get; set; }
    public double CurrentFillLevel { get; set; }             // 0.0 - 100.0
    public double CapacityLiters { get; set; }               // e.g., 1100
    public DateTime LastCollected { get; set; }
    public DateTime LastSensorReading { get; set; }
    public DateTime InstalledDate { get; set; }

    public ICollection<FillLevelReading> FillLevelReadings { get; set; } = [];
    public ICollection<Collection> Collections { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
}
