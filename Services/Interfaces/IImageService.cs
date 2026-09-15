using Microsoft.AspNetCore.Http;

namespace Clinic_Application_Doctor_Management.Services.Interfaces
{
    public interface IImageService
    {
        Task<string?> SaveDoctorImageAsync(IFormFile file, int doctorId);
        void DeleteImage(string? relativeUrl);
    }
}