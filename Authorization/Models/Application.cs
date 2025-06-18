using Authorization.enums;

namespace Authorization.Models;

public class Application
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string CreatedById { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;

    public ApplicationType Type { get; set; }
    public string? CustomType { get; set; } 
    public string? Comment { get; set; }

    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssistantReviewedAt { get; set; }
    public string? AssistantComment { get; set; }
    public DateTime? DirectorReviewedAt { get; set; }
    public string? DirectorComment { get; set; }
}