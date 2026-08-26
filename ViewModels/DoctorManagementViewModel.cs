using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorManagementViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Qualification is required.")]
        public string Qualification { get; set; } = string.Empty;

        [Required]
        [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years.")]
        [Display(Name = "Experience (years)")]
        public int Experience { get; set; }
    }
}