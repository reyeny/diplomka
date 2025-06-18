using Authorization.enums;

namespace Authorization.Dto.Company;

public class InvitationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string UserEmail { get; set; } = null!;
    public CompanyRole Role { get; set; }
    public bool Accepted { get; set; }
    
    public string CompanyName { get; set; }  
    public string RoleName { get; set; }         
    public string CreatedAt { get; set; }

    
    
}