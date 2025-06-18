using Authorization.enums;

namespace Authorization.Dto;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Type { get; set; }
    public string? CustomType { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? AssistantReviewedAt { get; set; }
    public string? AssistantComment { get; set; }
    public DateTime? DirectorReviewedAt { get; set; }
    public string? DirectorComment { get; set; }

    // --- Сотрудник, отправивший заявку ---
    public string? CreatorId { get; set; }
    public string CreatorName { get; set; }
    public string? CreatorEmail { get; set; }

    // --- Помощник, который рассматривал заявку (может быть null) ---
    public string? AssistantId { get; set; }
    public string? AssistantName { get; set; }
    public string? AssistantEmail { get; set; }

    // --- Директор, который рассматривал заявку (может быть null) ---
    public string? DirectorId { get; set; }
    public string? DirectorName { get; set; }
    public string? DirectorEmail { get; set; }
}
