using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace FeriadoTracker.Web.E2E.Suporte;

/// <summary>
/// Responde /api/push/* no lugar do servidor e guarda cada chamada, na ordem.
/// Isola o front: o rate limit do servidor é global e travaria testes seguidos.
/// </summary>
public sealed class ApiPushFalsa
{
    // Chave pública VAPID válida em base64url: o push.js converte com atob antes de inscrever.
    private const string ChaveVapid = "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U";

    private readonly List<Chamada> _chamadas = [];
    private readonly Dictionary<string, int> _status = [];
    private readonly Dictionary<string, int> _atrasoMs = [];
    private readonly Stopwatch _relogio = Stopwatch.StartNew();

    /// <summary>Uma chamada recebida. Inicio e Fim em ms desde a instalação (Fim: quando a resposta saiu).</summary>
    public sealed class Chamada(string metodo, string caminho, JsonElement? corpo, long inicio)
    {
        public string Metodo { get; } = metodo;
        public string Caminho { get; } = caminho;
        public JsonElement? Corpo { get; } = corpo;
        public long Inicio { get; } = inicio;
        public long? Fim { get; internal set; }
    }

    public IReadOnlyList<Chamada> Chamadas { get { lock (_chamadas) return [.. _chamadas]; } }

    public IReadOnlyList<Chamada> Puts => Chamadas.Where(c => c.Metodo == "PUT" && c.Caminho == "/api/push/locais").ToList();

    /// <summary>Faz o caminho (ex.: "/api/push/subscribe") responder com este status.</summary>
    public void Responder(string caminho, int status) => _status[caminho] = status;

    /// <summary>Segura a resposta do caminho por este tempo.</summary>
    public void Atrasar(string caminho, int ms) => _atrasoMs[caminho] = ms;

    public static async Task<ApiPushFalsa> InstalarAsync(IPage page)
    {
        var api = new ApiPushFalsa();
        await page.RouteAsync("**/api/push/**", async route =>
        {
            var req = route.Request;
            var caminho = new Uri(req.Url).AbsolutePath;
            JsonElement? corpo = req.PostData is { Length: > 0 } texto ? JsonDocument.Parse(texto).RootElement.Clone() : null;
            var chamada = new Chamada(req.Method, caminho, corpo, api._relogio.ElapsedMilliseconds);
            lock (api._chamadas) api._chamadas.Add(chamada);

            if (api._atrasoMs.TryGetValue(caminho, out var atraso)) await Task.Delay(atraso);

            var status = api._status.GetValueOrDefault(caminho, 200);
            var json = caminho.EndsWith("vapid-public-key") ? JsonSerializer.Serialize(new { publicKey = ChaveVapid }) : "{}";
            chamada.Fim = api._relogio.ElapsedMilliseconds;
            await route.FulfillAsync(new() { Status = status, ContentType = "application/json", Body = json });
        });
        return api;
    }

    /// <summary>Espera até a condição valer (ou falha depois do limite).</summary>
    public async Task EsperarAsync(Func<ApiPushFalsa, bool> condicao, int limiteMs = 5000)
    {
        var fim = DateTime.UtcNow.AddMilliseconds(limiteMs);
        while (!condicao(this))
        {
            if (DateTime.UtcNow > fim) throw new TimeoutException("Chamadas recebidas: " + string.Join(", ", Chamadas.Select(c => $"{c.Metodo} {c.Caminho}")));
            await Task.Delay(50);
        }
    }

    public static int[] Locais(Chamada put) => put.Corpo!.Value.GetProperty("locais").EnumerateArray().Select(e => e.GetInt32()).ToArray();
}
