using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class ReceivingService : IReceivingService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public ReceivingService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public Supplier? FindSupplierByInvoiceNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return null;

        using var conn = new SqlConnection(_connectionString);

        var supplier = conn.QueryFirstOrDefault<Supplier>(@"
            SELECT s.* FROM SUPPLIER s
            INNER JOIN DOCUMENT d ON d.Details = CAST(s.Id AS NVARCHAR(20))
            WHERE d.DocNumber = @inv AND d.Type = 'receipt'",
            new { inv = invoiceNumber.Trim() });

        if (supplier != null)
            return supplier;

        supplier = conn.QueryFirstOrDefault<Supplier>(
            "SELECT * FROM SUPPLIER WHERE Inn = @inv OR Name LIKE @pattern",
            new { inv = invoiceNumber.Trim(), pattern = $"%{invoiceNumber.Trim()}%" });

        return supplier ?? conn.QueryFirstOrDefault<Supplier>("SELECT TOP 1 * FROM SUPPLIER");
    }

    public void SaveBatch(Batch batch, List<string> scannedCodes)
    {
        batch.ProductionDate = batch.ProductionDate == default ? DateTime.Now : batch.ProductionDate;
        int batchId = _repo.Add(batch);

        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            foreach (var code in scannedCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                conn.Execute(@"
                    INSERT INTO UNIQUE_ITEM (QrCode, Status, BatchId)
                    VALUES (@code, 'in_stock', @batchId)",
                    new { code = code.Trim(), batchId },
                    transaction);
            }

            var doc = new Document(
                $"RCP-{DateTime.Now:yyyyMMddHHmmss}",
                "receipt",
                "posted",
                App.CurrentUser?.UserId ?? 1)
            {
                Details = batch.SupplierId.ToString(),
                Reason = batch.BatchNumber
            };
            _repo.Add(doc);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
