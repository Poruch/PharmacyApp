using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class ItemService : IItemService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public ItemService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public List<Item> GetAllItems() => _repo.GetAll<Item>();

    public Item? GetItemByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        using var conn = new SqlConnection(_connectionString);
        return conn.QueryFirstOrDefault<Item>(
            "SELECT * FROM ITEM WHERE Barcode = @barcode",
            new { barcode = barcode.Trim() });
    }
}
