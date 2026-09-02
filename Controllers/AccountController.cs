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
    public class AccountController : Controller{
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AccountController(ApplicationDbContext context, IAuditService audit){
            _context = context;
            _audit = audit;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model){
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Fix: Use BCrypt.Net.BCrypt.Verify and add null check
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash)){
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>{
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            await _audit.LogAsync("Login", "User", user.Id, "User logged in");

            return user.Role switch{
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Receptionist" => RedirectToAction("Dashboard", "Receptionist"),
                _ => RedirectToAction("Dashboard", "Patient")
            };
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model){
            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email)){
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var patient = new Patient{
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Age = DateTime.Now.Year - model.DateOfBirth.Year,
                Gender = model.Gender,
                Address = model.Address,
                CreatedAt = DateTime.Now
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            var user = new User{
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),  // fully qualified
                Role = "Patient",
                PatientId = patient.Id
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Register", "Patient", patient.Id, $"New patient registered");

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout(){
            var userIdClaim = User.FindFirst("UserId");
            int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;
            await _audit.LogAsync("Logout", "User", userId, "User logged out");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();
    }
}