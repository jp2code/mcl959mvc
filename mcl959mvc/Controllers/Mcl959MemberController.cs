using mcl959mvc.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace mcl959mvc.Controllers;

public abstract class Mcl959MemberController : Controller
{
    protected readonly UserManager<ApplicationUser> _userManager;
    protected readonly ILogger<Controller> _logger;
    public string UserEmail;
    public bool IsRegistered;
    public bool IsMember;
    public bool IsAdmin;

    public Mcl959MemberController(UserManager<ApplicationUser> userManager, ILogger<Controller> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // Exception filter implementation
    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception in {Controller} at {Path}",
            GetType().Name,
            context.HttpContext.Request.Path);

        // Optionally, show a user-friendly error page
        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/Error.cshtml"
        };
        context.ExceptionHandled = true;
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
            if (user == null)
            {
                return; // User not found, exit early
            }
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
