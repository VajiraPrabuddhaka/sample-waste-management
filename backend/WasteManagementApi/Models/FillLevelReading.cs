namespace WasteManagementApi.Models;

public class FillLevelReading
{
    public int Id { get; set; }
    public int BinId { get; set; }
    public double FillLevel { get; set; }                    // 0.0 - 100.0
    public double? Temperature { get; set; }                 // Celsius
    public double? BatteryLevel { get; set; }                // Sensor battery %
    public DateTime RecordedAt { get; set; }

    public WasteBin Bin { get; set; } = null!;
}
