using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Models;

[Table("Memorial")]
public partial class MemorialModel
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Unicode(false)]
    public string MemberNumber { get; set; } = string.Empty;

    [Column("DOB", TypeName = "datetime")]
    public DateTime? Dob { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Required]
    [Column("TimeStamp", TypeName = "datetime")]
    public DateTime TimeStamp { get; set; }
}