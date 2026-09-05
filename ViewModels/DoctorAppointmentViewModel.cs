using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class DoctorAppointmentViewModel{
        [Required]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Patient ID")]
        public string PatientCode { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";

        // Doctor's name for patient-side views
        [Display(Name = "Doctor Name")]
        public string DoctorName { get; set; } = string.Empty;

        // Specialization for patient-side views
        [Display(Name = "Specialization")]
        public string Specialization { get; set; } = string.Empty;
    }
}