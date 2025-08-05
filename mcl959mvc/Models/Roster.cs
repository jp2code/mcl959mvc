using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mcl959mvc.Models;

[Table("Roster")]
public partial class Roster
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Use this if a member does not want to go by their full legal name.
    /// </summary>
    [StringLength(200)]
    [Unicode(false)]
    public string? DisplayName { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string MemberNumber { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string? LifeMemberNumber { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? MemberStartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? MemberExpireDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LifeMemberDate { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? PersonalAddress { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PersonalPhone { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PersonalEmail { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? WorkAddress { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? WorkPhone { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? WorkEmail { get; set; }

    /// <summary>
    /// 0=Not Set; 1=Default is Personal Info; 2=Default is Work Info;
    /// </summary>
    public int DefaultInfo { get; set; }

    /// <summary>
    /// 0=Display Nothing on Website (Default); 1=Display Personal; 2=Display Work;
    /// </summary>
    public int WebsiteDisplay { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? TwoStepKey { get; set; }

    /// <summary>
    /// True if Member has completed TwoStepAuthentication
    /// </summary>
    public bool Authenticated { get; set; }

    /// <summary>
    /// Encrypted Password
    /// </summary>
    [StringLength(255)]
    [Unicode(false)]
    public string? HashPwd { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DiedOn { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? FirstName { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? MiddleName { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string? LastName { get; set; }
}
