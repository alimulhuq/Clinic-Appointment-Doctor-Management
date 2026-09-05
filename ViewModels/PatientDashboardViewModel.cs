using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels{
    public class PatientDashboardViewModel{
        [Required]
        [Display(Name = "Upcoming Appointments")]
        public int UpcomingAppointments { get; set; }

        [Required]
        [Display(Name = "Total Visits")]
        public int TotalVisits { get; set; }

        [Required]
        [Display(Name = "Pending Bills")]
        public int PendingBills { get; set; }

        [Required]
        [Display(Name = "Available Doctors")]
        public int AvailableDoctors { get; set; }

        // List of recent appointments for the dashboard table
        public List<DoctorAppointmentViewModel> RecentAppointments { get; set; } = new List<DoctorAppointmentViewModel>();
    }
}