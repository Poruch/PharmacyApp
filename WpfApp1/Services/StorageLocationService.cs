using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class StorageLocationService : IStorageLocationService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public StorageLocationService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public IReadOnlyList<StorageLocation> GetAll()
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<StorageLocation>(@"
            SELECT LocationId, Shelf, Cell
            FROM STORAGE_LOCATION
            ORDER BY Shelf, Cell").AsList();
    }

    public StorageLocation? GetById(int locationId)
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.QueryFirstOrDefault<StorageLocation>(@"
            SELECT LocationId, Shelf, Cell
            FROM STORAGE_LOCATION
            WHERE LocationId = @id",
            new { id = locationId });
    }

    public int GetOrCreate(string shelf, string? cell = null)
    {
        shelf = shelf.Trim();
        cell = string.IsNullOrWhiteSpace(cell) ? null : cell.Trim();

        using var conn = new SqlConnection(_connectionString);
        var existing = conn.QueryFirstOrDefault<StorageLocation>(@"
            SELECT LocationId, Shelf, Cell
            FROM STORAGE_LOCATION
            WHERE Shelf = @shelf AND ((@cell IS NULL AND Cell IS NULL) OR Cell = @cell)",
            new { shelf, cell });

        if (existing != null)
            return existing.LocationId;

        var location = new StorageLocation(shelf, cell);
        return _repo.Add(location);
    }

    public string GetDisplayName(int? locationId)
    {
        if (locationId == null)
            return "—";

        var loc = GetById(locationId.Value);
        return loc?.DisplayName ?? "—";
    }
}
