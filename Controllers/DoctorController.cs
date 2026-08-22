using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Application_Doctor_Management.Controllers;

public class DoctorController : Controller
{
    private readonly ApplicationDbContext _context;

    public DoctorController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Doctor
    public async Task<IActionResult> Index()
    {
        var doctors = await _context.Doctors.ToListAsync();

        return View(doctors);
    }

    // GET: /Doctor/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Doctor/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Doctor doctor)
    {
        if (ModelState.IsValid)
        {
            _context.Doctors.Add(doctor);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(doctor);
    }
}