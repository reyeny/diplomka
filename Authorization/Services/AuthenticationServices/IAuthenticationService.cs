using System.Security.Claims;
using Authorization.Dto;

namespace Authorization.Services.AuthenticationServices;

public interface IAuthenticationService
{
    Task<AuthResponseDto> Register(RegisterRequestDto registerRequestDto);
    Task<AuthResponseDto> LoginImmediate(LoginRequestDto loginRequestDto);
}