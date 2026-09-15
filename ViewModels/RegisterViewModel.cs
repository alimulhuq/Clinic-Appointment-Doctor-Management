using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please choose account type.")]
        [Display(Name = "Register As")]
        public string SelectedRole { get; set; } = "Patient";

        // ---------- SHARED ----------
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // ---------- PATIENT ----------
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "Guardian Name")]
        public string? GuardianName { get; set; }

        public string? Address { get; set; }

        // ---------- DOCTOR ----------
        public string? Specialization { get; set; }
        public string? Qualification { get; set; }

        [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60.")]
        [Display(Name = "Experience (years)")]
        public int? Experience { get; set; }

        [StringLength(500)]
        public string? About { get; set; }

        // ---------- DOCTOR SCHEDULE (optional) ----------
        public List<DoctorRegistrationScheduleInput> Schedule { get; set; } = new();
    }
}