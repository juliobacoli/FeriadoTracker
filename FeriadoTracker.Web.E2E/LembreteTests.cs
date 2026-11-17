using FeriadoTracker.Web.E2E.Suporte;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 2: botão "Me avisar dos feriados" (push.js, setupPushButtons).</summary>
public class LembreteTests(AppFixture app) : E2ETest(app)
{
    private ILocator Botao => Page.Locator("#btnLembrete");
    private ILocator Locais => Page.Locator("#locaisPush");

    private async Task<ApiPushFalsa> AbrirAsync(bool inscrito, string respostaPermissao = "granted", string? permissao = null)
    {
        await PushFalso.InstalarAsync(Page, inscrito, permissao: permissao ?? (inscrito ? "granted" : "default"), respostaPermissao: respostaPermissao);
        var api = await ApiPushFalsa.InstalarAsync(Page);
        await Page.GotoAsync("/");
        await Expect(Botao).ToHaveAttributeAsync("aria-pressed", inscrito ? "true" : "false");
        return api;
    }

    [Fact(DisplayName = "2.1 Sem inscrição: botão de ativar e pílulas ocultas")]
    public async Task SemInscricao()
    {
        await AbrirAsync(inscrito: false);

        await Expect(Botao).ToHaveTextAsync("Me avisar dos feriados");
        await Expect(Botao).ToHaveAttributeAsync("title", "Ativar lembretes");
        await Expect(Locais).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "2.2 Ativar: inscreve, envia os locais, avisa e mostra as pílulas")]
    public async Task AtivarComSucesso()
    {
        var api = await AbrirAsync(inscrito: false);

        await Botao.ClickAsync();

        await Expect(Page.Locator(".toast-success")).ToContainTextAsync("Lembretes ativados.");
        await Expect(Botao).ToHaveTextAsync("Lembretes ativos");
        await Expect(Botao).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Locais).ToBeVisibleAsync();

        var caminhos = api.Chamadas.Select(c => $"{c.Metodo} {c.Caminho}").ToList();
        Assert.Equal(["GET /api/push/vapid-public-key", "POST /api/push/subscribe", "PUT /api/push/locais"], caminhos);

        var corpo = api.Chamadas[1].Corpo!.Value;
        Assert.Equal(PushFalso.Endpoint, corpo.GetProperty("endpoint").GetString());
        Assert.Equal("p256dh-e2e", corpo.GetProperty("p256dh").GetString());
        Assert.Equal("auth-e2e", corpo.GetProperty("auth").GetString());
        Assert.Empty(ApiPushFalsa.Locais(api.Chamadas[2]));
    }

    [Fact(DisplayName = "2.3 Permissão negada no pedido: avisa e não inscreve")]
    public async Task PermissaoNegada()
    {
        var api = await AbrirAsync(inscrito: false, respostaPermissao: "denied");

        await Botao.ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync("Permissão negada pelo navegador.");
        await Expect(Botao).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.DoesNotContain(api.Chamadas, c => c.Caminho == "/api/push/subscribe");
    }

    [Theory(DisplayName = "2.5/2.6 Servidor recusa a inscrição: avisa e desfaz a inscrição local")]
    [InlineData(429, "Muitas tentativas. Aguarde 1 minuto e tente novamente.")]
    [InlineData(500, "Não foi possível alterar os lembretes. Tente novamente.")]
    public async Task ServidorRecusaInscricao(int status, string aviso)
    {
        var api = await AbrirAsync(inscrito: false);
        api.Responder("/api/push/subscribe", status);

        await Botao.ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync(aviso);
        await Expect(Botao).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Botao).ToBeEnabledAsync();
        await Expect(Locais).ToBeHiddenAsync();
        Assert.Equal(1, await PushFalso.DesinscricoesAsync(Page));
        Assert.Empty(api.Puts);
    }

    [Fact(DisplayName = "2.4 Permissão já bloqueada: botão desabilitado e explicado")]
    public async Task PermissaoJaBloqueada()
    {
        await AbrirAsync(inscrito: false, permissao: "denied");

        await Expect(Botao).ToBeDisabledAsync();
        await Expect(Botao).ToHaveAttributeAsync("title", "Notificações bloqueadas pelo navegador");
    }

    [Fact(DisplayName = "2.8 Desativar com 429: avisa e a inscrição continua")]
    public async Task DesativarLimitado()
    {
        var api = await AbrirAsync(inscrito: true);
        api.Responder("/api/push/unsubscribe", 429);

        await Botao.ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync("Muitas tentativas. Aguarde 1 minuto e tente novamente.");
        await Expect(Botao).ToHaveTextAsync("Lembretes ativos");
        await Expect(Botao).ToBeEnabledAsync();
        await Expect(Locais).ToBeVisibleAsync();
        Assert.Equal(0, await PushFalso.DesinscricoesAsync(Page));
    }

    [Fact(DisplayName = "2.9 Duplo clique gera uma única inscrição")]
    public async Task DuploClique()
    {
        var api = await AbrirAsync(inscrito: false);
        api.Atrasar("/api/push/subscribe", 1000);

        await Botao.DblClickAsync();

        await Expect(Botao).ToHaveTextAsync("Lembretes ativos");
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.Single(api.Chamadas, c => c.Caminho == "/api/push/subscribe");
    }

    [Fact(DisplayName = "2.10 Navegador sem push: botão oculto")]
    public async Task SemSuporte()
    {
        await Page.AddInitScriptAsync("delete window.PushManager;");
        await Page.GotoAsync("/");

        await Expect(Botao).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "2.7 Desativar: desinscreve, limpa a sincronização e esconde as pílulas")]
    public async Task Desativar()
    {
        var api = await AbrirAsync(inscrito: true);
        await Expect(Locais).ToBeVisibleAsync();

        await Botao.ClickAsync();

        await Expect(Page.Locator(".toast-info")).ToHaveTextAsync("Lembretes desativados.");
        await Expect(Botao).ToHaveTextAsync("Me avisar dos feriados");
        await Expect(Locais).ToBeHiddenAsync();

        var desinscrever = Assert.Single(api.Chamadas, c => c.Caminho == "/api/push/unsubscribe");
        Assert.Equal(PushFalso.Endpoint, desinscrever.Corpo!.Value.GetProperty("endpoint").GetString());
        Assert.Equal(1, await PushFalso.DesinscricoesAsync(Page));
        Assert.Null(await Page.EvaluateAsync<string?>("() => localStorage.getItem('locaisPush')"));
    }
}

