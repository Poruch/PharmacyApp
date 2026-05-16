namespace PharmacyApp.ViewModels;

public class ExpiredBatchViewModel
{
    public int BatchId { get; set; }
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal LossAmount { get; set; }
}
