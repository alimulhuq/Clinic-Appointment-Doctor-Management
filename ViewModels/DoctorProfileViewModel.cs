using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        public string Qualification { get; set; } = string.Empty;

        [Required, Range(0, 60)]
        public int Experience { get; set; }

        [Required, Range(0, 1000000)]
        [Display(Name = "Consultation Fee (৳)")]
        public decimal ConsultationFee { get; set; } = 500m;

        [Required]
        public string About { get; set; } = string.Empty;

        // ---------- Profile picture ----------
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }

        public string? ExistingProfilePicture { get; set; }

        public List<DoctorDayScheduleInput> Schedule { get; set; } = new();
    }

    public class DoctorDayScheduleInput
    {
        public string Day { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";
    }
}