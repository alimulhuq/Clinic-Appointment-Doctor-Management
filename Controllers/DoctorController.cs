using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Application_Doctor_Management.Controllers{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller{
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public DoctorController(ApplicationDbContext context, IAuditService audit){
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Dashboard(){
            // Get current doctor from logged-in user
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Doctor");

            var today = DateTime.Today;
            var model = new DoctorDashboardViewModel{
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == today),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor.Id && a.Status == "Pending"),
                TotalPatients = await _context.Appointments
                    .Where(a => a.DoctorId == doctor.Id)
                    .Select(a => a.PatientId)
                    .Distinct()
                    .CountAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> Appointments(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctor.Id)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var viewModels = appointments.Select(a => new DoctorAppointmentViewModel{
                Id = a.Id,
                PatientName = a.Patient?.FullName ?? "Unknown",
                PatientCode = $"P{a.PatientId:D3}",
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm tt"),
                Reason = a.Reason ?? "No reason",
                Status = a.Status
            }).ToList();

            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptAppointment(int id) => await UpdateAppointmentStatus(id, "Confirmed");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id) => await UpdateAppointmentStatus(id, "Rejected");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id) => await UpdateAppointmentStatus(id, "Completed");

        private async Task<IActionResult> UpdateAppointmentStatus(int id, string status){
            var appointment = await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null){
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }
            appointment.Status = status;
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Update", "Appointment", id, $"Appointment status changed to {status}");

            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} marked as {status}.";
            return RedirectToAction("Appointments");
        }

        // Prescriptions
        public async Task<IActionResult> Prescriptions(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items)
                .Where(p => p.DoctorId == doctor.Id)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return View(prescriptions);
        }

        public async Task<IActionResult> CreatePrescription(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View(new PrescriptionViewModel { DoctorId = doctor.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePrescription(PrescriptionViewModel model){
            if (!ModelState.IsValid){
                ViewBag.Patients = await _context.Patients.ToListAsync();
                return View(model);
            }

            var prescription = new Prescription{
                DoctorId = model.DoctorId,
                PatientId = model.PatientId,
                PrescriptionDate = DateTime.Now,
                Notes = model.Notes
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            foreach (var item in model.Items){
                var prescriptionItem = new PrescriptionItem{
                    PrescriptionId = prescription.Id,
                    MedicineName = item.MedicineName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Instructions = item.Instructions
                };
                _context.PrescriptionItems.Add(prescriptionItem);
            }
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Prescription", prescription.Id, $"Prescription created for patient {prescription.PatientId}");

            TempData["SuccessMessage"] = "Prescription created successfully.";
            return RedirectToAction("Prescriptions");
        }

        // Schedule
        public async Task<IActionResult> Schedule(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var schedules = await _context.Schedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
            return View(schedules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(Schedule model){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return Json(new { success = false, message = "Doctor not found" });

            model.DoctorId = doctor.Id;
            _context.Schedules.Add(model);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Schedule", model.Id, $"Schedule added for {model.Day}");

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSchedule(int id){
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null){
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                await _audit.LogAsync("Delete", "Schedule", id, "Schedule removed");
            }
            return Json(new { success = true });
        }

        // Profile
        public async Task<IActionResult> Profile(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Account");

            var model = new DoctorProfileViewModel{
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                About = doctor.About ?? ""
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model){
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Account");

            doctor.Name = model.FullName;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.Specialization = model.Specialization;
            doctor.Qualification = model.Qualification;
            doctor.Experience = model.Experience;
            doctor.About = model.About;

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Doctor", doctor.Id, "Profile updated");

            ViewBag.SuccessMessage = "Profile updated successfully.";
            return View(model);
        }
    }
}