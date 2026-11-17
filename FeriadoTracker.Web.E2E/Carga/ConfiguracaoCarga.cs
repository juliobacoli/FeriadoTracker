using System.Globalization;

namespace FeriadoTracker.Web.E2E.Carga;

/// <summary>
/// Parâmetros do teste de carga, lidos de variáveis de ambiente.
/// Sem CARGA_URL o teste é ignorado: ele só roda quando chamado de propósito.
/// </summary>
public sealed record ConfiguracaoCarga(
    Uri? Url,
    int[] DegrausRps,
    TimeSpan DuracaoDegrau,
    double P95MaximoMs,
    double FalhasMaximasPct)
{
    /// <remarks>
    /// CARGA_URL: endereço do site sob teste (ex.: http://localhost:8080).<br/>
    /// CARGA_DEGRAUS: req/s de cada degrau, separados por vírgula. Padrão 25,50,100,200,400.<br/>
    /// CARGA_SEGUNDOS: quanto tempo a carga fica em cada degrau. Padrão 30.<br/>
    /// CARGA_P95_MS: p95 máximo aceito, em ms. Padrão 500.<br/>
    /// CARGA_FALHAS_PCT: porcentagem máxima de falhas aceita. Padrão 1.
    /// </remarks>
    public static ConfiguracaoCarga DoAmbiente()
    {
        var url = Environment.GetEnvironmentVariable("CARGA_URL");
        return new(
            string.IsNullOrWhiteSpace(url) ? null : new Uri(url),
            Ler("CARGA_DEGRAUS", "25,50,100,200,400")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => int.Parse(d, CultureInfo.InvariantCulture))
                .ToArray(),
            TimeSpan.FromSeconds(int.Parse(Ler("CARGA_SEGUNDOS", "30"), CultureInfo.InvariantCulture)),
            double.Parse(Ler("CARGA_P95_MS", "500"), CultureInfo.InvariantCulture),
            double.Parse(Ler("CARGA_FALHAS_PCT", "1"), CultureInfo.InvariantCulture));
    }

    private static string Ler(string nome, string padrao) =>
        Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor ? valor : padrao;
}
