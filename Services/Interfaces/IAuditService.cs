using System.Threading.Tasks;

namespace Clinic_Application_Doctor_Management.Services.Interfaces{
    public interface IAuditService{
        Task LogAsync(string action, string entity, int? entityId, string details = "");
    }
}