using System.Diagnostics;
using Clinic_Application_Doctor_Management.Data;
using Clinic_Appointment_Doctor_Management.Models;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Appointment_Doctor_Management.Controllers{
    public class HomeController : Controller{
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context){
            _context = context;
        }

        public async Task<IActionResult> Index(){
            var allDoctors = await _context.Doctors
                .Include(d => d.Schedules)
                .ToListAsync();

            var departments = allDoctors
                .Select(d => d.Specialization)
                .Distinct()
                .ToList();

            var featuredDoctors = allDoctors.Take(3).ToList();

            var model = new HomeViewModel{
                FeaturedDoctors = featuredDoctors,
                Departments = departments,
                AllDoctors = allDoctors
            };

            return View(model);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(){
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}