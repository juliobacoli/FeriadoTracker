using System.Net;
using System.Security.Cryptography;
using FeriadoTracker.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebPush;

namespace FeriadoTracker.Web.Tests;

public class VapidWebPushClientTests
{
    #region Infraestrutura de teste

    /// <summary>
    /// Handler fake que captura a request HTTP real montada pela lib WebPush,
    /// permitindo validar headers no wire (ex.: TTL) sem rede.
    /// </summary>
    private sealed class CapturingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private static IConfiguration BuildConfig(bool withVapid = true)
    {
        var keys = withVapid ? VapidHelper.GenerateVapidKeys() : null;
        var values = new Dictionary<string, string?>
        {
            ["WebPush:Subject"] = withVapid ? "mailto:teste@exemplo.com" : null,
            ["WebPush:VapidPublicKey"] = keys?.PublicKey,
            ["WebPush:VapidPrivateKey"] = keys?.PrivateKey
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Gera p256dh/auth válidos: a lib criptografa o payload com a chave pública
    /// da subscription, então o ponto EC precisa ser real.
    /// </summary>
    private static (string P256dh, string Auth) MakeSubscriptionKeys()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdh.ExportParameters(includePrivateParameters: false);

        var uncompressed = new byte[65];
        uncompressed[0] = 0x04;
        p.Q.X!.CopyTo(uncompressed, 1);
        p.Q.Y!.CopyTo(uncompressed, 33);

        return (Base64Url(uncompressed), Base64Url(RandomNumberGenerator.GetBytes(16)));
    }

    private static (VapidWebPushClient Client, CapturingHandler Handler) BuildClient(
        HttpStatusCode statusCode, IConfiguration? config = null)
    {
        var handler = new CapturingHandler(statusCode);
        var client = new VapidWebPushClient(
            config ?? BuildConfig(),
            NullLogger<VapidWebPushClient>.Instance,
            new HttpClient(handler));
        return (client, handler);
    }

    #endregion

    #region Testes de integração — atravessam a lib WebPush e o pipeline HTTP (handler fake)

    [Theory]
    [InlineData(3600)]
    [InlineData(86400)]
    [InlineData(432000)]
    public async Task SendAsync_EnviaHeaderTtlDinamico(int ttlSeconds)
    {
        var (client, handler) = BuildClient(HttpStatusCode.Created);
        var (p256dh, auth) = MakeSubscriptionKeys();

        var outcome = await client.SendAsync(
            "https://fcm.googleapis.com/fcm/send/teste", p256dh, auth, "{\"title\":\"x\"}", ttlSeconds);

        Assert.Equal(PushSendOutcome.Success, outcome);
        Assert.NotNull(handler.CapturedRequest);
        Assert.True(handler.CapturedRequest!.Headers.TryGetValues("TTL", out var values));
        Assert.Equal(ttlSeconds.ToString(), Assert.Single(values!));
    }

    [Theory]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task SendAsync_RetornaGoneQuandoSubscriptionExpirou(HttpStatusCode statusCode)
    {
        var (client, _) = BuildClient(statusCode);
        var (p256dh, auth) = MakeSubscriptionKeys();

        var outcome = await client.SendAsync(
            "https://fcm.googleapis.com/fcm/send/teste", p256dh, auth, "{}", 86400);

        Assert.Equal(PushSendOutcome.Gone, outcome);
    }

    #endregion

    #region Testes de unidade — curto-circuito antes da lib, sem HTTP

    [Fact]
    public async Task SendAsync_RetornaFailedSemChamarRedeQuandoVapidNaoConfigurado()
    {
        var (client, handler) = BuildClient(HttpStatusCode.Created, BuildConfig(withVapid: false));

        var outcome = await client.SendAsync("https://push/x", "p", "a", "{}", 86400);

        Assert.Equal(PushSendOutcome.Failed, outcome);
        Assert.Null(handler.CapturedRequest);
    }

    #endregion
}
