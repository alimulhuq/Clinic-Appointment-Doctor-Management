using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic_Application_Doctor_Management.Models;

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

        // All patient bookings (appointments)
        public List<Appointment> AllAppointments { get; set; } = new List<Appointment>();

        // NEW: All registered patients
        public List<Patient> AllPatients { get; set; } = new List<Patient>();
    }
}