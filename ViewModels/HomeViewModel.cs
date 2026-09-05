using Clinic_Application_Doctor_Management.Models;

namespace ClinicManagementSystem.ViewModels
{
    public class HomeViewModel
    {
        // List of all doctors for the "Featured Doctors" section
        public List<Doctor> FeaturedDoctors { get; set; } = new List<Doctor>();

        // List of specializations (departments) for dropdown
        public List<string> Departments { get; set; } = new List<string>();

        // List of doctors for the quick search dropdown
        public List<Doctor> AllDoctors { get; set; } = new List<Doctor>();
    }
}