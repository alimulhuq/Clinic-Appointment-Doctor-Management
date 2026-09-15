namespace ClinicManagementSystem.ViewModels
{
    public class DoctorRegistrationScheduleInput
    {
        public string Day { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";
    }
}