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
    public int RosterId { get; set; }
    [Column("DOB", TypeName = "datetime")]
    public DateTime? Dob { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }
    [NotMapped]
    public string? DescriptionHtml { get; set; }

    [Required]
    [Column("TimeStamp", TypeName = "datetime")]
    public DateTime TimeStamp { get; set; }
}