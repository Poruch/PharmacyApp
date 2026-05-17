namespace PharmacyApp.Models;

public class ShiftInfo
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal Total => TotalCash + TotalCard;
}
