using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BookingMeetingRooms.Infrastructure.Data;
using BookingMeetingRooms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingMeetingRooms.Infrastructure.Middleware;

public class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string UserIdHeader = "X-UserId";
    private const string RoleHeader = "X-Role";
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase) { "Employee", "Admin" };

    public HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserIdHeader].FirstOrDefault();
        
        // Перевірка наявності X-UserId
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AuthenticateResult.NoResult();
        }

        // Перевірка формату X-UserId (має бути числом)
        if (!int.TryParse(userId, out var parsedUserId) || parsedUserId <= 0)
        {
            return AuthenticateResult.Fail("Invalid X-UserId format. Must be a positive integer.");
        }

        // Перевірка існування користувача в базі даних
        var dbContext = Context.RequestServices.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users.FindAsync(new object[] { parsedUserId });
        
        if (user == null)
        {
            return AuthenticateResult.Fail($"User with id {parsedUserId} not found in database.");
        }

        // Перевірка наявності X-Role (обов'язковий заголовок)
        var roleHeader = Request.Headers[RoleHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(roleHeader))
        {
            return AuthenticateResult.Fail("X-Role header is required.");
        }

        // Перевірка валідності ролі
        if (!ValidRoles.Contains(roleHeader))
        {
            return AuthenticateResult.Fail($"Invalid X-Role value. Must be one of: {string.Join(", ", ValidRoles)}");
        }
        
        // Перевірка відповідності ролі з бази даних
        var expectedRole = user.Role.ToString();
        if (!string.Equals(roleHeader, expectedRole, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail($"Role mismatch. User has role '{expectedRole}' but provided '{roleHeader}'.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, expectedRole)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
