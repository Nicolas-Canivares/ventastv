using Microsoft.EntityFrameworkCore;
using PhantomTvSales.Api.Data;

namespace PhantomTvSales.Api.Services;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var token = ExtractToken(context);
        if (!string.IsNullOrWhiteSpace(token))
        {
            var session = await db.UserSessions
                .Where(s => s.Token == token)
                .OrderByDescending(s => s.ExpiresAt)
                .Select(s => new { s.Token, s.ExpiresAt, s.User })
                .FirstOrDefaultAsync();

            if (session is not null && session.ExpiresAt > DateTime.UtcNow)
            {
                context.Items["User"] = session.User;
            }
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(token)) return token;
        }

        var queryToken = context.Request.Query["access_token"].ToString();
        if (!string.IsNullOrWhiteSpace(queryToken)) return queryToken;

        return null;
    }
}
