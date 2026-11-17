using FeriadoTracker.Web.E2E.Suporte;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 1: pílulas de estados e cidades abaixo do lembrete (push.js, setupPilulas).</summary>
public class PilulasTests(AppFixture app) : E2ETest(app)
{
    private ILocator Toggle => Page.Locator("#locaisToggle");
    private static readonly Dictionary<string, int> Codigos = new() { ["PA"] = 15, ["PE"] = 26, ["ES"] = 32, ["SP"] = 35, ["SC"] = 42 };
    private ILocator Pilula(string sigla) => Page.Locator($"#locaisPilulas .pilula[data-codigo='{Codigos[sigla]}']");

    private async Task<ApiPushFalsa> AbrirInscritoAsync(string url = "/", string? locaisSincronizados = "[]")
    {
        await PushFalso.InstalarAsync(Page, inscrito: true, locaisSincronizados: locaisSincronizados);
        var api = await ApiPushFalsa.InstalarAsync(Page);
        await Page.GotoAsync(url);
        await Expect(Toggle).ToBeVisibleAsync();
        return api;
    }

    [Fact(DisplayName = "1.1 Cinco toques rápidos geram um único PUT com a lista completa")]
    public async Task CincoToquesUmPut()
    {
        var api = await AbrirInscritoAsync();
        await Toggle.ClickAsync();

        foreach (var rotulo in new[] { "ES", "PA", "PE", "SC", "SP" })
            await Pilula(rotulo).ClickAsync();

        await api.EsperarAsync(a => a.Puts.Count >= 1);
        await Task.Delay(1500, TestContext.Current.CancellationToken); // passa de novo dos 800 ms: nenhum envio atrasado pode aparecer

        var put = Assert.Single(api.Puts);
        Assert.Equal([15, 26, 32, 35, 42], ApiPushFalsa.Locais(put));
        Assert.Equal(PushFalso.Endpoint, put.Corpo!.Value.GetProperty("endpoint").GetString());
    }

    [Fact(DisplayName = "1.2 Fechar as pílulas antes de 800 ms envia o PUT antes do reload")]
    public async Task FecharAntesDoDebounceEnviaAntesDoReload()
    {
        var api = await AbrirInscritoAsync();
        var ordem = new List<string>();
        Page.Request += (_, r) =>
        {
            lock (ordem)
            {
                if (r.Method == "PUT" && r.Url.EndsWith("/api/push/locais")) ordem.Add("PUT");
                else if (r.IsNavigationRequest) ordem.Add("reload");
            }
        };

        await Toggle.ClickAsync();
        await Pilula("ES").ClickAsync();
        // A URL já é /?local=32 antes do reload (replaceState): espera a navegação em si.
        var reload = Page.WaitForRequestAsync(r => r.IsNavigationRequest);
        await Toggle.ClickAsync(); // fecha logo em seguida, bem antes dos 800 ms
        Assert.EndsWith("/?local=32", (await reload).Url);
        await Page.WaitForLoadStateAsync();
        await Expect(Toggle).ToBeVisibleAsync();

        Assert.Equal([32], ApiPushFalsa.Locais(Assert.Single(api.Puts)));
        lock (ordem) Assert.Equal(["PUT", "reload"], ordem.Take(2));
    }

    [Fact(DisplayName = "1.3 Cada toque marca na hora: pílula, URL, resumos e localStorage")]
    public async Task ToqueAtualizaNaHora()
    {
        await AbrirInscritoAsync();
        await Toggle.ClickAsync();

        await Pilula("PE").ClickAsync();

        await Expect(Pilula("PE")).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.EndsWith("/?local=26", Page.Url);
        await Expect(Toggle).ToContainTextAsync("Estados e cidades: PE");
        await Expect(Page.Locator("#locaisResumo")).ToHaveTextAsync("PE");
        Assert.Equal("[\"26\"]", await Page.EvaluateAsync<string>("() => localStorage.getItem('locais')"));

        await Pilula("SC").ClickAsync();

        await Expect(Toggle).ToContainTextAsync("Estados e cidades: PE, SC");
        await Expect(Page.Locator("#locaisResumo")).ToHaveTextAsync("Várias localidades");
        Assert.EndsWith("/?local=26&local=42", Page.Url);
    }

