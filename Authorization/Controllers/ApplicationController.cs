using Authorization.Dto;
using Authorization.Exceptions;
using Authorization.Models;
using Authorization.Services.ApplicationService;
using Authorization.Utilities.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies/{companyId:guid}/applications")]
    [Authorize]
    public class ApplicationController(IApplicationService svc) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(Guid companyId)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var apps = await svc.ListApplicationsWithUsersAsync(userId, companyId);
            return Ok(apps.Select(a => a.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid companyId, [FromBody] CreateApplicationDto dto)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var app = await svc.CreateApplicationAsync(userId, companyId, dto);
            return Ok(app.ToDto());
        }

        [HttpPost("{appId:guid}/assistant-review")]
        public async Task<IActionResult> AssistantReview(
            Guid companyId,
            Guid appId,
            [FromBody] AssistantReviewDto dto)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var app = await svc.AssistantReviewAsync(userId, appId, dto);
            return Ok(app.ToDto());
        }

        [HttpPost("{appId:guid}/director-review")]
        public async Task<IActionResult> DirectorReview(
            Guid companyId,
            Guid appId,
            [FromBody] DirectorReviewDto dto)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var app = await svc.DirectorReviewAsync(userId, appId, dto);
            return Ok(app.ToDto());
        }
    }
}
