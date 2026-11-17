namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 10: a página abre limpa, com a CSP de produção ligada.</summary>
public class SaudeTests(AppFixture app) : E2ETest(app)
{
    [Fact(DisplayName = "10.1 Página carrega sem erro no console nem bloqueio de CSP")]
    public async Task SemErros()
    {
        var erros = new List<string>();
        Page.Console += (_, m) => { if (m.Type == "error") lock (erros) erros.Add("console: " + m.Text); };
        Page.PageError += (_, e) => { lock (erros) erros.Add("pageerror: " + e); };
        await Page.AddInitScriptAsync("""
            document.addEventListener('securitypolicyviolation', e => console.error('CSP bloqueou ' + e.violatedDirective + ' ' + e.blockedURI));
            """);

        var resposta = await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Task.Delay(1000, TestContext.Current.CancellationToken); // um ciclo da contagem e do push

        Assert.Contains("script-src", await resposta!.HeaderValueAsync("content-security-policy") ?? "");
        lock (erros) Assert.Empty(erros);
    }

    [Fact(DisplayName = "10.2 Nenhum aviso aparece sozinho ao abrir a página")]
    public async Task SemAvisoAoAbrir()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        Assert.Equal(0, await Page.Locator(".toast").CountAsync());
    }
}
