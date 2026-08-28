using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YouTube.ServerSide.Cacher.Services.Protection;

public class RequiredCookieAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var protectionService = context.HttpContext.RequestServices.GetRequiredService<IProtectionService>();
        if (protectionService.IsEnabled())
        {
            if (!context.HttpContext.Request.Cookies.TryGetValue("persistantKey", out var cookieValue)
                || string.IsNullOrWhiteSpace(cookieValue) || !protectionService.ValidateHashedKey(cookieValue))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