/// <summary>Caso 2.11: iPhone fora do app instalado abre o modal de instalação em vez de pedir permissão.</summary>
public class LembreteIosTests(AppFixture app) : E2ETest(app)
{
    protected override void ConfigurarContexto(BrowserNewContextOptions opcoes)
    {
        opcoes.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1";
        opcoes.HasTouch = true;
        opcoes.IsMobile = App.NomeBrowser != "firefox"; // Firefox não aceita isMobile
    }

    [Fact(DisplayName = "2.11 iPhone fora do app: modal de instalação abre e fecha no ×, no Esc e no fundo")]
    public async Task ModalDeInstalacao()
    {
        await Page.GotoAsync("/");
        var botao = Page.Locator("#btnLembrete");
        var modal = Page.Locator("#iosInstallOverlay");

        await botao.ClickAsync();
        await Expect(modal).ToBeVisibleAsync();
        await Page.Locator("#iosInstallClose").ClickAsync();
        await Expect(modal).ToBeHiddenAsync();

        await botao.ClickAsync();
        await Expect(modal).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(modal).ToBeHiddenAsync();

        await botao.ClickAsync();
        await Expect(modal).ToBeVisibleAsync();
        await Page.Mouse.ClickAsync(10, 10); // o modal fica embaixo; o topo da tela é o fundo
        await Expect(modal).ToBeHiddenAsync();
    }
}
