using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class PatientProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Guardian Name")]
        public string? GuardianName { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;
    }
}