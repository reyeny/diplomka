namespace Authorization.Dto.Company;

public class UserInCompanyDto
{
    public string UserId { get; set; }       
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string RoleName { get; set; }
    public DateTime? AcceptedAt { get; set; }
}