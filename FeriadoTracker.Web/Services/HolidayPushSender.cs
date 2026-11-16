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

        var linhas = await GetFeriadosAsync(today, until, ct);

        // Um aviso por feriado (Carnaval são duas linhas), já com a regra de folga e ponte.
        var feriados = MontarAvisos(linhas, linhas, today, until, local: null);

        var subscriptions = await db.PushSubscriptions.Include(s => s.Locais).ToListAsync(ct);
        if (subscriptions.Count == 0) return new PushSendResult(0, 0, 0);

        // Estaduais e municipais: só dos locais marcados por algum aparelho. A folga conta os vizinhos
        // (nacionais e, na cidade, os do estado), mas o cálculo fica separado por local
        // para não mudar o aviso nacional de quem não marcou aquele local.
        var locaisMarcados = subscriptions
            .SelectMany(s => s.Locais)
            .Select(l => LocaisLiberados.Buscar(l.Codigo))
            .OfType<LocalLiberado>()
            .Distinct()
            .ToList();

        if (locaisMarcados.Count > 0)
        {
            var regionais = await GetRegionaisAsync(locaisMarcados.Select(l => l.CodigoUf).Distinct().ToList(), today, until, ct);
            foreach (var local in locaisMarcados)
            {
                var alvo = regionais.Where(local.Inclui).ToList();
                var vizinhos = regionais.Where(f => f.CodigoUf == local.CodigoUf && f.CodigoMunicipio == null);
                feriados.AddRange(MontarAvisos(linhas.Concat(vizinhos).Union(alvo).ToList(), alvo, today, until, local));
            }
        }

        if (feriados.Count == 0)
        {
            logger.LogInformation("Nenhum feriado nos próximos {Days} dias.", settings.DaysAhead);
            return new PushSendResult(0, 0, 0);
        }

        var alreadySent = await GetAlreadySentAsync(feriados, subscriptions, ct);

        var toRemove = new List<PushSubscription>();
        var sent = 0;
        var skipped = 0;

        foreach (var (feriado, ids, local) in feriados)
        {
            var payload = BuildPayload(feriado, today, local);

            foreach (var sub in subscriptions.Where(s => local is null || s.Locais.Exists(l => l.Codigo == local.Codigo)))
            {
                // Qualquer dia do feriado já avisado conta (logs antigos eram por linha).
                if (ids.Any(id => alreadySent.Contains((sub.Id, id))))
                {
                    skipped++;
                    continue;
                }

                var localNow = time.GetLocalNow();
                var fimDoFeriado = new DateTimeOffset(feriado.Fim.ToDateTime(new TimeOnly(23, 59, 59)), localNow.Offset);
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
                            FeriadoId = ids[0],
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

    /// <param name="Local">Nulo nos nacionais; nos estaduais e municipais, o local de quem recebe.</param>
    private sealed record FeriadoParaAviso(FeriadoAnalisado Feriado, List<int> Ids, LocalLiberado? Local);

    /// <summary>
    /// Analisa <paramref name="todas"/> (o alvo mais os vizinhos que estendem a folga)
    /// e devolve um aviso por feriado de <paramref name="alvo"/> dentro da janela.
    /// </summary>
    private static List<FeriadoParaAviso> MontarAvisos(
        List<Feriado> todas, List<Feriado> alvo, DateOnly today, DateOnly until, LocalLiberado? local) =>
        FolgaCalculator.Analisar(todas)
            .Where(a => a.Fim >= today && a.Inicio <= until)
            .Select(a => new FeriadoParaAviso(
                a,
                alvo.Where(l => l.Nome == a.Nome && l.Data >= a.Inicio && l.Data <= a.Fim)
                    .OrderBy(l => l.Data)
                    .Select(l => l.Id)
                    .ToList(),
                local))
            .Where(f => f.Ids.Count > 0)
            .ToList();

    // Uma semana de margem: feriados vizinhos estendem a folga e o Carnaval pode ter começado ontem.
    private Task<List<Feriado>> GetFeriadosAsync(DateOnly today, DateOnly until, CancellationToken ct) =>
        db.Feriados
            .AsNoTracking()
            .Where(f => f.CodigoUf == null && f.Data >= today.AddDays(-7) && f.Data <= until.AddDays(7))
            .ToListAsync(ct);

    private Task<List<Feriado>> GetRegionaisAsync(
        List<int> codigosUf, DateOnly today, DateOnly until, CancellationToken ct) =>
        db.Feriados
            .AsNoTracking()
            .Where(f => f.CodigoUf != null && codigosUf.Contains(f.CodigoUf.Value)
                && f.Data >= today.AddDays(-7) && f.Data <= until.AddDays(7))
            .ToListAsync(ct);

    private async Task<HashSet<(int SubscriptionId, int FeriadoId)>> GetAlreadySentAsync(
        List<FeriadoParaAviso> feriados,
        List<PushSubscription> subscriptions,
        CancellationToken ct)
    {
        var feriadoIds = feriados.SelectMany(f => f.Ids).ToList();
        var subscriptionIds = subscriptions.Select(s => s.Id).ToList();

        var rows = await db.NotificationLogs
            .Where(l => feriadoIds.Contains(l.FeriadoId)
                && subscriptionIds.Contains(l.SubscriptionId))
            .Select(l => new { l.SubscriptionId, l.FeriadoId })
            .ToListAsync(ct);

        return rows.Select(x => (x.SubscriptionId, x.FeriadoId)).ToHashSet();
    }

    private static string BuildPayload(FeriadoAnalisado feriado, DateOnly today, LocalLiberado? local)
    {
        var diffDays = feriado.Inicio.DayNumber - today.DayNumber;
        return JsonSerializer.Serialize(new
        {
            title = local is null ? NotificationTemplates.Title : NotificationTemplates.TitleRegional(local.Local),
            body = NotificationTemplates.Body(diffDays, feriado),
            url = NotificationTemplates.DefaultUrl
        });
    }
}
