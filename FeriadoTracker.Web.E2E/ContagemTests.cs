using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>
/// Cenário 3: contagem regressiva (countdown.js). O relógio do navegador é fixado com page.Clock
/// em volta das datas da própria lista, então os casos valem em qualquer dia em que rodarem.
/// O servidor continua na hora real: a contagem é toda calculada no navegador.
/// </summary>
public class ContagemTests(AppFixture app) : E2ETest(app)
{
    private sealed record Linha(int Indice, string Nome, string Tipo, string InicioIso, string FimIso, string FolgaFimIso, string NotaNoDia);

    private ILocator Contagem => Page.Locator("#focusCount");
    private ILocator Kicker => Page.Locator("#focusKicker");

    /// <summary>Lê as linhas da lista e qual é a próxima folga (sem relógio falso).</summary>
    private async Task<(List<Linha> Linhas, int Proxima)> LerListaAsync()
    {
        await Page.GotoAsync("/");
        var json = await Page.EvaluateAsync<string>("""
            () => JSON.stringify({
                proxima: Number(document.getElementById('destaque').dataset.proxima),
                linhas: [...document.querySelectorAll('.row[data-indice]')].map(l => ({
                    indice: Number(l.dataset.indice), nome: l.dataset.nome, tipo: l.dataset.tipo,
                    inicioIso: l.dataset.inicioIso, fimIso: l.dataset.fimIso,
                    folgaFimIso: l.dataset.folgaFimIso, notaNoDia: l.dataset.notaNoDia }))
            })
            """);
        using var doc = JsonDocument.Parse(json);
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var linhas = doc.RootElement.GetProperty("linhas").Deserialize<List<Linha>>(opcoes)!;
        return (linhas, doc.RootElement.GetProperty("proxima").GetInt32());
    }

