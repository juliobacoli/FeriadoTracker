using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private readonly string _pastaDados = Path.Combine(Path.GetTempPath(), "feriadotracker-e2e-" + Guid.NewGuid().ToString("N"));
    private Factory? _factory;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = "";

    /// <summary>Serviços do site em execução (ex.: abrir o AppDbContext para conferir o que foi salvo).</summary>
    public IServiceProvider Services => _factory!.Services;
    public IBrowser Browser { get; private set; } = null!;
    public string NomeBrowser { get; } = (Environment.GetEnvironmentVariable("BROWSER") ?? "chromium").ToLowerInvariant();

    public async ValueTask InitializeAsync()
    {
        // Cada execução começa sem artefatos antigos: a pasta mostra só as falhas desta rodada.
        try { Directory.Delete(Path.Combine(E2ETest.PastaRaiz, NomeBrowser), recursive: true); } catch (DirectoryNotFoundException) { }

        _factory = new Factory(_pastaDados);
        _factory.UseKestrel(0);
        _factory.StartServer();
        BaseUrl = _factory.ClientOptions.BaseAddress.ToString().TrimEnd('/');

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
        if (_factory is not null) await _factory.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_pastaDados, recursive: true); } catch (IOException) { }
    }

    private sealed class Factory(string pastaDados) : WebApplicationFactory<global::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Production: liga a CSP e o tratamento de erro como no site publicado.
            builder.UseEnvironment("Production");
            // Fora de Development, os arquivos de wwwroot só vêm do publish; aqui vêm direto do projeto.
            builder.UseStaticWebAssets();
            builder.UseSetting("DB_PATH", Path.Combine(pastaDados, "feriados.db"));
            builder.UseSetting("DATA_PROTECTION_KEYS_PATH", Path.Combine(pastaDados, "keys"));

            builder.ConfigureServices(services =>
            {
                // O envio agendado de push não participa dos testes de tela.
                services.RemoveAll<IHostedService>(d => d.ImplementationType == typeof(HolidayNotificationService));
            });
        }
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services, Func<ServiceDescriptor, bool> filtro)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T) && filtro(d)).ToList())
            services.Remove(d);
    }
}
