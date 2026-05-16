using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("SUPPLIER")]
public class Supplier : EntityBase
{
    private int _id;
    private string _name = null!;
    private string? _inn;
    private string? _phone;
    private string? _email;
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
                throw new ArgumentException("Supplier name cannot be empty");
            _name = value;
        }
    }

    [Column("INN"), MaxLength(12)]
    public string? Inn
    {
        get => _inn;
        set => _inn = value;
    }

    [Column("Phone"), MaxLength(20)]
    public string? Phone
    {
        get => _phone;
        set => _phone = value;
    }

    [Column("Email"), MaxLength(255)]
    public string? Email
    {
        get => _email;
        set => _email = value;
    }

    public ICollection<Batch>? Batches
    {
        get => _batches ??= new List<Batch>();
        set => _batches = value;
    }

    public Supplier() { }

    public Supplier(string name)
    {
        Name = name;
    }
}