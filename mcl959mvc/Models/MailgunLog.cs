using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

[Table("MailgunLog")]
public partial class MailgunLog
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Date { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Subject { get; set; } = null!;

    [StringLength(5000)]
    [Unicode(false)]
    public string Message { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string Logger { get; set; } = null!;
}
