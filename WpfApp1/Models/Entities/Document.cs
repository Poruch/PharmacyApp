using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("DOCUMENT")]
public class Document : EntityBase
{
    private int _id;
    private string _docNumber = null!;
    private string _type = null!;
    private DateTime _date;
    private string _status = null!;
    private string? _reason;
    private string? _details;
    private DateTime _createdAt;
    private DateTime? _approvedAt;
    private int _createdBy;
    private int? _approvedBy;
    private AppUser? _creator;
    private AppUser? _approver;

    [Key, Column("Id")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("DocNumber"), MaxLength(50)]
    public string DocNumber
    {
        get => _docNumber;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Document number cannot be empty");
            _docNumber = value;
        }
    }

    [Column("Type"), MaxLength(30)]
    public string Type
    {
        get => _type;
        set
        {
            var allowed = new[] { "writeoff", "receipt", "inventory", "pricechange", "order" };
            if (!allowed.Contains(value))
                throw new ArgumentException($"Type must be one of: {string.Join(", ", allowed)}");
            _type = value;
        }
    }

    [Column("Date")]
    public DateTime Date
    {
        get => _date;
        set => _date = value;
    }

    [Column("Status"), MaxLength(20)]
    public string Status
    {
        get => _status;
        set
        {
            var allowed = new[] { "draft", "approved", "posted", "cancelled" };
            if (!allowed.Contains(value))
                throw new ArgumentException($"Status must be one of: {string.Join(", ", allowed)}");
            _status = value;
        }
    }

    [Column("Reason")]
    public string? Reason
    {
        get => _reason;
        set => _reason = value;
    }

    [Column("Details")]
    public string? Details
    {
        get => _details;
        set => _details = value;
    }

    [Column("CreatedAt")]
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }

    [Column("ApprovedAt")]
    public DateTime? ApprovedAt
    {
        get => _approvedAt;
        set => _approvedAt = value;
    }

    [Column("CreatedBy")]
    public int CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value;
    }

    [Column("ApprovedBy")]
    public int? ApprovedBy
    {
        get => _approvedBy;
        set => _approvedBy = value;
    }

    [ForeignKey(nameof(CreatedBy))]
    public AppUser? Creator
    {
        get => _creator;
        set => _creator = value;
    }

    [ForeignKey(nameof(ApprovedBy))]
    public AppUser? Approver
    {
        get => _approver;
        set => _approver = value;
    }

    public Document() { }

    public Document(string docNumber, string type, string status, int createdBy)
    {
        DocNumber = docNumber;
        Type = type;
        Status = status;
        CreatedBy = createdBy;
        Date = DateTime.Now;
        CreatedAt = DateTime.Now;
    }
}