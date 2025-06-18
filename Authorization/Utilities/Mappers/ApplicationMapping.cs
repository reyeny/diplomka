using Authorization.Dto;
using Authorization.enums;
using Authorization.Models;

namespace Authorization.Utilities.Mappers;

public static class ApplicationMapper
{
    public static ApplicationDto ToDto(this Application? a)
    {
        var assistant = a?.Company?.CompanyUsers?.FirstOrDefault(x => x.Role == CompanyRole.Assistant)?.User;
        var director  = a?.Company?.CompanyUsers?.FirstOrDefault(x => x.Role == CompanyRole.Director)?.User;

        return new ApplicationDto
        {
            Id            = a.Id,
            CompanyId     = a.CompanyId,
            Type          = a.Type.ToString(),
            CustomType    = a.CustomType,
            Comment       = a.Comment,
            Status        = a.Status.ToString(),
            CreatedAt     = a.CreatedAt,
            AssistantReviewedAt = a.AssistantReviewedAt,
            AssistantComment    = a.AssistantComment,
            DirectorReviewedAt  = a.DirectorReviewedAt,
            DirectorComment     = a.DirectorComment,

            CreatorId     = a.CreatedBy.Id,
            CreatorName   = $"{a.CreatedBy.Name} {a.CreatedBy.Surname}".Trim(),
            CreatorEmail  = a.CreatedBy.Email,

            AssistantId   = assistant?.Id,
            AssistantName = $"{assistant?.Name} {assistant?.Surname}".Trim(),
            AssistantEmail = assistant?.Email,

            DirectorId    = director?.Id,
            DirectorName  = $"{director?.Name} {director?.Surname}".Trim(),
            DirectorEmail = director?.Email,
        };
    }
}