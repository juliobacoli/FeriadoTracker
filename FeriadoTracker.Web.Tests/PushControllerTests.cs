using FeriadoTracker.Web.Controllers;
using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class PushControllerTests
{
    private static AppDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration BuildConfig(string? publicKey = "BLVN-key")
    {
        var values = new Dictionary<string, string?>
        {
            ["WebPush:VapidPublicKey"] = publicKey
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static FakeTimeProvider Time() =>
        new(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void GetVapidPublicKey_RetornaNotFoundQuandoSemConfig()
    {
        using var ctx = CreateContext(nameof(GetVapidPublicKey_RetornaNotFoundQuandoSemConfig));
        var controller = new PushController(ctx, Time(), BuildConfig(publicKey: null));

        var result = controller.GetVapidPublicKey();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetVapidPublicKey_RetornaOkComChave()
    {
        using var ctx = CreateContext(nameof(GetVapidPublicKey_RetornaOkComChave));
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = controller.GetVapidPublicKey();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Subscribe_RetornaBadRequestQuandoCamposVazios()
    {
        using var ctx = CreateContext(nameof(Subscribe_RetornaBadRequestQuandoCamposVazios));
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Subscribe(new PushSubscriptionDto("", "", ""), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await ctx.PushSubscriptions.CountAsync());
    }

    [Fact]
    public async Task Subscribe_PersisteNovaSubscription()
    {
        using var ctx = CreateContext(nameof(Subscribe_PersisteNovaSubscription));
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Subscribe(
            new PushSubscriptionDto("https://push/a", "p256", "auth"),
            CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var sub = await ctx.PushSubscriptions.SingleAsync();
        Assert.Equal("https://push/a", sub.Endpoint);
        Assert.Equal("p256", sub.P256dh);
    }

    [Fact]
    public async Task Subscribe_AtualizaQuandoEndpointJaExiste()
    {
        using var ctx = CreateContext(nameof(Subscribe_AtualizaQuandoEndpointJaExiste));
        ctx.PushSubscriptions.Add(new PushSubscription
        {
            Id = 1,
            Endpoint = "https://push/a",
            P256dh = "antigo",
            Auth = "antigo",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Subscribe(
            new PushSubscriptionDto("https://push/a", "novo-p256", "novo-auth"),
            CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var sub = await ctx.PushSubscriptions.SingleAsync();
        Assert.Equal("novo-p256", sub.P256dh);
        Assert.Equal("novo-auth", sub.Auth);
        Assert.Equal(1, await ctx.PushSubscriptions.CountAsync());
    }

    [Fact]
    public async Task Unsubscribe_RemoveSubscription()
    {
        using var ctx = CreateContext(nameof(Unsubscribe_RemoveSubscription));
        ctx.PushSubscriptions.Add(new PushSubscription
        {
            Id = 1,
            Endpoint = "https://push/a",
            P256dh = "p",
            Auth = "a",
            CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Unsubscribe(
            new UnsubscribeDto("https://push/a"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await ctx.PushSubscriptions.CountAsync());
    }

    [Fact]
    public async Task Unsubscribe_RetornaNoContentQuandoNaoExiste()
    {
        using var ctx = CreateContext(nameof(Unsubscribe_RetornaNoContentQuandoNaoExiste));
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Unsubscribe(
            new UnsubscribeDto("https://push/inexistente"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
