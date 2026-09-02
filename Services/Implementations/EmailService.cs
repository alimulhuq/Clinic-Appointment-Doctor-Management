using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Clinic_Application_Doctor_Management.Services.Implementations{
    public class EmailService : IEmailService{
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config){
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false){
            var smtpSettings = _config.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];

            // Guard against null values
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail)){
                throw new InvalidOperationException("SMTP settings are not configured properly.");
            }

            using var client = new SmtpClient(host, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);

            var mailMessage = new MailMessage{
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }
    }
}