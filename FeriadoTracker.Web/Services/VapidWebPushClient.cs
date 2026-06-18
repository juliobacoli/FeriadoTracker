using WebPushSubscription = WebPush.PushSubscription;
using WebPushClientLib = WebPush.WebPushClient;
using VapidDetails = WebPush.VapidDetails;
using WebPushException = WebPush.WebPushException;

namespace FeriadoTracker.Web.Services;

public class VapidWebPushClient(
    IConfiguration config,
    ILogger<VapidWebPushClient> logger,
    HttpClient? httpClient = null) : IWebPushClient
{
    // HttpClient injetável para testes (captura da request real); produção usa o padrão.
    private readonly WebPushClientLib _client = httpClient is null ? new() : new(httpClient);

    public async Task<PushSendOutcome> SendAsync(string endpoint, string p256dh, string auth, string payload, int ttlSeconds, CancellationToken ct = default)
    {
        var publicKey = config["WebPush:VapidPublicKey"];
        var privateKey = config["WebPush:VapidPrivateKey"];
        var subject = config["WebPush:Subject"];

        if (string.IsNullOrWhiteSpace(publicKey)
            || string.IsNullOrWhiteSpace(privateKey)
            || string.IsNullOrWhiteSpace(subject))
        {
            return PushSendOutcome.Failed;
        }

        var subscription = new WebPushSubscription(endpoint, p256dh, auth);

        var options = new Dictionary<string, object>
        {
            ["vapidDetails"] = new VapidDetails(subject, publicKey, privateKey),
            ["TTL"] = ttlSeconds
        };

        try
        {
            await _client.SendNotificationAsync(subscription, payload, options, ct);
            return PushSendOutcome.Success;
        }
        catch (WebPushException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.Gone
            || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return PushSendOutcome.Gone;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar push para {Endpoint}.", endpoint);
            return PushSendOutcome.Failed;
        }
    }
}
