namespace Authorization.Services.EmailSenderConfirm.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
}