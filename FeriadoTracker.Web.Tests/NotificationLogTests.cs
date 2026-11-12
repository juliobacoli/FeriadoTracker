using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Tests;

public class NotificationLogTests
{
    // ---- Unitários: a entidade em si ----

    [Fact]
    public void NovoLog_TemValoresPadrao()
    {
        var log = new NotificationLog();

        Assert.Equal(0, log.Id);
        Assert.Equal(0, log.SubscriptionId);
        Assert.Equal(0, log.FeriadoId);
        Assert.Equal(default, log.SentDate);
        Assert.Equal(default, log.SentAtUtc);
        Assert.Null(log.Subscription);
        Assert.Null(log.Feriado);
    }

    [Fact]
    public void Propriedades_GuardamOsValoresAtribuidos()
    {
        var assinatura = new PushSubscription { Id = 3 };
        var feriado = new Feriado { Id = 7 };
        var enviadoEm = new DateTime(2026, 12, 24, 11, 0, 0, DateTimeKind.Utc);

        var log = new NotificationLog
        {
            Id = 1,
            SubscriptionId = 3,
            FeriadoId = 7,
            SentDate = new DateOnly(2026, 12, 24),
            SentAtUtc = enviadoEm,
            Subscription = assinatura,
            Feriado = feriado,
        };

        Assert.Equal(1, log.Id);
        Assert.Equal(3, log.SubscriptionId);
        Assert.Equal(7, log.FeriadoId);
        Assert.Equal(new DateOnly(2026, 12, 24), log.SentDate);
        Assert.Equal(enviadoEm, log.SentAtUtc);
        Assert.Same(assinatura, log.Subscription);
        Assert.Same(feriado, log.Feriado);
    }

    // ---- Integração: SQLite em memória com as migrations reais ----
    // O EF InMemory não aplica índice único nem cascata, por isso estes testes usam SQLite.

    private sealed class BancoSqlite : IDisposable
    {
        private readonly SqliteConnection _conexao = new("DataSource=:memory:");
        private readonly DbContextOptions<AppDbContext> _options;

        public BancoSqlite()
        {
            _conexao.Open();
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options;
            using var ctx = Novo();
            ctx.Database.Migrate();
        }

        public AppDbContext Novo() => new(_options);

        public void Dispose() => _conexao.Dispose();
    }

    private static PushSubscription NovaAssinatura(string nome) => new()
    {
        Endpoint = $"https://push/{nome}",
        P256dh = "p256dh",
        Auth = "auth",
        CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static NotificationLog NovoLog(int assinaturaId, int feriadoId) => new()
    {
        SubscriptionId = assinaturaId,
        FeriadoId = feriadoId,
        SentDate = new DateOnly(2026, 5, 1),
        SentAtUtc = new DateTime(2026, 5, 1, 11, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public async Task Banco_RejeitaLogDuplicadoParaMesmaAssinaturaEFeriado()
    {
        using var banco = new BancoSqlite();
        int assinaturaId, feriadoId;

        await using (var ctx = banco.Novo())
        {
            var assinatura = NovaAssinatura("a");
            ctx.PushSubscriptions.Add(assinatura);
            await ctx.SaveChangesAsync();
            assinaturaId = assinatura.Id;
            feriadoId = (await ctx.Feriados.OrderBy(f => f.Id).FirstAsync()).Id;
            ctx.NotificationLogs.Add(NovoLog(assinaturaId, feriadoId));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = banco.Novo())
        {
            ctx.NotificationLogs.Add(NovoLog(assinaturaId, feriadoId));
            var erro = await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
            var sqlite = Assert.IsType<SqliteException>(erro.InnerException);
            Assert.Contains("UNIQUE", sqlite.Message);
        }

        await using (var ctx = banco.Novo())
        {
            Assert.Equal(1, await ctx.NotificationLogs.CountAsync());
        }
    }

    [Fact]
    public async Task Banco_ApagaOsLogsDaAssinatura_QuandoAAssinaturaEExcluida()
    {
        using var banco = new BancoSqlite();
        int removidaId, mantidaId;

        await using (var ctx = banco.Novo())
        {
            var removida = NovaAssinatura("removida");
            var mantida = NovaAssinatura("mantida");
            ctx.PushSubscriptions.AddRange(removida, mantida);
            await ctx.SaveChangesAsync();
            removidaId = removida.Id;
            mantidaId = mantida.Id;

            var feriados = await ctx.Feriados.OrderBy(f => f.Id).Take(2).ToListAsync();
            ctx.NotificationLogs.AddRange(
                NovoLog(removidaId, feriados[0].Id),
                NovoLog(removidaId, feriados[1].Id),
                NovoLog(mantidaId, feriados[0].Id));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = banco.Novo())
        {
            // ExecuteDelete não passa pelo rastreador do EF: quem apaga os logs é a cascata do próprio banco.
            await ctx.PushSubscriptions.Where(s => s.Id == removidaId).ExecuteDeleteAsync();
        }

        await using (var ctx = banco.Novo())
        {
            var restantes = await ctx.NotificationLogs.ToListAsync();
            var restante = Assert.Single(restantes);
            Assert.Equal(mantidaId, restante.SubscriptionId);
        }
    }

    [Fact]
    public async Task Banco_GravaELeDeVolta_SentDateESentAtUtc()
    {
        using var banco = new BancoSqlite();
        var data = new DateOnly(2026, 12, 24);
        var enviadoEm = new DateTime(2026, 12, 24, 11, 30, 45, DateTimeKind.Utc);
        int logId;

        await using (var ctx = banco.Novo())
        {
            var assinatura = NovaAssinatura("a");
            ctx.PushSubscriptions.Add(assinatura);
            await ctx.SaveChangesAsync();
            var feriadoId = (await ctx.Feriados.OrderBy(f => f.Id).FirstAsync()).Id;

            var log = NovoLog(assinatura.Id, feriadoId);
            log.SentDate = data;
            log.SentAtUtc = enviadoEm;
            ctx.NotificationLogs.Add(log);
            await ctx.SaveChangesAsync();
            logId = log.Id;
        }

        await using (var ctx = banco.Novo())
        {
            var lido = await ctx.NotificationLogs.AsNoTracking().SingleAsync(n => n.Id == logId);

            Assert.Equal(data, lido.SentDate);
            Assert.Equal(enviadoEm, lido.SentAtUtc);
        }
    }
}
