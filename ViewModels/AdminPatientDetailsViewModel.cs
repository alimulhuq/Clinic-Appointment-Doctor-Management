using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class AdminPatientDetailsViewModel{
        public int Id { get; set; }

        [Display(Name = "Patient Code")]
        public string PatientCode { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Age")]
        public int Age { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Medical History")]
        public string MedicalHistory { get; set; } = string.Empty;

        [Display(Name = "Allergies")]
        public string Allergies { get; set; } = "None recorded";

        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; } = string.Empty;

        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; } = string.Empty;

        [Display(Name = "Emergency Contact Phone")]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        [Display(Name = "Registered On")]
        public DateTime CreatedAt { get; set; }
    }
}