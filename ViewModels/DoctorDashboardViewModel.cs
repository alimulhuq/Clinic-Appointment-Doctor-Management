using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class DoctorDashboardViewModel{
        [Required]
        [Display(Name = "Today's Appointments")]
        public int TodayAppointments { get; set; }

        [Required]
        [Display(Name = "Pending Appointments")]
        public int PendingAppointments { get; set; }

        [Required]
        [Display(Name = "Total Patients")]
        public int TotalPatients { get; set; }

        // List of recent/upcoming appointments for the dashboard table
        public List<DoctorAppointmentViewModel> RecentAppointments { get; set; } = new List<DoctorAppointmentViewModel>();
    }
}