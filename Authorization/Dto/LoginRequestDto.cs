using System.ComponentModel.DataAnnotations;

namespace Authorization.Dto;

public class LoginRequestDto
{
    [Required]
    public string? Email { get; init; }
    [Required]
    public string? Password { get; init; }
    
    public string RecaptchaToken { get; set; }
    public string? TelegramChatId { get; init; }
    
}