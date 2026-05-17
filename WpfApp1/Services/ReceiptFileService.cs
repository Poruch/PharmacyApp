using PharmacyApp.Models;
using System.Globalization;
using System.IO;
using System.Text;

namespace PharmacyApp.Services;

public static class ReceiptFileService
{
    private static string ReceiptsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PharmacyReceipts");

    private static string ShiftsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PharmacyShifts");

    public static string SaveSaleReceipt(SaleResult sale)
    {
        Directory.CreateDirectory(ReceiptsFolder);
        string path = Path.Combine(ReceiptsFolder,
            $"receipt_{sale.SaleId}_{sale.Date:yyyyMMdd_HHmmss}.txt");

        var sb = new StringBuilder();
        sb.AppendLine("══════════════════════════════════");
        sb.AppendLine("           ЧЕК ПРОДАЖИ");
        sb.AppendLine("══════════════════════════════════");
        sb.AppendLine($"Чек №:      {sale.SaleId}");
        sb.AppendLine($"Дата:       {sale.Date:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"Кассир:     {sale.CashierName}");
        sb.AppendLine($"Оплата:     {(sale.PaymentType == "cash" ? "Наличные" : "Карта")}");
        sb.AppendLine("──────────────────────────────────");
        sb.AppendLine("Товар                    Кол  Сумма");
        sb.AppendLine("──────────────────────────────────");

        foreach (var line in sale.Lines)
        {
            string name = line.ItemName.Length > 22 ? line.ItemName[..22] : line.ItemName.PadRight(22);
            sb.AppendLine($"{name} {line.Quantity,3}  {line.LineTotal,8:F2}");
        }

        sb.AppendLine("──────────────────────────────────");
        sb.AppendLine($"ИТОГО:                    {sale.TotalAmount,8:F2} ₽");
        sb.AppendLine("══════════════════════════════════");
        sb.AppendLine("       Спасибо за покупку!");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public static string SaveShiftReport(ShiftInfo info, string cashierName, IReadOnlyList<ShiftSaleEntry> sales)
    {
        Directory.CreateDirectory(ShiftsFolder);
        string endTime = (info.EndTime ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
        string safeName = SanitizeFileName(cashierName);
        string path = Path.Combine(ShiftsFolder, $"shift_{safeName}_{endTime}.txt");

        var sb = new StringBuilder();
        sb.AppendLine("══════════════════════════════════");
        sb.AppendLine("         ОТЧЁТ ПО СМЕНЕ");
        sb.AppendLine("══════════════════════════════════");
        sb.AppendLine($"Кассир:           {cashierName}");
        sb.AppendLine($"Начало смены:     {info.StartTime:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"Окончание смены:  {info.EndTime:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine("──────────────────────────────────");
        sb.AppendLine($"Количество чеков: {info.SalesCount}");
        sb.AppendLine($"Наличные:         {info.TotalCash.ToString("F2", CultureInfo.InvariantCulture)} ₽");
        sb.AppendLine($"Карта:            {info.TotalCard.ToString("F2", CultureInfo.InvariantCulture)} ₽");
        sb.AppendLine($"Итого выручка:    {info.Total.ToString("F2", CultureInfo.InvariantCulture)} ₽");
        sb.AppendLine("──────────────────────────────────");
        sb.AppendLine("Список продаж:");

        if (sales.Count == 0)
        {
            sb.AppendLine("  (продаж не было)");
        }
        else
        {
            foreach (var sale in sales)
            {
                string payment = sale.PaymentType == "cash" ? "нал" : "карта";
                sb.AppendLine($"  Чек {sale.SaleId} | {sale.Date:HH:mm:ss} | {sale.TotalAmount:F2} ₽ | {payment}");
            }
        }

        sb.AppendLine("══════════════════════════════════");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "cashier" : name.Replace(' ', '_');
    }
}

public class ShiftSaleEntry
{
    public int SaleId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = "";
}
