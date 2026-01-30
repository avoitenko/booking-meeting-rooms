using System.Security.Claims;

namespace BookingMeetingRooms.Infrastructure.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private const string UserIdHeader = "X-UserId";
    private const string RoleHeader = "X-Role";

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers[UserIdHeader].FirstOrDefault();
        var role = context.Request.Headers[RoleHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "Header");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}
