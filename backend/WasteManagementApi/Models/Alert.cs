namespace WasteManagementApi.Models;

public class Alert
{
    public int Id { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? BinId { get; set; }
    public int? TruckId { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public WasteBin? Bin { get; set; }
    public Truck? Truck { get; set; }
}
