using Authorization.enums;

namespace Authorization.Dto;

public class CreateApplicationDto
{
    public ApplicationType Type { get; set; }
    public string? CustomType { get; set; } 
    public string? Comment { get; set; }
}