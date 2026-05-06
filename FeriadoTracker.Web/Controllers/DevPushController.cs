using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeriadoTracker.Web.Controllers;

[ApiController]
[Route("api/dev/push")]
public class DevPushController(
    IHolidayPushSender sender,
    IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        var result = await sender.SendDailyAsync(ct);
        return Ok(result);
    }
}
