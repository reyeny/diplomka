using System.ComponentModel.DataAnnotations;

namespace Authorization.Dto;

public class RegisterRequestDto
{
    [Required]
    public string? Name { get; set; }
    [Required] 
    public string? Surname { get; set; }
    [Required]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }

}