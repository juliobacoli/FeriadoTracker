using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayService(AppDbContext context, TimeProvider timeProvider) : IHolidayService
{
    public async Task<Feriado?> GetProximoFeriadoAsync()
    {
        var hoje = timeProvider.GetLocalNow().Date;
        return await context.Feriados
            .Where(f => f.Data >= hoje)
            .OrderBy(f => f.Data)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Feriado>> GetProximosFeriadosAsync()
    {
        var hoje = timeProvider.GetLocalNow().Date;

        return await context.Feriados
            .Where(f => f.Data >= hoje)
            .OrderBy(f => f.Data)
            .ToListAsync();
    }
}
