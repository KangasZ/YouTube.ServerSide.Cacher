using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace YouTube.ServerSide.Cacher.Services.Protection;

public class RequiredApiKeyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var protectionService = context.HttpContext.RequestServices.GetRequiredService<IProtectionService>();
        if (protectionService.IsEnabled())
        {
            var apikey = context.HttpContext.Request.Query["apikey"].ToString();
            if (string.IsNullOrWhiteSpace(apikey) || !protectionService.ValidateApiKey(apikey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
