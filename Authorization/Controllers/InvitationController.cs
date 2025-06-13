using Authorization.Dto.Company;
using Authorization.enums;
using Authorization.Exceptions;
using Authorization.Mappers;
using Authorization.Models;
using Authorization.Services.InvitationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize]
    public class InvitationController(IInvitationService svc) : ControllerBase
    {
        // POST api/companies/{companyId}/invite
        [HttpPost("{companyId:guid}/invite")]
        public async Task<IActionResult> Invite(Guid companyId, [FromBody] CreateInvitationDto dto)
        {
            if (!Enum.IsDefined(typeof(CompanyRole), dto.Role))
                return BadRequest(new { message = "Роль не найдена!" });
            
            var inviterId = User.GetId();
            if (inviterId == null)
                return Unauthorized();
            
            var inviterRole = await svc.GetUserRoleInCompanyAsync(inviterId, companyId);
            if (inviterRole != CompanyRole.Admin)
                return Forbid("У вас нет прав приглашать сотрудников в эту компанию.");
            
            await svc.InviteAsync(inviterId, companyId, dto);
            return Ok();
        }

        // GET api/companies/invitations
        [HttpGet("invitations")]
        public async Task<IActionResult> MyInvitations()
        {
            var email = User.Identity!.Name!;
            var list = await svc.ListForUserAsync(email);
            return Ok(list);
        }

        // POST api/companies/invitations/{invId}/accept
        [HttpPost("invitations/{invId:guid}/accept")]
        public async Task<IActionResult> Accept(Guid invId)
        {
            var userId = User.GetId();
            await svc.AcceptAsync(userId!, invId);
            return Ok();
        }
    }
}
