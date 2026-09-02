using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller{
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PatientController(ApplicationDbContext context, IAuditService audit){
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Dashboard(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            var model = new PatientDashboardViewModel{
                UpcomingAppointments = await _context.Appointments
                    .CountAsync(a => a.PatientId == patient.Id && a.AppointmentDate >= DateTime.Today && a.Status != "Cancelled"),
                TotalVisits = await _context.Appointments
                    .CountAsync(a => a.PatientId == patient.Id && a.Status == "Completed"),
                PendingBills = await _context.Bills
                    .CountAsync(b => b.PatientId == patient.Id && b.Status == "Pending")
            };
            return View(model);
        }

        public async Task<IActionResult> Doctors(){
            var doctors = await _context.Doctors.ToListAsync();
            var viewModels = doctors.Select(d => new DoctorViewModel{
                DoctorId = d.Id,
                FullName = d.Name,
                Specialization = d.Specialization,
                Phone = d.Phone,
                Qualification = d.Qualification,
                Experience = d.Experience
            }).ToList();
            return View(viewModels);
        }

        public async Task<IActionResult> DoctorDetails(int id){
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null){
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }
            var viewModel = new DoctorViewModel{
                DoctorId = doctor.Id,
                FullName = doctor.Name,
                Specialization = doctor.Specialization,
                Phone = doctor.Phone,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience
            };
            return View(viewModel);
        }

        public async Task<IActionResult> BookAppointment(int id){
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null){
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }
            var model = new AppointmentViewModel{
                DoctorId = doctor.Id,
                DoctorName = doctor.Name,
                Specialization = doctor.Specialization,
                AppointmentDate = DateTime.Today.AddDays(1),
                AppointmentTime = "09:00"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(AppointmentViewModel model){
            if (!ModelState.IsValid) return View(model);

            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile");

            // Check for conflicting appointments
            var conflicting = await _context.Appointments
                .AnyAsync(a => a.DoctorId == model.DoctorId
                               && a.AppointmentDate == model.AppointmentDate
                               && a.AppointmentTime == TimeSpan.Parse(model.AppointmentTime)
                               && a.Status != "Cancelled");
            if (conflicting){
                ModelState.AddModelError("", "This time slot is already booked. Please choose another time.");
                return View(model);
            }

            var appointment = new Appointment{
                DoctorId = model.DoctorId,
                PatientId = patient.Id,
                AppointmentDate = model.AppointmentDate,
                AppointmentTime = TimeSpan.Parse(model.AppointmentTime),
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Appointment", appointment.Id, $"Appointment booked by {patient.Email}");

            // Send email notification (if configured)
            // await _emailService.SendEmailAsync(patient.Email, "Appointment Booked", "Your appointment has been booked.");

            TempData["SuccessMessage"] = $"Appointment requested with {model.DoctorName} on {model.AppointmentDate:dd MMM yyyy} at {model.AppointmentTime}.";
            return RedirectToAction("MyAppointments");
        }

        public async Task<IActionResult> MyAppointments(){
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

        public async Task<IActionResult> Prescriptions(){
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

        public async Task<IActionResult> Bills(){
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

        public async Task<IActionResult> Profile(){
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == userEmail);
            if (patient == null) return RedirectToAction("Profile", "Account");

            var model = new PatientProfileViewModel{
                FullName = patient.FullName,
                Email = patient.Email ?? "",
                Phone = patient.Phone,
                DateOfBirth = DateTime.Now.AddYears(-patient.Age), // approximate
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(PatientProfileViewModel model){
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
            // Update age from DOB if needed
            // patient.Age = DateTime.Now.Year - model.DateOfBirth.Year;

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Patient", patient.Id, "Profile updated");

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return View(model);
        }
    }
}