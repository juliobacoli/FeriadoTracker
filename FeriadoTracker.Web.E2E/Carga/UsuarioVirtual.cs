using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace FeriadoTracker.Web.E2E.Carga;

/// <summary>
/// Visitante simulado: cliente HTTP com cookie próprio, token antiforgery da página
/// e um endpoint de push inscrito no servidor.
/// </summary>
public sealed partial class UsuarioVirtual : IDisposable
{
    private UsuarioVirtual(HttpClient http, string endpoint)
    {
        Http = http;
        Endpoint = endpoint;
    }

    public HttpClient Http { get; }
    public string Endpoint { get; }

    /// <summary>Abre a página (token + cookie) e inscreve um endpoint único, como o push.js faz.</summary>
    public static async Task<UsuarioVirtual> PrepararAsync(Uri baseUrl, int numero)
    {
        var http = new HttpClient(new SocketsHttpHandler { CookieContainer = new CookieContainer() }) { BaseAddress = baseUrl };
        try
        {
            var html = await http.GetStringAsync("/");
            var token = TokenRegex().Match(html) is { Success: true } m
                ? m.Groups[1].Value
                : throw new InvalidOperationException("Token antiforgery não encontrado na página.");
            http.DefaultRequestHeaders.Add("RequestVerificationToken", token);

            var endpoint = $"https://carga.test/{numero}-{Guid.NewGuid():N}";
            using var inscricao = await http.PostAsJsonAsync("/api/push/subscribe",
                new { endpoint, p256dh = "p256dh-carga", auth = "auth-carga" });
            inscricao.EnsureSuccessStatusCode();

            return new UsuarioVirtual(http, endpoint);
        }
        catch
        {
            http.Dispose();
            throw;
        }
    }

    public void Dispose() => Http.Dispose();

    [GeneratedRegex("""<meta name="request-verification-token" content="([^"]+)"\s*/?>""")]
    private static partial Regex TokenRegex();
}
