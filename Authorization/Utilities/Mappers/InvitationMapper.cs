using Authorization.Dto.Company;
using Authorization.Models;

namespace Authorization.Utilities.Mappers
{
    public static class InvitationMapper
    {
        public static InvitationDto ToDto(this Invitation invitation) => new()
        {
            Id = invitation.Id,
            CompanyId = invitation.CompanyId,
            UserEmail = invitation.UserEmail,
            Role = invitation.Role,
            Accepted = invitation.Accepted,
            CompanyName = invitation.Company?.Name ?? "",
            RoleName = invitation.Role.ToString() switch
            {
                "Admin" => "Администратор",
                "Manager" => "Менеджер",
                "Employee" => "Сотрудник",
                _ => invitation.Role.ToString()
            }
        };
    }
}