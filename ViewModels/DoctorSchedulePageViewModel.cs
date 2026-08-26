using System.Collections.Generic;

namespace ClinicManagementSystem.ViewModels
{
    public class DoctorSchedulePageViewModel
    {
        public DoctorScheduleViewModel NewSchedule { get; set; } = new DoctorScheduleViewModel();
        public List<DoctorScheduleViewModel> ScheduleList { get; set; } = new List<DoctorScheduleViewModel>();
    }
}