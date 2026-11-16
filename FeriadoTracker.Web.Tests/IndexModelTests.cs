using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Pages;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class IndexModelTests
{
    private sealed class ServicoFake : IHolidayService
    {
        public IReadOnlyCollection<LocalLiberado>? Recebidos { get; private set; }

        public Task<List<FeriadoAnalisado>> GetProximosFeriadosAnalisadosAsync(
            IReadOnlyCollection<LocalLiberado>? locais = null, CancellationToken ct = default)
        {
            Recebidos = locais;
            return Task.FromResult(FolgaCalculator.Analisar([new Feriado { Nome = "Tiradentes", Data = new DateOnly(2027, 4, 21) }]));
        }
    }

    private static IndexModel Model(IHolidayService servico, params int[] codigos)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2027, 4, 1, 12, 0, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);
        return new IndexModel(servico, time)
        {
            CodigosLocais = codigos,
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            }
        };
    }

    [Fact]
    public async Task OnGet_SemLocais_SoNacionais()
    {
        var servico = new ServicoFake();
        var model = Model(servico);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Empty(servico.Recebidos!);
        Assert.Equal("Feriados nacionais", model.TituloLista);
        Assert.Equal("Feriados prolongados de 2027", model.ViewData["Title"]);
    }

    [Fact]
    public async Task OnGet_UmLocal_TituloComONomeDoLocal()
    {
        var servico = new ServicoFake();
        var model = Model(servico, 32);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal([32], servico.Recebidos!.Select(l => l.Codigo));
        Assert.Equal("Feriados no Espírito Santo", model.TituloLista);
        Assert.Equal("Feriados no Espírito Santo em 2027", model.ViewData["Title"]);
    }

    [Fact]
    public async Task OnGet_VariosLocais_TituloVariasLocalidades()
    {
        var servico = new ServicoFake();
        var model = Model(servico, 26, 32, 32);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal([32, 26], servico.Recebidos!.Select(l => l.Codigo));
        Assert.Equal("Feriados nacionais e de várias localidades", model.TituloLista);
        Assert.Equal("Feriados prolongados de 2027", model.ViewData["Title"]);
    }

    [Fact]
    public async Task OnGet_IgnoraLocalNaoLiberado()
    {
        var servico = new ServicoFake();
        var model = Model(servico, 29);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Empty(model.Locais);
        Assert.Equal("Feriados nacionais", model.TituloLista);
    }

    [Fact]
    public async Task OnGet_MontaListaEDestaque()
    {
        var model = Model(new ServicoFake());

        await model.OnGetAsync(CancellationToken.None);

        Assert.Single(model.Feriados);
        Assert.Equal("Tiradentes", model.Destaque?.Nome);
        Assert.Equal(0, model.IndiceDestaque);
        Assert.Equal("abril de 2027", model.PeriodoLista);
        Assert.Equal(19, model.DiasAteDestaque);
    }

    [Theory]
    [InlineData(new int[0], "Só nacionais")]
    [InlineData(new[] { 32 }, "ES")]
    [InlineData(new[] { 32, 26 }, "Várias localidades")]
    public void Resumo_DoBotaoDoTopo(int[] codigos, string esperado)
    {
        Assert.Equal(esperado, LocaisLiberados.Resumo(LocaisLiberados.Filtrar(codigos)));
    }
}
