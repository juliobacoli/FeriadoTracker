using System.Net;
using FeriadoTracker.Web.E2E.Suporte;

namespace FeriadoTracker.Web.E2E.Api;

/// <summary>
/// Limites de uso da API (LimitesDeUso) contra o site real, sem navegador.
/// Cada teste sobe o próprio site com limites pequenos; o IP do visitante vai no X-Forwarded-For,
/// como o proxy do Railway faz.
/// </summary>
public class LimitesDeUsoTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> ChamarAsync(HttpClient http, HttpMethod metodo, string url, string ip)
    {
        using var req = new HttpRequestMessage(metodo, url);
        req.Headers.Add("X-Forwarded-For", ip);
        if (metodo != HttpMethod.Get) req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await http.SendAsync(req, Ct);
        return res.StatusCode;
    }

    private static Task<HttpStatusCode> VapidAsync(HttpClient http, string ip) =>
        ChamarAsync(http, HttpMethod.Get, "/api/push/vapid-public-key", ip);

    [Fact(DisplayName = "Limite: cada IP tem a sua cota no push, e o 429 informa o Retry-After")]
    public async Task CotaPorIp()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string> { ["RATE_LIMIT_PUSH"] = "3" });
        var http = site.CreateClient();

        for (var i = 0; i < 3; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, await VapidAsync(http, "203.0.113.10"));

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/push/vapid-public-key");
        req.Headers.Add("X-Forwarded-For", "203.0.113.10");
        using var bloqueada = await http.SendAsync(req, Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.InRange(bloqueada.Headers.RetryAfter?.Delta?.TotalSeconds ?? 0, 1, 60);

        // Outro visitante não é afetado.
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await VapidAsync(http, "203.0.113.11"));
    }

    [Fact(DisplayName = "Limite: IP falso inserido antes no X-Forwarded-For não escapa da cota")]
    public async Task IpFalsoNaoEscapa()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string> { ["RATE_LIMIT_PUSH"] = "2" });
        var http = site.CreateClient();
        await VapidAsync(http, "203.0.113.20");
        await VapidAsync(http, "203.0.113.20");

        // Só a última entrada (a que o proxy escreve) conta; o resto vem do visitante.
        Assert.Equal(HttpStatusCode.TooManyRequests, await VapidAsync(http, "198.51.100.99, 203.0.113.20"));
    }

    [Fact(DisplayName = "Limite: IPv6 do mesmo /64 divide a mesma cota")]
    public async Task Ipv6PorBloco()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string> { ["RATE_LIMIT_PUSH"] = "2" });
        var http = site.CreateClient();
        await VapidAsync(http, "2001:db8:1:2::a");
        await VapidAsync(http, "2001:db8:1:2::b");

        Assert.Equal(HttpStatusCode.TooManyRequests, await VapidAsync(http, "2001:db8:1:2::c"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await VapidAsync(http, "2001:db8:1:3::a"));
    }

    [Fact(DisplayName = "Limite: PUT de locais tem cota própria, separada do push")]
    public async Task LocaisTemCotaPropria()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string>
        {
            ["RATE_LIMIT_PUSH"] = "1",
            ["RATE_LIMIT_LOCAIS"] = "2",
        });
        var http = site.CreateClient();
        const string ip = "203.0.113.30";
        await VapidAsync(http, ip);
        Assert.Equal(HttpStatusCode.TooManyRequests, await VapidAsync(http, ip));

        // Push esgotado, locais ainda livre (400: sem token antiforgery, mas passou do limitador).
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Put, "/api/push/locais", ip));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Put, "/api/push/locais", ip));
        Assert.Equal(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Put, "/api/push/locais", ip));
    }

    [Fact(DisplayName = "Limite: avaliação tem cota por IP")]
    public async Task AvaliacaoPorIp()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string> { ["RATE_LIMIT_AVALIACAO"] = "2" });
        var http = site.CreateClient();

        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Post, "/api/avaliacoes", "203.0.113.40"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Post, "/api/avaliacoes", "203.0.113.40"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Post, "/api/avaliacoes", "203.0.113.40"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ChamarAsync(http, HttpMethod.Post, "/api/avaliacoes", "203.0.113.41"));
    }

    [Fact(DisplayName = "Limite: teto global da /api vale para todos os IPs somados")]
    public async Task TetoGlobal()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string>
        {
            ["RATE_LIMIT_API_POR_SEGUNDO"] = "5",
            ["RATE_LIMIT_PUSH"] = "1000",
        });
        var http = site.CreateClient();

        // 20 visitantes diferentes, uma chamada cada, em rajada.
        var status = await Task.WhenAll(Enumerable.Range(1, 20).Select(i => VapidAsync(http, $"198.51.100.{i}")));

        // Janela de 1 s: mesmo caindo em duas janelas, no máximo 10 passam.
        Assert.InRange(status.Count(s => s != HttpStatusCode.TooManyRequests), 1, 10);
        Assert.Contains(HttpStatusCode.TooManyRequests, status);
    }

    [Fact(DisplayName = "Limite: páginas ficam fora do teto da /api")]
    public async Task PaginasForaDoTeto()
    {
        await using var site = new SiteDeTeste(new Dictionary<string, string> { ["RATE_LIMIT_API_POR_SEGUNDO"] = "1" });
        var http = site.CreateClient();

        var status = await Task.WhenAll(Enumerable.Range(1, 10).Select(i => ChamarAsync(http, HttpMethod.Get, "/", $"198.51.100.{i}")));

        Assert.All(status, s => Assert.Equal(HttpStatusCode.OK, s));
    }
}
