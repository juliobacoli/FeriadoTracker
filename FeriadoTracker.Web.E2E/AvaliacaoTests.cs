using FeriadoTracker.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>Cenário 6: avaliação da tela (avaliacao.js), com o servidor e o banco reais.</summary>
public class AvaliacaoTests(AppFixture app) : E2ETest(app)
{
    [Fact(DisplayName = "6.1 Card aparece para quem não avaliou e some para quem já avaliou")]
    public async Task CardSoParaQuemNaoAvaliou()
    {
        await Page.GotoAsync("/");
        await Expect(Page.Locator("#avaliacao")).ToBeVisibleAsync();

        await Page.EvaluateAsync("() => localStorage.setItem('avaliado', '1')");
        await Page.ReloadAsync();

        await Expect(Page.Locator("#avaliacao")).ToBeHiddenAsync();
    }

    [Fact(DisplayName = "6.2 Escolher estrela abre o comentário e mostra a nota")]
    public async Task EscolherEstrela()
    {
        await Page.GotoAsync("/");
        await Expect(Page.Locator("#avalCorpo")).ToBeHiddenAsync();

        await Page.Locator(".estrela-btn[data-valor='3']").ClickAsync();

        await Expect(Page.Locator("#avalCorpo")).ToBeVisibleAsync();
        await Expect(Page.Locator("#avalNota")).ToHaveTextAsync("3 de 5");
        await Expect(Page.Locator(".estrela-btn[aria-pressed='true']")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".estrela-btn[data-valor='3']")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator(".estrela-btn.is-active")).ToHaveCountAsync(3);
    }

    [Fact(DisplayName = "6.3 Contador de caracteres acompanha o texto; limite de 500")]
    public async Task ContadorDeCaracteres()
    {
        await Page.GotoAsync("/");
        await Page.Locator(".estrela-btn[data-valor='5']").ClickAsync();
        var campo = Page.Locator("#avalComentario");

        await campo.FillAsync("abc");
        await Expect(Page.Locator("#avalContador")).ToHaveTextAsync("3 / 500");

        await campo.FillAsync(new string('x', 600));
        Assert.Equal(500, (await campo.InputValueAsync()).Length);
        await Expect(Page.Locator("#avalContador")).ToHaveTextAsync("500 / 500");
    }

    [Theory(DisplayName = "6.5/6.6 Servidor recusa a avaliação: avisa, formulário continua e pode tentar de novo")]
    [InlineData(429, "Muitas tentativas. Aguarde 1 minuto e tente novamente.")]
    [InlineData(500, "Não foi possível enviar a avaliação. Tente novamente.")]
    public async Task ServidorRecusa(int status, string aviso)
    {
        await Page.RouteAsync("**/api/avaliacoes", r => r.FulfillAsync(new() { Status = status }));
        await Page.GotoAsync("/");
        await Page.Locator(".estrela-btn[data-valor='2']").ClickAsync();

        await Page.Locator("#avalEnviar").ClickAsync();

        await Expect(Page.Locator(".toast-error")).ToHaveTextAsync(aviso);
        await Expect(Page.Locator("#avalForm")).ToBeVisibleAsync();
        await Expect(Page.Locator("#avalEnviar")).ToBeEnabledAsync();
        Assert.Null(await Page.EvaluateAsync<string?>("() => localStorage.getItem('avaliado')"));
    }

    [Fact(DisplayName = "6.4 Enviar avaliação: salva no banco, agradece e não pede de novo")]
    public async Task EnviarAvaliacao()
    {
        var comentario = "e2e " + Guid.NewGuid().ToString("N");
        await Page.GotoAsync("/");

        await Page.Locator(".estrela-btn[data-valor='4']").ClickAsync();
        await Page.Locator("#avalComentario").FillAsync(comentario);
        await Page.Locator("#avalEnviar").ClickAsync();

        await Expect(Page.Locator("#avalPronto")).ToBeVisibleAsync();
        await Expect(Page.Locator("#avalPronto")).ToBeFocusedAsync();
        await Expect(Page.Locator("#avalForm")).ToBeHiddenAsync();
        await Expect(Page.Locator(".toast-success")).ToHaveTextAsync("Avaliação enviada. Muito obrigado pelo retorno!");

        using (var escopo = App.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            var salva = await db.Avaliacoes.SingleAsync(a => a.Comentario == comentario, TestContext.Current.CancellationToken);
            Assert.Equal(4, salva.Estrelas);
        }

        await Page.ReloadAsync();
        await Expect(Page.Locator("#avaliacao")).ToBeHiddenAsync();
    }
}
