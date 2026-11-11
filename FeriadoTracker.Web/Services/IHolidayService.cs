namespace FeriadoTracker.Web.Services;

public interface IHolidayService
{
    Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync(CancellationToken ct = default);
}
