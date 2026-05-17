using Dapper;
using Microsoft.Data.SqlClient;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using System.Globalization;
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

        var sales = conn.Query<DailySaleRow>(@"
            SELECT
                s.Id,
                s.Date,
                s.TotalAmount,
                s.PaymentType,
                s.CashierId,
                u.LastName + ' ' + u.FirstName AS CashierName
            FROM SALE s
            LEFT JOIN [USER] u ON u.UserId = s.CashierId
            WHERE CAST(s.Date AS DATE) = @day
            ORDER BY s.Date",
            new { day = date.Date }).AsList();

        var saleLines = conn.Query<DailySaleLineRow>(@"
            SELECT
                si.SaleId,
                i.Name AS ItemName,
                si.Quantity,
                si.PriceAtSale,
                si.Quantity * si.PriceAtSale AS LineTotal
            FROM SALE_ITEM si
            INNER JOIN SALE s ON s.Id = si.SaleId
            INNER JOIN ITEM i ON i.Id = si.ItemId
            WHERE CAST(s.Date AS DATE) = @day
            ORDER BY si.SaleId, i.Name",
            new { day = date.Date }).AsList();

        var writeoffs = conn.Query<DailyWriteoffRow>(@"
            SELECT DocNumber, Date, Reason, Details
            FROM DOCUMENT
            WHERE Type = 'writeoff' AND CAST(Date AS DATE) = @day
            ORDER BY Date",
            new { day = date.Date }).AsList();

        var (revenue, receiptCount, avgCheck) = GetStatsForDay(date);

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PharmacyReports");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"daily_report_{date:yyyyMMdd}.csv");

        var sb = new StringBuilder();
        sb.AppendLine($"Ежедневный отчёт;{date:dd.MM.yyyy}");
        sb.AppendLine();
        sb.AppendLine("Сводка");
        sb.AppendLine("Показатель;Значение");
        sb.AppendLine($"Выручка;{revenue.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Количество чеков;{receiptCount}");
        sb.AppendLine($"Средний чек;{avgCheck.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine();

        sb.AppendLine("Продажи (чеки)");
        sb.AppendLine("Id;Дата;Сумма;Оплата;Кассир");
        foreach (var s in sales)
            sb.AppendLine($"{s.Id};{s.Date:dd.MM.yyyy HH:mm};{s.TotalAmount:F2};{s.PaymentType};{EscapeCsv(s.CashierName)}");

        sb.AppendLine();
        sb.AppendLine("Позиции в чеках");
        sb.AppendLine("SaleId;Товар;Кол-во;Цена;Сумма");
        foreach (var line in saleLines)
            sb.AppendLine($"{line.SaleId};{EscapeCsv(line.ItemName)};{line.Quantity};{line.PriceAtSale:F2};{line.LineTotal:F2}");

        sb.AppendLine();
        sb.AppendLine("Списания");
        sb.AppendLine("Номер;Дата;Причина;Детали");
        foreach (var w in writeoffs)
            sb.AppendLine($"{EscapeCsv(w.DocNumber)};{w.Date:dd.MM.yyyy HH:mm};{EscapeCsv(w.Reason)};{EscapeCsv(w.Details)}");

        // UTF-8 BOM — Excel корректно открывает кириллицу
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    public int? CreateDraftOrderForDeficitItems()
    {
        var deficit = GetDeficitItems().Where(d => d.SuggestedOrderQty > 0).ToList();
        if (deficit.Count == 0)
            return null;

        int createdBy = AuthenticationService.CurrentUser?.UserId
            ?? App.CurrentUser?.UserId
            ?? 0;
        if (createdBy == 0)
            throw new InvalidOperationException("Не удалось определить пользователя для создания черновика. Войдите в систему.");

        var lines = deficit.Select(d =>
            $"{d.ItemId}|{d.ItemName}|{d.SuggestedOrderQty}|{d.CurrentStock}|{d.MinStock}");
        var details = string.Join(Environment.NewLine, lines);

        var doc = new Document(
            $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
            "order",
            "draft",
            createdBy)
        {
            Reason = "Черновик заказа поставщику по дефициту",
            Details = details
        };
        return _repo.Add(doc);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private sealed class DailySaleRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; } = "";
        public int CashierId { get; set; }
        public string? CashierName { get; set; }
    }

    private sealed class DailySaleLineRow
    {
        public int SaleId { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal PriceAtSale { get; set; }
        public decimal LineTotal { get; set; }
    }

    private sealed class DailyWriteoffRow
    {
        public string DocNumber { get; set; } = "";
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
        public string? Details { get; set; }
    }
}
