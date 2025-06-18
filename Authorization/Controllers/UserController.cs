using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;
using Authorization.Context;
using Authorization.Dto;
using Authorization.Models;
using Authorization.Services.AuthenticationServices;
using Authorization.Services.Captcha.Interface;
using Authorization.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(
        IAuthenticationService authenticationService,
        UnchainMeDbContext dbContext,
        UserManager<User> userManager,
        IRecaptchaService recaptchaService,
        IUserService userService,
        IConfiguration configuration,
        ITelegramBotClient botClient)
        : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var isCaptchaValid = await recaptchaService.VerifyTokenAsync(request.RecaptchaToken);
            if (!isCaptchaValid)
                return BadRequest(new { message = "Не прошли проверку reCAPTCHA" });

            try
            {
                await authenticationService.Register(request);
                return Ok(new { message = "Ссылка для подтверждения отправлена на ваш Email." });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("уже существует"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера." });
            }
        }

        [AllowAnonymous]
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new { success = false, message = "Некорректные параметры." });

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest(new { success = false, message = "Пользователь не найден." });

            var decodedToken = HttpUtility.UrlDecode(token);
            var result = await userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded) return Ok(new { success = true, message = "Email успешно подтверждён." });
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message = $"Не удалось подтвердить Email: {errors}" });

        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [Obsolete("Obsolete")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var isCaptchaValid = await recaptchaService.VerifyTokenAsync(request.RecaptchaToken);
            if (!isCaptchaValid)
                return BadRequest(new { message = "Не прошли проверку reCAPTCHA" });

            var user = await userManager.FindByEmailAsync(request.Email!);
            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password!))
                return BadRequest(new { message = "Неверный логин или пароль." });

            if (!user.EmailConfirmed)
                return BadRequest(new { message = "Email не подтверждён. Перейдите по ссылке из письма." });

            if (string.IsNullOrWhiteSpace(user.TelegramChatId))
                return BadRequest(new
                    { message = "Telegram не привязан. Напишите боту свой email, чтобы связать аккаунт." });

            var isFirst2Fa = !user.HasUsed2FA;
            if (isFirst2Fa)
            {
                user.HasUsed2FA = true;
                await userManager.UpdateAsync(user);
            }

            var loginRequest = new LoginRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsApproved = false,
                Code = new Random().Next(1000, 9999).ToString()
            };
            dbContext.LoginRequests.Add(loginRequest);
            await dbContext.SaveChangesAsync();

            var chatId = long.Parse(user.TelegramChatId!);
            await botClient.SendTextMessageAsync(
                chatId,
                $"Ваш код для подтверждения входа: {loginRequest.Code}"
            );

            return Ok(new
            {
                requestId = loginRequest.Id,
                requiresVerification = true,
                requiresCode = true,
                message = "Код отправлен в Telegram. Введите его на сайте."
            });
        }

        [AllowAnonymous]
        [HttpPost("ConfirmEmailCode")]
        public Task<IActionResult> ConfirmEmailCode([FromBody] ConfirmLoginDto dto)
            => ConfirmLogin(dto);

        [AllowAnonymous]
        [HttpPost("ConfirmLogin")]
        public async Task<IActionResult> ConfirmLogin([FromBody] ConfirmLoginDto dto)
        {
            var lr = await dbContext.LoginRequests.FindAsync(dto.RequestId);
            if (lr == null || DateTime.UtcNow > lr.ExpiresAt)
                return BadRequest(new { message = "Запрос не найден или просрочен." });

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                if (lr.Code != dto.Code.Trim())
                    return BadRequest(new { message = "Неверный код." });
                lr.IsApproved = true;
                await dbContext.SaveChangesAsync();
            }

            if (!lr.IsApproved)
                return BadRequest(new { message = "Ожидайте подтверждения." });

            var user = await userManager.FindByIdAsync(lr.UserId);
            if (user == null)
                return BadRequest(new { message = "Пользователь не найден." });

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName!),
                new(ClaimTypes.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
            };
            var roles = await userManager.GetRolesAsync(user);
            authClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                issuer: configuration["JWT:ValidIssuer"],
                audience: configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddHours(1),
                claims: authClaims,
                signingCredentials: creds
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return Ok(new { token = tokenString });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<UserDto>> Post(UserDto userDto) =>
            Ok(await userService.PostUserAsync(userDto));

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> Get(string id) =>
            Ok(await userService.GetUserByIdAsync(id));

        [Authorize]
        [HttpPut]
        public async Task<ActionResult<UserDto>> Put(UserDto userDto) =>
            Ok(await userService.PutUserAsync(userDto));

        [Authorize]
        [HttpDelete]
        public async Task<ActionResult> Delete(string id)
        {
            await userService.DeleteUserAsync(id);
            return Ok("Объект был удален");
        }
    }
}