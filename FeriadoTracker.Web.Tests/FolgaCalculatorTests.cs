using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class FolgaCalculatorTests
{
    private static Feriado F(int id, string nome, int ano, int mes, int dia) =>
        new() { Id = id, Nome = nome, Data = new DateOnly(ano, mes, dia), Tipo = "Nacional" };

    private static FeriadoAnalisado Unico(params Feriado[] feriados) =>
        Assert.Single(FolgaCalculator.Analisar(feriados));

    [Fact]
    public void Sexta_ViraFimDeSemanaDe3Dias()
    {
        var a = Unico(F(1, "Consciência Negra", 2026, 11, 20));

        Assert.Equal(TipoFolga.Prolongado, a.Tipo);
        Assert.Equal(new DateOnly(2026, 11, 20), a.FolgaInicio);
        Assert.Equal(new DateOnly(2026, 11, 22), a.FolgaFim);
        Assert.Equal(3, a.DiasDeFolga);
        Assert.Null(a.Ponte);
        Assert.Equal(3, a.DiasComPonte);
    }

    [Fact]
    public void Segunda_ViraFimDeSemanaDe3Dias()
    {
        var a = Unico(F(1, "Independência", 2026, 9, 7));

        Assert.Equal(TipoFolga.Prolongado, a.Tipo);
        Assert.Equal(new DateOnly(2026, 9, 5), a.FolgaInicio);
        Assert.Equal(new DateOnly(2026, 9, 7), a.FolgaFim);
        Assert.Equal(3, a.DiasDeFolga);
        Assert.Null(a.Ponte);
    }

    [Fact]
    public void Terca_TemPonteNaSegunda()
    {
        var a = Unico(F(1, "Tiradentes", 2026, 4, 21));

        Assert.Equal(TipoFolga.Ponte, a.Tipo);
        Assert.Equal(1, a.DiasDeFolga);
        Assert.Equal(new DateOnly(2026, 4, 20), a.Ponte);
        Assert.Equal(4, a.DiasComPonte);
    }

    [Fact]
    public void Quinta_TemPonteNaSexta()
    {
        var a = Unico(F(1, "Corpus Christi", 2027, 5, 27));

        Assert.Equal(TipoFolga.Ponte, a.Tipo);
        Assert.Equal(1, a.DiasDeFolga);
        Assert.Equal(new DateOnly(2027, 5, 28), a.Ponte);
        Assert.Equal(4, a.DiasComPonte);
    }

    [Fact]
    public void Quarta_EhFolgaNoMeioDaSemana()
    {
        var a = Unico(F(1, "Dia do Trabalho", 2024, 5, 1));

        Assert.Equal(TipoFolga.MeioDaSemana, a.Tipo);
        Assert.Equal(1, a.DiasDeFolga);
        Assert.Null(a.Ponte);
        Assert.Equal(1, a.DiasComPonte);
    }

    [Theory]
    [InlineData(2026, 11, 15)] // domingo
    [InlineData(2027, 5, 1)]   // sábado
    public void FimDeSemana_NaoViraFolga(int ano, int mes, int dia)
    {
        var a = Unico(F(1, "Feriado", ano, mes, dia));

        Assert.Equal(TipoFolga.FimDeSemana, a.Tipo);
        Assert.Equal(0, a.DiasDeFolga);
        Assert.Null(a.Ponte);
    }

    [Fact]
    public void Carnaval_JuntaOsDoisDiasEmUmFeriadoDe4Dias()
    {
        var a = Unico(F(1, "Carnaval", 2027, 2, 8), F(2, "Carnaval", 2027, 2, 9));

        Assert.Equal("Carnaval", a.Nome);
        Assert.Equal(new DateOnly(2027, 2, 8), a.Inicio);
        Assert.Equal(new DateOnly(2027, 2, 9), a.Fim);
        Assert.Equal(TipoFolga.Prolongado, a.Tipo);
        Assert.Equal(new DateOnly(2027, 2, 6), a.FolgaInicio);
        Assert.Equal(new DateOnly(2027, 2, 9), a.FolgaFim);
        Assert.Equal(4, a.DiasDeFolga);
    }

    [Fact]
    public void MesmoNomeEmDatasDistantes_NaoAgrupa()
    {
        var analisados = FolgaCalculator.Analisar([F(1, "Natal", 2026, 12, 25), F(2, "Natal", 2027, 12, 25)]);

        Assert.Equal(2, analisados.Count);
    }

    [Fact]
    public void ProximaFolga_PulaFeriadoDeFimDeSemana()
    {
        var analisados = FolgaCalculator.Analisar(
        [
            F(1, "Consciência Negra", 2026, 11, 20),
            F(2, "Proclamação da República", 2026, 11, 15)
        ]);

        Assert.Equal("Proclamação da República", analisados[0].Nome);
        Assert.Equal("Consciência Negra", FolgaCalculator.ProximaFolga(analisados)?.Nome);
    }

    private static HolidayService ServicoEm(AppDbContext ctx, DateTime agoraUtc)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(agoraUtc, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        return new HolidayService(ctx, time);
    }

    private static async Task<AppDbContext> ContextoComSeed(string nome)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(nome).Options;
        var ctx = new AppDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
        return ctx;
    }

    [Fact]
    public async Task Servico_Em9DeNovembro_ProximaFolgaEhConscienciaNegra()
    {
        await using var ctx = await ContextoComSeed(nameof(Servico_Em9DeNovembro_ProximaFolgaEhConscienciaNegra));

        var analisados = await ServicoEm(ctx, new DateTime(2026, 11, 9)).GetProximosFeriadosAnalisadosAsync();
        var proxima = FolgaCalculator.ProximaFolga(analisados);

        Assert.Equal(TipoFolga.FimDeSemana, analisados[0].Tipo);
        Assert.Equal("Dia Nacional da Consciência Negra", proxima?.Nome);
        Assert.Equal(new DateOnly(2026, 11, 22), proxima?.FolgaFim);
    }

    [Fact]
    public async Task Servico_NaTercaDeCarnaval_MantemOsDoisDias()
    {
        await using var ctx = await ContextoComSeed(nameof(Servico_NaTercaDeCarnaval_MantemOsDoisDias));

        var analisados = await ServicoEm(ctx, new DateTime(2027, 2, 9)).GetProximosFeriadosAnalisadosAsync();

        Assert.Equal("Carnaval", analisados[0].Nome);
        Assert.Equal(new DateOnly(2027, 2, 8), analisados[0].Inicio);
        Assert.Equal(4, analisados[0].DiasDeFolga);
    }

    [Fact]
    public async Task Servico_Tem13FeriadosEm2027()
    {
        await using var ctx = await ContextoComSeed(nameof(Servico_Tem13FeriadosEm2027));

        Assert.Equal(13, await ctx.Feriados.CountAsync(f => f.Data.Year == 2027));
    }
}
