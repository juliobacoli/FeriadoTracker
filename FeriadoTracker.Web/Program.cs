using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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

var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<TimeProvider, BrazilTimeProvider>();
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

// Headers de segurança
app.Use(async (context, next) =>
{
    var nonceBytes = new byte[16];
    System.Security.Cryptography.RandomNumberGenerator.Fill(nonceBytes);
    var nonce = Convert.ToBase64String(nonceBytes);
    context.Items["csp-nonce"] = nonce;

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
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data: https://*.clarity.ms; " +
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
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
