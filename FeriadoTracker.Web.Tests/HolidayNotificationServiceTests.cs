using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace FeriadoTracker.Web.Tests;

public class HolidayNotificationServiceTests
{
    private static HolidayNotificationService CreateService(FakeTimeProvider time)
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new HolidayNotificationService(
            scopeFactory,
            time,
            NullLogger<HolidayNotificationService>.Instance
        );
    }

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoAntesDas8h()
    {
        // 7:59
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 7, 59, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoExatamenteAs8h()
    {
        // 8:00
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoDepoisDas8h()
    {
        // 8:01
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 1, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaHoje_QuandoMeiaNoite()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoUmMinutoAntesDaMeiaNoite()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 23, 59, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Theory]
    [InlineData(2026, 4, 30, 2026, 5, 1)]   // virada de mês
    [InlineData(2026, 12, 31, 2027, 1, 1)]  // virada de ano
    public void ComputeNextRun_AtravessaViradaDeMesEAno_QuandoDepoisDas8h(
        int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(ano, mes, dia, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(anoEsperado, mesEsperado, diaEsperado, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Theory]
    [InlineData(2028, 2, 28, 2028, 2, 29)]  // 29/fev existe em 2028
    [InlineData(2028, 2, 29, 2028, 3, 1)]
    [InlineData(2026, 2, 28, 2026, 3, 1)]   // ano comum não tem 29/fev
    public void ComputeNextRun_RespeitaAnoBissexto_QuandoDepoisDas8h(
        int ano, int mes, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(ano, mes, dia, 9, 0, 0, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(anoEsperado, mesEsperado, diaEsperado, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRun_UsaHorarioLocalDoBrasil()
    {
        var brasilia = TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        // 10:59 UTC = 07:59 em Brasília (-03:00)
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 10, 59, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(brasilia);
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        // 08:00 em Brasília = 11:00 UTC, mesmo dia
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.FromHours(-3)), nextRun);
        Assert.Equal(TimeSpan.FromHours(-3), nextRun.Offset);
    }

    [Fact]
    public void ComputeNextRun_AgendaParaAmanha_QuandoPassaDeFracaoDeSegundoDas8h()
    {
        // 08:00:00,500: já passou das 8h por meio segundo
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 8, 0, 0, 500, TimeSpan.Zero));
        var service = CreateService(time);

        var nextRun = service.ComputeNextRun();

        Assert.Equal(new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), nextRun);
    }

    // ---- ExecuteAsync (laço em segundo plano) ----

    private static readonly TimeSpan Limite = TimeSpan.FromSeconds(5);

    private sealed class SenderState
    {
        public int Calls;
        public int Disposed;
        public readonly List<CancellationToken> Tokens = [];
        public Exception? Throw;
    }

    // Registrado como scoped: o contêiner descarta (Dispose) a instância ao fechar o escopo.
    private sealed class FakeSender(SenderState state) : IHolidayPushSender, IDisposable
    {
        public Task<PushSendResult> SendDailyAsync(CancellationToken cancellationToken = default)
        {
            lock (state.Tokens) state.Tokens.Add(cancellationToken);
            Interlocked.Increment(ref state.Calls);
            if (state.Throw is { } erro) throw erro;
            return Task.FromResult(new PushSendResult(1, 0, 0));
        }

        public void Dispose() => Interlocked.Increment(ref state.Disposed);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public readonly List<(LogLevel Nivel, Exception? Erro)> Entradas = [];

        public int Erros { get { lock (Entradas) return Entradas.Count(e => e.Nivel == LogLevel.Error); } }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Entradas) Entradas.Add((logLevel, exception));
        }
    }

    // Relógio falso que avisa quando o serviço começa a esperar (cria um timer).
    // Sem isso, o teste pode avançar o relógio antes de o laço do serviço chegar na espera.
    private sealed class TempoObservavel(FakeTimeProvider inner) : TimeProvider
    {
        private readonly SemaphoreSlim _timersCriados = new(0);

        public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();
        public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;
        public override long GetTimestamp() => inner.GetTimestamp();
        public override long TimestampFrequency => inner.TimestampFrequency;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = inner.CreateTimer(callback, state, dueTime, period);
            _timersCriados.Release();
            return timer;
        }

        public async Task EsperarServicoAguardarAsync()
        {
            if (!await _timersCriados.WaitAsync(Limite))
                throw new TimeoutException("O serviço não chegou a esperar o próximo horário.");
        }

        public void Avancar(TimeSpan tempo) => inner.Advance(tempo);
    }

    private static TempoObservavel TempoAs0759() =>
        new(new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 7, 59, 0, TimeSpan.Zero)));

    private static HolidayNotificationService CreateServiceComSender(
        TimeProvider time, SenderState state, ILogger<HolidayNotificationService> logger, bool registrarSender = true)
    {
        var services = new ServiceCollection();
        if (registrarSender) services.AddScoped<IHolidayPushSender>(_ => new FakeSender(state));
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        return new HolidayNotificationService(scopeFactory, time, logger);
    }

    private static async Task EsperarAsync(Func<bool> condicao, string descricao)
    {
        var fim = DateTime.UtcNow + Limite;
        while (!condicao())
        {
            if (DateTime.UtcNow > fim) throw new TimeoutException($"Não aconteceu a tempo: {descricao}");
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NaoEnvia_QuandoTokenJaCancelado()
    {
        var state = new SenderState();
        var service = CreateServiceComSender(TempoAs0759(), state, NullLogger<HolidayNotificationService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await service.StartAsync(cts.Token);

        Assert.Equal(0, state.Calls);
        Assert.True(service.ExecuteTask!.IsCompleted);
        Assert.False(service.ExecuteTask.IsFaulted);
    }

    [Fact]
    public async Task ExecuteAsync_TerminaSemEnviar_QuandoCanceladoDuranteAEspera()
    {
        var tempo = TempoAs0759();
        var state = new SenderState();
        var logger = new CapturingLogger<HolidayNotificationService>();
        var service = CreateServiceComSender(tempo, state, logger);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);
        Assert.Equal(0, state.Calls);
        Assert.Equal(0, logger.Erros);
    }

    [Fact]
    public async Task ExecuteAsync_ChamaSender_QuandoChegaAHora()
    {
        var tempo = TempoAs0759();
        var state = new SenderState();
        var service = CreateServiceComSender(tempo, state, NullLogger<HolidayNotificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        Assert.Equal(0, state.Calls); // 07:59: ainda não é hora

        tempo.Avancar(TimeSpan.FromSeconds(61)); // 08:00:01
        await EsperarAsync(() => Volatile.Read(ref state.Calls) >= 1, "primeiro envio");
        await tempo.EsperarServicoAguardarAsync(); // terminou o envio e agendou o próximo
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, state.Calls);
        Assert.True(state.Tokens[0].IsCancellationRequested, "recebeu o token do serviço, cancelado ao parar");
    }

    [Fact]
    public async Task ExecuteAsync_EnviaUmaVezPorDia_ENaoRepeteNoMesmoDia()
    {
        var tempo = TempoAs0759();
        var state = new SenderState();
        var service = CreateServiceComSender(tempo, state, NullLogger<HolidayNotificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        tempo.Avancar(TimeSpan.FromSeconds(61)); // 08:00:01 do dia 1
        await EsperarAsync(() => Volatile.Read(ref state.Calls) >= 1, "envio do dia 1");
        await tempo.EsperarServicoAguardarAsync();

        tempo.Avancar(TimeSpan.FromHours(23)); // 07:00:01 do dia 2: ainda não é hora
        await Task.Delay(50);
        Assert.Equal(1, state.Calls);

        tempo.Avancar(TimeSpan.FromHours(2)); // 09:00:01 do dia 2: passou das 08:00
        await EsperarAsync(() => Volatile.Read(ref state.Calls) >= 2, "envio do dia 2");
        await tempo.EsperarServicoAguardarAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(2, state.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_CriaEDescartaUmEscopoPorEnvio()
    {
        var tempo = TempoAs0759();
        var state = new SenderState();
        var service = CreateServiceComSender(tempo, state, NullLogger<HolidayNotificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        tempo.Avancar(TimeSpan.FromSeconds(61));
        await EsperarAsync(() => Volatile.Read(ref state.Disposed) >= 1, "escopo descartado");
        await tempo.EsperarServicoAguardarAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, state.Calls);
        Assert.Equal(1, state.Disposed);
    }

    [Fact]
    public async Task ExecuteAsync_RegistraErroEContinua_QuandoSenderLancaExcecao()
    {
        var tempo = TempoAs0759();
        var erro = new InvalidOperationException("falha simulada");
        var state = new SenderState { Throw = erro };
        var logger = new CapturingLogger<HolidayNotificationService>();
        var service = CreateServiceComSender(tempo, state, logger);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        tempo.Avancar(TimeSpan.FromSeconds(61));
        await EsperarAsync(() => logger.Erros >= 1, "erro registrado");
        await tempo.EsperarServicoAguardarAsync(); // depois da falha, agendou o próximo envio

        Assert.False(service.ExecuteTask!.IsCompleted, "o serviço continua rodando depois da falha");

        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask.IsCompletedSuccessfully);
        Assert.Equal(1, state.Calls);
        Assert.Contains(logger.Entradas, e => e.Nivel == LogLevel.Error && ReferenceEquals(e.Erro, erro));
    }

    [Fact]
    public async Task ExecuteAsync_RegistraErroEContinua_QuandoSenderNaoEstaRegistrado()
    {
        var tempo = TempoAs0759();
        var state = new SenderState();
        var logger = new CapturingLogger<HolidayNotificationService>();
        var service = CreateServiceComSender(tempo, state, logger, registrarSender: false);

        await service.StartAsync(CancellationToken.None);
        await tempo.EsperarServicoAguardarAsync();
        tempo.Avancar(TimeSpan.FromSeconds(61));
        await EsperarAsync(() => logger.Erros >= 1, "erro registrado");
        await tempo.EsperarServicoAguardarAsync();

        Assert.False(service.ExecuteTask!.IsCompleted, "o serviço continua rodando depois da falha");

        await service.StopAsync(CancellationToken.None);

        Assert.Equal(0, state.Calls);
        Assert.Contains(logger.Entradas, e => e.Nivel == LogLevel.Error && e.Erro is InvalidOperationException);
    }
}
