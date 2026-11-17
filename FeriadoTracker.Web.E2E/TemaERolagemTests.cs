using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenários 8 e 9: tema claro/escuro (script do _Layout) e rolagem suave (rolagem.js).</summary>
public class TemaERolagemTests(AppFixture app) : E2ETest(app)
{
    private static readonly System.Text.RegularExpressions.Regex Escuro = new(@"\bdark\b");

    [Fact(DisplayName = "8.1 Alternar o tema muda a cor da barra, salva e persiste no reload")]
    public async Task AlternarTema()
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Light });
        await Page.GotoAsync("/");
        var html = Page.Locator("html");
        var cor = Page.Locator("meta[name='theme-color']");
        await Expect(html).Not.ToHaveClassAsync(Escuro);

        await Page.Locator("#themeToggle").ClickAsync();

        await Expect(html).ToHaveClassAsync(Escuro);
        await Expect(Page.Locator("#themeToggle")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(cor).ToHaveAttributeAsync("content", "#0F1626");
        Assert.Equal("dark", await Page.EvaluateAsync<string>("() => localStorage.getItem('theme')"));

        await Page.ReloadAsync();
        await Expect(html).ToHaveClassAsync(Escuro);
        await Expect(cor).ToHaveAttributeAsync("content", "#0F1626");

        await Page.Locator("#themeToggle").ClickAsync();
        await Expect(html).Not.ToHaveClassAsync(Escuro);
        await Expect(cor).ToHaveAttributeAsync("content", "#F3F5F9");
    }

    [Theory(DisplayName = "8.2 Sem escolha salva, segue o tema do sistema")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SegueOSistema(bool sistemaEscuro)
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = sistemaEscuro ? ColorScheme.Dark : ColorScheme.Light });
        await Page.GotoAsync("/");

        if (sistemaEscuro) await Expect(Page.Locator("html")).ToHaveClassAsync(Escuro);
        else await Expect(Page.Locator("html")).Not.ToHaveClassAsync(Escuro);
        await Expect(Page.Locator("#themeToggle")).ToHaveAttributeAsync("aria-pressed", sistemaEscuro ? "true" : "false");
    }

    [Theory(DisplayName = "9.1 Com 'reduzir movimento', o Lenis não é carregado")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReduzirMovimento(bool reduzir)
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = reduzir ? ReducedMotion.Reduce : ReducedMotion.NoPreference });
        var pediuLenis = false;
        Page.Request += (_, r) => { if (r.Url.Contains("/lib/lenis/")) pediuLenis = true; };

        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Equal(!reduzir, pediuLenis);
    }
}
