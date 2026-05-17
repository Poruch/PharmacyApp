using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("SALE")]
public class Sale : EntityBase
{
    private int _id;
    private DateTime _date;
    private decimal _totalAmount;
    private string _paymentType = null!;
    private int _cashierId;
    private AppUser? _cashier;
    private ICollection<SaleItem>? _saleItems;
    private ICollection<Return>? _returns;

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

    [Column("TotalAmount")]
    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (value < 0)
                throw new ArgumentException("TotalAmount cannot be negative");
            _totalAmount = value;
        }
    }

    [Column("PaymentType"), MaxLength(20)]
    public string PaymentType
    {
        get => _paymentType;
        set
        {
            if (value != "cash" && value != "card")
                throw new ArgumentException("PaymentType must be 'cash' or 'card'");
            _paymentType = value;
        }
    }

    [Column("CashierId")]
    public int CashierId
    {
        get => _cashierId;
        set => _cashierId = value;
    }

    [ForeignKey(nameof(CashierId))]
    public AppUser? Cashier
    {
        get => _cashier;
        set => _cashier = value;
    }

    public ICollection<SaleItem>? SaleItems
    {
        get => _saleItems ??= new List<SaleItem>();
        set => _saleItems = value;
    }

    public ICollection<Return>? Returns
    {
        get => _returns ??= new List<Return>();
        set => _returns = value;
    }

    public Sale() { }

    public Sale(decimal totalAmount, string paymentType, int cashierId)
    {
        TotalAmount = totalAmount;
        PaymentType = paymentType;
        CashierId = cashierId;
        Date = DateTime.Now;
    }
}