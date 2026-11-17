using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 5: localidades vindas da URL e lembradas no aparelho (script do _Layout).</summary>
public class LocaisUrlTests(AppFixture app) : E2ETest(app)
{
    [Fact(DisplayName = "5.1 ?local= válido fica salvo no aparelho; código inválido é ignorado")]
    public async Task UrlSalvaNoAparelho()
    {
        await Page.GotoAsync("/?local=32&local=999");

        Assert.Equal("[\"32\"]", await Page.EvaluateAsync<string>("() => localStorage.getItem('locais')"));
        await Expect(Page.Locator("#listTitle")).ToHaveTextAsync("Feriados no Espírito Santo");
    }

    [Fact(DisplayName = "5.2 Abrir / com localidades salvas leva para /?local=…")]
    public async Task RaizRedirecionaParaSalvos()
    {
        await Page.GotoAsync("/");
        await Page.EvaluateAsync("() => localStorage.setItem('locais', JSON.stringify(['26']))");

        await Page.GotoAsync("/");

        await Page.WaitForURLAsync("**/?local=26");
        await Expect(Page.Locator("#listTitle")).ToHaveTextAsync("Feriados em Pernambuco");
        await Expect(Page.Locator("#locaisResumo")).ToHaveTextAsync("PE");
    }

    [Fact(DisplayName = "5.3 Uma localidade: título, kicker com o local e selo nos feriados regionais")]
    public async Task UmaLocalidade()
    {
        await Page.GotoAsync("/?local=32");

        await Expect(Page.Locator("#listTitle")).ToHaveTextAsync("Feriados no Espírito Santo");
        await Expect(Page.Locator("#locaisResumo")).ToHaveTextAsync("ES");
        await Expect(Page.Locator(".row .selo.is-regional").First).ToBeVisibleAsync();
        // O servidor escreve "Sua próxima folga no Espírito Santo"; a contagem não pode apagar o local.
        await Page.WaitForTimeoutAsync(1500);
        await Expect(Page.Locator("#focusKicker")).ToHaveTextAsync("Sua próxima folga no Espírito Santo");
    }

    [Fact(DisplayName = "5.4 Várias localidades: resumo 'Várias localidades' e título combinado")]
    public async Task VariasLocalidades()
    {
        await Page.GotoAsync("/?local=32&local=26");

        await Expect(Page.Locator("#locaisResumo")).ToHaveTextAsync("Várias localidades");
        await Expect(Page.Locator("#listTitle")).ToHaveTextAsync("Feriados nacionais e de várias localidades");
    }
}
