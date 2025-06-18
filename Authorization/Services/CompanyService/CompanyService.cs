using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.CompanyService;

public class CompanyService(UnchainMeDbContext db) : ICompanyService
{
    private const int MaxCompaniesPerUser = 5;

    public async Task<CompanyDto> CreateAsync(string userId, CreateCompanyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Название компании не может быть пустым");
        if (dto.Name.Length > 100)
            throw new AppException("Название компании слишком длинное (максимум 100 символов)");

        var userCompaniesCount = await db.Companies.CountAsync(x => x.OwnerId == userId);
        if (userCompaniesCount >= MaxCompaniesPerUser)
            throw new AppException("Вы не можете создать больше 5 компаний");

        var exists = await db.Companies.AnyAsync(x => x.OwnerId == userId && x.Name == dto.Name);
        if (exists)
            throw new AppException("У вас уже есть компания с таким названием");

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            OwnerId = userId
        };
        db.Companies.Add(company);

        var ownerLink = new CompanyUser
        {
            CompanyId = company.Id,
            UserId = userId,
            Role = CompanyRole.Admin,
            InvitedAt = DateTime.UtcNow,
            AcceptedAt = DateTime.UtcNow
        };
        db.CompanyUsers.Add(ownerLink);

        await db.SaveChangesAsync();

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            OwnerId = company.OwnerId
        };
    }

    public async Task<IEnumerable<CompanyDto>> ListForUserAsync(string userId)
    {
        return await db.CompanyUsers
            .AsNoTracking()
            .Where(cu => cu.UserId == userId && cu.AcceptedAt != null)
            .Select(cu => new CompanyDto
            {
                Id = cu.CompanyId,
                Name = cu.Company.Name,
                OwnerId = cu.Company.OwnerId,
                RoleName = cu.Role
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<UserCompanyRoleDto>> GetRolesForUserAsync(string userId)
    {
        return await db.CompanyUsers
            .AsNoTracking()
            .Where(cu => cu.UserId == userId && cu.AcceptedAt != null)
            .Select(cu => new UserCompanyRoleDto
            {
                CompanyId = cu.CompanyId,
                RoleName = cu.Role.ToString()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<UserInCompanyDto>> ListUsersInCompanyAsync(Guid companyId)
    {
        return await db.CompanyUsers
            .AsNoTracking()
            .Where(cu => cu.CompanyId == companyId && cu.AcceptedAt != null)
            .Include(cu => cu.User) 
            .Select(cu => new UserInCompanyDto
            {
                UserId = cu.UserId,
                UserName = cu.User.UserName, 
                Email = cu.User.Email,
                RoleName = cu.Role.ToString(),
                AcceptedAt = cu.AcceptedAt
            })
            .ToListAsync();
    }

    public async Task RemoveUserAsync(Guid companyId, string userId)
    {
        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);

        if (company == null)
            throw new AppException("Компания не найдена");

        if (company.OwnerId == userId)
            throw new AppException("Владельца компании удалить нельзя");

        var link = await db.CompanyUsers
            .FirstOrDefaultAsync(cu =>
                cu.CompanyId == companyId &&
                cu.UserId    == userId &&
                cu.AcceptedAt != null);

        if (link == null)
            throw new AppException("Пользователь не состоит в этой компании");

        db.CompanyUsers.Remove(link);
        await db.SaveChangesAsync();
    }
}