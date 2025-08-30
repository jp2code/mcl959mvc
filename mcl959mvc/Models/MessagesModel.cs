using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

public partial class MessagesModel
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required(ErrorMessage = "A Name is required.")]
    [StringLength(50)]
    [Unicode(false)]
    public required string? Name { get; set; }

    [Required(ErrorMessage = "An Email address is required.")]
    [StringLength(50)]
    [Unicode(false)]
    [EmailAddress]
    public required string? Email { get; set; }
    [Required(ErrorMessage = "The SendTo field is required.")]
    public string SendTo { get; set; } = "";

    [StringLength(50)]
    [Unicode(false)]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "A message is required.")]
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
