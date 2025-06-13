using Authorization.enums;

namespace Authorization.Models;

public class Invitation
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string UserEmail { get; set; } = null!;
    public CompanyRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Accepted { get; set; }

    public string RoleName { get; set; }
}