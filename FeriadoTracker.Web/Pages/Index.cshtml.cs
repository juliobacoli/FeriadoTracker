using System.Text.Json;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace FeriadoTracker.Web.Pages;

[ResponseCache(Duration = 86400)]
public class IndexModel(IHolidayService service, TimeProvider timeProvider) : PageModel
{
    /// <summary>Próxima folga: primeiro feriado que não cai no fim de semana.</summary>
    public FeriadoVisao? Destaque { get; private set; }

    public List<FeriadoVisao> Feriados { get; private set; } = [];

    /// <summary>Posição do destaque em <see cref="Feriados"/>; -1 sem destaque.</summary>
    public int IndiceDestaque { get; private set; } = -1;

    /// <summary>Dias de calendário até o destaque, para a primeira pintura antes do JS.</summary>
    public int DiasAteDestaque { get; private set; }

    /// <summary>Feriados em JSON para o cliente trocar o destaque ao clicar na lista.</summary>
    public string FeriadosJson => JsonSerializer.Serialize(Feriados, FeriadoJsonContext.Default.ListFeriadoVisao);

    public async Task OnGetAsync()
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        var analisados = await service.GetProximosFeriadosAnalisadosAsync();

        Feriados = analisados.Select(a => FeriadoVisaoBuilder.Criar(a, hoje)).ToList();

        var proxima = FolgaCalculator.ProximaFolga(analisados);
        if (proxima is not null)
        {
            IndiceDestaque = analisados.IndexOf(proxima);
            Destaque = Feriados[IndiceDestaque];
            DiasAteDestaque = Math.Max(0, proxima.Inicio.DayNumber - hoje.DayNumber);
        }
    }
}
