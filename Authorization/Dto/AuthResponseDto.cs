namespace Authorization.Dto;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required UserDto? UserDto { get; set; }
    public required IList<string> UserRole { get; set; }
}