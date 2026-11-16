using FeriadoTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayService(AppDbContext context, TimeProvider timeProvider) : IHolidayService
{
    public async Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync(
        IReadOnlyCollection<LocalLiberado>? locais = null, CancellationToken ct = default)
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        locais ??= [];
        var estados = locais.Where(l => !l.EhMunicipio).Select(l => (int?)l.Codigo).ToList();
        var municipios = locais.Where(l => l.EhMunicipio).Select(l => (int?)l.Codigo).ToList();

        // Nacionais, mais os dos estados e cidades marcados (cada local traz só os seus).
        // Uma semana para trás cobre feriados recentes que ainda estendem a folga de hoje.
        var feriados = await context.Feriados
            .AsNoTracking()
            .Where(f => f.Data >= hoje.AddDays(-7)
                && (f.CodigoUf == null
                    || (f.CodigoMunicipio == null && estados.Contains(f.CodigoUf))
                    || municipios.Contains(f.CodigoMunicipio)))
            .ToListAsync(ct);

        // Mantém o feriado enquanto a folga dele não acabou (sábado depois de um feriado na sexta).
        return FolgaCalculator.Analisar(feriados)
            .Where(f => f.FolgaFim >= hoje)
            .ToList();
    }
}
