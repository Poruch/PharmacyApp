using Dapper;
using Microsoft.Data.SqlClient;

namespace PharmacyApp.Services;

/// <summary>
/// Переводит BATCH.StorageLocation (строка) на StorageLocationId + справочник STORAGE_LOCATION.
/// </summary>
public static class StorageLocationSchemaMigrator
{
    public static void MigrateIfNeeded()
    {
        using var conn = new SqlConnection(ConfigManager.ConnectionString);
        conn.Open();

        if (!TableExists(conn, "BATCH"))
            return;

        EnsureStorageLocationTable(conn);

        bool hasLegacyColumn = ColumnExists(conn, "BATCH", "StorageLocation");
        bool hasFkColumn = ColumnExists(conn, "BATCH", "StorageLocationId");

        if (!hasFkColumn)
            conn.Execute("ALTER TABLE [BATCH] ADD [StorageLocationId] INT NULL");

        SeedDefaultLocationsIfEmpty(conn);

        if (hasLegacyColumn)
        {
            var legacyValues = conn.Query<string>(@"
                SELECT DISTINCT StorageLocation FROM BATCH
                WHERE StorageLocation IS NOT NULL AND LTRIM(RTRIM(StorageLocation)) <> ''")
                .ToList();

            var locationService = new StorageLocationService();
            foreach (var legacy in legacyValues)
            {
                var (shelf, cell) = ParseLegacyLocation(legacy);
                int locationId = locationService.GetOrCreate(shelf, cell);

                conn.Execute(@"
                    UPDATE BATCH SET StorageLocationId = @locationId
                    WHERE StorageLocation = @legacy AND (StorageLocationId IS NULL OR StorageLocationId = 0)",
                    new { locationId, legacy });
            }

            DropLegacyConstraintIfExists(conn, "FK_BATCH_STORAGE_LOCATION_StorageLocationId");
            DropLegacyConstraintIfExists(conn, "FK_BATCH_STORAGE_LOCATION");

            if (ColumnExists(conn, "BATCH", "StorageLocation"))
                conn.Execute("ALTER TABLE [BATCH] DROP COLUMN [StorageLocation]");
        }

        EnsureForeignKey(conn);
    }

    private static void EnsureStorageLocationTable(SqlConnection conn)
    {
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STORAGE_LOCATION')
            BEGIN
                CREATE TABLE [STORAGE_LOCATION] (
                    [LocationId] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    [Shelf] NVARCHAR(100) NOT NULL,
                    [Cell] NVARCHAR(50) NULL
                );
            END");
    }

    private static void EnsureForeignKey(SqlConnection conn)
    {
        if (!ColumnExists(conn, "BATCH", "StorageLocationId"))
            return;

        DropLegacyConstraintIfExists(conn, "FK_BATCH_STORAGE_LOCATION_StorageLocationId");

        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BATCH_STORAGE_LOCATION')
               AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'STORAGE_LOCATION')
            BEGIN
                ALTER TABLE [BATCH]
                ADD CONSTRAINT [FK_BATCH_STORAGE_LOCATION]
                FOREIGN KEY ([StorageLocationId]) REFERENCES [STORAGE_LOCATION]([LocationId])
                ON DELETE NO ACTION;
            END");
    }

    private static void DropLegacyConstraintIfExists(SqlConnection conn, string constraintName)
    {
        conn.Execute($@"
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{constraintName}')
                ALTER TABLE [BATCH] DROP CONSTRAINT [{constraintName}]");
    }

    private static bool TableExists(SqlConnection conn, string tableName) =>
        conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE name = @name",
            new { name = tableName }) > 0;

    private static bool ColumnExists(SqlConnection conn, string tableName, string columnName) =>
        conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            WHERE t.name = @table AND c.name = @column",
            new { table = tableName, column = columnName }) > 0;

    private static void SeedDefaultLocationsIfEmpty(SqlConnection conn)
    {
        if (!TableExists(conn, "STORAGE_LOCATION"))
            return;

        int count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM STORAGE_LOCATION");
        if (count > 0)
            return;

        var service = new StorageLocationService();
        service.GetOrCreate("Стеллаж А", null);
        service.GetOrCreate("Стеллаж А", "полка 1");
        service.GetOrCreate("Стеллаж А", "полка 2");
        service.GetOrCreate("Стеллаж Б", "полка 1");
        service.GetOrCreate("Стеллаж Б", "полка 2");
        service.GetOrCreate("Холодильник №1", "полка 1");
        service.GetOrCreate("Холодильник №1", "полка 2");
        service.GetOrCreate("Касса", "витрина");
    }

    private static (string shelf, string? cell) ParseLegacyLocation(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ("Не указано", null);

        const string marker = " полка ";
        int idx = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
            return (value[..idx].Trim(), value[(idx + marker.Length)..].Trim());

        return (value.Trim(), null);
    }
}
