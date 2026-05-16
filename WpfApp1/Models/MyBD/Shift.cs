using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("SHIFT")]
public class Shift : EntityBase
{
    [Key, Column("Id")]
    public int Id { get; set; }

    [Column("StartTime")]
    public DateTime StartTime { get; set; }

    [Column("EndTime")]
    public DateTime? EndTime { get; set; }

    [Column("CashierId")]
    public int CashierId { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }
}
