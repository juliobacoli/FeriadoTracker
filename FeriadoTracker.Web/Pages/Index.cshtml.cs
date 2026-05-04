using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeriadoTracker.Web.Pages;

public class IndexModel(IHolidayService service) : PageModel
{
    public Feriado? ProximoFeriado { get; set; }
    public List<Feriado> ProximosFeriados { get; set; } = new();

    public async Task OnGetAsync()
    {
        ProximoFeriado = await service.GetProximoFeriadoAsync();
        ProximosFeriados = await service.GetProximosFeriadosAsync();
    }
}