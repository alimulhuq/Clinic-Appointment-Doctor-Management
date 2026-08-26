using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.ViewModels;
using System.Linq;
using System.Collections.Generic;
namespace ClinicManagementSystem.Controllers
{
    public class ReceptionistController : Controller
    {
        // GET: /Receptionist/Dashboard
        public IActionResult Dashboard()
        {
            var model = new ReceptionistDashboardViewModel
            {
                TodaysAppointments = 12,
                TotalPatients = 340,
                PendingConfirmations = 4
            };

            return View(model);
        }
        private static List<PatientListItemViewModel> patients = new List<PatientListItemViewModel>
{
    new PatientListItemViewModel { Id = 1, FullName = "Rahim Ahmed", PatientCode = "P001", Phone = "01711111111", Age = 34, Gender = "Male" },
    new PatientListItemViewModel { Id = 2, FullName = "Karim Hasan", PatientCode = "P002", Phone = "01722222222", Age = 28, Gender = "Male" },
    new PatientListItemViewModel { Id = 3, FullName = "Nusrat Akter", PatientCode = "P003", Phone = "01733333333", Age = 41, Gender = "Female" }
};
        private static List<(int Id, string Name)> doctorList = new List<(int, string)>
{
    (1, "Dr. Ahmed Rahman - Cardiologist"),
    (2, "Dr. Nusrat Jahan - General Physician"),
    (3, "Dr. Tanvir Hasan - Dermatologist")
};

        private static List<ReceptionistBookingViewModel> bookedAppointments = new List<ReceptionistBookingViewModel>();

        // GET: /Receptionist/BookAppointment
        public IActionResult BookAppointment()
        {
            ViewBag.Patients = patients;
            ViewBag.Doctors = doctorList;
            return View(new ReceptionistBookingViewModel());
        }

        // POST: /Receptionist/BookAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(ReceptionistBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = patients;
                ViewBag.Doctors = doctorList;
                return View(model);
            }

            model.Id = bookedAppointments.Count + 1;
            model.PatientName = patients.FirstOrDefault(p => p.Id == model.PatientId)?.FullName ?? "Unknown";
            model.DoctorName = doctorList.FirstOrDefault(d => d.Id == model.DoctorId).Name ?? "Unknown";
            model.Status = "Pending";

            bookedAppointments.Add(model);

            TempData["SuccessMessage"] = $"Appointment booked for {model.PatientName}.";

            return RedirectToAction("Dashboard");
        }
        // GET: /Receptionist/Appointments
        public IActionResult Appointments()
        {
            return View(bookedAppointments);
        }

        // POST: /Receptionist/ConfirmAppointment/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmAppointment(int id)
        {
            var appointment = bookedAppointments.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            appointment.Status = "Confirmed";
            TempData["SuccessMessage"] = $"Appointment for {appointment.PatientName} confirmed.";

            return RedirectToAction("Appointments");
        }

        // POST: /Receptionist/CancelAppointment/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id)
        {
            var appointment = bookedAppointments.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            appointment.Status = "Cancelled";
            TempData["SuccessMessage"] = $"Appointment for {appointment.PatientName} cancelled.";

            return RedirectToAction("Appointments");
        }
        // GET: /Receptionist/Patients
        public IActionResult Patients(string? search)
        {
            var result = patients.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                result = result.Where(p =>
                    p.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.PatientCode.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.Search = search;

            return View(result.ToList());
        }
        // GET: /Receptionist/AddPatient
        public IActionResult AddPatient()
        {
            return View(new PatientListItemViewModel());
        }

        // POST: /Receptionist/AddPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPatient(PatientListItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Id = patients.Count + 1;
            model.PatientCode = $"P{model.Id:000}";

            patients.Add(model);

            TempData["SuccessMessage"] = $"Patient {model.FullName} added successfully.";

            return RedirectToAction("Patients");
        }
        // GET: /Receptionist/EditPatient/2
        public IActionResult EditPatient(int id)
        {
            var patient = patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }

            return View(patient);
        }

        // POST: /Receptionist/EditPatient/2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPatient(PatientListItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var patient = patients.FirstOrDefault(p => p.Id == model.Id);

            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }

            patient.FullName = model.FullName;
            patient.Phone = model.Phone;
            patient.Age = model.Age;
            patient.Gender = model.Gender;

            TempData["SuccessMessage"] = $"Patient {patient.FullName} updated successfully.";

            return RedirectToAction("Patients");
        }
    }
}