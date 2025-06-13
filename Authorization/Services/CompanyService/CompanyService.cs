using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.CompanyService
{
    public class CompanyService(UnchainMeDbContext db) : ICompanyService
    {
        private const int MaxCompaniesPerUser = 5;

        public async Task<CompanyDto> CreateAsync(string userId, CreateCompanyDto dto)
        {
            // Проверка: имя не пустое и не слишком длинное
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new AppException("Название компании не может быть пустым");
            if (dto.Name.Length > 100)
                throw new AppException("Название компании слишком длинное (максимум 100 символов)");

            // Проверка: лимит компаний на пользователя
            var userCompaniesCount = await db.Companies.CountAsync(x => x.OwnerId == userId);
            if (userCompaniesCount >= MaxCompaniesPerUser)
                throw new AppException("Вы не можете создать больше 5 компаний");

            // Проверка: уникальность названия для пользователя
            var exists = await db.Companies.AnyAsync(x => x.OwnerId == userId && x.Name == dto.Name);
            if (exists)
                throw new AppException("У вас уже есть компания с таким названием");

            // Создаём компанию
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                OwnerId = userId
            };
            db.Companies.Add(company);

            // Добавляем владельца как Admin и сразу принимаем
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
    }
}
