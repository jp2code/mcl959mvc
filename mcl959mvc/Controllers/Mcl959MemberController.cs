using mcl959mvc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace mcl959mvc.Controllers;

public abstract class Mcl959MemberController : Controller
{
    protected readonly UserManager<ApplicationUser> _userManager;
    protected bool IsRegistered;
    protected bool IsAdmin;

    public Mcl959MemberController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected async Task CheckUserIdentity()
    {
        IsRegistered = false;
        IsAdmin = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            IsRegistered = user?.IsRegistered ?? false;
            if (IsRegistered)
            {
                IsAdmin = user?.IsAdmin ?? false;
            }
        }
    }
}
