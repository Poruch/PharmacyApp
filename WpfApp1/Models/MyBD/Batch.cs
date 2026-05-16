using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PharmacyApp.Models;

[Table("BATCH")]
public class Batch :EntityBase
{
    private int _id;
    private string _batchNumber = null!;
    private DateTime _productionDate;
    private DateTime? _expiryDate;
    private decimal _purchasePrice;
    private decimal _retailPrice;
    private int _quantity;
    private string? _storageLocation;
    private int _itemId;
    private int _supplierId;

    [Key, Column("ID")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("BatchNumber")]
    public string BatchNumber
    {
        get => _batchNumber;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Batch number cannot be empty");
            _batchNumber = value;
        }
    }

    [Column("ProductionDate")]
    public DateTime ProductionDate
    {
        get => _productionDate;
        set => _productionDate = value;
    }

    [Column("ExpiryDate")]
    public DateTime? ExpiryDate
    {
        get => _expiryDate;
        set => _expiryDate = value;
    }

    [Column("PurchasePrice")]
    public decimal PurchasePrice
    {
        get => _purchasePrice;
        set
        {
            if (value < 0)
                throw new ArgumentException("PurchasePrice cannot be negative");
            _purchasePrice = value;
        }
    }

    [Column("RetailPrice")]
    public decimal RetailPrice
    {
        get => _retailPrice;
        set
        {
            if (value < 0)
                throw new ArgumentException("RetailPrice cannot be negative");
            if (value < PurchasePrice)
                throw new ArgumentException("RetailPrice cannot be less than PurchasePrice");
            _retailPrice = value;
        }
    }

    [Column("Quantity")]
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 0)
                throw new ArgumentException("Quantity cannot be negative");
            _quantity = value;
        }
    }

    [Column("StorageLocation")]
    public string? StorageLocation
    {
        get => _storageLocation;
        set => _storageLocation = value;
    }

    [Column("ItemId")]
    public int ItemId
    {
        get => _itemId;
        set => _itemId = value;
    }

    [Column("SupplierId")]
    public int SupplierId
    {
        get => _supplierId;
        set => _supplierId = value;
    }

    public Batch() { }

    [NotMapped]
    public string SerialNumber => BatchNumber;

    [NotMapped]
    public string? ItemName { get; set; }

    [NotMapped]
    public decimal LossAmount => Quantity * PurchasePrice;

    public Batch(string batchNumber, decimal purchasePrice, decimal retailPrice, int quantity, int itemId, int supplierId)
    {
        BatchNumber = batchNumber;
        PurchasePrice = purchasePrice;
        RetailPrice = retailPrice;
        Quantity = quantity;
        ItemId = itemId;
        SupplierId = supplierId;
        ProductionDate = DateTime.Now;
    }
}