using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 4: lista clicável e destaque (destaque.js).</summary>
public class DestaqueTests(AppFixture app) : E2ETest(app)
{
    [Fact(DisplayName = "4.1/4.2 Escolher um feriado na lista e voltar para a próxima folga")]
    public async Task EscolherEVoltar()
    {
        await Page.GotoAsync("/");
        var nome = Page.Locator("#focusName");
        var kicker = Page.Locator("#focusKicker");
        var voltar = Page.Locator("#focusBack");
        var proximaNome = await nome.TextContentAsync();
        var proximaKicker = await kicker.TextContentAsync();

        // Fixa pelo índice: o filtro por aria-pressed mudaria de alvo depois do clique.
        var indice = await Page.Locator(".row[data-indice][aria-pressed='false']").Nth(1).GetAttributeAsync("data-indice");
        var linha = Page.Locator($".row[data-indice='{indice}']");
        var escolhido = await linha.GetAttributeAsync("data-nome");
        var dataTexto = await linha.GetAttributeAsync("data-data-texto");
        var impacto = await linha.GetAttributeAsync("data-impacto");

        await linha.ClickAsync();

        await Expect(nome).ToHaveTextAsync(escolhido!);
        await Expect(Page.Locator("#focusDate")).ToHaveTextAsync(dataTexto!);
        await Expect(Page.Locator("#focusImpact")).ToHaveTextAsync(impacto!);
        await Expect(kicker).ToHaveTextAsync("Feriado escolhido");
        await Expect(voltar).ToBeVisibleAsync();
        await Expect(linha).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator(".row[data-indice][aria-pressed='true']")).ToHaveCountAsync(1);
        await Expect(nome).ToBeFocusedAsync();

        await voltar.Locator("button").ClickAsync();

        await Expect(nome).ToHaveTextAsync(proximaNome!);
        await Expect(kicker).ToHaveTextAsync(proximaKicker!);
        await Expect(voltar).ToBeHiddenAsync();
        await Expect(linha).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Fact(DisplayName = "4.3 Legenda de ponte só aparece quando o feriado tem ponte")]
    public async Task LegendaDePonte()
    {
        await Page.GotoAsync("/");
        var comPonte = Page.Locator(".row[data-tem-ponte='true']");
        var semPonte = Page.Locator(".row[data-tem-ponte='false']:not([data-tipo='FimDeSemana'])");
        if (await comPonte.CountAsync() == 0) Assert.Skip("Nenhum feriado com ponte no período atual.");

        await comPonte.First.ClickAsync();
        await Expect(Page.Locator("#legendPonte")).ToBeVisibleAsync();

        await semPonte.First.ClickAsync();
        await Expect(Page.Locator("#legendPonte")).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "4.4 Pelo teclado: troca sem animação e foco no nome")]
    public async Task PeloTeclado()
    {
        await Page.GotoAsync("/");
        var linha = Page.Locator(".row[data-indice][aria-pressed='false']").Nth(2);
        var escolhido = await linha.GetAttributeAsync("data-nome");

        await linha.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#focusName")).ToHaveTextAsync(escolhido!);
        await Expect(Page.Locator("#focusName")).ToBeFocusedAsync();
        await Expect(Page.Locator("#destaque")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-troca\b"));
    }

    [Fact(DisplayName = "4.5 No celular, escolher uma linha rola até o destaque")]
    public async Task RolaAteODestaque()
    {
        await Page.GotoAsync("/");
        var linha = Page.Locator(".row[data-indice]").Last;
        await linha.ScrollIntoViewIfNeededAsync();
        await Page.WaitForFunctionAsync("() => window.scrollY > 300");

        await linha.ClickAsync();

        // Para logo abaixo do cabeçalho fixo (cabeçalho + 8 px), com folga para a rolagem suave terminar.
        await Page.WaitForFunctionAsync("""
            () => {
                const topo = document.getElementById('destaque').getBoundingClientRect().top;
                const cabecalho = document.querySelector('.topbar').offsetHeight;
                return Math.abs(topo - cabecalho - 8) < 4;
            }
            """, null, new() { Timeout = 5000 });
    }
}

/// <summary>Caso 4.6: com mouse, passar sobre uma linha mostra a contagem viva dela.</summary>
public class DestaqueDesktopTests(AppFixture app) : E2ETest(app)
{
    protected override void ConfigurarContexto(BrowserNewContextOptions opcoes)
    {
        opcoes.ViewportSize = new() { Width = 1280, Height = 900 };
        opcoes.RecordVideoSize = new() { Width = 1280, Height = 900 };
    }

    [Fact(DisplayName = "4.6 No desktop, passar o mouse numa linha mostra a contagem viva")]
    public async Task ContagemVivaNoHover()
    {
        await Page.GotoAsync("/");
        var linha = Page.Locator(".row[data-indice][aria-pressed='false']").Nth(1);

        await linha.HoverAsync();
        await Expect(linha.Locator(".row-live")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(@"^\d+d \d\dh \d\dmin \d\ds$"));

        await Page.Mouse.MoveAsync(5, 5);
        await Expect(Page.Locator(".row-live")).ToHaveCountAsync(0);
    }
}
