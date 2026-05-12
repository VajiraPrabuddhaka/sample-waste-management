namespace WasteManagementApi.Models;

public class Collection
{
    public int Id { get; set; }
    public int BinId { get; set; }
    public int TruckId { get; set; }
    public CollectionStatus Status { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? FillLevelAtCollection { get; set; }
    public string? Notes { get; set; }

    public WasteBin Bin { get; set; } = null!;
    public Truck Truck { get; set; } = null!;
}
