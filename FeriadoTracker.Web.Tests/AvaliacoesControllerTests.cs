using System.Reflection;
using FeriadoTracker.Web.Controllers;
using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class AvaliacoesControllerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 11, 15, 30, 0, TimeSpan.Zero);

    private static AppDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static AvaliacoesController Controller(AppDbContext ctx) => new(ctx, new FakeTimeProvider(Agora));

    #region Casos válidos

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task Criar_AceitaNotaDe1a5SemComentario(int estrelas)
    {
        await using var ctx = CreateContext($"{nameof(Criar_AceitaNotaDe1a5SemComentario)}{estrelas}");

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = estrelas }, default);

        Assert.IsType<NoContentResult>(result);
        var salva = await ctx.Avaliacoes.SingleAsync();
        Assert.Equal(estrelas, salva.Estrelas);
        Assert.Null(salva.Comentario);
    }

    [Fact]
    public async Task Criar_AceitaNotaComComentario()
    {
        await using var ctx = CreateContext(nameof(Criar_AceitaNotaComComentario));

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 4, Comentario = "Muito prático." }, default);

        Assert.IsType<NoContentResult>(result);
        var salva = await ctx.Avaliacoes.SingleAsync();
        Assert.Equal(4, salva.Estrelas);
        Assert.Equal("Muito prático.", salva.Comentario);
    }

    [Fact]
    public async Task Criar_ResponderSemCorpo()
    {
        await using var ctx = CreateContext(nameof(Criar_ResponderSemCorpo));

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 5, Comentario = "ok" }, default);

        // Resposta cega: nada de Id nem dados internos devolvidos.
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Criar_AceitaComentarioNoLimiteDe500()
    {
        await using var ctx = CreateContext(nameof(Criar_AceitaComentarioNoLimiteDe500));

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 5, Comentario = new string('a', 500) }, default);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(500, (await ctx.Avaliacoes.SingleAsync()).Comentario!.Length);
    }

    #endregion

    #region Normalização do comentário

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t  \r\n")]
    public async Task Criar_ComentarioVazioOuSoEspacosViraNull(string? comentario)
    {
        await using var ctx = CreateContext($"{nameof(Criar_ComentarioVazioOuSoEspacosViraNull)}{comentario?.Length}");

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 3, Comentario = comentario }, default);

        Assert.IsType<NoContentResult>(result);
        Assert.Null((await ctx.Avaliacoes.SingleAsync()).Comentario);
    }

    [Fact]
    public async Task Criar_AplicaTrimNoComentario()
    {
        await using var ctx = CreateContext(nameof(Criar_AplicaTrimNoComentario));

        await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 3, Comentario = "  gostei  \n" }, default);

        Assert.Equal("gostei", (await ctx.Avaliacoes.SingleAsync()).Comentario);
    }

    [Fact]
    public async Task Criar_RemoveCaracteresDeControleEMantemQuebraDeLinha()
    {
        await using var ctx = CreateContext(nameof(Criar_RemoveCaracteresDeControleEMantemQuebraDeLinha));

        // \u0000 nulo, \u0007 sino, ​ não é de controle (fica), \u001B escape.
        await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 3, Comentario = "a\u0000b\u0007c\u001Bd\ne\tf" }, default);

        Assert.Equal("abcd\ne\tf", (await ctx.Avaliacoes.SingleAsync()).Comentario);
    }

    [Fact]
    public async Task Criar_ComentarioSoDeControleViraNull()
    {
        await using var ctx = CreateContext(nameof(Criar_ComentarioSoDeControleViraNull));

        await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 3, Comentario = "\u0000\u0007" }, default);

        Assert.Null((await ctx.Avaliacoes.SingleAsync()).Comentario);
    }

    [Fact]
    public async Task Criar_EspacosNasPontasNaoContamParaOLimite()
    {
        await using var ctx = CreateContext(nameof(Criar_EspacosNasPontasNaoContamParaOLimite));
        var texto = "  " + new string('a', 500) + "  ";

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 3, Comentario = texto }, default);

        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Casos inválidos

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task Criar_RejeitaNotaForaDe1a5(int estrelas)
    {
        await using var ctx = CreateContext($"{nameof(Criar_RejeitaNotaForaDe1a5)}{estrelas}");

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = estrelas, Comentario = "ok" }, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await ctx.Avaliacoes.ToListAsync());
    }

    [Fact]
    public async Task Criar_RejeitaComentarioComMaisDe500Caracteres()
    {
        await using var ctx = CreateContext(nameof(Criar_RejeitaComentarioComMaisDe500Caracteres));

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 5, Comentario = new string('a', 501) }, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await ctx.Avaliacoes.ToListAsync());
    }

    [Fact]
    public async Task Criar_MensagemDeErroNaoVazaDetalhes()
    {
        await using var ctx = CreateContext(nameof(Criar_MensagemDeErroNaoVazaDetalhes));

        var result = await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 9 }, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("{ error = Avaliação inválida. }", bad.Value!.ToString());
    }

    #endregion

    #region Persistência e privacidade

    [Fact]
    public async Task Criar_PreencheDataNoServidorComTimeProvider()
    {
        await using var ctx = CreateContext(nameof(Criar_PreencheDataNoServidorComTimeProvider));

        await Controller(ctx).Criar(new AvaliacaoDto { Estrelas = 5 }, default);

        Assert.Equal(Agora.UtcDateTime, (await ctx.Avaliacoes.SingleAsync()).CriadoEmUtc);
    }

    [Fact]
    public void Avaliacao_GuardaApenasNotaComentarioEData()
    {
        // Privacidade: se alguém acrescentar coluna (IP, User-Agent...), este teste obriga a rever.
        var colunas = typeof(Models.Avaliacao).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(["Comentario", "CriadoEmUtc", "Estrelas", "Id"], colunas);
    }

    #endregion

    #region Proteções do endpoint

    [Fact]
    public void Criar_ExigeTokenAntiforgery()
    {
        var acao = typeof(AvaliacoesController).GetMethod(nameof(AvaliacoesController.Criar))!;

        Assert.NotNull(acao.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(acao.GetCustomAttribute<HttpPostAttribute>());
    }

    [Fact]
    public void Controller_UsaALimitacaoDeEnviosDeAvaliacao()
    {
        var politica = typeof(AvaliacoesController).GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.Equal("avaliacao", politica?.PolicyName);
    }

    [Fact]
    public void Controller_NaoTemEndpointDeLeitura()
    {
        var leituras = typeof(AvaliacoesController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpGetAttribute>() is not null);

        Assert.Empty(leituras);
    }

    [Fact]
    public void Dto_ComentarioNaoEhObrigatorioParaOModelBinder()
    {
        // Com NullabilityInfoContextSupport=false o MVC pode exigir string? como obrigatória;
        // ValidateNever evita o 400 para avaliação sem comentário.
        var propriedade = typeof(AvaliacaoDto).GetProperty(nameof(AvaliacaoDto.Comentario))!;

        Assert.NotNull(propriedade.GetCustomAttribute<ValidateNeverAttribute>());
    }

    #endregion
}
