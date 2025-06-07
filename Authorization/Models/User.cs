using Microsoft.AspNetCore.Identity;

namespace Authorization.Models;

public class User : IdentityUser
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? TelegramChatId { get; set; }
    public bool HasUsed2FA { get; set; }
}