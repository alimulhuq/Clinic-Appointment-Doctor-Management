using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace Clinic_Application_Doctor_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AccountController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ---------- STANDARD LOGIN (Patients, Doctors, Receptionists) ----------
        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.Role != "Patient" && user.Role != "Doctor" && user.Role != "Receptionist")
            {
                ModelState.AddModelError("", "Please use the correct login page for your account type.");
                return View(model);
            }

            if (model.SelectedRole != user.Role)
            {
                ModelState.AddModelError("", $"This account is not registered as a {model.SelectedRole}. Please select the correct role.");
                return View(model);
            }

            var claims = new List<Claim>{
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserID", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            await _audit.LogAsync("Login", "User", user.Id, $"User logged in as {user.Role}");

            return user.Role switch
            {
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Receptionist" => RedirectToAction("Dashboard", "Receptionist"),
                _ => RedirectToAction("Dashboard", "Patient")
            };
        }

        // ---------- ADMIN LOGIN (separate) ----------
        [HttpGet]
        public IActionResult AdminLogin() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            ModelState.Remove(nameof(model.SelectedRole));

            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash) || user.Role != "Admin")
            {
                ModelState.AddModelError("", "Invalid admin credentials.");
                return View(model);
            }

            var claims = new List<Claim>{
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserID", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            await _audit.LogAsync("Admin Login", "User", user.Id, "Admin logged in");

            return RedirectToAction("Dashboard", "Admin");
        }

        // ---------- REGISTER (Patient OR Doctor) ----------
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // ---------- CONDITIONAL VALIDATION BASED ON ROLE ----------
            if (model.SelectedRole == "Patient")
            {
                if (!model.DateOfBirth.HasValue)
                    ModelState.AddModelError("DateOfBirth", "Date of birth is required.");

                if (string.IsNullOrWhiteSpace(model.Gender))
                    ModelState.AddModelError("Gender", "Please select a gender.");

                if (string.IsNullOrWhiteSpace(model.Address))
                    ModelState.AddModelError("Address", "Address is required.");
            }
            else if (model.SelectedRole == "Doctor")
            {
                if (string.IsNullOrWhiteSpace(model.DoctorGender))
                    ModelState.AddModelError("DoctorGender", "Please select a gender.");

                if (string.IsNullOrWhiteSpace(model.Specialization))
                    ModelState.AddModelError("Specialization", "Specialization is required.");

                if (string.IsNullOrWhiteSpace(model.Qualification))
                    ModelState.AddModelError("Qualification", "Qualification is required.");

                if (!model.Experience.HasValue)
                    ModelState.AddModelError("Experience", "Experience is required.");

                if (!model.ConsultationFee.HasValue || model.ConsultationFee <= 0)
                    ModelState.AddModelError("ConsultationFee", "Please enter a valid consultation fee.");
            }
            else
            {
                ModelState.AddModelError("SelectedRole", "Please select a valid role.");
            }

            if (!ModelState.IsValid) return View(model);

            // ---------- DUPLICATE EMAIL CHECK ----------
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // ---------- CREATE USER ----------
            var user = new User
            {
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.SelectedRole
            };

            if (model.SelectedRole == "Patient")
            {
                var patient = new Patient
                {
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Email = model.Email,
                    Age = model.DateOfBirth.HasValue
                        ? DateTime.Now.Year - model.DateOfBirth.Value.Year
                        : 0,
                    Gender = model.Gender ?? "",
                    Address = model.Address,
                    CreatedAt = DateTime.Now
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                user.PatientId = patient.Id;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _audit.LogAsync("Register", "Patient", patient.Id, "New patient registered");
            }
            else // Doctor
            {
                var doctor = new Doctor
                {
                    Name = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Gender = model.DoctorGender,
                    Specialization = model.Specialization!,
                    Qualification = model.Qualification!,
                    Experience = model.Experience ?? 0,
                    ConsultationFee = model.ConsultationFee ?? 500m,
                    About = model.About
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                user.DoctorId = doctor.Id;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Save schedule if provided
                if (model.Schedule != null && model.Schedule.Any())
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
                    await _context.SaveChangesAsync();
                }

                await _audit.LogAsync("Register", "Doctor", doctor.Id, "New doctor registered");
            }

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        // ---------- LOGOUT ----------
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("UserID");
            int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
            await _audit.LogAsync("Logout", "User", userId, "User logged out");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ---------- ACCESS DENIED ----------
        public IActionResult AccessDenied() => View();

        // ---------- PROFILE REDIRECT ----------
        public IActionResult MyProfile()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role switch
            {
                "Admin" => RedirectToAction("Profile", "Admin"),
                "Doctor" => RedirectToAction("Profile", "Doctor"),
                "Receptionist" => RedirectToAction("Profile", "Receptionist"),
                _ => RedirectToAction("Profile", "Patient")
            };
        }
    }
}