using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorAppointmentViewModel
    {
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
    }
}