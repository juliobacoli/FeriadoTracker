using System.Collections.Concurrent;
using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

namespace FeriadoTracker.Web.E2E.Carga;

/// <summary>
/// Teste de carga do PUT /api/push/locais (pílulas de estados e cidades), com NBomber.
/// Roda contra um site já em execução (CARGA_URL), de preferência no Docker com o mesmo
/// limite de memória e CPU do Railway (--memory=500m --cpus=0.5). Todos os usuários virtuais saem
/// do mesmo IP, então os limites precisam estar altos para medir o servidor e não o limitador:
/// RATE_LIMIT_PUSH, RATE_LIMIT_LOCAIS e RATE_LIMIT_API_POR_SEGUNDO (ver LimitesDeUso).
/// A carga sobe em degraus de req/s; o teste falha se o p95 ou a taxa de falhas passarem do aceito.
/// Relatórios (HTML, Markdown, CSV e log) em TestResults/carga/.
/// </summary>
[Trait("Categoria", "Carga")]
public class PutLocaisCargaTests
{
    private const string NomeCenario = "put_locais";
    private static readonly int[] LocaisLiberados = [15, 26, 32, 35, 42, 3550308];

    [Fact(DisplayName = "Carga: PUT /api/push/locais em degraus de req/s")]
    public async Task PutLocais()
    {
        var cfg = ConfiguracaoCarga.DoAmbiente();
        Assert.SkipWhen(cfg.Url is null, "Defina CARGA_URL para rodar o teste de carga.");

        var usuarios = new ConcurrentDictionary<int, Lazy<Task<UsuarioVirtual>>>();
        try
        {
            var cenario = Scenario.Create(NomeCenario, contexto => ExecutarAsync(contexto, cfg.Url!, usuarios))
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(Degraus(cfg));

            // O NBomber roda de forma síncrona: fora da thread do teste para não bloqueá-la.
            var stats = await Task.Run(() => NBomberRunner
                .RegisterScenarios(cenario)
                .WithTestSuite("FeriadoTracker")
                .WithTestName(NomeCenario)
                .WithReportFolder(Path.Combine(E2ETest.PastaResultados, "carga"))
                .WithReportFormats(ReportFormat.Html, ReportFormat.Md, ReportFormat.Csv)
                .Run(), TestContext.Current.CancellationToken);

            var resultado = stats.ScenarioStats.Get(NomeCenario);
            var total = resultado.Ok.Request.Count + resultado.Fail.Request.Count;
            var falhasPct = total == 0 ? 100 : 100.0 * resultado.Fail.Request.Count / total;

            Assert.True(total > 0, "Nenhuma requisição foi feita.");
            Assert.True(falhasPct <= cfg.FalhasMaximasPct,
                $"Falhas: {falhasPct:F2}% (máximo {cfg.FalhasMaximasPct}%). Veja o relatório em TestResults/carga/.");
            Assert.True(resultado.Ok.Latency.Percent95 <= cfg.P95MaximoMs,
                $"p95: {resultado.Ok.Latency.Percent95:F1} ms (máximo {cfg.P95MaximoMs} ms). Veja o relatório em TestResults/carga/.");
        }
        finally
        {
            foreach (var preparo in usuarios.Values.Where(l => l.IsValueCreated).Select(l => l.Value))
            {
                if (preparo.IsCompletedSuccessfully) (await preparo).Dispose();
            }
        }
    }

    /// <summary>Uma requisição: prepara o usuário da cópia na primeira vez e troca a lista de locais.</summary>
    private static async Task<IResponse> ExecutarAsync(IScenarioContext contexto, Uri url,
        ConcurrentDictionary<int, Lazy<Task<UsuarioVirtual>>> usuarios)
    {
        var numero = contexto.ScenarioInfo.InstanceNumber;
        UsuarioVirtual usuario;
        try
        {
            usuario = await usuarios.GetOrAdd(numero, n => new Lazy<Task<UsuarioVirtual>>(() => UsuarioVirtual.PrepararAsync(url, n))).Value;
        }
        catch (Exception ex)
        {
            usuarios.TryRemove(numero, out _); // tenta preparar de novo na próxima vez
            return Response.Fail(statusCode: "preparo", message: ex.Message);
        }

        var locais = LocaisLiberados.Where(_ => Random.Shared.Next(2) == 0).ToArray();
        using var resposta = await usuario.Http.PutAsJsonAsync("/api/push/locais", new { endpoint = usuario.Endpoint, locais });

        var status = ((int)resposta.StatusCode).ToString();
        return resposta.IsSuccessStatusCode
            ? Response.Ok(statusCode: status)
            : Response.Fail(statusCode: status, message: resposta.ReasonPhrase ?? "");
    }

    /// <summary>Para cada degrau: sobe em 10 s e segura pelo tempo configurado.</summary>
    private static LoadSimulation[] Degraus(ConfiguracaoCarga cfg) => cfg.DegrausRps
        .SelectMany(rps => new[]
        {
            Simulation.RampingInject(rate: rps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.Inject(rate: rps, interval: TimeSpan.FromSeconds(1), during: cfg.DuracaoDegrau),
        })
        .ToArray();
}
