using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Clinic_Application_Doctor_Management.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller{
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AdminController(ApplicationDbContext context, IAuditService audit){
            _context = context;
            _audit = audit;
        }

        // ---------- DASHBOARD ----------
        public async Task<IActionResult> Dashboard()
        {
            var allAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            // Weekly volume: last 7 days (including today), grouped by day of week (Mon=0 ... Sun=6)
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            var weekAppointments = allAppointments
                .Where(a => a.AppointmentDate.Date >= weekStart && a.AppointmentDate.Date <= today)
                .ToList();

            var weeklyCompleted = new int[7];
            var weeklyCancelled = new int[7];

            foreach (var appt in weekAppointments)
            {
                // DayOfWeek: Sunday=0 ... Saturday=6 → convert to Mon=0 ... Sun=6
                int dayIndex = ((int)appt.AppointmentDate.DayOfWeek + 6) % 7;

                if (appt.Status == "Completed" || appt.Status == "Confirmed")
                    weeklyCompleted[dayIndex]++;
                else if (appt.Status == "Rejected" || appt.Status == "Cancelled" || appt.Status == "Rescheduled")
                    weeklyCancelled[dayIndex]++;
            }

            var model = new AdminDashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalReceptionists = await _context.Users.CountAsync(u => u.Role == "Receptionist"),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = allAppointments.Count,

                AllAppointments = allAppointments,

                AllPatients = await _context.Patients
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(),

                WeeklyCompleted = weeklyCompleted,
                WeeklyCancelled = weeklyCancelled
            };
            return View(model);
        }

        // ---------- ALL PATIENTS LIST (admin view) ----------
        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Patients
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModels = patients.Select(p => new AdminPatientDetailsViewModel
            {
                Id = p.Id,
                PatientCode = $"P{p.Id:D3}",
                FullName = p.FullName,
                Email = p.Email ?? "",
                Phone = p.Phone,
                Age = p.Age,
                Gender = p.Gender,
                Address = p.Address ?? "",
                MedicalHistory = p.MedicalHistory ?? "",
                Allergies = p.Allergies ?? "None recorded",
                BloodGroup = p.BloodGroup ?? "",
                EmergencyContact = p.EmergencyContact ?? "",
                EmergencyContactPhone = p.EmergencyContactPhone ?? "",
                CreatedAt = p.CreatedAt
            }).ToList();

            return View(viewModels);
        }

        // ---------- ALL APPOINTMENTS LIST (admin view) ----------
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var viewModels = appointments.Select(a => new AdminAppointmentViewModel
            {
                Id = a.Id,
                AppointmentCode = $"A{a.Id:D4}",
                PatientName = a.Patient != null ? a.Patient.FullName : "—",
                PatientCode = a.Patient != null ? $"P{a.Patient.Id:D3}" : "",
                PatientPhone = a.Patient != null ? a.Patient.Phone : "",
                DoctorName = a.Doctor != null ? a.Doctor.Name : "—",
                DoctorSpecialization = a.Doctor != null ? a.Doctor.Specialization : "",
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Reason = a.Reason ?? "",
                Status = a.Status,
                DurationMinutes = a.DurationMinutes,
                CreatedAt = a.CreatedAt
            }).ToList();

            ViewBag.CurrentAction = "Appointments";
            return View(viewModels);
        }

        // ---------- APPOINTMENT DETAILS (admin view) ----------
        public async Task<IActionResult> AppointmentDetails(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            var model = new AdminAppointmentViewModel
            {
                Id = appointment.Id,
                AppointmentCode = $"A{appointment.Id:D4}",
                PatientName = appointment.Patient != null ? appointment.Patient.FullName : "—",
                PatientCode = appointment.Patient != null ? $"P{appointment.Patient.Id:D3}" : "",
                PatientPhone = appointment.Patient != null ? appointment.Patient.Phone : "",
                DoctorName = appointment.Doctor != null ? appointment.Doctor.Name : "—",
                DoctorSpecialization = appointment.Doctor != null ? appointment.Doctor.Specialization : "",
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Reason = appointment.Reason ?? "",
                Status = appointment.Status,
                DurationMinutes = appointment.DurationMinutes,
                CreatedAt = appointment.CreatedAt
            };

            ViewBag.CurrentAction = "AppointmentDetails";
            return View(model);
        }

        // ---------- ADD DOCTOR (from dashboard) ----------
        [HttpGet]
        public IActionResult AddDoctor() => View(new DoctorManagementViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorManagementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var doctor = new Doctor
            {
                Name = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Specialization = model.Specialization,
                Qualification = model.Qualification,
                Experience = model.Experience
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Doctor", doctor.Id, $"Doctor {doctor.Name} added");
            TempData["SuccessMessage"] = $"Doctor {model.FullName} added successfully.";
            return RedirectToAction("Dashboard");
        }

        // ---------- ADD RECEPTIONIST (from dashboard) ----------
        [HttpGet]
        public IActionResult AddReceptionist() => View(new ReceptionistManagementViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReceptionist(ReceptionistManagementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DefaultPassword123!"),
                Role = "Receptionist"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Receptionist", user.Id, $"Receptionist {user.FullName} added");
            TempData["SuccessMessage"] = $"Receptionist {model.FullName} added.";
            return RedirectToAction("Dashboard");
        }

        // ---------- PROFILE (view & update) ----------
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "User", user.Id, "Profile updated");
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // ---------- CHANGE PASSWORD ----------
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("ChangePassword", "User", user.Id, "Password changed");

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }
    }
}