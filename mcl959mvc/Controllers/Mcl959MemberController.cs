using mcl959mvc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace mcl959mvc.Controllers;

public abstract class Mcl959MemberController : Controller
{
    protected readonly UserManager<ApplicationUser> _userManager;
    protected string UserEmail;
    protected bool IsRegistered;
    protected bool IsMember;
    protected bool IsAdmin;

    public Mcl959MemberController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected async Task CheckUserIdentity()
    {
        IsRegistered = false;
        IsAdmin = false;
        IsMember = false;
        UserEmail = "";
        if (User.Identity?.IsAuthenticated == true)
        {
            IsRegistered = true;
            UserEmail = User.Identity.Name ?? string.Empty;
            var user = await _userManager.GetUserAsync(User);
            var claims = (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "isMember" || c.Type == "isRegistered" || c.Type == "isAdmin").ToList();
            if (claims != null)
            {
                IsRegistered = claims.Any(c => c.Type == "isRegistered" && c.Value == "true");
                IsMember = claims.Any(c => c.Type == "isMember" && c.Value == "true");
                IsAdmin = claims.Any(c => c.Type == "isAdmin" && c.Value == "true");
            }
        }
    }
}
