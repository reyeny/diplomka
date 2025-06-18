using Authorization.Exceptions;
using Authorization.Services.ApplicationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/applications")]
    [Authorize]
    public class ApplicationStatsController(IApplicationService svc) : ControllerBase
    {
        [HttpGet("stats")]
        public async Task<IActionResult> GetUserApplicationsStats()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var stats = await svc.GetUserApplicationsStatsAsync(userId);
            return Ok(stats);
        }
    }
}