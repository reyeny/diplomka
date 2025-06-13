using Authorization.enums;

namespace Authorization.Dto.Company;

public class CreateInvitationDto
{
    public string UserEmail { get; set; }
    public CompanyRole Role { get; set; }
}