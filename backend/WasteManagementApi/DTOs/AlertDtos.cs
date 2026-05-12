namespace WasteManagementApi.DTOs;

public record AlertSummary(
    int Id,
    string Type,
    string Severity,
    string Message,
    int? BinId,
    string? BinName,
    int? TruckId,
    string? TruckName,
    bool IsAcknowledged,
    string? AcknowledgedBy,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt
);

public record AcknowledgeAlertRequest(
    string AcknowledgedBy
);
