using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Application_Doctor_Management.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ReceptionistController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ---------- DASHBOARD ----------
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var model = new ReceptionistDashboardViewModel
            {
                TodaysAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
                TotalPatients = await _context.Patients.CountAsync(),
                PendingConfirmations = await _context.Appointments.CountAsync(a => a.Status == "Pending")
            };
            return View(model);
        }

        // ---------- BOOK APPOINTMENT ----------
        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            await LoadBookingDropdownsAsync();
            return View(new ReceptionistBookingViewModel { AppointmentDate = DateTime.Today.AddDays(1) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(ReceptionistBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadBookingDropdownsAsync();
                return View(model);
            }

            if (!TimeSpan.TryParse(model.AppointmentTime, out var parsedTime))
            {
                ModelState.AddModelError("AppointmentTime", "Invalid time format.");
                await LoadBookingDropdownsAsync();
                return View(model);
            }

            var appointmentDateTime = model.AppointmentDate.Date.Add(parsedTime);
            if (appointmentDateTime < DateTime.Now)
            {
                ModelState.AddModelError("", "Appointment date and time cannot be in the past.");
                await LoadBookingDropdownsAsync();
                return View(model);
            }

            var conflicting = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == model.DoctorId
                && a.AppointmentDate == model.AppointmentDate
                && a.AppointmentTime == parsedTime
                && a.Status != "Cancelled");

            if (conflicting)
            {
                ModelState.AddModelError("", "This time slot is already booked.");
                await LoadBookingDropdownsAsync();
                return View(model);
            }

            var doctor = await _context.Doctors.FindAsync(model.DoctorId);
            if (doctor == null)
            {
                ModelState.AddModelError("", "Doctor not found.");
                await LoadBookingDropdownsAsync();
                return View(model);
            }

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId,
                PatientId = model.PatientId,
                AppointmentDate = model.AppointmentDate,
                AppointmentTime = parsedTime,
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var bill = new Bill
            {
                PatientId = model.PatientId,
                AppointmentId = appointment.Id,
                Amount = doctor.ConsultationFee,
                PaidAmount = 0m,
                Status = "Unpaid",
                BillDate = DateTime.Now,
                Description = $"Consultation fee for Dr. {doctor.Name} (booked by receptionist)"
            };
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Appointment", appointment.Id, "Booked by receptionist");
            TempData["SuccessMessage"] = "Appointment created. Collect payment to confirm.";
            return RedirectToAction("Checkout", new { appointmentId = appointment.Id });
        }

        // ---------- CHECKOUT (GET) ----------
        [HttpGet]
        public async Task<IActionResult> Checkout(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor).Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null) return NotFound();

            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.AppointmentId == appointmentId);
            if (bill == null) return NotFound();

            if (bill.Status == "Paid" || bill.PaidAmount >= bill.Amount)
            {
                TempData["SuccessMessage"] = "This appointment is already paid.";
                return RedirectToAction("Appointments");
            }

            var balance = bill.Amount - bill.PaidAmount;

            return View(new CheckoutViewModel
            {
                AppointmentId = appointment.Id,
                BillId = bill.Id,
                DoctorName = appointment.Doctor?.Name ?? "",
                PatientName = appointment.Patient?.FullName ?? "",
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm"),
                TotalFee = bill.Amount,
                AlreadyPaid = bill.PaidAmount,
                AmountNow = balance,
                PaymentMethod = bill.PaymentMethod ?? "Cash",
                IsReceptionistFlow = true
            });
        }

        // ---------- CHECKOUT (POST) ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId);
            if (appointment == null) return NotFound();

            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.AppointmentId == model.AppointmentId);
            if (bill == null) return NotFound();

            if (bill.Status == "Paid" || bill.PaidAmount >= bill.Amount)
            {
                TempData["SuccessMessage"] = "This appointment is already paid.";
                return RedirectToAction("Appointments");
            }

            if (!ModelState.IsValid)
            {
                model.DoctorName = appointment.Doctor?.Name ?? "";
                model.PatientName = appointment.Patient?.FullName ?? "";
                model.AppointmentDate = appointment.AppointmentDate;
                model.AppointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm");
                model.TotalFee = bill.Amount;
                model.AlreadyPaid = bill.PaidAmount;
                model.IsReceptionistFlow = true;
                return View(model);
            }

            var balance = bill.Amount - bill.PaidAmount;
            decimal paidNow = model.AmountNow;
            if (paidNow > balance) paidNow = balance;

            bill.PaidAmount += paidNow;
            bill.PaymentMethod = model.PaymentMethod;

            if (bill.PaidAmount >= bill.Amount)
            {
                bill.Status = "Paid";
                appointment.Status = "Confirmed";
                TempData["SuccessMessage"] = "Payment complete. Appointment confirmed.";
            }
            else if (bill.PaidAmount > 0)
            {
                bill.Status = "Partial";
                appointment.Status = "Pending";
                TempData["WarningMessage"] =
                    $"Partial payment received ({paidNow:0.00}৳ of {bill.Amount:0.00}৳). " +
                    $"Remaining balance: {bill.Amount - bill.PaidAmount:0.00}৳. Appointment pending until full payment.";
            }
            else
            {
                bill.Status = "Unpaid";
                appointment.Status = "Pending";
                TempData["WarningMessage"] = "No payment received. Appointment remains pending.";
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Payment", "Bill", bill.Id,
                $"Receptionist collected {paidNow:0.00}; total paid {bill.PaidAmount:0.00}; status {bill.Status}");

            return RedirectToAction("Appointments");
        }

        // ---------- HELPER ----------
        private async Task LoadBookingDropdownsAsync()
        {
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FullName)
                .Select(p => new PatientListItemViewModel
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    PatientCode = "P" + p.Id.ToString("D3"),
                    Phone = p.Phone,
                    Age = p.Age,
                    Gender = p.Gender
                })
                .ToListAsync();

            ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
        }

        // ---------- APPOINTMENTS ----------
        public async Task<IActionResult> Appointments(DateTime? date, string status)
        {
            var query = _context.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();

            if (date.HasValue) query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var viewModels = appointments.Select(a => new ReceptionistBookingViewModel
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.FullName ?? "Unknown",
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor?.Name ?? "Unknown",
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                Reason = a.Reason ?? "",
                Status = a.Status
            }).ToList();

            ViewBag.Bills = await _context.Bills.ToDictionaryAsync(b => b.AppointmentId, b => b);
            ViewBag.FilterDate = date;
            ViewBag.FilterStatus = status;
            return View(viewModels);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var appointment = await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            appointment.Status = "Confirmed";
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Appointment", id, "Confirmed by receptionist");
            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} confirmed.";
            return RedirectToAction("Appointments");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.Include(a => a.Patient).FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Appointment", id, "Cancelled by receptionist");
            TempData["SuccessMessage"] = $"Appointment for {appointment.Patient?.FullName} cancelled.";
            return RedirectToAction("Appointments");
        }

        // ---------- PATIENTS ----------
        public async Task<IActionResult> Patients(string search, int? page)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.FullName.Contains(search)
                    || p.Phone.Contains(search)
                    || (p.Email != null && p.Email.Contains(search)));

            int pageSize = 10;
            int pageNumber = page ?? 1;

            var patients = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var viewModels = patients.Select(p => new PatientListItemViewModel
            {
                Id = p.Id,
                FullName = p.FullName,
                PatientCode = $"P{p.Id:D3}",
                Phone = p.Phone,
                Age = p.Age,
                Gender = p.Gender
            }).ToList();

            ViewBag.Search = search;
            ViewBag.Page = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            return View(viewModels);
        }

        public IActionResult AddPatient() => View(new PatientListItemViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPatient(PatientListItemViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var patient = new Patient
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Age = model.Age,
                Gender = model.Gender,
                CreatedAt = DateTime.Now
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Patient", patient.Id, $"Patient {patient.FullName} added");
            TempData["SuccessMessage"] = $"Patient {model.FullName} added successfully.";
            return RedirectToAction("Patients");
        }

        public async Task<IActionResult> EditPatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }

            return View(new PatientListItemViewModel
            {
                Id = patient.Id,
                FullName = patient.FullName,
                Phone = patient.Phone,
                Age = patient.Age,
                Gender = patient.Gender
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(PatientListItemViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var patient = await _context.Patients.FindAsync(model.Id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Patients");
            }

            patient.FullName = model.FullName;
            patient.Phone = model.Phone;
            patient.Age = model.Age;
            patient.Gender = model.Gender;
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Patient", patient.Id, $"Patient {patient.FullName} updated");
            TempData["SuccessMessage"] = $"Patient {patient.FullName} updated successfully.";
            return RedirectToAction("Patients");
        }

        // ---------- PROFILE ----------
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            return View(new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
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
    }
}