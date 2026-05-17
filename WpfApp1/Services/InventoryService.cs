using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using PharmacyApp.ViewModels;
using System.Collections.ObjectModel;

namespace PharmacyApp.Services;

public class InventoryService : IInventoryService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public InventoryService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public void MoveBatch(int batchId, int storageLocationId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Execute(
            "UPDATE BATCH SET StorageLocationId = @locId WHERE [ID] = @id",
            new { locId = storageLocationId, id = batchId });
    }

    public List<InventoryItem> GetInventoryRows()
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<InventoryItem>(@"
            SELECT
                b.[ID] AS BatchId,
                b.BatchNumber,
                i.Name AS ItemName,
                b.ExpiryDate,
                b.StorageLocationId,
                CASE
                    WHEN sl.LocationId IS NULL THEN N'—'
                    WHEN sl.Cell IS NULL OR sl.Cell = '' THEN sl.Shelf
                    ELSE sl.Shelf + N', ' + sl.Cell
                END AS StorageLocationName,
                b.Quantity AS AccountQuantity,
                b.Quantity AS ActualQuantity
            FROM BATCH b
            INNER JOIN ITEM i ON i.Id = b.ItemId
            LEFT JOIN STORAGE_LOCATION sl ON sl.LocationId = b.StorageLocationId
            WHERE b.Quantity > 0
            ORDER BY i.Name, b.ExpiryDate, b.BatchNumber").ToList();
    }

    public List<InventoryChangeDto> GetInventoryChanges(ObservableCollection<InventoryItem> inventoryItems) =>
        inventoryItems
            .Where(i => i.ActualQuantity != i.AccountQuantity)
            .Select(i => new InventoryChangeDto
            {
                BatchNumber = i.BatchNumber,
                ItemName = i.ItemName,
                ExpiryDate = i.ExpiryDate,
                StorageLocationName = i.StorageLocationName,
                AccountQuantity = i.AccountQuantity,
                ActualQuantity = i.ActualQuantity
            })
            .ToList();

    public void CompareAndCorrect(ObservableCollection<InventoryItem> inventoryItems)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var row in inventoryItems)
            {
                if (row.ActualQuantity == row.AccountQuantity)
                    continue;

                int updated = conn.Execute(
                    "UPDATE BATCH SET Quantity = @qty WHERE [ID] = @batchId",
                    new { qty = row.ActualQuantity, batchId = row.BatchId },
                    transaction);

                if (updated == 0)
                    throw new InvalidOperationException(
                        $"Партия {row.BatchNumber} ({row.ItemName}) не найдена в базе.");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public ObservableCollection<Batch> GetExpiredBatches()
    {
        using var conn = new SqlConnection(_connectionString);
        var batches = conn.Query<Batch>(@"
            SELECT
                b.[ID] AS Id,
                b.BatchNumber,
                b.ProductionDate,
                b.ExpiryDate,
                b.PurchasePrice,
                b.RetailPrice,
                b.Quantity,
                b.StorageLocationId,
                b.ItemId,
                b.SupplierId,
                i.Name AS ItemName,
                CASE
                    WHEN sl.LocationId IS NULL THEN N'—'
                    WHEN sl.Cell IS NULL OR sl.Cell = '' THEN sl.Shelf
                    ELSE sl.Shelf + N', ' + sl.Cell
                END AS StorageLocationName
            FROM BATCH b
            INNER JOIN ITEM i ON i.Id = b.ItemId
            LEFT JOIN STORAGE_LOCATION sl ON sl.LocationId = b.StorageLocationId
            WHERE CAST(b.ExpiryDate AS DATE) < CAST(GETDATE() AS DATE)
              AND b.Quantity > 0
            ORDER BY b.ExpiryDate, i.Name").ToList();

        return new ObservableCollection<Batch>(batches);
    }

    public void CreateWriteOffDocument(ObservableCollection<Batch> expiredBatches)
    {
        if (expiredBatches.Count == 0)
            return;

        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            var doc = new Document(
                $"WO-{DateTime.Now:yyyyMMddHHmmss}",
                "writeoff",
                "posted",
                App.CurrentUser?.UserId ?? 1)
            {
                Reason = "Списание просроченных товаров",
                Details = string.Join(", ", expiredBatches.Select(b => $"{b.ItemName} x{b.Quantity}"))
            };
            _repo.Add(doc);

            foreach (var batch in expiredBatches)
            {
                conn.Execute(
                    "UPDATE BATCH SET Quantity = 0 WHERE [ID] = @id",
                    new { id = batch.Id }, transaction);
                conn.Execute(@"
                    UPDATE UNIQUE_ITEM SET Status = 'written_off'
                    WHERE BatchId = @batchId AND Status = 'in_stock'",
                    new { batchId = batch.Id }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
