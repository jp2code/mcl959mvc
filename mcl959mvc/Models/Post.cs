using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

public partial class Post
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Title { get; set; }

    public DateTime Posted { get; set; }

    public DateTime? Starts { get; set; }

    public DateTime? Ends { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Address { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Line1 { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Image1 { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Line2 { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Image2 { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Line3 { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Image3 { get; set; }

    public Guid? ResetToken { get; set; }
}
