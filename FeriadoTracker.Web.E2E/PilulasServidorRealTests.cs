using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.E2E.Suporte;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.Playwright.Assertions;

namespace FeriadoTracker.Web.E2E;

/// <summary>
/// Pílulas contra o PushController e o banco reais (sem ApiPushFalsa): pega rate limit,
/// antiforgery e corrida no banco, que a API simulada esconde.
/// </summary>
public class PilulasServidorRealTests(AppFixture app) : E2ETest(app)
{
    [Fact(DisplayName = "1.9 Marcar todas as pílulas salva todas no servidor, sem aviso de erro")]
    public async Task MarcarTodas()
    {
        var ct = TestContext.Current.CancellationToken;
        const string endpoint = "https://push.e2e.test/sub/servidor-real";
        using (var escopo = App.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PushSubscriptions.Add(new PushSubscription { Endpoint = endpoint, P256dh = "p", Auth = "a", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(ct);
        }

        await PushFalso.InstalarAsync(Page, inscrito: true, endpoint: endpoint);
        await Page.GotoAsync("/");
        await Page.Locator("#locaisToggle").ClickAsync();

        var pilulas = Page.Locator("#locaisPilulas .pilula");
        var total = await pilulas.CountAsync();
        for (var i = 0; i < total; i++)
            await pilulas.Nth(i).ClickAsync();

        await Page.Locator("#locaisToggle").ClickAsync();
        await Page.WaitForLoadStateAsync();
        await Task.Delay(1500, ct);

        await Expect(Page.Locator(".toast-error")).ToHaveCountAsync(0);
        using (var escopo = App.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            var sub = await db.PushSubscriptions.Include(s => s.Locais).SingleAsync(s => s.Endpoint == endpoint, ct);
            Assert.Equal(total, sub.Locais.Count);
        }
    }
}
