namespace FeriadoTracker.Web.Services;

public interface IWebPushClient
{
    Task<PushSendOutcome> SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        string payload,
        CancellationToken ct = default);
}

public enum PushSendOutcome
{
    Success,
    Gone,
    Failed
}
