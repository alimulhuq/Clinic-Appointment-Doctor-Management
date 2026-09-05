using System.Threading.Tasks;
using Clinic_Application_Doctor_Management.Models;

namespace Clinic_Application_Doctor_Management.Services.Interfaces{
    public interface IAppointmentService{
        Task<(bool IsValid, string? ErrorMessage)> ValidateAppointmentAsync(Appointment appointment);
    }
}