using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic_Application_Doctor_Management.Models;

namespace ClinicManagementSystem.ViewModels{
    public class AdminDashboardViewModel{
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

        // All patient bookings (appointments) - used for status donut
        public List<Appointment> AllAppointments { get; set; } = new List<Appointment>();

        public List<Patient> AllPatients { get; set; } = new List<Patient>();

        // Weekly chart data (Mon–Sun), from database
        public int[] WeeklyCompleted { get; set; } = new int[7];
        public int[] WeeklyCancelled { get; set; } = new int[7];
    }
}