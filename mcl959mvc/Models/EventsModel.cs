using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

public partial class EventsModel
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? EventName { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    [Column(TypeName = "varchar(max)")]
    public string? EventDescription { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EventDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EventCreated { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? ImageFileName { get; set; }
}
