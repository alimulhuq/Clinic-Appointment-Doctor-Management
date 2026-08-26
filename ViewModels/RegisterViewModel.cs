using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Name must be between 3 and 50 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;


        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }


        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Display(Name = "Guardian Name")]
        public string? GuardianName { get; set; }


        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;
    }
}