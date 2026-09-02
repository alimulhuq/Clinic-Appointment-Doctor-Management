using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class DoctorManagementViewModel{
        public int Id { get; set; }

        [Required, StringLength(100)]
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
        [Display(Name = "Experience (years)")]
        public int Experience { get; set; }
    }
}