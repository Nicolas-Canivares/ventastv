using System.Net.Http.Headers;

namespace PhantomTvSales.Web.Services;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly AuthState _authState;

    public ApiAuthHandler(AuthState authState)
    {
        _authState = authState;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_authState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
