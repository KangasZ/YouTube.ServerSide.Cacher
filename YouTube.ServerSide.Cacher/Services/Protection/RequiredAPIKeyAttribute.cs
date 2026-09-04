using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YouTube.ServerSide.Cacher.Models;

namespace YouTube.ServerSide.Cacher.Services.Protection;

public class RequiredApiKeyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var protectionService = context.HttpContext.RequestServices.GetRequiredService<IProtectionService>();
        if (protectionService.IsEnabled())
        {
            var resource = context.HttpContext.Request.RouteValues["videoId"]?.ToString();
            if (resource is null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            var apikey = context.HttpContext.Request.Query["apikey"].ToString();
            // TODO: Update after youtube isnt the only supported
            if (string.IsNullOrWhiteSpace(apikey) || !protectionService.ValidateWatchKey(apikey, resource, SupportedSites.YouTube))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
