using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("RETURN")]
public class Return : EntityBase
{
    private int _id;
    private DateTime _date;
    private string? _reason;
    private decimal _refundAmount;
    private int _originalSaleId;
    private int _employeeId;
    private Sale? _originalSale;
    private AppUser? _employee;
    private ICollection<ReturnItem>? _returnItems;

    [Key, Column("Id")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("Date")]
    public DateTime Date
    {
        get => _date;
        set => _date = value;
    }

    [Column("Reason")]
    public string? Reason
    {
        get => _reason;
        set => _reason = value;
    }

    [Column("RefundAmount")]
    public decimal RefundAmount
    {
        get => _refundAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("RefundAmount cannot be negative");
            _refundAmount = value;
        }
    }

    [Column("OriginalSaleId")]
    public int OriginalSaleId
    {
        get => _originalSaleId;
        set => _originalSaleId = value;
    }

    [Column("EmployeeId")]
    public int EmployeeId
    {
        get => _employeeId;
        set => _employeeId = value;
    }

    [ForeignKey(nameof(OriginalSaleId))]
    public Sale? OriginalSale
    {
        get => _originalSale;
        set => _originalSale = value;
    }

    [ForeignKey(nameof(EmployeeId))]
    public AppUser? Employee
    {
        get => _employee;
        set => _employee = value;
    }

    public ICollection<ReturnItem>? ReturnItems
    {
        get => _returnItems ??= new List<ReturnItem>();
        set => _returnItems = value;
    }

    public Return() { }

    public Return(decimal refundAmount, int originalSaleId, int employeeId, string? reason = null)
    {
        RefundAmount = refundAmount;
        OriginalSaleId = originalSaleId;
        EmployeeId = employeeId;
        Reason = reason;
        Date = DateTime.Now;
    }
}