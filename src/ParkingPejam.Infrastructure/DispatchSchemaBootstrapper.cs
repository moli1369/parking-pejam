using Microsoft.EntityFrameworkCore;

namespace ParkingPejam.Infrastructure;

public static class DispatchSchemaBootstrapper
{
    public static Task EnsureAsync(ParkingDbContext db, CancellationToken ct = default) => db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS VehicleDispatchRecords (
    Id INTEGER NOT NULL CONSTRAINT PK_VehicleDispatchRecords PRIMARY KEY AUTOINCREMENT,
    ImportedVehicleId TEXT NOT NULL,
    DispatchReference TEXT NOT NULL,
    ReleaseAuthorization TEXT NULL,
    DriverName TEXT NULL,
    DriverId TEXT NULL,
    Destination TEXT NULL,
    TransportCompany TEXT NULL,
    Notes TEXT NULL,
    OperatorUsername TEXT NULL,
    DispatchedAtUtc TEXT NOT NULL,
    CONSTRAINT FK_VehicleDispatchRecords_ImportedVehicles FOREIGN KEY (ImportedVehicleId) REFERENCES ImportedVehicles (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_VehicleDispatchRecords_DispatchReference ON VehicleDispatchRecords (DispatchReference);
", ct);
}
