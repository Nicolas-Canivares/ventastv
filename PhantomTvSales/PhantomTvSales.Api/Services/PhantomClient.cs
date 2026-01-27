using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace PhantomTvSales.Api.Services;

public sealed class PhantomClient
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly PhantomOptions _opt;

    public PhantomClient(HttpClient http, IMemoryCache cache, IOptions<PhantomOptions> opt)
    {
        _http = http;
        _cache = cache;
        _opt = opt.Value;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("PHANTOM_TOKEN", out string? token) && !string.IsNullOrWhiteSpace(token))
            return token!;

        var url =
            $"{_opt.BaseUrl}?action=autentificar&api_user={Uri.EscapeDataString(_opt.ApiUser)}&api_pass={Uri.EscapeDataString(_opt.ApiPass)}";

        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Phantom HTTP {(int)resp.StatusCode}. Body: {body[..Math.Min(200, body.Length)]}");

        var trimmed = body.Trim();

        // Caso A: Phantom devuelve JSON (según doc de autentificar) :contentReference[oaicite:3]{index=3}
        if (trimmed.StartsWith("{"))
        {
            using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("token", out var tokEl))
            {
                token = tokEl.GetString();
            }
            else
            {
                throw new InvalidOperationException("Phantom devolvió JSON pero sin propiedad 'token'.");
            }
        }
        else
        {
            // Caso B: Phantom devuelve token como texto plano (similar al flujo Botmaker) :contentReference[oaicite:4]{index=4}
            if (trimmed.Contains("Error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Autentificación incorrecta. Respuesta Phantom: {trimmed}");

            token = trimmed.Replace(" ", "");
        }

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"Token vacío. Respuesta Phantom: {trimmed}");

        _cache.Set("PHANTOM_TOKEN", token, TimeSpan.FromMinutes(14));
        return token!;
    }


    public async Task<JsonElement> GetAbonadosPorOltAllAsync(CancellationToken ct)
    {
        // Abonados_por_OLT con OLT=* :contentReference[oaicite:3]{index=3}
        var token = await GetTokenAsync(ct);
        var url =
            $"{_opt.BaseUrl}?token={Uri.EscapeDataString(token)}&action=Abonados_por_OLT&OLT=*";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public async Task<string?> GetMovilByIdaAsync(string ida, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);

        // Endpoint: action=Consulta_Cliente_Avanzada, inputs Token + IDA + JSON :contentReference[oaicite:3]{index=3}
        var url =
            $"{_opt.BaseUrl}?token={Uri.EscapeDataString(token)}&action=Consulta_Cliente_Avanzada&IDA={Uri.EscapeDataString(ida)}&JSON=1";

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        string? ExtractMovil(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object) return null;

            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, "Movil", StringComparison.OrdinalIgnoreCase))
                    return p.Value.ToString();
            }
            return null;
        }

        var root = doc.RootElement;

        // Puede venir objeto o array dependiendo del caso; contemplamos ambos.
        var movil = root.ValueKind switch
        {
            JsonValueKind.Object => ExtractMovil(root),
            JsonValueKind.Array when root.GetArrayLength() > 0 => ExtractMovil(root[0]),
            _ => null
        };

        movil = movil?.Trim();
        return string.IsNullOrWhiteSpace(movil) ? null : movil;
    }

}
