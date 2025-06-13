using Authorization.Dto.Company;
using Authorization.Exceptions;
using Authorization.Mappers;
using Authorization.Services.CompanyService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize]
    public class CompanyController(ICompanyService svc) : ControllerBase
    {
        // POST api/companies
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
                // Возвращаем понятную ошибку для фронта
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Непредвиденная ошибка
                return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
            }
        }

        // GET api/companies
        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                var userId = User.GetId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                
                var list = await svc.ListForUserAsync(userId);
                // Можно вернуть массив компаний. Если их нет — фронт сам покажет "нет компаний".
                return Ok(list.Select(c => c.ToDto()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка загрузки компаний", details = ex.Message });
            }
        }
        
        // CompanyController.cs
        [HttpGet("roles-for-user")]
        public async Task<IActionResult> GetRolesForUser()
        {
            var userId = User.GetId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var roles = await svc.GetRolesForUserAsync(userId);
            return Ok(roles);
        }

    }
}