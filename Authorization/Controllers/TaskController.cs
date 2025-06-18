using Authorization.Dto.Company;
using Authorization.Exceptions;
using Authorization.Services.InvitationService;
using Authorization.Services.TaskService;
using Authorization.Utilities.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies/{companyId:guid}/tasks")]
    [Authorize]
    public class TaskController(ITaskService svc) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(Guid companyId)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var tasks = await svc.ListTasksAsync(userId, companyId);
            return Ok(tasks.Select(t => t.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            Guid companyId,
            [FromBody] CreateTaskDto dto)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var task = await svc.CreateTaskAsync(userId, companyId, dto);
                return Ok(task.ToDto());
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{taskId:guid}/claim")]
        public async Task<IActionResult> Claim(Guid companyId, Guid taskId)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await svc.ClaimTaskAsync(userId, taskId);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{taskId:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid companyId,
            Guid taskId,
            [FromBody] ChangeTaskStatusDto dto)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await svc.ChangeStatusAsync(userId, taskId, dto);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH api/companies/{companyId}/tasks/{taskId}/unassign
        // Сбросить задачу обратно в New — доступно только менеджеру и администратору
        [Authorize(Roles = "Manager,Admin")]
        [HttpPatch("{taskId:guid}/unassign")]
        public async Task<IActionResult> Unassign(Guid companyId, Guid taskId)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await svc.UnassignTaskAsync(userId, taskId);
                return NoContent();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Manager,Admin")]
        [HttpDelete("{taskId:guid}")]
        public async Task<IActionResult> Delete(Guid companyId, Guid taskId)
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await svc.DeleteTaskAsync(userId, taskId);
                return NoContent();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}