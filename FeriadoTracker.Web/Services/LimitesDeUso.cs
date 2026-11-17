using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace FeriadoTracker.Web.Services;

/// <summary>
/// Limites de uso da API: uma cota por visitante (IP) em cada política e um teto global
/// por segundo para toda a /api, que protege o servidor (0,5 vCPU no Railway aguenta ~200 req/s
/// no PUT de locais; o teto padrão fica em ~70% disso).
/// Todos os valores podem ser trocados por variável de ambiente, sem mudar código.
/// </summary>
public static class LimitesDeUso
{
    public const string Push = "push";
    public const string Locais = "locais";
    public const string Avaliacao = "avaliacao";

    /// <param name="Push">RATE_LIMIT_PUSH: inscrever, desinscrever e chave VAPID, por IP por minuto.</param>
    /// <param name="Locais">RATE_LIMIT_LOCAIS: PUT de estados e cidades (pílulas), por IP por minuto.</param>
    /// <param name="Avaliacao">RATE_LIMIT_AVALIACAO: envio de avaliação, por IP por minuto.</param>
    /// <param name="ApiPorSegundo">RATE_LIMIT_API_POR_SEGUNDO: teto de toda a /api somando todos os visitantes.</param>
    public sealed record Valores(int Push, int Locais, int Avaliacao, int ApiPorSegundo)
    {
        public static Valores De(IConfiguration config) => new(
            config.GetValue("RATE_LIMIT_PUSH", 10),
            config.GetValue("RATE_LIMIT_LOCAIS", 30),
            config.GetValue("RATE_LIMIT_AVALIACAO", 5),
            config.GetValue("RATE_LIMIT_API_POR_SEGUNDO", 140));
    }

    public static IServiceCollection AddLimitesDeUso(this IServiceCollection services, IConfiguration config)
    {
        var valores = Valores.De(config);

        // IP real do visitante: no Railway toda requisição chega pelo proxy, que acrescenta o IP
        // de quem chamou no fim do X-Forwarded-For. ForwardLimit = 1 lê só essa última entrada,
        // a que o proxy escreveu; o que o visitante mandar antes dela é ignorado.
        // O container não é acessível sem passar pelo proxy, por isso não há lista de proxies conhecidos.
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            o.ForwardLimit = 1;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (contexto, _) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    contexto.HttpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };

            // Teto global: só a /api; páginas e arquivos estáticos ficam de fora.
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                contexto.Request.Path.StartsWithSegments("/api")
                    ? RateLimitPartition.GetFixedWindowLimiter("api", _ => Janela(valores.ApiPorSegundo, TimeSpan.FromSeconds(1)))
                    : RateLimitPartition.GetNoLimiter("fora-da-api"));

            o.AddPolicy(Push, contexto => PorIp(contexto, valores.Push));
            o.AddPolicy(Locais, contexto => PorIp(contexto, valores.Locais));
            o.AddPolicy(Avaliacao, contexto => PorIp(contexto, valores.Avaliacao));
        });

        return services;
    }

    /// <summary>Uma cota por IP, renovada a cada minuto.</summary>
    private static RateLimitPartition<string> PorIp(HttpContext contexto, int porMinuto) =>
        RateLimitPartition.GetFixedWindowLimiter(Ip(contexto), _ => Janela(porMinuto, TimeSpan.FromMinutes(1)));

    private static FixedWindowRateLimiterOptions Janela(int permitidas, TimeSpan janela) => new()
    {
        PermitLimit = permitidas,
        Window = janela,
        QueueLimit = 0,
    };

    /// <summary>IPv4 inteiro; IPv6 agrupado por /64, que é o bloco que um provedor entrega a uma casa.</summary>
    internal static string Ip(HttpContext contexto)
    {
        var ip = contexto.Connection.RemoteIpAddress;
        if (ip is null) return "desconhecido";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) return ip.ToString();

        var bytes = ip.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }
}
