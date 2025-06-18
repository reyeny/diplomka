namespace Authorization.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}