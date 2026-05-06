using FeriadoTracker.Web.Data;
using FeriadoTracker.Web.Dtos;
using FeriadoTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FeriadoTracker.Web.Controllers;

[ApiController]
[Route("api/push")]
public class PushController(AppDbContext db, TimeProvider time, IConfiguration config) : ControllerBase
{
    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var key = config["WebPush:VapidPublicKey"];
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

        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint, ct);

        if (existing is not null)
        {
            existing.P256dh = dto.P256dh;
            existing.Auth = dto.Auth;
        }
        else
        {
            db.PushSubscriptions.Add(new PushSubscription
            {
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                CreatedAt = time.GetUtcNow().UtcDateTime
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint))
        {
            return BadRequest();
        }

        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint, ct);

        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
