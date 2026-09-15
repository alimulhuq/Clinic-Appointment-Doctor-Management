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

        // Full lists (used for donut + KPI context)
        public List<Appointment> AllAppointments { get; set; } = new List<Appointment>();
        public List<Patient> AllPatients { get; set; } = new List<Patient>();

        // ---------- Range selector ----------
        // "today" | "week" | "month"
        public string SelectedRange { get; set; } = "week";

        // ---------- Chart data (range-based) ----------
        public string ChartTitle { get; set; } = "Weekly Patient Consultation Volume";
        public string ChartSubtitle { get; set; } = "Last 7 days — completed vs cancelled from database";
        public string[] ChartLabels { get; set; } = System.Array.Empty<string>();
        public int[] ChartCompleted { get; set; } = System.Array.Empty<int>();
        public int[] ChartCancelled { get; set; } = System.Array.Empty<int>();

        // Donut counts (respect the selected range)
        public int RangeConfirmed { get; set; }
        public int RangePending { get; set; }
        public int RangeCompleted { get; set; }
        public int RangeRejectedOther { get; set; }
        public int RangeTotal { get; set; }
    }
}