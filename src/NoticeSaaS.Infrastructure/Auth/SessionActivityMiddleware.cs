using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NoticeSaaS.Application.Auth;

namespace NoticeSaaS.Infrastructure.Auth;

public sealed class SessionActivityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionClaim = context.User.FindFirstValue("session_id");
            if (Guid.TryParse(sessionClaim, out var sessionId))
            {
                var ok = await authService.TouchSessionAsync(sessionId, context.RequestAborted);
                if (!ok)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        code = "SESSION_EXPIRED",
                        message = "Your session has expired or was revoked."
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}
