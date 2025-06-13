using Authorization.enums;

namespace Authorization.Dto.Company;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public CompanyRole RoleName { get; set; }
}