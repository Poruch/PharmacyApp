using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("ITEM")]
public class Item : EntityBase
{
    private int _id;
    private string _name = null!;
    private string? _inn;
    private string? _dosage;
    private string? _form;
    private bool _prescriptionRequired;
    private string? _barcode;
    private int _minStock;
    private bool _isVital;
    private int? _tempMin;
    private int? _tempMax;
    private int _quantityPerPack;
    private string _unit = null!;
    private int _categoryId;
    private Category? _category;
    private ICollection<Batch>? _batches;

    [Key, Column("Id")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("Name"), MaxLength(255)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be empty");
            _name = value;
        }
    }

    [Column("INN"), MaxLength(255)]
    public string? Inn
    {
        get => _inn;
        set => _inn = value;
    }

    [Column("Dosage"), MaxLength(50)]
    public string? Dosage
    {
        get => _dosage;
        set => _dosage = value;
    }

    [Column("Form"), MaxLength(50)]
    public string? Form
    {
        get => _form;
        set => _form = value;
    }

    [Column("PrescriptionRequired")]
    public bool PrescriptionRequired
    {
        get => _prescriptionRequired;
        set => _prescriptionRequired = value;
    }

    [Column("Barcode"), MaxLength(255)]
    public string? Barcode
    {
        get => _barcode;
        set => _barcode = value;
    }

    [Column("MinStock")]
    public int MinStock
    {
        get => _minStock;
        set
        {
            if (value < 0)
                throw new ArgumentException("MinStock cannot be negative");
            _minStock = value;
        }
    }

    [Column("IsVital")]
    public bool IsVital
    {
        get => _isVital;
        set => _isVital = value;
    }

    [Column("TempMin")]
    public int? TempMin
    {
        get => _tempMin;
        set => _tempMin = value;
    }

    [Column("TempMax")]
    public int? TempMax
    {
        get => _tempMax;
        set => _tempMax = value;
    }

    [Column("QuantityPerPack")]
    public int QuantityPerPack
    {
        get => _quantityPerPack;
        set
        {
            if (value <= 0)
                throw new ArgumentException("QuantityPerPack must be positive");
            _quantityPerPack = value;
        }
    }

    [Column("Unit"), MaxLength(20)]
    public string Unit
    {
        get => _unit;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Unit cannot be empty");
            _unit = value;
        }
    }

    [Column("CategoryId")]
    public int CategoryId
    {
        get => _categoryId;
        set => _categoryId = value;
    }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category
    {
        get => _category;
        set => _category = value;
    }

    public ICollection<Batch>? Batches
    {
        get => _batches ??= new List<Batch>();
        set => _batches = value;
    }

    public Item() { }

    public Item(string name, string unit, int quantityPerPack, int categoryId)
    {
        Name = name;
        Unit = unit;
        QuantityPerPack = quantityPerPack;
        CategoryId = categoryId;
    }
}