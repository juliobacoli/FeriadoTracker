using System.Text.Json;
using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Services;

public class HolidayPushSender(
    AppDbContext db,
    IConfiguration config,
    IWebPushClient pushClient,
    TimeProvider time,
    ILogger<HolidayPushSender> logger) : IHolidayPushSender
{
    public async Task<PushSendResult> SendDailyAsync(CancellationToken ct = default)
    {
        var settings = LoadSettings();
        if (settings is null) return new PushSendResult(0, 0, 0);

        var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
        var until = today.AddDays(settings.DaysAhead);

        var feriados = await GetFeriadosAsync(today, until, ct);
        if (feriados.Count == 0)
        {
            logger.LogInformation("Nenhum feriado nos próximos {Days} dias.", settings.DaysAhead);
            return new PushSendResult(0, 0, 0);
        }

        var subscriptions = await db.PushSubscriptions.ToListAsync(ct);
        if (subscriptions.Count == 0) return new PushSendResult(0, 0, 0);

        var alreadySent = await GetAlreadySentAsync(feriados, subscriptions, ct);

        var toRemove = new List<PushSubscription>();
        var sent = 0;
        var skipped = 0;

        foreach (var feriado in feriados)
        {
            var payload = BuildPayload(feriado, today);

            foreach (var sub in subscriptions)
            {
                if (alreadySent.Contains((sub.Id, feriado.Id)))
                {
                    skipped++;
                    continue;
                }

                var localNow = time.GetLocalNow();
                var fimDoFeriado = new DateTimeOffset(feriado.Data.ToDateTime(new TimeOnly(23, 59, 59)), localNow.Offset);
                var ttlSeconds = (int)(fimDoFeriado - localNow).TotalSeconds;
                if (ttlSeconds < 0) ttlSeconds = 0;

                var outcome = await pushClient.SendAsync(
                    sub.Endpoint, sub.P256dh, sub.Auth, payload, ttlSeconds, ct);

                switch (outcome)
                {
                    case PushSendOutcome.Success:
                        db.NotificationLogs.Add(new NotificationLog
                        {
                            SubscriptionId = sub.Id,
                            FeriadoId = feriado.Id,
                            SentDate = today,
                            SentAtUtc = time.GetUtcNow().UtcDateTime
                        });
                        sent++;
                        break;
                    case PushSendOutcome.Gone:
                        toRemove.Add(sub);
                        break;
                    case PushSendOutcome.Failed:
                        break;
                }
            }
        }

        if (toRemove.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Envio: {Sent} enviadas, {Removed} removidas, {Skipped} já notificadas anteriormente.",
            sent, toRemove.Count, skipped);

        return new PushSendResult(sent, toRemove.Count, skipped);
    }

    private SendSettings? LoadSettings()
    {
        var publicKey = config["WebPush:VapidPublicKey"];
        var privateKey = config["WebPush:VapidPrivateKey"];
        var subject = config["WebPush:Subject"];
        var daysAhead = int.TryParse(config["WebPush:DaysAhead"], out var d) ? d : 3;

        if (string.IsNullOrWhiteSpace(publicKey)
            || string.IsNullOrWhiteSpace(privateKey)
            || string.IsNullOrWhiteSpace(subject))
        {
            logger.LogWarning("VAPID não configurado. Envio pulado.");
            return null;
        }

        return new SendSettings(daysAhead);
    }

    private Task<List<Feriado>> GetFeriadosAsync(DateOnly today, DateOnly until, CancellationToken ct) =>
        db.Feriados
            .Where(f => f.Data >= today && f.Data <= until)
            .ToListAsync(ct);

    private async Task<HashSet<(int SubscriptionId, int FeriadoId)>> GetAlreadySentAsync(
        List<Feriado> feriados,
        List<PushSubscription> subscriptions,
        CancellationToken ct)
    {
        var feriadoIds = feriados.Select(f => f.Id).ToList();
        var subscriptionIds = subscriptions.Select(s => s.Id).ToList();

        var rows = await db.NotificationLogs
            .Where(l => feriadoIds.Contains(l.FeriadoId)
                && subscriptionIds.Contains(l.SubscriptionId))
            .Select(l => new { l.SubscriptionId, l.FeriadoId })
            .ToListAsync(ct);

        return rows.Select(x => (x.SubscriptionId, x.FeriadoId)).ToHashSet();
    }

    private static string BuildPayload(Feriado feriado, DateOnly today)
    {
        var diffDays = feriado.Data.DayNumber - today.DayNumber;
        return JsonSerializer.Serialize(new
        {
            title = NotificationTemplates.Title,
            body = NotificationTemplates.Body(diffDays, feriado.Nome, feriado.Data),
            url = NotificationTemplates.DefaultUrl
        });
    }
}
