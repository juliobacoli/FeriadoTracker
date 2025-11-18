using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayService
{
    private readonly AppDbContext _context;

    public HolidayService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Feriado?> GetProximoFeriadoAsync()
    {
        var hoje = DateTime.Today;
        return await _context.Feriados
            .Where(f => f.Data >= hoje)
            .OrderBy(f => f.Data)
            .FirstOrDefaultAsync();
    }
}
