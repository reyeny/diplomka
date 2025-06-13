using Authorization.Dto.Company;

namespace Authorization.Services.CompanyService;

public interface ICompanyService
{
    Task<CompanyDto> CreateAsync(string userId, CreateCompanyDto dto);
    Task<IEnumerable<CompanyDto>> ListForUserAsync(string userId);
    Task<IEnumerable<UserCompanyRoleDto>> GetRolesForUserAsync(string userId);

}