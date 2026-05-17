using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("UNIQUE_ITEM")]
public class UniqueItem : EntityBase
{
    private int _id;
    private string _qrCode = null!;
    private string _status = null!;
    private int _batchId;
    private Batch? _batch;

    [Key, Column("Id")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("QrCode"), MaxLength(255)]
    public string QrCode
    {
        get => _qrCode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("QR code cannot be empty");
            _qrCode = value;
        }
    }

    [Column("Status"), MaxLength(20)]
    public string Status
    {
        get => _status;
        set
        {
            var allowed = new[] { "in_stock", "sold", "returned", "written_off" };
            if (!allowed.Contains(value))
                throw new ArgumentException($"Status must be one of: {string.Join(", ", allowed)}");
            _status = value;
        }
    }

    [Column("BatchId")]
    public int BatchId
    {
        get => _batchId;
        set => _batchId = value;
    }

    [ForeignKey(nameof(BatchId))]
    public Batch? Batch
    {
        get => _batch;
        set => _batch = value;
    }

    public UniqueItem() { }

    public UniqueItem(string qrCode, string status, int batchId)
    {
        QrCode = qrCode;
        Status = status;
        BatchId = batchId;
    }
}