namespace PhantomTvSales.Web.Services;

public class AuthState
{
    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public void Set(string token, string username, string role)
    {
        Token = token;
        Username = username;
        Role = role;
    }

    public void Clear()
    {
        Token = null;
        Username = null;
        Role = null;
    }
}
