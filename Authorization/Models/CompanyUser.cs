using Authorization.enums;

namespace Authorization.Models;

public class CompanyUser
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public CompanyRole Role { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}