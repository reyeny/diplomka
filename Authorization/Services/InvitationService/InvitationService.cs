using Authorization.Context;
using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.InvitationService;

public class InvitationService(UnchainMeDbContext db, UserManager<User> users) : IInvitationService
{
    private readonly UserManager<User> _users = users;

    public async Task InviteAsync(string inviterId, Guid companyId, CreateInvitationDto dto)
    {
        var invite = new Invitation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserEmail = dto.UserEmail,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            Accepted = false,
            RoleName = dto.Role.ToString() switch
            {
                "Admin" => "Администратор",
                "Manager" => "Менеджер",
                "Employee" => "Сотрудник",
                _ => dto.Role.ToString()
            }
        };
            
            
        db.Invitations.Add(invite);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<InvitationDto>> ListForUserAsync(string userEmail)
    {
        // Сначала загружаем данные без вычислений
        var invitations = await db.Invitations
            .AsNoTracking()
            .Where(i => i.UserEmail == userEmail && !i.Accepted)
            .Select(i => new InvitationDto
            {
                Id = i.Id,
                CompanyId = i.CompanyId,
                UserEmail = i.UserEmail,
                Role = i.Role,
                Accepted = i.Accepted,
                CompanyName = i.Company.Name
            })
            .ToListAsync();

        foreach (var invitation in invitations)
        {
            invitation.RoleName = invitation.Role switch
            {
                CompanyRole.Admin => "Администратор",
                CompanyRole.Manager => "Менеджер",
                CompanyRole.Employee => "Сотрудник",
                _ => invitation.Role.ToString()
            };
        }

        return invitations;
    }


    public async Task AcceptAsync(string userId, Guid invitationId)
    {
        var inv = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId);
        if (inv == null)
            throw new ArgumentException("Invitation not found");
        inv.Accepted = true;

        db.CompanyUsers.Add(new CompanyUser
        {
            CompanyId = inv.CompanyId,
            UserId = userId,
            Role = inv.Role,
            InvitedAt = inv.CreatedAt,
            AcceptedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
        
    public async Task<CompanyRole> GetUserRoleInCompanyAsync(string userId, Guid companyId)
    {
        var companyUser = await db.CompanyUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);

        if (companyUser == null)
            throw new UnauthorizedAccessException("Пользователь не состоит в этой компании");

        return companyUser.Role;
    }

}