using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace PhantomTvSales.Web.Services;

public class AuthState
{
    private const string StorageKey = "auth";
    private readonly ProtectedLocalStorage _storage;
    private bool _initialized;

    public AuthState(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public event Action? OnChange;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var result = await _storage.GetAsync<AuthPayload>(StorageKey);
        if (result.Success && result.Value is not null)
        {
            Token = result.Value.Token;
            Username = result.Value.Username;
            Role = result.Value.Role;
        }

        _initialized = true;
        NotifyStateChanged();
    }

    public async Task SetAsync(string token, string username, string role)
    {
        Token = token;
        Username = username;
        Role = role;
        await _storage.SetAsync(StorageKey, new AuthPayload(token, username, role));
        NotifyStateChanged();
    }

    public async Task ClearAsync()
    {
        Token = null;
        Username = null;
        Role = null;
        await _storage.DeleteAsync(StorageKey);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private sealed record AuthPayload(string Token, string Username, string Role);
}
