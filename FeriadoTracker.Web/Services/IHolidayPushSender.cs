namespace FeriadoTracker.Web.Services;

public interface IHolidayPushSender
{
    Task<PushSendResult> SendDailyAsync(CancellationToken cancellationToken = default);
}

public record PushSendResult(int Sent, int Removed, int Skipped);
