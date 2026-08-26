using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class PatientDetailsViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Patient ID")]
        public string PatientCode { get; set; } = string.Empty;

        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Range(0, 120)]
        public int Age { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Known Allergies")]
        public string Allergies { get; set; } = "None recorded";

        [Required]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; } = string.Empty;

        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;
    }
}