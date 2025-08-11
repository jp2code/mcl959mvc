using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Models;

[Table("Comments")]
public partial class CommentsModel
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Unicode(false)]
    public string TableSource { get; set; } = string.Empty;

    [Required]
    public int ParentId { get; set; }

    [Required]
    [StringLength(50)]
    [Unicode(false)]
    public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Message { get; set; }

    [Required]
    [Column("TimeStamp", TypeName = "datetime")]
    public DateTime TimeStamp { get; set; }
}