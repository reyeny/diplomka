namespace Authorization.Dto;

public class ConfirmEmailResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
