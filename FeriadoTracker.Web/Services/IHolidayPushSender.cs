using FeriadoTracker.Web.Dtos;

namespace FeriadoTracker.Web.Services;

public interface IHolidayPushSender
{
    Task<PushSendResult> SendDailyAsync(CancellationToken cancellationToken = default);
}
