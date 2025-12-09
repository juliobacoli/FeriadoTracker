using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FeriadoTracker.Web.Pages;

public class IndexModel : PageModel
{
    private readonly HolidayService _service;

    public Feriado? ProximoFeriado { get; set; }
    public List<Feriado> ProximosFeriados { get; set; } = new();


    public IndexModel(HolidayService service)
    {
        _service = service;
    }

    public async Task OnGetAsync()
    {
        ProximoFeriado = await _service.GetProximoFeriadoAsync();
        ProximosFeriados = await _service.GetProximosFeriadosAsync();
    }
}