using System;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorScheduleViewModel
    {
        public int Id { get; set; }
        public string Day { get; set; } = string.Empty;         // "Monday" etc.
        public string StartTime { get; set; } = string.Empty;    // "10:00"
        public string EndTime { get; set; } = string.Empty;      // "13:00"
        public bool IsActive { get; set; } = true;
    }
}