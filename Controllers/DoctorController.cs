using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Application_Doctor_Management.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly IImageService _imageService;

        public DoctorController(ApplicationDbContext context, IAuditService audit, IImageService imageService)
        {
            _context = context;
            _audit = audit;
            _imageService = imageService;
        }

        // ---------------- DASHBOARD ----------------
        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Doctor");

            var today = DateTime.Today;

            var model = new DoctorDashboardViewModel
            {
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor!.Id && a.AppointmentDate.Date == today),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctor!.Id && a.Status == "Pending"),
                TotalPatients = await _context.Appointments
                    .Where(a => a.DoctorId == doctor!.Id)
                    .Select(a => a.PatientId).Distinct().CountAsync(),
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor!.Id && a.AppointmentDate >= today)
                    .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
                    .Take(5)
                    .Select(a => new DoctorAppointmentViewModel
                    {
                        Id = a.Id,
                        PatientName = a.Patient.FullName,
                        PatientCode = "P" + a.PatientId.ToString("D3"),
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                        Reason = a.Reason ?? "",
                        Status = a.Status
                    }).ToListAsync()
            };

            return View(model);
        }

        // ---------------- APPOINTMENTS ----------------
        public async Task<IActionResult> Appointments()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctor.Id)
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var viewModels = appointments.Select(a => new DoctorAppointmentViewModel
            {
                Id = a.Id,
                PatientName = a.Patient?.FullName ?? "Unknown",
                PatientCode = "P" + a.PatientId.ToString("D3"),
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                Reason = a.Reason ?? "No reason",
                Status = a.Status
            }).ToList();

            return View(viewModels);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptAppointment(int id) => await UpdateAppointmentStatus(id, "Confirmed");

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id) => await UpdateAppointmentStatus(id, "Rejected");

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id) => await UpdateAppointmentStatus(id, "Completed");

        private async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Appointment", id, $"Appointment status changed to {status}");
            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} marked as {status}.";
            return RedirectToAction("Appointments");
        }

        // ---------------- PRESCRIPTIONS ----------------
        public async Task<IActionResult> Prescriptions()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient).Include(p => p.Items)
                .Where(p => p.DoctorId == doctor.Id)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            ViewBag.MyPicture = doctor.ProfilePicture;
            ViewBag.MyName = doctor.Name;
            ViewBag.MySpecialization = doctor.Specialization;

            return View(prescriptions);
        }

        public async Task<IActionResult> CreatePrescription()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            ViewBag.Patients = await _context.Patients.ToListAsync();
            return View(new PrescriptionViewModel { DoctorId = doctor.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePrescription(PrescriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _context.Patients.ToListAsync();
                return View(model);
            }

            var prescription = new Prescription
            {
                DoctorId = model.DoctorId,
                PatientId = model.PatientId,
                PrescriptionDate = DateTime.Now,
                Notes = model.Notes
            };
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            foreach (var item in model.Items)
            {
                _context.PrescriptionItems.Add(new PrescriptionItem
                {
                    PrescriptionId = prescription.Id,
                    MedicineName = item.MedicineName,
                    Dosage = item.Dosage,
                    Frequency = item.Frequency,
                    Duration = item.Duration,
                    Instructions = item.Instructions
                });
            }
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Prescription", prescription.Id, $"Prescription created for patient {prescription.PatientId}");
            TempData["SuccessMessage"] = "Prescription created successfully.";
            return RedirectToAction("Prescriptions");
        }

        // ---------------- SCHEDULE ----------------
        public async Task<IActionResult> Schedule()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile");

            var schedules = await _context.Schedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
            return View(schedules);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(Schedule model)
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return Json(new { success = false, message = "Doctor not found" });

            model.DoctorId = doctor.Id;
            _context.Schedules.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Schedule", model.Id, $"Schedule added for {model.DayOfWeek}");
            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                await _audit.LogAsync("Delete", "Schedule", id, "Schedule removed");
            }
            return Json(new { success = true });
        }

        // ---------------- PROFILE ----------------
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Account");

            var model = new DoctorProfileViewModel
            {
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                ConsultationFee = doctor.ConsultationFee,
                About = doctor.About ?? "",
                ExistingProfilePicture = doctor.ProfilePicture
            };

            var existing = await _context.Schedules
                .Where(s => s.DoctorId == doctor.Id && s.IsActive)
                .ToListAsync();

            var allDays = Enum.GetValues<DayOfWeek>().OrderBy(d => ((int)d + 1) % 7);
            foreach (var day in allDays)
            {
                var match = existing.FirstOrDefault(s => s.DayOfWeek == day);
                model.Schedule.Add(new DoctorDayScheduleInput
                {
                    Day = day.ToString(),
                    IsEnabled = match != null,
                    StartTime = match != null ? match.StartTime.ToString(@"hh\:mm") : "09:00",
                    EndTime = match != null ? match.EndTime.ToString(@"hh\:mm") : "17:00"
                });
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == userEmail);
            if (doctor == null) return RedirectToAction("Profile", "Account");

            // ---- Profile picture upload ----
            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                try
                {
                    var newUrl = await _imageService.SaveDoctorImageAsync(model.ProfilePictureFile, doctor.Id);
                    if (!string.IsNullOrEmpty(newUrl))
                    {
                        _imageService.DeleteImage(doctor.ProfilePicture);
                        doctor.ProfilePicture = newUrl;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ProfilePictureFile", ex.Message);
                    model.ExistingProfilePicture = doctor.ProfilePicture;
                    return View(model);
                }
            }

            doctor.Name = model.FullName;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.Specialization = model.Specialization;
            doctor.Qualification = model.Qualification;
            doctor.Experience = model.Experience;
            doctor.ConsultationFee = model.ConsultationFee;
            doctor.About = model.About;

            var oldSchedules = await _context.Schedules.Where(s => s.DoctorId == doctor.Id).ToListAsync();
            _context.Schedules.RemoveRange(oldSchedules);

            if (model.Schedule != null)
            {
                foreach (var s in model.Schedule.Where(x => x.IsEnabled))
                {
                    if (!Enum.TryParse<DayOfWeek>(s.Day, true, out var dow)) continue;
                    if (!TimeSpan.TryParse(s.StartTime, out var start)) continue;
                    if (!TimeSpan.TryParse(s.EndTime, out var end)) continue;
                    if (end <= start) continue;

                    _context.Schedules.Add(new Schedule
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = dow,
                        StartTime = start,
                        EndTime = end,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Doctor", doctor.Id, "Profile & schedule updated");

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // ---------------- PATIENT DETAILS ----------------
        public async Task<IActionResult> PatientDetails(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            var model = new PatientDetailsViewModel
            {
                Id = appointment.PatientId,
                PatientName = appointment.Patient?.FullName ?? "Unknown",
                PatientCode = "P" + appointment.PatientId.ToString("D3"),
                Phone = appointment.Patient?.Phone ?? "",
                Age = appointment.Patient?.Age ?? 0,
                Gender = appointment.Patient?.Gender ?? "",
                Allergies = appointment.Patient?.Allergies ?? "None recorded",
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm"),
                Reason = appointment.Reason ?? "",
                Status = appointment.Status
            };
            return View(model);
        }
    }
}