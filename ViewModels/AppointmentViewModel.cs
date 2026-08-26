using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class AppointmentViewModel
    {
        public int DoctorId { get; set; }

        public string DoctorName { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appointment date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required.")]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a reason for the visit.")]
        [StringLength(300, ErrorMessage = "Reason cannot exceed 300 characters.")]
        [Display(Name = "Reason for Visit")]
        public string Reason { get; set; } = string.Empty;
    }
}