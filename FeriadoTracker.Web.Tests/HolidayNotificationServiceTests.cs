using FeriadoTracker.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class HolidayNotificationServiceTests
{
    private static HolidayNotificationService CreateService(FakeTimeProvider time)
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new HolidayNotificationService(
            scopeFactory,
            time,
            NullLogger<HolidayNotificationService>.Instance
        );
    }

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoAntesDas8h()
    {
        // 7:59
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 7, 59, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoExatamenteAs8h()
    {
        // 8:00
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoDepoisDas8h()
    {
        // 8:01
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 1, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }
}
