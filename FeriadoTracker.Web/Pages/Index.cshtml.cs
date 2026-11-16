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

    /// <summary>Período coberto pela lista. Ex.: "de novembro de 2026 a dezembro de 2027".</summary>
    public string PeriodoLista { get; private set; } = "";

    /// <summary>Posição do destaque em <see cref="Feriados"/>; -1 sem destaque.</summary>
    public int IndiceDestaque { get; private set; } = -1;

    /// <summary>Dias completos até o destaque (como o relógio do JS conta), para a primeira pintura.</summary>
    public int DiasAteDestaque { get; private set; }

    /// <summary>Códigos IBGE vindos de ?local=32&amp;local=26; os fora da lista liberada são ignorados.</summary>
    [BindProperty(SupportsGet = true, Name = "local")]
    public int[] CodigosLocais { get; set; } = [];

    /// <summary>Estados e cidades marcados; vazio = só nacionais.</summary>
    public List<LocalLiberado> Locais { get; private set; } = [];

    /// <summary>Título da lista: "Feriados nacionais", "Feriados no Espírito Santo" ou "... e de várias localidades".</summary>
    public string TituloLista => Locais.Count switch
    {
        0 => "Feriados nacionais",
        1 => $"Feriados {Locais[0].Local}",
        _ => "Feriados nacionais e de várias localidades"
    };

    public async Task OnGetAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        Locais = LocaisLiberados.Filtrar(CodigosLocais);
        ViewData["Title"] = Locais.Count == 1
            ? $"Feriados {Locais[0].Local} em {hoje.Year}"
            : $"Feriados prolongados de {hoje.Year}";
        var analisados = await service.GetProximosFeriadosAnalisadosAsync(Locais, ct);

        Feriados = analisados.Select(a => FeriadoVisaoBuilder.Criar(a, hoje)).ToList();

        if (analisados.Count > 0)
        {
            var (de, ate) = (analisados[0].Inicio, analisados[^1].Inicio);
            PeriodoLista = (de.Year, de.Month) == (ate.Year, ate.Month)
                ? $"{PtBrFormat.Mes(ate)} de {ate.Year}"
                : de.Year == ate.Year
                    ? $"de {PtBrFormat.Mes(de)} a {PtBrFormat.Mes(ate)} de {ate.Year}"
                    : $"de {PtBrFormat.Mes(de)} de {de.Year} a {PtBrFormat.Mes(ate)} de {ate.Year}";
        }

        var proxima = FolgaCalculator.ProximaFolga(analisados);
        if (proxima is not null)
        {
            IndiceDestaque = analisados.IndexOf(proxima);
            Destaque = Feriados[IndiceDestaque];
            var faltam = proxima.Inicio.ToDateTime(TimeOnly.MinValue) - timeProvider.GetLocalNow().DateTime;
            DiasAteDestaque = Math.Max(0, (int)Math.Floor(faltam.TotalDays));
        }
    }
}
