namespace FeriadoTracker.Web.Services;

public class HolidayNotificationService : BackgroundService
{
    private const int RunHour = 8;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<HolidayNotificationService> _logger;

    public HolidayNotificationService(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        ILogger<HolidayNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = ComputeNextRun();
            var delay = nextRun - _time.GetLocalNow();

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IHolidayPushSender>();
                await sender.SendDailyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no envio de notificações de feriados.");
            }
        }
    }

    private DateTimeOffset ComputeNextRun()
    {
        var now = _time.GetLocalNow();
        var todayAtRunHour = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            RunHour, 0, 0, now.Offset);

        return now < todayAtRunHour ? todayAtRunHour : todayAtRunHour.AddDays(1);
    }
}
