using Authorization.enums;

namespace Authorization.Dto.Company;

public class TaskDto
{
     public Guid Id { get; set; }
     public Guid CompanyId { get; set; }
     public string Title { get; set; } = null!;
     public string? Description { get; set; }

     public string CreatedById { get; set; } = null!;
     public string? AssignedToId { get; set; }

     public TaskItemStatus Status { get; set; }

     public DateTime CreatedAt { get; set; }
     public DateTime UpdatedAt { get; set; }}