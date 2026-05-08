using FeriadoTracker.Web.Models;

namespace FeriadoTracker.Web.Services;

public interface IHolidayService
{
    Task<Feriado?> GetProximoFeriadoAsync();
    Task<List<Feriado>> GetProximosFeriadosAsync();
}
