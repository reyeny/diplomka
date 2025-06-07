namespace Authorization.Dto;

public class ConfirmLoginDto
{
    public Guid RequestId { get; init; }
    public string? Code { get; init; }
}