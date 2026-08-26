using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class ReceptionistBookingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a patient.")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Display(Name = "Doctor Name")]
        public string DoctorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appointment date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select a time.")]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";
    }
}