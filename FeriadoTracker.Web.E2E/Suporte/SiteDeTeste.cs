using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FeriadoTracker.Web.E2E.Suporte;

/// <summary>
/// O site em modo Production, com banco SQLite e chaves numa pasta temporária própria,
/// apagada no fim. Configurações extras (ex.: limites de uso) entram como variáveis do site.
/// </summary>
internal sealed class SiteDeTeste(IReadOnlyDictionary<string, string>? configuracoes = null) : WebApplicationFactory<global::Program>
{
    private readonly string _pastaDados = Path.Combine(Path.GetTempPath(), "feriadotracker-e2e-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production: liga a CSP e o tratamento de erro como no site publicado.
        builder.UseEnvironment("Production");
        // Fora de Development, os arquivos de wwwroot só vêm do publish; aqui vêm direto do projeto.
        builder.UseStaticWebAssets();
        builder.UseSetting("DB_PATH", Path.Combine(_pastaDados, "feriados.db"));
        builder.UseSetting("DATA_PROTECTION_KEYS_PATH", Path.Combine(_pastaDados, "keys"));
        foreach (var (chave, valor) in configuracoes ?? new Dictionary<string, string>())
            builder.UseSetting(chave, valor);

        builder.ConfigureServices(services =>
        {
            // O envio agendado de push não participa dos testes.
            foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(HolidayNotificationService)).ToList())
                services.Remove(d);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_pastaDados, recursive: true); } catch (IOException) { }
    }
}
