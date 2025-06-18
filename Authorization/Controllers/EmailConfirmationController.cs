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

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok(new ConfirmEmailResponseDto
                {
                    Success = true,
                    Message = "Email успешно подтверждён. Сейчас вы будете перенаправлены на главную."
                });
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new ConfirmEmailResponseDto
            {
                Success = false,
                Message = $"Не удалось подтвердить Email: {errors}"
            });
        }
    }
}
