using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Application_Doctor_Management.Models{
    public class Schedule{
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}