using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class UserProfileViewModel{
        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;
    }
}