using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic_Application_Doctor_Management.Models;

namespace ClinicManagementSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        [Required] public int TotalDoctors { get; set; }
        [Required] public int TotalReceptionists { get; set; }
        [Required] public int TotalPatients { get; set; }
        [Required] public int TotalAppointments { get; set; }

        public List<Appointment> AllAppointments { get; set; } = new List<Appointment>();
        public List<Patient> AllPatients { get; set; } = new List<Patient>();

        // Range selector
        public string SelectedRange { get; set; } = "week";

        // Chart data
        public string ChartTitle { get; set; } = "Weekly Patient Consultation Volume";
        public string ChartSubtitle { get; set; } = "Last 7 days — completed vs cancelled from database";
        public string[] ChartLabels { get; set; } = System.Array.Empty<string>();
        public int[] ChartCompleted { get; set; } = System.Array.Empty<int>();
        public int[] ChartCancelled { get; set; } = System.Array.Empty<int>();

        // Donut counts (range-scoped)
        public int RangeConfirmed { get; set; }
        public int RangePending { get; set; }
        public int RangeCompleted { get; set; }
        public int RangeCancelled { get; set; }
        public int RangeRejectedOther { get; set; }
        public int RangeTotal { get; set; }
    }
}