using Microsoft.EntityFrameworkCore;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Infrastructure;

public sealed class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
    public DbSet<ParkingEvent> ParkingEvents => Set<ParkingEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ParkingSensor> ParkingSensors => Set<ParkingSensor>();
    public DbSet<ParkingSensorReading> ParkingSensorReadings => Set<ParkingSensorReading>();
    public DbSet<ImportShipment> ImportShipments => Set<ImportShipment>();
    public DbSet<ImportedVehicle> ImportedVehicles => Set<ImportedVehicle>();
    public DbSet<VehicleArrivalRecord> VehicleArrivalRecords => Set<VehicleArrivalRecord>();
    public DbSet<VehicleDispatchRecord> VehicleDispatchRecords => Set<VehicleDispatchRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingSpot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SpotNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Zone).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.Zone, x.SpotNumber }).IsUnique();
            entity.HasIndex(x => x.ImportedVehicleId).IsUnique().HasFilter("ImportedVehicleId IS NOT NULL");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasOne(x => x.ImportedVehicle).WithMany().HasForeignKey(x => x.ImportedVehicleId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ParkingEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(128);
            entity.Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.TimestampUtc);
            entity.HasOne(x => x.ParkingSpot).WithMany().HasForeignKey(x => x.ParkingSpotId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<ParkingSensor>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.Property(x => x.DeviceKey).HasMaxLength(512).IsRequired();
            entity.HasOne(x => x.ParkingSpot).WithMany().HasForeignKey(x => x.ParkingSpotId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParkingSensorReading>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ParkingSensorId, x.ReceivedAtUtc });
            entity.HasOne(x => x.ParkingSensor).WithMany().HasForeignKey(x => x.ParkingSensorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportShipment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VesselName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.VoyageNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PortOfEntry).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ShipmentReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.BillOfLadingNumber).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.ShipmentReference).IsUnique();
        });

        modelBuilder.Entity<ImportedVehicle>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Vin).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Vin).IsUnique();
            entity.Property(x => x.EngineNumber).HasMaxLength(64);
            entity.Property(x => x.Make).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(64);
            entity.Property(x => x.OriginCountry).HasMaxLength(64);
            entity.Property(x => x.TemporaryPlate).HasMaxLength(32);
            entity.Property(x => x.CustomsStatus).HasMaxLength(64);
            entity.Property(x => x.DamageNotes).HasMaxLength(1000);
            entity.Property(x => x.Condition).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.InventoryStatus).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => new { x.ImportShipmentId, x.TallySequence }).IsUnique();
            entity.HasOne(x => x.ImportShipment).WithMany().HasForeignKey(x => x.ImportShipmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VehicleArrivalRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Source).HasMaxLength(32).IsRequired();
            entity.Property(x => x.OperatorUsername).HasMaxLength(64);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.ImportShipmentId, x.TallySequence }).IsUnique();
            entity.HasOne(x => x.ImportedVehicle).WithMany().HasForeignKey(x => x.ImportedVehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VehicleDispatchRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DispatchReference).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.DispatchReference).IsUnique();
            entity.Property(x => x.ReleaseAuthorization).HasMaxLength(120);
            entity.Property(x => x.DriverName).HasMaxLength(160);
            entity.Property(x => x.DriverId).HasMaxLength(100);
            entity.Property(x => x.Destination).HasMaxLength(200);
            entity.Property(x => x.TransportCompany).HasMaxLength(160);
            entity.Property(x => x.OperatorUsername).HasMaxLength(128);
            entity.HasOne(x => x.ImportedVehicle).WithMany().HasForeignKey(x => x.ImportedVehicleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
