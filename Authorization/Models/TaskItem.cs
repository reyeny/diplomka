using Authorization.enums;

namespace Authorization.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public string CreatedById { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;

    public string? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public TaskItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}