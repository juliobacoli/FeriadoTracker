using System.Globalization;
using System.Text.Json;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 7: botão Compartilhar (share.js).</summary>
public class CompartilharTests(AppFixture app) : E2ETest(app)
{
    private sealed record Compartilhado(string Title, string Text, string Url);

    private const string ShareFalso = "navigator.share = async dados => { window.__compartilhado = dados; };";

    private static string TextoEsperado(string nome, string inicioIso)
    {
        var inicio = DateOnly.ParseExact(inicioIso, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
        var dias = inicio.DayNumber - hoje.DayNumber;
        return dias <= 0 ? $"Hoje é {nome}! Veja no FeriadoTracker."
            : dias == 1 ? $"Amanhã é {nome}! Veja no FeriadoTracker."
            : $"Faltam {dias} dias para {nome}! Veja no FeriadoTracker.";
    }

    private async Task<Compartilhado> CompartilharAsync()
    {
        await Page.Locator("#btnShare").ClickAsync();
        await Page.WaitForFunctionAsync("() => window.__compartilhado");
        return JsonSerializer.Deserialize<Compartilhado>(
            await Page.EvaluateAsync<string>("() => JSON.stringify(window.__compartilhado)"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    [Fact(DisplayName = "7.1 Com navigator.share: texto com os dias até a próxima folga e o endereço do site")]
    public async Task CompartilhaProximaFolga()
    {
        await Page.AddInitScriptAsync(ShareFalso);
        await Page.GotoAsync("/");
        var linha = Page.Locator(".row[data-indice][aria-pressed='true']");
        var esperado = TextoEsperado((await linha.GetAttributeAsync("data-nome"))!, (await linha.GetAttributeAsync("data-inicio-iso"))!);

        var dados = await CompartilharAsync();

        Assert.Equal("FeriadoTracker", dados.Title);
        Assert.Equal(esperado, dados.Text);
        Assert.Equal(App.BaseUrl, dados.Url);
    }

    [Fact(DisplayName = "7.2 Sem navigator.share: copia o link e o rótulo volta depois de 3 s")]
    public async Task CopiaOLink()
    {
        await Page.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'share', { configurable: true, value: undefined });
            Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async t => { window.__copiado = t; } } });
            """);
        await Page.GotoAsync("/");
        var botao = Page.Locator("#btnShare");

        await botao.ClickAsync();

        await Expect(botao).ToHaveTextAsync("Link copiado");
        await Expect(Page.Locator(".toast-success")).ToHaveTextAsync("Link copiado. É só colar na conversa.");
        Assert.Equal(App.BaseUrl, await Page.EvaluateAsync<string>("() => window.__copiado"));
        await Expect(botao).ToHaveTextAsync("Compartilhar", new() { Timeout = 5000 });
    }

    [Fact(DisplayName = "7.3 Falha ao copiar: aviso de erro")]
    public async Task FalhaAoCopiar()
    {
        await Page.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'share', { configurable: true, value: undefined });
            Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async () => { throw new Error('negado'); } } });
            """);
        await Page.GotoAsync("/");

        await Page.Locator("#btnShare").ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync("Não foi possível copiar o link.");
        await Expect(Page.Locator("#btnShare")).ToHaveTextAsync("Compartilhar");
    }

    [Fact(DisplayName = "7.4 Compartilha o feriado escolhido na lista, não a próxima folga")]
    public async Task CompartilhaEscolhido()
    {
        await Page.AddInitScriptAsync(ShareFalso);
        await Page.GotoAsync("/");
        var indice = await Page.Locator(".row[data-indice][aria-pressed='false']").Nth(2).GetAttributeAsync("data-indice");
        var linha = Page.Locator($".row[data-indice='{indice}']");
        var esperado = TextoEsperado((await linha.GetAttributeAsync("data-nome"))!, (await linha.GetAttributeAsync("data-inicio-iso"))!);
        await linha.ClickAsync();

        var dados = await CompartilharAsync();

        Assert.Equal(esperado, dados.Text);
    }
}
