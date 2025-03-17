using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authorization.Dto;
using Authorization.Models;
using Authorization.Utilities.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Authorization.Services.AuthenticationServices;

public class AuthenticationService(UserManager<User> userManager, IConfiguration configuration)
    : IAuthenticationService
{
    public async Task<AuthResponseDto> Register(RegisterRequestDto registerRequestDto)
    {
        var userByEmail = await userManager.FindByEmailAsync(registerRequestDto.Email!);

        if (userByEmail is not null)
            throw new ArgumentException(
                $"Пользователь с {registerRequestDto.Email} уже существует.");

        var user = new User
        {
            Email = registerRequestDto.Email,
            Name = registerRequestDto.Name,
            Surname = registerRequestDto.Surname,
            UserName = registerRequestDto.Email
        };

        var result = await userManager.CreateAsync(user, registerRequestDto.Password!);

        if (!result.Succeeded)
            throw new ArgumentException(
                $"Невозможно зарегистрировать пользователя {registerRequestDto.Email}," +
                $"ошибка: {GetErrorsText(result.Errors)}");
        
        return await Login(new LoginRequestDto { Email = registerRequestDto.Email, 
            Password = registerRequestDto.Password });
    }

    public async Task<AuthResponseDto> Login(LoginRequestDto loginRequestDto)
    {
        var user = await userManager.FindByEmailAsync(loginRequestDto.Email!);

        if (user is null || !await userManager.CheckPasswordAsync(user, loginRequestDto.Password!))
            throw new ArgumentException(
                    $"Невозможно аутентифицировать пользователя {loginRequestDto.Email}");
        
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        var usersRoles = await userManager.GetRolesAsync(user);

        authClaims.AddRange(usersRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = GetToken(authClaims);

        return new AuthResponseDto { Token = new JwtSecurityTokenHandler()
            .WriteToken(token), UserDto = user.UserToUserDto(), UserRole = usersRoles};
    }
    
    private JwtSecurityToken GetToken(IEnumerable<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            issuer: configuration["JWT:ValidIssuer"],
            audience: configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey,
                SecurityAlgorithms.HmacSha256));

        return token;
    }

    
    private static string GetErrorsText(IEnumerable<IdentityError> errors)
        => string.Join(", ", errors.Select(error => error.Description).ToArray());
}