using FeriadoTracker.Web.E2E.Suporte;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(FeriadoTracker.Web.E2E.AppFixture))]

namespace FeriadoTracker.Web.E2E;

/// <summary>
/// Sobe o site uma vez por execução, em Kestrel numa porta livre, com banco SQLite temporário,
/// e abre o navegador escolhido pela variável BROWSER (chromium, webkit ou firefox; padrão chromium).
/// HEADED=1 mostra a janela do navegador.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private SiteDeTeste? _site;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = "";

    /// <summary>Serviços do site em execução (ex.: abrir o AppDbContext para conferir o que foi salvo).</summary>
    public IServiceProvider Services => _site!.Services;
    public IBrowser Browser { get; private set; } = null!;
    public string NomeBrowser { get; } = (Environment.GetEnvironmentVariable("BROWSER") ?? "chromium").ToLowerInvariant();

    public async ValueTask InitializeAsync()
    {
        // Cada execução começa sem artefatos antigos: a pasta mostra só as falhas desta rodada.
        try { Directory.Delete(Path.Combine(E2ETest.PastaRaiz, NomeBrowser), recursive: true); } catch (DirectoryNotFoundException) { }

        _site = new SiteDeTeste();
        _site.UseKestrel(0);
        _site.StartServer();
        BaseUrl = _site.ClientOptions.BaseAddress.ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        var tipo = NomeBrowser switch
        {
            "webkit" => _playwright.Webkit,
            "firefox" => _playwright.Firefox,
            _ => _playwright.Chromium,
        };
        Browser = await tipo.LaunchAsync(new()
        {
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        _playwright?.Dispose();
        if (_site is not null) await _site.DisposeAsync();
    }
}
