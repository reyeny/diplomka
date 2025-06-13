namespace Authorization.Dto.Company;

public class CreateTaskDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }

}