using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        [Required]
        [Display(Name = "Today's Appointments")]
        public int TodaysAppointments { get; set; }

        [Required]
        [Display(Name = "Total Patients")]
        public int TotalPatients { get; set; }

        [Required]
        [Display(Name = "Pending Confirmations")]
        public int PendingConfirmations { get; set; }
    }
}