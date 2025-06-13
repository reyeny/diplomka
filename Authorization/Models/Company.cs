namespace Authorization.Models;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public User Owner { get; set; } = null!;

    public ICollection<CompanyUser> CompanyUsers { get; set; } = new List<CompanyUser>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}