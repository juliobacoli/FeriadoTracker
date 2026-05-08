using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using FeriadoTracker.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class HolidayPushSenderTests
{
    private const string ValidPublicKey = "test-public-key";
    private const string ValidPrivateKey = "test-private-key";

    private static AppDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration BuildConfig(int daysAhead = 5, bool withVapid = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["WebPush:DaysAhead"] = daysAhead.ToString(),
            ["WebPush:Subject"] = withVapid ? "mailto:teste@exemplo.com" : null,
            ["WebPush:VapidPublicKey"] = withVapid ? ValidPublicKey : null,
            ["WebPush:VapidPrivateKey"] = withVapid ? ValidPrivateKey : null
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static FakeTimeProvider FakeTime(DateTime utcNow)
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(utcNow, TimeSpan.Zero));
        provider.SetLocalTimeZone(TimeZoneInfo.Utc);
        return provider;
    }

    private sealed class FakePushClient : IWebPushClient
    {
        public Func<string, PushSendOutcome> OutcomeFor { get; set; } = _ => PushSendOutcome.Success;
        public List<(string Endpoint, string Payload)> Calls { get; } = new();

        public Task<PushSendOutcome> SendAsync(
            string endpoint, string p256dh, string auth, string payload, CancellationToken ct = default)
        {
            Calls.Add((endpoint, payload));
            return Task.FromResult(OutcomeFor(endpoint));
        }
    }

    private static PushSubscription MakeSub(int id, string endpoint) => new()
    {
        Id = id,
        Endpoint = endpoint,
        P256dh = "p256",
        Auth = "auth",
        CreatedAt = DateTime.UtcNow
    };

    private static HolidayPushSender BuildSender(
        AppDbContext db,
        IWebPushClient client,
        FakeTimeProvider time,
        IConfiguration? config = null) =>
        new(
            db,
            config ?? BuildConfig(),
            client,
            time,
            NullLogger<HolidayPushSender>.Instance);

    [Fact]
    public async Task SendDailyAsync_RetornaZerosQuandoVapidNaoConfigurado()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_RetornaZerosQuandoVapidNaoConfigurado));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Trabalho", Data = new DateOnly(2026, 5, 1), Tipo = "Nacional" });
        ctx.PushSubscriptions.Add(MakeSub(1, "https://push/abc"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 4, 30)), BuildConfig(withVapid: false));

        var result = await sender.SendDailyAsync();

        Assert.Equal(0, result.Sent);
        Assert.Equal(0, result.Removed);
        Assert.Equal(0, result.Skipped);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SendDailyAsync_RetornaZerosQuandoNaoTemFeriadosNaJanela()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_RetornaZerosQuandoNaoTemFeriadosNaJanela));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Distante", Data = new DateOnly(2026, 12, 25), Tipo = "Nacional" });
        ctx.PushSubscriptions.Add(MakeSub(1, "https://push/abc"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(0, result.Sent);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SendDailyAsync_RetornaZerosQuandoNaoTemSubscriptions()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_RetornaZerosQuandoNaoTemSubscriptions));
        ctx.Feriados.Add(new Feriado { Id = 1, Nome = "Trabalho", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" });
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(0, result.Sent);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SendDailyAsync_EnviaParaCadaSubscriptionEPersisteLog()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_EnviaParaCadaSubscriptionEPersisteLog));
        ctx.Feriados.Add(new Feriado { Id = 10, Nome = "Trabalho", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" });
        ctx.PushSubscriptions.AddRange(
            MakeSub(1, "https://push/a"),
            MakeSub(2, "https://push/b"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(2, result.Sent);
        Assert.Equal(0, result.Removed);
        Assert.Equal(2, client.Calls.Count);
        Assert.Equal(2, await ctx.NotificationLogs.CountAsync());
    }

    [Fact]
    public async Task SendDailyAsync_PulaQuandoLogJaExiste()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_PulaQuandoLogJaExiste));
        ctx.Feriados.Add(new Feriado { Id = 10, Nome = "Trabalho", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" });
        ctx.PushSubscriptions.Add(MakeSub(1, "https://push/a"));
        ctx.NotificationLogs.Add(new NotificationLog
        {
            SubscriptionId = 1,
            FeriadoId = 10,
            SentDate = new DateOnly(2026, 4, 30),
            SentAtUtc = new DateTime(2026, 4, 30)
        });
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(0, result.Sent);
        Assert.Equal(1, result.Skipped);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task SendDailyAsync_RemoveSubscriptionsComOutcomeGone()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_RemoveSubscriptionsComOutcomeGone));
        ctx.Feriados.Add(new Feriado { Id = 10, Nome = "Trabalho", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" });
        ctx.PushSubscriptions.AddRange(
            MakeSub(1, "https://push/ok"),
            MakeSub(2, "https://push/gone"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient
        {
            OutcomeFor = endpoint => endpoint.EndsWith("gone") ? PushSendOutcome.Gone : PushSendOutcome.Success
        };
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(1, result.Sent);
        Assert.Equal(1, result.Removed);
        Assert.Equal(1, await ctx.PushSubscriptions.CountAsync());
        Assert.False(await ctx.PushSubscriptions.AnyAsync(s => s.Endpoint == "https://push/gone"));
    }

    [Fact]
    public async Task SendDailyAsync_NaoEnviaParaFeriadoForaDaJanela()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_NaoEnviaParaFeriadoForaDaJanela));
        ctx.Feriados.AddRange(
            new Feriado { Id = 1, Nome = "Dentro", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" },
            new Feriado { Id = 2, Nome = "Fora", Data = new DateOnly(2026, 5, 20), Tipo = "Nacional" });
        ctx.PushSubscriptions.Add(MakeSub(1, "https://push/a"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient();
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(1, result.Sent);
        Assert.Single(client.Calls);
    }

    [Fact]
    public async Task SendDailyAsync_OutcomeFailedNaoMarcaLogNemRemove()
    {
        await using var ctx = CreateContext(nameof(SendDailyAsync_OutcomeFailedNaoMarcaLogNemRemove));
        ctx.Feriados.Add(new Feriado { Id = 10, Nome = "Trabalho", Data = new DateOnly(2026, 5, 4), Tipo = "Nacional" });
        ctx.PushSubscriptions.Add(MakeSub(1, "https://push/a"));
        await ctx.SaveChangesAsync();

        var client = new FakePushClient { OutcomeFor = _ => PushSendOutcome.Failed };
        var sender = BuildSender(ctx, client, FakeTime(new DateTime(2026, 5, 1)));

        var result = await sender.SendDailyAsync();

        Assert.Equal(0, result.Sent);
        Assert.Equal(0, result.Removed);
        Assert.Empty(await ctx.NotificationLogs.ToListAsync());
        Assert.Equal(1, await ctx.PushSubscriptions.CountAsync());
    }
}
