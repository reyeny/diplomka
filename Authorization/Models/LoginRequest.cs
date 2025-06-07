namespace Authorization.Models;

public class LoginRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string? Code { get; set; }
    public bool IsApproved { get; set; }
    public DateTime ExpiresAt { get; set; }
}