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

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoMeiaNoite()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoUmMinutoAntesDaMeiaNoite()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 23, 59, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Theory]
    [InlineData(2026, 4, 30, 2026, 5, 1)]   // virada de mês
    [InlineData(2026, 12, 31, 2027, 1, 1)]  // virada de ano
    public void ComputeNextRun_AtravessaViradaDeMesEAno_QuandoDepoisDas8h(
        int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(ano, mes, dia, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(anoEsperado, mesEsperado, diaEsperado, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Theory]
    [InlineData(2028, 2, 28, 2028, 2, 29)]  // 29/fev existe em 2028
    [InlineData(2028, 2, 29, 2028, 3, 1)]
    [InlineData(2026, 2, 28, 2026, 3, 1)]   // ano comum não tem 29/fev
    public void ComputeNextRun_RespeitaAnoBissexto_QuandoDepoisDas8h(
        int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(ano, mes, dia, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(anoEsperado, mesEsperado, diaEsperado, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_UsaHorarioLocalDoBrasil()
    {
        var brasilia = TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        // 10:59 UTC = 07:59 em Brasília (-03:00)
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 10, 59, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(brasilia);
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        // 08:00 em Brasília = 11:00 UTC, mesmo dia
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.FromHours(-3)), nextRun);
        Assert.Equal(TimeSpan.FromHours(-3), nextRun.Offset);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoPassaDeFracaoDeSegundoDas8h()
    {
        // 08:00:00,500: já passou das 8h por meio segundo
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 0, 0, 500, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }
}
