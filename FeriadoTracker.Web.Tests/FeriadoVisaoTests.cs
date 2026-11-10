using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class FeriadoVisaoTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 9);

    private static Feriado F(string nome, int ano, int mes, int dia) =>
        new() { Nome = nome, Data = new DateOnly(ano, mes, dia), Tipo = "Nacional" };

    private static FeriadoVisao Visao(params Feriado[] feriados) =>
        FeriadoVisaoBuilder.Criar(Assert.Single(FolgaCalculator.Analisar(feriados)), Hoje);

    [Fact]
    public void Sexta_TextosEFaixaCentradaNaFolga()
    {
        var v = Visao(F("Consciência Negra", 2026, 11, 20));

        Assert.Equal("Sexta-feira, 20 de novembro", v.DataTexto);
        Assert.Equal("Fim de semana de 3 dias.", v.Impacto);
        Assert.Equal("Sexta-feira. Fim de semana de 3 dias", v.ImpactoCurto);
        Assert.Equal("Folga até domingo, 22 de novembro.", v.NotaNoDia);

        Assert.Equal(9, v.Faixa.Count);
        Assert.Equal("2026-11-17", v.Faixa[0].Iso);
        Assert.Equal("off holiday", v.Faixa[3].Classe);
        Assert.Equal(["", "", "", "off holiday", "off", "off", "", "", ""], v.Faixa.Select(d => d.Classe));
    }

    [Fact]
    public void Quinta_MarcaPonteNaSexta()
    {
        var v = Visao(F("Corpus Christi", 2027, 5, 27));

        Assert.True(v.TemPonte);
        Assert.Equal("Com a ponte na sexta-feira, são 4 dias de folga.", v.Impacto);
        Assert.Equal("Quinta-feira, 27 de maio de 2027", v.DataTexto);
        Assert.Equal("ponte", v.Faixa.Single(d => d.Iso == "2027-05-28").Classe);
        Assert.Contains("ponte possível", v.Faixa.Single(d => d.Iso == "2027-05-28").Rotulo);
    }

    [Fact]
    public void Domingo_NaoViraFolga()
    {
        var v = Visao(F("Proclamação da República", 2026, 11, 15));

        Assert.Equal("Cai num domingo. Não vira folga.", v.Impacto);
        Assert.Equal("Cai num domingo, então não vira folga extra.", v.NotaNoDia);
        Assert.Equal("weekend-only holiday", v.Faixa.Single(d => d.Iso == "2026-11-15").Classe);
        Assert.DoesNotContain(v.Faixa, d => d.Classe.Contains("off"));
    }

    [Fact]
    public void Carnaval_JuntaOsDoisDias()
    {
        var v = Visao(F("Carnaval", 2027, 2, 8), F("Carnaval", 2027, 2, 9));

        Assert.Equal("Segunda e terça, 8 e 9 de fevereiro de 2027", v.DataTexto);
        Assert.Equal("8–9", v.DiaLista);
        Assert.Equal("fev", v.MesLista);
        Assert.Equal(4, v.Faixa.Count(d => d.Classe.StartsWith("off")));
    }

    [Fact]
    public void Json_UsaCamelCaseEnumComoTextoEEscapaHtml()
    {
        var v = Visao(F("</script><b>", 2026, 11, 20));

        var json = System.Text.Json.JsonSerializer.Serialize([v], FeriadoJsonContext.Default.ListFeriadoVisao);

        Assert.Contains("\"tipo\":\"Prolongado\"", json);
        Assert.Contains("\"inicioIso\":\"2026-11-20\"", json);
        Assert.Contains("\"faixa\":[{\"iso\":", json);
        Assert.DoesNotContain("</script>", json);
    }

    [Fact]
    public async Task Servico_NoSabadoDepoisDaSexta_MantemOFeriadoAteOFimDaFolga()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(Servico_NoSabadoDepoisDaSexta_MantemOFeriadoAteOFimDaFolga)).Options;
        await using var ctx = new AppDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);

        var analisados = await new HolidayService(ctx, time).GetProximosFeriadosAnalisadosAsync();

        Assert.Equal("Dia Nacional da Consciência Negra", analisados[0].Nome);
    }
}
