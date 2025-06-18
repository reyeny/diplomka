using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Models;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.InvitationService
{
    public class InvitationService(UnchainMeDbContext db) : IInvitationService
    {
        public async Task InviteAsync(string inviterId, Guid companyId, CreateInvitationDto dto)
        {
            var company = await db.Companies.FindAsync(companyId);
            if (company is null)
                throw new AppException("Компания не найдена");

            var invite = new Invitation
            {
                Id         = Guid.NewGuid(),
                CompanyId  = companyId,
                UserEmail  = dto.UserEmail,
                Role       = dto.Role,
                CreatedAt  = DateTime.UtcNow,
                Accepted   = false,
                RoleName   = dto.Role.ToString() switch
                {
                    nameof(CompanyRole.Admin)    => "Администратор",
                    nameof(CompanyRole.Manager)  => "Менеджер",
                    nameof(CompanyRole.Employee) => "Сотрудник",
                    _                            => dto.Role.ToString()
                }
            };

            db.Invitations.Add(invite);
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<InvitationDto>> ListForUserAsync(string userEmail)
        {
            var list = await db.Invitations
                .AsNoTracking()
                .Include(i => i.Company)
                .Where(i => i.UserEmail == userEmail && !i.Accepted)
                .Select(i => new InvitationDto
                {
                    Id          = i.Id,
                    CompanyId   = i.CompanyId,
                    UserEmail   = i.UserEmail,
                    Role        = i.Role,
                    Accepted    = i.Accepted,
                    CompanyName = i.Company.Name,
                    RoleName    = i.RoleName
                })
                .ToListAsync();

            return list;
        }

        public async Task AcceptAsync(string userId, Guid invitationId)
        {
            var inv = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId);
            if (inv is null)
                throw new AppException("Приглашение не найдено");

            if (inv.Accepted)
                throw new AppException("Приглашение уже принято");

            inv.Accepted = true;
            db.CompanyUsers.Add(new CompanyUser
            {
                CompanyId  = inv.CompanyId,
                UserId     = userId,
                Role       = inv.Role,
                InvitedAt  = inv.CreatedAt,
                AcceptedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<InvitationDto>> ListForCompanyAsync(Guid companyId)
        {
            var list = await db.Invitations
                .AsNoTracking()
                .Include(i => i.Company)
                .Where(i => i.CompanyId == companyId)
                .Where(i => i.Accepted == false)
                .Select(i => new InvitationDto
                {
                    Id          = i.Id,
                    CompanyId   = i.CompanyId,
                    UserEmail   = i.UserEmail,
                    Role        = i.Role,
                    Accepted    = i.Accepted,
                    CompanyName = i.Company.Name,
                    RoleName    = i.RoleName,
                    CreatedAt = i.CreatedAt.ToString("yyyy-MM-dd"),
                })
                .ToListAsync();

            return list;
        }

        public async Task CancelAsync(string inviterId, Guid invitationId)
        {
            var inv = await db.Invitations.FindAsync(invitationId);
            if (inv is null)
                throw new AppException("Приглашение не найдено");

            if (inv.Accepted)
                throw new AppException("Нельзя отменить уже принятое приглашение");

            db.Invitations.Remove(inv);
            await db.SaveChangesAsync();
        }

        public async Task<CompanyRole> GetUserRoleInCompanyAsync(string userId, Guid companyId)
        {
            var cu = await db.CompanyUsers
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId);
            if (cu is null)
                throw new AppException("Роль пользователя в компании не найдена");

            return cu.Role;
        }
    }
}
