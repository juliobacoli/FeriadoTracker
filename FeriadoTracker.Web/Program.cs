using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

if (args.Length > 0 && args[0] == "generate-vapid-keys")
{
    var keys = WebPush.VapidHelper.GenerateVapidKeys();
    Console.WriteLine($"VapidPublicKey:  {keys.PublicKey}");
    Console.WriteLine($"VapidPrivateKey: {keys.PrivateKey}");
    return;
}

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddAntiforgery();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("push", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    // Limite global (a aplicação não lê cabeçalhos de proxy, então não dá para separar por visitante).
    // Folgado de propósito: quem já avaliou não vê o formulário de novo, então o uso normal é de um envio por pessoa.
    options.AddFixedWindowLimiter("avaliacao", o =>
    {
        o.PermitLimit = 20;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

var dbPath = builder.Configuration["DB_PATH"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "feriados.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

var legacyDbPath = Path.Combine(builder.Environment.ContentRootPath, "feriados.db");
if (File.Exists(legacyDbPath) && !File.Exists(dbPath))
{
    File.Move(legacyDbPath, dbPath);
}

var keysPath = builder.Configuration["DATA_PROTECTION_KEYS_PATH"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("FeriadoTracker");

builder.Services.AddDbContextPool<AppDbContext>(
    options => options.UseSqlite($"Data Source={dbPath}"),
    poolSize: 8);

builder.Services.AddSingleton<TimeProvider, BrazilTimeProvider>();
builder.Services.AddSingleton<IWebPushClient, VapidWebPushClient>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IHolidayPushSender, HolidayPushSender>();
builder.Services.AddHostedService<HolidayNotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Endereço público do site (canonical, Open Graph, sitemap). Sem SITE_URL, usa o host da requisição.
var siteUrl = app.Configuration["SITE_URL"]?.Trim().TrimEnd('/');

// Headers de segurança
app.Use(async (context, next) =>
{
    var nonceBytes = new byte[16];
    System.Security.Cryptography.RandomNumberGenerator.Fill(nonceBytes);
    var nonce = Convert.ToBase64String(nonceBytes);
    context.Items["csp-nonce"] = nonce;
    context.Items["site-url"] = string.IsNullOrEmpty(siteUrl) ? $"https://{context.Request.Host}" : siteUrl;

    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy",
        "camera=(), microphone=(), geolocation=(), payment=()");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' 'strict-dynamic' https://*.clarity.ms; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "img-src 'self' data: https://*.clarity.ms https://c.bing.com; " +
            "connect-src 'self' https://*.clarity.ms; " +
            "worker-src 'self' blob:; " +
            "frame-src 'none'; " +
            "object-src 'none'; " +
            "base-uri 'self'");
    }

    await next();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

// SEO: o endereço absoluto vem de SITE_URL (ou do host da requisição quando não configurado).
app.MapGet("/robots.txt", (HttpContext contexto) => Results.Text(
    $"User-agent: *\nAllow: /\n\nSitemap: {contexto.Items["site-url"]}/sitemap.xml\n",
    "text/plain"));

app.MapGet("/sitemap.xml", (HttpContext contexto) => Results.Text(
    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
    "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
    $"  <url><loc>{contexto.Items["site-url"]}/</loc></url>\n" +
    "</urlset>\n",
    "application/xml"));

app.Run();
