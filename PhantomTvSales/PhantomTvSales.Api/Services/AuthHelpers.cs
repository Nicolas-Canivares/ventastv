using PhantomTvSales.Api.Models;

namespace PhantomTvSales.Api.Services;

public static class AuthHelpers
{
    public static User? GetCurrentUser(this HttpContext context)
    {
        return context.Items.TryGetValue("User", out var user) ? user as User : null;
    }

    public static bool IsAdmin(this HttpContext context)
    {
        return context.GetCurrentUser()?.Role == UserRole.Admin;
    }
}
