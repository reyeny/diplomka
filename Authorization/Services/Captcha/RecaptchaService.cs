using System.Text.Json;
using Authorization.Models;
using Authorization.Services.Captcha.Interface;
using Microsoft.Extensions.Options;

namespace Authorization.Services.Captcha;

public class RecaptchaSettings
{
    public string SecretKey { get; set; }
}

public class RecaptchaService(HttpClient httpClient, IOptions<RecaptchaSettings> options) : IRecaptchaService
{
    private readonly RecaptchaSettings _settings = options.Value;

    public async Task<bool> VerifyTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var secret = _settings.SecretKey;
        var url = $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}";

        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);

        return recaptchaResponse?.Success == true;
    }
}
