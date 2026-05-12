using Microsoft.EntityFrameworkCore;
using WasteManagementApi.Models;

namespace WasteManagementApi.Data;

public class WasteManagementDbContext : DbContext
{
    public WasteManagementDbContext(DbContextOptions<WasteManagementDbContext> options) : base(options) { }

    public DbSet<WasteBin> WasteBins => Set<WasteBin>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<FillLevelReading> FillLevelReadings => Set<FillLevelReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WasteBin>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(50);
            e.Property(b => b.Location).HasMaxLength(200);
            e.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<Truck>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(50);
            e.Property(t => t.RegistrationNumber).HasMaxLength(20);
            e.HasIndex(t => t.RegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<Collection>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.Bin).WithMany(b => b.Collections).HasForeignKey(c => c.BinId);
            e.HasOne(c => c.Truck).WithMany(t => t.Collections).HasForeignKey(c => c.TruckId);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Message).HasMaxLength(500);
            e.HasOne(a => a.Bin).WithMany(b => b.Alerts).HasForeignKey(a => a.BinId);
            e.HasOne(a => a.Truck).WithMany().HasForeignKey(a => a.TruckId);
        });

        modelBuilder.Entity<FillLevelReading>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.Bin).WithMany(b => b.FillLevelReadings).HasForeignKey(r => r.BinId);
            e.HasIndex(r => new { r.BinId, r.RecordedAt });
        });
    }
}
