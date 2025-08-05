using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

public partial class Message
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public required string? Name { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    [EmailAddress]
    public required string? Email { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Subject { get; set; } = null!;

    [Unicode(false)]
    public string Comments { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    public Guid? ResetToken { get; set; }
    // For code verification
    [NotMapped]
    public string Code { get; set; } = string.Empty;

    [NotMapped]
    public bool CodeSent { get; set; } = false;
}
