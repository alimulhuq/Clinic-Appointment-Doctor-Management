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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AdminController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ---------- DASHBOARD ----------
        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalReceptionists = await _context.Users.CountAsync(u => u.Role == "Receptionist"),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),

                AllAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync(),

                AllPatients = await _context.Patients
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync()
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

        // ---------- PATIENT DETAILS (admin view, no password) ----------
        public async Task<IActionResult> PatientDetails(int patientId)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Dashboard");
            }

            var model = new AdminPatientDetailsViewModel
            {
                Id = patient.Id,
                PatientCode = $"P{patient.Id:D3}",
                FullName = patient.FullName,
                Email = patient.Email ?? "",
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender,
                Address = patient.Address ?? "",
                MedicalHistory = patient.MedicalHistory ?? "",
                Allergies = patient.Allergies ?? "None recorded",
                BloodGroup = patient.BloodGroup ?? "",
                EmergencyContact = patient.EmergencyContact ?? "",
                EmergencyContactPhone = patient.EmergencyContactPhone ?? "",
                CreatedAt = patient.CreatedAt
            };

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