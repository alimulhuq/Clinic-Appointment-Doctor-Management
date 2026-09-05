using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Clinic_Application_Doctor_Management.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly IAppointmentService _appointmentService;

        public PatientController(ApplicationDbContext context, IAuditService audit, IAppointmentService appointmentService)
        {
            _context = context;
            _audit = audit;
            _appointmentService = appointmentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var model = new PatientDashboardViewModel
            {
                UpcomingAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == patient!.Id && a.AppointmentDate >= DateTime.Today && a.Status != "Cancelled"),

                TotalVisits = await _context.Appointments
                    .CountAsync(a => a.PatientId == patient!.Id && a.Status == "Completed"),

                PendingBills = await _context.Bills
                    .CountAsync(b => b.PatientId == patient!.Id && b.Status == "Pending"),

                AvailableDoctors = await _context.Doctors.CountAsync(),

                RecentAppointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == patient!.Id && a.AppointmentDate >= DateTime.Today && a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .Take(5)
                    .Select(a => new DoctorAppointmentViewModel
                    {
                        Id = a.Id,
                        DoctorName = a.Doctor.Name,
                        Specialization = a.Doctor.Specialization,
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm tt"),
                        Status = a.Status
                    }).ToListAsync()
            };
            return View(model);
        }

        // ---------------- DOCTORS LIST ----------------
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            var viewModels = doctors.Select(d => new DoctorViewModel
            {
                DoctorId = d.Id,
                FullName = d.Name,
                Specialization = d.Specialization,
                Phone = d.Phone,
                Qualification = d.Qualification,
                Experience = d.Experience,
                AvailableDays = string.Join(", ", d.Schedules.Where(s => s.IsActive).Select(s => s.DayOfWeek.ToString()).Distinct()),
                AvailableTime = d.Schedules.Any(s => s.IsActive)
                    ? $"{d.Schedules.First(s => s.IsActive).StartTime.ToString(@"hh\:mm")} - {d.Schedules.First(s => s.IsActive).EndTime.ToString(@"hh\:mm")}"
                    : "Not specified"
            }).ToList();
            return View(viewModels);
        }

        // ---------------- DOCTOR DETAILS ----------------
        public async Task<IActionResult> DoctorDetails(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            var viewModel = new DoctorViewModel
            {
                DoctorId = doctor.Id,
                FullName = doctor.Name,
                Specialization = doctor.Specialization,
                Phone = doctor.Phone,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                AvailableDays = string.Join(", ", doctor.Schedules.Where(s => s.IsActive).Select(s => s.DayOfWeek.ToString()).Distinct()),
                AvailableTime = doctor.Schedules.Any(s => s.IsActive)
                    ? $"{doctor.Schedules.First(s => s.IsActive).StartTime.ToString(@"hh\:mm")} - {doctor.Schedules.First(s => s.IsActive).EndTime.ToString(@"hh\:mm")}"
                    : "Not specified"
            };
            return View(viewModel);
        }

        // ---------------- BOOK APPOINTMENT (GET) ----------------
        public async Task<IActionResult> BookAppointment(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            var model = new AppointmentViewModel
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.Name,
                Specialization = doctor.Specialization,
                AppointmentDate = DateTime.Today.AddDays(1),
                AppointmentTime = "09:00 AM"
            };
            return View(model);
        }

        // ---------------- BOOK APPOINTMENT (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(AppointmentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId,
                PatientId = patient.Id,
                AppointmentDate = model.AppointmentDate,
                AppointmentTime = TimeSpan.Parse(model.AppointmentTime),
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            var (isValid, errorMessage) = await _appointmentService.ValidateAppointmentAsync(appointment);
            if (!isValid)
            {
                if (!string.IsNullOrEmpty(errorMessage)) ModelState.AddModelError("", errorMessage);
                var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                if (doctor != null)
                {
                    model.DoctorName = doctor.Name;
                    model.Specialization = doctor.Specialization;
                }
                return View(model);
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Appointment", appointment.Id, $"Appointment booked by {patient.Email}");

            TempData["SuccessMessage"] = $"Appointment requested with {model.DoctorName} on {model.AppointmentDate:dd MMM yyyy} at {model.AppointmentTime}.";
            return RedirectToAction("MyAppointments");
        }

        // ---------------- MY APPOINTMENTS ----------------
        public async Task<IActionResult> MyAppointments()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();
            return View(appointments);
        }

        // ---------------- PRESCRIPTIONS ----------------
        public async Task<IActionResult> Prescriptions()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.Items)
                .Where(p => p.PatientId == patient.Id)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();
            return View(prescriptions);
        }

        // ---------------- BILLS ----------------
        public async Task<IActionResult> Bills()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var bills = await _context.Bills
                .Include(b => b.Appointment)
                .Where(b => b.PatientId == patient.Id)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
            return View(bills);
        }

        // ---------------- PROFILE (GET) ----------------
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile", "Account");

            var model = new PatientProfileViewModel
            {
                FullName = patient.FullName,
                Email = patient.Email ?? "",
                Phone = patient.Phone,
                DateOfBirth = DateTime.Now.AddYears(-patient.Age),
                Gender = patient.Gender,
                Address = patient.Address ?? "",
                MedicalHistory = patient.MedicalHistory,
                Allergies = patient.Allergies,
                BloodGroup = patient.BloodGroup,
                EmergencyContact = patient.EmergencyContact,
                EmergencyContactPhone = patient.EmergencyContactPhone
            };
            return View(model);
        }

        // ---------------- PROFILE (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile", "Account");

            patient.FullName = model.FullName;
            patient.Phone = model.Phone;
            patient.Gender = model.Gender;
            patient.Address = model.Address;
            patient.MedicalHistory = model.MedicalHistory;
            patient.Allergies = model.Allergies;
            patient.BloodGroup = model.BloodGroup;
            patient.EmergencyContact = model.EmergencyContact;
            patient.EmergencyContactPhone = model.EmergencyContactPhone;
            patient.Age = DateTime.Now.Year - model.DateOfBirth.Year;

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Patient", patient.Id, "Profile updated");
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // ---------------- CHANGE PASSWORD (NEW) ----------------
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