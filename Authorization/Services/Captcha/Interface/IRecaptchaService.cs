namespace Authorization.Services.Captcha.Interface;

public interface IRecaptchaService
{
    Task<bool> VerifyTokenAsync(string token);
}