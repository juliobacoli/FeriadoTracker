namespace FeriadoTracker.Web.Services;

// Assume execução em uma única réplica. Multi-réplica enviaria push duplicado
// porque cada instância dispararia seu próprio scheduler. Para escalar, é
// necessário um lock distribuído (ex.: tabela de claim) ou job runner externo.
public class HolidayNotificationService(IServiceScopeFactory scopeFactory, TimeProvider time, ILogger<HolidayNotificationService> logger) : BackgroundService
{
    private const int RunHour = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset? ultimaExecucao = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = ComputeNextRun();

            // ComputeNextRun inclui o instante exato das 8h. Se o serviço acordar nesse instante,
            // ele devolveria o mesmo horário que acabou de rodar e reenviaria sem parar.
            if (ultimaExecucao is { } ultima && nextRun <= ultima)
            {
                nextRun = ultima.AddDays(1);
            }

            var delay = nextRun - time.GetLocalNow();

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, time, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            ultimaExecucao = nextRun;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IHolidayPushSender>();
                await sender.SendDailyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no envio de notificações de feriados.");
            }
        }
    }

    internal DateTimeOffset ComputeNextRun()
    {
        var now = time.GetLocalNow();
        var todayAtRunHour = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            RunHour, 0, 0, now.Offset);

        return now <= todayAtRunHour ? todayAtRunHour : todayAtRunHour.AddDays(1);
    }
}
