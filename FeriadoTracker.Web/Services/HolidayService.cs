using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayService(AppDbContext context, TimeProvider timeProvider) : IHolidayService
{
    public async Task<Feriado?> GetProximoFeriadoAsync()
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        return await context.Feriados
            .AsNoTracking()
            .Where(f => f.Data >= hoje)
            .OrderBy(f => f.Data)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Feriado>> GetProximosFeriadosAsync()
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);

        return await context.Feriados
            .AsNoTracking()
            .Where(f => f.Data >= hoje)
            .OrderBy(f => f.Data)
            .ToListAsync();
    }

    public async Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync()
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);

        // Uma semana para trás cobre feriados recentes que ainda estendem a folga de hoje.
        var feriados = await context.Feriados
            .AsNoTracking()
            .Where(f => f.Data >= hoje.AddDays(-7))
            .ToListAsync();

        return FolgaCalculator.Analisar(feriados)
            .Where(f => f.Fim >= hoje)
            .ToList();
    }
}
