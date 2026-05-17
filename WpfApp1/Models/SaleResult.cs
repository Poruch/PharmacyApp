namespace PharmacyApp.Models;

public class SaleResult
{
    public int SaleId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = "";
    public string CashierName { get; set; } = "";
    public List<SaleLineResult> Lines { get; set; } = new();
}

public class SaleLineResult
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal => Quantity * Price;
}
