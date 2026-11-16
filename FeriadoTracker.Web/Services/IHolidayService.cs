namespace FeriadoTracker.Web.Services;

public interface IHolidayService
{
    /// <param name="locais">Estados e cidades marcados; vazio ou nulo = só nacionais.</param>
    Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync(
        IReadOnlyCollection<LocalLiberado>? locais = null, CancellationToken ct = default);
}
