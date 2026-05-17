using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("RETURN_ITEM")]
public class ReturnItem : EntityBase
{
    private int _id;
    private int _quantity;
    private int _returnId;
    private int? _uniqueItemId;
    private int _itemId;
    private Return? _return;
    private UniqueItem? _uniqueItem;
    private Item? _item;

    [Key, Column("Id")]
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

    [Column("ReturnId")]
    public int ReturnId
    {
        get => _returnId;
        set => _returnId = value;
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

    [ForeignKey(nameof(ReturnId))]
    public Return? Return
    {
        get => _return;
        set => _return = value;
    }

    [ForeignKey(nameof(UniqueItemId))]
    public UniqueItem? UniqueItem
    {
        get => _uniqueItem;
        set => _uniqueItem = value;
    }

    [ForeignKey(nameof(ItemId))]
    public Item? Item
    {
        get => _item;
        set => _item = value;
    }

    public ReturnItem() { }

    public ReturnItem(int quantity, int returnId, int itemId, int? uniqueItemId = null)
    {
        Quantity = quantity;
        ReturnId = returnId;
        ItemId = itemId;
        UniqueItemId = uniqueItemId;
    }
}