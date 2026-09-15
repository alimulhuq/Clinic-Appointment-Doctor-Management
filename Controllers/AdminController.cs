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
        public async Task<IActionResult> Dashboard(string range = "week")
        {
            if (range != "today" && range != "week" && range != "month")
                range = "week";

            var today = DateTime.Today;

            DateTime windowStart;
            DateTime windowEnd;
            string[] labels;
            List<(DateTime start, DateTime end, string label)> buckets = new();
            bool isHourly = false;

            if (range == "today")
            {
                isHourly = true;
                windowStart = today;
                windowEnd = today;

                labels = new string[10];
                for (int h = 9; h <= 18; h++)
                {
                    var start = today.AddHours(h);
                    var end = start.AddHours(1);
                    labels[h - 9] = start.ToString("htt").ToLower();
                    buckets.Add((start, end, labels[h - 9]));
                }
            }
            else if (range == "week")
            {
                windowStart = today.AddDays(-6);
                windowEnd = today;

                labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                for (int i = 0; i < 7; i++)
                {
                    var day = windowStart.AddDays(i);
                    buckets.Add((day.Date, day.Date.AddDays(1), labels[((int)day.DayOfWeek + 6) % 7]));
                }
            }
            else
            {
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

                windowStart = firstOfMonth;
                windowEnd = lastOfMonth;

                int days = lastOfMonth.Day;
                labels = new string[days];
                for (int d = 1; d <= days; d++)
                {
                    var day = new DateTime(today.Year, today.Month, d);
                    labels[d - 1] = d.ToString();
                    buckets.Add((day.Date, day.Date.AddDays(1), labels[d - 1]));
                }
            }

            var rangeAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate.Date >= windowStart.Date && a.AppointmentDate.Date <= windowEnd.Date)
                .ToListAsync();

            var allAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            var chartCompleted = new int[buckets.Count];
            var chartCancelled = new int[buckets.Count];

            foreach (var appt in rangeAppointments)
            {
                if (isHourly && (appt.AppointmentTime.Hours < 9 || appt.AppointmentTime.Hours > 18))
                    continue;

                DateTime apptDateTime = appt.AppointmentDate.Date.Add(appt.AppointmentTime);

                int idx = -1;
                for (int i = 0; i < buckets.Count; i++)
                {
                    if (apptDateTime >= buckets[i].start && apptDateTime < buckets[i].end)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx < 0) continue;

                bool isCompleted = appt.Status == "Completed" || appt.Status == "Confirmed";
                bool isCancelled = appt.Status == "Rejected" || appt.Status == "Cancelled" || appt.Status == "Rescheduled";

                if (isCompleted) chartCompleted[idx]++;
                else if (isCancelled) chartCancelled[idx]++;
            }

            int rangeConfirmed = rangeAppointments.Count(a => a.Status == "Confirmed");
            int rangePending = rangeAppointments.Count(a => a.Status == "Pending");
            int rangeCompleted = rangeAppointments.Count(a => a.Status == "Completed");
            int rangeCancelled = rangeAppointments.Count(a => a.Status == "Cancelled");
            int rangeTotal = rangeAppointments.Count;
            int rangeRejectedOther = rangeTotal - rangeConfirmed - rangePending - rangeCompleted;

            string chartTitle = range switch
            {
                "today" => "Today's Patient Consultation Volume",
                "month" => "This Month's Patient Consultation Volume",
                _ => "Weekly Patient Consultation Volume"
            };

            string chartSubtitle = range switch
            {
                "today" => "Hourly view (9 AM – 7 PM) — completed vs cancelled",
                "month" => $"{windowStart:MMMM yyyy} — completed vs cancelled from database",
                _ => "Last 7 days — completed vs cancelled from database"
            };

            var model = new AdminDashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalReceptionists = await _context.Users.CountAsync(u => u.Role == "Receptionist"),
                TotalPatients = await _context.Patients.CountAsync(),
                TotalAppointments = allAppointments.Count,
                AllAppointments = allAppointments,
                AllPatients = await _context.Patients.OrderByDescending(p => p.CreatedAt).ToListAsync(),

                SelectedRange = range,
                ChartTitle = chartTitle,
                ChartSubtitle = chartSubtitle,
                ChartLabels = labels,
                ChartCompleted = chartCompleted,
                ChartCancelled = chartCancelled,

                RangeConfirmed = rangeConfirmed,
                RangePending = rangePending,
                RangeCompleted = rangeCompleted,
                RangeCancelled = rangeCancelled,
                RangeRejectedOther = rangeRejectedOther,
                RangeTotal = rangeTotal
            };

            return View(model);
        }

        // ---------- PATIENTS ----------
        public async Task<IActionResult> Patients()
        {
            var patients = await _context.Patients.OrderByDescending(p => p.CreatedAt).ToListAsync();

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

        public async Task<IActionResult> PatientDetails(int patientId)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
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

        // ---------- APPOINTMENTS ----------
        public async Task<IActionResult> Appointments(string status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
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
                DoctorProfilePicture = a.Doctor != null ? a.Doctor.ProfilePicture : null,
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Reason = a.Reason ?? "",
                Status = a.Status,
                DurationMinutes = a.DurationMinutes,
                CreatedAt = a.CreatedAt
            }).ToList();

            ViewBag.StatusFilter = status;
            return View(viewModels);
        }

        public async Task<IActionResult> AppointmentDetails(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).Include(a => a.Doctor)
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
                DoctorProfilePicture = appointment.Doctor != null ? appointment.Doctor.ProfilePicture : null,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Reason = appointment.Reason ?? "",
                Status = appointment.Status,
                DurationMinutes = appointment.DurationMinutes,
                CreatedAt = appointment.CreatedAt
            };
            return View(model);
        }

        // ---------- DOCTORS ----------
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _context.Doctors
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            var viewModels = doctors.Select(d => new DoctorManagementViewModel
            {
                Id = d.Id,
                FullName = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Specialization = d.Specialization,
                Qualification = d.Qualification,
                Experience = d.Experience,
                Gender = d.Gender,
                ConsultationFee = d.ConsultationFee,
                About = d.About,
                ProfilePicture = d.ProfilePicture
            }).ToList();

            return View(viewModels);
        }

        public async Task<IActionResult> DoctorDetails(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Schedules)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            var model = new DoctorManagementViewModel
            {
                Id = doctor.Id,
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                Gender = doctor.Gender,
                ConsultationFee = doctor.ConsultationFee,
                About = doctor.About,
                ProfilePicture = doctor.ProfilePicture
            };

            ViewBag.Schedules = doctor.Schedules.Where(s => s.IsActive).OrderBy(s => ((int)s.DayOfWeek + 1) % 7).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            return View(new DoctorManagementViewModel
            {
                Id = doctor.Id,
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                Gender = doctor.Gender,
                ConsultationFee = doctor.ConsultationFee,
                About = doctor.About,
                ProfilePicture = doctor.ProfilePicture
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorManagementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var doctor = await _context.Doctors.FindAsync(model.Id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            doctor.Name = model.FullName;
            doctor.Email = model.Email;
            doctor.Phone = model.Phone;
            doctor.Specialization = model.Specialization;
            doctor.Qualification = model.Qualification;
            doctor.Experience = model.Experience;
            doctor.Gender = model.Gender;
            doctor.ConsultationFee = model.ConsultationFee;
            doctor.About = model.About;

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Doctor", doctor.Id, $"Doctor {doctor.Name} updated");

            TempData["SuccessMessage"] = $"Doctor {model.FullName} updated successfully.";
            return RedirectToAction("Doctors");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            return View(new DoctorManagementViewModel
            {
                Id = doctor.Id,
                FullName = doctor.Name,
                Email = doctor.Email,
                Phone = doctor.Phone,
                Specialization = doctor.Specialization,
                Qualification = doctor.Qualification,
                Experience = doctor.Experience,
                Gender = doctor.Gender,
                ConsultationFee = doctor.ConsultationFee,
                About = doctor.About,
                ProfilePicture = doctor.ProfilePicture
            });
        }

        [HttpPost, ActionName("DeleteDoctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Doctor not found.";
                return RedirectToAction("Doctors");
            }

            var hasAppointments = await _context.Appointments.AnyAsync(a => a.DoctorId == id);
            if (hasAppointments)
            {
                TempData["ErrorMessage"] = "Cannot delete: this doctor has existing appointments.";
                return RedirectToAction("Doctors");
            }

            var schedules = await _context.Schedules.Where(s => s.DoctorId == id).ToListAsync();
            if (schedules.Any())
            {
                _context.Schedules.RemoveRange(schedules);
            }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", "Doctor", id, $"Doctor {doctor.Name} deleted");

            TempData["SuccessMessage"] = $"Doctor {doctor.Name} removed successfully.";
            return RedirectToAction("Doctors");
        }

        // ---------- RECEPTIONISTS ----------
        public async Task<IActionResult> Receptionists()
        {
            var receptionists = await _context.Users
                .Where(u => u.Role == "Receptionist")
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            var viewModels = receptionists.Select(u => new ReceptionistManagementViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.FullName,
                Email = u.Email,
                Phone = u.Phone
            }).ToList();

            return View(viewModels);
        }

        public async Task<IActionResult> ReceptionistDetails(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Receptionist");
            if (user == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction("Receptionists");
            }

            return View(new ReceptionistManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            });
        }

        [HttpGet]
        public IActionResult AddReceptionist() => View(new ReceptionistManagementViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReceptionist(ReceptionistManagementViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required.");
            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                ModelState.AddModelError("ConfirmPassword", "Please confirm the password.");

            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password!),
                Role = "Receptionist"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _audit.LogAsync("Create", "Receptionist", user.Id, $"Receptionist {user.FullName} added");

            TempData["SuccessMessage"] = $"Receptionist {model.FullName} added successfully.";
            return RedirectToAction("Receptionists");
        }

        [HttpGet]
        public async Task<IActionResult> EditReceptionist(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Receptionist");
            if (user == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction("Receptionists");
            }

            return View(new ReceptionistManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceptionist(ReceptionistManagementViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.Remove(nameof(model.Password));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == model.Id && u.Role == "Receptionist");
            if (user == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction("Receptionists");
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
            {
                ModelState.AddModelError("Email", "This email is already used by another account.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;

            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            await _context.SaveChangesAsync();
            await _audit.LogAsync("Update", "Receptionist", user.Id, $"Receptionist {user.FullName} updated");

            TempData["SuccessMessage"] = $"Receptionist {model.FullName} updated successfully.";
            return RedirectToAction("Receptionists");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteReceptionist(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Receptionist");
            if (user == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction("Receptionists");
            }

            return View(new ReceptionistManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            });
        }

        [HttpPost, ActionName("DeleteReceptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReceptionistConfirmed(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.Role == "Receptionist");
            if (user == null)
            {
                TempData["ErrorMessage"] = "Receptionist not found.";
                return RedirectToAction("Receptionists");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Delete", "Receptionist", id, $"Receptionist {user.FullName} deleted");

            TempData["SuccessMessage"] = $"Receptionist {user.FullName} removed successfully.";
            return RedirectToAction("Receptionists");
        }

        // ---------- ADD DOCTOR ----------
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
                Experience = model.Experience,
                Gender = model.Gender,
                ConsultationFee = model.ConsultationFee,
                About = model.About
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            await _audit.LogAsync("Create", "Doctor", doctor.Id, $"Doctor {doctor.Name} added");
            TempData["SuccessMessage"] = $"Doctor {model.FullName} added successfully.";
            return RedirectToAction("Doctors");
        }

        // ---------- PROFILE ----------
        [HttpGet]
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