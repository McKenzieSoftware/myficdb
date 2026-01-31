using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyFicDB.Web.Controllers.API
{
    [Authorize]
    [Route("api/v1/system")]
    public sealed class ApiSystemController : Controller
    {

        [HttpGet("uptime")]
        public IActionResult GetSystemUptime(CancellationToken cancellationToken)
        {
            var systemStartTimeUtc = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var uptime = DateTimeOffset.UtcNow - systemStartTimeUtc;
            var systemUptime = $"{uptime.Days} Days, {uptime.Hours} Hours, {uptime.Minutes} Minutes, {uptime.Seconds} Seconds";

            var response = new
            {
                Uptime = systemUptime
            };

            return Ok(response);
        }
    }
}
