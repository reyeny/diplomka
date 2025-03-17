using Authorization.Dto;

namespace Authorization.Services.AuthenticationServices;

public interface IAuthenticationService
{
    Task<AuthResponseDto> Register(RegisterRequestDto registerRequestDto);
    Task<AuthResponseDto> Login(LoginRequestDto loginRequestDto);
}