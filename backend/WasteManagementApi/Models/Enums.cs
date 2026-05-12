namespace WasteManagementApi.Models;

public enum BinType { General, Recycling, Organic, Glass, Paper }
public enum BinStatus { Active, Inactive, Maintenance }
public enum TruckStatus { Available, OnRoute, Collecting, Returning, OutOfService }
public enum CollectionStatus { Scheduled, InProgress, Completed, Cancelled }
public enum AlertSeverity { Info, Warning, Critical }
public enum AlertType { BinFull, SensorTimeout, CollectionOverdue, TruckIssue }
