using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorScheduleViewModel
    {
        [Required(ErrorMessage = "Please select a day.")]
        public string? Day { get; set; }

        [Required(ErrorMessage = "Please select a start time.")]
        [Display(Name = "Start Time")]
        public string? StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end time.")]
        [Display(Name = "End Time")]
        public string? EndTime { get; set; }
    }
}