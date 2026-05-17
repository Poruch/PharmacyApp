using PharmacyApp.Models;

namespace PharmacyApp.Interfaces;

public interface IStorageLocationService
{
    IReadOnlyList<StorageLocation> GetAll();
    StorageLocation? GetById(int locationId);
    int GetOrCreate(string shelf, string? cell = null);
    string GetDisplayName(int? locationId);
}
