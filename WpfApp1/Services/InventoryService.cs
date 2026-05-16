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

    public void MoveBatch(int batchId, string newStorageLocation)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Execute(
            "UPDATE BATCH SET StorageLocation = @loc WHERE Id = @id",
            new { loc = newStorageLocation, id = batchId });
    }

    public void CompareAndCorrect(ObservableCollection<InventoryItem> inventoryItems)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var row in inventoryItems)
            {
                int diff = row.ActualQuantity - row.AccountQuantity;
                if (diff == 0)
                    continue;

                var batches = conn.Query<Batch>(
                    "SELECT * FROM BATCH WHERE ItemId = @itemId ORDER BY ExpiryDate ASC",
                    new { row.ItemId }, transaction).ToList();

                if (diff > 0)
                {
                    var target = batches.FirstOrDefault() ?? throw new InvalidOperationException(
                        $"Нет партии для товара {row.ItemName}");
                    conn.Execute(
                        "UPDATE BATCH SET Quantity = Quantity + @diff WHERE Id = @id",
                        new { diff, id = target.Id }, transaction);
                }
                else
                {
                    int toWriteOff = -diff;
                    foreach (var batch in batches)
                    {
                        if (toWriteOff <= 0) break;
                        int take = Math.Min(batch.Quantity, toWriteOff);
                        conn.Execute(
                            "UPDATE BATCH SET Quantity = Quantity - @take WHERE Id = @id",
                            new { take, id = batch.Id }, transaction);
                        toWriteOff -= take;
                    }
                }
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
            SELECT b.* FROM BATCH b
            WHERE b.ExpiryDate < GETDATE() AND b.Quantity > 0").ToList();

        foreach (var batch in batches)
        {
            batch.ItemName = conn.QueryFirstOrDefault<string>(
                "SELECT Name FROM ITEM WHERE Id = @id", new { id = batch.ItemId });
        }

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
                    "UPDATE BATCH SET Quantity = 0 WHERE Id = @id",
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
