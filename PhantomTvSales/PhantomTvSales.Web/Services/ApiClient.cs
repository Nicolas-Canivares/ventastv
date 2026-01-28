using System.Net.Http.Headers;

namespace PhantomTvSales.Web.Services;

public class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly AuthState _authState;

    public ApiClient(IHttpClientFactory factory, AuthState authState)
    {
        _factory = factory;
        _authState = authState;
    }

    public HttpClient Create()
    {
        var client = _factory.CreateClient("Api");
        if (!string.IsNullOrWhiteSpace(_authState.Token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authState.Token);
        }

        return client;
    }
}
