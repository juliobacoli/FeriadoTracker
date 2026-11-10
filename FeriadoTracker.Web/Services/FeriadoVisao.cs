namespace FeriadoTracker.Web.Services;

/// <summary>Um dia da faixa em volta do feriado.</summary>
/// <param name="Classe">Classes CSS: off (folga), ponte, emenda (folga que a ponte liga), weekend-only, holiday.</param>
public sealed record DiaFaixa(string Iso, string DiaSemana, int Dia, string Classe, string Rotulo);

/// <summary>Textos e faixa de dias prontos para exibir um feriado analisado.</summary>
public sealed record FeriadoVisao(
    string Nome,
    TipoFolga Tipo,
    string InicioIso,
    string FimIso,
    string FolgaFimIso,
    int Ano,
    string DataTexto,
    string Impacto,
    string ImpactoCurto,
    string NotaNoDia,
    bool TemPonte,
    bool FolgaLonga,
    string DiaLista,
    string MesLista,
    IReadOnlyList<DiaFaixa> Faixa);

public static class FeriadoVisaoBuilder
{
    public const int DiasNaFaixa = 9;

    public static FeriadoVisao Criar(FeriadoAnalisado a, DateOnly hoje)
    {
        var dataTexto = DataTexto(a);
        if (a.Inicio.Year != hoje.Year) dataTexto += $" de {a.Inicio.Year}";

        return new FeriadoVisao(
            a.Nome,
            a.Tipo,
            Iso(a.Inicio),
            Iso(a.Fim),
            Iso(a.FolgaFim),
            a.Inicio.Year,
            dataTexto,
            Impacto(a),
            ImpactoCurto(a),
            NotaNoDia(a),
            a.Ponte is not null,
            a.DiasDeFolga >= 4,
            a.Fim > a.Inicio ? $"{a.Inicio.Day}–{a.Fim.Day}" : a.Inicio.Day.ToString(),
            PtBrFormat.MesCurto(a.Inicio),
            Faixa(a));
    }

    private static string DataTexto(FeriadoAnalisado a)
    {
        if (a.Fim > a.Inicio)
        {
            var ini = PtBrFormat.DiaDaSemana(a.Inicio).Replace("-feira", "");
            var fim = PtBrFormat.DiaDaSemanaMinusculo(a.Fim).Replace("-feira", "");
            return $"{ini} e {fim}, {a.Inicio.Day} e {a.Fim.Day} de {PtBrFormat.Mes(a.Fim)}";
        }

        return $"{PtBrFormat.DiaDaSemana(a.Inicio)}, {a.Inicio.Day} de {PtBrFormat.Mes(a.Inicio)}";
    }

    public static string Impacto(FeriadoAnalisado a) => a.Tipo switch
    {
        TipoFolga.FimDeSemana => $"Cai num {PtBrFormat.DiaDaSemanaMinusculo(a.Inicio)}. Não vira folga.",
        TipoFolga.Prolongado => $"Fim de semana de {a.DiasDeFolga} dias.",
        TipoFolga.Ponte => $"Com a ponte na {PtBrFormat.DiaDaSemanaMinusculo(a.Ponte!.Value)}, são {a.DiasComPonte} dias de folga.",
        _ => "Folga no meio da semana."
    };

    private static string ImpactoCurto(FeriadoAnalisado a)
    {
        var dia = PtBrFormat.DiaDaSemanaMinusculo(a.Inicio).Replace("-feira", "");
        return a.Tipo switch
        {
            TipoFolga.FimDeSemana => $"{dia} · não vira folga",
            TipoFolga.Prolongado => $"{dia} · {a.DiasDeFolga} dias de folga",
            TipoFolga.Ponte => $"{dia} · {a.DiasComPonte} dias com a ponte",
            _ => $"{dia} · meio da semana"
        };
    }

    private static string NotaNoDia(FeriadoAnalisado a) => a.Tipo == TipoFolga.FimDeSemana
        ? $"Cai num {PtBrFormat.DiaDaSemanaMinusculo(a.Inicio)}, então não vira folga extra."
        : $"Folga até {PtBrFormat.DiaDaSemanaMinusculo(a.FolgaFim)}, {a.FolgaFim.Day} de {PtBrFormat.Mes(a.FolgaFim)}.";

    /// <summary>Nove dias centrados no período de folga (incluindo a ponte e o que ela emenda).</summary>
    private static List<DiaFaixa> Faixa(FeriadoAnalisado a)
    {
        // Com ponte, o período vai até o fim de semana que ela liga: são os DiasComPonte do texto.
        var inicio = a.Ponte is { } p1 && p1 < a.FolgaInicio ? a.FolgaFim.AddDays(-(a.DiasComPonte - 1)) : a.FolgaInicio;
        var fim = a.Ponte is { } p2 && p2 > a.FolgaFim ? a.FolgaInicio.AddDays(a.DiasComPonte - 1) : a.FolgaFim;
        var tamanho = Math.Min(fim.DayNumber - inicio.DayNumber + 1, DiasNaFaixa);
        var primeiro = inicio.AddDays(-((DiasNaFaixa - tamanho) / 2));

        var dias = new List<DiaFaixa>(DiasNaFaixa);
        for (var k = 0; k < DiasNaFaixa; k++)
        {
            var d = primeiro.AddDays(k);
            var folga = a.Tipo != TipoFolga.FimDeSemana && d >= a.FolgaInicio && d <= a.FolgaFim;
            var ponte = a.Ponte == d;
            var feriado = d >= a.Inicio && d <= a.Fim;
            var emenda = a.Ponte is not null && !folga && !ponte && d >= inicio && d <= fim;

            var classe = folga ? "off" : ponte ? "ponte" : emenda ? "emenda" : a.Tipo == TipoFolga.FimDeSemana && feriado ? "weekend-only" : "";
            if (feriado) classe = (classe + " holiday").Trim();

            var rotulo = $"{PtBrFormat.DiaDaSemanaMinusculo(d)} {d.Day}"
                + (folga || emenda ? ", folga" : "")
                + (ponte ? ", ponte possível" : "")
                + (feriado ? ", feriado" : "");

            dias.Add(new DiaFaixa(Iso(d), PtBrFormat.DiaDaSemanaCurto(d), d.Day, classe, rotulo));
        }

        return dias;
    }

    private static string Iso(DateOnly d) => $"{d.Year:0000}-{d.Month:00}-{d.Day:00}";
}
