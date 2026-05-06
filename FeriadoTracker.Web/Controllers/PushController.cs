using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Controllers;

[ApiController]
[Route("api/push")]
public class PushController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;
    private readonly IConfiguration _config;

    public PushController(AppDbContext db, TimeProvider time, IConfiguration config)
    {
        _db = db;
        _time = time;
        _config = config;
    }

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var key = _config["WebPush:VapidPublicKey"];
        if (string.IsNullOrEmpty(key))
        {
            return NotFound();
        }
        return Ok(new { publicKey = key });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint)
            || string.IsNullOrWhiteSpace(dto.P256dh)
            || string.IsNullOrWhiteSpace(dto.Auth))
        {
            return BadRequest(new { error = "Campos obrigatórios ausentes." });
        }

        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh = dto.P256dh;
            existing.Auth = dto.Auth;
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                CreatedAt = _time.GetUtcNow().UtcDateTime
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint))
        {
            return BadRequest();
        }

        var sub = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint, ct);

        if (sub is not null)
        {
            _db.PushSubscriptions.Remove(sub);
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
