using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Services.CompanyService;
using Authorization.Utilities.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize]
    public class CompanyController(ICompanyService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyDto dto)
        {
            try
            {
                var userId = User.GetId();
                var company = await svc.CreateAsync(userId, dto);
                return Ok(company.ToDto());
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                var userId = User.GetId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                
                var list = await svc.ListForUserAsync(userId);
                return Ok(list.Select(c => c.ToDto()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка загрузки компаний", details = ex.Message });
            }
        }
        
        [HttpGet("roles-for-user")]
        public async Task<IActionResult> GetRolesForUser()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var roles = await svc.GetRolesForUserAsync(userId);
            return Ok(roles);
        }

        [HttpGet("{companyId}/users")]
        public async Task<IActionResult> GetCompanyUsers([FromRoute] Guid companyId)
        {
            try
            {
                var userId = User.GetId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();
                
                var users = await svc.ListUsersInCompanyAsync(companyId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при получении сотрудников", details = ex.Message });
            }
        }
        
        [HttpDelete("{companyId}/users/{userId}")]
        public async Task<IActionResult> RemoveUser(
            [FromRoute] Guid companyId,
            [FromRoute] string userId)
        {
            try
            {
                var currentUserId = User.GetId();
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized();

                var roles = await svc.GetRolesForUserAsync(currentUserId);
                if (!roles.Any(r => r.CompanyId == companyId && r.RoleName == CompanyRole.Admin.ToString()))
                    return Forbid();

                await svc.RemoveUserAsync(companyId, userId);
                return NoContent();
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при удалении сотрудника", details = ex.Message });
            }
        }
    }
}