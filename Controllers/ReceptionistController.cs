using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ReceptionistController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var model = new ReceptionistDashboardViewModel
            {
                TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
                TotalPatients = await _context.Patients.CountAsync(),
                PendingConfirmations = await _context.Appointments.CountAsync(a => a.Status == "Pending")
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            ViewBag.Patients = await _context.Patients.ToListAsync();
            ViewBag.Doctors = await _context.Doctors.ToListAsync();
            return View(new ReceptionistBookingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(ReceptionistBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _context.Patients.ToListAsync();
                ViewBag.Doctors = await _context.Doctors.ToListAsync();
                return View(model);
            }

            // Check conflict
            var conflicting = await _context.Appointments
                .AnyAsync(a => a.DoctorId == model.DoctorId
                               && a.AppointmentDate == model.AppointmentDate
                               && a.AppointmentTime == TimeSpan.Parse(model.AppointmentTime)
                               && a.Status != "Cancelled");
            if (conflicting)
            {
                ModelState.AddModelError("", "This time slot is already booked.");
                ViewBag.Patients = await _context.Patients.ToListAsync();
                ViewBag.Doctors = await _context.Doctors.ToListAsync();
                return View(model);
            }

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId,
                PatientId = model.PatientId,
                AppointmentDate = model.AppointmentDate,
                AppointmentTime = TimeSpan.Parse(model.AppointmentTime),
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Appointment", appointment.Id, $"Booked by receptionist");

            TempData["SuccessMessage"] = $"Appointment booked for patient ID {model.PatientId}.";
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Appointments(DateTime? date, string status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (date.HasValue)
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var appointments = await query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToListAsync();

            var viewModels = appointments.Select(a => new ReceptionistBookingViewModel
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.FullName ?? "Unknown",
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor?.Name ?? "Unknown",
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm tt"),
                Reason = a.Reason ?? "",
                Status = a.Status
            }).ToList();

            ViewBag.FilterDate = date;
            ViewBag.FilterStatus = status;
            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var appointment = await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }
            appointment.Status = "Confirmed";
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Update", "Appointment", id, $"Confirmed by receptionist");

            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} confirmed.";
            return RedirectToAction("Appointments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }
            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Update", "Appointment", id, $"Cancelled by receptionist");

            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} cancelled.";
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Patients(string search, int? page)
        {
            var query = _context.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.FullName.Contains(search)
                                         || p.Phone.Contains(search)
                                         || (p.Email != null && p.Email.Contains(search)));
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;
            var patients = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModels = patients.Select(p => new PatientListItemViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                PatientCode = $"P{p.Id:D3}",
                Phone = p.Phone,
                Age = p.Age,
                Gender = p.Gender
            }).ToList();

            ViewBag.Search = search;
            ViewBag.Page = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);

            return View(viewModels);
        }

        public IActionResult AddPatient() => View(new PatientListItemViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPatient(PatientListItemViewModel model){
            if (!ModelState.IsValid) return View(model);

            var patient = new Patient{
                FullName = model.FullName,
                Phone = model.Phone,
                Age = model.Age,
                Gender = model.Gender,
                CreatedAt = DateTime.Now
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Patient", patient.Id, $"Patient {patient.FullName} added");

            TempData["SuccessMessage"] = $"Patient {model.FullName} added successfully.";
            return RedirectToAction("Patients");
        }

        public async Task<IActionResult> EditPatient(int id){
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null){
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }
            var model = new PatientListItemViewModel{
                Id = patient.Id,
                FullName = patient.FullName,
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(PatientListItemViewModel model){
            if (!ModelState.IsValid) return View(model);

            var patient = await _context.Patients.FindAsync(model.Id);
            if (patient == null){
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }
            patient.FullName = model.FullName;
            patient.Phone = model.Phone;
            patient.Age = model.Age;
            patient.Gender = model.Gender;
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Update", "Patient", patient.Id, $"Patient {patient.FullName} updated");

            TempData["SuccessMessage"] = $"Patient {patient.FullName} updated successfully.";
            return RedirectToAction("Patients");
        }
    }
}