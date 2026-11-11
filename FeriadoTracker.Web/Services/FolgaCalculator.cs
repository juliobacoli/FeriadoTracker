using FeriadoTracker.Web.Models;

namespace FeriadoTracker.Web.Services;

public enum TipoFolga
{
    /// <summary>Cai em sábado ou domingo: não vira folga.</summary>
    FimDeSemana,
    /// <summary>Período de folga com 3 dias ou mais (sexta, segunda, Carnaval).</summary>
    Prolongado,
    /// <summary>Menos de 3 dias, mas um único dia útil o separa do fim de semana.</summary>
    Ponte,
    /// <summary>Folga isolada no meio da semana.</summary>
    MeioDaSemana
}

/// <param name="Inicio">Primeiro dia do feriado.</param>
/// <param name="Fim">Último dia do feriado (difere de Inicio no Carnaval).</param>
/// <param name="FolgaInicio">Início da sequência contínua de dias de folga que contém o feriado.</param>
/// <param name="FolgaFim">Fim dessa sequência.</param>
/// <param name="DiasDeFolga">Tamanho da sequência; 0 quando o feriado cai no fim de semana.</param>
/// <param name="Ponte">Dia útil que, emendado, liga a folga ao fim de semana.</param>
/// <param name="DiasComPonte">Tamanho da folga contando a ponte; igual a DiasDeFolga sem ponte.</param>
public sealed record FeriadoAnalisado(
    string Nome,
    DateOnly Inicio,
    DateOnly Fim,
    TipoFolga Tipo,
    DateOnly FolgaInicio,
    DateOnly FolgaFim,
    int DiasDeFolga,
    DateOnly? Ponte,
    int DiasComPonte);

/// <summary>
/// Regra de folga e ponte. Dia de folga = sábado, domingo ou feriado.
/// </summary>
public static class FolgaCalculator
{
    /// <summary>
    /// Agrupa dias seguidos com o mesmo nome num feriado só (Carnaval) e analisa cada um.
    /// A lista deve trazer também os feriados vizinhos, pois eles estendem o período de folga.
    /// </summary>
    public static List<FeriadoAnalisado> Analisar(IEnumerable<Feriado> feriados)
    {
        var ordenados = feriados.OrderBy(f => f.Data).ToList();
        var diasFeriado = new HashSet<DateOnly>(ordenados.Select(f => f.Data));
        var resultado = new List<FeriadoAnalisado>();

        for (var i = 0; i < ordenados.Count; i++)
        {
            var inicio = ordenados[i].Data;
            var fim = inicio;
            while (i + 1 < ordenados.Count
                && ordenados[i + 1].Nome == ordenados[i].Nome
                && ordenados[i + 1].Data == fim.AddDays(1))
            {
                i++;
                fim = ordenados[i].Data;
            }

            resultado.Add(Analisar(ordenados[i].Nome, inicio, fim, diasFeriado));
        }

        return resultado;
    }

    /// <summary>Primeiro feriado que vira folga de fato (ignora os de fim de semana).</summary>
    public static FeriadoAnalisado? ProximaFolga(IEnumerable<FeriadoAnalisado> analisados) =>
        analisados.FirstOrDefault(f => f.Tipo != TipoFolga.FimDeSemana);

    private static FeriadoAnalisado Analisar(string nome, DateOnly inicio, DateOnly fim, HashSet<DateOnly> diasFeriado)
    {
        var soFimDeSemana = true;
        for (var d = inicio; d <= fim; d = d.AddDays(1))
        {
            if (!FimDeSemana(d)) soFimDeSemana = false;
        }

        if (soFimDeSemana)
            return new FeriadoAnalisado(nome, inicio, fim, TipoFolga.FimDeSemana, inicio, fim, 0, null, 0);

        bool Folga(DateOnly d) => FimDeSemana(d) || diasFeriado.Contains(d);

        var folgaInicio = inicio;
        var folgaFim = fim;
        while (Folga(folgaInicio.AddDays(-1))) folgaInicio = folgaInicio.AddDays(-1);
        while (Folga(folgaFim.AddDays(1))) folgaFim = folgaFim.AddDays(1);

        var dias = folgaFim.DayNumber - folgaInicio.DayNumber + 1;

        DateOnly? ponte = null;
        var antes = folgaInicio.AddDays(-1);
        var depois = folgaFim.AddDays(1);
        if (dias < 3 && Folga(antes.AddDays(-1))) ponte = antes;
        else if (dias < 3 && Folga(depois.AddDays(1))) ponte = depois;

        var diasComPonte = dias;
        if (ponte is { } p)
        {
            var pi = folgaInicio;
            var pf = folgaFim;
            if (p < folgaInicio)
            {
                pi = p;
                while (Folga(pi.AddDays(-1))) pi = pi.AddDays(-1);
            }
            else
            {
                pf = p;
                while (Folga(pf.AddDays(1))) pf = pf.AddDays(1);
            }
            diasComPonte = pf.DayNumber - pi.DayNumber + 1;
        }

        var tipo = dias >= 3 ? TipoFolga.Prolongado : ponte is not null ? TipoFolga.Ponte : TipoFolga.MeioDaSemana;
        return new FeriadoAnalisado(nome, inicio, fim, tipo, folgaInicio, folgaFim, dias, ponte, diasComPonte);
    }

    private static bool FimDeSemana(DateOnly d) =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