    [Fact(DisplayName = "1.4 Fechar as pílulas sem mudar nada não recarrega a página")]
    public async Task FecharSemMudancaNaoRecarrega()
    {
        await AbrirInscritoAsync();
        var navegacoes = 0;
        Page.Request += (_, r) => { if (r.IsNavigationRequest) Interlocked.Increment(ref navegacoes); };

        await Toggle.ClickAsync();
        await Toggle.ClickAsync();
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Equal(0, navegacoes);
        await Expect(Toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#locaisPilulas")).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "1.5 PUT com falha mostra aviso de erro")]
    public async Task PutComFalhaMostraErro()
    {
        var api = await AbrirInscritoAsync();
        api.Responder("/api/push/locais", 500);
        await Toggle.ClickAsync();

        await Pilula("ES").ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync("Não foi possível salvar. Tente novamente.");
    }

    [Fact(DisplayName = "1.6 Ao carregar com lista diferente da sincronizada, envia o PUT")]
    public async Task CarregarComListaNovaSincroniza()
    {
        var api = await AbrirInscritoAsync("/?local=35", locaisSincronizados: "[]");

        await api.EsperarAsync(a => a.Puts.Count == 1);

        Assert.Equal([35], ApiPushFalsa.Locais(api.Puts[0]));
        Assert.Equal("[35]", await Page.EvaluateAsync<string>("() => localStorage.getItem('locaisPush')"));
    }

    [Fact(DisplayName = "1.7 Ao carregar com a lista já sincronizada, nenhum PUT")]
    public async Task CarregarComListaSincronizadaNaoEnvia()
    {
        var api = await AbrirInscritoAsync("/?local=35", locaisSincronizados: "[35]");

        await Task.Delay(1500, TestContext.Current.CancellationToken);

        Assert.Empty(api.Puts);
    }

    [Fact(DisplayName = "1.8 Toques durante um PUT em andamento esperam ele terminar")]
    public async Task EnviosEmFila()
    {
        var api = await AbrirInscritoAsync();
        api.Atrasar("/api/push/locais", 2000);
        await Toggle.ClickAsync();

        await Pilula("ES").ClickAsync();
        await api.EsperarAsync(a => a.Puts.Count == 1);   // 1º PUT saiu e está preso no servidor
        await Pilula("PA").ClickAsync();                  // 800 ms depois quer enviar de novo

        await api.EsperarAsync(a => a.Puts.Count == 2, 8000);

        var (primeiro, segundo) = (api.Puts[0], api.Puts[1]);
        Assert.Equal([32], ApiPushFalsa.Locais(primeiro));
        Assert.Equal([15, 32], ApiPushFalsa.Locais(segundo));
        Assert.True(segundo.Inicio >= primeiro.Fim, $"2º PUT começou em {segundo.Inicio} ms, antes do 1º terminar ({primeiro.Fim} ms)");
    }

    [Fact(DisplayName = "1.10 PUT recusado por limite (429): avisa e tenta de novo sozinho após o Retry-After")]
    public async Task LimiteTentaDeNovo()
    {
        var api = await AbrirInscritoAsync();
        api.ResponderEmSequencia("/api/push/locais", 429);
        api.RetryAfterSegundos = 1;
        await Toggle.ClickAsync();

        await Pilula("PE").ClickAsync();

        await Expect(Page.Locator(".toast-info")).ToHaveTextAsync("Muitas alterações seguidas. Vamos salvar em instantes.");
        await api.EsperarAsync(a => a.Puts.Count == 2, 5000);
        Assert.Equal([26], ApiPushFalsa.Locais(api.Puts[1]));
        Assert.True(api.Puts[1].Inicio - api.Puts[0].Fim >= 900, "A nova tentativa saiu antes do Retry-After.");
        await Expect(Page.Locator(".toast-error")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => localStorage.getItem('locaisPush') === '[26]'");
    }
}
