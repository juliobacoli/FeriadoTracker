using Microsoft.Playwright;

namespace FeriadoTracker.Web.E2E;

/// <summary>
/// Base dos testes de tela. Cada teste ganha um contexto novo do navegador com vídeo e trace ligados.
/// Se o teste falhar, ficam em TestResults/e2e/&lt;browser&gt;/&lt;teste&gt;/: print.png, video.webm e trace.zip.
/// Se passar, tudo é descartado. E2E_ARTEFATOS=sempre guarda também dos testes que passaram.
/// Ver o trace: pwsh bin/Debug/net10.0/playwright.ps1 show-trace &lt;caminho&gt;/trace.zip
/// </summary>
public abstract class E2ETest(AppFixture app) : IAsyncLifetime
{
    internal static readonly string PastaRaiz = Path.Combine(
        typeof(E2ETest).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
            .Cast<System.Reflection.AssemblyMetadataAttribute>()
            .First(a => a.Key == "PastaDoProjeto").Value!,
        "TestResults", "e2e");
    private readonly string _pastaVideoTemp = Path.Combine(Path.GetTempPath(), "feriadotracker-e2e-video-" + Guid.NewGuid().ToString("N"));

    protected AppFixture App { get; } = app;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var opcoes = new BrowserNewContextOptions
        {
            BaseURL = App.BaseUrl,
            Locale = "pt-BR",
            TimezoneId = "America/Sao_Paulo",
            ViewportSize = new() { Width = 390, Height = 844 },
            RecordVideoDir = _pastaVideoTemp,
            RecordVideoSize = new() { Width = 390, Height = 844 },
        };
        ConfigurarContexto(opcoes);
        Context = await App.Browser.NewContextAsync(opcoes);
        // Nada sai para fora do site (ex.: Clarity): resposta vazia, sem erro no console e sem sujar a análise real.
        await Context.RouteAsync(url => !url.StartsWith(App.BaseUrl), r => r.FulfillAsync(new() { Status = 200, Body = "" }));
        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        Page = await Context.NewPageAsync();
    }

    /// <summary>Ajustes do contexto antes de abrir o navegador (ex.: user agent de iPhone).</summary>
    protected virtual void ConfigurarContexto(BrowserNewContextOptions opcoes) { }

    public async ValueTask DisposeAsync()
    {
        var resultado = TestContext.Current.TestState?.Result;
        var guardar = resultado == TestResult.Failed
            || Environment.GetEnvironmentVariable("E2E_ARTEFATOS") == "sempre";

        try
        {
            if (guardar)
            {
                var pasta = PastaDoTeste();
                Directory.CreateDirectory(pasta);
                if (!Page.IsClosed)
                    await Page.ScreenshotAsync(new() { Path = Path.Combine(pasta, "print.png"), FullPage = true });
                await Context.Tracing.StopAsync(new() { Path = Path.Combine(pasta, "trace.zip") });

                var video = Page.Video;
                await Context.CloseAsync();
                if (video is not null)
                    await video.SaveAsAsync(Path.Combine(pasta, "video.webm"));
            }
            else
            {
                await Context.Tracing.StopAsync();
                await Context.CloseAsync();
            }
        }
        finally
        {
            try { Directory.Delete(_pastaVideoTemp, recursive: true); } catch (IOException) { }
        }
    }

    private string PastaDoTeste()
    {
        var nome = TestContext.Current.Test?.TestDisplayName ?? "sem-nome";
        foreach (var c in Path.GetInvalidFileNameChars().Append(' '))
            nome = nome.Replace(c, '_');
        if (nome.Length > 120) nome = nome[..120];
        return Path.GetFullPath(Path.Combine(PastaRaiz, App.NomeBrowser, nome));
    }
}
