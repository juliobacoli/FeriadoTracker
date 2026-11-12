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
        Assert.Equal("sexta · 3 dias de folga", v.ImpactoCurto);
        Assert.False(v.FolgaLonga);
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
        Assert.Equal("quinta · 4 dias com a ponte", v.ImpactoCurto);
        Assert.Equal("ponte", v.Faixa.Single(d => d.Iso == "2027-05-28").Classe);
        Assert.Contains("ponte possível", v.Faixa.Single(d => d.Iso == "2027-05-28").Rotulo);

        // A barra tem os 4 dias do texto: quinta, ponte na sexta e o fim de semana emendado.
        Assert.Equal(
            ["", "", "off holiday", "ponte", "emenda", "emenda", "", "", ""],
            v.Faixa.Select(d => d.Classe));
        Assert.Contains("folga", v.Faixa.Single(d => d.Iso == "2027-05-29").Rotulo);
    }

    [Fact]
    public void Terca_EmendaOFimDeSemanaAntesDaPonte()
    {
        var v = Visao(F("Finados", 2027, 11, 2));

        Assert.Equal("2027-10-28", v.Faixa[0].Iso);
        Assert.Equal(
            ["", "", "emenda", "emenda", "ponte", "off holiday", "", "", ""],
            v.Faixa.Select(d => d.Classe));
    }

    [Fact]
    public void Domingo_NaoViraFolga()
    {
        var v = Visao(F("Proclamação da República", 2026, 11, 15));

        Assert.Equal("Cai num domingo. Não vira folga.", v.Impacto);
        Assert.Equal("domingo · não vira folga", v.ImpactoCurto);
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
        Assert.Equal("segunda · 4 dias de folga", v.ImpactoCurto);
        Assert.True(v.FolgaLonga);
        Assert.Equal(4, v.Faixa.Count(d => d.Classe.StartsWith("off")));
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

    [Fact]
    public void Quarta_FolgaIsoladaNoMeioDaSemana()
    {
        var v = Visao(F("Tiradentes", 2027, 4, 21));

        Assert.Equal(TipoFolga.MeioDaSemana, v.Tipo);
        Assert.Equal("Quarta-feira, 21 de abril de 2027", v.DataTexto);
        Assert.Equal("Folga no meio da semana.", v.Impacto);
        Assert.Equal("quarta · meio da semana", v.ImpactoCurto);
        Assert.Equal("Folga até quarta-feira, 21 de abril.", v.NotaNoDia);
        Assert.False(v.TemPonte);
        Assert.False(v.FolgaLonga);
        Assert.Equal("21", v.DiaLista);
        Assert.Equal("abr", v.MesLista);

        // Folga de um dia só: a faixa fica centrada nele (4 dias de cada lado).
        Assert.Equal("2027-04-17", v.Faixa[0].Iso);
        Assert.Equal(["", "", "", "", "off holiday", "", "", "", ""], v.Faixa.Select(d => d.Classe));
        Assert.Equal("quarta-feira 21, folga, feriado", v.Faixa[4].Rotulo);
        Assert.Equal("sábado 17", v.Faixa[0].Rotulo);
        Assert.Equal("qua", v.Faixa[4].DiaSemana);
        Assert.Equal(21, v.Faixa[4].Dia);
    }

    [Fact]
    public void Sabado_NaoViraFolga()
    {
        var v = Visao(F("Natal", 2027, 12, 25));

        Assert.Equal(TipoFolga.FimDeSemana, v.Tipo);
        Assert.Equal("Sábado, 25 de dezembro de 2027", v.DataTexto);
        Assert.Equal("Cai num sábado. Não vira folga.", v.Impacto);
        Assert.Equal("sábado · não vira folga", v.ImpactoCurto);
        Assert.Equal("Cai num sábado, então não vira folga extra.", v.NotaNoDia);
        Assert.False(v.TemPonte);
        Assert.False(v.FolgaLonga);
        Assert.Equal("2027-12-25", v.FolgaFimIso);

        Assert.Equal("weekend-only holiday", v.Faixa[4].Classe);
        Assert.Equal("sábado 25, feriado", v.Faixa[4].Rotulo);
        Assert.DoesNotContain(v.Faixa, d => d.Classe.Contains("off"));
    }

    [Fact]
    public void Segunda_FimDeSemanaProlongadoComDiasDeFolgaNaFaixa()
    {
        var v = Visao(F("Nossa Senhora Aparecida", 2026, 10, 12));

        Assert.Equal(TipoFolga.Prolongado, v.Tipo);
        Assert.Equal("Segunda-feira, 12 de outubro", v.DataTexto); // mesmo ano de "hoje": sem o ano no texto
        Assert.Equal("Fim de semana de 3 dias.", v.Impacto);
        Assert.Equal("segunda · 3 dias de folga", v.ImpactoCurto);
        Assert.Equal("Folga até segunda-feira, 12 de outubro.", v.NotaNoDia);
        Assert.False(v.FolgaLonga); // 3 dias ainda não é "longa" (a partir de 4)
        Assert.Equal("12", v.DiaLista);
        Assert.Equal("out", v.MesLista);
        Assert.Equal("2026-10-12", v.FolgaFimIso);

        // Sábado e domingo entram como folga; só a segunda é feriado.
        Assert.Equal("2026-10-07", v.Faixa[0].Iso);
        Assert.Equal(["", "", "", "off", "off", "off holiday", "", "", ""], v.Faixa.Select(d => d.Classe));
        Assert.Equal("sábado 10, folga", v.Faixa[3].Rotulo);
        Assert.Equal("domingo 11, folga", v.Faixa[4].Rotulo);
        Assert.Equal("segunda-feira 12, folga, feriado", v.Faixa[5].Rotulo);
    }

    [Fact]
    public void CamposBasicos_RefletemOFeriadoAnalisado()
    {
        var sexta = Visao(F("Consciência Negra", 2026, 11, 20));
        Assert.Equal("Consciência Negra", sexta.Nome);
        Assert.Equal(TipoFolga.Prolongado, sexta.Tipo);
        Assert.Equal("2026-11-20", sexta.InicioIso);
        Assert.Equal("2026-11-20", sexta.FimIso);
        Assert.Equal("2026-11-22", sexta.FolgaFimIso);
        Assert.Equal(2026, sexta.Ano);

        var carnaval = Visao(F("Carnaval", 2027, 2, 8), F("Carnaval", 2027, 2, 9));
        Assert.Equal("Carnaval", carnaval.Nome);
        Assert.Equal("2027-02-08", carnaval.InicioIso);
        Assert.Equal("2027-02-09", carnaval.FimIso);
        Assert.Equal("2027-02-09", carnaval.FolgaFimIso);
        Assert.Equal(2027, carnaval.Ano);

        var ponte = Visao(F("Corpus Christi", 2027, 5, 27));
        Assert.Equal(TipoFolga.Ponte, ponte.Tipo);
        Assert.Equal("2027-05-27", ponte.InicioIso); // mês e dia com zero à esquerda
        Assert.Equal("2027-05-27", ponte.FolgaFimIso);
        Assert.Equal(2027, ponte.Ano);
    }

    [Fact]
    public void DataTexto_SoMostraOAnoQuandoDiferenteDoDeHoje()
    {
        var analisado = Assert.Single(FolgaCalculator.Analisar([F("Confraternização Universal", 2027, 1, 1)]));

        var naVespera = FeriadoVisaoBuilder.Criar(analisado, new DateOnly(2026, 12, 31));
        var noDia = FeriadoVisaoBuilder.Criar(analisado, new DateOnly(2027, 1, 1));

        Assert.Equal("Sexta-feira, 1 de janeiro de 2027", naVespera.DataTexto);
        Assert.Equal("Sexta-feira, 1 de janeiro", noDia.DataTexto);
    }

    [Fact]
    public void Faixa_TemSempreNoveDiasSeguidosEIncluiOFeriado()
    {
        var casos = new[]
        {
            Visao(F("Consciência Negra", 2026, 11, 20)),           // sexta (prolongado)
            Visao(F("Nossa Senhora Aparecida", 2026, 10, 12)),     // segunda (prolongado)
            Visao(F("Corpus Christi", 2027, 5, 27)),               // quinta (ponte depois)
            Visao(F("Finados", 2027, 11, 2)),                      // terça (ponte antes)
            Visao(F("Tiradentes", 2027, 4, 21)),                   // quarta (meio da semana)
            Visao(F("Proclamação da República", 2026, 11, 15)),    // domingo (fim de semana)
            Visao(F("Natal", 2027, 12, 25)),                       // sábado (fim de semana)
            Visao(F("Carnaval", 2027, 2, 8), F("Carnaval", 2027, 2, 9)),
        };

        foreach (var v in casos)
        {
            Assert.Equal(FeriadoVisaoBuilder.DiasNaFaixa, v.Faixa.Count);

            var primeiro = DateOnly.ParseExact(v.Faixa[0].Iso, "yyyy-MM-dd");
            for (var k = 0; k < v.Faixa.Count; k++)
            {
                var dia = primeiro.AddDays(k);
                Assert.Equal(dia.ToString("yyyy-MM-dd"), v.Faixa[k].Iso);
                Assert.Equal(dia.Day, v.Faixa[k].Dia);
                Assert.Equal(PtBrFormat.DiaDaSemanaCurto(dia), v.Faixa[k].DiaSemana);
            }

            Assert.Contains(v.Faixa, d => d.Iso == v.InicioIso && d.Classe.Contains("holiday"));
        }
    }

    [Fact]
    public void Faixa_ComFolgaMaiorQueANoveDias_MostraOsPrimeirosNoveDiasDaFolga()
    {
        // Feriados com nomes diferentes em dias seguidos (2 a 6 e 9 a 13 de novembro) formam uma folga de 16 dias.
        var feriados = new List<Feriado>();
        foreach (var dia in new[] { 2, 3, 4, 5, 6, 9, 10, 11, 12, 13 })
            feriados.Add(F($"Feriado {dia}", 2026, 11, dia));

        var primeiro = FeriadoVisaoBuilder.Criar(FolgaCalculator.Analisar(feriados)[0], Hoje);

        Assert.Equal(TipoFolga.Prolongado, primeiro.Tipo);
        Assert.Equal(9, primeiro.Faixa.Count);
        Assert.Equal("2026-10-31", primeiro.Faixa[0].Iso); // começa no início da folga: sem centralizar
        Assert.Equal("2026-11-08", primeiro.Faixa[8].Iso);
        Assert.All(primeiro.Faixa, d => Assert.StartsWith("off", d.Classe));
        // "holiday" marca só o feriado analisado (2 de novembro); os outros dias são folga de feriados vizinhos.
        Assert.Equal("off holiday", primeiro.Faixa.Single(d => d.Iso == "2026-11-02").Classe);
        Assert.Equal(8, primeiro.Faixa.Count(d => d.Classe == "off"));
    }
}
