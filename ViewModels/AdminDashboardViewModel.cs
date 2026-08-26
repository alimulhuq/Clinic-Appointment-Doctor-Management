using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        [Required]
        [Display(Name = "Total Doctors")]
        public int TotalDoctors { get; set; }

        [Required]
        [Display(Name = "Total Receptionists")]
        public int TotalReceptionists { get; set; }

        [Required]
        [Display(Name = "Total Patients")]
        public int TotalPatients { get; set; }

        [Required]
        [Display(Name = "Total Appointments")]
        public int TotalAppointments { get; set; }
    }
}