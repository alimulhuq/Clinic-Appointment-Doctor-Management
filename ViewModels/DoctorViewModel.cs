using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorViewModel
    {
        public int DoctorId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Specialization { get; set; } = string.Empty;

        public string Qualification { get; set; } = string.Empty;

        public int Experience { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string AvailableDays { get; set; } = string.Empty;

        public string AvailableTime { get; set; } = string.Empty;
    }
}