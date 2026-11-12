using FeriadoTracker.Web.Controllers;
using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    [Theory]
    [InlineData("", "p", "a")]
    [InlineData("https://push/a", "", "a")]
    [InlineData("https://push/a", "p", "")]
    [InlineData("   ", "p", "a")]
    [InlineData("https://push/a", "p", "   ")]
    public async Task Subscribe_RetornaBadRequest_QuandoQualquerCampoEstaVazio(string endpoint, string p256dh, string auth)
    {
        using var ctx = CreateContext(nameof(Subscribe_RetornaBadRequest_QuandoQualquerCampoEstaVazio) + endpoint + p256dh + auth);
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Subscribe(new PushSubscriptionDto(endpoint, p256dh, auth), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await ctx.PushSubscriptions.CountAsync());
    }

    [Fact]
    public async Task Subscribe_GravaDataDeCriacaoEmUtc()
    {
        using var ctx = CreateContext(nameof(Subscribe_GravaDataDeCriacaoEmUtc));
        var time = Time();
        var controller = new PushController(ctx, time, BuildConfig());

        await controller.Subscribe(new PushSubscriptionDto("https://push/a", "p", "a"), CancellationToken.None);

        var sub = await ctx.PushSubscriptions.SingleAsync();
        Assert.Equal(time.GetUtcNow().UtcDateTime, sub.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Unsubscribe_RetornaBadRequest_QuandoEndpointVazio(string endpoint)
    {
        using var ctx = CreateContext(nameof(Unsubscribe_RetornaBadRequest_QuandoEndpointVazio) + endpoint.Length);
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Unsubscribe(new UnsubscribeDto(endpoint), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    // ---- Corrida entre duas inscrições do mesmo endpoint ----
    // O EF InMemory não aplica índice único, por isso estes testes usam SQLite em memória.

    // Antes do primeiro salvamento, outra "requisição" grava o mesmo endpoint no banco.
    private sealed class ConcorrenteInterceptor(SqliteConnection conexao, string endpoint, int vezes) : SaveChangesInterceptor
    {
        private int _restantes = vezes;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (_restantes > 0)
            {
                _restantes--;
                var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexao).Options;
                await using var outro = new AppDbContext(opcoes);
                outro.PushSubscriptions.Add(new PushSubscription
                {
                    Endpoint = endpoint,
                    P256dh = "da-outra-requisicao",
                    Auth = "da-outra-requisicao",
                    CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                });
                await outro.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }

    private static (SqliteConnection Conexao, DbContextOptions<AppDbContext> Opcoes) NovoBancoSqlite(Func<SqliteConnection, SaveChangesInterceptor>? interceptor = null)
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexao);
        using (var criador = new AppDbContext(builder.Options))
        {
            criador.Database.EnsureCreated();
        }
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor(conexao));
        }
        return (conexao, builder.Options);
    }

    [Fact]
    public async Task Subscribe_TentaDeNovo_QuandoOutraRequisicaoGravouOMesmoEndpointNoMeioDoCaminho()
    {
        const string endpoint = "https://push/corrida";
        var (conexao, opcoes) = NovoBancoSqlite(c => new ConcorrenteInterceptor(c, endpoint, vezes: 1));
        using var _ = conexao;
        await using var ctx = new AppDbContext(opcoes);
        var controller = new PushController(ctx, Time(), BuildConfig());

        var result = await controller.Subscribe(new PushSubscriptionDto(endpoint, "meu-p256", "meu-auth"), CancellationToken.None);

        Assert.IsType<OkResult>(result);
        await using var leitura = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conexao).Options);
        var sub = await leitura.PushSubscriptions.SingleAsync();
        Assert.Equal(endpoint, sub.Endpoint);
        // Na segunda tentativa o endpoint já existia, então o registro foi atualizado em vez de duplicado.
        Assert.Equal("meu-p256", sub.P256dh);
        Assert.Equal("meu-auth", sub.Auth);
    }

    // Simula um banco que recusa todo salvamento (por exemplo, a mesma corrida acontecendo nas duas tentativas).
    private sealed class FalhaSempreInterceptor : SaveChangesInterceptor
    {
        public int Tentativas { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Tentativas++;
            throw new DbUpdateException("falha simulada");
        }
    }

    [Fact]
    public async Task Subscribe_RepassaOErro_QuandoAsDuasTentativasFalham()
    {
        var interceptor = new FalhaSempreInterceptor();
        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(Subscribe_RepassaOErro_QuandoAsDuasTentativasFalham))
            .AddInterceptors(interceptor)
            .Options;
        await using var ctx = new AppDbContext(opcoes);
        var controller = new PushController(ctx, Time(), BuildConfig());

        // O filtro "when (attempt == 0)" só captura a primeira falha; na segunda o erro sobe,
        // então o "return Conflict()" no fim do método nunca é alcançado.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => controller.Subscribe(new PushSubscriptionDto("https://push/a", "p", "a"), CancellationToken.None));
        Assert.Equal(2, interceptor.Tentativas);
    }
}
