using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Authorization.Services.EmailSenderConfirm
{
    public class SmtpEmailSender(IConfiguration configuration) : IEmailSender, Interfaces.IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpHost = configuration["Smtp:Host"];      
            var smtpPort = int.Parse(configuration["Smtp:Port"]!);       
            var smtpUser = configuration["Smtp:Username"];  
            var smtpPass = configuration["Smtp:Password"];  
            var fromEmail = configuration["Smtp:FromEmail"]; 

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail!),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            using var smtpClient = new SmtpClient(smtpHost!, smtpPort);
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPass);
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;

            try
            {
                // await smtpClient.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmtpEmailSender] Не удалось отправить письмо на «{toEmail}»: {ex.Message}");
                throw; 
            }

            return Task.CompletedTask;
        }
    }
}
