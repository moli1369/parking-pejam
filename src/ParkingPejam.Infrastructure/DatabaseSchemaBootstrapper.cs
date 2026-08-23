using Microsoft.EntityFrameworkCore;

namespace ParkingPejam.Infrastructure;

public static class DatabaseSchemaBootstrapper
{
    public static async Task EnsureImportSchemaAsync(ParkingDbContext db, CancellationToken ct = default)
    {
        await EnsureColumnAsync(db, "ParkingSpots", "ImportedVehicleId", "TEXT NULL", ct);
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ParkingSpots_ImportedVehicleId ON ParkingSpots (ImportedVehicleId) WHERE ImportedVehicleId IS NOT NULL;", ct);

        await db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS ImportShipments (
    Id TEXT NOT NULL CONSTRAINT PK_ImportShipments PRIMARY KEY,
    VesselName TEXT NOT NULL,
    VoyageNumber TEXT NOT NULL,
    PortOfEntry TEXT NOT NULL,
    ShipmentReference TEXT NOT NULL,
    BillOfLadingNumber TEXT NOT NULL,
    DeclaredVehicleCount INTEGER NOT NULL,
    Status TEXT NOT NULL,
    ArrivalStartedUtc TEXT NULL,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_ImportShipments_ShipmentReference ON ImportShipments (ShipmentReference);

CREATE TABLE IF NOT EXISTS ImportedVehicles (
    Id TEXT NOT NULL CONSTRAINT PK_ImportedVehicles PRIMARY KEY,
    ImportShipmentId TEXT NOT NULL,
    Vin TEXT NOT NULL,
    EngineNumber TEXT NULL,
    Make TEXT NOT NULL,
    Model TEXT NOT NULL,
    ModelYear INTEGER NULL,
    Color TEXT NULL,
    Condition TEXT NOT NULL,
    OriginCountry TEXT NULL,
    TemporaryPlate TEXT NULL,
    CustomsStatus TEXT NULL,
    DamageNotes TEXT NULL,
    TallySequence INTEGER NOT NULL,
    InventoryStatus TEXT NOT NULL,
    ReceivedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL,
    CONSTRAINT FK_ImportedVehicles_ImportShipments FOREIGN KEY (ImportShipmentId) REFERENCES ImportShipments (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_ImportedVehicles_Vin ON ImportedVehicles (Vin);
CREATE UNIQUE INDEX IF NOT EXISTS IX_ImportedVehicles_ImportShipmentId_TallySequence ON ImportedVehicles (ImportShipmentId, TallySequence);

CREATE TABLE IF NOT EXISTS VehicleArrivalRecords (
    Id TEXT NOT NULL CONSTRAINT PK_VehicleArrivalRecords PRIMARY KEY,
    ImportedVehicleId TEXT NOT NULL,
    ImportShipmentId TEXT NOT NULL,
    TallySequence INTEGER NOT NULL,
    Source TEXT NOT NULL,
    OperatorUsername TEXT NULL,
    ReceivedAtUtc TEXT NOT NULL,
    Notes TEXT NULL,
    CONSTRAINT FK_VehicleArrivalRecords_ImportedVehicles FOREIGN KEY (ImportedVehicleId) REFERENCES ImportedVehicles (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_VehicleArrivalRecords_ImportShipmentId_TallySequence ON VehicleArrivalRecords (ImportShipmentId, TallySequence);
", ct);
    }

    private static async Task EnsureColumnAsync(ParkingDbContext db, string table, string column, string definition, CancellationToken ct)
    {
        var exists = await db.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM pragma_table_info('{table}') WHERE name = '{column}'").SingleAsync(ct);
        if (exists == 0)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};", ct);
    }
}
