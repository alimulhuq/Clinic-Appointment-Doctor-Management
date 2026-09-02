using System.Threading.Tasks;

namespace Clinic_Application_Doctor_Management.Services.Interfaces{
    public interface IEmailService{
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    }
}