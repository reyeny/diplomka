using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;
using Authorization.Dto;
using Authorization.Models;
using Authorization.Services.EmailSenderConfirm.Interfaces;
using Authorization.Utilities.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Authorization.Services.AuthenticationServices;

public class AuthenticationService(
    UserManager<User> userManager,
    IConfiguration configuration,
    IEmailSender emailSender)
    : IAuthenticationService
{
    public async Task<AuthResponseDto> Register(RegisterRequestDto registerRequestDto)
    {
        var existing = await userManager.FindByEmailAsync(registerRequestDto.Email!);
        if (existing != null)
            throw new ArgumentException($"Пользователь с {registerRequestDto.Email} уже существует.");

        var user = new User
        {
            Email = registerRequestDto.Email,
            Name = registerRequestDto.Name,
            Surname = registerRequestDto.Surname,
            UserName = registerRequestDto.Email,
            EmailConfirmed = false
        };
        
        var result = await userManager.CreateAsync(user, registerRequestDto.Password!);
        if (!result.Succeeded)
            throw new ArgumentException($"Невозможно зарегистрировать пользователя {registerRequestDto.Email}.");
        
        await userManager.AddToRoleAsync(user, "Admin");

        // Генерируем токен подтверждения Email
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = HttpUtility.UrlEncode(token);
        var link = $"{configuration["Frontend:BaseUrl"]}/#/confirm-email?userId={user.Id}&token={encoded}";
        var subject = "Подтвердите ваш Email";
        var body = $@"<p>Здравствуйте, {user.Name}!</p>
                      <p>Перейдите по ссылке: <a href=""{link}"">Подтвердить Email</a></p>";
        Console.WriteLine($"[EMAIL BODY]: {body}");

        // Отправляем письмо
        await emailSender.SendEmailAsync(user.Email!, subject, body);

        return new AuthResponseDto
        {
            Token = string.Empty,
            UserDto = user.UserToUserDto(),
            UserRole = new List<string>()
        };
    }

    public async Task<AuthResponseDto> LoginImmediate(LoginRequestDto loginRequestDto)
    {
        var user = await userManager.FindByEmailAsync(loginRequestDto.Email!);
        if (user == null)
            throw new ArgumentException($"Пользователь {loginRequestDto.Email} не найден.");

        if (!user.EmailConfirmed)
            throw new ArgumentException("Email не подтверждён.");

        if (!await userManager.CheckPasswordAsync(user, loginRequestDto.Password!))
            throw new ArgumentException("Неверный пароль.");

        if (string.IsNullOrWhiteSpace(user.TelegramChatId!))
            throw new ArgumentException("Telegram не привязан.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwt = BuildToken(claims);
        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            UserDto = user.UserToUserDto(),
            UserRole = roles
        };
    }

    private JwtSecurityToken BuildToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));
        return new JwtSecurityToken(
            issuer: configuration["JWT:ValidIssuer"],
            audience: configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddHours(1),
            claims: claims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
    }
}
