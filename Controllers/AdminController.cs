using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller{
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AdminController(ApplicationDbContext context, IAuditService audit){
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Dashboard(){
            var model = new AdminDashboardViewModel{
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TotalReceptionists = await _context.Users.CountAsync(u => u.Role == "Receptionist")
            };
            return View(model);
        }

        // Doctors management (CRUD)
        public async Task<IActionResult> Doctors(){
            var doctors = await _context.Doctors.ToListAsync();
            var viewModels = doctors.Select(d => new DoctorManagementViewModel{
                Id = d.Id,
                FullName = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Specialization = d.Specialization,
                Qualification = d.Qualification,
                Experience = d.Experience
            }).ToList();
            return View(viewModels);
        }

        public IActionResult AddDoctor() => View(new DoctorManagementViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorManagementViewModel model){
            if (!ModelState.IsValid) return View(model);

            var doctor = new Doctor{
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
            return RedirectToAction("Doctors");
        }

        public async Task<IActionResult> EditDoctor(int id){
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            var model = new DoctorManagementViewModel{
                Id = doctor.Id,
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorManagementViewModel model){
            if (!ModelState.IsValid) return View(model);

            var doctor = await _context.Doctors.FindAsync(model.Id);
            if (doctor == null) return NotFound();

            doctor.Name = model.FullName;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.Specialization = model.Specialization;
            doctor.Qualification = model.Qualification;
            doctor.Experience = model.Experience;

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Doctor", doctor.Id, $"Doctor {doctor.Name} updated");

            TempData["SuccessMessage"] = $"Doctor {model.FullName} updated.";
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id){
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null){
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                await _audit.LogAsync("Delete", "Doctor", id, $"Doctor {doctor.Name} deleted");
                TempData["SuccessMessage"] = "Doctor deleted.";
            }
            return RedirectToAction("Doctors");
        }

        // Receptionists management (similar pattern)
        public async Task<IActionResult> Receptionists(){
            var users = await _context.Users.Where(u => u.Role == "Receptionist").ToListAsync();
            var viewModels = users.Select(u => new ReceptionistManagementViewModel{
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone
            }).ToList();
            return View(viewModels);
        }

        public IActionResult AddReceptionist() => View(new ReceptionistManagementViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReceptionist(ReceptionistManagementViewModel model){
            if (!ModelState.IsValid) return View(model);

            var user = new User{
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
            return RedirectToAction("Receptionists");
        }

        // Patients list (read-only for admin)
        public async Task<IActionResult> Patients(){
            var patients = await _context.Patients.ToListAsync();
            var viewModels = patients.Select(p => new PatientListItemViewModel{
                Id = p.Id,
                FullName = p.FullName,
                PatientCode = $"P{p.Id:D3}",
                Phone = p.Phone,
                Age = p.Age,
                Gender = p.Gender
            }).ToList();
            return View(viewModels);
        }

        // Audit Logs
        public async Task<IActionResult> AuditLogs(){
            var logs = await _context.AuditLogs.OrderByDescending(l => l.Timestamp).Take(200).ToListAsync();
            return View(logs);
        }
    }
}