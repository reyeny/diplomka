
using Authorization.Dto.Company;
using Authorization.Exceptions;
using Authorization.Mappers;
using Authorization.Services.InvitationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies/{companyId:guid}/tasks")]
    [Authorize]
    public class TaskController(ITaskService svc) : ControllerBase
    {
        // GET api/companies/{companyId}/tasks
        [HttpGet]
        public async Task<IActionResult> List(Guid companyId)
        {
            var userId = User.GetId();
            var tasks  = await svc.ListTasksAsync(userId, companyId);
            return Ok(tasks.Select<TaskDto, object>(t => t.ToDto()));
        }

        // POST api/companies/{companyId}/tasks
        [Authorize(Roles = "Manager,Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(
            Guid companyId,
            [FromBody] CreateTaskDto dto)
        {
            var userId = User.GetId();
            var task   = await svc.CreateTaskAsync(userId, companyId, dto);
            return Ok(task.ToDto());
        }

        // POST api/companies/{companyId}/tasks/{taskId}/claim
        [Authorize(Roles = "Employee")]
        [HttpPost("{taskId:guid}/claim")]
        public async Task<IActionResult> Claim(Guid companyId, Guid taskId)
        {
            var userId = User.GetId();
            await svc.ClaimTaskAsync(userId, taskId);
            return Ok();
        }

        // PATCH api/companies/{companyId}/tasks/{taskId}/status
        [Authorize(Roles = "Employee")]
        [HttpPatch("{taskId:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid companyId,
            Guid taskId,
            [FromBody] ChangeTaskStatusDto dto)
        {
            var userId = User.GetId();
            await svc.ChangeStatusAsync(userId, taskId, dto);
            return Ok();
        }
    }
}
