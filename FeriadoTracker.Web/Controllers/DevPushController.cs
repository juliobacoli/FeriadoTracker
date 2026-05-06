using FeriadoTracker.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeriadoTracker.Web.Controllers;

[ApiController]
[Route("api/dev/push")]
public class DevPushController : ControllerBase
{
    private readonly IHolidayPushSender _sender;
    private readonly IWebHostEnvironment _env;

    public DevPushController(IHolidayPushSender sender, IWebHostEnvironment env)
    {
        _sender = sender;
        _env = env;
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _sender.SendDailyAsync(ct);
        return Ok(result);
    }
}
