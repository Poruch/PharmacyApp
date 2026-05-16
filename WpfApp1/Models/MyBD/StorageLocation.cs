using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("STORAGE_LOCATION")]
public class StorageLocation : EntityBase
{
    private int _locationId;
    private string _shelf = null!;
    private string? _cell;
    private ICollection<Batch>? _batches;

    [Key, Column("LocationId")]
    public int LocationId
    {
        get => _locationId;
        set => _locationId = value;
    }

    [Column("Shelf"), MaxLength(100)]
    public string Shelf
    {
        get => _shelf;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Shelf cannot be empty");
            _shelf = value;
        }
    }

    [Column("Cell"), MaxLength(50)]
    public string? Cell
    {
        get => _cell;
        set => _cell = value;
    }

    public ICollection<Batch>? Batches
    {
        get => _batches ??= new List<Batch>();
        set => _batches = value;
    }

    public StorageLocation() { }

    public StorageLocation(string shelf, string? cell = null)
    {
        Shelf = shelf;
        Cell = cell;
    }
}