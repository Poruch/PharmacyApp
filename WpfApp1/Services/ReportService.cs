using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using System.IO;
using System.Text;

namespace PharmacyApp.Services;

public class ReportService : IReportService
{
    private readonly string _connectionString;
    private readonly EntityRepository _repo;

    public ReportService()
    {
        _connectionString = ConfigManager.ConnectionString;
        _repo = new EntityRepository(_connectionString);
    }

    public (decimal revenue, int receiptCount, decimal avgCheck) GetYesterdayStats()
    {
        var day = DateTime.Today.AddDays(-1);
        return GetStatsForDay(day);
    }

    private (decimal revenue, int receiptCount, decimal avgCheck) GetStatsForDay(DateTime day)
    {
        using var conn = new SqlConnection(_connectionString);
        var row = conn.QueryFirstOrDefault<dynamic>(@"
            SELECT
                ISNULL(SUM(TotalAmount), 0) AS Revenue,
                COUNT(*) AS ReceiptCount
            FROM SALE
            WHERE CAST(Date AS DATE) = @day",
            new { day = day.Date });

        int count = (int)(row?.ReceiptCount ?? 0);
        decimal revenue = (decimal)(row?.Revenue ?? 0m);
        decimal avg = count > 0 ? revenue / count : 0m;
        return (revenue, count, avg);
    }

    public List<CategoryRevenueDto> GetTopCategoriesByRevenue()
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<CategoryRevenueDto>(@"
            SELECT c.Name AS CategoryName, ISNULL(SUM(si.Quantity * si.PriceAtSale), 0) AS Revenue
            FROM SALE_ITEM si
            INNER JOIN ITEM i ON i.Id = si.ItemId
            INNER JOIN CATEGORY c ON c.Id = i.CategoryId
            GROUP BY c.Name
            ORDER BY Revenue DESC").AsList();
    }

    public List<CashierStatsDto> GetCashierStats()
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<CashierStatsDto>(@"
            SELECT
                u.LastName + ' ' + u.FirstName AS CashierName,
                COUNT(s.Id) AS ReceiptCount,
                ISNULL(SUM(s.TotalAmount), 0) AS TotalSales,
                CASE WHEN COUNT(s.Id) > 0 THEN ISNULL(SUM(s.TotalAmount), 0) / COUNT(s.Id) ELSE 0 END AS AverageCheck
            FROM SALE s
            INNER JOIN [USER] u ON u.UserId = s.CashierId
            GROUP BY u.LastName, u.FirstName
            ORDER BY TotalSales DESC").AsList();
    }

    public List<DeficitItemDto> GetDeficitItems()
    {
        using var conn = new SqlConnection(_connectionString);
        return conn.Query<DeficitItemDto>(@"
            SELECT
                i.Id AS ItemId,
                i.Name AS ItemName,
                ISNULL(SUM(b.Quantity), 0) AS CurrentStock,
                i.MinStock,
                CASE WHEN i.MinStock > ISNULL(SUM(b.Quantity), 0)
                    THEN i.MinStock - ISNULL(SUM(b.Quantity), 0) ELSE 0 END AS SuggestedOrderQty
            FROM ITEM i
            LEFT JOIN BATCH b ON b.ItemId = i.Id AND b.ExpiryDate >= GETDATE()
            GROUP BY i.Id, i.Name, i.MinStock
            HAVING ISNULL(SUM(b.Quantity), 0) < i.MinStock").AsList();
    }

    public string ExportDailyReportToExcel(DateTime date)
    {
        using var conn = new SqlConnection(_connectionString);
        var sales = conn.Query(@"
            SELECT Id, Date, TotalAmount, PaymentType, CashierId
            FROM SALE WHERE CAST(Date AS DATE) = @day",
            new { day = date.Date }).ToList();

        var writeoffs = conn.Query(@"
            SELECT DocNumber, Date, Reason, Details
            FROM DOCUMENT
            WHERE Type = 'writeoff' AND CAST(Date AS DATE) = @day",
            new { day = date.Date }).ToList();

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PharmacyReports");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"daily_report_{date:yyyyMMdd}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Продажи");
        sb.AppendLine("Id;Date;TotalAmount;PaymentType;CashierId");
        foreach (var s in sales)
            sb.AppendLine($"{s.Id};{s.Date};{s.TotalAmount};{s.PaymentType};{s.CashierId}");

        sb.AppendLine();
        sb.AppendLine("Списания");
        sb.AppendLine("DocNumber;Date;Reason;Details");
        foreach (var w in writeoffs)
            sb.AppendLine($"{w.DocNumber};{w.Date};{w.Reason};{w.Details}");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public void CreateDraftOrderForDeficitItems()
    {
        var deficit = GetDeficitItems().Where(d => d.SuggestedOrderQty > 0).ToList();
        if (deficit.Count == 0)
            return;

        var details = string.Join("; ", deficit.Select(d =>
            $"{d.ItemName}: заказать {d.SuggestedOrderQty} (остаток {d.CurrentStock}, мин. {d.MinStock})"));

        var doc = new Document(
            $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
            "receipt",
            "draft",
            App.CurrentUser?.UserId ?? 1)
        {
            Reason = "Черновик заказа по дефициту",
            Details = details
        };
        _repo.Add(doc);
    }
}
