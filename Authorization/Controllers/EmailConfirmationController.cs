using System.Linq;
using System.Threading.Tasks;
using Authorization.Dto;
using Authorization.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailConfirmationController(UserManager<User> userManager) : ControllerBase
    {
        // GET api/EmailConfirmation/ConfirmEmail?userId={id}&token={token}
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new ConfirmEmailResponseDto
                {
                    Success = false,
                    Message = "Некорректные параметры."
                });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new ConfirmEmailResponseDto
                {
                    Success = false,
                    Message = "Пользователь не найден."
                });
            }

            // !!! Убрали вызов HttpUtility.UrlDecode(token) !!!
            // Здесь `token` уже приходит из URL в виде, закодированном через HttpUtility.UrlEncode, 
            // и ASP.NET Core автоматически перевёл `%2B` → `+` и т.д. в оригинальные символы.
            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok(new ConfirmEmailResponseDto
                {
                    Success = true,
                    Message = "Email успешно подтверждён. Сейчас вы будете перенаправлены на главную."
                });
            }
            else
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new ConfirmEmailResponseDto
                {
                    Success = false,
                    Message = $"Не удалось подтвердить Email: {errors}"
                });
            }
        }
    }
}
