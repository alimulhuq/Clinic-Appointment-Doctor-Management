using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.ViewModels;
using System.Linq;
using System.Collections.Generic;

namespace ClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private static List<DoctorManagementViewModel> doctors = new List<DoctorManagementViewModel>
        {
            new DoctorManagementViewModel { Id = 1, FullName = "Dr. Ahmed Rahman", Email = "ahmed@cliniccare.com", Phone = "01700000001", Specialization = "Cardiologist", Qualification = "MBBS, MD", Experience = 10 },
            new DoctorManagementViewModel { Id = 2, FullName = "Dr. Nusrat Jahan", Email = "nusrat@cliniccare.com", Phone = "01700000002", Specialization = "General Physician", Qualification = "MBBS, FCPS", Experience = 7 },
            new DoctorManagementViewModel { Id = 3, FullName = "Dr. Tanvir Hasan", Email = "tanvir@cliniccare.com", Phone = "01700000003", Specialization = "Dermatologist", Qualification = "MBBS, DDV", Experience = 8 }
        };
        private static List<ReceptionistManagementViewModel> receptionists = new List<ReceptionistManagementViewModel>
{
    new ReceptionistManagementViewModel { Id = 1, FullName = "Sadia Islam", Email = "sadia@cliniccare.com", Phone = "01799999999" }
};
        private static List<PatientListItemViewModel> patients = new List<PatientListItemViewModel>
{
    new PatientListItemViewModel { Id = 1, FullName = "Rahim Ahmed", PatientCode = "P001", Phone = "01711111111", Age = 34, Gender = "Male" },
    new PatientListItemViewModel { Id = 2, FullName = "Karim Hasan", PatientCode = "P002", Phone = "01722222222", Age = 28, Gender = "Male" },
    new PatientListItemViewModel { Id = 3, FullName = "Nusrat Akter", PatientCode = "P003", Phone = "01733333333", Age = 41, Gender = "Female" }
};
        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalDoctors = 3,
                TotalReceptionists = 1,
                TotalPatients = 4,
                TotalAppointments = 6
            };

            return View(model);
        }

        // GET: /Admin/Doctors
        public IActionResult Doctors()
        {
            return View(doctors);
        }

        // GET: /Admin/AddDoctor
        public IActionResult AddDoctor()
        {
            return View(new DoctorManagementViewModel());
        }

        // POST: /Admin/AddDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDoctor(DoctorManagementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Id = doctors.Count + 1;
            doctors.Add(model);

            TempData["SuccessMessage"] = $"Doctor {model.FullName} added successfully.";

            return RedirectToAction("Doctors");
        }
        // GET: /Admin/Receptionists
        public IActionResult Receptionists()
        {
            return View(receptionists);
        }

        // GET: /Admin/AddReceptionist
        public IActionResult AddReceptionist()
        {
            return View(new ReceptionistManagementViewModel());
        }

        // POST: /Admin/AddReceptionist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReceptionist(ReceptionistManagementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Id = receptionists.Count + 1;
            receptionists.Add(model);

            TempData["SuccessMessage"] = $"Receptionist {model.FullName} added successfully.";

            return RedirectToAction("Receptionists");
        }
        // GET: /Admin/Patients
        public IActionResult Patients()
        {
            return View(patients);
        }
    }
}