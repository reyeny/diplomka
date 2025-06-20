using Authorization.Dto.Company;
using Authorization.Models;

namespace Authorization.Utilities.Mappers
{
    public static class TaskItemMapper
    {
        public static TaskItem ToDto(this TaskDto t) => new()
        {
            Id           = t.Id,
            CompanyId    = t.CompanyId,
            Title        = t.Title,
            Description  = t.Description,
            CreatedById  = t.CreatedById,
            AssignedToId = t.AssignedToId,
            Status       = t.Status,
            CreatedAt    = t.CreatedAt,
            UpdatedAt    = t.UpdatedAt
        };
    }
}