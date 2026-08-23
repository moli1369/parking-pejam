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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingSpot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SpotNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Zone).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.Zone, x.SpotNumber }).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
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
    }
}
