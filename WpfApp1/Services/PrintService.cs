using Dapper;
using PharmacyApp.Interfaces;
using PharmacyApp.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PharmacyApp.Services;

public class PrintService : IPrintService
{
    public void ShowPreview(List<Item> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
            sb.AppendLine($"{item.Name} — {GetLatestRetailPrice(item.Id):C}");
        MessageBox.Show(sb.ToString(), "Предпросмотр ценников", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void PrintPriceTags(List<Item> items)
    {
        var doc = BuildFlowDocument(items);
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
            printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Ценники");
    }

    private static FlowDocument BuildFlowDocument(List<Item> items)
    {
        var doc = new FlowDocument { PagePadding = new Thickness(40) };
        foreach (var item in items)
        {
            var price = GetLatestRetailPrice(item.Id);
            doc.Blocks.Add(new Paragraph(new Run(item.Name)) { FontSize = 16, FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run($"{price:C}")) { FontSize = 20 });
            doc.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 0, 0, 12) });
        }
        return doc;
    }

    private static decimal GetLatestRetailPrice(int itemId)
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(ConfigManager.ConnectionString);
        return conn.QueryFirstOrDefault<decimal?>(@"
            SELECT TOP 1 RetailPrice FROM BATCH
            WHERE ItemId = @itemId AND Quantity > 0
            ORDER BY ExpiryDate ASC",
            new { itemId }) ?? 0m;
    }
}
