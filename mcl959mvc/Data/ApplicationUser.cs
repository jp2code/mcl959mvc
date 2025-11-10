using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcl959mvc.Data;

public class ApplicationUser : IdentityUser
{
    [NotMapped]
    public bool IsMember { get; set; } = false;

    [NotMapped]
    public bool IsAdmin { get; set; } = false;

    public bool? Unsubscribe { get; set; }

    public bool GetEmailUpdates { get; internal set; }
}