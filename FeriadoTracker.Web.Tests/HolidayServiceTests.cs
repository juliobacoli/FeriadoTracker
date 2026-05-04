using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

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

    private static TimeProvider FakeTime(DateTime utcNow)
    {
        var mock = new Mock<TimeProvider>();
        mock.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(utcNow, TimeSpan.Zero));
        mock.Setup(t => t.LocalTimeZone).Returns(TimeZoneInfo.Utc);
        return mock.Object;
    }

    [Fact]
    public async Task GetProximoFeriadoAsync_RetornaFeriadoMaisProximoNoFuturo()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximoFeriadoAsync_RetornaFeriadoMaisProximoNoFuturo));
        ctx.Feriados.AddRange(
            new Feriado { Id = 1, Nome = "Passado", Data = new DateTime(2026, 1, 1), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "Próximo", Data = new DateTime(2026, 5, 10), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Futuro distante", Data = new DateTime(2026, 12, 25), Tipo = "Nacional" }
        );
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximoFeriadoAsync();

        Assert.NotNull(resultado);
        Assert.Equal("Próximo", resultado!.Nome);
    }

    [Fact]
    public async Task GetProximoFeriadoAsync_IncluiFeriadoDeHoje()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximoFeriadoAsync_IncluiFeriadoDeHoje));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Hoje", Data = new DateTime(2026, 5, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximoFeriadoAsync();

        Assert.NotNull(resultado);
        Assert.Equal("Hoje", resultado!.Nome);
    }

    [Fact]
    public async Task GetProximoFeriadoAsync_RetornaNullQuandoSemFeriadosFuturos()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximoFeriadoAsync_RetornaNullQuandoSemFeriadosFuturos));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Velho", Data = new DateTime(2025, 1, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximoFeriadoAsync();

        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetProximosFeriadosAsync_OrdenaPorDataERemovePassados()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAsync_OrdenaPorDataERemovePassados));
        ctx.Feriados.AddRange(
            new Feriado { Id = 1, Nome = "B", Data = new DateTime(2026, 9, 7), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "A", Data = new DateTime(2026, 5, 10), Tipo = "Nacional" },
            new Feriado { Id = 3, Nome = "Velho", Data = new DateTime(2025, 12, 25), Tipo = "Nacional" }
        );
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximosFeriadosAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("A", resultado[0].Nome);
        Assert.Equal("B", resultado[1].Nome);
    }

    [Fact]
    public async Task GetProximosFeriadosAsync_RetornaListaVaziaQuandoSemFeriadosFuturos()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximosFeriadosAsync_RetornaListaVaziaQuandoSemFeriadosFuturos));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Velho", Data = new DateTime(2024, 1, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 5, 1)));

        var resultado = await service.GetProximosFeriadosAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task GetProximoFeriadoAsync_RespeitaTimeZoneLocal()
    {
        // BRT 23:30 do dia 01/05 = UTC 02:30 do dia 02/05.
        // Sem TimeZone correto, .Date retornaria 02/05 e perderia o feriado de hoje.
        await using var ctx = CreateInMemoryContext(nameof(GetProximoFeriadoAsync_RespeitaTimeZoneLocal));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Trabalho", Data = new DateTime(2026, 5, 1), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var brt = TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
        var mock = new Mock<TimeProvider>();
        mock.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(2026, 5, 2, 2, 30, 0, TimeSpan.Zero));
        mock.Setup(t => t.LocalTimeZone).Returns(brt);

        var service = new HolidayService(ctx, mock.Object);
        var resultado = await service.GetProximoFeriadoAsync();

        Assert.NotNull(resultado);
        Assert.Equal("Trabalho", resultado!.Nome);
    }

    [Fact]
    public async Task GetProximoFeriadoAsync_RetornaPrimeiroQuandoMultiplosNoMesmoDia()
    {
        await using var ctx = CreateInMemoryContext(nameof(GetProximoFeriadoAsync_RetornaPrimeiroQuandoMultiplosNoMesmoDia));
        ctx.Feriados.AddRange(
            new Feriado { Id = 3, Nome = "Carnaval segunda", Data = new DateTime(2026, 2, 16), Tipo = "Nacional" },
            new Feriado { Id = 4, Nome = "Carnaval terça", Data = new DateTime(2026, 2, 17), Tipo = "Nacional" }
        );
        await ctx.SaveChangesAsync();

        var service = new HolidayService(ctx, FakeTime(new DateTime(2026, 2, 1)));
        var resultado = await service.GetProximoFeriadoAsync();

        Assert.NotNull(resultado);
        Assert.Equal(new DateTime(2026, 2, 16), resultado!.Data);
    }
}
