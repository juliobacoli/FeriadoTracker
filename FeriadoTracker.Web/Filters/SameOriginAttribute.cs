using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FeriadoTracker.Web.Filters;

public class SameOriginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var host = request.Host.Value;

        if (TryGetAuthority(request.Headers["Origin"], out var origin))
        {
            if (!string.Equals(origin, host, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
            return;
        }

        if (TryGetAuthority(request.Headers["Referer"], out var referer))
        {
            if (!string.Equals(referer, host, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
            return;
        }

        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }

    private static bool TryGetAuthority(string? headerValue, out string authority)
    {
        authority = string.Empty;
        if (string.IsNullOrWhiteSpace(headerValue)) return false;

        if (!Uri.TryCreate(headerValue, UriKind.Absolute, out var uri)) return false;

        authority = uri.Authority;
        return true;
    }
}
