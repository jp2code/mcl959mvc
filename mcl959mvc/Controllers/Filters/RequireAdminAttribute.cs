using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace mcl959mvc.Controllers.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Mcl959MemberController baseCtl)
        {
            if (!baseCtl.IsAdmin)
            {
                if (Mcl959MemberController.IsAjaxRequest(context.HttpContext.Request))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.Result = new PartialViewResult { ViewName = "_AccessDeniedPartial" };
                }
                else
                {
                    context.Result = new ForbidResult();
                }
                return; // stop pipeline
            }
        }
        base.OnActionExecuting(context);
    }
}