    private static DateTimeOffset MeiaNoite(string iso) =>
        new(DateTime.ParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture), TimeSpan.FromHours(-3));

    /// <summary>Abre a página com o relógio do navegador neste instante (horário de Brasília).</summary>
    private async Task AbrirEmAsync(DateTimeOffset instante)
    {
        await Page.Clock.InstallAsync(new() { TimeString = instante.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture) });
        await Page.GotoAsync("/");
    }

    /// <summary>Primeira folga de 3+ dias a partir da próxima (para os casos "no dia" e "de folga").</summary>
    private static Linha Prolongada(List<Linha> linhas, int proxima) =>
        linhas.First(l => l.Indice >= proxima && l.Tipo == "Prolongado" && string.CompareOrdinal(l.FolgaFimIso, l.FimIso) > 0);

    [Fact(DisplayName = "3.1 Mais de 48 h: dias no número grande, relógio embaixo e título da aba")]
    public async Task MaisDe48Horas()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = linhas[proxima];
        await AbrirEmAsync(MeiaNoite(alvo.InicioIso) - new TimeSpan(5, 3, 4, 30));

        await Expect(Contagem).ToHaveTextAsync("5dias");
        await Expect(Page.Locator("#focusExact .clock-num").Nth(0)).ToHaveTextAsync("03");
        await Expect(Page.Locator("#focusExact .clock-num").Nth(1)).ToHaveTextAsync("04");
        await Expect(Page).ToHaveTitleAsync($"5 dias para {alvo.Nome}");
        await Expect(Kicker).ToHaveTextAsync("Sua próxima folga");
    }

    [Fact(DisplayName = "3.2 Entre 1 h e 48 h: horas e minutos")]
    public async Task Entre1e48Horas()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = linhas[proxima];
        await AbrirEmAsync(MeiaNoite(alvo.InicioIso) - new TimeSpan(0, 5, 7, 30));

        await Expect(Contagem).ToHaveTextAsync("5h07min");
        await Expect(Page.Locator("#focusExact")).ToContainTextAsync("Começa à meia-noite. Faltam 5h 07min");
        await Expect(Page).ToHaveTitleAsync($"5 h 07 min para {alvo.Nome}");
    }

    [Fact(DisplayName = "3.3 Última hora: mm:ss")]
    public async Task UltimaHora()
    {
        var (linhas, proxima) = await LerListaAsync();
        await AbrirEmAsync(MeiaNoite(linhas[proxima].InicioIso) - new TimeSpan(0, 0, 12, 30));

        await Expect(Contagem).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(@"^12:[0-5]\d$"));
        await Expect(Page.Locator("#focusExact")).ToHaveTextAsync("Começa à meia-noite.");
    }

    [Fact(DisplayName = "3.4 No dia do feriado: modo âmbar e 'Hoje é feriado'")]
    public async Task NoDia()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = Prolongada(linhas, proxima);
        await AbrirEmAsync(MeiaNoite(alvo.InicioIso).AddHours(10));

        await Expect(Page.Locator("#focusName")).ToHaveTextAsync(alvo.Nome);
        await Expect(Contagem).ToHaveTextAsync("Hoje é feriado");
        await Expect(Kicker).ToHaveTextAsync("Sua folga começou");
        await Expect(Page.Locator("body")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-holiday\b"));
        await Expect(Page).ToHaveTitleAsync($"Hoje é {alvo.Nome}");
    }

    [Fact(DisplayName = "3.5 Depois do feriado, ainda de folga; no último dia, 'Aproveite o dia.'")]
    public async Task DeFolga()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = Prolongada(linhas, proxima);

        await AbrirEmAsync(MeiaNoite(alvo.FimIso).AddDays(1).AddHours(10));
        await Expect(Contagem).ToHaveTextAsync("Você está de folga");
        await Expect(Kicker).ToHaveTextAsync("Sua folga começou");
        await Expect(Page.Locator("body")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-holiday\b"));
        if (MeiaNoite(alvo.FimIso).AddDays(1) < MeiaNoite(alvo.FolgaFimIso))
            await Expect(Page.Locator("#focusNote")).ToHaveTextAsync(alvo.NotaNoDia);

        await AbrirEmAsync(MeiaNoite(alvo.FolgaFimIso).AddHours(10));
        await Expect(Contagem).ToHaveTextAsync("Você está de folga");
        await Expect(Page.Locator("#focusNote")).ToHaveTextAsync("Aproveite o dia.");
    }

    [Fact(DisplayName = "3.5b Página aberta de um dia de folga para o último: nota vira 'Aproveite o dia.'")]
    public async Task DeFolgaViraUltimoDia()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = Prolongada(linhas, proxima);
        var ultimoDia = MeiaNoite(alvo.FolgaFimIso);
        if (MeiaNoite(alvo.FimIso).AddDays(1) >= ultimoDia) Assert.Skip("Folga sem dia intermediário depois do feriado.");

        await AbrirEmAsync(ultimoDia.AddSeconds(-3));
        await Expect(Page.Locator("#focusNote")).ToHaveTextAsync(alvo.NotaNoDia);

        await Page.Clock.RunForAsync(5000); // passa da meia-noite com a página aberta

        await Expect(Page.Locator("#focusNote")).ToHaveTextAsync("Aproveite o dia.");
    }

    [Fact(DisplayName = "3.6 A folga acaba com a página aberta: passa sozinha para a próxima")]
    public async Task AvancaSozinha()
    {
        var (linhas, proxima) = await LerListaAsync();
        var atual = linhas[proxima];
        var fimDaFolga = MeiaNoite(atual.FolgaFimIso).AddDays(1);
        var seguinte = linhas.First(l => l.Indice > proxima && l.Tipo != "FimDeSemana"
            && string.CompareOrdinal(l.FolgaFimIso, atual.FolgaFimIso) > 0);

        await AbrirEmAsync(fimDaFolga.AddSeconds(-3));
        await Expect(Page.Locator("#focusName")).ToHaveTextAsync(atual.Nome);

        await Page.Clock.RunForAsync(5000);

        await Expect(Page.Locator("#focusName")).ToHaveTextAsync(seguinte.Nome);
        await Expect(Kicker).ToHaveTextAsync("Sua próxima folga");
        await Expect(Page.Locator("body")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-holiday\b"));
        await Expect(Page.Locator($".row[data-indice='{seguinte.Indice}']")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#focusBack")).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "3.7 O dia de hoje aparece marcado na faixa")]
    public async Task HojeNaFaixa()
    {
        var (linhas, proxima) = await LerListaAsync();
        var alvo = Prolongada(linhas, proxima);
        await AbrirEmAsync(MeiaNoite(alvo.InicioIso).AddHours(10));

        await Expect(Page.Locator($"#focusStrip li[data-iso='{alvo.InicioIso}']")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\btoday\b"));
        await Expect(Page.Locator("#focusStrip li.today")).ToHaveCountAsync(1);
    }
}
