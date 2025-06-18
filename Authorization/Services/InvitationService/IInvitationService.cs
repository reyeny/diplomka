using Authorization.Dto.Company;
using Authorization.enums;

namespace Authorization.Services.InvitationService
{
    public interface IInvitationService
    {
        Task InviteAsync(string inviterId, Guid companyId, CreateInvitationDto dto);
        Task<IEnumerable<InvitationDto>> ListForUserAsync(string userEmail);
        Task AcceptAsync(string userId, Guid invitationId);
        Task<CompanyRole> GetUserRoleInCompanyAsync(string userId, Guid companyId);
        Task CancelAsync(string inviterId, Guid invitationId);
        Task<IEnumerable<InvitationDto>> ListForCompanyAsync(Guid companyId);
    }
}