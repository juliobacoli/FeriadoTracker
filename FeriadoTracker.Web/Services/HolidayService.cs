using FeriadoTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayService(AppDbContext context, TimeProvider timeProvider) : IHolidayService
{
    public async Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync(CancellationToken ct = default)
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);

        // Uma semana para trás cobre feriados recentes que ainda estendem a folga de hoje.
        var feriados = await context.Feriados
            .AsNoTracking()
            .Where(f => f.Data >= hoje.AddDays(-7))
            .ToListAsync(ct);

        // Mantém o feriado enquanto a folga dele não acabou (sábado depois de um feriado na sexta).
        return FolgaCalculator.Analisar(feriados)
            .Where(f => f.FolgaFim >= hoje)
            .ToList();
    }
}
