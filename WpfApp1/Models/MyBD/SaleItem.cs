using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("SALE_ITEM")]
public class SaleItem : EntityBase
{
    private int _id;
    private int _quantity;
    private decimal _priceAtSale;
    private int _saleId;
    private int? _uniqueItemId;
    private int _itemId;

    [Key, Column("ID")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("Quantity")]
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0)
                throw new ArgumentException("Quantity must be positive");
            _quantity = value;
        }
    }

    [Column("PriceAtSale")]
    public decimal PriceAtSale
    {
        get => _priceAtSale;
        set
        {
            if (value < 0)
                throw new ArgumentException("PriceAtSale cannot be negative");
            _priceAtSale = value;
        }
    }

    [Column("SaleId")]
    public int SaleId
    {
        get => _saleId;
        set => _saleId = value;
    }

    [Column("UniqueItemId")]
    public int? UniqueItemId
    {
        get => _uniqueItemId;
        set => _uniqueItemId = value;
    }

    [Column("ItemId")]
    public int ItemId
    {
        get => _itemId;
        set => _itemId = value;
    }

    public SaleItem() { }

    public SaleItem(int quantity, decimal priceAtSale, int saleId, int itemId)
    {
        Quantity = quantity;
        PriceAtSale = priceAtSale;
        SaleId = saleId;
        ItemId = itemId;
    }
}