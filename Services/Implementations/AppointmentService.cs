using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clinic_Application_Doctor_Management.Data;
using Clinic_Application_Doctor_Management.Models;
using Clinic_Application_Doctor_Management.Services.Interfaces;

namespace Clinic_Application_Doctor_Management.Services.Implementations{
    public class AppointmentService : IAppointmentService{
        private readonly ApplicationDbContext _context;
        public AppointmentService(ApplicationDbContext context){
            _context = context;
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateAppointmentAsync(Appointment appointment){
            // Use appointment.DurationMinutes if you have it; otherwise default 30
            int duration = appointment.DurationMinutes > 0 ? appointment.DurationMinutes : 30;
            DateTime startDateTime = appointment.AppointmentDate.Add(appointment.AppointmentTime);
            DateTime endDateTime = startDateTime.AddMinutes(duration);

            // 1. Past date/time check — BLOCK any time in the past
            if (startDateTime < DateTime.Now){
                return (false, "Appointment date and time cannot be in the past.");
            }

            // 2. Doctor availability
            var dayOfWeek = startDateTime.DayOfWeek;
            var timeOfDay = startDateTime.TimeOfDay;
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.DoctorId == appointment.DoctorId
                    && s.DayOfWeek == dayOfWeek
                    && timeOfDay >= s.StartTime
                    && endDateTime.TimeOfDay <= s.EndTime
                    && s.IsActive);

            if (schedule == null){
                return (false, "Doctor is not available at the selected date and time.");
            }

            // 3. Patient overlapping appointments
            bool hasConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == appointment.PatientId
                    && a.Id != appointment.Id
                    && a.Status != "Cancelled"
                    && a.AppointmentDate.Add(a.AppointmentTime) < endDateTime
                    && startDateTime < a.AppointmentDate.Add(a.AppointmentTime));

            if (hasConflict){
                return (false, "You already have an overlapping appointment at that time.");
            }

            return (true, null);
        }
    }
}