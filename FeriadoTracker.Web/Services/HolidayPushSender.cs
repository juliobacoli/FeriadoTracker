using System.Text.Json;
using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using Microsoft.EntityFrameworkCore;
using WebPushSubscription = WebPush.PushSubscription;
using WebPushClient = WebPush.WebPushClient;
using VapidDetails = WebPush.VapidDetails;
using WebPushException = WebPush.WebPushException;

namespace FeriadoTracker.Web.Services;

public class HolidayPushSender : IHolidayPushSender
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly TimeProvider _time;
    private readonly ILogger<HolidayPushSender> _logger;

    public HolidayPushSender(
        AppDbContext db,
        IConfiguration config,
        TimeProvider time,
        ILogger<HolidayPushSender> logger)
    {
        _db = db;
        _config = config;
        _time = time;
        _logger = logger;
    }

    public async Task<PushSendResult> SendDailyAsync(CancellationToken ct = default)
    {
        var publicKey = _config["WebPush:VapidPublicKey"];
        var privateKey = _config["WebPush:VapidPrivateKey"];
        var subject = _config["WebPush:Subject"];
        var daysAhead = int.TryParse(_config["WebPush:DaysAhead"], out var d) ? d : 3;

        if (string.IsNullOrWhiteSpace(publicKey)
            || string.IsNullOrWhiteSpace(privateKey)
            || string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("VAPID não configurado. Envio pulado.");
            return new PushSendResult(0, 0, 0);
        }

        var today = _time.GetLocalNow().Date;
        var until = today.AddDays(daysAhead);
        var todayDateOnly = DateOnly.FromDateTime(today);

        var feriados = await _db.Feriados
            .Where(f => f.Data >= today && f.Data <= until)
            .ToListAsync(ct);

        if (feriados.Count == 0)
        {
            _logger.LogInformation("Nenhum feriado nos próximos {Days} dias.", daysAhead);
            return new PushSendResult(0, 0, 0);
        }

        var subscriptions = await _db.PushSubscriptions.ToListAsync(ct);
        if (subscriptions.Count == 0)
        {
            return new PushSendResult(0, 0, 0);
        }

        var feriadoIds = feriados.Select(f => f.Id).ToList();
        var subscriptionIds = subscriptions.Select(s => s.Id).ToList();

        var alreadySent = await _db.NotificationLogs
            .Where(l => l.SentDate == todayDateOnly
                && feriadoIds.Contains(l.FeriadoId)
                && subscriptionIds.Contains(l.SubscriptionId))
            .Select(l => new { l.SubscriptionId, l.FeriadoId })
            .ToListAsync(ct);

        var sentSet = alreadySent
            .Select(x => (x.SubscriptionId, x.FeriadoId))
            .ToHashSet();

        var vapid = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();
        var toRemove = new List<PushSubscription>();
        var sent = 0;
        var skipped = 0;

        foreach (var feriado in feriados)
        {
            var diffDays = (feriado.Data.Date - today).Days;
            var title = "Feriado se aproxima!";
            var body = diffDays switch
            {
                0 => $"Hoje é {feriado.Nome}!",
                1 => $"Amanhã é {feriado.Nome}!",
                _ => $"Faltam {diffDays} dias para {feriado.Nome}."
            };

            var payload = JsonSerializer.Serialize(new { title, body, url = "/" });

            foreach (var sub in subscriptions)
            {
                if (sentSet.Contains((sub.Id, feriado.Id)))
                {
                    skipped++;
                    continue;
                }

                var pushSub = new WebPushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);

                try
                {
                    await client.SendNotificationAsync(pushSub, payload, vapid, ct);

                    _db.NotificationLogs.Add(new NotificationLog
                    {
                        SubscriptionId = sub.Id,
                        FeriadoId = feriado.Id,
                        SentDate = todayDateOnly,
                        SentAtUtc = _time.GetUtcNow().UtcDateTime
                    });

                    sent++;
                }
                catch (WebPushException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.Gone
                    || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    toRemove.Add(sub);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Falha ao enviar push para subscription {Id}.", sub.Id);
                }
            }
        }

        if (toRemove.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(toRemove);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Envio: {Sent} enviadas, {Removed} removidas, {Skipped} já enviadas hoje.",
            sent, toRemove.Count, skipped);

        return new PushSendResult(sent, toRemove.Count, skipped);
    }
}
