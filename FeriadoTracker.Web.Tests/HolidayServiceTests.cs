using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class HolidayServiceTests
{
    private static AppDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static FakeTimeProvider FakeTime(DateTime utcNow, TimeZoneInfo? localZone = null)
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(utcNow, TimeSpan.Zero));
        provider.SetLocalTimeZone(localZone ?? TimeZoneInfo.Utc);
        return provider;
    }

    [Fact]
    public async Task GetProximosFeriadosAnalisadosAsync_OrdenaPorDataERemovePassados()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAnalisadosAsync_OrdenaPorDataERemovePassados));
        ctx.Feriados.AddRange(
            new Feriado { Id = 1, Nome = "B", Data = new DateOnly(2026, 9, 7), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "A", Data = new DateOnly(2026, 5, 10), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Velho", Data = new DateOnly(2025, 12, 25), Tipo = "Nacional" }
        );
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximosFeriadosAnalisadosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("A", resultado[0].Nome);
        Assert.Equal("B", resultado[1].Nome);
    }

    [Fact]
    public async Task GetProximosFeriadosAnalisadosAsync_IncluiFeriadoDeHoje()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAnalisadosAsync_IncluiFeriadoDeHoje));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Hoje", Data = new DateOnly(2026, 5, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximosFeriadosAnalisadosAsync();

        Assert.Equal("Hoje", Assert.Single(resultado).Nome);
    }

    [Fact]
    public async Task GetProximosFeriadosAnalisadosAsync_RetornaListaVaziaQuandoSemFeriadosFuturos()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAnalisadosAsync_RetornaListaVaziaQuandoSemFeriadosFuturos));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Velho", Data = new DateOnly(2024, 1, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximosFeriadosAnalisadosAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetProximosFeriadosAnalisadosAsync_RespeitaTimeZoneLocal()
    {
        // BRT 23:30 do dia 21/04 = UTC 02:30 do dia 22/04.
        // Feriado numa quarta: a folga acaba no próprio dia. Sem o fuso correto,
        // "hoje" seria 22/04 e o feriado de hoje sumiria da lista.
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAnalisadosAsync_RespeitaTimeZoneLocal));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Tiradentes", Data = new DateOnly(2027, 4, 21), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var brt = TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
        var time = FakeTime(new DateTime(2027, 4, 22, 2, 30, 0), brt);

        var resultado = await new HolidayService(ctx, time).GetProximosFeriadosAnalisadosAsync();

        Assert.Equal("Tiradentes", Assert.Single(resultado).Nome);
    }

    [Fact]
    public async Task GetProximosFeriadosAnalisadosAsync_RespeitaCancelamento()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAnalisadosAsync_RespeitaCancelamento));
        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetProximosFeriadosAnalisadosAsync(new CancellationToken(canceled: true)));
    }

    [Fact]
    public void BrazilTimeProvider_UsaTimeZoneSaoPaulo()
    {
        var provider = new BrazilTimeProvider();

        Assert.Equal(TimeSpan.FromHours(-3), provider.LocalTimeZone.BaseUtcOffset);
    }

    [Fact]
    public void BrazilTimeProvider_GetLocalNow_EstaTresHorasAtrasDoUtc()
    {
        var provider = new BrazilTimeProvider();

        var local = provider.GetLocalNow();

        Assert.Equal(TimeSpan.FromHours(-3), local.Offset);
        Assert.Equal(provider.GetUtcNow().UtcDateTime, local.UtcDateTime, TimeSpan.FromSeconds(5));
    }

    // O serviço de notificações passou a usar Task.Delay(delay, time, ct). Em produção "time" é o
    // BrazilTimeProvider: estes testes garantem que a espera e o cancelamento seguem funcionando com ele.
    [Fact]
    public async Task BrazilTimeProvider_TaskDelay_CompletaAposAEspera()
    {
        var provider = new BrazilTimeProvider();
        var inicio = provider.GetTimestamp();

        await Task.Delay(TimeSpan.FromMilliseconds(100), provider, CancellationToken.None);

        Assert.True(provider.GetElapsedTime(inicio) >= TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public async Task BrazilTimeProvider_TaskDelay_CancelaComTaskCanceledException()
    {
        var provider = new BrazilTimeProvider();
        using var cts = new CancellationTokenSource();

        var espera = Task.Delay(TimeSpan.FromHours(1), provider, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => espera);
    }
}
